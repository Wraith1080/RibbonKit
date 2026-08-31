using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using RibbonKit.Writer.Models;
using RibbonKit.Writer.Preview;
using RibbonKit.Writer.Tests.Document;
using Xunit;
using Xunit.Abstractions;

namespace RibbonKit.Writer.Tests.Pagination;

/// <summary>
/// Test-isolated W2-G prototype for projecting one native editor's caret and selection over an
/// independently paginated clone. This is deliberately not a production Paper-view control.
/// </summary>
public sealed class WriterCaretSelectionCompositorPrototypeTests(ITestOutputHelper output)
{
    [Fact]
    public void PageClickAndCrossPageDragUpdateTheLiveSelectionAndComposePageOverlays()
    {
        StaTestHelper.Run(() =>
        {
            using var workspace = PrototypeWorkspace.Create();
            workspace.Compositor.SetVisiblePage(0);
            workspace.Scheduler.RunLatest();

            var firstPagePoint = workspace.Compositor.GetPointNearPageEnd(0);
            var secondPagePoint = workspace.Compositor.GetPointNearPageStart(1);
            var expectedStart = workspace.Compositor.HitTest(0, firstPagePoint);
            var expectedEnd = workspace.Compositor.HitTest(1, secondPagePoint);

            workspace.Compositor.SetCaretFromPagePoint(0, firstPagePoint);
            Assert.True(workspace.Editor.Selection.IsEmpty);
            Assert.Equal(expectedStart, OffsetOf(workspace.Editor.Document,
                workspace.Editor.CaretPosition));

            workspace.Compositor.SelectFromPagePoints(0, firstPagePoint, 1, secondPagePoint);
            Assert.False(workspace.Editor.Selection.IsEmpty);
            Assert.Equal(expectedStart, OffsetOf(workspace.Editor.Document,
                workspace.Editor.Selection.Start));
            Assert.Equal(expectedEnd, OffsetOf(workspace.Editor.Document,
                workspace.Editor.Selection.End));

            var overlays = workspace.Compositor.ComposeOverlays();
            Assert.True(HasOverlay(overlays[0], "selection"));
            Assert.True(HasOverlay(overlays[1], "selection"));
            Assert.False(HasOverlay(overlays[0], "caret"));
            Assert.True(HasOverlay(overlays[1], "caret"));

            output.WriteLine($"Cross-page projection: offsets={expectedStart}-{expectedEnd}; " +
                $"pages={string.Join(',', overlays.Keys.Order())}; " +
                $"selectionRects={overlays.Values.Sum(CountSelectionOverlays)}");
        });
    }

    [Fact]
    public void NativeTypingDeletionUndoAndRedoRemainOwnedByTheLiveEditorAcrossCloneRefreshes()
    {
        StaTestHelper.Run(() =>
        {
            using var workspace = PrototypeWorkspace.Create();
            var authoritativeDocument = workspace.Editor.Document;
            workspace.Compositor.SetVisiblePage(0);
            workspace.Scheduler.RunLatest();
            var originalText = DocumentText(authoritativeDocument);

            var startPoint = workspace.Compositor.GetPointNearPageEnd(0);
            var endPoint = workspace.Compositor.GetPointNearPageStart(1);
            workspace.Compositor.SelectFromPagePoints(0, startPoint, 1, endPoint);
            workspace.Editor.Selection.Text = "cross-page native typing";
            var typedText = DocumentText(authoritativeDocument);
            var typedEndOffset = OffsetOf(authoritativeDocument,
                workspace.Editor.CaretPosition);
            var typedStartOffset = typedEndOffset - "cross-page native typing".Length;
            Assert.NotEqual(originalText, typedText);
            Assert.True(workspace.Editor.CanUndo);
            workspace.Scheduler.RunLatestIgnoringCanceled();
            AssertCurrentClone(workspace, authoritativeDocument, typedText);

            SelectSymbolOffsets(workspace.Editor, typedStartOffset,
                typedStartOffset + "cross-page".Length);
            EditingCommands.Delete.Execute(null, workspace.Editor);
            var deletedText = DocumentText(authoritativeDocument);
            Assert.NotEqual(typedText, deletedText);
            workspace.Scheduler.RunLatestIgnoringCanceled();
            AssertCurrentClone(workspace, authoritativeDocument, deletedText);

            workspace.Editor.Undo();
            Assert.Equal(typedText, DocumentText(authoritativeDocument));
            workspace.Scheduler.RunLatestIgnoringCanceled();
            AssertCurrentClone(workspace, authoritativeDocument, typedText);

            workspace.Editor.Undo();
            Assert.Equal(originalText, DocumentText(authoritativeDocument));
            Assert.True(workspace.Editor.CanRedo);
            workspace.Scheduler.RunLatestIgnoringCanceled();
            AssertCurrentClone(workspace, authoritativeDocument, originalText);

            workspace.Editor.Redo();
            Assert.Equal(typedText, DocumentText(authoritativeDocument));
            workspace.Scheduler.RunLatestIgnoringCanceled();
            AssertCurrentClone(workspace, authoritativeDocument, typedText);

            workspace.Editor.Redo();
            Assert.Equal(deletedText, DocumentText(authoritativeDocument));
            workspace.Scheduler.RunLatestIgnoringCanceled();
            AssertCurrentClone(workspace, authoritativeDocument, deletedText);

            Assert.Same(authoritativeDocument, workspace.Editor.Document);
            Assert.True(workspace.Compositor.PublishedGeneration > 0);
            output.WriteLine($"Native history projection: generation={workspace.Compositor.PublishedGeneration}; " +
                $"lastSnapshot={workspace.Compositor.LastSnapshotMilliseconds:0.###} ms; " +
                $"lastVisibleMap={workspace.Compositor.LastMapMilliseconds:0.###} ms");
        });
    }

