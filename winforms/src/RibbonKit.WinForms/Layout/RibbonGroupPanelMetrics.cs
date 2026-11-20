namespace RibbonKit.WinForms.Layout;

// Decoration is derived from existing snapshots, never fed back into measurement.
internal readonly record struct RibbonGroupPanelMetrics(Rectangle Panel, Rectangle Interior, Rectangle Caption, int Radius, int Stroke)
{
    internal static Rectangle CaptionBounds(Rectangle bounds, Font font, int dpi)
    {
        int height = Math.Min(bounds.Height, Math.Max(font.Height + RibbonLayoutEngine.Scale(6, dpi), RibbonLayoutEngine.Scale(20, dpi)));
        return new(bounds.Left, bounds.Bottom - height, bounds.Width, height);
    }
    internal static RibbonGroupPanelMetrics Resolve(RibbonPaintContext context, bool collapsed)
    {
        var settings = context.Theme.BodyAppearance?.GroupPanel;
        var frame = !context.GroupFrameBounds.IsEmpty ? context.GroupFrameBounds : !context.GroupSeparatorBounds.IsEmpty ? context.GroupSeparatorBounds : context.Bounds;
        int limit = Math.Max(0, (Math.Min(frame.Width, frame.Height) - 1) / 2);
        int inset = Math.Clamp(RibbonLayoutEngine.Scale(settings?.Inset ?? 2, context.DeviceDpi), 0, limit);
        var panel = Rectangle.Inflate(frame, -inset, -inset);
        int stroke = Math.Clamp(RibbonLayoutEngine.Scale(settings?.BorderWidth ?? 1, context.DeviceDpi), 0, Math.Max(0, Math.Min(panel.Width, panel.Height) / 2));
        var interior = Rectangle.Inflate(panel, -stroke, -stroke);
        var caption = collapsed || context.IsSimplifiedGroup ? Rectangle.Empty : Rectangle.Intersect(interior,
            context.GroupCaptionBounds.IsEmpty ? CaptionBounds(context.Bounds, context.Font, context.DeviceDpi) : context.GroupCaptionBounds);
        int radius = Math.Clamp(RibbonLayoutEngine.Scale(settings?.CornerRadius ?? 3, context.DeviceDpi), 0, Math.Max(0, Math.Min(panel.Width, panel.Height) / 2));
        return new(panel, interior, caption, radius, stroke);
    }
    internal static IEnumerable<(RibbonGroup Group, Rectangle Bounds)> CompactFrames(RibbonLayoutSnapshot snapshot)
    {
        RibbonGroup? current = null; Rectangle bounds = Rectangle.Empty;
        foreach (var target in snapshot.Targets.Where(t => t.Kind == TargetKind.Item && !t.DropDownPart))
        {
            if (target.Element is not RibbonItem { Parent: RibbonGroup group }) continue;
            if (!ReferenceEquals(group, current))
            {
                if (current is not null) yield return (current, bounds);
                current = group; bounds = target.Bounds;
            }
            else bounds = Rectangle.Union(bounds, target.Bounds);
        }
        if (current is not null) yield return (current, bounds);
    }
}
