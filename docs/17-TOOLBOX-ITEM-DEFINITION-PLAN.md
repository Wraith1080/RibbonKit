# Toolbox item definitions

> T1 matrix approved and T2 authorized 2026-10-15. T2a definitions/discovery and
> T2b existing creation paths are implemented on `codex/api-and-component-updates`.
> Automated evidence and remaining live designer checks are below. T2c has not
> started. Runtime public APIs and unconfigured runtime defaults are unchanged.

## Goal

Refresh RibbonKit's declared Toolbox surface so application authors can discover
the supported controls, add them in appropriate locations, and receive consistent
defaults. Review the complete current authoring surface rather than only appending
the latest simplified-ribbon types.

Keep toolbox discovery, item creation defaults and feature availability separate:
the item list selects authorable types, designer initializers seed new items, and
runtime capability properties govern whether the feature can be used. Preserve
the accepted S6 Inline/label defaults and existing authored settings.

## Current boundary

- `src/RibbonKit/tools/VisualStudioToolsManifest.xml` is the package Toolbox
  allowlist, with RibbonKit as its Visual Studio/Blend category. It now follows
  the reviewed authoring matrix below.
- `src/RibbonKit/RibbonKit.csproj` packages that manifest under `tools/` and the
  net472 DesignTools assembly beside each runtime target in its `Design` folder.
- Project-reference Toolbox discovery reflects public controls and does not use
  that package allowlist. `ToolboxMetadata.cs` supplies matching visibility and
  category intent by string type name. Native host acceptance is still required;
  the manifest alone does not prove the Showcase Toolbox was filtered or updated.
- `Metadata.cs` registers designer providers by string type name. Initializers
  and Model API creation paths provide defaults; the manifest does not write
  per-command XAML properties. Use ModelProperty.IsSet to preserve assignments.

## T1 — inventory and authoring decisions

Compare public control types, package entries, designer metadata, context/editor
Add actions and public documentation. Produce one reviewed include/exclude matrix
with a reason and valid parent/location for each candidate. Public visibility
alone does not make a control useful as a Toolbox item.

### Inventory findings — 2026-10-15

At the T1 snapshot (`4d5c094`), the manifest had 18 unique entries. Source inspection
covers all 45 public FrameworkElement-derived types in `Controls`/`Layout`, including windows and
non-rendering merge sources, plus the nonvisual authoring objects below. Partial
class declarations count once. The approved package surface is 31 entries:
retain 18 and add 13. These are authoring destinations, not claims that
Visual Studio already routes a Toolbox drop into each property correctly.

The source of truth for this review is the [manifest](../src/RibbonKit/tools/VisualStudioToolsManifest.xml),
[metadata](../src/RibbonKit.Design/Metadata.cs),
[Model API helpers](../src/RibbonKit.Design/DesignModel.cs),
[context providers](../src/RibbonKit.Design/ContextMenuProviders.cs) and
[Ribbon Editor](../src/RibbonKit.Design/RibbonEditorWindow.cs). The
[setup guide](../src/RibbonKit.Design/SETUP-DESIGNTOOLS.md) describes the delivered
tooling. Public API visibility is preserved for excluded types.
Framework `ComboBoxItem`, `TextBlock`, `StackPanel` and other WPF content keep
their existing framework/editor routes; do not relist them as RibbonKit types.

### Reviewed include matrix

All types below are in `RibbonKit.Controls`. **Keep** identifies the original
manifest entries; **Add** identifies the approved T2a additions. A group panel
below means an application-authored panel inside a group, not a generated RibbonKit host.
The creation-support column records the T1 review; delivered changes are recorded
in the T2a/T2b sections below.

