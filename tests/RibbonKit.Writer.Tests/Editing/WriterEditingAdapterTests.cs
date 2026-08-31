using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using RibbonKit.Writer.Editing;
using RibbonKit.Writer.Tests.Document;
using Xunit;

namespace RibbonKit.Writer.Tests.Editing;

[Collection("Writer UI")]
public sealed class WriterEditingAdapterTests
{
    [Fact]
    public void EmptyDocumentReportsInsertionDefaultsAndNativeCommandState()
    {
        StaTestHelper.Run(() =>
        {
            using var fixture = CreateFixture(new FlowDocument());

            Assert.False(fixture.Adapter.State.HasTextContext);
            Assert.Equal(WriterSelectionValueKind.Unset, fixture.Adapter.State.Bold.Kind);
            Assert.True(fixture.Adapter.State.FontFamily.IsUniform);
            Assert.Equal(fixture.Editor.Document.FontFamily.Source,
                fixture.Adapter.State.FontFamily.Value.Source);
            Assert.True(fixture.Adapter.State.FontSize.IsUniform);
            Assert.Equal(fixture.Editor.Document.FontSize, fixture.Adapter.State.FontSize.Value);
            Assert.True(fixture.Adapter.State.Alignment.IsUniform);
            Assert.Equal(TextAlignment.Left, fixture.Adapter.State.Alignment.Value);
            Assert.False(fixture.Adapter.State.CanCopy);
            Assert.False(fixture.Adapter.State.CanCut);
            Assert.False(fixture.Adapter.State.CanUndo);
            Assert.False(fixture.Adapter.State.CanRedo);
            Assert.True(fixture.Adapter.CanExecute(EditingCommands.ToggleBold));
            fixture.Adapter.ToggleBold();
            fixture.Adapter.SetAlignment(TextAlignment.Center);
            Assert.Equal(WriterSelectionValueKind.Unset, fixture.Adapter.State.Bold.Kind);
            Assert.True(fixture.Adapter.State.Alignment.IsUniform);
            Assert.Equal(TextAlignment.Center, fixture.Adapter.State.Alignment.Value);
        });
    }

    [Fact]
    public void CaretOnlyStateReadsAdjacentCharacterWithoutSelectingIt()
    {
        StaTestHelper.Run(() =>
        {
            var paragraph = new Paragraph();
            var run = new Run("caret")
            {
                FontFamily = new FontFamily("Arial"),
                FontSize = 18,
                FontWeight = FontWeights.Bold
            };
            paragraph.Inlines.Add(run);
            using var fixture = CreateFixture(new FlowDocument(paragraph));
            fixture.Editor.Selection.Select(run.ContentStart, run.ContentStart);

            Assert.False(fixture.Adapter.State.HasSelection);
            Assert.True(fixture.Adapter.State.HasTextContext);
            Assert.True(fixture.Adapter.State.FontFamily.IsUniform);
            Assert.Equal("Arial", fixture.Adapter.State.FontFamily.Value.Source);
            Assert.Equal(18, fixture.Adapter.State.FontSize.Value);
            Assert.True(fixture.Adapter.State.Bold.Value);
            Assert.False(fixture.Adapter.State.CanCopy);
        });
    }

