# Flat theme variants

Historical implementation records. Current acceptance belongs in the linked active plan.

### 3.254 Office 2024 Flat theme — 2026-10-12

The user authorized a separate Office 2024 derivative with stronger borders at the
existing thickness, replacing shadow separation. Normal commands, collapsed groups
and launchers outline only pressed/active states; dropdown/split buttons additionally
outline hover. Combo box input and item-row presentation retain Office 2024 styling.

`Office2024Flat` is appended as enum value 6, preserving existing serialized enum
values and the shipped API baseline. Its XML documentation and Unshipped baseline
record the compatible addition. A separate theme value permits normal application
selection, preference persistence, scoped palettes and designer preview; it does
not introduce another template family or a new host integration API. Accent and
backdrop handling follows the Office 2024 branch.

Light/dark token dictionaries inherit their Office 2024 counterparts. Surface
borders use stronger neutral brushes; the one-DIP state metric is reserved so
native button hover/press does not change measurement. Shared popup effects,
dropdown hover borders, colored QAT state borders, and combo/gallery popup borders
have defaults preserving other themes. Effect resources retain their existing
types with zero opacity/blur/depth in the flat palette. Collapsed/launcher pressed
setters fill earlier gaps; combo item border metrics are separate so the flat
command outlines do not change combo row styling. Real resource entries are used,
avoiding the earlier unusable dictionary-alias markup pitfall (§3.195).

The RibbonKit-only consumer verifies eight light/dark, Compact/Touch and LTR/RTL
combinations, surface contrast, one-DIP geometry, command states, popup effects,
local/scoped resource precedence, accent changes and restoration to Office 2024.
Eighteen new deterministic visual scenes cover standard 100/125/150/200% rendering,
state samples and RTL at 200%, lower QAT and application menus. Each capture was
inspected before adding its new approval. The ordinary-button hover paint fixture
sets its wash explicitly because setting WPF's read-only mouse DP cannot synthesize
its native mouse flags; this is paint evidence, not live mouse-input acceptance.

Normal Debug Showcase and Release solution builds passed with zero warnings/errors.
The final focused Debug runtime slice passed 38 checks; the independent consumer
passed its eight flat-state combinations and scoped/restoration checks. All 23
selected visual comparisons passed: the 18 new flat approvals and five unchanged
Office/Crystal approvals. The shared rounded-menu capture fixture now initializes
native rendering before disconnected layout; inspection of actual/diff images
identified its earlier focused Crystal mismatch as startup geometry/text drift,
and the existing approval passed after this fixture correction.

The initial concurrent full Release attempt was not clean. The new enum exposed
count/inheritance/radius assumptions, corrected in the applicable checks; three
older simple-trigger assertions predate this branch and remain for final-stage
review. The full visual harness timed out and Writer failed a native popup-reopen
check during that concurrent run. The user explicitly deferred further full runs
until appearance review. Do not substitute the focused results for a full pass.

