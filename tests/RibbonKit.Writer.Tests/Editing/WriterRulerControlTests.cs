using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using RibbonKit.Writer.Models;
using RibbonKit.Writer.Editing;
using RibbonKit.Writer.Tests.Document;
using Xunit;

namespace RibbonKit.Writer.Tests.Editing;

public sealed class WriterRulerControlTests
{
    [Fact]
    public void AppearanceRefreshInvalidatesRealizedRulerWithoutChangingGeometry()
    {
        StaTestHelper.Run(() =>
        {
            using var ruler = new WriterRuler
            {
                Width = 520,
                IsPaperView = true,
                PageSettings = DocumentPageSettings.Letter()
            };
            var window = new Window
            {
                Content = ruler,
                Width = 520,
                Height = 100,
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
                var layout = ruler.Layout;
                Assert.True(ruler.IsArrangeValid);

                ruler.RefreshAppearance();

                Assert.False(ruler.IsArrangeValid);
                Assert.Same(layout, ruler.Layout);
            }
            finally
            {
                if (window.IsVisible)
                    window.Close();
            }
        });
    }

    [Fact]
    public void MarginDragPreviewsWithoutDirtyingOrCommittingUntilRelease()
    {
        StaTestHelper.Run(() =>
        {
            using var ruler = new WriterRuler
            {
                Width = 1200,
                IsPaperView = true,
                PageSettings = DocumentPageSettings.Letter()
            };
            var previews = 0;
            var commits = 0;
            DocumentPageSettings? committed = null;
            ruler.PageSettingsPreviewChanged += (_, _) => previews++;
            ruler.PageSettingsCommitted += (_, args) =>
            {
                commits++;
                committed = args.Settings;
            };

            var opening = ruler.PageSettings;
            Assert.True(ruler.TryBeginMarginDrag(WriterRulerMarginEdge.Left));
            ruler.UpdateMarginDragPosition(ruler.Layout.ContentStartDip + 24);

            Assert.True(ruler.IsMarginDragActive);
            Assert.True(previews > 0);
            Assert.Equal(opening.Margins, ruler.PageSettings.Margins);
            Assert.Equal(0, commits);
            ruler.CommitMarginDragPosition();

            Assert.False(ruler.IsMarginDragActive);
            Assert.Equal(1, commits);
            Assert.NotNull(committed);
            Assert.Equal(opening.Margins.LeftDip + 24, committed!.Margins.LeftDip, 6);
            Assert.Equal(committed.Margins, ruler.PageSettings.Margins);
        });
    }

    [Fact]
    public void EscapeOrCaptureLossCancellationLeavesPageSettingsUntouched()
    {
        StaTestHelper.Run(() =>
        {
            using var ruler = new WriterRuler
            {
                Width = 1200,
                IsPaperView = true,
                PageSettings = DocumentPageSettings.A4()
            };
            var cancelled = 0;
            var committed = 0;
            ruler.PageSettingsDragCancelled += (_, _) => cancelled++;
            ruler.PageSettingsCommitted += (_, _) => committed++;
            var opening = ruler.PageSettings;

            Assert.True(ruler.TryBeginMarginDrag(WriterRulerMarginEdge.Right));
            ruler.UpdateMarginDragPosition(ruler.Layout.ContentEndDip - 48);
            ruler.CancelMarginDragPosition();

            Assert.False(ruler.IsMarginDragActive);
            Assert.Equal(opening, ruler.PageSettings);
            Assert.Equal(1, cancelled);
            Assert.Equal(0, committed);
        });
    }

