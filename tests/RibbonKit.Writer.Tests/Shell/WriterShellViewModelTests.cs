using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Documents;
using RibbonKit.Writer.Models;
using RibbonKit.Writer.Services.Documents;
using RibbonKit.Writer.Services.RecentFiles;
using RibbonKit.Writer.Shell;
using RibbonKit.Writer.Tests.Document;
using Xunit;

namespace RibbonKit.Writer.Tests.Shell;

public sealed class WriterShellViewModelTests
{
    [Fact]
    public async Task TitleTracksUntitledNamedDirtyAndSuccessfulSaveStates()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            using var fixture = new ShellFixture();
            Assert.Equal("Untitled - RibbonKit Writer", fixture.Shell.Title);
            fixture.Shell.MarkEditorDirty();
            Assert.Equal("Untitled * - RibbonKit Writer", fixture.Shell.Title);

            fixture.Dialogs.SaveSelection = new WriterSaveDestination(fixture.File("named.rtf"),
                WriterDocumentFormat.RichText);
            Assert.True(await fixture.Shell.SaveAsAsync());
            Assert.Equal("named.rtf - RibbonKit Writer", fixture.Shell.Title);
            Assert.Equal("Saved RTF (advanced content best effort)", fixture.Shell.StatusText);

            fixture.Shell.MarkEditorDirty();
            Assert.Equal("named.rtf * - RibbonKit Writer", fixture.Shell.Title);
            Assert.True(await fixture.Shell.SaveAsync());
            Assert.Equal("named.rtf - RibbonKit Writer", fixture.Shell.Title);
            Assert.False(fixture.Shell.CurrentDocument.IsDirty);
        });
    }

    [Fact]
    public void InitialRecentEntriesAreLoadedIntoObservableShellState()
    {
        using var directory = new TemporaryDirectory();
        var recentPath = directory.File("recent.json");
        var recentDocument = directory.File("initial.txt");
        Assert.True(new RecentFileService(recentPath).TryAdd(recentDocument,
            WriterDocumentFormat.PlainText));

        using var fixture = new ShellFixture(recentPath, directory);
        Assert.Single(fixture.Shell.RecentEntries);
        Assert.Equal(recentDocument, fixture.Shell.RecentEntries[0].Path);
        Assert.Equal(WriterDocumentFormat.PlainText, fixture.Shell.RecentEntries[0].Format);
    }

    [Fact]
    public async Task OpenSaveAndDuplicateRecentEntriesStayNewestFirst()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            using var fixture = new ShellFixture();
            var first = fixture.File("first.txt");
            var second = fixture.File("second.rtf");
            File.WriteAllText(first, "first");
            File.WriteAllText(second, "second");

            fixture.Dialogs.OpenSelection = new WriterOpenSelection(first, WriterDocumentFormat.PlainText);
            Assert.True(await fixture.Shell.OpenAsync());
            fixture.Dialogs.OpenSelection = new WriterOpenSelection(second, WriterDocumentFormat.RichText);
            Assert.True(await fixture.Shell.OpenAsync());
            fixture.Dialogs.SaveSelection = new WriterSaveDestination(first, WriterDocumentFormat.PlainText);
            fixture.Dialogs.PlainTextFidelity = true;
            Assert.True(await fixture.Shell.SaveAsAsync());

            Assert.Equal(new[] { first, second }, fixture.Shell.RecentEntries.Select(x => x.Path).ToArray());
            Assert.Equal(WriterDocumentFormat.PlainText, fixture.Shell.RecentEntries[0].Format);
        });
    }

    [Fact]
    public async Task DirtySaveBeforeOpenAddsOpenedTargetNewestAndSavedPreviousOnce()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            using var fixture = new ShellFixture();
            var previousPath = fixture.File("previous.rtf");
            fixture.Shell.MarkEditorDirty();
            fixture.Dialogs.SaveSelection = new WriterSaveDestination(previousPath,
                WriterDocumentFormat.RichText);
            Assert.True(await fixture.Shell.SaveAsAsync());
            fixture.Persistence.Saves.Clear();

            fixture.Shell.MarkEditorDirty();
            var targetPath = fixture.File("opened.txt");
            File.WriteAllText(targetPath, "opened");
            fixture.Dialogs.OpenSelection = new WriterOpenSelection(targetPath,
                WriterDocumentFormat.PlainText);
            fixture.Dialogs.Decisions.Enqueue(UnsavedChangesDecision.Save);

            Assert.True(await fixture.Shell.OpenAsync());
            Assert.Equal(new[] { targetPath, previousPath },
                fixture.Shell.RecentEntries.Select(entry => entry.Path).ToArray());
            Assert.Equal(2, fixture.Shell.RecentEntries.Count);
            Assert.Single(fixture.Persistence.Saves);
            Assert.Equal(previousPath, fixture.Persistence.Saves[0].Path);
            Assert.Equal(WriterDocumentFormat.RichText, fixture.Persistence.Saves[0].Format);
        });
    }

    [Fact]
    public async Task DirtyOpenDialogCancellationDoesNotAttemptTransitionOrSave()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            using var fixture = new ShellFixture();
            fixture.Shell.MarkEditorDirty();
            var original = fixture.Shell.CurrentDocument;
            fixture.Dialogs.OpenSelection = null;

            Assert.False(await fixture.Shell.OpenAsync());
            Assert.Same(original, fixture.Shell.CurrentDocument);
            Assert.True(original.IsDirty);
            Assert.Empty(fixture.Dialogs.UnsavedTransitions);
            Assert.Empty(fixture.Persistence.Saves);
            Assert.Equal("Open cancelled", fixture.Shell.StatusText);
        });
    }

    [Fact]
    public async Task DirtySaveBeforeOpenRecentAddsRecentTargetNewest()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            using var fixture = new ShellFixture();
            var previousPath = fixture.File("previous-recent.rtf");
            fixture.Shell.MarkEditorDirty();
            fixture.Dialogs.SaveSelection = new WriterSaveDestination(previousPath,
                WriterDocumentFormat.RichText);
            Assert.True(await fixture.Shell.SaveAsAsync());
            fixture.Persistence.Saves.Clear();

            fixture.Shell.MarkEditorDirty();
            var targetPath = fixture.File("recent-target.txt");
            File.WriteAllText(targetPath, "recent target");
            fixture.Dialogs.Decisions.Enqueue(UnsavedChangesDecision.Save);

            Assert.True(await fixture.Shell.OpenRecentAsync(new RecentFileEntry(targetPath,
                WriterDocumentFormat.PlainText, DateTimeOffset.UtcNow)));
            Assert.Equal(new[] { targetPath, previousPath },
                fixture.Shell.RecentEntries.Select(entry => entry.Path).ToArray());
            Assert.Equal(2, fixture.Shell.RecentEntries.Count);
            Assert.Single(fixture.Persistence.Saves);
            Assert.Equal(previousPath, fixture.Persistence.Saves[0].Path);
        });
    }

    [Fact]
    public async Task RecentPersistenceFailureDoesNotBlockSaveAndIsVisibleInStatus()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            using var directory = new TemporaryDirectory();
            var recentPath = directory.Path;
            using var fixture = new ShellFixture(recentPath, directory);
            fixture.Dialogs.SaveSelection = new WriterSaveDestination(directory.File("saved.rtf"),
                WriterDocumentFormat.RichText);

            Assert.True(await fixture.Shell.SaveAsAsync());
            Assert.False(fixture.Shell.StatusText.Contains("failed", StringComparison.OrdinalIgnoreCase));
            Assert.Contains("recent list unavailable", fixture.Shell.StatusText);
            Assert.Equal(directory.File("saved.rtf"), fixture.Shell.CurrentDocument.Path);
            Assert.False(fixture.Shell.CurrentDocument.IsDirty);
        });
    }

    [Fact]
    public async Task StaleRecentDoesNotReplaceCurrentAndReportsError()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            using var fixture = new ShellFixture();
            var original = fixture.Shell.CurrentDocument;
            var stale = new RecentFileEntry(fixture.File("gone.txt"), WriterDocumentFormat.PlainText,
                DateTimeOffset.UtcNow);

            Assert.False(await fixture.Shell.OpenRecentAsync(stale));
            Assert.Same(original, fixture.Shell.CurrentDocument);
            Assert.Equal("Open recent failed", fixture.Shell.StatusText);
            Assert.Single(fixture.Dialogs.Errors);
        });
    }

    [Fact]
    public async Task FailedRecentLoadDoesNotReplaceCurrentAndReportsError()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            using var fixture = new ShellFixture();
            var path = fixture.File("broken.rtf");
            File.WriteAllText(path, "not valid RTF");
            fixture.Persistence.LoadHandler = (_, _, _) => Task.FromException<WriterDocument?>(
                new InvalidDataException("broken document"));
            var original = fixture.Shell.CurrentDocument;

            Assert.False(await fixture.Shell.OpenRecentAsync(new RecentFileEntry(path,
                WriterDocumentFormat.RichText, DateTimeOffset.UtcNow)));
            Assert.Same(original, fixture.Shell.CurrentDocument);
            Assert.Equal("Open failed", fixture.Shell.StatusText);
            Assert.Contains("broken document", fixture.Dialogs.Errors.Single());
        });
    }

    [Theory]
    [InlineData(UnsavedChangesDecision.Cancel, false)]
    [InlineData(UnsavedChangesDecision.Discard, true)]
    [InlineData(UnsavedChangesDecision.Save, true)]
    public async Task NewDecisionMatrixPreservesOrReplacesCurrentDocument(UnsavedChangesDecision decision,
        bool expected)
    {
        await StaTestHelper.RunAsync(async () =>
        {
            using var fixture = new ShellFixture();
            fixture.Shell.MarkEditorDirty();
            fixture.Dialogs.Decisions.Enqueue(decision);
            fixture.Dialogs.SaveSelection = new WriterSaveDestination(fixture.File("before-new.rtf"),
                WriterDocumentFormat.RichText);
            var original = fixture.Shell.CurrentDocument;

            Assert.Equal(expected, await fixture.Shell.NewAsync());
            if (expected)
            {
                Assert.NotSame(original, fixture.Shell.CurrentDocument);
                Assert.False(fixture.Shell.CurrentDocument.IsDirty);
            }
            else
            {
                Assert.Same(original, fixture.Shell.CurrentDocument);
                Assert.True(original.IsDirty);
                Assert.Equal("New cancelled or save failed", fixture.Shell.StatusText);
            }
        });
    }

    [Theory]
    [InlineData(UnsavedChangesDecision.Cancel, false)]
    [InlineData(UnsavedChangesDecision.Discard, true)]
    [InlineData(UnsavedChangesDecision.Save, true)]
    public async Task OpenDecisionMatrixPreservesOrReplacesCurrentDocument(UnsavedChangesDecision decision,
        bool expected)
    {
        await StaTestHelper.RunAsync(async () =>
        {
            using var fixture = new ShellFixture();
            var path = fixture.File("opened.txt");
            File.WriteAllText(path, "opened");
            fixture.Persistence.LoadHandler = (_, _, _) => Task.FromResult<WriterDocument?>(
                new WriterDocument(new FlowDocument()));
            fixture.Dialogs.OpenSelection = new WriterOpenSelection(path, WriterDocumentFormat.PlainText);
            fixture.Dialogs.Decisions.Enqueue(decision);
            fixture.Dialogs.SaveSelection = new WriterSaveDestination(fixture.File("before-open.rtf"),
                WriterDocumentFormat.RichText);
            fixture.Shell.MarkEditorDirty();
            var original = fixture.Shell.CurrentDocument;

            Assert.Equal(expected, await fixture.Shell.OpenAsync());
            if (expected)
            {
                Assert.NotSame(original, fixture.Shell.CurrentDocument);
                Assert.Equal(path, fixture.Shell.CurrentDocument.Path);
            }
            else
            {
                Assert.Same(original, fixture.Shell.CurrentDocument);
                Assert.True(original.IsDirty);
            }
        });
    }

    [Theory]
    [InlineData(UnsavedChangesDecision.Cancel, false, true)]
    [InlineData(UnsavedChangesDecision.Discard, true, true)]
    [InlineData(UnsavedChangesDecision.Save, true, false)]
    public async Task CloseDecisionMatrixPromptsOnceAndPreservesDirtyState(UnsavedChangesDecision decision,
        bool expected, bool remainsDirty)
    {
        await StaTestHelper.RunAsync(async () =>
        {
            using var fixture = new ShellFixture();
            fixture.Shell.MarkEditorDirty();
            fixture.Dialogs.Decisions.Enqueue(decision);
            fixture.Dialogs.SaveSelection = new WriterSaveDestination(fixture.File("close.rtf"),
                WriterDocumentFormat.RichText);

            Assert.Equal(expected, await fixture.Shell.RequestCloseAsync());
            Assert.Equal(remainsDirty, fixture.Shell.CurrentDocument.IsDirty);
            Assert.Single(fixture.Dialogs.UnsavedTransitions);
            Assert.Equal(DocumentTransition.Close, fixture.Dialogs.UnsavedTransitions[0]);
        });
    }

    [Fact]
    public async Task UntitledSaveDestinationCancellationLeavesDocumentUnchanged()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            using var fixture = new ShellFixture();
            fixture.Shell.MarkEditorDirty();
            var original = fixture.Shell.CurrentDocument;
            fixture.Dialogs.SaveSelection = null;

            Assert.False(await fixture.Shell.SaveAsync());
            Assert.Same(original, fixture.Shell.CurrentDocument);
            Assert.True(original.IsDirty);
            Assert.Equal("Save cancelled", fixture.Shell.StatusText);
            Assert.Empty(fixture.Persistence.Saves);
        });
    }

    [Fact]
    public async Task UntitledSaveSuccessCommitsDestinationAndClearsDirty()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            using var fixture = new ShellFixture();
            fixture.Shell.MarkEditorDirty();
            var destination = fixture.File("untitled.txt");
            fixture.Dialogs.SaveSelection = new WriterSaveDestination(destination,
                WriterDocumentFormat.PlainText);
            fixture.Dialogs.PlainTextFidelity = true;

            Assert.True(await fixture.Shell.SaveAsync());
            Assert.Equal(destination, fixture.Shell.CurrentDocument.Path);
            Assert.Equal(WriterDocumentFormat.PlainText, fixture.Shell.CurrentDocument.Format);
            Assert.False(fixture.Shell.CurrentDocument.IsDirty);
            Assert.Equal("Saved TXT; formatting and page content are not preserved", fixture.Shell.StatusText);
        });
    }

    [Fact]
    public async Task SaveAsPlainTextWarningRejectsBeforePersistenceAndAcceptsAccurately()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            using var fixture = new ShellFixture();
            fixture.Shell.MarkEditorDirty();
            var destination = fixture.File("plain.txt");
            fixture.Dialogs.SaveSelection = new WriterSaveDestination(destination,
                WriterDocumentFormat.PlainText);
            fixture.Dialogs.PlainTextFidelity = false;

            Assert.False(await fixture.Shell.SaveAsAsync());
            Assert.Equal("Save As cancelled", fixture.Shell.StatusText);
            Assert.Empty(fixture.Persistence.Saves);
            Assert.Equal(1, fixture.Dialogs.PlainTextWarningCalls);
            Assert.True(fixture.Shell.CurrentDocument.IsDirty);

            fixture.Dialogs.PlainTextFidelity = true;
            Assert.True(await fixture.Shell.SaveAsAsync());
            var save = Assert.Single(fixture.Persistence.Saves);
            Assert.Equal(destination, save.Path);
            Assert.Equal(WriterDocumentFormat.PlainText, save.Format);
            Assert.Equal(2, fixture.Dialogs.PlainTextWarningCalls);
            Assert.False(fixture.Shell.CurrentDocument.IsDirty);
        });
    }

    [Fact]
    public async Task SaveAsCancellationAndPersistenceFailurePreserveIdentityAndDirtyState()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            using var fixture = new ShellFixture();
            fixture.Shell.MarkEditorDirty();
            var original = fixture.Shell.CurrentDocument;
            fixture.Dialogs.SaveSelection = null;
            Assert.False(await fixture.Shell.SaveAsAsync());
            Assert.Same(original, fixture.Shell.CurrentDocument);
            Assert.Null(original.Path);
            Assert.True(original.IsDirty);
            Assert.Equal(0, fixture.Dialogs.PlainTextWarningCalls);

            fixture.Dialogs.SaveSelection = new WriterSaveDestination(fixture.File("failed.rtf"),
                WriterDocumentFormat.RichText);
            fixture.Persistence.SaveResult = false;
            Assert.False(await fixture.Shell.SaveAsAsync());
            Assert.Same(original, fixture.Shell.CurrentDocument);
            Assert.Null(original.Path);
            Assert.True(original.IsDirty);
            Assert.Equal("Save failed; document unchanged", fixture.Shell.StatusText);
        });
    }

    [Fact]
    public async Task RtfSaveReportsBestEffortCompletion()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            using var fixture = new ShellFixture();
            fixture.Shell.MarkEditorDirty();
            fixture.Dialogs.SaveSelection = new WriterSaveDestination(fixture.File("document.rtf"),
                WriterDocumentFormat.RichText);

            Assert.True(await fixture.Shell.SaveAsAsync());
            Assert.Equal("Saved RTF (advanced content best effort)", fixture.Shell.StatusText);
            Assert.Equal(WriterDocumentFormat.RichText, fixture.Shell.CurrentDocument.Format);
        });
    }

    [Theory]
    [InlineData(null, 1, null, null)]
    [InlineData("draft", 1, "draft.rtf", WriterDocumentFormat.RichText)]
    [InlineData("draft", 2, "draft.txt", WriterDocumentFormat.PlainText)]
    [InlineData("draft.rtf", 2, "draft.txt", WriterDocumentFormat.PlainText)]
    [InlineData("draft.txt", 1, "draft.rtf", WriterDocumentFormat.RichText)]
    public void SaveDialogSelectionKeepsFilterAndExtensionInAgreement(string? path, int filterIndex,
        string? expectedPath, WriterDocumentFormat? expectedFormat)
    {
        var selection = WriterSaveDialogSelection.Resolve(path, filterIndex);
        if (expectedPath is null)
        {
            Assert.Null(selection);
            return;
        }
        Assert.NotNull(selection);
        Assert.Equal(expectedPath, selection!.Path);
        Assert.Equal(expectedFormat, selection.Format);
    }

    [Fact]
    public async Task FalseOperationGetsCancellationStatusWhenNoInnerStatusWasSet()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            using var fixture = new ShellFixture();
            fixture.Shell.MarkEditorDirty();
            fixture.Dialogs.Decisions.Enqueue(UnsavedChangesDecision.Cancel);

            Assert.False(await fixture.Shell.NewAsync());
            Assert.Equal("New cancelled or save failed", fixture.Shell.StatusText);
        });
    }

    [Fact]
    public async Task ExplicitInnerCancellationStatusIsPreserved()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            using var fixture = new ShellFixture();
            fixture.Shell.MarkEditorDirty();
            fixture.Dialogs.SaveSelection = null;

            Assert.False(await fixture.Shell.SaveAsAsync());
            Assert.Equal("Save As cancelled", fixture.Shell.StatusText);
        });
    }

    [Fact]
    public async Task OperationGateDisablesCommandsDuringAnIncompleteOperation()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            using var fixture = new ShellFixture();
            var pending = new TaskCompletionSource<WriterOpenSelection?>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            fixture.Dialogs.OpenHandler = _ => pending.Task;
            var first = fixture.Shell.OpenAsync();
            await Task.Yield();
            Assert.True(fixture.Shell.IsBusy);
            Assert.False(fixture.Shell.CanOperate);
            Assert.False(fixture.Shell.NewCommand.CanExecute(null));
            Assert.False(fixture.Shell.OpenCommand.CanExecute(null));
            Assert.False(fixture.Shell.SaveCommand.CanExecute(null));
            Assert.False(fixture.Shell.SaveAsCommand.CanExecute(null));
            Assert.False(fixture.Shell.ExitCommand.CanExecute(null));
            Assert.False(await fixture.Shell.SaveAsync());
            pending.SetResult(null);
            Assert.False(await first);
            Assert.False(fixture.Shell.IsBusy);
            Assert.True(fixture.Shell.CanOperate);
            Assert.True(fixture.Shell.NewCommand.CanExecute(null));
        });
    }

    [Fact]
    public async Task ExitRequestsExactlyOneDecisionAndOneExitEvent()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            using var fixture = new ShellFixture();
            fixture.Shell.MarkEditorDirty();
            fixture.Dialogs.Decisions.Enqueue(UnsavedChangesDecision.Save);
            fixture.Dialogs.SaveSelection = new WriterSaveDestination(fixture.File("exit.rtf"),
                WriterDocumentFormat.RichText);
            var exitRequests = 0;
            fixture.Shell.ExitRequested += (_, _) => exitRequests++;

            Assert.True(await fixture.Shell.RequestExitAsync());
            Assert.Equal(1, exitRequests);
            Assert.Single(fixture.Dialogs.UnsavedTransitions);
            Assert.Equal(DocumentTransition.Close, fixture.Dialogs.UnsavedTransitions[0]);
            Assert.False(fixture.Shell.CurrentDocument.IsDirty);
        });
    }

    [Theory]
    [InlineData("New", true, true)]
    [InlineData("New", false, true)]
    [InlineData("New", false, false)]
    [InlineData("Open", true, true)]
    [InlineData("Open", false, true)]
    [InlineData("Open", false, false)]
    [InlineData("Close", true, true)]
    [InlineData("Close", false, true)]
    [InlineData("Close", false, false)]
    public async Task SaveBeforeTransitionUntitledTxtMatrixPreservesOrCommitsExactly(string operation,
        bool acceptFidelity, bool chooseDestination)
    {
        await StaTestHelper.RunAsync(async () =>
        {
            using var fixture = new ShellFixture();
            var original = fixture.Shell.CurrentDocument;
            original.MarkDirty();
            var destination = fixture.File($"before-{operation.ToLowerInvariant()}.txt");
            fixture.Dialogs.Decisions.Enqueue(UnsavedChangesDecision.Save);
            fixture.Dialogs.SaveSelection = chooseDestination
                ? new WriterSaveDestination(destination, WriterDocumentFormat.PlainText)
                : null;
            fixture.Dialogs.PlainTextFidelity = acceptFidelity;

            var openedPath = fixture.File("transition-opened.txt");
            File.WriteAllText(openedPath, "opened");
            fixture.Dialogs.OpenSelection = new WriterOpenSelection(openedPath,
                WriterDocumentFormat.PlainText);
            var expected = chooseDestination && acceptFidelity;

            var result = operation switch
            {
                "New" => await fixture.Shell.NewAsync(),
                "Open" => await fixture.Shell.OpenAsync(),
                "Close" => await fixture.Shell.RequestCloseAsync(),
                _ => throw new ArgumentOutOfRangeException(nameof(operation))
            };

            Assert.Equal(expected, result);
            Assert.Single(fixture.Dialogs.UnsavedTransitions);
            Assert.Equal(Enum.Parse<DocumentTransition>(operation), fixture.Dialogs.UnsavedTransitions[0]);
            Assert.Equal(chooseDestination ? 1 : 0, fixture.Dialogs.PlainTextWarningCalls);
            if (!expected)
            {
                Assert.Same(original, fixture.Shell.CurrentDocument);
                Assert.True(original.IsDirty);
                Assert.Empty(fixture.Persistence.Saves);
                Assert.Equal($"{operation} cancelled or save failed", fixture.Shell.StatusText);
                return;
            }

            var save = Assert.Single(fixture.Persistence.Saves);
            Assert.Equal(destination, save.Path);
            Assert.Equal(WriterDocumentFormat.PlainText, save.Format);
            Assert.False(original.IsDirty);
            Assert.Contains(fixture.Shell.RecentEntries, entry =>
                string.Equals(entry.Path, destination, StringComparison.OrdinalIgnoreCase));
            switch (operation)
            {
                case "New":
                    Assert.NotSame(original, fixture.Shell.CurrentDocument);
                    Assert.Equal("New document created", fixture.Shell.StatusText);
                    break;
                case "Open":
                    Assert.NotSame(original, fixture.Shell.CurrentDocument);
                    Assert.Equal(openedPath, fixture.Shell.CurrentDocument.Path);
                    Assert.Equal("Opened TXT; formatting and page content are not present",
                        fixture.Shell.StatusText);
                    break;
                case "Close":
                    Assert.Same(original, fixture.Shell.CurrentDocument);
                    Assert.Equal("Close approved", fixture.Shell.StatusText);
                    break;
            }
        });
    }

    [Theory]
    [InlineData("New")]
    [InlineData("Close")]
    public async Task SaveBeforeTransitionPersistenceFailurePreservesDirtyDocument(string operation)
    {
        await StaTestHelper.RunAsync(async () =>
        {
            using var fixture = new ShellFixture();
            var original = fixture.Shell.CurrentDocument;
            original.MarkDirty();
            fixture.Dialogs.Decisions.Enqueue(UnsavedChangesDecision.Save);
            fixture.Dialogs.SaveSelection = new WriterSaveDestination(fixture.File("failed-save.rtf"),
                WriterDocumentFormat.RichText);
            fixture.Persistence.SaveResult = false;

            var result = operation == "New"
                ? await fixture.Shell.NewAsync()
                : await fixture.Shell.RequestCloseAsync();

            Assert.False(result);
            Assert.Same(original, fixture.Shell.CurrentDocument);
            Assert.True(original.IsDirty);
            Assert.Single(fixture.Persistence.Saves);
            Assert.Equal($"{operation} cancelled or save failed", fixture.Shell.StatusText);
            Assert.Empty(fixture.Shell.RecentEntries);
        });
    }

    [Fact]
    public async Task ImplicitSavedPreviousDocumentGetsRecentEntryEvenWhenOpenLoadFails()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            using var fixture = new ShellFixture();
            var destination = fixture.File("saved-before-failed-open.rtf");
            fixture.Shell.MarkEditorDirty();
            fixture.Dialogs.Decisions.Enqueue(UnsavedChangesDecision.Save);
            fixture.Dialogs.SaveSelection = new WriterSaveDestination(destination,
                WriterDocumentFormat.RichText);
            fixture.Dialogs.OpenSelection = new WriterOpenSelection(fixture.File("missing-load.rtf"),
                WriterDocumentFormat.RichText);
            fixture.Persistence.LoadedDocument = null;

            Assert.False(await fixture.Shell.OpenAsync());
            Assert.False(fixture.Shell.CurrentDocument.IsDirty);
            Assert.Contains(fixture.Shell.RecentEntries, entry =>
                string.Equals(entry.Path, destination, StringComparison.OrdinalIgnoreCase));
            Assert.Equal("Open cancelled or load failed", fixture.Shell.StatusText);
        });
    }

    [Fact]
    public async Task DirtyAndIdentityChangesNotifyTitleWithoutRaisingCurrentDocument()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            using var fixture = new ShellFixture();
            var properties = new List<string?>();
            fixture.Shell.PropertyChanged += (_, e) => properties.Add(e.PropertyName);

            fixture.Shell.MarkEditorDirty();
            Assert.DoesNotContain(nameof(WriterShellViewModel.CurrentDocument), properties);
            Assert.Contains(nameof(WriterShellViewModel.Title), properties);

            properties.Clear();
            var savePath = fixture.File("identity.rtf");
            fixture.Dialogs.SaveSelection = new WriterSaveDestination(savePath,
                WriterDocumentFormat.RichText);
            Assert.True(await fixture.Shell.SaveAsAsync());
            Assert.DoesNotContain(nameof(WriterShellViewModel.CurrentDocument), properties);
            Assert.Contains(nameof(WriterShellViewModel.Title), properties);

            properties.Clear();
            Assert.True(await fixture.Shell.NewAsync());
            Assert.Equal(1, properties.Count(property =>
                property == nameof(WriterShellViewModel.CurrentDocument)));

            properties.Clear();
            var openPath = fixture.File("replacement.txt");
            File.WriteAllText(openPath, "replacement");
            fixture.Dialogs.OpenSelection = new WriterOpenSelection(openPath,
                WriterDocumentFormat.PlainText);
            Assert.True(await fixture.Shell.OpenAsync());
            Assert.Equal(1, properties.Count(property =>
                property == nameof(WriterShellViewModel.CurrentDocument)));
        });
    }

    private sealed class ShellFixture : IDisposable
    {
        private readonly bool _ownsDirectory;

        public ShellFixture(string? recentPath = null, TemporaryDirectory? directory = null)
        {
            Directory = directory ?? new TemporaryDirectory();
            _ownsDirectory = directory is null;
            RecentPath = recentPath ?? Directory.File("recent.json");
            Dialogs = new FakeDialogs();
            Persistence = new FakePersistence();
            var session = new WriterDocumentSession(Persistence,
                new WriterUnsavedChangesDecider(Dialogs), new WriterSaveDestinationProvider(Dialogs));
            Shell = new WriterShellViewModel(session, new RecentFileService(RecentPath), Dialogs);
        }

        public TemporaryDirectory Directory { get; }
        public string RecentPath { get; }
        public FakeDialogs Dialogs { get; }
        public FakePersistence Persistence { get; }
        public WriterShellViewModel Shell { get; }

        public string File(string name) => Directory.File(name);

        public void Dispose()
        {
            Shell.Dispose();
            if (_ownsDirectory)
                Directory.Dispose();
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
                System.IO.Directory.Delete(Path, recursive: true);
        }
    }

    private sealed class FakeDialogs : IWriterDialogService
    {
        public WriterOpenSelection? OpenSelection { get; set; }
        public Func<CancellationToken, Task<WriterOpenSelection?>>? OpenHandler { get; set; }
        public WriterSaveDestination? SaveSelection { get; set; }
        public Func<WriterDocument, CancellationToken, Task<WriterSaveDestination?>>? SaveHandler { get; set; }
        public bool PlainTextFidelity { get; set; }
        public Queue<UnsavedChangesDecision> Decisions { get; } = new();
        public List<DocumentTransition> UnsavedTransitions { get; } = new();
        public List<string> Errors { get; } = new();
        public List<string> Infos { get; } = new();
        public int SaveDestinationCalls { get; private set; }
        public int PlainTextWarningCalls { get; private set; }

        public Task<WriterOpenSelection?> ShowOpenAsync(CancellationToken cancellationToken = default) =>
            OpenHandler?.Invoke(cancellationToken) ?? Task.FromResult(OpenSelection);

        public Task<WriterSaveDestination?> ShowSaveAsync(WriterDocument document,
            CancellationToken cancellationToken = default)
        {
            SaveDestinationCalls++;
            return SaveHandler?.Invoke(document, cancellationToken) ?? Task.FromResult(SaveSelection);
        }

        public Task<UnsavedChangesDecision> ConfirmUnsavedAsync(WriterDocument document,
            DocumentTransition transition, CancellationToken cancellationToken = default)
        {
            UnsavedTransitions.Add(transition);
            return Task.FromResult(Decisions.Count == 0 ? UnsavedChangesDecision.Cancel : Decisions.Dequeue());
        }

        public Task<bool> ConfirmPlainTextFidelityAsync(CancellationToken cancellationToken = default)
        {
            PlainTextWarningCalls++;
            return Task.FromResult(PlainTextFidelity);
        }

        public Task ShowErrorAsync(string message, CancellationToken cancellationToken = default)
        {
            Errors.Add(message);
            return Task.CompletedTask;
        }

        public Task ShowInfoAsync(string message, CancellationToken cancellationToken = default)
        {
            Infos.Add(message);
            return Task.CompletedTask;
        }
    }

    private sealed class FakePersistence : IWriterDocumentPersistence
    {
        public WriterDocument? LoadedDocument { get; set; } = new(new FlowDocument());
        public bool SaveResult { get; set; } = true;
        public Func<string, WriterDocumentFormat, CancellationToken, Task<WriterDocument?>>? LoadHandler { get; set; }
        public Func<WriterDocument, string, WriterDocumentFormat, CancellationToken, Task<bool>>? SaveHandler { get; set; }
        public Collection<(string Path, WriterDocumentFormat Format)> Loads { get; } = new();
        public Collection<(string Path, WriterDocumentFormat Format)> Saves { get; } = new();

        public Task<WriterDocument?> LoadAsync(string path, WriterDocumentFormat format,
            CancellationToken cancellationToken)
        {
            Loads.Add((path, format));
            return LoadHandler?.Invoke(path, format, cancellationToken) ?? Task.FromResult(LoadedDocument);
        }

        public Task<bool> SaveAsync(WriterDocument document, string path, WriterDocumentFormat format,
            CancellationToken cancellationToken)
        {
            Saves.Add((path, format));
            return SaveHandler?.Invoke(document, path, format, cancellationToken) ?? Task.FromResult(SaveResult);
        }
    }
}
