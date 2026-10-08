# RibbonKit touch density

> Touch/Mouse dropdown implemented with shared popup headings and descriptive rows; heading corners refined for Office2007/2010/2024 and Crystal, 2026-10-08. Heading visual review is pending.
> Current full Release validation passed 2026-10-08: 1003 tests in combined final results, including the full RibbonKit-only consumer and 113 visual scenes. Package validation passed; visual/native acceptance remains separate.

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

The Touch/Mouse selector follow-up adds three optional properties:
`RibbonDropDownButton.DropDownHeader`, `RibbonMenuItem.Description` and
`RibbonMenuItem.LargeIcon`. Their dependency-property fields and CLR accessors
add nine symbols to `PublicAPI.Unshipped.txt`, with XML documentation. The
shipped baseline remains unchanged. Defaults retain existing menu presentation.

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

### Full Release validation — 2026-10-08

Validation covers `codex/touch-mode` at `28c37f9` plus the test-fixture and snapshot
corrections below. Restore and the normal Release solution build passed for both
runtime targets with zero warnings/errors. Combined final results cover all four
test projects: 519 runtime tests, 482 Writer tests, the default independent-consumer
aggregate and the visual aggregate covering 113 scenes, totaling 1003 tests with
zero failures or skips. These are final per-project results after investigation
and retries, rather than a claim that the initial solution run passed. This
checkpoint supersedes the earlier deferred full-test statuses in the dated
implementation notes; the historical 974-test result is not current evidence.

The full consumer ran with `RIBBONKIT_PORTABILITY_SCOPE` cleared and passed in
3m10s, including density/adaptive regressions, message transitions, popup headings,
QAT placements, keyboard/focus/navigation, galleries, application menus and
Backstage. It references RibbonKit without Showcase resources or helpers. Writer's
initial native keyboard check observed Ctrl held; the serial full retry passed
unchanged after the user confirmed input would remain idle. WPF test projects
were serialized for retries so their windows did not compete for focus.

Three test fixtures needed corrections, without runtime changes. The static
Office2024 QAT-shadow comparison now disables all motion while sampling paint,
including theme/minimize transitions, and restores the prior global setting.
Its pixel tolerance remains unchanged. The non-Crystal QAT matrix now reads the
combined minimized/message corner token when both states apply. Message-transition
checks retain values from the observed frame, accept nonterminal opacity rather
than requiring the middle of a short fade, and use software rendering only for
that offscreen fixture, restoring the previous process setting afterward. Traces
showed the first observed frame arriving at 131–164ms, sometimes after the 160ms
fade ended. Exact row/rim opacity matching, render/layout transform checks,
completion, interruption and cleanup assertions remain; runtime timings and
animation policy are unchanged.

All 113 visual scenes were captured and their comparison results inspected. Only
`office2024-message-bar-stack-100.png` and
`office2024-rtl-message-bar-stack-100.png` differed: actual/diff review showed the
intended removal of the connected lower-QAT's upward shadow at the body seam.
Only those two approvals were refreshed; the other 111 matched. The normal full
comparison then passed with unchanged tolerances. This verifies snapshots, not
new user acceptance of every visual state.

Release package creation and `eng/Validate-Package.ps1` passed, including package
layout, XML documentation, designer assets and clean net8/net9 WPF consumption
builds. Older package files were preserved; only the current 1.0.0 package pair
was supplied to the validator. Package-consumer runtime launch was not requested
or run. The shipped API baseline is unchanged. Normal Debug Showcase output was
already refreshed for the committed heading refinement; this pass changed no
runtime or Showcase code and did not manually launch an application.

Evidence is under `artifacts/touch-validation/dropdown-full-20261008`:

- `build-verified-release.log`: final normal Release solution build.
- `runtime-final.trx`: all 519 runtime tests passed.
- `serial/LENOVO_BRIN-MM-2026-00_2026-10-08_10_23_44_net8.0.trx`: all 482 Writer tests passed.
- `consumer-software-rendering.trx`: default full consumer passed, with no scope filter.
- `message-transition-final.trx`: final focused retry passed after also retaining both root and rim layout-transform checks in the saved frame.
- `visual-reviewed.trx`: normal 113-scene comparison passed after reviewed approvals.
- `visual-capture.trx`, `TestResults/visual-capture` and `visual-capture-start.txt`: capture/review evidence.
- `pack-release.log` and `package-validation.log`: package and clean consumption validation.

