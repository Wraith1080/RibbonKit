using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using RibbonKit.Controls;

namespace RibbonKit.Showcase;

/// <summary>Softens the document edge and optionally lets it scroll beneath the QAT.</summary>
internal sealed class CrystalDocumentEdgeFade
{
    private readonly Ribbon _ribbon;
    private ScrollViewer? _viewer;
    private readonly RichTextBox? _editor;
    private readonly FrameworkElement _surface;
    private readonly Thickness _originalMargin;
    private ScrollContentPresenter? _presenter;
    private ScrollBar? _verticalScrollBar;
    private Brush? _mask;
    private double _maskHeight;
    private double _maskFadeStart;
    private bool _crystal;
    private bool _requested = true;
    private bool _qatUnderlayRequested = true;

    public CrystalDocumentEdgeFade(Ribbon ribbon, RichTextBox editor, FrameworkElement surface)
    {
        _ribbon = ribbon;
        _editor = editor;
        _surface = surface;
        _originalMargin = surface.Margin;
        _ribbon.LayoutUpdated += (_, _) => Update();
        editor.Loaded += (_, _) => Update();
    }

    public void Apply(bool crystal, bool requested, bool qatUnderlayRequested)
    {
        _crystal = crystal;
        _requested = requested;
        _qatUnderlayRequested = qatUnderlayRequested;
        Update();
    }

    private void Update()
    {
        if (_editor != null)
        {
            _editor.ApplyTemplate();
            var viewer = _editor.Template?.FindName("PART_ContentHost", _editor) as ScrollViewer;
            if (!ReferenceEquals(_viewer, viewer))
            {
                if (_viewer != null) _viewer.ScrollChanged -= OnScrollChanged;
                _viewer = viewer;
                if (viewer != null) viewer.ScrollChanged += OnScrollChanged;
            }
        }
        if (_viewer == null) return;
        _viewer.ApplyTemplate();
        var (overlap, fadeStart) = GetQatOverlap();
        if (overlap > 0)
        {
            var margin = new Thickness(_originalMargin.Left, _originalMargin.Top - overlap,
                _originalMargin.Right, _originalMargin.Bottom);
            if (_surface.Margin != margin) _surface.SetCurrentValue(FrameworkElement.MarginProperty, margin);
        }
        else if (_surface.Margin != _originalMargin)
            _surface.SetCurrentValue(FrameworkElement.MarginProperty, _originalMargin);

        var scrollBar = _viewer.Template?.FindName("PART_VerticalScrollBar", _viewer) as ScrollBar;
        if (!ReferenceEquals(_verticalScrollBar, scrollBar))
        {
            _verticalScrollBar?.ClearValue(FrameworkElement.MarginProperty);
            _verticalScrollBar = scrollBar;
        }
        if (_verticalScrollBar != null)
        {
            var margin = new Thickness(0, overlap, 0, 0);
            if (overlap > 0 && _verticalScrollBar.Margin != margin)
                _verticalScrollBar.Margin = margin;
            else if (overlap == 0 &&
                     _verticalScrollBar.ReadLocalValue(FrameworkElement.MarginProperty) != DependencyProperty.UnsetValue)
                _verticalScrollBar.ClearValue(FrameworkElement.MarginProperty);
        }

        var presenter = _viewer.Template?.FindName("PART_ScrollContentPresenter", _viewer)
            as ScrollContentPresenter;
        if (!ReferenceEquals(_presenter, presenter))
        {
            _presenter?.ClearValue(UIElement.OpacityMaskProperty);
            _presenter = presenter;
        }
        if (_presenter == null) return;
        if (_crystal && _requested && (_viewer.VerticalOffset > 0.5 || fadeStart > 0))
        {
            double fadeHeight = fadeStart + 24;
            if (_mask == null || Math.Abs(_maskHeight - fadeHeight) > 0.5 ||
                Math.Abs(_maskFadeStart - fadeStart) > 0.5)
            {
                var mask = new LinearGradientBrush
                {
                    MappingMode = BrushMappingMode.Absolute,
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(0, fadeHeight),
                    SpreadMethod = GradientSpreadMethod.Pad,
                };
                // Keep the document faintly visible through the drawer, then finish
                // the fade in the first 24 DIP beneath its lower edge.
                var underlayOpacity = fadeStart > 0 ? (byte)64 : (byte)0;
                var underlayColor = Color.FromArgb(underlayOpacity, 255, 255, 255);
                mask.GradientStops.Add(new GradientStop(underlayColor, 0));
                if (fadeStart > 0)
                    mask.GradientStops.Add(new GradientStop(underlayColor, fadeStart / fadeHeight));
                mask.GradientStops.Add(new GradientStop(Colors.White, 1));
                mask.Freeze();
                _mask = mask;
                _maskHeight = fadeHeight;
                _maskFadeStart = fadeStart;
            }
            _presenter.SetCurrentValue(UIElement.OpacityMaskProperty, _mask);
        }
        else
            _presenter.ClearValue(UIElement.OpacityMaskProperty);
    }

    private void OnScrollChanged(object sender, ScrollChangedEventArgs e) => Update();

    private (double Overlap, double FadeStart) GetQatOverlap()
    {
        if (!_crystal || !_qatUnderlayRequested || !_ribbon.IsLoaded ||
            _ribbon.QuickAccessPosition != RibbonQuickAccessPosition.BelowRibbon ||
            _ribbon.HasOpenMessages) return (0, 0);

        _ribbon.ApplyTemplate();
        if (_ribbon.Template?.FindName("QatBelowHost", _ribbon) is not Border
            { Visibility: Visibility.Visible, ActualHeight: > 0 } qat) return (0, 0);

        double top = qat.TranslatePoint(new Point(), _ribbon).Y;
        double overlap = _ribbon.ActualHeight - top;
        return double.IsFinite(overlap) && overlap >= qat.ActualHeight
            ? (overlap, qat.ActualHeight) : (0, 0);
    }
}
