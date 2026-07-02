# RibbonKit

An open-source, Office Fluent UI–style **Ribbon control library for WPF** on modern .NET (net8.0-windows / net9.0-windows).

> **Status: early development (Phase 0 — scaffold).** The API is not stable and nothing is ready for production use yet. Follow the [roadmap](docs/03-ROADMAP.md) to see where things are headed.

## Planned features

Ribbon with tabs, groups, and adaptive button sizing (large / medium / small with automatic reduction), split & dropdown buttons, combo boxes, in-ribbon galleries with live preview, contextual tabs, application button with backstage view, quick access toolbar, minimize mode, tab merging, modal tabs, runtime customize dialog, KeyTips, per-monitor High-DPI support, and five Office theme generations (2007, 2010, 2013, 2019, 2024) with runtime switching.

## Getting started (once released)

```xml
xmlns:rk="urn:ribbonkit"
...
<rk:Ribbon>
    <!-- tabs, groups, buttons -->
</rk:Ribbon>
```

## Building from source

Requires Visual Studio 2022 (17.8+) with the .NET desktop development workload, or the .NET 8/9 SDK. Open `RibbonKit.sln` and set `RibbonKit.Showcase` as the startup project.

## Documentation

Planning and architecture documents live in [`docs/`](docs/): [overview](docs/00-PLANNING-OVERVIEW.md) · [architecture](docs/01-ARCHITECTURE.md) · [features](docs/02-FEATURES.md) · [roadmap](docs/03-ROADMAP.md).

## Contributing

Contributions are welcome once the core skeleton lands — see [CONTRIBUTING.md](CONTRIBUTING.md).

## License

[MIT](LICENSE)
