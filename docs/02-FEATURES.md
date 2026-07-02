# Feature Breakdown & Priorities

Priority key: **P0** = core skeleton (Milestone 1) · **P1** = required for a credible ribbon · **P2** = full Office parity · **P3** = differentiators / post-v1 polish

## Structure & layout

| Feature | Priority | Notes |
|---|---|---|
| Ribbon root control + tab strip | P0 | Selection, headers, content host |
| RibbonTab / RibbonGroup | P0 | Group header label, separators between groups |
| Adaptive sizing engine (large→medium→small→collapsed) | P0 | Prototype first; see architecture §3 |
| Group collapse to popup button | P1 | Final reduction step |
| Group dialog launcher (↘) | P1 | Command + ScreenTip |
| Minimize mode (double-click tab / chevron / Ctrl+F1) | P1 | Tabs act as popups when minimized |
| Simplified ribbon (single-row, 365-style) | P3 | Nice differentiator, big layout work |
| RibbonWindow (QAT + contextual headers in title bar) | P2 | Graceful fallback on plain Window |

## Controls

| Feature | Priority | Notes |
|---|---|---|
| RibbonButton — Large / Medium / Small | P0 | Icon + label layouts per size |
| ToggleButton | P1 | Group exclusivity option |
| SplitButton / DropDownButton | P1 | Split hit-testing, submenu support |
| RibbonComboBox (editable + readonly) | P1 | In-group width control |
| TextBox / CheckBox / RadioButton (ribbon-styled) | P2 | |
| Control groups (button stacks, 3-row small layout) | P1 | |
| RibbonMenu / MenuItem with icons + split items | P1 | Used by all dropdowns |
| ScreenTips (rich tooltip: title, body, image, F1 hint) | P1 | |
| In-ribbon gallery + expandable popup | P2 | Virtualized, grouped, filterable |
| Gallery in dropdown menus | P2 | Color picker, style gallery patterns |
| Live preview events (hover preview / commit / cancel) | P2 | |

## Application-level features

| Feature | Priority | Notes |
|---|---|---|
| ApplicationButton (File button / Office orb for 2007) | P1 | Orb shape differs per theme |
| Application menu (2007/2010 dropdown style) | P2 | |
| Backstage view (2013+ full-window) | P2 | Left nav + content, animation, Esc to close |
| Quick Access Toolbar (above/below, overflow, add/remove) | P2 | IQuickAccessItemProvider contract |
| Customize dialog (QAT reorder v1; tabs/groups later) | P3 | |
| State persistence (QAT, minimized, selected tab) | P2 | JSON, consumer-controlled storage |
| Contextual tabs + colored tab groups | P2 | Visibility bound to app state |
| Tab merging (child ribbon contributes into host) | P3 | RibbonMergeSource API |
| Modal tabs (Print-Preview-style exclusive tab) | P3 | IsModal + close button |
| Recent-items list (app menu / backstage) | P3 | |

## Input & accessibility

| Feature | Priority | Notes |
|---|---|---|
| KeyTips (Alt chained navigation) | P2 | Own subsystem |
| Arrow/Tab/F6 keyboard navigation | P1 | Required for accessibility sign-off |
| AutomationPeers for every control | P1 | Grows with each control |
| RTL support | P2 | |
| Localization of built-in strings | P2 | .resx + override point |

## Theming & rendering

| Feature | Priority | Notes |
|---|---|---|
| Token-based theme layer (shared templates) | P0 | Foundation — do before theme #2 |
| Office 2024 theme (default, light) | P0 | Ship-first look |
| Office 2019 theme (+ dark, accent colors) | P2 | |
| Office 2013 theme (white/light gray/dark gray) | P2 | |
| Office 2010 theme (silver/blue/black) | P2 | |
| Office 2007 theme (blue/silver/black, glass look) | P2 | Most geometry overrides |
| Runtime theme switching | P1 | ThemeManager |
| Per-monitor v2 High DPI | P1 | Vector icons only; test 100–200% |
| Built-in vector icon set for samples | P2 | Geometry-based |

## Developer experience & project infrastructure

| Feature | Priority | Notes |
|---|---|---|
| MVVM: ItemsSource + DataTemplates everywhere | P1 | Design API this way from day one |
| XAML designer preview (VS 2022) | P1 | Design-mode guards, default content |
| XML doc comments on all public API | P1 | Enforced in CI |
| Showcase/demo app | P0 | Grows with every feature; is the test bed |
| NuGet package + SourceLink + symbols | P1 | |
| CI: build + tests + pack on PR | P0 | GitHub Actions |
| Visual regression snapshots per theme/DPI | P2 | |
| Docs site (getting started, control gallery) | P2 | |
| README, LICENSE (MIT), CONTRIBUTING, issue templates | P0 | Open-source hygiene from day one |