| Type | Decision | Valid owner/location | Existing creation support and gap |
| --- | --- | --- | --- |
| `Ribbon` | Keep | Host panel/content | Toolbox seeds Home/Group; editor and context verbs exist |
| `RibbonTab` | Keep | `Ribbon.Tabs`, authored merge-source tabs | Root context/editor Add Tab seeds a group; direct Toolbox drop has no tab initializer |
| `RibbonGroup` | Keep | `RibbonTab.Groups`, contribution `Group` | Context/editor Add Group exists; direct Toolbox drop has no caption initializer |
| `RibbonButton` | Keep | Group `Items`, group panel, authored QAT item | Toolbox Simplified initializer, context/editor Add and leaf verbs |
| `RibbonToggleButton` | Keep | Group `Items`, group panel, authored QAT item | Same supported-command defaults and creation paths |
| `RibbonCheckBox` | Keep | Group `Items` or group panel | Context/editor Add and leaf verbs; native full-group presentation |
| `RibbonRadioButton` | Keep | Group `Items` or group panel | Context/editor Add and leaf verbs; preserve authored GroupName/state |
| `RibbonSplitButton` | Keep | Group `Items`, group panel, authored QAT item | Supported-command initializer; editor Add Item creates menu entries |
| `RibbonDropDownButton` | Keep | Group `Items`, group panel, authored QAT item | Supported-command initializer; editor Add Item creates menu entries |
| `RibbonComboBox` | Keep | Group `Items` or group panel | Supported-command initializer and editor Add Item; absent from group context Add/leaf registration |
| `RibbonTextBox` | Keep | Group `Items` or group panel | Context/editor Add and leaf verbs; native full-group presentation |
| `RibbonScrollBar` | Keep | Group panel or application content | Generic XAML/Properties; absent from editor/context Add |
| `RibbonMenuItem` | Keep | Split/dropdown `Items` | Editor Add Item; do not treat as an independent Simplified command |
| `RibbonGallery` | Keep | Group `Items` or group panel | Supported-command initializer and editor Add Item; group context support incomplete |
| `InRibbonGallery` | Keep | Group `Items` or group panel | Same defaults; preserve native gallery/selection behavior |
| `RibbonGalleryItem` | Keep | Either gallery's `Items` | Editor Add Item and caption/content editing; no independent command defaults |
| `Backstage` | Keep | Singleton `Ribbon.Backstage` | Ribbon context Add Backstage seeds a page; no direct-drop initializer |
| `BackstageTabItem` | Keep | `Backstage.Items` | Backstage context Add Nav Item/Button and editor Add Item |
| `RibbonGroupSeparator` | Add | Group `Items` or horizontal command cluster | Editor Add exists; no caption, command ID, KeyTip or Simplified default |
| `RibbonApplicationMenu` | Add | Singleton `Ribbon.ApplicationMenu` | Root context/editor Add and pane preview exist; currently creates an empty menu |
| `RibbonApplicationMenuItem` | Add | Application menu `Items` | Editor semantic Add Command; Content distinguishes command from pane entry |
| `RibbonApplicationMenuSeparator` | Add | Application menu `Items` | Editor semantic Add Separator; retain non-command semantics |
| `RibbonApplicationMenuPaneItem` | Add | Panel in menu `DefaultContent` or menu-item `Content` | Editor semantic Add Pane Item creates a managed StackPanel; preserves custom content |
| `RibbonApplicationMenuButton` | Add | Panel in menu `FooterContent` | Editor semantic Add Footer Button; not a navigation item |
| `RibbonOptionsPage` | Add | `RibbonOptionsDialog.Pages` | Generic XAML/Properties; its template is a navigation header, not a standalone content presenter |
| `RibbonCustomizePage` | Add | Options-page `Content`, Backstage page or host content | Authored reusable editor; host explicitly assigns `Ribbon` and factory reset baseline |
| `RibbonSimplifiedCustomizePage` | Add | Same page/content locations | Host assigns `Ribbon`/baseline; remains authorable while capability or customization is disabled |
| `RibbonQuickAccessPage` | Add | Same page/content locations | Host explicitly assigns `Ribbon`; independent QAT policy applies |
| `RibbonMessageBar` | Add | `Ribbon.MessageBar` or documented standalone host panel | Runtime authoring exists; no dedicated designer Add/initializer |
| `RibbonMessage` | Add | Message bar `Items` | Runtime authoring exists; needs a parent-specific Add path and useful new-item text |
| `MdiContainer` | Add | Application content panel | Public documented preview host; host supplies documents and optional `Ribbon` binding, no sample documents injected |

