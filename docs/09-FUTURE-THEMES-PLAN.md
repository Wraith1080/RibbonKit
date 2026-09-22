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

**Contextual tab study and button refinements — implemented for review:**
Showcase now includes teal Picture Format and purple Table Design tabs. Each has a
Change tint command. `CrystalContextualTab` derives selected/hover surfaces, rims,
darkened header text and the bubble marker from its effective solid contextual brush;
ordinary tabs remain neutral. The adapter updates on color changes and clears its
overrides for Compare 2024. Non-solid contextual brushes retain standard rendering.
The existing shared template is reused; the adapter binds its scoped text token to
the `ContextualHeaderText` part and therefore needs review if that part changes.

`RibbonTab.ContextualSelectionBrush` is an optional marker-only override, with null
falling back to the existing contextual tint. This additive API is needed to preserve
glass marker shading without applying that same brush to the header text. The marker
binding follows override replacement/removal and live context color changes.
Compact Clipboard/Text preview controls now use 3-DIP corners (large buttons retain
8-DIP corners); pressed fills are slightly deeper with a blue-gray lower border.
These compact metrics remain scoped to this preview's control stacks.
The popup follow-up reduces the shell radius from 12 to 8 DIP and scopes a 4-DIP
radius to menu rows through a resource-only style. Shared templates, padding and
shadows are unchanged; visual acceptance remains with the user.
The pressed/checked follow-up adds a soft reflection inside the lower part of the
fill, fading horizontally before the corners. Pressed/open controls use a deeper
lens fill; checked controls use a lighter fill with a quieter reflection. Both keep
a blue-gray outer edge. Hover, geometry and animation are unchanged; this is still
gradient/drawing-brush shading, with visual acceptance left to the user.

**Input study:** the Home tab now includes a font combo, size combo and title text
field, plus a Disable inputs toggle. Font/size update the sample paragraph; title text
updates its heading. Input-only style resources provide a softly inset pale surface,
4-DIP corners, a stronger hover border and blue focus edge, keeping the existing
native input templates and disabled treatment. Compare 2024 restores the baseline
styles without clearing values. Showcase Release build passed with zero warnings/errors;
nine focused Crystal/contextual/input tests passed, including palette isolation and
value preservation during comparison. Visual checking remains with the user.

The next input lighting iteration replaces the broad vertical fill with a pale radial
lens, a narrow upper inset shade and a localized white lower reflection. The resting
rim transitions from a darker top to a light bottom; dimensions, corners and native
editing behavior stay unchanged. This is simulated glass lighting, without background
blur or refraction. Visual acceptance of this iteration remains with the user.

2026-09-22 refinement: reduced the top shade and resting rim contrast after the
first lens appeared too heavily inset. Hover uses a localized white upper glint
and blue-gray lower edge. The input-scoped secondary brush also lights the native
combo chevron; focus retains its distinct accent edge. Showcase Release build
passed with zero warnings/errors and the nine focused Crystal/input tests passed.
The user will check the revised resting and hover appearance.

**Accent study (2026-09-22):** Appearance > Glass tint offers original Blue, Green,
Purple and Amber. The sample-only CrystalPalette builds each variant from the accepted
XAML palette, rotating hue while retaining saturation, lightness, alpha and reflection
geometry. Neutral text, shadows and pure-white highlights remain unchanged. Input
styles receive their own matching palette; focus/selected text use a darker accent.
Blue reloads the exact original brushes, with no cumulative color drift. Each preview
owns its dictionary; application theme preferences and contextual colors are independent.
Compare 2024 temporarily disables the selector and restores the chosen tint on return.
This is a preset prototype, not yet an application-wide accent setting or color picker.

Showcase Release build passed with zero warnings/errors. Eleven focused Crystal/input
tests cover palette replacement, input value preservation, original palette restoration,
contextual tint independence and preset selected-tab text contrast. Visual review of the
new presets remains with the user.

**File panel study (2026-09-22):** the preview explicitly uses Modern Backstage,
with Home, Appearance and a bottom About page. A pale opaque page surface and a softly
tinted rail reuse the accepted glass selection/hover materials; File-button states
follow the same palette. Appearance provides a tint selector for checking changes
while Backstage is open. Compare 2024 disables both tint selectors and restores the
baseline brushes. Native templates, navigation, keyboard behavior and animations remain
in use; no shared runtime changes or real file commands were introduced.

