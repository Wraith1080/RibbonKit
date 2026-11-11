# Continue native WinForms theme architecture after B3

Workspace:
`C:\Users\LENOVO\Claude\Projects\Professional Ribbon Custom Control for WPF`

Updated **2026-11-11**. **B1–B3 Backstage extraction is implemented.** One
registration pairs sibling layout/drawing helpers with defaults and caption/
motion profiles. Layout consumes measured inputs; existing engines execute
resolved motion policy and retain their clocks/lifecycle. Current evidence and
user review are recorded once in
[THEMES-PLAN](THEMES-PLAN.md#backstage-caption-and-motion-policy-b3--2026-11-11).
The subsequent navbar blank-frame/rounded-edge correction and the user's deferred
closing-title issue are tracked in its
[current review ledger](THEMES-PLAN.md#current-status-and-review-ledger).

## Establish scope from the user's current request

- Start with `git status --short` and inspect live branch/HEAD. The Backstage
  review follow-up began clean on `codex/Ribbonkit-Winform` at `bd9b4a9`
  (`B3 Rewrite`); the follow-up changes remain uncommitted. Preserve later user
  edits and committed fixes.
- Read [THEME-ARCHITECTURE-PLAN](THEME-ARCHITECTURE-PLAN.md), especially C1 and
  the ownership/compatibility rules. **C1 header presentation** is next. Keep
  Backstage B1–B3 intact; body/connected-frame work belongs to C2 and further
  surfaces/themes remain separate.
- For C1, inspect the owning header/File drawing, strip layout and header metric
  paths. Preserve Underline/Filled selection from `HeaderPresentation`, explicit
  public renderer precedence, candidate themes and custom clones. Keep shared
  strip measurement/overflow, tab-height scrollers, clipping and QAT reservations;
  do not infer a whole Office family from a palette name, radius or built-in ID.
- For planning, inspect only needed native owners and refine the bounded contract;
  do not run builds/tests or refresh packages for discussion alone. For an
  implementation request, carry the selected slice through its relevant checks.
- Geometry consolidation remains the foundation: reuse shared metrics, snapshots,
  DPI/rounding, clipping and native-content bounds. Do not replace the layout
  engines or reopen the closed geometry campaign.

## Preserve current contracts

- Office 2024 Backstage stays below its separate title bar. Its uncolored caption
  uses the animated neutral grey navigation tint; authored caption colors retain
  precedence, and closing restores the ordinary caption.
- Office 2019 real panes extend behind a transparent caption. Its title moves
  75% immediately and slides the last 25% without fading; resize/maximize/restore
  centering remains immediate. Keep the circular Back button and existing slide.
- Retain independent theme settings, nullable overrides, application theme/
  BuiltInTheme/Reset semantics, explicit public renderer hooks/precedence and
  native page/editor/presenter identity and state. No required Showcase helper.
- Preserve 2019 square surfaces, full-width body with inner padding, popup borders/
  shadows, tab-height scroller borders, QAT and all closed geometry results.

Use one agent and work under `winforms/`; WPF remains read-only. Preserve authored
Forms/UserControls/resources. Use normal Debug/Release outputs and the permanent
Designer Sample; leave actual VS review to the user and use no computer-use tools.
No commit, push, publication, branch switch or destructive cleanup.

The user requested **relevant tests only** to conserve usage. B1–B3 passing checks
are evidence for their slices, not fresh evidence for later changes. Select C1's
affected header/File/scroller/QAT/clipping cases and unchanged-family comparison;
do not restart the full suite or native matrix. Human appearance review scope is
in the themes ledger. Later light-theme order is unscheduled; dark variants stay
deferred.
