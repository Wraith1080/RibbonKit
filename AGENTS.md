## Imported Claude Cowork project instructions

A custom control in c# language for WPF Application to create Office Fluent UI looking Modern Ribbon that support multiple type of button size (large, medium, small), type (normal, split, dropdown, etc), groupings, and other features such as galleries, context tab, backstage, combo box, application button, customize dialog, minimize mode, quick access toolbar, theming support (office 2007, 2010, 2013, 2019, 2024), High DPI scaling support, tab merging support, modal tab support, xaml design-time preview, and other that i cannot think of

## Project context for coding agents

Last consolidated: 2026-08-13 from the repository Markdown files.

### Product and source of truth

- RibbonKit is an MIT-licensed, Office Fluent UI-style WPF control library for `net8.0-windows` and `net9.0-windows`. Version 1.0.0 is published through GitHub Releases; NuGet.org publication is not planned.
- Treat current user instructions and this file as authoritative. For current implementation status, use `04-DESIGN-NOTES.md` §5; for subsystem history and pitfalls, use its matching §3 entry. Use `README.md` for the public feature summary. Files under `docs/` are architecture/design records unless their status banner says otherwise; do not infer current implementation state from their original sequencing.
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

- Roadmap phases 0-8 and the GitHub v1.0.0 launch are complete. Post-v1 Office 2007 window-frame/Backstage S7-S9 is complete through design-notes §3.94. MDI M1-M3 remain; custom-control projections, future themes, and RibbonKit Writer are plans rather than implementation evidence.
- The Office 2007 two-pane application menu has shipped as `RibbonApplicationMenu`. The post-v1 S7 window-frame work is complete: the corrected no-material baseline and independently selectable Aero-inspired/Acrylic presentation passed the 100/125/150/175/200% proportional gate plus live 125%↔150% mixed-monitor, caption, eight-edge resize, maximize/work-area and Snap Layout checks. The additive `Glass2007` and `Classic2007` Backstage designs are complete without replacing the real application-menu default; Classic uses a shared-chrome Backstage orb proxy while the real ribbon button remains in its original layout slot.
- The Office 2010 `Classic2010` Backstage now follows Word 2010 placement below the live tab-header row. File, ribbon tabs, and title-bar QAT chrome remain exposed and interactive; custom templates without the anchor retain the full-content fallback. A three-DIP seam matches the open File gradient's bottom color, nav/page content is inset below it, a clipped darker shadow restores only the pane's left-side depth, the File tab retains its gradient depth, and native or merged ribbon-tab selected chrome yields while Backstage is active. The Office 2010 Aero/frame idea remains a separate future visual pass.
- MDI milestones M0 and M4 are complete: floating children plus tab/caption merge work. M1-M3 remain: arrange commands and keyboard cycling, full MVVM proof, tabbed-document mode, and persistence.
- Windows verification includes whole-surface/reduced-motion flyouts, split-button states, proxy enabled-state propagation, the application-menu theme/DPI matrix, localization/RTL popup and window behavior, the responsive Ribbon Editor (§3.63a), and the Office 2007 frame/Backstage work (§§3.89-3.94).
- The compact `RibbonCheckBox`, `RibbonRadioButton`, and `RibbonTextBox` input slice is user-verified, including `RibbonTextBox` at 100/125/150/175/200% DPI and focused RTL input behavior. A ribbon slider was considered and is intentionally not planned.
- Verified baseline on 2026-08-13: 347 logic tests plus one visual test covering 63 approved images, with zero build warnings/errors. Treat this as a dated checkpoint and rerun before quoting a current count. Evaluate live-resize performance outside Visual Studio's debugger; no resize-specific shadow workaround ships. Touch mode and richer QAT projections remain post-v1 considerations rather than frozen placeholder APIs.

### Working conventions

- Before changing a subsystem, read its section in `04-DESIGN-NOTES.md`; it records failure modes that are easy to reintroduce. Consult `docs/06-MERGE-AND-MODAL-PLAN.md`, `docs/05-MDI-EMULATION-PLAN.md`, or `src/RibbonKit.Design/SETUP-DESIGNTOOLS.md` when working in those areas.
- Keep changes narrowly scoped. Add or update a unit test, showcase scenario, and documentation where appropriate. Public APIs require XML documentation.
- On this Windows workspace, verify proportionally with `dotnet build RibbonKit.sln` and `dotnet test RibbonKit.sln`. Do not repeat the design notes' stale claim that WPF cannot be built locally without first trying the current environment.
- On a CI visual mismatch, download the failure-only `visual-snapshot-diagnostics` artifact and inspect its actual/diff PNGs before changing approvals or tolerances; the first failing matrix image does not prove later scenes passed.
- Preserve user edits and unrelated worktree changes. The user prefers concise, minimally formatted progress reports.
