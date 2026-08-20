using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using RibbonKit.Controls;
using RibbonKit.Writer;
using RibbonKit.Writer.Models;
using RibbonKit.Writer.Services.Documents;
using RibbonKit.Writer.Services.RecentFiles;
using RibbonKit.Writer.Shell;
using RibbonKit.Writer.Tests.Document;
using Xunit;

namespace RibbonKit.Writer.Tests.Shell;

public sealed class MainWindowIntegrationTests
{
    [Fact]
    public async Task MainWindowContractAndEditorLifecycleAreWiredOnTheRealTree()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            using var fixture = new WindowFixture(withRecentFile: true);
            fixture.Show();
            await PumpAsync();
            AssertRuntimeContract(fixture);

            var backstage = Assert.IsType<Backstage>(fixture.Ribbon.Backstage);
            var fileOpen = backstage.Items.OfType<BackstageTabItem>().Single(item =>
                AutomationProperties.GetAutomationId(item) == "FileOpen");
            fixture.Ribbon.IsBackstageOpen = true;
            await PumpAsync();
            fileOpen.RaiseEvent(new RoutedEventArgs(BackstageTabItem.ClickEvent));
            Assert.False(fixture.Ribbon.IsBackstageOpen);

            fixture.Ribbon.IsBackstageOpen = true;
            await PumpAsync();
            var recentList = FindVisualDescendants<ItemsControl>(backstage).Single(item =>
                AutomationProperties.GetAutomationId(item) == "RecentList");
            var recentButtons = FindVisualDescendants<Button>(recentList)
                .Where(button => fixture.RecentPaths.Contains(button.Content as string ?? ""))
                .ToArray();
            Assert.Equal(2, recentButtons.Length);
            Assert.Equal(2, recentButtons.Select(button =>
                AutomationProperties.GetAutomationId(button)).Distinct(StringComparer.OrdinalIgnoreCase).Count());
            foreach (var button in recentButtons)
            {
                var buttonPath = Assert.IsType<string>(button.Content);
                Assert.Equal(buttonPath, AutomationProperties.GetAutomationId(button));
                Assert.Equal(buttonPath, AutomationProperties.GetName(button));
                Assert.Same(fixture.Shell.OpenRecentCommand, button.Command);
                Assert.IsType<RecentFileEntry>(button.CommandParameter);
            }

            var recentButton = recentButtons[0];
            var recentEntry = Assert.IsType<RecentFileEntry>(recentButton.CommandParameter);
            var recentPeer = UIElementAutomationPeer.CreatePeerForElement(recentButton)
                ?? new ButtonAutomationPeer(recentButton);
            var invoke = Assert.IsAssignableFrom<IInvokeProvider>(
                recentPeer.GetPattern(PatternInterface.Invoke));
            invoke.Invoke();
            await WaitForShellIdleAsync(fixture.Shell);
            await PumpAsync();
            Assert.Equal(recentEntry.Path, fixture.Shell.CurrentDocument.Path);
            Assert.False(fixture.Ribbon.IsBackstageOpen);

            var original = fixture.Editor.Document;
            Assert.True(await fixture.Shell.NewAsync());
            Assert.NotSame(original, fixture.Editor.Document);
            Assert.Same(fixture.Shell.CurrentDocument.Content, fixture.Editor.Document);
            Assert.False(fixture.Shell.CurrentDocument.IsDirty);

            fixture.Editor.AppendText("typed text");
            Assert.True(fixture.Shell.CurrentDocument.IsDirty);
            Assert.Contains("Untitled *", fixture.Shell.Title);
            Assert.Contains("typed text", TextOf(fixture.Editor.Document));

            var dirtyEditorDocument = fixture.Editor.Document;
            fixture.Dialogs.SaveSelection = new WriterSaveDestination(fixture.File("same-editor.rtf"),
                WriterDocumentFormat.RichText);
            Assert.True(await fixture.Shell.SaveAsAsync());
            Assert.Same(dirtyEditorDocument, fixture.Editor.Document);
            fixture.Shell.MarkEditorDirty();
            Assert.Same(dirtyEditorDocument, fixture.Editor.Document);

            var originalDocument = fixture.Shell.CurrentDocument;
            var originalContent = fixture.Editor.Document;
            var originalText = TextOf(originalContent);
            fixture.Dialogs.OpenSelection = null;
            Assert.False(await fixture.Shell.OpenAsync());
            Assert.Same(originalDocument, fixture.Shell.CurrentDocument);
            Assert.Same(originalContent, fixture.Editor.Document);
            Assert.Equal(originalText, TextOf(fixture.Editor.Document));

