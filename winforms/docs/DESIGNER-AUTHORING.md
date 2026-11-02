# Native designer authoring checkpoint

Status, **2026-10-31: bounded custom designer increment accepted by the user**. Baseline: `516d121` on
`codex/Ribbonkit-Winform`. This checkpoint follows the disabled/state appearance
increment and preserves the accepted Backstage behavior.

Latest increment, **2026-11-02: ComboBox label/compact presentation, cross-owner
moves and shrinking size-policy choices**; see [designer rearrangement](DESIGNER-REARRANGEMENT.md).
The earlier menu increment is recorded under
[dropdown headings and descriptions](#dropdown-headings-and-descriptions--2026-11-01).
[Group item layout](GROUP-ITEM-LAYOUT.md) owns the earlier split-orientation,
two/three-row and in-group separator contract, evidence and remaining review.
The [QAT authoring checkpoint](QAT-DESIGNER-AUTHORING.md)
owns QAT's current contract, evidence and acceptance. See the sections below for earlier bounded contracts,
evidence and review steps. Earlier accepted scopes retain their acceptance.
Current Backstage authoring and preview status is in
[Backstage authoring](BACKSTAGE-DESIGNER-AUTHORING.md).

Current review project: the permanent
[Designer Sample solution](../samples/RibbonKit.WinForms.DesignerSample/RibbonKit.WinForms.DesignerSample.sln).
Refresh it in place with `./winforms/eng/Update-DesignerSample.ps1` from the
repository root. The earlier temporary paths below are historical evidence/backup
locations; new user review continues in this sample.

## Dropdown headings and descriptions — 2026-11-01

Status: **user confirmed the reviewed popup authoring works, 2026-11-02**.
Exhaustive VS save/reopen coverage remains separate. The user's four follow-up
notes are handled in [designer rearrangement](DESIGNER-REARRANGEMENT.md).
Edit Ribbon now exposes the existing `DropDownHeader` and `MenuPresentation`
properties on dropdown and split-button commands. Child `MenuDescription` was
already editable and now has help text explaining its inline presentation.
This changes designer authoring only; runtime defaults and popup geometry remain
unchanged. Galleries retain their separate heading/tile model and do not offer
these command-menu properties.

To reproduce the Showcase Paste popup:

1. Select the parent dropdown or split button in Edit Ribbon. Under Appearance,
   set **DropDownHeader = Paste options** and **MenuPresentation = Standard**.
2. Add a child Button. Set **Text = Paste text only**, choose its Icon, and set
   **MenuDescription = Insert clipboard text without its original formatting.**
3. Add a **Separator**, then another child Button with **Text = Paste special**
   and **MenuDescription = Choose which clipboard format to insert into the document.**
4. Press OK, save the Form and run it to open the popup. Reopen the designer to
   check the saved declarations. Cancel leaves the live model unchanged.

The parent heading is decorative and is not another Button. A child's Text is
its title; MenuDescription is the explanation under it (up to two wrapped lines).
ScreenTipDescription remains the separate hover tooltip. For the larger-artwork
variant with bold leaf-command titles, choose **IllustratedChoices** instead of
Standard. Empty DropDownHeader hides the heading; empty MenuDescription keeps a
title-only command. Icons use the existing picker.

Verification: 12 affected designer/menu checks passed across the initial focused
run (10 passed) and two repaired-test reruns. Coverage includes both dropdown and
split owners, Standard/IllustratedChoices, property-grid choices, detached draft
preview, Cancel/OK, model identity, reopen data, enum/string CodeDOM, framework
Undo/Redo, invalid input, and separator ownership. The older menu ownership test
now reflects the preceding increment's support for separators in groups.
Standard and IllustratedChoices 96-DPI popup captures under
`winforms/artifacts/designer-menu-authoring/` were inspected. Normal Debug design
server/client and the refreshed package-only Designer Sample build passed cleanly.
The user's authored Form files were preserved. Actual VS save/reopen, manual
keyboard interaction and physical mixed-DPI review remain separate; no full
native or WPF suite was run.

## Programmatic names and QAT selection — 2026-11-01

Current review and acceptance are recorded in the [QAT checkpoint](QAT-DESIGNER-AUTHORING.md).
The user reported that QAT could not be selected on the Form designer and that
returning from Edit Ribbon left the ribbon selected. They then requested a
programmatic Name field throughout the component editors.

- **Design → (Name)** now edits the actual generated component name in Edit
  Ribbon and Edit Backstage. It covers tabs, groups, all command/child kinds,
  catalog commands, QAT references, Backstage pages/commands/separators and the
  Ribbon root control. **Text** remains the displayed caption; **Id** remains the
  stable command identity. Organizational branches and fixed nested settings
  models do not acquire a renameable field.
- The client edits detached names. The server validates identifiers using the
  host name service (C# provider fallback in a minimal host), unique names and
  current container occupancy before opening a transaction. Names must be unused
  when OK is applied; reusing another component's current name requires a
  separate accepted rename/removal first. Existing types and object identities
  remain protected. New components can use the chosen name on first creation.
- Existing objects are renamed through their designer sites at the end of the
  same OK transaction, after old draft keys have resolved owners and sources.
  The host's ComponentRename notifications retain normal code generation and
  Undo/Redo. RibbonControl.Name is synchronized too. Name-only edits bypass
  collection rebuilding. Cancel and unchanged OK retain their existing behavior.
- QAT picking now includes its measured/clipped targets and selection outlines,
  and catalog/QAT selection keeps the adorner active. Clicking an entry selects
  its presentation; **Select source command** in Properties/designer actions
  selects the actual command for name, caption and event editing. It never
  executes the command or opens a runtime popup.
- Edit Ribbon publishes its final component selection after the modal property
  editor returns, using a selection-only design request. This avoids Visual
  Studio restoring the original ribbon property owner after the commit. An
  unpinned command is reached through Edit Ribbon's **Commands without a tab**
  branch and OK; that branch is not a separate Form-designer tree.

Focused evidence:

- **12 new naming cases pass across retained runs**: all named-node metadata,
  detached edits, root and child renames, invalid/occupied names rejected before
  transactions, first-creation names, one-unit framework Undo/Redo, unchanged
  objects/events/bindings/IDs/source references, no collection rebuild, retained
  native page handles/text/selection and CodeDOM references using the new field
  name. **80 affected authoring cases passed** separately. Reports are under
  `TestResults/winforms-designer-names/`; the initial 11 naming passes are retained
  in the task's console evidence, with serialization added afterward.
- The QAT-selection correction passed **17 focused selection cases** before the
  naming increment. Actual fresh-package VS review verified BelowRibbon entry
  picking/outline, Edit source → OK selecting `ribbonButton1` in the main
  Properties window, its Click event row, and the direct Select source command
  action. No handler was created or user-authored Form code changed for that check.
- The new (Name) field was inspected in the actual VS modal editor. Computer-use
  keyboard entry into that modal was unreliable, so an actual rename followed by
  save/reopen, generated-code build and handler creation remains a user review
  gate; it is not established by the framework/CodeDOM checks.
- Normal Debug runtime/server/client/sample builds and local package creation
  passed with zero warnings/errors. The permanent sample was refreshed to
  `0.1.0-designer.local20261101T151257400Z`, preserving authored source/resources.
  No runtime public API, theme, persistence schema, WPF, full-suite or release
  validation changed or ran for this designer increment.

### Visual Studio name availability correction — 2026-11-01

The user's first rename attempt failed with an existing `homeTab` collision while
editing the Save command's name. The server had called `INameCreationService`
for every component, including unchanged names. The VS implementation also checks
availability and has no component argument with which to exclude the current
owner. A minimal DesignSurface without that service did not reproduce this.

The shared preflight now checks draft uniqueness/container ownership first,
skips unchanged names, and calls the host validator for genuinely proposed unused
names. Case-only renames retain their own slot and use the host CodeDomProvider
for identifier syntax, with the existing C# fallback when unavailable. Duplicate
names, keywords and host-specific inherited-name rules remain enforced before a
transaction. No component is temporarily removed or renamed for validation.

Five service-backed regressions reproduced the prior failure, including the exact
`homeTab` message. **19 focused naming cases passed across the corrected 17-case
run and two added guard cases**: both editors, unchanged OK, case-only edits,
language keywords, inherited-name rejection, object/source/binding/page retention,
Undo/Redo and named-source serialization. Retained reports are
`host-availability-before.trx`, `host-availability-fixed.trx` and
`host-availability-guards.trx` under `TestResults/winforms-designer-names/`.
The unrelated prior authoring passes were not repeated.

Normal Debug server/client/sample builds and refresh passed with zero warnings or
errors. The permanent sample now uses local package
`0.1.0-designer.local20261101T154053425Z`. The currently open VS/editor session
still owns the previous assemblies and must be reopened before retrying the
rename. Actual retry/save/reopen acceptance remains with the user; the focused
checks do not claim a fresh live VS pass.

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
This increment offers selection of library-provided themes with a
designer preview. It does not expose an arbitrary theme-color/settings editor
or introduce a mutable theme owner. `Theme` remains an immutable runtime value
and remains hidden from the designer. The optional selection property below
round-trips independently and leaves shared theme instances untouched.
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
   The first implementation and review are in [Backstage authoring](BACKSTAGE-DESIGNER-AUTHORING.md).
4. **Theme selection and preview.** Offer supported library themes under the
   boundary above, using the improved designer workflow. No general theme
   authoring is planned. Advanced surface editing is not a prerequisite for this
   or Backstage authoring.
   The bounded implementation is recorded below; actual VS acceptance is pending.

Live visual inspection remains with the user. These are planned increments;
acceptance and automated evidence below apply to the completed functional slice.

## Built-in theme selection and preview — 2026-11-01

User review, **2026-11-01**: "looks good" accepts the built-in theme selection and
preview at the reviewed scope. Physical-DPI and mixed-monitor acceptance remain separate.

The current native catalog contains **only `RibbonThemes.Office2024Light`**.
This increment selects that existing palette; it adds no palette, renderer,
arbitrary color/settings editor or mutable theme owner. The proposed public
contract was stated before implementation in this session.

| Member/action | Contract |
| --- | --- |
| `RibbonBuiltInTheme.Office2024Light` | The existing immutable shared Office 2024 Light value. |
| `RibbonControl.BuiltInTheme` | Nullable selector. Default `null` means use the retained application theme; a new ribbon still uses Office 2024 Light. |
| Explicit built-in choice | Applies the built-in palette, retaining the last application theme as the fallback. The explicit choice serializes even when it matches the initial palette. |
| Reset / Application theme | Sets the selector to `null`, returns to that fallback and omits the generated selector assignment. With no application assignment, this returns Office 2024 Light. |
| Direct `Theme` assignment | Validates the immutable value, updates the application fallback and clears the selector, including assignment of the current effective value. A later direct application assignment wins over generated selection. |
| Custom themes | Remain application-owned through the existing hidden `Theme` property. Opening, Cancel and unchanged OK preserve the supplied value. No custom theme object is serialized by this editor. |

Generated C# for an explicit selection is:

```csharp
ribbon.BuiltInTheme = RibbonKit.WinForms.RibbonBuiltInTheme.Office2024Light;
```

In **Edit Ribbon**, select the protected **Ribbon** root. Its **Ribbon settings** section
offers **(Application theme)** and **Office2024Light**, identifies the fallback
as Office 2024 Light or a custom application theme, and provides **Preview ribbon
and Backstage…**. The preview window has separate scrollable Ribbon and Backstage
images. Both use the draft choice and existing renderer. The ribbon image draws
detached authored tabs/groups/commands and the File declaration. Backstage uses
the existing main/footer navigation renderer and shared body placeholder.
With the default application theme, both choices have the same appearance.

Draft preview does not mutate the live ribbon, execute commands, create/capture
native pages, change authored visibility, startup selection or File routing.
Contextual colors and borrowed artwork remain declarations. Hidden/disabled
ContentType pages are not instantiated. Native page controls keep their own
application styling; selecting a ribbon palette does not restyle them.

OK applies selection and any structural edits in one existing designer
transaction. A theme-only edit leaves collections/components untouched. Unchanged
OK creates no transaction. The revision includes selection and immutable effective/
application values, so external theme changes invalidate an open draft. Reset
and Undo/Redo restore the fallback without serializing custom theme internals.
The accepted Form preview layer, frame and runtime motion paths are unchanged.

Verification uses normal Debug output, with evidence under
`TestResults/winforms-designer-theme/`:

- **51 distinct relevant cases pass across the retained focused runs**: 18 theme
  cases, the 29 existing authoring/editor cases and four directly affected
  Backstage preview DPI cases. Reports retain the initial test-host setup/layout
  assertion failures and their focused corrections; this is assembled focused
  evidence, not a single full-suite pass.
- The theme cases cover nullable/default/Reset metadata, explicit enum CodeDOM,
  direct assignment precedence, detached root choice, Cancel/no-op OK, one combined
  transaction, stale/invalid rejection, actual framework UndoEngine Undo/Redo,
  both renderer-backed previews, native-content isolation and four synthetic
  root-dialog scale/resize cases. The standalone Undo host supplies the framework
  `CodeDomComponentSerializationService` that a minimal DesignSurface lacks.
- Native runtime, design server, net472 client and both protocol targets build.
  The final focused build reports no warnings/errors. Root/preview PNGs were
  inspected; synthetic scaling is not physical-DPI or actual VS acceptance.
- Independent package-only public API verification generated, normally compiled
  and reopened four forms in fresh processes: Form/RibbonForm, application/explicit
  choices. Custom fallback, Reset, direct assignment precedence, native content
  styling/ownership and actual generated enum assignment pass. This new check
  uses no RibbonKit internals or Showcase helpers. Evidence is in
  `C:\Users\LENOVO\AppData\Local\Temp\RibbonKit-WinForms-Designer-20261101-theme-selection-review\theme-selection\result.log`.
- Actual installed VS client protocol composition resolves both endpoints by
  name, request and response after the preview flag addition. This is protocol
  evidence, not actual VS editing acceptance.
- A broader unit/STA run was started, then stopped on the user's instruction to
  test only relevant scopes. It reported the out-of-scope native tree double-buffer
  assertion in `ReviewFixTests`; that was not investigated or changed. No full-suite,
  Release, native motion, physical-DPI, mixed-monitor or new user acceptance is claimed.
  Earlier accepted preview-layer/frame/motion evidence is retained.

The existing user review solution remains
`C:\Users\LENOVO\AppData\Local\Temp\RibbonKit-WinForms-Designer-20261101-bottom-navigation-review\App\RibbonKit.Designer.Check.sln`.
Its package configuration is backed up as `NuGet.Config.theme-selection-20261101.bak`;
the theme package uses a fresh private cache. Hashes confirm all 15 authored
C#/resx/project files were preserved. `theme-selection-update-20261101.json` in
that review root records the refresh.

Actual VS review:

1. Close/reopen the review solution to load the refreshed package. Open **Edit
  Ribbon → Ribbon → Ribbon settings**, select Office2024Light and inspect both preview views.
2. Cancel, reopen and verify the prior choice remains. Accept a choice with OK;
   use Undo/Redo and verify it is one operation. Unchanged OK should add no operation.
3. Save, close and reopen the Form designer. Inspect the enum assignment in
   generated code. Reset `BuiltInTheme` in Properties (or select Application theme
   in the editor), then save/reopen and verify the assignment is omitted.
4. Review File/Back and main/footer preview navigation, contextual header tint,
   and the retained shared placeholder. Final actual VS/visual and physical-DPI
   acceptance remains with the user.

## Animation selection in Ribbon settings — 2026-11-01

Status: **implemented; actual VS/user review pending**. The protected Ribbon root
now groups theme and animation under **Ribbon settings**. The **Animation**
dropdown exposes the existing `RibbonControl.AnimationLevel`: **None**, **Subtle**,
or **Expressive**. None remains the runtime/designer default. Nondefault choices
serialize as ordinary enum assignments, for example:

```csharp
ribbon.AnimationLevel = RibbonKit.WinForms.RibbonAnimationLevel.Subtle;
```

Reset in Properties restores None and omits the assignment. This is designer
access to an existing preference; no runtime/public API or motion implementation
changes. System-disabled animation continues to use the existing immediate path.
The dropdown edits a detached snapshot, Cancel leaves the model unchanged, and
OK applies animation with other edits in one designer transaction. Unchanged OK
creates no undo entry. Root-only changes leave collections untouched. External
animation changes invalidate an open draft. Image previews remain static and do
not enable the selected motion level on the preview or live ribbon.

Relevant Debug verification only:

- **17 focused cases pass**: 12 new animation cases, the four affected root-layout
  scale/resize cases and the existing theme Undo/Redo case. The four layout cases
  also pass with explicit animation-row ordering/width assertions. Evidence:
  `TestResults/winforms-designer-animation/designer-animation-focused.trx` and
  `designer-animation-layout.trx`. These are 17 distinct cases, not 21.
- The new cases cover all dropdown choices, JSON snapshot round-trip, detached
  preview, Cancel/no-op OK, invalid/numeric/stale rejection, a combined theme/
  animation framework UndoEngine unit, enum serialization and Reset/default
  handling. The rendered root-page PNG was inspected.
- Runtime/protocol, design server and net472 client compile in their normal Debug
  folders with no warnings/errors. No broad suite or native motion tests were run.
- An independent package-only consumer generated, normally compiled and reopened
  None/Subtle/Expressive initializers in a fresh process; default omission and
  Reset pass. No RibbonKit internals or Showcase helpers are used. Evidence:
  `C:\Users\LENOVO\AppData\Local\Temp\RibbonKit-WinForms-Designer-20261101-animation-selection-review\animation-selection\result.log`.

The existing bottom-navigation review solution uses the updated package through
`packages-animation-selection-20261101`. Its prior configuration is backed up as
`NuGet.Config.animation-selection-20261101.bak`; hashes confirm all 15 authored
C#/resx/project files remain unchanged. `animation-selection-update-20261101.json`
in that review root records the refresh.

For actual VS review, reopen that solution, select **Edit Ribbon → Ribbon →
Ribbon settings → Animation**, then check Cancel, OK, Undo/Redo and save/reopen.
Selecting None (or Reset in Properties) should remove the saved assignment.
Native motion behavior, physical-DPI and mixed-monitor acceptance are unchanged.

### Shared tab content animation setting — 2026-11-01

The root **Ribbon settings** now exposes **Tab content animation**, offering
Fade (the existing default) and SlideDown. It edits the existing property used by
expanded tab content, arriving headers and group collapse/restore; the user's new
[Backstage page motion](BACKSTAGE-PAGE-MOTION.md) uses that same preference.
Groups retain their sideways slide. None/Subtle/Expressive remains a separate
animation-level setting. No new public API is added.

The style participates in the detached snapshot and revision, one mixed-root
transaction and Undo/Redo. Cancel and unchanged OK preserve live values; Reset
restores Fade and omits its generated assignment. Nondefault code is the ordinary
`ribbon.TabContentAnimation = RibbonTabContentAnimation.SlideDown;` assignment.
Static designer previews remain unchanged in behavior.

The 24 focused designer cases, root-page inspection and package-only native
verification are recorded once in the linked page-motion plan. The permanent
Designer Sample has been refreshed in place; no new review project was created.
Actual VS/visual review is pending for this addition.

### Window backdrop in Ribbon settings — 2026-11-01

**Edit Ribbon → Ribbon → Ribbon settings → Window backdrop** offers **None**,
**Mica** and **Acrylic**. It edits the existing containing `RibbonForm.Backdrop`
property, rather than adding another preference to RibbonControl. The dropdown
is enabled only when that editable RibbonForm is the designer root containing
the ribbon; ordinary Form and UserControl designers retain a disabled choice.
The label identifies the backdrop as a runtime effect. Static ribbon/Backstage
preview images stay inert and do not activate a native window material.

The detached draft reads the current requested backdrop. Cancel and unchanged
OK leave live settings and undo history unchanged. OK reports the Form property
change through the designer change service inside the same transaction as theme,
animation and structure edits. The revision includes the containing-host capability
and current backdrop, rejecting stale host/property changes. Values are validated
against the actual host, including forged dialog metadata and numeric enum values.
The existing property serializes nondefaults on the Form:

```csharp
Backdrop = RibbonKit.WinForms.RibbonWindowBackdrop.Mica;
```

None remains the default and Reset omits the assignment. Existing native fallback,
Windows policy and application content responsibilities remain as documented in
[native backdrops](M6C-BACKDROPS.md); this addition changes designer tooling only.

Focused evidence: **19 distinct checks pass across retained runs** under
`TestResults/winforms-designer-backdrop/`: three dropdown/JSON/detached-preview
choices, Cancel/no-op OK, three invalid values, two stale host/property cases,
unsupported-host rejection, mixed Form/ribbon framework Undo/Redo, three existing
Form-property serialization/Reset values, and four root-dialog scale/resize cases.
`backdrop-settings.trx` retains 18 passing results; the combined Undo test passes
in `backdrop-undo-sited.trx` after naming/siting the fixture Form and tab as a real
designer does. The root-dialog image with the enabled Acrylic choice was inspected.
Normal Debug runtime/server/client/sample builds pass with zero warnings/errors.
The permanent Designer Sample was refreshed in place, preserving authored files.
No runtime, motion, native-material campaign or full suite was run for this tooling
increment. Actual VS interaction, save/reopen and target-machine material appearance
remain user review.

For review, reopen the permanent sample solution, open Edit Ribbon and select
the Ribbon root. Check backdrop Cancel, then OK and one Undo/Redo alongside a
ribbon edit. Save/reopen and run the Form to review the chosen material. The
designer canvas and preview images do not simulate the operating-system material.

### Transparent Backstage in Ribbon settings — 2026-11-01

The root **Ribbon settings** now includes **Transparent Backstage navigation**,
matching the existing Showcase toggle. Checking it sets the existing
`ribbon.Backstage.NavigationBackgroundOpacity` to **0**; clearing it sets **1**.
Transparency reveals an active Mica/Acrylic backdrop through the navigation
background. Text, icons and application page content retain their opacity; Solid
or unsupported hosts keep their opaque fallback. The checkbox does not enable
a backdrop, switch the File destination or open Backstage. It remains available
for authoring the preference before the application enables a material.

The snapshot preserves the actual float value: a pre-existing partial opacity
appears checked and stays exact unless the checkbox is toggled. Cancel and no-op
OK preserve it. The value participates in revision validation, including edits
made in the existing Edit Backstage dialog. Invalid/out-of-range/non-finite values
are rejected before mutation. The inert preview remains Solid.

For designer Undo, the existing Backstage model is registered in the ribbon's
host-provided nested container when available, under its existing `Backstage`
property name. It remains the same owned model, outside the host's top-level
component collection. The primitive property and its ribbon content owner receive
change notifications in the same transaction as backdrop/theme/animation/tree
edits. This avoids serializing an unsited model on its own and restores the
default opaque value as well as nondefaults. Microsoft's
[nested-container contract](https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.nestedcontainer?view=net-10.0)
and [designer site implementation](https://source.dot.net/System.Windows.Forms.Design/System/ComponentModel/Design/DesignerHost.Site.cs.html)
describe the ownership/service path used here. No runtime API or painting path changes.

The generated setting remains ordinary nested content:

```csharp
ribbon.Backstage.NavigationBackgroundOpacity = 0F;
```

Reset restores **1** and omits that assignment. New nested-site checks preserve
model/command identity, the existing content serialization path, one-time model
disposal and borrowed application page ownership.

**23 focused designer checks pass** in
`TestResults/winforms-designer-transparent-backstage/transparent-settings-final.trx`:
checkbox/partial-opacity behavior, Cancel/no-op, invalid values, stale drafts in
both editors, nested serialization/Reset/lifetime, combined framework Undo/Redo,
the affected backdrop choices and four scale/resize cases. The enabled checkbox
layout was inspected. Normal Debug runtime/server/client/sample builds pass with
zero warnings/errors, and the permanent package-only sample was refreshed in
place. No motion, native material campaign or full suite was run. Actual VS
save/reopen and runtime material appearance remain user review: reopen the sample
solution, select Mica/Acrylic and the checkbox, then run and open File.

### Designer Sample runtime animation correction — 2026-11-01

The user reported that selecting Expressive had no runtime effect. The authored
`DesignerForm.Designer.cs` did contain the Expressive assignment, with no normal
startup override; the Windows client-area animation query also returned enabled.
The permanent sample lacked the compatibility manifest already present in
Showcase and the independent native consumer. Its animation drawing-window
creation failed and the existing runtime correctly fell back to immediate changes.

The sample now includes `app.manifest` and declares it through `ApplicationManifest`
in its project. It uses the same Windows compatibility/common-controls declarations
as the other native hosts, with its own assembly identity. This is a host
integration correction; no library, designer selection or public API changes were
needed. The user's authored Expressive/theme settings remain unchanged.

A focused `--verify-animation` sample check uses its actual compiled initializer,
public RibbonKit APIs, an offscreen native Form and real UI timers. Before the
manifest correction it logged Expressive, system animations enabled, a native
window-handle creation failure, and `IsAnimating=false` on tab selection. After
the correction, tab selection and Backstage opening/closing each report
`IsAnimating=true`, then complete with `IsAnimating=false`. No OS policy override,
page capture or RibbonKit internal reflection is used. Debug build passes with
zero warnings/errors. Evidence is under
`TestResults/winforms-designer-animation-runtime/before/` and `after/`.
No broad tests were repeated. User visual, physical-DPI and frame-pacing review
remain separate from this bounded native runtime check.

## Permanent Designer Sample — 2026-11-01

At the user's request, the existing bottom-navigation review project's edited
sources now live in `winforms/samples/RibbonKit.WinForms.DesignerSample`, with
`RibbonKit.WinForms.DesignerSample.sln` and a matching project name. The 14 C#/resx
files and both assets were copied byte-for-byte from the reviewed project. The
project's designer nesting metadata was retained, its package reference now uses
the local refresh version, and its existing source namespace remains unchanged.
The old temporary solution is retained as a backup. No reviewed design was reset.

`eng/Update-DesignerSample.ps1` builds the runtime/server/client in normal Debug
folders, packs a unique local prerelease, then restores and builds the same sample.
It writes only ignored `.local/DesignerPackage.props`/refresh metadata and normal
build/package outputs; it never copies fixtures over the user's source/project.
The private feed/cache is under ignored `winforms/artifacts/designer-sample/`.
NuGet source mapping keeps RibbonKit on that local feed. A fresh package version
avoids stale caches without deleting them or using the global package cache.

The standalone sample solution avoids restoring the sample before its designer
package exists. Run the refresh once after checkout, then open this solution in
Visual Studio. Reopen it after package updates. The no-destination
`Prepare-DesignerConsumer.ps1` command now delegates to the in-place refresh.
Its explicit `-Destination` path still prepares isolated automated fixtures when
a named independent-verification scope requires one. New user review does not
create another temporary project.

Relevant verification only: the Debug designer/sample builds pass with zero
warnings/errors; the second refresh through `Prepare-DesignerConsumer.ps1 -NoBuild`
uses the same project and preserves every authored file hash. The relocated Form
initializes, and its bitmap resource check passes. Its retained strict
`--verify-roundtrip` fixture check stops at the expected Bold SVG assertion:
the source review's `boldToggle.Icon` assignment/resource was already absent.
Both the verifier and designer source match that original review byte-for-byte;
no icon was restored or test relaxed to make it pass. This is not a complete
round-trip pass. Evidence and migration hashes are under
`TestResults/winforms-designer-sample/`. Actual VS review at the new location is
pending; no runtime, broad regression or physical-DPI tests are needed for this move.

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

Status: **accepted by the user, 2026-10-31**: the provided preview/selection review
workflow "works perfectly". This accepts that bounded designer workflow; it does
not establish broad physical-DPI or mixed-monitor acceptance. Baseline: clean `1caf206` on
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
names were retained at that checkpoint; the programmatic-names follow-up above
now supports renaming them inside the component editors as well.

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
selection/preview is implemented in its separate increment above. The earlier baseline introduced
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

For ongoing review, run `winforms/eng/Update-DesignerSample.ps1` from the repository
root and reopen the permanent Designer Sample solution linked above. The
no-destination `Prepare-DesignerConsumer.ps1` command does the same. Use an explicit
`-Destination` only for a separate independent-verification fixture; existing
destinations are preserved.
