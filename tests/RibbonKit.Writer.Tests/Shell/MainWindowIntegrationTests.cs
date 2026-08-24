using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Reflection;
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
using RibbonKit.Writer.Editing;
using RibbonKit.Writer;
using RibbonKit.Writer.Models;
using RibbonKit.Writer.Page;
using RibbonKit.Writer.Preview;
using RibbonKit.Writer.Printing;
using RibbonKit.Writer.Services.Documents;
using RibbonKit.Writer.Services.RecentFiles;
using RibbonKit.Writer.Shell;
using RibbonKit.Writer.Tests.Document;
using RibbonKit.Writer.View;
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
            await AssertBackstageFileCommandsRestoreEditorFocusAsync(fixture);
            var surface = Assert.IsType<WriterEditorSurface>(fixture.Window.FindName("EditorSurface"));
            var settings = DocumentPageSettings.A4(DocumentPageOrientation.Landscape,
                new DocumentPageMargins(42, 54, 42, 54));
            Assert.True(fixture.Shell.CurrentDocument.SetPageSettings(settings));
            await Dispatcher.Yield(DispatcherPriority.Render);
            Assert.Same(fixture.Editor, surface.Editor);
            Assert.Equal(settings.WidthDip, fixture.Editor.Document.PageWidth);
            Assert.Equal(settings.HeightDip, fixture.Editor.Document.PageHeight);
            Assert.Equal(settings.Margins.LeftDip, fixture.Editor.Document.PagePadding.Left, 4);
            Assert.Equal(settings.Margins.TopDip, fixture.Editor.Document.PagePadding.Top, 4);
            Assert.Equal(settings.WidthDip, surface.PaperWidthDip, 4);
            Assert.True(fixture.Shell.CurrentDocument.SetPageSettings(DocumentPageSettings.Letter()));
            await Dispatcher.Yield(DispatcherPriority.Render);
            AssertWriterIconCatalog(fixture);
            await AssertEditingRibbonControlsAsync(fixture);
            await AssertPageViewIntegrationAsync(fixture);

            var editingController = fixture.Window.EditingController;
            var stateParagraph = new Paragraph();
            stateParagraph.Inlines.Add(new Run("bold") { FontWeight = FontWeights.Bold });
            stateParagraph.Inlines.Add(new Run(" plain"));
            fixture.Editor.Document.Blocks.Clear();
            fixture.Editor.Document.Blocks.Add(stateParagraph);
            fixture.Editor.SelectAll();
            editingController.RefreshState();
            Assert.Equal(WriterSelectionValueKind.Mixed, editingController.State.Bold.Kind);
            Assert.Null(Assert.IsType<RibbonToggleButton>(fixture.Window.FindName("BoldButton")).IsChecked);
            stateParagraph.Inlines.OfType<Run>().Last().FontWeight = FontWeights.Bold;
            editingController.RefreshState();
            Assert.True(editingController.State.Bold.IsUniform);
            Assert.True(Assert.IsType<RibbonToggleButton>(fixture.Window.FindName("BoldButton")).IsChecked);
            fixture.Editor.CaretPosition = stateParagraph.Inlines.OfType<Run>().First().ContentEnd;
            fixture.Editor.Selection.Select(fixture.Editor.CaretPosition, fixture.Editor.CaretPosition);
            editingController.RefreshState();
            Assert.True(editingController.State.Bold.IsUniform);
            fixture.Editor.IsReadOnly = true;
            editingController.RefreshState();
            Assert.False(editingController.State.CanFormat);
            Assert.False(Assert.IsType<RibbonToggleButton>(fixture.Window.FindName("BoldButton")).IsEnabled);
            fixture.Editor.IsReadOnly = false;
            fixture.Editor.IsEnabled = false;
            editingController.RefreshState();
            Assert.False(editingController.State.IsEnabled);
            Assert.False(Assert.IsType<RibbonButton>(fixture.Window.FindName("CopyButton")).IsEnabled);
            fixture.Editor.IsEnabled = true;
            fixture.Editor.Document.Blocks.Clear();
            fixture.Shell.CurrentDocument.MarkClean();

            fixture.Editor.AppendText("one two one");
            var find = editingController.FindReplace.FindNext("one", matchCase: true, wrap: true);
            Assert.True(find.Found);
            Assert.Equal(2, editingController.FindReplace.ReplaceAll("one", "three", matchCase: true));
            Assert.True(editingController.Zoom.TrySet(135));
            await PumpAsync();
            Assert.Equal("135%", Assert.IsType<TextBlock>(fixture.Window.FindName("ZoomText")).Text);
            await Task.Delay(350);
            await PumpAsync();
            Assert.Contains("3 words", Assert.IsType<TextBlock>(fixture.Window.FindName("StatisticsText")).Text);
            Assert.Contains("15 characters", Assert.IsType<TextBlock>(fixture.Window.FindName("StatisticsText")).Text);
            editingController.Zoom.Reset();
            fixture.Editor.Document.Blocks.Clear();
            fixture.Shell.CurrentDocument.MarkClean();

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
                .Where(button => button.CommandParameter is RecentFileEntry)
                .ToArray();
            Assert.Equal(2, recentButtons.Length);
            Assert.Equal(2, recentButtons.Select(button =>
                AutomationProperties.GetAutomationId(button)).Distinct(StringComparer.OrdinalIgnoreCase).Count());
            foreach (var button in recentButtons)
            {
                var rowEntry = Assert.IsType<RecentFileEntry>(button.CommandParameter);
                Assert.Same(rowEntry, button.Content);
                Assert.Equal(rowEntry.Path, AutomationProperties.GetAutomationId(button));
                Assert.Equal(rowEntry.FileName, AutomationProperties.GetName(button));
                Assert.Equal(rowEntry.Path, AutomationProperties.GetHelpText(button));
                Assert.NotNull(button.ContentTemplate);
                var rowText = FindVisualDescendants<TextBlock>(button).Select(text => text.Text).ToArray();
                Assert.Contains(rowEntry.FileName, rowText);
                Assert.Contains(rowEntry.FolderPath, rowText);
                Assert.Contains(rowEntry.FormatLabel, rowText);
                Assert.Contains(rowEntry.LastUsedLabel, rowText);
                Assert.Same(fixture.Shell.OpenRecentCommand, button.Command);
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
            await Task.Delay(350);
            await PumpAsync();
            Assert.Equal("0 words, 0 characters",
                Assert.IsType<TextBlock>(fixture.Window.FindName("StatisticsText")).Text);

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

            fixture.Shell.RecentEntries.Clear();
            fixture.Ribbon.IsBackstageOpen = true;
            await PumpAsync();
            var emptyState = FindVisualDescendants<FrameworkElement>(backstage).Single(element =>
                AutomationProperties.GetAutomationId(element) == "RecentEmptyState");
            Assert.Equal(Visibility.Visible, emptyState.Visibility);
            Assert.DoesNotContain(FindVisualDescendants<Button>(recentList),
                button => button.CommandParameter is RecentFileEntry);
            fixture.Ribbon.IsBackstageOpen = false;

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
        }, TimeSpan.FromSeconds(20));
    }

    private static async Task AssertEditingRibbonControlsAsync(WindowFixture fixture)
    {
        fixture.Window.Width = 1800;
        fixture.Window.UpdateLayout();
        var paragraph = new Paragraph(new Run("plain text"));
        fixture.Editor.Document.Blocks.Clear();
        fixture.Editor.Document.Blocks.Add(paragraph);
        fixture.Editor.SelectAll();
        fixture.Window.EditingController.RefreshState();

        var bold = Assert.IsType<RibbonToggleButton>(fixture.Window.FindName("BoldButton"));
        Assert.True(bold.Focus());
        Toggle(bold);
        await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
        AssertEditorFocusRestored(fixture);
        Assert.True(fixture.Window.EditingController.State.Bold.IsUniform);
        Assert.True(fixture.Window.EditingController.State.Bold.Value);

        var fontFamily = Assert.IsType<RibbonComboBox>(fixture.Window.FindName("FontFamilyCombo"));
        fontFamily.IsDropDownOpen = true;
        fontFamily.SelectedIndex = 2;
        fontFamily.IsDropDownOpen = false;
        await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
        fixture.Window.EditingController.RefreshState();
        Assert.Equal("Arial", fixture.Window.EditingController.State.FontFamily.Value.Source);

        var fontSize = Assert.IsType<RibbonComboBox>(fixture.Window.FindName("FontSizeCombo"));
        fontSize.IsDropDownOpen = true;
        fontSize.SelectedIndex = 3;
        fontSize.IsDropDownOpen = false;
        await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
        fixture.Window.EditingController.RefreshState();
        Assert.True(fixture.Window.EditingController.State.FontSize.IsUniform);
        Assert.Equal(16d, fixture.Window.EditingController.State.FontSize.Value, 3);
        Assert.Equal("12", fontSize.Text);
        var commitFontSize = typeof(WriterEditingRibbonController).GetMethod("CommitFontSize",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(commitFontSize);
        commitFontSize.Invoke(fixture.Window.EditingController, new object[] { "16" });
        fixture.Window.EditingController.RefreshState();
        Assert.Equal(WriterEditingRibbonController.PointsToDips(16),
            fixture.Window.EditingController.State.FontSize.Value, 3);

        Invoke(Assert.IsType<RibbonMenuItem>(fixture.Window.FindName("TextColorBlue")));
        await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
        fixture.Window.EditingController.RefreshState();
        Assert.Equal(Colors.Blue, fixture.Window.EditingController.State.Foreground.Value);
        AssertEditorFocusRestored(fixture);

        Invoke(Assert.IsType<RibbonMenuItem>(fixture.Window.FindName("HighlightYellow")));
        await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
        fixture.Window.EditingController.RefreshState();
        Assert.Equal(Colors.Yellow, fixture.Window.EditingController.State.Highlight.Value);

        Invoke(Assert.IsType<RibbonMenuItem>(fixture.Window.FindName("ParagraphSpacingOpen")));
        await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
        fixture.Window.EditingController.RefreshState();
        Assert.Equal(WriterEditingRibbonController.PointsToDips(6),
            fixture.Window.EditingController.State.SpacingBefore.Value, 3);
        Assert.Equal(WriterEditingRibbonController.PointsToDips(12),
            fixture.Window.EditingController.State.SpacingAfter.Value, 3);

        fixture.Editor.CaretPosition = fixture.Editor.Document.ContentEnd.GetInsertionPosition(
            LogicalDirection.Backward)!;
        fixture.Editor.Selection.Select(fixture.Editor.CaretPosition, fixture.Editor.CaretPosition);
        fixture.Editor.AppendText("!");
        Assert.EndsWith("!\r\n", TextOf(fixture.Editor.Document));
        fixture.Ribbon.IsMinimized = true;
        var qatUndo = Assert.IsType<RibbonButton>(fixture.Window.FindName("QatUndo"));
        Assert.True(qatUndo.Focus());
        Invoke(qatUndo);
        await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
        Assert.DoesNotContain("!", TextOf(fixture.Editor.Document));
        Assert.True(fixture.Ribbon.IsMinimized);
        AssertEditorFocusRestored(fixture);

        fixture.Editor.IsReadOnly = true;
        fixture.Window.EditingController.RefreshState();
        Assert.False(Assert.IsType<RibbonButton>(fixture.Window.FindName("ReplaceButton")).IsEnabled);
        Assert.True(Assert.IsType<RibbonButton>(fixture.Window.FindName("FindButton")).IsEnabled);
        Assert.False(Assert.IsType<RibbonDropDownButton>(
            fixture.Window.FindName("TextColorButton")).IsEnabled);
        Assert.False(Assert.IsType<RibbonDropDownButton>(
            fixture.Window.FindName("ParagraphSpacingButton")).IsEnabled);
        Assert.False(WriterRibbonCommands.Replace.CanExecute(null, fixture.Window));
        Assert.True(WriterRibbonCommands.Find.CanExecute(null, fixture.Window));

        var findDialog = new WriterFindReplaceDialog(fixture.Window.EditingController.FindReplace);
        findDialog.SetCanReplace(false);
        Assert.False(Assert.IsType<Button>(findDialog.FindName("ReplaceButton")).IsEnabled);
        Assert.False(Assert.IsType<Button>(findDialog.FindName("ReplaceAllButton")).IsEnabled);
        findDialog.Close();

        fixture.Editor.IsReadOnly = false;
        fixture.Ribbon.IsMinimized = false;
        fixture.Editor.Document.Blocks.Clear();
        fixture.Shell.CurrentDocument.MarkClean();
    }

    private static async Task AssertPageViewIntegrationAsync(WindowFixture fixture)
    {
        fixture.Window.Width = 1500;
        fixture.Window.UpdateLayout();
        var window = fixture.Window;
        var ribbon = fixture.Ribbon;
        var editor = fixture.Editor;
        var surface = Assert.IsType<WriterEditorSurface>(window.FindName("EditorSurface"));
        var preview = Assert.IsType<WriterDocumentPreviewView>(window.FindName("PreviewView"));

        var pageControls = new (string Name, string AutomationId, string CommandId)[]
        {
            ("PaperSizeButton", "PagePaperSize", "Writer.Page.Size"),
            ("OrientationButton", "PageOrientation", "Writer.Page.Orientation"),
            ("MarginsButton", "PageMargins", "Writer.Page.Margins.Choose"),
            ("PageColorButton", "PageColor", "Writer.Page.Color")
        };
        Assert.Equal(RibbonControlSize.Large,
            Assert.IsType<RibbonDropDownButton>(window.FindName("PaperSizeButton")).Size);
        Assert.Equal(RibbonControlSize.Large,
            Assert.IsType<RibbonDropDownButton>(window.FindName("OrientationButton")).Size);
        var viewControls = new (string Name, string AutomationId, string CommandId)[]
        {
            ("ContinuousViewButton", "ViewContinuous", "Writer.View.Continuous"),
            ("PaperViewButton", "ViewPaper", "Writer.View.Paper"),
            ("PrintPreviewViewButton", "ViewPrintPreview", "Writer.View.PrintPreview"),
            ("ZoomOutButton", "ZoomOut", "Writer.Home.Editing.ZoomOut"),
            ("ZoomResetButton", "ZoomReset", "Writer.Home.Editing.ZoomReset"),
            ("ZoomInButton", "ZoomIn", "Writer.Home.Editing.ZoomIn")
        };
        var previewControls = new (string Name, string AutomationId, string CommandId)[]
        {
            ("OnePageButton", "PreviewOnePage", "Writer.View.Preview.OnePage"),
            ("TwoPagesButton", "PreviewTwoPages", "Writer.View.Preview.TwoPages"),
            ("PageWidthButton", "PreviewPageWidth", "Writer.View.Preview.PageWidth"),
            ("PreviousPageButton", "PreviewPreviousPage", "Writer.View.Preview.Previous"),
            ("NextPageButton", "PreviewNextPage", "Writer.View.Preview.Next"),
            ("PreviewZoomOutButton", "PreviewZoomOut", "Writer.Home.Editing.ZoomOut"),
            ("PreviewZoomResetButton", "PreviewZoomReset", "Writer.Home.Editing.ZoomReset"),
            ("PreviewZoomInButton", "PreviewZoomIn", "Writer.Home.Editing.ZoomIn")
        };
        foreach (var (name, automationId, commandId) in pageControls.Concat(viewControls).Concat(previewControls))
        {
            var control = Assert.IsAssignableFrom<FrameworkElement>(window.FindName(name));
            Assert.Equal(automationId, AutomationProperties.GetAutomationId(control));
            Assert.False(string.IsNullOrWhiteSpace(AutomationProperties.GetName(control)));
            Assert.Equal(commandId, Ribbon.GetCommandId(control));
            Assert.False(string.IsNullOrWhiteSpace(KeyTip.GetKeys(control)));
            AssertScreenTip(control);
        }
        Assert.Equal(pageControls.Length, pageControls.Select(item =>
            KeyTip.GetKeys(Assert.IsAssignableFrom<FrameworkElement>(window.FindName(item.Name)))!)
            .Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Equal(viewControls.Length, viewControls.Select(item =>
            KeyTip.GetKeys(Assert.IsAssignableFrom<FrameworkElement>(window.FindName(item.Name)))!)
            .Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Equal(previewControls.Length, previewControls.Select(item =>
            KeyTip.GetKeys(Assert.IsAssignableFrom<FrameworkElement>(window.FindName(item.Name)))!)
            .Distinct(StringComparer.OrdinalIgnoreCase).Count());
        foreach (var name in new[]
        {
            "PaperSizeA4", "PaperSizeLetter", "PaperSizeLegal", "OrientationPortrait",
            "OrientationLandscape", "MarginsNormal", "MarginsNarrow", "MarginsModerate",
            "MarginsWide", "CustomMargins", "PageColorWhite", "PageColorIvory", "PageColorBlue"
        })
        {
            var item = Assert.IsType<RibbonMenuItem>(window.FindName(name));
            Assert.False(string.IsNullOrWhiteSpace(Ribbon.GetCommandId(item)));
            Assert.False(string.IsNullOrWhiteSpace(KeyTip.GetKeys(item)));
            Assert.False(string.IsNullOrWhiteSpace(AutomationProperties.GetAutomationId(item)));
            Assert.False(string.IsNullOrWhiteSpace(AutomationProperties.GetName(item)));
        }
        foreach (var (name, iconKey) in new[]
        {
            ("BackstagePrintButton", "Icon.WriterPrint"),
            ("BackstagePreviewButton", "Icon.WriterPrintPreview")
        })
        {
            var button = Assert.IsType<RibbonButton>(window.FindName(name));
            var content = Assert.IsType<StackPanel>(button.Content);
            var image = Assert.IsType<Image>(content.Children[0]);
            Assert.Same(window.TryFindResource(iconKey), image.Source);
            Assert.False(string.IsNullOrWhiteSpace(AutomationProperties.GetName(button)));
            Assert.False(string.IsNullOrWhiteSpace(Ribbon.GetCommandId(button)));
            Assert.False(string.IsNullOrWhiteSpace(KeyTip.GetKeys(button)));
            AssertScreenTip(button);
        }

        Assert.Equal(4, ribbon.Tabs.Count);
        var home = ribbon.Tabs.Single(tab => Equals(tab.Header, "Home"));
        Assert.DoesNotContain(FindLogicalDescendants<FrameworkElement>(home), element =>
            AutomationProperties.GetAutomationId(element) is "ZoomOut" or "ZoomReset" or "ZoomIn");
        var view = ribbon.Tabs.Single(tab => Equals(tab.Header, "View"));
        Assert.Equal(new[] { "Document Views", "Zoom" },
            view.Groups.Select(group => group.Header?.ToString()).ToArray());
        var printPreviewTab = Assert.IsType<RibbonTab>(window.FindName("PrintPreviewTab"));
        Assert.True(printPreviewTab.IsModal);
        Assert.Equal(Visibility.Collapsed, printPreviewTab.Visibility);
        Assert.Equal(new[] { "Preview Layout", "Navigation", "Zoom" },
            printPreviewTab.Groups.Select(group => group.Header?.ToString()).ToArray());
        Assert.Null(window.FindName("PrintButton"));
        foreach (var name in viewControls.Select(item => item.Name)
                     .Concat(previewControls.Select(item => item.Name)))
        {
            var size = window.FindName(name) switch
            {
                RibbonButton button => button.Size,
                RibbonToggleButton toggle => toggle.Size,
                var control => throw new Xunit.Sdk.XunitException(
                    $"{name} was not a sized ribbon control: {control?.GetType().Name ?? "null"}.")
            };
            Assert.Equal(RibbonControlSize.Large, size);
        }
        Assert.Same(window.TryFindResource("Icon.WriterPreviousPage"),
            Assert.IsType<RibbonButton>(window.FindName("PreviousPageButton")).Icon);
        Assert.Same(window.TryFindResource("Icon.WriterNextPage"),
            Assert.IsType<RibbonButton>(window.FindName("NextPageButton")).Icon);
        Assert.NotSame(window.TryFindResource("Icon.WriterUndo"),
            Assert.IsType<RibbonButton>(window.FindName("PreviousPageButton")).Icon);
        Assert.NotSame(window.TryFindResource("Icon.WriterRedo"),
            Assert.IsType<RibbonButton>(window.FindName("NextPageButton")).Icon);

        var document = fixture.Shell.CurrentDocument;
        var openingPreviewSnapshot = preview.Snapshot;
        document.Content.Blocks.Clear();
        var paragraph = new Paragraph(new Run("selection survives every Writer view"));
        document.Content.Blocks.Add(paragraph);
        editor.Selection.Select(paragraph.ContentStart.GetPositionAtOffset(1)!,
            paragraph.ContentStart.GetPositionAtOffset(10)!);
        var selectedText = editor.Selection.Text;
        editor.AppendText(" undo");
        Assert.True(editor.CanUndo);
        editor.Selection.Select(paragraph.ContentStart.GetPositionAtOffset(1)!,
            paragraph.ContentStart.GetPositionAtOffset(10)!);
        var liveEditor = editor;
        var liveDocument = editor.Document;

        Toggle(Assert.IsType<RibbonToggleButton>(window.FindName("ContinuousViewButton")));
        await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
        Assert.Equal(WriterViewMode.ContinuousEdit, window.CurrentViewMode);
        Assert.Equal(WriterEditorViewMode.Continuous, surface.ViewMode);
        Assert.Same(liveEditor, fixture.Editor);
        Assert.Same(liveDocument, fixture.Editor.Document);
        Assert.Equal(selectedText, editor.Selection.Text);
        Assert.True(editor.CanUndo);

        Toggle(Assert.IsType<RibbonToggleButton>(window.FindName("PaperViewButton")));
        await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
        Assert.Equal(WriterViewMode.Paper, window.CurrentViewMode);
        Assert.Equal(WriterEditorViewMode.Paper, surface.ViewMode);
        Assert.Same(liveDocument, fixture.Editor.Document);
        Assert.Equal(selectedText, editor.Selection.Text);
        Assert.True(editor.CanUndo);

        Assert.False(window.IsPreviewRebuildEnabled);
        Assert.Null(preview.Snapshot);
        Toggle(Assert.IsType<RibbonToggleButton>(window.FindName("PrintPreviewViewButton")));
        await WaitForAsync(() => preview.Snapshot is not null &&
            !ReferenceEquals(openingPreviewSnapshot, preview.Snapshot));
        await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
        Assert.True(ribbon.IsModal);
        Assert.Same(printPreviewTab, ribbon.ModalTab);
        Assert.True(window.IsPreviewRebuildEnabled);
        Assert.Equal(WriterViewMode.PrintPreview, window.CurrentViewMode);
        Assert.Equal(Visibility.Collapsed, surface.Visibility);
        Assert.Equal(Visibility.Visible, preview.Visibility);
        Assert.NotNull(preview.Snapshot);
        Assert.Same(liveDocument, fixture.Editor.Document);
        Assert.Equal(selectedText, editor.Selection.Text);
        Assert.True(editor.CanUndo);

        Toggle(Assert.IsType<RibbonToggleButton>(window.FindName("OnePageButton")));
        Assert.Equal(WriterPreviewViewMode.OnePage, preview.ViewMode);
        Toggle(Assert.IsType<RibbonToggleButton>(window.FindName("TwoPagesButton")));
        Assert.Equal(WriterPreviewViewMode.TwoPages, preview.ViewMode);
        Toggle(Assert.IsType<RibbonToggleButton>(window.FindName("PageWidthButton")));
        Assert.Equal(WriterPreviewViewMode.PageWidth, preview.ViewMode);
        var fittedZoom = preview.Zoom;
        Click(Assert.IsType<RibbonButton>(window.FindName("PreviewZoomInButton")));
        Assert.Equal(WriterPreviewViewMode.OnePage, preview.ViewMode);
        Assert.Equal(Math.Min(preview.MaxZoom, fittedZoom + 10), preview.Zoom, 3);
        Click(Assert.IsType<RibbonButton>(window.FindName("PreviewZoomResetButton")));
        Assert.Equal(100, preview.Zoom, 3);

        var firstSnapshot = preview.Snapshot;
        var printDevice = new RecordingPrintDevice();
        var printResult = window.TryPrintCurrentSnapshot(printDevice);
        Assert.NotNull(printResult);
        Assert.True(printResult.Submitted);
        Assert.Same(firstSnapshot!.Paginator, printDevice.SubmittedPaginator);
        editor.AppendText(" pending edit");
        Assert.Null(window.TryPrintCurrentSnapshot(new RecordingPrintDevice()));
        await WaitForAsync(() => preview.Snapshot is not null);
        Assert.NotNull(preview.Snapshot);
        Assert.NotSame(firstSnapshot, preview.Snapshot);

        Assert.True(ribbon.ExitModal());
        await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
        Assert.False(ribbon.IsModal);
        Assert.False(window.IsPreviewRebuildEnabled);
        Assert.Equal(WriterViewMode.Paper, window.CurrentViewMode);

        var pageSettingChanges = 0;
        document.PropertyChanged += CountPageSettings;
        ribbon.SelectedTab = ribbon.Tabs.Single(tab => Equals(tab.Header, "Page"));
        window.UpdateLayout();
        Click(Assert.IsType<RibbonMenuItem>(window.FindName("PaperSizeA4")));
        Assert.Equal(DocumentPaperSize.A4, document.PageSettings.PaperSize);
        Click(Assert.IsType<RibbonMenuItem>(window.FindName("OrientationLandscape")));
        Assert.Equal(DocumentPageOrientation.Landscape, document.PageSettings.Orientation);
        Click(Assert.IsType<RibbonMenuItem>(window.FindName("MarginsNarrow")));
        Assert.Equal(WriterPageUi.CreateMargins(WriterMarginPreset.Narrow), document.PageSettings.Margins);
        Assert.Equal(3, pageSettingChanges);
        document.PropertyChanged -= CountPageSettings;

        var custom = new DocumentPageMargins(40, 50, 60, 70);
        var customReplacement = document.PageSettings.WithMargins(custom);
        Assert.True(window.TryApplyPageSettings(customReplacement));
        Assert.Same(customReplacement, document.PageSettings);
        Assert.False(window.TryApplyPageSettings(customReplacement));

        Click(Assert.IsType<RibbonMenuItem>(window.FindName("PageColorIvory")));
        Assert.Equal(Color.FromRgb(255, 253, 240),
            Assert.IsType<SolidColorBrush>(document.Content.Background).Color);
        Assert.Equal(document.Content.Background, editor.Background);
        var summary = Assert.IsType<TextBlock>(window.FindName("BackstagePageSummaryText")).Text;
        Assert.Contains("A4", summary);
        Assert.Contains("Landscape", summary);
        Assert.Contains("Page colour: Ivory", summary);
        Toggle(Assert.IsType<RibbonToggleButton>(window.FindName("PrintPreviewViewButton")));
        await WaitForAsync(() => preview.Snapshot?.SourceClone.Background is SolidColorBrush brush &&
            brush.Color == Color.FromRgb(255, 253, 240));
        var colouredSnapshot = preview.Snapshot!;
        Assert.Equal(document.PageSettings, colouredSnapshot.PageSettings);
        var colouredPrintDevice = new RecordingPrintDevice();
        Assert.True(window.TryPrintCurrentSnapshot(colouredPrintDevice)!.Submitted);
        Assert.Same(colouredSnapshot.Paginator, colouredPrintDevice.SubmittedPaginator);

        window.FlowDirection = FlowDirection.RightToLeft;
        window.Width = 540;
        window.UpdateLayout();
        Assert.Equal(FlowDirection.RightToLeft, window.FlowDirection);
        Assert.All(ribbon.Tabs, tab => Assert.NotNull(tab.Header));
        window.FlowDirection = FlowDirection.LeftToRight;
        window.Width = 1500;

        Assert.True(ribbon.ExitModal());
        await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
        Assert.Equal(WriterViewMode.Paper, window.CurrentViewMode);
        Assert.Equal(Visibility.Visible, surface.Visibility);
        Assert.Same(liveDocument, editor.Document);
        Assert.Equal(selectedText, editor.Selection.Text);
        Assert.True(editor.CanUndo);
        document.Content.Blocks.Clear();
        document.Content.Background = null;
        editor.Background = Brushes.White;
        document.MarkClean();
        return;

        void CountPageSettings(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(WriterDocument.PageSettings))
                pageSettingChanges++;
        }
    }

    private static void AssertEditorFocusRestored(WindowFixture fixture)
    {
        // The visual-test host can own the OS foreground window when solution projects run in parallel.
        // WPF retains logical focus in that case, which is the deterministic evidence that activation will
        // return keyboard input to the editor rather than to the ribbon command that initiated the action.
        var logicalFocus = FocusManager.GetFocusedElement(fixture.Window);
        Assert.True(fixture.Editor.IsKeyboardFocusWithin || ReferenceEquals(logicalFocus, fixture.Editor),
            "The editor should retain keyboard or logical focus after the ribbon action.");
    }

    private static async Task AssertBackstageFileCommandsRestoreEditorFocusAsync(WindowFixture fixture)
    {
        var backstage = Assert.IsType<Backstage>(fixture.Ribbon.Backstage);
        foreach (var automationId in new[] { "FileNew", "FileOpen", "FileSave", "FileSaveAs" })
        {
            var item = backstage.Items.OfType<BackstageTabItem>().Single(candidate =>
                AutomationProperties.GetAutomationId(candidate) == automationId);
            fixture.Ribbon.IsBackstageOpen = true;
            await PumpAsync();
            FocusManager.SetFocusedElement(fixture.Window, fixture.Ribbon);
            Assert.NotSame(fixture.Editor, FocusManager.GetFocusedElement(fixture.Window));

            item.RaiseEvent(new RoutedEventArgs(BackstageTabItem.ClickEvent));
            item.Command!.Execute(null);
            await WaitForShellIdleAsync(fixture.Shell);
            await PumpAsync();

            Assert.False(fixture.Ribbon.IsBackstageOpen);
            AssertEditorFocusRestored(fixture);
        }
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
        var presentationHost = Assert.IsType<Grid>(dock.Children[2]);
        var editorSurface = Assert.IsType<WriterEditorSurface>(presentationHost.Children[0]);
        Assert.Same(editor, editorSurface.Editor);
        Assert.Equal(WriterEditorViewMode.Paper, editorSurface.ViewMode);
        Assert.Equal("DocumentEditor", AutomationProperties.GetAutomationId(editor));
        Assert.Equal("Document editor", AutomationProperties.GetName(editor));
        AssertEditorFocusRestored(fixture);
        var initialState = fixture.Window.EditingController.State;
        Assert.True(initialState.FontFamily.IsUniform);
        Assert.True(initialState.FontSize.IsUniform);
        Assert.True(initialState.Alignment.IsUniform);
        Assert.Equal(TextAlignment.Left, initialState.Alignment.Value);
        Assert.Equal(editor.Document.FontFamily.Source,
            Assert.IsType<RibbonComboBox>(window.FindName("FontFamilyCombo")).Text);
        Assert.Equal(WriterEditingRibbonController.DipsToPoints(editor.Document.FontSize)
                .ToString("0.##", CultureInfo.CurrentCulture),
            Assert.IsType<RibbonComboBox>(window.FindName("FontSizeCombo")).Text);
        Assert.True(Assert.IsType<RibbonToggleButton>(window.FindName("AlignLeftButton")).IsChecked);
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

        Assert.Equal(new[] { "Home", "Page", "View", "Print Preview" },
            ribbon.Tabs.Select(tab => tab.Header?.ToString()).ToArray());
        Assert.True(ribbon.Tabs[3].IsModal);
        Assert.Equal(Visibility.Collapsed, ribbon.Tabs[3].Visibility);
        var home = ribbon.Tabs[0];
        Assert.Equal("Home", home.Header);
        Assert.Equal(new[] { "Clipboard", "Font", "Paragraph", "Editing" },
            home.Groups.Select(group => group.Header?.ToString()).ToArray());
        var expectedHome = new (string Name, string AutomationId, string AutomationName, string CommandId)[]
        {
            ("PasteButton", "HomePaste", "Paste", "Writer.Home.Clipboard.Paste"),
            ("CutButton", "HomeCut", "Cut", "Writer.Home.Clipboard.Cut"),
            ("CopyButton", "HomeCopy", "Copy", "Writer.Home.Clipboard.Copy"),
            ("BoldButton", "HomeBold", "Bold", "Writer.Home.Font.Bold"),
            ("ItalicButton", "HomeItalic", "Italic", "Writer.Home.Font.Italic"),
            ("UnderlineButton", "HomeUnderline", "Underline", "Writer.Home.Font.Underline"),
            ("TextColorButton", "TextColor", "Text Color", "Writer.Home.Font.TextColor"),
            ("HighlightColorButton", "HighlightColor", "Text Highlight", "Writer.Home.Font.Highlight"),
            ("AlignLeftButton", "AlignLeft", "Align Left", "Writer.Home.Paragraph.AlignLeft"),
            ("AlignCenterButton", "AlignCenter", "Center", "Writer.Home.Paragraph.AlignCenter"),
            ("AlignRightButton", "AlignRight", "Align Right", "Writer.Home.Paragraph.AlignRight"),
            ("AlignJustifyButton", "AlignJustify", "Justify", "Writer.Home.Paragraph.AlignJustify"),
            ("BulletsButton", "ParagraphBullets", "Bullets", "Writer.Home.Paragraph.Bullets"),
            ("NumberingButton", "ParagraphNumbering", "Numbering", "Writer.Home.Paragraph.Numbering"),
            ("IncreaseIndentButton", "IncreaseIndent", "Increase Indent", "Writer.Home.Paragraph.IncreaseIndent"),
            ("DecreaseIndentButton", "DecreaseIndent", "Decrease Indent", "Writer.Home.Paragraph.DecreaseIndent"),
            ("ParagraphSpacingButton", "ParagraphSpacing", "Paragraph Spacing", "Writer.Home.Paragraph.Spacing"),
            ("FindButton", "EditingFind", "Find", "Writer.Home.Editing.Find"),
            ("ReplaceButton", "EditingReplace", "Replace", "Writer.Home.Editing.Replace"),
            ("SelectAllButton", "EditingSelectAll", "Select All", "Writer.Home.Editing.SelectAll"),
            ("SpellCheckButton", "EditingSpelling", "Spelling", "Writer.Home.Editing.Spelling")
        };
        foreach (var (name, automationId, automationName, commandId) in expectedHome)
        {
            var element = Assert.IsAssignableFrom<FrameworkElement>(window.FindName(name));
            Assert.Equal(automationId, AutomationProperties.GetAutomationId(element));
            Assert.Equal(automationName, AutomationProperties.GetName(element));
            Assert.Equal(commandId, Ribbon.GetCommandId(element));
            Assert.False(string.IsNullOrWhiteSpace(KeyTip.GetKeys(element)));
            AssertScreenTip(element);
        }
        var homeIds = expectedHome.Select(item => item.AutomationId).ToArray();
        Assert.Equal(homeIds.Length, homeIds.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        var homeKeys = expectedHome.Select(item => KeyTip.GetKeys(Assert.IsAssignableFrom<FrameworkElement>(window.FindName(item.Name)))!).ToArray();
        Assert.Equal(homeKeys.Length, homeKeys.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.DoesNotContain(home.Groups.SelectMany(group => FindVisualDescendants<FrameworkElement>(group)),
            element => AutomationProperties.GetAutomationId(element) is "ZoomOut" or "ZoomReset" or "ZoomIn");
        var menuCommands = new (string Name, string CommandId)[]
        {
            ("TextColorAutomatic", "Writer.Home.Font.TextColor.Automatic"),
            ("TextColorBlack", "Writer.Home.Font.TextColor.Black"),
            ("TextColorBlue", "Writer.Home.Font.TextColor.Blue"),
            ("TextColorRed", "Writer.Home.Font.TextColor.Red"),
            ("HighlightNone", "Writer.Home.Font.Highlight.None"),
            ("HighlightYellow", "Writer.Home.Font.Highlight.Yellow"),
            ("HighlightGreen", "Writer.Home.Font.Highlight.Green"),
            ("ParagraphSpacingCompact", "Writer.Home.Paragraph.Spacing.Compact"),
            ("ParagraphSpacingNormal", "Writer.Home.Paragraph.Spacing.Normal"),
            ("ParagraphSpacingOpen", "Writer.Home.Paragraph.Spacing.Open")
        };
        foreach (var (name, commandId) in menuCommands)
        {
            var item = Assert.IsType<RibbonMenuItem>(window.FindName(name));
            Assert.Equal(commandId, Ribbon.GetCommandId(item));
            Assert.False(string.IsNullOrWhiteSpace(AutomationProperties.GetName(item)));
            Assert.False(string.IsNullOrWhiteSpace(AutomationProperties.GetAutomationId(item)));
            Assert.False(string.IsNullOrWhiteSpace(KeyTip.GetKeys(item)));
            if (!name.StartsWith("ParagraphSpacing", StringComparison.Ordinal))
                Assert.NotNull(item.Command);
        }
        var fontFamily = Assert.IsType<RibbonComboBox>(window.FindName("FontFamilyCombo"));
        var fontSize = Assert.IsType<RibbonComboBox>(window.FindName("FontSizeCombo"));
        Assert.False(string.IsNullOrWhiteSpace(fontFamily.ScreenTipTitle));
        Assert.False(string.IsNullOrWhiteSpace(fontFamily.ScreenTipText));
        Assert.False(string.IsNullOrWhiteSpace(fontSize.ScreenTipTitle));
        Assert.False(string.IsNullOrWhiteSpace(fontSize.ScreenTipText));
        var qatItems = ribbon.QuickAccessItems.OfType<RibbonButton>().ToArray();
        var qatSave = Assert.IsType<RibbonButton>(qatItems[0]);
        Assert.Equal("QatSave", AutomationProperties.GetAutomationId(qatSave));
        Assert.Equal("Save", AutomationProperties.GetName(qatSave));
        Assert.Same(fixture.Shell.SaveCommand, qatSave.Command);
        var saveResource = fixture.Window.TryFindResource("Icon.WriterSave");
        Assert.IsType<DrawingImage>(saveResource);
        Assert.Same(saveResource, qatSave.Icon);
        foreach (var iconKey in new[]
        {
            "Icon.WriterDocument", "Icon.WriterSave", "Icon.WriterUndo", "Icon.WriterRedo",
            "Icon.WriterPaste", "Icon.WriterCut", "Icon.WriterCopy", "Icon.WriterFont",
            "Icon.WriterTextColor", "Icon.WriterHighlight", "Icon.WriterBold", "Icon.WriterItalic",
            "Icon.WriterUnderline", "Icon.WriterAlignLeft", "Icon.WriterAlignCenter",
            "Icon.WriterAlignRight", "Icon.WriterJustify", "Icon.WriterBullets", "Icon.WriterNumbering",
            "Icon.WriterIndentIncrease", "Icon.WriterIndentDecrease", "Icon.WriterParagraphSpacing",
            "Icon.WriterFind", "Icon.WriterReplace", "Icon.WriterSelectAll", "Icon.WriterSpellCheck",
            "Icon.WriterZoomOut", "Icon.WriterZoomReset", "Icon.WriterZoomIn"
        })
            Assert.IsType<DrawingImage>(fixture.Window.TryFindResource(iconKey));
        foreach (var iconKey in new[]
        {
            "Icon.WriterDocument.Large", "Icon.WriterSave.Large", "Icon.WriterPaste.Large",
            "Icon.WriterUndo.Large", "Icon.WriterRedo.Large"
        })
            Assert.IsType<DrawingImage>(fixture.Window.TryFindResource(iconKey));
        var paste = Assert.IsType<RibbonButton>(window.FindName("PasteButton"));
        Assert.Equal(RibbonControlSize.Large, paste.Size);
        Assert.Same(fixture.Window.TryFindResource("Icon.WriterPaste.Large"), paste.LargeIcon);
        var paragraphSpacing = Assert.IsType<RibbonDropDownButton>(window.FindName("ParagraphSpacingButton"));
        Assert.Equal(RibbonControlSize.Large, paragraphSpacing.Size);
        Assert.NotNull(paragraphSpacing.LargeIcon);
        Assert.Equal(new[] { "QatSave", "QatUndo", "QatRedo" },
            qatItems.Select(AutomationProperties.GetAutomationId).ToArray());
        Assert.Same(ApplicationCommands.Undo, Assert.IsType<RibbonButton>(qatItems[1]).Command);
        Assert.Same(ApplicationCommands.Redo, Assert.IsType<RibbonButton>(qatItems[2]).Command);
        Assert.Equal(new[] { "Writer.Save", "Writer.Undo", "Writer.Redo" },
            qatItems.Select(Ribbon.GetCommandId).ToArray());

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

    private static void AssertWriterIconCatalog(WindowFixture fixture)
    {
        var iconDictionary = Assert.Single(fixture.Window.Resources.MergedDictionaries,
            dictionary => dictionary.Source?.OriginalString.EndsWith("Icons.xaml", StringComparison.OrdinalIgnoreCase) == true);
        var iconKeys = iconDictionary.Keys.OfType<string>()
            .Where(key => key.StartsWith("Icon.Writer", StringComparison.Ordinal))
            .ToArray();

        Assert.True(iconKeys.Length >= 100, $"Expected at least 100 Writer icons, found {iconKeys.Length}.");
        foreach (var key in new[]
        {
            "Icon.WriterNew", "Icon.WriterOpen", "Icon.WriterSaveAs", "Icon.WriterExportPdf",
            "Icon.WriterPrint", "Icon.WriterPrintPreview", "Icon.WriterPageSize", "Icon.WriterPortrait",
            "Icon.WriterLandscape", "Icon.WriterMargins", "Icon.WriterPageColor", "Icon.WriterColumns",
            "Icon.WriterPageBreak", "Icon.WriterEditLayout", "Icon.WriterTwoPages", "Icon.WriterPageWidth",
            "Icon.WriterImage", "Icon.WriterHyperlink", "Icon.WriterDateTime", "Icon.WriterTable",
            "Icon.WriterAddRowAbove", "Icon.WriterAddColumnRight", "Icon.WriterDeleteRow",
            "Icon.WriterMergeCells", "Icon.WriterSplitCells", "Icon.WriterDistributeColumns",
            "Icon.WriterCellAlignMiddle", "Icon.WriterCellShading", "Icon.WriterBorders",
            "Icon.WriterTheme", "Icon.WriterDarkMode", "Icon.WriterBackdrop", "Icon.WriterCustomizeRibbon",
            "Icon.WriterOptions", "Icon.WriterWarning", "Icon.WriterInformation", "Icon.WriterError",
            "Icon.WriterLock", "Icon.WriterImport", "Icon.WriterExport", "Icon.WriterReset"
        })
            Assert.IsType<DrawingImage>(fixture.Window.TryFindResource(key));

        foreach (var key in new[]
        {
            "Icon.WriterDocument", "Icon.WriterSave", "Icon.WriterPaste", "Icon.WriterCopy",
            "Icon.WriterTextColor", "Icon.WriterHighlight", "Icon.WriterFind", "Icon.WriterSpellCheck",
            "Icon.WriterZoomIn", "Icon.WriterImage", "Icon.WriterTable", "Icon.WriterTheme"
        })
        {
            var image = Assert.IsType<DrawingImage>(fixture.Window.TryFindResource(key));
            var drawing = Assert.IsType<DrawingGroup>(image.Drawing);
            Assert.True(drawing.Children.Count >= 2, $"{key} must retain layered color artwork.");
        }

        var chromaticBrushKeys = iconDictionary.Keys.OfType<string>()
            .Where(key => key.StartsWith("Writer.Icon.Brush.", StringComparison.Ordinal))
            .Except(new[]
            {
                "Writer.Icon.Brush.Ink", "Writer.Icon.Brush.Slate", "Writer.Icon.Brush.Paper",
                "Writer.Icon.Brush.PaperShadow"
            })
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(new[]
        {
            "Writer.Icon.Brush.Amber", "Writer.Icon.Brush.Blue"
        }, chromaticBrushKeys);
        foreach (var brushKey in chromaticBrushKeys)
            Assert.IsType<SolidColorBrush>(fixture.Window.TryFindResource(brushKey));

        Assert.Equal("#FF3F4650",
            Assert.IsType<SolidColorBrush>(fixture.Window.TryFindResource("Writer.Icon.Brush.Ink")).Color.ToString());
        Assert.Equal("#FF607A96",
            Assert.IsType<SolidColorBrush>(fixture.Window.TryFindResource("Writer.Icon.Brush.Blue")).Color.ToString());
        Assert.Equal("#FFB28A4A",
            Assert.IsType<SolidColorBrush>(fixture.Window.TryFindResource("Writer.Icon.Brush.Amber")).Color.ToString());

        var pens = iconDictionary.Values.OfType<Pen>().ToArray();
        Assert.Equal(5, pens.Length);
        Assert.All(pens, pen =>
        {
            Assert.Equal(1.4, pen.Thickness);
            Assert.Equal(PenLineCap.Round, pen.StartLineCap);
            Assert.Equal(PenLineCap.Round, pen.EndLineCap);
            Assert.Equal(PenLineJoin.Round, pen.LineJoin);
        });

        AssertSharedIconColors(fixture, "Icon.WriterUndo", "Icon.WriterRedo");
        AssertSharedIconColors(fixture, "Icon.WriterBold", "Icon.WriterItalic", "Icon.WriterUnderline");
        AssertSharedIconColors(fixture, "Icon.WriterAlignLeft", "Icon.WriterAlignCenter",
            "Icon.WriterAlignRight", "Icon.WriterJustify");
        AssertSharedIconColors(fixture, "Icon.WriterBullets", "Icon.WriterNumbering");
        AssertSharedIconColors(fixture, "Icon.WriterFind", "Icon.WriterReplace");
        AssertSharedIconColors(fixture, "Icon.WriterZoomOut", "Icon.WriterZoomReset", "Icon.WriterZoomIn");

        var standardColors = new[]
        {
            Assert.IsType<SolidColorBrush>(fixture.Window.TryFindResource("Writer.Icon.Brush.Ink")).Color.ToString(),
            Assert.IsType<SolidColorBrush>(fixture.Window.TryFindResource("Writer.Icon.Brush.Blue")).Color.ToString(),
        }.OrderBy(color => color, StringComparer.Ordinal).ToArray();
        foreach (var key in new[]
        {
            "Icon.WriterDocument", "Icon.WriterSave", "Icon.WriterUndo", "Icon.WriterRedo",
            "Icon.WriterPaste", "Icon.WriterCut", "Icon.WriterCopy", "Icon.WriterFont",
            "Icon.WriterBold", "Icon.WriterItalic", "Icon.WriterUnderline",
            "Icon.WriterAlignLeft", "Icon.WriterAlignCenter", "Icon.WriterAlignRight", "Icon.WriterJustify",
            "Icon.WriterBullets", "Icon.WriterNumbering", "Icon.WriterIndentIncrease",
            "Icon.WriterIndentDecrease", "Icon.WriterParagraphSpacing", "Icon.WriterFind",
            "Icon.WriterReplace", "Icon.WriterSelectAll", "Icon.WriterSpellCheck",
            "Icon.WriterZoomOut", "Icon.WriterZoomReset", "Icon.WriterZoomIn"
        })
            Assert.Equal(standardColors, IconSemanticColors(fixture, key));
    }

    private static void AssertSharedIconColors(WindowFixture fixture, params string[] keys)
    {
        var expected = IconColors(fixture, keys[0]);
        foreach (var key in keys.Skip(1))
            Assert.Equal(expected, IconColors(fixture, key));
    }

    private static string[] IconColors(WindowFixture fixture, string key)
    {
        var image = Assert.IsType<DrawingImage>(fixture.Window.TryFindResource(key));
        var drawing = Assert.IsType<DrawingGroup>(image.Drawing);
        return drawing.Children.OfType<GeometryDrawing>()
            .SelectMany(child => new[] { child.Brush, child.Pen?.Brush })
            .OfType<SolidColorBrush>()
            .Select(brush => brush.Color.ToString())
            .Distinct(StringComparer.Ordinal)
            .OrderBy(color => color, StringComparer.Ordinal)
            .ToArray();
    }

    private static string[] IconSemanticColors(WindowFixture fixture, string key)
    {
        var palette = new HashSet<string>(new[]
        {
            "Writer.Icon.Brush.Ink", "Writer.Icon.Brush.Blue", "Writer.Icon.Brush.Amber"
        }.Select(resourceKey =>
            Assert.IsType<SolidColorBrush>(fixture.Window.TryFindResource(resourceKey)).Color.ToString()),
            StringComparer.Ordinal);
        return IconColors(fixture, key).Where(palette.Contains).ToArray();
    }

    private static void AssertScreenTip(FrameworkElement element)
    {
        var (title, text) = element switch
        {
            RibbonButton button => (button.ScreenTipTitle, button.ScreenTipText),
            RibbonToggleButton button => (button.ScreenTipTitle, button.ScreenTipText),
            RibbonDropDownButton button => (button.ScreenTipTitle, button.ScreenTipText),
            _ => (null, null)
        };
        Assert.False(string.IsNullOrWhiteSpace(title));
        Assert.False(string.IsNullOrWhiteSpace(text));
    }

    private static void Invoke(Button button)
    {
        var peer = UIElementAutomationPeer.CreatePeerForElement(button) ?? new ButtonAutomationPeer(button);
        Assert.IsAssignableFrom<IInvokeProvider>(peer.GetPattern(PatternInterface.Invoke)).Invoke();
    }

    private static void Toggle(ToggleButton button)
    {
        var invoke = button.GetType().GetMethod("InvokeFromKeyTip",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(invoke);
        invoke.Invoke(button, null);
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

    private static async Task WaitForAsync(Func<bool> condition)
    {
        for (var attempt = 0; attempt < 100; attempt++)
        {
            if (condition())
                return;
            await Task.Delay(10);
            await Dispatcher.Yield(DispatcherPriority.Background);
        }
        Assert.True(condition(), "The expected Writer window state did not arrive within one second.");
    }

    private static void Click(ButtonBase button) =>
        button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, button));

    private static IEnumerable<T> FindLogicalDescendants<T>(DependencyObject root) where T : DependencyObject
    {
        foreach (var child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>())
        {
            if (child is T match)
                yield return match;
            foreach (var descendant in FindLogicalDescendants<T>(child))
                yield return descendant;
        }
    }

    private sealed class RecordingPrintDevice : IWriterPrintDevice
    {
        public WriterPrintDeviceCapabilities? Capabilities => null;
        public DocumentPaginator? SubmittedPaginator { get; private set; }

        public void Submit(DocumentPaginator paginator, string documentName)
        {
            SubmittedPaginator = paginator;
        }
    }
}
