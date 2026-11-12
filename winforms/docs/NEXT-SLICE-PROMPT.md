# Continue native WinForms themes after D4b Application Menu surfaces

Workspace:
`C:\Users\LENOVO\Claude\Projects\Professional Ribbon Custom Control for WPF`

Updated **2026-11-12**. B1–B3, C1–C2, D1a–D1c, D2–D3, D4a–D4b and adjustability backfills
A00–A04/A06–A09/A11–A16 are implemented. Current evidence and exact user review
scope live in the [themes ledger](THEMES-PLAN.md#current-status-and-review-ledger).
The latest implementation is [D4b](THEMES-PLAN.md#application-menu-backgrounds-inner-frame-and-headings-d4b--2026-11-12).
Use the ledger for the exact user appearance review scope and remaining gates.
The user committed D4a at `49329fe` before D4b. Do not overwrite later user commits.

## Next bounded implementation

Start with `git status --short` and inspect live branch/HEAD; preserve later user
commits and edits. Follow the current user request. For a continuation, D4c
**Application Menu content presentation** is next proposed: inspect fixed icon/
text/description metrics and rich-title weight in DrawApplicationMenuItem and
matching RowHeight/Footer measurement. Reuse existing row/pane spacing, D1b
image/indicator/arrow roles, actual renderer hooks and inert designer previews.
Preserve defaults, native bounds and the reviewed File/Backstage contract; use one
resolved metric owner for measurement and paint where these values are shared.
D4a owns command state chrome; D4b owns surface/headings/clips; D2 owns the outer frame;
ordinary menu decoration, galleries, tips and library dialogs retain their owners.
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

**Review tabs:** the user requested a fresh tab for D4a. Put new controls on
**Theme effects 5** through `ThemeEffectsReviewTab` until directed otherwise.
`ThemeEffects2ReviewTab` / `ThemeEffects3ReviewTab` / `ThemeEffects4ReviewTab` pin
existing groups to their original tabs. Earlier tabs retain seven/five/four/seven
groups; tab 5 has four: **Try Application Menu / Application Menu state effects /
Application Menu surfaces / Application Menu headings**.
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
- Existing Theme assignment closes ordinary menus through `ModelChanged`;
  retained models reopen normally. Do not change lifecycle to satisfy a probe.
- A05 caption tint, A10 preferred page minimum, A17 tip placement and the reported
  Backstage closing-title ink flicker remain separate. The latter is deferred to
  title work, with recording/timestamps in the ledger. No new theme family now;
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
