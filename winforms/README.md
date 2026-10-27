# RibbonKit.WinForms native preview

A separate native Windows Forms implementation, developed in `winforms/` on the
user's `codex/Ribbonkit-Winform` branch. WPF v1.1.0 is the behavioral reference.
This is an unpublished preview. The [M1 foundation ledger](docs/M1-STATUS.md)
records its remaining review; the current [M2 slices](docs/M2-STATUS.md) add
split buttons and native TextBox/ComboBox editing. Accessibility compatibility is deferred
while the control structure is implemented; it remains possible future work.

Requires Windows and the .NET 10 SDK selected by `global.json`. Open
`RibbonKit.WinForms.sln` for native development. From the parent checkout:

```powershell
dotnet run --project winforms/samples/RibbonKit.WinForms.Showcase
```

Resize the ordinary Form, open a collapsed group, or use Alt/F10 and F6. The
sample's document editor, icons, handlers and BindingSource belong to the application.
The Inputs tab demonstrates library-owned native ribbon editors.

The foundation includes tabs, groups, buttons, two-state toggles and dropdowns;
three group reduction modes, fixed/adaptive command sizes, explicit priorities,
tab/body scrolling, collapsed flyouts, keyboard focus/KeyTips and a native UIA
provider. Neighbouring medium rows share column widths, icon slots and text
origins; large labels share a first-line origin. Layout and rendering consume
the same device-pixel rectangles, with font-aware row/caption metrics.
Large regular buttons, toggles, dropdowns and both split arrangements share a
compact two-line caption layout. Native text retains its font size and first-line
origin; `DrawText` receives each measured line separately. Captions use tighter
baseline spacing and three logical pixels of bottom clearance. Large buttons grow
up to `RibbonTheme.LargeButtonMaxWidth` (108 logical pixels by default), wrap to at
most two rows and end the last row with an ellipsis when needed. The original
`Text` remains intact and supplies the default ScreenTip title. Caption length does not increase ribbon
body height. Wrapped dropdowns and vertical splits centre the second row and its
arrow together; a single-row vertical split keeps its arrow below the caption.
Horizontal splits retain their separate arrow column. Medium/Small and menu rows
retain their existing single-line layout.

The width limit includes the entire Large button, including any separate arrow
column, and scales with DPI. Values below the existing 60-logical-pixel minimum
are rejected when assigning `Theme`. For example:

```csharp
ribbon.Theme = ribbon.Theme with { LargeButtonMaxWidth = 120 };
```

`RibbonSplitButtonItem` adds a primary `Click`/`PerformClick()` action and a separate
owned dropdown. Large supports both `Layout=Horizontal` (the default) and
`Layout=Vertical`, which places the icon above its caption/menu;
Medium/Small and menu rows use horizontal halves. `KeyTip` and `DropDownKeyTip`
address the actions separately. `CheckOnClick=true` makes primary activation toggle
its bindable `Checked` value, raising `CheckedChanged` before `Click`. Assigning
`Checked` raises no `Click`; opening the menu never toggles the primary half.
Showcase's Paste demonstrates an ordinary Large vertical split; Track changes
demonstrates a Large horizontal toggle split. A standalone two-state button uses `RibbonToggleItem`;
`RibbonButtonItem` remains an ordinary command without `Checked`.
`RibbonDropDownItem.IsDropDownOpen` exposes the current popup state. Dropdowns and
split arrows retain their pressed appearance while open, independently of hover
and the primary half's checked state.
Custom renderers override `DrawSplitItem` to receive both measured action contexts.
The retained foundation bridge does not provide complete accessibility semantics
for this new item; no split accessibility compatibility is claimed.

Office2024 light, Compact density, LTR and immediate transitions are the initial
scope. Mica client transparency, animation, custom window chrome, QAT,
customization, galleries and WPF template/resource
compatibility are later work. Ordinary application controls remain native Forms
controls. The runtime references neither WPF nor the designer SDK.

## Application contract

Set `HighDpiMode.PerMonitorV2` before creating windows, and enable visual styles.
Add a `RibbonControl` to a normal Form; it docks to the top. Compose its get-only
`Tabs`, `Groups` and `Items` collections using components with stable explicit
IDs. New unspecified IDs are unique, but applications persisting identities
should author them explicitly. One ribbon per Form is the initial input scope.

