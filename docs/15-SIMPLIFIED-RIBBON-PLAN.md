# Simplified ribbon

> Active implementation plan. First layout slice authorized 2026-10-13.
> S2 customization implemented after the user's continuation on 2026-10-13.
> S3 shared custom-tab/group draft editor implemented and committed at 4ab4604.
> S4a–c committed at 4036aea. Follow-up review refinement implemented 2026-10-13;
> focused evidence below, live user review and full validation pending.
> S5 motion and mode-popup cleanup accepted 2026-10-13. S6 designer defaults
> authorized 2026-10-14 before the deferred full validation pass.
> S7 host defaults and RTL/Writer dialog integration implemented 2026-10-14;
> focused verification and user review below; full validation remains deferred.

## Layout contract

- Classic remains the default. Simplified is a single command row, independent
  of Compact/Touch density, theme, minimized state and QAT placement.
- Applications select commands for the row with `Ribbon.SimplifiedPlacement`.
  Higher `Ribbon.SimplifiedPriority` values stay inline longer on resize; equal
  priorities overflow from the end. Remaining commands are accessible through
  native full-group flyouts in the More Commands menu.
- `Ribbon.SimplifiedSize` chooses Small (icon only) or Medium (icon and label)
  independently of classic sizing. Medium is the default. Overflow copies retain
  their labels and accessible names/ScreenTips stay source-linked in either size.
  Galleries use the existing QAT native dropdown projection in both sizes.
- The original controls, bindings, commands, data and group structure remain
  source-owned. Shared command copies render supported inline buttons, toggles,
  splits, dropdowns, combo selectors and galleries. Arbitrary/native editors
  remain available in their original group flyout; an editable combo's inline
  copy opens its source-linked choice list rather than a second text editor.
- Sources are temporarily collapsed while their simplified full-group opener
  exists. Switching back releases that constraint, preserving QAT membership and
  the normal adaptive policy. No classic layout or customization is serialized
  from this temporary state.
- Shared templates/tokens own geometry, separators, More Commands, keyboard
  access and popup behavior. Showcase owns its selector and appearance preference.
- Simplified commands have vertical clearance through
  `RibbonKit.Metrics.SimplifiedCommandsMargin` (0,3) within the existing 42/52-DIP
  row. Its body scroller keeps `RibbonKit.Metrics.SimplifiedGroupsRowMargin` (4,1),
  with no extra horizontal toolbar inset. Touch commands retain 44-DIP targets.
  Body-card and lower-QAT/message continuation metrics keep their shared policy.

## Customize Simplified Ribbon

The user requested mutually exclusive customization surfaces: the simplified
page is available only while Simplified is active; normal Customize Ribbon is
available only while Classic is active. QAT customization is available in both.
The shared pages must enforce their own mode eligibility as well as the host's
options navigation and context menus. Switching mode while Options is open must
update eligibility and selection without discarding saved choices.
Following the user's review, the inactive page is hidden. Both layouts present
the same Customize Ribbon page title and one Customize the Ribbon context entry;
the active layout determines the editor and request event. The simplified tab
selector fills its selected-command column with no side label.

S2 implementation:

1. A lookless `RibbonSimplifiedCustomizePage` with a tab selector and two
   command lists: available commands (grouped by their original group) and
   selected inline commands. Reuse command catalog identity and metadata; group
   flyouts are access surfaces, not pinnable command entries.
2. Add/Remove, Move Up/Down, and reset for the current tab or all tabs, with a
   Show Label choice mapped to Small/Medium. An explicit per-tab ordered source
   list permits movement across groups. Dividers follow contiguous runs of the
   original groups, including repeated sections after reordering. Original
   `RibbonTab.Groups` and group items remain unchanged.
3. Pin to Simplified Ribbon / Remove from Simplified Ribbon in command
   context menus in Simplified mode, including overflow entries. Resolve original
   sources before deduplication, and disable unsupported inline editor types.
4. Simplified selections/order/display size are stored separately from classic
   tab/group layout and QAT customization, using stable `Ribbon.CommandId` identities and a
   optional `Simplified` serializer section (version 1). Tabs and commands need
   explicit IDs for restart persistence; untagged choices work during the session.
   Duplicate identities use the first eligible source. Missing commands are ignored safely;
   contextual/modal/merged transient commands are not persisted. Host-authored
   defaults remain the reset baseline. Loading classic-only data stays compatible.
5. The page works on a draft with no live preview. Apply/OK commits; Cancel/close
   discards uncommitted edits. An earlier Apply remains committed after Cancel.
   Standalone hosts use `ApplyChanges()` / `CancelChanges()`; the shared options
   dialog integrates this contract before notifying the host's Applied handler.
   Preserve per-layout choices across switches and restart; mode itself remains
   a host-owned appearance preference.
6. Focused verification covers page gating, pin/unpin from inline and overflow, duplicate identities,
   ordering, reset, cancellation, malformed/older JSON, missing commands,
   contextual/merged commands, RTL, localization and independent-consumer use.

### Custom tab and group editor

S3 uses the shared page, templates, classic identity helpers and name dialog:

- Tab/group selectors fill the selected-command column. All Groups retains the
  complete ordered row; choosing a group filters its selections and targets Add.
  Discovery covers the whole eligible ribbon with tab/group headings. Groups
  remain structure choices, never available command entries.
- New Tab stages a tab with one custom group; New Group stages an insertion
  after the selected group, or appends with All Groups selected. Rename Tab and
  Rename Group stage names. Footer actions wrap within the existing fill page,
  preserving room for Touch rows at short heights.
- Detached structures and source references remain drafts through retemplate
  and mode switches. Apply/OK attaches the structures and creates source-linked
  membership copies. Cancel leaves no live mutations or proxy bindings behind.
  Built-in groups accept selections of their own commands; new membership goes
  into custom groups. Names and membership appear in both layouts. The classic
  editor refreshes when it becomes enabled again.
- Simplified selection/order/labels remain a separate profile, keyed by source
  and target group. The same source can appear in different groups with distinct
  labels/order. Inline copies are cached by declaration, and inline/overflow
  context actions retain the target group. Remove/reset affect selections;
  shared tabs/groups and their command membership remain available. Whole-ribbon
  structure removal/reordering stays with the existing Classic editor.
- Serializer version 2 records GroupId alongside SourceId/Size and reads version
  1 by inferring the first eligible group. Stable group IDs preserve assignments;
  untagged built-in groups retain source-only fallback. Unknown group IDs skip
  their commands, and malformed shapes reject Apply before any live mutation.
  Sources are resolved again when a draft is applied, preserving current host
  replacements and skipping missing/transient commands. Absent/unknown optional
  sections remap existing profiles across rebuilt custom tab/group identities.
- The only public additions in this slice are four localized string identifiers
  (AllGroups, SimplifiedGroup, RenameTab, RenameGroup); their values append to the
  baseline. Existing page/API/event contracts provide the host integration.

### S4 editor redesign — authorized 2026-10-13

The user's S3 review requested a less crowded Customize Ribbon page and then
authorized implementation. S4a–c implement the following contract in the shared
editor, retaining the shared page title and mode-based navigation. Focused
evidence is recorded below; physical input and user acceptance remain open.

#### Columns and selection

- Give each column one full-width tab selector immediately beneath its heading,
  with no label beside the selector. Both selectors and lists align vertically.
  The left selector filters available commands by source tab; the right selector
  chooses the destination tab being customized. Initialize both to the active
  eligible tab, then let them change independently. Include an All Tabs source
  choice for discovery. Changing the source filter preserves destination
  selection, draft edits and order. The 2026-10-13 follow-up review excludes
  custom tabs/groups from this source catalog and adds Commands not in the ribbon
  for retained QAT-only declarations, deduplicated against authored commands.
