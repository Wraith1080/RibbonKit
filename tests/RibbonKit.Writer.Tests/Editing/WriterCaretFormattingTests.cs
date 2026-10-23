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
public sealed class WriterCaretFormattingTests
{
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