- Collections own attached models. Removal detaches without disposing; disposing
  a model removes it, and disposing the ribbon disposes attached descendants.
- Images, vector icons and custom renderers are borrowed. Applications dispose
  those resources after their controls. The default renderer owns its caches.
- Change attached state on the owning UI thread. Use `BeginUpdate`/`EndUpdate`
  for batches; `ISupportInitialize` maps to the same contract.
- `PerformClick`, mouse, keyboard and UIA share activation. Toggles update Checked,
  then raise CheckedChanged, then Click. Assigning Checked does not raise Click.
- `DataBindings` uses ordinary WinForms Binding/BindingSource. No implicit
  ItemsSource/MVVM adapter or WPF command contract is included.
- Override the public `RibbonRenderer` for painting, or supply borrowed
  `IRibbonIcon` providers. Geometry, hit testing and accessibility remain library
  owned. The mirrored-corner painter avoids independent per-corner arc paths;
  current pixel checks do not substitute for native visual review.

## Compact check and radio options

`RibbonCheckBoxItem` and `RibbonRadioButtonItem` are compact option rows with
bindable `Checked` and `CheckedChanged`, followed by the normal activating `Click`.
Mouse, Space/Enter, KeyTips and `PerformClick` use that same path; programmatic
state assignment raises no Click. Inherited size/image settings do not replace
the compact indicator and label.

Checkboxes also expose bindable WinForms `CheckState` and `CheckStateChanged`.
With `ThreeState = true`, activation cycles Unchecked → Checked → Indeterminate.
`Checked` is true for both Checked and Indeterminate. For an enum-valued source,
use `formattingEnabled: false` in its CheckState Binding: formatted WinForms
binding maps Indeterminate to a null source value. Bind `Checked` for boolean data.

Radio items select themselves and clear their peers. An empty `GroupName` groups
siblings in the same logical item collection; a nonempty name spans that ribbon,
including menus and inactive tabs. Detached named groups share their logical
tree. Repeated activation retains selection. Programmatic clearing permits no
selection; removal does not select a replacement. Attaching a selected item or
changing its group normalizes selection in the new scope. Hidden and disabled
peers are cleared too, while their own input activation remains disabled.

`DrawCheckBoxIndicator` and `DrawRadioButtonIndicator` receive measured indicator
slots through the shared renderer. Office uses softly rounded checkboxes; neutral
uses square ones. Both use circular radio indicators. The Showcase's Options tab
demonstrates checkbox binding, mixed state, radio choices, menus and collapse.
Accessibility compatibility remains deferred.

## Native TextBox items

`RibbonTextBoxItem` uses `Text` as its label and bindable `Value` as its live text.
`InputWidth` is the editor width in logical pixels (minimum 24, default 120),
excluding the label. It always uses one compact row; inherited command size/icon
settings do not change its presentation. Stacked fields in a column share a
label span. `ReadOnly` keeps native selection/copy; `MaxLength` limits user input,
with zero meaning unlimited. Programmatic values are not truncated.

Bind `Value` through the item's ordinary `DataBindings`. `OnPropertyChanged`
writes live edits; `OnValidation` waits for a successful commit. Handle
`ValueValidating` and set `Cancel` to reject a commit. Successful validation writes
deferred bindings, raises `ValueValidated`, then `ValueCommitted` for changed
values. The distinct event name prevents Component bindings from writing before
the application's validation handlers finish.

Enter commits and keeps editing focus. Tab/Shift+Tab validate before navigation;
F6 returns to the document. Escape restores the focus-entry/last-committed value,
raises `EditCanceled` when it changes, and returns to the ribbon or closes one
popup level. With live binding this also writes the restored value back. External
value changes become the new rollback baseline. Forced hiding, disabling or popup
closure commits valid edits and rolls back rejected ones. Removal disposes only
the native presentation; the detached model retains its value and bindings.

`PerformClick()` or a KeyTip focuses the active editor without replacing its
selection. The library owns one real TextBox per item and transfers it into/out
of collapsed-group popups; applications must not reparent or dispose that child.
Ordinary typing, caret movement, selection and clipboard commands remain native.
The renderer's `DrawTextBox` callback paints the label/frame around the native
text area. Native editor appearance is opaque; accessibility compatibility and
actual VS authoring remain unverified.

