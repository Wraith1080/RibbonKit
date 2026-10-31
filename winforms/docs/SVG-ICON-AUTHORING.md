# SVG icons and designer catalog

Status, **2026-10-31: initial SVG picker accepted by the user; catalog expansion
available for review**. The user confirmed it "works perfectly" against the
provided stock/import, preview, Cancel/OK, Undo/Redo and save/reopen workflow.
This accepts that bounded initial workflow, without implying broad physical-DPI
or full-suite acceptance.
Baseline: clean `b530a05` on `codex/Ribbonkit-Winform`. One agent; normal
Debug/Release outputs. WPF runtime/build/CI, the reference Commandbar project and
accepted Backstage behavior remain unchanged. No commit, push or publication.

## Contract presented before implementation

Use the existing borrowed `IRibbonIcon`/`Icon` slot. Add immutable SVG artwork,
a curated stock catalog and a modal picker. Selecting a stock icon or importing
SVG edits the detached ribbon draft; outer OK uses one designer transaction.
Cancel retains original icons, image resources, events and bindings. Clearing a
vector icon exposes existing Image/LargeImage fallback without disposing them.
Existing arbitrary custom providers remain code-authored and are preserved
unless explicitly replaced/cleared.

The bounded first picker supports stock and imported SVGs. A new raster picker,
root feature settings, theme authoring/selection, surface selection, Backstage
authoring, RTL and broad dialog styling are outside this increment.

## Public/runtime and resource behavior

- `RibbonSvgIcon(source, useForegroundColor = false)` implements `IRibbonIcon`.
  Source is inline SVG XML or a case-sensitive `stock:key`; it is never an
  external file/URL. Source and color mode are immutable. Original colors are the
  default; opt-in foreground tint uses the renderer's effective color, including
  disabled state. Repeated paints update the color correctly.
- `RibbonStockIcons.All` exposes read-only key/name metadata for 512 Fluent UI
  regular icons. `RibbonStockIcons.Create(key)` creates foreground-tinted stock
  artwork. No Showcase helper/resource reference is required.
- `RibbonSvgIconConverter` supplies complete constructor InstanceDescriptors and
  a versioned string resource representation containing source/color mode.
  Constructor expressions and string-backed .resx resources are both supported.
  No BinaryFormatter payload is used. Imported artwork survives removal of the
  original SVG file because its content is embedded in saved project data.
- Item/group Icon metadata now permits SVG serialization. ShouldSerializeIcon
  excludes opaque custom providers and null; their code-authored boundary is
  retained. Existing image properties and icon-over-image precedence are unchanged.
  The intentional additions are recorded in the native Unshipped API baseline;
  no shipped baseline or WPF API changed.

SVG.NET **3.4.8** provides the managed GDI+ backend, with ExCSS **4.2.3**. The
private document/geometry is retained for repeated vector draws, with no bitmap
cache per target size and no new application asset owner. Draw restores borrowed
Graphics state. SVG.NET can cache the first resolved currentColor paint; the
adapter replaces its private foreground paints each draw so disabled/theme
changes are not frozen to that first result. Original-color mode remains intact.

Supported imports are self-contained static SVG with a positive viewBox: paths,
basic shapes, groups/transforms, gradients, clipping and internal use/symbol
references. External resources, DTDs, scripts, embedded images, text, filters and
other unsupported elements are rejected. Convert text to paths before import.
Limits are 512K source characters (file import also limits byte size), 4096
elements and 64 levels; duplicate/missing/cyclic references are rejected before
the drawing backend. A small rendering preflight rejects unsupported drawing
values before they reach a paint event. These are bounded icon semantics, not
full SVG/browser compatibility.

## Designer implementation

Select a group or command in Edit Ribbon and use **Icon → Choose icon…**. Search
filters stock and already-authored project SVGs. Import SVG reads an application
file, validates it and embeds its XML, adding a filename-only title when needed.
Preview uses vector drawing at small/large device sizes. **Use ribbon color** is
explicit, and its choice survives filtering. Clear vector icon retains raster
fallback. Tabs and the presentation-only Ribbon root have no command icon editor.

Protocol nodes carry an icon draft separately from scalar properties. Opaque
custom providers are represented as preserved references; clients cannot invent
one. The server preflights changed SVGs before mutation, reuses the prepared
instances on commit, and retains unchanged live icon instances. Name/raster
indicators are presentation metadata, not new resource owners. No external file
is written and no NuGet reference is added to the consumer by the picker.

Stock SVGs are pinned to Microsoft fluentui-system-icons commit
`9eb184e96268d3293462f9d9d19225b4c7b07774`, with source paths and MIT attribution
under `src/Shared/Icons/Stock/`. Full SVG.NET, ExCSS, .NET support-library and stock
notices ship in the package; see [dependency notices](../THIRD-PARTY-NOTICES.md).
The ordinary runtime package depends on Svg. The private net472 designer folder
contains matching Svg/ExCSS and framework support DLLs; the protocol still has no
runtime library dependency, and the runtime still has no Designer SDK dependency.

## Expanded stock catalog — 2026-10-31

