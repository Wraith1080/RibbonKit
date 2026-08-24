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
  leaf controls in their groups. The same controls retain app-assigned automation metadata, expose working
  in-process RibbonKit peers, and pass actual mouse and KeyTip activation.
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

## 5. Closed observations

None yet.
