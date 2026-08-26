using System.Windows;
using System.Windows.Media;

namespace RibbonKit.Writer.Editing;

/// <summary>Draws one accessible cell in Writer's 8×8 Insert-table gallery.</summary>
public sealed class WriterTableGridCellPreview : FrameworkElement
{
    public static readonly DependencyProperty IsHighlightedProperty = DependencyProperty.Register(
        nameof(IsHighlighted), typeof(bool), typeof(WriterTableGridCellPreview),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Gets or sets whether this cell belongs to the currently previewed table size.</summary>
    public bool IsHighlighted
    {
        get => (bool)GetValue(IsHighlightedProperty);
        set => SetValue(IsHighlightedProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize) => new(20, 20);

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);

        var highContrast = SystemParameters.HighContrast;
        var background = IsHighlighted
            ? highContrast
                ? SystemColors.HighlightBrush
                : TryFindResource("RibbonKit.Brushes.Control.CheckedBackground") as Brush
                    ?? SystemColors.HighlightBrush
            : TryFindResource("RibbonKit.Brushes.Control.SurfaceBackground") as Brush
                ?? SystemColors.WindowBrush;
        var border = IsHighlighted && highContrast
            ? SystemColors.HighlightTextBrush
            : TryFindResource("RibbonKit.Brushes.Text.Secondary") as Brush
                ?? SystemColors.WindowTextBrush;
        var pen = new Pen(border, 1);
        if (pen.CanFreeze)
            pen.Freeze();

        drawingContext.DrawRectangle(background, pen,
            new Rect(0.5, 0.5, Math.Max(0, ActualWidth - 1), Math.Max(0, ActualHeight - 1)));
    }
}