### Proposed exclusions and alternate authoring routes

These 14 remaining public FrameworkElement-derived types stay outside the
package Toolbox. `Layout.*` identifies types in `RibbonKit.Layout`; the others
are in `RibbonKit.Controls`.

| Type | Decision/reason | Correct route |
| --- | --- | --- |
| `RibbonWindow` | Exclude: application window root | Author the Window root/base class in XAML/code |
| `RibbonOptionsDialog` | Exclude: owned Window with host lifecycle | Construct/declare the dialog root, then author its Pages; no new-item template in this slice |
| `RibbonCustomizeEditDialog` | Exclude: operation dialog | Existing customization/editor operation |
| `RibbonScreenTip` | Exclude: normally generated, requires tooltip ownership | ScreenTipTitle/ScreenTipText, or explicit `ToolTip` property XAML for advanced use |
| `RibbonMergeSource` | Exclude: non-rendering contextual definition | Resource/property XAML or host composition; its tabs/groups remain authorable |
| `MdiChild` | Exclude: generated document container | MdiContainer with documents/ItemTemplate; retain advanced template APIs |
| `RibbonTabControl` | Exclude: generated ribbon chrome | Ribbon template |
| `RibbonGroupsHost` | Exclude: generated tab content/presentation | RibbonTab/template lifecycle |
| `RibbonQuickAccessToolBar` | Exclude: library-owned projection host | Ribbon.QuickAccessItems/placement and templates |
| `BackstageNavPanel` | Exclude: template layout | Backstage template |
| `Layout.RibbonGroupsPanel` | Exclude: adaptive layout implementation | Group-host ItemsPanel/template |
| `Layout.RibbonQuickAccessPanel` | Exclude: adaptive strip layout | Toolbar ItemsPanel/template |
| `Layout.RibbonScrollContentHost` | Exclude: template scrolling decorator | Ribbon scrolling template |
| `Layout.MdiCanvasPanel` | Exclude: generated document layout | MdiContainer ItemsPanel/template |

Nonvisual authoring is reviewed separately and stays out of the Toolbox:

| Types/family | Reason and authored route |
| --- | --- |
| `RibbonGroupContribution` | DependencyObject definition in merge-source Groups; no independent drop surface |
| `MdiDocument` | Host document model, supplied through ItemsSource or AddDocument |
| `RibbonIconChoice`, `RibbonCustomizeNode`, `RibbonCommandEntry` | Editor/catalog data, not controls |
| `CapturedBackdrop` | Host-managed disposable effect/integration |
| `RibbonStringExtension` | XAML markup extension; use localization markup |
| Public automation peers and event args | Generated/accessibility or event payloads |
| Static animation, theming, localization, merge/customization and layout helpers; enums/interfaces | Services, attached-property configuration or contracts, not visual items |
| Internal proxies, flyout presenters and helpers | Generated presentation; neither package entries nor public authoring candidates |

### Defaults and creation-path alignment

Retain the accepted seven-family new-command helper unchanged in intent:
button, toggle, split, dropdown, combo, gallery and in-ribbon gallery get
`SimplifiedPlacement=Inline` and `SimplifiedSize=Medium` only when the corresponding
ModelProperty is not set. Check boxes, radio buttons, text boxes, scrollbars,
menu/gallery entries and separators do not acquire unsupported inline behavior.
These are designer creation defaults; runtime Overflow/Medium defaults remain.

The T1 review found specific gaps, addressed in the bounded T2b/T2c slices:

- Only Ribbon has starter structure; the seven command families have Simplified
  initializers. Direct tab/group/Backstage, combo/gallery and new container drops
  have no dedicated starter-content initializers. Root Ribbon initialization also
  unconditionally seeds a tab; any expanded initializer must guard existing model
  assignments/children before writing or appending.
- Group context Add and leaf metadata cover button/toggle/check/radio/split/
  dropdown/text only. Ribbon Editor additionally creates combo, both galleries,
  group separators and native TextBlock/StackPanel content. Scrollbar is missing
  from those Add lists. Align these meaningful paths without turning menu entries,
  native inputs or layout objects into Simplified commands.
- App-menu command, separator, pane and footer creation already has semantic
  Model API paths. Reuse them and preserve arbitrary custom Content. Do not turn
  every application-menu item into a pane or replace user content with a starter.
- Messages need a bounded root Add Message Bar / bar Add Message route and tree
  ownership before advertising an editor creation workflow. Options pages and
  MdiContainer belong to host authoring, outside the Ribbon Editor's structure
  tree. Their new-item defaults must not guess a Ribbon name/binding, attach
  events, inject persistence, documents, commands, icons or host windows.
- Seed useful captions and minimal empty-container content only for newly created,
  unassigned items. Do not populate a bound ItemsSource or overwrite explicit empty
  values, bindings, Content, Header, selections, dimensions or existing children.
  Use ModelProperty.IsSet and collection checks, inside the caller's single undo
  transaction; preserve existing Overflow/Small choices and native Size settings.
- Feature policy remains runtime/user-operation policy. Keep the authoring list
  static and disabled-feature settings editable with the existing explanation.
  Do not block developer-authored QAT placement or remove prepared definitions
  because a CanUser permission is false.

### Discovery, SDK and presentation decisions

The locally pinned designer SDK is
`Microsoft.VisualStudio.DesignTools.Extensibility` **17.10.34916.79**, referenced
by the [design project](../src/RibbonKit.Design/RibbonKit.Design.csproj).
Read-only metadata inspection of its two installed reference DLLs confirms
DefaultInitializer, ParentAdapter, ToolboxBrowsableAttribute/Behavior,
ToolboxCategoryAttribute and ToolboxTabNameAttribute are available. This verifies
API presence, not that a particular Visual Studio discovery/cache path honors
each attribute. The package contains no XML documentation or manifest XSD.

Recommended discovery contract:

1. **Package reference:** the manifest is the delivered allowlist, retaining
   `Reference`, `Type`, VSCategory/BlendCategory and UIFramework syntax. Apply the
   reviewed type set without inventing manifest display-name, order or icon fields.
2. **Project reference:** register the same visibility intent by string-type
   designer metadata, using the SDK's Toolbox visibility support. Verify its
   effect in a fresh VS session separately. Do not claim the manifest filters
   project references, or change public visibility to force filtering. If the
   host ignores metadata for discovered helpers, document the actual limit before
   proposing any runtime attributes or extra integration.
3. Keep **RibbonKit** as the VS/Blend category and normal CLR display names. Group
   the manifest for readability; do not promise its order becomes Toolbox order.
   Retain existing/default icons. Custom Toolbox artwork and renamed subcategories
   add no functional authoring value to this slice and are deferred.
4. A documented parent/property is not an automatic drop adapter. Check the native
   package drop path for singleton File surfaces, message bars, item children and
   OptionsPage. Prefer existing semantic editor/property creation. If a valid
   parent still requires a focused ParentAdapter, describe that specific gap for
   review before adding it; generic drag/drop infrastructure remains out of scope.

### Proposed bounded implementation/verification slices

