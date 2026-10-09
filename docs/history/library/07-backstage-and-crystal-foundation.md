# Library history: Backstage and Crystal foundation

> Archived implementation evidence. Dates, test counts and pending work describe
> their original checkpoints; later records may supersede them.

[History directory](../01-library.md) · [Current status](../../../04-DESIGN-NOTES.md#5-current-state--next-steps) · [Working agreement](../../../AGENTS.md).

### 3.91 RibbonKit-original Glass2007 Backstage prototype — 2026-08-12

The translucent Modern Backstage over the Office 2007 Aero frame suggested a useful post-v1 design
that is intentionally not presented as historical Office behavior. Real Office 2007 still defaults
to RibbonKit's two-pane orb application menu. The additive public
`RibbonBackstageDesign.Glass2007` value is an explicitly selected full-window alternative; neither
the Office 2007 theme nor this design enables Acrylic automatically.

The first visual stage stays in the one shared `Controls.Backstage.xaml` template. Its opaque mode
uses the generation-aware `Backstage.NavBackground`; translucent mode clears that rail and applies
the same low-strength theme/accent tint as the title and outer frame over the app-controlled Acrylic
path. The content sheet remains opaque and keeps only the
established historical shadow. Live review removed the authored rail perimeter, content-edge stroke
and residual top specular line. A follow-up screenshot clarified that the remaining large
frame-sized outline was the window frame's `AeroFrameInnerHighlight` plus the title surface's bottom
highlight, not a navigation-tile border; both now collapse through the existing
`IsTitleBarContentVisible=False` Backstage state.
Hover and selected tile outlines remain crisp, while their fills plus the orb-derived back-button
disc use a shared `0.88` opacity token, allowing a restrained amount of rail color through without
fading text, icons or the back arrow. All five base themes define the new metric, retaining a single
shared template and an identical token contract.

The next live pass exposed three composition details. The translucent rail had used
`Backstage.Classic.NavBackground` while the title used the host-overridable `AeroFrameTint`, and the
rail overlays stayed at active strength after the title became inactive. The rail now binds the same
window tint/intensity and keeps the title's established inactive opacity. While inactive Backstage
owns the window, `AeroInactiveBackstageFrameOpacity=1` compensates for DWM using a different inactive
fallback in the outermost frame pixels than in the transparent client rail. At 125%, the resulting
left frame, bottom frame and rail boundary all sample `#B6BFCA`; using the normal frame attenuation
left a lighter strip. The already-approved top join keeps its existing title/rail composition. The
relative diagonal reflection and tiled grain are
material-only decoration: they appear only for an active, normal title/frame with accepted Acrylic
and are suppressed while Backstage owns the surface, while the window is inactive, or while the
opaque fallback is in use. Mapping those brushes independently into a 34-DIP fill and a full-height
border had visibly restarted them at shared edges. Outside Backstage, inactive Aero instead keeps its
shared fallback base fully opaque and quiets title content separately; title and restored frame then
use the normal `AeroInactiveOverlayOpacity` over that common base. Consequently the inactive Backstage
material stays continuous when Windows suspends Acrylic, while the normal no-material title/frame pair
shares the same inactive fallback composition. Explicitly
opaque `Glass2007` under `Office2007Aero` retains its historical rail and local `1,1,0,1` white bevel.
The title-bottom highlight is now a final,
non-hit-test overlay above the caption buttons; hover/pressed caption fills cannot interrupt or
overlap that one-pixel seam, while Backstage still collapses it with the larger frame bevel.
Dark Office 2007 review also found that the Glass2007 back disc and unselected navigation labels
retained their light-palette colors against the neutral translucent rail. The disc now consumes the
same `ItemSelectedGlass`/`ItemSelectedBorder` pair as a selected tile, its arrow remains explicitly
white, and unselected labels/icons consume the Aero title foreground. That foreground is dark in the
light palette and white in the dark overlay, preserving contrast without forking the template or
changing the page-content foreground.

The Showcase adds a `2007 Glass` selector and persists it through the app-owned appearance document,
separate from ribbon customization. The frame tint slider now has a compact Reset action that assigns
the shared `DefaultAeroFrameTintIntensity` constant (`0.16`), so UI reset, schema migration and the
factory value cannot drift. Logic contracts cover the enum, opaque/translucent template triggers,
generation-aware navigation resources, Showcase wiring and appearance round-trip. Acrylic remains a
live visual check; the next stage is user review of this shell before changing the Backstage page
content layout. The full automated gate passes **318 logic tests plus one visual test covering 62
approved images**.

### 3.92 Separate Classic2007 Backstage concept — 2026-08-13

A new user-supplied concept image explored what a full-window Backstage might have looked like if
Office 2007 had shipped one. It is not historical evidence and does not replace either the real
two-pane Office 2007 application menu or RibbonKit's existing modern `Glass2007` interpretation.
The additive `RibbonBackstageDesign.Classic2007` value is appended as enum value 4, leaving the
already-persisted `Glass2007=3` contract stable. The Showcase labels those two independent choices
`2007 Classic` and `2007 Modern` respectively.

`Classic2007` stays in the single shared Backstage template. Its final shell is an opaque
Office-blue field shared by the 220-DIP navigation rail and the content pane; it deliberately ignores
`Backstage.Translucent`, while retaining that preference for the next switch back to `Glass2007` or
Modern. A single continuous dark-blue perimeter and a matching one-pixel pane divider replace the
earlier segmented and etched borders. The divider terminates inside the continuous white inner
highlight, so the horizontal and vertical strokes never compound at the bottom junction. A second
full-perimeter border carrying `Backstage.ContentShadow` casts depth into the eight-DIP shell gutter.

Classic originally reparented the real ribbon application button into `BackstageAdorner`. That
implementation passed the initial gate, but animated Classic-to-Modern switching later exposed its
dependence on title-row clipping and template ancestry. Section §3.94 supersedes the hosting detail
with a pixel-identical private Backstage proxy while retaining this section's visual design.

When the containing window uses `Office2007Aero`, the Backstage draws a local four-sided white join
bevel between the Aero frame and the opaque shell. The normal window-wide Aero inner highlight stays
suppressed while Backstage is open, preserving the measured maximize/work-area compensation and the
previously approved modern-glass seams.

The Showcase Home page preserves its original `Good morning` content for every established design
and conditionally reveals a Classic2007-only document dashboard: Recent Documents, Preview,
Properties, Quick Actions and a three-part document-management footer. This demonstrates that the
library still accepts arbitrary application content rather than introducing a special-purpose page
control. Live review tightened the content padding, reduced the sample data enough to remove the
scrollbar and added an eight-DIP render gutter around the dashboard. That gutter is balanced by an
equal shell-padding reduction, so the approved layout does not move while every card shadow receives
enough room to render without being clipped by the `ScrollViewer`. A direct same-process and restart
mode-switch check confirmed that `Glass2007` returns to its previous Acrylic rail and original page
unchanged, while Classic restores the same real orb. Final automated gate: **325 logic tests plus one
visual test covering 62 approved images**.

### 3.93 S7 Office 2007 window-frame final verification — 2026-08-13

S7 is complete. Proportional geometry checks cover 100/125/150/175/200% for the frame, maximized
overhang compensation and native maximize hit bounds. Live testing used a real per-monitor-v2
125% 1920×1200 primary display and 150% 2560×1440 secondary display. Repeated restored moves
125%→150%→125% updated the HWND DPI and caption metrics from 58×42 to 69×51 physical pixels and
back without restarting. All eight resize edges/corners retained their native hit codes and direct
drag-resize behavior. Real caption clicks passed minimize, maximize, restore and close; maximize
produced the expected WindowChrome overhang while the measured root inset kept the title, ribbon,
document and status surfaces flush with the monitor work area.

The mixed-monitor pass caught one issue that single-monitor and synthetic `WM_DPICHANGED` checks
could not: WPF `PointToScreen` retained the previous monitor's screen transform for the realized
maximize template part, moving `HTMAXBUTTON` away from the rendered button after a DPI transition.
`RibbonWindow` now transforms the part into window-client DIPs, combines it with the native
`ClientToScreen` origin and applies the window's current `DpiScale`. Focused contracts cover that
conversion at all five required scales, negative monitor coordinates and mirrored corners. On the
real 150% monitor the corrected region measured 69×51 pixels exactly over the visible button; the
Windows 11 Snap Layout flyout appeared, the custom non-client hover/pressed bridge remained visible,
and maximize/restore completed from that same region. Moving back to 125% restored the 58×42 region.

Active/inactive Acrylic, maximized Acrylic and the deterministic opaque fallback were inspected
live. Acrylic remained confined to the transparent title/restored frame, inactive treatment stayed
quieter, and the opaque fallback preserved the same frame geometry. The first post-gate mitigation
for persisted `Classic2007` startup used temporary title-row clipping; §3.94 replaces that workaround
because its animated transition could still expose a partial orb. Actual Acrylic composition remains
a live-only approval rather than a snapshot baseline.

### 3.94 Classic2007 Backstage orb proxy and seamless design switching — 2026-08-13

Classic2007 now owns a private Back button above `BackstageAdorner`; it does not reparent or duplicate
the real application button's command/state ownership. Both presentations render the same
`RibbonKit.Templates.ApplicationOrbChrome` data template, so the sphere, glyph and dynamic theme
resources remain pixel-identical. The real button stays measured and arranged in its original ribbon
slot, but is opacity- and hit-test-suppressed for the entire Backstage lifetime. Ordinary Backstage
designs omit the proxy, while Classic attaches it at the real button's transformed bounds. The old
`RibbonWindow` title-row clipping workaround and `Tag.Backstage.Design` template state are removed;
the historical two-pane application-menu overlay continues to move the real button as before.

The proxy carries the localized Back tooltip and automation name, participates in window-chrome hit
testing, closes through `IsBackstageOpen`, and rotates only the shared `OrbGlyph`. Its content is an
inert string: an early implementation used the owning `Ribbon` as data, which tried to give that
already-parented control a second logical parent and crashed on open. The private proxy template also
pins its `ContentPresenter` to `VerticalAlignment=Top`. Without that explicit alignment, the orb's
negative top margin was stretched inside the proxy and the rendered sphere moved down on open.

Focused lifecycle contracts cover persisted Classic→Glass→Classic switching, stable real-button
parentage/suppression, shared chrome identity, inert proxy content, top alignment, accessibility and
unchanged application-menu layering. A live 60-fps named-window capture verified clean Classic and
ordinary open/close animations. Frame analysis held the orb's yellow-glyph bounds at y=22…69 before
and after proxy takeover, confirming no vertical movement; ordinary Backstage showed no partial
Classic orb. Final gate: **339 logic tests plus one visual test covering 62 approved images**, with
zero build warnings or errors.

A follow-up first-use capture exposed two timing edges. On the first Classic open, the proxy's
`ContentPresenter` had not inflated the shared data template when rotation was requested, so
`OrbGlyph` was absent and only later opens animated. The request is now retained until the proxy's
first Loaded/layout pass, before first rendering, and cancelled on design/template teardown. Ordinary
Backstage also used to restore the real ribbon orb only in the close-completion callback. It now
restores that orb at close start beneath the departing Backstage surface, letting the slide reveal it
naturally; Classic alone keeps the real orb suppressed until its proxy completes the exit rotation.
Fresh-process 60-fps captures verified first-open rotation and both close timings.

### 3.95 Office 2010 Backstage begins below the tab headers — 2026-08-13

`Classic2010` now uses the Word 2010 File-surface placement that was originally deferred in §3.27.
The title bar and complete tab-header row remain visible; the Backstage begins at the live bottom of
that row and covers the ribbon body, below-ribbon QAT/message rows, and document. This is still the
window-level `BackstageAdorner`, not an in-ribbon host: the shared `RibbonTabControl` template exposes
`PART_TabHeaderHost`, and the adorner transforms that part's bottom edge into its adorned-root
coordinates on every relevant layout pass. Resizing, theme/DPI reflow, and tab-row QAT changes can
therefore move the boundary without a fixed theme-specific height. A custom template that omits the
part retains the established full-content overlay rather than failing open or clipping content.

The exposed chrome is functional rather than decorative. The real File button stays visible and
checked, title-bar QAT content remains available, and the Backstage's redundant round Back button is
collapsed only while the inset placement is active. Clicking File again, the selected ribbon tab, or
a different tab closes the surface; a different tab continues through normal selection. Hit testing
above the inset passes through the adorner to those controls. Other Backstage designs retain their
full-content placement, including both Office 2007 paths and the Classic2007 orb proxy. A remembered
`Translucent` preference does not hide the adorned root in this mode because doing so would also erase
the deliberately exposed title/tab band.

Three focused contracts pin the named template anchor, live inset/reflow and hit routing, plus the
placement-owned Back-button state. Live testing on the 125% per-monitor-v2 display confirmed the
Word-style boundary, visible File/tab strip, selected-tab and different-tab close paths, and the
unchanged Office 2010 page shell. The future Office 2010 Aero/frame treatment remains separate work;
this placement deliberately leaves the complete caption/tab band available for that material pass.
Current gate: **342 logic tests plus one visual test covering 62 approved images**, with zero build
warnings or errors.

### 3.96 Office 2010 Backstage tab-strip polish — 2026-08-13

The below-tabs `Classic2010` surface now completes the visual handoff at the exposed tab row. A
device-snapped three-DIP `Classic2010TopSeam` spans the Backstage top edge using the dedicated
`ApplicationButton.MenuOpenBottom` token. Office 2010 sets that token to the checked File gradient's
bottom stop, so the rule reads as a direct continuation of the button while still separating Home
(the first navigation item) from it. Every palette defines the same key, and custom accents derive it
beside `MenuOpenBackground`, preserving the shared-template/theme-token boundary.

The checked File trigger had still been painting the raw flat `Accent` brush even though Office 2010
already supplied a dimensional `ApplicationButton.MenuOpenBackground` gradient. It now consumes that
dedicated open-state token, retaining the established white Backstage foreground. This fixes the flat
open state without altering the short mouse-down `PressedBackground` state or the separate
application-menu-open shadow path.

While a real Backstage (not `RibbonApplicationMenu`) is open, the ribbon-tab template now suppresses
selected fill/border/connect-foot/indicator chrome and restores the normal tab-strip foreground. The
logical `RibbonTabControl.SelectedItem` is deliberately untouched, so the same selection returns on
close. Exposed tabs remain hoverable with the ordinary unselected hover treatment and retain the §3.95
close/select behavior. Both the tab-control host and every `RibbonTab` receive a read-only
`IsBackstageActive` state. Publishing it directly on each tab matters for joined ribbons: a merged
contextual tab did not reliably resolve the earlier ancestor binding in the live Showcase even though
the isolated snapshot did. Collection and merge changes now synchronize newly arriving tabs too.

The historical `Backstage.ContentShadow` remains available to older/full-overlay generations, but
`Classic2010` no longer applies it to the complete `ContentArea`. Instead a clipped 14-DIP host at
the pane divider casts only to the left through the dedicated `Backstage.ContentLeftShadow` token.
Office 2007/2010 palettes use a nine-DIP blur with the three-DIP projection and opacity raised to
0.48. The clipped host keeps that darker, softer edge off the top connector. The
navigation host also gains three DIPs of top padding, and content top padding grows by the same amount,
so neither the first selected row nor page content sits beneath the seam.

Focused contracts pin the seam geometry/color, checked File token, clipped left-only 2010 shadow,
padding, token parity, and host/per-tab Backstage ownership. Intentional shell updates and the new
`office2010-merged-backstage-tabs-100` scene were reviewed. Live testing on the 125% per-monitor-v2
display confirmed the thicker connected edge, clean top boundary, darker left divider depth, padded
first navigation row/content, open File depth, ordinary close/select paths, and a selected merged
`Chart Design` header becoming inactive while Backstage owns the row. Current gate: **347 logic tests
plus one visual test covering 63 approved images**, with zero build warnings or errors.

### 3.97 Office 2010 Aero-inspired frame prototype — 2026-08-20

The optional `RibbonWindowFrameAppearance` contract now includes `Office2010Aero` beside the
established 2007 value. It remains an authored presentation choice rather than a backdrop request:
the host still selects Acrylic independently, and an unavailable or disabled material uses the
tokenized opaque fallback. The restored left/right/bottom frame, title tint, reflection, caption
foreground/state and inactive fallback reuse the proven `RibbonWindow` frame composition with
Office-2010-specific palette values.

Unlike the 2007 treatment, the 2010 material continues through the complete live tab-header row.
The ordinary ribbon root becomes transparent only for this appearance; the groups body, document
surface and status area remain opaque. The tab row owns a deterministic Aero fallback layer which
fades to zero only after Acrylic is accepted. It and the title use the same fallback brush plus the
same live tint brush/intensity, so the caption and tab row form one surface in both opaque fallback
and Acrylic modes. Acrylic itself supplies the textured blur; no authored white readability box or
QAT-specific veil remains above it. File, ordinary/contextual tabs, both QAT placements and caption
controls therefore sit directly on the shared material.

The Showcase's Frame group now exposes mutually exclusive, generation-gated `2007 Aero` and
`2010 Aero` selectors. The existing tint/accent controls apply to either appearance, persistence
round-trips the new enum value, and changing themes leaves a mismatched preference dormant rather
than applying 2010 chrome to another generation. When 2010 Aero is active over Acrylic, the Showcase
keeps the document/status surface opaque so material remains confined to the authored frame,
caption and tab row. Focused template, token, persistence and Showcase contracts accompany the
change. Current automated gate: **350 logic tests plus one visual test covering 63 approved images**,
with zero build warnings or errors. Live Acrylic/fallback comparison and final visual tuning remain
the user-review gate for this prototype.

Early live passes tested a separate feathered white readability pane, first at full height and then
as a shorter blurred layer. That layer was removed after Acrylic proved to already provide the
required textured blur; retaining it only introduced an extra band and obscured material continuity.
The pale Office 2010 outer client edge was instead darkened directly.

Office 2010 light replaces the white outer client edge with a translucent blue-gray rule, but its
upper glass is now structurally continuous. The title-bottom rule is suppressed for `Office2010Aero`,
the frame's side bevel begins at 69 DIPs (the 34-DIP caption plus the live 35-DIP tab row) rather than below the 34-DIP
caption), and the tab row owns a tint layer bound to the same host `AeroFrameTint` and
`AeroFrameTintIntensity` as the title. Accepted Acrylic therefore removes both opaque fallback
layers while preserving one tint percentage across caption and tabs. The title-only reflection and
grain overlays are also suppressed for this appearance because their relative brushes restart at
the row boundary; Acrylic already supplies the texture continuously. Office 2007 and both
dark-generation edge treatments remain unchanged. Office 2010 tab and tab-strip-control hover fills
now carry alpha instead of replacing that shared material with opaque amber/blue rectangles. The
File tab's left margin is zero only while `Office2010Aero` is active and those two DIPs move into its
left padding, so the blue chrome reaches the ribbon body's left rule without shifting either the
label or right edge. The ordinary 2010 frame retains its original two-DIP left margin and narrower
padding, preventing the File tab from touching the non-Aero frame.
The Acrylic tab hover keeps its alpha-bearing amber fill and connector, but its one-DIP outline now
uses a dedicated fully opaque, stronger amber token instead of the paler shared control border.
Office 2010 Aero caption buttons follow the same separation: minimize/maximize/restore use neutral
translucent hover/press washes with opaque gray outlines, while Close uses translucent red with an
opaque red outline. Dedicated sibling state-chrome layers handle ordinary WPF mouse input. The
maximize/restore surface is different: advertising `HTMAXBUTTON` for Windows 11 Snap Layouts moves
its input into non-client messages, so `IsMouseOver` does not activate. The existing native bridge
now resolves the 2010 Aero fill and border resources explicitly and drives all three template-bound
properties; previously it recognized only 2007 Aero and therefore kept applying the ordinary 2010
blue gradient despite correct-looking XAML triggers.

