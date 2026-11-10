# Continue native WinForms theme architecture after B1

Workspace:
`C:\Users\LENOVO\Claude\Projects\Professional Ribbon Custom Control for WPF`

Updated **2026-11-10**. The user authorized the architecture plan's first slice
after planning. **B1 Backstage registration is implemented**; one resolver now
pairs the existing sibling layout/drawing helpers. Current evidence and pending
human review are recorded once in
[THEMES-PLAN](THEMES-PLAN.md#backstage-registration-b1--2026-11-10).

## Establish scope from the user's current request

- Start with `git status --short` and inspect live branch/HEAD. B1 was implemented
  on `codex/Ribbonkit-Winform` over `47d42cd` (`Backstage fix`); changes remain
  uncommitted. Preserve later user edits and the already committed caption fixes.
- Read [THEME-ARCHITECTURE-PLAN](THEME-ARCHITECTURE-PLAN.md), especially the B2
  row and ownership/compatibility rules. **B2 explicit layout inputs/defaults**
  is the next proposed scope. B3 caption/motion policy and headers/body remain
  separate. Do not repeat B1 or add another theme family.
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

The user requested **relevant tests only** to conserve usage. B1's passing checks
are evidence for B1, not fresh evidence for later changes. Select the next slice's
affected cases; do not restart the full suite or native matrix. Human appearance
review remains pending and later light-theme order is unscheduled; dark variants
remain deferred.
