# RibbonKit touch density

> QAT/message corners and Touch modal geometry corrected with focused verification, 2026-10-08; visual review is pending.
> The full Release checkpoint predates the later geometry and animation follow-ups; visual/native acceptance remains separate.

## Contract and API review

Add `RibbonDensity` (`Compact = 0`, `Touch = 1`) and an inherited attached
`Ribbon.Density` dependency property, including the normal ribbon CLR property
and `GetDensity`/`SetDensity` accessors. Compact remains the default. This additive
API belongs in `PublicAPI.Unshipped.txt`; the shipped baseline is unchanged.

The transition adds `RibbonAnimationAction.DensityChange = 15` after `MessageBar`,
without renumbering any existing action. This is an intentional compatible addition
to the unshipped baseline: consumers need independent control over density motion
through the existing per-action API. Its XML documentation and duration token use
the same global level and reduced-motion policy as the other actions.

Density describes geometry, independently of theme, accent, dark mode, DPI,
command size and customization layout. An attached property also lets a consumer
opt a detached menu or a standalone ribbon control into the same geometry.
Local density and dimension overrides retain ordinary WPF precedence. Clearing
a local density resumes inheritance. Applications own the selector and preference
persistence; density is excluded from ribbon customization serialization.

## Implementation scope

1. Shared controls/templates: buttons, toggles, both split-button hit areas,
   dropdowns, check/radio options, text/combo inputs and combo item containers.
   Touch uses 44-DIP command targets, visible spacing and larger vector icons.
2. Ribbon surfaces: tab/File/utility buttons, stacked group space, dialog
   launchers, group flyouts and minimized ribbon content. Refresh cached adaptive
   widths and selection chrome when density changes. The dialog launcher retains
   its normal 14-DIP height and gains only Touch width, keeping caption bands aligned.
3. Quick Access Toolbar: all three placements, overflow, customization opener
   and the native QAT context menu. Explicitly connect the detached title-bar
   toolbar to its owning ribbon's density.
4. Galleries: keep the tile viewport and selection behavior; show a single
   popup opener beside the tiles in touch mode. Enable native vertical
   panning in gallery and menu scroll viewers.
5. File surfaces: application-menu rows, split arrows and footer actions;
   shared Backstage navigation/back/action styles, including Crystal layouts.
   Application-authored document/content controls remain host-owned.
   Message rows enlarge their action and dismiss targets; a title-bar QAT keeps
   the shared window caption at touch height while Backstage hides that QAT.
6. Showcase: a View-tab touch-mode selector, using the library setting. No
   Showcase-only geometry or behavior. Writer integration and automatic device
   detection are outside this slice.

Use one template set and matching `RibbonKit.Metrics.Touch.*` keys in every base
theme. Dark color overlays inherit their base theme's geometry. Preserve compact
metrics and scoped resources except for expressly requested shared visual changes:
gallery spacing and flat Office2010 QAT paint now apply in both densities. Popup
roots explicitly receive their owning control's density. No global input hooks
or automatic mode switching.

## Verification and acceptance

### Live density transition — 2026-10-07

The final density is applied before any motion. A coalesced dispatcher operation
runs after inherited callbacks, binding/layout, selection/gallery viewport refresh
and deferred QAT placement. It completes the window/adaptive layout and selection
chrome before starting an 85%-to-rest opacity settle. Actual ribbon/body height,
overflow-arrow availability, caption targets and Backstage sizing therefore follow
the final density throughout the transition. Text/icons keep their final unscaled
geometry; no Width, Height, Margin or LayoutTransform is animated.

Subtle uses 160 ms; Expressive follows the existing 1.4 duration multiplier
(224 ms). Both use a frozen cubic EaseInOut curve so the early opacity dip stays
visible while the eye follows the geometry change. The expanded below-ribbon QAT gets
a four-DIP settle (7.2 DIP in Expressive), upward from the final Touch position or
downward from the final Compact position. Tab-row and title-bar QATs only fade.
Below-ribbon glide is suppressed while a flyout/context menu is anchored there or
the ribbon is minimized. This keeps popup placement and the existing minimize
glide intact. Density motion lives entirely in RibbonKit; Showcase uses its
existing selector and Motion controls.

