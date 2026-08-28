using System.Globalization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace RibbonKit.Writer.Editing;

/// <summary>A RibbonKit-themed solid-color picker with palette and exact-value entry.</summary>
public partial class WriterColorDialog : Window
{
    private static readonly Color[] StandardColors =
    [
        Colors.Black,
        Color.FromRgb(0x80, 0x80, 0x80),
        Colors.White,
        Color.FromRgb(0xFF, 0x00, 0x00),
        Color.FromRgb(0xED, 0x7D, 0x31),
        Color.FromRgb(0xFF, 0xFF, 0x00),
        Color.FromRgb(0x00, 0xB0, 0x50),
        Color.FromRgb(0x00, 0xB0, 0xF0),
        Color.FromRgb(0x00, 0x70, 0xC0),
        Color.FromRgb(0x70, 0x30, 0xA0)
    ];

    private readonly Dictionary<Color, Button> _swatchButtons = new();
    private Color _selectedColor;
    private bool _updating;
    private bool _initialized;
    private bool _draggingSaturationValue;
    private bool _draggingHue;
    private double _hue;
    private double _saturation;
    private double _brightness;

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

    private void OnSaturationValueMouseDown(object sender, MouseButtonEventArgs e)
    {
        _draggingSaturationValue = true;
        SaturationValueSurface.Focus();
        Mouse.Capture(SaturationValueSurface);
        UpdateSaturationValueFromPoint(e.GetPosition(SaturationValueSurface));
        e.Handled = true;
    }

    private void OnSaturationValueMouseMove(object sender, MouseEventArgs e)
    {
        if (_draggingSaturationValue && e.LeftButton == MouseButtonState.Pressed)
            UpdateSaturationValueFromPoint(e.GetPosition(SaturationValueSurface));
    }

    private void OnHueMouseDown(object sender, MouseButtonEventArgs e)
    {
        _draggingHue = true;
        HueSurface.Focus();
        Mouse.Capture(HueSurface);
        UpdateHueFromPoint(e.GetPosition(HueSurface));
        e.Handled = true;
    }

    private void OnHueMouseMove(object sender, MouseEventArgs e)
    {
        if (_draggingHue && e.LeftButton == MouseButtonState.Pressed)
            UpdateHueFromPoint(e.GetPosition(HueSurface));
    }

    private void OnPickerMouseUp(object sender, MouseButtonEventArgs e)
    {
        ReleasePickerCapture();
        e.Handled = true;
    }

    private void OnPickerLostMouseCapture(object sender, MouseEventArgs e)
    {
        _draggingSaturationValue = false;
        _draggingHue = false;
    }

    private void OnPickerSizeChanged(object sender, SizeChangedEventArgs e) => UpdatePickerVisuals();

    private void OnSaturationValueKeyDown(object sender, KeyEventArgs e)
    {
        var step = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? 0.05 : 0.01;
        switch (e.Key)
        {
            case Key.Left: _saturation = Math.Clamp(_saturation - step, 0, 1); break;
            case Key.Right: _saturation = Math.Clamp(_saturation + step, 0, 1); break;
            case Key.Up: _brightness = Math.Clamp(_brightness + step, 0, 1); break;
            case Key.Down: _brightness = Math.Clamp(_brightness - step, 0, 1); break;
            default: return;
        }
        SetSelectedHsv();
        e.Handled = true;
    }

    private void OnHueKeyDown(object sender, KeyEventArgs e)
    {
        var step = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? 10d : 1d;
        if (e.Key == Key.Left)
            _hue = NormalizeHue(_hue - step);
        else if (e.Key == Key.Right)
            _hue = NormalizeHue(_hue + step);
        else
            return;
        SetSelectedHsv();
        e.Handled = true;
    }

    private void UpdateSaturationValueFromPoint(Point point)
    {
        var width = Math.Max(1, SaturationValueSurface.ActualWidth);
        var height = Math.Max(1, SaturationValueSurface.ActualHeight);
        _saturation = Math.Clamp(point.X / width, 0, 1);
        _brightness = Math.Clamp(1 - point.Y / height, 0, 1);
        SetSelectedHsv();
    }

