# RibbonKit agent instructions

## Scope and working agreement

RibbonKit is an MIT-licensed, lookless WPF ribbon library. Runtime targets are in
`src/RibbonKit/RibbonKit.csproj`; runnable consumers are `samples/RibbonKit.Showcase`
and `samples/RibbonKit.Writer`. Releases use GitHub Releases; NuGet.org publication is not planned.

- Follow current user instructions over repository and skill guidance, subject to host/system rules. Carry authorized work through implementation and appropriate verification; resolve routine choices without another approval. Ask only when missing information changes scope, correctness, or authorization, and continue independent work meanwhile.
- Preserve user edits and unrelated worktree changes. Keep changes narrowly scoped. Do not commit, push, or publish unless requested.
- During Writer work, keep `src/RibbonKit/**` read-only unless the user explicitly authorizes a focused runtime change. First establish a focused reproduction; record consumer glue, timing workarounds, automation gaps, and testing exceptions in `docs/12-RIBBONKIT-WRITER-CONSUMER-FRICTION-LOG.md`. Existing explicit authorization does not need to be requested again.
- Use a single agent by default; delegate only when the user requests it. Independent read-only searches may run concurrently; serialize edits and builds that share outputs.
- Report the outcome, relevant verification, and remaining limits in concise, plain prose. If a skill blocks authorized work, link the exact file and quote the blocking instruction rather than inventing an approval requirement.

## Context and workflow

- Start with `git status --short`. Before code changes, read the relevant part of `04-DESIGN-NOTES.md` §5 and the matching §3 subsystem entry. Search by feature name and follow the matching index link into `docs/history/`; read bounded sections rather than the entire history.
- Use `README.md` for public features and `CONTRIBUTING.md` for API compatibility and validation. Plans under `docs/` are design records unless their status banner says otherwise. Historical test counts and acceptance checkpoints are not current evidence.
- For WPF implementation, debugging, or verification, use `.agents/skills/ribbonkit-wpf-workflow/SKILL.md`. For documentation-only work, inspect the changed instructions, links, and diff; a WPF build is unnecessary unless the edit changes build behavior.
- Read `docs/06-MERGE-AND-MODAL-PLAN.md`, `docs/05-MDI-EMULATION-PLAN.md`, or `src/RibbonKit.Design/SETUP-DESIGNTOOLS.md` only for the corresponding subsystem.

## Architecture contracts

- Build lookless custom controls with dependency properties, routed events, commands, `ItemsSource`, templates, MVVM, keyboard access, and UI Automation.
- Keep one shared template set: `Themes/Office2024.xaml` aggregates `Controls.*.xaml`. Theme differences use matching `Tokens.Office*.xaml` keys through `DynamicResource`. Add new keys to every theme; avoid theme-specific literal colors/metrics. A `Brush` token may be a gradient.
- Preserve vector rendering and per-monitor-v2 DPI behavior; the host owns its DPI-awareness manifest. Animate opacity/transforms rather than layout, and honor reduced-motion/global animation settings.
- Keep modal/merge behavior in services, outside adaptive layout. Exclude transient state from customization persistence. Refresh the sliding underline and 2010/2013 connected-tab notch after tab collection, visibility, or layout changes.
- Keep `net472` design tooling independent of the runtime project, identify controls by type-name strings, and use the Visual Studio Model API. Preserve `RibbonKit.DesignTools.dll` packaging under each runtime assembly's `Design` folder.
- Preserve the shipped public API baseline. Document public APIs with XML comments and follow `CONTRIBUTING.md` for intentional additions and breaking changes.

## Evidence required for completion

Run the applicable checks in `CONTRIBUTING.md`; satisfy explicit task gates. Add meaningful regression coverage for behavior changes where appropriate. Once checks pass, repeat or broaden only for new changes, failures, or unresolved risk.

Separate build, focused tests, full-suite results, live UI/IME/DPI checks, and target-machine acceptance. Report which actually ran. For visual snapshot failures, inspect the `visual-snapshot-diagnostics` actual/diff PNGs before changing approvals or tolerances; an early failure does not establish that later scenes passed.
