# RibbonKit

An open-source, Office Fluent UI–style **Ribbon control library for WPF** on modern .NET (`net8.0-windows` / `net9.0-windows`).

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Target](https://img.shields.io/badge/.NET-8.0%20%7C%209.0-512BD4)](https://dotnet.microsoft.com/)
[![NuGet](https://img.shields.io/badge/NuGet-0.1.0--alpha.1-orange)](https://www.nuget.org/)

> **Status: alpha (`0.1.0-alpha.1`).** The control set is feature-complete for most real applications — ribbon, backstage, galleries, QAT, contextual tabs, KeyTips, a runtime customization dialog with persistence, four Office themes, and a full design-time experience in Visual Studio. The public API is **not frozen yet**; expect renames before `1.0`. See the [roadmap](docs/03-ROADMAP.md).

<!-- Screenshots: drop PNGs in docs/images/ and swap these in.
![Office 2024 theme](docs/images/theme-2024.png)
![Office 2010 theme](docs/images/theme-2010.png)
![Backstage with Mica](docs/images/backstage-mica.png)
![Ribbon Editor (design-time)](docs/images/ribbon-editor.png)
-->

## Why RibbonKit

WPF's built-in `System.Windows.Controls.Ribbon` is visually stuck around Office 2010 and effectively unmaintained. RibbonKit targets modern .NET only, ships **five Office generations from one shared template set**, and is MVVM-first from day one — `ItemsSource` and `DataTemplate` everywhere, dependency properties, routed events, `ICommand`.

Everything renders from vector geometries, never bitmaps, so per-monitor High-DPI scaling is free.

## Feature status

Legend: ✅ done · 🚧 in progress · 📋 planned

### Structure & layout

| Feature | Status |
|---|---|
| Ribbon root, tab strip, tabs, groups | ✅ |
| Adaptive sizing engine (large → medium → small → collapsed) | ✅ |
| Group collapse-to-popup + dialog launcher (↘) | ✅ |
| Minimize mode (double-click tab / chevron / Ctrl+F1) | ✅ |
| Horizontal scroll for tab strip + groups row (chevrons + wheel) | ✅ |
| `RibbonWindow` — QAT and contextual headers in the title bar | ✅ |
| Simplified single-row ribbon | 📋 post-v1 |

### Controls

| Feature | Status |
|---|---|
| `RibbonButton` — large / medium / small | ✅ |
| Toggle, split and drop-down buttons | ✅ |
| `RibbonMenu` / `RibbonMenuItem` with icons and split items | ✅ |
| `RibbonComboBox` (editable + read-only) | ✅ |
| Control groups / button stacks | ✅ |
| Rich ScreenTips (title, body, image, F1 hint) | ✅ |
| `InRibbonGallery` + expandable, resizable popup | ✅ |
| Galleries inside drop-down menus (color / style pickers) | ✅ |
| Live-preview event contract | ✅ |
| Ribbon-styled TextBox / CheckBox / RadioButton | 📋 |

### Application-level

| Feature | Status |
|---|---|
| Application button + application menu | ✅ |
| Backstage view (Modern 2024, Classic 2013, Classic 2010 designs) | ✅ |
| Backstage footer items, button items, recent-items pattern | ✅ |
| Quick Access Toolbar — 3 placements, overflow, right-click add/remove | ✅ |
| Contextual tabs with colored tab groups | ✅ |
| "Customize the Ribbon" + QAT customization dialog (Word-Options style) | ✅ |
| Customization persistence — JSON serialize / restore / reset | ✅ |
| Import / export customization to a file | ✅ |
| Tab merging (`RibbonMergeSource`) | 🚧 next |
| Modal tabs (Print-Preview style) | 🚧 next |

### Input & accessibility

| Feature | Status |
|---|---|
| KeyTips — full chained Alt navigation (Alt → H → F → S) | ✅ |
| Arrow / Tab / F6 keyboard navigation | ✅ |
| UI Automation peers | ✅ |
| RTL support | 📋 |
| Localization of built-in strings (.resx) | 📋 |

### Theming & rendering

| Feature | Status |
|---|---|
| Token-based theme layer (one shared template set) | ✅ |
| Office 2024 theme (default) | ✅ |
| Office 2019 theme | ✅ |
| Office 2013 theme | ✅ |
| Office 2010 theme ("Blue" — gradients, glass buttons, connected tabs) | ✅ |
| Office 2007 theme (Office orb, heavy glass) | 📋 |
| Runtime theme switching + custom accent colors | ✅ |
| Per-monitor v2 High DPI (verified 100 / 125 / 150 / 175 / 200%) | ✅ |
| Mica and Acrylic system backdrops (Windows 11) | ✅ |
| Animation system — tab slide, hover cross-fade, sliding underline, KeyTip pop, scroll glide, with a reduced-motion switch | ✅ |
| Dark mode | 📋 |

### Developer experience

| Feature | Status |
|---|---|
| MVVM — `ItemsSource` + `DataTemplate` throughout | ✅ |
| XAML designer preview (active tab + backstage on the design surface) | ✅ |
| Design-time smart tags / quick actions in Visual Studio | ✅ |
| **Ribbon Editor** design-time dialog with drag-drop tree reordering | ✅ |
| NuGet package bundling the design-tools assembly + toolbox manifest | ✅ |
| Showcase / demo app | ✅ |
| Visual regression snapshot suite per theme × DPI | 📋 |
| Documentation site | 📋 |

### Preview: MDI emulation

WPF has no native MDI. RibbonKit ships an in-window emulation — themed floating child windows with drag, resize, minimize, maximize, close, cascade placement and state animations, driven by an MVVM-friendly document model (`MdiDocument` / `MdiContainer` / `MdiChild`). Milestone M0 is working across all four themes; window-management commands, a switchable tabbed-documents mode, and caption-merge-into-the-ribbon are planned. Design: [`docs/05-MDI-EMULATION-PLAN.md`](docs/05-MDI-EMULATION-PLAN.md).

## Getting started

```xml
<rk:RibbonWindow x:Class="MyApp.MainWindow"
                 xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                 xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                 xmlns:rk="urn:ribbonkit"
                 Title="My App" UseLayoutRounding="True">
  <DockPanel>
    <rk:Ribbon DockPanel.Dock="Top" QuickAccessPosition="BelowRibbon">

      <rk:Ribbon.Backstage>
        <rk:Backstage Design="Modern">
          <rk:BackstageTabItem Header="Info">
            <TextBlock Text="Document properties…" />
          </rk:BackstageTabItem>
        </rk:Backstage>
      </rk:Ribbon.Backstage>

      <rk:RibbonTab Header="Home">
        <rk:RibbonGroup Header="Clipboard">
          <rk:RibbonButton Header="Paste"
                           Size="Large"
                           LargeIcon="{StaticResource Icon.Paste}"
                           ScreenTipTitle="Paste (Ctrl+V)"
                           Command="{Binding PasteCommand}" />
          <rk:RibbonButton Header="Cut"  Size="Small" Icon="{StaticResource Icon.Cut}" />
          <rk:RibbonButton Header="Copy" Size="Small" Icon="{StaticResource Icon.Copy}" />
        </rk:RibbonGroup>
      </rk:RibbonTab>

    </rk:Ribbon>

    <!-- your document area -->
  </DockPanel>
</rk:RibbonWindow>
```

`RibbonWindow` is optional — the ribbon renders self-contained inside a plain `Window`, you just lose the title-bar integration.

Switch themes and accents at runtime — the shared templates read tokens through `DynamicResource`, so swapping the token dictionary re-colors every control instantly:

```csharp
ThemeManager.Apply(Application.Current, RibbonTheme.Office2010);
ThemeManager.SetAccent(Application.Current, Colors.SeaGreen);  // ClearAccent() returns to the theme default
```

## Building from source

Requires Visual Studio 2022 (17.8+) with the .NET desktop development workload, or the .NET 8/9 SDK on Windows.

```
git clone <repo-url>
cd RibbonKit
dotnet build RibbonKit.sln
```

Open `RibbonKit.sln` and set **`RibbonKit.Showcase`** as the startup project — it is a Word-like demo window that exercises every feature and includes a theme switcher, backdrop toggles, gallery and live-preview samples, the customize dialog, and the MDI demo.

Pack the NuGet package (bundles the runtime assembly plus `RibbonKit.DesignTools.dll` and the toolbox manifest):

```
dotnet pack src/RibbonKit/RibbonKit.csproj -c Release
```

Design-time tooling setup is documented in [`src/RibbonKit.Design/SETUP-DESIGNTOOLS.md`](src/RibbonKit.Design/SETUP-DESIGNTOOLS.md).

## Documentation

| Document | Contents |
|---|---|
| [Planning overview](docs/00-PLANNING-OVERVIEW.md) | Vision, guiding principles, technical risks |
| [Architecture](docs/01-ARCHITECTURE.md) | Control hierarchy, sizing engine, theming, subsystems |
| [Features](docs/02-FEATURES.md) | Full feature inventory with priorities |
| [Roadmap](docs/03-ROADMAP.md) | Phased milestones to v1.0 |
| [Design notes](04-DESIGN-NOTES.md) | Living record of every implemented feature, decision and pitfall |
| [MDI emulation plan](docs/05-MDI-EMULATION-PLAN.md) | Design for the in-window MDI control |
| [Merge & modal plan](docs/06-MERGE-AND-MODAL-PLAN.md) | Design for tab merging and modal tabs (Phase 7, in flight) |

## Roadmap to v1.0

Remaining before the API freeze: tab merging and modal tabs (Phase 7), the Office 2007 theme and dark mode, RTL and localization, the visual regression suite, then release engineering — API review, docs site, SourceLink, and a performance pass.

## Contributing

Contributions are welcome — see [CONTRIBUTING.md](CONTRIBUTING.md). One feature per PR, with tests, a showcase page, and a docs snippet.

## License

[MIT](LICENSE)
