# RibbonKit Writer — Luna Execution Plan

> **Status:** durable execution decomposition created 2026-08-20. W0-A through W0-F, W1-A through W1-D and
> W2-A through W2-F plus W3-A through W3-C are accepted on the available hardware, including the 2026-08-26 W3-C
> corrective UI pass and its 2026-08-27 full regression/live visual reacceptance. Live mixed-monitor checking remains
> deferred to W4-C because only one display is connected. W1-E Home formatting implementation and its automated Debug
> gate are complete; the corrective app-owned RibbonKit-themed Font/Color/Paragraph pass has passed its refreshed full
> Debug gate and awaits live visual reacceptance.
> W3-D round-trip/TXT/RTF compatibility is accepted through design-notes §3.120. W3-E is planned next after
> W1-E live reacceptance for context-aware structured-object menus, stable contextual state and direct picture/table resizing.
> W2-G is planned after W3-E as a high-risk true editable-pagination packet; it may not fake page breaks.
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
| W0-E | Document profiles and format-transition policy | W0-C, W1-A, W2-B | Luna |
| W0-F | New gallery and capability-aware command projection | W0-D, W0-E, W1-C, W2-E | Luna, UI-exclusive |
| W1-A | Formatting command/state engine | W0-B | Luna |
| W1-B | Find/replace, spelling, counts and zoom | W0-B | Luna |
| W1-C | Home ribbon, QAT, KeyTips and editing integration | W0-D, W1-A, W1-B | Luna, UI-exclusive |
| W1-D | Writer iconography and first visual-polish pass | W1-C | Luna, UI-exclusive |
| W1-E | Home formatting completion and dialogs | W0-F, W1-C, W1-D | Luna, UI-exclusive |
| W2-A | Page settings and validation | W0-B | Luna |
| W2-B | Versioned `.rkw` persistence | W0-C, W2-A | Luna, high-risk |
| W2-C | Centred paper editing surface | W1-D, W2-A | Luna |
| W2-D | Preview, pagination and printing | W2-C | Luna |
| W2-E | Page/View ribbon and preview integration | W2-B, W2-D | Luna, UI-exclusive |
| W2-F | Margin guides and interactive horizontal ruler | W0-F, W2-E | Luna, UI-exclusive |
| W2-G | True editable pagination architecture and delivery | W3-E | Luna, high-risk, UI-exclusive |
| W3-A | Images and hyperlinks | W0-E, W1-D, W2-B | Luna |
| W3-B | FlowDocument table core | W0-E, W1-D, W2-B | Luna, high-risk |
| W3-C | Insert tab, structured-content interaction and contextual Table Tools | W0-F, W2-F, W3-A, W3-B | Luna, UI-exclusive |
| W3-D | Structured-content round-trip and RTF fixtures | W0-E, W3-A, W3-C | Luna |
| W3-E | Structured-object context, Picture Tools and direct resizing | W1-E, W3-D | Luna, UI-exclusive |
| W4-A | Customization and appearance persistence | W3-E | Luna, UI-exclusive |
| W4-B | Automated integration and hardening | W4-A, W2-G | Luna |
| W4-C | Manual GUI, DPI, RTL and performance acceptance | W4-B | Lead |
| W5-A | Distribution decision | W4-C and sustained use | User and lead |

Useful parallel groups after their dependencies pass are W0-C/W1-A/W1-B, W1-A/W1-B/W2-A,
W1-D/W2-A, W3-A/W3-B and W2-F/W3-A/W3-B after W0-F. Never parallelize two UI-exclusive packets. The lead may choose less
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

### W0-E — Document profiles and format-transition policy

**Status (2026-08-24): accepted.** Canonical Plain Text, Rich Text and RibbonKit Writer profiles now own
the shared format/extension and command/content/page-metadata capability matrix. Typed New and Save As use
those profiles, capability-set comparison centralizes profile-level conversion warnings, and failed or cancelled
conversions cannot change the active identity. W0-F must inject the single UI warning decider and remove the
legacy Plain Text-specific warning rather than creating a second prompt path.

**Owns:** document-profile/capability models, typed New/session contracts, format-transition policy and
focused document/persistence tests. It does not edit the main window or ribbon XAML.

