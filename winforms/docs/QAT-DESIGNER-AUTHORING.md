# Quick Access Toolbar in Edit Ribbon

Status, **2026-11-01: accepted by the user for the reviewed QAT authoring workflow, source selection and component renaming**.

The user approved a QAT branch inside the existing Edit Ribbon dialog, including
commands that need no tab/group slot. This is designer tooling over existing
runtime ownership, catalog and QAT projection APIs. No runtime API, painting,
animation or customization-persistence contract changes are introduced.

## Authoring workflow

Open **Edit Ribbon → Quick Access Toolbar**. Its protected branches are:

- **Toolbar entries**: ordered QAT presentations. Add an existing command/group,
  change a compatible reference's source, edit its caption/KeyTip and availability,
  move it up/down or remove it from QAT. **Edit source** selects its command
  declaration for normal command properties and icon editing.
- **Commands without a tab**: application command declarations. **New QAT-only
  button/toggle** creates a sited command and a referencing toolbar entry, then
  selects the command for caption/icon/state editing. **Delete command** is
  disabled while toolbar references use that command or its descendants. Remove
  those entries first to delete the command explicitly.

Select the QAT branch or Toolbar entries branch to edit **Visible** and
**Position** (TabRow, BelowRibbon, TitleBar). Placement retains the existing
RibbonForm caption requirement and TabRow fallback. Protected branches cannot be
removed or reordered; tabs retain their existing ordering independently of QAT.

The source picker searches current captions/categories. Existing buttons,
toggles, checkbox/radio commands, dropdown/split/gallery commands, ComboBoxes and
whole groups reuse their existing matching projection classes. TextBox commands
are not QAT candidates. Groups are offered as QAT references only. This increment
creates new top-level QAT-only **buttons and toggles**; existing built-in catalog
command structures retain their editable properties/children.

After OK, the chosen command or reference is selected through the designer
selection service after the modal property editor returns. Wire Click and other
handlers in Visual Studio's ordinary Properties/Events view. The editor creates
ordinary components and does not generate application handlers.

Visible QAT entries are selectable on the Form designer using the existing
measured/clipped targets. A click selects the toolbar entry and outlines it;
**Select source command** in its designer actions selects the real command for
Properties/Events. Catalog command selection keeps surface picking active.
Commands without a visible QAT entry remain reachable through **Edit Ribbon →
Quick Access Toolbar → Commands without a tab**, selecting the command and OK.
The branch belongs to Edit Ribbon; it is not a separate Form-designer tree.

