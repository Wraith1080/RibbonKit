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
