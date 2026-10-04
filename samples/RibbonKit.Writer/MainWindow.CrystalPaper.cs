using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using RibbonKit.Controls;
using RibbonKit.Theming;
using RibbonKit.Writer.View;

namespace RibbonKit.Writer;

public partial class MainWindow
{
    private Thickness _paperHostMargin;
    private ScrollContentPresenter? _paperFadePresenter;
    private ScrollBar? _paperScrollBar;
    private LinearGradientBrush? _paperEdgeMask;
    private double _paperUnderlayHeight;
    private bool _trackingPaperUnderlay;

    private void InitializeCrystalPaperPresentation()
    {
        _paperHostMargin = DocumentPresentationHost.Margin;
        MainRibbon.Loaded += OnCrystalPaperLoaded;
        MainRibbon.SizeChanged += OnCrystalPaperSizeChanged;
    }

    private void OnCrystalPaperLoaded(object sender, RoutedEventArgs e) => UpdateCrystalPaperPresentation();

    private void OnCrystalPaperSizeChanged(object sender, SizeChangedEventArgs e) => UpdateCrystalPaperPresentation();

    private void OnCrystalPaperLayoutUpdated(object? sender, EventArgs e) =>
        UpdateCrystalPaperPresentation();

    private void UpdateCrystalPaperPresentation()
    {
        if (DocumentPresentationHost is null) return;
        double overlap = 0;
        bool requested = _appearancePreferences.Theme == RibbonTheme.CrystalLight
            && CurrentViewMode == WriterViewMode.Paper && !_rulerVisible
            && MainRibbon.QuickAccessPosition == RibbonQuickAccessPosition.BelowRibbon
            && !SystemParameters.HighContrast && MainRibbon.IsLoaded;
        if (requested != _trackingPaperUnderlay)
        {
            _trackingPaperUnderlay = requested;
            if (requested) MainRibbon.LayoutUpdated += OnCrystalPaperLayoutUpdated;
            else MainRibbon.LayoutUpdated -= OnCrystalPaperLayoutUpdated;
        }
        if (requested && !MainRibbon.HasOpenMessages
            && MainRibbon.Template?.FindName("QatBelowHost", MainRibbon) is Border
                { IsVisible: true, ActualHeight: > 0 } drawer)
        {
            // Writer owns document layout. Read the shared drawer's geometry;
            // leave its template, paint, input and native scrolling intact.
            double candidate = MainRibbon.ActualHeight - drawer.TranslatePoint(new Point(), MainRibbon).Y;
            if (double.IsFinite(candidate) && candidate >= drawer.ActualHeight)
                overlap = candidate;
        }

        var margin = _paperHostMargin;
        margin.Top -= overlap;
        if (DocumentPresentationHost.Margin != margin) DocumentPresentationHost.Margin = margin;
        if (overlap > 0)
        {
            if (EditorTopSeparator.Visibility != Visibility.Collapsed)
                EditorTopSeparator.SetCurrentValue(VisibilityProperty, Visibility.Collapsed);
        }
        else if (_paperUnderlayHeight > 0)
            EditorTopSeparator.ClearValue(VisibilityProperty);

        if (overlap == 0 && _paperUnderlayHeight == 0) return;

        EditorViewport.ApplyTemplate();
        var presenter = EditorViewport.Template?.FindName("PART_ScrollContentPresenter", EditorViewport)
            as ScrollContentPresenter;
        var scrollBar = EditorViewport.Template?.FindName("PART_VerticalScrollBar", EditorViewport) as ScrollBar;
        if (!ReferenceEquals(_paperFadePresenter, presenter))
        {
            _paperFadePresenter?.ClearValue(OpacityMaskProperty);
            _paperFadePresenter = presenter;
        }
        if (!ReferenceEquals(_paperScrollBar, scrollBar))
        {
            _paperScrollBar?.ClearValue(MarginProperty);
            _paperScrollBar = scrollBar;
        }

        if (overlap > 0)
        {
            if (_paperEdgeMask is null || Math.Abs(_paperUnderlayHeight - overlap) > 0.1)
            {
                double fadeEnd = overlap + 18;
                var faint = Color.FromArgb(48, 255, 255, 255);
                _paperEdgeMask = new LinearGradientBrush
                {
                    MappingMode = BrushMappingMode.Absolute,
                    StartPoint = new Point(), EndPoint = new Point(0, fadeEnd),
                    GradientStops = [new GradientStop(faint, 0),
                        new GradientStop(faint, overlap / fadeEnd), new GradientStop(Colors.White, 1)],
                };
                _paperEdgeMask.Freeze();
            }
            if (_paperFadePresenter is not null && !ReferenceEquals(_paperFadePresenter.OpacityMask, _paperEdgeMask))
                _paperFadePresenter.SetCurrentValue(OpacityMaskProperty, _paperEdgeMask);
            if (!ReferenceEquals(MarginGuide.OpacityMask, _paperEdgeMask))
                MarginGuide.SetCurrentValue(OpacityMaskProperty, _paperEdgeMask);
            if (_paperScrollBar is not null)
            {
                // Keep the native scrollbar's buttons and track below the drawer.
                var barMargin = new Thickness(0, overlap, 1, 0);
                if (_paperScrollBar.Margin != barMargin) _paperScrollBar.SetCurrentValue(MarginProperty, barMargin);
            }
        }
        else if (_paperUnderlayHeight > 0)
        {
            _paperFadePresenter?.ClearValue(OpacityMaskProperty);
            _paperScrollBar?.ClearValue(MarginProperty);
            MarginGuide.ClearValue(OpacityMaskProperty);
            _paperEdgeMask = null;
        }
        _paperUnderlayHeight = overlap;
    }

    private void DisposeCrystalPaperPresentation()
    {
        MainRibbon.LayoutUpdated -= OnCrystalPaperLayoutUpdated;
        MainRibbon.Loaded -= OnCrystalPaperLoaded;
        MainRibbon.SizeChanged -= OnCrystalPaperSizeChanged;
        DocumentPresentationHost.Margin = _paperHostMargin;
        _paperFadePresenter?.ClearValue(OpacityMaskProperty);
        _paperScrollBar?.ClearValue(MarginProperty);
        MarginGuide.ClearValue(OpacityMaskProperty);
        EditorTopSeparator.ClearValue(VisibilityProperty);
        _paperFadePresenter = null;
        _paperScrollBar = null;
        _paperEdgeMask = null;
    }
}
