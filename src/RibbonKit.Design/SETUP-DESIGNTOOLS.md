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
| `DesignModel.cs` | ModelItem edits, collection/attached-property handling and undo transactions |
| `RibbonDefaultInitializer.cs`, `ContextMenuProviders.cs` | Toolbox starter content and context commands |
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

## Editor behavior

Dropping a Ribbon seeds Home/Group. Context commands add/reorder/delete tabs, groups,
controls and File surfaces, and select QAT placement. **Edit Ribbon…** opens a structure
tree with contextual Add/Move/Delete actions and separate Properties/Design Preview tabs.
Edits apply immediately to the model as individual undo transactions, not through an
OK/Cancel staging wrapper. Drag/drop type-checks destinations and rejects descendants.

The tree follows group items, panel children and supported combo/gallery/menu/File
collections, not arbitrary visual Content graphs. Caption edits Header/Content/Tag
as appropriate; stacks expose orientation. Type-aware editors cover sizing, ScreenTips,
input state/width, contextual color, group reduction/launcher and File navigation fields.
`Ribbon.CommandId` and `KeyTip.Keys` use type-qualified attached-property lookup; clearing
the field removes the attribute. Enum/brush conversion errors are logged.

Application menu is a singleton root with command, pane, separator and footer items.
Managed standard pane slots are editable; arbitrary content is shown as custom content
for XAML editing and is never flattened or overwritten. Group separators are adaptive
chrome without captions. Preview theme changes palette/metrics, not authored structural
choices such as File shape.

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

The package toolbox manifest filters authorable controls. Project-reference consumers
can still see reflected public controls; package-only filtering is not evidence about
the in-solution Toolbox. Consult the manifest instead of maintaining a duplicate count.

Known/deferred boundaries: no floating smart-tag glyph in the isolated designer;
no general scalar reset/icon-clear action (remove the XAML attribute); no ParentAdapter
valid-drop extension or design-time Add to QAT for runtime proxies. The manual icon
browser remains the fallback for automatic discovery ambiguity/failure.
