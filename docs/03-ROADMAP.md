# Roadmap to v1.0

Each phase ends with the showcase app demonstrating everything built so far, tests green, and a tagged pre-release. Phases are sequential but small overlaps are fine.

> **Progress (2026-08-11):** Phases 0–7 are complete. Phase 6 closed with the user-verified live RTL popup/window pass in [`04-DESIGN-NOTES.md`](../04-DESIGN-NOTES.md) §3.61: all five themes ship with dark/black variants, per-monitor DPI is verified at 100/125/150/175/200%, the deterministic 40-image theme/variant × DPI suite plus twenty-two focused scenes are green, localization/provider coverage is complete, and the Showcase lab verifies split/nested/context menus, normal/maximized screen edges, Backstage, and Office 2024/2007 application-menu/button/orb behavior. Phase 8 release engineering is complete through §3.83; a local `v1.0.0` GitHub-release candidate is being prepared, while public distribution is deferred until explicitly requested. Post-v1 MDI emulation is unusually shaped: M0 and M4 are done, M1–M3 are not.
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

**API review and freeze: complete (2026-08-10).** The nullability-aware v1 surface is captured in `PublicAPI.Shipped.txt`; `Microsoft.CodeAnalysis.PublicApiAnalyzers` treats additions, removals, invalid baselines and nullability drift as build errors for both TFMs. Unsupported automatic QAT projections now return `false` instead of creating an unusable generic proxy, and the release solution builds with zero warnings.

**Repository documentation: complete (2026-08-10).** A separate website was deliberately avoided: the GitHub README is the public landing page and contains the visual control overview, source-reference path, pasteable first ribbon, theming and DPI guidance, and task-oriented links into the existing focused Markdown references and executable Showcase.

**Source Link and symbols: complete (2026-08-11).** The .NET 8+ SDK's built-in GitHub provider emits commit-pinned source mappings for both runtime TFMs; the symbol package contains both portable PDBs, and the NuSpec publishes the repository URL and commit.

**Portable output and deterministic versioning: complete (2026-08-11).** Local packs write to the
repository's ignored `artifacts/` directory. The default advanced from `0.1.0-alpha.1` to `1.0.0`
when the local GitHub-release candidate was prepared.

**Package and clean-consumer gate: complete (2026-08-11).** CI now validates the exact main/symbol package layout and metadata, then restores exclusively from that local package into an isolated cache and compiles real RibbonKit XAML for both runtime TFMs. Both package files are retained as CI artifacts; no publish step exists.

**Performance and installed-package validation: complete (2026-08-11).** The Release Showcase has a repeatable outside-the-debugger startup/resize/memory baseline; a locally packaged executable loads the explicit theme/control dictionaries and passes Backstage plus repeated-resize runtime checks; and the same package is user-verified in Visual Studio 2026 through the live designer context commands and Ribbon Editor.

**Local GitHub-release candidate: complete (2026-08-11).** Version 1.0.0 uses the neutral
`RibbonKit contributors` attribution and includes an explicit AI-development provenance note. The
repeatable preparation script produces the main package, symbols, release notes, and SHA-256 sums
without containing any upload or publish operation.

Remaining: optional public launch. The chosen distribution path is a downloadable package attached to
a GitHub Release, not NuGet.org, and publication is deferred until explicitly requested. **Exit
criteria:** a signed-off `v1.0.0` GitHub Release.

## Post-v1 candidates

Simplified (single-row) ribbon, touch/pen affordances, additional theme variants (colorful/black for 2013+), Office-style status bar, and a ribbon designer/serializer from XML definitions. Richer QAT projections are also candidates: group-as-dropdown first, gallery-as-icon-dropdown second, and an inline combo-box projection only after its editable/selection/overflow contract is proven. These should create source-linked representations rather than move the original WPF content, and should prove the strip/overflow/popup lifecycle before exposing a public provider interface. Possible visual polish: an optional purpose-authored `MonochromeIcon` for QAT-capable commands on accent-colored title/tab surfaces; this is not a committed API, and no separate dark-icon property is currently planned. Also consider a non-Mica light/dark transition that captures the old opaque window chrome, swaps the palette underneath, then fades the capture away while honoring `RibbonAnimationAction.ThemeSwitch` and reduced motion. Mica should keep its native DWM retint rather than layering a second transition over it.

**MDI emulation control** — themed in-window "child windows" (float/resize/cascade/tile/minimize/maximize) plus a switchable tabbed-documents mode, with the maximized child's caption merging into the ribbon. Orchestrates existing subsystems (tab merging, `RibbonState` persistence, token theming, `RibbonWindow` chrome/DPI) rather than adding much new mechanism; can build most of the way without waiting on Phase 7's tab-merging API. Full design in [`docs/05-MDI-EMULATION-PLAN.md`](05-MDI-EMULATION-PLAN.md).

## Suggested working rhythm

Keep issues per phase in a GitHub Project board; one feature = one PR with tests + showcase page + docs snippet. Tag `0.x` pre-releases at every phase exit so early adopters generate feedback while the API can still change.