    private void UpdateHueFromPoint(Point point)
    {
        var width = Math.Max(1, HueSurface.ActualWidth);
        var ratio = Math.Clamp(point.X / width, 0, 1);
        _hue = ratio >= 1 ? 359.999d : ratio * 360d;
        SetSelectedHsv();
    }

    private void SetSelectedHsv()
    {
        SetSelectedColor(FromHsv(_hue, _saturation, _brightness), updateHsv: false);
    }

    private void ReleasePickerCapture()
    {
        _draggingSaturationValue = false;
        _draggingHue = false;
        if (ReferenceEquals(Mouse.Captured, SaturationValueSurface) ||
            ReferenceEquals(Mouse.Captured, HueSurface))
            Mouse.Capture(null);
    }

    private void SetSelectedColor(
        Color color,
        bool updateHex = true,
        bool updateRgb = true,
        bool updateHsv = true)
    {
        color = Color.FromRgb(color.R, color.G, color.B);
        _selectedColor = color;
        if (updateHsv)
            ToHsv(color, out _hue, out _saturation, out _brightness);
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
        UpdatePickerVisuals();
        ShowValidation(string.Empty);
    }

    private void UpdatePickerVisuals()
    {
        HueBackground.Background = CreateBrush(FromHsv(_hue, 1, 1));
        if (SaturationValueSurface.ActualWidth > 0 && SaturationValueSurface.ActualHeight > 0)
        {
            var x = _saturation * SaturationValueSurface.ActualWidth;
            var y = (1 - _brightness) * SaturationValueSurface.ActualHeight;
            Canvas.SetLeft(SaturationValueIndicatorOuter, x - 7);
            Canvas.SetTop(SaturationValueIndicatorOuter, y - 7);
            Canvas.SetLeft(SaturationValueIndicatorInner, x - 5);
            Canvas.SetTop(SaturationValueIndicatorInner, y - 5);
        }
        if (HueSurface.ActualWidth > 0)
            Canvas.SetLeft(HueIndicator, _hue / 360d * HueSurface.ActualWidth - 2);
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

    internal static Color FromHsv(double hue, double saturation, double brightness)
    {
        hue = NormalizeHue(hue);
        saturation = Math.Clamp(saturation, 0, 1);
        brightness = Math.Clamp(brightness, 0, 1);
        var chroma = brightness * saturation;
        var section = hue / 60d;
        var intermediate = chroma * (1 - Math.Abs(section % 2 - 1));
        var (red, green, blue) = section switch
        {
            < 1 => (chroma, intermediate, 0d),
            < 2 => (intermediate, chroma, 0d),
            < 3 => (0d, chroma, intermediate),
            < 4 => (0d, intermediate, chroma),
            < 5 => (intermediate, 0d, chroma),
            _ => (chroma, 0d, intermediate)
        };
        var match = brightness - chroma;
        return Color.FromRgb(
            ToByte(red + match),
            ToByte(green + match),
            ToByte(blue + match));
    }

    internal static void ToHsv(
        Color color,
        out double hue,
        out double saturation,
        out double brightness)
    {
        var red = color.R / 255d;
        var green = color.G / 255d;
        var blue = color.B / 255d;
        var maximum = Math.Max(red, Math.Max(green, blue));
        var minimum = Math.Min(red, Math.Min(green, blue));
        var delta = maximum - minimum;
        brightness = maximum;
        saturation = maximum <= double.Epsilon ? 0 : delta / maximum;
        if (delta <= double.Epsilon)
        {
            hue = 0;
            return;
        }

        hue = maximum == red
            ? 60d * ((green - blue) / delta % 6d)
            : maximum == green
                ? 60d * ((blue - red) / delta + 2d)
                : 60d * ((red - green) / delta + 4d);
        hue = NormalizeHue(hue);
    }

    private static bool TryParseChannel(string? text, out byte value) =>
        byte.TryParse(text?.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out value);

    private static string ToHex(Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";

    private static byte ToByte(double value) =>
        (byte)Math.Round(Math.Clamp(value, 0, 1) * 255d, MidpointRounding.AwayFromZero);

    private static double NormalizeHue(double hue)
    {
        hue %= 360d;
        return hue < 0 ? hue + 360d : hue;
    }

    private static SolidColorBrush CreateBrush(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }
}