Final full-suite evidence and native acceptance are maintained in the
[flat theme trial](../../09-FUTURE-THEMES-PLAN.md#office-2024-flat-trial).

Light-border refinement, 2026-10-12: user review found the initial light outline
too strong. Surface and dropdown-hover borders moved from `#757575` to
`#A6A6A6`, with pressed/checked outlines from `#616161` to `#8A8A8A`. Dark
paint and one-DIP geometry are retained. The light consumer assertion now checks
stronger separation than regular Office 2024 rather than imposing the prototype's
3:1 surface-border threshold; the dark threshold remains. Inspection of all nine
light actual/diff captures showed border-only paint changes. Nine light approvals
were updated; all 18 flat visual comparisons and the independent consumer passed.
The normal Debug Showcase rebuild passed with zero warnings/errors. Full-suite
validation remains deferred until visual review, by the user's instruction.

Popup hierarchy, 2026-10-12: the user retained stronger popup outlines to distinguish
overlapping surfaces. Light popup and ScreenTip frames use `#757575`; stationary
ribbon/QAT outlines and dropdown-hover borders remain `#A6A6A6`, and pressed/checked
commands `#8A8A8A`. `Popup.Border` separates shared dropdown/split, collapsed-group,
QAT overflow and native context/submenu frames from `ScreenTip.Border`, which is
also consumed by action buttons and customization panels. Defaults in all authored
palettes retain prior popup paint; flat dark reasserts its existing `#929292` border.
Combo/gallery popup and File-menu resources follow the same hierarchy.

The independent consumer checks stronger light-popup separation than the ribbon
surface and verifies the realized native dropdown frame. Actual/diff inspection
showed only the intended File-menu outline change before updating its one light
approval. All 23 selected visual comparisons and the focused consumer passed; the
normal Debug Showcase build passed with zero warnings/errors. Full validation
remains at the user's deferred final stage.

File-surface framing, 2026-10-12: theme-controlled, noninteractive overlays add a
one-DIP perimeter and navigation/page divider to the existing Modern/2024 Rail
Backstage. The shared `Backstage.Modern.Border` and `ModernBorderThickness` keys
have transparent/zero defaults in authored palettes; Flat sets light/dark framing
and reasserts the metric after the inherited dark dictionary. Divider width follows
the perimeter thickness, and WPF mirrors its existing grid/alignment in RTL.
Overlay borders do not inset content, reparent controls or create a new layout/API.

Flat application-menu `InnerOutline`, `PaneBorder` and `Separator` brushes now
frame the content and active-page regions more clearly, retaining the stronger
popup perimeter and existing row/clip/scroll geometry. No menu template copy was
needed. The independent consumer passed eight palette/density/direction cases,
checking passive framing, the divider's page-facing edge, menu resource resolution
and unchanged page size after Office restoration. Local Backstage coordinates
are themselves mirrored in RTL, so geometry checks compare the page-facing edge
rather than assuming local bounds are physical screen coordinates.

All eight new/changed captures were inspected before six new approvals and two
menu approval updates. All 28 selected visual comparisons passed, including the
standard Office 2024/2010 Backstage and Crystal menus without approval changes.
Normal Debug Showcase built with zero warnings/errors. Native appearance and
final full-suite validation remain in the user's review stage.

Navigation outline refinement, 2026-10-12: Flat application-menu row state outlines
now use square corners to match their square fills. The dedicated
`ApplicationMenuItemOutlineCornerRadius` metric preserves the former control
radius in all authored palettes; both Flat variants override it to zero. Normal
button and popup-frame rounding retain their existing metrics. The independent
consumer verifies pressed-row shape agreement in all eight combinations and
restoration to Office 2024's outline radius. Two new pressed-menu captures were
inspected before adding approvals; existing split captures were byte-identical.
All nine selected menu comparisons passed, including unchanged Office 2024 and
Crystal scenes. Normal Debug Showcase built with zero warnings/errors.

Rail/title-bar join, 2026-10-12: the Flat Modern perimeter metric is now
`1,0,1,1`, leaving the navigation rail open at the top. A passive top-edge overlay
in the page column derives its height from the perimeter's side thickness, keeping
the page framed without a new metric, spacing change or layout. Existing authored
zero-thickness defaults keep this overlay hidden. The independent consumer passed
eight palette/density/direction cases, checking the page edge's size, alignment and
restoration. Four actual/diff captures were inspected before updating approvals;
all six selected Backstage comparisons passed, including unchanged Office 2024
and Office 2010 scenes. Normal Debug Showcase built with zero warnings/errors.

Hover-border concept, 2026-10-12: Flat neutral hover borders now match their
hover fills (`#E6E6E6` light, `#333333` dark) at the reserved one-DIP width.
The dedicated launcher-hover brush has transparent defaults in every authored
palette, preserving legacy launcher paint. Split companions use the dropdown
hover border and a transparent fill. Existing companion tags route either half's
hover/open/press state; colored QAT companions resolve the corresponding colored
outline after the neutralizing QAT trigger. No new input handling is needed.

A passive `CheckedHoverRim` above the toggle's hover wash keeps its checked outline
visible when the hover-colored border would otherwise cover it. Its thickness
defaults to zero in authored palettes and is one only in Flat; press paint remains
above it. The consumer passed eight palette/density/direction cases, both hover
halves in horizontal/vertical layouts, colored QAT and unchanged dimensions.
Six existing split-hover runtime cases passed. Four expanded state captures and
two menu-split captures were inspected before approval updates; two new captures
show all four horizontal/vertical primary/arrow hover combinations. All 16 selected
visual comparisons passed, including unchanged Office/Crystal scenes. Normal Debug
Showcase built with zero warnings/errors. Native review and full validation remain
deferred as recorded in the active plan.

Soft body shadow, 2026-10-12: user review retained a faint lift behind the main
ribbon and its connected surfaces. Flat `ContentShadow` and `QatExtenderShadow`
now use a 12-DIP blur, 1-DIP downward depth and 0.10 opacity in both modes, compared
with regular 2024's 12/2/0.16. The existing message-bar effect consumes
`ContentShadow`; lower-QAT clipping and connected geometry are inherited unchanged.
No shadow host, new token or layout was needed. Floating-menu and Backstage effects
retain their previous zero-opacity overrides.

The independent consumer passed eight palette/density/direction combinations,
verifying the realized body, lower-QAT and message shadows are present and weaker
than regular 2024. All 26 changed/new captures and their current diff diagnostics
were inspected before updating 24 approvals and adding two connected-message
approvals. All 34 selected comparisons passed: 30 Flat scenes and four unchanged
Office/Crystal references. Normal Debug Showcase built with zero warnings/errors.
Native appearance review and final full validation remain in the active plan.

Backstage selection refinement, 2026-10-12: by user direction, the trial Modern
perimeter, page-top edge and navigation divider were removed from the shared
template, along with the unused `Modern.Border` and `ModernBorderThickness`
resources. Removing the Flat Backstage shadow overrides restores the inherited
Office 2024 page and historical left-shadow effects.

`Backstage.Modern.ItemSelectedBorder` replaces the perimeter brush, with transparent
defaults in all authored palettes and Flat active-state colors `#8A8A8A` light,
`#B0B0B0` dark. Only the Modern selected-item trigger sets the brush. Existing
Chrome already reserves one DIP and has the fill's four-DIP corner radius, so no
new overlay, inset or sizing change is needed. The independent consumer passed
eight palette/density/direction cases, including selection transfer, unchanged
row sizes, restored page-shadow parameters and neutral Office restoration.
Four actual/diff captures were inspected before approval updates; all six selected
Backstage comparisons passed, including unchanged Office 2024/2010 references.
Normal Debug Showcase built with zero warnings/errors. Native appearance and final
full-suite validation remain in the active plan.

Initial frame interpretation, 2026-10-12: the softer one-DIP Backstage frame was
applied to the whole surface, with four-DIP corners when title-bar coloring is off.
The user's subsequent clarification corrected its target to the content page below.
Flat frame brushes are
`#D1D1D1` light and `#555555` dark. When the existing title-color state is on, the
top edge opens and both upper corners become square; sides/bottom retain framing
and lower rounding. A passive `ModernBorderFrame` uses paired frame profiles from
tokens and the existing internal `QatTitleBarColored` state, requiring no new public
API, input coordinator or Showcase helper. The soft shadow and active-item border
remain; the rail/page boundary has no hard divider.

`BackstageFrameGeometryConverter` reuses the shared rounded-rectangle geometry to
clip the root fill to the frame. It returns null when the frame is collapsed or has
zero thickness, preserving unframed authored layouts and their shadow overflow.
All authored palettes have transparent/zero profile defaults. The independent
consumer passed eight palette/density/direction cases, toggling coloring with
backdrop/translucent states while checking shape changes and unchanged page size.
Classic layout and Office restoration retain a null clip.

Four actual/diff captures and four new captures of the real RibbonWindow caption
with coloring off/on were inspected before approval updates/additions. All ten
selected Backstage comparisons passed, including unchanged Office 2024/2010
references. Normal Debug Showcase built with zero warnings/errors. User appearance,
native mixed-monitor/motion/designer and full-suite gates remain in the active plan.

Content-page frame correction, 2026-10-12: the Task Manager reference identifies
the page beside the navigation rail, with only its top/leading edges framed and
their meeting corner rounded. `ModernPageFrame` now overlays that page with
thickness `1,1,0,0` and radius `4,0,0,0`; WPF mirrors both in RTL. The existing
title-color state switches only the corner to square. Quiet colors remain
`#D1D1D1` light / `#555555` dark. Page-specific tokens have transparent/zero defaults
in all authored palettes; the redundant connected-thickness profile was removed.

`BackstagePageGeometryConverter` clips only the page fill to the rim. The rail
material paints behind its corner, including translucent mode. The soft shadow
lives on the unclipped `ContentAreaHost`, so rounding does not cut off its spill.
Neither the rail nor outer root is framed or clipped; selected-item outlines and
page padding stay unchanged. No new layout, public API or Showcase helper is needed.

The independent consumer passed eight palette/density/direction combinations,
including exact page-frame bounds, live title-color/backdrop/translucent changes,
unchanged page size, unclipped shadow host, Classic layout and Office restoration.
All eight new actual/diff captures were inspected before approval updates. Ten
selected visual comparisons passed, including unchanged Office 2024/2010 references.
Normal Debug Showcase built with zero warnings/errors. Live appearance and the
user-deferred full validation remain open in the active plan.

Title-color border/shadow switch, 2026-10-12: after confirming the content-page
placement, the user requested exclusive treatments. An uncolored title bar uses
the rounded top/leading rim with no page shadow; a colored title bar uses a square
page with the original soft shadow and zero frame thickness. The existing clip
converter returns null for the zero-thickness profile, removing the corner cutout.
`ModernPageShadow` defaults to each authored palette's existing page effect; Flat
overrides it with zero opacity/blur/depth. `ModernConnectedPageFrameThickness`
defaults to zero. The colored Modern trigger restores `Backstage.ContentShadow`.
Other layouts keep their original effects and the selected-nav outline remains.

Everything resides in shared RibbonKit templates and paired palette tokens. Any
consumer can apply `Office2024Flat` and use `ThemeManager.SetAccentedTitleBar`;
there is no Showcase-specific style, helper or new public API. The independent
consumer passed eight palette/density/direction cases, repeated live switching
with backdrop/translucent combinations, unchanged page size and Office restoration.
Eight captures and five fresh diffs were inspected before approval updates; the
three below-tolerance dark shadow changes produced no new diff diagnostics.
Ten selected visual comparisons passed, including unchanged Office 2024/2010
references. Normal Debug Showcase built with zero warnings/errors. Current live
acceptance and the user-deferred full validation remain in the active plan.

Single split separator, 2026-10-12: the user identified both subbutton borders
meeting at the join, creating a two-DIP line in Office 2024 Flat, Office 2010 and
Office 2007. Their primary rims now omit the joining edge (`1,1,0,1` horizontal,
`1,1,1,0` vertical), leaving the dropdown rim to paint the separator. Matching
padding replaces the removed border's layout reservation, preserving native
content and companion-glow positions rather than moving the icon or caption.
WPF mirrors the same logical edge in RTL. Hover, pressed and open state brushes
keep their existing rules. Crystal's existing metrics and paint stay unchanged.

The shared primary template uses `SplitHorizontalPrimaryBorderThickness`,
`SplitHorizontalPrimaryPadding` and `SplitVerticalPrimaryPadding`; all palettes
define matching keys with their previous geometry as defaults. The existing
`SplitVerticalPrimaryBorderThickness` is refined only for the requested themes.
No runtime/public API or Showcase-specific patch is needed; QAT copies use the
same template. The historical legacy vertical-metric expectation was updated.

Seven focused runtime regressions passed. The independent SplitButtonDensity
consumer passed its full named scope across every theme, light/dark, both densities,
LTR/RTL, 125/200% and QAT placements/copies, checking one-DIP joins and preserved
content insets for the three themes. Flat2024's separate consumer scope passed
hover in both halves, press, colored-QAT and unchanged size checks. Sixteen
captures and two fresh diffs were inspected before updating six approvals and
adding ten legacy/RTL split-hover approvals. All 24 selected visual comparisons
passed, including byte-identical Office/Crystal reference captures. Normal Debug
Showcase built with zero warnings/errors; user appearance and deferred full gates
remain in the active plan.
