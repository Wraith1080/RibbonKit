# Ribbon and host content animation synchronization

> H1 architecture/visual direction accepted and implementation authorized on
> 2026-10-16. H2 is implemented locally; H3 evidence and remaining acceptance are
> recorded below. No commit, push, merge, publication or scheduling is authorized.

## Goal and scope

Coordinate Classic/Simplified, Compact/Touch and QAT placement through one
optional lifecycle. RibbonKit owns reusable coordination and QAT animation;
applications own their content animator. The Showcase main window animates its
`DocumentSurface` card containing `DocumentEditor` (RichTextBox), and RTL Lab
animates its content through the same public contract.

Actual QAT relocation covers both directions of TitleBar/BelowRibbon and
TabRow/BelowRibbon, including the entire lower panel, clipping and shadow.
General resizing, tab switching, popup-opening motion, Writer/MDI integration and
a generic document animation framework remain outside this track. Existing input
policies and Crystal capture-registration optimization are retained.

## H1 — accepted architecture and visual direction

`Ribbon` owns a private per-instance coordinator. It observes effective property
changes, capability coercion and overlapping requests; the existing selection
properties remain the selection APIs. `RibbonWindow` supplies its window-wide
presentation plane and caption visual internally. Ribbon remains usable in an
ordinary WPF Window; a custom window without a safe caption plane snaps title-bar
relocation instead of drawing across an unreliable clipping boundary.

The selected visual direction is continuous travel of inert command-strip drawings
between measured positions in a safe common plane. Source/destination drawings
crossfade at natural size, accommodating different labels, tints and overflow
presentation. Live controls commit their final ownership once and never fly.

Composed interrupted drawings and nontrivial LayoutTransform hosts use coordinated
short departure/arrival. Unsafe native/media/live-VisualBrush capture, an unavailable
presentation plane, persistent anchored popups and invalidated geometry settle
immediately. Snapshots preserve opacity masks and child Effect -> parent Clip
ordering. Only lower-panel chrome scales; command drawings and application text
never scale. The panel grows/retracts with the boundary while the strip travels.

## Public contract

The three ordinary CLR events on Ribbon use `RibbonLayoutTransitionEventArgs`:

| Event | Ordering and host use |
| --- | --- |
| `LayoutTransitionPreparing` | Synchronous old-presentation capture/hold, before a destination frame can be exposed |
| `LayoutTransitionStarting` | Committed geometry is measured; shared sources remain at zero until every synchronous handler returns |
| `LayoutTransitionEnded` | Exactly once per prepared identity, including superseded/interrupted/skipped work that never started |

Handlers have no Cancel, deferral, clock controller or completion obligation. They
must be short synchronous notifications; RibbonKit never awaits an application.
Callback exceptions trigger library cleanup and terminal notification, then propagate
normally. An exception in an application event handler is still an application error.

Context includes a per-Ribbon identity, reason flags, immutable previous/target
effective state, explicit CoordinateRoot, previous/final occupied and painted
bounds, QAT strip/panel bounds and root DPI. FinalGeometry is null in Preparing
and may remain null if canceled/skipped before measurement. Reasons distinguish
layout, density, placement, visibility and capability coercion. Requested dependency
property values, bindings, command objects and customization data remain intact.

All rectangles use CoordinateRoot's local DIPs. Transform through that visual for
RTL and detached title-bar coordinates; do not infer screen pixels or caption height.
OccupiedBounds reserves layout space, including the lower row and hosted messages;
PaintedBounds also includes shadow overhang. Touch title-bar QAT changes can alter
caption height: previous body/header/caption origins are retained in the stable
window plane rather than the content adorner layer that itself moves.

`RibbonLayoutTransitionMotion` exposes read-only Progress through a read-only DP,
Action, EffectiveLevel, actual Duration and frozen Easing. BoundaryMotion and the
optional QuickAccessMotion are child clocks of one library-owned ParallelTimeline.
The host binds only its own render presentation to these sources, without a timer.

Boundary policy priority for a combined effective change is LayoutModeChange,
then DensityChange, otherwise QuickAccessMove. Relocating commands retain a
separate QuickAccessMove track. Existing cadence remains 130/182 ms for layout/
density, 150/210 ms for QAT; Expressive QAT retains BackEase. Starting is shared;
Ended follows all participating tracks. Clips and alpha clamp eased overshoot.