Pixel inspection of the reported capture identified the protruding stroke as the frame inner-right
highlight, not the ribbon body's border: it began at the earlier assumed 68-DIP boundary, one row
above the actual 69-DIP body top. The temporary split/inset ribbon-right workaround was removed and
the original four-sided `ContentHost` border restored. The complete frame inner-right rule remains;
its top margin now matches the real caption-plus-tab-row boundary, so it joins the body at the top
and continues to the bottom frame without the extra row. Final live confirmation remains part of
the prototype's user-review gate.

### 3.98 Collapsed-group and Classic2007 follow-up fixes — 2026-08-20

The collapsed-group flyout now applies the same negative-four-DIP horizontal compensation already
used by the QAT overflow popup. Its visible border therefore begins at the collapsed button's left
edge instead of beginning four DIPs to the right because of the popup host's shadow margin. The
collapsed toggle's private template also gains the standard command disabled opacity (`0.4`). A
disabled group remains non-interactive rather than opening a flyout of disabled commands, but its
collapsed representation now communicates that state as clearly as the full group contents.

The Classic2007 orb proxy no longer snapshots `RibbonString.Back` when it is first created. Both its
tooltip and automation name bind to the shared live localization source, so replacing the provider
or calling `RibbonLocalization.Refresh()` updates an already-realized proxy. The Classic2007 frame
join is now visible only when the containing `RibbonWindow` actually uses `Office2007Aero`; ordinary
frames no longer receive the stray top rule. The Aero join also consumes the generation's existing
semi-transparent `WindowFrame.AeroInnerHighlight` brush instead of adding an opaque white edge, so
it reads as one continuation of the frame rule rather than a heavier second stroke.

