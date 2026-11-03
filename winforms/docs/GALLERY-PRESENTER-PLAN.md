# Optional native gallery presenter and dropdown content hosting

Status, **2026-11-02: ordinary inline and source/QAT custom gallery popups accepted at the reviewed scope**.
**2026-11-03:** the [inline class-picker increment](#inline-gallery-designer-class-selection--2026-11-03)
is implemented with focused and package-consumer evidence; actual VS review remains pending.
The subsequently reported popup-card clipping and temporary group-body fallback
are tracked in the correction record below.
The user previously confirmed the dropdown gallery presenter works.
The user accepted the synchronized gallery fade/slide entrance on **2026-11-02**.
The built-in gallery's first-opening scrollbar capture correction is recorded
[below](#built-in-gallery-scrollbar-opening-capture--2026-11-03). User review,
**2026-11-03**: the reported first-opening black scrollbar is "fixed perfectly".
Gallery UserControl class selection in the ribbon designer is implemented with
focused and package-consumer evidence below; actual VS review remains pending.
The follow-up moves the Designer Sample hierarchy into designer-managed code;
the focused native check passed, while actual VS review of that relocation is pending.
The user selected the custom gallery UserControl presenter as the next slice.
The runtime contract and ownership decisions for this increment are recorded below;
the existing built-in gallery remains the default.
This is native RibbonKit.WinForms work; WPF runtime/build/CI remain outside scope.
The ordinary dropdown/split UserControl increment is tracked in the
[retained content plan](DROPDOWN-CONTENT-PLAN.md); the separate
[inert popup designer preview](POPUP-DESIGNER-PREVIEW.md) is now implemented. Inline/QAT custom presenters
now include the [first ordinary inline body increment](#first-inline-runtime-increment--2026-11-02).
The [QAT gallery popup increment](#qat-gallery-popup-increment--2026-11-02) reuses the
existing dropdown class/factory. The
[temporary body correction](#popup-card-rendering-and-temporary-inline-hosts--2026-11-02)
adds source-group, minimized-tab and QAT-group bodies. Inline designer class
selection is now implemented under the [inline/QAT contract](#inline-and-qat-gallery-presenter-contract--proposal-2026-11-02).

## First increment contract — 2026-11-02

- `RibbonGalleryItem.DropDownPresenterFactory` is an optional runtime factory.
  It returns a fresh, unparented UserControl implementing
  `IRibbonGalleryPresenter`. RibbonKit owns and disposes each accepted control
  with its popup. Creation is lazy, after DropDownOpening succeeds; reopening
  creates a new view. There is no borrowed-instance or eager initialization mode.
- `IRibbonGalleryPresenter` synchronizes from a library-supplied context and
  reports item bounds, hit testing, layout-specific navigation and row/page
  scrolling. The native Control supplies preferred size. The library constrains
  the viewport and keeps its own popup frame, scroll buttons and keyboard route.
- `RibbonGalleryPresenterContext` supplies the original gallery and display
  settings. Its Preview/FocusItem/Commit requests use the existing source behavior;
  stale contexts are inert after close. NotifyViewChanged reports transient
  bounds/scroll changes, never model or persistence changes.
- Only ordinary dropdown-gallery openings use the factory in this increment.
  Inline strips and QAT keep their built-in presenters; no source or tile clones
  are introduced. Designer class selection, ordinary dropdown/split UserControl
  content and popup designer previews remain subsequent slices.
- The consumer owns its visuals, application data/assets and internal layout.
  RibbonKit owns navigation dispatch and accepted activation. Presenter methods
  run on the ribbon's UI thread and must not independently execute tile actions.

## Using and reviewing this increment

Select the gallery in **Edit Ribbon**, then choose **Data → DropDownPresenterType**.
Build the consumer project to refresh available eligible UserControl classes.
The class choice is saved as:

```csharp
gallery.DropDownPresenterType = typeof(MyGalleryControl);
```

For construction with application arguments, use the runtime factory instead.
Clear the class selection first; the class and factory are mutually exclusive:

```csharp
gallery.DropDownPresenterFactory = () => new MyGalleryControl();
```

`MyGalleryControl` is an ordinary UserControl implementing
`IRibbonGalleryPresenter`. The library calls `Synchronize(context)` before
measuring it with `GetPreferredSize`, then supplies the final constrained
`context.Viewport`. Use `context.Dpi` during this pre-parenting measurement.
Return current source-tile bounds from GetItemBounds, including scrolling, and
resolve navigation using the actual layout. The library clips an oversized
native minimum size inside the constrained viewport.

Source selection changes synchronize and reveal the selected available tile;
they never become commits. Native child pointer handlers report Preview,
FocusItem and Commit through their context. Root-control pointer events and
navigation keys are library-routed; do not add a second keyboard dispatcher or
call tile.PerformClick independently. Input requests during Synchronize are
rejected, even though an open context still reports IsActive. Report internal
layout/scroll changes with NotifyViewChanged. Borrowed font, renderer and
application assets must not be disposed by the presenter.

The permanent Designer Sample declares **Gallery tab → Document styles** in
`DesignerForm.Designer.cs`: tab, group, gallery, 18 named tile fields, stable IDs,
initial selection and event hookups all live in InitializeComponent. They are
available to the existing Edit Ribbon editor rather than injected at startup.
It uses [GalleryStylesControl](../samples/RibbonKit.WinForms.DesignerSample/GalleryStylesControl.cs),
with a separate normal UserControl designer file. Existing user-authored form
items and UserControl edits were preserved. The presenter-class assignment is
also in the designer file; no factory hookup is needed in `DesignerForm.cs`.
Reopen the Form designer to see the
Gallery branch in Edit Ribbon; restart the sample to review runtime behavior:

1. Open Document styles; hover or use arrows, Home/End and PageUp/PageDown. The
   application status label distinguishes preview from accepted selection.
2. Use the footer arrows or mouse wheel to browse rows. Click or Enter/Space
   commits through the source gallery; Escape cancels preview and closes.
3. Reopen: source selection survives and is revealed in a fresh native control.
   Uncommitted browse position is transient and is not restored between views.

To review the class selector in Visual Studio, reopen the refreshed solution,
open the Form designer and select **Gallery → Custom gallery → Document styles** in
Edit Ribbon. Check `DropDownPresenterType`, try `(None)` and Cancel, then apply a
class choice with OK. Check Undo/Redo and save/reopen; the assignment should be
`typeof(GalleryStylesControl)` with no constructed control or factory delegate.
The separate UserControl designer edits the actual body.

For the designer-friendly card example, open **GalleryStyleTileControl.cs → View
Designer** to edit its normal paper panel, accent panel, preview label and title
label. Open **GalleryStylesControl.cs → View Designer** to see six actual card
controls without a runtime gallery. Runtime captions still come from the source
items in Edit Ribbon; the tile's Caption property is illustrative designer text.
The parent presenter arranges the runtime two-column grid automatically.

The factory is intentionally hidden from designer serialization and customization
JSON. The built-in presenter remains the default when both selectors are null and
for inline strips, direct QAT galleries and galleries reached through QAT groups.

The user accepted the initial runtime presenter on 2026-11-02 and clarified that
the Designer Sample must demonstrate designer-authored structure. After moving
the declarations, the normal Debug sample build and the existing public-API
gallery check passed in `artifacts/gallery-presenter-designer-source/` at 120 DPI.
The check verifies the hierarchy, tiles and initial selection exist before
Form.Load, exercises the designer-wired preview/commit handlers, and retains its
reopen/disposal checks. No runtime change, repack or unit-suite rerun was needed.

## Implemented evidence — 2026-11-02

- Normal Debug runtime, Designer Sample and Showcase builds pass without
  warnings/errors. The 31 new API entries are in the native Unshipped baseline;
  shipped API files and WPF projects were not changed.
- The affected gallery/menu selection passed **38/38** in
  `gallery-presenter-related.trx`. Further focused runs passed **2/2** focus/size,
  **2/2** pointer/validation, **3/3** context/QAT, **1/1** disabled focus,
  **3/3** native-button paint/keyboard-preview and **1/1** navigation cases.
  These scopes overlap: **17 presenter cases plus 30 existing cases** passed
  across separate runs, not one full-suite run. Files are under
  `winforms/artifacts/test-results/gallery-presenter-*.trx`.
- Coverage includes lazy/cancelled opening, per-popup ownership and fresh reopen,
  failed initialization, rejecting borrowed/parented controls, source availability
  changes/disposal, stale callbacks, preview ownership, programmatic reveal,
  accepted activation once, validation cancellation/reentrancy, bounds and scroll
  endpoints, all navigation keys, matching pointer press/release, keyboard preview
  versus stationary-pointer repaint messages, and built-in inline/QAT paths.
- The independent package-only Designer Sample passed its native public-API
  check at actual **120 DPI** in
  `winforms/artifacts/gallery-presenter-review-painted/`. Complete-popup PNGs
  were inspected at initial selection, last-row selection and reopened/page-up.
  The live native handle survives synchronization/reveal; popup close disposes
  the view, while source items and selection survive. One controlled commit is
  delivered. No Showcase reference/helper is used.
- Earlier diagnostics exposed stale text under the new scroll buttons; they now
  own a complete opaque background and themed arrow drawing. An earlier expanded
  capture found a disposed view before sampling and is not a pass. The final
  fixture activates and verifies its owned foreground before opening/capture.

The local sample package used for that earlier dropdown increment was
`0.1.0-designer.local20261102T052414776Z`. Run its public check with:

```powershell
dotnet run --project winforms/samples/RibbonKit.WinForms.DesignerSample -c Debug --no-build -- --verify-gallery-presenter winforms/artifacts/gallery-presenter-designer-source
```

Physical pointer/keyboard review, mixed-monitor DPI, actual VS body-design review,
resource/performance budgets and accessibility certification remain separate.
Existing item identities/bounds reach the popup automation path; this increment
does not add gallery UIA patterns or claim native child accessibility parity.
No full suite, Release campaign or WPF checks ran. Ordinary dropdown/split
UserControl hosting still requires its separate lifetime/content contract below.

## Opening-delay correction — 2026-11-02

The user supplied `20261102-0220-07.2122985.mp4` and clarified that the problem
was a delay before content appeared. The recording shows the popup's empty white
frame before its cards become visible. The factory does create a new view for
each opening, as specified above, but measured managed construction was around
0.1–0.3 ms on repeat openings in this sample. Built-in gallery openings also had
substantial setup/animation time. These results do not establish the cost of a
different application's constructor or native setup.

The concrete error was in shared anchored-popup motion: its clock began after
Show, native layout and snapshot preparation, and its first published drawing
always used progress zero. An empty frame could wait for the first WinForms timer
tick, followed by the entire requested animation. The capture bitmap itself was
valid. The clock now starts when eligible opening preparation begins; the first
published image uses elapsed progress. Setup counts toward the existing 130/182 ms
budget instead of preceding a second complete duration. Native frame, easing,
travel, animation policy and per-popup factory ownership remain unchanged. No
cache, retained-view API, application timer or global Windows setting was added.
The following gallery entrance increment supersedes this elapsed-first-frame
behavior for galleries, after the user reported that their animation was no longer
visible. Other anchored menu openings retain it.

- A regression simulates 60 ms of application drawing during preparation. Its
  first frame was empty before (**1/1 failed**) and contains content after.
  The affected selection passed **13/13**, including existing popup motion/input/
  policy/lifetime cases and custom-body capture in both tab-content settings.
  Results: `gallery-opening-first-frame-before.trx` and
  `gallery-opening-clock-fixed.trx` under `artifacts/test-results`.
- At actual 120 DPI, the native trace's sampled in-flight frames were empty in
  `artifacts/gallery-opening-delay/native-fast/`. In
  `artifacts/gallery-opening-delay/native-clock-fixed/`, its first sampled frame
  already contains the gallery with zero timer ticks; subsequent sampled frames
  retain that content. Before/after PNGs were inspected. This directly checks the
  blank interval, not frame rate or a guaranteed click-to-visible latency: the
  capture/PNG diagnostic itself delays the UI thread.
- The profile records factory, synchronization, paint, synchronous opening and
  settling separately. `before/`, `ticks-before/` and `timings-fixed/` beneath
  `artifacts/gallery-opening-delay/` are local diagnostics; cold starts, native
  work and machine load vary. An earlier drawing trace stopped on foreground
  loss and is not a passing comparison. The bitmap-only capture cases passed
  before the fix, which is why the native trace was necessary.
- The public-only functional check passed after the change in
  `artifacts/gallery-opening-delay/functional-fixed/`, preserving designer source,
  preview/commit, handles within a view, fresh reopening and disposal. Debug sample
  build/package refresh passed cleanly. Only the affected motion tests and native
  gallery checks ran; no full suite, WPF or Release campaign.

Developer diagnostics:

```powershell
dotnet run --project winforms/samples/RibbonKit.WinForms.DesignerSample -c Debug --no-build -- --profile-gallery-opening winforms/artifacts/gallery-opening-delay/profile
dotnet run --project winforms/samples/RibbonKit.WinForms.DesignerSample -c Debug --no-build -- --profile-gallery-opening-animated winforms/artifacts/gallery-opening-delay/animated-profile
dotnet run --project winforms/samples/RibbonKit.WinForms.DesignerSample -c Debug --no-build -- --trace-gallery-opening winforms/artifacts/gallery-opening-delay/trace
```

The diagnostic profiler inspects internal motion state through reflection; it is
not a public extension contract. The normal sample and `--verify-gallery-presenter`
continue to use public APIs only. User-perceived response/smoothness and other
machines remain acceptance gates. Reusing controls may help genuinely expensive
application views, but is a separate ownership/lifetime decision, not this fix.

## Built-in gallery scrollbar opening capture — 2026-11-03

Baseline: clean `codex/Ribbonkit-Winform` at `b4af3ca` (title-bar customization).
The user reported a black native scrollbar during the ordinary gallery's first
opening. The supplied recording shows the black band during the fade/slide,
followed by the correctly drawn native track/thumb at handoff.

The inert opening bitmap had the control's full `Size`, but its `WM_PRINT`
request covered only client/background drawing. Native scrollbars occupy the
nonclient portion, so those pixels remained black. Gallery opening captures now
also request `PRF_NONCLIENT`; the default capture path for other motion remains
client-only. The capture still composes owned child controls separately and keeps
the real popup, scrollbar, selection and input owners. No public API, palette,
scroll geometry or animation-policy change was added.
See the platform's [WM_PRINT drawing flags](https://learn.microsoft.com/en-us/windows/win32/gdi/wm-print).

Evidence under `artifacts/gallery-scrollbar/` and the native test project's
`TestResults/`:

- Two new Office/neutral raster tests reproduced the first-opening black band
  before the fix (`gallery-scrollbar-before.trx`). Both now pass for first opening
  and reopening (`gallery-scrollbar-after.trx`), checking the actual native track
  and thumb rather than a replacement background fill. Before/after PNGs were
  inspected.
- The related regression selection ran **89 passed / 1 failed / 90 total** in
  `gallery-scrollbar-regressions.trx`. The inline descendant synthetic-keyboard
  case also failed its first isolated retry, then passed against both the
  previously packaged baseline and the fixed runtime in fresh processes
  (`gallery-scrollbar-focus-{baseline,fixed-confirm}.trx`). The fixed test-output
  DLL was restored and hash-checked after the baseline comparison. This is not a
  clean aggregate pass; no unrelated input behavior was changed.
- The independent public-API package consumer reproduced the real screen defect
  with the old package (`native-before/result.log`: first opening had a fully
  dark scrollbar sample). The fixed package passed **12 first/reopening scopes**:
  Solid/Mica/Acrylic × Compact/Touch × two openings, with **132 composed-screen
  samples at actual 120 DPI**. Requested Mica/Acrylic were confirmed effective;
  every sampled scrollbar had zero detected near-black coverage. Evidence and
  captured PNGs: `native-after/`. Fixture-owned windows and asynchronous capture
  preserve the real opening timer; no physical input was injected.
- Normal Debug runtime/designer server/client, package consumer and Showcase
  builds passed with zero warnings/errors. The permanent Designer Sample was
  refreshed in place to **`0.1.0-designer.local20261103T073843725Z`**, preserving
  authored Forms/UserControls/resources.

Human review, actual VS interaction and mixed-monitor DPI remain separate.
No full suite, Release, WPF, commit or push work ran for this bounded correction.

## Synchronized gallery entrance — 2026-11-02

The gallery card, border and shadow now fade together, starting with the item
slide on one visible clock. The fade is linear; the downward item slide keeps its
cubic ease-out, natural item size and fixed rounded viewport. Existing
`AnimationLevel` still selects Subtle (130 ms / 8 logical pixels), Expressive
(182 ms / 12 logical pixels), or immediate presentation. `TabContentAnimation`
does not select popup motion. Built-in and custom gallery bodies share this path.

Preparation captures the body and draws the frame/shadow before the entrance
clock starts. Alpha-1 holds keep the real popup and its native controls alive;
the static chrome stays hidden during the single inert drawing's fade. This
avoids both an opaque empty card before the items and preparation consuming the
visible animation. The drawing reuses its composition bitmap and DIB each frame.
At settlement, the real controls are repainted underneath it and a native position
batch swaps the inert drawing for the static chrome, avoiding doubled shadow
alpha. Input, policy changes and closing still settle immediately. No public API,
presenter caching, factory ownership or application animation code was added.

- Focused Debug checks passed **14/14** in two retained selections:
  `tests/RibbonKit.WinForms.Tests/TestResults/gallery-card-fade.trx` (13 cases)
  and `gallery-card-fade-policy.trx` (1 case). Coverage includes whole-card/shadow
  alpha, natural-size item travel, slow application capture, fixed native bounds,
  editor identity, both drawing holds, policy/input settlement and disposal.
- The actual native entrance in `artifacts/gallery-card-fade/native-worker-fixed/`
  shows intermediate card/shadow opacity and final content. PNGs were inspected;
  the trace recorded 11 timer frames with a first interval around 16 ms. This is
  a local diagnostic, not a frame-rate or response-time guarantee. Capture now
  runs off the UI thread and saves PNGs after playback: the earlier synchronous
  trace starved the timer. An initial worker diagnostic also failed while saving
  a DIB-backed clone after its motion window was disposed; it was corrected to
  copy pixels into an owned bitmap and is not passing evidence.
- The package-only public gallery check passed at **120 DPI** in
  `artifacts/gallery-card-fade/functional/`, including preview/cancel/commit,
  native viewport, reveal, fresh reopening and disposal. The normal Debug
  Designer Sample was refreshed in place with the package above. No full suite,
  WPF or Release campaign ran; perceived smoothness and physical input remain
  user-review gates. User review, **2026-11-02**: the gallery fade/slide entrance
  looks good at the reviewed scope; broader physical-input/display gates remain.

## Inline gallery designer class selection — 2026-11-03

Implemented as the first user-authorized follow-up, starting from clean
`codex/Ribbonkit-Winform` at `669af3ddfbbfe3f58b404114480ace6713ecbec5`.
This completes the existing inline selector's designer authoring; runtime view
creation, sizing, routing and ownership are unchanged. The user requested stopping
after each slice. Popup designer preview and appearance consolidation remain next,
one at a time, with themes afterward; none was implemented in this increment.

After building the consumer, select the gallery in **Edit Ribbon → Data →
InRibbonPresenterType**, or use the component's **Properties → Data** class picker.
Choose a public concrete UserControl implementing `IRibbonGalleryPresenter` with
a public parameterless constructor. `(None)` restores the built-in inline strip.
Keep `IsInRibbon = true` for the strip; `DropDownPresenterType` remains the separate
expanded-popup choice. The body is edited in its normal UserControl designer.

The existing metadata-only picker now serves both selectors and retains both
assigned classes when discovery is unavailable. Detached drafts, inert previews,
Cancel, Apply, CodeDOM and Undo/Redo never construct application controls. Each
selector rejects its own runtime factory conflict before live mutation; the other
selector and source selection remain intact. Factories stay excluded from saved
code. No public member, customization schema, runtime painting or popup preview
was added.

Verification:

- **16 picker tests passed**, including both selectors' discovery, eligible-only
  choices, retained types, draft/preview/Cancel/reset, invalid choices, factory
  conflicts, framework Undo/Redo and actual `typeof(...)` serialization.
- The affected existing designer/inline/temporary/QAT/retained-dropdown selection
  passed **124/125** initially. Its remaining assertion required the formerly hidden
  inline property. It now checks the intentionally visible metadata; only that
  case was rerun and passed **1/1**. These are 141 distinct passing cases assembled
  across runs, not a full-suite or single aggregate result. Logs:
  `inline-gallery-class-picker.trx`, `inline-gallery-class-picker-related.trx` and
  `inline-gallery-class-picker-metadata.trx` under the native test project's TestResults.
- Normal Debug runtime, designer server/client, local package and permanent
  Designer Sample builds passed with zero warnings/errors. The sample was refreshed
  in place using `0.1.0-designer.local20261103T032226969Z`; authored Forms and
  UserControls were preserved.
- The package-only public consumer passed at actual **120 DPI**, including visible
  property metadata, independent inline/popup CodeDOM type assignments already
  compiled in the authored Form, source selection, retained native cards/handles,
  separate popup lifetime, cancel/commit, keyboard/child input, hiding and disposal.
  Evidence: `artifacts/inline-gallery-class-picker/consumer/`.

Actual Visual Studio class discovery, Properties/Edit Ribbon interaction,
save/reopen and Undo/Redo review remain separate from those automated checks.
No full suite, Release, WPF, commit or push work ran. This slice stops here.

## Gallery presenter class selection — 2026-11-02

Implemented as the user-selected bounded designer increment. Nullable
`RibbonGalleryItem.DropDownPresenterType`, accepting a public concrete UserControl
implementing `IRibbonGalleryPresenter` with a public parameterless constructor.
It is mutually exclusive with the existing runtime `DropDownPresenterFactory`;
clear one before setting the other. Null for both keeps the built-in presenter.
Construction remains lazy after accepted runtime opening, with a fresh popup-owned
view per opening. Type selection, draft validation and inert previews never create
application controls. Inline strips and QAT retain their current presenters.

Edit Ribbon and component Properties discover eligible classes from the designer
type service, retaining an already selected class. The existing detached draft,
Cancel and single-transaction OK contracts remain. Generated code stores a
`typeof(...)` assignment; body editing stays in the ordinary UserControl designer.
The Designer Sample's presenter assignment now lives in its designer file; its
code-only factory hookup was removed. Actual VS class discovery/Undo/Redo/save/reopen and
user review remain distinct from automated and package-consumer verification.

- **11 new focused cases pass**: lazy/cancelled creation, fresh popup lifetime,
  built-in reset, invalid classes, factory exclusivity/events, metadata discovery,
  retained class choices, detached preview/Apply/reset, unavailable/forged choices,
  factory-conflict rejection, actual CodeDOM `typeof` serialization and framework
  Undo/Redo without constructing application controls. Results are retained in
  `tests/RibbonKit.WinForms.Tests/TestResults/gallery-presenter-type.trx` (10/10)
  and `gallery-presenter-type-undo.trx` (1/1).
- **88 affected existing cases passed** in `gallery-presenter-type-related.trx`:
  gallery presenter behavior/capture, ribbon draft authoring, QAT source/command
  edits and static theme/structure previews. That selection initially failed the
  new Undo/Redo case because a bare DesignSurface lacked the project type-resolution
  service for the application class. Supplying that fixture service corrected the
  test; only the failed scope was rerun. These are 99 distinct focused passing
  cases assembled across selections, not a full-suite or single aggregate pass.
- Normal Debug runtime/API/XML, designer server/client, local package and Designer
  Sample builds passed with zero warnings/errors. The sample was refreshed in place
  with the package above. Its public-only check passed at **120 DPI** in
  `artifacts/gallery-presenter-type/consumer/`, using the designer-selected class
  without a replacement factory. It verifies initial source/selection/event hooks,
  preview/cancel/commit, native viewport/handle retention, reveal, fresh reopening,
  stale requests and disposal. The initial popup capture was inspected.
  That consumer run used `0.1.0-designer.local20261102T052122328Z`; the final
  package refresh above only clarifies the selector's help text, with prior
  passing behavioral evidence retained.
- No full suite, WPF or Release campaign ran. Actual Visual Studio class discovery,
  Properties integration, Undo/Redo and generated-code save/reopen remain user-review
  gates; the automated designer service/serializer checks are separate evidence.

## Designer-editable sample cards — 2026-11-02

The user said gallery class selection looks good, then requested a simpler custom
control example with visually editable tiles. This follow-up changes only the
Designer Sample and its documentation; no runtime API, library ownership, gallery
motion or source model changes were needed.

`GalleryStyleTileControl` is an ordinary UserControl with panels and labels declared
in its `.Designer.cs` file. Its paper preview now uses a simple native "Aa" label
and accent panel. Only the selected/focused outline uses a short Paint override.
`GalleryStylesControl.Designer.cs` declares six actual preview cards. Runtime
synchronization reuses them, creates any remaining cards for the source items,
and retains those view controls during selection and scrolling. Views refer to the
original source items through Tag; they do not duplicate models or command state.
Root code has separate methods for synchronization, layout, state, pointer routing,
navigation and scrolling instead of one dense tile-painting loop. The card designer
owns appearance; source captions, selection and commands remain in the ribbon.

- Normal Debug Designer Sample build passed with zero warnings/errors, using the
  existing local package. No runtime/design rebuild or package refresh was needed.
- The public-only native consumer check passed at **120 DPI** in
  `artifacts/gallery-designer-cards/consumer-final/`. It checks six designer-visible
  native cards before a runtime context exists, 18 source-bound runtime views,
  preview/cancel/commit, unchanged card/control identity through synchronization,
  reveal and scrolling, fresh reopening and owned-child disposal. Native messages
  to a child label verify one left-click commit, no right-click commit and no
  commit after releasing outside the card. This is fixture-owned native event
  routing evidence; physical input and actual VS designer review remain separate.
- Designer-control and native popup PNGs were inspected. The native entrance in
  `artifacts/gallery-designer-cards/entrance/` includes the child labels/panels at
  intermediate opacity; it recorded 11 timer frames. The later edit tightens mouse
  release validation and designer-only card margins without changing motion.
- `artifacts/gallery-designer-cards/opening-profile/` records four custom and four
  built-in openings, separating construction, preparation and animation. Custom
  preparation ranged about 108–215 ms in that local run; native child controls add
  handle/layout work, and this is not a performance guarantee or an assertion that
  the native-control version is faster than custom painting.
- Only sample build and the affected native gallery diagnostics ran. Existing
  library/designer test evidence was retained; no full suite, WPF or Release checks.
  User review, **2026-11-02**: the designer-editable gallery cards are accepted as
  more readable. This confirmation covers the sample authoring experience;
  broader actual VS, physical-input and display gates remain separate.

## First inline runtime increment — 2026-11-02

The user directed continuation with the plan. This implements its first bounded
slice: **a custom body in the ordinary ribbon strip**, alongside the existing
separately owned expanded popup. Temporary group/minimized-tab/QAT-group bodies
and QAT gallery popups retain their built-in views. No QAT opt-in API, inline
class-picker behavior, ordinary dropdown content hosting or popup designer preview
was added. The existing accepted popup-card entrance and sample popup cards remain.

- Added nullable `InRibbonPresenterType` / `InRibbonPresenterFactory` and their
  Changed events. Their exclusivity is independent of the dropdown pair. Type
  eligibility matches the existing popup selector. Type assignment is lazy; the
  factory must return a fresh unparented UserControl implementing the existing
  `IRibbonGalleryPresenter`. Both null keeps the built-in strip. The inline class
  is code-selected and hidden from Properties until the dedicated designer slice;
  an authored `typeof(...)` assignment remains serializable. Factory delegates
  stay hidden from serialization. Only ten native Unshipped API entries were
  added; shipped baselines and interface method signatures are unchanged.
- Added `RibbonGalleryPresentation` and context.Presentation. The context now
  connects internally to either host while keeping the existing application
  request methods. The permanent inline native tree stays in its ribbon viewport;
  opening a popup never moves it. Hiding/suspension retires its context, cancels
  only its owned preview and releases pending pointer state. Resumption issues
  another context while retaining native controls, row and focus identity. Final
  source detach, disposal, selector replacement/reset or clearing IsInRibbon
  disposes the owned tree. Failed initialization and reentrant factory detach
  unwind ownership without disposing original source tiles.
- Shared selection/reordering reveals the source tile without a user commit.
  Input during synchronization, stale requests, foreign/unavailable tiles and
  reentrant activation are rejected. Native root/child keys and wheel use one
  library route; child pointer activation goes through the supplied context.
  Validation can cancel or invalidate a request before accepted activation.
  Cancelled expansion restores native focus without revealing an older focus
  tile over the independently browsed row.
- The library keeps the existing strip dimensions, equal-height utility buttons
  and inset. Native MinimumSize is clipped by the allocated viewport. Font/theme/
  density/DPI contexts refresh without replacing child controls. The custom body
  participates in library motion capture and existing backdrop-body opacity
  masks. Its viewport reuses RibbonBackdropPanel and composes its native children
  as one opaque subtree, retaining handles over Mica. Logical native accessibility
  exposes tile names, clipped bounds, selection/focus and the same activation route;
  full gallery UIA patterns and combined external-tree acceptance are not claimed.
- The existing Designer Sample declares the inline type, IsInRibbon and a readable
  TileWidth in DesignerForm.Designer.cs. New
  [GalleryStylesStripControl](../samples/RibbonKit.WinForms.DesignerSample/GalleryStylesStripControl.cs)
  has three real designer-visible cards made from the same ordinary panels/labels;
  the accepted two-column popup and its six example cards are unchanged. The strip
  root uses context-sized physical layout without a second automatic scale pass.
  Runtime views bind to the original 18 source tiles. View/scroll/context state
  does not enter customization JSON.

Verification recorded separately:

- Normal Debug runtime/API/XML, native designer server/client and Designer Sample
  builds passed with zero warnings/errors; the package-only sample was refreshed
  in place. The native solution Release build also passed with zero warnings/errors.
- **74 distinct cases passed across focused selections**, including **16 new inline
  cases**. This is overlapping focused evidence, not a full-suite or single aggregate
  pass. Retained results in `tests/RibbonKit.WinForms.Tests/TestResults/` are
  `inline-gallery-related.trx` (50/50),
  `inline-gallery-lifetime-accessibility-fixed.trx` (4/4),
  `inline-gallery-native-surface.trx` (32/32),
  `inline-gallery-final-focused.trx` (3/3),
  `inline-gallery-selector-metadata.trx` (1/1) and
  `inline-gallery-pointer-validation-accepted.trx` (1/1). Coverage includes native ownership,
  stale/reentrant requests, validation, preview versus commit, source changes,
  row/scroll/reveal, minimum-size clipping, native child routing, logical
  accessibility bounds/actions, capture and affected popup/material motion.
- Initial tests caught cancelled-expansion focus resetting the browsed row; that
  correction and the affected cases pass. An attempted reentrant layout retry
  caused a stack overflow and was removed; source validity is checked at construction
  and synchronization boundaries instead. That aborted run is not passing evidence.
  The final pointer regression caught UserControl's implicit focus validation
  occurring before MouseDown. The host now abandons a failed native focus attempt
  and validates explicit child-focus requests once through the library editor
  path; the same test verifies one successful validation and one commit afterward.
- The package-only public-API check passed at actual **120 DPI** in
  `artifacts/inline-gallery-presenter/consumer-final/`, using local package
  `0.1.0-designer.local20261102T075122572Z`. It verifies designer-authored
  separate classes, original source identities, retained control/card handles,
  independent popup disposal, cancel row/focus, stale contexts, native child-label
  clicks and queued keyboard navigation, exactly two controlled commits, hiding
  and owned disposal. The native strip, standalone card designer, expanded popup
  and restored row PNGs were inspected. An earlier fixture left the existing
  right-click customization menu open before the next gesture; the check now
  dismisses it. A separate run stopped on foreground loss and is not a pass.
- The first native capture exposed transparent native child drawing over Mica;
  opaque subtree composition and removing duplicate scaling in the strip sample
  corrected the inspected result. The application content outside the ribbon is
  unchanged. Actual VS body authoring/class discovery, physical input,
  mixed-monitor DPI, RTL, perceived motion/performance and external UIA/accessibility
  certification remain separate review gates. No full suite or WPF checks ran.

Review **Gallery → Document styles** in the normal Debug Designer Sample. Browse
rows, expand/cancel, select from either view and switch tabs. To edit the new body,
open GalleryStylesStripControl.cs in its ordinary UserControl designer. The inline
selector is now offered in Edit Ribbon and Properties by the
[designer increment](#inline-gallery-designer-class-selection--2026-11-03).
The following QAT increment reuses the source dropdown selectors through an
explicit opt-in, retaining the compact QAT launcher. Separate custom bodies in
temporary group/minimized-tab hosts are implemented in the later correction record.

```powershell
dotnet run --project winforms/samples/RibbonKit.WinForms.DesignerSample -c Debug --no-build -- --verify-inline-gallery-presenter winforms/artifacts/inline-gallery-presenter/consumer-final
```

## QAT gallery popup increment — 2026-11-02

Implemented by user direction after acceptance of the ordinary inline gallery.
The compact QAT launcher opens the existing dropdown-gallery UserControl class;
no second application presenter class, QAT tile-strip mode or custom toolbar body
was introduced. Temporary group/minimized-tab bodies and inline class discovery
remain outside this increment.

- `RibbonGalleryItem.UseDropDownPresenterInQuickAccess` defaults false, with a
  Changed event and normal Behavior/default-value metadata. True uses the source's
  existing DropDownPresenterType or DropDownPresenterFactory; both null still
  selects the built-in popup. The inline selectors are independent. Four native
  Unshipped entries cover this bool/event and context.IsQuickAccess; no interface
  member or shipped API was changed.
- Popup creation records the actual QAT owner/opener chain, including direct or
  duplicate references, overflow, nested QAT menus and borrowed-group routes.
  Simply having a QAT reference does not mark an ordinary opening as QAT.
  context.IsQuickAccess is immutable route metadata; Presentation remains DropDown.
  The canonical source supplies the original tiles, selection and event handlers.
- Each accepted opening creates its own popup-owned control, with the existing
  constrained sizing, keyboard/child-input routing, reveal/scroll, capture and
  synchronized card/shadow fade. Cancellation creates no view. Reopening or using
  another launcher creates a fresh view; no source tile or inline control is moved.
  The existing one-expanded-popup-per-source policy remains.
- Changing the QAT policy closes only this source's QAT gallery popup. The next
  opening resolves the current choice; ordinary popup selection and retained
  inline controls survive the policy change. Construction and activation recheck
  the policy after application callbacks. Closing, rebinding/removing a reference,
  source disposal and validation-time policy changes retire stale requests. A
  commit guard rejects reentrant activation through the shared popup presenter
  path. Preview, programmatic selection and accepted actions remain distinct.
- The source opt-in is ordinary serialized application configuration, excluded
  from customization JSON with presenter/view state. Properties and authored code
  can set it; the Edit Ribbon draft bool entry remains part of the later authoring
  slice. The Designer Sample declares a source opt-in and a Document styles QAT
  reference with KeyTip 3, reusing GalleryStylesControl. Its user-authored gallery
  column/row settings and existing card controls were preserved.

Verification, **2026-11-02**:

- Normal Debug runtime/API/XML and native designer server/client builds passed.
  Native solution Release build passed with zero warnings/errors. The independent
  Designer Sample package refresh and Debug build passed with zero warnings/errors.
- **12 new QAT cases passed across focused scopes**:
  `tests/RibbonKit.WinForms.Tests/TestResults/qat-gallery-presenter.trx` (10/10)
  and `qat-gallery-simplified-boundary-final.trx` (2/2) in the same folder.
  **45 affected existing cases passed**, `qat-gallery-presenter-related.trx` in
  the same folder: ordinary popup/type selection, built-in inline/QAT behavior,
  QAT menus/groups and ownership. These are 57 distinct focused cases across these
  selections, not a full-suite run. New coverage includes default compatibility,
  type/factory routing, inactive tabs, actual overflow ancestry, nested/group
  routes versus ordinary menus, policy replacement, cancelled/reentrant opening,
  validation, exactly-once activation, source/reference disposal, stale callbacks,
  child keyboard/wheel, capture and nondefault CodeDOM bool serialization.
  Simplified More Commands shares internal QAT reference/overflow types, so QAT
  classification uses actual collection membership and the opener chain rather
  than CLR type. The two extra cases cover ordinary Simplified inline commands
  and More Commands/full-group openings without a QAT opt-in or QAT context flag.
- The public-API package-only native check passed at actual **120 DPI** in
  `artifacts/qat-gallery-presenter/consumer-final/`, using package
  `0.1.0-designer.local20261102T105123362Z`. It verifies the authored QAT reference
  and existing popup class, native child-label click, queued keyboard/KeyTips,
  two controlled commits, fresh views, independent inline/card handles and row,
  inactive-tab behavior, policy reset and built-in fallback. The fixture switches
  to bounded TabRow mode to force overflow and verifies the overflow parent exists
  before selecting its gallery row. The joined BelowRibbon toolbar uses its full
  row and was not proof of overflow. Direct, last-row and true-overflow PNGs were
  inspected. These are fixture-driven native events, separate from physical input.
- An initial refresh/check overlap opened the prior sample and locked its output;
  the task-owned check was stopped, the failed sample build completed, and the
  user-requested fresh package refresh passed. An early native retry also needed
  to exit the parent KeyTip layer after Escape before starting another F10 gesture.
  Those incomplete attempts are not passing evidence; no runtime KeyTip behavior
  was changed. Previously successful unrelated scopes were retained.
- Actual VS designer/save-reopen review, physical pointer/keyboard, mixed-monitor
  DPI, RTL and perceived motion/performance remain separate gates. Existing
  accessibility providers/bounds are reused; no new gallery UIA patterns or full
  accessibility certification is claimed. No full suite or WPF checks ran.

Review the third **Document styles** button in the normal Debug Designer Sample's
BelowRibbon QAT, or press F10 then 3. It should open the same custom popup class
as the ribbon gallery, including while another tab is selected. Selection from
either route updates the same gallery. The subsequent temporary-host continuation
is recorded below.

```powershell
dotnet run --project winforms/samples/RibbonKit.WinForms.DesignerSample -c Debug --no-build -- --verify-qat-gallery-presenter winforms/artifacts/qat-gallery-presenter/consumer-final
```

## Popup card rendering and temporary inline hosts — 2026-11-02

**User accepted the popup-card rendering and collapsed/QAT group-body corrections on 2026-11-02.** The
user accepted ordinary dropdown/split UserControl popups and reported two gallery
defects: clipped native previews in the dropdown's six authored cards and built-in
gallery bodies inside collapsed/QAT group hosts.

- Reproduced the exact dropdown clipping in the package-only Designer Sample at
  120 DPI. The first six cards had 40-pixel paper panels while later dynamically
  created cards had 63-pixel panels. The popup example's root was applying another
  font-scaling pass to physical layout supplied by its presenter. Its root now
  uses AutoScaleMode.None, matching the inline example. Native nested cards keep
  their ordinary designer scaling; all 18 popup panels are now 63 pixels high and
  contain their preview labels. The accepted inline card appearance is preserved.
- Custom inline bodies now have fixed ribbon or popup-session slots. Ordinary
  source-group collapse, collapse while that group has a QAT reference, direct
  QAT groups, QAT overflow groups and minimized-tab bodies use the existing inline
  selectors. Each temporary host owns a fresh UserControl and context; closing it
  disposes its native tree and invalidates callbacks. The retained ribbon view is
  never moved or disposed by this handover. Its visibility follows the existing
  group/layout ownership rules. Null selectors preserve the movable built-in path.
- Temporary contexts report Presentation=InRibbon and the host's actual QAT
  ancestry. A QAT reference elsewhere does not mark an ordinary collapsed group
  as QAT. Expanding a body uses the existing dropdown selectors and QAT opt-in;
  cancel restores the same temporary view, independently browsed row and focus.
  Input dispatch, validation, preview, commit, native accessible bounds, motion
  capture and parent-menu Tab navigation use the shared inline host. No public
  API, source item cloning, new persistence state or alternate popup policy was
  added. General customized command-copy presenter forwarding remains outside
  this source-group increment.

Verification, 2026-11-02:

- Normal Debug runtime and package-consumer builds passed with zero warnings or
  errors. The decisive focused run passed **80/80**: 17 new temporary-host cases,
  16 existing inline cases, 20 expanded-presenter cases, 12 QAT-presenter cases,
  11 popup-motion cases and 4 QAT-group cases. A separate built-in gallery/reduction
  selection passed **13/13**. These are 93 distinct cases across two runs.
  Earlier fixture failures were corrected before the 80-case pass; they are not
  counted as additional successful cases. A reused or failing factory must unwind
  the temporary host without disposing the retained ribbon body. Native QAT group
  borrowing can keep that body parked until its reference is removed.
- Evidence: `TestResults/gallery-temporary-related.trx` and
  `TestResults/gallery-temporary-builtin-related.trx`. Initial/scoped diagnostic
  runs remain beside those results. No new full-suite campaign was run; the prior
  full-run limits remain in the ordinary dropdown content plan.
- Independent package-only check passed at actual **120 DPI**:
  `artifacts/gallery-hosts/consumer-final/result.log`. It verifies all authored and
  dynamic popup cards on the initial and last pages; real collapsed-group pointer
  opening with/without a QAT reference; direct/overflow QAT group KeyTips; a
  minimized-tab body; native child-label commits; F4/Escape and focus/row
  restoration; an inactive source tab; original permanent HWND and 18 tiles; and
  three exactly-once commits. Source popup, collapsed-group, QAT overflow-group
  and minimized-tab PNGs were inspected. Before/fixed clipping geometry and
  captures remain under `artifacts/gallery-hosts/{before,card-scaling-fixed}`.
- The accepted ordinary dropdown/split package consumer also passed with Subtle
  motion against the corrected library:
  `artifacts/gallery-hosts/retained-dropdown-check/result.log`.
- Normal Release solution build and final Debug runtime/designer/client builds
  passed with zero warnings/errors. The permanent Designer Sample was refreshed
  in place to `0.1.0-designer.local20261102T121407933Z`; the complete gallery-host
  check passed against that final package too:
  `artifacts/gallery-hosts/consumer-package-final/result.log`.

Actual Visual Studio authoring/save-reopen, physical pointer/touch/IME, RTL,
mixed-monitor DPI and external accessibility
remain separate gates. Existing motion policy is reused; this is not a new
perceived-motion acceptance claim.

## First Gallery-tab opening — 2026-11-02

**Measured and optimized in the Designer Sample; user review pending.** The user
reported a noticeable delay on the first visit to the Gallery tab. Fresh-process
profiling separated native initialization, presenter synchronization and the
existing Expressive/SlideDown transition at actual 120 DPI.

The first visit created native windows for all 18 inline cards and their children:
91 controls, including offscreen rows. The presenter constructor itself took about
2 ms. The dominant avoidable cost was native window creation during synchronization.
Disabling motion still reproduced a first-use delay, so the delay was not solely
the chosen animation duration.

The sample now hides newly connected cards before parenting and makes only the
current row visible. Offscreen cards keep their managed objects, bindings and
source identities; their native windows are created when first shown, then retained
through later row browsing. First visibility completes normal native autoscaling
before physical presenter bounds are applied. The three designer-authored cards
remain intact. This changes sample presentation code only; no library API,
initialization timing contract, eager preload or animation policy was changed.

| Fresh-process scope | First tab selection | Presenter synchronization | Native windows |
| --- | ---: | ---: | ---: |
| Before, Expressive/SlideDown | 243.08 ms | 130.74 ms | 91 |
| After, same motion | 123.69 ms | 15.60 ms | 16 |
| After, second fresh process with additional timing probes | 108.08 ms | 12.52 ms | 16 |
| Before, motion disabled | 122.51 ms | 88.45 ms | 91 |
| After, motion disabled | 103.96 ms | 25.40 ms | 16 |

These are local diagnostic measurements, not a performance guarantee. The first
three authored cards account for the retained 16-window initial tree; only two
cards are shown. First-use framework/capture work and the requested motion still
contribute to total opening time. The improvement does not make cold and warm
visits identical. Logs are under `artifacts/inline-gallery-first-tab/` in
`before-{default,no-motion,builtin}`, `after-{default,no-motion,detailed}`.

Normal Debug sample builds passed with zero warnings/errors. The existing
package-only inline consumer now verifies that an unvisited row has no native
window and returning to a visited row preserves the same card handles. Its source,
selection, preview, child-click, keyboard, expansion/cancel and disposal checks
passed: `artifacts/inline-gallery-first-tab/consumer-inline/result.log`.
The complete collapsed/QAT/overflow/minimized-host consumer also passed:
`artifacts/inline-gallery-first-tab/consumer-hosts/result.log`. Initial inline and
collapsed-group native captures were inspected. The current library package is
unchanged; normal Debug consumer output is rebuilt. No full-suite or Release
campaign was rerun for this sample-only correction. Actual VS and physical
mixed-monitor/IME review remain separate.

## Inline and QAT gallery presenter contract — proposal, 2026-11-02

**User directed continuation on 2026-11-02; ordinary inline and QAT popup slices recorded above.**
The complete target contract below includes later, still-unimplemented slices.
Its original inspection used the clean `codex/Ribbonkit-Winform` worktree at
`d8d893b4c1a53cc66e1da636543609150fdf88ff` (`Gallery Usercontrol custom presentation
pop up`). Inline selectors, the QAT opt-in and context.Presentation/IsQuickAccess
are now available, including the temporary source-group/minimized bodies above.
Inline designer discovery is implemented; customized command-copy presenter
forwarding remains a later slice.
The accepted popup entrance, designer-editable cards and earlier focus, native
editor and backdrop fixes remain the baseline. Ordinary dropdown/split content
hosting is tracked in [its current plan](DROPDOWN-CONTENT-PLAN.md); the separate
inert popup designer preview is implemented in [its own plan](POPUP-DESIGNER-PREVIEW.md).

### Presentations and selector boundaries

| Presentation | Proposed body and settings | View owner and lifetime |
| --- | --- | --- |
| Inline ribbon body | Opt-in `InRibbonPresenterType` or `InRibbonPresenterFactory` when `IsInRibbon` actually receives a strip slot. Null for both retains the built-in strip. | Ribbon-owned control for that source/projection slot; retained while the source remains retained by this ribbon. |
| Expanded gallery popup from that body, or an ordinary gallery dropdown | Existing `DropDownPresenterType` or `DropDownPresenterFactory`; independent of the inline selectors. Null for both retains the built-in popup. | Fresh popup-owned control after every accepted opening, disposed on close. Never the inline control. |
| Direct gallery reference on the QAT toolbar | Keep the existing compact gallery launcher, caption/icon, KeyTip, sizing and overflow route. It is not a tile viewport and gets no UserControl in this proposal. | Existing QAT reference borrows the source; no custom body is constructed until an eligible popup opens. |
| Gallery popup opened through QAT | Use the source's existing dropdown selectors only when `UseDropDownPresenterInQuickAccess` is true. Its default is false, preserving existing built-in QAT behavior even on sources with a custom ordinary popup. | Fresh popup-owned control per accepted opening, including duplicate references, overflow and inactive source tabs. |

A gallery strip inside a collapsed-group or minimized-tab popup is an **inline
body in a temporary host**, not an expanded gallery popup. A QAT group flyout can
also contain such a body: it uses the inline selectors and has its own control.
Expanding that body uses the QAT-popup opt-in. A reduced gallery dropdown button
has no inline control; opening it follows the appropriate dropdown route.
The existing layout decides when a strip is available; this contract does not
add a new Simplified layout, reduction mode or QAT tile-strip mode.

QAT ancestry must follow the complete opener chain, including nested menus and
borrowed groups, rather than testing only the immediate owner. Direct ordinary
openings and body expansion must not acquire QAT behavior merely because another
QAT reference points to the same source. QAT-only differences remain a routing
choice on the source, with no presenter overrides on individual QAT references.

### Proposed public surface and compatible defaults

```csharp
// Implemented first-slice additions to RibbonGalleryItem.
public Type? InRibbonPresenterType { get; set; }                // default null
public event EventHandler? InRibbonPresenterTypeChanged;
public Func<Control>? InRibbonPresenterFactory { get; set; }    // default null
public event EventHandler? InRibbonPresenterFactoryChanged;
// Implemented QAT addition.
public bool UseDropDownPresenterInQuickAccess { get; set; }     // default false
public event EventHandler? UseDropDownPresenterInQuickAccessChanged;

// Implemented context metadata.
public enum RibbonGalleryPresentation { DropDown = 0, InRibbon = 1 }
// Add to RibbonGalleryPresenterContext:
public RibbonGalleryPresentation Presentation { get; }
// Implemented QAT route metadata.
public bool IsQuickAccess { get; }
```

Use the existing `IRibbonGalleryPresenter` without adding interface members or
changing its method signatures. A class can serve both body kinds by inspecting
`Presentation`, or an application can author separate ordinary UserControls.
`IsQuickAccess` describes the view's opening/host route, including a QAT group
body; it does not identify a different model. Existing ordinary popup contexts
report `DropDown` and false. No borrowed-Control property, retained-popup option,
QAT-toolbar presenter, general content host or context-aware factory is proposed.

The new inline type/factory pair is mutually exclusive **within that pair**;
clear one before setting the other. It is independent of the dropdown pair:
both an inline type and a dropdown type may be assigned. Retain the existing
dropdown pair's exclusivity. `UseDropDownPresenterInQuickAccess = true` with both
dropdown selectors null still uses the built-in popup; it never falls back to
the inline selector. Setting inline selectors does not turn `IsInRibbon` on.

Type eligibility matches the existing dropdown selector: public, concrete,
closed UserControl implementing `IRibbonGalleryPresenter`, with a public
parameterless constructor. Runtime factories keep `Func<Control>` compatibility
and must return a fresh, unparented, undisposed UserControl implementing that
interface. A factory may supply constructor arguments. Reject controls already
owned by any active or parked view, even if Parent is null. Application icons,
fonts, renderer and source items remain borrowed under their existing contracts.

Attached setters run on the ribbon UI thread, validate before changing the configuration,
and raise their Changed event only for a real change. Inline type metadata uses
Category Data and DefaultValue(null); the factory is non-browsable and hidden
from designer serialization. The first runtime slice also hides the inline Type
from Properties until its dedicated class picker is implemented. The QAT opt-in uses Category Behavior and
DefaultValue(false). Invalid types/conflicting selectors leave the prior choice
and live views intact. No constructor runs when assigning a type.

### One model, separate controls and explicit lifetime

The canonical source gallery owns Items, selection, availability, bindings and
application event handlers. Resolve QAT/structural routing to that source before
creating a context. Bind views to its original tiles; do not clone items, command
state or bindings. Separate native card controls referring to those tiles are
views, not new models. Each simultaneous body or popup has a distinct root
Control, context, focused item, pointer state, preview-owner token and scroll
position. The same source still has at most one expanded gallery popup under
the existing popup-manager rule; opening it from a different launcher replaces
the previous popup rather than introducing parallel popup sessions.

| Transition | Proposed behavior |
| --- | --- |
| First visible inline slot | Construct lazily after the runtime layout allocates a nonempty slot, synchronize and attach to that slot's library-owned viewport. No construction for hidden tabs, reduced buttons, QAT launchers or design previews. |
| Inline resize, selection, item or display refresh | Reuse the control and its children. Synchronize from source; do not reconstruct the view or create a second event subscription for each refresh. |
| Tab hidden, clipped away, ribbon minimized or Backstage opened | Clear press/capture, cancel only that view's preview and invalidate its input context. Retain a permanent body hidden in its own ribbon host; it cannot receive focus or input. Resume with a new context and reconcile accumulated source changes. |
| Expanded popup or modal route temporarily blocks a visible body | Keep its real control at the same bounds and allow it to remain drawn behind the popup. Cancel only its preview, clear press/capture and invalidate input context. Reactivation supplies a new context without recreating the control or resetting its row. |
| Group/minimized-tab/QAT-group body slot opens | Create a separate session-owned inline view after accepted host opening. A permanent ribbon body may remain alive simultaneously. No transfer of its control into the flyout. |
| Temporary body host closes | Invalidate context, cancel its preview, detach subscriptions and dispose its owned control/children before the host is destroyed. Reopening that host creates another view. |
| Expanded gallery opens/closes | Preserve the accepted lazy/fresh popup lifetime and creation after DropDownOpening. Cancellation creates no popup view. Closing disposes only the popup view; the inline body and source survive. |
| Selector replaced/reset or `IsInRibbon` cleared | Validate first; then retire affected contexts and dispose the affected views. Recreate lazily for the next eligible slot/opening, or use the built-in path after reset. Never mutate a live custom body into another control in place. |
| QAT opt-in changes | Close an affected QAT gallery popup and invalidate its context; the next opening resolves the new policy. Preserve ordinary popup and inline configuration. |
| QAT reference removed/rebound | Close sessions belonging to that reference, including descendant bodies/popups. Do not dispose the borrowed source, another reference's view or the permanent body. |
| Source detached from this ribbon, no longer retained, disposed, or ribbon disposed | Invalidate all its contexts first, release preview/capture/focus and dispose every owned view and subscription. A retained inactive/merged source is not a final detach. Reattachment after final detach starts fresh. |

An inline slot is keyed by canonical source, authored projection and owning
container/session identity. A permanent slot stays with its ribbon host; a
temporary slot stays with its popup session. Controls are never reparented
between these slots, even when their visibility alternates. This deliberately
differs from the existing built-in strip manager's one movable control per
source. Keep the built-in path's current behavior when no custom body is chosen.

Changing the inline selector pair retires inline views only; changing the
dropdown pair closes affected expanded gallery views, including opted-in QAT
popups, without replacing inline controls. Clearing `IsInRibbon` disposes inline
views and retains the gallery's dropdown configuration. If a retired inline view
is the opener of an expanded popup, close that descendant session first so it
cannot retain a dead opener.

Hiding a source retains an already created permanent body but makes it inactive;
disabling it retains its visuals, rejects activation and cancels preview. Removing
it from all retention paths disposes the body. On temporary suspension, preserve
the browsed row if selection has not changed: cancelling expansion must not
silently reset the inline position. A selection change while hidden is marked
for reveal on resumption. No eager initialization before Form.Load is promised.

Detach the context and stop library callbacks before invoking disposal or any
cancellation callback that can reenter application code. Dispose only accepted
owned controls and fresh invalid results; never dispose a rejected parented or
already owned control borrowed from another view. Failed construction or initial
synchronization must unwind the partial host and subscriptions without altering
source selection. Report the failure through the existing UI-thread error path;
do not silently substitute a built-in view or repeatedly invoke a failing factory
from Paint. Retry belongs to a new presentation request or configuration change.
Presenters release their own application subscriptions/resources in Dispose and
must not dispose the borrowed source or assets.

### Context, selection, preview and accepted activation

Generalize the context's internal host connection to a view, while retaining its
public Preview, FocusItem, Commit and NotifyViewChanged signatures. Display
settings are read-only snapshots for an attachment period. Reissue the context
on reactivation or font/theme/renderer/density/DPI changes; the old context stays
inert permanently. Reuse the control, not the context. `Viewport` can change
within that period during resize. Synchronize before measurement and again after
the final viewport is supplied. All calls remain on the owning ribbon UI thread.

`IsActive` means the context is current, attached, effectively visible/enabled,
and permitted to interact in the current popup/Backstage/modal route. A lower
inline view cannot accept input through an expanded popup. Preparation contexts
are inactive; callbacks during Synchronize are rejected even if IsActive would
otherwise be true. Stale Preview/FocusItem/Commit return false and stale
NotifyViewChanged does nothing after close, hide, replacement, detach or disposal.
Requests must also recheck source/tile ownership, availability and host generation.

- `SelectedItem` is shared authoritative state. Synchronize every realized view
  after its change, reveal the available selection without stealing keyboard
  focus, and mark hidden retained views for reveal. Assigning it from code raises
  SelectedItemChanged only, never SelectionCommitted or a second application action.
  Removing a selected tile clears selection; hiding/disabling it retains authored
  selection but prevents activation, matching the existing gallery model.
- Focus and scroll are per view. FocusItem reveals and moves logical/native
  gallery focus without selecting or previewing. Synchronize/Reveal/ScrollView/
  NotifyViewChanged must not execute application commands or change selection.
- The source has one active temporary preview with a view-owner token. A new
  valid view request takes that ownership and reports the requested preview;
  transfer does not insert a cancellation between successive previews. A stale
  view's leave, close or Preview(null) cannot cancel the new owner's preview.
  Source mutation/programmatic selection can cancel the current preview globally.
  Successful commit ends preview without ItemPreviewCancelled; the application
  continues to own document preview and rollback.
- Commit is one accepted activation through RibbonKit's gallery activation and
  validation route, including the already-selected tile. Validate the focused
  library editor and applicable native focus transition once, then recheck context,
  host/source and tile after reentrant application callbacks. A cancelled validation
  leaves the view open/focused with no selection commit, command or dismissal.
  Removal, replacement or disposal during validation makes the request stale.
- Guard activation against reentrant delivery. One accepted pointer/key/UIA gesture
  produces at most one SelectionCommitted and one existing application action;
  later distinct activations may commit that same tile again. The presenter must
  not also call PerformClick, invoke handlers or execute an application command.
  Gallery actions continue through the existing source event contract; no new
  tile Click/command-dispatch semantics are introduced by custom presentation.
  Teardown on disposal/removal cannot be blocked by validation and never commits.

Popup activation retains the existing dismissal policy and returns focus to its
actual opener: inline body, QAT launcher, overflow row or parent flyout, if still
available. Cancelling expansion restores the retained inline body's row and focus.
Inline body activation leaves the ribbon body available; activation in a temporary
body follows the existing group/tab popup dismissal policy. QAT activation never
selects the source tab. If an opener disappears, use the existing ribbon/host
focus fallback rather than keeping focus in a disposed native child.

### Sizing, scrolling and display changes

The library owns strip frame, equal-height up/down/expand buttons and their inset,
popup frame/shadow/placement, popup scroll controls, clipping and accessible bounds.
The application owns the body layout, native card visuals and application assets.
All interface rectangles/points are physical pixels relative to the root custom
control, including scroll offset; library hit testing intersects them with the
actual viewport and rejects out-of-view tiles.

- Inline geometry remains allocated by the existing ribbon layout: authored
  TileWidth/InRibbonColumnCount, density/body height and reduction determine the
  slot. The custom body fills only the interior viewport; GetPreferredSize or a
  native MinimumSize cannot enlarge the ribbon or move its utility buttons.
  InRibbonColumnCount remains a width/layout hint, not a mandate to reproduce
  the built-in grid inside a custom control. A zero-area slot has no active view.
- Popup sizing retains the existing Synchronize → GetPreferredSize(limit) → final
  constrained viewport sequence. Use context.Dpi before parenting. Native minimum
  sizes are preferences subject to screen/slot limits: clip an oversized control
  inside a library viewport and require the presenter to make its tiles reachable.
  Do not add popup resize handles, new sizing properties or automatic layout
  inference from the application's child controls. ColumnCount/MaxVisibleRows and
  tile metrics remain available source hints, not forced custom-layout dimensions.
- Up/down and wheel dispatch RowBackward/RowForward; PageUp/PageDown navigation
  and ScrollView page requests use the custom layout's actual viewport. Define a
  row using that layout, not built-in tile indexes. CanScrollBackward/Forward must
  match clamped endpoints. One owner handles each wheel gesture, including native
  descendants; there must not be both native AutoScroll and a library row step.
- Reveal makes an available tile reachable without committing, previews or focus
  theft. On first presentation/reopen reveal selection; thereafter reveal on actual
  selection/focus changes, not every Synchronize, preserving independent browsing.
  Internal asynchronous layout/scroll changes call NotifyViewChanged to update
  scroll buttons, hit bounds and accessibility. No gallery items still yields a
  valid empty viewport with disabled scroll and no commit target.
- Reflow retained controls on font, theme/renderer, Compact/Touch, resize and DPI
  changes without replacing native children. Clamp scroll and preserve a source-tile
  anchor where possible, then reveal keyboard focus if needed. Replace display
  context before remeasurement, use current DPI once rather than scaling already
  scaled bounds, and clip every painted/hit/accessibility layer consistently.
  Do not force new handles merely to measure. Existing popup close/reopen policy
  on host/DPI changes remains authoritative; do not add retention to bypass it.

### Native input, focus and accessibility responsibilities

RibbonKit remains the single keyboard dispatcher for the view root and its native
descendants. Arrows/Home/End/PageUp/PageDown call Navigate using actual custom
layout, then FocusItem/Preview; Enter/Space request one commit. Root pointer input
is library-routed. Child pointer handlers translate to root coordinates and use
the context, with matching left-button down/up on the same available visible tile,
capture-loss/outside-release cancellation and no right-click commit. Native Click
handlers must not dispatch the same activation again. Stationary pointer repaint
messages must not overwrite a keyboard preview.
If a selectable native child already attempted focus and validation rejected it,
abandon its press; do not call FocusItem again for that failed attempt. If an
explicit FocusItem request returns false, abandon that gesture before Commit.

Inline Tab/Shift+Tab leaves the gallery for the containing ribbon/flyout navigation;
F4/Alt+Down and expand open its gallery popup. Inline Escape cancels its preview
and returns focus to the gallery command. Expanded-popup Tab/Shift+Tab retains
the current popup item navigation contract; Escape dismisses one popup level.
Library utility buttons participate through the existing focus route without
adding independent tab stops. There is one native gallery focus entry with a
logical focused tile; application tile children do not add competing tab stops
or a second keyboard dispatcher. This proposal covers native tile/card controls,
including panels, labels and activation buttons. It does not introduce arbitrary
embedded text/IME editors or nested native dropdowns with conflicting key rules;
those need a separate input-negotiation contract. Existing ribbon editors outside
the gallery retain their validation, native state and ownership.

The library must expose the source tile identity, clipped bounds, availability,
selection/focus and accepted activation for **each view**; automation identity
must distinguish the host slot as well as the source tile. Retained hidden bodies
are offscreen/noninteractive; disposed popup providers become unavailable cleanly.
The application supplies meaningful names/descriptions and native child
accessibility for its visuals. Decorative panels/labels must not introduce a
second selectable/invokable copy of a tile. Any custom child activation provider
uses the same context validation route. Native UserControl accessibility alone
does not prove gallery parity: focused inspection must check the combined native
child and library tree for duplicate actions, focus and selection reporting.
This proposal neither claims full gallery UIA patterns nor accessibility
certification; the exact provider integration is an implementation verification
gate, not evidence inherited from the accepted popup cards.

### Motion, designer and persistence integration

Include the custom bodies and their native children in existing library motion
capture while preserving the one real control per view. Motion images are inert;
they cannot focus, preview, commit or create another source/control. Keep the
accepted complete-card/shadow fade and item slide on the same clock, fixed native
bounds and input/policy settlement. Keep existing native editor handles, backdrop
composition and focus-return behavior. No application timer, separate custom-body
animation, new capture API or view caching for expanded popups is part of this
contract. Verify panels/labels/buttons in capture; controls that cannot be captured
must use the existing immediate/failure settlement path, not an empty animated
frame or a live-control clone.

Later designer work can reuse metadata-only class discovery, retained choices,
detached draft/Cancel, single-transaction OK, Undo/Redo and CodeDOM typeof
serialization for InRibbonPresenterType. Preserve runtime factory conflicts
instead of silently clearing application delegates. Serialize the QAT opt-in as
an ordinary nondefault bool. Selecting classes, validating drafts and inert ribbon
previews must never construct or capture application UserControls. Body authoring
stays in ordinary UserControl designers; preserve GalleryStyleTileControl's real
panels/labels and GalleryStylesControl's six visible example cards. A compact
inline sibling can reuse that card authoring approach without rewriting the
accepted popup example into a dense custom-paint loop.

Designer source serialization and customization persistence remain distinct.
Presenter types/factories, controls, contexts, HWNDs, focus, preview, cached bounds
and scroll offsets stay out of customization JSON; keep the QAT opt-in with the
application's authored configuration as well. Existing stable source/reference
IDs continue to drive customization and QAT routing. Add no persisted view state
or new customization schema for this extension.

### Proposed bounded implementation sequence and review decisions

1. **First suggested slice: inline runtime in the ordinary ribbon body.** After
   contract review, add only the inline selectors/events and context metadata used
   by that slice, the owned fixed-slot host and source-mediated input. Demonstrate
   a normal designer-editable strip UserControl alongside the existing popup:
   distinct simultaneous controls, shared original tiles/selection, independent
   scrolling, expansion/cancel/commit, retained hide/show and owned disposal.
   Keep QAT custom popups and designer class-picker changes for later slices.
2. **QAT gallery popup opt-in — implemented above.** Add the bool/event and resolve dropdown
   selectors through direct, duplicate, overflow, nested and inactive-tab routes.
   Retain the compact toolbar launcher and one expanded popup per source. Verify
   default-false compatibility and per-opening disposal separately from bodies.
3. **Temporary inline hosts.** Add separate controls in collapsed groups,
   minimized-tab bodies and QAT group flyouts. Verify permanent/temporary views
   coexist, popup teardown never disposes the permanent view, and no live body
   is moved between hosts. Preserve default strip behavior.
4. **Designer authoring — implemented.** The
   [inline class picker](#inline-gallery-designer-class-selection--2026-11-03)
   extends the existing metadata-only selection path, with generated-code/Undo/Redo
   checks and an in-place package-consumer refresh. Actual VS review remains separate.

Each slice should add the native API/XML baseline entries it actually implements,
not reserve the entire proposal in advance. Use normal Debug outputs and direct
native focused selections, not the WPF harness or the broad Test-Native.ps1
campaign by default. Relevant existing scopes are GalleryPresenterTests,
InRibbonGalleryTests, GalleryReductionTests and the affected QAT cases; motion,
editor, theme/density and designer cases are included only when their paths change.

New focused coverage must establish lazy/failed creation; selector reset/conflict;
separate identities/parents; retention/disposal/subscription cleanup; stale and
reentrant callbacks; preview ownership; selection versus commit/action counts;
validation cancellation; row/page/reveal endpoints; child pointer/keyboard routing;
and constrained font/density/DPI geometry. Verify the implemented slice in a
package-only consumer without Showcase helpers, including default-presenter
compatibility. Inspect combined accessibility bounds/actions and motion capture
for any changed host. Actual VS, physical input, mixed-monitor DPI, perceived
motion/performance and accessibility certification remain separately reported
gates. Do not rerun unrelated or already successful historical scopes for
repetition. The original contract-only session ran none of those checks; the
implemented first-slice evidence is recorded separately above.

Recorded contract review decisions:

- **QAT representation:** recommend retaining the compact launcher and customizing
  its opened gallery only. An embedded QAT tile strip/UserControl would require
  an additional toolbar sizing, overflow and accessibility contract outside this
  proposal; confirm if that is the intended eventual presentation instead.
- **QAT compatibility switch:** recommend default-false
  UseDropDownPresenterInQuickAccess using the existing dropdown selectors, with
  no per-reference override. Review the name and explicit opt-in rather than
  silently changing how existing DropDownPresenterType assignments behave.
- **Inline lifetime and geometry:** recommend retained fixed-host permanent bodies,
  session-owned temporary bodies and layout-allocated inline size. Review that
  tradeoff against reusing one movable control or allowing application preferred
  sizes to drive ribbon reduction; neither alternative is assumed authorized.

The four runtime/authoring stages above are implemented; their separate review
gates are recorded in the corresponding increments. Inert popup designer preview
is now implemented separately in the [popup preview plan](POPUP-DESIGNER-PREVIEW.md).
Arbitrary embedded editors, custom QAT toolbar bodies, new gallery UIA patterns,
general dropdown hosting/preview, new themes and unrelated refactors stay deferred.

## Purpose and current boundary

Allow an application to supply a custom gallery body, potentially an ordinary
visually designed UserControl implementing a small gallery-specific contract.
RibbonKit retains gallery chrome and behavior while the presenter supplies
custom item layout and visuals.

Existing galleries already support renderer overrides for DrawGalleryTile and
DrawGalleryHeading, and custom IRibbonIcon artwork. This proposal addresses an
entire custom content control. It does not replace the existing renderer hooks.
The current [gallery model and behavior](../README.md#galleries-in-dropdowns-the-ribbon-and-quick-access)
remain the compatibility baseline.

## Original communication goals and later presentations

The first dropdown increment above implements these goals. The
[inline/QAT proposal](#inline-and-qat-gallery-presenter-contract--proposal-2026-11-02)
defines their simultaneous-view and lifetime recommendations for review:

| Direction | Proposed responsibility |
| --- | --- |
| Gallery → presenter | Supply the existing tile declarations, authoritative selection and availability; refresh theme, font, DPI and viewport context. |
| Gallery → presenter | Request row/page scrolling, directional/Home/End navigation, and revealing an item. The presenter resolves movement using its actual layout. |
| Presenter → gallery | Report a hovered/focused item for temporary preview, request an accepted item commit, and cancel its preview. |
| Presenter → gallery | Report scroll availability and usable item bounds/hit regions for chrome, focus, keyboard integration and accessibility. |

For example, clicking the gallery's down arrow requests one row of presenter
scrolling. Clicking a custom tile requests a commit through the gallery's
existing activation path. Scrolling, temporary preview and committed selection
remain distinct operations.

## State and integration rules

- Keep one source gallery model for Items, selected identity, enabled/visible
  state and bindings. A custom presenter displays that state rather than owning
  an independent competing selection model. Existing programmatic selection
  changes raise SelectedItemChanged without becoming user commits.
- Preserve SelectionCommitted, ItemPreview and ItemPreviewCancelled semantics,
  accepted-tile validation and command execution exactly once. Preview cancellation
  belongs to the requesting view; one view must not cancel another's active preview.
- RibbonKit retains popup opening/closing, validation, dismissal and source/QAT
  routing. Presenter input enters that same behavior path. Settle keyboard/focus
  routing explicitly so the same input is not processed twice.
- Define item identity, bounds, hit testing and accessible selection alongside
  navigation. A custom UserControl alone does not establish gallery keyboard or
  accessibility parity. Preserve the existing default presenter in every mode.
- Viewport/scroll state is transient presentation state. Do not add it to ribbon
  customization persistence as part of this slice.

## Native control lifetime and ownership

Inline and expanded galleries can be visible together. Plan separate presenter
instances for different simultaneous presentations, sharing the source model.
Each native Control has one parent; do not move a single live Control between
simultaneous views or clone source items, bindings or command state to imitate it.

The [inline/QAT proposal](#inline-and-qat-gallery-presenter-contract--proposal-2026-11-02)
now specifies the creation, ownership, retention, replacement, detach and disposal
target. The ordinary inline body and QAT gallery popup opt-in are implemented
above; temporary custom bodies remain a later slice. Do not
silently inherit Backstage's Content/ContentType lifetime rules: its single
page-body contract differs from gallery presentations. Application assets remain
application-owned under their existing contracts.

## Original bounded delivery sequence

1. Review the smallest gallery-specific runtime contract and native API baseline
   additions, with compatible defaults and explicit ownership/creation timing.
2. Implement an opt-in **dropdown gallery** presenter and one example UserControl
   showing custom tiles, scrolling, hover preview and accepted selection through
   the existing source gallery. Keep the built-in path unchanged by default.
3. Verify the extension in an independent consumer using public APIs, without
   Showcase helpers or resources. Document the host's implementation obligations.
4. After that contract is verified, plan inline strips and QAT presentations with
   distinct view instances and shared selection/preview semantics. Designer
   presenter-class selection can follow the established runtime contract.

Do not include a general-purpose content-host framework, arbitrary replacement
data models, new themes, broad gallery/dialog restyling, new animation, localization
or RTL implementation in the first increment. Existing supported behavior still
needs regression coverage where the extension changes a shared path.

## Focused verification when resumed

Use normal Debug outputs and checks relevant to the actual change. Cover row/page
scroll boundaries and reported button availability; pointer and keyboard navigation;
selection versus preview/commit/cancel; programmatic selection reveal; hidden,
disabled, removed and disposed tiles; popup dismissal/validation; theme/DPI geometry;
and event delivery without duplicate execution.

Verify native ownership, subscription cleanup, factory creation/disposal and any
reuse policy, followed by independent-consumer behavior and applicable default
presenter regressions. Once inline/QAT are included, verify their simultaneous
view/state/focus interactions separately. Keep automated, generated-code/designer,
actual visual/native input and physical-DPI acceptance distinct. Preserve prior
passing evidence unless a change or unresolved risk warrants rerunning it.

## Dropdown popup designer and custom content — deferred addition, 2026-11-01

**Update, 2026-11-02:** the user approved retained ordinary dropdown/split content.
Stage 2's first implementation and current evidence are now in the
[retained content plan](DROPDOWN-CONTENT-PLAN.md). **2026-11-03:** Stage 1's inert popup
preview is implemented in the [popup preview plan](POPUP-DESIGNER-PREVIEW.md).
The following is the original proposal and decision checklist.

Recorded at the user's request alongside the gallery hosting proposal. Keep
ordinary dropdown/split-button menus and custom gallery presenters as distinct
contracts; they share native hosting concerns but have different content behavior.

### Current native capability

- Edit Ribbon already authors nested dropdown/split-button Items, including
  buttons, options, textboxes, comboboxes, separators and nested menus. There is
  no dedicated popup preview on the Form designer surface.
- Showcase's **Inputs → Edit in popup** uses RibbonTextBoxItem and
  RibbonComboBoxItem rows. It demonstrates library-hosted native editors, not an
  arbitrary application UserControl.
- RibbonDropDownItem currently exposes an owned Items collection. It has no
  arbitrary Control content or ContentType selection contract. Backstage's
  Content/ContentType feature does not implicitly apply to dropdowns.

### Stage 1: popup authoring access and designer preview

Reuse the existing Edit Ribbon nested-items editor. Consider an **Edit popup…**
contextual/smart-tag shortcut that selects the relevant dropdown branch, avoiding
a second competing editor for the same declaration. Preview dropdown and split
menus, their nested submenus, separators and static native field representations
using shared layout/rendering.

Preview clicks select authored components for Properties; they must not execute
commands, validate/commit runtime input or open the live runtime popup. Preview
open/close, menu navigation and selection remain transient and do not enter
generated code or the undo stack. Actual authoring retains the existing detached
draft, Cancel and single-transaction OK behavior. Determine the smallest feasible
surface-preview interaction contract before implementation.

### Stage 2: optional custom UserControl popup body

For richer layouts, design an ordinary UserControl separately and select its
class through the ribbon designer after a runtime hosting contract is agreed.
Also consider application-created instances assigned from code, following the
explicit ownership distinction used by Backstage. Exact public member names and
APIs remain proposals requiring review before implementation.

Settle these boundaries first:

- Keep owned Items as the compatible default. Specify whether custom content is
  exclusive with Items or may be composed with rows; do not silently choose or
  introduce both modes in the first increment.
- Define creation timing, eager initialization before Form.Load for declared
  classes, reuse across reopening, replacement/reset, detach and disposal. Review
  the eager-initialization choice explicitly; do not inherit a gallery's per-view
  factory lifetime automatically. Specify ownership of declared-type instances
  versus borrowed application instances.
- RibbonKit retains popup frame, placement, monitor bounds, DPI sizing, opening/
  closing, validation, dismissal and source/QAT routing. The application retains
  its content layout, data, bindings and business logic. Define preferred/minimum
  sizes, constrained-height scrolling and how content requests dismissal.
- Define focus entry/return and Tab, Escape, Enter and nested native ComboBox
  handling, including validation cancellation. Preserve native control handles,
  ownership, text and selection through reopening and routing changes. A live
  Control has one parent; settle any reuse or simultaneous-presentation policy.
- The ribbon designer selects the class and shows a placeholder/readout; body
  editing happens in the normal UserControl designer. Do not instantiate or
  capture arbitrary application content for an inert ribbon popup preview.

Verify the agreed runtime contract in an independent public-API consumer before
adding designer class selection and generated-code round trips. Include a small
UserControl example and relevant checks for initialization/ownership, reopening,
focus/validation/dismissal, source/QAT routing, native input state, constrained
geometry, existing menu compatibility, Undo/Redo and save/reopen. Separate actual
VS, visual/native input and physical-DPI review from automated evidence. This
stage does not require the gallery-specific tile/preview/scroll interface for
ordinary custom dropdown bodies.

This historical proposal adds no acceptance evidence. The linked retained-content
plan owns dropdown/split runtime evidence; the [popup preview plan](POPUP-DESIGNER-PREVIEW.md)
owns current designer-preview evidence.
