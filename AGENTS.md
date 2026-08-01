## Imported Claude Cowork project instructions

A custom control in c# language for WPF Application to create Office Fluent UI looking Modern Ribbon that support multiple type of button size (large, medium, small), type (normal, split, dropdown, etc), groupings, and other features such as galleries, context tab, backstage, combo box, application button, customize dialog, minimize mode, quick access toolbar, theming support (office 2007, 2010, 2013, 2019, 2024), High DPI scaling support, tab merging support, modal tab support, xaml design-time preview, and other that i cannot think of

## Project context for coding agents

Last consolidated: 2026-08-01 from the repository Markdown files.

### Product and source of truth

- RibbonKit is an MIT-licensed, Office Fluent UI-style WPF control library for `net8.0-windows` and `net9.0-windows`, currently alpha (`0.1.0-alpha.1`).
- Treat current user instructions and this file as authoritative. For implementation status and pitfalls, prefer `04-DESIGN-NOTES.md`, especially sections 4-5, over older plans. Use `README.md` for the public feature summary. Files under `docs/` record architecture and historical plans; their original sequencing can be stale even when their design constraints remain useful.
- The showcase at `samples/RibbonKit.Showcase` is the executable integration demo and should grow with product features.

### Architecture invariants

- Build lookless WPF custom controls, not `UserControl`s. Follow WPF conventions: dependency properties, routed events, commands, `ItemsSource`, templates, MVVM, keyboard access, and UI Automation.
- Keep one shared control-template set. `Themes/Office2024.xaml` aggregates the `Themes/Controls.*.xaml` dictionaries; Office generations differ primarily through matching `Tokens.Office*.xaml` keys consumed with `DynamicResource`.
- Do not hardcode theme-specific colors or metrics in templates. When adding a token, add the same key to every theme dictionary; flat themes can use transparent or zero values. A token typed as `Brush` may contain a gradient.
- Preserve vector rendering and per-monitor-v2 DPI behavior. The host application must carry the DPI-awareness manifest; the library cannot set process DPI awareness for it.
- Motion should animate opacity and transforms, not layout properties such as width, height, or margin, and must honor the reduced-motion/global animation setting.
- Modal and merge behavior belongs in its services, not in the adaptive layout engine. Transient merged/modal state must not leak into customization persistence. After tab collection, visibility, or layout changes, explicitly refresh the sliding underline and the 2010/2013 connected-tab notch.
- Design tooling targets `net472`, does not reference the runtime project, names control types by string, and edits through the Visual Studio Model API. It is packaged as `RibbonKit.DesignTools.dll` under each runtime assembly's `Design` folder.

### Status at consolidation

- Roadmap phases 0-5 and 7 are complete. All five themes (Office 2007, 2010, 2013, 2019, and 2024) ship, and DPI was verified at 100/125/150/175/200%. Phase 6 still owes dark mode, RTL, localization, and visual-regression snapshots. Phase 8/API freeze has not started.
- The Office 2007 two-pane application menu has shipped as `RibbonApplicationMenu`; the 2007 window frame remains deferred.
- MDI milestones M0 and M4 are complete: floating children plus tab/caption merge work. M1-M3 remain: arrange commands and keyboard cycling, full MVVM proof, tabbed-document mode, and persistence.
- The latest design notes still request Windows verification for sections 3.42-3.46, notably whole-surface flyout animation, vertical split-button/companion states, proxy enabled-state propagation, and the Office 2007 application menu.
- The design notes record 47 green tests as of 2026-07-27. Important missing coverage includes merge/modal invariants, customization round trips, KeyTip resolution, and remaining reduction-algorithm cases.

### Working conventions

- Before changing a subsystem, read its section in `04-DESIGN-NOTES.md`; it records failure modes that are easy to reintroduce. Consult `docs/06-MERGE-AND-MODAL-PLAN.md`, `docs/05-MDI-EMULATION-PLAN.md`, or `src/RibbonKit.Design/SETUP-DESIGNTOOLS.md` when working in those areas.
- Keep changes narrowly scoped. Add or update a unit test, showcase scenario, and documentation where appropriate. Public APIs require XML documentation.
- On this Windows workspace, verify proportionally with `dotnet build RibbonKit.sln` and `dotnet test RibbonKit.sln`. Do not repeat the design notes' stale claim that WPF cannot be built locally without first trying the current environment.
- Preserve user edits and unrelated worktree changes. The user prefers concise, minimally formatted progress reports.
