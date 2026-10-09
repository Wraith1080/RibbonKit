# Library history: Touch density and gallery projections

> Archived implementation evidence. Dates, test counts and pending work describe
> their original checkpoints; later records may supersede them.

[History directory](../01-library.md) · [Current status](../../../04-DESIGN-NOTES.md#5-current-state--next-steps) · [Working agreement](../../../AGENTS.md).

## 3.226 Shared Compact/Touch density — 2026-10-07

RibbonDensity and inherited Ribbon.Density introduced shared Compact/Touch
layout, independent of theme, DPI, command size and customization. Compact is
the default; hosts own selectors and preference persistence. Detached QAT,
popup, context-menu and Backstage roots follow the owning density, while local
values retain WPF precedence. Shared tokens cover all six base palettes.

The final layout refinements consolidated from the screenshot passes are:

- Three 44-DIP command rows determine the 136-DIP large-command/body extent.
  Large/small icons, horizontal split/dropdown spacing and File padding follow
  touch metrics while navigation font sizes stay normal. Theme-specific File
  insets preserve flat and rounded generation geometry.
- Separators stretch with adjacent content using a 44-DIP minimum rather than a
  96-DIP minimum that enlarged compact nested rows. Group launchers keep their
  normal caption height with a 44-DIP touch width and mirrored spacer.
- Native gallery strips derive one row of equal cells from tile/viewport geometry;
  Compact, expanded and explicitly sized panels keep WrapPanel layout. Deferred
  refresh retains the selected row after resize/density/popup return.
- Application menus enlarge hit areas without shrinking labels; Classic2007
  keeps its rail aligned to the content top and puts orb clearance inside the rail.
- Overflow proxies preserve local left alignment while ribbon dropdowns center.
  Message rows have larger targets/icons without replacing commands or text policy.
- A retained touch title-bar QAT reserves a 46-DIP caption band even while Backstage
  hides its presenter. Caption buttons match that height; Compact/QAT relocation
  restores the original extent. Office2010 Aero bevel geometry follows the rendered
  owning header, preserving scoped margins and preventing secondary ribbons from
  extending it into the document.
- Office2010 lower QAT paint is flat at the body ramp's end color. Office2024 upper
  shadow is clipped by a parent after the child's effect renders, preserving outer
  and minimized halos. Clipping the effect-bearing border itself was too early.
- The minimized Office2007 divider belongs at the footer/document edge and is hidden
  when QAT/messages already provide a border. Message stacks expose one native top
  rim in Office palettes; Crystal retains card borders. Rounded exposed message/QAT
  corners follow their existing combined-state metrics and first-row dismissal.

Adaptive/lifecycle pitfalls retained:

- Density invalidates both the adaptive width cache and the group's measurement
  subtree. Clearing the cache alone left valid Large-state commands at old widths.
- An adaptive probe can expand a collapsed group while its flyout is still open.
  Reclaim content before measuring or an empty normal host produces a false width.
  Cached scroller reports survive unchanged child measures; empty/replaced content
  clears the report. The regressions reproduced both failures.
- LayoutUpdated orb reconciliation must skip equal assignments and valid measured
  bounds. Repeated template/focus/direction assignments created a Classic2007 loop.
- Consumer fixtures wait for the closing Backstage adorner to detach before replacing
  its content. Otherwise a later opening could focus a row outside the live surface.
- Clip message row paint inside the native upper rim, not the whole stack. Native
  Border half-stroke geometry matters; the initial circular clip trimmed its curve.
  The rim mirrors the first row's effective opacity without a second clock or slide,
  and releases/rebinds on dismissal, empty state, unload and template replacement.

DensityChange = 15 is additive to the unshipped animation API. It completes final
layout before a render-only settle: Subtle 160 ms, 85% opacity and a frozen cubic
EaseInOut curve; Expressive uses existing timing/travel multipliers. Only an
expanded lower QAT translates, preserving its prior transform/binding. Open QAT
surfaces suppress that glide. Startup snaps until the first rendered frame;
live toggles replace pending/current work, with generation/reentrancy checks.
Unload, template replacement, motion-policy changes, theme/QAT moves and minimize
release the transition; new templates reacquire their hosts.
Native dropdown headings/descriptive rows add DropDownHeader, Description and
LargeIcon through shared controls, including split/QAT copies. Empty defaults
preserve ordinary menus. Showcase owns density commands/icons/highlight/persistence
and keeps the legacy command ID. Selected row tests respect hover/press precedence.
The final header-corner metric is uniform 3/6/8 DIPs for Office2007/2010,
Office2024 and Crystal; Office2013/2019 remain square.
Verification uses an independent RibbonKit-only consumer across themes, light/dark,
LTR/RTL, densities, QAT placements, Backstage designs, overrides and lifecycle states,
plus focused regressions and inspected renders. Static paint fixtures disable only
relevant motion and restore policy; short animations observe actual rendered frames.
Native physical input, open-menu scale changes and mixed-monitor gates stay separate.

The 2026-10-08 full Release checkpoint superseded the earlier 974-test checkpoint:
combined final results passed 1,003 tests, both runtime builds and package/designer/
net8/net9 consumption checks. Writer input interference cleared on an unchanged
serial full retry with input idle. Two Office2024 message approvals changed only
after actual/diff inspection; the other 111 scenes and comparison tolerances stayed
unchanged. This historical pass predates the later regressions and [test audit](12-keytips-layout-and-validation.md#3239-test-suite-redundancy-review--2026-10-09).
Exact current evidence and acceptance belong to the
[active touch plan](../../14-TOUCH-DENSITY-PLAN.md#verification-and-acceptance).

### 3.227 Gallery popup headings and QAT-only command recovery — 2026-10-08

`InRibbonGallery.DropDownHeader` reuses the dropdown heading dependency property
through `AddOwner`. The shared gallery template pins the noninteractive heading
above the popup scroller; null/empty leaves the original collapsed strip and
popup geometry. Paint and corner geometry use the existing heading tokens in
every theme. Split buttons already inherit this property and QAT copies bind it.
Showcase now demonstrates headings on Paste and the Styles/Theme/Accent galleries.
The compatible API addition is three gallery symbols plus the protected
`Ribbon.LogicalChildren` override in Unshipped; the shipped baseline is unchanged.

Both customization pages previously discovered only group commands, so a directly
declared Undo/Redo item disappeared from their available lists when removed from
QAT. RibbonKit now retains supported directly declared command sources for each
ribbon's lifetime and includes them once in the shared command/icon catalog.
Generated proxies and transient merge commands do not become retained definitions.
Re-adding a declared QAT item restores the original object; custom groups use the
existing source-linked proxy factory.

Retaining a reference alone is insufficient: removing the visual item also loses
inherited DataContext and can break routed commands. Parentless declared sources
therefore remain logical children of the ribbon, preserving bindings/resources,
inherited changes and the route to window command bindings. Existing logical
parents and local DataContext overrides retain precedence. This introduces no
public command-registry API or new application-owned presentation helper.

Persistence includes retained sources and their icons in identity resolution.
Applications declare default QAT commands before applying saved customization and
assign stable CommandIds. Removed commands then remain discoverable after restart;
custom-group copies, re-add and repeated Reset resolve the fresh application's
source. Untagged QAT-only commands remain available only in the current instance.
Existing directly declared unsupported elements retain their serializer behavior.

Five focused command regressions cover remove/replace/clear, deduplication,
icon discovery, repeated Reset, fresh-consumer custom-group restore, enabled
state, inherited/local binding precedence and window command execution. An
independent RibbonKit-only consumer checks both actual customization pages and
96 gallery combinations across six themes, light/dark, Compact/Touch, LTR/RTL
and simulated 125%/200% DPI. Rendered heading previews were inspected. Initial
click assertions needed dispatcher delivery of WPF's asynchronous Invoke provider;
no product delay or activation workaround was added. The existing permanent-
scroller DPI test now verifies the scroller under the heading grid rather than
assuming it is the popup border's direct child.

Release solution and normal Debug Showcase builds passed with no warnings/errors.
The full runtime run passed 523 of 524 checks before the affected scroller
assertion was updated; that check then passed in isolation. Writer passed all
482 checks. All 113 visual scenes passed after actual/diff review and refresh
of only `office2024-rtl-qat-customize-100`: its scrollbar thumb reflects the two
additional QAT-only entries. The full independent-consumer aggregate timed out
at its existing 300-second limit during the concurrent solution run, then passed
unchanged in a fresh process by itself in 234 seconds. Combined final evidence
covers 524 runtime checks, 482 Writer checks and both full aggregates (1008 test
cases, with the runtime scroller check's focused retry recorded above). The final
41-check focused runtime run also passed. No test timeout or pixel tolerance
was increased.
Live mouse/keyboard appearance acceptance and native/mixed-monitor DPI remain
separate from these automated checks.

Gallery, combo-box and group QAT representations were assessed as feasible but
not implemented in this slice. Gallery dropdowns need shared selection/live
preview and careful presenter ownership; combo copies need synchronized
selection/text and editable-input semantics; groups need a compact flyout with
reusable contents. Their catalog, overflow, KeyTips, persistence and portability
contracts require a separate bounded implementation.

### 3.228 Grouped Theme gallery with an independent popup width — 2026-10-08

Showcase's Theme picker now groups its six existing tiles into Modern (Crystal),
Office Modern (2024/2019/2013) and Office Legacy (2010/2007). A native
`CollectionViewSource` groups the flat item list by the host's theme-family
converter. Tags, selection handlers and saved theme identity remain unchanged.
`IsSynchronizedWithCurrentItem="False"` keeps view initialization from selecting
Crystal before the rest of the window has initialized.

`InRibbonGallery` supplies a shared `GroupItem` template when consumers have not
provided a `GroupStyle`. Group names use existing heading paint/text/corner
tokens, are noninteractive, and disappear in the collapsed strip. Native WPF
group presenters retain the gallery's leaf items panel, selection and generated
containers. A style-local alias makes the shared group style discoverable through
the control's default theme style, rather than relying on application resources.

The compatible `PopupWidth` dependency property adds three Unshipped symbols;
the shipped baseline is unchanged. Its default `NaN` preserves previous automatic
sizing. An explicit positive finite width sizes the popup card and changes
presenter wrapping only while expanded. Showcase uses 440 DIP, enough for all
three Office Modern tiles in one row in Compact and Touch. Width bindings update
an already open popup; closing restores the strip's own wrapping and reveals the
selected tile. The strip and popup continue to own separate permanent scrollers.

Release solution and normal Debug Showcase builds passed with zero warnings and
errors. All 15 focused runtime checks passed, including Showcase construction,
theme/selection integration, popup-width validation and existing gallery
retention checks. An independent RibbonKit-only grouped-data consumer passed
96 combinations of six themes, light/dark, Compact/Touch, LTR/RTL and simulated
125%/200% DPI. It checks section order, one row per section, bounds, shared paint,
width-binding updates, presenter ownership and selected-tile reveal. The actual
Showcase popup and Office/Crystal consumer previews were inspected.

The existing visual snapshot aggregate also completed successfully; no snapshot
approvals changed in this follow-up. At the user's request the full runtime and
Writer runs were interrupted, and the full independent-consumer aggregate was
deferred. Those are pending gates, distinct from the focused passing checks.
Native interaction and mixed-monitor DPI acceptance remain unrecorded.

An icon dropdown projection was assessed as simpler for selection-only QAT
combo boxes. Editable typing semantics and the remaining catalog/proxy/overflow
integration require a later implementation. Dropdown and split buttons still
offer one `DropDownHeader`; multiple sections currently use composed popup
content. This follow-up does not add a QAT combo projection or a menu-section API.

### 3.229 Combo-box dropdown copies for QAT and customization — 2026-10-08

`RibbonComboBox.Icon` is an optional, compatible dependency property used by
QAT and custom-group dropdown copies. Its three symbols are in Unshipped; the
shipped API baseline is unchanged. The original combo keeps its input layout,
editable text, items, selection and application handlers. Showcase's Font and
Font Size combos supply vector icons and stable command IDs.

The command catalog includes combos as single commands, without descending into
their choices. Both customization pages, the non-editing right-click surface,
`AddToQuickAccess`, saved customization and QAT overflow use an internal shared
dropdown projection. Directly declared QAT combos are retained as sources and
normalized to dropdowns after collection notification, including restoration of
defaults. Combo automatic IDs use a separate counter and prefix so discovery
does not shift existing automatically identified button commands.

Each projection builds its own choice buttons and updates the source's native
SelectedIndex with SetCurrentValue. Source ItemsSource/selection bindings and
SelectionChanged handlers remain intact, including duplicate boxed values and
sources on inactive tabs. Weak collection-change listening refreshes an open
chooser. Data templates, selectors, formats, DisplayMemberPath and authored
container overrides supply presentation; raw visual items use text or accessible
names instead of changing parents. Shared lookless row templates use existing
Office/Crystal paint, focus and Touch tokens. MaxDropDownHeight follows the source.
Editable typing and its native context menu remain on the original input.

Placement reload restores the owning ribbon's FlowDirection binding. Inspection
of actual Touch previews also caught a stale Compact height in the popup's
nonlogical decorators. The projection invalidates the full popup measure branch
when opening or rebuilding rows; a card-clip assertion now covers that failure.
No delayed test waits or preview-only layout repairs are required.

The Release solution and normal Debug Showcase builds passed with zero warnings
and errors. All 54 focused runtime checks passed, including seven new combo
checks and existing overflow, customization, retained-command and Showcase
coverage. A RibbonKit-only consumer passed 288 combinations of six themes,
light/dark, Compact/Touch, LTR/RTL, simulated 125%/200% DPI and all three QAT
placements. It exercises the real customization pages, inactive-tab sources,
shared paint and complete row/card bounds. Nested overflow keyboard checks passed
disabled-item skipping, End scrolling, Enter/Space selection, Escape/focus return,
outer-popup dismissal and removal/re-addition. Office/Crystal Compact/Touch
consumer previews were inspected.

User live review (2026-10-08) accepted the popup mouse highlighting and dropdown
copies added to a new custom tab and group. The full runtime/Writer/visual/consumer
suite remains deferred at the user's request; mixed-monitor DPI was outside this
review.

Mouse-hover follow-up (2026-10-08): the user found that the first enabled row
retained the opening keyboard focus while another choice was hovered, producing
an extra hover-colored row alongside the current selection. Choice rows now
transfer focus on MouseEnter without changing selection. An internal library
extension hook lets the combo projection start at its current enabled choice;
ordinary dropdowns retain first/last focus, and Up still opens at the last row.
Missing or disabled selections fall back to the first enabled choice.

The independent consumer reproduced the stale-focus failure before the fix and
then passed hover/focus paint, unchanged selection, disabled-row handling and
mixed mouse/keyboard activation checks. The expanded focused runtime selection
passed 64 checks, including an Office2007/Crystal Compact/Touch hover regression
and the existing ordinary dropdown borrowing checks. The 288-case combo consumer
matrix and its overflow/keyboard checks passed again. Full-suite and user live
acceptance remain separate from these synthetic-input results.

### 3.230 Gallery dropdown copies for QAT and customization — 2026-10-08

`RibbonGallery` now exposes optional `Header` and `Icon` metadata. Both gallery
types appear as whole commands in the customization catalog, without descending
into tiles. QAT, overflow and custom-group copies use a shared internal dropdown
projection. Showcase gives Styles, Theme and Accent galleries icons, captions
and stable command IDs. Directly declared QAT galleries are normalized to copies
and retained for removal/re-addition and persistence, like combo boxes. Gallery
automatic IDs use a separate counter so previous button/combo IDs stay stable.
Seven compatible Unshipped API symbols were added; the shipped baseline is unchanged.

The source keeps its control, items, native containers, templates, collection-view
groups, selection bindings and application handlers. Only its existing items
presenter is borrowed while a copy is open; every copy keeps a separate popup
viewport. This preserves arbitrary authored visual tiles as well as templated
data, including on an inactive source tab. Opening another copy or the native
gallery popup closes the previous copy before transferring the presenter.
Close, removal, overflow dismissal and template replacement return it; the
source strip reveals the selected tile when it becomes visible again.

The user's first live QAT/overflow review exposed an empty original Styles strip
while its presenter was borrowed. A temporary vector preview now keeps that
visible strip painted until close. It copies the visible drawings, transforms,
clipping, opacity and pixel-snapping guidelines without cloning controls, items,
bindings or handlers. Matching the viewport's flow direction preserves RTL tile
positions and text. Closing returns the live presenter and reveals the chosen
tile through the existing selection path. This fix adds no public API.

Native tile selection events need an explicit relay because the original selector
is outside the copy's visual event route. The relay preserves source selection
and binding behavior. Arrows/Home/End navigate visible tiles and select through
the native container; Enter/Space commits the focused tile and closes. Mouse
close waits for release, including capture on the original selector. Closing
releases gallery-owned capture before returning the presenter and cancels an
active preview. The viewport's automation peer exposes the source's native item
and selection providers alongside native scrolling. Group headings, gallery
scrollbars and tile paint use shared templates/tokens; popup width and heading
bindings remain live. Custom gallery templates require a presenter in a scroller.

Release solution and normal Debug Showcase builds passed with zero warnings and
errors. The initial focused runtime run passed 71 checks covering gallery behavior,
combo/button compatibility, customization persistence and Showcase's real Theme
selection handler. An independent RibbonKit-only consumer passed 288 combinations
of six themes, light/dark, Compact/Touch, LTR/RTL, simulated 125%/200% DPI and all
three QAT placements. It checks the customization pages, inactive sources, group
headings, one row per section, bounds, native UIA selection and presenter return.
Its synthetic keyboard/mouse checks passed disabled-item fallback, arrow picks,
Up/last-item opening, Enter/Space commit, Escape/focus return, nested overflow
dismissal, preview/cancel events and removal. Office/Crystal Compact/Touch popup
images were inspected; no snapshot approvals changed.

After the visible-strip fix, the final focused runtime run passed 75 checks,
including four new Office/Crystal, Compact/Touch and LTR/RTL cases at simulated
125%/200% DPI. They cover both a direct QAT popup and nested overflow, selected
tile preservation, presenter return and reveal after an overflow pick. An
earlier combined run failed one existing native-popup selection-reveal case;
its four cases passed in a fresh process, then all 75 passed together without
changing that test. The transient failure's cause is unconfirmed.

The independent consumer's existing 288 inactive-source combinations completed
again. A separate follow-up run passed 48 visible, data-templated source-strip
combinations across Office/Crystal, Compact/Touch, LTR/RTL, simulated 125%/200%
DPI and all QAT placements, plus visible overflow dismissal and the existing
keyboard/mouse checks. Its initial follow-up fixture needed selection set
before load so the native strip revealed that row before testing QAT. Cropped
window renders preserve the actual RTL coordinate context; Office/Crystal
before/open images were inspected with matching tile positions, text and chrome.
Release solution and normal Debug Showcase builds passed with zero warnings and
errors. No snapshot approvals changed.

The Localization/RTL lab now has a separate Gallery & Combo tab with a native
Font combo, a bilingual Styles gallery and a bound Selection Preview. Both
commands have icons and stable IDs for customization; its QAT still starts with
Save and Undo so the user can add the new commands manually. The lab uses the
existing shared controls and templates without a runtime change. Normal Debug
Showcase and Release builds passed, along with four existing lab markup checks
and the realized bilingual application-menu check. Live review of these new
controls remains for the user.

The 2026-10-09 lab follow-up adds the existing Touch/Mouse Mode menu presentation
to Localization Lab > Mode, with a stable customization ID and active-choice
highlight. Its handler changes only `DemoRibbon.Density`, and the status text
reports the current spacing. The QAT still starts with Save and Undo; Font and
Styles copies remain user-added for Touch/RTL testing. The normal Debug Showcase
build passed without warnings or errors, and five focused Release lab checks
passed (four markup checks and the realized bilingual application-menu check).
Live Touch/RTL combo and gallery popup review remains for the user.

The full runtime/Writer/visual/consumer suite remains deferred at the user's
request. The user confirmed the visible-strip fix works in live QAT review on
2026-10-08; live RTL gallery/combo review and native mixed-monitor DPI are pending.
Group-to-QAT support is a later slice; group entries must be available only in
Customize QAT, excluded from Customize Ribbon.

### 3.231 Native gallery direction changes, popup origin and strip retention — 2026-10-08

Live review in the Localization/RTL lab exposed three native `InRibbonGallery`
failures: the first opening after either direction toggle could show an empty
strip and a heading-only popup, wide cards opened from the wrong edge, and the
strip briefly painted another row while opening. The user reproduced the blank
opening without using any gallery copies. Combo and gallery dropdown copies
were behaving correctly.

The shared native template now targets the gallery itself. Custom placement
aligns the painted card's top-left corner in LTR or top-right in RTL to the
matching gallery corner, accounting for the shadow margin. This intentionally
overlaps the side buttons, as requested; WPF retains screen-edge constraints.
The control prepares the presenter and lets source bindings transfer before
opening at Render priority. Direction changes dismiss an open native popup,
return its live presenter and refresh the strip. Template replacement returns
the presenter before dismantling the old viewports.

Rendered independent-consumer diagnostics reproduced a remaining heading-only
popup after a direction/DPI sequence, although `ViewportHeight` still reported
the previous nonzero value. Its actual viewport height was zero: an intervening
`ScrollContentPresenter` retained an empty measure while the gallery's native
presenter already had valid tiles. Refresh now invalidates measure and arrange
on the full visual path between presenter and viewport. Coverage checks actual
height at the first `Popup.Opened` notification, card/viewport containment and
the rendered surface, rather than trusting cached scrolling metrics.

Opening captures the visible strip before its wrapping flag changes, using the
existing vector preview used by QAT borrowing. The strip keeps that row painted
while its one live presenter is expanded; close returns the presenter and the
previous offset before revealing the selected item. No public API was added.

The focused Release runtime run passed 96 checks, including the real RTL lab,
grouped/ungrouped data templates, repeated direction/DPI changes, wide-card
placement, template rebuilding, strip rendering, QAT/combo compatibility and
customization persistence. The separate RibbonKit-only consumer passed 96
combinations of six themes, light/dark, Compact/Touch, LTR/RTL and simulated
125%/200% DPI. Office/Crystal strip and popup images were inspected; no snapshot
approvals changed. Release solution and normal Debug Showcase builds passed with
zero warnings and errors. Full-suite testing remains deferred; live review of repeated
direction toggles, animated opening/closing and native mixed-monitor DPI remains
for the user.

### 3.232 Native gallery dismissal preserves the viewed row — 2026-10-09

Follow-up live recording showed the original gallery exposing its first row for
one frame on dismissal, then jumping back to Emphasis without a new selection.
The earlier opening checks did not inspect the first uncovered strip frame.
New frame sampling also exposed a Touch panel's measure notification queuing
another viewport reset after the strip had already been returned and restored.

The initial Render-priority return passed 112 focused checks and 96 consumer
combinations, but live retesting still exposed a transient row. The user also
reproduced it through QAT and noted that Escape after closing could select the
first focused tile. Eight new checks reproduce a focused rich-text gallery in a
ribbon, pumping rendered frames across the whole open/close boundary without
first forcing layout. All eight failed with the initial implementation.

The shared template now supplies `PART_StripPreviewHost`, a noninteractive
decorator outside the scrolling surface. Both native and QAT borrowing keep the
same frozen vector row there while the live viewport is transparent. Capture
uses already painted drawings without flushing pending focus/scroll layout.
Native closing waits for collapsed wrapping/headings to transfer, hides the
popup HWND before removing its content, then returns the presenter behind the
picture. The picture remains through WPF's Loaded/Render work; Input priority
restores the viewed offset/focus and uncovers the live strip. A reentrant Touch
geometry refresh is suppressed during that return. No controls, bindings or
application handlers are cloned, and legacy template parts retain their fallback.

Cancellation through either popup preserves the viewed row, even when browsed
away from the selection. Only a different selection runs the existing reveal;
native focus then follows the new pick rather than reselecting the previous tile.
QAT focus returns to its opener. Reopening cancels a pending return, and native/QAT
takeovers retain the frozen row's original offset rather than an empty viewport's
zero. Template replacement/QAT borrowing flush a pending native return before
taking the presenter. No public API or shared animation policy changed.

The final focused Release run passed 120 checks. Twelve Crystal cases sample
every rendered frame for 300 ms after cancellation across LTR/RTL, selected or
browsed rows, motion disabled/enabled and Compact/Touch. Additional cases check
changed selections, rapid reopening and native-to-QAT handoff. Existing native
viewport tests wait for the render boundary before checking presenter return
and popup-page reset, while retaining their scroller identity/DPI assertions. The
eight focused rich-text cases additionally check every open/close frame, Escape
after dismissal, focus after a different pick, and QAT-to-native takeover.

The separate RibbonKit-only consumer passed 96 theme, palette, density, direction
and simulated-DPI combinations. Its Office/Crystal checks capture the first
`Popup.Closed` strip surface and sample animated focused native and QAT borrowing
in eight cases per path. Before/open/first-close and focused-return images were
inspected; no snapshot approvals changed. Release
solution and normal Debug Showcase builds passed with zero warnings and errors.
User live retesting remains pending. The full suite and native mixed-monitor
DPI gate remain deferred.

### 3.233 Sibling popup opening preserves other gallery rows — 2026-10-09

After confirming the native/QAT frozen-strip behavior, the user reported that
opening Accent or an ordinary dropdown could make the separate Theme gallery
briefly show Crystal, then return to its selected Office 2024 row. Eight new
rendered-frame tests reproduced the reset in Compact/Touch and LTR/RTL, with no
reload of the observed gallery. The trace showed `Window.DpiChanged` receiving a
routed notification whose `OriginalSource` was a newly attached `Image`.

WPF propagates DPI notifications from descendants such as popup icons and the
frozen strip picture. Every gallery subscribed to the owning window had treated
these as a window monitor transition, synchronously zeroed its strip and then
queued the selected-row refresh. The owner handler now requires `OriginalSource`
to be the window itself before resetting viewports. Actual owner notifications
retain the existing DPI refresh. The correction is local to `InRibbonGallery`;
no public API, template or shared animation behavior changed.

All eight reproduction cases failed before the correction. The final focused
Release run passed 128 checks, including repeated sibling gallery/dropdown
opening, descendant DPI notifications, continued handling of simulated window
DPI changes, and the existing native/QAT, RTL, focus, layout and customization
coverage. The RibbonKit-only consumer passed its 96 theme/palette/density/
direction/simulated-DPI combinations and 16 additional animated sibling-popup
scenarios across Office 2024/Crystal, Compact/Touch and LTR/RTL. Rendered strip
comparisons were inspected; no snapshot approvals changed.

Release solution and normal Debug Showcase builds passed with zero warnings and
errors. Live retesting of this sibling-popup correction remains pending; the full
suite and native mixed-monitor DPI review remain deferred.
