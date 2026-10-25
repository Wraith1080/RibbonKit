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
| 3 | RKWF-003/004/006 | Shared main-ribbon, QAT and Backstage page-content discovery/action defects have bounded corrections and independent-client evidence. Complete actual-consumer/presentation acceptance in the 1.1 plan. |

These candidates benefit ribbon consumers generally. This review does not implement
or approve new runtime APIs; first establish the focused reproduction and scope.

### Existing fixes needing narrower acceptance

- Narrow live follow-ups after implemented fixes: RKWF-016/019; W4-C physical driver
  and cold-start checks remain in RKWF-007/009/010.
- RKWF-008 is a bounded harness allowance, not a product defect.
- RKWF-026 now uses the available shared glyph hook; only Writer's migrated W live appearance acceptance remains.
- RKWF-049 has a shared popup-completion correction with native diagnostic evidence;
  visual acceptance in the actual Writer process remains open.

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

### RKWF-065 — Clicking the unchanged caret discards pending font choices

- First seen: 2026-10-24, W8 formatting review. The user confirmed the reset occurs at the same caret position or in an empty document, rather than only after moving to differently formatted text. The other contextual-tab and Undo/Redo checks worked.
- Classification: Writer-owned native-editor adaptation, following RKWF-055. WPF's mouse-placement path clears pending typing properties. Six inline/Font-dialog/Clear Formatting reproductions lose their chosen values after that reset, while the different-position/intervening-edit guard case passes.
- Correction: remember explicit caret formatting and retain effective inline properties across a plain left click only while the document and insertion point stay unchanged. Skip already-equal property writes; discard retention after caret/selection movement, edits/replacement, availability changes or disposal. Preserve native selection, word formatting and Undo; no shared runtime, settings or public API change.
- Focused caret/adapter/copied-font checks pass, including no extra same-word Undo step. Tests simulate the native reset without moving the desktop pointer; the separate human click/type review is recorded in the [acceptance plan](20-V1.1-ACCEPTANCE-AND-RELEASE-PLAN.md#writer-same-caret-mouse-formatting--2026-10-24).

### RKWF-064 — Find matches lose their visible highlight while the dialog owns focus

- First seen: 2026-10-24, W7 Find/Replace review. The user reports the other checks work, but the main window stays inactive after a search and requests focus there to show the match.
- Classification: Writer-owned modeless-dialog completion. The search service selects the correct native TextRange; Find Next never activates or focuses its editor, whose inactive selection uses WPF's existing hidden-highlight behavior.
- Correction: after a successful Find Next, activate the editor's window and focus the editor. Keep the dialog open and its query unchanged. Empty/missing searches retain dialog focus. Replacement and general popup selection-display behavior are unchanged; no RibbonKit runtime change.
- Focused dialog regressions reproduce the inactive owner before correction and pass afterward alongside the service regressions. Automated focus and final visual review remain distinct; see the [current evidence and pending recheck](20-V1.1-ACCEPTANCE-AND-RELEASE-PLAN.md#writer-search-result-focus--2026-10-24).

### RKWF-063 — Touch table picker collapses to one cell and wraps its highlight

- First seen: 2026-10-24, W7 Table/Picture review; screenshots `codex-clipboard-a26baf13-442c-4dad-a948-f741b372acc4.png` and `codex-clipboard-7c9a98fe-f72f-4ddc-a607-e5b1d5ab1318.png`. The user reports the other checks look good.
- Classification: Writer-owned table-picker layout adaptation. Source inspection shows the 24 choices retain three logical rows of eight columns, but use a general-purpose wrapping gallery with a wide Custom Table footer. The Touch strip's equal-cell measurement includes that footer, resulting in a single visible tile. Expanded Touch items have a 44-DIP minimum while the gallery retains its 278-DIP width; six visual columns wrap the eight-column logical selection into a zigzag.
- Correction: after initially deferring the change, the user authorized implementation on 2026-10-24. Writer now uses the public ItemsPanel extension with an eight-column WPF Grid and a separate full-width footer row. Touch-sized cells fit a wider popup; the ribbon retains one visible row. The footer reuses the shared themed Touch action style. Existing preview/selection/insertion handlers, keyboard mapping, item identities and QAT/custom/Simplified borrowing stay intact. No runtime source or API change.
- Focused layout/copy and insertion checks pass; final enabled-state renders are inspected separately from pointer acceptance. Geometry accounts for fractional column rounding. See the [implementation pitfalls](history/03-writer-host-integration.md#2026-10-24--semantic-table-picker-layout) and [current evidence/pending Writer recheck](20-V1.1-ACCEPTANCE-AND-RELEASE-PLAN.md#writer-table-picker-layout--2026-10-24).

### RKWF-062 — Height resizing shifts the ribbon and document

- First seen: 2026-10-24, W6 mouse review and `20261023-2331-03.2494863.mp4`. The user identifies jumping/flicker, rather than slow dragging, and observes it without a debugger. Exact short-transition interruption timing cannot be established by the manual mouse attempt.
- Shared reproduction: a minimal RibbonWindow at native 125% alternates its Compact caption between 42 and 43 pixels during height-only changes. It does not require Writer, capture, a popup or a ribbon transition. This is distinct from the old debugger-only resize slowdown.
- Caption correction: move the existing 34-DIP minimum from the window grid's Auto row to TitleBarBand, so its rounded desired height is stable before remaining space is divided.
- The user clarified that the larger symptom is the whole window image moving in jumps, supplied a stronger Showcase recording, authorized minimizing it and preferred the cached diagnostic variant. RibbonWindow now temporarily caches ribbon and detached title-bar QAT paint during the native move/resize loop, preserving live controls and authored CacheMode values. It releases the cache on exit/cancel and lifecycle changes. No Writer workaround, document capture, shadow suppression or public API change.
- Focused runtime, Writer and independent-consumer checks pass, with cached/uncached renders inspected. Native timing varies; the integrated Writer probe improved, while one minimal-consumer run did not. Final visual review of the integrated mitigation remains open. [Exact evidence and pending W6 review](20-V1.1-ACCEPTANCE-AND-RELEASE-PLAN.md#native-resize-paint-mitigation--2026-10-24).

### RKWF-061 — Modern Office Glass stops at the ribbon body

- First seen: 2026-10-24, the user's Showcase screenshots and material-policy review. Glass does not carry into collapsed groups or ribbon/QAT overflow. The user approved those continuations following Glass, retained ordinary selection surfaces and Crystal, and explicitly excluded Office 2007/2010.
- Shared correction: the existing CapturedBackdrop constructor accepts a Ribbon or overflow toolbar. A Ribbon registration follows the scoped modern Glass overlay, owns only group/QAT/Simplified registrations, and retires them on disable/removal/unload/disposal. The host still supplies the capture source. No new public member, setting or Writer-specific popup template.
- Writer glue: the appearance scope owns the registration; the separate explicit popup-blur option retains dropdowns without duplicating modern Glass group capture. Legacy Glass is suspended in rendering and the checkbox while its saved modern choice survives. The preferences schema and Apply/Cancel behavior remain compatible.
- Focused material, Writer rollback/legacy, independent-consumer and selected visual checks pass. Visible-window renders are inspected separately from offscreen fallback. Exact evidence and pending user review are in the [Glass plan](09-FUTURE-THEMES-PLAN.md#ribbon-continuation-material--2026-10-24). W5's accepted motion scope is retained.

### RKWF-060 — Returning from Simplified briefly dims ribbon ink

- First seen: 2026-10-23, the user's W5 recording after the density correction. A visible Writer context-menu probe reproduces the contrast change while RibbonTabControl opacity remains 1, without theme notifications or per-group restore clocks.
- Cause: the shared body presentation fades its entire incoming command layer. Separately, the earlier stationary-QAT fix excludes BelowRibbon because its bounds move vertically; complementary-alpha copies of the same icon dim during that movement.
- Correction: mode changes reveal incoming commands at authored opacity. Mode-only QAT motion with unchanged strip/panel sizes translates one drawing; reversal retains its interpolated band bounds. No Writer workaround, new public API or change to real theme-switch policy.
- Twelve new runtime paint cases fail before and pass after; the surrounding host-motion cases and independent consumer paint checks pass. Native before/after captures show the Save icon retaining its original contrast after correction. Exact evidence and remaining user review are in the [1.1 plan](20-V1.1-ACCEPTANCE-AND-RELEASE-PLAN.md#simplified-exit-ink-contrast--2026-10-23); implementation pitfalls are in [library history](history/library/16-host-layout-transitions.md#3264-stable-caption-and-content-paint-during-layout-transitions--2026-10-18).

### RKWF-059 — Touch caption reflow exposes a destination frame before animation

- First seen: 2026-10-23, the user's W5 follow-up recording after RKWF-058. The ribbon briefly shows its final position, returns to the start and animates. This is a shared pending-cover defect, separate from caption foreground alignment; RKWF-058's change did not alter the hold.
- Focused reproduction: all four Touch title-bar routes move a unique body marker by 12 DIPs after the deferred Background placement claim but before LayoutTransitionStarting. Previous Render-priority coverage inspected the hold before that claim. The supplied recording contains the same destination → start → destination sequence.
- Cause/correction: title-bar placement can move the local adorner layer with the ribbon, and density reflow can arrange the live body before the adorner publishes its cached placement. Correct the inert drawing from the adorner's actual presented origin to the saved outer-coordinate origin, with a live-source fallback only before attachment. Refresh at LayoutUpdated and Rendering and remove both listeners on disposal. Retain local stacking/opacity/shadows, one real layout commit and shared timing; no host workaround or public API change.
- Related protection: the historical [two-part FLIP repair](history/library/04-office2007-and-popup-motion.md#341-two-ordering-bugs-an-effect-painted-over-and-a-flip-that-flashed--2026-07-28) and [density-selector repair](history/library/14-simplified-ribbon.md#visible-layoutdensity-motion-trial--2026-10-13) require old-layout coverage and synchronous start seeding. Body paint is staged before attachment, using its existing presentation routine. The four seed-contract cases and earlier hold checks remain valid but did not establish native density acceptance; their evidence is retained under `TestResults/v1.1-density-motion-contract-20261023/`.
- Latest video: native density review still failed after seeding. At 125%, the supplied recording shows expansion top 82 → 64 → 82 → 100 pixels, and contraction 100 → 118 → 100 → 82. A visible Writer probe reproduced the 18-pixel opposite-direction jump: correction used new live geometry while the adorner was still at its old placement. The revised anchor eliminates that displacement in four native programmed density changes (187 valid screen captures; 16 held-render samples with zero origin error). This is controlled native paint, not physical-input or user acceptance; obscured before-capture frames were excluded.
- Current verification and the remaining native recheck are in the [1.1 plan](20-V1.1-ACCEPTANCE-AND-RELEASE-PLAN.md#deferred-caption-reflow-destination-frame--2026-10-23). Latest recording analysis, probe source, traces and before/after native evidence are under `TestResults/v1.1-density-video-20261023/`; previous evidence remains in the earlier directories above. The implementation pitfall is in [library history](history/library/16-host-layout-transitions.md#3259-ribbon-owned-lifecycle-and-qat-relocation-presentation--2026-10-16).

### RKWF-058 — Touch QAT relocation makes the title-bar height change look abrupt

- First seen: 2026-10-23, W5 manual motion review and the user's recording. Shared runtime presentation defect; the other reviewed W5 behavior passed. The user requested smooth expansion/contraction when QAT enters or leaves the title bar.
- Focused reproduction: all four Touch TitleBar ↔ TabRow/BelowRibbon routes pass caption-background boundary checks, but fail a foreground pixel check. A 16-pixel window-icon marker occupies 22 rows during the crossfade: old and final caption content remain at different vertical centers while the band height interpolates. Background-only assertions missed this.
- Correction: translate both caption foreground drawings to the interpolated band center on the existing shared boundary clock. Scale only the separate background and keep clipping at the interpolated band bounds. Real layout still commits once; Writer owns no title-bar animation or completion workaround. No public API or animation-policy change.
- Evidence and the remaining native recheck are recorded in the [1.1 acceptance plan](20-V1.1-ACCEPTANCE-AND-RELEASE-PLAN.md#touch-caption-height-motion--2026-10-23). Reproduction logs, supplied-video frame extracts and focused renders are under `TestResults/v1.1-touch-caption-20261023/`. The implementation pitfall is in [library history](history/library/16-host-layout-transitions.md#3264-stable-caption-and-content-paint-during-layout-transitions--2026-10-18).

### RKWF-057 — Committing a nested menu choice leaves its containing overflow open

- First seen: 2026-10-23, W4 follow-up after the user passed RKWF-055/056. Shared runtime reproduction: combo, dropdown and split-button menu choices close their own popup but leave QAT/Simplified overflow open. All six standalone cases reach the failing parent-close assertion after a single action and successful child close. The independent ComboQuickAccessKeyboard consumer reproduces it after Enter; later cases were not reached.
- Cause: RibbonDropDownButton closes its menu on the child Click route, while RibbonQuickAccessToolBar only observes direct ButtonBase and split-primary clicks. It deliberately ignores dropdown openers, but receives no separate child-choice completion notification. Prior keyboard expectations returned to the still-open overflow entry instead of requiring dismissal after a committed action.
- Correction: explicitly authorized and implemented on 2026-10-23. An internal dropdown completion notification lets the containing toolbar use CloseOverflow for combo/dropdown/split leaves in both overflow types. Opening or canceling a child keeps its parent available. The parent owns post-choice focus so a hidden child opener cannot overwrite an application command's explicit focus target; keyboard fallback returns to the visible overflow opener. No public API, Writer-specific handler or timer is added.
- Verification: assembled runtime coverage includes 89 unique menu/focus cases; a combined run passed 86, with three pre-invocation focus-setup failures passing a fresh 3/3 retry. A separate final focus/commit selection passes 37/37; counts overlap. Repeat choices, source ownership, one command execution and explicit host focus are covered. Both independent combo keyboard/OverflowMenus scopes and all eight Writer font-choice cases pass. Release net8/net9 and Debug Writer builds have zero warnings/errors. Computer-use interference is plausible but unconfirmed. Original failure/proposal and correction logs remain in `TestResults/v1.1-overflow-dismissal-20261023/`. Implementation is in [library history](history/library/11-touch-density-and-gallery-projections.md#overflow-choice-completion-and-focus--2026-10-23); current evidence and native review remain in the [acceptance plan](20-V1.1-ACCEPTANCE-AND-RELEASE-PLAN.md#shared-overflow-choice-dismissal--2026-10-23).

### RKWF-055 — Caret-only font choices revert instead of updating native typing state

- First seen: 2026-10-23, W4 manual review; Writer responsibility. With no selected text, size, Bold and other inline choices reverted. Focused adapter cases reproduced both size and Bold failures in empty documents and at a caret in existing text.
- Cause/correction: a new TextRange at the selection endpoints does not preserve TextSelection's pending typing properties. Inline mutations, Font dialog commits and Clear Formatting now use the native TextSelection; state reads its effective insertion values instead of adjacent text. Native WPF word/caret semantics remain authoritative, without cached formatting or selection ownership in Writer.
- Evidence: caret regressions cover empty/whitespace insertion, current-word formatting, toggling, family/size/style/colors, dialog/clear, focus return, state refresh, caret movement and document replacement. Synthetic WPF text composition checks subsequent insertion after initial window/dispatcher readiness; it is not physical keyboard or OS IME acceptance. Logs are in `TestResults/v1.1-writer-font-20261023/`; current verification and native recheck belong in the [acceptance plan](20-V1.1-ACCEPTANCE-AND-RELEASE-PLAN.md#writer-caret-formatting-and-copied-font-commits--2026-10-23).

### RKWF-056 — Font presets copied into QAT/custom/overflow change selection without formatting

- First seen: 2026-10-23, W4 manual review; Writer command integration. Font family and size choices in all three menu-copy locations reproduced the failure. Editor selection endpoints/text stayed intact while opening overflow and choosing a preset; inactive blue highlighting was a separate display state.
- Cause/correction: Writer listened to source DropDownClosed, Enter and LostKeyboardFocus, but a selection-only menu updates the source selection without those events. Writer now commits completed source selection changes while preserving active native edits. It binds the Classic/Simplified native font editors' explicit boundaries, identifying Simplified editors through Ribbon.GetQuickAccessSource on focus, and preserves their partial text during state refresh. This is bounded host glue for the existing RKWF-002 commit-semantics candidate; no runtime API or library edit is introduced.
- Test scope: source-linked font family/size menus in QAT, custom groups and QAT overflow; caret-only overflow; Classic/Simplified dropdown close, partial typing, Enter and focus loss. QAT remains selection-only even for an editable source: the earlier manual instruction to type there was invalid. Native review remains in the same acceptance plan; personal settings are not touched by the temporary test fixtures.

### RKWF-054 — Copied toggle buttons forward events but skip the source command

- First seen: 2026-10-23, W4 manual review. Writer's original Bold formats selected text, but its QAT and custom-group copies do not. Shared runtime defect, reproduced without Writer resources or glue.
- Cause: the toggle branch of `Ribbon.CreateCommandProxy` mirrors IsChecked and raises the source Click event, but never executes the source ICommand. `ButtonBase.RaiseEvent(Click)` does not run the native OnClick command stage. Writer correctly sets Bold's Command and editor CommandTarget. Showcase's Bold sample has no formatting Command; its other switches use Checked/Unchecked or Click handlers, so those copies can work despite this gap.
- Focused reproduction: two standalone command-copy cases and both Writer paths failed, while two event-only cases passed. Writer's original-button checks passed before the copied formatting assertion failed. The subsequent audit also reproduced skipped commands in the KeyTip fallback for a button without an automation peer and in stock WPF toggle/checkbox/radio state-only peers. Original logs are retained in `TestResults/v1.1-toggle-proxy-20261023/`; fixtures do not change personal settings.
- Correction: the user explicitly authorized the focused library fix and audit. Internal `ButtonCommandInvocation` raises Click, then reads and executes the source's current Command/CommandParameter/CommandTarget with CanExecute honored. It preserves the single checked-state update and source event handlers. Toggle copies and the affected KeyTip paths share this completion step. Added Click/Command stages apply only to the exact stock WPF state-only peer types; custom providers retain their own activation semantics. No Writer workaround, public API, template or token change.
- Verification: 42 focused runtime cases and both Writer Bold regressions pass. The independent `CommandCopies` consumer passes 72 layouts, 144 Bold on/off round trips and 288 ordinary/split executions after public customization restore. It uses staged public UIA Toggle plus Click routing, not native mouse input. Normal Release runtime builds for net8/net9 and normal Debug Writer build pass with zero warnings/errors. Writer was rebuilt/reopened; native recheck and remaining W4 workflows stay in the [acceptance plan](20-V1.1-ACCEPTANCE-AND-RELEASE-PLAN.md#writer-manual-acceptance).

### RKWF-053 — Dropdown and split-button popups retain native scrollbar chrome

- First seen: 2026-10-23, Writer 1.1 manual review; shared runtime defect. The user's font-color split-button screenshot shows the OS scrollbar inside the themed menu. The user explicitly authorized correction in the main control project.
- Focused reproduction: overflowing standalone `RibbonDropDownButton` and `RibbonSplitButton` popups, using only library resources, both failed the shared-style assertion. Before captures show native rectangular scrollbar chrome. Combo boxes already had the required viewport styling mechanism.
- Correction: extract that existing mechanism into internal `PopupScrollBarStyle` and use it from dropdowns (including derived split buttons) and combos. Register the existing shared style on the native viewport, with the existing local resource-dictionary fallback for deferred popup lookup. Authored viewport scrollbar styles retain precedence. No Writer workaround, public API, token or template change.
- Evidence: 27 focused runtime passes; the independent library-only consumer passes 112 theme/palette/density/direction/control combinations and the existing gallery popup viewport scope. Normal Release runtime builds for net8/net9 and normal Debug Writer build have zero warnings/errors. Before/after and six representative consumer renders were inspected without changing approvals/tolerances. Evidence: `TestResults/v1.1-dropdown-scrollbars-20261023/`. Writer was rebuilt/reopened; user appearance and pointer-scrolling review remains pending in the [1.1 acceptance plan](20-V1.1-ACCEPTANCE-AND-RELEASE-PLAN.md#shared-dropdown-scrollbar-correction--2026-10-23).

### RKWF-050 — Simplified combo display labels attempt to write to read-only font names

- First seen: 2026-10-22, Writer 1.1 host integration; shared runtime defect.
- Focused reproduction: run the existing Writer test `SimplifiedSettingsRequestsDraftApplyCancelAndRestartUseSharedPage` alone in a fresh process. Entering Simplified with Home selected aborts the test host: `A TwoWay or OneWayToSource binding cannot work on the read-only property 'DisplayName'` of `WriterFontChoice`.
- Cause: `RibbonComboBoxSimplifiedProxy.RefreshRows` binds `TextSearch.Text` to a display-label path without an explicit mode; its inherited default is TwoWay. The label is presentation only. The precise runtime correction is `Mode = BindingMode.OneWay` on both display-label binding branches, with a read-only-item regression. Selection/Text synchronization must retain its explicit TwoWay behavior.
- Correction: after reproduction, the user explicitly authorized focused straggler fixes in this chat. Only those two runtime binding modes changed; selection/Text synchronization and Writer's Simplified defaults are intact. The focused regression and existing combo/grouping cases pass 23/23; the independent `SimplifiedCombos` consumer passes its 56 theme/palette/density/direction cases, including simulated 150% Touch DPI. Writer's original Simplified draft/Apply/Cancel/restart test now passes. The earlier aborted run is not reported as a pass.

### RKWF-051 — Detached Writer dialogs need explicit density and appearance scopes

- First seen: 2026-10-22, Writer 1.1 integration; host responsibility.
- Reproduction: select Touch, then open an owned Writer dialog. Window Owner relationships do not inherit `Ribbon.Density`. Find/Replace also retained native white controls under a dark palette.
- Correction: the existing Writer appearance scope applies the saved density to each registered window, including nested color pickers. Find/Replace consumes shared input, checkbox, action-button and surface resources. The View selection is restored before Show and saved separately from ribbon structure; Appearance defaults retain that View preference. The Showcase-style dropdown binds its density permission explicitly to MainRibbon so it follows ribbon hosting and QAT participation.
- Evidence: all ten detached Writer dialog types re-theme and resize through Touch → Compact → Touch, including the nested color picker. Actual Windows file/print pickers remain OS-owned. Mixed-monitor review remains manual.

### RKWF-052 — Writer document presentation needs its own ribbon-boundary animator

- First seen: 2026-10-22; optional application integration, not a library gap.
- Existing behavior: the ribbon animates, but Writer's paper/ruler/preview immediately takes its final available bounds. The user approved a bounded presenter after reviewing the proposal.
- Correction: Writer captures only realized WPF document visuals, holds old paint in Preparing, binds its own translation/clip to BoundaryMotion in Starting, and releases by identity in Ended. Real editor/document ownership never changes. No Showcase helper, runtime API, host completion callback or independent playback timer is used.
- Limits: active composition, unsafe native/media/live-brush visuals, nontrivial root transforms and capture work above 4,000 visual nodes snap. Crystal's live underlay remains host-owned; temporary drawing clips below the occupied ribbon/QAT boundary. Synthetic composition checks fallback cleanup; simulated DPI and offscreen rendering provide automated evidence. Current evidence and review scope live in the [Writer integration checkpoint](19-WRITER-HOST-INTEGRATION.md).

### RKWF-001 — KeyTip leaf activation has no consumer focus-handoff contract

Open focus-handoff candidate. Writer defers post-KeyTip focus to ContextIdle to avoid leaking terminating key input. A minimal cross-control/QAT/editable-input reproduction is still needed.

[Recorded evidence](history/writer-friction-evidence.md#rkwf-001--keytip-leaf-activation-has-no-consumer-focus-handoff-contract).

### RKWF-002 — Editable RibbonComboBox has no single semantic commit boundary

Open commit-semantics candidate. Writer's native editable font combos commit on dropdown close, Enter or focus loss; selection-only menu copies now commit a completed source choice (RKWF-056). A general reusable contract, cancellation and OS composition remain separate from this bounded host correction.

[Recorded evidence](history/writer-friction-evidence.md#rkwf-002--editable-ribboncombobox-has-no-single-semantic-commit-boundary).

### RKWF-003 — QAT external UIA action semantics need an isolated reproduction

Bounded shared correction implemented: QAT and overflow item peers forward real control roles/patterns, and Toggle completes the existing source state/Click/Command path once. A tab-peer bridge exposes the tab-row QAT. Binding/activation regressions and an independent external client cover all three placements; actual consumer and remaining overflow review stay in the [1.1 plan](20-V1.1-ACCEPTANCE-AND-RELEASE-PLAN.md). Discovery alone is not an action pass; Backstage remains separate.

[Recorded evidence](history/writer-friction-evidence.md#rkwf-003--qat-external-uia-action-semantics-need-an-isolated-reproduction).

### RKWF-004 — Backstage custom page actions are absent from external UIA traversal

Bounded shared correction implemented: Backstage groups navigation and native page controls separately, reusing existing template elements and native providers. The navigation Group owns its native item peers, preserving File's selection interface; region resets run after discovery/presenter realization to avoid recursive notification callbacks. No Writer workaround, template/layout or binding changes are needed. Focused runtime/Writer tests and a notification-subscribed independent client cover Modern, Classic2010, Classic2007 and both Crystal layouts. See the [implementation record](history/library/19-accessibility.md#3279-backstage-page-content-automation--2026-10-25); current Inspect hierarchy review and remaining presentation acceptance stay in the [1.1 plan](20-V1.1-ACCEPTANCE-AND-RELEASE-PLAN.md).

[Recorded evidence](history/writer-friction-evidence.md#rkwf-004--backstage-custom-page-actions-are-absent-from-external-uia-traversal).

### RKWF-005 — Ribbon groups have no first-class in-group separator

Corrected and accepted. RibbonGroupSeparator now supplies adaptive in-group chrome; theme/variant, collapsed, RTL and 100–200% DPI evidence is recorded. Whole-ribbon contrast themes remain separate.

[Recorded evidence](history/writer-friction-evidence.md#rkwf-005--ribbon-groups-have-no-first-class-in-group-separator).

### RKWF-006 — Main-ribbon leaf commands are absent from external UIA traversal

Bounded shared corrections implemented: the standard selected-content presenter name restores discovery with binding-safe stretched layout defaults. The existing source Toggle providers now also run native Click/Command activation; Writer's document-formatting comparison exposed the earlier state-only behavior despite correct discovery. No Writer workaround or new public API is added. See the [activation pitfall](history/library/19-accessibility.md#3278-source-toggle-automation-completes-host-actions--2026-10-25) and the [1.1 plan](20-V1.1-ACCEPTANCE-AND-RELEASE-PLAN.md) for focused evidence and remaining actual-consumer/presentation acceptance; QAT wrapper semantics remain in RKWF-003.

[Recorded evidence](history/writer-friction-evidence.md#rkwf-006--main-ribbon-leaf-commands-are-absent-from-external-uia-traversal).

### RKWF-007 — Windows WPF print picker cannot host Writer's existing fixed preview

App-resolved with WriterPrintSetupDialog and isolated preview/print submission. W4-C still needs a non-PDF physical driver with unusual queue/ticket/imageable-area constraints.

[Recorded evidence](history/writer-friction-evidence.md#rkwf-007--windows-wpf-print-picker-cannot-host-writers-existing-fixed-preview).

### RKWF-008 — The single real-window STA integration case needs a parallel-load timeout allowance

Retained harness exception: the default remains ten seconds, with explicit allowances for combined window and theme/DPI matrices. Writer tests now share one serialized STA dispatcher to avoid WPF's cached WindowChrome crossing threads, and nested STA fixtures reuse it. See the 2026-10-02 update in the evidence record.

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

The shared `Ribbon.ApplicationOrbGlyphTemplate` hook is available (§3.194). Writer migrated to a glyph-only W template on 2026-10-02 and removed its post-render visual-tree patch. Main Orb and Classic2007 proxy checks pass; final live Writer appearance acceptance remains open. No new runtime API is proposed.

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

### RKWF-045 — Insert forms bypassed themed controls and dialog resources

App-corrected: Table, Picture, Hyperlink and Date and Time now consume themed fields,
buttons and surfaces. A Writer-local DatePicker/calendar template retains native WPF
behavior while replacing stock colors; RibbonKit has no date-picker control. Calendar
weekday resources must live directly in ControlTemplate.Resources. No runtime change.

[Verification](history/02-writer-foundation.md#3163-writer-insert-dialog-theming--2026-09-16).

### RKWF-046 — Crystal paper underlay needs Writer-owned layout and native scroll presentation

App-owned integration: Crystal Paper view with QAT below and the ruler hidden
reads the shared `QatBelowHost` bounds, overlaps Writer's document host and applies
a short content fade. Writer owns the native scroll-content opacity mask,
scrollbar inset and margin-guide paint; changes of ruler, theme, QAT or view
restore ordinary layout. No shared ribbon template or public API is changed.
The shared drawer and native ScrollViewer template-part names are integration
dependencies. Final appearance and live input remain user review gates.

[Verification](history/02-writer-foundation.md#3216-writer-crystal-integration-and-deferred-validation--2026-10-02).

### RKWF-047 — Simplified customization needs host dialog and persistence integration

App-corrected 2026-10-14. Writer previously registered only classic/QAT requests
and pages, leaving Customize Ribbon without a handler in Simplified mode. Add the
shared simplified page with the factory reset baseline, route its request and
select the current mode's editor; reuse the shared dialog's page eligibility.
Author useful command defaults using existing attached placement/size properties.
No runtime change or host preference-schema change is required.

Writer owns ribbon storage and outer dialog rollback. The simplified page's Apply
must advance the ribbon-only checkpoint through SimplifiedCustomizationChanged;
otherwise a later outer Cancel reverses that explicit Apply. A temporary handler
is scoped around the modal dialog and removed in finally. The ordinary handler
saves pin/remove changes outside Options, honoring existing load/rollback guards.
Appearance previews retain their separate Apply/Cancel boundary.

Two focused independent Writer checks passed: actual context-menu request routes,
draft Cancel, Apply then Cancel, OK, pin persistence/restart, and the existing
Crystal appearance rollback/persistence contract. No new timing workaround or
automation gap was introduced. Live visual review/full validation remain open in
the [active simplified plan](15-SIMPLIFIED-RIBBON-PLAN.md#s7-host-defaults-and-customization-integration--2026-10-14).

### RKWF-048 — Solution Toolbox discovery exposes Writer sample controls

App authoring correction, first reported 2026-10-15 during Toolbox review.
Visual Studio can discover public Writer visual types from the open solution even
though the sample is not packable and none are advertised by RibbonKit's package
manifest. Mark those types with standard ToolboxItem(false) and
DesignTimeVisible(false) hints; preserve their public visibility and runtime use.
No control behavior, timing workaround or host integration is added. One focused
Writer reflection contract passed. Project-reference filtering depends on the
active Visual Studio host; package consumer validation cannot prove that result.
Current acceptance stays in
[Toolbox plan T2c](17-TOOLBOX-ITEM-DEFINITION-PLAN.md#t2c--owner-authoring-and-live-review-corrections-2026-10-15).

### RKWF-049 — Popup text sharpens after opening

Shared correction implemented 2026-10-17; Writer live acceptance remains open. Opening a
ComboBox or dropdown initially shows soft text that becomes sharper; the delay
varies from quick to noticeable, including after movement stops. The user's
clarification concerns animated popup text, not a confirmed document-editor defect.

The earlier WPF 10 offscreen redraw probe did not reproduce the native symptom.
An actual screen-pixel sequence using WPF 8.0.31, hardware tier 2 and the display's
125% DPI reproduced soft text at opacity 1/content offset 0 that sharpened roughly
half a second later. Releasing the completed transform clock, fixed hinting and
removing the popup effect did not remove that delay. Invalidating only the popup
border also failed; invalidating its visible TextBlocks once at rest removed the
long delay in bracketed baseline/fix/baseline comparisons.

PlayFlyoutOpen now requests that text redraw at slide completion, or fade completion
for a surface without sliding content. No timer, font-mode override or Writer glue
is added. Existing timing/easing, reduced-motion policy and popup anchors remain.
The independent consumer's Crystal/Office2024 first-open and interrupted-reopen
sequences were unchanged from their first captured settled frame through one second;
those first samples were 37–97ms after managed rest. An earlier verification caught
the preceding compositor frame at about 28ms, followed by sharp pixels around 77ms:
this does not promise synchronous screen presentation on the managed callback.
The three new dropdown/gallery/fade-only regressions cover the redraw request,
authored font settings, text binding and unchanged bounds; the original regression
fails when completion redraw is removed. NativeGalleryPopup consumer checks passed.

Current limits: no actual Writer input-driven frame recording, mixed-monitor DPI
or OS reduced-motion acceptance. The native capture uses programmatic opening in
a separate consumer, not Writer. Diagnostic source, frames and logs are retained
under TestResults/native-text-settle and text-settle-*; the temporary visible-window
probe was removed from both test projects. No snapshot approvals changed.
Implementation and verification are recorded in
[§3.262](history/library/04-office2007-and-popup-motion.md#3262-refresh-popup-text-when-opening-motion-settles--2026-10-17).
The separate static Crystal shadow correction did not solve this temporal symptom;
its evidence remains in
[plan 18](18-HOST-LAYOUT-TRANSITION-PLAN.md#post-review-geometry-repairs-and-full-validation--2026-10-17).

**Ribbon control scope expansion — 2026-10-17.** The user also reported the delay
after changing ribbon tabs. Shared control motion now refreshes the affected
visible render subtree once at rest: Classic/Simplified tab bodies, opening and
restored surfaces, Backstage pages, contextual headers, messages, KeyTips, title
glides, theme fades, ribbon/gallery scrolling and existing MDI state transitions.
Layout/density drawing cleanup also refreshes the real ribbon presentation.
Font modes, animation cadence and public APIs are unchanged. Actual icon-only QAT
strips are exempt; the Simplified row reuses the same toolbar type with visible
labels and remains eligible. Decorative washes, the tab marker and orb rotation
do not need text refreshes.

Current evidence: 79 unique focused runtime cases across the 67-case existing
selection and 12 new completion/exemption cases. The independent native consumer
reproduced Classic/Simplified text changes after movement stopped, then retained
unchanged text from its first settled capture through one second with the fix,
including editable RibbonComboBox text. This is hardware tier 2, WPF 8.0.31 at the
single display's 125% DPI. Logs and before/after PNGs are under
TestResults/native-tab-text-settle and text-settle-tab-*. The independent host
contract covers 24 theme/direction/density/subscriber combinations; its QAT motion
and optional host-content animation remain separate from text rendering policy.
Normal Debug/Release solution builds pass with zero warnings/errors, including
both runtime TFMs/API/XML checks.

The user accepted the improved appearance on 2026-10-18 ("it looks great now").
This accepts the control-animation text they reviewed, without extending the
separate native gates below.

The user explicitly limited this text-settle work to RibbonKit controls. Application document
rendering and RichTextBox-specific handling are excluded; no rendering-hint pulse,
host content-animator change or RichTextBox-specific test is retained for that fix. Native Writer
review, physical input, genuine mixed-monitor DPI and OS reduced-motion acceptance
remain open. No new full-suite/package run or approval update is claimed. The
implementation record is [§3.263](history/library/04-office2007-and-popup-motion.md#3263-shared-text-refresh-after-control-animation--2026-10-17).

### Verification note — 2026-09-15 cleanup

77 focused Writer tests passed. The combined window run hit the known WPF WindowChrome
cross-thread cache issue; the affected table check passed in a fresh process. No new live UI
or full-suite acceptance is claimed.
