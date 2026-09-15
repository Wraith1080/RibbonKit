# Office 2007 theme reference

S0–S9 are complete at the recorded gates through design-history §3.94. This reference
retains visual measurements and implementation traps; it is no longer a staged work
order. See [current status](../04-DESIGN-NOTES.md#5-current-state--next-steps) and
[history](../04-DESIGN-NOTES.md#3-implemented-features-chronological-with-pitfalls).

## 1. Implemented boundary

Office 2007 uses the shared template set and its light/black token dictionaries.
The real two-pane `RibbonApplicationMenu` ships independently of Backstage.
`ApplicationButtonShape=Orb` supplies the overlapping round File button. Dark mode,
localization/RTL, snapshots and v1 release engineering are no longer future blockers.

## 4. Measured palette — Office 2007 Blue

These are measurements from the original supplied references, not a generated inventory
of current token values. Gradients run vertically. Compare code and the accepted
reference before changing a token; pixel samples include reference rounding.
The defining shape is a light–dark–light valley in title/group surfaces, a flat
`#BFDBFF` tab band, connected selected tabs and gold/orange hard-crease state fills.

### 4.1 Chrome

| Token | Value |
|---|---|
| `Ribbon.Background` (tab strip band) | **flat `#BFDBFF`** — a `SolidColorBrush`, *not* a gradient. Confirmed identical in the Aero and non-Aero shots. |
| `Ribbon.Border` | `#8DB2E3` (ribbon body top edge) |
| `Ribbon.ContentBackground` (groups area) | `0.00 #DBE6F4` → `0.17 #C8D9ED` → `1.00 #E3F4FF` |
| `TitleBar.Background` | `0.00 #E4EBF6` → `0.14 #D5E5FA` → `0.28 #CADEF7` → `1.00 #E4EFFD` |
| `TitleBar.Foreground` | `#15428B` |
| Restored non-Aero edge (see §6) | One native/system edge; no Office-painted full-window band. The `#9BBBE3` strip is client-area padding beside the ribbon/document only. |
| Client area below the ribbon | `#A3C2EA` |

### 4.2 Tabs

| Token | Value |
|---|---|
| `Tab.SelectedBackground` | `0.00 #F0F6FF` → `0.08 #F6FAFF` → `0.15 #EBF3FE` → `1.00 #DBE6F5` |
| `Tab.SelectedBorderBrush` | `#8DB2E3` top, `#94C5EB` sides |
| `Tab.SelectedForeground`, `TabStrip.Foreground` | `#15428B` |
| Unselected tab | `Transparent` — the `#BFDBFF` strip shows through |
| `TabCornerRadius` | `3,3,0,0` |

### 4.3 Hover glass

| Offset | Colour | |
|---|---|---|
| 0.00 | `#FFFFF7` | near-white cream |
| 0.38 | `#FFE798` | end of the pale upper half |
| 0.38 | `#FFD758` | ← **hard crease**, instant step to saturated gold |
| 0.48 | `#FFD350` | darkest point |
| 1.00 | `#FFE89F` | lighter gold at the bottom |

Pressed glass

| Offset | Colour | |
|---|---|---|
| 0.00 | `#FEB96C` | |
| 0.40 | `#FDA461` | |
| 0.40 | `#FC8F3D` | ← **hard crease** |
| 0.63 | `#FC973D` | darkest band |
| 1.00 | `#FEBE63` | |

### 4.4 Contextual bands

| Element | Value |
|---|---|
| Band, top | matches `TitleBar.Background` exactly (`#E3EBF6` → `#D5E5F7`) |
| Band, ~40% down | `#CEDFED` — first visible divergence from the plain blue |
| Band, ~70% down | `#E7EBD4` → `#ECECBC` |
| Band, bottom | `#EEE583` |
| Band bottom edge | **`#FDE41B`** — a 1px saturated yellow line, the strongest signal of the group |
| Band side borders | `#C6D6E5`, 1px |
| Selected contextual tab | `0.00 #FFF6A9` → `1.00 #FFFAD5` (pale yellow, warm) |

### 4.5 Group boxes

| Element | Value |
|---|---|
| Group border | `#A8BFD4` (`#9CBEDA` alongside the label strip) |
| Inner highlight, 1px inside the border | `#E4ECF5` (`#EDF5FB` alongside the label) |
| Group body fill | `0.00 #DEE8F5` → `0.16 #C7D8ED` → `1.00 #D9E8F6` — the valley again |
| **Group label strip** | flat **`#C1D9F1`**, a clear step darker than the body above it |
| Label strip height | ~19px of the ~107px box |
| Corner radius | ~3px |

Historical shared group-token values

| Key | 2007 | Every other theme |
|---|---|---|
| `Brushes.Group.Background` | gradient `#DEE8F5` → `#C7D8ED` @0.16 → `#D9E8F6` | `Transparent` |
| `Brushes.Group.Border` | `#A8BFD4` | `Transparent` |
| `Brushes.Group.InnerHighlight` | `#E4ECF5` | `Transparent` |
| `Brushes.Group.LabelBackground` | `#C1D9F1` | `Transparent` |
| `Metrics.GroupBorderThickness` | `1` | `0` |
| `Metrics.GroupCornerRadius` | `3` | `0` |
| `Metrics.GroupLabelCornerRadius` | `0,0,2,2` | `0` |

Equal gradient offsets create the hard crease. Hover uses a pale inner glow (`#FFF2C7`) and bottom highlight (`#FFFAB3`);
pressed uses border `#B6A892` and orange inner/bottom highlights (`#FDAD10`/`#FDAD11`).
Selected-tab hover adds the gold border/cream rim without replacing its resting fill.

Office 2007 groups are adjacent bordered boxes, not etched inter-group separators:
`Group.Separator` is transparent, and the darker label band has bottom corners that
fit inside the group radius. Group chrome follows the re-homed subtree into collapsed
flyouts. The body border loops around the content; preserve the selected-tab notch.
Contextual hue remains application-controlled. Original token counts and instructions
to add already-existing keys have been removed.

## 5. Orb geometry and ownership

The reference orb is about 36–37 pixels at 96 DPI, centered near the title/tab boundary,
with roughly 22 pixels of upward overhang. Its resting rim was sampled as `#D8DEE8`
with `#8B97A8` edge; hover adds bright `#FEEA37` yellow. Preserve clipping and hit testing
across the title/tab layers. A wrapper must not steal the File slot's stretch alignment.
Public shape/preview properties must remain recognized by the designer.

## 6. Window frame and Backstage contracts

### 6.1 Opaque baseline

The root stays flush with the physical-LTR host; native chrome supplies its edge.
Do not add a duplicate full-window blue band or disturb measured maximize insets.
Client bevel/padding stays inside client templates. Active/inactive title treatment
uses shared tokens and neutral values in other themes.

### 6.2 Optional Aero-inspired frame

`RibbonWindow.FrameAppearance=Office2007Aero` is separate from theme and backdrop.
App-local tint, highlights and the supported Acrylic path retain an opaque fallback;
choosing Office 2007 must not enable material implicitly. The frame collapses when
maximized. Current native origin plus window DPI determines caption hit bounds after
monitor changes; stale WPF screen transforms caused real first-transition failures.

Do not replace this with transparency-window mode, desktop capture/CPU blur, DWM
injection or private composition hooks. Original DWMBlurGlass images were visual
references, not a dependency. Keep deterministic opaque checks separate from live
Acrylic inspection, and store frame/material preferences outside ribbon customization.

### 6.4 Glass2007

This is a RibbonKit-original optional Backstage, not historical Office 2007 UI.
The page stays opaque while the rail may expose material beneath the same frame/title
tint. Inactive composition must account for DWM fallback differences; separate title,
frame and Backstage opacity contracts prevent seams. Reflection/grain are suppressed
in inactive, fallback and Backstage states where separately mapped brushes restart.
Avoid extra nested rail/content borders; preserve local bevel/highlight visibility.

### 6.5 Classic2007

This separate RibbonKit-original opaque dashboard uses one continuous blue perimeter,
white inner highlight, divider and gutter-contained shadow. The implemented Backstage
uses a shared-chrome orb proxy; the real ribbon application button remains in its
layout slot. The old reparent-the-real-button description is obsolete (§3.94).
Only the glyph rotates, and switching/restarting must restore owner bindings and
placement. Glass2007 and Classic2007 remain independently selectable.

## 7. Regression scope

Follow [CONTRIBUTING.md](../CONTRIBUTING.md#proportional-validation), not the retired
per-step commit/test ritual. Check token parity and deferred dictionary scope, all
relevant light/dark surfaces, collapsed groups, orb overhang, backdrop fallback,
active/inactive and normal/maximized geometry, native caption/resize/Snap behavior,
and 100/125/150/175/200% plus mixed-monitor transitions when affected.

## 8. Accent behavior

Reapply theme-aware accent overrides without replacing the 2007 chrome indiscriminately.
Preserve contextual hue and the foreground/contrast policy of title, File and QAT.
Use current `ThemeManager` behavior and recorded reference checks as the boundary.