## Native ComboBox items

`RibbonComboBoxItem` uses the same label, `InputWidth`, live `Value`, validation,
commit/cancel and binding conventions as TextBox. Its owned `Items` collection
contains strings; `SelectedIndex` is bindable, with -1 meaning no selected choice.
Selection-only mode is the default. `IsEditable=true` allows free text, preserving
its exact case and using index -1 when it does not match an authored choice.
An unmatched Value in selection-only mode clears the selection and text. Choice
mutations preserve a surviving selection, update its index and close a stale list.
This slice has no object DataSource/DisplayMember/ValueMember adapter.

`PerformClick()` and KeyTips focus the editor. `ShowDropDown()`, F4 or Alt+Down
opens a library-owned choice popup containing a native ListBox; `CloseDropDown()`
cancels its pending selection. The Windows ComboLBox stays closed.
Mouse selection and closed-list keyboard selection validate and commit once.
Enter accepts a list selection; Escape cancels only that list first, then the next
Escape restores the editor's committed value and exits one ribbon popup level.
Tab validates before navigation. Live binding sees preview and rollback values;
deferred binding writes only after successful validation.

Mixed TextBox/ComboBox rows share label spans. The library transfers one native
ComboBox per item into/out of popups, retaining editable selection. `DrawComboBox`
paints the label, border and arrow: rounded in Office 2024 and square in the flat
renderer, with hover, pressed and open states. Native text input is clipped inside
that frame. `DrawComboBoxItem` paints each native list row; rows are at least 28
logical pixels tall and grow with the font. The closed field stays compact and
aligned with TextBox. Native text/caret/selection are retained.
Layout, model, tab and visibility changes synchronize native editors. Hover and
pressed-state painting leave their bounds and text selection alone.

The ListBox supplies scrollbars, wheel scrolling and incremental search; the
library coordinates preview, validation, keyboard navigation and dismissal.
A stationary pointer cannot replace
typed text or keyboard selection during repaint. The popup fits the screen work
area, opens above the field when needed, and shows up to eight rows before scrolling.
Outside presses and deactivation cancel unaccepted preview. Escape closes the choice
popup before its containing ribbon popup. Actual VS authoring and accessibility compatibility
remain unverified. Inputs shows
selection-only Font, editable Zoom, a disabled chooser and a popup chooser using
ordinary application-owned data and validation.

## Passive ScreenTips

Every `RibbonItem` supports localizable, designer-editable `ScreenTipTitle` and
`ScreenTipDescription` strings. An empty title uses the complete `Text` caption,
including captions shortened with an ellipsis on screen. An empty description
gives a title-only tip; an item with no title/caption or description has no tip.
These properties are separate from command state and accessible descriptions.

```csharp
var find = new RibbonButtonItem {
    Text = "Find matching text throughout this document",
    ScreenTipDescription = "Search the current document."
};
var unavailable = new RibbonButtonItem {
    Text = "Export", Enabled = false,
    ScreenTipDescription = "Open a document before exporting."
};
```

Pause over an item for approximately 600 ms to show its tip. Buttons, toggles,
dropdowns, both split-button halves, check/radio options and native TextBox/
ComboBox item slots share this behavior on the ribbon, command menus and
collapsed-group popups. Disabled items may describe themselves without gaining
hover/press visuals or command execution. Split halves use one item-level tip.
The tip takes no focus or capture and preserves native editor selection.

Leaving the item, keyboard/press/wheel input, command or menu activation,
model changes/removal, layout/DPI changes, owner movement/deactivation, hiding
and disposal cancel pending/visible tips. After keyboard/press cancellation,
real pointer movement is required before another hover starts. Placement fits
the monitor working area and flips above the item near its bottom edge. Tips
remain above library menus without activating them. Very large descriptions are
clipped to the available working area; this first version has no scrolling.

`RibbonRenderer.DrawScreenTip` receives the full title/description and measured
content rectangles. It draws a bold title and optional wrapped description using
the ribbon font and theme foreground. Existing `DrawPopupFrame`,
`GetPopupCornerRadius` and `DrawPopupShadow` hooks supply the shared frame:
rounded Office or square neutral. Another Form can use these public APIs without
Showcase resources. Images, help links, pinned/interactive tips, keyboard-triggered
help and per-choice ComboBox row tips are later work. While a choice list is open,
item tips are suppressed. Accessibility compatibility remains deferred.

