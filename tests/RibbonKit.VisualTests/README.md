# RibbonKit visual snapshots

The harness renders fixed scenes off-screen, independent of Showcase preferences.
It pins culture, dimensions, text rendering, rounding, software rendering and disabled
animation. Each scene renders twice and must be pixel-identical before approval comparison.

The source inventory on 2026-09-08 contains 63 approved PNGs: five themes in light and
dark/black at 100/125/150/200% plus focused RTL, File, Backstage, application-menu,
merged-tab and window-frame scenes. This is a file count, not a fresh test result.
See the test source for the exact scene matrix rather than maintaining another list.

The harness assigns and checks root DPI before layout; bitmap DPI metadata alone
cannot control a disconnected WPF tree. RTL cases separate invariant-English layout
from representative bidi shaping. Open/checked controls stand in for state brushes
where disconnected `IsMouseOver` cannot be forced reliably. These snapshots do not
establish actual mouse, IME, monitor-transition or DWM-material acceptance.

## Review and update

Approved images are under `Snapshots/approved`. Inspect actual and magnified diff
PNGs in `TestResults/visual` before deliberately replacing a baseline or threshold.
CI uploads them in the failure-only `visual-snapshot-diagnostics` artifact. An early
matrix failure says nothing about later scenes.

After the intended visual change has been reviewed, run from the repository root:

```powershell
$previousSnapshotUpdate = $env:RIBBONKIT_UPDATE_SNAPSHOTS
try {
    $env:RIBBONKIT_UPDATE_SNAPSHOTS = '1'
    dotnet test tests/RibbonKit.VisualTests/RibbonKit.VisualTests.csproj --configuration Release
} finally {
    $env:RIBBONKIT_UPDATE_SNAPSHOTS = $previousSnapshotUpdate
}
```

Review the resulting image diff and test output. Ordinary verification leaves the
update variable unset; see [validation tiers](../../CONTRIBUTING.md#proportional-validation).
