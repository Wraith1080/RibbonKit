# Continuation prompt: native RibbonKit.WinForms M4C Backstage

Continue the native RibbonKit.WinForms implementation in:

`C:\Users\LENOVO\Claude\Projects\Professional Ribbon Custom Control for WPF`

Prepared on **2026-10-27**. The next bounded slice is **M4C Backstage**. First
inspect the current code and relevant WPF behavior, state a small native public
API/ownership/interaction proposal, then carry the authorized slice through
implementation and relevant verification. Resolve routine choices without another
approval; ask only when missing information materially changes scope or correctness.
Do not implement the rest of the roadmap in this session.

## Starting state and working agreement

- Start with `git status --short`. The branch at handoff is
  `codex/Ribbonkit-Winform`, HEAD **61e4693 — File Menu Implementation**.
  The checkout was clean before this handoff/acceptance documentation edit. The
  user committed the implementation, including the File left-inset correction.
  Recheck live state; preserve all tracked/untracked user work. Do not assume the
  dirty M2/M3 checkout described by older prompts still exists.
- Read root `AGENTS.md` and `winforms/AGENTS.md`. Use a single agent. No commit,
  push, publication, branch switch, stash/reset/clean, or new worktree unless asked.
- Work in `winforms/`. Keep WPF runtime/build/CI unchanged. WPF is a behavioral
  reference, not a runtime dependency or a template/control implementation to share.
- Use normal Debug outputs for local review. The user explicitly requests
  **relevant tests only**. Do not automatically run the full suite, Release,
  package campaign or unrelated regressions. Reuse passed evidence until a new
  change, failure or unresolved concern justifies repeating that scope.
- Keep reusable behavior/presentation in the native library. Showcase and another
  ordinary Form must obtain the same behavior through public APIs, without
  Showcase helpers/resources/internal access. Application commands, documents,
  data, icons and host-specific window/content integration stay application-owned.
- Accessibility/Narrator compatibility, broad theme consolidation/expansion,
  animation, optional window chrome/Mica and native touch/IME tests remain deferred.
  Retain the foundation bridge; do not add new providers/patterns or claim new
  accessibility compatibility. Actual VS, package and physical mixed-DPI/monitor
  acceptance stay separate from focused automated/native checks.

## Read bounded current sources

- `docs/21-NATIVE-WINFORMS-PLAN.md`: current banner, File/Backstage feature row,
  native ownership/architecture, section 6 and Deferred theming consolidation.
- `winforms/docs/M4-STATUS.md`: accepted M4A, M4B contract/evidence, File left-inset
  follow-up and remaining slices. This is the current continuation ledger.
- `winforms/README.md`: FileMenu, contextual tabs, QAT, native editors and relevant
  public-consumer verification guidance. Inspect M2/M3 ledgers only for affected
  behavior; their next-step text can be historical.
- Before code, follow root AGENTS' relevant design-index/history rule. WPF
  references for this slice include `src/RibbonKit/Controls/Backstage.cs`,
  `BackstageAdorner.cs`, `Ribbon.cs`, `Themes/Controls.Backstage.xaml` and the
  matching design-index/history entries. Inspect only what settles the current
  native behavior; do not start WPF implementation, broad audits or WPF tests.
- WPF checkbox/radio QAT ideas are intentionally deferred in
  `docs/22-FUTURE-WPF-IMPROVEMENTS.md`; do not implement that plan now.

## Implemented behavior to preserve

- M1/M2 provide the native adaptive ribbon, menus/splits/options, TextBox/ComboBox,
  passive ScreenTips, minimize, keyboard/KeyTips and shared renderer contracts.
  ScreenTips stay above library popups without stealing focus. The known
  unavailable-element COM exception was repaired; don't dismiss a reproducible
  exception as ordinary popup teardown or add broad accessibility work for it.
- ComboBox hover is separate from accepted selection; leaving clears hover and
  keyboard preview remains valid. Painting must not synchronize/move native
  editors, reset selection or produce startup/resize flicker.
- The body uses `BodyMargin` (default 7,1,7,7 logical pixels), downward spacing and
  inset separators accepted against the WPF reference. Preserve those geometries,
  native editor dimensions and the existing rounded-corner/edge-join fixes.
- M3 QAT supports TabRow/BelowRibbon, overflow, borrowed button/toggle/checkbox/
  radio/dropdown/split/ComboBox/group references and toolbar-only button/toggle
  commands. Sources own state, events, bindings and lifetime. Group QAT membership
  keeps the source collapsed until the last reference is removed. A source group
  and its references reuse a single live presentation and native editor handles.
- QAT check/radio references look like icon toggle buttons. Radios preserve their
  original exclusivity and cannot toggle themselves off. Checkboxes preserve the
  exact two/three-state cycle; mixed state has a small bar below the action icon.
- Toggles, check/radio options and checking split primaries retain QAT overflow
  for repeated activation. Ordinary commands dismiss it. Long QAT overflow uses
  opaque scroller buttons with hover/pressed feedback; unrelated menus keep their
  existing scrolling. Groups are addable only in future Customize QAT, never in
  Customize Ribbon's available-command list.
- Keep QAT declarations, measurement and painting independent of placement origin.
  Future title-bar QAT belongs to M6's optional RibbonForm/window integration.
  Do not add TitleBar to the current enum or require custom window chrome now.
- M4A contextual tabs expose `IsContextual` and `ContextualColor`, with binding and
  events. `Color.Empty` falls back to theme accent. Host `Visible`/selection governs
  applicability; hide/disable/remove uses normal deterministic tab fallback.
