# M6A native geometry motion

Status: **first opt-in implementation; human visual and performance acceptance pending**, 2026-10-29.

User review, 2026-10-29: layout, density and QAT movement otherwise look good;
the active marker lingering during movement and blue Office hover markers required
the correction below. Corrected marker behavior and new tab-switch motion await review.

The user selected Classic/Simplified, Mouse/Touch and QAT movement as the first
visible motion. M6A remains ahead of M6B window integration/title-bar QAT.

## Current contract

- `RibbonControl.AnimationLevel` accepts `None`, `Subtle` or `Expressive`. The
  library defaults to None. Showcase opts into Subtle. Applications own this
  appearance preference; customization JSON does not persist it.
- Single changes to `LayoutMode`, `Density`, `QuickAccessPosition`, `IsMinimized`
  and expanded `SelectedTab` animate. Minimized-tab popup activation remains immediate.
  Geometry, command ownership and native editor bounds commit once. An inert
  drawing plane presents natural-size endpoint drawings, interpolated panel
  geometry and actual QAT strip travel. Text/icons are not stretched with panels.
  `IsAnimating` reports the drawing phase, not an uncommitted model transaction.
- Subtle requests 130 ms for layout/density and 150 ms for QAT. Expressive uses
  1.4 times those durations and adds a gentle spring to the active tab indicator.
  Minimize/restore requests 180/252 ms for Subtle/Expressive.
  A monotonic clock determines progress; the UI timer
  requests frames and is not display synchronization. Endpoint capture/layout
  does not run on each tick. The layered drawing plane reuses its DIB/DC.
- The library owns completion. Input, repeated geometry requests, popup opening,
  model changes, resize, movement/scroll of an ancestor, hiding, deactivation,
  handle destruction and disposal settle the committed state and release the
  drawing plane. Input continues to the real control. A repeated request commits
  the newest state and snaps; continuous retargeting is not implemented.
- Invalid focused input continues to block layout/density changes. System-disabled
  client-area animation always uses immediate presentation. Startup, batch updates,
  Backstage, clipped source/destination bounds, oversized surfaces, application-owned
  root/Form opacity or transparency and unavailable native presentation also retain
  immediate changes.
