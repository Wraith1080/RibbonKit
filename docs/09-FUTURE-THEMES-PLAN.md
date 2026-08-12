# Future theme expansion plan

> **Status:** post-v1 candidate track. Office 2021 is the leading first addition; implementation has
> not started.

RibbonKit may add more visual themes after v1.0.0. The first candidate is **Office 2021**, which was
deliberately skipped before v1 because it is a transitional step between the existing Office 2019
and Office 2024 themes. It now has value as a deliberate bridge rather than a release blocker. Later
choices can be additional official variants, later generations, or explicitly named RibbonKit/
community palettes.

## 1. Architectural boundary

- Preserve one shared control-template set. A new look should normally be a complete token
  dictionary, plus its dark/black overlay where that variant is promised.
- Do not hardcode theme colors or metrics into shared templates.
- Every new token must exist in every shipped theme dictionary; themes that do not use the effect
  receive a transparent/zero value.
- A genuinely different geometry may add tokenized metrics or narrowly scoped template triggers,
  but must not fork the complete template family.
- Continue using `DynamicResource` so open ribbons, popups, custom controls and projections recolor
  during a live `ThemeManager` switch.
- Adding a value to the public `RibbonTheme` enum is an additive API change and requires XML docs,
  API-baseline review and switch/default-path tests.

## 2. Leading candidate: Office 2021 pre-rounded transition

RibbonKit's `Office2021` label means the **sharp-edged, pre-Windows-11-refresh Office UI** shown in the
user-approved reference. It is not the later rounded visual refresh that is already substantially
represented by RibbonKit's Office 2024 theme.

The intended position is visibly between the current themes:

- preserve Office 2019's compact density, square control edges, flat group geometry and thin
  separators;
- retain the saturated blue integrated title bar and white ribbon surface;
- add the newer centered title-bar search box and its rectangular results flyout;
- use the updated command/icon treatment and later-era title/QAT arrangement without introducing
  Office 2024-style rounded cards, pill-like controls, Mica surfaces or generous spacing;
- keep menus, Backstage and popup edges predominantly square, with only subtle system-level rounding
  where the reference actually shows it.

The key visual signature is therefore **almost Office 2024 in capability and title-bar composition,
but still Office 2019 in geometry**. Reference capture must settle exact title-bar/search dimensions,
tab underline/selection, group separators, hover/pressed washes, popup shadow, Backstage treatment
and light/dark variants. A side-by-side matrix must make the 2019 -> 2021 -> 2024 progression legible
without letting 2021 collapse into either neighbor.

Provisional implementation shape:

- `RibbonTheme.Office2021`;
- `Tokens.Office2021.xaml` and `Tokens.Office2021.Dark.xaml`;
- the existing shared `Office2024.xaml` control-template aggregator;
- a Showcase selector entry and complete theme/variant snapshot rows.

Icons remain application assets. RibbonKit should match the reference's built-in chrome glyph weight,
but a theme must not silently replace application-authored command artwork.

## 3. Original RibbonKit theme shortlist

After Office 2021 closes the historical gap, RibbonKit should add an original theme that establishes
an identity beyond reproducing Office generations.

### 3.1 RibbonKit Aurora — flagship original candidate

Aurora is the recommended first original theme: a dark indigo foundation with restrained
blue-violet depth. It must not be merely another Black variant. Its identity comes from a coherent
material and geometry policy:

- matte indigo ribbon and group surfaces;
- a subtle blue-to-violet title-band gradient with an opaque fallback;
- approximately 6-DIP corners, between Office 2019's flat geometry and Office 2024's stronger
  rounding;
- a thin accent underline for the selected tab;
- translucent hover/pressed washes instead of strong state borders;
- a neutral dark Backstage rail, with accent reserved for selection and intentional commands;
- optional Mica/Acrylic title material without making the theme dependent on DWM support;
- subtle inner highlights rather than Office 2007/2010 gloss.

Provisional core palette:

| Role | Value |
|---|---|
| Window/ribbon base | `#111525` |
| Raised control surface | `#1A2135` |
| Group surface | `#171D30` |
| Border/separator | `#34415D` |
| Primary text | `#F2F5FA` |
| Secondary text | `#AAB6CA` |
| Default accent | `#63A8FF` |
| Hover wash | `#26FFFFFF` |
| Pressed wash | `#3DFFFFFF` |
| Checked surface | `#344E78` |

