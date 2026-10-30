# M4 broader native surfaces

Status: **M4E gallery buttons and M4F1 merging accepted at reviewed scope; M4F2 modal scopes, M4G density, M4I1 joined BelowRibbon QAT and M4J messages implemented, human review pending**, 2026-10-28. Continuation follows the
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

### Navigation text legibility — 2026-10-28

The user reported that native navigation labels looked smaller than WPF. Modern
WPF's BackstageTabItem uses FontSize 14; its 15-pixel override belongs to Classic
2007 and was not adopted. The native proposal preceded implementation:
`RibbonTheme.BackstageNavigationFontSize` is a positive finite logical-pixel
minimum, default 14 (10.5 points). It preserves a larger host font and the host's
font family/style. Only navigation entries inherit the owned cached derived font;
Back and native page content retain their existing fonts. The font is refreshed
for source-font/theme/DPI changes and disposed with the surface. Row height and
Auto rail width use this same font, including long labels and large host fonts.

Normal Debug and Release native solution builds passed with zero warnings/errors,
including the two additive theme accessor entries and XML documentation. Focused
width/chrome checks passed 5/5 in `m4c-backstage-typography-focused.trx`; its two new
typography cases initially omitted the visible File prerequisite. After fixing
the fixture, those cases passed 2/2 in `m4c-backstage-typography-repaired.trx`.
The final native Release unit/STA aggregate passed 164/164 in
`tests/RibbonKit.WinForms.Tests/TestResults/m4c-backstage-typography-release-full.trx`.
Coverage includes both renderers, Compact/Touch, 9/16-point host fonts, 14/22-pixel
minima, measured label fit, theme validation, native HWND/text/selection/focus and
borrowed page retention.

Public-only Office/neutral native interaction runs passed at actual 120 DPI in
`TestResults/visual/m4c-backstage-font-{office-final,neutral}/`, verifying the font
minimum through public paint contexts plus File routing, navigation, native page
bindings/validation, keyboard focus, close/reopen and disposal. The first Office
launch stopped at its foreground prerequisite; the retry completed. These runs
use project outputs, not a fresh package. Office/neutral Info captures were
inspected; Showcase captures are in `TestResults/visual/m4c-backstage-font-showcase/`.
The user accepted the navigation-font refinement on 2026-10-28. Actual VS and physical mixed-monitor checks remain
deferred. No WPF runtime/build/CI or unrelated theme geometry was changed.

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
The user accepted Backstage and its width settings on 2026-10-27 and authorized
continuation. Actual Visual Studio property editing/code round-trip is explicitly
deferred to the designer slice. This acceptance covers the reviewed native scope;
the separately listed hardware/package/accessibility gates remain open.
The public fixture is linked into the project runner
and included in the independent package consumer, but no fresh package/isolated
consumer campaign was run. Full/unrelated suites, Release, actual VS round-trip,
physical mixed-DPI/monitor, RTL and new accessibility compatibility remain unrun.
No native touch/IME, motion, legacy File designs, chrome or additional themes were
added. WPF runtime/build/CI remain unchanged. M4D galleries are the next planned
slice; its bounded foundation is recorded separately below.

## M4D dropdown gallery foundation — 2026-10-27

Proposed before implementation: a dropdown gallery owns native tile models and
source selection, borrows application artwork, and separates temporary preview
from committed selection. It reuses the ordinary popup stack. Application document
preview/rollback and command effects stay in the host. In-ribbon/QAT presentation
is a later slice; no WPF hosting or control/template implementation is shared.
The behavioral reference was WPF RibbonGallery's preview/cancel/selection contract
and the indexed native-container gallery records; no WPF checks ran.

Implemented public contract:

- `RibbonGalleryItem : RibbonDropDownItem` owns only `RibbonGalleryTile` entries.
  It inherits normal header, opening cancellation, closing, KeyTips and ScreenTips.
  Tiles use normal identity, binding context, availability and borrowed artwork.
  Reparenting/duplicate identity rules still apply. A bindable SelectedItem starts
  null and can reference only an owned tile. Removal/disposal clears it; hidden or
  disabled authored selection is retained but cannot be activated by the user.
- SelectedItemChanged reports source/binding changes. ItemPreview reports temporary
  pointer/keyboard targets without writing selection. ItemPreviewCancelled ends an
  active preview once. SelectionCommitted reports each accepted activation once,
  including reselecting the same tile. Commit ends preview without cancellation;
  programmatic selection does not synthesize a commit. Reentrant collection/source
  changes are revalidated before further events or presentation.
- GroupName supplies headings for consecutive visible tile groups. ColumnCount,
  TileWidth, TileHeight and MaxVisibleRows are bounded authored metrics. Shared
  DPI/font-aware geometry fits the screen, scrolls long contents and reveals an
  available selected tile on opening. Headings are noninteractive; hidden tiles
  consume no slots and disabled tiles are skipped by navigation/activation.
- Shared DrawGalleryTile/DrawGalleryHeading hooks paint both Office and neutral
  configurations. Images retain aspect ratio and existing disabled-image rendering;
  vector providers can draw host-owned rich previews. Paint does not update model
  state, preview or selection. Twenty-seven additive API entries have XML and
  Unshipped coverage. Designer tile/command collection editors are included.
- The popup stack retains editor validation, nested Escape and focus return. Grid
  arrows, Home/End, PageUp/PageDown, Enter/Space and tile KeyTips are supported.
  Outside dismissal, collection changes, source loss and disposal cancel preview.
  User wheel/scrollbar scrolling clears a stale hover preview; focus-driven scroll
  preserves keyboard preview. Stationary-pointer messages cannot replace it.
- Showcase Home → Galleries contains Styles and Text color. The ordinary Form
  fixture uses public APIs only. Direct gallery QAT projection is rejected until
  M4E's contract exists. A gallery can still be opened normally from a nested menu
  or an existing borrowed group presentation.

Relevant verification only:

- Normal Debug solution build passed with **zero warnings/errors**, including
  runtime API/XML and separate design. Five new focused cases cover ownership,
  reference binding/borrowed assets, preview/cancel/commit and reentrant mutation,
  grouped/font/DPI geometry, selected-row/keyboard/nested lifecycle, and image/state
  rendering. The affected ordinary-menu geometry case is the sixth distinct case.
  Initial selection passed 3/4; its remaining fixture tested a disallowed collection
  instead of a duplicate gallery owner. That corrected case passed 1/1. The new
  image/menu selection passed 2/2; after the wheel/scroll refinement, only the
  affected popup case was rerun and passed 1/1. These are separate focused results,
  not a full or uninterrupted aggregate pass. Evidence:
  `tests/RibbonKit.WinForms.Tests/TestResults/m4d-gallery-{focused,ownership,rendering-menu,scroll}.trx`.
- Public-only shown Office and neutral scopes passed at **120 DPI**, including
  cancelled opening, pending editor validation, selected-row retention, pointer and
  keyboard preview, disabled/hidden tiles, wheel cancellation, programmatic source
  updates, repeated and multi-character-KeyTip commits, outside dismissal, nested
  Escape, source disable/removal/disposal and zero unavailable-element exceptions.
  Real pointer departure after keyboard navigation is also covered, separately from
  stationary-pointer paint messages. Final evidence:
  `TestResults/visual/m4d-gallery-complete-{office,neutral}/result.log`.
- The fixture first needed to leave KeyTips through the normal document-focus
  return and wait for selected-row scrolling to paint before reading recorded
  pointer bounds. Early partial captures/logs remain separate. No runtime focus
  stealing workaround was added. Office selected-row/preview/nested captures,
  neutral last-row capture and Showcase Styles/Text color captures were inspected.
  Showcase evidence: `TestResults/visual/m4d-gallery-showcase/`.

### Office 2024 gallery heading treatment — 2026-10-27

The user found the gallery presentation good and requested section headings that
cannot be confused with tile labels, using WPF's shaded header as the reference.
They explicitly limited this treatment to Office 2024. RibbonOffice2024Renderer
now paints rounded heading bands with stronger text, using the paired
GalleryHeadingBackground/GalleryHeadingForeground theme settings. Defaults follow
WPF Office 2024's #F0F0F0 background / #201F1E foreground. The neutral renderer's
plain heading appearance is retained. Heading geometry, target membership and
selection/preview behavior are unchanged. The five additive theme/override API
symbols have XML and Unshipped coverage; no broad theme consolidation was started.

