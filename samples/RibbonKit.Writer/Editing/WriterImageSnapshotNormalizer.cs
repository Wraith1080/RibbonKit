using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace RibbonKit.Writer.Editing;

/// <summary>
/// Projects images repaired after native Undo back to the ordinary image-only inline shape used by
/// persistence and preview snapshots. The live document is never mutated, so its native redo stack
/// remains intact.
/// </summary>
internal static class WriterImageSnapshotNormalizer
{
    internal static bool RequiresNormalization(FlowDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return WriterInlineInsertion.EnumerateImages(document).Any(container =>
            container.Child is Grid { Children.Count: 1 } grid
            && grid.Children[0] is Image);
    }

    internal static void NormalizeClone(FlowDocument source, FlowDocument clone)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(clone);
        if (!RequiresNormalization(source))
            return;

        NormalizeBlocks(source.Blocks.Cast<Block>().ToArray(),
            clone.Blocks.Cast<Block>().ToArray());
    }

    private static void NormalizeBlocks(IReadOnlyList<Block> source,
        IReadOnlyList<Block> clone)
    {
        RequireMatchingCount(source, clone);
        for (var index = 0; index < source.Count; index++)
        {
            switch (source[index], clone[index])
            {
                case (Paragraph sourceParagraph, Paragraph cloneParagraph):
                    NormalizeInlines(sourceParagraph.Inlines, cloneParagraph.Inlines);
                    break;
                case (Section sourceSection, Section cloneSection):
                    NormalizeBlocks(sourceSection.Blocks.Cast<Block>().ToArray(),
                        cloneSection.Blocks.Cast<Block>().ToArray());
                    break;
                case (List sourceList, List cloneList):
                    NormalizeListItems(sourceList.ListItems.Cast<ListItem>().ToArray(),
                        cloneList.ListItems.Cast<ListItem>().ToArray());
                    break;
                case (Table sourceTable, Table cloneTable):
                    NormalizeTables(sourceTable, cloneTable);
                    break;
                default:
                    RequireMatchingType(source[index], clone[index]);
                    break;
            }
        }
    }

    private static void NormalizeListItems(IReadOnlyList<ListItem> source,
        IReadOnlyList<ListItem> clone)
    {
        RequireMatchingCount(source, clone);
        for (var index = 0; index < source.Count; index++)
            NormalizeBlocks(source[index].Blocks.Cast<Block>().ToArray(),
                clone[index].Blocks.Cast<Block>().ToArray());
    }

    private static void NormalizeTables(Table source, Table clone)
    {
        var sourceGroups = source.RowGroups.Cast<TableRowGroup>().ToArray();
        var cloneGroups = clone.RowGroups.Cast<TableRowGroup>().ToArray();
        RequireMatchingCount(sourceGroups, cloneGroups);
        for (var groupIndex = 0; groupIndex < sourceGroups.Length; groupIndex++)
        {
            var sourceRows = sourceGroups[groupIndex].Rows.Cast<TableRow>().ToArray();
            var cloneRows = cloneGroups[groupIndex].Rows.Cast<TableRow>().ToArray();
            RequireMatchingCount(sourceRows, cloneRows);
            for (var rowIndex = 0; rowIndex < sourceRows.Length; rowIndex++)
            {
                var sourceCells = sourceRows[rowIndex].Cells.Cast<TableCell>().ToArray();
                var cloneCells = cloneRows[rowIndex].Cells.Cast<TableCell>().ToArray();
                RequireMatchingCount(sourceCells, cloneCells);
                for (var cellIndex = 0; cellIndex < sourceCells.Length; cellIndex++)
                    NormalizeBlocks(sourceCells[cellIndex].Blocks.Cast<Block>().ToArray(),
                        cloneCells[cellIndex].Blocks.Cast<Block>().ToArray());
            }
        }
    }

    private static void NormalizeInlines(InlineCollection source, InlineCollection clone)
    {
        var sourceInlines = source.Cast<Inline>().ToArray();
        var cloneInlines = clone.Cast<Inline>().ToArray();
        RequireMatchingCount(sourceInlines, cloneInlines);
        for (var index = 0; index < sourceInlines.Length; index++)
        {
            var sourceInline = sourceInlines[index];
            var cloneInline = cloneInlines[index];
            if (sourceInline is InlineUIContainer sourceContainer
                && sourceContainer.Child is Grid { Children.Count: 1 } grid
                && grid.Children[0] is Image sourceImage)
            {
                var normalized = new InlineUIContainer(
                    WriterImageService.CloneImageElement(sourceImage));
                CopyInlineValues(sourceContainer, normalized);
                clone.InsertBefore(cloneInline, normalized);
                clone.Remove(cloneInline);
                continue;
            }

            switch (sourceInline, cloneInline)
            {
                case (Span sourceSpan, Span cloneSpan):
                    NormalizeInlines(sourceSpan.Inlines, cloneSpan.Inlines);
                    break;
                case (Figure sourceFigure, Figure cloneFigure):
                    NormalizeBlocks(sourceFigure.Blocks.Cast<Block>().ToArray(),
                        cloneFigure.Blocks.Cast<Block>().ToArray());
                    break;
                case (Floater sourceFloater, Floater cloneFloater):
                    NormalizeBlocks(sourceFloater.Blocks.Cast<Block>().ToArray(),
                        cloneFloater.Blocks.Cast<Block>().ToArray());
                    break;
                default:
                    RequireMatchingType(sourceInline, cloneInline);
                    break;
            }
        }
    }

    private static void CopyInlineValues(Inline source, Inline target)
    {
        foreach (var property in new[]
                 {
                     TextElement.FontFamilyProperty,
                     TextElement.FontSizeProperty,
                     TextElement.FontStretchProperty,
                     TextElement.FontStyleProperty,
                     TextElement.FontWeightProperty,
                     TextElement.ForegroundProperty,
                     TextElement.BackgroundProperty,
                     FrameworkElement.FlowDirectionProperty,
                     Inline.BaselineAlignmentProperty,
                     Inline.TextDecorationsProperty
                 })
        {
            var value = source.ReadLocalValue(property);
            if (value != DependencyProperty.UnsetValue)
                target.SetValue(property, value);
        }
    }

    private static void RequireMatchingCount<TSource, TClone>(
        IReadOnlyCollection<TSource> source, IReadOnlyCollection<TClone> clone)
    {
        if (source.Count != clone.Count)
            throw new InvalidOperationException(
                "The native WPF snapshot changed document structure while normalizing an image.");
    }

    private static void RequireMatchingType(object source, object clone)
    {
        if (source.GetType() != clone.GetType())
            throw new InvalidOperationException(
                "The native WPF snapshot changed document structure while normalizing an image.");
    }
}
