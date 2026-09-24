using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace RibbonKit.Showcase;

/// <summary>Softens the document viewport's top edge as content scrolls beneath it.</summary>
internal sealed class CrystalDocumentEdgeFade
{
    private readonly ScrollViewer _viewer;
    private readonly Brush _mask;
    private ScrollContentPresenter? _presenter;
    private bool _crystal;
    private bool _requested = true;

    public CrystalDocumentEdgeFade(ScrollViewer viewer)
    {
        _viewer = viewer;
        var mask = new LinearGradientBrush
        {
            MappingMode = BrushMappingMode.Absolute,
            StartPoint = new Point(0, 0),
            EndPoint = new Point(0, 24),
            SpreadMethod = GradientSpreadMethod.Pad,
        };
        mask.GradientStops.Add(new GradientStop(Color.FromArgb(0, 255, 255, 255), 0));
        mask.GradientStops.Add(new GradientStop(Colors.White, 1));
        mask.Freeze();
        _mask = mask;
        _viewer.Loaded += (_, _) => Update();
        _viewer.ScrollChanged += (_, _) => Update();
    }

    public void Apply(bool crystal, bool requested)
    {
        _crystal = crystal;
        _requested = requested;
        Update();
    }

    private void Update()
    {
        _viewer.ApplyTemplate();
        var presenter = _viewer.Template?.FindName("PART_ScrollContentPresenter", _viewer)
            as ScrollContentPresenter;
        if (!ReferenceEquals(_presenter, presenter))
        {
            _presenter?.ClearValue(UIElement.OpacityMaskProperty);
            _presenter = presenter;
        }
        if (_presenter == null) return;
        if (_crystal && _requested && _viewer.VerticalOffset > 0.5)
            _presenter.SetCurrentValue(UIElement.OpacityMaskProperty, _mask);
        else
            _presenter.ClearValue(UIElement.OpacityMaskProperty);
    }
}
