# RibbonKit WinForms Designer Sample

The paired **Command content** / **Application Menu geometry** review tabs are
available with `--menu-geometry-demo`, alongside **Menu geometry**, **Illustrated
menus** and **Command splits**. This launch selects Application Menu geometry;
normal startup and authored Forms/resources remain unchanged.

- **Command content:** compare **Roomier command content** / **Reset command
  content** in Mouse/Touch and Classic/Simplified. Check centered Small icons,
  Medium caption clearance, check/radio alignment and separate split actions.
  Apply sets horizontal padding 12 / vertical padding 16; reset clears only these.
- **Application Menu geometry:** apply **Roomier Application Menu**, then **Open
  Application Menu** to open the existing authored File example in CommandMenu
  mode. Compare navigation/pane/footer row spacing, pane inset, wrapped footer
  gaps and scroll reachability. Roomier navigation extends to the shared divider;
  the active title starts beside it, with the content inset retained. Apply sets
  64/72/52, inset 22 and gap 12. Reset
  clears only these five roles; **Restore startup File mode** restores the
  presentation captured when the demo was enabled.
- Combine these with earlier geometry tabs, then reset independently. Existing
  editor text, File/default/footer models, command state, handlers and QAT sources
  should remain. These tabs use public, code-only Theme settings; they do not add
  theme properties to the designer or replace the generated initializer.

Build/package evidence and the combined review gate are in the
[geometry plan](../../docs/GEOMETRY-CONSOLIDATION-PLAN.md). The opt-in
`--verify-paired-geometry <output>` check exercises actual public command layout/
paint in both renderers/densities/layouts, combined settings, exact reset and
sample ownership without shown windows or global input. Application Menu native
layout/scroll/hover/validation/retained HWND and inert preview checks are separate
library STA evidence. Actual VS and physical input/hardware review remains separate.

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

Touch correction, 2026-11-04: Small command icons center within their usable
button area. After the user found the initial arrow-only width allowance too
small, ComboBoxes and TextBoxes now scale their widths by the field-height
increase, using the same current font/DPI rule. For example, 24 → 42 pixel height
gives 1.75× width. The arrow allowance remains a floor for larger fonts.
Increase either model's
`InputWidth` in Layout properties for more width; the authored value is the Compact
width and stays unchanged when switching density. The refreshed normal Debug
package preserves the authored Form/resources. Review **Home → Formatting** in
Mouse and Touch; live Visual Studio review remains user-owned.

`--verify-group-layout <evidence-folder>` also checks centered Small Touch icons,
proportional matching field widths and retained native handles through public package APIs on
an offscreen Form. This bounded check passed at actual 120 DPI; its captures and
result are in `artifacts/touch-width-proportional/package-group/`. It uses no
Showcase resources, global input or computer-use automation.

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

The third group, **Menu size, heading and overflow**, batches the next six theme
roles. Open **Try headed menu** at defaults; choose **Roomier menu containers**
and reopen to compare width, heading padding and the gap before the first row.
Turn **Overflow examples** on, then open Quick Access overflow and review the
top/bottom scroll bands and row reveal in Mouse/Touch. Turn the toggle off to
remove only those 28 temporary entries (24 commands and four references); authored
QAT entries/order are retained.
**Reset menu containers** resets only this batch's six properties, preserving
the earlier horizontal/vertical settings and the accepted Touch field scaling.
These opt-in controls are public-API demo code; the authored Form/resources and
normal startup presentation are preserved.

Menu-editor alignment follow-up: **Try this menu** also contains a labelled
**Font family** ComboBox and an unlabelled chooser. With **Wider menu spacing**
applied, **Name** and **Font family** labels align with neighboring command text,
and their fields share a starting column. The unlabelled ComboBox starts directly
at the command-text column. Compare Mouse/Touch and Reset; authored `InputWidth`
and existing Touch scaling remain in effect. The same content geometry is used
by native presenters and inert designer preview. No authored Form/resource file
was changed for these opt-in examples.

Menu fill-width follow-up: the opt-in demo's editor examples now explicitly use
`FillMenuWidth=true`. **Fill menu fields** starts checked; turn it off to compare
fixed `InputWidth` sizing, then back on to fill the available row in all three
example menus. Combine **Wider menu spacing** with **Roomier menu containers**
for a clear width comparison. Visible labels stay aligned; the unlabelled chooser
fills from the command-text column, and heights stay unchanged in each density.
The model property defaults to false outside these explicitly configured examples.
It is available under Layout in the designer, with Boolean serialization/reset;
actual Visual Studio review remains user-owned.

The fourth group, **Menu arrows and separators**, has **Wider arrow areas**,
**Reset arrow areas** and **Try arrow menu**. Compare the Export submenu indicator,
the two Paste split areas and separator ends at defaults and with overrides.
Paste's primary toggles its state; its arrow opens the choices. Check Mouse and
Touch, then Reset; earlier spacing/container and fill-field settings are retained.

