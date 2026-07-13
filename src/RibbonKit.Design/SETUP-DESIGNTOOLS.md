# RibbonKit.DesignTools — setup & verification

Design-time smart-tag / quick-action support for the **new** WPF XAML designer (Visual
Studio 2019 16.x+ and VS 2022). First cut: toolbox default content + right-click "Add …"
verbs for building a ribbon on the design surface. Runtime behavior is unchanged — this is
tooling only.

## What's here

| File | Role |
| --- | --- |
| `RibbonKit.Design.csproj` | The design-time project. Targets **net472** (VS runs on .NET Framework) and outputs **`RibbonKit.DesignTools.dll`**. Does **not** reference RibbonKit. |
| `Metadata.cs` | `[assembly: ProvideMetadata]` + the attribute table wiring providers to the control types (by string name). |
| `DesignModel.cs` | One-place helpers over the designer Model API (create item + add to content collection). |
| `RibbonDefaultInitializer.cs` | Seeds a dropped `Ribbon` with a starter tab + group. |
| `ContextMenuProviders.cs` | "Add Tab" (Ribbon), "Add Group" (tab), "Add Button/Toggle/Split/Drop-Down" (group). |

## Why net472 + no project reference

The new designer loads extensions **inside the VS process**, which is .NET Framework, and
runs them **isolated from your live .NET 8/9 controls**. So the design assembly must (a)
target .NET Framework and (b) refer to every control by **string type name**, never `typeof`.
That's why `Metadata.cs` uses `"RibbonKit.Controls.Ribbon"` etc. and edits go through the
Model API (`ModelItem`), not real control instances.

## How the designer finds it

The new designer discovers extensions by the **`*.designtools.dll`** name in a **`Design`**
subfolder next to the control assembly. The csproj's `DeployToDesignFolder` target copies the
built dll into:

```
src/RibbonKit/bin/<Config>/net8.0-windows/Design/RibbonKit.DesignTools.dll
src/RibbonKit/bin/<Config>/net9.0-windows/Design/RibbonKit.DesignTools.dll
```

For the **NuGet package**, place it at `lib/<tfm>/Design/RibbonKit.DesignTools.dll` so it
flows to consumers (a `.targets`/`Pack` step for the RibbonKit package — not wired yet).

## Add it to the solution

```
dotnet sln add src/RibbonKit.Design/RibbonKit.Design.csproj
```

Build the design project once, then **close and reopen the XAML designer** (or Roslyn/xdesproc
caches the old metadata). The designer only reloads design assemblies on a fresh open.

## Try it

1. Open the showcase (or any window using `<rk:Ribbon>`) in the VS designer.
2. Drop a `Ribbon` from the toolbox → it should come in with a "Home" tab and a "Group".
3. Right-click the ribbon → **Add Tab**; right-click a tab → **Add Group**; right-click a
   group → **Add Button** (and Toggle/Split/Drop-Down). Each should insert the element and be
   a single Ctrl+Z undo.

## Verify-in-VS checklist (can't be compiled on the Linux build box)

These are the spots most likely to need a one-line tweak against the SDK you have installed —
IntelliSense will confirm each quickly:

1. **Package version.** `Microsoft.VisualStudio.DesignTools.Extensibility` is pinned to
   `17.10.34916.79`; match it to your VS if restore complains.
2. **Namespaces.** `ContextMenuProvider` / `MenuAction` / `MenuActionEventArgs` are expected in
   `Microsoft.VisualStudio.DesignTools.Extensibility.Interaction`. If they don't resolve, hover
   `MenuAction` and fix the `using` (they mirror the old `Microsoft.Windows.Design.Interaction`).
3. **Type creation** (`DesignModel.Create`). `ModelFactory.CreateItem(EditingContext, TypeIdentifier)`
   with `new TypeIdentifier("RibbonKit.Controls", "RibbonTab")`. If the designer can't resolve
   the type, this one line is the thing to adjust (CLR namespace vs xmlns form, or the overload).
4. **Group items collection.** Adding a button uses `group.Content.Collection` (RibbonGroup's
   content is `Items`). If that throws for the group only, switch `DesignModel.AddChild` to use
   `parent.Properties["Items"].Collection` for the group case.

## Deferred to the next cut

- **ParentAdapter** (restrict what can be dropped where — RibbonTab only in Ribbon, etc.). Held
  back because it needs full `CanParent`/`Parent`/`RemoveParent` implementations that are riskier
  to ship without a compile; easy to add once the above is confirmed working.
- The floating **smart-tag adorner panel** (glyph + task list via `AdornerProvider`) — the
  richer WinForms-style UX, a separate follow-up.
- NuGet `Design/` packaging target on the RibbonKit package.
