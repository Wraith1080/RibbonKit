using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using RibbonKit.Writer.Editing;
using RibbonKit.Writer.Tests.Document;
using Xunit;

namespace RibbonKit.Writer.Tests.Editing;

[Collection("Writer UI")]
public sealed class WriterTableResizeTests
{
    [Fact]
    public void GeometryExposesSelectionColumnRowAndOverallHandlesWithLargeHitTargets()
    {
        var layout = new WriterTableLayoutSnapshot(new Rect(10, 20, 200, 100),
            new[] { 10d, 110d, 210d }, new[] { 20d, 70d, 120d }, 0);
        var dpi = new DpiScale(1.25, 1.25);
        var handles = WriterTableResizeGeometry.GetHandleRects(layout, dpi,
            WriterTableResizeGeometry.VisualHandleSize);

        Assert.Equal(6, handles.Count);
        Assert.Contains(new WriterTableResizeHandle(WriterTableResizeHandleKind.Select), handles.Keys);
        Assert.Contains(new WriterTableResizeHandle(WriterTableResizeHandleKind.Column, 0), handles.Keys);
        Assert.Contains(new WriterTableResizeHandle(WriterTableResizeHandleKind.Column, 1), handles.Keys);
        Assert.Contains(new WriterTableResizeHandle(WriterTableResizeHandleKind.Row, 0), handles.Keys);
        Assert.Contains(new WriterTableResizeHandle(WriterTableResizeHandleKind.Row, 1), handles.Keys);
        Assert.Contains(new WriterTableResizeHandle(WriterTableResizeHandleKind.Overall), handles.Keys);

        var column = new WriterTableResizeHandle(WriterTableResizeHandleKind.Column, 0);
        var visible = handles[column];
        var hitPoint = new Point(visible.X + visible.Width / 2d,
            visible.Y + visible.Height / 2d + 6);
        Assert.False(visible.Contains(hitPoint));
        Assert.True(WriterTableResizeGeometry.TryHitHandle(hitPoint, layout, dpi, out var hit));
        Assert.Equal(column, hit);
        Assert.Same(System.Windows.Input.Cursors.Hand,
            WriterTableResizeAdorner.GetCursor(
                new WriterTableResizeHandle(WriterTableResizeHandleKind.Select)));
        Assert.Same(System.Windows.Input.Cursors.SizeWE,
            WriterTableResizeAdorner.GetCursor(column));
        Assert.Same(System.Windows.Input.Cursors.SizeNS,
            WriterTableResizeAdorner.GetCursor(
                new WriterTableResizeHandle(WriterTableResizeHandleKind.Row, 0)));
        Assert.Same(System.Windows.Input.Cursors.SizeNWSE,
            WriterTableResizeAdorner.GetCursor(
                new WriterTableResizeHandle(WriterTableResizeHandleKind.Overall)));

        var projected = WriterTableLayoutResolver.ProjectRect(new Rect(120, 40, 624, 24),
            new Point(120, 40), 1.25, 1.25);
        Assert.Equal(new Rect(120, 40, 780, 30), projected);
    }

    [Fact]
    public void ResizeMathEnforcesColumnMinimumAndOverallPageBound()
    {
        Assert.Equal(WriterTableResizeGeometry.MinimumColumnWidth,
            WriterTableResizeGeometry.ResizeColumn(100, -1000, 500));
        Assert.Equal(180, WriterTableResizeGeometry.ResizeColumn(100, 1000, 180));
        Assert.Equal(new[] { 125d, 125d },
            WriterTableResizeGeometry.ResizeOverallWidths(new[] { 100d, 100d }, 100, 250));
        Assert.Equal(new[] { 24d, 24d },
            WriterTableResizeGeometry.ResizeOverallWidths(new[] { 100d, 100d }, -1000, 250));
        Assert.Equal(new[] { 24d, 24d, 24d },
            WriterTableResizeGeometry.ResizeOverallWidths(new[] { 24d, 1000d, 24d }, -1000, 500));
    }

