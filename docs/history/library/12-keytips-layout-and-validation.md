# Library history: KeyTips, layout and validation

> Archived implementation evidence. Dates, test counts and pending work describe
> their original checkpoints; later records may supersede them.

[History directory](../01-library.md) · [Current status](../../../04-DESIGN-NOTES.md#5-current-state--next-steps) · [Working agreement](../../../AGENTS.md).

### 3.234 Combo-copy KeyTips hand control to the choice list — 2026-10-09

Combo-box dropdown copies were classified as ordinary menu openers, so activating
a custom-tab or overflow copy produced a KeyTip for each choice. Direct QAT
activation also lacked an invocation path for the dropdown projection's automation
peer, which exposes ExpandCollapse rather than Invoke/Toggle. Eight realized-window
reproductions failed before the correction in QAT, overflow, expanded custom groups
and collapsed groups, in both directions.

`KeyTipService` now treats the internal combo projection as an input leaf. Its
opener keeps its badge; activation focuses the opener, opens the choice list and
exits KeyTip mode. Choice rows receive no badges, and the containing overflow or
collapsed-group popup stays available while the user navigates. The existing
source selection, arrow/Enter/Escape and focus-return behavior remain in use.
Ordinary dropdown and split-button menus still descend into their KeyTip levels.
No public API, template or animation policy changed.

The final focused Release selection passed 73 checks, including ten new combo/menu
KeyTip cases and existing input, KeyTip, overflow, retained-command and collapsed
group coverage. The independent RibbonKit-only consumer passed 24 QAT/overflow/
custom-tab scenarios across Office 2007/Crystal, Compact/Touch and LTR/RTL, plus
its existing nested-overflow keyboard/mouse checks. It adds the custom-tab copy
through the public customization page, with the source input on an inactive tab.
The collapsed fixture uses real adaptive reduction: manually forcing a fixed
group's size state is unstable because a later probe restores its Large state.

A broader combined run failed three existing native focus-outline cases; a fresh
eight-case focus-only rerun passed seven and still failed Office 2013 RTL accent
focus because the expected adorner collection was null. This remains an unresolved
verification limit, with no focus implementation or approval changes. Earlier
combo input failures passed in the final focused run; their intermittent cause
was not established. Both runtime targets and normal Debug Showcase built with
zero warnings/errors. User live review and the full suite remain pending/deferred.

### 3.235 Touch stacked custom groups keep three rows per column — 2026-10-09

The user's custom group showed a fourth command below the first three in Touch
mode, increasing the ribbon's height. Stacked groups used a vertical WrapPanel
whose wrapping depended on the height constraint. Compact supplies that constraint;
Touch uses an automatically sized groups row to accommodate larger targets, so
the panel could keep extending its first column.

The generated Stacked items panel now derives from WrapPanel and limits Touch
columns to three noncollapsed commands during measure and arrange. Commands four
and seven start the next columns, retaining the ribbon's height for three rows.
Each column measures its actual command widths and heights; existing target-size
tokens supply the control geometry. WPF mirrors the columns in RTL. Compact still
uses WrapPanel's existing layout, and Default/Large layouts use their existing
panels. The same panel works after content moves into a collapsed-group flyout.
There are no new public APIs or theme tokens.

All eight realized-window reproductions failed before the fix: adding the fourth
Touch command increased the measured ribbon height from 224 to 269.6 DIP. They
now pass with combo, dropdown and gallery copies, initial Touch or density toggles,
LTR/RTL, four through seven commands, collapsed-command visibility, removal and
collapsed-group popup layout. The final focused Release selection passed 64
checks, including existing adaptive sizing, density animation, customization and
combo KeyTip regressions.

A RibbonKit-only consumer creates the copies through the public customization
page with their source tab inactive. Its 48 theme/density/direction/simulated-DPI
combinations passed, including stable height after each added or removed command.
Office 2024 LTR and Crystal RTL Touch previews were inspected without changing
snapshot approvals. Release solution and normal Debug Showcase builds passed with
zero warnings/errors. User live retesting remains pending; the full suite and
native mixed-monitor DPI review remain deferred.

### 3.236 Touch horizontal split buttons retain a larger primary action — 2026-10-09

The shared Touch template gave both split halves a 44-DIP minimum width. Large
horizontal content could shrink to 48 DIP beside the 44-DIP arrow, and small/QAT
halves could be equal. The independent consumer reproduced this before the fix.
Horizontal primary targets now have a 56-DIP minimum through the matching
`Touch.SplitPrimaryMinWidth` token in every base palette; Large horizontal
primaries use the existing 72-DIP Large-button minimum. The arrow retains its
44-DIP touch target. Vertical halves still span the same width, and Compact
geometry, menu borrowing, primary routing and public APIs are unchanged.

The RibbonKit-only regression passed 288 theme/light-dark/direction/simulated-DPI/
density/QAT-placement combinations, plus Compact return checks, Large/Medium/
Small ribbon buttons, reduced vertical layouts, source-backed QAT copies, scoped
metric overrides and primary/menu invocation. LTR/RTL Office 2024 Touch renders
were inspected. The focused runtime selection passed 35 checks for hover,
Showcase sizing, adaptive groups and menu borrowing. Release solution and normal
Debug Showcase builds passed without warnings or errors.

The broader Touch consumer failed its existing gallery selected-row visibility
assertion (item top 224 DIP against a 56-DIP viewport). Removing only the new
split triggers reproduced the same failure; the corrected template was restored
and rebuilt, and its focused consumer passed again. That gallery check remains
an unresolved verification gap. Full-suite and native touch/DPI review were not
rerun; user live split-button acceptance remains pending.

### 3.237 Gallery row scrolling stays aligned after direction reversals — 2026-10-09

The user's recording shows compact Theme tiles drifting vertically after browsing
to the bottom and reversing, then drifting the other way after reaching the top.
The arrow handler advanced by `ViewportHeight`; fractional-DPI rounding, grouping
and varying row heights make that different from the arranged tile-row pitch.
The native end clamp then carried its remainder into every subsequent step.
Six of ten new realized-window cases reproduced the error before the correction,
including grouped and two-column Compact galleries and animated LTR/RTL browsing.

`InRibbonGallery` now finds the nearest arranged strip row and scrolls to its
adjacent row using current container coordinates plus the native vertical offset.
First/last steps retain the native scroll limits even when the last row is taller
or shorter than the viewport. Expanded native/QAT popups and logical-scroll
custom templates retain viewport paging. Selection, panning, animation policy,
frozen popup borrowing and public APIs are unchanged.

All ten new cases pass two complete direction-reversal cycles in Compact/Touch,
grouped and multi-column layouts, LTR/RTL, and with motion enabled/disabled.
The wider gallery selection passed 88 checks; its one failure was an existing
template-test substring lookup matching both `RibbonComboBox` and
`RibbonComboBoxQuickAccessItem`, also present in HEAD. Restricting that helper to
the exact type name retains the intended surface assertions, and all six
dark-theme template-contract checks pass.

The separate RibbonKit-only grouped-gallery consumer passed 96 theme/palette/
density/direction/simulated-DPI combinations with repeated endpoint reversals,
stable row origins, retained selection and existing popup/width-binding checks.
Office 2024 LTR and Crystal RTL strip renders were inspected. Release solution
and normal Debug Showcase builds passed with zero warnings/errors. New live
row-scrolling acceptance, full-suite and native monitor/DPI checks remain pending
or deferred. The earlier broader Touch consumer's selection-visibility assertion
is a separate unresolved verification gap.

### 3.238 Focused local validation and visual scene selection — 2026-10-09

Local validation no longer treats every shared-template edit as a full-solution
gate. Bounded appearance changes use affected builds, rendering/geometry checks,
selected scenes and appropriate independent-consumer coverage. Broad shared
behavior/template contracts, public API/build changes and release readiness keep
full validation. Existing regression tests remain; this change improves selection
rather than removing coverage without a redundancy review.

`eng/Test-Focused.ps1` requires an explicit Runtime/Writer filter, Visual scene
selection, or existing Portability scope. It builds normal Debug outputs by
default, supports normal Release validation, rejects zero discovered tests, and
temporarily clears/restores selection and capture/update variables. Portability
scope validation uses exact casing because the consumer harness compares its
environment value case-sensitively; an unrecognized value would otherwise fall
through to the full consumer. CI explicitly clears these variables and continues
to run the full solution, package and package-consumer gate.

Visual selections accept comma-separated exact scene names or case-insensitive
`*`/`?` patterns without `.png`; every pattern must match. Unselected scenes skip
bitmap rendering/comparison, selected scenes retain double-render determinism,
approvals and thresholds, and resource/lifecycle assertions still run. Output
reports completed scene names/count independently of the aggregate test count.

The initial dark-Crystal QAT selection differed by 6,659 significant pixels while
the unfiltered 113-scene matrix passed. Actual/diff images showed text/edge
differences. The full matrix opens native WPF windows in the preceding light
Crystal fixtures; skipping them omitted window/DPI initialization. A minimal
off-screen window before focused dark fixtures preserves that initialization.
The isolated dark QAT scene, all 25 dark Crystal scenes, and a four-scene
Office LTR/RTL plus Crystal light/dark selection then matched the original
approvals. No baseline or comparison tolerance changed.

Release verification passed: four selector regressions; the full visual project
(5 tests, including all 113 scenes); focused Runtime reduction checks (29), Writer
zoom checks (3), and the independent-consumer GroupLauncher scope (1 aggregate).
An intentionally unmatched Runtime filter was rejected; inherited scope/update
variables were restored after both success and failure. Script parsing, scope
inventory, changed documentation targets and diff checks passed. The complete
solution suite, package validation and live UI/native monitor checks were not
rerun for this test-tooling change.

### 3.239 Test suite redundancy review — 2026-10-09

The first pruning pass reduces current discovered cases from 1,107 to 1,094:
Runtime 619 → 607, Writer 482 → 481, Visual 5, Portability 1. These are fresh
Release discovery counts, not earlier acceptance checkpoints. Visual still
contains all 113 scenes; Portability remains an aggregate with many scenarios.
Neither discovered counts nor this review measure percentage code coverage.

Five redundant cases are removed; eight cases are consolidated into retained
fixtures while preserving their assertions. The retained coverage is:

| Removed or consolidated cases | Retained check | Case reduction |
| --- | --- | ---: |
| Phase-zero Ribbon inheritance and construction smoke tests | Realized Ribbon layout/control tests and the independent consumer require the WPF control contract and instantiate Ribbon on STA. | 2 |
| Writer MainWindow base-type smoke test | The RibbonWindow XAML root/build and MainWindowIntegrationTests exercise the actual Writer window. The manifest/DPI test remains. | 1 |
| Separate TextBox metadata/default test | Native_editor_contract_and_rich_screen_tip_remain_available checks the native base type, style key, InputWidth default, text state and screen tip on one control. | 1 |
| Separate option-control metadata test | Option_controls_keep_their_style_keys_and_create_rich_screen_tips retains both native base types, style keys and screen tips on the same two controls. | 1 |
| Standard size-definition parse test | SizeFor_maps_each_group_state_to_the_declared_size also asserts the parsed array; aliases, invalid input and short-definition clamping remain separate. | 1 |
| ApplicationButton pressed-gradient row repeated in two theories | File_button_body_has_no_uniform_bright_bottom_foot already checks the same key, brush type, stop count and offsets. | 1 |
| Legacy button-ID test with combo discovery | Gallery_discovery_preserves_existing_button_and_combo_automatic_ids restores the same legacy button-ID format with both combo and gallery discovery, and also checks the combo ID. | 1 |
| Separate flyout content-offset and opacity theories | The_surface_is_never_transformed checks surface identity, seeded opacity and negative content offset for both actions at every animation level, using the existing fixture. Action overrides and restoration remain separate. | 4 |
| Separate Crystal customization-tree window test | Crystal_customize_pages_keep_tree_styles_rounded_frames_and_scrolling retains style/binding checks for root and expanded child rows alongside all frame and scrolling assertions in one dialog. | 1 |

This removes one Showcase/customization-dialog launch and repeated STA/control
setups. It does not substitute loops for unrelated test cases or sample away
theme/DPI/input/persistence matrices. Further large reductions were not justified
by proven overlap in this pass. Tests that failed validation remain in the suite.

Release solution build passed with zero warnings/errors. The affected Runtime
selection passed 62 checks. The unfiltered solution run executed all 1,094
discovered tests: 1,091 passed and 3 failed. Runtime was 606/607, Writer 480/481,
Visual 5/5 including all 113 scenes, and Portability 0/1.

The failed Crystal RTL focus-motion row passed in an isolated retry of all four
rows. Writer's paper-scroll offset test and retained manifest check passed in a
separate two-test retry. These focused retries do not turn the original full run
into a clean pass; concurrent WPF project processes remain a possible source of
input/layout interference. No activation assertions or timing tolerances changed.

The consumer reproduced the previously recorded Touch gallery-selection failure
(item top 224 DIP against a 56-DIP viewport, §3.236). Its aggregate stops there,
so later consumer scenarios did not run. That failure remains unresolved; the
test was not removed or weakened. Diff checks passed. No package gate or new
live/native monitor acceptance was performed for this test-only pruning.
