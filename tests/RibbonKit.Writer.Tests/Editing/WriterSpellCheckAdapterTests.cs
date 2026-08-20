using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using RibbonKit.Writer.Tests.Document;
using RibbonKit.Writer.Editing;
using Xunit;

namespace RibbonKit.Writer.Tests.Editing;

public sealed class WriterSpellCheckAdapterTests
{
    [Fact]
    public void NativeSpellCheckReportsSupportAndRestoresOriginalStateOnDispose()
    {
        StaTestHelper.Run(() =>
        {
            using var fixture = CreateFixture();
            var original = SpellCheck.GetIsEnabled(fixture.Editor);
            using var adapter = new WriterSpellCheckAdapter(fixture.Editor);

            Assert.True(adapter.IsSupported);
            Assert.Equal(original, adapter.IsEnabled);
            Assert.True(adapter.Enable());
            Assert.True(SpellCheck.GetIsEnabled(fixture.Editor));
            Assert.True(adapter.Disable());
            Assert.False(SpellCheck.GetIsEnabled(fixture.Editor));
            adapter.Enable();
            adapter.Dispose();
            Assert.Equal(original, SpellCheck.GetIsEnabled(fixture.Editor));
            Assert.True(adapter.IsDisposed);
            Assert.Throws<ObjectDisposedException>(() => adapter.Enable());
        });
    }

    [Fact]
    public void ReadOnlyAndDisabledEditorsRefuseEnableButCanDisable()
    {
        StaTestHelper.Run(() =>
        {
            using var fixture = CreateFixture();
            using var adapter = new WriterSpellCheckAdapter(fixture.Editor);
            fixture.Editor.IsReadOnly = true;
            Assert.False(adapter.CanEnable);
            Assert.False(adapter.Enable());
            Assert.True(adapter.Disable());

            fixture.Editor.IsReadOnly = false;
            fixture.Editor.IsEnabled = false;
            Assert.False(adapter.CanEnable);
            Assert.False(adapter.Enable());
            Assert.True(adapter.Disable());
        });
    }

    [Fact]
    public void AvailabilityChangesAreObservableAndDoNotChangeSelection()
    {
        StaTestHelper.Run(() =>
        {
            using var fixture = CreateFixture();
            var run = (Run)((Paragraph)fixture.Editor.Document.Blocks.First()).Inlines.First();
            fixture.Editor.Selection.Select(run.ContentStart, run.ContentEnd);
            using var adapter = new WriterSpellCheckAdapter(fixture.Editor);
            var changes = 0;
            adapter.PropertyChanged += (_, _) => changes++;
            fixture.Editor.IsReadOnly = true;
            fixture.Editor.IsReadOnly = false;
            Assert.True(changes >= 2);
            Assert.Equal("text", fixture.Editor.Selection.Text);
        });
    }

    [Fact]
    public void DirectNativeSpellStateChangesAreObservableOnceAndStopAfterDispose()
    {
        StaTestHelper.Run(() =>
        {
            using var fixture = CreateFixture();
            using var adapter = new WriterSpellCheckAdapter(fixture.Editor);
            var enabledChanges = 0;
            adapter.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(WriterSpellCheckAdapter.IsEnabled))
                    enabledChanges++;
            };

            SpellCheck.SetIsEnabled(fixture.Editor, true);
            Assert.True(adapter.IsEnabled);
            Assert.Equal(1, enabledChanges);
            SpellCheck.SetIsEnabled(fixture.Editor, false);
            Assert.False(adapter.IsEnabled);
            Assert.Equal(2, enabledChanges);

            adapter.Dispose();
            var afterDispose = enabledChanges;
            Assert.False(SpellCheck.GetIsEnabled(fixture.Editor));
            SpellCheck.SetIsEnabled(fixture.Editor, true);
            Assert.Equal(afterDispose, enabledChanges);
            Assert.True(SpellCheck.GetIsEnabled(fixture.Editor));
        });
    }

    private static Fixture CreateFixture()
    {
        var document = new FlowDocument(new Paragraph(new Run("text")));
        var editor = new RichTextBox { Document = document };
        var window = new Window { Content = editor, Width = 180, Height = 120, ShowInTaskbar = false };
        window.Show();
        editor.Focus();
        return new Fixture(window, editor);
    }

    private sealed class Fixture(Window window, RichTextBox editor) : IDisposable
    {
        public RichTextBox Editor { get; } = editor;
        public void Dispose() => window.Close();
    }
}