    [Fact]
    public void UniformSelectionReportsValuesWithoutCoercingDocument()
    {
        StaTestHelper.Run(() =>
        {
            var paragraph = new Paragraph();
            var run = new Run("uniform")
            {
                FontWeight = FontWeights.Bold,
                FontStyle = FontStyles.Italic,
                TextDecorations = WriterFontEffects.CreateDecorations(
                    underline: true, WriterStrikethroughStyle.Double),
                BaselineAlignment = BaselineAlignment.Superscript,
                Foreground = Brushes.DarkBlue,
                Background = Brushes.LightYellow
            };
            paragraph.Inlines.Add(run);
            using var fixture = CreateFixture(new FlowDocument(paragraph));
            fixture.Editor.Selection.Select(run.ContentStart, run.ContentEnd);

            var state = fixture.Adapter.State;
            Assert.True(state.Bold.IsUniform && state.Bold.Value);
            Assert.True(state.Italic.IsUniform && state.Italic.Value);
            Assert.True(state.Underline.IsUniform && state.Underline.Value);
            Assert.Equal(WriterStrikethroughStyle.Double, state.Strikethrough.Value);
            Assert.Equal(WriterBaselineEffect.Superscript, state.BaselineEffect.Value);
            Assert.Equal(Colors.DarkBlue, state.Foreground.Value);
            Assert.Equal(Colors.LightYellow, state.Highlight.Value);
            Assert.True(state.CanCopy);
            Assert.True(state.CanCut);
        });
    }

    [Fact]
    public void UnformattedSelectionReportsEffectiveNativeDefaultsAsUniform()
    {
        StaTestHelper.Run(() =>
        {
            var paragraph = new Paragraph(new Run("defaults"));
            using var fixture = CreateFixture(new FlowDocument(paragraph));
            fixture.Editor.SelectAll();

            var state = fixture.Adapter.State;
            Assert.True(state.FontFamily.IsUniform, state.FontFamily.Kind.ToString());
            Assert.True(state.FontSize.IsUniform, state.FontSize.Kind.ToString());
            Assert.True(state.Bold.IsUniform, state.Bold.Kind.ToString());
            Assert.True(state.Italic.IsUniform, state.Italic.Kind.ToString());
            Assert.True(state.Underline.IsUniform, state.Underline.Kind.ToString());
            Assert.True(state.Foreground.IsUniform, state.Foreground.Kind.ToString());
            Assert.True(state.Highlight.IsUniform, state.Highlight.Kind.ToString());
            Assert.True(state.Alignment.IsUniform, state.Alignment.Kind.ToString());
        });
    }

    [Fact]
    public void MixedSelectionRemainsMixedUntilAnExplicitCommandChangesIt()
    {
        StaTestHelper.Run(() =>
        {
            var paragraph = new Paragraph();
            var bold = new Run("bold") { FontWeight = FontWeights.Bold };
            var normal = new Run("normal") { FontWeight = FontWeights.Normal };
            paragraph.Inlines.Add(bold);
            paragraph.Inlines.Add(normal);
            using var fixture = CreateFixture(new FlowDocument(paragraph));
            fixture.Editor.Selection.Select(bold.ContentStart, normal.ContentEnd);

            Assert.Equal(WriterSelectionValueKind.Mixed, fixture.Adapter.State.Bold.Kind);
            Assert.True(fixture.Adapter.TryExecute(EditingCommands.ToggleBold));

            var selected = new TextRange(fixture.Editor.Selection.Start, fixture.Editor.Selection.End);
            Assert.Equal(FontWeights.Bold, selected.GetPropertyValue(TextElement.FontWeightProperty));
            Assert.True(fixture.Adapter.State.Bold.IsUniform && fixture.Adapter.State.Bold.Value);
        });
    }

    [Fact]
    public void MixedParagraphStateIsReportedWithoutChoosingTheFirstParagraph()
    {
        StaTestHelper.Run(() =>
        {
            var first = new Paragraph(new Run("left")) { TextAlignment = TextAlignment.Left, Margin = new Thickness(4, 2, 0, 3) };
            var second = new Paragraph(new Run("right")) { TextAlignment = TextAlignment.Right, Margin = new Thickness(20, 6, 0, 9) };
            using var fixture = CreateFixture(CreateDocument(first, second));
            fixture.Editor.SelectAll();

            Assert.Equal(WriterSelectionValueKind.Mixed, fixture.Adapter.State.Alignment.Kind);
            Assert.Equal(WriterSelectionValueKind.Mixed, fixture.Adapter.State.Indentation.Kind);
            Assert.Equal(WriterSelectionValueKind.Mixed, fixture.Adapter.State.SpacingBefore.Kind);
            Assert.Equal(WriterSelectionValueKind.Mixed, fixture.Adapter.State.SpacingAfter.Kind);
        });
    }

