# State commands in group and overflow popups

Status, **2026-11-03: implemented and packaged; focused/native checks passed;
the user confirmed "everything works" for the popup-retention correction**.

The user reported split-button checking primaries, checkboxes and radio buttons
closing ordinary collapsed-group and Simplified group flyouts, while QAT groups
retained them. The existing keep-open policy was restricted to groups retained by
QAT; direct More Commands/QAT overflow rows already had a matching state policy.

## Correction

One shared activation decision now recognizes a presented state command in either
a group popup or a More Commands/QAT overflow popup. It includes ordinary toggles,
checkboxes, radio buttons and split primaries with `CheckOnClick=true`, preserving
the same popup and logical focus for mouse and keyboard activation. QAT membership
is no longer required for group popups. It checks that the activated source or
reference is actually presented in that surface.

Ordinary command actions and nonchecking split primaries still dismiss the stack.
Split arrows continue opening their own menu; menu commands retain their existing
dismissal. The decision is made before activation, so state-before-Click ordering,
radio peer selection and one activation per input remain. Callback-driven closing,
structural invalidation and explicit dismissal still take effect without reopening
a popup. There is no public API, ownership, palette or geometry addition.

## Verification

- The pre-fix Simplified group tests reproduced closing for all four state command
  types; direct overflow and QAT group cases passed. The initial collapsed setup
  incorrectly disabled group reduction. After correcting the fixture, all four
  ordinary collapsed-group cases reproduced the same closing failure before the
  runtime change (`state-popups-collapsed-before.trx`).
- **24/24 new Debug unit/STA cases passed**, covering native popup mouse-down/up,
  keyboard Space, first/repeated changes, state/Click order, radio peer clearing,
  the same popup/focus, ordinary commands, nonchecking split primaries, nested menu
  action dismissal and explicit callback closure across collapsed, Simplified
  group, direct Simplified overflow and QAT group presentations.
  Evidence: `tests/RibbonKit.WinForms.Tests/TestResults/state-popups-after.trx`.
- **40/40 related Debug cases passed** across QAT groups, QAT menu/options,
  Simplified layout/selection, split buttons and menu choices in
  `state-popups-regressions.trx`.
- The independent public-API native QAT group consumer passed at actual **120
  DPI**, including pointer/KeyTip/Space activation, retained editor handles and
  binding values, split arrow/menu dismissal, Escape stack, source removal and
  restoration. Evidence: `artifacts/state-command-popups/native-qat-group/`.
  This fixture owns its windows/input; it is not user or mixed-monitor acceptance.
- Normal Debug runtime, native runner, designer server/client, permanent Designer
  Sample and Showcase builds passed with zero warnings/errors. The Designer Sample
  now uses **`0.1.0-designer.local20261103T101745293Z`**, refreshed in place without
  replacing its authored Forms/UserControls/resources.

The preceding gallery scrollbar correction was preserved. No full suite, Release,
WPF, commit or push work is part of this slice. After user review, the next feature
is the [Application Menu replacement](APPLICATION-MENU-PLAN.md), followed by
geometry consolidation. Do not start that feature as part of this fix.