- Keep the left list grouped by source group with native pixel scrolling.
  Single-tab filtering needs only group names; All Tabs uses Tab > Group paths.
  Available entries remain commands; a group header is not an addable command.
  Preserve the existing exclusions for contextual, modal and merged content.
- Replace the right Group selector with an ordered list containing selectable
  group headers and command rows. Empty groups retain a selectable header, so a
  newly created group is an immediate Add target. Selecting a command targets
  its group; selecting a header targets that group. Without a row selection, Add
  uses the command's existing destination membership when it is unambiguous.
  If multiple destination groups qualify, require choosing a header.
- Prefer this header list over a conventional tree: the current profile permits
  Clipboard > Font > Clipboard order. Render headers for contiguous runs,
  including repeated headers when a group recurs; selecting either header
  identifies the same group. Insert empty-group headers without changing command
  order. Do not regroup existing profiles into unique-group blocks when opening,
  saving or importing them. A header's selection replaces the old Add-target
  selector; Show Label does not depend on a Group selector.

#### Reordering and compact actions

- Make right-side command rows draggable, with an insertion indicator and
  edge autoscrolling. A drop changes simplified row order and preserves source
  and target-group identity; crossing a header can create a new contiguous run.
  Use the existing Add rules for custom-group membership and duplicate checks.
  Keep Move Up/Down as a keyboard-accessible equivalent with the same ordering
  semantics. Preserve selection/focus and bring the moved row into view.
- Put **New**, **Edit** and **Apply** below the right list. New opens a menu with New Tab
  and New Group. New Tab creates a tab with one custom group; New Group inserts
  after the selected group or appends when no group is selected. Select the new
  destination immediately. Creation and names remain staged until Apply/OK.
- Edit opens a menu with Rename Tab and Rename Group, using the shared edit
  dialog and selection to determine the group. Custom groups expose icon/layout
  controls and stage those changes alongside their name. For a selected command, also show
  a checkable **Show item label** entry mapped to Medium/Small. Remove the inline
  checkboxes from command rows. Label state belongs to the selected source/group
  occurrence, preserving independent copies in other groups. Menu availability
  follows selection, and its checked state refreshes whenever it opens.
- Keep Add/Remove between the lists and Move Up/Down beside the right list. Group
  header selection supports targeting/naming; command selection supports removal,
  ordering and label editing. Reuse the existing simplified removal contract:
  removing an inline command retains its shared custom-group membership.

#### Utility actions and reset scopes

- Place **Reset** and icon-only **Import/Export** under the left list, aligned
  with the right-side New/Edit/Apply row. Both sides wrap independently at narrow
  widths while retaining standard button
  target sizes. Reserve footer height before sizing the lists so Touch rows and
  scrollbar targets remain usable at short heights. No standalone Reset Tab,
  Reset All, New Tab, New Group or Rename buttons remain in the standard template.
- Reset opens one menu with explicit scopes:

  | Entry | Draft result |
  | --- | --- |
  | Reset this tab | Restore the destination tab's authored simplified selection, order and labels; retain shared tab/group structure and membership |
  | Reset all tabs | Restore all simplified profiles; retain shared structure, membership and QAT |
  | Reset Quick Access Toolbar | Restore authored QAT membership, order, position and visibility; retain ribbon customization |
  | Reset all customizations | Restore the host's complete customization baseline: shared/classic structure and properties, simplified profiles and QAT |

  The separate All Tabs and All Customizations entries preserve the current
  lightweight reset and make the proposed "reset everything" scope explicit.
  QAT/full reset use an authored host baseline, never a capture of already
  customized state. Disable those entries when no baseline is supplied. A reset
  only changes the draft; Cancel discards it and Apply/OK commits it.

#### Import, export and draft safety

- Reuse the existing customization JSON format and native file dialogs. The file
  covers shared/classic structure, simplified profiles and QAT; theme and ribbon
  layout mode remain host appearance preferences. Continue reading simplified
  versions 1 and 2 and handling missing source/group IDs safely.
- Import validates a file into a detached draft, replacing the pending
  customization shown by this editor. Refresh the selectors, group headers and
  row choices without mutating the live ribbon. Report unreadable or invalid
  files while retaining the previous draft. Imported changes, including QAT and
  classic fields, commit together on Apply/OK and are discarded on Cancel.
- Export writes the current draft, including pending creation, renaming,
  ordering, labels and resets. Build that snapshot without applying it to the
  live ribbon. Exporting a file does not mark the draft as applied.
- Extend the current draft model to stage imported/reset fields as well as
  structures and simplified profiles. Rebase untouched fields onto current live
  state before Apply so editing another options page does not restore unrelated
  fields from an old snapshot. Later edits on the QAT/classic page take precedence
  over an older pending reset/import of the same scope; clear that superseded
  pending operation and refresh its draft view. Test this cross-page sequence.
- Bounded public addition: an optional `ResetLayout` dependency property
  on `RibbonSimplifiedCustomizePage`, matching the existing Classic page's host
  baseline contract. Showcase supplies its existing startup baseline; another
  consumer can supply its own. Draft parsing/snapshot helpers remain internal.
  Document added template parts and localized strings, retain existing
  template-part hooks as compatibility paths, and record intentional public
  additions through the normal API baseline. Drag behavior stays internal.

#### Implementation order and acceptance

1. **S4a — page presentation:** source-tab filtering, selectable ordered group
   headers, New/Edit menus, label choice in Edit, and responsive action rows.
   Preserve existing draft, membership and ordering behavior before adding drag.
2. **S4b — interaction:** command drag ordering, insertion/autoscroll, keyboard
   equivalence, menu focus/Escape return, and selection retention. Keep native
   pixel scrolling and mode/template draft survival covered.
3. **S4c — persistence utilities:** baseline integration, scoped Reset menu,
   detached Import and draft Export, and cross-page edit precedence. Implement
   the bounded baseline-property proposal above with XML documentation and API
   baseline coverage. Import and QAT reset use the same draft safety contract.

Use relevant focused checks for each implementation slice. Cover independent
source/destination selectors, empty and repeated group headers, same-source
copies in different groups, drag versus Move Up/Down, label/menu state, Apply/
Cancel, reset scopes, malformed/legacy imports, draft export round trips, and
later QAT/classic edits. Verify shared themes, Compact/Touch, LTR/RTL, narrow/short
pages and an independent RibbonKit consumer. Inspect rendered evidence and ask
for user review of the revised layout and drag behavior. Full-suite testing
remains reserved for the end; physical input and genuine monitor-DPI acceptance
remain separate gates.

## Delivery and acceptance

| Slice | Scope | Status |
| --- | --- | --- |
| S1 | Shared layout/selection/priority, native full-group access, keyboard/KeyTips, Showcase toggle and classic-customization gating | Implemented; user accepted the current visuals 2026-10-13. Native-input/DPI and earlier combined/full-suite limits remain open |
| S2 | Dedicated customization page, pin/unpin, separate ordered state and persistence | Implemented; user accepted scrolling/focus repairs 2026-10-13 and committed them at 1a2a743. Broader end-of-feature gates remain below |
| S3 | Shared custom-tab/group creation from the simplified editor | Implemented and committed at 4ab4604; user review 2026-10-13 requested the S4 page redesign |
| S4 | Source filtering, ordered group headers, compact menus and draft-safe persistence utilities | S4a–c committed at 4036aea; catalog/footer/dialog/default refinements and custom-tab positioning implemented 2026-10-13; focused evidence below; awaiting live user review |
| S5 | Shared visible layout/density collapse and simplified tab switching | Implemented; original and follow-up motion review recorded below; broader native/full gates remain open |
| S6 | Designer defaults and per-command simplified visibility/label controls | Implemented; focused evidence and user review below; full/package/native gates remain deferred |
| S7 | MDI/RTL/Writer defaults and RTL/Writer simplified customization integration | Implemented; focused consumer checks and accepted live review below; broader full/native gates remain open |

