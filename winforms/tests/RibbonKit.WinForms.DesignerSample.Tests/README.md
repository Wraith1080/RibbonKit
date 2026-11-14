# Designer Sample verification host

This executable holds checks, profiles and fixtures formerly compiled into the
editable Designer Sample. It is a command-driven native test host, not an xUnit
project or another interactive review solution. Normal sample startup/build/refresh
does not compile or run it.

The host links the sample's real authored Form/UserControl sources and resources,
plus the shared sample appearance adapter. Test-only partial members can inspect
the authored graph without making private controls public or adding runtime hooks.
It uses the same local **public RibbonKit package** selected by
`samples/RibbonKit.WinForms.DesignerSample/.local/DesignerPackage.props`; there is
no library ProjectReference, friend access, WPF dependency or Showcase helper.
Authored source/resource files are not copied or rewritten by a check/build.

- `Checks/`: native/direct-print, serialization, appearance, lifecycle and authored-
  model checks, including the extracted DesignerForm verification partials.
- `Profiles/`: opt-in timing and activation trace tools.
- `Fixtures/`: numeric geometry settings and generated navigation forms/resources.
- `Program.cs`: explicit verification dispatch. No arguments/unknown modes fail
  with exit code 2 instead of launching the sample as a fallback.

The project is invoked explicitly rather than added to the ordinary native
solution build: it requires the persistent sample's already-selected local package,
just like the other package-consumer fixtures.

## Run focused checks

From the repository root, refresh the persistent sample's package once if its
`.local` selection is missing. Then use:

```powershell
& winforms/eng/Test-DesignerSample.ps1 -List
& winforms/eng/Test-DesignerSample.ps1 -Check --verify-sample-isolation
& winforms/eng/Test-DesignerSample.ps1 -Check --verify-clean-sample
& winforms/eng/Test-DesignerSample.ps1 -Check --verify-theme-gallery
& winforms/eng/Test-DesignerSample.ps1 -Check --verify-office2013-composed
```

The runner builds the ordinary sample and host in normal Debug folders, launches
the selected check and reports its failure exit code. `-Configuration Release`
uses normal Release folders. `-NoBuild` requires an up-to-date matching build.
`-Output <path>` chooses the evidence directory; otherwise each run gets a fresh
timestamped directory under `winforms/artifacts/designer-sample-checks/`.
`-TimeoutSeconds` defaults to 120 and stops only that check process on timeout.
`-CheckArguments @('value', 'another value')` forwards optional check-specific
arguments after the output path, including values containing spaces.

For example, verify the retained selection-only startup alias without adding tabs:

```powershell
& winforms/eng/Test-DesignerSample.ps1 -Check --verify-clean-sample -CheckArguments --office2019-light
```

`--generate-*` and `--trace-choice-focus` take a **file path** in `-Output`, rather
than an evidence directory. Generated source belongs under this project's
`Fixtures/`, followed by a normal host build before its matching `--verify-*`
command. Never generate files back into the editable sample. Profilers and native
input/capture modes may require a visible desktop/foreground owner; a hidden or
interrupted run does not establish their live acceptance.

## Retired demos and migration boundary — 2026-11-14

The cleanup started from `1802dad` (`office 2010 planning`) with a clean worktree.
It moved 69 check/profile/fixture source files plus verification dispatch out of
the sample, extracted its verification-only partial members, and removed **31
retired rewrite/consolidation review-tab builders**. `GalleryPresenterDemo` was a
verification class, so it was retained as `GalleryPresenterCheck` under tests.
Four shared numeric geometry-setting classes survive as test fixtures; their
interactive tabs and buttons do not.

The 31 UI-only demo smoke calls and their stale success messages were removed.
The actual control regression matrices remain. The gallery popup-button lifetime
check now authors a small fixture group on the existing Gallery tab inside the
test process, instead of rebuilding the retired Gallery geometry demo.

Three commands existed solely to test retired UI and were removed:

- `--verify-office2019-review-demo`
- `--verify-menu-geometry-demo`
- `--verify-editor-appearance-demo`

All **86 other existing verification/profile/generator/trace commands** remain;
the new `--verify-sample-isolation` brings the host list to **87**. Ordinary sample
verification/profile/trace/generator/capture switches now fail with exit code 2.
Normal `--office2019-light` selection and its legacy selection-only aliases remain.

The isolation check reads the actual compiled sample assembly metadata, confirming
its authored controls/resources remain and no check/profile/demo/generated-fixture
types, verification methods or test-host references are present. At this checkpoint
the Debug sample assembly has 24 types, zero verification methods and is 84,480
bytes. This is a current footprint observation, not a before/after startup benchmark.
The retired builders were already inactive during normal launch; no latency gain
is claimed from their deletion alone.

Seventeen protected authored files were checked byte-for-byte: designer partials,
theme/input-mode code, UserControls and resources are unchanged. DesignerForm.cs
retains its constructor and runtime handlers; only verification members moved.
Program now contains normal startup and a diagnostic-command rejection guard.
`SampleStartup` shares the normal theme-selection behavior with the host's checks.
No RibbonKit runtime, public API, renderer or native window policy changed.

## Verification evidence and limits

Evidence is retained under `winforms/artifacts/designer-sample-cleanup/`:

- Normal sample and host **Debug and Release builds pass**, zero warnings/errors.
- `isolation/` and `isolation-release/`: actual sample assembly boundary checks pass.
- `clean-default/`, `clean-2019/`, `clean-legacy/`, `clean-release/`: authored
  Home/Edit/Gallery/View graph and startup selections pass without extra tabs.
- `theme-gallery/`: moved authored theme/accent/title/Reset checks pass in both
  layouts; `composed/` passes the authored Office 2013 native surface handoffs.
- `command-appearance/` and `chrome-geometry/`: representative retained rewrite
  and consolidation checks pass through the new package-only host.
- `gallery-backstage-final-pass/`: built-in/native gallery and Backstage geometry
  checks pass for both renderers/densities at actual 120 DPI. A run that did not
  acquire native focus reports that limit; it does not claim physical focus acceptance.
- `content-codegen-final/`: compiled generated Form/RibbonForm Backstage ownership,
  initialization and resource/type references pass in the relocated fixture.

The moved gallery geometry fixture had obsolete assumptions. It now inspects the
visible ToolStrip surface on its UI thread, captures it before queued external
deactivation, measures the public native page rectangle instead of a private parent
depth, and compares an authored inset with explicit zero rather than a nullable
family default. It preserves the observed initial focus state and reports a missing
focus prerequisite. Initial failures remain in the evidence directory.

This cleanup did not run all 87 modes, foreground gallery lifetime/profiling,
physical input/IME, actual Visual Studio designer save/reopen, desktop material
pixels or mixed-monitor checks. Some historical modes assert the old editable
sample's exact authored values: `--verify-roundtrip`, for example, expects the
Bold SVG removed during earlier user review. Its existing limitation remains;
relocation is not a claim that every historical fixture currently passes.
No unrelated full runtime suite, WPF build/test, commit or push ran.

Office 2010 remains at its [F1 contract](../../docs/OFFICE2010-BLUE-PLAN.md).
The next theme implementation is still F2 shared gradient/state-chrome foundations.