Normal Debug solution build passed with zero warnings/errors. Only the focused
heading rendering case ran (1/1), covering Office/neutral scoping, default/custom
contrasting palettes, strong text, noninteractive states, odd/even widths and
96/120/144/192 DPI: `tests/RibbonKit.WinForms.Tests/TestResults/m4d-gallery-headings-office2024.trx`.
Shown public-only Office gallery checks passed at 120 DPI:
`TestResults/visual/m4d-gallery-headings-office/result.log`.
Shown Showcase Styles and Text color captures were inspected in
`TestResults/visual/m4d-gallery-headings-showcase/`. Debug Showcase is refreshed;
unrelated/full, neutral shown, Release/package and deferred designer/hardware/
accessibility scopes were not rerun. WPF remains a reference only.

The user approved continuation after requesting the neutral heading follow-up
recorded below. Filtering controls and popup resizing remain later work. No
Release/package/full/unrelated suite, new accessibility providers, actual VS,
physical mixed-DPI/monitor, RTL, motion or native touch/IME acceptance is claimed.
Actual VS editing/round-trip remains explicitly deferred to the designer slice.
The independent fixture ran through the Debug project runner, not a fresh package.
WPF runtime/build/CI remain unchanged. No commit or push was made.

## M4E in-ribbon galleries and QAT projection — 2026-10-27

The user requested bold neutral section headings on a transparent background,
then continuation to the next slice. The neutral renderer now uses its existing
SecondaryText color with a bold inherited font and no heading fill; Office 2024
retains its themed rounded band. The focused heading rendering case passed 1/1
at 96/120/144/192 DPI, with odd/even widths and contrasting palettes:
`tests/RibbonKit.WinForms.Tests/TestResults/m4e-neutral-gallery-heading.trx`.

Proposed before implementation: opt into an in-ribbon gallery strip with authored
column count, realized-row scrolling and expansion to the existing popup. QAT
copies borrow the same source gallery and retain its selection, bindings and
preview/commit handlers. No WPF implementation or hosting is shared.

- `RibbonGalleryItem.IsInRibbon` defaults false; Large presentations show the
  single-row strip. The original Medium/Small dropdown reduction is superseded
  by the full-column Large fallback follow-up recorded below.
  `InRibbonColumnCount` defaults 3, range 1–12, independent of popup ColumnCount.
  TileWidth sets logical column width; ribbon body height sets strip height.
  Eleven additive property/event/enum/renderer symbols have XML/Unshipped coverage.
- One library-owned reusable presenter follows each source into group and
  minimized-tab popups. Parking/reparenting preserves its identity; popup teardown
  releases it before disposing its parent. Borrowed models and application assets
  are not cloned or disposed. Native editor ownership remains with the existing
  editor manager.
- Up/down and wheel browse whole rows; endpoints disable navigation. Arrows,
  Home/End and PageUp/PageDown preview; Enter/Space commit once. F4/Alt+Down or
  expand opens the existing gallery. Cancelling expansion retains the browsed
  row and returns focus. Source selection changes reveal the selected row.
  Internal preview ownership prevents a stale surface from cancelling another
  surface's active preview.
- `RibbonQuickAccessDropDownReference.Source` now accepts galleries. Direct,
  duplicate, inactive-tab and overflow copies use the original collection and
  events. QAT does not select the source tab or remove its visible strip.
  Removing a reference leaves the source alive.
- The user reviewed several utility layouts. The final requested layout uses
  **equal-height up/down/expand buttons filling the interior**, inset four logical
  pixels from the frame. Shared integer edges distribute at most one device pixel
  of rounding. Painting and hit testing use those same bounds; hover/press cannot
  paint across the frame. The expand glyph stays compact and centered.
- Showcase Home → Galleries demonstrates the Styles strip and Text color popup;
  its first two QAT entries borrow those galleries. Document preview, fonts,
  colors and application actions remain host-owned.

Relevant verification, separately recorded:

- Normal Debug solution build passed with zero warnings/errors. Focused gallery
  behavior/ownership checks passed 7/7, including five new M4E cases and two
  affected M4D cases: `tests/RibbonKit.WinForms.Tests/TestResults/m4e-gallery-focused.trx`.
  The latest equal-height/inset geometry and all-four-edge hover/pressed rendering
  cases passed 2/2 at 96/120/144/192 DPI and odd/even heights:
  `tests/RibbonKit.WinForms.Tests/TestResults/m4e-gallery-utility-equal.trx`.
- Public-only native Office strip/QAT checks passed at 120 DPI before utility
  refinements: `TestResults/visual/m4e-in-ribbon-office-pointer/result.log`.
  The compact-inset iteration passed separate Office/neutral strip scopes and the
  remaining neutral QAT scope in `m4e-strip-inset-{office,neutral}/result.log` and
  `m4e-qat-gallery-neutral/result.log` under `TestResults/visual/`. Coverage includes
  pointer/keyboard preview, exact-once commit, endpoints/reversals, expansion cancel,
  inactive-tab/overflow QAT, reusable strip/editor HWNDs, removal/disposal and zero
  unavailable-element exceptions. Early runs interrupted by desktop interaction
  are partial evidence, not library failures or completed runs. The final
  equal-height revision has focused geometry/rendering and Showcase capture
  evidence; a fresh uninterrupted native input run was not repeated for that
  geometry-only change.
- Office and neutral headers, strip/frame spacing, QAT borrowed-group rendering
  and Showcase gallery captures were inspected. Latest equal-height Showcase
  evidence: `TestResults/visual/m4e-gallery-showcase-equal/`.
  The shared public-only fixture is also included in the independent consumer;
  this turn ran the project runner, not a freshly packed consumer.

On 2026-10-28 the user explicitly confirmed the three equal-height gallery
buttons look okay; that adjustment is accepted at the reviewed scope. Actual
VS testing stays deferred to the designer slice. No full/Release/package campaign,
new accessibility providers, physical mixed-monitor/DPI, RTL or motion acceptance
is claimed. Comprehensive theming remains deferred. WPF runtime/build/CI are
unchanged; no commit or push was made.

## M4F1 tab/group merge foundation — 2026-10-28

The user requested continuation. M4F was explicitly bounded to tab/group merging
first, with modal scopes next. The public proposal preceded code: source-owned
tabs and group contributions; Merge/Unmerge on the host; presentation snapshots
separate from authored collections; stable ordering, selection fallback and QAT/
native-presenter parking. WPF §3.33 and docs/06-MERGE-AND-MODAL-PLAN.md supplied
behavioral reference only; no WPF runtime/build/CI changes or checks were made.

Implemented contract:

- `RibbonMergeSource : RibbonElement` owns Tabs and Groups declarations with an
  independent binding context. `RibbonGroupContribution` owns its Group and targets
  an authored host tab by exact stable TargetTabId. Empty/unmatched targets are
  skipped; matching host additions/removals and target changes reconcile normally.
- `RibbonControl.Merge` / `Unmerge` borrow a source for one host. Attachment checks
  every identity before mutation; another active host is rejected. There is no
  focus-driven activation or automatic QAT/File/Backstage/nested-source merging.
  The host's authored Tabs/Groups remain unchanged. GetPresentedTabs/Groups expose
  ordered snapshots; adaptive layout and existing accessibility traversal consume
  those snapshots. Model owners, commands, selection and bindings are not cloned.
- Tab order uses source Order; group order uses contribution Order. Host content
  is `(0, -1)`; contributions use `(order, first-merge sequence)` and declaration
  order. Negative/equal orders, multiple sources, repeated cycles and live order
  changes retain deterministic placement. Removing a source preserves a surviving
  selected host tab or uses the existing visible/enabled fallback.
- QAT entries park disabled on unmerge, retain original source state and revive
  on remerge. GetMergeSource identifies transient provenance even after a borrowed
  source is disposed, and resets when the QAT reference is reassigned. No M5
  serializer/customization UI is implemented or claimed by this slice.
- Native editor/gallery presenters park across unmerge/remerge on the same host,
  preserving object/HWND identity and source data. Retargeting after unmerge releases
  the old host's cache. Source disposal tears down its projections and owned models;
  host disposal releases its borrow without disposing the source. Cleanup publishes
  removal before preview-cancel callbacks, allowing source disposal without recursive
  unmerge. Host binding-context changes do not replace child source contexts.
- Twenty-two additive API symbols have XML/Unshipped coverage. Native designer
  declaration traversal and contribution collection metadata are included; actual
  VS editing/round-trip remains explicitly deferred. Showcase Merging → Chart context
  demonstrates a Chart Design tab, a Home contribution and parked/revived QAT entries.

Relevant verification:

- Normal Debug native solution build passed with zero warnings/errors, including
  API/XML and separate design compilation. Focused selection initially passed 6/7;
  its retention case exposed an unmerge preview-cleanup gap. That focused case passed
  1/1 after repair. Reentrant preview cancellation/disposal plus retention passed 2/2.
  The final provenance case passed 1/1 after the shown fixture exposed lost transient
  identity on source disposal. These are separate focused results, not a clean full
  aggregate. Evidence under `tests/RibbonKit.WinForms.Tests/TestResults/`:
  `m4f-merge-focused.trx`, `m4f-merge-native-retention.trx`,
  `m4f-merge-reentrant-removal.trx`, `m4f-merge-provenance.trx`.
- Adjacent contextual selection and QAT state/source-lifetime scopes passed 3/3, recorded
  separately in `m4f-merge-adjacent.trx`. Prior gallery checks were not rerun merely
  for repetition.
- Public-only shown Office and neutral consumers passed at actual 120 DPI:
  `TestResults/visual/m4f-merge-office-complete/result.log` and
  `TestResults/visual/m4f-merge-neutral/result.log`. Coverage includes real application
  activation, source-owned groups/commands, native field binding, focus stability,
  selected-source removal, parked/revived QAT, editor HWND reuse, inactive-tab command
  execution, gallery overflow commit, source disposal and zero unavailable-element
  exceptions. Host-group, native field and neutral parked-QAT captures were inspected.
  The fixture uses public APIs and is included in the independent consumer; this turn
  ran the project runner, not a fresh package.

The user confirmed merging works fine on 2026-10-28; M4F1 is accepted at that
reviewed scope. M4F2 modal entry/exit, exclusive presentation, File/minimize/
Backstage blocking, cancellation and deterministic restoration are next. No full or
Release/package campaign, new accessibility providers, physical mixed-monitor/DPI,
RTL or motion acceptance is claimed. Actual VS stays deferred to the designer slice.
No commit or push was made.

## M4F2 modal scopes — 2026-10-28

The user confirmed merging works and explicitly accepted the three equal-height
gallery buttons, then requested continuation. Proposed before implementation:
EnterModal/ExitModal, cancellable transitions, exclusive tab presentation with QAT
retained, shared close affordance, File/minimize/Backstage blocking and coherent
restoration. WPF §3.32 and the retained merge/modal plan supplied behavioral
reference only. Authored visibility is preserved through native presentation
filtering, avoiding temporary hidden values in future storage.

Implemented contract:

- RibbonTab adds IsModal (default false), CanClose (true), CloseButtonText (empty
  uses Close), binding/change metadata and XML comments. The host adds EnterModal,
  ExitModal, IsModal/ModalTab and Entering/Entered/Exiting/Exited events.
  RibbonModalEventArgs reports tab, reason, Cancel and IsForced. Twenty-eight
  additive API entries have Unshipped coverage; no shipped baseline was changed.
- The scope owns transition state outside adaptive reduction. A generic header
  action slot presents the themed close command instead of minimize. Its renderer
  hook, keyboard focus, preferred X KeyTip and ordinary button automation are shared
  library behavior. Header bounds remain disjoint at narrow widths/large fonts.
  CanClose false hides user close and blocks Escape; application exit remains
  cancellable and available. Nested/switching modes require explicit exit first.
- GetPresentedTabs projects only the active modal tab. Authored Tabs, Visible,
  source collections and File visibility remain unchanged. QAT commands remain
  available according to authored source availability; File references are blocked.
  Native and programmatic minimize/File/Backstage requests cannot latch a deferred
  value. Child lists/popups/KeyTips get Escape first; application-content Escape
  can exit the modal scope.
- Normal entry/exit respects native editor validation; entry respects an open
  Backstage's closing veto. Tab removal/unmerge, hiding, disabling or removal of
  IsModal eligibility forces TabUnavailable exit, ignoring cancellation/validation.
  Reentrant transitions and callbacks that remove an entering candidate are guarded.
- Exit restores prior valid selection or visible/enabled fallback, minimized state
  and available focus without taking focus from another window. KeyTip-driven entry
  retains prior application focus. Explicit restoration clears consumed fallback
  indices so a later ordinary selection loss does not reuse a stale source index.
  Visibility edits and sources merged during modal remain authored on exit.
- Showcase Merging → Print preview borrows a preview source, provides native Copies/
  Zoom fields and an application-owned preview page/print-request action, then
  unmerges it on exit. Document paint, content visibility and printing stay in the
  host. The existing gallery geometry was not changed.

Relevant verification, recorded separately:

- Normal Debug native solution build passed with zero warnings/errors, including
  runtime API/XML and design compilation. Six new focused modal cases passed 6/6:
  `tests/RibbonKit.WinForms.Tests/TestResults/m4f2-modal-focused.trx`. The changed
  restoration/validation cases passed 2/2 after the KeyTip-focus/fallback refinement:
  `m4f2-modal-restoration.trx`. The open-Backstage veto case passed 1/1 in
  `m4f2-modal-backstage.trx`. These are separate focused runs, not a full aggregate.
- Adjacent File header geometry, minimized-state contract, merge selection and
  Backstage routing passed 4/4: `m4f2-modal-adjacent.trx` in the same directory.
  Prior gallery and unrelated suites were not repeated for acceptance.
- Public-only native Office and neutral consumers passed at actual 120 DPI:
  `TestResults/visual/m4f2-modal-office/result.log` and
  `TestResults/visual/m4f2-modal-neutral-complete/result.log`. Coverage includes
  cancelled entry, real KeyTips, QAT execution, blocked File/minimize requests,
  popup-first Escape, native validation, close cancellation, focus/minimized-state
  restoration, CanClose policy, editor HWND reuse, merge/visibility edits during
  modal, forced unmerge exit and zero unavailable-element exceptions. An earlier
  neutral startup lost foreground before interactions and is partial evidence.
- Office/neutral modal headers and the actual Showcase preview were inspected.
  Showcase evidence: `TestResults/visual/m4f2-modal-showcase/print-preview.png`.
  The public-only fixture is included in the independent consumer; this turn used
  the project runner, not a freshly packed consumer.

Human review is pending. Actual VS testing remains deferred to the designer slice.
No full/Release/package campaign, new accessibility providers, physical mixed-DPI/
monitor, RTL or motion acceptance is claimed. WPF runtime/build/CI are unchanged.
No commit or push was made. The subsequent authorized continuation implements M4G
below; it does not establish acceptance of every modal gate.

### Header utility sizing follow-up — 2026-10-28

The user requested smaller modal-close and minimize buttons with vertical
centering and uniform top/right/bottom spacing. Both shared renderers now use
`RibbonTheme.HeaderButtonInset`, default four logical pixels, within the existing
header slots. Hover/pressed/focus visuals are inset and clipped; the modal close
glyph is slightly smaller. Original slot geometry, click targets, tab/overflow
spacing and text size remain unchanged. Minimize still clears its full opaque
slot before painting so overflowing tab text cannot bleed through. The metric is
nonnegative, DPI-scaled and safely clamped; zero restores full-slot visuals. Its
two additive API entries have XML/Unshipped coverage.

Normal Debug solution build passed with zero warnings/errors. The new margin/
paint-boundary case and affected minimize-direction/modal-header geometry cases
passed 3/3: `tests/RibbonKit.WinForms.Tests/TestResults/m4f2-header-buttons.trx`.
Coverage includes Office/neutral hover/pressed/focus, 96/120/144/192 DPI, odd/even
bounds, default/custom/zero/oversized insets and unchanged hit slots. Synthetic
captures and live Office Showcase hover captures were inspected:
`TestResults/visual/m4f2-header-buttons-{rendering,showcase}/`.
Actual Showcase entry/exit ran for capture; broader native input, full/package/
Release, designer and physical monitor scopes were not repeated for this visual
refinement. The user accepted the header-button size/spacing on 2026-10-28;
the gallery button adjustment is preserved. This confirms that visual refinement,
not every remaining modal acceptance gate.

## M4G Compact/Touch density — 2026-10-28

The user requested continuation after the header-button refinement. The public
proposal preceded implementation: per-ribbon Compact/Touch geometry, retained
native editors and source models, validation before switching, refreshed owned
surfaces and a host-owned selector. WPF's shared density/Touch records supplied
behavioral reference only; no WPF implementation was changed.

Implemented contract:

- `RibbonControl.Density` defaults to Compact, has Appearance/default/binding
  metadata and raises DensityChanged after an accepted change. RibbonDensity and
  RibbonPaintContext.Density complete eight additive XML/Unshipped API entries.
  Custom renderers receive density on every library-owned paint surface.
