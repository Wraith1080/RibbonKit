# Custom-control integration and projection candidate

No public projection-provider API is frozen. Ordinary `FrameworkElement` content
already works in a `RibbonGroup`; `IRibbonSizeAware` is an optional reduction hook.
The proposal below adds customization/QAT participation without changing that baseline.

Built-in combo and gallery dropdown copies are now implemented separately
([library history §3.229](history/library/11-touch-density-and-gallery-projections.md#3229-combo-box-dropdown-copies-for-qat-and-customization--2026-10-08),
[§3.230](history/library/11-touch-density-and-gallery-projections.md#3230-gallery-dropdown-copies-for-qat-and-customization--2026-10-08)).
They do not introduce the generic provider API proposed below. Gallery copies
retain the native source presenter, borrowing it between separate permanent
viewports as `InRibbonGallery` already does for its own expansion. The generic
fresh-view contract below remains a candidate for third-party controls. A temporary
vector preview preserves the visible source strip while a gallery copy is open;
closing restores the live strip and reveals the committed selection.

Built-in group-to-QAT support uses the source group's existing collapsed flyout.
Group entries appear and are addable only in Customize QAT, with those entries
excluded from Customize Ribbon. Individual commands keep their normal availability.
The source group stays collapsed for the entire QAT membership, by user direction
on 2026-10-11; removal releases that constraint to adaptive sizing. Opening and
dismissal do not resize the ribbon or wait for input completion. This implementation
adds no generic projection-provider API.

## Group QAT implementation and acceptance

The library owns the compact dropdown, membership collapse, source flyout anchor,
native content restoration, overflow, KeyTips/UIA, hover suppression and caption
context menus. Hosts supply group icons, commands/data and customization storage.
Collapse is derived from the live QAT collection and excluded from serialized
group layout; saved QAT identity recreates the constraint. Hidden QAT retains its
members. Recreated custom groups resolve to their final source objects on restore;
transient merge contributions retain the existing persistence exclusions.

| Gate | Status on 2026-10-11 |
| --- | --- |
| Permanent collapse, native ownership, inactive tabs, adaptive removal, interruption cleanup, catalog and persistence | All 138 focused Release runtime checks passed on the complete rerun, including active-tab/overflow content presses and toggle/check/radio clicks that keep the group flyout open. Synthetic input checks cover QAT group/combo/gallery/dropdown dismissal and command/File activation. |
| Independent consumer | `GroupQuickAccess` covers 288 theme/variant/density/flow/simulated-DPI/placement cases plus overflow, keyboard, native UIA and KeyTips; no Showcase resources or helpers. |
| Extra empty QAT popup | Reproduced in four active/inactive strip and overflow cases. A local one-way closed binding now prevents the unused dropdown popup from appearing as a small circle. All 32 group runtime cases and the 288-case independent consumer scope passed after the fix. User confirmed the stray circle is gone in Showcase on 2026-10-11. |
| Existing combo/gallery compatibility | The complete runtime rerun and the `GalleryQuickAccess` consumer passed. The preceding runtime run passed 137/138 with one split-button KeyTip failure; the unchanged baseline passed the corresponding 106-case runtime slice. The combo consumer keyboard/KeyTip scope is intermittent: it also failed around overflow KeyTips in an unchanged `a2ebe40` baseline archive. This is not a clean combo consumer pass. |
| Visuals | 15 selected QAT scenes passed. The changed Customize QAT snapshot was updated after actual/diff inspection; three native group flyout captures were inspected. |
| Builds | Normal Release runtime (`net8.0-windows` and `net9.0-windows`) and normal Debug Showcase passed with zero warnings/errors. |
| Live acceptance | Live mouse/keyboard, physical touch, slow-machine interaction and native/mixed-monitor DPI review remain open. Full validation is deferred by request. |

Implementation details and final automated evidence are recorded in
[library history §3.249](history/library/12-keytips-layout-and-validation.md#3249-groups-in-qat-with-membership-collapse--2026-10-11).

### Hover binding follow-up — 2026-10-12

The modal close button's shared Crystal rim reported a failed attached-property
lookup. The utility-rim and application-orb bindings now use the registered
suppression dependency property through an internal `PropertyPath`, preserving
QAT hover suppression and pressed feedback without a public API addition.

Verification: 47 focused Release runtime cases passed. `GroupQuickAccess` passed
its existing matrix and 12 new utility/orb binding cases across all placements
and both directions, including native modal-close invocation. The Office 2007 orb
visual scene passed. The Crystal utility fixture follow-up is fixed on 2026-10-12:
initializing WPF's window renderer before the disconnected scene's first measurement
restores repeatability. All six light/dark, 100/200% and RTL utility scenes passed
against unchanged approved images and tolerances; the original 100% case also passed
in three fresh processes. Repeatability failures now save both captures and their
diff for inspection. Normal Release runtime builds for both frameworks
and Debug Showcase passed with zero warnings/errors. On 2026-10-12 the user completed
the Crystal Print Preview/QAT and Office 2007 orb checks and reported no binding
errors. The user accepted the visual-fixture correction on 2026-10-12 and plans
to merge this slice. Full validation stays deferred.
Details: [library history §3.252](history/library/12-keytips-layout-and-validation.md#3252-internal-hover-suppression-bindings-use-property-identity--2026-10-12).
The visual-fixture correction is recorded in [§3.253](history/library/12-keytips-layout-and-validation.md#3253-crystal-utility-snapshots-initialize-native-rendering--2026-10-12).

## Custom-group command copies in QAT

Fixed on 2026-10-11: context-menu additions from custom tabs/groups resolve to the
original command, preserving dropdown/split menu items and combo/gallery behavior.
All copies of that source share one QAT identity. Customize QAT disables Add for
an existing source, refreshes after external additions/removals, and rejects a
repeated add through the same runtime check.

Verification: 114 focused Release runtime cases passed. The independent
`QatCommandInteraction` scope passed, including 12 new dropdown/split cases across
three placements and both flow directions, native menus/invocation, and duplicate
prevention in the context menu and Customize QAT page. One selected RTL QAT-page
visual passed after inspecting/updating its disabled Add-button appearance.
Release runtime builds and Debug Showcase passed with zero warnings/errors.
Live Showcase confirmation of this command-copy fix remains open; full validation
remains deferred. Details: [library history §3.250](history/library/12-keytips-layout-and-validation.md#3250-custom-group-command-additions-resolve-the-original-qat-source--2026-10-11).

## Gallery ScreenTips

Implemented on 2026-10-11: `RibbonGallery.ScreenTipTitle` and `ScreenTipText`
create the shared rich tooltip and are inherited by `InRibbonGallery`. QAT,
overflow and custom-group copies follow source tip metadata; a null title falls
back to the copy's current header, including a custom rename. Tile tooltips remain
intact. Styles, Theme, Accent and the Localization/RTL Styles demo now have gallery
descriptions. No per-item KeyTips were added, by user direction.

Verification: 26 focused Release runtime cases passed. The independent
`GalleryQuickAccess` consumer passed its existing gallery matrix and 48 new
gallery-type/theme/light-dark/flow combinations, with four realized native
ScreenTips per combination (source, custom group, QAT and overflow). Office 2024
and Crystal captures were inspected. Normal Release runtime builds for both
frameworks and Debug Showcase passed with zero warnings/errors. The user reported
that the gallery ScreenTips look good on 2026-10-11; further user checks and final
live hover acceptance remain pending. Full validation remains deferred by user direction.
Details and additive API rationale: [library history §3.251](history/library/12-keytips-layout-and-validation.md#3251-gallery-screentips-follow-qat-and-custom-group-copies--2026-10-11).

## Grouped combo dropdowns

Implemented 2026-10-21. Native combo boxes and their QAT/custom-group/Simplified
copies retain collection-view sections, themed nonselectable headings and source
selection. Headings scroll with the choices; one scrollbar serves the full list.
The current spacing gives rows and headings one DIP above/below, except at the
first top and last bottom edge: uniform two-DIP gaps, with no larger section gap.
Shared panels/tokens also avoid doubling Touch margins and account for fractional
display scaling.
An automatic overall Font caption is omitted for grouped copies. Explicit
GroupStyle/caption overrides remain available through existing WPF properties.
The Showcase Font combo now demonstrates Common fonts and Other fonts, using
host-owned sample data and shared RibbonKit presentation. No public API addition.

Focused validation passed across separate runs: seven new grouping/sample cases,
seven existing combo-projection cases, and ten affected KeyTip/keyboard-handoff
cases. Coverage includes source identity and selection events, editable combos,
nested groups, group sorting, multiple membership, dynamic collection/grouping
changes, custom style precedence and the actual Showcase sample without loading
user preferences. The independent `GroupedCombos` scope passed 56 theme/light-dark/
density/direction combinations across native, QAT and custom-group surfaces, using
simulated 100/150% root DPI. It checks section gaps, paint, one scrollbar,
scrolling headings, last-item selection and open-view refresh. All 28 full-frame
Compact/Touch, light/dark popup captures were inspected in
`TestResults/grouped-combos`. Existing passed checks were retained while failed
or changed scopes were repaired; no unrelated suite or snapshot approvals ran.

The uniform-spacing refinement on 2026-10-21 passed nine focused native/copy/
dropdown/split geometry cases, including nested/empty groups, collapsed rows,
Compact/Touch and flat/grouped view changes. The same 56 independent-consumer
combinations (168 surfaces) passed, now checking every row gap and both outer edges.
All 28 updated renders in `TestResults/grouped-combos-uniform-spacing` were inspected.
Previously passed keyboard and unrelated behavior checks were not repeated.
User appearance review 2026-10-21: accepted the uniform vertical spacing in the
Font dropdown.

Normal Debug runtime net8/net9 and Showcase builds passed with zero warnings/errors,
reusing the unchanged designer project output held open by Visual Studio. Native
pointer/IME and mixed-monitor review and full-suite
release/package gates remain open. Implementation details are in
[library history §3.274](history/library/11-touch-density-and-gallery-projections.md#3274-grouped-combo-dropdown-sections--2026-10-21).

## Capability and lifetime contract

Customization eligibility needs stable `Ribbon.CommandId`, display name and small
icon. Projection additionally needs an opt-in provider. Missing metadata must not
break hosting; unsupported automatic QAT requests return false rather than a blank proxy.

A provider creates a fresh unparented view for each purpose: small QAT strip, medium
QAT overflow or requested-size custom group. Never clone/reparent the source or reuse
one visual across parents. Bind commands, parameters, enabled/selected/text/checked
state to the source/shared model rather than copying current values. Events, namescopes
and popup ownership cannot be inferred by generic serialization.

Provisional names are `RibbonProjectionPurpose`, `RibbonProjectionContext`,
`RibbonCommandDescriptor`, `IRibbonProjectionProvider` and `RibbonProjection`.
The context carries owner, purpose and requested size; the descriptor carries display,
icon and ScreenTip metadata. A projection carries the view and idempotent cleanup.
Keep `Ribbon.CommandId` as the single persisted identity. An attached adapter for
third-party controls is a later option, not an existing property.

| RibbonKit owns | Provider owns |
| --- | --- |
| Creation/removal timing, placement, overflow and source identity | Fresh context-appropriate view and presentation |
| Resource/data context, flow/DPI context, KeyTip placement | Command/state bindings and complex popup semantics |
| Merge parking and disposal invocation | Subscription/timer/popup cleanup |
| Source-ID persistence and restore lookup | Selection, validation and preview/commit behavior |

Remove/close an open projection before releasing its source. Dispose exactly once.
Restore creates fresh views from live source IDs. Merged sources stay parked when
unavailable and excluded from application-owned persistence. Group, gallery and combo
projections are separate proofs: synchronized preview/commit or editable text is not
validated by a command-button proxy.

## Theme boundary

Use `DynamicResource`, not palette snapshots. Before release, define a small stable
consumer token subset for surface/text/border/accent/states/radius/spacing; do not
implicitly freeze every internal key. Code-derived rendering may use theme-change
notifications; normal XAML should not need them. App-authored icons retain ownership.

## Delivery and acceptance

1. Prove normal hosting and adaptive/theme behavior with a Showcase-owned custom control.
2. Prototype factory/lifetime internally for strip and overflow.
3. Prove catalog validation, source-ID restore and one command plus one stateful popup control.
4. Review the additive API, XML docs, designer discovery and consumer reference before release.
5. Prove richer third-party popup controls in subsequent bounded slices.

Acceptance covers invalid opt-in metadata, distinct parents, live state synchronization,
cleanup during open popups, save/restore, merge park/revive, disabled targets, KeyTips,
UIA, RTL, reduced motion, theme/variant switching and relevant DPI scales. Current scope
and implementation status remain in [design notes §5](../04-DESIGN-NOTES.md#5-current-state--next-steps).
