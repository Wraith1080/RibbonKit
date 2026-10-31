# M1 native foundation evidence

Status: **implementation available for review; M1 acceptance remains open**.
Date: 2026-10-26. Branch: `codex/Ribbonkit-Winform`. Base commit:
`cf1b942a1ec707538084a51202b03ad68d9be33e`; the user committed the original native
foundation as `300c741` (Ribbon Winform First Commit). The popup-width, disabled
input/window-activation and keyboard-focus-cue follow-ups are uncommitted. The agent has not pushed,
published or merged these changes.
The WPF runtime/build/CI are unchanged.

The implementation uses independent .NET 10 projects in this subtree, an ordinary
Form, component collections, a native layout/paint/input pipeline, ToolStrip popup
windows and a public Windows COM UIA bridge. The provisional public API remains
Unshipped. Initial scope is Office2024 light, Compact, LTR and immediate transitions.

## Environment

- SDK 10.0.401; .NET desktop runtime 10.0.12; x64; Windows build 10.0.26300.
- Intel Core Ultra 7 255H, 16 logical processors; Balanced power plan.
- One reported display, 1920 x 1200; actual control DPI 120 (125%). No OS scale was changed.
- Visual Studio Community 2026 18.10.2 is installed. Design projects use
  Microsoft.WinForms.Designer.SDK 1.6.0. Current native authoring evidence and
  pending review are tracked in [the designer checkpoint](DESIGNER-AUTHORING.md).

## Acceptance ledger

