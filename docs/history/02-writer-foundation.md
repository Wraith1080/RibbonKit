# Writer foundation and structured editing history

> Historical evidence, moved without changing its technical content on 2026-09-08.
> Dates, test counts, pending work, and instructions describe their original checkpoint;
> later entries may supersede them. Use [current status](../../04-DESIGN-NOTES.md#5-current-state--next-steps)
> and [AGENTS.md](../../AGENTS.md) for present work. Read only the relevant numbered entry.

### 3.99 RibbonKit Writer W0-A application scaffold — 2026-08-20

The first dependency-ready Writer execution packet is complete on `codex/ribbonkit-writer`.
`samples/RibbonKit.Writer` is a `net8.0-windows` WPF executable with a project reference to the
runtime library, a PerMonitorV2 host manifest, Office 2024 application-scope tokens, an app-owned
vector `Icons.xaml`, and a minimal `RibbonWindow` containing an empty ribbon shell and native
`RichTextBox` editing surface. `tests/RibbonKit.Writer.Tests` is a separate xUnit project and begins
with a scaffold contract proving that the main window derives from `RibbonWindow`. Both projects are
wired into `RibbonKit.sln` under dedicated solution folders.

The scaffold adds no file/document lifetime model, persistence, commands, Backstage content or
placeholder feature buttons. W0-B remains the next dependency-ready packet; this entry is not
evidence for any later Writer milestone.

The lead gate rebuilt the full solution with zero warnings/errors and passed **356 logic tests plus
one visual test covering 63 approved images**. The actual Writer executable opened a responsive
1375×900 `RibbonKit Writer` window; UI Automation exposed `DocumentEditor` as a keyboard-focusable
Document, live text entry succeeded, and the window closed normally.

### 3.100 RibbonKit Writer W0-B document lifetime model — 2026-08-20

Writer now has an app-owned, UI-independent document lifetime layer. `WriterDocument` owns a native
`FlowDocument`, nullable path, explicit Rich Text/Plain Text/RibbonKit Writer format identity and
dirty state. The document and `WriterDocumentSession` publish focused `INotifyPropertyChanged`
notifications so the later shell can bind the dirty title and current identity without moving UI
state into the model.

The session coordinates New, Open, Save, Save As and close requests through three narrow contracts:
document persistence, unsaved-change decisions and destination selection for an untitled Save. A
candidate loaded by Open does not replace the current document until loading succeeds. Save and Save
As commit clean/identity state only after persistence succeeds. Cancelled decisions, cancelled Save
As destinations, persistence cancellation and ordinary load/save exceptions preserve the current
document and dirty content; ordinary failures propagate while cancellation returns a non-destructive
false result. Document page settings, real TXT/RTF IO, recent files, dialogs and window integration
remain owned by later packets.

The first Luna draft exposed two contract gaps during lead review: an untitled Save decision had no
way to obtain a destination, and its asynchronous STA tests passed only because every fake completed
synchronously. The accepted version adds a destination-provider boundary and a timeout-bounded STA
runner with a live `DispatcherSynchronizationContext`. A final review added complete New/Open/Close
decision coverage, strict path/format validation and observable state changes.

The Writer project tests passed **25/25** five consecutive times. The full gate then passed **380
logic tests plus one visual test covering 63 approved images**, with zero build warnings/errors and a
clean `git diff --check`. W0-B changes no UI, so §3.99 remains the current live Writer surface gate.

### 3.101 RibbonKit Writer W0-C TXT/RTF persistence and recent files — 2026-08-20

Writer now implements the W0-B persistence contract for `.txt` and `.rtf` without coupling document
IO to the window. Plain-text loading is Unicode/BOM aware and saving removes only the structural
terminal paragraph break added by `FlowDocument`, so intentional final newlines survive repeated
round trips without accumulating new ones. A queryable format-capability record makes loss of
formatting, images, tables and page settings explicit before plain-text export. RTF uses WPF's native
`TextRange`/`DataFormats.Rtf` path for representative formatting, rejects corrupt input, and remains
explicitly best effort for advanced content. Native `.rkw` persistence still fails explicitly because
its versioned, security-gated implementation belongs to W2-B.

`AtomicFileWriter` serializes to a same-directory temporary file, durably flushes it, and only then
replaces or creates the destination. Producer failure and cancellation after a partial temporary
write leave an existing destination unchanged and clean temporary/backup artifacts. The recent-file
service reuses that helper for versioned app-owned JSON, stores canonical absolute paths and UTC
timestamps, orders newest first, de-duplicates paths case-insensitively, and applies a bounded
capacity. Corrupt or unreadable state degrades to an empty list; recoverable write failure returns
false and rolls back the in-memory candidate; cancellation propagates without losing the previous
list. All defined Writer formats are retained so W2-B can add `.rkw` without reopening W0-C.

Lead review required several correction passes plus a fresh Luna test-hardening pass to prove exact
TXT newline behavior, dispatcher affinity, atomic failure/cancellation, recent-file reload ordering,
failure isolation and the UI-thread deadlock regression. The accepted Writer suite passed **45/45**
five consecutive times. The full gate then passed **400 logic tests plus one visual test covering 63
approved images**, with zero build warnings/errors and a clean `git diff --check`. W0-C changes no UI,
so §3.99 remains the current live Writer surface gate.

### 3.102 RibbonKit Writer W0-D shell, Backstage and file-command integration — 2026-08-20

Writer now exposes the W0-B/W0-C document and persistence contracts through a real application shell.
`WriterShellViewModel` owns the observable dirty title, operation status, recent-file collection and
New/Open/Save/Save As/Exit commands behind one non-reentrant operation gate. The `RibbonWindow` uses
an ordinary Modern `Backstage` with recent documents and File actions, a compact Home/Document group,
Ctrl+N/Ctrl+O/Ctrl+S/Ctrl+Shift+S bindings, and a below-ribbon QAT Save command with an app-owned
vector glyph. Stable command IDs, KeyTips and Automation names/IDs cover the primary shell surface;
recent rows bind their full paths as unique UI Automation identities.

Native Open/Save dialogs keep the selected filter authoritative and normalize the destination
extension to `.rtf` or `.txt`. Plain-text export requires an explicit fidelity confirmation and RTF
status remains honest about advanced-content best effort. Successful explicit and implicit saves add
recents without changing the document result when recent persistence is unavailable; Save-before-Open
records the saved previous document before the newly opened target so the target remains newest.
Cancelled or failed New/Open/Close transitions preserve the current document and report a
non-misleading result. Dirty/path changes now update only the derived title; the editor replaces its
`FlowDocument` only when the session actually changes `CurrentDocument`.

Window close uses a cancel-first asynchronous decision followed by one guarded dispatcher close, so
Cancel leaves the window usable and an approved close or Exit cannot re-enter the prompt. Production
windows own their shell; the public injected-shell constructor leaves disposal with its caller. Shown,
arranged STA integration tests exercise the realized Backstage recent button through its Automation
peer, actual editor dirty/replacement behavior, command/gesture/QAT contracts, and clean/dirty/Exit
close completion rather than detached XAML objects.

The accepted Writer suite passed **91/91** across the lead's repeated focused runs, including **46
W0-D shell cases**. The final full gate passed **446 logic tests plus one visual test covering 63
approved images**, with zero build warnings/errors and a clean whitespace/CRLF check. On the actual
1375×900 Writer surface, the lead opened Backstage, entered content, saved and reopened RTF, rejected
then accepted the TXT fidelity warning, reopened TXT, confirmed the visible QAT Save glyph, cancelled
a dirty close without losing the document, and then discarded to close. No RibbonKit runtime change
was required.

### 3.103 RibbonKit Writer W1-A formatting command and selection-state engine — 2026-08-20

Writer now has an app-owned `Editing` layer over the native `RichTextBox`. `WriterEditingCommands`
provides stable routed commands for font family/size, solid foreground/highlight colours, alignment,
indentation, paragraph spacing, bullets and numbering; standard WPF commands remain authoritative for
clipboard, Select All and undo/redo. `WriterEditingAdapter` installs and removes the bindings without
owning the document, selection, IME, clipboard storage or native undo stack. Disabled and read-only
editors cannot be mutated through direct or routed adapter paths, while enabled read-only editors
retain Copy and Select All.

`WriterEditingState` distinguishes uniform, mixed, unset and explicitly unsupported projected values.
State observation does not coerce a mixed selection or create an undo unit. Caret-only inspection uses
adjacent native text formatting; paragraph state and mutation traverse only the selected structural
range through `TextPointer.Paragraph`, including Section, ListItem and TableCell content rather than
rescanning the whole document. Availability dependency-property changes and command requery refresh
the immutable snapshot; `RefreshState()` remains the deterministic clipboard refresh seam. Imported
gradient brushes stay untouched and report unsupported because Writer's W1 colour surface is solid
colour only.

Seventeen shown-STA tests cover empty, caret-only, uniform, mixed and default selections; routed and
direct command enablement; disabled/read-only transitions; clipboard refresh; disposal/unsubscription;
font, colour, alignment, indentation, spacing and list operations; Section/List/TableCell boundaries;
invalid WPF values; mutation-free state reads; and grouped multi-paragraph undo. An independent
max-effort review reproduced and drove corrections for disabled mutation, stale availability state,
whole-document paragraph scans and incomplete container traversal before acceptance.

The lead passed the focused W1-A suite **17/17 across five consecutive runs**, the full Writer suite
**108/108**, and the full solution at **463 logic tests plus one visual test covering 63 approved
images**, with zero build warnings/errors. A separately shown native editor then passed mixed-state
detection, routed bold normalization, parameterized font/alignment formatting, grouped undo,
disabled-command rejection and TableCell paragraph formatting. W1-A deliberately makes no MainWindow
or ribbon-XAML change; that real Writer integration remains W1-C. No RibbonKit runtime change was
required.

### 3.104 RibbonKit Writer W1-B editing utilities and status state — 2026-08-20

Writer now has four app-owned editing utilities without changing the shell or shared ribbon. The
ordinal `WriterFindReplaceService` defines explicit current/after-selection/document-start and wrap
semantics, rejects empty queries, collects replace-all matches once and applies them from the end in
one native undo unit. Its canonical FlowDocument snapshot preserves formatting boundaries, represents
paragraph and soft-line breaks predictably, and inserts non-text barriers around embedded UI, images,
Figures and Floaters so ordinary text searches cannot bridge across or delete them. Matches that cross
a structural paragraph boundary can be selected for Find but are never replaced; the post-replacement
caret follows the actual native range end for plain, multiline and surrogate-pair text.

`WriterSpellCheckAdapter` only controls WPF's native spelling property, observes direct native changes,
restores the original value on disposal and leaves selection, caret, IME and dictionaries to WPF.
`WriterDocumentStatistics` publishes explicit Unicode word and text-element counts: spaces, tabs and
soft line breaks count as characters, while structural paragraph breaks and embedded objects do not;
objects still separate adjacent words. Text changes perform only constant-time generation and
trailing-edge scheduling. A dispatcher-affine 250-ms callback is cancelled and replaced on each edit,
and pending identity, document identity and generation checks reject stale work. `Refresh()` is the
explicit seam when W1-C replaces `RichTextBox.Document`. `WriterZoomModel` is UI-independent state with
a 25-400% range, 100% default, ten-point steps, finite-input rejection, clamping and change-only
notifications.

An independent max-effort review reproduced and drove corrections for an invalid default
`DispatcherTimer` path, embedded-object deletion, replacement-caret overshoot, throttle rather than
trailing-edge debounce behaviour, unenforced generations, duplicated List/TableCell separators and
unobserved external spelling changes. Thirty STA/model tests now cover those regressions plus case and
wrap options, empty and current selections, replace-all termination/undo, LineBreak/List/TableCell and
cross-run text, empty documents, Unicode/apostrophe counts, document replacement, disposal and zoom
bounds.

The lead passed the focused W1-B suite **30/30 across five consecutive runs**, the full Writer suite
**138/138**, and the full solution at **493 logic tests plus one visual test covering 63 approved
images**, with zero build warnings/errors and a clean whitespace check. A separately shown native
editor then passed real find/replace, embedded-object preservation, the default debounced count path
(three words/seventeen characters), native spelling and 135% zoom together. W1-B deliberately makes no
MainWindow or ribbon-XAML change; W1-C owns that integration and must call the statistics `Refresh()`
seam after replacing the editor document. No RibbonKit runtime change was required.

### 3.105 RibbonKit Writer W1-C editing ribbon integration — 2026-08-21

Writer now exposes its accepted W1-A/W1-B editing services through an app-owned Home ribbon. The
Clipboard, Font, Paragraph and Editing groups cover native clipboard/undo, family and point-size
selection, bold/italic/underline, solid foreground and highlight colours, alignment, indentation,
lists, paragraph-spacing presets, find/replace, spelling and Select All. Stable QAT Save/Undo/Redo
commands remain usable below a minimized ribbon. Every W1-C action has a command identity, KeyTip,
ScreenTip and automation name; incomplete Styles and Format Painter surfaces were not added.

`WriterEditingRibbonController` bridges the realized controls to the existing editing contracts
without taking ownership of document content, selection, caret, native undo or IME. It converts the
point values displayed by the ribbon to WPF device-independent units, projects uniform and mixed
selection state, gates mutations for disabled/read-only editors, refreshes statistics after document
replacement, and restores editor focus at dispatcher context-idle so terminating KeyTip input cannot
enter the document. Editable family/size combos commit only at an explicit Enter, drop-down-close or
focus-loss boundary, preventing partial typed values from redirecting the remaining keystrokes into
the editor. The Writer-owned find/replace dialog leaves Find available while disabling replacement in
read-only mode.

An independent max-effort Luna review reported no priority-one defects and drove corrections for
missing colour/highlight/spacing surfaces, read-only replacement, focus restoration, point/DIP
semantics and integration-test coverage. Its targeted follow-up found one remaining point-comparison
edge, which was corrected. Lead live verification then exposed and corrected the KeyTip focus timing
and editable-combo commit races before acceptance.

The final Writer suite passed **138/138** and the full solution passed **493 logic tests plus one
visual test covering 63 approved images**, with zero build warnings/errors. On the actual Writer
window, mouse and full Home KeyTip formatting, family/size entry, colour/highlight/spacing, mixed and
caret-only selection state, find/replace, spelling, live counts, zoom, minimized-ribbon QAT
undo/redo, TXT/RTF document replacement, Save/Open/New and dirty-prompt Cancel/Discard paths all
passed. No RibbonKit runtime or project-file change was required.

### 3.106 RibbonKit Writer W1-D iconography and first visual-polish candidate — 2026-08-21

Writer now uses one coherent app-owned vector family for all accepted W1-C commands. After the user
rejected the first monochrome/accent-heavy draft as too basic, the lead redesigned the artwork directly
as layered multicolor drawings rather than delegating another visual pass. The resources share a 24-unit
geometry convention, rounded stroke treatment, a restrained Writer-owned semantic brush palette and
purpose-sized large variants for primary actions. Paste now leads the Clipboard group at Large size,
Cut/Copy form the supporting stack, and Font, Paragraph and Editing retain their accepted commands,
IDs, KeyTips, ScreenTips, automation metadata and state behavior. QAT silhouettes and the status bar
received the same restrained spacing and hierarchy pass without adding a fake document canvas ahead
of W2-C.

`Icons.xaml` now contains **101 vector resources**: 96 small/general icons plus five explicit Large
variants. Sixty-seven reserves cover the planned file/Backstage, page-layout, preview/view, image/link,
table-structure, Table Tools, appearance and general-action surfaces through W4. The catalog and reuse
rules live beside the app in `samples/RibbonKit.Writer/ICON-CATALOG.md`; future packets should reuse these
before drawing near-duplicates.

The next live reviews found that the direct redesign had swung too far toward variation and then remained
too bright and contrast-heavy. The implemented follow-up limits the complete catalog to dark-grey structure,
one muted-blue action/detail accent and a muted amber reserved for semantic emphasis. Related commands now
share a stable treatment: undo/redo, bold/italic/underline, all four alignments, bullets/numbering,
find/replace and all three zoom actions. Their geometry communicates the operation; color communicates role
instead of changing from sibling to sibling or group to group. Ordinary Home/QAT commands therefore share
one ink/blue identity across Clipboard, Font, Paragraph and Editing.

All reusable palette pens now use the same 1.4-unit round-cap/round-join treatment. Alignment, list, indent,
paragraph, find and zoom drawings were converted from visually heavy filled bars or lenses to this shared
stroke system; semantic underline/highlight accents use it too. Bold letterforms and filled directional
silhouettes remain intentionally solid because their weight carries meaning rather than simulating a line.

Backstage recent items remain real focusable WPF buttons for keyboard and Invoke semantics, but their
native rectangular chrome is replaced by a Writer-owned row template. Each row now presents a document
glyph, filename, containing folder, format and last-used time with trimming, a full-path tooltip and
full-path automation help text; an explicit empty state covers a new installation. Computed presentation
properties are excluded from the versioned recent-file JSON, so persistence compatibility is unchanged.

An independent max-effort Luna review found no blocking defects and confirmed the packet boundary,
dynamic-resource ownership and persistence shape. The actual Writer window passed standard and narrow
layout inspection, recent-row hover and mouse activation, disabled commands, minimized-ribbon/QAT and
Backstage reflow. External `UIAutomationClient` traversal repeatedly omitted the arbitrary recent-page
button subtree even though direct peers and actual activation passed; this is recorded as open consumer
evidence in `docs/12-RIBBONKIT-WRITER-CONSUMER-FRICTION-LOG.md` RKWF-004 rather than expanding W1-D into
RibbonKit runtime work. Writer's existing PerMonitorV2 application manifest now also requests Windows
Common Controls v6, so the real WPF `MessageBox` confirmation path uses current themed native buttons
without replacing app dialog code. The final Writer suite passed **140/140** and the full solution passed **495
logic tests plus one visual test covering 63 approved images**, with zero build warnings/errors and a
clean diff check. No RibbonKit runtime or project-file change was required.

The user accepted the final dark-grey, muted-blue and muted-amber live screenshots on 2026-08-24,
closing W1-D's visual acceptance gate.

### 3.107 RibbonKit Writer W2-A page-settings model — 2026-08-24

W2-A adds a UI-independent, immutable `DocumentPageSettings` model without changing the live editor or
RibbonKit runtime. Named A4, Letter and Legal factories use 96-DIP-per-inch physical conversion; custom
paper accepts explicit portrait-basis dimensions. The model stores one canonical portrait basis and only
projects `WidthDip`/`HeightDip` for orientation, so even repeated portrait/landscape toggles cannot accumulate
floating-point drift. Margins are immutable values validated against the effective orientation and must leave
positive content width and height. Failed preset, orientation, custom-size or margin updates throw before a
replacement instance exists, preserving the prior valid settings.

`DocumentLength` provides finite, non-negative inch/millimetre/DIP conversions and rejects converted overflow.
The W2-A gate covers named physical sizes, conversion round trips, custom paper, immutable preset/margin updates,
invalid dimensions, invalid margins, invalid enum values, orientation-dependent margin rejection and 1,000
orientation toggles. W2-A remains deliberately disconnected from `FlowDocument` and persistence: W2-C owns
applying settings to the editor/paper surface, while W2-B owns the versioned `.rkw` schema and safety boundary.

The same visual review identified a missing first-class separator for command clusters inside a `RibbonGroup`.
That consumer request is recorded as RKWF-005 in the Writer friction log for a separately approved
`RibbonGroupSeparator` investigation. W2-A makes no changes under `src/RibbonKit/**`.

The final Writer suite passed **169/169** and the full solution passed **524 logic tests plus one visual test
covering 63 approved images**, with zero build warnings/errors.

### 3.108 RibbonKit Writer W2-B versioned native persistence — 2026-08-24

W2-B makes `.rkw` a reachable Open/Save/Save As format and persists the document-owned page-settings model.
The deterministic outer ZIP contains exactly `manifest.json`, `document-settings.json` and
`content.xamlpackage`. The manifest identifies RibbonKit Writer, separates manifest/content/settings schema
versions, records the minimum reader version and reserves an explicit required-feature list. Version 1 is a
closed schema: unknown, duplicate or missing fields fail closed, and later migrations must add an explicit
version-switch branch rather than guessing at unknown content. Page settings store named/custom portrait-basis
dimensions, orientation and margins; named dimensions are revalidated against canonical presets.

The native load boundary snapshots at most 64 MiB through one bounded file handle. Both ZIP layers require exact,
case-sensitive part sets, reject duplicates/case collisions/unexpected paths and cap expanded content. Inner
relationships and content types must match the WPF-generated text-package shape. `Document.xaml` is decoded as
strict UTF-8, parsed with DTD/entity resolution disabled, bounded for depth/elements/attributes/text and checked
against a presentation-namespace allowlist. Only current text primitives (`Section`, `Paragraph`, `List`,
`ListItem`, `Run`, `Span`, `Bold`, `Italic`, `Underline` and `LineBreak`) and bounded primitive formatting values
are manually reconstructed. Untrusted native content never reaches `XamlReader` or `TextRange.Load`; arbitrary
CLR types, markup extensions, event hooks, external URIs, images and tables fail closed. W3 will deliberately
extend this allowlist for its owned image/table structures instead of weakening the v1 boundary broadly.

Save first creates WPF's trusted in-memory XamlPackage, runs that generated content back through the same safe
reader and only then uses the existing same-directory atomic replacement. Unsupported current content,
serialization failure and cancellation therefore cannot replace an existing destination. Dialog routing,
recent-file identity and open/save status now distinguish `.rkw` from RTF/TXT. W2-B restores page settings as
document metadata; W2-C remains responsible for applying them to `FlowDocument` and the centred paper surface,
so no live editor/layout claim is made here. No `src/RibbonKit/**` change or new consumer-friction entry was
required.

The final Writer suite passed **192/192** and the full solution passed **547 logic tests plus one visual test
covering 63 approved images**, with zero build warnings/errors and a clean diff check.

### 3.109 RibbonKit Writer W2-C centred paper editing surface — 2026-08-24

W2-C adds an app-owned `WriterEditorSurface` around the one long-lived native `RichTextBox`. The actual Writer
window now opens in Paper mode: the current document's logical width, height and physical margins are applied to
`FlowDocument.PageWidth`, `PageHeight` and `PagePadding`, while a white paper surface is centred over the
theme-aware companion workspace. Continuous mode restores the previous workspace-filling editor padding and
unconstrained document layout. Switching presentation never replaces the editor or document, so native selection,
caret, undo, IME, spelling, clipboard and automation ownership remain with the same WPF control.

The existing editing controller continues to own the editor's single `LayoutTransform`. The paper host scales only
its logical chrome dimensions, preventing double zoom. Paper mode uses one outer horizontal/vertical scroll surface
and disables the editor's inner vertical scrollbar; Continuous mode restores the inverse arrangement. This keeps
Letter/A4 paper and long continuous content reachable without nested vertical scrolling or fake page breaks.
`MainWindow` also observes the current `WriterDocument.PageSettings`, detaches on document replacement/close and
reapplies the model without marking the document dirty.

Independent review caught and corrected two first-pass defects before acceptance: fitting paper was not guaranteed
to centre through the viewport alignment, and paper taller than the window could be clipped below the viewport.
Eight focused surface tests now cover hosted centring, Letter/A4 vertical reachability, zoomed horizontal overflow,
long-content growth, real edit/undo and selection preservation, focus/editor-state preservation, page margins,
document replacement and single-scale controller zoom. The real `MainWindow` integration test also proves live
page-setting propagation. The focused W2-C plus window-integration gate passed **9/9** and the final application
capture passed at the current **125%** desktop scale. The solution builds with zero warnings/errors; RibbonKit passed
**355/355** and the visual suite passed its one test covering 63 approved images.

The clipboard lock later cleared and the unchanged final W2-C tree passed the complete Writer suite **200/200**.
W2-C is therefore accepted on the available hardware and W2-D is dependency-ready. Only one 125%-scale display is
connected, so live mixed-monitor movement could not be performed; that hardware-only check remains explicitly deferred
to W4-C's cross-DPI acceptance rather than being represented as completed. No RibbonKit runtime change or new
consumer-friction entry was required.

### 3.110 RibbonKit Writer W2-D stable preview, pagination and printing — 2026-08-24

W2-D adds an app-owned preview pipeline that clones the trusted live `FlowDocument`, reapplies its effective root
formatting and the immutable page settings, and eagerly serializes the resulting one-column paginator into an owned
in-memory XPS package. The published snapshot exposes the package's `FixedDocumentSequence` and stable fixed paginator;
the mutable source clone remains internal. Preview and printing therefore consume the exact same isolated fixed pages
without attaching the live editor document to another viewer. A fixed-sequence paginator's generic `PageSize` remains
WPF's Letter-sized suggested metadata, so correctness is intentionally verified against each actual
`GetPage(index).Size`, which matches A4, Letter and landscape settings.

`WriterDocumentPreviewView` hosts real `DocumentPageView` instances inside a scrollable surface and supports one-page,
two-page and page-width modes, one-based navigation, and bounded 10–500% zoom. `WriterPreviewController` debounces
rebuilds, rejects stale generations and withholds the current snapshot while a rebuild is pending. Snapshot replacement
is synchronous: a bound view must detach or replace its page views during `SnapshotChanged` before the controller
disposes the prior XPS package. W2-E must likewise detach the view before disposing its controller.

The print boundary merges and validates the selected `PrintTicket`, reports missing capabilities, all four printable-area
margin conflicts and page-size mismatches as structured results, and supports report-only or reject behavior. A
landscape snapshot uses a matching landscape ticket, and the print service submits the snapshot's exact paginator.
Independent review also tightened stale-snapshot printing, nullable capability handling, zoom bounds, clone language
fidelity and snapshot lifecycle ownership. A dedicated xUnit collection serializes the WPF XPS tests after parallel
first-use exposed an intermittent framework parser race.

The focused preview/printing gate passes **19/19**. The live 125%-scale proof compared both the preview and every page of
Microsoft Print to PDF output: A4 produced five clean 595.276 × 841.89-point pages and Letter produced five clean
612 × 792-point pages, with no device page-size or imageable-area conflict. The page-four reserialization regression
specifically guards the raw-flow-paginator corruption found during the first A4 proof. W2-D is accepted and W2-E is
dependency-ready but has not started. No `src/RibbonKit/**` change or new consumer-friction entry was required.

### 3.111 RibbonKit Writer W2-E Page/View ribbon and preview integration — 2026-08-24

Writer now exposes the accepted page-settings and fixed-preview contracts through app-owned Page and View tabs.
Page offers A4/Letter/Legal size, portrait/landscape orientation, four margin presets, a transactional four-edge
Custom Margins dialog and White/Ivory/Light Blue page colours. The dialog validates all physical edges immediately,
disables Apply for invalid or non-fitting values, previews the current page orientation and commits one immutable
`DocumentPageSettings` replacement only after Apply. Page changes update the one editor surface, preview rebuild and
Backstage summary; page colour is carried by the isolated preview clone used for exact-paginator printing.

View switches the same long-lived native `RichTextBox` among continuous editing, centred paper and fixed print
preview without replacing its document or sacrificing selection and undo state. The accepted W2-D view supplies
one-page, two-page and page-width modes, one-based navigation and active-mode zoom. Ribbon zoom moved from Home to
View with its existing `Writer.Home.Editing.Zoom*` command identities, while the status-bar zoom remains global.
Pending content/settings rebuilds clear the visible preview, disable both print commands and must satisfy
`TryGetCurrentSnapshot` before and after the native print dialog, preventing an older paginator from being submitted.
Document replacement also detaches the view before changing controller ownership, and teardown detaches the view
before the owned XPS snapshot is disposed.

Backstage now presents a compact primary Print action and outlined preview action with Writer vector icons instead
of stretched generic buttons, plus the current paper/orientation/page-count/margin/colour summary. New and relocated
actions retain command IDs, unique KeyTips, ScreenTips and automation names/IDs. External `UIAutomationClient`
traversal of the actual 125%-scale Writer window still exposed the Page/View tabs but not their realized leaf
commands; the working direct peers, pointer and KeyTip paths plus the remaining investigation are recorded as
RKWF-006 without changing `src/RibbonKit/**`.

The focused Page/View, preview and window-integration gate passes **20/20**. The actual Writer surface passed Page,
Custom Margins valid/invalid/Cancel, View/preview, status zoom, keyboard tab selection, standard and 620-pixel narrow
layouts, RTL and Backstage checks at 125% DPI. Integration tests prove Backstage and ribbon printing submit the exact
fresh preview paginator, including coloured-page settings; W2-D's retained live A4/Letter Microsoft Print to PDF
proof remains the device-output evidence. Independent read-only review identified and closed pending-overlay,
automatic-zoom notification, clone-failure staleness and Backstage metadata gaps. W2-E is accepted and W2-F is
dependency-ready. No RibbonKit runtime change was made.

### 3.111a Writer W2-E modal-preview, typing-performance and print-setup correction — 2026-08-24

User review of the accepted W2-E surface found four concrete UX problems: View used icon-only-looking small stacks,
preview-only commands remained visibly disabled during editing, Previous/Next reused Undo/Redo artwork, and the
Windows 11 WPF print picker displayed an empty pane claiming that the app did not support print preview. Writer now
uses RibbonKit's existing modal-tab contract instead. The ordinary View tab has three large labelled Document Views
commands and three large labelled Zoom commands. Entering Print Preview activates a dedicated modal tab, hides File
and ordinary tabs, exposes large One Page/Two Pages/Page Width, distinct document-arrow Previous/Next and preview
zoom commands, and uses the built-in Close Print Preview affordance to restore the prior Continuous or Paper view.
Print remains exclusively in Backstage.

Preview pagination is now demand-driven. `WriterPreviewController` still observes every document/settings change and
immediately makes an older snapshot ineligible for printing, but while ordinary editing is active it does not create,
cancel or run debounce timers and does not clone/XPS-paginate on the UI thread. Repeated typing coalesces as dirty
state; entering modal preview or selecting Backstage Print schedules one trailing-edge rebuild. Leaving those surfaces
suspends any pending work without exposing the older snapshot. This removes the visible print-enabled flicker and the
correlated typing stalls while preserving the strict `TryGetCurrentSnapshot` print gate.

Backstage Print now opens a Writer-owned print setup window rather than the Windows WPF picker. It enumerates the
installed queues, selects the default printer, shows page setup and fits the exact current fixed paginator in a real
preview pane with navigation. Accepting still creates a validated WPF `PrintTicket`, analyzes device limits and submits
the same snapshot paginator through `WriterPrintService`; cancel detaches the preview without disposing the
controller-owned snapshot. The standard WPF `PrintDialog` does not accept a paginator until after `ShowDialog`, which
is why Windows could not populate its integrated preview pane; RKWF-007 records the app-owned resolution.

The focused correction gate passes **8/8** and the complete Writer suite passes **233/233**. At the actual 125%-DPI
surface, large labelled View controls, modal entry/close, preview layout/navigation/zoom, distinct disabled navigation
glyphs and the fitted Microsoft Print to PDF setup preview passed. No Save dialog or print job was triggered during
this correction pass; W2-D's retained five-page A4/Letter PDF proof and the exact-paginator integration tests remain
the output evidence. No `src/RibbonKit/**` change was required and W2-F remains dependency-ready but unstarted.
The single full-window STA integration case retains one dispatcher/thread and has a case-specific 20-second ceiling;
its former global 10-second ceiling became flaky only when the 63-image visual project ran beside it. All other STA
tests retain the 10-second default, and RKWF-008 records this bounded harness exception.

### 3.111b Writer startup paper invariant and document-profile plan insertion — 2026-08-24

User review found an intermittent cold-start state in which the paper remained correctly centred and sized but the
first untitled `FlowDocument` retained zero `PagePadding`, placing its caret against the paper's top-left edge. New
appeared to fix the problem because document replacement forced another complete surface application. The initial
document/page model is assigned before the native `RichTextBox` completes its first Loaded/template pass, and the
surface previously had no post-Loaded invariant. `WriterEditorSurface` now reapplies its current Paper/Continuous
layout idempotently on Loaded without replacing the editor or document. A hosted-window regression deliberately
resets all four margins after setup but before first load and requires the Loaded pass to restore them. RKWF-009
records the bounded app-owned timing correction and the remaining cold-launch/DPI acceptance check.

The proposed New chooser is recorded as document-profile work rather than conflated with content templates. W0-E
now owns the non-UI Plain Text/Rich Text/RibbonKit Writer capability and format-transition policy, explicit typed-New
contract, default extensions and loss decisions. W0-F then exclusively owns a pictured, labelled Backstage New page,
Ctrl+N/default-profile behaviour and capability-aware ribbon/Page/View state. True letter, note or report templates
may later sit inside a compatible profile. W2-F now depends on W0-F so ruler, page and paragraph commands cannot ship
with a second ad hoc format gate; W3 packets consume the same W0-E capability contract.

The focused editor-surface suite passes **9/9**. The complete solution builds with zero warnings/errors and passes
Writer **234/234**, RibbonKit **355/355** and the unchanged visual test **1/1** over 63 approved images. No
`src/RibbonKit/**` change was required. W0-E is the next dependency-ready Writer packet; W2-F is intentionally waiting
for W0-F.

### 3.111c Writer cold-start editing state and ribbon-density correction — 2026-08-24

Further cold-start review found that the first Paper surface did not own keyboard focus, so a newly launched Writer
could not accept typing until the user clicked the paper. Initialization intentionally avoided focusing the editor
while the visual tree was incomplete but never completed the handoff. `MainWindow` now performs a one-shot
post-`ContentRendered` focus request at Input priority, establishes both logical and keyboard focus on the long-lived
`RichTextBox`, and refreshes its ribbon projection. It skips the handoff if the window is no longer visible, Backstage
has opened or Print Preview owns the presentation. RKWF-010 records this app-owned startup boundary.

The empty-caret formatting projection also previously suppressed every inline and paragraph value until a real text
character existed. It now reads the native insertion-point font family and size, falling back to the document's
effective defaults, and reads the current empty paragraph alignment. A brand-new document therefore shows its font,
point size and checked Left alignment before the first keystroke without inserting content or dirtying the document;
mixed and unsupported content states remain distinct.

The Page tab's Paper Size and Orientation dropdowns are now large labelled controls alongside the already-large
Margins and Page Color actions. Paragraph Spacing was removed from the trailing end of the compact paragraph row and
is now a separate large labelled dropdown beside the two-row alignment/list/indent cluster. Existing command IDs,
KeyTips, ScreenTips, automation metadata and app-owned vector artwork are retained.

Backstage New, Open, Save and Save As now request the same editor-focus handoff after their asynchronous shell work
finishes; recent-document activation follows it as well. The click closes Backstage and marks a pending return, the
shell's `IsBusy` transition completes it after any unsaved/save/open dialog has closed, and a dispatcher fallback
covers a command that completes before publishing a busy transition. Success, failure and cancellation all return
to the editor, while Print Preview, an open Backstage or a closing/hidden window still prevents an inappropriate
focus steal. The assertions remain inside the existing combined real-window case so WPF's non-freezable
`WindowChrome` never crosses fresh STA threads; a separate first attempt reproduced the RKWF-008 harness failure.

The focused insertion-state and real-window integration cases pass **2/2**. The complete solution builds with zero
warnings/errors and passes Writer **234/234**, RibbonKit **355/355** and the unchanged visual test **1/1** over 63
approved images. No `src/RibbonKit/**` change was required. Actual cold-start typing and the revised Home/Page group
balance remain a live user check; W0-E remains the next dependency-ready packet.

### 3.112 RibbonKit Writer W0-E document profiles and format-transition policy — 2026-08-24

Writer now has three canonical creation profiles over the existing `WriterDocumentFormat` identity: Plain Text,
Rich Text and RibbonKit Writer. One catalog owns their display identity, default `.txt`/`.rtf`/`.rkw` extension,
persistence fidelity and derived content/page-metadata capability sets. Command capabilities distinguish operations
that mutate persisted content from common editing, preview and print actions, so Plain Text disables font/paragraph
formatting without unnecessarily losing preview or print. Profiles remain separate from future letter, note or report
content templates.

`WriterDocumentSession` accepts typed New, Open and Save As requests, keeps Rich Text as the compatible no-argument
default, and validates profile instances at its boundaries. A centralized transition policy compares declared
capability sets rather than enum ordering: same-format and capability-superset saves do not warn, while Rich Text to
Plain Text reports formatting loss, RibbonKit Writer to Rich Text reports page-settings loss, and RibbonKit Writer to
Plain Text reports both. Hyperlink/image/table loss flags are already part of the contract but remain unsupported
until their W3 serializers are accepted. This analysis is deliberately profile-level rather than a document-tree
claim.

Loss confirmation occurs before persistence. Cancellation, a declined warning, a `false` persistence result,
`OperationCanceledException`, or an ordinary persistence exception cannot commit a new path/profile; identity changes
only after a successful save. The existing shell retains its legacy Plain Text-specific prompt only until W0-F, which
must inject the generic transition decider and remove the duplicate warning owner in the same UI-exclusive packet.

The focused W0-E profile/transition gate passes **25/25**. The complete solution builds with zero warnings/errors and
passes Writer **259/259**, RibbonKit **355/355** and the unchanged visual test **1/1** over 63 approved images. W0-E
did not edit `src/RibbonKit/**` or MainWindow/ribbon files. W0-F is the next dependency-ready packet; W2-F remains
intentionally waiting for its capability-aware UI projection.

### 3.113 RibbonKit Writer W0-F New gallery and capability-aware command projection — 2026-08-24

Backstage New is now a page rather than an immediate generic New action. It presents Plain Text, Rich Text and
RibbonKit Writer as three labelled cards with distinct app-owned vector document pictures, descriptions, extensions,
automation names/help text and explicit `1`/`2`/`3` KeyTips. The content uses a constrained Grid plus a horizontal
`WrapPanel` inside a vertical `ScrollViewer`: three compact cards fit at the standard width, while the 800×900 live
surface wraps to a clean single column without sliced cards or a horizontal rail. Content templates remain a separate
future layer.

Each card routes the existing shell New command to the corresponding canonical W0-E profile. Ctrl+N and ordinary
one-click New retain Rich Text as the configured default. Untitled Save now starts with the active profile's
`.txt`/`.rtf`/`.rkw` extension while Save As keeps all three filters. The shell injects one generic W0-E transition
decider; the former Plain Text-only warning owners were removed, so same-format/upgrades do not prompt and every
lower-fidelity Save As uses one centralized warning before the session can commit identity.

Profile capabilities are projected at tab/group boundaries as well as leaf state. Plain Text disables Font,
Paragraph and the complete Page tab while retaining View, preview and print; Rich Text enables the supported
formatting surface but keeps Writer-only Page work unavailable; RibbonKit Writer enables all currently implemented
groups. The projection runs only during initialization, document replacement or a successful format identity change,
not on the caret/selection/typing refresh path that previously exposed performance sensitivity.

Writer now reuses its single `Ribbon.IsBackstageOpen` dependency-property observer for every Backstage focus return.
When the property becomes false it marks one pending return, defers a dispatcher turn so a File command can enter
`IsBusy`, and focuses the long-lived editor only after any dialog/command finishes and no modal preview, hidden or
closing window owns focus. Per-New/Open/Save/Save As/recent focus requests are gone. RibbonKit has no public
close-completed callback: the DP changes at exit-animation start and adorner/Classic-orb teardown completes only in an
internal callback. RKWF-011 therefore proposes a non-cancellable `Ribbon.BackstageClosed` event after teardown while
keeping focus ownership in the host; no `src/RibbonKit/**` change is authorized or present.

The complete solution builds with zero warnings/errors and passes Writer **259/259**, RibbonKit **355/355** and the
unchanged visual test **1/1** over 63 approved images. W0-F assertions are integrated into the existing shell/window
facts, so the inventory remains 614 logic tests plus one visual test. Lead live checks at the available scale passed
the standard and 800×900 New page, all three profile states, Ctrl+N default behavior and post-card editor focus.
External Backstage-content UIA traversal remains RKWF-004 rather than a W0-F app defect. W2-F is the next
UI-exclusive packet and has not started.

### 3.114 RibbonKit Writer W2-F margin guides and interactive horizontal ruler — 2026-08-25

Paper view now owns a non-printing horizontal ruler and an optional content-boundary guide without adding either
surface to the `FlowDocument`, clipboard, native persistence, preview clone, paginator or print output. The ruler
uses the live paper canvas origin plus one document zoom scale, so its ticks, shaded margin zones and first-line,
hanging, left and right paragraph markers follow paper centring and horizontal scrolling. Continuous editing and
modal Print Preview hide both adornments. View exposes large labelled Ruler and Margin Guides toggles with stable
command IDs, KeyTips, ScreenTips and automation metadata; their state remains app view state for W4-A persistence.

Page-margin dragging keeps a visual-only candidate and commits one validated immutable `DocumentPageSettings`
replacement on release. Paragraph-marker dragging is likewise deferred until release and then applies one native
editor undo unit. Escape, capture loss, view/profile/document/page changes, visibility changes, unload and disposal
cancel active work. Cancellation never writes paragraph properties, so inherited/`NaN` values, selection, dirty
state and undo history remain untouched. Mixed paragraph selections suppress the marker projection instead of
presenting the first paragraph as uniform. The guide is non-hit-testable, clipped to the editor row and uses a
slightly translucent neutral secondary-text gray; High Contrast switches ruler and guide drawing to system brushes.

The focused ruler/drag gate passes **22/22** and the complete Writer suite passes **281/281**. The solution builds
with zero warnings/errors and the unchanged RibbonKit/visual suites pass **355/355** and **1/1** respectively. A
clean live launch at the available 125% scale confirmed immediate editor focus, paper/ruler alignment, the lighter
neutral-gray guide, clipping above the status bar, View KeyTip toggles and Paper/Continuous visibility restoration.
External UI Automation still reaches the View tab but not its realized Ruler/Margin Guides leaves; that remains the
bounded RKWF-006 library investigation rather than permission for a `src/RibbonKit/**` change. W2-F is accepted;
W3-A images/hyperlinks and W3-B simple FlowDocument table core are the next dependency-ready packets.

### 3.115 RibbonKit Writer W3-A portable images, hyperlinks and date/time — 2026-08-26

Writer now has app-owned structured-inline services without adding Picture Tools or other ribbon UI. Portable
PNG/JPEG/GIF/BMP insertion decodes from bounded on-load streams, freezes the bitmap, keeps the inline container inert
and rejects invalid signatures, dimensions and pixel counts before WPF performs the full decode. The shared header
validator and native `.rkw` reader enforce the same 16 MiB, 8192-pixel-edge and 32-megapixel limits. Empty documents
gain their first paragraph atomically, and insertion/removal participates in the native editor undo stack.

Hyperlink creation, edit and removal accept only bounded absolute HTTP, HTTPS and conservative `mailto` targets.
Encoded controls, credentials, external file/package targets and other schemes fail without mutation. URI edits
replace the inline inside one native change scope because WPF does not record a direct `NavigateUri` dependency-
property assignment in undo history. Date/time insertion requires an explicit value/format and supports deterministic
culture input while retaining a current-culture default for application use.

Native persistence remains data-only. The allowlist reconstructs only the approved `Hyperlink` and inert image shape,
requires internal image relationships, exact image content-type declarations and safe URI values, and rejects missing,
extra, case-colliding, external, corrupt, oversized or decoder-invalid parts before object use. Save preflights the
same 61 MiB expanded-package ceiling enforced by Load, so a successful native Save is guaranteed to reopen under the
same limits. The native capability catalog now advertises portable image/hyperlink fidelity and downgrade warnings
include both losses.

The combined W3-A/native-persistence gate passes **47/47**. Independent security review closed hyperlink undo,
encoded-URI, corrupt-image, predecode-dimension, content-type and save/load-size findings. W3-A is accepted; its
dialog/ribbon presentation remains a later UI integration seam rather than part of this non-UI packet.

### 3.116 RibbonKit Writer W3-B simple FlowDocument table core — 2026-08-26

Writer now has an app-owned `WriterTableService` for 1×1 through 8×8 insertion, caret/cell/range discovery, row and
column insertion/deletion, rectangular merge, spanned-cell split, final-cell Tab row creation, alignment, padding,
border/background, bounded sizing and row/column distribution. Span-based occupancy validation rejects gaps,
overlaps, empty row groups, foreign references, excessive grids and partial ranges before mutation. Failed and no-op
operations leave the tree, selection, `TextChanged` count and undo history unchanged; semantic empty-cell placeholders
normalize to one valid paragraph while meaningfully formatted empty blocks remain intact.

Realized `RichTextBox` tests established the undo boundary. Row/cell structural and formatting mutations use one
native `BeginChange` unit. WPF does not record isolated `Table.Columns` collection/property edits, so column metadata
operations deep-clone and replace the containing table inside the same native unit, then resolve fresh references and
restore a logical caret. The clone contract is intentionally bounded to package-serializable FlowDocument content;
unsupported custom/external objects fail before replacement, and structural commands intentionally collapse a
non-empty selection to the resulting caret. Row height remains a documented symmetric-padding approximation because
WPF exposes no native fixed `TableRow.Height`.

The focused W3-B gate passes **24/24**, including native undo/redo, unequal row groups, spanned final Tab, empty and
formatted-empty merge/split, exact-range no-ops, rollback, clone fidelity and grid limits. W3-B is accepted. Table
picker/keyboard routing/contextual Table Tools belong to W3-C, while table `.rkw`/RTF fidelity remains W3-D; the native
capability catalog therefore still reports `PreservesTables=false`. No `src/RibbonKit/**` change was required.

### 3.117 RibbonKit Writer W3-C Insert, table interaction and contextual Table Tools — 2026-08-26

Writer now projects the accepted W3-A/W3-B services onto a complete Insert surface. Large labelled Picture,
Hyperlink and Date and Time commands open accessible app-owned dialogs with validation, default actions, RTL owner
direction and editor-focus recovery. The Table group owns a compact 1×1 through 3×8 quick gallery plus a separated
Custom Table button for validated 1×1 through 8×8 manual entry, with stable command IDs, rich ScreenTips,
per-choice automation names and standard WPF Invoke providers. Mouse, two-dimensional keyboard and UIA commit paths
converge on one insertion boundary so the gallery finishes its input route before its shared strip/popup presenter is
re-homed.

Table-cell PreviewKeyDown routing now precedes the later W1-E paragraph Tab path. Tab and Shift+Tab navigate a
deterministic span-aware order, the visual bottom-right occupant remains the final-cell row-creation target, and
Ctrl+Tab plus the Table Tools Literal Tab command insert an actual tab inside one cell. The live profile/editor gate
leaves routing unhandled when table editing is unavailable. Deferred commands also re-check the current document and
`Shell.IsBusy`, preventing a queued mutation from landing during Save or after document replacement.

The contextual Table Tools tab appears only while the caret/selection is in a table and exposes the supported
row/column, merge/split, sizing, distribution, alignment, border and background operations. Enablement mirrors W3-B
failure rules, including final-row/final-column and unequal-row-group constraints; commands restore a valid caret and
editor focus after success or rejection. Large dropdowns participate in adaptive sizing, and standard, narrow,
minimized and RTL ribbon states remain usable. RKW alone advertises the new `TableEditing` command capability, while
all profiles continue to report `PreservesTables=false`; the UI explicitly identifies W3-D as the persistence owner.

Live inspection found that `InRibbonGallery`'s popup-host dynamic background remained unresolved in its separate HWND,
allowing ribbon content to show through. Writer applies the current ribbon-content brush (or the system Window brush
in High Contrast) directly to that app-owned popup instance and records the reusable control gap as RKWF-013. The
gallery itself stays locally LTR to avoid the generic template's empty RTL strip, while physical arrow semantics still
follow the Writer window. No `src/RibbonKit/**` file changed.

The focused W3-C gate passes **19/19** and the complete Writer suite passes **341/341**. RibbonKit remains **355/355**,
the visual suite remains **1/1** over 63 approved images, and the solution builds with zero warnings/errors. A real
125%-scale Writer window passed Picture mouse launch, Hyperlink/Date-Time keyboard launch, insertion and focus recovery;
mouse and keyboard table-grid commits; Tab/Shift+Tab/Ctrl+Tab/final-row behavior; contextual tools; 620-DIP narrow,
minimized and RTL layouts; and a three-page table preview. Mixed-monitor movement remains W4-C hardware work, external
leaf traversal remains RKWF-006, and a live Windows High Contrast switch was unavailable for this pass. W3-C is
accepted; W1-E and W3-D have not started.

A user visual follow-up on 2026-08-26 found four app-owned presentation defects in that accepted surface: star-sized
dialog rows stretched the Date/Time format and Hyperlink display-text controls, Writer dialogs retained normal-window
maximize/resize chrome, the ruler-hidden editor lacked a ribbon/content separator, and table insertion plus All
Borders styled only the outer table with a nearly white separator brush. The corrective pass replaces those dialog
rows with compact shared grids and fixed dialog chrome, adds a one-DIP editor separator only while the ruler is
collapsed, limits the quick gallery to three rows with a separated Custom Table dialog, and applies a theme-aware
secondary-text brush to both the outer frame and every cell border. The Writer test project builds with zero
warnings/errors and seven directly affected tests pass. The postponed complete gate was run on 2026-08-27 after a
live follow-up also centered the Custom Table action buttons: Writer passes **343/343**, RibbonKit remains
**355/355**, the visual suite passes **1/1** over 63 approved images, and the solution builds with zero
warnings/errors. The real available 125%-scale Writer window confirms compact fixed Picture, Hyperlink, Date/Time
and Custom Table dialogs; the ruler-hidden separator; exactly three quick gallery rows plus the separated custom
action; visible default inner borders; and the No Borders to All Borders transition. The corrective pass is accepted.

### 3.118 RibbonKit Writer structured-object interaction planning correction — 2026-08-27

A later user review identified three connected usability gaps that were not explicit in the original Writer packet
split. The native `RichTextBox` still supplies its dated default context menu; a Table Tools command can appear to
flash back to Home while table structure is being replaced; and inserted pictures/tables have ribbon sizing commands
but no direct selection or resize handles. This section records packet ownership only; no implementation or acceptance
claim changes in this planning correction.

W1-E now owns a modern Writer editor context-menu base. It captures the invocation target before popup focus moves,
keeps native spelling suggestions/actions, projects the supported text/clipboard/Font/Paragraph commands and exposes
one bounded extension seam. W3-E adds context-aware rows: a table target receives only valid table operations, a
picture receives supported picture operations, and a hyperlink may receive its safe edit/open/remove operations.
Ordinary text must not inherit object-only commands, and structured objects must retain the shared text operations
that remain semantically valid.

The Table Tools flash is currently classified as an app-owned state-publication problem, not a confirmed RibbonKit
runtime defect. `WriterTableInteractionController` publishes every transient native selection change, while
`RefreshStructuredContentState` immediately collapses `TableToolsTab` whenever the intermediate selection cannot be
resolved to a table. A structural replacement can therefore remove the old table, publish an outside-table state,
restore the caret into the replacement and publish the table state again; RibbonKit is expected to choose a normal
tab when its selected contextual tab is actually collapsed. W3-E must hold a stable object/context snapshot while
focus is in contextual ribbon or menu chrome, suppress transient projection during an app-owned mutation and publish
one final state. It keeps Table Tools selected when the committed result remains in the table, but collapses it after
real deletion, document replacement, undo or a committed selection outside the object. RKWF-014 records the evidence
boundary; a RibbonKit change remains prohibited unless a minimal consumer reproduces a fallback while the contextual
tab never becomes hidden.

W3-E also owns direct structured-object manipulation after W1-E and W3-D. Non-printing adorners provide picture
edge/corner handles and a table selection grip, row/column boundary grips and an overall size grip. Picture corners
preserve aspect ratio while edges change one axis; table grips reuse W3-B's bounded column-width and documented
row-height approximation rather than pretending WPF exposes a fixed `TableRow.Height`. They track zoom, scrolling,
view mode, RTL and per-monitor DPI, preview locally during a drag, commit one native undo unit on release and roll back
on Escape or capture loss. Picture Tools becomes real only for supported size/replace/remove behavior. The overlays
never enter the FlowDocument package, preview, print or ordinary automation content, and keyboard/ribbon alternatives
remain available. Crop, correction, rotation, wrapping and other Word-only affordances remain absent until
corresponding document and persistence contracts exist.

### 3.119 RibbonKit Writer W1-E Home formatting completion — 2026-08-27

W1-E completes the supported Home editing workflow without adding Word-only formatting promises. The Font family
source now caches installed families with deterministic fallback and recent choices, keeps editable entry, virtualizes
its popup and previews each family in its own face. Point sizes use the conventional list plus validated finite custom
values, and deterministic Grow Font/Shrink Font commands step through the same policy. Paste is a real split action
with Keep Source Formatting and Keep Text Only, while Clear Formatting removes direct character formatting without
rewriting paragraph structure.

Text colour and highlight are last-used split actions backed by named theme/standard/recent swatches, Automatic or No
Color and an app-owned exact-value Color dialog. The Font launcher uses an app-owned Font dialog for the supported
family, style, size, underline and colour values, including non-closing Apply. The host suppresses a redundant final
OK application when it matches the last Apply so one user choice does not create a second undo action. User inspection
showed that the temporary Windows common dialogs could not match RibbonKit input/button shape or theme colouring, so
the final dialogs are WPF surfaces composed from `RibbonComboBox`, `RibbonTextBox`, `RibbonCheckBox` and the shared
Options-dialog action styles. Their cards, previews, borders and text use dynamic RibbonKit resources, with no
WinForms/Drawing project dependency.

The Paragraph launcher is likewise a fixed-width, content-height app-owned dialog. A corrective 2026-08-28 pass
replaced the data-form presentation with grouped General, Indentation, Spacing and live Preview cards; editable
RibbonKit ComboBoxes retain custom values and visible point units. An empty document now opens with normal zero/None
values rather than mixed blanks, while genuinely mixed formatting may remain unset. Content-height sizing removes the
earlier 125%-DPI clipping risk. Font-family ribbon synchronization also now preserves an active editable/dropdown
session, so typing a search prefix is not overwritten by the editor's current value.

A second 2026-08-28 live follow-up simplifies each quick colour popup to Automatic/No Color plus ten base standard
colours; accent/background entries and light/dark variants are removed. More Colors retains exact Hex/RGB entry and
adds a compact keyboard- and pointer-operable saturation/brightness field with a hue strip. The Font dialog's family
field now uses a bounded grid column with additional trailing space at the observed DPI. Its Effects card adds
underline, mutually exclusive single/double strikethrough and mutually exclusive superscript/subscript with a live
preview. These are real selection properties rather than decorative controls: they publish selection state, commit in
one native undo change and clear/undo correctly. Double strikethrough uses a deterministic two-line composition of
WPF's predefined Strikethrough and Baseline decorations. The strict `.rkw` reader now accepts only the exact bounded
`TextDecorationCollection` property graph emitted by XamlPackage, rejects custom pens/locations/object children and
round-trips those effects with `BaselineAlignment`; no arbitrary XAML loading or RibbonKit runtime change is involved.

Editor Tab handling is capability-aware and yields first to W3-C table-cell routing. At paragraph boundaries Tab and
Shift+Tab indent or outdent, Ctrl+Tab inserts a literal tab, plain mid-paragraph Tab remains literal, Shift+Tab at a
mid-paragraph caret is left unhandled, and F6/Shift+F6 use the explicit focus-exit seam. The Writer-owned modern context
menu captures and restores its target before popup focus moves, preserves spelling suggestions and supported actions,
projects shared text/clipboard/Font/Paragraph commands and exposes one bounded W3-E extension seam. It intentionally
does not guess table, picture or hyperlink rows. The Styles audit found no complete named-style and persistence
contract, so no decorative or non-persistent Styles gallery was added.

The implementation stays under `samples/RibbonKit.Writer/**` and its tests; no `src/RibbonKit/**` file changes.
Independent Luna review of the original W1-E slice found no confirmed product-code defect in Apply/OK suppression, Color cancel/black
handling, native ownership, context-target restoration or table-Tab bypass. Its two test-hygiene findings were fixed:
the new clipboard test restores prior clipboard data and all shown context-menu test windows close deterministically.
The refreshed complete Debug gate passes Writer **388/388**, RibbonKit **355/355** and the visual suite **1/1** over 63
approved images after a zero-warning/error solution build. Live visual reacceptance of the themed Font/Color dialogs, corrected
Paragraph dialog and representative ribbon/context-menu states remains pending, so W1-E is not yet marked visually
accepted.

### 3.120 RibbonKit Writer final formatting/preview polish and W3-D structured-content round-trip — 2026-08-28

A small live-review addendum replaces the hue indicator's fractional-DPI-sensitive one-DIP canvas offset with
layout-driven vertical centring. Print Preview now has the same one-DIP dynamic ribbon-border handoff used when the
paper editor's ruler is hidden. Realized WPF tests cover hue-centre geometry and the preview separator's collapsed,
visible and modal-exit lifecycle; the separator itself was committed separately at `46f9ede` before this packet.
The Writer-owned print setup also now follows the app's themed-dialog contract: dynamic surface/text tokens, the
shared action-button styles and `RibbonComboBox` printer selector replace its remaining stock WPF chrome. Its exact
fixed-paginator preview and native queue/ticket/driver submission path are unchanged, and the existing realized test
now covers those theme resources without increasing the test inventory. The printer input takes its width from its
realized host instead of assuming the rail's nominal width, preserving the divider and avoiding right-edge clipping
under fractional DPI rounding.

W3-D closes the native table-persistence gap without passing untrusted XAML to `XamlReader` or `TextRange.Load`.
The data-only reader now admits only bounded `Table`, column, row-group, row and cell shapes; reconstructs supported
text/block, width, spacing, span, padding, background and border properties; recursively reads the already-allowed
paragraph/list/image/hyperlink content inside cells; and validates each realized grid with W3-B's occupancy rules.
Tables, rows, columns and spans are capped at the same 1024 discovered-dimension ceiling, while foreign controls,
invalid spans, overlapping grids, unsafe attributes and oversized shapes fail before the current document can change.

New saves retain outer-manifest schema v1 and settings schema v1 but declare reader/content schema v2. The reader still
loads representative v1 content, rejects inconsistent version combinations and rejects table markup that falsely claims
content schema v1. A combined fixture survives save-close-reopen and a second save/reopen with formatted text, a safe
hyperlink, a packaged image, custom page settings and a styled two-column table with a merged heading. The native
capability catalog now truthfully reports `PreservesTables=true`; format-transition warnings consequently include tables
when saving native content as RTF or TXT. Compatibility fixtures show the bounded loss: TXT flattens table content to
characters, while WPF RTF retains representative table text but loses the tested merge geometry and exact outer frame.

The focused W3-D/profile/transition gate passes **63/63** and the complete Debug suites pass Writer **396/396**,
RibbonKit **355/355** and visual **1/1** over 63 approved images. The solution build succeeds with zero errors; its only
12 warnings are retry/copy warnings for `RibbonKit.DesignTools.dll` held open by Visual Studio process 33288. The
Writer project build itself is zero-warning. No `src/RibbonKit/**` file changed. W3-E remains the next structured-object
packet after the pending W1-E live visual reacceptance.

### 3.121 RibbonKit Writer W3-E1 structured-context and contextual-state foundation — 2026-08-28

W3-E has begun with one bounded foundation; this is not completion of the full packet in §3.118. The W1-E context-menu
extension now classifies a document-bound target as ordinary text, table, picture or hyperlink without replacing the
shared text/spelling rows. Table targets add capability-checked insert/delete, merge/split, cell-size,
borders/background and true table-deletion actions; pictures expose only the implemented remove action; hyperlinks
expose edit/remove. Every callback revalidates the captured document and exact live object immediately before restoring
the native selection and executing. Replaced documents and removed/replaced objects therefore reject stale popup work.

`WriterTableInteractionController` now defers state publication through an app-owned structural mutation and emits one
final projection after caret recovery. Realized Writer-window coverage keeps the selected Table Tools tab through row
insertion, but collapses it and selects a normal fallback after true table deletion. Table deletion leaves a valid caret
paragraph and is one native undo/redo unit. Picture removal is likewise covered through the realized context-menu and
editing-controller Undo/Redo route, addressing the live report that removal appeared irreversible. A follow-up using
the first Backstage Recent document exposed WPF's loaded-content variant: native Undo recreated the removed
`InlineUIContainer` with an empty `Grid` instead of its `Image`, then discarded native Redo when the child was replaced.
Writer now retains bounded app-owned removal metadata, repairs that empty placeholder without adding a second native
undo unit, and supplies the paired redo only after WPF's native redo chain is exhausted. Older text undo/redo units keep
their original order. Unmodified Delete and Backspace are intercepted only when the selection is exactly the picture or
the caret is on the matching forward/backward picture boundary; ordinary text and opposite-direction deletion remain
native. Save and preview use an isolated normalized clone so this live undo bridge never leaks into `.rkw`
content or fixed pagination; the repaired picture is covered through preview plus save/reopen. Nested table, section/list
and figure/floater traversal keeps structured targets and deletion ownership within the live document.

The original focused controller/resolver/table gate passed **17/17** and the complete Debug suites passed Writer
**401/401**, RibbonKit **355/355** and visual **1/1** over 63 approved images. After the loaded-picture correction, the
exact realized Recent/menu/keyboard/history/preview/save-reopen path passes, as do the direct image undo, directional
Delete/Backspace and preview regressions.
The proportional rerun passed Writer **400/400** in the combined non-focus run plus the one process-focus test **1/1**
in isolation; that split avoids its known process-global keyboard-focus interference. RibbonKit remains **355/355**,
visual remains **1/1**, and the solution build has zero warnings/errors. No
`src/RibbonKit/**` file changed, so RKWF-014 remains an app-owned correction rather than a RibbonKit runtime defect.
The loaded-picture undo exception is recorded separately as RKWF-015 and likewise requires no runtime change.
W3-E2 still owns explicit picture selection, a real Picture Tools tab, non-printing picture/table adorners and bounded
direct resizing across zoom/scroll/view/RTL/DPI. Those features and the complete W3-E live acceptance matrix remain
pending before W4-A may begin.

### 3.122 RibbonKit Writer W3-E2a explicit picture selection, Picture Tools and resizing — 2026-08-29

The first bounded W3-E2 slice is complete without beginning table adorners. `WriterPictureInteractionController` owns
an exact `FlowDocument`/`InlineUIContainer` picture target, selects it from a click hit or exact native picture range and
keeps it stable while focus moves into ribbon controls. A final editor selection outside the picture, true deletion,
document replacement or target invalidation clears the target. The real contextual Picture Tools tab exposes only
implemented width, height, Apply Size and Remove Picture operations; its keyboard/KeyTip/UIA path shares the same
transaction as pointer sizing. Context-menu Original Size and Fit to Page Width actions use that path as well. A valid
picture keeps Picture Tools selected through the resize replacement; true removal collapses it to deterministic Home.

`WriterPictureResizeAdorner` is a visual-layer-only, automation-peer-free `Adorner` with four corner and four edge
handles. Corner geometry preserves aspect ratio, edge geometry changes one axis, all paths enforce a 12-DIP minimum and
the current page/editor content bounds, and handle rectangles align at 100/125/150/175/200% DPI. The adorner remains
outside the `FlowDocument`, so preview/print clones, `.rkw` content and UI Automation contain no interaction chrome.
Because it adorns the realized `Image`, the frame follows Paper/Continuous layout, zoom transforms, scrolling and RTL
without a second coordinate model. Pointer moves preview only the live image properties; they do not create text-change,
dirty or Undo units. Escape, capture loss and view change restore the opening dependency-property geometry.

The 2026-08-29 live follow-up maps every handle to its physical resize cursor (`NWSE`, `NESW`, `WE` or `NS`) and places
the Picture Tools Width/Height inputs in an aligned two-column, two-row grid beside Apply Size. A second follow-up keeps each visible handle at 8
DIP while expanding its invisible mouse target to 16 DIP; nearest-handle resolution keeps overlapping targets
deterministic on very small pictures. Refreshed visual confirmation of the polish changes remains pending.

Mouse release and ribbon sizing commit one native text-container replacement unit. `WriterImageService` records inert
opening/committed snapshots around that replacement and extends the existing RKWF-015 empty-placeholder repair to both
Undo and Redo, retaining older history and exact dimensions after save-close-reopen. Existing context-menu and
Delete/Backspace picture-removal Undo/Redo coverage remains green. No `src/RibbonKit/**` file changed and no runtime gap
was found.

The focused W3-E2a gate passes **34/34**. The complete Writer inventory passes **434/434** in one run (including the
historically process-global focus case without needing a split), RibbonKit passes **355/355**, visual passes **1/1** over
63 approved images, and the solution build has zero warnings/errors. Live pointer/ribbon acceptance in the actual Writer
window remains pending. The rest of W3-E2 still owns table selection, its selection/row/column/overall resize grips,
bounded table resize transactions and the complete W3-E live matrix; W3-E is not complete.

### 3.123 RibbonKit Writer W3-E2b table selection and direct resizing — 2026-08-29

The bounded table half of W3-E2 is now implemented as app-owned Writer interaction chrome. A
`WriterTableResizeController` follows the exact live table resolved by `WriterTableInteractionController` and attaches
one automation-peer-free adorner to the editor layer. Its top-left selection grip selects the table range, top boundary
grips resize logical columns, left boundary grips resize logical rows and the bottom-right grip scales the overall
table. The frame and handles stay outside the `FlowDocument`, `.rkw`, preview, print and UI Automation trees.

`WriterTableLayoutResolver` derives realized table, row and column boundaries from live cell text geometry rather than
inventing a second document layout. Explicit pixel `TableColumn.Width` values remain authoritative; Auto columns use
the realized boundaries. The adorner is invalidated from table-state, editor-size and scroll changes, follows
Paper/Continuous presentation and uses DPI-aligned 8-DIP handles with 18-DIP invisible hit targets. Column cursors are
horizontal, row cursors vertical and the overall grip uses the physical diagonal cursor.

Column preview changes only native column metadata and is restored before commit. WPF records direct
`TableCell.Padding` changes in native Undo even outside an explicit change scope, so row preview deliberately moves only
the adorner geometry; release computes the final symmetric-padding approximation without touching the document during
pointer movement. Escape, capture loss, view change and document replacement restore the exact opening column/padding
state. Release restores that opening state and `WriterTableService.ApplyResize` performs one cloned-table replacement,
preserving the caret and producing one native Undo unit for column, row or combined overall sizing. Columns enforce a
24-DIP content minimum, rows a 12-DIP realized minimum and overall width the current page/editor content bound.

The first live 125%-scale review showed the frame ending early, and an initial DPI projection overcorrected it past the
table edge. The actual defect was boundary inference: an internal boundary averaged the previous empty cell's content
end with the next cell's content start, placing grips near cell midpoints. Boundaries now prefer the next logical cell's
realized start, explicit pixel `TableColumn.Width` remains authoritative without a DPI multiplier, and only the
editor's deliberate zoom `LayoutTransform` participates in projection. Auto final columns reuse the median realized
preceding width when their empty content end cannot describe the cell edge. Pointer deltas are transformed back through
zoom before changing native widths or padding.

A second live review exposed the post-commit form of the same WPF timing gap: immediately after cloned-table replacement,
an empty trailing cell can temporarily return no usable character rectangle. Deriving logical column count from only
realized cells therefore omitted the valid last column and ended the frame one cell early after resize. Logical row and
column counts now come from the native table structure; realized character rectangles supply coordinates only, with
explicit column metadata/fallback interpolation filling a temporarily unrealized edge.

The next live screenshot showed a smaller but cumulative post-resize drift: every explicit-width handle was roughly one
native `Table.CellSpacing` unit left of its rendered boundary. `TableColumn.Width` does not include that spacing even
though WPF contributes it once per laid-out column. Explicit boundary projection now adds the table's finite,
non-negative cell spacing for every column; resize math and persisted width values remain spacing-free native column
widths.

The overall bottom-right grip initially mixed two preview mechanisms: height was projected immediately from pointer
delta while width waited for native `TableColumn` re-layout. WPF can defer that layout while the adorner owns mouse
capture, making the frame appear to preview only one direction. Overall preview now scales both boundary arrays from
the immutable opening snapshot on every mouse move, so its bottom-right corner follows the bounded pointer X/Y
directly; native column metadata remains the release-time commit source. Maximum content width also excludes the
rendered per-column cell-spacing contribution.

The first two-axis correction still performed a live-layout availability check before entering that opening-snapshot
branch. Horizontal preview remained visible through the native column-width mutation, but when reflow temporarily
returned no realized table layout the synthetic frame—including its vertical movement—was skipped. The overall branch
now runs before any live-layout query and remains fully opening-snapshot-driven until release. A focused regression
forces the live resolver to return null mid-drag and pins simultaneous X/Y movement.

Table placement is intentionally separate from cell-text alignment. Table Tools now exposes Left, Center and Right
table alignment, implemented as Writer-owned horizontal `Table.Margin` placement against the current document content
width. It preserves the table/cell `TextAlignment`, vertical margins, native undo and `.rkw` persistence. The existing
Cell Alignment menu continues to affect text in the current cell only.

At the user-requested minimal verification level, the focused table-resize geometry/adorner/Undo plus table-placement
gate passes **5/5**,
the existing real-tree MainWindow integration case passes **1/1**, the Writer project builds with zero warnings/errors
and `git diff --check` is clean. The full Writer/RibbonKit/visual/solution gates were intentionally not rerun. No
`src/RibbonKit/**` file changed. Live table-grip acceptance and the remaining W3-E mouse/keyboard/UIA, zoom/DPI/RTL,
span/multi-row-group, save/reopen and preview/print matrix remain pending; W3-E is not yet accepted.

### 3.124 Writer-promoted RibbonKit friction corrections — 2026-08-29

Four Writer observations now have focused runtime reproductions and the smallest shared-control corrections.
`RibbonGroupSeparator` is the first theme-owned visual partition that can sit inside a horizontal group layout.
It is lookless and non-interactive, participates in `IRibbonSizeAware` measurement at 9/7/5 DIPs for
large/medium/small, and maps a collapsed group back to large because the full content is re-homed into the flyout.
Symmetric chrome follows RTL without bespoke placement logic. KeyTips, QAT projection, customization command
discovery and UI Automation all continue to treat it as decoration. Writer uses the control between its Paragraph
command clusters, and the isolated net472 Ribbon Editor now creates it by string name instead of inserting a stock
WPF `Separator`. The Showcase adds three complementary examples: a compact height-constrained separator before
Superscript in Home/Font, a full-height direct-child separator before Screenshot in Insert/Illustrations, and a
full-height separator between View/Zoom's large command and its stacked 100% / Page Width cluster. A focused static
contract pins those examples and the new vector Superscript icon without coupling visual snapshots to the executable
Showcase XAML.

`InRibbonGallery` resolves its popup background from the connected gallery when the popup HWND opens rather than
trusting resource lookup inside the detached popup branch. It falls back to `SystemColors.WindowBrush`, uses that
brush explicitly in High Contrast, and re-resolves an open surface after RibbonKit theme or system High Contrast
notifications. Writer no longer applies its template, finds `PART_PopupHost` or assigns that private part directly.

`RibbonTabControl` now observes its `RibbonTab` collection and each tab's effective visibility. An add, remove, move,
reset or visibility transition coalesces one Loaded-priority refresh after layout, keeping both the Office 2024
sliding marker and the Office 2010/2013 connected-tab notch under the selected header. Writer therefore retains the
authored Home, Insert, contextual Table/Picture Tools, Page, View and Print Preview order; its startup relocation of
all contextual tabs to a trailing segment is removed.

`Ribbon.BackstageClosed` is a new non-cancellable completion event. It is raised only after a real Backstage's close
animation, adorner/proxy teardown, placement reset and motion cleanup. A close generation prevents a stale animation
callback from winning after close/reopen/reclose; `RibbonApplicationMenu` dismissal never raises the event. Writer
still owns preview demand, command busy state and intended focus, but begins its guarded editor-focus return from the
exact completion boundary instead of the early `IsBackstageOpen=false` transition. The Showcase reports the event in
its status bar, and the public API carries XML documentation.

The focused friction/example gate passes **9/9** across the adaptive/decorative separator contract, its three
Showcase arrangements, opaque popup, contextual marker, Modern/Classic2010/Classic2007 Backstage closure, reopen
cancellation and application-menu
exclusion. The Writer real-tree integration case passes **1/1** with both former app workarounds removed and the
Paragraph prototype present. Live theme/DPI/RTL separator, popup and marker coverage plus File-toggle,
Back/Escape and KeyTip closure remain the proportional acceptance work; automated success is not live visual
acceptance. The complete Debug gate passes RibbonKit **364/364**, Writer **439/439** and visual **1/1** over 63
approved images after a zero-warning/error solution build; `git diff --check` is clean apart from line-ending
normalization notices.

High Contrast was later removed from the bounded separator acceptance matrix: RibbonKit does not currently claim a
whole-ribbon Windows contrast-theme mode. The system-color fallbacks introduced by the RKWF-013 gallery correction
and RKWF-019 scrollbar remain targeted behavior rather than evidence of full-surface support.

### 3.125 Showcase Ribbon Lab split and Office 2010 inactive frame continuity — 2026-08-29

The Showcase's former View tab mixed the single document-view group with seven configuration and diagnostic
groups. View now contains only Zoom. Theme, Aero Frame, Accent, Motion, Inputs, Backstage and Application move
together to a new Ribbon Lab tab. Existing group command IDs remain unchanged so the layout split does not
change command identity or QAT references; a previously saved per-tab group arrangement naturally cannot transfer
those groups from their former parent tab. A static integration contract pins the two tab inventories.

A 2026-08-30 Showcase follow-up refines that split around user intent rather than treating every configuration
surface as a lab feature. View now owns Zoom plus the Theme, Accent and Backstage presentation groups. Ribbon Lab
retains Aero Frame, Motion, Inputs, Scrolling and Application as its extended-control and diagnostic surfaces. The
groups moved intact, including their command IDs and event handlers, and the static inventory contract reflects the
new ownership.

The Office 2010 Aero title already changed to the inactive fallback and attenuated only its live tint when its
`RibbonWindow` lost activation, but the tab-header row retained the active fallback/tint composition. The tab-row
template now uses the same inactive fallback and wraps its bound tint in a visual whose opacity can be attenuated by
the existing inactive-overlay metric. This preserves Acrylic's independent material opacity and changes no layout or
input surface. The focused template contract pins the inactive fallback and tint handoff. The complete Debug gate
passes RibbonKit **365/365**, Writer **439/439** and visual **1/1** over 63 approved images after a zero-warning/error
solution build. Live active-to-inactive color continuity remains a separate visual acceptance check.

### 3.126 Localization/RTL separator acceptance surface — 2026-08-30

The user accepted `RibbonGroupSeparator` through live theme switching and an actual collapsed-group flyout; RTL,
DPI and High Contrast remain. The Localization/RTL lab now places a named direct-child separator between Paste and
Select in Popup Checks. Its existing local flow-direction toggle makes the physical reorder visible without changing
the main Showcase window, and the checklist calls out that the separator must remain between the two commands. The
existing localization structural test now pins that authored neighborhood. The focused test passes **1/1** and the
Showcase Debug project builds with zero warnings/errors. The complete solution gate was not repeated because runtime
code and the test inventory are unchanged.

### 3.127 Live-DPI InRibbonGallery viewport and side-button correction — 2026-08-30

After the user accepted RKWF-005, RKWF-013 and RKWF-016 through all themes/light-dark variants and the complete
100-200% DPI matrix (plus collapsed-flyout and RTL coverage for the separator), a different live-transition defect
appeared in the Showcase Styles gallery. The popup was closed while the per-monitor-v2 window changed DPI. On the
first subsequent interaction, the lower expand glyph could behave like scroll-down and the strip could show an empty
page until opening Snipping Tool induced a later activation/layout/render pass.

The gallery already repaired its shared strip/popup `ScrollViewer` once on popup open, but it did not observe its
owner Window's DPI transition while closed. It now retains a lifecycle-safe owner subscription, stops an old scroll
animation, resets to a safe offset synchronously, and performs generation-guarded Loaded and Render remeasure passes.
After closed-strip layout settles it restores the selected row without animation; a later popup open still starts at
offset zero. A synchronous metric refresh at up/down invocation prevents an old viewport from supplying the page
distance. Transient collapsed-group re-homing retains the known owner through its unload/load pair, while a gallery
that remains removed detaches at ApplicationIdle.

The fixed 17-DIP vertical `StackPanel` is replaced by three equal, layout-rounded Grid rows. Each button template now
has a full transparent root hit surface, leaving its inset border as visual chrome only. That first correction passed
its two focused regressions but failed its live recheck: recordings at 200% and 125% showed the popup's transparent
window/shadow rectangle still covering part of the main-window button column, and a 150%-to-125% transition could
still leave the returned strip blank. The missing boundary was between HWNDs, not within the button template;
transparent Popup pixels still intercept input before WPF can hit-test the underlying button.

The first HWND-boundary correction width-bound the Popup child to `PART_ContentHost`. Its next live check revealed why
that was also the wrong constraint: after a DPI transition it clipped deterministically, and the narrower card wrapped
a gallery authored for three columns after only two tiles. The Popup remains explicitly anchored to the content host,
but now keeps its natural measured width. A custom placement aligns the whole popup-window edge with the content/button
boundary and lets the extra width expand leftward in LTR or rightward in RTL. Shadow accommodation therefore stays
away from all three buttons without squeezing the gallery card. The close path still stops any animation, requests
offset zero before re-homing, and synchronously commits a freshly measured strip viewport at zero after re-homing; a
later selected-item reveal remains deferred until valid strip geometry exists.

Five realized cases now cover closed downward-DPI open, natural three-column popup layout plus HWND/button separation
in LTR and RTL, immediate popup-page clearing on close, and left/center/right hit ownership for all button rows. The
complete Debug gate passes RibbonKit **370/370**, Writer **439/439** and visual **1/1** over 63 approved images after a
zero-warning/error solution build. This Packet 1 correction still requires live mixed-DPI acceptance. The themed
scrollbar proposal remains deferred until then.

The next live check accepted the natural three-column geometry, popup/button separation and input behavior; one blank
closed-strip frame still waited for an unrelated redraw after DPI. A generation-guarded ContextIdle pass was added to
commit layout once more and explicitly invalidate the presenter, scroller, active host, gallery and every realized
tile. A focused item proved that extra render occurs, but the next live check still produced the clipped strip. The
attempt therefore demonstrates only repaint scheduling, not a fix, and repeated invalidation is exhausted.

WPF's Popup lifecycle supports the narrower diagnosis: closing hides its separate window synchronously, queues repaint
for the underlying window and schedules destruction; a quick reopen can cancel pending destruction. Some popup-window
lifetime is therefore transiently retained, but the captured tiny selected-border corner means the gallery visual is
back in the main tree rather than merely hidden or bitmap-cached. The stronger suspect was the one `ScrollViewer`
whose viewport/clip state crossed the main and popup HWND/DPI contexts.

The default template now implements the host-specific boundary: `PART_ScrollViewer` stays permanently under the
main-window `PART_ContentHost`, `PART_PopupScrollViewer` stays permanently under the Popup's `PART_PopupHost`, and only
`PART_ItemsPresenter` moves between their `Content` slots. The failed final Render/ContextIdle invalidation workaround
was removed because it repainted the same stale clip rather than correcting ownership. A compatibility fallback keeps
the original whole-content re-home working for custom templates without the two new optional parts. The accepted
custom placement, natural three-column width, reduced horizontal gap, mirrored RTL direction and `VerticalOffset=-8`
are unchanged. Eight focused gallery cases pin the two-scroller ownership, post-DPI first open, paging isolation,
three-column geometry and input behavior. The zero-warning/error solution build plus RibbonKit **371/371**, visual
**1/1**, and Writer **439/439** pass. The user subsequently accepted the formerly failing first-open/close redraw in
the live 150%-to-125% and 200% mixed-DPI paths, closing RKWF-018 and Packet 1.

### 3.128 Theme-aware scrollbar control and gallery overflow — 2026-08-30

Packet 2 replaces the gallery popup's OS-native overflow chrome without replacing WPF scrolling behavior. The public
lookless `RibbonScrollBar : ScrollBar` inherits native range commands, keyboard/mouse handling, horizontal/vertical
orientation, RTL behavior and the `ScrollBarAutomationPeer` RangeValue contract. Its shared vertical and horizontal
templates use vector arrows, native `Track`/`Thumb` parts, explicit hover/drag/pressed states and a system-colour High
Contrast fallback. Eight scrollbar brushes plus six geometry metrics are defined in every Office 2007-2024 light/dark
token dictionary; the older generations remain wider and squarer than the modern palettes.

Gallery `ScrollViewer`s still own ordinary generated `ScrollBar` controls. A local implicit style applies the same
`RibbonKit.ScrollBarStyle` to those generated parts, so `RibbonGallery` and the popup-only
`PART_PopupScrollViewer` gain the themed chrome while RKWF-018's permanent strip/popup viewport ownership remains
untouched. A realized-window test caught that a deferred gallery template could not resolve a `StaticResource` from an
earlier sibling aggregate dictionary. The aggregator still lists every `Controls.*` part directly, while Galleries
also imports the one scrollbar-template source into its deferred resource scope and keeps a small same-file adapter
style; this preserves the designer rule that `BasedOn` chains never cross part files without duplicating templates.
Ribbon Lab carries standalone vertical/horizontal examples, and its Accent gallery has enough swatches to force the
real popup overflow path. The package toolbox allowlist exposes `RibbonScrollBar`, and the README lists the public
control. The Localization/RTL lab also hosts both orientations under its live mirroring toggle. Six focused cases
cover lookless/native inheritance, commands and thumb-to-range propagation, RangeValue
automation, all theme tokens, both orientations, Showcase structure, and a realized overflowing popup whose generated
scrollbar uses the custom template and drives the popup viewport.

The preliminary live pass exposed three visual geometry defects: 12-15 DIP rails were too narrow, the `Auto` arrow
rows/columns collapsed to the small vector glyph rather than a full button, and `Margin` applied directly to `Thumb`
interfered with `Track`'s calculated pill geometry. The corrected template uses 16-DIP modern and 18-DIP 2007/2010
rails, forces each arrow button to a full thickness square, and moves vertical/horizontal inset to an inner `Pill`
border. `ButtonCornerRadius`, `ThumbCornerRadius`, and `RailCornerRadius` are independent public dependency/attached
properties, allowing any chrome to be square without replacing the template and also reaching native scrollbars generated by
gallery `ScrollViewer`s. Office 2007/2010 thumb states use outlined multi-stop gradients for their generation-specific
glass/gel treatment; Office 2013-2024 remain flat. The focused cases also pin square button geometry, public and
attached radius values, gallery theme defaults, and the legacy gradient
resources.

The next live capture showed that removing longitudinal inset was insufficient: a compact 125%-DPI probe measured a
22.4-DIP Thumb whose native proportional `Track` slot and layout clip were only 8 DIP. WPF's proportional path ignores
`Thumb.MinHeight` and instead clamps with half of the locally resolved system scrollbar-button metric. Thus the style
minimum enlarged the child behind the clip without enlarging its visible or draggable slot. The internal
`RibbonScrollBarTrack : Track` maps the active `MinThumbLength` token into Track-local vertical/horizontal system
resource keys, allowing native Track to calculate the correct slot, density, drag mapping and page-button lengths.
No application-wide system resource or ScrollBar range behavior changes. The Thumb no longer carries a conflicting
minimum or any inner inset, so the Pill uses the entire vertical rail width or horizontal rail height as requested.

The labeled Showcase example remains 56 DIP tall, which preserves the wider rail without pushing its bottom line
button beneath the generation-dependent group footer.
The new `RailCornerRadius` dependency/attached property defaults to each theme's button radius, so the track background
does not expose sharp corners behind rounded hover chrome. Office 2007/2010 line buttons also use an outlined normal
gradient to remain visibly actionable before hover; modern themes retain transparent normal button chrome. A realized
compact Office 2010 and Office 2024 cases at the current fractional desktop DPI prove that layout slot, Pill and hit
geometry agree without a layout clip; both line buttons remain inside the rounded rail host, and generation-specific
normal/radius resources apply. The user live-accepted the corrected full-width thumb and compact vertical behavior,
then requested that the flat Office 2013 and Office 2019 generations remain completely square. Their light/dark
button, thumb and rail radius tokens are now all zero, with four dedicated theme cases preventing drift. Sixteen
focused cases pass. The zero-warning Release solution build, RibbonKit **387/387**, visual **1/1**, and Writer
**439/439** pass; only the final Office 2013/2019 square-token visual recheck remains.

An adoption pilot now reuses the shared templates on the native `ScrollBar` instances generated by only the two
overflow panes in `RibbonCustomizePage`. `Controls.Customize.xaml` imports the scrollbar template dictionary into its
own deferred-template scope and defines a small same-file adapter; the implicit style lives inside the Customize Ribbon
control template, so `RibbonQuickAccessPage`, app-owned options pages and Backstage content remain unchanged. A
realized Office 2010 test forces both the available-command list and ribbon-structure tree to overflow, verifies the
shared line-button/Track template and generation radii, and proves line scrolling changes each viewport. The focused
scrollbar gate is now **18/18**. The Release solution build has zero warnings/errors; RibbonKit passes **389/389**,
visual passes **1/1**, and Writer passes **439/439**. The user live-accepted this scoped Customize Ribbon scrollbar
pilot as visually successful. The sibling QAT and optional Backstage surfaces stay outside this decision gate.

The requested follow-ups first gave all ten ordinary Customize Ribbon action/reorder buttons a Button-targeted
counterpart of the scrollbar line-button chrome, then promoted that treatment into the dialog's existing shared
`OptionsDialogActionButtonStyle`. QAT customization, Customize Ribbon, both Cancel buttons, and all compact
reorder/import/export actions now receive the same generation-aware normal background/border, hover/pressed states
and scrollbar button-radius token while preserving their established dimensions, content, keyboard focus and disabled
opacity. The primary OK style still overrides the base with its colored gel template, and the title-bar Close style
retains its Windows-red caption hover. WPF cannot assign the `RepeatButton` template object directly to `Button`, so
the shared action style contains the equivalent Button-targeted template rather than changing the accepted scrollbar.
The realized Office 2010 case pins the gel brush, border and two-DIP radius; the structural contract pins both built-in
pages and the OK/Cancel/Close exceptions. The reviewed Office 2024 RTL QAT snapshot changed only those four ordinary
buttons and its approval was refreshed. The full Release counts remain **389/389**, **439/439**, and **1/1** with zero
build warnings/errors; live visual acceptance of the dialog-wide treatment remains pending.

The next live review caught that the QAT page's two list boxes had not joined the scrollbar pilot. Its control-template
resources now apply the same local generated-`ScrollBar` adapter already used by Customize Ribbon, without changing
either `ListBox`, its items or scrolling ownership. A new realized Office 2010 case forces both available/current QAT
lists to overflow, verifies shared template parts and theme radii, and proves each viewport responds to line scrolling.
The Office 2024 RTL QAT diff was inspected before approval: only the available-list scrollbar changed to the full-width
RibbonKit thumb and arrow buttons. The focused gate is **19/19**; the zero-warning Release build, RibbonKit **390/390**,
Writer **439/439**, and refreshed visual suite **1/1** pass. Live QAT scrollbar acceptance remains pending.

Live Office 2024 review then exposed an important scale distinction: the transparent normal-state brush that suits a
small scrollbar arrow makes a full dialog action disappear into the form. The shared action style now consumes two
dedicated `Dialog.ActionBackground`/`Dialog.ActionBorder` tokens and a one-DIP outline. Office 2007/2010 duplicate the
accepted scrollbar gel and outline exactly; Office 2013/2019/2024 light/dark define simple nontransparent flat fills
and visible generation-appropriate borders. Scrollbar tokens remain unchanged, so their modern arrows stay quiet.
The action template still uses the scrollbar button-radius token, preserving square 2013/2019 and rounded 2024 chrome.
All ten dictionaries carry both new brush keys. Static coverage pins modern nontransparency and legacy gradients; a
realized Office 2024 case pins fill, border, thickness and radius. The inspected RTL QAT diff changed only its four
ordinary buttons. The focused gate is **21/21**; the zero-warning Release build, RibbonKit **392/392**, Writer
**439/439**, and refreshed visual suite **1/1** pass. Live modern-theme acceptance remains pending.

A 2026-08-31 follow-up applies the exact keyed `RibbonKit.ScrollBarStyle` to native vertical scrollbars generated by
`RibbonOptionsDialog`'s outer `PART_ContentScroll`. `OnApplyTemplate` first preserves any implicit `ScrollBar` style
already supplied by a custom template, then resolves the shared keyed style or loads its existing resource dictionary
into only that viewport and registers the same `Style` object under the native `ScrollBar` type key. This avoids a
third copied adapter and avoids a nested XAML dictionary import that broke the already-deferred Customize/QAT resource
graph; range ownership, page selection, fill-page scroll disabling and inner page viewports are unchanged. A realized
Office 2010 regression forces an ordinary tall options page to overflow, pins exact shared-style identity, template
parts and generation radii, and proves `LineDown` changes `PART_ContentScroll.VerticalOffset`. The complete focused
scrollbar class passes **22/22**, including the pre-existing Customize Ribbon and QAT overflow cases.
The accepted follow-up adds a one-DIP right margin to `PART_ContentScroll`, moving only its generated rail slightly
left so the themed chrome no longer touches the dialog edge.
The gallery-local adapter now carries the same one-DIP trailing margin for both `RibbonGallery` and
`InRibbonGallery` popup viewports, preventing their generated vertical rails from clipping against the popup card
while leaving the closed in-ribbon strip and shared scrollbar style unchanged.

RKWF-029 extends the same native-scrollbar adoption to the shared `RibbonComboBox` popup without replacing either
`ComboBox` or `ScrollViewer` ownership. The existing viewport is now the optional `PART_PopupScrollViewer`;
`RibbonComboBox.OnApplyTemplate` preserves any implicit native scrollbar style supplied by a custom template, then
resolves the exact keyed `RibbonKit.ScrollBarStyle` or imports its existing dictionary into only that deferred viewport
and registers the same `Style` object under the native `ScrollBar` type key. This follows the accepted Options pattern
without adding a cross-part `BasedOn` chain or another copied adapter/template. The popup background, border, sizing,
`ItemsPresenter`, editable/non-editable trigger and item states are unchanged; the generated rail continues to own
native range, virtualization, mouse, keyboard, RTL and DPI behavior while inheriting the existing generation tokens
and High Contrast template fallback. A realized Office 2010 theory forces overflow through both editable and
non-editable paths, pins exact shared-style identity, generation radii and native `LineDown` viewport movement, and
passes **2/2**. The Release RibbonKit project builds for `net8.0-windows` and `net9.0-windows` with zero warnings/errors.
A fresh Release Writer launch completed successfully, and the user live-accepted the overflowing font-combo scrollbar
as looking great. RKWF-029 is closed on that separate actual-window evidence.

### 3.129 RibbonKit Writer table selection normalization and vertical cell alignment — 2026-08-30

Four live table reports exposed one missing command and one shared structural-selection defect. WPF table selections
use an exclusive text end whose parent/affinity can resolve to the next physical cell. Writer had passed both raw
endpoints through ordinary cell discovery, so the selection grip omitted the final cell visually, a two-cell merge
could absorb its horizontal neighbor, and right-clicking a reverse-dragged rectangle could collapse the selection
before the context snapshot was captured.

Structural range discovery now normalizes endpoint order and resolves a non-empty end against the containing cell's
first real insertion position. WPF can project a two-cell highlight with its exclusive end inside the next cell's
paragraph/run wrappers but exactly at that insertion position; Writer therefore steps to the preceding physical cell
because no content in the containing cell is selected. Merge carries that normalized rectangle through deferred ribbon
and context-menu execution. The merge engine retains its stricter partial-span rejection. The
top-left grip selects through the last cell's `ElementEnd`, and table-aware context-menu hit testing preserves a
selection whenever the click resolves inside its normalized cell rectangle, including right-to-left and bottom-to-top
drag direction. Generic text selection keeps the existing half-open pointer rule.

Table Tools also exposes a separate Vertical Alignment dropdown using the already prepared Top/Middle/Bottom icons.
Native `FlowDocument.TableCell` has no vertical-alignment property, so Writer redistributes the cell's existing total
vertical padding while preserving horizontal padding and total padded height: all below content for Top, split for
Center, and all above content for Bottom. This matches the existing bounded row-height/padding model and remains native
undo/persistence data rather than serializing app-only state.

The earlier focused table-service, resize-adorner and context-menu slice passed **41/41**. After the final live endpoint
representation was identified from the supplied recording, only its exact merge regression was rerun and passes
**1/1** per the requested minimal-test loop. Coverage includes forward/reverse endpoints,
rightmost structural selection, neighbor-safe merge, structured right-click preservation, vertical-padding
redistribution and the prior span-safety cases. The real-tree MainWindow integration case passes **1/1**. Full Writer,
RibbonKit, visual and solution gates were not rerun; live confirmation of the four reported interactions remains
pending. No `src/RibbonKit/**` file changed.

### 3.130 RibbonKit Writer alignment range and stable table adorners — 2026-08-30

Two live alignment recordings exposed distinct Writer-owned assumptions. First, an empty cell's insertion rectangle
moves horizontally with its paragraph `TextAlignment`. The resize resolver had allowed those rectangles to influence
the table origin and perimeter, so centering or right-aligning the first cell displaced the adorner even though the
native table grid stayed fixed. The resolver now anchors the grid at `Table.ElementStart` plus native `CellSpacing`,
uses that fixed first boundary while projecting explicit column widths, and derives the perimeter from resolved row and
column boundaries rather than the union of text rectangles.

Second, the Table Tools horizontal and vertical cell-alignment handlers used the caret cell even when the native table
selection covered a rectangle. Both commands now capture the normalized `WriterTableRange` and mutate every native
cell intersecting its logical matrix exactly once, preserving one native undo unit and leaving cells outside the range
untouched.

Per the live correction loop's minimal-test request, only the two exact regressions were run: the realized 3x8 explicit-
width table retains identical bounds and row/column boundaries after first-cell centering, and a 2x2 selection within a
2x3 table receives both horizontal and vertical alignment without changing its third column. The focused result is
**2/2**. Full Writer, RibbonKit, visual and solution gates were not rerun; live confirmation remains pending. No
`src/RibbonKit/**` file changed.

### 3.131 RibbonKit Writer table-placement persistence — 2026-08-30

The live W3-E2 save/reopen check confirmed that resized table geometry persists correctly, but selecting any Table
Tools Left/Center/Right placement made the next `.rkw` save fail with an invalid block-margin value. Native WPF
`Table.Margin` defaults to `Auto`/`NaN` components while it has no local value. The placement service copied its
untouched vertical components into a newly local `Thickness`, so XAML emitted `Auto` even though only the finite left
placement was intended. Writer's strict native-package validator correctly rejects that non-finite margin.

The Writer-owned placement mutation now preserves finite top/bottom margins and materializes inherited non-finite
defaults as zero before setting the local margin. The later §3.133 correction supersedes the original one-sided
offset: finite left/right margins now encode Left, Center or Right placement so the intent survives both `.rkw`
round-trip and view-width changes. Table/cell text alignment is unchanged. Focused coverage exercises the service
contract plus Left, Center and Right placement through real `.rkw` save/load. Full Writer, RibbonKit, visual and
solution gates were not rerun. The user confirmed the corrected alignment save/reopen path in the fresh Writer build;
ordinary resized geometry had already passed the same live save/reopen check. No `src/RibbonKit/**` file changed.

### 3.132 RibbonKit Writer table-adorners under editor zoom — 2026-08-30

The remaining W3-E live batch found the table frame and grips drifting away from native grid edges when Writer zoom
changed in either Paper or Continuous view. `TextPointer.GetCharacterRect` and the Writer adorner both operate in the
adorned `RichTextBox`'s local coordinate space; WPF then applies the editor's `LayoutTransform` to the complete
editor/adorner pair. The table resolver had additionally multiplied its inferred boundaries by that same zoom, so the
chrome was projected twice while the table was projected once.

The Writer-owned resolver now keeps table geometry and pointer deltas in the shared local coordinate space and leaves
the single visual zoom transform to WPF. Realized 50%, 150% and 200% cases plus the existing resize rollback/commit
regression pass **4/4**. Full Writer, RibbonKit, visual and solution gates were not rerun. The user accepted grip/frame
alignment in both Paper and Continuous views. The separately reported semantic placement and pictured-PDF failures
are corrected in §§3.133-3.134 and await their combined live confirmation. No `src/RibbonKit/**` file changed.

### 3.133 RibbonKit Writer semantic table placement across editing views — 2026-08-31

Paper uses a finite page content width while Continuous uses the live editor viewport. The original table-placement
command persisted only a left offset calculated from whichever view was active, so Center and Right were coordinates,
not semantic placements: switching views kept the stale coordinate, and re-centering there displaced the table after
returning to Paper. Native WPF `Table` has no horizontal-placement property and `Auto` block margins do not align it.

Writer now persists the unused horizontal remainder across both finite margin sides: Left is `(0, remainder)`, Center
splits the remainder evenly, and Right is `(remainder, 0)`. A Writer-owned projection recomputes those margins after a
Paper/Continuous or viewport-width change. WPF records even a presentation reflow as a native edit; its internal
`BeginChangeNoUndo` path also clears existing history. The bounded net8 Writer bridge therefore snapshots the native
undo count and redo stack, applies only the margin projection, removes only the unit that projection appended, and
restores Redo. MainWindow suppresses dirty/preview invalidation only while that projection is active. Explicit Table
Tools placement remains a normal undoable mutation.

The focused service/persistence/undo gate passes **6/6**, and the realized MainWindow view-width regression passes
**1/1**: Center receives equal margins at both widths, returns to the original Paper margins, does not mark a clean
document dirty, and preserves the preceding native Undo/Redo history. Full gates were not run. The user confirmed the
corrected cross-view placement in the fresh Writer build. No `src/RibbonKit/**` file changed.

### 3.134 RibbonKit Writer pictured virtual-printer submission — 2026-08-31

The accepted preview pipeline eagerly serializes an isolated `FlowDocument` clone into an in-memory fixed XPS package.
Submitting that fixed paginator to Microsoft Print to PDF performs a second XPS serialization. Text-only fixed pages
survived that path, but fixed pages containing bitmap package resources caused the process to terminate during the
device spool write.

The snapshot now retains two paginator surfaces over the same isolated clone and page settings: the stable fixed
paginator remains the preview/navigation surface, while the clone's flow paginator is submitted to physical or virtual
printers. Thus preview remains immutable and printing never touches the live editor, but a picture is serialized only
once into the device spool instead of being copied out of an already fixed package. Printer imageable-area analysis and
logical margins are unchanged.

The focused pictured-spool and print-service contract cases pass **2/2**, and the existing real MainWindow lifecycle
case passes **1/1** with both ordinary and coloured-page snapshots submitting the isolated print paginator. Full gates
were not run. The user confirmed that Microsoft Print to PDF containing a picture completes correctly in the fresh
Writer build. No `src/RibbonKit/**` file changed.

### 3.135 RibbonKit Writer W3-E live acceptance closure — 2026-08-31

The user completed the remaining W3-E live matrix in the actual Writer window. Structured context menus, contextual
Table/Picture Tools stability, picture selection/removal/direct resize, table selection and merge scope, cell-range
horizontal/vertical alignment, Left/Center/Right table placement, row/column/overall resize, Escape cancellation,
Undo/Redo, save-close-reopen, Paper/Continuous zoom geometry, preview chrome exclusion and pictured Microsoft Print
to PDF all passed. The only batch failures were the zoom double-projection, cross-view placement coordinate and fixed-
XPS bitmap spool path corrected in §§3.132-3.134; their fresh-build rechecks passed.

Per the user's explicit minimal-testing direction, closure relies on the focused **8/8** combined regression, the
realized cross-view **1/1**, the existing real-window print lifecycle **1/1**, the zero-warning Writer build and the
completed live matrix. Full Writer/RibbonKit/visual/solution gates were intentionally not rerun, so no new full-suite
inventory is claimed. W3-E is accepted. W4-A has not begun. No `src/RibbonKit/**` file changed.

### 3.136 RibbonKit Writer W4-A customization and appearance persistence — 2026-08-31

Writer now owns two independent local settings files. `appearance.json` is a schema-versioned app contract for Office
generation, light/dark-black palette, default/custom accent, accented title bar, Backstage design/translucency, DWM
backdrop, compatible frame/application-button presentation, global ribbon motion, system reduced-motion policy and
the Paper-view ruler/margin-guide toggles. `ribbon-layout.json` contains only
`RibbonCustomizationSerializer` output. Corrupt, future-schema or invalid appearance data falls back to factory
appearance without touching ribbon customization; appearance defaults likewise leave the ribbon/QAT layout intact.

File > Settings and the ribbon/QAT context-menu requests now open one **Settings**-captioned
`RibbonOptionsDialog`. Its app-owned **Appearance** page sits beside RibbonKit's built-in **Customize Ribbon** and
**Quick Access Toolbar** pages. Appearance edits preview live; OK persists both independent channels, the page's Apply
action establishes a new rollback snapshot, and Cancel restores the latest uncommitted appearance and structural
ribbon snapshots. Appearance defaults reset only that page. Structural Import/Export/Reset never includes appearance,
document content/page settings or transient modal/merge state.

Compatibility-dependent controls stay visible and disable with an explanation. Historical Backstage designs, Aero
frames and the Orb normalize to generation-compatible fallbacks; historical Aero frames accept only None/Acrylic;
unsupported system backdrops render as None without erasing a portable stored request; Backstage translucency requires
an active supported backdrop and excludes Classic2007. Theme changes re-theme the open dialog and refresh its native
dark-chrome hint while preserving its selected page and pending values. All implementation remains under Writer-owned
sample/test files; no `src/RibbonKit/**` file changed and no new consumer-friction entry was needed.

The first live review exposed two app-owned composition mistakes. Writer's page workspace inherited
`Control.CompanionBackground`, which is transparent over the white Office 2007/2010 window surface and blue-tinted in
Office 2013; it now uses one neutral Writer-owned gray (`#E7E7E7` light, `#343434` dark, or the system Control brush in
High Contrast) across generations. The presentation host, editor surface and viewport become transparent only after a
requested DWM backdrop is actually active, so Mica/Acrylic/Tabbed can show behind the white paper. Office 2007 now
always normalizes to the Orb while every later generation normalizes to the File tab; this also gives Classic2007 the
application-button anchor its shared chrome requires. The incompatible alternative remains visible but disabled with
an explanation on the Appearance page.

Per the user's minimal-testing direction, the focused persistence, schema, separation, compatibility, platform-fallback
and Appearance-page automation gate passes **6/6**. A fresh Debug Writer build succeeds with zero warnings/errors and
the actual executable reaches its main window. Full Writer/RibbonKit/visual/solution gates were intentionally not run.
Live Settings, restart, corrupt-file, Apply/Cancel, every generation/palette/Backstage/frame/backdrop combination and
reduced-motion review formed the W4-A acceptance boundary at this implementation checkpoint.

The user then confirmed the Settings defaults, disabled compatibility choices, Cancel rollback, Apply behavior and
restart persistence. The follow-up visual polish gives every Backstage navigation item a vector silhouette, corrects
Settings to an even-odd gear with a true center cutout, and adds one Writer W identity as a nine-frame executable icon
and app-owned Office 2007 Orb mark. The Orb keeps RibbonKit's themed sphere and interaction states; a post-render
Writer hook replaces only the glyph template and Classic2007's existing proxy reuses it. RKWF-026 records the missing
host-level glyph hook. The focused realized icon/identity regression passes **1/1**; fresh-window icon acceptance is
recorded by the later W4-A live correction sequence and closure below.

The first live icon pass was rejected: its reuse of layered ribbon artwork ignored Backstage's single-color opacity
mask, so New, Save and Save As collapsed into nearly identical blocks and Open/Print lost useful structure. Five
dedicated Backstage-only masks now encode their distinguishing details as solid outlines and real negative space:
page-plus, open folder, cut-out floppy disk, floppy disk with pencil, and printer. Home and the corrected gear remain
unchanged; the richer ribbon resources are no longer reused on this monochrome surface.

The near-final live review retained two mask details: Open's rear folder outline now uses a genuinely closed left
edge behind the solid front flap, and Save As gives the diagonal pencil a transparent center stripe. The focused
identity/navigation regression asserts both negative-space contracts.

The final Appearance-page review removed the page's duplicate `Appearance` heading because the
`RibbonOptionsDialog` container already renders `SelectedPage.Header`. Writer's Accent, Appearance defaults and Apply
buttons now consume RibbonKit's existing dialog action-button styles. The visible content scrollbar does not belong
to the page: it is generated inside the container's `PART_ContentScroll`, which currently does not apply the existing
keyed `RibbonKit.ScrollBarStyle`. RKWF-027 records that separately approved container correction; Writer cannot safely
reach the internal native scrollbar and no `src/RibbonKit/**` file changed here.

The first button-style attempt referenced those keys dynamically but did not merge their defining dictionary into
the custom page, so WPF retained stock button chrome. Writer now follows its other app-owned dialogs by merging
`Controls.OptionsDialog.xaml` locally; the focused page regression requires all three buttons to resolve to the exact
RibbonKit action/primary `Style` instances rather than merely carrying unresolved resource expressions.

The initial Writer Orb mark used conventional white glyph chrome, but the active Office 2007 sphere is pale enough
that the W visually merged into it. The mark now uses the same fixed `#3F94DF` to `#145AA6` blue identity gradient as
the application artwork; RibbonKit still owns the sphere, ring, shadow and interaction states. The focused identity
regression pins both gradient stops.

Writer's ruler now reveals the active window material through its base surface only when Office 2024 is selected and
a requested backdrop actually activates. Older Office generations, no-material/platform fallback and High Contrast
retain the accepted opaque ruler surface. Ticks, border, markers and margin-zone shading remain rendered above the
material; a focused compatibility regression pins the four-way theme/backdrop/High-Contrast boundary.

The ruler's paragraph-indent markers, ticks and other chrome resolve theme brushes manually during `OnRender`.
Replacing the application theme/accent resource dictionaries therefore did not by itself invalidate this custom
surface; unrelated layout sometimes hid the defect by causing a coincidental redraw. The shared appearance pipeline
now explicitly refreshes the ruler after applying theme, accent, backdrop and dark chrome, so live preview, Apply and
Cancel rollback all repaint from the same final resources without changing ruler geometry.

The app-owned `DocumentPresentationHost` now locally merges RibbonKit's shared scrollbar dictionary and adapts its
keyed native `ScrollBar` style as one implicit style. This changes chrome only: Paper view's outer viewport,
Continuous view's native RichTextBox viewport and Print Preview retain their existing scrolling ownership, templates,
keyboard/automation behavior and view-switching state. The font-family popup remains a separate RibbonKit runtime
candidate because its `ScrollViewer` is created inside the shared `RibbonComboBox` template.

Writer's scoped adapter gives vertical main-content scrollbars a one-DIP trailing margin, matching the accepted
Options-dialog content inset while leaving horizontal scrollbars uninset. Applying the inset to the generated
`ScrollBar` rather than the editor/preview `ScrollViewer` keeps paper centring and viewport geometry unchanged.

Writer's Home/Recent and New Backstage pages now opt into that same shared native scrollbar chrome. New retains its
existing cards viewport; Home replaces its unbounded vertical `StackPanel` with a three-row grid and scrolls only the
recent-document list beneath the fixed heading and description. Both page-local bars retain their native zero margin
because their padded page regions already keep them away from the window edge; Backstage's shared navigation/content
template remains unchanged.

The first maximized-window review exposed a separate WPF alignment trap: Writer had applied `MaxWidth` while leaving
the capped Home description/recent-row content, New description and complete Print page on stretch alignment. WPF
centred each capped child in the widening layout slot, so its left edge drifted right as the window grew. Those capped
surfaces are now explicitly left-aligned; the intentional fixed Backstage content padding remains unchanged.

The final Print-page review confirmed that its right-side surface was intended as a compact page-setup summary card,
not a full-height preview pane. Top-aligning that app-owned border removes the accidental default WPF stretch without
changing its content or column layout. The user accepted the resulting actual window and explicitly closed W4-A on
2026-08-31. Closure retains the original focused **6/6** appearance gate, the proportional focused follow-up checks
and repeated zero-warning Writer builds; the final one-property card alignment was live-accepted without another test
or build at the user's request. Full Writer/RibbonKit/visual/solution suites were intentionally not rerun, so no new
full-suite inventory is claimed.

### 3.162 RibbonKit Writer expanding Paper default and growing margin guide — 2026-09-15

Paper uses one centered native editor with fixed page width and minimum page height.
The sheet and dotted guide grow downward with content. The guide previously stayed at
one physical page's content height; it now follows the measured paper interior minus
scaled margins. That measured height already includes zoom. Print Preview and printing
retain physical pages, while editing keeps native selection, history and object interaction.

The initial surface/ruler gate passed 20/20 with growth, scrolling and Undo/Redo at
75/100/150% zoom. A live Release window showed the dotted bottom following 1,354 words
and an added newline, then shrinking with Undo. These are bounded checks, not a benchmark.

Subsequent cleanup: 77 focused tests passed across the editor, ruler, pictures, table
resize, preview and actual-window view switching. Release Writer build: zero warnings/errors.
The combined window run encountered WPF WindowChrome cross-thread caching; the affected
check passed in a fresh process. No new live UI or full-suite acceptance is claimed.
