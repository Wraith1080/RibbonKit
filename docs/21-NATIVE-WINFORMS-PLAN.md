# Native RibbonKit.WinForms assessment and implementation plan

> Implementation through [M5E application restart persistence](../winforms/docs/M5E-SHOWCASE-PERSISTENCE.md),
> with [M6C backdrop review corrections](../winforms/docs/M6C-BACKDROPS.md#resumed-review-corrections--2026-10-30),
> 2026-10-30. Current bounded
> scope, evidence and remaining review are in the [M5 ledger](../winforms/docs/M5-STATUS.md)
> and [M4 ledger](../winforms/docs/M4-STATUS.md).
> The [M3 ledger](../winforms/docs/M3-STATUS.md) retains QAT/projection evidence.
> [Remaining slice sequence](../winforms/docs/M4-STATUS.md#remaining-slices) is a
> bounded working decomposition of the milestones below.
> Comprehensive theming is deferred
> until most component slices are implemented; see the sequencing decision in §6.
> [M2 scope and evidence](../winforms/docs/M2-STATUS.md) records the bounded API and
> implementation gates. Accessibility compatibility is deferred while the necessary
> control structure is implemented, by user direction. Outstanding M1 review gates
> remain recorded separately. The user authorized the foundation
> on their `codex/Ribbonkit-Winform` branch. Native projects are isolated under
> [winforms/](../winforms/README.md); [the M1 ledger](../winforms/docs/M1-STATUS.md)
> records current evidence and open gates. WPF v1.1.0 remains a reference, not a dependency.

The product is a reusable System.Windows.Forms library using native rendering,
input, binding, events, collections, accessibility and Visual Studio authoring.
Avalonia, WPF hosting/ElementHost and compatibility wrappers are outside this plan.

## 1. Reference, evidence and scope

### Verified reference identity

The working tree was clean before the assessment. At that point local `main`, `HEAD` and the
locally recorded `origin/main` resolve to
`cf1b942a1ec707538084a51202b03ad68d9be33e`, the supplied reference commit.
`git diff <reference> HEAD` and `<reference>..HEAD` contain no changes or commits.
Consequently, all implementation inspected here belongs to the requested release
source; there is no post-1.1 checkout delta to classify.

A read-only remote tag query independently resolved `v1.1.0` to that full commit.
The [published GitHub release](https://github.com/Wraith1080/RibbonKit/releases/tag/v1.1.0)
was also verified through the public GitHub API: neither draft nor prerelease,
published 2026-10-25 23:22:26 UTC (2026-10-26 06:22:26 Asia/Jakarta), with
`RibbonKit.1.1.0.nupkg`, `RibbonKit.1.1.0.snupkg`, `RELEASE_NOTES.md` and
`SHA256SUMS.txt`. The local tag inventory did not yet contain v1.1.0; no fetch or
tag creation was needed. Asset contents/checksums were not downloaded or revalidated.

Evidence labels in this document:

- **Verified:** source, test assertions, project metadata or remote release metadata
  inspected during this assessment. Static test inspection is not execution.
- **Recorded:** prior automated or user-reviewed results in the release ledger,
  retained with their original scope and limitations.
- **Proposed:** recommended WinForms behavior, architecture, API or future gate.
- **Assumed:** a planning default requiring confirmation if product needs differ.

Read inputs included [repository instructions](../AGENTS.md), [README](../README.md),
[release notes](../RELEASE_NOTES.md), [contribution guidance](../CONTRIBUTING.md),
[design index/status](../04-DESIGN-NOTES.md), [architecture](01-ARCHITECTURE.md),
the [1.1 acceptance ledger](20-V1.1-ACCEPTANCE-AND-RELEASE-PLAN.md#campaign-gate-ledger),
and relevant implementation/test sections linked below. No new WPF build, test,
app launch, screenshot comparison, performance run or native acceptance occurred.

### Assessment of the released product

**Verified:** WPF is a substantial behavioral specification, not just a visual
mockup. It has lookless control classes, shared templates/tokens, adaptive sizing,
source-linked projections, lifetime services, keyboard services, automation peers,
structural customization and independent-consumer tests. The runtime project uses
`UseWPF` and targets `net8.0-windows;net9.0-windows`; its separate XAML designer
targets `net472`. These targets and package identities are WPF-specific facts,
not constraints to inherit into WinForms.

**Recorded:** release ledger A2 records a user-confirmed full hosted Release
build/API/XML and unfiltered runtime, Writer, visual and portability pass on
2026-10-25. No workflow identifier or fresh totals accompanied that confirmation.
A3 retains the 195-scene visual inventory and repeatability evidence. A4/A5 cover
bounded Writer/Showcase review; A6 covers single-display 100/150/200% and return to
125%, plus reduced-motion switching. A7 records critical designer save/reopen and
undo review. A9 records final local package preparation/provenance. These are
recorded WPF results, not newly reproduced passes or WinForms evidence.

**Recorded limitations:** genuine mixed-monitor movement was unavailable;
physical touch, OS IME and exhaustive accessibility certification were outside the
release review. Production RTL certification and full Windows contrast-theme
support are not claimed. MDI arrange/cycling, full MVVM proof, tabbed mode and
placement persistence remain unfinished; see [MDI status](05-MDI-EMULATION-PLAN.md).
Release A8 accepts a fixed-content median startup comparison of 571 ms for 1.0
versus 720 ms for the then-current library and a 33% longer paced resize workload,
despite lower process CPU. These measurements neither predict WinForms speed nor
establish visible frame latency or a universal slowdown.

### Documentation discrepancies to retain as assessment findings

| Document text | More specific evidence and interpretation |
| --- | --- |
| Architecture page, dated 2026-09-08, calls combo/gallery/group QAT projections candidates. | Release source contains the proxies and tests listed in the matrix. They are implemented in 1.1; that sentence is stale. |
| Design notes opening banner refers to evidence through early September; §5 still uses several remaining-review summaries and a v1.0 release-era distinction. | Later linked plans and release ledger A1-A9 own the dated 1.1 outcome. The banner/summary is not a reliable release feature inventory. |
| Contribution introduction emphasizes published v1.0/frozen v1 API. | Its API section correctly names both shipped 1.0 and 1.1 contracts; release notes identify 1.1. Preserve both baselines. |
| Release plan says GitHub tagging/upload/publication remain user-owned and does not claim publication. | Remote publication is now independently verified above. This is a historical handoff boundary, not evidence that 1.1 remains unpublished. |
| README's broad adaptive-size list can suggest every group always walks all sizes. | `RibbonGroup.ReductionMode` defaults to `Collapse`; stepped resizing is opt-in. Panel ordering also differs from the pure algorithm's default order. |
| A broad reading of customization can suggest uniform Cancel rollback. | README and ledger distinguish live-edit classic/QAT pages, draft Simplified edits, and Writer's host checkpoint. WinForms needs an explicit transaction contract. |

These findings do not authorize cleanup of WPF documentation or changes to its
behavior. Reference evidence below is pinned conceptually to the full release
commit above; relative links are navigation into this checkout.

## 2. Feature matrix and native implementation boundary

Milestones: **M1** complete foundation slice; **M2** editors/menus and stronger
input; **M3** QAT/projections; **M4** broader presentation/File surfaces and
contextual/merge/modal services; **M5** customization and Simplified Ribbon;
**M6** advanced motion/window integration. Details and dependencies are in §6.
Every row's WinForms column is proposed, not an existing capability.

| Area | WPF 1.1 verified source and representative tests | WinForms target behavior | Reuse versus reimplementation | Milestone |
| --- | --- | --- | --- | --- |
| Tabs, selection and groups | [RibbonTab](../src/RibbonKit/Controls/RibbonTab.cs), [RibbonTabControl](../src/RibbonKit/Controls/RibbonTabControl.cs), [RibbonGroup](../src/RibbonKit/Controls/RibbonGroup.cs); [panel assertions](../tests/RibbonKit.Tests/RibbonGroupsPanelTests.cs), [selected-content discovery](../tests/RibbonKit.Tests/TabContentAutomationTests.cs). Tabs own concrete group controls; this is not a general TabsSource/GroupsSource model. | Owned component collections, deterministic selection fallback, selected-tab groups, authored visibility/enabled state. Ordinary Form hosting. | Reuse selection/ownership scenarios; reimplement item model, arrangement and notification. | M1 |
| Adaptive sizes, priorities and scrolling | [ReductionAlgorithm](../src/RibbonKit/Layout/ReductionAlgorithm.cs), [RibbonGroupsPanel](../src/RibbonKit/Layout/RibbonGroupsPanel.cs), [RibbonScrollContentHost](../src/RibbonKit/Layout/RibbonScrollContentHost.cs); [pure tests](../tests/RibbonKit.Tests/ReductionAlgorithmTests.cs), [panel tests](../tests/RibbonKit.Tests/RibbonGroupsPanelTests.cs). Modes are Collapse, Resize, ResizeThenCollapse; fixed groups are excluded; remaining overflow scrolls. | Same three group policies and reduction order. Per-item fixed/adaptive size policy; no lost commands at minimum width. Scroll tab strip and fully reduced body independently, including focus reveal. | Copy pure selector with provenance; port ordering rules/test vectors; replace WPF probing, invalidation, scroll host and layout. | M1 |
| Collapsed flyouts and minimization | [RibbonGroup popup lifecycle](../src/RibbonKit/Controls/RibbonGroup.cs), [PopupDismissHelper](../src/RibbonKit/Controls/PopupDismissHelper.cs); [template contracts](../tests/RibbonKit.Tests/RibbonGroupTemplateTests.cs), [independent press checks](../tests/RibbonKit.Portability.Tests/CollapsedGroupPressChecks.cs), [collapsed/minimized UIA](../tests/RibbonKit.Tests/CollapsedMinimizedAutomationTests.cs). | Popup renders the same group's command state; nested dropdown dismisses before its enclosing group; restore focus on keyboard close. Minimize/Ctrl+F1 later. | Reuse lifecycle requirements, not WPF Popup/capture code or moved visual subtrees. | Flyout M1; minimize M2 |
| Buttons, toggles, split/dropdown commands | [RibbonButton](../src/RibbonKit/Controls/RibbonButton.cs), [RibbonToggleButton](../src/RibbonKit/Controls/RibbonToggleButton.cs), [dropdown](../src/RibbonKit/Controls/RibbonDropDownButton.cs), [split](../src/RibbonKit/Controls/RibbonSplitButton.cs); [Toggle activation assertions](../tests/RibbonKit.Tests/ToggleAutomationActivationTests.cs), [split density checks](../tests/RibbonKit.Portability.Tests/SplitButtonDensityPortabilityChecks.cs). Split primary/arrow are separate actions; the vertical split layout applies only at Large size. | One activation path for mouse, keys, KeyTips and accessibility. Toggle changes state once before Click; dropdown only opens. Split gets separate hit/focus targets. | Reuse state/order scenarios and visual state values; implement painting, capture, hit targets and CLR events. | Buttons/two-state toggles/dropdowns M1; splits, radio/check variants M2 |
| Editors, menus and tooltips | [RibbonComboBox](../src/RibbonKit/Controls/RibbonComboBox.cs), [RibbonTextBox](../src/RibbonKit/Controls/RibbonTextBox.cs), [RibbonMenuItem](../src/RibbonKit/Controls/RibbonMenuItem.cs); [grouping](../tests/RibbonKit.Tests/DropDownGroupingTests.cs), [text edit menus](../tests/RibbonKit.Tests/TextEditMenuTests.cs), [overflow keyboard assertions](../tests/RibbonKit.Tests/OverflowMenuKeyboardTests.cs). Native WPF editing is retained. | Native WinForms TextBox/ComboBox hosted in library-owned slots. Preserve caret/selection, validation, read-only copy and BindingSource updates. Menus support disabled/hidden rows, descriptions, nested Escape and focus return; ScreenTips include accessible descriptions/help. | Reuse grouping/commit/dismiss contracts and strings; reimplement editors' integration, popup navigation and rich tooltip presentation. No simulated text editor. | Basic dropdown rows M1; full editors, nested menus/ScreenTips M2 |
| Galleries | [RibbonGallery](../src/RibbonKit/Controls/RibbonGallery.cs), [InRibbonGallery](../src/RibbonKit/Controls/InRibbonGallery.cs), [gallery panel](../src/RibbonKit/Layout/InRibbonGalleryPanel.cs); [realized-row scrolling](../tests/RibbonKit.Tests/GalleryRowScrollingTests.cs), [popup widths](../tests/RibbonKit.Tests/GalleryPopupWidthTests.cs), [independent gallery checks](../tests/RibbonKit.Portability.Tests/GalleryQuickAccessPortabilityChecks.cs). Preview/cancel differs from selection commit. | Tiles, grouping/filtering, selected-row retention, resizable popup and explicit preview/cancel/commit events. Preserve native scroll endpoints and accessible selection. | Reuse scenario/data specifications; reimplement rendering, tile measurement, scrolling and selection providers. No WPF panel/template reuse. | M4 after M2; projections after M3 |
| QAT placement and overflow | [toolbar](../src/RibbonKit/Controls/RibbonQuickAccessToolBar.cs), [QAT panel](../src/RibbonKit/Layout/RibbonQuickAccessPanel.cs); [overflow](../tests/RibbonKit.Tests/QuickAccessOverflowTests.cs), [keyboard](../tests/RibbonKit.Tests/OverflowMenuKeyboardTests.cs), [UIA](../tests/RibbonKit.Tests/QuickAccessAutomationTests.cs). TitleBar, TabRow, BelowRibbon exist; below-ribbon uses full row width, the cap applies to the other placements. | Source-state presentations, stable order, visibility/placement retained while hidden, overflow reachability and scroll; TabRow/BelowRibbon work in ordinary Forms. TitleBar requires optional host integration. | Reuse placement and overflow specifications; new layout/popup logic. | M3; title-bar host M6 |
| QAT source synchronization/projections | [command catalog](../src/RibbonKit/Controls/RibbonCommandCatalog.cs), [combo proxy](../src/RibbonKit/Controls/RibbonComboBoxQuickAccessProxy.cs), [gallery proxy](../src/RibbonKit/Controls/RibbonGalleryQuickAccessProxy.cs), [group lifetime](../src/RibbonKit/Controls/RibbonGroup.QuickAccess.cs); [combo assertions](../tests/RibbonKit.Tests/ComboBoxQuickAccessTests.cs), [groups](../tests/RibbonKit.Tests/GroupQuickAccessTests.cs), [galleries](../tests/RibbonKit.Tests/GalleryQuickAccessTests.cs). Groups stay collapsed while in QAT; editable-combo QAT is a chooser, typing stays at source. | Several presentation identities refer to one command/state identity; execute once; synchronize label, enabled/checked/selection and source lifetime. Groups appear only in Customize QAT, never Customize Ribbon's available-command list. | Reuse identity rules/scenarios; replace WPF bindings, logical-tree retention and presenter borrowing. Native HWND editors must have only one live owner. | Command projections M3; gallery/group/editor parity depends on their feature milestone |
| File/application menu and Backstage | [RibbonApplicationMenu](../src/RibbonKit/Controls/RibbonApplicationMenu.cs), [Backstage](../src/RibbonKit/Controls/Backstage.cs), [Backstage automation](../src/RibbonKit/Automation/BackstageAutomationPeer.cs); [menu layering](../tests/RibbonKit.Tests/ApplicationMenuLayeringTests.cs), [Backstage UIA](../tests/RibbonKit.Tests/BackstageAutomationTests.cs). Orb/two-pane menu and Backstage are distinct surfaces; multiple designs exist. | Library owns navigation/menu chrome, page switching, keyboard/accessibility and host content slots. Application supplies page Controls, commands and documents. Start with one File/Backstage design, expand individually. | Reuse navigation/selection/region contracts and tokens; replace adorners, templates and page lifetime integration. | M4 |
| Contextual tabs, merging and modal | [RibbonTab contextual properties](../src/RibbonKit/Controls/RibbonTab.cs), [merge service](../src/RibbonKit/Controls/RibbonMergeService.cs), [modal scope](../src/RibbonKit/Controls/RibbonModalScope.cs); [merge/modal assertions](../tests/RibbonKit.Tests/RibbonMergeModalTests.cs), [merge selection](../tests/RibbonKit.Tests/MergeSelectionTests.cs), [retained contract](06-MERGE-AND-MODAL-PLAN.md). | Contextual colors/visibility; stable source ordering, reference-based removal, deterministic selected-tab fallback. Modal scope hides File, preserves QAT, restores authored visibility; transient contributions never enter persistence. Native WinForms MDI remains application-owned. | Reuse service invariants and ordering concepts; implement against component identities. Do not import WPF Window activation or unfinished WPF MDI plans. | M4; persistence cross-check M5 |
| Themes, accents and density | [shared aggregator](../src/RibbonKit/Themes/Office2024.xaml), [Office2024 tokens](../src/RibbonKit/Themes/Tokens.Office2024.xaml), [ThemeManager](../src/RibbonKit/Theming/ThemeManager.cs), [RibbonDensity](../src/RibbonKit/RibbonDensity.cs); [theme contracts](../tests/RibbonKit.Tests/ThemeDictionaryScopeTests.cs), [Touch layout](../tests/RibbonKit.Tests/TouchAdaptiveLayoutTests.cs), [consumer checks](../tests/RibbonKit.Portability.Tests/TouchDensityPortabilityChecks.cs). Five Office generations plus Flat/Crystal palettes; gradients are supported. Compact is default and independent of Classic/Simplified. | One renderer family with named palette/metric/fill values. Office2024 light and Compact first; later accent palettes, gradients, dark/legacy/Crystal and Touch. Per-ribbon scope, propagated to owned popups. | Translate visual values/formulas with provenance; new text/icon/gradient rendering and DPI metrics. WPF effects/backdrops do not transfer. | Office2024 light M1; theme/density expansion M4 |
| Localization and RTL | [localization/provider](../src/RibbonKit/Localization/RibbonLocalization.cs), [strings](../src/RibbonKit/Resources/Strings.resx); [resource/provider assertions](../tests/RibbonKit.Tests/RibbonLocalizationTests.cs), [RTL popup/Backstage tests](../tests/RibbonKit.Tests/RtlBackstageWindowTests.cs). App-authored text stays host-owned; runtime provider refresh exists. | Resource-backed built-in strings and localizable Text properties. Mirror layout, arrows, popup direction and focus traversal independently from editor text direction. | Reuse strings with attribution and scenario expectations; replace markup extensions and FlowDirection handling with WinForms conventions. | Resource/DPI-aware text foundations M1; supported RTL/localization matrix M4 |
| Keyboard, KeyTips and focus | [KeyTipService](../src/RibbonKit/Controls/KeyTipService.cs), [focus tracking](../src/RibbonKit/Controls/FocusVisualTracking.cs); [resolution assertions](../tests/RibbonKit.Tests/KeyTipResolutionTests.cs), [focus tests](../tests/RibbonKit.Tests/KeyboardFocusInteractionTests.cs), [independent keyboard checks](../tests/RibbonKit.Portability.Tests/KeyboardNavigationPortabilityChecks.cs). ASCII A-Z/0-9 keys are normalized and prefix-free; explicit conflicts use deterministic fallback. | Alt release/F10 levels, Escape/back navigation, arrows/Home/End, Tab/F6 routing and visible focus. Native editors own ordinary text/AltGr input. No application-wide shortcut interception outside the owning Form. | Reuse resolver rules/test vectors; optional later isolated resolver copy after dependency audit. Replace WPF input hooks, adorners, traversal and focus coordinates. | Basic keys/KeyTips/focus M1; editors and richer traversal M2 |
| Accessibility | [automation peers](../src/RibbonKit/Automation/RibbonAutomationPeers.cs), [selected-content bridge](../src/RibbonKit/Automation/RibbonTabControlAutomationPeer.cs); [tab command assertions](../tests/RibbonKit.Tests/TabContentAutomationTests.cs), [Toggle action ordering](../tests/RibbonKit.Tests/ToggleAutomationActivationTests.cs), [QAT UIA](../tests/RibbonKit.Tests/QuickAccessAutomationTests.cs). Ledger S15-S23 records bounded external-client review, not certification. | Discoverable tabs/groups/items/popups with roles, names, IDs, live state, screen bounds, focus and actionable patterns. Inactive/closed commands are not falsely presented as visible. | Reuse semantic contracts; entirely new AccessibleObject/UIA provider implementation and external-process acceptance. | Basic tree/Invoke/Toggle/selection/expand M1; each later feature adds its patterns |
| Customization, IDs, storage and Apply/Cancel | [serializer](../src/RibbonKit/Controls/RibbonCustomizationSerializer.cs), [Simplified customization](../src/RibbonKit/Controls/Ribbon.SimplifiedCustomization.cs); [round-trip assertions](../tests/RibbonKit.Tests/RibbonCustomizationSerializerTests.cs), [Simplified reviews](../tests/RibbonKit.Tests/SimplifiedCustomizationReviewTests.cs), [Writer preferences](../tests/RibbonKit.Writer.Tests/Shell/WriterHostPreferencesTests.cs). Stable explicit IDs; malformed/foreign JSON is rejected; transient merge/modal state is excluded. Classic/QAT are live-edit, Simplified is draft, Writer provides host checkpoints. | Stable identities from M1. Later versioned WinForms schema with validation, atomic draft Apply, Cancel rollback to latest applied checkpoint, Reset/Import/Export and application-owned storage. Explicitly document this more uniform native transaction contract. | Reuse schema lessons, validation cases and transaction scenarios; reimplement DTO/application logic without UI types. No promise to load WPF JSON or Writer preferences. | Identity M1; persistence/customization and Simplified M5 |
| Motion and reduced motion | [animation policy](../src/RibbonKit/Animation/RibbonAnimation.cs), [transition context](../src/RibbonKit/Animation/RibbonLayoutTransitionContext.cs), [ribbon lifecycle](../src/RibbonKit/Controls/Ribbon.LayoutTransitions.cs); [host transition assertions](../tests/RibbonKit.Tests/HostLayoutTransitionTests.cs), [independent transitions](../tests/RibbonKit.Portability.Tests/HostLayoutTransitionPortabilityChecks.cs). Library commits geometry and owns completion; host animation is optional. | M1 immediate transitions. Later presentation-only motion with cancellation, system reduce-motion policy and optional host notifications; real layout, input and automation geometry stay coherent. | Reuse lifecycle/policy requirements; new paint scheduling/cancellation. No WPF clocks, transforms or capture helpers. | M6; None policy is inherent in M1 |
| Window integration and designer authoring | [RibbonWindow](../src/RibbonKit/Controls/RibbonWindow.cs), [XAML designer guide](../src/RibbonKit.Design/SETUP-DESIGNTOOLS.md), [toolbox plan](17-TOOLBOX-ITEM-DEFINITION-PLAN.md); [toolbox initialization](../tests/RibbonKit.Tests/DesignerToolboxInitializationTests.cs), [RTL chrome](../tests/RibbonKit.Tests/RtlWindowChromeTests.cs). WPF designer uses isolated net472/Model API and packaged Design folders; A7 reviews critical authoring. | Ordinary Form first; native collection editors, generated InitializeComponent and resource/event serialization, save/reopen and one-step undo. Optional nonclient chrome later. | Reuse authoring scenarios and ownership lessons; replace the entire XAML designer architecture. Host owns DPI declaration, Form lifetime, backdrop and document area. | Basic designer M1; advanced authoring M2/M5; chrome M6 |

## 3. Reuse assessment and provenance

### ReductionAlgorithm is the strongest code candidate

**Verified:** its two `ComputeStates` overloads operate only on numeric arrays and
indices. It has no WPF types or resource access. It returns state indices, exhausts
one ordered group before reducing the next, skips equal/wider states, uses a
scale-aware floating-point tolerance, and validates malformed input. Positive
infinite available width is supported after input validation; impossible fits
return exhausted states rather than silently inventing a fit.

The source tests inspect exact/fractional fits, priority supplied by the caller,
non-monotonic widths, omitted/duplicate indices, fixed children, invalid values,
null/empty tables and combined-width overflow. The history's
[§3.73 record](history/library/06-messages-release-and-window-frames.md#373-reduction-thresholds-priority-order-and-malformed-width-coverage--2026-08-10)
explains the threshold fixes; its historical counts are not a new test run.

**Proposed:** vendor a small internal copy into
`RibbonKit.WinForms/Layout/ReductionAlgorithm.cs`, plus adapted test vectors.
Preserve the MIT license/copyright notice and add `THIRD-PARTY-NOTICES.md` and
`docs/reference/WPF-1.1.0.md` recording repository URL, full commit, original paths,
file hashes, copy date and every adaptation (initially namespace/visibility only).
Keep the original WPF source and public class unchanged. Make the copied class
internal: the WinForms public API need not expose this implementation choice.
Track later WPF fixes by explicit review/cherry-pick of the small copy, not automatic
sync to WPF main. No code has been copied by this planning task.

Copying is preferable to a cross-repository source link: builds remain reproducible
when the WPF checkout is absent. Sharing source within one repo could avoid drift,
but would couple releases/builds and require WPF restructuring. A broader shared
core/package extraction is a separate proposal after two implementations reveal
stable boundaries; it is not part of M1.

The caller still needs a new native implementation of:

- State width measurement, per-item size maps and group caption/spacing inclusion.
- Default `Collapse`; optional `Resize` and `ResizeThenCollapse` sequences.
- Explicit priorities descending, ties by trailing authored index; then unspecified
  groups by largest Large width, ties by trailing authored index. Fixed groups are
  omitted. M1 is LTR; RTL physical tie behavior must be settled in M4.
- Width-cache keys/invalidation and residual scrolling. The pure algorithm's
  rightmost-first default overload does not encode the panel's richer policy.

Other safe reuse is specification-level: token names/values, embedded built-in
strings, ordering formulas and regression scenarios after reviewing attribution.
Customization DTOs require a new compatibility decision; the serializer itself,
command catalog, proxies, theme manager and KeyTip service are UI-coupled.
WPF dependency properties, XAML, templates, automation peers, designer ModelItems,
animation and HWND helpers are reference material, not shared runtime code.

## 4. Recommended native architecture

### Repository, targets and release boundary

**Selected for M1:** use the user's existing `codex/Ribbonkit-Winform` branch and
an isolated `winforms/` subtree in this checkout. It has its own instructions,
solution, SDK selection, development version and API baseline. Its nearest
`Directory.Build.props` prevents inheritance of WPF build/version settings.
The WPF runtime, solution and CI are unchanged. This supersedes the original
sibling-repository location assumption; no nested Git repository is created.

```text
winforms/
  RibbonKit.WinForms.sln
  Directory.Build.props                  # independent version/analyzers
  global.json                            # tested .NET 10 SDK feature band
  src/RibbonKit.WinForms/                 # native runtime, layout, render, input, UIA
  src/RibbonKit.WinForms.Design/          # design-only SDK integration as needed
  samples/RibbonKit.WinForms.Showcase/    # public API only, normal Form
  tests/RibbonKit.WinForms.Tests/         # pure layout and STA behavior
  tests/RibbonKit.WinForms.NativeTests/   # real HWND/UIA/keyboard/popup tests
  tests/RibbonKit.WinForms.Consumer/      # small app, runtime package only
  eng/                                   # own scoped validation/performance scripts
  docs/reference/WPF-1.1.0.md             # pinned source map and copying provenance
  THIRD-PARTY-NOTICES.md
```

Recommend `net10.0-windows`, `UseWindowsForms=true`, nullable enabled, ordinary
Debug/Release configurations and their normal output folders. Start an independent
`0.1.0` development version and package/assembly/namespace `RibbonKit.WinForms`;
do not imply WPF 1.1 compatibility through matching version numbers. Before a
stable release establish XML documentation and API analyzer baselines. Recommend
GitHub Releases distribution following the existing product practice; no publishing
action or new CI configuration is included here.

**Verified external facts, checked 2026-10-26:** [.NET 10 is LTS through
2028-11-14; .NET 8 and 9 support ends 2026-11-10](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core).
[Targeting .NET 10 requires Visual Studio 2026 18.0 or later](https://learn.microsoft.com/en-us/dotnet/core/install/windows),
with SDK feature-band compatibility checked separately. Local read-only inventory
found SDK 10.0.401 and VS installation versions 18.6.11822.322 and 18.10.12217.157.
This is inventory, not a verified designer/workload/package combination. Pin the
actual SDK/VS/Designer.SDK tuple after the first designer round-trip gate.
Use supported Windows 11 as the initial acceptance platform; confirm exact OS
editions/architectures before making distribution claims. Validate x64 first;
x86/Arm64 support requires explicit additional gates.

**Recommendation: do not target .NET Framework 4.8 in M1.** It can be justified by
a named customer/host that cannot migrate, rather than its familiarity. [.NET
Framework follows the underlying Windows lifecycle](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-framework);
it is not inherently unsupported. Supporting net48 would add a second designer
path, different DPI/runtime capabilities, API/polyfill restrictions, dependency
choices and a doubled compatibility matrix. Even the pure selector uses APIs such
as `ThrowIfNull`/`double.IsFinite` that need adaptation there. If required, decide
before finalizing the API: isolate modern-only helpers, evaluate net48 versus
net481 for the named deployment, and budget separate packages/designer/native
acceptance. Do not silently multi-target or lower M1 quality to accommodate an
unidentified legacy consumer.

### Rendering and component model choice

| Approach | Benefit | Cost | Decision |
| --- | --- | --- | --- |
| Child Control for every command | Native focus, binding, designer and accessibility foundations. | Many HWNDs, expensive reflow potential and inconsistent Office geometry; costs need measurement, not assumption. | Retain as an early fallback if custom accessibility cannot meet M1. |
| Every item fully custom drawn | Precise geometry, a small HWND count, common renderer. | Must implement traversal, hit testing, automation and editing; painted text fields are unacceptable. | Use for chrome and non-editing command surfaces only. |
| Mixed | Draw tabs/groups/commands; use real native editors/content Controls where required. | Requires explicit focus/popup/accessible-tree integration. | Recommended. Prove accessibility and designer feasibility early in M1. |

`RibbonControl : Control` is the dockable surface. Tabs, groups and command items
are nonvisual `Component` objects in typed owned collections. Command items
implement `IBindableComponent` and CLR change events; they are not hidden Controls.
An internal presentation references a model item plus a surface identity. Its
hot/pressed/focused/bounds state is separate from authored Text, Enabled, Checked
and identity. Later QAT/Simplified presentations can therefore share state without
moving models or duplicating command execution.

Keep these library layers explicit:

1. **Model/collections:** single logical parent, duplicate/cycle/ID validation,
   membership notifications and change categories. `ISupportInitialize` batches
   designer construction; `BeginUpdate/EndUpdate` coalesces runtime changes in a
   `try/finally`. Collection mutation is on the owning UI thread.
2. **Measurement/layout:** consume immutable item/theme/font/DPI inputs and return
   one layout snapshot: rectangles, size states, scroll extents and navigation
   targets. Measure all permitted states without mutating live Checked/Size or
   invoking events. Cache by content revision, font, theme metrics, DPI and
   available height; width-only resizes reuse state widths. Theme/font/text/icon/
   membership changes invalidate the right cache.
3. **Rendering:** paint a committed snapshot with a theme and state. Paint does not
   change layout, activate commands, allocate persistent GDI objects or create
   popups. Invalidate damaged regions; use WinForms buffering styles.
4. **Hit testing:** use the same snapshot and clipped bounds; distinguish item,
   dropdown arrow, scroll arrow and group opener. No second hand-maintained set
   of geometry. Mouse capture release/cancel/deactivation clears pressed state.
5. **Input/accessibility:** dispatch semantic actions to one activation service;
   navigate semantic targets using the committed snapshot. Marshal provider
   callbacks safely to the UI thread and avoid shutdown/deadlock races.

Internal services are per ribbon/Form, not global coordinators. They cover layout,
popup ownership, keyboard/KeyTips and accessibility. Do not copy WPF dependency
property precedence, routed events, template parts or the Model API. Use ordinary
properties/events, TypeDescriptor metadata, WinForms binding and owned collections.

### Theme, renderer, text, icons and DPI

Use an immutable `RibbonTheme` containing named colors, logical metrics and
`RibbonFill` values (solid or ordered linear-gradient stops), plus a replaceable
`RibbonRenderer` with virtual paint operations for ribbon/tab/group/item/popup/
focus/KeyTip. Keep a single state vocabulary across themes. Renderer callbacks
receive read-only geometry/state and borrowed Graphics; renderers must not alter
hit targets. If custom measurement is later needed, add an explicit measured-item
contract after review, rather than letting Paint secretly change desired size.

The first theme translates Office2024 light values, including default blue
`#2B579A`, from the release tokens. Maintain a mapping file from each adopted WPF
key to the native typed value and any deliberate rendering difference. Supporting
gradient fills in the value model prevents a later legacy theme from requiring a
second renderer architecture. Public custom rendering is library capability; the
sample must not patch internal rectangles or subclass controls for essential paint.

Implemented refinement, 2026-10-26: `RibbonRenderer` is the flat rendering
foundation; `RibbonOffice2024Renderer` owns the current Office presentation and
remains the default. Body/scroll painting and shared text/image/dropdown-arrow/
chrome hooks are renderer-owned. The control retains measurement, geometry,
clipping and input. See the [current renderer/arrow evidence](../winforms/docs/M2-STATUS.md#renderer-and-arrow-refinement)
and [public renderer guidance](../winforms/README.md#renderer-customization).

Use `TextRenderer.MeasureText/DrawText` with matching flags, font, padding,
mnemonic policy, wrapping and ellipsis. Measure large labels consistently with
painting. Do not assume GDI text equals WPF glyph rasterization pixel for pixel;
compare visible bounds, baselines, clipping and interaction states, then approve
native visual baselines. Native editors retain their own text rendering.

`IRibbonIcon` supports resolution-independent drawing into supplied device bounds;
standard Image/LargeImage properties support application images and designer resx
serialization. Icon takes precedence when assigned; otherwise choose the image
for the effective size, with LargeImage falling back to Image. Library chrome/
glyphs are vectors. Icons/images are app-owned, borrowed and never silently
recolored/disposed. Icons are immutable while assigned; replace them to update.
Cache rasterized variants by icon identity, DPI and state. The vector drawing
interface is code-authored in M1; the designer edits the standard image properties.
A future SVG adapter must not require WPF DrawingImage or sample resources.

Store logical metrics at 96 DPI, compute layout in doubles, then round shared
edges once into device pixels; widths come from rounded right-minus-left edges.
Paint, hit testing and accessibility use that same result. Reserve an actual
one-device-pixel hairline where intended. Rebuild font/icon/measurement caches
on DPI changes and recompute popup placement using its target monitor/work area.
Never multiply values already scaled by WinForms AutoScale. M1 host establishes
PerMonitorV2 before handle creation; the library must not change process DPI mode.

**Reported rendering risk, 2026-10-26:** the user has observed .NET GDI/GDI+
rounded rectangles whose four equal-radius corners render differently. This has
not been reproduced in this assessment; the affected API, runtime version and
paint settings remain to be isolated. Do not assume built-in rounded-rectangle
helpers, or the .NET 11 modern renderer, resolve it. Use one library-owned rounded
geometry definition for fill, border, focus and clipping, and verify the actual
rasterization: a custom GraphicsPath alone is not proof of a correction.

F9 must include filled and stroked equal-radius shapes with odd/even bounds,
representative radii and border widths at 100/125/150/200%. Compare reflected corner
coverage with equivalent pixel-grid alignment; inspect all four corners and their
edge joins. Record antialiasing, pixel offset, pen alignment, runtime and DPI. Do
not accept unexplained asymmetry by widening image tolerances. If the GDI/GDI+
path cannot meet the agreed appearance, review another drawing primitive/backend
or an explicit fallback before declaring rounded chrome complete.

### Native editors, popup ownership and focus

M1 uses library-owned `ToolStripDropDown` windows hosting a ribbon-painted popup
surface (`ToolStripControlHost`), with a shared popup-session stack. Measure the
ordinary dropdown and collapsed-group popup with the same theme/layout services.
Reconcile native ToolStrip menu handling with the library keyboard dispatcher so
one keystroke is handled once. This strategy requires the nested/focus/DPI gates;
do not replace it with a sample popup workaround if those fail.

Only the active presentation owns a popup. The stack records owner Form, opener,
parent popup, prior focus, screen bounds and dismissal reason. Escape closes the
innermost layer; an outside press, Form deactivation, owner removal/disposal or
move/resize closes the appropriate chain. Re-clicking an opener must not close
and immediately reopen it. Keyboard close returns to a valid visible opener;
if it disappeared, choose a deterministic ribbon/document fallback. Mouse dismissal
must not steal focus from the control just clicked. No stale capture/message filter
may survive the final close.

M2 hosts actual TextBox/ComboBox Controls in measured slots. Prefer one native
editor instance with explicit ownership transfer when a group moves into a popup;
record/restore its parent, bindings, selection and focus, and end editing before an
incompatible transition. Projections are choosers over the source state, not cloned
HWND editors. Custom content requires a later `RibbonControlHostItem` contract
with stated sizing, focus, accessibility and disposal behavior. The sample does
not reparent editors or implement dismissal itself.

### Events, binding and optional commands

M1 exposes CLR Click, CheckedChanged, SelectedTabChanged and property change events;
binding uses `IBindableComponent.DataBindings`, the ribbon's propagated
BindingContext and the host's BindingSource/INotifyPropertyChanged data. Authored
collections and data-source ownership are separate. General data-generated
tabs/groups are not an implicit M1 feature; explicit factory code can populate
collections. Add a future collection binding adapter only with a defined update,
currency and designer contract.

Toggle activation checks effective enabled/visibility, updates Checked through its
normal property path, raises CheckedChanged, then Click, each once. Programmatic
Checked assignment only raises the state event. `PerformClick`, mouse, keyboard,
KeyTips and accessibility use the same activation path; disposal/reentrant item
removal must safely terminate it. Dropdown activation opens a menu and does not
also invoke a primary command.

M2 may add `System.Windows.Input.ICommand`/CommandParameter as optional interop
without WPF references or routed CommandTarget/CommandManager. EffectiveEnabled
combines authored Enabled, parent state and CanExecute without overwriting a
bound Enabled value. Recheck CanExecute immediately before execution. The order
is state change, Click, then command execution once, matching the tested WPF action
sequence; hosts should use one execution mechanism rather than duplicate work in
both handlers. Unsubscribe on replacement/removal/disposal. Cross-thread command
notifications are marshaled to the UI thread. No command interface is required
to use the WinForms control.

### Accessibility is an M1 design input

Create an AccessibleObject tree for the ribbon, tab list/tab items, selected group
region, groups, items and open popup surfaces. Names come from AccessibleName or
Text; IDs use stable authored identity plus presentation scope. Expose roles,
enabled/checked/selected/expanded/offscreen state, keyboard shortcuts/help and
device-pixel screen bounds derived from the committed clipped layout. Accessible
focus tracks the logical item when the ribbon HWND holds focus. Hosted native
editors later retain their native provider without duplicate painted children.

Required M1 client-observable semantics: Button Invoke, two-state Toggle, tab
Selection/SelectionItem, and dropdown/collapsed-group ExpandCollapse, plus correct
focus, property and structure notifications. Closed popup children and inactive
tab commands must not appear as visible actionable duplicates. Scrolled-off items
need accurate offscreen state and reveal-on-focus behavior. Disabled invocation
must not execute; invoke/toggle must follow the common activation path.

**Feasibility gate:** public [AccessibleObject](https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.accessibleobject?view=windowsdesktop-10.0)
provides the MSAA foundation; do not assume every internal WinForms UIA pattern
hook is overridable by an external library. Prove the required patterns with an
external UIA client on the pinned .NET 10 runtime. Where the public surface is
insufficient, use a library-owned supported Win32 UIA fragment-provider bridge
following Microsoft's [provider guidance](https://learn.microsoft.com/en-us/windows/win32/winauto/uiauto-providerportal),
with public SDK/COM definitions and explicit root/popup ownership. No reflection
into WinForms internals or WPF peer reuse. If that bridge makes the slice too large,
review the child-Control alternative before building the rest of the renderer;
do not reduce the gate to accessible names alone. This remains implementation
work, not a feasibility claim from this assessment.

### Designer authoring and generated code

Modern WinForms uses an [out-of-process designer and Designer.SDK APIs](https://learn.microsoft.com/en-us/dotnet/desktop/winforms/controls-design/designer-differences-framework).
Its advanced [design-time extensions](https://learn.microsoft.com/en-us/dotnet/desktop/winforms/controls-design/designer-overview)
can need client/server separation. Do not transplant the WPF net472 Model API
assembly or its Design-folder packaging convention.

M1 uses parameterless components, stable get-only collection instances marked
`DesignerSerializationVisibility.Content`, Category/DefaultValue/Localizable
metadata, property reset/ShouldSerialize methods where required, and SDK-compatible
collection editors. Tabs, groups and heterogeneous command items must be added,
removed, reordered and configured in the Properties workflow. The design adapter
sites components in the designer container, assigns unique component names and
explicit stable IDs, and reports changes through IComponentChangeService inside
DesignerHost transactions. Destroy/delete must clear membership, event references
and child components coherently. The runtime library loads without Designer.SDK.

Generated `InitializeComponent` must construct each component once, assign scalar
properties/resources/event handlers, then add children in order. Support image/
localizable text resources and typed size-policy conversion; runtime computed
bounds, hot/focus state, popup windows, effective sizes and renderer instances
are not serialized. Start with built-in compatible editors; custom modal UI,
smart tags, full ribbon tree editing and drag/drop authoring are later work.

M1 proves change -> one Undo -> Redo, save -> close designer/VS -> reopen -> build
and run. Check actual generated C# and resx, not just a serializer unit test.
Verify event hookup and a two-way binding survive. Design mode suppresses live
popup activation, timers and command execution. Identify the minimum compatible
design-only assembly/package arrangement during this gate, and verify it in the
small package consumer; do not label designer support complete from a project
reference alone. No separate client assembly is required unless the chosen
editor actually needs custom cross-process UI.

### Resource ownership, disposal and performance

The ribbon owns and disposes its popup windows, internal hosts, measurement caches,
created brushes/pens/paths, subscriptions, filters and later timers. It borrows
Graphics, supplied fonts, icons/images and supplied renderers; never dispose those behind
the application's back. The built-in default renderer is library-owned. Theme
values contain data, not live GDI handles. Cached
GDI objects have bounded lifetimes and are released on invalidation/disposal.
Use Save/Restore around renderer/icon calls. No process-global theme state or
unbounded DPI/theme cache.

An owned collection transfers lifecycle ownership on insertion. Removing an item
detaches it without disposal so it can be reinserted; clearing also detaches.
Disposing a ribbon recursively disposes items still owned by it, idempotently,
including designer-container double-disposal paths. External subscribers remain
the host's responsibility. A future hosted Control has an explicit borrowed versus
owned rule before that API ships. Closing a popup disposes its presentation, not
its command models. Removed items cannot leave live command/binding subscriptions
to the former ribbon.

Record baseline startup, resize, popup latency, allocation, private memory and
GDI/USER handles under §7's fixed workload. No speedup is predicted from the
rendering choice or WPF comparison. Optimize only measured costs while retaining
layout/input/automation semantics. Test many-command stress separately from the
representative product sample.

**Animation risk, 2026-10-26:** the user flags possible hitching in WinForms motion.
This is a risk to investigate, not a reproduced failure or a conclusion that smooth
motion is impossible. The ordinary WinForms timer depends on the UI message pump;
it is not a guarantee of display-synchronized frame delivery. Layout, input and
painting can compete for that thread. [Timer documentation](https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.timer?view=windowsdesktop-10.0).
Double buffering addresses flicker; enabling it alone does not establish steady
frame pacing. [Buffering documentation](https://learn.microsoft.com/en-us/dotnet/desktop/winforms/advanced/how-to-reduce-graphics-flicker-with-double-buffering-for-forms-and-controls).

M1 retains immediate changes. Before later motion is accepted, prototype the
actual transition with real popups/editors and visible native input. Drive progress
from monotonic elapsed time, coalesce pending paints, and avoid allocating/capturing
or recomputing layout on every frame. These are proposed safeguards, not proof of
smoothness. If the selected renderer cannot meet an agreed pacing budget, retain
immediate behavior or review another rendering strategy in that bounded motion
slice. Do not promise WPF motion parity from average FPS or .NET 11 styling alone.

Applications own document/content Controls, command business logic, storage,
selection persistence, icon assets, DPI startup declarations and Form/nonclient
integration. The library owns reusable ribbon behavior. Optional future host
contracts must work without Showcase helpers and must not let a host complete
library transitions or dismiss library popups on its behalf.

**Optional native window, proposed 2026-10-26:** `RibbonForm : Form` is the intended
counterpart to `RibbonWindow` for a later window-integration slice. It would own
reusable title-bar painting, ribbon/chrome placement, caption interaction and
DPI-aware frame geometry. Applications would retain their document Controls,
commands, process DPI declaration and window lifetime; an ordinary Form would
remain supported. Preserve native resizing, system menus, maximize/Snap and
accessibility before adding advanced chrome. Inheriting Form does not provide a
different rendering backend: client-area Mica and animation still need their own
feasibility and native acceptance. This records the proposed direction without
adding a new M1 runtime API.

### WinForms-specific development rules

Use these as the seed for the native repository's own instructions:

- Keep public behavior in the runtime library and verify it through an unrelated
  normal Form; the Showcase consumes public capabilities.
- Use CLR properties/events, owned components and WinForms binding/serialization.
  Add XML/API baselines deliberately; do not recreate dependency properties,
  routed-event infrastructure or XAML templates.
- Treat layout as a side-effect-free calculation; paint, hit testing, focus and
  providers consume its committed geometry. Batch UI-thread mutations.
- Use real editor Controls, public framework/Win32 integration and explicit popup/
  resource ownership. Avoid private WinForms reflection and application-wide hooks
  that outlive the owning Form.
- Keep designer dependencies separate, and prove generated-code/resource/undo
  round-trips on the supported SDK/VS tuple before expanding authoring features.
- Separate pure/STA tests, external-client/native review, DPI hardware checks and
  performance evidence. Normal Debug is for review, Release for measurement and
  packages. A WPF pass never substitutes for a native gate.

## 5. Small proposed public API

This is the M1 review surface, not compiling implementation or a final API baseline.
Standard Control/Component members are omitted. Concrete collection classes expose
normal Add/Insert/Remove/Clear/index/enumeration, validate ownership and support
designer content serialization; no public layout/popup coordinator is proposed.

| Type | Proposed members and defaults |
| --- | --- |
| `RibbonControl : Control` | `Tabs : RibbonTabCollection` (content); `SelectedTab : RibbonTab?`, `SelectedTabChanged`; `Theme : RibbonTheme` (Office2024Light); `Renderer : RibbonRenderer?` (null uses library renderer); `BeginUpdate()`, `EndUpdate()`. Dock.Top; height computed from theme/font/DPI, not a fixed sample height. |
| `RibbonTab : Component` | `Id`, `Text`, `Visible=true`, `Enabled=true`, `KeyTip`; `Groups : RibbonGroupCollection` (content). |
| `RibbonGroup : Component` | `Id`, `Text`, `KeyTip`, `Enabled=true`, `Visible=true`; `Items : RibbonItemCollection` (content); `ReductionMode=Collapse`, `ReductionPriority : int?=null`, `CanResize=true`. |
| `RibbonItem : Component, IBindableComponent` | `Id`, `Text`, `Enabled=true`, `Visible=true`, `KeyTip`, `AccessibleName`, `AccessibleDescription`; borrowed `Image`, `LargeImage` (standard resx) and `Icon : IRibbonIcon?` (code-only, designer-hidden); `SizePolicy : RibbonSizePolicy` (fixed Large); normal property Changed events, DataBindings and BindingContext. |
| `RibbonButtonItem : RibbonItem` | `Click`, `PerformClick()`. |
| `RibbonToggleItem : RibbonButtonItem` | `Checked=false`, `CheckedChanged`; two-state M1. |
| `RibbonDropDownItem : RibbonItem` | `Items : RibbonItemCollection` (content); `DropDownOpening` (cancelable), `DropDownClosed`; `ShowDropDown()`, `CloseDropDown()`. No primary Click. M1 child rows are buttons/toggles; a dropdown inside a collapsed group is supported. |
| `RibbonGroupReductionMode` | `Collapse`, `Resize`, `ResizeThenCollapse`. |
| `RibbonItemSize` / `RibbonSizePolicy` | Large/Medium/Small; immutable typed `Fixed(size)` or `Adaptive(largeState, mediumState, smallState)` mapping, serialized by a tested converter. Effective size is internal presentation state. |
| `RibbonTheme`, `RibbonThemes`, `RibbonFill` | Immutable named values; `RibbonThemes.Office2024Light`; solid and linear-gradient fill definitions. No mutable global palette. |
| `RibbonRenderer`, `IRibbonIcon` | Renderer virtual paint operations with read-only paint contexts. `IRibbonIcon.Draw(Graphics, Rectangle, RibbonIconContext)` receives borrowed graphics, device bounds and effective color/state/DPI. |

IDs must be nonblank and unique within a ribbon across tabs/groups/items; reject
duplicates/cycles and membership in two collections. Validate ID changes atomically.
Designer-created IDs are explicit and serialized, never regenerated on reopening.
Text, component Site.Name, ordinal position and localized labels are not identity.
Changing an ID after saved customization exists will require an application migration
in M5. Collection changes preserve a valid selection or choose the next visible
enabled tab, then the previous one, then none. Runtime selection and bindings must
not be reset by a width-only layout pass.

Proposed usage on an ordinary Form (the types below are the planned API):

```csharp
using RibbonKit.WinForms;
using System.Windows.Forms;

public sealed class EditorForm : Form
{
    public EditorForm()
    {
        Text = "Native RibbonKit consumer";
        var status = new Label { Dock = DockStyle.Fill, Text = "Ready" };
        var ribbon = new RibbonControl
        {
            Dock = DockStyle.Top,
            Theme = RibbonThemes.Office2024Light
        };
        var home = new RibbonTab { Id = "tab.home", Text = "Home", KeyTip = "H" };
        var insert = new RibbonTab { Id = "tab.insert", Text = "Insert", KeyTip = "N" };
        var clipboard = new RibbonGroup
        {
            Id = "group.clipboard", Text = "Clipboard",
            ReductionMode = RibbonGroupReductionMode.ResizeThenCollapse
        };
        var copy = new RibbonButtonItem
        {
            Id = "cmd.copy", Text = "Copy", KeyTip = "C",
            SizePolicy = RibbonSizePolicy.Adaptive(
                RibbonItemSize.Large, RibbonItemSize.Medium, RibbonItemSize.Small)
        };
        copy.Click += (_, _) => status.Text = "Copy requested";
        clipboard.Items.Add(copy);
        home.Groups.Add(clipboard);

        var font = new RibbonGroup { Id = "group.font", Text = "Font" };
        var bold = new RibbonToggleItem { Id = "cmd.bold", Text = "Bold", KeyTip = "B" };
        bold.CheckedChanged += (_, _) => status.Text = $"Bold: {bold.Checked}";
        font.Items.Add(bold);
        home.Groups.Add(font);

        var illustrations = new RibbonGroup { Id = "group.illustrations", Text = "Illustrations" };
        var shapes = new RibbonDropDownItem { Id = "cmd.shapes", Text = "Shapes", KeyTip = "S" };
        var rectangle = new RibbonButtonItem { Id = "cmd.rectangle", Text = "Rectangle", KeyTip = "R" };
        rectangle.Click += (_, _) => status.Text = "Rectangle requested";
        shapes.Items.Add(rectangle);
        illustrations.Items.Add(shapes);
        insert.Groups.Add(illustrations);
        ribbon.Tabs.Add(home);
        ribbon.Tabs.Add(insert);
        ribbon.SelectedTab = home;
        Controls.Add(status);
        Controls.Add(ribbon);
    }
}
```

The application entry point configures PerMonitorV2 and normal WinForms startup
before constructing this Form. The full M1 sample adds enough commands/groups to
exercise all reduction states and residual scrolling; it does not implement any
ribbon layout or popup code. Standard two-way binding is intended to work as:

```csharp
bold.DataBindings.Add(new Binding(
    nameof(RibbonToggleItem.Checked), bindingSource, "Bold",
    formattingEnabled: true, DataSourceUpdateMode.OnPropertyChanged));
```

The binding example belongs in the same scope as `bold`, with an application-owned
BindingSource. M2's optional Command/CommandParameter, native editor items and split
items are additive proposals, not hidden M1 commitments. QAT, File/Backstage,
customization, Simplified, density selectors and custom window classes are absent
from this initial public surface.

## 6. Phased roadmap and completion gates

No calendar estimate or parity promise is justified before M1 measurements and
designer/accessibility feasibility. Each milestone closes only its stated scope.

| Phase | Dependencies and deliverable | Completion gate |
| --- | --- | --- |
| M0: scope/API decision | This assessment; choose repository/framework/platform defaults and review the API/ownership rules. | Record decisions D1-D4 below. Next task is explicitly bounded M1 implementation; no WPF restructuring/shared-core extraction. |
| M1: complete native foundation | M0. Ordinary Form, two tabs, representative groups, button/toggle/dropdown, Office2024 light, adaptive sizing, collapsed flyout, keyboard/KeyTips, accessible actions, designer collection serialization and independent consumer. | All §7 required gates pass or have an explicit accepted hardware limitation. Designer/provider feasibility happens first within M1; a failed feasibility gate triggers architecture review before expanding. M1 is not full WPF parity. |
| M2: editing and richer interaction | M1. Native editors, split/check/radio variants, hierarchical menus, ScreenTips/help, minimize, extended keyboard/editor focus integration; optional ICommand. | BindingSource updates and caret/selection survive resize/popup/commit/cancel; nested dismissal, UIA Value/selection/expand patterns, event order and designer resources pass. Mouse/keyboard acceptance required; OS IME/native touch are separate optional scopes, not automatic gates. |
| M3: QAT and projections | M1/M2 command/state contracts. TabRow/BelowRibbon, visibility/order/overflow, source commands and QAT-only declarations. | Source/copy update and execution-once checks, inactive-source behavior, overflow navigation, removal/reinsert/disposal and independent-consumer review. Add combo/group projections only when their source feature works; gallery projection follows M4. TitleBar remains M6. |
| M4: broader surfaces | M2/M3 foundations. Additional themes/accents/gradients, Compact/Touch, localization/RTL, galleries, File/application menu/Backstage, contextual tabs, merge/modal services. Deliver as separate bounded slices. | Per-slice native visuals/input/DPI/UIA/designer gates; mixed-direction editors differ from mirrored layout; gallery preview/cancel/commit proven. Merge/modal state/order/restoration and transient provenance tested before M5 persistence integration. Full contrast-theme support needs its own defined slice. |
| M5: customization and Simplified Ribbon | M3 and relevant M4 services; stable IDs from M1. Versioned structural persistence, draft/Apply/Cancel, custom tabs/groups, QAT commands, Simplified pin/overflow/customization, feature policies. | Malformed/foreign/unknown/future schema cases, restart and stable IDs, Reset/Import/Export, Apply -> further edits -> Cancel checkpoint, transient exclusion, projection sync and independent app acceptance. Groups remain QAT-only available entries. Storage and appearance mode persistence remain host-owned. |
| M6: motion and optional window integration | Stable earlier geometry/lifetimes; explicit new scope. Reduced-motion-aware presentation, interruptions, host synchronization, title-bar QAT, optional chrome/backdrop integration. | Meet the later-motion frame-pacing protocol below, including first-use and concurrent resize/popup activity. Cleanup and immediate final geometry under interruption/reduced motion; command ownership/input/UIA remain coherent. Independent ordinary Form still works. Validate native hit testing, maximize/Snap/DPI and genuine mixed-monitor behavior for each optional host integration. |

**M3A hosting boundary, 2026-10-27:** command declarations, borrowed source
references, row measurement and rendering remain independent of placement.
The current API supports TabRow/BelowRibbon in an ordinary Form. The user
explicitly requested future title-bar QAT support; M6's optional RibbonForm/window
integration must reuse those declarations and state, without requiring command
recreation, model reparenting or a second owner. An unsupported TitleBar value
is not exposed ahead of that host. No title-bar/chrome implementation is included
in M3A. [Current implementation and next projection scope](../winforms/docs/M3-STATUS.md).

Milestones are dependency groups, not invitations to implement all deferred features
at once. Each new item type needs public-API review, designer serialization,
automation, binding/lifetime and independent-consumer evidence before it is called
portable. Native WinForms MDI/document hosting is a separate host concern even if
it later consumes the merge service.

Current phase override, 2026-10-26: the user explicitly prioritized necessary
structure and deferred accessibility compatibility. New accessibility providers,
patterns, screen-reader work and accessibility sign-off are future work, rather
than gates for the current structural slices. Retain the existing foundation bridge
and its recorded evidence; do not claim new items are accessibility-compatible.
The M1/M2 status ledgers separate these deferred compatibility gates from current
mouse/keyboard/layout/public-consumer verification.

### Deferred theming consolidation

User sequencing decision, **2026-10-27**: continue the main component slices
first. Consolidate appearance settings after most components exist, before final
visual polishing and native public API stabilization. This is a cross-milestone
checkpoint, not the next M2 slice or authorization to build additional themes now.
Later theme/palette expansion should build on that consolidated contract.

During component work, keep reusable appearance in shared renderers and keep
measurement/spacing centralized and DPI-aware. Preserve the existing theme
settings and extension points; add a setting only where the current slice needs
it. Record any remaining appearance constants for the later inventory without
starting a comprehensive theme API refactor in each component slice.

The later pass will audit all implemented ribbon surfaces, hosted-editor frames,
menus, popups and interaction states, then propose a bounded theme contract for:

- Colors/fills, gradients, text and separators; normal, hover, pressed, checked,
  open, disabled and keyboard-focus states.
- Border color, thickness and visible sides; component-specific roles with shared
  defaults/fallbacks rather than accidental coupling between unrelated controls.
- Content padding, outer margins, icon/text gaps, row spacing and scroll-button
  insets; independent body, button, editor and popup corner radii.
- Shadow enablement, color/opacity, blur radius and offset, with matching reserved
  drawing space; typography roles and arrow/check/radio/tab-indicator metrics.

Office 2024 follow-up, **2026-10-28**: the BelowRibbon QAT should visually extend
the ribbon body, following the user's WPF reference. Include a joined background
and side frame, a subtle internal separator, bottom corners at the QAT's lower
edge and one outer body/QAT shadow. Coordinate shared measurement, clipping and
painting so the join has no gap, doubled border or intermediate body shadow.
This belongs to M4I's theming/geometry pass; keep QAT models, command ownership and
placement behavior independent. Review expanded and minimized presentations
separately, representative DPI and a neutral renderer without imposing Office's
joined-panel styling on every theme.

The bounded joined-QAT follow-up is implemented as M4I1; human review remains
pending. Its current API, automated/native evidence and limits are recorded in
the [M4 ledger](../winforms/docs/M4-STATUS.md#m4i1-office-2024-joined-belowribbon-qat--2026-10-28).
This does not complete the comprehensive inventory or authorize every later theme.

Message-panel foundation is implemented as M4J, with human review pending in the
[M4 ledger](../winforms/docs/M4-STATUS.md#m4j-message-panel-foundation--2026-10-28).
User reminder,
**2026-10-28**: an open panel beneath the BelowRibbon QAT changes the adjoining
QAT bottom corners from rounded to square. Its eventual layout/frame contract
must cover panel visibility, stacking, minimized state, margins and shadow
ownership before the deferred localization/RTL pass; do not bake the current
standalone QAT bottom edge into the message-panel presentation. When the body is
minimized and QAT is not BelowRibbon, messages instead expose their standalone top
edge; this condition also covers future title-bar QAT hosting. Preceding body
shadows must not obscure message content. The completed scope and focused evidence
are recorded in that ledger; message accessibility, motion and later palettes
retain their separate gates.

Themes supply appearance values; renderers draw them. Layout, clipping, popup
placement and hit targets must consume the same relevant geometry. Keep values
in 96-DPI logical units and preserve the four-corner raster verification rule.
Validate coverage using the existing Office 2024 presentation and a deliberately
different neutral configuration, including another public-API consumer. Inspect
remaining hardcoded values and every implemented state before calling the pass
complete. Future components must join the same contract and coverage inventory.
Backdrop blur/Mica remains an optional M6 host/window feature; this pass's blur
settings describe shadow softness and do not expand backdrop scope.

## 7. Implementation-ready M1 work package

### Fixed scope and work sequence

Use normal Debug for local review, normal Release for package/performance gates.
Create no alternate task configurations/output directories. Keep all reusable
layout, rendering, key handling, popups and accessibility in the runtime library.

1. Establish the sibling solution and independent runtime/sample/tests. Record SDK,
   Windows build, VS/Designer.SDK combination, architecture, source provenance and
   proposed public API baseline. Inventory the actual available monitors.
2. Build the component/collection model and a minimal painted ribbon with one tab,
   one group, a button, toggle and popup. Immediately prove designer edit/undo/
   save/reopen and external UIA Invoke/Toggle/selection/expand. This is an early
   checkpoint of M1, not a substitute for the full slice.
3. Vendor the pure selector/tests with attribution. Implement native measuring,
   group modes/priorities, caching, shared rounded geometry and residual scrolling.
   Add two tabs and the complete sample scenario below.
4. Complete Office2024 light paint, hover/press/check/disabled/focus states, icons,
   keyboard/KeyTips and nested dropdown-in-collapsed-group lifecycle. Ensure the
   two-way Checked binding and event handlers survive presentation changes.
5. Build a small consumer that references only the packed native runtime (and
   design-only package where needed), using its own Form, icons and handlers.
   Repeat the behavior/designer/native gates there, then record performance and
   human DPI review. Close M1 with the evidence ledger and remaining limits.

Sample fixture: Home has Clipboard (adaptive Copy/Paste actions), Font (Bold/Italic
toggles and disabled action), and Editing (dropdown). Insert has Illustrations
(dropdown) and Links (buttons). Include one fixed-size item, one CanResize=false
group and an explicit reduction priority. Use non-conflicting authored KeyTips,
long labels and app-supplied vector icons. Provide a normal document-area TextBox
to prove focus entry/return without building Writer or document editing features.

At wide width show expanded groups; at intermediate thresholds demonstrate all
opted-in sizes; at narrow width show collapsed groups; below the sum of minimum
widths show usable body scrolling. Exact device thresholds are measured from the
fixture at each DPI, not hardcoded from WPF screenshots. No File button, QAT,
gallery, editor item, Backstage, customization UI, Simplified, Touch-density selector,
animated transition or custom Form chrome is required for M1.

### Verification checklist (all currently Not run for WinForms)

| ID | Required evidence and pass condition |
| --- | --- |
| F1 Build/API/package | Normal Debug build for review and Release build/test/pack; XML/API checks pass. Runtime has no WPF or designer runtime dependency. Package consumer restores from an isolated local source and builds/runs without either source checkout or Showcase assets. Package ID/version/provenance belong to WinForms. |
| F2 Pure selection | Execute adapted pure tests: exact/fractional fit, non-monotonic/equal states, impossible widths, fixed groups, valid/custom/duplicate/omitted order and malformed widths/indices. Test native caller ordering separately; the pure copy cannot prove it. |
| F3 Real native layout | STA with actual HWNDs/message loop: full permitted state sequences; priority/fixed-item behavior; width decrease/increase reversibility; long-label/font/collection changes invalidate cached widths; repeated identical layouts stabilize. Every command is reachable after residual/tab scrolling; hidden/inactive items do not get hit targets. Adjacent rounded edges agree at fractional DPI. |
| F4 Commands/binding | Mouse, Enter/Space, KeyTip, PerformClick and external Invoke/Toggle each execute once; disabled does nothing. CheckedChanged precedes Click; Checked assignment does not Click. Two-way BindingSource state survives tab switch, resize, group popup and reopen. Removal/disposal during handlers is safe. |
| F5 Popups/lifetime | First open and repeat open, opener re-click, outside press, nested Escape, deactivate, move/resize, owner removal and Form disposal. Dropdown inside collapsed popup closes first; keyboard returns focus, outside click retains the clicked target's focus. No orphan HWND/filter/capture or duplicate item model. Anchors stay within work area at each reviewed DPI. |
| F6 Keyboard/KeyTips | Alt press/release and F10 enter/exit; tab -> commands -> dropdown levels; Escape ascends then restores prior content focus. Tab/Shift+Tab/F6, arrows/Home/End, disabled/hidden skipping and focus reveal pass. Resolver handles explicit conflicts/prefix collisions/non-Latin fallback; badges are typeable and aligned. Only one input dispatcher handles each message; unrelated Form shortcuts and AltGr are not consumed. |
| F7 Accessibility | External-process UIA tree traversal and actions with a real Form: names/IDs/roles/patterns/states/bounds; visible selected-tab content and open popup discovery; closed/inactive/offscreen distinctions; focus and state/structure events; disabled invocation. In-process AccessibleObject calls alone do not close this gate. Record any unsupported pattern as a failure/gap. Narrator-specific work and manual Narrator review are outside scope by user direction, 2026-10-26. |
| F8 Designer | Supported VS/.NET/SDK tuple: add ribbon; collection-edit two tabs/groups/heterogeneous rows; set Text/KeyTip/IDs/size/Checked and image resource; wire Click and a binding. Reorder/remove with one-step undo/redo; save, close/reopen designer and VS, inspect generated C#/resx, build/run. No duplicate IDs/items, lost resources/events/bindings, runtime-only state or designer dependency in running app. Repeat key round-trip against package consumer. |
| F9 Native appearance/DPI | Human inspect 100%, 125%, 150%, 200% on available display(s): wide/narrow, long text, hover/press/checked/disabled/focus, tab scroll/body scroll, collapsed popup and nested dropdown. Include the four-corner symmetry/edge-join checks specified in the rendering section, with filled/stroked shapes and odd/even bounds. Compare the fixed Office2024 light WPF reference scenario to native screenshots for geometry/state semantics; approve native rasterization separately. Verify actual Windows scale, not just synthetic dimensions. Restore original display settings. |
| F10 Monitor boundary | If two different-scale monitors are available: move the Form with popup closed/open, re-open at destination and verify sizing/placement/focus. Otherwise record hardware inventory and mark genuine movement **Unavailable**, with an explicit disposition before M1 closure; simulated DPI does not pass this row. |
| F11 Independent consumer | Use its own normal Form, icons, BindingSource and handlers with only public runtime/design capabilities. Verify startup, resize, popup, keyboard, UIA and designer scenarios. No Showcase reference, internal-access grant, reflection, sample renderer fix or copied input helper. Any such dependency is an open portability gap. |
| F12 Stability/performance | Complete the fixed workload below outside the debugger in Release, with no exception, stranded popup or sustained resource growth. Store raw measurements plus environment and limitations; investigate outliers before deciding a budget. Baseline creation is required; beating WPF or an invented latency target is not. |

Use meaningful unit tests for the pure engine/model/identity rules, STA integration
for real layout and lifetime, native keyboard automation for focus-sensitive cases,
and external-client UIA tests. Serialize tests sharing UI focus/desktop state.
Capture native screenshots only after layout stabilizes, recording theme, DPI,
font and state. Inspect actual/diff images before changing approved baselines; do
not copy WPF approval images into a WinForms pass/fail test.

No native touch or OS IME acceptance is added by default. M1 has no ribbon editor
item. Record those scopes as outside M1; later native editor work retains synthetic
binding/editing checks and explicitly states whether genuine OS IME was reviewed.
Full RTL, high-contrast certification and non-x64 platforms remain later explicit
scopes rather than implied by a successful light/LTR sample.

### Baseline measurement protocol

Record machine/CPU, Windows build, display/scale, power mode, SDK/runtime, Release
revision, command count and exact app settings. Use the same application workload
for all repetitions; no WPF/WinForms performance comparison is promised.

- **Startup:** at least 10 process launches from the same built executable, time
  process start to the first completed ribbon paint. This is an instrumented
  readiness proxy, not measured compositor/display latency. Report all samples,
  median and range; label first launch separately and subsequent warm launches.
  File-cache cold startup needs a separate controlled protocol; do not call the
  first launch a proven cold-cache measurement.
- **Resize:** after one warm-up, run three identical 30-second paced width sweeps
  crossing expanded, stepped, collapsed and scrolling thresholds. Record layout/
  paint count and elapsed time, UI-thread work, process CPU, allocation and paint
  intervals (median/p95/max). Distinguish instrumented/offscreen timings from
  native visible responsiveness; do a live drag review too.
- **Popups:** warm-up then 100 open/close cycles each for a normal dropdown and
  collapsed group, with periodic nested dropdowns. Record first and warmed
  open-to-ready paint latency and distributions. At checkpoints record managed/
  private bytes and GDI/USER/total handle counts; after disposal/idle verify owned
  resources/subscriptions return to a bounded plateau. A single GC snapshot is
  not a leak proof. Repeat an investigation only if growth/outliers justify it.

The first report establishes a baseline and a proposed budget for later slices.
Do not manufacture performance acceptance thresholds after seeing results; document
observations, reproducibility and any proposed follow-up with the user. A functional
freeze, sustained leak, runaway layout or stranded popup blocks M1 irrespective
of averages.

### Later-motion frame-pacing protocol

This is a gate for separately implemented motion, not an animation requirement
added to M1. Agree the workload and pacing budget before judging results. Record
the actual display refresh rate, renderer/runtime, DPI, transition and command
count. Exercise first-use and warmed motion, rapid interruptions, live resize,
popup opening/closing and native-editor focus alongside the animation. Include
reduced motion and immediate fallback behavior.

Capture frame/paint intervals as distributions (p50/p95/p99/max), deadline overruns
against the selected display cadence, and input responsiveness. Correlate long
intervals with layout, capture, allocation/GC and popup/handle activity where
instrumentation allows. Paint completion remains a proxy for visible presentation;
retain a live/native review or suitable presentation trace. A good average FPS,
offscreen timing, animation-completion test or double-buffering setting does not
close this gate. Reproduce visible hitches and disposition their causes before
claiming that transition is accepted; do not generalize a passing fade to group,
density, layout-mode or QAT geometry motion.

### Evidence ledger format and completion rule

Native commands from `winforms/` (actual results belong in the M1 ledger):

```powershell
dotnet build RibbonKit.WinForms.sln --configuration Debug
dotnet build RibbonKit.WinForms.sln --configuration Release
dotnet test tests/RibbonKit.WinForms.Tests/RibbonKit.WinForms.Tests.csproj --configuration Release --no-build
dotnet run --project tests/RibbonKit.WinForms.NativeTests --configuration Release --no-build
dotnet run --project tests/RibbonKit.WinForms.NativeTests --configuration Release --no-build -- --keyboard
dotnet pack src/RibbonKit.WinForms/RibbonKit.WinForms.csproj --configuration Release --no-build
```

Run native tests sequentially on an available interactive desktop; a headless
run cannot close native focus/designer/hardware gates. The independent consumer
must restore the produced package through its own isolated-source build, rather
than a project reference. `eng/Test-Native.ps1 -Configuration Release -Package`
provides that isolated restore/build/native-input check. These commands do not alter WPF CI.

For each F row record status (Not run/Passed/Failed/Unavailable/Accepted limitation),
date, revision, configuration/runtime, Windows/VS/DPI scope, actual cases, result,
artifact location and remaining limit. Keep automated, external-native, human and
target-machine results separate. A failed early run says nothing about unexecuted
later cases. Once the relevant checks pass, repeat only for changed code, failures
or unresolved risks. M1 is complete when F1-F9/F11-F12 pass and F10 either passes
or has a visible accepted hardware disposition. A designer/accessibility failure
cannot be relabeled as an unavailable hardware check.

## 8. Open decisions that materially change scope

These decisions do not block this plan. The defaults make the next task concrete;
change them before committing to the corresponding API/compatibility contract.

| ID | Recommendation / assumption | What a different answer changes |
| --- | --- | --- |
| D1 Repository/release identity | Selected: isolated `winforms/` subtree on the user's `codex/Ribbonkit-Winform` branch; independent 0.1 development version. GitHub release publication remains future work. | Native build/version/API settings are isolated. No WPF restructuring, CI changes or publication is authorized. |
| D2 Supported hosts | .NET 10 only, Windows 11/x64 first; no net48 without a named deployment requirement. | Legacy framework support adds API restrictions, separate designer paths and native validation; x86/Arm64/other Windows versions add target-machine gates. |
| D3 Rendering/provider feasibility | Mixed custom drawing/native editors, with an early public-API accessibility/provider proof and designer package proof. | If public integration is insufficient, approve a supported provider bridge or revise toward child Controls before broad layout implementation. No internal WinForms reflection. |
| D4 Public model and ownership | Nonvisual components, typed owned collections, stable IDs, standard binding/events, borrowed icons/renderers; independent native API. | Choosing a Control per item, universal data-generated hierarchy or binary compatibility with another ribbon library changes the core design and designer/automation work. |
| D5 Persistence compatibility, before M5 | Independent WinForms schema and uniform draft/Apply/Cancel-to-latest-Apply semantics. | WPF JSON interchange, importing Writer settings or retaining live-edit classic/QAT semantics requires an explicit migration/transaction design and tests. |
| D6 Visual/platform expansion | Office2024 light/Compact/LTR/instant M1; add later palettes, RTL, Touch, custom content and chrome in bounded slices. | Requiring any of them in M1 expands its native review, API and lifetime gates. Full pixel equality with WPF text is not a defensible acceptance metric. |

The implementation covers the M1 foundation, M2A split buttons and the bounded
M2B TextBox/M2C ComboBox/M2D check/radio/M2E ScreenTip/M2F menu/M2G minimize slices
under these recorded decisions; [the M2 ledger](../winforms/docs/M2-STATUS.md) owns
the current slice scope and remaining review.
Its [acceptance ledger](../winforms/docs/M1-STATUS.md) keeps the remaining designer,
human appearance and hardware gates open; M1 is not yet accepted.
Broader shared-core extraction and other roadmap features remain separate work
proposals. The user authorized M2 continuation without claiming full M1 acceptance.