Enable **Overflow examples** in the container group, open QAT overflow and scroll
to its last four entries: Export, Paste, Font family and the ordinary-menu group
reference. These demonstrate menu/split/ComboBox/group indicators using the same
arrow roles. Turning the toggle off removes all temporary commands/references
while retaining authored QAT identities/order. The public package check verifies
the demo apply/reset and the separate submenu/chooser/split paint slots without
showing a Form or using internal layout APIs. Runtime split hit/keyboard/UIA and
inert preview hit rectangles are covered by the STA tests.

Arrow alignment correction: ordinary dropdown, split and QAT/More chooser
indicators now share the split arrow's trailing column, even with different
submenu/split/chooser reservation values. Caption ends and trailing clearance
are aligned too; larger submenu/chooser reservations can still add extra text
space without moving the glyph. Split hit areas and actions are retained.
Reopen the refreshed sample and compare Export/Paste/Font family/group arrows in
**Try arrow menu** and the last four **Overflow examples** entries, in Mouse/Touch,
at defaults and with **Wider arrow areas**. No extra setting is needed.

The fifth group, **Native editor spacing**, has **Roomier editors**, **Reset
editors** and **Try editor menu**. Compare taller fields, label gaps, field text
insets, wider ComboBox buttons and choice-row spacing in Mouse/Touch. The menu contains a
TextBox, labelled editable ComboBox and unlabelled selection-only ComboBox. Open
both choice lists, retain a typed value, then reset. Combine with previous menu
spacing and **Fill menu fields**, and compare the authored Formatting fields.
The preset requests a 54-pixel field minimum and 10-pixel top/bottom text padding.
TextBox/ComboBox fields and their containing rows grow together; text/labels stay
vertically centered with the same font size. Existing Touch widths remain.
Reset affects only the seven editor theme roles. No InitializeComponent or authored
Form/resource changes are needed.

The same `--menu-geometry-demo` launch also includes **Illustrated
menus**; the earlier **Menu geometry** tab remains available. Use **Roomier
illustrated menu**, **Reset illustrated menu** and **Try illustrated menu** to
compare artwork size, row height, title/description gaps and desired width in
Mouse/Touch. **Nested choices** opens a separate illustrated example. **Menu on
QAT** adds a temporary reference to the direct menu's same source; turn it off
to remove only that reference. Selected choices keep their state through theme
reset. Earlier menu/editor overrides and the authored startup File mode remain.

The same opt-in launch also includes **Command splits**. Compare **Roomier
command splits** and **Reset command splits**, using the existing Mouse/Touch
selector and the priority inline **Simplified layout** toggle. Small/Medium/Large
horizontal examples use the same width roles. Primary clicks toggle once; arrows
open their menus independently. **Vertical paste** is the unchanged Large vertical
comparison in Classic; Simplified presents it horizontally at Medium. **Split on
QAT** adds/removes only a temporary reference, retaining QAT's own geometry.
Reset clears only the three split-width roles and preserves mode, state, earlier
menu/editor geometry and authored File presentation.

`--verify-command-split-geometry <evidence-folder>` checks automatic layout and
painting of the actual public RibbonControl through DrawToBitmap, without showing
windows or calling internal layout APIs. It verifies both renderers/densities and
Classic/Simplified, full arrow slots, primary/caption budgets, tiled regions, exact
default/reset pixels and public demo mode/state/QAT contracts. Its log records the
control's reported native DPI. Separate library STA tests cover synthetic DPI,
native hit/keyboard/UIA/action and editor-retention scopes; actual VS/input/hardware
acceptance remains separate.

`--verify-illustrated-geometry <evidence-folder>` checks the public package's
seven illustrated roles/reset/validation and artwork/title/description paint in
both renderers/densities at synthetic 96/120/144/192 DPI. It verifies the permanent
demo's apply/reset, toggle state and retained File/QAT/source identities without
showing windows, opening popups or using internals/Showcase helpers. Library STA
checks separately cover measured menus, nested/QAT openers, preview, scroll/hit/
UIA bounds and retained native mixed editors. Actual VS/input/hardware acceptance
remains user-owned.

`--verify-editor-geometry <evidence-folder>` checks the refreshed public package's
new API/reset/validation, both renderers/densities and synthetic 96/120/144/192-DPI
label/button paint. It also checks public Classic/Simplified ribbon height growth
and reset in both densities, the permanent demo's seven-role apply/reset actions and
retained File/QAT/fill behavior without showing windows, creating native editors,
opening popups or using internals. Separate library STA tests cover native layout,
text clipping, choice heights, UIA bounds and HWND retention. Actual VS/native
input/hardware acceptance remains user-owned.

`--verify-menu-container-geometry <evidence-folder>` verifies public package
properties, validation/reset and heading/scroll paint with both renderers and
Compact/Touch. It also invokes the permanent
demo's apply/reset/toggle actions without showing a Form. It uses no Showcase
helpers, global input or computer use. Runtime popup measurement/reveal and inert
designer-preview/composited-paint coverage are separate STA tests; actual VS/live review remains
user-owned.

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
