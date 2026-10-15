# RibbonKit.DesignTools — WPF XAML designer support

This guide describes repository tooling, not a claim of fresh validation on every
Visual Studio version. Dated installation/interaction evidence remains in the
[design-history index](../../04-DESIGN-NOTES.md). The delivered surface is context
commands and Ribbon Editor; a floating smart-tag adorner is not supported.

## Files and loading boundary

| File | Role |
| --- | --- |
| `RibbonKit.Design.csproj` | `net472`, output `RibbonKit.DesignTools.dll`, no runtime-project reference |
| `Metadata.cs`, `PropertyMetadata.cs` | String-type metadata registration and property descriptions |
| `ToolboxMetadata.cs` | String-type Toolbox visibility/category intent, checked against the package manifest |
| `DesignModel.cs`, `DesignModel.NewItems.cs`, `NewItemDefaults.cs` | ModelItem edits, guarded creation policy, collection/attached-property handling and undo transactions |
| `RibbonDefaultInitializer.cs`, `RibbonTabDefaultInitializer.cs`, `RibbonItemDefaultInitializer.cs`, `ContextMenuProviders.cs` | Toolbox starter content and context commands |
| `SimplifiedCommandInitializer.cs` | Supported toolbox command defaults, shared with Model API creation |
| `RibbonTabsPlacementAdapter.cs` | Collection-owned placement for Ribbon children without free-form size/position writes |
| `RibbonEditorWindow.cs`, `TabPreview.cs` | Structure editor and design-only preview coordination |

The extension works through the Visual Studio Model API, not live .NET 8/9 control
instances. Preserve string type names and isolation. SDK reference assemblies are
provided by Visual Studio and are not runtime dependencies of the library.

## Setup and packaging

The project is already in `RibbonKit.sln`; do not add it again. Build the solution
with the desktop workload and required SDKs, then reopen the XAML designer after a
design-tools change. Fully exit Visual Studio for a clean installed-package check
if its assembly cache or DLL lock could hide the changed build.

Discovery uses `Design/RibbonKit.DesignTools.dll` beside `RibbonKit.dll`.
`DeployToDesignFolder` copies it into the runtime target output folders. The runtime
project's build-only reference uses `ReferenceOutputAssembly=false`,
`UndefineProperties=TargetFramework` and `SkipGetTargetFrameworkProperties=true`;
preserve them so runtime target properties do not force the design project off net472.

Package layout includes:

- `lib/net8.0-windows7.0/RibbonKit.dll` and the net9 equivalent;
- each runtime folder's `Design/RibbonKit.DesignTools.dll`;
- `tools/VisualStudioToolsManifest.xml`, the package-only toolbox allowlist.

