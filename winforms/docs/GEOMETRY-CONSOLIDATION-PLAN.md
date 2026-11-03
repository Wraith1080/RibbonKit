# Native geometry consolidation

Status, **2026-11-04: second ordinary-menu geometry set appearance accepted;
bounded Touch Small-icon and editor-width corrections verified, awaiting review. Themes and
further geometry increments remain deferred**.
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
| Commands, splits and native editors | rows 26/45; split arrow 20/28, horizontal 20/44; combo button 24/32; native editor height font+8 / at least 42 | density/content/split/editor layout, renderer, editor manager and borrowed popup editors, keyboard/UIA | Authored input widths and native font minima retained. Editor padding, arrow/check/radio glyphs and system EDIT rendering deferred. |
| Ordinary command menus, nested menus, QAT overflow, Simplified More | rows 28/44; icon 24/32; leading 8; trailing 8; icon/text gap 10/12. No existing public override | density metrics, `RibbonMenuLayout`, popup sizing, shared renderer and QAT group-reference paint; targets feed scrolling, keyboard/KeyTips/UIA and editor hosting | **Selected first set:** five nullable theme overrides below, one shared resolver. Description vertical padding 5/10, gap 2/4, icon vertical clearance 4/6, separator 9, minimum width 170/220 and description width 280/320 remain internal density defaults. |
| Application Menu | model navigation/pane dimensions and font/content minima; nav rows 40/44, pane 28/44; arrow 26/44 | component-specific metrics/layout, Application Menu renderer, native pane hosting and column scrolling, designer preview | Deferred. Accepted hover/default-pane/split ownership and every current default retained. Nested ordinary submenus use the selected ordinary-menu contract. |
| Illustrated menu choices and ComboBox choices | illustrated width 300, icon 36, row at least 64; ComboBox choice rows 28/44 or font+10 | `RibbonMenuLayout` illustrated path, editor layout/list window, designer popup preview | Distinct presentations. Illustrated rows, choice lists and native value slots retain existing geometry. |
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

**Stop for the user's review of the wider Touch fields in the refreshed sample.**
Actual VS/native touch/IME/RTL/mixed-monitor acceptance and the previously noted
broader-suite failures remain outside these automated results. No subsequent
geometry increment, additional theme, commit, push or publication.