    [Fact]
    public void DebounceRejectsStaleCallbacksAndMapsOnlyTheVisibleAndAdjacentPages()
    {
        StaTestHelper.Run(() =>
        {
            using var workspace = PrototypeWorkspace.Create();
            workspace.Compositor.SetVisiblePage(0);
            workspace.Compositor.SetVisiblePage(2);
            Assert.Equal(1, workspace.Scheduler.PendingCount);
            Assert.True(workspace.Scheduler.CanceledCount >= 1);

            workspace.Scheduler.RunOldestIgnoringCancellation();
            Assert.Equal(0, workspace.Compositor.PublishedGeneration);
            workspace.Scheduler.RunLatestIgnoringCanceled();

            Assert.Equal(workspace.Compositor.RequestedGeneration,
                workspace.Compositor.PublishedGeneration);
            Assert.Equal(new[] { 1, 2, 3 }, workspace.Compositor.MappedPages);
            Assert.True(workspace.Compositor.PageCount > workspace.Compositor.MappedPages.Count);

            var published = workspace.Compositor.PublishedGeneration;
            workspace.Editor.AppendText(" first queued edit");
            workspace.Editor.AppendText(" second queued edit");
            workspace.Scheduler.RunOldestIgnoringCancellation();
            Assert.Equal(published, workspace.Compositor.PublishedGeneration);
            workspace.Scheduler.RunLatestIgnoringCanceled();
            Assert.Equal(workspace.Compositor.RequestedGeneration,
                workspace.Compositor.PublishedGeneration);
            Assert.Contains("second queued edit", workspace.Compositor.CloneText);

            output.WriteLine($"Bounded map: totalPages={workspace.Compositor.PageCount}; " +
                $"mapped={string.Join(',', workspace.Compositor.MappedPages.Select(page => page + 1))}; " +
                $"snapshot={workspace.Compositor.LastSnapshotMilliseconds:0.###} ms; " +
                $"map={workspace.Compositor.LastMapMilliseconds:0.###} ms; " +
                $"entries={workspace.Compositor.MappedEntryCount}");
        });
    }

    [Fact]
    public void PageInteractionRestoresTheLiveCommandTargetForNativeEditingHistoryAndSelection()
    {
        StaTestHelper.Run(() =>
        {
            using var workspace = PrototypeWorkspace.Create();
            workspace.Compositor.SetVisiblePage(0);
            workspace.Scheduler.RunLatest();
            var anchor = workspace.Compositor.CapturePageEndInteraction(0);
            var moving = workspace.Compositor.CapturePageStartInteraction(1);

            FocusManager.SetFocusedElement(workspace.Window, workspace.Viewer);
            workspace.Viewer.Focus();
            Keyboard.Focus(workspace.Viewer);
            Assert.NotSame(workspace.Editor,
                FocusManager.GetFocusedElement(workspace.Window));

            Assert.True(workspace.Compositor.TryApplySelection(anchor, moving));
            AssertLiveEditorFocus(workspace);
            Assert.False(workspace.Editor.Selection.IsEmpty);
            Assert.False(EditingCommands.Delete.CanExecute(null, workspace.Viewer));
            Assert.False(ApplicationCommands.Undo.CanExecute(null, workspace.Viewer));

            workspace.Editor.Selection.Text = "ABCDE";
            var typedText = DocumentText(workspace.Editor.Document);
            var typedEndOffset = OffsetOf(workspace.Editor.Document,
                workspace.Editor.CaretPosition);
            var typedStartOffset = typedEndOffset - 5;
            SelectSymbolOffsets(workspace.Editor, typedStartOffset + 1,
                typedStartOffset + 1);
            Assert.True(EditingCommands.Backspace.CanExecute(null, workspace.Editor));
            EditingCommands.Backspace.Execute(null, workspace.Editor);
            var backspacedText = DocumentText(workspace.Editor.Document);
            Assert.NotEqual(typedText, backspacedText);

            Assert.True(EditingCommands.Delete.CanExecute(null, workspace.Editor));
            EditingCommands.Delete.Execute(null, workspace.Editor);
            var deletedText = DocumentText(workspace.Editor.Document);
            Assert.NotEqual(backspacedText, deletedText);

            Assert.True(ApplicationCommands.Undo.CanExecute(null, workspace.Editor));
            ApplicationCommands.Undo.Execute(null, workspace.Editor);
            Assert.Equal(backspacedText, DocumentText(workspace.Editor.Document));
            ApplicationCommands.Undo.Execute(null, workspace.Editor);
            Assert.Equal(typedText, DocumentText(workspace.Editor.Document));
            Assert.True(ApplicationCommands.Redo.CanExecute(null, workspace.Editor));
            ApplicationCommands.Redo.Execute(null, workspace.Editor);
            Assert.Equal(backspacedText, DocumentText(workspace.Editor.Document));
            ApplicationCommands.Redo.Execute(null, workspace.Editor);
            Assert.Equal(deletedText, DocumentText(workspace.Editor.Document));

            Assert.True(ApplicationCommands.SelectAll.CanExecute(null, workspace.Editor));
            ApplicationCommands.SelectAll.Execute(null, workspace.Editor);
            Assert.Equal(DocumentText(workspace.Editor.Document),
                workspace.Editor.Selection.Text);
            AssertLiveEditorFocus(workspace);

            workspace.Scheduler.RunLatestIgnoringCanceled();
            Assert.Equal(deletedText, workspace.Compositor.CloneText);
            output.WriteLine($"Native target bridge: generation={workspace.Compositor.PublishedGeneration}; " +
                "typing/backspace/delete/undo/redo/select-all remained on the live editor.");
        });
    }

    [Fact]
    public void AsynchronousPageSwapPreservesLiveFocusAndRejectsTheOldPageEvent()
    {
        StaTestHelper.Run(() =>
        {
            using var workspace = PrototypeWorkspace.Create();
            workspace.Compositor.SetVisiblePage(0);
            workspace.Scheduler.RunLatest();
            var staleEvent = workspace.Compositor.CapturePageEndInteraction(0);
            Assert.True(workspace.Compositor.TryApplyCaret(staleEvent));
            AssertLiveEditorFocus(workspace);

            workspace.Editor.Selection.Text = "new generation";
            var requestedGeneration = workspace.Compositor.RequestedGeneration;
            workspace.Scheduler.RunLatestIgnoringCanceled();
            Assert.Equal(requestedGeneration, workspace.Compositor.PublishedGeneration);
            AssertLiveEditorFocus(workspace);

            var currentEvent = workspace.Compositor.CapturePageStartInteraction(1);
            Assert.True(workspace.Compositor.TryApplyCaret(currentEvent));
            var currentCaret = OffsetOf(workspace.Editor.Document,
                workspace.Editor.CaretPosition);
            Assert.False(workspace.Compositor.TryApplyCaret(staleEvent));
            Assert.Equal(currentCaret, OffsetOf(workspace.Editor.Document,
                workspace.Editor.CaretPosition));

            output.WriteLine($"Stale page event rejected: old={staleEvent.Generation}; " +
                $"current={workspace.Compositor.PublishedGeneration}; caret={currentCaret}.");
        });
    }

