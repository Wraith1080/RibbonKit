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

Input edge follow-up: the idle combo/text-box rim now ends in muted blue-gray
instead of near-white, separating the bottom edge from the pale ribbon. The white
reflection remains inside the existing lens surface, and the local accent palette
continues to recolor the outline. Showcase Release build and seventeen focused
Crystal/input tests passed; visual acceptance remains with the user.

The user accepted the input bottom outline. The next study covers RibbonCheckBox
and RibbonRadioButton through sample-scoped resources, retaining the native shared
templates and interaction. Idle indicators reuse the clear input lens with a muted
outline; selected/mixed indicators use a deeper tinted glass fill with a narrow
reflection and white marks. Checkbox corners and row hover washes use 3-DIP rounding;
radio indicators remain circular. Design exposes unchecked, checked, mixed and
grouped examples plus an Enable options toggle for disabled-state review. Tint
replacement recolors both the merged idle lens and selected fill. Showcase Release
build and nineteen focused Crystal/option-control tests passed, including state,
grouping, tint, disabled and baseline-restoration coverage. Visual review remains
with the user.

The first option-control study was too solid in appearance, and its Enable options
ElementName bindings failed after group-content reparenting. The preview now binds
both panels directly to the toggle instance. A test of the actual preview window
reproduced the failure and passes with the fix; local icon resources let that window
load independently of Showcase application resources. Selected indicators now use
pale tinted lenses, shaded rims, dark marks and localized upper/lower reflections.
Showcase Release build and twenty focused Crystal/option-control tests passed.
Visual acceptance of the revised indicators remains with the user.

The user found the indicator treatment too flashy and identified the broad shading
as the main issue. The next pass keeps the center nearly even, concentrates the
shade transition near the perimeter, and reduces the size/intensity of the white
reflections. Indicator dimensions and marks are unchanged. Release build and twenty
focused Crystal/option-control tests passed; visual review remains with the user.

The radial checkbox shading still looked uneven, and sharing the selected fill with
the outline/focus brush constrained further refinement. Crystal now uses sample-only
option templates with independent selected-surface, selected-border and focus keys.
The checkbox face has a quiet vertical wash instead of circular inner shading; the
radio has a continuous muted tinted outline. Focus uses a compact ring around the
indicator instead of a full-row box. Native hover/press wash parts, check/mixed marks,
radio grouping and disabled behavior remain. Comparison restores the shared template.
Showcase Release build and twenty focused tests passed, including focus transfer,
transparent row borders, tint replacement and baseline template restoration. The user
retains visual acceptance.

Customization preview: Home / Appearance / Customize and both native right-click
customization requests now open the standard RibbonOptionsDialog with Ribbon and
Quick Access pages targeting this preview. Reset uses its captured initial layout;
changes remain session-local. Owned dialogs explicitly receive the current palette.
Crystal adds sample-scoped list/tree row templates for hover, selection and focus,
while preserving the tree's selection/expansion bindings and header part. The native
scrollbar templates use tinted glass thumbs, a visible outline and 14-DIP rails;
action buttons and the navigation background follow the same palette. Baseline
comparison opens the unmodified standard presentation. Release Showcase build and
thirteen focused Crystal tests passed, including opening both pages, selected rows,
tree expansion and realized scrollbar resources. Visual review remains with the user.

Customization navigation/actions follow-up: selected navigation entries now reuse
the active tab surface, rim and crystal underline, with rounded standalone corners.
An accent outline replaces the platform dotted navigation focus box. All page actions,
including compact reorder/import/export buttons and OK/Cancel, reuse the Backstage
action template with dialog-sized padding; OK has a semibold accent label. Shared
commands, default/cancel behavior and page switching remain in place. Release build
and thirteen focused Crystal tests passed, including active-marker transfer and
realized OK/Cancel glass surfaces. Visual acceptance remains with the user.

OK now uses a darker version of the effective accent with white text, a narrow
internal reflection and separate hover/pressed depths. These overrides are local
to OK, leaving other action buttons pale. Resolve the accent through dialog resources:
the default blue overlay inherits that key from Office 2024 rather than defining it
itself. Scrollbar rails are transparent while thumbs and arrow buttons remain visible.
Release build and thirteen focused Crystal tests passed, including the realized
transparent rail and isolated primary-button surface. Visual review remains with
the user.