- Shared logical metrics enlarge command rows, tab/QAT targets, native field
  frames and chooser rows, split arrows, menus, gallery tiles/utility buttons,
  scroll buttons and Backstage navigation. Touch retains three rows per column,
  with 44-logical-pixel command rows, 44-pixel menu/choice rows, 64×80 minimum
  popup gallery tiles and a 48-pixel tab row. Small split/scroll utility parts
  remain narrower than full command targets. Authored font, theme, input widths,
  tile metrics and source ownership remain unchanged. Compact restores its prior
  geometry, including custom tab-height minima. Existing equal-height gallery
  utilities and their border inset are retained.
- Valid focused native edits commit first; invalid input blocks the change.
  Accepted changes close transient menus/lists, cancel gallery previews and
  KeyTips, clear layout caches and refresh Backstage. Retained native TextBox/
  ComboBox handles, binding, selection and focus survive where available. Restore
  editable ComboBox selection after native focus, which otherwise resets it.
  A parked QAT command uses the presenting host's density even when its source
  has no active ribbon. Merge/modal/minimized and open Backstage state survive.
- Showcase's third QAT entry and Merging → Input mode supply a Mouse/Touch
  selector through public APIs. The application owns selector state, device
  detection, preferences, document/page/dialog layout and persistence. No motion,
  automatic device switching or new accessibility provider is included.

Relevant verification, recorded separately:

- Normal Debug native solution build passed with zero warnings/errors, including
  runtime API/XML and design compilation. Four new focused facts were exercised:
  `tests/RibbonKit.WinForms.Tests/TestResults/m4g-density-focused.trx` initially
  passed 3/4 and exposed editable ComboBox caret loss. Its repaired case passed
  1/1 in `m4g-density-editor-retention.trx`. The popup case, extended for parked
  QAT sources, passed 1/1 in `m4g-density-parked-qat.trx`. These are separate runs,
  not a claimed clean four-case aggregate. Synthetic geometry covers repeated
  Compact/Touch switching at 96/120/144/192 DPI, native validation/retention,
  gallery preview cancellation, split/field rectangles, merging/modal/Backstage
  retention and narrow collapsed-command reachability.
- Affected existing Compact split geometry, native ComboBox refresh, QAT row
  measurement and rich-menu row/editor layout passed 5/5:
  `tests/RibbonKit.WinForms.Tests/TestResults/m4g-density-adjacent.trx`.
- Public-only Office and neutral native consumers passed at actual 120 DPI:
  `TestResults/visual/m4g-density-office-complete/result.log` and
  `TestResults/visual/m4g-density-neutral/result.log`. Real pointer/keyboard
  coverage includes invalid input, caret/focus/HWND/binding retention, three
  rows, chooser/menu sizing and command routing, QAT gallery/overflow commits,
  popup dismissal, lower QAT sizing, Backstage page retention, return to Compact
  and zero unavailable-element exceptions. The first Office fixture incorrectly
  required the native chooser to own foreground; its partial log is not a pass.
  The corrected fixture observes public renderer row geometry without that
  assumption. These passes preceded the bounded parked-QAT fix, which has its
  own focused result above.
- Actual Showcase Compact/Touch captures and Office lower-QAT/neutral Touch
  captures were inspected. Showcase evidence is under
  `TestResults/visual/m4g-density-showcase/`. The public-only fixture is included
  in the independent consumer; this turn ran the project runner, not a newly
  packed consumer.

Human density review remains pending. Actual VS testing stays deferred to the
designer slice. No full/Release/fresh-package campaign, physical mixed-monitor/DPI,
RTL, new accessibility or motion acceptance is claimed. Native touch/IME hardware
is not required by the user's testing direction. WPF runtime/build/CI are unchanged;
no commit or push was made. By user direction on 2026-10-28, M4H localization/RTL
is deferred until the remaining components, including customization, animation
and themes, exist so its cross-component pass covers the complete control set.
The requested joined Office 2024 BelowRibbon QAT frame remains in M4I's appearance
pass, as recorded in the main plan.

### Touch content alignment and vertical split ownership — 2026-10-28

Human review found that Touch retained Compact's fixed icon/caption offsets while
the vertical split's primary band grew from 44 to 68 logical pixels. That left its
caption inside the primary action and its arrow near the lower edge. The user
requested centered content with level neighboring buttons, then clarified that the
vertical split's label belongs to the lower menu action.

The first alignment follow-up used two bands: a centered icon in the upper image band
and a centered lower envelope reserving two caption rows, a four-logical-pixel gap
and a twelve-pixel indicator slot. First caption lines stay level for regular,
toggle, dropdown and horizontal/vertical split buttons. Vertical splits center the
label/indicator envelope within the lower action; both single- and two-line labels
stay there and the arrow sits below them, centered horizontally. The shared split
boundary uses the same font-aware band calculation as painting, giving taller
captions room without transferring them into the primary action. Medium/Small,
menu-row and Compact content placement are unchanged. No public API was added.

Normal Debug solution build passed with zero warnings/errors. Five affected
existing Compact caption/split-layout/joined-chrome cases passed in
`tests/RibbonKit.WinForms.Tests/TestResults/m4g-touch-alignment.trx`. That initial
run's two new cases incorrectly counted only exact foreground-colored arrow pixels,
excluding antialiased edges; after correcting the pixel observation, both new
Office/neutral cases passed 2/2 in `m4g-touch-alignment-rendering.trx`. Coverage spans
96/120/144/192 DPI, 9/14-point fonts, hover/pressed/focus, shared icon/baseline
alignment, caption ownership, split tiling, indicator placement and actual arrow
ink bounds. Rendering captures, including normal-font 125% and larger-font 200%,
were inspected under `TestResults/visual/m4g-touch-alignment-rendering/`.

The public-only density consumer now also clicks the primary icon, lower caption
and arrow independently and verifies exact-once primary/menu routing. Office and
neutral runs passed at actual 120 DPI, retaining the previous density/editor/QAT/
Backstage coverage:
`TestResults/visual/m4g-touch-alignment-{office,neutral}/result.log`. The fixture
uses public renderer observations and is linked into the independent consumer;
these runs used the project runner rather than a new package. Showcase wide/narrow
and separate primary/menu hover captures were inspected under
`TestResults/visual/m4g-touch-alignment-showcase/`. Actual VS, full/Release/package
and physical mixed-monitor scopes were not repeated; human review remains pending.

### Button proportions and illustrated density chooser — 2026-10-28

Further human review requested a smaller ordinary icon/caption gap and a larger
vertical-split primary action, using the WPF screenshot as a reference. The user
also requested its illustrated Mouse/Touch chooser. This supersedes the previous
follow-up's shared caption-band geometry while preserving split label ownership.

- Ordinary Large Touch buttons now place captions eight logical pixels beneath
  their icon slots. Regular, toggle, dropdown and horizontal split first lines
  remain aligned; their icons align with vertical splits. The nominal vertical
  primary grows from 68 to 90 logical pixels, clamped by the two-row/font clearance
  required below it. Its lower action centers the actual one- or two-line caption,
  four-pixel gap and twelve-pixel indicator as a group. The caption remains a menu
  action. Compact, Medium/Small and standard menu geometry are preserved.
- The reusable dropdown proposal preceded implementation: optional DropDownHeader,
  MenuPresentation.Standard/IllustratedChoices and DrawMenuHeader. These ten
  additive API entries have XML/Unshipped coverage. A header occupies measured
  space without becoming a command, focus stop, KeyTip or hit target. The shared
  header hook uses existing theme roles: rounded Office 2024 fill and neutral
  transparent bold text. Rows use larger artwork, bold titles, existing two-line
  MenuDescription and Checked/Hover/Pressed fills; no separate control or provider
  was introduced. Native editors, separators and submenus stay standard; gallery
  layouts retain their own headings. A QAT dropdown reference shares the source
  heading/rows; an individually projected command keeps standard QAT row sizing.
- Showcase's existing selector now uses those public APIs, application-owned
  vector mouse/hand artwork, the requested heading/descriptions and the current
  mode's toolbar icon. Radio selection is reconciled with the accepted Density
  after both programmatic changes and click handlers, preserving validation veto
  behavior. The application continues to own mode detection/preferences/storage.

Verification:

- Normal Debug native solution build passed with zero warnings/errors, including
  API/XML and design compilation. Focused proportion, choice/header, existing
  command/File/QAT menu and Compact-caption checks passed 17/17:
  `tests/RibbonKit.WinForms.Tests/TestResults/m4g-button-proportions-choices.trx`.
  Updated rendering cases cover both renderers, 96/120/144/192 DPI, 9/14-point
  fonts and hover/pressed/focus, checking close ordinary captions, larger primary
  geometry, split caption/arrow ownership, selected fills and bold/normal text.
