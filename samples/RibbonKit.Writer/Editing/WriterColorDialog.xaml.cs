using System.Globalization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;

namespace RibbonKit.Writer.Editing;

/// <summary>A RibbonKit-themed solid-color picker with palette and exact-value entry.</summary>
public partial class WriterColorDialog : Window
{
    private static readonly Color[] StandardColors =
    [
        Colors.Black,
        Color.FromRgb(0x40, 0x40, 0x40),
        Color.FromRgb(0x80, 0x80, 0x80),
        Color.FromRgb(0xC0, 0xC0, 0xC0),
        Colors.White,
        Color.FromRgb(0xC0, 0x00, 0x00),
        Color.FromRgb(0xFF, 0x00, 0x00),
        Color.FromRgb(0xED, 0x7D, 0x31),
        Color.FromRgb(0xFF, 0xC0, 0x00),
        Color.FromRgb(0xFF, 0xFF, 0x00),
        Color.FromRgb(0x70, 0xAD, 0x47),
        Color.FromRgb(0x00, 0xB0, 0x50),
        Color.FromRgb(0x00, 0xB0, 0xF0),
        Color.FromRgb(0x00, 0x70, 0xC0),
        Color.FromRgb(0x44, 0x72, 0xC4),
        Color.FromRgb(0x70, 0x30, 0xA0)
    ];

    private readonly Dictionary<Color, Button> _swatchButtons = new();
    private Color _selectedColor;
    private bool _updating;
    private bool _initialized;

    /// <summary>Creates a color dialog with optional recent colors.</summary>
    public WriterColorDialog(
        Color initial,
        IEnumerable<Color>? recentColors = null,
        Window? owner = null)
    {
        InitializeComponent();
        if (owner is not null)
            Owner = owner;
        if (SystemParameters.HighContrast)
        {
            SetResourceReference(BackgroundProperty, SystemColors.WindowBrushKey);
            SetResourceReference(ForegroundProperty, SystemColors.WindowTextBrushKey);
        }

        BuildPalette(initial, recentColors);
        _initialized = true;
        SetSelectedColor(initial);
    }

    /// <summary>Gets the accepted color, or null until OK is chosen.</summary>
    public Color? Result { get; private set; }

    private void BuildPalette(Color initial, IEnumerable<Color>? recentColors)
    {
        var colors = new[] { initial }
            .Concat(recentColors ?? [])
            .Concat(StandardColors)
            .Select(color => Color.FromRgb(color.R, color.G, color.B))
            .Distinct()
            .Take(24);
        foreach (var color in colors)
        {
            var swatch = new Border
            {
                Width = 22,
                Height = 18,
                Background = CreateBrush(color),
                BorderBrush = SystemColors.ControlDarkBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(2)
            };
            var button = new Button
            {
                Width = 38,
                Height = 34,
                MinWidth = 0,
                Padding = new Thickness(6),
                Margin = new Thickness(0, 0, 8, 8),
                Content = swatch,
                ToolTip = ToHex(color)
            };
            button.SetResourceReference(FrameworkElement.StyleProperty, "OptionsDialogActionButtonStyle");
            button.Click += (_, _) => SetSelectedColor(color);
            AutomationProperties.SetName(button, $"Choose {ToHex(color)}");
            PalettePanel.Children.Add(button);
            _swatchButtons[color] = button;
        }
    }

    private void OnHexChanged(object sender, TextChangedEventArgs e)
    {
        if (!_initialized || _updating)
            return;
        if (TryParseHex(HexBox.Text, out var color))
            SetSelectedColor(color, updateHex: false);
        else
            ShowValidation("Enter a Hex color as #RRGGBB.");
    }

    private void OnRgbChanged(object sender, TextChangedEventArgs e)
    {
        if (!_initialized || _updating)
            return;
        if (TryParseChannel(RedBox.Text, out var red) &&
            TryParseChannel(GreenBox.Text, out var green) &&
            TryParseChannel(BlueBox.Text, out var blue))
        {
            SetSelectedColor(Color.FromRgb(red, green, blue), updateRgb: false);
        }
        else
        {
            ShowValidation("RGB channels must be whole numbers from 0 through 255.");
        }
    }

    private void OnOk(object sender, RoutedEventArgs e)
    {
        if (!OkButton.IsEnabled)
            return;
        Result = _selectedColor;
        DialogResult = true;
    }

    private void SetSelectedColor(Color color, bool updateHex = true, bool updateRgb = true)
    {
        color = Color.FromRgb(color.R, color.G, color.B);
        _selectedColor = color;
        _updating = true;
        try
        {
            if (updateHex)
                HexBox.Text = ToHex(color);
            if (updateRgb)
            {
                RedBox.Text = color.R.ToString(CultureInfo.InvariantCulture);
                GreenBox.Text = color.G.ToString(CultureInfo.InvariantCulture);
                BlueBox.Text = color.B.ToString(CultureInfo.InvariantCulture);
            }
        }
        finally
        {
            _updating = false;
        }

        PreviewSwatch.Background = CreateBrush(color);
        SelectedColorText.Text = $"{ToHex(color)}   RGB {color.R}, {color.G}, {color.B}";
        foreach (var (swatchColor, button) in _swatchButtons)
        {
            button.BorderBrush = swatchColor == color
                ? TryFindResource("RibbonKit.Brushes.Accent") as Brush ?? SystemColors.HighlightBrush
                : TryFindResource("RibbonKit.Brushes.ScreenTip.Border") as Brush ?? SystemColors.ControlDarkBrush;
            button.BorderThickness = swatchColor == color ? new Thickness(2) : new Thickness(1);
        }
        ShowValidation(string.Empty);
    }

    private void ShowValidation(string message)
    {
        ValidationText.Text = message;
        OkButton.IsEnabled = message.Length == 0;
    }

    internal static bool TryParseHex(string? text, out Color color)
    {
        color = default;
        var value = text?.Trim();
        if (value is null || value.Length != 7 || value[0] != '#')
            return false;
        if (!byte.TryParse(value.AsSpan(1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var red) ||
            !byte.TryParse(value.AsSpan(3, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var green) ||
            !byte.TryParse(value.AsSpan(5, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var blue))
            return false;
        color = Color.FromRgb(red, green, blue);
        return true;
    }

    private static bool TryParseChannel(string? text, out byte value) =>
        byte.TryParse(text?.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out value);

    private static string ToHex(Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";

    private static SolidColorBrush CreateBrush(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }
}