Customization material follow-up: OK's dark fill was too subdued, so its face now
mixes light into the effective accent, with a localized upper glint and stronger
lower reflection. The vertical scrollbar thumb uses dedicated shading across its
width and an internal lengthwise reflection instead of a stretched button surface;
hover and drag deepen its tint. Rails remain transparent. Release Showcase build
and thirteen focused Crystal tests passed; visual review remains with the user.

The stronger thumb/OK effects were rejected. The thumb now reuses the scrollbar
arrow-button surface and border with a 3.5% dark wash (5% hover, 8% drag), retaining
the transparent rail. OK reuses each ordinary Backstage button state with only a
4% accent wash and the normal dark label. The bespoke lighting helpers were removed.
Release Showcase build and thirteen focused Crystal tests passed; visual acceptance
remains with the user.

Tint-strength follow-up: OK's wash is increased to 12% to distinguish it from Cancel
at a glance. The thumb's neutral dark wash is replaced by a faint 6% effective-accent
wash, increasing to 8% on hover and 11% during dragging. Accepted base surfaces and
transparent rails are retained. Release build and thirteen focused Crystal tests
passed; the user will review the resulting tint strength.

The user accepted customization tint strength for now. The next study detaches the
below-ribbon QAT into a content-sized glass panel with a 7-DIP gap, 10-DIP corners,
a stronger version of the tab-hover wash and the existing reflective control rim.
The ribbon body's lower corners remain rounded. A sample adapter changes the existing
host without reparenting commands or replacing the runtime template; comparison clears
the overrides. The preview starts with Paste, Copy and Select below the ribbon for
inspection. Release Showcase build and fourteen focused Crystal tests passed, covering
the measured gap, compact width, tint changes, minimization, position switching and
baseline restoration. The user now owns both launching and visual review; no preview
was launched by the agent for this pass.

QAT refinement (2026-09-23): the panel now spans the ribbon body's width, uses a
3-DIP gap and 2-DIP vertical padding. The stronger material was rejected; its fill
and border now reference the original tab-hover brushes directly, without the
panel shadow or separate reflection layers. Baseline comparison restores the original
effect and host geometry. Release build and fourteen focused Crystal tests passed;
launching and visual review remain with the user.

The quieter QAT surface was accepted but felt flat. A depth-only follow-up adds a
soft 1-DIP downward shadow (5-DIP blur, 16% opacity), retaining the tab-hover fill,
outline and compact full-width geometry. Release build and fourteen focused Crystal
tests passed; launching and visual acceptance remain with the user.

QAT drawer study: remove the gap and inset each end by 16 DIP relative to the body.
The QAT now shares the ribbon's fill/rim, with no top border and rounding only at
its lower corners while expanded. Native minimized-state triggers restore a complete
rounded outline and a small gap when the body is hidden. The shallow shadow remains.
Release build and fourteen focused Crystal tests passed, including measured drawer
alignment, minimized geometry and baseline restoration. The user will launch and
review the new composition.

Drawer correction: use the original tab-hover fill/rim with square upper corners,
not the ribbon-body fill. Raise the tab/body host above the QAT in drawing order so
its shadow falls onto the drawer seam; direct the QAT's own shallow shadow upward.
Comparison restores the original stacking. Build passed after closing the preview
to resolve executable-lock warnings; fourteen focused Crystal tests passed. The user
will relaunch and assess the seam visually.

Release solution build passed with zero warnings/errors; the library suite passed
400/400 and existing visual snapshots 1/1. The full Writer run passed 465/472, with
seven failures including WindowChrome cross-thread initialization. Isolated reruns
also failed on an expected menu missing Settings and a table-selection count (2 vs 1).
No Writer code was changed. New Crystal visual review and DPI/RTL acceptance remain
with the user; existing snapshot success does not establish Crystal visual acceptance.

The separate-panel comparison temporarily returned the QAT to a
separate full-width panel using the ribbon body's fill and rim, with a 3-DIP gap,
10-DIP rounded corners, compact padding and a shallow downward shadow. Removed the
drawer's inset, open top edge and stacking override. Release Showcase build passed
without warnings; fourteen focused Crystal tests passed, including panel alignment,
palette replacement, minimized geometry and baseline restoration. Launching and
visual acceptance remain with the user.

