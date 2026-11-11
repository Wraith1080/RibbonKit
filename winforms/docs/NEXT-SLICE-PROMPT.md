# Continue native WinForms theme architecture after D1a command-state chrome

Workspace:
`C:\Users\LENOVO\Claude\Projects\Professional Ribbon Custom Control for WPF`

Updated **2026-11-12**. **B1–B3, C1–C2 and D1a command-state chrome are implemented.**
Current evidence and review scope are in the
[themes ledger](THEMES-PLAN.md#current-status-and-review-ledger) and
[C1 record](THEMES-PLAN.md#header-presentation-c1--2026-11-11).
The user then requested a review of all earlier rewrite slices before C2.
The [adjustability audit](THEME-EFFECT-ADJUSTABILITY-AUDIT.md) is complete.
[A00–A04 fill routing/motion backfill](THEMES-PLAN.md#backstage-fill-routing-and-motion-overrides--2026-11-11)
is implemented and reviewed. [A06–A09 decoration/spacing](THEMES-PLAN.md#backstage-decoration-and-spacing-overrides--2026-11-11)
is implemented; [A11–A16 header/shared helpers](THEMES-PLAN.md#header-and-shared-effect-overrides--2026-11-11)
are implemented. [C2 body/connected frames](THEMES-PLAN.md#body-and-connected-frames-c2--2026-11-11)
is implemented. [D1a command state/split chrome](THEMES-PLAN.md#command-state-and-split-chrome-d1a--2026-11-12)
is implemented and verified. D1b command content/indicators is next proposed; follow
the user's selected scope. A05's caption choices,
A10's page-width preference and A17's shared tip placement remain separate.

## Establish scope from the user's current request

- Start with `git status --short` and inspect live branch/HEAD. During D1a the user
  committed C2 as `f83a0ef` on `codex/Ribbonkit-Winform`; that commit was rechecked
  and preserved. Recheck live state and preserve user edits/commits and later work.
- Read the audit findings first. On an implementation continuation, preserve
  A00–A04/A06–A09/A11–A16, C2 and D1a. Continue the user's selected remaining surface.
  Preserve current defaults and keep the separately deferred title-ink defect
  separate. The audit is a recommendation for follow-up, not a new approval gate
  if the user selects C2 or another scope.
- `RibbonTheme.BackstageMotion` is one immutable, independently resettable
  override group. Existing engines capture its resolved family policy. Keep
  zero-duration snap, pane/title duration coupling, independent page Fade,
  slide-only preparation alpha and native ownership. Explicit navigation fills
  take precedence in inset material drawing; null still uses the established
  backdrop fallback. Do not reintroduce derived fills as authored overrides.
- `BackstageAppearance` contains immutable marker, Back circle/glyph and navigation
  state-shape overrides. Existing visibility/radius/coverage flags remain
  independent. Keep null fractional-DPI rounding, explicit zero/hide, alpha,
  target geometry, row clipping and actual renderer entry/chrome/text/focus hooks.
  The three spacing overrides feed existing shared metrics; `MenuSeparatorHeight`
  stays menu-only. The permanent sample includes **Backstage appearance** controls.
- Preserve the header/shared overrides: `HeaderAppearance`, `Chevrons`, focus
  inset/width, shared tab-content/header-arrival slide distance and indicator
  spring amount. Keep null/Empty defaults, actual renderer precedence, body/header
  scroller separation, native focus visibility and scene-captured theme values.
- **User instruction:** Office 2019 and **Theme effects 2** are full. Add new
  review groups/buttons to **Theme effects 3** until the user says it is full.
  Reuse `ThemeEffectsReviewTab` (now tab 3); `ThemeEffects2ReviewTab` pins existing
  shared/body groups to tab 2. Do not add buttons to either full tab or declare
  tab 3 full from layout alone.
- C2 resolves explicit neutral/Office body defaults into value-only frame metrics
  and shared drawing. `BodyAppearance` adds border/divider/join overrides and
  `DrawGroupSeparator` serves Classic/Simplified paint. Preserve null device-pixel
  defaults, actual renderer capabilities/hooks, existing snapshots/reduction,
  native identity, margins/shadows, full-width 2019 body and popup anchors.
- Preserve `CommandAppearance`, value-only metrics and the shared split-perimeter
  helper. Ordinary/menu/QAT command states use actual virtual chrome hooks;
  null preserves one-device-pixel geometry. Explicit companion fills precede
  material fallback; existing state fills/QAT opacity and active-half precedence
  remain. Content layout, split targets/KeyTips, native editors and Application
  Menu recipes retain their existing owners.
- Read [THEME-ARCHITECTURE-PLAN](THEME-ARCHITECTURE-PLAN.md), especially D and
  ownership/compatibility rules. D1b command content/indicators is next proposed:
  inspect the remaining shared content/glyph recipes and metrics, then choose a
  bounded surface. Native editor styling remains a separate subsequent slice.
  Group/caption command content remains with those owners; C2 extracted frames.
  A new family selector is justified only by actual structural difference;
  do not infer presentation from palette name, header style or corner radius.
- **User requirement:** while extracting each surface, turn remaining fixed
  visual effects into independently adjustable theme settings, retaining the
  current defaults and validation. Reuse existing roles first. Internal family
  constants alone do not satisfy this. C1's public header roles are the example;
  keep native lifecycle, accessibility/safety clamps and clocks shared.
- For planning only, inspect needed owners and refine the bounded contract;
  no builds/tests or package refresh are needed for discussion. For an
  implementation request, carry the selected slice through its relevant checks.
- Geometry consolidation remains the foundation: reuse shared metrics, snapshots,
  DPI/rounding, clipping and native-content bounds. Do not replace the layout
  engines or reopen the closed geometry campaign.

## Preserve current contracts

- C1 pairs Underline/2024 and Filled/2019 sibling drawing/defaults through
  `RibbonPresentationResolver.Header`. `HeaderPresentation` is independent of
  palette, radius, caption extension and Backstage. The public Office renderer
  facade, flat/custom renderer precedence and actual text/focus/indicator hooks
  remain authoritative. Header effects must not change strip/hit-target geometry.
- Retain C1's six independently resettable public effects and previous header
  spacing, scroller, clipping and File/QAT reservations. The permanent sample's
  optional Office 2019 theme demo includes **Header effects** for review.
- Office 2024 Backstage stays below its separate title bar with neutral animated
  caption tint unless an authored caption color takes precedence. Office 2019
  real panes extend behind a transparent caption, with the accepted short title
  slide and immediate resize centering. Retain the page handoff and rounded-edge
  repaint corrections. The separately reported closing-title ink flicker is
  deferred in the themes ledger; later control slices do not silently absorb it.
- Preserve independent settings, nullable override/Reset semantics, application
  theme ownership and native control/editor/presenter identity and state. Keep
  2019's full-width body/inner padding, square defaults, popup borders/shadows,
  QAT/message joins and all closed geometry results. No required Showcase helper.

Use one agent and work under `winforms/`; WPF remains read-only. Preserve authored
Forms/UserControls/resources. Use normal Debug/Release outputs and the permanent
Designer Sample; leave actual VS review to the user and use no computer-use tools.
No commit, push, publication, branch switch or destructive cleanup.

The user requested **relevant tests only** to conserve usage. Earlier passing
checks belong to their own slices. Select the next batch's affected command/
editor paint, content/geometry, native state and popup routing cases, plus an
unchanged-default comparison. Keep Classic/Simplified and Compact/Touch where
affected. Reuse C2 frame evidence unless new changes actually affect it.
Do not restart the full suite or native matrix.
Human appearance review scope is in the themes ledger. Later light-theme order
is unscheduled; dark variants stay deferred.
