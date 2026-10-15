# Ribbon and host content animation synchronization

> Proposed follow-on plan, requested 2026-10-14. Proceed after the
> [feature availability](16-RIBBON-FEATURE-AVAILABILITY-PLAN.md) and
> [Toolbox item-definition](17-TOOLBOX-ITEM-DEFINITION-PLAN.md) plans.
> This document authorizes no runtime API implementation or validation run.

## Goal

Let an application animate its content area alongside ribbon geometry changes,
with both presentations starting together and following the same motion policy.
Cover Classic/Simplified, Compact/Touch and QAT placement changes through one
optional shared contract. In particular, moving the QAT into or out of the area
below the ribbon introduces/removes a panel and changes the content's available
space; it must participate alongside the ribbon body.

Host handlers may be anonymous functions. Application content, its animator and
its lifecycle remain owned by the application. The intended result is a continuous
moving boundary between the ribbon/QAT area and the host content.

## Current boundary

- Mode changes use `Ribbon.LayoutMode`/`SetLayoutMode`; built-in toggles use the
  same dependency property. There is no callback-taking toggle method or public
  notification of the layout animation's exact start/completion.
- `Ribbon.LayoutMotion.cs` captures the old ribbon presentation synchronously,
  commits real layout, and starts its vector presentation at ContextIdle after
  simplified copies, borrowed popup content and density measurements settle.
  Below-ribbon QAT motion participates in that transition; arbitrary host content
  does not. QuickAccessMove policy is defined, but current QAT relocation cancels
  layout motion and reparents items without a playback call site. H1 must review
  relocation presentation as well as the shared notifications.
- In the user's RTL Lab recording, the DockPanel content moves immediately to
  its final bounds while the ribbon still presents its previous drawing. The
  ribbon is waiting for its own layout work, not for a host content animator.
- `RibbonAnimation` exposes action policy, duration and easing, honoring global,
  per-action and system reduced-motion settings. These helpers alone do not
  synchronize a host animation with the ribbon's deferred start.

## H1 — optional transition contract and API review

Recommend transition events on Ribbon rather than requiring a new toggle wrapper.
Events cover built-in menus, XAML bindings and programmatic changes, including
capability coercion from the preceding availability plan. Retain existing
mode/density/QAT placement properties as the selection APIs.

Proposed lifecycle and names, subject to API review:

| Phase | Host use | Required ordering |
| --- | --- | --- |
| `LayoutTransitionPreparing` | Capture old content bounds/presentation and install a temporary visual hold if needed | Before layout can expose the destination frame |
| `LayoutTransitionStarting` | Start the content animator with the ribbon/QAT timing and easing | After final geometry settles, in the same dispatcher/render phase as the library animation |
| `LayoutTransitionEnded` | Release the host's hold, clocks, transforms and clips | Exactly once per prepared transition, identifying completion, cancellation or skipped animation |

The read-only context should identify the transition, reason/action, old/new
mode, density or QAT position, and previous/final occupied bounds. Include the
effective below-ribbon QAT panel and its clipping/shadow wrapper in the measured
extent, rather than reporting only the ribbon body height. Define a coordinate
space that also supports the detached title-bar QAT host.

Supply the effective motion level, duration and easing used by the participating
library animation. Reuse LayoutModeChange, DensityChange and QuickAccessMove
policies, without implicitly changing their accepted cadence. Agree on a common
start-time/clock mechanism; starting a host timer from SizeChanged is insufficient.

```csharp
// Proposed API; these events and context members do not exist yet.
ribbon.LayoutTransitionPreparing += (_, transition) =>
    contentAnimator.Prepare(transition);
ribbon.LayoutTransitionStarting += (_, transition) =>
    contentAnimator.Start(transition);
ribbon.LayoutTransitionEnded += (_, transition) =>
    contentAnimator.Settle(transition);

ribbon.LayoutMode = RibbonLayoutMode.Simplified;
// Existing QAT placement changes would use the same event lifecycle.
```