The first implementation expressed that condition as an `AncestorType=RibbonWindow` binding in the
Backstage template. Live follow-up showed why that was incorrect: the Backstage lives in the window
adorner's separate branch, so the lookup returned no ancestor and left the rule collapsed even in
Aero mode. `Ribbon` now gives `Backstage` an explicit one-way binding to the real host window's
`FrameAppearance`; the private named frame part follows that state and the live Backstage design.
This also keeps programmatic frame changes current while the surface is already open.

Focused contracts cover popup compensation, disabled collapsed chrome, live proxy localization and
the explicit live Aero-frame binding. A fresh-process Showcase pass inspected the
collapsed/disabled group and both Classic2007 frame paths; the pre-test appearance preferences were
restored afterward.
Current gate: **355 logic tests plus one visual test covering 63 approved images**, with zero build
warnings or errors.

### 3.164 Crystal contextual tab study and compact button refinement — 2026-09-21

Crystal remains a Showcase-only Office 2024 overlay. Teal Picture Format and purple
Table Design tabs now derive glass surfaces, rims, readable darkened text and bubble
markers from their effective solid contextual color. Change tint exercises live updates;
Compare 2024 removes the adapter overrides. Gradient/custom context brushes fall back
to standard rendering. The adapter depends on the shared ContextualHeaderText part;
there is no copied tab template. Compact Clipboard/Text stacks scope a 3-DIP radius,
while large controls keep 8 DIP. Pressed buttons gain a deeper fill and visible lower rim.

