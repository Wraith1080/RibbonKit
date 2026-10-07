# RibbonKit touch density

> Active touch plan, 2026-10-07. Screenshot feedback implemented; full consumer retry deferred at the user's request.
> Automated evidence and native acceptance are recorded separately below.

## Contract and API review

Add `RibbonDensity` (`Compact = 0`, `Touch = 1`) and an inherited attached
`Ribbon.Density` dependency property, including the normal ribbon CLR property
and `GetDensity`/`SetDensity` accessors. Compact remains the default. This additive
API belongs in `PublicAPI.Unshipped.txt`; the shipped baseline is unchanged.

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
   widths and selection chrome when density changes.
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

Only touch-specific tests ran during the implementation iterations. Full tests
were deferred until rendered cleanup review. Full runs exposed an existing
Classic Backstage fixture race: it replaced the File surface before its animated
close detached the old adorner. The fixture now waits for actual detachment and
builds successfully; its full retry is deferred at the user's request on 2026-10-07.
Focus and activation assertions are unchanged. Final rendered review also corrected
the Font separator's excessive minimum height. Physical input and final appearance
remain separate acceptance gates.

The subsequent eight-refinement pass used touch-only checks. It also exposed a
stale gallery scroll offset: the selected tile changed row height while its old
offset survived. The shared strip panel now asks the gallery's existing deferred
viewport refresh to restore the selected row when its cell geometry changes.
Checks assert tile visibility as well as cell size. No full-suite or native-input
retry ran during this pass; the user's deferral remains in force.

The subsequent polish pass replaces the uniform File-button horizontal geometry
with theme-specific tokens and a wider target. Classic2007 orb/menu clearance
comes from a taller menu band. Backstage clearance moves from an outer rail margin
to inner padding, preserving the rail/content background alignment. The actual
Showcase Classic2007 surface and a RibbonKit-only consumer verify that alignment.
The two shared gallery/QAT changes also receive focused compact checks. Full-suite,
snapshot approval and native-input retries remain deferred until visual readiness.

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

| Gate | Status and exact scope |
| --- | --- |
| Shared implementation and API documentation | Passed 2026-10-07: documented additive API; shipped baseline unchanged; matching Touch metrics in all six base themes |
| Touch cleanup checks | Passed 2026-10-07 in Debug and Release: six actual Showcase theme cases, including File width/edge spacing, gallery fill/selection and compact/touch neighbor spacing, large/three-row sizing, modal Close, Font separator, message targets/normal fonts, stable title height through Backstage, and actual Classic2007 rail/page paint alignment; independent consumer across every theme/light-dark/LTR-RTL combination and all seven Backstage designs, including title/caption height, left-aligned overflow dropdown/split rows, message dismissal/disabled action, orb/menu clearance and flat Office2010 light/dark QAT paint |
| RibbonKit-only consumer | Touch scope passed 2026-10-07: local overrides, detached QAT, overflow/context menus, long-menu reachability, retained gallery selection, adaptive widths and routed touch dismissal; corrected full-consumer retry deferred at the user's request on 2026-10-07 |
| Release solution build and tests | Last build passed 2026-10-07 for both runtime targets with zero warnings/errors; focused Showcase and touch-only consumer checks passed before the final caption-button and Aero-bevel adjustments, which received focused Debug checks under the user's minimal-test instruction. The earlier cleanup had 971 passing full-suite tests (488 runtime, 482 Writer, 1 visual aggregate), preceding subsequent refinements, polish and message/title/overflow changes. Full tests await visual readiness and the full consumer retry remains deferred by the user |
| Normal Debug Showcase output | Passed 2026-10-07: default Debug Showcase/net8 runtime output refreshed after the Office2010 Aero-bevel correction; its focused shared-resource consumer check passed. The preceding normal solution build covered both runtime targets with zero warnings/errors |
| Visual acceptance | Pending: Office and Crystal, normal/minimized ribbon, narrow widths, QAT placements/overflow, message targets, title/Backstage transition, gallery and File surfaces |
| Native touch input | Pending: split halves, tap invocation, panning without accidental selection, nested popup dismissal and editable inputs |
| DPI / RTL / keyboard | Current automated touch LTR/RTL checks passed; prior-cleanup Writer native keyboard checks passed. Full consumer keyboard retry deferred by the user on 2026-10-07; live review pending at 100/125/150/200%, monitor transitions, touch-mode Tab/KeyTips/Esc and reduced motion |

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
affect compact scenes; snapshot approvals remain unchanged pending final visual review.
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
Snapshot approvals and tolerances are unchanged. No application was manually
launched; live acceptance remains with the user.

Native touch input cannot be inferred from synthetic mouse or keyboard tests.
Custom templates can consume the inherited setting and touch metric keys; fixed
heights in application-authored content may need corresponding host adjustments.

## Sources

- [Office Touch/Mouse mode](https://support.microsoft.com/en-us/word/turn-touch-mode-on-or-off)
- [Windows touch targets](https://learn.microsoft.com/en-us/windows/apps/develop/input/touch-interactions#hit-targets)
