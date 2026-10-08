using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace RibbonKit.Controls;

// A temporary view of the strip while the gallery's native presenter is borrowed.
// No controls, items, bindings or event handlers are cloned or reparented here.
internal static class GalleryStripPreview
{
    internal static Image? Create(ScrollViewer viewport)
    {
        viewport.UpdateLayout();
        Size size = viewport.RenderSize;
        if (size.Width <= 0 || size.Height <= 0) return null;
        var picture = Capture(viewport);
        // Transparent bounds retain the full viewport size, including blank space,
        // so DrawingImage's content bounds cannot crop or stretch the tile row.
        picture.Children.Insert(0, new GeometryDrawing(Brushes.Transparent, null,
            new RectangleGeometry(new Rect(size))));
        picture.ClipGeometry = new RectangleGeometry(new Rect(size));
        if (picture.CanFreeze) picture.Freeze();
        return new Image
        {
            Source = new DrawingImage(picture), Width = size.Width, Height = size.Height,
            Stretch = Stretch.Fill, HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top, IsHitTestVisible = false,
            FlowDirection = viewport.FlowDirection,
            SnapsToDevicePixels = true, UseLayoutRounding = viewport.UseLayoutRounding,
        };
    }

    private static DrawingGroup Capture(Visual visual)
    {
        var picture = new DrawingGroup();
        if (visual is UIElement { Visibility: not Visibility.Visible }) return picture;
        var xGuidelines = VisualTreeHelper.GetXSnappingGuidelines(visual);
        var yGuidelines = VisualTreeHelper.GetYSnappingGuidelines(visual);
        if (xGuidelines is { Count: > 0 } || yGuidelines is { Count: > 0 })
            picture.GuidelineSet = new GuidelineSet(xGuidelines?.ToArray() ?? Array.Empty<double>(), yGuidelines?.ToArray() ?? Array.Empty<double>());
        if (VisualTreeHelper.GetDrawing(visual) is { } drawing)
            picture.Children.Add(drawing.CloneCurrentValue());
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(visual); i++)
        {
            if (VisualTreeHelper.GetChild(visual, i) is not Visual child) continue;
            var branch = Capture(child);
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
}
