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

    /// <summary>Gets or sets the caption color. Empty follows the ribbon theme. A selected color is solid without a backdrop and tints Mica/Acrylic using TitleBarTintOpacity. Themes opting into ExtendTitleBarColorToHeader also color the associated ribbon's tab band. Must be opaque or Empty. Application content is unaffected.</summary>
    [Category("Appearance"), DefaultValue(typeof(Color), ""), Description("Caption color. Reset follows the ribbon theme. Tints Mica/Acrylic without replacing the backdrop.")]
    public Color TitleBarColor
    {
        get => _titleBarColor;
        set
        {
            _ribbon?.VerifyAccess();
            if (!value.IsEmpty && value.A != 255) throw new ArgumentException("Choose an opaque caption color or reset to Empty.", nameof(value));
            if (_titleBarColor == value) return;
            if (_ribbon?.Theme.ExtendTitleBarColorToHeader == true) _ribbon.SettleMotion();
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
            if (_ribbon?.Theme.ExtendTitleBarColorToHeader == true) _ribbon.SettleMotion();
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
    internal RibbonTheme BackstageNavigationCaptionTheme(RibbonControl ribbon, RibbonTheme normal, bool composited)
    {
        var theme = ribbon.Theme;
        var navigation = theme.BackstageCaptionTheme;
        int alpha = ribbon.BackstageSurface?.MotionAlpha ?? 255;
        if (ribbon.HasWindowBackdrop) alpha = (int)Math.Round(alpha * ribbon.Backstage.NavigationBackgroundOpacity);
        // The title plane stays opaque while the live surface fades beneath it.
        // Compare its two existing inks against that same frame's blended fill.
        // Material uses the normal theme backing as its contrast estimate, as
        // ordinary authored caption colors do; no desktop sampling is required.
        var background = Blend(theme.BackstageNavigationColor, CaptionContrastBackground(theme, composited), alpha / 255f);
        var foreground = ReadableCaptionInk(navigation.Foreground, normal.Foreground, background);
        var secondary = ReadableCaptionInk(navigation.SecondaryText, normal.SecondaryText, background);
        return foreground == navigation.Foreground && secondary == navigation.SecondaryText ? navigation
            : navigation with { Foreground = foreground, SecondaryText = secondary };
    }
    internal RibbonTheme BackstageTintCaptionTheme(RibbonControl ribbon, RibbonTheme normal, bool composited)
    {
        if (!ribbon.Theme.TitleBarForeground.IsEmpty) return normal;
        var tint = ribbon.CaptionBackground(composited);
        var background = Blend(tint, CaptionContrastBackground(ribbon.Theme, composited), tint.A / 255f);
        var foreground = ReadableCaptionInk(normal.Foreground, Contrast(background), background);
        // Preserve the ordinary inactive ink whenever it is already readable.
        var secondary = ReadableCaptionInk(normal.SecondaryText, Contrast(background), background);
        return foreground == normal.Foreground && secondary == normal.SecondaryText ? normal
            : normal with { Foreground = foreground, SecondaryText = secondary };
    }
    private Color CaptionContrastBackground(RibbonTheme theme, bool composited)
    {
        var color = TitleBarColor.IsEmpty ? theme.TitleBarBackground : TitleBarColor;
        return color.IsEmpty ? theme.Background : composited ? Blend(color, theme.Background, TitleBarTintOpacity) : color;
    }
    private static Color ReadableCaptionInk(Color preferred, Color fallback, Color background)
    {
        double Ratio(Color color)
        {
            double a = CaptionLuminance(color), b = CaptionLuminance(background);
            return (Math.Max(a, b) + .05) / (Math.Min(a, b) + .05);
        }
        // Retain authored/family ink if readable; use the existing alternate ink
        // when fading would otherwise leave light text over the light caption.
        return Ratio(preferred) >= 4.5 || Ratio(preferred) >= Ratio(fallback) ? preferred : fallback;
    }
    private static Color Blend(Color foreground, Color background, float coverage) => Color.FromArgb(
        (int)Math.Round(foreground.R * coverage + background.R * (1 - coverage)),
        (int)Math.Round(foreground.G * coverage + background.G * (1 - coverage)),
        (int)Math.Round(foreground.B * coverage + background.B * (1 - coverage)));
    private static Color Contrast(Color background)
        => CaptionLuminance(background) > .179 ? Color.Black : Color.White;
    private static double CaptionLuminance(Color background)
    {
        static double Linear(byte value) { double channel = value / 255d; return channel <= .04045 ? channel / 12.92 : Math.Pow((channel + .055) / 1.055, 2.4); }
        return .2126 * Linear(background.R) + .7152 * Linear(background.G) + .0722 * Linear(background.B);
    }
}
