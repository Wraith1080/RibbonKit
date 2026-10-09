# RibbonKit agent instructions

## Scope and working agreement

RibbonKit is an MIT-licensed, lookless WPF ribbon library. Runtime targets are in
`src/RibbonKit/RibbonKit.csproj`; runnable consumers are `samples/RibbonKit.Showcase`
and `samples/RibbonKit.Writer`. Releases use GitHub Releases; NuGet.org publication is not planned.

- Follow current user instructions over repository and skill guidance, subject to host/system rules. Carry authorized work through implementation and appropriate verification; resolve routine choices without another approval. Ask only when missing information changes scope, correctness, or authorization, and continue independent work meanwhile.
- Preserve user edits and unrelated worktree changes. Keep changes narrowly scoped. Do not commit, push, or publish unless requested.
- Use the normal Debug configuration and default output folder for local review. Required Release validation uses the normal Release output. Do not create task-specific configurations or output folders unless the user requests them.
- During Writer work, keep `src/RibbonKit/**` read-only unless the user explicitly authorizes a focused runtime change. First establish a focused reproduction; record consumer glue, timing workarounds, automation gaps, and testing exceptions in `docs/12-RIBBONKIT-WRITER-CONSUMER-FRICTION-LOG.md`. Existing explicit authorization does not need to be requested again.
- Use a single agent by default; delegate only when the user requests it. Independent read-only searches may run concurrently; serialize edits and builds that share outputs.
- Report the outcome, relevant verification, and remaining limits in concise, plain prose. If a skill blocks authorized work, link the exact file and quote the blocking instruction rather than inventing an approval requirement.

## Context and workflow

- Start with `git status --short`. Before code changes, read the relevant part of `04-DESIGN-NOTES.md` §5 and the matching §3 subsystem entry. Search by feature name and follow the matching index link into `docs/history/`; read bounded sections rather than the entire history.
- Use `README.md` for public features and `CONTRIBUTING.md` for API compatibility and validation. Plans under `docs/` are design records unless their status banner says otherwise. Historical test counts and acceptance checkpoints are not current evidence.
- Keep `docs/history/01-library.md` as a compact directory. Put concise implementation records in the matching `docs/history/library/` archive; start another bounded file when one approaches 800 lines. Keep decisions, regression pitfalls and verification limits; remove superseded next-step instructions, repeated status recaps and investigation narration. Link directly to the record from the design index. Current acceptance belongs in the active plan.
- For WPF implementation, debugging, or verification, use `.agents/skills/ribbonkit-wpf-workflow/SKILL.md`. For documentation-only work, inspect the changed instructions, links, and diff; a WPF build is unnecessary unless the edit changes build behavior.
- Read `docs/06-MERGE-AND-MODAL-PLAN.md`, `docs/05-MDI-EMULATION-PLAN.md`, or `src/RibbonKit.Design/SETUP-DESIGNTOOLS.md` only for the corresponding subsystem.

### Quick documentation and acceptance updates

- For a user confirmation, read only the active plan's current acceptance section and any directly affected text. Reuse instructions and evidence already read in this conversation; do not reload the WPF skill, history, implementation, memory or test logs for a routine confirmation.
- Record the result once in the active plan's existing status row or review item, with its date and exact review scope. Keep `04-DESIGN-NOTES.md` as a short pointer to that plan. Do not mirror acceptance prose into its §5, README, and history or create a numbered history/index entry for every confirmation, including final acceptance.
- Add history for new implementation details, changed automated evidence, a new pitfall or an explicitly requested historical record. Preserve existing history; it is not a second current-status checklist.
- Batch the authorized text edits, review only the changed content and any changed link targets, then run `git diff --check` once. Validate skill metadata only if a skill changed. No build, test, screenshot inspection, app launch, broad repository audit or new handoff for prose-only acceptance.
- If the user only asks about status, read the active plan's status section and answer; no documentation edit is needed. Finish after the required review, without speculative cleanup or repeated verification.

## Architecture contracts

- Build lookless custom controls with dependency properties, routed events, commands, `ItemsSource`, templates, MVVM, keyboard access, and UI Automation.
- Treat Showcase as a consumer and demonstration of RibbonKit, not the sole
  implementation of reusable features. Every ribbon/control feature and visual
  demonstrated there must be attainable by another application through shared
  controls, templates, theme tokens and public extension points, with feature
  and visual parity. A Showcase-only style, subclass, template-part patch or
  presentation helper is a temporary prototype for reusable control behavior;
  promote it into RibbonKit before calling that behavior portable or complete.
  Keep application content, commands, data, persistence, icons, document paint
  and platform/window integration that necessarily depends on the host in the
  host. When a reusable effect needs host participation, provide an optional
  library integration contract and document the host's part.
- Keep one shared template set: `Themes/Office2024.xaml` aggregates `Controls.*.xaml`. Theme differences use matching `Tokens.Office*.xaml` keys through `DynamicResource`. Add new keys to every theme; avoid theme-specific literal colors/metrics. A `Brush` token may be a gradient.
- Preserve vector rendering and per-monitor-v2 DPI behavior; the host owns its DPI-awareness manifest. Animate opacity/transforms rather than layout, and honor reduced-motion/global animation settings.
- Keep modal/merge behavior in services, outside adaptive layout. Exclude transient state from customization persistence. Refresh the sliding underline and 2010/2013 connected-tab notch after tab collection, visibility, or layout changes.
- Keep `net472` design tooling independent of the runtime project, identify controls by type-name strings, and use the Visual Studio Model API. Preserve `RibbonKit.DesignTools.dll` packaging under each runtime assembly's `Design` folder.
- Preserve the shipped public API baseline. Document public APIs with XML comments and follow `CONTRIBUTING.md` for intentional additions and breaking changes.

## Evidence required for completion

Run the applicable checks in `CONTRIBUTING.md`; satisfy explicit task gates. Add meaningful regression coverage for behavior changes where appropriate. Once checks pass, repeat or broaden only for new changes, failures, or unresolved risk.

Use focused local validation by default for bounded changes, including small shared-template/token adjustments. Select affected regressions, visual scenes and independent-consumer scopes with `eng/Test-Focused.ps1` or the documented direct commands. Reserve the full local suite for broad shared behavior/template contracts, API/build changes, release readiness or an explicit gate; CI retains full validation. Clear local selection and capture/update variables before a full run.

Separate build, focused tests, full-suite results, live UI/IME/DPI checks, and target-machine acceptance. Report which actually ran. For visual snapshot failures, inspect the `visual-snapshot-diagnostics` actual/diff PNGs before changing approvals or tolerances; an early failure does not establish that later scenes passed.

For Showcase-originated ribbon/control features, verify portability in a consumer that does not reference Showcase resources or helpers. Record any remaining Showcase-only behavior as a gap rather than claiming RibbonKit feature or visual parity.