**Deliver:** make Plain Text, Rich Text and RibbonKit Writer explicit creation profiles over the existing
`WriterDocumentFormat` identity. Define the commands, content and page metadata each profile can preserve,
create untitled documents with an explicit initial format, map each profile to its default extension and
centralize lower-fidelity conversion warnings. Save As may choose another supported format, but identity
changes only after the save succeeds. Keep content templates such as letters or reports as a separate,
future layer within a compatible profile.

**Exit:** tests cover all three typed-New paths, capability matrices, default extensions, upgrade and
downgrade decisions, cancelled/failed conversions and the invariant that failed saves cannot change the
active document profile.

### W0-F — New gallery and capability-aware command projection

**Status (2026-08-24): accepted.** Backstage New now presents three pictured, labelled, keyboard/UIA-described
profile cards with explicit KeyTips and a constrained wrapping/vertical-scroll layout. Typed New, default Ctrl+N,
profile extension defaults, the single W0-E conversion decider and group/tab-level capability projection are wired
without placing projection work on the typing/selection hot path. Writer observes one `IsBackstageOpen` transition
for busy-aware focus return; RKWF-011 records the missing RibbonKit close-completed event without authorizing runtime
work. Standard and 800×900 live surfaces, all three profile states and editor focus passed at the available scale.

**Owns:** Writer shell/MainWindow/Backstage and primary ribbon resources exclusively, plus their UI
integration tests. No `src/RibbonKit/**` change is permitted.

**Deliver:** replace the Backstage New action with a keyboard/UIA-accessible page of three labelled cards:
Plain Text, Rich Text and RibbonKit Writer. Each card uses an app-owned icon or small document preview and
creates the corresponding untitled profile. Ctrl+N and one-click New use the configured default profile.
Project profile capabilities into ribbon, Page/View, preview and future contextual command enabled state;
do not merely grey individual leaf buttons while leaving an apparently active unsupported group. Save
preselects the profile extension, while Save As keeps all supported formats available and surfaces the W0-E
loss decision before committing a lower-fidelity identity.

**Exit:** lead verifies all three New cards, Ctrl+N, unsaved-change cancellation, profile-specific group and
command state, Save defaults, cross-format Save As, narrow/minimized ribbon, KeyTips, UIA names/states and a
real reopen of each output. Full build/tests pass before W2-F or another UI-exclusive packet begins.

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

### W1-E — Home formatting completion and dialogs

**Owns:** the Home Font group, app-owned RibbonKit-themed Font/Color/Paragraph dialogs,
colour/highlight popups, the conditional Styles
surface, the modern base editor context menu and their command/state/UI tests exclusively. It may refine Home ribbon
layout and Writer-owned icons while holding the UI lock, but must preserve accepted command identities and
profile-capability projection.

**Deliver:** replace the static five-font list with a cached installed-font source and a virtualized/searchable popup
whose items render in their own font face; retain editable entry and honest current/mixed state. Expand size choices to
the conventional Office set while accepting validated finite custom values in the engine's supported range; add
grow/shrink only with deterministic step tests. Convert text/highlight colour into last-used primary actions plus a
concise base-standard/recent popup without accent/background or light/dark variants, Automatic/No Color and an
accessible **More Colors…** dialog with an HSV field/hue strip plus exact Hex/RGB entry. Add a Font group launcher and
transactional sample-preview dialog for supported character properties, including underline, single/double
strikethrough and mutually exclusive superscript/subscript with complete state, undo and native-format round-trip. Audit
the promised Paste split button, Styles gallery, Clear Formatting and Paragraph dialog rather than silently omitting
them; implement only complete commands with correct native undo, selection-state and persistence behaviour. Route
Tab/Shift+Tab while the editor owns focus so paragraph boundaries and paragraph selections indent/outdent and valid
list positions change nesting rather than moving focus into ribbon chrome. Do not solve this with an unconditional
`AcceptsTab`: define deterministic mid-paragraph/literal-tab behaviour and retain a documented keyboard focus-exit
path. Plain Text must not expose rich paragraph formatting, and table-cell Tab routing remains exclusively W3-C.
Replace the stock WPF editor menu with a Writer-owned modern context menu that snapshots the invocation target before
popup focus moves, preserves native spelling suggestions/actions, and projects Undo/Redo, Cut/Copy/Paste, Select All,
Clear Formatting and supported Font/Paragraph launchers through the existing command/capability state. Provide a
bounded extension contract for later table, picture and hyperlink rows; W1-E must not guess structured-object actions
or introduce a mini formatting toolbar.

