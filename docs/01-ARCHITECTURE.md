# Architecture Plan

## 1. Solution & repository layout

```
/ (repo root)
├── src/
│   └── <LibraryName>/                  # single control library, net8.0-windows;net9.0-windows
│       ├── Controls/                   # one file per control
│       ├── Layout/                     # sizing engine, panels, size definitions
│       ├── Theming/                    # theme manager, resource keys
│       ├── Input/                      # KeyTips, keyboard navigation
│       ├── Automation/                 # UIA peers
│       ├── State/                      # QAT/customization persistence
│       └── Themes/
│           ├── Generic.xaml            # default (Office 2024) templates
│           ├── Office2007/ Office2010/ Office2013/ Office2019/ Office2024/
│           └── Shared/                 # tokens: brushes, metrics, geometry icons
├── samples/
│   └── Showcase/                       # demo app exercising every feature + theme switcher
├── tests/
│   ├── UnitTests/                      # layout math, size reduction, state serialization
│   └── VisualTests/                    # rendered snapshot comparisons per theme/DPI
├── docs/                               # these planning docs + user documentation
└── .github/workflows/                  # CI: build, test, pack, publish
```

## 2. Control class hierarchy

```
Ribbon (Control)
├── QuickAccessToolbar
├── ApplicationButton  ──►  ApplicationMenu (simple)  |  Backstage (full-window)
├── RibbonTabControl
│   ├── RibbonTab (regular / contextual / modal)
│   │   └── RibbonGroup
│   │       ├── RibbonButton              (Large / Medium / Small size states)
│   │       ├── RibbonToggleButton
│   │       ├── RibbonSplitButton
│   │       ├── RibbonDropDownButton
│   │       ├── RibbonComboBox
│   │       ├── RibbonTextBox / RibbonCheckBox / RibbonRadioButton
│   │       ├── InRibbonGallery  ──►  expands to GalleryPopup
│   │       ├── RibbonSeparator / RibbonControlGroup (button stacks)
│   │       └── GroupDialogLauncher (small ↘ button in group corner)
│   └── ContextualTabGroup (colored header spanning related tabs)
└── RibbonStatusBar (optional, later)
```

Shared base: `RibbonControl` abstract class providing `Size` (Large/Medium/Small), `SizeDefinition` (allowed reduction sequence), `Header`, `Icon`/`LargeIcon`, `ScreenTip`, `KeyTip`, `ICommand` plumbing. All items containers support `ItemsSource` + templates for MVVM.

## 3. The adaptive sizing engine (critical subsystem)

A custom panel (`RibbonGroupsPanel`) measures groups at their preferred size and, when width is insufficient, applies **reduction steps** in a declared order (e.g. `"LargeGroup, MediumGroup, SmallGroup, Collapsed"` per group, with a tab-level priority order for *which* group shrinks first). Each control declares its own `SizeDefinition` ("Large, Middle, Small") describing how it renders at each group state. Final step collapses a group to a single dropdown button that opens the full group in a popup. Requirements: no layout loops, single measure pass per width change where possible, smooth behavior during window resize, and correct interaction with the minimized ribbon. **Prototype this in Phase 1** with plain buttons before any other feature depends on it.

## 4. Theming system

Two-layer design: **templates are theme-agnostic**; they reference a token layer (`ComponentResourceKey`s or markup extension) of brushes, thicknesses, corner radii, fonts, and metrics. Each Office theme (2007, 2010, 2013, 2019, 2024) is a `ResourceDictionary` that supplies token values; only where a generation genuinely changed geometry (e.g. 2007's rounded glass tabs vs 2013's flat tabs) does a theme override a template. `ThemeManager` static class swaps merged dictionaries at runtime (whole app or per-ribbon), supports accent color variants and dark mode for 2019/2024. This keeps 5 themes maintainable: a visual bug fix lands once, in the shared template.

## 5. Window integration

`RibbonWindow` (optional but recommended) uses `WindowChrome` to draw the QAT and contextual tab group headers into the title bar, matching Office. Must handle: maximize (content not clipped), per-monitor DPI changes, Windows 11 snap layouts on the caption buttons, and graceful fallback when the consumer uses a normal `Window` (ribbon renders self-contained, QAT below title bar).

## 6. Input: KeyTips & keyboard navigation

Global Alt-key handler enters KeyTip mode: overlay adorner shows each control's `KeyTip` string; typing chains into tab → group → control (Alt, H, F, S style). Esc walks back one level; any click/focus loss dismisses. Arrow-key navigation across the ribbon per UIA expectations. F6 cycles regions. This is its own subsystem (`Input/`) with the ribbon exposing an attachment point — do not scatter key handling across controls.

## 7. Backstage, galleries, QAT, customization

**Backstage**: full-window overlay replacing the ribbon on ApplicationButton click; left nav of `BackstageTabItem`s + content area; own theming; Esc/back-button to exit; animates in per 2013+ behavior.

**Galleries**: `InRibbonGallery` shows a scrollable strip of items in the ribbon with expand button opening a resizable popup; `Gallery` inside dropdowns. Both use `VirtualizingPanel`, item grouping with headers, filter support, and live-preview hooks (mouse-over raises preview events, click commits).

**QAT**: item collection mirroring ribbon controls via a lightweight "quick-access clone" contract (`IQuickAccessItemProvider` — a control knows how to produce its QAT representation). Above/below-ribbon placement, overflow dropdown, add/remove via right-click context menu.

**Customize dialog + persistence**: runtime dialog for reordering QAT (v1) and tabs/groups (post-v1); `RibbonState` serializer (JSON) persists QAT contents, ribbon minimized flag, selected tab; consumer chooses storage location.

## 8. Tab merging & modal tabs

**Tab merging**: a child context (e.g. an embedded document editor or MDI child) contributes tabs/groups into the host ribbon and removes them when deactivated — API: `RibbonMergeSource` + `Merge()/Unmerge()` with ordering hints. **Modal tabs**: a tab that, when active, temporarily hides all others plus shows a Close button (e.g. Print Preview mode) — API: `IsModal` on tab + `RibbonModalScope` events. Both are rare features; isolate them behind their own services so core layout never special-cases them.

## 9. Design-time experience

`XmlnsDefinition`/`XmlnsPrefix` attributes for clean namespaces; a `*.DesignTools` metadata story is unnecessary for modern VS — instead ensure templates render in the XAML designer without runtime services (guard `DesignerProperties.GetIsInDesignMode`), give every control sensible default content, and ship VS item-template snippets in docs. Verify designer preview in VS 2022+ per theme.

## 10. Accessibility & internationalization

Every control gets an `AutomationPeer` (patterns: Invoke, Toggle, ExpandCollapse, Selection, Value). Localizable strings (customize dialog, tooltips like "Minimize the Ribbon") in `.resx` with a `RibbonLocalization` override point. RTL (`FlowDirection`) supported in layout and verified in the showcase app.

## 11. Testing strategy

Layout/size-reduction logic factored into testable pure classes (unit tests, xUnit). Visual regression: showcase pages rendered off-screen per theme at 100/125/150/200% DPI, compared to approved snapshots in CI. Accessibility: automated UIA tree checks + Accessibility Insights passes per milestone. Manual matrix: Windows 10/11, mixed-DPI multi-monitor.
