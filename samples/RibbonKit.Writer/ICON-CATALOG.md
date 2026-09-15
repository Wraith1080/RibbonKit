# RibbonKit Writer icon catalog

`Icons.xaml` is the source of truth: the 2026-09-08 source inventory contains
113 `DrawingImage` resources, including five `.Large` variants. Icons are app-owned;
resource existence does not mean the associated feature is implemented.

## Visual contract

Use a 24-unit vector grid and stable `Icon.Writer*` keys. Large artwork is reserved
for commands with primary visual weight. Shared palette pens use rounded 1.4-unit
strokes. Dark ink owns structure, muted blue secondary/action detail and muted amber
color/warning/emphasis; paper/slate/shadow remain structural neutrals. Related commands
share the palette rather than receiving group-specific colors. Palette resources are
dynamic so appearance changes do not require geometry replacement.

## Sources and uses

- [Icons.xaml](Icons.xaml) contains Home, File, Page/View, Insert, Table/Picture and
  general-purpose resources; inspect consuming XAML/code before treating one as reserve.
- `Document`, `Save`, `Paste`, `Undo` and `Redo` have explicit `.Large` variants.
- `Home`, `BackstageNew`, `BackstageOpen`, `BackstageSave`, `BackstageSaveAs`,
  `BackstagePrint`, `Options` and `Exit` are File-navigation silhouettes. Backstage
  uses a foreground-tinted mask, so cutouts and outline distinctions must survive tinting.
- [Assets/Writer.svg](Assets/Writer.svg) is the identity master; `Assets/Writer.ico`
  supplies the multiframe application icon. Writer's Orb uses the same W mark with
  its blue gradient, distinct from the pale sphere.

Reuse semantically matching resources. The old table labeled Page/View/Insert/Table
artwork as future reserve even after those commands shipped; it has been retired.
Do not expose a dead command merely because an icon exists. For resource discovery
and designer assignment, see the [Icons.xaml browser](../../src/RibbonKit.Design/SETUP-DESIGNTOOLS.md#using-the-iconsxaml-browser).
