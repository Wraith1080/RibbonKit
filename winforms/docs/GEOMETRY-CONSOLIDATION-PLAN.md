# Native geometry consolidation

Status, **2026-11-04: the fifth batch's editor spacing, choice-row geometry and
taller-field Roomier extension are implemented and verified. The current review
result is recorded below. Await instruction before another batch or additional
themes**.
Baseline: `codex/Ribbonkit-Winform`, `9dd0054b9ebe4f6f9ff5b57bf75198e443f5b4e5`.
The three existing documentation handoff edits are preserved. This plan owns the
bounded geometry contract and review gate; additional geometry and themes require
a later increment after user review.

## Surface inventory

Values below are 96-DPI logical pixels, Compact / Touch, unless stated otherwise.
The inventory identifies ownership, not new acceptance of deferred surfaces.

| Surface | Public settings and current defaults | Owners and consumers | Consolidation / boundary |
| --- | --- | --- | --- |
| Body and groups | `BodyHeight=110` minimum, `BodyMargin=(7,1,7,7)`, `LargeButtonMaxWidth=108`, `CollapsedGroupMaxWidth=108`, scroll vertical inset 2 | `RibbonTheme`, `RibbonLayout`, content/reduction/Simplified layout, Office renderer, body viewport, hit targets, group motion | Preserve font-driven three rows and joined frame. Group separator clearance 12 and collapsed opener insets 6/8 are separate deferred roles. |
| Header, tabs and utilities | `TabHeight=40` minimum (Touch 48), `HeaderButtonInset=4`; QAT optical offset 2 | density/header/caption layout, Office tab paint/indicator measurement, hit targets, KeyTips/UIA, independent indicator motion | Tab label/indicator and caption geometry deferred. Device-pixel hairlines are raster rules. |
| Commands, splits and native editors | rows 26/45; split arrow 20/28, horizontal 20/44; combo button 24/32; native editor height font+8 / at least 42 | density/content/split/editor layout, renderer, editor manager and borrowed popup editors, keyboard/UIA | Fifth batch exposes editor label gap, text inset and ComboBox button width. Authored input widths and native font minima retained. Outer padding, command/split geometry, glyph shapes and system EDIT rendering remain deferred/platform-owned. |
| Ordinary command menus, nested menus, QAT overflow, Simplified More | rows 28/44; icon 24/32; leading/trailing 8; icon/text gap 10/12; desired width 170/220, described 280/320 | `RibbonMenuMetrics`, row/container/heading layout, popup sizing, shared renderer, overflow bands and inert preview; targets feed scrolling, keyboard/KeyTips/UIA and editor hosting | First/second sets expose row/horizontal/vertical roles. Third exposes container/heading/overflow bands; fourth exposes menu arrow slots and separator clearance. Popup inner margin, glyph shapes and platform work-area rules remain separate. |
| Application Menu | model navigation/pane dimensions and font/content minima; nav rows 40/44, pane 28/44; arrow 26/44 | component-specific metrics/layout, Application Menu renderer, native pane hosting and column scrolling, designer preview | Deferred. Accepted hover/default-pane/split ownership and every current default retained. Nested ordinary submenus use the selected ordinary-menu contract. |
| Illustrated menu choices and ComboBox choices | illustrated width 300, icon 36, row at least 64; ComboBox choice rows 28/44 or font+10 | `RibbonMenuLayout` illustrated path, editor layout/list window, designer popup preview | Fifth batch exposes ComboBox choice minimum and vertical text padding. Illustrated rows and choice-list horizontal padding remain distinct deferred roles. |
| Galleries | model tile sizes; density fallback 32x32 / 64x80, utility width 18/32 | gallery strip/popup layout, renderer, presenter viewport/scroll, borrowed UserControl cards, UIA | Tile padding, heading/utility slots deferred. Application artwork and card content stay host-owned. |
| QAT strip and message joins | QAT button 28/44, row 32/48; message minimum 34 (Touch floor), content padding (12,4,8,4) | QAT/message layout, renderer and joined body frame, native caption host, motion endpoints | Strip metrics and frame joins deferred. QAT overflow menu rows are selected; source identity/placement is unchanged. |
| Backstage | navigation width bounds/model opacity; font minimum 14; rows 42/52, Back button 36/44 | Backstage layout/renderer, live native page clipping/hosting, whole-surface motion, retained designer layer | Deferred. Host page layout/HWND ownership and moving caption coverage retained. |
| Frames, radii, shadows | `CornerRadius=4`, popup radius 6 (Office; neutral square), popup shadow enable/color; blur 6, reserved chrome 8, downward offset 2 | shared/Office renderer, popup chrome/regions, raster/cache keys, body/QAT/message joins and Backstage page clipping | Deferred together: painting, clipping, clearance and caches must change as one contract. One-device-pixel borders/coverage and Windows shadow policy stay platform rules. |
| ScreenTips, KeyTips and caption | ScreenTip width cap 320, padding 12, title gap 6, placement gap 8; KeyTip Office radius 2 / neutral square | ScreenTip layout/window, badge renderer, caption layout/Win32 hit testing | Deferred. Native system frame, scrollbars and host document content are not theme geometry. |

## First contract: ordinary command-menu rows and horizontal content

Use the existing immutable, per-ribbon `RibbonTheme`; add no global coordinator,
mutable setting owner, new renderer hook or public measurement type.

| Additive `int?` property | Null Compact / Touch | Meaning |
| --- | --- | --- |
| `MenuMinimumRowHeight` | 28 / 44 | Minimum standard menu row height, before font/icon minima and tallest-sibling normalization. Touch always retains at least 44. |
| `MenuIconSize` | 24 / 32 | Ordinary icon slot side. Check/radio glyphs keep their density size, capped to fit a smaller authored slot. |
| `MenuLeadingPadding` | 8 / 8 | Row-left to icon-slot-left distance. |
| `MenuTrailingPadding` | 8 / 8 | Text/arrow trailing clearance within the row. |
| `MenuIconTextGap` | 10 / 12 | Icon-slot-right to label-left distance. |

All are 96-DPI logical integers. Row/icon values accept 1..1024; padding/gap accept
0..1024; null resets each role. Assignment of an invalid theme throws before
changing the application theme or built-in selection. Existing properties,
minimum-height meanings, built-in override/reset semantics and public APIs remain.
Explicit values apply to both densities; null resolves at the current density.
Native font+10 and icon+two density clearances remain content minima. Descriptions
retain existing vertical spacing and two-line measurement. Mixed rows continue
using the tallest nonseparator row, including native editor targets.
The default Compact row commonly measures 32, because the 24-pixel icon plus two
4-pixel clearances exceeds the 28-pixel minimum. These meanings are unchanged.
QAT group-reference rows share row, icon, leading and gap roles; their existing
trailing chooser clearance remains independent of `MenuTrailingPadding`.

An internal `RibbonMenuMetrics` resolves theme+density in logical units. Existing
`RibbonLayoutEngine.Scale` rounds at the device boundary; summed logical insets
are scaled together to retain fractional-DPI default parity. Popup desired width,
row heights, icon/text slots and overflow group-reference painting consume that
resolver. Hit testing, native editor positioning, keyboard/KeyTips, UIA, scrolling,
popup placement/regions and popup-motion endpoints use the resulting target and
surface rectangles, not independent theme offsets. Work-area limits and native
scrollbars remain authoritative. No layout animation is introduced.

Theme/density/font/DPI changes keep existing invalidation and transient-popup
lifetime behavior. Reopening measures the new values; native editors/models are
retained. No radius, region, shadow or raster-cache dimension changes are selected.

## Designer and sample contract

Configuration is code-only through the existing hidden, nonserialized `Theme`.
XML comments and `PublicAPI.Unshipped.txt` describe the additive values. Built-in
theme selection and its one-transaction Undo/Redo, generated-code round trips,
detached editor drafts, inert popup/Backstage preview and authored handlers/bindings
are unchanged. There is no new theme-authoring UI or designer-owned component.

The permanent Designer Sample has an opt-in command-line demonstration that
assigns meaningful nondefault values using only public APIs and can reset them;
ordinary startup and the authored File presentation remain unchanged.

## Requested-change checklist and verification

- [x] Capture default measurements/paint before runtime edits, including both
  renderers, densities, fonts and synthetic 96/120/144/192 DPI; inspect all corners.
