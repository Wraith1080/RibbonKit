# M5 native customization and Simplified Ribbon

Status: **M5A/M5B accepted at the reviewed scope; M5C and M5D implemented with human review pending**, 2026-10-28.

User acceptance, 2026-10-28: the QAT/options dialog, rounded controls, dual search,
spacing/minimum width, caption fixes, larger Showcase and owner centering work as
expected. Physical monitor, actual VS and accessibility gates remain separate.

## M5A catalog and QAT transaction foundation — 2026-10-28

The user authorized continuation and confirmed focused verification only. M5A
starts with a usable QAT engine; M5B adds the right-click customization menu and
QAT editor UI. M5C extends the same identity/transaction foundation to structural
Ribbon editing. This slice implements no context menu or customization window.

Public contract proposed before implementation:

- Stable owned `RibbonControl.CommandCatalog`, with owned application `Commands`
  for sources that need no ordinary ribbon slot, and available-entry snapshots for
  QAT versus Ribbon customization. Groups occur only in the QAT list. Discover
  authored groups/direct commands and QAT sources; deduplicate by source identity.
- Preserve QAT-only sources removed by customization, including Undo/Redo, so both
  available lists can restore them. Projections borrow source models; native
  fields, values, command handlers and bindings retain their existing owners.
- Application-authored source IDs must be explicit and stable for JSON export.
  Generated runtime IDs continue working for in-process drafts. No caption/path
  fallback, WPF JSON compatibility, automatic application storage or appearance
  persistence is introduced. Changing stable IDs requires application migration.
- A detached QAT draft describes membership/order and toolbar visibility/placement.
  Apply preflights all sources/identities and batches accepted changes. Invalid
  focused native input blocks Apply. Cancel discards only unapplied edits; the
  latest successful Apply is the checkpoint. Reset uses captured catalog defaults.
  Parallel/external permanent QAT changes make a session stale until Reload.
- Versioned, application-scoped JSON uses data-only entries. Reject malformed,
  duplicate-property, foreign, future-version and unknown-command input before
  touching live models. Import changes only the draft. Merge/modal/notification/
  File/Backstage content and transient references are excluded; current transient
  QAT declarations survive Apply without entering saved data.
- UI/menu entry points, custom tabs/groups, proxy presentation in Ribbon groups,
  Simplified layout, animation, themes, localization/RTL and actual VS review
  remain their planned slices. Verify only catalog, draft/serialization/Apply and
  affected QAT source/native-editor/merge boundaries in this slice.

Implemented behavior:

- Catalog discovery includes authored groups/direct commands, declared application
  Commands and ordinary QAT sources, deduplicated by source identity. Display
  labels remain source-backed. File/Backstage/notification and merge/modal sources
  are excluded. TextBox sources are Ribbon-only; groups are QAT-only. Existing
  button/check/radio, dropdown/gallery/split, ComboBox and group projections are
  reused. No generic projection-provider API was introduced.
- Explicit source-ID assignment is tracked independently of generated runtime IDs.
  Runtime-only drafts still work; Export/Import require stable selected source IDs.
  Presentation keys distinguish duplicate source presentations; explicit reference
  IDs are preferred, otherwise authored source/occurrence keys are retained.
  Session-created presentation keys are stored in JSON. GetQuickAccessEntryId maps
  live permanent entries for the future context menu without assuming source IDs
  and presentation IDs are equal.
- Removing QAT-only owned commands through Apply moves them into catalog Commands,
  preserving the same component, handlers and bindings. Reset can return the same
  model to its original QAT slot. Removed presentations are retained for reuse and
  disposed with the catalog when still detached. Ordinary collection Remove keeps
  its existing detach semantics; application-only sources should be declared in
  Commands when they must remain cataloged independently of toolbar membership.
- Drafts contain only permanent membership/order and toolbar visibility/placement.
  Existing authored presentation models/aliases are reused. Newly added entries
  use source-backed default labels; this version supplies no rename UI or alias
  persistence schema. Source values, checked/selection/enabled/visible state,
  captions, bindings, theme/density, transient scope and application content are
  not restored from JSON. Host-authored reference metadata should have stable
  explicit IDs when its identity must survive changes to application declarations.
