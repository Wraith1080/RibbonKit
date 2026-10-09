# Contributing to RibbonKit

Thanks for your interest! RibbonKit v1.0.0 is published through GitHub Releases. The v1 public API is
frozen, so the most valuable contributions are focused fixes, documentation, showcase feedback, and
discussion of additive post-v1 features.

## Ground rules

- One feature or fix per pull request, with a matching showcase page or unit test where it makes sense.
- All controls are **lookless custom controls** — templates live in theme dictionaries, never hardcoded visuals in code-behind.
- Public API needs XML doc comments.
- Match nearby code and the analyzer settings in `Directory.Build.props` and project files; nullable reference types are enabled.

## Public API compatibility

`src/RibbonKit/PublicAPI.Shipped.txt` is the frozen v1 baseline. The
`Microsoft.CodeAnalysis.PublicApiAnalyzers` checks run for all runtime target frameworks and fail the
build when a public symbol is added, removed, or changes nullability without an explicit baseline
update.

- Compatible, intentional additions go in `src/RibbonKit/PublicAPI.Unshipped.txt` and require API-review justification in the pull request.
- Do not edit `PublicAPI.Shipped.txt` to hide a breaking change. Removals and signature/nullability changes require a versioning decision.
- Public members require XML documentation; missing or broken `cref` references fail the runtime build.

## Workflow

For external contributions, coordinate significant work in an issue and create a branch
from `main` in your fork. Local agent tasks can proceed from the user's authorized scope
and current checkout; an issue, fork, or pull request is not a prerequisite. Preserve
unrelated edits and publish only when requested.

Keep each change focused, choose the validation below, then review the diff. A pull
request should explain the resulting behavior, why it changed, and which checks ran.

### Proportional validation

Run commands from the repository root in Windows PowerShell. Project files specify
the required frameworks. Use the current local environment before concluding WPF
verification is unavailable.

| Change | Required local verification |
| --- | --- |
| Documentation or agent instructions only | Review content, links, and `git diff --check`; validate a changed skill's metadata. No WPF build/test for prose alone. |
| Focused code fix in one component | Build the affected project and run relevant existing or added regression tests. Check the actual UI when the result depends on rendering or interaction. |
| Bounded template, token, or appearance adjustment | Build the affected projects; run relevant rendering/geometry regressions and selected visual scenes for the affected palettes, states, DPI and RTL cases. Include a focused RibbonKit-only consumer check when reusable behavior is affected. A shared XAML file alone does not require the full solution suite. |
| Broad shared behavior or template-contract changes, public APIs, cross-project refactors, build configuration, or release readiness | Build and test `RibbonKit.sln` in Release; include affected live UI gates. For packaging/designer distribution, also pack and validate the package. |