Initial failures and intermediate diagnostic attempts remain in the same evidence
directory. Live heading review, physical touch, mixed-monitor/native DPI changes,
IME and target-machine reduced-motion review remain separate. No commit, push or
merge was performed.

### Touch/Mouse dropdown selector — 2026-10-08

Showcase's View-tab toggle is now a Touch/Mouse Mode dropdown with a heading,
large vector icons, two-line descriptions and a highlighted current choice.
Mouse selects Compact; Touch selects Touch. Existing appearance preference
restoration still sets density directly before the first render. The saved
Boolean preference and customization exclusion are unchanged. The explicit
command ID `auto:tab.view/group.view.backstage/Touch mode#2` preserves existing
saved QAT/custom-layout entries when the former toggle becomes a dropdown.

The presentation is reusable RibbonKit behavior. Both dropdown and split-button
templates place `DropDownHeader` outside the native scrolling items area; null
or empty hides it. QAT proxies bind to the source heading and retain the existing
borrow/return protocol. `RibbonMenuItem.Description` supplies wrapped secondary
text and default UI Automation help text. `LargeIcon` uses 32 DIP in Compact and
the existing Touch large-icon metric. Plain rows retain their prior small-icon
column, font weight and geometry. The existing public `Background` now paints
the row chrome, allowing application-owned choice highlighting; hover and press
retain precedence. No template coordinator or Showcase helper was introduced.
Applications own the icons, choices, commands and persistence.

The normal Release solution build passed for both runtime targets with zero
warnings/errors. Focused runtime verification passed in combined evidence:
61 existing dropdown-borrowing, Touch adaptive/layout, appearance preference and
startup checks passed in `density-selector-runtime-release.trx`; all seven
Showcase view-choice checks and eleven popup/DPI/dark-template checks passed in
the final `density-selector-popup-review-release.trx`. One initial Office2019
view-choice assertion incorrectly expected selected paint while the row was
hovered. The fixture now verifies the existing pressed/hover/selection precedence;
the selected background state remained correct and no product workaround or
pixel tolerance changed. The final view-choice checks cover all six themes,
selection/dismissal, QAT borrowing, legacy command identity and customization
round trips without reading or writing the user's preferences.

The independent RibbonKit-only `DensitySelector` scope passed in Release
(18 seconds), recorded in `density-selector-consumer-release.trx`. It covers all
six themes, light/dark, both densities and LTR/RTL for dropdown and split-button
headings, described and ordinary rows, native scrolling, scoped resources and
UI Automation help text. Its QAT cases cover both control types in all three
placements with live heading updates, density selection, dismissal and content
return. It references no Showcase resources or helpers. Set
`RIBBONKIT_PORTABILITY_SCOPE=DensitySelector` for this focused process; the default
consumer aggregate also includes it.

Office2007, Office2024 and Crystal actual popup renders were inspected under
`artifacts/density-selector-showcase-diagnostics`; independent consumer renders
are under `artifacts/density-selector-consumer-diagnostics`. TRX results are in
`artifacts/touch-validation`. Normal Debug Showcase output is refreshed for
review. These focused runs preceded the current full checkpoint above. Live
input and mixed-monitor gates remain separate. The historical 974-test checkpoint does
not validate this dropdown or the preceding adaptive and animation follow-ups.

The initial heading-corner refinement used the shared
`RibbonKit.Metrics.DropDownHeaderCornerRadius` resource in both dropdown and
split-button templates. All six base themes define it; dark palettes inherit
the same geometry. Its upper radii match the popup's 3 DIP for Office2007/2010,
6 for Office2024 and 8 for Crystal, with zero lower radii. Office2013/2019 remain
square. Padding, row layout, scrolling and popup outlines are unchanged; this
adds no C# API. The updated RibbonKit-only matrix passed in Release (16 seconds),
including both control types and QAT placements; all seven Showcase selector
checks passed again. Results are `density-selector-header-corners-consumer-release.trx`
and `density-selector-header-corners-showcase-release.trx` in
`artifacts/touch-validation`. Actual native-heading renders for the four affected
themes were inspected under `artifacts/density-selector-header-corner-diagnostics`
and `artifacts/density-selector-header-corner-showcase-diagnostics`. Normal
Release runtime and Debug Showcase builds passed with zero warnings/errors.
The subsequent review requested rounded lower corners to match the menu's
selection and hover surfaces. The same metric now uses uniform 3/6/8-DIP radii
in the four affected themes; Office2013/2019 stay square. This is a token-only
refinement, without new APIs or layout changes. Minimal verification is recorded
in `density-selector-all-corners-consumer-release.trx`: the updated RibbonKit-only
selector scope passed, and normal Debug Showcase output was refreshed with zero
warnings/errors. Current actual renders are under
`artifacts/density-selector-all-corner-diagnostics`. The preceding seven Showcase
checks and both-target Release build cover the upper-corner pass, not a fresh
full run. The current full checkpoint above covers the lower-corner refinement;
the new heading appearance awaits user review and live native gates stay separate.

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