The Showcase starts on Home: Find demonstrates full-caption fallback, Copy/Paste
authored descriptions and Unavailable a disabled explanation. Inputs and its
menu demonstrate native editors; Options supplies check/radio examples.

## Renderer customization

`RibbonRenderer` is the flat foundation. It shares measured content, image
scaling/clipping, dropdown arrows and interaction states without the Office
underline, group separators, rounded chrome or Office focus/badge treatment.
`RibbonOffice2024Renderer : RibbonRenderer` adds that presentation and remains
the default when `RibbonControl.Renderer` is null. Both consume the selected
`RibbonTheme`; choosing a renderer does not change layout or command ownership.

The default ribbon background is white. The shared `BodyMargin` defaults to
7,1,7,7 logical pixels. Layout reserves these
margins for the panel, scrolling and hit targets. Office paints that panel as a
rounded card with a border and a soft `BodyShadowColor` shadow; the neutral
renderer retains its flat surface. The default `BodyHeight` minimum is 110 logical
pixels; font-dependent measurement reserves three command rows, a group caption,
six logical pixels above commands and eight below them. Compared with the earlier
default, the body extends six logical pixels downward, with its top edge and tabs
anchored. Command/icon/editor sizes stay the same; the added height supplies
padding and caption clearance. Explicit taller `BodyHeight` settings retain their
minimum-height meaning. Office tab headers use text and blue underlines
for selection/hover/press/keyboard focus, with transparent backgrounds. The
underline follows the measured label width plus four logical pixels per side,
is three logical pixels thick, and has rounded ends. Scroll
controls use themed hover/pressed chrome. The body panel bounds supplied to
`DrawBody` are separate from the inner content viewport.
Office group dividers follow the WPF reference's inset proportions: a crisp
one-device-pixel `Border` hairline with 12 logical pixels of clearance at both
ends, scaled with DPI. The neutral renderer retains its separator-free surface.
Body scrollers use `PopupBorder` at rest and an accent border on hover/press.
Their top and bottom clearance is controlled by
`RibbonTheme.BodyScrollButtonVerticalInset`: two logical pixels by default,
matching the side clearance. Themes can use zero for full-height scrollers or
another nonnegative value; layout caps it at half the body height. For example,
`ribbon.Theme = ribbon.Theme with { BodyScrollButtonVerticalInset = 0 };`.
Collapsed group openers use the same inset rectangle for painting, focus, hit
testing and popup anchoring, with six logical pixels above and eight below.

Floating ribbon menus, collapsed groups and ComboBox choice popups share a
one-device-pixel frame.
Office 2024 uses `PopupCornerRadius` (six logical pixels by default); the neutral
renderer keeps square corners. `GetPopupCornerRadius` supplies the shared
device-pixel radius for frame painting, content clipping and shadow. Override it
with the renderer's drawing hooks to customize the complete popup shape.
`DrawPopupFrame` paints all four sides using `PopupBorder`. `PopupShadowEnabled`
enables the library's soft shadow, subject to Windows' shadow setting.
`DrawPopupShadow` uses `PopupShadowColor` (the same default opacity as the body),
six logical pixels of blur and a two-pixel logical downward offset. A separate
nonactivating, click-through layered window composites the shadow and partially
covered frame-edge pixels through
[UpdateLayeredWindow](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-updatelayeredwindow);
the popup and its native editors stay opaque and use matching mirrored clipping
regions. The smooth frame edge remains when shadows are disabled. Cut-out corners
pass pointer input through and count as outside presses for popup dismissal.
The library owns and destroys the frame/shadow window with its popup. The darker
native ToolStrip and ComboLBox shadows are disabled or kept hidden. Choice popups
use the same rounded Office frame, square neutral frame and soft shadow hooks.

Derive from `RibbonRenderer` for a flat/custom presentation, or from
`RibbonOffice2024Renderer` to retain Office styling while overriding selected
drawing. Earlier preview subclasses that called base painting to obtain Office
styling should now derive from the named Office renderer. Instances assigned to
`Renderer` remain borrowed and must outlive their ribbon/popups.