- Apply validates identities, sources and stale checkpoints before mutation, commits
  focused native ribbon input, revalidates after host validation callbacks, and
  batches the accepted topology. Invalid input returns false. Validation/identity
  changes during commit are rejected. A structural rollback restores the previous
  toolbar for an Apply-time host event failure; repeatedly throwing rollback event
  handlers remain application behavior rather than a general exception-isolation
  promise. Current transient QAT references retain their instances/relative slots.
- Cancel discards only edits after the latest Apply. Reload rebases external
  permanent changes; transient changes do not stale the draft. Reset stages the
  defaults captured before preferences were loaded. Import stages only validated
  native JSON; storage is application-owned and Applied is the persistence cue.
  JSON uses an explicit native discriminator, version and application identity,
  string placement values, required fields and duplicate/unknown-property rejection.

Relevant evidence:

- Normal Debug solution build passed without warnings/errors, including runtime
  XML/API-baseline and designer compilation. No full/Release/WPF/package campaign
  was run, following the user's focused-testing direction.
- Seven focused unit/STA cases passed in
  `tests/RibbonKit.WinForms.Tests/TestResults/m5a-customization-focused.trx`:
  catalog policy, QAT-only recovery, detached/Apply/Cancel/Reset/restart behavior,
  malformed/foreign/future/unknown JSON and duplicate IDs, transient preservation,
  external-stale detection, native-input veto/HWND retention, validation-callback
  source mutation, explicit persistence identities and host-event rollback.
  After moving rejected-plan cleanup before the identity-conflict throw, the
  affected rejection case passed separately in `m5a-rejected-plan-cleanup.trx`.
  No passed unrelated scope was repeated.
- Public-only Office and neutral consumers passed at actual 120 DPI under
  `TestResults/visual/m5a-customization-{office,neutral}/`. They verify invalid input
  blocking, successful Apply/Applied counts, shared toggle binding, single native
  editor borrowing through a customized whole-group QAT entry, removed-command
  recovery in both catalogs, reset to the original command instance/handler,
  retained native HWND/value and draft-only JSON import. Runs used project outputs,
  not a fresh package. Their captures and exported data-only profile were inspected.
- Showcase **Merging → QAT drafts** demonstrates a preset detached change, Apply,
  Cancel, Reset and application-owned in-memory Save/Restore. It uses only public
  engine APIs. Actual before/unapplied/applied/cancel/reset/import captures were
  inspected under `TestResults/visual/m5a-customization-showcase/`. This preset demo
  is not the M5B customization editor or context menu.

## M5B — QAT context menu and extensible options dialog — 2026-10-28

Authorized public contract: a reusable `RibbonQuickAccessCustomizationEditor`
owns a session and borrows the ribbon. Following the user's WPF references, the
resizable `RibbonOptionsDialog` provides a navigation rail, page heading and a
footer ordered OK/Cancel/Apply (Apply affects the selected page). `RibbonOptionsPage` hosts arbitrary native
application controls. `RibbonQuickAccessCustomizationForm` supplies its QAT page.
Root convenience opens the form,
with an opt-out for the built-in context menu and application identity for its
session. `CustomizationApplied` is a host persistence cue; storage stays host-owned.
The inherited application ContextMenuStrip takes precedence. Add/remove/order,
placement, visibility, Reset/Reload, Apply/OK/Cancel use the M5A draft engine.
Whole groups remain QAT-only. Structural Ribbon editing remains M5C.

Implemented behavior:

- Compact, single-line command paths follow catalog/ribbon order, with borrowed
  vector/image icons. Add/Remove sit between equal-width lists; reorder arrows are
  on the right. Search, Reset and Reload retain their draft behavior. Placement
  and visibility are context-menu commands. Missing draft sources remain removable; duplicate authored QAT rows
  retain separate identities, while Add avoids introducing duplicate sources.
- Application pages use `Applying` for cancellable validation, `Applied` for their
  own commit/checkpoint and `Cancelled` to discard unapplied edits. Apply affects
  only the selected page. OK validates all pages before applying them in order;
  there is no global rollback across application settings. Cancel preserves each
  page's prior Apply. Host-cancelled window closes preserve the active draft.
- The dialog owns attached pages/child controls; removing a page detaches it
  without disposal. `OptionsDialogOpening` lets the context-menu path include
  application pages. Showcase demonstrates an application-owned Editor page.
  The system title bar remains native; custom window chrome is a later slice.
- Right-click and Shift+F10/menu-key paths cover ordinary/disabled source commands,
  group flyouts, minimized body and QAT overflow. Context menus attach to the stable
  root after dismissing borrowed flyouts. Application context menus take precedence,
  and native input controls retain their editing menu. Alt+F4 forwards to the host
  form and respects cancellation, matching the existing popup policy.
