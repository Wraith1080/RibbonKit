# M4I2 appearance inventory and palette increments

Status, **2026-10-30: inventory, KeyTip palette and requested backdrop-state/closing
corrections, revised Solid page frame and Backstage caption match implemented;
visual review of the new appearance is pending**.
This starts the appearance-consolidation checkpoint after the main component and
M6C work. It does not complete comprehensive theming or add another built-in theme.
Designer authoring, localization/RTL and additional palettes retain their later
slices. Current-machine M6C acceptance remains in its own plan.

## Ownership and compatibility

- `RibbonTheme` remains an immutable per-ribbon value. Existing properties, their
  types and renderer defaults are retained. There is no global theme coordinator.
- Ordinary configurable appearance belongs in the shared theme/renderers, not in
  Showcase subclasses. Custom rendering remains an extension point for different
  drawing behavior. No new Showcase setup helper is needed for this increment.
- Layout, clipping, native editor bounds, popup placement and hit testing must use
  the same geometry whenever a later metric becomes configurable. A paint-only
  margin/radius change does not establish that contract.
- Host document content, native page layouts, command data, icons, persistence and
  window lifetime remain application-owned. Mica/Acrylic and the opaque GDI hosts
  retain their accepted composition/lifetime behavior.
- The runtime `Theme` property is currently hidden from the designer. Typed
  collection editors exist, but a complete VS property/resource/undo round-trip
  remains a separate authoring gate. This slice does not claim that workflow.

## Inventory of implemented surfaces

This is a source inventory, not visual acceptance of every state. Source links
identify the owners so later changes can include all affected presentation layers.
One-device-pixel hairlines, antialiasing coverage, alpha repair and Win32 message
constants are rendering/platform rules, not automatically missing theme settings.

