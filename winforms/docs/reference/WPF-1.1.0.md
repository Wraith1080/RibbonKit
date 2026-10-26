# WPF reference and copy provenance

Reference: [RibbonKit WPF v1.1.0](https://github.com/Wraith1080/RibbonKit/releases/tag/v1.1.0),
commit `cf1b942a1ec707538084a51202b03ad68d9be33e`, MIT licensed. Native implementation
and package versioning are independent. WPF runtime, XAML and peers are not linked.

| Original path at reference commit | Native destination | Adaptation |
| --- | --- | --- |
| `src/RibbonKit/Layout/ReductionAlgorithm.cs` | `src/RibbonKit.WinForms/Layout/ReductionAlgorithm.cs` | Namespace changed; class made internal; provenance comment added. Pure selector logic retained. |
| `tests/RibbonKit.Tests/ReductionAlgorithmTests.cs` | `tests/RibbonKit.WinForms.Tests/ReductionAlgorithmTests.cs` | Native namespace/import and provenance comment; seed cases retained. |

SHA-256 of original files in the verified reference checkout:

```text
ReductionAlgorithm.cs
1C0EEC5674D9805120650D89323C444A411B3C18EC82BCEAD4345C41D929B27F
ReductionAlgorithmTests.cs
CF20C9718D6782A55F3E92FFCF01E838B7CAB58AA0FFC7FBB376163EEC1FFB66
```

Native formatting follows this subtree's editor settings. Reuse the source
license in [LICENSE](../../LICENSE). Other native implementation is newly written
from the plan's behavioral contracts. Initial palette values are Office inspired;
no pixel-equivalence claim is made for WPF theme templates or text rasterization.

COM interface ABI was checked against the installed Windows SDK 10.0.26100.0
`UIAutomationCore.h`. It does not use WPF automation peers, WindowsBase structs,
private WinForms provider types or reflection into framework internals.
