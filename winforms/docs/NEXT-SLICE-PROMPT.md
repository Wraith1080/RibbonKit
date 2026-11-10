# Continue native WinForms theme architecture after B2

Workspace:
`C:\Users\LENOVO\Claude\Projects\Professional Ribbon Custom Control for WPF`

Updated **2026-11-11**. **B1 registration and B2 explicit layout inputs/defaults
are implemented.** The shared Backstage layout consumes numeric measurements;
family defaults are immutable and page padding is retained in its snapshot.
Current evidence and user review are recorded once in
[THEMES-PLAN](THEMES-PLAN.md#explicit-backstage-layout-inputs-b2--2026-11-11).

## Establish scope from the user's current request

- Start with `git status --short` and inspect live branch/HEAD. B2 began with a
  clean tree on `codex/Ribbonkit-Winform` at `4cf1501` (`B1 Rewrite`); B2 changes
  remain uncommitted. Preserve later user edits and committed B1/caption fixes.
- Read [THEME-ARCHITECTURE-PLAN](THEME-ARCHITECTURE-PLAN.md), especially the B3
  row and ownership/compatibility rules. **B3 caption/motion policy** is the next
  proposed scope. Keep the measured layout/defaults from B2; do not repeat B1/B2,
  rewrite transition clocks, extract headers/body or add another theme family.
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

The user requested **relevant tests only** to conserve usage. B1/B2 passing checks
are evidence for their slices, not fresh evidence for later changes. Select the next slice's
affected cases; do not restart the full suite or native matrix. Human appearance
review scope is in the themes ledger; later light-theme order is unscheduled and dark variants
remain deferred.