    [Fact]
    public void OverallPreviewKeepsBothAxesWhenNativeLayoutIsTemporarilyUnavailable()
    {
        StaTestHelper.Run(() =>
        {
            var editor = new RichTextBox
            {
                Document = new FlowDocument(new Paragraph(new Run("before")))
            };
            using var interaction = new WriterTableInteractionController(editor);
            var paragraph = Assert.IsType<Paragraph>(editor.Document.Blocks.First());
            editor.Selection.Select(paragraph.ContentStart, paragraph.ContentStart);
            var table = Assert.IsType<Table>(interaction.Tables.InsertTable(2, 2));
            table.Columns[0].Width = new GridLength(100, GridUnitType.Pixel);
            table.Columns[1].Width = new GridLength(120, GridUnitType.Pixel);
            interaction.MoveCaret(interaction.GetOrderedCells(table)[0]);

            var decorator = new AdornerDecorator { Child = editor };
            using var window = new TestWindow(decorator);
            window.Show();
            window.UpdateLayout();
            var cells = interaction.GetOrderedCells(table);
            Assert.True(WriterTableLayoutResolver.TryCreate(editor, cells, 0, out var layout));
            WriterTableLayoutSnapshot? liveLayout = layout;
            var opening = new WriterTableResizeOpening(table, layout, cells, table.Columns.Count,
                table.Columns.Select(column => column.Width).ToArray(), new[] { 100d, 120d },
                cells.ToDictionary(cell => cell.Cell, cell => cell.Cell.Padding));
            var adorner = new WriterTableResizeAdorner(editor, () => liveLayout, () => opening,
                (_, _, _) => true, () => { });

            Assert.True(adorner.BeginDragForTesting(
                new WriterTableResizeHandle(WriterTableResizeHandleKind.Overall), new Point()));
            liveLayout = null;
            adorner.UpdateDragForTesting(new Point(40, 20));

            var preview = Assert.IsType<WriterTableLayoutSnapshot>(adorner.RenderedLayoutForTesting);
            Assert.Equal(layout.Bounds.Right + 40, preview.Bounds.Right, 6);
            Assert.Equal(layout.Bounds.Bottom + 20, preview.Bounds.Bottom, 6);
            adorner.CancelDrag();
        });
    }

    [Fact]
    public void AdornerPreviewRollsBackAndOverallReleaseCreatesOneNativeUndoUnit()
    {
        StaTestHelper.Run(() =>
        {
            var editor = new RichTextBox
            {
                Document = new FlowDocument(new Paragraph(new Run("before"))),
                IsUndoEnabled = true
            };
            using var interaction = new WriterTableInteractionController(editor);
            var paragraph = Assert.IsType<Paragraph>(editor.Document.Blocks.First());
            editor.Selection.Select(paragraph.ContentStart, paragraph.ContentStart);
            var table = Assert.IsType<Table>(interaction.Tables.InsertTable(2, 2));
            table.Columns[0].Width = new GridLength(100, GridUnitType.Pixel);
            table.Columns[1].Width = new GridLength(120, GridUnitType.Pixel);
            interaction.MoveCaret(interaction.GetOrderedCells(table)[0]);
            editor.IsUndoEnabled = false;
            editor.IsUndoEnabled = true;

            var decorator = new AdornerDecorator { Child = editor };
            using var window = new TestWindow(decorator);
            var commits = 0;
            using var controller = new WriterTableResizeController(editor, interaction, () => commits++);
            window.Show();
            window.UpdateLayout();
            interaction.Refresh();
            controller.Refresh();
            window.Dispatcher.Invoke(DispatcherPriority.Render, new Action(() => { }));

            Assert.True(WriterTableLayoutResolver.TryCreate(editor,
                interaction.GetOrderedCells(table), 0, out var layout));
            Assert.Equal(2, layout.ColumnCount);
            Assert.Equal(2, layout.RowCount);
            Assert.Equal((100 + table.CellSpacing) * layout.ProjectionScaleX,
                layout.ColumnBoundaries[1] - layout.ColumnBoundaries[0], 6);
            Assert.Equal((120 + table.CellSpacing) * layout.ProjectionScaleX,
                layout.ColumnBoundaries[2] - layout.ColumnBoundaries[1], 6);
            Assert.Equal((220 + 2 * table.CellSpacing) * layout.ProjectionScaleX,
                layout.Bounds.Width, 6);
            var layer = Assert.IsType<AdornerLayer>(AdornerLayer.GetAdornerLayer(editor));
            var adorner = Assert.IsType<WriterTableResizeAdorner>(
                Assert.Single(layer.GetAdorners(editor)!));
            Assert.Null(UIElementAutomationPeer.CreatePeerForElement(adorner));
            Assert.DoesNotContain(nameof(WriterTableResizeAdorner),
                System.Windows.Markup.XamlWriter.Save(editor.Document), StringComparison.Ordinal);

            adorner.SelectTableForTesting();
            Assert.True(interaction.TryGetSelectionRange(out var fullTable));
            Assert.Equal(2, fullTable.RowCount);
            Assert.Equal(2, fullTable.ColumnCount);
            Assert.Equal(0, editor.Selection.End.CompareTo(
                table.RowGroups[0].Rows[1].Cells[1].ElementEnd));
            interaction.MoveCaret(interaction.GetOrderedCells(table)[0]);

            Assert.True(adorner.BeginDragForTesting(
                new WriterTableResizeHandle(WriterTableResizeHandleKind.Column, 0), new Point()));
            adorner.UpdateDragForTesting(new Point(30, 0));
            Assert.Equal(100 + 30 / layout.ProjectionScaleX,
                table.Columns[0].Width.Value, 6);
            Assert.False(editor.CanUndo);
            adorner.SimulateCaptureLossForTesting();
            Assert.Equal(100, table.Columns[0].Width.Value, 6);
            Assert.False(editor.CanUndo);

            Assert.True(adorner.BeginDragForTesting(
                new WriterTableResizeHandle(WriterTableResizeHandleKind.Row, 0), new Point()));
            var openingPadding = table.RowGroups[0].Rows[0].Cells[0].Padding;
            var openingBottom = adorner.RenderedLayoutForTesting!.Bounds.Bottom;
            adorner.UpdateDragForTesting(new Point(0, 20));
            Assert.Equal(openingPadding, table.RowGroups[0].Rows[0].Cells[0].Padding);
            Assert.True(adorner.RenderedLayoutForTesting!.Bounds.Bottom > openingBottom);
            Assert.False(editor.CanUndo);
            adorner.CancelDrag();
            Assert.Equal(openingPadding, table.RowGroups[0].Rows[0].Cells[0].Padding);
            Assert.False(editor.CanUndo);

            Assert.True(adorner.BeginDragForTesting(
                new WriterTableResizeHandle(WriterTableResizeHandleKind.Overall), new Point()));
            var overallOpening = adorner.RenderedLayoutForTesting!;
            adorner.UpdateDragForTesting(new Point(40, 20));
            var overallPreview = adorner.RenderedLayoutForTesting!;
            Assert.Equal(overallOpening.Bounds.Right + 40, overallPreview.Bounds.Right, 6);
            Assert.Equal(overallOpening.Bounds.Bottom + 20, overallPreview.Bounds.Bottom, 6);
            adorner.CompleteDragForTesting();

            Assert.Equal(1, commits);
            var committed = Assert.Single(editor.Document.Blocks.OfType<Table>());
            Assert.True(committed.Columns[0].Width.Value > 100);
            Assert.True(committed.RowGroups[0].Rows[0].Cells[0].Padding.Top > openingPadding.Top);
            interaction.Refresh();
            Assert.True(WriterTableLayoutResolver.TryCreate(editor,
                interaction.GetOrderedCells(committed), 0, out var committedLayout));
            Assert.Equal(2, committedLayout.ColumnCount);
            Assert.Equal(committedLayout.ColumnBoundaries[^1], committedLayout.Bounds.Right, 6);
            Assert.True(editor.CanUndo);
            Assert.True(editor.Undo());
            var restored = Assert.Single(editor.Document.Blocks.OfType<Table>());
            Assert.Equal(100, restored.Columns[0].Width.Value, 6);
            Assert.Equal(openingPadding, restored.RowGroups[0].Rows[0].Cells[0].Padding);
        });
    }