| Surface | Existing configuration and source | Remaining appearance work |
| --- | --- | --- |
| Form/ribbon surround and body | `Background`, `Surface`, `Border`, `BodyMargin`, `BodyHeight`, `BodyShadowColor`; [theme](../src/RibbonKit.WinForms/Rendering/RibbonTheme.cs), [Office renderer](../src/RibbonKit.WinForms/Rendering/RibbonOffice2024Renderer.cs) | Background/surface are solid colors; body radius is derived from the shared command radius. Separate body fill/radius, border sides/thickness and shadow metrics need a compatible proposal. Preserve joined body/QAT/message ownership. |
| Normal buttons and split buttons | `Foreground`, `Hover`, `Pressed`, `Checked`, `Accent`, `CornerRadius`; [shared renderer](../src/RibbonKit.WinForms/Rendering/RibbonRenderer.cs), [split layout](../src/RibbonKit.WinForms/Layout/RibbonSplitLayout.cs) | State borders and component-specific fills are coupled to shared roles. Icon/text/arrow spacing is measured locally. Do not stretch commands when adding body clearance. |
| Check/radio commands | Shared state fills, accent and text; indicator render methods in the shared renderer | Indicator border/fill/stroke and disabled presentation use shared colors/system gray. Distinguish indicator appearance from whole-row appearance before adding roles. |
| Tabs, contextual headers and indicators | Accent, foreground, secondary text, `TabHeight`, contextual model color; Office `DrawTab`, `GetTabIndicatorBounds` | Indicator thickness/insets, label clearance and contextual blending are local. Indicator measurement, drawing and motion endpoints must consume one metric contract. |
| Groups and collapsed openers | Secondary text, border, large/collapsed maximum widths; Office `DrawGroup`, shared content layout | Separator inset is 12 logical pixels; caption/icon/launcher metrics also live in layout. Separate shared defaults from component-specific overrides. |
| QAT and header utilities | Shared state fills, `QuickAccessBackdropOpacity`, `HeaderButtonInset`; [QAT layout](../src/RibbonKit.WinForms/Layout/RibbonQuickAccessLayout.cs) | Toolbar/button/overflow dimensions are density metrics. Any appearance expansion must retain TitleBar/TabRow/BelowRibbon identity, joined-frame policy and measured hit targets. |
| Command menus and overflow scrolling | Surface, foreground, secondary text, shared state fills; [menu layout](../src/RibbonKit.WinForms/Layout/RibbonMenuLayout.cs), popup surfaces | Compact/Touch row, icon gutter, description and arrow spacing are in density/layout helpers. Heading roles currently overlap gallery heading roles. Scroll rails/buttons need their own inventory when metrics change. |
| Native TextBox/ComboBox fields and choice rows | Surface/border/foreground, shared state fills, model input width; [editor layout](../src/RibbonKit.WinForms/Layout/RibbonEditorLayout.cs), [native presenter](../src/RibbonKit.WinForms/Editors/RibbonEditorManager.cs) | Padding/button dimensions and state borders are coupled. Disabled text still uses system gray; native-control colors do not all obey a painted-role override. Validate every native/presenter/choice layer before promising a configurable disabled palette. Preserve the corrected buffering and handles. |
| Gallery strip, tiles, headings and utilities | `GalleryHeadingBackground/Foreground`, shared fills/borders; [gallery renderer](../src/RibbonKit.WinForms/Rendering/RibbonRenderer.Gallery.cs), gallery layout helpers | Static gallery data has collection editing; dynamic data/artwork remains host-owned. Tile padding, selected borders, utility geometry and heading font/inset roles need consolidation. |
| Backstage rail, rows, Back button and page surface | `BackstageNavigationBackground/FontSize`, navigation width bounds/opacity, shared fills/accent; [rail layout](../src/RibbonKit.WinForms/Layout/RibbonBackstageLayout.cs), [entry renderer](../src/RibbonKit.WinForms/Rendering/RibbonRenderer.Backstage.cs) | Entry icon/text/marker insets, Back geometry and selected fill are local/coupled. Auto rail measurement and entry text placement must use the same future metrics. Native page content remains host-owned. |
| Message rows, action and close button | Message colors, `MessageMinimumHeight`, `MessageContentPadding`; [message layout](../src/RibbonKit.WinForms/Layout/RibbonMessageLayout.cs), message renderer | Action/close state colors and glyph metrics still share message text/accent roles. Preserve connected/square joins, standalone minimized presentation and the one outer shadow. |
| Popup frames and shadows | `PopupBorder`, `PopupCornerRadius`, `PopupShadowEnabled/Color`; [popup chrome](../src/RibbonKit.WinForms/Popups/PopupChromeWindow.cs), shared renderer | Blur is 6 logical pixels; chrome reserves 8. Body shadow offset is 2. A future blur/offset contract must reserve matching space and update native chrome bounds/cache keys, not just change paint. Windows shadow policy remains authoritative. |
| ScreenTips | Surface/border, foreground/secondary text, inherited font and popup frame; [ScreenTip window/layout](../src/RibbonKit.WinForms/Popups/RibbonScreenTipWindow.cs) | Maximum width 320, content padding 12, title gap 6 and placement clearance 8 are local logical metrics. Bold title is derived from the ribbon font. No need to invent separate colors unless independent styling is required. |
| KeyTips | New optional background/text/border overrides, described below | Geometry remains renderer-owned: Office retains radius 2 and neutral retains square corners. Placement/keyboard behavior is unchanged. |
| Caption buttons and material surround | Optional [RibbonForm](../src/RibbonKit.WinForms/RibbonForm.cs), existing theme text/hover/pressed, native frame/backdrop integration | Close hover uses the familiar red/white treatment; caption slot and glyph metrics are local. Treat caption styling as a separate host contract rather than mapping close-danger colors to ordinary button roles. Do not regress the material opening animation. |
| Customization/options dialogs | Standard WinForms controls, model state and icon resources | Native/system-control styling is distinct from owner-drawn ribbon theme coverage. Actual designer/package authoring remains unverified. A broad dialog restyle is not part of this increment. |

