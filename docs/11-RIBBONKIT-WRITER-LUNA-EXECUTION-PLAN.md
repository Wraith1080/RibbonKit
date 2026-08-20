# RibbonKit Writer — Luna Execution Plan

> **Status:** durable execution decomposition created 2026-08-20. W0-A through W0-D and W1-A through W1-C are
> implemented. W1-D is planned and dependency-ready, but no agent or implementation for it is implied.
> This document does not schedule future agents or imply that any later Writer packet exists.
> [`10-RIBBONKIT-WRITER-PLAN.md`](10-RIBBONKIT-WRITER-PLAN.md) owns product scope; current
> implementation status remains in [`04-DESIGN-NOTES.md` §5](../04-DESIGN-NOTES.md#5-current-state--next-steps).

## 1. Purpose and authority

This plan converts the approved Writer milestones W0-W5 into bounded packets that can be dispatched
later to `gpt-5.6-luna` subagents. It preserves the product boundary, architecture and acceptance
criteria in the functional plan; it only defines execution order, ownership, hand-off evidence and
integration gates.

Before any packet begins, the lead agent must re-read, in this order:

1. The current user instruction and repository `AGENTS.md`.
2. `04-DESIGN-NOTES.md` §5 for live status and §3.87/§3.87a for Writer decisions.
3. `docs/10-RIBBONKIT-WRITER-PLAN.md` for product scope.
4. This execution plan for packet boundaries.
5. `docs/12-RIBBONKIT-WRITER-CONSUMER-FRICTION-LOG.md` for known integration observations.
6. The current worktree and any Writer files that may have appeared since this plan was written.

If those sources conflict, current user instructions and `AGENTS.md` win, followed by design-notes
live status. This plan must be corrected before execution rather than used to overwrite newer work.

The Luna model choice is an execution constraint requested by the user, not a product dependency.
Use the exact `gpt-5.6-luna` model at **max reasoning effort** when dispatching these packets. If that
model or effort level is unavailable in a future session, stop and ask for direction rather than
silently substituting another model or effort.

## 2. Execution contract

### Lead-agent responsibilities

The lead agent owns coordination and acceptance. It must:

- audit `git status --short` before every wave and preserve unrelated or dirty user edits;
- establish shared contracts and exclusive file ownership before spawning workers;
- dispatch only dependency-ready packets, normally to one to three Luna workers while retaining one
  coordination slot, or fewer when the active session has less capacity;
- inspect every worker diff and run the packet's checks itself before accepting it;
- integrate shared UI and project-file changes serially;
- run the wave and milestone gates, including the real Writer GUI surface where required;
- record surprising RibbonKit glue, timing workarounds or automation/testing exceptions in the
  consumer-friction log while evidence is fresh;
- record completed implementation in `04-DESIGN-NOTES.md`, not by turning this plan into a progress
  checklist; and
- stop at the requested packet, wave or milestone boundary instead of continuing automatically.

Subagents are ephemeral. This file persists the decomposition, but no worker remains queued between
turns. Execution starts only after an explicit instruction such as those in §11.

### Luna packet contract

Each Luna worker receives exactly one packet and must:

- read the packet's required source files before changing anything;
- stay inside its assigned paths and preserve all pre-existing edits;
- add or update focused tests with the implementation;
- use WPF commanding, MVVM-friendly state, dependency properties and lookless RibbonKit controls as
  required by the functional plan;
- avoid public RibbonKit runtime API changes, snapshot approvals, release actions and status claims;
- report a missing dependency, shared-file collision or architecture gap instead of inventing a
  cross-cutting workaround;
- run the narrowest relevant build/tests and `git diff --check`; and
- return changed files, commands run, results, unresolved risks and any manual verification still
  needed. The worker must not commit or declare its milestone complete.

Use Luna's **max reasoning effort** for every packet. This applies equally to ordinary implementation,
native-package safety and document-structure work; do not silently lower the effort for simpler
packets. A failed packet returns to the lead for diagnosis; do not repeatedly retry it with broader
scope.

### Stop and escalate when

- a packet requires a change under `src/RibbonKit/**` or a new public RibbonKit API;
- the assigned files contain overlapping dirty edits or another worker is changing them;
- native-package loading cannot meet the approved executable-content safety boundary;
- printing, DPI, RTL, accessibility or keyboard behaviour requires a live choice that tests cannot
  establish;
- acceptance would require weakening a test, changing an approved snapshot or expanding a tolerance;
- product scope would grow into DOCX, OLE, editable page-by-page layout or another explicit non-goal;
  or
- the packet cannot pass its gate without editing files owned by a later packet.

## 3. Planned repository shape and ownership locks

W0-A may refine names before implementation, but the default shape is:

```text
samples/RibbonKit.Writer/
├── App.xaml and App.xaml.cs
├── MainWindow.xaml and MainWindow.xaml.cs
├── Models/
├── Editing/
├── Persistence/
├── Services/
├── StructuredContent/
├── ViewModels/
├── Views/
└── Resources/

tests/RibbonKit.Writer.Tests/
├── Document/
├── Editing/
├── Persistence/
├── Pagination/
└── StructuredContent/
```

The following locks prevent shared-workspace collisions:

| Files or paths | Ownership rule |
|---|---|
| `RibbonKit.sln`, Writer `.csproj` files, manifest and test bootstrap | W0-A only during scaffolding; lead only afterward |
| `App.xaml*`, `MainWindow.xaml*`, primary ribbon dictionaries | One UI-integration packet at a time |
| `04-DESIGN-NOTES.md`, roadmap and Writer plans | Lead only, after verification |
| `src/RibbonKit/**` | Read-only unless the user separately approves the runtime-gap protocol |
| Existing visual snapshot approvals | Read-only; inspect actual/diff diagnostics before proposing change |
| Another packet's production or test folder | Read-only unless the lead explicitly transfers ownership |

Because SDK-style projects include new source files automatically, normal packets should add files in
their owned folders without touching project files. The test project should reference the Writer app
assembly; do not introduce a separate Writer core library unless a measured boundary requires it and
the lead approves the change.

## 4. Dependency map

Only packets whose dependencies have passed their integration gate are ready.

| Packet | Scope | Depends on | Default owner |
|---|---|---|---|
| W0-A | Solution and application scaffold | Preflight | Luna, exclusive |
| W0-B | Document session and lifetime model | W0-A | Luna |
| W0-C | TXT/RTF persistence and recent files | W0-B | Luna |
| W0-D | Shell, Backstage and file-command integration | W0-C | Luna, UI-exclusive |
| W1-A | Formatting command/state engine | W0-B | Luna |
| W1-B | Find/replace, spelling, counts and zoom | W0-B | Luna |
| W1-C | Home ribbon, QAT, KeyTips and editing integration | W0-D, W1-A, W1-B | Luna, UI-exclusive |
| W1-D | Writer iconography and first visual-polish pass | W1-C | Luna, UI-exclusive |
| W2-A | Page settings and validation | W0-B | Luna |
| W2-B | Versioned `.rkw` persistence | W0-C, W2-A | Luna, high-risk |
| W2-C | Centred paper editing surface | W1-D, W2-A | Luna |
| W2-D | Preview, pagination and printing | W2-C | Luna |
| W2-E | Layout/View ribbon and preview integration | W2-B, W2-D | Luna, UI-exclusive |
| W3-A | Images and hyperlinks | W1-D, W2-B | Luna |
| W3-B | FlowDocument table core | W1-D, W2-B | Luna, high-risk |
| W3-C | Table interaction and contextual Table Tools | W2-E, W3-B | Luna, UI-exclusive |
| W3-D | Structured-content round-trip and RTF fixtures | W3-A, W3-C | Luna |
| W4-A | Customization and appearance persistence | W3-D | Luna, UI-exclusive |
| W4-B | Automated integration and hardening | W4-A | Luna |
| W4-C | Manual GUI, DPI, RTL and performance acceptance | W4-B | Lead |
| W5-A | Distribution decision | W4-C and sustained use | User and lead |

Useful parallel groups after their dependencies pass are W0-C/W1-A/W1-B, W1-A/W1-B/W2-A,
W1-D/W2-A and W3-A/W3-B. Never parallelize two UI-exclusive packets. The lead may choose less
concurrency when a packet is high risk or the worktree is already dirty.

## 5. W0 — Application shell and document lifetime

### W0-A — Solution and application scaffold

**Owns:** `RibbonKit.sln`, `samples/RibbonKit.Writer/**` project/bootstrap files and
`tests/RibbonKit.Writer.Tests/**` project/bootstrap files.

**Deliver:** a `net8.0-windows` WPF executable with PerMonitorV2 manifest, `ProjectReference` to
`src/RibbonKit`, app-owned `Icons.xaml`, a minimal `RibbonWindow`, and a separate test project. Add no
placeholder feature buttons and no Writer-specific runtime API.

**Exit:** both new projects restore and build through `RibbonKit.sln`; the app reaches its actual empty
editor window; the test bootstrap runs; existing solution tests remain green.

### W0-B — Document session and lifetime model

**Owns:** `Models/`, document-session services/view models and matching `Document/` tests.

**Deliver:** `WriterDocument`, current path/format, dirty state, new/open/save lifecycle contracts,
close decisions and testable dialog abstractions. Content, page settings, appearance and ribbon
customization remain separate state.

**Exit:** tests cover new/clean/dirty transitions, successful and failed save, cancel/discard/save
close decisions, and switching documents without silent data loss.

### W0-C — TXT/RTF persistence and recent files

**Owns:** TXT/RTF format services, atomic-file helper, recent-file service and matching persistence
tests. Do not edit the main window.

**Deliver:** load/save for `.txt` and `.rtf`, explicit fidelity-loss signalling, atomic replacement,
corrupt/unreadable input handling and app-owned recent-file persistence.

**Exit:** round-trip fixtures, failure-path tests and atomic-save tests pass; `.txt` never claims to
preserve formatting; recent-file failures cannot prevent the editor from opening.

### W0-D — Shell, Backstage and file-command integration

**Owns:** shell view model, `App.xaml*`, `MainWindow.xaml*` and primary ribbon resources exclusively.

**Deliver:** New/Open/Save/Save As, recent files, dirty title, unsaved-close flow, Backstage shell and
basic status surface wired to W0-B/W0-C contracts.

**Exit:** lead manually creates, edits, saves, closes and reopens TXT and RTF files; cancel at every
unsaved prompt preserves the current document; the full solution build/tests pass.

## 6. W1 — Functional rich-text editing

### W1-A — Formatting command and selection-state engine

**Owns:** formatting commands/state adapters and `Editing/` tests; no main-window edits.

**Deliver:** clipboard, undo/redo, font family/size, bold/italic/underline, foreground/highlight,
alignment, indentation, lists and paragraph spacing using native `RichTextBox`/FlowDocument behaviour.
Represent uniform, mixed and unset selection state without coercing mixed selections.

**Exit:** focused STA tests cover empty, caret-only, uniform and mixed selections plus command enablement
and undo grouping.

### W1-B — Editing utilities and status state

**Owns:** find/replace, spelling adapter, word/character count, zoom model and their tests; no shared
ribbon files.

**Deliver:** predictable find/replace semantics, optional native spelling support, debounced counts and
bounded zoom. Long-document typing must not synchronously recount or repaginate on every keystroke.

**Exit:** tests cover case options, zero-length matches, replace-all termination, counts over paragraph
boundaries and zoom bounds.

### W1-C — Editing ribbon integration

**Owns:** Home ribbon and editing bindings exclusively after W0-D is accepted.

**Deliver:** Clipboard, Font, Paragraph and Editing groups; small Styles gallery only if functional;
QAT defaults; stable `Ribbon.CommandId` values; KeyTips, ScreenTips and Automation names. Do not expose
Format Painter or style-preview behaviour unless it is complete.

**Exit:** automated state tests pass; lead verifies mouse and keyboard formatting, complete Home KeyTip
traversal, QAT actions, minimized-ribbon behaviour and selection-state refresh in the real Writer app.

### W1-D — Writer iconography and first visual-polish pass

**Owns:** app-owned Writer icon resources, existing Home/QAT/status presentation and their focused
integration checks. It may refine `MainWindow.xaml*` and Writer resource dictionaries exclusively, but
must preserve all accepted W1-C command IDs, KeyTips, automation names, bindings and behaviour.

**Deliver:** replace provisional or overly generic W1-C glyphs with a coherent vector family using
consistent grids, stroke/fill weight and large/small variants. Give primary actions intentional visual
weight, balance group density and alignment, make foreground/highlight and checked/mixed/disabled state
cues legible, refine QAT silhouettes and status spacing, and establish reusable Writer-owned icon and
layout conventions for later tabs. Use theme resources and vector geometry; do not hardcode a single
palette, introduce raster assets, add fake commands/styles, or build a temporary page canvas that
conflicts with W2-C.

**Exit:** focused tests prove every W1-C command keeps its identity, KeyTip, automation name and state
semantics; the solution gate remains green. The lead compares before/after screenshots on the actual
Writer window at standard and narrow widths, checks normal/hover/pressed/checked/disabled, open-menu,
minimized-ribbon and QAT states at representative 100/150/200% DPI, and obtains user visual approval.
Application startup alone is not acceptance. W4 remains responsible for the final cross-theme,
RTL/accessibility and whole-product consistency pass after W2/W3 add their surfaces.

## 7. W2 — Native format and paper model

### W2-A — Document page settings

**Owns:** `DocumentPageSettings`, presets/conversion helpers and tests.

**Deliver:** A4, Letter, Legal and custom dimensions in 96-DIP-per-inch units; orientation swap;
margin validation; immutable or transaction-safe updates; no UI state in the model.

**Exit:** tests cover conversions, round trips, invalid dimensions/margins and orientation changes
without cumulative drift.

### W2-B — Versioned native `.rkw` persistence

**Owns:** `.rkw` package reader/writer, manifest/settings schema, safety validators, fixtures and tests.

**Deliver:** atomic ZIP package containing `content.xamlpackage`, `document-settings.json` and a small
manifest; schema-version checks; additive migration contract; corrupt, foreign, oversized and unsafe
input handling.

Before dispatch, the lead must approve the content-loading threat boundary: allowed package parts,
size/count limits, URI handling and the strategy that prevents arbitrary application types or
executable content from being instantiated. Luna must not invent or weaken this policy.

**Exit:** complete content/page-setting round trips pass; unknown mandatory versions and unsafe content
fail closed with user-actionable errors; temporary files cannot silently replace the source on failure.

### W2-C — Centred paper editing surface

**Owns:** paper editor host/control, its resources and focused layout tests. Main-window insertion is
performed only while this packet holds the UI lock.

**Deliver:** continuous `RichTextBox` editing centred on a neutral workspace, width/margins driven by
the page model and zoom, with native caret, selection, IME, spelling and clipboard behaviour. Do not
draw fake page breaks.

**Exit:** lead verifies editing and scrolling at multiple zooms and paper presets, including focused RTL
input and live DPI movement.

### W2-D — Preview, pagination and printing

**Owns:** preview-clone service, paginator/print service, preview view and tests.

**Deliver:** isolated preview document, deterministic page inputs, one/two-page/page-width modes,
navigation, debounced rebuild and printing from the same page settings/paginator. Report or explicitly
clamp printer imageable-area conflicts.

**Exit:** clone isolation and pagination-input tests pass; lead compares A4 and Letter preview with
Microsoft Print to PDF or an available printer. Opening a dialog is not proof of a correct print path.

### W2-E — Layout/View integration

**Owns:** Layout/View ribbon groups, preview switching and Backstage print/page-summary UI exclusively.

**Deliver:** paper size, orientation, margins, page colour, Edit/Print Layout, view mode and zoom commands
with stable command identities and keyboard metadata.

**Exit:** lead completes paper-setting, preview and PDF-print workflows from both ribbon and Backstage;
full build/tests pass before W3 begins.

## 8. W3 — Structured content

### W3-A — Images and hyperlinks

**Owns:** image/hyperlink insertion and editing services, dialogs/view models, persistence fixtures and
tests. Picture Tools remain absent unless selection, sizing and removal are reliable.

**Deliver:** portable image insertion, hyperlink creation/edit/removal and date/time insertion without
introducing OLE or executable attachments.

**Exit:** native round trips preserve supported content; RTF behaviour is recorded as best effort;
invalid image/URI input fails safely.

### W3-B — FlowDocument table core

**Owns:** table discovery, selection and mutation helpers plus table tests; no contextual-ribbon edits.

**Deliver:** 1×1 through 8×8 insertion, caret-to-cell resolution, row/column insert/delete, rectangular
merge, spanned-cell split, final-cell Tab row creation, sizing/alignment/padding/border/background and
row/column distribution. Every operation must leave a valid document tree and predictable caret.

**Exit:** structural invariants and edge cases pass STA tests, including spans, first/last row/column,
empty cells, invalid selections and undo/redo. Do not compensate for a broken algorithm in UI code.

### W3-C — Table interaction and contextual Table Tools

**Owns:** table grid picker, keyboard routing and contextual Table Tools UI exclusively.

**Deliver:** Layout groups for rows/columns, merge/split, size and alignment; restrained Design controls
for supported border/background choices; correct contextual visibility and KeyTip traversal.

**Exit:** lead verifies insertion, Tab/Shift+Tab, mutation, contextual-tab visibility and caret recovery
using mouse and keyboard, including a table spanning preview pages and focused RTL cell input.

### W3-D — Structured-content round-trip and compatibility fixtures

**Owns:** cross-feature `.rkw` fixtures, RTF compatibility fixtures and integration tests.

**Deliver:** representative documents containing formatted text, images, hyperlinks, page settings and
tables; documented fidelity loss for TXT/RTF; schema migration fixtures.

**Exit:** save-close-reopen preserves all native content and settings; corrupt and partial packages fail
without losing the current document; W3 manual acceptance and full solution tests pass.

## 9. W4-W5 — Product integration, hardening and decision

### W4-A — Customization and appearance persistence

**Owns:** app settings, Options UI and final customization integration exclusively.

**Deliver:** RibbonCustomizationSerializer only for ribbon structure; separate app-owned versioned
settings for theme, backdrop, window and other appearance preferences; import/export/reset boundaries
that cannot leak transient modal/merge state or document settings.

**Exit:** tests prove separation among document, ribbon and appearance state; lead verifies restart,
corrupt settings, reset and theme/backdrop behaviour.

### W4-B — Automated integration and hardening

**Owns:** Writer integration/accessibility contract tests and measured fixes within already-owned app
boundaries. Shared UI edits require the lead's temporary lock.

**Deliver:** keyboard/UIA names and states, RTL contracts, reduced-motion behaviour, DPI-safe metrics,
long-document debounce and pagination/performance instrumentation. Do not create visual approvals until
the lead has reviewed actual/diff artifacts from deterministic scenes.

**Exit:** full `dotnet build RibbonKit.sln` and `dotnet test RibbonKit.sln` pass with current counts
recorded as a dated checkpoint; no warning or `git diff --check` regression remains.

### W4-C — Manual Windows acceptance

This is a lead-owned verification packet, not delegatable completion evidence. Exercise the exact
matrix in the functional plan: ordinary multi-page note/letter use, native reopen, TXT/RTF export,
A4/Letter preview and printing, keyboard/KeyTips, 100/125/150/175/200% DPI, live monitor movement,
light/dark themes, RTL text/tables, reduced motion, narrow/minimized ribbon, QAT/customization and
outside-debugger startup/resize/long-document performance.

Failures return to the smallest owning implementation packet. Record the completed milestone and exact
verification evidence in a new design-notes §3 entry and update §5.

### W5-A — Distribution decision

After sustained real use, the user and lead decide whether Writer remains a source sample or receives
a portable GitHub artifact. This packet cannot be delegated to Luna and must not alter RibbonKit's
runtime release cadence. Publishing, tagging and release uploads require a separate explicit request.

## 10. Integration gates and runtime-gap protocol

At every accepted packet:

1. Confirm the worker touched only owned files and preserved the pre-wave worktree.
2. Review the diff for placeholder UI, dead commands, hardcoded theme values and app concepts leaking
   into RibbonKit.
3. Run focused tests and build the affected project.
4. At W0-W4 milestone boundaries, run `dotnet build RibbonKit.sln` and `dotnet test RibbonKit.sln`.
5. Run `git diff --check` and verify relevant Markdown links/line endings when documentation changed.
6. Reach the actual Writer workflow named by the packet; application startup alone is insufficient.
7. Update live status only after the gate passes. Agent reports and plans are not implementation
   evidence.

If Writer reveals a genuine RibbonKit runtime gap:

1. Record the observation and current app workaround in
   `docs/12-RIBBONKIT-WRITER-CONSUMER-FRICTION-LOG.md`.
2. Reproduce it from Writer with a focused failing consumer test or minimal scenario.
3. Show why an app-owned solution would violate WPF/RibbonKit architecture or duplicate library
   responsibility.
4. Stop the Writer packet and request approval for a separate additive runtime packet.
5. If approved, add XML documentation, library tests, an appropriate Showcase scenario and any visual
   verification before resuming Writer.

Never smuggle a runtime API change through a Writer packet merely because both projects share a
solution.

## 11. Durable dispatch prompts

Future turns may use one of these instructions:

- `Execute RibbonKit Writer W0-A with gpt-5.6-luna; stop after its integration gate.`
- `Execute the next dependency-ready Writer Luna packets in parallel; obey exclusive ownership and
  stop at the next wave gate.`
- `Resume RibbonKit Writer from live repository status; audit AGENTS.md and design-notes §5 before
  selecting Luna packets.`
- `Run Writer W4-C manual acceptance only; do not implement fixes until you report the failures.`

The lead should instantiate each worker prompt from this template:

```text
You are implementing RibbonKit Writer packet <ID> with gpt-5.6-luna at max reasoning effort.

Read repository AGENTS.md, 04-DESIGN-NOTES.md §5 and §3.87/§3.87a,
docs/10-RIBBONKIT-WRITER-PLAN.md, and packet <ID> in
docs/11-RIBBONKIT-WRITER-LUNA-EXECUTION-PLAN.md.

Objective: <single packet objective>
Owned paths: <exact paths>
Read-only paths: <exact paths, including src/RibbonKit unless separately approved>
Known pre-existing edits: <git status summary>
Dependencies already accepted: <IDs and relevant contracts>
Required tests/manual evidence: <packet exit criteria>

Stay within the packet. Preserve unrelated edits. Do not commit, edit live-status documents,
approve snapshots, broaden product scope, or change RibbonKit public APIs. If blocked by a shared
file, missing contract, security boundary or architecture decision, stop and report it.

Return: summary, changed files, tests/commands and results, unresolved risks, and manual checks still
required. Your report is input to the lead's integration gate, not completion evidence.
```