After comparing the separate panel in the preview, the user preferred the drawer.
Restored the previous tab-hover surface, 16-DIP side insets, square upper corners,
open top edge and upward seam shadow. The separate panel remains a recorded study;
the drawer is the current direction.

The current material trial reuses the ordinary selected-tab fill and rim directly,
without its pill marker. QAT color follows only the global Crystal accent; selecting
or recoloring a contextual tab must not recolor it. The original gradient geometry,
ribbon shadow and drawer shadow are preserved. The contextual-color binding and
shadow-reduction experiment was reverted by the user. Release Showcase build and
fourteen focused Crystal tests passed; the QAT regression now compares actual
brushes with a long selected tab and verifies contextual isolation, global accent
updates, original body shadow, minimized layout and baseline restoration. User
visual review remains pending.

QAT depth follow-up: move its own shadow downward (2 DIP, 8-DIP blur, 18% opacity)
so the lower edge and sides separate from the window background. The ribbon body's
original shadow, drawer geometry and active-tab material are unchanged. Release
Showcase build and fourteen focused Crystal tests passed; user visual review pending.

The added QAT shadow was rejected and removed. Following clarification, the latest
trial uses the inactive tab's hover background and hover rim. Global accent changes
still apply; contextual colors do not.
The ribbon body's shadow is preserved. Release Showcase build and fourteen focused
Crystal tests passed; visual review remains with the user.

QAT rim experiment: vertically mirror a dedicated copy of the hover border brush,
placing its white reflection along the bottom. The shared tab rim, QAT fill and
shadow settings are unchanged. Global tint replacement preserves the mirror.
Release Showcase build and fourteen focused Crystal tests passed; visual review
remains with the user.

The bottom-lit QAT rim was accepted. A small separation shadow now sits below it:
1-DIP depth, 4-DIP blur and 10% opacity. This is lighter and tighter than the earlier
rejected shadow; the fill, bottom reflection and ribbon shadow remain unchanged.
Release Showcase build and fourteen focused Crystal tests passed; user review of
the new shadow remains pending.

The first separation shadow was too subtle. The next trial increases downward
depth to 2 DIP, blur to 6 DIP and opacity to 16%. Release Showcase build and fourteen
focused Crystal tests passed; visual review remains with the user.

Shadow alignment: QAT now uses 315 degrees, matching the ribbon body's rendered
down-and-right direction, while retaining 2-DIP depth, 6-DIP blur and 16% opacity.
Release Showcase build and the focused QAT regression passed. The regression now
initializes the ribbon before loading its resource dictionary so it also runs in
isolation, and verifies shadow-direction parity. Visual review remains pending.

The directional QAT shadow was changed to surrounding depth: centered (zero offset),
10-DIP blur and the existing 16% opacity. Its spread extends around the panel instead
of favoring the lower-right edge. The accepted fill, bottom-lit rim and ribbon-body
shadow remain unchanged. Release Showcase build and the focused QAT regression
passed; user visual review remains pending.

Opacity trial: increase the centered QAT shadow to 50%, keeping its 10-DIP blur
and zero offset. Release Showcase build and the focused QAT regression passed;
the user will assess the stronger shadow.

The user settled the centered QAT shadow at 30% opacity because 50% darkened the
translucent panel fill. That value is preserved.

Next slice: Crystal KeyTips reuse the existing badge geometry with an opaque pale
glass fill, a small lower reflection, a defined tinted rim and stable dark text.
Three window-scoped brush overrides keep the shared adorner and keyboard behavior
unchanged. The preview status line points to Alt for discovery. Release Showcase
build passed without warnings; all 48 selected Crystal/KeyTip tests passed, including
a new realized-badge test for live palette replacement, dimming, unchanged sizing
and baseline restoration, plus existing animated LTR/RTL placement checks. The user
will launch and inspect Alt/F10 labels; no new live UI or DPI acceptance is claimed.

