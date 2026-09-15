# RibbonKit roadmap

Phases 0–8 and the v1.0.0 GitHub launch are complete. Current progress, acceptance
limits and the next bounded task live in [design notes §5](../04-DESIGN-NOTES.md#5-current-state--next-steps).
This page retains phase identities and routes future work; it does not repeat test counts.

## Completed v1 phases

| Phase | Delivered scope |
| --- | --- |
| 0 | Solution, MIT license, repository and CI foundation |
| 1 | Ribbon/tab/group skeleton and adaptive sizing |
| 2 | Commands, menus, inputs, ScreenTips and keyboard interaction |
| 3 | File surfaces and Backstage |
| 4 | Galleries and live preview |
| 5 | QAT, window chrome, contextual tabs and customization |
| 6 | Five themes, dark/black variants, DPI, localization/RTL and snapshots |
| 7 | Tab merging and modal tabs; MDI caption integration |
| 8 | Public API freeze, documentation, Source Link/symbols, package/consumer verification and GitHub release |

The numbered [history](../04-DESIGN-NOTES.md#3-implemented-features-chronological-with-pitfalls)
retains implementation decisions and dated gates. Release assets are described in
[RELEASE_NOTES.md](../RELEASE_NOTES.md); later checkout changes are not automatically in v1.0.0.

## Post-v1 tracks

| Track | Boundary and reference |
| --- | --- |
| Office 2007 frame/File work | S0–S9 complete; [retained reference](07-OFFICE-2007-THEME-PLAN.md) |
| MDI | M0/M4 complete; arrangement/cycling, MVVM proof, tabbed mode and persistence remain in the [MDI plan](05-MDI-EMULATION-PLAN.md) |
| RibbonKit Writer | Separate rich-text consumer; [product contract](10-RIBBONKIT-WRITER-PLAN.md), [packet map](11-RIBBONKIT-WRITER-LUNA-EXECUTION-PLAN.md), [friction](12-RIBBONKIT-WRITER-CONSUMER-FRICTION-LOG.md) |
| Custom-control/QAT projections | Opt-in factories and source identity; [candidate design](08-CUSTOM-CONTROL-INTEGRATION-PLAN.md) |
| Additional themes | Office 2021, then original palettes; [candidate design](09-FUTURE-THEMES-PLAN.md) |

Unscheduled candidates include simplified single-row ribbon, touch density, an
Office-style status bar, XML-defined ribbon authoring, purpose-authored monochrome
QAT icons, and an opaque non-Mica theme-transition capture. These are not supported
APIs or commitments. A ribbon slider is intentionally not planned.

Follow [CONTRIBUTING.md](../CONTRIBUTING.md) for delivery and verification. The old
pre-release tagging rhythm, name reservation, speculative cost estimates and
instructions to begin already-completed phases no longer apply.
