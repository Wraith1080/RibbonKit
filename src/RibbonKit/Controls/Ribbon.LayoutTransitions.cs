using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using RibbonKit.Animation;

namespace RibbonKit.Controls;

public partial class Ribbon
{
    /// <summary>Raised synchronously before destination layout can be presented. Hosts may hold their previous content drawing.</summary>
    public event EventHandler<RibbonLayoutTransitionEventArgs>? LayoutTransitionPreparing;
    /// <summary>Raised after final measurement with progress staged at zero, immediately before RibbonKit starts its shared clock.</summary>
    public event EventHandler<RibbonLayoutTransitionEventArgs>? LayoutTransitionStarting;
    /// <summary>Raised exactly once for each prepared identity, including interruption or skipping before Starting. Host work is never awaited.</summary>
    public event EventHandler<RibbonLayoutTransitionEventArgs>? LayoutTransitionEnded;

    private LayoutTransitionCoordinator? _layoutCoordinator;
    private bool _changingSimplifiedCapability;

    private RibbonLayoutTransitionState LayoutState(RibbonLayoutMode? mode = null, RibbonDensity? density = null,
        RibbonQuickAccessPosition? position = null, bool? visible = null) => new(
            mode ?? LayoutMode, density ?? Density, position ?? QuickAccessPosition, visible ?? IsQuickAccessVisible, IsMinimized);

    private sealed class LayoutTransitionCoordinator
    {
        private readonly Ribbon _owner;
        private Session? _active;
        private (RibbonLayoutTransitionState Before, RibbonLayoutTransitionReason Reason)? _queued;
        private bool _notifying;
        private bool _processingRequests;
        private bool _cancelRequested;
        private long _nextId;

        internal LayoutTransitionCoordinator(Ribbon owner) => _owner = owner;

        internal void Request(RibbonLayoutTransitionState before, RibbonLayoutTransitionReason reason)
        {
            if (!_owner._densityTransitionsReady || !_owner.IsLoaded || !_owner.IsVisible
                || PresentationSource.FromVisual(_owner) is null || DesignerProperties.GetIsInDesignMode(_owner)) { Cancel(); return; }
            // Several effective properties can change inside one notification.
            // Keep the first old state, rather than reporting an intermediate
            // state from the last callback as the source of the combined move.
            _queued = _queued is { } queued ? (queued.Before, queued.Reason | reason) : (before, reason);
            if (_notifying || _processingRequests) return;
            _processingRequests = true;
            try
            {
                while (_queued is { } request)
                {
                    _queued = null;
                    Prepare(request.Before, request.Reason);
                }
            }
            finally { _processingRequests = false; }
        }