### Minimized document-boundary divider — 2026-10-08

The divider previously lived in RibbonTabControl's body row, so a minimized
ribbon left it above a below-ribbon QAT or open messages. It now belongs to the
owning Ribbon template's footer row, after both surfaces and immediately above
the document. The application-menu overlay and design Backstage host span that
footer too. The user's subsequent review found a duplicate edge too thick when
the QAT or messages already provide their own border. The divider is therefore
collapsed when QAT placement is BelowRibbon or any message is open. It appears
only for the minimized header directly above the document. Its existing full-width
brush, height token, hit transparency and immediate minimized-state trigger remain;
expanded ribbons and suppressed dividers reserve no footer height.
Office2007/2010/2013 keep their hairline, while the zero-height palettes retain
their opt-out. No public API, new resource key or consumer helper was added.

A RibbonKit-only consumer first reproduced the misplaced edge. The updated
visibility aggregate passed in Release across all six themes, light/dark and
LTR/RTL, both densities, all three
QAT placements, zero/one/two messages and repeated minimized/expanded changes.
It starts minimized and asserts the exposed divider ends at the document's top
and keeps full ribbon width, while QAT/message and expanded states contribute no
divider height. Office2007 exposed-header, lower-QAT and stacked-message renders
were inspected under `artifacts/minimized-divider-diagnostics`. The current focused
result is `minimized-divider-refinement-release.trx` under `artifacts/touch-validation`.
The initial relocation aggregate and broader Touch consumer (30 seconds) are
`minimized-divider-consumer-release.trx` and `minimized-divider-broader-consumer-release.trx`.

The initial relocation pass also had 24 passing selected layering, message-bar
and connected-QAT-shadow checks in Release, recorded in
`minimized-divider-runtime-release.trx`. These and the broader Touch run precede
the visibility refinement. The initial layering
check still assumed the QAT border was a direct overlay sibling, predating its
shadow-clip wrapper; it now checks that wrapper and the new divider remain below
the application menu. Product shadow geometry and visual approvals are unchanged.
Normal Release solution and Debug Showcase builds passed with zero warnings or
errors. The full suite, snapshots and native-input checks were not rerun.

### Office exposed message top border — 2026-10-08

Office message rows omit their top border to join the body or lower QAT.
When the ribbon is minimized and the QAT is elsewhere, the message stack now
supplies its own single top edge. The shared RibbonMessageBar template draws it
only while messages remain presented and no below-ribbon QAT provides that edge.
The stack border survives dismissal of the first row without tracking row order;
individual message borders, action targets and inter-row seams are unchanged.

The new `RibbonKit.Metrics.MessageBar.ExposedTopBorderThickness` theme metric is
`0,1,0,0` in all five Office themes' light/dark palettes and `0` in Crystal,
which retains its existing card borders. It is defined
in all twelve dictionaries and resolves through DynamicResource; the edge uses
the existing message-border brush. This adds no C# API and leaves the shipped
baseline unchanged. The resource extension is documented in README.

The RibbonKit-only focused Release consumer passed across all six themes,
light/dark, LTR/RTL, both densities and every QAT placement. It covers zero/one/two
messages, first-row dismissal with the next row still open, restore and empty-bar
cleanup. It checks the stack edge, unchanged individual borders and the accepted
divider visibility rule. Six message-bar runtime checks also passed in Release.
The initial Office2007 results are `message-top-border-consumer-release.trx` and
`message-top-border-runtime-release.trx` under `artifacts/touch-validation`, with
inspected renders under `artifacts/message-top-border-diagnostics`.

