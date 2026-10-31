# Native designer authoring checkpoint

Status, **2026-10-31: bounded custom designer increment accepted by the user**. Baseline: `516d121` on
`codex/Ribbonkit-Winform`. This checkpoint follows the disabled/state appearance
increment and preserves the accepted Backstage behavior.

Latest increment, **2026-10-31: surface preview and selection implemented;
actual-VS interaction, visual and DPI review pending**. See the bounded contract,
evidence and review steps below. Earlier accepted scopes retain their acceptance.

## Bounded contract

Use the existing runtime component/collection APIs. User direction, 2026-10-31:
provide one **Edit Ribbon** form for tabs, groups and commands instead of a stack
of collection dialogs. The VS-side form edits a detached draft; the design server
keeps existing real components and applies OK through one designer transaction.
Cancel makes no live changes. Keep the prior collection-editor types compatible.
Prove the workflow in actual Visual Studio and a package-only Form: author tabs,
groups and heterogeneous commands; edit/reorder/remove; Undo/Redo; save and reopen;
inspect generated C#/resx; build/run with events, image resources and two-way
binding retained. Fix concrete designer gaps exposed by that workflow.

The installed IDE is Visual Studio Community 2026 18.10.2; the native projects
target .NET 10 using SDK 10.0.401 and Designer.SDK 1.6.0. Record actual designer
server loading separately from compile/serialization tests. Use normal Debug
outputs, an isolated local package feed/cache and no publication.

`Professional Office Style Commandbar - Winform` is a read-only reference for
package discovery and designer change tracking. Its custom client/protocol editor
architecture supplies a reference for the requested custom modal editor. The
client and protocol are design-only; the runtime does not gain a Designer SDK
dependency or a new theme/model coordinator.

## Theme selection boundary

User direction, 2026-10-31: general theme authoring is outside the planned scope.
A later increment should offer selection of library-provided themes with a
designer preview. It should not expose an arbitrary theme-color/settings editor
or introduce a mutable theme owner. `Theme` remains an immutable runtime value
and is currently hidden from the designer. Selecting a theme must eventually
round-trip safely, support Undo/Redo and leave shared theme instances untouched.
Propose any required public selection/serialization API before implementation,
with compatible defaults. This does not defer per-tab contextual color authoring.

## Next designer increments

User direction, 2026-10-31, refines the proposed order:

1. **Editor UX and DPI.** Present the revised layout before implementation. Use
   contextual Add menus, consistent action grouping and a responsive layout.
   Creating a contextual tab and editing its header color are core authoring
   tasks, with explicit affordances instead of requiring discovery in a generic
   property grid. Reuse `IsContextual` and `ContextualColor` first. The current
   renderer uses this color for contextual header text and its selected indicator;
   a filled header background requires an explicit visual/state contract before
   changing runtime APIs or rendering. Preserve existing defaults. Verify actual
   VS-hosted scaling; font autoscaling alone is not physical-DPI acceptance.
2. **Preview and selection.** Reveal another tab's content, including contextual
   tabs, and synchronize tree/surface selection with Properties where feasible.
   Preview state stays separate from authored visibility and runtime startup
   state. Selection outlines are useful; direct drag/drop and inline editing are
   optional refinements. A smart-tag popup providing the equivalent contextual
   add/edit/remove/reorder actions is an accepted alternative when advanced
   surface manipulation is too complex. Preserve designer transactions and do
   not invoke application commands during preview. The first bounded implementation
   is recorded under **Surface preview and selection** below.
3. **Backstage authoring.** Add a dedicated navigation/page/command editor and
   preview. Define how separately designed native page controls are assigned and
   authored before implementation; retain application ownership and the accepted
   Backstage runtime behavior. Direct child-control editing on a Backstage canvas
   is additional designer-hosting scope, not implied by navigation editing.
4. **Theme selection and preview.** Offer supported library themes under the
   boundary above, using the improved designer workflow. No general theme
   authoring is planned. Advanced surface editing is not a prerequisite for this
   or Backstage authoring.

Live visual inspection remains with the user. These are planned increments;
acceptance and automated evidence below apply to the completed functional slice.

## Editor UX and DPI increment — 2026-10-31

Status: **implemented; revised layout and actual-VS DPI review pending**. The
earlier functional acceptance remains recorded below. The layout proposal was
presented before implementation: one Add menu, grouped ordering actions and a
separate Remove action, structure/properties split, dedicated Tab header controls
and a consistent OK/Cancel footer.