        private void Prepare(RibbonLayoutTransitionState before, RibbonLayoutTransitionReason reason)
        {
            _owner._densityTransitionGeneration++;
            _owner._simplifiedTabMotion?.Dispose(); _owner._simplifiedTabMotion = null;
            Session? previous = _active;
            if (previous is not null && !Connected(previous))
            { End(previous, RibbonLayoutTransitionOutcome.Interrupted); previous = null; }
            if (previous is { Started: false })
            { before = previous.Before; reason |= previous.Reason; }
            var target = _owner.LayoutState();
            if (before.LayoutMode != target.LayoutMode) reason |= RibbonLayoutTransitionReason.LayoutMode;
            if (before.Density != target.Density) reason |= RibbonLayoutTransitionReason.Density;
            if (before.QuickAccessPosition != target.QuickAccessPosition) reason |= RibbonLayoutTransitionReason.QuickAccessPlacement;
            if (before.IsQuickAccessVisible != target.IsQuickAccessVisible) reason |= RibbonLayoutTransitionReason.QuickAccessVisibility;
            RibbonAnimationAction action = before.LayoutMode != target.LayoutMode ? RibbonAnimationAction.LayoutModeChange
                : before.Density != target.Density ? RibbonAnimationAction.DensityChange : RibbonAnimationAction.QuickAccessMove;
            var (root, layer) = PresentationSurface();
            RibbonLayoutSnapshot? body = previous?.BodyMotion?.CapturePresentation()
                ?? previous?.BodyHold?.Snapshot
                ?? (_owner._ribbonContentHost is { IsVisible: true, ActualHeight: > 0 } content && !_owner.IsMinimized
                    ? RibbonLayoutSnapshot.Capture(content) : null);
            bool includeCaption = before.QuickAccessPosition == RibbonQuickAccessPosition.TitleBar
                || target.QuickAccessPosition == RibbonQuickAccessPosition.TitleBar;
            if (includeCaption && Window.GetWindow(_owner) is RibbonWindow captionWindow
                && captionWindow.RibbonPresentationSurface is null) layer = null;
            RibbonQuickAccessFrame qat = previous?.Qat?.CapturePresentation() ?? CaptureQat(root, before, includeCaption);
            RibbonLayoutTransitionGeometry geometry = Geometry(root, before, qat);
            if (previous is not null)
                geometry = new RibbonLayoutTransitionGeometry(qat.Occupied, geometry.PaintedBounds,
                    qat.Strip?.Bounds, qat.Panel?.Bounds, geometry.Dpi);
            var session = new Session(++_nextId, before, target, reason, root, geometry, action,
                new RibbonLayoutTransitionMotion(action, layer is not null && qat.IsSafe),
                before.QuickAccessPosition != target.QuickAccessPosition
                    ? new RibbonLayoutTransitionMotion(RibbonAnimationAction.QuickAccessMove, layer is not null && qat.IsSafe) : null);
            session.BodyBefore = body;
            session.Direction = _owner.FlowDirection;
            session.FocusedQuickAccess = _owner.QuickAccessItems.OfType<FrameworkElement>().Any(item => item.IsKeyboardFocusWithin)
                ? Keyboard.FocusedElement as UIElement : previous?.FocusedQuickAccess;
            session.BodyOrigin = previous?.BodyMotion?.PresentedOrigin ?? previous?.BodyHold?.Origin;
            if (layer is not null && qat.IsSafe && (session.Boundary.IsAnimated || session.QuickAccess?.IsAnimated == true))
            {
                session.Qat = new RibbonQuickAccessTransition(root, layer, qat);
                HideQat(session);
            }
            // Install the successor holds before notifying or releasing the old
            // presentation. Reentrant callbacks can force layout at this point.
            if (body is not null && session.Qat is not null && _owner._ribbonContentHost is { } surface && RibbonAnimation.IsEnabled(action))
                session.BodyHold = RibbonLayoutHold.Start(surface, body, null, session.BodyOrigin, root,
                    previous?.BodyMotion?.Opacity ?? previous?.BodyHold?.PresentationOpacity);
            session.BodyOrigin ??= session.BodyHold?.Origin;
            _active = session;
            // The old drawing is owned by its old session until End below. It
            // must not be treated as current by LayoutUpdated during callbacks.
            _owner._layoutMotion = null;
            AttachInput(session);
            _owner.IsLayoutMotionChanging = true;
            try
            {
                Notify(() => _owner.LayoutTransitionPreparing?.Invoke(_owner, Args(session)));
                if (previous is not null) End(previous, RibbonLayoutTransitionOutcome.Superseded);
                if (!ReferenceEquals(_active, session) || session.Ended) return;
                if (_cancelRequested || !_owner.IsLoaded || !_owner.IsVisible)
                { _cancelRequested = false; End(session, RibbonLayoutTransitionOutcome.Interrupted); return; }
                if (_queued is not null) return;
                if (!session.Boundary.IsAnimated && session.QuickAccess?.IsAnimated != true)
                { End(session, RibbonLayoutTransitionOutcome.Skipped); return; }
                _owner._layoutHold = session.BodyHold;
                _owner._pendingDensityTransition = _owner.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle,
                    (Action)(() => Start(session)));
            }
            catch
            {
                if (previous is not null) End(previous, RibbonLayoutTransitionOutcome.Interrupted);
                End(session, RibbonLayoutTransitionOutcome.Interrupted);
                throw;
            }
        }