| Slice | Implementation after matrix review | Verification when implemented |
| --- | --- | --- |
| T2a — definitions/discovery | Proposed 31-entry manifest, matching string metadata visibility intent, setup-guide delivery/discovery update | XML/type/duplicate/exclusion checks; normal Release designer/runtime build; normal pack and net8/net9 Design paths; independent package consumer; fresh package and project-reference Toolbox inventories separately |
| T2b — existing creation paths | Fill group context/leaf/scrollbar gaps; minimal safe initializers and shared caption/child defaults; reuse existing application-menu factories | Focused default/ownership checks and existing independent SimplifiedDesignerDefaults scope only where affected; Debug review output; live new drop/Add, explicit values/bindings/empty values, save/reload and one-step undo/redo |
| T2c — newly exposed host/message authoring | Bounded message bar/message creation; safe page captions without guessed Ribbon/host wiring; MdiContainer stays an empty host | Independent authored consumer for new families; property/collection ownership, policy-disabled authoring, existing content/ItemsSource preservation; native message/page/MDI creation and reload/undo/redo |
| T3 — acceptance | No new runtime API assumed; resolve any specific native routing gaps explicitly | Record exactly which package/project-reference routes and changed families were observed; retain previous passing evidence; broader gates only for a concrete API/build/distribution change |

The user approved the 13 additions, including the four reusable page types and
preview MdiContainer, the ScreenTip/window/helper exclusions, and the bounded
native-drop routing review. T2a precedes creation/default changes so discovery can
be assessed separately. No public runtime API is proposed.

T1 evidence was source/manifest inspection and read-only inspection of the installed
SDK metadata. The matrix was checked against the public-type inventory and current
manifest. No builds, tests, package generation, designer launch or changes to
defaults/metadata/runtime behavior were performed during that planning review.

## T2a — implementation and evidence, 2026-10-15

The manifest delivers the approved authoring list. `ToolboxMetadata.Register`
supplies matching ToolboxBrowsable, tab and category attributes for included
types and explicit exclusions for public presentation/root types. This remains
net472/string-type metadata without a runtime-project reference. Contract tests
compare the metadata lists, package manifest, reviewed matrix and the complete
public FrameworkElement inventory. No initializer, Model API factory, context
action, property assignment or runtime control behavior changed.

Package validation now rejects duplicate/invalid Toolbox names and generates an
independent consumer catalog from the packaged manifest. Its optional runtime
smoke check resolves and constructs each advertised type; this does not prove
designer drops or render every new family. The kept consumer's NuGet.Config pins
its private `.packages` directory so a later Visual Studio restore cannot silently
reuse an older global-cache package with the same version.

| Gate | Result |
| --- | --- |
| Focused Release definition contracts | 2/2 passed; 31 unique constructible public authoring types, 14 explicit public visual/root exclusions, all 45 public visual types classified; reviewed matrix and metadata agree |
| Normal Release solution build | Passed, 0 warnings/errors; net8/net9 runtime and net472 DesignTools |
| Normal local pack | Passed; previous current-version package pair preserved before replacement; existing package/deployment paths retained |
| Package contents | Manifest and all four packaged runtime/DesignTools DLL copies match current source/normal Release outputs; archive and designer paths validated |
| Independent package consumer | Clean private-cache net8/net9 Release builds passed, 0 warnings/errors; net8 runtime smoke passed and constructed all 31 advertised controls without Showcase resources/helpers |
| Normal Debug review output | Showcase and kept package-consumer builds passed, 0 warnings/errors. The first Showcase build left a stale net8 designer copy because Visual Studio held it; user closed Visual Studio, the necessary rebuild passed, and both normal Debug Design copies match the newly built DesignTools DLL |
| Live Visual Studio | Accepted 2026-10-15 for the user's observed Toolbox presentation: user reported it looks good in Visual Studio. Reference path was not specified, so separate package/project-reference inventories are not both established. Native drop/default/undo/reload checks remain for T2b/T2c |

Logs are `TestResults/toolbox-t2a-*-20261015.log`. Prior packages and the selected
validation pair are under `TestResults/toolbox-t2a-20261015/`. The retained
`TestResults/package-consumer/PackageConsumer.csproj` uses the selected local
package and private cache, with normal Debug and Release outputs. Its
`runtime-smoke.json` records the net8 constructor/smoke result.

