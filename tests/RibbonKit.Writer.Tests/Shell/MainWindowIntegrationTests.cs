using System.Collections.ObjectModel;
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
            await AssertEditingRibbonControlsAsync(fixture);

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

    private static void AssertEditorFocusRestored(WindowFixture fixture)
    {
        // The visual-test host can own the OS foreground window when solution projects run in parallel.
        // WPF retains logical focus in that case, which is the deterministic evidence that activation will
        // return keyboard input to the editor rather than to the ribbon command that initiated the action.
        var logicalFocus = FocusManager.GetFocusedElement(fixture.Window);
        Assert.True(fixture.Editor.IsKeyboardFocusWithin || ReferenceEquals(logicalFocus, fixture.Editor),
            "The editor should retain keyboard or logical focus after the ribbon action.");
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
            ("SpellCheckButton", "EditingSpelling", "Spelling", "Writer.Home.Editing.Spelling"),
            ("ZoomOutButton", "ZoomOut", "Zoom Out", "Writer.Home.Editing.ZoomOut"),
            ("ZoomResetButton", "ZoomReset", "Reset Zoom", "Writer.Home.Editing.ZoomReset"),
            ("ZoomInButton", "ZoomIn", "Zoom In", "Writer.Home.Editing.ZoomIn")
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