        private (FrameworkElement Root, AdornerLayer? Layer) PresentationSurface()
        {
            if (Window.GetWindow(_owner) is RibbonWindow window && window.RibbonPresentationSurface is { } surface)
                return surface;
            FrameworkElement root = _owner;
            AdornerLayer? layer = null;
            for (DependencyObject? node = _owner; node is not null; node = VisualTreeHelper.GetParent(node))
                if (node is AdornerDecorator { Child: FrameworkElement child } decorator)
                { root = child; layer = decorator.AdornerLayer; }
            return (root, layer);
        }

        private RibbonQuickAccessFrame CaptureQat(FrameworkElement root, RibbonLayoutTransitionState state, bool includeCaption)
        {
            FrameworkElement? strip = !state.IsQuickAccessVisible ? null : state.QuickAccessPosition switch
            {
                RibbonQuickAccessPosition.TitleBar => _owner._titleBarQatHost,
                RibbonQuickAccessPosition.TabRow => _owner._qatTabRowHost,
                _ => FindDescendantByName(_owner, "QatBelowItems"),
            };
            FrameworkElement? panel = state.IsQuickAccessVisible && state.QuickAccessPosition == RibbonQuickAccessPosition.BelowRibbon
                ? FindDescendantByName(_owner, "QatBelowShadowHost") ?? _owner._qatBelowHost : null;
            FrameworkElement? header = _owner._ribbonTabControl?.TabHeaderHost;
            FrameworkElement? caption = includeCaption && Window.GetWindow(_owner) is RibbonWindow window
                ? window.RibbonPresentationCaption : null;
            return new RibbonQuickAccessFrame
            {
                IsSafe = (strip is null || !RibbonVectorSnapshot.ContainsUnsupported(strip))
                    && (header is null || !RibbonVectorSnapshot.ContainsUnsupported(header, _owner._qatTabRowHost))
                    && (caption is null || !RibbonVectorSnapshot.ContainsUnsupported(caption, _owner._titleBarQatHost))
                    && (panel is null || !RibbonVectorSnapshot.ContainsUnsupported(panel, strip))
                    && (_owner._ribbonContentHost is null || !RibbonVectorSnapshot.ContainsUnsupported(_owner._ribbonContentHost)),
                Header = RibbonVectorSnapshot.Capture(header, root, _owner._qatTabRowHost),
                Caption = RibbonVectorSnapshot.Capture(caption, root, _owner._titleBarQatHost),
                Strip = RibbonVectorSnapshot.Capture(strip, root),
                Panel = RibbonVectorSnapshot.Capture(panel, root, strip),
                Occupied = Occupied(root),
                CanTranslate = strip?.LayoutTransform.Value.IsIdentity != false,
            };
        }

        private Rect Occupied(FrameworkElement root)
        {
            Thickness margin = _owner.Margin;
            return _owner.TransformToVisual(root).TransformBounds(new Rect(-margin.Left, -margin.Top,
                Math.Max(0, _owner.ActualWidth + margin.Left + margin.Right),
                Math.Max(0, _owner.ActualHeight + margin.Top + margin.Bottom)));
        }

        private RibbonLayoutTransitionGeometry Geometry(FrameworkElement root, RibbonLayoutTransitionState state, RibbonQuickAccessFrame qat)
        {
            Rect occupied = Occupied(root), painted = occupied;
            if (qat.Strip is { } commands) painted.Union(commands.Bounds);
            if (qat.Panel is { } panel)
            {
                Rect halo = panel.Bounds;
                if ((FindDescendantByName(_owner, "QatBelowHost") ?? _owner._qatBelowHost)?.Effect is DropShadowEffect shadow)
                    halo.Inflate(shadow.BlurRadius + Math.Abs(shadow.ShadowDepth) + 1,
                        shadow.BlurRadius + Math.Abs(shadow.ShadowDepth) + 1);
                painted.Union(halo);
            }
            if (FindDescendantByName(_owner, "ContentHost") is { Effect: DropShadowEffect bodyShadow } body)
            {
                Rect halo = body.TransformToVisual(root).TransformBounds(new Rect(body.RenderSize));
                halo.Inflate(bodyShadow.BlurRadius + Math.Abs(bodyShadow.ShadowDepth),
                    bodyShadow.BlurRadius + Math.Abs(bodyShadow.ShadowDepth));
                painted.Union(halo);
            }
            return new RibbonLayoutTransitionGeometry(occupied, painted, qat.Strip?.Bounds, qat.Panel?.Bounds,
                VisualTreeHelper.GetDpi(root));
        }

