# RibbonKit visual snapshots

This project renders fixed RibbonKit scenes off-screen and compares them with approved PNGs.
It complements the logic-focused `RibbonKit.Tests` project; it does not open a window or depend on
the showcase's persisted state.

The suite covers the light and dark/black variants of all five themes at 100%, 125%, 150%, and 200%
DPI. It fixes culture, dimensions,
text rendering, layout rounding, and software rendering, and disables RibbonKit animation before
taking each snapshot. Every scene is rendered twice and must produce identical pixels before it is
compared with the approved image.

Two additional Office 2024 snapshots at 100% apply `FlowDirection.RightToLeft`: one to the ribbon
scene and one to the real QAT-customization template. They keep invariant English resources so
mirrored layout/action-direction regressions are not confused with translation or font fallback.

A focused Office 2010 snapshot at 100% renders the open File application button, a checked toggle,
an open dropdown, and both halves of an open split button. These are deterministic stand-ins for the
shared state brushes because `IsMouseOver` cannot be forced reliably on the disconnected visual tree.
A second focused Office 2010 snapshot renders the real `Classic2010` Backstage shell, including its
square radial selection marker and content drop shadow. Two more focused snapshots render the complete
Office 2007 Black and Office 2010 Black application menus. Together with the 40-image matrix and RTL
smoke scenes, the suite contains 46 approved PNGs.

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