Motion becomes eligible after the first render, so density set before load or
restored during `Loaded` appears immediately. New toggles cancel the pending/current
transition; a generation guard also handles density/template changes during layout.
Completion, unload, template replacement, QAT moves, minimize and disabled motion
release opacity clocks and the private translation. Consumer opacity/transform
bindings and prior render transforms survive. Loaded controls observe both animation
configuration changes and the system client-area motion setting, unsubscribing on
unload. The shipped API and density template/token geometry remain unchanged.

The focused RibbonKit-only transition consumer covers all six themes, every QAT
placement, LTR/RTL, pre-load and Loaded restoration, Subtle/Expressive completion,
rapid queued/running reversals, global/per-action None, reduced-motion policy,
bindings, template replacement, running/pending unload, popup anchors and minimize
interruption. Its live-motion cases also cover flyout reclamation, collapse states,
immediate overflow arrows, gallery selected-tile visibility, message targets and
title-bar caption geometry. Set `RIBBONKIT_PORTABILITY_SCOPE=DensityTransition`
for this focused process; the default consumer also includes it. The existing
`Touch` scope continues to cover the broader density geometry checks without native
keyboard/navigation checks.

Normal Release solution build passed for both runtime targets with zero warnings
or errors. The 33 selected runtime checks passed in combined evidence: 32 passed
on the first run, and the sole static QAT-shadow comparison passed on its focused
retry after the fixture disabled only `DensityChange` and restored the prior
override. Its original pixel tolerance and product shadow geometry are unchanged;
comparing two changing opacity frames was inappropriate for a static paint gate.
These results are `density-animation-runtime-release.trx` and
`density-animation-shadow-release.trx` under `artifacts/touch-validation`.

The independent transition aggregate passed in Debug and Release; the Release
run took 27 seconds and the broader touch-only consumer also passed in Release
(33 seconds). Results are `density-transition-debug.trx`,
`density-transition-release.trx` and `density-animation-touch-release.trx` in the
same directory. Office2024 Compact/rest, Touch/settling and Touch/rest consumer
renders were inspected under `artifacts/density-transition-diagnostics`.
Normal Debug Showcase output is refreshed. The full suite, automated visual
aggregate and native keyboard/input checks remain deferred until visual review;
the historical 974-test checkpoint does not validate this animation. Live timing,
native reduced-motion changes and mixed-monitor/DPI acceptance remain separate.

The recording reviewed on 2026-10-08 showed that the initial 90% dip with fast
ease-out left almost all visible motion in the QAT. The agreed refinement uses
85% opacity and frozen cubic EaseInOut at both levels, preserving the 160/224-ms
timing and final geometry. Both runtime targets build cleanly in Release; the
focused policy/easing test and RibbonKit-only transition aggregate passed again
(36 seconds for the consumer). Results are `density-animation-refinement-release.trx`
and `density-transition-refinement-release.trx`. The revised consumer check observes
the stronger early dip as well as completion, interruption, disabled policy,
startup and the existing adaptive/QAT regressions. A settling render was inspected
under `artifacts/density-animation-refinement-diagnostics`; normal Debug Showcase
output is refreshed. The earlier broad evidence above predates this refinement.

### Group launcher caption height — 2026-10-08

The Touch trigger no longer overrides the dialog launcher's normal 14-DIP height.
Its width and mirrored caption spacer still use the 44-DIP Touch target, keeping
the wider hit area while preserving the same caption-band height as adjacent
groups without a launcher. This is one shared-template setter removal; it adds
no public API or theme metric and does not alter other Touch command targets.

The RibbonKit-only launcher aggregate passed in Release across all six themes,
light/dark modes, LTR/RTL, Compact/Touch/Compact toggles and launcher visibility.
It checks the nominal height plus DPI-rounded rendered size, neighboring caption
band/baseline alignment, and unchanged launcher/spacer widths. Its Office2007 Touch
render was inspected under `artifacts/touch-launcher-diagnostics`.
The broader Touch consumer also passed in Release (26 seconds), covering the
existing adaptive, gallery, QAT, message and title/Backstage geometry checks.
Results are `touch-group-launcher-release.trx` and
`touch-launcher-consumer-release.trx` under `artifacts/touch-validation`.
The normal Release solution and Debug Showcase builds passed with zero warnings
or errors. User review of the launcher correction remains pending; the full suite,
snapshot approval and native keyboard/input checks were not rerun.

