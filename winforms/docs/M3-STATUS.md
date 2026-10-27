# M3 Quick Access and projections

Status: **M3E checkbox/radio Quick Access icon presentation accepted**, 2026-10-27.
Continuation is authorized; the preceding minimize/expand review is in the
[M2 ledger](M2-STATUS.md#m2g-minimizeexpand--2026-10-27).
This is a bounded foundation, not completion of every M3 projection. Accessibility
expansion, comprehensive theming, animation, native touch/IME and custom chrome
retain the user's existing deferrals. No WPF runtime/build/CI changes.

## M3A public surface and behavior

The public additions are tracked in `PublicAPI.Unshipped.txt` with XML comments:

- `RibbonControl.QuickAccessItems`: content-serialized owned declarations.
- `QuickAccessVisible` and `QuickAccessPosition`, with change events and binding
  metadata; `RibbonQuickAccessPosition.TabRow` / `BelowRibbon`.
- `QuickAccessMaxWidth`: positive logical tab-row cap, minimum 28, default 240.
  The below-ribbon row uses the full width rather than this cap.
- `RibbonQuickAccessReference.Source`: borrowed button/toggle source, with
  `SourceChanged`. Inherited identity/visibility/enabled/KeyTip belong to the
  presentation; nonempty inherited Text overrides its caption. Source ScreenTip
  metadata remains shared. No second state or Click path is introduced.
- `RibbonQuickAccessCollection`: directly owns toolbar-only buttons/toggles or
  reference declarations. Dropdown/split/check/radio/editor/group declarations
  are rejected in this first slice.
- Renderer hooks `DrawQuickAccessToolbar`, `DrawQuickAccessItem` and
  `DrawQuickAccessOverflow`, shared by Office and neutral rendering.

Each reference is a separate presentation identity. Several references may point
at the same source; activation calls that source's `PerformClick` once, retaining
its toggle event order and existing WinForms binding. No source model is cloned,
reparented or disposed by its reference. Sources on inactive tabs remain usable.
Source/ancestor visibility and enabled state are respected. Detached/disposed
sources leave disabled declarations at their original position; reinserting the
same live source in this ribbon restores availability. Foreign attached sources
are rejected. Ordinary collection ownership and identity validation still apply.

TabRow precedes the tabs and preserves room for tab navigation and the existing
minimize button. BelowRibbon adds a row downward and stays available when the
body is minimized. Visibility retains declarations, order, placement and cap.
Overflow uses the existing owned popup stack, measured description rows and
scrolling. Mouse, Enter/Space, arrows/Home/End/Tab and Escape use shared library
activation/focus handling. Escape returns to the overflow opener. Numeric entry
KeyTip preferences and 0 for overflow participate in the existing prefix-free
resolver; larger Q-prefixed suffixes have equal lengths to avoid Q1/Q10 ambiguity.
The existing resolver can substitute preferences and reserves 9 on large levels.

Source metadata supplies ScreenTips at each reference's own rectangle; source
and reference anchors are never unioned together. Disabled sources retain passive
tips. Source/model changes, relocation, removal and normal popup teardown retain
the existing cancellation rules.

Showcase consumes public APIs: references to Copy/Bold/Italic/Find/Picture/Cut and
an application-owned M command that switches TabRow/BelowRibbon. Narrowing the
Form demonstrates overflow. No sample-only layout or popup helper is required.
The designer associates owned declarations, excluding borrowed sources; its QAT
collection editor offers buttons, toggles and references. Actual VS round-trip
acceptance remains open.

## Future title-bar hosting

The user explicitly requested retaining this path on 2026-10-27.
`RibbonQuickAccessLayout` accepts a device-pixel rectangle and fill-width policy;
it does not choose a window position. Row painting and command references likewise
do not depend on TabRow/BelowRibbon. A later title-bar surface can reuse the same
models and measured targets without changing their ownership. The owning ribbon
continues to own commands and reusable interaction behavior.

TitleBar is deliberately absent from the placement enum until M6 supplies its
optional RibbonForm/window integration. Ordinary Form support remains required.
No new host contract, custom frame, nonclient painting, caption hit testing or
second simultaneous presentation host is implemented or claimed here.

## Verification and remaining gates

- Normal Debug and Release solution builds: clean, including API/XML analyzers
  and the separate designer project.
- Release full unit/STA checkpoint: **79/79**, including seven new QAT cases.
  `tests/RibbonKit.WinForms.Tests/TestResults/m3a-release.trx`.
- After the bounded large-overflow KeyTip suffix refinement: **8/8 focused** QAT
  and existing KeyTip cases. `m3a-keytips-final.trx` in the same results directory.
  The full checkpoint is not represented as a second final full-suite run.
- Final Release package restored into a fresh external directory and empty cache;
  package-only consumer compiled without project references, parent props or
  Showcase helpers. Both Office and neutral QAT native scopes passed at **120 DPI**.
  Source click/state/binding sharing, inactive sources, disabled/removal/reinsert/
  disposal, toolbar-only commands, visibility/placement/minimize, scrolling to
  the last of 70 overflow commands, multi-character KeyTips, Escape/restore,
  stale overflow closure and passive source ScreenTips were exercised.
- Final package captures inspected for tab-row, full-width/below and minimized
  placement, reference ScreenTips, overflow/last-row/KeyTips and narrow geometry.
  Evidence: `TestResults/visual/m3a-final-package-{qat,qat-flat}/result.log` and PNGs;
  `TestResults/m3a-final-consumer-path.txt` identifies the external consumer.
- Debug Showcase captures at 1080/720/420/260 pixels were generated; wide and
  narrow QAT/tab/body geometry was inspected. `TestResults/visual/m3a-showcase/`.
- Before the final KeyTip-only refinement, package Office ScreenTip and minimize
  regressions passed (`TestResults/visual/m3a-package-{screentips,minimize}/`).
  The former observed zero unavailable-element first-chance exceptions in its
  ordinary menu/input/teardown scenario. These are retained bounded regressions,
  not new QAT accessibility acceptance.

Input fixtures require a shown Form and serialized runs. An initial concurrent
package launch invalidated neutral foreground focus; it was rerun serially.
Hidden startup suppresses the paint callbacks used by the fixture, so the native
input runner intentionally shows its test Form and waits before the next run.

Human visual review, actual VS save/reopen/undo and physical mixed-DPI/monitor
review remain open. The row-origin assertion checks placement independence at
several supplied device-pixel sizes; it is not a physical DPI claim. New QAT UIA
providers/patterns are deferred; foundation accessibility does not establish QAT
accessibility compatibility. No native touch/IME gates were added.

## M3B ComboBox chooser references and pointer repair — 2026-10-27

Accepted for standard use on 2026-10-27, following the user's confirmation that
ComboBox Quick Access and the choice-row pointer repair work perfectly in a
standard use case.

The user requested ComboBox-as-dropdown support during continuation. This slice
was brought forward ahead of ordinary dropdown/split references. The existing
button reference API remains unchanged. New `RibbonQuickAccessComboBoxReference`
borrows a `RibbonComboBoxItem` through `Source`, with `SourceChanged` and read-only
`IsDropDownOpen`; all six new public entries have XML/API baseline coverage.
The QAT designer collection editor offers the new declaration without associating
or owning its borrowed source.

A chooser reads the source choices, live selection, bindings and validation. It
works on inactive tabs, in either placement, while minimized, and from QAT overflow.
The strip gives it a 40-logical-pixel icon/initial-plus-arrow slot; mixed 28/40
slots share measured overflow. The source native editor stays where it belongs;
the existing editor presenter and choice-list implementation are reused, including
when that source has not been shown. There is one editor per source. Editable
free text remains editable at the source and is restored when a QAT preview is
canceled. QAT supplies choice search/selection, not another text field.

Each reference has independent open state. Root choices anchor below their entry;
overflow choices open beside their row, flip within the work area and retain the
parent popup. Escape closes choices before overflow and restores the QAT opener.
Accepted changes use the source's validation/binding/ValueCommitted path once.
External values and choice mutations close stale preview without rolling back an
authoritative change. Source/reference removal, replacement, relocation, disposal
and replacement during validation are guarded. Disabled-reference icons use their
presentation state even when the source remains enabled.

The user then supplied a recording and two screenshots showing mouse hover
changing source selection and multiple stale gray rows. The recording and supplied
images were inspected. Pointer hover now updates a separate row state; a matching
press/release selects and accepts, while a drag to another row cancels. Mouse leave
invalidates the previous hot row. Keyboard preview/search clear pointer hover and
retain their existing validation/rollback behavior. Owner drawing reads
`DrawItemEventArgs.State` rather than querying `SelectedIndex` during the native
selection transition; querying at that point could repaint the old row selected
and leave its fill behind. Renderers receive native selected/preview state and
pointer Hot/Pressed separately, using Checked versus Hover/Pressed fills.
The repair applies to ordinary and QAT choice lists.

Opening from a narrow QAT button also exposed clipped labels. Choice widths now
use a DPI-aware drawing context and include the frame plus twice the independently
rounded six-logical-pixel text inset. At 120 DPI, rounding twelve once differs
from rounding six twice. The native fixture asserts actual text fit as well as
inspecting the resulting captures.

Verification:

- Clean normal Debug/Release builds, including separate design support and API/XML
  checks. The first Release full checkpoint passed **82/82** before the final
  validation-callback, disabled-icon and pointer refinements.
- The callback/icon refinement passed **13/13** affected tests. After pointer
  repair, the same selection passed **12/13**; the existing focus-sensitive
  deferred-binding case did not receive its expected key route and passed its
  isolated retry. These are separate results, not a clean final full-suite claim.
  Evidence: `tests/RibbonKit.WinForms.Tests/TestResults/m3b-{release,final-focused,hover-focused}.trx`
  plus the isolated retry output for
  `Deferred_binding_waits_for_validation_and_escape_restores_the_committed_choice`.
- Final Release package restored into a fresh external directory and empty cache;
  its public-only consumer passed Office and neutral **QAT ComboBox and ordinary
  ComboBox** native scopes at **120 DPI**. The QAT scope checks successive hover
  rows, no source/binding/commit changes, one hot row, mouse-leave cleanup, press/
  drag cancellation, old selection repaint, keyboard/pointer validation, source
  editing/identity, both placements/minimize, overflow and lifetime mutations.
- Before the pointer repair, package Office/neutral ComboBox/QAT ComboBox and
  Office button-QAT/minimize/ScreenTip regressions passed. ScreenTip teardown
  observed zero unavailable-element exceptions. These retained results do not
  replace the final affected native scopes or establish new accessibility support.
- Final captures inspected for choice width, hover/leave/keyboard state, ordinary
  and overflow anchors, and neutral/Office rendering. Evidence:
  `TestResults/visual/m3b-hover-package-{qat-combo,qat-combo-flat,combo,combo-flat}/`;
  `TestResults/m3b-hover-consumer-path.txt` identifies the final external consumer.
  The earlier package is identified separately by `TestResults/m3b-consumer-path.txt`.

Showcase now declares Font/Zoom QAT references using only public APIs. Broader
visual coverage, actual VS round-trip, accessibility and physical mixed-DPI/monitor
gates remain open; no new native touch/IME scope was added.

## M3C dropdown/split references — 2026-10-27

The source/anchor contract was proposed before implementation. New
`RibbonQuickAccessDropDownReference.Source` borrows a `RibbonDropDownItem`,
including its split-button subtype, without widening the existing button or
ComboBox reference APIs. `SourceChanged`, read-only `IsDropDownOpen`, and
`DropDownKeyTip` complete the declaration. The new renderer hook
`DrawQuickAccessSplitItem` receives both measured halves and their independent
states. All nine public entries have XML/API coverage; the designer collection
editor offers the reference without owning its source.

Dropdowns open beneath their QAT entry or beside their overflow row, including
inactive source tabs and minimized/below-ribbon placement. Split references keep
horizontal primary/menu targets even for a Large vertical source. The primary
uses the original source's action and check-before-click order once; the menu
half leaves that state alone. Default split KeyTips append A/B to the generated
entry key; authored primary/menu keys participate in the normal resolver and
update without requiring another layout. Checked primary, pressed/open arrow,
hover and focus remain separate in both renderers.

Menu items, handlers, bindings and native fields retain their original source
owner. Only one presentation of a source menu opens at once, reusing the same
native editor instance/handle across references and its ordinary menu. Each
reference reports its own open state; source opening/closed events still run,
and `source.CloseDropDown()` closes the current presentation. Popup sessions
retain the captured source for the closing event even if a reference changes.
Nested KeyTips, submenu placement, Escape restoration, and overflow dismissal
reuse the existing popup stack. Reference/source changes, disable, removal and
disposal close stale presentations.

Source opening callbacks may populate or edit descendants before measurement.
Those bounded edits preserve an existing parent overflow; mutations of the
source or opener retain normal dismissal and post-callback revalidation. This
also supports ordinary nested menus populated during opening. Showcase now
declares Select, Paste and Track changes references through public APIs; narrow
tab-row placement puts them in overflow, while BelowRibbon exposes the full row.

Verification:

- Normal Debug/Release solution builds passed with zero warnings/errors,
  including separate design support and public API/XML checks. Four essential
  unit/STA cases bring the suite to 86. The full Release checkpoint passed
  **86/86** before the final opening-callback refinement. After that refinement,
  affected menu tests passed **7/7** in Debug; source/collection, lifecycle,
  button/ComboBox QAT and split regressions passed **36/36** in Release. These
  are separate results, not a clean final full-suite claim. Evidence:
  `tests/RibbonKit.WinForms.Tests/TestResults/m3c-{release,opening-focused,final-regressions}.trx`.
- Release package restored into a fresh external directory and empty cache;
  its public-only consumer passed Office and neutral **QAT menus/splits** at
  **120 DPI**. It exercised primary/arrow mouse and KeyTip actions, inactive
  sources, source events/cancellation, nested and callback-populated overflow,
  Escape/focus restoration, source-menu reuse, native TextBox/ComboBox input and
  source/reference lifetime changes. Both runs observed zero unavailable-element
  first-chance exceptions. No Showcase resources or helpers were referenced.
- The same package passed Office button-QAT, QAT ComboBox (including pointer
  hover), ordinary rich-menu, split, minimize and ScreenTip native regressions.
  ScreenTip teardown also observed zero unavailable-element exceptions. The
  first button-QAT run lost foreground focus at startup when the user used the
  keyboard; its isolated retry passed. The interrupted run and retry are retained
  separately, with no runtime focus change made in response.
- Final package captures inspected for inactive-source anchoring, separate split
  state, overflow submenus and the neutral nested stack. Evidence:
  `TestResults/visual/m3c-package-{qat-menus,qat-menus-flat,qat-isolated,qat-combo,menus,split,minimize,screentips}/`;
  the interrupted run is `m3c-package-qat/`.
  `TestResults/m3c-consumer-path.txt` identifies the external consumer.
- Normal Debug Showcase refreshed. Captures at 1080/720/420/260 pixels were
  generated, with wide and narrow geometry inspected:
  `TestResults/visual/m3c-showcase/`.

Actual VS save/reopen/undo, new QAT accessibility and
physical mixed-DPI/monitor acceptance remain open. Native input at the current
120 DPI desktop and synthetic geometry tests do not establish those gates.
No native touch/IME scope was added.

### Toggle-overflow follow-up — 2026-10-27

The user accepted the preceding dropdown/split slice, then requested that Bold
and Track changes toggles retain overflow. They also requested only relevant
tests for now.

QAT overflow now stays open after a toggle or `CheckOnClick` split primary,
retaining the active row's focus and source check-before-click behavior. Ordinary
buttons and nonchecking split primaries still dismiss it; split arrows retain
their menu action. Removal/hiding by a command handler still closes stale
presentations. KeyTip activation now finds the actual overflow opener, matching
pointer/Space/Enter activation and anchoring projected menus to their row.
No public API was added.

Verification was limited to the affected behavior:

- Normal Debug solution build passed with zero warnings/errors. The two new
  overflow state/lifetime regressions passed **2/2**; the existing split-source
  primary/menu event test passed **1/1**. Suite inventory is now 88, without a
  new full-suite claim. Evidence:
  `tests/RibbonKit.WinForms.Tests/TestResults/m3c-toggle-{overflow-final,split-source}.trx`.
- Only Office button-QAT and QAT menu/split native input scopes ran, at **120 DPI**.
  Both passed isolated runs, exercising repeated pointer toggles, source binding,
  KeyTips, Space/Enter, retained split primary/menu targeting, ordinary command
  dismissal and Escape. The menu scope observed zero unavailable-element
  exceptions. Evidence:
  `TestResults/visual/m3c-toggle-{qat-isolated,qat-menu-isolated}/result.log`.
- Initial native attempts are retained separately in
  `TestResults/visual/m3c-toggle-{qat,qat-menu}/`: the QAT attempt lost foreground
  before input; the menu attempt failed its root split-arrow click assertion.
  The latter capture was inspected and the unchanged isolated retry passed.
  These are separate attempts, not a single uninterrupted pass.

Release, package, neutral-renderer and unrelated native scopes were not rerun
for this bounded follow-up. Live acceptance of the new dismissal behavior remains
open; preceding package evidence applies to the preceding implementation.

### QAT overflow scroller buttons — 2026-10-27

The user requested WPF-style overflow scrollers, then explicitly requested opaque
buttons with hover and click feedback. WPF's shared overflow-scroller source and
history were read as behavior references; no WPF implementation was changed.

Long QAT overflow now reserves separate 16-logical-pixel top/bottom bands and
clips command painting, hit targets and KeyTips to the intervening viewport.
There are no native horizontal or vertical scrollbars. Short menus reserve no
bands. Caption widths clamp to the work area. The existing popup model, source
references, toggle retention, split halves and dismissal policy remain shared.
Ordinary dropdown/group popups retain their existing scrolling in this slice.

Clicks move by actual row starts; hover and held presses repeat after 350 ms at
90-ms intervals. Wheel and keyboard/focus navigation share the offset, with Home/
End preserving padding and reaching disabled scroller endpoints. Scrolling pauses
for a child menu or native chooser. Leaving, focus loss, hide, resize and disposal
stop repeat; the popup owns and disposes its timer.

The proposed renderer hook `DrawOverflowScrollButton(context, upward, enabled)`
was added with XML/API coverage. Both renderers fill every band pixel opaquely,
including idle/disabled states, before themed hover or pressed chrome. Disabled
arrows use the disabled foreground and cannot scroll. No new model, theme or
placement API was added.

Relevant verification only, by the user's continuing request:

- Normal Debug build passed with zero warnings/errors, including API/XML checks.
  The initial focused selection passed **5/6**, exposing last-row padding at the
  lower endpoint; that correction passed its **1/1** isolated rerun. Final opaque
  idle/disabled/hover/pressed rendering passed **1/1**, including distinct fills
  in both renderers. Inventory is now 92; no new full-suite result is claimed.
  Evidence: `tests/RibbonKit.WinForms.Tests/TestResults/m3c-{overflow-scrollers-focused,overflow-endpoint,scrollers-states}.trx`.
- Final Office and neutral shown public-consumer QAT input scopes both passed at
  **120 DPI**: scrollbar absence, click/hover/held repeat, leave cancellation,
  wheel, Home/End and endpoint state, last-command reachability, KeyTips, toggle
  retention and dismissal. Actual hover and pressed contexts were observed while
  the native pointer hovered/held. Evidence and inspected captures:
  `TestResults/visual/m3c-scrollers-final-qat-checks{,-flat}/`.
- Earlier input checkpoints preceded the explicit opaque fill. During the opacity
  check, the neutral hover fixture's fixed 600-ms wait expired before sufficient
  timer progress. It now waits for the required row progress, bounded to 1.5 s;
  both final scopes passed. Older captures/logs are retained separately under
  `m3c-scrollers-{qat-checks,qat-checks-flat,opaque-qat-checks,opaque-qat-checks-flat}`.

Release, package, unrelated popup and full-suite scopes were not rerun. New QAT
accessibility, actual VS and mixed-monitor acceptance remain deferred/open as
recorded above. The user accepted the scroller follow-up on 2026-10-27, including
the requested opaque bands and hover/pressed feedback; no broader hardware or
designer acceptance was implied.

## M3D group references — 2026-10-27

The group source/collapse/editor contract was proposed before implementation.
New `RibbonQuickAccessGroupReference` borrows a `RibbonGroup` through `Source`,
with `SourceChanged` and read-only `IsDropDownOpen`. Its six public entries have
XML/API coverage. The QAT designer collection editor offers the declaration,
without associating or owning its borrowed source. Groups remain QAT-only for
the later customization catalog, not available commands in Customize Ribbon.

Attached membership derives a collapsed source-group layout, including
`CanResize=false`, hidden/disabled references and hidden QAT. Expanded widths and
reduction ordering exclude retained groups. Authored policies remain unchanged;
removing the last reference restores adaptive sizing, including natural collapse
in a narrow ribbon. This state is not a new persisted group setting. Source label,
availability, compact caption-initial/arrow rendering and open state use the
shared QAT renderer/layout. No group icon/ScreenTip metadata was introduced.

Group references open beneath their strip entry or beside their overflow row,
without selecting an inactive source tab. BelowRibbon/minimized placement also
works. The popup captures its original group and measures its normal flyout
content. Only one live presentation of that group opens, whether from a reference
or its source collapsed opener. Native TextBox/ComboBox instances and handles are
reused across these attached presentations; models, handlers and bindings stay
with the source. Detaching the source follows the established editor lifetime:
presenters are disposed and recreated upon reinsertion, with retained model
values/bindings. References never dispose their borrowed groups.

Toggles, checkboxes, radios and `CheckOnClick` split primaries retain the group
flyout while QAT membership is retained. Ordinary commands dismiss the stack.
KeyTips enter the source command level, nested menus retain existing behavior,
and Escape restores group/overflow openers in order. Disable, source replacement,
collection removal and disposal close stale group sessions. Existing source
menus and group editors are reused without cloned command trees or a new host
coordinator. Showcase adds Input fields using only the public group-reference API.

Relevant verification only, by the user's continuing request:

- Normal Debug build passed with zero warnings/errors, including design support,
  API and XML checks. The group/menu-reference selection passed **8/8**. Additional
  radio-retention and explicit detach/reinsert presenter assertions in the group
  cases passed their separate **1/1** reruns. Inventory is 96; no new full-suite
  result is claimed. Evidence:
  `tests/RibbonKit.WinForms.Tests/TestResults/m3d-{groups-focused,group-options,group-lifetime}.trx`.
- The shown public-only consumer passed Office and neutral group scopes at
  **120 DPI**, without Showcase helpers. It covers inactive groups, shared native
  TextBox/ComboBox handles and input, bindings, ComboBox QAT from a retained group,
  repeated toggles/checks/split primaries, source opener switching, KeyTips,
  nested overflow/Escape, BelowRibbon/minimize, membership sizing restoration,
  source detach/reinsert/disposal and zero unavailable-element exceptions.
  Evidence: `TestResults/visual/m3d-groups-{source-click,neutral}/result.log`.
- Office inactive-group and nested-overflow captures and the neutral minimized
  capture were inspected. Debug Showcase was refreshed for local review.
- Earlier native attempts remain separate: `m3d-groups-office` lost foreground
  before input; `m3d-groups-office-isolated` exposed a fixture assumption that a
  detached editor would survive; that assertion was corrected to the existing
  lifetime contract. `m3d-groups-final-qat-group-checks` missed a source-opener
  toggle click. Its capture was inspected; the traced retry in `source-click`
  received one down/up and passed without a runtime input workaround. The neutral
  scope also passed. These are separate runs, not an uninterrupted initial pass.

Release, package and unrelated/full-suite scopes were not rerun. Actual VS,
physical mixed-DPI/monitor and new QAT accessibility gates remain open/deferred.
The user accepted the group slice on 2026-10-27 and attributed the missed input
to their computer use. No native touch/IME scope was added.

## M3E option references — 2026-10-27

The user authorized checkbox/radio QAT support with icon toggle presentation.
This deliberately extends the current WPF standalone-source catalog; matching
WPF work is deferred in the [future improvements plan](../../docs/22-FUTURE-WPF-IMPROVEMENTS.md).
No WPF runtime/build/CI changes were made.

Existing `RibbonQuickAccessReference.Source` now also accepts `RibbonCheckBoxItem`
and `RibbonRadioButtonItem`. No new declaration type or public paint-context flag
is required. Directly owned checkbox/radio QAT entries remain rejected. References
borrow the source's existing `Icon`/`Image`/`LargeImage` and show the same button
chrome as toggles. Missing icons retain the caption-initial fallback; overflow
shows the caption/description. A small bar beneath the action icon distinguishes
an indeterminate checkbox, including in tall description rows. Body and group
flyout options retain their normal check/radio indicator presentation.

Activation calls the original source once. Checkbox exact state, state cycle,
binding and event order remain source-owned. Radios retain their original named
or unnamed scope; repeated activation of a selected radio raises Click without
clearing selection. Several references share updates, with independent enabled
state and caption overrides. Options retain overflow and keyboard focus for
repeated mouse/Space/Enter activation; ordinary commands still dismiss it.
Inactive tabs, source availability and borrowed lifetime follow existing QAT
rules. Showcase demonstrates Ruler/Gridlines and Print/Web layout references
with application-owned vector icons through public APIs.

Relevant verification only:

- Normal Debug solution build passed with zero warnings/errors, including design,
  API and XML checks. Focused option/reference coverage passed **7/7**; after the
  mixed-state bar's final placement refinement, only its affected rendering case
  was rerun and passed **1/1**. It covers both renderers at synthetic 96/120/144/192
  DPI, pixel parity with checked toggle buttons, disabled icons, mixed state,
  image/fallback rendering and absence of check/radio indicators in QAT.
  Evidence: `tests/RibbonKit.WinForms.Tests/TestResults/m3e-{icon-options-focused,mixed-mark}.trx`.
  Inventory is 99; no new full-suite result is claimed.
- The public-only shown consumer passed Office and neutral scopes at **120 DPI**:
  checkbox binding/cycle, duplicate refs, programmatic updates, original body
  activation, radio exclusivity/repeated selection, KeyTips, repeated overflow
  pointer/Space/Enter, reference/source disable, ordinary dismissal,
  BelowRibbon/minimize, detach/reinsert/disposal, and zero unavailable-element
  exceptions. Final evidence:
  `TestResults/visual/m3e-options-final-{office,neutral}/result.log`.
- Office strip/body/overflow captures and neutral minimized presentation were
  inspected. The final mixed-state bar is attached to its icon rather than the
  bottom of a tall description row. Earlier passing captures remain under
  `m3e-options-{office,neutral}`. Debug Showcase is refreshed for local review.

The user accepted the option icon presentation on 2026-10-27 and authorized
continuation. Release/package, unrelated/full-suite,
actual VS, physical mixed-DPI/monitor and new QAT accessibility scopes were not
run. No native touch/IME scope was added. The reusable fixture is linked into the
native runner and available to the independent package consumer; this slice ran
the Debug project runner, not a newly packed consumer.

## Next bounded slice

Continue with the [M4A contextual-tab slice](M4-STATUS.md). Do not infer another standalone editor
projection from the existence of an editor control; any extension needs its own
use case and contract. Gallery projection depends on M4. After the current review,
select later M4 component proposals from the native roadmap; comprehensive
theming remains deferred until most components exist. Title-bar hosting stays
with M6, using the existing placement-independent declarations/layout/rendering.
Customization/storage stays with M5. Optional ICommand remains a separate proposal.
