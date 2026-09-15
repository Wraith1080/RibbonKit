# Custom-control integration and projection candidate

No public projection-provider API is frozen. Ordinary `FrameworkElement` content
already works in a `RibbonGroup`; `IRibbonSizeAware` is an optional reduction hook.
The proposal below adds customization/QAT participation without changing that baseline.

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
