# Roadmap to v1.0

Each phase ends with the showcase app demonstrating everything built so far, tests green, and a tagged pre-release. Phases are sequential but small overlaps are fine.

> **Progress (2026-08-20):** Phases 0–8 are complete and `v1.0.0` is published as a GitHub Release. Phase 6 closed with the user-verified live RTL popup/window pass in [`04-DESIGN-NOTES.md`](../04-DESIGN-NOTES.md) §3.61: all five themes ship with dark/black variants, per-monitor DPI is verified at 100/125/150/175/200%, and the deterministic 40-image theme/variant × DPI suite plus twenty-two focused scenes are green. Post-v1 Office 2007 frame and Backstage S7-S9 are complete through §3.94. MDI M0 and M4 are done while M1-M3 remain; custom-control projection/extensibility and future themes remain candidate tracks. RibbonKit Writer has completed the verified W0-A scaffold and W0-B document-lifetime layer (§§3.99-3.100); W0-C through W5 remain.
>
> Live DPI switching needed a fix outside the library: an app must declare **PerMonitorV2** in its own manifest or Windows bitmap-stretches it until restart. The showcase now does, and the README tells consumers to (§3.42).
>
> The 2007 window frame was deferred on purpose and is now complete: a corrected no-material baseline
> plus an optional app-controlled Aero-inspired enhancement passed its independent post-v1
> implementation/verification gate, including maximize, Snap Layout and real 125%↔150% mixed-DPI
> checks ([Office 2007 plan](07-OFFICE-2007-THEME-PLAN.md) §6/S7). The
> other deferral — the real two-pane 2007 application menu — **shipped on 2026-07-28** as
> `RibbonApplicationMenu`, a new control rather than a theme (§3.46).

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

**Community launch: complete (2026-08-11).** The signed-off `v1.0.0` package, symbols and checksums
are distributed through the GitHub Release rather than NuGet.org. **Exit criteria met:** published
`v1.0.0` GitHub Release.

## Post-v1 candidates

Simplified (single-row) ribbon, touch/pen affordances, Office-style status bar, and a ribbon
designer/serializer from XML definitions remain candidates.

**Custom-control integration and richer projections** are planned as one opt-in extensibility track.
Arbitrary group content remains unrestricted; controls that want customization/QAT participation
provide stable identity, display metadata, an icon and a context-aware projection factory with an
explicit cleanup lifetime. Commands and state are source-bound rather than copied. Group, gallery and
combo projections remain separate proof cases. Full provisional design:
[`docs/08-CUSTOM-CONTROL-INTEGRATION-PLAN.md`](08-CUSTOM-CONTROL-INTEGRATION-PLAN.md).

**Additional themes/variants** are a separate candidate track. Office 2021's sharp-edged,
pre-rounded UI is the leading first addition: Office 2024-era title/search composition while retaining
Office 2019's compact square geometry, not the later rounded visual refresh. The shared-template/token
boundary and whole-surface acceptance matrix remain mandatory. The recommended original-theme
sequence after it is RibbonKit Aurora (dark indigo
flagship), Warm Sand (non-white light), then Graphite Copper (warm professional dark); Evergreen,
Aubergine and Polar Slate remain exploratory. Full intake and verification plan:
[`docs/09-FUTURE-THEMES-PLAN.md`](09-FUTURE-THEMES-PLAN.md).

**RibbonKit Writer** is the complex consumer/reference application, now implemented through W0-B as
a separate app/test project with a document-lifetime layer rather than another Showcase page. Its
planned scope includes `.txt`, `.rtf` and a native
paper-aware format, Backstage/QAT/customization, images and hyperlinks, paginated preview/printing,
and native FlowDocument tables with contextual Table Tools. OLE embedding, DOCX compatibility and
editable Word-style page layout are explicit non-goals. Full plan:
[`docs/10-RIBBONKIT-WRITER-PLAN.md`](10-RIBBONKIT-WRITER-PLAN.md). The durable dependency packets,
exclusive file ownership and later Luna dispatch gates are recorded separately in
[`docs/11-RIBBONKIT-WRITER-LUNA-EXECUTION-PLAN.md`](11-RIBBONKIT-WRITER-LUNA-EXECUTION-PLAN.md); that
execution decomposition is planning, not implementation evidence.

Possible visual polish also includes an optional purpose-authored `MonochromeIcon` for QAT-capable
commands and a non-Mica light/dark transition that captures the old opaque window chrome, swaps the
palette underneath, then fades the capture away while honoring reduced motion. Mica should retain
its native DWM retint.

**MDI emulation control** — themed in-window "child windows" (float/resize/cascade/tile/minimize/maximize) plus a switchable tabbed-documents mode, with the maximized child's caption merging into the ribbon. Orchestrates existing subsystems (tab merging, `RibbonState` persistence, token theming, `RibbonWindow` chrome/DPI) rather than adding much new mechanism; can build most of the way without waiting on Phase 7's tab-merging API. Full design in [`docs/05-MDI-EMULATION-PLAN.md`](05-MDI-EMULATION-PLAN.md).

## Suggested working rhythm

Keep issues per phase in a GitHub Project board; one feature = one PR with tests + showcase page + docs snippet. Tag `0.x` pre-releases at every phase exit so early adopters generate feedback while the API can still change.