- [x] Implement five optional roles and shared resolver; preserve default paint
  and measurements; document explicit exclusions and reset behavior.
- [x] Cover invalid values, isolation, built-in/reset, density/font/DPI, row/paint/
  hit/editor/keyboard/UIA agreement, work-area scrolling and popup invalidation.
- [x] Build native Debug/Release with API analyzers; run focused native regressions.
- [x] Refresh the permanent sample in place; inspect package contents; verify an
  isolated package-only consumer in both renderers and relevant presentations.
- [x] Record actual evidence and limits; stop for user review before another
  geometry increment or additional themes.

No WPF build/test is evidence for this native slice. Prior broader Release failures
remain recorded in the Application Menu plan; unrelated group/Backstage fixes are
outside this contract. Actual VS, external accessibility, physical touch/IME/RTL
and mixed-monitor hardware acceptance remain separate from synthetic/fixture checks.

## Implementation and evidence - 2026-11-03

- Added only the five nullable `RibbonTheme` properties, with XML comments,
  assignment-time validation and Unshipped API entries. `RibbonMenuMetrics` owns
  ordinary-menu fallbacks; the independent ComboBox choice minimum remains in
  density metrics. Popup desired sizes/rows and both shared renderer paths use
  the resolver, including QAT group icon/text positioning. No renderer API,
  serialized model, control owner, region/shadow policy or WPF file changed.
- **32 pre-change default paint/measurement captures**: both renderers, both
  densities, 9/14-point fonts and synthetic 96/120/144/192 DPI. The post-change
  default captures match every pixel and row rectangle. Inspected all four frame
  corners in `artifacts/geometry/default-corners.png`; no approval/tolerance
  changes. Override slot tests cover 9/24-point fonts at the same synthetic DPIs,
  both renderers/densities, plus smaller check slots and QAT group-reference slots.
- Normal native **Debug and Release solution builds passed with zero warnings or
  errors**, including public API/XML analyzers. Normal Release pack passed;
  inspected `artifacts/geometry/release-package/contents.txt` and the package
  dependency declaration. This is a local package, not a published release.
- Focused Release `geometry-release.trx`: **184/184 passed**. Follow-up
  `geometry-overflow-designer-release.trx`: **19/19 passed**, including the new
  QAT group slot regression and existing group/designer/context-menu contracts.
  Eight cases overlap: **195 distinct Release cases**, not a full-suite result.
  Coverage includes live theme invalidation, exact null reset, per-ribbon and
  built-in isolation, rejected values, font/icon/Touch minima, hit targets,
  provider UIA bounds/from-point agreement, keyboard reveal, native editor HWND
  and value retention, constrained ordinary/overflow scrolling, density, popup
  motion, accepted Application Menu/state-command behavior and designer previews.
- Refreshed the permanent sample in place with `eng/Update-DesignerSample.ps1`,
  normal Debug package **`0.1.0-designer.local20261103T142934277Z`**. Authored
  Forms/UserControls/resources and selected startup File presentation are retained.
  The opt-in example is in `MenuGeometryDemo.cs`; launch the normal Debug sample
  with `--menu-geometry-demo`, choose **Wider menu spacing**, open **Try this
  menu**, then choose **Reset menu spacing** and reopen. No extra review app.
- **12/12 isolated public-package native scopes passed at actual 120 DPI**:
  Office/neutral x Solid/Mica/Acrylic x Compact/Touch. Each covers Classic and
  Simplified, nested menus, QAT/More, actual icon/text paint, row hit activation,
  KeyTips/keyboard invocation, work-area bounds, exact reset and retained editor
  HWND/text. Fixtures run sequentially, stop input on focus loss and use Subtle
  popup motion. Evidence: `artifacts/geometry/package-final/`, including package
  contents, isolated consumer location, 12 logs and 72 native captures. Inspected
  representative default/override, nested, overflow and material/Touch/More paint.
- The permanent sample's `--verify-menu-geometry-demo` passed its public actions,
  changed extent, exact reset and unchanged File presentation. Evidence:
  `artifacts/geometry/sample-final/`; inspected its actual override capture.
  This static sample fixture disables animation, following its existing static
  capture checks; the isolated native consumer above covers Subtle endpoints.
  Initial fixture failures were recorder/coordinate and unsettled startup-motion
  assumptions; they required fixture corrections, not runtime changes.

