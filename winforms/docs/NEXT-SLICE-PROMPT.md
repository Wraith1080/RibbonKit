# Starting prompt: native RibbonKit.WinForms geometry consolidation

Execution checkpoint, **2026-11-04**: the seventh batch is accepted and committed
at `c695fbc51596e66f3f6c29bcf77d59abfc9684f2` on `codex/Ribbonkit-Winform`.
The user then authorized related batches together: the eighth (Small/Medium
command-content padding) and ninth (Application Menu spacing) are implemented
and verified, with **one combined user review pending**. The reported Roomier
Application Menu navigation/title/divider gap is corrected in the same review
scope; use the latest refreshed sample and the plan's correction evidence.
[The geometry plan](GEOMETRY-CONSOLIDATION-PLAN.md) owns both contracts, evidence,
review checklist and the provisional 6–8 remaining implementation-batch estimate.
Going forward, select 2–3 coherent batches per session when appropriate, present
all selected API/layout/designer contracts upfront, preserve accepted behavior,
verify the combined scope and refresh the permanent sample once. Stop for that
review before more batches or themes. This supersedes the original one-batch
session boundary below; the other scope and preservation rules still apply.
Visual Studio was closed by the user; do not open/control it. No computer use.
The original bounded starting contract follows for context.

Continue work in:

`C:\Users\LENOVO\Claude\Projects\Professional Ribbon Custom Control for WPF`

Prepared **2026-11-03**. Start the next bounded **native geometry consolidation**
slice after the accepted Application Menu. Inventory the existing geometry,
present a concrete native API/layout/designer contract before implementation,
then complete a coherent first implementation and appropriate verification.
Stop for user review before another geometry increment or additional themes.
Do not stop at a general roadmap, and do not turn the inventory into permission
for an unbounded rewrite.

## Live baseline and boundaries

- Start with `git status --short` and verify branch/HEAD. At preparation:
  **`codex/Ribbonkit-Winform`**, HEAD
  **`9dd0054b9ebe4f6f9ff5b57bf75198e443f5b4e5`**
  (`Application Menu Implementation`), **clean before this documentation handoff**.
  This handoff updates only `NEXT-SLICE-PROMPT.md`, `APPLICATION-MENU-PLAN.md` and
  `M4I2-APPEARANCE-INVENTORY.md` under `winforms/docs/`; those changes may still be
  uncommitted. The user committed the preceding implementation and accepted its
  appearance/interaction. Recheck
  live state; preserve all later tracked/untracked edits and commits.
- Read root `AGENTS.md` and `winforms/AGENTS.md`. Use one agent. No commit, push,
  publish, branch switch, stash/reset/clean or disposal of existing work.
- Implement only under `winforms/`. WPF is a read-only dimension/behavior reference;
  do not modify/build/test WPF or add WPF hosting/dependencies. Native font
  measurement, DPI, Touch and usability take precedence over copying WPF values.
- Use normal Debug/default outputs for review and normal Release outputs for
  required Release validation. Refresh the permanent Designer Sample in place
  with `winforms/eng/Update-DesignerSample.ps1`; preserve authored Forms,
  UserControls, resources and the selected startup File presentation. No new
  user review app. An isolated package-only verification consumer is appropriate.
- Reusable measurement, painting, input, clipping, scrolling, popup placement,
  editor hosting, keyboard/KeyTips and accessibility stay in the library.
  Applications own data, commands, icons, native page/document layout,
  persistence, activation/capture and platform integration that requires a host.

## Sequence and preserved behavior

Milestone sequence remains M6A motion -> M6B window integration/TitleBar QAT ->
M6C backdrops -> deferred M4I2 appearance consolidation. Shared color/fill roles,
the state-command popup correction and Application Menu precede this geometry
slice. Additional built-in themes and final visual polish come later.

Preserve the accepted behavior and current default dimensions:

- Application Menu: entering a navigation dropdown or either split half selects
  its right pane without invoking. A plain command restores DefaultPane. Gaps,
  separators, disabled rows and pane/footer transit retain selection. Reopen
  starts on DefaultPane. Both split halves share normal hover fill; selected and
  unhovered, the main half is lighter than the arrow. Keep shaded active-pane
  title bands, the inset default heading, continuous inner border and matching
  outer/default-pane background. Preserve validation vetoes, command order,
  retained native content/HWND/text and navigation/footer scroll offsets.
- State-command popups: accepted check/radio/toggle and split activation,
  state-before-Click/one invocation, dismissal and retained-content policies.
- Gallery UserControl cards/presenter, inline/QAT ownership and retention,
  native editor/ComboBox behavior, scroll reveal and generated designer examples.
- File alignment/anchoring, body/QAT/message joins, Classic/Simplified and
  Compact/Touch behavior, TitleBar/TabRow/BelowRibbon QAT identity, independent
  motion markers, whole-surface Backstage/caption motion and material startup.
- Retained opaque Backstage designer preview, public renderer hooks, owned model
  identities, authored handlers/bindings, serialization and shipped APIs.
  A consolidation must not silently redesign these accepted surfaces.

## Bounded reading and inventory

Read current contracts, not every historical ledger:

- `winforms/docs/M4I2-APPEARANCE-INVENTORY.md` and
  `winforms/docs/APPEARANCE-COLOR-ROLES.md` for geometry gaps and shared roles.