### Minimized QAT/message corners and Touch modal height — 2026-10-08

Office2024's existing combined minimized/message corner token now uses `8,8,0,0`.
The exposed QAT upper edge is rounded in both densities, while the lower edge
still joins the first message flush. Its dark palette inherits the same metric;
other palettes, drawer spacing, border and shadow rules are unchanged.

The Touch modal-close trigger retains the default six-DIP top inset used by the
minimize chevron it replaces. Removing its margin override preserves the normal
Touch header height on modal entry and exit, including labeled and icon-only
Close buttons. The 44-DIP Close target and bottom alignment remain. Both fixes
use existing shared templates/tokens and add no public API or resource key.

Both failures were reproduced in a RibbonKit-only consumer before their fixes.
The focused Release aggregate now passes across all six themes, light/dark and
LTR/RTL, all QAT placements, Touch modal entry/exit, and Compact/Touch minimized
message combinations. It checks header/body position, ribbon height, QAT corners
and Office2024's flush message seam. Results are
`touch-chrome-consumer-release.trx` under `artifacts/touch-validation`.
The broader Touch consumer passed in Release (26 seconds), including existing
adaptive, gallery, launcher, QAT and title/Backstage checks, recorded in
`touch-chrome-broader-consumer-release.trx`.

The six actual Showcase Touch cases, six message-bar tests and existing connected
QAT shadow regression passed in Release (13 selected runtime checks), recorded in
`touch-chrome-runtime-release.trx` and `touch-chrome-shadow-release.trx`.
Office2024 light/dark minimized-message and before/modal renders were inspected
under `artifacts/touch-chrome-diagnostics`. Normal Release solution and Debug
Showcase builds passed with zero warnings/errors. User visual review remains
pending; full-suite, snapshot approvals and native keyboard/input were not rerun.

### Earlier geometry and adaptive evidence

The screenshot cleanup, refinements and subsequent spacing/paint polish address:

- Align tab headers with the body so the connected notch and hover foot meet it.
- Use content-driven body height with a 136-DIP minimum, rather than a fixed tall row.
- Keep gallery tiles with one 44-DIP popup opener beside them; hide strip scrolling arrows.
  Default touch-strip cells fill the viewport height and available row width, retaining
  the selected row after density/size changes. Expanded galleries retain normal wrapping.
- Center horizontal split content and give large commands a consistent 136-DIP height,
  matching three 44-DIP small commands with their margins, subject to DPI rounding.
- Enlarge small/QAT icons to 20 DIP and large icons to 36 DIP; retain 44-DIP minimum targets.
- Match application-menu fills to their 44-DIP arrow targets; use 56-DIP rows and a wider nav column.
- Stretch in-group separators with adjacent commands, using a 44-DIP minimum so
  the small Font row does not grow to large-button height; align captions at the bottom.
- Enlarge shared and Crystal Backstage navigation to at least 52 DIP while retaining
  normal font sizes for navigation and page content.
- Use a shared 52-DIP orb and adjusted margin for the application button and Classic2007 proxy.
- Match tab-shaped File buttons to the tab panel's height and text alignment, with
  a 64-DIP minimum width and theme-specific horizontal padding. Office2013/2019
  hug the leading edge; Office2024/Crystal retain an eight-DIP leading inset.
- Let modal Close actions fit their complete label while retaining a touch target.
- Widen horizontal dropdown targets to 56 DIP and separate the 20-DIP icon from the arrow.
- Give the Classic2007 application menu a taller top band to clear the touch orb.
- Give the Classic2007 Backstage rail extra inner padding beneath its orb proxy;
  its painted edge stays aligned with the content pane to avoid a color seam.
- Add four-DIP outer horizontal gallery spacing in both densities and use a flat
  Office2010 lower-QAT brush in light and dark modes, matching the body ramp's end color.
- Honor the QAT overflow proxy's left content alignment for dropdown/split rows,
  while ordinary touch commands keep their centered default.
- Give message actions and dismiss buttons 44-DIP targets in at least 52-DIP rows,
  with 20-DIP message icons and unchanged message/action fonts.
- Reserve a 46-DIP window title band and native caption area while its title-bar
  content has touch density, including when Backstage hides the QAT presenter.
  Minimize, maximize/restore and close targets fill that 46-DIP band.
  Returning that content to compact density or moving the QAT clears the reservation.