    [Fact]
    public void ReadOnlyEditorDisablesMutatingCommandsButStillAllowsCopy()
    {
        StaTestHelper.Run(() =>
        {
            using var fixture = CreateFixture(new FlowDocument(new Paragraph(new Run("read only"))));
            fixture.Editor.SelectAll();
            fixture.Editor.IsReadOnly = true;
            fixture.Adapter.RefreshState();

            Assert.True(fixture.Adapter.State.CanCopy);
            Assert.False(fixture.Adapter.State.CanCut);
            Assert.False(fixture.Adapter.State.CanPaste);
            Assert.False(fixture.Adapter.CanExecute(EditingCommands.ToggleBold));
            Assert.False(fixture.Adapter.TryExecute(EditingCommands.ToggleBold));
        });
    }

    [Fact]
    public void DisabledEditorBlocksEveryAdapterCommandAndReenableRefreshesState()
    {
        StaTestHelper.Run(() =>
        {
            try
            {
                Clipboard.Clear();
                Clipboard.SetText("paste");
                using var fixture = CreateFixture(new FlowDocument(new Paragraph(new Run("disabled"))));
                fixture.Editor.SelectAll();
                var stateChanges = 0;
                fixture.Adapter.StateChanged += (_, _) => stateChanges++;

                fixture.Editor.IsEnabled = false;

                Assert.False(fixture.Adapter.State.IsEnabled);
                Assert.False(fixture.Adapter.State.CanFormat);
                Assert.False(fixture.Adapter.State.CanCopy);
                Assert.False(fixture.Adapter.State.CanCut);
                Assert.False(fixture.Adapter.State.CanPaste);
                Assert.False(fixture.Adapter.State.CanSelectAll);
                Assert.False(fixture.Adapter.State.CanUndo);
                Assert.False(fixture.Adapter.State.CanRedo);
                Assert.False(fixture.Adapter.CanExecute(ApplicationCommands.Copy));
                Assert.False(fixture.Adapter.CanExecute(ApplicationCommands.Cut));
                Assert.False(fixture.Adapter.CanExecute(ApplicationCommands.Paste));
                Assert.False(fixture.Adapter.CanExecute(ApplicationCommands.SelectAll));
                Assert.False(fixture.Adapter.CanExecute(ApplicationCommands.Undo));
                Assert.False(fixture.Adapter.CanExecute(ApplicationCommands.Redo));
                Assert.False(fixture.Adapter.CanExecute(EditingCommands.ToggleBold));
                Assert.False(fixture.Adapter.CanExecute(WriterEditingCommands.ApplyFontSize, 12d));
                Assert.False(fixture.Adapter.TryExecute(EditingCommands.ToggleBold));
                Assert.False(fixture.Adapter.TryExecute(ApplicationCommands.SelectAll));
                Assert.True(stateChanges > 0);

                fixture.Editor.IsEnabled = true;
                Assert.True(fixture.Adapter.State.IsEnabled);
                Assert.True(fixture.Adapter.State.CanFormat);
                Assert.True(fixture.Adapter.State.CanCopy);
                Assert.True(fixture.Adapter.State.CanSelectAll);
            }
            finally
            {
                Clipboard.Clear();
            }
        });
    }

