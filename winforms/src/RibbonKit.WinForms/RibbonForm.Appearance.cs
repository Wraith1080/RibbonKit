using System.ComponentModel;

namespace RibbonKit.WinForms;

public partial class RibbonForm
{
    private Color _titleBarColor = Color.Empty;
    private float _titleBarTintOpacity = .6f;
    private RibbonTheme? _captionThemeSource, _captionTheme;
    private Color _captionThemeColor;
    private float _captionThemeOpacity;
    private bool _captionThemeComposited;

    /// <summary>Gets or sets the caption color. Empty follows the ribbon theme. A selected color is solid without a backdrop and tints Mica/Acrylic using TitleBarTintOpacity. Must be opaque or Empty. Application content is unaffected.</summary>
    [Category("Appearance"), DefaultValue(typeof(Color), ""), Description("Caption color. Reset follows the ribbon theme. Tints Mica/Acrylic without replacing the backdrop.")]
    public Color TitleBarColor
    {
        get => _titleBarColor;
        set
        {
            _ribbon?.VerifyAccess();
            if (!value.IsEmpty && value.A != 255) throw new ArgumentException("Choose an opaque caption color or reset to Empty.", nameof(value));
            if (_titleBarColor == value) return;
            _titleBarColor = value; InvalidateCaption(); TitleBarColorChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    /// <summary>Occurs after TitleBarColor changes.</summary>
    public event EventHandler? TitleBarColorChanged;

    /// <summary>Gets or sets selected caption-color coverage over Mica/Acrylic, from zero to one. Defaults to .6. Has no effect on solid windows or an unset caption color.</summary>
    [Category("Appearance"), DefaultValue(.6f), Description("Caption color coverage over Mica/Acrylic, from 0 to 1.")]
    public float TitleBarTintOpacity
    {
        get => _titleBarTintOpacity;
        set
        {
            _ribbon?.VerifyAccess();
            if (!float.IsFinite(value) || value < 0 || value > 1) throw new ArgumentOutOfRangeException(nameof(value));
            if (_titleBarTintOpacity == value) return;
            _titleBarTintOpacity = value; InvalidateCaption(); TitleBarTintOpacityChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    /// <summary>Occurs after TitleBarTintOpacity changes.</summary>
    public event EventHandler? TitleBarTintOpacityChanged;

    internal Color CaptionColor(RibbonTheme theme, bool composited)
    {
        var color = TitleBarColor.IsEmpty ? theme.TitleBarBackground : TitleBarColor;
        return color.IsEmpty ? composited ? Color.Transparent : theme.Background
            : composited ? Color.FromArgb((int)Math.Round(255 * TitleBarTintOpacity), color) : color;
    }
    internal RibbonTheme CaptionTheme(RibbonTheme theme, bool composited)
    {
        var color = TitleBarColor.IsEmpty ? theme.TitleBarBackground : TitleBarColor;
        if (color.IsEmpty && theme.TitleBarForeground.IsEmpty) return theme;
        if (ReferenceEquals(theme, _captionThemeSource) && color == _captionThemeColor &&
            _captionThemeOpacity == TitleBarTintOpacity && _captionThemeComposited == composited) return _captionTheme!;
        var background = color.IsEmpty ? theme.Background : composited ? Blend(color, theme.Background, TitleBarTintOpacity) : color;
        var foreground = theme.TitleBarForeground.IsEmpty ? Contrast(background) : theme.TitleBarForeground;
        _captionThemeSource = theme; _captionThemeColor = color; _captionThemeOpacity = TitleBarTintOpacity; _captionThemeComposited = composited;
        return _captionTheme = theme with
        {
            Background = Color.Transparent, Surface = Color.Transparent,
            Foreground = foreground, SecondaryText = Blend(foreground, background, .65f),
            DisabledForeground = Blend(foreground, background, .45f), Accent = foreground,
            Border = Color.FromArgb(40, foreground),
            Hover = new(Color.FromArgb(24, foreground)), Pressed = new(Color.FromArgb(40, foreground)), Checked = new(Color.FromArgb(50, foreground)),
            BackdropHover = new(Color.FromArgb(24, foreground)), BackdropPressed = new(Color.FromArgb(40, foreground))
        };
    }
    private static Color Blend(Color foreground, Color background, float coverage) => Color.FromArgb(
        (int)Math.Round(foreground.R * coverage + background.R * (1 - coverage)),
        (int)Math.Round(foreground.G * coverage + background.G * (1 - coverage)),
        (int)Math.Round(foreground.B * coverage + background.B * (1 - coverage)));
    private static Color Contrast(Color background)
    {
        static double Linear(byte value) { double channel = value / 255d; return channel <= .04045 ? channel / 12.92 : Math.Pow((channel + .055) / 1.055, 2.4); }
        double luminance = .2126 * Linear(background.R) + .7152 * Linear(background.G) + .0722 * Linear(background.B);
        return luminance > .179 ? Color.Black : Color.White;
    }
}
