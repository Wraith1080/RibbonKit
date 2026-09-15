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
| Shared templates, public APIs, cross-project behavior, build configuration, or release readiness | Build and test `RibbonKit.sln` in Release; include affected live UI gates. For packaging/designer distribution, also pack and validate the package. |

Explicit task acceptance gates take precedence. Broaden a focused run when dependency
impact, a failure, or unresolved risk warrants it. Do not repeat successful checks
without a relevant new change. `--no-build` requires a successful matching build of
the test project/solution with the same configuration and framework.

For a focused test, substitute an existing test class or method name:

```powershell
dotnet build samples/RibbonKit.Writer/RibbonKit.Writer.csproj --configuration Release
dotnet test tests/RibbonKit.Writer.Tests/RibbonKit.Writer.Tests.csproj --configuration Release --filter 'FullyQualifiedName~ActualTestClass'
```

Use `tests/RibbonKit.Tests/RibbonKit.Tests.csproj` for runtime-control regressions
and `tests/RibbonKit.VisualTests/RibbonKit.VisualTests.csproj` for snapshot coverage.
Confirm the filter discovered and executed the intended tests; zero matches is not a pass.

The full validation sequence mirrors `.github/workflows/ci.yml`:

```powershell
dotnet restore RibbonKit.sln
dotnet build RibbonKit.sln --no-restore --configuration Release
dotnet test RibbonKit.sln --no-build --configuration Release --verbosity normal
dotnet pack src/RibbonKit/RibbonKit.csproj --no-build --configuration Release --output artifacts
./eng/Validate-Package.ps1 -PackageDirectory artifacts
```

CI retains the full build/test/package gate on pushes and pull requests to `main`.
Report focused tests, the full suite, and live acceptance separately. On a visual
mismatch, inspect the failure artifact's actual/diff PNGs before changing approvals.

## Development setup

Use Windows with the SDKs declared by the projects and Visual Studio with the .NET desktop workload for designer work. Open `RibbonKit.sln`; Showcase is the component lab and Writer is the document-editing consumer. See the [README](README.md#building-from-source) for launch commands.
