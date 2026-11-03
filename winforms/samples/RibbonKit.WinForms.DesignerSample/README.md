# RibbonKit WinForms Designer Sample

The bottom-docked `statusStrip` replaces the floating binding textbox and status
label. Its designer-authored labels show the latest status message and the
current **Name:** value. Edit **Name** on the ribbon's Edit tab to see the bound
value update; status messages from commands, galleries and Mouse/Touch changes
use the left label. The value label is read-only in the UI and remains the
source of the existing WinForms binding.

The Showcase-style **Mouse/Touch** selector is available under **Home → Input mode**
and as the **third Quick Access entry**, including while reviewing the geometry
tab. It uses the illustrated choices, heading, descriptions and Mouse/Touch
artwork. Choosing a mode updates ribbon density, the selected radio, caption and
icon; rejected native-editor validation restores the effective selection.

The group, dropdown, radio choices, QAT source reference and named event hookups
are declared in `DesignerForm.Designer.cs`, so they remain editable through
**Edit Ribbon…** and Properties. `DesignerForm.InputMode.cs` contains only the
density event handlers/synchronization. `DesignerForm.resx` owns typed SVG icon
resources, adapted from Showcase's vector artwork; there is no
Showcase assembly/helper dependency or runtime-created selector.

Verification, 2026-11-03: normal Debug sample build passed with zero warnings or
errors. `--verify-input-mode` passed public-package native pointer/keyboard checks
at actual 120 DPI, retained editor HWND/value, no-op/current choice, external
density synchronization and validation veto. Menu metadata/QAT source CodeDOM
serialization and SVG converter round-trips passed; Mouse/Touch captures were
inspected in `artifacts/designer-input-mode/`. The reported line-836 designer
error was corrected with standard typed-resource assignments for all three Icon
properties. The post-fix Debug build and `--verify-input-mode` passed, now including
those Icon serialization paths and compiled typed resources; evidence is in
`artifacts/designer-input-mode/icon-designer-fix/`. **User confirmed the repaired
designer works on 2026-11-03** ("everything works now"). Separate save/reopen/Undo
cycles were not recorded. The older `--verify-roundtrip` helper stops at its pre-existing
Command identity expectation: it expects Bold in Commands, while read-only HEAD
already declares it in Formatting. That helper and the authored grouping are
unchanged; evidence is in `artifacts/designer-input-mode/roundtrip/`.

For the opt-in ordinary-menu geometry example, launch the normal Debug sample
with `--menu-geometry-demo`. On its **Menu geometry** tab, use **Wider menu spacing**,
open **Try this menu**, then use **Reset menu spacing** and reopen it. The example
uses only public theme properties; ordinary startup and the authored File
presentation stay unchanged. The [geometry plan](../../docs/GEOMETRY-CONSOLIDATION-PLAN.md)
records the bounded scope and review gate.

The same tab now includes **Vertical menu spacing**. Open **Try described menu**
at defaults, close it, choose **More vertical spacing**, and reopen. Check the
icon clearance, title/description gap, separator spacing and native Name field.
The **Keep formatting** checkbox should toggle without closing the menu. Use
**Reset vertical spacing** and reopen to compare; this resets only the four
vertical roles and preserves any horizontal settings from the first group.

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

For Application Menu review, select the ribbon and use **File opens → Application
Menu**, then **Edit Application Menu…**. The generated example has an Open pane
using `PopupOptionsControl`, large command icons, split Save As with illustrated
format choices, Print/Prepare/Send/Publish panes, numbered recent documents and
Options/Exit footer buttons. Its existing Backstage declaration/startup choice
is preserved. `--verify-application-menu <evidence-folder>` runs the bounded
package/generated-code and retained native content check in this same sample,
and saves actual default/Print/native/Save As captures in that folder. Hover a
left dropdown or either Save As half to show its pane; hover New/Save/Close to
restore recent documents. Moving into the right pane retains the selected source.

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
