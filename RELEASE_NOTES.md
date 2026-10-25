# RibbonKit v1.1.0

Local release candidate prepared on 2026-10-25. Publication and the rebuild from
the chosen release commit remain pending. Distribution remains GitHub Releases;
NuGet.org publication is not planned.

RibbonKit 1.1 expands the shared ribbon controls and the Writer/Showcase consumers
while preserving the 1.0 public API. Runtime targets remain `net8.0-windows` and
`net9.0-windows`; Visual Studio design tooling remains an isolated `net472` assembly
packaged beside each runtime under `Design/`.

## Highlights

- **Simplified Ribbon:** a single command row with authored defaults, priority
  overflow, native group flyouts, pin/remove commands, custom tabs/groups and
  shared customization persistence. Custom structure removal stays synchronized
  with Classic mode. Classic remains the default.
- **Touch density:** larger shared targets and spacing across commands, galleries,
  QAT and File surfaces. Hosts choose and persist Mouse/Touch mode; Compact remains
  the default. Writer includes the shared descriptive Mouse/Touch dropdown.
- **Crystal and Office 2024 Flat:** new palettes, themed utility controls and
  scrollbars, scoped glass/backdrop integration and optional Office 2007/2010
  Aero-inspired frames. Modern ribbon Glass is disabled for Office 2007/2010.
  Ordinary popup backdrops remain explicit host/control opt-ins; shared collapsed
  groups and QAT/Simplified overflow follow the ribbon continuation treatment.
- **Richer QAT/customization:** source-linked combo, gallery and group projections,
  three icon appearances, below-ribbon overflow, menu arrow scrolling and corrected
  nested Escape/keyboard behavior. Groups are offered in Customize QAT; Customize
  Ribbon's available-command list remains command-only.
- **Per-ribbon feature policies:** hosts can enable or restrict Simplified mode,
  customization, minimization, QAT visibility/placement and density changes without
  replacing existing selectors or bindings.
- **Coordinated motion:** Classic/Simplified, density and QAT relocation share
  transition lifecycles with optional host-content synchronization. Reduced motion,
  interruptions and cleanup retain immediate final layout and sharp final paint.
  Native resize uses temporary ribbon paint reuse with cleanup on exit/cancel.
- **Accessibility:** selected-tab command discovery, actionable QAT/overflow
  providers, complete Toggle activation, grouped Backstage navigation/page regions,
  Simplified command discovery and named collapsed/minimized controls.
- **Writer integration:** appearance preview/Apply/Cancel checkpoints, persistent
  density and QAT appearance, caret-only formatting, copied command execution,
  overflow font commits, search-result focus and an eight-column table picker with
  a custom gallery layout. Document ownership stays in the application.

## Compatibility and migration

All 1,093 previously shipped API entries are retained. The 264 reviewed additive
entries are promoted into the 1.1 shipped baseline; future additions start in
Unshipped again. The package, runtime, sample and tooling versions now derive
from the shared `VersionPrefix=1.1.0`.

- Office 2007 now follows its conventional Orb default. Set
  `ApplicationButtonShape="Tab"` explicitly to retain the former File-tab
  presentation. Local WPF values retain precedence.
- The Ribbon container itself is skipped in keyboard Tab navigation; its commands
  remain destinations. Hosts that explicitly focus the container should review
  their focus routing.
- Existing saved layouts/preferences retain their compatibility paths. The
  optional Simplified sections and new density/QAT choices do not change Writer
  document formats. Downgrading new enum choices into an older application is
  not a supported forward-compatibility promise.
- Custom themes should review the more specific surface keys below. Older shared
  brush keys remain available, but overriding them alone may no longer repaint
  these surfaces. Menu row minimum width now follows
  `RibbonKit.Metrics.MenuItemMinWidth` instead of the former 160-DIP style literal.

| Surface | Override keys |
| --- | --- |
| ScreenTip background | `RibbonKit.Brushes.ScreenTip.Surface` |
| Text input background/border | `RibbonKit.Brushes.Input.SurfaceBackground`, `RibbonKit.Brushes.Input.Border` |
| Options action buttons | `RibbonKit.Brushes.Dialog.ActionBackground`, `RibbonKit.Brushes.Dialog.ActionBorder` |

## Verification and accepted limits

The [1.1 acceptance record](docs/20-V1.1-ACCEPTANCE-AND-RELEASE-PLAN.md) distinguishes
automated coverage, actual-user review and unavailable hardware scopes.

