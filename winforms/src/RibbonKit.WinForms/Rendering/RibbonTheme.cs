namespace RibbonKit.WinForms;

/// <summary>Identifies the supported built-in native ribbon palettes; custom values remain application-owned through RibbonControl.Theme.</summary>
public enum RibbonBuiltInTheme
{
    /// <summary>The Office 2024 inspired light palette.</summary>
    Office2024Light
}

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
    /// <summary>Gets an optional keyboard-badge fill. Null preserves the renderer's default: pale yellow for Office 2024, or Surface for the neutral renderer.</summary>
    public RibbonFill? KeyTipBackground { get; init; }
    /// <summary>Gets the keyboard-badge text color. Empty preserves the renderer's default: black for Office 2024, or Foreground for the neutral renderer.</summary>
    public Color KeyTipForeground { get; init; } = Color.Empty;
    /// <summary>Gets the keyboard-badge outline color. Empty uses Border; Transparent suppresses the outline.</summary>
    public Color KeyTipBorder { get; init; } = Color.Empty;
    /// <summary>Gets the ribbon background color.</summary>
    public Color Background { get; init; } = Color.White;
    /// <summary>Gets the command and popup surface color.</summary>
    public Color Surface { get; init; } = Color.White;
    /// <summary>Gets the optional opaque ribbon body fill. Null uses Surface. Both explicit stops must be opaque. Does not recolor application-owned controls.</summary>
    public RibbonFill? BodyBackground { get; init; }
    /// <summary>Gets the body frame color. Empty uses Border.</summary>
    public Color BodyBorder { get; init; } = Color.Empty;
    /// <summary>Gets the opaque floating popup background. Empty uses Surface; explicit values must be opaque.</summary>
    public Color PopupBackground { get; init; } = Color.Empty;
    /// <summary>Gets the opaque native editor field background. Empty uses Surface; explicit values must be opaque.</summary>
    public Color EditorBackground { get; init; } = Color.Empty;
    /// <summary>Gets enabled native editor text, label and arrow color. Empty uses Foreground.</summary>
    public Color EditorForeground { get; init; } = Color.Empty;
    /// <summary>Gets the idle native editor frame color. Empty uses Border; active fields retain Accent.</summary>
    public Color EditorBorder { get; init; } = Color.Empty;
    /// <summary>Gets the optional command-menu heading fill. Null preserves the renderer's gallery-heading treatment.</summary>
    public RibbonFill? MenuHeadingBackground { get; init; }
    /// <summary>Gets command-menu heading text color. Empty preserves the renderer's gallery-heading treatment.</summary>
    public Color MenuHeadingForeground { get; init; } = Color.Empty;
    /// <summary>Gets the optional ordinary command-menu minimum row height in 96-DPI logical pixels, from 1 to 1024. Null uses Compact 28 or Touch 44. Font/icon minima still apply; Touch retains at least 44. Includes nested menus, Quick Access overflow and Simplified More Commands; excludes Application Menu-specific and illustrated rows and ComboBox choice lists.</summary>
    public int? MenuMinimumRowHeight { get; init; }
    /// <summary>Gets the optional ordinary command-menu icon slot side in 96-DPI logical pixels, from 1 to 1024. Null uses Compact 24 or Touch 32. Check/radio glyphs retain their density size, capped to the slot.</summary>
    public int? MenuIconSize { get; init; }
    /// <summary>Gets the optional ordinary command-menu leading icon-slot padding in 96-DPI logical pixels, from 0 to 1024. Null uses 8 in either density.</summary>
    public int? MenuLeadingPadding { get; init; }
    /// <summary>Gets the optional ordinary command-menu text/arrow trailing padding in 96-DPI logical pixels, from 0 to 1024. Null uses 8 in either density. Quick Access group-reference rows retain their separate chooser clearance.</summary>
    public int? MenuTrailingPadding { get; init; }
    /// <summary>Gets the optional ordinary command-menu icon-to-text gap in 96-DPI logical pixels, from 0 to 1024. Null uses Compact 10 or Touch 12.</summary>
    public int? MenuIconTextGap { get; init; }
    /// <summary>Gets optional clearance above and below an ordinary menu icon slot in 96-DPI logical pixels, from 0 to 1024. Null uses Compact 4 or Touch 6. Contributes to the row's icon-driven minimum; includes QAT/More overflow rows.</summary>
    public int? MenuIconVerticalPadding { get; init; }
    /// <summary>Gets optional top/bottom padding for ordinary menu title/description stacks in 96-DPI logical pixels, from 0 to 1024. Null uses Compact 5 or Touch 10. Plain rows, native field internals, illustrated choices and Application Menu-specific content retain their own padding.</summary>
    public int? MenuDescriptionVerticalPadding { get; init; }
    /// <summary>Gets the optional ordinary menu title-to-description gap in 96-DPI logical pixels, from 0 to 1024. Null uses Compact 2 or Touch 4. Illustrated choices and Application Menu-specific content retain their own gap.</summary>
    public int? MenuDescriptionGap { get; init; }
    /// <summary>Gets the optional decorative separator row height in ordinary menus and QAT/More overflow, in 96-DPI logical pixels from 1 to 1024. Null uses 9 in either density. The line stays one device pixel and noninteractive; Application Menu-specific separators retain their own geometry.</summary>
    public int? MenuSeparatorHeight { get; init; }
    /// <summary>Gets the optional RibbonForm caption color, overridden by RibbonForm.TitleBarColor. Empty retains Background in solid mode and a clear caption over a backdrop. Explicit values must be opaque.</summary>
    public Color TitleBarBackground { get; init; } = Color.Empty;
    /// <summary>Gets optional caption and TitleBar Quick Access foreground. Empty chooses contrast for a colored caption, or preserves Foreground for an unset caption.</summary>
    public Color TitleBarForeground { get; init; } = Color.Empty;
    internal RibbonFill BodyFill => BodyBackground ?? new(Surface);
    internal Color BodyBorderColor => BodyBorder.IsEmpty ? Border : BodyBorder;
    internal Color PopupColor => PopupBackground.IsEmpty ? Surface : PopupBackground;
    internal Color EditorColor => EditorBackground.IsEmpty ? Surface : EditorBackground;
    internal Color EditorTextColor => EditorForeground.IsEmpty ? Foreground : EditorForeground;
    internal Color EditorBorderColor => EditorBorder.IsEmpty ? Border : EditorBorder;
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
    /// <summary>Gets the Office ribbon body and material Backstage page shadow color, including its maximum opacity. Transparent disables these shadows. Solid Backstage uses Border instead.</summary>
    public Color BodyShadowColor { get; init; } = Color.FromArgb(24, 0, 0, 0);
    /// <summary>Gets the ribbon body margins in 96-DPI logical pixels. The content viewport and hit targets respect them.</summary>
    public Padding BodyMargin { get; init; } = new(7, 1, 7, 7);
    /// <summary>Gets the body scroll buttons' top and bottom inset in 96-DPI logical pixels. Defaults to 2 to match the side clearance; zero gives full-height buttons. Must be nonnegative; layout clamps it to half the body height.</summary>
    public int BodyScrollButtonVerticalInset { get; init; } = 2;
    /// <summary>Gets the minimize/modal-close visual inset on each side of their header slots, in 96-DPI logical pixels. Defaults to 4; must be nonnegative and clamps to half the slot's smaller dimension. Full measured hit areas remain available.</summary>
    public int HeaderButtonInset { get; init; } = 4;
    /// <summary>Gets the enabled text and vector icon color.</summary>
    public Color Foreground { get; init; } = Color.FromArgb(26, 26, 26);
    /// <summary>Gets the color for library-painted disabled text, glyphs and vector command icons. Empty uses SystemColors.GrayText. Does not recolor raster artwork, Windows-drawn text in TextBox/editable ComboBox controls, or application-owned content.</summary>
    public Color DisabledForeground { get; init; } = Color.Empty;
    internal Color DisabledColor => DisabledForeground.IsEmpty ? SystemColors.GrayText : DisabledForeground;
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
    /// <summary>Gets an optional hover/selected-navigation fill over a window backdrop. Null uses Foreground at alpha 24. Applies to header Quick Access/utilities, minimize/maximize caption buttons and Backstage entries; Solid mode and opaque command panels retain Hover.</summary>
    public RibbonFill? BackdropHover { get; init; }
    /// <summary>Gets an optional pressed/open fill over a window backdrop, including minimize/maximize caption buttons. Null uses Foreground at alpha 40. Checked Quick Access entries use Accent at alpha 40 unless this fill is supplied. Solid mode and opaque command panels retain their authored state fills.</summary>
    public RibbonFill? BackdropPressed { get; init; }
    /// <summary>Gets unselected tab/File hover-indicator coverage over a window backdrop, from 0 to 1. Default is .45. Solid mode and selected-tab accents retain their authored coverage.</summary>
    public float BackdropTabHoverOpacity { get; init; } = .45f;
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
    internal bool IsMenuHeading { get; init; }
    internal Layout.RibbonApplicationMenuPart ApplicationMenuPart { get; init; }
    internal bool ApplicationMenuDefaultPane { get; init; }
    internal bool CompositedText { get; init; }
    internal bool WindowBackdrop { get; init; }
    internal Rectangle GroupSeparatorBounds { get; init; }
    internal bool SuppressTabIndicators { get; init; }
    internal bool SuppressShadow { get; init; }
    internal bool IsSimplifiedGroup { get; init; }
    internal bool FileMenuClosing { get; init; }
}
