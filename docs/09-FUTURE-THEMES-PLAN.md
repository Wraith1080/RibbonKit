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