- Release builds and API/XML checks pass. Runtime 1,290/1,290, Writer 564/564 and
  visual 5/5 checks pass, including the full 195-scene approved inventory.
- All 58 independent-consumer sections and final assertions have passing coverage
  in the retained full-run prefix and continuations. Two obsolete test assumptions
  were corrected after accessibility changes. Original failed attempts remain
  recorded; this is assembled coverage, not a clean single full-suite pass.
- Writer/Showcase manual acceptance, the critical package/designer review and
  available single-monitor display/motion checks passed at their recorded scopes.
- The local 1.1 package pair passes layout/metadata checks, clean net8/net9 builds
  and runtime smokes, assembly/symbol provenance checks and checksums. A consumer
  binary built against 1.0 also launches with the replacement 1.1 runtime. The
  committed-source rebuild remains pending; A9 records that release boundary.
- A local fixed-content comparison measured median startup at 571 ms with the
  1.0 library and 720 ms with the current library, plus a 33% longer paced resize
  workload despite lower process CPU. This measured cost is accepted for 1.1.
  Offscreen timings do not establish visible frame latency or a universal slowdown.
- Genuine mixed-monitor movement was unavailable. Physical touch/OS IME and an
  exhaustive accessibility certification are outside the reviewed scope.
- Showcase customization uses its existing live host behavior; Cancel rollback is
  a documented host-integration recommendation. Writer retains its applied
  checkpoint when later appearance edits are canceled.
- Remaining MDI emulation milestones and broader host/platform investigations
  retain their existing plan status; 1.1 does not close them by implication.

## Distribution and provenance

Local candidate assets are `RibbonKit.1.1.0.nupkg`, `RibbonKit.1.1.0.snupkg`, these
notes and `SHA256SUMS.txt`. The runtime package includes XML docs, the toolbox
manifest and per-TFM design tooling; the symbol package includes portable PDBs
with Source Link.

Uncommitted release preparation still records the current HEAD in NuSpec/Source
Link. Commit the reviewed release changes and rebuild the assets before tagging
or publishing so those records identify the exact release source. No tag, upload
or GitHub Release is created by local preparation.

RibbonKit remains MIT-licensed and attributed to **RibbonKit contributors**.
Development primarily used AI coding assistants under human direction, visual
review and automated verification.

---

# RibbonKit v1.0.0

> Release-specific record. Later repository progress and acceptance are tracked in
> [design notes §5](04-DESIGN-NOTES.md#5-current-state--next-steps).

RibbonKit v1.0.0 is published through GitHub Releases.

RibbonKit is an MIT-licensed Office Fluent UI-style Ribbon control library for WPF applications on
modern .NET. Version 1.0.0 freezes the validated public API and packages the runtime, Visual Studio
design tools, documentation, symbols, and Source Link support into downloadable GitHub Release
assets.

## Highlights

- Lookless WPF ribbon controls with adaptive large, medium, small, collapsed, split, dropdown, and
  gallery presentations.
- Office 2007, 2010, 2013, 2019, and 2024 themes, including dark and black variants.
- Backstage and Office 2007 application-menu surfaces, QAT customization, KeyTips, ScreenTips,
  contextual tabs, tab merging, modal tabs, localization, RTL, and per-monitor DPI support.
- Runtime customization persistence and a Visual Studio Ribbon Editor with icon browsing.
- MDI floating-child and ribbon/caption merge foundations.

## Verification

- Both `net8.0-windows` and `net9.0-windows` package consumers build from an isolated local source.
- The packaged runtime and explicit theme/control dictionaries were exercised in a live smoke test.
- The package-installed Visual Studio designer, context commands, and Ribbon Editor were verified.
- The Release Showcase performance baseline and deterministic visual approvals are recorded in
  `04-DESIGN-NOTES.md`.

## Known post-v1 work at release

- Office 2007 window-frame/Backstage S7-S9 was completed after v1.0.0; see `04-DESIGN-NOTES.md`
  §§3.89-3.94 for the current repository state.
- Remaining MDI arrange/cycling, full MVVM proof, tabbed-document mode, and persistence milestones.
- Touch-density architecture and richer automatic QAT projections.

## Development provenance

RibbonKit was developed primarily with AI coding assistants under human direction, visual review,
and automated verification. Release metadata uses the neutral attribution **RibbonKit contributors**.

## Distribution

Distribution is `RibbonKit.1.0.0.nupkg` attached directly to the GitHub Release, with
`RibbonKit.1.0.0.snupkg` and SHA-256 checksums alongside it. No NuGet.org publication is planned.