Utility-button pass: collapse/expand, pin, scroll arrows and QAT overflow now use
the accepted command glass shading through their existing shared TabStrip tokens.
Hover uses the checked-button surface at 80% strength; press uses the deeper pressed
surface. Idle tab-scroll buttons borrow the opaque ribbon-body surface. The shared
templates also carry this styling to merged child-caption buttons. Geometry and
interaction behavior are unchanged. Release Showcase build and 54 selected Crystal,
RibbonScrollBar and QAT overflow checks passed. The user will inspect the small
controls' hover/pressed appearance in the preview; no live visual acceptance claimed.

Utility-rim and preview-access follow-up: `CrystalUtilityChrome` adds a 1-DIP,
non-hit-testable overlay to the shared utility Chrome parts. Hover/open states use
the existing glass rim; pressing uses the pressed border. Disabling Crystal removes
the wrapper and restores the original content. No runtime templates were copied.
The window minimum width is now 420 DIP. Home → Appearance → Preview controls can
add temporary navigation tabs to expose scroll arrows, or move QAT to the tab row
with extra sample commands and a 100-DIP cap to expose overflow. Both actions toggle
back and remove only their own sample items; QAT restores its prior width and position.
After adding/removing tabs, the preview realizes headers and calls the public scroll
host Refresh method to invalidate its cached extent. Release Showcase build passed;
all 54 selected Crystal, RibbonScrollBar and QAT overflow tests passed. Integration
coverage checks narrow sizing, both realized arrow rims, overflow opening, cleanup
and comparison restoration. User launching and hover/pressed visual review remain pending.

Body-scroll preview follow-up: Home → Appearance → Preview controls → Keep Home
groups expanded temporarily sets every Home group's CanResize to false. Toggling
back restores each saved value. Narrowing the window now exposes the body arrows
without group reduction; these use the same Crystal translucent-idle/opaque-hover
surface wiring as tab arrows. Release Showcase build and all 15 Crystal tests passed,
including both realized body arrows and resizing restoration. Live hover appearance
remains for the user's preview check. After visual review, body arrows use a 32-DIP
width and 10-DIP corner radius: copying the body's 14-DIP radius onto a 22-DIP
button made its ends look pinched. Tab arrows retain their original dimensions.
The 15 Crystal checks also cover body-arrow insets, rim geometry and restoring
the original width/radius for comparison; Release Showcase build passed.

Message-panel trial: Home → Appearance → Preview controls can show two sample
notices one at a time or dismiss both. Each notice uses a rounded amber glass strip
and the existing Backstage action-button style. Amber remains semantic rather than
following the selected glass accent. The original message templates retain action,
close, wrapping, live-region and animation behavior; a sample-only presentation
adapter rounds each row and restores the baseline on comparison. Notices sit below
the below-ribbon QAT with a small gap, preserving the drawer's corners and shadow;
they also work with QAT in the tab row. Release Showcase build and 21 Crystal/message
checks passed, covering both placements, separate dismissal/reopen, actions, empty
bar spacing and comparison. Animation was disabled for the layout assertions;
live appearance and motion remain for the user's preview review.

The user accepted the softer message gradient with background brush opacity 0.75
and border brush opacity 0.85. These brush-level settings preserve fully opaque
text, icons and actions; the semantic amber palette remains excluded from accent
rotation. Keep this treatment as the current message-panel study.

Document/Backstage scrollbar follow-up: the main document and all three Backstage
pages now connect their native ScrollViewer bars to the existing shared
RibbonKit.ScrollBarStyle. CrystalScrollBars centralizes the accepted customization
palette (transparent track, 14-DIP thickness, lightly tinted thumb, 4-DIP corners),
also used by CrystalCustomization. Local scopes survive Backstage reparenting,
refresh on accent changes, and are removed for Office 2024 comparison. No shared
runtime templates were copied or changed. Release Showcase build and 39 focused
Crystal/scrollbar checks passed, including realized bars, scrolling and comparison
restoration on all four viewers. Live appearance remains for user review.

