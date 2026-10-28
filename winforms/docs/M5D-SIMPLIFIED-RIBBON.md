# Native M5D Simplified Ribbon

Status, 2026-10-28: **D1 selection, D2 native row and D3 shared customization/profile
integration implemented; human visual review pending.**

## Behavior and ownership

Classic remains the default. `RibbonControl.LayoutMode` selects Classic/Simplified
independently of Compact/Touch density, minimization, theme and QAT placement.
Applications own mode persistence; no motion is introduced before M6.

The [WPF layout/customization contract](../../docs/15-SIMPLIFIED-RIBBON-PLAN.md) and
[implementation record](../../docs/history/library/14-simplified-ribbon.md#3255-simplified-ribbon-first-layout-slice--2026-10-13)
are behavioral references. Native model/layout/popup services implement this mode;
WPF controls and evidence are not used as native acceptance.

- Original commands, handlers, bindings and native editor owners remain authoritative.
  Inline commands use original presentations; displaced references invoke their
  sources once and full-group flyouts borrow original content. Native ComboBox
  handles move with their existing presenters and return when popups close.
- Authored `SimplifiedPlacement`, `SimplifiedSize` and `SimplifiedPriority` are
  separate from Classic SizePolicy. Small suppresses the label; Medium includes
  it. Split commands project horizontally, galleries open their shared gallery
  popup, and supported ComboBox editors retain configured width/editability.
  Arbitrary/TextBox editors remain accessible through full groups.
- The measured selector reserves More Commands before fitting candidates. Higher
  priority survives longer; ties displace from the end. Surviving and displaced
  commands retain draft order. Repeated canonical sources use their first eligible
  presentation. Dividers follow contiguous group runs, including repeated runs.
- More Commands includes displaced commands, a compact separator, then every
  visible full group, including groups without inline commands. Below its minimum
  extent, scrolling/focus reveal preserves usable targets. Shared device-pixel
  rectangles drive painting, hit testing and popup anchoring.
- Minimized tab popups use the same row. Full groups retain Classic geometry.
  Existing lower-QAT/message joins and native popup keyboard/dismissal paths are
  reused. Mode changes close the current popup stack; invalid focused editor input
  blocks the change. Transient merged/contextual/modal commands may use authored
  defaults but do not enter permanent Simplified preferences.

## Shared structure, independent selections

Both modes use one ordinary tab/group structure and the same structural engine.
New/removed tabs and groups, names and structural order synchronize on Apply.
Custom group commands remain source-backed presentations; authored source command
collections and owners are preserved. Simplified inline choices/order/size are
independent per tab. No override means authored defaults; an explicit empty
selection means full-group access through More only.

`RibbonCustomizationTarget.Simplified` edits shared structure and Simplified
preferences while preserving live QAT settings. Classic-only structural removal
prunes invalid live Simplified tab/command references atomically. A stale structural
or Simplified checkpoint rejects Apply, rather than overwriting another page's
changes. Failed Apply restores structure, selection, pins and independent settings.

The Options window offers exactly one Customize Ribbon page for its current mode,
alongside QAT and application-provided pages. Switching modes preserves detached
pending drafts; an inactive mode's editor cannot Apply. A clean newly active page
reloads live changes. Invalid input blocks Apply; Cancel keeps the last Apply.

The Simplified page follows the user's WPF reference: independent source/target
tab selectors, two searches, grouped available/selected lists, Add/Remove in the
middle, reorder on the right, Reset/Import/Export under the left pane and New/Edit
under the right. Show Label changes Small/Medium. Reset offers current-tab choices,
all choices, shared structure plus choices, and Reload. New/Edit/Reset use the
ordinary ribbon popup model, measured menu surface, frame, shadow and keyboard
paths. They have no headings; checked actions use a plain vector tick. Their input
owner belongs to Options so commands work while the modal dialog disables its main
window, and Escape restores the opener. Alt+F4 targets Options and honors canceled
form closing.
Native selector height is included in table measurement, preserving equal search
gaps at Compact/Touch density. Only the shared **OK, Cancel, Apply** footer commits.
Context pin/unpin uses the same session, preserving hidden authored defaults.

The regular ribbon context menu also offers **Simplified Ribbon**, with the shared
plain tick when active. It changes the existing `LayoutMode`, retaining native
input validation and appearance ownership. QAT panel/item contexts omit it in
both placements; the exclusion follows QAT overflow/group flyout origins. Regular
ribbon flyouts, including groups reached from Simplified More Commands, retain it.

Custom groups in both modes share a name/icon/layout editor. Icon choices borrow
catalog command artwork through an optional stable `IconCommandId`; None clears
the icon. `RibbonCustomizationGroupLayout.Stacked` is the default, including older
profiles with no layout field and new Simplified groups synchronized to Classic.
Stacked preserves Small and demotes Large to Medium; explicit Large uses a row of
Large commands and locks the command-size picker. Native editors retain their
compact presentation. Detached edits, serialization, Apply, source artwork updates
and rollback share these preferences without changing authored commands/assets.

The tab selectors and Classic command-size picker keep native ComboBox selection
and events while sharing the ribbon field painter and native choice-list popup.
Office2024 rounds both field and popup; neutral retains square geometry. Keyboard
preview commits only on acceptance; Escape, external selection, hiding/disabling,
font/DPI changes and disposal clean up the choice window.

## Profiles and Showcase

Native JSON version three adds optional `Simplified`, containing tab IDs and an
ordered command-ID/Small-or-Medium-size list. Stable explicit identities are required
for persisted tabs/sources. Unknown, duplicate, unsupported and transient selections
are rejected before live mutation. Version-one/two input remains readable; absent
sections preserve live preferences. Scoped export merges other scopes' live values.
Mode, density, values and temporary merged/modal state are excluded. Import remains
detached; applications own files, dialogs, storage and migration decisions.

Showcase **Options → Show → Simplified Ribbon** switches modes. Paste, Copy, Cut,
Bold, Italic and representative commands/ComboBoxes have authored defaults; Paste
and Bold receive higher priority. Shared public APIs supply the row and editor.
The sample owns its toggle, SVG artwork, handlers and import/export file dialogs.

## Evidence and pending review

- Normal Debug native solution build: zero warnings/errors, 2026-10-28.
- D1: 11 focused selector cases, including exact fit/reservation, priorities,
  repeated runs, deduplication, More-only/scroll fallback, synthetic
  96/120/144/192-DPI measurements, seeded partitions and 10,000 commands.
- `m5d-existing-contracts.trx`: 44 passed across the existing catalog, scoped
  sessions, structural editor/options and selection contracts. This scope includes
  the selector cases; counts are not additive.
- `m5d-integration-final.trx`: nine passed for row/split/width/density behavior,
  original actions/native editor handle reuse, version-three restart/legacy input,
  QAT independence, stale rejection, removal pruning, transient exclusion,
  rollback, minimized nested access, context/gallery behavior and Options drafts.
- Independent public-only consumer: `--simplified-checks` and
  `--simplified-checks-flat`, Office2024/neutral, actual native 120 DPI,
  Compact/Touch, narrow/wide row and More/full-group popups, original editor
  handle/ownership, minimized row, QAT/message capture, shared structural Apply,
  footer Cancel, independent selectors, uniform rows/search gaps, themed New menu
  and pending-draft mode switches. Results/captures are in
  `artifacts/m5d-native-office/` and `artifacts/m5d-native-neutral/`.
- Showcase generated captures: `artifacts/m5d-showcase-final/`, Compact/Touch at
  1280/680 widths, Inputs, Simplified Options and a version-three profile.
- Customization popup refinement, 2026-10-28: eight focused Options cases passed
  across initial/repair selections (modal menu ownership/no heading/plain tick,
  choice lifecycle, Classic size picker in both densities and tick bounds at four
  synthetic DPIs). The existing mode-specific editor check and two shared menu/
  Alt+F4 regressions also passed. Normal Debug native solution build is clean.
  Public-only `--options-popup-checks`/`--options-popup-checks-flat` passed at actual
  120 DPI in both densities/renderers, with modal input, no menu heading/checkbox,
  independent selector keyboard commit/cancel, opener focus, canceled menu Alt+F4
  and choice Alt+F4 closing Options while preserving the main form. Final results
  and inspected captures: `artifacts/options-popups-office/` and
  `artifacts/options-popups-neutral/`. These supersede the earlier M5D Options/New
  menu captures for these controls; broader pending gates below are unchanged.

The public consumer fixture compiles against the project for these native checks;
its package entry points are ready, but a fresh package was not produced or tested.
No full suite, WPF or Release build, actual VS designer, external UIA/screen-reader,
physical mixed-monitor/touch/IME/reduced-motion campaign was run. Synthetic DPI
and native 120-DPI captures do not establish those gates or four-corner symmetry
at every size/DPI. Human review of the row, full groups and customization remains
pending. M5C acceptance is unchanged. No commit, push or publication was performed.

## Eight review fixes — 2026-10-28

Status: **implemented and focused verification complete; human review pending.**

| Review item | Result |
| --- | --- |
| 1. Apply confirmation text | Successful Classic/Simplified/QAT Apply clears the status row; validation errors remain. |
| 2. Right-click dismisses the source popup | Customization opens as another level above the original group/menu/body/overflow. Escape removes that level and retains the source. Options action-menu right-clicks stay in place. |
| 3. Tree flicker and expander hover | Native `TVS_EX_DOUBLEBUFFER` plus managed buffering; checkbox hover invalidates affected rows only. Expanders have no hover/pressed fill or color change. |
| 4. Custom group icon/layout | Both pages use the shared create/edit dialog and borrowed icon gallery. Optional profile fields persist the icon identity and Stacked/Large layout. Missing layout defaults to Stacked, as requested. |
| 5. Opening popup flashes the inactive window frame | A native trace reproduced eight inactive-frame repaints in eight menu openings. A scoped native-window hook retains the host's active nonclient frame during synchronous activation within its owned popup family. Keyboard focus still reaches popups/native editors; genuine activation changes keep normal dismissal. |
| 6. Context action icons | QAT add/remove and Simplified pin/remove commands are text only. |
| 7. Header height and minimize shape | Compact default tab height is 40 logical pixels; Touch remains 48. The minimize visual keeps its width, reduces its height to a square and stays vertically centered inside the full click target. |
| 8. Header QAT alignment | Measured QAT targets use the density row height plus a named two-logical-pixel optical offset within the taller header; below-ribbon placement is unchanged. Painting, hit testing and anchors use those targets. |

Focused evidence: 12 review cases passed across `review-eight-focused-repair.trx`
and `review-tree-repair.trx`; 17 affected structure/options/minimize checks passed
in `review-eight-regressions.trx`; 17 context/QAT/Alt+F4 checks passed in
`review-context-header-qat.trx`. These are bounded selections, not a full-suite run.
The synthetic tick/header measurements include 96/120/144/192 DPI; the tree
pixel comparison exercises an unchanging expander hover with visual styles enabled.

Public-only `--review-checks`/`--review-checks-flat` passed in both renderers at
actual 120 DPI, Compact/Touch, both customization modes: no inactive-title-bar
repaint over eight open/close cycles, normal dismissal/focus on a different window,
retained right-click popup levels, quiet Apply, icon borrowing, Stacked defaults,
Large/Stacked command presentation and native tree buffering. Results, profiles
and inspected captures are in `artifacts/review-eight-office/` and
`artifacts/review-eight-neutral/`. The affected Office2024 options/choice keyboard
and cancellable Alt+F4 checks also passed in `artifacts/review-options-focus/`.

Normal Debug native solution build passed with zero warnings/errors. Generated
Showcase header/icon-gallery captures are in `artifacts/review-eight-showcase/`;
these do not establish native window-frame acceptance. ImageList can defer cloning
its source bitmap until handle creation, so the icon gallery retains its owned
placeholder bitmap until disposal. An initial test-host crash exposed that lifetime
error; the repaired group-dialog checks and native captures passed.

No WPF, Release, full-suite, fresh-package, actual VS or physical mixed-monitor/
touch/IME campaign was run. The inactive-frame result covers the recorded native
sequence, not every possible application/OS activation race. Broader gates and
human review remain separate. The work remains uncommitted.

### Context-menu layout toggle — 2026-10-28

Nine focused checks passed in `simplified-context-toggle.trx`: both mode/checkmark
states and directions, unchanged QAT/density, pointer and keyboard QAT exclusion
in both placements/densities, QAT group/overflow versus regular More-group origins,
invalid native-input gating, and retained text-only add/remove/pin actions.
The normal Debug native solution build passed with zero warnings/errors.
Public-only `--simplified-context-checks`/`--simplified-context-checks-flat` passed
at actual 120 DPI in both renderers, Compact/Touch and both QAT placements, with
native context opening, clicking both mode directions and QAT panel/item omission.
Checked/unchecked captures were inspected under `artifacts/simplified-context-office/`
and `artifacts/simplified-context-neutral/`. No new public API/profile field was
added. No full suite, WPF/Release build, package or physical mixed-monitor check
was run for this bounded addition; human review remains pending.
