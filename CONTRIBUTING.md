# Contributing to RibbonKit

Thanks for your interest! RibbonKit v1.0.0 is published through GitHub Releases. The v1 public API is
frozen, so the most valuable contributions are focused fixes, documentation, showcase feedback, and
discussion of additive post-v1 features.

## Ground rules

- One feature or fix per pull request, with a matching showcase page or unit test where it makes sense.
- All controls are **lookless custom controls** — templates live in theme dictionaries, never hardcoded visuals in code-behind.
- Public API needs XML doc comments.
- Match the existing code style (`.editorconfig` / analyzers will guide you; nullable reference types are enabled).

## Public API compatibility

`src/RibbonKit/PublicAPI.Shipped.txt` is the frozen v1 baseline. The
`Microsoft.CodeAnalysis.PublicApiAnalyzers` checks run for both runtime target frameworks and fail the
build when a public symbol is added, removed, or changes nullability without an explicit baseline
update.

- Compatible, intentional additions go in `src/RibbonKit/PublicAPI.Unshipped.txt` and require API-review justification in the pull request.
- Do not edit `PublicAPI.Shipped.txt` to hide a breaking change. Removals and signature/nullability changes require a versioning decision.
- Public members require XML documentation; missing or broken `cref` references fail the runtime build.

## Workflow

1. Open or comment on an issue before starting significant work, so effort isn't duplicated.
2. Fork, create a branch from `main`, make your change.
3. Ensure `dotnet build` and `dotnet test` pass locally.
4. Open a pull request describing what changed and why.

## Development setup

Visual Studio 2022 (17.8+) with the .NET desktop development workload. Open `RibbonKit.sln`; `RibbonKit.Showcase` is the runnable demo used to exercise every feature.