```csharp
// contentAnimator belongs to the application. Dispose subscriptions with its host.
ribbon.LayoutTransitionPreparing += (_, e) =>
    contentAnimator.Hold(e.TransitionId, e.CoordinateRoot);
ribbon.LayoutTransitionStarting += (_, e) =>
    contentAnimator.Start(e.TransitionId, e.BoundaryMotion);
ribbon.LayoutTransitionEnded += (_, e) =>
    contentAnimator.Release(e.TransitionId, e.Outcome);
```

The Showcase implementation is `ShowcaseContentTransition`; it captures its own
vector content, translates/clips inert drawings, preserves editor identity and
restores its own composed opacity clock. It is application code. The independent
consumer has a separately authored animator with no Showcase reference.

## H2 — implementation and interruption rules

- Selection and single-parent item transfer commit to the latest destination.
  Deferred placement claims coalesce, and popup/borrowed-content cleanup follows
  the routed invocation. Final anchors and tab selection markers update from real
  layout without waiting for playback. Hidden QAT and unchanged content bounds
  do not invent host movement.
  Relocation snapshots only the old QAT's open popup ownership before reparenting;
  deferred cleanup leaves File menus and subsequently reopened popups alone.
- Old drawings hold synchronously through deferred preparation. Final capture,
  source staging, Starting and clock release happen without a render boundary
  between them. Real layout is not changed per frame.
- Rapid replacement samples the currently composed drawing before releasing it.
  Preparing(B) may precede Ended(A, Superseded), so hosts can capture their current
  presentation. Host cleanup must be identity-scoped. Notifications serialize
  reentrant requests; stale starts/completions cannot release newer sessions.
  Multiple changes within one notification retain the first previous effective
  state and combine reason flags. Successor holds retain presentation opacity
  instead of sampling the already-hidden real body.
- Preview mouse, wheel, keyboard, touch and stylus input settle the transition
  without consuming input, including the detached caption QAT and host content.
- Unload, template/theme replacement, geometry/RTL/DPI and policy changes release
  clocks/holds. Check common ancestry before measuring: detachment can precede
  Unloaded. Startup restores snap until first rendering. No Motion skips playback.
- Temporary drawings are inert. Real controls retain commands, focus, hit testing
  and automation. Focus recovery is guarded against an external focus choice.
  Authored transforms, clips, opacity, bindings and existing animation clocks
  survive; cleanup removes only the library's composed hide contribution.
- With no host subscribers, QAT and library motion still play. Application content
  takes its final layout immediately unless its application supplies an animator.

## H3 — verification and acceptance, 2026-10-16

| Gate | Current result |
| --- | --- |
| Focused lifecycle/runtime | The final affected selection passed 101/101: host lifecycle/QAT, existing layout motion, Simplified, dropdown pressed visuals, overflow focus/utility and popup dismissal. The subsequent queued-state repair passed all 25/25 HostLayoutTransition cases, including combined changes from Ended. Covers pixel travel on all four QAT routes, forced-layout overlapping holds, effective capability coercion, reentrant notifications, exceptions, interruption, binding/clock preservation, stale popup cleanup and actual MainWindow integration |
| Main-window render inspection | Captured prepared/start/middle/final frames for mode, density and all four QAT routes in Office2024 and Crystal. Inspected natural glyph/icon scale, document movement, panel/shadow and caption-height synchronization. Frames are under TestResults/host-layout-frames; this is offscreen rendering, not native acceptance |
| Independent host consumer | 24/24 combinations passed after the presentation/popup repairs: Office2010/Office2024Flat/CrystalLight, LTR/RTL, Compact/Touch, with/without a separately authored host animator. Each covers all four required QAT routes plus layout/density changes, single ownership and terminal cleanup. Touch cases simulate 150% root DPI and Compact uses 100%; this is not a full DPI cross product or mixed-monitor acceptance. No Showcase resources/helpers |
| Full Release run and targeted continuation | Runtime 959/959 and Writer 483/483 passed in the full solution run. The consumer aggregate was completed through retained 20-scope prefix, 11-scope continuation and 9-scope final continuation plus its remaining aggregate assertions. Existing density/layout matrices passed; the old minimize fixture now invokes its real click policy. Later focused repairs are recorded above. This is assembled evidence, not a clean single full-suite pass |
| Visual comparisons | Four harness tests passed. The matrix passed 63 scenes before the known Crystal light QAT-below 100% mismatch; 106 remaining selected comparisons passed normally. Total 169/171. The residual 100% and 200% differences remain 5,551 and 17,043 significant pixels; inspected actual/diff images and SHA256 comparisons match the retained pre-track actual PNGs byte-for-byte. No approval/tolerance changed |
| Final Release/API/package gates | Final normal Release solution build passed, 0 warnings/errors, including both runtime TFMs and API/XML analyzers. Shipped baseline untouched; additions are in PublicAPI.Unshipped. Normal pack and archive/designer-layout validation passed; clean net8/net9 WPF package consumers built with 0 warnings/errors. Older packages were preserved and the current pair was selected in a validation-input directory. Package runtime smoke/native designer discovery were not run |
| Normal Debug review output | Final normal solution Debug build passed, 0 warnings/errors. Showcase is ready in its standard output folder |
| User appearance review | On 2026-10-16 the user reported that the implemented host-layout/QAT/content animation "looks perfect." This accepts the appearance they reviewed; the later QAT-height and Chart Tools marker reports are tracked below |
| Native acceptance | Open: exhaustive real mouse/touch/keyboard and KeyTips, focused editor interaction, popup anchors and cleanup, genuine mixed-monitor DPI and OS reduced motion. The reported appearance approval does not establish these separate gates |

