# Native Office 2013 Light and square-window plan

**2026-11-14 — E1 approved; E2–E4 implemented; E5 automated composed review delivered.**
Diagnostic tabs remain retired. Both apps now include the requested theme/accent
galleries and colored-title toggle. E5's sample/public-package checks pass;
composed user visual review remains separate. E2 entered at `fd356b1`; later
user commits `e6ced78` and `f14e856` and authored Form/resource edits are preserved
on the same branch.
Current acceptance remains in [THEMES-PLAN](THEMES-PLAN.md#current-status-and-review-ledger).

## User reference and intended appearance

These are user-provided WPF screenshots used as visual references, not native
WinForms acceptance captures:

- [Neutral caption and ribbon outline](reference/themes/office2013-user-neutral-overview.png).
- [Command hover, selected gallery and tab outline](reference/themes/office2013-user-command-hover.png).
- [Colored caption with a white tab strip](reference/themes/office2013-user-colored-caption.png).

The existing [WPF 2013 tokens](../../src/RibbonKit/Themes/Tokens.Office2013.xaml)
are a read-only color/behavior reference. They identify white surfaces, gray
outline `#919191`, accent `#2B579A`, light accent washes for hover/press/checked,
white File ink and darker File hover/pressed fills. Native implementation must
use its own shared controls/recipes and retain adjustable settings.

| Area | Intended native Office 2013 behavior |
| --- | --- |
| Base surfaces | Flat white caption/tab band/body, square theme-owned chrome, readable dark text. Keep the selected tab white and its text accent-colored. |
| Commands | Hover, pressed and checked/toggled treatments derive from the current accent, replacing 2019's neutral-gray state palette. Use light accent washes for readability, with a stronger checked/pressed treatment; checked-hover remains distinguishable. Cover buttons, toggles, split halves and their QAT/menu projections through shared recipes. |
| File | A full rectangular accent block at rest, white text, darker accent on hover and a deeper pressed/open state. Cover both CommandMenu and Backstage routes, disabled/focus states, closing and reopening. It must not revert to a neutral File button when the menu closes. |
| Ribbon/tab frame | Thin gray ribbon outline and selected-tab top/left/right outline connected to the body. The selected tab's bottom is open into the body, without a double seam or accidental underline. Borders stay visible around the top/header/body connection as in the references. |
| Colored title bar | Optional accent-colored caption with contrasting ink; the tab strip remains white as in the third reference. Do not inherit 2019's default caption-color extension into the tab band. |
| Window silhouette | Square native top-level form corners for both Office 2019 and Office 2013. Office 2024 retains system-default window corners. This is independent of control, popup and Backstage-page radii. |
| Backstage and native pages | Start by reusing the accepted flat Classic Backstage owner/layout and title handoff. Keep A10, native page handles/values/validation and host-owned page-control appearance. Do not add legacy 2010 effects or new motion without a concrete additional requirement. |

## Reuse and bounded extensions

Reuse the neutral command, menu, popup, gallery, dialog and Backstage helpers from
the completed rewrite. Do not clone the 2019 renderer or introduce a family flag
solely to select blue rather than gray.

Three actual gaps need bounded contracts before their implementation:

1. **Accent-derived state defaults.** Changing Accent must update default 2013
   state colors consistently, including derived projections. Keep explicit
   authored state fills authoritative and retain alpha/gradient/Transparent and
   independent Reset. Reuse Hover/Pressed/Checked roles where possible. Resolve
   the live-derivation/explicit-override boundary in the first palette slice;
   do not silently change existing nonnullable public property types or depend
   on a Showcase-only recoloring callback. Any added wash/darkening strengths
   must be adjustable rather than new internal fixed effects.
2. **File state coverage.** Existing HeaderHover/HeaderPressed and the
   HeaderAppearance FileHighlight members do not fully express this contract:
   the current highlight helper covers only an already-open CommandMenu. Add
   the smallest immutable, independently resettable state overrides needed for
   idle/hover/pressed/open fills and ink, with accent-derived 2013 defaults and
   unchanged 2019/2024 fallbacks. Preserve actual text/focus hooks, click targets,
   menu routing and captured closing state.
3. **Connected outline.** Reuse BodyBorder/BodyAppearance.BorderWidth,
   HeaderAppearance.FilledBorderColor/FilledBorderWidth and shared Filled
   primitives first. A selected-tab opening into the body may need a small
   explicit seam policy or a genuine connected-header sibling. Measure current
   paint order and bounds before choosing; do not paint a full rectangle over
   the active-tab opening. Include tab scrolling/collection/visibility changes,
   contextual tabs, minimization, QAT positions, Classic/Simplified and both
   densities. Add no host-owned layout coordinator.

Initial colors should follow the references, but the palette, border width/ink
and state strengths remain changeable public theme settings. User accent changes
must propagate without losing explicit overrides. Inactive/disabled/native EDIT
colors and material contrast remain separate from ordinary enabled button washes.

## Native square-window contract

Yes: Windows 11 exposes `DWMWA_WINDOW_CORNER_PREFERENCE` (attribute 33), including
`DWMWCP_DONOTROUND`. Microsoft's enum documentation lists support from Windows
build 22000. Use the existing RibbonForm native integration boundary rather than
removing the resizable frame or assigning Form.Region:

- [DWM corner preference](https://learn.microsoft.com/en-us/windows/win32/api/dwmapi/ne-dwmapi-dwm_window_corner_preference).
- [DWM attribute and application guidance](https://learn.microsoft.com/en-us/windows/apps/desktop/modernize/ui/apply-rounded-corners).

Additive E2 API:

- `RibbonWindowCornerPreference` with SystemDefault=0, Square=1, Rounded=2 and
  SmallRounded=3, matching the supported DWM values.
- `RibbonTheme.WindowCornerPreference`: SystemDefault for existing 2024, Square
  for 2019 and the 2013 definition added by E3.
- Nullable `RibbonForm.WindowCornerPreference`; null follows the associated theme,
  explicit values win, and Reset restores theme ownership. Validate enum values
  before mutation. This remains an adjustable setting, not a theme-name check
  buried in native paint code.
- `RibbonForm.EffectiveWindowCornerPreference` reports the resolved request,
  including before handle creation and on unsupported systems. It does not
  report native API success or physical appearance. Without an associated
  ribbon, null resolves to SystemDefault. `WindowCornerPreferenceChanged` reports
  override changes; theme changes retain the override. Property-grid Reset uses
  the nullable default, and the effective property is hidden from serialization.

Apply through DwmSetWindowAttribute on eligible top-level runtime handles and
reapply after handle recreation or an effective theme/override change. Skip
designer/child handles, preserve requested values when unsupported and keep a
safe OS fallback. Restoring SystemDefault when switching back to 2024 matters.
Do not change native caption buttons, resize hit zones, maximize/restore/snap,
Mica/Acrylic eligibility, document controls or caption clocks. Do not reset DWM
appearance while Windows is taking the final closing frame; handle destruction
owns native cleanup. Physical corner captures remain a separate acceptance gate
from a successful API call/readback.

## Implementation order and review boundaries

1. **E2 — window-corner preference, implemented.** Native API/lifecycle evidence
   is recorded below; actual visible corner acceptance remains separate.
2. **E3 — 2013 accent palette and File states, implemented.** Finalize accent/default/override rules,
   append a stable Office2013Light built-in identity, and implement accent-driven
   button states plus the always-colored File block. Keep existing identities,
   application-theme Reset, persistence and designer/generated code compatible.
3. **E4 — connected ribbon/tab outline, implemented.** Shared measured connection,
   adjustable borders and immediate selected-header feedback are recorded below;
   current user review is recorded in the themes ledger.
4. **E5 — composed light-family review, automated checks delivered.** Remaining
   native editor/surface handoffs pass in both samples and an independent public
   consumer. Whole-family user visual review precedes another theme or dark variant.

Use relevant focused checks; reuse passed rewrite/geometry evidence unless
affected. Existing 2019/2024 defaults must remain unchanged except the explicitly
requested 2019 window silhouette. Include alternate accent colors, all button/
File states, alpha/zero/Reset, narrow/scrolling headers, both layout modes and
densities, representative DPI and native page retention. Keep automated paint,
native DWM observation, continuous motion, actual VS and physical/mixed-monitor
acceptance distinct. No new diagnostic tabs are authorized by this plan; use
focused fixtures and ordinary sample/Showcase theme selection when implemented.

## E2 implementation and verification — 2026-11-13

`RibbonForm.Corners.cs` owns the policy-to-DWM adapter, using attribute 33 only
on supported top-level runtime handles. Form association/release, effective
Theme/BuiltInTheme changes, explicit override/Reset, handle creation/recreation
and native settings/theme/composition/style notifications reapply the resolved
request. Enum validation happens before state mutation. No form handle is
created just to store a preference. Unsupported calls leave requested settings
intact; designer and child windows skip application. No theme-name branch,
Region, frame removal, backdrop reset, title/pane clock or paint change is added.
Native handle destruction owns attribute cleanup, including final close frames.

Evidence is under `artifacts/window-corners/`:

- **25/25 focused Release cases pass**, zero skips, in `e2-focused-final.trx`:
  15 new corner cases plus 10 existing caption-handoff cases. Covers defaults,
  all choices, explicit override/Reset, application-theme restoration, invalid
  atomic assignments, metadata, inert designer/child HWNDs, supported DWM
  readback, association/release, resize/caption hit targets, maximize/restore,
  recreation, material coexistence, canceled close and WM_DESTROY readback.
  A dedicated change-during-close case retains both title/pane clocks and the
  native page handle/parent/value/selection. The initial run's new motion fixture
  omitted File visibility; enabling its intended Backstage route and deterministic
  test motion policy corrected the setup. Initial `e2-focused.trx` is retained.
- Normal **Debug runtime, designer server/client and persistent Designer Sample
  builds pass with zero warnings/errors**. The sample now uses local package
  `0.1.0-designer.local20261113T091119035Z`; authored source/resources/project files
  and Home/Edit/Gallery startup remain untouched. No diagnostic tabs were added.
- **6/6 independent public-package scopes pass** in
  `public-final/native/result.log`, Windows `10.0.26300.0`, actual **120 DPI**:
  Office 2019/2024 x None/Mica/Acrylic, Classic/Compact only. Each covers all
  four requests, theme changes and Reset, atomic validation, reassociation,
  recreation, retained live native page handles/parent/text/focus/selection with
  no spurious validation on corner changes, native resizing, HTMAXBUTTON,
  standard system maximize/restore/minimize/close, canceled close and final
  WM_DESTROY. Both requested materials applied on this machine.
  `eng/Test-WindowCorners.ps1` creates a fresh public-only package consumer/cache
  with local build-import barriers and normal bin/Debug output; no project
  reference, Showcase resource/helper or friend access is used. The first broad
  consumer compile encountered pre-existing internal Parent/IsDisposed references
  in unrelated fixtures; the scoped runner compiles only the E2 fixture and its
  entry point, leaving those unrelated fixtures unchanged.

Reproduce from the repository root after `eng/Update-DesignerSample.ps1` refreshes
the normal Debug package:

```powershell
dotnet test winforms/tests/RibbonKit.WinForms.Tests -c Release --filter "FullyQualifiedName~WindowCornerPreferenceTests|FullyQualifiedName~BackstageCaptionHandoffTests"
& winforms/eng/Test-WindowCorners.ps1
```

These are native API/offscreen lifecycle and package checks, **not visual corner
acceptance**. No physical silhouette capture, all-four-corner/DPI symmetry review,
desktop Snap gesture, actual VS session, physical input/IME or mixed-monitor
acceptance ran. Unsupported-OS policy is deterministically exercised, but an
actual pre-build-22000 machine was not available. The accepted title handoff is
protected by focused regression checks; no new visible-motion acceptance is
claimed. No broad geometry/appearance/native matrix, full suite or WPF check ran.
E2's implementation boundary is retained; E3 followed on the user's continuation.

## E3 accent palette, File states and sample appearance controls — 2026-11-13

`Office2013Light = 2` appends to the built-in identity without renumbering 2024/2019.
It reuses Filled headers and Classic Backstage with white surfaces and Square
window preference. No new renderer family/selector or layout algorithm was added.
The connected gray outline/seam remains E4. `HeaderSelectedUsesAccent` supplies
live selected-tab accent ink; explicit selected ink, contextual and disabled roles
retain precedence. The optional colored caption keeps a separate white tab strip.

`AccentStates` supplies adjustable live hover/pressed/checked/checked-hover washes,
initially 0.10/0.20/0.16/0.25 over Surface. All strengths validate finite 0..1 before
Theme mutation. Existing nonnullable Hover/Pressed/Checked signatures remain:
assignment sets the explicit HoverOverride/PressedOverride/CheckedOverride value;
null on an override resets only that state. CheckedHover is separately nullable.
Explicit alpha, gradients and Transparent survive accent changes. Office 2019/2024
ordinary hover/press remain neutral; the user's follow-up below makes their
checked/selected states accent-derived too. Record equality retains authored
override ownership even when it happens to equal a derived default.

`FileButtonAppearance` opts into one shared accent-filled rectangle for either
File route; null retains legacy rendering and open-highlight settings. Idle/hover/
pressed/open/disabled fills and ink reset independently. Null fills derive from
Accent or the idle fill; hover/pressed/open darkening starts at 0.10/0.20/0.20 and
supports zero. Foreground defaults to White. Existing filled-header radius/border
overrides, actual text/focus hooks, targets, routing and closing flags remain.
Captured closing and closed File return to the accent idle fill. Commands/splits,
QAT/menu projections and galleries share checked-hover selection; null preserves
the earlier palettes' fallback behavior.

Both **Showcase and Designer Sample** provide View theme/accent galleries and
**Color title bar**. Six opaque accents plus Default are supplied. Title coloring
uses the effective accent and white caption ink; off restores previous caption
color/ink while retaining the accent. Default resets only the accent. Selecting a
theme resets its accent and retains title-color mode. A small shared sample
preference adapter composes existing public settings and retains base identity/
application-theme ownership; it owns no controls/renderers. Showcase persists
base theme, optional accent ARGB and title mode in backward-compatible version-one
preferences. Designer Sample declares ordinary galleries, tiles and toggle in
InitializeComponent. No diagnostic tabs were restored. The sample no longer pins
TitleBarColor to White: Office 2019 follows its existing #E6E6E6 caption/header.

The reported SVG loss was reproduced with framework CodeDOM loading: omitted
optional constructor arguments are not bound by the designer. All authored SVGs
now explicitly supply the Boolean color-mode argument. The regression loads the
actual source expressions, saves Icon members through framework code/resources,
and reloads all **11** theme/accent SVG sources and color modes. Actual VS
save/reopen remains a separate review; no library serialization workaround.

Evidence under `artifacts/office2013/`:

- `e3-focused-final.trx`: **78/78 Release cases** for palette/overrides/zero/atomic
  validation, four-DPI command/toggle/split/QAT/menu/File paint, File routes and
  closing/reopen/disabled/focus hooks, designer enum/protocol, preference identity,
  2019 fallbacks and SVG framework round trips.
- `e3-dependencies.trx`: **84/84 relevant existing Release cases**, covering command,
  Application Menu and gallery appearance plus the accepted caption handoff.
- `appearance-controls-final.trx`: **47/47 Release cases** for sample appearance
  ownership/Reset/white caption ink, SVG load/save/reload and preference compatibility.
  SVG cases overlap the set above; do not sum these as unique tests.
- Normal Debug runtime/designer server/client, Designer Sample and Showcase builds
  pass with zero warnings/errors. The persistent sample uses local package
  `0.1.0-designer.local20261113T125147350Z`; the user's Backstage routing and resx
  tray metadata remain intact.
- `e3-final-alpha-designer.trx`: **13/13 focused Release cases** verify final
  transparent-surface accent composition and all 11 authored SVG expressions
  through framework load/save/reload. Other passed dependency sets were not repeated.
- **8 public-package paint scopes** pass in `paint-final/native/result.log`: Office/
  neutral renderers x 96/120/144/192 DPI, each with three accents, command/QAT states,
  File idle/hover/pressed, alpha/gradient/Transparent and Reset. **8 native scopes**
  pass in `native-final/native/result.log`: Classic/Simplified x Compact/Touch x
  CommandMenu/Backstage, actual 120 DPI. Each checks three accents, File open/close/
  reopen, permanent accent fill, application Reset, separate colored caption/tab
  band and retained native editable-choice/page handles/text/selection. The fixture
  uses the supported Simplified editable ComboBox path, configured before assigning
  free text; no runtime timing workaround was added.
- `appearance-final/designer/result.log`: four theme choices in both layouts,
  six accents for each light family in both layouts, title toggle/white ink,
  independent Default/off, selected family and native/model retention. The Classic
  expanded source is checked before activation pumping. The Showcase counterpart
  in `appearance-final/showcase-layout/result.log` covers real gallery/toggle
  actions, all six accents and isolated saved-preference restart restoration.
  Both apps' direct-paint layouts and the independent state grid were inspected.

`eng/Test-Office2013.ps1` builds a fresh public-only package consumer/cache with
normal Debug output and no Showcase/shared sample helper. `-NativeOnly` and
`-PaintOnly` select scopes without repeating unrelated passed checks. No full suite, WPF checks or
unrelated geometry/material matrix ran. Offscreen/native API and direct-paint
checks do not establish physical corners, continuous motion, actual VS, physical
input/IME or mixed-monitor acceptance. Stop after E3. Next: **E4 connected gray
selected-tab/ribbon outline**.

### E3 follow-up: 2019/2024 accent selection and dialog buttons — 2026-11-13

The user's WPF comparison clarified the policy: **both 2019 and 2024 retain grey
ordinary hover/press and use the current accent for toggles and selected gallery
tiles**, including stronger checked-hover. Read-only WPF ThemeManager evidence
uses 0.18/0.28 custom-accent coverage over White; native presets use those strengths
over Surface. Office 2019 keeps #DEDCDA/#CFCDCB ordinary states and its neutral
header; 2024 keeps #E6E6E6/#DADADA ordinary states. 2013 retains all four accent
washes. No WPF source/build/test changed.

`AccentStates.HoverBackground`/`PressedBackground` hold independently adjustable
neutral defaults without materializing them as explicit theme overrides. Null on
either restores its wash; explicit Hover/Pressed/Checked and CheckedHover still
win and reset independently. All routes use the shared checked-hover recipe,
including illustrated choices, splits, QAT, Application Menu and galleries.
The user subsequently clarified that accent text belongs to active sibling
navigation (selected tab, Backstage entry and Options page), not ribbon commands.
Command/gallery text, vectors and arrows retain ordinary foreground in every
state. The overbroad unshipped CommandForegroundUsesAccent/CommandStateForeground
properties and passive content-context helper were removed. Existing navigation
roles retain their accent/contrasting white text; actual renderer hooks, borrowed
artwork and disabled/native editor roles remain authoritative.

`OptionsButtonAccentStates` supplies a separate default palette for library
configuration buttons: **only primary OK has a solid accent idle fill**; Cancel,
Apply, Reset, Reload and other ordinary buttons use Surface (white by default)
with ordinary text. Ordinary hover/press use 0.10/0.20 accent washes; primary OK
darkens its idle fill by 0.10/0.20 with Surface ink (white by default) throughout.
Pressed buttons retain an Accent border. Existing ButtonBorderColor/Width override
that pressed border, including alpha/Transparent/zero; a primary pressed border
uses the ordinary one-device-pixel default. Explicit options-button and state
fills win. Null restores legacy ordinary/system and primary treatment;
native click/default/cancel actions, bounds and handle ownership remain intact.

#### Filled OK feedback refinement — 2026-11-13

The user requested that OK keep its filled appearance instead of switching to a
pale wash. The shared Options recipe now darkens each idle-fill gradient stop
by 0.10 on hover and 0.20 on press, preserving alpha. Secondary-button fills,
navigation ink, border overrides, disabled roles and native actions remain.
OptionsAppearance adds nullable AccentButtonHoverDarkening/PressedDarkening,
explicit AccentButtonHoverBackground/PressedBackground and AccentButtonForeground.
Zero darkening retains the exact idle fill; null restores defaults independently.
Explicit primary state fills win, followed by existing theme state overrides and
the explicit options pressed fill. Invalid strengths fail before Theme mutation.
OptionsButtonAccentStates null retains legacy behavior, subject to explicit
primary overrides.

Evidence under `artifacts/accent-adaptation/`:

- `primary-darken-before.trx` reproduces the pale hover. `primary-darken-focused.trx`
  passes **23/23 Release cases**: all three primary presets/three accents,
  progressively darker feedback, white native text, four-DPI button/border paint,
  alpha/gradients, zero/reset, explicit fills/ink, validation and existing Options
  shell native action/ownership checks. `primary-darken-overrides-final.trx`
  passes **2/2** targeted cases after preserving the explicit options pressed fill.
- `primary-darken-final/native/result.log`: **2 independent package-only native
  dialog scopes** pass (2019/2024, each three accents, actual 120 DPI). Targeted
  native mouse-move/Space messages verify hover/press pixels, white OK text,
  unchanged secondary washes, borders and native button/field state. Idle/hover/
  pressed captures were inspected. `eng/Test-AccentAdaptation.ps1 -NativeOnly`
  skips unrelated command/gallery scopes.
- Normal Debug runtime/designer/client and both sample builds pass with zero
  warnings/errors. Persistent Designer Sample is refreshed in place to package
  `0.1.0-designer.local20261113T140211698Z`. No authored sample resources changed.

No unrelated theme matrix or full suite was repeated. Programmatic native
messages/offscreen paint do not establish physical pointer/keyboard, actual VS,
mixed-monitor or user visual acceptance. E4 remains next.

Navigation/text and primary-idle correction evidence below predates this filled
OK feedback refinement; retained under `artifacts/accent-adaptation/`:

- `navigation-button-correction-before.trx` reproduces **10 failures** in ordinary
  command ink, secondary-button idle fill and pressed border before the correction.
- `navigation-button-correction-final.trx`: **58/58 Release cases** pass for
  ordinary command/gallery text/vector hooks, accent selected fills, active
  tab/Backstage/Options navigation ink, native OK/Cancel/Apply hover/key-press
  callbacks, darker washes/accent borders at four DPI values, authored border
  overrides/hiding and retained native actions/handles/selection. Existing
  affected Options/content checks also pass. `navigation-ink-final.trx` reruns
  the one navigation case after removing analyzer warnings; clean compile.
- `navigation-only-public/native/result.log`: **8 independent public-package
  paint scopes** (both renderers x four DPI values, both themes/three accents),
  plus **2 native dialog scopes** at actual 120 DPI, pass. Public native Space
  messages verify the actual held-key wash/border; losing focus cancels the key
  without activating buttons. Field/button handles, bounds, text/selection and
  AcceptButton/CancelButton remain unchanged. Current captures follow in
  `navigation-only-review/native/`, including idle/pressed native buttons.
- Normal Debug runtime/designer/client, Designer Sample and Showcase builds
  pass with zero warnings/errors. The persistent Designer Sample uses package
  `0.1.0-designer.local20261113T135021022Z` after the final XML documentation
  refresh; its behavior matches the sample action checks. Current public-package
  evidence and captures are in `navigation-only-review/native/`.
  Sample action/persistence checks in `navigation-only-samples/` pass;
  both apps' 2024 selected tiles/toggle keep ordinary text/icon colors. Their
  navigation/header and independent dialog/state captures were inspected.

Earlier evidence below predates the user's navigation-only text and primary-only
button correction. It is retained as history, not acceptance of the corrected
foreground/idle-button policy:

- `neutral-hover-accent-selection.trx`: **52/52 Release cases** for both themes'
  final hover/selection policy, four-DPI actual root/menu/QAT/split/gallery content
  hooks and fills, explicit overrides/reset, dialog palette and native button/
  field retention, plus affected 2013/command/options appearance dependencies.
- `content-designer-final.trx`: **94/94 Release cases**, including the final
  illustrated-choice recipe, neutral recipe gradients/Transparent/reset,
  affected content/disabled paint, inert designer previews and authored SVG
  framework load/save/resource reload. Some cases overlap the set above.
- `public-final/native/result.log`: **8 public-only package paint scopes**
  (neutral/Office renderers at 96/120/144/192 DPI, each with both themes and three
  accents) plus **2 native options-dialog scopes** at actual 120 DPI. They check
  actual toggle/gallery pixels and text/vector hooks, explicit fills/Reset,
  accent OK/Cancel/Apply fills and retained native field/button state. The fixture
  is isolated from project references and all sample helpers by
  `eng/Test-AccentAdaptation.ps1`; review captures use sufficient gallery height
  for full labels in `public-review/native/`.
- Normal Debug runtime/designer server/client and both apps build with zero
  warnings/errors. Designer Sample was refreshed in place to local package
  `0.1.0-designer.local20261113T132626889Z`; authored routing/resources are preserved.
- `samples-final/designer/result.log` and `samples-final/showcase/result.log`
  pass actual gallery/toggle actions for all three themes and six accents,
  independent Default/off, white title ink and native/model retention. Showcase
  also checks isolated saved-preference startup restoration for each family.
  Both apps' 2019/2024 purple selection/title captures and independent state/
  dialog captures were inspected.

No full suite, unrelated geometry/material matrix or computer-use tools ran.
Synthetic-DPI direct paint and offscreen native checks do not establish actual
VS save/reopen, physical input/IME, mixed-monitor, native corner silhouette or
user visual acceptance. E4 remains the next slice; no connected-outline work here.

## E4 connected selected-tab/body outline — 2026-11-14

The accepted E3/sample/button work was committed locally at the user's request as
`5a85343`; the user subsequently committed E4 and the authored FileMenuMode setting
at `f14e856`. E5 started from that clean checkpoint without changing commits.

Office2013Light now uses BodyBorder `#919191`, a complete body perimeter and
HeaderAppearance.ConnectSelectedTabToBody. The shared Filled renderer and neutral
renderer use the same connected-frame helper. No family selector, layout engine,
host coordinator, diagnostic tab, native window frame or Backstage clock changed.

Connection geometry comes from the existing body frame, selected-tab bounds and
header clip. Only the visible tab interior opens the body's top border; the tab
keeps top/left/right outlines and an open bottom. Fully off-viewport selections
leave the body border complete. Missing/separated bodies and minimized headers
keep a closed tab frame. Body border ink/width and FilledBorderColor/Width remain
independently adjustable, including Empty fallback, alpha, Transparent and zero.
The selected outline inherits the body border when those header overrides are
unset. ConnectSelectedTabToBody false disables the connection. Disjoint paint
clips retain the original full-frame fill/gradient and avoid double alpha
composition. Very thick strokes clamp consistently on short frames.

RibbonPaintContext.HeaderBodyConnectionBounds carries passive measured geometry
to actual body/tab renderer hooks; standalone public painters can supply it.
Target bounds, layout snapshots, source ownership and native editor handles are
unchanged by the connection option.

The user reported that the border arrived late in Designer Sample. Its
Expressive/SlideDown setting exposed the existing whole-header crossfade, and the
transient body frame lacked the connection payload. Connected ordinary tab
selection now commits the arrival header and opening from frame zero. Content
still fades/slides, and newly arriving headers retain their separate policy.
Office 2024 Underline/pill motion stays on its original path.

Evidence under `artifacts/office2013-outline/`:

- `outline-before.trx` reproduces the missing top outline. `outline-focused.trx`
  passes **8/8 Release cases** for both renderers at four DPI values, visible/
  clipped joins, overrides/Reset, alpha and live selection/scroll/visibility/
  collection/minimize changes. `outline-final.trx` passes **28/28**, adding short-
  frame stroke-clamp coverage and relevant body-presentation cases.
- `outline-dependencies.trx`: **56/56 relevant Release cases** pass for existing
  header/body/QAT/clipping and 2013 palette behavior. Sets overlap; do not sum them.
- `public-final/native/result.log`: **8 public-only paint scopes** (neutral/Office
  x 96/120/144/192 DPI) and **4 native scopes** (Classic/Simplified x Compact/Touch,
  actual 120 DPI) pass. Three QAT positions, neutral/colored captions, contextual,
  partial/off-viewport selection, connection off/Reset, minimize/restore and native
  field handles/parent/text/selection are covered. Default and authored-border
  captures were inspected. `eng/Test-Office2013.ps1 -OutlineOnly` isolates E4.
- `outline-motion-before.trx` reproduces the late state in all four Fade/SlideDown
  and Subtle/Expressive combinations. `outline-motion-final.trx` passes **83/84**
  motion/outline cases; its old immediate-Backstage-page fixture assumed page
  animation was disabled. That fixture now explicitly sets PageDuration=0 to
  match its stated scope; no production Backstage change. The targeted rerun in
  `outline-motion-targeted-final.trx` passes **5/5** including that fixture.
- `outline-motion-frames-final.trx`: **4/4** exact-frame cases pass. Both old/new
  tab headers match the final header pixels from progress zero through completion,
  the body opening is already clear, and body pixels still move/change. Captures
  in `motion-frames/` were inspected. `office2024-pill-final.trx` passes **6/6**
  existing spring/settling cases after the user's explicit 2024-pill question.
- `public-motion-before/native/result.log` reproduces the missing transient
  connection in a package consumer. `public-motion-final/native/result.log` passes
  all **4 native scopes**, including the first live Expressive/SlideDown frame
  and animation completion. Public hooks are inspected before another root paint
  can conceal the transient-layer bug. Only native scopes were repeated.
- Normal Debug runtime/designer/client and both samples build with zero warnings/
  errors. Designer Sample is refreshed in place to local package
  `0.1.0-designer.local20261114T035724165Z`; authored Forms/resources are preserved.
  Both ordinary sample action/persistence checks pass under `samples/`, and their
  Office 2013 colored-caption/white-strip/connected-outline captures were inspected.
  `outline-alpha-gradient-final.trx` separately verifies the final opening's full-
  frame gradient endpoints and single alpha composition, plus border override/Reset.

### Tab-switch flash follow-up — 2026-11-14

The user reported intermittent form/ribbon flashes in Designer Sample using
Office 2013. Instrumentation found that each solid tab animation added and removed
WS_EX_LAYERED on the live ribbon, despite its opaque motion drawing already
covering the committed state. That native style churn is a plausible compositor
flash source; the reported random physical flash itself was not captured.

Stable, non-minimized tab-content changes with an opaque background now retain
the live ribbon's native style. Completion and interruption repaint native
children while the opaque motion window still covers them, before retiring it.
Host selection callbacks that also alter frame geometry commit immediately.
Other geometry/minimize transitions and translucent solid backgrounds retain
their drawing hold; the material path and 2024 pill policy are unchanged.

Evidence under `artifacts/tab-switch-flash/`:

- `handoff-before.trx` fails both Fade/Subtle and SlideDown/Expressive profiles.
  Each trace records 12 animation starts (3 complete, 9 interrupted), 12 live
  ribbon layer-style add/remove cycles and 12 host nonclient paints. After the
  change the same counts are zero for style cycles and nonclient paints; motion
  still starts and native editor handles, parent and value remain unchanged.
- `handoff-focused.trx` passes **57/57 Release cases** across handoff, geometry
  motion, material handoff and connected outline. `handoff-coverage-final.trx`
  passes **4/4**, strengthening coverage of repaint under a visible opaque cover
  and geometry-changing/throwing host callbacks. These scopes overlap.
- `public-final/native/result.log` passes **4 independent public-package scopes**
  (Classic/Simplified x Compact/Touch, actual 120 DPI). Each observes 4 completed
  and 4 interrupted handoffs with zero live ribbon/form layer-style cycles;
  animation and native editor handle/parent/text/selection retention are checked.
- Normal Debug runtime/designer/client, Designer Sample and Showcase builds pass
  with zero warnings/errors. The persistent Designer Sample is refreshed in place
  to `0.1.0-designer.local20261114T044002490Z`; authored files remain intact.

These checks establish the native lifecycle correction, not physical display
acceptance. The subsequent user review is recorded once in the themes ledger.

No full suite or unrelated palette/material matrix ran. Offscreen native windows,
direct paint and deterministic transient frames do not establish actual VS
save/reopen, physical input/IME, desktop material pixels, native window-corner
silhouette, mixed-monitor timing or user appearance acceptance. E5 followed below
on the user's authorized continuation.

## E5 composed light-family/native-editor review — 2026-11-14

No runtime or public API correction was needed in this bounded review.
`Office2013CompositionChecks` uses public RibbonKit/WinForms APIs against an
independently authored model and, through explicit command-line checks, the
ordinary Showcase and Designer Sample models. It adds no diagnostic tabs and
does not run during normal startup. Both samples link the same public-only
fixture; the isolated package runner copies that fixture without any sample
resources, helpers, project references or internal access.

The checks cover an accent change while Backstage is open, native page switching
and closing back to the ribbon; real native ribbon and popup editors; menu and
gallery open/reopen with source, selection and borrowed artwork retained; caption
color Reset; and actual Options navigation, accent OK/neutral Cancel idle fills,
rejected validation, selected-page Apply and post-Apply Cancel. A staged QAT removal
is refreshed in the real editor and retained across Options navigation without
changing live commands. Custom draft controls retain their native handle and parent.
Options Cancel leaves the authored tabs, groups, commands, QAT and pages intact.

Evidence under `artifacts/office2013-composed/`:

- `public-reviewed/native/result.log`: **4/4 new native scopes pass**,
  Classic/Simplified x Compact/Touch, actual **120 DPI**, using package
  `0.1.0-designer.local20261114T044002490Z` from a fresh isolated package cache.
- `designer-reviewed/result.log` and `showcase-reviewed/result.log`: both ordinary
  authored sample models pass at actual **120 DPI**, Classic/Compact. These are
  separate sample checks, not four additional mode/density scopes per sample.
- Both samples' normal **Debug builds pass with zero warnings/errors**. Existing
  package/runtime outputs are retained; no runtime change required a new package.
- Client-surface, popup, Backstage and Options captures were inspected, including
  the refreshed pending-QAT view. Captures establish direct-print appearance only.

The fixture initially assumed a split button's primary click opens its menu;
it now uses the public ShowDropDown arrow route. Offscreen popup inspection is
synchronous because pumping unrelated foreground deactivation correctly dismisses
it. Full-form printing also gave misleading child order/native caption placeholders;
ribbon and Backstage captures now target their actual client surfaces. Initial
fixture runs remain in the evidence directory and are not counted as passing gates.

Reproduce only this new scope from the repository root:

```powershell
& winforms/eng/Test-Office2013.ps1 -ComposedOnly
& winforms/eng/Test-DesignerSample.ps1 -Check --verify-office2013-composed -Output winforms/artifacts/office2013-composed/designer-review
dotnet run --project winforms/samples/RibbonKit.WinForms.Showcase -c Debug -- --verify-office2013-composed winforms/artifacts/office2013-composed/showcase-review
```

Animation is disabled only inside these checks; E4's accepted tab motion/flash
and 2024 pill evidence is reused. No unrelated palette/border/material matrix,
Release/full test suite, WPF check or computer-use tool ran. Programmatic opening
and direct print do not establish physical keyboard/mouse/IME, actual VS designer
save/reopen, DWM corner silhouette, desktop material pixels, continuous motion or
mixed-monitor acceptance. User composed visual review remains open. Stop at E5;
no next family or dark variant is implicitly authorized.

## Diagnostic-tab retirement evidence

The authored Designer Sample has only Home/Edit/Gallery. Review tabs were added
by two interactive startup modes. Program now leaves the authored collection
intact, retires geometry-tab creation, and makes `--office2019-light` (plus the
legacy `--office2019-theme-demo` alias) a tab-free 2019 selection. Existing
explicit `--verify-*` fixtures and their review-model builders remain available;
no library features, regression coverage, authored Forms/UserControls/resources
or user settings were deleted.

Normal Debug sample build passes with zero warnings/errors. Three scoped launch
checks pass under `artifacts/review-tab-removal/final/`: normal startup, combined
legacy switches and the new 2019 switch. Each retains the current tab/group/
command/QAT/page/control references/order, File mode, layout and density and
contains exactly Home/Edit/Gallery. The first attempt called an older broad
round-trip check whose Command identity expectation no longer matches the
current authored sample; its failure is retained under the initial default
artifact folder. The scoped check compares the actual current graph and does
not change authored commands to satisfy that older fixture. No full library,
geometry or caption matrix was rerun for this consumer-only launch change.
