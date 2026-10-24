# Library history: Simplified ribbon

> Archived implementation details. Current acceptance and remaining work are in
> the [active plan](../../15-SIMPLIFIED-RIBBON-PLAN.md).

[History directory](../01-library.md) · [Design index](../../../04-DESIGN-NOTES.md).

### 3.255 Simplified ribbon first layout slice — 2026-10-13

Additive `RibbonLayoutMode` and inherited `Ribbon.LayoutMode` select Classic or
Simplified. Classic remains the default. `Ribbon.SimplifiedPlacement` selects
inline commands, and `Ribbon.SimplifiedPriority` retains higher-priority commands
on resize. Original command/group order is preserved; equal priorities remove
commands from the end. Density stays independent.

The shared groups-host template retains a hidden, measured source presenter and
shows a sibling command toolbar. The hidden groups panel realizes source controls
without reporting its widths to the body scroller. Command copies use the existing
source factory and source-owned state, actions and menus. More Commands exposes
displaced copies and original native full-group flyouts, including arbitrary
editors and dialog launchers. The active host retains group-flyout references and
releases them on mode changes, tab disconnection, group removal and retemplating;
ordinary QAT membership still controls its own collapsed constraint.

The simplified toolbar reuses overflow focus suppression, popup dismissal, cached
source copies and nested-menu return. Overflow cache pruning uses the toolbar's
own items rather than assuming QAT membership. Group dividers remain visible only
between inline groups; they never become overflow commands. KeyTips use the exposed
row copies and More Commands, then the existing overflow/group levels. Down/Up on
More Commands opens the menu and transfers focus.

Named simplified resources stay with the groups-host dictionary that references
them. Cross-dictionary static resource lookup from a library-only implicit style
failed for the original placement. Shared popup focus resources are merged there;
the overflow visibility uses a template trigger rather than an external converter.
New compact/touch row and opener metrics are supplied by every palette, including
Crystal's overlay. Native layout rounding means ActualHeight can differ from the
42/52-DIP declared row height by a physical pixel.

Classic customization uses binding-driven owner-mode eligibility so detached,
unselected options pages also update. The context-menu request is guarded by mode.
Showcase owns its View selector and separate appearance preference. Dedicated
simplified customization, ordered state and persistence remain the next slice;
they are not implemented by this layout slice. No generic projection-provider API
was introduced. Automated evidence and unrun native gates are in the active plan.

#### Small commands, gallery dropdowns and overflow borders

`Ribbon.SimplifiedSize` adds Small/Medium inline presentation without mutating
classic command sizing. The host updates cached command copies when it changes;
overflow copies continue using Medium with their source labels. Showcase's Home
row uses Small Bold/Italic/Underline and an inline Styles gallery dropdown. Both
gallery types reuse the QAT projection, preserving native tiles and selection.

The overflow toggle now applies the existing Control Hover/Checked/Pressed border
brush and highlight-thickness tokens with its background states. Office 2007/2010
therefore retain their gradient fills and matching outlines. Other themes resolve
their own existing border metrics; no palette literals or new tokens are needed.

A plain gallery returning from an inline copy could reopen empty through an
overflow copy while its source group was hidden. Its native presenter was already
the source ScrollViewer's logical Content, but had not returned to the measured
visual tree. `RibbonGallery.FindPresenter` now checks that viewport content as well
as visual descendants. InRibbonGallery's named presenter follows the same existing
borrowing path. Focused row-to-overflow regressions verify both types, source
selection and return to Classic; independent-consumer evidence is in the plan.

#### Simplified body spacing

The simplified toolbar now stretches to its existing 42/52-DIP row height rather
than centering at the commands' natural height. Inline buttons and the More
Commands toggle therefore use the available height for their hit and state
surfaces. The toolbar's extra four-DIP side margin is removed, and the shared body
scroller uses `RibbonKit.Metrics.SimplifiedGroupsRowMargin` (4,1) only in Simplified
mode. Every palette supplies that token; classic layout and native QAT retain
their existing styles and metrics. Body-card margins remain shared with the
lower-QAT/message continuation surfaces.

#### Native combo editors in original slots — 2026-10-21

`RibbonGroupsHost` selects a native `RibbonComboBoxSimplifiedProxy` only when a
combo source is its original declaration in a non-custom tab/group. The internal
factory flag is opt-in at that call site; QAT, custom copies and overflow keep
their menu projections. Original-slot fields keep their configured input widths
and native presentation at either Small/Medium command size. Existing group and
declaration identities still drive customization and priority; no public API or
persistence format was added.