- `CustomizationApplied` is raised after the engine checkpoint, including context
  operations and QAT page Apply. Application callback exceptions propagate instead
  of being misreported as draft errors. No library file dialogs/storage were added.

DPI scope: derive spacing from 96-DPI metrics and row/button sizes from measured
font height; resize on parent DPI/font changes, bound the initial window and
minimum to the working area, and allow scrolling when the page cannot fit.
The shell also clamps its initial bounds and, after WinForms installs new monitor
bounds, recalculates/clamps to that monitor's working area. Native child controls
remain owned by their page through DPI/font changes. Heading fonts used during
WinForms autoscaling are retained until child disposal; retiring them mid-scale
caused a measured-label crash during development and is specifically avoided.

Focused evidence (normal Debug outputs):

- Native solution build: zero warnings/errors. Editor/menu/metrics checks passed
  in per-scope runs: `m5b-editor-final-scopes.trx` (8),
  `m5b-options-ownership-isolated.trx` (1), `m5b-options-apply-lifecycle.trx` (1),
  `m5b-context-source-final.trx` (1), `m5b-context-editor-preservation.trx` (1),
  `m5b-catalog-order-and-close.trx` (2), and `m5b-dpi-working-area.trx` (5).
  These overlap; they are not a single aggregate or a sum of distinct cases.
- `m5b-apply-close-boundaries.trx` passed 8 affected checks: the seven M5A engine
  cases plus page Cancel after a host-vetoed OK. Earlier development runs include
  a font-lifetime crash; the affected page lifecycle was repaired and rerun.
- Pure measurements cover 96/120/144/192 DPI, enlarged fonts and an oversized
  window on a working area with negative desktop coordinates. Public-only native
  Office/neutral consumers passed at actual 120 DPI under
  `TestResults/visual/m5b-options-{office,neutral}/`. They cover application-page
  isolation, QAT Apply/Cancel, removed-command recovery, Reset/Reload, stale Apply,
  large-font Touch geometry, repeated font changes and the extensible modal path.
  These use project outputs, not a fresh package. Office/neutral actual captures
  and the Showcase options captures were inspected; human acceptance remains open.
  The final working-area clamp/catalog-order revision passed the Office consumer
  under `TestResults/visual/m5b-options-office-final/`; final Showcase captures are
  under `TestResults/visual/m5b-options-showcase-final/`.

### Dialog review refinements — 2026-10-28

The user's five-point review is implemented, awaiting their next visual review:

- Placement and visibility controls removed from the page. Its Reset stages only
  default commands and retains these preferences; raw session Reset remains a
  complete-profile operation.
- Apply moved beside Cancel in the common footer, ordered OK → Cancel → Apply;
  it continues to apply only the selected page. The separate page button row is
  removed. Empty status text reserves no space.
- Default Mouse window reduced to 920 × 520 logical pixels; Touch uses 1030 × 650.
  Lists fill the available page height down to the left-side Reset/Reload row.
  DPI/font minimums, scrolling and working-area clamps remain in effect.
- Search uses a density-sized frame around a single-line native EDIT. Touch
  enlarges the hit area without moving its text to the top of a multiline field.
- Context customization now uses `RibbonPopupWindow` / `RibbonPopupSurface` and
  shared menu rows/rendering, matching the QAT overflow popup family. It no longer
  uses the stock ContextMenuStrip presentation. A context screen anchor enters the
  existing popup manager, so keyboard, Escape/outside dismissal and Alt+F4 use
  the shared lifecycle. Temporary command models are library-owned and released
  on replacement/disposal; application context-menu precedence remains.

Showcase's default QAT now contains only Bold, Italic and Mouse/Touch mode. Extra
gallery/control and transient chart references are no longer pre-attached. Their
source controls remain in the ribbon. The standalone Move QAT command remains in
the command catalog. The preset draft demo replaces Italic with Redo; its Reset
returns the sparse default. Capture mode exports `default-qat.json` for review.

