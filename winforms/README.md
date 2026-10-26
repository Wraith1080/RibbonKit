# RibbonKit.WinForms foundation

A separate native Windows Forms implementation, developed in `winforms/` on the
user's `codex/Ribbonkit-Winform` branch. WPF v1.1.0 is the behavioral reference.
This is an unpublished foundation preview; [M1 acceptance](docs/M1-STATUS.md)
is still open.

Requires Windows and the .NET 10 SDK selected by `global.json`. Open
`RibbonKit.WinForms.sln` for native development. From the parent checkout:

```powershell
dotnet run --project winforms/samples/RibbonKit.WinForms.Showcase
```

Resize the ordinary Form, open a collapsed group, or use Alt/F10 and F6. The
sample's editor, icons, handlers and BindingSource belong to the application.

The foundation includes tabs, groups, buttons, two-state toggles and dropdowns;
three group reduction modes, fixed/adaptive command sizes, explicit priorities,
tab/body scrolling, collapsed flyouts, keyboard focus/KeyTips and a native UIA
provider. Neighbouring medium rows share column widths, icon slots and text
origins; large labels share a first-line origin. Layout and rendering consume
the same device-pixel rectangles, with font-aware row/caption metrics.

Office2024 light, Compact density, LTR and immediate transitions are the initial
scope. Mica client transparency, animation, custom window chrome, QAT,
customization, native ribbon editors, galleries and WPF template/resource
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

The provisional API is documented in XML and tracked in
`src/RibbonKit.WinForms/PublicAPI.Unshipped.txt`. Nothing has been promoted to a
shipped compatibility baseline. Public COM interfaces in the Accessibility
namespace exist for the native UIA bridge and are hidden from IntelliSense;
applications should use Windows UIA clients rather than implement those types.

## Verification and packaging

Run these commands from `winforms/`. Native keyboard/mouse checks briefly operate
their own test Form; let that process retain focus until it finishes.

```powershell
./eng/Test-Native.ps1
./eng/Test-Native.ps1 -Configuration Release -Package
./eng/Measure-Baseline.ps1
```

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