For separate reference-path confirmation, open the retained package consumer and
ConsumerWindow.xaml, and inspect the RibbonKit Toolbox list/category. Separately
open the normal solution/Showcase project-reference designer and check whether
the matching visibility metadata is honored, especially generated hosts/panels
and windows. Record the two results independently before attributing an observed
list to the manifest. T2b/T2c own the remaining creation/default additions and
representative drop/property/reload/undo checks; a focused routing gap needs its
own review rather than an implicit generic ParentAdapter.

No full runtime/Writer/visual/consumer suite was repeated for these bounded
definitions/metadata changes. Earlier passed evidence and residual Crystal/native
gates are retained. No commit, push, merge, publication or scheduling was performed.

## T2b — existing creation paths, 2026-10-15

Toolbox initializers, context Add actions and Ribbon Editor now share one guarded
creation policy through the Model API adapter. Only newly created unassigned
properties receive captions or starter children. `ModelProperty.IsSet` protects
local values, bindings and explicit empty values; collection counts protect
existing children. An assigned `ItemsSource`, including explicit null, prevents
item seeding. Initializers open no edit scopes; editor/context actions retain one
scope per operation. Raw child factories suppress SDK default initialization so
the shared policy runs once rather than duplicating structure. Native SDK mapping,
serialization and undo behavior remain live acceptance gates.

| New item | Starter content |
| --- | --- |
| Ribbon | One Home tab with one Group; a separately created tab gets New Tab/New Group |
| Group and captioned controls/items | A useful Header or Content caption only when unassigned; no guessed size, dimensions, selection, command, icon or host binding |
| Backstage | One Info page |
| Combo and either gallery | One native ComboBoxItem or RibbonGalleryItem captioned Item |
| Split/dropdown | One RibbonMenuItem captioned Menu Item |
| Application menu | One New Command with no pane Content; existing semantic pane/footer factories preserve arbitrary custom content |

The seven supported command families retain Inline/Medium designer defaults.
Native inputs, menu/gallery entries and separators receive no independent
Simplified command defaults. Group context menus now add combo, both galleries,
scrollbar and group separator. Those controls have leaf move/delete registration;
leaf actions resolve the actual Items/Children collection rather than assuming
every control is directly in a group. Ribbon Editor adds Scroll Bar and exposes
its native orientation/range/value fields. Existing stacked Size choices and
application-menu semantic ownership are preserved. No runtime source, public API,
Toolbox inventory or host/message/page authoring behavior changed in this slice.

| Gate | Result |
| --- | --- |
| Focused Release contracts | 44/44 passed: 42 new creation-policy cases and the two definition contracts. Tests execute the compiled net472 policy through reflection without the VS SDK; they cover starter ownership, idempotence, explicit empties, existing children, bindings, ItemsSource/null and supported-family boundaries. They do not exercise ModelItem transactions |
| Independent authored consumer | SimplifiedDesignerDefaults passed: 12 theme/direction/label combinations with native seeded combo/gallery/menu children, plus starter Ribbon/Backstage/application-menu/scrollbar XAML ownership and unchanged empty runtime constructors; no Showcase resources/helpers |
| Normal Release solution build | Passed, 0 warnings/errors; net472 DesignTools and both runtime targets |
| Local pack and package consumer | Archive/layout checks passed; manifest and four runtime/DesignTools DLL entries match normal Release outputs. Clean private-cache net8/net9 consumer Release builds passed, 0 warnings/errors; net8 smoke constructed all 31 advertised types |
| Normal Debug review output | Showcase and net8/net9 package consumer builds passed, 0 warnings/errors. Both runtime Debug Design copies match the new DesignTools DLL; no stale deployment remains |
| Native Visual Studio creation | Pending: fresh-session Toolbox drop and context/editor Add, authored assignments/children, save/reload, property editing and one-step undo/redo. T2a's observed Toolbox presentation acceptance is retained; separate reference-path discovery remains unconfirmed |

