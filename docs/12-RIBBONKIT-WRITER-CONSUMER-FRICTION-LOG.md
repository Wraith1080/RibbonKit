# RibbonKit Writer — Consumer Friction Log

This index separates open investigations from corrected behavior. Titles retain the
original symptom and stable RKWF identifier; they do not claim that symptom remains.
Index reviewed on 2026-09-15. Statuses reflect the linked recorded evidence, not new
live acceptance. Full reproductions, measurements and historical gates are linked per entry.

## 1. Purpose and promotion

Record surprising control glue, timing, automation gaps and testing exceptions.
Distinguish app responsibility, a runtime candidate, authorized correction and closure.
An entry does not authorize changes under `src/RibbonKit/**`: first reproduce the
issue, identify the library responsibility and smallest compatible correction, and
obtain explicit authorization unless already supplied. Follow `AGENTS.md` and
`CONTRIBUTING.md` for implementation/verification; do not create another approval ritual.

## 2. Active follow-ups

### RibbonKit control improvement candidates

| Priority | Entries | Benefit and next bounded step |
| --- | --- | --- |
| 1 | RKWF-002 | Make editable ComboBox commits predictable. Reproduce typing, selection, Enter, Escape and focus loss; document a reusable pattern before deciding whether an optional commit event is needed. Preserve native behavior and IME composition. |
| 2 | RKWF-001 | Return focus safely after KeyTip activation without leaking the final key into the editor. Reproduce across buttons, toggles, menus and QAT before choosing documentation or a general completion hook. |
| 3 | RKWF-003/004/006 | Improve external UI Automation discovery and actions for QAT, Backstage and ribbon commands. Start with one missing main-ribbon leaf and a second independent client; these remain investigations, not confirmed library defects. |
| 4 | RKWF-026 | Let hosts supply an Office 2007 Orb glyph without modifying the realized visual tree. Evaluate an optional content-template/image hook shared by the main Orb and Classic2007 proxy. |

These candidates benefit ribbon consumers generally. This review does not implement
or approve new runtime APIs; first establish the focused reproduction and scope.

### Existing fixes needing narrower acceptance

- Narrow live follow-ups after implemented fixes: RKWF-016/019; W4-C physical driver
  and cold-start checks remain in RKWF-007/009/010.
- RKWF-008 is a bounded harness allowance, not a product defect.

All other entries below retain corrections, constraints or accepted workarounds.
No complete Windows contrast-theme, genuine OS IME or production RTL
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

### RKWF-044 — Expanding Paper outgrew its fixed-height dotted margin guide

The native sheet grew with content, but the guide used one physical page's content height.
Guide drawing and invalidation now use the measured paper interior minus scaled margins.
Growth, scrolling and Undo/Redo retain the dotted bottom and sides without changing content
or printed page breaks. The focused regression covers 75/100/150% zoom.

[Verification](history/02-writer-foundation.md#3162-ribbonkit-writer-expanding-paper-default-and-growing-margin-guide--2026-09-15).

### Verification note — 2026-09-15 cleanup

77 focused Writer tests passed. The combined window run hit the known WPF WindowChrome
cross-thread cache issue; the affected table check passed in a fresh process. No new live UI
or full-suite acceptance is claimed.