- Start Office2010 Aero side bevels beneath the actual touch tab-header row,
  including the taller caption when the QAT occupies the title bar.
- Clip the expanded Office2024 lower QAT's upward shadow at the connected body edge,
  preserving its outer halo and the minimized QAT's floating shadow in both densities.

Only touch-specific tests ran during the implementation iterations. Full tests
were deferred until rendered cleanup review. Full runs exposed an existing
Classic Backstage fixture race: it replaced the File surface before its animated
close detached the old adorner. The fixture now waits for actual detachment and
builds successfully; its full retry was initially deferred at the user's request on 2026-10-07.
Focus and activation assertions are unchanged. Final rendered review also corrected
the Font separator's excessive minimum height. Physical input and final appearance
remain separate acceptance gates.

The subsequent eight-refinement pass used touch-only checks. It also exposed a
stale gallery scroll offset: the selected tile changed row height while its old
offset survived. The shared strip panel now asks the gallery's existing deferred
viewport refresh to restore the selected row when its cell geometry changes.
Checks assert tile visibility as well as cell size. No full-suite or native-input
retry ran during this pass; the user's deferral remained in force at that stage.

The subsequent polish pass replaces the uniform File-button horizontal geometry
with theme-specific tokens and a wider target. Classic2007 orb/menu clearance
comes from a taller menu band. Backstage clearance moves from an outer rail margin
to inner padding, preserving the rail/content background alignment. The actual
Showcase Classic2007 surface and a RibbonKit-only consumer verify that alignment.
The two shared gallery/QAT changes also receive focused compact checks. Full-suite,
snapshot approval and native-input retries were deferred until visual readiness.

The message/title/overflow pass uses shared templates and three matching metrics
in every base palette. Overflow alignment is a style default, allowing the proxy's
local left alignment to win. Title-bar density follows the retained title content,
so hiding its presenter does not change caption height. Focused tests cover
message target geometry, unchanged fonts and disabled actions, routed dismissal,
overflow alignment, compact return, and every theme/Backstage design's title height.
This pass did not run native input or the full suite.

The final caption correction overrides the caption styles' fixed 34-DIP button
height within that existing touch-title trigger. One shared-resource consumer
check passed in Debug on 2026-10-07, covering all four caption parts, their painted
height, hidden title content, compact return and moving the QAT below the ribbon.
Its rendered title band was inspected. Testing stayed minimal at the user's request;
the earlier Release and broader touch results precede this four-setter adjustment.

The Office2010 Aero follow-up replaces its fixed compact 69-DIP bevel start with
the rendered header's bottom edge in touch mode. Shared internal geometry plumbing
updates only when that edge changes; compact/non-Aero/Office2007 margins retain
their token path. One Debug consumer check using only RibbonKit resources passed
on 2026-10-07 for all three QAT placements, larger header text, RTL, compact return
and scoped left/right/bottom margin values. Actual below-ribbon and title-bar QAT
renders were inspected. This follow-up adds no public API and keeps testing focused.

For the touch-only consumer, set `RIBBONKIT_PORTABILITY_SCOPE=Touch` for that test
process; omit/clear it for the full consumer. This avoids running the existing
keyboard/navigation aggregate during each visual iteration.

The user resumed full testing on 2026-10-07. Normal Release restore/build passed
for both runtime targets with zero warnings/errors. All four test projects passed:
490 runtime, 482 Writer, the full independent-consumer aggregate and the visual
aggregate covering 113 scenes, totaling 974 tests with none failed or skipped.
The full consumer ran without the touch-only environment filter and completed
in 2m56s, including the previously deferred keyboard/focus and Backstage checks.
Package creation and validation passed, including clean net8/net9 WPF consumption.

The first full run found two stale test assumptions: the shared window now has
compact and touch caption configurations, and the Office2010 QAT deliberately
uses the body's flat end color. The contracts now check those approved changes;
the full runtime retry passed. All 113 scenes were captured deterministically.
The one visual comparison failure was the Office2010 connected message/QAT scene;
its actual/diff images showed only the intended flat QAT strip. Only that approved
PNG was refreshed, then the entire visual aggregate passed. Comparison tolerances
and product code were unchanged during this validation pass.

