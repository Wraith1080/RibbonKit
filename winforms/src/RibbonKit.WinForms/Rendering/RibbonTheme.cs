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
    /// <summary>Gets the shared notification background fill.</summary>
    public RibbonFill MessageBackground { get; init; } = new(Color.FromArgb(255, 244, 206));
    /// <summary>Gets the shared notification text color.</summary>
    public Color MessageForeground { get; init; } = Color.FromArgb(50, 49, 48);
    /// <summary>Gets the notification frame and row-separator color.</summary>
    public Color MessageBorder { get; init; } = Color.FromArgb(230, 199, 83);
    /// <summary>Gets the default notification icon color.</summary>
    public Color MessageIconForeground { get; init; } = Color.FromArgb(0, 120, 212);
    /// <summary>Gets the minimum notification row height in logical pixels; Touch also applies its larger target minimum. Must be positive.</summary>
    public int MessageMinimumHeight { get; init; } = 34;
    /// <summary>Gets notification content padding in logical pixels. Sides must be nonnegative.</summary>
    public Padding MessageContentPadding { get; init; } = new(12, 4, 8, 4);
    /// <summary>Gets the Office 2024 Backstage navigation rail color. Backstage.NavigationBackgroundOpacity can expose an active window backdrop behind it.</summary>
    public Color BackstageNavigationBackground { get; init; } = Color.FromArgb(245, 244, 243);
    /// <summary>Gets the minimum Backstage navigation font size in 96-DPI logical pixels. Defaults to 14, matching the WPF Modern navigation; larger ribbon fonts are retained. Must be positive and finite. Page content retains the host font.</summary>
    public float BackstageNavigationFontSize { get; init; } = 14;
    /// <summary>Gets the paired background fill for Office 2024 noninteractive gallery section headings.</summary>
    public RibbonFill GalleryHeadingBackground { get; init; } = new(Color.FromArgb(240, 240, 240));
    /// <summary>Gets the Office 2024 text color paired with GalleryHeadingBackground.</summary>
    public Color GalleryHeadingForeground { get; init; } = Color.FromArgb(32, 31, 30);
    /// <summary>Gets the ribbon background color.</summary>
    public Color Background { get; init; } = Color.White;
    /// <summary>Gets the command and popup surface color.</summary>
    public Color Surface { get; init; } = Color.White;
    /// <summary>Gets the separator and outline color.</summary>
    public Color Border { get; init; } = Color.FromArgb(224, 224, 224);
    /// <summary>Gets the outline color of floating ribbon popup windows.</summary>
    public Color PopupBorder { get; init; } = Color.FromArgb(191, 191, 191);
    /// <summary>Gets the Office popup corner radius in 96-DPI logical pixels; zero gives square corners.</summary>
    public int PopupCornerRadius { get; init; } = 6;
    /// <summary>Gets whether floating popups use the renderer's soft shadow, subject to the Windows shadow preference.</summary>
    public bool PopupShadowEnabled { get; init; } = true;
    /// <summary>Gets the popup shadow color and maximum opacity; its default matches BodyShadowColor.</summary>
    public Color PopupShadowColor { get; init; } = Color.FromArgb(24, 0, 0, 0);
    /// <summary>Gets the Office ribbon body's shadow color, including its maximum opacity. Transparent disables it.</summary>
    public Color BodyShadowColor { get; init; } = Color.FromArgb(24, 0, 0, 0);
    /// <summary>Gets the ribbon body margins in 96-DPI logical pixels. The content viewport and hit targets respect them.</summary>
    public Padding BodyMargin { get; init; } = new(7, 1, 7, 7);
    /// <summary>Gets the body scroll buttons' top and bottom inset in 96-DPI logical pixels. Defaults to 2 to match the side clearance; zero gives full-height buttons. Must be nonnegative; layout clamps it to half the body height.</summary>
    public int BodyScrollButtonVerticalInset { get; init; } = 2;
    /// <summary>Gets the minimize/modal-close visual inset on each side of their header slots, in 96-DPI logical pixels. Defaults to 4; must be nonnegative and clamps to half the slot's smaller dimension. Full measured hit areas remain available.</summary>
    public int HeaderButtonInset { get; init; } = 4;
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
    /// <summary>Gets QAT hover/pressed/checked and header utility hover/pressed background coverage over an active window backdrop, from 0 to 1. Solid and popup presentations keep their authored fills. Text and icons retain their opacity.</summary>
    public float QuickAccessBackdropOpacity { get; init; } = .6f;
    /// <summary>Gets the minimum tab height in 96-DPI logical pixels.</summary>
    public int TabHeight { get; init; } = 40;
    /// <summary>Gets the minimum command body height in 96-DPI logical pixels.</summary>
    public int BodyHeight { get; init; } = 110;
    /// <summary>Gets the maximum total width of a Large button in 96-DPI logical pixels; must be at least 60. Captions wrap to two rows and ellipsize within this width.</summary>
    public int LargeButtonMaxWidth { get; init; } = 108;
    /// <summary>Gets the maximum collapsed group frame width in 96-DPI logical pixels, excluding its one-device-pixel divider. Captions grow from the density minimum, then wrap/ellipsize. Must be at least 80.</summary>
    public int CollapsedGroupMaxWidth { get; init; } = 108;
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
public readonly record struct RibbonPaintContext(Graphics Graphics, Rectangle Bounds, RibbonTheme Theme, Font Font, int DeviceDpi, bool Hot = false, bool Pressed = false, bool Focused = false, bool ReserveIcon = false)
{
    /// <summary>Gets whether this frame's top edge adjoins the preceding ribbon/QAT surface and uses square top corners.</summary>
    public bool IsTopEdgeConnected { get; init; }
    /// <summary>Gets whether this frame's bottom edge adjoins open messages and uses square bottom corners.</summary>
    public bool IsBottomEdgeConnected { get; init; }
    /// <summary>Gets the owning ribbon's density. Standalone paint contexts default to Compact.</summary>
    public RibbonDensity Density { get; init; }
    /// <summary>Gets whether an in-ribbon gallery is represented by its full-height Large dropdown fallback. The source's IsInRibbon and SizePolicy remain authored values.</summary>
    public bool IsGalleryDropDown { get; init; }
    /// <summary>Gets whether this Quick Access toolbar paint joins an expanded body. DrawBody receives the combined outer frame; this row paints only its internal separator. Minimized standalone rows are false.</summary>
    public bool IsQuickAccessToolbarJoined { get; init; }
    internal bool IsBodyScrollButton { get; init; }
    internal bool CompositedText { get; init; }
    internal Rectangle GroupSeparatorBounds { get; init; }
    internal bool SuppressTabIndicators { get; init; }
    internal bool SuppressShadow { get; init; }
    internal bool IsSimplifiedGroup { get; init; }
}
