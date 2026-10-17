using System.Diagnostics;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using RibbonKit.Animation;
using RibbonKit.Controls;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Portability.Tests;

// This consumer references RibbonKit alone. Its content animator below is
// independently authored against the public lifecycle/progress contract.
internal static class HostLayoutTransitionPortabilityChecks
{
    internal static void Verify(Application app)
    {
        var level = RibbonAnimation.GlobalLevel; bool reduce = RibbonAnimation.RespectSystemReduceMotion;
        var actions = new[] { RibbonAnimationAction.LayoutModeChange, RibbonAnimationAction.DensityChange, RibbonAnimationAction.QuickAccessMove };
        var overrides = actions.ToDictionary(a => a, RibbonAnimation.GetActionOverride);
        try
        {
            RibbonAnimation.GlobalLevel = RibbonAnimationLevel.Subtle; RibbonAnimation.RespectSystemReduceMotion = false;
            foreach (var action in actions) RibbonAnimation.ClearActionLevel(action);
            VerifyCaptionPaint(app);
            foreach (var theme in new[] { RibbonTheme.Office2010, RibbonTheme.Office2024Flat, RibbonTheme.CrystalLight })
            foreach (var direction in new[] { FlowDirection.LeftToRight, FlowDirection.RightToLeft })
            foreach (var density in new[] { RibbonDensity.Compact, RibbonDensity.Touch })
            foreach (bool synchronize in new[] { false, true })
                VerifyCase(app, theme, direction, density, synchronize);
        }
        finally
        {
            RibbonAnimation.GlobalLevel = level; RibbonAnimation.RespectSystemReduceMotion = reduce;
            foreach (var action in actions)
                if (overrides[action] is { } value) RibbonAnimation.SetActionLevel(action, value); else RibbonAnimation.ClearActionLevel(action);
        }
    }