    [Fact]
    public void PageSettingReflowPreservesLiveRangeFocusAndRejectsPreReflowPageEvents()
    {
        StaTestHelper.Run(() =>
        {
            using var workspace = PrototypeWorkspace.Create();
            var authoritativeDocument = workspace.Editor.Document;
            workspace.Compositor.SetVisiblePage(0);
            workspace.Scheduler.RunLatest();
            var anchorEvent = workspace.Compositor.CapturePageEndInteraction(0);
            var movingEvent = workspace.Compositor.CapturePageStartInteraction(1);
            Assert.True(workspace.Compositor.TryApplySelection(anchorEvent, movingEvent));
            var anchorOffset = OffsetOf(authoritativeDocument,
                workspace.Editor.Selection.Start);
            var movingOffset = OffsetOf(authoritativeDocument,
                workspace.Editor.Selection.End);
            AssertLiveEditorFocus(workspace);

            var reflowSettings = DocumentPageSettings.A4(
                DocumentPageOrientation.Landscape,
                new DocumentPageMargins(48, 72, 60, 84));
            workspace.Compositor.SetPageSettings(reflowSettings);
            workspace.Scheduler.RunLatestIgnoringCanceled();

            Assert.Same(authoritativeDocument, workspace.Editor.Document);
            Assert.Equal(reflowSettings, workspace.Compositor.PageSettings);
            Assert.Equal(reflowSettings.WidthDip,
                workspace.Compositor.CloneDocument.PageWidth, 4);
            Assert.Equal(reflowSettings.HeightDip,
                workspace.Compositor.CloneDocument.PageHeight, 4);
            Assert.Equal(anchorOffset, OffsetOf(authoritativeDocument,
                workspace.Editor.Selection.Start));
            Assert.Equal(movingOffset, OffsetOf(authoritativeDocument,
                workspace.Editor.Selection.End));
            AssertLiveEditorFocus(workspace);

            Assert.False(workspace.Compositor.TryApplyCaret(anchorEvent));
            Assert.Equal(anchorOffset, OffsetOf(authoritativeDocument,
                workspace.Editor.Selection.Start));
            Assert.Equal(movingOffset, OffsetOf(authoritativeDocument,
                workspace.Editor.Selection.End));

            output.WriteLine($"Compositor reflow: generation={workspace.Compositor.PublishedGeneration}; " +
                $"A4 landscape={reflowSettings.WidthDip:0.###}x{reflowSettings.HeightDip:0.###}; " +
                $"anchors={anchorOffset}-{movingOffset}.");
        });
    }

    [Fact]
    public void DocumentReplacementImmediatelyRejectsQueuedPageAndStructuredObjectEvents()
    {
        StaTestHelper.Run(() =>
        {
            var settings = DocumentPageSettings.Letter();
            var opening = CreateParagraphDocument(settings, 180);
            var firstParagraph = Assert.IsType<Paragraph>(opening.Blocks.FirstBlock);
            var hyperlink = new Hyperlink(new Run(" authoritative object"))
            {
                NavigateUri = new Uri("https://example.invalid/w2-g-lifecycle")
            };
            firstParagraph.Inlines.Add(hyperlink);
            using var workspace = PrototypeWorkspace.Create(opening);
            workspace.Compositor.SetVisiblePage(0);
            workspace.Scheduler.RunLatest();
            var stalePageEvent = workspace.Compositor.CapturePageStartInteraction(0);
            var staleObjectEvent = workspace.Compositor.CaptureStructuredObjectInteraction(
                hyperlink, 0);
            Assert.True(workspace.Compositor.TryApplyStructuredObjectInteraction(
                staleObjectEvent));

            workspace.Editor.AppendText(" queued old-document edit");
            var replacement = CreateParagraphDocument(settings, 60);
            workspace.Compositor.ReplaceDocument(replacement);

            Assert.Same(replacement, workspace.Editor.Document);
            Assert.False(workspace.Compositor.HasPublishedGeometry);
            Assert.False(workspace.Compositor.TryApplyCaret(stalePageEvent));
            Assert.False(workspace.Compositor.TryApplyStructuredObjectInteraction(
                staleObjectEvent));
            AssertLiveEditorFocus(workspace);

            workspace.Scheduler.RunOldestIgnoringCancellation();
            Assert.False(workspace.Compositor.HasPublishedGeometry);
            Assert.False(workspace.Compositor.TryApplyCaret(stalePageEvent));
            workspace.Scheduler.RunLatestIgnoringCanceled();

            Assert.True(workspace.Compositor.HasPublishedGeometry);
            Assert.Equal(workspace.Compositor.RequestedGeneration,
                workspace.Compositor.PublishedGeneration);
            Assert.Same(replacement, workspace.Editor.Document);
            Assert.Equal(DocumentText(replacement), workspace.Compositor.CloneText);
            Assert.DoesNotContain("queued old-document edit", workspace.Compositor.CloneText);
            Assert.False(workspace.Compositor.TryApplyStructuredObjectInteraction(
                staleObjectEvent));
            AssertLiveEditorFocus(workspace);

            output.WriteLine($"Document replacement: generation=" +
                $"{workspace.Compositor.PublishedGeneration}; pages=" +
                $"{workspace.Compositor.PageCount}; mapped=" +
                $"{string.Join(',', workspace.Compositor.MappedPages.Select(page => page + 1))}; " +
                "queued old callback and object event rejected.");
        });
    }

    [Fact]
    public void PageWindowHandoffEvictsOldGeometryAndCurrentInteractionRestoresLiveFocus()
    {
        StaTestHelper.Run(() =>
        {
            using var workspace = PrototypeWorkspace.Create();
            workspace.Compositor.SetVisiblePage(0);
            workspace.Scheduler.RunLatest();
            var oldEvent = workspace.Compositor.CapturePageStartInteraction(0);
            var lastPage = workspace.Compositor.PageCount - 1;
            Assert.True(lastPage > 2);

            FocusManager.SetFocusedElement(workspace.Window, workspace.Viewer);
            workspace.Viewer.Focus();
            Keyboard.Focus(workspace.Viewer);
            workspace.Compositor.SetVisiblePage(lastPage);
            Assert.False(workspace.Compositor.TryApplyCaret(oldEvent));
            workspace.Scheduler.RunLatestIgnoringCanceled();

            Assert.DoesNotContain(0, workspace.Compositor.MappedPages);
            Assert.Equal(new[] { lastPage - 1, lastPage },
                workspace.Compositor.MappedPages);
            Assert.InRange(workspace.Compositor.MappedPages.Count, 1, 3);
            Assert.False(workspace.Compositor.TryApplyCaret(oldEvent));
            var currentEvent = workspace.Compositor.CapturePageEndInteraction(lastPage);
            Assert.True(workspace.Compositor.TryApplyCaret(currentEvent));
            AssertLiveEditorFocus(workspace);
            Assert.True(ApplicationCommands.SelectAll.CanExecute(null, workspace.Editor));

            output.WriteLine($"Page-window handoff: total={workspace.Compositor.PageCount}; " +
                $"mapped={string.Join(',', workspace.Compositor.MappedPages.Select(page => page + 1))}; " +
                "page-one geometry evicted and live command focus restored.");
        });
    }

