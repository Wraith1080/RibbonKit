# RibbonKit Writer icon catalog

> **Status:** app-owned W1-D artwork. `Icons.xaml` currently contains 103 vector `DrawingImage`
> resources: 98 small/general resources and five explicit Large variants. RibbonKit itself does not
> own or synthesize these icons.

## Conventions

- All artwork uses a 24-unit coordinate grid and remains vector at every WPF/DPI scale.
- Current command keys keep the `Icon.Writer*` identities already consumed by `MainWindow.xaml`.
- Large command artwork uses the `.Large` suffix.
- Muted blue and muted amber are the only chromatic `Writer.Icon.Brush.*` resources. Dark ink, slate,
  paper and paper-shadow are structural neutrals and do not introduce extra command identities.
- Dark ink owns primary structure, muted blue owns secondary/action detail, and muted amber is reserved
  for color, warning or deliberate emphasis. Ordinary Home/QAT commands use the same ink/blue pairing
  across Clipboard, Font, Paragraph and Editing instead of assigning a palette per group.
- Every palette pen uses a 1.4-unit rounded stroke. Line-based current commands use those pens instead
  of ad hoc filled bars so alignment, list, indent, paragraph, find and zoom glyphs have one optical weight.
- The semantic palette is referenced with `DynamicResource`; a later appearance dictionary can replace
  it without changing command resources or geometry.
- Contextual sets keep the same color treatment: undo/redo; bold/italic/underline; all text alignments;
  bullets/numbering; find/replace; and all zoom actions. Do not assign a different color just to
  distinguish siblings whose geometry already communicates the operation.

## Current Home/QAT resources

`Document`, `Save`, `Undo`, `Redo`, `Paste`, `Cut`, `Copy`, `Font`, `TextColor`, `Highlight`, `Bold`,
`Italic`, `Underline`, `AlignLeft`, `AlignCenter`, `AlignRight`, `Justify`, `Bullets`, `Numbering`,
`IndentIncrease`, `IndentDecrease`, `ParagraphSpacing`, `Find`, `Replace`, `SelectAll`, `SpellCheck`,
`ZoomOut`, `ZoomReset` and `ZoomIn`.

Large variants currently exist for `Document`, `Save`, `Paste`, `Undo` and `Redo`.

## Prepared reserve

| Feature area | Resource suffixes after `Icon.Writer` |
|---|---|
| File and Backstage | `New`, `Open`, `SaveAs`, `CloseDocument`, `ExportPdf`, `Print`, `PrintPreview`, `Properties`, `Recent` |
| Page layout | `PageSize`, `Portrait`, `Landscape`, `Margins`, `PageColor`, `Columns`, `PageBreak` |
| View | `EditLayout`, `PrintLayout`, `OnePage`, `TwoPages`, `PageWidth`, `PreviousPage`, `NextPage`, `FullScreen`, `Ruler`, `Gridlines` |
| Insert | `Image`, `Hyperlink`, `RemoveLink`, `DateTime`, `Table` |
| Table structure | `AddRowAbove`, `AddRowBelow`, `AddColumnLeft`, `AddColumnRight`, `DeleteRow`, `DeleteColumn`, `MergeCells`, `SplitCells`, `DistributeRows`, `DistributeColumns` |
| Table presentation | `CellAlignTop`, `CellAlignMiddle`, `CellAlignBottom`, `CellShading`, `Borders` |
| Appearance | `Theme`, `DarkMode`, `Backdrop`, `CustomizeRibbon`, `Options` |
| General actions | `Refresh`, `Delete`, `Check`, `Warning`, `Information`, `Error`, `Lock`, `Unlock`, `Import`, `Export`, `Reset`, `Plus`, `Minus`, `Close`, `ArrowUp`, `ArrowDown`, `ArrowLeft`, `ArrowRight` |

Reserve artwork is deliberately present before its command surfaces. Future packets should reuse the closest
semantic resource, add a Large variant only when the ribbon actually gives the action primary visual weight,
and remove a reserve only when the corresponding planned feature is intentionally dropped.