Cross-cutting remaining work: disabled/focus/open state roles; independent surface
fills and border roles; typography; shared density-aware geometry; radius and shadow
coverage. Keep these as bounded proposals. Reusing an existing suitable role is
preferred to adding a setting for every numeric constant.

## First implemented contract: KeyTip palette

The three additive settings were proposed before implementation:

| `RibbonTheme` property | Default/fallback | Authored behavior |
| --- | --- | --- |
| `RibbonFill? KeyTipBackground` | `null`: Office keeps pale yellow; neutral uses `Surface` | A supplied solid or vertical two-stop gradient is honored by both renderers. |
| `Color KeyTipForeground` | `Color.Empty`: Office keeps black; neutral uses `Foreground` | A supplied color changes badge text. |
| `Color KeyTipBorder` | `Color.Empty`: use existing `Border` | A supplied color changes the outline; `Color.Transparent` removes its visible coverage. |

```csharp
ribbon.Theme = ribbon.Theme with
{
    KeyTipBackground = new RibbonFill(Color.MidnightBlue),
    KeyTipForeground = Color.White,
    KeyTipBorder = Color.CornflowerBlue
};
```

These properties change drawing only. Existing geometry, default appearance,
keyboard handling, command ownership and backdrop setup are unchanged. They have
XML documentation and six additive Unshipped accessors; the shipped API baseline
is unchanged. Reset with `null`/`Color.Empty` to recover renderer-specific defaults.

## Verification and next bounded contract

Normal Debug and Release native solution builds passed with zero warnings/errors.
The public-only KeyTip consumer passed both renderers at 96/120/144/192 DPI:
default colors, three overrides, exact raster reset, gradient fill and transparent
border. Default/custom output images were inspected. It uses no Showcase helper,
reflection, internal access, windows or input injection. Run the native runner with
`--keytip-appearance-checks <directory>`. Artifacts are under
`artifacts/appearance-20261030/keytips-public/`.

The initial full native Release run passed **486/489**
(`appearance-full-release.trx`). Two assertions still expected the former
32-pixel tab height rather than the existing 40-pixel default. The layout test
now uses its authored theme height; the cross-thread test verifies that the
original theme object survives the rejected mutation. Only those two corrected
cases were rerun: **2/2** passed in `appearance-failed-height-cases-fixed.trx`.
The TreeView native-buffer test passed **1/1** in a fresh process
(`tree-fresh-process.trx`) but failed in the aggregate, leaving a test-order/setup
limitation. A global visual-style test-startup trial was removed; the repeated
aggregate was stopped at the user's request. No clean full-suite pass is claimed,
and the already passing cases were retained rather than repeated again.

Generated rasters are separate from actual mixed-DPI/display, live keyboard,
high-contrast, designer/package and human visual acceptance; those gates were not
rerun here.

The next appearance contract should address disabled and focus/state colors across
painted commands and native fields before palette expansion. First identify which
native disabled layers require a supported drawing extension; do not promise the
entire native control changes color from one new property. Body/button/popup radii,
shadow metrics and layout spacing follow as separate geometry slices. New built-in
themes should use those consolidated contracts rather than copying renderers.

## Backdrop state and closing review — 2026-10-30

The user supplied `1108-59` and five screenshots, with five bounded appearance
requests. The recording and later clarification identify a lingering white
Backstage page background during closing; they are not new startup/frame evidence.
Only File's hover change applies with the backdrop off. Review of this increment
remains separate from the accepted M6C startup/native-frame corrections.