- The individual-QAT row-sizing refinement was added after those checks and the
  native runs below. Extended choice/header and affected QAT-menu cases passed
  7/7 in `m4g-illustrated-qat-scope.trx`. This keeps the illustrated layout scoped
  to the presenting source menu, including whole-dropdown QAT references.
- Office and neutral public-only native runs passed at actual 120 DPI:
  `TestResults/visual/m4g-proportions-choice-{office,neutral}/result.log`.
  In addition to retained density/editor/gallery/Backstage and split input checks,
  they verify QAT illustrated rows, a noninteractive header, mouse selection of
  Touch, checked-state synchronization, keyboard return to Mouse and zero
  unavailable-element exceptions. The independently included fixture uses its
  own artwork and public APIs; this turn used the project runner, not a new pack.
- Ordinary/split Showcase hover, both selected chooser states, and larger-font
  200% rendering captures were inspected. Evidence is in
  `TestResults/visual/m4g-proportions-choice-showcase-final/` and
  `TestResults/visual/m4g-button-proportions-rendering/`. The final chooser captures
  use pointer opening; earlier keyboard-open captures correctly included KeyTips.

Human review remains pending; actual VS stays deferred to the designer slice.
No full/Release/fresh-package or physical mixed-monitor campaign was run. WPF
runtime/build/CI are unchanged, and no commit or push was made.

### In-ribbon gallery reduction retains a Large column — 2026-10-28

Human review found that a reduced in-ribbon gallery became a Medium/Small command
despite its full-column presentation. The user requested a Large dropdown instead.
The shared layout now treats a non-Large SizePolicy entry on IsInRibbon galleries
as a Large dropdown fallback, with ordinary Large icon/caption/arrow drawing and a
narrower width than the tile strip. It finishes neighboring row columns before
and after the gallery, so the fallback remains full-height and exclusive. Large
policy entries still request the strip; authored SizePolicy/IsInRibbon values and
source tile ownership are unchanged. Dropdown-only galleries and QAT entries keep
their normal sizing. Group reduction mode still determines whether resizing is
attempted before whole-group collapse.

The proposed strip/dropdown presentation distinction is carried through measured
items, root/minimized/group-popup targets and strip parking. The retained strip is
hidden during fallback instead of overlaying the Large dropdown, then reused when
space returns. RibbonPaintContext.IsGalleryDropDown exposes the measured fallback
to custom renderers; its two additive XML/Unshipped entries were build-checked.
Popup tiles, preview/cancel/commit and selection continue to use the same source.

Normal Debug native solution build passed with zero warnings/errors. Geometry at
96/120/144/192 DPI in Compact/Touch, explicit/adaptive policy entries, exclusive
columns, ordinary-gallery sizing and adjacent reduction/strip/borrowed-group cases
passed 9/10 in `tests/RibbonKit.WinForms.Tests/TestResults/m4g-gallery-large-fallback.trx`.
The resize fixture initially used the default Collapse-only group policy; after
opting it into ResizeThenCollapse, that case passed 1/1 in
`m4g-gallery-large-fallback-resize.trx`. It verifies root and minimized-popup paint
state, source popup routing, exact-once selection, parking and strip HWND reuse.
These are separate focused results, not a claimed clean aggregate.

Public-only Office and neutral gallery-reduction scopes passed at actual 120 DPI
for both Compact and Touch:
`TestResults/visual/m4g-gallery-large-fallback-{office,neutral}/result.log`.
Pointer opening, keyboard commit, full-height geometry, authored-state retention,
restoration of the same strip/selected tile and zero unavailable-element exceptions
were verified. The fixture is shared with the independent consumer and uses its
own artwork; this turn used the project runner, not a fresh package. Office Touch
fallback and actual Showcase narrow Touch captures were inspected; Showcase
evidence is under `TestResults/visual/m4g-gallery-large-fallback-showcase/`.

Human review remains pending. Actual VS remains deferred, and no full/Release/
fresh-package or physical mixed-monitor campaign was repeated. WPF runtime/build/
CI are unchanged; no commit or push was made.

### Collapsed-group right separators — 2026-10-28

The user reported missing right-side dividers on collapsed groups. The Office
renderer explicitly skipped that state. Expanded and collapsed Office groups now
paint the existing Border-colored, one-device-pixel divider at the full group
frame's right edge, with the same 12-logical-pixel top/bottom clearance. Layout
retains the full frame separately from the inset collapsed opener, so hover/focus
visuals and click bounds are preserved. Root and minimized-body popup painting
carry and scroll that frame together. Neutral retains its existing appearance;
no public API or new theme role was added.

Normal Debug native solution build passed with zero warnings/errors. Five focused
renderer/body cases passed in
`tests/RibbonKit.WinForms.Tests/TestResults/m4g-collapsed-group-separators.trx`.
The updated renderer contract covers expanded/collapsed and Office/neutral states,
normal/hover/pressed, 96/120/144/192 DPI and odd/even bounds. The new mixed-group
case verifies Compact/Touch root and minimized-popup endpoint alignment, actual
divider pixels and retained opener hit bounds. The previous M2 collapsed-absence
assertion is intentionally replaced by this requested behavior.

The actual 120-DPI Showcase capture was inspected:
`TestResults/visual/m4g-collapsed-group-separators-showcase/Touch-collapsed.png`.
Editing and Galleries both retain dividers aligned with expanded groups. This is
rendering/capture evidence; native input, a fresh independent package, full/Release,
designer and physical mixed-monitor scopes were not repeated for this paint fix.
Human review remains pending. WPF runtime/build/CI are unchanged; no commit or push
was made.

### Command-popup spacing, uniform rows, headers and host close — 2026-10-28

The user requested larger command dropdowns in Touch, equal item heights following
the tallest row, contextual headings and repair of Alt+F4 leaving empty frames.
They clarified that Alt+F4 should close the host Form, then identified vertically
misaligned Touch ComboBox text during review. All changes remain in shared native
layout/input/editor behavior; Showcase supplies the menu wording.

- Command menus and QAT overflow normalize visible interactive rows to the tallest
  measured row in each popup, including disabled items and native fields. Headers
  and separators retain their own sizes; hidden items do not contribute. Group/
  minimized-body columns and gallery tile layouts retain their own measurement.
- Standard Touch menu minimum widths are 220 logical pixels, or 320 for described
  menus, versus Compact's 170/280. Standard described Touch rows use ten-pixel
  vertical padding and a four-pixel description gap, reserving two description
  lines. Touch icon/arrow slots enlarge without changing fonts. Native frames
  remain centered inside uniform rows. Illustrated-choice descriptions retain
  their established card geometry. These named density values remain candidates
  for the deferred theme consolidation.
- Showcase supplies Paste options, Selection options, Track changes, Choose a shape,
  Input options and other contextual headings through the existing DropDownHeader
  API. The existing Office rounded fill and neutral transparent/bold treatment are
  reused. No new public API or theme color role was added.
- Alt+F4 reproduced the user's blank-frame defect before the fix:
  `TestResults/visual/m4g-popup-alt-f4-reproduction/`. The session and outer wrapper
  stayed open while native default close handling hid its hosted content. The key
  now cancels native choice previews, closes the entire owned popup chain normally,
  and requests the containing Form's close. FormClosing cancellation is honored;
  typed fields and native HWNDs survive reopening. Message ownership limits the
  handler to this host, and repeat keydowns do not issue duplicate close requests.
  Escape keeps its existing one-level behavior. Earlier exploratory popup-only
  dismissal evidence is superseded by the user's host-close clarification.
- The ComboBox regression measured a five-pixel Touch text offset in the STA case:
  owner-drawn selection height exceeded PreferredHeight, which was still used for
  positioning. Presentation now centers the actual native height in the painted
  field without requesting a new height. Both editable and selection-only text
  align with the frame/arrow in strip and popup presentations; handles, values,
  selection and paint/position stability remain covered.

Relevant verification:

- Normal Debug native solution build passed with zero warnings/errors, including
  API/XML and design compilation. The focused popup/menu/choice/overflow run passed
  11/12 in `tests/RibbonKit.WinForms.Tests/TestResults/m4g-popup-refinements-focused.trx`.
  Its remaining assertion required different command/editor row heights; after
  updating that obsolete contract, the case passed 1/1 in
  `m4g-popup-uniform-row-contract.trx`. New coverage includes 96/120/144/192-DPI
  menu geometry, 9/14-point fonts, uniform targets, thin separators, Touch growth,
  native field slots and cancellable host close/repeat handling.
- The ComboBox alignment regression failed before the positioning fix in
  `m4g-touch-combo-alignment-before.trx`. The repaired geometry case plus affected
  native editing/density retention cases passed 5/5 in
  `m4g-touch-combo-alignment.trx`. Both native styles, font changes, Compact/Touch
  switching, root/borrowed-group presentation, actual edit-child rectangles and
  text-region centering within one device pixel are covered.