    [Fact]
    public void NativeSpellingRangeProjectsToPagesAndCorrectionHistorySurvivesReflow()
    {
        StaTestHelper.Run(() =>
        {
            using var workspace = PrototypeWorkspace.Create();
            workspace.Editor.Language = XmlLanguage.GetLanguage("en-US");
            SpellCheck.SetIsEnabled(workspace.Editor, true);
            workspace.Compositor.SetVisiblePage(1);
            workspace.Scheduler.RunLatest();

            const string misspelling = " qzxwvv ";
            var paragraph = workspace.Editor.Document.Blocks.OfType<Paragraph>().ElementAt(45);
            var insertion = paragraph.ContentStart.GetInsertionPosition(LogicalDirection.Forward)!;
            new TextRange(insertion, insertion).Text = misspelling;
            DrainSpelling(workspace.Window);
            var errorRange = FindSpellingRange(workspace.Editor, "qzxwvv");
            Assert.NotNull(errorRange);
            var errorPosition = errorRange!.Start.GetPositionAtOffset(1,
                LogicalDirection.Forward)!;
            var error = workspace.Editor.GetSpellingError(errorPosition);
            Assert.NotNull(error);
            workspace.Scheduler.RunLatestIgnoringCanceled();

            var spellingToken = workspace.Compositor.CaptureSpellingRange(errorRange);
            var overlays = workspace.Compositor.TryComposeSpellingOverlays(spellingToken);
            Assert.NotNull(overlays);
            Assert.Contains(overlays!.Values.SelectMany(canvas =>
                canvas.Children.OfType<Rectangle>()), element =>
                    Equals(element.Tag, "spelling"));

            error!.Correct("spelling");
            Assert.DoesNotContain("qzxwvv", DocumentText(workspace.Editor.Document));
            Assert.True(workspace.Editor.CanUndo);
            workspace.Editor.Undo();
            Assert.Contains("qzxwvv", DocumentText(workspace.Editor.Document));
            workspace.Editor.Redo();
            Assert.DoesNotContain("qzxwvv", DocumentText(workspace.Editor.Document));

            workspace.Compositor.SetPageSettings(DocumentPageSettings.A4(
                DocumentPageOrientation.Landscape,
                new DocumentPageMargins(48, 72, 60, 84)));
            workspace.Scheduler.RunLatestIgnoringCanceled();
            Assert.DoesNotContain("qzxwvv", workspace.Compositor.CloneText);
            Assert.Null(workspace.Compositor.TryComposeSpellingOverlays(spellingToken));

            output.WriteLine($"Native spelling: range={spellingToken.StartOffset}-" +
                $"{spellingToken.EndOffset}; oldGeneration={spellingToken.Generation}; " +
                $"reflowGeneration={workspace.Compositor.PublishedGeneration}.");
        });
    }

    [Fact]
    public void WpfTextCompositionEventsStayOwnedByTheFocusedLiveEditorAcrossPageSwap()
    {
        StaTestHelper.Run(() =>
        {
            using var workspace = PrototypeWorkspace.Create();
            workspace.Compositor.SetVisiblePage(0);
            workspace.Scheduler.RunLatest();
            var pageEvent = workspace.Compositor.CapturePageStartInteraction(1);
            Assert.True(workspace.Compositor.TryApplyCaret(pageEvent));
            AssertLiveEditorFocus(workspace);

            var starts = 0;
            var completions = 0;
            IInputElement? startSource = null;
            IInputElement? completionSource = null;
            TextCompositionEventHandler startHandler = (_, args) =>
            {
                starts++;
                startSource = args.OriginalSource as IInputElement;
            };
            TextCompositionEventHandler completionHandler = (_, args) =>
            {
                completions++;
                completionSource = args.OriginalSource as IInputElement;
            };
            TextCompositionManager.AddPreviewTextInputStartHandler(
                workspace.Editor, startHandler);
            TextCompositionManager.AddPreviewTextInputHandler(
                workspace.Editor, completionHandler);
            try
            {
                var composition = new TextComposition(InputManager.Current,
                    workspace.Editor, "composition");
                Assert.True(TextCompositionManager.StartComposition(composition));
                workspace.Window.Dispatcher.Invoke(DispatcherPriority.Input,
                    new Action(() => { }));
            }
            finally
            {
                TextCompositionManager.RemovePreviewTextInputStartHandler(
                    workspace.Editor, startHandler);
                TextCompositionManager.RemovePreviewTextInputHandler(
                    workspace.Editor, completionHandler);
            }

            Assert.Equal(1, starts);
            Assert.Equal(1, completions);
            Assert.Same(workspace.Editor, startSource);
            Assert.Same(workspace.Editor, completionSource);
            AssertLiveEditorFocus(workspace);

            workspace.Compositor.SetPageSettings(DocumentPageSettings.A4());
            workspace.Scheduler.RunLatestIgnoringCanceled();
            AssertLiveEditorFocus(workspace);
            Assert.False(workspace.Compositor.TryApplyCaret(pageEvent));

            output.WriteLine("Synthetic WPF composition start/complete routed only to the live editor; " +
                "OS IME candidate-window behavior remains a manual gate.");
        });
    }

