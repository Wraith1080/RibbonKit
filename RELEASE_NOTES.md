# RibbonKit v1.0.0

RibbonKit v1.0.0 is published through GitHub Releases.

RibbonKit is an MIT-licensed Office Fluent UI-style Ribbon control library for WPF applications on
modern .NET. Version 1.0.0 freezes the validated public API and packages the runtime, Visual Studio
design tools, documentation, symbols, and Source Link support into downloadable GitHub Release
assets.

## Highlights

- Lookless WPF ribbon controls with adaptive large, medium, small, collapsed, split, dropdown, and
  gallery presentations.
- Office 2007, 2010, 2013, 2019, and 2024 themes, including dark and black variants.
- Backstage and Office 2007 application-menu surfaces, QAT customization, KeyTips, ScreenTips,
  contextual tabs, tab merging, modal tabs, localization, RTL, and per-monitor DPI support.
- Runtime customization persistence and a Visual Studio Ribbon Editor with icon browsing.
- MDI floating-child and ribbon/caption merge foundations.

## Verification

- Both `net8.0-windows` and `net9.0-windows` package consumers build from an isolated local source.
- The packaged runtime and explicit theme/control dictionaries were exercised in a live smoke test.
- The package-installed Visual Studio designer, context commands, and Ribbon Editor were verified.
- The Release Showcase performance baseline and deterministic visual approvals are recorded in
  `04-DESIGN-NOTES.md`.

## Known post-v1 work

- Office 2007 window-frame emulation.
- Remaining MDI arrange/cycling, full MVVM proof, tabbed-document mode, and persistence milestones.
- Touch-density architecture and richer automatic QAT projections.

## Development provenance

RibbonKit was developed primarily with AI coding assistants under human direction, visual review,
and automated verification. Release metadata uses the neutral attribution **RibbonKit contributors**.

## Distribution

Distribution is `RibbonKit.1.0.0.nupkg` attached directly to the GitHub Release, with
`RibbonKit.1.0.0.snupkg` and SHA-256 checksums alongside it. No NuGet.org publication is planned.