    [Fact]
    public void ReadOnlyEditorBlocksUndoRedoAndFormattingButAllowsEnabledCopyAndSelectAll()
    {
        StaTestHelper.Run(() =>
        {
            using var fixture = CreateFixture(new FlowDocument(new Paragraph(new Run("readonly"))));
            fixture.Editor.SelectAll();
            fixture.Adapter.ToggleBold();
            fixture.Adapter.Undo();
            fixture.Adapter.ToggleBold();
            Assert.True(fixture.Editor.CanUndo);

            fixture.Editor.IsReadOnly = true;

            Assert.True(fixture.Adapter.State.IsEnabled);
            Assert.True(fixture.Adapter.State.IsReadOnly);
            Assert.False(fixture.Adapter.State.CanFormat);
            Assert.True(fixture.Adapter.State.CanCopy);
            Assert.False(fixture.Adapter.State.CanCut);
            Assert.False(fixture.Adapter.State.CanUndo);
            Assert.False(fixture.Adapter.State.CanRedo);
            Assert.True(fixture.Adapter.State.CanSelectAll);
            Assert.False(fixture.Adapter.CanExecute(ApplicationCommands.Undo));
            Assert.False(fixture.Adapter.CanExecute(ApplicationCommands.Redo));
            Assert.False(fixture.Adapter.CanExecute(EditingCommands.ToggleBold));
            Assert.True(fixture.Adapter.CanExecute(ApplicationCommands.Copy));
            Assert.True(fixture.Adapter.CanExecute(ApplicationCommands.SelectAll));

            var before = fixture.Adapter.State.Bold;
            fixture.Adapter.Undo();
            fixture.Adapter.Redo();
            fixture.Adapter.ToggleBold();
            Assert.Equal(before, fixture.Adapter.State.Bold);
        });
    }

    [Fact]
    public void AvailabilityNotificationsStopAfterDispose()
    {
        StaTestHelper.Run(() =>
        {
            using var fixture = CreateFixture(new FlowDocument(new Paragraph(new Run("events"))));
            var stateChanges = 0;
            fixture.Adapter.StateChanged += (_, _) => stateChanges++;
            fixture.Editor.IsReadOnly = true;
            fixture.Editor.IsEnabled = false;
            Assert.True(stateChanges >= 2);

            var beforeDispose = stateChanges;
            fixture.Adapter.Dispose();
            fixture.Editor.IsReadOnly = false;
            fixture.Editor.IsEnabled = true;
            fixture.Editor.SelectAll();
            CommandManager.InvalidateRequerySuggested();
            fixture.Editor.Dispatcher.Invoke(DispatcherPriority.ApplicationIdle, new Action(() => { }));
            Assert.Equal(beforeDispose, stateChanges);
        });
    }

    [Fact]
    public void ClipboardStateHasExplicitRefreshAndLiveCanExecuteSeam()
    {
        StaTestHelper.Run(() =>
        {
            try
            {
                Clipboard.Clear();
                using var fixture = CreateFixture(new FlowDocument(new Paragraph(new Run("clipboard"))));
                fixture.Editor.SelectAll();
                fixture.Adapter.RefreshState();
                Assert.False(fixture.Adapter.State.CanPaste);

                Clipboard.SetText("new clipboard text");
                Assert.True(fixture.Adapter.CanExecute(ApplicationCommands.Paste));
                Assert.False(fixture.Adapter.State.CanPaste);

                CommandManager.InvalidateRequerySuggested();
                fixture.Editor.Dispatcher.Invoke(DispatcherPriority.ApplicationIdle, new Action(() => { }));
                Assert.True(fixture.Adapter.State.CanPaste);

                fixture.Adapter.RefreshState();
                Assert.True(fixture.Adapter.State.CanPaste);
                Assert.True(fixture.Adapter.CanExecute(ApplicationCommands.Paste));
            }
            finally
            {
                Clipboard.Clear();
            }
        });
    }