| Requested correction | Implemented behavior |
| --- | --- |
| QAT and minimize/expand hover plus clicked/pressed visuals | Existing QAT alpha was already active, but multiplied an opaque grey fill by `.6`. Material states now use a low-alpha foreground tint; open and checked QAT states also retain translucency. Split QAT entries leave their inactive half clear. Overflow/chevron use the same palette, and artwork keeps its coverage. Solid and popup-menu fills are unchanged. |
| File hover should match tab headers | Office File shares tab indicator measurement and has no filled hover rectangle. Hover, press and keyboard focus use the tab-style underline in both Solid and material modes; disabled state and open accent remain distinct. Neutral retains its existing flat File/header treatment. |
| Lighter material tab hover underline | Only the unselected indicator's coverage is reduced. Selected-tab accent coverage and Solid indicators are unchanged. |
| Translucent Backstage selected navigation and Back hover/press | Material entries use the same new backdrop hover/pressed roles, independently of the rail's background-opacity preference. Text, icons and active markers keep their coverage; Solid entries retain their authored fills. |
| Fade Backstage as one complete panel | Transparent material closing fades one image of the already-visible composed panel, including the white page background. No component exit sequence runs before it. Native controls are not printed or reparented, and their handles/data survive close cancellation/reopening. |

The added `RibbonTheme` contract was proposed before implementation:

- `RibbonFill? BackdropHover`: null derives Foreground at alpha 24.
- `RibbonFill? BackdropPressed`: null derives Foreground at alpha 40. Idle checked
  QAT instead derives Accent at alpha 40, unless an explicit pressed fill is supplied.
- `float BackdropTabHoverOpacity`: default `.45`, finite and bounded to `[0,1]`.

Existing `QuickAccessBackdropOpacity` remains a multiplier, default `.6`. With the
new derived fills, QAT hover/pressed coverage is approximately **14/24**, rather
than 153 over a grey background. A supplied backdrop fill retains its authored
alpha before multiplication. Normal body/popup state roles and the requested
rail-background opacity are unchanged. XML documentation and seven additive
Unshipped accessors/override entries cover this increment; shipped API is untouched.

Closing uses one bounded, temporary composed image only for transparent material
navigation. It reads visible screen pixels once, avoiding WM_PRINT/DrawToBitmap on
native page controls. The live surface is hidden beneath its drawing until close
completes, or restored before that drawing is removed on reopening. Input settles
the close and cannot leak typing/clicks into the underlying document. Original
validation/cancellation, one Closed notification, reduced-motion/None behavior and
Solid's native fade remain. Off-screen, unavailable or greater-than-4,194,304-pixel
capture uses immediate completion. The image is released after the transition;
the runtime does not save it. Opening/page switching keep their live native path.

The first live capture rejected a parent-background-only correction: navigation
had faded to `.22` while the page remained at `.956`. Layering the whole transparent
native surface also exposed a black rail and was discarded. Neither strategy is
retained. The composed closing capture measured matching page/navigation opacity:
Mica **.516/.514** and **.151/.153**; Acrylic **.493/.494** and **.151/.149**.

Focused evidence in `artifacts/backdrop-appearance-20261030/`:

- **34 distinct affected cases** passed across the separate state, chevron and
  final closing selections. State rasters cover both renderers, Solid/material,
  96/120/144/192 DPI, hover/press/checked/split/overflow, File-vs-tab pixels,
  selected accents, Backstage fills, authored overrides and range validation.
  Two chevron assertions were repaired to compare actual antialiased glyph
  coverage instead of requiring alpha-255 pixels in a thin diagonal stroke; only
  those failed cases were repeated. Final closing lifecycle covers both materials
  with clear, partial and opaque rail backgrounds, reopening, native handle/data
  retention and input consumption. Earlier closing probes predate the final strategy.
- The public-only consumer produced Mica state captures and then checked actual
  Mica/Acrylic closing: **14 frames**, including four synchronized intermediate
  frames. Field handles/parents/data were retained. Headers, selected navigation
  and closing images were inspected. No Showcase helper, internal API/reflection,
  pointer or keyboard input was used. `composed-closing/` is the passing fade
  evidence; `public-live/` and `whole-surface-candidate/` retain rejected strategies.