S1 requires focused runtime regressions, a RibbonKit-only consumer matrix, normal
Debug Showcase build, Release solution build/test for the additive public API,
and review of rendered evidence. Native input and mixed-monitor DPI acceptance
remain explicit live gates.

### Recorded verification — 2026-10-13

These results record the earlier implementation checkpoints on
`codex/simplified-mode`; S3 is committed at `4ab4604`.

User-requested validation policy: run affected builds and relevant focused checks
during implementation; reserve full-suite testing for the end. The full run below
is the earlier S1 checkpoint and has not been repeated for this polish slice.

| Check | Result |
| --- | --- |
| Normal Release solution build | Passed, 0 warnings/errors |
| Normal Debug Showcase build | Passed, 0 warnings/errors |
| Release focused `SimplifiedRibbonTests` and `ProxyMirrorTests` | 15/15 passed; includes command/state/bindings, priority and focus suppression, native group/editor access, menu return, dynamic membership, initial mode, separators, combo selection, KeyTips, keyboard opening and customization eligibility |
| Release `SimplifiedRibbon` independent-consumer scope | 1 aggregate test passed: 224 cases over seven themes, light/dark, Compact/Touch, LTR/RTL, and simulated 100/125/150/200% root DPI; only public RibbonKit resources/APIs |
| Full Release run | 1,163 passed, 2 failed, 1,165 total. Runtime 678/678 and visual project 5/5 passed; Writer 480/481 and portability 0/1. Final Showcase-declaration rendering test and additional keyboard assertions were validated afterward in the focused run |
| Render inspection | Inspected classic/simplified fixture, More Commands, and the actual Showcase Home XAML declaration with its icons. No snapshot approvals/tolerances changed |
| User visual review | 2026-10-13: user reported the simplified row looks good on every theme, with missing overflow borders in Office 2007/2010; requested Small icon-only commands and approved reuse of QAT gallery dropdown behavior |
| Baseline comparison | Unchanged HEAD built in a temporary checkout. Focused Touch and Writer checks each passed on baseline and feature branch. The baseline unfiltered consumer aggregate was stopped before completion and establishes no aggregate pass |

The full-run failures are
`MainWindowIntegrationTests.WriterSharedMaterialsEffectsOrbAndOpenSurfaceDpiReturnsStayBounded`
at the open-popup DPI assertion (line 300), and the portability aggregate's
`TouchDensityPortabilityChecks.VerifyPopups` at its synthetic dropdown touch
assertion (line 337). Passing isolated runs does not clear these full-suite
failures or prove their cause. Logs are in repository `TestResults/simplified-*.log`.
Rendered PNGs are in the runtime test project's normal Release output under
`TestResults/simplified/`, including `showcase-classic.png` and
`showcase-simplified.png`.

### S1 polish verification — 2026-10-13

Implemented Small/Medium inline sizing, the Showcase icon-only commands and
Styles dropdown, Office 2007/2010 overflow state borders, and a plain-gallery
presenter restoration fix for reopening from inline to overflow. The latter uses
the returned presenter's logical viewport ownership when its hidden source has
not been measured again.

| Check | Result |
| --- | --- |
| Normal Debug library (net8/net9) and Showcase builds | Passed, 0 warnings/errors |
| Release focused `SimplifiedRibbonTests` and `GalleryQuickAccessTests` | 31/31 passed; covers Small/Medium changes, labeled overflow, accessible names, both gallery types, native presenter borrowing/restoration and legacy overflow border tokens |
| Release `SimplifiedRibbon` independent-consumer scope | Passed: 224 combinations with Small commands and both gallery types, plus two inline-to-overflow gallery handoffs |
| Focused render capture and inspection | 3/3 checks passed; inspected Office 2007/2010 open overflow borders and the actual Showcase Home declaration with icon-only B/I/U and Styles dropdown |
| Full suite | Deferred until the end, per the user's instruction; earlier S1 failures remain recorded above |

Logs are `TestResults/simplified-polish-*.log`; PNGs use the same normal Release
output folder described above. No snapshot approvals or tolerances changed.

### S1 body spacing verification — 2026-10-13

The user requested tighter body margins/padding after comparing Showcase with
Office. The simplified toolbar and More Commands now fill the 42/52-DIP row;
removing its extra side inset and reducing the scroller margin to 4,1 gives the
commands more usable height without increasing the ribbon height.

| Check | Result |
| --- | --- |
| Normal Debug library (net8/net9) and Showcase builds | Passed, 0 warnings/errors |
| Release focused `SimplifiedRibbonTests` | 16/16 passed, including resize/overflow, density switches, keyboard, gallery and legacy-border cases |
| Release `SimplifiedRibbon` independent-consumer scope | Passed: 224 combinations plus both gallery handoffs; measured full-height inline/More surfaces, simplified inset and classic-margin restoration across themes, light/dark, density, RTL and simulated root DPI |
| Render inspection | Compared the Showcase Home declaration before/after spacing; inspected open Font Size and checked Bold surfaces in `showcase-simplified-spacing.png` |
| Full suite | Deferred until the end; the earlier S1 checkpoint and its failures remain above |

Logs are `TestResults/simplified-spacing-*.log`. Human review of this spacing and
native mixed-monitor/input acceptance remain open.

### S1 overflow menu presentation — 2026-10-13

The user requested normal menu spacing for QAT/simplified overflow and combo
choices, with child popups beside the parent menu and a trailing submenu arrow.
Shared rows now have 32-DIP minimum height (44 in Touch) and 200-DIP minimum width.
Both overflow hosts apply this to command copies, including dropdowns, splits,
combos, galleries and groups. Native menu/combo choice rows use the same tokens.
The monitor-work-area policy prefers right in LTR/left in RTL, flips to the fitting
side at screen edges, and uses the overlaid fallback if neither side fits.

| Check | Result |
| --- | --- |
| Normal Release library / Debug Showcase builds | Final net8/net9 Release library and net8 Debug Showcase passed, 0 warnings/errors |
| Combined focused runtime slice | 98/99 passed over overflow, simplified, combo/group QAT and Touch adaptive checks; one existing `Qat_popups_leave_the_original_outside_press_available_to_commands_and_file(kind: "group", file: True)` failed at line 381 |
| Outside-press retry | All eight selected cases passed in isolation; this does not clear the combined-run failure or establish its cause |
| Final overflow presentation regressions | 16/16 passed, including actual popup-frame geometry at both screen edges, oversized fallback, RTL, Touch, menu rows, native/projected combo choices, submenu keys, split invocation, cached reopening and group placement restoration |
| Final independent `OverflowMenus` consumer scope | Passed after the final placement correction: 112 QAT/simplified combinations over seven themes, light/dark, density, direction and sampled simulated 100/150% root DPI, using only public RibbonKit resources/APIs |
| Render inspection | Inspected both overflow hosts with side content and trailing arrows; captures are in normal Release test output under `TestResults/overflow-menus/` |
| Full suite | Deferred until the end, per user instruction |

Logs are `TestResults/overflow-menus-*.log`. Human visual/native input and genuine
mixed-monitor DPI review remain open. The earlier full-suite failures and this
combined-run outside-press case remain explicit verification limits.