        internal void HideCurrentQat() { if (_active is { Ended: false } session) HideQat(session); }
        private void HideQat(Session session)
        {
            foreach (var host in new[] { _owner._titleBarQatHost, _owner._qatTabRowHost,
                FindDescendantByName(_owner, "QatBelowShadowHost") ?? _owner._qatBelowHost,
                _owner._ribbonTabControl?.TabHeaderHost,
                (session.Before.QuickAccessPosition == RibbonQuickAccessPosition.TitleBar || session.Target.QuickAccessPosition == RibbonQuickAccessPosition.TitleBar)
                    && Window.GetWindow(_owner) is RibbonWindow window ? window.RibbonPresentationCaption : null }.OfType<FrameworkElement>())
                if (!session.Hidden.ContainsKey(host)) session.Hidden.Add(host, RibbonLayoutDrawing.HideImmediately(host));
        }
        private static void ShowQat(Session session)
        {
            foreach (var clock in session.Hidden.Values) RibbonLayoutDrawing.ReleaseHide(clock);
            session.Hidden.Clear();
        }

        private void Start(Session session)
        {
            if (!ReferenceEquals(_active, session) || session.Ended) return;
            _owner._pendingDensityTransition = null;
            try
            {
                if (!_owner.IsLoaded || !_owner.IsVisible || !Connected(session)) { End(session, RibbonLayoutTransitionOutcome.Interrupted); return; }
                (Window.GetWindow(_owner) as FrameworkElement ?? _owner).UpdateLayout();
                if (!ReferenceEquals(_active, session)) return;
                if (!Connected(session) || !_owner.IsLoaded || !_owner.IsVisible)
                { End(session, RibbonLayoutTransitionOutcome.Interrupted); return; }
                _owner.RefreshSelectionVisuals();
                if (!ReferenceEquals(_active, session)) return;
                if (!Connected(session) || !_owner.IsLoaded || !_owner.IsVisible)
                { End(session, RibbonLayoutTransitionOutcome.Interrupted); return; }
                // Temporarily release only our hide clocks to capture the real
                // committed drawing. No dispatcher/render boundary occurs here.
                ShowQat(session);
                var finalQat = CaptureQat(session.Root, session.Target,
                    session.Before.QuickAccessPosition == RibbonQuickAccessPosition.TitleBar || session.Target.QuickAccessPosition == RibbonQuickAccessPosition.TitleBar);
                session.Final = Geometry(session.Root, session.Target, finalQat);
                if (session.FocusedQuickAccess is { IsVisible: true, IsEnabled: true } focus
                    && (Keyboard.FocusedElement is null || ReferenceEquals(Keyboard.FocusedElement, focus))) focus.Focus();
                HideQat(session);
                bool safe = session.Qat is { MatchesRoot: true } && finalQat.IsSafe && !_owner.HasLayoutMotionPopup()
                    && (session.BodyBefore is not null || session.Qat.HasPresentation || finalQat.Strip is not null || finalQat.Panel is not null);
                session.Boundary = new RibbonLayoutTransitionMotion(session.Action, safe);
                if (session.QuickAccess is not null)
                    session.QuickAccess = new RibbonLayoutTransitionMotion(RibbonAnimationAction.QuickAccessMove,
                        safe && (session.Qat!.HasPresentation || finalQat.Strip is not null));
                session.BodyHold?.Dispose(); session.BodyHold = null;
                _owner._layoutHold = null;
                if (session.BodyBefore is { } before && _owner._ribbonContentHost is { IsVisible: true } body
                    && session.Boundary.IsAnimated && !_owner.IsMinimized)
                {
                    session.BodyMotion = RibbonLayoutTransition.Start(body, before, session.Action, () => { }, session.Boundary, session.BodyOrigin, session.Root);
                    _owner._layoutMotion = session.BodyMotion;
                }
                session.Qat?.Start(finalQat, session.Boundary, session.QuickAccess ?? session.Boundary);
                if (!session.Boundary.IsAnimated && session.QuickAccess?.IsAnimated != true)
                { End(session, RibbonLayoutTransitionOutcome.Skipped); return; }
                var timeline = new ParallelTimeline();
                timeline.Children.Add(session.Boundary.Timeline());
                if (session.QuickAccess is { } qat) timeline.Children.Add(qat.Timeline());
                session.Clock = (ClockGroup)timeline.CreateClock(true);
                session.Boundary.Attach((AnimationClock)session.Clock.Children[0]);
                if (session.QuickAccess is { } quick) quick.Attach((AnimationClock)session.Clock.Children[1]);
                session.Clock.Completed += (_, _) =>
                { if (ReferenceEquals(_active, session) && !session.Ended) End(session, RibbonLayoutTransitionOutcome.Completed); };
                session.Started = true;
                Notify(() => _owner.LayoutTransitionStarting?.Invoke(_owner, Args(session)));
                if (_cancelRequested)
                { _cancelRequested = false; End(session, RibbonLayoutTransitionOutcome.Interrupted); }
                else if (_queued is { } next)
                { _queued = null; Request(next.Before, next.Reason); }
                else if (ReferenceEquals(_active, session) && !session.Ended)
                {
                    _owner.IsLayoutMotionChanging = false;
                    session.Clock.Controller!.Begin();
                    session.Clock.Controller.SeekAlignedToLastTick(TimeSpan.Zero, TimeSeekOrigin.BeginTime);
                }
            }
            catch { End(session, RibbonLayoutTransitionOutcome.Interrupted); throw; }
        }