**Exit:** tests cover installed-font enumeration failures/fallback, popup virtualization and own-face preview,
recommended/recent/current fonts, typed and listed sizes, invalid input, last-used colours, base-standard/recent and
custom colours, HSV conversion/pointer-field layout, Automatic/No Color, Font-dialog Apply/OK/Cancel, supported text
effects, mixed selections, one native undo unit and safe native-format round-trip,
profile enablement,
KeyTips, ScreenTips and UIA. Tab routing tests cover empty and populated paragraphs, paragraph selections, first and
nested list items, mid-paragraph/literal-tab behaviour, Plain Text versus RTF/RKW, undo/redo, retained caret/editor
focus, and the keyboard focus-exit path; popups, dialogs, Backstage and preview must not be intercepted. Lead verifies
ordinary text, spelling-error and unsupported-profile context menus using mouse, Shift+F10 and the Context Menu key;
opening or navigating the menu must preserve the intended selection and editor command target. Lead also verifies
standard/narrow/minimized ribbon, keyboard-only operation, high contrast, RTL and representative
100/125/150/175/200% DPI in the real Writer window. No unsupported Word effect may appear.

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

**Deliver:** preserve the current continuous editor presentation and add a centred Paper presentation
over the same live `RichTextBox`/document, with width and margins driven by the page model and zoom.
Switching modes must preserve caret, selection, undo history, IME, spelling, clipboard state and focus.
Do not draw fake page breaks.

**Exit:** lead verifies editing and scrolling at multiple zooms and paper presets, including focused RTL
input and live DPI movement.

### W2-D — Preview, pagination and printing

**Status (2026-08-24): accepted.** Preview and Microsoft Print to PDF use the same stable fixed paginator; A4 and
Letter preview/output plus all five pages of each PDF passed the live gate. W2-E consumed this contract and is
accepted separately below.

**Owns:** preview-clone service, paginator/print service, preview view and tests.

**Deliver:** isolated preview document, deterministic page inputs, one/two-page/page-width modes,
navigation, debounced rebuild and printing from the same page settings/paginator. Report or explicitly
clamp printer imageable-area conflicts.

**Exit:** clone isolation and pagination-input tests pass; lead compares A4 and Letter preview with
Microsoft Print to PDF or an available printer. Opening a dialog is not proof of a correct print path.

### W2-E — Page/View integration

**Status (2026-08-24): accepted.** The actual 125%-scale Writer surface passed Page/View switching, transactional
custom margins, page colour, preview modes/navigation/zoom, narrow/RTL, keyboard and icon-led Backstage checks. The
same long-lived editor and exact fresh W2-D paginator are retained. A same-day user-review correction moved preview-
only commands into a dedicated modal Print Preview tab, made View/preview/zoom commands large and labelled, replaced
Undo/Redo-derived page navigation artwork, suspended pagination work during ordinary typing, removed Print from the
ribbon and replaced the Windows picker's unsupported-preview pane with Writer-owned printer setup around the exact
fixed paginator. W0-E is accepted; W0-F is next and W2-F waits for its capability-aware command projection.

**Owns:** Page/View ribbon tabs and groups, view switching, zoom-command relocation and Backstage
print/page-summary UI exclusively.

**Deliver:** a Page tab for paper size, orientation, margin presets, a validated four-edge **Custom Margins…**
dialog and page colour; a View tab for Continuous Edit, Paper and Print Layout/Preview switching,
one/two-page/page-width modes and zoom. Custom-margin Cancel changes nothing; Apply commits one valid
`DocumentPageSettings` replacement. Move the existing
ribbon zoom controls from Home/Editing to View while retaining their command identities and the globally
available status-bar zoom control. Do not leave duplicate ribbon zoom controls behind. All relocated and
new commands retain stable keyboard and automation metadata.