Focused checks and normal outputs use standard Debug/Release configurations.
Full Release validation is required for the public additions; XML documentation
and PublicAPI.Unshipped are additive, with the shipped baseline untouched.
Retain passing evidence; broaden/repeat for concrete source changes or failures.

Logs under TestResults: host-layout-full-release.log,
host-layout-final-runtime.log, host-layout-coalescing-final.log,
host-layout-consumer-continuation.log, host-layout-consumer-final-tail.log,
host-layout-consumer-latest.log, host-layout-visual-final.log,
host-layout-remaining-visual.log, host-layout-crystal-200.log,
host-layout-release-build-complete.log, host-layout-debug-build.log,
host-layout-pack.log and host-layout-package-validation.log.

Prior Simplified end-of-feature evidence remains assembled from a full run and
targeted repairs, not a clean single full-suite pass. Its two Crystal QAT snapshot
comparisons and native limits remain in the
[active Simplified plan](15-SIMPLIFIED-RIBBON-PLAN.md#end-of-feature-validation-and-repairs--2026-10-14).
Toolbox native creation/transaction/save/reload and separate package-reference
discovery remain in [plan 17](17-TOOLBOX-ITEM-DEFINITION-PLAN.md). Accepted dropdown
press behavior and the latest overflow timing follow-up remain in
[plan 09](09-FUTURE-THEMES-PLAN.md#colored-qat-split-and-dropdown-states).
This track does not establish a latency improvement or Visual Studio acceptance.

## Post-review geometry repairs and full validation — 2026-10-17

The user requested a fresh full test after two bounded repairs: stable lower-QAT
height in Office 2024 Flat and correct contextual marker placement while Chart
Tools headers enter in Office 2024, Flat and Crystal. The fixes were committed by
the user as fc90dfb on 2026-10-16. They remain in shared tokens and RibbonTabControl,
with no API additions or Showcase-only workaround.

| Gate | Current result |
| --- | --- |
| Independent geometry checks | QatRowGeometry and ContextualMarker passed 24 combinations each: three palettes, light/dark, LTR/RTL, Compact/Touch. Touch cases simulate 150% root DPI; Compact uses the current display DPI. Checks add/remove dropdown, split, gallery and group QAT proxies, sample merged-header entrance/rest/remerge geometry, and retain the compact ordinary selection glide |
| Actual Showcase | MergeSelectionTests passed 8/8, including all three pill themes. Inspected the Chart Tools captures under TestResults/geometry-fixes; this is authored offscreen rendering, not new native input acceptance |
| Full Release runtime | 969/970 passed in the full run. The remaining sequential-transition test reached the correct combined state but checked its second completion after a fixed single-transition interval. Replacing that interval with a bounded wait for both Ended notifications passed the focused repair. All 970 cases have passing coverage assembled from the full run and this repair; this is not a clean single runtime/full-suite pass |
| Writer | 483/483 passed in the full run; retained without repetition |
| Full independent consumer | The full aggregate hit its 20-minute cap during OverflowMenus after 30 named scopes passed, including both new geometry matrices. The unfinished overflow scope passed separately. The next continuation passed three more scopes, then exposed an old assertion requiring every non-Crystal minimum height to be zero. It now expects Flat's intended 32-DIP reserve. The final continuation passed the remaining eight scopes and aggregate assertions: 42 unique named scopes covered. Future full runs have a bounded 30-minute cap; the passed prefix was not rerun to claim a clean aggregate |
| Visual comparisons | Four harness tests passed in the full run, plus 63 comparisons before the known Crystal 100% mismatch. The unfinished selection passed in retained 66-scene and six-scene prefixes followed by the final 34-scene comparison. Total 169/171. Sixteen Flat approvals were updated only after actual/diff inspection: lower-QAT, state, connected-message and File-menu scenes reflect the intended row reserve and consequent message position. No tolerance changed. The two Crystal QAT-below 100%/200% residuals remain 5,551/17,043 significant pixels; both current actual PNGs match retained pre-track captures byte-for-byte |
| Final builds/API | Normal Release and Debug solution builds passed with 0 warnings/errors. Both runtime TFMs and API/XML checks passed; neither public API baseline changed in these geometry repairs |
| Package | Normal pack and archive/designer-layout validation passed, plus clean net8/net9 WPF package builds with 0 warnings/errors. Packaged runtime DLL hashes match the final normal Release outputs. The previous package pair was preserved before packing; a validation-input directory selects the current pair. Package runtime smoke and native Toolbox discovery were not run |
| Review/output limits | Normal Debug Showcase is ready. Existing physical input/KeyTip, popup-anchor, genuine mixed-monitor DPI, OS reduced-motion and designer gates remain separate. New follow-up work after user commit fc90dfb consists of validation fixtures, continuations/timeouts, reviewed approvals and documentation; no additional runtime behavior was changed |
| Crystal reconciliation, 2026-10-17 | The user accepted the current 200% capture; its approval was updated and the normal selected comparison passed. With retained coverage, 170/171 comparisons now have passing evidence. At 100%, corrected text spacing is accepted but the current capture remains too soft. The user accepted a sharp diagnostic with isolated shadow chrome; that preview is not an implemented correction or acceptance of the current 100% capture |

Implementation rationale is in
[the geometry correction record](history/library/13-flat-theme-variants.md#3260-stable-lower-qat-and-contextual-selection-geometry--2026-10-16).

Evidence under TestResults: geometry-full-release.log,
geometry-terminal-wait-repair.log, geometry-consumer-tail.log,
geometry-consumer-overflow.log, geometry-consumer-final-tail.log,
geometry-remaining-visual.log, geometry-final-visual.log,
geometry-final-visual-tail.log, geometry-crystal-200.log,
geometry-flat-visual-capture.log, geometry-flat-visual-update.log,
geometry-flat-states-capture.log, geometry-flat-states-update.log,
geometry-flat-menu-capture.log, geometry-flat-menu-update.log, geometry-final-release-build.log,
geometry-final-debug-build.log, geometry-final-pack.log and
geometry-final-package-validation.log. Capture/update-mode success alone is not
an approval-comparison pass; the normal comparisons above supply that evidence.

The 100% diagnostic reproduces the current actual PNG byte-for-byte with motion
disabled and fixed/static text hinting. The body and lower-QAT shadow effects
enclose their foreground content. Changing RenderingBias from Performance to
Quality leaves both captures unchanged in this software-rendered path; moving
decorative chrome/shadow into separate inert siblings produces crisp foreground
text and icons while retaining the shadow. This is a diagnostic visual-tree
experiment, with no runtime template change and no 100% approval update.
Evidence is under TestResults/crystal-sharpness; the accepted 200% comparison is
recorded in crystal-200-reviewed-comparison.log.

The next bounded correction should separate decorative shadow chrome from live
foreground in the shared templates, retain lower-QAT panel clipping and occupied
bounds, and adjust transition capture/bounds lookup where necessary. Verify
affected Crystal body/QAT scenes, relocation with/without host synchronization,
and an independent consumer. Retain existing passing checks and cadence.

The separately reported Writer popup sharpening is an open investigation in
[RKWF-049](12-RIBBONKIT-WRITER-CONSUMER-FRICTION-LOG.md#rkwf-049--popup-text-sharpens-after-opening).
It is not established as the same cause and does not expand this host-layout
track into popup animation changes.