Rows correspond to [the plan's F1-F12 gates](../../docs/21-NATIVE-WINFORMS-PLAN.md#7-implementation-ready-m1-work-package).
Passed describes the named automated scope; Partial does not close the full gate.

| Gate | Result and evidence | Remaining scope |
| --- | --- | --- |
| F1 build/API/package | Passed: normal Debug/Release builds; XML documentation and public API analyzer checks; local pack. Isolated source/cache restore outside checkout, consumer build and external input/UIA execution. Package contains runtime/XML, MIT notices and design server DLL; no runtime WPF/SDK dependency. | Public API is provisional; packaging does not prove designer loading. |
| F2 pure selection | Passed: adapted WPF selector cases plus native caller priority/fixed-group/tie behavior. Original file hashes and adaptations are in the reference record. | No WPF test result is counted as native evidence. |
| F3 layout | Passed for bounded automated scope: width decrease/increase, reduction, fixed policies, residual/tab scrolling and focus reveal, stable repeated snapshots, font/text invalidation, ownership and visibility. Three real-HWND resize sweeps completed. | Human appearance and genuine DPI movement are F9/F10. |
| F4 commands/binding | Passed: PerformClick/order/disabled rules; handler detach/disposal; real mouse, Enter, Space, KeyTips, external Invoke/Toggle; two-way BindingSource in independent consumer. Checked state survives tab switch, resize, collapsed popup and reopen. | No editor/IME/native touch scope is implied. |
| F5 popups/lifetime | Partial: cancellation/reopen, model removal, ribbon hiding/Form disposal, repeat dropdown/group cycles, nested Escape and focus return, outside press preserving content focus. | Human opener re-click, movement/resize and edge-placement review remain. See the dated activation follow-up for automated window-switch coverage. |
| F6 keys/KeyTips | Passed for native automated scope: Alt release, F10 toggle, tab/command tips, Escape, arrows, Home/End, Tab/Shift+Tab, F6, disabled skipping, scrolling focus reveal; prefix/conflict/non-Latin and 80-item resolution cases. Both test host and package consumer pass. | Badge appearance is part of F9. Complex nested containers and multiple ribbons per Form are outside this slice. |
| F7 accessibility | Partial: separate-process discovery, names/IDs/action bounds, selected-tab content, Invoke/Toggle/selection/ExpandCollapse, disabled rejection, popup discovery/focus, external toggle property-change event. | Broader state/structure/focus event inspection remains. Narrator-specific work/review is outside scope by user direction, 2026-10-26. This is not accessibility certification. |
| F8 designer | Partial: Content metadata, serialization, typed editors and custom Edit Ribbon client/protocol/server are implemented. Current evidence and acceptance: [designer checkpoint](DESIGNER-AUTHORING.md). | Scope-specific review and remaining limits live in the checkpoint; broader designer authoring is outside this increment. |
| F9 appearance/DPI | Partial: native Form captures at actual 125% inspected at 1080, 420 and 260 widths; medium-row widths/icon slots/text starts and large-label first line covered at synthetic 96/120/144/192 DPI and varying fonts. Four equal-radius fill/stroke corner pixel checks pass at those scales. | Human review at actual 100/125/150/200%, hover/press/checked/focus, popup edges and WPF reference comparison remains. Synthetic DPI does not close this gate. |
| F10 monitor boundary | Unavailable: one display reported. | Genuine mixed-scale monitor movement has not run. No hardware limitation has been accepted as a substitute. |
| F11 independent consumer | Passed bounded runtime scope: package-only app with its own Form, image, BindingSource and handlers; same external UIA, keyboard, mouse and nested-popup actions. No Showcase reference/helpers or internal-access grant. | Package designer authoring follows F8; human/native display review follows F9/F10. |
| F12 baseline/stability | Partial: ten launches, three 30-second sweeps, 100 dropdown and 100 collapsed-group cycles plus ten nested cycles; raw timings/resource checkpoints retained. No unhandled exception or stranded popup in completed workloads. | Human drag responsiveness, extended idle resource plateau and any performance budget remain unaccepted. Initial timings precede the final icon-clipping refinement. No animation/frame-rate claim. |

Automated evidence: the combined 57-case Release suite and package checks passed.
Final review then corrected a cached UIA group's parent after its tab became
inactive; the new focused HWND/provider regression also passed (58 distinct
cases verified, not a claimed single 58-case run). Focused alignment checks passed
after measurement and drawing were changed to matching GDI text-padding flags.
The original aggregate package run preceded that one-line inactive-provider correction;
the new case is covered in-process and broader external inactive-provider review
remains part of F7.

Popup-width follow-up, 2026-10-26: five focused Debug checks passed for single-column
row paint/hit bounds at two font sizes, multi-column separation, and normal versus
two-axis overflow sizing. The existing popup-lifetime regression and separate-process
native UIA/mouse/keyboard scenario also passed. Native popup capture at actual
120 DPI (125%) confirmed a 136 x 106 surface, five-pixel right inset and no unused
scrollbar gutter; checked and hover images were inspected. The normal Debug solution
build and a fresh isolated Debug package consumer also passed with the updated
popup implementation. These checks are
separate from the earlier aggregate runs and do not close the broader F8-F10 gates.

Disabled interaction/window-activation follow-up, 2026-10-26: three new disabled
input/render regressions failed before the fix and passed afterward. The focused
Debug run passed nine cases including the existing popup layout/lifetime checks.
Disabled rows in both strip and popup receive no hover/pressed/focus paint flags,
do not capture the mouse or execute, and the default renderer ignores contradictory
interaction flags. Native 120-DPI disabled-hover/pressed captures were inspected.
The native popup-dismissal reproduction failed while another process was genuinely
foreground, then passed after the fix: normal dropdown and complete nested stack
dismiss without restoring focus; opening nested popups does not dismiss their
parent; another Form in the same process also dismisses without losing its focus.
The existing external input/UIA and keyboard scenarios passed. A fresh isolated
Debug package consumer passed the cross-process/nested dismissal cases. Same-process
second-Form coverage belongs to the native test host. No full suite, Release run,
or additional hardware-DPI review was performed for this bounded correction.

Keyboard-focus-cue follow-up, 2026-10-26: the new regressions reproduced unwanted
mouse focus outlines. Seven new cases pass in a 16-case focused Debug run including
the related disabled-input and popup layout/lifetime checks. The normal Debug
solution builds with zero warnings/errors. Separate-process screen checks pass
for the native host and a fresh isolated package consumer: click retains UIA focus
without an outline, keyboard navigation restores it, pointer motion preserves it,
and Tab/F6 entry, assistive SetFocus, mouse/keyboard popup opening and nested Escape
returns have the expected cues. The existing Alt/F10/navigation scenarios also
pass in both apps. Native screen crops were inspected. An initial probe stalled on
a rapid second click at the same document point; using a different document point
completed the probe without a runtime focus workaround. No full suite, Release
run or additional hardware scale was exercised; M1 acceptance remains open.

## Alignment and rendering decisions

The engine owns row pitch, group caption height, medium column width and leading
icon slots. Neighbouring medium labels have the same text origin, including rows
without an image. Large labels share a first-line origin. Fonts expand row/body
metrics instead of inheriting a fixed clipped height. Main strip and popups use
the same content rectangles. Image drawing, including disabled images, is scaled
and clipped to its icon slot so it cannot bleed into neighbouring rows.

Collapsed flyouts now measure visible command extents, excluding strip-only
caption/trailing space. Single-column row painting, focus, hit targets and UIA
bounds fill the usable popup width. Scrollbar space uses actual DPI metrics and
is reserved only for overflow, including interaction between the two scrollbars.
Multi-column group geometry is retained.

Disabled targets are excluded from mouse interaction, and both default and custom
renderers receive inert interaction state. A disabled toggle can retain its actual
checked indication; it gains no hover, pressed or focus feedback.

Focus cues are shared library input state, separate from logical/native focus and
UIA HasKeyboardFocus. Pointer presses hide the cue; supported keyboard navigation,
Alt/F10 KeyTips and explicit assistive SetFocus show it. Pointer motion alone does
not hide a keyboard cue. Popup opening and focus restoration inherit that state,
so a mouse-opened popup has no automatic first-row outline and keyboard dismissal
returns a visible cue to its opener. Default and custom renderers receive the same
cue policy through the existing paint context; no public API was added.

Popup windows now observe native activation changes as well as the owner's Form
events and surface focus changes. A deferred check uses the real
[foreground window](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getforegroundwindow),
because [thread-local keyboard focus](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getfocus)
can still identify an inactive popup. The owner or any live popup in the same
stack is allowed; switching to a different window closes the stack without focus
restoration. The observer belongs to each disposable popup window; no timer or
application-owned dismissal handler is needed.

The rounded painter samples one canonical quadrant and mirrors coverage. Pixel
symmetry checks cover the candidate native implementation, not a correction to
.NET/GDI rounded-rectangle helpers. Native edge-join/appearance acceptance remains
open. Mica and animations have not been enabled.

## Initial measurements

These are instrumented native paint-completion proxies, not compositor/display
latency, a WPF comparison or a cold-cache study. The workload has 32 commands,
five groups and two tabs. Resize uses a 33 ms WinForms timer; normal dropdown
measurement shows one expanded group and collapsed measurement shows all five.

- Startup: first observed process-age-to-ready 855 ms; subsequent nine launches
  approximately 342 ms median, 283-497 ms range. Managed-entry warm median 272 ms.
- Resize: 637/635/632 timed updates in three 30-second sweeps. UI-thread work
  medians 17.4/19.7/20.3 ms, p95 30.7/34.8/43.2 ms, maxima 61.2/101.1/99.2 ms.
  Paint interval p95 approximately 57-60 ms; maxima 88-93 ms. These timer/paint
  intervals do not establish smooth animation. No motion is implemented.
- Warm popup medians/p95/max: dropdown 49.4/62.1/69.9 ms; collapsed group
  55.7/70.8/97.1 ms; ten nested openings 44.3/48.2/48.6 ms.
- Separate first-open probe: dropdown 117.4 ms, collapsed group 57.7 ms. It uses
  a fresh process; it is distinct from the warmed 100-cycle distributions.
- During the warmed popup checkpoints GDI/USER counts stayed at 25/31, with
  total handles 371-372. After Form disposal and GC: GDI 22, USER 14, total
  handles 364; managed bytes about 0.61 MB versus 1.09 MB at initial warm-up.
  Private bytes rose from 12.4 MB to 39.3 MB before GC, then 35.5 MB. This short
  workload shows no sustained GDI/USER growth; it does not establish leak freedom.

One benchmark setup failed after the completed resize sweeps: a monitor tracking
limit kept the normal-dropdown opener collapsed. The successful resize evidence
was retained; only popup measurement was retried after explicitly keeping that
group expanded. The earlier outside-press test also hit the popup rather than
uncovered document space; correcting the test geometry passed without adding a
focus workaround. These failures are not concealed as successful whole runs.

## Local artifacts and reproducibility

Ignored local evidence is under `TestResults/`: `validation-release.log`,
`validation-inactive-provider.log`,
`baseline/startup.json`, `baseline/native-baseline.json` (completed resize plus
the recorded fixture failure), `baseline/native-popups.json`,
`baseline/first-popups.json` and `visual/native-*-120dpi.png`.
The popup-width follow-up adds `visual/popup-width/` (test-runner DPI 96) and
`visual/popup-width-native/popup-{checked,hover}-120dpi.png` (native PMv2 window).
`popup-width-consumer-path.txt` records the fresh package consumer location.
The disabled/activation correction adds `disabled-before.log`,
`disabled-popup-focused.log`, `disabled-popup-native.log`,
`disabled-popup-package.log`, `disabled-popup-consumer-path.txt`, and
`visual/popup-disabled/popup-disabled-{hover,pressed}-120dpi.png`.
The focus-cue correction adds `focus-cues-{before,focused,build,native,keyboard,package,package-keyboard}.log`,
`focus-cues-consumer-path.txt`, and `visual/focus-{test-host,package-consumer}/`.
The external package consumer's retained temporary path is printed in the log.

From this folder's parent (`winforms/`), `eng/Test-Native.ps1 -Configuration Release -Package`
rebuilds, tests, packs and runs a fresh isolated package consumer.
`eng/Measure-Baseline.ps1` executes the full baseline; `-Scope Startup` or
`-Scope Popups` isolates unfinished measurement work. No passed workload needs
repeating without a relevant change or unresolved concern.
Capture the native popup alone with `dotnet run --project tests/RibbonKit.WinForms.NativeTests -- --capture-popups TestResults/visual/popup-width-native`.
Run just window-switch dismissal with `dotnet run --project tests/RibbonKit.WinForms.NativeTests -- --popup-dismissal`;
append `--app <consumer.exe>` for the package consumer. The normal native validation
script now includes this scope.
Run native screen/input focus checks with `dotnet run --project tests/RibbonKit.WinForms.NativeTests -- --focus-cues TestResults/visual/focus-test-host`;
append `--app <consumer.exe>` for an independent consumer. This scope is also in
the normal native validation script.

Next human review: open `RibbonKit.WinForms.sln`, use the sample's normal Debug
Form, then complete F8 and the applicable F5/F7/F9 native cases. Reopen VS and the
package consumer designer; edit/reorder/remove tab/group/item collections,
resources, events and binding; verify one-step undo/redo and save/reopen/build.
Showcase review, 2026-10-26: the user reported that the current Showcase is going
well and explicitly authorized the next slice. Continue with the bounded
[M2A split-button work](M2-STATUS.md); the remaining M1 gates above stay open and
are not implied to have passed by that Showcase review.