Build/package logs, deployment hashes and preserved prior packages/consumer are
under `TestResults/toolbox-t2b-20261015/`. The newly generated independent consumer
is `TestResults/package-consumer/PackageConsumer.csproj`, with normal Debug outputs
and a private package cache. The prior consumer was preserved before regeneration.

For native review, reopen the designer after loading the new DLL/package:

- Drop a new Ribbon/tab and confirm a single starter structure; check captions
  and native item children for combo, galleries and split/dropdown controls.
- Use group right-click Add and Ribbon Editor for the added families; check
  Scroll Bar fields and leaf move/delete both directly in groups and in panels.
- Preserve explicit empty/bound captions, Overflow/Small, Size/selection,
  ItemsSource and existing/custom content across editing and save/reload. Repeat
  initialization must not append placeholders. Feature-disabled authoring stays
  available under the existing explanation.
- Confirm each drop/Add/property edit/move/delete has one undo and redo, then
  reload the saved XAML. Check package and project-reference discovery separately.

No full runtime/Writer/visual suite was repeated for this designer-only slice.
Earlier assembled end-of-feature evidence, the two Crystal QAT comparisons and
remaining native gates are retained. T2c and the host transition API review remain
separate work. No commit, push, merge, publication or scheduling was performed.

## T2 — definitions and designer alignment

- Apply the approved matrix to the manifest and corresponding designer metadata.
  Keep entries unique, correctly qualified and backed by shipped runtime types.
- Add only the missing initializer/context/editor support required for approved
  authoring entries. Preserve the net472/string-type/Model API boundary and one
  undo operation per creation or property edit.
- Retain Inline placement and shown-label defaults for newly created supported
  commands. Existing properties, bindings and explicit Overflow/Small choices
  must survive copy, reload and undo/redo.
- Honor the feature-availability contract agreed in the preceding plan. Define
  whether an unavailable feature's authoring item is hidden or remains available
  with an explanation; either choice must preserve runtime enforcement and the
  author's prepared settings. Do not assume a static manifest can vary per ribbon.
- Preserve existing package/deployment paths. Update the setup guide for the
  actual delivered surface, including package versus project-reference behavior.
  Avoid maintaining a second stale type/count list in general documentation.

## T3 — focused verification and live acceptance

Check manifest XML, duplicate/missing entries and the intended exclusion set.
Build the affected designer/runtime outputs, pack locally and validate the
manifest and DesignTools paths for both runtime targets. Inspect a clean package
consumer that has no Showcase resources or helpers; package inspection alone
does not prove Visual Studio loaded the updated Toolbox definitions.

In a fresh Visual Studio session, verify the package consumer's Toolbox contents,
approved drop locations, representative controls from each changed family,
defaults, saved XAML reload, property editing and undo/redo. Separately check the
project-reference workflow and record its discovery limits. Cover the preceding
capability switch in enabled/disabled states where it affects authoring.

Reuse earlier passing evidence where behavior is unchanged. Run only affected
checks for a bounded definition/metadata change; use the broader API/build/package
gates if implementation changes public API or distribution behavior. Keep live
designer results distinct from builds, package inspection and runtime consumers.

## Sequencing and acceptance

The intended follow-on sequence is feature availability, then this Toolbox
definition refresh, then the
[ribbon/host animation synchronization plan](18-HOST-LAYOUT-TRANSITION-PLAN.md).
T1's include/exclude matrix should be reviewable before T2
implementation. The current S6 defaults/toolbox repair remains accepted in the
[simplified plan](15-SIMPLIFIED-RIBBON-PLAN.md#s6-designer-defaults--2026-10-14).

Simplified end-of-feature evidence is assembled from a full run and targeted
repairs; the two residual Crystal QAT comparisons and specific native gates remain
in [plan 15](15-SIMPLIFIED-RIBBON-PLAN.md#end-of-feature-validation-and-repairs--2026-10-14).
Creating this plan does not start Toolbox implementation, schedule work or
authorize publishing a package.