### S1 visible joins and button spacing — 2026-10-13

User screenshots showed detached left-opening popups and inconsistent group
placement. Positioning now joins the actual painted borders rather than their
transparent allocations, with the native group's `PART_PopupHost` supplied
explicitly through its adorner wrapper. Screen-fit checks still use the full
frame. Vertical anchoring uses the final arranged row slot so the opening content
slide and native group loading do not freeze an intermediate position.

The user clarified the spacing request concerns ribbon buttons. Added the shared
`SimplifiedCommandsMargin` (0,3) inside the existing row, keeping the outer body
inset 4,1 and Touch targets at least 44 DIP. No ribbon-height increase is needed.

| Check | Result |
| --- | --- |
| Normal Release library / Debug Showcase builds | net8/net9 Release library and net8 Debug Showcase passed, 0 warnings/errors |
| Focused runtime | Earlier 34/34 overflow/simplified cases passed; final 20/20 presentation checks include both overflow hosts/edges, actual painted-border joins for groups and combo/dropdown/split copies, oversized fallback, and frozen parent-slide group/combo cases |
| Independent simplified spacing scope | 224 combinations plus both gallery handoffs passed; includes visible inline command geometry and preserved Touch targets |
| Final independent overflow scope | 112 combinations passed with painted-border joins across command, split, combo, both gallery types and group popups, plus simplified command-clearance geometry |
| Render inspection | Inspected updated Showcase button spacing and left-opening QAT combo/simplified group joins. Explicit arranged viewboxes corrected the capture's effect-expanded brush scaling; no snapshot approvals/tolerances changed |
| Full suite | Deferred until the end; previous combined/full-run limits remain recorded above |

Logs are `TestResults/overflow-join-*.log`; popup captures remain under normal
Release test output `TestResults/overflow-menus/`. Human visual/native input and
genuine mixed-monitor DPI review remain open. On 2026-10-13 the user accepted the
current visual fixes ("everything's is perfect"); this confirms the reported
button-spacing and submenu joins, not unreported native keyboard/touch/DPI gates.

### S2 customization — 2026-10-13

Showcase now hosts the shared draft editor next to Classic and QAT customization
and persists applied/pinned changes through its existing customization file.
Both layout-specific pages enforce mode eligibility even when detached; the
options navigation updates and selects the eligible counterpart after a switch.
Choices survive switches without mutating the classic/QAT layout. Native merge
contributions retain their authored inline policy and stay outside saved choices.

The shared API additions are the lookless page with a Ribbon property and
ApplyChanges/CancelChanges, PinToSimplifiedRibbon/RemoveFromSimplifiedRibbon,
and the customization-request/change events. XML documentation and the additive
unshipped API baseline record this bounded public surface.

| Check | Result |
| --- | --- |
| Builds | Final normal Release solution and Debug Showcase builds passed, 0 warnings/errors; includes net8/net9 runtime targets |
| Runtime | 39/39 customization, serializer and classic-dialog cases; 42/42 final page/localization cases; 18/18 source-state cases (including all 16 existing simplified-layout cases); 21/21 final custom-tab/overflow cases; 11/11 final malformed/legacy JSON cases; final page render/automation-name check passed. These are overlapping focused selections, not a full-suite result |
| Independent consumer | 112 combinations passed across seven themes, light/dark, Compact/Touch, LTR/RTL and simulated 100/150% root DPI; public RibbonKit controls/resources only. Includes constrained dialog geometry, draft/apply/cancel/reset and live mode/navigation gating |
| Render inspection | Inspected the shared page in Office 2024 LTR and Crystal RTL. The tab selector uses descriptors rather than reparenting actual RibbonTab controls; full-window captures keep the arranged page and action rows visible |
| Full suite | Deferred until the end; earlier S1 combined/full-suite failures remain open |

Logs use `TestResults/simplified-customization-*.log`. Render captures are in the
runtime test project's normal Release output under `TestResults/simplified-customization/`.
No snapshot approvals or tolerances changed. Human visual/native review of S2 is pending.

Copied commands on custom tabs prefer the active tab for pin/unpin. Authored
placement, size, visibility, labels and enabled state come from that tab's
declaration; persistence resolves the original source ID. The focused custom-tab
case covers rebuild/load, disabled groups and renamed inline/overflow captions.
The legacy classic-dialog geometry fixture now explicitly selects Classic and
suppresses preference writes instead of depending on the user's saved layout.

### S2 review fixes — 2026-10-13

Implemented the user's layout/seamless-navigation and utility-focus review:
full-column tab selector without a side label, consistent column headings and
trimming, shared Customize Ribbon titles/context label, and mode-based hiding of
the inactive page. Visibility observes mode independently of host enablement.

QAT overflow is now focusable and follows visible commands. Both toolbar
templates put the command presenter before overflow in Tab/Shift+Tab order.
Arrow keys enter overflow for both kinds; Escape returns to its opener through
an optional dismissal callback, leaving outside-press dismissal unchanged.
The full-width lower QAT keeps its existing non-overflow policy.

Flat pressed/open utility rims use separate pressed-opacity and checked-border
tokens in all palettes. Regular Office retains zero opacity; Crystal retains
its glass drawing and pressed-over-checked priority. Hover behavior and layout
geometry are preserved. Custom-tab/group creation is the proposal above.

| Check | Result |
| --- | --- |
| Normal Release solution / Debug Showcase | Passed, 0 warnings/errors |
| Runtime focus/utility cases | 10/10 passed: title/tab-row QAT and simplified order, both densities/directions, arrow opening/Escape return, Flat light/dark pressed/open rims and Office/Crystal restoration |
| Runtime editor/menu/localization/overflow cases | Larger selection passed 83/84; the one failing test assumed different Classic/Simplified captions. Updated it to assert one seamless menu entry and correct event routing; its focused retry passed 1/1. Final mode/visibility and classic-dialog checks passed 4/4. This is assembled focused evidence, not a clean combined/full run |
| Independent editor consumer | 112 theme/dark/density/direction/simulated-DPI cases passed, including selector widths at 560/700/900 pixels and inactive-page hiding |
| Independent Flat consumer | Eight light/dark, density and direction combinations passed with native pressed/open utility-rim checks |
| Selected visual comparisons | Final 6/6 QAT/utility/Flat scenes passed with unchanged approvals/tolerances. Initial Crystal QAT mismatch's actual/diff showed startup text/rounding drift; initializing rendering before its disconnected fixture restored its existing approval |
| Render inspection | Inspected responsive editor and Flat utility-state captures; native input and user visual review remain pending |
| Full suite | Deferred until the end; earlier S1 combined/full-run limits remain open |

Logs are `TestResults/simplified-customization-*`. Captures remain in normal
Release test output under `TestResults/simplified-customization/`; the initially
failing actual/diff are retained in repository `TestResults/visual/`.

### Grouped-list scrolling and overflow focus — 2026-10-13

The simplified editor's available and selected lists now use native pixel
scrolling. Group headings no longer become whole-group scroll steps; small
scrollbar offsets and line scrolling retain partial rows. Selecting the last
command still brings it into view. This is scoped to the simplified page.

Both QAT and simplified overflow transfer focus after their entries are realized
on every opening path, including click, Enter/Space and KeyTips. Disabled entries
are skipped; dropdown/group entries can focus their native inner button. Up
enters the last eligible entry, other opening paths enter the first, and Escape
returns to the opener. Closing/reopening/retemplating invalidates pending focus
requests so dismissal cannot later steal focus from another control.