After the user reported the same missing edge in the other Office themes, the
existing metric was enabled in Office2010/2013/2019/2024 light/dark. The updated
independent matrix passed in Release (6 seconds), together with all six runtime
message-bar checks. Latest results are `office-message-top-border-consumer-release.trx`
and `office-message-top-border-runtime-release.trx` under `artifacts/touch-validation`.
Office2010/2013/2019/2024 and unchanged Crystal renders were inspected under
`artifacts/office-message-top-border-diagnostics`. Normal Release solution and
Debug Showcase builds passed with zero warnings/errors. This extension adds no
new resource key, template behavior or C# API. User visual review remains pending;
full-suite, snapshot approvals and native-input checks were not rerun.

### Minimized exposed message and Office2007 QAT corners — 2026-10-08

When messages directly meet a minimized header, Office2007 now rounds the exposed
message stack's upper corners to 3 DIP and Office2024 to 8 DIP. The additive
`RibbonKit.Metrics.MessageBar.ExposedTopCornerRadius` resource has matching keys
in all twelve palettes: `3,3,0,0` for Office2007, `8,8,0,0` for Office2024 and `0`
elsewhere. The shared stack clips the rows' paint inside a native top rim, then
draws that rim above the rows so the first background cannot cover the curved
border. This avoids first-container state, preserves
individual message borders and lower corners, follows first-row dismissal, and
releases the clip on restore, empty state or below-ribbon QAT placement. Existing
row margins, action targets and message animation behavior remain.

Office2007's existing `QatExtenderCornerRadiusMinimizedMessageBar` metric now uses
`3,3,0,0`, inherited by its dark palette. As with Office2024, a minimized lower
QAT keeps its exposed upper corners while its lower edge joins messages flush.
Both changes are in RibbonKit; no C# API or shipped baseline changed. The new
theme resource is documented in README.

The RibbonKit-only exposed-message matrix passed in Release (5 seconds), covering
all six themes, light/dark, LTR/RTL, both densities, every QAT placement, zero/one/two
messages, first-row dismissal and restore. It checks the rounded clip as well as
the radius, row borders and accepted divider policy. The Office2007 QAT case first
failed with square corners, then the focused chrome matrix passed (3 seconds),
including Office2007/2024 joined-message corners and Touch modal geometry.
Results are `minimized-message-corners-consumer-release.trx` and
`minimized-qat-corners-consumer-release.trx` under `artifacts/touch-validation`.
Sixteen message-bar, Showcase startup and theme-resource checks passed in Release
(`minimized-corners-runtime-release.trx`). Office2007 light/dark QAT and exposed
Office2007/2024 message renders were inspected under
`artifacts/minimized-top-corner-diagnostics`. Normal Release solution and Debug
Showcase builds passed with zero warnings/errors. Full-suite, snapshot approvals
and native-input checks were not rerun; user visual review remains pending.

The next screenshots exposed a clipped-looking curve in both Office2007 and
Office2024. The initial whole-stack circular clip also trimmed the native rim,
whose rendered radius includes border thickness. The shared template now clips
only the ItemsPresenter. A private `MessageBarGeometryConverter` follows the
native border's inner curve and layout-rounded stroke/inset; the native top rim
uses the rows' side thickness and a rectangular top band that leaves its curve
untouched. This matches the lower corners without altering the existing top
inset, row layout or lower borders. No additional public API or theme key was
added. The previous application-menu geometry converter is unchanged.

The refined RibbonKit-only exposed-message matrix passed in Release (5 seconds),
including an exact corner-alpha comparison against an unclipped native border
at the consumer's current 125% DPI, first-row dismissal, all themes/light-dark,
both densities, QAT placements and restore/empty cleanup. The separate chrome
matrix passed (4 seconds), preserving Office2007/2024 QAT joins and Touch modal
geometry. Sixteen message/startup/resource-scope checks and normal Release/Debug
builds passed. Latest results are `message-corner-clip-consumer-release.trx`,
`message-corner-clip-qat-consumer-release.trx` and
`message-corner-clip-runtime-release.trx` under `artifacts/touch-validation`.
Office2007/2024 light/dark renders and the supplied enlarged corner were inspected
under `artifacts/message-corner-clip-diagnostics`. Full/native/snapshot gates remain
deferred; the refreshed Debug Showcase is ready for renewed visual review.