Relevant evidence: `m5b-review-layout-menu-initial.trx` passed 12 of 14 cases;
two focus-sensitive popup scopes were corrected to check synchronous ownership,
then both passed in `m5b-review-context-lifecycle-final.trx`. The separate
`m5b-review-reset-preferences.trx` passed the hidden-preference preservation case.
Public-only native Office/neutral checks passed at actual 120 DPI under
`TestResults/visual/m5b-review-{office,neutral}/`, including footer order, list
spacing, centered Touch search, keyboard placement/visibility actions and actual
Alt+F4 host cancellation. This is focused evidence, not a full-suite result.
The final Reset preference check also passed in
`TestResults/visual/m5b-review-office-final/`. Mouse/Touch Showcase captures were
inspected in `TestResults/visual/m5b-review-showcase-{mouse,touch}/`; both exported
profiles contain exactly `cmd.bold`, `cmd.italic` and `density.selector`.
Normal Debug solution and the final affected sample build had zero warnings/errors.

### Rounded options chrome, right search and caption width — 2026-10-28

The subsequent review is implemented, awaiting visual acceptance:

- The Office 2024 OK button, both search frames and both list frames use the
  shared mirrored corner painter and existing surface/accent/border/radius roles.
  Native Button behavior, EDIT controls and list windows remain; list child
  regions protect rounded border joins. Neutral controls remain square.
- Both lists have independent filters. Current-list Remove and reorder resolve
  the selected presentation ID in the complete draft, so filtering cannot remove
  a hidden command or confuse duplicate source presentations.
- A single scaled gap separates labels/searches, searches/lists and lists/Reset.
  Both native search editors remain centered in the density-sized frames.
- The QAT content has a 650-logical-pixel width floor, enlarged for large fonts.
  A narrow dialog clips/scrolls that fixed-width page rather than compressing the
  columns. Larger host-set native MinimumSize widths survive font changes.
- Collapsed group measurement now grows for the caption, up to the proposed
  `RibbonTheme.CollapsedGroupMaxWidth` API (default 108 logical pixels; minimum
  80; excludes the divider). Shared scaled caption insets, two-line wrapping and
  final-line ellipsis replace the fixed width/unbounded text path. Arrow and hit
  geometry still use the measured opener, preserving equal outer padding.

Focused evidence: six cases passed in `m5b-round-search-caption-initial.trx`;
the caption case's exception-type expectation was corrected and passed separately
in `m5b-caption-limit-final.trx`. `m5b-options-minimum-width.trx` passed a shown
narrow-window/larger-host-minimum check. This selection covers filtered identity,
exact spacing, native corner clipping, odd/even four-corner raster joins at
synthetic 96/120/144/192 DPI, caption widths/ellipsis and existing group padding/
divider behavior. These are separate focused runs, not an aggregate count.

Public-only Office and neutral consumers passed at actual 120 DPI under
`TestResults/visual/m5b-search-corners-office-final/` and
`TestResults/visual/m5b-search-corners-neutral/`. They additionally verify native
OK mouse messages applying/closing the dialog, right-list filtering/removal,
large-font Touch fields and actual short/long collapsed caption painting. The
first neutral attempt was interrupted while the user typed; only its successful
retry is evidence. Native and Showcase captures were inspected separately from
the synthetic raster checks. The normal Debug solution build had zero warnings
or errors. Showcase still exports the three-command default profile.
Final mouse/Touch options captures are in
`TestResults/visual/m5b-options-rounded-{mouse,touch}/`; `qat-narrow.png` shows the
minimum-width page clipping at the viewport, with native scroll access. The
caption consumer's short/long images and all four options-frame corners were
inspected at actual 120 DPI. The owner-window capture waits for the OS dialog
close animation; this is capture-tool timing, not a runtime animation change.

### Showcase size and owner centering — 2026-10-28

The Showcase default client size is now 1280 × 720 (previously 1080 × 540).
Options dialogs re-center on the actual owner after Load/DPI sizing has established
their final rectangle, then apply the existing working-area clamp. A host's Manual
StartPosition remains respected. The focused shown-window check passed modal and
modeless owner placement in `m5b-options-owner-centering.trx`; unrelated tests were
not repeated. The affected sample build and an updated Showcase capture verify
the larger default and owner-centered dialog.

Physical monitor transitions remain separate. No full suite, Release/package,
actual Visual Studio, external UIA/accessibility or WPF checks were run.

## M5C — Customize Ribbon — 2026-10-28

Use the supplied WPF layout: command list left, structural tree right, Add/Remove
between, reorder arrows beside the tree, Reset/Import/Export and New Tab/New Group/
Edit below. Keep the shared navigation shell, search frames, minimum width, density
and footer. Contextual/modal/merge content stays application-owned and outside
structural persistence. Groups remain QAT-only in the available catalog.

