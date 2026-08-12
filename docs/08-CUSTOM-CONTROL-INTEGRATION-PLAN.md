# Custom control integration and projection plan

> **Status:** post-v1 design candidate. The concepts and example names below are provisional; no
> public projection-provider API is frozen yet.

Ribbon groups already accept arbitrary WPF content. That remains the baseline: an application can
place any `FrameworkElement` in a `RibbonGroup` without implementing a RibbonKit interface, supplying
an icon, or opting into customization. `IRibbonSizeAware` remains the optional hook for content that
needs to react to the group's adaptive size state.

This plan adds an opt-in contract for custom content that also wants to appear in the built-in
customization catalog, a custom ribbon group, or the Quick Access Toolbar (QAT). It does not clone or
reparent the original WPF element.

## 1. Capability levels

| Capability | Proposed requirement |
|---|---|
| Host inside a `RibbonGroup` | Any `FrameworkElement`; no icon or RibbonKit interface required |
| React to ribbon reduction | Implement the existing `IRibbonSizeAware` contract |
| Appear in customization | Stable `Ribbon.CommandId`, non-empty display name, and a 16px icon |
| Appear in QAT/custom groups | Customization metadata plus an opt-in projection provider |
| Follow RibbonKit themes | Use the documented consumer-facing `DynamicResource` keys |

Missing integration metadata must not break ordinary group hosting. It makes the control ineligible
for automatic discovery/projection, and `Ribbon.AddToQuickAccess` should continue to return `false`
rather than create a blank or nonfunctional proxy.

## 2. Why projections are factories, not copies

A WPF element can have only one visual parent. Serialization or visual-tree cloning also cannot
reliably preserve bindings, event handlers, namescopes, `ElementName` references, ancestor-relative
bindings, popup ownership, or control-specific state.

One source can simultaneously need several representations:

- its original content in the authored group;
- a small icon-only QAT strip item;
- a medium icon-and-label entry in QAT overflow;
- a requested-size representation in a customized ribbon group.

The custom-control creator is the only party that understands which commands and state make those
representations equivalent. RibbonKit should request a fresh projection for each context and manage
the surrounding lifecycle.

## 3. Provisional contract shape

Names are illustrative and must be proven internally before becoming public API:

```csharp
public enum RibbonProjectionPurpose
{
    QuickAccessStrip,
    QuickAccessOverflow,
    CustomGroup,
}

public sealed class RibbonCommandDescriptor
{
    public required string DisplayName { get; init; }
    public required ImageSource SmallIcon { get; init; }
    public ImageSource? LargeIcon { get; init; }
    public string? ScreenTipTitle { get; init; }
    public string? ScreenTipText { get; init; }
}

public readonly struct RibbonProjectionContext
{
    public Ribbon Owner { get; }
    public RibbonProjectionPurpose Purpose { get; }
    public RibbonControlSize RequestedSize { get; }
}

public interface IRibbonProjectionProvider
{
    RibbonCommandDescriptor Descriptor { get; }
    RibbonProjection CreateProjection(RibbonProjectionContext context);
}
```

`RibbonProjection` would expose the newly created `FrameworkElement` and an idempotent cleanup path
(most naturally `IDisposable`). Creation is the callback where the provider binds commands/shared
state and wires any custom events. Disposal is where it releases subscriptions, borrowed popup
content, timers, or other projection-specific state.

The existing attached `Ribbon.CommandId` remains the persisted identity. Do not introduce a second
competing ID inside the descriptor. To support third-party controls that cannot implement an
interface, consider an attached `Ribbon.ProjectionProvider` adapter as a second entry point after the
provider lifecycle is proven.

## 4. Responsibility boundary

| RibbonKit owns | Projection provider owns |
|---|---|
| Deciding when and why a projection is needed | Creating a fresh view for that context |
| Source identity and persistence | Command and command-parameter bindings |
| Enabled-state mirroring and merge parking | Selection, text, checked, preview, and commit state |
| Placement, overflow, KeyTip level, removal | Context-specific compact/expanded presentation |
| `DataContext`, flow direction, DPI and resource context | Custom event subscriptions and cleanup |
| Calling disposal exactly once | Popup semantics that RibbonKit cannot infer |

