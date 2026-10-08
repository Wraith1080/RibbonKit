using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using RibbonKit.Controls;

namespace RibbonKit.Layout;

// The collapsed touch strip has one full-height row of equally sized cells. The
// same presenter returns to ordinary WrapPanel layout in the expanded viewport.
internal sealed class InRibbonGalleryPanel : WrapPanel
{
    private ScrollViewer? _viewport;
    private readonly List<UIElement> _visibleChildren = new();
    private double _cellWidth;
    private double _cellHeight;
    private int _columns;

    public InRibbonGalleryPanel()
    {
        Loaded += (_, _) => AttachViewport();
        Unloaded += (_, _) => DetachViewport();
    }

    protected override Size MeasureOverride(Size constraint)
    {
        AttachViewport();
        var gallery = ItemsControl.GetItemsOwner(this) as InRibbonGallery;
        double height = _viewport?.ViewportHeight ?? 0;
        double width = constraint.Width;
        _visibleChildren.Clear();
        if (gallery is null || Ribbon.GetDensity(gallery) != RibbonDensity.Touch
            || gallery.IsDropDownOpen || Orientation != Orientation.Horizontal
            || !double.IsNaN(ItemWidth) || !double.IsNaN(ItemHeight)
            || !double.IsFinite(width) || width <= 0 || height <= 0
            || InternalChildren.Count == 0)
        {
            _columns = 0;
            return base.MeasureOverride(constraint);
        }

        double naturalWidth = 0;
        foreach (UIElement child in InternalChildren)
        {
            child.Measure(new Size(double.PositiveInfinity, height));
            if (child.Visibility == Visibility.Collapsed) continue;
            _visibleChildren.Add(child);
            naturalWidth = Math.Max(naturalWidth, child.DesiredSize.Width);
        }
        if (_visibleChildren.Count == 0)
        {
            _columns = 0;
            return base.MeasureOverride(constraint);
        }

        int columns = Math.Min(_visibleChildren.Count,
            Math.Max(1, (int)Math.Floor(width / Math.Max(1, naturalWidth))));
        bool geometryChanged = _columns != columns || _cellWidth != width / columns || _cellHeight != height;
        _columns = columns;
        _cellWidth = width / _columns;
        _cellHeight = height;
        foreach (UIElement child in _visibleChildren)
            child.Measure(new Size(_cellWidth, _cellHeight));
        if (geometryChanged) gallery.RefreshStripGeometry();
        int rows = (_visibleChildren.Count + _columns - 1) / _columns;
        return new Size(width, rows * _cellHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (_columns == 0)
            return base.ArrangeOverride(finalSize);

        foreach (UIElement child in InternalChildren)
            if (child.Visibility == Visibility.Collapsed) child.Arrange(new Rect());
        for (int i = 0; i < _visibleChildren.Count; i++)
            _visibleChildren[i].Arrange(new Rect((i % _columns) * _cellWidth,
                (i / _columns) * _cellHeight, _cellWidth, _cellHeight));
        return finalSize;
    }

    private void AttachViewport()
    {
        ScrollViewer? viewport = null;
        for (DependencyObject? current = VisualTreeHelper.GetParent(this);
             current is not null; current = VisualTreeHelper.GetParent(current))
        {
            if (current is ScrollViewer scroller)
            {
                viewport = scroller;
                break;
            }
        }
        if (ReferenceEquals(viewport, _viewport)) return;
        DetachViewport();
        _viewport = viewport;
        if (_viewport is not null) _viewport.ScrollChanged += OnViewportChanged;
    }

    private void DetachViewport()
    {
        if (_viewport is not null) _viewport.ScrollChanged -= OnViewportChanged;
        _viewport = null;
    }

    private void OnViewportChanged(object sender, ScrollChangedEventArgs e)
    {
        if (e.ViewportHeightChange != 0 || e.ViewportWidthChange != 0)
            InvalidateMeasure();
    }
}
