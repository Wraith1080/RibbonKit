# Custom-control integration and projection candidate

No public projection-provider API is frozen. Ordinary `FrameworkElement` content
already works in a `RibbonGroup`; `IRibbonSizeAware` is an optional reduction hook.
The proposal below adds customization/QAT participation without changing that baseline.

Built-in combo and gallery dropdown copies are now implemented separately
([library history §3.229](history/01-library.md#3229-combo-box-dropdown-copies-for-qat-and-customization--2026-10-08),
[§3.230](history/01-library.md#3230-gallery-dropdown-copies-for-qat-and-customization--2026-10-08)).
They do not introduce the generic provider API proposed below. Gallery copies
retain the native source presenter, borrowing it between separate permanent
viewports as `InRibbonGallery` already does for its own expansion. The generic
fresh-view contract below remains a candidate for third-party controls. A temporary
vector preview preserves the visible source strip while a gallery copy is open;
closing restores the live strip and reveals the committed selection.

Group-to-QAT support is implemented as a separate internal projection. Group
entries appear and are addable only in Customize QAT, with those entries excluded
from Customize Ribbon. Individual commands keep their normal customization
availability. The generic provider API below remains a proposal.

The source group owns one content handoff, reusing the shared collapsed-group
template content. A vector preview retains its visible footprint while borrowed.
Native/QAT/overflow opening, close/removal, template changes and adaptive resizing
restore that tree and nested popups. Inactive tabs establish their normal resource
and command route without changing selection. Persistence uses existing group IDs
and resolves custom groups after rebuilding them.

Current group slice verification (2026-10-09): 58 focused runtime checks pass,
including 13 group checks. The independent consumer's initial 288-case layout/state
matrix passed; final Office/Crystal Compact/Touch previews and nested overflow
keyboard checks pass after the cleanup corrections. Release solution and normal
Debug Showcase builds pass with zero warnings/errors. Detailed implementation and
evidence are in [library history §3.238](history/01-library.md#3238-ribbon-groups-in-quick-access-toolbar--2026-10-09).

The QAT popup follow-up implements outside-click focus/capture preservation,
paused ribbon/tab hover and ScreenTips, click-through restoration of a group's
preview, and the existing QAT menu on group captions. It adds no public API.
70 focused Release runtime checks pass; the final collapsed-source case was also
rerun after strengthening its setup. The independent consumer's nested keyboard
check and eight native mouse scenarios pass (four popup kinds in Office2007
Compact/LTR and Office2024 Touch/RTL), including commands, File/orb, tab selection,
preview click-through and the themed caption menu. Manual review remains pending.
Details are in [library history §3.239](history/01-library.md#3239-qat-popup-input-hover-and-group-caption-menu--2026-10-09).

Full validation is deferred by user direction. The interrupted run completed
631 runtime checks (629 passed, an obsolete group-unsupported assertion now
corrected and passing, plus an Office2013 LTR focus-adorner failure remaining for
later verification). Its visual run stopped at `office2024-rtl-qat-customize-100`:
actual/diff images show the intended added group rows; approvals remain unchanged
pending live review. Writer testing was interrupted and the unfiltered portability
aggregate did not run. Live mouse/keyboard, physical touch, reduced-motion and
native mixed-monitor DPI acceptance remain pending.

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
5. Add richer group/gallery/combo projections in subsequent bounded slices.

Acceptance covers invalid opt-in metadata, distinct parents, live state synchronization,
cleanup during open popups, save/restore, merge park/revive, disabled targets, KeyTips,
UIA, RTL, reduced motion, theme/variant switching and relevant DPI scales. Current scope
and implementation status remain in [design notes §5](../04-DESIGN-NOTES.md#5-current-state--next-steps).