Public proposal before implementation: extend the native state with a version-two
Ribbon hierarchy (version-one QAT input remains supported); add scoped sessions,
the reusable Ribbon editor and an entry point selecting that page in the existing
options dialog. Ordinary tabs/groups retain their source objects; labels and user
visibility are presentation preferences. Custom groups use owned typed command
presentations borrowing original models/assets and forwarding actions and editor
validation to their sources. Native editor windows belong to one presentation;
original native editor handles and source bindings are retained. Import/export UI
raises host requests; the application owns file dialogs and storage. Apply/Cancel,
Reset, stale-state rejection and data validation keep the existing transaction
contract. Adding/reordering commands is limited to custom groups; builtin commands
remain read-only. No Simplified layout, comprehensive theme campaign, animation or WPF runtime work.

Implemented:

- `RibbonCustomizationEditor` supplies the available command list, tab/group/command
  tree, dual search, Add/Remove, sibling reorder and New Tab/New Group/Edit controls.
  Built-in commands are read-only; authored tabs/groups can be renamed and reordered
  within their parent, and ordinary tabs can be hidden while retaining one visible
  preference. Custom groups accept typed command presentations with label/size edits.
  Filters change the view only. Custom entries retain generated stable identities.
- `RibbonQuickAccessCustomizationForm` keeps its existing name/API and initial QAT
  selection, and now includes `RibbonEditor`. `ShowRibbonCustomization` and the shared
  context menu select Customize Ribbon. Application pages, owner centering, minimum
  content width, shared footer and independent per-page Apply remain intact.
- Scoped `BeginCustomization(applicationId, target)` sessions preserve the other
  page's live settings. The existing overload applies complete profiles. Export
  merges the scoped draft with live preferences from the other scope. Native JSON
  version two adds optional Ribbon structure; version-one/QAT-only input preserves
  live structure. Full Reset stages captured defaults for both scopes. Applications
  must assign explicit stable IDs to persisted authored tabs/groups and sources.
- Authored models, command collections, handlers and bindings retain their owners.
  User tab visibility and tab/group aliases are presentation preferences, rather
  than writes to bound source properties. Owned button/check/radio/split/dropdown,
  text/ComboBox and gallery presentations borrow source assets/state and invoke
  original actions, validation, commits, previews and selections. Native editor
  windows remain distinct per presentation; existing handles survive Apply/reorder.
- Preflight rejects missing definitions, unknown/transient commands, duplicate and
  cross-scope identities, incompatible group moves and stale drafts. Native input
  validation precedes structural mutation and is followed by another preflight.
  Apply-time failure restores topology/aliases/preferences. Reload recognizes host
  definition removals. Modal/contextual/merge state stays outside saved structure.
- Tree rows retain native selection, scrolling and keyboard expansion. Windows'
  fixed-size state/expand glyphs are replaced by themed checkbox painting and vector
  chevrons with matching pointer regions. Mouse uses an 18-logical-pixel checkbox
  and 12-pixel chevron; Touch uses 24 and 16, with larger surrounding targets. This
  avoids native state-ImageList downscaling. Office uses the existing rounded check
  indicator; the neutral renderer keeps its square indicator. This is a bounded
  options refinement, not completion of comprehensive theming or accessibility.
- Original transparent 24-viewBox [Import](../src/RibbonKit.WinForms/Assets/Icons/Import.svg)
  and [Export](../src/RibbonKit.WinForms/Assets/Icons/Export.svg) SVGs share rounded
  strokes and a tray/arrow motif. The runtime reads their owned polyline geometry
  and draws at current device size using existing secondary/accent theme roles;
  no general SVG dependency is added. The source SVGs are included in the package.
  Showcase owns the Import/Export file dialogs and JSON storage operations.
- The requested smaller button/navigation font experiment was reverted after
  visual review. Both pages, the Edit dialog, navigation and footer again inherit
  the original font sizes. The larger tree controls and SVG icons remain.
  Generated icon bitmaps are owned and disposed with their controls.

Focused evidence (separate selections, not an aggregate/full-suite claim):

- Normal Debug native solution builds passed with zero warnings/errors, including
  XML documentation, public API baseline and designer compilation.
