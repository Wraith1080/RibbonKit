# RibbonKit WinForms Designer Sample

This is the persistent, editable Visual Studio sample. Normal startup contains
the authored **Home, Edit, Gallery and View** tabs. Test code, profilers and retired
rewrite/consolidation review tabs are excluded from this application.

Open `RibbonKit.WinForms.DesignerSample.sln` and run normally with F5, or from the
repository root:

```powershell
dotnet run --project winforms/samples/RibbonKit.WinForms.DesignerSample -c Debug
```

View offers Office 2013/2019/2024 Light and Application theme, six accents plus
Default, and Color title bar with white title text. Classic has inline tiles;
Simplified offers gallery launchers. Hover previews do not apply preferences.
Default resets only the accent; title-color off restores its previous roles.
Office 2019 has a neutral grey header and white body. Office 2013 uses the
connected grey outline and immediate selected-header feedback while content
continues animating. Office 2024 retains its independent sliding pill.

The galleries and title toggle are authored fields/collections in
DesignerForm.Designer.cs. SVG constructors supply both source and color-mode
arguments so designer serialization preserves the artwork. Gallery cards remain
ordinary designer-editable UserControls. Backstage Info/Print and popup content
also remain authored controls; their content and event handlers belong to this
sample. Keep editing these files and resources in place.

Right-click the ribbon for Simplified Ribbon and customization. The Mouse/Touch
selector changes density independently. `--office2019-light` selects Office 2019;
the old selection-only `--office2019-theme-demo` alias still works, and
`--menu-geometry-demo` adds no tabs.

## Refresh the local package

From the repository root:

```powershell
& winforms/eng/Update-DesignerSample.ps1 -Configuration Debug
```

The refresh preserves authored forms, UserControls, project files and resources.
Reopen the solution in Visual Studio afterward. Normal builds, launch and refresh
do not run checks. Keep the ordinary Debug/Release output folders.

## Verification lives under tests

All former sample checks and profiles now live in
[the dedicated test host](../../tests/RibbonKit.WinForms.DesignerSample.Tests/README.md).
It compiles the real authored sample sources/resources against the same selected
public package. The sample application does not compile or reference that host.

```powershell
& winforms/eng/Test-DesignerSample.ps1 -List
& winforms/eng/Test-DesignerSample.ps1 -Check --verify-sample-isolation
& winforms/eng/Test-DesignerSample.ps1 -Check --verify-theme-gallery
& winforms/eng/Test-DesignerSample.ps1 -Check --verify-office2013-composed
```

The ordinary application rejects verification/profile/generator/trace commands
with exit code 2. Use the runner so old automation cannot silently open the sample
and report success. Retired review-tab builders and their UI-only smoke checks
have been removed; focused control regressions remain in tests.

Current theme scope and acceptance belong to the
[themes ledger](../../docs/THEMES-PLAN.md#current-status-and-review-ledger).
Generated-code and offscreen checks remain separate from actual Visual Studio
save/reopen, physical input, desktop material pixels and mixed-monitor review.
