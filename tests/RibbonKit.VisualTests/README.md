# RibbonKit visual snapshots

This project renders fixed RibbonKit scenes off-screen and compares them with approved PNGs.
It complements the logic-focused `RibbonKit.Tests` project; it does not open a window or depend on
the showcase's persisted state.

The first slice covers Office 2024 at 100% DPI. It fixes culture, dimensions, text rendering,
layout rounding, and software rendering, and disables RibbonKit animation before taking a snapshot.
Each scene is rendered twice and must produce identical pixels before it is compared with the
approved image.

Approved images live in `Snapshots/approved` and are reviewed like source changes. To intentionally
replace them after reviewing a visual change, run:

```powershell
$env:RIBBONKIT_UPDATE_SNAPSHOTS = '1'
dotnet test .\tests\RibbonKit.VisualTests\RibbonKit.VisualTests.csproj --configuration Release
Remove-Item Env:RIBBONKIT_UPDATE_SNAPSHOTS
```

On a mismatch, the test writes the actual image and a magnified difference image beneath
`TestResults/visual`; that directory is already ignored by Git.