- Run `--backdrop-appearance-checks <directory>` for controlled state painting and
  closing, or `--backstage-close-checks <directory>` for closing alone. The latter
  exists to retry the failed fade without repeating passing state scopes. These
  are controlled painting/frame checks, not physical pointer acceptance.

All review windows closed and visible checking stopped. No aggregate suite,
startup, unrelated passing component, package, actual VS, mixed-monitor or broader
accessibility campaign was repeated for this increment. User visual acceptance
of these five corrections remains pending.

## Left/top Backstage page shadow — 2026-10-30

The user added a soft page-shadow request and explicitly chose both Solid and
backdrop modes. Office now reserves six logical pixels above the page and beside
the navigation panel. The parent surface paints the soft left/top edge in that
space, so native navigation scrolling cannot obscure it and no shadow enters the
title bar. Right/bottom shadow pixels are clipped to the surface bounds. Native
page contents retain their opaque background and existing ownership.

The colour reuses `BodyShadowColor`; Transparent disables this shadow and its
clearance. A proposed-before-implementation public renderer hook,
`GetBackstagePageShadowInset(theme, deviceDpi)`, shares the same measurement between
paint and layout. Office returns scaled six-pixel clearance, neutral returns zero,
and custom renderers can override it. Effective clearance is bounded by the
available size and 32 logical pixels. The hook and Office override add two
Unshipped entries. Auto rail measurement reserves the matching extra space for
long labels without changing an explicitly authored total rail width.

The live background owns the stationary shadow. Transparent navigation-motion
images omit it, preventing double shading or a shadow that moves with the items.
The complete closing image naturally includes the page shadow.

Six additional affected shadow/width cases passed in `page-shadow.trx`: soft alpha
falloff on both edges at 96/120/144/192 DPI, opaque page pixels, no caption/right/
bottom bleed, consistent layout clearance, disabled/neutral fallback, narrow
bounds, authored width and retained native focus/selection/handles. Earlier passing
colour/fade selections were retained rather than repeated for this addition.
The public-only shadow capture passed Solid/Mica/Acrylic at actual 120 DPI, with
eight-device-pixel clearance and visible shadow on both edges. The Mica image was
inspected. Run only `--backstage-page-shadow-checks <directory>` to review this
addition; captures/log are in `page-shadow-native/` under the same artifact root.

Normal native Debug/Release solution builds passed with zero warnings/errors.
There are **40 distinct focused passing cases** across the requested changes and
this shadow addition, assembled from the separately recorded selections. No clean
aggregate, package, physical mixed-DPI or actual VS acceptance is implied. Review
windows are closed and visible checks have stopped. Human appearance review is pending.

## Solid page frame and Backstage caption — 2026-10-30

The user rejected the reserved grey strip between the Solid page and title bar.
This supersedes the Solid shadow choice in the preceding checkpoint. Solid now
uses a one-device-pixel top/left `Border`, with only the top-left corner rounded
to the existing Office body radius. Its outer frame begins directly beneath the
caption; the native page starts one pixel inside the border. The grey shadow band
is gone. Mica/Acrylic keep their prior shadow and clearance.

Frame drawing and native clipping share the new public renderer hooks
`GetBackstagePageBorderWidth` / `GetBackstagePageCornerRadius` (neutral: zero;
Office: one-pixel border and twice the shared logical `CornerRadius`). The native
page region excludes the antialiased corner edge so application Controls cannot
overwrite it. Other three corners remain square. Only the small corner bitmap is
cached, avoiding full-page bitmap allocations during resizing. Material changes
refresh the open page geometry/region and preserve the real field handles/data.
Five additive Unshipped method/override entries and XML comments cover the hooks
and Office background override; shipped API remains untouched.