    private static void VerifyCaptionPaint(Application app)
    {
        ThemeManager.Apply(app, RibbonTheme.Office2024Flat);
        var color = Color.FromRgb(0x87, 0x64, 0xB8);
        var ribbon = new Ribbon { QuickAccessPosition = RibbonQuickAccessPosition.TitleBar, Background = Brushes.Navy };
        var tab = new RibbonTab { Header = "Home" };
        var group = new RibbonGroup { Header = "Commands" };
        var command = new RibbonButton { Header = "Save",
            Icon = new DrawingImage(new GeometryDrawing(Brushes.Magenta, null, new RectangleGeometry(new Rect(0, 0, 16, 16)))) };
        group.Items.Add(command); tab.Groups.Add(group); ribbon.Tabs.Add(tab); ribbon.SelectedTab = tab;
        ribbon.AddToQuickAccess(command);
        var dock = new DockPanel(); DockPanel.SetDock(ribbon, Dock.Top); dock.Children.Add(ribbon);
        dock.Children.Add(new Border { Background = Brushes.White });
        var window = new RibbonWindow { Content = dock, Title = "Steady consumer title", Width = 700, Height = 350,
            Background = Brushes.White, Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
        window.Resources["RibbonKit.Brushes.TitleBar.Background"] = new SolidColorBrush(color);
        RibbonLayoutTransitionMotion? motion = null;
        RibbonLayoutTransitionEventArgs? transition = null;
        bool ended = false; int samples = 0;
        byte[]? titleInk = null, qatInk = null;
        var progressProperty = DependencyPropertyDescriptor.FromProperty(RibbonLayoutTransitionMotion.ProgressProperty, typeof(RibbonLayoutTransitionMotion));
        ribbon.LayoutTransitionStarting += (_, e) =>
        {
            motion = e.BoundaryMotion; transition = e;
            titleInk = motion.Action == RibbonAnimationAction.LayoutModeChange ? TitlePixels() : null;
            qatInk = titleInk is not null ? QatPixels() : null;
            progressProperty.AddValueChanged(motion, SamplePaint);
        };
        ribbon.LayoutTransitionEnded += (_, e) =>
        {
            progressProperty.RemoveValueChanged(e.BoundaryMotion, SamplePaint);
            Console.WriteLine($"Caption paint {e.Reason}: {e.Outcome}, progress={motion?.Progress}, samples={samples}.");
            ended = true;
        };
        void SamplePaint(object? sender, EventArgs args)
        {
            if (motion is null || ended || motion.Progress <= 0 || motion.Progress >= 1) return;
            var bitmap = new RenderTargetBitmap(700, 350, 96, 96, PixelFormats.Pbgra32); bitmap.Render(window);
            byte[] pixel = new byte[4]; bitmap.CopyPixels(new Int32Rect(450, 12, 1, 1), pixel, 4, 0);
            Assert.Equal(color, Color.FromArgb(pixel[3], pixel[2], pixel[1], pixel[0])); samples++;
            double top = transition!.PreviousGeometry.OccupiedBounds.Top
                + (transition.FinalGeometry!.OccupiedBounds.Top - transition.PreviousGeometry.OccupiedBounds.Top) * motion.Progress;
            Point header = transition.CoordinateRoot.TranslatePoint(new Point(450, top + 2), window);
            bitmap.CopyPixels(new Int32Rect((int)header.X, (int)header.Y, 1, 1), pixel, 4, 0);
            Assert.Equal(Colors.Navy, Color.FromArgb(pixel[3], pixel[2], pixel[1], pixel[0]));
            if (titleInk is not null) Assert.Equal(titleInk, TitlePixels());
            if (qatInk is not null) Assert.Equal(qatInk, QatPixels());
        }
        byte[] TitlePixels()
            => Pixels(new Int32Rect(220, 10, 200, 16));
        byte[] QatPixels()
        {
            var qat = (FrameworkElement)ribbon.QuickAccessItems[0];
            Rect bounds = qat.TransformToVisual(window).TransformBounds(new Rect(qat.RenderSize));
            return Pixels(new Int32Rect((int)Math.Ceiling(bounds.X) + 1, (int)Math.Ceiling(bounds.Y) + 1,
                (int)Math.Floor(bounds.Width) - 2, (int)Math.Floor(bounds.Height) - 2));
        }
        byte[] Pixels(Int32Rect rect)
        {
            var bitmap = new RenderTargetBitmap(700, 350, 96, 96, PixelFormats.Pbgra32); bitmap.Render(window);
            byte[] pixels = new byte[rect.Width * rect.Height * 4];
            bitmap.CopyPixels(rect, pixels, rect.Width * 4, 0);
            return pixels;
        }
        try
        {
            window.Show(); Drain(); window.UpdateLayout();
            bool rendered = false; EventHandler onRender = (_, _) => rendered = true;
            CompositionTarget.Rendering += onRender;
            try { PumpUntil(() => rendered); } finally { CompositionTarget.Rendering -= onRender; }
            ribbon.LayoutMode = RibbonLayoutMode.Simplified;
            PumpUntil(() => ended); Assert.True(samples > 0);
            ended = false; samples = 0; motion = null; ribbon.Density = RibbonDensity.Touch;
            PumpUntil(() => ended); Assert.True(samples > 0);
            Console.WriteLine($"Independent accent-caption paint: {samples} intermediate samples passed.");
        }
        finally
        {
            if (motion is not null) progressProperty.RemoveValueChanged(motion, SamplePaint);
            window.Close(); Drain();
        }
    }

    private static void VerifyCase(Application app, RibbonTheme theme, FlowDirection direction, RibbonDensity density, bool synchronize)
    {
        Console.WriteLine($"Host layout: {theme}/{direction}/{density}/synchronize={synchronize}");
        ThemeManager.Apply(app, theme);
        var ribbon = new Ribbon { QuickAccessPosition = RibbonQuickAccessPosition.TitleBar, Density = density };
        var group = new RibbonGroup { Header = "Consumer commands" };
        var command = new RibbonButton { Header = "Save", Icon = new DrawingImage(new GeometryDrawing(Brushes.SteelBlue, null, new RectangleGeometry(new Rect(0, 0, 16, 16)))) };
        Ribbon.SetSimplifiedPlacement(command, RibbonSimplifiedPlacement.Inline); group.Items.Add(command);
        var tab = new RibbonTab { Header = "Home" }; tab.Groups.Add(group); ribbon.Tabs.Add(tab); ribbon.SelectedTab = tab;
        ribbon.AddToQuickAccess(command);
        var text = new TextBox { Text = "Public-only consumer content\nText and icons stay at their natural scale.", AcceptsReturn = true, Padding = new Thickness(20) };
        var content = new Border { Background = Brushes.White, Child = text };
        var dock = new DockPanel(); DockPanel.SetDock(ribbon, Dock.Top); dock.Children.Add(ribbon); dock.Children.Add(content);
        var window = new RibbonWindow { Content = dock, FlowDirection = direction, Width = 900, Height = 550,
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
        ConsumerAnimator? animator = synchronize ? new ConsumerAnimator(ribbon, content) : null;
        try
        {
            window.Show(); Drain(); window.UpdateLayout();
            if (density == RibbonDensity.Touch)
            { VisualTreeHelper.SetRootDpi(window, new DpiScale(1.5, 1.5)); window.UpdateLayout(); Drain(); }
            bool rendered = false; EventHandler onRender = (_, _) => rendered = true;
            CompositionTarget.Rendering += onRender;
            try { PumpUntil(() => rendered); } finally { CompositionTarget.Rendering -= onRender; }
            foreach (var position in new[] { RibbonQuickAccessPosition.BelowRibbon, RibbonQuickAccessPosition.TitleBar,
                RibbonQuickAccessPosition.TabRow, RibbonQuickAccessPosition.BelowRibbon, RibbonQuickAccessPosition.TabRow })
            {
                FrameworkElement original = (FrameworkElement)ribbon.QuickAccessItems[0];
                Size before = content.RenderSize;
                ribbon.SetCurrentValue(Ribbon.QuickAccessPositionProperty, position);
                Assert.Single(QatScenes(window));
                // Force a destination layout/render before Background/ContextIdle.
                // The old inert presentation must still cover the real controls.
                Dispatcher.CurrentDispatcher.Invoke(() => window.UpdateLayout(), DispatcherPriority.Render);
                Assert.Single(QatScenes(window));
                Drain(); window.UpdateLayout();
                Assert.Single(QatScenes(window));
                Assert.Same(original, ribbon.QuickAccessItems[0]); Assert.NotNull(VisualTreeHelper.GetParent(original));
                if (synchronize && before != content.RenderSize && animator!.Active.Count > 0) Assert.Equal(0, content.Opacity);
                WaitForIntermediate(window, theme, direction, density, synchronize, position);
                PumpUntil(() => QatScenes(window).Length == 0);
                Assert.Equal(1, content.Opacity);
                Assert.Same(original, ribbon.QuickAccessItems[0]);
                AssertNoShadowAncestor(command);
                AssertNoShadowAncestor(original);
                Assert.All(QatScenes(window), a => Assert.False(a.IsHitTestVisible));
            }
            ribbon.LayoutMode = RibbonLayoutMode.Simplified;
            Drain(); PumpUntil(() => QatScenes(window).Length == 0);
            ribbon.Density = density == RibbonDensity.Compact ? RibbonDensity.Touch : RibbonDensity.Compact;
            Drain(); PumpUntil(() => QatScenes(window).Length == 0);
            if (animator is not null)
            { Assert.Equal(animator.Prepared, animator.Ended); Assert.True(animator.Started >= 7); Assert.Empty(animator.Active); }
        }
        finally { animator?.Dispose(); window.Close(); Drain(); }
    }

    private static void WaitForIntermediate(Window window, RibbonTheme theme, FlowDirection direction, RibbonDensity density,
        bool synchronize, RibbonQuickAccessPosition position)
    {
        string? folder = Environment.GetEnvironmentVariable("RIBBONKIT_LAYOUT_MOTION_DIAGNOSTICS");
        if (string.IsNullOrEmpty(folder)) return;
        var watch = Stopwatch.StartNew(); PumpUntil(() => watch.ElapsedMilliseconds >= 55);
        Directory.CreateDirectory(folder);
        var bitmap = new RenderTargetBitmap((int)window.ActualWidth, (int)window.ActualHeight, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(window);
        var png = new PngBitmapEncoder(); png.Frames.Add(BitmapFrame.Create(bitmap));
        using var output = File.Create(Path.Combine(folder, $"{theme}-{direction}-{density}-{synchronize}-{position}.png")); png.Save(output);
    }

    private static Adorner[] QatScenes(Window window) => Descendants(window).OfType<AdornerDecorator>()
        .Where(d => d.Child is not null).SelectMany(d => d.AdornerLayer.GetAdorners(d.Child!) ?? [])
        .Where(a => a.GetType().Name == "RibbonQuickAccessTransition").ToArray();
    private static IEnumerable<DependencyObject> Descendants(DependencyObject node)
    { for (int i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++) { var child = VisualTreeHelper.GetChild(node, i); yield return child; foreach (var descendant in Descendants(child)) yield return descendant; } }
    private static void Drain() => Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
    private static void AssertNoShadowAncestor(DependencyObject foreground)
    {
        for (DependencyObject? node = foreground; node is not null; node = VisualTreeHelper.GetParent(node))
            if (node is UIElement element) Assert.False(element.Effect is DropShadowEffect,
                "A command must remain outside the decorative shadow's effect surface.");
    }
    private static void PumpUntil(Func<bool> condition)
    {
        var watch = Stopwatch.StartNew(); var frame = new DispatcherFrame();
        var timer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(4) };
        timer.Tick += (_, _) => { if (condition() || watch.ElapsedMilliseconds > 3000) frame.Continue = false; };
        timer.Start(); try { Dispatcher.PushFrame(frame); } finally { timer.Stop(); }
        Assert.True(condition(), "Consumer transition did not settle.");
    }

    private sealed class ConsumerAnimator : IDisposable
    {
        private readonly Ribbon _ribbon; private readonly Border _content;
        internal int Prepared, Started, Ended;
        internal Dictionary<long, ContentAdorner> Active = new();
        internal ConsumerAnimator(Ribbon ribbon, Border content)
        {
            _ribbon = ribbon; _content = content;
            ribbon.LayoutTransitionPreparing += Prepare; ribbon.LayoutTransitionStarting += Start; ribbon.LayoutTransitionEnded += End;
        }
        private void Prepare(object? sender, RibbonLayoutTransitionEventArgs e)
        {
            Prepared++;
            var layer = AdornerLayer.GetAdornerLayer(e.CoordinateRoot);
            Assert.NotNull(layer);
            Active.Add(e.TransitionId, new ContentAdorner(e.CoordinateRoot, layer, _content));
        }
        private void Start(object? sender, RibbonLayoutTransitionEventArgs e)
        {
            Started++;
            Assert.Equal(0, _content.Opacity); // Before the shared clock is released.
            Assert.Equal(0, e.BoundaryMotion.Progress);
            Assert.NotNull(e.FinalGeometry);
            if (e.FinalGeometry.QuickAccessBounds is { } qat) Assert.True(e.FinalGeometry.PaintedBounds.Contains(qat));
            if (e.FinalGeometry.QuickAccessPanelBounds is { } panel) Assert.True(e.FinalGeometry.PaintedBounds.Contains(panel));
            Active[e.TransitionId].Start(e.BoundaryMotion);
        }
        private void End(object? sender, RibbonLayoutTransitionEventArgs e)
        { Ended++; if (Active.Remove(e.TransitionId, out var presentation)) presentation.Dispose(); }
        public void Dispose()
        {
            _ribbon.LayoutTransitionPreparing -= Prepare; _ribbon.LayoutTransitionStarting -= Start; _ribbon.LayoutTransitionEnded -= End;
            foreach (var presentation in Active.Values) presentation.Dispose(); Active.Clear();
        }
    }

    private sealed class ContentAdorner : Adorner, IDisposable
    {
        private static readonly DependencyProperty ProgressProperty = DependencyProperty.Register("Progress", typeof(double), typeof(ContentAdorner),
            new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsRender));
        private readonly FrameworkElement _root; private readonly Border _content; private readonly AdornerLayer _layer;
        private readonly DrawingGroup _drawing; private readonly Rect _before;
        private Rect _after; private AnimationClock? _hide;
        internal ContentAdorner(FrameworkElement root, AdornerLayer layer, Border content) : base(root)
        {
            _root = root; _content = content; _layer = layer;
            IsHitTestVisible = false; Focusable = false;
            _drawing = Capture(content); _before = _after = content.TransformToVisual(root).TransformBounds(new Rect(content.RenderSize));
            _hide = new DoubleAnimation(0, 0, TimeSpan.FromDays(1)) { FillBehavior = FillBehavior.Stop }.CreateClock();
            content.ApplyAnimationClock(OpacityProperty, _hide, HandoffBehavior.Compose);
            _hide.Controller!.Begin(); _hide.Controller.SeekAlignedToLastTick(TimeSpan.Zero, TimeSeekOrigin.BeginTime);
            layer.Add(this);
        }
        internal void Start(RibbonLayoutTransitionMotion motion)
        {
            _after = _content.TransformToVisual(_root).TransformBounds(new Rect(_content.RenderSize));
            SetBinding(ProgressProperty, new Binding(nameof(RibbonLayoutTransitionMotion.Progress)) { Source = motion });
        }
        protected override void OnRender(DrawingContext dc)
        {
            double p = Math.Clamp((double)GetValue(ProgressProperty), 0, 1);
            double top = _before.Top + (_after.Top - _before.Top) * p;
            dc.PushClip(new RectangleGeometry(new Rect(_before.Left, top, _after.Width,
                Math.Max(0, _before.Height + (_after.Height - _before.Height) * p))));
            dc.PushTransform(new TranslateTransform(_before.Left, top));
            dc.DrawDrawing(_drawing); dc.Pop(); dc.Pop();
        }
        private static DrawingGroup Capture(Visual source)
        {
            var group = new DrawingGroup(); if (VisualTreeHelper.GetDrawing(source) is { } drawing) group.Children.Add(drawing.CloneCurrentValue());
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(source); i++)
            {
                if (VisualTreeHelper.GetChild(source, i) is not Visual child) continue;
                var copy = Capture(child); var t = child.TransformToAncestor(source);
                Point o = t.Transform(new Point()), x = t.Transform(new Point(1, 0)), y = t.Transform(new Point(0, 1));
                copy.Transform = new MatrixTransform(new Matrix(x.X - o.X, x.Y - o.Y, y.X - o.X, y.Y - o.Y, o.X, o.Y)); group.Children.Add(copy);
            }
            return group;
        }
        public void Dispose()
        {
            BindingOperations.ClearBinding(this, ProgressProperty);
            _hide?.Controller?.SeekAlignedToLastTick(TimeSpan.FromDays(1), TimeSeekOrigin.BeginTime); _hide?.Controller?.Remove(); _hide = null;
            _layer.Remove(this);
        }
    }
}
