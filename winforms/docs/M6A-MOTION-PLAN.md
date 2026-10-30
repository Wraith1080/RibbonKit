# M6A native geometry motion

Status: **requested motion implemented; user visual review positive at the reviewed scope, performance/display gates pending**, 2026-10-29.

User review, 2026-10-29: all animations are good for now. The five subsequent
border, QAT, shadow, temporary-body and customization-column corrections are
tracked in the [pre-M6B review slice](M4-STATUS.md#pre-m6b-review-fixes--2026-10-29).
This accepts the reviewed animation presentation, separately from performance,
physical display/DPI and other broader gates.

The user selected Classic/Simplified, Mouse/Touch and QAT movement as the first
visible motion. M6A remains ahead of M6B window integration/title-bar QAT.
The user reserved a small fixing slice before M6B on 2026-10-29; its specific
items follow the current motion pass and do not start window integration.

The later RibbonForm frame/width and live Backstage fade corrections are recorded
in [M6B motion review corrections](M6B-WINDOW-INTEGRATION.md#motion-review-corrections--2026-10-29).
Backstage's earlier navigation/page fades are superseded by the
[whole-surface and caption slide](M4I2-APPEARANCE-INVENTORY.md#whole-surface-backstage-and-caption-slide--2026-10-30).

## Current contract

- `RibbonControl.AnimationLevel` accepts `None`, `Subtle` or `Expressive`. The
  library defaults to None. Showcase opts into Subtle. Applications own this
  appearance preference; customization JSON does not persist it.
- Single changes to `LayoutMode`, `Density`, `QuickAccessPosition`, `IsMinimized`
  and expanded `SelectedTab` animate. Command/gallery and minimized-tab popup
  opening uses the separate inner-content effect below. Command-menu closing remains
  immediate; outside-click dismissal of a temporary tab body is described in the review slice.
  Geometry, command ownership and native editor bounds commit once. An inert
  drawing plane presents natural-size endpoint drawings, interpolated panel
  geometry and actual QAT strip travel. Text/icons are not stretched with panels.
  `IsAnimating` reports geometry, group, message-panel, popup and Backstage motion,
  not an uncommitted model transaction.
- Backstage translates its one live native surface over 15% of its width: it
  starts 85% visible on opening and hides after moving 15% left on closing,
  over 220/308 ms, with a smooth opacity fade at both ends. Its integrated caption
  tint tracks the same edge and opacity. Only effective TitleBar QAT presentation
  and hit targets are suppressed until closing completes. TabRow/BelowRibbon,
  including TitleBar fallback to TabRow, retain their normal presentation.
  Authored toolbar visibility, placement and commands are preserved. Page switches are
  immediate. Native page redirection fades with movement and stays opaque after
  opening until hidden. Transparent navigation/frames fade their premultiplied
  paint alongside the page layer. No Backstage page capture is used.
  Solid opening holds the previously composed document drawing beneath the fade
  until Backstage is opaque, avoiding native RichTextBox sibling-clip redraw.
  This temporary passive drawing leaves the actual editor and its state untouched.
- Subtle requests 130 ms for layout/density and 150 ms for QAT. Expressive uses
  1.4 times those durations and adds a gentle spring to the active tab indicator.
  Minimize/restore requests 180/252 ms for Subtle/Expressive.
  Message show/dismiss requests 160/224 ms.
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
4. Further motion requires its own bounded scope and relevant checks.
   Command/gallery, message-panel and Backstage navigation opening are implemented below;
   popups settle active geometry before opening. Command-menu closing and batch
   customization remain immediate. Backstage/page fading and validated closing
   are described in the latest pass below.

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

## Anchored command and gallery opening — 2026-10-29

The next bounded pass adds an inner-content entrance to command menus, galleries,
QAT/group flyouts and temporary minimized-tab bodies. Existing `AnimationLevel`
controls it; no public signatures are added. `IsAnimating` now includes this
opening presentation. `TabContentAnimation` still controls only tab body/header
arrival, independently of popup opening. Closing, native ComboBox choices and
Backstage remain immediate. None, system-disabled motion, update batches, authored
Form transparency, unavailable native support and unsafe surface bounds use the
immediate path. User review, 2026-10-29: popup opening looks good at the reviewed
scope. Existing performance/display and physical-input gates remain separate.

The popup's actual anchor, window bounds, rounded frame/clip and shadow are fixed.
Only a natural-size inner snapshot fades and slides down, using cubic ease-out:
Subtle requests 130 ms and 8 logical pixels; Expressive 182 ms and 12. The live
surface is put into the existing alpha-1 drawing hold before Show, avoiding a
destination-content flash while retaining native focus, input and editor HWNDs.
Endpoint capture reuses the normal HWND printing/native-editor clipping path.
One inert, nonactivating owned drawing plane reuses its DIB/DC until completion;
capture and layout do not run per frame. All drawings remain in the library.

Pointer movement into the popup, button/key/wheel input, source mutation, resize,
policy changes, close/disposal and activation loss settle/release the presentation.
Input continues to the live controls. Hover/preview is suppressed while an inert
opening is being shown; genuine pointer movement settles it before normal hover.
Opening a submenu settles its parent, keeping the parent's live popup open.
The existing geometry hold and capture were factored into shared internal helpers;
the accepted geometry policy and public source/ownership models are retained.

Focused evidence and limits:

- The two deterministic menu/gallery drawing cases and nested/disposal case passed
  in `m6a-popup-motion.trx`. Its foreground-sensitive playback assertions failed
  when an unrelated foreground HWND caused the existing activation guard to close
  the popup before a timer frame. Diagnostics confirmed a foreground outside the
  host/popup/drawing family. Autonomous playback/focus was therefore moved to the
  dedicated native host; STA checks now explicitly settle rather than require
  foreground ownership while pumping. The settled lifetime/input/policy cases
  passed 5/5 in `m6a-popup-settle.trx`.
- The shared-hold callback/policy case passed 1/1 in
  `m6a-popup-callback-policy.trx`; batch/transparency and updated minimized-popup
  expectations passed 2/2 in `m6a-popup-batch-minimized.trx`. A separate experiment
  throwing from an app icon's native paint callback aborted its test host; it was
  removed as an unsuitable managed-exception assertion. Recovery from arbitrary
  application painting failures is not established by this slice.
- Relevant menu/gallery/QAT/options and shared geometry/editor-capture/input/
  transparency regressions passed 48/48 in `m6a-popup-regressions.trx`. Passed
  scopes were retained; these are assembled focused results, not a full-suite run.
- Office's eight body/QAT menu/gallery Compact/Touch playback scopes passed in
  `artifacts/m6a-popup-office/` before a later fixture field-coordinate lookup
  failed. Its remaining native interaction scopes passed separately in
  `artifacts/m6a-popup-office-input-final/`. Neutral passed the complete native
  fixture in `artifacts/m6a-popup-neutral-final/`. All report actual 120 DPI.
  Coverage includes stable frame/corner pixels, visible intermediate content,
  autonomous completion, HWND/text/source retention, no playback command/selection
  commits, exact-once pointer actions, submenu Escape, native ComboBox selection,
  QAT groups, minimized bodies, real deactivation and cancellable host Alt+F4.
  Actual first/intermediate/final frames were inspected for both renderers.
- The native fixture now uses the editor's own coordinates and seeds its selected
  index, rather than treating child painter bounds as popup coordinates or assuming
  `Value` selected a choice. Pointer movement is dispatched for 20 ms before the
  button injection. Earlier zero-delay injection and one fresh first-click attempt
  missed their targets; those failed attempts remain diagnostics, not a proven
  runtime cause or a zero-delay/physical-input acceptance claim. Office evidence
  is assembled from the passed playback prefix and corrected interaction run.
- Normal Debug native build includes XML/API and design compilation. No aggregate,
  WPF, Release, fresh-package, actual VS, physical mixed-monitor/touch/IME or
  performance/resource-plateau campaign was run. Existing human/display/performance
  gates remain separate; the unit system-policy override never changes the OS setting.

Reproduce with native `--popup-motion-checks` / `--popup-motion-checks-flat` or
consumer `--validate-popup-motion` / `--validate-popup-motion-flat`, followed by an
evidence directory. The `*-motion-input*` variants run only the remaining input/
lifetime scopes, allowing passed playback scopes to be retained. Showcase's
existing View animation level is sufficient to review the effect.

## Live-page Backstage navigation opening — 2026-10-29

The next bounded animation applies the existing `AnimationLevel` to library-owned
Backstage navigation. Subtle fades/slides it from the left over 220 ms and 28 logical
pixels with cubic ease-out; Expressive uses 308 ms, 50 logical pixels and quintic
ease-out. No public signatures or host acknowledgement are added. `IsAnimating`
includes the navigation presentation. This new visual awaits user review.

Application pages stay live at their final bounds. Their controls, bindings, values,
native handles and persistent library parent are not moved or hidden by the effect.
The library holds only Back and the navigation panel before showing Backstage,
captures those library controls once, and uses the existing inert native drawing
plane. Background painting is direct; application content is excluded from capture.
The Backstage handle/layout is established before preparing the first entrance.

Input settles before normal page/native-editor/KeyTip interaction. Closing remains
immediate, with the existing validation and Closing cancellation rules. Resize,
source/appearance changes, deactivation, handle loss and disposal release drawings
and holds. None, disabled system motion, update batches, authored Form transparency,
unsafe bounds or unavailable native presentation keep the immediate path. Existing
selection and minimized state are retained; opening also settles obsolete ribbon
geometry drawings. Hosts continue to own application page presentation.

Focused evidence:

- First-pass tests exposed the need to create/layout the hidden Backstage handle
  before preparing its rail. They also caught page print requests from the first
  whole-container capture. That path was replaced with direct background paint and
  explicit library-child capture. Normal Windows TextBox parent-background print
  requests during BringToFront/layer-window presentation are distinct from capture;
  stack-qualified probes now assert that capture never reaches the borrowed page.
- Four renderer/level ownership cases passed in `m6a-backstage-ownership.trx`.
  They assert navigation-only holds, no page capture, live native controls,
  unchanged page bounds/parent/handles/value and reuse on reopening. The two
  deterministic drawing cases and interruption/cancellation policy case passed in
  `m6a-backstage-capture-final.trx`; its earlier one remaining ownership assertion
  misclassified ordinary Windows repainting and is superseded by the qualified
  probe run. The drawing checks cover horizontal travel, fade, natural size and
  exact resting artwork. Nullable Auto width arithmetic in the mutation fixture
  was corrected to assign an actual width change.
- Related Backstage, popup-settle, native-editor capture and callback cleanup
  regressions plus the new Backstage disposal/callback case passed 14/14 in
  `m6a-backstage-related.trx`. These are separate focused results; no full-suite
  result is implied. Previously passed popup scopes were retained.
- Public-only native checks passed for Office2024 and neutral at actual 120 DPI in
  `artifacts/m6a-backstage-office/` and `artifacts/m6a-backstage-neutral/`.
  Subtle/Expressive and Compact/Touch show intermediate navigation while page
  screenshot pixels remain stationary. Native HWND/parent/binding, selected tab
  and minimized state survive opening. The checks cover autonomous completion,
  nonactivation, Back, native editing/binding, page switch, invalid-page close
  blocking, valid close, File/Backstage KeyTips, resize and None. Actual first,
  intermediate and final captures were inspected.
- Normal Debug native solution build includes XML/API and design compilation.
  No aggregate, WPF, Release, fresh-package, actual VS, physical mixed-monitor/
  touch/IME or performance/resource-plateau campaign was run. System-policy unit
  overrides do not alter the OS setting. Broader existing gates remain open.

Use native `--backstage-motion-checks` / `--backstage-motion-checks-flat`, or consumer
`--validate-backstage-motion` / `--validate-backstage-motion-flat`, and an evidence
directory. Showcase's existing View animation level controls this entrance too.

## Live application-page fades and Backstage closing — 2026-10-29

Later exception: [transparent material closing](M4I2-APPEARANCE-INVENTORY.md#backdrop-state-and-closing-review--2026-10-30)
now fades the already-visible composed panel as one image, including its page
background. It does not print/reparent native controls. The historical live-page
path below remains in use for opening/page selection and Solid closing.

The user requested animation for the application-page area and Backstage closing.
The existing level now fades live pages on opening/selection and fades Backstage
out for Back, Escape and visibility requests. Controls are not copied to bitmaps,
reparented or individually styled for the effect. Navigation retains its existing
entrance. The native opacity primitive is shared, with the same 220/308-ms cadence,
system policy, None fallback and library-owned completion. The user reviewed the
Backstage/page opening and closing presentation as looking good on 2026-10-29;
broader performance/display gates remain separate.

An opaque library background sits behind Backstage during opening/page changes.
The top-level Backstage child provides the live fade; a stable navigation drawing
keeps the rail steady during page selection. The first direct nested-page-host
attempt passed coarse pixel-change checks but showed an empty host and underlying
ribbon in actual frames. It was replaced after inspection. The current path shows
native page text at intermediate opacity over the opaque Backstage background.
Closing removes that background while fading the complete live surface toward the
underlying application. It hides the surface before restoring native opacity,
avoiding a final opaque flash. The background is not a keyboard stop.

Validation and Closing cancellation run before a close starts. With animation on,
`IsBackstageOpen` stays true until that close completes; visibility/Closed events
and focus restoration follow once. Reopening cancels a pending close and snaps to
the live open state. Policy/unsafe changes can finish it immediately. New input
targeting the outgoing page finishes closing and is consumed to prevent stale
control actions; host Alt+F4 still works. Command, File-mode and modal transitions
close synchronously before application work. None keeps the original immediate
behavior. Appearance remains host-owned and is not added to customization JSON.

Focused evidence:

- Existing Backstage/navigation checks passed 14/14 in
  `m6a-backstage-live-fade-first.trx`. Three new native-parent, close/reopen/input,
  synchronous-command and throwing-callback cleanup cases passed in
  `m6a-backstage-page-closing.trx`. The final top-level/background approach passed
  the new cases and legacy Backstage cases 9/9 in `m6a-backstage-live-surface.trx`.
- Related modal, popup and geometry/input/callback checks passed 22/22 in
  `m6a-backstage-fade-related.trx`. These are retained separate focused results,
  not an aggregate suite claim. Failed nested-page captures remain diagnostics;
  passing coarse frame-change checks alone did not establish visual correctness.
- Public-only native checks passed at actual 120 DPI in
  `artifacts/m6a-backstage-live-office/` and `artifacts/m6a-backstage-live-neutral/`.
  Both levels/densities cover opening, page switch, autonomous closing, retained
  HWND/parent/value/binding, native input, validation, KeyTips, resize, synchronous
  commands, None and Alt+F4 during closing. Actual opening/page-switch/closing/end
  frames were inspected. Neutral additionally asserts that intermediate page
  pixels contain faded native text, rather than merely differing from the end.
- Normal Debug native build includes XML/API and design compilation. No full
  aggregate, WPF, Release, fresh-package, actual VS, physical mixed-monitor/touch/
  IME or performance/resource-plateau campaign was run. The native runs prompted
  by the compositing repair do not replace those broader pending gates.

The existing `--backstage-motion-checks` / `--backstage-motion-checks-flat` native
commands and consumer `--validate-backstage-motion` variants cover this extension.
The public visibility timing is documented in the API and README; no new public
signature or required host animation coordinator was introduced.

## Message-panel show and dismissal — 2026-10-29

The existing animation level now covers adding/opening/reopening messages,
Dismiss, user Close, removal/Clear and message/bar visibility. Subtle requests
160 ms; Expressive requests 224 ms. The shared geometry coordinator reveals or
retracts natural-size row content through the changing panel height with a short
fade for arriving/departing rows. Surviving rows move without a glyph crossfade.
State, ownership, bindings, ribbon height and callbacks commit immediately.
Application content still reflows immediately; host synchronization remains a
separate proposal. This new message presentation awaits user review.

Message chrome is repainted at each measured intermediate frame rather than
copied from an endpoint's rounded corners. The body/BelowRibbon QAT keeps a square
join while the message panel is visible, then regains its lower rounded edge.
The message panel's lower edge stays unconnected, including during minimize
motion; frame inspection caught and corrected an inherited bottom-join flag.
A minimized TabRow or hidden QAT keeps the messages standalone. Stacked additions
and removals use packed row positions. Same-direction requests before the first
timer tick coalesce; opposite or later requests snap to the newest committed state.

The Close target includes focus repair inside the transaction so moving focus
away from a removed target does not immediately settle its own dismissal drawing.
Dismissal callbacks remain exact-once and synchronous. Throwing callbacks release
the drawing/opacity hold while retaining the committed state. None, system policy,
startup, update batches and the existing unsafe-context fallbacks remain immediate.
Disposal, caption/content edits and collection replacement also remain immediate;
appearance stays outside customization JSON. No public signatures were added.

Focused evidence:

- New message lifecycle/policy checks first passed 6/6; their four drawing cases
  passed after the expected positions used the same screen-coordinate rounding
  as presentation. Existing message/minimize/geometry checks and those drawings
  passed 51/52 in `m6a-message-drawing-related.trx`. The remaining header fixture
  expected a header entrance during a still-running minimize request, contrary to
  the existing snap contract. It now settles minimize before testing header
  arrival; the corrected header and join checks passed 5/5 in
  `m6a-message-joins-header.trx`. No runtime header behavior changed.
- Wrapping programmatic Dismiss also keeps its callback inside cleanup. Its new
  throwing-callback case and the six affected lifecycle/policy cases passed 7/7
  in `m6a-message-dismiss-callback.trx`. The message and affected minimize drawing
  cases passed 14/14 in `m6a-message-bottom-corners.trx`, checking exposed message
  bottoms, connected/standalone frames, row clipping, natural icon widths and
  survivor positions. These are assembled focused results, not a full-suite run.
- Normal Debug native solution build includes XML/API and design compilation.
  The public-only native consumer covers both renderers, both placements,
  Compact/Touch, expanded/minimized and hidden-QAT cases. Captures wait for an
  actual intermediate frame height and flush the desktop compositor before
  recording, rather than assuming a fixed delay has reached the new frame.
  Both final runs passed at actual 120 DPI in
  `artifacts/m6a-message-office-corners/` and
  `artifacts/m6a-message-neutral-corners/`: arrival, Close/Reopen/Clear, intermediate
  heights, autonomous completion, once-only actions/dismissal, native document
  focus/selection/handle/data retention, exact final height, None and resize.
  Actual first/intermediate/final frames were inspected, including standalone
  upper/lower corners and attached message bottoms. Earlier `*-final` captures
  precede the bottom-join correction and remain diagnostics.
- No full aggregate, WPF, Release, fresh-package, actual VS, physical mixed-monitor/
  touch/IME or performance/resource-plateau campaign was run. Autonomous completion
  and screenshots do not establish frame-cadence or human smoothness acceptance.

Use native `--message-motion` / `--message-motion-flat`, or independent consumer
`--validate-message-motion` / `--validate-message-motion-flat`, followed by an
evidence directory. Showcase's existing View → Messages Add/Reopen/Clear menu and
message Close buttons use this behavior through the public model.

## Group collapse and restore — 2026-10-29

Implemented for review, following the user's requested leftward collapse and
rightward restoration. The user's follow-up assigns this to the existing ribbon
body Fade/Slide radio selector (`TabContentAnimation`); no new option is added.
Fade crossfades group drawings in their allocated slot. SlideDown retains vertical
tab/header travel and selects horizontal travel for groups. None and the system
policy still commit immediately. Subtle uses 150 ms / 12 logical-pixel travel;
Expressive uses 210 ms / 20. Changing style or level settles active group motion.

- Classic width reduction/restoration detects collapsed-state changes. Simplified
  width overflow/return detects visible group-run membership changes; a full-group
  popup's temporary medium button and its return use the same path, including in
  a minimized tab body. Interleaved Simplified group runs keep separate identities.
- The model, hit targets and real editor bounds commit once. Only an inert body
  viewport drawing animates; headers, frame, QAT, messages and live popup windows
  retain their own presentation. Text/icons are never stretched. Slide presents
  the arriving group once, avoiding sliced mixtures of old and new captions.
  Fade keeps the departure opaque under the arriving image, preserving the weight
  of identical pixels. Popups remain above the row drawing even if flipped above
  their anchor. No editor cloning, drawing-opacity hold or profile changes are used.
- Further width changes update the arrival endpoint without restarting the clock.
  Layout/capture occurs on the change, never on timer ticks; frames reuse the
  drawing plane's DIB/DC/alpha helper. Input, model/policy/style changes, movement,
  scrolling, hiding, deactivation and disposal release it. Inactive owners, update
  batches, startup, unavailable/oversized bounds or authored Form transparency use
  immediate presentation. A minimized QAT group without a temporary body retains
  its ordinary popup behavior. Native setup failure still commits the action once;
  application callback exceptions release drawings and propagate.

The implementation adds internal scene/controller helpers and records the inherited
protected `SetBoundsCore` override in the unshipped API baseline. Existing public
appearance properties and source/native-control ownership contracts are retained.

Relevant evidence (assembled focused results, not a full aggregate):

- Normal Debug native solution build passed with zero warnings/errors, including
  XML/API and design compilation. Native fixtures use the same Debug outputs.
- Classic lifecycle/editor identity and immediate fallback cases passed in
  `m6a-group-motion-lifecycle.trx` (four passed cases); corrected root/minimized
  borrowing and restoration passed 2/2 in `m6a-group-motion-popup-foreground.trx`.
  `m6a-group-motion-fallback-final.trx` passed 3/3, including a minimized QAT group
  with no temporary body. Simplified width checks passed across the Touch prefix
  in `m6a-group-motion-simplified-width.trx` and Compact isolated retry in
  `m6a-group-motion-simplified-width-compact.trx`.
- `m6a-group-motion-render-regressions.trx` passed 26/26 selected group, Simplified,
  popup-opening and source-restoration checks. After the shared style hookup,
  `m6a-group-content-selector-final.trx` passed 6/6: fixed-position fade blending,
  natural-size opaque slide ink at four DPI-scaled travel distances, style/policy
  interruptions and cleanup. Counts overlap. Unit policy/active-owner overrides
  are controlled checks, separate from actual native foreground ownership.
- Final public-only native runs passed at actual 120 DPI, Compact/Touch, both
  layouts, ordinary and temporary minimized rows, both styles/renderers:
  `artifacts/m6a-groups-slide-office-final/`,
  `artifacts/m6a-groups-slide-neutral-final/`,
  `artifacts/m6a-groups-fade-office-click/` and
  `artifacts/m6a-groups-fade-neutral-final/`. They cover real host resizing and
  continued resizing, full-group/temporary-button pointer actions, autonomous
  completion, original editor HWNDs/current values/checked state and exact profile
  preservation. Intermediate captures were inspected.
- Earlier probes caught recursive immediate fallback and ToolStrip-window owner
  detection, both corrected. Native failures also exposed foreground denial and
  a synthetic press/release pair that omitted MouseUp during focus transfer; the
  fixture now establishes actual foreground ownership and spaces the tap. Its
  continued-resize assertion runs before potentially slow PNG saving. Earlier
  failed/interrupted artifacts remain diagnostic, not acceptance.

Run native `--group-motion-checks` / `--group-motion-checks-flat` for Slide, or
`--group-fade-checks` / `--group-fade-checks-flat` for Fade, followed by an evidence
directory. The public consumer offers corresponding `--validate-group-motion`
and `--validate-group-fade` variants. No full aggregate, WPF, Release/package,
actual VS, physical mixed-monitor/touch/IME or performance campaign was run.
User review, 2026-10-29: positive response to the group motion and shared Fade/Slide
selector, followed by authorization to continue to M6B. Performance, physical
display/DPI and broader native acceptance gates remain pending. M6B is the next
slice; its implementation has not started.