- Final public-only Office and neutral native runs passed at actual 120 DPI:
  `TestResults/visual/m4g-popup-combo-{office,neutral}/result.log`. They verify wider/
  taller Touch menus, uniform rows, editable/selection-only ComboBox centering,
  exactly-once host closing from command/nested menus, native fields/choosers,
  QAT choosers/overflow, borrowed groups, galleries and temporary minimized bodies.
  Cancellation preserves typed text, selection and native HWNDs; gallery preview
  genuinely starts and is cancelled without commit. A permitted final close exits
  the host. Unavailable-element exceptions remain zero. An earlier fixture used a
  stale fourth QAT rectangle after Touch overflowed it; the final fixture reserves
  sufficient toolbar width and asserts the intended gallery actually opened.
- Existing rich-menu Office/neutral native scopes passed separately in
  `TestResults/visual/m4g-uniform-menu-{office,neutral}/result.log`, covering nested
  placement/flipping, scrolling, separators, keyboard, activation and retained native
  fields. These passes preceded only the ComboBox-position refinement, which has
  the dedicated evidence above. Public-only fixtures are included in the independent
  consumer; these runs used project outputs, not a fresh package.
- Actual Showcase Compact/Touch Paste, Select, Track changes and native Input
  options captures were inspected under
  `TestResults/visual/m4g-command-menu-showcase-final/`; final native ComboBox
  captures were also inspected. A collapsed Editing group is opened normally by
  the capture helper before reaching its commands. Earlier foreground-startup and
  capture-helper interruptions are not completed acceptance runs.

These are separate focused results, not a full aggregate. Human review is pending;
actual VS remains deferred. No full/Release/fresh-package or physical mixed-monitor
campaign was run. WPF runtime/build/CI remain unchanged; no commit or push was made.

### Wider Touch horizontal split arrow — 2026-10-28

Horizontal ribbon/group splits now reserve 44 logical pixels for the Touch arrow
instead of 28. Measurement widens the whole command, preserving the previous
Large caption budget and a primary minimum of 72 logical pixels for Large or 56
for Medium/Small. The arrow no longer stops at one third of a narrow command;
constrained geometry still keeps the primary larger. Reduced vertical sources
follow the same horizontal contract. Compact, Large vertical splits, command-menu
and QAT utility splits retain their previous geometry. No public API was added.

Normal Debug native solution build passed with zero warnings/errors. The focused
geometry/alignment/seam/density selection passed 8/8 in
`tests/RibbonKit.WinForms.Tests/TestResults/m4g-touch-horizontal-split.trx`, including
96/120/144/192 DPI, 9/14-point fonts, authored width limits, preserved caption
space and Compact return. Public-only Office/neutral native runs passed at actual
120 DPI under `TestResults/visual/m4g-touch-horizontal-split-{office,neutral}/`:
Large/Medium/Small arrow widths, separate mouse/keyboard actions and Compact
restoration. The first neutral launch failed its foreground prerequisite; its
retry completed. These runs use project outputs, not a fresh package.

Office/neutral hover and pressed captures and the actual Showcase
`TestResults/visual/m4g-touch-horizontal-split-showcase/Touch-horizontal-split.png`
were inspected. The capture temporarily changes Paste's orientation and restores
it; the normal sample retains its authored layout. The user accepted this wider
Touch horizontal split-arrow adjustment on 2026-10-28. Actual VS and physical
mixed-monitor checks remain deferred. No full/Release or WPF checks were run for
this bounded change.

## M4I1 Office 2024 joined BelowRibbon QAT — 2026-10-28

The bounded renderer/layout proposal preceded implementation: Office 2024 joins
the lower QAT to the body, neutral retains its separate row, and minimized Office
uses a standalone card. Existing Surface, Border, CornerRadius, BodyShadowColor
and BodyMargin supply the appearance; no palette or independent-radius refactor
was started. Command ownership, models and authored placement remain unchanged.

- `RibbonRenderer.JoinBelowRibbonQuickAccess` defaults false; Office overrides
  it to true. Shared measurement moves the row inside the body side margins,
  directly beneath the command body. `DrawBody` receives the combined outer frame;
  `RibbonPaintContext.IsQuickAccessToolbarJoined` tells the QAT callback to add
  only its single-device-pixel internal separator. One outer rounded frame/shadow
  replaces the intermediate body bottom border, gap and shadow. Expanded height
  and group/editor/hit geometry stay fixed; QAT anchors follow its new row.
- Minimized Office keeps the QAT available as an all-corner rounded card, with
  theme-provided top/bottom margins included in height. Neutral and Office-derived
  renderers opting out retain the original full-width strip. Setting/reassigning
  Renderer now refreshes measurement, including a mutable policy change. Five
  additive override/property/accessor entries have XML/API-baseline coverage.
- Message-panel support is recorded separately as M4J by user request. Its open
  panel must square the adjoining QAT bottom corners and coordinate margins/frame/
  shadow geometry. This change implements no message-panel runtime API.

Verification:

- Final normal Debug and Release native solution builds passed without warnings
  or errors. Focused Debug geometry/File/QAT checks passed 5/5 in
  `tests/RibbonKit.WinForms.Tests/TestResults/m4i-qat-frame-focused.trx`.
  The native unit/STA Release aggregate passed 162/162 in
  `m4i-qat-frame-release-full.trx`, including the final same-renderer reassignment
  regression. New pixel checks cover 96/120/144/192 DPI, odd/even bounds, four-corner
  symmetry, a one-device-pixel join and preservation of the single outer shadow.
- Public-only Office and neutral native QAT runs passed at actual 120 DPI under
  `TestResults/visual/m4i-qat-frame-{office,neutral}-complete/`: Compact/Touch expanded
  and minimized geometry, pointer/source binding and retention, disabled/removed
  declarations, KeyTips, overflow/wheel/repeat/keyboard, narrow reachability and
  focus return. These use project outputs, not a fresh package. Early runs stopped
  at ScreenTip/foreground prerequisites; the fixture now waits for activation and
  starts hover from a known point. A later fixture attempt reused a Touch rectangle
  after returning to Compact; refreshing recorded paint targets fixed the probe.
  Successful native runs preceded only the same-renderer reassignment refinement,
  which the final Release aggregate covers.
- Actual Office/neutral expanded and minimized captures were inspected, together
  with Showcase Compact expanded and Touch minimized captures in
  `TestResults/visual/m4i-qat-frame-showcase/`. The capture helper exercises the
  normal public placement/density APIs and restores its temporary sample state.

Human review remains pending. Actual VS, fresh-package, physical mixed-monitor
and broader appearance/palette acceptance remain deferred. No WPF runtime/build/CI
changes, commit or push were made. Localization/RTL remains after the remaining
components, including customization, animation and themes.

### Equal group side padding — 2026-10-28

The user reported unequal side clearance around expanded commands and collapsed
group openers, requesting the right side match the left. Measurement now removes
the final inter-column gap and applies the existing six-logical-pixel inset to
both ends of the content block. Caption-driven extra width is shared equally,
including odd device-pixel bounds. A separate one-device-pixel divider lane keeps
the painted right separator from consuming the right inset. Collapsed openers
retain their three-logical-pixel left inset and existing button width, with equal
right clearance to that divider. Painting consumes the same named divider width;
no public API, item width, vertical spacing or caption text was changed.

Normal Debug solution build passed without warnings/errors. The focused padding,
divider, adaptive-reduction and gallery-reduction selection passed 11/11 in
`tests/RibbonKit.WinForms.Tests/TestResults/m4i-group-side-padding-final.trx`.
This includes Compact/Touch, 96/120/144/192 DPI, expanded/reduced columns, long
caption centering, collapsed targets and minimized-body divider coverage. The
earlier `m4i-group-side-padding.trx` ran against a stale assembly after a fixture
compile failure and is not evidence for the new tests; only the final file counts.

Public-only Office checks passed at actual 120 DPI in
`TestResults/visual/m4i-group-padding-hover-verified/`, asserting equal expanded
padding, collapsed clearance to actual painted separators and equal content
padding in the opened flyout. The flyout intentionally omits the group caption/
divider and keeps its existing window padding; an initial fixture incorrectly
treated it as a strip frame. Final native Compact/Touch hover captures were
inspected using real pointer placement. The user accepted the group-spacing fix
on 2026-10-28. Only affected
checks ran; no full/Release/package, WPF, actual VS or mixed-monitor campaign ran.

## M4J message-panel foundation — 2026-10-28

