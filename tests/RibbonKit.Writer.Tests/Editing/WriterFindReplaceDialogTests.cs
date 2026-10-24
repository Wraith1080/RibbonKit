using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Threading;
using RibbonKit.Controls;
using RibbonKit.Writer.Editing;
using RibbonKit.Writer.Tests.Document;
using Xunit;

namespace RibbonKit.Writer.Tests.Editing;

public sealed class WriterFindReplaceDialogTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SuccessfulFindFocusesEditorWhileFailedSearchKeepsDialogFocus(bool showReplace)
    {
        await StaTestHelper.RunAsync(async () =>
        {
            var editor = new RichTextBox { Document = new FlowDocument(new Paragraph(new Run("red blue red"))) };
            var owner = new Window { Content = editor, Width = 500, Height = 250,
                Left = -10000, Top = -10000, ShowInTaskbar = false };
            using var service = new WriterFindReplaceService(editor);
            var dialog = new WriterFindReplaceDialog(service, showReplace) {
                WindowStartupLocation = WindowStartupLocation.Manual, Left = -10000, Top = -10000 };
            try
            {
                owner.Show();
                dialog.Owner = owner;
                editor.Selection.Select(editor.Document.ContentStart, editor.Document.ContentStart);
                dialog.Show(); await Idle();
                var query = (RibbonTextBox)dialog.FindName("QueryBox");
                var next = (Button)dialog.FindName("FindNextButton");
                query.Text = "red";
                TextPointer? previous = null;
                for (int match = 0; match < 3; match++)
                {
                    dialog.Activate(); query.Focus(); await Idle();
                    Assert.True(dialog.IsActive);
                    next.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, next));
                    await Idle();
                    Assert.True(owner.IsActive);
                    Assert.True(editor.IsKeyboardFocusWithin);
                    Assert.Equal("red", editor.Selection.Text);
                    Assert.Equal("red", query.Text);
                    Assert.True(dialog.IsVisible);
                    if (previous is not null)
                        Assert.Equal(match == 1 ? 1 : -1, editor.Selection.Start.CompareTo(previous));
                    previous = editor.Selection.Start;
                }
                foreach (string failedQuery in new[] { "missing", "" })
                {
                    dialog.Activate(); query.Text = failedQuery; query.Focus(); await Idle();
                    next.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, next)); await Idle();
                    Assert.True(dialog.IsActive);
                    Assert.False(editor.IsKeyboardFocusWithin);
                    Assert.True(query.IsKeyboardFocusWithin);
                }
            }
            finally { dialog.Close(); owner.Close(); }
        }, TimeSpan.FromSeconds(20));

        static Task Idle() => Dispatcher.CurrentDispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle).Task;
    }
}