| Check | Result |
| --- | --- |
| Focused grouped/selected scrolling | 2/2 passed, with both densities inside each direction case: fractional offsets, native line scrolling and last-command visibility |
| Focused overflow navigation | Final 10/10 passed: both hosts, directions and densities, native toggle click handler, synthetic Enter/Space/Up/Down, disabled-entry skipping, Tab navigation, Escape and cancellation of deferred focus |
| Related runtime regressions | Larger selection passed 74/75; one new focus case failed. Its isolated retry passed, then the focus fixture was given an active owner window and the final ten-case selection passed. This is assembled focused evidence, not a clean combined/full run |
| Independent editor consumer | 112 theme/dark/density/direction/simulated-DPI combinations passed, now with two source groups, fractional scrolling and last-command visibility |
| Independent overflow consumer | 56 theme/dark/density/direction combinations passed in both ribbon modes, including opening focus and Escape return. Root DPI is 100% for light and 150% for dark. Focus checks start from an active window and focused opener; the initial inactive-window fixture had no keyboard focus |
| Builds | Normal Release runtime, net8/net9, and normal Debug Showcase passed with 0 warnings/errors |
| Full suite and live review | User accepted these scrolling/focus repairs 2026-10-13. Full testing remains deferred until the end; broader native/DPI gates remain below |

Logs are `TestResults/simplified-scroll-focus-*.log`. The repair is committed at
`1a2a743` after the user's confirmation.

### S3 focused evidence — 2026-10-13

| Check | Result |
| --- | --- |
| Related runtime baseline | 83/85 initially passed; the two scrolling cases exposed a Touch viewport smaller than one row. Consolidating the footer repaired both |
| New structure/migration/cancel cases | Initial selection passed 10/13; the two scrolling cases and one invocation assertion were repaired in a 3/3 focused run. Invocation assertions now drain the native UIA dispatch before counting calls. This is assembled evidence, not a clean combined/full run |
| Final presentation/localization | 6/6 passed: responsive columns, native rename dialog, live host/provider names, mode/template draft retention and bounded Office/Crystal rendering |
| Final scrolling | 2/2 passed after restoring standard footer button minimum widths, with Compact/Touch inside both direction cases |
| Source replacement/removal during drafts | 2/2 passed: membership binds to the current source object or skips a removed command |
| Legacy/unknown optional sections | 13/13 passed, including profile preservation across recreated custom tabs/groups and existing malformed/absent-section cases |
| Copy targets | 4/4 passed: explicit copies retain their tab/group even when another tab is active; original commands retain active-tab preference; inline/overflow removal leaves other group copies selected |
| Overflow/keyboard regressions | 30/30 passed, including both hosts, entry focus/Escape, menu sizing, submenu placement/fallback and nested source restoration |
| Independent consumer | Final 112 theme/dark/density/direction/simulated-DPI cases passed: creation/cancel/apply, two custom groups, source membership, serialization/restoration, reset preserving structure, both selectors and all footer actions at 560/700/900-pixel widths |
| Selected visual comparison | Existing Office2024 RTL QAT customization scene passed 1/1 with unchanged approval/tolerance |
| Render inspection | Inspected the S3 independent-consumer draft with named tab/group and the RTL editor capture. Capture waits for layout after modal rename |
| Builds | Normal Release solution and Debug Showcase passed, 0 warnings/errors |
| Full/native acceptance | Deferred: full end-of-feature suite, physical input/mixed-monitor checks, and user review of S3 creation/naming/membership and restart behavior |

Logs are `TestResults/simplified-structure-*.log`; rendered evidence lives in the
normal Release test outputs under `TestResults/simplified-customization/`.
S3 is committed at `4ab4604` (Simplified mode custom tab + custom group WIP).

### S4 editor redesign evidence — 2026-10-13

Implemented independent source/destination selectors, selectable contiguous and
empty-group headers, command drag reordering with an insertion marker and edge
scrolling, compact New/Edit menus, and the separate Reset/Import/Export/Apply row.
The standard template inherits owner ribbon density and keeps Touch rows/actions
at least 44 DIP. Existing custom-template parts retain their compatibility paths.

Import projects a validated layout into detached tab/group descriptors and source
references; no preview proxies borrow native content. Export overlays draft changes
onto the current capture without applying it. Apply commits the snapshot once;
Cancel discards it. QAT-only reset applies just QAT fields, preserving live group
objects and untagged session choices. Later live edits supersede an older pending
import/reset of the same ribbon or QAT scope. Missing/transient custom membership
is filtered from the draft and its exported file. Duplicate tab IDs or group IDs
within one tab reject the file; group IDs can recur in separate tabs.

The only public additions are the optional host-authored `ResetLayout` property
and six appended localized string identifiers. The property has XML documentation
and an additive API baseline entry; Showcase supplies its existing startup capture.

| Check | Result |
| --- | --- |
| Existing editor/serializer selection | 63/63 passed after updating assertions to use menu actions and selectable headers |
| Initial new workflows | 12/14 passed; native UIA Toggle exposed a draft label-update bug, repaired by observing Checked/Unchecked. The other failure assumed TitleBar rather than the fixture's captured TabRow baseline; the assertion now uses the authored baseline. The two repaired checks passed 2/2 |
| Drag and imported snapshot follow-up | 6/6 passed: actual WPF drag-event routes, LTR/RTL insertion geometry and edge scrolling, stale/foreign rejection, later Classic edits, draft round trip and one commit notification |
| Final editor selection | 30/30 passed after Touch targets and selection remapping: menu focus/Escape, native UIA labels, drag events, legacy/unknown imports, scopes, cancellation, source filtering, smooth scrolling, localization and responsive selectors |
| Final import edges | 8/8 passed after filtering missing/transient membership from the exported draft; includes contextual/modal sources, group IDs scoped to tabs, older profiles, export round trip and existing merge-rebuild behavior |
| Independent consumer | Final 112 theme/dark/density/direction/simulated 100/150% root-DPI combinations passed, using public RibbonKit controls/resources only. Covers 560/700/900-pixel widths, 44-DIP Touch actions, source filtering including staged custom tabs, native label toggle, creation/naming, resets, cancellation and persistence |
| Render inspection | Inspected the actual Showcase Customize Ribbon page and shared Office/Crystal RTL captures. No snapshot approvals or tolerances changed |
| Builds | Final normal Release solution and Debug Showcase builds passed, 0 warnings/errors; includes both runtime target frameworks |
| Full/native acceptance | Full suite remains deferred until the end. Physical drag, native file-picker use, mouse/touch/keyboard review and mixed-monitor DPI acceptance remain open |

These are overlapping focused selections, not a clean combined/full-suite result.
Logs are `TestResults/simplified-editor-redesign-*.log`; rendered PNGs are in the
normal test outputs under `TestResults/simplified-customization/`, including
`showcase-customize-redesign.png`. S4 is committed at `4036aea`.

### S4 review refinement — authorized 2026-10-13

The user approved the five follow-up requests and the defaults/commit boundaries:
authored source catalog plus retained QAT-only commands; balanced column actions;
shared final-size dialog centering; shared custom structures with neutral icons,
Stacked creation defaults and hidden labels for new normal-mode command additions;
and authored simplified defaults across Showcase tabs. Existing labels/icons/Large
layouts remain user choices. Simplified names/icons/layouts/imports are drafts
until Apply/OK, with Cancel discarding them. Normal editing retains its existing
Options transaction and snapshot restoration contract.
The only public addition in this refinement is the appended
`RibbonString.CommandsNotInRibbon` localization identifier; no persistence schema
version changed. Saved simplified profiles retain precedence over the new Showcase
authored defaults; Reset this tab previews the revised defaults.