- `winforms/docs/APPLICATION-MENU-PLAN.md` current status and WPF-hover follow-up;
  `winforms/docs/STATE-COMMAND-POPUPS.md` for accepted activation policy.
- Relevant geometry/theme/density/renderer/designer sections of `winforms/README.md`;
  `docs/21-NATIVE-WINFORMS-PLAN.md` deferred-theming and DPI contracts.
- `04-DESIGN-NOTES.md` section 5 and matching subsystem/history pointers as needed;
  do not reopen unrelated WPF work or treat historical test counts as new evidence.

Trace existing owners before proposing settings: `RibbonTheme`,
`RibbonDensityMetrics`, core/header/content/Simplified layout, split/editor/menu/
Application Menu/gallery/QAT/message/Backstage/caption layout, shared and Office
renderers, popup windows/chrome, motion snapshots, editor hosting and designer
previews. Follow the source owners relevant to each proposed metric rather than
loading all implementations at once.

Produce a compact inventory by surface: existing public setting, current
Compact/Touch default and units, current owner(s), every geometry consumer,
consolidation candidate and deferred/platform rule. Include spacing/padding,
icon/text/arrow slots, row heights, radii/border sides, indicator/scroll geometry
and shadow clearance. Distinguish 96-DPI logical metrics from device-pixel
hairlines/raster coverage, native system metrics and host-owned content. Equal
numeric literals do not by themselves justify sharing one setting.

## Contract before edits, then bounded implementation

1. Present a concrete first set of geometry roles and its exact affected surfaces,
   defaults, fallback/override rules and non-goals. Audit broadly, but implement
   one cohesive initial set that can be reviewed and verified independently.
   Explicitly list deferred roles; do not claim complete consolidation if they
   remain. Create/update `winforms/docs/GEOMETRY-CONSOLIDATION-PLAN.md` to own the
   selected contract, implementation checklist, evidence and current review gate.
2. Propose the smallest per-ribbon immutable configuration/resolution contract.
   Reuse existing public theme/density properties and renderer extension points;
   preserve authored values, minimum-height meanings and unset/reset behavior.
   Explain any additive public API before editing it; include XML documentation,
   validation and PublicAPI bookkeeping. Do not silently rename/remove settings,
   add a global coordinator or expose every rendering constant as a property.
3. Define units, scaling/rounding once, font/content-driven minima, Compact/Touch
   fallbacks, invalid-value handling and behavior on live theme/density/DPI changes.
   Paint, layout, hit targets, native editor slots, keyboard/UIA bounds, scrolling,
   popup placement/regions and motion endpoints must consume the same resolved
   measurements. Radius/shadow changes require matching clipping, raster/cache
   keys and reserved drawing space, not a paint-only adjustment.
4. State the designer contract explicitly: how values are authored, defaults/reset
   and serialization, inert preview behavior, ownership and Undo/Redo if affected.
   Do not silently build a broad theme-authoring UI. If code-only configuration
   is the bounded contract, say so and preserve existing designer round-trips.
5. Implement the presented first set through shared library code and a small,
   public-API demonstration in the permanent sample. Keep defaults visually and
   behaviorally equivalent; demonstrate meaningful non-default values and reset.
   Avoid consumer-only offsets, duplicate geometry formulas, layout animation or
   replacement/recreation of native controls as a measurement workaround.

Resolve routine choices within this scope without repeated approval. Ask only
when missing information materially changes scope, compatibility or correctness,
and continue independent work while waiting.

## Verification and review boundary

- Establish baseline measurements/captures for affected surfaces before changing
  them. Keep a requested-change checklist and compare actual painted bounds and
  all four frame corners; default-parity and intentional override evidence are
  separate. Inspect actual/diff captures before changing expectations/tolerances.
- Run proportionate focused native regressions for the selected roles: defaults,
  overrides/reset, density/font/DPI scaling, invalidation/cache changes, actual
  paint/hit/editor/keyboard/UIA agreement, scroll/work-area constraints and any
  affected popup/lifetime/motion/designer serialization contracts. Include
  representative synthetic 96/120/144/192 DPI without calling it hardware coverage.
- Build native Debug/Release, satisfy applicable API/package checks and refresh
  the permanent sample. Verify a package-only consumer without Showcase helpers
  or internals, both renderers and relevant material/density/layout presentations.
  Run foreground native fixtures sequentially; stop input on focus loss.
- Reuse passed evidence. Broaden/repeat only for a new change, failure, unresolved
  risk or explicit gate. A broad API/cross-project change may require the native
  full suite; a bounded change defaults to focused checks. Never run WPF checks
  as evidence for this task.
- Starting evidence is recorded, not freshly rerun: Application Menu's focused
  Release scope passed 85/85 and its package consumer passed 12/12 actual-120-DPI
  scopes. The prior broader Release aggregate still had six failures; do not
  assume a clean suite or silently fix unrelated group/Backstage behavior.
  Actual VS, external accessibility, physical touch/IME/RTL and genuine
  multi-monitor DPI gates remain distinct from synthetic/fixture evidence.
- Report the exact implemented roles, default/override results, native/package
  checks, deferred geometry and remaining limits. Stop for user review. Do not
  start additional geometry increments or themes as part of this slice.