The user also requested a matching title bar only while Backstage is open. The
integrated caption now uses `BackstageNavigationBackground`, with the same
navigation-opacity preference over material. The title-bar QAT leaves its already
painted background alone, preventing a white patch or compounded tint. Closing
restores the original caption background. This is a paint-time projection; the
authored theme, Form BackColor/Opacity, native styles and command ownership are
unchanged. Ordinary native Form captions remain host-owned.

Evidence in `artifacts/solid-backstage-frame-20261030/`:

- `solid-frame.trx`: **9/9** affected cases. They cover odd/even raster bounds at
  96/120/144/192 DPI, all four corner policies, top/left stroke joins and inner
  native clipping, no reserved Solid shadow band, material shadow falloff and
  open-page Solid/Mica/Acrylic switching with retained field handles/parents/data.
  Raster images were retained under `raster/`; the actual 120-DPI Solid image was
  inspected with the native corner clip in place.
- `native/result.log`: the public-only frame capture passed Solid's flush top,
  visible top/left borders and rounded corner; Mica/Acrylic retained both shadow
  edges. It used no pointer/keyboard input or Showcase helpers.
- `caption.trx`: **3/3** additional caption cases, including rail opacity 0/.65/1,
  Solid/material backgrounds, no double QAT tint and normal-background restoration.
  The caption-only native capture stopped on foreground loss before establishing
  a pass (`caption-native/result.log`). It was not retried automatically.

Only these changed scopes were selected; earlier passing hover/fade/keytip scopes
and the aggregate suite were not repeated. All review windows closed. Native
caption visual acceptance, broader DPI/display and designer/package gates remain
separate. Current user visual review remains pending.

## Whole-surface Backstage and caption slide — 2026-10-30

The user requested replacing every Backstage animation with one complete-surface
slide and then specified that the title-bar colouring should slide alongside it.
This supersedes the earlier navigation slide, native-page opening/page-switch
fades, composed-image closing fade and immediate caption tint described above.

The latest user review shortened travel to 15%: the live Backstage container starts
85% visible and slides right into place, then moves 15% left and hides on closing.
It uses cubic ease-out over 220/308 ms for
Subtle/Expressive. Navigation, background, page frame/shadow and application
controls move together. Child layout, parents, handles, selection and data stay
intact. The subsequent review added smooth opacity fades on opening/closing,
including the caption tint, while keeping the shortened travel and QAT hiding.
Page selection is immediate. There is no page capture,
moving navigation bitmap or stationary replacement-background control.

The RibbonForm caption tint paints up to the same moving edge and repaints in the
surface's timer turn. Caption buttons remain stationary. Only effective TitleBar
QAT layout/paint and hit targets are suppressed throughout Backstage opening, display and accepted
closing, including a canceled close. Closing restores the authored visibility,
placement, references and commands. No QAT preference events are raised and the
underlying ribbon/document height is retained, including BelowRibbon placement.
TabRow/BelowRibbon QAT presentation is retained, including a TitleBar preference
that currently falls back to TabRow; the Backstage surface can naturally cover those rows.
Solid/material and authored navigation opacity keep their existing palette rules.
Native opacity follows the slide clock, returning to full coverage after opening.
Transparent rails and frames fade premultiplied paint alongside their separate
native page layer, including text/selection. Both the surface and that page layer
repaint after movement, preventing invisible panels/fields.
Closing validation, delayed Closed/focus restoration, cancellation/reopening,
synchronous command closes and None/system-disabled policies are retained.
No public API was added.

Earlier full-width-slide evidence in `artifacts/backstage-slide-20261030/`:

- Normal native Debug solution build passed with zero warnings/errors; Showcase
  is rebuilt in its normal output folder.
- `backstage-slide-final.trx`: **43/43** affected unit/STA cases passed, covering
  full-width travel, immediate pages, caption edge/alpha, native ownership/state,
  policies/input/cancellation/disposal, existing frame/shadow and window motion.
  Initial partial-alpha pixel assertions required the normal one-channel GDI+
  premultiplication rounding allowance; that correction passed before the final run.
