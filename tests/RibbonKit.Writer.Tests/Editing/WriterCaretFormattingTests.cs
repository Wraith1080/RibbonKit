using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Reflection;
using RibbonKit.Writer.Editing;
using RibbonKit.Writer.Tests.Document;
using Xunit;

namespace RibbonKit.Writer.Tests.Editing;

[Collection("Writer UI")]
public sealed class WriterCaretFormattingTests
{
    [Theory]
    [InlineData(false, "inline")]
    [InlineData(true, "inline")]
    [InlineData(false, "dialog")]
    [InlineData(true, "dialog")]
    [InlineData(false, "clear")]
    [InlineData(true, "clear")]
    public void CaretFormattingSurvivesClickAtTheSameInsertionPoint(bool empty, string command)
    {
        StaTestHelper.Run(() =>
        {
            using var fixture = new CaretFixture(empty);
            if (command == "clear")
            {
                fixture.AllText.ApplyPropertyValue(TextElement.FontSizeProperty, 30d);
                fixture.AllText.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Bold);
                fixture.Adapter.ClearFormatting();
            }
            else if (command == "dialog")
                fixture.Adapter.ApplyFontDialogValues(new FontFamily("Arial"), 32, FontStyles.Italic,
                    FontWeights.Bold, Colors.Red, true, WriterStrikethroughStyle.Double, WriterBaselineEffect.Superscript);
            else
            {
                fixture.Adapter.ApplyFontFamily(new FontFamily("Arial"));
                fixture.Adapter.ApplyFontSize(32);
                fixture.Adapter.ToggleBold();
                fixture.Adapter.ToggleItalic();
                fixture.Adapter.ToggleUnderline();
                fixture.Adapter.ApplyForeground(Colors.Red);
                fixture.Adapter.ApplyHighlight(Colors.Yellow);
            }
            var expected = fixture.Adapter.State;
            string text = fixture.Text;
            bool undo = fixture.Editor.CanUndo;
            var caret = fixture.Editor.CaretPosition;
            fixture.Editor.UpdateLayout();
            var blankArea = fixture.Editor.GetPositionFromPoint(new Point(fixture.Editor.ActualWidth - 12,
                fixture.Editor.ActualHeight / 2), snapToText: true)!;
            foreach (var target in new[] { caret, blankArea })
            {
                fixture.BeginCaretClick();
                fixture.ReselectCaret(target);
                fixture.EndCaretClick();
            }
            fixture.Adapter.RefreshState();
            Assert.Equal(expected.FontSize, fixture.Adapter.State.FontSize);
            Assert.Equal(expected.FontFamily.Value.Source, fixture.Adapter.State.FontFamily.Value.Source);
            Assert.Equal(expected.Bold, fixture.Adapter.State.Bold);
            Assert.Equal(expected.Italic, fixture.Adapter.State.Italic);
            Assert.Equal(expected.Underline, fixture.Adapter.State.Underline);
            Assert.Equal(expected.Strikethrough, fixture.Adapter.State.Strikethrough);
            Assert.Equal(expected.BaselineEffect, fixture.Adapter.State.BaselineEffect);
            Assert.Equal(expected.Foreground, fixture.Adapter.State.Foreground);
            Assert.Equal(expected.Highlight, fixture.Adapter.State.Highlight);
            Assert.Equal(text, fixture.Text);
            Assert.Equal(undo, fixture.Editor.CanUndo);
            fixture.Type("x");
            Assert.Equal(expected.FontSize.Value, fixture.Editor.Selection.GetPropertyValue(TextElement.FontSizeProperty));
            Assert.Equal(empty ? "x" : "before x", fixture.Text);
        });
    }

    [Fact]
    public void CaretClickDoesNotCarryFormattingToAnotherPositionOrAcrossAnEdit()
    {
        StaTestHelper.Run(() =>
        {
            using var fixture = new CaretFixture(empty: false);
            fixture.Adapter.ApplyFontSize(32);
            fixture.BeginCaretClick();
            var start = fixture.Editor.Document.ContentStart.GetInsertionPosition(LogicalDirection.Forward)!;
            fixture.ReselectCaret(start);
            fixture.EndCaretClick();
            Assert.Equal(16d, fixture.Editor.Selection.GetPropertyValue(TextElement.FontSizeProperty));

            fixture.Editor.CaretPosition = fixture.Editor.Document.ContentEnd;
            fixture.Adapter.ApplyFontSize(32);
            fixture.BeginCaretClick();
            fixture.Editor.AppendText("changed");
            var caret = fixture.Editor.CaretPosition;
            fixture.ReselectCaret(caret);
            fixture.EndCaretClick();
            Assert.Equal(16d, fixture.Editor.Selection.GetPropertyValue(TextElement.FontSizeProperty));
        });
    }

    [Theory]
    [InlineData("selection")]
    [InlineData("document")]
    [InlineData("readonly")]
    [InlineData("disabled")]
    [InlineData("disposed")]
    public void InvalidatedCaretClickDoesNotRestoreOldFormatting(string reason)
    {
        StaTestHelper.Run(() =>
        {
            using var fixture = new CaretFixture(empty: false);
            fixture.Adapter.ApplyFontSize(32);
            var caret = fixture.Editor.CaretPosition;
            fixture.BeginCaretClick();
            if (reason == "selection") fixture.Editor.SelectAll();
            else if (reason == "document")
            {
                fixture.Editor.Document = new FlowDocument(new Paragraph(new Run("new document"))) { FontSize = 20 };
                caret = fixture.Editor.CaretPosition;
            }
            else if (reason == "readonly") { fixture.Editor.IsReadOnly = true; fixture.Editor.IsReadOnly = false; }
            else if (reason == "disabled") { fixture.Editor.IsEnabled = false; fixture.Editor.IsEnabled = true; }
            else fixture.Adapter.Dispose();
            fixture.ReselectCaret(caret);
            fixture.EndCaretClick();
            Assert.Equal(reason == "document" ? 20d : 16d,
                fixture.Editor.Selection.GetPropertyValue(TextElement.FontSizeProperty));
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CaretFontSizeAppliesToSubsequentTyping(bool empty)
    {
        StaTestHelper.Run(() =>
        {
            using var fixture = new CaretFixture(empty);
            fixture.Adapter.ApplyFontSize(32);
            Assert.Equal(32d, fixture.Editor.Selection.GetPropertyValue(TextElement.FontSizeProperty));
            Assert.Equal(32d, fixture.Adapter.State.FontSize.Value);
            fixture.Type("x");
            Assert.Equal(32d, fixture.Editor.Selection.GetPropertyValue(TextElement.FontSizeProperty));
            Assert.Equal(empty ? "x" : "before x", fixture.Text);
            if (!empty)
                Assert.Same(DependencyProperty.UnsetValue, fixture.AllText.GetPropertyValue(TextElement.FontSizeProperty));
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CaretBoldCanBeToggledBeforeTyping(bool empty)
    {
        StaTestHelper.Run(() =>
        {
            using var fixture = new CaretFixture(empty);
            fixture.Adapter.ToggleBold();
            Assert.Equal(FontWeights.Bold, fixture.Editor.Selection.GetPropertyValue(TextElement.FontWeightProperty));
            Assert.True(fixture.Adapter.State.Bold.IsUniform && fixture.Adapter.State.Bold.Value);
            fixture.Adapter.ToggleBold();
            Assert.Equal(FontWeights.Normal, fixture.Editor.Selection.GetPropertyValue(TextElement.FontWeightProperty));
            fixture.Adapter.ToggleBold();
            fixture.Type("x");
            Assert.Equal(FontWeights.Bold, fixture.Editor.Selection.GetPropertyValue(TextElement.FontWeightProperty));
            Assert.Equal(empty ? "x" : "before x", fixture.Text);
            if (!empty)
                Assert.Same(DependencyProperty.UnsetValue, fixture.AllText.GetPropertyValue(TextElement.FontWeightProperty));
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CaretInlinePropertiesSurviveFocusAndStateRefresh(bool empty)
    {
        StaTestHelper.Run(() =>
        {
            using var fixture = new CaretFixture(empty);
            fixture.Adapter.ApplyFontFamily(new FontFamily("Arial"));
            fixture.Adapter.ToggleItalic();
            fixture.Adapter.ToggleUnderline();
            fixture.Adapter.ApplyForeground(Colors.Red);
            fixture.Adapter.ApplyHighlight(Colors.Yellow);
            fixture.MoveFocusAwayAndBack();
            fixture.Adapter.RefreshState();
            Assert.Equal("Arial", fixture.Adapter.State.FontFamily.Value.Source);
            Assert.True(fixture.Adapter.State.Italic.Value);
            Assert.True(fixture.Adapter.State.Underline.Value);
            Assert.Equal(Colors.Red, fixture.Adapter.State.Foreground.Value);
            Assert.Equal(Colors.Yellow, fixture.Adapter.State.Highlight.Value);
            fixture.Type("x");
            Assert.Equal(empty ? "x" : "before x", fixture.Text);
            Assert.Equal(FontStyles.Italic, fixture.Editor.Selection.GetPropertyValue(TextElement.FontStyleProperty));
            Assert.Equal("Arial", Assert.IsType<FontFamily>(fixture.Editor.Selection.GetPropertyValue(TextElement.FontFamilyProperty)).Source);
            Assert.Equal(Colors.Red, Assert.IsType<SolidColorBrush>(fixture.Editor.Selection.GetPropertyValue(TextElement.ForegroundProperty)).Color);
            Assert.True(fixture.Adapter.State.Underline.Value);
            Assert.Equal(Colors.Yellow, fixture.Adapter.State.Highlight.Value);
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CaretFontDialogAndClearFormattingUseNativeTypingState(bool empty)
    {
        StaTestHelper.Run(() =>
        {
            using var fixture = new CaretFixture(empty);
            fixture.Adapter.ApplyFontDialogValues(new FontFamily("Arial"), 32, FontStyles.Italic,
                FontWeights.Bold, Colors.Red, true, WriterStrikethroughStyle.Double, WriterBaselineEffect.Superscript);
            Assert.Equal(32, fixture.Adapter.State.FontSize.Value);
            Assert.True(fixture.Adapter.State.Bold.Value && fixture.Adapter.State.Italic.Value && fixture.Adapter.State.Underline.Value);
            Assert.Equal(WriterStrikethroughStyle.Double, fixture.Adapter.State.Strikethrough.Value);
            Assert.Equal(WriterBaselineEffect.Superscript, fixture.Adapter.State.BaselineEffect.Value);
            fixture.Adapter.ClearFormatting();
            Assert.Equal(16, fixture.Adapter.State.FontSize.Value);
            Assert.False(fixture.Adapter.State.Bold.Value || fixture.Adapter.State.Italic.Value || fixture.Adapter.State.Underline.Value);
            Assert.Equal(WriterStrikethroughStyle.None, fixture.Adapter.State.Strikethrough.Value);
            Assert.Equal(WriterBaselineEffect.Normal, fixture.Adapter.State.BaselineEffect.Value);
            fixture.Type("x");
            Assert.Equal(empty ? "x" : "before x", fixture.Text);
            Assert.Equal(16d, fixture.Editor.Selection.GetPropertyValue(TextElement.FontSizeProperty));
            Assert.Equal(FontWeights.Normal, fixture.Editor.Selection.GetPropertyValue(TextElement.FontWeightProperty));
        });
    }

    [Fact]
    public void CaretInsideAWordUsesNativeWordFormattingWithoutCreatingASelection()
    {
        StaTestHelper.Run(() =>
        {
            using var fixture = new CaretFixture(false);
            var run = ((Paragraph)fixture.Editor.Document.Blocks.FirstBlock).Inlines.FirstInline;
            var caret = run.ContentStart.GetPositionAtOffset(2)!;
            fixture.Editor.Selection.Select(caret, caret);
            fixture.Adapter.ApplyFontSize(32);
            fixture.Adapter.ToggleBold();
            Assert.True(fixture.Editor.Selection.IsEmpty);
            var word = new TextRange(fixture.Editor.Document.ContentStart.GetNextInsertionPosition(LogicalDirection.Forward)!,
                fixture.Editor.Document.ContentStart.GetNextInsertionPosition(LogicalDirection.Forward)!.GetPositionAtOffset(6)!);
            Assert.Equal(32d, word.GetPropertyValue(TextElement.FontSizeProperty));
            Assert.Equal(FontWeights.Bold, word.GetPropertyValue(TextElement.FontWeightProperty));
            var sameCaret = fixture.Editor.CaretPosition;
            fixture.BeginCaretClick();
            fixture.ReselectCaret(sameCaret);
            fixture.EndCaretClick();
            fixture.Adapter.Undo();
            Assert.Equal(32d, word.GetPropertyValue(TextElement.FontSizeProperty));
            Assert.Equal(FontWeights.Normal, word.GetPropertyValue(TextElement.FontWeightProperty));
            fixture.Adapter.Undo();
            Assert.Equal(16d, word.GetPropertyValue(TextElement.FontSizeProperty));
        });
    }

    [Fact]
    public void MovingCaretAndReplacingDocumentReadNativeFormattingWithoutRetainingOldChoices()
    {
        StaTestHelper.Run(() =>
        {
            using var fixture = new CaretFixture(false);
            fixture.Adapter.ApplyFontSize(32);
            fixture.Adapter.ToggleBold();
            EditingCommands.MoveToDocumentStart.Execute(null, fixture.Editor);
            fixture.Adapter.RefreshState();
            Assert.Equal(16, fixture.Adapter.State.FontSize.Value);
            Assert.False(fixture.Adapter.State.Bold.Value);
            fixture.Editor.Document = new FlowDocument(new Paragraph(new Run("replacement"))) { FontSize = 20 };
            fixture.Adapter.RefreshState();
            Assert.Equal(20, fixture.Adapter.State.FontSize.Value);
            Assert.False(fixture.Adapter.State.Bold.Value);
        });
    }

    private sealed class CaretFixture : IDisposable
    {
        private readonly Window _window;
        private readonly Button _other = new() { Content = "Ribbon command" };
        public CaretFixture(bool empty)
        {
            var run = new Run(empty ? string.Empty : "before ");
            Editor = new RichTextBox { Document = new FlowDocument(new Paragraph(run)) { FontSize = 16 } };
            var panel = new StackPanel(); panel.Children.Add(_other); panel.Children.Add(Editor);
            _window = new Window { Content = panel, Width = 250, Height = 140, ShowInTaskbar = false };
            _window.Show();
            _window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
            Assert.True(Editor.Focus());
            Assert.Same(Editor, Keyboard.FocusedElement);
            var caret = empty ? run.ContentStart : run.ContentEnd;
            Editor.Selection.Select(caret, caret);
            Adapter = new WriterEditingAdapter(Editor);
        }
        public RichTextBox Editor { get; }
        public WriterEditingAdapter Adapter { get; }
        public TextRange AllText => new(Editor.Document.ContentStart, Editor.Document.ContentEnd);
        public string Text => AllText.Text.TrimEnd('\r', '\n');
        public void MoveFocusAwayAndBack() { _other.Focus(); Editor.Focus(); }
        public void BeginCaretClick() => Editor.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice,
            Environment.TickCount, MouseButton.Left) { RoutedEvent = Mouse.PreviewMouseDownEvent });
        public void ReselectCaret(TextPointer caret)
        {
            // Invoke WPF's exact pending-format reset used by TextEditorMouse. This
            // exercises click sequencing without moving the desktop pointer or injecting input.
            typeof(TextSelection).GetMethod("ClearSpringloadFormatting", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(Editor.Selection, null);
            Editor.CaretPosition = caret;
        }
        public void EndCaretClick() => Editor.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice,
            Environment.TickCount, MouseButton.Left) { RoutedEvent = Mouse.MouseUpEvent });
        public void Type(string text)
        {
            Editor.Focus();
            Assert.Same(Editor, Keyboard.FocusedElement);
            var composition = new TextComposition(InputManager.Current, Editor, text);
            TextCompositionManager.StartComposition(composition);
            Editor.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        }
        public void Dispose() { Adapter.Dispose(); _window.Close(); }
    }
}
