# Library history: Host layout transitions

Current evidence and remaining acceptance belong to the
[active plan](../../18-HOST-LAYOUT-TRANSITION-PLAN.md).

### 3.259 Ribbon-owned lifecycle and QAT relocation presentation — 2026-10-16

The user selected continuous QAT movement with departure/arrival fallback,
approved Ribbon-owned coordination with optional RibbonWindow presentation
support, and authorized implementation plus animation of the main Showcase
document card. Writer and MDI integration remain separate opt-in work.

The old QuickAccessMove policy had no relocation playback call site. A private
per-Ribbon coordinator now covers effective layout, density, capability coercion,
QAT placement and visibility. Existing selection APIs and bindings remain intact.
The three additive CLR lifecycle events expose immutable state/geometry snapshots
and read-only progress sources backed by child clocks of one ParallelTimeline.
Boundary policy priority is layout, density, then QAT; relocating command drawings
retain QuickAccessMove timing. No host controls or completes library playback.

Vector QAT/header/caption drawings use RibbonWindow's outer plane so the detached
title-bar host and lower panel share coordinates. Ownership commits once; live
controls remain at final bounds. Command-strip source/destination drawings travel
and crossfade at natural size. Panel chrome is separate from commands, retaining
its nested effect/clip tree, shadow and opacity masks. A nontrivial LayoutTransform
or composed reversal uses local departure/arrival. Missing caption planes, native/
media/live-VisualBrush capture and persistent anchored popups settle immediately.

The body and QAT holds are installed synchronously before callbacks can expose
destination layout. Final capture and shared playback start happen in one deferred
operation after normal preparation and borrowed-item returns. Supersession samples
the currently composed frame before release; Preparing(B) can precede Ended(A).
Event reentrancy is serialized and cleanup is identity-scoped. Every prepared
identity terminates even when canceled/skipped before Starting. Exceptions clean
up library state before normal propagation.

Two geometry/lifecycle pitfalls were reproduced during implementation:

- Touch title-bar placement changes caption height. A point saved in the content
  adorner layer moves when that layer moves, so it does not hold the old window
  position. Preserve body/header/caption origins in the stable outer coordinate
  plane and animate the body's origin correction as drawing translation.
- Content detachment can fire LayoutUpdated before Unloaded. Verify common
  ancestry before TransformToVisual; otherwise a cancellation path can throw while
  the detached control still reports IsLoaded.
- A replacement hold must inherit the previous presentation's opacity; the real
  body already has an owned zero-opacity clock. Reading that hidden body produced
  a blank pending cover. Clear the stale current-body pointer before callbacks
  that can force layout, while keeping the old session alive for handoff.
- Multiple changes inside one lifecycle callback retain the first old state and
  combine reasons. Overwriting the queued state with the last property's callback
  incorrectly reported an intermediate density as the previous density.
- Broad popup dismissal during QAT relocation closed an unrelated File menu.
  Capture old QAT ownership before reparenting and defer only those closures.
  Version checks skip a popup closed/reopened since capture. Startup does not
  enqueue cleanup that could close a later menu.

Temporary hide clocks compose over existing animation. Seek the owned contribution
to its stopped fill synchronously, then remove that clock; clearing all opacity
animation would destroy another animator's clock. WPF may retain inactive animation
storage until its next timing tick, so immediate-settle checks observe visible
opacity/presentation rather than HasAnimatedProperties alone. Existing density
consumer assertions now verify inert QAT drawings and unchanged live transforms;
the focused lifecycle scope retains unload/template/policy/popup checks.
The Crystal utility consumer's minimize fixture now performs its native click
instead of assigning a OneWay-bound IsChecked value, matching the implemented
feature-availability policy without altering minimize behavior.

ShowcaseContentTransition is application-owned and uses only public events/progress.
MainWindow animates DocumentSurface containing DocumentEditor; RTL Lab uses the same
example. An independently authored public-only consumer animator verifies optional
host synchronization separately from QAT movement without subscribers. Runtime
pixel checks follow a unique QAT icon through both directions of both routes and
assert natural dimensions, instead of treating a declared policy/adorner as proof
of movement. Main-window captures inspect prepared/start/middle/final frames.

Exact final build/test/package results and native limits remain in the active plan.
Older full-run-plus-repair evidence is not reclassified as a clean suite pass, and
the two residual Crystal QAT comparisons remain open unless separately reconciled.

### 3.261 Foreground stays outside decorative shadow effects — 2026-10-17