Application accent overrides must remain supported. Application-authored icons are not recolored or
replaced except through the existing explicit QAT monochrome behavior.

### 3.2 Warm Sand — non-white light candidate

Warm Sand provides a light theme without white or cool gray as its dominant surface. It uses muted
parchment/sand surfaces, dark brown-gray text and a restrained teal accent. The intended result is
comfortable and professional, not textured or ornamental; all rendering remains vector/solid or
tokenized gradient.

Suggested anchors: base `#E8DDC7`, raised surface `#F1E8D7`, border `#B8A98F`, primary text
`#3D352B`, secondary text `#6F6252`, accent `#2F7D76`, hover `#DED0B7`, pressed `#CCB995`, and checked
surface `#C5DDD6`.

### 3.3 Graphite Copper — warm professional dark candidate

Graphite Copper combines charcoal surfaces and warm ivory text with a copper/orange default accent.
It should use compact 2–4 DIP geometry, restrained one-pixel outlines and almost no shadow, giving it
a denser engineering/creative-tool character than Aurora.

Suggested anchors: base `#181818`, raised surface `#242321`, border `#3A3733`, primary text
`#F2EEE7`, secondary text `#B8AEA1`, accent `#D48A3A`, hover `#34302B`, pressed `#493B2C`, and checked
surface `#5A4229`.

### 3.4 Later exploratory palettes

- **Evergreen:** deep forest and sage with pale cream text; a calm low-saturation dark option.
- **Aubergine:** dark plum with restrained lavender highlights; elegant but requires strict
  saturation/contrast control.
- **Polar Slate:** cool blue-gray surfaces with an icy cyan accent and subtle translucency; reject it
  if it overlaps too closely with Office 2024 or Aurora.

High contrast is not part of this aesthetic shortlist. It should follow system accessibility colors
and behavior rather than appear as another user-selected RibbonKit palette.

Recommended order: **Office 2021 -> RibbonKit Aurora -> Warm Sand -> Graphite Copper**. Evergreen,
Aubergine and Polar Slate remain exploratory until the earlier themes establish how much maintenance
the expanded visual matrix requires.

## 4. Theme intake checklist

Before implementation, record:

1. Theme/variant name and whether it represents a real Office generation or RibbonKit styling.
2. Reference images for ribbon, title bar, Backstage/application menu, menus and common controls.
3. Light/dark/black scope and accent-color behavior.
4. Window backdrop policy (opaque, Mica, Acrylic, or generation-specific glass).
5. Geometry differences that cannot be expressed by existing tokens.
6. Licensing/provenance of any reference or distributable artwork.

Prefer a token-only variant as the first post-v1 theme slice. It exercises the extension boundary
without committing to another generation-specific control-template branch.

## 5. Surface checklist

Each selected theme must cover, rather than only recolor the main ribbon:

- `RibbonWindow`, title bar, caption buttons, QAT placements and contextual tabs;
- normal/toggle/dropdown/split controls at all adaptive sizes and states;
- groups, collapsed flyouts, galleries, menus, ScreenTips and KeyTips;
- Backstage and Office 2007-style application-menu surfaces;
- inputs, customization/options dialogs, message bar and MDI chrome;
- disabled, hover, pressed, checked, focus and high-contrast-sensitive states;
- custom-control consumer tokens defined by the integration plan.

## 6. Verification gate

- Token-key parity tests pass against every existing theme and variant.
- No new literal theme color/metric leaks into shared control templates.
- Deterministic snapshots cover the representative matrix at 100/125/150/200% DPI, with manual
  175% inspection where fractional rounding is most revealing.
- Live switching verifies open ribbon content, QAT, Backstage/application menu, popups and custom
  projections without stale resources.
- LTR/RTL, normal/maximized window edges, reduced motion and the supported backdrop modes are checked.
- The Showcase exposes the new theme and its intended variants before release documentation claims
  support.

Office 2021 remains first. Its implementation begins only after representative Word/Excel/PowerPoint
references are collected and the user approves the intended midpoint against RibbonKit's current
Office 2019 and Office 2024 themes. Original themes then follow the ordered shortlist above, with
their palettes treated as starting points rather than approved final values.
