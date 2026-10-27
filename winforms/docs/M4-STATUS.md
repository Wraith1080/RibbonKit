# M4 broader native surfaces

Status: **M4C Backstage implemented; human review pending**, 2026-10-27. Continuation follows the
accepted [M3E option icon presentation](M3-STATUS.md#m3e-option-references--2026-10-27).
M4 is a collection of bounded slices, not blanket implementation of every roadmap
feature. Existing accessibility, full theming, animation, touch/IME and optional
window/chrome deferrals remain in force. WPF runtime/build/CI remain unchanged.

## M4A contextual-tab contract

The public proposal was stated before implementation:

- Add `RibbonTab.IsContextual`, default false, and `ContextualColor`, default
  `Color.Empty` (fall back to the ribbon theme accent). Both have change events
  and ordinary WinForms binding/designer metadata. Color is a value, not owned.
- Use the existing `Visible` property and selection rules. The host decides when
  the context is applicable and whether to select its tab; setting IsContextual
  alone does not hide/show/select anything. No context manager or document coupling.
- Follow WPF's contextual header treatment: tinted label, subdued while unselected,
  full tint when selected; Office's existing underline uses the contextual tint.
  No additional top marker or contextual group/title-bar band. Neutral rendering
  uses the same text colors with its existing flat tab chrome.
- Keep tab measurements, hit targets, horizontal scrolling, keyboard/KeyTips,
  minimized layout and QAT ownership unchanged. Hidden/disabled/removed selected
  tabs use the existing deterministic fallback. Contextual flags are not a new
  persistence schema. Merge/modal services and contextual collections are separate.
- Keep appearance in shared renderers. The WPF Office 2024 unselected opacity
  of 0.6 is a provenance-backed component constant for the later theme inventory;
  do not start the deferred comprehensive theme API consolidation here.

Relevant WPF references: `RibbonTab.IsContextual` / `ContextualColor`, Office 2024
`ContextualUnselectedOpacity`, and
[the original contextual-tab record](../../docs/history/library/01-core-controls.md#34-contextual-tabs--custom-coloring-no-marker-line).

## Acceptance

The implementation uses the existing tab collection, selection, layout, popup and
QAT contracts. The six additive property/event API entries have XML documentation
and an Unshipped baseline. Showcase's Insert > Picture reveals and selects Picture
Format; Close picture tools clears that host-owned context. It uses public APIs.

Relevant verification only, 2026-10-27:

- Normal Debug solution build passed with zero warnings/errors, including API/XML
  checks and the separate designer. Three of the four selected tests passed in
  the initial run. The rendering case compared a named Color with equal ARGB pixels
  using Color identity; after correcting those pixel assertions, only that failed
  case was rerun and passed **1/1**. All four selected cases are covered across these
  runs, not one uninterrupted aggregate pass. Evidence:
  `tests/RibbonKit.WinForms.Tests/TestResults/m4a-contextual-{focused,rendering}.trx`.
- The three new cases cover defaults/designer metadata, two-way binding/events,
  no incidental visibility/selection/measurement changes, hidden/disabled/removed/
  disposed selected-tab fallback, contextual QAT source availability, and Office/
  neutral text/underline colors at synthetic 96/120/144/192 DPI. Rendering includes
  unselected/selected/hover/pressed/disabled states, no top marker, Color.Empty
  theme fallback, runtime accent changes and alpha compositing before GDI text.
  Inventory is 102; no new full-suite result is claimed.
- Shown public-only consumer passed Office and neutral scopes at **120 DPI**:
  host-bound visibility, pointer and multi-character KeyTips, original commands,
  inactive contextual QAT source, tint changes with an open menu, selected hide
  fallback, minimized flyout/context-loss teardown, theme fallback, narrow-strip
  reachability, disable/remove/reinsert/disposal and zero unavailable-element
  exceptions. Evidence:
  `TestResults/visual/m4a-contextual-{office-retry,neutral}/result.log`.
- Initial Office native scope lost foreground in its narrow-window capture after
  the earlier checks passed; the isolated retry passed with unchanged runtime and
  fixture. Its partial evidence remains under `m4a-contextual-office`; this is not
  represented as an uninterrupted initial pass or a diagnosed runtime focus bug.
- Office selected/unselected, theme-fallback and narrow-strip captures and neutral
  minimized capture were inspected. Debug Showcase is refreshed for local review.

The user accepted M4A's contextual-tab presentation on 2026-10-27 and authorized
continuation. Release/package, unrelated/full-suite,
actual Visual Studio round-trip, physical mixed-DPI/monitor, RTL and new
accessibility compatibility scopes were not run. No native touch/IME was added.
The same public fixture is linked into the native runner and available in the
independent package consumer; this slice ran the Debug project runner, not a new
package. The original foundation accessibility bridge remains unchanged.

## Next bounded slice

M4B implements the File command-menu foundation below. The working decomposition
for subsequent milestones is in the [remaining slices](#remaining-slices).

## M4B File command-menu contract

Proposed before implementation: `RibbonControl.FileMenu` is a stable owned
`RibbonDropDownItem`, serialized as content. It starts hidden, with Text "File"
and KeyTip "F", preserving existing consumers until they opt in. Its existing
Items, availability, ScreenTips, opening/closed events and bindings supply the
normal menu behavior. Models and native presenters belong to the ribbon;
application commands and borrowed icons stay application-owned.

The File header occupies a measured slot before TabRow QAT/tabs, including while
minimized; BelowRibbon QAT retains its own full row. Header paint uses a separate
`DrawFileButton` renderer hook. The existing menu stack supplies opening,
cancellation, nested commands, keyboard/KeyTips, Escape and native editors.
Opening File does not change the selected tab. Header geometry, hit testing and
popup anchoring share measured bounds, including narrow widths and larger fonts.
The File slot starts at the scaled `BodyMargin.Left`, aligning its button and
popup anchor with the body panel. This inset remains while minimized and respects
an authored zero or larger body margin. Hidden File reserves no leading space.

This first slice is a command dropdown. Full Backstage navigation/pages follow
in M4C. Office 2007's two-pane application menu/orb and optional window integration
remain later work; this slice does not claim those designs or hosted-page parity.
The existing menu's scrollbar remains unchanged (QAT scroller buttons are separate).

The two additive public API entries have XML/Unshipped coverage. The File menu
joins identity validation, binding-context propagation, native editor ownership,
designer associated components and disposal. Its header uses Text; command rows
retain normal icons and content. Direct disposal permanently releases this stable
declaration for the ribbon's lifetime. Hidden/disabled File retains the normal
availability rules, and disabling its open root closes the complete stack.
Showcase uses public APIs for New/Open/Save/Recent demonstration commands.

Relevant verification only, 2026-10-27:

- Normal Debug solution build passed with zero warnings/errors, including designer,
  API and XML checks. Five of six selected tests passed initially. The ownership
  case assumed an unattached Control had a nonnull BindingContext, although model
  components create a fallback context; after explicitly supplying the test's
  BindingContext, only that failed case was rerun and passed **1/1**. All six cases
  are covered across these runs; no clean aggregate rerun is claimed. Evidence:
  `tests/RibbonKit.WinForms.Tests/TestResults/m4b-file-{focused,ownership}.trx`.
- Four new cases cover stable content metadata, ownership/identity/borrowed-image
  lifetime, File/QAT/tab/minimize geometry with normal/large fonts and narrow/long
  captions, lazy/cancelled opening, KeyTips, nested teardown and shared renderer
  hover/pressed/focus/disabled states at synthetic 96/120/144/192 DPI. Existing QAT
  placement and contextual-source selection/lifetime cases are the other two
  selected regressions. Inventory is 106; no new full-suite result is claimed.
- Public-only shown Office and neutral consumers passed at **120 DPI**: cancelled
  opening, lazy population, pointer/KeyTips, original tab retention, native TextBox
  binding/commit and handle reuse, nested commands/Escape, disabled-root teardown,
  minimized File, narrow/BelowRibbon QAT geometry, hide/show and disposal, and zero
  unavailable-element exceptions. Office root/nested/narrow captures and neutral
  minimized capture were inspected. Evidence:
  `TestResults/visual/m4b-file-{office,neutral}/result.log` and PNGs.
- The same fixture is linked into the native runner and available in the independent
  package consumer. This slice ran the Debug project runner, not a newly packed
  consumer. Debug Showcase is refreshed for local review.

Current human review is recorded in the left-inset follow-up below.
Release/package, unrelated/full-suite, actual VS, physical mixed-DPI/monitor, RTL
and new accessibility compatibility scopes were not run. No native touch/IME was
added. WPF runtime/build/CI remain unchanged. The M4C starting
[continuation prompt](NEXT-SLICE-PROMPT.md) retains its original scope; current
implementation and evidence follow below.

### File left-inset follow-up — 2026-10-27

The user requested left padding for Office 2024 so File remains aligned inside
the ribbon body. The measured header now consumes the existing body-left margin;
QAT/tab placement and popup anchoring follow that rectangle. Width budgeting
includes the inset and retains room for the minimize utility, tabs and QAT overflow.
No new public API or renderer-specific layout branch was added.

Normal Debug build passed with zero warnings/errors. Only the affected layout
case was rerun and passed **1/1**, covering zero/default/larger insets, normal/large
fonts, narrow/wide widths, both QAT placements and minimized/expanded state:
`tests/RibbonKit.WinForms.Tests/TestResults/m4b-file-left-inset.trx`.
The shown public-only Office and neutral File scopes both passed at **120 DPI**,
including new assertions for File/body alignment, QAT adjacency and popup anchoring.
Evidence: `TestResults/visual/m4b-file-inset-{office,neutral}/result.log`.
Office root and narrow/BelowRibbon captures were inspected; Debug Showcase is
refreshed for review. Previous results remain separate. Unrelated/full-suite,
Release/package and deferred native/VS/accessibility gates were not rerun.
The user accepted the File menu presentation, including this padding correction,
on 2026-10-27. This records visual review at the available scope; it does not close
the separate VS, hardware or accessibility gates.

## M4C native Backstage — 2026-10-27

The bounded API/ownership proposal was stated before runtime edits, after reading
WPF's Modern Backstage, command-versus-page selection, initial focus, Tab cycling
and Back/Escape contracts. WPF remains a behavioral reference without shared code,
hosting, templates or runtime dependencies.

- `RibbonControl.Backstage` is a stable, owned, content-serialized
  `RibbonBackstage`. Its Items own page and plain button models; page Controls
  are borrowed. `FileMenuMode` defaults to `CommandMenu`; the explicit Backstage
  value routes the same File header and ShowDropDown/CloseDropDown path. Existing
  FileMenu consumers retain their default behavior, metadata and commands.
- `RibbonBackstagePageItem.Content` accepts one initially unparented, non-top-level
  native Control per page. A persistent library slot keeps its parent across page
  changes/close/reopen, preserving HWNDs, values, bindings and selection. Content
  replacement, page removal and page/ribbon/Form disposal detach and restore
  Dock/Bounds/Visible without disposing application content. Applications must not
  move/dispose assigned Controls and retain eventual disposal responsibility.
- Bindable `SelectedPage`, FileMenuMode and IsBackstageOpen use ordinary events.
  Available page fallback is next, previous, none. Commands retain page selection
  and close before Click. File DropDownOpening cancellation precedes Backstage
  Opening; Opened/Closing/Closed and visibility events describe the new surface.
  File's IsDropDownOpen/DropDownClosed also follow its active Backstage route.
- The library overlays the ordinary Form's complete client area. It owns navigation,
  page hosting, scrollable rail, sizing, keyboard/KeyTips and focus restoration.
  Application documents stay in place; no host click handler, custom chrome or
  coordinator is required. Selected tab and minimized state are retained.
- Opening commits pending ribbon edits and closes menus/choice lists/ScreenTips.
  Covered ribbon menu/editor activation is blocked. Visible enabled native page
  validation can veto navigation, commands, requested close or mode switching;
  Closing can also cancel. Availability/lifetime teardown forces close. Escape
  closes a native page ComboBox list first, then Backstage. Tab cycles internally,
  arrows/Home/End move navigation focus, Enter/Space activate, F6 switches regions,
  Alt/F10 shows KeyTips, and covered Form default buttons/mnemonics are excluded.
- Thirty additive Unshipped entries cover the required model/state APIs, two
  renderer hooks and the rail color. XML/API checks pass. Designer association and
  a bounded page/button collection editor are included; borrowed Control references
  serialize normally. Rail/row/back metrics and state appearance constants remain
  part of M4I's planned inventory, not comprehensive theme consolidation.
- Showcase File demonstrates Info (bound name and retained notes), Print (native
  copies/paper and a local preview action), New and Save. The unrelated ordinary
  Form fixture uses public APIs only. Page content, icons, business actions,
  bindings, storage and disposal remain application-owned.

Relevant verification, 2026-10-27:

- Normal Debug native solution build passed with **zero warnings/errors**, including
  runtime API/XML and the separate designer. Six new STA cases cover ownership,
  metadata/binding, cancellation/routing, handle/selection reuse, validation,
  commands, availability/fallback/content replacement/visibility/disposal and shared
  rendering/geometry at synthetic 96/120/144/192 DPI. A separate focus case verifies
  programmatic page changes/removal keep focus inside the open surface and closed
  changes retain document focus. The existing File lifecycle
  regression is the seventh selected case. Initial recorded selection passed 4/5;
  its binding fixture needed CreateControl before binding a native Control. Only
  that failed fixture was rerun (1/1), then the added lifetime and focus cases each
  passed (1/1).
  These are separate focused results, not an aggregate or full-suite pass:
  `tests/RibbonKit.WinForms.Tests/TestResults/m4c-backstage-{focused,metadata,lifecycle,focus}.trx`.
- Shown public-only Office and neutral scopes passed at **120 DPI** after the final
  dialog-key/lifetime changes. They exercise cancelled File opening, pending ribbon
  validation, native page binding, pointer/KeyTips, page/handle/parent/text-selection
  reuse, page validation after moving navigation focus, native ComboBox first-Escape,
  Tab/Shift+Tab containment, covered Form default-button protection, resize with
  minimized/BelowRibbon geometry, Back/document focus return, popup exclusion,
  command execution once, disable/remove/disposal and zero unavailable-element
  exceptions. Earlier passing scopes are retained separately. Final evidence:
  `TestResults/visual/m4c-backstage-final-{office,neutral}/result.log` and PNGs.
- Inspected final Office Info, neutral Print and narrow captures, as well as earlier
  Office Info/Print/narrow captures. Shown Showcase Info and Print captures were
  also inspected: `TestResults/visual/m4c-backstage-showcase-shown/`. Its initial
  DrawToBitmap captures rendered covered document Controls over the overlay; they
  are not visual acceptance evidence. The dedicated capture now records shown
  screen pixels with a foreground guard. Debug Showcase is refreshed for review.

Visual follow-up, 2026-10-27: the user found the overall Backstage presentation
good and requested rounded ends on the selected navigation marker and circular
Back hover/pressed feedback. The shared renderer now uses mirrored rounded
coverage for the marker and Back fill/outline; keyboard Back focus also follows
the circular outline. Normal Debug solution build passed with zero warnings/errors.
Only the two new rendering cases ran (2/2), covering Office/neutral normal/hover/
pressed/focus states and symmetric edge coverage at odd/even sizes and synthetic
96/120/144/192 DPI: `tests/RibbonKit.WinForms.Tests/TestResults/m4c-backstage-chrome.trx`.
The shown public-only Office scope passed at 120 DPI, including Back press/cancel
and ordinary Back activation. Actual hover/pressed captures and the rendering
matrix were inspected: `TestResults/visual/m4c-backstage-rounded-office/` and
`TestResults/visual/m4c-backstage-chrome/`. Debug Showcase is refreshed; no unrelated
or full suite was rerun. The two corrections await human review.

Stroke/image follow-up, 2026-10-27: the user requested a slightly heavier Back
circle/arrow and a navigation image example. Back now targets a 1.5-logical-pixel
stroke (circle snapped to device pixels; arrow keeps fractional antialiased width
and rounded caps/joins). Info demonstrates a borrowed full-color `Image`; Print
and commands retain vector `Icon` examples. The application disposes its image.
Normal Debug solution build passed with zero warnings/errors; only the affected
Back rendering case ran and passed 1/1 across both renderers, odd/even sizes and
synthetic 96/120/144/192 DPI. Evidence:
`tests/RibbonKit.WinForms.Tests/TestResults/m4c-backstage-back-stroke.trx`.
Inspected the updated rendering matrix and shown Showcase Info/Print captures in
`TestResults/visual/m4c-backstage-image-stroke-showcase/`. The public-only shown
Office scope also passed at 120 DPI, including hover/pressed captures, Back
activation/cancel and the existing interaction checks:
`TestResults/visual/m4c-backstage-back-stroke-office/result.log`.
Debug Showcase is refreshed; earlier unrelated passes were retained.

### Designer navigation width — 2026-10-27

The user accepted the preceding stroke/image refinements and authorized navigation
width configuration, clarifying that this means **designer properties**, not a
runtime drag interaction. Current public properties on the owned Backstage model:

- `NavigationWidth : int?` defaults to null/Auto. Its property-grid converter shows
  an Auto choice and accepts Auto, a positive number, an empty value or Reset.
- `MinimumNavigationWidth` / `MaximumNavigationWidth` default to 160/320 logical
  pixels. Both accept values of at least 80; a larger minimum takes precedence,
  allowing designer properties to be assigned in either order.
- Values use 96-DPI logical pixels and normal binding/Changed events. The Backstage
  property expands in the property grid, with these settings in Layout. The nine
  additive Unshipped entries have XML documentation and API coverage.
- Auto starts at 200 and measures visible captions, icon space, padding and scroll
  clearance. Shared layout constrains Auto and explicit widths to leave page space;
  very narrow hosts share the remaining width. Temporary constraints never rewrite
  the authored property, so widening restores its preferred width. Page Controls
  retain their parent, handle, selection, focus, values and pending validation.
- No drag handle, end-user resizing policy, width-setting UI, storage service or
  persistence schema was added. Application settings remain application-owned.
  Nullable-width binding uses formatting enabled and DataSourceNullValue=null.

Relevant final verification:

- Normal Debug solution build passed with zero warnings/errors, including API/XML
  and design. The final property-only selection passed **4/4**: three new property,
  binding/metadata/reset, DPI/Auto/constraints and native-state cases plus the
  affected existing Backstage geometry case. Synthetic DPI: 96/120/144/192. Evidence:
  `tests/RibbonKit.WinForms.Tests/TestResults/m4c-backstage-width-properties.trx`.
- Shown public-only Office and neutral width scopes both passed at **120 DPI**:
  explicit width/binding, maximum and narrow-window clamping without rewriting the
  preference, wide restoration, live Auto caption measurement, pending native edits,
  focus/parent/handle/text-selection retention and the existing Backstage interaction
  contract. Final property-only evidence:
  `TestResults/visual/m4c-backstage-width-properties-{office,neutral}/result.log`.
  Office explicit/long-caption Auto and neutral narrow captures were inspected.
- Debug Showcase is refreshed and continues to consume the public properties.
  Actual VS property-grid editing/code round-trip, Release/package, physical
  mixed-DPI/monitor, RTL and accessibility acceptance remain separate. No WPF build
  or tests ran for the documentation-only future item.

The corresponding WPF idea is deferred in
[the future improvements plan](../../docs/22-FUTURE-WPF-IMPROVEMENTS.md#backstage-navigation-width-in-designer-properties),
with a short pointer in the design index. WPF runtime/build/CI remain unchanged.
Human review of the width settings remains pending. The public fixture is linked into the project runner
and included in the independent package consumer, but no fresh package/isolated
consumer campaign was run. Full/unrelated suites, Release, actual VS round-trip,
physical mixed-DPI/monitor, RTL and new accessibility compatibility remain unrun.
No native touch/IME, motion, legacy File designs, chrome or additional themes were
added. WPF runtime/build/CI remain unchanged. M4D galleries are the next planned
slice after review; they were not implemented in this session.

## Remaining slices

This is a working delivery order, not a promise that each row fits one change.
Each public API and ownership contract is proposed before its implementation.

| Slice | Remaining deliverable |
| --- | --- |
| M4B | File command-menu foundation (accepted at the recorded scope) |
| M4C | Backstage navigation, commands and native page Controls implemented; human review pending |
| M4D | Gallery model and dropdown tiles, selection, preview/cancel/commit |
| M4E | In-ribbon gallery, scrolling/expansion and QAT projection |
| M4F | Tab/group merge service, then modal scopes with deterministic restoration |
| M4G | Compact/Touch density geometry and input reachability |
| M4H | Built-in localization and RTL layout/navigation/editor-direction contracts |
| M4I | Deferred appearance inventory/consolidation, then additional themes/accents/gradients and File designs in bounded passes |
| M5A | Command catalog, stable IDs and customization draft/persistence/Apply/Cancel foundation |
| M5B | QAT customization UI, including groups only on its available list |
| M5C | Ribbon customization UI and structural changes using the same draft/storage contract |
| M5D | Simplified Ribbon layout, pinning/overflow and customization/storage designed together |
| M6A | Motion, interruption/reduced-motion handling and optional host synchronization |
| M6B | Optional RibbonForm/window integration and title-bar QAT using existing source models |
| M6C | Optional backdrop/chrome treatments and their native window/DPI behavior |

Cross-cutting follow-ups remain separate: optional ICommand support; deferred
accessibility compatibility; actual VS designer/package/independent-consumer
acceptance; physical mixed-monitor/DPI and release/performance gates. Run only
relevant checks during component work. Native touch/IME is not an automatic gate.
WPF improvement ideas remain deferred in their separate plan.