| Check | Result |
| --- | --- |
| Dialog reproduction | All three section-size cases reproduced the vertical offset before the fix; all three passed after moving final sizing before CenterOwner positioning |
| Initial focused editor/serializer/QAT selection | 103/106 passed. Three new checks exposed missing neutral icons on detached groups; repaired in the shared resource lookup |
| Resource repair | 4/4 passed: both draft/import metadata paths, native full group dialog, and normal additions preserving existing labels/Large layout |
| Final serializer/resource follow-up | 28/29 passed. The remaining existing picker test expected only its QAT icon; the new neutral default intentionally adds a second choice. Updated coverage verifies both distinct choices and deduplication; the repaired contract and native full dialog passed 2/2 |
| Independent consumer | 112 theme/light-dark/density/direction/simulated 100/150% root-DPI combinations passed, using public RibbonKit resources only. Includes 560/700/900 widths, Touch targets, QAT-only discovery after removal, source/destination separation, group icons/Stacked defaults, native group editing and persistence |
| Showcase defaults | Added 27 prioritized inline policies across authored ordinary, contextual/modal and merged content. Focused runtime coverage confirms defaults on authored ribbon tabs |
| Render inspection | Inspected actual Showcase editor, independent consumer, Crystal RTL page and full group dialog captures. Footer columns align; the neutral vector icon and Stacked selection render. No approvals/tolerances changed |
| Builds | Normal Release solution (net8.0/net9.0 runtime targets) and Debug Showcase builds passed, 0 warnings/errors |
| Full/native acceptance | Full suite remains deferred until the end. Live user review, physical drag/file picker, genuine mixed-monitor DPI and native mouse/touch/keyboard acceptance remain open |

These are overlapping focused selections and scoped repairs, not a clean combined
or full-suite run. Logs are `TestResults/simplified-review-*.log`; captures remain
in normal test outputs under `TestResults/simplified-customization/`. This review
refinement is uncommitted.

### Custom-tab positioning — authorized 2026-10-13

The user approved **Edit → Move tab…** with Before/After and a target-tab selector.
It is available for custom destination tabs when another eligible tab exists.
Targets exclude the moving tab and contextual/modal/merged content. The shared
internal modal reuses edit-dialog chrome, final-size centering, native selectors,
localized bindings, keyboard focus, Enter/Cancel actions and Touch targets.

Creation and positioning use chronological draft placements. This keeps a tab
created after a moved tab in the requested position. Moves, repeated placements
and names survive retemplating/mode changes; Cancel clears placements; export
includes draft order; import and Apply use the existing Tabs sequence. Apply
resolves current tabs/anchors by reference or stable ID, skips removed anchors,
and preserves existing objects, command profiles/QAT and the active tab. No
persistence version or public runtime-control API changed; five localized string
identifiers were appended.

| Check | Result |
| --- | --- |
| Initial move selection | 4/8 passed. Active-tab selection reset during Apply required preservation around collection moves. Two modal-input harness callbacks ran while opening the menu rather than the dialog; scheduling now starts after opening the menu |
| Selection/dialog repair | 4/4 passed, covering both before/after moves and native LTR OK/RTL Cancel |
| Modal reproduction | Moving a custom tab during modal mode incorrectly restored it as Collapsed. Reproduced 1/1 before fixing Move notifications to preserve modal/merge membership and authored visibility |
| Final relevant runtime selection | 31/31 passed: positioning, chronological creation, export/import, cancellation, missing/replaced anchors, original objects/profiles, source separation and existing merge/modal/selection regressions |
| Dialog/provenance follow-up | 3/3 passed: Compact/Touch final-size owner centering, live title/position localization, and moving merged content without losing provenance or Unmerge cleanup |
| Independent consumer | 12 public-only cases passed across Office2010, Office2024Flat and Crystal × Compact/Touch × LTR/RTL. Includes native menu/dialog routing, initial focus, accessible names, bounds/Touch targets, simulated 150% root DPI on Touch dialogs, draft/committed moves, Cancel and persistence |
| Render inspection | Inspected compact LTR and Crystal Touch RTL dialog captures; both selectors and actions fit without clipping. No approvals/tolerances changed |
| Builds | Normal Release solution (net8.0/net9.0 runtime targets) and Debug Showcase builds passed, 0 warnings/errors |
| Full/native acceptance | Full suite remains deferred until the end. User review and genuine mixed-monitor/native physical-input acceptance remain open |

The focused selections overlap; they are not a full-suite run. Evidence is in
`TestResults/simplified-tab-move-*.log` and normal test captures under
`TestResults/simplified-customization/move-tab-*.png`. This follow-up and the prior
review refinement remain uncommitted.

#### Move-then-Apply binding diagnostics — 2026-10-13

The user reported HorizontalContentAlignment/VerticalContentAlignment ancestor
binding errors after moving a custom tab and clicking Apply. Reproduction opened
the native selectors and Move dialog, then inspected discarded containers after
Apply refreshed ItemsSource. Enabling the trace recorder requires
PresentationTraceSources.Refresh; an invalid probe prevents a silent false pass.
The exact pair reproduced before the shared RibbonComboBox cleanup fix. The fix
suppresses detached generated containers' system-style fallback and restores it
before reuse; authored containers/local settings remain app-owned. No template
paint or alignment policy changed. Two inherited protected hooks are now
implemented and recorded in the additive API baseline.

| Check | Result |
| --- | --- |
| Focused combined runtime | 17/20 passed, including all three Office2024/Office2010/Crystal-RTL move/Apply trace cases and QAT behavior. Three KeyTip activation cases failed; the same three passed 3/3 in a fresh focused process. The user reported using the computer during the combined run; focus interference is plausible but unconfirmed. This is not a clean combined run |
| Reuse/ownership | 1/1 passed using actual collections: a discarded generated item renders with normal styling when reused, and authored alignment/local style settings survive removal |
| Independent consumer | All 12 public-only legacy/flat/Crystal × density × direction cases passed with realized-popup/discarded-container trace assertions on Apply; Touch dialogs also use simulated 150% root DPI |
| Builds | Normal Debug Showcase and Release solution (net8.0/net9.0 runtime targets) builds passed, 0 warnings/errors |
| User review | Confirmed 2026-10-13: the user followed the manual dropdown/open, custom-tab move and Apply sequence and reported no binding errors. The move-then-Apply alignment-binding fix is accepted |
| Remaining | Full end-of-feature validation and broader native-input/DPI gates remain open |

Logs are `TestResults/simplified-move-apply-*.log`; per-palette binding traces are
under the normal runtime test output's `TestResults/simplified-customization/`.

### S5 motion trial — 2026-10-13

The user requested a more visible collapse/expansion after reviewing the existing
group-spread effect in recordings, then authorized the same treatment for
Compact/Touch toggling. `LayoutModeChange = 16` is additive; `DensityChange = 15`
retains its identity. After the user's recorded retry, both use 130 ms Subtle /
182 ms Expressive with the existing group-expansion easing (cubic/quintic EaseOut).
After the 2026-10-14 follow-up below, simplified tabs use a 120/160-ms,
6/8-DIP downward settle with cubic EaseOut through the existing TabSwitch policy;
classic tab timing remains 130/182 ms.

The real layout commits immediately, including ribbon height, caption sizing and
document placement. A temporary noninteractive vector adorner reveals/contracts
the card edge and moves unscaled command/group drawings between their old and
final slots. Outgoing content clears early to avoid overlapping captions. The
lower QAT's clipping/shadow wrapper follows the actual position delta without an
opacity dip while the vector presentation runs. Its previous transform and consumer
bindings survive. This creates visible ribbon collapse; it does not glide the
host's document layout. The compressed source-group slots explain the spread seen
in the user's first recording; ordinary group/QAT animation remains its own path.

