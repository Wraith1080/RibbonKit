# Continue native WinForms themes after D1c editor appearance

Workspace:
`C:\Users\LENOVO\Claude\Projects\Professional Ribbon Custom Control for WPF`

Updated **2026-11-12**. B1–B3, C1–C2, D1a–D1c and adjustability backfills
A00–A04/A06–A09/A11–A16 are implemented. Current evidence and exact user review
scope live in the [themes ledger](THEMES-PLAN.md#current-status-and-review-ledger).
The latest implementation is [D1c](THEMES-PLAN.md#native-editor-appearance-d1c--2026-11-12).
D1c appearance review remains pending; use the ledger for earlier review scope.
The user committed D1b at `2a38995` before D1c.

## Next bounded implementation

Start with `git status --short` and inspect live branch/HEAD; preserve later user
commits and edits. Follow the current user request. For a continuation, D2
**ordinary popup-frame appearance** is next proposed: inspect existing popup
frame/shadow/clip/measurement roles and actual renderer hooks; choose a bounded
extraction using existing settings before adding immutable public settings.
Application Menu recipes and other later surfaces remain separate. Keep current defaults. Read the matching source map and rules in
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

**Review tabs:** the user requested a fresh tab for D1c. Put new controls on
**Theme effects 4** through `ThemeEffectsReviewTab` until directed otherwise.
`ThemeEffects2ReviewTab` / `ThemeEffects3ReviewTab` pin existing controls to their
original tabs. Earlier tabs retain seven/five/four groups; tab 4 has **Try text
fields**, **Try choices** and **Editor appearance effects**. Do not move old
groups or infer fullness from reduction.

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
avoid JIT dialogs. Separate offscreen/automated evidence from actual VS, visible
frame pacing, physical input/IME and mixed-monitor review.