API justification: the optional, null-default RibbonTab.ContextualSelectionBrush lets
consumers supply a material marker independently of contextual text color. Existing
callers retain contextual tint as the fallback. The marker now binds to both brushes,
so replacement and clearing update the selected tab immediately. Shipped API is intact;
the addition is recorded in PublicAPI.Unshipped.txt and XML documentation.

Release solution build: zero warnings/errors. Library: 400/400; existing visual test:
1/1. Focused Crystal tests additionally confirm mutable-brush updates and comparison
restoration (2/2). Writer full run: 465/472, seven failures including the recorded
WindowChrome cross-thread issue. Fresh-process follow-ups also failed on the expected
menu lacking Settings and a table-selection count (expected 2, actual 1). These remain
unresolved outside this change; no Writer code was edited. The user owns Crystal visual
checking, so no new live visual/DPI/RTL acceptance is claimed.

2026-09-22 File-panel follow-up: sample-only Modern Backstage palette and preview pages
reuse the accepted Crystal lighting and local tint variants. A top-level keyed
StaticResource alias resolved to the baseline fill in the realized-control test;
explicit DrawingBrush resources with nested geometry/brush references resolved the
intended Crystal fill. Keep the realized selection-fill assertion when changing these
resources. Showcase Release build and twelve focused Crystal/input tests passed;
Backstage live visual acceptance remains with the user.