- The initial customization/editor selection passed 30 tests. The subsequent
  `m5c-structure-boundaries.trx` selection passed 26 and caught one QAT dropdown
  ownership-status regression. Restoring its existing per-presentation contract
  passed both affected cases in `m5c-dropdown-ownership-final.trx`. Full-profile
  Reset and existing QAT reset/round-trip passed two focused cases in
  `m5c-full-profile-reset.trx`. Six button/row/font cases passed in
  `m5c-button-font-metrics.trx` during the subsequently reverted font experiment.
  Tests also cover original validation bindings,
  retained native handles, source actions, gallery identity, rollback, profile
  migration/legacy input, external definitions and view-only filters.
  The final gallery availability check passed in `m5c-gallery-availability-final.trx`.
- Public-only native consumers passed Office and neutral at actual 120 DPI in
  `artifacts/m5c-tree-{office,neutral}-final/`. They exercise tree pointer edges,
  single toggles, native keyboard expansion, last-visible protection, name/size
  editing, independent page Apply, Cancel-after-Apply, Reset, profile export,
  compact/Touch/larger fonts and narrow content. Captures were inspected. An earlier
  modeless-close capture lost foreground; only the successful guarded retry counts.
- The existing QAT/options native consumer passed the affected additional Ribbon
  page/OK semantics in `artifacts/m5c-qat-compatibility/`. Showcase captures and full
  profile export verified stable IDs and application page integration. The latest
  reverted-font experiment's mouse capture is `artifacts/m5c-final-fonts/`. Its QAT review passed in
  `artifacts/m5c-qat-fonts-final-retry/`; final Touch Showcase captures are in
  `artifacts/m5c-touch-fonts-final-retry/`. Earlier foreground-interrupted attempts
  are not passing evidence. Restored-font captures are in `artifacts/m5c-fonts-restored/`.

Physical mixed-monitor transitions, actual touch/IME hardware, external UIA and
screen readers, actual Visual Studio and a fresh package remain separate gates.
No full suite, Release/package or WPF build was run. Human visual acceptance of
M5C is still pending; icon feedback was positive and the font-size experiment was reverted.

Remaining work: M5C visual review; [M5D native row/customization review](M5D-SIMPLIFIED-RIBBON.md);
actual VS, fresh-package, physical mixed-monitor and separate accessibility gates.
WPF runtime/build/CI are unchanged. No commit, push or application file persistence
was performed by the runtime.

### Showcase command and group icons — 2026-10-28

The user requested artwork for every ribbon command, including replacement of
repeated placeholders and icons for groups. The sample now assigns semantic SVG
artwork to all 101 existing command/group declarations, including File/Backstage,
menu children, contextual/merge/modal sources and catalog-only commands. Gallery
previews and Mouse/Touch artwork remain. Message actions receive their artwork
when created. Original SVG sources live in `samples/RibbonKit.WinForms.Showcase/Assets/CommandIcons/`.

The proposed shared addition, `RibbonGroup.Icon`, borrows an `IRibbonIcon` and
disposes no artwork. The default renderer paints it above the caption/arrow in a
collapsed group and in QAT strip/overflow entries. Both customization views show
group icons. There is no Showcase-only renderer patch or native SVG dependency.
Command/group icon preferences do not enter customization JSON.

The normal Debug Showcase/runtime/design build passed with zero warnings/errors.
Four focused group icon cases passed at synthetic 96/120/144/192 DPI in
`group-icons-focused.trx`, verifying caption separation, bounds, QAT surfaces,
disabled state and borrowed ownership. The complete coverage list, SVG contact
sheet and generated tab/collapsed captures were inspected under
`artifacts/showcase-command-icons/`. These are generated captures, not physical
mixed-monitor/touch, actual VS or fresh-package acceptance. No broad suite was run.

### Larger standard popup icons — 2026-10-28

Standard dropdown/split menu artwork is now 24 logical pixels in Compact and 32
in Touch. Shared named metrics reserve the matching gutter, text gap and minimum
row height; popup width/description measurement and QAT group overflow use those
same metrics. Native editor rows, checkbox/radio indicator sizes and illustrated
choice artwork retain their existing contracts. No public API was added.

The affected menu/popup/group selection passed 20 checks; four new rendering cases
passed at synthetic 96/120/144/192 DPI in `popup-icon-bounds.trx`, verifying enlarged
artwork inside split primary rows and clear of text. The Debug sample/runtime/design
build passed without warnings/errors. Actual Compact/Touch command popup captures
in `artifacts/popup-icons-enlarged/` were inspected. The user requested continuation
after this refinement. Full-suite, physical mixed-monitor and hardware checks were
not repeated. Next implementation slice is M5D Simplified Ribbon.