The subsequent recording exposed a collapsed-group width-probe race: changing
density from the open Backstage-group flyout could probe the normal host while
its commands still belonged to the popup. The group now closes and reclaims that
content synchronously before the probe, retaining the existing focus-return and
nested-popup cleanup behavior. The scroller also retains the panel's true width
when WPF reuses a cached measure, rather than replacing it with a viewport-clamped
DesiredSize. Empty rows report zero and replacing the hosted child clears the old
report. No public API or template/token geometry changed.

Focused Release checks cover repeated flyout density changes from both initial
modes across all six themes, serialized-layout/touch initialization before and
after Showcase realization, cached overflow, empty/replaced content, forced
expansion, and immediate body-arrow visibility. The Release solution build has
zero warnings/errors and 64 focused runtime checks pass. The touch-only independent
consumer passed in Debug and Release (the final Release run took 44s), recorded in
`artifacts/touch-validation/touch-adaptive-consumer-release.trx`. Normal Debug
Showcase output is refreshed. Startup and Office2024/Crystal overflow renders
were inspected in `artifacts/touch-adaptive-diagnostics`. The earlier full suite
has not been rerun after this correction.

The Office2024 seam follow-up clips the lower QAT's rendered child effect through
a parent geometry. Clipping the effect-bearing border itself would leave its
upward halo intact. A matching boolean token enables this only for connected
Office2024 drawers; minimized or top-inset drawers and other palettes keep their
previous shadow path. One Debug regression check using only shared RibbonKit
resources passed for compact/touch and light/dark paint, retained bottom and
minimized shadows, and Crystal/Office2010 isolation. Actual light/dark touch renders
were inspected under `artifacts/office2024-qat-shadow-diagnostics`; the result is
`artifacts/touch-validation/office2024-qat-shadow.trx`. Testing was minimal at the
user's request: no full-suite, snapshot approval or native-input retry.

| Gate | Status and exact scope |
| --- | --- |
| Shared implementation and API documentation | Passed 2026-10-07: documented additive API; shipped baseline unchanged; matching Touch metrics in all six base themes |
| Live density transition | Refined 2026-10-08 to 85% opacity and cubic EaseInOut; clean Release runtime builds for both targets, passing focused policy/easing test and independent Release transition aggregate. The initial 2026-10-07 implementation had a clean Release solution build, 33 selected runtime checks and independent Debug/Release and broader Touch evidence. Completion/interruption, startup, disabled policy, bindings, template/unload, QAT placements, popup anchors and affected adaptive geometry covered. Full suite and native keyboard/input deferred until visual review |
| Group launcher caption height | Passed focused Release verification 2026-10-08: RibbonKit-only six-theme/light-dark/LTR-RTL launcher aggregate and broader Touch consumer; nominal 14-DIP height, equal neighboring caption bands, retained Touch width and visibility toggles. Office2007 render inspected; user visual review pending |
| Minimized QAT/message and Touch modal geometry | Passed focused Release verification 2026-10-08: independent chrome and broader Touch aggregates, 13 selected runtime checks and clean solution build. Office2024 rounds only exposed upper QAT corners above messages in both densities; Touch modal entry/exit preserves header/body position across all themes and QAT placements. Renders inspected; user review pending |
| Touch cleanup checks | Passed 2026-10-07 in the final Release suite: six actual Showcase theme cases, caption-button and Office2010 Aero-bevel regressions, File width/edge spacing, gallery fill/selection and compact/touch neighbor spacing, large/three-row sizing, modal Close, Font separator, message targets/normal fonts, stable title height through Backstage, and actual Classic2007 rail/page paint alignment; independent consumer across every theme/light-dark/LTR-RTL combination and all seven Backstage designs |
| RibbonKit-only consumer | Adaptive follow-up touch-only scope passed 2026-10-07 in Debug and Release (final Release 44s), including repeated open-flyout density changes and immediate overflow arrows. The earlier full default scope passed in 2m56s before the follow-up, including keyboard/focus/navigation and the corrected Backstage fixture |
| Release solution build and tests | Full checkpoint passed 2026-10-07 before the adaptive-layout follow-up: both runtime targets, zero build warnings/errors, all 974 tests passed with zero failed/skipped (490 runtime, 482 Writer, full consumer aggregate and 113-scene visual aggregate); package contents/designer assets and clean net8/net9 WPF package consumption validated. The follow-up has a fresh clean Release solution build and 64 passing focused runtime checks; no new full-suite run |
| Normal Debug Showcase output | Passed 2026-10-08: default Debug Showcase/net8 runtime output refreshed for the minimized QAT/message corners and Touch modal-header correction after the launcher/animation changes; zero warnings/errors. The connected lower-QAT shadow regression also passed again with density motion disabled for static paint comparison |
| Visual acceptance | User accepted the revised 85% EaseInOut density settle on 2026-10-08 after live review. Group launcher caption-height correction awaits review separately. User confirmed the adaptive collapse/scroller correction on 2026-10-07. Office2024 light/dark touch seam renders inspected; target-machine acceptance of that shadow correction remains pending. The 113-scene automated checkpoint predates the adaptive, shadow and animation follow-ups |
| Native touch input | Pending: split halves, tap invocation, panning without accidental selection, nested popup dismissal and editable inputs |
| DPI / RTL / keyboard | Full automated keyboard/focus/navigation, touch LTR/RTL and rendered 100/125/150/200% checks passed 2026-10-07; physical input, native monitor/DPI transitions, IME and reduced-motion live review remain separate |

