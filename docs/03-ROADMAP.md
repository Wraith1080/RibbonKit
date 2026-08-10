# Roadmap to v1.0

Each phase ends with the showcase app demonstrating everything built so far, tests green, and a tagged pre-release. Phases are sequential but small overlaps are fine.

> **Progress (2026-08-08):** Phases 0–7 are complete. Phase 6 closed with the user-verified live RTL popup/window pass in [`04-DESIGN-NOTES.md`](../04-DESIGN-NOTES.md) §3.61: all five themes ship with dark/black variants, per-monitor DPI is verified at 100/125/150/175/200%, the deterministic 40-image theme/variant × DPI suite plus seven focused scenes are green, localization/provider coverage is complete, and the Showcase lab verifies split/nested/context menus, normal/maximized screen edges, Backstage, and Office 2024/2007 application-menu/button/orb behavior. Phase 8 has not started. Post-v1 MDI emulation is unusually shaped: M0 and M4 are done, M1–M3 are not.
>
> Live DPI switching needed a fix outside the library: an app must declare **PerMonitorV2** in its own manifest or Windows bitmap-stretches it until restart. The showcase now does, and the README tells consumers to (§3.42).
>
> Deferred out of the 2007 work on purpose: the 2007 window frame (Windows 11 draws its own border, and the frame is the change most likely to perturb the measured-margin maximize fix). The other deferral — the real two-pane 2007 application menu — **shipped on 2026-07-28** as `RibbonApplicationMenu`, a new control rather than a theme (§3.46).

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

## Phase 6 — Full theme range & DPI hardening ✅ COMPLETE (2026-08-08)

Office 2019 (+dark/accents), 2013, 2010, 2007 themes on the token layer; visual regression snapshot suite per theme; DPI matrix testing (100/125/150/200%, mixed monitors, per-monitor v2); RTL verification; localization resources. **Exit criteria met:** snapshot suite green across 5 themes × 4 DPI levels. *All five themes ship (2007 landed 2026-07-27, §3.38), every generation has a dark/black palette (§3.49), and the deterministic 40-image theme/variant × DPI matrix plus seven focused approvals are green. Localization/provider, File-width reflow, representative bidirectional content, live Backstage/title transitions, synchronized Showcase File surfaces, and the final live popup/window, screen-edge, and Office 2024/2007 application-menu/orb verification are complete (§§3.52–3.61).*

## Phase 7 — Power features ✅ COMPLETE (2026-07-27)

KeyTips subsystem (full Alt-chain), tab merging API, modal tabs, customize dialog (QAT reordering v1). KeyTips and the customize dialog shipped early, alongside Phases 2–5; tab merging and modal tabs landed in one arc — modal tabs, whole-tab merging, group contributions into host tabs with a declarative activation path, and the MDI tab/caption merge (which also closed MDI milestone M4). Design in [`06-MERGE-AND-MODAL-PLAN.md`](06-MERGE-AND-MODAL-PLAN.md); implementation notes and pitfalls in [`04-DESIGN-NOTES.md`](../04-DESIGN-NOTES.md) §3.32–§3.34. **Exit criteria met:** Alt-H-F-S chains work end-to-end; merge and modal demos are in the showcase; the plan's §7 automated invariants landed on 2026-08-01.

## Phase 8 — v1.0 release engineering

API review and freeze (rename pass, hide internals, `PublicAPI.txt` analyzer), documentation site with control gallery and getting-started guide, NuGet polish (icon, readme, SourceLink), performance pass (startup time, resize CPU, memory), community launch (announce, good-first-issues). **Exit criteria:** v1.0.0 on NuGet.

## Post-v1 candidates

Simplified (single-row) ribbon, touch/pen affordances, additional theme variants (colorful/black for 2013+), Office-style status bar, and a ribbon designer/serializer from XML definitions. Richer QAT projections are also candidates: group-as-dropdown first, gallery-as-icon-dropdown second, and an inline combo-box projection only after its editable/selection/overflow contract is proven. These should create source-linked representations rather than move the original WPF content, and should prove the strip/overflow/popup lifecycle before exposing a public provider interface. Possible visual polish: an optional purpose-authored `MonochromeIcon` for QAT-capable commands on accent-colored title/tab surfaces; this is not a committed API, and no separate dark-icon property is currently planned. Also consider a non-Mica light/dark transition that captures the old opaque window chrome, swaps the palette underneath, then fades the capture away while honoring `RibbonAnimationAction.ThemeSwitch` and reduced motion. Mica should keep its native DWM retint rather than layering a second transition over it.

**MDI emulation control** — themed in-window "child windows" (float/resize/cascade/tile/minimize/maximize) plus a switchable tabbed-documents mode, with the maximized child's caption merging into the ribbon. Orchestrates existing subsystems (tab merging, `RibbonState` persistence, token theming, `RibbonWindow` chrome/DPI) rather than adding much new mechanism; can build most of the way without waiting on Phase 7's tab-merging API. Full design in [`docs/05-MDI-EMULATION-PLAN.md`](05-MDI-EMULATION-PLAN.md).

## Suggested working rhythm

Keep issues per phase in a GitHub Project board; one feature = one PR with tests + showcase page + docs snippet. Tag `0.x` pre-releases at every phase exit so early adopters generate feedback while the API can still change.