RibbonKit may provide a default adapter for straightforward `ICommandSource` implementations. It can
bind `Command`, `CommandParameter`, `CommandTarget`, label, icon, ScreenTip and enabled state into a
standard `RibbonButton`. Complex controls such as galleries, editable combos and color pickers still
need their own provider because a generic button cannot preserve their semantics.

Bindings must target the source or a shared view model rather than copy a current value. Routed event
handlers are not copied. A provider that subscribes manually must unsubscribe through the projection
lifetime. Each call must return an unparented instance; returning the original control or a cached
view is invalid.

## 5. Customization, persistence and QAT rules

- The command catalog includes an opted-in custom control only when its ID, display name, small icon
  and provider are valid.
- Serialization stores the source `Ribbon.CommandId`, never the generated view.
- Restore resolves the live source and asks it for a new context-specific projection.
- Strip and overflow are independent views. Overflow must not reparent the strip view.
- A merged source is parked/disabled with the existing merge machinery and is not persisted as an
  application-owned command.
- Removing a projection while its popup is open closes the popup and disposes the projection before
  releasing the source.
- KeyTips, UI Automation name/patterns, RTL popup direction and reduced-motion behavior belong to the
  acceptance contract, not optional polish.

Group-to-QAT, gallery-to-QAT and combo-to-QAT remain separate proof cases. A `RibbonGroup` should
project as a small dropdown built from source-linked child projections. A gallery needs synchronized
preview/commit behavior. An editable combo needs distinct strip and overflow views synchronized for
items, selection, text, validation and width. Proving one does not automatically validate the others.

## 6. Theme integration for custom content

Custom controls should not pull a one-time palette snapshot. Their XAML should use
`DynamicResource`, allowing `ThemeManager` to swap dictionaries and update the control in place:

```xml
<Border
    Background="{DynamicResource RibbonKit.Brushes.Control.SurfaceBackground}"
    BorderBrush="{DynamicResource RibbonKit.Brushes.Ribbon.Border}">
    <TextBlock
        Foreground="{DynamicResource RibbonKit.Brushes.Text.Primary}" />
</Border>
```

Before the provider API ships, publish a small stable consumer subset (potentially through a
strongly named `RibbonThemeResourceKeys` class) rather than declaring every internal token public.
The initial subset should cover control surface, primary/secondary text, border, accent,
hover/pressed/checked backgrounds, disabled foreground, control corner radius and common spacing.

Normal XAML should not subscribe to `ThemeManager.Changed`; `DynamicResource` already provides live
updates. The event is reserved for controls that calculate derived values in code. Projection views
must use dynamic tokens or source bindings as well, otherwise they can retain a stale brush or icon
after a theme switch.

## 7. Implementation sequence

1. Add a Showcase-owned custom control that hosts normally, implements `IRibbonSizeAware`, and uses
   only the proposed public theme-token subset.
2. Prototype the provider and projection lifetime internally for QAT strip and overflow contexts.
3. Add catalog discovery, validation messages, persistence identity and restore behavior.
4. Prove one command-style control and one stateful/popup control before freezing the interface.
5. Add the public API to `PublicAPI.Unshipped.txt`, XML documentation, Ribbon Editor discovery and
   consumer documentation for a v1.x additive release.
6. Consider group/gallery/combo projections only as subsequent slices.

## 8. Verification gate

- Ordinary arbitrary group content remains unaffected and requires no icon.
- Missing/invalid opt-in metadata is rejected without a blank proxy.
- Strip, overflow and custom-group requests return distinct unparented elements.
- Commands, parameters, enabled state and mutable state stay synchronized with the source.
- Projection cleanup occurs exactly once, including removal while a popup is open.
- Save/load restores through source IDs and creates fresh views.
- Merge park/unpark, QAT overflow, KeyTips, UI Automation and disabled-target blocking work.
- All shipped themes and dark/black variants recolor the original and every projection live.
- Focused 100/125/150/175/200% DPI, LTR/RTL and reduced-motion checks pass.

