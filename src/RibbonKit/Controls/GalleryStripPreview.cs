using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace RibbonKit.Controls;

// A temporary view while a gallery strip or group's native content is borrowed.
// No controls, items, bindings or event handlers are cloned or reparented here.
internal static class GalleryStripPreview
{
    private static readonly DependencyProperty LivePreviewProperty = DependencyProperty.RegisterAttached(
        "LivePreview", typeof(LivePreview), typeof(GalleryStripPreview), new PropertyMetadata(null));

    internal static Image? Create(FrameworkElement viewport, bool live = false)
    {
        // Copy the row that is already painted. Flushing layout here can process
        // a pending focus/scroll request before the picture has been frozen.
        Size size = viewport.RenderSize;
        if (size.Width <= 0 || size.Height <= 0) return null;
        var state = live ? new LivePreview(viewport) : null;
        var picture = Capture(viewport, state);
        // Transparent bounds retain the full viewport size, including blank space,
        // so DrawingImage's content bounds cannot crop or stretch the tile row.
        picture.Children.Insert(0, new GeometryDrawing(Brushes.Transparent, null,
            new RectangleGeometry(new Rect(size))));
        picture.ClipGeometry = new RectangleGeometry(new Rect(size));
        if (picture.CanFreeze) picture.Freeze();
        var preview = new Image
        {
            Source = new DrawingImage(picture), Width = size.Width, Height = size.Height,
            Stretch = Stretch.Fill, HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top, IsHitTestVisible = false,
            FlowDirection = viewport.FlowDirection,
            SnapsToDevicePixels = true, UseLayoutRounding = viewport.UseLayoutRounding,
        };
        if (state != null)
        {
            preview.SetValue(LivePreviewProperty, state);
            state.Attach();
        }
        return preview;
    }

    // Live group previews retain each control's original slot while its native
    // paint follows selection, text, enabled state, bindings and resources.
    // Gallery strips continue using the frozen path to preserve their viewed row.
    private static DrawingGroup Capture(Visual visual, LivePreview? live = null)
    {
        var picture = new DrawingGroup();
        if (visual is UIElement { Visibility: not Visibility.Visible }) return picture;
        if (live != null && visual is FrameworkElement element
            && (element is Control or TextBlock || VisualTreeHelper.GetChildrenCount(visual) == 0)
            && element.RenderSize.Width > 0 && element.RenderSize.Height > 0)
        {
            // Content bounds can contain only the glyph ink, much smaller than
            // its layout slot. Use the complete layout box to retain alignment.
            var slot = new GeometryDrawing(null, null, new RectangleGeometry(new Rect(element.RenderSize)));
            live.Add(element, slot);
            picture.Children.Add(slot);
            return picture;
        }
        var xGuidelines = VisualTreeHelper.GetXSnappingGuidelines(visual);
        var yGuidelines = VisualTreeHelper.GetYSnappingGuidelines(visual);
        if (xGuidelines is { Count: > 0 } || yGuidelines is { Count: > 0 })
            picture.GuidelineSet = new GuidelineSet(xGuidelines?.ToArray() ?? Array.Empty<double>(), yGuidelines?.ToArray() ?? Array.Empty<double>());
        if (VisualTreeHelper.GetDrawing(visual) is { } drawing)
            picture.Children.Add(drawing.CloneCurrentValue());
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(visual); i++)
        {
            if (VisualTreeHelper.GetChild(visual, i) is not Visual child) continue;
            var branch = Capture(child, live);
            var transform = new TransformGroup();
            if (VisualTreeHelper.GetTransform(child) is { } childTransform)
                transform.Children.Add(childTransform.CloneCurrentValue());
            Vector offset = VisualTreeHelper.GetOffset(child);
            transform.Children.Add(new TranslateTransform(offset.X, offset.Y));
            branch.Transform = transform;
            branch.Opacity = VisualTreeHelper.GetOpacity(child);
            if (VisualTreeHelper.GetOpacityMask(child) is { } mask)
                branch.OpacityMask = mask.CloneCurrentValue();
            if (VisualTreeHelper.GetClip(child) is { } clip)
                branch.ClipGeometry = clip.CloneCurrentValue();
            picture.Children.Add(branch);
        }
        return picture;
    }

    internal static void DetachLive(Image? preview)
    {
        if (preview?.GetValue(LivePreviewProperty) is LivePreview live)
        {
            live.Detach();
            preview.ClearValue(LivePreviewProperty);
        }
        if (preview?.Source is DrawingImage { Drawing: { } drawing }) Detach(drawing);
        static void Detach(Drawing drawing)
        {
            if (drawing is GeometryDrawing { Brush: VisualBrush brush })
            {
                brush.Visual = null;
            }
            else if (drawing is DrawingGroup group)
                foreach (Drawing child in group.Children) Detach(child);
        }
    }

    private sealed class LivePreview(FrameworkElement viewport)
    {
        private readonly List<(FrameworkElement Source, GeometryDrawing Drawing)> _slots = new();

        internal void Add(FrameworkElement source, GeometryDrawing drawing)
        {
            _slots.Add((source, drawing));
            drawing.Brush = CreateBrush(source, LayoutBox(source));
        }

        internal void Attach() => viewport.LayoutUpdated += OnLayoutUpdated;

        private void OnLayoutUpdated(object? sender, EventArgs e)
        {
            // Reparenting can move a control without changing ActualWidth/Height.
            // Follow its full source box while keeping the captured destination slot.
            foreach (var slot in _slots)
            {
                Rect bounds = LayoutBox(slot.Source);
                var brush = (VisualBrush)slot.Drawing.Brush;
                if (brush.Viewbox != bounds)
                {
                    slot.Drawing.Brush = CreateBrush(slot.Source, bounds);
                    brush.Visual = null;
                }
            }
        }

        internal void Detach()
        {
            viewport.LayoutUpdated -= OnLayoutUpdated;
            foreach (var slot in _slots) ((VisualBrush)slot.Drawing.Brush).Visual = null;
            _slots.Clear();
        }

        private static Rect LayoutBox(FrameworkElement visual)
        {
            Rect box = new(visual.RenderSize);
            if (VisualTreeHelper.GetTransform(visual) is { } transform) box = transform.TransformBounds(box);
            box.Offset(VisualTreeHelper.GetOffset(visual));
            return box;
        }

        private static VisualBrush CreateBrush(FrameworkElement source, Rect bounds) => new(source)
        {
            AutoLayoutContent = false, Stretch = Stretch.Fill,
            AlignmentX = AlignmentX.Left, AlignmentY = AlignmentY.Top,
            ViewboxUnits = BrushMappingMode.Absolute, Viewbox = bounds,
        };

    }
}
