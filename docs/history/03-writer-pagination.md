# Writer pagination history

> Historical evidence, moved without changing its technical content on 2026-09-08.
> Dates, test counts, pending work, and instructions describe their original checkpoint;
> later entries may supersede them. Use [current status](../../04-DESIGN-NOTES.md#5-current-state--next-steps)
> and [AGENTS.md](../../AGENTS.md) for present work. Read only the relevant numbered entry.

### 3.137 RibbonKit Writer W2-G editable-pagination feasibility — 2026-08-31

The bounded W2-G architecture proof stops before any production Paper-view replacement. A deterministic 180-paragraph
Letter corpus establishes the useful half of stock WPF's contract: the live `FlowDocument` attached to one
`RichTextBox` can also supply a `DynamicDocumentPaginator`; a range can cross the paginator's first page boundary,
replacement typing/deletion remains in the native Undo/Redo stack, and after Undo the live page count and every page-
start symbol offset match the accepted W2-D isolated preview/print-input paginator. One authoritative document can
therefore own editing semantics and paginator-consistent break metadata.

Stock WPF does not provide the corresponding editable paged presentation. The `RichTextBox` renders through its
internal bottomless `FlowDocumentView`, contains no `DocumentPageView`, and lays the same multipage corpus as one
continuous surface without repeated per-page margins. `FlowDocumentPageViewer` supplies real finite pages and a range
`TextSelection`, but exposes no caret or native Undo surface and its Delete, Paste, Undo and Redo command routes remain
disabled. Assigning the same live document to both controls succeeds as an object reference, but realizing them
together produced a framework `NullReferenceException` in `FlowDocumentPage.UpdateViewport`; the poisoned follow-up
layout then terminated the test host with an invalid `ScrollViewer` size. That intentionally destabilizing case is
recorded as measured prototype evidence rather than retained as a passing regression.

The feasibility decision is therefore **no for a stock-WPF production replacement**. The accepted W2-C Paper surface
and W2-D preview/print pipeline are unchanged, no runnable Writer code was added, and no `src/RibbonKit/**` file changed.
The retained focused characterization passes **3/3**; no full suite, standalone build or application launch was run.

Any later implementation must be a composite editing architecture, not a different arrangement of stock viewers. The
long-lived `RichTextBox` remains the sole authoritative document, selection, command, IME, spelling and Undo owner. An
isolated, disposable pagination clone may supply non-authoritative page visuals only. A generation-stamped page map
must translate source symbol offsets and structured-object identities to page-local rectangles and hit-test results;
the paged surface must project caret, selection and W3-E chrome back to that owner, invalidate stale generations after
edits/reflow and never persist its cache. This requires a supported bidirectional geometry seam; WPF's public paginator
currently exposes page starts but not a complete editable text-view map, so relying on `MS.Internal` text services or
the crashing shared formatter is prohibited.

The next bounded W2-G slice is a **public page-geometry map spike**, still isolated from production Paper. For plain
paragraphs first, prove source-offset to page/local-caret-rectangle and page-point to source-offset mapping against an
isolated clone, then repeat with one page-spanning table and image while measuring rebuild latency. Stop if public WPF
APIs cannot provide stable bidirectional geometry or if the clone map cannot preserve source identity; in that case the
next decision is a supported document-editor/layout engine or a purpose-built editor, not simulated WPF page gaps.

### 3.138 RibbonKit Writer W2-G public page-geometry map spike — 2026-08-31

The isolated public-API spike succeeds without changing production Paper or using `MS.Internal`. It paginates the
accepted `WriterPreviewCloneService` clone in a standalone `FlowDocumentPageViewer`, visits one real page at a time and
uses `DynamicDocumentPaginator` page positions plus `TextPointer.GetCharacterRect` to build page-local insertion
geometry. A cached nearest-caret lookup supplies the reverse page-point to clone/source-offset mapping. Source and clone
symbol offsets remain identical for every sampled paragraph and table-cell target, so the clone stays a disposable
presentation cache while the live document remains authoritative.

The deterministic plain Letter corpus produces six pages. Every page start resolves to the same finite page-local
caret rectangle, six sampled offsets round-trip exactly through point hit testing, and the complete public map contains
10,440 insertion entries. The refreshed Debug run built that map in about **0.73 seconds**; an earlier cold run took
about **1.11 seconds**. Snapshot/page-start sampling ranged from roughly **0.23–0.44 seconds** across cold and warm runs.
This is viable for a debounced/virtualized generation, not synchronous work on every keystroke.

The structured corpus produces five pages. Its 55-row table spans pages one through four; sampled first-column offsets
on all four pages preserve source identity and round-trip exactly. The 5,216-entry map took about **0.45 seconds** after
an approximately **0.27-second** snapshot. A cloned 180×120 image exposes finite page-local bounds and the public
`DocumentPageView.InputHitTest` returns the exact cloned `Image`, proving a supported object-hit seam as well as text
geometry. All captured rectangles remain inside their realized page bounds.

The focused spike passes **3/3**. No full suite, standalone build or application launch was run; no runnable Writer code
or `src/RibbonKit/**` file changed. These results prove a supported geometry primitive, not an editable-page product.
Full-document insertion enumeration is deliberately retained only in the test prototype because its measured latency
requires current/adjacent-page virtualization, edit debouncing and generation cancellation before integration.

The next bounded W2-G slice is a **clone-backed caret/selection compositor prototype**, still outside production Paper
and initially limited to plain paragraphs. Keep one separately realized live `RichTextBox` as the native selection,
typing, deletion and Undo/Redo owner; render only an isolated paginated clone. Project the live caret and a cross-page
range onto page-local overlays, map a page click/drag back to exact live offsets, then rebuild through a generation-
stamped debounce after typing, deletion, Undo and Redo while rejecting stale results. Map only the visible and adjacent
pages and measure latency. Stop before IME, spelling, clipboard, tables/images and W3-E adorners; those remain later
proof gates after the compositor preserves the basic native editing target without simulated breaks.

### 3.139 RibbonKit Writer W2-G paragraph caret/selection compositor prototype — 2026-08-31

The test-isolated compositor proof succeeds for plain paragraphs without changing production Paper. One realized live
`RichTextBox` remains the sole owner of the authoritative `FlowDocument`, selection, caret and native history. A separate
`FlowDocumentPageViewer` realizes only the accepted paginator clone; page-local `Canvas` overlays project the live
selection and caret without inserting content or sharing the live formatter. A page-one click maps back to the exact
live WPF symbol offset, and a drag across the first page boundary selects offsets 2,129–2,147. The composed result has
selection geometry on both genuine pages and the caret only on the destination page.

Replacement typing across that boundary, command-routed deletion of the inserted run, two Undos and two Redos all keep
the original live document instance authoritative. Each edit schedules a new clone generation, and every published
clone contains the exact current live text while remaining a distinct document. The history sequence reached generation
seven without replacing the live editor or its undo manager.

The prototype maps only the visible page and its immediate neighbours. With page three visible in the deterministic
six-page Letter corpus, it visits pages two through four and caches 6,090 insertion entries. A warm measured generation
took about **77–94 ms** for the accepted snapshot clone and **0.36–0.48 seconds** for the three-page public geometry map.
Canceled callbacks were invoked deliberately after later requests; identity and generation guards rejected them, and
two queued edits published only the newest text. This proves bounded virtualization and stale-result rejection, but the
map is still too slow to build synchronously on Writer's UI dispatcher without a perceptible pause.

The focused compositor gate passes **3/3**, and the combined W2-G pagination gate passes **9/9**. No full suite,
standalone build or application launch was run; no runnable Writer code or `src/RibbonKit/**` file changed. Together
with §§3.137–3.138, the feasibility decision is **positive for
a clone-backed custom compositor architecture**: one native editor can retain caret, cross-page typing/deletion/range
selection and Undo/Redo while isolated real pages retain the accepted preview/print break contract. It is not evidence
that stock `RichTextBox` can display those pages, nor approval to replace production Paper. IME, spelling, clipboard,
focus/command routing, page-setting reflow, structured objects, DPI/RTL and W3-E chrome remain unproved.

The next bounded W2-G slice should be a **dedicated-STA layout-worker spike**, still test-only and paragraph-only. Capture
one immutable generation from the live document, paginate and build visible/adjacent-page geometry on a separate STA
dispatcher, marshal only immutable page/offset rectangles back to the UI dispatcher, and reject an older worker result
after a newer edit or visible-page request. Measure UI-dispatcher capture time, worker latency and typing responsiveness.
Stop if public WPF pagination cannot be isolated safely across dispatchers; in that case the concrete implementation
decision is to replace the WPF clone compositor with a supported editor/layout engine rather than block typing or fake
page gaps.

### 3.140 RibbonKit Writer W2-G dedicated-STA layout-worker spike — 2026-08-31

The paragraph-only worker spike succeeds: public WPF pagination and realized page geometry can run on a persistent STA
thread separate from Writer's editor dispatcher. The UI thread captures one immutable XAML-package generation plus only
primitive document/page formatting. The worker owns every clone, paginator, `FlowDocumentPageViewer`, `Window` and
`TextPointer` it creates; it returns only immutable page counts, page-start offsets, page numbers, source offsets,
primitive rectangles, content text and measurements. No dispatcher-owned WPF object crosses back to the editor.

For the deterministic Letter corpus, the worker produces six pages and every page-start offset exactly matches the
accepted `WriterPreviewCloneService` print-input paginator. With page three visible it maps only pages two through four,
returning 5,775 finite insertion rectangles. The refreshed runs captured the live generation on the UI dispatcher in
about **7.6–14.4 ms** and completed clone pagination plus three-page geometry on the worker in about **0.57–0.63
seconds**. The worker cost remains substantial, but it no longer stalls typing or input dispatch.

A deliberately gated race proves the separation. While generation one was blocked on the worker, the UI dispatcher ran
an input-priority callback and appended text plus captured generation two in about **8.5 ms**. After release, generation
one completed but its UI publication was rejected; only generation two's exact current text and geometry were accepted.
The prototype therefore proves supported cross-dispatcher isolation and generation rejection. It does not yet avoid the
wasted cost of fully processing a stale active request or collapse multiple queued edits.

The focused worker gate passes **2/2**, and the combined W2-G pagination gate passes **11/11**. No full suite,
standalone build or application launch was run; no runnable Writer code or `src/RibbonKit/**` file changed. The
feasibility decision remains positive for the clone-backed compositor: its
expensive pagination/map stage can leave the native editor dispatcher while preserving the accepted page-break contract.
This is still architecture evidence, not approval to replace production Paper.

The next bounded W2-G slice should be a **latest-generation coalescing and cooperative-cancellation worker spike**,
still test-only and paragraph-only. Retain at most one active and one newest pending generation, supersede intermediate
queued captures, and check cancellation between page realization and insertion-geometry batches so a stale long map
cannot delay the newest viewport. Prove a rapid edit burst publishes only the final text, bounds completed worker work
to the active plus final generation and leaves the editor responsive. Stop before production integration, IME, spelling,
clipboard, structured objects, DPI/RTL and W3-E chrome.

### 3.141 RibbonKit Writer W2-G latest-only worker coalescing and cancellation — 2026-08-31

The dedicated-STA prototype now retains exactly one active and one newest pending generation. A new request cancels the
active token and atomically replaces any older pending capture; the superseded pending request completes without ever
creating WPF layout objects. The active worker checks cancellation before pagination, before every realized page, after
every page and every 128 insertion positions. Publication still requires the current UI generation and live document
identity, so cancellation, queue coalescing and the existing UI guard are independent protections.

The focused burst deliberately pauses generation one after it has completely mapped one of three requested pages, then
performs 12 separate live-editor appends/captures. Those 13 total generations start only **two** worker jobs: the stale
active job and the final pending job. Generation one stops after its first mapped page, 11 intermediate captures are
superseded before start, exactly one worker result completes, and only the final generation's exact live text publishes.
The complete 12-edit/capture burst took about **97 ms** on the UI dispatcher; each individual immutable capture stayed
below the existing 250-ms responsiveness ceiling, and the final three-page worker generation took about **0.59 seconds**.

The refreshed dedicated-worker gate passes **3/3**, and the combined W2-G pagination gate passes **12/12**. No full
suite, standalone build or application launch was run; no runnable Writer code or `src/RibbonKit/**` file changed. The
bounded scheduling decision is positive: an expensive stale map need not form a backlog or delay the newest viewport.
The prototype remains test architecture, not a production Paper replacement.

The next bounded W2-G slice should be a **paragraph focus and native-command-target bridge prototype**, still test-only.
Route clone-page click and drag geometry to the live `RichTextBox`, explicitly retain or restore its keyboard focus, and
prove that native typing, Delete/Backspace, Undo/Redo and selection commands continue targeting that editor while an
asynchronous page generation swaps underneath it. Also prove that a stale page event cannot move the current live caret.
Stop before system clipboard mutation, IME composition, spelling, production integration, structured objects, DPI/RTL
and W3-E chrome; those require later focused gates after the keyboard target is stable.

### 3.142 RibbonKit Writer W2-G paragraph focus and native-command bridge — 2026-08-31

The test compositor now stamps every page click/drag payload with the published geometry generation. An accepted event
maps its page point to the exact live symbol offset, changes only the authoritative `RichTextBox` selection and restores
that editor as both the `Window`'s logical focus element and the keyboard focus target. Events whose generation no longer
matches the displayed map, or whose page is outside that map, return before resolving an offset or touching focus/caret.

The focused command proof first gives the isolated `FlowDocumentPageViewer` focus, then drags a genuine cross-page range.
The bridge returns focus to the live editor; the viewer still cannot execute Delete or Undo, while the editor performs
replacement typing, routed Backspace, routed Delete, two routed Undos, two routed Redos and routed Select All through its
native command bindings/history. The final current text is cloned at generation eight without replacing the live editor.
Native Select All is characterized by its actual insertion-position contract: the selected text equals the complete
document text rather than requiring selection endpoints to equal `FlowDocument` element-boundary pointers.

A second proof retains live-editor focus across a scheduled clone/map swap. After generation two publishes, a captured
generation-one page event is rejected and cannot move the current live caret (offset 2,161 in the deterministic corpus).
The focused bridge tests pass **2/2**, and the combined W2-G pagination gate passes **14/14**. No full suite, standalone
build or application launch was run; no runnable Writer code or `src/RibbonKit/**` file changed. This proves the basic
keyboard/command ownership seam, not OS-level IME, spelling or clipboard behavior and not production Paper integration.

The next bounded W2-G slice should be a **paragraph page-setting reflow and live-anchor proof**, still test-only. Change
Letter/A4 orientation and margins while a live caret and cross-page range exist; rebuild through the latest-only worker,
compare every resulting page start/count with the accepted preview/print paginator, preserve the authoritative document,
focus and logical symbol anchors, and reject events from the pre-reflow page generation. Stop before system clipboard,
IME, spelling, production integration, structured objects, DPI/RTL and W3-E chrome.

### 3.143 RibbonKit Writer W2-G paragraph page-setting reflow and live anchors — 2026-08-31

Page settings are now part of each immutable test generation rather than mutable worker state. The live UI capture carries
its exact dimensions, orientation-derived width/height, content width and four margins to the latest-only STA worker; the
immutable result returns that same specification with its page count, every page start and visible/adjacent geometry.
Changing settings schedules a new generation without replacing or rewriting the authoritative live document.

The worker proof changes the deterministic corpus from six-page portrait Letter with normal margins to seven-page A4
landscape with custom 48/72/60/84-DIP margins. Every A4 page-start offset and the page count exactly match a fresh
accepted `WriterPreviewCloneService` print-input paginator for those settings, and they differ from the Letter breaks.
The reflow capture took about **6.6 ms** and the worker generation about **0.44 seconds**. The original live document,
keyboard/logical focus and cross-page selection offsets 2,028–2,032 remain unchanged.

The compositor proof independently retains its cross-page offsets 2,129–2,147 and live-editor focus while swapping to
the same A4 landscape/custom-margin clone. The clone exposes the exact 1,122.52×793.70-DIP logical page, generation two
replaces generation one, and a pre-reflow page event is rejected without collapsing or moving the preserved range.

The focused reflow gate passes **2/2**, and the combined W2-G pagination gate passes **16/16**. No full suite, standalone
build or application launch was run; no runnable Writer code or `src/RibbonKit/**` file changed. Page-setting reflow is
therefore feasible inside the clone-backed compositor without sacrificing paginator agreement or live editing anchors.
This remains prototype evidence, not production Paper integration.

The next bounded W2-G slice should be a **paragraph native-spelling projection spike**, still test-only. Keep WPF
`SpellCheck` on the focused live editor, obtain a deterministic misspelling range through public APIs, project its live
symbol offsets to page-local overlay geometry, and prove a native correction remains undoable/redoable and survives one
page-setting reflow while stale spelling geometry cannot publish. Stop if installed language services cannot provide a
deterministic automated corpus; record that limitation rather than simulating spelling. Continue to defer system
clipboard, IME composition, production integration, structured objects, DPI/RTL and W3-E chrome.

### 3.144 RibbonKit Writer W2-G paragraph input-services prototype batch — 2026-08-31

Three bounded paragraph input-service proofs now sit on the same generation-stamped compositor while the realized live
`RichTextBox` remains their only editing owner.

Native WPF spelling is deterministic in the current `en-US` environment. The live editor reports the deliberate
`qzxwvv` error at symbol offsets 2,748–2,754 through public spelling APIs; those offsets project to a red page-local
overlay on the genuine clone page. `SpellingError.Correct("spelling")` remains one native undoable/redoable edit. The
correction persists through A4 reflow to generation six, while the generation-two spelling token can no longer publish.
This proves spelling ownership, public-range projection and stale-geometry rejection without copying WPF's dictionary or
spelling engine into the paged presentation.

Synthetic WPF `TextCompositionManager.StartComposition` raises one start and one completion event with the live editor as
the original source; in this route WPF auto-completes the composition immediately. Focus remains on the editor across a
subsequent page swap and the old page event is rejected. This is useful command/input routing evidence, but it is **not**
evidence for a genuine OS IME candidate window, reconversion or target-language composition session. Those remain a live
manual gate; the prototype does not fake them with ordinary text insertion.

The native clipboard proof selects 16 characters across a real page boundary and executes routed Copy, Cut, Paste, Undo
and Redo against the focused live editor. The clone publishes the final current text at generation six. The test preserves
and restores the pre-existing system clipboard data object in `finally` and does not report its contents. This establishes
native clipboard/history ownership without introducing a second document owner or a compositor-specific clipboard.

The focused input-services batch passes **3/3**, and the combined W2-G pagination gate passes **19/19**. No full suite,
standalone build or application launch was run; no runnable Writer code or `src/RibbonKit/**` file changed. Spelling and
clipboard feasibility are positive; synthetic composition routing is positive while genuine OS IME behavior remains
explicitly unaccepted pending live verification. Production Paper is still unchanged.

The next batched W2-G prototype should remain test-only and combine three related structured/viewport slices: preserve
page-spanning table, image and hyperlink anchors through latest-only reflow; project W3-E table/picture selection chrome
as non-printing page-local geometry; and characterize the same maps under RTL plus zoom/DPI coordinate transforms. Keep
the live editor/document and accepted preview/print paginator authoritative, reject stale object events, and stop before
production Paper replacement. Genuine OS IME remains a separate later live gate rather than an automated claim.

### 3.145 RibbonKit Writer W2-G structured-content and viewport prototype batch — 2026-08-31

The bounded test-only prototype now composes structured-object and viewport behavior around the same authoritative live
`RichTextBox`/`FlowDocument` and accepted `WriterPreviewCloneService` paginator; production Paper remains untouched.

The structured corpus contains a 55-row two-column table, an inline image and a hyperlink. Sampled table, image and
hyperlink symbol offsets are identical in the source and each isolated paginator clone. Letter portrait yields six pages:
the table spans pages one through four and both later anchors are on page four. Letter landscape yields eight pages: the
table spans pages one through six and both later anchors move to page six. A native live selection spanning a table cell
to the hyperlink retains its exact endpoints, document identity and keyboard focus across both snapshots. Thus accepted
page count/break ownership and structured anchors survive a real page-setting reflow without moving edit ownership into
the clone.

The chrome proof creates page-local overlay canvases for a table fragment and the actual image bounds, then reuses
`WriterPictureResizeGeometry` for all eight pixel-aligned handles at 150% DPI. The canvases are non-hit-testable and
detached from both source and clone document trees. Source XAML remains byte-for-byte unchanged, and neither selection
tag appears in serialized paginator content. This is an isolated composition proof, not a new W3-E interaction surface;
the accepted W3-E controllers remain the production selection/resize authority.

The viewport seam starts from genuine RTL clone insertion geometry and tests the explicit page-local-to-device transform
plus its inverse over four zooms (75/100/150/200%), three DPI scales (100/150/200%) and two scroll positions. All 24
combinations return the exact original insertion offset; all W3-E picture-handle edges remain device-pixel aligned, and
the paginator page count is invariant. This proves that page-local public geometry can be kept independent of mirrored,
zoomed, scrolled device presentation rather than baking presentation coordinates into document offsets.

The focused structured/viewport batch passes **3/3**, and the combined W2-G pagination gate passes **22/22**. No full
suite, standalone build or application launch was run; no runnable Writer code or `src/RibbonKit/**` file changed.
Structured-content, non-printing W3-E chrome and RTL/zoom/DPI/scroll feasibility are positive. Production Paper is still
unchanged.

The next bounded automated W2-G batch should close the remaining lifecycle seams: reject stale structured-object events
after authoritative document replacement, restore the current editing target after focus loss/page-window handoff, and
verify virtualized visible/adjacent page eviction does not retain old document geometry. After that, genuine OS IME plus
ordinary multi-page authoring/responsiveness remain the final live feasibility gate before any production replacement
packet can be designed.

### 3.146 RibbonKit Writer W2-G lifecycle closure and production feasibility decision — 2026-08-31

The final test-only lifecycle batch closes the automated W2-G exit seams without changing production Paper. Generation
validity now requires the published geometry generation to remain the latest requested generation, so a queued edit,
reflow or replacement invalidates page events immediately rather than leaving the old page actionable until its successor
publishes.

Document replacement was exercised with both an outstanding old-document rebuild and a generation-stamped hyperlink
event. Replacement clears published geometry immediately, retains the new authoritative `FlowDocument` and live-editor
focus, rejects the old page and hyperlink events before and after the canceled callback runs, then publishes only the new
two-page document at generation three. The clone contains none of the queued old-document edit. Page-window handoff on
the six-page corpus moves the value-only map from pages one/two to pages five/six, rejects the page-one event, and restores
the live editor as the native command target only when a current page interaction is applied.

The focused lifecycle batch passes **2/2**, and the combined W2-G pagination gate passes **24/24**. No full suite,
standalone build or application launch was run; no runnable Writer code or `src/RibbonKit/**` file changed. The automated
feasibility program is complete.

The production feasibility decision is a qualified **go** for an app-owned composite paginated-editing architecture:
one authoritative native editor owns document mutation, selection, input services and Undo/Redo; immutable captures are
coalesced on one dedicated STA; generation-stamped value results describe accepted-paginator page starts and only the
visible/adjacent page geometry; paged visuals and non-printing overlays project those offsets back to the native editor.
Stock WPF still does not provide an editable paged control, so this is custom Writer composition rather than a hidden
framework mode.

One limitation remains deliberately unaccepted: synthetic WPF composition proves routing but cannot prove a genuine OS
IME candidate window, reconversion or target-language session, especially while the native input owner and projected
caret occupy different visual coordinates. Ordinary multi-page authoring and long-document responsiveness likewise need
a runnable surface. Therefore the first production packet must be opt-in and must not replace accepted Paper by default.
It should productionize the immutable capture/dedicated-STA/latest-only engine and generation contract, add a diagnostic
paginated surface that keeps the live editor authoritative, and expose a focused actual-window gate for OS IME,
cross-page authoring, focus and responsiveness. Default Paper, preview and print remain unchanged until that gate is
accepted.

### 3.147 RibbonKit Writer W2-G opt-in production compositor and LTR live decision — 2026-09-01

The first production W2-G batch is runnable but remains private and opt-in. `--writer-paginated-diagnostic` (or the
internal `RIBBONKIT_WRITER_PAGINATED_DIAGNOSTIC=1` switch) replaces only the presentation inside the existing Paper
host for that process; ordinary launches retain the accepted W2-C Paper surface. `--writer-pagination-seed` supplies a
six-page diagnostic document without affecting normal New/Open behavior. Continuous, preview, print, persistence,
ruler/margin guides and the accepted W3-E source interactions were not replaced, and no `src/RibbonKit/**` file changed.

One live `RichTextBox` and `FlowDocument` remain authoritative for mutation, selection, spelling, clipboard, focus and
native Undo/Redo. The UI dispatcher captures XamlPackage bytes, primitive document formatting, page settings and
source-offset/object-identity records; it never transfers a dispatcher-owned WPF object to the worker. One persistent
STA owns the clone, accepted `DynamicDocumentPaginator`, hidden layout host and page rendering. It keeps at most one
active and one latest pending request, cooperatively cancels active work, and publishes immutable value data only:
generation/document identity, page count/starts, visible plus adjacent page PNG bytes, insertion rectangles and
table/picture/hyperlink geometry. Disposal cancels and joins the worker deterministically.

The app-owned surface virtualizes the visible/adjacent page window, scales rendered pages for zoom, rebuilds pixel-density
page bytes after DPI changes, and projects caret, selection, native spelling, table and picture chrome without adding
document content. Page events are rejected unless generation, document identity, mapped page and structured-object
identity are current, then invert page coordinates to source symbol offsets and restore the transparent-but-realized
native editor as the command target. A measured live defect showed that `FlowDocumentPageViewer` fits its
`DocumentPageView` while page PNGs use full paginator dimensions; normalizing all public character/object rectangles
from realized view size to paginator page size aligned text/table/picture overlays. Clicking the clone-backed picture
then selected the exact live `InlineUIContainer`, exposed Picture Tools and left keyboard focus on `Document editor`.

The Release surface was exercised in the actual Writer window at 125% DPI. Six genuine pages rendered; cold capture was
about 40–49 ms and cold layout about 776–921 ms, while cached viewport capture was 0.4–3 ms and warm layout about
370–585 ms. Page clicks recovered native focus; cross-page drag produced a 536-character live selection; replacing it,
Backspace, native Undo/Redo, and clipboard paste all mutated only the live document. Undo restored the exact 9,605-
character pre-replacement text and Redo restored the 9,078-character replacement. After the page-view normalization,
the reduced spelling probes underline `qzxwvv` at the end of the intended lines rather than the middle of each line;
page-window handoff reached the table/picture page, and portrait-to-landscape reflow changed the seed from six
to eight pages without accepting stale geometry. Table chrome followed the page-spanning table; picture chrome aligned
with the rendered image and activated the existing source picture workflow.

The focused production tests pass **3/3** and the namespace-scoped W2-G gate passes **27/27** after making the prototype
clone-measurement window explicitly non-activating. A fresh Release Writer build passes with **0 warnings / 0 errors**.
No full suite or solution build was run. The LTR production-composition decision is a qualified **go for continued
opt-in hardening**, not approval to replace default Paper.

At the user's direction, genuine OS IME and RTL are deferred together. This batch makes no candidate-window,
reconversion, target-language composition or production RTL claim; the earlier synthetic/transform proofs remain only
automated feasibility evidence. The next bounded production slice should keep the surface opt-in and add page-local
table/picture resize handles plus ruler/margin-guide projection and an actual-window document-replacement check. A later
paired RTL/IME input-bridge packet should resume only when those two concerns are scheduled together.

### 3.148 RibbonKit Writer W2-G page-local resize and page-chrome hardening — 2026-09-01

The second private production batch keeps the same opt-in boundary and single authoritative editor. Clone-page picture
selection now projects all eight fixed-screen-size W3-E handles; table selection projects the existing overall
bottom-right handle on the last mapped table fragment. Pointer start/update/commit/cancel events carry generation,
document identity, mapped page, object identity, object kind and handle identity. The controller rejects any mismatch,
then delegates preview and the one-unit commit to the existing live `WriterPictureInteractionController` or
`WriterTableResizeController`. Escape, capture loss, page-setting invalidation, disposal and authoritative document
replacement cancel the live W3-E drag and discard the page-local preview. No clone or overlay owns mutation or history.

The diagnostic also owns a non-printing top-aligned ruler and one dotted margin rectangle per realized clone page. They
reuse `WriterRulerGeometry`, current page settings, zoom and live paragraph indentation; ribbon Page commands and View
toggles update the projection without changing accepted Paper, preview, print or serialized content. The first live
capture exposed the ruler canvas vertically centred over the page because a fixed-height Grid child retained default
alignment. Pinning it to `VerticalAlignment.Top` corrected the real window while preserving the compositor's 24-DIP
viewport inset.

Focused production coverage now proves ruler/guide projection through zoom and landscape reflow, real picture and table
resize commits through the W3-E controllers, native Undo availability, active-drag cancellation on document replacement,
and stale/empty replacement behavior. A clean Ctrl+N live check initially terminated the process after it had switched
documents: an empty `FlowDocument` legitimately published no insertion rectangles, and the periodic caret overlay called
`MinBy` on that empty set. The overlay now uses an empty-safe lookup, and the replacement regression uses an actually
empty one-page document. The rerun stayed responsive at **document 2, generation 3, page 1/1**.

In the actual Release window, selecting the page-spanning table exposed Table Tools and its page-local overall handle;
dragging it published generation 3, and native Undo/Redo published generations 4 and 5. Selecting the picture exposed
Picture Tools and eight aligned handles; a bottom-right drag enlarged the source picture, published generation 6 and
left native Undo enabled. The corrected ruler stays directly below the ribbon, margin guides follow each realized page,
and the real Orientation -> Landscape command reflowed the six-page seed to eight pages at generation 2 while expanding
the ruler from the portrait width to the full landscape width. Warm captures measured about 2.8-10.5 ms and warm layouts
about 277-295 ms during these interactions. The window remained responsive.

The focused production gate passes **6/6**, the namespace-scoped pagination gate passes **30/30**, and the final Release
Writer build passes with **0 warnings / 0 errors**. No full suite or solution build was run. Default Paper and
`src/RibbonKit/**` remain unchanged. Genuine OS IME and production RTL remain jointly deferred, with no claim in this
batch. The next bounded opt-in slice should publish immutable table row/column boundary geometry and project the
remaining W3-E row/column handles, then add diagnostic-surface UIA semantics and zoom/DPI hit-target checks before any
default-Paper decision.

### 3.149 RibbonKit Writer W2-G immutable table boundaries and accessible resize projection — 2026-09-01

The third private production batch completes the planned table-interaction projection without changing the opt-in
boundary. The dedicated STA now publishes immutable page-local table fragments containing the authoritative object
identity, page number, fragment bounds, ordered column boundaries, row-group/row-index bottom boundaries and
first/last-fragment flags. Only values cross the dispatcher boundary. Column, row and overall interactions include the
same generation, document, mapped-page, object and handle identities used by the existing strict stale-event gate;
accepted events delegate to the live W3-E column/row/overall resize controller and its native one-unit Undo contract.

The page compositor renders column handles at each published grid boundary, row handles at each published row bottom,
and an overall handle only on the last fragment. Visual and pointer hit geometry share one descriptor source. Eight-DIP
visual handles and eighteen-DIP hit targets are converted through zoom and the current `DpiScale`, then pixel-aligned so
they remain approximately screen-sized instead of growing with the clone page. The surface is a UIA Pane; every
structured table/picture/hyperlink fragment is now a Button/Invoke target that runs the same current-generation
page-to-source activation path, and each resize handle is a named Button/Invoke target with a stable handle identity.
Invoking a handle applies one twelve-DIP diagnostic step through the authoritative W3-E controller and restores native
editor command ownership.

A focused regression exposed that `Table.ElementStart.GetCharacterRect` inside the hidden paged viewer is not a stable
table origin: right-aligning cell text moved the reported X coordinate from **104.5** to **145.44** DIPs even though the
grid did not move. The LTR diagnostic therefore derives explicit-column boundaries from the paginator-page content
origin plus finite table margin and `CellSpacing`, while logical row/span occupancy supplies row identities. The focused
test proves right-aligned cell content cannot move those published boundaries. This is recorded as RKWF-032; it is a
WPF page-geometry constraint, not a RibbonKit runtime defect. Auto-width and production RTL table origins remain outside
this batch.

In the actual Release Writer window at 125% DPI, UIA activation of the page-four table exposed **39** virtualized
column/row/overall handles with **39** page/object-qualified AutomationIds and left `Document editor` focused. Invoking
the first column moved its page-four boundary
from physical X **849** to **863**; reactivation after the resulting object-identity replacement exposed the new map.
Invoking row 15 moved its boundary from Y **427** to **444**. Those commits published generations three and four;
native QAT Undo and Redo published generations five and six. At 130% zoom the visible handles remained aligned at
about **10-11 physical pixels** on the 125% monitor. The real Orientation -> Landscape command published generation
seven, reflowed the seed from six to eight pages, and exposed **22** correctly mapped handles on the visible page-five
fragment. A later virtualized handoff published generation eight; UIA activation of the page-six picture exposed all
eight picture handles, and invoking its right handle published generation nine with the live editor focused. Warm
layouts during that final handoff/resize were about **198-208 ms** and the window remained responsive.

The focused production gate passes **11/11**; the combined production plus W3-E table-resize gate passes **19/19**; and
the namespace-scoped pagination gate passes **36/36**, including the native clipboard/history case after the transient
external clipboard lock cleared. The final Release Writer build passes with **0 warnings / 0 errors**. No full suite or
solution build was run. Default Paper, accepted preview/print/Continuous behavior and `src/RibbonKit/**` remain
unchanged. Genuine OS IME and production RTL remain jointly deferred with no claim. The next bounded opt-in slice should
cover multi-row-group and spanned-cell boundary matrices, explicit/Auto-width fallback policy, keyboard resize semantics
and longer-document cancellation/responsiveness before any default-Paper decision.

### 3.150 RibbonKit Writer W2-G structural table matrix and safe Auto-column fallback — 2026-09-01

The fourth private production batch closes the multi-row-group, `RowSpan` and `ColumnSpan` portion of the next slice
without changing the opt-in boundary. Immutable logical-cell capture now carries each cell's last occupied row, so a
row-spanning cell contributes its bottom to the row where the span ends rather than its starting row. Published row
identities retain both row-group and row indexes, and the surface exposes unambiguous group-qualified UIA names. This
preserves one authoritative live editor and sends only values across the dedicated-STA boundary.

The batch also makes the horizontal table-geometry contract explicit. When every `TableColumn.Width` is a finite,
positive absolute value, the paginator can publish trusted structural column boundaries and the existing column/overall
handles remain available. Auto, star, nonpositive or incomplete column definitions now publish
`HasTrustedColumnBoundaries = false` with no column boundaries. The compositor suppresses column and overall handles,
and the controller rejects those events before they can reach W3-E; trustworthy row handles remain available. This is
safer than reconstructing Auto widths from cell insertion rectangles, whose X positions depend on paragraph alignment
and cannot resolve spanned grid boundaries. RKWF-033 records the bounded unsupported policy as a WPF paginator geometry
constraint rather than a RibbonKit defect.

A private `--writer-pagination-structural-seed` switch, still gated by `--writer-paginated-diagnostic`, supplies an Auto-
width three-column table with two row groups plus row and column spans for actual-window verification. In the Release app
at 125% DPI, page one exposed exactly **5** unique group-qualified row handles and **0** column/overall handles. Invoking
group two, row one moved its handle from Y **642** to **659**, published generation two with a replacement table-object
identity, restored editor focus and left the window responsive. Native QAT Undo and Redo published generations three and
four. The five row handles remained structurally distinct; the table and its spanned cells stayed inside the page margin
guides. Initial capture/layout measured about **46/346 ms** and later warm capture/layout about **2-11/170-231 ms**.

The focused production gate passes **12/12**; the combined production plus W3-E table-resize gate passes **20/20**; and
the namespace-scoped pagination gate passes **37/37**. The final Release Writer build passes with **0 warnings / 0
errors**. No full suite or solution build was run. Default Paper, accepted preview/print/Continuous behavior and
`src/RibbonKit/**` remain unchanged. Genuine OS IME and production RTL remain jointly deferred with no claim. The next
bounded opt-in slice should cover keyboard resize semantics plus longer-document coalescing, active cancellation and
actual-window responsiveness before any default-Paper decision.

### 3.151 RibbonKit Writer W2-G keyboard resize, worker telemetry and live scalability boundary — 2026-09-01

The fifth private production batch deliberately combines three related slices without changing the opt-in boundary.
First, selected table and picture handles now support a transactional editor-owned keyboard mode. `Ctrl+Alt+R` enters
handle navigation, Tab/Shift+Tab chooses a handle, Enter begins the native W3-E transaction, arrows apply one-DIP steps,
Shift+arrow applies twelve-DIP steps, and Enter/Escape commits or cancels. The compositor draws the current keyboard
target, while the authoritative `RichTextBox` remains the keyboard and command target throughout. A direct-focus attempt
was rejected after the actual `Viewbox` overlay could not retain focus across overlay rebuilds. The live shortcut also
exposed WPF's Alt routing: `PreviewKeyDown` reports `Key.System`, so the production path must normalize `SystemKey` before
matching `R`. RKWF-034 records both findings.

Second, the persistent STA publishes thread-safe value telemetry for started, completed, cooperatively cancelled-active
and coalesced-pending jobs. The surface reports those counters beside capture/layout time. A focused 1,600-paragraph
production test waits until one reflow is active, queues a rapid page-setting burst, restores the original settings, and
proves at least one active cancellation plus pending replacement before accepting only the final generation. The test
also proves the final page settings, visible/adjacent window and current-generation invariants.

Third, private `--writer-pagination-stress-seed` and `--writer-pagination-stress-burst` switches exercise the same path
in the actual Release app. The accepted live corpus is intentionally **120** short paragraphs (**602 words / 5,174
characters**, three pages): generation 20 published after an eighteen-setting burst with **1** active cancellation,
**17** coalesced pending requests, **2/3** completed/started jobs, **0.1 ms** capture and **384.4 ms** final layout. A real
wheel handoff to page three published generation 21 in **286.1 ms**, mapped the last page and left the window responsive.
The first spelling probe remained underlined under the exact `qzxwvv` token.

The live keyboard matrix used the structural Auto-width table at 125% DPI. After UIA selected the table, the native
editor held focus while Down plus Shift+Down committed one row-resize transaction; the first row handle moved from
physical Y **1103** to **1128**. Ctrl+Z returned it to **1103**, Ctrl+Y restored **1128**, and a later Shift+Down session
followed by Escape left the handle at **1128** with all five handles present and `DocumentEditor` focused.

Long-document live acceptance is not closed. Clean isolated runs with **180** separate short paragraphs (**903 words /
7,761 characters**) still had not published after more than twenty seconds despite continuing to answer window messages.
Longer 240- and 480-block probes were worse; the 480-block run reached about **95 CPU seconds** and **1.05 GB** working
set before being stopped. Broad UI Automation traversal amplified one run, but the no-automation 180-block isolation
confirmed a real block-count/paginator cost. RKWF-035 records this as an open WPF/Writer performance investigation and
prevents the 120-block result from being presented as long-document acceptance.

The focused production gate passes **14/14**; the combined production plus W3-E table-resize gate passes **22/22**; and
the namespace-scoped pagination gate passes **39/39**. The final Release Writer build passes with **0 warnings / 0
errors**. No full suite or solution build was run. Default Paper, accepted preview/print/Continuous behavior and
`src/RibbonKit/**` remain unchanged. Genuine OS IME and production RTL remain jointly deferred. The next bounded slice
should instrument load, page-count, page-start, mapped-geometry and raster phases separately, then define a measured
long-document work budget/cancellation design before any default-Paper decision.

### 3.152 RibbonKit Writer W2-G staged publication and spelling-cliff correction — 2026-09-01

The sixth private production batch combines phase diagnosis, staged UI publication and one measured correction while
keeping default Paper unchanged. Immutable results now carry separate XamlPackage-load, formatting, page-count,
page-start, object-map, page-realization, insertion-geometry, raster and structured-geometry timings. The persistent STA
also exposes its current generation/phase as a thread-safe value snapshot, while the controller records generation-local
capture and end-to-end time. The existing status banner acts as the one non-modal loading surface; it reports the active
phase while the realized authoritative editor remains available. An optional private telemetry environment variable
mirrors those value messages to a caller-supplied file for actual-window diagnosis without traversing the editor UIA
tree. `--writer-pagination-stress-blocks=N` makes the private stress corpus reproducible.

The measurements rejected the earlier paginator-cliff diagnosis. In the live 120-block run, the dedicated STA completed
all layout phases and went idle in under one second, but final UI publication remained blocked. The compositor was
calling `RichTextBox.GetNextSpellingErrorPosition` synchronously from `RefreshOverlays`, including once inside page
rebuild and again on every 750-ms overlay tick. Searching the long correct-text gaps between the sparse `qzxwvv` probes
occupied the editor dispatcher even though page layout had finished. RKWF-036 records this Writer/WPF consumer seam.

The bounded correction keeps native spelling owned by the single live editor. For only the current visible/adjacent page
window, the compositor builds value word-start offsets, checks at most 64 candidates and eight milliseconds of completed
work per dispatcher turn with `GetSpellingErrorRange`, caches generation/document-stamped source ranges, and draws only
those cached ranges. Invalidation, document replacement and page-window changes clear the scan immediately. No second
spell-checking control, cross-thread `TextPointer`, fake page or modal dialog was added. A focused 180-block test proves
publication plus the exact source range and left/right underline bounds of `qzxwvv`.

In the actual Release app, 120 blocks published three pages in **852.8 ms** end-to-end (**801.2 ms** worker); the formerly
failing 180 blocks published five pages in **1005.1 ms** end-to-end (**911.6 ms** worker). An 18-setting burst over the
180-block corpus accepted only generation 20 after **1** active cancellation and **18** coalesced requests, publishing in
**797.3 ms** end-to-end with a **744.0 ms** final worker layout. The user confirmed the window was very fast, and the
process remained responsive. The focused production gate passes **15/15**, the namespace pagination gate passes
**39/39**, the W3-E table-resize regression passes **8/8**, and the Release Writer project builds with zero warnings and
errors. No full suite, solution build, genuine OS IME or production RTL gate was run. Larger real-world document mixes
still need a separate work-budget gate before any default-Paper decision; the next bounded slice should stage page-window
handoff prefetch/publication and define measured retention/eviction limits without changing native editor ownership.

### 3.153 RibbonKit Writer W2-G reusable layout session and bounded directional page cache — 2026-09-04

The seventh private production batch removes the full clone/paginator rebuild from scroll-only page-window requests.
The dedicated STA now owns one disposable layout session containing the immutable clone, paginator, computed page count,
page starts, structured-object map and realized page viewer. A separate layout identity changes for content, document,
formatting, page-setting or DPI invalidation; viewport generations change independently. Replacing a layout disposes its
hidden viewer and cached values on the worker STA before the next session is created. Scroll-only requests reuse the
session while retaining the existing generation/document/current-page rejection gates.

The session caches immutable PNG bytes plus insertion, structured-object and table geometry under an eight-page and
64-MB default budget. Each result publishes the retained-page inventory and cache telemetry. The compositor merges new
page frames, removes only worker-evicted pages and preserves other valid images instead of clearing the surface. Geometry
remains interactive only for the latest generation's current visible/adjacent window; retained stale frames and loading
placeholders reject pointer, UIA and resize routing. Visible work publishes first. A second, lower-priority request then
prefetches two pages in the current direction; a new visible request cancels active speculative work and wins the pending
slot. An uncached jump immediately inserts a white page-sized non-interactive loading placeholder under the existing
status banner.

The expanded production gate passes **19/19** and covers sequential forward/reverse scrolling, directional prefetch,
cache-hit revisits with zero package-load/page-count/page-start work, two-jump latest-only acceptance, eviction and
rerender, content/formatting/page-setting/DPI/document invalidation, and placeholder/stale-cache interaction rejection.
The namespace-scoped pagination gate passes **43/43**. The final Release Writer project builds with **0 warnings / 0
errors**. No full suite or solution build was run.

The actual opt-in Release probe used **600 short blocks / 15 pages**. Initial layout took **643.7 ms** worker time. Once
directional prefetch was warm, ordinary forward and reverse visible requests were cache hits with **0.1–0.2 ms** worker
time; measured UI arrival was **49.0–74.2 ms** forward and **24.1–147.3 ms** reverse. A cached revisit arrived in **43.1
ms** with no new pagination. Two uncached jumps showed both placeholders, abandoned the first request and accepted only
page 13 in **529.0 ms** (**442.3 ms** worker). Landscape reflow created a new session in **654.5 ms** (**540.3 ms**
worker), and restoring the original settings took **426.2 ms** (**340.9 ms** worker). The UI reached ApplicationIdle in
**37.6 ms** after the sequence. The cache stayed at its eight-page cap and reported about **1.3 MB** of encoded
PNG/geometry values, recording seven evictions. That first counter did not include the decoded WPF bitmap footprint and
is corrected by §3.154.

Scrolling/cache feasibility is a **qualified go**: ordinary bidirectional scrolling no longer reloads or repaginates
the document, cached revisits are immediate, and fast jumps retain page-shaped feedback without weakening stale-event
rejection. Process working set nevertheless rose from **366.5 MB** after initial publication to a **515.2 MB** high-water
mark and ended at **483.9 MB**, while managed memory remained near **30 MB** and the then-reported encoded/geometry
cache was only **1.3 MB**. That separation points to WPF/native page-realization high-water rather than an unbounded
Writer cache, but it is
not yet a long-run plateau proof. Default Paper remains unchanged; genuine OS IME and production RTL remain deferred.
The next bounded slice should repeat multi-cycle forward/reverse/jump/reflow runs over mixed tables/images and define a
process working-set plateau/reclamation budget before broader real-document or default-Paper work.

### 3.154 RibbonKit Writer W2-G decoded page-memory and mixed-content plateau gate — 2026-09-04

The eighth private production batch closes the accounting gap from §3.153 without changing the compositor architecture.
Each cached page now reports encoded PNG bytes, projected decoded BGRA pixels at the current DPI and geometry overhead;
the eight-page/64-MB limit is enforced against their combined footprint. Surface eviction detaches the page frame,
clears each image source and child collection, and removes the overlay before the page can be revisited. The diagnostic
can seed mixed paragraphs, tables and images and run multiple complete forward/reverse cycles while recording cache,
working set, private bytes, managed heap and session lifetime at every cycle boundary.

Two focused regressions cover byte-driven eviction/release and two complete mixed-content scroll cycles. They prove that
decoded bytes dominate encoded bytes, evicted surface frames are released, the retained-frame count matches the worker
inventory, table and picture geometry survive rerender, and one unchanged layout identity keeps exactly one undisposed
STA session. The expanded production gate passes **21/21**, the namespace-scoped pagination gate passes **45/45**, and
the final Release Writer project builds with **0 warnings / 0 errors**. No full suite or solution build was run.

The decisive opt-in Release probe used **600 mixed blocks / 18 pages** and six complete forward/reverse cycles. After
warm-up, cycles 2–6 delivered forward pages at a **25.6 ms median / 65.4 ms maximum** UI arrival and reverse pages at a
**26.1 ms median / 88.6 ms maximum**; median worker work in both directions was **0.1 ms** because the stable session
kept package load, pagination and page starts alive. A cached revisit took **41.8 ms**. Two uncached fast jumps displayed
both placeholders, abandoned page 18 and accepted only page 16 in **388.6 ms** (**349.8 ms** worker). Landscape reflow
created a new session in **470.6 ms** (**435.1 ms** worker), and restoring the original settings created another in
**321.2 ms** (**286.7 ms** worker). The final UI-idle probe took **0.2 ms**.

The corrected cache remained exactly **8 pages / 42.4 MB total**, including **41.1 MB decoded** and about **0.7 MB
encoded**, throughout the stable-layout cycles; the full run recorded 123 safe evictions. Process memory reached its
**696.5 MB working-set / 589.9 MB private-byte** high-water at cycle 2, then reclaimed to cycle-end working sets of
**660.4, 630.0, 593.6 and 602.6 MB** for cycles 3–6 while managed memory stayed **28.5–32.7 MB**. Reflow/restore replaced
two sessions deterministically and reduced working set to **537.1 MB** before final prefetch settled at **566.6 MB**.
The measured plateau rule is therefore satisfied: after warm-up, later cycle ends are non-monotonic, remain below the
two-cycle high-water, keep the app cache at its declared limit and do not grow the managed heap or session count.

Live scrolling/cache feasibility remains a **qualified go**, now with a bounded decoded-page budget and observed native
reclamation over repeated mixed-content traversal. This is an automated self-running actual Release-window diagnostic,
not manual visual acceptance or a default-Paper authorization. The next bounded slice should exercise a few saved
real-world `.rkw` documents and long paragraphs under the same counters, then add a deterministic low-memory/cache-budget
option; genuine OS IME and production RTL remain deferred together.

### 3.155 RibbonKit Writer W2-G saved-document and reduced-cache feasibility — 2026-09-05

The ninth private opt-in batch exposes deterministic cache limits through `--writer-pagination-cache-pages=1..8`
and `--writer-pagination-cache-mb=1..64`. Missing options retain the **eight-page/64-MB default**; invalid values fail
explicitly. These are retention targets with the existing protected visible/adjacent interaction-window floor, not a
process-memory limit. A 1-page/1-MB regression deliberately exceeds both targets to preserve current-page interaction,
retains only protected pages and evicts everything else. Temporary worker rasterization and WPF/native allocations are
outside the retained-value counter. Probe publications report `budget-exceeded` and released-frame counts; the on-window
denominator follows the configured page limit.

`--writer-pagination-document=<absolute .rkw path>` with `--writer-pagination-scroll-probe` loads through the normal
bounded native persistence/session replacement path without updating recent files, then detaches the source path.
Four existing Documents files were read and their SHA-256 hashes checked unchanged: `Test2.rkw` (formatted text),
`test3.rkw` and `test4.rkw` (pictures and formatting), and `Untitled.rkw` (eight-column table, picture and hyperlink;
about 5.7 MB on disk). All four are one-page documents under their saved settings; repeated no-op page requests are
**not** eviction or scrolling evidence. Landscape reflow expands the table/picture document to two pages. This small
personal corpus is not general large-document coverage. The synthetic long-paragraph seed uses four paragraphs of 400
sentences and yields 26 pages; the existing 600-block mixed seed supplies the table/image eviction corpus.

A 50-ms Background dispatcher heartbeat measures dispatch gaps throughout each self-running Release-window probe.
End-of-probe disposal, explicitly labelled forced collection and weak decoded-image references distinguish collectibility
from ordinary GC cadence and process working-set observations. A zero retained cache or collected BitmapImage does not
mean the entire process has returned to startup memory. Probe exceptions set a failure exit code; prefetch timeout and
superseded fast-jump acceptance fail explicitly.

The four saved-file probes all exited normally at **3 pages/24 MB**, retaining **5.3–5.9 MB** at the original page
settings and collecting **1/1** decoded page images after session disposal. Their maximum dispatcher gaps were
**429.5–568.2 ms**, including load/reflow. The image/table file's working set fell **608.5 → 511.4 MB** and private bytes
**512.0 → 413.4 MB** after explicitly forced release/collection; these are not natural-GC plateau measurements.

Both synthetic runs completed **six full forward/reverse cycles** at **3 pages/24 MB**, with no published budget overrun,
one undisposed session during stable traversal, two-placeholder latest-only jumps, and two reflow/restore session
replacements. The long-paragraph run retained **16.6 MB total / 15.4 MB decoded**, recorded **816 evictions**, and delivered
cycles 2–6 at **539.7/520.3 ms median** forward/reverse arrival (**868.7/1016.8 ms maximum**). Median worker work was
**470.9/453.4 ms**. A revisit needed one missing page (**465.2 ms**); the latest fast jump took **1297.7 ms**. Its
**343.8-ms** maximum dispatcher gap and **0.2-ms** final UI-idle arrival demonstrate continued dispatch, not smooth
interactive scrolling. Dense insertion geometry and speculative pages evicted immediately by the protected three-page
window account for substantial repeated work.

Long-paragraph cycle-end working sets were **555.8, 654.9, 710.8, 692.0, 708.6, 684.6 MB**; managed cycle-end memory was
**37.6–52.9 MB**. Natural reclamation occurred, but later cycles exceeded the two-cycle high-water: this corpus does
**not** pass §3.154's earlier plateau rule. The full probe peaked at **730.4 MB working set / 629.1 MB private bytes**.
End-of-probe disposal plus forced collection reclaimed **721.3 → 680.3 MB** working set and **618.3 → 577.0 MB** private
bytes, with **3/3** decoded images collected. A smaller retained cache does not impose a smaller process-memory ceiling.

The 600-mixed-block/18-page comparison retained **15.9 MB total / 15.4 MB decoded**, recorded **528 evictions**, and
arrived at **156.3/155.8 ms median** forward/reverse (**183.5/186.8 ms maximum**) after warm-up. Its revisit took
**162.3 ms**, latest jump **462.2 ms**, maximum dispatcher gap **135.6 ms**, and final idle **0.2 ms**. Cycle-end working
sets **516.8, 504.3, 514.5, 506.8, 507.8, 513.4 MB** and managed heaps **24.8–25.3 MB** stayed stable. Final forced release
collected **3/3** images and reduced working set **526.9 → 500.4 MB**, private bytes **416.2 → 390.9 MB**. Compared with
§3.154's default-cache historical run, retention is lower but page arrivals are slower; this is not a controlled
same-run 8/64 versus 3/24 benchmark.

The final focused production gate passes **26/26**. The namespace gate passed **50/50** before the final weak-reference
assertion and telemetry refinements; the affected production tests were then rerun. Coverage includes default/invalid option
handling, reduced-budget latest-only placeholders and stale-event rejection, content/format/settings/DPI/document
invalidation, saved-package long-paragraph revisits, the deliberately undersized protected-window floor, and weak
references proving evicted page-zero decoded images are collectible while the editor/session remain alive. An initial
all-prefetched-images collection assertion was corrected: a boundary request protects only two pages, so one prefetched
image can legitimately remain retained. The final Release Writer build has **0 warnings / 0 errors**. No full suite,
solution build, manual visual acceptance, genuine OS IME or production RTL gate was run.

Evidence is local under `artifacts/w2g-low-memory/` (per-corpus logs, `summary.json`, source hashes and runner).
The six-corpus measurements precede the final telemetry-only denominator/session-count correction and stricter probe
failure guards. The first final smoke retained the disposed controller to read its counters and observed 0/3 and 0/1
images collected: the observer itself kept worker/dispatcher objects reachable. A non-inlined synchronous disposal helper
now returns only value counters, removing that async-state-machine root before collection. Reachability failure now
fails the probe. The verified final mixed/saved smokes exited 0, collected **3/3 and 1/1** images, and reported all
**3/3 and 5/5** sessions disposed respectively, checking those refinements. Reproduce using `--writer-paginated-diagnostic`, a document
or seed flag, `--writer-pagination-cache-pages=3 --writer-pagination-cache-mb=24 --writer-pagination-scroll-probe
--writer-pagination-scroll-cycles=6 --writer-pagination-exit-after-probe`, with `RIBBONKIT_WRITER_PAGINATION_TELEMETRY`
pointing to a fresh log. The mixed seed also uses `--writer-pagination-stress-blocks=600`.

**Decision: qualified go for further opt-in correctness/retention hardening; no broader long-document performance or
hard process-memory guarantee.** Saved inputs, eviction/revisit correctness and decoded collectibility are positive;
low-budget speculative churn and long-paragraph geometry/native high-water remain measured limitations.

The next bounded slice is **budget-aware speculative admission and long-paragraph page-geometry cost**: avoid rendering
prefetch pages that cannot survive the protected window, then compare 3/24, 4/24 and default 8/64 on the same
long-paragraph/mixed corpora. Preserve paginator parity, non-interactive placeholders, latest-only acceptance and the one
authoritative editor. Stop after a measured retention/arrival tradeoff; genuine OS IME and production RTL remain deferred
together, and default Paper remains unchanged.
