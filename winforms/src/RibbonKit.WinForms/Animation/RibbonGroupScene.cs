using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

// Committed body endpoints. Commands never scale, and the live editors are neither
// cloned nor moved by motion. A key includes the run for interleaved Simplified pins.
internal sealed class RibbonGroupScene : IDisposable
{
    internal readonly record struct Key(RibbonGroup Group, int Run);
    internal readonly record struct Slot(Rectangle Bounds, bool Collapsed, int Commands);
    internal readonly Bitmap Image;
    internal readonly Rectangle Viewport;
    internal readonly Point Origin;
    internal readonly int Dpi;
    internal readonly Dictionary<Key, Slot> Groups;
    private readonly Rectangle[] _fixedTargets;
    internal RibbonGroupScene(RibbonControl ribbon, Control surface, RibbonLayoutSnapshot layout)
    {
        Dpi = ribbon.DeviceDpi; Origin = surface.PointToScreen(Point.Empty);
        var viewport = Rectangle.Intersect(surface.ClientRectangle, layout.BodyViewport);
        for (var parent = surface.Parent; parent is not null; parent = parent.Parent)
            viewport = Rectangle.Intersect(viewport, surface.RectangleToClient(parent.RectangleToScreen(parent.ClientRectangle)));
        viewport.Inflate(0, -RibbonLayoutEngine.Scale(2, Dpi)); Viewport = viewport;
        Groups = Measure(ribbon, layout);
        _fixedTargets = layout.Targets.Where(t => t.Kind is TargetKind.QuickAccessOverflow or TargetKind.BodyBack or TargetKind.BodyNext).Select(t => t.VisibleBounds).ToArray();
        Image = new(surface.Width, surface.Height, PixelFormat.Format24bppRgb);
        try { RibbonMotionScene.Capture(ribbon, surface, Image); }
        catch { Image.Dispose(); throw; }
    }
    internal static Dictionary<Key, Slot> Measure(RibbonControl ribbon, RibbonLayoutSnapshot layout)
    {
        if (ribbon.LayoutMode == RibbonLayoutMode.Classic)
            return layout.Groups.ToDictionary(g => new Key(g.Group, 0), g => new Slot(g.Frame, g.Collapsed, g.Group.Items.Count));
        var result = new Dictionary<Key, Slot>(); var runs = new Dictionary<RibbonGroup, int>();
        Key? last = null;
        foreach (var target in layout.Targets.Where(t => t.Kind is TargetKind.Item or TargetKind.Group))
        {
            var element = target.Element; while (element is not null && element is not RibbonGroup) element = element.Parent as RibbonElement;
            if (element is not RibbonGroup group) continue;
            if (last is not { } key || !ReferenceEquals(key.Group, group))
            { int run = runs.GetValueOrDefault(group); runs[group] = run + 1; last = new Key(group, run); }
            var id = last.Value;
            var bounds = result.TryGetValue(id, out var old) ? Rectangle.Union(old.Bounds, target.Bounds) : target.Bounds;
            result[id] = new(bounds, target.Kind == TargetKind.Group, old.Commands + (target.Kind == TargetKind.Item ? 1 : 0));
        }
        // Include each run's following divider/gap in its natural-size drawing.
        var ordered = result.Keys.ToArray();
        for (int i = 0; i + 1 < ordered.Length; i++)
        { var slot = result[ordered[i]]; result[ordered[i]] = slot with { Bounds = slot.Bounds with { Width = result[ordered[i + 1]].Bounds.Left - slot.Bounds.Left } }; }
        return result;
    }
    internal static bool Changes(IReadOnlyDictionary<Key, Slot> from, IReadOnlyDictionary<Key, Slot> to)
        => from.Keys.Union(to.Keys).Any(key => !from.TryGetValue(key, out var old) || !to.TryGetValue(key, out var next) || old.Collapsed != next.Collapsed || old.Commands != next.Commands);
    internal static int Direction(Slot from, Slot to) => to.Collapsed != from.Collapsed ? to.Collapsed ? -1 : 1 : to.Commands < from.Commands ? -1 : to.Commands > from.Commands ? 1 : 0;
    internal static void Draw(Graphics graphics, RibbonGroupScene from, RibbonGroupScene to, double progress, int travel, Color surface, bool slide = true, ImageAttributes? alpha = null)
    {
        using var ownedAlpha = alpha is null ? new ImageAttributes() : null; alpha ??= ownedAlpha!;
        var fade = new ColorMatrix();
        double p = 1 - Math.Pow(1 - Math.Clamp(progress, 0, 1), 3);
        graphics.CompositingMode = CompositingMode.SourceCopy; graphics.Clear(Color.Transparent); graphics.CompositingMode = CompositingMode.SourceOver;
        // Only the body viewport is covered. Headers, frame, QAT, messages and
        // popup windows keep their live drawings and their own motion.
        graphics.TranslateTransform(-to.Viewport.Left, -to.Viewport.Top);
        using var background = new SolidBrush(surface); graphics.FillRectangle(background, to.Viewport);
        var saved = graphics.Save();
        try
        {
            graphics.SetClip(to.Viewport, CombineMode.Intersect);
            // More Commands and body scrollers remain at committed positions.
            var backdrop = graphics.Save();
            foreach (var slot in to.Groups.Values) graphics.ExcludeClip(slot.Bounds);
            graphics.DrawImageUnscaled(to.Image, Point.Empty); graphics.Restore(backdrop);
            foreach (var target in to._fixedTargets) graphics.ExcludeClip(target);
            foreach (var key in from.Groups.Keys.Union(to.Groups.Keys))
            {
                bool had = from.Groups.TryGetValue(key, out var old), has = to.Groups.TryGetValue(key, out var next);
                if (!had) old = new(next.Bounds with { Width = 0 }, false, 0);
                if (!has) next = new(old.Bounds with { Width = 0 }, true, 0);
                var slot = RibbonMotionScene.Between(old.Bounds, next.Bounds, p);
                if (slot.Width <= 0 || slot.Height <= 0) continue;
                int direction = Direction(old, next);
                if (direction == 0) { if (has) Draw(to, next.Bounds, slot.Location, slot); continue; }
                if (!slide)
                {
                    if (had) Draw(from, old.Bounds, slot.Location, slot, has ? 1 : 1 - p);
                    if (has) Draw(to, next.Bounds, slot.Location, slot, p);
                    continue;
                }
                // Present the committed group once. Mixing sliced old/new
                // captions briefly creates fragments of two different labels.
                // Its natural-size arrival travels in the requested direction;
                // the allocated width and neighbouring positions settle with it.
                if (has) Draw(to, next.Bounds, new(slot.Left - direction * (int)Math.Round(travel * (1 - p)), slot.Top), slot);
                else if (had) Draw(from, old.Bounds, new(slot.Left + direction * (int)Math.Round(travel * p), slot.Top), slot);
            }
        }
        finally { graphics.Restore(saved); graphics.ResetTransform(); }
        void Draw(RibbonGroupScene scene, Rectangle crop, Point position, Rectangle clip, double opacity = 1)
        {
            if (clip.Width <= 0 || clip.Height <= 0 || opacity <= 0) return;
            var bounded = Rectangle.Intersect(new(Point.Empty, scene.Image.Size), crop);
            if (bounded.Width <= 0 || bounded.Height <= 0) return;
            position.Offset(bounded.Left - crop.Left, bounded.Top - crop.Top); var state = graphics.Save();
            try
            {
                graphics.SetClip(clip, CombineMode.Intersect);
                fade.Matrix33 = (float)opacity; alpha.SetColorMatrix(fade);
                graphics.DrawImage(scene.Image, new Rectangle(position, bounded.Size), bounded.X, bounded.Y, bounded.Width, bounded.Height, GraphicsUnit.Pixel, alpha);
            }
            finally { graphics.Restore(state); }
        }
    }
    public void Dispose() => Image.Dispose();
}