The user authorized the next slice, requested focused testing only, and explicitly
called out minimized bodies where the QAT is **not BelowRibbon**, including future
title-bar placement. The bounded proposal preceded implementation: repeatable
title/body messages with borrowed icons, an optional action, retained open state,
binding, keyboard access, and connected body/QAT geometry.

- `RibbonControl.MessageBar` is stable and owned. Its typed Items own RibbonMessage
  models; inherited Text is body text, Title is bold, and IsOpen/IsDismissible are
  bindable. A stable owned Action uses the existing RibbonButtonItem command path.
  Its invocation leaves the message open. Dismiss is idempotent, raises Dismissed
  once and retains the model/action/bindings for reopening. Icons/images are
  borrowed. BindingContext changes propagate through the message/action tree.
- Shared measurement wraps text, moves actions beneath text when needed and
  recomputes ribbon height on width/font/DPI/density changes. Compact starts at
  34 logical pixels; Touch action/close targets have 44-pixel minima. Theme roles
  supply message fills, foreground, border, icon color, padding and minimum height.
  Public renderer callbacks receive measured stack, text/icon and action geometry.
- Office attaches messages beneath the body or lower QAT and squares the adjoining
  frame's bottom corners. The final message edge stays rounded. With a minimized
  body and QAT **not BelowRibbon**, or a hidden QAT, messages form a standalone card
  below the tabs with an exposed rounded top. The predicate does not require
  TabRow; actual title-bar hosting remains M6B. Neutral keeps a separate flat stack.
  Closing the last message restores the exact previous height and foot geometry.
- Following the user's shadow report, the preceding body's shadow is clipped at
  the join and message content paints after body/QAT chrome. It cannot obscure the
  panel; the message stack retains its lower shadow. Mixed top/bottom corner masks
  reuse the shared analytic painter. Message arrival preserves host focus/selection;
  close restores a surviving message target or ribbon tab without stealing focus
  from another application window opened by a host action.
- Showcase **Merging → Messages** demonstrates add/reopen/clear. The demo's action
  updates application status; reusable layout, input and painting stay in RibbonKit.
  A message collection editor is wired for later designer review.

Relevant verification only, as requested:

- Normal Debug native solution build passed without warnings/errors, including
  XML/API-baseline and design compilation. No full aggregate, Release campaign,
  WPF checks or fresh-package campaign was run for this slice.
- Initial message/QAT geometry selection passed 8/8 in
  `tests/RibbonKit.WinForms.Tests/TestResults/m4j-messages-focused-initial.trx`.
  The three existing QAT-frame cases were retained. New short-text coverage then
  exposed an action/close overlap in a wrapped layout; the corrected main-row
  height/close placement passed `m4j-message-short-wrap-fixed.trx` (1/1).
  Final message-specific checks passed 6/6 in `m4j-messages-final-focused.trx`,
  including 96/120/144/192 DPI, odd/even corner bounds, stacking, both renderers,
  Compact/Touch, QAT visible/hidden and both placements, minimized/expanded state,
  close restoration, narrow resize and a high-opacity shadow non-overlap regression.
  The final binding-context and short-action Touch-width refinements separately
  passed 1/1 in `m4j-message-binding-context.trx` and
  `m4j-message-touch-target-width.trx`; earlier passed scopes were retained.
- Public-only Office native checks passed at actual 120 DPI in
  `TestResults/visual/m4j-messages-office-complete/`. Neutral passed in
  `m4j-messages-neutral-verified/`. Both cover all eight density/placement/minimize
  combinations, arrival focus/HWND/selection, explicit action KeyTip, pointer
  action/close, Enter close and exact height restoration, nondismissible narrow
  Touch and idempotent programmatic dismissal. Early native runs found missing
  keyboard focus restoration after a closed target and missing height reflow on
  resize; both were fixed. Neutral's final narrow assertion initially expected
  Office's top gap, and one retry stopped at foreground acquisition; final results
  above completed. The Office pass preceded only the short-action 44-pixel minimum,
  which has dedicated focused coverage. These use project outputs, not a package.
- Actual Office/neutral and Showcase captures were inspected, including expanded
  BelowRibbon, standalone minimized not-BelowRibbon and narrow Touch. Showcase
  captures are under `TestResults/visual/m4j-messages-showcase/`.

Human review remains pending. Actual VS, physical mixed-monitor, localization/RTL,
message live-region accessibility, motion and theme expansion remain separate
gates/slices. Messages are application notifications, not merge/customization
persistence. WPF runtime/build/CI remain unchanged; no commit or push was made.

## Pre-M6B review fixes — 2026-10-29

The user accepted the reviewed animation presentation and requested this bounded
fixing slice before M6B. All five corrections are implemented for review:

| Requested fix | Result |
| --- | --- |
| First message's top border | Office's attached panel omits its top stroke; the preceding body/QAT owns the shared edge. A standalone minimized not-BelowRibbon/hidden-QAT panel retains its full frame. |
| Lower QAT height | Uses its own Compact/Touch row metric instead of the taller tab-header metric. Ordinary targets use the same square dimensions as TabRow targets, centered with balanced vertical clearance. |
| Shadow discontinuity at messages | Office paints one shadow for the combined body/QAT/message outer frame and suppresses the component shadows. This keeps the side shadow continuous through the join in live and animated presentation. Neutral retains its independent surfaces. |
| Temporary minimized body | Opens directly after the tabs with external body side margins, above QAT/messages. Shared body geometry is translated into the popup interior, including scroll rebuilding. Outside-click dismissal releases the real popup and retained editors immediately while an inert drawing fades/slides away; application focus/input proceeds normally. |
| Customization split widths | One internal column layout supplies equal command columns and measured action rails to QAT, Classic and Simplified editors. Footer captions no longer alter the split; lists/trees, searches and labels keep their widths and horizontal positions across page selection. The Add/Remove/Up/Down follow-up also uses shared button dimensions, margins and vertical placement. |

Action alignment follow-up, 2026-10-29: all three pages use the same action area,
with one common search-row clearance. Simplified's additional tab-selector row
therefore does not move its buttons. QAT's footer now occupies the same table row
as the other pages' footer; native button identities, handlers and tab order are
retained. Add aligns with Up, Remove aligns with Down, and all four rectangles
match across page changes in Compact and Touch. This follow-up awaits user review.

The dismissal uses existing None/Subtle/Expressive policy (130/182 ms with 8/12
logical-pixel travel), with immediate startup/update-batch/system-policy fallback.
Input, policy changes, resize/movement/deactivation and disposal release drawings;
model ownership and application content remain unchanged. Command-menu closing,
Escape and programmatic closure retain their immediate behavior. No public API
signatures were added; the shadow/column/dismissal helpers are library-owned.

Focused evidence:

- New targeted checks passed 5/5 in `pre-m6b-review-first.trx`: square targets,
  no attached top stroke, continuous shadow pixels at 96/120/144/192 DPI and
  odd/even bounds, three-page column parity, popup margins/overlay and retained
  editor handles. New/affected dismissal-policy and square-target cases passed
  3/3 in `pre-m6b-popup-policy-final.trx` after exact target-size centering.
- Related message, QAT frame and options checks passed 19/20 in
  `pre-m6b-related.trx`. Its remaining geometry assertion encoded the old popup's
  internal outer margins. It now compares the shared body at the popup's actual
  interior width and translated origin. That correction plus affected message/
  minimize drawings passed 13/13 in `pre-m6b-drawing-popup.trx`.
- A fault-injection attempt with a persistently throwing application frame renderer
  aborted `pre-m6b-popup-policy.trx` during native painting. That probe was removed;
  arbitrary application paint-failure recovery is not established. The final
  policy test covers None, disabled system motion, resize, level changes and
  repeated cleanup without that unsafe probe. Failed evidence remains diagnostic.
- Public-only native checks passed for Office/neutral at actual 120 DPI in
  `artifacts/pre-m6b-review-office/` and `artifacts/pre-m6b-review-neutral/`:
  Compact/Touch, both placements, expanded/minimized frames, square QAT targets,
  actual pointer popup opening/dismissal, document focus/data/handle retention,
  and QAT/Classic/Simplified column/list/search parity. Additional native popup
  scopes in `artifacts/pre-m6b-popup-office/` and
  `artifacts/pre-m6b-popup-neutral/` cover both ribbon layouts/densities/
  placements, including inspected intermediate closing frames. Passed review
  scopes were retained; later checks target changed or unresolved paths only.
- Normal Debug native build includes XML/API and design compilation. Actual
  resting, overlay, intermediate closing and customization captures were inspected.
  No full aggregate, WPF, Release, fresh-package, actual VS, physical mixed-monitor/
  touch/IME or performance/resource campaign was run. Human review of these fixes
  remains pending; M6B has not started.
