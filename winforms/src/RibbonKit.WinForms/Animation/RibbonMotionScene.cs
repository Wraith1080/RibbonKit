using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

// One capture per committed endpoint, including owned native editor HWNDs.
internal sealed class RibbonMotionScene : IDisposable
{
    private const int SubtleTabSlide = 8, ExpressiveTabSlide = 12; // 96-DPI logical pixels; body drawing remains natural size.
    private const double IndicatorSpring = .35; // Matches the WPF marker's gentle BackEase amplitude.
    internal readonly Bitmap Image;
    internal readonly Rectangle Bounds, Header, Body, Frame, Qat, Strip, Messages;
    internal readonly RibbonQuickAccessPosition Placement;
    internal readonly bool Joined, BottomConnected, Minimized;
    internal readonly int Dpi;
    internal readonly Rectangle Indicator, IndicatorClip;
    internal readonly RibbonTab? ActiveTab;
    internal readonly Rectangle TabViewport;
    internal readonly Dictionary<RibbonTab, Rectangle> Headers;
    internal readonly Dictionary<RibbonMessage, MessageRow> MessageRows;
    internal readonly record struct MessageRow(Rectangle Bounds, Rectangle Content);
    private readonly record struct MessageTrack(RibbonMotionScene Scene, MessageRow Row, Rectangle From, Rectangle To, bool Arriving, bool Leaving);
    private RibbonMotionScene? _messageDeparture;
    private MessageTrack[] _messageTracks = [];
    private RibbonMotionScene? _headerDeparture;
    private HeaderTrack[] _headerTracks = [];
    internal readonly record struct HeaderTrack(Rectangle Bounds, bool Arriving);
    internal RibbonMotionScene(RibbonControl ribbon)
    {
        var snapshot = ribbon.Snapshot; Dpi = ribbon.DeviceDpi; Bounds = ribbon.RectangleToScreen(ribbon.ClientRectangle);
        Header = new(0, ribbon.WindowCaptionHeight, ribbon.Width, RibbonLayoutEngine.Scale(ribbon.LayoutTheme.TabHeight, Dpi));
        Body = snapshot.BodyBounds; Frame = snapshot.BodyFrameBounds; Qat = snapshot.QuickAccessBounds; Messages = snapshot.MessageBounds;
        MessageRows = snapshot.Messages.ToDictionary(row => row.Message, row =>
        {
            var content = Rectangle.Empty;
            foreach (var slot in new[] { row.Title, row.Text, row.Icon, row.Action, row.Close })
                if (slot.Width > 0 && slot.Height > 0) content = content.IsEmpty ? slot : Rectangle.Union(content, slot);
            return new MessageRow(row.Bounds, Rectangle.Intersect(row.Bounds, content));
        });
        var strip = snapshot.Targets.Where(t => (t.Kind is TargetKind.QuickAccess or TargetKind.QuickAccessOverflow) && t.Element is not RibbonSimplifiedMore).Select(t => t.VisibleBounds).Where(b => !b.IsEmpty).ToArray();
        Strip = strip.Length == 0 ? Rectangle.Empty : strip.Aggregate(Rectangle.Union);
        Placement = ribbon.QuickAccessPosition; Joined = ribbon.JoinBelowRibbonQuickAccess && !Body.IsEmpty; BottomConnected = ribbon.MotionBottomConnected;
        ActiveTab = ribbon.SelectedTab; Minimized = ribbon.IsMinimized;
        TabViewport = snapshot.TabViewport;
        Headers = snapshot.Targets.Where(t => t.Kind == TargetKind.Tab && t.Element is RibbonTab).ToDictionary(t => (RibbonTab)t.Element!, t => t.Bounds);
        if (ActiveTab is not null && snapshot.Targets.FirstOrDefault(t => t.Kind == TargetKind.Tab && ReferenceEquals(t.Element, ActiveTab)) is { } active)
        {
            using var measurement = Graphics.FromHwnd(ribbon.Handle);
            Indicator = ribbon.Painter.GetTabIndicatorBounds(new(measurement, active.Bounds, ribbon.Theme, ribbon.Font, Dpi) { Density = ribbon.Density }, ActiveTab);
            IndicatorClip = Rectangle.Intersect(Header, active.Clip);
        }
        // TextRenderer/native GDI writes RGB glyphs without maintaining alpha.
        // Endpoint surfaces are opaque; only the final drawing plane needs alpha.
        Image = new(ribbon.Width, ribbon.Height, PixelFormat.Format24bppRgb);
        bool previous = ribbon.CapturingMotionIndicators; ribbon.CapturingMotionIndicators = true;
        try
        {
            Capture(ribbon, ribbon, Image);
        }
        catch { Image.Dispose(); throw; }
        finally { ribbon.CapturingMotionIndicators = previous; }
    }
    public void Dispose() => Image.Dispose();
    internal static void Capture(RibbonControl ribbon, Control surface, Bitmap image)
    {
        PrintClient(surface, image); CaptureChildren(ribbon, surface.Controls.Cast<Control>(), image);
    }
    internal static void CaptureChildren(RibbonControl ribbon, IEnumerable<Control> children, Bitmap image)
    {
        using var graphics = Graphics.FromImage(image);
        foreach (Control child in children)
        {
            if (!child.Visible || child.Width < 1 || child.Height < 1 || ribbon.Editors.PaintMotionScene(child, graphics)) continue;
            using var childImage = new Bitmap(child.Width, child.Height, PixelFormat.Format24bppRgb);
            child.DrawToBitmap(childImage, new(Point.Empty, childImage.Size)); var saved = graphics.Save();
            try { graphics.TranslateTransform(child.Left, child.Top); if (child.Region is { } region) graphics.SetClip(region, CombineMode.Intersect); graphics.DrawImageUnscaled(childImage, Point.Empty); }
            finally { graphics.Restore(saved); }
        }
    }
    internal bool HasHeaderArrivals(RibbonControl ribbon) => ribbon.Snapshot.Targets.Any(t => t.Kind == TargetKind.Tab && t.Element is RibbonTab tab && !Headers.ContainsKey(tab) && t.VisibleBounds.Width > 0 && t.VisibleBounds.Height > 0);
    internal bool HasMessageChanges(RibbonControl ribbon) => ribbon.Snapshot.Messages.Count != MessageRows.Count || ribbon.Snapshot.Messages.Any(row => !MessageRows.ContainsKey(row.Message));
    private MessageTrack[] MessageTracksFrom(RibbonMotionScene from)
    {
        if (ReferenceEquals(_messageDeparture, from)) return _messageTracks;
        _messageDeparture = from;
        if (MessageRows.Count == from.MessageRows.Count && MessageRows.Keys.All(from.MessageRows.ContainsKey)) return _messageTracks = [];
        var tracks = new List<MessageTrack>();
        int removedBefore = 0;
        foreach (var (message, row) in from.MessageRows)
            if (!MessageRows.ContainsKey(message)) { tracks.Add(new(from, row, row.Bounds, row.Bounds with { Y = row.Bounds.Y - removedBefore, Height = 0 }, false, true)); removedBefore += row.Bounds.Height; }
        int addedBefore = 0;
        foreach (var (message, row) in MessageRows)
        {
            bool arriving = !from.MessageRows.TryGetValue(message, out var old);
            tracks.Add(new(this, row, arriving ? row.Bounds with { Y = row.Bounds.Y - addedBefore, Height = 0 } : old.Bounds, row.Bounds, arriving, false));
            if (arriving) addedBefore += row.Bounds.Height;
        }
        return _messageTracks = tracks.ToArray();
    }
    private HeaderTrack[] HeaderTracksFrom(RibbonMotionScene from)
    {
        if (ReferenceEquals(_headerDeparture, from)) return _headerTracks;
        _headerDeparture = from; _headerTracks = [];
        if (!Headers.Any(h => !from.Headers.ContainsKey(h.Key) && Rectangle.Intersect(h.Value, TabViewport) is { Width: > 0, Height: > 0 })) return _headerTracks;
        // Header layout is already committed. Keep complete natural-size labels
        // in those slots; only incoming headers fade or travel vertically.
        return _headerTracks = Headers.Select(h => new HeaderTrack(h.Value, !from.Headers.ContainsKey(h.Key))).ToArray();
    }
    internal static void PrintClient(Control control, Bitmap image)
    {
        // WM_PRINT without PRF_CHILDREN uses the normal HWND paint context and
        // font metrics. Owned editors are composed separately with their clips.
        using var graphics = Graphics.FromImage(image); var dc = graphics.GetHdc();
        try { SendMessage(control.Handle, 0x317, dc, 0xC); }
        finally { graphics.ReleaseHdc(dc); }
    }
    [DllImport("user32.dll", EntryPoint = "SendMessageW")] private static extern nint SendMessage(nint window, uint message, nint parameter, nint flags);
    internal static Rectangle At(Rectangle rectangle, Rectangle bounds) => rectangle.IsEmpty ? Rectangle.Empty : new(rectangle.X + bounds.X, rectangle.Y + bounds.Y, rectangle.Width, rectangle.Height);
    internal static Rectangle Between(Rectangle from, Rectangle to, double progress)
    {
        int L(int a, int b) => (int)Math.Round(a + (b - a) * progress);
        return new(L(from.X, to.X), L(from.Y, to.Y), L(from.Width, to.Width), L(from.Height, to.Height));
    }
    internal static void Render(Graphics graphics, Rectangle canvas, RibbonControl ribbon, RibbonMotionScene from, RibbonMotionScene to, double progress, ImageAttributes alpha)
    {
        graphics.CompositingMode = CompositingMode.SourceCopy; graphics.Clear(Color.Transparent); graphics.CompositingMode = CompositingMode.SourceOver;
        double remaining = 1 - Math.Clamp(progress, 0, 1);
        double p = 1 - remaining * remaining * remaining;
        bool slideTab = ribbon.TabContentAnimation == RibbonTabContentAnimation.SlideDown && from.ActiveTab is not null && to.ActiveTab is not null && !ReferenceEquals(from.ActiveTab, to.ActiveTab);
        var headerTracks = to.HeaderTracksFrom(from);
        var messageTracks = to.MessageTracksFrom(from); bool messages = messageTracks.Length > 0;
        bool minimize = from.Minimized != to.Minimized;
        Rectangle Local(Rectangle screen) => new(screen.X - canvas.X, screen.Y - canvas.Y, screen.Width, screen.Height);
        Rectangle Interpolate(Rectangle old, Rectangle next) => Local(Between(At(old, from.Bounds), At(next, to.Bounds), p));
        var expandedBody = from.Minimized ? to.Body : from.Body;
        var body = minimize ? Interpolate(from.Body.IsEmpty ? expandedBody with { Height = 0 } : from.Body, to.Body.IsEmpty ? expandedBody with { Height = 0 } : to.Body) : Rectangle.Empty;
        var occupied = Local(Between(from.Bounds, to.Bounds, p));
        var messageFrame = messages ? Interpolate(from.Messages.IsEmpty ? to.Messages with { Height = 0 } : from.Messages, to.Messages.IsEmpty ? from.Messages with { Height = 0 } : to.Messages) : Rectangle.Empty;
        using (var background = new SolidBrush(ribbon.Theme.Background)) graphics.FillRectangle(background, occupied);
        var belowFrom = from.Placement == RibbonQuickAccessPosition.BelowRibbon && !from.Qat.IsEmpty;
        var belowTo = to.Placement == RibbonQuickAccessPosition.BelowRibbon && !to.Qat.IsEmpty;
        bool joined = minimize ? body.Height > 0 && (from.Joined || to.Joined) : to.Joined || from.Joined;
        bool connected = messages ? messageFrame.Height > 0 && (from.BottomConnected || to.BottomConnected) : minimize ? (from.BottomConnected || to.BottomConnected) && (body.Height > 0 || belowTo && ribbon.JoinBelowRibbonQuickAccess) : to.BottomConnected;
        var context = new RibbonPaintContext(graphics, occupied, ribbon.Theme, ribbon.Font, to.Dpi) { Density = ribbon.Density, IsBottomEdgeConnected = connected };
        if (connected)
        {
            var panel = messages ? messageFrame : Interpolate(from.Messages, to.Messages);
            var upper = minimize ? body : Interpolate(from.Frame, to.Frame);
            if (belowFrom || belowTo) { var qat = Interpolate(from.Qat, to.Qat); upper = upper.IsEmpty ? qat : Rectangle.Union(upper, qat); }
            if (!upper.IsEmpty && !panel.IsEmpty && ribbon.Painter.PaintConnectedMessageShadow(context, upper, panel)) context = context with { SuppressShadow = true };
        }
        if (messages && messageFrame.Width > 0 && messageFrame.Height > 0)
            ribbon.Painter.DrawMessageBar(context with { Bounds = messageFrame, IsTopEdgeConnected = connected, IsBottomEdgeConnected = false });
        if (minimize && !from.Messages.IsEmpty && !to.Messages.IsEmpty)
            ribbon.Painter.DrawMessageBar(context with { Bounds = Interpolate(from.Messages, to.Messages), IsTopEdgeConnected = connected, IsBottomEdgeConnected = false });
        if (minimize)
        {
            if (body.Width > 0 && body.Height > 0)
                ribbon.Painter.DrawBody(context with { Bounds = joined ? Rectangle.Union(body, Interpolate(from.Qat, to.Qat)) : body });
        }
        else if (!from.Frame.IsEmpty && !to.Frame.IsEmpty) ribbon.Painter.DrawBody(context with { Bounds = Interpolate(from.Frame, to.Frame) });
        else if (!to.Frame.IsEmpty) ribbon.Painter.DrawBody(context with { Bounds = Local(At(to.Frame, to.Bounds)) });
        if (belowFrom || belowTo)
        {
            var old = belowFrom ? At(from.Qat, from.Bounds) : new Rectangle(to.Bounds.Left + to.Qat.Left, from.Bounds.Top + (from.Frame.IsEmpty ? from.Header.Bottom : from.Frame.Bottom), to.Qat.Width, 0);
            var next = belowTo ? At(to.Qat, to.Bounds) : new Rectangle(from.Bounds.Left + from.Qat.Left, to.Bounds.Top + (to.Frame.IsEmpty ? to.Header.Bottom : to.Frame.Bottom), from.Qat.Width, 0);
            ribbon.Painter.DrawQuickAccessToolbar(context with { Bounds = Local(Between(old, next, p)), IsQuickAccessToolbarJoined = joined }, RibbonQuickAccessPosition.BelowRibbon);
        }
        // Keep the departure opaque beneath the arriving image. Fading both
        // with source-over changes the weight of identical overlapping glyphs.
        Draw(from, to, 1); Draw(to, from, p);
        // The caption stays on its own measured band during body/header motion.
        if (to.Header.Top > 0)
        {
            var caption = new Rectangle(0, 0, to.Image.Width, to.Header.Top);
            graphics.DrawImage(to.Image, Local(At(caption, to.Bounds)), caption, GraphicsUnit.Pixel);
        }
        if (messages && messageFrame.Width > 2 && messageFrame.Height > 2)
        {
            var interior = Rectangle.Inflate(messageFrame, -1, -1);
            foreach (var track in messageTracks)
            {
                var row = Interpolate(track.From, track.To); var clip = Rectangle.Intersect(interior, row);
                if (clip.Width <= 0 || clip.Height <= 0) continue;
                var crop = track.Row.Content; if (crop.Width <= 0 || crop.Height <= 0) continue;
                double opacity = track.Arriving ? p : track.Leaving ? 1 - p : 1; alpha.SetColorMatrix(new ColorMatrix { Matrix33 = (float)opacity }); var saved = graphics.Save();
                try
                {
                    graphics.SetClip(clip, CombineMode.Intersect);
                    if (row.Top > messageFrame.Top) { using var border = new SolidBrush(ribbon.Theme.MessageBorder); graphics.FillRectangle(border, interior.Left, row.Top, interior.Width, 1); }
                    var destination = new Rectangle(row.X + crop.X - track.Row.Bounds.X, row.Y + crop.Y - track.Row.Bounds.Y, crop.Width, crop.Height);
                    graphics.DrawImage(track.Scene.Image, destination, crop.X, crop.Y, crop.Width, crop.Height, GraphicsUnit.Pixel, alpha);
                }
                finally { graphics.Restore(saved); }
            }
        }
        if (slideTab && !from.Body.IsEmpty && !to.Body.IsEmpty)
        {
            int inset = RibbonLayoutEngine.Scale(4, to.Dpi); var crop = Rectangle.Inflate(to.Body, -inset, -inset);
            var clip = Interpolate(from.Body, to.Body); clip.Inflate(-inset, -inset); var position = clip;
            int distance = RibbonLayoutEngine.Scale(ribbon.AnimationLevel == RibbonAnimationLevel.Expressive ? ExpressiveTabSlide : SubtleTabSlide, to.Dpi);
            position.Y -= (int)Math.Round(distance * (1 - p)); var saved = graphics.Save();
            try
            {
                alpha.SetColorMatrix(new ColorMatrix { Matrix33 = 1 }); graphics.SetClip(clip, CombineMode.Intersect);
                graphics.DrawImage(to.Image, new Rectangle(position.Location, crop.Size), crop.X, crop.Y, crop.Width, crop.Height, GraphicsUnit.Pixel, alpha);
            }
            finally { graphics.Restore(saved); }
        }
        if (headerTracks.Length > 0)
        {
            var viewport = Interpolate(from.TabViewport, to.TabViewport);
            foreach (var track in headerTracks)
            {
                var slot = Local(At(track.Bounds, to.Bounds)); var clip = Rectangle.Intersect(slot, viewport);
                if (clip.Width <= 0 || clip.Height <= 0) continue;
                var position = slot.Location;
                if (track.Arriving && ribbon.TabContentAnimation == RibbonTabContentAnimation.SlideDown)
                    position.Y -= (int)Math.Round(RibbonLayoutEngine.Scale(ribbon.AnimationLevel == RibbonAnimationLevel.Expressive ? ExpressiveTabSlide : SubtleTabSlide, to.Dpi) * (1 - p));
                var crop = Rectangle.Intersect(new(Point.Empty, to.Image.Size), track.Bounds);
                if (crop.Width <= 0 || crop.Height <= 0) continue;
                position.Offset(crop.Left - track.Bounds.Left, crop.Top - track.Bounds.Top);
                alpha.SetColorMatrix(new ColorMatrix { Matrix33 = track.Arriving && ribbon.TabContentAnimation == RibbonTabContentAnimation.Fade ? (float)p : 1 });
                var saved = graphics.Save();
                try { graphics.SetClip(clip, CombineMode.Intersect); graphics.DrawImage(to.Image, new Rectangle(position, crop.Size), crop.X, crop.Y, crop.Width, crop.Height, GraphicsUnit.Pixel, alpha); }
                finally { graphics.Restore(saved); }
            }
        }
        if (to.ActiveTab is { } activeTab && !to.Indicator.IsEmpty)
        {
            var indicator = from.Indicator.IsEmpty ? Local(At(to.Indicator, to.Bounds)) : Interpolate(from.Indicator, to.Indicator);
            if (!from.Indicator.IsEmpty && ribbon.AnimationLevel == RibbonAnimationLevel.Expressive)
            {
                // Only the marker springs. Keep panel clips/opacity on their
                // bounded curve and prevent a shrinking pill from inverting.
                double spring = p + IndicatorSpring * remaining * Math.Sin(Math.PI * remaining);
                var sprung = Local(Between(At(from.Indicator, from.Bounds), At(to.Indicator, to.Bounds), spring));
                indicator.X = sprung.X;
                indicator.Width = Math.Clamp(sprung.Width, Math.Min(from.Indicator.Width, to.Indicator.Width), Math.Max(from.Indicator.Width, to.Indicator.Width));
            }
            var clip = from.IndicatorClip.IsEmpty ? Local(At(to.IndicatorClip, to.Bounds)) : Interpolate(from.IndicatorClip, to.IndicatorClip);
            var saved = graphics.Save();
            try { graphics.SetClip(clip, CombineMode.Intersect); ribbon.Painter.DrawTabIndicator(context with { Bounds = indicator }, activeTab, true); }
            finally { graphics.Restore(saved); }
        }
        void Draw(RibbonMotionScene scene, RibbonMotionScene other, double opacity)
        {
            if (opacity <= 0 && !(minimize && ReferenceEquals(scene, to))) return;
            var matrix = new ColorMatrix { Matrix33 = (float)opacity }; alpha.SetColorMatrix(matrix);
            var origin = Between(from.Bounds, to.Bounds, p).Location;
            void Segment(Rectangle crop, Rectangle destination, Rectangle? clip = null, Rectangle? exclude = null, bool headerBackdrop = false)
            {
                crop = Rectangle.Intersect(new(Point.Empty, scene.Image.Size), crop); if (crop.IsEmpty) return;
                var saved = graphics.Save();
                try
                {
                    if (clip is { } clipping) graphics.SetClip(clipping, CombineMode.Intersect);
                    if (exclude is { } excluded) graphics.ExcludeClip(excluded);
                    if (headerBackdrop && headerTracks.Length > 0)
                        foreach (var tab in scene.Headers.Values) graphics.ExcludeClip(new Rectangle(origin.X + tab.Left - canvas.X, origin.Y + tab.Top - canvas.Y, tab.Width, tab.Height));
                    graphics.DrawImage(scene.Image, new Rectangle(destination.Location, crop.Size), crop.X, crop.Y, crop.Width, crop.Height, GraphicsUnit.Pixel, alpha);
                }
                finally { graphics.Restore(saved); }
            }
            var header = Interpolate(from.Header, to.Header);
            if (!minimize || ReferenceEquals(scene, to))
            {
                // The utility chevron reflects committed state immediately;
                // crossfading up/down glyphs would briefly produce an X.
                if (minimize) alpha.SetColorMatrix(new ColorMatrix { Matrix33 = 1 });
                Segment(scene.Header, new(origin.X - canvas.X, origin.Y - canvas.Y, scene.Header.Width, scene.Header.Height), header,
                    scene.Placement == RibbonQuickAccessPosition.TabRow ? new Rectangle(origin.X + scene.Qat.X - canvas.X, origin.Y + scene.Qat.Y - canvas.Y, scene.Qat.Width, scene.Qat.Height) : null, headerBackdrop: true);
                if (minimize) alpha.SetColorMatrix(matrix);
            }
            if (minimize && !scene.Body.IsEmpty)
            {
                int inset = RibbonLayoutEngine.Scale(4, scene.Dpi); var crop = Rectangle.Inflate(scene.Body, -inset, -inset); var clip = Rectangle.Inflate(body, -inset, -inset);
                if (crop.Width > 0 && crop.Height > 0 && clip.Width > 0 && clip.Height > 0)
                {
                    // Reveal/retract the complete body drawing through a moving
                    // edge. Native text and icons stay opaque and natural size.
                    alpha.SetColorMatrix(new ColorMatrix { Matrix33 = 1 }); Segment(crop, new(clip.Location, crop.Size), clip); alpha.SetColorMatrix(matrix);
                }
            }
            else if (!slideTab && !scene.Body.IsEmpty && !other.Body.IsEmpty)
            {
                int inset = RibbonLayoutEngine.Scale(4, scene.Dpi); var crop = Rectangle.Inflate(scene.Body, -inset, -inset);
                var position = Interpolate(from.Body, to.Body); position.Inflate(-inset, -inset); Segment(crop, position, position);
            }
            if (!messages && !scene.Messages.IsEmpty && !other.Messages.IsEmpty) Segment(scene.Messages, Interpolate(from.Messages, to.Messages));
            if (!scene.Strip.IsEmpty && !other.Strip.IsEmpty) { var strip = Interpolate(from.Strip, to.Strip); Segment(scene.Strip, strip, strip); }
        }
    }
}
