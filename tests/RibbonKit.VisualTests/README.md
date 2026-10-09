# RibbonKit visual snapshots

The harness renders fixed scenes off-screen, independent of Showcase preferences.
It pins culture, dimensions, text rendering, rounding, software rendering and disabled
animation. Each scene renders twice and must be pixel-identical before approval comparison.

The approved images include Office themes in light and dark/black at
100/125/150/200%, plus focused RTL, File, Backstage, application-menu, QAT,
message-bar, utility and Crystal scenes. Image counts are not test results: the
matrix is one aggregate test, with its selected scene count reported separately.
See the test source for the exact scene matrix rather than maintaining another list.

The harness assigns and checks root DPI before layout; bitmap DPI metadata alone
cannot control a disconnected WPF tree. RTL cases separate invariant-English layout
from representative bidi shaping. Open/checked controls stand in for state brushes
where disconnected `IsMouseOver` cannot be forced reliably. These snapshots do not
establish actual mouse, IME, monitor-transition or DWM-material acceptance.

## Select affected scenes

For a bounded visual change, run only the affected scenes from the repository root:

```powershell
./eng/Test-Focused.ps1 -Suite Visual -Scenes 'office2024-default-100,office2024-rtl-100'

./eng/Test-Focused.ps1 -Suite Visual -Scenes 'crystal-*-qat-*' -Configuration Release
```

Scene names match approved PNG filenames without `.png`. Comma-separated patterns
use case-insensitive `*` and `?` wildcards; every pattern must match a scene or the
run fails. Selected scenes still render twice and compare against the same
approvals with the same thresholds. The output lists completed scene names/count;
resource/lifecycle harness assertions still run. Report this as selected visual
coverage, not a complete theme/DPI matrix pass.

Focused dark Crystal runs initialize an off-screen native WPF window before the
disconnected fixtures. The full matrix normally does this in earlier light scenes;
preserving that initialization keeps the existing approvals independent of selection.

The helper restores inherited environment settings and always compares approvals.
For direct invocation, set `RIBBONKIT_VISUAL_SCENES` temporarily and restore its
previous value in `finally`. Leaving it unset runs the full matrix. Full validation
also requires `RIBBONKIT_CAPTURE_SNAPSHOTS` and `RIBBONKIT_UPDATE_SNAPSHOTS` unset.
CI explicitly clears all local selection and capture/update flags.

## Review and update

Approved images are under `Snapshots/approved`. Inspect actual and magnified diff
PNGs in `TestResults/visual` before deliberately replacing a baseline or threshold.
CI uploads them in the failure-only `visual-snapshot-diagnostics` artifact. An early
matrix failure says nothing about later scenes.

To render every scene for review without changing approvals, set
`RIBBONKIT_CAPTURE_SNAPSHOTS=1` for one direct test run with
`RIBBONKIT_VISUAL_SCENES` unset. Set a scene selection instead to capture a subset.
Actual PNGs and diffs for mismatches are written to `TestResults/visual-capture`;
clear the capture variable for
the normal comparison run.

After the intended visual change has been reviewed, run from the repository root.
Clear `RIBBONKIT_CAPTURE_SNAPSHOTS`; leave `RIBBONKIT_VISUAL_SCENES` unset to update
the full matrix, or explicitly select the reviewed scenes:

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
