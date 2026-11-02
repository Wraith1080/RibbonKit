# Group item layout and split orientation

Status, **2026-11-01: implemented; actual Visual Studio and user layout review pending**.
This bounded native increment adds explicit horizontal rows and in-group separators,
and exposes the existing split-button orientation in Edit Ribbon. The permanent
Designer Sample is refreshed in place; its authored Form files are preserved.

## Authoring

In **Edit Ribbon**, select a group and set **Layout**:

| Value | Arrangement |
| --- | --- |
| Columns (default) | Existing automatic stacking of up to three Small/Medium items per column. No Row assignments are required. |
| TwoRows | Two horizontal flows. Assign commands to Row = First or Second. |
| ThreeRows | Three horizontal flows. Assign commands to Row = First, Second or Third. |

Set each row command's **SizePolicy** to fixed Small or Medium, then set **Row**.
Collection order determines the horizontal order within each row; rows start at
the same leading edge and retain their individual item widths. Native TextBox
commands remain Medium fields. ComboBox Medium/Large remains a native field;
Small now uses a compact chooser button. See the
[ComboBox/rearrangement follow-up](DESIGNER-REARRANGEMENT.md) for ShowLabel and
moving between layouts.
Large items retain their full-height presentation, regardless of Row. A Large
item ends both/all current row flows and starts the next section after itself.

**Add → Separator** now works on a group. It ends the current column/row section
and draws a vertical divider. This reuses `RibbonMenuSeparatorItem`; the same
component remains horizontal inside menus and Backstage. Separators have no
activation, command focus, KeyTips or catalog entries. A group separator spans the
command area; it is not a separator confined to one horizontal row.

For split buttons, select **Layout = Horizontal or Vertical**. Horizontal places
the arrow on the right. Vertical places the caption/menu half below the primary
action at Large size; Small/Medium and menu rows remain Horizontal.

Save the Form and reopen its designer to review serialization. Cancel leaves the
live ribbon unchanged; OK uses the existing single designer transaction and
standard Undo/Redo. Nondefault Layout/Row values serialize as ordinary enums.

## Runtime contract and defaults

- New public enums: `RibbonGroupLayout` (Columns, TwoRows, ThreeRows) and
  `RibbonItemRow` (First, Second, Third).
- `RibbonGroup.Layout` defaults to Columns; `RibbonItem.Row` defaults to First.
  Invalid enum values are rejected. Third maps to the final row in TwoRows so
  changing a group's layout keeps every command visible.
- Explicit two/three-row bands center vertically inside the command area. In the
  automatic Columns layout, a group with exactly two occupied rows and no Large
  commands also centers vertically. Single-row, three-row and mixed-Large automatic
  layouts keep their existing arrangement. Decorative separators do not count as
  Large commands.
- Full-group flyouts, including collapsed, Simplified full-group access and QAT
  group access, reuse the same measurement. Simplified inline commands and menus
  retain their existing single-row/menu layouts; Row has no effect there.
- The model retains one owned Items collection. These are native ribbon layout
  modes, not embedded FlowLayoutPanel/TableLayoutPanel controls. IDs, commands,
  binding ownership and customization persistence remain with the existing model.
- `RibbonRenderer.DrawGroupSeparator` is the new replaceable vertical paint hook;
  `DrawMenuSeparator` retains the horizontal menu contract.

## Focused verification

- Normal Debug runtime, design server/client and refreshed package-only Designer
  Sample builds: clean. Public API/XML analyzers passed with the new additive API.
- Initial 33 focused tests passed for split behavior, designer UX, group padding
  and the first two-row implementation. After the three-row extension, 35 focused
  tests passed for the updated layout/designer cases, native TextBox/ComboBox,
  popup geometry and Touch split width. These runs overlap; they are not a claim
  of 68 distinct cases. No full native or WPF suite was run.
- Geometry covers Compact/Touch at 96/120/144/192 DPI, independent row widths,
  all three expanded reduction states, boundaries at Large commands/separators,
  automatic centering and runtime layout invalidation. Designer checks include
  detached preview, Cancel/OK, component names, enum CodeDOM, Undo and Redo.
- The permanent sample's public-API `--verify-group-layout` check passed against
  the packaged library. It covers automatic two/three-item stacks, explicit
  two/three rows, separator painting, a Large vertical split, and retained native
  editor handles/values through Compact/Touch changes. The two 120-DPI captures in
  `winforms/artifacts/group-item-layout/` were inspected.

Actual VS save/reopen, manual keyboard navigation, physical DPI/mixed-monitor
movement and final user visual acceptance remain separate review gates.

From the repository root, the package-only check is:

```powershell
dotnet run --project winforms/samples/RibbonKit.WinForms.DesignerSample -c Debug --no-build -- --verify-group-layout winforms/artifacts/group-item-layout
```
