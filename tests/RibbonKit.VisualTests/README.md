# RibbonKit visual snapshots

This project renders fixed RibbonKit scenes off-screen and compares them with approved PNGs.
It complements the logic-focused `RibbonKit.Tests` project; it does not open a window or depend on
the showcase's persisted state.

The suite covers all five themes at 100%, 125%, 150%, and 200% DPI. It fixes culture, dimensions,
text rendering, layout rounding, and software rendering, and disables RibbonKit animation before
taking each snapshot. Every scene is rendered twice and must produce identical pixels before it is
compared with the approved image.

`RenderTargetBitmap`'s 96-DPI setting does not override the DPI WPF assigns to a disconnected visual
tree. The harness explicitly assigns each scene's root DPI with `VisualTreeHelper.SetRootDpi` before
layout, verifies that WPF reports the requested value, and scales both bitmap dimensions and DPI
metadata consistently. The matrix is therefore independent of the physical monitor's scaling.

Approved images live in `Snapshots/approved` and are reviewed like source changes. To intentionally
replace them after reviewing a visual change, run:

```powershell
$env:RIBBONKIT_UPDATE_SNAPSHOTS = '1'
dotnet test .\tests\RibbonKit.VisualTests\RibbonKit.VisualTests.csproj --configuration Release
Remove-Item Env:RIBBONKIT_UPDATE_SNAPSHOTS
```

On a mismatch, the test writes the actual image and a magnified difference image beneath
`TestResults/visual`; that directory is already ignored by Git.
