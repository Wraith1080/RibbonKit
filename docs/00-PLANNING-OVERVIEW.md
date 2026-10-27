# RibbonKit planning overview

The original 2026-07-02 foundation plan is complete: RibbonKit is an MIT-licensed,
Office Fluent UI-style WPF control library with a separate Showcase and Writer.
This page retains the product intent; it is not a second progress board.

## Product intent

Provide lookless, reusable controls with WPF commanding, binding, templates,
keyboard access and UI Automation. Preserve Office-generation visual fidelity,
vector chrome and host-owned per-monitor DPI awareness. Keep theme differences in
shared-template tokens so fixes apply across generations.

The enduring engineering risks are adaptive layout stability, popup/resource
lifetimes, native window chrome and DPI, KeyTip/focus routing, and a growing visual
matrix. Their measured failures and fixes belong in the design history.

## Sources of truth

| Need | Reference |
| --- | --- |
| Use the library and check supported features | [README](../README.md) |
| Locate implementation and architectural contracts | [Architecture](01-ARCHITECTURE.md) |
| Current progress and remaining gates | [Design notes §5](../04-DESIGN-NOTES.md#5-current-state--next-steps) |
| RibbonKit 1.1 acceptance and release preparation | [1.1 acceptance plan](20-V1.1-ACCEPTANCE-AND-RELEASE-PLAN.md) |
| Native WinForms scope and first implementation milestone | [WinForms plan](21-NATIVE-WINFORMS-PLAN.md), [native acceptance](../winforms/docs/M1-STATUS.md) |
| Completed phases and candidate tracks | [Roadmap](03-ROADMAP.md) |
| Planned ribbon follow-ons, in order | [Feature availability](16-RIBBON-FEATURE-AVAILABILITY-PLAN.md), [Toolbox definitions](17-TOOLBOX-ITEM-DEFINITION-PLAN.md), [Ribbon/host animation synchronization](18-HOST-LAYOUT-TRANSITION-PLAN.md) |
| Deferred WPF ideas for later review | [Future WPF improvements](22-FUTURE-WPF-IMPROVEMENTS.md) |
| Work and validate a change | [AGENTS.md](../AGENTS.md), [CONTRIBUTING.md](../CONTRIBUTING.md) |

Original name-selection, package-name reservation, competitor comparisons and
pre-v1 setup checklists have been retired. They no longer guide implementation.
