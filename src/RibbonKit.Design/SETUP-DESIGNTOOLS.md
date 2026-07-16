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
Tabs → Groups → items and a toolbar (Add Tab · Add Group · Add Control ▾ · Add Stack · Move Up/Down ·
Delete) plus a Header rename box. A group's items can be leaf controls OR **layout containers**
(`StackPanel`) that hold their own children, so the tree recurses into any node with a `Children`
collection — matching the Office pattern of a vertical stack of horizontal icon rows. "Add Stack"
inserts a `StackPanel` (vertical in a group, horizontal inside another stack); "Add Control" targets
whatever's selected (a group's `Items`, a container's `Children`, or as a sibling of a control) and
defaults stacked buttons to `Size="Small"`. Container nodes expose an `Orientation` editor. The
"Add Control ▾" menu covers Button / Toggle / Split / Drop-Down (each gets a Header caption) plus Combo
Box, Gallery (in-ribbon / drop-down), and Separator (no caption); creation tries the RibbonKit xmlns
first, then WPF framework namespaces (so `Separator` works too). The tree also descends into **item
containers** — combo boxes, galleries, and the **split / drop-down buttons** (their `Items`, which are
`RibbonMenuItem`s) — and surfaces the **Backstage** (the File menu) as its own root node with editable
nav items. **Add Item** creates the right child for the selected container (a `ComboBoxItem`,
`RibbonGalleryItem`, a split/drop-down `RibbonMenuItem`, or backstage `BackstageTabItem`). The
**Caption** box edits `Header`, `Content`, or (for gallery items, whose Content is a visual) `Tag` —
whichever carries simple text — so it names buttons, tabs, backstage pages, combo items, and gallery
items (shown by their Tag) alike. Property editors are also type-aware: a backstage page shows
`IsButton` / `Placement`, a combo box shows `InputWidth` / `IsEditable`, on top of the shared
control/tab/group editors. Tabs, groups, and command controls also get two **attached-property** rows —
**Command Id (persistence)** editing `Ribbon.CommandId` (the stable identity the customization serializer
uses to persist/restore layout) and **KeyTip (Alt access key)** editing `KeyTip.Keys` (the access-key
badge shown on `Alt`; blank lets the ribbon auto-derive one from the label, like Office). Because
attached members don't surface through the normal `Properties[name]` indexer (it only sees an element's
own members), `DesignModel.FindAttached(item, ownerType, name)` resolves them by a type-qualified
`PropertyIdentifier`, binding the collection accessor by reflection and logging which shape worked —
confirmed on Windows as `Find(PropertyIdentifier)` (same spike style as the StaticResource icon path).
Both rows are hidden on entries inside a combo/gallery/menu/backstage (those carry neither a persistence
identity nor a surface KeyTip); clearing either box removes the attribute. Color properties (`ContextualColor`, a TextBlock's `Foreground`) use a
**color editor** — a live swatch + hex/name box + a "…" button opening a palette picker
(`ColorPickerDialog`, self-contained WPF, no WinForms). The tree recurses into Panels (`Children`) and item
containers (`Items`, i.e. combos/galleries/split+drop-down menus/backstage) but NOT into a control's `Content` (expanding
every page/gallery item's visual tree was too noisy). A combo item's text lives in its `Content` (a
string) and is edited via the **Caption** box. `TextBlock` editors and an "Add Text Block" menu entry
exist for text added directly into a group's panels. A **Show backstage** checkbox (next to Preview tab)
opens the backstage on the surface design-only — same `DesignModeValueProvider` path as the tab
preview, now covering `IsBackstageOpen` too; it enables only when the ribbon has a backstage. A
**Page** combo beside it previews a specific backstage page: it lists the nav pages (footer action
buttons excluded) and drives the backstage's `SelectedIndex` through a second provider
(`BackstagePagePreviewProvider`, attached to `Backstage`) the same design-only way — enabled only while
the backstage is shown, "(default)" clears the override. Because `SelectedIndex` is inherited from
`Selector`, the provider registers (and the coordinator invalidates) under both the `Backstage` and
`Selector` declaring types, since which one the designer reports for an inherited DP is unverified. It runs in-process on the VS UI thread (only the design
*surface* is process-isolated, not extension code), so it's a plain code-built WPF `Window`
(the design assembly can't reference RibbonKit's themes). Every change is applied straight to
the `ModelItem` tree through `DesignModel`, each as its own single undo — same transaction model
as the verbs, no OK/Cancel wrapper. The surface updates live.

The dialog also has a **per-item property panel** that shows editors for the selected node,
skipping any property the type doesn't have (via `DesignModel.HasProperty` / `FindProperty`):

- Controls (button/toggle/split/drop-down): `Size` (Large/Medium/Small), `SizeDefinition`,
  `ScreenTipTitle`, `ScreenTipText`.
- Tab: `IsContextual`, `ContextualColor` (typed as a name or `#hex`, applied through the brush converter).
- Group: `ShowDialogLauncher`, `ReductionMode` (Collapse/ResizeThenCollapse/Resize), `CanResize`.

Enum and brush values are set as strings and resolved by the property's type converter (same trick
as the QAT verb's enum set); an invalid value is logged, not thrown. Each edit is its own undo.

Controls also have **Icon / Large icon** editing (verified working). Each row shows the current
resource key and has a "…" button that opens the **icon picker** (`IconPickerDialog`):

- It always lists the icon keys **already used elsewhere in this ribbon** (no file needed).
- **"Load Icons.xaml…"** browses to the project's icon dictionary; it's parsed in-process
  (`XamlReader.Load`) so the icons show as real **thumbnails**, and the dictionary is cached for the
  session (`IconCatalog`). A filter box narrows the grid; the current icon is highlighted.
- Picking a tile (or typing a key + Set) writes `{StaticResource key}` via `DesignModel.SetStaticResource`,
  which builds a StaticResource **ModelItem** with `ResourceKey` set in the model — a raw
  `StaticResourceExtension` object loses its key when the model serializes it, so that indirection is
  required. `GetStaticResourceKey` reads the current key back.

The extension can't auto-discover Icons.xaml (resources live in the isolated surface process; there's
no document-path service), which is why the file is loaded once via the browse button per session.

## Diagnostics (`DesignLog`)

The design tooling has no console and usually no attached debugger, so `DesignLog` appends to a
log file: **`%LOCALAPPDATA%\RibbonKit\DesignTools.log`** (falls back to `%TEMP%`). The "Edit
Ribbon…" verb is wrapped in try/catch — if the dialog can't open it logs the full exception and
shows a MessageBox with the log path. The dialog's tree reads are defensive: each tab / group /
control node is read in isolation, so a control type or property the reader can't handle is logged
(with the offending type) and skipped rather than aborting the whole editor. To investigate an
editor problem, reproduce it, then open the log — the last lines show how far construction got and
any `ERROR …` entry names the item that failed. `DesignLog.Enabled = false` silences it; it's a
development aid — gate or remove before shipping.

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
