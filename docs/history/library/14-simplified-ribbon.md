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