The subsequent user-requested structural redesign uses a sample-only Backstage
template with a floating horizontal navigation island and separate content cards.
Compare 2024 clears this style to recover the shared layout. The overlay lives outside
the ordinary window namescope/resource route: an explicit scoped palette and binding
source are required for preview colors and document text to survive reparenting.
CrystalBackstagePresentation now owns that bridge. Thirteen focused tests passed,
including detached binding changes, minimum-width navigation, back requests and style
restoration. Live visual review remains with the user.

The floating layout is now optional. The default Glass sidebar preserves traditional
vertical navigation, bottom About placement and a large right-hand content area, with
Crystal surfaces and selection rims. The Appearance page switches layouts in place;
CrystalBackstagePresentation retains the chosen layout during palette replacement and
baseline comparison. Release Showcase build and eight focused Crystal tests passed,
including both navigation arrangements at the preview minimum width. Visual acceptance
remains with the user.

Crystal screen-tip scope note (2026-09-22): detached tooltips require their own palette
bridge. Once the palette is merged into ToolTip.Resources, its general ScreenTip.Border
outranks an implicit style's resource with that key. The preview's bridge therefore
publishes the dedicated glass rim directly in its own scope, removing it on baseline
comparison. A realized open-tooltip test covers tint replacement and restoration;
the focused Crystal/input filter passed sixteen tests. Visual acceptance remains with
the user.

Crystal option preview follow-up (2026-09-22): ElementName bindings from borrowed
group content to EnableOptionsToggle did not resolve after layout. Explicit binding
Source references fix both Options and Spacing panels. A realized CrystalPreviewWindow
test failed before the fix and passes for disable/re-enable afterwards; isolated
control tests alone had missed this preview wiring. The window now merges its icon
resources locally for standalone construction. Release Showcase build and twenty
focused Crystal/option-control tests passed; the user owns visual acceptance.

Crystal runtime token promotion (2026-09-24): the preview's 90 top-level brush,
drawing, effect and metric resources moved unchanged into the packaged
`Tokens.Crystal.Light.xaml` dictionary. It merges Office 2024 tokens for keys Crystal
does not override, while the Showcase dictionary retains its eight local control
styles. A tint variant must recolor the merged Crystal dictionary itself; recoloring
only the sample overlay silently leaves shared templates at the blue palette. The
sample still removes its entire overlay for Office 2024 comparison. This first slice
does not add a public theme enum or move host-owned blur and document underlay into
the control. Release Showcase and both runtime target builds passed without warnings;
the new standalone token test, focused tint and customization checks, and all four
fresh-process follow-ups passed. A grouped Crystal run passed 17/21 and hit the known
WindowChrome cross-thread cache failure in the other four. No new live visual or DPI
acceptance is claimed.
The user then inspected the preview and reported no perceptible difference on the
current display; this accepts the resource move, not the later main-window theme.

### 3.165 Crystal Light theme selection — 2026-09-24

`RibbonTheme.CrystalLight` appends public enum value 5, preserving the existing Office
values. `ThemeManager.Apply` and the design-only `Ribbon.DesignPreviewTheme` share one
mapping to `Tokens.Crystal.Light.xaml`; the net472 editor mirrors value 5 without a
runtime reference. Showcase offers Crystal Light in its main Theme group and saves
the choice separately from the Crystal preview window. It has no dark palette, so
Showcase hides the dark toggle while it is selected; a stored dark preference still
applies when returning to an Office theme.

When a custom accent is set, the shared semantic accent and selected-tab text follow
it. The Crystal reflective selected marker, checked wash and open File surface retain
their drawing brushes rather than flattening into solid colors. The preview's Glass
tint still rotates the full material separately. A focused runtime switch test covers
Crystal → Office 2024 → Crystal with accent and dark preference; a design preview
test covers the token URI and local scope, and Showcase preference serialization
round-trips the new value. Release solution build passed without warnings. Live
main-window appearance, designer interaction, DPI/RTL and Crystal snapshots remain
unverified.

### 3.166 Crystal main Showcase presentation — 2026-09-24

The user's side-by-side screenshot showed the normal Showcase retained a flat QAT
strip and square message bars after selecting Crystal Light. Shared token brushes
alone did not install the preview's host-owned presentation. The preview's implicit
control styles were split into a reusable Showcase dictionary; the main window merges
that dictionary and applies the existing QAT, message, utility, menu and popup
adapters only for Crystal Light. The preview still merges the same styles with its
window-scoped palette. Crystal selects the application menu as the default File
surface, while a separately saved explicit File choice is restored afterward.
Switching back removes the styles and adapters without changing Office resources.
Focused host switch and preview style checks passed; main-window visual acceptance
remains with the user.

### 3.167 Crystal Light first-phase gaps — 2026-09-24