Review gate, **2026-11-03: user passed the appearance review of this first
ordinary-menu geometry set** ("review passed for the looks"). The checkbox
interaction correction below was also accepted on **2026-11-03** ("it works now");
the user authorized continuing to the next set. Other geometry remains deferred.
The reported **Keep formatting** menu-closure issue is corrected and verified in
the [ordinary-menu checkbox follow-up](STATE-COMMAND-POPUPS.md#ordinary-menu-checkbox-follow-up---2026-11-03);
its evidence is retained there. The permanent sample at this checkpoint uses local package
`0.1.0-designer.local20261103T145247154Z`.
The next bounded geometry increment follows below; no additional theme started.
Actual VS Properties/Undo/Redo/save-reopen, external accessibility clients,
physical touch/IME/RTL and genuine display/mixed-monitor DPI remain unrun here.
The previously recorded six broader Release failures remain open; this slice
neither reran the full native suite nor changed unrelated group/Backstage behavior.

## Second contract: ordinary-menu vertical spacing - 2026-11-03

Continue from the accepted first set and checkbox correction on the same branch
and HEAD, preserving all existing tracked/untracked work. This is one bounded
increment; stop for user review when it is implemented and verified.

| Additive `int?` theme property | Null Compact / Touch | Scope |
| --- | --- | --- |
| `MenuIconVerticalPadding` | 4 / 6 | Clearance above/below the ordinary icon slot when calculating its row minimum, including QAT group-reference/overflow rows. |
| `MenuDescriptionVerticalPadding` | 5 / 10 | Top/bottom inset of an ordinary menu row's title/description stack. Plain-text/native-editor internal padding stays separate. |
| `MenuDescriptionGap` | 2 / 4 | Gap between title and description in those rows. |
| `MenuSeparatorHeight` | 9 / 9 | Decorative horizontal separator row height in ordinary menus and QAT/More overflow. The line stays one device pixel and noninteractive. |

Padding/gap accept 0..1024; separator height accepts 1..1024. Values are 96-DPI
logical integers, resolved per ribbon using the existing immutable theme and
`RibbonMenuMetrics`, then rounded with the existing Scale helper. Null resets
each role to the current density fallback. Invalid assignments throw atomically.
Existing row-height meanings, font/icon minima, Touch's 44-pixel minimum,
tallest-sibling normalization and first-set authored values remain unchanged.
Explicit vertical padding reserves both individually scaled sides (and the
scaled icon slot), preventing a one-pixel shortfall at fractional DPI. Null keeps
legacy summed rounding for exact default/first-set parity. One shared metrics
helper owns each clearance calculation; paint uses the same scaled side value.

Measurement, title/description paint, row targets and separator placement consume
the same resolver. Resulting target/surface bounds feed hit testing, native editor
hosting, keyboard/KeyTips/UIA, work-area scrolling, popup regions/placement and
motion endpoints. Reuse existing theme/density/font/DPI invalidation and popup
lifetime behavior; retain native controls and checkbox keep-open behavior.

Illustrated choice artwork/title/description retains its independent metrics;
ordinary separator rows within such a menu still use the separator role.
Application Menu-specific columns/rows/separators, ComboBox choices, minimum menu
widths, description width limits, body/group/header/QAT strip metrics, radii,
borders, shadows and new themes remain deferred. No paint-only clipping/cache or
shadow change is selected.

Designer contract: code-only through the existing hidden/nonserialized Theme.
Add XML comments and Unshipped API entries; no new authoring UI, mutable owner,
serialization transaction or Undo/Redo semantics. The inert designer popup
renderer already uses shared row targets and receives the theme in paint.
Authored Forms/UserControls/resources, bindings, handlers and startup File
presentation remain. Add a separate vertical-spacing group to the existing
opt-in `--menu-geometry-demo`; preserve the first example and its reset scope.

### Second-set checklist and evidence

- [x] Capture pre-change defaults and accepted horizontal overrides at synthetic
  96/120/144/192 DPI, both densities/renderers and representative fonts.
- [x] Add the four roles; prove default and first-set parity, nondefault/reset,
  invalid values, density/font scaling and shared paint/row/separator bounds.
- [x] Verify editor ownership, checkbox retention, keyboard/UIA, constrained
  scrolling, popup motion and inert designer preview/serialization contracts.
- [x] Build native Debug/Release; pack/inspect and refresh the permanent sample;
  run isolated public-package native checks sequentially in both renderers and
  densities with relevant material presentations.
- [x] Inspect captures and all frame corners; record actual evidence and limits;
  stop for review before another geometry increment or themes.

### Second-set implementation and verification - 2026-11-03

- Added exactly the four nullable theme roles above, with XML comments, atomic
  validation and Unshipped API entries. `RibbonMenuMetrics` now resolves vertical
  clearances and separator height. Row measurement and shared title/description
  painting use those values. The separator painter continues centering its one
  device pixel in the measured row. No renderer API, designer authoring control,
  model ownership, serialized declaration, region/radius/shadow or WPF change.
- **64 pre-change captures** include both renderers/densities, 9/14-point fonts,
  synthetic 96/120/144/192 DPI, and default plus accepted first-set horizontal
  overrides. Every PNG hash and row rectangle matches after this increment.
  Evidence: `artifacts/geometry/vertical/baseline/` and `final/`. Inspected all
  four frame corners in the two `default-corners-font*.png` sheets; approvals and
  tolerances were not changed.
- A targeted explicit-padding test reproduced a **one-pixel description shortfall
  at synthetic 120 DPI**. Explicit sides now reserve their individually scaled
  values through the shared resolver; null preserves legacy summed rounding.
  The corrected case passes all four synthetic DPIs, both densities/renderers,
  and 9/24-point fonts, including complete native-measured two-line descriptions
  and icon clearance. Evidence: `menu-vertical-fractional-before.trx` / `after.trx`.
- Nine new focused cases passed in Debug. The final focused **Release selection
  passed 213/213** (`menu-vertical-release.trx`), including those nine plus first-set
  geometry, checkbox retention, group/overflow, menus/choices, density, popup
  layout/motion, accepted Application Menu and designer theme/menu/preview cases.
  This is a focused result and overlaps earlier evidence, not a full-suite claim.
  New coverage includes invalid values, isolated and vertical-only reset, native
  editor HWND/value retention, separator ink/noninteraction, keyboard skipping,
  checkbox state/focus, provider UIA bounds, constrained scrolling, independent
  illustrated/Application Menu metrics and inert designer preview without editors
  or transactions.
- Normal native **Debug and Release solution builds passed, zero warnings/errors**.
  Normal Release pack passed; inspected runtime/XML/designer entries and package
  dependencies in `artifacts/geometry/vertical/release-package/contents.txt`.
  Refreshed the permanent sample in place using `eng/Update-DesignerSample.ps1`
  with normal Debug outputs and local package
  **`0.1.0-designer.local20261103T150844512Z`**. Authored Forms/UserControls/resources,
  bindings/handlers and selected startup File presentation remain.
- **12/12 isolated public-package native scopes passed at actual 120 DPI**:
  Office/neutral x Solid/Mica/Acrylic x Compact/Touch, each covering Classic and
  Simplified. Actual title/description slots, separator height/noninteraction,
  described checkbox pointer/Space retention, native editor HWND/text, nested
  menu invocation, KeyTips, exact all-role reset, independent vertical reset,
  QAT/More and work-area bounds passed. Subtle popup motion remains enabled;
  foreground fixtures ran sequentially and guard input on focus loss.
  Evidence: `artifacts/geometry/vertical/package/`, including 12 logs, 120 native
  captures, isolated consumer path and package inventory. Inspected representative
  Office/neutral vertical override, separator and material/Touch captures.
- The first Touch fixture assumed a minimum-height override must increase a
  description-driven row that was already taller; its assertion now respects
  that existing minimum meaning. One neutral/Acrylic Space-key check failed
  before vertical overrides were applied and passed on its isolated retry;
  retained `space-first-failure.log`. No runtime input changes were made.
- The permanent sample's `--verify-menu-geometry-demo` passed both examples,
  measured overrides, exact reset and unchanged startup File presentation.
  Evidence: `artifacts/geometry/vertical/sample-retry/`, six captures and result
  log; inspected the vertical override. The first attempt stopped on the existing
  foreground-capture guard; the retry passed with an added menu-reachability
  assertion. This static sample fixture disables animation; the independent
  native consumer above covers Subtle endpoints.

### Second-set review gate

**2026-11-04: user accepted the vertical-spacing appearance** ("vertical spacing
looks okay i think"). This confirms appearance only. The review procedure was:
launch the normal Debug sample with
`--menu-geometry-demo`. In **Vertical menu spacing**, open **Try described menu**
at defaults; close it, choose **More vertical spacing**, and reopen. Review icon
clearance, title/description spacing, separator gap and Name field placement.
Toggle **Keep formatting** and verify the menu remains open. Then choose
**Reset vertical spacing** and reopen; first-set horizontal values should remain
if independently applied. The first example and its reset are still available.
The designer-authored Mouse/Touch selector is now available in the third QAT
entry and under **Home → Input mode** for live density review; its implementation
and checks are recorded in the [sample README](../samples/RibbonKit.WinForms.DesignerSample/README.md).

Stop before another geometry increment or additional theme. Width limits, other
components, radii and shadows remain deferred. Actual VS Properties/Undo/Redo/
save-reopen, external accessibility, physical touch/IME/RTL and genuine display/
mixed-monitor DPI were not run here. The recorded input/foreground retries limit
the native fixture evidence; this is not an exhaustive stability claim. The
previous six broader Release failures remain open, and the full suite was not
rerun. No commit, push or publication.

### Touch correction contract — 2026-11-04

Reported during review: Small Bold/Italic/Underline icons retain the Compact
left inset in their wider Touch slots; a narrow font-size ComboBox loses text
space because its arrow grows from 24 to 32 logical pixels while the field stays
the same width. The user also requested matching TextBox widths.

Current correction baseline: `codex/Ribbonkit-Winform`,
`11873710ef6e14968aa3953f4a055293c30e6199`, initially clean.

- Public API/designer: keep the existing `InputWidth` property and serialization.
  It remains the Compact field width in 96-DPI logical pixels, excluding the
  label. Following width review, Touch scales both ComboBox and TextBox widths
  by their field-height increase, using the current font/DPI and the surface's
  height minima. The arrow-width difference remains a minimum increase, so large
  fonts retain the Compact text budget. Equal authored widths use the same rule.
  Increasing `InputWidth` in Properties increases either field normally. Density
  changes do not mutate authored values, fonts, selections or model identities.
- Resolve height and width centrally in device pixels; round proportional widths
  upward and reserve both field insets to avoid clipping at fractional DPI.
  Classic columns/explicit rows, Simplified full ComboBoxes, ordinary-menu sizing
  and actual editor bounds consume this resolver; existing available-space clamps
  remain in force. Compact sizing retains its existing rounding.
- Center Small Touch icons in the actual command content bounds. Ordinary
  dropdowns reserve their existing arrow area; split buttons center in their
  primary half. Keep icon size, hit targets, menu gutters, Medium/Large and QAT
  geometry, Compact rendering and accepted menu behavior.
- Keep reusable behavior in the library and preserve the sample's authored
  fields/resources. Verify focused regressions and offscreen paint, then refresh
  the permanent Debug sample in place. User performs live/VS review; no computer
  use, automatic review-app launch, extra themes or subsequent slice.

Initial implementation and verification (the arrow-only width policy is
superseded by the proportional-width follow-up below):

- Shared Small-icon bounds now center Touch icons; the renderer reserves the
  ordinary dropdown's measured arrow region. `CommandDropDownArrow` is resolved
  through the same density metric in Classic/Simplified measurement and painting.
  Split content uses its actual primary half. Compact/menu placement is retained.
- Shared editor width resolution now feeds every authored-width measurement and
  actual field rectangle. Existing `InputWidth` XML/Properties descriptions and
  the README explain density behavior; there are no new public APIs or authored
  sample Form/resource changes.
- Five new Release regression cases reproduced both defects before the fix and
  passed afterward. They cover both renderers, Small toggles/dropdowns/splits,
  pressed/rest states, 96/120/144/192 DPI painting; Columns/TwoRows/ThreeRows and
  Simplified full fields, authored width changes, retained Compact sizing and
  ComboBox text budgets at 96/104/120/144/168/192 synthetic DPI.
- **116/116 related Release checks passed** for native editor alignment/lifetime,
  density, ComboBoxes/QAT, group/Simplified layout, renderer bounds, ordinary
  menus/checkbox retention and affected designer previews. After consolidating
  the unchanged arrow-width constants, **31/31 affected checks passed** (including
  the five new cases); these are overlapping focused runs, not a full-suite run.
- Normal Debug runtime/designer/client and permanent package-only sample builds
  passed with zero warnings/errors. The sample was refreshed in place with local
  package `0.1.0-designer.local20261103T232028591Z`.
- The extended existing `--verify-group-layout` public-package check passed at
  **actual 120 DPI** using an offscreen, non-activating Form: centered Small Touch
  icons, matching native field widths, unchanged `InputWidth`, retained ComboBox
  and TextBox HWNDs/values and ordinary group layout. No Showcase helpers/global
  input/computer use. Compact's existing one-pixel summed-padding clamp at this
  DPI is preserved; the check compares Touch against the measured Compact width.
- Evidence: `artifacts/touch-geometry-correction/` contains before/after/focused/
  final TRX files, renderer captures and `package-group/result.log`. Before/after
  Small-icon captures and the package Touch capture were inspected.

**Review gate:** reopen the permanent sample solution to load the refreshed
package. In **Home → Formatting**, compare Mouse and Touch: Small B/I/U icons
should be centered and the narrow font-size value should retain its text space.
Matching TextBox/ComboBox `InputWidth` values receive the same Touch increase;
Properties can increase either authored width. Review these two corrections
before another geometry increment or themes. Actual Visual Studio authoring,
physical touch/IME/RTL and mixed-monitor checks remain user-owned/unrun here;
previous broader-suite failures remain outside this correction.

### Proportional Touch field widths — 2026-11-04

The user found the initial width increase insufficient: preserving text space
alone still left fields much taller without a comparable width increase. The
revised contract above replaces the fixed extra allowance with the same growth
ratio used by the field's Compact/Touch heights. For example, 24 → 42 pixel
height yields 1.75× width (50 → 88 pixels after rounding). ComboBoxes and TextBoxes
share this rule; Simplified full ComboBoxes use their own 26/44 logical-pixel
height minima. The extra arrow width is still a floor when larger fonts reduce
the height ratio. Authored `InputWidth`, Compact sizing and Small-icon centering
remain intact. This is a correction within the pending review, not a new slice.

Verification:

- The three extended field-width cases failed against the arrow-only policy
  (50-pixel input: expected 88, actual 58) and passed with proportional sizing.
  Coverage includes 9/14-point fonts, authored widths 50/100/120, six synthetic
  DPI values, all three Classic group layouts and Simplified full ComboBoxes.
  Assertions cover equal field widths, unchanged authored values/Compact layout
  and at least the original ComboBox text budget. Touch measurement now reserves
  both scaled field insets; Compact's original rounding is retained.
- **62/62 focused Release checks passed** for the corrected geometry, native
  density/editor alignment/lifetime, group/Simplified layout, popup layout and
  ordinary menu geometry. After aligning Simplified's width ratio with its
  actual Compact minimum height, **12/12 affected checks passed**. These overlap;
  the full suite was not rerun.
- Normal Debug runtime/designer/client/package/sample builds passed without
  warnings or errors. Permanent sample refreshed in place with
  `0.1.0-designer.local20261103T232732724Z`; authored Forms/resources unchanged.
- The public-package offscreen group check passed at actual **120 DPI**, checking
  proportional matching native field widths, retained handles/values and centered
  Small icons. The old/new Touch captures were inspected. Evidence is in
  `artifacts/touch-width-proportional/` (before/focused/final TRX files and
  `package-group/result.log` plus captures).

**2026-11-04: user requested continuation after the wider Touch fields**, asking
to group subsequent geometry work into batches. This releases that review gate;
it does not establish additional VS/hardware acceptance.
Actual VS/native touch/IME/RTL/mixed-monitor acceptance and the previously noted
broader-suite failures remain outside these automated results. No subsequent
geometry increment, additional theme, commit, push or publication.

## Third batch: menu containers, headings and overflow

Baseline: `codex/Ribbonkit-Winform`, `70c2d47b2474ffb771447a8d44c9463062f386c1`,
initially clean. Group three connected areas into one reviewable batch, preserving
accepted row/vertical/Touch geometry and all unset defaults.

| Additive nullable `RibbonTheme` property | Null Compact / Touch | Meaning |
| --- | --- | --- |
| `MenuMinimumWidth` | 170 / 220 | Desired minimum ordinary-dropdown/QAT overflow/More width; labels, fields and available work area still govern actual size. |
| `MenuDescriptionMinimumWidth` | 280 / 320 | Additional desired minimum when ordinary command descriptions are present. |
| `MenuHeaderHorizontalPadding` | 6 / 6 | Text inset on each side of an ordinary dropdown heading. Shared gallery-heading hooks apply this only for menu headings. |
| `MenuHeaderVerticalPadding` | 6 / 8 | Clearance above/below heading text; font height remains the content minimum. |
| `MenuHeaderBottomGap` | 4 / 4 | Space between the heading band and first row. |
| `MenuOverflowScrollButtonHeight` | 16 / 28 | QAT/More overflow top/bottom button bands; Touch retains at least 28, and tiny work areas clamp bands to leave a viewport. |

Widths/band height accept 1..1024; padding/gap accept 0..1024. All are logical
96-DPI pixels. Null restores density defaults; explicit values apply to both
densities subject to stated floors/clamps. Assignment validation is atomic.
Keep the same immutable per-ribbon Theme contract, built-in/reset semantics and
Unshipped API bookkeeping. No mutable geometry owner or new renderer API.

Resolve values through `RibbonMenuMetrics`; share heading measurement/text bounds
between runtime painting and inert designer preview. Preserve default summed
rounding; explicit symmetric padding reserves separately scaled sides. Overflow
paint, hit rectangles, clips, row reveal, keyboard/UIA and scroll limits use the
same band/viewport geometry. Ordinary menus keep native/system scrollbar metrics;
the new band property applies only to QAT/More overflow.

Application Menu-specific geometry, native ComboBox choice lists, custom content,
gallery tiles/headings and illustrated-choice row metrics retain their owners.
The shared dropdown container/heading still surrounds illustrated choices; its
new container settings apply there without changing illustrated row metrics.
Arrow widths/glyphs, overall width caps/work-area safety gaps, inner popup margin,
body/header/QAT strip geometry, galleries, Backstage, radii/shadows and additional
themes are deferred to later coherent batches.

Designer/configuration contract: these remain code-authored theme values; Theme
stays hidden from generic VS property serialization. Add apply/reset examples to
the existing opt-in `--menu-geometry-demo` tab in the permanent sample, preserving
authored Forms/resources and startup behavior. Reset only this batch's six values.
Detached previews consume the same geometry without native editors, transactions
or application commands. User performs actual VS/live review; use no computer use
or foreground/global-input fixtures for this batch.

- [x] Capture default measurements/paint, then implement shared resolution.
- [x] Verify overrides/reset/invalid values, font/DPI/density, native editor and
  scroll reachability, inert preview and excluded surfaces.
- [x] Build native Debug/Release, inspect API/package artifacts and refresh the
  permanent sample. Check public-only package API/paint without Showcase helpers.
- [x] Record evidence/limits and stop before the next batch or additional themes.

### Third-batch implementation and evidence — 2026-11-04

- Added the six nullable properties with XML comments, atomic validation and 12
  Unshipped accessor entries. Existing theme selection/reset and all authored
  model values/identities remain intact. No WPF or authored sample designer/
  resource files changed.
- `RibbonMenuMetrics` resolves widths, header pads/gap and band height. Heading
  text rectangles are shared by both renderer paths; inert preview uses the same
  runtime surface. Overflow's existing viewport supplies paint/hit/clip/reveal
  geometry from one band height. Application Menu/gallery heading paths remain
  independent. Extreme heading padding clamps text width to zero safely.
- **40 before/after captures match every pixel and recorded rectangle**: 32
  heading/scroll paint scenes cover both renderers/densities, 9/14-point fonts and
  synthetic 96/120/144/192 DPI; eight runtime ordinary/QAT container scenes at the STA host DPI
  include headings, descriptions, a native field target and overflow clipping.
  Whole-frame captures including all four corners were inspected at representative
  120/144 DPI, along with the public package override render.
- **10/10 batch checks passed** (default parity plus nine override cases): resolved
  padding/measurement/paint, zero/large values, atomic rejection/reset/isolation,
  ordinary/QAT/More desired widths, font and editor-content minima, work-area
  clamps, scroll-band clips/row reveal/Touch floor/tiny areas, excluded heading
  geometry and detached designer preview without commands/editors/transactions.
- **159/159 related focused Release checks passed**, including accepted menu
  geometry/checkbox retention, overflow scrolling, Simplified, Touch fields and
  density, renderer boundaries, designer theme/menu preview and Application Menu.
  After moving compositing coverage out of the public helper, **2/2 affected
  heading cases passed** in opaque/composited contexts. These overlap earlier
  batch cases; this is not a full-suite run or hardware/material acceptance.
- Normal native solution **Debug and Release builds passed**, each with zero
  warnings/errors. The Debug package runtime/XML/design server/client/protocol
  contents and all six XML API entries were inspected. Permanent sample refreshed
  in place with `0.1.0-designer.local20261103T234733759Z`.
- The package-only `--verify-menu-container-geometry` check passed through public
  APIs: both renderers and Compact/Touch paint, six-property apply/reset/validation,
  and permanent demo actions including adding/removing only temporary QAT examples.
  It shows no Form, opens no popup and uses no Showcase helpers or global input.
  Its first build correctly exposed an internal-only compositing flag in the
  verification helper; that dependency was removed, with composited paint kept
  in STA coverage. No internal API was promoted to bypass that boundary.
- Evidence: `artifacts/geometry/menu-container/` contains baseline/parity/override/
  focused/composited TRX files, `before/`, `after/`, `overrides/`, public-package
  captures and `package/result.log`, plus inspected `package-contents.txt`.

### Third-batch review gate

Reopen the permanent Designer Sample solution, launch its normal Debug output
with `--menu-geometry-demo`, and use **Menu geometry → Menu size, heading and
overflow**. Open **Try headed menu** at defaults, apply **Roomier menu containers**
and reopen. Compare width, heading inset/height, the gap before the first row and
Name field/checkbox behavior. Enable **Overflow examples**, open QAT overflow,
then scroll/reveal rows in Mouse and Touch. Toggle the examples off to restore
the original QAT membership. **Reset menu containers** restores only these six
roles; earlier horizontal/vertical settings remain independently applied.

**2026-11-04: user passed the third-batch review, including combining the current
settings with the earlier implemented geometry tests** ("everything seems fine").
This records the reviewed combination; it does not expand the verification scope.

Actual VS Undo/Redo/save-reopen, external accessibility,
native foreground input, physical touch/IME/RTL, actual Mica/Acrylic hosts and
mixed-monitor/display-DPI checks were not run for this batch. The previous broader
suite failures remain outside these focused results. Arrow geometry, body/header/
QAT strip roles, galleries, Backstage, frame/radius/shadow geometry and additional
themes remain later work. No commit, push or publication.

### Menu editor alignment follow-up — 2026-11-04

The user reported that Wider menu spacing moves command content but leaves the
native TextBox/label at the popup edge, and explicitly included labelled and
hidden-label ComboBoxes in the correction.

Contract: use the ordinary menu's resolved command-text gutter and trailing
clearance for native editor content. Visible TextBox/ComboBox labels start at
the command text column, with their fields after the existing shared measured
label span. `ShowLabel=false` ComboBoxes start the field at that text column and
reserve no label span; their hidden command name remains intact and does not
inflate menu measurement. Keep `InputWidth`, field height/Touch scaling and native
identity/value behavior. This is alignment with fixed authored widths, not a new
fill-width mode or public API/designer setting. Application Menu-specific fields,
strip/Simplified fields, group flyouts, custom content and choice lists retain
their own layout. Ordinary nested dropdowns follow the corrected menu contract.

Implementation/evidence:

- Shared `RibbonEditorLayout` menu insets/content bounds now feed actual native
  presentation, painting and inert preview. Desired menu width reserves the same
  gutter, visible label, exact authored field and trailing clearance. Menu
  containers with fields can intentionally grow; command-only defaults and all
  existing public settings remain unchanged. Full-row hit/clip/UIA targets stay
  intact while native field bounds move within them.
- Two renderer regression cases reproduced the screenshot's mismatch in both
  densities, then passed after the fix. Before/after previews were retained and
  inspected. Fractional-DPI measurement coverage includes default/wider/minimal
  gutters, 9/14-point fonts and synthetic 96/120/144/192 DPI, shown/hidden labels,
  exact field widths and non-menu exclusions.
- **155/155 focused Release checks passed**, including those three alignment
  cases and related menu/container/ComboBox/native alignment/density/Touch/
  Simplified/designer/Application Menu scopes. A subsequent **1/1 live native STA
  popup check passed at its reported 96 DPI**: actual native presenter label/field
  columns, retained TextBox and both ComboBox handles/values/authored widths
  across default/wider themes and both densities. Its capture was inspected.
  These are focused results; the full suite and physical display changes were
  not run.
- Native solution Release build and Debug runtime/design-server/client plus
  permanent sample builds passed with zero warnings/errors. The refreshed local
  package is `0.1.0-designer.local20261104T001328624Z`; packaged runtime and design
  server hashes match the current Debug builds. The public-only package API/demo
  check passed, including labelled/hidden-label example declarations. It does not
  claim native package popup or actual VS acceptance.
- Evidence: `artifacts/geometry/menu-editor-alignment/` contains before/after/
  focused/native TRX files, before/after preview captures, native captures and
  `package/result.log`. No authored Form/resource or WPF file was changed; no
  computer-use tools, global input, commit, push or publication.

Review: reopen the permanent sample, run `--menu-geometry-demo`, apply **Wider
menu spacing**, then open **Try this menu**. Compare **Name**, **Font family** and
the unlabelled chooser with the command text column in Mouse/Touch; confirm the
first two fields line up after their labels and the unlabelled field begins at
the text column. Reset and compare default spacing too. Stop for this correction's
review before further geometry batches or themes. Prior third-batch acceptance
and the previously documented hardware/VS/material/full-suite limits remain scoped
to their recorded evidence.

### Optional menu editor fill width — 2026-11-04

The user approved the optional menu-only sizing mode discussed after alignment.
Add `FillMenuWidth`, a Boolean CLR property defaulting to false, to
`RibbonTextBoxItem` and `RibbonComboBoxItem`. True fills the available ordinary
dropdown row after the measured shared label column and trailing clearance.
`ShowLabel=false` ComboBoxes fill from the command-text column. Keep label/font/
height/Touch rules and authored `InputWidth`; it remains the preferred minimum
when the menu is measured, with existing screen-space/scroll/clipping limits.
False preserves the accepted fixed-width geometry. Strip/Simplified fields,
group flyouts, Application Menu-specific fields and native choice lists ignore
the fill setting. Ordinary nested/QAT-source dropdowns use the menu contract.

Designer contract: expose the property under Layout, reset to false, emit a
normal Boolean assignment only when true. Include it in detached drafts and
one-transaction Apply/Undo/Redo, preserving component/binding/event identities.
Customization copies inherit the source's flag through existing synchronization;
no new customization persistence schema. Add XML docs and four Unshipped accessor
entries. No enum, global sizing coordinator or broad designer UI.

Layout/native/preview contract: the existing shared content/field rectangles
resolve both fixed and fill modes. Menu measurement still reserves `InputWidth`;
only the final field width consumes the remaining row. Height and full-row hit/
clip/UIA targets remain intact. Changing the layout flag follows normal model
invalidation and closes a stale popup; reopening retains native HWNDs and values.

The opt-in geometry demo explicitly enables filling for its menu editors, with
**Fill menu fields** toggling all those examples back to fixed sizing for
comparison. Normal application/default model behavior stays fixed; authored
Form/resource files and startup presentation remain untouched. Review combines
Wider menu spacing and Roomier menu containers in Mouse/Touch. Finish the bounded
checks, refresh the permanent sample and stop for review; no additional theme.

Implementation and verification:

- Added the two documented CLR properties, default/reset metadata and four
  Unshipped accessor entries. The shared field resolver fills only when the
  ordinary-menu presentation and model flag both request it. Menu measurement
  still uses the authored preferred minimum. Native paint/placement and inert
  preview consume the same rectangle; presentation invalidation includes the flag.
- Added the flag to designer scalar drafts and customization-source copying.
  Standard CodeDOM emits a Boolean literal, and framework component serialization
  demonstrated one-transaction Apply, Undo and Redo without replacing the models.
- **7/7 final focused fill checks passed**: both renderer previews and both
  densities, changing menu widths, shown/hidden labels, exact trailing edges,
  unchanged left/height/value/width properties, fonts 9/14 at synthetic
  96/120/144/192 DPI, constrained/non-menu layouts, native HWND/value retention,
  designer draft/default/reset/serialization/Undo/Redo and customization copies.
  The earlier four fixed-width alignment regressions also passed with default
  `FillMenuWidth=false`.
- The larger related Release run recorded **187 passes / 1 failure out of 188**.
  Six of those passes overlap the new fill cases; the remaining **181 related
  checks passed**. The failed native fixture found its popup closed after message
  pumping even at fixed/default mode. The final fixture inspects synchronous
  native layout/retention before processing external activation messages, then
  closes/drains its own popup between stages; all seven fill cases passed after
  that test-only correction. This does not establish native focus/input acceptance
  or a single clean 188-case run. Do not rerun those passed related scopes merely
  to replace the aggregate result.
- Native solution Release build and Debug runtime/design-server/client/sample
  builds passed with zero warnings/errors. The permanent sample was refreshed
  in place using `0.1.0-designer.local20261104T002943794Z`. Packaged runtime/design
  server hashes match the Debug builds, and XML contains both new public APIs.
- The public-only package check passed model Layout metadata/default/set/reset
  for both editor types and the permanent demo's fill/fixed toggle. Existing
  renderer/density/theme/QAT demo checks also passed. No Form is shown and no
  Showcase helper/global input/computer-use tool is used by that package check.
- Fixed/fill preview and native captures were inspected, including the filled
  TextBox, labelled ComboBox and hidden-label ComboBox sharing one trailing edge.
  Evidence: `artifacts/geometry/menu-editor-fill/`, including the initial and
  focused results, `menu-fill-final.trx`, `after/` captures and `package/result.log`.

**Review gate (passed 2026-11-04):** the user confirmed "everything here is good
now" and requested the next batches, covering the reviewed editor alignment/fill
work. This records that review, not additional VS/hardware acceptance. The review
procedure was: reopen the permanent sample and run `--menu-geometry-demo`.
**Fill menu fields** starts enabled. Combine **Wider menu spacing** and **Roomier
menu containers**, then open **Try this menu**. Verify Name/Font family keep their
label column, the unlabelled chooser fills from the command-text column, and all
fields share the trailing edge without becoming taller. Toggle **Fill menu
fields** off/on to compare fixed/fill modes in Mouse and Touch. The same toggle
also applies to the vertical and headed-menu editor examples. Resetting theme
geometry does not reset this independent model flag.

Stop for review before another batch or theme. Actual VS save/reopen and target
machine input/IME/RTL/material/mixed-monitor checks remain separate; previous
broader-suite failures remain outside this slice. Authored Forms/resources and
WPF files are preserved. No commit, push or publication.

## Fourth batch: menu arrow slots and separator clearance

Baseline remains `codex/Ribbonkit-Winform`,
`70c2d47b2474ffb771447a8d44c9463062f386c1`, with all preceding worktree changes
preserved. Complete another bounded group of connected menu-row roles.

| Additive nullable `RibbonTheme` property | Null Compact / Touch | Meaning |
| --- | --- | --- |
| `MenuSubmenuArrowWidth` | 16 / 28 | Reserved text/indicator width for ordinary non-split submenu rows. |
| `MenuSplitArrowWidth` | 20 / 28 | Desired ordinary-menu and QAT/More split secondary region width; existing one-third narrow-row cap and Touch minimum 28 remain. |
| `MenuChooserArrowWidth` | 16 / 16 | Reserved caption/indicator width for QAT/More menu, ComboBox and group references. |
| `MenuSeparatorHorizontalPadding` | 8 / 8 | Inset at each end of ordinary/QAT/More decorative separator lines. |

Arrow widths accept 1..1024; separator padding accepts 0..1024, all in logical
96-DPI pixels. Null resets only that role. Positive desired widths remain subject
to content/row/work-area clamps. Preserve exact default summed rounding and
current glyph shapes, strokes, colors and one-device-pixel separator thickness.
Validation is atomic; keep immutable per-ribbon Theme, built-in/reset behavior,
XML documentation and Unshipped accessor bookkeeping.

One resolver supplies menu measurement/description budgets, text/arrow paint slots
and split hit/keyboard/KeyTip/UIA rectangles. QAT/More chooser budgets use their
own arrow role; ordinary submenu width must not accidentally resize QAT chooser
slots. Recompute description rows using the corresponding reserved width. Inert
previews share the same split regions; explicit submenu widths also update their
arrow-opening hit rectangle while null retains the existing preview click map.

Exclude ribbon/Simplified strip and group-flyout arrows, QAT strip buttons,
Application Menu-specific layout and separator geometry, native choice lists,
field sizing/labels, glyph shapes, popup margin/frames/radii/shadows and themes.
Ordinary nested dropdowns remain in scope, including ones opened from QAT.

Designer contract remains code-authored Theme values (hidden from generic VS
serialization), with no new theme editor. Extend the existing opt-in permanent
sample with apply/reset and actual submenu/split/chooser/separator examples. Reset
only these four theme values, preserving prior geometry and editor fill flags.
Do not change authored Forms/resources or startup presentation. Verify defaults,
overrides, narrow clamps, hit/paint/keyboard/UIA agreement and inert preview; refresh
the native Debug package and stop for review. User owns actual VS/hardware/input
review; use no computer-use tools or global-input fixtures.

### Fourth-batch implementation and evidence — 2026-11-04

- Added four documented/validated nullable properties and eight Unshipped accessor
  entries. `RibbonMenuMetrics` owns separate submenu, split and chooser roles.
  Ordinary and QAT/More width/description budgets select the corresponding role;
  changing submenu width does not alter chooser budgeting, and vice versa.
- Ordinary/QAT split regions flow through `RibbonSplitLayout` into paint, hit,
  keyboard/KeyTip and existing UIA command bounds. Narrow rows retain the one-third
  cap. The existing UIA command node follows the primary target; no new UIA node
  or whole-row bounding policy was introduced. An initial test incorrectly assumed
  a union bound; corrected expectations now check the existing primary policy.
- Explicit submenu widths update inert preview arrow-opening hit rectangles;
  null retains the prior click map. Menu separator line ends resolve the new pad,
  while Application Menu-specific separators retain 8 and group lines retain
  their own implementation. Glyph shapes/strokes and field fill are unchanged.
- **32 default paint/measurement captures match exactly**, covering both renderers,
  both densities, 9/14-point fonts and synthetic 96/120/144/192 DPI. Scenes include
  ordinary submenu/split, QAT menu/ComboBox/group choosers, QAT split and separator.
  Default and package override images were inspected, including all four frame
  corners at representative 120/144 DPI; no approvals/tolerances were changed.
- **8 batch cases passed** across focused runs (one default-parity case and seven
  override cases): role-specific paint/text slots, narrow and Touch split clamps,
  ordinary/QAT/More hit/keyboard/UIA geometry, separator endpoints/exclusions,
  invalid/reset/isolation/built-in behavior, description budgets and inert preview
  hit maps. The related run recorded **154 passes / 1 failure out of 155**, with
  seven new cases included among its passes. Its failure was the older native
  alignment fixture losing its popup after message pumping. Applying the same
  synchronous-layout-before-external-activation scope used by the fill fixture
  yielded **1/1 targeted pass**. Passed scopes were retained, not rerun to replace
  the aggregate. This is not a single clean full-suite or native input result.
- Native solution Release and Debug runtime/design-server/client/permanent-sample
  builds passed without warnings/errors. Sample refreshed in place with
  `0.1.0-designer.local20261104T005024662Z`. Packaged runtime/design server hashes
  and all four new XML API entries were verified.
- Public-only package check passed both renderers/densities, container/arrow
  reset, submenu/chooser/split paint slots, fill metadata and all demo actions.
  The demo adds/removes 28 temporary QAT entries (24 commands, four references)
  without changing authored QAT identities/order. It shows no Form, opens no
  popup and uses no Showcase helpers or internal layout APIs; actual runtime hit/
  keyboard/UIA geometry is separate STA evidence. Authored Forms/resources and
  startup presentation remain intact.
- Evidence: `artifacts/geometry/menu-arrows/` contains before/default-parity/
  override/hit-preview/focused/native-layout TRX files, `before/`, `after/`, package
  captures and `package/result.log`.

### Fourth-batch review gate

Reopen the permanent sample and launch `--menu-geometry-demo`. In **Menu arrows
and separators**, compare **Try arrow menu** before/after **Wider arrow areas**.
Export should open its nested menu; Paste's primary should toggle and its arrow
should open choices, with clearly separated hover areas. Check separator ends.
Enable **Overflow examples** in the container group, open QAT overflow and scroll
to the last four reference entries (Export/Paste/Font family/ordinary-menu group).
Compare Mouse/Touch, then **Reset arrow areas**. Earlier row/container/heading
overrides and editor fill flags should remain independently applied. Turning
Overflow examples off must preserve the original QAT entries/order.

Stop for review before another batch or theme. Actual VS save/reopen/Undo/Redo,
native input/physical touch/IME/RTL, actual material hosts and mixed-monitor DPI
remain separate. The previous broader-suite failures are unchanged. Body/header/
QAT strip, galleries, Backstage, glyph/radius/shadow/frame and popup inner-margin
roles remain later work. No WPF changes, computer-use tools, global input, commit,
push or publication.

## Fifth batch: native editor content and ComboBox choice rows — 2026-11-04

The user accepted the shared arrow-column appearance and requested the next
batches. Implementation starts from clean `codex/Ribbonkit-Winform`,
`9800f76a195df7bc626ced0442d90a4479b33fce`. The following contract was presented
before runtime edits. No WPF files, authored Forms/resources, or previous theme
settings are selected for change.

### API, layout and designer contract

Five additive nullable `RibbonTheme` integer properties share an internal
`RibbonEditorMetrics` resolver. Values are 96-DPI logical pixels, resolved per
ribbon/density. Null resets each role independently to its existing default.

| Property | Null Compact / Touch | Meaning and limits |
| --- | --- | --- |
| `EditorLabelGap` | 6 / 6 | Label reservation and paint clearance, 0..1024. Hidden ComboBox labels reserve none; existing outer field inset remains. |
| `EditorTextInset` | 4 / 4 | Horizontal field text clearance on both sides, 0..1024. Editable and selection-only ComboBox text use the same slot as inert preview. Authored field width/height remain. |
| `ComboBoxButtonWidth` | 24 / 32 | Painted field button and native text exclusion, 1..1024. Touch keeps at least 32. The slot clamps to the actual field width. |
| `ComboBoxMinimumChoiceHeight` | 28 / 44 | Native choice-row minimum, 1..1024, subject to font/padding and Touch's 44 floor. |
| `ComboBoxChoiceVerticalPadding` | 5 / 5 | Padding above/below choice text, 0..1024. Null retains scaled summed 10; explicit values reserve separately rounded sides. |

The resolved choice height is capped at the native ListBox limit of **255 device
pixels**, including at synthetic high DPI or extreme settings. Field/choice
measurement, label painting, ComboBox button painting, native EDIT/text clipping,
choice ListBox row height and popup sizing consume the shared values. Text slots
clamp to empty inside narrow fields rather than escaping past the field/button.
Touch width scaling still uses the accepted font/DPI-aware height ratio and an
arrow-growth floor shared by TextBox and ComboBox. A wider button does not change
authored `InputWidth` or `FillMenuWidth`. Height scaling, menu label gutters,
shared label columns, matching field widths and fill behavior remain intact.

Classic, Simplified, group/menu/Application Menu editor fields inherit these
editor roles. Small command projections and QAT/More chooser arrows keep their
accepted command/menu geometry. The options ComboBox uses the same painted button
width while retaining its separate dialog text inset. Choice-list horizontal
padding, illustrated rows, outer editor padding, native selection/caret/IME and
system EDIT paint remain separate. The following review extension adds field-height roles.

Theme assignment validates all five roles before changing Theme/BuiltInTheme.
Existing theme/density/font/DPI invalidation closes stale lists/popups; reopen
measures current values. Existing editor models and HWNDs remain retained.
Hit targets, keyboard focus and UIA continue using the measured row/field targets;
no new event, coordinator, motion or public measurement hook is added.

The existing hidden/nonserialized Theme remains code-authored. XML comments and
ten Unshipped accessors document the API; shipped API is untouched. Model
serialization, detached drafts, designer Undo/Redo and authored handlers/bindings
are unchanged. Inert popup and ribbon previews use the same text/button slots;
they do not create native editors or invoke application commands. The permanent
sample's opt-in demo adds a fifth group with apply/reset and editable/selection-
only, labelled/hidden-label examples, without editing InitializeComponent.

### Review extension: taller fields in Roomier editors — 2026-11-04

The user said the initial editor spacing looked good, then requested that the
Roomier option also increase editor height. This extends the fifth batch; it is
not a new geometry batch or theme. The native API/layout/designer contract was
presented before implementation.

| Additional nullable `RibbonTheme` property | Unset behavior | Explicit behavior |
| --- | --- | --- |
| `EditorMinimumHeight` | Existing density/font minima: font+8 in Compact; Touch at least 42; Simplified ComboBox at least 26/44. | 1..1024 logical pixels; font/padding and density floors still apply. |
| `EditorVerticalPadding` | 4 logical pixels per side, preserving scaled summed 8. | 0..1024 logical pixels per side, scaled separately. |

Both TextBox and ComboBox fields share the resolved height. Native text and
painted labels remain vertically centered, with unchanged font size. Actual
field height still clamps to its available slot; command/menu measurement now
reserves the requested height first. Ordinary mixed menu rows normalize to the
tallest sibling. Classic editor-containing command rows and group flyouts grow;
the classic ribbon body reserves their three-row band. Simplified inline field
candidates reserve their taller body band, including while their group is
borrowed, so ownership changes retain the band height. Menu-only fields and Small
projections do not enlarge the ribbon body. Application Menu editor rows/footer
grow within their existing work-area/scroll constraints; other File geometry
and accepted behavior remain.

The existing font/DPI/density ratio continues to size Touch field widths.
Explicit height settings do not rescale authored widths or narrow Touch fields.
FillMenuWidth, shared label columns, hidden-label behavior, row hit/keyboard/UIA
bounds, editor identity/HWNDs and transient-popup invalidation are retained.
Tab selection updates the containing ribbon height and clears group measurements
when height overrides are present, avoiding stale bands measured from a different
tab or borrowed QAT group. Unset height settings retain the prior path.

Configuration stays code-only through the hidden Theme, with XML documentation,
validation before assignment and four additive Unshipped accessors. No model
property, broad designer UI, serialization/Undo/Redo contract, control replacement
or animation is added. Native presenters and inert previews use the same field
height helper. The existing Roomier preset sets **54 minimum / 10 vertical
padding**, together with its previous five roles; Reset clears all seven. The
permanent sample's authored InitializeComponent/resources remain untouched.

Verification:

- The initial focused run passed **12/12**, including saved default-pixel/
  measurement parity and the new native-height cases. Two-renderer menu checks
  exercise both densities, fill/fixed widths, visible/hidden labels and retained
  HWND/value/UIA bounds. Synthetic 96/120/144/192 DPI and 9/14-point fonts cover
  padding/minimum/floor/reset and extreme values without changing widths.
- Related checks recorded **185 passes / 1 failure out of 186**. The minimized
  group-motion case failed again in isolation (**1 pass / 1 failure out of 2**).
  Its same failure, "The group stays open after motion", is recorded in the
  **2026-11-03 pre-Application-Menu baseline** at
  `artifacts/application-menu/baseline/pre-application-menu-order-sensitive.trx`.
  It remains a pre-existing motion/lifetime gate, not a clean aggregate pass.
- After the tab-switch and Application Menu refinements, targeted checks passed
  **58/58**. Strengthened fractional-padding/off-tab-group/work-area cases passed
  **2/2**, tab/cache/native-centering cases **4/4**, and a 30-field scrolling menu
  reveal/retained-native-editor case **1/1**. Passed scopes were retained.
- Native Release solution build passed with zero warnings/errors.
- Debug runtime/design server/client/sample builds passed with zero warnings/
  errors. Refreshed the same sample with package
  `0.1.0-designer.local20261104T051922952Z`; runtime/design-server package hashes
  match current Debug outputs. Authored Forms/resources remain unchanged.
- Public-package verification passed all seven roles/reset/demo actions, both
  renderers/densities and synthetic 96/120/144/192-DPI paint. A public-only check
  also passed Classic/Simplified ribbon height growth and exact reset in both
  densities, without showing windows or using internals. Representative Office/
  neutral package frames were inspected at fractional DPI, including all corners.
- Actual VS, native input/IME, material and mixed-monitor hardware acceptance
  remain separate. No computer use, global input, WPF edits, commit, push or
  publication; no clean full-suite pass is claimed.

Evidence: `artifacts/geometry/editor-height/`, including `package/result.log`,
public-package/native-DPI menu/inline captures and focused/related/isolated/tab/
surface/cache/scroll TRX files. Inert menu
captures show the painted/native text contract; DrawToBitmap inline captures can
include Windows' unused ComboBox chrome and are not onscreen input acceptance.
Review **Roomier editors → Try editor menu** in Mouse/Touch, then compare the
authored Formatting fields and reset. Fields and rows should become taller,
labels/text should remain centered, and widths/values should remain stable.
**Current review result is recorded below; await instruction before another
geometry batch or theme.**

### Remaining review batches

This is a **working grouping, not a pre-existing fixed slice count**. About
**six further review batches** remain, grouped as follows:

1. Command/split content, glyph slots and special/illustrated menu geometry.
2. Ribbon body/groups/header/tabs and QAT/message spacing/joins.
3. Gallery tiles, headings and utility slots.
4. Backstage navigation and page-container geometry.
5. ScreenTips, KeyTips and caption geometry.
6. Frames/radii/shadows and matching popup inner clearance/clipping/cache keys.

Inventory/review may split a broad group or leave platform-owned rules unchanged.
These groups are a roadmap, not authorization to implement all of them in one
turn. Additional built-in themes still follow consolidation.

### Verification and current review gate

- [x] Saved 32 before/after default measurement/paint pairs across both renderers,
  densities, 9/14-point fonts and synthetic 96/120/144/192 DPI. Measurement files
  and every captured default pixel match. These synthetic paint fixtures are
  separate from real device-DC/hardware rendering.
- [x] Five-role overrides/reset/invalid assignment, Touch floors, fractional-DPI
  padding and native row cap; field/button/text/label agreement, fill/fixed widths,
  narrow/empty slots and TextBox/ComboBox width parity covered.
- [x] Native STA editor/preview cases cover default/override/extreme/reset,
  retained HWND/value, native text clipping, UIA row bounds, actual ListBox row
  height and keyboard selection. No global input is injected.
- [x] First new scope **12/12 passed**. Related scope **69 passed / 1 failed out
  of 70**; the customization context-menu lifetime case passed **1/1 isolated**
  without a code change. This is an aggregate fixture/lifetime limitation, not
  a clean aggregate pass. Additional layout/designer scope **94/94 passed**,
  including the strengthened native/preview cases. Passed scopes were retained.
  A final targeted **4/4** covered Classic/Simplified inline measurement and
  retained native handles in both densities with apply/reset.
- [x] Native Release solution build passed with zero warnings/errors.
- [x] Debug runtime/design server/client/sample builds passed with zero warnings/
  errors. Refreshed the same permanent sample with package
  `0.1.0-designer.local20261104T045532630Z`; runtime/design-server package hashes
  match current Debug assemblies. No authored Form/resource or WPF file changed.
- [x] Public-package API/reset/validation and label/button/hidden-label paint
  passed both renderers/densities at four synthetic DPIs (16 presentations),
  plus permanent demo apply/reset and retained previous geometry/Fill/File/QAT
  contracts. No window/popup/internal/Showcase helper is used by this check.
  Inspected representative Office/neutral package scenes, all four frame corners,
  and native-DPI default/override inert previews. These are separate from actual
  hardware/material acceptance.
- [x] **2026-11-04: user accepted the editor spacing and taller-field appearance**
  ("looks good now"). This confirms appearance only. Actual VS designer, physical
  touch/IME, external accessibility and mixed-monitor/material hardware gates
  remain separate; no full-suite pass is claimed.

Evidence is under `artifacts/geometry/editors/`: `before/`, `after/`, `previews/`
and the baseline/new/related/isolated/layout-designer/inline TRX files, plus
`package/result.log` and public-package captures. Review normal
Debug sample with `--menu-geometry-demo`: **Native editor spacing**, then compare
**Roomier editors** and **Reset editors** using **Try editor menu** in Mouse/Touch.
Open both labelled editable and unlabelled selection-only font choices. Combine
with prior menu spacing and fill toggles, and compare the authored Formatting
fields. Captions should remain aligned, text should clear the wider button,
choice rows should grow, and values should survive reset. **Await instruction
before implementing another batch or theme.**

### Shared menu arrow column correction — 2026-11-04

The user reported nonuniform arrow positions in the QAT overflow capture and
approved a shared trailing column using the split button's position as reference.

Contract/implementation: add no public setting and preserve authored theme values.
The resolved split secondary width, with its existing narrow-row cap, defines
`RibbonMenuLayout.ArrowColumn`. Ordinary submenu and QAT/More menu/ComboBox/group
indicators use this column. Existing submenu/chooser widths remain minimum caption
reservations; each reserves at least the shared column. Larger authored values
can add text space without moving glyphs. Common text bounds/trailing padding
align caption ends, including projected ComboBox and group captions. Native
editor fields and glyph shapes are untouched.

Split measurement and QAT split expansion use the same column helper; existing
nonzero-row hit areas/primary/secondary actions remain unchanged. Zero-width rows
now safely return an empty indicator region. Inert preview arrow-opening hit areas
also use the shared column in both default and overridden themes. Menu description
measurement consumes actual device-pixel reservations. Application Menu-specific,
strip/group-flyout and field/fill geometry retain their owners. Default menu arrow
positions and some caption clearances intentionally change for this correction;
the previous batch's default-paint parity is historical, not a current pixel gate.

Evidence:

- Two new renderer regression cases reproduced the mismatch before the change,
  then passed. Coverage includes default/mixed role widths, combined leading/
  trailing/icon overrides, both densities, 9/14-point fonts and synthetic
  96/120/144/192 DPI. A separate narrow/zero/larger-reservation case passed.
- **32 before/after split-region measurement files remain identical.** Arrow/
  caption pixel changes are intentional. Before/after renderer scenes and the
  public-package override render were inspected, including representative frame
  corners. The submenu/split/menu-reference/ComboBox-reference/group-reference
  centers align, with no split target-width change.
- The related focused Release run recorded **161 passes / 1 failure out of 162**.
  Its failure was the old QAT group-caption assertion that required separate
  chooser clearance. Updated it to the approved common column/trailing rule;
  **1/1 targeted check passed** at all four synthetic DPIs/both densities/renderers.
  Passed scopes were retained, not rerun. This is not a single clean full-suite run.
- Release native solution and Debug runtime/design server/client/sample builds
  passed with zero warnings/errors. Permanent sample refreshed in place with
  `0.1.0-designer.local20261104T012835997Z`; runtime/design-server package hashes
  match current Debug outputs. No authored Form/resource or WPF file changed.
- The public-only package check passed shared submenu/chooser/split column
  positions through both renderers/densities and retained demo/reset/fill/QAT
  behavior. It shows no windows, opens no popup and uses no internal layout APIs.
  Actual native input/focus/VS/hardware/material gates remain separate.
- Evidence: `artifacts/geometry/menu-arrow-column/`, with before/regression/related/
  targeted TRX files, `before/`, `after/` and `package/result.log`/captures.

**2026-11-04: user accepted the shared arrow-column appearance** ("looks good
now"). This confirms appearance only; existing verification limits remain.

Review procedure: use the refreshed normal Debug sample with `--menu-geometry-demo`. Compare
**Try arrow menu** and the final four **Overflow examples** references at defaults
and with **Wider arrow areas**, in Mouse/Touch and with prior spacing overrides.
All chevrons should share a trailing column; captions should keep consistent
clearance. Paste's primary/arrow boundary and actions should be unchanged. Await
instruction before any further batch or theme. Prior live/input/
hardware/full-suite limits still apply; no computer use, global input, commit,
push or publication.