- M4B `RibbonControl.FileMenu` is a stable owned, content-serialized
  `RibbonDropDownItem`, hidden by default, Text File / KeyTip F. It joins model
  identity validation, binding context, editor ownership, designer association and
  disposal. `DrawFileButton` is the public header renderer hook. Opening File
  preserves selected tab; normal nested commands/editors, opening cancellation,
  lazy population, Escape and close events use the existing popup stack.
- **Latest accepted correction:** File's measured left edge follows scaled
  `BodyMargin.Left`. Its button, hit target and popup anchor align with the body;
  QAT/tabs follow its right edge. This also works minimized, with BelowRibbon QAT,
  narrow widths and zero/larger authored body margins. Do not put File back at x=0
  or replace measured geometry with a paint-only offset.
- The File menu is currently a command dropdown. Full Backstage and Office 2007's
  two-pane application menu/orb are distinct later surfaces, not existing parity.

## M4C: bounded Backstage foundation

1. Inspect the WPF behavior, then propose a native model/host/page API and its
   ownership, event and disposal contract before editing runtime APIs. Use normal
   CLR properties/events, owned collections, binding and native Controls.
2. Start with one Office 2024 Backstage design: open/close/back navigation, command
   entries, selected page and application-supplied native page content. Keep
   document loading/saving/recent data and page-specific actions in the consumer.
   Additional File designs, orb/two-pane legacy menus and effects remain separate.
3. Define how File chooses its existing command menu versus the new Backstage.
   Preserve existing FileMenu consumers, cancellation, metadata and keyboard
   behavior. Keep one visible File entry and library-owned routing; don't implement
   the feature solely as a Showcase click handler or duplicate header.
4. Explicitly settle native page Control ownership, parenting, reuse and disposal.
   A native HWND has one live parent. Hiding/changing/reopening pages must preserve
   values, bindings and editor state according to that contract. Do not clone
   command trees, dispose borrowed content accidentally or depend on WPF hosting.
5. Use an ordinary Form. Library code owns reusable navigation, page hosting,
   focus/keyboard/Escape behavior and presentation. If host participation is
   necessary, make the library contract small and explicit; keep document/content
   ownership with the application. Do not introduce custom title-bar chrome or a
   broad global coordinator as a prerequisite.
6. Define interaction with pending editor validation, existing menus/choice lists,
   minimize, resizing and DPI. Prevent overlapping active surfaces/stale focus and
   restore focus sensibly on close. Keep initial transitions immediate.
7. Demonstrate commands plus at least two native page examples through public APIs.
   Verify in the public-only consumer as well as Showcase. Add only API additions
   required for this bounded foundation, with XML docs, Unshipped baseline and
   appropriate designer/binding metadata. Put rendering and geometry in shared
   library code; comprehensive theme API consolidation remains later.

## Evidence and focused validation

Recorded evidence is historical, not permission to rerun everything:

- Latest normal Debug build: zero warnings/errors, including API/XML and design.
- Before the inset correction, six selected File/QAT/context cases were covered
  across an initial 5-pass/1-fail run and a 1/1 corrected test-only rerun. The
  fixture explicitly supplies BindingContext; this was not a runtime fix.
- Latest affected File layout case passed **1/1**:
  `winforms/tests/RibbonKit.WinForms.Tests/TestResults/m4b-file-left-inset.trx`.
- Public-only shown Office and neutral File checks passed at **120 DPI**, with
  alignment, popup anchor, nested commands/KeyTips, native field commit/binding/
  handle reuse, minimize, narrow/below geometry and disposal:
  `winforms/TestResults/visual/m4b-file-inset-{office,neutral}/result.log`.
- Inventory is **106** tests, not a new full-suite pass. Latest native evidence
  came from the Debug project runner, not a newly packed isolated consumer.
- The user accepted M3E option icons, M4A contextual tabs and M4B File presentation
  including its left inset. Earlier VS/hardware/accessibility limits stay open.

For M4C, build the native solution in normal Debug, select meaningful new and
actually affected regressions, and run bounded shown Office/neutral public-only
consumer checks. Inspect actual captures before claiming visual success. Check
API/XML/designer metadata as applicable. Record any remaining package/VS/hardware
scope plainly; do not run the broad `eng/Test-Native.ps1` campaign by default.

Existing focused native modes include `--file-checks` / `--file-checks-flat` and
`--contextual-checks` / `--contextual-checks-flat`; each takes an evidence folder.
Only rerun them if the Backstage change affects their contracts. Retain earlier
passing evidence. Keep input foreground guards: stop sending input if ownership
is lost. The user often uses the computer during tests; an isolated retry may be
appropriate, but a missed click/lost foreground alone does not justify a runtime
focus-stealing workaround. Do not close a user-owned Showcase to unlock builds.

Update the native README and M4 ledger with concise implementation, actual evidence
and remaining limits. Keep human acceptance separate. Finish with what changed,
where to try it and what was actually verified. No commit/push.

## Later sequence

The detailed working decomposition is in M4-STATUS.md. After M4C: M4D dropdown
galleries; M4E in-ribbon gallery/QAT; M4F merge/modal; M4G Compact/Touch;
M4H localization/RTL; M4I deferred appearance consolidation and theme/File designs.
M5 covers catalog/draft/storage/Apply/Cancel, QAT and Ribbon customization, then
Simplified Ribbon with its customization/persistence designed from the first slice.
M6 covers motion/reduced motion, optional RibbonForm/title-bar QAT and backdrop/
chrome integration. Optional ICommand, accessibility, VS/package acceptance and
physical hardware/release gates remain separately tracked. WPF work stays deferred.