The user's next screenshots exposed the remaining first-phase differences: a plain
contextual tab, merged chart tabs using their ordinary color treatment, Office 2024
scrollbars, an unstyled main options dialog, a split button without the Crystal
paired-hover treatment, ordinary Backstage, and an accent picker that changed only
semantic color. The main Showcase now uses the preview's contextual tab adapter for
its own and merged tabs, scopes the shared scrollbar style and Crystal Backstage
style to the Crystal selection's Modern Backstage, and applies the existing customization adapter to
the detached options dialog. Its accent picker now builds a fresh hue-rotated local
palette via `CrystalPalette.Create`; Office themes remove that window overlay.
The split button's idle companion takes the ordinary hover wash and its active half
receives a slightly stronger wash. A split example was added to the preview's Design
tab without changing its accepted Home layout. Focused main presentation, merge,
tint, Office restoration, and preview customization checks passed. Live visual
acceptance is still open. MDI, localization/RTL, modal tabs, and dark mode are phase
two by user direction.

Theme-contract follow-up: the stronger active split wash uses the shared
`RibbonKit.Brushes.Control.SplitActiveHover` token. Each Office base palette defines
it as Transparent (also inherited by its dark variant); only the Crystal host adapter
uses it, and that adapter removes its local override when Crystal is deselected.
The audit found that Crystal's other `RibbonKit.*` overrides already exist in every
Office base palette. `Crystal.*` resources remain scoped to the opt-in Showcase
adapters rather than becoming shared template tokens.

### 3.168 Crystal Backstage designs and shared split hover — 2026-09-27

The two accepted Crystal Backstage layouts are now distinct `Backstage.Design`
values, `CrystalSidebar` (5) and `CrystalFloating` (6). Their existing templates
move into a shared RibbonKit resource dictionary. The common Backstage style
selects either template and its matching navigation panel only for those explicit
values; the Office designs keep their existing templates. The main Showcase adds
both choices beside its Office layout choices and persists the selected value.
The preview uses the same design values and restores Modern during comparison.
The detached Backstage still receives its own palette scope.

The split-button helper is removed. Both halves' hover triggers now read
`RibbonKit.Brushes.Control.SplitActiveHover`; Crystal defines the stronger wash,
and every Office base palette defines the same brush as its ordinary hover.
The 2013/2019/2024 dark variants override it to match their dark hover brush;
the 2007/2010 dark variants inherit the unchanged base hover.
The new shared template behavior therefore preserves the Office appearance.
An attempted `StaticResource` alias resolved to a neighboring brush during WPF
resource loading, so the Office tokens duplicate their hover brush definitions.
Focused resource and layout checks cover switching between the two Crystal
designs, returning to Modern, preview comparison, and Office split parity.
Live appearance and mixed-monitor DPI remain for user review.

Screenshot follow-up: the first layout check supplied `Office2024.xaml` as a host
resource, which hid a detached Backstage lookup failure. In a real open adorner,
the Crystal template keys were unavailable through `DynamicResource`, so the
control kept its Classic template. `Controls.Backstage.xaml` now merges the
Crystal templates directly and its design triggers resolve them with
`StaticResource`. A focused detached-window test opens the actual Backstage,
checks both Crystal layouts, and checks the Modern return under Office 2024.
The Crystal split companion outline also uses a stronger border brush; its
color remains scoped to Crystal.

QAT height follow-up: a below-ribbon Crystal drawer measured 28.8 DIPs with a
plain small button and 30.4 DIPs after a dropdown joined it. The Crystal
palette now reserves 32 DIPs on the shared drawer template; Office palettes
retain a zero floor. A focused layout check adds both a dropdown
and split button without moving the drawer's lower edge. Office palettes keep
their existing row sizing.

Floating Backstage width follow-up: its inner grid had a 1040-DIP cap, leaving
large side gutters in a maximized window. The Crystal floating template now
uses the available width inside its 28-DIP side margins. A focused layout check
covers ordinary and 1600-DIP widths; the sidebar template is unchanged.

Shared-template audit follow-up: message-row margin and corner radius, and the
below-ribbon QAT minimum height, now come from shared template tokens instead
of Crystal code setting realized parts. Crystal retains its 2-DIP row spacing,
10-DIP corners, and 32-DIP QAT floor. Every Office base palette supplies zero
for these new metrics, including the effective dark palettes that inherit the
base values. The existing split-active hover brush matches ordinary hover in
all Office light/dark palettes; Crystal keeps its stronger active wash.

### 3.169 Crystal File hover rim in the shared template — 2026-09-27

The File button's existing `InnerRim` now uses separate thickness and idle-opacity
tokens, with a shared hover trigger that requires mouse-over without press or open
state. Crystal supplies the accepted 1-DIP glass rim and hides it at rest; its
`ApplicationButton.InnerGlow` drawing matches the tab hover rim. Office palettes
retain their previous inner-rim thickness and full idle opacity, including the
Office 2010 radial glow and all dark variants. The Showcase-only
`CrystalFileHover` part adapter and its call sites are removed. No public API was
added. A Release Showcase build and focused Crystal rim, preview, and main-window
switch checks passed. The Office 2010 File-rim contract checks passed; a separate
unchanged dropdown hover-consumer count check in that class still fails. Live
hover appearance was accepted by the user on 2026-09-27. The separate
`Office2010ThemeContractTests.Every_ribbon_button_family_consumes_the_shared_hover_glass`
case for `Controls.DropDowns.xaml` reports 3 consumers against its threshold of
5. The user deferred that investigation.

### 3.170 Minimized tab shape in the shared template — 2026-09-27

The Showcase-only `CrystalTabShape` helper previously bound
`Ribbon.IsMinimized` and overrode two tab metrics in the ribbon's resource
dictionary. The shared `RibbonTab` template now applies minimized corner
radius and border thickness from dedicated dynamic tokens to every header.
Crystal keeps its accepted top-only 8-DIP corners while expanded and rounds
all four corners at 8 DIPs when minimized; its reserved border changes from
`1,1,1,0` to `1`. The helper and its main-window and preview call sites are
removed. All five Office base palettes set their minimized metrics to their
existing tab metrics, and the dark palettes inherit the same geometry. Focused
checks cover Crystal state and comparison switches and Office light/dark token
parity. Live minimized-tab appearance remains for user review.

### 3.171 Crystal body-scroll geometry in the shared template — 2026-09-27