- `native-native-ink/result.log`: **10** public-only shown scopes passed at actual
  120 DPI: Solid/Mica/Acrylic, Subtle/Expressive and Compact/Touch, plus Mica/Acrylic
  navigation opacity .65/0. Intermediate captures verify the visible page beneath
  the tint edge; final captures assert real native field ink as well as retained
  handles/parents/data. Actual Solid, Mica and Acrylic intermediate/final images
  were inspected. Earlier captures exposed caption repaint lag, missing material
  panels and missing text in a separately redirected page; all three were repaired
  before this final capture run.
- Public ordinary-Form Office opening/closing prefixes passed in `native-office/`.
  Its input tail targeted a field before it had entered the viewport; the fixture
  now uses native F6 to settle opening. The independently selectable tail passed
  in `native-office-tail-fixed/`. `native-neutral/` passed the full motion scope.
  These cover pointer opening/Back, native editing/bindings, immediate page/KeyTip
  selection, validation, resize, synchronous commands, Alt+F4 and None. Evidence
  is assembled from these scopes; the initial Office run was not a clean pass.

Earlier quarter-width/QAT refinement evidence in
`artifacts/backstage-quarter-slide-20261030/`:

- Normal Debug native solution build passed with zero warnings/errors; Showcase
  is rebuilt in its normal output folder.
- `backstage-quarter-slide.trx`: **40/40** focused unit/STA cases passed, including
  75%-visible opening, hiding at the matching closing offset, caption synchronization,
  QAT suppression/restoration at all three placements and both authored visibility
  states, retained command references/preferences and unchanged host height.
- `native-final/result.log`: **10** public-only shown scopes passed at actual
  120 DPI for Solid/Mica/Acrylic, both levels/densities, and material rail opacity
  .65/0. Renderer probes verify that QAT chrome/items are absent during Backstage
  and return after closing. Captures verify shortened travel, native page/text
  visibility and the shared tint edge. Solid opening, Mica translucent open and
  Acrylic closing captures were inspected.

Latest fade/black-strip correction evidence in `artifacts/backstage-fade-20261030/`:

- The supplied 5.2-second recording was decoded and inspected. Frame 1.433 s
  shows a tall black strip along the moving material page's left edge.
  Motion now adds `SWP_NOCOPYBITS` to WinForms' own bounds transaction so newly
  exposed pixels repaint instead of copying clipped redirection pixels. The
  managed transaction preserves caption clipping and bounds notifications.
- Opening/closing use smoothstep opacity alongside the existing cubic slide.
  Native redirection is prepared before visibility at alpha 1 (retaining native
  hit testing), stays opaque between transitions and releases when hidden.
  Transparent paint scales all premultiplied channels together; repeated paints
  clear the buffer before applying coverage. Caption colour shares the same alpha.
- Normal Debug native solution/Showcase builds passed with zero warnings/errors
  after the user's Visual Studio debug run released its output locks.
- `backstage-fade-first.trx`: 58/58 affected cases passed. The final bounds-transaction
  change reran only the affected 40 motion/caption/window cases, passing 40/40 in
  `backstage-fade-final.trx`; the other 18 appearance cases retain their first-run
  evidence. `buffer-fade.trx` adds 3/3 premultiplied-channel/nonaccumulating-repaint
  cases. There are **61 distinct focused passing cases**, not a full-suite run.
- `native-final/result.log`: ten public-only Solid/Mica/Acrylic, density/level and
  transparent-rail scopes passed at actual 120 DPI. Captures include intermediate
  opening/closing fades, final native field ink and QAT suppression/restoration.
  `input-final/result.log` passed the ordinary-Form Office pointer, native editing,
  immediate page/KeyTip, validation, resize, synchronous command, Alt+F4 and None scope.