For documentation-only acceptance, use the [quick documentation workflow](AGENTS.md#quick-documentation-and-acceptance-updates).
Update one existing acceptance row/item in the active plan; keep its date, review
scope and remaining limits precise. Review that edit and any changed link target,
then run `git diff --check` once. Existing links do not need a fresh repository-wide
link audit. Do not duplicate the confirmation into history, the design index or
README; those change only when they contain independently affected information.
Previously read instructions and recorded test evidence need not be reloaded.

Explicit task acceptance gates take precedence. Broaden a focused run when dependency
impact, a failure, or unresolved risk warrants it. Do not repeat successful checks
without a relevant new change. `--no-build` requires a successful matching build of
the test project/solution with the same configuration and framework.

Choose checks by the behavior changed. A Writer-only change normally needs Writer
coverage; a QAT change needs QAT regressions and relevant consumer/visual scenes.
Small paint adjustments need affected visual checks; layout, input and lifecycle
changes also need behavior regressions. Run the full suite locally for the broad
changes above or an explicit task gate, rather than after every iteration. Keep
existing regression coverage unless a review establishes that it is redundant.

When reducing the suite, identify the retained check that covers each removal.
Combine assertions that exercise the same fixture when this avoids repeated setup;
keep distinct boundary, input, persistence and rendering scenarios. Record removed
cases separately from consolidated assertions: a lower discovered-test count alone
does not establish faster execution or unchanged code coverage. See the
[first redundancy review](docs/history/library/12-keytips-layout-and-validation.md#3239-test-suite-redundancy-review--2026-10-09).

### Selecting a local test slice

`eng/Test-Focused.ps1` requires an explicit selection, builds the selected test
project in its normal output folder, and fails if the filter discovers no tests.
It defaults to Debug; use `-Configuration Release` for required Release checks.
It temporarily clears inherited scopes and capture/update flags, applies only the
requested scope, and restores the previous environment even when a test fails.

```powershell
./eng/Test-Focused.ps1 -Suite Runtime -Filter 'FullyQualifiedName~GalleryRowScrollingTests'

./eng/Test-Focused.ps1 -Suite Writer -Filter 'FullyQualifiedName~WriterZoomModelTests'

./eng/Test-Focused.ps1 -Suite Visual -Scenes 'crystal-*-qat-*' -Configuration Release

./eng/Test-Focused.ps1 -Suite Portability -Scope GroupedGalleries -Configuration Release
```

Visual selections accept exact approved-image names without `.png`, comma-separated
names, or `*`/`?` wildcards. Every requested pattern must match at least one scene;
output lists the completed scenes and their count. A selected run still includes
the harness's resource/lifecycle assertions, and is not full-matrix evidence.
See [visual selection and review](tests/RibbonKit.VisualTests/README.md).
Portability uses its existing named scopes; see `Get-Help ./eng/Test-Focused.ps1 -Detailed`
and the script's `Scope` parameter for supported values. Each consumer scope is
one aggregate test, so its reported test count is not its scenario count.

For a focused test, substitute an existing test class or method name:

```powershell
dotnet build samples/RibbonKit.Writer/RibbonKit.Writer.csproj --configuration Release
dotnet test tests/RibbonKit.Writer.Tests/RibbonKit.Writer.Tests.csproj --configuration Release --filter 'FullyQualifiedName~ActualTestClass'
```

Use `tests/RibbonKit.Tests/RibbonKit.Tests.csproj` for runtime-control regressions
and `tests/RibbonKit.VisualTests/RibbonKit.VisualTests.csproj` for snapshot coverage.
Use `tests/RibbonKit.Portability.Tests/RibbonKit.Portability.Tests.csproj` for
consumer checks that must load only RibbonKit resources, without Showcase.
Confirm the filter discovered and executed the intended tests; zero matches is not a pass.

The full validation sequence mirrors `.github/workflows/ci.yml`:

```powershell
dotnet restore RibbonKit.sln
dotnet build RibbonKit.sln --no-restore --configuration Release
# Clear local selections and capture/update modes for a full comparison run.
$env:RIBBONKIT_VISUAL_SCENES = $null
$env:RIBBONKIT_PORTABILITY_SCOPE = $null
$env:RIBBONKIT_CAPTURE_SNAPSHOTS = $null
$env:RIBBONKIT_UPDATE_SNAPSHOTS = $null
dotnet test RibbonKit.sln --no-build --configuration Release --verbosity normal
dotnet pack src/RibbonKit/RibbonKit.csproj --no-build --configuration Release --output artifacts
./eng/Validate-Package.ps1 -PackageDirectory artifacts
```

CI retains the full build/test/package gate on pushes and pull requests to `main`,
with local selections and capture/update flags explicitly cleared.
Report focused tests, the full suite, and live acceptance separately. On a visual
mismatch, inspect the failure artifact's actual/diff PNGs before changing approvals.

## Development setup

Use Windows with the SDKs declared by the projects and Visual Studio with the .NET desktop workload for designer work. Open `RibbonKit.sln`; Showcase is the component lab and Writer is the document-editing consumer. See the [README](README.md#building-from-source) for launch commands.
