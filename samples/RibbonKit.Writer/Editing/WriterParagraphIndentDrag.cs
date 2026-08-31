using System.Windows;
using System.Windows.Documents;

namespace RibbonKit.Writer.Editing;

/// <summary>
/// A deferred paragraph-indent drag whose final formatting is one native undo unit.
/// </summary>
/// <remarks>
/// Pointer updates only retain a candidate delta. They do not write WPF paragraph properties, so
/// cancelling a drag cannot coerce inherited values, dirty the document or create an undo entry.
/// Commit asks the adapter to open one native change scope and apply the final values to every
/// selected paragraph, preserving mixed-selection relative deltas.
/// </remarks>
public sealed class WriterParagraphIndentDrag : IDisposable
{
    private const double Epsilon = 0.0001;
    private readonly WriterEditingAdapter _adapter;
    private readonly WriterRulerIndentMarker _marker;
    private readonly IReadOnlyList<ParagraphSnapshot> _paragraphs;
    private readonly double _anchorMarkerDip;
    private double _delta;
    private bool _hasPreview;
    private bool _completed;

    internal WriterParagraphIndentDrag(
        WriterEditingAdapter adapter,
        WriterRulerIndentMarker marker,
        IReadOnlyList<ParagraphSnapshot> paragraphs,
        double contentWidthDip)
    {
        ArgumentNullException.ThrowIfNull(adapter);
        ArgumentNullException.ThrowIfNull(paragraphs);
        if (!Enum.IsDefined(marker))
            throw new ArgumentOutOfRangeException(nameof(marker), marker, "Unknown ruler marker.");
        if (paragraphs.Count == 0)
            throw new ArgumentException("At least one paragraph is required.", nameof(paragraphs));

        _adapter = adapter;
        _marker = marker;
        _paragraphs = paragraphs;
        _anchorMarkerDip = GetMarkerDip(paragraphs[0], marker, contentWidthDip);
    }

    /// <summary>Gets the marker being dragged.</summary>
    public WriterRulerIndentMarker Marker => _marker;

    /// <summary>Gets whether the drag still accepts updates or completion.</summary>
    public bool IsActive => !_completed;

    /// <summary>Gets whether a pointer update has produced a candidate value.</summary>
    internal bool HasPreview => _hasPreview;

    /// <summary>Gets the candidate marker position in logical content DIPs.</summary>
    internal double PreviewMarkerPositionDip => _anchorMarkerDip + _delta;

    /// <summary>
    /// Records a pointer position expressed in logical content DIPs. No document property or undo
    /// state is changed until <see cref="Commit"/>.
    /// </summary>
    public void Update(double markerPositionDip)
    {
        ThrowIfCompleted();
        if (!double.IsFinite(markerPositionDip))
            throw new ArgumentOutOfRangeException(nameof(markerPositionDip), markerPositionDip,
                "The paragraph marker position must be finite.");

        _delta = markerPositionDip - _anchorMarkerDip;
        _hasPreview = true;
    }

    /// <summary>Applies the candidate once through one native formatting undo unit.</summary>
    public void Commit()
    {
        if (_completed)
            return;

        try
        {
            if (_hasPreview && Math.Abs(_delta) > Epsilon)
                _adapter.CommitParagraphIndentDrag(this);
        }
        finally
        {
            _completed = true;
        }
    }

    /// <summary>
    /// Cancels the candidate without writing paragraph properties, changing selection or touching
    /// the native undo history.
    /// </summary>
    public void Cancel()
    {
        if (_completed)
            return;
        _completed = true;
    }

    /// <inheritdoc />
    public void Dispose() => Commit();

    /// <summary>Applies the final values while the adapter owns its native change scope.</summary>
    internal void ApplyCommittedValues()
    {
        ThrowIfCompleted();
        foreach (var paragraph in _paragraphs)
        {
            var (margin, textIndent) = GetCandidate(paragraph, _marker, _delta);
            var range = new TextRange(paragraph.Paragraph.ContentStart, paragraph.Paragraph.ContentEnd);
            if (_marker is WriterRulerIndentMarker.Left or WriterRulerIndentMarker.Right or
                WriterRulerIndentMarker.Hanging)
            {
                range.ApplyPropertyValue(Paragraph.MarginProperty, margin);
            }
            if (_marker is WriterRulerIndentMarker.FirstLine or WriterRulerIndentMarker.Hanging)
            {
                range.ApplyPropertyValue(Paragraph.TextIndentProperty, textIndent);
            }
        }
    }

    private static (Thickness Margin, double TextIndent) GetCandidate(
        ParagraphSnapshot paragraph, WriterRulerIndentMarker marker, double delta)
    {
        var margin = paragraph.Margin;
        var textIndent = paragraph.TextIndent;
        switch (marker)
        {
            case WriterRulerIndentMarker.Left:
                margin.Left = Math.Max(0, paragraph.Margin.Left + delta);
                break;
            case WriterRulerIndentMarker.Right:
                margin.Right = Math.Max(0, paragraph.Margin.Right - delta);
                break;
            case WriterRulerIndentMarker.FirstLine:
                textIndent = paragraph.TextIndent + delta;
                break;
            case WriterRulerIndentMarker.Hanging:
                // Move the body marker and compensate TextIndent by the applied margin delta so
                // the physical first-line position remains fixed.
                margin.Left = Math.Max(0, paragraph.Margin.Left + delta);
                textIndent = paragraph.TextIndent - (margin.Left - paragraph.Margin.Left);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(marker), marker, "Unknown ruler marker.");
        }
        return (margin, textIndent);
    }

    private static double GetMarkerDip(ParagraphSnapshot paragraph, WriterRulerIndentMarker marker,
        double contentWidthDip) => marker switch
    {
        WriterRulerIndentMarker.Left => paragraph.Margin.Left,
        WriterRulerIndentMarker.Right => contentWidthDip - paragraph.Margin.Right,
        WriterRulerIndentMarker.FirstLine => paragraph.Margin.Left + paragraph.TextIndent,
        WriterRulerIndentMarker.Hanging => paragraph.Margin.Left,
        _ => throw new ArgumentOutOfRangeException(nameof(marker), marker, "Unknown ruler marker.")
    };

    private void ThrowIfCompleted()
    {
        if (_completed)
            throw new InvalidOperationException("The paragraph-indent drag has already completed.");
    }

    internal sealed class ParagraphSnapshot
    {
        public ParagraphSnapshot(Paragraph paragraph)
        {
            ArgumentNullException.ThrowIfNull(paragraph);
            Paragraph = paragraph;
            LocalMarginValue = paragraph.ReadLocalValue(Paragraph.MarginProperty);
            LocalTextIndentValue = paragraph.ReadLocalValue(Paragraph.TextIndentProperty);
            var margin = paragraph.Margin;
            Margin = new Thickness(
                Normalize(margin.Left), Normalize(margin.Top),
                Normalize(margin.Right), Normalize(margin.Bottom));
            TextIndent = Normalize(paragraph.TextIndent);
        }

        public Paragraph Paragraph { get; }

        /// <summary>Gets the effective opening margin used for candidate math.</summary>
        public Thickness Margin { get; }

        /// <summary>Gets the effective opening signed TextIndent used for candidate math.</summary>
        public double TextIndent { get; }

        /// <summary>Gets the raw local margin value captured for semantic diagnostics.</summary>
        internal object LocalMarginValue { get; }

        /// <summary>Gets the raw local TextIndent value captured for semantic diagnostics.</summary>
        internal object LocalTextIndentValue { get; }

        private static double Normalize(double value) => double.IsNaN(value) ? 0 : value;
    }
}