The user requested substantially more icons after accepting the initial picker.
The catalog now contains **512 regular SVGs** from the same pinned Fluent source,
up from 24. All original keys, names and initial ordering are preserved. New
coverage includes files/documents, text/formatting, tables/data, layout/windows,
navigation, people/collaboration, mail/scheduling, media/drawing, status/security,
devices/development, general commands and cloud/network actions. Exact source
paths are grouped in the stock provenance record. All SVG source files together
are 679,452 bytes before assembly/package compression.

The metadata list is generated separately from the unchanged SVG renderer.
Existing public APIs, SVG semantics, dependencies, ownership and stored
identifiers are unchanged. The picker still parses/draws artwork lazily as it is
selected or becomes visible, rather than pre-rendering the full catalog.

Focused Debug evidence: **20 SVG/catalog cases passed**, including every one of
the 512 assets at six sizes (3,072 checked renderings), unique keys, original-key
compatibility, broad category searches, multiple Save results and no-match
behavior. Report: `artifacts/icon-catalog-20261031/stock-512-first.trx` at the
repository root. A fresh package consumer verifies expanded catalog metadata and
an added stock asset through public APIs, alongside prior SVG/resource/binding
checks. Normal Debug/Release build/package checks cover both runtime and net472
client payloads. Prior unrelated evidence below is retained; no new full suite,
Backstage campaign or physical-DPI acceptance is claimed.

The expanded collection's browsing/appearance remains user review. Use the fresh
solution below and try searches such as Document, Table, Calendar, Shield and
Code. Existing saved stock selections should still reopen unchanged.

## Original 24-icon evidence and limits

Artifacts are under `artifacts/svg-icons-20261031/` at the repository root.

- Native solution Release build passed with zero warnings/errors, including the
  native API analyzer and both modern/server and net472/client targets. Affected
  projects also build in normal Debug outputs.
- `svg-designer-release-final.trx`: **48 focused Release cases passed**, covering
  SVG and existing designer behavior. This includes every stock asset at 16/20/
  24/32/48/64 pixels, Graphics state/clip retention, original/tinted/disabled
  repeated paints, gradients/transforms, rejected inputs, constructor/string/resx
  round-trip, explicit replacement/clearing, unchanged-instance retention,
  detached draft behavior and picker filtering/import. Final preflight-allocation
  and picker-window refinements retain that evidence and have affected follow-up
  checks. No physical-DPI or visual approval is implied.
- Earlier .resx validation found the framework requires a serializable marker
  even for converter-backed resources. The icon stores serializable source/mode
  while its private parsed cache is excluded; string-resource checks explicitly
  reject a binary resource payload. This was fixed before the final focused run.
- The broader Release unit run was **597 passed / 4 failed / 601 total**, not a
  clean aggregate. In `native-failed-scopes-isolated.trx`, four of five related
  cases passed (including both motion variants); the material-on-open-Backstage
  opacity assertion still failed. `baseline-material-case.trx` reproduces the
  same assertion on untouched `b530a05` in a detached checkout. That checkout was
  removed after verification. Accepted Backstage/runtime behavior was not changed.
  The full run predates the final tint/preflight refinements; affected SVG checks
  cover those refinements. No full-suite pass is claimed.
- The isolated package-only Form compiles and verifies stock SVG from .resx,
  a generated group-icon constructor, public vector drawing at 16/32/64 pixels,
  foreground color, original raster resources, IDs/ownership, events and two-way
  binding. This proves reusable runtime/package behavior, not custom-dialog
  loading or saved authoring inside VS.

The initial actual-VS workflow was subsequently accepted as recorded in the
status above. Expanded-catalog appearance and physical DPI remain user review.
No app screenshot or visual inspection was performed by the agent. No unchanged
appearance/Backstage capture campaign was repeated.

## User review

Close the older RibbonKit.Designer.Check VS window, then open:

```text
C:\Users\LENOVO\AppData\Local\Temp\RibbonKit-WinForms-Designer-20261031-icons-512-review\App\RibbonKit.Designer.Check.sln
```

1. Open DesignerForm.cs with View Designer. Select the ribbon → Edit Ribbon…,
   then select a command/group → Choose icon…. Search Save/Copy, check the two
   preview sizes, and try foreground/original colors.
2. Import `App\Assets\ExampleBadge.svg`. It should retain both colors with Use
   ribbon color off and become monochrome with it on. Cancel the picker, then
   test accepting the picker but cancelling the outer editor: neither should
   change live icons.
3. Accept an icon through both dialogs. Undo/Redo once on the designer surface,
   save, close/reopen the designer and solution, then run. Confirm the assigned
   icon and stock group icon, including a collapsed group if desired. Rename the
   imported source file after saving; the assigned icon should still load.
4. Clear a vector icon and confirm any existing raster fallback remains. Keep
   the fixture's original Apply image, Bold stock icon and Commands group stock
   icon when using its supplied verifier; make exploratory assignments on newly
   added commands. Resize both dialogs and inspect your available display scales.
5. Follow [the consumer verifier command](DESIGNER-AUTHORING.md#user-review-steps)
   after saving. Before-save package checks and after-VS authoring are distinct.
