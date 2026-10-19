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

### 3.240 Modern Office glass surfaces and legacy Aero choices — 2026-10-11

Showcase's Glass look now targets Office 2013/2019/2024 and Crystal. By user
direction, Office 2007/2010 retain their separate Aero path. Their switch is
disabled and unchecked without overwriting the saved glass override; the real
refresh handler also prevents an overlay from surviving a switch into a legacy
theme. Returning to a supported theme restores the saved on/off choice. The
preference schema and native backdrop activation are unchanged.

Office 2013/2019 had an opaque ribbon-wide background behind the translucent
body, so reducing BodyBackground opacity could not reveal the host backdrop.
The shared template now reads `RibbonKit.Brushes.Ribbon.TabStripBackground` on
PART_TabHeaderHost. Every base palette supplies transparent neutral paint and
dark overlays inherit it. The optional glass factory clones the effective ribbon
band using `RibbonKit.Metrics.Ribbon.TabStripGlassOpacity` and clears the
ribbon-wide brush opacity. Body opacity defaults to its established 0.44. Office
2013/2019 use 0.48, matching the title bar's glass translucency in light and dark
modes following user review; other palettes retain 0.88. The metric resolves from
the scope rather than the global theme. Glass-off paint is unchanged. Light palettes whose File
button uses white text on a colored block keep the original File hover brush;
the pale tab hover otherwise washed out Office 2013's label.

Native Office 2019 dark review showed the two bands blending over gray Acrylic:
the #252525 header at 0.48 and #1E1E1E body at 0.44 converge over a mid-gray
backdrop. `RibbonKit.Metrics.Ribbon.BodyGlassOpacity` now defaults to 0.44 in all
base palettes and overrides to 0.64 only in Office 2019 dark. This deepens the
body while retaining the header's title-bar treatment and 36% backdrop contribution.
The independent consumer renders both surfaces over #959595 and requires a
header/body shade difference of at least 16; gray-backdrop fixtures also cover
100% and narrow RTL at 200%. These checks reproduce the paint issue without
establishing native DWM acceptance.

These are shared tokens/templates and the existing scoped overlay API. Host theme
eligibility remains separate from the generic factory; it still works for scoped
palettes independently of the global theme. No public API or Writer source changed.

New glass fixtures cover File hover, the active split half, selected tabs, QAT
and body paint over a fixed light/dark WPF backdrop at 100% and narrow RTL at 200%.
They use synthetic hover state, not native pointer input or DWM Acrylic. Place
these scenes after the established matrix: native-window initialization changes
WPF's disconnected text rendering. An early insertion altered Crystal text/edge
pixels; actual/diff inspection led to restoring scene order, without replacing
an existing approval or relaxing determinism/tolerances.