**Exit:** lead completes continuous/paper/preview switching without content, selection, undo or focus loss;
paper-setting, zoom and PDF-print workflows pass from ribbon, status bar and Backstage; Home no longer owns
ribbon zoom controls; full build/tests pass before the next UI-exclusive packet begins.

### W2-F — Margin guides and interactive horizontal ruler

**Owns:** the app-owned paper content-boundary overlay, horizontal ruler/control and coordinator, the
corresponding View-tab toggles, transactional drag state and focused layout/interaction tests. It may edit
the Page/View ribbon surface only while holding the UI lock.

**Deliver:** an optional dotted content-boundary guide that follows the current page margins without
entering document content, persistence, clipboard, undo or print output. Add a Paper-view horizontal ruler
aligned with the paper, zoom and horizontal scroll position. It shows calibrated ticks, shaded margin zones
and first-line, hanging, left and right paragraph-indent markers. Page-margin dragging previews locally,
commits one validated `DocumentPageSettings` change on release and cancels on Escape/capture loss; paragraph
indent dragging uses the editor's undoable formatting path. Ruler/guide visibility is app view state for
W4-A persistence. Keep both out of Continuous/Preview when their geometry would be misleading. Do not add a
vertical ruler, mirrored/gutter margins or inert tab-stop markers. Reuse the prepared `Margins`, `Ruler` and
`Gridlines` resources unless live icon comparison justifies one dedicated margin-guide glyph.

**Exit:** deterministic tests cover normal/custom margins, portrait/landscape, zoom, horizontal scrolling,
RTL, high contrast, DPI rounding, mixed selections, drag commit/cancel/capture loss and undo/redo. The overlay
is non-hit-testable and absent from cloned preview/print output. Lead verifies ruler/guide alignment and
mouse/keyboard/UIA operation in the actual Writer window before W3-C begins.

### W2-G — True editable pagination architecture and delivery

**Owns:** a genuinely page-by-page editable Paper presentation and the reflow/virtualization architecture required
to support it. It begins only after W3-E stabilizes table/picture selection, contextual state and resizing; it does
not replace the accepted W2-D preview/print paginator or weaken its output contract.

**Deliver:** first prove the editing architecture in a bounded prototype before replacing W2-C. Keep one authoritative
document and paginator-consistent page geometry; do not split content into independently owned RichTextBoxes, inject
blank paragraphs, or draw decorative breaks. Caret and range selection must cross page boundaries while IME,
spell-check, clipboard, drag/drop, native undo/redo and command routing remain coherent. Reflow must follow paper size,
margins, zoom, font/paragraph changes and structured-object edits without losing the current editing target. Tables,
images, hyperlinks, W3-E adorners, margin guides and the horizontal ruler must follow page/scroll/DPI/RTL geometry,
while non-printing chrome stays out of `.rkw`, RTF, clipboard, preview and print. Virtualize or debounce long-document
layout from measured evidence. If stock WPF primitives cannot satisfy the proof, stop with the measured limitation
and an implementation design; never ship fake editable pages as a partial result.

**Exit:** focused tests cover cross-page typing/deletion, selection, undo/redo, IME composition boundaries, spelling,
clipboard, page-setting reflow, tables/images crossing or moving between pages, zoom/scroll/DPI/RTL, focus recovery and
document replacement. Compare editable page count and break positions against the accepted preview paginator for a
deterministic corpus. The lead verifies ordinary multi-page authoring and long-document responsiveness in the actual
Writer window before W4-B hardening.

## 8. W3 — Structured content

### W3-A — Images and hyperlinks

**Status (2026-08-26): accepted.** Portable images, safe/undoable hyperlinks, deterministic date/time insertion and
the bounded data-only native persistence extension pass the focused security/round-trip gate. UI presentation remains
outside this non-UI packet.

**Owns:** image/hyperlink insertion and editing services, dialogs/view models, persistence fixtures and
tests. Picture Tools remain absent unless selection, sizing and removal are reliable.

**Deliver:** portable image insertion, hyperlink creation/edit/removal and date/time insertion without
introducing OLE or executable attachments.

**Exit:** native round trips preserve supported content; RTF behaviour is recorded as best effort;
invalid image/URI input fails safely.

