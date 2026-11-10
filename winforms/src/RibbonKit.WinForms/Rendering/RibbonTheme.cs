namespace RibbonKit.WinForms;

/// <summary>Identifies the supported built-in native ribbon palettes; custom values remain application-owned through RibbonControl.Theme.</summary>
public enum RibbonBuiltInTheme
{
    /// <summary>The Office 2024 inspired light palette.</summary>
    Office2024Light,
    /// <summary>The flat Office 2019 inspired light palette.</summary>
    Office2019Light
}

/// <summary>Identifies the default Office renderer's tab and File presentation.</summary>
public enum RibbonHeaderPresentation
{
    /// <summary>Text with the Office 2024 selection and interaction underline.</summary>
    Underline,
    /// <summary>Square filled headers with a selected tab continuous with the body.</summary>
    Filled
}

/// <summary>Chooses the native Backstage layout and rendering family independently of its palette.</summary>
public enum RibbonBackstageStyle
{
    /// <summary>Office 2024's inset navigation, page frame and separate caption treatment.</summary>
    Modern,
    /// <summary>Office 2019's flat navigation and page treatment.</summary>
    Classic
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
    /// <summary>Gets the default Office renderer's header presentation. Defaults to Underline. Explicit custom renderers retain their drawing policy.</summary>
    public RibbonHeaderPresentation HeaderPresentation { get; init; }
    /// <summary>Gets an optional header hover fill. Null uses Hover for filled headers and preserves the legacy underline presentation.</summary>
    public RibbonFill? HeaderHover { get; init; }
    /// <summary>Gets an optional header pressed/open fill. Null uses Pressed for filled headers.</summary>
    public RibbonFill? HeaderPressed { get; init; }
    /// <summary>Gets inactive header text and glyph color. Empty uses Foreground. Selected filled tabs retain Foreground on Surface.</summary>
    public Color HeaderForeground { get; init; } = Color.Empty;
    /// <summary>Gets whether a supporting RibbonForm extends its active caption color into the tab band. Defaults to false. Does not change authored caption or theme values.</summary>
    public bool ExtendTitleBarColorToHeader { get; init; }
    /// <summary>Gets whether the default Office body draws only its bottom border. Defaults to false; popup borders and shadows are independent.</summary>
    public bool BodyBottomBorderOnly { get; init; }
    /// <summary>Gets the active collapsed Classic/Simplified group opener border. Empty uses Border; Transparent removes it without changing focus or hit targets.</summary>
    public Color CollapsedGroupBorder { get; init; } = Color.Empty;
    /// <summary>Gets whether active Application Menu split rows draw a divider between their unchanged action regions. Defaults to true.</summary>
    public bool ApplicationMenuSplitDividerVisible { get; init; } = true;
    /// <summary>Gets an optional library options-button radius in 96-DPI logical pixels, 0..64. Null preserves system painting for ordinary buttons and existing accent-button chrome; explicit values use shared theme painting with native Button semantics.</summary>
    public int? OptionsButtonCornerRadius { get; init; }
    /// <summary>Gets whether message frames include side borders. Defaults to true. False keeps horizontal rules and existing connected-edge ownership.</summary>
    public bool MessageSideBordersVisible { get; init; } = true;
    /// <summary>Gets whether header scrollers use the effective tab height as their desired width when HeaderScrollButtonWidth is null. Defaults to false. Narrow-strip clamps still apply.</summary>
    public bool HeaderScrollButtonsMatchTabHeight { get; init; }
    /// <summary>Gets an optional header-scroller width in 96-DPI logical pixels, 1..1024. Null uses the density width or opted-in tab height. Touch keeps its density minimum; narrow strips clamp width.</summary>
    public int? HeaderScrollButtonWidth { get; init; }
    /// <summary>Gets an optional header-scroller resting fill. Null uses Surface for explicitly configured full-slot header chrome, or preserves legacy body-scroll drawing otherwise. Body scrollers are independent.</summary>
    public RibbonFill? HeaderScrollButtonBackground { get; init; }
    /// <summary>Gets the header-scroller border. Empty preserves the legacy inset border; an explicit value enables full-slot header chrome and Transparent removes its border. Body scrollers are independent.</summary>
    public Color HeaderScrollButtonBorder { get; init; } = Color.Empty;
    /// <summary>Gets an optional Application Menu work-area clearance in 96-DPI logical pixels, 0..1024. Null retains eight; zero permits alignment with a maximized client edge. Placement and custom-pane resize share it; ordinary menus are unchanged.</summary>
    public int? ApplicationMenuWorkAreaInset { get; init; }
    /// <summary>Gets an optional hover fill for a header following an active caption color. Null uses the caption's contrast-derived state fill.</summary>
    public RibbonFill? ColoredHeaderHover { get; init; }
    /// <summary>Gets an optional pressed/open fill for a header following an active caption color. Null uses the caption's contrast-derived state fill.</summary>
    public RibbonFill? ColoredHeaderPressed { get; init; }
    /// <summary>Gets an optional Office focus-outline corner radius in 96-DPI logical pixels, 0..64. Null preserves the existing command-radius-derived value.</summary>
    public int? FocusCornerRadius { get; init; }
    /// <summary>Gets an optional Office KeyTip corner radius in 96-DPI logical pixels, 0..64. Null preserves the existing two-device-pixel Office radius; neutral badges remain square.</summary>
    public int? KeyTipCornerRadius { get; init; }
    /// <summary>Gets an optional Office checkbox frame corner radius in 96-DPI logical pixels, 0..64. Null preserves two logical pixels; the neutral frame remains square.</summary>
    public int? CheckBoxCornerRadius { get; init; }
    /// <summary>Gets an optional Backstage Back-button frame corner radius in 96-DPI logical pixels, 0..64. Null preserves its circular frame.</summary>
    public int? BackstageBackButtonCornerRadius { get; init; }
    /// <summary>Gets an optional Backstage and options-navigation selection-marker radius in 96-DPI logical pixels, 0..64. Null preserves the half-width radius.</summary>
    public int? NavigationMarkerCornerRadius { get; init; }
    /// <summary>Gets an optional Application Menu footer-button radius in 96-DPI logical pixels, 0..64. Null preserves three logical pixels.</summary>
    public int? ApplicationMenuFooterCornerRadius { get; init; }
    /// <summary>Gets an optional open Application Menu File-button radius in 96-DPI logical pixels, 0..64. Null preserves three logical pixels.</summary>
    public int? ApplicationMenuFileButtonCornerRadius { get; init; }
    /// <summary>Gets an optional Office header-underline corner radius in 96-DPI logical pixels, 0..64. Null preserves the half-height capsule; filled headers have no underline.</summary>
    public int? HeaderIndicatorCornerRadius { get; init; }
    internal Color HeaderTextColor => HeaderForeground.IsEmpty ? Foreground : HeaderForeground;
    /// <summary>Gets the optional group side padding in 96-DPI logical pixels, 0..1024. Null retains expanded 6 / collapsed 3. Larger collapsed padding reserves width beyond the legacy caption cap; Simplified openers use the expanded value.</summary>
    public int? GroupHorizontalPadding { get; init; }
    /// <summary>Gets the optional gap between group columns/explicit-row items in 96-DPI logical pixels, 0..1024. Null retains Compact 3 / Touch 6, and Simplified 3. Menu rows and native field dimensions retain their own geometry.</summary>
    public int? GroupColumnGap { get; init; }
    /// <summary>Gets the optional inner body viewport inset in 96-DPI logical pixels, 0..1024. Null retains Classic 2 / Simplified 4. Insets clamp within available width; reduction and scrolling use the resulting viewport.</summary>
    public int? BodyContentHorizontalPadding { get; init; }
    /// <summary>Gets the optional group-divider clearance at top/bottom in 96-DPI logical pixels, 0..1024. Null retains Classic Office 12 / Simplified 6. Clamps within the frame; neutral Classic keeps its renderer policy.</summary>
    public int? GroupSeparatorVerticalInset { get; init; }
    /// <summary>Gets the optional horizontal padding on each side of tab and File labels in 96-DPI logical pixels, 0..1024. Null retains 14. Measurement, overflow and interaction targets share the resulting width.</summary>
    public int? TabHorizontalPadding { get; init; }
    /// <summary>Gets the optional minimum tab width in 96-DPI logical pixels, 1..1024. Null retains 58. Text/padding requirements and Touch's 44-pixel minimum still apply; File retains its separate 50-pixel minimum.</summary>
    public int? TabMinimumWidth { get; init; }
    /// <summary>Gets the optional Office tab/File indicator label extension and edge clearance in 96-DPI logical pixels, 0..1024. Null retains 4. Bounds clamp within the header; neutral rendering and indicator thickness retain their policies.</summary>
    public int? HeaderIndicatorPadding { get; init; }
    /// <summary>Gets the optional horizontal Quick Access edge padding in 96-DPI logical pixels, 0..1024. Null retains 2. TabRow, BelowRibbon and TitleBar slots clamp available space; source identities and button/row sizes remain.</summary>
    public int? QuickAccessHorizontalPadding { get; init; }
    /// <summary>Gets the optional gap between Quick Access entries and before overflow in 96-DPI logical pixels, 0..1024. Null retains 0. Visible-entry selection, overflow and targets share the spacing; menu projections keep their own geometry.</summary>
    public int? QuickAccessItemGap { get; init; }
    /// <summary>Gets the optional horizontal gap between message text and an inline action in 96-DPI logical pixels, 0..1024. Null retains 10. Action wrapping/targets share the value; stacked-action vertical clearance retains 4.</summary>
    public int? MessageActionGap { get; init; }
    /// <summary>Gets the optional gap between inline message title and text in 96-DPI logical pixels, 0..1024. Null retains 8. Inline-versus-wrapped measurement and painted rectangles share the value.</summary>
    public int? MessageTitleTextGap { get; init; }
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
    /// <summary>Gets the Backstage layout and renderer family. Defaults to Modern; Office 2019 selects Classic. Individual appearance and caption settings remain independently configurable.</summary>
    public RibbonBackstageStyle BackstageStyle { get; init; }
    /// <summary>Gets whether Backstage navigation and its caption tint follow Accent. False preserves BackstageNavigationBackground. Options dialogs retain their neutral navigation background.</summary>
    public bool BackstageNavigationUsesAccent { get; init; }
    /// <summary>Gets whether navigation state fills reach every edge of their measured row. False retains ordinary inset chrome.</summary>
    public bool BackstageFullWidthNavigation { get; init; }
    /// <summary>Gets whether the real Backstage panes extend behind a transparent integrated title bar. False keeps Backstage below the title bar. Enabled by Office 2019 Light.</summary>
    public bool BackstageExtendsIntoTitleBar { get; init; }
    /// <summary>Gets whether an open Backstage centers the integrated window title on the full client width. Enabled by Office 2019 Light; false retains ordinary left-aligned caption placement.</summary>
    public bool BackstageTitleCentered { get; init; }
    /// <summary>Gets the animated fraction of the title's total distance on Backstage opening and closing, from 0 to 1. Null uses the final 25 percent after immediate placement 75 percent toward the destination. Zero snaps; one animates the full distance. The title never fades. Must be finite.</summary>
    public double? BackstageTitleSlideDistanceRatio { get; init; }
    /// <summary>Gets optional Backstage slide travel as a fraction of its width, from 0 to 1. Null retains 15 percent travel. Zero disables travel while keeping the configured fade. Must be finite.</summary>
    public double? BackstageSlideDistanceRatio { get; init; }
    /// <summary>Gets navigation and integrated Backstage caption ink. Empty retains Foreground and existing caption behavior. Native application pages keep their own colors.</summary>
    public Color BackstageNavigationForeground { get; init; } = Color.Empty;
    /// <summary>Gets selected navigation ink. Empty retains Accent.</summary>
    public Color BackstageSelectedForeground { get; init; } = Color.Empty;
    /// <summary>Gets disabled navigation ink. Empty retains DisabledColor.</summary>
    public Color BackstageDisabledForeground { get; init; } = Color.Empty;
    /// <summary>Gets an optional navigation hover fill. Null retains Hover, or an Accent-to-white 22 percent mix when BackstageNavigationUsesAccent is true.</summary>
    public RibbonFill? BackstageNavigationHover { get; init; }
    /// <summary>Gets an optional selected/pressed navigation fill. Null retains Hover, or an Accent-to-black 28 percent mix when BackstageNavigationUsesAccent is true.</summary>
    public RibbonFill? BackstageNavigationSelected { get; init; }
    /// <summary>Gets whether selected Backstage rows draw the accent indicator. Defaults to true.</summary>
    public bool BackstageSelectionIndicatorVisible { get; init; } = true;
    /// <summary>Gets whether Backstage icons and images use their alpha silhouette tinted with navigation ink. False retains authored colors; true matches the WPF Classic navigation mask.</summary>
    public bool BackstageMonochromeIcons { get; init; }
    /// <summary>Gets the optional logical navigation icon left inset, 0..1024. Null retains 16. Text placement and Auto width use the same value.</summary>
    public int? BackstageNavigationIconInset { get; init; }
    /// <summary>Gets the optional logical navigation icon size, 1..1024. Null retains 16. Text placement and Auto width use the same value.</summary>
    public int? BackstageNavigationIconSize { get; init; }
    /// <summary>Gets the optional Auto navigation width starting minimum in logical pixels, 1..1024. Null retains 200. Captions, authored width constraints and available page space still apply.</summary>
    public int? BackstageAutoMinimumNavigationWidth { get; init; }
    /// <summary>Gets the optional logical footer bottom clearance, 0..1024. Null uses BackstageNavigationPadding. Short hosts still use inline scrolling.</summary>
    public int? BackstageFooterBottomPadding { get; init; }
    /// <summary>Gets the optional logical top/left page border width, 0..32. Null retains each renderer's existing border. Zero keeps page content flush; layout and painting share it.</summary>
    public int? BackstagePageBorderWidth { get; init; }
    internal Color BackstageNavigationColor => BackstageNavigationUsesAccent ? Accent : BackstageNavigationBackground;
    internal Color BackstageNavigationInk => BackstageNavigationForeground.IsEmpty ? Foreground : BackstageNavigationForeground;
    internal RibbonTheme BackstageCaptionTheme => BackstageNavigationForeground.IsEmpty ? this : this with
    {
        Foreground = BackstageNavigationInk, SecondaryText = BackstageNavigationInk,
        Hover = BackstageHoverFill, Pressed = BackstageSelectedFill,
        BackdropHover = BackstageHoverFill, BackdropPressed = BackstageSelectedFill
    };
    internal RibbonFill BackstageHoverFill => BackstageNavigationHover ?? (BackstageNavigationUsesAccent ? new(MixBackstageAccent(Color.White, .22)) : Hover);
    internal RibbonFill BackstageSelectedFill => BackstageNavigationSelected ?? (BackstageNavigationUsesAccent ? new(MixBackstageAccent(Color.Black, .28)) : Hover);
    private Color MixBackstageAccent(Color target, double amount) => Color.FromArgb(
        (int)Math.Round(Accent.R * (1 - amount) + target.R * amount),
        (int)Math.Round(Accent.G * (1 - amount) + target.G * amount),
        (int)Math.Round(Accent.B * (1 - amount) + target.B * amount));
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
    /// <summary>Gets the optional native editor label-to-field gap in 96-DPI logical pixels, from 0 to 1024. Null uses 6. Hidden ComboBox labels reserve no gap.</summary>
    public int? EditorLabelGap { get; init; }
    /// <summary>Gets the optional horizontal native field text inset in 96-DPI logical pixels, from 0 to 1024. Null uses 4. Text is clipped to available field space; authored field widths and heights are retained.</summary>
    public int? EditorTextInset { get; init; }
    /// <summary>Gets the optional native TextBox/ComboBox field minimum height in 96-DPI logical pixels, from 1 to 1024. Null preserves the density/font minimum. Font padding still applies; Touch retains at least 42, or 44 for an inline Simplified ComboBox. Containing rows grow to fit. Does not change authored input widths.</summary>
    public int? EditorMinimumHeight { get; init; }
    /// <summary>Gets optional padding above and below native field text in 96-DPI logical pixels, from 0 to 1024. Null preserves scaled summed padding of 8. Explicit values scale each side separately. Font size stays unchanged; text and labels remain vertically centered.</summary>
    public int? EditorVerticalPadding { get; init; }
    /// <summary>Gets the optional native ComboBox button width in 96-DPI logical pixels, from 1 to 1024. Null uses Compact 24 or Touch 32. Touch retains at least 32; the slot is capped to the field width. Excludes Small command projections and menu-reference arrows.</summary>
    public int? ComboBoxButtonWidth { get; init; }
    /// <summary>Gets the optional native ComboBox choice-row minimum height in 96-DPI logical pixels, from 1 to 1024. Null uses Compact 28 or Touch 44. Font/padding minima and the Touch floor of 44 apply, capped at the native list limit of 255 device pixels.</summary>
    public int? ComboBoxMinimumChoiceHeight { get; init; }
    /// <summary>Gets optional padding above and below native ComboBox choice text in 96-DPI logical pixels, from 0 to 1024. Null uses 5 with legacy summed rounding. Explicit values scale each side separately; native choice rows are capped at 255 device pixels.</summary>
    public int? ComboBoxChoiceVerticalPadding { get; init; }
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
    /// <summary>Gets the optional desired minimum dropdown, Quick Access overflow and More Commands width in 96-DPI logical pixels, from 1 to 1024. Null uses Compact 170 or Touch 220. Content minima and work-area limits still apply; excludes Application Menu, custom content and native choice lists.</summary>
    public int? MenuMinimumWidth { get; init; }
    /// <summary>Gets the optional desired minimum menu width when command descriptions are present, in 96-DPI logical pixels from 1 to 1024. Null uses Compact 280 or Touch 320. Content and work-area limits still apply.</summary>
    public int? MenuDescriptionMinimumWidth { get; init; }
    /// <summary>Gets optional horizontal padding on each side of a dropdown heading in 96-DPI logical pixels, from 0 to 1024. Null uses 6. Gallery and Application Menu headings retain their own geometry.</summary>
    public int? MenuHeaderHorizontalPadding { get; init; }
    /// <summary>Gets optional padding above and below dropdown heading text in 96-DPI logical pixels, from 0 to 1024. Null uses Compact 6 or Touch 8. Font height remains the content minimum; Application Menu headings retain their own geometry.</summary>
    public int? MenuHeaderVerticalPadding { get; init; }
    /// <summary>Gets the optional gap below a dropdown heading in 96-DPI logical pixels, from 0 to 1024. Null uses 4. Menus without a heading reserve no gap.</summary>
    public int? MenuHeaderBottomGap { get; init; }
    /// <summary>Gets the optional height of Quick Access and More Commands overflow scroll-button bands in 96-DPI logical pixels, from 1 to 1024. Null uses Compact 16 or Touch 28. Touch retains at least 28; small viewports clamp the bands to preserve content space. Native system scrollbars are unchanged.</summary>
    public int? MenuOverflowScrollButtonHeight { get; init; }
    /// <summary>Gets the optional minimum caption reservation before ordinary submenu indicators in 96-DPI logical pixels, from 1 to 1024. Null uses Compact 16 or Touch 28. The shared split-arrow column can reserve additional space; glyphs align in that column. Excludes strip, Application Menu-specific and native choice-list arrows.</summary>
    public int? MenuSubmenuArrowWidth { get; init; }
    /// <summary>Gets the optional desired ordinary-menu and Quick Access/More overflow split secondary width and shared visual indicator-column width in 96-DPI logical pixels, from 1 to 1024. Null uses Compact 20 or Touch 28. Touch retains at least 28; narrow rows retain the existing one-third cap.</summary>
    public int? MenuSplitArrowWidth { get; init; }
    /// <summary>Gets the optional horizontal command split secondary width in 96-DPI logical pixels, from 1 to 1024. Null preserves Compact 20 or Touch 44 and existing preferred sizing. Touch retains at least 44. Explicit geometry reserves content and the existing half/third cap budget. Excludes menus, QAT projection rows and Large vertical splits.</summary>
    public int? CommandSplitArrowWidth { get; init; }
    /// <summary>Gets the optional Small/Medium horizontal command split primary minimum width in 96-DPI logical pixels, from 0 to 1024. Null retains existing layout sizing: Classic Compact 0 / Touch 56; Simplified keeps its current content-based width. Explicit geometry keeps Touch's 56 floor and the existing arrow cap. Reduced vertical commands use their horizontal presentation.</summary>
    public int? CommandSplitPrimaryMinimumWidth { get; init; }
    /// <summary>Gets the optional Large horizontal command split primary minimum width in 96-DPI logical pixels, from 0 to 1024. Null retains Compact 0 / Touch 72 and existing caption sizing. Explicit geometry keeps Touch's 72 floor and arrow cap. Large vertical and menu/QAT presentations retain their own geometry.</summary>
    public int? CommandLargeSplitPrimaryMinimumWidth { get; init; }
    /// <summary>Gets optional symmetric Small/Medium command content padding in 96-DPI logical pixels, from 0 to 1024. Null preserves 5 and existing optical icon offsets. Larger values reserve width; smaller values retain density minima. Excludes Large commands, menus, QAT projections, native editors and galleries.</summary>
    public int? CommandContentHorizontalPadding { get; init; }
    /// <summary>Gets optional minimum clearance above/below Small/Medium command text and 16-pixel artwork in 96-DPI logical pixels, from 0 to 1024. Null preserves existing rows and the intrinsic 3-pixel text inset. Explicit values grow containing rows/bands as needed; density/font minima remain. Values above 3 also center shared Large icon/two-caption-row/arrow bands, reserving unused bands and synchronizing vertical split regions.</summary>
    public int? CommandContentVerticalPadding { get; init; }
    /// <summary>Gets the optional Application Menu navigation-row minimum height in 96-DPI logical pixels, from 1 to 1024. Null retains Compact 40 / Touch 44 and existing native-editor minima. Content minima still apply; Touch retains 44.</summary>
    public int? ApplicationMenuNavigationMinimumRowHeight { get; init; }
    /// <summary>Gets the optional Application Menu right-pane row minimum height in 96-DPI logical pixels, from 1 to 1024. Null retains Compact 28 / Touch 44 and existing illustrated/editor content minima. Touch retains 44.</summary>
    public int? ApplicationMenuPaneMinimumRowHeight { get; init; }
    /// <summary>Gets the optional Application Menu footer-row minimum height in 96-DPI logical pixels, from 1 to 1024. Null uses Compact 30 / Touch 44. Font/icon/editor minima and the Touch floor of 44 still apply.</summary>
    public int? ApplicationMenuFooterMinimumRowHeight { get; init; }
    /// <summary>Gets the optional Application Menu pane-content/default-heading horizontal inset in 96-DPI logical pixels, from 0 to 1024. Null preserves 10. Larger insets reserve desired width and extend navigation by the excess over 10; explicit values clamp within the available pane. Active-pane title bands keep their full width.</summary>
    public int? ApplicationMenuPaneHorizontalPadding { get; init; }
    /// <summary>Gets the optional gap between Application Menu footer items and wrapped footer rows in 96-DPI logical pixels, from 0 to 1024. Null preserves 6. Footer scrolling and work-area limits remain.</summary>
    public int? ApplicationMenuFooterItemGap { get; init; }
    /// <summary>Gets the optional minimum caption reservation before Quick Access/More overflow menu, ComboBox and group chooser indicators in 96-DPI logical pixels, from 1 to 1024. Null uses 16 in both densities. The shared split-arrow column can reserve additional space; glyphs align in that column. QAT strip buttons and native choice lists retain their own geometry.</summary>
    public int? MenuChooserArrowWidth { get; init; }
    /// <summary>Gets optional horizontal padding at each end of ordinary-menu and Quick Access/More separator lines in 96-DPI logical pixels, from 0 to 1024. Null uses 8. Lines stay one device pixel; Application Menu-specific and group separators retain their own geometry.</summary>
    public int? MenuSeparatorHorizontalPadding { get; init; }
    /// <summary>Gets the optional illustrated-choice menu desired minimum width in 96-DPI logical pixels, from 1 to 1024. Null uses 300 in both densities. Content/header minima and work-area limits still apply. Excludes Application Menu-specific and QAT/More projection rows.</summary>
    public int? IllustratedMenuMinimumWidth { get; init; }
    /// <summary>Gets the optional illustrated-choice row minimum height in 96-DPI logical pixels, from 1 to 1024. Null uses 64 in both densities. Font/artwork minima still apply; Touch retains at least 44. Mixed menu rows use the tallest sibling.</summary>
    public int? IllustratedMenuMinimumRowHeight { get; init; }
    /// <summary>Gets the optional illustrated-choice artwork slot side in 96-DPI logical pixels, from 1 to 1024. Null uses 36. Explicit illustrated geometry keeps artwork within constrained rows and grows the row's artwork minimum.</summary>
    public int? IllustratedMenuIconSize { get; init; }
    /// <summary>Gets the optional illustrated-choice leading artwork padding in 96-DPI logical pixels, from 0 to 1024. Null uses 8.</summary>
    public int? IllustratedMenuLeadingPadding { get; init; }
    /// <summary>Gets the optional illustrated-choice trailing text padding in 96-DPI logical pixels, from 0 to 1024. Null uses 8.</summary>
    public int? IllustratedMenuTrailingPadding { get; init; }
    /// <summary>Gets the optional illustrated-choice artwork-to-text gap in 96-DPI logical pixels, from 0 to 1024. Null uses 12.</summary>
    public int? IllustratedMenuIconTextGap { get; init; }
    /// <summary>Gets the optional illustrated-choice title-to-description gap in 96-DPI logical pixels, from 0 to 1024. Null uses 2 in both densities. Measurement and painting retain the existing two-line description and font sizing.</summary>
    public int? IllustratedMenuDescriptionGap { get; init; }
    /// <summary>Gets the optional RibbonForm caption color, overridden by RibbonForm.TitleBarColor. Empty retains Background in solid mode and a clear caption over a backdrop. Explicit values must be opaque.</summary>
    public Color TitleBarBackground { get; init; } = Color.Empty;
    /// <summary>Gets optional caption and TitleBar Quick Access foreground. Empty chooses contrast for a colored caption, or preserves Foreground for an unset caption.</summary>
    public Color TitleBarForeground { get; init; } = Color.Empty;
    /// <summary>Gets the optional container and inline viewport/utility gap in 96-DPI logical pixels, from 0 to 1024. Null uses 4. Resets independently; available bounds clamp spacing.</summary>
    public int? GalleryContentPadding { get; init; }
    /// <summary>Gets the optional inline utility width in 96-DPI logical pixels, from 1 to 1024. Null uses Compact 18 / Touch 32; Touch retains 32. Resets independently; available bounds clamp spacing.</summary>
    public int? GalleryUtilityWidth { get; init; }
    /// <summary>Gets the optional built-in tile content padding; application presenter internals remain host-owned in 96-DPI logical pixels, from 0 to 1024. Null uses 4. Resets independently; available bounds clamp spacing.</summary>
    public int? GalleryTilePadding { get; init; }
    /// <summary>Gets the optional gallery heading horizontal padding in 96-DPI logical pixels, from 0 to 1024. Null uses 6. Resets independently; available bounds clamp spacing.</summary>
    public int? GalleryHeadingHorizontalPadding { get; init; }
    /// <summary>Gets the optional gallery heading padding above and below text in 96-DPI logical pixels, from 0 to 1024. Null uses 6. Resets independently; available bounds clamp spacing.</summary>
    public int? GalleryHeadingVerticalPadding { get; init; }
    /// <summary>Gets the optional Backstage row minimum; font minima remain in 96-DPI logical pixels, from 1 to 1024. Null uses Compact 42 / Touch 52; Touch retains 52. Resets independently; available bounds clamp spacing.</summary>
    public int? BackstageMinimumRowHeight { get; init; }
    /// <summary>Gets the optional Backstage row side and footer-bottom padding in 96-DPI logical pixels, from 0 to 1024. Null uses 8. Resets independently; available bounds clamp spacing.</summary>
    public int? BackstageNavigationPadding { get; init; }
    /// <summary>Gets the optional Backstage Back button side in 96-DPI logical pixels, from 1 to 1024. Null uses Compact 36 / Touch 44; Touch retains 44. Resets independently; available bounds clamp spacing.</summary>
    public int? BackstageBackButtonSize { get; init; }
    /// <summary>Gets the optional Backstage Back button top and left inset in 96-DPI logical pixels, from 0 to 1024. Null uses 20. Resets independently; available bounds clamp spacing.</summary>
    public int? BackstageBackButtonInset { get; init; }
    /// <summary>Gets the optional gap below the Backstage Back button in 96-DPI logical pixels, from 0 to 1024. Null uses 24. Resets independently; available bounds clamp spacing.</summary>
    public int? BackstageBackButtonBottomGap { get; init; }
    /// <summary>Gets the optional Backstage navigation icon-to-text gap in 96-DPI logical pixels, from 0 to 1024. Null uses 12. Resets independently; available bounds clamp spacing.</summary>
    public int? BackstageIconTextGap { get; init; }
    /// <summary>Gets the optional padding inside the Backstage page frame; native content layout remains host-owned in 96-DPI logical pixels, from 0 to 1024. Null uses 0. Resets independently; available bounds clamp spacing.</summary>
    public int? BackstagePagePadding { get; init; }
    /// <summary>Gets the optional maximum passive ScreenTip outer width in 96-DPI logical pixels, from 1 to 1024. Null uses 320. Resets independently; available-space limits remain.</summary>
    public int? ScreenTipMaximumWidth { get; init; }
    /// <summary>Gets the optional padding on each side of passive ScreenTip content in 96-DPI logical pixels, from 0 to 1024. Null uses 12. Resets independently; available-space limits remain.</summary>
    public int? ScreenTipContentPadding { get; init; }
    /// <summary>Gets the optional passive ScreenTip title-to-description gap in 96-DPI logical pixels, from 0 to 1024. Null uses 6. Resets independently; available-space limits remain.</summary>
    public int? ScreenTipTitleGap { get; init; }
    /// <summary>Gets the optional passive ScreenTip clearance from the anchor and pointer in 96-DPI logical pixels, from 0 to 1024. Null uses 8. Resets independently; available-space limits remain.</summary>
    public int? ScreenTipPlacementGap { get; init; }
    /// <summary>Gets the optional horizontal keyboard-badge padding on each side in 96-DPI logical pixels, from 0 to 1024. Null uses the existing root/menu size or Backstage 4; null retains summed DPI rounding. Resets independently; available-space limits remain.</summary>
    public int? KeyTipHorizontalPadding { get; init; }
    /// <summary>Gets the optional vertical keyboard-badge padding on each side in 96-DPI logical pixels, from 0 to 1024. Null uses the existing root/menu size or Backstage 2; null retains summed DPI rounding. Resets independently; available-space limits remain.</summary>
    public int? KeyTipVerticalPadding { get; init; }
    /// <summary>Gets the optional RibbonForm caption leading padding in 96-DPI logical pixels, from 0 to 1024. Null uses 10 with an icon or 8 without an icon. Resets independently; available-space limits remain.</summary>
    public int? CaptionLeadingPadding { get; init; }
    /// <summary>Gets the optional RibbonForm caption icon-to-content gap in 96-DPI logical pixels, from 0 to 1024. Null uses 10. Resets independently; available-space limits remain.</summary>
    public int? CaptionIconTextGap { get; init; }
    /// <summary>Gets the optional RibbonForm TitleBar QAT-to-title gap in 96-DPI logical pixels, from 0 to 1024. Null uses 10. Resets independently; available-space limits remain.</summary>
    public int? CaptionQuickAccessTextGap { get; init; }
    /// <summary>Gets the optional RibbonForm title-width reservation beside TitleBar QAT, before the QAT-to-title gap in 96-DPI logical pixels, from 1 to 1024. Null uses 100. Resets independently; available-space limits remain.</summary>
    public int? CaptionTitleReservationWidth { get; init; }
    /// <summary>Gets the optional Office body/connected QAT/message panel corner radius in 96-DPI logical pixels, from 0 to 64. Null uses twice CornerRadius with legacy rounding; neutral stays square. Resets independently; existing renderer and native space limits remain.</summary>
    public int? BodyCornerRadius { get; init; }
    /// <summary>Gets the optional Office Backstage top-left page corner radius in 96-DPI logical pixels, from 0 to 64. Null uses twice CornerRadius; neutral stays square. Resets independently; existing renderer and native space limits remain.</summary>
    public int? BackstagePageCornerRadius { get; init; }
    /// <summary>Gets the optional Office body/connected-panel shadow blur reach in 96-DPI logical pixels, from 0 to 32. Null uses 6; zero suppresses the shadow. Resets independently; existing renderer and native space limits remain.</summary>
    public int? BodyShadowBlur { get; init; }
    /// <summary>Gets the optional Office body/connected-panel shadow vertical offset in 96-DPI logical pixels, from -32 to 32. Null uses 2; positive moves downward. Resets independently; existing renderer and native space limits remain.</summary>
    public int? BodyShadowVerticalOffset { get; init; }
    /// <summary>Gets the optional floating-popup shadow blur reach in 96-DPI logical pixels, from 0 to 32. Null uses 6; zero suppresses the shadow. Resets independently; existing renderer and native space limits remain.</summary>
    public int? PopupShadowBlur { get; init; }
    /// <summary>Gets the optional floating-popup shadow vertical offset in 96-DPI logical pixels, from -32 to 32. Null uses 2; positive moves downward. Resets independently; existing renderer and native space limits remain.</summary>
    public int? PopupShadowVerticalOffset { get; init; }
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
    /// <summary>Gets the flat Office 2019 light theme with filled headers, square chrome, full-width body and bordered, shadowed popups.</summary>
    public static RibbonTheme Office2019Light { get; } = new()
    {
        HeaderPresentation = RibbonHeaderPresentation.Filled, ExtendTitleBarColorToHeader = true,
        BodyBottomBorderOnly = true, Background = Color.FromArgb(230, 230, 230),
        CollapsedGroupBorder = Color.Transparent, ApplicationMenuSplitDividerVisible = false, OptionsButtonCornerRadius = 0,
        MessageSideBordersVisible = false, HeaderScrollButtonsMatchTabHeight = true,
        ApplicationMenuWorkAreaInset = 0,
        BackstageStyle = RibbonBackstageStyle.Classic,
        BackstageNavigationUsesAccent = true, BackstageFullWidthNavigation = true,
        BackstageExtendsIntoTitleBar = true, BackstageTitleCentered = true,
        BackstageNavigationForeground = Color.White, BackstageSelectedForeground = Color.White,
        BackstageDisabledForeground = Color.FromArgb(180, 195, 215), BackstageSelectionIndicatorVisible = false, BackstageMonochromeIcons = true,
        BackstageNavigationPadding = 0, BackstageNavigationIconInset = 26, BackstageAutoMinimumNavigationWidth = 220,
        BackstageFooterBottomPadding = 14, BackstagePageBorderWidth = 0,
        Surface = Color.White, Foreground = Color.FromArgb(32, 31, 30), SecondaryText = Color.FromArgb(96, 94, 92),
        Border = Color.FromArgb(200, 198, 196), PopupBorder = Color.FromArgb(200, 198, 196),
        Hover = new(Color.FromArgb(222, 220, 218)), Pressed = new(Color.FromArgb(207, 205, 203)),
        Checked = new(Color.FromArgb(206, 220, 240)),
        HeaderHover = new(Color.FromArgb(237, 237, 237)), HeaderPressed = new(Color.FromArgb(216, 216, 216)),
        CornerRadius = 0, PopupCornerRadius = 0, BodyCornerRadius = 0, BackstagePageCornerRadius = 0,
        FocusCornerRadius = 0, KeyTipCornerRadius = 0, CheckBoxCornerRadius = 0,
        NavigationMarkerCornerRadius = 0, ApplicationMenuFooterCornerRadius = 0, ApplicationMenuFileButtonCornerRadius = 0,
        HeaderIndicatorCornerRadius = 0,
        BodyMargin = Padding.Empty, BodyShadowColor = Color.Transparent,
        GalleryHeadingBackground = new(Color.FromArgb(243, 242, 241)), GalleryHeadingForeground = Color.FromArgb(32, 31, 30)
    };
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
    internal int? ApplicationMenuDividerX { get; init; }
    internal bool ApplicationMenuDefaultPane { get; init; }
    internal bool CompositedText { get; init; }
    internal bool WindowBackdrop { get; init; }
    internal Rectangle GroupSeparatorBounds { get; init; }
    internal bool SuppressTabIndicators { get; init; }
    internal bool SuppressShadow { get; init; }
    internal bool IsSimplifiedGroup { get; init; }
    internal bool FileMenuClosing { get; init; }
}
