# RibbonKit.DesignTools — design-time support for the WPF XAML designer

Design-time tooling for the **new** WPF XAML designer (Visual Studio 2019 16.x+ / VS 2022):
toolbox default content, right-click verbs for building a ribbon on the surface, and a polished
Properties window. Runtime is untouched — this is tooling only. **All features below are verified
working in VS.**

## Files

| File | Role |
| --- | --- |
| `RibbonKit.Design.csproj` | The design project. Targets **net472** (VS runs on .NET Framework), outputs **`RibbonKit.DesignTools.dll`**. Does **not** reference RibbonKit. |
| `Metadata.cs` | `[assembly: ProvideMetadata]` + the attribute table wiring providers and property metadata to the control types (by string name). |
| `DesignModel.cs` | Helpers over the Model API: create an item, add to a named collection, reorder, delete. |
| `RibbonDefaultInitializer.cs` | Seeds a dropped `Ribbon` with a starter tab + group. |
| `ContextMenuProviders.cs` | All the right-click verbs (see below). |
| `PropertyMetadata.cs` | Properties-window categories + descriptions. |

## Why net472 + no project reference

The new designer loads extensions **inside the VS process** (.NET Framework) and runs them
**isolated from your live .NET 8/9 controls**. So the design assembly targets .NET Framework and
refers to every control by **string type name** (never `typeof`); all edits go through the Model API
(`ModelItem`), not real control instances.

## Discovery

The new designer finds extensions by the **`*.designtools.dll`** name in a **`Design`** subfolder
next to the control assembly. The csproj `DeployToDesignFolder` target copies the built dll into
`src/RibbonKit/bin/<Config>/net{8,9}.0-windows/Design/`. For the NuGet package it belongs at
`lib/<tfm>/Design/` (packaging not wired yet).

## Setup

```
dotnet sln add src/RibbonKit.Design/RibbonKit.Design.csproj
```

Build `RibbonKit.Design`, then **close and reopen the XAML designer** (it caches design assemblies;
an in-place rebuild won't reload).

## What you get

**Toolbox default:** dropping a `Ribbon` seeds a "Home" tab with a "Group".

**Right-click verbs:**

- Ribbon — **Edit Ribbon…** (opens the structure editor dialog) · Add Tab · Add Backstage (once; also surfaces the File button) · Quick Access Toolbar ▸ Title Bar / Tab Row / Below Ribbon (checked on current).
- Tab — Add Group · Move Tab Left/Right · Delete Tab.
- Group — Add Button / Toggle / Split / Drop-Down · Move Group Left/Right · Delete Group.
- Button/Toggle/Split/Drop-Down — Move Control Left/Right · Delete Control.
- Backstage — Add Nav Item (page) · Add Nav Button (footer action). Select the backstage via the Document Outline or `d:IsBackstageOpen="True"`.

Every verb is a single undo.

**Ribbon Editor dialog (`RibbonEditorWindow`):** "Edit Ribbon…" opens a modal with a tree of
Tabs → Groups → Controls and a toolbar (Add Tab · Add Group · Add Control ▾ · Move Up/Down ·
Delete) plus a Header rename box. It runs in-process on the VS UI thread (only the design
*surface* is process-isolated, not extension code), so it's a plain code-built WPF `Window`
(the design assembly can't reference RibbonKit's themes). Every change is applied straight to
the `ModelItem` tree through `DesignModel`, each as its own single undo — same transaction model
as the verbs, no OK/Cancel wrapper. The surface updates live.

## Tab preview via DesignModeValueProvider (design-only, no runtime leak)

The editor's **Preview tab** picker shows any tab on the design surface without changing your
XAML or the running app. Mechanism (`TabPreview.cs`):

- `SelectedTabPreviewProvider : DesignModeValueProvider` is registered on `Ribbon.SelectedIndex`.
  When a preview is active it returns the chosen index from `TranslatePropertyValue`, so the
  surface renders that tab. Nothing is serialized, and the migration docs note the provider is
  never invoked for run-time code — so the running app is unaffected.
- The new designer calls the provider **lazily** — only on `ValueTranslationService.InvalidateProperty`
  or when the property is edited in the designer, **NOT on initial load** (confirmed on Windows:
  a load-time-only probe did nothing). So the picker drives it explicitly: `TabPreviewCoordinator.Set`
  stores the chosen index and calls
  `ribbon.Context.Services.GetRequiredService<ValueTranslationService>().InvalidateProperty(ribbon, selectedIndexId)`,
  which makes the surface re-evaluate and repaint. "(no preview)" clears it.

This is the supported equivalent of a hand-authored `d:SelectedIndex`: a literal `d:` attribute
can NOT be written programmatically (the model API has no design-namespace write path), so the
value provider is the route to the same design-only, no-runtime-leak result. Because the preview
lives in design-session state (not the XAML), it resets when the designer is reloaded.

**Properties window:** the main controls' key properties are grouped under a "RibbonKit" category
with descriptions. `IsBackstageOpen` is hidden from the grid (it would persist to runtime); preview
it design-time-only with `d:IsBackstageOpen="True"` in XAML. `SelectedIndex` stays visible with a
runtime-vs-preview note; preview a tab with `d:SelectedIndex="N"`.

## Toolbox cleanup (package-only)

The new designer populates the Toolbox from a NuGet-package **`tools\VisualStudioToolsManifest.xml`**
allowlist (NOT `ToolboxBrowsableAttribute`). It's in the RibbonKit project and packed into the
package, listing only the 14 authorable controls under a "RibbonKit" tab. **It only takes effect when
RibbonKit is consumed as a NuGet package** — in a project-reference setup the designer still reflects
all public controls, so it has no effect on the in-solution showcase.

## Known limit: no smart-tag adorner glyph

A floating smart-tag glyph on the surface (via `AdornerProvider`) was attempted and **does not render
in the new designer**: the provider activates and `Adorners.Add` succeeds, but the new
surface-isolation designer renders the surface in a separate process and does not host custom adorner
visuals. The context-menu verbs are the delivery surface for these actions. (See design notes §3.22.)

## Deferred

- `DesignModeValueProvider`-based design-only preview toggles (the one avenue left for a togglable
  tab/backstage preview — design-time *values* render, unlike adorner overlays).
- `ParentAdapter` (valid-drop rules); design-time "Add to QAT" (QAT items are runtime proxies, not
  plain XAML); NuGet packaging (`lib/<tfm>/Design/` dll + the toolbox manifest going live).