- The host needs a Windows 8-or-later compatibility declaration for layered child
  windows. Showcase and the public consumer supply a Windows 10 declaration in
  their manifests. The host still owns DPI awareness through its startup/manifest.
  Without the compatibility declaration, native setup fails safely to immediate
  geometry. See [Microsoft's layered-child manifest contract](https://learn.microsoft.com/en-us/windows/win32/winmsg/using-windows#using-layered-windows).

The live ribbon remains logically visible, keeps its editor HWNDs and uses a
temporary native opacity hold. The owned drawing plane does not activate or
capture input. Cleanup restores only the style bit added by the library, including
when an application change callback throws. Authored layered opacity is preserved
by choosing the immediate path instead.

## Capture corrections from review

`DrawToBitmap` on the complete control ignored the native ComboBox's text-only
region and captured its square native arrow over the themed frame. That produced
the reported button-edge flash. Endpoint capture now prints the root/presenter
through their normal HWND paint context, composes native text only inside the
editor's live region, and retains other child presentations such as galleries.
Compact and Touch therefore use the same arrow geometry/clipping as live painting.

A direct `Graphics.FromImage` paint attempt changed the GDI text appearance and
was replaced with `WM_PRINT` using the normal HWND font context. Opaque RGB endpoint
bitmaps avoid GDI alpha-channel artifacts. The departure remains opaque beneath
the arriving image so identical overlapping glyphs retain their normal weight.
An early empty-value capture was a fixture initializer-order error, not lost input.

## Focused evidence

- Normal Debug native solution build passed with zero warnings/errors, including
  native XML documentation/API baseline and design compilation.
- `tests/RibbonKit.WinForms.Tests/TestResults/m6a-geometry-final.trx`: 9/9 passed.
  Covers the three transitions, handle/owner retention, autonomous completion,
  None/system-disabled/startup fallback, input/repeated requests/resize/policy,
  invalid input and throwing callbacks, ancestor movement and gallery capture.
  Compact/Touch pixel checks preserve the themed arrow marker and label, compare
  captured tab text to normal HWND printing, and verify unchanged glyph pixels
  through intermediate blend values. Static captures were inspected under
  `TestResults/visual/m6a-offscreen/`.
- The final clipped-destination/Form-transparency guard separately passed 1/1 in
  `m6a-safe-destination.trx`; it adds an immediate fallback and preserves authored
  opacity. Related existing density, Simplified, QAT and ComboBox regressions
  passed 19/19 in `m6a-existing-geometry-editors.trx`. Earlier native captures
  precede only that fallback guard; their successful playback path is unchanged.
- Public-only ordinary-Form consumer passed for Office2024 and neutral at actual
  120 DPI, reported GDI refresh 60 Hz: `artifacts/m6a-geometry-office-final/` and
  `artifacts/m6a-geometry-neutral-final/`. It covers first and return transitions,
  actual screen frames, focus/HWND/owner retention, typed input during motion,
  repeated requests and None. Intermediate layout, Touch and QAT frames were
  inspected. It uses project outputs and no Showcase resources/helpers.
- `timing.json` retains each transition's preparation cost, paint-interval
  samples/p50/p95/p99/max, total elapsed time and UI-thread managed allocation.
  Office preparation was 23–68 ms, median paint intervals about 15–16 ms, with a
  27 ms maximum; neutral preparation was 23–64 ms and maximum interval 19 ms.
  Office allocated about 2.5–7.0 MB per transition; neutral about 0.11–0.16 MB.
  These short Debug runs are observations, not an agreed pacing budget or
  compositor/FPS/leak acceptance. Endpoint preparation and Office allocation
  remain performance work.

Initial checks caught a recursive immediate fallback (fixed), missing host
compatibility manifest, and an invalid screenshot enum combination (capture now
uses the native capture flags). Passed earlier scopes were retained; only changed
or unfinished checks were repeated. No full aggregate, WPF, Release, package,
actual VS or physical mixed-monitor campaign was run for this pass.

## Remaining bounded work

1. Human review of the corrected Classic/Simplified, density and QAT presentation,
   especially native fields and source/destination crossfades. Agree representative
   workloads and a display-cadence budget before performance acceptance. Diagnose
   preparation/allocation outliers and review resource plateaus under repetition.
2. Optional host transition/progress notifications and application-content
   synchronization, proposed before implementation. **Application content commits
   its layout immediately in this pass.** The library must always complete without
   host acknowledgement. Include both QAT placements, minimized bodies and message
   joins when coordinating application content.
3. Broader native gates: physical mixed DPI/monitor movement, popup/editor focus
   combinations, minimized QAT and message joins, RTL/localization after their
   component slice, reduced-motion setting changes during a live transition and
   high-refresh presentation tracing. Unit system-policy overrides do not alter
   the machine setting. Title-bar relocation follows M6B's host contract.
4. Additional motion (popup/gallery/Backstage effects)
   requires its own bounded scope and relevant checks. Current popups settle an
   active geometry transition before opening. Batch customization remains immediate.

## Reproduce the focused native pass

From the repository root, build the normal Debug native solution, then run:

```powershell
dotnet test winforms/tests/RibbonKit.WinForms.Tests --filter FullyQualifiedName~GeometryMotionTests
winforms/tests/RibbonKit.WinForms.NativeTests/bin/Debug/net10.0-windows/RibbonKit.WinForms.NativeTests.exe --geometry-motion-checks winforms/artifacts/m6a-review-office
winforms/tests/RibbonKit.WinForms.NativeTests/bin/Debug/net10.0-windows/RibbonKit.WinForms.NativeTests.exe --geometry-motion-checks-flat winforms/artifacts/m6a-review-neutral
```

The package consumer accepts `--validate-geometry-motion` and
`--validate-geometry-motion-flat`; a fresh-package run remains unperformed.

## Menu-triggered motion correction — 2026-10-29

The user's recording showed Mouse/Touch and QAT placement snapping in Showcase.
The first native checks exercised property setters directly and did not establish
playback through those menus. Two distinct causes were reproduced:

- The density selector synchronized its icon both inside `DensityChanged` and
  after the command returned. `RibbonItem.Icon` intentionally invalidates layout
  on assignment, even for the same borrowed object, so the second synchronization
  immediately settled the new animation. Showcase now assigns only when the icon
  identity changes. Checked state and caption still synchronize after rejected
  input; the library's icon invalidation contract is unchanged.
- The QAT context placement command applies a customization transaction with
  batched updates, which bypassed the individual position setter's motion wrapper.
  The placement command now wraps the whole existing transaction in one library
  geometry transition. It preserves validation, rollback, QAT membership/source
  ownership and `CustomizationApplied`. Other customization batches stay immediate.

Both regressions failed before correction and passed 2/2 in
`tests/RibbonKit.WinForms.Tests/TestResults/m6a-menu-routing-fixed.trx`.
Two relevant existing context/source/input transaction checks passed in
`m6a-menu-customization-regressions.trx`. Its existing focus-sensitive popup
preservation case failed after the combined UI interactions, then passed alone in
a fresh process (`m6a-context-popup-isolated.trx`) without a runtime change. This
is an isolated retry, not a clean three-case aggregate. Previous passed scopes
were retained. The normal Debug native solution build passed without warnings
or errors, including the corrected Showcase.

Public-only native pointer checks passed for both renderers at actual 120 DPI
and Showcase's Subtle timing in `artifacts/m6a-menu-motion-office/` and
`artifacts/m6a-menu-motion-neutral/`. They click the QAT density chooser and the
QAT context placement action in both directions, assert that playback remains
active after selection, observe 10–13 transition paints before library completion,
and retain customization notifications, membership and source ownership. Actual
transition frames were inspected. This is menu-route evidence, not new performance
or human visual acceptance. The corrected Showcase is ready for human review.

Use `--geometry-menu-motion-checks` / `--geometry-menu-motion-checks-flat` on the
native host, or `--validate-geometry-menu-motion` /
`--validate-geometry-menu-motion-flat` on the package consumer, with an evidence
directory. These selectors run only the new menu routes, without repeating the
earlier direct-setter/timing checks.

## Moving tab marker and tab switching — 2026-10-29

The user's second recording exposed a stationary departure underline during QAT
movement. Each captured header contained its own marker, so header crossfading
could leave the old marker until the handoff. Endpoint capture now suppresses
tab indicators and measures the active marker separately. Each frame paints one
indicator at the interpolated rectangle and tab viewport clip. This follows both
horizontal QAT relocation and vertical density changes, without measuring text
or rebuilding layout on the timer. Office's unselected hover/pressed/focus marker
uses `Theme.SecondaryText` grey; an active tab keeps Accent or its contextual tint.

The next bounded animation is expanded tab selection in Classic and Simplified.
It uses the same 130-ms Subtle timing, Expressive multiplier, system policy,
opacity hold and interruption cleanup, with natural-size body crossfades and the
moving marker. Real selection/editor layout commits once. Shared pointer activation
focuses the tab before changing selection, preventing its focus restoration from
settling the newly started animation. Keyboard navigation uses the same setter.
None, minimized selection, disabled/modal host presentation and unsafe native
contexts stay immediate. Native editor handles and source ownership are retained.

Two additive renderer hooks were proposed before implementation:
`GetTabIndicatorBounds` measures the indicator from header bounds, and
`DrawTabIndicator` paints its measured bounds. The neutral renderer defaults to
no separate indicator; Office supplies its existing rounded geometry. Live tab
painting and motion use the same hooks. Another consumer can override both without
Showcase helpers; XML documentation and the unshipped API baseline include them.

Focused evidence:

- `m6a-tab-indicator-first.trx` passed 17/17: affected geometry/capture/lifecycle
  checks, a single interpolated marker with no departure-color pixels, shared
  tab activation and editor reuse, None/system/minimized fallbacks, Office hover
  versus selected colors, contextual tint retention, and existing four-scale
  odd/even marker symmetry. No full aggregate or WPF run was performed.
- A final direct no-selection cleanup guard separately passed the affected tab
  lifecycle case in `m6a-tab-null-selection.trx`: assigning null while playing
  settles immediately instead of retaining the previous indicator. Explicit
  pointer/keyboard animation takes the unchanged motion branch. Final normal
  Debug native solution build passed with zero warnings/errors.
- Public-only pointer and keyboard tab checks passed at actual 120 DPI, Subtle
  timing, both directions and both layouts in
  `artifacts/m6a-tabs-office-repair/` and `artifacts/m6a-tabs-neutral/`. The Office
  pass checks the marker moving before completion and actual grey/blue pixels
  while hovering another tab. The neutral pass exercises body crossfades with its
  unchanged indicator-free style. Native editor handles/ownership and None
  keyboard selection are checked. Actual intermediate and hover frames were inspected.
- The initial Office run (`artifacts/m6a-tabs-office/`) missed the active phase
  at the first Simplified pointer assertion after finishing Classic. A fresh run
  with added state diagnostics passed without another runtime change. The initial
  failure remains recorded; these short native checks do not close performance
  acceptance or establish an interruption-free presentation trace.
- Office's menu-route pass was repeated for the changed marker layer in
  `artifacts/m6a-indicator-menu-office/`: Mouse/Touch and QAT relocation in both
  directions keep multiple distinct marker rectangles during playback and retain
  the earlier transaction/source checks. Previously passed unrelated scopes were retained.

Use `--tab-motion-checks` / `--tab-motion-checks-flat` on the native test host, or
`--validate-tab-motion` / `--validate-tab-motion-flat` on the package consumer,
with an evidence directory. The corrected normal Debug Showcase supports the same
tab clicks/arrow-key navigation. Human review, physical mixed-monitor/high-refresh
and broader contextual/overflow motion gates remain pending.

## View controls and tab-content style — 2026-10-29

The user requested a simpler Showcase View tab and an alternative to tab-content
fading. The former Merging tab now displays **View** with KeyTip V; its stable
`tab.merging` identity remains unchanged. The obsolete QAT draft group, stored
preview state and demo handlers are removed. Customization still uses its existing
context menu/options pages. The optional capture-only customization exercise uses
a local session rather than restoring the removed sample UI/state.

Chart and Messages each have one dropdown with their original three actions.
**Ribbon animation** contains None/Subtle/Expressive choices plus a **Tab content**
menu for Fade/Slide down, with authored inline choices in Simplified mode. The
original SVG set supplies distinct new menu/animation artwork; artwork stays owned
by the sample. Changing these controls requires no private runtime helpers.

The additive `RibbonTabContentAnimation` enum and `TabContentAnimation` property
were proposed before implementation and have XML/API-baseline coverage. Fade is
the compatibility default. SlideDown applies only when the active tab changes:
the arriving body is opaque and moves down 8 logical pixels for Subtle, 12 for
Expressive, within the existing body clip. Its text/icons remain natural size and
the departure body is not retained. Header/marker animation stays independent;
Classic/Simplified, density and QAT geometry keep their prior presentation.
Changing the style settles an active transition. None and system-disabled motion
still commit immediately. Hosts own this appearance preference; profiles exclude it.

Focused verification:

- Geometry/clip checks passed 2/2 in `m6a-tab-slide-clip.trx`; style enum validation,
  active cleanup and None behavior passed in the earlier `m6a-tab-slide.trx`.
  That initial run's Expressive assertion incorrectly required a full-height icon
  even where its arriving top lay outside the viewport; the assertion was corrected
  to allow clipping while retaining width, opaque pixels and full size once visible.
- Public-only SlideDown pointer/keyboard checks passed for both renderers at actual
  120 DPI, both layouts and directions, in `artifacts/m6a-slide-office/` and
  `artifacts/m6a-slide-neutral/`. They retain marker motion, actual Office hover
  colors, editor HWND/ownership and immediate None checks. Frames were inspected.
- Showcase's `--capture-view` run passed at actual 120 DPI in
  `artifacts/m6a-view-showcase/`. Mouse/Touch/Simplified View layouts and the three
  dropdowns were inspected. All three level choices, Slide down selection and all
  message/chart actions were exercised. Native solution Debug build passed with
  zero warnings/errors. Previous passed scopes were retained; no aggregate,
  WPF, Release, actual VS or fresh-package campaign was run.

The public consumer/native host adds `--validate-tab-slide-motion` /
`--tab-slide-motion-checks` (and their `-flat` variants) for this scope only.
New View controls and the SlideDown presentation await human review; existing
performance, host synchronization and physical display gates remain open.

## Contextual and merged header arrivals — 2026-10-29

User review: the View controls and SlideDown changes look good at the reviewed
scope. The requested next step reuses that style for newly appearing headers;
this new arrival behavior awaits human review.

`TabContentAnimation` now also controls headers exposed by `RibbonTab.Visible`,
live `Tabs.Add`/insertion and merging a visible source. A merged source becoming
visible uses the same route. No extra public setting or appearance coordinator
is introduced. None, disabled system motion, startup, update batches and unsafe
presentation retain immediate changes. Hiding/unmerging remains immediate and
keeps the existing selection fallback and retained-source rules.

Endpoint metadata records tab identities/rectangles once. When new visible headers
exist, header captions are composed separately from the captured header backing.
Existing slots take their committed positions; only arrivals fade or slide down
with the existing 8/12-logical-pixel travel and timing. The first attempt grew
header slots and could clip captions until late in the effect. Frame inspection
led to the full-slot approach, keeping labels natural size and complete. The
selected marker remains independently painted through the shared renderer hooks.

Showing/merging and immediately selecting a tab before the first timer tick
retain the original departure and replace the arrival capture. Picture/Chart
commands therefore produce one transition instead of cancelling their header
effect. This also works for programmatic selection on a minimized ribbon without
opening a popup. Later repeated requests retain the prior settle-to-latest policy;
input, resize, removal, policy changes and exceptions still release native holds.
Source models, handlers, bindings and HWND ownership stay with their original owners.

Focused evidence:

- `m6a-header-arrivals.trx` passed 5/5 for both styles: contextual visibility,
  live additions, merged tabs, idempotent merge, immediate selection coalescing,
  source/editor retention, policy/batches/validation failure/resize cleanup and
  header drawing. The refined full-caption compositor separately passed 2/2 in
  `m6a-header-caption.trx`; the extended minimized fallback/selection case passed
  in `m6a-header-minimized.trx`. Earlier passed scopes were retained.
- Relevant merge, contextual-tab, geometry, interruption and tab-selection
  regressions passed 14/14 in `m6a-header-related-regressions.trx`.
- Public-only native pointer checks passed for both renderers at actual 120 DPI
  in `artifacts/m6a-headers-office-final/` and `artifacts/m6a-headers-neutral-final/`.
  They exercise contextual show/select and prefix-ordered merge/select, Fade and
  SlideDown, Classic and Simplified, multiple painted frames, ownership/HWND reuse
  and removal fallback. Actual header/body frames were inspected. The initial
  neutral run did not activate its first pointer target; the fixture's initial
  shown wait was extended to 300 ms and both final runs completed. This is not a
  new compositor/performance acceptance claim.
- Normal Debug native build includes XML/API/design checks. No full aggregate,
  WPF, Release, actual VS, fresh-package or physical mixed-monitor run was made.

Use native `--header-arrival-checks` / `--header-arrival-checks-flat`, or consumer
`--validate-header-arrivals` / `--validate-header-arrivals-flat`, with an evidence
directory. The existing View style control and animation level are sufficient;
new public API signatures were not needed. Human and existing performance gates
remain separate.

## Expressive active tab pill — 2026-10-29

User review found no distinct Expressive effect on the active tab pill. Its
rectangle used the same cubic ease-out as Subtle; only duration differed.
The marker now uses the WPF-style sine BackEase-out term with amplitude 0.35
for horizontal travel. Width follows that curve within its source/destination
range, avoiding an inverted or excessively thin pill when captions differ.
Vertical geometry, clips, opacity and content keep the established bounded curve.
The one shared renderer marker settles at the exact destination, including after
an interruption. No new API, timer or host coordination is introduced.

Focused evidence:

- The new bidirectional Fade/SlideDown drawing cases reproduced the missing
  spring (4/4 failed in `m6a-expressive-pill-repro.trx`) and passed after the repair
  (4/4 in `m6a-expressive-pill.trx`). They compare Subtle and Expressive, exact
  endpoints, overshoot/settling, valid width/thickness, clipping and no stale copy.
- Relevant existing marker, tab activation, header arrival, interruption and
  None/system/startup regressions passed 7/7 in
  `m6a-expressive-pill-regressions.trx`.
- The public-only native consumer passed pointer/keyboard switching in both
  directions, Classic and Simplified, at actual 120 DPI. Its Office marker
  overshot by 5–6 pixels before settling; final pill pixels, grey hover,
  immediate None behavior and editor/source retention passed. Frames under
  `artifacts/m6a-expressive-pill/` were inspected. Reproduce with native
  `--expressive-pill-checks` or consumer `--validate-expressive-pill` and an
  evidence directory. Neutral has no separate pill, so it was not rerun.
- Normal Debug native solution build passed with zero warnings/errors.
  No full aggregate, WPF, Release, fresh-package, actual VS or physical
  mixed-monitor checks were run. Human motion and performance acceptance remain
  open; these bounded checks establish the spring path and cleanup.

## Minimize and restore — 2026-10-29

The user selected minimize/restore as the next bounded motion slice, including
QAT/message joins. Existing `AnimationLevel` now covers `IsMinimized` and its
utility-button, Ctrl+F1 and tab-double-click routes. No new public API is needed.
Subtle uses 180 ms and Expressive 252 ms. Real height, editor parking, focus and
the bindable state commit once; library-owned presentation finishes independently.
Application document Controls still reflow immediately, as in the earlier pass.

The body stays opaque at natural size and is revealed/retracted through its
changing lower edge. A BelowRibbon QAT and intact message rows travel with that
edge. Office's body/QAT frame joins shrink to a standalone minimized QAT card;
TabRow or hidden QAT leaves the message stack standalone when minimized. Message
frames/shadows are painted at their moving bounds before the body, retaining the
existing connection policy. Neutral retains its separate flat stack. Native
editor HWNDs and command/message ownership are retained.

Frame review caught the old/new minimize glyphs forming an X when crossfaded.
The header now uses its committed chevron direction immediately, including the
zero-progress frame, while the body continues moving. Existing selected-marker
painting stays separate. Modal entry/exit deliberately keeps immediate geometry;
startup, batches, None/system-disabled motion, unsafe presentation and interruption
retain the established immediate/settle rules. Temporary minimized-tab popups
settle a running transition before opening.

Focused evidence:

- The first `m6a-minimize-motion.trx` run passed 10/13; three lifecycle assertions
  incorrectly assumed a margin above neutral's BelowRibbon QAT and populated
  Classic group slots in Simplified mode. These fixture assumptions were repaired;
  all four lifecycle cases passed in `m6a-minimize-lifecycle.trx`. They cover both
  renderers/layouts, both placements, open/closed messages, Compact/Touch and
  Subtle/Expressive, one notification per change and retained HWND/source/value.
  The initial policy/interrupt/popup/modal/startup case also passed and was retained.
- Final drawing/utility checks passed 10/10 in `m6a-minimize-drawing-final.trx`:
  both directions/renderers/layouts/placements, opaque natural-size body clipping,
  intact QAT/message pixels, exact endpoints, hidden-QAT messages and chevron
  direction at zero/intermediate/final progress. The final drawing rerun followed
  the chevron and moving-shadow corrections, not an unchanged repeat.
- Related minimize, message, modal, active-marker and tab-selection cases plus the
  initial utility checks passed 21/21 in `m6a-minimize-related.trx`. Passed scopes
  were retained; these files are separate focused results, not one full-suite run.
- Public-only native checks passed for Office2024 and neutral at actual 120 DPI
  in `artifacts/m6a-minimize-office-final/` and
  `artifacts/m6a-minimize-neutral-final/`. They exercise utility input in both
  directions/layouts/placements, Compact/Touch and Subtle/Expressive, multiple
  moving QAT/message frames, exact restored height, once-only notifications,
  source/HWND/foreground retention, Ctrl+F1, double-click restoration from a
  temporary body popup and immediate None behavior. Final native frames were
  inspected, including joined/standalone message and QAT edges.
- Normal Debug native solution build passed with zero warnings/errors; XML/API
  and design compilation are included. No full aggregate, WPF, Release,
  fresh-package, actual VS or physical mixed-monitor pass was run. This adds
  functional/native evidence, not performance or human motion acceptance.

Use native `--minimize-motion-checks` / `--minimize-motion-checks-flat`, or consumer
`--validate-minimize-motion` / `--validate-minimize-motion-flat`, with an evidence
directory. Showcase's existing View animation choices apply to all three minimize
gestures. This presentation awaits user review; optional host synchronization and
the earlier performance/display gates remain open.
