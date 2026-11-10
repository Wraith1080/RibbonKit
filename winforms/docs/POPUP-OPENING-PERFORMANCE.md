# Gallery and dropdown opening performance

Status **2026-11-10**: implemented; focused automated and native validation below.
Human review of perceived opening responsiveness remains pending.

## Reproduction and historical comparison

Compared detached revisions using the same native benchmark source, normal Release
outputs, actual 120 DPI, animation disabled and one visible command group. The
first opening is reported separately; medians use 100 repeat ordinary-menu
openings or 20 repeat large-gallery/content openings. Times measure synchronous
ShowDropDown plus explicit native Update/paint, not desktop-compositor latency.
Revisions are `7e1e753` before custom presenters, `779f529` after presenter/content
work, `9dd0054` before geometry, and `2864e1a` before this correction.

| Scope, repeat median (ms) | Before presenters | After presenters | Before geometry | Current before fix | Fixed |
| --- | ---: | ---: | ---: | ---: | ---: |
| Ordinary command dropdown | 55.59 | 54.53 | 61.28 | 55.48 | 36.41 |
| Built-in gallery, 1,000 tiles | 66.05 | 63.37 | 63.68 | 72.71 | 52.58 |
| Custom dropdown body, 1,000 ListBox entries | n/a | 56.39 | 58.80 | 56.73 | 44.42 |

The retained dropdown workload is an application UserControl containing a native
ListBox with 1,000 entries; it does not create 1,000 child controls. Custom bodies
did not exist at the first revision. Historical results show no large shared
presenter-era slowdown. The current built-in gallery is somewhat slower in this
run; this local comparison does not isolate small geometry costs from variation.

The Designer Sample's actual native-card gallery scales very differently:

| Custom gallery, 1,000 source entries, Expressive | Before fix | Fixed |
| --- | ---: | ---: |
| First synchronous opening | 2,633.31 ms | 130.46 ms |
| Median of three repeat synchronous openings | 2,217.09 ms | 84.87 ms |
| Initial realized native cards | All 1,000 | 6 |

The fixed 200-entry scope measured 127.50 ms for its first synchronous opening
and 79.83 ms for the median repeat, also realizing six cards initially.

These sample timings use normal Debug/package-only outputs, independently of the
Release benchmark above. The first opening uses a fresh process; repeat custom
openings still create fresh presenter instances. Fixed total settling is about
295–346 ms, including the unchanged 182 ms gallery entrance and timer/handoff
work. The older diagnostic's settling deadline was measured from request time,
so its >2-second synchronous openings did not wait for motion completion; only
its synchronous measurements are compared here. A 200-entry early diagnostic
lost foreground after two samples and is not a completed benchmark.

Raw outputs, exact first-opening samples and resource checkpoints are under
`winforms/artifacts/popup-opening-performance/`; `comparison.json` summarizes the
Release scopes. `before-1000/result.log` and `final-1000/result.log` contain the
custom-gallery comparison. Do not combine Debug and Release timings into one
population or treat these local samples as a general performance guarantee.

## Causes and correction

`RibbonPopupWindow` set AutoClose=false before disabling the system class shadow.
WinForms' AutoClose setter applies native topmost state, creating the HWND;
changing DropShadowEnabled afterward recreates it. Per-phase probes measured
roughly 17–22 ms for that unnecessary second setup. The constructor now sets
shadow, renderer and other properties before AutoClose. This applies to shared
gallery, command, custom-content and choice popups without caching their views.
The [WinForms implementation](https://github.com/dotnet/winforms/blob/v10.0.0/src/System.Windows.Forms/System/Windows/Forms/Controls/ToolStrips/ToolStripDropDown.cs)
documents both setter paths. A thread-local native CBT hook regression observes
window destruction during construction: it fails on `2864e1a` and passes with the
new ordering, while verifying system shadow remains disabled.

The sample `GalleryStylesControl` constructed every card tree during Synchronize,
then marked every card visible, including offscreen rows. Parenting, HWND creation
and native capture therefore grew with the whole collection. It now calculates
all item bounds from an index and creates card trees only for visible rows.
Visited cards retain identity and native handles until their popup closes.
Initial realization is capped by both the monitor budget and preferred size,
avoiding a transient monitor-sized set before final measurement. Native
first-show autoscaling runs before physical card bounds are applied. The six
designer-authored cards remain available in the ordinary UserControl designer.

This card realization belongs to the supplied application presenter. RibbonKit
cannot virtualize an arbitrary application control tree. Other custom presenters
should follow this pattern, keep bounds lookups inexpensive, and choose their
own visible-row strategy. Per-opening ownership, retained dropdown-body lifetime,
selection/preview, commands, motion duration and geometry contracts are unchanged.

## Verification and reproduction

- Release regression/related slice: **168/168** passed across native-window
  creation, popup motion/layout/hover, gallery presenter/type/QAT, retained
  dropdown content/routing, state-command retention and frame geometry.
- The new native-window regression failed before the correction (**1/1**) and
  passed afterward. Its failure was actual window destruction, not a timing limit.
- Normal Debug runtime/designer, Showcase and package-only Designer Sample builds
  passed without warnings/errors. Release native/test project builds passed too.
- Package-only custom-gallery checks passed with **18, 200 and 1,000 entries**, including lazy
  offscreen allocation, last-item reveal, return to visited handles, preview,
  cancellation, exactly-once native child-label commits, fresh reopening and
  disposal. Initial and last-selected native screenshots were inspected at 120 DPI.
- Package-only animated retained dropdown/split content and QAT gallery checks
  passed, preserving native editor state, native ComboBox first-Escape handling,
  validation, inactive-tab/QAT overflow routes and exactly-once activation.
  That earlier retained-content check did not assert that motion actually ran.
  The subsequent [drawing-hold correction](DROPDOWN-CONTENT-PLAN.md#native-drawing-hold-correction--2026-11-10)
  reproduces and fixes the caught error 87 and adds active-motion/no-exception gates.

Run from the parent checkout after refreshing the normal Designer Sample package:

```powershell
./winforms/eng/Update-DesignerSample.ps1 -Configuration Debug
dotnet run --project winforms/samples/RibbonKit.WinForms.DesignerSample -c Debug --no-build -- --profile-gallery-opening winforms/artifacts/popup-opening-performance/review-1000 1000
dotnet run --project winforms/samples/RibbonKit.WinForms.DesignerSample -c Debug --no-build -- --verify-gallery-presenter winforms/artifacts/popup-opening-performance/check-1000 1000
dotnet build winforms/tests/RibbonKit.WinForms.NativeTests -c Release
dotnet run --project winforms/tests/RibbonKit.WinForms.NativeTests -c Release --no-build -- --benchmark-gallery winforms/artifacts/popup-opening-performance/gallery-1000.json 1000
dotnet run --project winforms/tests/RibbonKit.WinForms.NativeTests -c Release --no-build -- --benchmark-content winforms/artifacts/popup-opening-performance/content-1000.json 1000
```

The optional count also supports the ordinary 18-entry and 200-entry cases.
No WPF changes, full-suite run, actual Visual Studio editing, external UIA,
physical mixed-monitor/touch/IME campaign or general resource-plateau certification
is claimed. No commit or publication was requested.