Showcase Release build and twelve focused Crystal/input tests passed, including
realized Backstage palette replacement and baseline restoration without losing the
selected page. Visual review remains with the user.

**Floating workspace redesign (2026-09-22, supersedes the File-panel layout above):**
the user found the rail-and-sheet version too similar to Office. Crystal now has an
original sample-only Backstage template in `Themes/Crystal.Backstage.xaml`: a return
capsule, a horizontal glass navigation island, and independent rounded content cards
over the tinted window surface. Overview includes a document preview and a working
Continue editing action. Appearance and About use the same open canvas. This is an
intentional layout experiment, not a copied Office template or a new runtime theme.
The shared Backstage control still handles selection, back requests and its existing
motion; comparison removes the sample style and restores the stock layout.

The detached adorner does not reliably inherit the preview window's palette/namescope.
`CrystalBackstagePresentation` supplies its own baseline/layout/current palette scope
and binds the document title directly to the live input, fixing the blank title and
fallback colors. Tint replacement updates both window and overlay. Showcase Release
build passed, and thirteen focused tests passed, including detached title updates,
horizontal navigation at 680 DIP, the back request, and comparison restoration.
The user owns visual acceptance; no live keyboard, DPI or RTL acceptance is claimed.

**Layout options (2026-09-22, current default):** preserve the traditional Backstage
structure in a Glass sidebar variant: full-height vertical navigation, a bottom About
entry, and a large page area on the right. Softly rounded boundaries, reflective
selection and tint-aware surfaces carry Crystal's appearance. The floating workspace
above remains available as an alternative. File > Appearance > Choose layout switches
between them without losing the current page, document title or tint. The selection
survives Compare 2024 and is scoped to this preview session. No enlarged-control
mode is implied; the user's clarification was to retain the traditional overall layout.
Showcase Release build passed with no warnings/errors and eight focused Crystal tests
passed, including vertical/footer and horizontal navigation at 680 DIP and restoration
through comparison. Visual review remains with the user.

File hover follow-up: the stronger lens/extra reflections were rejected as too different
from tab headers. File now uses the same subtle tab wash and top-rounded shape.
The sample-only CrystalFileHover adapter puts the existing Tab.HoverBorder brush on the
native InnerRim overlay, visible only while hovered and neither pressed nor checked.
It follows live tint changes, adds no label padding, and restores original resources
for Compare 2024. The adapter depends on TabControlHost, PART_ApplicationButton and
InnerRim template parts; the shared template is unchanged. Release build via the test
run and eight focused Crystal tests passed. Visual acceptance remains with the user.

**Gallery study (2026-09-22):** Design > Document style provides six typography tiles
and a Disable gallery toggle. Picking a tile updates the sample document heading.
Gallery items use 5-DIP corners, the existing reflective tab hover rim and the accepted
checked glass fill; the native selected accent outline stays visible. The expanded
popup keeps its opaque glass surface and gains the same reflective edge. Scoped
gallery resources follow all tint presets without changing ribbon group separators
or unrelated popup borders. Shared gallery templates, viewport ownership, placement
and scrolling behavior remain unchanged. Showcase Release build passed with zero
warnings/errors; nine focused Crystal tests passed, including a realized popup,
scoped resources, live tint replacement and baseline restoration without losing
selection. The user owns visual review and live pointer/DPI acceptance.

Gallery boundary refinement: the hover-style white rim disappeared into the pale
surfaces inside and outside the strip. Its persistent border now uses an opaque
blue-gray base and a restrained tinted top glint, shared by strip and expanded popup.
Tile hover and selection retain their accepted treatment. The dedicated gallery rim
follows tint changes. Showcase Release build and nine focused Crystal tests passed;
the user will check the revised boundary visually.

Gallery surface refinement: both the strip and popup now use an opaque radial lens
with a broad upper reflection and a narrow inner lower reflection. Their persistent
blue-gray border remains intact, separating the material from the outer ribbon. The
gallery-only surface resources follow the accent palette without affecting the ribbon
body or other controls. Showcase Release build and nine focused Crystal tests passed;
realized strip/popup checks confirm both surfaces and tint replacement, with no resource
leakage. Visual acceptance remains with the user.

