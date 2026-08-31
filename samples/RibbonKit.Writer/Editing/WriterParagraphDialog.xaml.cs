using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace RibbonKit.Writer.Editing;

/// <summary>An immutable set of paragraph values committed by <see cref="WriterParagraphDialog"/>.</summary>
public sealed record WriterParagraphDialogResult(
    TextAlignment? Alignment,
    double? LeftIndent,
    double? RightIndent,
    double? SpecialBy,
    bool? Hanging,
    double? SpacingBefore,
    double? SpacingAfter);

/// <summary>
/// A Writer-owned, transactional paragraph formatting dialog. It does not mutate a document directly.
/// </summary>
public partial class WriterParagraphDialog : Window
{
    private static readonly DependencyPropertyDescriptor ComboBoxTextDescriptor =
        DependencyPropertyDescriptor.FromProperty(ComboBox.TextProperty, typeof(ComboBox));
    private readonly List<ComboBox> _numericFields = new();
    private bool _initialized;

    /// <summary>Creates a paragraph dialog. Null inputs represent mixed or unset selection state.</summary>
    public WriterParagraphDialog(TextAlignment? initialAlignment = null,
        double? initialLeftIndent = null,
        double? initialRightIndent = null,
        double? initialSpecialBy = null,
        bool? initialHanging = null,
        double? initialSpacingBefore = null,
        double? initialSpacingAfter = null,
        Window? owner = null)
    {
        InitializeComponent();
        _numericFields.AddRange(new[]
        {
            LeftIndentBox,
            RightIndentBox,
            SpecialByBox,
            SpacingBeforeBox,
            SpacingAfterBox
        });
        foreach (var field in _numericFields)
            ComboBoxTextDescriptor.AddValueChanged(field, OnNumericTextChanged);
        if (SystemParameters.HighContrast)
        {
            SetResourceReference(BackgroundProperty, SystemColors.WindowBrushKey);
            SetResourceReference(ForegroundProperty, SystemColors.WindowTextBrushKey);
        }
        if (owner is not null)
            Owner = owner;

        if (initialAlignment.HasValue && TryGetAlignmentTag(initialAlignment.Value, out var alignment))
            AlignmentBox.SelectedItem = AlignmentBox.Items.OfType<ComboBoxItem>().FirstOrDefault(item =>
                string.Equals(item.Tag as string, alignment, StringComparison.Ordinal));
        if (initialLeftIndent.HasValue)
            LeftIndentBox.Text = FormatNumber(initialLeftIndent.Value);
        if (initialRightIndent.HasValue)
            RightIndentBox.Text = FormatNumber(initialRightIndent.Value);
        if (initialSpecialBy.HasValue && initialSpecialBy.Value == 0d)
            SpecialBox.SelectedIndex = 0;
        else if (initialSpecialBy is > 0 && initialHanging.HasValue)
        {
            SpecialBox.SelectedIndex = initialHanging.Value ? 2 : 1;
            SpecialByBox.Text = FormatNumber(initialSpecialBy.Value);
        }
        if (initialSpacingBefore.HasValue)
            SpacingBeforeBox.Text = FormatNumber(initialSpacingBefore.Value);
        if (initialSpacingAfter.HasValue)
            SpacingAfterBox.Text = FormatNumber(initialSpacingAfter.Value);

        _initialized = true;
        UpdateValidation();
    }

    /// <summary>Gets the immutable values committed by OK or Apply, or null before the first commit.</summary>
    public WriterParagraphDialogResult? Result { get; private set; }

    /// <summary>
    /// Raised after the non-closing Apply button commits <see cref="Result"/>. OK commits and closes
    /// through the modal result path so a host can apply it once without double-applying the final state.
    /// </summary>
    public event EventHandler? Applied;

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateValidation();

    private void OnNumericTextChanged(object? sender, EventArgs e) => UpdateValidation();