Startup appearance restoration snaps until the first rendered frame. Rapid mode
and density changes replace pending/current work. Input, policy changes, tab/theme
changes, minimize, template replacement, unload, resize, direction/DPI changes and
programmatically opened popups release the presentation. Completion callbacks are
generation-guarded so an old card cannot cancel a newer tab settle. Keyboard and
KeyTips use the real committed controls without a fixed animation delay. Existing
popup frames, side selection and immediate dismissal remain intact; an already
anchored menu uses the opacity-only fallback. Custom templates without an adorner
layer also use that fallback. The new action follows global/per-action/reduced-motion
settings, without Showcase-owned presentation helpers.

The user's follow-up recording showed a destination frame before the animation
returned to its start, and Touch/Mouse selection missed the visible transition.
The repair installs an inert old-body/old-QAT vector cover synchronously, before
deferred layout can expose the destination. Its opacity clock seeks immediately,
without replacing consumer bindings or waiting for a timing tick. The cover is
replaced by the animation in the same dispatcher operation after row construction.
Capture previous geometry while the selector is still open; decide whether a
persistent popup requires fallback only after dismissal/borrowed-item returns.
This preserves the visible density transition through both native and simplified
selector menus. The user requested the quicker group-expansion cadence in place
of the initial 300/420-ms trial. The user's 2026-10-13 review accepted the repaired
motion, as recorded below.

The follow-up review identified a mode toggle inside a collapsed-group popup:
ordinary toggle clicks intentionally keep that group open, leaving its native
window behind after the ribbon changed mode. LayoutMode changes now dismiss
ribbon-owned flyouts at Background priority, after the current click/command
finishes and before the deferred transition captures its final layout. Dismiss
registered nested flyouts from inside out using an ownership snapshot, including
simplified overflow and the detached title-bar QAT. Existing close callbacks
restore borrowed group/menu content and opener state. Native combo dropdowns,
ribbon context menus and the File application menu also close. Other controls'
popups in the same window remain open, and ordinary toggles retain their exemption.
Density changes keep their existing popup policy.

| Check | Result |
| --- | --- |
| Focused runtime | Initial 30/30 animation, popup and simplified-layout checks and initial three-case policy retry passed. Snappy repair selection passed 3/4, including the new immediate-clock/binding check; corrected the density test's curve expectation for its per-action override and its focused retry passed 1/1. This is assembled focused evidence |
| Shared layout-motion consumer | Final snappy repair scope passed cleanly: 24 legacy/Flat/Crystal × LTR/RTL × density × initial-mode cases, each forcing a Render-priority layout before deferred row construction and verifying the destination stays covered. Mode/density motion, startup, bindings, action overrides, interruption, tab settling, resize/template/unload and popup/minimize checks passed. Native and simplified selector menus each passed Touch and Mouse selection with actual popup dismissal |
| Existing density consumer | All 42 theme × QAT-placement × startup-direction cases and interruption checks completed; the aggregate then failed on outdated popup/minimize assertions after the animation surface moved to the QAT wrapper. Corrected surface-specific assertions passed in the focused popup retry and the final layout-motion scope. The entire density aggregate was not repeated |
| Render inspection | Inspected pending-cover/start/intermediate/rest frames in Office2010, Office2024Flat and Crystal, including RTL. The forced pre-dispatcher frame retains the old body and QAT. No snapshot approvals/tolerances changed |
| Builds | Normal Debug Showcase and Release solution builds passed, 0 warnings/errors |
| User review | Accepted 2026-10-13: the user reported that the repaired motion is perfect after the frame-jump, Touch/Mouse visibility and snappy-timing changes. This accepts the S5 presentation; specific native input/DPI/system-reduced-motion gates remain open |
| Mode-popup runtime follow-up | Six focused cases passed across the initial run and narrow retry: ordinary toggle exemption, both directions from a native group popup, detached title-bar QAT with nested menu, native combo/context-menu closure, unrelated-popup isolation and existing nested Escape/unload cleanup. The initial run passed 3/6; three test-only NormalHost casts used Border instead of Decorator. Corrected casts and the failed-only retry passed 3/3 |
| Mode-popup independent consumer | All six Office2010/Office2024Flat/Crystal × LTR/RTL cases passed. Each activates a real toggle in a classic collapsed group, reverses through simplified overflow/group access, then repeats from the title-bar QAT with a nested menu. Click dispatch finishes in the popup, content returns, opener/overflow state clears, and the classic-to-simplified vector transition runs |
| Mode-popup build | Normal Debug Showcase rebuilt with 0 warnings/errors. No public API changed in this follow-up; the preceding Release evidence is retained |
| Mode-popup user review | Accepted 2026-10-13: the user confirmed the mode-toggle popup cleanup works perfectly, including the reported collapsed-group case. Feature implementation is finished; full testing is explicitly deferred until the user prompts for it |

Logs are `TestResults/simplified-motion-*.log`; latest repair frames are under
`TestResults/simplified-motion-jump/renders/`. The final repair consumer log is
`simplified-motion-jump-consumer-complete.log`. The timing check initially reproduced
an obsolete card completion canceling a newer tab animation; disposal/generation
guards repaired it and the final complete scope passed. These focused results do
not close full end-of-feature or live/native gates. Popup follow-up logs are
`TestResults/simplified-mode-popup-*.log`; the user's live collapsed-group review
is accepted. This closed implementation at the 2026-10-13 checkpoint; the user's
2026-10-14 follow-up adds the bounded S6 designer slice below. Earlier unresolved
full-suite/native gates below remain recorded for the user-requested validation pass.

### S5 tab timing and initial row readiness repair — 2026-10-14

The user reported that simplified Expressive tab switching felt too long and
that No Motion exposed a briefly empty row on tab changes and cold startup.
The supplied recording shows an empty selected Home row before its commands
appear. Simplified tab timing is now 120 ms Subtle / 160 ms Expressive, with
6/8-DIP travel and cubic EaseOut. The incoming row stays fully opaque; global,
per-action and system reduced-motion policies still determine whether it moves.
Classic tab and layout/density collapse timings remain unchanged.

The new tab's copies were built by a Background refresh after Loaded, allowing
layout/render to precede row construction. A focused public-only consumer
reproduced missing commands on the first tab switch when forcing a Render-priority
layout before Background. RibbonGroupsHost now consumes pending row preparation
during its first measure, including before Loaded, and cancels the queued refresh.
Loaded avoids rebuilding a row already prepared there. Keep the existing unload
cleanup and source/proxy ownership; no all-tab cache or host-owned presentation
was introduced. Open registered flyouts retain the deferred refresh path to avoid
returning borrowed content during a command that forces synchronous layout.
A reentrancy guard protects measure-triggered preparation. The protected WPF
MeasureOverride hook is recorded in the additive API baseline.

| Check | Result |
| --- | --- |
| Focused runtime | 26/26 LayoutAnimationTests and SimplifiedRibbonTests passed, including 120/160-ms clock durations, 6/8-DIP travel, easing, per-action None, bindings, native popup cleanup/returns and the Showcase Home declaration |
| First-render consumer | 24 Office2010/Office2024Flat/Crystal × LTR/RTL × density × initial/Loaded-restored mode cases passed. Commands are arranged at Render priority before Background on startup, first visit and repeated tab revisits; No Motion has no body hold/clock |
| Motion/popup consumer | Final LayoutMotionPaint scope passed cleanly after the preparation change, including forced old-frame covers, layout/density motion, interruption, tab cancellation, selector popup dismissal, six themed mode-popup cases and the 24 readiness cases |
| Builds | Normal Debug Showcase and Release solution builds passed, 0 warnings/errors |
| Live/full gates | Animation accepted 2026-10-14: the user described the repaired tab motion as perfectly animated. This confirms the new visual cadence; actual No Motion/cold-launch blank-interval acceptance was not separately reported. Full tests and earlier unresolved native/full-suite gates remain deferred until an explicit prompt |

