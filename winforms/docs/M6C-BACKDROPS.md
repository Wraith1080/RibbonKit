# M6C native window backdrops

Status, **2026-10-30: investigation resumed by the user; visual acceptance pending**.
After branching the experiment, the user clarified that document flicker also
occurred before backdrops and restored M6C through merge `589e8d5`. This removes
the earlier deferral; it does not establish the cause or resolution of flicker.
M5E persistence was implemented first as requested. Resumed review corrections
and current evidence are recorded below.

## Native paint and HRESULT boundary follow-up — 2026-10-30

Current candidate addresses the user's rapid-dropdown exception, first-display
document backing, header utility tint and the newly recorded native text dropouts.
Human acceptance remains pending; earlier handler-only capture passes did not
establish that real pointer-driven drawing was stable.

- UIAutomationCore now receives a reserved native provider bridge with explicit
  [PreserveSig HRESULT signatures](https://learn.microsoft.com/en-us/dotnet/standard/native-interop/preserve-sig).
  A queued request for a closed popup returns `0x80040201` without throwing a
  managed exception inside the application. Provider events, navigation, patterns
  and disconnection use the bridge; only actual roots expose the fragment-root
  interface. Existing published managed interfaces remain unchanged. The eight
  new native ABI declarations are public because CLR COM export requires visible
  interface types; they have XML documentation, are hidden from IntelliSense and
  are recorded as 34 additive Unshipped entries, not application extension points.
- First-frame preparation runs synchronously in the original native show
  transaction, with direct native field warm-up before application Shown handlers.
  The candidate does not hide, cloak, re-show, animate or change Form opacity.
  A temporary cloak experiment was removed because it could consume the Windows
  opening animation. Original Windows appearance-animation acceptance remains
  with the user; the diagnostic now samples from first presentation, not 100 ms later.
- The user's `0934-46` recording contains a lower-document text loss near 7.57 s
  and a brief black startup content frame. `0935-39` contains repeated missing
  ComboBox values. The earlier `0643-26` clip was exception evidence only. These
  are distinct from the intentional gallery font previews in `0907-57`.
- `RibbonBackdropPanel` now uses opaque layered redirection without adding
  `WS_EX_COMPOSITED` to the application document tree. Ribbon field presenters
  retain native subtree composition but disable their second managed double
  buffer while that native buffer is owned; normal popup/Solid buffering returns
  on release. HWNDs, values and native editing are retained. The parent-OnPaint
  forced child repaint was removed after the reported regression. Temporary
  Showcase font/redraw batching was also removed; gallery previews keep their
  authored behavior. This is a shared native-host correction, not a RichTextBox
  replacement or recurring repaint timer. Microsoft's [buffer-completion explanation](https://devblogs.microsoft.com/oldnewthing/20171018-00/?p=97245)
  documents why adding composited buffering changes native drawing publication;
  it is supporting context, not proof of this machine's original failure cause.
- QAT overflow and ribbon collapse/expand hover/pressed fills now honor the
  existing `QuickAccessBackdropOpacity`; their icons remain opaque. Solid/popup
  fills keep their normal authored coverage. The default is still `.6`, not zero.

Focused evidence across separate runs: 11 native/managed provider-lifetime cases,
17 alpha cases and 13 editor/panel lifecycle cases passed (41 distinct cases).
Native COM-vtable tests require zero first-chance unavailable exceptions on both
threads. External-process UIA discovery, patterns, property events, disabled
actions, focus and popup dismissal also passed. Debug Showcase/native-test builds
passed with zero warnings/errors; no full-suite, Release/package or mixed-DPI
campaign was run.

Actual Mica pointer review (`artifacts/startup-native-20261030/pointer-document/`)
sampled Font/Name and every populated document band: 586 samples, zero detected
dropouts, with real pointer movement from Font into the document. Empty/occluded
baseline logs from the earlier `pointer-layered-only` and `pointer-single-buffer`
experiments are not acceptance evidence. The recorder now rejects blank field
baselines and avoids interpreting Zoom's ordinary ScreenTip occlusion as flicker.
The current Acrylic first-presentation/editor probe passed 16 startup and 36 hover
samples; the public-only independent Mica/Acrylic RichTextBox/status consumer also
passed after a Load resize. See `single-buffer-final-Acrylic/` and
`independent-single-buffer/` under the same artifact root. All owned review windows
closed and computer use stopped. Restart Showcase before reviewing this candidate;
the latest human reports supersede earlier broad flicker acceptance claims.

## Startup content, hover and provider follow-up — 2026-10-30

User review on 2026-10-30 confirmed that the earlier black title-area box appears
eliminated. The new reports concern black application content on restored-material
startup, random RichTextBox hover flicker and a stale-popup accessibility query.
Older pending black-box entries below describe earlier checkpoints.

- A fresh launch of the actual saved Mica profile reproduced black document and
  status surfaces without pointer overlap. An isolated actual Showcase reproduction
  then measured 100% black in both. Applying material without restoring saved bounds
  passed, so that simpler launch is insufficient regression coverage. The saved
  bounds are applied in Load after hidden composition was established. RibbonForm
  now finishes opaque-panel painting once after native show, following the Load
  resize. An earlier panel VisibleChanged repaint was insufficient and was removed.
  This uses the library's opaque host, retains native children and needs no sample
  hover handler, repaint timer or replacement document control.
- Popup surfaces no longer invalidate for movement within an unchanged hover
  target. Gallery preview and overflow repeat still receive pointer processing.
  Popup shadows no longer enter native positioning on every paint when bounds,
  visibility and sibling order already match; real moves and order repairs remain.
  Focused regressions first reproduced six invalidations for one unchanged item
  and five redundant native positioning calls. They now observe one and zero.
  These are bounded hover corrections, not acceptance of every random flicker.
- Queued accessibility reads return a lifetime result from the UI callback and
  raise `0x80040201` on the requesting client if the popup disappeared. The prior
  reproduction raised that COMException inside WinForms' reflected UI callback.
  Closed providers still report the [documented unavailable result](https://learn.microsoft.com/en-us/windows/win32/api/uiautomationcoreapi/nf-uiautomationcoreapi-uiadisconnectprovider);
  unrelated live-provider failures are preserved. Pattern/property race tests now
  require zero unavailable exceptions on the UI thread and retain the earlier
  no-ObjectDisposedException assertion for popup closure.

Verification: normal Debug Showcase and native-test builds passed with zero
warnings/errors. A total of 52 focused cases passed across provider lifetime,
popup/gallery behavior, native frame policy, retained editors and alpha rendering.
Actual Showcase diagnostics restored Touch/material/window placement from isolated
preferences: Solid/Mica/Acrylic each passed 16 startup samples beginning 100 ms
after Shown, and 48 popup/ribbon handler hover samples with zero changed sampled
document/status pixels. Mica startup was also inspected on the actual user profile;
Paste → right-click customization opened successfully. A public-only independent
consumer passed Mica/Acrylic startup after a Load resize with RichTextBox, status
strip and retained editor handles; it references no Showcase code/resources.
Artifacts: `artifacts/backdrop-startup-20261030/` (`final-*`, `independent`). All
review windows closed and computer use stopped. Handler-driven capture is not
physical pointer acceptance; random native hover flicker still needs user review.
No full-suite, package, mixed-monitor/DPI or native-input campaign was run.

## Solid release, saved selection and fully clear navigation — 2026-10-30

The user reported the whole form turning black when disabling material. Release
now switches managed paint to Solid before native teardown, repaints and flushes GDI
client drawing while the old material still covers it, then removes the native
material/extension and finishes the Solid frame/client synchronously. This avoids
leaving the zero-alpha black material backing exposed until a later paint. Teardown
does not repaint during handle destruction/disposal. Window and native editor
ownership remain unchanged.

Showcase now saves the requested `Backdrop` (including a preference currently using
Solid fallback) and `TransparentBackstage` in its application `preferences.json`.
Both are optional additions to version one: existing files default to Solid and
opaque navigation. Changes save automatically; startup restores before display.
Malformed/foreign/future inputs remain protected from automatic overwrite.
The transparent option now selects navigation background opacity `0`, not `.65`.
It adds no grey rail tint; Windows' chosen material retains its own appearance,
and entry state indicators, text/icons and native page content retain coverage.

Verification: Debug build passed, as did 51 focused cases in two nonvisible scopes
(17 preference/storage, 11 alpha, 23 frame/editor/provider cases). An isolated
`--check-backdrop-preferences-hidden` run exercised actual Showcase save events for
Mica/Acrylic/Solid and restoration in a fresh MainForm, without Show/Application.Run
or desktop input. The user then approved one automatic visible check: 16 full-client
samples immediately after each Mica→Solid and Acrylic→Solid switch passed, with
zero-percent sampled near-black coverage. First captured frames were inspected.
Log/images: `artifacts/backdrop-off-20261030/live-off-final/`; hidden preference
evidence: `artifacts/backdrop-off-20261030/preferences-hidden/`. The test app closed
and visible checking stopped immediately afterward. These bounded checks do not
close the separate historical startup/top-edge/popup flicker acceptance below.

## Solid-frame regression and stale-popup accessibility — 2026-10-30

The user confirmed that the startup block persists in Solid mode and that popup
opening had regressed. **The black-block issue remains open for final acceptance.**
The earlier sampled passes and simple/extended RECT comparison did not establish
its cause or resolution.

Comparison with pre-backdrop commit `c8515a2` found that the later custom caption
style removed `WS_CAPTION` even in Solid mode. Solid now retains that earlier
native frame style. Caption suppression applies only while a material is actually
applied and is reversed on Solid/fallback; style changes preserve the Form HWND.
The unsuccessful simple-RECT customization was removed: caption reclamation again
applies to the committed extended client calculation. Maximized top sizing uses
the resize inset rather than the native title height, including the restored
Solid style. Existing popup-family activation/no-repaint handling is retained.

The reported `ObjectDisposedException` was reproduced with a hidden popup and a
queued client-thread property/pattern query. Checking a popup before invoking
through that same short-lived Control left a disposal race. Providers now dispatch
through the retained ribbon and recheck popup availability on the owner thread.
Stale queries, including root identity properties, return UIA element-not-available
(`0x80040201`). Synchronous owner-thread disconnection callbacks remain supported;
unrelated live-provider errors are not swallowed.

Verification: Debug Showcase build and 34 focused hidden/offscreen cases passed.
Six accessibility cases cover queued popup/owner closure, stale identity and live
errors; the popup-close cases also assert no first-chance ObjectDisposedException
on the client thread. Eight hidden frame-policy/calculation cases and the 20
existing alpha/editor cases passed. A brief user-approved actual Showcase check
at the retained Simplified/TitleBar-QAT settings opened Paste, queried its provider
tree, then right-clicked to open the customization menu without the reported
exception. The caption was clean in the inspected startup/settled-popup frames.
This was not a high-speed first-display capture. Exhaustive startup/popup frames,
material/maximize visual revalidation of the changed frame policy and human
acceptance remain pending. The review app closed and computer use stopped after
that bounded check; no commit or push was made.

## Frame calculations, optional transparency and live follow-up — 2026-10-30

The frame-calculation conclusion in this earlier follow-up is superseded by the
Solid-frame investigation above; its transparency implementation/evidence remains.

The supplied recording still showed a top-edge strip at startup and Print Preview
entry after the earlier title-row correction. `WM_NCCALCSIZE` was customized only
for its extended form. Its simple RECT form retained a native top inset: eight
pixels for Sizable, one for FixedSingle and three for Fixed3D in the regression's
96-DPI context. Both forms were temporarily given the same caption reclamation.
The user reported no fix and a popup regression, so that simple-RECT change was
subsequently removed. A difference between query paths was not sufficient evidence
that it caused the rendering defect.

The user also authorized the previously deferred transparency work:

- `RibbonTheme.QuickAccessBackdropOpacity` controls QAT hover/pressed/checked fill
  coverage over an active backdrop (default `.6`, finite range 0–1). Artwork keeps
  its opacity; Solid and popup rows keep their authored fills.
- `RibbonBackstage.NavigationBackgroundOpacity` is opt-in (default `1`, finite
  range 0–1, with a matching Changed event). Lower values expose an active native
  backdrop through the navigation rail; unsupported/Solid hosts remain opaque.
  Showcase offers **View → Window → Backdrop → Transparent Backstage**, enabled
  when a material is applied, selecting `.65` versus `1`. This experimental choice
  is not added to application persistence.
- Navigation uses alpha-preserving drawing and composited text. Page fields stay
  on an opaque native parent; transparent mode fades that parent rather than the
  whole surface. Motion captures only library-owned navigation drawing. Entry
  backgrounds replace their rail pixels instead of applying the tint twice.
- Live Showcase review caught two integration gaps beyond the independent
  consumer: the new command needed an explicit sample icon mapping, and its
  AutoScroll page tree could remain blank after a fade. Restoration now repaints
  the native target subtree before revealing it at full opacity. Page controls,
  bindings and ownership are retained.

Evidence and limits:

- Normal Debug builds passed. The final focused offscreen/hidden run passed 20
  cases: 11 alpha/coverage cases plus nine retained-editor regressions. The earlier
  19 frame cases and 12 independent public-consumer transparency cases remain
  recorded evidence; they were not repeated during the usage-conscious live pass.
- Independent Office and neutral consumers covered Compact/Touch ×
  Solid/Mica/Acrylic, including partial/zero coverage, Solid fallback, page changes
  and reopen with native values/handles retained. Actual pixels confirmed material
  exposure; transition captures exposed and verified the tint-compounding repair.
- Native frame interaction passed at 120 DPI: move, resize, maximize/restore,
  minimize, close and work-area containment. Fresh Showcase launch and Solid
  preview entry/exit were inspected live. The final actual-Showcase probe painted
  both Info and Print pages without mouse input or `Refresh`, then sampled the top
  client edge 16 times at 16-ms intervals during each Solid/Acrylic preview entry.
  Both entries passed without a wide changed strip. Log/images:
  `artifacts/transparency-review-20261030/showcase-final/`.
- These bounded samples do not prove every first-display or interruption frame is
  clean. Mica preview entry, exhaustive startup/hover/rapid-interruption review,
  physical mixed-DPI/IME, Release/package/designer and human acceptance remain
  pending. The live check ended and computer use stopped at the user's request to
  conserve usage; no background desktop automation remains active.

## Startup and Print Preview title-row follow-up — 2026-10-30

The user still sees a brief black title-area bar at startup and during Print
Preview, and requested a small first correction. Showcase had no black BackColor
assignment; material background painting deliberately clears to transparent black
to expose DWM. That clear is not proof of the reported flash's cause.

Solid geometry motion held the whole real ribbon but copied its tab header starting
below the custom caption, omitting that caption row. It now carries the caption
pixels too, with QAT glyphs excluded for their existing independent movement.
Showcase also initializes BackColor from the ribbon theme before associating the
window host, providing matching startup backing. Material header painting and the
deferred QAT/Backstage transparency work were not changed.

Normal Debug Showcase build passed with zero warnings/errors. To keep this small,
no additional tests or visible app/input checks ran. Startup and Print Preview
entry/exit still need visual confirmation in Solid/Mica/Acrylic; this correction
does not establish that the reported black frames are gone, especially in material
mode. Computer use remains off.

## Returning to editor tabs — 2026-10-30

The user reported TextBox and enabled ComboBox flicker when returning to their
tab. This reopens that specific visual check; the earlier disabled-choice
correction did not cover unchanged enabled presenters.

- Presentation comparison now covers enabled fields too, including their enabled,
  read-only and layout state. Unchanged synchronization skips the extra frame
  invalidation. Parking or recreating a presenter clears the comparison; label,
  value/choice, palette, renderer and geometry changes still refresh it. Native
  focus, hover, press and actual list changes retain their own repaint paths.
- A returning presenter requests a synchronous client/subtree repaint after
  visibility/parenting completes. Material motion also finishes the committed
  ribbon's native child paints while its covering drawing is still present.
  Solid already does this when releasing its drawing hold. These requests do not
  include non-client frame painting or introduce another form style change.
- Geometry-motion release no longer invalidates the entire subtree after removing
  that cover, or when settling an already idle controller. Closing an already
  closed choice list also avoids its previous unconditional invalidations.

Normal Debug Showcase build passed with zero warnings/errors. Nine focused hidden
cases passed: six `HiddenEditorTabReturnTests`, the existing disabled-choice case,
and two editor composition/root-transfer cases. Before correction, five unchanged
synchronizations produced five presenter invalidations per enabled field; five
idle settles also produced five ribbon invalidations. Both are now zero. Repeated
away/return cycles preserve native handles, text/choice values, editable selections
and commit counts. Classic includes TextBox and both ComboBox modes; Simplified
covers its explicitly inline ComboBoxes (TextBox commands stay in full groups).

Computer use remains off. Hidden native windows do not consume visible child
paints, so these checks establish state and repaint-request behavior, **not** the
absence of composed-screen flicker or the animation handoff's visible timing.
Return-to-tab review under Solid/Mica/Acrylic, with Fade/SlideDown and interruptions
by popups/Backstage, remains pending. No public API or persistence schema changed;
no visible launch/input, full suite, WPF, Release/package, commit or push ran.

## Disabled choices, popup caption state and preview containment — 2026-10-30

The user reported remaining disabled ComboBox flicker on popup opening, grey
owner-title text while menus/temporary minimized-tab ribbons own keyboard focus,
and a title-bar bar during Print Preview entry. Computer-use input remains off;
the work below used source review, hidden HWNDs and offscreen drawing only.

| Report | Bounded correction | Current evidence/limit |
| --- | --- | --- |
| Disabled ComboBox repaints when an unrelated popup opens | Popup opening synchronizes editors. Previously every `Presenter.Present` ended in `Invalidate`, including an unchanged disabled choice. The initial correction compared disabled presenters' actual surface, geometry/clip, palette/renderer, font/density/DPI, label/value/choices, editable mode and material state. Parking/handle changes clear the comparison. The tab-return follow-up above extends this to enabled fields and removes no-op list-closing redraws. | The hidden regression observed five invalidations for five unchanged synchronizations before the fix and zero after it. Label, choice/value, palette, renderer and enabled-state changes still repaint and retain the native handle. The intermittent composed-screen flicker has not yet been rechecked. |
| Title text turns grey for owned popups/temporary minimized ribbons | `ContainsFocus` describes keyboard focus, which legitimately moves into ToolStrip/choice windows. Caption text now follows `WM_NCACTIVATE` state, including the existing popup-family activation guard. It uses the same existing foreground/secondary theme roles. Real deactivation still dims the caption. | Two hidden/offscreen cases passed for normal and composited caption drawing: active text stays stronger without keyboard focus and becomes secondary on native deactivation. Actual menu/minimized-tab opening, focus return and external-application activation remain live gates. |
| Print Preview can paint into the title area | The application preview was a direct full-form sibling, shown before `BringToFront` corrected its docking order. It now shares the document's opaque body host from initialization, with the status strip reserved below it. Preview cannot escape that host during entry/layout and its native GDI drawing uses the same opaque surface as the document. This remains Showcase-owned application content. | The real Showcase launch/exit command passed 12 hidden layout cases: Solid/Mica/Acrylic × Compact/Touch × initially expanded/minimized. Managed form-space rectangles do not overlap the ribbon or status strip, and exit restores the previous minimized state. Actual intermediate composed frames remain unverified. |

Verification:

- Normal Debug Showcase build passed with zero warnings/errors; no runtime public
  API or persisted schema was added for these corrections.
- `HiddenCaptionColorTests` (2 cases) and `HiddenDisabledComboTests` (1 case)
  passed. Both paths first exposed the prior behavior. The disabled-choice case
  includes legitimate presentation changes, not just unchanged-call suppression.
- `--check-modal-layout-hidden <directory>` runs the actual Showcase consumer
  without `Show`, `Activate`, `Application.Run` or input injection. All 12 cases
  passed; ignored log: `artifacts/m6c-hidden-modal-20261030-final/result.log`.
  Invisible native controls may be parked at unrelated screen coordinates, so
  this check deliberately uses their managed layout in a common form coordinate
  space. It proves hierarchy/layout contracts, not visible pixel stability.
- No visible app launch, mouse/keyboard automation, full suite, WPF, Release,
  package, commit or push work ran during this follow-up.

On the next live review, repeat disabled-choice popup openings (including after
tab/Backstage interruptions), one-click temporary opening of minimized tabs and
their nested popups, then preview entry/exit from both minimized states. Inspect
title ink and short bar frames under Solid/Mica/Acrylic. Keep the broader startup,
hover and combined-action checklist below open; these headless results do not
establish that all reported flashes have disappeared.

## Startup and combined-action paint review — 2026-10-30

The user's subsequent review reopened painting acceptance. **This investigation
is not complete.** Computer-use input stopped at the user's request after the
startup reproduction/recheck; no later mouse/keyboard automation or visible test
windows were run. The earlier focused passes below do not close these new reports.

| Requested check | Current result |
| --- | --- |
| Editor absent until hover at startup | Reproduced on a fresh, untouched Solid launch. The accessibility tree contained the document while its pixels were absent. After the content-host lifecycle change, a second untouched launch showed the document and status bar immediately. Material startup has native lifecycle coverage; actual fresh-launch Mica/Acrylic pixel review remains pending. |
| Ribbon TextBox/ComboBox flicker after rapid tab changes followed by Backstage or other actions | Corrected material composition lifetime when retained presenters move into opaque popup roots and return. Composition is established before showing/reparenting completes, and unchanged opacity is no longer rewritten every ribbon paint. Hidden-window checks cover root transfer, repeated synchronization and mask interruptions with retained HWNDs/values. Rapid visible sequences remain pending. |
| Editor flicker on hover | The supplied recording has a one-frame loss of the lower document text at frame 248, about 8.23 seconds, followed by restoration. The old capture sweeps did not cross the document/status boundary shown there. Solid no longer uses a layered/composited document host; repeated boundary sweeps in Solid/Mica/Acrylic remain pending. |
| Black/white bar over the title bar when opening popups | Added a scoped `WM_NCACTIVATE` no-repaint guard for the custom caption, preserving default activation handling and minimized-window handling. This is an implemented candidate correction, not a visually verified fix. Popup opening/closing, keyboard focus, system-menu and external activation checks remain pending. |
| Showcase label backgrounds | The user clarified application/form/status labels, not ribbon captions. The Showcase content host/status strip now use `SystemColors.Control`, and the status label uses its ambient/default background. Existing native Label controls already use ambient backgrounds; authored Backstage page backgrounds and ribbon palette/caption roles remain unchanged. Final visual review is pending. |
| Status bar starts black or missing | Covered by the same content-host initialization defect. The corrected untouched Solid launch displayed the status bar. Repeated startup captures in all material choices remain pending. |

### Cause and bounded implementation

`RibbonBackdropPanel` previously requested `COMPOSITED | LAYERED` even in Solid
mode and before it belonged to a native Form. `HandleCreated` can occur in the
WinForms parking window, where initializing layered opacity is deferred. A layered
child can then stay undrawn until a later paint/hover. The panel now uses ordinary
Panel painting for Solid/unattached hosts, retries material initialization after
native show/position parenting messages, tracks the owning root across attachment,
and removes only its composition flags on Solid fallback. Child controls are not
recreated. Two inherited implementation overrides are recorded in Unshipped; no
new application preference/API setting was introduced.

Editor presenters now release library-owned material composition when their native
root becomes an opaque popup instead of retaining the main-window composition
there. Returning presenters initialize against the actual root before presentation.
Mask changes still restore opacity 1/255 without replacing native fields. Existing
application-owned opacity remains outside the library's drawing hold.

For custom caption activation, Microsoft's [WM_NCACTIVATE documentation](https://learn.microsoft.com/en-us/windows/win32/winmsg/wm-ncactivate)
defines `lParam = -1` as suppressing default nonclient repaint. The guard keeps
normal native activation processing, then invalidates only the owned caption.
The visible popup-bar result still requires confirmation; do not infer it from
the documented mechanism or a clean build.

### Evidence and remaining gate

- Normal Debug Showcase build passed, zero warnings/errors.
- Three new `BackdropPanelLifecycleTests` cover precreated/parked handles, Solid
  style policy, Mica/Acrylic initialization before hover and handle/value retention
  through material changes. All passed after the correction. The old code failed
  the precreated-handle cases. These visible lifecycle tests ran before computer
  use was stopped.
- Two `HiddenEditorCompositionTests` passed without showing/activating windows or
  injecting input. The fixture explicitly completes native parenting of hidden
  controls because WinForms deliberately parks them; this is lifecycle/opacity
  evidence, not composed-screen evidence.
- Computer-use window snapshots established the missing Solid startup document/
  status bar before the fix and their presence before hover after it. No forced
  Refresh/Update or mouse movement was used for those observations.
- User recording: `20261029-2340-45.6762179.mp4`, 30 fps, 10.37 seconds. Ignored
  extracted frames: `artifacts/review-video-full-20261030/`, especially 247–249.
  The recording confirms document paint loss; the reported popup/title-bar bar
  has not been isolated in those frames.

Resume with fresh-process no-input startup captures, then repeated document/status
boundary crossings and interrupted tab → Backstage → popup sequences in Solid,
Mica and Acrylic. Include both native field types, actual Showcase pages/status,
both densities, and an independent consumer with a real RichTextBox. Showcase's
current document is a multiline TextBox, despite the user's generic editor name.
Capture short intermediate frames rather than checking only settled pixels. Check
caption/bar pixels during actual popup activation and focus return. Keep this
six-item checklist open until those live scopes pass and the user reviews them.
No full suite, WPF, Release, package, commit or push work ran for this follow-up.

## Resumed review corrections — 2026-10-30

Implemented, with focused/native evidence below; human acceptance remains pending:

| Requested/reproduced issue | Change and observed result |
| --- | --- |
| Backstage opening briefly loses the document/native editors before the replacement paints | Showing the replacement sibling clips native HWNDs immediately. `ShowBackstageBackdrop` now paints that opaque background synchronously before preparing the fading surface or capturing navigation. The previous rail-displacement check missed this separate blank interval; the opening diagnostic now checks opaque page coverage too. |
| Minimize/maximize/restore/close glyphs double when material is enabled | `RibbonForm.CreateParams` suppresses the native `WS_CAPTION` while retaining a one-pixel border, resize, system-menu and min/max styles. The same style applies to Solid/Mica/Acrylic, with no handle recreation when switching materials. Captures show one set of custom glyphs in both densities and normal/maximized states. The inherited override is recorded in Unshipped; no new application option was added. |
| Removing the native caption makes maximize cover the taskbar | `WM_GETMINMAXINFO` fits the window's native border around the current monitor work area. Explicit host `MaximizedBounds`/`MaximumSize` remain authoritative. The independent native check reports client `1920×1140`, matching the actual work area. |
| In-ribbon gallery labels become white under material | The gallery child now paints into a reusable opaque DIB. After GDI/application artwork and labels paint, it flushes GDI and restores alpha coverage for that deliberately opaque surface. This preserves existing RGB/disabled/selection colors and legacy `IRibbonIcon` text without forcing new icon drawing rules. It does not layer/recreate the gallery HWND or make the main ribbon body translucent. The buffer is released on Solid fallback/disposal. |

The caption style preserves the border needed by the custom frame; disabling all
DWM nonclient rendering is unsuitable because it disables extended-frame effects.
See Microsoft's [DWM composition guidance](https://learn.microsoft.com/en-us/windows/win32/dwm/composition-ovw)
and [custom-window rounding guidance](https://learn.microsoft.com/en-us/windows/apps/desktop/modernize/ui/apply-rounded-corners).

Verification, normal Debug output, actual display DPI **120**:

- `BackstageMotionTests`: 12 passed, including a new ordering regression proving
  the background paints before the translucent surface becomes visible.
- `RibbonFormTests`: final 15 passed, including caption style/handle stability,
  native-frame fallback and maximized work-area containment.
- `WindowBackdropTests` + `InRibbonGalleryTests`: 29 passed, including preservation
  of opaque GDI artwork and gallery-label ink. These are focused runs, not one
  full-suite result.
- Restored-M6C Solid/Acrylic sweeps and physical interaction captures retained
  fields/data and sampled width-group playback in both densities. Pixel inspection
  then exposed a gap the older success log had not checked. Offline review of the
  same page region found 4/2 gap frames in Solid Compact/Touch and 2/2 in Acrylic.
  After the handoff change the new opening check reports **0/0/0/0** gaps, with
  intermediate frames inspected. This compares current Solid/material modes;
  it is not a fresh build of the pre-M6C `c8515a2` reference.
- Public-only independent Backstage motion checks passed after making the test
  await actual native mouse release and sample the page before full-window screen
  copies stall its UI timer. Its zero-opacity initial frame is legitimately blank;
  a separately sampled intermediate must still contain faded native text. Validation,
  KeyTips, page switching, command close, resize, input and close cancellation passed.
- Public-only native frame checks passed real caption drag, edge resize,
  maximize/restore/minimize/close, system menu and detach/ordinary-Form/reattach.
  The separate keyboard Snap check passed. Snap hover appearance remains a visual
  review item; keyboard Snap is not proof of that flyout.
- Public-only gallery material checks passed all three label-ink comparisons
  against Solid under Mica/Acrylic, both densities: exact dark-pixel counts and
  retained native handle/selection, without Showcase resources/helpers.
- Actual Showcase caption/gallery captures cover both densities, three material
  choices and normal/maximized states. Full-form images show rounded normal-window
  corners, opaque command/document areas and dark inline labels/artwork. Acrylic
  Styles/Text color dropdown captures also retain dark labels.
- M5E Apply/save and a second real process restore passed again after the final
  frame changes; only that affected restart scope was repeated.

Ignored raw evidence: `artifacts/m6c-resumed-20261030-061116/`, especially
`opening-painted-first-ready/`, `caption-and-gallery-opaque/`,
`independent-backstage-motion-sampled/`, `independent-window-frame-fitted/`,
`independent-window-snap/`, `independent-gallery/`, `showcase-gallery-popups/` and
`persistence-after-frame-fix/`. The first opening retry aborted on lost foreground;
the user then freed the computer and the guarded retry passed. That abort is not
acceptance evidence. Earlier failed/diagnostic runs remain in the same ignored root.

These changes resolve the captured Backstage handoff gap and reported caption/
gallery defects at this scope. The broader intermittent document blink, physical
monitor migration, other DPI values and human visual acceptance remain open. The
main ribbon body stays opaque as requested. No WPF, full-suite, Release or package
campaign ran; no commit/push/branch operation was performed.

Earlier deferral context:
The user accepted M6B's final frame/group/Backstage/editor motion fixes on
2026-10-29 and authorized M6C. During review the user expanded the backdrop to
the form background and explicitly retained the opaque main ribbon body.
At deferral the user preferred the stable pre-M6C ribbon, handled branching and
planned to revisit the experiment after the remaining work. M6C had not received
visual acceptance; the historical checks below did not establish that acceptance.

## Deferral and resume checkpoint

- **Stable reference:** `c8515a2` (`Flickering Fix`), the committed pre-M6C HEAD
  observed at deferral on `codex/Ribbonkit-Winform`. M6B's accepted frame, width-group,
  Backstage and editor-motion fixes are the baseline to preserve.
- M6C work was still tracked/untracked local changes at this checkpoint. The user
  owns the experimental branch creation/name and the return to the stable checkout.
  No branch switch, commit, reset, stash or source rollback was performed for this
  documentation update. Recheck live Git state when resuming; do not assume the
  branch or dirty state remains the same.
- At deferral the backdrop API, opaque GDI host, native editor/Backstage composition,
  motion masking/capture and caption-repaint follow-up stayed together as the
  experiment. The caption fix's isolated test pass did not establish acceptance
  of the whole rendering path. The later user-owned restoration is recorded above.
- **Outstanding symptom:** the Showcase document intermittently flickers during
  ribbon/Backstage animation, more likely with pointer movement. Controlled captures
  missed the visible blink, including before the caption repair. Its visual cause
  and resolution remain unconfirmed. Native form/background redirection and the
  new layered/composited content host remain investigation areas, not proven causes.
- **Required appearance on a later revisit:** native material in exposed form and
  ribbon background; the main ribbon body remains opaque. Native fields, Backstage
  pages and document content need opaque readable surfaces. Full translucent
  command panels were explicitly declined.

Investigation method retained from deferral: begin with a bounded reproduction against
the stable reference and the experimental branch. Include pointer sweeps across
caption/ribbon/document during tab, minimize/density and Backstage open/close
transitions. Inspect intermediate composed frames and paint/erase activity, then
isolate one rendering change at a time. Preserve real native editor identity,
validation, focus and values; a settled screenshot or logical HWND rectangle alone
does not prove freedom from flicker. Do not continue feature expansion first.

Useful implementation entry points:

| Area | Sources |
| --- | --- |
| Native material and GDI content | `RibbonForm.Backdrop.cs`, `RibbonBackdropPanel.cs`, `Rendering/RibbonBackdropBuffer.cs` |
| Ribbon paint, native editors and Backstage | `RibbonControl.Window.cs`, `RibbonControl.cs`, `Editors/RibbonEditorManager.cs`, `Backstage/RibbonBackstageSurface.cs` |
| Drawing/opacity coordination | `Animation/RibbonDrawingHold.cs`, `Animation/RibbonGeometryMotion.cs`, `Animation/RibbonMotionScene.cs` |
| Actual consumer and diagnostics | `samples/RibbonKit.WinForms.Showcase/MainForm.Backdrops.cs`, `samples/RibbonKit.WinForms.Showcase/MainForm.BackstageMotionCapture.cs`, `tests/RibbonKit.WinForms.Consumer/WindowBackdropChecks.cs`, `tests/RibbonKit.WinForms.Tests/WindowBackdropTests.cs` |

Runtime paths in the table are relative to `winforms/src/RibbonKit.WinForms/`;
sample/test paths are relative to `winforms/`. The API remains an unaccepted
Unshipped proposal on this experimental implementation.

`winforms/artifacts/` captures and test `TestResults/` are ignored local evidence;
the branch does not carry those files. Preserve any needed raw captures separately
when moving the experiment; the scope/result summaries below are the durable
record. Await completed builds before launching a diagnostic, run one native UI
check at a time, and close the Showcase before rebuilding its normal Debug output
to avoid DLL locks. Foreground-guard aborts and interrupted runs are not passes.

## Public contract and ownership

- `RibbonForm.Backdrop`: `RibbonWindowBackdrop.None` (default), `Mica`, `Acrylic`.
  Native material fills exposed form/ribbon background. The main command body,
  messages, popups, native editors and Backstage pages retain opaque surfaces.
  The authored theme's Background and Surface are unchanged.
- `EffectiveBackdrop` reports the setting successfully applied to the current
  handle; unsupported/unsafe contexts retain the preference with solid fallback.
  Windows may itself render an applied material as solid according to OS policy.
- `BackdropChanged` reports requested-preference changes. The application can
  adjust its own content margins. No host handler completes library animation.
- `RibbonBackdropPanel : Panel` is an explicit opaque content surface for native
  GDI application controls over material. It composes its native child drawing at
  alpha 255, retaining native input, handles and ownership. The application owns
  the panel, its controls, data and layout. Use an opaque BackColor and the Windows
  8-or-later layered-child compatibility manifest already required by motion.
- RibbonForm owns DWM setup, handle/system/DPI refresh and cleanup after opt-in.
  It does not change application Opacity/TransparencyKey, reparent application
  content or assume ownership of icons, commands, persistence or DPI startup.
  Plain Forms remain supported. No WPF hosting, screen-capture blur, undocumented
  composition attributes or broad Glass/theme campaign was added.

The documented [DWM backdrop choices](https://learn.microsoft.com/en-us/windows/win32/api/dwmapi/ne-dwmapi-dwm_systembackdrop_type)
require Windows 11 build 22621. See the
[custom-frame alpha guidance](https://learn.microsoft.com/en-us/windows/win32/dwm/customframe).
Unsupported OS/design contexts, high contrast, non-top-level/borderless/layered
Forms, application opacity/key and drawing failures use solid fallback.
Drawing is bounded to 4,194,304 pixels; a failed paint retries after a new
preference or handle. Full native OS-policy/contrast-theme compatibility is not claimed.

## Native composition and motion decisions

The header-only candidate was expanded at the user's request. Its acceptance
evidence is retained as diagnostics; it does not establish the full-form result.
The translucent-command-panel experiment was removed when the user clarified
that only the surrounding ribbon background should show material.

The library paints material-aware ribbon pixels through one reusable top-down
32-bit DIB on the existing HWND. Composited theme text preserves black glyph alpha;
the graphics context must address the real top-down DC. An intermediate bitmap
HDC inverted some diagnostic labels. Window paint uses transparent Background
without changing the opaque Surface role or the authored immutable theme.

GDI content needs an opaque composited surface. Native editor presenters use an
owned alpha-255 hold; Backstage upgrades its existing stable opacity surface.
Application content uses the public panel. Native styles are borrowed only after
the HWND belongs to the current native root; parked handles during Form recreation
are deferred. Reapplying native material refreshes panel drawing so GDI text does
not remain as a black rectangle. No native editor handles are recreated to animate
or change material.

Layering the whole ribbon blackened the DWM material. Material mode keeps the
caption/tab strip live, committing its geometry/indicator immediately. During
geometry playback the library masks only real body drawing and lowers its already
composited editor surfaces to alpha 1. An inert drawing covers the committed body;
real drawing/alpha 255 returns before the overlay is removed. Existing body/group,
popup and Backstage motion policies remain. Solid retains its full header motion.
Material snapshots copy the original DIB alpha directly; printing through a bitmap
HDC otherwise converted transparent pixels into opaque black. Captured taller
headers/QAT strips are excluded from material body motion during density changes.

Showcase consumes these public APIs. It places its existing document/status
controls in the shared panel and owns transparent surrounding margins. View /
Window / Backdrop selects Solid, Mica or Acrylic. Appearance is not persisted by
the library or included in ribbon customization JSON.

## Document pointer-repaint follow-up - 2026-10-30

The user reported intermittent document flicker during ribbon/Backstage animation,
more likely while moving the pointer. The main Showcase document is a native
multiline TextBox in this checkout. Controlled samples did not reproduce a visible
blink, so sampled success is not a claim that every intermittent case is closed.

A concrete defect was reproduced: repeated nonclient caption motion invalidated
the entire client, including the document, even when hover state was unchanged.
Caption title/activation/button/hover updates now invalidate only the measured
caption bands, and unchanged hover does not invalidate again. The regression
failed **2/2** before and passed **2/2** after in
`m6c-caption-hover-before.trx` / `m6c-caption-hover-fixed.trx`.

`artifacts/m6c-document-hover-fixed/` captures controlled pointer sweeps across
caption, tabs and document, tab motion with pointer movement, and Backstage close.
All five scopes have zero sampled visible document changes. Before-fix samples
also happened to be stable; these captures support current behavior, while the
invalidation regression proves the bounded repair. Human visual review remains open.
The last two full-form density capture attempts stopped when foreground ownership
changed (`m6c-full-opaque-density-final` and `m6c-full-opaque-density-retry`). They
are not passes. The focused density raster and width-playback evidence remains
separate; no further input was sent after the capture guard stopped the run.

## Focused evidence

- Normal native Debug solution builds passed with zero warnings/errors, including
  API/XML and the separate design assembly. Additions are in Unshipped; shipped
  baselines were not edited.
- Full-form focused tests in `m6c-full-form-tests.trx` initially passed **18/20**.
  The density raster assertion was corrected to distinguish translucent shadow
  from opaque black and passed in `m6c-full-form-tail-fixed.trx`; the Backstage
  fixture was configured to actually open File/Backstage and passed **1/1** in
  `m6c-open-backstage-fixed.trx`. These are separate repaired scopes, not one clean
  aggregate. They include the requested opaque-body/transparent-surround contract,
  native text alpha, policy/metadata, composition restart, width playback and
  retained editor identity.
- `m6c-full-form-related.trx`: **33/33** affected Backstage, TextBox, ComboBox and
  RibbonForm checks. Earlier `m6c-related-motion.trx` retains **65/65** geometry,
  minimize and previous window-motion scopes; selections overlap.
- At actual **120 DPI on Windows build 26300**, public-only Office and neutral
  checks passed all three choices in Compact/Touch in
  `artifacts/m6c-full-office-native-settled/` and
  `artifacts/m6c-full-neutral-native-settled/`. They assert that Backstage actually
  opens, native handles/values and authored state survive, application GDI text
  remains dark over a white surface, native opacity/key/border fallbacks recover,
  and handle recreation/maximize/restore work. Native window captures wait for
  compositor stability, not just committed HWND bounds; early OS zoom/restore
  frames produced false pixel failures and remain diagnostics.
- `artifacts/m6c-full-simplified-parked-field/` adds the public-only Simplified
  path. It establishes handles in Classic before switching: the TextBox remains
  parked/retained while the supported ComboBox is inline. This does not claim
  inline TextBox support.
- Actual Showcase full-form captures in `artifacts/m6c-opaque-body-showcase/`
  passed all material/density combinations with real Inputs/Info/Print pages,
  tab/minimize intermediates and retained native editor handles/data. Images
  confirm an opaque white main body with material around it and the content card.
- `artifacts/m6c-full-form-interactions/` passed actual Back hover/click, native
  name commit/font choice and physical width drags under Acrylic in both densities;
  group playback was sampled in both directions. Typing/drag counts establish
  input and identity, not automatic pixel-stability acceptance.
- `artifacts/m6c-caption-native-input/` passed real caption drag, edge resize,
  maximize hover/click, restore, system menu, minimize, host fallback/reassociation
  and close after the caption invalidation repair.

Native runner/consumer `--backdrop-checks`, `--backdrop-checks-flat` and
`--backdrop-simplified-checks` take an evidence directory. Showcase diagnostic
switches cover real page, menu, density, pointer and interaction scopes separately
so passed prefixes need not repeat.

## Remaining limits

Human visual acceptance, physical mixed-monitor/DPI migration, other display
scales, actual older Windows/high-contrast/transparency-policy behavior, broader
theme/RTL/designer/accessibility compatibility and performance/resource budgets
remain separate. Actual new native-material appearance is not inferred from pure
policy or geometry tests. No WPF runtime/build, full suite, Release or package
campaign was run; the independent source is linked into the native runner rather
than freshly built from a package.
