# Future theme candidates

These are unimplemented design candidates, not `RibbonTheme` values or release
commitments. Current support is in [README](../README.md#theming--rendering).
The intended order is Office 2021 → Aurora → Warm Sand → Graphite Copper;
Evergreen, Aubergine and Polar Slate remain exploratory.

## Architecture and intake

Keep one shared template family, complete token-key parity, dynamic resources,
opaque backdrop fallback and application-owned icons. Add narrowly scoped tokenized
geometry only when existing metrics cannot express the approved reference. Public
new-theme enum/API additions require review and XML documentation.

Before implementation, establish name/provenance, representative ribbon/title/File/
menu/control images, palette variants, accent/material policy and geometry differences.
The Office 2021 direction still needs user approval of collected references against
current 2019/2024 before implementation; already-given approval need not be repeated.

## Office 2021 reference direction

The project label means the user's sharp-edged, pre-rounded reference: compact 2019
geometry and separators, blue integrated title bar/white ribbon, newer centered
rectangular search/title composition and updated command treatment. Avoid importing
2024 floating cards, pills, Mica dependence or generous spacing. Compare title/QAT,
tab selection, popup shadows and Backstage side by side so the result remains distinct
from both neighbors. Proposed dictionary names and enum value remain provisional.

## Original palettes

### Crystal light study — 2026-09-16

An experimental Office 2024 derivative is available in Showcase under View → Theme →
Crystal preview, or by launching Showcase with `--crystal`. It opens a separate window
with window-scoped Office 2024 tokens and `Themes/Crystal.Light.xaml` overrides.
Compare 2024 removes/reinstates the overlay without changing saved preferences.
The first slice covers title/ribbon surfaces, selected/hover/disabled controls and an
Arrange dropdown. It uses opaque gradients, bright edges and rounded geometry;
it does not implement live blur/refraction or add a public theme enum.

Live inspection found that dropdown templates also consume `Ribbon.ContentBackground`.
Its initial translucency exposed underlying text through menus, so this study keeps
that brush opaque. Separate popup/material tokens need consideration before real
translucency. Dark mode, system materials, whole-surface coverage and DPI/RTL acceptance
remain later slices. The initial Showcase Release build passed with zero warnings/errors.

The 2026-09-21 interaction pass replaces the bright white hover cap/rim with a faint
blue tint and a low-contrast outline. Pressed/open controls use an opaque radial
highlight near the lower center, with a shaded top edge and light lower rim; checked
controls use a quieter version of the same shading. The extra white inner outline is
removed. These are palette-only changes in the preview, using the existing shared
templates and animation policy; no new motion or runtime APIs are introduced.
Showcase Release build: zero warnings/errors. Live checks covered dropdown hover/open,
Escape dismissal and Office 2024 comparison in both directions on the current display.
No full-suite or new DPI/RTL acceptance is claimed; visual acceptance remains with the user.

Follow-up feedback requests the content card's mixed white/dark outline on hover.
The hover rim now grades from white at the top to blue-gray at the bottom, retaining
the quiet fill and the pressed/open shading. Visual checking of this revision is
delegated to the user at their request to conserve usage; the prior live checks above
apply to the earlier revision.

The user's clarified preference is the localized-reflection version: a vector drawing
brush with steady blue-gray sides, a white top-center glint and darker bottom-center
shading. Its slight inset impression is preferred to the full-height gradient's apparent
taper. This version is restored with the original reflection radii (0.48, 0.10).
Geometry, fill and pressed state are unchanged. Visual checking remains with the user.

The next tab study adds a pale-blue radial selected fill and reuses the chosen button
rim, with a 60%-opacity rim and faint blue wash on hover. After feedback, the ordinary
selection underline uses an icy gradient (white upper edge, pale blue center, blue-gray
lower edge) instead of solid blue; the gray hover underline is hidden. The existing
marker geometry and motion are retained. Contextual tab markers retain their own tint.
The 55%-opacity revision proved too subtle in user review. The current marker instead
explores an air bubble inside glass: a pale luminous center bounded by blue-gray upper
and lower edges, plus a localized white glint, at 90% brush opacity. It retains the
existing pill geometry and motion; this is brush shading, not optical refraction.
All tabs reserve the same top/side border thickness to avoid label movement on state
changes. The shared rim drawing leaves the accepted button appearance unchanged.
This remains a Showcase-only token overlay. The initial tab Release build passed with
zero warnings/errors; the marker follow-up also passed, with two transient copy-retry
warnings from the running preview. Tab switching, hover and visual acceptance are left
to the user as requested.

**Next planned slice — contextual tab color (deferred at the user's usage limit):**
Make Crystal tab headers tintable from each tab's existing contextual color, covering
the header surface as well as text and the selection marker. Preserve the glass rim,
bubble highlight and clear idle/hover/selected states across different context colors.
Use the existing contextual-brush contract where possible, keep ordinary tabs neutral,
and preserve live color changes and readable text. Add a Showcase comparison with at
least two differently colored contextual tabs; leave visual acceptance to the user.
This is a plan only; contextual Crystal header tinting has not been implemented.

Values are starting anchors, not approved final colors. Columns list base, raised
surface, border, primary text, secondary text and accent respectively.

| Candidate | Palette anchors | Geometry/material direction |
| --- | --- | --- |
| Aurora | `#111525`, `#1A2135`, `#34415D`, `#F2F5FA`, `#AAB6CA`, `#63A8FF` | Matte indigo, restrained blue/violet title gradient, ~6-DIP corners, thin underline, optional material with opaque fallback |
| Warm Sand | `#E8DDC7`, `#F1E8D7`, `#B8A98F`, `#3D352B`, `#6F6252`, `#2F7D76` | Parchment/sand light surfaces, restrained teal; no texture or ornamental chrome |
| Graphite Copper | `#181818`, `#242321`, `#3A3733`, `#F2EEE7`, `#B8AEA1`, `#D48A3A` | Compact 2–4 DIP charcoal geometry, warm ivory/copper, thin outlines and little shadow |

Aurora group/hover/pressed/checked anchors: `#171D30`, `#26FFFFFF`, `#3DFFFFFF`,
`#344E78`. Warm Sand state anchors: `#DED0B7`, `#CCB995`, `#C5DDD6`.
Graphite Copper state anchors: `#34302B`, `#493B2C`, `#5A4229`.
Keep live accent overrides. Evergreen explores forest/sage/cream; Aubergine plum/lavender
with restrained saturation; Polar Slate blue-gray/cyan and subtle translucency, only
if distinct from Aurora/2024.

Windows contrast themes are separate accessibility work based on system colors,
not an aesthetic preset. Current gallery/scrollbar fallbacks do not establish
whole-ribbon contrast-theme support.

## Acceptance

Cover window/caption/QAT/contextual tabs; all command sizes and states; collapsed
flyouts, galleries, menus, ScreenTips/KeyTips; Backstage/application menu; inputs,
options/customization, message bars, MDI and consumer tokens. Check token parity,
deterministic 100/125/150/200% snapshots, live 175% where needed, open-surface theme
switching, LTR/RTL, normal/maximized edges, reduced motion and backdrop fallback.
Showcase must expose the selected variants before documentation claims they ship.
