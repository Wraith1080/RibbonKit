## Imported Claude Cowork project instructions

A custom control in c# language for WPF Application to create Office Fluent UI looking Modern Ribbon that support multiple type of button size (large, medium, small), type (normal, split, dropdown, etc), groupings, and other features such as galleries, context tab, backstage, combo box, application button, customize dialog, minimize mode, quick access toolbar, theming support (office 2007, 2010, 2013, 2019, 2024), High DPI scaling support, tab merging support, modal tab support, xaml design-time preview, and other that i cannot think of

## Project context for coding agents

Last consolidated: 2026-08-01 from the repository Markdown files.

### Product and source of truth

- RibbonKit is an MIT-licensed, Office Fluent UI-style WPF control library for `net8.0-windows` and `net9.0-windows`, currently alpha (`0.1.0-alpha.1`).
- Treat current user instructions and this file as authoritative. For implementation status and pitfalls, prefer `04-DESIGN-NOTES.md`, especially sections 4-5, over older plans. Use `README.md` for the public feature summary. Files under `docs/` record architecture and historical plans; their original sequencing can be stale even when their design constraints remain useful.
- The showcase at `samples/RibbonKit.Showcase` is the executable integration demo and should grow with product features. Its dedicated Localization/RTL lab is the manual verification surface for provider refresh, mirroring, popup direction, and customization dialogs.

### Architecture invariants

- Build lookless WPF custom controls, not `UserControl`s. Follow WPF conventions: dependency properties, routed events, commands, `ItemsSource`, templates, MVVM, keyboard access, and UI Automation.
- Keep one shared control-template set. `Themes/Office2024.xaml` aggregates the `Themes/Controls.*.xaml` dictionaries; Office generations differ primarily through matching `Tokens.Office*.xaml` keys consumed with `DynamicResource`.
- Do not hardcode theme-specific colors or metrics in templates. When adding a token, add the same key to every theme dictionary; flat themes can use transparent or zero values. A token typed as `Brush` may contain a gradient.
- Preserve vector rendering and per-monitor-v2 DPI behavior. The host application must carry the DPI-awareness manifest; the library cannot set process DPI awareness for it.
- Motion should animate opacity and transforms, not layout properties such as width, height, or margin, and must honor the reduced-motion/global animation setting.
- Modal and merge behavior belongs in its services, not in the adaptive layout engine. Transient merged/modal state must not leak into customization persistence. After tab collection, visibility, or layout changes, explicitly refresh the sliding underline and the 2010/2013 connected-tab notch.
- Design tooling targets `net472`, does not reference the runtime project, names control types by string, and edits through the Visual Studio Model API. It is packaged as `RibbonKit.DesignTools.dll` under each runtime assembly's `Design` folder.

### Status at consolidation

- Roadmap phases 0-7 are complete. All five themes (Office 2007, 2010, 2013, 2019, and 2024) ship with generation-specific dark/black variants, and DPI was verified at 100/125/150/175/200%. The deterministic 40-image theme/variant matrix covers ten palettes at 100/125/150/200%, with focused RTL ribbon, QAT-customization, and representative bidirectional Backstage approvals. Phase 6's localization/RTL work is complete through design-notes §3.61, and the responsive Ribbon Editor/application-menu authoring work is complete and user-verified in VS through §3.62. Phase 8/API freeze has not started.
- The Office 2007 two-pane application menu has shipped as `RibbonApplicationMenu`; the 2007 window frame remains deferred.
- MDI milestones M0 and M4 are complete: floating children plus tab/caption merge work. M1-M3 remain: arrange commands and keyboard cycling, full MVVM proof, tabbed-document mode, and persistence.
- Windows verification is complete through design-notes §3.62, including whole-surface/reduced-motion flyouts, the Ribbon Editor's Large-only split-layout behavior, runtime split-button/companion states, proxy enabled-state propagation, the application-menu theme/DPI matrix, the final live localization/RTL popup/window pass, and crash-free atomic Ribbon Editor cycling among Closed, Backstage, and application-menu previews with pane selection.
- The compact `RibbonCheckBox`, `RibbonRadioButton`, and `RibbonTextBox` input slice is user-verified, including `RibbonTextBox` at 100/125/150/175/200% DPI and focused RTL input behavior. A ribbon slider was considered and is intentionally not planned.
- The test suite has 206 green logic tests plus one visual test covering 62 approved images as of 2026-08-08. Phase 7 merge/modal invariants, nested dark-foreground/control-surface contracts, Office 2010 seam/button-state/Backstage-shell/glass-caption contracts, animated repeatable connected message-bar API/template/theme/RTL/Showcase, first-frame motion-seeding, pending-template entrance ownership, and application-menu shadow-isolation contracts, complete 2007/2010 Black application-menu palettes, all ten theme palettes, focused dark/Black neutral Backstage rails, 2007/2010/2024 Backstage depth and connected message-bar stacking, the 2024 rectangular and 2007 orb open-menu/message compositions, classic-dark menu scenes, RTL ribbon/QAT-customization/bidirectional-Backstage/message-bar/input scenes, compact option and text input controls in light/dark, RTL physical-frame/logical-content isolation, live Backstage flow/title-axis/slide-edge behavior, synchronized Showcase Backstage/application-menu/orb choices, persistent application-menu footer-button outlines, localized ribbon context-menu/customization/chrome/default-File/conventional application-menu-footer contracts, application-button-width selection-visual reflow, and design-preview runtime isolation plus scoped theme-token replacement are covered; important remaining gaps include broader customization round trips, KeyTip resolution, and remaining reduction-algorithm cases.

### Working conventions

- Before changing a subsystem, read its section in `04-DESIGN-NOTES.md`; it records failure modes that are easy to reintroduce. Consult `docs/06-MERGE-AND-MODAL-PLAN.md`, `docs/05-MDI-EMULATION-PLAN.md`, or `src/RibbonKit.Design/SETUP-DESIGNTOOLS.md` when working in those areas.
- Keep changes narrowly scoped. Add or update a unit test, showcase scenario, and documentation where appropriate. Public APIs require XML documentation.
- On this Windows workspace, verify proportionally with `dotnet build RibbonKit.sln` and `dotnet test RibbonKit.sln`. Do not repeat the design notes' stale claim that WPF cannot be built locally without first trying the current environment.
- On a CI visual mismatch, download the failure-only `visual-snapshot-diagnostics` artifact and inspect its actual/diff PNGs before changing approvals or tolerances; the first failing matrix image does not prove later scenes passed.
- Preserve user edits and unrelated worktree changes. The user prefers concise, minimally formatted progress reports.