### M5D first bounded step — 2026-10-28

The user's continuation starts [M5D](M5D-SIMPLIFIED-RIBBON.md). Its initial design
includes source/default ownership, scoped draft/Apply/Cancel, mode-specific options
eligibility and compatible native profile evolution. The internal selection
foundation now handles priority, stable source order, deduplication, group-run
dividers, reserved More Commands access and narrow-width fallback. Eleven focused
checks passed in `m5d1-simplified-selection.trx`. The native row, source presenters,
popup/editor integration, scoped persistence and editor were the subsequent D2/D3
scope completed below. D1 itself exposed no public mode or profile schema.

### M5D native mode and shared customization — 2026-10-28

The source-backed row and More Commands now use shared native layout/popup/editor
services. Mode is independent of density, QAT placement, minimization and profiles.
The Options dialog offers exactly one Customize Ribbon page for the current mode.
Simplified follows the WPF grouped-list reference, with independent tab selectors,
New/Edit/Reset menus and the shared footer Apply. Both modes edit one ordinary
tab/group structure; inline order/size choices remain separate. Classic structural
removal prunes invalid Simplified references atomically, while stale drafts reject
Apply and preserve pending edits. QAT scope stays independent.

The runtime exposes authored placement/size/priority and version-three optional
Simplified profiles, with older native profiles readable. Context pin/unpin shares
the same transaction. Original sources, handlers and native editor handles remain
authoritative. Showcase adds a mode toggle and representative default commands.

Normal Debug native solution build passed with zero warnings/errors. The 44-case
existing catalog/session/structure/options/selection scope passed, followed by nine
Simplified integration cases in `m5d-integration-final.trx`. Public-only consumer
checks passed in both native renderers at actual 120 DPI, Compact/Touch, including
row/More/full-group editor reuse, minimized geometry, applied shared structure,
mode-specific pending drafts and footer Apply/Cancel. Final captures and results
are under `artifacts/m5d-native-office/`, `artifacts/m5d-native-neutral/` and
`artifacts/m5d-showcase-final/`. The [active M5D plan](M5D-SIMPLIFIED-RIBBON.md)
records exact scope and remaining gates. No full suite, WPF/Release build, fresh
package, actual VS, physical mixed-monitor/touch/IME or accessibility acceptance
was run; human M5D visual review remains pending.

### Customization popup and ComboBox refinement — 2026-10-28

New/Edit/Reset now use the ordinary ribbon popup services with the Options form's
input/lifetime/DPI owner. They have no headings; checked commands use a plain tick.
The tab selectors and Classic command-size picker share ribbon ComboBox field and
choice-popup painting: rounded for Office2024, square for neutral. Native selection,
independent selectors, acceptance/cancellation and modal host-close behavior remain.
Eight focused Options cases, the existing editor integration case and two shared
menu/Alt+F4 regressions passed across the recorded focused/repair selections. Debug
native solution build passed without warnings/errors. Public-only native checks
passed at 120 DPI, Compact/Touch, in both renderers; inspected final captures are in
`artifacts/options-popups-office/` and `artifacts/options-popups-neutral/`. See the
[M5D evidence](M5D-SIMPLIFIED-RIBBON.md#evidence-and-pending-review) for scope and
unchanged broader gates. Human review remains pending; no full suite was repeated.

### Eight-item customization/header review — 2026-10-28

The requested Apply, popup/context, tree, custom group, activation and header/QAT
fixes are implemented. Older custom groups and new groups in either mode default
to Stacked. The [M5D review checklist](M5D-SIMPLIFIED-RIBBON.md#eight-review-fixes--2026-10-28)
records every item, the measured activation reproduction/fix, public profile
additions, focused/native evidence and remaining limits. Native 120-DPI checks
passed in both renderers and densities; Debug build is clean. Human review is
pending, and no full suite or broader acceptance gate was repeated.

The regular ribbon context menu now includes a plain-checkmark Simplified Ribbon
toggle, excluding QAT panel/items and their flyout origins. Nine focused checks,
clean Debug build and public-only native Compact/Touch checks in both renderers
at 120 DPI passed; [scope and captures](M5D-SIMPLIFIED-RIBBON.md#context-menu-layout-toggle--2026-10-28)
are recorded in the active plan. Human review remains pending.
