# RibbonKit Writer — Consumer Friction Log

This index separates open investigations from corrected behavior. Titles retain the
original symptom and stable RKWF identifier; they do not claim that symptom remains.
Summaries reflect recorded evidence through 2026-09-05, not new acceptance. Full
reproductions, measurements and historical gates are linked per entry.

## 1. Purpose and promotion

Record surprising control glue, timing, automation gaps and testing exceptions.
Distinguish app responsibility, a runtime candidate, authorized correction and closure.
An entry does not authorize changes under `src/RibbonKit/**`: first reproduce the
issue, identify the library responsibility and smallest compatible correction, and
obtain explicit authorization unless already supplied. Follow `AGENTS.md` and
`CONTRIBUTING.md` for implementation/verification; do not create another approval ritual.

## 2. Active follow-ups

- Runtime/documentation candidates: RKWF-001/002/026.
- External UIA investigations: RKWF-003/004/006.
- Narrow live follow-ups after implemented fixes: RKWF-016/019; W4-C physical driver
  and cold-start checks remain in RKWF-007/009/010.
- Opt-in pagination performance: RKWF-037/038, with RKWF-035 explained by RKWF-036.
- RKWF-008 is a bounded harness allowance, not a product defect.

All other entries below retain corrections, constraints or accepted workarounds.
No complete Windows contrast-theme, genuine OS IME or production pagination RTL
acceptance follows from these isolated fixes. Overall progress stays in
[design notes §5](../04-DESIGN-NOTES.md#5-current-state--next-steps).

## 3. New-entry format

Use the next RKWF ID with first-seen date/packet, classification, minimal reproduction,
current containment/correction, evidence and remaining gate. Keep the summary here;
put long diagnostic history in the evidence file. Preserve prior results with dates
instead of repeatedly appending them to the current-status summary.

## 4. Observations by ID

### RKWF-001 — KeyTip leaf activation has no consumer focus-handoff contract

Open focus-handoff candidate. Writer defers post-KeyTip focus to ContextIdle to avoid leaking terminating key input. A minimal cross-control/QAT/editable-input reproduction is still needed.

[Recorded evidence](history/writer-friction-evidence.md#rkwf-001--keytip-leaf-activation-has-no-consumer-focus-handoff-contract).

### RKWF-002 — Editable RibbonComboBox has no single semantic commit boundary

Open commit-semantics candidate. Writer commits editable combos on dropdown close, Enter or focus loss, not intermediate selection. Typing, refresh, cancellation and IME need a reusable minimal proof.

[Recorded evidence](history/writer-friction-evidence.md#rkwf-002--editable-ribboncombobox-has-no-single-semantic-commit-boundary).

### RKWF-003 — QAT external UIA action semantics need an isolated reproduction

Open external-UIA investigation, not a confirmed library defect. Compare hand-authored, source-linked and overflow QAT Invoke/Toggle/name/enabled behavior with a second client.

[Recorded evidence](history/writer-friction-evidence.md#rkwf-003--qat-external-uia-action-semantics-need-an-isolated-reproduction).

### RKWF-004 — Backstage custom page actions are absent from external UIA traversal

Open external-UIA investigation. Custom Backstage page content needs external-client comparison across Modern/Classic2010/Classic2007; keyboard focus is not traversal proof.

[Recorded evidence](history/writer-friction-evidence.md#rkwf-004--backstage-custom-page-actions-are-absent-from-external-uia-traversal).

### RKWF-005 — Ribbon groups have no first-class in-group separator

Corrected and accepted. RibbonGroupSeparator now supplies adaptive in-group chrome; theme/variant, collapsed, RTL and 100–200% DPI evidence is recorded. Whole-ribbon contrast themes remain separate.

[Recorded evidence](history/writer-friction-evidence.md#rkwf-005--ribbon-groups-have-no-first-class-in-group-separator).

### RKWF-006 — Main-ribbon leaf commands are absent from external UIA traversal

Open main-ribbon leaf traversal investigation; runtime scope is not approved. Compare expanded/collapsed/minimized Writer and Showcase with an independent UIA client.

[Recorded evidence](history/writer-friction-evidence.md#rkwf-006--main-ribbon-leaf-commands-are-absent-from-external-uia-traversal).

### RKWF-007 — Windows WPF print picker cannot host Writer's existing fixed preview

App-resolved with WriterPrintSetupDialog and isolated preview/print submission. W4-C still needs a non-PDF physical driver with unusual queue/ticket/imageable-area constraints.

[Recorded evidence](history/writer-friction-evidence.md#rkwf-007--windows-wpf-print-picker-cannot-host-writers-existing-fixed-preview).

### RKWF-008 — The single real-window STA integration case needs a parallel-load timeout allowance

Retained harness exception: only the combined real-window case uses a 20-second allowance; the default is 10 seconds. Monitor real duration rather than broadening timeouts.

[Recorded evidence](history/writer-friction-evidence.md#rkwf-008--the-single-real-window-sta-integration-case-needs-a-parallel-load-timeout-allowance).

### RKWF-009 — Initial paper margins need a post-Loaded invariant

App-resolved by idempotent Loaded-time presentation/margin restoration. Cold-start checks across W4-C DPI scales remain relevant.

[Recorded evidence](history/writer-friction-evidence.md#rkwf-009--initial-paper-margins-need-a-post-loaded-invariant).

### RKWF-010 — Initial editor focus needs a post-render handoff

App-resolved by guarded first-render/input-focus handoff and insertion-state refresh. Respect intended Backstage/Preview focus; cold-start Latin/IME/RTL remains a live gate.

[Recorded evidence](history/writer-friction-evidence.md#rkwf-010--initial-editor-focus-needs-a-post-render-handoff).

### RKWF-011 — Backstage has no host-level close-completed callback

Corrected and live-accepted. Ribbon.BackstageClosed fires after teardown; Writer owns busy/dialog/focus policy. Reopen cancellation, reduced motion and File-surface distinctions have focused coverage.

[Recorded evidence](history/writer-friction-evidence.md#rkwf-011--backstage-has-no-host-level-close-completed-callback).

### RKWF-012 — Realized native-undo tests must serialize visible WPF window ownership

Resolved in the Writer UI test collection. Serialize tests that show/focus real windows to preserve native Undo evidence; do not serialize unrelated pure tests.

[Recorded evidence](history/writer-friction-evidence.md#rkwf-012--realized-native-undo-tests-must-serialize-visible-wpf-window-ownership).

### RKWF-013 — InRibbonGallery popup background can remain unresolved in its popup HWND

Corrected and accepted. Resolve popup background from the connected gallery with system Window fallback; tokenless/scoped-theme tests and theme/DPI live evidence replace the Writer override.

[Recorded evidence](history/writer-friction-evidence.md#rkwf-013--inribbongallery-popup-background-can-remain-unresolved-in-its-popup-hwnd).

### RKWF-014 — Transient Writer selection can collapse the active contextual tab during a table command

App-corrected and accepted. Capture/revalidate structured targets and defer intermediate selection publication until mutation/caret recovery; reject stale context after real deletion/replacement.

[Recorded evidence](history/writer-friction-evidence.md#rkwf-014--transient-writer-selection-can-collapse-the-active-contextual-tab-during-a-table-command).

### RKWF-015 — Native Undo can restore a loaded image container without its image child

App-corrected. Bounded inert image snapshots repair empty Undo placeholders and preserve older native history; redo runs only after native units. Normalize isolated persistence/preview clones, never the live history.

[Recorded evidence](history/writer-friction-evidence.md#rkwf-015--native-undo-can-restore-a-loaded-image-container-without-its-image-child).

### RKWF-016 — Revealing a contextual tab can leave the active-tab marker at its previous coordinate

Library-corrected with coalesced post-layout underline/notch refresh. Theme/variant and DPI visibility toggles are accepted; RTL, reduced motion, merging and customization reorder remain recorded follow-ups.

[Recorded evidence](history/writer-friction-evidence.md#rkwf-016--revealing-a-contextual-tab-can-leave-the-active-tab-marker-at-its-previous-coordinate).

### RKWF-017 — Empty trailing table cells can briefly lack usable text geometry after replacement

App-corrected. Derive table grid counts from structure, then use realized geometry or bounded explicit-width fallback, including CellSpacing per column.

[Recorded evidence](history/writer-friction-evidence.md#rkwf-017--empty-trailing-table-cells-can-briefly-lack-usable-text-geometry-after-replacement).

### RKWF-018 — Live DPI transition can stale InRibbonGallery scrolling and side-button hit geometry

Library-corrected and live-accepted. Permanent strip/popup scrollers move only the items presenter. Natural-width placement, side-button separation and first post-DPI open/close redraw passed.

[Recorded evidence](history/writer-friction-evidence.md#rkwf-018--live-dpi-transition-can-stale-inribbongallery-scrolling-and-side-button-hit-geometry).

### RKWF-019 — Gallery overflow falls back to OS-native scrollbar chrome

Core gallery scrollbar geometry and Customize Ribbon pilot are accepted. QAT list styling, final 2013/2019 square-token and modern ordinary-button comparisons retain the narrower pending live checks; do not infer their closure from RKWF-027/029.

[Recorded evidence](history/writer-friction-evidence.md#rkwf-019--gallery-overflow-falls-back-to-os-native-scrollbar-chrome).

### RKWF-020 — FlowDocument table selection endpoints can resolve to an adjacent cell

App-corrected TextPointer affinity. Normalize selection endpoints so a boundary at the next cell does not expand the selected table range incorrectly.

[Recorded evidence](history/writer-friction-evidence.md#rkwf-020--flowdocument-table-selection-endpoints-can-resolve-to-an-adjacent-cell).

### RKWF-021 — Table insertion rectangles move with cell text alignment

App-corrected table origin: text alignment is not the table border. Use structural/element origin, spacing and widths for the adorner perimeter.

[Recorded evidence](history/writer-friction-evidence.md#rkwf-021--table-insertion-rectangles-move-with-cell-text-alignment).

### RKWF-022 — Locally setting table placement can materialize inherited Auto margins

App-corrected inherited Auto margins. Preserve finite vertical margins and materialize inherited non-finite defaults safely when committing placement.

[Recorded evidence](history/writer-friction-evidence.md#rkwf-022--locally-setting-table-placement-can-materialize-inherited-auto-margins).

### RKWF-023 — Adorner-local table geometry must not reapply the editor zoom transform

App-corrected coordinate ownership. Table adorner geometry/deltas are already local; applying the editor zoom again double-projects them.

[Recorded evidence](history/writer-friction-evidence.md#rkwf-023--adorner-local-table-geometry-must-not-reapply-the-editor-zoom-transform).

### RKWF-024 — WPF tables have no semantic horizontal placement across changing widths

App-corrected and accepted semantic horizontal table placement. Preserve the margin-ratio placement across Paper/Continuous widths, persistence and native history without dirtying view-only projection.

[Recorded evidence](history/writer-friction-evidence.md#rkwf-024--wpf-tables-have-no-semantic-horizontal-placement-across-changing-widths).

### RKWF-025 — Re-serializing fixed XPS bitmap resources can terminate virtual printing

App-corrected and accepted pictured printing. Keep stable fixed preview but print through the isolated flow path to avoid re-serializing fixed XPS bitmap resources.

[Recorded evidence](history/writer-friction-evidence.md#rkwf-025--re-serializing-fixed-xps-bitmap-resources-can-terminate-virtual-printing).

### RKWF-026 — Ribbon has no host-level Office Orb glyph override

App-owned Orb-glyph workaround; runtime direction is not approved. The realized custom template may justify a future general host hook, not a Writer-specific public API.

[Recorded evidence](history/writer-friction-evidence.md#rkwf-026--ribbon-has-no-host-level-office-orb-glyph-override).

### RKWF-027 — Options dialog content scroller did not consume RibbonKit's native ScrollBar style

Closed shared-container correction, accepted with W4-A. Register the exact native ScrollBar style on the realized Options content viewport; preserve inner page scrollers.

[Recorded evidence](history/writer-friction-evidence.md#rkwf-027--options-dialog-content-scroller-did-not-consume-ribbonkits-native-scrollbar-style).

### RKWF-028 — Manually drawn consumer chrome needs explicit invalidation after appearance changes

Closed app integration. Manually drawn ruler/chrome must invalidate after appearance preview or rollback; geometry remains unchanged.

[Recorded evidence](history/writer-friction-evidence.md#rkwf-028--manually-drawn-consumer-chrome-needs-explicit-invalidation-after-appearance-changes).

### RKWF-029 — RibbonComboBox popup viewport retains the native OS scrollbar

Closed shared-template correction, live-accepted in Writer. Register the native style on the realized RibbonComboBox popup viewport rather than patching only Writer font controls.

[Recorded evidence](history/writer-friction-evidence.md#rkwf-029--ribboncombobox-popup-viewport-retains-the-native-os-scrollbar).

### RKWF-030 — FlowDocumentPageViewer geometry is fitted-view local, not paginator-page local

Closed app pagination geometry correction. FlowDocumentPageViewer values are fitted-view-local; normalize to paginator-page coordinates before publishing immutable interaction values.

[Recorded evidence](history/writer-friction-evidence.md#rkwf-030--flowdocumentpageviewer-geometry-is-fitted-view-local-not-paginator-page-local).

### RKWF-031 — An empty FlowDocument can publish a page without insertion geometry

Closed app lifecycle edge. Empty documents may have a page without insertion geometry; publish a safe empty state without nullable caret failure.

[Recorded evidence](history/writer-friction-evidence.md#rkwf-031--an-empty-flowdocument-can-publish-a-page-without-insertion-geometry).

### RKWF-032 — Hidden paged table origins can inherit cell-text alignment

Closed within explicit-width LTR diagnostics. Use page content origin plus finite table margin/spacing rather than text-aligned hidden character rectangles. Production RTL is not implied.

[Recorded evidence](history/writer-friction-evidence.md#rkwf-032--hidden-paged-table-origins-can-inherit-cell-text-alignment).

### RKWF-033 — Hidden paginator has no stable public Auto-column grid boundaries

Closed by bounded unsupported policy. Publish horizontal boundaries only for finite positive explicit columns; hide Auto/star column/overall resize instead of inventing geometry.

[Recorded evidence](history/writer-friction-evidence.md#rkwf-033--hidden-paginator-has-no-stable-public-auto-column-grid-boundaries).

### RKWF-034 — Virtual page handles cannot be a durable keyboard-focus owner

Closed by editor-owned keyboard handle mode. Keep the editor focused, use Ctrl+Alt+R navigation and generation-valid targets; virtual page handles cannot own durable focus.

[Recorded evidence](history/writer-friction-evidence.md#rkwf-034--virtual-page-handles-cannot-be-a-durable-keyboard-focus-owner).

### RKWF-035 — Live pagination appeared to have a block-count cliff

The 120/180-block cliff was traced and contained by RKWF-036. Broader real-document latency/memory remains open under RKWF-037/038; default Paper is unchanged.

[Recorded evidence](history/writer-friction-evidence.md#rkwf-035--live-pagination-appeared-to-have-a-block-count-cliff).

### RKWF-036 — Forward spelling-error enumeration can block staged page publication

Closed for the opt-in compositor. Bounded visible-page spelling candidates replace unbounded forward enumeration; generation-stamped slices preserve exact-word overlays.

[Recorded evidence](history/writer-friction-evidence.md#rkwf-036--forward-spelling-error-enumeration-can-block-staged-page-publication).

### RKWF-037 — Reusable WPF page realization has a larger process high-water than the app cache

Mixed-content retention/reclamation has measured positive evidence; long-paragraph native high-water remains open. App cache bytes, process working set and natural versus forced collection are distinct metrics.

[Recorded evidence](history/writer-friction-evidence.md#rkwf-037--reusable-wpf-page-realization-has-a-larger-process-high-water-than-the-app-cache).

### RKWF-038 — A reduced cache protects interaction but can discard every speculative page

Speculative churn is now guarded by app-owned admission before realization, using the
existing eviction policy and largest observed cached footprint. Protected pages remain
mandatory; unusual content can exceed the estimate. Dense insertion profiling and exact-map
traversal work are recorded under RKWF-039. The broader latency/native-memory tradeoff
remains open; no hard working-set cap or default-Paper approval.

[Admission implementation and comparison](history/03-writer-pagination.md#3156-ribbonkit-writer-w2-g-speculative-admission-and-page-cost-timing--2026-09-09).

[Recorded evidence](history/writer-friction-evidence.md#rkwf-038--a-reduced-cache-protects-interaction-but-can-discard-every-speculative-page).

### RKWF-039 — Dense insertion mapping repeats native caret-boundary work

App-owned profiling isolates rectangle queries and traversal as the dominant costs.
The opt-in worker uses a local text-context fast path while retaining WPF caret-boundary
validation and native fallback at complex/markup edges. Exhaustive synthetic parity passes;
timing gains are modest/mixed and do not close long-document authoring or native-memory gates.
Repeated `GetTextRunLength` is unsuitable as a per-position guard because split text nodes
can make it rescan the run. The bounded parity test uses explicit forward page affinity
and a 60-second helper limit; its initial repeated-complex-script timeout is not acceptance.

[Implementation, profiling and verification](history/03-writer-pagination.md#3157-ribbonkit-writer-w2-g-insertion-traversal-profiling-and-exact-map-parity--2026-09-09).

### RKWF-040 — Native caret navigation did not move the paginated viewport

Corrected in the opt-in compositor. A focused reproduction moved the live editor to document
end while the paginated view remained on page 1 of 12. Selection changes now request the
caret's page using the existing layout session and reveal its rectangle at the current zoom.
Edits defer that request until current geometry publishes. Ordinary scrolling cancels pending
caret following; surface selection/resize interactions retain their existing ownership.

[Implementation and evidence](history/03-writer-pagination.md#3158-ribbonkit-writer-w2-g-native-caret-page-following--2026-09-09).

### RKWF-041 — Default activation needs the native ruler and context menu

The user authorized default paginated Paper on 2026-09-09. The diagnostic hid the accepted
interactive ruler with its native editor and did not attach the editor context menu to page
visuals. Default activation now retains that same ruler above the compositor, supplies its
visible page origin, and routes mapped right-click targets to native selection/menu actions.
Right-clicks within a selection preserve it; hidden pagination does not consume resize keys.
Detailed telemetry is opt-in; normal loading/failure text is user-facing.

Default-view appearance follow-up: the compositor's hard-coded `#FFE5E8EB` workspace
masked the active Mica/Acrylic material. Its background now binds to the existing
`DocumentPresentationHost.Background`, following successful backdrop activation,
light/dark opaque fallback and rollback without changing page images or document color.
A realized-window regression reproduced the mismatch and passes after the binding fix.

The combined production/window test run hit dispatcher-affine WindowChrome theme caching.
Fresh-process checks avoid that harness contamination without changing the runtime. The broad
window contract also expects the pre-Settings menu and fails identically with pagination
disabled; it was not rewritten as part of this change. The inspected render is hosted WPF
page/ruler evidence, not full application theme, physical input or OS IME acceptance.

[Default activation and verification](history/03-writer-pagination.md#3159-ribbonkit-writer-default-paginated-paper--2026-09-09).
