# Source and dependency notices

RibbonKit.WinForms' own code is distributed under the [MIT license](LICENSE).
The NuGet package declares `MIT AND MS-PL` because it also bundles components
under those licenses; this does not change the license of RibbonKit's own code.

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

## SVG icon support

The native runtime uses **SVG.NET (Svg) 3.4.8**, copyright 2009 Microsoft,
2011 vvvv Group and 2021 SVG.NET Contributors, under the Microsoft Public License
(MS-PL). Its CSS dependency **ExCSS 4.2.3**, copyright Tyler Brinks and
contributors, uses the MIT license. These are managed GDI+ dependencies, not WPF
or Skia components. SVG.NET is a normal runtime package dependency. The private
net472 designer folder also redistributes its framework-specific Svg/ExCSS DLLs
and the required System.Memory/Buffers/Numerics.Vectors/Unsafe support DLLs.
Microsoft support libraries use the accompanying .NET MIT license and notices.
Full dependency notices are included under `licenses/` in the package.
Application developers must retain the applicable license and copyright notices
when redistributing these components. The package supplies the notice files but
does not automatically copy them into application publish or installer output;
include them through the application's distribution process, which can be automated.

The curated 512 regular SVG assets come from Microsoft **Fluent UI System Icons**,
commit `9eb184e96268d3293462f9d9d19225b4c7b07774`, under the MIT license, copyright
2020 Microsoft Corporation. Exact original paths are recorded in
`src/Shared/Icons/Stock/PROVENANCE.md`; the complete stock license is in
`src/Shared/Icons/Stock/LICENSE.txt` and packaged as
`licenses/Fluent-System-Icons.txt`. Artwork is embedded in runtime/designer
assemblies; attribution is retained. Imported application SVG remains
application-owned artwork.