    [Fact]
    public void RulerUsesPhysicalPageOriginRatherThanASecondScrollOffsetModel()
    {
        StaTestHelper.Run(() =>
        {
            var document = new FlowDocument(new Paragraph(new Run("paper")));
            var editor = new RichTextBox { Document = document };
            var viewport = new ScrollViewer
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalContentAlignment = HorizontalAlignment.Center
            };
            var paper = new Border { Child = editor, Width = 1300, Height = 1000 };
            viewport.Content = paper;
            var root = new Grid { Width = 520, Height = 360 };
            root.Children.Add(viewport);
            using var ruler = new WriterRuler { IsPaperView = true, Width = 520 };
            root.Children.Add(ruler);
            ruler.Attach(editor, viewport, paper);
            var window = new Window
            {
                Content = root,
                Width = 520,
                Height = 360,
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
                var before = ruler.Layout.PageOriginDip;
                viewport.ScrollToHorizontalOffset(viewport.ScrollableWidth);
                window.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Render,
                    new Action(() => { }));
                var after = ruler.Layout.PageOriginDip;
                Assert.True(viewport.ScrollableWidth > 0);
                Assert.Equal(before - viewport.HorizontalOffset, after, 3);
            }
            finally
            {
                if (window.IsVisible)
                    window.Close();
            }
        });
    }

    [Fact]
    public void MarginGuideClipMatchesItsEditorSurfaceSlotAndStopsBeforeStatusRow()
    {
        StaTestHelper.Run(() =>
        {
            var editor = new RichTextBox
            {
                Document = new FlowDocument(new Paragraph(new Run("guide")))
            };
            var paper = new Border { Child = editor, Width = 900, Height = 700 };
            var viewport = new ScrollViewer
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Content = paper
            };
            var root = new Grid { Width = 480, Height = 300 };
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(30) });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(28) });
            Grid.SetRow(viewport, 1);
            root.Children.Add(viewport);
            var guide = new WriterMarginGuide
            {
                IsPaperView = true,
                PageSettings = DocumentPageSettings.Letter()
            };
            Grid.SetRow(guide, 1);
            root.Children.Add(guide);
            var statusRow = new Border { Height = 28 };
            Grid.SetRow(statusRow, 2);
            root.Children.Add(statusRow);
            guide.Attach(editor, viewport, paper);
            var window = new Window
            {
                Content = root,
                Width = 480,
                Height = 300,
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

                var clip = Assert.IsType<RectangleGeometry>(guide.Clip);
                Assert.Equal(guide.ActualWidth, clip.Bounds.Width, 3);
                Assert.Equal(guide.ActualHeight, clip.Bounds.Height, 3);
                var guideBottom = guide.TransformToAncestor(root)
                    .Transform(new Point(0, guide.ActualHeight)).Y;
                var statusTop = statusRow.TransformToAncestor(root).Transform(new Point(0, 0)).Y;
                Assert.True(guideBottom <= statusTop + 0.1);
                guide.Dispose();
                Assert.Throws<ObjectDisposedException>(() => guide.Attach(editor, viewport, paper));
            }
            finally
            {
                if (window.IsVisible)
                    window.Close();
            }
        });
    }

    [Fact]
    public void CoincidentDefaultMarkersUseDistinctHitBandsAndLeaveMarginBandReachable()
    {
        StaTestHelper.Run(() =>
        {
            using var ruler = new WriterRuler
            {
                Width = 1200,
                IsPaperView = true,
                PageSettings = DocumentPageSettings.Letter()
            };
            var left = ruler.Layout.ContentStartDip;
            var right = ruler.Layout.ContentEndDip;

            Assert.Equal(WriterRulerIndentMarker.FirstLine,
                ruler.HitTestIndentMarkerAt(left, 4));
            Assert.Equal(WriterRulerIndentMarker.Left,
                ruler.HitTestIndentMarkerAt(left, 21));
            Assert.Equal(WriterRulerIndentMarker.Hanging,
                ruler.HitTestIndentMarkerAt(left, 28));
            Assert.Null(ruler.HitTestIndentMarkerAt(left, 14));
            Assert.True(ruler.HitTestMarginEdgeAt(left, 14, out var leftEdge));
            Assert.Equal(WriterRulerMarginEdge.Left, leftEdge);

            Assert.Equal(WriterRulerIndentMarker.Right,
                ruler.HitTestIndentMarkerAt(right, 4));
            Assert.Null(ruler.HitTestIndentMarkerAt(right, 14));
            Assert.True(ruler.HitTestMarginEdgeAt(right, 14, out var rightEdge));
            Assert.Equal(WriterRulerMarginEdge.Right, rightEdge);
        });
    }

    [Fact]
    public void MixedParagraphSelectionSuppressesRulerMarkersAndDragTargets()
    {
        StaTestHelper.Run(() =>
        {
            var first = new Paragraph(new Run("first"))
            {
                Margin = new Thickness(24, 0, 12, 0),
                TextIndent = 4
            };
            var second = new Paragraph(new Run("second"))
            {
                Margin = new Thickness(48, 0, 20, 0),
                TextIndent = -8
            };
            var document = new FlowDocument();
            document.Blocks.Add(first);
            document.Blocks.Add(second);
            var editor = new RichTextBox { Document = document };
            var adapter = new WriterEditingAdapter(editor);
            var viewport = new ScrollViewer();
            var paper = new Border { Child = editor, Width = 900, Height = 600 };
            viewport.Content = paper;
            var root = new Grid { Width = 520, Height = 300 };
            root.Children.Add(viewport);
            using var ruler = new WriterRuler { IsPaperView = true, Width = 520 };
            root.Children.Add(ruler);
            ruler.Attach(editor, viewport, paper, adapter);
            editor.SelectAll();
            var window = new Window
            {
                Content = root,
                Width = 520,
                Height = 300,
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
                var left = ruler.Layout.ContentStartDip;

                Assert.True(adapter.HasMixedRulerIndentation);
                Assert.Null(ruler.HitTestIndentMarkerAt(left, 4));
                Assert.False(ruler.TryBeginParagraphIndentDrag(WriterRulerIndentMarker.Left));
                Assert.False(ruler.IsParagraphIndentDragActive);
            }
            finally
            {
                adapter.Dispose();
                if (window.IsVisible)
                    window.Close();
            }
        });
    }

    [Fact]
    public void RulerLifecycleChangesCancelActiveMarginAndParagraphDrags()
    {
        StaTestHelper.Run(() =>
        {
            var paragraph = new Paragraph(new Run("lifecycle"));
            var editor = new RichTextBox { Document = new FlowDocument(paragraph) };
            var adapter = new WriterEditingAdapter(editor);
            var viewport = new ScrollViewer();
            var paper = new Border { Child = editor, Width = 900, Height = 600 };
            viewport.Content = paper;
            var root = new Grid { Width = 520, Height = 300 };
            root.Children.Add(viewport);
            using var ruler = new WriterRuler
            {
                IsPaperView = true,
                Width = 520,
                PageSettings = DocumentPageSettings.Letter()
            };
            root.Children.Add(ruler);
            ruler.Attach(editor, viewport, paper, adapter);
            editor.SelectAll();
            var window = new Window
            {
                Content = root,
                Width = 520,
                Height = 300,
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

                Assert.True(ruler.TryBeginParagraphIndentDrag(WriterRulerIndentMarker.Left));
                ruler.CanEditParagraphs = false;
                Assert.False(ruler.IsParagraphIndentDragActive);

                ruler.CanEditParagraphs = true;
                Assert.True(ruler.TryBeginMarginDrag(WriterRulerMarginEdge.Left));
                ruler.PageSettings = DocumentPageSettings.A4();
                Assert.False(ruler.IsMarginDragActive);

                Assert.True(ruler.TryBeginMarginDrag(WriterRulerMarginEdge.Right));
                ruler.CanEditMargins = false;
                Assert.False(ruler.IsMarginDragActive);

                ruler.CanEditMargins = true;
                Assert.True(ruler.TryBeginMarginDrag(WriterRulerMarginEdge.Right));
                ruler.IsPaperView = false;
                Assert.False(ruler.IsMarginDragActive);

                ruler.IsPaperView = true;
                Assert.True(ruler.TryBeginMarginDrag(WriterRulerMarginEdge.Right));
                ruler.IsRulerVisible = false;
                Assert.False(ruler.IsMarginDragActive);
            }
            finally
            {
                adapter.Dispose();
                if (window.IsVisible)
                    window.Close();
            }
        });
    }
}