Body scroll arrows and tab scroll arrows share the left/right button templates.
The body instances now carry an internal `BodyScroll` marker, letting those
shared templates select `RibbonKit.Metrics.BodyScrollButtonCornerRadius` without
changing the tab arrows. Their width reads
`RibbonKit.Metrics.BodyScrollButtonWidth` directly from the ribbon template.
Crystal keeps the accepted 32-DIP width and 10-DIP corners. Every Office base
palette supplies the previous 22-DIP width and its existing control corner
radius; dark variants inherit those values. `CrystalUtilityChrome` no longer
sets or clears these geometric properties. It still owns the utility rim and
scroll-arrow surface switching; moving those across the several shared utility
templates is the next bounded pass. The focused preview interaction and Office
light/dark token checks passed. Live appearance after this migration remains
for user review.

### 3.172 Compact Showcase theme and Backstage choices — 2026-09-27

The main Showcase View tab now puts its six theme choices in a compact
`InRibbonGallery`; the separate Crystal preview action still opens its own
window. The seven Backstage designs are menu rows in one
`RibbonDropDownButton`, whose label shows the current design. Both selectors
are synchronized when appearance preferences are restored. Theme selection
continues through the existing `ApplyTheme` path, and Backstage selection
continues to persist the design and notify the File surface.

An initial `SelectedIndex` on the gallery fired `SelectionChanged` while
`MainWindow.InitializeComponent` was still constructing the window, before
the File-surface controls and application-menu field were ready. Initial
selection now happens after initialization under the synchronization guard.
A focused headless window test covers construction, theme selection, and
Backstage selection without touching saved preferences. The Release Showcase
build passed. Popup layout and live visual acceptance remain with the user;
no Showcase process was launched during this pass.

### 3.173 Crystal Acrylic surface transparency — 2026-09-27

The shared `RibbonTabControl` body now uses `RibbonKit.Brushes.Ribbon.BodyBackground`.
Every Office light and dark palette defines it with the former body paint;
Crystal also retains its original paint when Acrylic is inactive. Dropdowns and
gallery popups continue to use the opaque `ContentBackground` token, so the
new body translucency cannot wash out detached surfaces.

When the Showcase actually activates Acrylic under Crystal Light, its scoped
palette softens the ribbon body, selected tab fill and marker, title bar, and
window/status background. Turning Acrylic off, changing the tint, or switching
themes rebuilds or removes that palette; no public API or Office paint changes.
The focused Crystal presentation test covers token parity across all Office
light and dark palettes, the realized body brush, translucent overrides, and
restoration. Live Acrylic appearance remains a visual review gate.

Acrylic hover follow-up: the accepted blue command hover wash blends into the
more saturated material in the user's screenshot. The Showcase's Acrylic-only
Crystal palette now replaces that shared hover token with a brighter, neutral
glass wash; the existing rim and non-Acrylic Crystal brush remain unchanged.
The focused presentation test confirms the Acrylic value and restoration after
turning Acrylic off. The new hover contrast still needs live visual acceptance.

### 3.174 Optional Acrylic glass treatment across Showcase themes — 2026-09-27

The later screenshot clarified that the faint hover was the tab header marker;
the command-button hover from §3.173 was already accepted. Its exact Acrylic
wash is retained. A brighter tab hover fill and rim, plus matching companion
and active-half split-button washes, now share the same window-scoped
`AcrylicGlassPresentation` as the ribbon-body, selected-tab, title-bar, and
status-bar opacity changes. The shared control templates still resolve theme
tokens; no RibbonKit public API or theme baseline changed.

The Showcase View tab has a separate Glass look switch. Existing preferences
default to the current behavior: Crystal applies it with Acrylic, while Office
themes retain their prior Acrylic appearance. The switch may override that
default for any theme and persists separately from the DWM backdrop choice.
The overlay is rebuilt after theme, tint, dark-mode, and backdrop changes, and
is removed whenever Acrylic is inactive. Focused presentation, selector, and
preference checks cover palette restoration, Office opt-in, and persistence;
live contrast and width/overflow remain visual review gates.

Tab-hover contrast follow-up: the first glass hover wash was too close to the
selected tab in the user's screenshot. Its light and dark alpha and hover-rim
opacity were reduced; the selected marker, command-button hover, and split
hover values remain as accepted in the preceding pass. The focused presentation
check covers both light and dark hover tokens. Live appearance still needs a
new screenshot.

Split-button and switch follow-up: vertically stacked split halves used to
paint both edges at their join. The shared template now reads a border-thickness
token for the primary half. Crystal omits its bottom edge, so the lower half
draws the divider once; all Office light and dark palettes retain their existing
one-pixel border. The Showcase Glass look switch now uses a large icon and
displays its label. Focused checks and WPF renders in both glass states covered
the controls; native Acrylic compositing still needs live visual review.

### 3.175 Crystal translucent Backstage option — 2026-09-27

The Showcase's Transparent Backstage switch already sets `Backstage.Translucent`,
and the Ribbon hides content behind the open surface so DWM Acrylic can show.
Neither Crystal layout template responded to that property: each kept its
window-colored root and opaque navigation surface. Both shared Crystal templates
now clear their root fill when translucent and use the existing translucent
navigation brush token. Crystal supplies a tinted, alpha-bearing value for that
token; Office palettes keep their previous values. The content card stays opaque
for text readability. Turning the option off restores the original Crystal
brushes without changing the chosen layout or adding a public API. A focused
realized-template test checks both Crystal layouts and the on/off transition;
native Acrylic appearance remains for live screenshot review.

### 3.176 Acrylic hover accent and File wash — 2026-09-27

The Glass look overlay's bright command hover had a fixed near-white color, so
it stayed neutral when the user selected another accent. The overlay now blends
the active accent into that wash while retaining its previous light/dark alpha.
The split-button companion and stronger active-half washes use the same tint.
The File button's hover fill now resolves to the same brighter glass brush as a
tab header; its selected and pressed surfaces are unchanged. These remain
Showcase-scoped overrides, removed when Glass look is off. A focused Crystal
presentation test checks accent changes, File/tab resource parity, Office opt-in,
dark alpha, and restoration. Live Acrylic color balance awaits screenshot review.

