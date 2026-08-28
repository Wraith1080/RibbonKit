using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace RibbonKit.Writer.Editing;

/// <summary>One complete character-formatting value returned by <see cref="WriterFontDialog"/>.</summary>
public sealed record WriterFontDialogResult(
    FontFamily Family,
    double SizePoints,
    FontStyle Style,
    FontWeight Weight,
    bool Underline,
    Color Color);

/// <summary>A RibbonKit-themed Writer font dialog with transactional Apply/OK behavior.</summary>
public partial class WriterFontDialog : Window
{
    private static readonly DependencyPropertyDescriptor ComboBoxTextDescriptor =
        DependencyPropertyDescriptor.FromProperty(ComboBox.TextProperty, typeof(ComboBox));
    private readonly IReadOnlyList<WriterFontChoice> _availableFonts;
    private readonly List<ComboBox> _editableFields = new();
    private Color _selectedColor;
    private bool _initialized;

    /// <summary>Creates a font dialog from one complete initial value.</summary>
    public WriterFontDialog(
        WriterFontDialogResult initial,
        WriterFontCatalog catalog,
        Window? owner = null)
    {
        ArgumentNullException.ThrowIfNull(initial);
        ArgumentNullException.ThrowIfNull(catalog);
        InitializeComponent();
        if (owner is not null)
            Owner = owner;
        if (SystemParameters.HighContrast)
        {
            SetResourceReference(BackgroundProperty, SystemColors.WindowBrushKey);
            SetResourceReference(ForegroundProperty, SystemColors.WindowTextBrushKey);
        }

        _availableFonts = catalog.CreateProjection(initial.Family).Items;
        FontFamilyBox.ItemsSource = _availableFonts;
        FontFamilyBox.Text = initial.Family.Source;
        FontStyleBox.SelectedIndex = StyleIndex(initial.Style, initial.Weight);
        FontSizeBox.Text = initial.SizePoints.ToString("0.##", CultureInfo.CurrentCulture);
        UnderlineBox.IsChecked = initial.Underline;
        _selectedColor = initial.Color;

        _editableFields.AddRange(new ComboBox[] { FontFamilyBox, FontSizeBox });
        foreach (var field in _editableFields)
            ComboBoxTextDescriptor.AddValueChanged(field, OnEditableTextChanged);
        _initialized = true;
        UpdateState();
    }

    /// <summary>Gets the most recently committed value.</summary>
    public WriterFontDialogResult? Result { get; private set; }

    /// <summary>Raised when Apply commits without closing the dialog.</summary>
    public event EventHandler? Applied;

    /// <summary>Compares two dialog values while allowing harmless point-size conversion noise.</summary>
    public static bool AreEquivalent(WriterFontDialogResult first, WriterFontDialogResult second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);
        return string.Equals(first.Family.Source, second.Family.Source, StringComparison.OrdinalIgnoreCase) &&
               Math.Abs(first.SizePoints - second.SizePoints) < 0.01 &&
               first.Style == second.Style &&
               first.Weight == second.Weight &&
               first.Underline == second.Underline &&
               first.Color == second.Color;
    }

    private void OnEditableTextChanged(object? sender, EventArgs e) => UpdateState();

    private void OnInputChanged(object sender, RoutedEventArgs e) => UpdateState();

    private void OnChooseColor(object sender, RoutedEventArgs e)
    {
        var dialog = new WriterColorDialog(_selectedColor, owner: this);
        if (dialog.ShowDialog() == true && dialog.Result.HasValue)
        {
            _selectedColor = dialog.Result.Value;
            UpdateState();
        }
    }

    private void OnApply(object sender, RoutedEventArgs e) => Commit(close: false);

    private void OnOk(object sender, RoutedEventArgs e) => Commit(close: true);

    private void Commit(bool close)
    {
        UpdateState();
        if (!TryBuildResult(out var result, out _))
            return;
        Result = result;
        if (close)
            DialogResult = true;
        else
            Applied?.Invoke(this, EventArgs.Empty);
    }

    private void UpdateState()
    {
        if (!_initialized)
            return;
        var valid = TryBuildResult(out var result, out var message);
        ApplyButton.IsEnabled = valid;
        OkButton.IsEnabled = valid;
        ValidationText.Text = valid ? string.Empty : message;
        UpdateColorSwatch();
        if (!valid)
            return;

        PreviewText.FontFamily = result.Family;
        PreviewText.FontSize = WriterFontSizePolicy.PointsToDip(result.SizePoints);
        PreviewText.FontStyle = result.Style;
        PreviewText.FontWeight = result.Weight;
        PreviewText.TextDecorations = result.Underline ? TextDecorations.Underline : null;
        PreviewText.Foreground = CreateBrush(result.Color);
    }

    private bool TryBuildResult(out WriterFontDialogResult result, out string message)
    {
        result = default!;
        var familyText = FontFamilyBox.Text.Trim();
        var family = _availableFonts.FirstOrDefault(choice =>
            string.Equals(choice.DisplayName, familyText, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(choice.SourceName, familyText, StringComparison.OrdinalIgnoreCase));
        if (family is null)
        {
            message = "Choose an installed font family.";
            return false;
        }
        if (!WriterFontSizePolicy.TryParsePoints(
                FontSizeBox.Text.Trim(), CultureInfo.CurrentCulture, out var sizePoints))
        {
            message = $"Font size must be between {WriterFontSizePolicy.MinimumPointSize:0} and " +
                      $"{WriterFontSizePolicy.MaximumPointSize:0} points.";
            return false;
        }
        if (FontStyleBox.SelectedItem is not ComboBoxItem { Tag: string styleTag })
        {
            message = "Choose a font style.";
            return false;
        }

        var (style, weight) = styleTag switch
        {
            "Italic" => (FontStyles.Italic, FontWeights.Normal),
            "Bold" => (FontStyles.Normal, FontWeights.Bold),
            "BoldItalic" => (FontStyles.Italic, FontWeights.Bold),
            _ => (FontStyles.Normal, FontWeights.Normal)
        };
        result = new WriterFontDialogResult(
            family.FontFamily,
            sizePoints,
            style,
            weight,
            UnderlineBox.IsChecked == true,
            _selectedColor);
        message = string.Empty;
        return true;
    }

    private void UpdateColorSwatch()
    {
        ColorSwatch.Background = CreateBrush(_selectedColor);
        ColorNameText.Text = $"#{_selectedColor.R:X2}{_selectedColor.G:X2}{_selectedColor.B:X2}";
    }

    private static SolidColorBrush CreateBrush(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

    private static int StyleIndex(FontStyle style, FontWeight weight)
    {
        var bold = weight.ToOpenTypeWeight() >= FontWeights.SemiBold.ToOpenTypeWeight();
        var italic = style == FontStyles.Italic || style == FontStyles.Oblique;
        return (bold, italic) switch
        {
            (true, true) => 3,
            (true, false) => 2,
            (false, true) => 1,
            _ => 0
        };
    }

    /// <inheritdoc />
    protected override void OnClosed(EventArgs e)
    {
        foreach (var field in _editableFields)
            ComboBoxTextDescriptor.RemoveValueChanged(field, OnEditableTextChanged);
        base.OnClosed(e);
    }
}
