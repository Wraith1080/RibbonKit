using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Threading;
using RibbonKit.Writer.Editing;
using RibbonKit.Writer.Models;
using RibbonKit.Writer.Tests.Document;
using Xunit;

namespace RibbonKit.Writer.Tests.Editing;

public sealed class WriterEditorSurfaceTests
{
    [Fact]
    public void PaperSwitchKeepsTheNativeEditorSelectionAndUndoState()
    {
        StaTestHelper.Run(() =>
        {
            var document = new FlowDocument(new Paragraph(new Run("paper text")));
            var editor = new RichTextBox { Document = document, Padding = new Thickness(28, 22, 28, 22) };
            var surface = CreateSurface(editor);
            var run = Assert.IsType<Run>(Assert.IsType<Paragraph>(document.Blocks.First()).Inlines.FirstInline);
            editor.Selection.Select(run.ContentStart, run.ContentEnd);
            var canUndo = editor.CanUndo;
            var selectionStart = editor.Selection.Start;
            var selectionEnd = editor.Selection.End;

            surface.PageSettings = DocumentPageSettings.A4();
            surface.ZoomPercent = 150;
            surface.ViewMode = WriterEditorViewMode.Paper;

            Assert.Same(editor, surface.Editor);
            Assert.Same(document, editor.Document);
            Assert.Equal(0, selectionStart.CompareTo(editor.Selection.Start));
            Assert.Equal(0, selectionEnd.CompareTo(editor.Selection.End));
            Assert.Equal(canUndo, editor.CanUndo);
            Assert.Equal(DocumentPageSettings.A4().WidthDip, document.PageWidth);
            Assert.Equal(DocumentPageSettings.A4().HeightDip, document.PageHeight);
            Assert.Equal(DocumentPageSettings.A4().WidthDip * 1.5, surface.PaperWidthDip, 4);
            Assert.Equal(WriterEditorViewMode.Paper, surface.ViewMode);
        });
    }

    [Fact]
    public void ContinuousSwitchRestoresFluidDocumentLayoutAndPaperZoomTracksOrientation()
    {
        StaTestHelper.Run(() =>
        {
            var document = new FlowDocument { PagePadding = new Thickness(0) };
            var editor = new RichTextBox { Document = document, Padding = new Thickness(28, 22, 28, 22) };
            var surface = CreateSurface(editor);
            var continuousWidth = document.PageWidth;
            var continuousHeight = document.PageHeight;

            var landscape = DocumentPageSettings.Letter(DocumentPageOrientation.Landscape,
                new DocumentPageMargins(36, 48, 36, 48));
            surface.PageSettings = landscape;
            surface.ZoomPercent = 125;
            surface.ViewMode = WriterEditorViewMode.Paper;

            Assert.Equal(landscape.WidthDip * 1.25, surface.PaperWidthDip, 4);
            Assert.Equal(landscape.HeightDip * 1.25, surface.PaperHeightDip, 4);
            Assert.Equal(landscape.WidthDip, document.PageWidth);
            Assert.Equal(landscape.HeightDip, document.PageHeight);
            Assert.Equal(ScrollBarVisibility.Auto, GetViewport(surface).HorizontalScrollBarVisibility);
            Assert.Equal(ScrollBarVisibility.Auto, GetViewport(surface).VerticalScrollBarVisibility);

            surface.ViewMode = WriterEditorViewMode.Continuous;

            Assert.Equal(continuousWidth, document.PageWidth);
            Assert.Equal(continuousHeight, document.PageHeight);
            Assert.Equal(new Thickness(0, 0, 0, 0), document.PagePadding);
            Assert.Equal(new Thickness(28, 22, 28, 22), editor.Padding);
            Assert.True(double.IsNaN(GetPaper(surface).Width));
            Assert.Equal(ScrollBarVisibility.Disabled, GetViewport(surface).HorizontalScrollBarVisibility);
            Assert.Equal(ScrollBarVisibility.Disabled, GetViewport(surface).VerticalScrollBarVisibility);
        });
    }

