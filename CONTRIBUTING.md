# Contributing to RibbonKit

Thanks for your interest! RibbonKit is in early development, so the most valuable contributions right now are feedback on the planned API, bug reports against the showcase app, and discussion on the roadmap issues.

## Ground rules

- One feature or fix per pull request, with a matching showcase page or unit test where it makes sense.
- All controls are **lookless custom controls** — templates live in theme dictionaries, never hardcoded visuals in code-behind.
- Public API needs XML doc comments.
- Match the existing code style (`.editorconfig` / analyzers will guide you; nullable reference types are enabled).

## Workflow

1. Open or comment on an issue before starting significant work, so effort isn't duplicated.
2. Fork, create a branch from `main`, make your change.
3. Ensure `dotnet build` and `dotnet test` pass locally.
4. Open a pull request describing what changed and why.

## Development setup

Visual Studio 2022 (17.8+) with the .NET desktop development workload. Open `RibbonKit.sln`; `RibbonKit.Showcase` is the runnable demo used to exercise every feature.
