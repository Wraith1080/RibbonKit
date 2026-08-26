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
- **Status:** Open additive-control candidate; no RibbonKit runtime work approved in this packet.
- **Consumer goal:** visually partition related command clusters inside one `RibbonGroup` without creating
  fake groups, hard-coded borders or layout-only command items.
- **Reproduction and evidence:** the accepted Writer Home surface needs lighter divisions within command-dense
  groups, but RibbonKit exposes only whole-group boundary chrome, menu separators and the application-menu
  separator. None participates as an adaptive item inside a ribbon group.
- **Friction:** an app-owned `Border` or `Separator` would need to reproduce theme tokens, large/medium/small
  measurement, collapsed-group behavior, RTL placement, visibility and automation semantics that belong to
  the ribbon item system.
- **Current app-owned workaround:** rely on spacing and whole-group boundaries; do not introduce a one-theme
  visual approximation into Writer.
- **Application impact:** dense groups are harder to scan, while consumer-created dividers risk inconsistent
  adaptive layout and theme behavior across Office generations.
- **Smallest possible library direction:** investigate a lookless, non-command `RibbonGroupSeparator` control
  with theme-owned chrome and explicit adaptive/RTL/collapsed behavior. It should remain absent from KeyTips,
  QAT projection, customization command lists and UI Automation control content unless accessibility review
  identifies a useful semantic role.
- **Evidence still required:** a focused consumer prototype in Writer or Showcase covering horizontal group
  layouts, all ribbon sizes, collapsed flyouts, all themes, RTL, high DPI, customization and automation trees.

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
- **Status:** Open additive runtime/documentation candidate; app-owned workaround accepted for Writer.
- **Consumer goal:** restore editor focus after any Backstage dismissal without wiring every File action or guessing the
  exit-animation duration.
- **Reproduction and evidence:** `Ribbon.IsBackstageOpen` is the only general observable state, but it changes to
  `false` when closing starts. The Backstage adorner and any Classic orb proxy remain until an internal
  `RibbonMotion.PlayClose` completion callback removes them. `Backstage.BackRequested` covers only a request from the
  Back button/Escape and also precedes teardown; no public Ribbon lifecycle event reports that animated teardown has
  completed. `IsBackstageOpen` additionally represents `RibbonApplicationMenu`, whose popup lifecycle is separate.
- **Friction:** a host can observe logical close initiation centrally, but cannot reliably distinguish it from visual
  close completion across File-toggle, Back/Escape, KeyTip, programmatic, Classic2010 and Classic2007 paths.
- **Current app-owned workaround:** Writer observes the single `IsBackstageOpen` dependency property, marks a pending
  editor-focus return when it becomes false, defers once so a File command can enter its busy state, and completes the
  return only after dialogs/commands finish. Preview, hidden/closing-window and intended-focus guards remain app-owned.
- **Application impact:** the workaround removes per-command focus wiring, but it cannot use the exact adorner-removal
  boundary and must continue coordinating its own asynchronous command state.
- **Smallest possible library direction:** add a non-cancellable CLR `Ribbon.BackstageClosed` event raised once after
  the exit animation completes, the Backstage adorner/proxy is removed, placement state is restored and the close was
  not cancelled by a reopen. Do not raise it for `RibbonApplicationMenu` and do not make Ribbon own a focus target.
  A non-cancellable `BackstageClosing` event may be considered separately for symmetry, but is too early for focus
  restoration.
- **Evidence still required:** focused realized-STA tests for File toggle, Back/Escape, KeyTips, programmatic close,
  reduced motion, reopen-during-close, Classic2010 and Classic2007; define unload/deactivation behavior explicitly and
  prove that application-menu dismissal does not raise the Backstage event. Add Showcase usage and public API/XML
  documentation in any separately approved runtime packet.

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
- **Status:** App-owned workaround accepted; open RibbonKit investigation with no runtime change approved.
- **Consumer goal:** an expanded `InRibbonGallery` should paint an opaque theme/high-contrast popup surface before its
  shared presenter is re-homed, so underlying ribbon commands never show through the tile grid.
- **Reproduction and evidence:** in the real 125%-scale Writer window, `PART_PopupHost.Background` remained null even
  though the shared template declares `RibbonKit.Brushes.Ribbon.ContentBackground`. The 8-column table grid rendered,
  but the File/Home ribbon content was visible between tiles. Direct inspection confirmed the null background; the
  final standard and RTL captures became opaque after the app supplied the resolved brush.
- **Friction:** the consumer cannot set the popup host through public `InRibbonGallery` API. It must apply the template,
  find the declared part by name and assign a brush after initialization.
- **Current app-owned workaround:** Writer assigns the current ribbon-content brush directly to `PART_PopupHost`, or
  `SystemColors.WindowBrush` in High Contrast. A real-tree assertion requires a non-null surface, while mouse/keyboard
  and popup re-homing continue through the unchanged RibbonKit control.
- **Application impact:** without the workaround, the table picker is visually ambiguous and can expose unrelated
  controls beneath its choices; with it, standard and RTL popup captures are opaque and input behavior is unchanged.
- **Smallest possible library direction:** reproduce the unresolved DynamicResource in a minimal consumer, then make
  the popup host resolve/reapply the theme-owned background when its separate HWND opens. Preserve every theme,
  High Contrast, re-homing, reduced motion and runtime resource refresh; do not special-case Writer.
- **Evidence still required:** a focused Showcase/runtime reproduction across Office generations, light/dark,
  standard/collapsed groups, RTL, 100-200% DPI and High Contrast without the Writer override.

## 5. Closed observations

None yet.
