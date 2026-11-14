# Native Office 2010 Blue — normal theme first

**2026-11-14 — F3 shell delivered; reported hover/minimize details and shared File-width follow-up corrected, awaiting user recheck. Next implementation is F4 below-tabs Backstage.**
The user selected Office 2010 after the Office 2013 review and supplied eight
WPF references, including an additional ordinary-button hover example. They
explicitly confirmed rounded corners for this non-flat theme. Deliver the normal
opaque Office 2010 Blue theme first. An Aero-inspired frame is a separate later
experiment, after normal-theme review; neither these gradients nor a colored
caption constitute Aero support.

Current acceptance belongs in the
[themes ledger](THEMES-PLAN.md#current-status-and-review-ledger). F1 is a contract
and source assessment, not a build, native implementation or visual acceptance.
F2 delivers reusable paint support; F3 now supplies the Office 2010 ribbon/File
preset. The complete family is not accepted before F4/F5.

Before F2, the user requested Designer Sample cleanup. Checks now live in the
[dedicated test host](../tests/RibbonKit.WinForms.DesignerSample.Tests/README.md);
retired rewrite/consolidation demo builders are removed. That cleanup is committed
at `c7794b6` and remains separate from F2.

## Baseline and scope

- Branch `codex/Ribbonkit-Winform`, HEAD `14aba66` (`2013 E5 Slice`). Entry status
  contained the uncommitted E5 acceptance ledger update and a Designer Sample
  `DesignerForm.Themes.cs` edit. Preserve user edits and commits; recheck live
  status because the authored file may change during the session.
- Work only inside `winforms/`, with one agent, normal Debug/Release outputs and
  retained evidence. No computer-use tools, commit, staging, push or publication.
- Parent WPF source/history is read-only reference. Keep authored sample Forms,
  UserControls, SVGs, resources and persistence. No diagnostic/geometry tabs.
- Keep accepted Office 2013/2019/2024 behavior, including connected-tab immediate
  feedback, style-stable tab motion, 2024 pill motion and closing-title handoff.
- Application document content, Backstage page controls, command handlers and
  icons remain host-owned. The WPF document cards, greetings, warning text and
  sample-only commands are not a request to copy that application into WinForms.
- Dark/Black/Silver variants, Office 2007, Aero/glass reflections, custom OS frame
  replacement and any new material compositor are outside the normal-theme scope.

## Supplied references

These files are unchanged copies of the user's WPF screenshots. They define
visual intent, not native acceptance or embedded instructions:

| Reference | What it establishes |
| --- | --- |
| [Neutral overview](reference/themes/office2010-user-neutral-overview.png) | Pale blue caption/tab band, light blue body gradient, connected selected tab, blue File, full-width lower QAT and message stack. |
| [Colored caption](reference/themes/office2010-user-colored-caption.png) | Optional blue gradient caption with white title ink; tab band retains its light blue treatment. |
| [Theme gallery](reference/themes/office2010-user-theme-gallery.png) | Blue-tinted floating surface, pale heading bands, rounded frame/shadow and gold selected tile. |
| [Backstage](reference/themes/office2010-user-backstage.png) | Caption and complete tab row remain visible; panes begin below tabs, with a blue top connector, pale blue navigation, dimensional blue selected row and white page. |
| [Open split button](reference/themes/office2010-user-open-split.png) | Gold active half, thin shared perimeter/divider, readable ordinary ink and blue-tinted menu. |
| [Choice popup](reference/themes/office2010-user-choice-popup.png) | Native field remains white; dropdown has blue-tinted backing, heading bands and gold selected row. |
| [States and ScreenTip](reference/themes/office2010-user-states-tip.png) | Rounded gold checked states, normal check/radio roles, readable disabled commands and a light rounded tip. |
| [Ordinary command hover](reference/themes/office2010-user-command-hover.png) | Add Message uses a smooth pale-gold hover ramp, amber outline and light inner edge; text/artwork keep their normal ink. |

Read-only implementation references:
[WPF palette](../../src/RibbonKit/Themes/Tokens.Office2010.xaml),
[gradient/state pitfalls](../../docs/history/library/03-office-themes-and-layout.md#327-office-2010-blue-theme--the-first-gradient-theme),
[below-tabs placement](../../docs/history/library/07-backstage-and-crystal-foundation.md#395-office-2010-backstage-begins-below-the-tab-headers--2026-08-13),
[File/seam/tab handoff](../../docs/history/library/07-backstage-and-crystal-foundation.md#396-office-2010-backstage-tab-strip-polish--2026-08-13).
Historical WPF test counts and live approval do not transfer to native controls.

## Appearance contract

All dimensions below are 96-DPI logical pixels. Initial values follow the WPF
reference; public theme settings remain independently adjustable and resettable.

| Area | Normal Office 2010 Blue contract |
| --- | --- |
| Caption and tab band | Neutral caption ramp `#EAF1F9 → #D8E5F3`; tab surround `#D8E5F3 → #CBDAEC`. Keep the join continuous. Optional colored caption has white title ink and retains the light tab band. Form.TitleBarColor remains authoritative. |
| Body | Full-width vertical `#F6F9FC → #DAE5F1`, thin blue-grey outline `#8DA5C4`, subtle group dividers. Preserve content padding and current layout. Below-ribbon QAT uses the matching blue surface and separator. |
| Selected tab | Light `#FCFDFF → #F6F9FC`, dark blue `#15428B` ink, blue-grey top/side outline, rounded top corners and an open bottom connected to the body. No 2024 pill. The header/opening commits immediately while content animation continues. |
| Rounded geometry | Ordinary controls, fields, gallery tiles, menu rows, popups and tips start at radius 3; small controls at 2. Tabs and File use radius 3 at the top and square joined bottoms. Split halves round only exposed perimeter corners. Full-width body/QAT/message joins remain straight; non-flat does not mean rounding every joined edge. |
| Native window | Theme requests `RibbonWindowCornerPreference.Rounded`, independently of control radii. Preserve nullable Form override/Reset, unsupported retention, native resizable frame, caption actions, maximize/restore/snap and backdrop eligibility. No Form.Region or Aero frame. |
| Ordinary hover | Smooth four-stop gold ramp: `0:#FFF7CE`, `.5:#FFDC8C`, `.9:#FFEFB6`, `1:#FFFFFB`; outer border `#E9C25E`, subtle `#88FFFFFF` inner rim. Keep ordinary command text/icons. |
| Pressed/checked | Pressed is deeper (`#F3C258 → #F8CC6A → #FCDA86`) with border `#CB9A34`. Checked and checked-hover use separately adjustable gold ramps, not a copied hover or a blue accent wash. Preserve disabled/read-only, focus and check/radio semantics. |
| Split controls | Active half receives its state fill; the companion stays clear with a shared amber perimeter/light rim. Preserve primary/arrow routing and existing horizontal/vertical geometry. |
| File | Rounded-top blue gradient, white ink, distinct idle/hover/pressed/open ramps and thin frame/rim. Both CommandMenu and Backstage routes retain their existing source/event contract. Closing restores the idle blue gradient. |
| Popups and galleries | Rounded blue-grey frame, blue-tinted opaque ramp, restrained shadow, pale blue headings, white inline gallery well, gold selected/hover tiles. Preserve custom presenter ownership and borrowed vectors. |
| Native fields and choices | White editor well, blue-grey border, independent focus/hover states. Choice popup backing/headings use 2010 surfaces and gold active rows. Native text/caret, selection, IME and validation retain their owners. |
| Backstage | New below-tabs presentation described below. Its pale navigation is not the solid accent rail used by 2013. Content stays white and page-control appearance remains host-owned. |
| Options and utility surfaces | Reuse accepted navigation, primary OK/secondary-button roles, native controls and draft lifecycle. Apply rounded geometry and shared 2010 surfaces/state chrome through their existing roles. Do not invent a separate dialog layout or recolor host page controls. |
| Accents | Preserve sample accent selection and title toggle. Blue identity surfaces (File, selected Backstage row and colored caption) adapt through an adjustable recipe; gold command highlights remain gold by default. Explicit state/surface overrides win. Do not use a host recoloring callback or recolor ordinary command text. |

File's initial blue ramps are idle `#4E97CE/#2470B2/#3D86C4`, hover
`#63A9DD/#3382C2/#4E96CE`, pressed `#2C71AC/#175792/#276CA4`, and open
`#3E8BC5/#1E62A1/#3277B4`, at offsets 0/.5/1. Accent adaptation must retain
their relative lightness/depth and expose its strengths; simply darkening a
single solid accent is not equivalent to this reference.

## Native reuse and actual gaps

| Owner | Reuse | Bounded extension needed |
| --- | --- | --- |
| [RibbonTheme/RibbonFill](../src/RibbonKit.WinForms/Rendering/RibbonTheme.cs) | Immutable palette, two-stop fills, explicit override/reset aliases, stable built-in identities. | Multi-stop fills; gradient companions for roles currently restricted to Color; an explicit 2010 identity recipe. |
| [Shared renderer](../src/RibbonKit.WinForms/Rendering/RibbonRenderer.cs) and neutral paint helpers | Actual public virtual hooks, source-owned text/icons, clipping and opacity. | Central fill evaluation so intermediate stops survive alpha transforms, motion, clipping and solid fast paths. |
| [Command appearance](../src/RibbonKit.WinForms/Rendering/RibbonCommandAppearanceSettings.cs) and [File appearance](../src/RibbonKit.WinForms/Rendering/RibbonFileButtonAppearanceSettings.cs) | State fills, sizes/insets, split geometry, File routing and captured closing state. | Opt-in per-state outer ink and adjustable inner rim; File depth/rim cannot be approximated by recoloring command text. |
| [Header appearance](../src/RibbonKit.WinForms/Rendering/RibbonHeaderAppearanceSettings.cs) and connected-frame helper | Filled selection, radius, measured connection and immediate header feedback. | Independent hover/pressed outline ink alongside the selected blue-grey outline. Reuse Filled, not a new family flag for a palette. |
| [Presentation resolver](../src/RibbonKit.WinForms/Rendering/RibbonPresentationResolver.cs) | One pairing of family layout, drawing, caption and motion defaults. | A genuine 2010 Backstage sibling for below-tabs placement and exposed-header behavior. |
| [Backstage host adapter](../src/RibbonKit.WinForms/RibbonControl.Backstage.cs) | Real native page host, controls, clipping, title integration and closing lifecycle. | Current bounds start below the caption or at the form top. A measured below-tabs anchor and reflow/input handoff are new work. |
| [Designer protocol](../src/RibbonKit.WinForms.Design.Protocol/RibbonDesignSnapshot.cs) and design committer | Type-name/string boundary and inert previews; no runtime dependency from net472 client. | New built-in selection/parse/reset/Undo/Redo support and any added immutable-value representation. |
| Both samples and independent consumer | Authored View galleries, shared public appearance adapter, persistence and native controls. | One designer-authored 2010 tile per sample with a serializable two-argument SVG constructor, plus focused public-package verification. Preserve existing user edits. |

## Additive API contracts by slice

The names below are the concrete proposed additions for implementation review.
Reuse an existing member wherever it already expresses the contract; do not add
duplicate settings or silently change existing public property types/defaults.

### F2: fill and state-chrome foundations

- Keep `RibbonFill(Color)` and `RibbonFill(Color, Color)`, Start/End, deconstruction
  and legacy two-stop behavior. Add `RibbonGradientStop(double Offset, Color Color)`
  and an immutable, defensively copied `RibbonGradientStops` value collection.
  `RibbonFill.IntermediateStops` is nullable; null/empty means the existing ramp.
  Intermediate offsets are finite, strictly increasing and strictly between 0/1;
  endpoints remain Start/End, including after existing `with` endpoint edits.
  Equality is structural, not collection identity. Expose no mutable arrays or
  disposable GDI objects. Invalid assignments fail before effective theme mutation.
- Add an optional gradient direction with existing vertical behavior as the
  default and horizontal available for genuine directional surfaces. Null/reset
  restores existing behavior. Rendering uses one resolved fill helper; checks
  must include equal endpoints with different intermediate colors, alpha,
  short bounds, clipping and preserving every stop during opacity transforms.
- Add nullable fill companions only where the existing Color role blocks the
  reference: ribbon/header surround, caption, popup, lower QAT and Backstage rail.
  Use existing BodyBackground, heading, state, File and message fills directly.
  Old Color setters keep their authority by clearing the corresponding fill
  override; assigning the new fill explicitly owns that role. Null restores its
  old Color fallback. Keep Form.TitleBarColor above theme caption fills. Validate
  every stop in opaque roles, not only Start/End; native EDIT wells remain solid.
- Add shared opt-in state chrome through the existing appearance groups: separate
  hover/pressed/checked/checked-hover border ink, inner-rim ink and logical width
  (0..32, zero hides), plus inset (0..1024, clamped to paint bounds). Empty/null
  preserves existing recipes; Transparent hides ink. Explicit existing border
  overrides remain authoritative. The defaults add no new effect to 2013/2019/2024.
  Reserve paint space without changing live target bounds or state-dependent layout.
- Keep any further radial reflection separate from the linear fill contract.
  First compare native File/state captures with the supplied references; if a
  radial layer is required, define its independently adjustable parameters before
  adding it. A missing reflection remains a reported visual gap, not hidden fidelity.

F2 stops at reusable foundations and their independent consumer checks. It does
not expose a partially implemented 2010 theme, add a gallery tile or alter Backstage.

### F3: built-in shell, File and both sample galleries

- Append `RibbonBuiltInTheme.Office2010Blue = 3` and `RibbonThemes.Office2010Blue`;
  preserve existing enum values, application-theme Reset, custom themes and saved
  preferences. Runtime validation, design parsing and candidate previews agree.
- Compose the reference palette with Filled headers, the measured connected-tab
  helper, rounded radii, gold states and blue File. Add independent header
  hover/pressed border overrides rather than forcing their gold ink onto the
  selected blue-grey outline. State rims use F2's shared primitive and actual hooks.
- Add one immutable adjustable recipe for blue identity-surface accent adaptation;
  it preserves reference ramp positions/depth and explicit per-role overrides.
  Resetting accent/theme/caption remains independent. Preserve the primary OK
  versus secondary-button distinction and ordinary command text/icons.
- Add the 2010 tile to both existing View galleries as authored fields/collections,
  using public APIs and SVG constructors that survive designer serialization.
  Do not recreate diagnostic tabs. Normal launches keep the current startup choice.
- F3 is the ribbon/File shell review boundary. The new below-tabs Backstage is
  explicitly pending F4; do not call the complete family accepted at F3.

### F4: below-tabs Backstage sibling

- Append `RibbonBackstageStyle.Office2010 = 2`. Add sibling layout/drawing helpers
  under the existing pure bases, registered with their own explicit placement,
  caption and motion defaults. Palette and Backstage style stay independently
  selectable; 2019/2024 implementations are not the new family's base classes.
- Compute the pane top from the **live measured tab-header bottom**, transformed
  into host-client coordinates. Include integrated caption, QAT position, font,
  DPI, density, resizing and layout reflow; never add a fixed caption+tab constant.
  Cover body, lower QAT/messages and document while leaving the real caption and
  tab row visible. Clamp safely in tiny hosts and ordinary Form hosts.
- Keep the real File selected/open. Hide the redundant round Back target by
  default. File, selected tab and other tabs close Backstage; another tab also
  selects normally. Exposed QAT/caption actions keep existing input ownership.
  Suspend only the selected tab's visual connection/highlight while open; preserve
  logical selection and restore it on close. Include contextual/merged headers.
- Extend BackstageAppearance only for missing independent roles: nullable
  BackButtonVisible, TopSeamHeight (0..32, default 3 for 2010/0 for existing styles),
  TopSeamBackground, PageDividerShadowWidth (0..64) and shadow ink/opacity (finite
  0..1). Null uses family defaults; zero/Transparent hides the relevant effect.
  Existing navigation radius/border/fills, page padding and A10 width preference
  remain authoritative. The selected blue row uses square full-width geometry
  even though ordinary 2010 controls are rounded, as shown in the reference.
- Top seam follows the open File ramp's bottom color; page shadow is clipped to
  the navigation/page divider's left side, without a shadow across the top seam.
  Keep page text dark against white and preserve native controls/handles/parents,
  text/RTF, selection, validation and focus through page/pane open/close/reversal.
- Reuse the accepted clocks and safe native handoff. 2010 leaves the caption
  separate and does not acquire 2019's centered/moving full-height-pane title.

### F5: composed normal-theme review

Verify all delivered surfaces in both ordinary samples and a public-package
consumer with its own model and no sample resources/helpers/internal access.
Review before another family or any Aero work. The user owns final live visual
acceptance; automated print/native API evidence is reported separately.

## Verification and stop boundaries

Run only affected checks for each slice; retain E2–E5 evidence for unchanged
2013/2019/2024 behavior. F2 needs public API compatibility, gradient/value/atomic
validation and representative routed paint/opacity checks. Later slices add
focused state/split/header/caption, designer serialization, below-tabs native
input/lifecycle and public-package coverage. Preserve normal output paths.

Include default and alternate accents, explicit overrides/independent Reset,
alpha/Transparent/zero, 96/120/144/192 paint DPI, Classic/Simplified and Compact/Touch,
clipped/narrow/contextual/merged tabs and all three QAT placements where affected.
Build the normal Debug runtime/designer/client and refresh the persistent Designer
Sample only when its package needs updating. Inspect actual/diff images before
changing visual expectations. Do not run an unrelated passed matrix for repetition.

Separate focused tests, offscreen native/public-package checks, generated-code
round trips, user continuous-motion review, actual VS save/reopen, physical
keyboard/mouse/IME, native four-corner silhouette/DPI symmetry, desktop material
pixels and mixed-monitor movement. DWM preference readback is not corner acceptance.

F1 verification is limited to source/contract review, unchanged reference copies,
link targets and `git diff --check`. No build, runtime test, native capture or app
launch was needed for that documentation-only slice. **Next: F4 only.** Aero remains
deferred until normal Office 2010 has been implemented and reviewed.

## F2 implementation and verification — 2026-11-14

Entered on `codex/Ribbonkit-Winform` at `c7794b6` (`Tidy up testing and demos`)
with a clean worktree. The user subsequently committed F2 at `ec78e11`
(`Office 2010 F1,F2`). That slice exposed no Office 2010 preset, gallery tile or
Backstage sibling.

### Delivered contract

- `RibbonGradientStop` and immutable `RibbonGradientStops` copy their input,
  validate finite strictly increasing intermediate offsets inside (0,1), and
  compare/hash structurally. `RibbonFill.IntermediateStops` canonicalizes an
  empty collection to null. Existing constructors, Start/End, deconstruction and
  endpoint `with` edits remain available. Nullable `Direction` supports Vertical
  and Horizontal; null paints vertically, while an explicit Vertical is retained.
- `RibbonFillColors` evaluates/maps every stop. `RoundedPainter` caches the full
  value and uses the existing analytic corner coverage. Equal endpoints with a
  different middle are not treated as a solid. Alpha conversion, primary-button
  darkening and motion backgrounds retain positions, colors and direction.
- Nullable `BackgroundFill`, `TitleBarBackgroundFill`, `PopupBackgroundFill`,
  `QuickAccessBackgroundFill` and `BackstageNavigationBackgroundFill` own the five
  previously Color-only surfaces. Their Color setters clear the matching fill;
  Background also clears the lower-QAT fill. Null restores the previous fallback.
  Every stop must be opaque in these roles and BodyBackground. Invalid nested
  values fail before Theme/BuiltInTheme changes. Form.TitleBarColor still wins.
- Passive `RibbonPaintContext.BackgroundBounds` carries full-surface coordinates
  into clipped popup/Backstage rows and partial caption restoration. State fills
  keep their own coordinates. The caption and tab surround remain independent;
  native caption tint scales every alpha stop without changing title ownership.
- `RibbonStateChromeSettings` is optional on CommandAppearance,
  FileButtonAppearance, GalleryAppearance (tiles), EditorAppearance (choices) and
  OptionsAppearance (buttons). It supplies hover/pressed/checked/checked-hover
  outline ink plus inner-rim ink, width and inset. Existing explicit border ink/
  width wins. `EditorAppearance.ChoiceBorderWidth` independently controls choice
  state outlines. Transparent/zero hides; Empty/null falls back. Empty groups do
  not opt native buttons into custom paint. Rims composite without clearing the
  interior, clamp to tiny bounds and preserve joined edges and all hit targets.
- Existing first-stop fallback recipes, such as default caption-button fills and
  Options row/tree selections, stay unchanged. Explicit authored fills support
  the richer data. No layout engines, clocks, native frame/corner/backdrop policy,
  editor/popup owners or sample-authored model/resource files changed.

### Evidence and limits

- Normal Debug runtime, designer server, net472 client, Designer Sample and
  Showcase builds pass with zero warnings/errors. API analyzers pass; 76 additive
  accessor/type declarations are in Unshipped and Shipped is unchanged. Designer
  Sample is refreshed to package `0.1.0-designer.local20261114T112449399Z`.
- Final focused Release run: **85/85**, including **22 F2 cases** and affected
  command/editor/gallery/Options appearance checks. It covers copied values,
  equality, endpoint edits, bad offsets/enum/opaque middle stops, atomic failure,
  independent Reset/precedence, all-stop alpha/darkening, horizontal/clipped
  backgrounds, actual paint hooks at 96/120/144/192 DPI, SourceCopy rim safety,
  split/state outlines and native partial-caption restoration.
- An earlier affected-scope run passed 266/270, including unchanged connected-tab,
  title handoff, motion and 2024 pill cases. Its four gallery cases had a stale
  fixture: Hover was authored while CheckedHover was left to the independent live
  accent recipe. The fixture now explicitly authors its expected CheckedHover;
  the final focused run passes. No runtime state recipe or pixel approval changed.
  These overlapping runs are not a clean full-suite result or additive totals.
- [Independent public-package runner](../eng/Test-GradientFoundation.ps1) builds a
  consumer with its own model, isolated imports/cache and no internal/sample
  helpers. Against retained pre-F2 package `0.1.0-designer.local20261114T044002490Z`,
  **24 default/legacy two-stop image pairs match byte-for-byte**: all three existing
  themes, neutral/Office painters and 96/144 paint DPI, each exercising twelve
  public hooks. New public checks pass for immutable gradients, five surface roles,
  state ink, caption override/reset, application-theme Reset, atomic rejection and
  native editor handle/parent/value retention. Diagnostic captures were inspected;
  caption/header pixel comparison verifies that an override changes only its role.
- Retained evidence: `artifacts/gradient-foundation/gradient-complete.trx`,
  `gradient-focused.trx`, `public-complete/`, and the earlier baseline captures in
  `public-first/baseline/evidence/`. The existing separated sample-isolation check
  also passed during F2; verification code remains outside the normal sample.
- This is direct paint, offscreen native lifecycle and package/API evidence. It
  does not establish actual Office 2010 appearance, continuous visible motion,
  physical mouse/keyboard/IME, actual Visual Studio save/reopen, native four-corner
  silhouette, desktop material pixels or mixed-monitor acceptance. No unrelated
  E2–E5 matrix was repeated. Aero remains deferred.

F2 stopped before the shell. Its successor F3 is recorded below; below-tabs
Backstage remains F4 and composed normal-theme review remains F5.

## F3 implementation and verification — 2026-11-14

Entered with clean status at `ec78e11` on `codex/Ribbonkit-Winform`. F3 remains
uncommitted and entirely within `winforms/`; normal outputs and retained evidence
are preserved. No Aero or new Backstage style/placement was implemented.

### Delivered shell and public contract

- `RibbonBuiltInTheme.Office2010Blue = 3` and `RibbonThemes.Office2010Blue` append
  to the existing identities. The preset is composed from the neutral theme and
  existing Filled/connected-header, body, popup and state helpers. It does not
  inherit the 2019/2013 Backstage behavior. Runtime selection, application Reset,
  designer parsing/candidate previews, enum serialization and preferences agree.
- Caption/tab/body surfaces use the reference blue ramps with matching joins.
  The body and lower QAT remain full width with straight shared edges. Selected
  tabs use the measured open-bottom connection and immediate header feedback;
  ordinary chrome starts at radius 3 with smaller utility/focus radii. Native
  window policy requests Rounded through the existing adapter.
- Ordinary hover/pressed/checked/checked-hover retain the distinct reference gold
  ramps, outer inks and F2 inner rim. Active split halves get gold while the
  companion remains clear with one shared perimeter/divider. Selected gallery
  tiles and choices use those state roles. Popups/headings use pale blue surfaces;
  native field wells stay white. Primary OK/secondary button roles remain intact.
- `HeaderAppearance.FilledHoverBorderColor` and `FilledPressedBorderColor` affect
  unselected Filled tabs independently of the selected blue-grey outline. Empty
  falls back, Transparent hides, FilledBorderWidth zero hides the outline, and
  explicit existing FilledBorderColor wins. No state-dependent target/layout change.
- `RibbonIdentityAccentSettings` is one immutable, adjustable reference-ramp recipe
  exposed by `Theme.IdentityAccent`. It has independently adjustable FileStrength
  and CaptionStrength (finite 0..1), reference accent RGB, four File ramps, File
  border ink and a colored-caption ramp. `AdaptFill` rebases each color relative
  to the reference channel while preserving offsets, direction, alpha and depth;
  zero strength retains reference colors. `CreateCaptionFill` exposes the same
  library recipe to other applications. Invalid strengths/nonopaque caption stops
  fail before theme selection/application state changes.
- File's explicit role fills win, then IdentityAccent ramps, then the existing
  solid-accent/darkening fallback. `FileButtonAppearance.BorderColor` provides an
  independent idle outline, with state chrome and existing explicit header ink
  retaining precedence. Empty follows the identity/header fallback. The preset's
  default outline also follows accent; explicit ink stays authored.
  `SquareBottomCorners` preserves the rounded-top joined foot independently of
  HeaderFilledCornerRadius. Both File routes retain distinct idle/hover/pressed/
  open ramps; closing paints idle. Ordinary command text/icons do not follow accent.
- Both existing View galleries include an Office 2010 Blue choice. Designer Sample
  uses a normal authored field/item and an explicit two-argument SVG constructor.
  Its startup remains Office 2024; no review tabs or test dispatch were added to
  the normal sample. The shared sample preference adapter calls the public
  caption recipe for 2010's title toggle and keeps neutral tab/header colors.
  Turning it off restores the neutral caption; accent Default and Application
  theme Reset remain independent. Form.TitleBarColor retains library precedence.
- Designer choices use the existing string protocol, runtime-free net472 client,
  inert detached previews and framework transaction/Undo/Redo path. No new design
  protocol shape or theme-authoring coordinator was needed. Backstage still uses
  an existing presentation until F4; do not approve its current placement as 2010.

### Verification

- Normal Debug runtime, designer server/net472 client, both samples and separated
  sample test host build with zero warnings/errors. Initial F3 Designer Sample package:
  `0.1.0-designer.local20261114T115136488Z`. API analyzers pass with 41 additive
  entries in Unshipped; Shipped is unchanged. The last refresh updates XML docs
  after the implementation package used for the first sample captures.
- Fifteen new F3 cases are verified across focused Release runs: preset/enum/
  application and Form Reset, all-stop accent adaptation, invalid atomic writes,
  96/120/144/192 paint DPI, gold/blue state separation, split companion/perimeter,
  header/File override/Transparent/zero precedence, both native File routes,
  title toggle, inert candidate rendering and preference roundtrip. Existing
  designer enum serialization/Undo/Redo tests now include 2010. The actual authored
  SVG source is loaded, serialized and reloaded with its resource to protect the
  previously reported vector loss.
- Retained focused runs under `artifacts/office2010-f3/`: `f3-focused.trx` passed
  73/74 including designer/SVG and existing 2013 cases; its remaining new native
  fixture was corrected to measure the stable parent after first page adoption.
  `f3-native-fixed.trx` passes 18/18. `f3-frame-split.trx` passes 37/41 including
  connected-outline/header presentation cases; four new split probes sampled
  the arrow ink instead of clear companion backing. Corrected probes pass 4/4 in
  `f3-split-fixed.trx`; the additional outline Reset case passes 1/1 in
  `f3-outline-reset.trx`. The first File center-pixel fixture also crossed label
  ink; all corrected probes sample decoration away from content. No runtime
  fix, visual tolerance or approved pixel baseline changed for these fixture
  corrections. Runs overlap; they are not aggregate totals or a clean full suite.
- [Independent package runner](../eng/Test-Office2010Shell.ps1) passes against the
  current package with isolated imports/cache, its own model, and public APIs only.
  `public-complete/` retains four Classic/Simplified x Compact/Touch scopes, all
  three QAT positions, both File routes, source identity/native editor handle/
  parent/text/selection retention, Application Reset and atomic validation at
  actual 120 DPI. It references no sample/helper/internal code.
- `designer-final/` and `showcase-final/` pass the same affected native shell scopes
  against their actual authored models, plus their actual theme/accent/title
  actions and independent Reset. Showcase additionally passes actual startup
  persistence for 2010 + Purple + gradient/white caption, then independent
  caption/accent Reset. `sample-isolation/` confirms the compiled normal Designer
  Sample still contains zero test/check/profile/retired-demo types or dispatch.
- Captures were inspected against the supplied neutral overview and Add Message
  hover reference: neutral/colored shell, blue File states, gold commands/tiles,
  popup/choice backing and lower QAT. This implements linear reference ramps with
  the independently adjustable F2 rim. A separate radial File reflection is not
  implemented or claimed; its fidelity remains a visual-review limit, not Aero.
  The native checks use offscreen windows and direct paint, not desktop pixels.

### User review and stop boundary

In both normal samples, choose **View > Themes > Office 2010 Blue** (expand the
gallery if its tile is scrolled out of the inline strip), then:

1. Return to Home. Switch Home/Edit/View and check the blue band, immediate
   connected selected outline and full-width blue body/lower QAT. No whole-ribbon
   flash should appear. Resize normally and try the existing density/layout controls.
2. Hover/hold an ordinary button and each split half. Compare the gold hover,
   deeper pressed state, checked toggle and selected gallery tile. Text/artwork
   should retain ordinary ink; targets must stay still.
3. Hover/hold/open/close File. Check its four blue ramps, white text and rounded
   top/square foot; closing restores idle. Current Backstage placement remains
   the existing style and is outside this slice's appearance review.
4. Choose Purple or Teal and turn **Color title bar** on. File and caption should
   follow the accent with white title text; gold command states and the light blue
   tab band should remain. Turn it off, choose Default accent, then Application
   theme to check the three independent resets.
5. For actual Visual Studio review, reopen the refreshed Designer Sample solution.
   Inspect View's new authored tile; in Edit Ribbon select Office2010Blue, preview,
   Apply, Undo/Redo, save and reopen. Check the new and existing gallery SVGs remain.
   Framework serialization checks passed, but this actual VS gate was not run here.

User appearance/continuous-motion acceptance is pending. Actual VS, physical
mouse/keyboard/IME, native four-corner silhouette/DPI symmetry, desktop materials
and mixed-monitor movement are separate gates. Existing 2024 pill and closing
handoff implementations are untouched; no unrelated accepted matrix was rerun.
Stop after F3. Next: **F4 below-tabs Backstage sibling**; F5 composed normal-theme
review and any Aero experiment remain later work.

### F3 header review corrections and shared File minimum — 2026-11-14

The user described the shell as beautiful, then reported a button-like bottom
border on hovered tabs and a rectangular patch around the ribbon minimize button.
They also requested a general File minimum-width setting, with wider defaults
for all delivered families except Office 2024. This feedback is not acceptance
of the corrections before recheck and does not start F4.

- Office 2010 sets `HeaderAppearance.FilledTabBottomBorderVisible = false` for
  unselected hover/pressed states. Top/side outlines remain; top corners stay
  rounded and the foot is square/open. The existing disjoint-clip frame helper
  paints the last fill row once, retaining alpha and every gradient stop. Default
  true restores the closed outline in other/custom themes. The selected tab/body
  connection and File button remain independent; targets/layout are unchanged.
- The minimize slot had repainted `Theme.Background` as a solid, and the derived
  header-utility theme's Color setter had cleared its fill companion. Utilities
  now retain the fill and repaint the backing in the full header coordinates.
  TabRow QAT receives the same coordinates. Composited gradient restoration uses
  SourceCopy without changing the caller's mode, avoiding doubled tint alpha.
  State/glyph bounds and all caption/animation/input owners are unchanged.
- `Theme.FileButtonMinimumWidth` is a general nullable metric, 1..1024 logical
  pixels, used by the shared header measurement for every theme/presentation,
  Classic/Simplified and both File routes. Null uses the legacy 50 fallback;
  Touch retains at least 44, text/padding can demand more, and tiny-strip safety
  clamps remain. Office 2010/2013/2019 presets now use **64**; 2024 retains its
  previous value. Explicit overrides, null/group/application Reset and atomic
  validation are covered. Idle/hover/pressed/open use one measured target.
- Focused Release runs pass **28/28** (`header-review.trx`) and **65/65**
  (`header-layout.trx`) under `artifacts/office2010-header-review/`. The disjoint
  scopes include 13 new cases, actual top/side-only paint, alpha, vertical and
  horizontal gradient restoration at 96/120/144/192 paint DPI, all four themes'
  File overrides/reset/validation, layout/density/routes, narrow bounds, existing
  header utilities, selected outlines, clipping and chrome geometry. An added
  native QAT-placement assertion passes 1/1 in `header-qat-placements.trx`; it
  overlaps the earlier case and is not an extra aggregate test.
- `eng/Test-Office2010Shell.ps1 -HeaderOnly` passes the independent package scope
  using only public APIs and its own model. Retained `public-final/` evidence includes
  four-theme default/override captures, open hover/pressed tab captures and the
  continuous native minimize slot in expanded/minimized states. Actual captures
  were inspected against the reported patch. This is direct paint/offscreen
  native evidence at actual 120 DPI, not physical input/VS/material acceptance.
- Normal Debug runtime/server/net472 client and both sample builds pass; the
  persistent Designer Sample is refreshed. Four additive API accessor entries
  bring F3's additions to 45, with Shipped unchanged. Existing test separation,
  2024 underline/pill logic, native frame and Backstage closing handoff remain.
  Current package is recorded in the continuation prompt. No unrelated accepted
  composed, corner, material or input matrix was repeated.

Recheck the reported two header details and the wider File entry in both samples.
The complete family and actual VS/physical/native-silhouette gates remain pending.
Next is still F4 below-tabs Backstage; Aero remains deferred.