The design client now provides contextual Add menus in the toolbar and tree
popup. They offer Tab/Contextual tab/Group and the command types supported at the
selected location. A contextual tab is created through the existing
`IsContextual` property, with a unique component name and stable ID. The Tab header
section exposes the contextual toggle, a native color picker and Use theme accent
(`Color.Empty`). Its label explicitly describes header text/indicator tint;
runtime rendering and filled backgrounds are unchanged. Tree labels distinguish
contextual tabs. Remove keeps a nearby selection; ordering actions also work from
the tree popup and Ctrl+Up/Down. Delete applies only while the tree has focus.

Table layouts and an overflowing native ToolStrip replace the loose button row.
The dialog has an explicit 96-DPI baseline and DPI scaling, uses the system dialog
font, and recomputes splitter/minimum constraints and screen-size limits on load
and DPI changes. The extension does not change VS's process/thread DPI policy or
application configuration. .NET Framework high-DPI behavior also depends on its
host's opt-in configuration; see [Microsoft's high-DPI guidance](https://learn.microsoft.com/en-us/dotnet/desktop/winforms/high-dpi-support-in-windows-forms?view=netframeworkdesktop-4.8).
These changes do not establish mixed-monitor or physical-DPI acceptance.

Verification:

- The actual net472 client builds in normal Debug and Release outputs with no
  warnings/errors. Runtime/server APIs, commit logic and ownership are unchanged.
- Eight distinct focused UX cases passed across targeted Debug runs: contextual
  creation/tint/reset accepted through one transaction, menu targeting/reorder/
  removal with no acceptance, four synthetic 100/125/150/200% layout factors and
  two larger-font cases. They exercise hidden controls with the SDK-free dialog
  source linked into the .NET test host, not the packaged net472 dialog in VS.
  Reports are under `artifacts/designer-ux-20261031/` at the repository root.
  Final layout evidence is `designer-ux-layout-visible.trx`; an earlier assertion
  incorrectly included a hidden swatch's stale bounds and is superseded there.
- The updated local package restores/builds in the independent Form and passes
  its nonvisual resource/event/binding/ownership check. This is baseline runtime
  evidence, not a new actual-VS authoring result. The prior 18 designer-host cases
  and accepted functional review are retained; no unchanged server, appearance
  or Backstage workload was repeated.

For user review, use the fresh UX solution in the review steps below. Check the
Add menu at tab/group/dropdown/gallery locations; create a contextual tab, change
and reset its header color, and try Cancel before OK. Then repeat the accepted
Undo/Redo and save/reopen workflow. Resize the dialog, move its splitter and check
toolbar overflow, wrapping, color controls and footer at your available display
scales. Genuine mixed-scale monitor movement remains a separate hardware check.
Visual inspection, actual-VS behavior of the revised dialog and physical-DPI
acceptance are left to the user. Preview/selection, Backstage authoring and theme
selection remain later increments; general theme authoring is outside scope.

## Property review, root and icon proposal — 2026-10-31

Status: **Size Policy dropdown and Ribbon root implemented; actual-VS review
pending**. User feedback identified the ScreenTip field order, manual SizePolicy
entry, missing asset picking and the need for future root-level feature settings.

- ScreenTip Description precedes Title because the categorized PropertyGrid sorts
  alphabetically. Preserve that standard behavior under the user's alphabetical
  exception; no property names, category tricks or hidden sort prefixes are added.
  See [PropertyGrid sorting](https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.propertygrid.propertysort).
- Size Policy now uses an exclusive dropdown. Common choices appear first as
  Fixed Large/Medium/Small and Large → Medium → Small. All 27 runtime-supported
  combinations remain available, including unusual existing policies. Friendly
  captions convert to the existing canonical triples for commit/serialization;
  runtime size-policy APIs/defaults and authored semantics are unchanged.
- A permanent **Ribbon** root contains the tab nodes and opens selected when there
  is no selected tab/group/command to reveal. It is a
  view-only node, cannot be removed/reordered, and does not enter the draft's
  component/ID/ownership tree or generated code. Its only current property is a
  read-only tab count. Removing the last draft tab retains the root and Add menu.
  Future theme selection/preview and feature controls have a natural location;
  none are silently introduced by this placeholder.

Verification: all 11 affected UX cases passed in
`artifacts/designer-ux-20261031/designer-root-sizepolicy.trx` at the repository
root. These include protected/no-op root behavior, replacing the last tab, all
27 policy conversions and a selected-policy commit, plus the affected prior UX
and synthetic layout/font cases. Client and server Debug/Release builds and the
Debug test-project build passed without warnings/errors after warning cleanup.
The fresh package-only consumer restores/builds and passes its baseline
resource/event/binding check. Actual-VS dropdown behavior, Undo/Redo and saved
generated policy declarations remain user review. Prior unrelated evidence is
retained; no full suite or new physical-DPI result is claimed.

### Image and SVG authoring slice

The user authorized the icon recommendation after committing the preceding
designer work as `b530a05`. The compatible SVG API, source/ownership and dependency
contract was presented before implementation. Current implementation, verification
limits and user review live in [SVG icon authoring](SVG-ICON-AUTHORING.md). Existing
raster resources and arbitrary custom providers retain their ownership; a general
raster picker and root feature policies remain separate work.

### Later feature-policy and root-settings slice

Audit existing switches before adding new ones: `QuickAccessVisible`,
`QuickAccessCustomizationEnabled` and `AnimationLevel` already cover parts of the
requested behavior. Map customization entry points, right-click behavior, QAT
visibility/customization, animation and any missing permissions to their actual
runtime owners. Propose only the gaps, with compatible defaults, then expose
appropriate root categories and verify that designer settings affect the shared
runtime behavior. Keep theme selection/preview distinct from theme authoring.
This is later work; the root added now is not a new settings owner or master flag.

## Surface preview and selection — 2026-10-31

Status: **implemented; user review pending**. Baseline: clean `1caf206` on
`codex/Ribbonkit-Winform`. The bounded contract was presented before implementation:
click tabs/groups/commands on the Form designer surface to select their actual
components in Properties; use a smart-tag selector to preview hidden or disabled
tabs, including contextual tabs. Do not execute commands, toggle values, open
popups or mutate authored visibility/enabled state to obtain a preview.

The design server owns an adorner with a selection outline. Picking and outlines
reuse the runtime's measured/clipped layout, including split-button parts, group
frames and scroll controls. The adorner is active while the ribbon or one of its
tab descendants is selected; selecting another Form control releases its picking.
Tab/body scroll controls scroll the preview without command activation. Hidden
or displaced children have no invented editable geometry; the nearest visible
ancestor supplies the outline. Collapsed groups and dropdowns remain closed.

**Preview tab** in the smart tag lists every authored tab, with component names
and hidden/disabled labels. The ribbon and its elements also expose **Edit Ribbon…**
and **Select Ribbon**. Preview uses an internal, design-only display-tab override;
`SelectedTab`, authored properties and component ownership are unchanged. There
is no public API addition or new runtime owner. The runtime grants internal access
to its design assembly so the designer can reuse layout rather than reconstruct it.
Resetting to **(Startup tab)** or disposing the designer restores the normal
display. Removing a previewed tab falls back safely. A minimized ribbon retains
its authored minimized presentation; expand it to review its body.

Design mode already suppresses native editor creation. Static TextBox/ComboBox
previews now reuse the existing label/frame/arrow renderer and show their values
with theme foreground/disabled roles. These are inert illustrations, not native
caret, selection, popup, IME or complete Windows styling previews. No editor
handles or bindings are created or moved. Runtime fields retain native ownership.

Opening **Edit Ribbon…** reveals the selected component in its tree. Cancel keeps
the live model and surface selection; OK uses the existing transaction and returns
the final tree selection to Properties, including newly created components.
Selection metadata is excluded from the authoring revision, so selecting another
node and accepting a no-op does not create an undo entry. Draft edits still apply
only on OK; the surface does not live-render unaccepted draft changes. Direct
drag/drop, inline editing, popup editing, Backstage authoring, themes, root feature
policies, localization and RTL remain separate increments.

Verification in normal **Debug** outputs:

- **11 new STA cases passed**, covering Classic/Simplified hidden contextual
  preview, disabled selection, command suppression, native-field placeholders,
  split/group geometry, clipping and scrolling, Properties/tree selection,
  no-op/one-transaction acceptance, new component identity, reset/removal/disposal,
  runtime isolation and serialization metadata. The final report is
  `artifacts/designer-selection-20261031/designer-selection-final.trx` at the
  repository root. The initial Simplified assertion assumed inline placement;
  the fixture now explicitly configures an inline command.
  After the final reset refinement, the two affected reset/scroll cases passed
  again in `designer-selection-reset-final.trx`; returning to the startup display
  also brings its header into view.
- **33 affected cases passed**: existing authoring/UX, SVG draft commit/rejection,
  Classic reduction and Simplified row geometry. Report:
  `artifacts/designer-selection-20261031/designer-selection-affected.trx`.
- Runtime/server and actual net472 client builds passed without warnings/errors.
  The isolated Debug package restores/builds in an independent consumer; all five
  packaged runtime/client/server/protocol assemblies match the current builds.
  Its public-only verifier passes ownership, IDs, artwork, events and two-way
  binding, plus native field values/handles retained across runtime tab switches.
- No actual-VS interaction, visual inspection, physical-DPI/mixed-monitor or full
  suite result is claimed. SDK-free selection policy and dialog source are linked
  into the STA test host; actual SDK adorner hit routing and smart-tag dropdown
  behavior remain user review. Prior unrelated passing evidence is retained.

For this increment, open the fresh solution under **User review steps** below:

1. Close older consumer solutions, open this one and use **View Designer**. Select
   the ribbon first, then click Home/Edit headers, Commands/Fields group captions,
   Apply/Bold and the Name/Font field illustrations. Properties should select the
   corresponding component and an outline should follow. Bold must not toggle;
   Apply must not run. Double-clicks should not run commands or create handlers.
2. With a component selected, use its smart tag to open **Edit Ribbon…**. The tree
   should reveal that component. Select another node and Cancel, then try OK with
   selection only; OK should return that component to Properties without an undo
   entry. Make one actual edit and review Undo/Redo using the baseline steps below.
3. Add a contextual tab with a group/command. Set its Visible and Enabled values
   to False and choose OK. It should be previewable without changing those values.
   Select Ribbon, use **Preview tab** to switch between it and Home/Edit, then
   choose **(Startup tab)**. Hidden/disabled markers should be clear. Save/reopen
   the designer and run the app: preview-only choices must not persist; the hidden
   tab must stay hidden at runtime.
4. Check narrow widths/overflow, contextual header tint, icon rendering, static
   fields, outlines and smart-tag readability at available display scales. Select
   an ordinary Form control and confirm normal selection/layout still works.

## Accepted functional baseline

The ribbon's **Edit Ribbon…** smart-tag action and **Tabs** property ellipsis
open one custom window. Its tree supports built-in tabs, groups and command
types, nested dropdown commands and gallery tiles. Add/remove and sibling
reordering work on the detached draft. A property grid edits supported scalar
properties; ComboBox choices have a separate multiline editor. Existing component
names are retained; rename them through Visual Studio's Properties window.

The server validates the draft before starting a live transaction, rejects stale
drafts and duplicate IDs/names, and prevents removal of a QAT reference's source.
OK uses the designer host to create/destroy components and reports collection and
property changes in one `Edit Ribbon` transaction. Existing component instances,
images, event handlers, bindings, selected tab and retained gallery selection
are preserved. Unsited components receive designer names without replacement.
ComboBox mode/value/selection dependencies, choice removal and three-state checkbox editing have
focused coverage. Cancel and an unchanged OK do not request a live transaction.

The accepted first window did not author File/Backstage/QAT content, images,
event handlers or bindings. The subsequent SVG picker is tracked separately in
[SVG icon authoring](SVG-ICON-AUTHORING.md). Existing values remain on their original components; the
ordinary designer properties and compatible collection-editor types remain.
Custom subclasses are outside the window's supported built-in type set. Theme
selection/preview remains deferred as described above. That baseline introduced
design-only editor/protocol/commit logic without runtime API or ownership changes;
the separate SVG record documents its compatible runtime additions.

The package now carries a .NET Framework 4.7.2 client and protocol beside the
modern design server and its matching protocol. This follows Microsoft's
[out-of-process designer architecture](https://learn.microsoft.com/en-us/dotnet/desktop/winforms/controls-design/designer-differences-framework)
and [package layout](https://github.com/microsoft/winforms-designer-extensibility/blob/main/docs/sdk/control-library-nuget-package-spec.md).
The runtime does not reference the design projects or Designer SDK.

## Evidence and limits

Initial evidence: the isolated package loads its runtime and design server into
the actual VS designer; the independent Form renders and its pre-VS resource,
event and two-way binding checks pass. Its SDK editor created, captioned and
reordered a sited tab and generated C#/resx with its stable ID. The fixture needed
explicit Form metadata and ordinary resource-cast syntax: the designer parser
rejects a null-forgiving suffix in InitializeComponent.

User observation, 2026-10-31: an Undo attempt showed a cannot-complete message;
on retry the added tab disappeared successfully. This is evidence for undoing
that addition in the current session, not a full Undo/Redo acceptance. One agent
attempt reached VS's code-window Undo warning and was cancelled; it is excluded
from designer acceptance.

Functional-baseline automated evidence, 2026-10-31:

- Native solution Debug and Release builds passed with no warnings/errors. The
  design server and protocol were rebuilt in both configurations after the final
  field-state refinement; the unchanged client build evidence is retained.
- Eighteen focused STA cases passed in the final Debug designer run: detached/no-op
  drafts, retained component/resource/event/binding/QAT identities, unsited naming,
  subtree disposal, six invalid/stale rejection paths, native choices/free text,
  editable mode/value changes, three-state checks, duplicate-choice selection,
  new selection by value and choice removal with a stale index.
  Final report: `artifacts/designer-authoring-20261031/designer-selection-final.trx`
  at the repository root; preceding targeted reports are retained there.
  These use an independent designer-host test double; they do not prove actual
  Visual Studio Undo/Redo.
- The local package contains client/protocol/server DLLs in their respective
  design folders, with no Designer SDK package dependency on the runtime.
  The independent package-only Form restores/builds with no warnings/errors and
  passes its nonvisual component/ID/order/image/event/two-way-binding checks.
  No Showcase resources, helpers or project reference is used.

User acceptance, 2026-10-31: the user confirmed "everything works" in response to
the supplied review checklist. This accepts the bounded custom Edit Ribbon
workflow: opening and editing the tree, add/remove/reorder, Cancel/OK, designer
Undo/Redo, save/reopen and the independent Form's resource/event/binding behavior.
Visual inspection was performed by the user. This is user-reported acceptance,
separate from the automated evidence above.
No full test suite, new appearance/Backstage run, physical-DPI review or broad
designer acceptance is claimed. Prior bounded appearance evidence is retained.
WPF runtime/build/CI and the reference project remain untouched. No commit, push
or publish is authorized.

## User review steps

A fresh Debug package consumer is ready at:

```text
C:\Users\LENOVO\AppData\Local\Temp\RibbonKit-WinForms-Designer-20261031-selection-final-review\App\RibbonKit.Designer.Check.sln
```

Close the older `RibbonKit.Designer.Check` VS window first so the previous
package's designer assemblies are released. Open the solution above. It has its
own local feed and package cache; a runtime project reference does not exercise
the packaged client extension. Do not add a reference to Showcase or the design
projects.

For the latest increment, start with the **Surface preview and selection** review
checklist above; the following steps retain the accepted functional baseline.

1. Open `DesignerForm.cs` with **View Designer**. Select the ribbon, then choose
   **Edit Ribbon…** from its smart tag; the **Tabs** ellipsis should open the same
   window. Confirm that it is the custom tree/property window and Home/Edit are
   present. Check sizing, readable labels, Tab navigation and Escape/Cancel.
2. Add a temporary tab and group, change their Text/KeyTip, add a Button and
   ComboBox, enter a few choices, and move the tab/commands up and down. Remove a
   temporary command. Choose **Cancel**; the live ribbon and generated code
   should be unchanged. Repeat a smaller set of edits and choose **OK**. Preserve
   the original Home/Edit relative order and the original commands/field choices
   for the supplied runtime check. Added tabs, groups and commands are allowed.
3. Click the **designer surface**, then Undo once. The entire accepted dialog
   edit should revert in one step. Redo once should restore it. Repeat with a
   removal. Note any cannot-complete message and the exact preceding action.
   Avoid Undo from the C# code window, which has a separate generated-code warning.
4. Save All, close and reopen the Form designer, then close/reopen the solution.
   The authored tree/order/captions/IDs should remain. Inspect
   `DesignerForm.Designer.cs` and `DesignerForm.resx`: the image resource, Apply
   Click hookup, field binding, choices and authored components should remain
   ordinary generated declarations without duplicate IDs or lost references.
5. Build and run. The blue Apply image should remain; typing in the ordinary
   TextBox should update the ribbon Name field and vice versa. Apply should show
   `Applied: <value>`. Check the newly authored commands and reopen Edit Ribbon.
   Visual appearance, focus and interaction are yours to assess.

After saving, the nonvisual verifier can check the generated Form again. From
PowerShell, inside that solution's `App` directory:

```powershell
dotnet build -c Debug
Start-Process .\bin\Debug\net10.0-windows\RibbonKit.WinForms.DesignerConsumer.exe -ArgumentList '--verify-roundtrip', '..\after-vs' -WindowStyle Hidden -Wait
Get-Content ..\after-vs\result.log
```

The check permits additional authored tabs/groups/commands, but expects the
original Home/Edit ownership and relative order, Apply/Bold commands, Name/Font
fields and original Font choices to remain. It checks resource/event/binding
behavior without showing a window. A passing result before VS edits is baseline
evidence only; no separate after-save verifier output has been reported.

To prepare another isolated consumer later, run
`winforms/eng/Prepare-DesignerConsumer.ps1` from the repository root. It builds
the native client/server in normal Debug outputs, packs locally and prints a new
solution path. Existing destinations are preserved.