- `showcase-managed-move/`: ten actual Showcase opening/hover cases passed, covering
  Compact/Touch, Solid and Mica/Acrylic with clear/opaque rails. A separate screen
  sampler recorded intermediate compositor frames and found zero tall black strips.
  Actual intermediate/settled Mica and Acrylic frames were inspected. The capture
  guard checks visible window ownership as well as foreground process ownership.
  Earlier hidden-launch and direct-DLL-launch captures are excluded from acceptance;
  use the apphost executable so its native compatibility manifest applies.

Latest 15% travel and Solid RichTextBox correction evidence in
`artifacts/backstage-subtle-20261030/`:

- Solid opening now retains the already-composed document drawing below the ribbon
  in one passive temporary control behind Backstage. It captures before native
  sibling clipping changes, paints at natural size and releases when opening
  completes or is interrupted. It does not print/reparent/resize the document or
  Backstage pages. QAT/caption content is outside that retained drawing, preserving
  QAT hiding. Material behavior, fade curves and 220/308 ms timing are unchanged.
  This is internal library behavior; no new public API or host callback was added.
- Normal Debug native solution build passed with zero warnings/errors. Showcase
  is rebuilt in its normal output folder.
- The 40 affected behavior/motion/caption/window cases passed in
  `backstage-fifteen.trx`. That run also contained an unreliable desktop-pixel case:
  the shared test runner's prior DPI initialization changed its coordinate basis.
  That screen case was removed from the unit assembly and moved to a fresh,
  DPI-aware native process. The run is not reported as a clean aggregate pass.
- `native-richtext/result.log`: independent public-only Compact/Subtle and
  Touch/Expressive checks passed at actual 120 DPI with **zero changed initial
  document pixels**, no native resize or scroll, retained handle/parent/text/
  selection and no leaked temporary drawing. Before/initial-fade PNGs were inspected.
  The corrected standalone pre-fix screen probe exposed 30,215 changed pixels
  (`richtext-dpi-before.trx`); earlier coordinate-mismatched probes are diagnostic.
  Rerun with `--backstage-richtext-checks <directory>`; the independent package
  consumer exposes `--validate-backstage-richtext`.
- `showcase/None-1/result.log`: actual Showcase reports **zero changed initial
  RichTextBox pixels** for Compact and Touch. Its ten opening/hover scopes across
  Solid, Mica/Acrylic and clear/opaque rails passed with zero tall black strips.
  Solid before/initial-fade document captures were inspected.
- `native-input/result.log`: public-only Office Subtle/Expressive, Compact/Touch,
  pointer opening/Back, native editing/binding, immediate page/KeyTips, validation,
  resize, synchronous commands, Alt+F4 and None checks passed at actual 120 DPI.

QAT refinement, 2026-10-30: the user's hide request now applies only to effective
TitleBar placement. TabRow/BelowRibbon and ordinary-Form TitleBar fallback retain
their normal layout, renderer calls and targets. All 16 focused caption/QAT cases
passed in `artifacts/backstage-titlebar-qat-only-20261030/backstage-titlebar-qat-only.trx`,
including opening/closing/reopening and authored visible/hidden preferences.
The normal Debug solution/Showcase build passed with zero warnings/errors.

All review windows closed. Human motion/smoothness acceptance, broader physical
DPI/display, performance, Release/package and actual VS checks remain pending.
WPF runtime/build/CI were not changed or run.

## Showcase diagnostic organisation

Nine diagnostic-only partial files were moved into
[`Diagnostics/`](../samples/RibbonKit.WinForms.Showcase/Diagnostics/README.md).
Twenty-three diagnostic members were extracted unchanged from eleven mixed
feature files into the same folder, including capture-only renderers and native
input helpers. Application setup, handlers, persistence capture and Designer files
remain in their feature files; Program retains the existing switch dispatch.
The normal Debug Showcase build passed after relocation. No live input was needed
to verify this file organisation change.