            var path = fixture.File("failed.rtf");
            File.WriteAllText(path, "broken");
            fixture.Dialogs.OpenSelection = new WriterOpenSelection(path, WriterDocumentFormat.RichText);
            fixture.Persistence.LoadHandler = (_, _, _) => Task.FromException<WriterDocument?>(
                new InvalidDataException("cannot load"));
            fixture.Dialogs.Decisions.Enqueue(UnsavedChangesDecision.Discard);
            Assert.False(await fixture.Shell.OpenAsync());
            Assert.Same(originalDocument, fixture.Shell.CurrentDocument);
            Assert.Same(originalContent, fixture.Editor.Document);
            Assert.Equal(originalText, TextOf(fixture.Editor.Document));
            Assert.True(originalDocument.IsDirty);

            fixture.Dialogs.Decisions.Enqueue(UnsavedChangesDecision.Cancel);
            fixture.Window.Close();
            await PumpAsync();
            Assert.True(fixture.Window.IsVisible);
            Assert.Equal(1, fixture.Dialogs.UnsavedTransitions.Count(transition =>
                transition == DocumentTransition.Close));

            fixture.Dialogs.Decisions.Enqueue(UnsavedChangesDecision.Discard);
            await fixture.CloseAndWaitAsync();
            Assert.False(fixture.Window.IsVisible);
            Assert.Equal(2, fixture.Dialogs.UnsavedTransitions.Count(transition =>
                transition == DocumentTransition.Close));
            Assert.Equal(DocumentTransition.Close, fixture.Dialogs.UnsavedTransitions[^1]);