### Exposed message rim transition — 2026-10-08

The exposed top rim was outside each row's animated `PART_Root`, so adding or
dismissing the first message left that edge fully opaque. RibbonKit now mirrors
the first presented row's effective opacity onto the stack-owned rim. It uses
the existing MessageBar timing and reduced-motion policy without another clock.
The rim stays anchored to the fixed corner clip while the row keeps its existing
glide; moving the rim separately would pull it above the clipped fill during a
transition. Adding lower rows does not replay the top edge. First-row dismissal
hands it to the next presented row, and empty state, unload and template replacement
release the old binding. Generated ItemsSource containers are supported.

The additive API record contains `RibbonMessageBar.OnApplyTemplate()` overriding
the inherited WPF lifecycle method to reconnect the rim after template replacement.
It has XML documentation; `PublicAPI.Shipped.txt` is unchanged. No new motion
action, resource key or consumer setting is required.

The RibbonKit-only transition aggregate first reproduced an opaque rim over a
transparent entrance frame, then passed in Release (20 seconds). Its live clocks
cover Office2007/2024 light/dark, both densities, title/tab-row QAT placement,
opening and dismissal, lower-row addition, first-row handoff, rapid reopen,
global/action-disabled motion, connected below-QAT placement, Expressive motion,
generated MVVM containers, template replacement and unload/reload. System-policy
inheritance is checked against the current OS setting; native reduced-motion
review remains deferred. Render-frame observation avoids missing a short fade
during offscreen theme/layout work.

The existing six-theme corner/divider/QAT-placement matrix passed again in Release
(3 seconds), retaining its native-border alpha comparison and cleanup checks.
Sixteen focused message/startup/resource-scope runtime tests passed. Results are
`message-transition-consumer-release.trx`, `message-transition-geometry-release.trx`
and `message-transition-runtime-release.trx` under `artifacts/touch-validation`;
opening/dismissal renders were inspected under `artifacts/message-transition-diagnostics`.
Normal Release solution (both runtime targets) and Debug Showcase builds passed
with zero warnings/errors. Full-suite, snapshot approvals and native-input checks
were not rerun; user visual review remains pending.

### Startup theme replacement — 2026-10-08

Restoring a tab-row QAT before the first theme application could throw
`InvalidOperationException` for `QatTabRowColored`. The actual Showcase startup
reproduction failed with the user's stack: removing App.xaml's palette synchronously
invalidated the shared QAT selectors, whose callback read another selector while
no palette was available. `ThemeManager.Apply` now merges the replacement first,
then removes prior palettes while excluding the replacement from first-call
cleanup. Existing WPF precedence, scoped palettes and accent-band policy remain.
No public API or shipped baseline changed; saved preferences were not modified.

