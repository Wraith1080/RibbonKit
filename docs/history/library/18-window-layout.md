# Library history: Window layout

> Implementation decisions and regression pitfalls. Current acceptance belongs in
> the [1.1 plan](../../20-V1.1-ACCEPTANCE-AND-RELEASE-PLAN.md#height-resize-caption-stability--2026-10-24).

[History directory](../01-library.md) · [Design index](../../../04-DESIGN-NOTES.md#374-visual-studio-debugger-distorted-live-resize-performance--2026-08-10).

### Height-resize caption stability — 2026-10-24

Writer's W6 review reported ribbon/document jumping during height resizing outside
the debugger. The supplied 0.8-second recording and an initial programmed resize
did not establish a complete reproduction of that visible flicker. A smaller
shared defect was reproduced independently: at native 125%, the caption alternates
between 42 and 43 pixels as total window height changes. Four Compact cases fail
before correction; Touch cases retain their height.

The 34-DIP minimum now belongs to TitleBarBand rather than the Auto RowDefinition.
WPF rounds the caption's desired height before the Auto/star grid divides remaining
pixels. Caption/body origins stay stable through growth and shrinkage without
replacing the layout panel, adding resize hooks, changing shadows or adding API.
Touch's existing MinHeight trigger and authored title-bar content remain effective.
This is separate from the earlier debugger-only performance investigation (§3.74).

Regression coverage includes native-window height steps, Writer's Crystal paper
underlay with ruler shown/hidden and both densities, and a separate RibbonKit-only
consumer across seven themes, light/dark, LTR/RTL and both caption densities at the
machine's native 125%. Initial consumer-fixture drafts directly rearranged a
connected window's root across simulated DPI values and read inconsistent parent
arrangement; the retained check uses normal HWND height changes instead. Those
drafts establish no additional DPI coverage. The temporary performance diagnostic
is archived outside the project; its reentrant render-callback cadence is not a
performance measurement. Exact final checks and remaining visual review are in
the active plan.

### Native resize paint reuse — 2026-10-24

The follow-up Writer and Showcase recordings clarified whole-window image jumps,
with stable tracked internal caption/ribbon spacing. The user observes this outside
the debugger too, authorized minimizing it and preferred the cached diagnostic
variant. This is separate from both the one-pixel caption correction above and
the old debugger-only slowdown. WPF's native interactive resize path waits for a
rendered frame in [HwndTarget.OnResize](https://github.com/dotnet/wpf/blob/v8.0.31/src/Microsoft.DotNet.Wpf/src/PresentationCore/System/Windows/InterOp/HwndTarget.cs);
ordinary programmatic size changes do not reproduce that scheduling by themselves.

RibbonWindow's existing native hook now scopes RibbonResizePaintCache to
WM_ENTERSIZEMOVE/WM_EXITSIZEMOVE and WM_CANCELMODE. The helper caches loaded visible
Ribbon controls and their detached title-bar QAT, with ClearType and pixel snapping
enabled. It reuses device-resolution paint while keeping the live visual/input
tree: content/size changes invalidate the cache normally. It does not cache the
host document, suppress shadows, replace window painting, force synchronous
rendering or add an integration API. No artificial warm-up delay is retained.

Only default CacheMode values are eligible. Local values (including explicit
null), styles, bindings and animated/current values are left alone. Cleanup removes
only the helper's still-current instance, preserving an author's replacement made
during the drag. Native exit/cancel, deactivation, unload/close, theme, window-state
and DPI changes dispose the temporary cache; ordinary vector rendering then resumes.
This avoids retaining a raster cache across monitor changes or outside the loop.

Native-message diagnostics without a debugger found low UI-thread CPU compared
with elapsed native resize-call time, consistent with presentation waits rather
than a layout storm, but do not identify a GPU/driver defect. Shadow-only caching
and removing those decorations did not yield useful gains and were discarded.
Whole-ribbon caching improved the manually primed prototype and the integrated
Writer run; another integrated minimal-consumer run did not improve. Keep both
results: compositor cadence/load make these probes variable, not a frame-rate
guarantee. Diagnostic source and JSON are retained outside test projects under
TestResults/v1.1-height-resize-20261024; the active plan records exact evidence.

Permanent coverage exercises native-message cache lifecycle and authored-value
preservation, Writer document/selection/zoom retention while caching is active,
and a RibbonKit-only consumer with all QAT positions and both densities in Crystal
and Office 2024 Flat. Before/during/after render captures show live header updates
and intact paint. RenderTargetBitmap captures do not establish desktop presentation
smoothness, other native DPI scales or mixed-monitor acceptance. The user's
preference for the diagnostic variant is not final acceptance of the integrated fix.