### 3.177 Crystal selected-tab bridge — 2026-09-27

The user's Acrylic screenshot showed the selected Crystal tab ending above the
tab strip's last pixel and the ribbon body's white top border. The shared tab
foot and body-side `PART_ConnectNotch` already bridge these two pixels for
connected Office themes, but Crystal inherited Office 2024's disabled tokens.
Crystal now supplies the one-pixel foot transform and visible selected-foot and
notch brushes. A follow-up screenshot showed a white strip when Glass look was
off: the opaque notch token used a different color from the foot, while the
Glass look overlay matched them. Both Crystal tokens now use the same
accent-tinted color, so the bridge stays consistent as Glass look changes
without a Showcase-only notch override or layout adjustment. Office 2024 still
has no notch. A focused realized-control test checks Crystal geometry, foot/notch
color in both glass states, tint changes, and Office 2024 comparison; live
ordinary and Acrylic appearance, including minimized tabs, still need review.

### 3.178 Crystal hover tab closes below the header — 2026-09-27

The unselected Crystal tab inherited Office 2024's top-only hover geometry. A
separate, hit-transparent `HoverChrome` now draws the hover outline in the shared
tab template. Crystal supplies full 8-DIP corners and a complete one-pixel border;
its unselected hover hides the connecting foot. The selected tab continues to use
the existing foot and body notch. The normal and Backstage-active hover triggers
use the same overlay, and Office light/dark palettes retain their original hover
corner, border and foot values.

Changing the measured `HeaderChrome` border on hover shifted the ribbon body by
one pixel, so the outline was moved to a visual overlay. A focused WPF check
confirms the overlay's geometry, unchanged tab/ribbon desired sizes when it is
shown, and Office token parity across light and dark variants. Live hover shape
and body position still need screenshot review. The reported 200% icon-bearing
gallery selection clipping is recorded separately in the future themes plan.

### 3.179 Crystal hover and selected tabs share an open lower edge — 2026-09-27

The closed hover pill in §3.178 looked inconsistent when a tab became selected:
Office 2007/2010 avoid that shape change by using top-rounded, open-bottom chrome
and a strip-side hover foot. Crystal now uses the same geometry with its existing
8-DIP upper corners. Its hover foot has the same translucent paint as the hover
fill, including the Showcase's optional Glass look overlay and tint changes.
The selected body-side notch remains reserved for the selected tab. The
separate `HoverChrome` layer and its three hover metrics from §3.178 were
removed: the original measured `HeaderChrome` now changes only brushes on
hover. Its existing minimized trigger still supplies the accepted full-corner
shape. A focused geometry check confirms the brush swap leaves tab and ribbon
measurements unchanged, and presentation checks cover the Glass look foot tint.
Live pointer appearance remains for user review.

### 3.180 Crystal hover foot clipping — 2026-09-27

The follow-up screenshot showed the open-bottom hover tab's side outline ending
before the ribbon body. A focused realized-control check placed `ConnectFoot` at
37 DIP while `PART_TabScroll` clipped at 36.8 DIP: its one-pixel border was fully
outside the strip. The shared template now reads a separate render-only hover
transform. Crystal keeps its hover foot at Y=0, inside the strip, while its
selected foot remains at Y=1 for the existing body notch. Every Office light
and dark palette keeps the same hover displacement it had before. The focused
geometry check verifies the hover trigger, foot/strip intersection, token
parity, and unchanged tab/ribbon measure. Live pointer and DPI appearance
remain for user review.

### 3.181 Crystal hover uses one translucent surface — 2026-09-27

The visible hover foot from §3.180 still crossed a fractional strip edge at some
scales and painted the translucent hover wash twice, making a darker band. A
realized-control check shows the ordinary `HeaderChrome` already extends to the
strip's clip edge. Crystal now suppresses the hover foot's fill and side borders
with a shared opacity token; Office light and dark themes retain opacity 1.
The hover stays top-rounded and open-bottom, while the selected tab keeps its
existing opaque foot and body notch. The separate hover transform and its
Glass look foot override were removed. A focused check covers the header/strip
geometry, token parity, selected foot restoration, and unchanged tab/ribbon
measure; live pointer appearance remains for user review.

### 3.182 Crystal hover rounding and accent-tinted QAT glass — 2026-09-27

The follow-up screenshot still showed an awkward lower edge on the open-bottom
hover tab. The shared `RibbonTab` template now gives its existing measured
`HeaderChrome` a hover corner-radius token. Crystal uses an 8-DIP radius on all
four corners; selected tabs restore their connected upper-only shape. Office
light and dark palettes keep their previous hover radius, and minimized tabs
retain their existing radius trigger. This does not add a second visual layer
or change border thickness. The translucent hover foot remains suppressed for
Crystal, avoiding overlapping wash at fractional DPI; the live lower-edge
appearance still needs screenshot review.

The below-ribbon QAT had reused `Tab.HoverBackground`, which Glass look changes
to a neutral quiet hover wash. Its shared template now reads
`QatExtender.Background` instead. Crystal's normal token matches its former
hover brush and rotates with the accent; optional Glass look gives the QAT an
accent-tinted translucent brush independently of tab hover. Every Office light
and dark palette supplies the exact previous QAT paint, including the 2007 and
2010 gradients. Focused tests verified tab measure, Crystal tint and Glass
switching, and Office token parity; live visual acceptance remains open.

### 3.183 Glass tab hover follows the accent — 2026-09-27

The accepted rounded hover still looked blue against a warm accent because
optional Glass look replaced `Tab.HoverBackground` with a fixed blue-white
wash. It now uses the same accent-aware color recipe as the QAT while retaining
the quieter tab-hover alpha (0x50 light, 0x30 dark). The File button shares the
tab hover token, so it tracks the accent as well. The independent QAT token
remains available for later tuning. Focused presentation checks cover purple,
green, red, and dark glass colors; live color acceptance remains user-owned.
