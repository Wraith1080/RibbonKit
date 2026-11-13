# Continue native WinForms themes after D8b caption-button decoration

Workspace:
`C:\Users\LENOVO\Claude\Projects\Professional Ribbon Custom Control for WPF`

Updated **2026-11-13**. B1–B3, C1–C2, D1a–D1c, D2–D3, D4a–D4c, D5a–D5b, D6, D7a–D7f, D8a–D8b and adjustability backfills
A00–A09/A11–A17 are implemented. Current evidence and exact user review
scope live in the [themes ledger](THEMES-PLAN.md#current-status-and-review-ledger).
The latest implementation is [D8b](THEMES-PLAN.md#caption-button-decoration-d8b--2026-11-13).
Preserve D8a's [closing handoff follow-up](THEMES-PLAN.md#closing-caption-handoff-follow-up--2026-11-13), which
unifies temporary/settled title glyph rendering and paints the normal caption
before foreground HWND retirement. Check its current review status in the ledger.
Use the ledger for the exact user appearance review scope and remaining gates.
D7f's square geometry is retained; current review scope is recorded in the ledger.
The user committed D8a and its closing correction at `ae34108` before D8b.
Do not overwrite later user commits.

## Next bounded implementation

Start with `git status --short` and inspect live branch/HEAD; preserve later user
commits and edits. Follow the current user request. For a continuation, **A10
preferred Backstage page minimum** is next proposed for source review. Inspect
the fixed 240-logical-pixel preference in Backstage metrics and the layout provider's
navigation-width negotiation. Define one optional theme preference before editing;
null retains 240. Preserve explicit/minimum/maximum/Auto navigation-width behavior,
available-space and half-host safety clamps, short/narrow hosts and native pages.
Select relevant width/layout/DPI/reset/native-retention checks after reviewing the
contract. This is a bounded layout preference, not a new geometry campaign.
Keep D8a's frame-matched readable opaque title ink, Office 2024 moving neutral tint,
full-height Office 2019 panes, clipping and captured pane/title clocks intact.
Preserve D8b's caption-button appearance contract and targets. No new theme family
or broad window/animation rewrite. Current user review belongs only in the ledger.
D7f owns group icon-picker tile drawing/metrics. Keep native ListView items,
handles, selection, cell spacing, scrolling, borrowed artwork, validation and draft
OK/Cancel with their existing owners.
D7e owns tree state/content/glyph drawing and decoration metrics; preserve native
expand/check/text targets, indentation/scrolling and actual checkbox hooks.
D7d owns QAT/Classic/Simplified command-list rows; preserve
its direct native text/focus contracts and shared content slots. Extract fixed
effects into independently adjustable settings with matching measurement.
D7c owns field/list wrapper paint and native clearance plus
search/closed-choice text slots. Do not cover native children with wider paint.
Preserve native editors, validation, keyboard/focus, draft Apply/Cancel, dialog
lifetime and inert candidate-theme preview safety. Application dialog content
stays application-owned. D7b owns Options buttons and heading typography. D7a
owns Options navigation and rail/footer dividers;
preserve its direct native text path and actual popup-radius hook. D6 owns shared
ScreenTip/KeyTip paint and A17 placement.
D5a owns gallery tile-state/headings and D5b content/inline frame/utilities;
preserve gallery source identity, preview/cancel/commit and native/custom presenter
ownership. Authoring cards remain ordinary editable UserControls.
D4a owns Application Menu state chrome; D4b owns surfaces/headings/clips; D4c owns
content/measurement; D2 owns the outer frame.
Preserve current defaults. Read the matching source map and rules in
[THEME-ARCHITECTURE-PLAN](THEME-ARCHITECTURE-PLAN.md). Planning-only requests do not
need builds, tests or package refresh.

**User requirement:** replace fixed visual effects in each extracted surface
with independently adjustable theme settings. Internal constants alone do not
satisfy this. Reuse existing colors/fills/radii/spacing first, preserve null/Empty
and independent/group Reset, validate before Theme/BuiltInTheme mutation, honor
authored alpha/zero/hide and retain actual application renderer precedence.
A new family selector needs an actual structural difference, not a palette name,
header style or radius. Native editor HWND, parent, value, selection, focus,
validation, IME and popup ownership remain with their existing shared owners.

**Review tabs:** the user requested a fresh tab for D8b. Put new controls on
**Theme effects 11** through `ThemeEffectsReviewTab` until directed otherwise.
`ThemeEffects2ReviewTab` / `ThemeEffects3ReviewTab` / `ThemeEffects4ReviewTab` /
`ThemeEffects5ReviewTab` / `ThemeEffects6ReviewTab` / `ThemeEffects7ReviewTab` /
`ThemeEffects8ReviewTab` / `ThemeEffects9ReviewTab` / `ThemeEffects10ReviewTab` pin existing groups to their original tabs.
Earlier tabs retain seven/five/four/seven/six/three/eight/five/six groups. Tab 6 retains **Try built-in
galleries / Gallery tile states / Gallery headings**. Tab 7 has **Try gallery content /
Gallery artwork and labels / Gallery inline frames / Gallery utility glyphs**.
Tab 7 now also has **Try tips and badges / Tip presentation** (six total groups).
**Try Options dialogs / Options navigation** brings tab 7 to eight groups. Tab 8
has **Try Options shell / Options buttons / Options headings / Try frames and choices /
Options frames and text**, five total groups. Tab 9 has **Try command rows /
Command row content / Command row selection / Try customization tree / Tree glyphs /
Tree selection and focus**, six total groups. Tab 10 has **Try group icon picker /
Icon picker selection / Icon picker content / Backstage caption tint / Try caption and title**,
five total groups. Tab 11 has **Try caption buttons / Caption button fills /
Caption button shape / Caption button glyphs**, four total groups. Keep tab 7/8/9/10's
accepted groups in place; do not infer fullness from reduction.
The File preview uses existing authored models and restores the prior File mode.
Its divider-visibility button uses the existing independent flag (2019 defaults
false); group Reset clears only the new settings. The earlier optional overflow
preview still restores its original toolbar items/settings. Use tab/body scrollers
when needed. Do not move old groups or infer fullness from reduction.

## Preserve delivered contracts

- Geometry consolidation remains the foundation: shared metrics, snapshots,
  DPI/rounding, clips, targets, native content bounds and closed geometry results.
  Do not replace layout engines or reopen the geometry campaign.
- B1–B3 retain sibling Backstage registration, measured inputs and captured
  caption/motion profiles. Keep opaque native ownership, whole-pane motion,
  coupled pane/title durations, independent page Fade/SlideDown, authored fill
  precedence and zero-duration/reduced-motion behavior.
- C1 retains sibling header presentation, existing content/strip reservations,
  actual text/focus/indicator hooks and adjustable header/shared effects.
- C2 retains resolved body frames, group separators, joins, full-width 2019 body,
  existing margins/shadows, native identities and popup anchors.
- D1a retains `CommandAppearance`, shared split perimeters/half fills/separators,
  active-half and material fallback precedence and actual chrome hooks.
- D1b retains `CommandContent`, shared check/radio/QAT-mixed/raster drawing and
  filled-triangle dimensions. Null is one-device-pixel indicator borders,
  canonical centered marks/dots, 8 × 6 triangles and established raster recipes.
  Explicit opacity/grayscale settings cover independently disabled QAT references
  as well as disabled sources; enabled/vector images retain their owners. Actual
  public indicator/arrow and protected image hooks remain authoritative.
- D1c retains `EditorAppearance`, shared field/choice drawing and resolved
  chrome defaults. Existing editor colors, state fills, glyph/geometry roles and
  actual field/choice/chrome/text/arrow hooks remain authoritative. Field and
  choice corners/insets reset independently; native opaque input bounds, native
  selected-value paint, editor manager, binding/validation and ownership remain
  unchanged. Frame decoration does not add native input padding. Default paint
  has 64 exact image/geometry pairs; do not repeat that passed batch for no change.
- D2 retains shared popup frame/shadow defaults and `PopupAppearance` border
  width, horizontal shadow offset and Gaussian falloff. Actual public frame/
  shadow/radius/clearance hooks remain authoritative. `GetPopupBorderWidth`
  supplies device pixels to frame paint, shell/host padding, hosted corner masks,
  menu/custom-body sizing, choice fitting and passive tip clearance. Zero hides
  border paint but keeps one-device-pixel content clearance; authored widths
  reserve space and clamp to available work areas. Null keeps all old rounding.
  Existing colors/radius/shadow enable/opacity/blur/vertical offset are reused.
  Choice width also consumes D1c horizontal text padding. Shadow falloff is in
  the bounded shared painter cache key; body shadows retain their own default.
  No native activation/stack/ownership or animation-clock rewrite was performed.
- D3 retains `MenuAppearance` and shared heading/separator/overflow-band drawing.
  Header shape/weight, centered separator ink/thickness and opaque band backing/
  active shape reset independently. Existing fills, spacing and Chevrons remain
  in use. DrawMenuHeader still reaches the actual DrawGalleryHeading override;
  chrome/text/separator/band hooks keep precedence. Real gallery headings,
  Application Menu and scoped Backstage separators retain their old recipes.
  New decoration does not change rows, bands, UIA or native owner state. Band
  backing stops must stay opaque. The default comparison has 80 exact pairs;
  reuse it unless the next change actually affects those owners.
- D4a retains `ApplicationMenuAppearance`, shared row/footer perimeter and clipped
  split-half recipes. Null keeps square rows, device-pixel insets, zero row border,
  the one-device-pixel footer rim (even for transparent ink), legacy Pen divider
  and 0.25 selected-primary mix. Existing state fills, footer radius and divider
  visibility remain authoritative. Explicit primary fill takes precedence over
  tint; alpha/zero/hide, clamps and independent/group Reset remain supported.
  Actual item/split/text/image/arrow/focus hooks remain; the pre-existing direct
  primitive path does not introduce protected DrawChrome interception. Native
  ownership/layout/input/validation stay shared. Reuse the 32 exact default pairs
  and 41 distinct passing latest relevant cases unless a change affects them.
- D4b retains shared background/frame/divider/heading paint and resolved inner
  clips. Null preserves the 0.125 opaque pane/surround mix, actual popup radius
  minus one device pixel, one-device-pixel frame/divider/rule, square heading fill,
  host styles plus Bold and 2/10 logical-pixel header text insets. Existing heading
  fills/ink remain authoritative. Authored alpha composites over opaque native
  backing; explicit zero/hide, clamps and independent/group resets remain.
  Border/radius settings affect paint clips, not targets or native content bounds.
  Actual background/header/text/radius hooks remain. Reuse 32 exact PNGs and 37
  relevant passing cases unless affected. New surface/heading sample resets keep
  the other group and D4a roles; whole-group Reset still clears the entire record.
- D4c retains `ApplicationMenuContent` and shared content measurement/drawing.
  Null keeps 32/16 icons, 8/6 icon insets, 8 gap, 52 large-text/6 plain-text inset,
  6-pixel paint/8-pixel measurement trailing allowances, total row/icon padding
  10/8, description gap/inset/lines 2/4/2, old centered footer widths, Bold rich
  titles, 26/44 arrow columns and 9/8 separator row/inset with a device-pixel line.
  Authored metrics feed row/footer/nav measurement, split targets and paint;
  preserve Touch minima, clamps, null rounding and native field frame owners.
  Actual image/indicator/text/arrow/focus/separator hooks remain authoritative.
  Live separator DrawItem calls must carry ApplicationMenuPart; ordinary-menu
  context bypasses component settings. D1b glyph/raster policies remain shared.
  Reuse 64 exact default pairs and 39 distinct passing latest relevant cases
  unless affected. Description member Reset keeps icon/arrow roles; whole content
  Reset preserves Appearance. Native body bounds are compared after actual
  autoscaling, and borrowed raster assets must outlive the ribbon.
- D5a retains `GalleryAppearance`, shared built-in tile-state/heading decoration
  and actual protected DrawChrome precedence. Null keeps actual primitive radii,
  device-pixel inset/stroke when ink exists, selected Accent/otherwise transparent
  tile ink, neutral no-fill/SecondaryText headings and Office GalleryHeadingBackground/
  Foreground headings. HeaderBold null adds Bold; false removes it while preserving
  other styles. Explicit fill/ink retains alpha; zero/hide, clamps and independent/
  group Reset remain. State fills/eligibility, content/focus/target geometry and
  custom/native presenter ownership remain shared. Ordinary menu heading forwarding
  stays separate. Reuse 64 exact default pairs and 26 relevant passing cases unless
  affected. Package checks retain the same custom presenter/editor and exercise
  active preview/cancel/commit sequences without claiming physical input.
- D5b retains `GalleryContent`, shared built-in artwork/label/glyph paint and popup
  minimum-height inputs. Null retains two MiddleCenter label lines, legacy 12 paint /
  24 measure reserve plus triple TilePadding, horizontal/top TilePadding + 4 / + 2,
  expand slot/bar/inset/stroke 18/10/4/rounded 2. Explicit lines/alignment/reserve/
  insets and glyph dimensions/ink support alpha/zero/hide/clamps and independent
  reset. GalleryAppearance strip/utility roles retain actual primitive defaults
  and native bounds; frame reset preserves tile/headings. Actual hooks and D1b
  raster policy remain shared. Reuse 32 exact default pairs and 45 relevant latest
  passing cases unless affected. Package checks retain the same native editor and
  source selection, restored popup geometry and unchanged inline targets.
- D6 retains `TipAppearance`, shared ScreenTip/KeyTip recipes and resolved tip
  defaults. TitleBold null adds Bold; false removes it for both measurement and
  paint, preserving other styles. Badge inset/stroke null is 0/one device pixel;
  existing palette/Office corners/padding remain independent. A17 right/width/
  height clearances null retain 4/8/4 logical pixels and legacy fractional rounding.
  Alpha/zero/hide/clamps and independent/group Reset remain. Public tip/text hooks,
  native ownership and keyboard routes stay authoritative; no protected chrome
  hook was introduced where the original badge path used a direct primitive.
  Reuse 32 exact defaults, 31 relevant passing cases and four offscreen package
  scopes unless affected. Real passive window focus/ownership was checked in STA;
  the package fixture calls public ScreenTip rendering, without a physical-hover claim.
- D7a retains `OptionsAppearance`, shared navigation/divider recipes and resolved
  Options metrics. Null preserves actual popup-radius fallback, ceiling-scaled
  selection/text/marker values 2/12/3/7 with summed 14 height deduction, device-pixel
  focus inset/divider line, Checked selection, Accent marker/text and Border rules.
  Authored shape/fill/insets/marker/divider values support alpha/zero/hide/clamps
  and independent/group Reset. Native direct text painting and existing marker
  corners/palette stay in use; no new command/text interception was introduced.
  Reuse 16 exact native default PNG/geometry pairs at actual 120 DPI, authored and
  legacy-rounding checks at 96/120/144/192 DPI, 32 distinct latest passing cases and
  four package scopes unless affected. The retained choice fixture failed in the
  initial batch and isolated retry; it now pumps initial owner work and establishes
  focus before opening, and passes in its final isolated scope. No popup runtime
  change was made; do not represent this as a clean initial aggregate run.
- D7b retains shared native button painting and heading font/padding metrics in
  OptionsAppearance. Null keeps button inset 0, ordinary rim one device pixel,
  accent rim 0 and focus inset four device pixels. Existing button corners,
  state palettes and direct native image/text/focus paths remain. Only scoped
  ordinary-button overrides opt a null-corner ordinary button into shared paint;
  accent-only fill/navigation/heading values retain its native fallback.
  Heading increment/minimum/gap null retain 5/14 points and 12 logical pixels;
  null style is legacy Bold-only, explicit weight preserves other host styles.
  Headings resolve during existing construction/load/font/DPI layout; choose
  settings before opening. No live theme coordinator or button/input rewrite.
  Partial button/heading resets retain navigation and each other. Reuse 16 exact
  native default PNG/geometry pairs at actual 120 DPI, four-DPI authored paint,
  23 distinct passing cases and four package scopes unless affected.
- D7c retains OptionsAppearance FrameCornerRadius/FrameBorderWidth/FrameBorderColor
  and shared native search/closed-choice metrics. Null uses the actual popup/command
  radius minimum, one-device-pixel frame stroke, legacy search rounding and six-logical-
  pixel choice inset. Authored strokes reserve real native padding and search/name
  height; zero hides paint with minimum one-device-pixel clearance. Alpha/Transparent
  retain space. Reset restores scaled native padding. Search/choice text reuses
  EditorTextInset/EditorForeground; closed-choice value clearance also uses existing
  EditorAppearance field inset/stroke. Native handles/parent/value/selection/drafts,
  actual field/text/arrow hooks, disabled native ink and staged choice lifecycle remain.
  Choose presets before opening; existing layout/font/DPI refresh applies geometry.
  Reuse 32 exact native default PNG/geometry/region/text-slot pairs, 28 distinct latest
  relevant cases and four package scopes unless affected. Its four-case native theory
  supersedes the two older mode-only display names; do not double-count those results.
- D7d retains shared command-list row drawing/slots and six OptionsAppearance row
  members. Null keeps QAT/Classic padding/gap 6, Simplified 8, icon 20 with ceiling
  rounding, square solid Checked.Start selection and Bold-only Simplified headings.
  Authored content clamps inside unchanged native rows; zero icon size reclaims
  its slot/gap. Alpha/gradient/Transparent selection composites over Surface; native
  Foreground/focus and renderer-independent direct text retain their contracts.
  Explicit row heading weight preserves other host styles through existing owned
  construction/font/DPI fonts; choose before opening. Content/selection/all-row
  resets preserve earlier frames/buttons/navigation and each other. Tree/icon-picker
  targets/slots/decorations are separate. Reuse 16 exact native default row PNG/
  geometry/icon-slot pairs, 24 passing relevant cases and four package scopes unless affected.
- D7e retains shared tree painting and pure decoration slots. Six OptionsAppearance
  tree members control selection corners/fill, check/icon sizes, icon gap and native
  focus inset; shared Chevrons controls expander length/depth/stroke. Null keeps
  square solid Checked.Start selection, checks 18/24, icon 16/gap 2, one-device-pixel
  focus inset and expander lengths 12/16 with legacy half-depth/strokes 1.6/1.8.
  Authored paint fits existing slots; zero hides check/icon/chevron paint while
  native targets/Space/expansion/scrolling remain. Visible checks use the actual
  indicator hook/context; native text/disabled ink and focus cue policy remain.
  Tree glyph/selection/shared-chevron resets retain each other and earlier roles.
  Reuse 16 exact native default PNG/geometry/hook pairs, 28 distinct latest passing
  cases and four package scopes unless affected. Initial fixture corrections and
  the separately passing native buffer case are recorded in D7e; do not describe
  that evidence as a clean initial aggregate. Initialize native visual-style
  fixtures before creating controls; do not change runtime behavior to satisfy probes.
- D7f square follow-up retains seven appearance members but intentionally revises
  fixed-cell/content-independent-inset behavior. Native hidden-label style and
  measured icon margins give None/artwork shared square icon hit/paint/focus cells.
  Positive artwork size resizes the cells with font/density minima and a native
  256-device-pixel cap; zero hides artwork with the default footprint. Selection
  inset/outline clearance constrains both artwork and None; extra text inset is
  applied inside that region. Native names/tooltips/items/handles/selection,
  scrolling, group validation and draft OK/Cancel remain. Use the current square
  follow-up's 20 latest passing cases and four package scopes; the original 16
  byte comparisons are historical because the intended default geometry changed.
  Use native icon rectangles for geometry/hit tests, not full label metadata
  rectangles. Queue native mouse down/up together; synchronous down can block in
  native drag detection. Do not change runtime input behavior to satisfy probes.
- Existing Theme assignment closes ordinary menus through `ModelChanged`;
  retained models reopen normally. Do not change lifecycle to satisfy a probe.
- D8a implements A05 `CaptionTintEnabled`/`CaptionTintColor`/`CaptionTintOpacity`
  in BackstageAppearance. Preserve Form/theme caption-color precedence and
  full-height pane ownership; alpha/zero/Transparent and independent Reset remain.
  Closing ink uses the same frame's fade coverage without fading glyphs or
  changing clips/clocks. Reuse the handoff follow-up's 50 latest passing focused
  cases, exact active/inactive title coverage, ordered/repeated handback checks
  and four package scopes unless affected. Original stationary byte comparisons
  are historical for solid title smoothing, which intentionally matches the plane
  now. Keep native owners painted before layer disposal and detach its slot before
  synchronous repaint for safe reentry. The record distinguishes fixture failures from
  latest passing scopes, actual transparent-plane frames from estimated underlays,
  and native timer/owner-paint checks from visible/DWM/physical acceptance.
  Reject screen captures unless the owner is confirmed foreground; all 16 screen
  samples were skipped in the latest guarded run. Do not infer visible acceptance.
- D8b uses CaptionButtonAppearance's 11 nullable/Empty members for fill/shape/
  glyph decoration. Preserve solid first-stop palette fallbacks, material close
  alpha 210/235, default 10-logical-pixel vectors and the legacy three-device-pixel
  restore offset. Authored metrics scale/clamp within native targets. Restore
  cutouts use the owner background callback and refill the same gradient origin
  once. Glyph zero/stroke zero/Transparent and independent/group Reset remain.
  Reuse 41 relevant passing cases, 72 byte-exact native default pairs, four-DPI
  authored checks and four package scopes unless affected. Material DrawToBitmap
  may flatten alpha: exact coverage is established by runtime DIB checks. Keep
  the older CaptionButtonCheck/--verify-caption-buttons intact; D8b diagnostics
  are CaptionButtonAppearanceCheck/--verify-caption-button-appearance.
- A10 preferred page minimum is proposed next. No new theme family now;
  later light-theme order is unscheduled and dark themes stay deferred.

Use one agent under `winforms/`; WPF stays read-only. Normal Debug/Release outputs
and the permanent Designer Sample only; preserve authored Forms/UserControls/
resources/project files. Refresh in place using `eng/Update-DesignerSample.ps1`.
No computer-use tools, commits, pushes, publication, branch switches or cleanup.

The user requested **relevant tests only**. Reuse previous passing evidence;
select only the next surface's paint/geometry/native-state/ownership regressions,
an unchanged-default comparison and a public-package consumer scope where needed.
Keep Classic/Simplified × Compact/Touch where affected. Do not restart the full
suite/native matrix. Log native callback failures outside paint callbacks to
avoid JIT dialogs. Package probes use the supported keyboard route for QAT
overflow, escape prior KeyTip hints before F10/0, and close through library
keyboard/model routes rather than ToolStrip.Close on its internal window. Ordinary
popup MSAA GetChildCount may be -1; do not treat it as UIA child enumeration.
The Application Menu accessible root supports its own command children; the D4a
package probe exercises primary/footer actions there. Its offscreen host is
activated before opening, and state-paint assertions respect idle eligibility.
The D4b package probe pumps pending host work before opening, then checks
synchronous model/paint/native state before normal queued foreground-loss
dismissal; it claims no foreground-input acceptance and changes no runtime lifecycle.
Separate offscreen/automated evidence from actual VS, visible
frame pacing, physical input/IME and mixed-monitor review.