For naming, **Appearance → Text** changes the displayed caption, while
**Design → (Name)** changes the programmatic component name inside Edit Ribbon
or Edit Backstage. The same name is available in Visual Studio's Properties
window. Stable **Id** is independent of both names. The
[naming and designer-picking follow-up](DESIGNER-AUTHORING.md#programmatic-names-and-qat-selection--2026-11-01)
owns its current contract and focused/live evidence.

## Ownership and transaction contract

New commands live in `ribbon.CommandCatalog.Commands`; QAT presentations borrow
them through the existing Source properties. Commands need no authored ribbon
slot. Removing a presentation preserves its source, handlers, bindings, data,
images and catalog discoverability. Existing directly owned QAT buttons/toggles
keep their original model/ownership while pinned; removing their toolbar row
moves that same model into the catalog. Removing an unused command declaration
is the separate destructive command-deletion operation.

The detached protocol carries command declarations, QAT presentations and toolbar
settings. Reference rows and command sources have distinct stable identities.
Existing source/component objects are retained; source references are never
implemented as copied application commands. QAT-only edits do not rebuild tabs.
Catalog galleries keep their selected item during auxiliary collection changes.

Cancel and unchanged OK create no live changes or undo entry. One OK transaction
covers tabs, catalog/QAT and root settings together. Nested catalog design sites
make the existing content model addressable as `ribbon.CommandCatalog`; its
source components are included in the ribbon designer's associated components.
The host retains ordinary component creation/destruction and code serialization.

Preflight validates fresh schemas, names/IDs, source availability/type, immutable
existing component identities, source-deletion dependencies and placement values.
External QAT/catalog changes invalidate an open draft. Client-supplied source
metadata cannot authorize an unknown source. A ribbon source and its QAT entries
can be removed together in one valid draft.

Generated code uses the existing runtime declarations, for example:

```csharp
qatUndo = new RibbonKit.WinForms.RibbonButtonItem();
qatUndo.Id = "app.undo";
qatUndo.Text = "Undo";
qatUndoReference = new RibbonKit.WinForms.RibbonQuickAccessReference();
qatUndoReference.Id = "qat.undo";
qatUndoReference.Source = qatUndo;
ribbon.CommandCatalog.Commands.Add(qatUndo);
ribbon.QuickAccessItems.Add(qatUndoReference);
```

The theme preview draws detached tab/catalog/QAT declarations and honors toolbar
visibility/placement. No application command is invoked and no native application
page is initialized, reparented or captured. Existing references to sources
outside the editable declarations are retained; their preview can use an inert
unavailable caption placeholder rather than reproducing application-only state.
The editor retains its existing built-in-component authoring boundary.

## Focused verification

Evidence is under `TestResults/winforms-designer-qat/`:

- **79 distinct focused designer cases pass across retained bounded runs**. These
  comprise 32 new QAT cases and 47 affected existing authoring, editor UX,
  selection, root settings/Undo and preview cases. No full/native motion suite ran.
- New cases cover detached creation/Cancel, all nine supported source kinds,
  source/entry identity, handlers, pin/unpin/restore, legacy owned-command
  retention, guarded deletion, reorder/aliases/settings, invalid/stale rejection,
  combined framework Undo/Redo, reference retarget/removal Undo, normal Properties
  selection, CodeDOM named-source/catalog/QAT serialization, inert drawing and
  four synthetic scale/resize/source-search cases.
- `qat-editor-focused.trx` retains the initial 21 passes; corrected UI visibility,
  stock-icon name and serialization-context assertions pass in
  `qat-editor-corrected.trx`. `qat-editor-reference-preview.trx` records reference
  Undo and inert preview plus the changed QAT action layouts. The original tab
  root count assertions now count declarations separately from the new protected
  branch; last-tab removal still selects the Ribbon root.
- `qat-editor-baseline.trx`, `qat-editor-corrected.trx` and
  `qat-editor-affected-designer.trx` retain the affected existing passes. Final
  accepted-command selection passes in `qat-editor-selection-final.trx`; the
  editor explicitly updates detached selection even before a TreeView handle
  exists. These are retained passing scopes, not a single aggregate full run.
- The QAT branch, searchable picker and below-ribbon static preview images were
  inspected. Synthetic scale checks cover 100/125/150/200% at two dialog sizes;
  they do not establish physical or mixed-monitor DPI acceptance.
- Normal Debug runtime/server/client/sample builds and local package creation
  pass with zero warnings/errors. The permanent package-only Designer Sample was
  refreshed in place; authored Forms/resources were preserved. No new review
  solution, runtime dependency, commit or publication was created.

Actual VS hosted interaction, save/reopen, generated-code compilation after a
user edit, runtime command invocation and physical-DPI review remain user gates.
Framework Undo/Redo tests are separate from actual Visual Studio acceptance.

## Review in the permanent sample

1. Reopen the Designer Sample solution to load the refreshed package. In View
   Designer, open Edit Ribbon and select Quick Access Toolbar.
2. Add the existing Apply command and a new QAT-only button/toggle. Edit its
   caption/icon, reference KeyTip/alias, order, toolbar visibility and placement.
   Cancel once and confirm that the Form remains unchanged.
3. Repeat and accept. Use one designer-surface Undo/Redo, then inspect the selected
   command in Properties and wire a Click handler through the ordinary Events UI.
4. Save/reopen, build and run. Confirm the command appears only in its authored
   QAT slot and the handler executes. Remove the QAT entry, accept, and confirm
   its command remains in Commands without a tab so it can be added again.
5. Remove the entry before explicitly deleting its unused command. Confirm that
   existing ribbon commands, bindings, images and event hookups remain intact.
