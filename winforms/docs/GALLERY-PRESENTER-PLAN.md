# Optional native gallery presenter and dropdown content hosting

Status, **2026-11-02: user confirmed the dropdown gallery presenter works**.
The user accepted the synchronized gallery fade/slide entrance on **2026-11-02**.
Gallery UserControl class selection in the ribbon designer is implemented with
focused and package-consumer evidence below; actual VS review remains pending.
The follow-up moves the Designer Sample hierarchy into designer-managed code;
the focused native check passed, while actual VS review of that relocation is pending.
The user selected the custom gallery UserControl presenter as the next slice.
The runtime contract and ownership decisions for this increment are recorded below;
the existing built-in gallery remains the default.
This is native RibbonKit.WinForms work; WPF runtime/build/CI remain outside scope.
The deferred dropdown popup designer and custom UserControl hosting proposal is
recorded separately below and remains deferred. Inline/QAT custom presenters
remain subsequent increments.

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

The current local sample package is
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

The first dropdown increment above implements these goals. Inline/QAT extensions
still need their own simultaneous-view and lifetime review:

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

A factory creating a presenter for each presentation is a candidate extension
point. Before implementation, explicitly settle who creates/disposes those views,
whether any borrowed-instance option is needed, creation timing (initialization
or presentation opening), reuse/caching, detach and source-disposal behavior.
Do not silently inherit Backstage's Content/ContentType lifetime rules: its
single page-body contract differs from gallery presentations. Application assets
remain application-owned under their existing contracts.

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

This deferred ordinary-dropdown record adds no acceptance evidence for custom
dropdown/split content or popup designer previews. The implemented gallery-only
increment and its current evidence are recorded above.