            await AssertExitCloseAsync(UnsavedChangesDecision.Save);
            await AssertExitCloseAsync(UnsavedChangesDecision.Discard);
            await AssertCleanCloseAsync();
        });
    }

    private static void AssertRuntimeContract(WindowFixture fixture)
    {
        var window = fixture.Window;
        var ribbon = fixture.Ribbon;
        var editor = fixture.Editor;
        var dock = Assert.IsType<DockPanel>(window.Content);
        Assert.Equal("WriterWindow", AutomationProperties.GetAutomationId(dock));
        Assert.Equal(3, dock.Children.Count);
        Assert.Same(ribbon, dock.Children[0]);
        Assert.Equal("Bottom", DockPanel.GetDock(dock.Children[1]).ToString());
        Assert.IsType<StatusBar>(dock.Children[1]);
        Assert.Same(editor, dock.Children[2]);
        Assert.Equal("DocumentEditor", AutomationProperties.GetAutomationId(editor));
        Assert.Equal("Document editor", AutomationProperties.GetName(editor));
        Assert.Equal("MainRibbon", AutomationProperties.GetAutomationId(ribbon));
        Assert.Equal("Main ribbon", AutomationProperties.GetName(ribbon));
        var status = Assert.IsType<StatusBar>(dock.Children[1]);
        Assert.Equal("Status", AutomationProperties.GetAutomationId(status));
        Assert.Equal("Status", AutomationProperties.GetName(status));

        var backstage = Assert.IsType<Backstage>(ribbon.Backstage);
        Assert.Equal("WriterBackstage", AutomationProperties.GetAutomationId(backstage));
        Assert.Equal("File", AutomationProperties.GetName(backstage));
        Assert.Null(ribbon.ApplicationMenu);
        var fileActions = backstage.Items.OfType<BackstageTabItem>().Where(item => item.IsButton).ToArray();
        Assert.Equal(new[] { "New", "Open", "Save", "Save As", "Exit" },
            fileActions.Select(item => item.Header?.ToString()).ToArray());
        Assert.Equal(new[] { "FileNew", "FileOpen", "FileSave", "FileSaveAs", "FileExit" },
            fileActions.Select(AutomationProperties.GetAutomationId).ToArray());
        Assert.All(fileActions, item => Assert.False(string.IsNullOrWhiteSpace(
            AutomationProperties.GetName(item))));
        Assert.All(fileActions, item => Assert.NotNull(item.Command));

        var home = Assert.Single(ribbon.Tabs);
        Assert.Equal("Home", home.Header);
        var group = Assert.Single(home.Groups);
        var homeActions = group.Items.OfType<RibbonButton>().ToArray();
        Assert.Equal(new[] { "HomeNew", "HomeOpen", "HomeSave", "HomeSaveAs" },
            homeActions.Select(AutomationProperties.GetAutomationId).ToArray());
        Assert.Equal(new[] { "New", "Open", "Save", "Save As" },
            homeActions.Select(AutomationProperties.GetName).ToArray());
        Assert.All(homeActions, item => Assert.NotNull(item.Command));
        var qatSave = Assert.IsType<RibbonButton>(Assert.Single(ribbon.QuickAccessItems));
        Assert.Equal("QatSave", AutomationProperties.GetAutomationId(qatSave));
        Assert.Equal("Save", AutomationProperties.GetName(qatSave));
        Assert.Same(fixture.Shell.SaveCommand, qatSave.Command);
        var saveResource = fixture.Window.TryFindResource("Icon.WriterSave");
        Assert.IsType<DrawingImage>(saveResource);
        Assert.Same(saveResource, qatSave.Icon);

        var bindings = window.InputBindings.OfType<KeyBinding>().ToArray();
        Assert.Contains(bindings, binding => binding.Key == Key.N && binding.Modifiers == ModifierKeys.Control &&
            ReferenceEquals(binding.Command, fixture.Shell.NewCommand));
        Assert.Contains(bindings, binding => binding.Key == Key.O && binding.Modifiers == ModifierKeys.Control &&
            ReferenceEquals(binding.Command, fixture.Shell.OpenCommand));
        Assert.Contains(bindings, binding => binding.Key == Key.S && binding.Modifiers == ModifierKeys.Control &&
            ReferenceEquals(binding.Command, fixture.Shell.SaveCommand));
        Assert.Contains(bindings, binding => binding.Key == Key.S &&
            binding.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift) &&
            ReferenceEquals(binding.Command, fixture.Shell.SaveAsCommand));
    }

    private static string TextOf(FlowDocument document) =>
        new TextRange(document.ContentStart, document.ContentEnd).Text;

    private static IEnumerable<T> FindVisualDescendants<T>(DependencyObject root) where T : DependencyObject
    {
        if (root is T match)
            yield return match;
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            foreach (var descendant in FindVisualDescendants<T>(child))
                yield return descendant;
        }
    }

    private static async Task PumpAsync()
    {
        await Dispatcher.Yield(DispatcherPriority.Loaded);
        await Task.Delay(300);
        await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
    }

    private static async Task WaitForShellIdleAsync(WriterShellViewModel shell)
    {
        for (var attempt = 0; attempt < 100; attempt++)
        {
            if (!shell.IsBusy)
                return;
            await Task.Delay(10);
        }
        Assert.False(shell.IsBusy);
    }

    private static async Task AssertExitCloseAsync(UnsavedChangesDecision decision)
    {
        using var fixture = new WindowFixture();
        fixture.Show();
        await PumpAsync();
        fixture.Shell.MarkEditorDirty();
        fixture.Dialogs.Decisions.Enqueue(decision);
        if (decision == UnsavedChangesDecision.Save)
        {
            fixture.Dialogs.SaveSelection = new WriterSaveDestination(fixture.File("exit.rtf"),
                WriterDocumentFormat.RichText);
        }

        var exitRequests = 0;
        fixture.Shell.ExitRequested += (_, _) => exitRequests++;
        var closed = fixture.ClosedTask();
        Assert.True(await fixture.Shell.RequestExitAsync());
        await closed;
        Assert.False(fixture.Window.IsVisible);
        Assert.Equal(1, exitRequests);
        Assert.Single(fixture.Dialogs.UnsavedTransitions);
        Assert.Equal(DocumentTransition.Close, fixture.Dialogs.UnsavedTransitions[0]);
        if (decision == UnsavedChangesDecision.Save)
            Assert.False(fixture.Shell.CurrentDocument.IsDirty);
    }

    private static async Task AssertCleanCloseAsync()
    {
        using var fixture = new WindowFixture();
        fixture.Show();
        await PumpAsync();
        await fixture.CloseAndWaitAsync();
        Assert.Empty(fixture.Dialogs.UnsavedTransitions);
        Assert.False(fixture.Window.IsVisible);
        Assert.True(fixture.Shell.CanOperate);
        Assert.True(fixture.Shell.NewCommand.CanExecute(null));
    }

    private sealed class WindowFixture : IDisposable
    {
        private readonly TemporaryDirectory _directory = new();
        private bool _disposed;
        public WindowFixture(bool withRecentFile = false)
        {
            Dialogs = new FakeDialogs();
            Persistence = new FakePersistence();
            var recentPath = _directory.File("recent.json");
            if (withRecentFile)
            {
                var service = new RecentFileService(recentPath);
                for (var index = 1; index <= 2; index++)
                {
                    var path = _directory.File($"recent-{index}.txt");
                    System.IO.File.WriteAllText(path, $"recent {index}");
                    Assert.True(service.TryAdd(path, WriterDocumentFormat.PlainText));
                    RecentPaths.Add(path);
                }
            }
            var session = new WriterDocumentSession(Persistence,
                new WriterUnsavedChangesDecider(Dialogs), new WriterSaveDestinationProvider(Dialogs));
            Shell = new WriterShellViewModel(session, new RecentFileService(recentPath), Dialogs);
            Window = new MainWindow(Shell);
        }

        public FakeDialogs Dialogs { get; }
        public FakePersistence Persistence { get; }
        public WriterShellViewModel Shell { get; }
        public MainWindow Window { get; }
        public List<string> RecentPaths { get; } = new();
        public Ribbon Ribbon => Assert.IsType<Ribbon>(Window.FindName("MainRibbon"));
        public RichTextBox Editor => Assert.IsType<RichTextBox>(Window.FindName("DocumentEditor"));

        public string File(string name) => _directory.File(name);

        public void Show()
        {
            Window.WindowStartupLocation = WindowStartupLocation.Manual;
            Window.Left = -10000;
            Window.Top = -10000;
            Window.ShowInTaskbar = false;
            Window.Opacity = 0.01;
            Window.Show();
            Window.UpdateLayout();
        }

        public Task ClosedTask()
        {
            if (!Window.IsVisible)
                return Task.CompletedTask;
            var completion = new TaskCompletionSource<object?>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            EventHandler? handler = null;
            handler = (_, _) =>
            {
                Window.Closed -= handler;
                completion.TrySetResult(null);
            };
            Window.Closed += handler;
            return completion.Task;
        }

        public async Task CloseAndWaitAsync()
        {
            var closed = ClosedTask();
            Window.Close();
            await closed;
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            if (Window.IsVisible)
                Window.Close();
            // MainWindow intentionally does not dispose an injected shell. This fixture is the
            // caller and owns cleanup after asserting that the shell survived window closure.
            Shell.Dispose();
            _directory.Dispose();
        }
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "RibbonKitWriterTests",
                Guid.NewGuid().ToString("N"));
            System.IO.Directory.CreateDirectory(Path);
        }

        public string Path { get; }
        public string File(string name) => System.IO.Path.Combine(Path, name);
        public void Dispose()
        {
            if (System.IO.Directory.Exists(Path))
                System.IO.Directory.Delete(Path, true);
        }
    }

    private sealed class FakeDialogs : IWriterDialogService
    {
        public WriterOpenSelection? OpenSelection { get; set; }
        public WriterSaveDestination? SaveSelection { get; set; }
        public bool PlainTextFidelity { get; set; }
        public Queue<UnsavedChangesDecision> Decisions { get; } = new();
        public List<DocumentTransition> UnsavedTransitions { get; } = new();
        public Task<WriterOpenSelection?> ShowOpenAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(OpenSelection);
        public Task<WriterSaveDestination?> ShowSaveAsync(WriterDocument document,
            CancellationToken cancellationToken = default) => Task.FromResult(SaveSelection);
        public Task<UnsavedChangesDecision> ConfirmUnsavedAsync(WriterDocument document,
            DocumentTransition transition, CancellationToken cancellationToken = default) =>
            ConfirmUnsavedCore(transition);

        private Task<UnsavedChangesDecision> ConfirmUnsavedCore(DocumentTransition transition)
        {
            UnsavedTransitions.Add(transition);
            return Task.FromResult(Decisions.Count == 0 ? UnsavedChangesDecision.Cancel : Decisions.Dequeue());
        }
        public Task<bool> ConfirmPlainTextFidelityAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(PlainTextFidelity);
        public Task ShowErrorAsync(string message, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
        public Task ShowInfoAsync(string message, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class FakePersistence : IWriterDocumentPersistence
    {
        public Func<string, WriterDocumentFormat, CancellationToken, Task<WriterDocument?>>? LoadHandler { get; set; }
        public Task<WriterDocument?> LoadAsync(string path, WriterDocumentFormat format,
            CancellationToken cancellationToken) => LoadHandler?.Invoke(path, format, cancellationToken) ??
            Task.FromResult<WriterDocument?>(new WriterDocument(new FlowDocument()));
        public Task<bool> SaveAsync(WriterDocument document, string path, WriterDocumentFormat format,
            CancellationToken cancellationToken) => Task.FromResult(true);
    }
}
