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
| Completed phases and candidate tracks | [Roadmap](03-ROADMAP.md) |
| Work and validate a change | [AGENTS.md](../AGENTS.md), [CONTRIBUTING.md](../CONTRIBUTING.md) |

Original name-selection, package-name reservation, competitor comparisons and
pre-v1 setup checklists have been retired. They no longer guide implementation.