- Action alignment follow-up: the two density/three-page rectangle checks and
  existing search-spacing check passed 3/3 in `pre-m6b-action-alignment.trx`.
  Public-only options checks passed at actual 120 DPI for Office and neutral in
  `artifacts/pre-m6b-action-alignment-office/` and
  `artifacts/pre-m6b-action-alignment-neutral/`. Exact native button bounds,
  Add/Up and Remove/Down row alignment, column positions and list/search widths
  match across QAT, Classic and Simplified. Actual captures were inspected.
  The normal Debug build passed; unrelated motion/message tests were not rerun.

Reproduce with native `--pre-m6b-review` / `--pre-m6b-review-flat` and an evidence
directory, or consumer `--validate-pre-m6b-review` variants. Native
`--pre-m6b-popup-close` variants select just the temporary-body checks.
Native `--customization-alignment` / `--customization-alignment-flat`, or consumer
`--validate-customization-alignment` variants, select just the options-page checks.

### Additional review corrections — 2026-10-29

The requested overflow alignment and subsequent eight fixes are implemented for
review. M6B remains queued; these changes add no public API signatures.

| Requested fix | Result |
| --- | --- |
| Simplified overflow placement | More Commands aligns with the body's right inset when the row fits. The same target drives paint, input and anchoring; minimum-width scrolling remains available. |
| Backstage clipped letters | KeyTip badges measure their actual navigation font and add DPI-scaled clearance, replacing the fixed-height letter box. Rich hover ScreenTips are unchanged. |
| Apply/OK tree flicker | Native list/tree redraw stays suspended through refresh and application callbacks, including closing on OK. Tree images refresh within that hold; the selected and top visible nodes are retained. |
| Print Preview/Chart defaults | Showcase authors inline commands and supported ComboBoxes for both transient tabs. Other editors remain available through full groups. |
| Picture Format order | Showcase inserts View before Picture Format, leaving Picture Format after the normal authored tabs and before merged contributions. General library tab-order semantics are unchanged. |
| Import/export artwork | Classic and Simplified customization swap the two SVG mappings; the command handlers and identities stay the same. |
| Simplified ComboBox height | Inline fields use 26/44 logical-pixel Compact/Touch minima, capped/centered in the existing row. Classic and full-group field sizes are retained. |
| Borrowed full-group contents | The inline group becomes one medium collapsed group button while the popup uses its original controls. Closing restores the same handles/order and current values/checked states. Clicking the temporary button closes the popup without activating the command restored underneath. Root and temporary minimized rows share this behavior; profiles are untouched. |
| Tab-header scrollers | Both built-in renderers use the body scroller's themed chrome and vector chevron instead of a small text glyph. Tab-header target dimensions and layout stay unchanged. |

Relevant verification only:

- Normal Debug native solution build passed with zero warnings/errors, including
  API/XML and design compilation. No WPF files or build were involved.
- `simplified-more-right-edge.trx`: 3/3. `pre-m6b-followup-badges-tree.trx`:
  5/5 for both-density badges/fields, transfer mapping and Apply/OK redraw/scroll.
  `pre-m6b-followup-group-dismiss.trx`: 2/2 normal/minimized group restoration
  and click return. Related Simplified, Backstage and options cases passed 17/17
  in `pre-m6b-followup-related.trx`; selections overlap and are not additive.
- `pre-m6b-tab-scroller-chrome.trx`: 2/2, covering shared header/body chrome and
  glyph pixels at synthetic 96/120/144/192 DPI, odd/even bounds, both renderers,
  directions and resting/hover/pressed states, plus disjoint header utility bounds.
- Public-only native core checks passed for Office/neutral at actual 120 DPI,
  Compact/Touch, in `artifacts/pre-m6b-followup-office/` and
  `artifacts/pre-m6b-followup-neutral/`. These verify measured badges, larger
  fields, More placement, native editor identity/current state, and actual
  Apply/OK draw suppression with scroll/handle retention.
- Group-button click-return evidence is assembled from the passed Compact prefixes
  in `artifacts/pre-m6b-followup-groups-office/` and
  `artifacts/pre-m6b-followup-groups-neutral/`, then the Touch tails in
  `artifacts/pre-m6b-followup-groups-office-touch/` and
  `artifacts/pre-m6b-followup-groups-neutral-touch/`. An unavailable Classic field
  baseline in the expanded fixture caused its earlier Touch assertion failures;
  the fixture now keeps that source group expanded and verifies it was painted.
  Passed scopes were retained instead of rerunning unrelated checks.
- Showcase's ordering/default checks and inspected captures passed in
  `artifacts/pre-m6b-followup-showcase-final/`; the tab-scroller capture passed in
  `artifacts/pre-m6b-tab-scrollers-showcase/`, actual 120 DPI. Captures verify
  foreground ownership and the GUI process is awaited before another native run.
  Earlier overlapping-window captures are diagnostic, not visual acceptance.

Core native checks use `--review-followup` / `--review-followup-flat` (consumer:
`--validate-review-followup` variants). Native `--review-followup-groups` variants
select group restoration; `--review-followup-groups-touch` variants select its
Touch tail. Showcase `--capture-review-followup` and `--capture-tab-scrollers`
accept an evidence directory. Actual captures were inspected. Full aggregate,
Release/package, actual VS, physical mixed-monitor/touch/IME and performance gates
were not run. Human visual acceptance of these refinements remains pending.

## Remaining slices

This is a working delivery order, not a promise that each row fits one change.
Each public API and ownership contract is proposed before its implementation.

| Slice | Remaining deliverable |
| --- | --- |
| M4B | File command-menu foundation (accepted at the recorded scope) |
| M4C | Backstage navigation, commands and native page Controls accepted at reviewed scope; actual VS checks deferred to designer slice |
| M4D | Dropdown foundation reviewed; neutral bold/transparent heading follow-up implemented; filtering controls/popup resize remain refinements |
| M4E | In-ribbon/QAT gallery foundation reviewed at the 2026-10-28 continuation scope; Large dropdown fallback implemented, human review pending |
| M4F1 | Tab/group merge foundation accepted at the 2026-10-28 reviewed scope |
| M4F2 | Modal scopes implemented; human review pending |
| M4G | Compact/Touch density implemented; human review pending |
| M4H | Deferred by user direction, 2026-10-28: built-in localization and RTL layout/navigation/editor-direction contracts follow the remaining component work, including customization, animation and themes |
| M4I | Joined Office 2024 BelowRibbon QAT implemented as M4I1, human review pending; comprehensive appearance inventory/consolidation and later themes remain separate bounded passes |
| M4J | Message-panel foundation implemented, human review pending; connected/square joins, standalone minimized not-BelowRibbon case and shadow protection included |
| M5A | Catalog and QAT draft/persistence/Apply/Cancel foundation implemented; current acceptance and evidence in [M5 ledger](M5-STATUS.md) |
| M5B | QAT context menu and extensible options dialog implemented; current acceptance and evidence in [M5 ledger](M5-STATUS.md) |
| M5C | Ribbon customization UI and structural changes implemented; focused evidence and visual review in [M5 ledger](M5-STATUS.md) |
| M5D | [Native row, More Commands, shared structure and Simplified customization/profiles implemented; human review pending](M5D-SIMPLIFIED-RIBBON.md) |
| M5E | [Showcase automatic customization/application preferences and restart restore implemented; focused and actual-process checks passed, human review pending](M5E-SHOWCASE-PERSISTENCE.md) |
| M6A | [Opt-in geometry, group collapse/restore, tab/header, minimize/restore, popup, Backstage/page opening/closing and message-panel motion implemented; visual/performance acceptance and optional host synchronization remain](M6A-MOTION-PLAN.md) |
| Pre-M6B fixes | [Initial fixes, action alignment and additional review corrections implemented; human review pending](#pre-m6b-review-fixes--2026-10-29) |
| M6B | [Optional RibbonForm and TitleBar QAT implemented; current evidence and remaining review](M6B-WINDOW-INTEGRATION.md) |
| M6C | [Direct material alpha preserves native opening motion on Windows build 26100+; current evidence, user review and remaining platform gates. Computer use stopped](M6C-BACKDROPS.md#first-show-material-alpha-and-windows-opening-motion--2026-10-30) |

Cross-cutting follow-ups remain separate: optional ICommand support; deferred
accessibility compatibility; actual VS designer/package/independent-consumer
acceptance; physical mixed-monitor/DPI and release/performance gates. Run only
relevant checks during component work. Native touch/IME is not an automatic gate.
WPF improvement ideas remain deferred in their separate plan.