The inline editor owns independent ComboBoxItem containers and borrows no source
visuals. Reuse choice-presentation bindings and resolved source group paths from
the menu projection, including source styles, grouping order and leaf occurrences.
TextSearch labels let native editing/selection display the source's data labels.
Explicit Text/SelectedIndex binding commits are guarded during rebuilds and
reentrancy. A plain TwoWay refresh can publish an empty value while clearing rows;
publishing selection-driven text before its index can pick the first duplicate
label instead of the selected occurrence. Keep both operations guarded.

Weak collection/group/style listeners follow loaded lifetime. Closing an inline
picker on overflow prevents a popup anchored to its zero-width slot; transfer
focused input to More Commands after layout. Classic restoration remembers WPF's
tab-control fallback when hiding the old presenter, but never adopts an unrelated
user focus choice as that fallback. A native editable ComboBox.Focus() can return
false while redirecting focus successfully to its TextBox; assert focus within
the editor instead. Window.Hide() does not establish Unloaded; detach/reconnect
content when testing listener teardown and reconstruction.

Showcase's existing inline Font/Font Size declarations demonstrate the shared
behavior without consumer template patches. The standalone XAML render fixture
seeds host font data because its code-behind is intentionally not run. Focused
evidence and native review limits are in the
[active plan](../../15-SIMPLIFIED-RIBBON-PLAN.md#native-combo-editors-in-original-slots--2026-10-21).

Release acceptance on 2026-10-23 exposed a stale designer-defaults consumer
assertion: original-slot combos no longer have the menu projection's MediumLayout
and SmallLayout parts. Check the native combo type, authored InputWidth and visible
field at either size; retain those template-part assertions for the other command
families. The unchanged check failed both after the preceding matrices and alone;
the corrected 12-case scope passed. No runtime behavior changed for this repair.
The [release campaign](../../20-V1.1-ACCEPTANCE-AND-RELEASE-PLAN.md#a2-release-execution)
owns the current aggregate result and its limits.

#### Measured inline clearance — 2026-10-21

After the user reset the blanket-margin experiment, measured painted bounds
showed two-DIP Compact button gaps versus three-DIP clearance on either side of
the group divider stroke. Touch already had four DIP for both. Keep the existing
five-DIP Small separator slot: its one-DIP stroke already has two DIP beside it.
Do not use the full inter-group gap as a per-command margin or widen that slot.

`RibbonQuickAccessPanel` adds `RibbonSimplifiedRow.Spacing` only between kept
neighboring commands, excluding dividers and row edges. Measure/overflow and
arrange use the same span calculation. Shared Simplified toolbar styles supply
one DIP in Compact and zero in Touch through dynamic palette tokens; native QAT
uses its existing layout branch. All six base palettes supply the new metrics.

Only original native combo copies and generated dividers receive the row-metrics
marker. Shared combo chrome grows to 28 DIP in Compact, with one-DIP side margins
to compensate for the field's flush chrome. Touch's existing 44-DIP field and
2,1 margins win. Separator control height grows to 36 DIP in Compact, producing
a 28-DIP line after its existing vertical inset; Touch's 44-DIP minimum remains.
Ordinary buttons/gallery padding and margins, Classic/full-group controls, QAT
and overflow presentation retain their prior metrics. No public API was added.

The focused consumer checks painted bounds rather than only control margins,
including nested dropdown/split chrome, both directions and simulated DPI.
Diagnostic strips must capture the toolbar's parent allocation with an explicit
viewbox so its vertical margin and effects do not clip or scale Touch fields.
Current evidence and rounding limits are in the
[active plan](../../15-SIMPLIFIED-RIBBON-PLAN.md#measured-inline-spacing-and-field-height--2026-10-21).

#### Square Small inline actions — 2026-10-21

Small action and toggle copies stretched vertically with the simplified row but
kept their icon's narrower natural width. `RibbonQuickAccessPanel` now reserves
their square width during overflow measurement and uses the same rule during
arrange. Subtract vertical margins from the row height, then add horizontal
margins to the resulting slot width; Touch's unequal margins must not produce
rectangular chrome. Explicit widths and minimum/maximum constraints retain
precedence. Labeled controls, dropdown/split arrows, classic groups, native QAT
and labeled overflow entries keep their existing sizing paths.

The focused consumer checks the actual control and Chrome dimensions, priority
overflow, explicit width and classic/QAT restoration. Its overflow boundary
allows a few physical pixels for independently rounded margins at fractional
DPI; before/after native text sizes allow one pixel. Current verification and
live review are in the [active plan](../../15-SIMPLIFIED-RIBBON-PLAN.md#small-inline-button-shape--2026-10-21).

#### Menu-sized overflow and side popups

Both QAT and simplified overflow copies acquire an internal, non-inherited menu
entry marker. Shared button/toggle/dropdown/split styles give them 32-DIP minimum
height (44 in Touch) and 200-DIP minimum width. The same theme tokens size native
menu rows, native combo choices and combo projection choices. Dropdown copies use
an icon/caption/trailing-arrow grid; split copies retain separate primary and
submenu actions, with the arrow at the trailing edge. RTL mirrors presentation.
The marker bindings use their registered dependency-property identity and Self
in outer template triggers; a TemplatedParent binding there reads the wrong owner.

`RibbonOverflowMenu` supplies custom placement only for overflow copies. It reads
the nearest monitor's work area in physical pixels and checks the full measured
popup frame, including its outer margin/shadow allocation. Prefer the right side
in LTR, the left in RTL, then the other fitting side. If neither fits, use the
overlaid placement below the row, clamped vertically to the work area. The native
group flyout restores its original placement/callback when its QAT session ends.
Ordinary strip openers keep their existing placement.

[WPF's Popup position implementation](https://github.com/dotnet/wpf/blob/main/src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows/Controls/Primitives/Popup.cs)
passes transformed child bounds to the placement callback. Those bounds omit the
child's outer margin: using only that width caused a left-opening popup to overlap
its parent by the margin allocation. Actual popup-frame checks at both screen
edges reproduced and verified the correction. Wide-popup checks cover fallback.

The direction key enters a submenu; the opposite key returns from menu buttons
to the parent row. Embedded editors/gallery navigation keep their native keys.
Borrowed menu-item returns briefly change inherited density in Touch. Their
geometry invalidation now skips rebuilding the simplified command collection,
which otherwise closed the parent overflow after returning a submenu's items.
Ownership, deferred item return, selection and split action behavior stay native.

#### Visible submenu joins and button clearance

User screenshots exposed a remaining gap when opening left and inconsistent
group-flyout alignment. Clearing the full popup frame cleared its transparent
shadow allocation too. The parent overflow surface is now supplied to each entry
on every opening, including cached entries after retemplating. The placement rule
joins the child's visible border to the parent's visible border, allowing only
the transparent allocations to overlap; full-frame extents still govern screen
fit and vertical clamping. Combo, dropdown, split, gallery and native group
popups use the same rule on either side.

Native group popups wrap `PART_PopupHost` in an AdornerDecorator. That wrapper
includes the inner border's margin, so measuring `Popup.Child` as painted chrome
left a group-specific gap even when wrapper-coordinate assertions passed. The
dropdown/group integration now supplies the actual `PART_MenuHost` or
`PART_PopupHost` to placement. Geometry checks and renders inspect that border,
including group loading on both screen edges and combo choices opening left.

Anchor the vertical coordinate to the entry's arranged slot, excluding the
parent menu's render-only opening slide. Returning coordinates relative to WPF's
current animated target origin cancels that motion. This avoids freezing a native
group flyout at an intermediate position when its borrowed content resizes it.
Captured visual brushes use an explicit arranged viewbox so effect-expanded brush
bounds cannot fabricate a gap in the evidence image.

The user clarified that the requested vertical spacing concerned the ribbon
buttons. `SimplifiedCommandsMargin` adds three DIP above/below the command strip
inside its existing row; the outer `SimplifiedGroupsRowMargin` remains 4,1. Compact
buttons use the remaining 36-DIP height and Touch targets remain at least 44 DIP.
Every palette provides the new metric. The active plan records current evidence.

#### S2 draft customization and persistence — 2026-10-13

`RibbonSimplifiedCustomizePage` is a shared lookless fill page. Available commands
are grouped by their original group; the selected list edits per-tab order and
Small/Medium label choices. Add/Remove, reorder, current/all-tab reset and Apply
work on a draft. Loaded/unloaded/retemplated pages preserve unfinished edits.
The shared options dialog commits before its Applied event and discards drafts on
all other close paths. Apply followed by Cancel preserves the earlier commit.
The classic and QAT pages retain their existing live-edit contract.

Ribbon-owned override lists keep command sources/size separate from authored
placement, classic tab/group items and QAT membership. Ordering can cross groups;
the simplified host creates a separator per contiguous section, and adaptive
measurement selects the separator preceding each retained section rather than
reusing one separator for every occurrence of a group. Native group access stays
in original group order. Reset removes an override, exposing the current authored
defaults again, instead of overwriting attached-property values or bindings.

The context-menu/API pin actions resolve original sources, deduplicate explicit
IDs and prefer the active tab when a command is also exposed through custom-tab
copies. Those declarations retain their own authored placement, visibility,
enabled state and renamed captions. Row and overflow captions follow the local
declaration while invocation/persistence retain original source identity. Group
flyouts are not command candidates; merged/contextual/modal sources cannot be
customized or saved. Transient merge contributions retain their authored row policy.

The serializer adds an optional `Simplified` section with version 1 and explicit
tab/source IDs. Empty command arrays represent an intentionally empty row.
Missing IDs are ignored and duplicates use the first eligible source. Absent
legacy sections preserve current choices; malformed section shapes are rejected
before changing any layout. Custom-tab choices resolve after classic proxies are
rebuilt, not against stale tab references. Layout mode stays host-owned.

Both page types observe owner mode through registered bindings even when not
selected. Options navigation follows their eligibility and selects the enabled
counterpart after bindings settle. The native tab selector holds descriptors
with `Tab.Header` bindings: using actual RibbonTab UIElements as combo entries
produced a blank selection surface and risked sharing their visual content.
Shared row styles provide readable automation names; resource-backed labels
update when the localization provider changes.

Focused runtime selections cover draft/reset/cancel, mode switches, explicit
host disable, template refresh, reordered dividers, copy/duplicate identities,
custom-tab rebuilding/labels/enabled state, optional JSON compatibility and live
localization. A public-API-only consumer passed 112 palette/dark/density/direction/
simulated-DPI combinations with constrained dialog geometry. Final normal Release
solution and Debug Showcase builds passed without warnings. Full testing remains
deferred by the user; native input/DPI acceptance and earlier S1 suite limits are
recorded in the active plan.

#### Seamless editor presentation and utility focus — 2026-10-13

User review replaced disabled duplicate navigation/context entries with one
Customize Ribbon identity per active mode. Options visibility binds to owner
mode, separately from IsEnabled, preserving explicit host enablement semantics.
The simplified tab combo fills the selected-list column and has no side label;
source labels use constrained grids for trimming at narrow widths.

QAT overflow's template had Focusable=false. Its opener is now a native focus
stop, and both toolbar templates put the items presenter in a local navigation
group before the opener. This retains docking order for measurement while fixing
Tab/Shift+Tab order. Arrow entry applies to QAT and simplified overflow. An
optional internal PopupDismissHelper callback returns focus after Escape only,
avoiding the window focus fallback while preserving outside presses. Lower QAT
uses the existing full-width non-overflow host.

Utility rims inherited Office's zero active opacity. Separate pressed-opacity
and checked-brush tokens let Flat outline pressed/open utilities without enabling
hover rims. Defaults preserve other Office palettes and Crystal's existing glass
checked drawing; the final pressed trigger retains press precedence over checked.
Tokens exist in every palette, and overlays do not affect size or hit testing.

Focused focus/utility regressions passed ten cases with both density/direction
choices, native Flat/Office/Crystal states and Escape return. Editor consumer
coverage passed 112 combinations, including selector resizing; the independent
Flat consumer passed eight state combinations. Six QAT/utility/Flat snapshots
passed without approval/tolerance changes. The first selected Crystal QAT fixture
needed InitializeWindowRendering before construction to match its existing text
and rounding geometry; actual/diff inspection identified the startup drift.
Final Release solution and Debug Showcase builds passed. Current acceptance,
assembled runtime selections, full-run limits and the proposed structure editor
belong to the active plan.

#### Pixel scrolling and overflow focus handoff — 2026-10-13

The grouped simplified command list inherited logical content scrolling. Native
group containers could become the scroll units, giving group jumps rather than
small offsets. Both simplified lists now set CanContentScroll=false through the
existing frame's template binding. Native line/thumb scrolling and selected-item
visibility remain WPF-owned; classic/QAT customization templates are unchanged.

Overflow focus had been queued only by the toolbar's Up/Down handler. The popup's
Opened lifecycle now queues the same handoff for click, Enter/Space and KeyTips,
after proxy realization. A pending Up request selects the last eligible row;
other paths select the first. Focus falls into native template children for
non-focusable dropdown/group containers. Request generations and popup identity
guard deferred work across close/reopen/template replacement. Escape's existing
return callback and outside-press behavior remain intact.

Native focus tests need an active owner window and a focused opener to model a
user opening a menu. An inactive independent fixture retained no keyboard focus;
activating its offscreen window established that prerequisite. Focused runtime
scroll/navigation and public-resource-only editor/overflow consumer checks passed;
the active plan records exact counts and the earlier combined-run failure.
Normal Release runtime and Debug Showcase builds passed. Physical input, user
review and final full testing remain separate gates.

#### Shared custom-tab/group draft editor — 2026-10-13

S3 stages detached tabs/groups, name overrides and source references in the
simplified page. It reuses the classic ID/Stacked-group helpers and name dialog.
Apply attaches only the edited structures and creates native source-linked
membership copies; Cancel drops references without mutating the live ribbon.
Current sources are resolved again at Apply so host replacements with stable
IDs are honored and missing/transient sources cannot become stale membership.
The classic tree refreshes on re-enablement after a mode switch.

Discovery spans eligible tabs, deduplicates original commands and retains their
tab/group path; groups stay out of the available-command list. All Groups shows
the complete row, while an explicit group filters selections and targets Add.
Built-in membership remains app-owned; custom groups accept cross-tab commands.
Remove/reset change the simplified profile, preserving shared structure/membership.

Selections now carry group identity. Source/group pairs deduplicate within a
group, and the row cache keys by declaration so two groups can host separate
copies of one source. Internal group metadata follows overflow copies and
context pin/remove actions. Version 2 persists GroupId with SourceId/Size;
version 1 and untagged built-in groups retain first-source-group fallback.
Unknown groups skip their selections; malformed optional shapes prevent any Apply.
Absent/unknown optional sections remap current overrides by stable tab/group IDs
when Classic-only data recreates custom structures.

Selector descriptors bind host Header properties through private dependency
properties and bind All Groups to the localization source, preserving live
updates without using UIElements as combo items. The footer wraps as one action
surface to preserve Touch viewport height. Native invocation tests drain UIA
dispatch; render captures settle layout after modal rename. Focused behavior,
independent-consumer geometry/interaction and render checks passed; current
counts, assembled-run limits and full/native acceptance stay in the active plan.

#### Source filtering, ordered headers and draft utilities — 2026-10-13

S4 replaces the standard Group selector and per-row checkboxes with selectable
contiguous group headers and an Edit menu label toggle. Headers can recur after
cross-group reordering; empty groups remain Add targets. Two independent tab
selectors keep source discovery bounded without changing the destination draft.
New/Edit menus and a separate utility row reduce footer crowding. The shared
template inherits owner density and uses existing Touch target tokens.

WPF drag/drop changes only simplified order, preserving source/group identity.
The insertion marker uses the actual scroll-content viewport, including RTL
scrollbar placement. Edge scrolling stops on leave/drop/template replacement;
generation checks reject stale drags. Native menu opening transfers focus on
every open and Escape returns to the opener. Checked/Unchecked events update
label drafts for mouse/keyboard and UIA Toggle; Click alone missed UIA changes.

Serializer parsing/capture/writing helpers support detached imported descriptors
and source references. Export overlays the current draft without mutating the
ribbon; Apply materializes it once. Missing/transient membership is normalized
before export/commit. Duplicate tab IDs and same-tab group IDs reject malformed
files; separate tabs may share a group ID. Legacy optional profiles remap across
recreated groups, and selection restoration uses stable tab/group/source IDs.

The optional ResetLayout property shares Classic's host-authored startup baseline
contract. QAT-only reset preserves live group objects and session-only profiles;
whole reset/import use the existing complete serializer contract. Later live
changes supersede older pending operations of the same scope. The current
acceptance, exact focused evidence and full/native limits remain in the active plan.

#### Authored catalog, shared group editing and dialog centering — 2026-10-13

The follow-up review separates the source catalog from destination drafts. Only
authored eligible tabs/groups supply ribbon commands; a separate localized choice
lists retained QAT-only declarations. Canonical sources deduplicate copies. QAT
declarations participate in Apply-time membership resolution and persistence even
after removal from the toolbar. Custom tabs/groups stay on the destination side.

Simplified group editing reuses the full shared name/icon/layout dialog. Metadata
is drafted independently of live groups; export/import and Apply use identical
layout normalization, including Large-to-Stacked command sizes and explicit None
icons. Both editors use the shared Stacked creation factory and neutral vector
resource. A per-ribbon weak resource cache gives detached drafts an icon before
joining the visual tree and a stable serializer identity across restart. A plain
DynamicResource on an unattached group did not resolve the generic theme icon.
Normal Add seeds the new source/group selection as Small while retaining existing
profiles. Existing group layouts and label preferences are preserved.

The shared edit dialog now determines its final section height before WPF performs
CenterOwner positioning; changing Height in SourceInitialized shifted the final
dialog upward. Reset/icon Import/icon Export and New/Edit/Apply now occupy the two
column footers, wrapping independently with the existing Touch target styles.
Showcase declares a few prioritized simplified controls in each authored tab,
including contextual/modal and merge content, without changing persistence policy.
Current verification and human review remain in the active plan.

#### Custom-tab positioning draft — 2026-10-13

The simplified Edit menu opens a shared Before/After positioning modal for custom
destination tabs. Creation and movement are chronological relative placements:
collecting every creation before replaying moves would misplace a later tab
created after an already moved tab. The draft fold and Apply replay the same
placements; Apply resolves live references/stable IDs and preserves tab/group
objects and active selection. Missing anchors skip moves. The existing serialized
Tabs sequence carries draft and committed order; no optional schema was added.

Collection Move notifications previously ran removal/addition bookkeeping. During
modal mode this erased a custom tab's authored visibility and recorded its
temporary Collapsed state. Move now preserves modal/merge membership and visibility
subscriptions; genuine removals/additions retain their existing lifecycle. The
focused reproduction, merge/provenance cleanup, themed independent consumer and
remaining acceptance are recorded in the active plan.

Apply refreshes the tab-selector ItemsSource. WPF clears generated combo-item
styles, exposing the system style's ItemsControl-ancestor alignment bindings on
discarded containers. The shared RibbonComboBox cleanup now suppresses only this
detached system-style fallback and restores it before reuse; authored containers
and local values retain ownership. The protected container hooks are recorded in
the additive API baseline. Trace reproduction must call
PresentationTraceSources.Refresh: setting the switch/listener alone silently
missed these errors outside the debugger. A deliberately invalid probe verifies
the recorder before capturing Apply, and discarded alignment properties are
inspected as the debugger's binding diagnostics does.

#### Visible layout/density motion trial — 2026-10-13

The user requested a stronger collapse after seeing source groups spread as
simplified mode released its full-group openers, and extended the trial to replace
density toggling. RibbonKit now commits real geometry, then presents a temporary
vector drawing of the body: only its chrome stretches; command drawings retain
natural glyph/icon geometry. Cached source/group slots provide horizontal movement.
The new additive LayoutModeChange action and existing DensityChange action share
timing/easing; simplified tabs use a slower downward settle with the TabSwitch
override. Current numbers, evidence and acceptance are in the active plan's S5.

Capture final drawings at ContextIdle, after Background simplified-row and gallery
refreshes. Suppress per-group QAT clocks only while this shared layout is being
committed, avoiding several independently started restores. Translate the lower
QAT's clipping/shadow wrapper, not its Border: moving that Border upward is cropped
by the wrapper's final slot. Keep the moving QAT opaque so the host document does
not show through it. Document layout itself still commits immediately.

The adorner is inert; real controls retain focus, commands and automation. Input
and policy/layout lifecycle changes release its opacity clock and drawings.
Already anchored popups/custom templates without an adorner layer use the previous
opacity-only fallback. Guard both disposed and superseded completion callbacks:
an old card's queued completion otherwise cancels a newer simplified-tab settle.
Native minimize owns the body's existing animation after cancellation; regression
assertions must distinguish that clock from the removed vector card. Final review,
focused evidence and unrun native/full gates are maintained once in the active plan.

The user's retry recording exposed the old FLIP timing failure again: the live
destination could render before the ContextIdle transition began. Cover the old
body and QAT synchronously with vector drawings while deferred copies/layout
settle. A newly applied hide clock also needs SeekAlignedToLastTick at zero; its
base opacity otherwise remains visible until the next timing tick. This preserves
bindings and avoids a destination-to-start flash. Replace the cover and start the
final animation in one operation; cancellation releases both covers and clocks.

Density selection runs while its menu is still open. Capturing no previous layout
on that fact alone silently selected the opacity fallback, even though the menu
closed before the transition. Capture the prior geometry first and check persistent
popup anchoring after menu dismissal/borrowed-item return. Independent-consumer
regressions cover both native and simplified selector paths, and force a render
boundary before Background/ContextIdle work. The latest cadence matches group
expansion; exact timings and current evidence stay in S5 of the active plan.

A mode toggle inside a collapsed group inherits the ordinary toggle exemption,
so changing LayoutMode must dismiss ribbon-owned popups independently of the
group's click classification. Queue cleanup at Background priority after routed
click/command dispatch; returning the group's grid during that dispatch would
repeat the earlier reparenting bug. Snapshot registered ownership before closing
nested flyouts from inside out. Follow popup owners across native popup trees
and explicit QAT ownership, including the detached title-bar host, while leaving
unrelated same-window flyouts alone. Use existing close callbacks to return native
content and borrowed menu items; close native combo/context-menu surfaces as well.
The mode path closes File's application menu, while density and ordinary toggle
policies remain unchanged. Focused independent-consumer checks exercise both
mode directions and title-bar QAT nesting across three palettes and LTR/RTL;
current results and remaining native/full gates stay in the active plan.

#### Simplified defaults in designer authoring — 2026-10-14

New supported commands use Inline/Medium through a shared new-item initializer.
Register it for toolbox drops and call the same helper from both ModelFactory
creation paths, covering context commands and nested Ribbon Editor additions.
Write attached enum names through the existing type-qualified ModelProperty
resolver inside the caller's transaction. Check local model assignments before
seeding defaults so copies/repeated initializer calls retain authored values.

The editor's Boolean presentation maps to existing enum properties: simplified
visibility uses Inline/Overflow; label visibility uses Medium/Small. Restrict
these rows to supported commands in groups/panels, excluding menu/gallery entries
and native editors. This adds no runtime API and changes no unconfigured runtime
default. The net472 tooling remains independent of RibbonKit runtime types.
Public-only authored-XAML checks prove runtime layout/label interpretation;
toolbox/Model API edits, undo and reload still require live Visual Studio review.
Current evidence and remaining gates are in the active plan's S6.

The live toolbox follow-up exposed an unsafe new-item guard: ModelProperty.Value
can wrap an effective default even when there is no authored attribute. Use
ModelProperty.IsSet to preserve local assignments and initialize unassigned
placement/size. Log the toolbox initializer's entry separately from per-property
state so the next native drop can distinguish skipped defaults from a callback
that never ran. The package-only toolbox manifest lists allowed controls; it is
not a property-default mechanism and does not affect project-reference Toolbox
population. Current native retry and retained evidence remain in S6.

#### Simplified tab cadence and first-render readiness — 2026-10-14

The shorter body now uses 120/160-ms Subtle/Expressive tab motion, 6/8-DIP travel
and cubic EaseOut, keeping its row opaque and honoring TabSwitch's effective
policy. Classic timing and the accepted shared card collapse remain unchanged.

Building copies only at Background after Loaded permits an empty first frame on
tab changes. Prepare the row in RibbonGroupsHost's first measure, including
before Loaded; cancel the pending refresh and avoid a redundant Loaded rebuild.
Keep unload ownership cleanup and defer when a registered flyout remains open,
so forced layout during a routed command cannot return its borrowed content
mid-dispatch. Guard reentrant layout while group membership is retained. Record
the inherited protected measure override in the additive API baseline.

The public-only regression forces Render-priority layout before Background for
startup, Loaded-restored mode, first visits and revisits. The old implementation
failed on a missing row; current runtime and consumer evidence, exact timing,
visual rereview and unrun full/native gates are maintained in the active plan.

#### MDI, RTL and Writer host integration — 2026-10-14

Applications must compose the shared simplified editor into their own options
dialog and handle SimplifiedCustomizeRequested. RTL Lab and Writer now include
both classic/simplified editors with the factory reset baseline, select by mode
and reuse RibbonOptionsDialog's eligibility switching. QAT remains a separate
entry. MDI adds only command defaults, including its transient document merge
command; no dialog was added. Writer and RTL author useful Inline/size/priority
defaults through the existing attached properties.

Writer's outer dialog rollback previously knew only the appearance Apply/OK
boundary. The simplified page emits SimplifiedCustomizationChanged on its own
Apply, so advance the ribbon-only checkpoint and storage there while the dialog
is open. Otherwise Apply then Cancel would restore the obsolete opening snapshot.
Subscribe temporarily around ShowDialog and remove the handler in finally;
outside Options, use the same ribbon event for pin/remove persistence. Appearance
preview rollback remains independent. This is host integration using existing
public APIs, with no runtime or preference-schema changes. Focused real event,
draft, persistence/restart and MDI target checks are recorded in the active plan.

#### Read-only choice labels — 2026-10-22

Writer's existing Simplified Home integration reproduced a shared combo failure:
`TextSearch.Text` inherited TwoWay mode and attempted to write to the read-only
`WriterFontChoice.DisplayName`. Explicit OneWay bindings on both display-label
branches fix object choices and authored containers; source-owned editable Text
and selection retain their explicit TwoWay synchronization. No API or Writer
Simplified default changed. Runtime regression/adjacent combo and grouping checks
pass 23/23; the independent SimplifiedCombos matrix passes 56 combinations.
See RKWF-050 and the [Writer checkpoint](../../19-WRITER-HOST-INTEGRATION.md).

#### End-of-feature validation repairs — 2026-10-14

First-measure realization exposed a detached custom-group declaration's enabled
state: mirroring only that declaration does not reliably inherit the destination
group's disabling. Simplified row copies now supply that group explicitly to the
existing source/parking MultiBinding. Ordinary QAT copies retain the two-input
contract; no public API changed. A realized independent consumer checks group
disable/reenable and original source disable/reenable across the palette/density/
direction matrix.

Touch derivatives in Customize/Move templates crossed the OptionsDialog BasedOn
file boundary. Keep the three action/primary/reorder derivatives beside their
base styles and consume them from both templates. Brushes, metrics and triggers
remain shared. Update stale list/page counts, checked-rim token and stretched
dropdown-row expectations without weakening their binding/geometry assertions.

The new accessible QAT chevron introduced inner TabIndex values. Without a local
navigation group on the toolbar, WPF sorts its ItemsPresenter ahead of preceding
siblings in a standalone host and Tab skips the strip. Scope the toolbar locally;
forward/reverse order includes inline items and More, excluding zero-slot sources.
Independent checks cover all themes/directions and embedded placement changes.

Retain passing prefixes when the aggregate stops: the bounded keyboard selection
and two new continuation points avoid repeating large matrices. Measured scope
durations now exceed the old full timeout; allow 20 minutes for the full consumer
and 10 for its large continuations. Current results, the two reproduced
pre-feature Crystal QAT approval mismatches and native limits remain solely in
the active plan's end-of-feature record.

### Custom structure removal — 2026-10-24

The S9 review found creation/rename without a corresponding structure-removal path
in the Simplified editor. The middle Remove button now deletes a selected custom
group header; a selected command retains its existing profile-removal behavior.
Edit → Remove tab deletes the destination custom tab. Built-in and transient
structures remain protected. The Classic editor's existing deletion code is unchanged.

Removal records stay detached until Apply/OK, scoped by tab/group identity and
stable IDs. Cancel discards them; draft export/import removes deleted structures
and profiles. Apply preserves remaining live structures, source commands and
unrelated QAT copies. Newly created then removed anchors must not be attached just
to place later creations/moves: use the surviving draft order for those insertions.
Actual shared collections change on Apply, so both ribbon modes and settings
reflect deletion. A loaded Simplified page now refreshes on re-enablement, covering
Classic edits while that page remains in the visual tree; Loaded alone missed
that case. Saved layouts retain the deletion without a schema change.

The only public addition is the appended localization identifier
RibbonString.RemoveTab (90), with XML documentation, a default resource and an
unshipped API entry. It gives providers one complete action label without changing
existing identifiers or adding a control API.

All seven initial structure-removal cases failed. Final focused Release validation
passes 145 runtime cases, including eleven removal/import/order/synchronization
regressions and existing editor, serializer, policy and binding checks. Four
independent-consumer cases cover modern/legacy/Crystal palettes, Compact/Touch,
LTR/RTL and simulated 150% Touch root DPI without Showcase helpers. Release solution
and normal Debug Showcase/Writer builds pass with zero warnings/errors. The broad
test attempt was stopped after reported failures; it is not a clean full-suite
pass. The lower-QAT alignment correction and 16 unchanged visual comparisons belong
to the related QAT record. Current manual review, logs and remaining release gates
are in [S9 of the acceptance plan](../../20-V1.1-ACCEPTANCE-AND-RELEASE-PLAN.md#s9-custom-structure-removal--2026-10-24).

Menu captures render the ContextMenu itself; capturing its native owner root at
open can produce an empty image during the native fade, even with valid geometry.