    [Fact]
    public void NativeClipboardCopyCutPasteAndHistoryRemainOnTheLiveEditor()
    {
        StaTestHelper.Run(() =>
        {
            var originalClipboard = Clipboard.GetDataObject();
            try
            {
                using var workspace = PrototypeWorkspace.Create();
                workspace.Compositor.SetVisiblePage(0);
                workspace.Scheduler.RunLatest();
                var anchor = workspace.Compositor.CapturePageEndInteraction(0);
                var moving = workspace.Compositor.CapturePageStartInteraction(1);
                Assert.True(workspace.Compositor.TryApplySelection(anchor, moving));
                var copiedText = workspace.Editor.Selection.Text;
                Assert.NotEmpty(copiedText);

                Assert.True(ApplicationCommands.Copy.CanExecute(null, workspace.Editor));
                ApplicationCommands.Copy.Execute(null, workspace.Editor);
                Assert.True(Clipboard.ContainsText());
                Assert.Equal(copiedText, Clipboard.GetText());

                var beforeCut = DocumentText(workspace.Editor.Document);
                Assert.True(ApplicationCommands.Cut.CanExecute(null, workspace.Editor));
                ApplicationCommands.Cut.Execute(null, workspace.Editor);
                var afterCut = DocumentText(workspace.Editor.Document);
                Assert.NotEqual(beforeCut, afterCut);
                ApplicationCommands.Undo.Execute(null, workspace.Editor);
                Assert.Equal(beforeCut, DocumentText(workspace.Editor.Document));
                ApplicationCommands.Redo.Execute(null, workspace.Editor);
                Assert.Equal(afterCut, DocumentText(workspace.Editor.Document));

                var end = workspace.Editor.Document.ContentEnd.GetInsertionPosition(
                    LogicalDirection.Backward)!;
                workspace.Editor.Selection.Select(end, end);
                Assert.True(ApplicationCommands.Paste.CanExecute(null, workspace.Editor));
                ApplicationCommands.Paste.Execute(null, workspace.Editor);
                Assert.Contains(copiedText.Trim(), DocumentText(workspace.Editor.Document));
                ApplicationCommands.Undo.Execute(null, workspace.Editor);
                Assert.Equal(afterCut, DocumentText(workspace.Editor.Document));

                workspace.Scheduler.RunLatestIgnoringCanceled();
                Assert.Equal(afterCut, workspace.Compositor.CloneText);
                AssertLiveEditorFocus(workspace);
                output.WriteLine($"Native clipboard: copied={copiedText.Length} chars; " +
                    $"publishedGeneration={workspace.Compositor.PublishedGeneration}.");
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

    private static void AssertCurrentClone(PrototypeWorkspace workspace,
        FlowDocument authoritativeDocument, string expectedText)
    {
        Assert.Same(authoritativeDocument, workspace.Editor.Document);
        Assert.NotSame(authoritativeDocument, workspace.Compositor.CloneDocument);
        Assert.Equal(expectedText, workspace.Compositor.CloneText);
        Assert.Equal(workspace.Compositor.RequestedGeneration,
            workspace.Compositor.PublishedGeneration);
    }

    private static void AssertLiveEditorFocus(PrototypeWorkspace workspace)
    {
        var logicalFocus = FocusManager.GetFocusedElement(workspace.Window);
        Assert.True(workspace.Editor.IsKeyboardFocusWithin ||
            ReferenceEquals(logicalFocus, workspace.Editor),
            "The page interaction must leave the live editor as keyboard or logical focus owner.");
    }

    private static void DrainSpelling(Window window)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            window.Dispatcher.Invoke(DispatcherPriority.ApplicationIdle,
                new Action(() => { }));
            window.UpdateLayout();
        }
    }

    private static TextRange? FindSpellingRange(RichTextBox editor, string expectedText)
    {
        for (var position = editor.GetNextSpellingErrorPosition(
                 editor.Document.ContentStart, LogicalDirection.Forward);
             position is not null;
             position = editor.GetNextSpellingErrorPosition(position,
                 LogicalDirection.Forward))
        {
            var range = editor.GetSpellingErrorRange(position);
            if (range is not null && string.Equals(range.Text, expectedText,
                    StringComparison.OrdinalIgnoreCase))
                return range;
            var next = range?.End.GetNextInsertionPosition(LogicalDirection.Forward);
            if (next is null)
                break;
            position = next;
        }
        return null;
    }

    private static bool HasOverlay(Canvas canvas, string tag) =>
        canvas.Children.OfType<Rectangle>().Any(element => Equals(element.Tag, tag));

    private static int CountSelectionOverlays(Canvas canvas) =>
        canvas.Children.OfType<Rectangle>().Count(element => Equals(element.Tag, "selection"));

    private static int OffsetOf(FlowDocument document, TextPointer position) =>
        document.ContentStart.GetOffsetToPosition(position);

    private static string DocumentText(FlowDocument document) =>
        new TextRange(document.ContentStart, document.ContentEnd).Text;

    private static void SelectSymbolOffsets(RichTextBox editor, int startOffset, int endOffset)
    {
        var start = editor.Document.ContentStart.GetPositionAtOffset(startOffset,
            LogicalDirection.Forward)!;
        var end = editor.Document.ContentStart.GetPositionAtOffset(endOffset,
            LogicalDirection.Forward)!;
        editor.Selection.Select(start, end);
    }

    private sealed class PrototypeWorkspace : IDisposable
    {
        private readonly Window _window;

        private PrototypeWorkspace(RichTextBox editor, FlowDocumentPageViewer viewer,
            ManualScheduler scheduler, ParagraphPageCompositor compositor, Window window)
        {
            Editor = editor;
            Viewer = viewer;
            Scheduler = scheduler;
            Compositor = compositor;
            _window = window;
        }

        public RichTextBox Editor { get; }
        public FlowDocumentPageViewer Viewer { get; }
        public Window Window => _window;
        public ManualScheduler Scheduler { get; }
        public ParagraphPageCompositor Compositor { get; }

        public static PrototypeWorkspace Create(FlowDocument? document = null)
        {
            var settings = DocumentPageSettings.Letter();
            document ??= CreateParagraphDocument(settings, 180);
            var editor = new RichTextBox
            {
                Document = document,
                IsUndoEnabled = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            var viewer = new FlowDocumentPageViewer();
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            Grid.SetColumn(editor, 0);
            Grid.SetColumn(viewer, 1);
            grid.Children.Add(editor);
            grid.Children.Add(viewer);
            var window = new Window
            {
                Content = grid,
                Width = settings.WidthDip * 2 + 200,
                Height = 800,
                WindowStartupLocation = WindowStartupLocation.Manual,
                Left = -10000,
                Top = -10000,
                ShowInTaskbar = false,
                Opacity = 0.01
            };
            window.Show();
            UpdateLayout(window);
            ApplyPageSettings(document, settings);
            UpdateLayout(window);

            var scheduler = new ManualScheduler();
            var compositor = new ParagraphPageCompositor(editor, viewer, window, settings,
                scheduler);
            return new PrototypeWorkspace(editor, viewer, scheduler, compositor, window);
        }

        public void Dispose()
        {
            Compositor.Dispose();
            if (_window.IsVisible)
                _window.Close();
        }
    }

    private sealed class ParagraphPageCompositor : IDisposable
    {
        private readonly RichTextBox _editor;
        private readonly FlowDocumentPageViewer _viewer;
        private readonly Window _host;
        private DocumentPageSettings _settings;
        private readonly ManualScheduler _scheduler;
        private ScheduledRebuild? _pending;
        private WriterPreviewSnapshot? _snapshot;
        private VisiblePageGeometryMap? _map;
        private long _nextIdentity;
        private int _visiblePage;
        private bool _disposed;

        public ParagraphPageCompositor(RichTextBox editor, FlowDocumentPageViewer viewer,
            Window host, DocumentPageSettings settings, ManualScheduler scheduler)
        {
            _editor = editor;
            _viewer = viewer;
            _host = host;
            _settings = settings;
            _scheduler = scheduler;
            _editor.TextChanged += OnTextChanged;
        }

        public long RequestedGeneration { get; private set; }
        public long PublishedGeneration { get; private set; }
        public int PageCount => RequireMap().PageCount;
        public IReadOnlyList<int> MappedPages => RequireMap().MappedPages;
        public int MappedEntryCount => RequireMap().EntryCount;
        public bool HasPublishedGeometry => _map is not null;
        public double LastSnapshotMilliseconds { get; private set; }
        public double LastMapMilliseconds { get; private set; }
        public DocumentPageSettings PageSettings => _settings;
        public FlowDocument CloneDocument => _snapshot?.SourceClone ??
            throw new InvalidOperationException("No clone has been published.");
        public string CloneText => DocumentText(CloneDocument);

        public void SetVisiblePage(int pageNumber)
        {
            _visiblePage = pageNumber;
            RequestRebuild();
        }

        public void SetPageSettings(DocumentPageSettings settings)
        {
            ArgumentNullException.ThrowIfNull(settings);
            if (settings == _settings)
                return;
            _settings = settings;
            RequestRebuild();
        }

        public void ReplaceDocument(FlowDocument replacement)
        {
            ArgumentNullException.ThrowIfNull(replacement);
            if (ReferenceEquals(replacement, _editor.Document))
                return;
            var restoreEditorFocus = HasLiveEditorFocus();
            ApplyPageSettings(replacement, _settings);
            _editor.TextChanged -= OnTextChanged;
            try
            {
                _editor.Document = replacement;
            }
            finally
            {
                _editor.TextChanged += OnTextChanged;
            }
            _map = null;
            RequestRebuild();
            if (restoreEditorFocus)
                RestoreLiveEditorFocus();
        }

        public Point GetPointNearPageEnd(int pageNumber) =>
            RequireMap().GetPointNearPageEnd(pageNumber);

        public Point GetPointNearPageStart(int pageNumber) =>
            RequireMap().GetPointNearPageStart(pageNumber);

        public int HitTest(int pageNumber, Point point) =>
            RequireMap().HitTest(pageNumber, point).SourceOffset;

        public PageInteraction CapturePageEndInteraction(int pageNumber) =>
            new(PublishedGeneration, pageNumber, GetPointNearPageEnd(pageNumber));

        public PageInteraction CapturePageStartInteraction(int pageNumber) =>
            new(PublishedGeneration, pageNumber, GetPointNearPageStart(pageNumber));

        public StructuredObjectInteraction CaptureStructuredObjectInteraction(
            TextElement element, int pageNumber)
        {
            ArgumentNullException.ThrowIfNull(element);
            var sourceOffset = OffsetOf(_editor.Document, element.ElementStart);
            return new StructuredObjectInteraction(_editor.Document, element, sourceOffset,
                CapturePageStartInteraction(pageNumber));
        }

        public bool TryApplyStructuredObjectInteraction(
            StructuredObjectInteraction interaction)
        {
            if (!ReferenceEquals(interaction.Document, _editor.Document) ||
                interaction.Element.Parent is null || !IsCurrent(interaction.PageInteraction))
                return false;
            if (OffsetOf(_editor.Document, interaction.Element.ElementStart) !=
                interaction.SourceOffset)
                return false;
            RestoreLiveEditorFocus();
            return true;
        }

        public bool TryApplyCaret(PageInteraction interaction)
        {
            if (!IsCurrent(interaction))
                return false;
            var position = GetLivePosition(HitTest(interaction.PageNumber, interaction.Point));
            _editor.Selection.Select(position, position);
            RestoreLiveEditorFocus();
            return true;
        }

        public bool TryApplySelection(PageInteraction anchor, PageInteraction moving)
        {
            if (!IsCurrent(anchor) || !IsCurrent(moving))
                return false;
            var anchorPosition = GetLivePosition(HitTest(anchor.PageNumber, anchor.Point));
            var movingPosition = GetLivePosition(HitTest(moving.PageNumber, moving.Point));
            _editor.Selection.Select(anchorPosition, movingPosition);
            RestoreLiveEditorFocus();
            return true;
        }

        public SpellingRangeToken CaptureSpellingRange(TextRange range)
        {
            ArgumentNullException.ThrowIfNull(range);
            return new SpellingRangeToken(PublishedGeneration,
                OffsetOf(_editor.Document, range.Start),
                OffsetOf(_editor.Document, range.End));
        }

        public IReadOnlyDictionary<int, Canvas>? TryComposeSpellingOverlays(
            SpellingRangeToken token)
        {
            if (_map is null || token.Generation != PublishedGeneration)
                return null;
            var pages = _map.MappedPages.ToDictionary(page => page,
                _ => new Canvas { Width = _settings.WidthDip, Height = _settings.HeightDip });
            var projected = false;
            foreach (var fragment in _map.ProjectSelection(token.StartOffset, token.EndOffset))
            {
                var underline = new Rectangle
                {
                    Tag = "spelling",
                    Width = Math.Max(2, fragment.Rect.Width),
                    Height = 1,
                    Fill = Brushes.Red
                };
                Canvas.SetLeft(underline, fragment.Rect.Left);
                Canvas.SetTop(underline, Math.Max(fragment.Rect.Top,
                    fragment.Rect.Bottom - 1));
                pages[fragment.PageNumber].Children.Add(underline);
                projected = true;
            }
            return projected ? pages : null;
        }

        public void SetCaretFromPagePoint(int pageNumber, Point point)
        {
            var position = GetLivePosition(HitTest(pageNumber, point));
            _editor.Selection.Select(position, position);
        }

        public void SelectFromPagePoints(int anchorPage, Point anchorPoint,
            int movingPage, Point movingPoint)
        {
            var anchor = GetLivePosition(HitTest(anchorPage, anchorPoint));
            var moving = GetLivePosition(HitTest(movingPage, movingPoint));
            _editor.Selection.Select(anchor, moving);
        }

        public IReadOnlyDictionary<int, Canvas> ComposeOverlays()
        {
            var map = RequireMap();
            var startOffset = OffsetOf(_editor.Document, _editor.Selection.Start);
            var endOffset = OffsetOf(_editor.Document, _editor.Selection.End);
            var caretOffset = OffsetOf(_editor.Document, _editor.CaretPosition);
            var pages = map.MappedPages.ToDictionary(page => page,
                _ => new Canvas { Width = _settings.WidthDip, Height = _settings.HeightDip });

            if (startOffset != endOffset)
            {
                foreach (var fragment in map.ProjectSelection(startOffset, endOffset))
                {
                    var rectangle = new Rectangle
                    {
                        Tag = "selection",
                        Width = Math.Max(2, fragment.Rect.Width),
                        Height = Math.Max(1, fragment.Rect.Height),
                        Fill = new SolidColorBrush(Color.FromArgb(80, 0, 120, 215))
                    };
                    Canvas.SetLeft(rectangle, fragment.Rect.Left);
                    Canvas.SetTop(rectangle, fragment.Rect.Top);
                    pages[fragment.PageNumber].Children.Add(rectangle);
                }
            }

            var caret = map.GetNearestByOffset(caretOffset);
            if (caret is not null)
            {
                var rectangle = new Rectangle
                {
                    Tag = "caret",
                    Width = 1,
                    Height = Math.Max(1, caret.PageRect.Height),
                    Fill = Brushes.Black
                };
                Canvas.SetLeft(rectangle, caret.PageRect.Left);
                Canvas.SetTop(rectangle, caret.PageRect.Top);
                pages[caret.PageNumber].Children.Add(rectangle);
            }
            return pages;
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            _editor.TextChanged -= OnTextChanged;
            _pending?.Dispose();
            _pending = null;
            _viewer.Document = null;
            _snapshot?.Dispose();
            _snapshot = null;
            _map = null;
        }

        private void OnTextChanged(object sender, TextChangedEventArgs e) => RequestRebuild();

        private void RequestRebuild()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            RequestedGeneration++;
            _pending?.Dispose();
            var pending = new ScheduledRebuild(++_nextIdentity, RequestedGeneration);
            _pending = pending;
            pending.Registration = _scheduler.Schedule(() => CompleteRebuild(pending));
        }

        private void CompleteRebuild(ScheduledRebuild pending)
        {
            if (_disposed || _pending is null || !ReferenceEquals(_pending, pending) ||
                pending.Identity != _pending.Identity || pending.Generation != RequestedGeneration)
                return;

            var source = _editor.Document;
            var settings = _settings;
            var restoreEditorFocus = HasLiveEditorFocus();
            var total = Stopwatch.StartNew();
            var snapshotWatch = Stopwatch.StartNew();
            var nextSnapshot = new WriterPreviewCloneService().CreateSnapshot(source, settings);
            snapshotWatch.Stop();
            var mapWatch = Stopwatch.StartNew();
            _viewer.Document = nextSnapshot.SourceClone;
            UpdateLayout(_host);
            var paginator = Assert.IsAssignableFrom<DynamicDocumentPaginator>(
                nextSnapshot.PrintPaginator);
            var nextMap = VisiblePageGeometryMap.Build(nextSnapshot.SourceClone, paginator,
                _viewer, _host, _visiblePage);
            mapWatch.Stop();
            total.Stop();

            if (_disposed || pending.Generation != RequestedGeneration ||
                !ReferenceEquals(source, _editor.Document) || settings != _settings)
            {
                _viewer.Document = _snapshot?.SourceClone;
                nextSnapshot.Dispose();
                return;
            }

            var previous = _snapshot;
            _snapshot = nextSnapshot;
            _map = nextMap;
            _pending = null;
            PublishedGeneration = pending.Generation;
            LastSnapshotMilliseconds = snapshotWatch.Elapsed.TotalMilliseconds;
            LastMapMilliseconds = mapWatch.Elapsed.TotalMilliseconds;
            _ = total.Elapsed;
            previous?.Dispose();
            if (restoreEditorFocus)
                RestoreLiveEditorFocus();
        }

        private bool IsCurrent(PageInteraction interaction) =>
            _map is not null && interaction.Generation == PublishedGeneration &&
            PublishedGeneration == RequestedGeneration &&
            _map.MappedPages.Contains(interaction.PageNumber);

        private bool HasLiveEditorFocus() => _editor.IsKeyboardFocusWithin ||
            ReferenceEquals(FocusManager.GetFocusedElement(_host), _editor);

        private void RestoreLiveEditorFocus()
        {
            FocusManager.SetFocusedElement(_host, _editor);
            _editor.Focus();
            Keyboard.Focus(_editor);
        }

        private TextPointer GetLivePosition(int sourceOffset)
        {
            var position = _editor.Document.ContentStart.GetPositionAtOffset(sourceOffset,
                LogicalDirection.Forward) ?? throw new InvalidOperationException(
                    $"Live offset {sourceOffset} is outside the authoritative document.");
            var insertion = position.GetInsertionPosition(LogicalDirection.Forward);
            if (insertion is null || OffsetOf(_editor.Document, insertion) != sourceOffset)
                throw new InvalidOperationException(
                    $"Clone offset {sourceOffset} is not the same live insertion position.");
            return insertion;
        }

        private VisiblePageGeometryMap RequireMap() => _map ??
            throw new InvalidOperationException("No geometry generation has been published.");
    }

    private sealed class VisiblePageGeometryMap
    {
        private readonly List<PageGeometryEntry> _entries;
        private readonly Dictionary<int, PageGeometryEntry[]> _byPage;

        private VisiblePageGeometryMap(int pageCount, List<PageGeometryEntry> entries)
        {
            PageCount = pageCount;
            _entries = entries;
            _byPage = entries.GroupBy(entry => entry.PageNumber)
                .ToDictionary(group => group.Key, group => group.ToArray());
            MappedPages = _byPage.Keys.Order().ToArray();
        }

        public int PageCount { get; }
        public IReadOnlyList<int> MappedPages { get; }
        public int EntryCount => _entries.Count;

        public Point GetPointNearPageEnd(int pageNumber)
        {
            var entry = _byPage[pageNumber][^5];
            return CenterLeft(entry.PageRect);
        }

        public Point GetPointNearPageStart(int pageNumber)
        {
            var entry = _byPage[pageNumber][Math.Min(10, _byPage[pageNumber].Length - 1)];
            return CenterLeft(entry.PageRect);
        }

        public PageGeometryEntry HitTest(int pageNumber, Point point) =>
            _byPage[pageNumber].MinBy(entry => DistanceSquared(entry.PageRect, point))!;

        public PageGeometryEntry? GetNearestByOffset(int sourceOffset)
        {
            if (_entries.Count == 0)
                return null;
            var nearest = _entries.MinBy(entry => Math.Abs((long)entry.SourceOffset - sourceOffset));
            return nearest is not null && Math.Abs((long)nearest.SourceOffset - sourceOffset) <= 1
                ? nearest
                : null;
        }

        public IEnumerable<SelectionFragment> ProjectSelection(int firstOffset, int secondOffset)
        {
            var start = Math.Min(firstOffset, secondOffset);
            var end = Math.Max(firstOffset, secondOffset);
            foreach (var page in _byPage)
            {
                var selected = page.Value
                    .Where(entry => entry.SourceOffset >= start && entry.SourceOffset <= end)
                    .GroupBy(entry => Math.Round(entry.PageRect.Top, 1));
                foreach (var line in selected)
                {
                    var left = line.Min(entry => entry.PageRect.Left);
                    var right = line.Max(entry => entry.PageRect.Right);
                    var top = line.Min(entry => entry.PageRect.Top);
                    var bottom = line.Max(entry => entry.PageRect.Bottom);
                    yield return new SelectionFragment(page.Key,
                        new Rect(left, top, Math.Max(2, right - left), Math.Max(1, bottom - top)));
                }
            }
        }

        public static VisiblePageGeometryMap Build(FlowDocument clone,
            DynamicDocumentPaginator paginator, FlowDocumentPageViewer viewer, Window host,
            int visiblePage)
        {
            paginator.ComputePageCount();
            if (visiblePage < 0 || visiblePage >= paginator.PageCount)
                throw new ArgumentOutOfRangeException(nameof(visiblePage));
            var pages = Enumerable.Range(Math.Max(0, visiblePage - 1),
                    Math.Min(paginator.PageCount - 1, visiblePage + 1) -
                    Math.Max(0, visiblePage - 1) + 1)
                .ToArray();
            var entries = new List<PageGeometryEntry>();
            foreach (var pageNumber in pages)
            {
                viewer.GoToPage(pageNumber + 1);
                UpdateLayout(host);
                var pageView = Assert.Single(viewer.PageViews,
                    page => page.PageNumber == pageNumber);
                var pageStart = Assert.IsType<TextPointer>(
                    paginator.GetPagePosition(paginator.GetPage(pageNumber)));
                var pageEnd = pageNumber + 1 < paginator.PageCount
                    ? Assert.IsType<TextPointer>(
                        paginator.GetPagePosition(paginator.GetPage(pageNumber + 1)))
                    : clone.ContentEnd;
                for (var position = pageStart.GetInsertionPosition(LogicalDirection.Forward);
                     position is not null && position.CompareTo(pageEnd) < 0;
                     position = position.GetNextInsertionPosition(LogicalDirection.Forward))
                {
                    if (paginator.GetPageNumber(position) != pageNumber)
                        continue;
                    var rect = position.GetCharacterRect(LogicalDirection.Forward);
                    if (rect.IsEmpty || !double.IsFinite(rect.X) || !double.IsFinite(rect.Y) ||
                        rect.Height <= 0)
                        continue;
                    Assert.InRange(rect.Left, -0.5, pageView.ActualWidth + 0.5);
                    Assert.InRange(rect.Bottom, -0.5, pageView.ActualHeight + 0.5);
                    entries.Add(new PageGeometryEntry(
                        clone.ContentStart.GetOffsetToPosition(position), pageNumber, rect));
                }
            }
            return new VisiblePageGeometryMap(paginator.PageCount, entries);
        }

        private static Point CenterLeft(Rect rect) =>
            new(rect.Left, rect.Top + rect.Height / 2);

        private static double DistanceSquared(Rect rect, Point point)
        {
            var xDistance = Math.Abs(point.X - rect.X);
            var yDistance = point.Y < rect.Top
                ? rect.Top - point.Y
                : point.Y > rect.Bottom
                    ? point.Y - rect.Bottom
                    : 0;
            return xDistance * xDistance + yDistance * yDistance * 16;
        }
    }

    private sealed class ManualScheduler
    {
        private readonly List<ScheduledCallback> _callbacks = new();

        public int PendingCount => _callbacks.Count(callback => !callback.Canceled);
        public int CanceledCount => _callbacks.Count(callback => callback.Canceled);

        public IDisposable Schedule(Action callback)
        {
            var scheduled = new ScheduledCallback(callback);
            _callbacks.Add(scheduled);
            return scheduled;
        }

        public void RunOldestIgnoringCancellation()
        {
            if (_callbacks.Count == 0)
                throw new InvalidOperationException("No callback is scheduled.");
            var callback = _callbacks[0];
            _callbacks.RemoveAt(0);
            callback.Callback();
        }

        public void RunLatest()
        {
            var latest = _callbacks.LastOrDefault(callback => !callback.Canceled) ??
                throw new InvalidOperationException("No live callback is scheduled.");
            _callbacks.Remove(latest);
            latest.Callback();
        }

        public void RunLatestIgnoringCanceled()
        {
            if (_callbacks.Count == 0)
                throw new InvalidOperationException("No callback is scheduled.");
            var latest = _callbacks[^1];
            _callbacks.RemoveAt(_callbacks.Count - 1);
            latest.Callback();
        }

        private sealed class ScheduledCallback(Action callback) : IDisposable
        {
            public Action Callback { get; } = callback;
            public bool Canceled { get; private set; }
            public void Dispose() => Canceled = true;
        }
    }

    private sealed class ScheduledRebuild(long identity, long generation) : IDisposable
    {
        public long Identity { get; } = identity;
        public long Generation { get; } = generation;
        public IDisposable? Registration { get; set; }
        public void Dispose() => Registration?.Dispose();
    }

    private sealed record PageGeometryEntry(int SourceOffset, int PageNumber, Rect PageRect);
    private sealed record SelectionFragment(int PageNumber, Rect Rect);
    private readonly record struct PageInteraction(long Generation, int PageNumber, Point Point);
    private readonly record struct StructuredObjectInteraction(FlowDocument Document,
        TextElement Element, int SourceOffset, PageInteraction PageInteraction);
    private readonly record struct SpellingRangeToken(long Generation,
        int StartOffset, int EndOffset);

    private static FlowDocument CreateParagraphDocument(DocumentPageSettings settings, int count)
    {
        var document = new FlowDocument
        {
            PageWidth = settings.WidthDip,
            PageHeight = settings.HeightDip,
            PagePadding = new Thickness(settings.Margins.LeftDip, settings.Margins.TopDip,
                settings.Margins.RightDip, settings.Margins.BottomDip),
            ColumnWidth = settings.ContentWidthDip,
            ColumnGap = 0,
            IsColumnWidthFlexible = false,
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 14
        };
        for (var index = 0; index < count; index++)
        {
            document.Blocks.Add(new Paragraph(new Run(
                $"Paragraph {index:D3}: deterministic paragraph compositor corpus."))
            {
                Margin = new Thickness(0, 0, 0, 6)
            });
        }
        return document;
    }

    private static void ApplyPageSettings(FlowDocument document, DocumentPageSettings settings)
    {
        document.PageWidth = settings.WidthDip;
        document.PageHeight = settings.HeightDip;
        document.PagePadding = new Thickness(settings.Margins.LeftDip, settings.Margins.TopDip,
            settings.Margins.RightDip, settings.Margins.BottomDip);
        document.ColumnWidth = settings.ContentWidthDip;
        document.ColumnGap = 0;
        document.IsColumnWidthFlexible = false;
    }

    private static void UpdateLayout(Window window)
    {
        window.UpdateLayout();
        window.Dispatcher.Invoke(DispatcherPriority.Render, new Action(() => { }));
    }
}
