# RibbonKit Writer packet map

This path retains the original Luna-plan identity for existing references. It now
contains delivery boundaries rather than duplicate progress reports and completed
scaffolding instructions. [Product scope](10-RIBBONKIT-WRITER-PLAN.md) and
[current status](../04-DESIGN-NOTES.md#5-current-state--next-steps) take precedence.

## 1. Execution agreement

Use the current user request and `AGENTS.md`. Single-agent execution is the default;
this historical plan does not itself request workers or select a model. If the user
explicitly requests the original Luna dispatch mode, its recorded setting is
`gpt-5.6-luna` at max reasoning unless the current request overrides it. Do not silently
substitute an unavailable explicitly requested model. No agents are queued by this file.

Read only the relevant product/packet/history sections and known friction. Resolve
routine implementation choices inside authorized scope. Preserve the current checkout;
completed packets are maintenance boundaries, not instructions to recreate files.

If delegation is requested, assign exclusive paths, pass pre-existing edits and
accepted dependencies, and serialize shared UI, project, documentation and WPF output
work. The integrating agent reviews evidence and runs additional checks when the diff,
failures or shared impact justify them; successful unchanged checks need not be duplicated.
Workers return changed behavior/files, checks/results, remaining risks and live gates.
Publication, snapshot approval and runtime scope do not follow from worker assignment.

## 2. Packet identities and dependencies

Historical dependencies describe integration order, not permission to reopen accepted work.
Current acceptance is recorded only in design-notes §5 and its linked evidence.

| Packet | Scope | Depends on |
| --- | --- | --- |
| W0-A | App/test/solution scaffold and manifest | Initial foundation |
| W0-B | Document lifetime and session | W0-A |
| W0-C | TXT/RTF, atomic saves and recent files | W0-B |
| W0-D | File-command shell/Backstage | W0-C |
| W0-E | Profiles and format transitions | W0-C, W1-A, W2-B |
| W0-F | New gallery and capability projection | W0-D/E, W1-C, W2-E |
| W1-A | Formatting/state engine | W0-B |
| W1-B | Find/spelling/counts/zoom | W0-B |
| W1-C | Home/QAT/KeyTip integration | W0-D, W1-A/B |
| W1-D | App iconography and hierarchy | W1-C |
| W1-E | Home completion and dialogs | W0-F, W1-C/D |
| W2-A | Page settings | W0-B |
| W2-B | Bounded versioned native persistence | W0-C, W2-A |
| W2-C | Default centered Paper surface | W1-D, W2-A |
| W2-D | Preview and printing | W2-C |
| W2-E | Page/View integration | W2-B/D |
| W2-F | Ruler and margin guides | W0-F, W2-E |
| W2-G | Default true editable pagination and remaining acceptance | W3-E |
| W3-A | Pictures, hyperlinks and date/time | W0-E, W1-D, W2-B |
| W3-B | Table structural core | W0-E, W1-D, W2-B |
| W3-C | Insert/Table Tools and cell navigation | W0-F, W2-F, W3-A/B |
| W3-D | Native structure and TXT/RTF fixtures | W0-E, W3-A/C |
| W3-E | Object context, Picture Tools and resizing | W1-E, W3-D |
| W4-A | Settings, appearance and customization | W3-E |
| W4-B | Automated integration/hardening | W4-A, W2-G |
| W4-C | Manual Windows acceptance | W4-B |
| W5-A | Distribution decision | W4-C and sustained use |

## 3. Ownership and safeguards

App code/tests remain in `samples/RibbonKit.Writer` and `tests/RibbonKit.Writer.Tests`.
Keep one owner for `App.xaml*`, `MainWindow.xaml*` and shared ribbon dictionaries at a
time. Project/bootstrap changes and status integration belong to the integrating agent
when workers are used. SDK source globs normally avoid project edits for new code.

`src/RibbonKit/**` remains read-only during Writer work without a focused reproduction
and explicit runtime authorization. Existing authorization need not be repeated.
Record observations in the [friction log](12-RIBBONKIT-WRITER-CONSUMER-FRICTION-LOG.md);
prove why the correction belongs in the library before changing that scope.
Do not weaken package safety, native history, paginator parity or snapshot tolerances
to pass a packet. Complete independent checks when hardware or a decision blocks a gate.

## 4. Remaining bounded work

### W1-E — Live dialog reacceptance

Automated evidence exists, but the record still lacks final live acceptance for the
corrected Font/Color/Paragraph dialogs and representative ribbon/context-menu states.
Verify the actual surface against the product contract before closing it. Named styles
remain excluded until their semantics and persistence are complete.

### W2-G — True editable pagination architecture and delivery

Continue from §3.159, not the original prototype. The user authorized default paginated
Paper on 2026-09-09. Normal startup enables it; `--writer-classic-paper` or environment
`RIBBONKIT_WRITER_PAGINATED_DIAGNOSTIC=0` restores the prior Paper surface.
One authoritative live `FlowDocument`/editor owns selection, input, native history,
spelling and clipboard. Immutable clone-backed pages and value-only geometry run on
a dedicated STA; generation checks reject stale work/events. Preserve accepted
preview/print page-start parity and current/adjacent interaction during reflow.
Never split into independent editors, inject blank blocks or draw fake page breaks.

Budget-aware speculative admission, insertion profiling, exact-map traversal parity and
native caret page following are implemented. Continue actual paginated-editing work with
focused regressions for changed behavior. Preserve the interaction floor, exact mapping,
latest-only rejection and native editing/history ownership.

The fixed-cadence rapid-scroll probe proposed after §3.157 is **deferred by user direction**.
It was a suggested validation method, not a pre-existing delivery prerequisite. Existing
cancellation/coalescing and latest-only coverage remains in use; do not require another
cache-budget benchmark matrix for unrelated implementation work. Real scrolling/authoring
responsiveness still needs acceptance after the user-directed default switch, but that does not
require this particular automated probe.

The retained scope includes cross-page editing/deletion/selection/history; focus and
command routing; page-setting reflow; table/picture/hyperlink hits and resize; rulers,
non-printing chrome, zoom/DPI transforms and empty/replaced documents. Trusted explicit
column geometry and the unsupported Auto/star resize policy remain separate. Test
cancellation/coalescing and page-window handoff, including dense long paragraphs.

Genuine OS IME and production RTL remain deferred together. W2-G is not closed by the
default switch. Its complete exit needs
the outstanding input/geometry and actual long-document authoring gates, not merely a
passing worker test. Record limitations rather than substitute decorative pagination.

### W4-B — Automated integration and hardening

The default-Paper decision is made; W2-G acceptance remains outstanding. Cover integrated native
persistence, keyboard/UIA state, RTL, reduced motion, DPI metrics and performance
instrumentation. Run the full solution build/tests with dated counts and inspect
visual differences before changing approvals. Correct warnings or explain external
locks/environment limits without claiming a clean gate.

### W4-C — Manual Windows acceptance

Exercise ordinary multi-page authoring, native save/reopen, TXT/RTF conversion,
A4/Letter preview/PDF and a physical driver, context actions/resizing, keyboard/KeyTips,
all required DPI scales and real mixed-monitor movement, themes/backdrops and relevant
RTL/IME. Review actual icon hierarchy, disabled states, Settings and narrow/minimized
surfaces. Automated tests and subagent reports cannot substitute for target-surface evidence.

### W5-A — Distribution decision

After sustained use, the user decides source sample versus portable artifact.
Tagging/upload/publishing needs an explicit request and does not change RibbonKit's cadence.

## 5. Handoff

A useful continuation names the packet, branch/tip, accepted state, exact owned paths,
pre-existing edits, objective, relevant history/friction, required evidence and stopping
boundary. Return what changed, checks that actually ran, and remaining acceptance.
Use [CONTRIBUTING.md](../CONTRIBUTING.md#proportional-validation) for command sequences.
Do not restart completed packets or impose the old full-suite-per-wave ritual on a
narrow repair unless the task's explicit gate or shared impact requires it.