Focused Release verification passed 12 tests, including six actual Showcase
startup cases and existing Crystal theme/resource-scope checks
(`qat-showcase-startup-release.trx`). The RibbonKit-only consumer passed its
startup and live theme matrix across all six themes, both densities, light/dark
and accent toggles, plus scoped-policy/QAT-placement checks (9 seconds,
`qat-theme-restore-consumer-release.trx`). Both result files are under
`artifacts/touch-validation`. Normal Release solution and Debug Showcase builds
passed with zero warnings/errors. Offscreen startup was verified; full-suite,
snapshot and native-input checks remain deferred.

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
had not been rerun at that focused checkpoint; the current 2026-10-08 full results
above include this correction.

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
| Touch/Mouse dropdown selector | Full Release runtime and RibbonKit-only consumer passed 2026-10-08 for the final uniformly rounded headings, dropdown/split-button matrix and all QAT placements. Three documented shared properties, nine additive Unshipped symbols; shipped baseline unchanged. Actual renders inspected; heading user review pending |
| Live density transition | Refined to 85% opacity and cubic EaseInOut, visually accepted 2026-10-08. Current full Release runtime and independent consumer passed, covering completion/interruption, startup, disabled policy, bindings, template/unload, QAT placements, popup anchors and adaptive geometry. Physical input and live reduced-motion review remain separate |
| Group launcher caption height | Passed focused Release verification 2026-10-08: RibbonKit-only six-theme/light-dark/LTR-RTL launcher aggregate and broader Touch consumer; nominal 14-DIP height, equal neighboring caption bands, retained Touch width and visibility toggles. Office2007 render inspected; user visual review pending |
| Minimized QAT/message and Touch modal geometry | Passed focused Release verification 2026-10-08 after adding Office2007 to the upper-corner correction: independent chrome aggregate checks Office2007/2024 exposed upper QAT corners, flush message seams, both densities and Touch modal position across all themes/placements. Initial Office2024 fix's broader Touch aggregate and 13 runtime checks remain earlier evidence. Latest renders inspected; user review pending |
| Minimized document-boundary divider | User accepted the conditional divider on 2026-10-08: it appears only below an exposed minimized header and is suppressed with a lower QAT or open messages. Focused Release six-theme/both-density visibility matrix passed. Initial relocation's broader Touch consumer and 24 runtime checks predate this refinement; full/native gates remain separate |
| Office exposed message top border and corners | User visually accepted the message-rim fade and corner appearance on 2026-10-08. Current full Release runtime and consumer passed, including Office2007/2024 light/dark, both densities, QAT placements, interruption, disabled motion, template/unload/generated-container cleanup and the six-theme native-corner geometry matrix. Native gates remain separate |
| Startup theme replacement | User confirmed startup runs correctly on 2026-10-08. Focused Release evidence reproduced the exact tab-row QAT exception, then passed six actual Showcase startup cases, six existing theme/resource-scope tests and the RibbonKit-only startup/live-switch/scoped-policy aggregate. Replacement palettes remain available during cleanup; public API and saved preferences unchanged |
| Touch cleanup checks | Passed 2026-10-07 in the final Release suite: six actual Showcase theme cases, caption-button and Office2010 Aero-bevel regressions, File width/edge spacing, gallery fill/selection and compact/touch neighbor spacing, large/three-row sizing, modal Close, Font separator, message targets/normal fonts, stable title height through Backstage, and actual Classic2007 rail/page paint alignment; independent consumer across every theme/light-dark/LTR-RTL combination and all seven Backstage designs |
| RibbonKit-only consumer | Default full scope passed 2026-10-08 in Release (3m10s), with no scope filter and no Showcase resources/helpers. Includes all adaptive, transition, heading, QAT, message, keyboard/focus/navigation and Backstage matrices |
| Release solution build and tests | Current full checkpoint passed 2026-10-08: both runtime targets, zero build warnings/errors, 1003 tests in combined final results with zero failed/skipped (519 runtime, 482 Writer, full consumer aggregate and 113-scene visual aggregate). Package contents/designer assets and clean net8/net9 WPF package consumption validated. Initial failures and reviewed fixture/snapshot corrections are recorded above |
| Normal Debug Showcase output | Passed 2026-10-08: default Debug Showcase/net8 runtime output refreshed for the Touch/Mouse dropdown after the message-rim, corner, QAT, startup, border, divider, modal, launcher and density animation fixes; zero warnings/errors. The preceding connected lower-QAT shadow regression passed with density motion disabled for static paint comparison |
| Functional and visual acceptance | User accepted the current functional implementation for their own merge on 2026-10-08; closer Office-style command-position glides are deferred. Previously accepted the revised density settle, adaptive collapse/scroller correction and message-rim appearance. Separate heading/launcher/shadow target-machine review and native gates remain separate. Current 113-scene automated comparison passed 2026-10-08 after review of only the two intended Office2024 connected-QAT shadow changes; automated snapshots do not replace live acceptance |
| Native touch input | Pending: split halves, tap invocation, panning without accidental selection, nested popup dismissal and editable inputs |
| DPI / RTL / keyboard | Current full automated keyboard/focus/navigation, touch LTR/RTL and rendered DPI matrices passed 2026-10-08. Writer's full native keyboard retry passed with input idle. Physical touch, native monitor/DPI transitions, IME and reduced-motion live review remain separate |

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

## Popup title follow-up

Requested 2026-10-08: add an optional title to gallery dropdown popups with the
same shared, themed heading treatment. Split-button dropdowns already inherit
`DropDownHeader` and use that treatment; retain them in the follow-up's consumer
examples and parity checks. This future gallery work is outside the current
merge-validation run and is not implemented here.

## Sources

- [Office Touch/Mouse mode](https://support.microsoft.com/en-us/word/turn-touch-mode-on-or-off)
- [Windows touch targets](https://learn.microsoft.com/en-us/windows/apps/develop/input/touch-interactions#hit-targets)
