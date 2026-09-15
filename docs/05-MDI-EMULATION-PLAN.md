# MDI emulation: implemented boundary and remaining plan

M0 floating windows and M4 tab/caption merge are implemented; M2 has theme/model
support but lacks full MVVM Showcase proof. M1/M3 remain. See [current status](../04-DESIGN-NOTES.md#5-current-state--next-steps)
and design-history §3.34. Future names below are proposals, not shipped API.

## Existing architecture

`MdiDocument` is the bindable content/title/icon/placement/state model.
`MdiContainer` is an `ItemsControl`; `MdiCanvasPanel` positions children.
`MdiChild` is a lookless `ContentControl` with drag, resize, activation, close,
minimize/maximize and themed state transitions. Placement uses DIPs.

## 4. Caption and tab merge

`MdiContainer.Ribbon` connects active-document `MergeSource` tabs to the existing
merge service. A maximized active child also supplies its icon and caption controls
through `Ribbon.ShowMergedCaption`/`ClearMergedCaption`, hiding its own title bar.
`IsCaptionMergeEnabled="False"` retains tab merging with the child caption; leaving
`Ribbon` unset maximizes within the client area. Merge-site ownership is therefore
settled on Ribbon, not an unresolved attached-service proposal.

The theme keys and shared MDI templates already exist. Extend those contracts rather
than adding a second chrome family. [Merge/modal invariants](06-MERGE-AND-MODAL-PLAN.md)
apply to document activation, modal tabs and parked QAT commands.

## Remaining milestones

| Milestone | Remaining work and exit |
| --- | --- |
| M1 | Cascade/tile/arrange commands, keyboard document cycling and bounds/minimum-size verification. Prove a Window command group, minimized arrangement and predictable focus recovery. |
| M2 | Prove `ItemsSource` plus content templates beside direct document injection in Showcase, with representative per-theme activation/resize checks. |
| M3 | Switchable tabbed presenter and placement persistence. Switching must preserve document identity/content; restore must survive missing documents and changed viewport bounds. |

Classic MDI plus tabbed presentation over one document model is the chosen direction;
AvalonDock-style docking is outside scope. `DocumentMode`, `MdiTabStrip` and
`MdiLayoutState` in the original plan remain provisional until implemented and reviewed.

## Persistence proposal

Reuse the existing JSON/storage conventions rather than inventing another store.
The consumer supplies stable document keys. Save ordered placement, window state,
z-order, active document and presentation mode; restore only matching live documents
and clamp placements to the current client area. Confirm schema/versioning and key
ownership before exposing public API. This capability has not shipped.

## Verification risks

- Separate tab merge from caption merge; activation must not depend on transient ribbon focus.
- Maintain one active child and restore focus into its content. Exercise close/cancel,
  maximize/restore, source switching and removal of a document with an active modal tab.
- Hosted native HWND content has airspace constraints; do not promise correct floating
  overlap or treat the unimplemented tabbed presenter as an available workaround.
- Test viewport/DPI changes, edge clamping, theme/accent changes and keyboard/UIA behavior.
  Floating children are not virtualized; measure large collections before promising scale.
- Guard live services in designer mode. Public additions require the compatibility
  and validation gates in [CONTRIBUTING.md](../CONTRIBUTING.md).