**Screen-tip study (2026-09-22):** rich tips reuse the gallery's opaque lens and
restrained border, retaining the shared title/description template and 8-DIP corners.
Arrange, Glass tint, the font/size/title inputs, Document style and the disabled command
provide examples alongside Compare 2024. CrystalScreenTipPalette explicitly scopes
the baseline/current palette to each detached tooltip. Its border override sits above
the merged general popup border because ToolTip.Resources precedes Style.Resources.
No shared template or popup timing changes were made. Showcase Release build passed;
sixteen focused Crystal/input tests passed, including tint changes while a real tip
is open and restoration of baseline brushes. Visual review remains with the user.

Screen-tip rim refinement: a dedicated rim retains the gallery-like upper and side
definition, then lightens toward a muted blue-gray bottom (#BECFDE in the original
palette). It echoes the input's lower edge without approaching white. Gallery borders
are unchanged. Showcase Release build and ten focused Crystal tests passed; visual
acceptance remains with the user.

Screen-tip curvature study: a dedicated tint-aware background adds a broad upper
highlight, a pale central reading area and a shaded lower roll ending in a narrow
reflection. The intended impression is gently convex, with the existing distinct rim
retained. Input/gallery surfaces are unchanged. Showcase Release build and ten focused
Crystal tests passed; curvature and strength remain for the user's visual review.

The convex treatment was rejected as too inflated. The next screen-tip iteration uses
a flatter diagonal wash and a broad off-center reflection, with depth concentrated in
the crisp blue-gray rim. An upper-left rim glint and a smaller lower-right reflection
replace the symmetric top/bottom bands. Geometry and readable text remain unchanged.
Showcase Release build and ten focused Crystal tests passed; visual review is pending.

The following screen-tip iteration keeps that flatter background and separates the
edge into a continuous muted blue-gray outer line and a faint inner white reflection.
A soft shadow provides separation from pale surroundings, with margin to avoid
clipping. A sample-only screen-tip template provides these independent layers and
retains the title/description visibility behavior. Its dedicated border resource
replaces the earlier scoped general-border override; accent changes still apply to
open tips, and comparison restores the baseline template. Showcase Release build
and ten focused Crystal tests passed. Visual acceptance remains with the user.

The user accepted the separate outer rim, inner reflection and soft shadow for
screen tips. The next gallery study leaves that treatment intact and gives both
the ribbon gallery and its popup a shallow recessed surface: a narrow inward top
shadow, softer side shading, a clear opaque center and a restrained lower reflection.
The distinct gallery border, tile states and accent recoloring remain in place.
Showcase Release build and ten focused Crystal tests passed. The user accepted the
recessed gallery treatment. A follow-up adds a narrow, off-center white specular
glint along the inner lower edge of both surfaces, fading before the corners and
leaving the outer border intact. Release build and ten focused Crystal tests passed
again; visual acceptance of the added glint remains with the user.

The lower glint alone did not give the gallery enough glass character. The next
iteration pairs a thin bright upper lip with the inward shadow immediately below,
adds a soft off-center face reflection and concentrates the rim reflection over that
area. Both the strip and popup use this raised-edge/recessed-center study. Screen
tips are unchanged. Showcase Release build and ten focused Crystal tests passed;
visual acceptance remains with the user.

The user accepted the gallery's raised-edge/recessed-center treatment. The File
button now uses 8-DIP rounding on all four corners, including its complete hover
rim. Minimized ribbon tab headers (ordinary and contextual) use the same complete
rounding; expanded headers retain their connected bottom corners. A sample-only
binding scopes the tab metrics to the preview ribbon and removes them for baseline
comparison. Showcase Release build and eleven focused Crystal tests passed,
including minimize/expand, tint replacement and comparison restoration. Visual
acceptance of these header shapes remains with the user.

Release solution build passed with zero warnings/errors; the library suite passed
400/400 and existing visual snapshots 1/1. The full Writer run passed 465/472, with
seven failures including WindowChrome cross-thread initialization. Isolated reruns
also failed on an expected menu missing Settings and a table-selection count (2 vs 1).
No Writer code was changed. New Crystal visual review and DPI/RTL acceptance remain
with the user; existing snapshot success does not establish Crystal visual acceptance.

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