### W3-B — FlowDocument table core

**Status (2026-08-26): accepted.** The app-owned structural service passes its span/occupancy/caret/failure/native-
undo gate. Contextual ribbon interaction remains W3-C and table persistence/RTF compatibility remains W3-D.

**Owns:** table discovery, selection and mutation helpers plus table tests; no contextual-ribbon edits.

**Deliver:** 1×1 through 8×8 insertion, caret-to-cell resolution, row/column insert/delete, rectangular
merge, spanned-cell split, final-cell Tab row creation, sizing/alignment/padding/border/background and
row/column distribution. Every operation must leave a valid document tree and predictable caret.

**Exit:** structural invariants and edge cases pass STA tests, including spans, first/last row/column,
empty cells, invalid selections and undo/redo. Do not compensate for a broken algorithm in UI code.

### W3-C — Insert tab, structured-content interaction and contextual Table Tools

**Status:** accepted through `04-DESIGN-NOTES.md` §3.117, including the 2026-08-26 corrective UI pass and its
2026-08-27 full regression/live visual reacceptance. W3-D remains unstarted and still owns table round-trip/RTF
compatibility. A later user-observed contextual-tab flicker during Table Tools command execution is assigned to W3-E;
it does not authorize a RibbonKit runtime change without a focused reproduction.

**Owns:** the Insert tab, image/hyperlink/date-time command presentation, table grid picker, table keyboard routing
and contextual Table Tools UI exclusively.

**Deliver:** large labelled Picture, Hyperlink and Date and Time commands wired to the accepted W3-A services and
accessible app-owned dialogs; a 1×1–3×8 quick table gallery plus separated Custom Table entry for the supported
1×1–8×8 range wired to W3-B; Layout groups for rows/columns, merge/split,
size and alignment; restrained Design controls for supported border/background choices; correct contextual visibility,
profile gating and KeyTip traversal. Inside tables, Tab/Shift+Tab navigate cells, with deterministic final-cell row
creation; provide an explicit literal-tab path inside a cell and ensure this takes precedence over W1-E paragraph
indent routing. Picture Tools remain deferred until image selection, sizing and removal are reliable enough for a
real contextual workflow.

**Exit:** lead verifies Picture/Hyperlink/Date-Time and table insertion, forward/reverse cell navigation, final-cell
row creation, literal-tab entry, mutation, contextual-tab visibility and caret recovery using mouse and keyboard,
including a table spanning preview pages and focused RTL cell input. All Insert commands retain stable IDs, KeyTips,
ScreenTips, UIA names/patterns and profile-capability state.

### W3-D — Structured-content round-trip and compatibility fixtures

**Status (2026-08-28): accepted.** Native `.rkw` content schema v2 strictly round-trips supported tables alongside
formatted text, packaged images, safe hyperlinks and page settings; the outer manifest and settings schemas remain v1,
and v1 text-content fixtures still load. Inconsistent version declarations, v1 table injection, invalid spans/grids and
unsafe child objects are rejected before document-session replacement. TXT flattens tables to characters and RTF retains
representative table text while demonstrably losing merged geometry and exact outer styling, so both continue to advertise
table fidelity loss. The complete Debug gate passes Writer 396/396, RibbonKit 355/355 and visual 1/1 over 63 images.

**Owns:** cross-feature `.rkw` fixtures, RTF compatibility fixtures and integration tests.

**Deliver:** representative documents containing formatted text, images, hyperlinks, page settings and
tables; documented fidelity loss for TXT/RTF; schema migration fixtures.

**Exit:** save-close-reopen preserves all native content and settings; corrupt and partial packages fail without
losing the current document; the W3-D persistence/compatibility gate and full solution tests pass. Final structured-
object interaction and W3 manual acceptance remain W3-E.

### W3-E — Structured-object context, Picture Tools and direct resizing

**Owns:** structured-object hit testing and selection state, the W1-E context-menu extension rows, Picture Tools,
table/picture selection adorners, direct resizing and contextual-tab stability exclusively. It may refine Writer-owned
structured-content commands and icons while holding the UI lock; it must not change RibbonKit runtime code unless a
separate minimal reproduction proves a library defect and the user approves that runtime packet.

