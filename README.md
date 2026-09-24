# RibbonKit

An MIT-licensed, Office Fluent UI-style **WPF ribbon library** for
`net8.0-windows` and `net9.0-windows`.

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Release](https://img.shields.io/badge/release-v1.0.0-blue)](https://github.com/Wraith1080/RibbonKit/releases/tag/v1.0.0)

[Getting started](#getting-started) · [Features](#feature-status) ·
[Theming](#theming--rendering) · [Design tools](#developer-experience) ·
[Documentation](#documentation) · [Roadmap](#post-v1-roadmap)

![Showcase in Office 2024 with adaptive groups, gallery and QAT overflow](docs/images/theme-2024.png)

RibbonKit uses lookless controls, WPF commands/binding and one shared template set
for five Office generations. Vector chrome remains sharp when the host enables
per-monitor DPI awareness. Applications own their command icons and document model.

`v1.0.0` is distributed through GitHub Releases, not NuGet.org. The feature summary
below describes the current checkout, including post-release additions; see
[release notes](RELEASE_NOTES.md) for the published package and
[current progress](04-DESIGN-NOTES.md#5-current-state--next-steps) for dated acceptance limits.

## Feature status

### Structure & layout

- Ribbon, tabs, groups, dialog launchers and adaptive large → medium → small → collapsed sizing.
- Collapsed-group flyouts, minimized ribbon, double-click/chevron/Ctrl+F1 toggle.
- Tab/group horizontal scrolling and optional `RibbonWindow` title/QAT/contextual integration.
- Arbitrary WPF group content, with optional `IRibbonSizeAware` participation.
- Simplified single-row ribbon remains a candidate.

### Controls

- `RibbonButton`, toggle, split and dropdown commands in adaptive sizes; button stacks.
- `RibbonMenu`/`RibbonMenuItem`, editable/read-only `RibbonComboBox` and `RibbonTextBox`,
  `RibbonCheckBox`, `RibbonRadioButton` and `RibbonGroupSeparator`.
- `InRibbonGallery`, resizable gallery popups, grouping/filtering and live-preview events.
- ScreenTips with title/body/image/F1 help and chained KeyTips.
- `RibbonScrollBar` preserves native range behavior. Its button, thumb and rail corner
  properties also style native scrollbars created by a `ScrollViewer`.

### Application-level

- File tab or Office 2007 orb, two-pane application menu, and Backstage with Modern,
  Classic, Classic2010, Glass2007 and Classic2007 designs.
- Repeatable message bars with actions/dismissal; Backstage page/footer/recent patterns.
- Three QAT placements, overflow and source-linked button/toggle/split/dropdown proxies.
- Contextual tabs, tab/group merging and modal-tab lifetimes.
- Ribbon/QAT customization with JSON Import/Export/Reset and application-controlled storage.

![Backstage with Windows material](docs/images/backstage-mica.png)

### Input & accessibility

Keyboard arrows/Tab/F6, Alt chains, UIA peers, localized built-in strings and RTL
surfaces have recorded verification. Showcase's Localization/RTL lab exercises
provider refresh, mirroring, popup edges and customization. Application-authored
labels remain the application's responsibility. A complete Windows contrast-theme
mode is **not supported**; gallery/scrollbar system-color fallbacks are narrower.

### Theming & rendering

Office 2007, 2010, 2013, 2019 and 2024 each have light and dark/black palettes, with
live theme/accent switching. Crystal Light adds a light-only palette for the same
shared `Controls.*.xaml` templates. Showcase applies its Crystal QAT, message,
menu and popup presentation when that theme is selected; these host-owned effects
are not installed by `ThemeManager`. The preview's document-under-QAT effect remains
separate.
`RibbonWindow` supports compatible Mica/Acrylic backdrops and separate optional
frame appearance. Theme selection does not silently enable a material.
Contextual tabs can optionally set `RibbonTab.ContextualSelectionBrush` for a distinct
selection marker; null uses the contextual tint. Showcase's experimental Crystal
preview (`--crystal`) demonstrates colored glass headers and markers.
Motion honors reduced-motion settings. Recorded DPI checks include 100/125/150/175/200%
and mixed-monitor scenarios; new changes still need their applicable checks.

![Office 2010 gradient chrome and connected selected tab](docs/images/theme-2010.png)

### Developer experience

MVVM-friendly binding/templates, public API/XML-doc enforcement, deterministic visual
snapshots and the runnable Showcase support development. The separate Visual Studio
design-tools assembly provides context commands, responsive Ribbon Editor, structural
drag/drop and design-only theme/tab/File preview. Its delivery surface is context
menus, **not a floating smart-tag glyph**.

![Visual Studio Ribbon Editor](docs/images/ribbon-editor.png)

See [design-tools setup](src/RibbonKit.Design/SETUP-DESIGNTOOLS.md), including the
[Icons.xaml browser](src/RibbonKit.Design/SETUP-DESIGNTOOLS.md#using-the-iconsxaml-browser).

### Preview: MDI emulation

`MdiContainer`, `MdiDocument` and `MdiChild` provide themed floating child windows.
Setting `MdiContainer.Ribbon` merges the active document's tabs and, when maximized,
its caption controls. `IsCaptionMergeEnabled="False"` keeps tab merge without moving
caption controls. Leaving `Ribbon` unset maximizes inside the client area.

Arrange/cycle commands, full MVVM demonstration, tabbed mode and layout persistence
remain in the [MDI plan](docs/05-MDI-EMULATION-PLAN.md).

## Getting started

Download `RibbonKit.1.0.0.nupkg` from the
[v1.0.0 release](https://github.com/Wraith1080/RibbonKit/releases/tag/v1.0.0) into a local
package folder, then run in your WPF application project:

```powershell
dotnet add package RibbonKit --version 1.0.0 --source C:\path\to\downloaded-packages
```

For source development, reference `src/RibbonKit/RibbonKit.csproj` instead.
Use `urn:ribbonkit`; Office 2024 light is the default. A minimal window:

```xml
<rk:RibbonWindow x:Class="MyApp.MainWindow"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:rk="urn:ribbonkit" Title="My App" UseLayoutRounding="True">
  <DockPanel>
    <rk:Ribbon DockPanel.Dock="Top" QuickAccessPosition="BelowRibbon">
      <rk:RibbonTab Header="Home">
        <rk:RibbonGroup Header="Clipboard">
          <rk:RibbonButton Header="Paste" Size="Large"
              Command="{Binding PasteCommand}" ScreenTipTitle="Paste (Ctrl+V)" />
          <rk:RibbonButton Header="Cut" Size="Small" />
          <rk:RibbonButton Header="Copy" Size="Small" />
        </rk:RibbonGroup>
      </rk:RibbonTab>
    </rk:Ribbon>
    <Grid><!-- application content --></Grid>
  </DockPanel>
</rk:RibbonWindow>
```

Make the code-behind base `RibbonKit.Controls.RibbonWindow`, match `x:Class`, and wire
your commands. Assign application-owned `ImageSource` resources to `Icon`/`LargeIcon`.
A normal WPF `Window` can host the ribbon when title-bar integration is unnecessary.
For File surfaces, galleries and richer command wiring, use the Showcase examples.

### DPI manifest

The host owns process DPI awareness. Copy the complete
[Showcase manifest](samples/RibbonKit.Showcase/app.manifest) and set
`<ApplicationManifest>app.manifest</ApplicationManifest>` in the app project.
It contains the DPI-awareness and Windows compatibility declarations; copying only
one XML fragment can omit required host configuration. Verify actual monitor changes.

### Theme and localization APIs

```csharp
// using RibbonKit.Theming;
ThemeManager.Apply(Application.Current, RibbonTheme.Office2010);
ThemeManager.SetAccent(Application.Current, Colors.SeaGreen);
ThemeManager.SetDarkMode(Application.Current, true);
// ClearAccent returns to the selected theme's default accent.
```

Assign `RibbonLocalization.Provider` to override built-in text; returning null from
`IRibbonLocalizationProvider.GetString` uses the embedded culture fallback.

## Building from source

Use Windows with the .NET SDKs required by the projects and, for designer work,
Visual Studio with the .NET desktop workload. The solution also builds the separate
`net472` design-tools project.

```powershell
dotnet build RibbonKit.sln
dotnet run --project samples/RibbonKit.Showcase/RibbonKit.Showcase.csproj
dotnet run --project samples/RibbonKit.Writer/RibbonKit.Writer.csproj
```

Choose the app you want to inspect. [CONTRIBUTING.md](CONTRIBUTING.md#proportional-validation)
owns focused/full validation and pack/consumer-check commands. Packages go to ignored
`artifacts/`; repository scripts do not publish them. Use
`eng/Measure-ShowcasePerformance.ps1` for outside-debugger Release measurements.

## Documentation

| Reference | Purpose |
| --- | --- |
| [Design notes §5](04-DESIGN-NOTES.md#5-current-state--next-steps) | Current progress, next task and remaining acceptance |
| [Design-history index](04-DESIGN-NOTES.md#3-implemented-features-chronological-with-pitfalls) | Numbered implementation evidence and pitfalls |
| [Architecture](docs/01-ARCHITECTURE.md) | Actual source layout and subsystem contracts |
| [Roadmap](docs/03-ROADMAP.md) | Completed phases and candidate tracks |
| [MDI](docs/05-MDI-EMULATION-PLAN.md) / [merge and modal](docs/06-MERGE-AND-MODAL-PLAN.md) | Service/lifetime contracts and remaining MDI work |
| [Office 2007](docs/07-OFFICE-2007-THEME-PLAN.md) | Retained visual measurements and frame/File boundaries |
| [Custom projections](docs/08-CUSTOM-CONTROL-INTEGRATION-PLAN.md) / [future themes](docs/09-FUTURE-THEMES-PLAN.md) | Provisional designs, not shipped APIs |
| [Writer product](docs/10-RIBBONKIT-WRITER-PLAN.md) / [packet map](docs/11-RIBBONKIT-WRITER-LUNA-EXECUTION-PLAN.md) | Consumer scope and remaining delivery gates |
| [Writer friction](docs/12-RIBBONKIT-WRITER-CONSUMER-FRICTION-LOG.md) | Open investigations, corrections and linked evidence |
| [Visual tests](tests/RibbonKit.VisualTests/README.md) | Deterministic rendering and approval procedure |

## Post-v1 roadmap

Writer includes native persistence, page settings, preview/printing, tables/pictures,
contextual editing and Settings. Paper opens as one centered sheet that grows downward
with the document; its dotted margin guide grows with it. Print Preview and printing
retain physical pagination. View > Continuous provides a workspace-filling editor.
Editable multipage editing was canceled due to bloat. Outstanding acceptance gates and
the distribution decision remain detailed in the current-status page.
Library work includes the remaining MDI milestones and separately scoped projection,
accessibility and theme candidates. Plans do not establish implementation or acceptance.

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md). Keep changes focused and verify the affected
behavior. Public APIs follow the shipped/unshipped compatibility baseline.

## Development provenance

RibbonKit was developed primarily with AI assistants under human direction, visual
review and automated verification. Attribution is **RibbonKit contributors**.

## License

[MIT](LICENSE)
