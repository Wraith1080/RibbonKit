# RibbonKit Writer — Consumer Friction Log

> **Status:** living consumer-evidence record created 2026-08-21. Entries describe friction found while
> building RibbonKit Writer; they are not confirmed RibbonKit defects, approved runtime work or current
> implementation status. [`04-DESIGN-NOTES.md` §5](../04-DESIGN-NOTES.md#5-current-state--next-steps)
> remains authoritative.

## 1. Purpose

Writer is RibbonKit's sustained real-application consumer. This log captures places where an ordinary
WPF application needs surprising glue, timing workarounds, duplicated integration code or testing
exceptions to use the ribbon successfully. It keeps that evidence separate from Writer's feature plan
and from RibbonKit's accepted implementation history.

Record friction when it is encountered; do not wait until a packet closes. An entry may later become:

- **App-owned:** normal document/editor responsibility or ordinary WPF behaviour; keep the workaround
  in Writer and improve samples or documentation if useful.
- **Runtime candidate:** repeated or high-impact friction that RibbonKit can solve additively without
  taking over application state.
- **Approved runtime work:** separately authorized RibbonKit scope with a focused failing test, public
  API review when applicable, Showcase coverage and proportional live/visual verification.
- **Closed:** resolved, superseded, not reproducible or intentionally outside RibbonKit's boundary.

## 2. Promotion gate

Do not edit `src/RibbonKit/**` merely because an entry exists. Before promotion, the lead must:

1. Reproduce the behaviour in a minimal RibbonKit consumer or focused Writer test.
2. Identify the exact RibbonKit control, template, service or automation peer involved.
3. Show why the app-owned workaround is unsafe, misleading, excessively repetitive or impossible.
4. State the smallest additive library direction and its compatibility/accessibility consequences.
5. Obtain separate user approval for the runtime packet.

Every accepted runtime correction must add RibbonKit tests, XML documentation for public API, a
relevant Showcase scenario and live verification on the affected surface. Existing snapshot approvals
remain read-only until actual/diff evidence justifies a deliberate change.

## 3. Entry template

```text
### RKWF-NNN — Short title

- First seen / packet:
- Status:
- Consumer goal:
- Reproduction and evidence:
- Friction:
- Current app-owned workaround:
- Application impact:
- Smallest possible library direction:
- Evidence still required:
```

## 4. Open observations

### RKWF-001 — KeyTip leaf activation has no consumer focus-handoff contract

- **First seen / packet:** 2026-08-21, Writer W1-C Home formatting commands.
- **Status:** Open runtime/documentation candidate; app workaround accepted for W1-C.
- **Consumer goal:** after a KeyTip invokes a formatting command, return keyboard input to the native
  document editor without inserting any characters from the KeyTip sequence.
- **Reproduction and evidence:** the actual Writer window leaked terminating KeyTip input into the
  document when the app restored editor focus at `DispatcherPriority.Input`. The accepted controller
  attaches focus restoration to each relevant command and defers it to `ContextIdle`.
- **Friction:** `KeyTipService` invokes a leaf and tears down its session internally, but consumers have
  no public activation-completed notification or declarative focus-return target. An editor app must
  know the service's input timing and repeat focus glue across ribbon/QAT actions.
- **Current app-owned workaround:** `WriterEditingRibbonController.RestoreEditorFocusDeferred()`.
- **Application impact:** timing-sensitive integration code in any long-lived editor surface; an
  incorrect dispatcher priority can corrupt typed content.
- **Smallest possible library direction:** first evaluate documentation or a general completion hook;
  only then consider an additive focus-return contract that does not make RibbonKit own editor focus.
- **Evidence still required:** a minimal consumer test proving the leakage and expected focus behavior
  across ordinary buttons, toggles, menus, QAT proxies and editable ribbon inputs.

### RKWF-002 — Editable RibbonComboBox has no single semantic commit boundary

- **First seen / packet:** 2026-08-21, Writer W1-C font-family and font-size controls.
- **Status:** Open ergonomics/documentation candidate; may remain ordinary WPF app responsibility.
- **Consumer goal:** let users type or choose a value, commit once, run a command and return to editing.
- **Reproduction and evidence:** committing on `SelectionChanged` accepted the first type-to-search
  match, restored editor focus too early and sent the remaining characters into the document.
- **Friction:** a command-backed editable ribbon combo must compose `DropDownClosed`, Enter handling and
  keyboard-focus loss while preventing selection-state refresh from overwriting in-progress text.
- **Current app-owned workaround:** Writer commits from those three boundaries and deliberately ignores
  intermediate `SelectionChanged` events.
- **Application impact:** repeated, easy-to-get-wrong glue for Office-style font and size controls.
- **Smallest possible library direction:** document a canonical binding pattern or provide an optional
  additive commit event/command behavior; do not change inherited WPF ComboBox semantics by default.
- **Evidence still required:** a reusable minimal example covering typing, auto-selection, drop-down
  choice, Enter, Escape, focus loss, invalid input, selection refresh and IME composition.

### RKWF-003 — QAT external UIA action semantics need an isolated reproduction

- **First seen / packet:** 2026-08-21, Writer W1-C live minimized-ribbon/QAT verification.
- **Status:** Open investigation; not yet a confirmed RibbonKit defect.
- **Consumer goal:** external UI Automation clients should discover named QAT commands as actionable
  buttons with the same Invoke/Toggle semantics as their source controls.
- **Reproduction and evidence:** live UIA traversal presented the visible Writer QAT entries through
  `DataItem`-like wrappers without an available Invoke pattern, while direct in-process
  `RibbonButtonAutomationPeer` activation and the actual mouse path succeeded.
- **Friction:** the difference forced live verification to use clickable bounds and leaves uncertainty
  about what external automation and assistive-technology clients receive from the QAT item hierarchy.
- **Current app-owned workaround:** preserve automation IDs/names and verify commands through direct
  peers plus real mouse/keyboard paths.
- **Application impact:** possible fragility for external automated testing; accessibility impact is
  unproven until the hierarchy is reproduced with more than one client.
- **Smallest possible library direction:** inspect the QAT `ItemsControl` container/peer hierarchy and
  add a specialized peer only if a minimal external UIA reproduction confirms lost action semantics.
- **Evidence still required:** Inspect.exe or equivalent external-client capture for hand-declared QAT
  buttons, projected QAT proxies and overflow proxies, including Invoke/Toggle, name, enabled state and
  source/proxy consistency.

### RKWF-004 — Backstage custom page actions are absent from external UIA traversal

- **First seen / packet:** 2026-08-21, Writer W1-D recent-document live verification.
- **Status:** Open investigation; not yet a confirmed RibbonKit defect.
- **Consumer goal:** arbitrary application content hosted by Backstage should retain its ordinary WPF
  UI Automation peers after the surface is presented through the Backstage adorner.
- **Reproduction and evidence:** the realized recent rows are ordinary focusable `Button` controls with
  names, unique IDs, full-path help text and a working in-process Invoke pattern. Actual mouse activation
  opens the document and closes Backstage. Repeated external `UIAutomationClient` traversal of the live
  Backstage window exposed the navigation labels but no recent-row buttons or their text content.
- **Friction:** Writer can prove its own button semantics and real interaction, but cannot make an external
  accessibility client discover those actions without understanding or replacing the Backstage host peer.
- **Current app-owned workaround:** keep native `Button` semantics, keyboard focus, automation metadata,
  tooltips and in-process automation coverage; also verify the real mouse/keyboard path.
- **Application impact:** external automation and assistive-technology clients may be unable to discover
  actionable custom Backstage page content even though sighted pointer interaction works.
- **Smallest possible library direction:** isolate whether the adorner/content-presenter peer boundary is
  excluding the arbitrary page subtree, then add or bridge peers only if multiple external clients reproduce
  the omission. Do not special-case Writer or recent documents.
- **Evidence still required:** Inspect.exe or equivalent captures for Modern, Classic2010 and Classic2007
  Backstage content, including ordinary buttons, lists and custom automation peers; confirm keyboard-client
  discovery separately from actual focus movement.

### RKWF-005 — Ribbon groups have no first-class in-group separator

- **First seen / packet:** 2026-08-24, Writer W1-D visual review and W2 planning.
- **Status:** Corrected in RibbonKit on 2026-08-29; user-verified through every supported theme/light-dark variant,
  actual collapsed flyouts, RTL and the 100-200% DPI matrix by 2026-08-30. High Contrast is not an RKWF-005
  acceptance requirement because RibbonKit does not currently claim a whole-ribbon Windows contrast-theme mode.
- **Consumer goal:** visually partition related command clusters inside one `RibbonGroup` without creating
  fake groups, hard-coded borders or layout-only command items.
- **Reproduction and evidence:** the accepted Writer Home surface needs lighter divisions within command-dense
  groups, but RibbonKit exposes only whole-group boundary chrome, menu separators and the application-menu
  separator. None participates as an adaptive item inside a ribbon group.
- **Friction:** an app-owned `Border` or `Separator` would need to reproduce theme tokens, large/medium/small
  measurement, collapsed-group behavior, RTL placement, visibility and automation semantics that belong to
  the ribbon item system.
- **Current resolution:** the lookless `RibbonGroupSeparator` uses shared theme chrome, adapts its desired
  width and height across large/medium/small group states and returns to its large presentation with content
  re-homed into a collapsed flyout. Writer uses it between Paragraph command clusters. The Showcase now
  demonstrates a height-constrained separator inside Font's compact button row, a full-height direct-child
  separator in Insert/Illustrations, a separator between a large View/Zoom command and a stacked medium
  command cluster, and a direct-child separator between Paste and Select in the Localization/RTL lab.
- **Application impact:** dense groups are harder to scan, while consumer-created dividers risk inconsistent
  adaptive layout and theme behavior across Office generations.
- **Implemented library direction:** `RibbonGroupSeparator` is a non-focusable, non-hit-test control whose
  symmetric template follows RTL layout naturally. It implements `IRibbonSizeAware`, exposes a read-only
  effective `SizeState`, and is deliberately excluded from KeyTips, QAT projection, customization command
  discovery and UI Automation control content. The Ribbon Editor now inserts this type instead of a stock WPF
  separator.
- **Evidence still required:** focused realized coverage now proves horizontal LTR/RTL placement, all effective
  size states, collapsed-to-large behavior, theme-brush resolution and exclusion from command/accessibility
  surfaces; structural Showcase contracts pin the authored examples and their neighboring commands. The user has
  accepted every supported theme/light-dark variant, actual collapsed flyouts, RTL placement and 100-200% DPI.
  Whole-ribbon Windows contrast-theme support is separate future accessibility work rather than a separator defect.

### RKWF-006 — Main-ribbon leaf commands are absent from external UIA traversal

- **First seen / packet:** 2026-08-24, Writer W2-E Page/View live verification.
- **Status:** Open investigation; not yet a confirmed RibbonKit defect and no runtime change is approved.
- **Consumer goal:** external UI Automation clients should discover the visible Page/View ribbon commands with
  their assigned names, IDs, enabled state and Invoke/Toggle/ExpandCollapse patterns.
- **Reproduction and evidence:** an exact-process `UIAutomationClient` traversal of the live 125%-scale Writer
  window discovered the Page and View `TabItem` peers and ordinary status content, but none of the realized named
  leaf controls in their groups. W2-F repeated the result: the View tab was reachable, but its realized `ViewRuler`
  and `ViewMarginGuides` toggle leaves were not. The same controls retain app-assigned automation metadata, expose
  working in-process RibbonKit peers, and pass actual mouse and KeyTip activation.
- **Friction:** Writer can preserve metadata and prove direct peer/action behavior, but it cannot make an external
  accessibility client traverse children that the ribbon/group peer hierarchy does not publish.
- **Current app-owned workaround:** keep explicit automation names/IDs, stable command IDs and KeyTips; cover
  in-process peer semantics and separately exercise the real mouse/keyboard paths.
- **Application impact:** external test clients and assistive technologies may not discover Page/View commands even
  though sighted pointer and keyboard operation work.
- **Smallest possible library direction:** isolate the `RibbonAutomationPeer`/`RibbonGroupAutomationPeer` child
  hierarchy with a minimal consumer before considering an additive peer bridge. Do not special-case Writer.
- **Evidence still required:** Inspect.exe or equivalent captures across expanded, narrow/collapsed and minimized
  ribbon states, plus comparison with the Showcase and a second UIA client.

### RKWF-007 — Windows WPF print picker cannot host Writer's existing fixed preview

- **First seen / packet:** 2026-08-24, Writer W2-E user-review correction.
- **Status:** Resolved in the app; not a RibbonKit runtime defect.
- **Consumer goal:** select a printer while seeing the exact fixed paginator that Writer will submit.
- **Reproduction and evidence:** `System.Windows.Controls.PrintDialog.ShowDialog()` opened the current Windows print
  picker with a large empty pane stating that the app did not support print preview, even though Writer already owned
  a stable `FixedDocumentSequence`. WPF's public flow returns the selected queue/ticket from `ShowDialog` and accepts
  the paginator later through `PrintDocument`; it offers no paginator input for that Windows preview pane.
- **Friction:** the OS message contradicted Writer's working preview and made the accepted same-paginator contract look
  broken. It could not be corrected from ribbon templates or by changing the existing preview view.
- **Current app-owned resolution:** Backstage Print opens `WriterPrintSetupDialog`, which selects from installed queues,
  fits the exact current snapshot in a Writer preview, detaches it on close and submits through the existing validated
  `WriterPrintDialogDevice`/`WriterPrintService` path without showing the unsupported Windows pane.
- **Application impact:** Writer currently exposes its streamlined printer/page summary rather than every advanced
  driver-specific option from the Windows picker. Page size, orientation and margins remain on Writer's Page tab.
- **Smallest possible library direction:** none. Keep this app-owned unless a future reusable print-setup control is
  explicitly scoped outside RibbonKit's ribbon-control responsibilities.
- **Evidence still required:** later W4-C physical-printer coverage should include a non-PDF driver with unusual
  capabilities and confirm that its queue/ticket validation and conflict report remain correct.

### RKWF-008 — The single real-window STA integration case needs a parallel-load timeout allowance

- **First seen / packet:** 2026-08-24, Writer W2-E user-review correction full gate.
- **Status:** Bounded test-harness exception; no product or RibbonKit defect.
- **Reproduction and evidence:** the complete Writer suite passed alone in nine seconds, but the one intentionally
  combined `MainWindow`/`WindowChrome` STA case exceeded the helper's ten-second ceiling when the 63-image visual
  project ran concurrently. The same assertions passed after the harness allowed that case twenty seconds.
- **Friction:** splitting the case across fresh STA threads previously triggered WPF `WindowChrome` cross-thread
  ownership failures, while increasing the global timeout would weaken feedback for every small STA test.
- **Current app-owned resolution:** `StaTestHelper.RunAsync` accepts an optional timeout; only the combined real-window
  case requests twenty seconds. The default remains ten seconds and production code is unchanged.
- **Evidence still required:** keep watching the case duration in full parallel solution runs; investigate subdivision
  only if WPF chrome ownership can remain on one dispatcher or the duration approaches the bounded allowance.

### RKWF-009 — Initial paper margins need a post-Loaded invariant

- **First seen / packet:** 2026-08-24, Writer W2-C startup correction after W2-E.
- **Status:** Resolved in the app; not a RibbonKit runtime defect.
- **Consumer goal:** the initial untitled document must enter Paper mode with its logical page margins already
  applied, so the first caret never appears against the paper's top-left edge.
- **Reproduction and evidence:** the real Writer window intermittently showed correct centred paper geometry but
  zero `FlowDocument.PagePadding`; using New replaced the document and reapplied the margins. Initialization assigned
  the document and page model before the native `RichTextBox` completed its first Loaded/template pass, with no
  later surface invariant to repair a late reset.
- **Current app-owned resolution:** `WriterEditorSurface` reapplies its selected presentation idempotently on Loaded.
  A focused hosted-window regression resets `PagePadding` after setup but before first load and requires all four
  logical margins to be restored by that first Loaded pass.
- **Application impact:** one extra layout application occurs when the editor surface loads; it neither replaces the
  editor/document nor changes selection, undo, clipboard, IME or preview ownership.
- **Smallest possible library direction:** none. This is ordering between Writer's app-owned document model and its
  app-owned editor surface.
- **Evidence still required:** repeat cold launches outside the debugger and include the startup caret/margin check in
  W4-C across the available DPI matrix.

### RKWF-010 — Initial editor focus needs a post-render handoff

- **First seen / packet:** 2026-08-24, Writer cold-start correction after W2-E.
- **Status:** Resolved in the app; not a RibbonKit runtime defect.
- **Consumer goal:** a normal cold launch should place the insertion caret in the document so typing works immediately
  without clicking the paper.
- **Reproduction and evidence:** startup called the initial Paper-mode transition with focus restoration disabled while
  the window was not yet rendered, but no later owner completed the focus transfer. The paper was visible and enabled
  while the ribbon/window retained focus.
- **Current app-owned resolution:** after the first `ContentRendered`, Writer queues one Input-priority handoff that
  assigns both logical and keyboard focus to the existing `RichTextBox` and refreshes insertion-state ribbon values.
  New/Open/Save/Save As and recent-file activation reuse it after the shell leaves its busy state, including cancelled
  dialogs. It does not run when Backstage or Print Preview has become the intended surface.
- **Application impact:** ordinary cold launches accept typing immediately without replacing the editor/document or
  altering undo, selection, IME, clipboard or preview state.
- **Smallest possible library direction:** none. RibbonKit does not own the host application's initial document-focus
  policy.
- **Evidence still required:** repeat cold launches outside the debugger and confirm caret visibility plus immediate
  Latin/IME/RTL input at W4-C DPI scales.

### RKWF-011 — Backstage has no host-level close-completed callback

- **First seen / packet:** 2026-08-24, Writer W0-F Backstage focus integration.
- **Status:** Corrected in RibbonKit on 2026-08-29 and live-accepted on 2026-08-30. The user confirmed
  that the host behavior occurs after the Backstage close animation finishes, matching the intended
  close-completed boundary.
- **Consumer goal:** restore editor focus after any Backstage dismissal without wiring every File action or guessing the
  exit-animation duration.
- **Reproduction and evidence:** `Ribbon.IsBackstageOpen` is the only general observable state, but it changes to
  `false` when closing starts. The Backstage adorner and any Classic orb proxy remain until an internal
  `RibbonMotion.PlayClose` completion callback removes them. `Backstage.BackRequested` covers only a request from the
  Back button/Escape and also precedes teardown; no public Ribbon lifecycle event reports that animated teardown has
  completed. `IsBackstageOpen` additionally represents `RibbonApplicationMenu`, whose popup lifecycle is separate.
- **Friction:** a host can observe logical close initiation centrally, but cannot reliably distinguish it from visual
  close completion across File-toggle, Back/Escape, KeyTip, programmatic, Classic2010 and Classic2007 paths.
- **Current resolution:** `Ribbon.BackstageClosed` is a non-cancellable CLR event raised only after the real Backstage
  adorner, Classic orb proxy and placement state have been torn down. Writer now starts its guarded focus return from
  that completion event; its preview-demand observer and dialog/busy/intended-focus policy remain app-owned.
- **Application impact:** hosts can now use the exact adorner-removal boundary without per-command focus wiring;
  asynchronous command, dialog and intended-focus policy remains correctly owned by the application.
- **Implemented library direction:** a non-cancellable CLR `Ribbon.BackstageClosed` event is raised once after
  the exit animation completes, the Backstage adorner/proxy is removed, placement state is restored and the close was
  not cancelled by a reopen. Do not raise it for `RibbonApplicationMenu` and do not make Ribbon own a focus target.
  A non-cancellable `BackstageClosing` event may be considered separately for symmetry, but is too early for focus
  restoration.
- **Evidence still required:** none for the consumer friction. Realized-STA coverage proves
  programmatic/reduced-motion closure for Modern, Classic2010 and Classic2007, reopen cancellation and exclusion of
  `RibbonApplicationMenu`; Showcase usage and public API/XML documentation are present. The user's live acceptance
  confirms that host behavior begins only after the close animation completes.

### RKWF-012 — Realized native-undo tests must serialize visible WPF window ownership

- **First seen / packet:** 2026-08-26, Writer W3-B FlowDocument table core.
- **Status:** Resolved in the Writer test harness; not a RibbonKit runtime defect.
- **Consumer goal:** prove that structural table commands enter the actual `RichTextBox` undo stack without making
  unrelated focus-sensitive editor tests intermittent.
- **Reproduction and evidence:** direct FlowDocument table mutations reported native undo only when the editor was
  hosted in a shown Window. Running the new realized tests in parallel with the existing editor-surface focus case
  intermittently moved process keyboard focus and failed that unrelated assertion; isolated runs passed.
- **Current app-owned resolution:** the W3-A structured-content and W3-B table suites join the existing non-parallel
  `Writer UI` xUnit collection. After rebuilding, the complete 333-test Writer suite passed repeated runs and the
  full solution gate while retaining real-window native undo evidence.
- **Application impact:** production code is unchanged. Only focus-owning WPF integration tests are serialized; pure
  geometry, model and persistence tests remain parallelizable.
- **Smallest possible library direction:** none. Process-global Windows focus and WPF native undo realization belong
  to the consumer test environment, not RibbonKit.
- **Evidence still required:** keep the collection bounded to tests that truly show/focus Windows, and watch full-run
  duration before moving additional tests into it.

### RKWF-013 — InRibbonGallery popup background can remain unresolved in its popup HWND

- **First seen / packet:** 2026-08-26, Writer W3-C live table-picker acceptance.
- **Status:** Corrected in RibbonKit on 2026-08-29; user-verified through every theme/light-dark variant and the
  100-200% DPI matrix by 2026-08-30. The tokenless system-background fallback is focused-tested; collapsed/RTL do
  not change that brush-resolution contract, and whole-ribbon High Contrast is not currently supported.
- **Consumer goal:** an expanded `InRibbonGallery` should paint an opaque theme/high-contrast popup surface before its
  shared presenter is re-homed, so underlying ribbon commands never show through the tile grid.
- **Reproduction and evidence:** in the real 125%-scale Writer window, `PART_PopupHost.Background` remained null even
  though the shared template declares `RibbonKit.Brushes.Ribbon.ContentBackground`. The 8-column table grid rendered,
  but the File/Home ribbon content was visible between tiles. Direct inspection confirmed the null background; the
  final standard and RTL captures became opaque after the app supplied the resolved brush.
- **Friction:** the consumer cannot set the popup host through public `InRibbonGallery` API. It must apply the template,
  find the declared part by name and assign a brush after initialization.
- **Current resolution:** `InRibbonGallery` resolves the theme-owned content background from the connected gallery
  when its popup HWND opens, uses the system Window brush in High Contrast, and refreshes an open surface after theme
  or High Contrast changes. Writer's template-part lookup and direct background assignment have been removed.
- **Application impact:** the popup is now opaque without private template-part access; gallery selection, re-homing
  and input behavior remain unchanged.
- **Implemented library direction:** the focused realized consumer reproduces the unresolved surface; the gallery now
  resolves/reapplies the theme-owned popup background from its connected resource scope when the separate HWND opens.
  High Contrast uses the system Window brush, and open popups follow runtime theme/High Contrast notifications.
- **Evidence still required:** the user has accepted every Office generation/light-dark variant and 100-200% DPI
  without the Writer override. A focused tokenless-consumer case now proves that opening a popup with no
  `RibbonKit.Brushes.Ribbon.ContentBackground` resource uses `SystemColors.WindowBrush`; the existing scoped-theme
  case separately proves recovery of the connected gallery's resolved brush after the detached popup host loses it.
  No tokenless whole-Showcase visual check is required because RibbonKit otherwise requires a theme token dictionary,
  and complete Windows contrast-theme support is not currently claimed.

### RKWF-014 — Transient Writer selection can collapse the active contextual tab during a table command

- **First seen / packet:** 2026-08-27 user follow-up after Writer W3-C acceptance; assigned to planned W3-E.
- **Status:** Corrected in the Writer-owned W3-E1 foundation; live repeated-command observation remains pending. This
  is not a confirmed RibbonKit defect and no runtime change is approved.
- **Consumer goal:** invoking a command from Table Tools should keep that contextual page visually stable when the
  committed result and caret remain in the table, even if the native mutation temporarily replaces table structure or
  transfers focus into ribbon/popup chrome.
- **Reproduction and evidence:** the user observed Table Tools quickly switch to Home and return while clicking one of
  its commands. Current code publishes `WriterTableInteractionController.StateChanged` for every native selection
  change and immediately maps an unresolved intermediate selection to `TableToolsTab.Visibility=Collapsed`. Structural
  table services can replace document elements and restore the caret within one logical command, creating a credible
  outside-table then inside-table publication sequence. No trace yet proves that RibbonKit changes tabs while Table
  Tools remains continuously visible.
- **Friction:** a contextual ribbon consumer must distinguish durable editor context from transient WPF selection and
  focus states. Publishing every intermediate state makes the ribbon react correctly to state that should never have
  become user-visible.
- **Current app-owned correction:** W3-E1 captures a document-bound table/picture/hyperlink snapshot before popup
  focus moves, revalidates the exact live object before execution and defers table-state refresh while an app-owned
  structural mutation emits intermediate selection/text events. The final state publishes once after caret recovery;
  realized coverage keeps Table Tools selected after row insertion and collapses it after true table deletion. Stale
  document/object targets are rejected.
- **Application impact:** the flash makes Table Tools appear unreliable and can disorient keyboard users even when the
  requested command succeeds. An unconditional always-visible tab would instead expose stale or unsafe commands.
- **Smallest possible library direction:** none at present. Treat RibbonKit's fallback selection as correct when the
  host actually collapses the selected contextual tab. Consider runtime work only if a minimal consumer shows an
  unwanted fallback while contextual visibility remains continuously true, or if multiple consumers require a
  narrowly defined contextual-selection transaction API.
- **Evidence still required:** repeat ordinary buttons, dropdown items, popup focus, row/column replacement,
  merge/split, table deletion, undo/redo, reduced motion and keyboard/KeyTip invocation in the real Writer window.
  Repeat in a minimal Showcase contextual tab before proposing any `src/RibbonKit/**` change.

### RKWF-015 — Native Undo can restore a loaded image container without its image child

- **First seen / packet:** 2026-08-28 user follow-up during Writer W3-E1.
- **Status:** Corrected in Writer; ordinary WPF embedded-content behaviour, not a RibbonKit runtime defect.
- **Consumer goal:** removing a packaged picture through the structured context menu must be one reversible operation,
  even when the document was opened from Backstage Recent rather than created during the current editor session.
- **Reproduction and evidence:** with the first Recent `.rkw` document, removing the first-line picture paused briefly
  and native Undo appeared to do nothing. A focused reproduction showed that WPF did restore the
  `InlineUIContainer`, but its child was an empty `Grid` instead of the deserialized `Image`. Assigning a new direct
  child then cleared WPF's redo chain; leaving the repaired `Grid` in the live document caused XamlPackage save to
  replace the picture with a space.
- **Current app-owned correction:** Writer captures a bounded inert image snapshot before removal, repairs WPF's empty
  placeholder after Undo, and supplies the matching redo only after native redo units have been traversed. W3-E2a reuses
  that bounded snapshot/placeholder strategy for its one-unit image-container replacement on resize, preserving exact
  opening and committed dimensions without serializing interaction state. Unmodified
  Delete/Backspace route through the same transaction only for an exact picture selection or the matching directional
  caret boundary; ordinary text deletion remains native. Persistence and preview normalize an isolated clone back to
  the approved direct-image graph, leaving the live undo history untouched. Realized coverage opens through Recent,
  removes through the actual menu and both keyboard keys, traverses older text history, redoes/removes again, previews,
  and saves/reopens the restored picture.
- **Application impact:** without the bridge, Undo consumes a unit without a visual restoration and a naïve repair can
  either destroy Redo or silently omit the picture on save. The correction stays inside Writer editing and snapshot
  services and does not broaden the data-only `.rkw` allowlist.
- **Smallest possible library direction:** none. The affected object graph is owned by the native WPF `RichTextBox` and
  Writer persistence, not a RibbonKit control or service.
- **Evidence still required:** repeat the exact removal and direct-resize workflows in the visible Writer window and
  retain multi-picture, nested-span/table-cell, repeated undo/redo and save/reopen regression coverage as W3-E grows.

### RKWF-016 — Revealing a contextual tab can leave the active-tab marker at its previous coordinate

- **First seen / packet:** 2026-08-29, Writer W3-E2a live Picture Tools follow-up.
- **Status:** Corrected in RibbonKit on 2026-08-29; user-verified through every theme/light-dark variant and the
  100-200% DPI matrix by 2026-08-30. RTL, reduced motion, merging and customization reorder remain pending.
- **Consumer goal:** revealing or hiding a contextual tab must keep the selected-tab marker aligned with the selected
  normal tab, including when the contextual tab occupies an earlier collection position.
- **Reproduction and evidence:** with Page selected, Picture Tools was located between Insert and Page in Writer's tab
  collection. Selecting a picture revealed Picture Tools and shifted Page to the right. Page content and selected text
  remained active, but the selection underline stayed at Page's former coordinate beneath Picture Tools. The user
  supplied a live screenshot of this contradictory state.
- **Friction:** the ribbon's selected content and header state can be correct while its animated active marker points
  at a different tab after contextual visibility changes the positions of later headers. This makes an ordinary
  contextual-tab insertion look like a selection change even though no selection change occurred.
- **Current resolution:** `RibbonTabControl` observes tab collection and visibility changes and coalesces a
  post-layout refresh of both selection visuals. Writer keeps Table Tools and Picture Tools in their authored middle
  positions; its startup reordering workaround has been removed.
- **Application impact:** without the ordering constraint, users can see Page content while the active marker appears
  under Picture Tools, obscuring which commands are currently displayed and undermining contextual-tab trust.
- **Implemented library direction:** the realized regression keeps normal tabs on both sides of a hidden contextual
  tab, reveals it while the right-side normal tab stays selected, and requires the shared marker to move to the new
  header coordinate. The existing Showcase contextual-tab surface exercises the same authored ordering; the control
  refreshes the sliding underline and connected-tab notch after collection or visibility layout settles.
- **Evidence still required:** the focused normal/contextual/normal realized case passes, and the user has accepted
  visibility toggles through every Office generation/light-dark variant and 100-200% DPI. Repeat RTL and reduced
  motion, then cover tab merging and customization reorder.

### RKWF-017 — Empty trailing table cells can briefly lack usable text geometry after replacement

- **First seen / packet:** 2026-08-29, Writer W3-E2b live direct-table-resize follow-up.
- **Status:** Corrected in Writer; native WPF document-layout timing, not a confirmed RibbonKit runtime defect.
- **Consumer goal:** direct-selection chrome must continue to cover every logical table row and column immediately
  after a resize commits through cloned native-table replacement.
- **Reproduction and evidence:** before resize, all empty cells produced enough character geometry for the Writer
  adorner. After the one-Undo replacement, an empty trailing cell could temporarily produce no usable character
  rectangle while the rendered table itself still contained the column. The adorner derived its logical column count
  from only the realized subset and visibly ended one cell before the table edge.
- **Current app-owned correction:** Writer derives logical row/column counts from the native table grid and uses live
  character rectangles only for coordinates. Explicit `TableColumn.Width` metadata and bounded interpolation cover a
  temporarily unrealized edge; subsequent layout passes naturally replace the fallback with realized geometry. When
  explicit widths are projected, Writer also adds one native `Table.CellSpacing` contribution per column: WPF renders
  that spacing outside `TableColumn.Width`, so omitting it creates cumulative handle drift after resize.
- **Application impact:** without the structural count, selection and resize chrome becomes misleading precisely after
  the user completes a resize, and the bottom-right handle can target the wrong table extent.
- **Smallest possible library direction:** none. The chrome and geometry resolver are Writer-owned consumers of native
  `FlowDocument` tables; no RibbonKit control participates in the document layout.
- **Evidence still required:** repeat multi-row, 1-8 column, spanned-cell, zoom/DPI, RTL, undo/redo and save/reopen
  cases in the visible Writer window.

### RKWF-018 — Live DPI transition can stale InRibbonGallery scrolling and side-button hit geometry

- **First seen / packet:** 2026-08-30, Showcase Styles gallery after the RKWF-005/013/016 live matrix.
- **Status:** Corrected and user-verified on 2026-08-30. Natural width, three-column layout, popup/button separation,
  button behavior, the `-8` vertical placement and the first post-DPI open/close redraw all passed live. The default
  template keeps separate permanent strip/popup scrollers and moves only the items presenter across hosts.
- **Consumer goal:** after moving an active per-monitor-v2 window to a different DPI, the closed gallery strip must
  remain populated and the first click on each side button must invoke that exact button; opening afterward must show
  a freshly measured popup at its first item.
- **Reproduction and evidence:** after a live transition, especially above 150%, the lower expand button sometimes
  behaved like scroll-down and the strip could page to a completely empty viewport. Opening Snipping Tool caused a
  later activation/layout/render pass that immediately repaired the gallery. The supplied screenshot captured the
  empty Styles strip with all three side glyphs still visible. The popup was closed during the DPI transition and was
  opened only afterward. Two follow-up recordings then showed the initial correction was insufficient: at 200% the
  transparent popup-window/shadow rectangle visibly covered the left portion of the side-button column, while a
  normal 125% run still produced partial button response and an empty strip. The blank state also reproduced on a
  downward 150%-to-125% transition. Constraining the popup window to the narrow content-column width then stopped the
  HWND overlap, but the next live check exposed a deterministic post-DPI clip and forced a gallery authored for three
  columns to wrap after two items. Natural-width edge placement then passed live for geometry and interaction, leaving
  one purely visual symptom: the closed strip could retain a blank/stale frame until another window action repainted it.
  A generation-guarded final layout plus explicit realized-item invalidation still did not eliminate that live state.
  The first open/close cycle after a DPI transition can fail while the immediate second cycle works, meaning the first
  cross-HWND layout is itself priming or repairing state used by the second.
- **Friction:** `InRibbonGallery` shares one `ScrollViewer` and items presenter between its one-row strip and popup.
  Popup opening already refreshed the re-homed viewport once, but the closed strip did not observe its owner window's
  DPI transition. Its fixed-height side-button stack also left the inset visual border as the only transparent hit
  surface, creating dead edge gutters at fractional device scales. More importantly, the expanded surface is a
  separate transparent Popup HWND: its margin/shadow accommodation extended that rectangular input window over the
  main-window side buttons, and transparent pixels in one HWND cannot pass clicks through to controls in another.
  Closing after browsing a later popup page also re-homed the presenter before the old vertical offset was committed
  back to a valid strip offset, allowing a blank frame. WPF does hide its separate popup window synchronously and may
  keep it alive until asynchronous destruction (or cancel that destruction on a quick reopen), but the captured tiny
  selected-border corner shows this failure is not merely a hidden popup or cached bitmap: the live item visual has
  returned and is being clipped by stale geometry carried with the re-homed scroller.
- **Current partial resolution:** the gallery tracks its owning Window while loaded, stops stale scroll animations and resets
  to a safe offset synchronously on DPI change, then performs generation-guarded Loaded and Render layout passes.
  A closed strip restores its selected row after the new metrics settle; a subsequently opened popup starts at offset
  zero with a fresh viewport. The three buttons now occupy equal layout-rounded Grid rows whose full roots are
  transparent hit surfaces; inset chrome no longer defines input geometry. Scroll commands refresh metrics before
  calculating their next page. The Popup remains anchored to the content host but no longer inherits its narrow fixed
  width. A custom placement uses the popup's natural measured size, aligns its outer edge with the content/button
  boundary, and expands away from the buttons: leftward in LTR and rightward in RTL. Thus three authored columns remain
  on one row without putting any part of the popup HWND over the buttons. On close it clears the popup-page offset
  before re-homing, then synchronously lays out the returned strip at offset zero before any deferred selected-row
  reveal. The attempted final Render/ContextIdle invalidation proved an additional render was requested, but live
  evidence showed that repaint still consumed the wrong clip, so that workaround was removed. The default template
  now leaves `PART_ScrollViewer` permanently under `PART_ContentHost` in the main HWND and
  `PART_PopupScrollViewer` permanently under `PART_PopupHost` in the Popup HWND. Only `PART_ItemsPresenter` moves.
  Each viewport therefore retains its own DPI, extent and clip state. The visible card keeps its reduced horizontal
  gap, and the accepted `VerticalOffset=-8` compensates for the popup child's existing top margin/target inset.
  Horizontal placement mirrors in RTL.
- **Application impact:** popup geometry/button input and the post-DPI first-open redraw are corrected.
- **Implemented and next library direction:** the owner subscription survives transient collapsed-group unload/reload
  re-homing and detaches after a genuine removal. Host-specific scrollers are now the default-template boundary; a
  compatibility fallback retains the original whole-content re-home for custom templates that do not yet expose the
  two new optional parts. Focused gallery regressions pass **8/8**, including the invariant that neither scroller
  changes host during a downward post-DPI open/close cycle. The zero-warning/error solution build, RibbonKit
  **371/371**, visual **1/1**, and Writer **439/439** pass. The future themed-scrollbar slice remains separate.
- **Evidence still required:** none for Packet 1. The user accepted the previously failing first-open/close sequence
  after mixed-DPI changes, including the 150%-to-125% and 200% cases; preserve the regression matrix for future work.

### RKWF-019 — Gallery overflow falls back to OS-native scrollbar chrome

- **First seen / packet:** 2026-08-30, Showcase Styles gallery after RKWF-018; Packet 2.
- **Status:** Core gallery geometry and the scoped Customize Ribbon scrollbar pilot are live-accepted. QAT list-box
  scrollbars are implemented and await live confirmation; the final Office 2013/2019 square-token adjustment and
  modern dialog-action normal-state follow-up also await visual comparison.
- **Consumer goal:** gallery overflow should remain visually continuous with the active RibbonKit Office-generation
  palette while retaining ordinary WPF range, keyboard, mouse, wheel and accessibility semantics.
- **Reproduction and evidence:** opening an `InRibbonGallery` with enough rows to exceed its 340-DIP popup cap realized
  the operating system's default vertical scrollbar inside an otherwise RibbonKit-themed popup.
- **Friction:** the native chrome changed independently of RibbonKit theme and dark-mode switches. Replacing scrolling
  logic inside each gallery would risk the just-accepted DPI/viewport correction and duplicate mature WPF behavior.
- **Current resolution:** the public lookless `RibbonScrollBar : ScrollBar` preserves native range commands, both
  orientations, RTL mirroring and the inherited RangeValue automation peer. One shared template set supplies vector
  arrows, page tracks, thumbs, pressed/hover states and a High Contrast fallback. Eight palette brushes and six
  metrics are present in every light/dark Office 2007-2024 token dictionary. `RibbonGallery` and the popup-only
  `InRibbonGallery` viewport apply a same-file adapter that reuses those shared templates on the native `ScrollBar`
  instances generated by their `ScrollViewer`s, without replacing either viewport or moving it across HWNDs. Ribbon
  Lab includes standalone vertical/horizontal examples, and its Accent gallery has enough swatches to force the real
  popup overflow path. The dedicated Localization/RTL lab includes both orientations under its live mirroring toggle.
  A later adoption pilot first applied the same shared template to the generated scrollbars in `RibbonCustomizePage`'s
  available-command list and ribbon-structure tree. The user accepted that page-local scrollbar treatment as visually
  successful. Follow-up then put the same implicit adapter inside `RibbonQuickAccessPage`, covering its available and
  current lists while app-owned options pages and optional Backstage content remain unchanged. The Button-targeted counterpart was then
  promoted into `OptionsDialogActionButtonStyle`, so QAT customization, Customize Ribbon, main/edit Cancel and every
  compact action share the scrollbar-derived interaction template and radius token. Primary OK keeps its colored gel
  override and caption Close keeps its Windows-red hover; app-owned page content remains opt-in. Because transparent
  modern scrollbar-button normal tokens made full action buttons blend into the form, the shared action style now uses
  dedicated dialog normal-background/border tokens: legacy themes keep the same gel, while 2013/2019/2024 use visible
  flat fills and one-DIP outlines without changing their scrollbar arrows.
- **Preliminary visual correction:** the first live pass found the 12-15 DIP rails squeezed, arrow rows sized only to
  their 7-by-4 vector content, and the vertical thumb pill compressed. The rail is now 16 DIP for Office 2013-2024
  and 18 DIP for Office 2007/2010, both line buttons reserve a full square, and orientation-specific padding moved
  from `Thumb.Margin` into the thumb template's inner border. That preserves `Track`'s calculated thumb geometry.
  Public `ButtonCornerRadius`, `ThumbCornerRadius`, and `RailCornerRadius` properties (also usable as attached
  properties on generated native scrollbars) independently allow square or rounded chrome. Office 2007/2010 thumbs use outlined
  multi-stop glass/gel gradients; later generations retain their flatter palettes.
- **Second preliminary visual correction:** the first inner-inset correction still removed one DIP from each end of
  a short vertical thumb, which left its rounded outline looking like two disconnected caps. Removing the longitudinal
  inset preserved the Thumb's desired size, but the next live capture proved that only a small cap was still rendered.
  The
  Showcase's labeled vertical sample returned to a 56-DIP height so its bottom button stays above the group footer;
  rail width remains 16/18 DIP. `RailCornerRadius` is now an independent public/attached property with theme defaults
  matching the button radius, preventing the track background's sharp corners from showing behind rounded hover
  chrome. Office 2007/2010 line buttons also receive a visible outlined gradient in their normal state, while modern
  themes retain the quieter transparent default.
- **Track-level correction:** the compact 125% geometry probe found a 22.4-DIP Thumb arranged into an 8-DIP native
  `Track` slot with an 8-DIP layout clip. WPF's proportional-scrollbar path derives its minimum from half of the local
  system scrollbar-button resource, not `Thumb.MinHeight`, so the earlier minimum enlarged only the child behind the
  clip. The internal `RibbonScrollBarTrack` now maps the active theme's minimum-thumb token into those two Track-local
  system resource keys. Native Track therefore calculates the correct slot, value density, drag area and page-button
  lengths itself. No process-wide system resource is changed. The Thumb no longer has a conflicting minimum or any
  inset: its Pill fills the complete vertical rail width or horizontal rail height. Compact Office 2010 and Office
  2024 realized cases pin equal layout-slot/Pill geometry and the absence of a layout clip. After live acceptance of
  that geometry, the Office 2013 and Office 2019 light/dark button, thumb and rail radius tokens were all set to zero;
  four focused theme cases prevent either flat generation from regaining rounded scrollbar chrome.
- **Application impact:** overflow chrome now follows RibbonKit rather than the host OS theme; scrolling behavior and
  RKWF-018's two-scroller ownership boundary remain unchanged.
- **Evidence still required:** inspect standalone vertical/horizontal controls and overflowing galleries in every
  Office generation/light-dark variant, then check pointer arrows, track paging, thumb dragging, wheel/keyboard,
  100-200% DPI, RTL horizontal direction and High Contrast. The user accepted the corrected full-width thumb and
  compact vertical behavior and the Customize Ribbon scrollbar pilot; the final Office 2013/2019 square-token visual
  recheck remains. Compare the dialog-wide ordinary-button treatment in all modern light/dark themes. Automated
  focused coverage is **21/21**, including realized overflow/scrolling in both lists on both built-in pages, Office 2010 gel/radius chrome, modern visible normal chrome, both built-in
  pages and the OK/Cancel/Close exceptions. The reviewed Office 2024 RTL QAT approval changed only the four expected
  buttons and then only its available-list scrollbar; the zero-warning Release build, RibbonKit **392/392**, visual
  **1/1**, and Writer **439/439** pass.

### RKWF-020 — FlowDocument table selection endpoints can resolve to an adjacent cell

- **First seen / packet:** 2026-08-30, Writer W3-E2b live table-selection follow-up.
- **Status:** Corrected in Writer; native WPF text-pointer affinity, not a RibbonKit runtime defect.
- **Consumer goal:** whole-table selection, rectangular merge and context-menu invocation must address the same cells
  regardless of drag direction, without absorbing the cell beyond an exclusive selection end.
- **Reproduction and evidence:** the table selection grip left the rightmost cell unhighlighted; merging a horizontal
  selection also merged its next cell; and right-click after right-to-left or bottom-to-top selection collapsed the
  range. `TextSelection.End` is exclusive, and at a table boundary its parent/ordinary lookup can identify the next
  cell even though that cell is not part of the visible selection.
- **Current app-owned correction:** Writer orders endpoints and compares a non-empty end with the containing cell's
  first real insertion position. When WPF places the exclusive end inside the next cell's structural wrappers but no
  later than that insertion position, Writer uses the preceding physical cell. That normalized rectangle is carried
  through ribbon/context-menu execution. Whole-table selection still ends at the final cell's `ElementEnd`, and
  normalized cell containment protects right-click selection. Partial-span merge rejection remains unchanged.
- **Application impact:** selection chrome, merge scope and context actions now share one deterministic structural
  range for forward and reverse drags.
- **Smallest possible library direction:** none. RibbonKit only hosts the commands; native `RichTextBox`, Writer's
  table service and its context-menu target resolver own these document pointers.
- **Evidence still required:** repeat forward/reverse row, column and rectangle selections with empty/formatted cells,
  spans, RTL, right-click menu execution and undo/redo in the visible Writer window.

### RKWF-021 — Table insertion rectangles move with cell text alignment

- **First seen / packet:** 2026-08-30, Writer W3-E2b live alignment follow-up.
- **Status:** Corrected in Writer; native WPF document geometry, not a RibbonKit runtime defect.
- **Consumer goal:** table resize chrome must remain on native grid edges when cell text alignment changes, and cell
  alignment commands must format every cell in the selected rectangle.
- **Reproduction and evidence:** centering/right-aligning the first cell moved its empty insertion rectangle and the
  Writer adorner's right edge; selecting multiple cells and invoking horizontal or vertical alignment changed only the
  caret cell because both handlers used `MutateCurrentCell`.
- **Current app-owned correction:** anchor the adorner at `Table.ElementStart` plus `CellSpacing`, derive its perimeter
  from resolved grid boundaries, and route both alignment menus through one normalized `WriterTableRange` mutation.
- **Smallest possible library direction:** none. RibbonKit raises the menu click; Writer owns native `TableCell`
  formatting and FlowDocument geometry projection.
- **Evidence still required:** visible left/center/right and top/center/bottom checks over single cells, columns,
  rectangles, spans, RTL and undo/redo.

## 5. Closed observations

None yet.