`contentAnimator` is application code, not a proposed RibbonKit class. The
notifications do not await host work or make the host responsible for completing
the library transition. Do not add a callback-taking method alongside events
without a concrete uncovered use case.

Review final API names/shape, callback reentrancy, zero-duration behavior and
fallback semantics before implementation. A prepared transition must end even
if it is superseded or cannot animate before Starting fires. Unchanged occupied
bounds, including QAT moves that do not change content space, let the host skip
its content animation without suppressing ordinary QAT relocation.

## H2 — shared lifecycle and bounded host demonstration

- Implement notifications in RibbonKit's layout/density and QAT relocation
  paths. With no subscribers, preserve accepted behavior. Avoid duplicate events
  when a mode/density change also moves the below-ribbon QAT, and retain existing
  item ownership/reparenting, placement bindings and title-bar host integration.
- Keep actual layout committed once. Animate the host presentation through
  translation and clipping, retaining its previous drawing where reflow needs
  it. Avoid per-frame Height/Margin animation, repeated ribbon measurement and
  text/icon stretching.
- Hold the previous ribbon/QAT and host presentations until the shared start.
  Guard generation changes after callbacks so rapid or reentrant changes cannot
  start obsolete motion. A stale completion must not cancel a newer transition.
- Disabled motion, system reduced motion and policy changes during playback
  settle participating presentations immediately. No fixed delay or wait for a
  zero-duration animation clock is needed.
- Relay existing interruption/lifecycle handling to the host, including input,
  unload, template/geometry/DPI and policy changes. Release temporary state on
  cancellation or window close; preserve host transforms, clips, opacity and
  bindings. Review popup/custom-template fallbacks and interrupted QAT moves.
- Keep temporary drawings inert. Real controls retain committed bounds, focus,
  commands, hit testing, keyboard navigation and UI Automation. Preserve popup
  dismissal, borrowed-content return and final anchor positioning; neither focus
  transfer nor live popup placement should wait for the host animation.
- Demonstrate the contract in RTL Lab with a host-owned content animator. Verify
  the same public integration in a consumer without Showcase resources/helpers.
  Writer, MDI and other applications opt in separately. Record later Writer glue
  in its consumer friction log.

Tab switching, popup-opening animation, general window resizing and a generic
application/document animation framework are outside this first slice.

## H3 — focused verification and live acceptance

Verify lifecycle ordering, exactly-once termination, shared start/timing, no
destination-to-start flash, and no-subscriber compatibility. Exercise both mode
directions, Compact/Touch, and QAT moves into/out of BelowRibbon through built-in
selectors, bindings and code. Cover other QAT placements, hidden QAT, unchanged
content bounds and overlapping mode/density/placement requests.

Include rapid reversals, callback reentrancy, input interruption, unload,
global/per-action disabled motion, reduced motion and popup/custom-template
fallback. Check binding/transform/clip preservation and QAT reparenting/focus.
Use independent-consumer geometry/presentation checks across affected themes,
LTR/RTL, QAT placements and representative DPI scales.

Separately review the moving boundary in normal Debug output, keyboard focus,
popup anchors, native interaction, genuine mixed-monitor DPI and OS reduced
motion. Automated or simulated coverage does not establish those native gates.

Document intentional public additions with XML comments and the additive API
baseline; run applicable API/build/package checks. Select affected runtime,
visual and portability checks. Retain passed evidence unless changes or failures
justify repeating it; creating this plan implies no full-suite rerun.

## Sequencing and acceptance

The planned order is feature availability, Toolbox definitions, then this host
animation integration. Review H1's public contract before H2; implementation
requires a later user request.

Existing simplified acceptance, assembled end-of-feature validation, the two
residual Crystal QAT snapshot comparisons and specific native limits remain in
the [active simplified plan](15-SIMPLIFIED-RIBBON-PLAN.md#end-of-feature-validation-and-repairs--2026-10-14).
This proposal does not close outstanding gates, schedule, publish or commit work.