        private void Notify(Action notify)
        {
            bool previous = _notifying; _notifying = true;
            try { notify(); } finally { _notifying = previous; }
        }

        internal void Validate()
        {
            if (_active is { Started: true, Ended: false } session && session.Final is { } final
                && (!Connected(session) || session.Qat?.MatchesRoot == false || _owner.FlowDirection != session.Direction
                    || Occupied(session.Root) != final.OccupiedBounds || CurrentQatBounds(session) != final.QuickAccessBounds
                    || _owner.HasLayoutMotionPopup())) Cancel();
        }

        private Rect? CurrentQatBounds(Session session)
        {
            FrameworkElement? strip = !session.Target.IsQuickAccessVisible ? null : session.Target.QuickAccessPosition switch
            {
                RibbonQuickAccessPosition.TitleBar => _owner._titleBarQatHost,
                RibbonQuickAccessPosition.TabRow => _owner._qatTabRowHost,
                _ => FindDescendantByName(_owner, "QatBelowItems"),
            };
            if (strip is null || strip.ActualWidth <= 0 || strip.ActualHeight <= 0) return null;
            try { return strip.TransformToVisual(session.Root).TransformBounds(new Rect(strip.RenderSize)); }
            catch (InvalidOperationException) { return null; }
        }

        private bool Connected(Session session) => ReferenceEquals(_owner, session.Root)
            || _owner.FindCommonVisualAncestor(session.Root) is not null;

        internal void Cancel()
        {
            if (_notifying) { if (_active is not null) _cancelRequested = true; return; }
            _queued = null;
            if (_active is { } session) End(session, RibbonLayoutTransitionOutcome.Interrupted);
        }

