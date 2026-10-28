# M2 editing and richer interaction

Status: **Implementation through M2G minimize/expand**, 2026-10-27.
The user accepted the demonstrated M2C startup/editor polish and authorized M2D.
M2D/M2E/M2F retain their bounded review gates; outstanding designer, display-scale and monitor
gates remain in [the M1 ledger](M1-STATUS.md).
Accessibility compatibility is deferred by explicit user direction: this phase
prioritizes the necessary control structure. No new split accessibility providers,
patterns, screen-reader support or acceptance gates are included. The existing
foundation bridge is retained; new split items are not accessibility-compatible.
That is a current limitation, not a decision against future accessibility work.

## Next component slice

2026-10-27: **M2G implementation and review are recorded below**.
Work continues with [M3A Quick Access](M3-STATUS.md); optional ICommand remains a separate proposal.
The [M2G record below](#m2g-minimizeexpand--2026-10-27) owns current scope and evidence.
The [prior continuation prompt](NEXT-SLICE-PROMPT.md) records the starting
constraints; its proposed-slice wording and earlier checkpoint are historical.

The user deferred comprehensive theming until most component slices exist,
before final visual polishing/API stabilization. The authoritative scope and
sequencing are in the [native roadmap](../../docs/21-NATIVE-WINFORMS-PLAN.md#deferred-theming-consolidation).
Continue structural component work; this does not authorize the theme refactor
or new accessibility work in M2G. Current implementation evidence below does not
establish acceptance of every visual, designer or hardware gate.

## M2A scope and API

Add `RibbonSplitButtonItem : RibbonDropDownItem` with primary `Click` and
`PerformClick()`, inherited owned `Items` and dropdown lifecycle methods/events,
`Layout` (`RibbonSplitButtonLayout.Horizontal` default or `Vertical`) and an
optional `DropDownKeyTip`. Vertical presentation applies only at Large; reduction
to Medium/Small uses horizontal halves. Dropdown-only items retain their behavior.
The existing dropdown base becomes extensible to support this additive item type.

The renderer gets `DrawSplitItem(primaryContext, dropDownContext, item, size, menu)`
so another consumer or custom renderer receives both measured action rectangles
and their independent interaction states. No public layout or popup coordinator.

Mouse release activates only the same half that was pressed. Enter/Space invokes
the focused half; Tab/Shift+Tab traverses both. Down opens the focused split's menu
from the ribbon; Right opens it within a popup. Escape restores its arrow focus.
Primary and arrow KeyTips resolve independently. A future accessibility slice
must expose both actions, their bounds/focus, state changes and action patterns.

Showcase demonstrates Paste with a vertical large split. The independent package
consumer must use the public item/renderer APIs with no sample helpers.
The native TextBox and ComboBox follow-ups are recorded below; check/radio variants,
full menus/ScreenTips, minimize and optional ICommand remain later M2 slices.
M3-M6 scope is unchanged.

## Verification

- Initial M2A checkpoint: eight focused split cases, then the full **81-case native Release
  unit/STA suite**, with zero skipped cases. Geometry covers synthetic
  96/120/144/192 DPI, two fonts, all three sizes, both arrangements and menu rows.
  Input cases distinguish press/release halves, primary/menu activation,
  disabled state, cancel/removal during opening, KeyTips and collection metadata.
- Passed: normal Debug and Release solution/API/XML checks and package build, zero
  warnings/errors. A fresh consumer restored solely from a local feed/cache
  outside the checkout, built and ran the public split validation Form.
- Passed at actual **120 DPI (125%)** in the native host and package consumer:
  primary/arrow actions, Enter/Space, Tab/Shift+Tab, separate KeyTips, disabled
  rejection, medium/small fallback, collapsed-group ownership and three popup
  levels with Escape focus return. Public renderer hooks record action rectangles;
  the validation Form supplies no runtime input/layout workaround.
- Inspected captures of vertical/horizontal Large, Medium, Small, disabled,
  arrow focus and collapsed/nested popups. Default and custom painting use the
  same two action contexts. The ordinary M1 keyboard/focus/window-dismissal and
  existing bridge regression scenarios passed in both consumers; those results
  do not establish split accessibility compatibility.
  Debug Showcase captures were also inspected at 1080/720/420/260 window widths,
  actual 120 DPI, including horizontal reduction and collapsed groups.
- Remaining: actual VS designer round-trip, a split-specific
  binding authoring review and additional physical scale/monitor checks. The sample
  compiles explicit component/collection/event/property initialization; this does
  not establish successful VS editing. Accessibility remains deferred by user
  direction and does not block the current structural slice.

Joined-edge polish, 2026-10-26: the user reported a doubled join and rounded inner
corners in the two vertical hover states. Each half now clips one shared outer
rounded contour, including its fill and border; the join has no half inset or
inner rounding, and one device-pixel separator is painted once. The same rule
applies to horizontal splits and pressed states. Four new pixel regressions failed
before the fix at 96/120/144/192 DPI, then passed in a **16-case focused Debug run**
with split interaction and existing four-corner symmetry checks. The normal Debug
solution build and a fresh isolated Debug package consumer passed. Native host and
package-consumer hover/pressed captures were inspected at actual 120 DPI, including
both orientations, smaller sizes, collapsed groups and menu rows. This is bounded
follow-up evidence; no new full-suite/Release or additional hardware-scale run was
performed. Keyboard focus cues and public API are unchanged.

## Renderer and arrow refinement

2026-10-26: the user requested larger downward arrows, then explicitly authorized
separating the common rendering foundation from Office styling. The arrow was
completed and captured before the renderer refactor.

- Initial indicators were shared 8 x 4 logical-pixel vectors, independent of font glyph
  shape/size. Dropdown labels/icons reserve arrow space; collapsed groups use the
  same painter. Four new arrow cases failed beforehand and passed afterward,
  covering 96/120/144/192 DPI and two fonts. The first focused run passed 29
  rendering/layout/popup cases; native Showcase and popup captures were inspected.
- `RibbonRenderer` supplies flat chrome, common content and overridable body,
  scroll-button, text, image and arrow drawing. `RibbonOffice2024Renderer` supplies
  rounded chrome, selected-tab underline, group separators and Office focus/KeyTip
  styling. Null Renderer keeps the Office default. The existing protected rounded
  helper remains available. The control no longer paints body fills or scroll
  arrows directly. Rendering and interaction geometry stay separate.
- The additive renderer class and hooks are XML-documented in the provisional
  Unshipped API. Preview subclasses wanting the old base appearance should derive
  from `RibbonOffice2024Renderer`. No shipped baseline or accessibility contract
  was changed. This does not start native editor or broader theme implementation.
- Latest Release solution/API/XML/package checks passed with
  zero warnings/errors; **93 native unit/STA tests passed**, zero skipped. Four
  renderer-boundary cases cover default-versus-explicit Office pixels, flat chrome,
  custom body/scroll/popup painting without layout changes, and shared content slots.
- Both flat and Office public-API validation Forms passed native mouse/keyboard,
  KeyTips, disabled/cross-half behavior, nested popup returns and renderer hooks,
  at actual 120 DPI (125%). A fresh isolated package consumer passed both modes.
  Existing foundation input/focus/bridge/dismissal regressions also passed; they
  establish no new accessibility compatibility.
- Office Showcase captures at 1080/720/420/260 widths are pixel-identical to the
  post-arrow/pre-refactor captures. Office and flat consumer captures were inspected,
  including hover joins and popup rows. Other physical scales, VS authoring and
  human acceptance remain open. Accessibility work remains deferred.

Final review gave scroll buttons their own hover/press state rather than comparing
their null model identity with command state. The boundary regression now checks
idle, hover and pressed callback values as well as layout independence. The final
Release build and all 93 unit/STA cases passed, as did both native renderer modes.
That aggregate attempt then stopped at the package Office mode's first keyboard
focus assertion. A fresh process using the unchanged package consumer binary passed
the complete Office mode; the flat mode and the remaining external input, keyboard,
focus-paint and popup-dismissal scopes also passed separately. The cause of that
focus interruption is unconfirmed. This is combined evidence, not a claim that
the final aggregate script completed in one uninterrupted run.

Evidence: `TestResults/dropdown-arrow-{before,focused,build,native}.log`,
`renderer-boundary-{initial,focused,build,release-package}.log`,
`visual/dropdown-arrow-{showcase,native}/`, `visual/renderer-office-showcase/`,
`visual/renderer-flat-{native,package}/`, and the refreshed
`visual/m2a-{native,package-consumer}/` result logs/captures. The package log records
its fresh consumer path. The first arrow build needed an explicit Rectangle type
to resolve a DrawText overload; the renderer's initial analyzer run rejected its
new symbols until their explicit Unshipped declarations were added.
The final scopes add `renderer-boundary-release-package-final.log`,
`renderer-boundary-final-focused.log`, `renderer-boundary-consumer-path-final.txt`,
`renderer-package-{input,keyboard,focus,dismissal}-final.log`, and
`visual/renderer-{office,flat}-package-final/`. The failed consumer result remains
in the initial aggregate capture folder; the fresh successful result is separate.

2026-10-26 arrow-height follow-up: the user approved the renderer refinement and
initially requested twice the arrow height. That 8 x 8 version passed 12 focused
rendering cases, but the user found it stretched and requested a 6-pixel height.
The shared indicator is now 8 x 6 logical pixels, with unchanged width and
centering; collapsed groups reserve the matching slot. All four arrow cases
passed in Debug at 96/120/144/192 DPI, and the normal Debug Showcase build passed
with zero warnings/errors. Updated captures at 1080/720/420/260 widths are in
`TestResults/visual/dropdown-arrow-6px-showcase/`; the wide ribbon and narrow
collapsed groups were inspected at actual 120 DPI without arrow clipping.
The initial 8 x 8 captures remain in `visual/dropdown-arrow-height-showcase/`.
Release/package checks were not repeated for those metric changes. The user
accepted the 8 x 6 arrow and demonstrated M2A polish on 2026-10-26, then authorized
the next slice. This acceptance does not close designer or other hardware gates.

The opening path revalidates ownership and presentation after cancelable opening
callbacks. A callback that removes/hides/disposes the owner must not create a
popup from a stale anchor. Strip and popup navigation track the action half as
well as the model, and closing a split's dropdown restores its arrow.

Ignored evidence: `TestResults/m2a-focused.log`, `m2a-build-debug.log`,
`m2a-release-package.log`, and `visual/m2a-{native,package-consumer}/` including
`result.log`, plus `visual/m2a-showcase/native-*-120dpi.png`.
The package consumer's retained temporary path is in the package log.
The initial API-analyzer build failed until the explicit additive Unshipped entries
were recorded; no shipped baseline was changed. An initial validation-harness build
also required its missing System.IO import; no runtime workaround was added.
Joined-edge evidence is in `TestResults/split-chrome-{before,focused,build,native}.log`,
`split-chrome-consumer-path.txt` and `visual/split-chrome-{native,package}/`.

From `winforms/`, `eng/Test-Native.ps1 -Configuration Release -Package` runs the
recorded aggregate gate. For split checks alone, use
`dotnet run --project tests/RibbonKit.WinForms.NativeTests -- --split-checks TestResults/visual/m2a-native`.
The isolated package app accepts `--validate-split <capture-folder>` and writes
its public-API assertions to `result.log`. Both modes share validation source,
not Showcase helpers or runtime implementation.

## M2B native TextBox

2026-10-26: continuation batches the native TextBox model, shared measured slots,
binding/validation, native input, popup ownership and consumer verification.
ComboBox remains a subsequent slice. No accessibility work is added.

- `RibbonTextBoxItem : RibbonItem` has bindable live `Value`, `InputWidth=120`
  logical pixels (minimum 24, excluding label), `ReadOnly=false`, `MaxLength=32767`,
  `ValueChanged`, cancelable `ValueValidating`, `ValueValidated`, `ValueCommitted`
  and `EditCanceled`. `Text` remains the label. `PerformClick()` focuses the active
  native TextBox. It has one compact row independent of command SizePolicy/icons.
- Native typing updates Value through one path. Enter validates and commits;
  ordinary native validation, Tab and F6 reject invalid navigation. Escape rolls
  back to the focus-entry/last-committed value and exits to the strip or closes
  the innermost popup. OnPropertyChanged bindings write live and rollback values;
  OnValidation bindings write only after validation succeeds. External model
  updates become the new rollback baseline. Forced hiding/disable/close commits
  valid edits or rolls back rejected ones.
- One library-owned native TextBox per item is parked on the ribbon and borrowed
  by the active popup. Transfer preserves instance, caret and selection. Closing
  a popup never owns/disposes that editor. Removal disposes its presentation,
  retaining the detached model and its bindings. Renderer ownership is unchanged.
- Shared layout aligns label spans across neighbouring editor rows and reserves
  InputWidth at each reduction step. Native children use viewport clipping and
  popup scroll positions. `DrawTextBox(context, item, editorBounds)` is the public
  label/frame hook; native text, selection and caret stay inside the real TextBox.
  The provisional API/XML baseline and designer collection inventory include it.
- Showcase's Inputs tab demonstrates bound Name, read-only/disabled fields and
  a menu editor, with ordinary application-owned validation and data. No sample
  reparenting, focus dispatcher or layout repair implements the library feature.

Verification:

- Initial M2B checkpoint, before the essential-test review below: normal Release solution, XML/public API and package builds passed with zero
  warnings/errors; **102 unit/STA cases passed**, zero skipped. Nine editor cases
  cover compact alignment at synthetic 96/120/144/192 DPI, live and deferred
  binding, rejected validation, event order, cancellation, read-only selection,
  collapse parking/expansion, ownership and removal during a validation callback.
- The public-API native Form and a fresh isolated package consumer passed **31
  editor assertions each** at actual 120 DPI (125%). These include ordinary native
  typing/caret/selection, length/read-only/disabled behavior, Tab/F6/KeyTips,
  invalid mouse focus transfer, nested Escape, shared-instance popup transfer,
  disable/resize rollback, removal/reinsertion, renderer hooks and deactivation
  without stealing focus from another Form. The consumer references the package
  from a fresh local feed/cache, with no Showcase, reflection or internal access.
- Office and flat split interaction modes plus the existing foundation input,
  keyboard, focus-paint and popup-dismissal checks passed in the native host and
  package consumer. Existing bridge checks are regression evidence only; they
  establish no new accessibility compatibility.
- The Debug Showcase build passed with zero warnings/errors. Wide Inputs and narrow
  collapsed-group captures at actual 120 DPI were inspected, along with the native
  and package consumer's field/menu/collapsed-field captures. Captures were also
  generated at 720/420 widths; this is not additional physical-DPI acceptance.

The deferred-binding regression initially exposed Component binding's automatic
write on an event named Validating, before later handlers could reject the value.
The model uses ValueValidating and writes deferred bindings after all validation
handlers succeed; the rejection regression failed before this repair and passed
afterward. Floating-window assertions were moved from a foreground-sensitive unit
case into the public native mode; the unit case retains strip parking/selection
and removal assertions. No runtime activation workaround or acceptance skip was
introduced. Input fixtures now check foreground ownership before sending keys.

The final aggregate attempt passed the Release build, all 102 unit cases, native
scopes and fresh package build, then stopped because the new package editor launcher
used a hidden startup window. Managed Visible was true while native CanFocus was
false. The same package passed when shown; the interactive launcher is corrected.
Package editor and remaining package scopes passed separately. This is combined
evidence, not a claim that the aggregate completed uninterrupted. The final two
deactivation assertions then passed in both native and isolated package modes.

Evidence: `TestResults/m2b-release-package-validated.log`,
`m2b-package-continuation.log`, `m2b-consumer-path.txt`, and
`visual/m2b-{native,package}-deactivation/result.log`. Earlier diagnostic results
remain separate. Captures are under those mode directories and `visual/m2b-showcase/`.
Run `--editor-checks <folder>` in NativeTests or `--validate-editors <folder>` in
the isolated consumer to reproduce the editor checks; the aggregate script includes
both. Actual VS authoring, additional physical display scales/monitor movement and
human acceptance remain open. Native touch and OS IME acceptance are not added by
default; accessibility compatibility remains deferred.

## Essential-test review — 2026-10-26

The user requested a smaller set containing important regressions. The initial
102 discovered unit/STA cases came from 69 methods, including 29 imported layout
algorithm cases and repeated DPI/input rows. The current set is **56 cases from
44 methods**. This is an intentional reduction in scenarios, not a claim of
unchanged percentage coverage or a measured speedup.

| Removed or consolidated scope | Retained important coverage | Cases before → after |
| --- | --- | ---: |
| Reduction algorithm's separate width/order fixtures and numerous invalid-input permutations | Six checks: exact fit/rightmost-first/minimum; custom order/fixed/omitted groups; widening/equal-step skipping; fractional fit; empty input; representative invalid arguments. Fixed-group overflow also remains in NativeLifecycleTests. Infinity, combined double overflow and duplicate-order permutations unreachable from the native caller are omitted. | 29 → 6 |
| Neighbouring row alignment DPI/font matrix | AlignmentAndLifecycleTests retains 120 DPI/9 pt and 192 DPI/18 pt, plus the separate large-label baseline test. | 8 → 6 |
| Arrow DPI repetitions | DropDownArrowTests retains the accepted 8 x 6 geometry at 120 DPI with both fonts and dropdown/split presentations. | 4 → 1 |
| Direct item disposal during activation alongside owner disposal | Activation_handlers_may_detach_item_or_dispose_ribbon retains detach and full-owner disposal. Foundation ownership tests retain direct item disposal; editor removal during validation remains separate. | 9 → 8 |
| Collapsed popup's repeated small-font run | PopupLayoutTests keeps the large-font trailing-hover/checked/click regression, multiple columns, and both fitting/overflowing menu cases. | 5 → 4 |
| Split geometry and separator DPI repetitions | SplitButtonTests and SplitChromeRenderingTests retain 120/192 DPI, both arrangements and size/menu distinctions. | 12 → 8 |
| Reflection/message-filter focus fixtures duplicating native interaction | NativeTests FocusCues retains mouse/keyboard outlines, pointer movement, Tab/F6, assistive focus and nested returns. Keyboard and public split modes retain F10/KeyTips. Those modes remain in Test-Native.ps1. | 7 → 0 unit cases |
| Separate default-renderer bitmap comparison | Default_and_custom_renderers_keep_flat_and_office_styles_distinct keeps default/custom/reset policy and the distinguishing chrome pixels in one existing fixture. Body/scroll/popup ownership and content-slot checks remain. | 4 → 3 |
| Editor geometry DPI repetitions, duplicate collapse/removal fixture, simple metadata/property-guard smoke checks | TextBoxTests retains 120/192 DPI alignment, exact binding/validation/event-order regressions and removal during validation. Public TextBoxInteractionChecks retains real typing, limits/read-only/disabled state, same-instance popup/caret/selection transfer and removal/reinsertion. Metadata and redundant setter-guard permutations are omitted. | 9 → 5 |

The four-corner fill/stroke pixel symmetry cases remain unchanged at
96/120/144/192 DPI. Disabled pointer/rendering checks, ownership, threading,
selection fallback and KeyTip conflicts remain. This review changes tests and
documentation only; runtime behavior and native/package modes are unchanged.

Validation: **56/56 Release unit/STA cases passed**, zero skipped; the final
renderer-policy consolidation, arrow fixture cleanup and activation rename passed
in a six-case focused Release follow-up. Logs:
`TestResults/essential-tests-release.log` and `essential-tests-final-focused.log`.
Native GUI/package checks were not rerun for this test-only change; their M2B
evidence above remains historical evidence for unchanged runtime code.

That review's next planned slice was ComboBox selection/editing, recorded below.

## M2C native ComboBox

2026-10-26: the user authorized the next item after the essential-test review.
This slice extends the shared editor host rather than creating a separate popup,
binding or focus coordinator. Accessibility compatibility remains deferred.

- `RibbonComboBoxItem : RibbonItem` has owned string `Items`, bindable live `Value`
  and `SelectedIndex=-1`, `IsEditable=false`, `InputWidth=120` and `MaxLength=32767`.
  It shares TextBox's validation/commit/cancel events and adds
  `SelectedIndexChanged`, `IsDropDownOpen`, `ShowDropDown()` and `CloseDropDown()`.
  `PerformClick()` focuses. The provisional API/XML baseline and designer item
  inventory include the type; actual VS string-collection round-trip is unverified.
- Selection-only unmatched values clear the selection/text. Editable unmatched
  values retain exact text and index -1. Inserts/removals preserve a surviving
  choice, including duplicate-label indices; replacing a selected choice updates
  Value. This bounded API has no object DataSource/DisplayMember/ValueMember adapter.
- Native mouse selection and closed-list keyboard selection validate/commit once.
  Live bindings receive preview/rollback; deferred bindings write after validation.
  F4/Alt+Down opens the native list. Escape first cancels that list, then restores
  the accepted editor value and exits one library popup level on the next press.
  Enter and Tab accept a valid list selection before their normal editor action.
- One library-owned native ComboBox per model transfers through collapsed/menu
  popups and preserves editable selection. The native list counts as part of its
  containing popup even outside that popup's bounds. Deactivation closes both
  without stealing another Form's focus. Choice changes close stale lists;
  removal during a selection-validation callback safely disposes presentation.
- Mixed TextBox/ComboBox rows share measured label spans. In the initial M2C
  implementation, `DrawComboBox` painted the label beside the native frame/list;
  the requested presentation follow-up below replaces that native chrome.
  Showcase demonstrates bound Font, editable validated Zoom, a disabled chooser
  and a menu chooser. All reusable behavior lives in the library.

Regression pitfalls: native selection-commit notification can precede text-change
notification, so acceptance reads the native index first. List cancellation must
not become an external-model rollback baseline. WinForms Text assignment can
select a case-insensitive match; unmatched editable text uses the public native
text setter to retain exact spelling. Handle creation can occur before native
choices are copied: positive selection is applied only after population. A test
run initially tried selected index 1 with no native choices; the recovered
stack trace points to `NativeComboBox.OnHandleCreated`. The corrected ordering
passes the editor regressions. Unit STA threads now throw unhandled exceptions
to the test runner instead of opening the framework's modal error dialog.

Initial M2C verification, before the presentation follow-up below:

- The recovered exception is preserved in `TestResults/m2c-initialization-repro.log`.
  After the repair, all eight focused Debug editor cases passed. Three new ComboBox
  unit cases cover choice mutations, deferred validation/rollback, selection binding,
  native refresh/style reuse and exact-case editable values. The existing two-DPI
  TextBox geometry case now includes mixed editor types; it adds no test cases.
- The final Release solution/API/XML build passed with zero warnings/errors;
  **59/59 unit/STA cases passed**, zero skipped. The complete
  `eng/Test-Native.ps1 -Configuration Release -Package` run passed, including
  packing and restoring/building a fresh consumer outside the checkout with its
  own package cache and no Showcase or internal-access dependency.
- The native host and isolated package consumer each passed **25 ComboBox
  assertions** at actual **120 DPI (125%)**: native mouse/keyboard selection,
  live/deferred binding, rejected validation, two-stage Escape, KeyTips/F6,
  disabled rejection, native-list bounds outside the parent popup, deactivation,
  same-instance popup transfer/selection, choice mutation, removal during a
  validation callback and the public label renderer hook.
- Existing TextBox checks (31 assertions per consumer), Office/flat split modes,
  foundation input/bridge regressions, keyboard navigation, focus appearance and
  popup dismissal passed in both consumers. Bridge regression checks establish
  no new accessibility compatibility. A prior aggregate stopped at the legacy
  focus fixture's first mouse action; the fixture now waits for its own foreground
  window before clicking. The final aggregate passed without a runtime focus fix.
- Native and package captures of mixed fields, native lists and collapsed fields
  were inspected. The normal Debug Showcase solution build passed with zero
  warnings/errors. Showcase captures at 1080/720/420/260 widths are retained;
  additional physical DPI/monitor movement, actual VS authoring and user visual
  acceptance remain open. Native touch/OS IME testing is not added by default.

Showcase capture limitation: DrawToBitmap omits unfocused selection-only ComboBox
text in the saved wide image. The visible native window showed the bound Font
and disabled choice correctly, and a visible popup capture showed its selected
choice. The existing reuse regression now also asserts the initial selected index
and native text before any input; its focused Debug and Release follow-ups passed.
No runtime painting workaround was added for the bitmap-capture limitation.

Initial evidence: `TestResults/m2c-release-package-final.log`,
`m2c-debug-build-final.log`, `visual/m2c-{native,package-consumer}/result.log`
and their captures, plus `visual/m2c-showcase/`. The earlier interrupted aggregate
is retained separately as `m2c-release-package.log`. The final aggregate log
records the retained isolated consumer directory. Subsequent native captures in
the stable `m2c-native` folder reflect the presentation follow-up.

### ComboBox selection and presentation polish — 2026-10-26

The user's recording showed pointer selection reverting to the previous value,
short dropdown rows, missing field borders and missing interaction visuals.

| Requested fix | Implementation and review scope |
| --- | --- |
| Pointer choice persists | Native hover/press can change the pending native index before any managed selection notification. A ribbon/popup repaint previously copied the unchanged model back over that index between mouse down and up. Presentation now preserves that preview while the list is open, while still applying actual model/choice changes. The public fixture pauses during hover and between press/release, refreshes the field, and verifies both native text and live/deferred bindings. |
| Taller list rows | Variable owner drawing gives list rows a minimum of 28 logical pixels, or font height plus 10 logical pixels. The compact closed field retains its alignment with TextBox. The list viewport accommodates up to eight padded rows; native scrolling and screen positioning remain native. |
| Office 2024 and neutral borders | Shared `DrawComboBox` paints the field border, arrow and label through the selected renderer. Office 2024 uses the same mirrored rounded chrome as TextBox; the neutral renderer uses square borders. The actual native control/edit child is clipped into the measured text area. |
| Hover, pressed and open visuals | The field supplies real pointer/press/focus state. The arrow area uses theme Hover, Pressed and Checked fills; open/focused/hot fields use the accent outline. Disabled fields suppress active states. `DrawComboBoxItem` is the additive public row-paint hook, using the native highlight state. |

Native input and HWND list ownership remain in the library; Showcase supplies no
painting or input workaround. The renderer hook is XML-documented in the Unshipped
API. Deferred-binding validation, exact-case editable text, native edit/caret
selection, nested Escape, deactivation and same-instance popup transfer remain
part of the retained checks. Accessibility and extra hardware gates stay deferred.

The previous mouse fixture moved, pressed and released without allowing an
intervening repaint. Adding realistic hover and press intervals reproduced the
reported reset before the fix. The native list's integral-height policy is fixed
at creation so sizing its padded viewport does not recreate a handle during
opening. Text placement/clipping precedes value synchronization, preserving exact
editable text through native layout. Visible-window captures replace DrawToBitmap
in the ComboBox fixture because the latter omits or ignores native clipped chrome.

Verification:

- Normal Debug and Release solution/API/XML builds passed with zero warnings or
  errors. **59/59 Release unit/STA cases passed**, zero skipped. The existing
  renderer-policy case checks both border styles, all four corners, hover,
  pressed and disabled suppression at synthetic 120/192 DPI; no unit cases were
  added for this follow-up.
- Office 2024 and flat modes each passed **33 native assertions** in the native
  host and a fresh isolated package consumer at actual **120 DPI (125%)**. This
  includes realistic pointer timing, selection from document focus, both
  selection-only and live-bound editable fields, deferred validation/rollback,
  list row height, field/row renderer hooks and hover/pressed/open states, plus
  the retained popup/lifetime/key checks. The final binding-specific extension
  passed in all four modes using the same unchanged runtime package.
- Existing TextBox (31 assertions per consumer), split Office/flat, foundation
  input, keyboard and popup dismissal scopes passed in both consumers. The
  aggregate stopped on an older focus-capture fixture's stale popup UIA reference;
  that unchanged fixture passed in a fresh process. The remaining native scope,
  pack/isolated restore/build and complete package scopes passed separately.
  This is combined evidence, not an uninterrupted aggregate pass; no new
  accessibility compatibility is claimed.
- Inspected visible captures of both renderers' fields, padded lists, hover,
  pressed/open states and popup transfer. An exact Showcase check also retained
  Font = Times New Roman and Zoom = 125% after paused pointer selection, verified
  their host status text, and closed/reopened the list by clicking its arrow.
  Additional physical DPI/monitor gates, actual VS authoring and user visual
  acceptance remain open.

Evidence: `TestResults/combo-polish-{before-hover,selection-fixed-retry,release-package,focus-retry,package-continuation,debug-build,showcase-final}.log`,
`visual/m2c-{native,flat-native}/`, `visual/combo-polish-package-{combo,combo-flat}/`,
`visual/combo-polish-binding-{combo-checks,combo-checks-flat}/` and
`visual/combo-polish-package-binding-{combo,combo-flat}/` contain results/captures.
The Showcase live capture is retained under
`TestResults/ComboRepro/bin/Debug/net10.0-windows/showcase-polished-120dpi.png`.
The continuation log records the isolated consumer path. Reproduce the public
fixture with `--combo-checks <folder>` or `--combo-checks-flat <folder>` in
NativeTests; the package app accepts `--validate-combo` / `--validate-combo-flat`.

### Existing-component Office 2024 consistency — 2026-10-26

The user added the ribbon body and tab headers to the consistency pass and asked
for appropriate borders and shadows before another functional slice.

| Requested scope | Implementation |
| --- | --- |
| Ribbon body margins | Shared theme `BodyMargin` defaults to 7,1,7,7 logical pixels, following the WPF Office 2024 reference. The body panel has its own bounds; content viewport, reduction, scrolling, native editors and hit targets use the inset geometry. |
| Tab headers | Office 2024 paints text and a blue underline for selected, hovered, pressed or keyboard-focused tabs, with transparent backgrounds and no rounded outline. Selected text uses the accent. This follows the user's blue-underline direction; the WPF reference's hover underline is grey. Neutral tab painting is retained. |
| Borders and shadows | Office's body is a rounded bordered card with a soft `BodyShadowColor` shadow. Its cached small blur is stretched in nine pieces so it does not rebuild a large shadow bitmap at every width. Floating ribbon menus and collapsed groups reserve a one-device-pixel frame, painted through `DrawPopupFrame` and `PopupBorder`, and request the native Windows shadow through `PopupShadowEnabled`. Native ComboBox list windows keep their Windows frame/shadow ownership. |
| Earlier navigation styling | Office scroll controls have transparent idle surfaces and themed hover/pressed chrome, over the existing body/tab surface. Neutral drawing remains available through the base renderer. |

Theme values and the popup-frame callback are additive, XML-documented Unshipped
API. No sample painting helper implements these features. The default renderer
owns and disposes the bounded shadow cache; assigned renderers remain borrowed.
The initial tab regression failed on its unwanted filled background before the
fix. Eight focused Debug renderer/layout/four-corner cases then passed. Existing
odd/even four-corner cases now also check the shadow mask at 96/120/144/192 DPI;
the unit case count stays 59. Native popup frame/Windows shadow-policy and renderer
hook checks extend the existing public ComboBox fixture.

Verification:

- Normal Debug and Release solution/API/XML builds passed with zero warnings or
  errors. **59/59 Release unit/STA cases passed**, zero skipped. The complete
  Release/package aggregate passed uninterrupted, including fresh restore/build
  outside the checkout and native/package Office and neutral modes. The final
  body-callback XML description was then rebuilt/repacked without a behavior
  change; passed input scopes were not repeated for that documentation edit.
- Each native/package ComboBox mode passed **36 assertions** at actual 120 DPI
  (125%), including the renderer-owned popup frame, unobscured hosted content
  and Windows shadow policy. Existing TextBox, split Office/flat, foundation
  input, keyboard, focus appearance and popup-dismissal checks passed in both
  consumers. These bridge regressions establish no new accessibility support.
- Inspected the body/tab/field and floating-popup frame/shadow captures. Debug
  Showcase Home and Inputs captures cover 1080/720/420/260 widths, including
  collapsed/scrolling states. The ordinary app uses the library's default Office
  renderer; no Showcase style patch supplies the body, tabs, borders or shadows.
  A single diagnostic width pass logged 7.4–45.6 ms for combined resize/layout/
  paint work; this is not a compositor frame rate or motion/performance acceptance.

Evidence: `TestResults/office-consistency-{before,native-debug-retry,release-package,debug-build,final-doc-build,showcase}.log`,
the refreshed `visual/m2c-{native,flat-native,package-consumer,flat-package-consumer}/`
results/captures, and `TestResults/ComboRepro/bin/Debug/net10.0-windows/office-consistency/`.
An initial Debug native launch did not acquire foreground focus; its fresh retry
and the final aggregate passed. User visual acceptance, additional physical
scales/monitor movement and actual VS authoring remain open. Accessibility
compatibility and motion remain deferred by user direction.

The popup/open-state and split-toggle follow-up is recorded below. ScreenTip,
minimize, radio variants, richer menus and optional command APIs remain separate work.

### Popup open states, softer shadows and split toggles — 2026-10-26

| Requested scope | Implementation |
| --- | --- |
| Dropdown and split-arrow open appearance | The renderer uses the owned popup session, exposed through read-only `RibbonDropDownItem.IsDropDownOpen`, to retain the pressed fill while open. The opener is invalidated on open/close. Collapsed group openers use the same treatment. Disabled items do not acquire an open fill. |
| Visible popup border and softer shadow | A ToolStrip renderer adapter delegates background/frame painting to RibbonKit, avoiding default ToolStrip border/corner treatment. All four sides use a one-device-pixel `PopupBorder`, now #BFBFBF. The native dark shadow is disabled. `DrawPopupShadow` uses the body's six-logical-pixel blur and two-logical-pixel downward offset; `PopupShadowColor` defaults to the same ARGB(24,0,0,0) opacity as the body. |
| Toggleable split primary | Opt-in `CheckOnClick` defaults false. Primary mouse, keyboard, KeyTip and `PerformClick` activation changes bindable `Checked`, raises `CheckedChanged`, then `Click`. Direct assignment raises no `Click`. Menu opening never changes the primary value. The checked fill and open-arrow fill can coexist; the shared square join and single separator are preserved. |
| Ordinary toggle button | Existing `RibbonToggleItem : RibbonButtonItem` supplies standalone two-state commands with `Checked`/`CheckedChanged`. Ordinary `RibbonButtonItem` remains a command. Showcase Home adds Track changes as a toggle split while Paste remains an ordinary split. |

The library owns one nonactivating, click-through layered shadow window per open
popup. Only the shadow uses per-pixel transparency; the content/editor HWNDs stay
opaque. The helper owns its native handle and temporary GDI resources and is
destroyed when the popup hides/closes/disposes. Its bitmap is refreshed for size,
DPI, theme or renderer changes, not for each pointer repaint. `PopupShadowEnabled`
also respects the Windows shadow preference. Native ComboBox list windows keep
their native frame/shadow. The added APIs have XML comments and Unshipped entries;
applications can override shadow/frame painting through shared renderer hooks.

Verification:

- Debug and Release solution/API/XML builds passed without warnings or errors.
  **59/59 Release unit/STA cases passed**, with no new test cases. Existing tests
  cover checked-state event ordering, programmatic assignment, two-way binding,
  disabled activation and independent primary/menu input. A final extension of
  the existing joined-edge test also passed at 120/192 DPI with both checked
  values; this was a focused two-case run after the aggregate.
- The complete Release/package aggregate passed uninterrupted, including fresh
  restore/build outside the checkout. Each native/package Office and neutral
  ComboBox mode passed **43 assertions**, plus its presentation summary. These
  check actual screen pixels on all four popup edges, the soft shadow's opacity,
  nonactivation/click-through window flags, shadow destruction and disabling,
  latched dropdown paint, native-list input and focus-safe dismissal.
- Each native/package split mode passed **35 assertions**, plus its summary.
  Primary pointer/keyboard/KeyTip toggling, independent menu state and retained
  checked/open paint passed. Existing TextBox, foundation input, keyboard, focus
  appearance and nested popup dismissal scopes also passed in both consumers.
  The retained bridge checks establish no new accessibility compatibility.
- Inspected the Office/neutral visible popup-frame/shadow captures and Office
  checked-primary/open-arrow captures at actual **120 DPI (125%)**. Additional
  physical DPI/monitor movement, actual VS authoring and user visual acceptance
  remain open. Accessibility compatibility and animation remain deferred.

Evidence: `TestResults/popup-toggle-{build,combo-debug,split-debug,release-package,debug-final,checked-join}.log`,
`visual/popup-toggle-{combo-debug,split-debug}/`, and refreshed
`visual/m2c-{native,flat-native,package-consumer,flat-package-consumer}/` and
`visual/{m2a-native,renderer-flat-native,m2a-package-consumer,renderer-flat-package}/`.
The aggregate log records the isolated package consumer path.

### Office 2024 rounded popup corners — 2026-10-26

Office ribbon menus, nested menus and collapsed-group popups now use a default
six-logical-pixel `PopupCornerRadius`, matching the WPF Office 2024 reference.
The neutral renderer keeps square corners. Public `GetPopupCornerRadius` provides
the shared device-pixel radius for the frame, native clipping and shadow; both
new APIs are XML-documented and tracked as Unshipped. Negative theme radii are
rejected with the other invalid metrics.

The existing mirrored coverage calculation creates the popup and hosted-content
regions. Their opaque HWNDs include only fully covered pixels. The existing
nonactivating, click-through helper now composites both the soft shadow and the
partially covered frame edge, retaining smooth corners without transparent native
editors or a rectangular background halo. Frame edges remain when shadows are
disabled. Controls own/dispose their assigned regions; popup closure destroys
the frame/shadow helper. Outside-press dismissal respects the rounded region,
including clicks through removed corner pixels.

Verification:

- Debug and Release solution/API/XML builds passed with zero warnings/errors;
  **59/59 Release unit/STA cases passed**, zero skipped. The existing four-corner
  cases additionally compare every frame pixel and its opaque clipping mask at
  odd/even bounds and 96/120/144/192 DPI, including real partial-alpha edges and
  neutral square output. Fractional-point GDI+ region queries initially probed
  neighbouring pixels; integer device-pixel queries match the raster exactly.
- The first aggregate stopped before native interaction because its test Form
  could not acquire foreground focus. A shown-window retry passed. The remaining
  native/package scopes passed in a continuation without repeating the passed
  unit suite. This is combined evidence, not an uninterrupted aggregate pass.
- Native and fresh isolated package consumers each passed **46 Office assertions**
  and **44 neutral assertions**, plus their presentation summary, at actual
  **120 DPI (125%)**. These include four equal clipping corners, visible borders,
  the soft shadow, native ComboBox list/ownership behavior, shadow disabling,
  corner-click dismissal and focus preservation. Existing TextBox, split,
  foundation input, keyboard, focus appearance and nested popup dismissal scopes
  passed in both consumers. No accessibility compatibility is added or claimed.
- Inspected actual screen captures of Office rounded popup borders/shadows and
  neutral square popups. Physical scales beyond 125%, monitor movement, actual
  VS authoring and user visual acceptance remain open; animation remains deferred.

Evidence: `TestResults/popup-corners-{debug-build,four-corner,release-package,native-retry,continuation,debug-final}.log`,
`visual/popup-corners-{debug,native-shown}/`, and refreshed
`visual/m2c-{flat-native,package-consumer,flat-package-consumer}/` captures/results.
The continuation log records the isolated consumer directory and completion.

### Large horizontal example, collapsed spacing and staged choice popup — 2026-10-26

- Large split items already support both arrangements. Showcase Track changes
  now uses fixed Large size and explicit Horizontal layout, with an application
  pencil icon. Paste remains the Large vertical example. The shared split model,
  toggle behavior, measurement and renderer contracts are unchanged.
- Collapsed opener painting previously used the full group slot while hit
  testing used a smaller rectangle. Layout now supplies one inset rectangle to
  the group slot and hit target: three logical pixels horizontally and four
  vertically. Painting, focus, hit testing, the retained bridge bounds and popup
  anchoring agree. Group reduction widths and ordinary expanded bounds are
  unchanged. Office and neutral renderers consume the same geometry.
- The user explicitly chose **stage the custom ComboBox choice popup next**.
  Native choice lists remain unchanged in this slice. An isolated public-API
  prototype observed ComboLBox class style 0x24808, including CS_DROPSHADOW;
  per-window DWM nonclient-policy, layered-alpha and region experiments retained
  its dark rectangular shadow. Changing the shared Windows class is not a
  suitable library styling boundary. No class-global setting was changed, and
  none of the prototype styling is in the runtime.

Next focused slice: retain native editor text/caret/selection and the existing
`RibbonComboBoxItem` value, selection, binding and validation contracts, while
using a library-owned choice popup with the shared frame/radius/shadow renderer.
Prefer a native ListBox inside that popup for rows, scrolling and incremental
search. Establish pointer/keyboard preview and commit/cancel, first-Escape list
closure, editable selection, outside/deactivation dismissal, screen fitting and
nested-popup ownership before replacing the native list. Any required public
API changes must be surfaced first; no new model API is proposed at this point.
Accessibility compatibility and animation remain deferred.

Verification: Release solution/API/XML build and **59/59 unit/STA cases passed**.
Focused native Office/neutral split and ComboBox scopes passed, including the
new opener-inset assertion in the existing split fixture. Fresh isolated package
restore/build and Office/neutral split, TextBox and ComboBox checks passed.
No full native foundation/focus/dismissal aggregate was repeated for this bounded
geometry change. Debug and Release Showcase builds passed; actual Debug Showcase
captures at **120 DPI (125%)** show Large horizontal Track changes unchecked and
checked, plus the inset collapsed Editing actions opener. Other physical scales,
monitor movement, actual VS authoring and user visual acceptance remain open.

Evidence: `TestResults/group-inset-{release,fixture-build,native-scopes,package,review-build,showcase-review,debug-final,showcase-release}.log`,
`visual/group-inset-{native,flat-native,combo-native,combo-flat-native}/`, refreshed
package consumer results and `TestResults/GroupReview/bin/Debug/net10.0-windows/`
captures. The native choice-list prototype is in `TestResults/ListChromeProbe/`;
its captures and `list-chrome-probe-run.log` are diagnostic evidence only.

### Shared Large-caption spacing and complete second rows — 2026-10-26

The reported Track changes caption used two native 20-device-pixel rows inside a
37-pixel label area at actual 120 DPI. The second row lost its lowest glyph pixels.
The user clarified that the fix must cover all Large button types.

Regular buttons, toggles, dropdowns and horizontal/vertical splits now share
`RibbonCaptionLayout`. It measures native text, prefers word boundaries, keeps
text-element boundaries for long words, and draws up to two native single-line
cells through the existing `DrawText` hook. Baseline advance removes two logical
pixels of native cell leading (rounded upward); Large captions keep three logical
pixels of bottom clearance. Font size, icon position and the shared first-line
origin stay unchanged. A bounded renderer-owned cache avoids remeasuring stable
captions during pointer repaint; it holds strings/metrics rather than borrowed
Font objects or glyph bitmaps.

The initial fix reserved the existing arrow band for wrapped vertical splits by
calculating an additional minimum body height across visible tabs. The following
slice supersedes that policy with width-based fitting and an inline second-row
arrow, as requested by the user. The icon/label origins, split geometry rules,
joined border, state behavior and arrow size are preserved. No public API was added.
Medium/Small and menu-row presentation is unchanged. The custom ComboBox choice
popup remains staged next as the user chose.

Verification:

- Debug and Release solution/API/XML builds passed; **59/59 Release unit/STA
  cases passed**, zero skipped. The existing alignment case now checks the
  reported horizontal caption and all five Large presentations at synthetic
  96/120/144/192 DPI in normal/hover/checked states. It compares every second-line
  glyph pixel, including descenders, with an unconstrained native reference line.
  The test count did not increase.
- Native Office and neutral split fixtures passed the all-type caption checks,
  including wrapped vertical splits and the reported horizontal caption after
  removing the additional vertical height requirement. Visible captures were
  delayed until the compositor presented resize/repaint changes; initial immediate
  captures contained stale window pixels and were not accepted as visual evidence.
- Pack, isolated restore/build and package Office/neutral split, ComboBox and
  TextBox checks passed. An initial package ComboBox pointer-open check failed;
  that unchanged scope passed in a fresh process, and the remaining package scopes
  passed separately. This is focused combined evidence, not a clean full aggregate.
- Inspected fresh Office/neutral all-type and reported-caption screen captures,
  plus actual Debug Showcase Track changes, at **120 DPI (125%)**. Additional
  physical scales/monitor movement and user visual acceptance remain open.
  Accessibility compatibility, animation and the ComboBox popup replacement remain
  outside this fix.

Evidence: `TestResults/caption-{probe,spacing-focused,spacing-release,spacing-native-final,spacing-package,spacing-package-continuation,spacing-showcase,spacing-debug-final}.log`,
`visual/caption-{native-final,flat-native-final,package-split,package-split-flat,package-combo,package-combo-flat,package-editors}/`
and `TestResults/GroupReview/bin/Debug/net10.0-windows/` captures. The package log
records the fresh external consumer path.

### Two-row width fitting and wrapped vertical arrows — 2026-10-26

This slice initially measured the complete caption for Large regular buttons,
toggles, dropdowns and both split arrangements before group reduction. The former
108-logical-pixel maximum became a preferred width: a button could grow beyond it
when required to fit at most two full rows. Word wrapping keeps text-element
boundaries; explicit breaks after
the first are folded into the second row. Single graphemes cannot become an empty
second row just to accommodate an arrow. The renderer always draws one or two
measured native single-line cells, without a third-row fallback or ellipsis.

Wrapped Large dropdowns and vertical splits centre their last caption row and
16-logical-pixel arrow slot together. The arrow remains 8 by 6 logical pixels.
Single-row vertical splits retain the arrow below the caption; horizontal splits
retain the separate full-height arrow column. Measurement reserves the matching
space, while split action rectangles, joined chrome and activation remain intact.
The additional caption-dependent body minimum has been removed. Existing
font-aware three-row sizing remains; caption length and split orientation cannot
raise body height. Wider content uses the existing reduction/collapse/scroll paths.
No public API was added. The custom ComboBox choice popup remains the next slice.

Verification:

- Debug and Release solution/API/XML builds passed with no warnings or errors.
  **59/59 Release unit/STA cases passed**; the existing caption case was extended,
  without adding test cases. It covers all five Large presentations, long words,
  multiword captions, explicit breaks, Japanese, surrogate pairs and combining
  marks at synthetic 96/120/144/192 DPI. It checks complete two-row text, measured
  widths, arrow placement, unchanged body height and second-line glyph pixels.
  A final single-grapheme safeguard passed the affected caption case again.
- Native and freshly packed independent consumers passed Office and neutral
  split fixtures: complete captions, dynamic widening/shrinking, fixed body height,
  row-two arrows, separate split actions and the existing popup/input lifecycle.
  The isolated consumer uses public APIs and no Showcase assets or helpers.
- Inspected actual screen captures for all five Large types, long vertical and
  horizontal captions, and neutral dropdowns at **120 DPI (125%)**. Debug Showcase
  review retains the original Track changes and collapsed-group presentation.
  Other physical DPI scales/monitor movement and user visual acceptance remain
  pending. Unrelated native/ComboBox/TextBox and WPF suites were not repeated.

Evidence: `TestResults/two-row-{focused,release,release-final,debug,native,package,showcase}.log`,
`TestResults/visual/two-row-{native,flat-native,package,flat-package}/` and the Debug
`TestResults/GroupReview/` captures. The package log records the fresh external
consumer directory.

### Configurable Large-button width ceiling and ellipsis — 2026-10-26

The user found unlimited widening visually excessive and approved a configurable
width ceiling with two caption rows and ellipsis. This supersedes the preceding
slice's unlimited width fitting; its row-two arrow and fixed body-height rules remain.

`RibbonTheme.LargeButtonMaxWidth` defaults to 108 logical pixels and caps the entire
Large button, including a horizontal split's arrow column. Assigning a theme with
a value below the existing 60-logical-pixel minimum throws. Changing the immutable
theme remeasures the strip and closes existing popups through the model invalidation
path; reopened groups use the new metric. Regular buttons, toggles, dropdowns and both split arrangements
share the policy; no new item-specific setting was added.

Native text measurement wraps at word/text-element boundaries and trims only the
last row to its available width using a literal ellipsis. The measured arrow slot
is reserved before trimming, so the last row and arrow remain centred together.
Trimming preserves surrogate pairs and combining sequences. The complete model
`Text`, icon space, body height, split action rectangles and single-row vertical
arrow placement remain intact. ScreenTips remain future work.

Verification:

- Debug and Release solution/API/XML builds passed with no warnings or errors;
  **59/59 Release unit/STA cases passed**, with no additional test cases. The
  existing caption regression covers 60/108/160 width limits, all five Large
  presentations, synthetic 96/120/144/192 DPI, glyph clipping, Unicode-safe
  truncation, complete model text, arrow clearance and fixed body height.
- Native Office and neutral split fixtures passed default-cap ellipsis, live
  public theme changes to 160, shrinking to short labels and existing split/input
  lifecycle checks. Fresh isolated package restore/build and both renderer scopes
  passed. The package neutral scope initially failed its foreground-ownership
  guard before native input; only that scope was retried in a fresh process and
  passed. These are focused combined results, not a clean full aggregate.
- Inspected default-limit and configured-width screen captures, including
  ellipsis beside the second-row arrow, in Office and neutral at **120 DPI (125%)**.
  Debug Showcase Track changes and collapsed-group review passed. Other physical
  scales/monitor movement and user visual acceptance remain pending. Unrelated
  ComboBox/TextBox/native foundation and WPF suites were not repeated.

Evidence: `TestResults/caption-limit-{focused,release,native,package,debug,showcase}.log`,
`TestResults/visual/caption-limit-{native,flat-native,package,flat-package}/` and
Debug `TestResults/GroupReview/` captures. The package log retains the fresh
external consumer path. The custom ComboBox choice popup remains staged next.

### Themed ComboBox choice popup — 2026-10-27

The staged replacement is implemented in the shared library. The native ComboBox
and editable child retain text, caret, selection and closed-field behavior. Their
Windows ComboLBox stays hidden; `RibbonChoicePopup` owns a native ListBox inside
`RibbonPopupWindow`. Menus, collapsed groups and choice lists now use that same
opaque frame, mirrored corner regions and layered soft shadow. Existing renderer
hooks, `PopupBorder`, `PopupCornerRadius` and shadow settings apply to all of them.
Office uses rounded corners; neutral retains square corners. No public API was added.

The existing value/binding/validation contracts remain: pointer movement and keyboard
navigation preview live values, successful acceptance commits once, and Escape or
outside/deactivation cancellation restores the opening state. Editable typing
keeps native editor focus, including while the popup is open. A first Escape
closes choices, and the next exits the containing ribbon popup. Rejected keyboard
acceptance stays open; rejected pointer acceptance restores the opening value.
Accepted callbacks can close the popup without rolling an accepted value back.

The ListBox supplies native scrolling and incremental search. The library
coordinates arrows/Home/End/PageUp/PageDown and Tab with the editor, fits up to
eight rows inside the screen work area and opens above the field when needed.
Nested choice input preserves its owning ribbon popup. Closing, moving, disabling,
reparenting or removing the presentation releases the list and its shadow;
external value/choice updates close stale preview without overwriting new state.

Investigation found that Windows can synthesize mouse movement under a stationary
pointer when the list repaints. Treating that as new hover replaced editable text
with the row underneath it. The list now compares actual pointer position before
changing hover selection; the regression types with the pointer left in place.

Verification:

- Debug and Release solution/API/XML builds passed with no warnings or errors.
  **59/59 Release unit/STA cases passed**. Existing ComboBox and popup tests were
  extended for empty lists, callback removal/closure, theme changes and work-area
  fitting at synthetic 96/120/144/192 DPI; no test cases were added.
- Native Office/neutral ComboBox, TextBox and Office/neutral split scopes passed.
  ComboBox scopes cover actual pointer/keyboard preview, live/deferred binding,
  rejected acceptance, native editing/search/wheel scrolling, Escape in two steps,
  four-corner clipping/frame pixels, screen edges, nested ownership, deactivation
  and lifecycle mutations. Fresh pack, isolated restore/build and the same five
  public-API consumer scopes passed, without Showcase resources or helper access.
- Inspected Office/neutral choice frame/shadow captures and actual Debug Showcase
  Font/Zoom popups at **120 DPI (125%)**. Showcase Font pointer acceptance and Zoom
  typing/cancellation passed. Its initial startup foreground guard failed under
  `dotnet run`; the unchanged compiled probe passed when launched directly in a
  fresh shown process. Earlier editable-typing failures preceded the pointer fix.
- These are focused combined results, not a full native foundation/UIA aggregate.
  Other physical scales/monitor movement, actual VS authoring and user visual
  acceptance remain pending. Accessibility compatibility and animation stay
  deferred; no native touch/IME gate was added.

Evidence: `TestResults/choice-popup-{focused,release-complete,native-final,package,debug,showcase-final}.log`,
`TestResults/visual/choice-popup-{office-final,flat-final,editors-final,split-final,split-flat-final,package-office,package-flat,package-editors,package-split,package-split-flat}/`
and Debug `TestResults/ChoicePopupReview/` captures. The package log retains the
fresh external consumer directory. Showcase's Inputs tab uses the shared behavior
with no sample-only popup implementation.

### White ribbon background and label-width tab indicators — 2026-10-27

The shared default `RibbonTheme.Background` is now white, including the tab strip
and the area around the body card. Applications can still replace that existing
theme value. Office tab indicators are centred on the measured native label width
with four logical pixels of extra length per side. Their thickness is three
logical pixels, with a one-logical-pixel bottom gap and rounded ends painted by
the existing mirrored coverage renderer. Text measurement and drawing both use
NoPadding/NoPrefix. Tab hit bounds, strip scrolling and body geometry are retained.

Selected, hovered, pressed and keyboard-focused enabled Office tabs share this
indicator shape; disabled tabs suppress it. Neutral retains its tab presentation
over the same white default background. No public API or Showcase-only painting
was added.

Verification:

- Release solution/API/XML build and **9/9 focused unit/STA cases passed**. The
  existing renderer case checks several label lengths, odd/even bounds, all four
  active states, disabled suppression and mirrored cap pixels at synthetic
  96/120/144/192 DPI. Existing corner, font/text refresh and tab-overflow cases
  passed. The test inventory remains 59; the full suite was not repeated.
- Native Office/neutral split scopes passed the white-background checks and
  existing input/popup behavior. Office additionally checks actual underline
  width, thickness and rounded-cap coverage. Its capture isolates blue chroma
  because the body's gray shadow also extends under the tab strip; counting all
  non-white pixels initially included that shadow rather than just the indicator.
- Fresh package restore/build and independent Office/neutral consumer checks,
  Debug build and Showcase review are recorded in the evidence below. Actual
  display inspection is at 120 DPI (125%); other physical scales/monitor movement
  and user visual acceptance remain pending.

Evidence: `TestResults/tab-underline-{focused,release,native,package,debug,showcase}.log`,
`TestResults/visual/tab-underline-{native,flat-native,package,flat-package}/` and the
Debug `TestResults/GroupReview/` captures. The package log records the fresh
external consumer path.

### Startup paint and native editor stability — 2026-10-27

The startup recording's apparent ribbon movement was staggered painting. A shown
Debug trace kept the ribbon at `(0,0,1080,185)` and the document at its measured
offset throughout startup at 120 DPI. Native ComboBoxes painted while the first
buffered ribbon draw was still constructing its background. The initial ribbon
paint took about 140 ms, including about 75 ms in body rendering.

Ribbon and popup painting no longer synchronize native editor presentation.
Model/layout changes, tab selection, renderer replacement and hide/show maintain
that presentation explicitly. ComboBoxes retain their native height rather than
repeatedly requesting a different PreferredHeight, embedded edit windows move
only when their rectangle changes, and unchanged selections are not reapplied.
This removes the resize/reselection path behind hover/click flicker and text
nudges. The rounded painter fills its fully covered central band directly;
analytic corner coverage, gradients, opacity and borders retain their existing
math. No public API, Showcase timing workaround or window-animation change was
added.

Verification:

- Debug and Release solution/API/XML builds passed without warnings or errors.
  **59/59 Release unit/STA cases passed** after the runtime changes. The existing
  ComboBox case additionally checks stable first-show bounds, paint without
  repositioning/reselection, tab switching and hide/show; its three-case class
  passed after the final lifecycle assertions. The inventory remains 59.
- Shown Office/neutral native ComboBox scopes passed. Fresh isolated package
  restore/build and both independent consumer scopes also passed. Their final
  checks include actual pointer hover, press/cancel and opening/closing a
  neighbouring command popup: existing TextBox, ComboBox and embedded edit HWNDs
  received **zero paint, position or selection-reset messages** during that
  interaction. Existing editing, binding, validation, nested popup, screen-edge
  and lifecycle checks passed. An initial neutral run lost foreground focus;
  the fresh retry passed.
- A Debug startup trace measured about **20 ms** for the first ribbon paint and
  **8 ms** for body drawing. The 60 fps startup capture was inspected; the earlier
  sustained native-text-only ribbon phase was removed. Four existing Debug
  Showcase captures (normal, hover, checked and collapsed-group hover) were
  pixel-identical before/after the renderer optimization.
- Native evidence is at **120 DPI (125%)**. These are focused native/package
  scopes, not the full foundation/UIA aggregate. Other physical scales/monitor
  movement remain pending. The user accepted the demonstrated startup, editor
  flicker and text-position fixes on 2026-10-27; accessibility compatibility and
  animation remain deferred.

Evidence: `TestResults/startup-{debug,release,tests,lifecycle-tests,package,rendering}.log`,
`TestResults/startup-after/{trace.log,startup.mp4}`, and
`TestResults/visual/startup-{editor-office,editor-flat-retry,package-office-click,package-flat-click}/`.
`TestResults/startup-consumer-path.txt` records the fresh external consumer.

## M2D compact check and radio options

2026-10-27: the user accepted the startup/editor polish and authorized continuing.
The next bounded M2 slice adds `RibbonCheckBoxItem` and `RibbonRadioButtonItem`,
both ordinary `RibbonButtonItem` commands with compact indicator/label rows.
Checkboxes expose bindable `Checked` and WinForms `CheckState`, optional
`ThreeState`, and their change events. Radio items expose bindable `Checked` and
`GroupName`: an empty name groups siblings in their logical item collection;
nonempty names group options within one ribbon (or their detached logical tree).
Selection clears peers, including hidden/disabled peers, before the activating
Click; clicking an already selected radio does not clear it. Removing an option
does not select a replacement. Attaching a checked option or changing its group
normalizes the new scope. Binding, popup transfer and resizing use the same model.

Shared renderer hooks paint the check/radio indicators. Options always use a
compact Medium row; inherited image/size settings do not replace the indicator.
An Options tab, nested choices and collapsed groups demonstrate the public API.
ScreenTips, richer menus, minimize and optional ICommand remain later work.
No accessibility implementation or animation is included in this slice.

Implementation and verification:

- `DrawCheckBoxIndicator` and `DrawRadioButtonIndicator` are public renderer
  hooks; Office and neutral share layout, label alignment and vector marks.
  The collection designer offers both new item types. The normal Debug Showcase
  starts on Options, with bound Ruler entries, mixed Gridlines, document-view
  radios, nested page-color choices and the same models in collapsed groups.
- Radio selection clears peers before staging its own new value. The first
  binding regression exposed BindingSource refreshing the pending option from
  stale source data during a peer update; publishing in this order prevents that
  rollback. Attachment/group changes normalize selection, and reentrant removal
  can safely end it without retaining a stale selected item.
- Enum-valued `CheckState` bindings use `formattingEnabled: false`. The initial
  formatted binding reported that DBNull could not be assigned to CheckState:
  WinForms formatting interprets Indeterminate as a null source value. Boolean
  `Checked` bindings retain indeterminate when refreshing an already true value.
  XML documentation and README record the enum-binding setting.
- Normal Debug/Release solution, XML/API and package builds passed without
  warnings/errors. **62/62 Release unit/STA cases passed**. Three new essential
  cases cover check state/events/binding, radio grouping/binding/lifecycle and
  compact layout/renderer coverage. Indicator symmetry covers odd/even bounds at
  synthetic 96/120/144/192 DPI. No existing cases were removed or multiplied.
- Shown Office/neutral native checks and fresh isolated Release package consumer
  checks passed mouse, keyboard, KeyTips, mixed/disabled options, shared boolean
  binding, radio bindings, nested-menu exclusivity, collapse and resize return.
  Actual check/radio pixels were symmetric on the **120 DPI (125%)** display.
  The first native pixel capture was too early during the opening animation;
  settled captures passed. An early package Space assertion failed; the same
  runtime passed a fresh process after adding state/focus diagnostics. The cause
  of that interruption is unconfirmed. Subsequent fixture-only completion/input
  guards stopped before input when the test Form could not obtain foreground
  focus. Earlier native/package passes remain combined evidence. The final guarded
  scopes subsequently completed in the centering follow-up below.
- Actual Debug Showcase wide, menu, nested-radio and collapsed captures were
  inspected at 120 DPI. Additional physical scales/monitor movement, actual VS
  authoring and user review remain pending. This was not the complete native
  foundation/UIA aggregate. Accessibility compatibility remains deferred.

Evidence: `TestResults/options-{focused,debug,release,tests,package,showcase}.log`,
`TestResults/visual/options-{office,flat,package-office-retry,package-flat}/` and
Debug `TestResults/OptionsShowcaseReview/` captures. Final guarded startup stops
are retained separately in `visual/options-package-{final,guarded}-options/`.
`TestResults/options-consumer-path.txt` identifies the fresh independent consumer.

### Checkmark and mixed-state centering — 2026-10-27

The user's captures confirmed that both marks sat low; horizontal inspection also
found a slight rightward offset. The shared painter now anchors both to the box's
pixel centre, using the tick's visual weight for its two-dimensional translation.
The tick shape/size, stroke width, indicator box and label positions are retained.
The anchor derives from actual device-pixel bounds; stroke width still scales with
DPI. Office and neutral use the same corrected marks.

The existing compact-rendering case failed before each axis correction, then
passed ink-centre checks for Checked/Indeterminate in both styles at synthetic
96/120/144/192 DPI and odd/even dimensions. **7/7 focused Release unit/STA cases
passed**, including option behavior and four-corner coverage; no cases were added
and the full 62-case suite was not repeated. Debug and Release builds passed.

Fresh isolated package restore/build and both final Office/neutral option scopes
passed with the completion/focus/modifier guards enabled. These include actual
native pointer, keyboard, binding and popup/collapse behavior. At **120 DPI (125%)**,
the mark centres were within 0.1 physical pixel of the box centre on both axes.
Actual Debug Showcase and package captures were inspected. The initial Showcase
popup capture was interrupted; a focused wide capture completed separately.
Other physical scales/monitor movement and user visual review remain pending.

Evidence: `TestResults/checkmark-{centering-before,horizontal-before,centering-tests,centering-release,centering-debug,centering-package,centering-showcase}.log`,
`TestResults/visual/checkmark-centered-package-{office,flat}/`, and the updated
Debug `OptionsShowcaseReview/options-wide.png` capture. The consumer location is
in `TestResults/checkmark-centering-consumer-path.txt`.

### Body scroll buttons — 2026-10-27

Body overflow buttons now leave two logical pixels above and below their hit and
paint bounds, matching the existing side clearance. Their width and the body/content
viewport remain unchanged. Both renderers use `PopupBorder` at rest, an accent
border on hover/press, and the existing interaction fills. Office
keeps rounded corners; neutral keeps square corners. Shared vector chevrons replace
the tiny font glyphs with a six-by-ten logical-pixel arrow and a DPI-scaled stroke.
Tab-strip scroll buttons retain their appearance. The existing public
`DrawScrollButton` override still owns the complete scroller.

The initial four-pixel vertical gap and dark border were refined after user review.
`RibbonTheme.BodyScrollButtonVerticalInset` now owns the top/bottom clearance:
default 2, zero for full-height buttons, or another nonnegative logical value.
Layout caps oversized values at half the body height; negative values are rejected
before changing the theme. The provisional API baseline, XML and README document
this setting. Renderer selection does not silently change the selected theme metric.

**62/62 Release unit/STA cases passed** after the refinement; no cases were added.
Existing coverage checks frame states, arrow size/centering, mirrored corners at
synthetic 96/120/144/192 DPI and odd/even bounds, uniform default clearance,
zero/custom/clamped theme insets, body/tab geometry, and custom-renderer ownership.
Normal Debug/Release and fresh isolated package restore/build passed without
warnings/errors. Both shown Office/neutral package-consumer scopes passed at
**120 DPI (125%)**: idle/hover/pressed frame and clearance pixels, inactive gap
clicks, scrolling in both directions and public theme changes to zero/six-pixel
insets. Actual Office idle/hover and neutral idle captures were inspected after
the refinement. The Debug Showcase is rebuilt; user review and additional physical
display scales remain pending. This was not the full native/UIA aggregate.

Current evidence: `TestResults/body-scroll-refinement-{tests,release,debug,package}.log`,
`TestResults/visual/body-scroll-refined-package-{office,flat}/` and
`TestResults/body-scroll-refinement-consumer-path.txt`. Initial native/package
evidence remains in `TestResults/visual/body-scroll-{office,flat,package-office,package-flat}/`.

## M2E passive ScreenTips — 2026-10-27

Started from the user's committed `547693b` baseline with a clean checkout. The
new provisional API is `RibbonItem.ScreenTipTitle` and `ScreenTipDescription`,
localizable designer strings, plus `RibbonRenderer.DrawScreenTip` with measured
title/description slots. Empty title uses the full model `Text`; captions are
never replaced by their displayed ellipsis. Metadata is separate from command
state and accessibility descriptions. Both split halves share one tip.

A per-ribbon manager uses existing layout targets, including native editor
slots, command menus and collapsed groups. Passive hover waits 600 ms; disabled
items may explain themselves without gaining interaction state or activation.
The window takes no focus/capture, stays outside the menu stack and cancels on
pointer departure, keyboard/press/wheel input, command/menu activation, model
mutation/removal, layout/DPI changes, hiding, movement/deactivation and disposal.
Real pointer movement restarts a hover after input cancellation. Native text
selection and nested Escape/focus behavior are retained. Working-area placement
flips above an item when needed. Choice-list rows have no separate metadata and
suppress item tips while open. Images, help links, interactive/pinned tips and
keyboard-triggered help remain later work; accessibility compatibility is deferred.

Frame/radius/shadow painting uses the existing shared popup hooks; Office is
rounded and neutral square. The later theme inventory should include the current
320-logical-pixel width ceiling, 12-pixel padding, six-pixel description gap,
eight-pixel placement/work-area clearance and bold title font treatment. This
slice does not start the comprehensive theming pass. The Showcase starts on Home
with full-caption Find, authored Copy/Paste and disabled explanations; Inputs,
its popup and Options demonstrate editors and check/radio tips.

Two user-reported follow-ups were reproduced and repaired:

- Tips initially fell behind ToolStrip's topmost menus. The tip is now raised
  after passive showing with `SWP_NOACTIVATE`. Capturing both surfaces exposed
  a second ownership error: a nested popup's decorative frame could rise over
  its own content. Frames now share their popup's immediate native owner.
  Checks verify tip/frame order and unchanged nested command-row pixels.
- Clicking/closing a popup raised first-chance COM `0x80040201` ("Element is no
  longer available"). UIAutomationCore reenters provider property/navigation/host
  callbacks during disconnection. The existing bridge now remains live until
  those calls finish, with a reentrancy guard; later stale-client calls still
  reject access. The existing popup lifecycle case failed before this repair
  and passed afterward. No new provider, pattern or accessibility acceptance
  scope was introduced.

Verification:

- Normal Debug/Release solution builds, XML and API analysis passed without
  warnings/errors. **65/65 Release unit/STA cases passed**. Three new essential
  cases cover metadata/fallback, pending cancellation/lifetime and bounded
  layout/renderer frames. The existing popup-lifecycle case additionally checks
  exception-free disconnect and stale-provider rejection. Synthetic frame and
  working-area coverage includes 96/120/144/192 DPI and odd/even bounds.
- Shown native Office checks confirmed layering and zero unavailable-element
  exceptions after the repairs. Fresh isolated Release package restore/build
  and Office/neutral ScreenTip scopes verify mouse/keyboard cancellation,
  disabled commands, intact full captions, native editor focus/selection,
  menus/collapsed groups, row-pixel retention, command clicks, nested Escape,
  deactivation, removal/disposal and work-area bounds. These are bounded
  structural checks at actual **120 DPI (125%)**, not the full native/UIA suite.
  Both package scopes recorded zero unavailable-element exceptions. The existing
  external package popup-dismissal check also passed ordinary/nested window
  switching and focus preservation after the bridge/frame fixes.
- Actual Office/neutral popup/tip captures were inspected. Early captures/checks
  missed overlap and were superseded by the stacking/pixel assertions; some
  earlier hover attempts stopped before completion, with input cancellation
  observed in one diagnostic run. They are not counted as final passes.
  The normal Debug Showcase is rebuilt for user review. Actual VS authoring,
  other physical scales/monitor movement and human acceptance remain pending.

Current evidence: `TestResults/screentips-{debug,release,tests,package-final}.log`,
`TestResults/visual/m2e-final-package-screentips{,-flat}/` and
`TestResults/screentips-consumer-path.txt`. Failing stacking/disconnect
reproductions remain in `visual/screentips-zorder-before/` and
`screentips-disconnect-before.log`; focused repaired evidence is in
`screentips-disconnect-focused.log` and `visual/screentips-layer-fixed/`.

### Ribbon body padding and downward height — 2026-10-27

The user approved a small spacing refinement using the WPF body as a visual
reference. The shared native body now grows six logical pixels downward; tab
geometry, its top edge and outer margins retain their anchors. `BodyHeight`'s
default minimum increases from 104 to 110, and the font-dependent three-row
minimum also gains six. Central layout insets increase from 4/4 to 6/8 logical
pixels above/below commands, giving two more above and four more between commands
and group captions. Large command/icon, compact-row and editor sizes are retained
at the default layout; captions follow the lower body edge. Explicit taller theme
minimums retain their meaning. The later theme inventory should include these
shared inner insets; no new public API or comprehensive theming work was added.

Collapsed group openers use the same 6/8 inset for painting, focus, hit testing
and popup anchoring. Body scrollers follow the taller body with their existing
two-pixel clearance. Group flyouts retain their own compact padding and command
sizes; native editors keep their field metrics and selection behavior.

Normal Debug/Release builds and API/XML checks passed without warnings/errors.
**18 focused Release cases passed across two runs**: 17 existing alignment,
caption, option, editor, popup, renderer and scroll/layout cases passed in the
initial selection; the new body-geometry case then passed separately. It verifies
retained sizes and added clearance at synthetic 96/120/144/192 DPI and two font
sizes, plus fixed top/tab anchors, collapsed bounds and flyout sizing. Its first
clearance assertion assumed independently rounded pixel edges would reproduce an
absolute inset; it now compares the added clearance against the prior geometry
at the same DPI. The full 66-case suite was not repeated for this bounded change.

Fresh isolated Release package restore/build and shown Office/neutral split/body
scopes passed at actual **120 DPI (125%)**, including Large-caption retention,
collapsed/nested popups, scroller geometry/frame/spacing, gap clicks and scrolling.
The package native-editor scope also passed focus, selection, binding, resize and
popup ownership. Debug Showcase before/after captures at 1080/720/420/260 widths
and package Office/neutral captures were inspected. The ordinary Debug Showcase
is rebuilt. The user accepted the body-spacing result and requested the separator
follow-up below. Additional physical DPI/monitor/VS gates remain pending. WPF
runtime/build/CI and the prior ScreenTip work are preserved.

Evidence: `TestResults/body-spacing-{focused,geometry,debug,release,package}.log`,
`TestResults/body-spacing-{before,after}/`,
`TestResults/visual/body-spacing-package-{split,split-flat,editors}/` and
`TestResults/body-spacing-consumer-path.txt`.

### Group separator aligned with the WPF reference — 2026-10-27

The user requested the WPF reference's separator treatment for the native ribbon.
Its shared group template uses ten logical pixels of vertical separator margin
inside a two-pixel groups-row inset. Native Office now uses that combined
12-logical-pixel clearance from each body edge, replacing six fixed device pixels.
The existing light `Border` color and one-device-pixel hairline are retained;
integer rectangle painting makes both endpoint gaps exact. Group widths, command
geometry, body height, collapsed buttons and popup separators retain their prior
contracts. Neutral keeps its separator-free presentation. The vertical inset and
separate separator color role belong in the deferred theming inventory; no public
API or WPF runtime changes were made for this follow-up.

**4/4 focused Release cases passed**, including the extended Office/neutral
renderer regression at synthetic 96/120/144/192 DPI and odd/even bounds. It checks
both endpoint gaps, a single-pixel stroke, theme color, collapsed absence and
neutral absence; the inset assertion failed before the change. No cases were
added. Normal Debug/Release/API/XML builds passed without warnings/errors.

The historical collapsed-absence contract is superseded by the
[2026-10-28 collapsed-divider correction](M4-STATUS.md#collapsed-group-right-separators--2026-10-28).

Fresh isolated package restore/build and Office/neutral split/body scopes passed
at actual **120 DPI (125%)**, including separator pixels, body geometry, captions,
collapsed/nested popups and scrollers. The first shown attempt stopped at its
foreground-ownership guard before sending input; both subsequent guarded scopes
completed. Debug Showcase wide/compact and package captures were inspected.
The normal Debug Showcase is rebuilt. The user accepted the separator appearance
on 2026-10-27 and authorized continuing the native plan. This is a bounded visual
refinement; full-suite, other physical DPI/monitor and VS acceptance were not run.

Evidence: `TestResults/group-separator-{before-test,focused,debug,release,package}.log`,
`TestResults/group-separator-showcase/`,
`TestResults/visual/group-separator-package-{split,split-flat}-review/` and
`TestResults/group-separator-consumer-path.txt`.

## M2F richer menus — 2026-10-27

The user accepted the group separator appearance and authorized continuing the
native component plan. The proposed bounded API was stated before implementation:
localizable `RibbonItem.MenuDescription`, dropdown-only `RibbonMenuSeparatorItem`,
and `RibbonRenderer.DrawMenuSeparator` / `DrawSubmenuArrow`. No public layout or
popup coordinator was added. Optional ICommand and minimize remain separate.

Menu commands can display a caption plus at most two wrapped description lines.
The complete string remains in the model; passive ScreenTip metadata is separate.
Native editor rows, strip commands and collapsed-group command slots keep their
existing compact metrics. Mixed menus share one measured target sequence for
painting, mouse input, keyboard navigation and popup anchoring. Decorative
separators have no activation, focus, KeyTips or ScreenTip; group collections
reject them before changing ownership. A dropdown-specific designer collection
editor offers the new item. The retained foundation bridge omits decorative rows;
this is structural filtering, not new accessibility compatibility or acceptance.

Nested command menus open beside their row and flip left at the screen edge.
Left/Escape closes one level and returns to the valid opener. Collapsed-group
dropdowns retain their downward opening. Scrolling a parent closes children
before their anchors move; existing model changes dismiss stale popup chains.
Shared text hooks receive descriptions in SecondaryText (GrayText when disabled),
and menu arrows use the shared vector chevron. Office and neutral use the same
geometry. Showcase Paste, Select and Options demonstrate the public capability.

The deferred theme inventory should include the local menu constants: 280-logical-
pixel minimum width when descriptions exist, five-pixel top/bottom text padding,
two-pixel description gap, nine-pixel separator row, eight-pixel separator inset
and work-area clearance, and four-pixel submenu row alignment. Hairlines and popup
frames remain one device pixel. Placement accounts for the frame; an initial
native check exposed a one-device-pixel overlap, corrected before packaging.

Normal Debug/Release builds, XML/public API checks and design assembly compilation
passed without warnings/errors. The full Release unit/STA run executed **69 cases:
68 passed and one new focus-dependent menu test failed**. That new test was replaced
with deterministic mixed-row/placement checks; keyboard and popup lifetime are
verified by the shown public consumer. The repaired case passed separately.
After the placement correction, **7/7 focused menu/popup cases passed**. The other
passed cases were not repeated. Synthetic geometry/rendering covers
96/120/144/192 DPI and two fonts; no clean final 69-case aggregate run is claimed.

A fresh Release package was restored into an empty cache outside the checkout.
Its public-only consumer built without Showcase resources, project references,
reflection or internal access. The first consumer compile caught a test-only
internal IsDisposed assertion; it was replaced with public collection membership.
Both Office and neutral shown menu scopes passed at actual **120 DPI (125%)**:
descriptions, hidden/disabled/decorative rows, native mouse activation,
Home/Down/Right/Left/Escape, nested radio exclusivity, separate split actions,
checkboxes, committed native field values, passive ScreenTips, execution once,
edge flipping, scrolling, source removal and outside dismissal. Captures of mixed
menus, both submenu directions, split choices, native editing, tip overlay and
overflow were inspected. This is independent-consumer evidence for M2F.

Earlier ScreenTip regression attempts exposed a foreground guard stop and
intermittent rapid pointer-hop timing. The fixture now waits for the target's
real MouseMove before starting its existing hover-delay check, resets over a
visible blank ribbon area rather than a potentially offscreen document, and
saves a failure screenshot plus the HWND under the pointer. Temporary runtime
tracing was removed; ScreenTip runtime behavior was not changed by this follow-up.
The test continues to check native ownership, selection, popup stacking and
first-chance unavailable-element exceptions. With that synchronization, both
Office and neutral package ScreenTip scopes passed at actual 120 DPI, including
nested/collapsed tips, frame ownership, click-to-close and zero first-chance
unavailable-element exceptions. The existing external package popup-dismissal
regression also passed, including switching to another process with a nested
stack open. These are retained-bridge/lifetime regressions, not new accessibility
acceptance. The ordinary Debug Showcase is rebuilt for human review.

Evidence: `tests/RibbonKit.WinForms.Tests/TestResults/m2f-{release,geometry-repaired,placement}.trx`,
`TestResults/visual/m2f-package-{menus,menus-flat}/` and
`TestResults/visual/m2f-package-screentips-{office-complete,flat-complete}/`,
`TestResults/m2f-package-dismissal.log` and `TestResults/m2f-consumer-path.txt`.
Outstanding human appearance, actual VS
designer round-trip, additional physical DPI/monitor and deferred accessibility
gates remain open. The next planned bounded component slice is **M2G minimize /
Ctrl+F1**; comprehensive theming and optional ICommand remain separate.

## M2G minimize/expand — 2026-10-27

The user authorized continuing the native plan. The bounded API was stated before
implementation: bindable/serializable `RibbonControl.IsMinimized` (false by
default), `IsMinimizedChanged`, and `RibbonRenderer.DrawMinimizeButton`. The
property participates in BeginUpdate/EndUpdate. No public popup coordinator,
new accessibility providers, host layout helper or animation was added.

Ctrl+F1, the tab-row chevron and tab double-click toggle the mode. Held Ctrl+F1
consumes repeats after one toggle and does not act in another Form. User gestures
commit the active editor and stop on validation cancellation; authoritative
property/binding changes retain the existing editor parking policy (commit valid
text, roll back an invalid draft). Document focus remains with the host when the
ribbon did not own it. Mode changes retain the selected tab and command state.

Only the tab row is docked while minimized. A tab click, Down/Enter/Space or its
KeyTip opens its actual groups in a temporary body below the row. A shared body
layout supplies expanded and temporary command geometry, reduction, collapsed
groups, scrollers and native editor slots. Models are neither cloned nor
reparented. Keyboard return and command dismissal reuse the popup stack; nested
menus and group flyouts remain above the tab session until individually closed.
Changing tabs replaces the temporary body. Source changes, disabling/removing the
selected tab, outside clicks, host movement/resize and deactivation close stale
sessions. Selection fallback now also closes the old body when a tab is disabled
without a layout change. Removing a model releases its native presentation;
parking/transferring one between docked and popup bodies preserves its instance,
caret, selection and value.

The utility owns an opaque 28-logical-pixel slot, preventing scrolling tab text
from showing beneath its chevron. Very narrow tab rows shrink the two tab
scrollers to retain up to 24 logical pixels of tab space. The temporary body has
a 200-logical-pixel minimum width when the work area permits; all these local
metrics belong in the deferred theme inventory. KeyTip matching now ignores
Shift for letters/digits while retaining Ctrl/Alt exclusions, so uppercase input
routes through the new tab session as well as existing command surfaces.

Normal Debug/Release solution builds, XML/public API checks and package creation
passed without warnings/errors. A full **72/72 native Release unit/STA checkpoint**
passed. Subsequent bounded fallback, batching and header refinements passed their
affected selections; the final **18/18 focused cases** include minimized state,
serialization, batching, shared body geometry, narrow reachability, tab/utility
separation and renderer boundaries. The suite was not repeated in full after
those bounded changes. Chevron direction/containment and opaque utility corners
cover synthetic 96/120/144/192 DPI and odd/even slots in both painters; body
geometry covers two fonts and several widths. GDI+ stroke antialiasing differs
under reflection, so glyph assertions test direction and containment rather than
requiring mirrored alpha pixels. Actual glyphs were inspected; the existing
rounded-frame symmetry checks retain their separate contract.

Fresh Release packages restored into empty caches outside the checkout, with no
Showcase resources, project references, reflection or internal-access grant.
The final Office/neutral minimize scopes passed at actual **120 DPI (125%)**:
two-way binding, focus, all three toggle gestures, held-key suppression,
tab/command KeyTips, nested Escape and execution once, same TextBox/ComboBox and
selection, validation/rollback, native choice commit, popup scrolling/collapse,
very narrow keyboard access, deactivation/other-Form isolation, source changes,
selection fallback and native-editor disposal. The independent compile caught a
test's internal paint-role flag; its recorder now identifies this fixture's tall
body-scroll slots through public measured geometry. Separate automated tab
gestures also respect Windows' double-click interval.

Office/neutral menu and ScreenTip regression scopes passed before the final
header-only refinements, including popup stacking and zero first-chance
unavailable-element exceptions. Existing external package keyboard and
popup-dismissal regressions passed. These retained-bridge checks do not establish
minimized-tab or utility accessibility compatibility. Expanded/minimized,
temporary/KeyTip bodies, nested menus, narrow/collapsed and very narrow captures
were inspected. The normal Debug Showcase is rebuilt; it receives the feature
directly from the library without new host glue.

Evidence: `tests/RibbonKit.WinForms.Tests/TestResults/m2g-{release,fallback,batching,final-rendering,tab-access}.trx`,
`TestResults/visual/m2g-complete-{minimize,minimize-flat}/`,
`TestResults/visual/m2g-final-{menus,menus-flat,screentips,screentips-flat}/`,
`TestResults/m2g-package-{keyboard,dismissal}.log` and
`TestResults/m2g-complete-consumer-path.txt`.

Human review, 2026-10-27: the user confirmed "it works now" for the demonstrated
minimize/expand behavior and authorized continuation. Actual VS round-trip and
other physical DPI/monitor gates remain open. The next roadmap milestone is
**M3 QAT/projections**; optional ICommand remains a separate proposal.
