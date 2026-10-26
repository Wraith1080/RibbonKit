namespace RibbonKit.WinForms;

/// <summary>A solid or vertical two-stop gradient fill; contains no disposable resources.</summary>
public readonly record struct RibbonFill(Color Start, Color End)
{
    /// <summary>Initializes a RibbonFill instance.</summary>
    public RibbonFill(Color color) : this(color, color) { }
}

/// <summary>Immutable named appearance and 96-DPI metrics for one ribbon.</summary>
public sealed record RibbonTheme
{
    /// <summary>Gets the ribbon background color.</summary>
    public Color Background { get; init; } = Color.FromArgb(243, 243, 243);
    /// <summary>Gets the command and popup surface color.</summary>
    public Color Surface { get; init; } = Color.White;
    /// <summary>Gets the separator and outline color.</summary>
    public Color Border { get; init; } = Color.FromArgb(224, 224, 224);
    /// <summary>Gets the enabled text and vector icon color.</summary>
    public Color Foreground { get; init; } = Color.FromArgb(26, 26, 26);
    /// <summary>Gets the group caption color.</summary>
    public Color SecondaryText { get; init; } = Color.FromArgb(97, 97, 97);
    /// <summary>Gets the selected-tab and focus color.</summary>
    public Color Accent { get; init; } = Color.FromArgb(43, 87, 154);
    /// <summary>Gets the hover fill.</summary>
    public RibbonFill Hover { get; init; } = new(Color.FromArgb(230, 230, 230));
    /// <summary>Gets the pressed fill.</summary>
    public RibbonFill Pressed { get; init; } = new(Color.FromArgb(218, 218, 218));
    /// <summary>Gets the checked-command fill.</summary>
    public RibbonFill Checked { get; init; } = new(Color.FromArgb(217, 225, 237));
    /// <summary>Gets the minimum tab height in 96-DPI logical pixels.</summary>
    public int TabHeight { get; init; } = 32;
    /// <summary>Gets the minimum command body height in 96-DPI logical pixels.</summary>
    public int BodyHeight { get; init; } = 104;
    /// <summary>Gets the corner radius in 96-DPI logical pixels.</summary>
    public int CornerRadius { get; init; } = 4;
}
/// <summary>Provides built-in native ribbon themes.</summary>
public static class RibbonThemes
{
    /// <summary>Gets the initial Office 2024 inspired light palette.</summary>
    public static RibbonTheme Office2024Light { get; } = new();
}

/// <summary>Draws a borrowed application icon into device-pixel bounds.</summary>
public interface IRibbonIcon
{
    /// <summary>Draws into the supplied device-pixel bounds without taking ownership of Graphics.</summary>
    void Draw(Graphics graphics, Rectangle bounds, RibbonIconContext context);
}
/// <summary>Describes icon DPI, foreground color and enabled state.</summary>
public readonly record struct RibbonIconContext(int DeviceDpi, Color Foreground, bool Enabled);
/// <summary>Describes device-pixel geometry, palette, font and interaction state for painting.</summary>
public readonly record struct RibbonPaintContext(Graphics Graphics, Rectangle Bounds, RibbonTheme Theme, Font Font, int DeviceDpi, bool Hot = false, bool Pressed = false, bool Focused = false, bool ReserveIcon = false);