    [Fact]
    public void SelectionLocalParagraphTraversalHandlesSectionListTableAndCollapsedBoundaries()
    {
        StaTestHelper.Run(() =>
        {
            var before = new Paragraph(new Run("before"));
            var section = new Section();
            var sectionFirst = new Paragraph(new Run("section first"));
            var sectionSecond = new Paragraph(new Run("section second"));
            section.Blocks.Add(sectionFirst);
            section.Blocks.Add(sectionSecond);

            var list = new List();
            var listItem = new ListItem();
            var listParagraph = new Paragraph(new Run("list item"));
            listItem.Blocks.Add(listParagraph);
            list.ListItems.Add(listItem);

            var cellParagraph = new Paragraph(new Run("table cell"));
            var table = new Table();
            table.Columns.Add(new TableColumn());
            var tableGroup = new TableRowGroup();
            var tableRow = new TableRow();
            var tableCell = new TableCell();
            tableCell.Blocks.Add(cellParagraph);
            tableRow.Cells.Add(tableCell);
            tableGroup.Rows.Add(tableRow);
            table.RowGroups.Add(tableGroup);

            var after = new Paragraph(new Run("after"));
            var document = new FlowDocument();
            document.Blocks.Add(before);
            document.Blocks.Add(section);
            document.Blocks.Add(list);
            document.Blocks.Add(table);
            document.Blocks.Add(after);
            using var fixture = CreateFixture(document);

            fixture.Editor.Selection.Select(sectionFirst.ContentStart, sectionFirst.ContentEnd);
            fixture.Adapter.SetAlignment(TextAlignment.Center);
            Assert.Equal(TextAlignment.Center, sectionFirst.TextAlignment);
            Assert.Equal(TextAlignment.Left, sectionSecond.TextAlignment);
            Assert.Equal(TextAlignment.Left, before.TextAlignment);
            Assert.Equal(TextAlignment.Left, after.TextAlignment);

            fixture.Editor.Selection.Select(listParagraph.ContentStart, listParagraph.ContentEnd);
            fixture.Adapter.SetParagraphSpacingAfter(13);
            Assert.Equal(13, listParagraph.Margin.Bottom);
            Assert.NotEqual(13, sectionSecond.Margin.Bottom);

            fixture.Editor.Selection.Select(cellParagraph.ContentStart, cellParagraph.ContentEnd);
            fixture.Adapter.SetAlignment(TextAlignment.Right);
            Assert.Equal(TextAlignment.Right, cellParagraph.TextAlignment);
            Assert.Equal(TextAlignment.Center, sectionFirst.TextAlignment);
            Assert.Equal(TextAlignment.Left, sectionSecond.TextAlignment);

            fixture.Editor.Selection.Select(sectionSecond.ContentStart, sectionSecond.ContentStart);
            fixture.Adapter.SetAlignment(TextAlignment.Justify);
            Assert.Equal(TextAlignment.Justify, sectionSecond.TextAlignment);
            Assert.Equal(TextAlignment.Center, sectionFirst.TextAlignment);
            Assert.Equal(TextAlignment.Right, cellParagraph.TextAlignment);
        });
    }

    [Fact]
    public void FontSizeDimensionAndGradientValidationRejectUnsupportedInputs()
    {
        StaTestHelper.Run(() =>
        {
            var run = new Run("validation");
            var paragraph = new Paragraph(run);
            using var fixture = CreateFixture(new FlowDocument(paragraph));
            fixture.Editor.SelectAll();

            Assert.False(fixture.Adapter.CanExecute(WriterEditingCommands.ApplyFontSize, double.NaN));
            Assert.False(fixture.Adapter.CanExecute(WriterEditingCommands.ApplyFontSize, double.PositiveInfinity));
            Assert.False(fixture.Adapter.CanExecute(WriterEditingCommands.ApplyFontSize, double.MaxValue));
            Assert.Throws<ArgumentOutOfRangeException>(() => fixture.Adapter.ApplyFontSize(double.MaxValue));
            Assert.False(fixture.Adapter.CanExecute(WriterEditingCommands.SetIndentation, double.NaN));
            Assert.False(fixture.Adapter.CanExecute(WriterEditingCommands.SetIndentation, double.MaxValue));
            Assert.False(fixture.Adapter.CanExecute(WriterEditingCommands.SetParagraphSpacingAfter, -1d));
            Assert.Throws<ArgumentOutOfRangeException>(() => fixture.Adapter.SetIndentation(double.MaxValue));

            var gradient = new LinearGradientBrush(Colors.Red, Colors.Blue, 0);
            Assert.False(fixture.Adapter.CanExecute(WriterEditingCommands.ApplyForeground, gradient));
            Assert.False(fixture.Adapter.TryExecute(WriterEditingCommands.ApplyForeground, gradient));
            Assert.Throws<ArgumentException>(() => fixture.Adapter.ApplyForeground(gradient));

            run.Foreground = gradient;
            fixture.Editor.Selection.Select(run.ContentStart, run.ContentEnd);
            Assert.True(fixture.Adapter.State.Foreground.IsUnsupported);
            Assert.False(fixture.Adapter.State.Foreground.IsUniform);
        });
    }