**Deliver:** classify each context-menu invocation from a stable editor target snapshot. Ordinary text retains the
W1-E menu; a table target adds only valid table actions such as row/column insertion or deletion, merge/split,
borders/background, sizing and table deletion; a picture target adds supported picture actions such as size,
replace/remove and properties; a hyperlink may add edit/open/remove when its safety contract permits. Reuse shared
text commands rather than cloning menus, preserve native spelling rows where applicable and recompute enabled state
against the captured document/selection before executing.

Introduce explicit picture selection and a real Picture Tools tab only for implemented size/remove/replace behavior.
Use non-printing `AdornerLayer` overlays for picture edge/corner handles and a table selection grip, row/column boundary
grips and an overall size grip. Picture corner drags preserve aspect ratio and edge drags change one axis. Table drags
reuse W3-B's bounded column-width and documented row-height-approximation semantics, respect content minimums and must
not imply a fixed WPF `TableRow.Height` that does not exist. Geometry follows Paper/Continuous presentation, zoom,
scrolling, RTL and per-monitor DPI. A drag previews without repeatedly dirtying the document, commits one bounded
native undo unit on release and restores the opening geometry on Escape, capture loss, document replacement, view
change or invalidation. Enforce minimum dimensions and page/editor bounds without corrupting FlowDocument structure;
keep ribbon or keyboard/UIA size alternatives for users who cannot operate pointer handles. Do not add wrapping, crop,
correction or rotation controls unless the underlying document and persistence contracts genuinely support them.

Stabilize contextual state across focus transfer into Table/Picture Tools and their popups. Suppress transient
selection publications while an app-owned structural replacement is in flight, execute against the captured valid
object context, then publish one final state. Keep the selected contextual tab when the committed caret/object remains
valid; collapse it and choose a deterministic normal-tab fallback only when deletion, document replacement, undo or
the final selection truly leaves that object. This is currently an app-owned Writer correction, not confirmed
RibbonKit friction.

**Exit:** deterministic tests cover text/table/picture/hyperlink menu composition and command state, mouse and
Shift+F10/Context Menu invocation, stale-target rejection, spelling coexistence, profile gating, undo/redo and focus
restoration. Realized tests cover Table Tools command execution without a Home-tab flash, popup/dropdown commands,
true object deletion fallback and rapid repeated mutations. Resize tests cover all handles, bounds, aspect behavior,
zoom/scroll/view changes, RTL, 100/125/150/175/200% DPI, capture loss, Escape, undo/redo, save-close-reopen and absence
from preview/print/UIA noise. Lead verifies the actual Writer window before W4-A begins.

## 9. W4-W5 — Product integration, hardening and decision

### W4-A — Customization and appearance persistence

**Owns:** app settings, the **Settings**-captioned customization dialog, its app-owned **Appearance**
page and final customization integration exclusively.

**Deliver:** RibbonCustomizationSerializer only for ribbon structure; separate app-owned versioned
settings for Office theme generation, light/dark-black palette, custom/default accent, accented title bar,
Backstage design/translucency, supported DWM backdrop, compatible window-frame/application-button
presentation, global ribbon animation level and respect-system-reduced-motion. Keep RibbonKit's built-in
**Customize Ribbon** and **Quick Access Toolbar** pages, add the **Appearance** page, and caption the host
dialog **Settings**; the page itself is not called Settings. Import/export/reset boundaries cannot leak
transient modal/merge state, document settings or appearance state.

Appearance editing is transactional: live preview may update the app and the open dialog, Apply/OK validates
and persists, Cancel restores the opening snapshot, and an Appearance-only defaults action does not reset
ribbon/QAT customization. Compatibility-dependent choices remain discoverable but disabled with an
explanation and a valid fallback rather than exposing a dead control.

**Exit:** tests prove separation among document, ribbon and appearance state plus snapshot rollback,
schema validation and unsupported-platform fallback. Lead verifies restart, corrupt settings, Appearance
defaults, Cancel rollback, live dialog re-theming, every theme generation/palette, Backstage design,
backdrop/frame compatibility, keyboard/UIA naming and reduced-motion behavior.

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