Application-menu study: Preview controls can switch File between Backstage and a
sample RibbonApplicationMenu. The sample includes direct commands, a split Save as
row, a pane-only Print row, recent documents, disabled commands and footer actions.
CrystalApplicationMenuPresentation maps existing Crystal materials onto the shared
application-menu tokens, rounds the combined split-row silhouette, and sizes the
right pane to fit the narrow preview window. Tint changes refresh the materials;
comparison removes the local overrides and restores Office 2024. All sample commands
only report status. The menu footer can restore Backstage or close the menu.
Release Showcase build and 26 focused Crystal/application-menu/KeyTip tests passed,
including pane switching, sample action dismissal, narrow width, live tint refresh
and comparison restoration. Real hover/keyboard appearance remains for user review.

Application-menu material refinement: the navigation separator now has 12-DIP side
insets and 4-DIP vertical breathing room. The top/footer bands are transparent over
one continuous frame material, avoiding the white gradient restart caused by painting
the ribbon brush separately in each band. The frame adds a soft lower reflection and
the glass rim; the navigation uses the selected-tab lens and the right pane uses the
screen-tip surface. The active page stays transparent over that pane. Accent updates
and comparison restoration remain covered. Release Showcase build and 19 focused
Crystal/menu checks passed; the new material strength remains for visual review.

Menu frame follow-up: the user requested the standard glass outer rim again and
the same background as the below-ribbon QAT (`Tab.HoverBackground`). Transparent
top/footer bands preserve one continuous fill, while the inset content retains its
steady outline. The dedicated centered shadow remains (16-DIP blur, zero offset,
0.24 opacity). Removed the superseded frame-reflection brush. Release Showcase
build and the focused preview integration check passed; visual review remains pending.

Translucent-menu shadow correction: applying DropShadowEffect to the whole menu
made its almost-clear outer frame cast little shadow while its opaque inner panes
and buttons cast visibly inside the frame. CrystalMenuShadow now supplies an opaque
rounded caster in a separate non-hit-testable layer, then clips that layer after
the effect to keep only the outside halo. The interior remains transparent and the
original frame effect returns in comparison mode. The layer is installed before
opening motion chooses its content surface, so frame and shadow move together.
Release Showcase build and the focused preview test passed; a rendered pixel check
detects shadow on all four outer edges and zero alpha at the shadow layer's center.
Live opening motion and final shadow strength remain for user inspection.

Menu transparency trial: the outer frame now uses a dedicated cool gradient at
0.88 brush opacity to reduce readable content showing through. Its slight bottom
reflection, glass border and outside-only shadow remain. This is tint/opacity,
not live backdrop blur; the QAT surface is unchanged. Release Showcase build and
the focused preview/shadow rendering check passed; visual acceptance is pending.

Menu backdrop blur: the translucent frame now overlays a cropped WPF snapshot
of the content behind it, with a 6-DIP Gaussian blur and 24-DIP sampling allowance.
The entire menu and its shadow are excluded during synchronous capture, then restored
before returning to the dispatcher. The blurred layer is clipped after its effect;
foreground text, buttons, rims and the outside-only shadow stay sharp. Captures refresh
on opening, geometry/DPI changes, document scrolling and palette changes, and are
released on hiding or comparison restoration. This is an event-refreshed backdrop,
not continuous live refraction or an OS compositor material. A rendered black/white
edge test checks smoothing, foreground exclusion, rounded clipping, scrolling,
resizing and reopening; the preview integration checks the actual menu wiring.
Release Showcase build and 20 focused Crystal/application-menu checks passed.
The user accepted the 6-DIP blur after live visual review. Opening motion and
mixed-monitor DPI remain unverified.

Dropdown and collapsed-group popup frost trial: Crystal now samples the preview
window beneath each open ribbon dropdown or collapsed-group flyout, applies the
same 6-DIP Gaussian blur with 24-DIP sampling padding, then overlays the accepted
cool application-menu frame tint. The shared popup border, corner radius, shadow,
items and opening behavior stay in place; only the popup background changes.
An opaque base covers any part of a popup that extends beyond the preview window.
Snapshots refresh for opening geometry, owner resizing, scrolling and tint changes.
Compare 2024 restores the original background resource. The Showcase Release build
and two focused popup/rendering checks passed; mixed-monitor DPI remains unverified.

Popup-opacity follow-up: the dropdown menu and collapsed-group flyout now share a
slightly clearer 0.84 tint over the same 6-DIP blur. The accepted application-menu
frame stays at 0.88. The user accepted both popup surfaces after live visual review.

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
