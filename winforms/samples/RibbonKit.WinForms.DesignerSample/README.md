# RibbonKit WinForms Designer Sample

This is the permanent editable review project, migrated from the existing
bottom-navigation review solution with its reviewed Forms, UserControls,
resources and assets preserved. Keep making designer changes here.

From the repository root, refresh the package and build this same project:

```powershell
./winforms/eng/Update-DesignerSample.ps1
```

Then open `RibbonKit.WinForms.DesignerSample.sln` in this folder and open
`DesignerForm` in the Visual Studio Form designer. Reopen the solution after a
package refresh so Visual Studio reloads the design assemblies. F5 runs the sample.

The refresh builds the native runtime/design server/client in normal Debug
folders, creates a local package, restores and builds the existing sample.
`-NoBuild` reuses already-built matching designer outputs; `-Configuration Release`
is available when explicitly needed. It never recopies fixture files or replaces
your authored C#, resx, assets or project settings. No tests run automatically.

The sample uses a **PackageReference**, including the packaged designer support.
Its own solution is separate from the native source-build solution so package
preparation can finish before the sample is restored. The private feed/cache lives
under ignored `winforms/artifacts/designer-sample/`; the selected local package
version lives in ignored `.local/DesignerPackage.props`. Each refresh uses a new
local prerelease version to avoid stale package copies without clearing caches.
Normal `bin/Debug` and `obj` folders are used. The project's source namespace is
retained to preserve resource and generated designer relationships.

`eng/Prepare-DesignerConsumer.ps1` with no destination also refreshes this sample.
An explicit `-Destination` still creates a separate package-only fixture when a
specific independent verification requires one; it is not the normal user review
workflow. The former temporary review remains a backup, not the active project.

The copied project also retains some older fixture-verification flags. Their
assertions describe the original fixture and may no longer match an edited sample.
In particular, `--verify-roundtrip` expects the Bold SVG icon that was already
removed from the reviewed Form; it is not a passing acceptance check for this
editable copy. Migration preserved that design and did not relax the assertion.