    [Fact]
    public void FormattingCommandsApplyNativePropertiesAndParagraphState()
    {
        StaTestHelper.Run(() =>
        {
            var first = new Paragraph(new Run("first"));
            var second = new Paragraph(new Run("second"));
            using var fixture = CreateFixture(CreateDocument(first, second));
            fixture.Editor.SelectAll();

            fixture.Adapter.ApplyFontFamily(new FontFamily("Arial"));
            fixture.Adapter.ApplyFontSize(22);
            fixture.Adapter.ToggleItalic();
            fixture.Adapter.ToggleUnderline();
            fixture.Adapter.ApplyForeground(Colors.DarkGreen);
            fixture.Adapter.ApplyHighlight(Colors.LightGreen);
            fixture.Adapter.SetAlignment(TextAlignment.Center);
            fixture.Adapter.SetIndentation(24);
            fixture.Adapter.SetParagraphSpacingBefore(6);
            fixture.Adapter.SetParagraphSpacingAfter(8);

            Assert.Equal("Arial", fixture.Adapter.State.FontFamily.Value.Source);
            Assert.Equal(22, fixture.Adapter.State.FontSize.Value);
            Assert.True(fixture.Adapter.State.Italic.Value);
            Assert.True(fixture.Adapter.State.Underline.Value);
            Assert.Equal(Colors.DarkGreen, fixture.Adapter.State.Foreground.Value);
            Assert.Equal(Colors.LightGreen, fixture.Adapter.State.Highlight.Value);
            Assert.Equal(TextAlignment.Center, fixture.Adapter.State.Alignment.Value);
            Assert.Equal(24, fixture.Adapter.State.Indentation.Value);
            Assert.Equal(6, fixture.Adapter.State.SpacingBefore.Value);
            Assert.Equal(8, fixture.Adapter.State.SpacingAfter.Value);

            fixture.Adapter.ToggleUnderline();
            fixture.Adapter.ApplyForeground(null);
            fixture.Adapter.ApplyHighlight(null);
            Assert.False(fixture.Adapter.State.Underline.Value);
            Assert.Null(fixture.Adapter.State.Foreground.Value);
            Assert.Null(fixture.Adapter.State.Highlight.Value);
        });
    }

