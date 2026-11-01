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

The project includes `app.manifest` with Windows compatibility enabled. Native
ribbon animations use layered child windows and need this host declaration,
matching Showcase and the independent runtime consumer. Without it, an authored
Subtle/Expressive value can load correctly while native window creation fails and
the ribbon falls back to immediate changes. Windows' disabled-animation preference
still takes precedence.

In **Edit Ribbon → Ribbon → Ribbon settings**, **Animation** chooses
None/Subtle/Expressive and **Tab content animation** chooses Fade/SlideDown.
The latter also applies to arriving tab headers, group collapse/restore and
Backstage page changes. Groups retain sideways travel. Backstage opening/closing
keeps its accepted complete-surface motion; page changes move/fade only the live
incoming body, with navigation/frame/caption fixed.

With Subtle or Expressive selected, this focused public-API check runs the sample's
compiled initializer and verifies tab motion, Backstage opening, both native page
styles and closing, retaining native page owners and handles. In check mode only,
it also adds real RichTextBox controls and verifies their handles, RTF and selection:

```powershell
dotnet run --project winforms/samples/RibbonKit.WinForms.DesignerSample -c Debug -- --verify-animation TestResults/designer-sample-animation
```

It uses an offscreen native Form and real UI timers. It does not force-enable
system animations, edit the authored setting, or establish visual/frame-pacing
acceptance. It reports a disabled Windows animation preference rather than
overriding it. No tests are run by a normal launch or package refresh.

Backstage page changes buffer native children within the library-owned page
container. Their drawing is retained through Fade/SlideDown without a forced
completion erase. Text/RichTextBox controls remain live; no page capture or redraw
freeze is used. Review visible smoothness in this sample after reopening the solution.

`eng/Prepare-DesignerConsumer.ps1` with no destination also refreshes this sample.
An explicit `-Destination` still creates a separate package-only fixture when a
specific independent verification requires one; it is not the normal user review
workflow. The former temporary review remains a backup, not the active project.

The copied project also retains some older fixture-verification flags. Their
assertions describe the original fixture and may no longer match an edited sample.
In particular, `--verify-roundtrip` expects the Bold SVG icon that was already
removed from the reviewed Form; it is not a passing acceptance check for this
editable copy. Migration preserved that design and did not relax the assertion.
