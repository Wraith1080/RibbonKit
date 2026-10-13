# Simplified ribbon

> Active implementation plan. First layout slice authorized 2026-10-13.
> S2 customization implemented after the user's continuation on 2026-10-13.

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

### Custom tab and group editor proposal

The user asked how creation will fit this command-list editor. Proposed next
bounded slice; these creation controls are not implemented yet:

- Add New Tab/New Group/Rename actions under the selected-command list, with a
  full-column group selector alongside the tab selector. Left-side discovery
  must offer commands from the whole ribbon, so a new empty tab can receive
  commands. Groups remain structure choices, never available command entries.
- Reuse the existing custom tab/group identities and source-linked proxies.
  Structure belongs to the shared ribbon and appears in both layouts;
  simplified selection/order/labels remain a separate per-layout profile.
- Draft new/renamed structure and membership until Apply/OK. Cancel must leave
  no created tabs/groups behind. Reset-current/all must state whether it resets
  selections or custom structure; the current reset actions reset selections.
- Record explicit target-group identity with each selected command. Current
  S2 infers the group from original source discovery; that is insufficient when
  the same command also belongs to a new custom group. Extend the optional
  serializer section with migration from version 1 and preserve missing/transient
  source behavior before exposing that assignment.
- Verify creation, naming, command membership, group placement/dividers, save/load,
  reset/cancel, switching modes and a consumer without Showcase resources.

## Delivery and acceptance

| Slice | Scope | Status |
| --- | --- | --- |
| S1 | Shared layout/selection/priority, native full-group access, keyboard/KeyTips, Showcase toggle and classic-customization gating | Implemented; user accepted the current visuals 2026-10-13. Native-input/DPI and earlier combined/full-suite limits remain open |
| S2 | Dedicated customization page, pin/unpin, separate ordered state and persistence | Implemented 2026-10-13; focused checks/consumer evidence below; awaiting user visual review |
| S3 | Shared custom-tab/group creation from the simplified editor | Proposed after user review; implementation pending |

S1 requires focused runtime regressions, a RibbonKit-only consumer matrix, normal
Debug Showcase build, Release solution build/test for the additive public API,
and review of rendered evidence. Native input and mixed-monitor DPI acceptance
remain explicit live gates.

### Recorded verification — 2026-10-13

Work is uncommitted on `codex/simplified-mode`.

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

### Remaining acceptance

- Human review of S2's customization page, ordering and labels. The earlier S1
  visual fixes are accepted; native mouse/touch, full keyboard/KeyTip traversal
  and nested popup dismissal still need their specific live review. The independent
  matrices are automated geometry/interaction evidence.
- Genuine mixed-monitor DPI transitions and native input; simulated root DPI
  does not establish physical-monitor acceptance. Live reduced-motion review
  remains unrun; the row switches immediately and native group/QAT effects reuse
  existing animation policy.
- Resolve or separately accept the two unfiltered suite failures; no clean
  full-suite pass is claimed.
- Full end-of-feature validation and human acceptance of the S2 page's applied,
  canceled and restarted selections.
- The proposed S3 custom-tab/group editor and its persistence/cancellation gates.
