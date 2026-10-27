# M4 broader native surfaces

Status: **M4A contextual tabs implemented for review**, 2026-10-27. Continuation follows the
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

Human visual acceptance remains open. Release/package, unrelated/full-suite,
actual Visual Studio round-trip, physical mixed-DPI/monitor, RTL and new
accessibility compatibility scopes were not run. No native touch/IME was added.
The same public fixture is linked into the native runner and available in the
independent package consumer; this slice ran the Debug project runner, not a new
package. The original foundation accessibility bridge remains unchanged.

## Next bounded slice

Review contextual-tab behavior and presentation. Then propose another native M4
component, such as the File surface or gallery foundation, with its API/ownership
contract before implementation. Merge/modal services, comprehensive theme
consolidation, customization/storage and optional window integration retain their
separate milestones. Contextual presentation does not authorize a context manager
or document/MDI coordination service.