Logs are `TestResults/simplified-tab-*.log`; recording samples are under
`TestResults/simplified-tab-followup-video/`. Build/consumer evidence does not
establish actual cold-launch performance or physical keyboard/touch acceptance.

### S6 designer defaults — 2026-10-14

The user requested simplified defaults for items added through the designer and
confirmed Inline placement with labels shown by default. Toolbox initializers,
group context commands and Ribbon Editor creation share one helper that writes
the existing attached `Ribbon.SimplifiedPlacement=Inline` and
`Ribbon.SimplifiedSize=Medium` values inside the creation transaction. It does not
overwrite assigned values or change the runtime's Overflow/Medium defaults.

Supported button/toggle/split/dropdown, combo and gallery commands expose **Show
in simplified ribbon** and **Show simplified label** checkboxes in Ribbon Editor.
These author runtime XAML: unchecked visibility maps to Overflow, and unchecked
label maps to Small. Each edit is undoable through the existing attached-property
resolver. Native editors and unsupported option/menu-item types retain full-group
access; they are not silently converted into simplified command copies.

| Check | Result |
| --- | --- |
| Debug builds | DesignTools and normal Showcase builds passed, 0 warnings/errors; deployed DesignTools DLL hashes match both normal runtime Design folders |
| Independent consumer | Focused SimplifiedDesignerDefaults scope passed: 12 Office2010/Office2024Flat/Crystal × LTR/RTL × shown/hidden-label cases. Fresh authored XAML realizes all seven supported command families and the selected label layout. Unconfigured runtime commands still default to Overflow/Medium; native editors remain excluded from inline copies |
| Live designer | Accepted 2026-10-14: after the toolbox guard repair, the user confirmed the placement/default now appears for the reported RibbonButton/RibbonToggleButton drops, and defaults, Ribbon Editor and undo/redo work. Earlier checklist acceptance of context/editor Add, checkbox edits, reload and runtime presentation is retained |
| Toolbox guard follow-up | Replaced the Value-null test with ModelProperty.IsSet so an effective default wrapper cannot suppress a new authored default. Added a toolbox-callback entry and per-property IsSet/effective-value diagnostics. DesignTools and normal Debug Showcase builds passed, 0 warnings/errors; deployed DLL hashes match both runtime Design folders. Native outcome is recorded in the live-designer row |
| Full/package/native gates | Deferred until the user's explicit prompt, including the earlier unresolved full-suite/native gates; no capability API was added |

Logs are `TestResults/simplified-designer-*.log`; toolbox callback diagnostics use
`%LOCALAPPDATA%/RibbonKit/DesignTools.log`. The separately requested
[feature availability plan](16-RIBBON-FEATURE-AVAILABILITY-PLAN.md) proposes a
persistent host capability Boolean starting with Simplified Ribbon. It is a design
record, not an implemented runtime switch or additional authorized feature slice.

### S7 host defaults and customization integration — 2026-10-14

The user requested useful default commands in MDI, RTL Lab and Writer, plus the
missing simplified customization page in RTL/Writer. MDI intentionally receives
defaults only: its five Window commands and each document's merged UPPERCASE
command use Inline placement. No MDI customization dialog or event wiring was
added. RTL defaults include direction/localization, density, customization and
the sample font/styles/clipboard commands. Writer defaults cover common commands
on Home, Insert, contextual Table/Picture Tools, Page, View and Print Preview;
text formatting and small navigation actions use icon-only labels, while primary
actions and font/gallery selectors retain labels. Other commands remain available
through the shared overflow/full-group surfaces.

The missing dialog is host composition: RTL and Writer had the classic/QAT pages
but neither the shared simplified page nor its request handler. Each now adds
`RibbonSimplifiedCustomizePage` with the existing factory reset baseline, handles
`SimplifiedCustomizeRequested`, and selects the editor for the current layout.
QAT entry retains both mode-specific editors; shared RibbonOptionsDialog handles
their visibility, eligibility and mode changes. No runtime/API or persistence
schema changes were needed; Classic remains the initial mode in these hosts.

Writer's host saves pin/remove changes outside Options. While Options is open,
the shared page's Apply advances only the ribbon rollback/storage checkpoint,
so later Cancel discards subsequent drafts without reversing Apply or committing
appearance previews. OK and appearance Apply retain their existing contracts.
The [Writer friction record](12-RIBBONKIT-WRITER-CONSUMER-FRICTION-LOG.md#rkwf-047--simplified-customization-needs-host-dialog-and-persistence-integration)
records this consumer glue.

| Check | Result |
| --- | --- |
| RTL host | Focused realized-window check passed: real simplified context-menu request opens the enabled shared editor in RTL, QAT entry exposes it, and changing mode switches to the classic counterpart |
| MDI host | Focused check passed: five Window defaults project, merged UPPERCASE commands stay visible and target their contributing document across activation/revisit. The first harness assertion ran before deferred click forwarding; it passed after draining that work |
| Independent Writer consumer | Two focused tests passed without Showcase resources/helpers: simplified and QAT event routes, draft Cancel, Apply then further draft/Cancel, OK, pin persistence and restart, plus the existing Crystal appearance Apply/Cancel/OK/restart contract |
| Debug builds | Normal Showcase and Writer builds passed with 0 warnings/errors. Earlier focused builds encountered a DesignTools copy lock from Visual Studio; after the user closed VS, normal builds completed cleanly |
| User review | Accepted 2026-10-14: the user reported everything is good after reviewing MDI/RTL/Writer simplified defaults and RTL/Writer customization integration. MDI remains defaults-only. This accepts S7's current presentation and integration; it does not establish the earlier specific full-suite/native gates |
| Limits | Full testing remains deferred until prompted; all earlier unresolved full-suite/native gates are retained. No commit/push/publication |

### Remaining acceptance

- Human review of S2's customization page, ordering and labels. The earlier S1
  visual fixes are accepted; native mouse/touch, full keyboard/KeyTip traversal
  and nested popup dismissal still need their specific live review. The independent
  matrices are automated geometry/interaction evidence.
- Genuine mixed-monitor DPI transitions and native input; simulated root DPI
  does not establish physical-monitor acceptance. Live reduced-motion review
  remains unrun; real layout commits immediately and S5's presentation follows
  the existing animation policy.
- Resolve or separately accept the two unfiltered suite failures; no clean
  full-suite pass is claimed.
- Full end-of-feature validation and human acceptance of the S2 page's applied,
  canceled and restarted selections.
- Human acceptance of S3's custom-tab/group editor and its persistence/cancellation
  behavior; automated focused evidence is recorded above. The 2026-10-13 review
  requested the S4 redesign; it did not accept the current page layout.
- User review of S4's column/header/menu layout, physical drag and keyboard
  ordering, and native Reset/Import/Export use. Focused evidence is recorded above.
- S5's original motion presentation and mode-toggle popup cleanup are accepted.
  The tab follow-up's remaining native review scope is recorded above. Broader
  physical input, mixed-monitor DPI and live system reduced-motion behavior
  remain open. Focused offscreen consumer checks do not establish those gates.