    [Fact]
    public void CellTextAlignmentDoesNotMoveRealizedTableBoundaries()
    {
        StaTestHelper.Run(() =>
        {
            var editor = new RichTextBox
            {
                Document = new FlowDocument(new Paragraph(new Run("before")))
            };
            using var interaction = new WriterTableInteractionController(editor);
            var paragraph = Assert.IsType<Paragraph>(editor.Document.Blocks.First());
            editor.Selection.Select(paragraph.ContentStart, paragraph.ContentStart);
            var table = Assert.IsType<Table>(interaction.Tables.InsertTable(3, 8));
            foreach (var column in table.Columns)
                column.Width = new GridLength(120, GridUnitType.Pixel);
            interaction.MoveCaret(interaction.GetOrderedCells(table)[0]);

            var decorator = new AdornerDecorator { Child = editor };
            using var window = new TestWindow(decorator) { Width = 930, Height = 426 };
            window.Show();
            window.UpdateLayout();
            var cells = interaction.GetOrderedCells(table);
            Assert.True(WriterTableLayoutResolver.TryCreate(editor, cells, 0, out var before));
            Assert.True(interaction.Tables.TryGetCell(table.RowGroups[0].Rows[0].Cells[0],
                out var first));

            Assert.True(interaction.Tables.SetCellAlignment(first, TextAlignment.Center));
            window.UpdateLayout();
            window.Dispatcher.Invoke(DispatcherPriority.Render, new Action(() => { }));
            Assert.True(WriterTableLayoutResolver.TryCreate(editor, cells, 0, out var after));

            Assert.Equal(before.Bounds.Left, after.Bounds.Left, 6);
            Assert.Equal(before.Bounds.Top, after.Bounds.Top, 6);
            Assert.Equal(before.Bounds.Right, after.Bounds.Right, 6);
            Assert.Equal(before.Bounds.Bottom, after.Bounds.Bottom, 6);
            Assert.Equal(before.ColumnBoundaries.Count, after.ColumnBoundaries.Count);
            for (var index = 0; index < before.ColumnBoundaries.Count; index++)
                Assert.Equal(before.ColumnBoundaries[index], after.ColumnBoundaries[index], 6);
            Assert.Equal(before.RowBoundaries.Count, after.RowBoundaries.Count);
            for (var index = 0; index < before.RowBoundaries.Count; index++)
                Assert.Equal(before.RowBoundaries[index], after.RowBoundaries[index], 6);
        });
    }

    private sealed class TestWindow : Window, IDisposable
    {
        internal TestWindow(UIElement content)
        {
            Width = 620;
            Height = 420;
            Content = content;
            ShowInTaskbar = false;
            WindowStyle = WindowStyle.None;
        }

        public void Dispose()
        {
            if (IsVisible)
                Close();
        }
    }
}
