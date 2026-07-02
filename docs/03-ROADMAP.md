# Roadmap to v1.0

Each phase ends with the showcase app demonstrating everything built so far, tests green, and a tagged pre-release. Phases are sequential but small overlaps are fine.

## Phase 0 — Foundation (before any control code)

Library name: **RibbonKit** (chosen 2026-07-02; NuGet ID verified free — reserve it and the GitHub repo early; note an unrelated Swift/iOS library shares the name). Root namespace `RibbonKit`, controls named `Ribbon`, `RibbonTab`, etc. within it. Add MIT license, README, CONTRIBUTING, issue templates. Create the solution: library project (`net8.0-windows;net9.0-windows`, `UseWPF`), showcase app, unit-test project. Set up GitHub Actions CI (build + test + pack on every PR) and `Directory.Build.props` with analyzers, nullable enabled, and XML-doc enforcement. **Exit criteria:** empty ribbon control renders "Hello Ribbon" in the showcase app via CI-built package.

## Phase 1 — Core ribbon skeleton  ← *Milestone 1 (locked)*

Ribbon, RibbonTabControl, RibbonTab, RibbonGroup, RibbonButton in all three sizes, with the **adaptive sizing engine prototyped and working** (large→medium→small reduction on window resize; collapse-to-popup may stub). One theme only: Office 2024 light, built on the token layer so later themes slot in. Basic ScreenTip and ICommand support on buttons. **Exit criteria:** showcase shows a Word-like Home tab that reflows correctly when the window narrows; sizing logic covered by unit tests.

## Phase 2 — Control set & interaction

ToggleButton, SplitButton, DropDownButton, RibbonMenu/MenuItem, ComboBox, control groups/button stacks, group dialog launcher, full ScreenTips, group collapse popup completed, minimize mode, runtime theme-switch API (even with one theme). Keyboard navigation (arrows/Tab/F6) and AutomationPeers for everything shipped so far. **Exit criteria:** a real app could ship on the library for basic scenarios; accessibility scan passes.

## Phase 3 — Application button & backstage

ApplicationButton, application menu (dropdown style), Backstage view with tab navigation and animation, recent-items pattern in the showcase. **Exit criteria:** File-menu experience matches Office 2024 behavior.

## Phase 4 — Galleries & live preview

Gallery + InRibbonGallery with virtualization, grouping, filtering, resizable popup; live-preview event contract; color-picker and style-gallery samples. **Exit criteria:** 1,000-item gallery scrolls smoothly; live preview demo works.

## Phase 5 — QAT, window chrome & contextual tabs

RibbonWindow with title-bar integration, Quick Access Toolbar (placement, overflow, add/remove via context menu), state persistence (JSON), contextual tab groups with colored headers bound to app state. **Exit criteria:** showcase behaves like Office when an "image" object is selected (Picture Tools appear); QAT survives app restart.

## Phase 6 — Full theme range & DPI hardening

Office 2019 (+dark/accents), 2013, 2010, 2007 themes on the token layer; visual regression snapshot suite per theme; DPI matrix testing (100/125/150/200%, mixed monitors, per-monitor v2); RTL verification; localization resources. **Exit criteria:** snapshot suite green across 5 themes × 4 DPI levels.

## Phase 7 — Power features

KeyTips subsystem (full Alt-chain), tab merging API, modal tabs, customize dialog (QAT reordering v1). **Exit criteria:** Alt-H-F-S style chains work end-to-end; merge/modal demos in showcase.

## Phase 8 — v1.0 release engineering

API review and freeze (rename pass, hide internals, `PublicAPI.txt` analyzer), documentation site with control gallery and getting-started guide, NuGet polish (icon, readme, SourceLink), performance pass (startup time, resize CPU, memory), community launch (announce, good-first-issues). **Exit criteria:** v1.0.0 on NuGet.

## Post-v1 candidates

Simplified (single-row) ribbon, full ribbon customization dialog (tabs/groups), touch/pen affordances, additional theme variants (colorful/black for 2013+), Office-style status bar, ribbon designer/serializer from XML definitions.

## Suggested working rhythm

Keep issues per phase in a GitHub Project board; one feature = one PR with tests + showcase page + docs snippet. Tag `0.x` pre-releases at every phase exit so early adopters generate feedback while the API can still change.