    [Fact]
    public void ReplacingDocumentUpdatesPageModelWithoutReplacingEditor()
    {
        StaTestHelper.Run(() =>
        {
            var first = new FlowDocument();
            var second = new FlowDocument();
            var editor = new RichTextBox { Document = first };
            var surface = CreateSurface(editor);
            surface.ViewMode = WriterEditorViewMode.Paper;
            surface.SetDocument(second);
            surface.PageSettings = DocumentPageSettings.Legal();

            Assert.Same(editor, surface.Editor);
            Assert.Same(second, editor.Document);
            Assert.Equal(DocumentPageSettings.Legal().WidthDip, second.PageWidth);
            Assert.Equal(DocumentPageSettings.Legal().HeightDip, second.PageHeight);
        });
    }

    [Fact]
    public void FirstLoadedPassReassertsPaperMarginsAfterLateDocumentInitialization()
    {
        StaTestHelper.Run(() =>
        {
            var settings = DocumentPageSettings.Letter(
                margins: new DocumentPageMargins(48, 60, 48, 60));
            var document = new FlowDocument { PagePadding = new Thickness(0) };
            var editor = new RichTextBox { Document = document };
            var surface = CreateSurface(editor);
            surface.PageSettings = settings;
            surface.ViewMode = WriterEditorViewMode.Paper;

            // Model the late first-load reset seen in the real Writer window. Before the surface
            // is loaded, native editor initialization can still replace the FlowDocument inset.
            document.PagePadding = new Thickness(0);

            var window = new Window
            {
                Content = surface,
                Width = 1200,
                Height = 900,
                WindowStartupLocation = WindowStartupLocation.Manual,
                Left = -10000,
                Top = -10000,
                ShowInTaskbar = false,
                Opacity = 0.01
            };
            try
            {
                window.Show();
                window.UpdateLayout();

                Assert.Equal(settings.Margins.LeftDip, document.PagePadding.Left, 4);
                Assert.Equal(settings.Margins.TopDip, document.PagePadding.Top, 4);
                Assert.Equal(settings.Margins.RightDip, document.PagePadding.Right, 4);
                Assert.Equal(settings.Margins.BottomDip, document.PagePadding.Bottom, 4);
            }
            finally
            {
                if (window.IsVisible)
                    window.Close();
            }
        });
    }