The shared hooks are `DrawSurface`, `DrawBody`, `DrawPopupFrame`, `DrawPopupShadow`, `DrawScrollButton`, `DrawText`,
`DrawDropDownArrow`, `DrawTextBox`, `DrawComboBox`, `DrawComboBoxItem`, `DrawScreenTip`, and protected
`DrawItemImage`/`DrawChrome`, in addition to
whole tab/group/item/split/focus/KeyTip methods. Content callbacks receive the
library's measured slots. Respect those rectangles, font and clipping; painting
does not redefine hit targets or desired size. The existing protected
`DrawRounded` helper remains available for custom chrome. These are independent
RibbonKit renderer types rather than subclasses of `ToolStripRenderer`.

Dropdown indicators use an 8 x 6 logical-pixel vector shape rather than a text
glyph. They scale with DPI and have reserved label/icon space in dropdowns,
split buttons and collapsed groups.

The provisional API is documented in XML and tracked in
`src/RibbonKit.WinForms/PublicAPI.Unshipped.txt`. Nothing has been promoted to a
shipped compatibility baseline. Public COM interfaces in the Accessibility
namespace exist for the native UIA bridge and are hidden from IntelliSense;
applications should use Windows UIA clients rather than implement those types.

## Verification and packaging

Run these commands from `winforms/`. Native keyboard/mouse checks briefly operate
their own test Form; let that process retain focus until it finishes.

The unit/STA suite contains 66 essential cases: the [test review](docs/M2-STATUS.md#essential-test-review--2026-10-26)
reduced the earlier set to 56; M2C adds three ComboBox cases and M2D adds three
check/radio state, grouping/binding and compact-rendering cases. M2E adds three
ScreenTip metadata, cancellation/lifetime and measurement/rendering cases.
One body-spacing case checks downward growth, retained content/editor sizes and
collapsed-group geometry across representative fonts and synthetic DPI scales.
The existing editor alignment case also covers mixed rows. For routine changes, run the affected
test classes. Reserve the full native/package sequence for changes that need those
gates. Add cases for distinct failure modes and use representative DPI/font rows;
the four-corner symmetry regression deliberately keeps all four scales.

```powershell
./eng/Test-Native.ps1
./eng/Test-Native.ps1 -Configuration Release -Package
./eng/Measure-Baseline.ps1
```

For the bounded shown ScreenTip scope, run the native test host with
`--screentip-checks TestResults/visual/screentips-office` or
`--screentip-checks-flat TestResults/visual/screentips-flat`. The independent
consumer accepts `--validate-screentips` / `--validate-screentips-flat` followed
by an evidence directory. Both paths check delayed cancellation, native editor
selection, disabled commands, popup stacking and nested Escape; captures include
the popup and overlaid tip. They do not imply full native/UIA acceptance.

The package gate restores from an isolated local feed/cache into a fresh temporary
directory outside the checkout, builds the independent consumer and drives it
through the external UIA/input client. Its folder is printed and retained.
The consumer has no Showcase reference, copied helper or internal-access grant.

Build the solution before `dotnet pack src/RibbonKit.WinForms -c Release --no-build`.
The runtime package includes XML docs, license/notices and the design assembly
under `lib/net10.0-windows7.0/Design/WinForms/Server` (NuGet's canonical moniker
for the target alias); the designer SDK is not a runtime
dependency. This follows the [Microsoft package convention](https://github.com/microsoft/winforms-designer-extensibility/blob/main/docs/sdk/control-library-nuget-package-spec.md).
Collection editors compile against Microsoft.WinForms.Designer.SDK 1.6.0.
Actual Visual Studio load, collection editing, undo/redo and C#/resx round-trip
remain unverified; see the exact review checklist in the acceptance ledger.

Raw measurements and captures go under ignored `TestResults/`. Builds use the
normal Debug/Release `bin` folders. The measurement tool records native paint
completion and resource checkpoints, not compositor frame rate or a leak proof.

See [the original plan](../docs/21-NATIVE-WINFORMS-PLAN.md),
[source provenance](docs/reference/WPF-1.1.0.md) and [license notices](THIRD-PARTY-NOTICES.md).
