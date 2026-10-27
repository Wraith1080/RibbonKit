# Future WPF improvements

Status: **Deferred design ideas**, 2026-10-27. No WPF implementation is authorized
by this record. Current development remains focused on native WinForms. Revisit
these proposals as a separate WPF slice, including API review and focused checks.

## Checkbox and radio options in Quick Access

The current WPF Add to Quick Access/customization source path supports buttons,
toggles, dropdowns/split buttons, ComboBoxes, galleries and groups. Standalone
`RibbonCheckBox` and `RibbonRadioButton` are excluded; options inside a borrowed
group are a different presentation. Relevant code: `Ribbon.IsSupportedQuickAccessSource`,
`Ribbon.CreateCommandProxy` and `RibbonCommandCatalog.CollectControls`.

### Proposed presentation and API

- Present standalone options in QAT as compact icon buttons using the existing
  toggle-button chrome. Keep ordinary ribbon checkbox/radio indicators unchanged.
- Consider additive optional `Icon` and `LargeIcon` dependency properties on both
  option controls, following the existing button metadata convention. Bind icon
  changes into projections; icons identify the action, such as Gridlines or an
  alignment choice. Preserve a readable no-icon fallback and descriptive ScreenTips.
- Show checked/selected state through QAT chrome. Give a three-state checkbox's
  indeterminate value a distinct mixed-state mark without replacing its action icon.
- Overflow retains the icon, caption and state. Changing an option should keep the
  overflow open, consistent with the intended toggle interaction.

### Source-owned behavior

The source retains bindings, commands, routed events, identity and radio scope.
Do not clone/reparent the source or make QAT proxies into another radio group.
Activation must invoke the source's original behavior once: a checkbox follows
its two/three-state cycle, and a radio selects itself and clears its original
peers. Clicking an already selected radio must not clear it. Programmatic source
updates must refresh all projections without synthesizing another click.

The existing toggle proxy's two-way binding is useful precedent, but must not be
copied blindly for radio activation or three-state checks. Preserve command
target/CanExecute behavior, source availability independent of tab selection,
per-presentation focus/KeyTips, and automation semantics appropriate to the source.

### Later implementation and acceptance

Review the additive API, proxy lifetime/cleanup, catalog/context-menu eligibility,
customization identity and persistence together. Existing saved customization must
remain compatible. Keep this reusable in RibbonKit, with a RibbonKit-only consumer
that does not depend on Showcase helpers or resources.

Focused acceptance should cover checkbox bindings and event order, all three
states, named and unnamed radio groups, repeated selected-radio activation,
duplicate projections, inactive tabs, disabled sources/references, source removal
and disposal, icons/fallbacks, overflow retention, keyboard/KeyTips and the affected
automation contract. Review representative theme/DPI/RTL visuals and all supported
QAT placements, including the title bar. Historical WinForms results do not establish
WPF acceptance. No WPF build or tests are required for this deferred document.

## Backstage navigation width in designer properties

Deferred proposal, **2026-10-27**. The user requested a WPF counterpart to the
native WinForms designer-configurable navigation width. This means authoring
properties with Auto and explicit sizes; it does not introduce a draggable
divider or user-resize interaction. WPF implementation remains a later slice.

### Proposed authoring contract

- Add a designer-visible navigation-width dependency property with an Auto value
  and explicit widths in WPF device-independent pixels (96-DPI logical units).
  Review the exact WPF type/name before implementation; native nullable-integer
  APIs need not be copied into WPF. Include normal binding, styles, Reset/default
  metadata and XAML/designer serialization.
- Include minimum/maximum navigation widths with consistent validation and a
  deterministic assignment-order policy. Preserve each design's current baseline
  (for example, Modern's 200 and Classic's 220 logical pixels) when Auto has room.
- Auto measures visible navigation captions using the actual font, reserved
  icon/picture slot, item padding and scroll clearance. Re-evaluate for caption,
  visibility, font, localization and design changes; retain aligned no-icon rows.
- Constrain the displayed rail to leave useful page space when the host narrows.
  Keep the authored width/binding value unchanged by this temporary constraint,
  so widening restores it. Convert/round once at the layout/render boundary;
  measurement, hit testing, focus/KeyTips and any connected chrome use the same
  resulting geometry across DPI and RTL.
- Keep layout and presentation in shared Backstage controls/templates/resources.
  A Showcase sample may demonstrate the public properties, but must not implement
  navigation width with a template-part patch or local layout helper. Application
  settings persistence remains host-owned.

### Later implementation and acceptance

Map the contract deliberately to each supported Backstage design, including the
rail-based Office/Crystal layouts; floating designs require their own applicability
decision. Preserve selected page, content identity, bindings, editor state,
keyboard navigation, open/close behavior and theme switching while width changes.
No animation, runtime drag interaction or new persistence schema is implied.

Verify property metadata and XAML/designer round-trips for Auto/explicit values,
style/binding changes, bounds and assignment order, narrow/wide restoration,
long/localized/hidden labels, large fonts, icons/pictures/fallbacks and DPI/RTL
geometry. Include a RibbonKit-only consumer and inspect representative rendered
results. WinForms evidence is a reference, not WPF acceptance. This record changes
documentation only; no WPF runtime, build or tests are part of the current task.