    [Fact]
    public void ZoomedPaperUsesHorizontalViewportWithoutTakingOverVerticalEditorScrolling()
    {
        StaTestHelper.Run(() =>
        {
            var editor = new RichTextBox { Document = new FlowDocument() };
            var surface = CreateSurface(editor);
            surface.ZoomPercent = 200;
            surface.ViewMode = WriterEditorViewMode.Paper;
            var host = new Window
            {
                Content = surface,
                Width = 480,
                Height = 360,
                WindowStartupLocation = WindowStartupLocation.Manual,
                Left = -10000,
                Top = -10000,
                ShowInTaskbar = false,
                Opacity = 0.01
            };

            host.Show();
            host.UpdateLayout();
            var viewport = GetViewport(surface);

            Assert.True(viewport.ScrollableWidth > 0);
            Assert.Equal(ScrollBarVisibility.Auto, viewport.HorizontalScrollBarVisibility);
            Assert.Equal(ScrollBarVisibility.Auto, viewport.VerticalScrollBarVisibility);
            Assert.Equal(ScrollBarVisibility.Disabled, editor.VerticalScrollBarVisibility);
            viewport.ScrollToHorizontalOffset(viewport.ScrollableWidth);
            host.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Render, new Action(() => { }));
            Assert.Equal(viewport.ScrollableWidth, viewport.HorizontalOffset);
            host.Close();
        });
    }

    [Fact]
    public void FittingPaperIsCenteredAndShortHostedWindowsScrollThePaperBothWays()
    {
        StaTestHelper.Run(() =>
        {
            using var fitting = CreateHostedSurface(DocumentPageSettings.Letter(), 1200, 1000);
            var fittingPaper = GetPaper(fitting.Surface);
            var paperOrigin = fittingPaper.TranslatePoint(new Point(0, 0), fitting.Surface);
            var fittingViewport = GetViewport(fitting.Surface);
            var viewportOrigin = fittingViewport.TranslatePoint(new Point(0, 0), fitting.Surface);
            var expectedLeft = viewportOrigin.X + (fittingViewport.ViewportWidth - fittingPaper.ActualWidth) / 2d;
            Assert.InRange(Math.Abs(paperOrigin.X - expectedLeft), 0, 2.0);

            foreach (var settings in new[] { DocumentPageSettings.Letter(), DocumentPageSettings.A4() })
            {
                using var hosted = CreateHostedSurface(settings, 480, 360);
                var viewport = GetViewport(hosted.Surface);
                Assert.True(viewport.ScrollableHeight > 0);
                viewport.ScrollToVerticalOffset(viewport.ScrollableHeight);
                viewport.ScrollToHorizontalOffset(viewport.ScrollableWidth);
                hosted.Window.Dispatcher.Invoke(DispatcherPriority.Render, new Action(() => { }));
                Assert.Equal(viewport.ScrollableHeight, viewport.VerticalOffset);
                Assert.True(viewport.ScrollableWidth > 0);
                Assert.Equal(viewport.ScrollableWidth, viewport.HorizontalOffset);
            }
        });
    }

    [Fact]
    public void PaperSwitchPreservesRealUndoSelectionEditorStateAndConditionalFocus()
    {
        StaTestHelper.Run(() =>
        {
            using var hosted = CreateHostedSurface(DocumentPageSettings.A4(), 760, 620);
            var editor = hosted.Editor;
            editor.IsUndoEnabled = true;
            editor.IsReadOnly = false;
            SpellCheck.SetIsEnabled(editor, false);
            editor.Focus();
            using var service = new WriterFindReplaceService(editor);
            Assert.True(service.ReplaceNext("paper", "edited", matchCase: true, wrap: false).Replaced);
            Assert.True(editor.CanUndo);
            var textAfterEdit = new TextRange(editor.Document.ContentStart, editor.Document.ContentEnd).Text;
            var selectionStart = editor.Selection.Start;
            var selectionEnd = editor.Selection.End;
            var isReadOnly = editor.IsReadOnly;
            var isUndoEnabled = editor.IsUndoEnabled;
            var isSpellCheckEnabled = SpellCheck.GetIsEnabled(editor);

            hosted.Surface.ViewMode = WriterEditorViewMode.Continuous;
            hosted.Surface.ViewMode = WriterEditorViewMode.Paper;

            Assert.Same(editor, hosted.Surface.Editor);
            Assert.Same(hosted.Document, editor.Document);
            Assert.Equal(textAfterEdit, new TextRange(editor.Document.ContentStart, editor.Document.ContentEnd).Text);
            Assert.Equal(0, selectionStart.CompareTo(editor.Selection.Start));
            Assert.Equal(0, selectionEnd.CompareTo(editor.Selection.End));
            Assert.True(editor.CanUndo);
            Assert.Equal(isReadOnly, editor.IsReadOnly);
            Assert.Equal(isUndoEnabled, editor.IsUndoEnabled);
            Assert.Equal(isSpellCheckEnabled, SpellCheck.GetIsEnabled(editor));
            Assert.True(editor.IsKeyboardFocusWithin);
            Assert.Equal(DocumentPageSettings.A4().Margins.TopDip, hosted.Document.PagePadding.Top, 4);
            Assert.Equal(DocumentPageSettings.A4().Margins.LeftDip, hosted.Document.PagePadding.Left, 4);

            editor.Undo();
            Assert.DoesNotContain("edited", new TextRange(editor.Document.ContentStart, editor.Document.ContentEnd).Text);
        });
    }

    [Fact]
    public void ControllerZoomAndPaperGeometryUseOneScale()
    {
        StaTestHelper.Run(() =>
        {
            using var hosted = CreateHostedSurface(DocumentPageSettings.Letter(), 1500, 1200);
            using var controller = new WriterEditingRibbonController(hosted.Editor);
            controller.Zoom.TrySet(150);
            hosted.Surface.ZoomPercent = controller.Zoom.Value;
            hosted.Window.UpdateLayout();

            var paper = GetPaper(hosted.Surface);
            Assert.Equal(DocumentPageSettings.Letter().WidthDip * 1.5, paper.ActualWidth, 1.5);
            Assert.NotEqual(DocumentPageSettings.Letter().WidthDip * 2.25, paper.ActualWidth, 2.0);
        });
    }

    [Fact]
    public void LongPaperContentExpandsTheSingleOuterVerticalScrollSurface()
    {
        StaTestHelper.Run(() =>
        {
            using var hosted = CreateHostedSurface(DocumentPageSettings.Letter(), 760, 620);
            for (var index = 0; index < 120; index++)
                hosted.Document.Blocks.Add(new Paragraph(new Run($"Paragraph {index}: continuous paper content.")));
            hosted.Window.UpdateLayout();

            var viewport = GetViewport(hosted.Surface);
            var paper = GetPaper(hosted.Surface);
            Assert.Equal(ScrollBarVisibility.Disabled, hosted.Editor.VerticalScrollBarVisibility);
            Assert.Equal(ScrollBarVisibility.Auto, viewport.VerticalScrollBarVisibility);
            Assert.True(paper.ActualHeight > hosted.Surface.PaperHeightDip);
            Assert.True(viewport.ScrollableHeight > hosted.Surface.PaperHeightDip - viewport.ViewportHeight);

            viewport.ScrollToBottom();
            hosted.Window.Dispatcher.Invoke(DispatcherPriority.Render, new Action(() => { }));
            Assert.Equal(viewport.ScrollableHeight, viewport.VerticalOffset);
        });
    }

    private static WriterEditorSurface CreateSurface(RichTextBox editor)
    {
        var surface = new WriterEditorSurface();
        surface.Children.Add(new ScrollViewer());
        var viewport = Assert.IsType<ScrollViewer>(surface.Children[0]);
        var paper = new Border { Child = editor };
        viewport.Content = paper;
        surface.Attach(editor, viewport, paper);
        return surface;
    }

    private static ScrollViewer GetViewport(WriterEditorSurface surface) =>
        Assert.IsType<ScrollViewer>(surface.Children[0]);

    private static Border GetPaper(WriterEditorSurface surface) =>
        Assert.IsType<Border>(GetViewport(surface).Content);

    private static HostedSurface CreateHostedSurface(DocumentPageSettings settings, double width, double height)
    {
        var document = new FlowDocument(new Paragraph(new Run("paper content")));
        var editor = new RichTextBox
        {
            Document = document,
            IsUndoEnabled = true,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Padding = new Thickness(28, 22, 28, 22)
        };
        var surface = CreateSurface(editor);
        surface.PageSettings = settings;
        surface.ViewMode = WriterEditorViewMode.Paper;
        var window = new Window
        {
            Content = surface,
            Width = width,
            Height = height,
            WindowStartupLocation = WindowStartupLocation.Manual,
            Left = -10000,
            Top = -10000,
            ShowInTaskbar = false,
            Opacity = 0.01
        };
        window.Show();
        window.UpdateLayout();
        return new HostedSurface(window, surface, editor, document);
    }

    private sealed class HostedSurface(Window window, WriterEditorSurface surface,
        RichTextBox editor, FlowDocument document) : IDisposable
    {
        public Window Window { get; } = window;
        public WriterEditorSurface Surface { get; } = surface;
        public RichTextBox Editor { get; } = editor;
        public FlowDocument Document { get; } = document;

        public void Dispose()
        {
            if (Window.IsVisible)
                Window.Close();
        }
    }
}