The first pass had combined passing evidence for 966 tests (482 runtime, 482 Writer,
the visual aggregate covering 113 scenes, and the independent consumer), including
unchanged native keyboard retries after the user confirmed concurrent input.
Those results precede this cleanup. The first-cleanup touch results are in
`artifacts/touch-validation/touch-cleanup.trx` and `touch-showcase-cleanup.trx`.
Refinement results are `touch-consumer-refinement.trx` and
`touch-showcase-refinement.trx` in that directory, with actual renders under
`artifacts/touch-refinement-diagnostics`. Current polish results are
`touch-consumer-polish.trx`, `touch-showcase-polish.trx` and their Release counterparts.
The File/header, gallery/QAT and actual Showcase Classic2007 renders were inspected
under `artifacts/touch-polish-diagnostics`, alongside the independent menu/orb and
Backstage renders. Intentional gallery spacing and Office2010 QAT paint changes
affect compact scenes; snapshot approvals remained unchanged during those iterations.
The latest message/title/overflow results are `touch-consumer-surfaces.trx`,
`touch-showcase-surfaces.trx` and their Release counterparts in that directory.
Office2024/Crystal message and title/Backstage renders, plus independent overflow
renders, were inspected under `artifacts/touch-surfaces-diagnostics`.
The final caption-only result is `touch-caption-buttons.trx` in the same validation
directory, with the inspected `touch-caption-buttons.png` in that render directory.
The Aero-bevel result is `touch-aero-bevel.trx`, with inspected
`Office2010-aero-touch-BelowRibbon.png` and `Office2010-aero-touch-TitleBar.png`
in the same render directory.
Corrected-build full results are under `artifacts/touch-validation/cleanup-confirmed`;
the stopped initial cleanup run is under `cleanup-final`. Both consumer failures
occurred at the Classic nav row's initial focus, in different theme/direction cases.
The wrapper's previous Escape diagnostic described an earlier key event, rather
than that failing focus call; the row assertion now includes visibility/parent details.
Final full-test results are under `artifacts/touch-validation/merge-final`;
initial failures and capture evidence are under `merge-full`. Only
`office2010-message-bar-connected-100.png` was reapproved after actual/diff review.
Comparison tolerances remain unchanged. No application was manually
launched; live acceptance remains with the user.

The adaptive follow-up results are `touch-adaptive-debug.trx` (60 focused checks),
`touch-adaptive-release.trx` (64 focused checks including collapsed templates and
nested dismissal), and `touch-adaptive-consumer-debug.trx` /
`touch-adaptive-consumer-release.trx` (touch-only independent consumer) in the same
validation directory. The initial flyout and cached-overflow reproduction results
are preserved separately. These focused runs do not replace the earlier full gate.

Native touch input cannot be inferred from synthetic mouse or keyboard tests.
Custom templates can consume the inherited setting and touch metric keys; fixed
heights in application-authored content may need corresponding host adjustments.

## Sources

- [Office Touch/Mouse mode](https://support.microsoft.com/en-us/word/turn-touch-mode-on-or-off)
- [Windows touch targets](https://learn.microsoft.com/en-us/windows/apps/develop/input/touch-interactions#hit-targets)