        private void AttachInput(Session session)
        {
            session.InputRoot = Window.GetWindow(_owner) as UIElement ?? _owner;
            session.InputRoot.AddHandler(Mouse.PreviewMouseDownEvent, new MouseButtonEventHandler(OnInput), true);
            session.InputRoot.AddHandler(Mouse.PreviewMouseWheelEvent, new MouseWheelEventHandler(OnInput), true);
            session.InputRoot.AddHandler(Keyboard.PreviewKeyDownEvent, new KeyEventHandler(OnInput), true);
            session.InputRoot.AddHandler(UIElement.PreviewTouchDownEvent, new EventHandler<TouchEventArgs>(OnInput), true);
            session.InputRoot.AddHandler(Stylus.PreviewStylusDownEvent, new StylusDownEventHandler(OnInput), true);
        }
        private void OnInput(object? sender, InputEventArgs args) => Cancel();

        private void End(Session session, RibbonLayoutTransitionOutcome outcome)
        {
            if (session.Ended) return;
            session.Ended = true;
            session.Clock?.Controller?.Remove();
            session.Boundary.Detach(outcome == RibbonLayoutTransitionOutcome.Completed);
            session.QuickAccess?.Detach(outcome == RibbonLayoutTransitionOutcome.Completed);
            session.BodyHold?.Dispose(); session.BodyMotion?.Dispose(); session.Qat?.Dispose(); ShowQat(session);
            if (session.InputRoot is { } input)
            {
                input.RemoveHandler(Mouse.PreviewMouseDownEvent, new MouseButtonEventHandler(OnInput));
                input.RemoveHandler(Mouse.PreviewMouseWheelEvent, new MouseWheelEventHandler(OnInput));
                input.RemoveHandler(Keyboard.PreviewKeyDownEvent, new KeyEventHandler(OnInput));
                input.RemoveHandler(UIElement.PreviewTouchDownEvent, new EventHandler<TouchEventArgs>(OnInput));
                input.RemoveHandler(Stylus.PreviewStylusDownEvent, new StylusDownEventHandler(OnInput));
            }
            if (ReferenceEquals(_active, session))
            {
                _active = null;
                _owner._pendingDensityTransition?.Abort(); _owner._pendingDensityTransition = null;
                _owner._layoutMotion = null; _owner._layoutHold = null; _owner.IsLayoutMotionChanging = false;
            }
            Notify(() => _owner.LayoutTransitionEnded?.Invoke(_owner, Args(session, outcome)));
            if (!_notifying && !_processingRequests && _queued is { } next)
            { _queued = null; Request(next.Before, next.Reason); }
        }

        private static RibbonLayoutTransitionEventArgs Args(Session s, RibbonLayoutTransitionOutcome? outcome = null) => new(
            s.Id, s.Reason, s.Before, s.Target, s.Root, s.Previous, s.Final, s.Boundary, s.QuickAccess, s.Started, outcome);

        private sealed class Session(long id, RibbonLayoutTransitionState before, RibbonLayoutTransitionState target,
            RibbonLayoutTransitionReason reason, FrameworkElement root, RibbonLayoutTransitionGeometry previous,
            RibbonAnimationAction action, RibbonLayoutTransitionMotion boundary, RibbonLayoutTransitionMotion? quickAccess)
        {
            internal long Id = id;
            internal RibbonLayoutTransitionState Before = before, Target = target;
            internal RibbonLayoutTransitionReason Reason = reason;
            internal FrameworkElement Root = root;
            internal RibbonLayoutTransitionGeometry Previous = previous;
            internal RibbonLayoutTransitionGeometry? Final;
            internal RibbonAnimationAction Action = action;
            internal RibbonLayoutTransitionMotion Boundary = boundary;
            internal RibbonLayoutTransitionMotion? QuickAccess = quickAccess;
            internal RibbonLayoutSnapshot? BodyBefore;
            internal Point? BodyOrigin;
            internal RibbonLayoutHold? BodyHold;
            internal RibbonLayoutTransition? BodyMotion;
            internal RibbonQuickAccessTransition? Qat;
            internal ClockGroup? Clock;
            internal UIElement? InputRoot;
            internal UIElement? FocusedQuickAccess;
            internal bool Started, Ended;
            internal FlowDirection Direction;
            internal Dictionary<FrameworkElement, AnimationClock> Hidden = new();
        }
    }
}