The user's Crystal 100% comparison isolated a soft foreground despite corrected
text spacing. A software-rendered diagnostic reproduced the actual PNG exactly
with animation disabled and Fixed hinting. RenderingBias=Quality did not change
it; separating shadow chrome from commands produced the accepted sharp image.

The shared body and lower-QAT templates now place effect-bearing decoration and
foreground in siblings. Existing named decorative borders retain their resources
and geometry; foreground insets bind to those borders. Complete presentation roots
own minimize/layout opacity and motion, while tab switching still moves content
only. The lower-QAT clip remains on its parent after the child effect renders.
Custom templates without the new body wrapper keep their old lookup fallback.

Body holds/playback/reversal snapshots retain a separate vector chrome tree so
the animation adorner does not reapply the effect around text. Boundary scaling
affects decoration only. Painted geometry still reads the decorative borders'
shadow extent. Initialize visual-child enumeration defensively: WPF can query it
while the constructor sets IsHitTestVisible, before the collection is assigned.

Pixel checks compare foreground with its own shadow toggled, retaining a neighboring
body shadow that can legitimately overlap the QAT. Keep a disconnected capture root
independent of native Window content offsets when sampling by visual coordinates.
Utility-arrow coordinates use TransformToVisual because decoration is a sibling.
Independent consumers verify both QAT routes with/without host synchronization,
plus existing binding, geometry and interruption checks. Reviewed visual updates
cover sharper foreground in affected shadow themes, with no tolerance change.
Exact evidence and remaining native gates stay in plan 18; Writer popup sharpening
is a separate open RKWF-049 investigation.

### 3.264 Stable caption and content paint during layout transitions — 2026-10-18

Cross-fading complete opaque drawings with complementary opacity exposes their
backing: source-over alpha is 0.75 at the midpoint, rather than 1. The detached
title-bar QAT track included caption background paint in both drawings, explaining
the white flash over Showcase's white window backing. Caption foreground now fades
over one independently carried background; its geometry follows the shared boundary.
Capture actual drawing bounds because layout can change before a Border repaints.

A growing title-bar QAT also moves the real ribbon origin. Its transparent header
cannot fill the gap above that origin. A separate themed RibbonBackgroundLayer
provides inert gap paint, clipped to the presented header outside the real backdrop.
Keep the real backdrop visible and exclude its bounds: overlapping copies change
authored alpha, and a full backdrop in the window-wide adorner obscures body motion.
Reversals retain the composed geometry; custom captions without separable backdrop
parts settle immediately. Placement ownership, bindings and cadence are unchanged.

The application-owned Showcase animator carries one content drawing after shared
start and scales only its root decoration to the moving viewport. Whole-card fades
flash the paper; clipping two sizes leaves an early bottom-border seam. Child text
and icons stay unscaled. The main window starts with the same themed background
used by its normal backdrop setup. No RichTextBox rendering policy is involved.

Regression pixels cover caption color/alpha, the moving header gap, card interior
and border, plus unique body ink so geometry-only checks cannot miss occlusion.
Wait for terminal cleanup and a rendered frame when fixtures need settled drawings;
observe the public Progress DP rather than assuming a background timer sees short
intermediate frames. Current focused evidence and native limits belong to plan 18.

The user's subsequent video isolated stationary caption/tab-header ink dimming
during Simplified toggles. This is the same complementary-alpha hole applied to
foreground: repairing the background alone did not preserve text contrast. Paused
mid-transition pixel checks reproduce it before any completion text refresh runs.
For a mode-only change with unchanged caption/header rectangles, carry one final
foreground at authored opacity from shared start. Density, placement, visibility,
combined changes and genuinely moving chrome retain their existing presentation.
Keep the actual caption/header band bounds when sampling a reversal; foreground
descendant bounds omit empty padding and otherwise invent geometry movement.
The shared text-settle correction is unchanged. Native glyph-region comparisons
cover before/start/intermediate/rest/600-ms-later frames, rather than sampling only
background color. Current evidence remains in the active plan.

The title-bar QAT had its own strip crossfade and needed the same stationary
mode-only rule. Its icon-only status excludes text refresh, not opacity stability.
Carry one committed strip when its endpoint rectangles match, without altering
relocation, density or combined requests. Preserve the strip's band rectangle for
pending holds and stationary sampled reversals; descendant ink bounds alone can
otherwise trigger a false departure/arrival. Extend pixel checks to actual QAT
icons, since stable caption text does not prove stability in that separate layer.
