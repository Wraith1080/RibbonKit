# RibbonKit architecture

Checked against the repository on 2026-09-08. This replaces the original speculative
layout and API sketches. For dated decisions and regressions, follow the numbered
[design-history index](../04-DESIGN-NOTES.md#3-implemented-features-chronological-with-pitfalls).

## Repository layout

| Path | Responsibility |
| --- | --- |
| `src/RibbonKit/Controls` | Lookless controls, KeyTips, customization, merge/modal and MDI services |
| `src/RibbonKit/Layout` | Adaptive group sizing and custom panels |
| `src/RibbonKit/Theming`, `Themes` | Theme manager, token dictionaries and shared templates |
| `src/RibbonKit/Animation`, `Interop` | Motion and native window/backdrop integration |
| `src/RibbonKit/Automation`, `Localization` | UIA peers, built-in strings and provider fallback |
| `src/RibbonKit.Design` | Separate `net472` Visual Studio design tools; no runtime-project reference |
| `samples/RibbonKit.Showcase` | Component, theme, RTL/localization and MDI laboratory |
| `samples/RibbonKit.Writer` | App-owned document, editing, persistence and pagination services |
| `tests/RibbonKit.Tests`, `tests/RibbonKit.Writer.Tests`, `tests/RibbonKit.VisualTests` | Runtime, consumer and deterministic rendering coverage |
| `eng`, `.github/workflows/ci.yml` | Package/performance utilities and Windows build/test/pack validation |

Runtime frameworks are declared in `RibbonKit.csproj`; CI has no publishing step.

## Controls and adaptive layout

`Ribbon` hosts tabs, groups, commands, File surfaces, contextual state and QAT.
Use actual control APIs and the shipped/unshipped public baselines rather than the
old proposed universal `RibbonControl` base or unimplemented status-bar types.
`RibbonGroupsPanel` and `ReductionAlgorithm` reduce groups through permitted sizes
and collapsed flyouts. Keep modal/merge logic out of that engine.
Arbitrary WPF group content is supported; `IRibbonSizeAware` is an optional reduction
hook. Preserve source ownership when a group subtree moves into a popup.

## One template set, dynamic tokens

`Themes/Generic.xaml` supplies the default theme path. `Themes/Office2024.xaml`
aggregates `Controls.*.xaml` for every Office generation; it is a stable shared
composition, not an experimental split. `Tokens.Office*.xaml` and dark dictionaries
supply matching brush/metric/effect keys through `DynamicResource`.

Do not fork per-theme templates. New keys belong in every theme, with neutral values
where unused. Brush tokens may be gradients. Keep dependent `StaticResource` and
`BasedOn` chains within valid dictionary/deferred-template scope; realized popups
and native scrollbars need explicit resource-scope verification (§3.37 and later
consumer corrections). `Controls.Shared.xaml` remains first in the aggregator.

`ThemeManager` swaps dictionaries and reapplies accent/title overrides. Clear old
override keys before deriving the next palette. XAML should use dynamic resources;
code-drawn consumer chrome must invalidate after appearance changes. Animate opacity
and transforms, honoring reduced motion, rather than layout or a token brush's Color.

## Native window boundary

`RibbonWindow` adds title/QAT integration, themed caption controls and optional
backdrops; ordinary `Window` hosting remains supported. The application owns its
PerMonitorV2 manifest. Maximize uses measured work-area inset compensation in DIPs.
A WM_GETMINMAXINFO-only fix was insufficient; preserve the measured inset contract.
Preserve native maximize hit testing and non-client button handling for Snap Layouts;
a WPF hover alone is not the native contract. Theme selection, frame appearance and
DWM backdrop preference are separate choices with an opaque fallback.

## Input, File surfaces and customization

KeyTips and keyboard navigation are control services; avoid duplicating input state
in layout code. Localized built-in text uses embedded resources and
`RibbonLocalization.Provider`, with null results falling back to the current culture.
Application command text remains app-owned.

Backstage placement depends on its design; Classic2010 starts below the live tab row.
Office 2007's two-pane application menu is a distinct control. Galleries combine
strip/popup presentation and live-preview semantics. Test realized popups, not only
resource declarations.

QAT proxies for buttons, toggles, splits and dropdowns stay linked to source commands.
Overflow, borrowed menus, disabled state and merge parking share the source lifetime.
Group/gallery/combo projections remain candidates in the [integration plan](08-CUSTOM-CONTROL-INTEGRATION-PLAN.md).
`RibbonCustomizationSerializer` owns structural layout; applications own storage and
separate appearance/document settings. Stable command IDs support persistence.

## 8. Merge, modal and MDI

[Merge/modal contracts](06-MERGE-AND-MODAL-PLAN.md) isolate transient source insertion,
authored visibility, QAT parking and layout restoration from persisted customization.
Refresh the selected-tab underline and connected notch after changes.
[MDI](05-MDI-EMULATION-PLAN.md) reuses these services for active-document tabs and
maximized-child caption controls; arrange/tabbed/persistence work remains separate.

## Design tooling and verification

`RibbonKit.DesignTools.dll` uses type-name strings and the Visual Studio Model API.
It is packaged under each runtime assembly's `Design` folder. Follow the
[setup guide](../src/RibbonKit.Design/SETUP-DESIGNTOOLS.md) for installation and the
Icons.xaml browser; designer startup alone is not interaction acceptance.

[CONTRIBUTING.md](../CONTRIBUTING.md#proportional-validation) owns validation commands.
Automated layout/STA tests, deterministic snapshots and actual-window input/DPI/IME
checks establish different evidence. Complete Windows contrast-theme support is
still unclaimed; isolated system-color fallbacks are narrower contracts.