    [Fact]
    public void ClearFormattingPreservesParagraphStructureAndIsUndoable()
    {
        StaTestHelper.Run(() =>
        {
            var run = new Run("formatted")
            {
                FontFamily = new FontFamily("Arial"),
                FontSize = 30,
                FontWeight = FontWeights.Bold,
                FontStyle = FontStyles.Italic,
                TextDecorations = WriterFontEffects.CreateDecorations(
                    underline: true, WriterStrikethroughStyle.Double),
                BaselineAlignment = BaselineAlignment.Superscript,
                Foreground = Brushes.DarkRed,
                Background = Brushes.Gold
            };
            var paragraph = new Paragraph(run)
            {
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(24, 3, 12, 9)
            };
            using var fixture = CreateFixture(new FlowDocument(paragraph));
            fixture.Editor.Selection.Select(run.ContentStart, run.ContentEnd);

            Assert.True(fixture.Adapter.TryExecute(WriterEditingCommands.ClearFormatting));

            Assert.Equal(TextAlignment.Center, paragraph.TextAlignment);
            Assert.Equal(new Thickness(24, 3, 12, 9), paragraph.Margin);
            Assert.True(fixture.Adapter.State.Bold.IsUniform && !fixture.Adapter.State.Bold.Value);
            Assert.True(fixture.Adapter.State.Italic.IsUniform && !fixture.Adapter.State.Italic.Value);
            Assert.True(fixture.Adapter.State.Underline.IsUniform && !fixture.Adapter.State.Underline.Value);
            Assert.Equal(WriterStrikethroughStyle.None, fixture.Adapter.State.Strikethrough.Value);
            Assert.Equal(WriterBaselineEffect.Normal, fixture.Adapter.State.BaselineEffect.Value);
            Assert.Null(fixture.Adapter.State.Highlight.Value);

            fixture.Adapter.Undo();
            Assert.True(fixture.Adapter.State.Bold.Value);
            Assert.True(fixture.Adapter.State.Italic.Value);
            Assert.True(fixture.Adapter.State.Underline.Value);
            Assert.Equal(WriterStrikethroughStyle.Double, fixture.Adapter.State.Strikethrough.Value);
            Assert.Equal(WriterBaselineEffect.Superscript, fixture.Adapter.State.BaselineEffect.Value);
            Assert.Equal(Colors.Gold, fixture.Adapter.State.Highlight.Value);
        });
    }

    [Fact]
    public void FontDialogEffectsApplyAsOneUndoableSelectionChange()
    {
        StaTestHelper.Run(() =>
        {
            var run = new Run("effects");
            using var fixture = CreateFixture(new FlowDocument(new Paragraph(run)));
            fixture.Editor.Selection.Select(run.ContentStart, run.ContentEnd);

            fixture.Adapter.ApplyFontDialogValues(
                family: null,
                sizeInDips: null,
                style: null,
                weight: null,
                foreground: null,
                underline: true,
                strikethrough: WriterStrikethroughStyle.Double,
                baselineEffect: WriterBaselineEffect.Superscript);

            Assert.True(fixture.Adapter.State.Underline.IsUniform &&
                        fixture.Adapter.State.Underline.Value);
            Assert.Equal(WriterStrikethroughStyle.Double,
                fixture.Adapter.State.Strikethrough.Value);
            Assert.Equal(WriterBaselineEffect.Superscript,
                fixture.Adapter.State.BaselineEffect.Value);
            Assert.True(fixture.Editor.CanUndo);

            fixture.Adapter.Undo();
            Assert.Equal(WriterStrikethroughStyle.None,
                fixture.Adapter.State.Strikethrough.Value);
            Assert.Equal(WriterBaselineEffect.Normal,
                fixture.Adapter.State.BaselineEffect.Value);
        });
    }

    [Fact]
    public void PasteTextOnlyUsesPlainClipboardTextAsOneUndoableEdit()
    {
        StaTestHelper.Run(() =>
        {
            var originalClipboard = Clipboard.GetDataObject();
            try
            {
                Clipboard.Clear();
                Clipboard.SetText("plain paste");
                using var fixture = CreateFixture(new FlowDocument(new Paragraph(new Run("replace me"))));
                fixture.Editor.SelectAll();

                Assert.True(fixture.Adapter.CanExecute(WriterEditingCommands.PasteTextOnly));
                Assert.True(fixture.Adapter.TryExecute(WriterEditingCommands.PasteTextOnly));
                Assert.Contains("plain paste", new TextRange(fixture.Editor.Document.ContentStart,
                    fixture.Editor.Document.ContentEnd).Text);
                Assert.True(fixture.Editor.CanUndo);

                fixture.Adapter.Undo();
                Assert.Contains("replace me", new TextRange(fixture.Editor.Document.ContentStart,
                    fixture.Editor.Document.ContentEnd).Text);
            }
            finally
            {
                if (originalClipboard is null)
                    Clipboard.Clear();
                else
                    Clipboard.SetDataObject(originalClipboard, copy: true);
            }
        });
    }

