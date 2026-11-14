# RibbonKit.WinForms native preview

A separate native Windows Forms implementation, developed in `winforms/` on the
user's `codex/Ribbonkit-Winform` branch. WPF v1.1.0 is the behavioral reference.
This is an unpublished preview. The [M1 foundation ledger](docs/M1-STATUS.md)
records its remaining review; [M2](docs/M2-STATUS.md) adds
split buttons and native TextBox/ComboBox editing. The current [M3 slice](docs/M3-STATUS.md)
adds button/toggle Quick Access, checkbox/radio icon references, ComboBox choosers
and dropdown/split/group references.
The current [M4 slice](docs/M4-STATUS.md) adds contextual-tab header colors,
the two-pane Application Menu, native Backstage pages, dropdown/in-ribbon/QAT galleries
and source-owned tab/group merging with modal scopes and Compact/Touch density. [Remaining slices](docs/M4-STATUS.md#remaining-slices)
record the working sequence through galleries, customization and window integration.
The [M5 implementation](docs/M5-STATUS.md) adds shared catalog discovery, detached
Ribbon/QAT drafts, versioned JSON, Apply/Cancel checkpoints and an extensible native
Options dialog with Customize Ribbon and Quick Access Toolbar pages.
The Showcase supplies an original [SVG command/group icon set](samples/RibbonKit.WinForms.Showcase/Assets/CommandIcons/README.md).
`RibbonGroup.Icon` accepts a borrowed `IRibbonIcon` for collapsed group buttons,
QAT group entries and customization lists, using the same vector contract as commands.
The library also supplies immutable `RibbonSvgIcon` artwork and a curated
`RibbonStockIcons` catalog with 512 regular icons. In Edit Ribbon, select a
group/command and choose
**Icon â†’ Choose iconâ€¦** to search stock SVGs or import project artwork. SVG source
is embedded for save/reopen; original colors are retained unless **Use ribbon
color** is selected. Stock icons opt into foreground/disabled tinting. Existing
Image/LargeImage fallback remains available when a vector icon is cleared.
See [SVG icon authoring](docs/SVG-ICON-AUTHORING.md) for supported artwork,
serialization, dependency notices, code examples and pending actual-VS review.
The [Simplified Ribbon implementation](docs/M5D-SIMPLIFIED-RIBBON.md) adds a single
command row, More Commands/full-group access and a grouped customization page.
Classic and Simplified share tab/group structure; per-tab inline selections and
QAT preferences apply independently. In Showcase, choose **Options â†’ Show â†’
Simplified Ribbon**. Paste, Copy, Cut, Bold, Italic and representative commands have
authored inline defaults. Human visual review remains pending.
The [first M6A motion pass](docs/M6A-MOTION-PLAN.md) adds opt-in layout, density,
QAT travel and expanded tab switching. Showcase opts into Subtle; the library
retains immediate defaults.
Accessibility compatibility is deferred
while the control structure is implemented; it remains possible future work.

Requires Windows and the .NET 10 SDK selected by `global.json`. Open
`RibbonKit.WinForms.sln` for native development. From the parent checkout:

```powershell
dotnet run --project winforms/samples/RibbonKit.WinForms.Showcase
```

Resize the ordinary Form, open a collapsed group, or use Alt/F10 and F6. The
sample's document editor, icons, handlers and BindingSource belong to the application.
The Inputs tab demonstrates library-owned native ribbon editors.

Set `RibbonControl.LayoutMode` to `RibbonLayoutMode.Simplified` to use the row.
Commands opt into authored defaults through `SimplifiedPlacement = Inline`;
`SimplifiedSize` accepts Small (icon) or Medium (icon and label), and higher
`SimplifiedPriority` survives longer when space is limited. Classic remains the
default; layout mode and Compact/Touch density are independent. The host owns
appearance persistence. More Commands keeps displaced commands and every visible
group accessible, including native editors that are not supported inline.
It sits at the body's right inset when the row fits. While a full-group popup
borrows its controls, that group's inline commands become one medium group button;
closing it restores the live commands and their latest values. This temporary
presentation does not alter customization. Simplified inline ComboBox fields use
a slightly taller minimum within the existing row.

Custom groups have a shared name/icon/layout dialog in both customization pages.
`RibbonCustomizationGroup.IconCommandId` borrows artwork from a catalog command;
null selects None. `Layout` chooses `RibbonCustomizationGroupLayout.Stacked`
(Medium/Small columns) or `Large` (Large commands in a row). Stacked is the default,
including older profiles without a layout field and groups created in Simplified
mode. Import/export and Apply normalize sizes consistently; original commands and
application-owned artwork remain unchanged.

`ShowRibbonCustomization()` opens the page for the active mode. Simplified uses
independent source/target tab selectors, grouped lists, Add/Remove/reorder, Show
Label, New/Edit and Reset actions, with only the Options footer's **OK, Cancel,
Apply** buttons. Structural changes synchronize with Classic after Apply; switching
modes preserves pending page drafts. `RibbonCustomizationTarget.Simplified` scopes
a transaction to shared structure and Simplified pins, preserving live QAT settings.
Native version-three JSON adds optional `Simplified` tabs with ordered source
command IDs and sizes. Version-one/two input remains readable; absent sections
preserve live settings. Files, appearance mode, density and command values remain
application-owned.

The foundation includes tabs, groups, buttons, two-state toggles and dropdowns;
three group reduction modes, fixed/adaptive command sizes, explicit priorities,
tab/body scrolling, collapsed flyouts, keyboard focus/KeyTips and a native UIA
provider. Neighbouring medium rows share column widths, icon slots and text
origins; large labels share a first-line origin. Layout and rendering consume
the same device-pixel rectangles, with font-aware row/caption metrics.
Large regular buttons, toggles, dropdowns and both split arrangements share a
compact two-line caption layout. Native text retains its font size and first-line
origin; `DrawText` receives each measured line separately. Captions use tighter
baseline spacing and three logical pixels of bottom clearance. Large buttons grow
up to `RibbonTheme.LargeButtonMaxWidth` (108 logical pixels by default), wrap to at
most two rows and end the last row with an ellipsis when needed. The original
`Text` remains intact and supplies the default ScreenTip title. Caption length does not increase ribbon
body height. Wrapped dropdowns and vertical splits centre the second row and its
arrow together; a single-row vertical split keeps its arrow below the caption.
Horizontal splits retain their separate arrow column. Medium/Small and menu rows
retain their existing single-line layout.

The width limit includes the entire Large button, including any separate arrow
column, and scales with DPI. Values below the existing 60-logical-pixel minimum
are rejected when assigning `Theme`. For example:

```csharp
ribbon.Theme = ribbon.Theme with { LargeButtonMaxWidth = 120 };
```

`RibbonSplitButtonItem` adds a primary `Click`/`PerformClick()` action and a separate
owned dropdown. Large supports both `Layout=Horizontal` (the default) and
`Layout=Vertical`, which places the icon above its caption/menu;
Medium/Small and menu rows use horizontal halves. `KeyTip` and `DropDownKeyTip`
address the actions separately. `CheckOnClick=true` makes primary activation toggle
its bindable `Checked` value, raising `CheckedChanged` before `Click`. Assigning
`Checked` raises no `Click`; opening the menu never toggles the primary half.
Showcase's Paste demonstrates an ordinary Large vertical split; Track changes
demonstrates a Large horizontal toggle split. A standalone two-state button uses `RibbonToggleItem`;
`RibbonButtonItem` remains an ordinary command without `Checked`.
`RibbonDropDownItem.IsDropDownOpen` exposes the current popup state. Dropdowns and
split arrows retain their pressed appearance while open, independently of hover
and the primary half's checked state.
Custom renderers override `DrawSplitItem` to receive both measured action contexts.
The retained foundation bridge does not provide complete accessibility semantics
for this new item; no split accessibility compatibility is claimed.

Office 2024 Light and Office 2019 Light, Compact/Touch density and LTR are the current appearance scope.
The optional `RibbonForm` supplies the native caption and title-bar QAT described
below. Optional native Mica/Acrylic form backgrounds are available through that host;
full Glass/translucent command panels and further theme treatments remain later work.
WPF template/resource compatibility is outside this independent native library.
Ordinary application controls remain native Forms controls. The runtime references neither WPF nor the designer SDK.

The [native themes plan](docs/THEMES-PLAN.md) tracks Office 2019 Light verification
and review, plus later light-theme work. Dark variants remain deferred.

Select `ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2019Light;` for square,
filled headers, a flat full-width white body with retained inner padding, fine
dividers and square bordered/shadowed popups. With a supporting `RibbonForm`,
active title-bar coloring extends across the tab band; reset restores grey.
The selected tab stays white with dark text by default. Office 2024 remains the default.
Custom values can start from `RibbonThemes.Office2019Light with { ... }`.

`RibbonFill` supports immutable intermediate stops without changing its existing
solid/two-color constructors, Start/End, deconstruction or endpoint `with` edits:

```csharp
var gold = new RibbonFill(Color.FromArgb(255, 247, 206), Color.FromArgb(255, 255, 251))
{
    IntermediateStops = new RibbonGradientStops(new[]
    {
        new RibbonGradientStop(.5, Color.FromArgb(255, 220, 140)),
        new RibbonGradientStop(.9, Color.FromArgb(255, 239, 182))
    })
};
ribbon.Theme = ribbon.Theme with
{
    Hover = gold,
    CommandAppearance = (ribbon.Theme.CommandAppearance ?? new()) with
    {
        StateChrome = new RibbonStateChromeSettings
        {
            HoverBorderColor = Color.Goldenrod,
            InnerBorderColor = Color.FromArgb(136, Color.White),
            InnerBorderWidth = 1
        }
    }
};
```

Offsets must increase strictly inside (0,1); the collection copies its input and
compares by value. Null/empty stops use the old ramp. Nullable `Direction` is
Vertical by default, with Horizontal also available. Opacity and darkening preserve
every stop. `BackgroundFill`, `TitleBarBackgroundFill`, `PopupBackgroundFill`,
`QuickAccessBackgroundFill` (lower QAT) and `BackstageNavigationBackgroundFill`
are optional opaque surface fills; null restores the previous Color fallback.
Assigning the matching Color clears its fill, and Background also clears the
lower-QAT fill. Form.TitleBarColor retains priority. Every stop in opaque roles,
including BodyBackground, must be opaque.

`StateChrome` is also available on FileButtonAppearance, GalleryAppearance (tiles),
EditorAppearance (choices) and OptionsAppearance (buttons). Hover/pressed/checked/
checked-hover border inks and inner-rim ink/width/inset are independent of fills.
Existing border overrides win; null/Empty preserves defaults, Transparent hides
ink and zero hides width. Empty groups add no effect. ChoiceBorderWidth controls
choice-state outlines separately from editor fields. These are shared foundations;
the [Office 2010 preset and review](docs/OFFICE2010-BLUE-PLAN.md) are later slices.

`HeaderPresentation` selects Filled or Underline independently of colors, corner
radius, caption extension and Backstage style. The default Office renderer keeps
application text/focus/indicator overrides. Header effects are per-ribbon theme
settings; null/Empty retains the established appearance:

| Setting | Default and scope |
| --- | --- |
| `HeaderSelectedBackground` | Surface for selected filled tabs; accepts a fill, including a gradient. |
| `HeaderSelectedForeground` | Existing foreground/accent/contextual ink. An explicit color changes selected-tab text and its underline, including contextual selections. |
| `HeaderFilledCornerRadius` | Square filled tab/File states. Explicit 0..64 logical pixels changes their corners. |
| `HeaderIndicatorThickness` | Three logical pixels for tab/File underlines; 0 hides them, up to 64 clamps within the header. |
| `HeaderIndicatorBottomInset` | One logical pixel below the underline; 0..1024 clamps its available height. |
| `HeaderIndicatorHoverColor` | SecondaryText for unselected tab/closed-File underlines; Transparent hides them. Material hover also applies `BackdropTabHoverOpacity`. |

These settings keep measured targets, File/QAT reservations and overflow intact.
Existing `HeaderHover`, `HeaderPressed`, `HeaderForeground`, indicator padding/
radius, tab spacing and scroller settings stay independent. Clear the new values
to restore the active style's defaults. Explicit flat/custom renderers retain
their own drawing policy.

Both native apps offer **View > Themes**, **Accent colors** and **Color title bar**.
Choose Office 2013/2019/2024 Light or Application theme, six accents or Default.
Color title bar follows the accent and uses white title text; off restores prior
caption color/ink. Accent Default resets only the accent. Selecting a theme uses
that theme's default accent and retains title-color mode. Showcase saves these
choices between sessions. Designer Sample declares its galleries/tiles/toggle in
the editable Form designer file; diagnostic tabs remain retired.

Office 2013 Light adds live accent-derived hover/pressed/checked/checked-hover
states and an always-accent File block for both CommandMenu and Backstage.
Its connected grey tab/body outline is enabled by
`HeaderAppearance.ConnectSelectedTabToBody`. The selected tab opens into the
adjacent body; scrolling clips the opening to the visible tab interior. BodyBorder
and BodyAppearance.BorderWidth control the body; FilledBorderColor/Width override
the selected border independently, including Transparent/zero. Missing/separated
bodies close the tab outline. Ordinary selected-header state updates immediately
while content animation continues; Office 2024 pill animation is unchanged. Custom
paint hooks receive HeaderBodyConnectionBounds as passive device-pixel geometry.
See the
[Office 2013 scope and evidence](docs/OFFICE2013-LIGHT-PLAN.md#e4-connected-selected-tabbody-outline--2026-11-14).
`AccentStates` controls wash strengths. Explicit Hover/Pressed/Checked assignments
remain authoritative; null on HoverOverride/PressedOverride/CheckedOverride resets
that state independently. `CheckedHover` is a separate nullable override.
Office 2019/2024 keep neutral grey ordinary hover and press; checked toggles and
selected gallery tiles use live accent washes (0.18 checked, 0.28 checked-hover),
matching WPF's custom-accent recipe. `AccentStates.HoverBackground` and
`PressedBackground` retain neutral fills independently of those washes; null on
either derives its accent wash. Command text and icons retain their ordinary
foreground, including hover, pressed and checked states. Accent text belongs to
active sibling navigation, such as the 2024 selected tab, Backstage entry and
Options page; classic navigation keeps its contrasting white text.
Library configuration buttons (OK/Cancel/Apply included) have a separate
`OptionsButtonAccentStates` palette: only primary OK has an accent idle fill;
Cancel, Apply and other ordinary buttons use Surface (white by default), with a
light hover wash and darker pressed wash. OK darkens its filled color by 0.10 on
hover and 0.20 on press, keeping white text by default. In OptionsAppearance,
AccentButtonHoverDarkening/PressedDarkening adjust those amounts (null restores
defaults, zero keeps the idle fill); AccentButtonHoverBackground/PressedBackground
and AccentButtonForeground supply explicit primary state fills and ink. Alpha
and gradient stops of the idle fill are preserved while darkening.
Pressed buttons keep their Accent border. Existing
OptionsAppearance.ButtonBorderColor/Width override that border, including
Transparent/zero. Explicit button/state fills win; null restores the
legacy ordinary/system and primary-button treatment. Application-owned buttons
keep their native ownership and appearance.
`FileButtonAppearance` supplies resettable state fills/ink and accent darkening;
null restores legacy File rendering. Gradients, alpha, Transparent and zero
strengths are retained. Assigning Theme remains application ownership and clears
BuiltInTheme; the samples keep their selected base identity in host preferences.

Shared per-ribbon settings also cover `CollapsedGroupBorder`,
`ApplicationMenuSplitDividerVisible`, `OptionsButtonCornerRadius`,
`MessageSideBordersVisible`, `HeaderScrollButtonsMatchTabHeight`,
`HeaderScrollButtonWidth`, `HeaderScrollButtonBackground`,
`HeaderScrollButtonBorder` and `ApplicationMenuWorkAreaInset`. The 2019 preset
uses borderless collapsed groups, undivided menu splits, square library dialog
buttons, messages with horizontal rules, tab-sized scrollers and zero File menu
work-area clearance. Defaults preserve Office 2024 behavior. Explicit options
button radii use the shared palette while retaining native button behavior;
application-owned controls keep their own appearance.

Office 2019 also uses WPF Classic-style Backstage: an accent rail, white navigation
and monochrome icon silhouettes, full-width square state fills, a 220-logical-pixel
Auto width starting point, and a flush page. Authored width constraints and native
page content remain effective. Office 2024 retains its inset Modern layout below
its separate title bar. Office 2019 enables `BackstageExtendsIntoTitleBar`: the
live navbar/page panes extend to the top of `RibbonForm`, behind a transparent
foreground containing the centered title and caption controls. Native page content
stays clear of the caption controls. The Back button is circular.
`BackstageTitleCentered` enables title centering for 2019; its default is false
for 2024. Closing Backstage restores the normal caption and authored color.
Navigation colors/state fills, icon masking/spacing, Auto width minimum, footer
clearance and page border are shared `RibbonTheme.Backstage*` settings.
Header scrollers retain tab-height-sized slots and visible `PopupBorder` borders;
`HeaderScrollButtonBorder` remains independently configurable.

KeyTip colors can be configured without a custom renderer:

```csharp
ribbon.Theme = ribbon.Theme with
{
    KeyTipBackground = new RibbonFill(Color.MidnightBlue),
    KeyTipForeground = Color.White,
    KeyTipBorder = Color.CornflowerBlue
};
```

The background defaults to `null`, preserving Office's pale yellow and the neutral
renderer's `Surface`. Empty text/border colors preserve their defaults; a
transparent border removes the outline. Geometry and keyboard behavior are
unchanged. [Appearance inventory and remaining consolidation](docs/M4I2-APPEARANCE-INVENTORY.md).
Showcase capture/check implementations are grouped under
[`Diagnostics`](samples/RibbonKit.WinForms.Showcase/Diagnostics/README.md).

### Opt-in geometry motion

```csharp
ribbon.AnimationLevel = RibbonAnimationLevel.Subtle; // None is the library default.
ribbon.TabContentAnimation = RibbonTabContentAnimation.SlideDown; // Fade is the default.
```

This animates individual Classic/Simplified, Compact/Touch, TabRow/BelowRibbon
QAT changes, minimize/restore and expanded tab selection by pointer, keyboard or
the matching properties. Minimize/restore reveals or retracts opaque natural-size
body drawings while the QAT and message frame follow the changing edge. It uses
180 ms for Subtle and 252 ms for Expressive. Temporary minimized-tab popups use
the opening effect below. The chevron shows the committed state throughout the effect.
The active Office indicator follows header geometry during playback. Expressive
adds a gentle horizontal spring before the pill settles; Subtle keeps a bounded
ease-out. Hovered unselected tabs use themed grey. The library commits real layout once and owns the inert drawings and
completion. System-disabled client animations, startup, batch updates and unsafe
presentation contexts keep immediate changes. Input and interruption settle the
latest committed state. `IsAnimating` reports geometry, group, message-panel, popup and Backstage motion. Application
content currently reflows immediately; optional host synchronization is next work.

Adaptive Classic group collapse/restore and Simplified inline overflow/return also
animate. While a Simplified full-group popup borrows its controls, the temporary
medium group button uses the same effect, including in a minimized tab body.
The existing `TabContentAnimation` selector applies: Fade crossfades group content;
SlideDown uses horizontal leftward collapse and rightward restoration for groups.
Subtle uses 150 ms / 12 logical pixels; Expressive uses 210 ms / 20. Text/icons keep
their natural size and real controls commit once. Continued width changes update
the arrival drawing; input, policy/style changes, hiding and disposal settle it.
Group presentation does not change customization JSON or native editor ownership.

Layered child presentation requires a Windows 8-or-later compatibility declaration
in the application's manifest; the sample's `app.manifest` supplies a Windows 10
declaration. DPI awareness still belongs to the host. Missing native support falls
back to immediate geometry. This preview still needs human visual and performance
acceptance; see the M6A plan for measured costs and pending gates.

Custom renderers can override `GetTabIndicatorBounds` and `DrawTabIndicator` to
participate in live and animated tab indicators. The former receives full tab
header bounds; the latter receives the measured indicator rectangle. Neutral
defaults to no separate indicator.

Command menus, gallery popups, QAT/group flyouts and temporary minimized-tab bodies
also use `AnimationLevel`. Their real frame, rounded clip, anchor and shadow stay
fixed while natural-size contents fade and slide down: 130 ms / 8 logical pixels
for Subtle, 182 ms / 12 for Expressive. Pointer/keyboard input settles the drawing
before normal interaction. Command-menu closing and native ComboBox choices remain immediate.
This opening style is independent of `TabContentAnimation`.

A minimized tab's temporary body keeps the ribbon's side margins and opens just
below the tabs, overlaying a lower QAT and messages. An outside click releases the
real popup/editors immediately and fades/slides an inert drawing away over
130/182 ms. The clicked application control receives focus and input normally.
None, system policy and update batches retain immediate dismissal; a new request,
resize, movement, deactivation or disposal releases the outgoing drawing.

By default, the real Backstage panes slide and fade as one surface, starting 15% left of their
settled location. `Theme.BackstageExtendsIntoTitleBar` chooses whether they extend
behind the integrated caption's transparent foreground (Office 2019) or stay below
the separate title bar (Office 2024). Their backgrounds are not duplicated by a
separate caption color animation. `Theme.BackstageSlideDistanceRatio` chooses
travel as a width fraction from 0 to 1: null uses 15%, zero explicitly requests
fading without travel. Subtle uses 220 ms and Expressive 308 ms; None and reduced-
motion policy remain immediate. When title centering is enabled, opening and
closing place the title 75% toward its destination immediately, then slide the
remaining 25% with the same duration and default easing, without fading the title.
`Theme.BackstageTitleSlideDistanceRatio` configures that animated fraction: null
uses 25%, zero snaps, and one animates the entire distance. Resize, maximize and
restore immediately remeasure and recenter the title without animation.

`Theme.BackstageMotion` accepts an immutable `RibbonBackstageMotionSettings`
group. Null members use the active family's Subtle/Expressive defaults:

| Member | Default and behavior |
| --- | --- |
| `PaneDuration` | 220/308 ms, shared by pane and title. Accepts 0..60000 ms; zero snaps both. |
| `PageDuration` | 130/182 ms. Accepts 0..60000 ms; zero switches pages immediately. |
| `PageSlideDistance` | 8/12 logical pixels. Accepts 0..1024; zero removes slide travel while duration remains independent. Does not change page Fade. |
| `PaneEasing`, `TitleEasing`, `PageEasing` | EaseOutCubic. Each can independently select `RibbonBackstageEasing.EaseOutCubic`, `SmoothStep` or `Linear`. |
| `PaneFadeEasing` | SmoothStep; uses the same curve choices. |
| `PaneFadeAmount` | 1, full pane fading. Accepts finite 0..1; zero keeps the pane opaque for slide-only motion. Title ink stays opaque and page Fade remains independent. |

For example:

```csharp
ribbon.AnimationLevel = RibbonAnimationLevel.Subtle;
ribbon.Theme = ribbon.Theme with
{
    BackstageMotion = new RibbonBackstageMotionSettings
    {
        PaneDuration = 320, PageDuration = 180, PageSlideDistance = 16,
        PaneFadeAmount = 0, PaneEasing = RibbonBackstageEasing.SmoothStep
    }
};
```

Clear a member with `with { PageDuration = null }` to restore just its family
default, or set `BackstageMotion = null` to reset the group. Existing slide ratios,
animation level, reduced-motion policy and validation remain authoritative.
Settings are captured when a transition starts; normal theme-change settling
applies to an active transition. Native timer pacing and hit-testing opacity
guards remain internal. The optional sample theme demo includes **Backstage
motion** controls for fade/slide-only, duration, page distance and reset.

Explicit `BackstageNavigationHover` and `BackstageNavigationSelected` fills also
apply to inset material rows, with authored alpha retained. Null values preserve
the established generic backdrop fallback in that path; full-width and Solid
defaults are unchanged.

`Theme.BackstageAppearance` accepts an immutable
`RibbonBackstageAppearanceSettings` group for navigation decoration and separate-caption tint. Every
nullable member resets independently; null retains the existing appearance:

| Members | Default and behavior |
| --- | --- |
| `MarkerLeftInset`, `MarkerWidth`, `MarkerTopInset`, `MarkerBottomInset` | 2, 3, 10, 10 logical pixels, each 0..1024. Width zero hides the marker. Top/bottom clearances are summed before DPI rounding, preserving fractional-DPI defaults. Row bounds clip painting. |
| `MarkerColor` | Empty follows Accent; supports alpha. Existing `BackstageSelectionIndicatorVisible` and `NavigationMarkerCornerRadius` remain independent. |
| `BackVisualInset` | 2 logical pixels inside the Back click target; accepts 0..1024. Does not change target size or `BackstageBackButtonCornerRadius`. |
| `BackOutlineWidth`, `BackGlyphStrokeWidth` | 1.5 logical pixels, accepting finite 0..32. Zero hides the outline or arrow respectively. Null preserves the existing outline rounding and one-device-pixel minima. |
| `BackGlyphScale` | 1, accepting finite 0..8; zero hides the arrow. Scales the vector's 5/6-pixel extents with their original separate DPI rounding, independently of circle/target size. |
| `NavigationCornerRadius` | Full-width rows are square; inset rows use the active renderer's chrome. An authored 0..64 logical pixels controls the state shape independently of width coverage. |
| `NavigationBorderColor`, `NavigationBorderWidth` | Empty/transparent, with no border. Authored color supports alpha; width accepts 0..32 logical pixels. Null width uses one device pixel when ink is authored. Zero width or transparent ink hides the outline. |
| `CaptionTintEnabled` | Null retains Modern's neutral separate-caption tint and Classic's opt-out. True/false independently enables/disables it. Full-height panes and authored Form/theme caption colors retain precedence. |
| `CaptionTintColor` | Empty follows the navigation color with existing coverage. Authored alpha multiplies tint coverage; Transparent hides paint. Does not recolor full-height panes. |
| `CaptionTintOpacity` | Null uses 1; finite 0..1 multiplies pane alpha, material navigation opacity and authored tint alpha. Zero hides paint. Independent of `RibbonForm.TitleBarTintOpacity`, which belongs to authored caption coloring. |

Tint follows the existing moving edge and pane fade. Caption ink uses readable
contrast over the tint; `TitleBarForeground` supplies an authored separate-caption
ink. During full-height Backstage closing, the transparent title plane retains
opaque ink and switches from navigation ink to normal caption ink when fading
would otherwise leave light text over a light background. Material contrast uses
the theme backing as an estimate, as ordinary authored caption colors do.
Pane/title durations, title travel, native pages and window integration retain
their existing owners. Temporary and settled captions use matching grayscale
title glyph coverage, and closing paints the normal caption before retiring its
temporary foreground layer. The optional demo's **Theme effects 10** has **Backstage
caption tint** presets/reset and **Try caption and title** review controls.

`Theme.CaptionButtonAppearance` accepts immutable
`RibbonCaptionButtonAppearanceSettings` for caption-button decoration:

| Members | Default and behavior |
| --- | --- |
| `HoverBackground`, `PressedBackground` | Null retains the resolved caption palette's first color stop and material fallback coverage. Authored fills support alpha, vertical gradients and Transparent. |
| `CloseHoverBackground`, `ClosePressedBackground` | Null retains red (232,17,35), opaque in solid captions and alpha 210/235 over material or the temporary foreground plane. Authored fills are independent. |
| `StateInset`, `CornerRadius` | Null retains full-target square state fills. Accept 0..1024 and 0..64 logical pixels; paint clamps inside the existing native target. |
| `GlyphSize`, `GlyphStrokeWidth` | Null retains a 10-logical-pixel vector and max(1 device pixel, DPI/96) stroke. Accept 0..256 and finite 0..32 logical pixels. Zero hides glyphs; authored geometry clamps within the target. |
| `GlyphColor`, `CloseHotGlyphColor` | Empty follows resolved caption Foreground or close hover/pressed White respectively. Alpha and Transparent are supported independently. |
| `RestoreOffset` | Null retains the legacy three **device** pixels between restore outlines. An authored 0..256 logical pixels scales with DPI and clamps inside the glyph; zero overlaps the outlines. |

Reset members independently, or assign `CaptionButtonAppearance = null` for the
group. Other caption/tint settings remain. Button hit areas, window actions,
title rendering and animation retain their existing owners. The optional demo's
**Theme effects 11** contains fill/shape/glyph presets and an isolated caption
preview; closing that preview leaves the main sample open.

Spacing uses three nullable theme properties, each accepting 0..1024 logical
pixels: `BackstageRowTextVerticalPadding` defaults to 16 total clearance added to
font height, `BackstageSeparatorHeight` defaults to 9, and
`BackstageTrailingPadding` defaults to 8. Font/Touch minima and available-space
clamps remain authoritative. Trailing clearance feeds both Auto navigation width
and text painting; separator height also feeds footer/scroll layout. Zero removes
separator space. The existing `MenuSeparatorHeight` remains menu-only.

```csharp
ribbon.Theme = ribbon.Theme with
{
    BackstageAppearance = new RibbonBackstageAppearanceSettings
    {
        MarkerWidth = 6, MarkerColor = Color.Gold,
        BackGlyphScale = 1.5, BackGlyphStrokeWidth = 2,
        NavigationCornerRadius = 8,
        NavigationBorderColor = Color.DarkOrange, NavigationBorderWidth = 2
    },
    BackstageSeparatorHeight = 16, BackstageTrailingPadding = 20
};
```

These settings retain selection, native page/editor identity and click targets.
Inherited drawing consumes them through the actual application renderer; custom
entry/chrome overrides keep precedence. Reset the group with
`BackstageAppearance = null` and spacing properties individually to null.
The optional sample theme demo includes **Backstage appearance**, with marker
visibility separate from its decoration/reset controls.

Header and shared effects also have independent overrides:

| Setting | Default and behavior |
| --- | --- |
| `HeaderAppearance.FilledBorderColor`, `FilledBorderWidth` | Filled tab/File states remain borderless. Authored ink supports alpha; width accepts 0..32 logical pixels. Null uses one device pixel when ink is authored; zero hides the border. |
| `HeaderAppearance.ScrollerCornerRadius`, `ScrollerBorderWidth` | Preserve existing configured square scrollers or ordinary renderer chrome, with a one-device-pixel border. Accept 0..64 and 0..32 logical pixels. Existing `HeaderScrollButtonBorder` supplies ink; body scroller shape remains independent. |
| `HeaderAppearance.ContextualUnselectedOpacity` | 0.6, multiplied by authored contextual-color alpha before composition. Accepts finite 0..1; selected/disabled ink and explicit selected foreground remain independent. |
| `HeaderAppearance.FileHighlightInset`, `FileHighlightBackground`, `FileHighlightForeground` | Open CommandMenu File highlight uses a 3-logical-pixel inset, Accent fill and automatic black/white ink. Inset accepts 0..1024 with available-space clamps; existing corner radius remains independent. Fill supports alpha/gradients; Empty ink uses contrast against the fill's first color. Explicit highlight settings also opt Filled headers into this common highlight; null retains their existing Filled recipe. |
| `Chevrons.Length`, `Depth`, `StrokeWidth` | Shared horizontal/vertical stroked chevrons use 10/6 logical pixels and 1.5-pixel stroke, with a one-device-pixel default stroke minimum. Dimensions accept 0..64 and stroke finite 0..32; zero hides the glyph. Utility slots still clamp size. Applies to header/body scroll, overflow, minimize, submenu and gallery chevrons; filled dropdown triangles and the Back arrow retain their own settings. |
| `FocusInset`, `FocusStrokeWidth` | Null retains 2 device pixels of inset and native dotted/Office one-device-pixel outlines. Authored inset accepts 0..1024 logical pixels and clamps inside short targets; width accepts 1..32. Explicit neutral widths use a dotted outline. Keyboard focus stays visible; custom focus hooks retain precedence. |
| `TabContentSlideDistance` | Shared tab-content/header-arrival SlideDown travel is 8/12 logical pixels for Subtle/Expressive. Accepts 0..1024; zero removes travel. Backstage page motion remains separate. |
| `HeaderIndicatorSpringAmount` | Expressive underline spring amount is 0.35. Accepts finite 0..1; zero removes overshoot. Existing motion clocks, reduced-motion policy, clips and width clamps remain effective. |

`HeaderAppearance` and `Chevrons` are immutable
`RibbonHeaderAppearanceSettings` / `RibbonChevronSettings` records. Null members
retain defaults, and assigning either group to null resets just that group:

```csharp
ribbon.Theme = ribbon.Theme with
{
    HeaderAppearance = new RibbonHeaderAppearanceSettings
    {
        FilledBorderColor = Color.DarkOrange, FilledBorderWidth = 2,
        ScrollerCornerRadius = 8, ScrollerBorderWidth = 2,
        ContextualUnselectedOpacity = .9
    },
    Chevrons = new RibbonChevronSettings { Length = 16, Depth = 9, StrokeWidth = 2 },
    FocusInset = 4, FocusStrokeWidth = 2,
    TabContentSlideDistance = 24, HeaderIndicatorSpringAmount = .7
};
```

All values validate before theme/selection mutation. These paint/motion overrides
retain header/body targets and native editor identity. Scene capture retains its
authored motion values; theme changes use the existing settle path.
The optional `--office2019-theme-demo` keeps these earlier review groups on **Theme
effects 2**; new command controls use **Theme effects 3**. File-highlight
review includes a CommandMenu/Backstage toggle; appearance reset stays separate
from presentation, routing and animation-level choices.

The title-bar QAT is hidden throughout Backstage opening, display and closing;
its authored visibility, placement and commands return when Backstage closes.
TabRow and BelowRibbon QAT presentation is retained, including a TitleBar
preference that currently falls back to TabRow. Caption buttons
stay in place above the real panes. By default, opening and closing also fade the surface to
soften their appearance/disappearance. Page changes use the shared
`TabContentAnimation`: Fade fades in the incoming native page body; SlideDown
slides it down 8/12 logical pixels for Subtle/Expressive. Navigation, page frame
and caption stay fixed, and native page controls keep their parent/handles/state.
None and system-disabled animation remain immediate. No page is captured.
See [page-motion contract and focused evidence](docs/BACKSTAGE-PAGE-MOTION.md).
Application controls retain their native handles, parents,
values and selection. Backstage page controls are never captured or printed.
Solid opening temporarily holds the already-composed document drawing beneath
the fade to prevent native RichTextBox repaint flicker, then releases that image.
Back/Escape and visibility requests slide out Backstage after validation accepts
closing. `IsBackstageOpen` stays true until completion, then the normal close events
and focus restoration run once. Reopening cancels a pending close; new input can
finish it without activating the outgoing page. Command, mode and modal transitions
close immediately. None preserves immediate behavior throughout.

Message arrivals, reopening, dismissal, removal and Clear also use the same level:
160 ms for Subtle or 224 ms for Expressive. Rows reveal/retract at natural size;
remaining rows move into place, and body/QAT joins follow the visible panel edge.
Minimized layouts with a TabRow or hidden QAT retain a standalone message card.
Message state, ribbon height and callbacks commit immediately, independently of
the drawing. Same-direction changes before the first frame share one transition;
opposite/repeated requests settle to the latest state. Startup, update batches,
disposal, caption edits and collection replacements remain immediate.

Showcase's **View â†’ Ribbon animation** group selects None, Subtle or Expressive.
Its **Tab content** menu chooses Fade or Slide down. Slide down moves the arriving
body at natural size inside the existing viewport, without a content fade; the
other geometry transitions keep their existing behavior. These appearance choices
remain application-owned and are excluded from customization profiles.
The same choice animates newly shown contextual headers, live tab additions and
merged headers. Header slots use committed layout positions; incoming captions
fade or slide down at natural size. Showing/merging and immediately selecting a
tab in the same command share one transition. None, startup and update batches
remain immediate; hiding or unmerging restores the normal selection fallback.

### Compact and Touch spacing

`RibbonControl.Density` defaults to `RibbonDensity.Compact`. Set it to
`RibbonDensity.Touch` for larger command rows, native field frames, popup choices,
QAT targets, gallery tiles and Backstage navigation. Three command rows remain in
each column. Font size, source models, selection, binding and authored theme values
stay the same. Metrics use 96-DPI logical pixels and scale with the ribbon's DPI;
Touch applies minimum target sizes without rewriting authored tile or input widths.

```csharp
ribbon.Density = RibbonDensity.Touch;
ribbon.DensityChanged += (_, _) => SaveDensityPreference(ribbon.Density);
```

A change first commits valid focused ribbon input; invalid input blocks the change.
Accepted changes close transient popups, cancel gallery previews and preserve native
editor handles, focus and text selection where still available. Open Backstage stays
open with its native page intact. Application pages, document content and dialogs
remain sized by the host. Changes are immediate by default; automatic device detection and
preference storage belong to the application. Custom renderers receive the owning
density in `RibbonPaintContext.Density`.

Large Touch commands align their icons across button types. Ordinary captions sit
eight logical pixels below their icon slots, with room for two aligned text rows.
Vertical splits use a larger primary action, with the actual one- or two-line
caption and centered indicator grouped in the lower menu action; clicking the
caption opens that menu. The split boundary leaves room for larger fonts.
Horizontal Touch splits reserve a 44-logical-pixel arrow and a larger primary
action (at least 72 for Large, 56 for smaller presentations). Large horizontal
splits add the arrow allowance to the existing caption budget, so their effective
total width can exceed `LargeButtonMaxWidth`; the authored metric is unchanged.
Compact content positions and standard menu-row layout retain their existing
geometry.

Showcase's third QAT entry, also under **View â†’ Input mode**, demonstrates a
host-owned Mouse/Touch selector. Actual VS property editing remains deferred to the
designer slice. Office 2024 joins a BelowRibbon QAT to the body's outer frame;
the minimized toolbar becomes a standalone card.

## Message panels

Messages are library-owned models attached to the ribbon. The application owns
their text, icons and actions. Showcase demonstrates them in the **View â†’ Messages** dropdown.

```csharp
var notice = new RibbonMessage
{
    Title = "PROTECTED VIEW",
    Text = "Review this document before enabling editing."
};
notice.Action.Text = "Enable editing";
notice.Action.KeyTip = "E";
notice.Action.Click += (_, _) => ReviewDocument(); // Host behavior; no automatic dismissal.
ribbon.MessageBar.Items.Add(notice);
notice.Dismiss(); // Retained model, one Dismissed event; repeated calls are harmless.
notice.IsOpen = true; // Reopen with the same action/bindings.
```

Open visible messages stack and wrap; narrower layouts can put an action beneath
the text. `IsOpen` and `IsDismissible` support ordinary WinForms binding. Set
`IsDismissible` false to omit the user close target; code can still call Dismiss.
`MessageBar.HasOpenMessages` and its change event report whether the stack exists.
Removing a model detaches it; disposing it releases its owned action. Icon/Image
assets are borrowed, with Icon taking precedence. No message appears by default.

Office connects the stack to the body or BelowRibbon QAT, squaring the adjoining
bottom corners and keeping the last message edge rounded. When the body is
minimized and QAT is **not BelowRibbon**, the stack is a standalone rounded card
below the tabs. Hidden QAT and TitleBar placement use this same condition.
Neutral renders a separate flat stack. Closing the final message restores the prior ribbon geometry. The body shadow stops at the join so it cannot
obscure message content; the stack retains its own lower shadow.

Tab/arrows, Enter/Space, explicit action KeyTips and automatic message action/close
KeyTips use the ribbon's normal input paths. Message arrival retains document
focus. Theme message roles control fill/text/border/icon colors, padding and row
minimum; Touch enlarges action and close targets. Renderer callbacks receive the
same geometry used by hit testing and wrapping. Message motion follows the
opt-in policy above; localization and live-region accessibility remain later slices.

## Application contract

`FileMenu` is an owned, stable `RibbonDropDownItem` declaration. It starts hidden;
enable it and supply application commands through its normal Items collection:

```csharp
ribbon.FileMenu.Id = "file";
ribbon.FileMenu.Visible = true;
var open = new RibbonButtonItem { Id = "file.open", Text = "Open", KeyTip = "O" };
open.Click += (_, _) => OpenDocument();
ribbon.FileMenu.Items.Add(open);
```

The default header is File with KeyTip F. It has a measured slot before TabRow QAT
and the tabs, using `BodyMargin.Left` to align its button and popup with the body.
It remains available while minimized. BelowRibbon QAT uses the available row
width, inside the body margins when joined. The existing owned popup stack supplies described/nested commands,
native fields, cancellation through DropDownOpening, closed events and Escape.
Opening File retains tab selection. Hide/disable/dispose closes its open stack.
`DrawFileButton` lets custom renderers paint the header using measured bounds.
The header uses Text; command rows use normal icons. FileMenu itself is owned by
the ribbon and cannot be reparented; its direct disposal permanently releases that
stable declaration. Images/renderers remain borrowed.

### Native Application Menu

`RibbonFileMenuMode.CommandMenu` (numeric value 0) now opens the two-pane
Application Menu. Existing `FileMenu.Items` are its left commands. Hovering a left
dropdown or either split half selects its right pane without invoking a command.
A split primary still invokes on activation; its arrow selects the pane. Hovering
a plain command restores the default pane. Gaps, separators, disabled rows, footer
and right-pane transit retain the selected pane. Right-pane dropdowns continue
using ordinary submenus. Each opening starts with `ApplicationMenu.DefaultPane`.
Selection is transient.

```csharp
ribbon.ApplicationMenu.DefaultPane.DropDownHeader = "Recent documents";
ribbon.ApplicationMenu.DefaultPane.Items.Add(
    new RibbonButtonItem { Id = "recent.report", Text = "Quarterly report" });
var export = new RibbonSplitButtonItem {
    Id = "file.export", Text = "Export", DropDownHeader = "Export document" };
export.Click += (_, _) => ExportDefault();
export.Items.Add(new RibbonButtonItem { Id = "export.pdf", Text = "PDF document" });
ribbon.FileMenu.Items.Add(export);
ribbon.ApplicationMenu.FooterItems.Add(
    new RibbonButtonItem { Id = "file.options", Text = "Options" });
```

`ApplicationMenu`, its `DefaultPane` and `FooterItems` are stable owned content
properties. Pane declarations reuse dropdown `Items`, `DropDownHeader`, `Content`
and `ContentType`; do not put a component into two collections. Plain commands
close File before invoking once. Native validation can veto pane changes/actions,
Escape or outside dismissal. Structural/lifetime teardown still closes. Existing
File opening cancellation and closed events remain. Explicit `FileMenu.Content`
or `ContentType` retains the compatible whole-body override.

Native content follows the [retained dropdown contract](#usercontrol-popup-bodies):
explicit unparented Controls remain application-owned; selected UserControl types
are created before Form.Load, retained and declaration-owned. Pane changes/reopen
preserve controls, values and HWNDs. The library clips/scrolls the hosting slot;
the application owns body layout, commands, data and styling. Constructor failures
propagate without retaining an untracked popup.

The measured layout starts from 150/340 logical pixels for navigation/pane width;
navigation grows up to 260 for native text and scrollbar space. Navigation rows
start at 40, plain pane rows at 28, and footer buttons at 30; all reach at least
44 in Touch and grow for native fonts. Navigation and illustrated choices use
32-pixel icons. `MenuPresentation = IllustratedChoices` on a pane enables bold
choice titles and measured descriptions. A continuous inner frame encloses both
columns, with an aligned divider. Default content has an inset accent heading;
selected panes have a full-width shaded heading band. The surrounding inset and
footer share the default pane's background color. Footer buttons align right
and reserve space for wrapping/overflow. Split sections share one painted perimeter;
an open split's unhovered primary has a lighter fill than its arrow. Hovering
either half gives both the same normal hover fill. Columns
scroll independently, and text preserves each viewport's clip. The popup fits the
monitor work area on a usable side of File. Paint, pointer, keyboard reveal,
KeyTips and accessibility share these bounds. `DrawApplicationMenuBackground`
uses shared menu/border roles; application controls retain their own appearance.
Native validation can veto a hover-driven pane change or default-pane reset.
Switches retain each native body and preserve navigation/footer scroll position.

Choose **Edit Application Menuâ€¦** from the ribbon smart tag or the ApplicationMenu
property editor. The dedicated tree edits left commands, the default pane, nested
pane commands, section separators and footer actions. Properties and preview
clicks select draft entries. Native bodies are inert placeholders. OK applies one
transaction; Cancel/no-op does not mutate. Existing components, bindings/events,
icons and borrowed content retain identity. The accepted component is selected in
Properties after the modal editor closes. Framework Undo/Redo and generated-code
checks have automated evidence; actual VS save/reopen remains a review gate.

Showcase starts with Application Menu and offers a Backstage footer action; its
Backstage offers a return action. The permanent Designer Sample retains its
Backstage startup choice and adds designer-authored Application Menu examples:
choose **File opens â†’ Application Menu** to review them. No extra review app is
required. See [contract, evidence and remaining gates](docs/APPLICATION-MENU-PLAN.md).

After refreshing the permanent sample, `eng/Test-ApplicationMenu.ps1` builds an
isolated package-only consumer and runs the selected native material/density/
renderer scopes. Its foreground fixtures run sequentially and stop input on
focus loss.

### Illustrated command choices

An optional `RibbonDropDownItem.DropDownHeader` adds a noninteractive heading.
`MenuPresentation = RibbonMenuPresentation.IllustratedChoices` gives its leaf
commands and options 36-logical-pixel artwork, bold titles, two description lines
and a full-row selected fill. Supply artwork through the existing borrowed Icon,
Image or LargeImage properties and descriptions through MenuDescription:

```csharp
var modes = new RibbonDropDownItem {
    Text = "Input mode",
    DropDownHeader = "Optimize spacing between commands",
    MenuPresentation = RibbonMenuPresentation.IllustratedChoices
};
modes.Items.Add(new RibbonRadioButtonItem {
    Text = "Touch", Icon = touchIcon,
    MenuDescription = "More space between commands.\nOptimized for use with touch."
});
```

Standard is the default presentation. Editors, separators and submenus retain
their layouts; galleries keep their existing group headings. Projecting the whole
dropdown through QAT retains its heading and illustrated choices. Individual
commands projected into QAT retain the toolbar's own presentation. The renderer's
DrawMenuHeader hook reuses its existing heading theme roles; Office 2024 uses a
rounded fill, and neutral uses transparent bold text. The sample's Mouse/Touch
selector supplies its own vector illustrations and synchronizes its selected row
with accepted Density changes, including validation vetoes.

### Transient tab and group merging

`RibbonMergeSource` owns content for an application context, such as a document
or chart editor. Call `ribbon.Merge(source)` when that context becomes active and
`ribbon.Unmerge(source)` when it leaves. Focus changes do not activate sources.
Both methods return false when that operation is already satisfied on this host.
A source can be active on one host at a time; unmerge before retargeting it.

```csharp
var source = new RibbonMergeSource { Text = "Chart document", Order = 10 };
var tools = new RibbonTab { Text = "Chart Design", KeyTip = "CD" };
var data = new RibbonGroup { Text = "Data" };
data.Items.Add(new RibbonTextBoxItem { Text = "Series", Value = "Revenue" });
tools.Groups.Add(data);
source.Tabs.Add(tools);
var contribution = new RibbonGroupContribution {
    TargetTabId = home.Id, Order = -1
};
contribution.Group.Text = "Chart context";
contribution.Group.Items.Add(new RibbonButtonItem { Text = "Update chart" });
source.Groups.Add(contribution);
ribbon.Merge(source);
// Later: ribbon.Unmerge(source); dispose source when the context is destroyed.
```

The host's Tabs and each tab's Groups remain authored collections. Use
`GetPresentedTabs()` / `GetPresentedGroups(tab)` for presentation-order snapshots,
including merged content. Models keep their source owner, IDs and bindings; the
source keeps an independent BindingContext, while painting uses the host's theme,
font and DPI. Host disposal unmerges borrowed sources without disposing them.
Source disposal removes its contributions and disposes its owned models.

Host content has order zero and precedes merged content at equal order. Source
Order sorts whole tabs; each contribution has its own group Order. Negative orders
precede host content. Equal orders use first-merge sequence, retained across
unmerge/remerge on the same host, then declaration order. A missing TargetTabId
has no group presentation; adding a matching authored tab makes it available.
Duplicate IDs are rejected before attachment. Surviving selected host tabs stay
selected; removal of the selected source tab uses the normal visible fallback.

QAT references to merged commands park disabled while the source is away and
revive on remerge. Its native editor and gallery presenters are parked for reuse
on that host; moving the source to another host releases the old native cache.
Use `GetMergeSource(element)` to identify transient models and QAT references,
including parked references after source disposal, so future structural storage
does not persist context-owned content. A source's lifetime belongs to the app.
There is no automatic source-QAT, File/Backstage or nested-source merging.

Showcase **View â†’ Chart â†’ Chart context** adds Chart Design and Home's Chart context
group. Public-only shown checks are available through `--merge-checks` /
`--merge-checks-flat` on the native runner, followed by an evidence directory;
the independent consumer includes `--validate-merge` / `--validate-merge-flat`.
Actual VS review remains deferred.

### Modal tab scopes

Mark a tab `IsModal = true`, then call `EnterModal(tab)` to present it exclusively.
The tab must already be visible, enabled and authored or merged into this ribbon.
Entry does not change authored tab/File visibility. QAT remains available,
including commands from ordinary authored tabs. File, Backstage and minimize are
blocked while the scope is active; blocked requests leave no deferred value.

```csharp
var preview = new RibbonTab {
    Text = "Print Preview", IsModal = true,
    CloseButtonText = "Close Print Preview"
};
// Add preview and its groups to the host or an application-owned merge source.
ribbon.EnterModal(preview);
// Later: ribbon.ExitModal();
```

`IsModal` and `ModalTab` on the ribbon describe transient state. `ModalEntering`
and `ModalExiting` receive cancellable RibbonModalEventArgs; `ModalEntered` and
`ModalExited` report completed transitions. Normal entry/exit respects native
editor validation, and entry respects an open Backstage's closing veto. A second
different mode must wait for explicit exit; reentrant transitions are guarded.

The close command replaces minimize in the header and uses shared theme states,
keyboard focus, automatic KeyTips (preferred X) and the existing button automation
path. Both header utilities use `RibbonTheme.HeaderButtonInset` (default four
logical pixels) for smaller, vertically centered visuals with equal top/right/
bottom spacing. Their full measured click targets stay available. Zero uses the
full slot; oversized values clamp safely. `RibbonTab.CanClose = false` hides that command and blocks user Escape;
application ExitModal remains available with normal cancellation/validation.
Escape closes a native choice list, popup or KeyTip level before leaving modal
presentation. From application content it can exit directly. Removing, unmerging,
hiding, disabling or clearing IsModal on the active tab forces a noncancellable
`TabUnavailable` exit, even when pending editor input is invalid.

Exit restores the prior valid selected tab or a deterministic visible fallback,
prior minimized state and still-available focus. Visibility edits and merged
sources added during the scope survive exit. GetPresentedTabs returns only the
modal tab while active; authored Tabs/Visible values remain appropriate inputs
for future structural persistence. The library does not own a print document,
printer, preview paint, application content visibility or nested modal stack.

Showcase **View â†’ Print preview** demonstrates an application-owned preview
page with native Copies/Zoom fields. The shared public-only consumer checks use
`--modal-checks` / `--modal-checks-flat` on the native runner, or
`--validate-modal` / `--validate-modal-flat` in the independent consumer, followed
by an evidence directory.

### Galleries in dropdowns, the ribbon and Quick Access

`RibbonGalleryItem` is a native gallery command owning `RibbonGalleryTile`
declarations through its content-serialized Items collection. Put it in a ribbon
group or a normal menu. Tiles retain normal IDs, captions, KeyTips, availability,
binding context and borrowed `Icon` / `Image` / `LargeImage` assets. Tile models
belong only to a gallery; ordinary commands cannot be placed in gallery Items.

```csharp
var gallery = new RibbonGalleryItem {
    Text = "Styles", KeyTip = "GS", ColumnCount = 3,
    TileWidth = 100, TileHeight = 80, MaxVisibleRows = 3
};
var normal = new RibbonGalleryTile { Text = "Normal", GroupName = "Document" };
var heading = new RibbonGalleryTile { Text = "Heading", GroupName = "Headings" };
gallery.Items.AddRange(normal, heading);
group.Items.Add(gallery);
gallery.SelectedItem = normal;
gallery.SelectedItemChanged += (_, _) => ApplyStyle(gallery.SelectedItem);
gallery.ItemPreview += (_, e) => PreviewStyle(e.Item);
gallery.ItemPreviewCancelled += (_, _) => ApplyStyle(gallery.SelectedItem);
gallery.SelectionCommitted += (_, _) => RecordStyleAction(gallery.SelectedItem);
```

`SelectedItem` is bindable and starts null. Setting it from code/binding raises
SelectedItemChanged, with no user commit. Activating an available tile updates
selection/binding and raises SelectionCommitted once, including reselecting the
current tile; the latter does not raise SelectedItemChanged again. Removing or
disposing a selected tile clears selection. Hiding/disabling it retains authored
selection, while preventing user activation. Direct tile PerformClick uses the
same commit path; direct gallery PerformClick/ShowDropDown uses the popup stack.

Preview never writes SelectedItem. Pointer hover and keyboard navigation raise
ItemPreview; leaving, user scrolling, Escape, outside dismissal, source loss,
collection mutation and programmatic selection cancel an active preview once.
Successful commit ends preview without cancellation. The application owns its
document preview, baseline/rollback, action and settings persistence. Painting and
stationary-pointer messages must not override keyboard preview.

Consecutive visible tiles with the same nonempty GroupName share a noninteractive
heading. Authored order and blank-heading groups are retained. ColumnCount is a
preferred count (1â€“32); TileWidth/TileHeight are logical-pixel metrics (32â€“512),
and MaxVisibleRows is a preferred upper row budget (1â€“32). Fonts, headings and
screen work-area limits constrain the actual grid/viewport; long contents use
native scrolling. Images retain aspect ratio; a renderer can override
`DrawGalleryTile` and `DrawGalleryHeading`. Another application can provide rich
tile artwork through IRibbonIcon without Showcase helpers or native page hosting.
Office 2024 section headings use a rounded theme band and bold text, with paired
`GalleryHeadingBackground` / `GalleryHeadingForeground` settings. The neutral
renderer uses bold text on a transparent background. Headings remain
noninteractive and scroll with their tiles.

Opening reveals an available selected tile. Arrow keys move preview through the
grid, Home/End and PageUp/PageDown navigate long content, Enter/Space and tile
KeyTips commit, and Escape dismisses one popup level. Opening respects native
editor validation and normal DropDownOpening cancellation. Layout-affecting
mutation closes stale geometry; native editors/command models are not cloned.
Showcase Home â†’ Galleries demonstrates Styles and Text color with host-owned
document preview and commit actions.

Set `IsInRibbon = true` to display a single-row strip in Large presentations.
`InRibbonColumnCount` (1â€“12, default 3) sets its columns independently of popup
ColumnCount. TileWidth determines its logical width; ribbon body height determines
its height. SizePolicy entries of Medium/Small select a full-height Large dropdown
fallback in its own column. They do not create stacked medium/small gallery
buttons. A Large entry retains the strip; the authored policy stays unchanged.
Dropdown-only galleries (`IsInRibbon = false`) retain ordinary size policies.
Custom renderers can read `RibbonPaintContext.IsGalleryDropDown` to distinguish
the reduced dropdown from the strip without changing the source model.
Up/down buttons and the mouse wheel browse whole realized rows, with
disabled endpoint buttons. Arrow keys/Home/End preview tiles, PageUp/PageDown
browse rows, and Enter/Space commit. F4, Alt+Down or the expand button opens the
ordinary gallery popup. Cancelling expansion retains the strip's browsed row;
programmatic selection changes reveal the selected row.
The three utility buttons share equal heights within a four-logical-pixel inset,
filling the gallery interior while preserving its frame in hover/pressed states.

```csharp
gallery.IsInRibbon = true;
gallery.InRibbonColumnCount = 3;
ribbon.QuickAccessItems.Add(new RibbonQuickAccessDropDownReference {
    Source = gallery
});
```

QAT references open the same source gallery, including through overflow or while
its source tab is inactive. Selection, bindings, preview and commit handlers stay
on the source. Copies do not clone tiles, change the active tab or dispose the
source when removed. One reusable library strip presenter follows a source into
borrowed group/minimized-tab popups; application editors keep their existing
ownership. Only the active presentation owns temporary preview cancellation.
Showcase exposes Styles as a strip and Styles/Text color as its first QAT entries.

Filtering controls, user-resizable popup geometry,
additional themes and new accessibility providers remain separate refinements.
Designer metadata/collection authoring is included; actual VS acceptance is
deferred to the designer slice.

After the normal Debug build, focused shown checks are available as
`--gallery-checks` / `--gallery-checks-flat` on the native runner, each followed by
an evidence directory. The same public-only fixture is included in the independent
consumer as `--validate-gallery` / `--validate-gallery-flat`. A project-run pass
does not establish a newly packed consumer or physical monitor acceptance.
The additional strip/QAT scope uses `--in-ribbon-gallery-checks` /
`--in-ribbon-gallery-checks-flat`, with consumer equivalents
`--validate-in-ribbon-gallery` / `--validate-in-ribbon-gallery-flat`.
Use `--gallery-strip-checks[-flat]` or `--qat-gallery-checks[-flat]` for separate
strip or projection scopes; their consumer flags are
`--validate-gallery-strip[-flat]` and `--validate-qat-gallery[-flat]`.

### Custom dropdown gallery UserControls

An ordinary gallery dropdown can use an application-designed UserControl. In
**Edit Ribbon**, select the gallery and choose **Data â†’ DropDownPresenterType**.
Build the consumer project to refresh the list of available classes. The class
must be public, concrete, implement `IRibbonGalleryPresenter` and have a public
parameterless constructor. Generated code stores the class selection:

```csharp
gallery.DropDownPresenterType = typeof(MyGalleryControl);
```

For construction that needs application arguments, use the runtime factory instead:

```csharp
gallery.DropDownPresenterFactory = () => new MyGalleryControl();
```

The control implements `IRibbonGalleryPresenter` for bounds/hit testing, layout
navigation, selection reveal and row/page scrolling. RibbonKit supplies a
`RibbonGalleryPresenterContext` containing the original source, theme, renderer,
font, DPI and constrained viewport. Its Preview, FocusItem and Commit methods
use the existing gallery behavior; programmatic selection remains distinct from
accepted activation. The library owns the popup frame, scroll buttons and
navigation route. Application visuals/data stay in the control.

Each successful opening creates a fresh owned view; popup close disposes it and
makes its context inert. The class and factory are mutually exclusive; clear one
before setting the other. Null for both uses the built-in gallery. Class selection,
draft validation and inert previews never construct the UserControl. Inline bodies
have independent selectors below. QAT popups use these dropdown selectors when
UseDropDownPresenterInQuickAccess is true; its default false preserves the built-in
QAT popup. The factory is excluded from designer
serialization; presenter controls/state are excluded from customization JSON.
Ordinary dropdown/split UserControl bodies use the separate
[retained-content contract](docs/DROPDOWN-CONTENT-PLAN.md).

The Designer Sample's designer-declared **Gallery â†’ Document styles** demonstrates the
public API with [GalleryStylesControl](samples/RibbonKit.WinForms.DesignerSample/GalleryStylesControl.cs).
Its tab, group, gallery, tiles, initial selection, event hookups and presenter-class
assignment are in `DesignerForm.Designer.cs`, visible through Edit Ribbon. The
UserControl body stays in its ordinary UserControl designer.
Open **GalleryStyleTileControl.cs â†’ View Designer** to edit one card using ordinary
panels and labels. **GalleryStylesControl.cs â†’ View Designer** shows six real card
instances. At runtime the presenter creates cards for visible rows of the
two-column grid and retains visited cards for that popup. Bounds for unvisited
items are calculated without creating controls. Captions/items still come from
Edit Ribbon; the card's Caption property supplies illustrative text in the
standalone designer. The sample paints
only its selection outline, rather than drawing every thumbnail and label by hand.
See the [contract, review steps and focused evidence](docs/GALLERY-PRESENTER-PLAN.md).

For large custom galleries, keep `GetItemBounds` inexpensive and create native
child controls only for visible rows. Initial synchronization receives the monitor
budget before preferred size is measured; bound realization by the presenter's
natural size too. The library preserves fresh-view ownership and cannot virtualize
an application's arbitrary control tree. See the
[opening measurements and 1,000-entry checks](docs/POPUP-OPENING-PERFORMANCE.md).

### Custom inline gallery UserControls

The ordinary in-ribbon strip can use a separate application-designed UserControl:

```csharp
gallery.IsInRibbon = true;
gallery.InRibbonPresenterType = typeof(MyInlineGalleryControl);
gallery.DropDownPresenterType = typeof(MyPopupGalleryControl);
```

Both implement the existing `IRibbonGalleryPresenter`; context.Presentation is
InRibbon or DropDown. The two selector pairs are independent. For runtime
construction with arguments, clear InRibbonPresenterType and assign
InRibbonPresenterFactory instead. Null for both inline selectors retains the
built-in strip. After building the consumer, select **Data â†’ InRibbonPresenterType**
in Edit Ribbon or component Properties. Its class list is independent of
DropDownPresenterType, and `(None)` restores the built-in inline strip. Discovery
and inert previews never construct the application UserControl; generated code
stores its `typeof(...)` assignment. Edit the body in the ordinary UserControl designer.

RibbonKit creates the ordinary inline control for a visible strip slot and
retains it across hiding and popup expansion. Collapsed-group, minimized-tab and
QAT-group bodies create separate inline controls owned by their popup sessions.
Closing those hosts disposes their bodies; reopening creates fresh ones. An
expanded gallery popup also creates its own fresh control. All views display the
original source items and selection; focus, scroll
and preview ownership are per view. Hidden/suspended contexts become inert, and
reactivation supplies another context. Replacement, final source detach or
disposal releases the owned inline tree. The library controls viewport size,
clipping, utility buttons, validation and keyboard routing; inline Tab leaves the
gallery, while F4/Alt+Down expands it. Use context input requests for native child
tiles and avoid a second keyboard/command dispatcher. Keep context-sized physical
layout from being scaled again after parenting. A presenter root that assigns
physical bounds in Synchronize can use AutoScaleMode.None; its nested native
UserControls retain normal designer scaling, as in both sample gallery views.
Abort a child press if its native focus attempt was rejected or FocusItem returns
false, so validation is not repeated on release.

The permanent Designer Sample's Gallery tab uses a visually editable
[GalleryStylesStripControl](samples/RibbonKit.WinForms.DesignerSample/GalleryStylesStripControl.cs)
beside the existing popup example. Temporary bodies use the same inline selectors,
with context.Presentation=InRibbon and route-aware context.IsQuickAccess. Expanding
a QAT body still follows UseDropDownPresenterInQuickAccess. Presenter types, controls and
transient view state stay out of customization JSON. See the
[temporary-host correction and current evidence](docs/GALLERY-PRESENTER-PLAN.md#popup-card-rendering-and-temporary-inline-hosts--2026-11-02).

### Custom gallery popups from Quick Access

Reuse the source gallery's dropdown class or runtime factory for QAT openings:

```csharp
gallery.DropDownPresenterType = typeof(MyPopupGalleryControl);
gallery.UseDropDownPresenterInQuickAccess = true;
ribbon.QuickAccessItems.Add(new RibbonQuickAccessDropDownReference { Source = gallery });
```

The toolbar entry stays a compact launcher. Direct entries, duplicates, overflow,
nested QAT menus and QAT group routes resolve the original source and open a fresh
popup-owned control. context.IsQuickAccess identifies that route; Presentation is
DropDown. Selection/events stay on the original gallery, and popup close disposes
the view. Existing inline controls and source tiles are retained. QAT activation
does not select the source tab.

The opt-in defaults false; null dropdown selectors still use the built-in popup.
Changing the opt-in closes this source's QAT popup and makes its callbacks inert,
while an ordinary gallery popup keeps its own route. The bool supports normal
Properties/source serialization and stays out of customization JSON. Its Edit
Ribbon draft entry is deferred to the authoring slice. No extra QAT presenter
class is needed. The Designer Sample declares this opt-in and a **Document styles**
QAT entry with KeyTip **3**, reusing GalleryStylesControl.
See [QAT evidence and review steps](docs/GALLERY-PRESENTER-PLAN.md#qat-gallery-popup-increment--2026-11-02).

### Native Backstage

`FileMenuMode` defaults to `CommandMenu`, preserving existing File consumers.
Choose `Backstage` to route the same File header, pointer/keyboard activation and
`FileMenu.ShowDropDown()` to the library-owned surface. `FileMenu.Items` remains
the Application Menu left-command declaration; `Backstage.Items` owns separate navigation models.
No header duplication, custom Form subclass or Showcase helper is required.

```csharp
var infoContent = new UserControl(); // Application supplies and owns native content.
var printContent = new UserControl();
var infoImage = SystemIcons.Information.ToBitmap(); // Borrowed by the navigation item.
ribbon.Backstage.Items.AddRange(
    new RibbonBackstagePageItem { Text = "Info", KeyTip = "I", Content = infoContent, Image = infoImage },
    new RibbonBackstagePageItem { Text = "Print", KeyTip = "P", Content = printContent });
var save = new RibbonButtonItem { Text = "Save", KeyTip = "S" };
save.Click += (_, _) => SaveDocument();
ribbon.Backstage.Items.Add(save);
ribbon.Backstage.Items.Add(new RibbonMenuSeparatorItem());
var options = new RibbonButtonItem { Text = "Options", KeyTip = "O" };
options.Click += (_, _) => ShowOptions();
ribbon.Backstage.FooterItems.Add(options); // Bottom-aligned; empty by default.
ribbon.FileMenu.Visible = true;
ribbon.FileMenuMode = RibbonFileMenuMode.Backstage;
// Dispose application content after the ribbon/Form has released it.
form.Disposed += (_, _) => { infoContent.Dispose(); printContent.Dispose(); infoImage.Dispose(); };
```

`Backstage` is a stable, content-serialized owned component. Its `Items` and
`FooterItems` collections accept `RibbonBackstagePageItem`, plain `RibbonButtonItem`
commands and decorative `RibbonMenuSeparatorItem` entries. IDs,
binding contexts, change events and owned-model disposal follow the ribbon tree.
`Backstage.SelectedPage` is bindable; the first available page is initially
selected. Hiding, disabling, removing or disposing the selected page selects the
next available page, then the previous page, then none. Navigation focus and
commands do not change selection; commands close Backstage before invoking Click.

`FooterItems` is empty by default. Entries there stay at the bottom while the
main `Items` list scrolls above them. When the window is too short to retain the
bottom section and a main row, both sections share one scrollable list, in main
then bottom order. Separators use the existing theme Border color and nine
logical pixels of row height; they have no activation, focus or KeyTips. A model
still has one collection owner: remove it before moving it between sections,
preferably inside a ribbon update batch. Page content ownership and eager
`ContentType` initialization apply equally in both sections.

Navigation items reserve the same left icon slot with or without an image. Set
`Image` for an application-owned full-color bitmap/picture, or `Icon` for an
application-owned vector provider (`Icon` takes precedence when both are set).
The library scales that slot with DPI and borrows either asset. Showcase Info uses
a full-color information image; Print and the commands use vector icons.

Navigation labels use a 14-logical-pixel font minimum, matching WPF Modern
Backstage. `RibbonTheme.BackstageNavigationFontSize` adjusts that minimum; larger
ribbon fonts retain their size, family and style. Row height and Auto rail width
measure the same navigation font. Page content and the Back button retain the
host/ribbon font.

In the Visual Studio property grid, expand the ribbon's **Backstage** property,
then its **Layout** properties:

| Property | Default | Authoring behavior |
| --- | --- | --- |
| `NavigationWidth` | `Auto` | Enter a positive logical-pixel width, or choose/type Auto. Reset or an empty value also restores Auto. |
| `MinimumNavigationWidth` | `160` | Preferred lower limit, at least 80 logical pixels. |
| `MaximumNavigationWidth` | `320` | Preferred upper limit, at least 80 logical pixels. A larger minimum takes precedence, so designer assignments can occur in either order. |

These are designer-serializable, bindable component properties with Changed
events. Numeric values use 96-DPI logical pixels: 200 becomes 250 device pixels
at 125% scaling. Auto starts at 200 and measures visible captions, the reserved
icon slot, padding and scrollbar clearance, then applies the limits. A narrow
Form can temporarily display less than the preferred minimum to preserve page
space. Neither resizing the Form nor a DPI change rewrites the authored value.
No runtime drag handle or width-setting UI is added.

`Theme.BackstagePreferredMinimumPageWidth` optionally adjusts the preferred page
space used by the same navigation-width negotiation. It accepts 0..1024 logical
pixels; null retains each current light family's 240-pixel preference. Zero
reserves no preferred page space and can let wide navigation occupy the host.
This is a preference, not a guaranteed content minimum: very narrow hosts still
allow navigation up to half the available width. Auto and explicit navigation
widths retain their existing minimum/maximum rules and available-space clamps.
Resizing never rewrites those authored properties. Reset this theme member to
null independently of page padding, caption-button appearance and other settings.
The optional **Theme effects 11 → Backstage page space** group includes presets
and separate fixed/Auto navigation previews; the main sample's navigation-width
properties remain unchanged.

```csharp
ribbon.Backstage.NavigationWidth = 224; // Or null for Auto.
ribbon.Backstage.MinimumNavigationWidth = 160;
ribbon.Backstage.MaximumNavigationWidth = 320;
ribbon.Theme = ribbon.Theme with { BackstagePreferredMinimumPageWidth = 360 };
```

When binding the nullable width, use `formattingEnabled: true` and
`DataSourceNullValue = null` so Auto round-trips as null. The library owns
measurement; the application owns any settings storage. Property metadata, Auto
conversion and reset behavior are checked; an actual VS round-trip remains a
separate pending gate.

Set the bindable `IsBackstageOpen` to request visibility. File's existing
`DropDownOpening` cancellation runs first, followed by `Backstage.Opening`.
`Opened`, `Closing`, `Closed` and `IsBackstageOpenChanged` report the lifecycle;
`FileMenu.IsDropDownOpen`, `CloseDropDown()` and `DropDownClosed` also describe the
active File surface. Cancelled opening raises no closed event. Page validation
or `Closing` cancellation retains the current surface/mode. Hiding/disabling/
disposing File, Backstage or the ribbon, or changing the ribbon's parent/handle,
forces close without validation or Closing; an open surface still raises Closed.
Switching File mode closes its current surface while retaining both declarations.

Each Content must initially be an unparented, undisposed, non-top-level native
Control used by exactly one page declaration. The library borrows it, temporarily
docks it in a persistent page slot and retains that parent across page switches,
close and reopen. Values, established binding contexts, HWNDs and text selection
survive those operations. Do not reparent or dispose assigned content. Setting
Content to null/replacing it, removing the page, or disposing its page/ribbon/Form
detaches it, restores its authored Dock/Bounds/Visible and never disposes it.
The application must eventually dispose content assigned through `Content`.

For a designer-selected page body, use the optional `ContentType` instead. In
**Edit Backstageâ€¦**, select a page and choose its **ContentType** from the
**Page content** category. Build the consuming project first so its public
UserControl classes are available. Each class needs a public parameterless
constructor; edit its layout in its ordinary UserControl designer, including
TableLayoutPanel/FlowLayoutPanel children as needed. The saved declaration is
`infoPage.ContentType = typeof(InfoPageControl)`.

`ContentType` defaults to null. **All declared page classes are instantiated
during runtime ribbon initialization, before `Form.Load` and before File is
clicked**, including hidden and disabled pages. The designer-generated
`ISupportInitialize.EndInit` creates the instances, so their `Content` is available
after `InitializeComponent`. Normal runtime control creation also initializes
pages for code-built consumers without an init batch. Use BeginInit/EndInit for
hand-built or initially hidden ribbons to establish the same explicit boundary.
Types/pages assigned later are instantiated immediately after the model change,
or when an active BeginUpdate/EndUpdate batch finishes. Creating the instances
does not open Backstage, force HWND creation, or force hidden UserControl.Load
events; native handle/visibility/Load timing remains the normal WinForms lifecycle.

Instances are retained across switches and close/reopen. The page disposes its
instance when its type is reset/replaced or the page is disposed.
Removing a page releases its visual parent; dispose the removed declaration when
finished with it. Existing borrowed `Content` must be cleared before selecting a
type, and `ContentType` must be cleared before assigning borrowed content. Page
constructors run as application code during initialization and constructor failures propagate; designer
previews do not create them. Generated code saves the type rather than the
realized instance. Actual VS assignment/Undo/save-reopen remains a user review gate.

The overlay covers its ordinary Form's client area, leaving document Controls
in place. It follows client resizing and DPI changes, with scrollable navigation;
page-specific scrolling/styling remains with application content. Opening commits
pending ribbon edits and closes menus/choice lists/ScreenTips. Underlying ribbon
popups and editor activation are blocked while Backstage is open. Page changes,
commands and requested close validate visible enabled native page fields.
Back/Escape returns focus to the previous available control, or File. Escape first
closes an open native page ComboBox list. Tab/Shift+Tab cycle inside the surface;
arrows/Home/End move navigation focus, Enter/Space activate entries, F6 switches
navigation/page focus, and Alt/F10 displays navigation KeyTips. Covered Form default
buttons/mnemonics stay outside Backstage's dialog-key scope. Selected ribbon tab
and minimized mode survive opening/closing. Transitions remain immediate.

`DrawBackstageBackground` and `DrawBackstageEntry` are shared renderer hooks;
`BackstageNavigationBackground` supplies the initial Office 2024 rail color.
Rail/row/back metrics and appearance constants join M4I's deferred consolidation.
Showcase's File demonstrates Info, Print and application commands through these
same public APIs; document storage, print jobs and native page styling stay in
the application. This foundation adds no new accessibility provider/patterns.

Focused shown checks, using the normal Debug project runner after building:

```powershell
winforms/tests/RibbonKit.WinForms.NativeTests/bin/Debug/net10.0-windows/RibbonKit.WinForms.NativeTests.exe --backstage-checks winforms/TestResults/visual/backstage-office
winforms/tests/RibbonKit.WinForms.NativeTests/bin/Debug/net10.0-windows/RibbonKit.WinForms.NativeTests.exe --backstage-checks-flat winforms/TestResults/visual/backstage-neutral
```

The public-only fixture also ships with the independent consumer as
`--validate-backstage` / `--validate-backstage-flat`. Running the project fixture
does not establish new package, VS, accessibility or physical monitor acceptance.

The same runner also provides `--backstage-width-checks` /
`--backstage-width-checks-flat` for Auto, explicit/limited widths, narrow/wide
restoration, bindings and native page-state retention. Independent consumer names
are `--validate-backstage-width` / `--validate-backstage-width-flat`.

Contextual tools use ordinary `RibbonTab` components:

```csharp
var pictureFormat = new RibbonTab {
    Text = "Picture Format", KeyTip = "JP", IsContextual = true,
    ContextualColor = Color.FromArgb(196, 62, 150), Visible = false
};
ribbon.Tabs.Add(pictureFormat);
// When the application selects a picture:
pictureFormat.Visible = true;
ribbon.SelectedTab = pictureFormat;
```

`IsContextual` enables tinted text; `ContextualColor=Color.Empty` uses the current
theme accent. Unselected contextual labels are subdued; selected labels use the
full tint, including Office's selection underline. No extra header band is added.
Both properties support ordinary binding and change events. The application owns
context applicability through `Visible` and decides when to select a tab. Hiding,
disabling or removing a selected contextual tab uses normal selection fallback.
Showcase's Insert > Picture opens Picture Format; Close picture tools hides it.

Set `HighDpiMode.PerMonitorV2` before creating windows, and enable visual styles.
Add a `RibbonControl` to a normal Form; it docks to the top. Compose its get-only
`Tabs`, `Groups` and `Items` collections using components with stable explicit
IDs. New unspecified IDs are unique, but applications persisting identities
should author them explicitly. One ribbon per Form is the initial input scope.

- Collections own attached models. Removal detaches without disposing; disposing
  a model removes it, and disposing the ribbon disposes attached descendants.
- Images, vector icons and custom renderers are borrowed. Applications dispose
  those resources after their controls. The default renderer owns its caches.
- Change attached state on the owning UI thread. Use `BeginUpdate`/`EndUpdate`
  for batches; `ISupportInitialize` maps to the same contract.
- `PerformClick`, mouse, keyboard and UIA share activation. Toggles update Checked,
  then raise CheckedChanged, then Click. Assigning Checked does not raise Click.
- `DataBindings` uses ordinary WinForms Binding/BindingSource. No implicit
  ItemsSource/MVVM adapter or WPF command contract is included.
- Override the public `RibbonRenderer` for painting, or supply borrowed
  `IRibbonIcon` providers. Geometry, hit testing and accessibility remain library
  owned. The mirrored-corner painter avoids independent per-corner arc paths;
  current pixel checks do not substitute for native visual review.

### SVG icon sources

```csharp
copyButton.Icon = RibbonStockIcons.Create("copy");
pictureButton.Icon = new RibbonSvgIcon(svgXml); // Retain the artwork's colors.
monoButton.Icon = new RibbonSvgIcon(svgXml, useForegroundColor: true);
```

SVG data must be self-contained static artwork with a positive viewBox. The
adapter draws vectors at the requested device bounds; it does not take ownership
of application images or custom providers. No additional resource owner or
Showcase helper is needed. The built-in SVG converter supports generated
constructors and string-backed .resx resources; opaque custom providers remain
code-authored. See [the complete bounded SVG contract](docs/SVG-ICON-AUTHORING.md).

## Minimize and temporary tab bodies

`RibbonControl.IsMinimized` is a bindable, designer-serializable Boolean with a
default of false and an `IsMinimizedChanged` event. Ctrl+F1, the tab-row chevron,
and double-clicking a tab toggle it. Holding Ctrl+F1 changes the mode once;
keyboard input in another Form does not toggle this ribbon. Applications can use
ordinary WinForms binding to store their preferred mode.

```csharp
ribbon.IsMinimized = true;
ribbon.IsMinimizedChanged += (_, _) => SaveRibbonMode(ribbon.IsMinimized);
```

Minimized mode reserves only the tab-row height. Clicking a tab, pressing
Down/Enter/Space on a focused tab, or choosing its KeyTip opens a temporary body
below the row. It uses the same groups, adaptive sizes, split actions, scrollers
and native editor instances. Selecting another tab replaces that body. Commands
close the transient stack; Escape closes one level at a time and returns to the
opener. Outside input, owner changes, host movement/resizing and deactivation
dismiss stale presentations. The selected tab and command values remain model
state; opening a temporary body does not change `IsMinimized`.

User toggles commit the focused native editor and stop if validation is canceled.
An authoritative property/binding change uses the existing parking contract:
valid edits commit, invalid drafts roll back, and native selection is preserved.
Mode changes preserve document focus when the ribbon does not own focus.

`DrawMinimizeButton(context, minimized)` receives the shared measured utility
slot. The chevron occupies 28 logical pixels at the right of the tab row; tab
overflow excludes that opaque slot. Tab scrollers shrink on very narrow rows to
retain up to 24 logical pixels of tab space. A temporary body is at least 200 logical pixels wide
when the work area permits, so a very narrow docked ribbon can still open its
groups. These local dimensions belong in the deferred theme inventory.
Minimized-tab and utility accessibility compatibility remains deferred.

## Compact check and radio options

`RibbonCheckBoxItem` and `RibbonRadioButtonItem` are compact option rows with
bindable `Checked` and `CheckedChanged`, followed by the normal activating `Click`.
Mouse, Space/Enter, KeyTips and `PerformClick` use that same path; programmatic
state assignment raises no Click. Inherited size/image settings do not replace
the compact indicator and label.

Checkbox activation in an ordinary or nested dropdown keeps the same menu open,
including menus opened through a QAT reference. Mouse and Space/Enter toggle the
source state before Click while retaining native editor instances. Escape,
ordinary commands, explicit application closure and other menu-choice dismissal
retain their existing behavior. Application Menu-specific activation is unchanged.

Checkboxes also expose bindable WinForms `CheckState` and `CheckStateChanged`.
With `ThreeState = true`, activation cycles Unchecked â†’ Checked â†’ Indeterminate.
`Checked` is true for both Checked and Indeterminate. For an enum-valued source,
use `formattingEnabled: false` in its CheckState Binding: formatted WinForms
binding maps Indeterminate to a null source value. Bind `Checked` for boolean data.

Radio items select themselves and clear their peers. An empty `GroupName` groups
siblings in the same logical item collection; a nonempty name spans that ribbon,
including menus and inactive tabs. Detached named groups share their logical
tree. Repeated activation retains selection. Programmatic clearing permits no
selection; removal does not select a replacement. Attaching a selected item or
changing its group normalizes selection in the new scope. Hidden and disabled
peers are cleared too, while their own input activation remains disabled.

`DrawCheckBoxIndicator` and `DrawRadioButtonIndicator` receive measured indicator
slots through the shared renderer. Office uses softly rounded checkboxes; neutral
uses square ones. Both use circular radio indicators. The Showcase's Options tab
demonstrates checkbox binding, mixed state, radio choices, menus and collapse.
Accessibility compatibility remains deferred.

## Native TextBox items

`RibbonTextBoxItem` uses `Text` as its label and bindable `Value` as its live text.
`InputWidth` is the editor width in logical pixels (minimum 24, default 120),
excluding the label. It always uses one compact row; inherited command size/icon
settings do not change its presentation. Stacked fields in a column share a
label span. `ReadOnly` keeps native selection/copy; `MaxLength` limits user input,
with zero meaning unlimited. Programmatic values are not truncated.

Bind `Value` through the item's ordinary `DataBindings`. `OnPropertyChanged`
writes live edits; `OnValidation` waits for a successful commit. Handle
`ValueValidating` and set `Cancel` to reject a commit. Successful validation writes
deferred bindings, raises `ValueValidated`, then `ValueCommitted` for changed
values. The distinct event name prevents Component bindings from writing before
the application's validation handlers finish.

Enter commits and keeps editing focus. Tab/Shift+Tab validate before navigation;
F6 returns to the document. Escape restores the focus-entry/last-committed value,
raises `EditCanceled` when it changes, and returns to the ribbon or closes one
popup level. With live binding this also writes the restored value back. External
value changes become the new rollback baseline. Forced hiding, disabling or popup
closure commits valid edits and rolls back rejected ones. Removal disposes only
the native presentation; the detached model retains its value and bindings.

`PerformClick()` or a KeyTip focuses the active editor without replacing its
selection. The library owns one real TextBox per item and transfers it into/out
of collapsed-group popups; applications must not reparent or dispose that child.
Ordinary typing, caret movement, selection and clipboard commands remain native.
The renderer's `DrawTextBox` callback paints the label/frame around the native
text area. Native editor appearance is opaque; accessibility compatibility and
actual VS authoring remain unverified.

## Native ComboBox items

`RibbonComboBoxItem` uses the same label, `InputWidth`, live `Value`, validation,
commit/cancel and binding conventions as TextBox. Its owned `Items` collection
contains strings; `SelectedIndex` is bindable, with -1 meaning no selected choice.
Selection-only mode is the default. `IsEditable=true` allows free text, preserving
its exact case and using index -1 when it does not match an authored choice.
An unmatched Value in selection-only mode clears the selection and text. Choice
mutations preserve a surviving selection, update its index and close a stale list.
This slice has no object DataSource/DisplayMember/ValueMember adapter.

Set `InputWidth` in the designer's Layout properties to widen a ComboBox or
TextBox. It defines the Compact field width, excluding the label. Touch scales
both field types' widths by their height increase, using the current font and DPI.
For example, a height increase from 24 to 42 pixels gives a width factor of 1.75.
The wider arrow's extra space remains a minimum increase so large fonts retain
the Compact ComboBox text budget. Equal authored widths use the same scaling.
Switching density leaves the authored `InputWidth` unchanged. Small Touch command
icons center within their command area, with dropdown/split arrows kept separate.

`PerformClick()` and KeyTips focus the editor. `ShowDropDown()`, F4 or Alt+Down
opens a library-owned choice popup containing a native ListBox; `CloseDropDown()`
cancels its pending selection. The Windows ComboLBox stays closed.
Mouse selection and closed-list keyboard selection validate and commit once.
Enter accepts a list selection; Escape cancels only that list first, then the next
Escape restores the editor's committed value and exits one ribbon popup level.
Tab validates before navigation. Live binding sees preview and rollback values;
deferred binding writes only after successful validation.

Mixed TextBox/ComboBox rows share label spans. The library transfers one native
ComboBox per item into/out of popups, retaining editable selection. `DrawComboBox`
paints the label, border and arrow: rounded in Office 2024 and square in the flat
renderer, with hover, pressed and open states. Native text input is clipped inside
that frame. `DrawComboBoxItem` paints each native list row; rows are at least 28
logical pixels tall and grow with the font. The closed field stays compact and
aligned with TextBox. Native text/caret/selection are retained.
Layout, model, tab and visibility changes synchronize native editors. Hover and
pressed-state painting leave their bounds and text selection alone.

The ListBox supplies scrollbars, wheel scrolling and incremental search; the
library coordinates preview, validation, keyboard navigation and dismissal.
A stationary pointer cannot replace
typed text or keyboard selection during repaint. The popup fits the screen work
area, opens above the field when needed, and shows up to eight rows before scrolling.
Outside presses and deactivation cancel unaccepted preview. Escape closes the choice
popup before its containing ribbon popup. Actual VS authoring and accessibility compatibility
remain unverified. Inputs shows
selection-only Font, editable Zoom, a disabled chooser and a popup chooser using
ordinary application-owned data and validation.

## Quick Access Toolbar

`QuickAccessItems` owns toolbar-only `RibbonButtonItem`/`RibbonToggleItem` commands
or `RibbonQuickAccessReference` declarations borrowing existing buttons, toggles,
checkboxes or radio options:

```csharp
ribbon.QuickAccessItems.AddRange(
    new RibbonQuickAccessReference { Source = copy },
    new RibbonQuickAccessReference { Source = bold },
    new RibbonButtonItem { Text = "Save", Icon = saveIcon });
ribbon.QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon;
```

The default placement is `TabRow`, before the tabs. `BelowRibbon` adds a row
beneath the body, or beneath the tabs while minimized. Office 2024 uses the body's
side margins and one continuous frame/shadow, with a thin internal separator and
the bottom corners beneath the QAT. While minimized, it becomes a standalone
rounded card with the body's top/bottom clearance. Neutral rendering retains its
separate full-width row. `QuickAccessMaxWidth`
defaults to 240 logical pixels and caps the tab-row and title-bar placements.
Overflow opens a library-owned command menu. Long QAT menus use separate opaque up/down scroll
bands, with hover/pressed feedback, disabled endpoints, row-wise clicks and
hover/held repeat. Short menus reserve no bands; native scrollbars are absent.
The wheel and keyboard keep commands reachable, and scrolling pauses while a
child menu or chooser is open. Arrows/Home/End/Tab navigate it; Enter/
Space activate, and Escape returns to the opener. F10/Alt shows automatic numeric
entry KeyTips (preferred 1-9, then Q1, Q2, ... with equal-width numeric suffixes
for larger collections); 0 opens overflow. These preferences and explicit entry
`KeyTip` values participate in the existing conflict resolver, which may substitute
a key and reserves the 9 prefix on large levels.

Custom renderers choose this layout through `JoinBelowRibbonQuickAccess` (false
on the base renderer, true on Office 2024). When joined, `DrawBody` receives the
combined body/QAT frame; `DrawQuickAccessToolbar` receives its measured row with
`IsQuickAccessToolbarJoined` true and paints the internal separator. Minimized
rows are false and Office paints their standalone card. Reassign `Renderer` after
changing a mutable renderer's layout policy to refresh measurement. Existing
surface/border/corner/shadow theme roles supply the appearance. Future message
panels will square the adjoining QAT bottom edge; that component remains planned.

Toggle, checkbox and radio entries and the primary half of a `CheckOnClick` split keep QAT overflow
open, retaining focus for repeated mouse, Space, Enter or KeyTip activation.
Ordinary commands and nonchecking split primaries dismiss overflow. Escape
still closes it.

References retain separate IDs, visibility and optional `Text` caption overrides.
They read source label, borrowed icon, checked/enabled state and ScreenTip metadata
without moving or copying the source. An inactive tab does not prevent activation.
A hidden/disabled ancestor, hidden/disabled source, detached source or disposed
source disables the declaration in place. Reinserting the same live source in
the same ribbon restores it. A reference never owns or disposes its source;
directly declared commands are owned by `QuickAccessItems`. Changing
`QuickAccessVisible` retains placement and order. Collection Insert/Remove supports
application-authored ordering. The M5 engine supplies detached QAT customization,
persistence and the shared customization menu/editor.

Showcase starts with Bold, Italic and Mouse/Touch mode. Its context menu changes
toolbar placement and visibility; other permanent commands can be added through
customization. Narrow the window to open overflow. Buttons, toggles, checkbox/radio references, ComboBox choosers and
dropdown/split/group references are supported. New QAT
accessibility support and actual VS save/reopen remain pending. Model ownership,
row measurement and painting are shared by all three placements. The optional
`RibbonForm` hosts TitleBar without replacing declarations or moving their sources.

Checkbox/radio references use the source's borrowed `Icon`, `Image` or `LargeImage`
with the same checked/hover/pressed button chrome as toggles. Without an icon,
the strip uses the caption's first text element; overflow shows its full caption.
An indeterminate checkbox adds a small bar below the action icon. Ordinary body
and group-flyout checkbox/radio indicators keep their existing presentation.

```csharp
gridlines.Icon = gridIcon;
printLayout.Icon = printIcon;
ribbon.QuickAccessItems.AddRange(
    new RibbonQuickAccessReference { Source = gridlines },
    new RibbonQuickAccessReference { Source = printLayout });
```

Activation uses the original source once, preserving its checkbox state cycle,
binding and event order. Radios retain their original named/unnamed group;
clicking a selected radio keeps it selected. These options are borrowed references;
directly adding checkbox/radio models to `QuickAccessItems` remains unsupported.
WPF's corresponding extension is deferred in the
[future WPF improvements plan](../docs/22-FUTURE-WPF-IMPROVEMENTS.md).

`RibbonQuickAccessComboBoxReference` adds a chooser for an existing ComboBox:

```csharp
ribbon.QuickAccessItems.Add(
    new RibbonQuickAccessComboBoxReference { Source = fontInput });
```

It shares the source choices, selection, bindings, validation and ScreenTip, and
works while the source tab is inactive. It opens at its own QAT button, or beside
its row in QAT overflow. Its `IsDropDownOpen` reports that presentation's popup;
the source field retains its own open state. An editable source is still a chooser
in QAT; typing stays in its original native field. No second editor is created.
Showcase demonstrates Font and Zoom, including Zoom's unmatched typed percentages.

Pointer hover highlights a choice without changing selection or binding. A matching
row press/release accepts it; dragging to another row cancels the press. Leaving
clears pointer highlight. The actual selected/keyboard-preview row uses the checked
fill; pointer hover uses the hover fill. `DrawComboBoxItem` receives those states
separately through its `selected` argument and context `Hot`/`Pressed`. Keyboard
arrows and native incremental search retain tentative preview, source validation
and Escape rollback. This pointer policy applies to ordinary field choice lists
as well as QAT choosers. List widths measure text and both separately rounded DPI
insets, rather than assuming the small QAT button can fit a choice label.

`RibbonQuickAccessDropDownReference` borrows a dropdown or split button:

```csharp
ribbon.QuickAccessItems.AddRange(
    new RibbonQuickAccessDropDownReference { Source = selectMenu },
    new RibbonQuickAccessDropDownReference {
        Source = pasteSplit, KeyTip = "P", DropDownKeyTip = "V" });
```

Its menu opens beneath the QAT entry or beside its overflow row, including when
the source tab is inactive. Commands, checked state, handlers and native editors
stay with the original source. A split source has separate horizontal primary
and menu targets in both presentations: the primary calls the source once, while
the arrow opens its menu. Default split KeyTips append A/B to the entry number
(for example 3A/3B); explicit `KeyTip` and `DropDownKeyTip` values use the same
conflict resolver. Custom renderers receive both halves through
`DrawQuickAccessSplitItem`.

`IsDropDownOpen` belongs to each presentation. Only one presentation of the same
source menu opens at once; native editor instances are retained across them.
The source's cancellable `DropDownOpening` and `DropDownClosed` events also run
for its references, and `source.CloseDropDown()` closes its current presentation.
Opening handlers may populate source items before the menu is measured. Nested
KeyTips and Escape preserve the popup stack. Disabling, removing or replacing a
reference/source closes stale menus without transferring source ownership.

`RibbonQuickAccessGroupReference` opens an existing group's native flyout:

```csharp
ribbon.QuickAccessItems.Add(
    new RibbonQuickAccessGroupReference { Source = formattingGroup });
```

The source group stays collapsed for the entire attached QAT membership,
including hidden/disabled references or a hidden toolbar. This derived constraint
also applies when `CanResize=false`; its authored reduction settings remain intact.
Removing the last reference releases the constraint and restores normal adaptive
sizing. Groups belong only in future Customize QAT's available list, not Customize
Ribbon's command list.

The compact caption-initial/arrow entry opens below the strip or beside its
overflow row, including inactive source tabs and minimized/below-ribbon placement.
Commands, handlers, values and bindings remain source-owned. Only one presentation
of a group opens at once; switching between references and its collapsed source
opener reuses the same native editor controls. Detaching the group follows the
existing editor contract: native presenters are disposed, while models, values
and bindings survive for reinsertion. `IsDropDownOpen` describes each reference.
Source/reference disable, replacement, removal and disposal close stale flyouts.

Toggles, checkboxes, radios and split primaries with `CheckOnClick=true` keep
group and More Commands/QAT overflow popups open. This includes ordinary
collapsed groups, Simplified full-group flyouts and QAT groups. Ordinary commands
and nonchecking split primaries dismiss them; split arrows open their own menus.
KeyTips and nested Escape navigation
use the original command level and restore each opener. Showcase's Input fields
reference demonstrates this with the Fields group. Group-specific icon/ScreenTip
metadata is not added in this slice; custom renderers can override
`DrawQuickAccessItem` for the group declaration.

### Optional RibbonForm and title-bar QAT

```csharp
// The application still sets PerMonitorV2 at startup and owns its document.
var window = new RibbonForm { Text = "My application" };
var ribbon = new RibbonControl { Dock = DockStyle.Top };
window.Controls.Add(document); // Application-owned, usually DockStyle.Fill.
window.Controls.Add(ribbon);
window.Ribbon = ribbon;
ribbon.QuickAccessPosition = RibbonQuickAccessPosition.TitleBar;
```

`RibbonForm.Ribbon` borrows a direct, top-docked ribbon. It does not add, move or
otherwise own command declarations. Use empty Form padding and no other visible
top-docked sibling for caption integration; document controls remain ordinary
application controls. Normal Form child-control disposal rules still apply.
Removing/reparenting the ribbon or changing its Dock releases the association.
Setting `window.Ribbon = null` detaches it without disposing the ribbon or sources.

`QuickAccessPosition` retains the requested preference, including while hidden.
`EffectiveQuickAccessPosition` reports what is displayed. TitleBar uses TabRow in
an ordinary/unsupported Form or when caption buttons leave no room; it returns
to TitleBar when support/space returns. Unsupported padding or sibling docking
uses an ordinary ribbon below the separately reserved caption. Other border
styles retain their own native window arrangement.
Existing TabRow/BelowRibbon profiles and numeric enum values are preserved;
versioned JSON now accepts the TitleBar name without a schema change. The menu
offers TitleBar and an explicit TabRow command only when a caption host exists.
Customize QAT continues editing commands; placement/visibility remain menu choices.

The library measures caption painting, native hit targets, QAT targets, keyboard
bounds and popup anchors in device pixels. Caption drag/double-click, edge resize,
system menu, minimize/maximize/restore/close and native Snap use Win32 window
semantics. Interactive or disabled QAT bounds exclude caption dragging. Backstage
leaves the caption accessible; messages and temporary minimized bodies retain
the existing frame/overlay rules. The same menu/gallery/group/ComboBox projections,
validation, KeyTips, overflow and editor handles serve every placement.

**First-pass motion limit:** moves into/out of TitleBar settle immediately.
TabRow/BelowRibbon movement retains its existing opt-in animation; the caption
band stays steady during body/header motion. Document/MDI management remains
application-owned. [M6B scope and evidence](docs/M6B-WINDOW-INTEGRATION.md).

Select **TitleBarColor** on the Form's Properties panel to choose its caption
color. Reset follows `ribbon.Theme.TitleBarBackground`, whose default retains the
existing appearance. With Mica/Acrylic, the color tints the retained material;
`TitleBarTintOpacity` controls coverage (default `.6`). Without a backdrop, the
color is solid. Caption text/buttons and TitleBar QAT choose black/white contrast;
`Theme.TitleBarForeground` can override it. A theme with
`ExtendTitleBarColorToHeader` (Office 2019 Light) also colors the tab band and
contrasts its inactive labels, TabRow QAT and utilities. BelowRibbon QAT and
document controls retain their own palette. Office 2024 retains its caption-only
coloring. Close hover keeps its red/white treatment.

```csharp
window.TitleBarColor = Color.DarkSlateBlue;
window.TitleBarTintOpacity = .6f;
// Reset the Form override, following the theme again:
window.TitleBarColor = Color.Empty;

ribbon.Theme = ribbon.Theme with
{
    BodyBackground = new RibbonFill(Color.White, Color.AliceBlue),
    BodyBorder = Color.LightSteelBlue,
    PopupBackground = Color.Ivory,
    EditorBackground = Color.White,
    EditorForeground = Color.DarkSlateGray,
    EditorBorder = Color.SlateGray,
    MenuHeadingBackground = new RibbonFill(Color.AliceBlue),
    MenuHeadingForeground = Color.DarkSlateBlue
};
```

Unset roles retain the existing palette. Body fill stops, popup/editor backgrounds
and caption colors are opaque; material caption coverage uses the separate opacity
setting. [Color roles, fallbacks and review scope](docs/APPEARANCE-COLOR-ROLES.md).
Theme roles do not recursively recolor application-owned UserControls.

### Native window-corner preference

`RibbonTheme.WindowCornerPreference` selects `SystemDefault`, `Square`, `Rounded`
or `SmallRounded`. Office 2019 uses Square; Office 2024 uses SystemDefault.
`RibbonForm.WindowCornerPreference` is a nullable override: an explicit value wins,
and null/property-grid Reset follows the associated ribbon theme (SystemDefault
without a ribbon). `EffectiveWindowCornerPreference` reports that resolved request,
not native success or visible appearance.

```csharp
window.WindowCornerPreference = RibbonWindowCornerPreference.SmallRounded;
window.WindowCornerPreference = null; // Reset to theme ownership.
ribbon.Theme = ribbon.Theme with { WindowCornerPreference = RibbonWindowCornerPreference.Square };
```

The supported DWM preference is applied on Windows 11 build 22000 or later,
including handle recreation and theme/override changes. Unsupported systems keep
the request; designer and child handles remain inert. Windows owns the final
silhouette. Native resizing, caption actions, backdrops and control/popup radii
remain independent. [E2 evidence and visual-review limits](docs/OFFICE2013-LIGHT-PLAN.md#e2-implementation-and-verification--2026-11-13).

### Optional native window backdrops

**Experimental.** [Current evidence, user review and remaining platform checks](docs/M6C-BACKDROPS.md#first-show-material-alpha-and-windows-opening-motion--2026-10-30).
The following API describes the experimental implementation.

`RibbonForm.Backdrop` defaults to `RibbonWindowBackdrop.None`. Opt into `Mica` or
`Acrylic` to use native material across the form background and behind the ribbon.
The host application must declare Windows 8-or-later compatibility in its
application manifest for layered native child editors; PerMonitorV2 startup alone
does not supply that declaration. See the permanent Designer Sample's
[manifest](samples/RibbonKit.WinForms.DesignerSample/app.manifest). The isolated
designer consumer carries the same compatibility declaration.
The ribbon's main body remains opaque:

```csharp
window.Backdrop = RibbonWindowBackdrop.Mica;
var applied = window.EffectiveBackdrop;

// Application-owned GDI content keeps an opaque, composited native surface.
var content = new RibbonBackdropPanel { Dock = DockStyle.Fill };
content.Controls.Add(documentControl);
window.Controls.Add(content);
window.Controls.SetChildIndex(content, 0); // Fill the space remaining below the ribbon.
```

The library owns the backdrop and client-alpha setup after opt-in. It
reapplies them after handle recreation and DPI/system changes. Windows 11 build
22621 or later is required. Unsupported, high-contrast, borderless, child/layered
windows and application opacity/transparency use the solid fallback while retaining
the requested preference. `EffectiveBackdrop` reports the applied native setting;
Windows may itself render that setting as solid according to its material policy.
Setting None restores solid presentation and releases the library's setup.
On Windows 11 build 26100 or later, material uses the documented redirection-bitmap
alpha attribute with zero frame extension, preserving the original native caption
style and Windows opening animation. Earlier supported Windows versions, or an
unavailable alpha attribute, use the extended-glass path with caption suppression.
Solid retains the original native caption style. Material changes preserve the
Form handle; appearance on older Windows still requires native review.

Command panels, popups, native editors and Backstage pages remain opaque. The
authored theme and application Opacity/TransparencyKey are unchanged. Host native
GDI document/content controls inside `RibbonBackdropPanel`; it preserves opaque
text and child drawing over the material. It uses layered-child composition and
requires the same Windows 8-or-later compatibility manifest as ribbon motion.
The application owns the panel, its controls and their layout. It can use
`BackdropChanged` to update its own margins; no host handler completes animations.
Material mode keeps the real caption/tab strip live and commits its geometry
immediately while body/group motion continues. Solid mode retains its existing
header animation. This prevents layered animation holds from blackening the
extended native frame.

Material QAT/header utility and Backstage entry states use
`RibbonTheme.BackdropHover` / `BackdropPressed`. Null uses the theme foreground at
alpha 24 / 40, producing a darkening tint for light themes and a lightening tint
for dark themes. Checked QAT entries instead use Accent at alpha 40 unless an
explicit pressed fill is supplied. QAT hover/pressed/checked and overflow/chevron
fills additionally multiply alpha by `QuickAccessBackdropOpacity` (default `.6`);
`1` keeps the authored state fill's alpha. Artwork stays opaque. Solid mode and
opaque command panels retain their existing state fills. Office File uses the
same hover underline as a tab in both modes. Unselected material tab/File
indicators multiply their alpha by `BackdropTabHoverOpacity` (default `.45`);
selected-tab accents retain full coverage. Backstage navigation is opaque by
default. Opt into translucent navigation with
`ribbon.Backstage.NavigationBackgroundOpacity = 0;` while leaving native page
controls opaque. Both values accept finite numbers from `0` through `1`; Solid
fallback remains opaque. Showcase exposes **Transparent Backstage** in its Backdrop
menu when material is active; this option uses a fully clear rail background.
Neither setting changes application window opacity.

Solid and material Backstage share the full-surface slide/fade policy. Native
material layers retain page fields throughout motion. Explicitly sliding pages
discard clipped cached pixels to prevent a black edge strip. Reopening restores
the open location; input and unsafe changes settle the motion. See the
[current theme-specific caption and title-motion evidence](docs/THEMES-PLAN.md#theme-specific-title-bars-and-short-title-slide--2026-11-10)
and [earlier slide implementation](docs/M4I2-APPEARANCE-INVENTORY.md#whole-surface-backstage-and-caption-slide--2026-10-30).

Solid Office 2024 Backstage pages sit flush below the title bar with a thin grey top/
left `Border` and only their top-left corner rounded. There is no reserved grey
shadow band. `GetBackstagePageBorderWidth` and `GetBackstagePageCornerRadius` share
the frame dimensions with the native page clip; other corners stay square.
Material mode keeps the soft left/top shadow and six-logical-pixel clearance,
using `BodyShadowColor` and `GetBackstagePageShadowInset`. Neutral stays flat.
The page frame does not paint into the caption or add a bottom/right edge.

Office 2019's real navigation/page backgrounds continue behind the transparent
`RibbonForm` caption, including navigation opacity over material. Office 2024 keeps
its ordinary title bar above Backstage. The title-bar QAT is hidden while Backstage
is open and returns on closing, preserving its authored theme, toolbar preferences
and window style.

Custom renderers receive a transparent paint-time `Theme.Background` for the material
surrounding ribbon background without changing `ribbon.Theme` or `Theme.Surface`.
Honor that background and delegate composited
text to the base `DrawText` method to preserve glyph alpha. The neutral and Office
renderers use the same library path. Host content, persistence and DPI startup stay
application-owned. Showcase exposes **View â†’ Window â†’ Backdrop**; its default is
Solid on first use. The selected backdrop and transparent Backstage choice are
saved in Showcase's application preferences and restored on restart. Its document
and status bar use the shared opaque panel, with material
visible in the application-owned surrounding margins.
[M6C scope, focused evidence and remaining review](docs/M6C-BACKDROPS.md).

## Richer command menus

`RibbonItem.MenuDescription` adds localizable inline text below a command-menu
caption, displaying at most two wrapped lines in the secondary text color. The
full string stays in the model. This property is independent of `ScreenTipDescription`;
native editors do not display descriptions. Strip and group-flyout command columns
retain their own layout. Descriptions also work on toggles, checkboxes, radio choices,
submenus and either layout of split buttons.

Each command popup gives all visible interactive rows the tallest measured row
height, including disabled commands and native field rows. Headers and separators
keep their own heights; hidden commands do not contribute. Each nested popup
measures independently. QAT overflow uses the same rule. Native field frames keep
their density-appropriate size and center inside the shared row height.

Touch menus use wider minimums, larger icon/arrow slots and more vertical padding
while keeping the authored font size. Described Touch rows reserve both available
description lines. Optional DropDownHeader text receives the shared themed heading
treatment; Showcase supplies headings such as Paste options and Selection options.

```csharp
var formats = new RibbonDropDownItem { Text = "Format" };
formats.Items.AddRange(
    new RibbonButtonItem {
        Text = "Keep source formatting",
        MenuDescription = "Preserve the source document's fonts and paragraph styles."
    },
    new RibbonMenuSeparatorItem(),
    new RibbonButtonItem { Text = "Text only" });
```

`RibbonMenuSeparatorItem` belongs only in a dropdown's `Items`; group collections
reject it before changing ownership. It has no activation, focus, KeyTips or
ScreenTip. The dropdown collection editor offers separators alongside ordinary
items. `DrawMenuSeparator` paints the shared one-pixel hairline, and
`DrawSubmenuArrow` paints a right-pointing chevron; `DrawText` receives the complete
description and its measured slot. Both Office and neutral renderers share this
content layout.

Nested command menus open beside their row, flip left near the screen edge and
stay inside the work area. Right opens a submenu; Left/Escape closes one level
and restores its opener. Decorative, hidden and disabled rows are skipped during
navigation. Scrolling a parent closes its children before their anchors move.
Collection/property changes dismiss stale popups through the existing model path.
Alt+F4 from an owned popup requests closing its host Form. RibbonKit first cancels
choice/gallery previews and dismisses the popup chain through its ownership path,
then calls the Form's normal close handling. FormClosing may cancel; retained native
editors and typed values remain available when menus reopen. Escape retains its
existing one-level dismissal behavior.

### UserControl popup bodies

Ordinary dropdowns and split buttons can display a designer-editable UserControl:

```csharp
options.ContentType = typeof(MyOptionsControl);
```

Choose **ContentType** in Edit Ribbon or the component Properties view after
building your project. Ribbon initialization creates the body before Form.Load;
`options.Content` returns it. The item owns and retains this instance across source
and QAT openings. Its native width/height plus popup padding determine the popup
size, bounded by the monitor; application content owns any internal scrolling.
No gallery presenter interface is required.

Alternatively assign an unparented application-owned Control to **Content** from
code. It remains borrowed and is never disposed by the ribbon. Content and
ContentType are mutually exclusive. A body can belong to only one dropdown or
Backstage declaration. Do not reparent it while assigned.

Custom content replaces the visible menu rows. The Items collection is retained;
clear ContentType/Content to restore it. Native controls retain text, selection
and handles on reopening. Apply/Enter actions and validation belong to the
application; call the source's existing CloseDropDown method to dismiss. Tab
traverses native children, and Escape dismisses a native ComboBox list before the
popup. A split button's primary action is independent. Gallery items continue
using their separate presenter properties.

The permanent Designer Sample's **Home â†’ Select** and **Paste arrow** demonstrate
this with `PopupOptionsControl`. Edit that body using its normal UserControl
designer. [Contract, evidence and remaining review](docs/DROPDOWN-CONTENT-PLAN.md).

In **Edit Ribbon**, select a dropdown/split button or menu child and choose
**Preview popupâ€¦**. Click rows to select their Properties, use arrows to preview
submenus, and use **Parent menu** to return. A selected dropdown/split also offers
**Edit popupâ€¦** in designer actions. Fields are static and UserControl bodies show
a placeholder; previewing runs no command or application control. Existing
OK/Cancel and Undo/Redo behavior is retained.
[Preview workflow and verification](docs/POPUP-DESIGNER-PREVIEW.md).

## Passive ScreenTips

Every `RibbonItem` supports localizable, designer-editable `ScreenTipTitle` and
`ScreenTipDescription` strings. An empty title uses the complete `Text` caption,
including captions shortened with an ellipsis on screen. An empty description
gives a title-only tip; an item with no title/caption or description has no tip.
These properties are separate from command state and accessible descriptions.

```csharp
var find = new RibbonButtonItem {
    Text = "Find matching text throughout this document",
    ScreenTipDescription = "Search the current document."
};
var unavailable = new RibbonButtonItem {
    Text = "Export", Enabled = false,
    ScreenTipDescription = "Open a document before exporting."
};
```

Pause over an item for approximately 600 ms to show its tip. Buttons, toggles,
dropdowns, both split-button halves, check/radio options and native TextBox/
ComboBox item slots share this behavior on the ribbon, command menus and
collapsed-group popups. Disabled items may describe themselves without gaining
hover/press visuals or command execution. Split halves use one item-level tip.
The tip takes no focus or capture and preserves native editor selection.

Leaving the item, keyboard/press/wheel input, command or menu activation,
model changes/removal, layout/DPI changes, owner movement/deactivation, hiding
and disposal cancel pending/visible tips. After keyboard/press cancellation,
real pointer movement is required before another hover starts. Placement fits
the monitor working area and flips above the item near its bottom edge. Tips
remain above library menus without activating them. Very large descriptions are
clipped to the available working area; this first version has no scrolling.

`RibbonRenderer.DrawScreenTip` receives the full title/description and measured
content rectangles. By default it draws a bold title and optional wrapped description using
the ribbon font and theme foreground. Existing `DrawPopupFrame`,
`GetPopupCornerRadius` and `DrawPopupShadow` hooks supply the shared frame:
rounded Office or square neutral. Another Form can use these public APIs without
Showcase resources. Images, help links, pinned/interactive tips, keyboard-triggered
help and per-choice ComboBox row tips are later work. While a choice list is open,
item tips are suppressed. Accessibility compatibility remains deferred.

The Showcase starts on Home: Find demonstrates full-caption fallback, Copy/Paste
authored descriptions and Unavailable a disabled explanation. Inputs and its
menu demonstrate native editors; Options supplies check/radio examples.

## QAT customization foundation

`ribbon.CommandCatalog.GetAvailable(RibbonCustomizationTarget.QuickAccess)` lists
supported command sources and whole groups. The `Ribbon` list excludes groups.
Catalog entries borrow source models; labels/state stay shared. Popup-only,
File/Backstage/message and transient merge/modal content is excluded. Contextual
authored commands remain discoverable even when their tabs are currently hidden.

Declare application-only commands in `CommandCatalog.Commands` when they need no
ordinary ribbon slot. Both customization lists can still find them after QAT
removal. Legacy directly owned QAT buttons removed through customization are moved
into that collection without replacing their handlers or bindings. Raw collection
Remove retains its normal detach semantics.

Assign explicit stable `Id` values to all selected sources before saving. Runtime
IDs work for in-process drafts but are rejected for JSON persistence. Do not derive
source identity from display labels; changing IDs requires application migration.

```csharp
var undo = new RibbonButtonItem { Id = "cmd.undo", Text = "Undo" };
undo.Click += (_, _) => UndoDocument(); // Application behavior.
ribbon.CommandCatalog.Commands.Add(undo);
ribbon.CommandCatalog.CaptureDefaults(); // After authoring, before loading preferences.

using var edit = ribbon.CommandCatalog.BeginCustomization("com.example.editor");
edit.Draft.QuickAccess.Add(new RibbonCustomizationEntry { CommandId = undo.Id });
edit.Draft.QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon;
if (edit.Apply()) SavePreferences(edit.ExportJson()); // Application owns storage.
edit.Cancel(); // Discards later unapplied edits; it keeps the latest successful Apply.
```

Draft membership/order and toolbar placement/visibility change live models only
through Apply. Invalid focused ribbon input blocks it. Reset stages catalog defaults;
ImportJson stages validated data. Reload rebases an external permanent QAT change.
Applied fires after the accepted checkpoint is updated. A context-menu/editor can
use `GetQuickAccessEntryId` to identify a presentation when one source occurs twice.

JSON is versioned and application-scoped. Version two contains QAT preferences and
optional ordinary Ribbon structure, label/size preferences and source/presentation
IDs. Live objects, command values, checked state, theme, density and window state
are excluded. Version-one/QAT-only input remains supported and preserves live
Ribbon structure. Malformed/foreign/future/unknown data is rejected before live
changes. Transient QAT references remain live without entering saved state.

Showcase automatically saves committed customization and separate application
preferences under `%LOCALAPPDATA%/RibbonKit/WinForms.Showcase/`, restoring them
before its first display. See [restart persistence](docs/M5E-SHOWCASE-PERSISTENCE.md)
for stored state, rejected-file recovery and isolated real-process checks.

Showcase uses the built-in customization pages rather than a separate QAT draft
demo group. Right-click a
command, whole group or permanent QAT entry (also in flyouts/overflow), or use
Shift+F10 / the menu key with ribbon focus, for the shared customization menu.
Set `QuickAccessCustomizationEnabled = false` to opt out. A host
`ContextMenuStrip` takes precedence; native text editors retain their editing menu.

The native `RibbonOptionsDialog` follows the WPF options layout: a navigation
rail, page heading and a footer ordered **OK, Cancel, Apply**. `RibbonOptionsPage` accepts
any application controls; use `Applying` to validate/cancel, `Applied` to commit
application settings and update their checkpoint, and `Cancelled` to discard
unapplied settings. Apply affects only the selected page. OK validates every page
before applying them in order; each page owns its transaction, without a global
rollback across application settings. Cancel/window close preserves prior Apply.
The dialog owns attached pages and child controls. Removing a page detaches it
without disposal. Native system title-bar chrome is retained in this slice.

```csharp
ribbon.CustomizationApplicationId = "my.application";
ribbon.OptionsDialogOpening += (_, e) =>
{
    var page = new RibbonOptionsPage { Text = "Editor" };
    var wrap = new CheckBox { Text = "Wrap text", AutoSize = true,
        Checked = LoadEditorPreference() };
    page.Controls.Add(wrap);
    // Load application settings here; keep an application-owned checkpoint.
    page.Applied += (_, _) => SaveEditorPreference(wrap.Checked);
    page.Cancelled += (_, _) => wrap.Checked = LoadEditorPreference();
    e.Dialog.Pages.Insert(0, page);
};
ribbon.ShowQuickAccessCustomization(ownerForm);
```

`RibbonQuickAccessCustomizationForm` is the ready-made options dialog containing
both pages, initially selecting QAT. `Editor.Session` exposes its QAT draft and
`RibbonEditor.Session` exposes its Ribbon draft. Both editors can be embedded
independently. `RefreshDraft()` updates controls after programmatic session edits,
and `ApplyChanges()` reports draft errors on the page.
`ribbon.CustomizationApplied` is the persistence cue for successful engine,
context-menu and page Apply; storage stays application-owned.

Use `ribbon.ShowRibbonCustomization(ownerForm)` to open Customize Ribbon. The page
follows the WPF arrangement: commands left, hierarchy right, transfer controls
between, order arrows beside the tree, and structural controls below. Its two
searches filter views only. Built-in commands retain their original collections;
new command presentations belong to custom groups. Ordinary tabs/groups support
rename and sibling reorder. Tabs support user visibility preferences, with at
least one retained; contextual/modal/merged structure remains application-owned.
Whole groups appear only in the QAT available-command list.

```csharp
using var edit = ribbon.CommandCatalog.BeginCustomization(
    "com.example.editor", RibbonCustomizationTarget.Ribbon);
var group = new RibbonCustomizationGroup { Label = "My commands" };
group.Commands.Add(new RibbonCustomizationCommand {
    CommandId = "cmd.copy", Size = RibbonItemSize.Medium });
edit.Draft.Ribbon!.Tabs[0].Groups.Add(group);
if (edit.Apply()) SaveProfile(edit.ExportJson()); // Host-owned storage.
```

Scoped sessions apply/reset only their selected scope; exporting combines that
draft with the other scope's current live preferences. The original overload edits
complete profiles. Full Reset stages captured defaults for both scopes. Call
`CaptureDefaults()` after application declarations and before loading preferences.
For structural profiles, assign explicit stable IDs to authored tabs/groups and
selected command sources. Changed definitions require a matching profile or host
migration. Reload rebases external changes; aliases preserve bound source Text.
`GetSource(element)` resolves a customization/QAT presentation to its original
command. New text/ComboBox presentations own separate native controls and use the
original source's values, bindings, validation and commit events.

`RibbonCustomizationEditor.ImportRequested` and `ExportRequested` request host
file/storage UI; use its Session import/export methods, then RefreshDraft after
import. Showcase demonstrates native JSON file dialogs. The transparent vector
[Import](src/RibbonKit.WinForms/Assets/Icons/Import.svg) and
[Export](src/RibbonKit.WinForms/Assets/Icons/Export.svg) assets are included under
`assets/icons/` in the package and rendered with the dialog's current palette/DPI.
Tree checkboxes and chevrons have explicit mouse/Touch sizes and pointer regions;
selection, scrolling and arrow-key navigation remain native. Options buttons and
navigation retain their original inherited font sizes.

Both QAT lists have independent search fields. Filtering changes only the view;
Remove and reorder use presentation identity in the complete draft, preserving
hidden commands and duplicate presentations. The search fields use the same
vertical gap above/below as the list-to-Reset gap. Office 2024 uses rounded field
and list frames plus a rounded accent OK button; the neutral renderer stays square.
Native text editing, list selection/scrolling, keyboard and button semantics remain.
Options chrome uses `CornerRadius`, capped by the renderer's rounded/square popup
policy, with the existing surface, accent and popup-border palette roles.

The QAT page reserves the row beneath its lists for Reset/Reload on the left.
Placement and visibility belong to the context menu. Its Reset restores default
commands while keeping those menu-only preferences; the underlying session Reset
still stages the complete profile. Mouse mode opens a smaller dialog than Touch.
The customization menu uses the shared ribbon popup frame, row metrics, renderer
and dismissal handling used by QAT overflow and command menus.

Rows use one compact command path with borrowed icons. Touch expands targets,
including a taller search frame that vertically centers its native text editor;
font changes and parent DPI changes recalculate sizes from logical values rather
than scaling old dimensions repeatedly. The dialog bounds its window/minimum to
the current working area and scrolls page content when it cannot fit. A host
embedding the editor independently should provide a scrolling container and size
it for its content. Physical mixed-monitor acceptance remains separate.
The page/editor have a 650-logical-pixel minimum content width, with a larger
font-based floor when needed. Narrower dialog viewports clip/scroll the page
instead of squeezing its columns. A larger application-set `MinimumSize.Width`
is retained through font changes; WinForms scales the native minimum for DPI.

## Renderer customization

The immutable theme exposes `HeaderPresentation` (Underline/Filled), optional
`HeaderHover`, `HeaderPressed`, `HeaderForeground`, `ColoredHeaderHover` and
`ColoredHeaderPressed`, `ExtendTitleBarColorToHeader` and `BodyBottomBorderOnly`.
An explicit renderer retains its policy. Header presentation survives custom
theme clones and does not depend on the built-in selection enum.

Additional nullable corner settings share validation and DPI scaling:
`FocusCornerRadius`, `KeyTipCornerRadius`, `CheckBoxCornerRadius`,
`BackstageBackButtonCornerRadius`, `NavigationMarkerCornerRadius`,
`ApplicationMenuFooterCornerRadius`, `ApplicationMenuFileButtonCornerRadius` and
`HeaderIndicatorCornerRadius`. Values accept 0..64 logical pixels; null preserves
the previous owner-specific default. Office 2019 sets them to zero. Popup radius,
border and shadow settings remain independent of the body.

`RibbonRenderer` is the flat foundation. It shares measured content, image
scaling/clipping, dropdown arrows and interaction states without the Office
underline, group separators, rounded chrome or Office focus/badge treatment.
`RibbonOffice2024Renderer : RibbonRenderer` adds that presentation and remains
the default when `RibbonControl.Renderer` is null. Both consume the selected
`RibbonTheme`; choosing a renderer does not change layout or command ownership.

Ordinary command menus, nested menus, QAT overflow and Simplified More Commands
share five optional geometry roles on `RibbonTheme`: `MenuMinimumRowHeight`,
`MenuIconSize`, `MenuLeadingPadding`, `MenuTrailingPadding`, and `MenuIconTextGap`.
Values are 96-DPI logical integers; null retains each density's existing default.
For example:

```csharp
ribbon.Theme = ribbon.Theme with
{
    MenuMinimumRowHeight = 64, MenuIconSize = 36,
    MenuLeadingPadding = 14, MenuTrailingPadding = 18, MenuIconTextGap = 16
};
```

Row/icon values accept 1..1024; padding/gap accept 0..1024. Font/icon minima still
apply, Touch retains a minimum 44-pixel row, and mixed rows use the tallest sibling.
Set the five values to null to reset without changing other theme values. Theme
changes dismiss stale popups; reopening measures the new geometry and retains
native editors. This is code-only configuration through the existing hidden
`Theme` property. Application Menu-specific geometry, illustrated-choice artwork,
ComboBox choice lists and QAT strip buttons retain their component metrics. QAT
group-reference rows retain their separate trailing chooser clearance. See the
[geometry contract and verification](docs/GEOMETRY-CONSOLIDATION-PLAN.md).

Four optional vertical roles share the same menu resolver:
`MenuIconVerticalPadding` (Compact 4 / Touch 6),
`MenuDescriptionVerticalPadding` (5 / 10), `MenuDescriptionGap` (2 / 4), and
`MenuSeparatorHeight` (9 / 9). Padding/gap accept 0..1024; separator height accepts
1..1024. All values are 96-DPI logical pixels; null restores the density fallback.
For example:

```csharp
ribbon.Theme = ribbon.Theme with
{
    MenuIconVerticalPadding = 12, MenuDescriptionVerticalPadding = 14,
    MenuDescriptionGap = 10, MenuSeparatorHeight = 20
};
```

Icon clearance contributes to the row minimum. Description padding/gap control
the measured title/description stack; plain text and native field internals keep
their own padding. Explicit vertical padding reserves both scaled sides at
fractional DPI; null preserves existing rounding and appearance. Separator rows
stay decorative, with a centered one-device-pixel line. These roles also apply to
QAT/More menu rows. Illustrated-choice artwork/descriptions and Application
Menu-specific geometry keep their component metrics; ordinary separator rows
inside an illustrated menu use `MenuSeparatorHeight`. Reset these four values to
null independently of the five horizontal/row roles.

Menu containers, headings and overflow bands have six further optional roles:

| Property | Null Compact / Touch | Meaning |
| --- | --- | --- |
| `MenuMinimumWidth` | 170 / 220 | Desired minimum dropdown/QAT/More width. |
| `MenuDescriptionMinimumWidth` | 280 / 320 | Desired minimum when ordinary command descriptions are present. |
| `MenuHeaderHorizontalPadding` | 6 / 6 | Text inset on each side of a dropdown heading. |
| `MenuHeaderVerticalPadding` | 6 / 8 | Padding above/below heading text. |
| `MenuHeaderBottomGap` | 4 / 4 | Space below a present heading. |
| `MenuOverflowScrollButtonHeight` | 16 / 28 | QAT/More top/bottom scroll-button bands. |

Widths and band height accept 1..1024; padding/gap accept 0..1024, in 96-DPI
logical pixels. Null resets each role independently. Content/font minima and
available screen space still govern actual sizes. Touch overflow bands retain
at least 28 logical pixels; tiny work areas clamp both bands to leave a viewport.
Plain dropdowns retain Windows scrollbar metrics. The shared dropdown container
and heading can surround illustrated choices without changing illustrated row
metrics. Application Menu-specific, gallery, native choice-list and custom-content
geometry stays independent. Example:

```csharp
ribbon.Theme = ribbon.Theme with
{
    MenuMinimumWidth = 380, MenuDescriptionMinimumWidth = 520,
    MenuHeaderHorizontalPadding = 24, MenuHeaderVerticalPadding = 16,
    MenuHeaderBottomGap = 12, MenuOverflowScrollButtonHeight = 44
};
```

Heading paint/measurement and overflow paint/hit/reveal/clip geometry share the
resolved values, including inert designer previews. Theme remains code-authored;
the existing opt-in sample geometry tab demonstrates apply/reset for this batch.

Four further nullable menu-row roles control reserved arrow areas and separator
clearance without changing glyph shapes:

| Property | Null Compact / Touch |
| --- | --- |
| `MenuSubmenuArrowWidth` | 16 / 28 |
| `MenuSplitArrowWidth` | 20 / 28 |
| `MenuChooserArrowWidth` | 16 / 16 |
| `MenuSeparatorHorizontalPadding` | 8 / 8 |

Arrow widths accept 1..1024; separator padding accepts 0..1024, in 96-DPI logical
pixels. Null restores the current density default. All ordinary submenu and
QAT/More chooser glyphs use one trailing visual column defined by the resolved
split secondary region. Split width also controls its hit/keyboard/KeyTip bounds;
Touch retains a preferred minimum 28 and narrow rows keep their existing one-third
cap. Submenu and chooser widths are minimum caption reservations; each also
reserves at least the shared column. Larger authored values add text clearance
without moving the glyph. Trailing padding and projected ComboBox/group caption
bounds use the same menu text rules. Description measurement shares those actual
reservations. The existing UIA command bounds continue following the primary target.
Separator padding changes only the line ends; thickness stays one device pixel.

These theme roles exclude strip/group-flyout arrows and Application Menu-specific
layout/separators. Choice-list geometry, field fill, glyph shapes, popup margins
and frame/shadow geometry are unchanged. Values remain code-authored; inert preview
arrow-opening hit areas use the same shared column. Example:

```csharp
ribbon.Theme = ribbon.Theme with
{
    MenuSubmenuArrowWidth = 52, MenuSplitArrowWidth = 64,
    MenuChooserArrowWidth = 44, MenuSeparatorHorizontalPadding = 24
};
```

Body/group, header and QAT/message spacing share optional per-ribbon theme roles:

| Property | Null behavior |
| --- | --- |
| `GroupHorizontalPadding` | Expanded groups 6; Classic collapsed outer clearance 3. Simplified group openers use 6. |
| `GroupColumnGap` | Classic Compact 3 / Touch 6; Simplified inter-command gap 3. |
| `BodyContentHorizontalPadding` | Inner body viewport inset: Classic 2 / Simplified 4. |
| `GroupSeparatorVerticalInset` | Office Classic divider clearance 12; Simplified 6. |
| `TabHorizontalPadding` | Tab/File horizontal label-width allowance of 14 per side. |
| `TabMinimumWidth` | 58, before font/padding requirements. File retains its separate minimum of 50. |
| `HeaderIndicatorPadding` | Office tab/File indicator label extension and edge clearance 4. |
| `QuickAccessHorizontalPadding` | QAT edge padding 2. |
| `QuickAccessItemGap` | Between QAT entries and before overflow: 0. |
| `MessageActionGap` | Horizontal text-to-inline-action gap 10. |
| `MessageTitleTextGap` | Inline title-to-body gap 8. |

Values are logical pixels at 96 DPI. Spacing accepts 0..1024; `TabMinimumWidth`
accepts 1..1024 and retains Touch's 44-pixel floor. Each null value restores the
existing geometry independently. Larger collapsed-group outer padding reserves
width beyond the legacy caption-width cap; command/editor dimensions stay intact.
Body/indicator/divider insets clamp within available bounds. Neutral Classic
keeps its divider/indicator policy. Tab overflow, group reduction/scrolling,
borrowed groups, hit targets, keyboard/KeyTips/UIA and inert designer previews
consume the same layout as paint.

QAT sizing/entry selection includes gaps before overflow in TabRow, BelowRibbon
and supported TitleBar slots; original sources and intrinsic button/row sizes
remain. Message inline-versus-wrapped measurement and action/close targets share
the resulting rectangles. Existing `BodyMargin`, `BodyHeight`, `TabHeight`,
`HeaderButtonInset`, `MessageMinimumHeight` and `MessageContentPadding` remain the
corresponding APIs. Native caption buttons, font/Touch minima, message stacked
action vertical gap, intrinsic artwork and connected frame/radius/shadow rules
retain their existing owners. No theme-authoring UI or new renderer API is added.

```csharp
ribbon.Theme = ribbon.Theme with
{
    GroupHorizontalPadding = 12, GroupColumnGap = 10,
    BodyContentHorizontalPadding = 12, GroupSeparatorVerticalInset = 20,
    TabHorizontalPadding = 22, TabMinimumWidth = 90, HeaderIndicatorPadding = 8,
    QuickAccessHorizontalPadding = 8, QuickAccessItemGap = 6,
    MessageActionGap = 18, MessageTitleTextGap = 12
};
```

Theme stays immutable, hidden/nonserialized and code-only. Invalid values are
rejected before assignment, preserving the current theme and built-in selection.

Horizontal command split buttons share three optional theme roles:

| Property | Null behavior |
| --- | --- |
| `CommandSplitArrowWidth` | Existing desired Compact 20 / Touch 44. |
| `CommandSplitPrimaryMinimumWidth` | Classic Small/Medium 0 / 56; Simplified retains its current content-based sizing. |
| `CommandLargeSplitPrimaryMinimumWidth` | Large horizontal Compact 0 / Touch 72, including existing caption sizing. |

These are 96-DPI logical pixels. Arrow width accepts 1..1024; primary minima
accept 0..1024. Null resets each role independently and preserves default layout
and paint. Explicit relevant geometry reserves the primary caption budget and
full arrow slot, including enough width for existing narrow-target caps. Touch
retains arrow 44 and primary 56/72 floors. Content/cap requirements may exceed an
authored minimum. For example:

```csharp
ribbon.Theme = ribbon.Theme with
{
    CommandSplitArrowWidth = 48,
    CommandSplitPrimaryMinimumWidth = 104,
    CommandLargeSplitPrimaryMinimumWidth = 144
};
```

Classic, Simplified and group flyouts consume the same preferred-width/target
resolver. Paint, hit testing, keyboard/KeyTips and UIA use the resulting regions;
primary state/invocation and separate dropdown actions remain. Narrow external
rectangles keep the existing Compact third / Touch half cap; zero-width horizontal
targets stay nonnegative. Large-only and Small/Medium-only primary settings remain
independent. Actual Large vertical splits, menu/QAT/Application Menu-specific
regions, ordinary dropdowns, editor dimensions and content vertical alignment
retain their own contracts. Theme stays code-only and nonserialized; existing
designer authoring and round trips are unchanged.

Small/Medium command content has two optional theme roles:

| Property | Null behavior |
| --- | --- |
| `CommandContentHorizontalPadding` | Existing 5-pixel text padding and optical icon offsets. |
| `CommandContentVerticalPadding` | Existing rows and intrinsic 3-pixel text inset. |

Both accept 0..1024 logical pixels at 96 DPI. Larger horizontal padding reserves
symmetric caption space and width, including split primary content. Smaller
values retain density/preferred-width minima. Vertical padding is a minimum
clearance above/below text and 16-pixel artwork; containing Classic rows and
Simplified bands grow when needed, retaining centered content and font/density
minima. Small Touch icons keep their accepted centering. Native editor fields
keep their dimensions/handles even when a shared command row grows. With vertical
padding above 3, fitting Large buttons/dropdowns/splits use shared icon, two-line
caption and below-caption arrow bands, centered as one reservation. Unused bands
remain reserved: icons and caption starts align across one/two-line labels, and
vertical split arrows share a row. Vertical split boundaries follow those bands
for hit testing, keyboard and UIA. Text-only commands retain the caption baseline.
Null, horizontal-only padding and vertical padding at most 3 preserve legacy
placement; constrained bounds fall back to it. Gallery content, menu/QAT projections
and intrinsic glyph sizes keep their owners. Null resets independently.

Application Menu spacing has five separate optional theme roles:

| Property | Null default, Compact / Touch |
| --- | --- |
| `ApplicationMenuNavigationMinimumRowHeight` | 40 / 44 |
| `ApplicationMenuPaneMinimumRowHeight` | 28 / 44 |
| `ApplicationMenuFooterMinimumRowHeight` | 30 / 44 |
| `ApplicationMenuPaneHorizontalPadding` | 10 / 10 |
| `ApplicationMenuFooterItemGap` | 6 / 6 |

Row minima accept 1..1024; padding/gap accept 0..1024, in 96-DPI logical pixels.
Font, artwork, native editor and Touch 44 minima remain. The pane inset applies
to content and the default heading; selected-pane title bands retain full width.
Insets above the legacy 10 also extend the navigation column by the excess,
reserving desired width so rows reach the shared divider and active-title boundary.
The right content retains its configured inset; available space still clamps
geometry. Footer gap applies between items and wrapped rows. Work-area limits,
independent column/footer scrolling, hover selection, validation and retained
native content stay with existing owners. Nested ordinary menus use their own
metrics. Each null setting preserves legacy geometry, including editor minima.

```csharp
ribbon.Theme = ribbon.Theme with
{
    CommandContentHorizontalPadding = 12, CommandContentVerticalPadding = 16,
    ApplicationMenuNavigationMinimumRowHeight = 64,
    ApplicationMenuPaneMinimumRowHeight = 72,
    ApplicationMenuFooterMinimumRowHeight = 52,
    ApplicationMenuPaneHorizontalPadding = 22,
    ApplicationMenuFooterItemGap = 12
};
```

These immutable per-ribbon settings use the existing code-only, hidden and
nonserialized `Theme` property. Measurement, paint and inert designer preview
share library geometry. Authored models, bindings, handlers and designer-generated
code retain their existing contracts; no theme-authoring UI is introduced.

Illustrated-choice dropdown rows have their own optional theme roles:

| Property | Null default, Compact and Touch |
| --- | --- |
| `IllustratedMenuMinimumWidth` | 300 |
| `IllustratedMenuMinimumRowHeight` | 64 |
| `IllustratedMenuIconSize` | 36 |
| `IllustratedMenuLeadingPadding` | 8 |
| `IllustratedMenuTrailingPadding` | 8 |
| `IllustratedMenuIconTextGap` | 12 |
| `IllustratedMenuDescriptionGap` | 2 |

These are 96-DPI logical pixels. Dimensions accept 1..1024; padding/gap accept
0..1024. Null resets each role independently and preserves previous measurements
and paint. Font/artwork minima still apply, Touch keeps at least 44, and mixed
menu rows use the tallest sibling. Existing title/font sizing, bold title,
two-line description and state/invocation policy remain. For example:

```csharp
ribbon.Theme = ribbon.Theme with
{
    IllustratedMenuMinimumWidth = 540, IllustratedMenuMinimumRowHeight = 88,
    IllustratedMenuIconSize = 48, IllustratedMenuLeadingPadding = 18,
    IllustratedMenuTrailingPadding = 22, IllustratedMenuIconTextGap = 20,
    IllustratedMenuDescriptionGap = 8
};
```

Desired widths reserve larger artwork/insets; work-area limits and scrolling
remain authoritative. Explicit geometry clips slots safely inside constrained
rows. Popup sizing, title/artwork/description paint and inert preview share one
resolver. Theme assignment closes stale popups; reopening retains the source
models and native mixed editors. Code-only configuration leaves designer model
authoring/serialization unchanged. The roles apply to ordinary illustrated menus
opened directly, nested or from a QAT source reference. Standard menus, QAT/More
projection rows, Application Menu-specific rows, native editor geometry and split
actions keep their separate contracts.

Native editor content and ComboBox choice rows have seven optional theme roles:

| Property | Null Compact / Touch |
| --- | --- |
| `EditorLabelGap` | 6 / 6 |
| `EditorTextInset` | 4 / 4 |
| `EditorMinimumHeight` | Font minimum / 42 |
| `EditorVerticalPadding` | 4 / 4 |
| `ComboBoxButtonWidth` | 24 / 32 |
| `ComboBoxMinimumChoiceHeight` | 28 / 44 |
| `ComboBoxChoiceVerticalPadding` | 5 / 5 |

Values are 96-DPI logical pixels. Gaps/padding/insets accept 0..1024; button and
field/choice minima accept 1..1024. Null resets each role independently. Touch keeps a
32-pixel button minimum and 44-pixel choice-row minimum. Font/padding minima apply;
native choice height is capped at 255 device pixels. Explicit vertical padding
scales each side separately; null preserves the existing summed rounding.
For example:

```csharp
ribbon.Theme = ribbon.Theme with
{
    EditorLabelGap = 18, EditorTextInset = 10, EditorMinimumHeight = 54,
    EditorVerticalPadding = 10, ComboBoxButtonWidth = 48,
    ComboBoxMinimumChoiceHeight = 60, ComboBoxChoiceVerticalPadding = 14
};
```

Layout, paint, native text clipping and inert previews share these roles. Hidden
ComboBox labels reserve no gap; narrow text slots clamp to empty within the
field/button. Authored `InputWidth`, `FillMenuWidth` and the accepted proportional
Touch width rules remain. Optional height/padding values grow native TextBox and
ComboBox fields together, with centered text/labels and unchanged font size.
Classic command rows, group flyouts, menu/Application Menu rows and the Simplified
field band reserve their resolved height. Menu-only fields and Small command
projections do not enlarge the ribbon body. Touch fields retain a minimum of 42,
or 44 for inline Simplified ComboBoxes; Simplified Compact retains 26. Unset field
padding preserves scaled summed 8; explicit padding scales each side separately.
Height overrides do not rescale field widths. Changing theme closes stale lists/popups;
reopening uses current metrics without replacing the native editors. Small command
projections and menu-reference arrows retain their separate geometry. Choice-list
horizontal padding, outer field insets, system EDIT/caret/IME and application
content remain outside this contract. Theme configuration is code-only; existing
designer model authoring and generated code are unchanged.

Gallery and Backstage spacing is also code-only through nullable `RibbonTheme`
roles, with independent null reset:

| Surface | Properties and null defaults (96-DPI logical pixels) |
| --- | --- |
| Galleries | `GalleryContentPadding` 4; `GalleryUtilityWidth` Compact 18 / Touch 32; `GalleryTilePadding` 4; `GalleryHeadingHorizontalPadding` 6; `GalleryHeadingVerticalPadding` 6 |
| Backstage | `BackstageMinimumRowHeight` Compact 42 / Touch 52; `BackstageNavigationPadding` 8; `BackstageBackButtonSize` Compact 36 / Touch 44; `BackstageBackButtonInset` 20; `BackstageBackButtonBottomGap` 24; `BackstageIconTextGap` 12; `BackstagePagePadding` 0 |

Spacing accepts 0..1024, utility/row/button sizes 1..1024. Touch retains utility
32, row 52 and Back button 44 minima; font/content minima and available-space
clamps still apply. Assignment validates all roles before changing the theme.
Gallery container padding covers inline and dropdown/native presenter viewports;
utility width is inline-only, while custom dropdown footer buttons retain their
existing slots. Built-in tile padding retains artwork allowances and two-line
popup font minima; inline artwork fits the existing body-height allocation.
Model tile dimensions/columns, native card internals and custom preferred sizes
remain application-owned. Ordinary menu headings keep their separate roles.

Backstage Auto rail measurement and entry painting share the icon/text offset.
Existing `Backstage.NavigationWidth`, width bounds, navigation font and opacity
APIs remain. Page padding insets the native page host inside its existing frame;
application page layout, Controls/HWNDs, selection/validation and whole-surface
motion/caption coverage retain their owners. The opaque designer preview shares
selected geometry without hosting or capturing application content. There is no
new designer theme-authoring UI or generated-code change.

```csharp
ribbon.Theme = ribbon.Theme with
{
    GalleryContentPadding = 10, GalleryUtilityWidth = 40,
    GalleryTilePadding = 10, GalleryHeadingHorizontalPadding = 16,
    GalleryHeadingVerticalPadding = 12,
    BackstageMinimumRowHeight = 62, BackstageNavigationPadding = 14,
    BackstageBackButtonSize = 52, BackstageBackButtonInset = 28,
    BackstageBackButtonBottomGap = 32, BackstageIconTextGap = 22,
    BackstagePagePadding = 18
};
// Reset a single role without changing any other setting.
ribbon.Theme = ribbon.Theme with { BackstagePagePadding = null };
```

The existing sample's `--menu-geometry-demo` includes **Gallery geometry** and
**Backstage geometry**, with independent gallery/navigation/page apply/reset
controls. See the [grouped contract, evidence and pending combined review](docs/GEOMETRY-CONSOLIDATION-PLAN.md#grouped-thirteenthfourteenth-batches-galleries-and-backstage--2026-11-04).

ScreenTip/KeyTip and caption spacing also uses independently nullable,
code-only `RibbonTheme` roles:

| Role | Null default, 96-DPI logical pixels |
| --- | --- |
| `ScreenTipMaximumWidth`, `ScreenTipContentPadding`, `ScreenTipTitleGap`, `ScreenTipPlacementGap` | 320, 12, 6, 8 |
| `KeyTipHorizontalPadding`, `KeyTipVerticalPadding` | Existing root/menu TextRenderer sizing; Backstage 4 / 2 per side with legacy summed DPI rounding |
| `CaptionLeadingPadding`, `CaptionIconTextGap`, `CaptionQuickAccessTextGap`, `CaptionTitleReservationWidth` | Leading 10 with icon / 8 without; gaps 10 / 10; title reservation 100 before the QAT/title gap |

Widths accept 1..1024; spacing accepts 0..1024. Null resets independently. Passive
work-area bounds, inherited fonts, hover/cancellation/nonactivation and existing
badge keyboard targets remain. Explicit badge padding fits existing targets;
Backstage retains its outer row/trailing clearance. Caption settings govern the
optional supported RibbonForm's icon/content/QAT/title allocation. Native caption
height, system buttons/glyphs, frame/resize/drag behavior and material/Backstage
motion remain unchanged. The title reservation yields to the existing QAT minimum
and narrow-window overflow/fallback. There is no new designer theme editor or
model serialization change. The sample's **Tips geometry** and **Caption geometry**
tabs demonstrate independent presets/reset; the [active geometry plan](docs/GEOMETRY-CONSOLIDATION-PLAN.md)
owns actual evidence and the pending combined review.

Native fields in ordinary dropdown menus use the same command-text gutter and
trailing clearance as neighboring commands. TextBox and visible ComboBox labels
start at that text column; their fields share the measured label span. With
`ShowLabel=false`, a ComboBox starts its field directly at the command-text column
without reserving label space or measuring its hidden command name. Menu sizing
reserves the gutter, label and authored `InputWidth`, so adding the gutter may
widen a menu. Field width/height and Touch scaling retain their existing rules.
Runtime native presenters and inert designer previews use one content rectangle;
strip/Simplified fields, group flyouts and Application Menu-specific fields retain
their own layout. The alignment itself needs no extra setting.

Set `FillMenuWidth=true` on a `RibbonTextBoxItem` or `RibbonComboBoxItem` to fill
the remaining ordinary dropdown row. This is an optional Layout property in
Properties and Edit Ribbon; its default/reset value is false. A visible label
keeps the shared label column; a hidden-label ComboBox fills from the command-text
column. Fields end at the menu's trailing clearance. Font and height remain
unchanged. `InputWidth` remains the preferred minimum used to measure the menu,
subject to available space and existing scrolling/clipping. Turning fill off
restores fixed sizing without changing the authored width or value. Strip,
Simplified, group-flyout and Application Menu-specific fields retain their own
sizing even when the flag is true. Designer drafts/Boolean serialization and
customization copies preserve the setting.

```csharp
nameField.FillMenuWidth = true;
fontChooser.FillMenuWidth = true; // Also works with ShowLabel=false.
```

`RibbonTheme.DisabledForeground` optionally colors library-painted disabled text,
vector command icons, arrows and check/radio indicators, including field labels,
selection-only ComboBox text and choice rows. Its default `Color.Empty` preserves
Windows gray; reset to Empty to restore it. Native TextBox/editable ComboBox input,
caret and selection retain Windows drawing, and bitmap artwork keeps its existing
treatment. Focus, hover, pressed and checked colors reuse existing theme roles.
See the [state and native-layer map](docs/M4I2-APPEARANCE-INVENTORY.md#state-and-native-layer-map)
for the supported layers and limits.

The default ribbon background is white. The shared `BodyMargin` defaults to
7,1,7,7 logical pixels. Layout reserves these
margins for the panel, scrolling and hit targets. Office paints that panel as a
rounded card with a border and a soft `BodyShadowColor` shadow; the neutral
renderer retains its flat surface. The default `BodyHeight` minimum is 110 logical
pixels; font-dependent measurement reserves three command rows, a group caption,
six logical pixels above commands and eight below them. Compared with the earlier
default, the body extends six logical pixels downward, with its top edge and tabs
anchored. Command/icon/editor sizes stay the same; the added height supplies
padding and caption clearance. Explicit taller `BodyHeight` settings retain their
minimum-height meaning. Office tab headers use text and blue underlines
for selection/hover/press/keyboard focus, with transparent backgrounds. The
underline follows the measured label width plus four logical pixels per side,
is three logical pixels thick, and has rounded ends. Scroll
controls use themed hover/pressed chrome. The body panel bounds supplied to
`DrawBody` are separate from the inner content viewport.
Office group dividers follow the WPF reference's inset proportions: a crisp
one-device-pixel `Border` hairline with 12 logical pixels of clearance at both
ends, scaled with DPI. The neutral renderer retains its separator-free surface.
Expanded and collapsed groups use the full group frame for aligned dividers;
collapsed button hover/focus visuals and click targets keep their inset bounds.
Body scrollers use `PopupBorder` at rest and an accent border on hover/press.
Their top and bottom clearance is controlled by
`RibbonTheme.BodyScrollButtonVerticalInset`: two logical pixels by default,
matching the side clearance. Themes can use zero for full-height scrollers or
another nonnegative value; layout caps it at half the body height. For example,
`ribbon.Theme = ribbon.Theme with { BodyScrollButtonVerticalInset = 0 };`.
Collapsed group openers use the same inset rectangle for painting, focus, hit
testing and popup anchoring, with six logical pixels above and eight below.
Their measured width grows with the caption up to
`RibbonTheme.CollapsedGroupMaxWidth` (108 logical pixels by default, minimum 80).
The limit excludes the one-device-pixel divider. Long captions use up to two
centered lines and ellipsize the final line; the arrow stays centered below.
The same caption inset is used by measurement and painting at every DPI.

Floating ribbon menus, collapsed groups and ComboBox choice popups share a
one-device-pixel frame.
Office 2024 uses `PopupCornerRadius` (six logical pixels by default); the neutral
renderer keeps square corners. `GetPopupCornerRadius` supplies the shared
device-pixel radius for frame painting, content clipping and shadow. Override it
with the renderer's drawing hooks to customize the complete popup shape.
`DrawPopupFrame` paints all four sides using `PopupBorder`. `PopupShadowEnabled`
enables the library's soft shadow, subject to Windows' shadow setting.
`DrawPopupShadow` uses `PopupShadowColor` (the same default opacity as the body),
six logical pixels of blur and a two-pixel logical downward offset by default. A separate
nonactivating, click-through layered window composites the shadow and partially
covered frame-edge pixels through
[UpdateLayeredWindow](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-updatelayeredwindow);
the popup and its native editors stay opaque and use matching mirrored clipping
regions. The smooth frame edge remains when shadows are disabled. Cut-out corners
pass pointer input through and count as outside presses for popup dismissal.
The library owns and destroys the frame/shadow window with its popup. The darker
native ToolStrip and ComboLBox shadows are disabled or kept hidden. Choice popups
use the same rounded Office frame, square neutral frame and soft shadow hooks.

Frame geometry is also code-only through independently nullable `RibbonTheme`
roles. `BodyCornerRadius` and `BackstagePageCornerRadius` accept 0..64 logical
pixels; null keeps Office's existing doubled `CornerRadius` and neutral square
policy. Backstage rounds only the top-left page corner. `BodyShadowBlur` and
`PopupShadowBlur` accept 0..32 (null 6, zero suppresses the painted shadow).
`BodyShadowVerticalOffset` and `PopupShadowVerticalOffset` accept -32..32
(null 2, positive downward). Each null resets only that role; invalid assignment
leaves the previous Theme/BuiltInTheme intact. Existing `PopupCornerRadius`,
shadow colors/enable and authored `BodyMargin` remain separate.

```csharp
ribbon.Theme = ribbon.Theme with
{
    BodyCornerRadius = 14, BackstagePageCornerRadius = 18,
    BodyShadowBlur = 12, BodyShadowVerticalOffset = 4,
    PopupShadowBlur = 12, PopupShadowVerticalOffset = 4
};
// Reset the panel shadow independently of corners and popup settings.
ribbon.Theme = ribbon.Theme with { BodyShadowBlur = null, BodyShadowVerticalOffset = null };
```

`GetBodyCornerRadius`, `GetBodyShadowPadding` and `GetPopupShadowPadding` are
virtual renderer measurements in **device pixels**, without state mutation.
Custom shadow painters report their complete footprint; consumers clamp clearance
to 0..128 logical pixels per side. Explicit body-shadow roles reserve at least
that footprint and authored `BodyMargin` without rewriting Theme.BodyMargin.
Both null preserve exact legacy margins, so explicitly selecting default blur/
offset can reserve more space than null. The extra reservation changes available
panel width/height and can trigger existing reduction/overflow; command bands and
native field sizes retain their own metrics. Connected body/QAT/messages keep
square adjoining corners and one outer shadow, including standalone minimized
cards. Popup chrome, gallery motion, temporary-ribbon dismissal and passive tip
work-area inset share the renderer's popup clearance. Both null preserve legacy
scaled-eight rounding; explicit roles use scaled blur plus absolute offset, at
least one device pixel. Ordinary popup placement still clamps the opaque frame;
external shadows can be trimmed at a display edge. Null frame-border overrides
retain one-device-pixel borders/insets. Windows shadow policy, native frames, Backstage motion and
opaque designer preview retain their existing behavior. See the
[frame contract, evidence and review gate](docs/GEOMETRY-CONSOLIDATION-PLAN.md#seventeenth-session-frames-and-shadows-2026-11-05).

`Theme.BodyAppearance` accepts an immutable `RibbonBodyAppearanceSettings` group
for body and connected-frame decoration. Each null member preserves its existing
default; assigning the group to null resets it independently:

| Member | Default and behavior |
| --- | --- |
| `BorderWidth`, `MessageBorderWidth` | One device pixel. Authored widths accept 0..32 logical pixels; zero hides the body or message outer border. Existing body bottom-edge and message side-border choices remain independent. Message contents clip inside the authored outer border; row rules and command geometry keep their existing owners. |
| `GroupSeparatorWidth`, `GroupSeparatorColor` | One device pixel and Border ink. Width accepts 0..32 logical pixels; zero hides the divider. Empty ink follows Border; alpha/Transparent are supported. Classic group frames and Simplified dividers share the public `DrawGroupSeparator` hook, with unchanged reduction, gaps and hit targets. |
| `QuickAccessSeparatorWidth`, `QuickAccessSeparatorInset`, `QuickAccessSeparatorColor` | Joined below-ribbon separator uses one device pixel of thickness and body-border ink. Null inset retains zero for bottom-border-only bodies or one device pixel otherwise. Authored width/inset accept 0..32 / 0..1024 logical pixels; row bounds clamp painting. Zero width or Transparent ink hides it. |
| `JoinBelowRibbonQuickAccess`, `JoinMessageBarToBody` | Null/true uses the active renderer's existing join capability; false requests separation through shared layout. A custom renderer that disables joining remains authoritative. The existing rule that messages cannot join through a separate below-ribbon QAT row is retained. |

```csharp
ribbon.Theme = ribbon.Theme with
{
    BodyAppearance = new RibbonBodyAppearanceSettings
    {
        BorderWidth = 3, MessageBorderWidth = 3,
        GroupSeparatorWidth = 3, GroupSeparatorColor = Color.Teal,
        QuickAccessSeparatorWidth = 2, QuickAccessSeparatorInset = 8,
        JoinBelowRibbonQuickAccess = false
    }
};
```

Borders/dividers are decoration over the consolidated snapshots. Join overrides
use existing layout and refresh paths; commands, native editors, borrowed popup
content and model ownership are retained. Radius, shadow, margins, inner padding,
palette, header style and popup frames keep their existing independent settings.
Both light themes compose the same body-frame structure; changing a header or
Backstage presentation does not choose a body structure. Neutral/Office renderer
defaults and custom virtual drawing/measurement hooks remain distinct.
The optional demo appends **Body and connected frames** to **Theme effects 2**,
including a preview of the below-ribbon QAT/message stack and a group reset.

`Theme.CommandAppearance` accepts an immutable
`RibbonCommandAppearanceSettings` group for ordinary command-state and split
chrome, including ordinary menu rows and Quick Access command projections:

| Member | Default and behavior |
| --- | --- |
| `CornerRadius` | Active renderer's square neutral or existing Office corners. Authored values accept 0..64 logical pixels. |
| `BorderColor`, `BorderWidth` | Empty ink retains each recipe's border: ordinary states/QAT may be borderless, ordinary splits retain their outline. Width accepts 0..32 logical pixels; null retains one device pixel when ink exists. Zero hides the outline; width alone does not invent border ink. |
| `Inset` | One device pixel inside the unchanged target. Authored 0..1024 logical pixels clamp to available bounds; zero fills the target. |
| `SplitSeparatorWidth`, `SplitSeparatorInset`, `SplitSeparatorColor` | One device pixel of thickness, end clearance following the command inset, and Border ink. Width/inset accept 0..32 / 0..1024 logical pixels. Zero width or Transparent ink hides the separator; Empty ink follows Border. Existing split orientation/action regions are unchanged. |
| `SplitCompanionBackground` | Null retains the resting-half recipe while the other half is active, including transparent material QAT companions. Authored alpha/gradients precede material fallback. Hover, press, checked and open states retain their existing fill settings and precedence. |

```csharp
ribbon.Theme = ribbon.Theme with
{
    CommandAppearance = new RibbonCommandAppearanceSettings
    {
        CornerRadius = 8, BorderColor = Color.DarkBlue, BorderWidth = 2,
        Inset = 0, SplitSeparatorWidth = 3,
        SplitSeparatorColor = Color.DarkOrange,
        SplitCompanionBackground = new RibbonFill(Color.LightSkyBlue)
    }
};
```

Null members/Empty ink reset independently; `CommandAppearance = null` resets
the group. Ranges validate before theme/BuiltInTheme mutation. Existing
`Hover`/`Pressed`/`Checked`, QAT backdrop roles and opacity remain consumed.
Authoring state chrome does not make an idle command permanently active.
The real application renderer still receives public item/split/QAT/text/focus
callbacks and its protected chrome hook; custom replacement drawing keeps
precedence. Both halves clip the same perimeter so inner seams stay square.
Command content, native editor frames, indicator glyphs, headers, body/group
frames and Application Menu recipes retain their separate owners/settings.
The optional demo adds **Try commands** and **Command state effects** on **Theme
effects 3**, preserving the full Office 2019 and Theme effects 2 tabs.

`Theme.CommandContent` accepts immutable `RibbonCommandContentSettings` for
shared command indicators and raster-image paint:

| Member | Default and behavior |
| --- | --- |
| `IndicatorBorderWidth`, `IndicatorBorderColor` | One device pixel and existing state ink. Width 0..32 logical pixels clamps inside the slot; zero/Transparent hides the outline, Empty resets ink. |
| `CheckMarkColor`, `CheckMarkStrokeWidth`, `CheckMarkScale` | Existing tick/mixed paint: Surface ink and 1.5-logical-pixel checkbox stroke with a device-pixel minimum; Foreground/DisabledColor and a 6 × 2 logical-pixel QAT mixed bar. Authored stroke 0..32 and scale 0..4; scale changes QAT bar width, stroke changes its height. Zero hides paint while retaining state. |
| `RadioDotInset`, `RadioDotColor` | Four logical pixels; Accent/DisabledColor ink. Inset 0..1024; zero fills the circle and consuming the slot hides it. |
| `DropDownGlyphWidth`, `DropDownGlyphHeight` | Filled triangle 8 × 6 logical pixels. Authored dimensions 0..64 clamp to its existing slot; zero hides it. `Chevrons` separately controls stroked arrows. |
| `DisabledImageOpacity`, `DisabledImageGrayscaleAmount` | Null retains ordinary grayscale opacity 0.55 and the framework treatment for independently disabled QAT references. Authoring either finite 0..1 role selects the shared matrix for both paths, with independent null fallbacks 0.55 opacity / 1 grayscale. Grayscale zero retains source color; intermediate values partly desaturate. Enabled images and borrowed vector-icon treatment remain unchanged. |

Colors retain authored alpha. Numeric members reset independently with null,
colors with Empty; `CommandContent = null` resets this group. Values validate
before theme/BuiltInTheme mutation. Existing state fills, checkbox radius,
content padding, text layout, native editor frames and model/keyboard state
remain with their current owners. Actual public indicator/arrow and protected
image hooks keep precedence. Shared image/arrow callers inherit these settings;
library options-button images retain their dialog owner's treatment.

```csharp
ribbon.Theme = ribbon.Theme with
{
    CommandContent = new RibbonCommandContentSettings
    {
        IndicatorBorderWidth = 2, IndicatorBorderColor = Color.DarkBlue,
        CheckMarkScale = .8, RadioDotInset = 3,
        DropDownGlyphWidth = 12, DisabledImageOpacity = .35
    }
};
```

The optional demo adds **Try indicators / images** and **Command content effects**
to **Theme effects 3**. Theme changes keep the existing menu-close lifecycle;
the owned command/menu models and native editor state are retained.

`Theme.EditorAppearance` accepts immutable `RibbonEditorAppearanceSettings`
for native TextBox/ComboBox frame and choice-row decoration:

| Member | Default and behavior |
| --- | --- |
| `CornerRadius`, `BorderWidth`, `Inset` | Square neutral or existing Office corners, one-device-pixel border/inset. Authored logical ranges are 0..64 / 0..32 / 0..1024; widths/insets clamp inside the field. Zero width hides the outline. Native input bounds and opaque text/selection surfaces remain unchanged; frame inset is separate from `EditorTextInset`. |
| `ActiveBorderColor` | Empty follows Accent; authored alpha/Transparent is retained. Existing TextBox focus and ComboBox hot/focus/press/open rules remain; `EditorBorder` supplies resting ink. |
| `ChoiceCornerRadius`, `ChoiceInset` | Independent active-row shape, 0..64 / 0..1024 logical pixels. Null retains actual renderer corners / one device pixel inset. Targets and row heights are unchanged. |
| `ChoiceTextHorizontalPadding` | Six logical pixels on each side. Authored 0..1024 clamps to available width; native input text keeps its existing inset. |

```csharp
ribbon.Theme = ribbon.Theme with
{
    EditorAppearance = new RibbonEditorAppearanceSettings
    {
        CornerRadius = 8, BorderWidth = 2, ActiveBorderColor = Color.DarkOrange,
        ChoiceCornerRadius = 6, ChoiceInset = 2, ChoiceTextHorizontalPadding = 12
    }
};
```

Null members/Empty ink reset independently; `EditorAppearance = null` resets
only this group. Ranges validate before Theme/BuiltInTheme mutation. Existing
editor colors, state fills, glyph settings and field/choice sizing remain in use.
The actual field/choice/chrome/text/arrow hooks retain precedence. Native controls,
validation, caret, selection, IME and popup ownership keep their existing paths.
The optional demo adds **Try text fields**, **Try choices** and **Editor appearance
effects** to the new **Theme effects 4** tab; earlier review tabs remain in place.

`Theme.PopupAppearance` accepts immutable `RibbonPopupAppearanceSettings` for
the shared floating-popup frame and shadow:

| Member | Default and behavior |
| --- | --- |
| `BorderWidth` | One device pixel. Authored 0..32 logical pixels; zero hides paint while keeping legacy content clearance. Wider borders reserve space around hosted content. |
| `ShadowHorizontalOffset` | Zero. Authored -32..32 logical pixels; shared clearance includes both horizontal and existing vertical offsets. |
| `ShadowFalloff` | Gaussian divisor 2.5. Authored finite 0.1..16; larger values concentrate the shadow near the frame, smaller values soften it within the existing blur footprint. |

```csharp
ribbon.Theme = ribbon.Theme with
{
    PopupAppearance = new RibbonPopupAppearanceSettings
    {
        BorderWidth = 3, ShadowHorizontalOffset = 6, ShadowFalloff = 1.4
    }
};
```

Members reset independently with null; `PopupAppearance = null` resets this
group. Values validate before Theme/BuiltInTheme mutation. Existing popup colors,
radius, shadow enabled/color/blur/vertical offset and system policy remain in use.
The actual renderer's additive `GetPopupBorderWidth` hook supplies device pixels
to paint and native hosted-content clearance, bounded to 128 logical pixels and
available space. Custom frame/shadow/radius/clearance overrides retain precedence.
Menus, nested menus, choice lists, galleries and passive tips share the frame;
content controls, editors, selection, activation and popup ownership stay with
their existing owners. Body shadows retain their own falloff/defaults.
The optional demo adds **Try popup frames** and **Popup frame effects** on
**Theme effects 4**. Use the existing body scrollers when a group is outside the
current width.

`Theme.MenuAppearance` accepts immutable `RibbonMenuAppearanceSettings` for
ordinary menu headings, horizontal separators and overflow scroll bands:

| Members | Defaults and behavior |
| --- | --- |
| `HeaderCornerRadius`, `HeaderInset`, `HeaderBorderWidth`, `HeaderBorderColor` | Actual renderer corners, one-device-pixel inset and borderless ink. Logical ranges 0..64 / 0..1024 / 0..32; authored ink retains alpha. Existing MenuHeadingBackground/Foreground remain independent; shape alone does not invent a neutral fill. |
| `HeaderBold` | Null/true adds Bold to host styles. False removes Bold and retains other styles. Header bounds remain unchanged. |
| `SeparatorThickness`, `SeparatorColor` | One device pixel and Border ink. Thickness 0..32 logical pixels clamps to the unchanged row and stays centered; zero/Transparent hides it. Existing row height and horizontal padding remain in use. |
| `OverflowBackground` | Opaque PopupBackground/Surface. Authored solid/gradient stops must both be opaque to cover scrolled content. |
| `OverflowCornerRadius`, `OverflowInset`, `OverflowBorderWidth`, `OverflowBorderColor` | Actual renderer active corners, one-device-pixel inset and transparent outline. Same logical ranges as headings. Hover/Pressed, foreground/disabled ink and Chevrons retain their existing roles; disabled/idle bands do not synthesize active chrome. |

```csharp
ribbon.Theme = ribbon.Theme with
{
    MenuAppearance = new RibbonMenuAppearanceSettings
    {
        HeaderCornerRadius = 8, HeaderBorderWidth = 2,
        HeaderBorderColor = Color.DarkBlue, HeaderBold = false,
        SeparatorThickness = 3, SeparatorColor = Color.DarkMagenta,
        OverflowBackground = new RibbonFill(Color.LightYellow, Color.LightSkyBlue)
    }
};
```

Null members/Empty ink reset independently; `MenuAppearance = null` resets this
group. Ranges and opaque-band requirements validate before Theme/BuiltInTheme
mutation. Ordinary menu settings preserve row/header/band targets, native state,
keyboard/automation and scrolling behavior. Actual menu/gallery-heading/chrome/
text/separator/band overrides retain precedence. Gallery headings, Application
Menu and Backstage decoration retain their separate recipes.
The optional **Menu decoration effects** group is on **Theme effects 4**. **Try
scrolling overflow** temporarily adds sample-owned toolbar commands; **Restore
preview toolbar** restores the original items and settings. Use the existing body
scrollers to reach these groups when needed.

`Theme.ApplicationMenuAppearance` accepts immutable
`RibbonApplicationMenuAppearanceSettings` for Application Menu command states:

| Members | Defaults and behavior |
| --- | --- |
| `RowCornerRadius`, `RowInset`, `RowBorderWidth`, `RowBorderColor` | Active navigation/pane rows: square, one-device-pixel inset, zero-width transparent outline. Logical ranges 0..64 / 0..1024 / 0..32. Set both width and ink to add an outline; alpha is supported. Idle rows retain their existing eligibility. |
| `FooterInset`, `FooterBorderWidth`, `FooterBorderColor` | One-device-pixel inset and stroke with PopupBorder ink, including the legacy transparent-border rim. Logical ranges 0..1024 / 0..32. Zero removes the stroke. Existing ApplicationMenuFooterCornerRadius remains independent. |
| `SplitDividerWidth`, `SplitDividerInset`, `SplitDividerColor` | Active seam: one device pixel with one-device-pixel end clearances and Border ink. Logical ranges 0..32 / 0..1024, clamped inside the unchanged row. Zero/Transparent hides paint. ApplicationMenuSplitDividerVisible remains authoritative, including Office 2019's false default. |
| `SelectedPrimaryTintStrength`, `SelectedPrimaryBackground` | Unhovered open split primary: 0.25 opaque PopupBackground/Pressed mix. Strength is finite 0..1. An explicit alpha/gradient fill takes precedence. Hover/press/checked and expanded-arrow rules retain their existing order. |

```csharp
ribbon.Theme = ribbon.Theme with
{
    ApplicationMenuSplitDividerVisible = true,
    ApplicationMenuAppearance = new RibbonApplicationMenuAppearanceSettings
    {
        RowCornerRadius = 8, RowBorderWidth = 2, RowBorderColor = Color.DarkBlue,
        FooterBorderWidth = 2, FooterBorderColor = Color.DarkOrange,
        SplitDividerWidth = 3, SplitDividerInset = 6,
        SplitDividerColor = Color.DarkMagenta, SelectedPrimaryTintStrength = .55
    }
};
```

Null members/Empty ink reset independently; `ApplicationMenuAppearance = null`
resets this group. Divider visibility remains an independent setting. Ranges
validate before Theme/BuiltInTheme mutation. Split halves share one perimeter;
decoration does not change command targets, content/focus bounds or native state.
Actual item/split/text/image/arrow/focus hooks retain precedence. Backgrounds,
headings, content layout and the shared outer popup frame retain their owners.
The optional **Try Application Menu / Application Menu state effects** groups
are on **Theme effects 5**. Use **Show / hide split dividers** when trying divider
colors in Office 2019. The preview uses existing File models; **Restore File mode**
returns the prior mode. Earlier tabs remain in place; use tab/body scrollers when
needed.

The same `ApplicationMenuAppearance` group also controls Application Menu
backgrounds, inner frame and headings:

| Members | Defaults and behavior |
| --- | --- |
| `SurroundBackground`, `NavigationBackground`, `DefaultPaneBackground`, `ActivePaneBackground` | Independent solid/gradient fills. Null keeps the old surrounding/default tint and PopupBackground/Surface navigation/selected pane. Authored alpha composites over the established opaque backing and existing layers; application-owned Controls retain their own styling. |
| `DefaultPaneTintStrength` | Finite 0..1 Border contribution to the opaque default-pane/surround fallback; null keeps 0.125. An explicit pane fill takes precedence for that pane; surrounding fill remains independent. |
| `InnerFrameCornerRadius`, `InnerFrameBorderWidth`, `InnerFrameBorderColor` | Logical ranges 0..64 / 0..32. Null follows actual GetPopupCornerRadius minus one device pixel and a one-device-pixel Border stroke. Empty ink follows Border; zero/Transparent hides it. Shared paint clipping protects the border, preserving at least the existing device-pixel inset. |
| `NavigationDividerWidth`, `NavigationDividerInset`, `NavigationDividerColor` | One-device-pixel width/end clearances and Border ink at null/Empty. Logical ranges 0..32 / 0..1024, clamped inside the unchanged inner frame. Zero/Transparent hides paint; the measured divider position remains unchanged. |
| `HeaderCornerRadius`, `HeaderBold` | Square heading fill and host styles plus Bold at null. False removes Bold while preserving other host styles; radius range 0..64 logical pixels. Existing MenuHeadingBackground/Foreground and selected-pane GalleryHeadingBackground remain in use. |
| `DefaultHeaderTextInset`, `ActiveHeaderTextInset` | 2/10 logical pixels at null. Authored values 0..1024 clamp within the unchanged header. |
| `HeaderRuleThickness`, `HeaderRuleColor` | One-device-pixel bottom rule with Border ink at null/Empty. Authored thickness 0..32 logical pixels clamps within the header. Alpha is retained; zero/Transparent hides paint. |

```csharp
ribbon.Theme = ribbon.Theme with
{
    ApplicationMenuAppearance = (ribbon.Theme.ApplicationMenuAppearance ?? new()) with
    {
        DefaultPaneTintStrength = .4,
        InnerFrameCornerRadius = 12, InnerFrameBorderWidth = 3,
        NavigationDividerColor = Color.DarkMagenta,
        HeaderBold = false, ActiveHeaderTextInset = 18,
        HeaderRuleThickness = 3, HeaderRuleColor = Color.DarkGreen
    }
};
```

These members follow the same atomic validation and independent null/Empty/group
Reset contract. They preserve measured columns, header height, command targets
and native content bounds. Public background/header/text/radius hooks remain
authoritative; ordinary menu decoration and the outer popup frame remain
independent. **Application Menu surfaces / Application Menu headings** are on
**Theme effects 5** beside the earlier File previews. Each new group has its own
Reset, preserving the other surface and D4a command-state settings. Use existing
group/tab/body scrolling and reduction as needed.

`Theme.ApplicationMenuContent` accepts immutable
`RibbonApplicationMenuContentSettings` shared by Application Menu measurement and
painting:

| Members | Null defaults and authored behavior |
| --- | --- |
| `LargeIconSize`, `SmallIconSize` | 32 navigation/illustrated and 16 ordinary-pane/footer logical pixels. Zero hides artwork; authored slots clamp inside commands. Native fields keep their frame metrics. |
| `LargeIconInset`, `SmallIconInset`, `IconTextGap` | 8/6/8 logical pixels. Footer artwork/label packs remain centered. |
| `LargeTextInset`, `TextInset`, `TextTrailingPadding` | 52 for navigation/illustrated labels; 6 for icon-free ordinary labels. Trailing allowance remains 6 for paint/8 for description measurement at null; an explicit value unifies them. Authored labels reserve at least artwork plus gap. |
| `RowTextVerticalPadding`, `IconVerticalPadding` | Total 10/8 logical pixels. Shared row measurement consumes authored values, retaining existing minimum rows and Touch rules. |
| `DescriptionGap`, `DescriptionVerticalInset`, `DescriptionMaxLines` | 2-pixel gap, 4-pixel top/bottom inset and two lines. Authored inset is also reserved in measurement. Zero lines hides descriptions and removes their height/gap. |
| `RichTitleBold` | Null/true adds Bold to described illustrated titles; false removes Bold while preserving other host styles. Existing title/secondary/disabled colors remain in use. |
| `FooterHorizontalPadding` | Existing centered paint/preferred-width recipes at null. Authored values reserve symmetric padding around label plus artwork, including check/radio indicators. |
| `ArrowColumnWidth` | 26 Compact/44 Touch logical pixels. Authored widths keep the 44-pixel Touch minimum; split action targets clamp to half the row. D1b glyph/Chevrons settings remain independent. |
| `SeparatorHeight`, `SeparatorHorizontalPadding`, `SeparatorThickness`, `SeparatorColor` | 9/8 logical pixels, one device pixel and Border ink. Zero height removes row space (footer separator width); zero thickness/Transparent hides ink. Stroke/inset clamp to the slot; authored alpha is supported. |

```csharp
ribbon.Theme = ribbon.Theme with
{
    ApplicationMenuContent = new RibbonApplicationMenuContentSettings
    {
        LargeIconSize = 40, SmallIconSize = 24,
        LargeTextInset = 72, IconTextGap = 12,
        DescriptionMaxLines = 3, DescriptionGap = 6,
        RichTitleBold = false, ArrowColumnWidth = 38
    }
};
```

Dimensions accept 0..1024 logical pixels, arrow width 1..1024, line cap and
separator stroke 0..32. Values validate before Theme/BuiltInTheme mutation.
Null/Empty members reset independently; `ApplicationMenuContent = null` resets
only content, preserving `ApplicationMenuAppearance`. Null and empty records
retain legacy rounding and allowances. Authored metrics intentionally affect
command measurement and hit targets; native editor frames, bindings and borrowed
Controls remain with their existing owners. Actual item/image/indicator/text/
arrow/focus/separator hooks remain authoritative. Ordinary menus and Backstage
retain separate settings.
**Application Menu content / Application Menu descriptions** are on **Theme
effects 5**. Apply presets, then use the existing File/default/selected-pane
previews. The descriptions reset retains icon/arrow settings; **Default all menu
content** clears this whole settings group. Existing group reduction and tab/body
scrolling remain available.

`Theme.GalleryAppearance` accepts immutable `RibbonGalleryAppearanceSettings` for
built-in tile-state, noninteractive gallery-heading and inline frame/utility decoration:

| Members | Null defaults and behavior |
| --- | --- |
| `TileCornerRadius`, `TileInset`, `TileBorderWidth`, `TileBorderColor` | Actual renderer corners, one-device-pixel inset, and one-device-pixel stroke when ink exists. Empty ink keeps Accent for selected tiles and transparent ink otherwise. Radius/inset/stroke ranges are 0..64 / 0..1024 / 0..32 logical pixels. Alpha is retained; zero/Transparent hides the outline. Settings do not activate idle tiles or alter content/focus/target slots. |
| `HeaderCornerRadius`, `HeaderInset`, `HeaderBorderWidth`, `HeaderBorderColor` | Same ranges and actual-primitive fallbacks for headings. Empty ink keeps the transparent outline. Shape alone does not add a neutral heading fill; explicit ink can add only an outline. Existing heading text padding remains independent. |
| `HeaderBackground` | Optional alpha/gradient fill for both renderers. Null keeps no neutral fill and existing GalleryHeadingBackground for Office. Neutral SecondaryText and Office GalleryHeadingForeground retain their roles. |
| `HeaderBold` | Null/true adds Bold to host styles; false removes Bold and preserves other styles. Measured heading height and text bounds remain unchanged. |
| `StripCornerRadius`, `StripInset`, `StripBorderWidth`, `StripBorderColor`, `StripBackground` | Same dimension ranges and primitive defaults as tile chrome. Empty ink follows PopupBorder; null fill follows Surface. Alpha/gradient fills and zero/Transparent outlines are supported. Decoration does not change native content padding, presenter bounds or utility targets. |
| `UtilityCornerRadius`, `UtilityInset`, `UtilityBorderWidth`, `UtilityBorderColor` | Independently decorate existing enabled Hover/Pressed buttons, with the same ranges and primitive defaults. Empty ink keeps a transparent outline. Shape/ink does not activate idle or disabled buttons. |

```csharp
ribbon.Theme = ribbon.Theme with
{
    GalleryAppearance = new RibbonGalleryAppearanceSettings
    {
        TileCornerRadius = 10, TileInset = 3,
        TileBorderWidth = 2, TileBorderColor = Color.DarkBlue,
        HeaderBackground = new RibbonFill(Color.LightYellow, Color.LightSkyBlue),
        HeaderCornerRadius = 8, HeaderBold = false
    }
};
```

Values validate before Theme/BuiltInTheme mutation. Members reset independently
with null/Empty; `GalleryAppearance = null` resets only this group. Paint dimensions
clamp inside existing bounds. Actual tile/heading/protected chrome/text/image/
focus overrides retain precedence. Existing Hover/Pressed/Checked fills,
source selection/preview/commit and presenter/native ownership retain their owners.
Artwork/label geometry uses the separate GalleryContent settings below.
Ordinary-menu headings keep their separate decoration. Application presenter visuals remain application-owned
and can explicitly use shared public renderer hooks.
The optional **Try built-in galleries / Gallery tile states / Gallery headings**
groups are on the requested new **Theme effects 6** tab. Tile and heading member
resets retain the other roles; **Default all gallery effects** clears the group.
Earlier controls remain on their original tabs. Use group reduction and tab/body
scrolling as needed.

`Theme.GalleryContent` accepts immutable `RibbonGalleryContentSettings` for built-in
artwork/labels and inline utility glyphs:

| Members | Null defaults and behavior |
| --- | --- |
| `LabelMaxLines`, `LabelAlignment` | Two lines, MiddleCenter. Line count is 0..32; zero hides labels and removes their measured line allowance. Any defined ContentAlignment is accepted. Wrapping/ellipsis remain; inline labels clamp to the existing viewport. |
| `ArtworkHeightReserve` | 0..1024 logical pixels, added to triple existing GalleryTilePadding. Null preserves legacy paint 12 / popup-measure 24; an explicit value unifies those allowances. |
| `ArtworkHorizontalInset`, `ArtworkTopInset` | 0..1024, null GalleryTilePadding + 4 / + 2. Explicit top inset also adjusts popup minimum-height measurement. Artwork slots clamp to the tile and preserve raster aspect fitting. |
| `ExpandGlyphHeight`, `ExpandBarWidth`, `ExpandBarInset`, `ExpandBarStrokeWidth` | Null 18 / 10 / 4 / DPI-rounded 2 with a one-device-pixel stroke minimum. Height/width ranges are 0..64, inset 0..1024, finite stroke 0..32. Zero height hides both expand marks; zero bar width/stroke hides the bar. A consuming inset hides the bar. Authored bar geometry clips to its existing action target. |
| `UtilityGlyphColor` | Empty retains enabled Foreground / DisabledColor; alpha is supported and Transparent hides glyphs. Existing Chevrons still controls chevron length/depth/stroke. Up/Down slots and utility action targets remain unchanged. |

```csharp
ribbon.Theme = ribbon.Theme with
{
    GalleryContent = new RibbonGalleryContentSettings
    {
        LabelMaxLines = 3, LabelAlignment = ContentAlignment.BottomRight,
        ArtworkHorizontalInset = 14, ArtworkTopInset = 10,
        ExpandBarWidth = 14, ExpandBarStrokeWidth = 3,
        UtilityGlyphColor = Color.DarkMagenta
    }
};
```

Values validate before Theme/BuiltInTheme mutation. Null/Empty resets a member;
`GalleryContent = null` preserves GalleryAppearance. Actual image/text/focus and
frame/utility/chrome hooks remain in use, as does shared disabled-raster treatment.
Custom presenter card internals remain application-owned.
The new **Theme effects 7** tab contains **Try gallery content / Gallery artwork
and labels / Gallery inline frames / Gallery utility glyphs**. Label, frame and
glyph resets retain the other roles; frame reset also retains tile/heading settings.
**Default all gallery content** clears only GalleryContent. Existing D5a controls
remain on Theme effects 6, which the user declared full. Keep new controls on tab 7
until directed otherwise.

`Theme.TipAppearance` accepts immutable `RibbonTipAppearanceSettings` for shared
ScreenTip title, keyboard-badge outline and Backstage badge placement:

| Members | Null defaults and behavior |
| --- | --- |
| `ScreenTipTitleBold` | Null/true adds Bold to host styles; false removes Bold and preserves other styles. Shared ScreenTip measurement and actual text painting use the same weight. Foreground and existing width/padding/gap/frame settings remain in use. |
| `KeyTipInset`, `KeyTipBorderWidth` | Paint inset 0..1024 / outline width 0..32 logical pixels. Null retains zero inset / one-device-pixel outline. Zero stroke hides it; authored alpha follows existing KeyTipBorder. Paint dimensions clamp inside the badge; text/command targets do not move. |
| `BackstageKeyTipRightClearance` | 0..1024, null 4 logical pixels. Badge placement clamps inside the existing entry target. |
| `BackstageKeyTipHorizontalClearance`, `BackstageKeyTipVerticalClearance` | Total deductions from the entry's available badge width/height, 0..1024, null 8 / 4 logical pixels. Zero permits the full dimension; consuming a dimension hides the badge. Existing KeyTipHorizontalPadding / KeyTipVerticalPadding retain independent text-sizing and legacy summed fractional-DPI rounding. |

Existing KeyTipBackground/Foreground/Border and Office KeyTipCornerRadius keep
their roles; neutral badges remain square. Members reset with null;
`TipAppearance = null` retains existing colors, corners, padding and unrelated
gallery settings. Values validate before Theme/BuiltInTheme mutation. Actual
DrawScreenTip/DrawKeyTip/DrawText overrides remain authoritative. Passive-window
delay/lifetime, keyboard routing and native page/editor ownership remain shared.

**Try tips and badges / Tip presentation** are on **Theme effects 7**. Hover over
the preview commands for ScreenTips, or press Alt/F10 for badges. **Inset Backstage
badges** opens the authored Backstage; press Alt/F10 there to show its badges.
Close Backstage and choose **Restore File mode** to restore the prior mode.
Placement Reset retains title/outline overrides; **Default tip presentation**
clears only TipAppearance. Existing tab/body scrollers and group reduction remain available.

`Theme.OptionsAppearance` accepts immutable `RibbonOptionsAppearanceSettings` for
library Options navigation, dividers, buttons, headings, frames and command-list rows:

| Members | Null defaults and behavior |
| --- | --- |
| `NavigationCornerRadius`, `NavigationSelectionInset` | Radius 0..64 / per-edge vertical inset 0..1024 logical pixels. Null uses the actual renderer's GetPopupCornerRadius and the existing 2-pixel selection inset. Authored inset clamps inside the unchanged native row. |
| `NavigationTextPadding`, `NavigationFocusInset` | Horizontal text padding 0..1024, null 12 logical pixels; native focus inset 0..1024, null one device pixel. Authored values clamp inside the row without changing its hit target. Native focus style remains. |
| `NavigationSelectedBackground` | Optional alpha/gradient fill, null Checked. Rail backing remains BackstageNavigationBackground; text remains selected Accent / ordinary Foreground. |
| `NavigationMarkerWidth`, `NavigationMarkerInset`, `NavigationMarkerFill` | Width 0..64, null 3; per-edge vertical inset 0..1024, null legacy 7 top / summed 14 height deduction; fill null Accent. Existing NavigationMarkerCornerRadius remains in use. Zero width, Transparent fill or a consuming inset hides marker paint. Explicit inset uses equal ends. |
| `DividerWidth`, `DividerColor` | Width 0..32, null one device pixel; ink Empty follows Border. Zero/Transparent hides the rules; alpha is honored. Authored stripes clamp inside existing rail/footer panels. |
| `ButtonInset`, `ButtonBorderWidth`, `ButtonBorderColor`, `ButtonFocusInset` | Paint inset 0..1024 / border width 0..32 / focus inset 0..1024 logical pixels. Null retains zero paint inset, ordinary one-device-pixel border when ink exists, accent border zero, and four-device-pixel focus inset. Empty ink follows PopupBorder / accent fill's first color. Zero/Transparent hides the outline; alpha/clamps are supported. Button content, native targets and IButtonControl/default/cancel behavior remain. Existing OptionsButtonCornerRadius is reused. |
| `ButtonBackground`, `AccentButtonBackground` | Optional enabled idle fills, including alpha/gradients; null Surface / Accent. Existing Hover/Pressed/disabled state recipes and foreground roles remain. Ordinary buttons with null corners retain native painting until an ordinary-button override is authored. Accent-only fill and heading/navigation settings do not opt ordinary buttons into shared painting. |
| `HeadingFontSizeIncrement`, `HeadingMinimumFontSize`, `HeadingBold`, `HeadingBottomPadding` | Finite point-size increment 0..64 / minimum 1..128, null 5 / 14; bottom padding 0..1024 logical pixels, null 12, zero removes the gap. Null weight retains legacy Bold-only style; explicit true adds Bold to host styles, false removes it and preserves other styles. The resulting font and padding feed existing native heading/page layout. |
| `FrameCornerRadius`, `FrameBorderWidth`, `FrameBorderColor` | Native field/list wrapper radius 0..64 / stroke 0..32 logical pixels; null retains the actual popup/command-radius minimum and one-device-pixel stroke. Empty ink follows PopupBorder; alpha/Transparent and zero/hide are supported. Wider outlines reserve native child padding and search/name-field height. Zero keeps minimum one-device-pixel clearance; Transparent retains reserved space. Child corner regions use the same resolved shape. Surface/native backgrounds retain their owners. |
| `RowTextPadding`, `RowIconSize`, `RowIconTextGap` | QAT/Classic/Simplified command-list content padding and icon gap 0..1024 / icon size 0..256 logical pixels. Null retains padding/gap 6 for QAT/Classic, 8 for Simplified and icon size 20, with legacy ceiling rounding. Authored content clamps inside the existing row; zero icon size hides borrowed artwork and reclaims its slot/gap. Native row height and targets stay unchanged. |
| `RowCornerRadius`, `RowSelectedBackground` | Selection corner radius 0..64, null 0; optional alpha/gradient fill, null solid Checked.Start. Authored paint composites over Surface; Transparent hides it. Foreground, unselected Surface and native focus rectangles retain their existing roles. Tree and icon-picker selection are separate. |
| `RowHeadingBold` | Simplified command-list header weight. Null retains legacy Bold-only style; true adds Bold to host styles, false removes it and preserves other styles. Choose before opening; existing construction/font/DPI paths own the font. Dialog title headings are independent. |
| `TreeCornerRadius`, `TreeSelectedBackground` | Customization-tree selection radius 0..64, null 0; optional alpha/gradient fill, null solid Checked.Start. Authored selection composites over Surface; Transparent hides it. List-row selection remains independent. |
| `TreeCheckSize`, `TreeIconSize`, `TreeIconTextGap` | Check size 0..64, icon size 0..256 and icon-to-text paint gap 0..1024 logical pixels. Null retains Compact/Touch checks 18/24, icon 16 and gap 2. Authored values fit existing check/icon reservations and row height. Zero hides the corresponding glyph; native click targets, indentation, text bounds and scrolling stay unchanged. The gap positions artwork to the left of existing native text. Visible checks keep the actual DrawCheckBoxIndicator hook. |
| `TreeFocusInset` | Native tree focus-rectangle inset 0..1024 logical pixels; null one device pixel. Authored inset clamps inside the row. Focus style and cue eligibility remain native. |
| `IconPickerCornerRadius`, `IconPickerInset` | Group icon-picker selection radius 0..64 / paint inset 0..1024 logical pixels. Null retains the actual popup/command-radius minimum and one-device-pixel inset. None and artwork use the same square cell; inset and outline clearance constrain both content types. |
| `IconPickerBorderWidth`, `IconPickerBorderColor`, `IconPickerSelectedBackground` | Selection outline width 0..32, null one device pixel; Empty ink follows Border, null fill follows Checked. Alpha/gradient/Transparent and zero outline are supported. Transparent fill preserves an independently configured outline. |
| `IconPickerIconSize`, `IconPickerTextInset` | Artwork size 0..256 / extra None-label inset 0..1024 logical pixels. Null retains Compact/Touch artwork 24/32 with ceiling rounding. Positive artwork size also resizes the common square cells, respecting font/density minima and the native 256-device-pixel cell limit. Zero hides artwork with the default cell footprint. None uses the shared selection/content clearance before its extra text inset; font/Foreground/centering remain. |

Legacy navigation/heading/search arithmetic keeps its ceiling-based DPI scaling
and summed rounding; new frame/choice overrides use the shared logical-pixel
scaling. Values validate before Theme/BuiltInTheme mutation. Null/Empty resets
each member; `OptionsAppearance = null` preserves button corners, tip/gallery
settings and the existing palette. Dialog sizes, native button/image/text/focus
behavior, application page styling and draft Apply/Cancel retain their existing
owners. The actual popup-radius hook remains in use; navigation/button text keeps
its direct native painter.

Search/name text and closed-choice values reuse `EditorTextInset` and
`EditorForeground`. Null preserves four-logical-pixel search padding (including
legacy summed rounding) and six-logical-pixel choice padding. Closed choices
also reserve existing `EditorAppearance` field inset/stroke around the value;
their actual DrawComboBox/DrawText and popup choice roles remain authoritative.
Disabled choice ink retains the native SystemColors.GrayText policy. Native
editors, handles, parents, values, selection and choice staging retain their owners.
Choose geometry presets before opening; existing layout/font/DPI refresh applies
the settings. A frame reset restores the native padding, including prior scaling.

**Try Options dialogs / Options navigation** are on **Theme effects 7**. Choose a
preset, then open **Options preview** or **ribbon/QAT options**. Marker Reset keeps
selection/divider settings; **Default Options decoration** clears only this group.
Earlier groups remain in place; existing tab/body scrollers and reduction remain available.

**Try Options shell / Options buttons / Options headings** are on the requested
new **Theme effects 8**. Choose presets before opening the preview; headings
resolve through the existing construction/load/font/DPI layout, without a live
theme coordinator. Button Reset retains headings/navigation; Heading Reset
retains buttons/navigation. Both retain existing button corners and unrelated
tip/gallery settings. The accepted tab 7 groups remain pinned in place.

**Try frames and choices / Options frames and text** also use **Theme effects 8**,
now five groups. Choose presets, then **Open frame/choice preview**. It uses sample
commands and an isolated public customization model, so preview edits stay there.
**Default Options frames**, **Default editor text** and **Default choice fields**
reset their own members and preserve the other effects. Existing row/choice popup
decoration remains independently adjustable.

**Try command rows / Command row content / Command row selection** are on the
requested new **Theme effects 9**. Choose presets before opening its QAT, Classic
or Simplified preview. These use isolated sample commands and preserve the real
ribbon's mode and sources. **Default row content** retains selection settings;
**Default row selection** retains content settings. **Default all row effects**
clears only the six row members, preserving frame/button/navigation effects.
The accepted five groups on tab 8 remain in place.

**Try customization tree / Tree glyphs / Tree selection and focus** also use
**Theme effects 9**, now six groups. Choose presets, then **Open tree preview**.
The preview uses isolated sample commands. Glyph/selection resets retain each
other; **Default all tree effects** clears only the six tree members. Native
checkbox/expander targets remain active when paint is hidden.

Tree expanders reuse `Theme.Chevrons.Length`, `Depth` and `StrokeWidth`, including
zero/hide. Their null defaults retain Compact/Touch long-axis sizes 12/16, the
legacy half-length depth and strokes 1.6/1.8 with a device-pixel minimum. Authored
geometry fits the existing expander slot. These settings also style the other
shared ribbon chevrons; **Default shared chevrons** resets that group independently.
Tree text keeps its direct native painter, existing Foreground and disabled
SystemColors.GrayText policy. Icon-picker selection remains independent.

**Try group icon picker / Icon picker selection / Icon picker content** are on
the requested new **Theme effects 10**, three groups. Choose presets, then
**Open group icon preview**. It opens the shared group editor through a public
customization form's New Group action, using isolated sample commands; the
preview closes after that editor. Selection/content resets retain each other;
**Default all picker effects** clears only the seven picker members and retains
tree/list/frame settings. Accepted groups on tabs 1–9 remain in place.

**Smaller picker tiles / Larger picker tiles** resize None and artwork together.
Native labels are hidden from layout so long command names cannot stretch the
highlight; names and tooltips remain available. Opening, layout/paint refresh,
font and DPI changes resolve the common square cells. Item identities, native
selection, scrolling and group validation remain with their existing owners.

Derive from `RibbonRenderer` for a flat/custom presentation, or from
`RibbonOffice2024Renderer` to retain Office styling while overriding selected
drawing. Earlier preview subclasses that called base painting to obtain Office
styling should now derive from the named Office renderer. Instances assigned to
`Renderer` remain borrowed and must outlive their ribbon/popups.

The shared hooks are `DrawSurface`, `DrawBody`, `DrawGroupSeparator`, `DrawPopupFrame`, `DrawPopupShadow`, `DrawScrollButton`, `DrawMinimizeButton`, `DrawText`,
`DrawQuickAccessToolbar`, `DrawQuickAccessItem`, `DrawQuickAccessSplitItem`, `DrawQuickAccessOverflow`, `DrawOverflowScrollButton`,
`DrawDropDownArrow`, `DrawSubmenuArrow`, `DrawMenuSeparator`, `DrawTextBox`, `DrawComboBox`, `DrawComboBoxItem`, `DrawScreenTip`, and protected
`DrawItemImage`/`DrawChrome`, in addition to
whole tab/group/item/split/focus/KeyTip methods. Content callbacks receive the
library's measured slots. Respect those rectangles, font and clipping; painting
does not redefine hit targets or desired size. The existing protected
`DrawRounded` helper remains available for custom chrome. These are independent
RibbonKit renderer types rather than subclasses of `ToolStripRenderer`.

Dropdown indicators use an 8 x 6 logical-pixel vector shape rather than a text
glyph. They scale with DPI and have reserved label/icon space in dropdowns,
split buttons and collapsed groups.

The provisional API is documented in XML and tracked in
`src/RibbonKit.WinForms/PublicAPI.Unshipped.txt`. Nothing has been promoted to a
shipped compatibility baseline. Public COM interfaces in the Accessibility
namespace exist for the native UIA bridge and are hidden from IntelliSense;
applications should use Windows UIA clients rather than implement those types.

## Verification and packaging

Run these commands from `winforms/`. Native keyboard/mouse checks briefly operate
their own test Form; let that process retain focus until it finishes.

The unit/STA suite contains 106 essential cases: the [test review](docs/M2-STATUS.md#essential-test-review--2026-10-26)
reduced the earlier set to 56; M2C adds three ComboBox cases and M2D adds three
check/radio state, grouping/binding and compact-rendering cases. M2E adds three
ScreenTip metadata, cancellation/lifetime and measurement/rendering cases.
One body-spacing case checks downward growth, retained content/editor sizes and
collapsed-group geometry across representative fonts and synthetic DPI scales.
M2F adds three menu metadata/ownership, content-slot/rendering and mixed-layout/
submenu-placement cases. Real keyboard and popup lifetime checks run in the shown
public consumer rather than relying on foreground focus during unit tests.
M2G adds three minimized-state/metadata, shared body geometry/reachability and
utility-slot/vector-direction cases. Its shown consumer covers native mode
gestures, binding, validation, native editor transfer, KeyTips and popup lifetime.
M3A adds seven source/lifetime/ownership, placement, overflow, host-independent row
and ScreenTip-anchor cases. Its shown consumer covers shared binding and native
pointer/keyboard input in both placements and overflow.
M3B adds three ComboBox-reference metadata/geometry, shared validation/binding and
mutation/lifetime cases. Its native consumer covers pointer-only hover, press/
drag cancellation, paint-state cleanup, inactive sources, editable-source choices,
overflow anchoring and one retained native editor per source.
M3C adds four menu/split-reference geometry/KeyTip, source activation/state,
cancellation/lifetime and retained-editor cases. Its shown consumer exercises
inactive sources, nested menus, callback-populated overflow, split halves,
source events and shared native fields through public APIs.
Two follow-up cases cover retained toggle overflow and handler-driven removal/
hiding; the affected native QAT fixtures also cover repeated mouse/keyboard toggles.
Four scroller cases cover row/band clipping and reachability, short-menu sizing,
opaque hover/pressed rendering and child-popup pause/lifetime. The shown QAT
consumer checks actual hover/held repeat, wheel, endpoints and command activation.
M3D adds four group membership/layout, shared native editor/action, source lifetime
and inactive overflow-stack cases. Its public consumer covers native fields,
binding, group/ComboBox QAT interaction, KeyTips, toggles, nested dismissal and
last-reference sizing restoration in both renderers.
M3E adds three checkbox binding/event-order, radio-scope and icon-toggle rendering
cases. The shown public consumer checks mouse/keyboard options, overflow retention,
mixed state, inactive sources, BelowRibbon/minimize and source lifetime in both
renderers. Run `--qat-option-checks` / `--qat-option-checks-flat` on the native host,
or `--validate-qat-options` / `--validate-qat-options-flat` on the independent
consumer, followed by an evidence directory.
M4A adds three contextual binding/metadata, visibility/selection/QAT lifetime and
renderer-state cases. Its shown public consumer covers context changes, tab
commands/KeyTips, minimized teardown, live tint, theme fallback and narrow strips.
Use `--contextual-checks` / `--contextual-checks-flat` on the native host, or
`--validate-contextual` / `--validate-contextual-flat` on the independent consumer,
followed by an evidence directory. These are focused scopes, not full acceptance.
M4B adds four File ownership, layout, activation/lifetime and renderer-state cases.
Its public consumer checks lazy/cancelled opening, commands/KeyTips, native field
binding and handle reuse, nested dismissal, minimized/narrow placement and disposal.
Use `--file-checks` / `--file-checks-flat` on the native host, or `--validate-file` /
`--validate-file-flat` on the independent consumer, followed by an evidence folder.
The existing editor alignment case also covers mixed rows. For routine changes, run the affected
test classes. Reserve the full native/package sequence for changes that need those
gates. Add cases for distinct failure modes and use representative DPI/font rows;
the four-corner symmetry regression deliberately keeps all four scales.

```powershell
./eng/Test-Native.ps1
./eng/Test-Native.ps1 -Configuration Release -Package
./eng/Measure-Baseline.ps1
```

For the bounded shown minimize scope, use `--minimize-checks` /
`--minimize-checks-flat` on the native test host, or `--validate-minimize` /
`--validate-minimize-flat` on the independent consumer, followed by an evidence
directory. The test Form retains foreground ownership while sending native input.

For the bounded shown menu scope, use `--menu-checks` / `--menu-checks-flat` on the
native test host, or `--validate-menus` / `--validate-menus-flat` on the independent
consumer, followed by an evidence directory. These cover described commands,
separators, nested/split/check/radio actions, native editors, passive tips,
keyboard return, edge flipping, scrolling and source removal.

For the bounded shown ScreenTip scope, run the native test host with
`--screentip-checks TestResults/visual/screentips-office` or
`--screentip-checks-flat TestResults/visual/screentips-flat`. The independent
consumer accepts `--validate-screentips` / `--validate-screentips-flat` followed
by an evidence directory. Both paths check delayed cancellation, native editor
selection, disabled commands, popup stacking and nested Escape; captures include
the popup and overlaid tip. They do not imply full native/UIA acceptance.

The package gate restores from an isolated local feed/cache into a fresh temporary
directory outside the checkout, builds the independent consumer and drives it
through the external UIA/input client. Its folder is printed and retained.
The consumer has no Showcase reference, copied helper or internal-access grant.

Build the solution before `dotnet pack src/RibbonKit.WinForms -c Release --no-build`.
The runtime package includes XML docs, license/notices and design support under
`lib/net10.0-windows7.0/Design/WinForms` (NuGet's canonical moniker for the target
alias). That folder contains the .NET Framework client/protocol; its `Server`
folder contains the modern design server/protocol. The designer SDK is not a
runtime dependency. This follows the [Microsoft package convention](https://github.com/microsoft/winforms-designer-extensibility/blob/main/docs/sdk/control-library-nuget-package-spec.md).
Design projects use Microsoft.WinForms.Designer.SDK 1.6.0.

The package declares `MIT AND MS-PL` for its bundled components; RibbonKit's own
code remains MIT. Application distributors must include the applicable notices
from the package's `licenses/` folder. These files are not automatically copied
into application publish or installer output; see [dependency notices](THIRD-PARTY-NOTICES.md).

Select a ribbon in the Form designer and choose **Edit Ribbonâ€¦** from its smart
tag, or open the **Tabs** property ellipsis. The custom window edits built-in
tabs/groups/commands as a detached draft; OK requests one designer transaction,
while Cancel leaves live components unchanged. Its **Add** menu includes contextual
tabs; the **Tab header** section edits their existing header text/indicator tint
and can reset it to the theme accent. The tree popup also provides add/remove and
ordering actions. Select the protected **Ribbon** root for built-in theme selection
and a static ribbon/Backstage preview. Its **Ribbon settings** also offers an
**Animation** dropdown: None (the default), Subtle or Expressive. This edits the
existing `AnimationLevel` property on OK, supports Undo/Redo and saves nondefault
values as ordinary enum assignments. Reset restores None; the image preview stays static.
**Tab content animation** offers Fade (default) or SlideDown and applies to tab
bodies, arriving headers, ribbon group collapse/restore and Backstage page changes.
Reset restores Fade. Both settings use the same detached draft and OK transaction.

The **Quick Access Toolbar** branch in Edit Ribbon edits toolbar entries,
visibility and placement. Add an existing command/group or create a QAT-only
button/toggle with no tab slot. New commands use the existing command catalog;
removing a toolbar entry keeps its source available to add again. Source editing,
compatible source changes, aliases/KeyTips, reorder and guarded command deletion
share the same Cancel/OK and Undo/Redo workflow. See
[QAT designer authoring](docs/QAT-DESIGNER-AUTHORING.md) for the bounded contract
and review checkpoint.
Groups also expose **Layout = Columns, TwoRows or ThreeRows**. Columns keeps
automatic Small/Medium stacking; explicit rows use each command's **Row** choice.
**Add â†’ Separator** inserts a vertical in-group divider. Split buttons expose
their existing **Layout = Horizontal/Vertical** choice. See
[group item layout](docs/GROUP-ITEM-LAYOUT.md) for authoring, centering and verification.
Dropdown and split commands expose **DropDownHeader** and **MenuPresentation**;
each child command's **Text**, **MenuDescription** and Icon produce a title with
an inline explanation and artwork. Standard matches the Paste options example;
IllustratedChoices offers larger artwork and bold titles. See
[menu authoring steps](docs/DESIGNER-AUTHORING.md#dropdown-headings-and-descriptions--2026-11-01).
**Move toâ€¦** moves existing commands between groups/menus across tabs, or groups
to another tab, with a row picker for explicit layouts. Size-policy choices now
offer only non-growing sequences while preserving existing authored values.
ComboBox **ShowLabel** hides its native-field label without erasing Text;
Small presentation opens a compact chooser using the same command/validation
owner. See [designer rearrangement](docs/DESIGNER-REARRANGEMENT.md) for authoring
and focused evidence.
The current native catalog offers
**Office2024Light** and **Office2019Light**; **(Application theme)** preserves the application value.
Command **SizePolicy** values have a dropdown of fixed/adaptive choices.
`BuiltInTheme` defaults to null; Reset restores the last application-assigned
`Theme`, initially Office 2024 Light. An explicit choice serializes as
`ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2024Light;`. Direct assignment to
`Theme` clears that choice. Custom theme authoring and native page-content styling
remain application-owned. The permanent
[Designer Sample](samples/RibbonKit.WinForms.DesignerSample/README.md) contains the
reviewed forms and has its own solution in `samples/RibbonKit.WinForms.DesignerSample`.
Run `./winforms/eng/Update-DesignerSample.ps1` from the repository root to rebuild
the local designer package and update that same project. Authored files are never
recopied by the refresh. `Prepare-DesignerConsumer.ps1` without a destination also
refreshes it; an explicit destination remains an optional independent fixture.
See [the authoring checkpoint and manual steps](docs/DESIGNER-AUTHORING.md)
for current evidence, actual-VS OK/Cancel, Undo/Redo and save/reopen review steps,
and acceptance limits.

In the Form designer, select the ribbon, then click tab headers, group captions
or commands to select their components in Properties. The smart tag's **Preview
tab** selector also reveals hidden/disabled contextual tabs without changing
authored properties or runtime startup selection. Fields use static designer
illustrations; preview clicks do not activate commands or open popups. **Edit
Ribbonâ€¦** reveals the selected component, and OK returns its tree selection to
Properties. See the [preview contract and current acceptance](docs/DESIGNER-AUTHORING.md#surface-preview-and-selection--2026-10-31);
physical-DPI and mixed-monitor review remain separate gates.

To show File, select the ribbon and check **Show File button** in its smart tag.
**File opens** chooses Command menu or Backstage. These edit the existing File
declaration with Undo/Redo support; File remains hidden until explicitly enabled.
The owned File declaration has matching designer defaults: caption **File**, KeyTip
**F**, visibility false. Enabling it serializes `FileMenu.Visible = true`; Reset
restores the File caption without affecting ordinary dropdown defaults.

The **Edit Backstageâ€¦** smart-tag action (also the **Backstage** property ellipsis)
opens a detached pages/commands/separators editor with icon picking, ordering, File routing
and a renderer-backed navigation preview. OK applies one designer transaction;
Cancel leaves the live model unchanged. Native page bodies are designed in
separate UserControls. Choose a page's optional `ContentType` in this editor for
page-owned runtime creation, or continue assigning application-owned instances
through `RibbonBackstagePageItem.Content`.
Select the protected **Main entries** or **Bottom entries** group before using
**Add â†’ Page / Command / Separator**. Change an entry's **Placement** dropdown
between **Main** and **Bottom** to move it; Move up/down orders its current section.
Both the editor image and Form designer preview show the bottom section.
All entries share one body placeholder in both previews; selection updates the
navigation and Properties. The Form designer retains its synthetic drawing in
a temporary opaque native layer, outside the authored Controls/component tree.
It never creates, reparents or captures page controls. See
[Backstage authoring and user review](docs/BACKSTAGE-DESIGNER-AUTHORING.md).
On the Form designer surface, File opens an inert Backstage preview when routed
to Backstage; Back closes it. Navigation clicks select components in Properties.
The **Preview Backstage** smart-tag action also works with File hidden. These
gestures do not change saved open state or activate application commands.

Raw measurements and captures go under ignored `TestResults/`. Builds use the
normal Debug/Release `bin` folders. The measurement tool records native paint
completion and resource checkpoints, not compositor frame rate or a leak proof.

See [the original plan](../docs/21-NATIVE-WINFORMS-PLAN.md),
[source provenance](docs/reference/WPF-1.1.0.md) and [license notices](THIRD-PARTY-NOTICES.md).
