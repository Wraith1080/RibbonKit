# Source and dependency notices

RibbonKit.WinForms is distributed under the [MIT license](LICENSE).

The pure group reduction selector and its seed tests are adapted from RibbonKit
WPF v1.1.0, commit `cf1b942a1ec707538084a51202b03ad68d9be33e`, under the same MIT
license. See [the pinned source map](docs/reference/WPF-1.1.0.md) for original paths,
hashes and adaptations. No WPF assembly or XAML is part of the native runtime.

The UI Automation interface declarations follow the public COM ABI in Windows
SDK 10.0.26100.0 `UIAutomationCore.h`; provider implementation is new native code.
Windows framework APIs remain Microsoft platform components.

Microsoft.WinForms.Designer.SDK 1.6.0 is a private build dependency of the design
assembly. Microsoft.CodeAnalysis.PublicApiAnalyzers 5.6.0 is a private analyzer
dependency. Neither SDK/analyzer binaries nor test packages are redistributed.
Design support is packaged in `lib/net10.0-windows7.0/Design/WinForms/Server` according
to Microsoft's WinForms designer package convention. Its actual VS loading and
authoring behavior remain subject to the acceptance ledger.