Verification: both normal Release runtime targets and normal Debug Showcase built
with zero warnings/errors. Focused selector/preferences/Crystal checks passed
35/35. The RibbonKit-only GlassOverlay scope passed one aggregate covering all
12 palettes, realized tab-band/body resource resolution, removal/local precedence
and an Office 2013 palette scoped inside Crystal. The selected visual aggregate
passed 36 scenes: 12 inspected new glass baselines and 24 unchanged Office/Crystal
comparisons. Subsequent tab-band/body adjustments rebuilt both Release runtime
targets and the normal Debug Showcase with zero warnings/errors. The independent
consumer passed matching tab-band/title-bar translucency, rendered gray separation
and scoped Office 2013 light/2019 dark palettes inside global Crystal light. The
latest selected visual run passed 35 scenes: 14 glass (including two gray cases)
and 21 existing comparisons. Affected new glass baselines were refreshed only
after inspecting actual/diff paint: the latest change affects the Office 2019
dark body, with other glass and non-glass approvals retained. The
full solution suite and package gate were not rerun. The active
[glass iteration status](../../09-FUTURE-THEMES-PLAN.md#office-glass-iteration)
owns native appearance acceptance and remaining live gates.

### 3.241 Gallery header contrast and balanced section spacing — 2026-10-11

Office 2007/2010 dark keeps dark primary text on its pale ribbon/gallery body,
but gallery headings used that same text over a #444444/#464646 fill. Shared
group headers and the overall gallery caption now pair the existing
`ApplicationMenu.HeaderBackground` with `ApplicationMenu.Foreground`. Shared
dropdown/split headings use the same pair so gallery QAT captions retain parity.
Tile foreground and palette-wide primary text are unchanged; scoped overrides
still resolve normally. No public API symbols or Showcase glue were added.

The old group heading had a two-DIP lower margin and no matching gap after its
tiles. `RibbonKit.Metrics.GallerySectionPadding` now supplies 0,4,0,4 in all six
base palettes, inherited by dark overlays. The group items presenter applies it
while expanded; the strip collapses both the heading and the gap. A non-grouped
popup with an overall caption applies the same spacing to its viewport. Empty
captions and grouped popups retain the original outer viewport geometry.

Keep the two heading roles distinct: the optional overall caption is fixed above
the viewport; every collection-view section heading scrolls with its tiles.
One native scrollbar serves all sections, with no nested per-section rail.
The existing direct ItemsPresenter-to-ScrollViewer ownership and permanent
strip/popup viewports were retained, preserving the native/QAT handoff contract.
Overflow coverage runs after the original six-item selection/width checks so its
larger dataset does not replace those cases. Consumer tile content centers its
label inside the authored box, making vertical gap previews representative.

Verification: normal Debug Showcase and both Release runtime targets built with
zero warnings/errors; 54 focused runtime tests and 27 unchanged visual scenes
passed. Three independent scopes passed: grouped and caption galleries each
cover 96 theme/light-dark/density/LTR-RTL/simulated 125%-200% combinations;
dropdown/split headings cover 48 combinations plus QAT. Tests check light legacy
heading text, scoped foreground precedence, equal section gaps, one overflow
rail, scrolling section headings and last-tile reachability. Targeted legacy
dark, modern, Crystal, Touch and overflow captures were inspected. Snapshot
approvals/tolerances and the public API baseline were retained. Full-suite and
package checks were not rerun. Current native review belongs to the
[gallery header status](../../09-FUTURE-THEMES-PLAN.md#gallery-header-and-section-layout).

### 3.242 Classic Backstage caption and Chart Tools selection — 2026-10-11

`RibbonWindow` continues Classic Backstage's rail and page through the caption
using realized `NavColumn`/`ContentArea` brushes, width, direction and foreground.
Internal `UtilityChrome` references connect the separate adorner branch to shared
chrome and update after retemplating. Only the custom caption icon is hidden;
closing or switching design restores chrome without changing the taskbar icon.
The shared rounded row edge prevents the half-covered pixel seam at fractional
DPI. No public API symbols or theme literals were added.

Caption paint binds to Backstage's live opacity and render transform, clipped
within the title row. Keep the caption source through the visual exit, then
release it in the adorner-removal callback; clearing at the logical close snaps
the colors early. Glyph foregrounds composite using the same fade progress,
reuse the resource brushes at endpoints and do not freeze mutable app brushes.
No second animation clock or layout animation is needed; RTL, interrupted
reopening and disabled motion follow the body automatically.

Reusing Backstage exposed a stale local flow direction after detachment. Capture
binding ownership before removing the child, then clear its owned value after
removal; checking ownership afterward is too late because removal can freeze
the last value. App-owned replacements survive. RTL evidence must assert the
physical rail side, not just matching caption/rail coordinates. Simulated DPI
changes on a reused visual need layout invalidation before rendering.

The Chart Tools reset was Showcase event ordering: `Unchecked` could still see
the source merged and jump to Chart Design just before removal. Selection polish
now checks the toggle and waits for declarative activation to settle, with a
cancellation guard. Shared merging already preserves surviving tabs, including
Home with injected Refresh, and was retained. Regressions cover Home/View/Ribbon
Lab, a removed selected tool tab and cancelled activation.

Verification: 54 focused Release runtime tests, 11 unchanged selected visual scenes
and one RibbonKit-only caption scope passed. The consumer covers 16 light/dark,
density, direction and simulated DPI combinations, scoped accents, retemplating,
design changes and close/reopen; seam pixels and the physical RTL side are checked.
It also samples Subtle/Expressive LTR/RTL opening, closing and interrupted
reopening, matching opacity, transforms, physical rail position and foreground
pixels, plus instant global/per-action-disabled paths. Resting and transition
captures were inspected. Normal Debug Showcase and both Release runtime
targets built without warnings/errors. Approvals and public API baselines were
retained; full-suite/package gates were not rerun. Current live review belongs
to the [caption status](../../09-FUTURE-THEMES-PLAN.md#classic-backstage-caption-continuation).

### 3.243 Classic caption icon fade and title continuity — 2026-10-11

`RibbonWindow` observes the realized caption image's `Visibility`, so template
triggers, null icons and orb suppression enter the same before/after title
measurement as QAT changes. `Hidden` still reserves width; `Collapsed` does not,
so `IsVisibleChanged` alone would miss that transition. The descriptor subscription
is removed on unload, close and template replacement. Interrupted title glides
clear their old translate clock before installing the new first-frame seed;
otherwise the old animated value survives until the next timing tick and jumps.

Classic Backstage retains a non-interactive copy of a previously visible icon's
paint while collapsing its layout slot. The copy uses the inverse of Backstage's
live opacity. At close start, the real icon slot returns with the QAT and its
image uses that same inverse fade; title geometry settles once with both widths.
Null icons and orb-owned caption suppression are preserved. Removing a template
trigger can discard a `SetCurrentValue` suppression, so the visibility observer
reasserts orb ownership before the image paints. No independent icon clock,
Showcase helper, public API or theme token was added.

Focused follow-up verification: four Release icon/title regressions, two unchanged
Office 2013 visual scenes and the independent caption scope passed. The title
checks preserve physical position on the first frame in LTR/RTL, an interrupted
glide, Hidden-to-Collapsed and coalesced QAT/icon changes; disabled motion and
template replacement are covered. The consumer adds inverse icon-opacity and
physical image-bounds checks to Subtle/Expressive opening/closing/reopening,
plus null-icon/orb restoration cases. Transition captures were inspected. Normal
Debug Showcase and both Release runtime targets built without warnings/errors.
Full-suite/package gates were not rerun; native timing, OS reduced motion and
monitor-DPI review remain in the [caption status](../../09-FUTURE-THEMES-PLAN.md#classic-backstage-caption-continuation).

### 3.244 Colored QAT split companion and legacy borders — 2026-10-11

The user's accent-caption screenshots exposed two shared-template leaks. The
inactive split half kept the neutral `Control.CompanionBackground`; active and
companion borders retained Office 2007/2010 amber even after colored hover/open
backgrounds were applied. Colored QAT split halves and dropdowns now suppress
their outline and companion glow. Their border thickness stays intact, avoiding
an icon/content shift between states; uncolored commands retain the theme paint.

`Qat.ColoredCompanionBackground` is a 30% white wash in Office 2013/2019/2024,
making the underlying accent paler than the active half. Office 2007/2010 and
Crystal supply transparent companions; dark palettes inherit those choices.
The companion trigger precedes active hover/press/open triggers so the active
half wins. Existing checked-state precedence keeps an open arrow's fill stable.
The Ribbon publishes the companion token into QAT item/host resources alongside
the existing hover/pressed brushes, preserving ribbon-local scope across the
detached title-bar host, and removes it when colored-surface context ends.
No public C# API or Showcase styling helper was added.

The new focused `ColoredQat` consumer scope first reproduced the grey companion,
then passed with realized source-backed QAT proxies across all five Office themes
in light/dark, default frames and both legacy Aero frames. It covers both hovered
halves, primary press, open/hover precedence, two accents, ribbon-local companion
overrides, colored Office 2019 tab-row placement and neutral/off restoration.
Modern companion pixels are checked over the accent band; colored borders and
glows are checked directly. LTR/RTL and simulated 125%/200% are included. Captured
caption strips were inspected. Three existing default Office 2024/2010/2007 visual
scenes passed without baseline changes. Both Release runtime targets and normal
Debug Showcase built cleanly. Current native review remains in the
[QAT status](../../09-FUTURE-THEMES-PLAN.md#colored-qat-split-and-dropdown-states).

### 3.245 Office 2024 neutral and backdrop QAT washes — 2026-10-11

Office 2024 light's neutral companion was `#F2F2F2` against the `#F3F3F3` caption.
It now uses `#ECECEC`, remaining quieter than active hover while visibly marking
the other half. Its dark companion already has adequate separation and retains
the authored paint. Other generations keep their neutral palettes.

`SetTitleBarBackdrop` now enables the scoped `QatUseBackdropBrushes` metric in its
existing Office 2024/non-accent branch. Every base palette supplies a false
default, including Crystal, so a nearer palette can suppress the application
policy. Ribbon applies owned merged dictionaries to QAT items and the active
title/tab-row host. Hover and press/checked washes borrow the caption's black
(light) or white (dark) alpha brushes; companion uses half the hover opacity.
This retains the material and normal icon colors through the existing templates
and wash animations. Explicit per-item resources win over the merged palette.
Refresh, item removal, placement changes, theme/accent/backdrop switches and unload
remove only the owned dictionaries, restoring original scoped paint. The opaque
below-ribbon surface keeps its normal brushes. No public C# API was added; hosts
continue to own native Mica/Acrylic activation through the existing integration.

The focused `QatBackdrop` independent consumer reproduced the one-channel neutral
contrast, then passed neutral/active separation, light/dark alpha relationships,
rendered companion composition over two underlay colors, plain/toggle wash
resources, checked paint, local precedence and restoration/ownership checks.
It covers title/tab/below placement, colored-caption priority, non-2024 and nearer
Crystal scopes, removal, LTR/RTL and simulated 125%/200%. Synthetic mouse flags
exercise nested template state; native plain-button pointer/fade timing remains
a live check. Neutral/material QAT captures were inspected. `ColoredQat` and three
existing Office 2024 light/dark and Office 2010 visual scenes also passed with
unchanged approvals. Normal Release runtime targets and Debug Showcase built
cleanly; full-suite/package gates were not rerun. Current acceptance remains in
the [QAT status](../../09-FUTURE-THEMES-PLAN.md#colored-qat-split-and-dropdown-states).

### 3.246 Restored QAT-only commands retain caption interaction — 2026-10-11

Removing and re-adding an authored Save/Undo/Redo-style command preserves its
object, bindings and handlers, but its retained logical Ribbon parent can supply
`WindowChrome.IsHitTestVisibleInChrome=False` instead of the caption visual host's
true value. Windows then treats its title-bar area as non-client chrome. The
shared `RibbonQuickAccessToolBar.OnItemsChanged` now explicitly sets the current
hit-test value on framework-element items, including collection reset/restore.
Logical ownership and the original control remain intact; no API or Showcase
helper was added. Ribbon-group copies continue to work through the same host.

The focused `QatCommandInteraction` independent consumer reproduced the false
caption hit-test value, then passed actual customization-page remove/add,
serializer clear/restore and all three placements in LTR/RTL. It verifies native
`WM_NCHITTEST=HTCLIENT` at realized title-bar targets, window command execution,
toggle/split/dropdown actions and a ribbon-group copy as the working comparison.
All five existing declared-command regressions also passed. Normal Release runtime
targets and Debug Showcase built cleanly. These checks do not claim live pointer
acceptance; current review scope is in the
[customization follow-up](../../09-FUTURE-THEMES-PLAN.md#qat-only-commands-after-customization).

### 3.247 QAT visibility and customization viewport rounding — 2026-10-11

`Ribbon.IsQuickAccessVisible` is a binding-preserving visibility preference,
defaulting to true. The shared QAT menu hides the toolbar; ribbon command, tab
and background menus offer show/hide. Native text-editing and host-supplied
background menus remain in charge of their own surfaces. Adding a new command
reveals QAT. Hiding preserves commands, order and placement while releasing all
visual projections, restoring host caption content and body/message geometry,
closing overflow/borrowed dropdowns and removing active and root QAT KeyTips.
The existing Customize QAT page is unchanged. Localization values are appended
without renumbering existing entries, and cached menu labels refresh on opening.

Customization JSON stores a nullable visibility field; older files preserve the
host's current preference. Showcase observes the property for its existing save
policy. Runtime owns no storage or Showcase-specific presentation helper.

The actual Showcase options dialog reproduced a 0.6-DIP action-button overrun at
125%: its rounded page extended beyond an unrounded ScrollContentPresenter clip.
The shared dialog now rounds its whole viewport. Width remains 860 DIP; widening
would conceal the mismatched rounding rather than correct it.

Both Release runtime targets and normal Debug Showcase built cleanly. Twenty-six
focused runtime tests passed. Independent `QatVisibility` checks cover all three
placements, expanded/minimized states, LTR/RTL, Touch density and Office 2024,
Office 2010 and Crystal; `CustomizationLayout` checks cover Office 2024/Crystal,
LTR/RTL and simulated 125%/200%. Dialog captures were inspected. Four selected
Office 2024, Office 2010 and Crystal visual scenes passed unchanged. Live pointer
and mixed-monitor review remain in the
[current status](../../09-FUTURE-THEMES-PLAN.md#qat-visibility-and-options-layout).

### 3.248 Glass look over Mica and Acrylic — 2026-10-11

The shared glass overlay is backdrop-independent, but Showcase gated its use on
`ActiveBackdrop=Acrylic`. The refresh now accepts Mica too. Both materials share
the existing scoped overlay, replacement/removal lifecycle and saved glass
choice. Office 2013/2019/2024 remain opt-in, Crystal retains its default and
Office 2007/2010 still suspend the modern treatment in favor of their Aero
choices. ScreenTip and API documentation now describe both materials. No public
API, brush, metric or native backdrop activation behavior changed.

Seven new selector cases first reproduced the absent Mica overlay, then passed.
Together with four legacy cases, eleven focused Release cases cover light/dark,
Crystal defaults, on/off, repeated Mica/Acrylic/None refresh, noncompounding
opacity and legacy choice restoration. They inject derived runtime backdrop
state and invoke the real post-activation refresh; they do not activate DWM.
The existing RibbonKit-only `GlassOverlay` consumer scope passed. Normal Debug
Showcase built with zero warnings/errors. Visual baselines and the full suite
were not rerun because authored glass paint is unchanged. Native Mica review is
tracked in the [Office glass status](../../09-FUTURE-THEMES-PLAN.md#office-glass-iteration).

### 3.249 Groups in QAT with membership collapse — 2026-10-11

By user direction, source groups stay collapsed for the entire QAT membership.
Opening/dismissing their QAT dropdown does not change row layout, so there is no
input-release restoration coordinator or fixed delay. Removal releases the
constraint and invalidates adaptive widths; a naturally collapsed group stays
collapsed. The sizing panel excludes retained groups from expanded width probes
and reduction ordering, including groups with `CanResize=false`. Visible
membership changes commit final layout first and reuse the existing render-only
transition under the dropdown motion policy. Motion changes and unload cancel
clocks while retaining consumer transform/opacity bindings; open sibling popup
anchors are excluded from the glide.

An internal dropdown proxy opens the source's existing collapsed `Popup` and
native content grid. Inactive tabs remain unselected. WPF defers opening a popup
under an unloaded visual parent, so the same popup is detached from its source
template panel during that session and reinserted on close; no second content
view or group picture is created. Logical items, namescope, resources, editor
bindings, gallery selection and native controls remain source-owned. Placement
reloads rebind the proxy's flow direction. Tab/group removal, selection changes,
retemplating, removal/reset and unload release active flyout ownership. The
original collapsed opener retargets the shared popup before handling its press,
because an immediate reopen can cancel asynchronous `Popup.Closed`.
The group proxy disables the base dropdown's own popup lifecycle before attaching
template handlers. Otherwise its unused popup can register another dismiss
helper, which cancels native content presses routed through an active source tab.
Active and inactive native content press regressions reproduce and guard that
failure. Toggle/check/radio clicks keep the collapsed flyout open for additional
changes; ordinary commands retain close-after-invocation behavior.
Clearing the unused popup's template binding and assigning `IsOpen=false` still
allowed the two-way expression to reopen it. Its empty border measured about
10 by 10 DIP and appeared as a small circle beside the source flyout. A local
one-way false binding now keeps that popup closed independently of the opener.
Native-popup assertions reproduced this in four active/inactive strip/overflow
cases before the fix; all 32 group runtime cases and the independent 288-case
matrix plus overflow/keyboard/KeyTips passed afterward.
The outer overflow dismiss helper follows a nested source flyout's opener through
the existing popup stack, so native content routed through an active tab remains
inside its parent flyout despite retaining its original logical ownership.

QAT popup helpers mark an inherited paint/preview suppression flag on the ribbon;
QAT hosts and native flyout content explicitly stay interactive. Shared hover
triggers, animated washes, ScreenTips and gallery previews honor the flag without
disabling commands. Mouse/touch outside presses remain available to normal input
routing, and outside dismissal avoids returning focus to the former opener.
Capture-owned outside presses release capture before normal routing. Dropdown
sources synchronously reclaim borrowed items when reopening before the deferred
return, keeping their native menu ready for the first opening layout.

Groups enter only the Customize QAT catalog and group caption context menu.
Existing command IDs keep their order; automatic group IDs use a separate prefix
and per-header ordinal, so reconciliation of unrelated named siblings does not
erase an untagged group's QAT identity.
Restore resolves recreated custom groups to their final objects and deduplicates
QAT references. Group references are rejected from custom-group command lists,
and existing merge provenance/persistence exclusions are retained. KeyTips
descend through the source flyout from strip and overflow, whose group copies
mirror the strip's explicit keys. The proxy's ExpandCollapse peer exposes native
flyout children. No public API or generic provider contract was added.

The complete focused Release runtime rerun passed 138 cases, including 32 group cases
and existing combo/gallery, popup, adaptive, serializer, animation and KeyTip
regressions. The independent `GroupQuickAccess` scope passed 288 combinations of
all six themes, both variants, densities, flow directions, simulated 125%/200%
DPI and all three placements, plus overflow/keyboard, inactive-tab KeyTips and
native UIA checks. Three native flyout captures were inspected. Fifteen selected
QAT visual scenes passed; only the Customize QAT list approval changed after
actual/diff review to include groups.
The existing `GalleryQuickAccess` independent consumer scope also passed.
The preceding runtime run passed 137 of 138 cases, with one split-button KeyTip
failure. The corresponding 106-case runtime slice passed in the unchanged baseline;
the complete rerun passed without code changes.
The additional combo consumer scope did not produce a clean pass: its keyboard/
KeyTip checks were intermittent, and a separate unchanged `a2ebe40` archive also
failed around overflow KeyTips. Existing focused runtime combo/KeyTip regressions
passed; the consumer instability remains a recorded verification limit.

Build and live acceptance status is maintained in the
[group QAT status](../../08-CUSTOM-CONTROL-INTEGRATION-PLAN.md#group-qat-implementation-and-acceptance).
Synthetic input and simulated DPI do not establish live pointer/keyboard,
physical touch, slow-machine or mixed-monitor acceptance. Full validation remains
deferred by request.

### 3.250 Custom-group command additions resolve the original QAT source — 2026-10-11

A custom-group command is already a proxy. Treating it as a new QAT source made
dropdown/split copies borrow an empty intermediate menu, showing only the header;
combo/gallery copies also lost their specialized representation. `AddToQuickAccess`
and `IsInQuickAccess` now resolve the original source through existing proxy links.
This preserves native items, handlers, selection and persistence identity, and
deduplicates additions from built-in controls and their custom-group copies.
No public API was added.

Customize QAT refreshes Add availability on selection and QAT collection changes,
including external context-menu additions and removal. Duplicate invocations are
also rejected by the runtime check. Four dropdown/split/combo/gallery cases failed
before the source fix. The final 114-case focused runtime slice passed, including
ownership, source invocation, restore, declared commands and merge exclusions.
The independent `QatCommandInteraction` scope passed 12 new dropdown/split cases
using the public Customize Ribbon page, routed context-menu opening and native
UIA invocation across all placements and both directions. It checks that Customize
QAT and the context menu disable duplicate additions and re-enable after removal.
One RTL QAT-page scene passed after actual/diff inspection showed only the intended
disabled Add-button change. Current builds and live acceptance are in the
[custom command QAT status](../../08-CUSTOM-CONTROL-INTEGRATION-PLAN.md#custom-group-command-copies-in-qat).

### 3.251 Gallery ScreenTips follow QAT and custom-group copies — 2026-10-11

`RibbonGallery` adds optional `ScreenTipTitle` and `ScreenTipText` dependency
properties, inherited by `InRibbonGallery`, using the existing `ScreenTipHelper`
and shared `RibbonScreenTip` template. Six compatible symbols are recorded in
Unshipped; the shipped API baseline is unchanged. This aligns gallery authoring
with existing ribbon controls and gives copies source-owned description metadata
without cloning tooltips or adding a projection-provider API.

Gallery copies bind the description and resolve the tip title from the source's
explicit title or the copy's own header. Keeping that fallback on the copy preserves
custom renames. The catalog can use the tip title when no gallery header is authored.
Clearing both source properties removes its generated tip; copies retain their
existing header-only fallback. Each control owns its tip instance, and native tile
tooltips are preserved. Showcase supplies descriptions for its three main galleries
and Localization/RTL gallery. Individual gallery items deliberately receive no
KeyTip badges; arrows and selection behavior are unchanged.

The focused 26-case runtime slice covers both gallery types, live metadata changes,
clearing, header fallback, renamed copies, catalog and existing gallery lifecycle.
The independent gallery consumer adds 48 combinations across both gallery types,
all themes, light/dark and LTR/RTL, opening and realizing source/custom/QAT/overflow
tips through shared resources. Two rendered captures were inspected. Current builds,
live hover review and the deferred full gate are maintained in the
[gallery ScreenTip status](../../08-CUSTOM-CONTROL-INTEGRATION-PLAN.md#gallery-screentips).

### 3.252 Internal hover-suppression bindings use property identity — 2026-10-12

The shared utility rim bound to `(controls:RibbonPopupInteraction.SuppressHover)`
by name. Runtime path resolution could not find the internal owner, producing the
reported `PART_ModalClose`/`CrystalUtilityRim` error. The shared application-orb
template had the same path. Both now reference an internal static `PropertyPath`
constructed with the registered dependency property as its parameter. Property
triggers already hold the DP identity and need no change; the helper remains internal
and the public API baseline is unchanged.

The regression evaluates each realized template's exact binding even when hover is
inactive, verifies false/true/false through QAT popup open/close, and retains pressed
feedback. Both cases failed with the original paths and pass with the fix. The final
47-case runtime slice and independent group consumer passed; 12 new consumer cases
cover both paths, all QAT placements, LTR/RTL and native modal-close invocation.
Visual evidence, original-binding fixture instability, normal builds and remaining
live review are maintained in the
[hover binding follow-up](../../08-CUSTOM-CONTROL-INTEGRATION-PLAN.md#hover-binding-follow-up--2026-10-12).

### 3.272 Colored Office 2024 captions preserve the ribbon backdrop — 2026-10-19

`ApplyTitleBarOverride` cleared the ribbon-wide background override whenever a
colored caption was enabled. Office 2024 light defaults to transparent ribbon
paint, but both dark 2024 variants default to opaque #181818. The same caption
toggle therefore covered the backdrop only in dark mode. With Glass look enabled,
the overlay also regenerated its tab-band wash from that opaque fallback.

The Office 2024/Flat backdrop branch now establishes a transparent ribbon-wide
background independently of caption coloring. The non-colored-caption branch
still owns transparent caption paint and material hover/press washes; the colored
branch retains its accent caption and QAT treatment. Disabling the backdrop clears
the override normally. The public backdrop remarks describe this distinction.
Native Mica/Acrylic activation and optional Glass overlay ownership remain with
the host; no Showcase-only correction or new public API was added.

Eight focused Showcase regressions cover both 2024 themes, light/dark, Mica/Acrylic
derived state, Glass look on/off and colored-caption round trips. Before the fix,
the four dark cases failed with fully opaque ribbon paint while the four light
cases passed; all eight passed afterwards. These tests inject post-activation
state and do not activate DWM.

The RibbonKit-only `Office2024Backdrop` scope passed 16 theme/light-dark/glass/
authored-underlay contexts. It compares actual tab-band/body pixels before and
after caption coloring, checks the accent caption, backdrop removal and consumer
resource precedence. Dark 2024/Flat on/off captures under
`TestResults/office2024-backdrop` were inspected. Normal Debug runtime net8/net9
and Showcase builds passed. No full suite, native Mica/Acrylic or mixed-monitor
acceptance was performed; current review belongs to the Office glass status table.

### 3.253 Crystal utility snapshots initialize native rendering — 2026-10-12

Selecting `crystal-light-utility-scrollbars-100` in a fresh process could measure
the disconnected scene before WPF's native window renderer initialized. The first
and second captures then differed in ribbon layout, preventing a trustworthy
comparison to the approved image. Inspection of the two captures and their diff
showed geometry drift during fixture startup.

`CreateCrystalUtilityScene` now calls the existing `InitializeWindowRendering`
helper before constructing/measuring the scene, as other disconnected fixtures
already do. Repeatability failures save the first capture, repeated capture and
their pixel diff before throwing. The exact equality guard and comparison tolerances
remain unchanged. This correction affects the test fixture, with no runtime or
approved-image changes.

All six selected light/dark utility scenes at 100/200% and RTL passed. The original
light 100% scene passed in three fresh test processes, each checking two identical
captures and the existing approval. This is focused visual evidence, not a full-suite
pass. Current status is in the
[hover binding follow-up](../../08-CUSTOM-CONTROL-INTEGRATION-PLAN.md#hover-binding-follow-up--2026-10-12).