    private void OnSpecialChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SpecialBox.SelectedItem is ComboBoxItem { Tag: "None" })
        {
            SpecialByBox.Text = "0";
            SpecialByBox.IsEnabled = false;
        }
        else
        {
            SpecialByBox.IsEnabled = true;
            if (_initialized && SpecialBox.SelectedItem is ComboBoxItem &&
                string.IsNullOrWhiteSpace(SpecialByBox.Text))
                SpecialByBox.Text = "0";
        }

        UpdateValidation();
    }

    private void OnApply(object sender, RoutedEventArgs e) => Commit(false);

    private void OnOk(object sender, RoutedEventArgs e) => Commit(true);

    private void Commit(bool close)
    {
        UpdateValidation();
        if (!OkButton.IsEnabled || !TryBuildResult(out var result))
            return;

        Result = result;
        if (close)
            DialogResult = true;
        else
            Applied?.Invoke(this, EventArgs.Empty);
    }

    private void UpdateValidation()
    {
        if (!_initialized && !IsInitialized)
            return;

        var valid = TryBuildResult(out _, out var message);
        OkButton.IsEnabled = valid;
        ApplyButton.IsEnabled = valid;
        ValidationText.Text = valid ? string.Empty : message;
        if (valid && TryBuildResult(out var previewResult))
            UpdatePreview(previewResult);
    }

    private bool TryBuildResult(out WriterParagraphDialogResult result) =>
        TryBuildResult(out result, out _);

    private bool TryBuildResult(out WriterParagraphDialogResult result, out string message)
    {
        result = new WriterParagraphDialogResult(
            ReadAlignment(),
            null,
            null,
            null,
            null,
            null,
            null);

        if (!TryReadOptionalNonNegative(LeftIndentBox, out var leftIndent) ||
            !TryReadOptionalNonNegative(RightIndentBox, out var rightIndent) ||
            !TryReadOptionalNonNegative(SpacingBeforeBox, out var spacingBefore) ||
            !TryReadOptionalNonNegative(SpacingAfterBox, out var spacingAfter))
        {
            message = "Indent and spacing values must be finite, non-negative numbers, or empty.";
            return false;
        }

        if (!TryReadSpecialIndent(out var specialBy, out var hanging, out message))
            return false;

        result = result with
        {
            LeftIndent = leftIndent,
            RightIndent = rightIndent,
            SpecialBy = specialBy,
            Hanging = hanging,
            SpacingBefore = spacingBefore,
            SpacingAfter = spacingAfter
        };
        message = string.Empty;
        return true;
    }

    private bool TryReadSpecialIndent(out double? specialBy, out bool? hanging, out string message)
    {
        specialBy = null;
        hanging = null;
        message = string.Empty;

        if (SpecialBox.SelectedItem is not ComboBoxItem item || item.Tag is not string tag)
        {
            if (string.IsNullOrWhiteSpace(SpecialByBox.Text))
                return true;
            message = "Choose First line or Hanging before entering a special indentation value.";
            return false;
        }

        if (tag.Equals("None", StringComparison.Ordinal))
        {
            specialBy = 0d;
            hanging = false;
            return true;
        }

        if (!TryReadRequiredNonNegative(SpecialByBox, out var value))
        {
            message = "Special indentation must be a finite, non-negative number.";
            return false;
        }

        specialBy = value;
        hanging = tag.Equals("Hanging", StringComparison.Ordinal);
        return true;
    }

    private TextAlignment? ReadAlignment() => AlignmentBox.SelectedItem is ComboBoxItem item &&
        item.Tag is string tag && Enum.TryParse<TextAlignment>(tag, out var alignment)
        ? alignment
        : null;

    private static bool TryGetAlignmentTag(TextAlignment alignment, out string tag)
    {
        tag = alignment switch
        {
            TextAlignment.Left => "Left",
            TextAlignment.Center => "Center",
            TextAlignment.Right => "Right",
            TextAlignment.Justify => "Justify",
            _ => string.Empty
        };
        return tag.Length > 0;
    }

    private static bool TryReadOptionalNonNegative(ComboBox box, out double? value)
    {
        if (string.IsNullOrWhiteSpace(box.Text))
        {
            value = null;
            return true;
        }

        if (TryParseFiniteDouble(box.Text.Trim(), out var parsed) && parsed >= 0d)
        {
            value = parsed;
            return true;
        }

        value = null;
        return false;
    }

    private static bool TryReadRequiredNonNegative(ComboBox box, out double value)
    {
        if (TryParseFiniteDouble(box.Text.Trim(), out value) && value >= 0d)
            return true;
        value = 0d;
        return false;
    }

    private static bool TryParseFiniteDouble(string text, out double value)
    {
        return (double.TryParse(text, NumberStyles.Float,
                    CultureInfo.CurrentCulture, out value) ||
                double.TryParse(text, NumberStyles.Float,
                    CultureInfo.InvariantCulture, out value)) &&
            double.IsFinite(value);
    }

    private static string FormatNumber(double value) => value.ToString("0.##", CultureInfo.CurrentCulture);

    private void UpdatePreview(WriterParagraphDialogResult result)
    {
        static double PreviewIndent(double? points) => Math.Clamp(points.GetValueOrDefault() * 0.8, 0, 64);
        static double PreviewSpacing(double? points) => Math.Clamp(points.GetValueOrDefault() * 0.35, 0, 10);

        var alignment = result.Alignment ?? TextAlignment.Left;
        PreviewFirstLine.TextAlignment = alignment;
        PreviewContinuation.TextAlignment = alignment;
        var left = PreviewIndent(result.LeftIndent);
        var right = PreviewIndent(result.RightIndent);
        var special = Math.Clamp(result.SpecialBy.GetValueOrDefault() * 0.8, 0, 36);
        PreviewFirstLine.Margin = new Thickness(
            left + (result.Hanging == true ? 0 : special), 0, right, 0);
        PreviewContinuation.Margin = new Thickness(
            left + (result.Hanging == true ? special : 0), 0, right, 0);
        PreviewParagraphChrome.Padding = new Thickness(8, 5, 8, 5);
        PreviewParagraphChrome.Margin = new Thickness(
            0,
            PreviewSpacing(result.SpacingBefore),
            0,
            PreviewSpacing(result.SpacingAfter));
    }

    /// <inheritdoc />
    protected override void OnClosed(EventArgs e)
    {
        foreach (var field in _numericFields)
            ComboBoxTextDescriptor.RemoveValueChanged(field, OnNumericTextChanged);
        base.OnClosed(e);
    }
}
