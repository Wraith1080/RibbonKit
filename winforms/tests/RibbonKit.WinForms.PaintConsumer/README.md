# Shared paint foundation consumer

This app uses only the native library's public APIs and framework drawing/controls.
It has no Showcase resources, helpers or friend-assembly access. Default images
cover four existing families, two painters and four DPI values. G2 checks exercise
exact repeated-offset transitions and measured panel hooks in eight offscreen
native Classic/Simplified, Compact/Touch, wide/reduced scopes. HWNDs, parentage,
values, selection, bounds and visibility remain native.

From `winforms/`, compare against a retained pre-G2 runtime DLL:

```powershell
./eng/Test-PaintFoundation.ps1 -BaselineAssembly '<old-library.dll>' -Configuration Debug
```

The script builds both references sequentially in this project's normal output
folder, starts the offscreen app hidden, preserves evidence and requires exactly
32 byte-identical default PNG pairs. `PaintBaselineAssembly` changes only the
baseline assembly reference; it does not change branches or old sources. Current
builds reference the runtime project. This does not refresh the Designer Sample.

For a new native assertion after unchanged default-paint verification, build the
current project normally and run the executable with `<evidence-directory>
features-only` to select only G2 checks. No automated image approval, desktop
capture, physical input, actual VS designer or human acceptance is implied.

The application-button scope uses the same consumer and baseline reference:

```powershell
./eng/Test-PaintFoundation.ps1 -BaselineAssembly '<old-library.dll>' -ApplicationButton -Configuration Debug
```

It compares 32 rectangular File images (four existing families, both renderers,
four DPI values and five states), then exercises the G3 Orb through public APIs.
Twelve native layout scopes cover both layouts/densities and all QAT positions.
F10/F KeyTip input, pane/native-content retention and override/Reset are checked.
A hidden second process uses Windows UIAutomationCore COM directly to find one
File element, retain its runtime ID and exercise ExpandCollapse. No WPF UIA
assemblies or sample helpers are used. Native layout scopes run offscreen at the
machine's actual DPI; they do not establish physical input or visual acceptance.

After unchanged default-image verification, `orb-features-only` reruns just the
current Orb/native/UIA assertions into a fresh evidence directory.
