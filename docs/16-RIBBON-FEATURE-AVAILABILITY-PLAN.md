# Ribbon feature availability

> Proposed design record, requested 2026-10-14. No runtime capability API is
> implemented or authorized by this planning slice. Simplified-command designer
> defaults are the separate S6 slice in the [simplified plan](15-SIMPLIFIED-RIBBON-PLAN.md#s6-designer-defaults--2026-10-14).

## Goal

Let an application author configure whether an optional ribbon feature is
available, using a Boolean property in ordinary XAML or a binding. The decision
must remain effective at runtime: hiding a selector in the designer must not
leave context menus, restored preferences or commands able to activate it.

Start with Simplified Ribbon. Review other optional features individually before
adding more switches; avoid a generic feature registry or a collection of flags
without concrete use cases.

## Recommended first API

Propose a `Ribbon.IsSimplifiedRibbonEnabled` Boolean dependency property,
defaulting to `true` for compatibility. It is an application capability setting,
separate from the current `Ribbon.LayoutMode` selection and per-command
`Ribbon.SimplifiedPlacement`/`SimplifiedSize` defaults.

```xaml
<!-- Proposed API; unavailable until the implementation slice is approved. -->
<rk:Ribbon IsSimplifiedRibbonEnabled="False" />
```

This is normal authored XAML, so the setting survives designer reload and remains
effective when the application starts. A host may bind the property to its own
configuration. A mutable WPF Boolean is not an irreversible lock: host code can
deliberately enable the feature later. "Permanently disabled" means that end-user
layout choices/customization cannot override the host's declared capability.

Final naming, effective-state/coercion mechanics and runtime re-enable behavior
need API review before implementation. The recommended re-enable behavior is to
re-evaluate the requested LayoutMode value through normal WPF precedence; do not
replace an application's binding while temporarily enforcing Classic.

## Disabled contract

When the property is false:

- The effective ribbon layout is Classic. Code, bindings, restored appearance
  preferences and explicit attached LayoutMode scopes inside that ribbon cannot
  force a simplified body. An attempted request remains subject to the capability
  guard; avoid overwriting host bindings or assigning permanent source visibility.
- Library context menus omit Simplified Ribbon and Pin/Remove from Simplified
  Ribbon actions. Customization routes to the classic editor. A standalone
  `RibbonSimplifiedCustomizePage` bound to that ribbon is ineligible, and its
  Apply entry points must also reject live changes.
- The library does not retain simplified command copies or native full-group
  borrowing constraints for that ribbon. Turning the capability off while active
  closes its flyouts, returns borrowed content and cancels simplified motion/
  KeyTip levels before restoring Classic. Keep ordinary QAT membership intact.
- Existing simplified profiles remain preserved but inert. Loading/saving a
  customization file must not enable the feature, discard profiles merely because
  the host disabled it, or serialize the capability as an end-user customization.
- Keyboard navigation, KeyTips and automation expose the effective Classic
  controls. If a focused simplified control disappears, transfer focus to a
  usable corresponding command or tab after layout, without an animation delay.
- Each Ribbon has its own setting. A disabled ribbon must not disable another
  ribbon, global motion, density or unrelated popups in the same application.

Custom templates and consumer-owned selectors must honor the capability. Shared
templates/pages/menu builders should provide the complete behavior. A host-authored
toggle or options page binds its availability to the same public property; the
library cannot discover and remove arbitrary application UI.

## Designer contract

Expose the capability in the Visual Studio Properties grid and Ribbon Editor as
an authored Boolean, with a description distinguishing it from a preview. A
disabled feature must also appear disabled on the design surface. Designer
preview commands must not bypass it.

Retain per-command placement/label values while disabled so authors can prepare
or re-enable the feature without losing their defaults. Explain their inactive
state in the editor. Continue using the isolated net472 Model API, string runtime
type names and one undo transaction per edit. No design-tools runtime-project
reference is needed.

## Other features to assess

| Feature | Existing control | Question for a later bounded slice |
| --- | --- | --- |
| Simplified Ribbon | LayoutMode selects a layout; command placement selects defaults | Add the first capability gate described above |
| QAT | IsQuickAccessVisible controls visibility; membership/customization remain supported | Is visibility sufficient, or does a consumer need to suppress adding/removing/customizing commands too? |
| Ribbon/QAT customization | Shared pages, context actions and host request events | Should author-controlled customization gates be independent of each other and of layout availability? |
| Touch density | Density selects Compact or Touch | Does a consumer need to enforce Compact across all selectors/preferences, or can it own the selector alone? |
| File surfaces | Backstage/ApplicationMenu content determines available surfaces | Reuse the existing content contract unless a concrete capability gap is demonstrated |
| Motion | Existing global, per-action and reduced-motion settings | Reuse these policies; avoid a duplicate feature flag |

These are assessment questions, not approved APIs or additional work in S6.

## Delivery and verification

1. **F1 — contract/API review:** agree on naming, disabled programmatic behavior,
   runtime re-enable/focus behavior, standalone page eligibility and host binding.
2. **F2 — Simplified Ribbon gate:** implement the single shared RibbonKit capability,
   including templates, menus, customization entry points and designer authoring.
   Showcase only demonstrates the public contract. Record intentional additions in
   the additive API baseline with XML documentation.
3. **F3 — acceptance:** verify an independent consumer with two differently
   configured ribbons, authored-XAML reload, runtime on/off, active group/QAT
   flyouts, binding preservation, imported/restored settings, keyboard/KeyTips,
   reduced motion and affected themes/directions. Include live Visual Studio
   property/undo/reload checks and the applicable API/build/package gates.

No implementation, test run, schedule or public API change is implied by this
plan. The full simplified-feature validation remains deferred until the user
explicitly requests it; earlier unresolved gates stay in its active plan.

The next planned follow-on is the
[Toolbox item-definition refresh](17-TOOLBOX-ITEM-DEFINITION-PLAN.md). Its inventory
and authoring decisions follow this feature-availability contract.