Update both deployment and package paths if runtime targets change. The `windows7.0`
NuGet folder spelling is the target-platform suffix, not a target-machine acceptance
claim. Use [CONTRIBUTING.md](../../CONTRIBUTING.md#proportional-validation) for pack and
validation commands. `eng/Validate-Package.ps1 -RunConsumer -KeepConsumer` provides the
isolated package consumer used for live validation. The 2026-08-11 recorded VS 2026
18.7.1 check proved packaged context commands and Ribbon Editor; it is historical evidence.

## Toolbox discovery and placement

The [package manifest](../RibbonKit/tools/VisualStudioToolsManifest.xml) declares
the authoring list under **RibbonKit** in Visual Studio and Blend. It includes
the ribbon/commands, Backstage and application-menu families, options/customization
pages, messages, group separators and the preview MDI host. Windows/dialog roots,
generated hosts, layout panels, automatic ScreenTips and nonvisual definitions
stay outside that list. See the reviewed
[type/parent matrix](../../docs/17-TOOLBOX-ITEM-DEFINITION-PLAN.md#reviewed-include-matrix)
for individual destinations.

Package and project references have separate discovery paths:

- A **package reference** uses `tools/VisualStudioToolsManifest.xml` for its
  allowlist. Archive/type/consumer validation does not prove the Visual Studio
  Toolbox loaded it; use a fresh session and the newly built package.
- A **project reference** reflects public controls. DesignTools now registers
  matching ToolboxBrowsable visibility intent and RibbonKit tab/category metadata
  by type-name string. The manifest itself does not filter project references.
  The actual result depends on the designer loading the updated DesignTools DLL
  and honoring its metadata; record that result separately from package discovery.
  If helpers remain visible, inspect cache/loading before assuming this metadata
  is supported by that discovery path. Public runtime types remain available.

Discovery does not route a drop into a semantic property. Application menus and
Backstage belong on their Ribbon singleton properties; menu/pane/footer entries,
options pages and messages belong in their matching collections/content slots.
Use the existing semantic Ribbon Editor/context actions or property-element XAML
when a direct drop cannot choose that slot. No ParentAdapter was added by the
definition refresh. Customization pages require the host's explicit Ribbon and
baseline assignments; MdiContainer requires host documents. No application
bindings, event handlers or persistence are guessed.

The T2b creation slice shares guarded starter defaults across Toolbox drops,
context actions and the editor. Message/page/host authoring additions remain a
subsequent bounded slice in the Toolbox plan. A disabled runtime feature does not
remove authored types/settings from the Toolbox.

## Editor behavior

Dropping an unassigned empty Ribbon seeds Home/Group; a new standalone tab seeds
New Tab/New Group. New Backstage and application-menu roots receive one Info page
or plain New Command. Combo/gallery and split/dropdown controls receive one native
item, without guessing selection or pane content. Assigned values, explicit empties,
bindings, ItemsSource and existing children block the corresponding default writes.
Initializers run inside the creation transaction and open no nested undo scopes.
RibbonTab opts out of generic designer initialization before applying its own
defaults, preventing the inherited TabItem Grid on new drops. Placement is a
separate phase: Ribbon's child placement adapter declines geometry writes, and
RibbonTab's DefaultSizeMode.None opts out of default sizing. This does not clear
content or dimensions already written into existing XAML. Live drop/reload/undo
acceptance is tracked in the Toolbox plan separately from SDK metadata checks.
Context commands add/reorder/delete tabs, groups,
controls and File surfaces, and select QAT placement. **Edit Ribbon…** opens a structure
tree with contextual Add/Move/Delete actions and separate Properties/Design Preview tabs.
Edits apply immediately to the model as individual undo transactions, not through an
OK/Cancel staging wrapper. Drag/drop type-checks destinations and rejects descendants.

The tree follows group items, panel children and supported combo/gallery/menu/File
collections, not arbitrary visual Content graphs. Caption edits Header/Content/Tag
as appropriate; stacks expose orientation. Type-aware editors cover sizing, ScreenTips,
input state/width, contextual color, group reduction/launcher and File navigation fields.
Group context Add includes combo, both galleries, scrollbar and group separator;
leaf move/delete resolves group Items or panel Children. The editor also offers
Scroll Bar with native orientation, range, value and change/viewport fields.
Supported command controls added through the toolbox, context commands or Ribbon
Editor start inline in the simplified ribbon with their label shown. The editor's
**Show in simplified ribbon** checkbox writes `Ribbon.SimplifiedPlacement` as
Inline/Overflow; **Show simplified label** writes `Ribbon.SimplifiedSize` as
Medium/Small. These are authored runtime settings, each with one undo operation.
Existing authored values are preserved. Native editors and unsupported option or
menu-entry types remain available through full-group flyouts. These creation
defaults do not change unconfigured runtime commands' Overflow/Medium defaults.
`Ribbon.CommandId` and `KeyTip.Keys` use type-qualified attached-property lookup; clearing
the field removes the attribute. Enum/brush conversion errors are logged.

Application menu is a singleton root with command, pane, separator and footer items.
Managed standard pane slots are editable; arbitrary content is shown as custom content
for XAML editing and is never flattened or overwritten. Group separators are adaptive
chrome without captions. Preview theme changes palette/metrics, not authored structural
choices such as File shape.

### Feature policy authoring

Select the Ribbon root in Ribbon Editor to edit the eight feature-policy Booleans,
also described in the Properties grid. These are ordinary persisted runtime
properties, not preview overrides. Simplified placement/label values remain
editable while the feature is disabled; the editor explains their inactive state.
Each explicit edit uses the existing single Model API undo scope. Live drop,
property binding, undo/redo and reload acceptance remains distinct from build and
authored-XAML consumer checks.

### Using the Icons.xaml browser


Controls and supported application-menu items have **Icon** and, where applicable, **Large icon**
rows in the Ribbon Editor. The browser expects WPF `ImageSource` resources; keyed `DrawingImage`
entries are recommended because they remain sharp at every DPI. A minimal `Icons.xaml` looks like
this:

```xaml
<ResourceDictionary
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <DrawingImage x:Key="Icon.Save">
        <DrawingImage.Drawing>
            <GeometryDrawing Brush="#0F6CBD" Geometry="M3,2 H21 V22 H3 Z" />
        </DrawingImage.Drawing>
    </DrawingImage>
</ResourceDictionary>
```

Merge the dictionary into application, window, or another ancestor resource scope before assigning
its keys. Loading the file into the browser provides thumbnails only; it does **not** make the
resources available to the running application. For example, in `App.xaml`:

```xaml
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <ResourceDictionary Source="Icons.xaml" />
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Application.Resources>
```

To assign an icon in Visual Studio:

1. Select the `Ribbon` on the XAML design surface, right-click it, and choose **Edit Ribbon…**.
2. Select the button, menu item, or other supported node in the editor's structure tree.
3. In **Properties**, find **Icon (resource key)** or **Large icon (resource key)** and click **…**.
4. The picker automatically checks the active XAML project for a single `Icons.xaml`. When it reports
   **auto-loaded**, filter or scroll to the icon. Otherwise click **Load Icons.xaml…** and select the
   intended dictionary manually.
5. Click a tile. The editor immediately writes the corresponding static-resource reference, for
   example `Icon="{StaticResource Icon.Save}"`, as one undoable designer edit.

The key can also be typed into the property row and applied with **Set**. Without loading a file, the
browser still offers keys already used elsewhere in the current ribbon, but without thumbnails. The
loaded file is cached for the current design-tools session. On the next Visual Studio session the
automatic check runs again; the browse button always remains available when no match is found, the
project contains multiple files with that name, or parsing fails. Keep browseable icon entries directly
in the selected dictionary, and use string `x:Key` values. Only `ImageSource` values receive previews.
To remove an assignment entirely, delete the `Icon` or `LargeIcon` attribute in XAML; the current
editor only sets or replaces icon references.

The extension still can't enumerate application resources directly because they live in the isolated
design-surface process. Instead, it reads the active document and solution paths from the DTE object
registered to the current Visual Studio process, looks beside the document and then within its project,
and loads only a single unambiguous match. No EnvDTE assembly is added to the package. The selected
dictionary is parsed in-process with `XamlReader.Load`, cached in `IconCatalog`, and written as a
`{StaticResource key}` model item through `DesignModel.SetStaticResource`. See the working catalog in
[`samples/RibbonKit.Showcase/Icons.xaml`](../../samples/RibbonKit.Showcase/Icons.xaml) and its merge in
[`samples/RibbonKit.Showcase/App.xaml`](../../samples/RibbonKit.Showcase/App.xaml).

## Design-only previews

Theme, active tab, File surface and page/pane choices use primitive design-preview
values and `DesignModeValueProvider`, without serializing overrides or changing
application resources. File surface is one atomic closed/Backstage/application-menu
choice; keep authored objects intact. Object-valued null/UnsetValue substitutions
previously crashed or poisoned isolated-designer model reads.

Providers are lazy: explicitly invalidate the appropriate model property when the
selection changes. Backstage inherited SelectedIndex registration must cover the
actual declaring-type paths. Reset clears the preview; session previews disappear
when the designer reloads. Hand-authored `d:SelectedIndex`/`d:IsBackstageOpen` remain
alternatives, but the Model API does not expose a general design-namespace write path.

The editor is a plain WPF window on the VS UI thread; the surface is isolated.
Live File-surface cycling and subsequent model editing must remain healthy. Its DPI
refresh and scrollable inspector need actual-window checks after layout changes.

## Diagnostics and limits

`DesignLog` writes `%LOCALAPPDATA%\RibbonKit\DesignTools.log` (TEMP fallback).
Failures include context/type details; individual unreadable nodes are skipped rather
than aborting the editor. Check the latest error after reproducing a failure. Logging
is currently enabled by default in code; this guide does not claim a release-time gate
has disabled it.

The package manifest filters authorable controls. Matching project-reference
metadata is an integration intent until checked in the active designer host;
package filtering alone is not evidence about the in-solution Toolbox. Consult
the manifest instead of maintaining a duplicate count. A locked DesignTools DLL
may leave a successful build's deployed copy stale: close Visual Studio, rebuild
in the normal configuration, and compare deployment copies before live review.

Known/deferred boundaries: no floating smart-tag glyph in the isolated designer;
no general scalar reset/icon-clear action (remove the XAML attribute); no ParentAdapter
valid-drop extension or design-time Add to QAT for runtime proxies. The manual icon
browser remains the fallback for automatic discovery ambiguity/failure.