    [Fact]
    public void ListCommandsAndIndentationPreserveParagraphBoundaries()
    {
        StaTestHelper.Run(() =>
        {
            var first = new Paragraph(new Run("one"));
            var second = new Paragraph(new Run("two"));
            using var fixture = CreateFixture(CreateDocument(first, second));
            fixture.Editor.SelectAll();

            Assert.True(fixture.Adapter.TryExecute(WriterEditingCommands.ToggleBullets));
            Assert.Equal(WriterListKind.Bulleted, fixture.Adapter.State.ListKind.Value);
            fixture.Adapter.IncreaseIndentation();
            Assert.True(fixture.Adapter.State.Indentation.Value > 0);
            Assert.True(fixture.Adapter.TryExecute(WriterEditingCommands.ToggleNumbering));
            Assert.Equal(WriterListKind.Numbered, fixture.Adapter.State.ListKind.Value);
        });
    }

    [Fact]
    public void ClipboardAndHistoryEnablementTracksNativeEditorState()
    {
        StaTestHelper.Run(() =>
        {
            try
            {
                Clipboard.Clear();
                using var fixture = CreateFixture(new FlowDocument(new Paragraph(new Run("clipboard"))));
                fixture.Editor.SelectAll();
                Assert.True(fixture.Adapter.State.CanCopy);
                Assert.True(fixture.Adapter.CanExecute(ApplicationCommands.Copy));
                fixture.Adapter.Copy();
                Assert.True(Clipboard.ContainsText());

                fixture.Adapter.Cut();
                Assert.True(fixture.Adapter.State.CanUndo);
                fixture.Adapter.Undo();
                Assert.Contains("clipboard", new TextRange(fixture.Editor.Document.ContentStart,
                    fixture.Editor.Document.ContentEnd).Text);
                Assert.True(fixture.Adapter.State.CanRedo);
                fixture.Adapter.Redo();
                Assert.DoesNotContain("clipboard", new TextRange(fixture.Editor.Document.ContentStart,
                    fixture.Editor.Document.ContentEnd).Text);
            }
            finally
            {
                Clipboard.Clear();
            }
        });
    }

    [Fact]
    public void ParagraphMutationIsOneUndoUnit()
    {
        StaTestHelper.Run(() =>
        {
            var first = new Paragraph(new Run("first"));
            var second = new Paragraph(new Run("second"));
            using var fixture = CreateFixture(CreateDocument(first, second));
            fixture.Editor.SelectAll();
            var originalFirst = first.Margin.Bottom;
            var originalSecond = second.Margin.Bottom;
            fixture.Adapter.SetParagraphSpacingAfter(17);
            Assert.Equal(17, first.Margin.Bottom);
            Assert.Equal(17, second.Margin.Bottom);

            fixture.Adapter.Undo();
            Assert.Equal(originalFirst, first.Margin.Bottom);
            Assert.Equal(originalSecond, second.Margin.Bottom);
        });
    }

    private static Fixture CreateFixture(FlowDocument document)
    {
        var editor = new RichTextBox { Document = document, IsUndoEnabled = true };
        var window = new Window { Content = editor, Width = 180, Height = 120, ShowInTaskbar = false };
        window.Show();
        editor.Focus();
        return new Fixture(window, editor, new WriterEditingAdapter(editor));
    }

    private static FlowDocument CreateDocument(params Paragraph[] paragraphs)
    {
        var document = new FlowDocument();
        foreach (var paragraph in paragraphs)
            document.Blocks.Add(paragraph);
        return document;
    }

    private sealed class Fixture(Window window, RichTextBox editor, WriterEditingAdapter adapter) : IDisposable
    {
        public RichTextBox Editor { get; } = editor;
        public WriterEditingAdapter Adapter { get; } = adapter;
        public void Dispose()
        {
            Adapter.Dispose();
            window.Close();
        }
    }
}
