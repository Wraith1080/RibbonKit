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
using RibbonKit.Writer.Services.Persistence;
using RibbonKit.Writer.Services.RecentFiles;
using RibbonKit.Writer.Shell;
using RibbonKit.Writer.Tests.Document;
using RibbonKit.Writer.View;
using Xunit;

namespace RibbonKit.Writer.Tests.Shell;

[CollectionDefinition("Writer UI", DisableParallelization = true)]
public sealed class WriterUiCollectionDefinition
{
}

[Collection("Writer UI")]
public sealed class MainWindowIntegrationTests
{
    private static async Task AssertBackstageNewGalleryAsync(WindowFixture fixture)
    {
        var backstage = Assert.IsType<Backstage>(fixture.Ribbon.Backstage);
        var newPage = backstage.Items.OfType<BackstageTabItem>().Single(item =>
            AutomationProperties.GetAutomationId(item) == "FileNew");
        Assert.False(newPage.IsButton);
        Assert.Equal("New", AutomationProperties.GetName(newPage));

        fixture.Ribbon.IsBackstageOpen = true;
        newPage.IsSelected = true;
        await PumpAsync();
        Assert.IsType<Grid>(fixture.Window.FindName("NewProfilePage"));
        var cardsScrollViewer = Assert.IsType<ScrollViewer>(
            fixture.Window.FindName("NewProfileCardsScrollViewer"));
        var cardsPanel = Assert.IsType<WrapPanel>(
            fixture.Window.FindName("NewProfileCardsPanel"));
        Assert.Equal(ScrollBarVisibility.Disabled,
            ScrollViewer.GetHorizontalScrollBarVisibility(cardsScrollViewer));
        Assert.Equal(ScrollBarVisibility.Auto,
            ScrollViewer.GetVerticalScrollBarVisibility(cardsScrollViewer));
        Assert.Equal(Orientation.Horizontal, cardsPanel.Orientation);

        var cards = FindVisualDescendants<Button>(backstage)
            .Where(button => AutomationProperties.GetAutomationId(button)
                .StartsWith("New", StringComparison.Ordinal))
            .ToArray();
        Assert.Equal(3, cards.Length);
        Assert.Equal(new[] { "Plain Text", "Rich Text", "RibbonKit Writer" },
            cards.Select(AutomationProperties.GetName).ToArray());
        Assert.Equal(new[] { "1", "2", "3" },
            cards.Select(KeyTip.GetKeys).ToArray());
        Assert.All(cards, card =>
        {
            Assert.True(card.Focusable);
            Assert.True(card.IsTabStop);
            Assert.InRange(card.ActualWidth, card.MinWidth, card.MaxWidth);
            Assert.False(string.IsNullOrWhiteSpace(AutomationProperties.GetHelpText(card)));
            Assert.Same(fixture.Shell.NewCommand, card.Command);
            Assert.IsType<DrawingImage>(card.Content is StackPanel panel
                ? ((Image)panel.Children[0]).Source
                : null);
        });

        var savedWidth = fixture.Window.Width;
        fixture.Window.Width = fixture.Window.MinWidth;
        await PumpAsync();
        Assert.True(cardsScrollViewer.ViewportWidth > 0);
        Assert.True(cardsPanel.ActualHeight > 0);
        Assert.All(cards, card => Assert.InRange(card.ActualWidth, card.MinWidth, card.MaxWidth));
        fixture.Window.Width = savedWidth;
        await PumpAsync();

        var savedFlowDirection = newPage.FlowDirection;
        newPage.FlowDirection = FlowDirection.RightToLeft;
        await PumpAsync();
        Assert.Equal(FlowDirection.RightToLeft, cardsPanel.FlowDirection);
        Assert.All(cards, card => Assert.Equal(FlowDirection.RightToLeft, card.FlowDirection));
        newPage.FlowDirection = savedFlowDirection;
        await PumpAsync();

        foreach (var (automationId, format) in new[]
        {
            ("NewPlainTextCard", WriterDocumentFormat.PlainText),
            ("NewRichTextCard", WriterDocumentFormat.RichText),
            ("NewRibbonKitWriterCard", WriterDocumentFormat.RibbonKitWriter)
        })
        {
            fixture.Ribbon.IsBackstageOpen = true;
            newPage.IsSelected = true;
            await PumpAsync();
            var card = FindVisualDescendants<Button>(backstage).Single(button =>
                AutomationProperties.GetAutomationId(button) == automationId);
            var peer = UIElementAutomationPeer.CreatePeerForElement(card)
                ?? new ButtonAutomationPeer(card);
            Assert.IsAssignableFrom<IInvokeProvider>(
                peer.GetPattern(PatternInterface.Invoke)).Invoke();
            await WaitForShellIdleAsync(fixture.Shell);
            await PumpAsync();
            Assert.False(fixture.Ribbon.IsBackstageOpen);
            Assert.Equal(format, fixture.Window.CurrentProfile.Format);
            Assert.True(fixture.Shell.CurrentDocument.IsUntitled);
            AssertEditorFocusRestored(fixture);

            if (format == WriterDocumentFormat.PlainText)
            {
                Assert.False(Assert.IsType<RibbonGroup>(fixture.Window.FindName("FontGroup")).IsEnabled);
                Assert.False(Assert.IsType<RibbonGroup>(fixture.Window.FindName("ParagraphGroup")).IsEnabled);
                Assert.False(Assert.IsType<RibbonGroup>(fixture.Window.FindName("PageSetupGroup")).IsEnabled);
                Assert.False(Assert.IsType<RibbonGroup>(fixture.Window.FindName("MarginsGroup")).IsEnabled);
                Assert.False(Assert.IsType<RibbonGroup>(fixture.Window.FindName("PageBackgroundGroup")).IsEnabled);
                Assert.False(Assert.IsType<RibbonTab>(fixture.Window.FindName("PageTab")).IsEnabled);
                Assert.True(Assert.IsType<RibbonTab>(fixture.Window.FindName("ViewTab")).IsEnabled);
                Assert.True(Assert.IsType<RibbonTab>(fixture.Window.FindName("PrintPreviewTab")).IsEnabled);
                Assert.False(Assert.IsAssignableFrom<UIElement>(
                    fixture.Window.FindName("BoldButton")).IsEnabled);
                Assert.False(Assert.IsType<RibbonDropDownButton>(
                    fixture.Window.FindName("PageColorButton")).IsEnabled);
                Assert.True(fixture.Window.SupportsProfileCommand(
                    WriterDocumentCommandCapabilities.Preview));
                Assert.True(fixture.Window.SupportsProfileCommand(
                    WriterDocumentCommandCapabilities.Printing));
                Assert.True(Assert.IsType<RibbonButton>(
                    fixture.Window.FindName("BackstagePreviewButton")).IsEnabled);
            }

            var nativeStructuredContent = format == WriterDocumentFormat.RibbonKitWriter;
            Assert.True(Assert.IsType<RibbonTab>(fixture.Window.FindName("InsertTab")).IsEnabled);
            Assert.True(Assert.IsType<RibbonButton>(fixture.Window.FindName("InsertDateTimeButton")).IsEnabled);
            Assert.Equal(nativeStructuredContent,
                Assert.IsType<RibbonButton>(fixture.Window.FindName("InsertPictureButton")).IsEnabled);
            Assert.Equal(nativeStructuredContent,
                Assert.IsType<RibbonButton>(fixture.Window.FindName("InsertHyperlinkButton")).IsEnabled);
            Assert.Equal(nativeStructuredContent,
                Assert.IsType<InRibbonGallery>(fixture.Window.FindName("TableGridPicker")).IsEnabled);
            Assert.Equal(nativeStructuredContent, fixture.Window.CurrentProfile.Supports(
                WriterDocumentCommandCapabilities.TableEditing));
            Assert.Equal(nativeStructuredContent,
                fixture.Window.CurrentProfile.Capabilities.PreservesTables);
        }

        Assert.True(await fixture.Shell.NewAsync());
        Assert.Equal(WriterDocumentFormat.RichText, fixture.Window.CurrentProfile.Format);

        var original = fixture.Shell.CurrentDocument;
        fixture.Editor.AppendText("unsaved");
        fixture.Dialogs.Decisions.Enqueue(UnsavedChangesDecision.Cancel);
        fixture.Ribbon.IsBackstageOpen = true;
        newPage.IsSelected = true;
        await PumpAsync();
        var cancelCard = FindVisualDescendants<Button>(backstage).Single(button =>
            AutomationProperties.GetAutomationId(button) == "NewPlainTextCard");
        var cancelPeer = UIElementAutomationPeer.CreatePeerForElement(cancelCard)
            ?? new ButtonAutomationPeer(cancelCard);
        Assert.IsAssignableFrom<IInvokeProvider>(
            cancelPeer.GetPattern(PatternInterface.Invoke)).Invoke();
        await WaitForShellIdleAsync(fixture.Shell);
        await PumpAsync();
        Assert.Same(original, fixture.Shell.CurrentDocument);
        Assert.True(original.IsDirty);
        Assert.Equal(WriterDocumentFormat.RichText, fixture.Window.CurrentProfile.Format);
        original.MarkClean();
    }

    [Fact]
    public async Task CenteredTableReflowsAcrossPaperAndContinuousWithoutDirtyingOrReplacingUndo()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            using var fixture = new WindowFixture();
            fixture.Show();
            Assert.True(await fixture.Shell.NewAsync(WriterDocumentProfiles.RibbonKitWriter));
            await PumpAsync();
            var window = fixture.Window;
            var editor = fixture.Editor;
            var document = fixture.Shell.CurrentDocument;
            document.Content.Blocks.Clear();
            var anchor = new Paragraph();
            document.Content.Blocks.Add(anchor);
            editor.Selection.Select(anchor.ContentStart, anchor.ContentStart);
            var tables = window.TableInteractionController.Tables;
            var table = Assert.IsType<Table>(tables.InsertTable(1, 2));
            table.Columns[0].Width = new GridLength(100);
            table.Columns[1].Width = new GridLength(130);
            window.TableInteractionController.MoveCaret(
                window.TableInteractionController.GetOrderedCells(table)[0]);
            window.TableInteractionController.Refresh();
            await PumpAsync();

            Assert.True(WriterTableLayoutResolver.TryCreate(editor,
                window.TableInteractionController.GetOrderedCells(table), 0, out var paperLayout));
            var padding = document.Content.PagePadding;
            var availableWidth = document.Content.PageWidth - padding.Left - padding.Right;
            var tableWidth = paperLayout.Bounds.Width / paperLayout.ProjectionScaleX;
            Assert.True(tables.SetTableHorizontalAlignment(table,
                WriterTableHorizontalAlignment.Center, tableWidth, availableWidth));
            WriterTableMarginProjection.Project(table, WriterTableHorizontalAlignment.Center,
                tableWidth, availableWidth);
            await PumpAsync();
            var paperMargin = table.Margin;
            Assert.Equal(paperMargin.Left, paperMargin.Right, 6);

            editor.IsUndoEnabled = false;
            editor.IsUndoEnabled = true;
            var cellParagraph = table.RowGroups[0].Rows[0].Cells[0].Blocks
                .OfType<Paragraph>().Single();
            editor.Selection.Select(cellParagraph.ContentStart, cellParagraph.ContentEnd);
            editor.Selection.Text = "history";
            Assert.True(editor.CanUndo);
            document.MarkClean();

            Toggle(Assert.IsType<RibbonToggleButton>(window.FindName("ContinuousViewButton")));
            await PumpAsync();

            var continuousMargin = table.Margin;
            Assert.Equal(WriterViewMode.ContinuousEdit, window.CurrentViewMode);
            Assert.Equal(continuousMargin.Left, continuousMargin.Right, 6);
            Assert.NotEqual(paperMargin.Left, continuousMargin.Left);
            Assert.True(editor.CanUndo);
            Assert.False(document.IsDirty);
            Assert.True(editor.Undo());
            await PumpAsync();
            Assert.Equal(continuousMargin.Left, table.Margin.Left, 6);
            Assert.Equal(continuousMargin.Right, table.Margin.Right, 6);
            Assert.NotEqual("history", new TextRange(cellParagraph.ContentStart,
                cellParagraph.ContentEnd).Text.Trim());

            Toggle(Assert.IsType<RibbonToggleButton>(window.FindName("PaperViewButton")));
            await PumpAsync();

            Assert.Equal(WriterViewMode.Paper, window.CurrentViewMode);
            Assert.Equal(paperMargin.Left, table.Margin.Left, 6);
            Assert.Equal(paperMargin.Right, table.Margin.Right, 6);
        });
    }

    [Fact]
    public async Task WriterIdentityAndBackstageNavigationIconsAreAppOwned()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            using var fixture = new WindowFixture();
            fixture.Show();
            await PumpAsync();

            Assert.NotNull(fixture.Window.Icon);
            Assert.True(fixture.Window.HasWriterOrbTemplate());
            var identityBrush = Assert.IsType<LinearGradientBrush>(
                fixture.Window.TryFindResource("Writer.Brushes.IdentityMark"));
            Assert.Equal(new[]
            {
                Color.FromRgb(0x3F, 0x94, 0xDF),
                Color.FromRgb(0x14, 0x5A, 0xA6)
            }, identityBrush.GradientStops.Select(stop => stop.Color).ToArray());

            var backstage = Assert.IsType<Backstage>(fixture.Ribbon.Backstage);
            var expectedIcons = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Home"] = "Icon.WriterHome",
                ["New"] = "Icon.WriterBackstageNew",
                ["Open"] = "Icon.WriterBackstageOpen",
                ["Save"] = "Icon.WriterBackstageSave",
                ["Save As"] = "Icon.WriterBackstageSaveAs",
                ["Print"] = "Icon.WriterBackstagePrint",
                ["Settings"] = "Icon.WriterOptions",
                ["Exit"] = "Icon.WriterExit"
            };

            var items = backstage.Items.OfType<BackstageTabItem>().ToArray();
            Assert.Equal(expectedIcons.Count, items.Length);
            foreach (var item in items)
            {
                string header = Assert.IsType<string>(item.Header);
                string resourceKey = expectedIcons[header];
                Assert.Same(fixture.Window.TryFindResource(resourceKey), item.Icon);
            }

            var gear = Assert.IsType<DrawingImage>(fixture.Window.TryFindResource("Icon.WriterOptions"));
            var gearDrawing = Assert.IsType<GeometryDrawing>(gear.Drawing);
            Assert.True(gearDrawing.Geometry.FillContains(new Point(12, 3)));
            Assert.False(gearDrawing.Geometry.FillContains(new Point(12, 12)));

            var open = Assert.IsType<DrawingImage>(fixture.Window.TryFindResource("Icon.WriterBackstageOpen"));
            var openDrawing = Assert.IsType<DrawingGroup>(open.Drawing);
            var openBack = Assert.IsType<GeometryDrawing>(openDrawing.Children[0]);
            Assert.True(openBack.Geometry.FillContains(new Point(3, 10)));

            var saveAs = Assert.IsType<DrawingImage>(fixture.Window.TryFindResource("Icon.WriterBackstageSaveAs"));
            var saveAsDrawing = Assert.IsType<DrawingGroup>(saveAs.Drawing);
            var pencil = Assert.IsType<GeometryDrawing>(saveAsDrawing.Children[1]);
            Assert.True(pencil.Geometry.FillContains(new Point(20.5, 9.5)));
            Assert.False(pencil.Geometry.FillContains(new Point(18, 13)));
        });
    }

    [Fact]
    public async Task MainContentScrollBarsUseSharedRibbonKitChrome()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            using var fixture = new WindowFixture();
            fixture.Show();
            await PumpAsync();

            var host = Assert.IsType<Grid>(fixture.Window.FindName("DocumentPresentationHost"));
            var sharedStyle = Assert.IsType<Style>(
                host.FindResource("RibbonKit.ScrollBarStyle"));
            var scopedStyle = Assert.IsType<Style>(host.FindResource(typeof(ScrollBar)));
            Assert.Same(sharedStyle, scopedStyle.BasedOn);

            var viewport = Assert.IsType<ScrollViewer>(fixture.Window.FindName("EditorViewport"));
            var scrollBars = FindVisualDescendants<ScrollBar>(viewport).ToArray();
            Assert.NotEmpty(scrollBars);
            Assert.All(scrollBars, scrollBar => Assert.Same(scopedStyle, scrollBar.Style));
            Assert.All(scrollBars.Where(scrollBar => scrollBar.Orientation == Orientation.Vertical),
                scrollBar => Assert.Equal(new Thickness(0, 0, 1, 0), scrollBar.Margin));
            Assert.All(scrollBars.Where(scrollBar => scrollBar.Orientation == Orientation.Horizontal),
                scrollBar => Assert.Equal(new Thickness(0), scrollBar.Margin));

            var preview = Assert.IsType<WriterDocumentPreviewView>(
                fixture.Window.FindName("PreviewView"));
            var inheritedPreviewStyle = Assert.IsType<Style>(
                preview.Viewer.FindResource(typeof(ScrollBar)));
            Assert.Same(scopedStyle, inheritedPreviewStyle);
        });
    }

    [Fact]
    public async Task BackstageRecentAndNewScrollBarsUseSharedRibbonKitChrome()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            using var fixture = new WindowFixture(withRecentFile: true);
            fixture.Show();
            await PumpAsync();

            var backstage = Assert.IsType<Backstage>(fixture.Ribbon.Backstage);
            fixture.Ribbon.IsBackstageOpen = true;
            backstage.Items.OfType<BackstageTabItem>().Single(item =>
                Equals(item.Header, "Home")).IsSelected = true;
            await PumpAsync();

            var recentScroll = Assert.IsType<ScrollViewer>(
                fixture.Window.FindName("RecentDocumentsScrollViewer"));
            AssertBackstageScrollBarChrome(recentScroll);

            backstage.Items.OfType<BackstageTabItem>().Single(item =>
                Equals(item.Header, "New")).IsSelected = true;
            await PumpAsync();

            var newScroll = Assert.IsType<ScrollViewer>(
                fixture.Window.FindName("NewProfileCardsScrollViewer"));
            AssertBackstageScrollBarChrome(newScroll);
        });
    }

    [Fact]
    public async Task BackstagePageContentStaysLeftAnchoredAsWindowWidens()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            using var fixture = new WindowFixture(withRecentFile: true);
            fixture.Show();
            await PumpAsync();

            var backstage = Assert.IsType<Backstage>(fixture.Ribbon.Backstage);
            fixture.Ribbon.IsBackstageOpen = true;
            await PumpAsync();

            var home = backstage.Items.OfType<BackstageTabItem>().Single(item =>
                Equals(item.Header, "Home"));
            home.IsSelected = true;
            await PumpAsync();
            var recentPage = Assert.IsType<Grid>(fixture.Window.FindName("RecentPage"));
            var recentDescription = Assert.IsType<TextBlock>(
                fixture.Window.FindName("RecentPageDescription"));
            var recentList = Assert.IsType<ItemsControl>(fixture.Window.FindName("RecentList"));
            var recentButton = FindVisualDescendants<Button>(recentList).First(button =>
                button.CommandParameter is RecentFileEntry);
            var recentContent = FindVisualDescendants<Grid>(recentButton).Single(grid =>
                Math.Abs(grid.MaxWidth - 760) < 0.01);
            double recentDescriptionLeft = LeftWithin(recentDescription, recentPage);
            double recentContentLeft = LeftWithin(recentContent, recentPage);

            var newPageItem = backstage.Items.OfType<BackstageTabItem>().Single(item =>
                Equals(item.Header, "New"));
            newPageItem.IsSelected = true;
            await PumpAsync();
            var newPage = Assert.IsType<Grid>(fixture.Window.FindName("NewProfilePage"));
            var newDescription = Assert.IsType<TextBlock>(
                fixture.Window.FindName("NewProfileDescription"));
            double newDescriptionLeft = LeftWithin(newDescription, newPage);

            var printPageItem = backstage.Items.OfType<BackstageTabItem>().Single(item =>
                Equals(item.Header, "Print"));
            printPageItem.IsSelected = true;
            await PumpAsync();
            var printPage = Assert.IsType<Grid>(fixture.Window.FindName("PrintBackstagePage"));
            double printPageLeft = LeftWithin(printPage, backstage);

            fixture.Window.Width = 1800;
            await PumpAsync();

            home.IsSelected = true;
            await PumpAsync();
            Assert.Equal(recentDescriptionLeft, LeftWithin(recentDescription, recentPage), 3);
            Assert.Equal(recentContentLeft, LeftWithin(recentContent, recentPage), 3);

            newPageItem.IsSelected = true;
            await PumpAsync();
            Assert.Equal(newDescriptionLeft, LeftWithin(newDescription, newPage), 3);

            printPageItem.IsSelected = true;
            await PumpAsync();
            Assert.Equal(printPageLeft, LeftWithin(printPage, backstage), 3);
        });
    }

    [Fact]
    public async Task MainWindowContractAndEditorLifecycleAreWiredOnTheRealTree()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            using var fixture = new WindowFixture(withRecentFile: true);
            fixture.Show();
            await PumpAsync();
            AssertRuntimeContract(fixture);
            await AssertBackstageNewGalleryAsync(fixture);
            await AssertBackstageFileCommandsRestoreEditorFocusAsync(fixture);
            Assert.True(await fixture.Shell.NewAsync(WriterDocumentProfiles.RibbonKitWriter));
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
            await AssertStructuredContentIntegrationAsync(fixture);

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
            Assert.False(Assert.IsType<RibbonTab>(fixture.Window.FindName("InsertTab")).IsEnabled);
            Assert.False(Assert.IsType<RibbonButton>(fixture.Window.FindName("InsertDateTimeButton")).IsEnabled);
            fixture.Editor.IsReadOnly = false;
            fixture.Editor.IsEnabled = false;
            editingController.RefreshState();
            Assert.False(editingController.State.IsEnabled);
            Assert.False(Assert.IsType<RibbonButton>(fixture.Window.FindName("CopyButton")).IsEnabled);
            Assert.False(Assert.IsType<RibbonTab>(fixture.Window.FindName("InsertTab")).IsEnabled);
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
            backstage.Items.OfType<BackstageTabItem>().Single(item =>
                Equals(item.Header, "Home")).IsSelected = true;
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
            Assert.Equal(3, recentButtons.Length);
            Assert.Equal(3, recentButtons.Select(button =>
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

    private static async Task AssertStructuredContentIntegrationAsync(WindowFixture fixture)
    {
        var window = fixture.Window;
        var editor = fixture.Editor;
        var insertTab = Assert.IsType<RibbonTab>(window.FindName("InsertTab"));
        var tableToolsTab = Assert.IsType<RibbonTab>(window.FindName("TableToolsTab"));
        var pictureToolsTab = Assert.IsType<RibbonTab>(window.FindName("PictureToolsTab"));
        Assert.Equal(new[] { "Table", "Illustrations", "Links", "Text" },
            insertTab.Groups.Select(group => group.Header?.ToString()).ToArray());
        Assert.True(tableToolsTab.IsContextual);
        Assert.Equal(Visibility.Collapsed, tableToolsTab.Visibility);
        Assert.Equal(new[] { "Rows & Columns", "Merge", "Cell Size", "Alignment", "Design" },
            tableToolsTab.Groups.Select(group => group.Header?.ToString()).ToArray());
        Assert.False(string.IsNullOrWhiteSpace(Ribbon.GetCommandId(insertTab)));
        Assert.False(string.IsNullOrWhiteSpace(KeyTip.GetKeys(insertTab)));
        Assert.False(string.IsNullOrWhiteSpace(AutomationProperties.GetHelpText(tableToolsTab)));
        Assert.True(pictureToolsTab.IsContextual);
        Assert.Equal(Visibility.Collapsed, pictureToolsTab.Visibility);
        Assert.Equal(new[] { "Size", "Picture" },
            pictureToolsTab.Groups.Select(group => group.Header?.ToString()).ToArray());
        Assert.False(string.IsNullOrWhiteSpace(Ribbon.GetCommandId(pictureToolsTab)));
        Assert.False(string.IsNullOrWhiteSpace(KeyTip.GetKeys(pictureToolsTab)));
        Assert.False(string.IsNullOrWhiteSpace(AutomationProperties.GetHelpText(pictureToolsTab)));
        var pageTab = Assert.IsType<RibbonTab>(window.FindName("PageTab"));
        Assert.True(fixture.Ribbon.Tabs.IndexOf(tableToolsTab) < fixture.Ribbon.Tabs.IndexOf(pageTab));
        Assert.True(fixture.Ribbon.Tabs.IndexOf(pictureToolsTab) < fixture.Ribbon.Tabs.IndexOf(pageTab));

        var insertButtons = new[]
        {
            "InsertPictureButton", "InsertHyperlinkButton", "InsertDateTimeButton"
        }.Select(name => Assert.IsType<RibbonButton>(window.FindName(name))).ToArray();
        Assert.All(insertButtons, button =>
        {
            Assert.Equal(RibbonControlSize.Large, button.Size);
            Assert.False(string.IsNullOrWhiteSpace(Ribbon.GetCommandId(button)));
            Assert.False(string.IsNullOrWhiteSpace(KeyTip.GetKeys(button)));
            Assert.False(string.IsNullOrWhiteSpace(AutomationProperties.GetAutomationId(button)));
            Assert.False(string.IsNullOrWhiteSpace(AutomationProperties.GetName(button)));
            AssertScreenTip(button);
        });

        var picker = Assert.IsType<InRibbonGallery>(window.FindName("TableGridPicker"));
        Assert.Equal(25, picker.Items.Count);
        Assert.Equal(FlowDirection.LeftToRight, picker.FlowDirection);
        Assert.Equal("Writer.Insert.Table.Grid", Ribbon.GetCommandId(picker));
        Assert.Equal("TG", KeyTip.GetKeys(picker));
        Assert.Contains("preserve", AutomationProperties.GetHelpText(picker),
            StringComparison.OrdinalIgnoreCase);
        var pickerItems = picker.Items.Cast<RibbonGalleryItem>().ToArray();
        var quickItems = pickerItems.Take(24).ToArray();
        Assert.Equal(new WriterTableGridChoice(1, 1), Assert.IsType<WriterTableGridChoice>(quickItems[0].Tag));
        Assert.Equal(new WriterTableGridChoice(3, 8), Assert.IsType<WriterTableGridChoice>(quickItems[^1].Tag));
        Assert.Equal(24, quickItems.Select(Ribbon.GetCommandId).Distinct(StringComparer.Ordinal).Count());
        Assert.All(quickItems, item =>
        {
            var invokeButton = Assert.IsType<Button>(item.Content);
            Assert.IsType<WriterTableGridCellPreview>(invokeButton.Content);
            Assert.IsType<RibbonScreenTip>(item.ToolTip);
            Assert.False(invokeButton.IsHitTestVisible);
            Assert.False(string.IsNullOrWhiteSpace(AutomationProperties.GetAutomationId(invokeButton)));
            Assert.False(string.IsNullOrWhiteSpace(AutomationProperties.GetName(invokeButton)));
            Assert.False(string.IsNullOrWhiteSpace(Ribbon.GetCommandId(item)));
            Assert.False(string.IsNullOrWhiteSpace(AutomationProperties.GetAutomationId(item)));
            Assert.False(string.IsNullOrWhiteSpace(AutomationProperties.GetName(item)));
            Assert.Contains("preserve", AutomationProperties.GetHelpText(item),
                StringComparison.OrdinalIgnoreCase);
        });
        var footer = Assert.IsType<StackPanel>(pickerItems[^1].Content);
        Assert.IsType<Separator>(footer.Children[0]);
        var customTable = Assert.IsType<Button>(footer.Children[1]);
        Assert.Equal("Writer.Insert.Table.Custom", Ribbon.GetCommandId(customTable));
        Assert.Equal("InsertCustomTableSize", AutomationProperties.GetAutomationId(customTable));
        Assert.Equal("Custom Table", AutomationProperties.GetName(customTable));
        Assert.True(customTable.IsHitTestVisible);
        Assert.True(customTable.Focusable);
        Assert.True(customTable.IsTabStop);
        picker.RaiseEvent(new RibbonGalleryPreviewEventArgs(
            RibbonGallery.ItemPreviewEvent, picker, quickItems[10]));
        Assert.Contains(quickItems, item =>
            Assert.IsType<WriterTableGridCellPreview>(
                Assert.IsType<Button>(item.Content).Content).IsHighlighted);
        picker.RaiseEvent(new RibbonGalleryPreviewEventArgs(
            RibbonGallery.ItemPreviewEvent, picker, pickerItems[^1]));
        Assert.DoesNotContain(quickItems, item =>
            Assert.IsType<WriterTableGridCellPreview>(
                Assert.IsType<Button>(item.Content).Content).IsHighlighted);
        var customEnter = new KeyEventArgs(Keyboard.PrimaryDevice,
            PresentationSource.FromVisual(window), 0, Key.Enter)
        {
            RoutedEvent = Keyboard.PreviewKeyDownEvent
        };
        customTable.RaiseEvent(customEnter);
        Assert.False(customEnter.Handled);
        Assert.IsType<RibbonScreenTip>(picker.ToolTip);
        picker.ApplyTemplate();
        var pickerPopupHost = Assert.IsType<Border>(
            picker.Template.FindName("PART_PopupHost", picker));
        pickerPopupHost.Background = null;
        var selectedTab = fixture.Ribbon.SelectedTab;
        fixture.Ribbon.SelectedTab = insertTab;
        await PumpAsync();
        picker.IsDropDownOpen = true;
        await PumpAsync();
        Assert.NotNull(pickerPopupHost.Background);
        picker.IsDropDownOpen = false;
        fixture.Ribbon.SelectedTab = selectedTab;

        var tableCommands = new[]
        {
            "InsertTableRowAboveButton", "InsertTableRowBelowButton",
            "InsertTableColumnLeftButton", "InsertTableColumnRightButton",
            "DeleteTableRowButton", "DeleteTableColumnButton", "MergeTableCellsButton",
            "SplitTableCellButton", "InsertTableLiteralTabButton", "TableRowHeightButton",
            "TableColumnWidthButton", "DistributeTableRowsButton",
            "DistributeTableColumnsButton", "TableAlignmentButton", "TableBordersButton",
            "TableBackgroundButton"
        }.Select(name => Assert.IsAssignableFrom<FrameworkElement>(window.FindName(name))).ToArray();
        Assert.Equal(tableCommands.Length,
            tableCommands.Select(KeyTip.GetKeys).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.All(tableCommands, command =>
        {
            Assert.False(string.IsNullOrWhiteSpace(Ribbon.GetCommandId(command)));
            Assert.False(string.IsNullOrWhiteSpace(KeyTip.GetKeys(command)));
            Assert.False(string.IsNullOrWhiteSpace(AutomationProperties.GetAutomationId(command)));
            Assert.False(string.IsNullOrWhiteSpace(AutomationProperties.GetName(command)));
            AssertScreenTip(command);
        });

        Assert.True(window.CurrentProfile.Supports(WriterDocumentCommandCapabilities.TableEditing));
        Assert.True(window.CurrentProfile.Capabilities.PreservesTables);
        var document = fixture.Shell.CurrentDocument;
        document.Content.Blocks.Clear();
        var paragraph = new Paragraph(new Run("table anchor"));
        document.Content.Blocks.Add(paragraph);
        editor.Selection.Select(paragraph.ContentStart, paragraph.ContentStart);
        document.MarkClean();

        var twoByThree = quickItems.Single(item =>
            Assert.IsType<WriterTableGridChoice>(item.Tag) == new WriterTableGridChoice(2, 3));
        var invokeButton = Assert.IsType<Button>(twoByThree.Content);
        var pickerPeer = UIElementAutomationPeer.CreatePeerForElement(invokeButton)
            ?? new ButtonAutomationPeer(invokeButton);
        var pickerInvoke = Assert.IsAssignableFrom<IInvokeProvider>(
            pickerPeer.GetPattern(PatternInterface.Invoke));
        pickerInvoke.Invoke();
        await PumpAsync();
        var table = Assert.Single(document.Content.Blocks.OfType<Table>());
        Assert.Equal(2, table.RowGroups[0].Rows.Count);
        Assert.All(table.RowGroups[0].Rows, row => Assert.Equal(3, row.Cells.Count));
        Assert.NotNull(table.BorderBrush);
        Assert.All(table.RowGroups[0].Rows.Cast<TableRow>()
            .SelectMany(row => row.Cells.Cast<TableCell>()), cell =>
        {
            Assert.NotNull(cell.BorderBrush);
            Assert.Equal(new Thickness(0.5), cell.BorderThickness);
        });
        Assert.True(window.TableInteractionController.IsInTable);
        Assert.Equal(Visibility.Visible, tableToolsTab.Visibility);
        Assert.True(tableToolsTab.IsEnabled);
        Assert.True(document.IsDirty);
        Assert.True(Assert.IsType<RibbonButton>(window.FindName("DeleteTableRowButton")).IsEnabled);
        Assert.True(Assert.IsType<RibbonButton>(window.FindName("DeleteTableColumnButton")).IsEnabled);

        window.EditorContextMenuController.Refresh();
        var tableContextMenu = FindMenuItem(window.EditorContextMenuController.Menu.Items, "Table");
        var tableInsertMenu = FindMenuItem(tableContextMenu.Items, "Insert");
        var contextRowBelow = FindMenuItem(tableInsertMenu.Items, "Row Below");
        Assert.NotNull(contextRowBelow.Command);
        Assert.True(contextRowBelow.Command!.CanExecute(contextRowBelow.CommandParameter));
        Assert.NotNull(FindMenuItem(tableContextMenu.Items, "Delete"));
        Assert.NotNull(FindMenuItem(tableContextMenu.Items, "Cell Size"));
        Assert.NotNull(FindMenuItem(tableContextMenu.Items, "Borders"));
        Assert.NotNull(FindMenuItem(tableContextMenu.Items, "Background"));

        var insertRowBelow = Assert.IsType<RibbonButton>(window.FindName("InsertTableRowBelowButton"));
        fixture.Ribbon.SelectedTab = tableToolsTab;
        Click(insertRowBelow);
        SetShellBusy(fixture.Shell, true);
        await Dispatcher.Yield(DispatcherPriority.ContextIdle);
        Assert.Equal(2, table.RowGroups[0].Rows.Count);
        Assert.Same(tableToolsTab, fixture.Ribbon.SelectedTab);
        SetShellBusy(fixture.Shell, false);

        contextRowBelow.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent, contextRowBelow));
        await PumpAsync();
        Assert.Equal(3, table.RowGroups[0].Rows.Count);
        Assert.Same(tableToolsTab, fixture.Ribbon.SelectedTab);
        AssertEditorFocusRestored(fixture);

        editor.Selection.Select(paragraph.ContentEnd, paragraph.ContentEnd);
        window.TableInteractionController.Refresh();
        await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
        Assert.Equal(Visibility.Collapsed, tableToolsTab.Visibility);

        document.Content.Blocks.Clear();
        var singleCellAnchor = new Paragraph();
        document.Content.Blocks.Add(singleCellAnchor);
        editor.Selection.Select(singleCellAnchor.ContentStart, singleCellAnchor.ContentStart);
        Assert.True(window.TryInsertTable(1, 1));
        await Dispatcher.Yield(DispatcherPriority.ContextIdle);
        Assert.False(Assert.IsType<RibbonButton>(window.FindName("DeleteTableRowButton")).IsEnabled);
        Assert.False(Assert.IsType<RibbonButton>(window.FindName("DeleteTableColumnButton")).IsEnabled);

        var unevenTable = new Table();
        var wideGroup = new TableRowGroup();
        var wideRow = new TableRow();
        wideRow.Cells.Add(new TableCell(new Paragraph()));
        wideRow.Cells.Add(new TableCell(new Paragraph()));
        wideGroup.Rows.Add(wideRow);
        var narrowGroup = new TableRowGroup();
        var narrowRow = new TableRow();
        var narrowCell = new TableCell(new Paragraph());
        narrowRow.Cells.Add(narrowCell);
        narrowGroup.Rows.Add(narrowRow);
        unevenTable.RowGroups.Add(wideGroup);
        unevenTable.RowGroups.Add(narrowGroup);
        document.Content.Blocks.Clear();
        document.Content.Blocks.Add(unevenTable);
        editor.Selection.Select(narrowCell.ContentStart, narrowCell.ContentStart);
        window.TableInteractionController.Refresh();
        var deleteColumn = Assert.IsType<RibbonButton>(window.FindName("DeleteTableColumnButton"));
        Assert.False(deleteColumn.IsEnabled);
        editor.Selection.Select(wideRow.Cells[0].ContentStart, wideRow.Cells[0].ContentStart);
        window.TableInteractionController.Refresh();
        Assert.False(deleteColumn.IsEnabled);
        editor.Selection.Select(wideRow.Cells[1].ContentStart, wideRow.Cells[1].ContentStart);
        window.TableInteractionController.Refresh();
        Assert.True(deleteColumn.IsEnabled);
        Click(deleteColumn);
        await Dispatcher.Yield(DispatcherPriority.ContextIdle);
        await PumpAsync();
        var reducedUnevenTable = Assert.Single(document.Content.Blocks.OfType<Table>());
        Assert.All(reducedUnevenTable.RowGroups.Cast<TableRowGroup>(), group =>
            Assert.All(group.Rows.Cast<TableRow>(), row => Assert.Single(row.Cells)));

        var deleteTargetCell = reducedUnevenTable.RowGroups[0].Rows[0].Cells[0];
        editor.Selection.Select(deleteTargetCell.ContentStart, deleteTargetCell.ContentStart);
        window.TableInteractionController.Refresh();
        fixture.Ribbon.SelectedTab = tableToolsTab;
        window.EditorContextMenuController.Refresh();
        var deleteTableMenu = FindMenuItem(
            FindMenuItem(window.EditorContextMenuController.Menu.Items, "Table").Items,
            "Delete");
        var deleteWholeTable = FindMenuItem(deleteTableMenu.Items, "Table");
        deleteWholeTable.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent, deleteWholeTable));
        await PumpAsync();
        Assert.Empty(document.Content.Blocks.OfType<Table>());
        Assert.Equal(Visibility.Collapsed, tableToolsTab.Visibility);
        Assert.NotSame(tableToolsTab, fixture.Ribbon.SelectedTab);
        Assert.True(window.EditingController.TryExecute(ApplicationCommands.Undo));
        Assert.Single(document.Content.Blocks.OfType<Table>());
        Assert.True(window.EditingController.TryExecute(ApplicationCommands.Redo));
        Assert.Empty(document.Content.Blocks.OfType<Table>());

        var objectParagraph = new Paragraph();
        var ordinaryRun = new Run("ordinary ");
        var hyperlink = new Hyperlink(new Run("link"))
        {
            NavigateUri = new Uri("https://example.test")
        };
        var picture = new InlineUIContainer(new Image { Width = 120, Height = 80 });
        objectParagraph.Inlines.Add(ordinaryRun);
        objectParagraph.Inlines.Add(hyperlink);
        objectParagraph.Inlines.Add(new Run(" "));
        objectParagraph.Inlines.Add(picture);
        document.Content.Blocks.Clear();
        document.Content.Blocks.Add(objectParagraph);

        Assert.True(window.PictureInteractionController.SelectPicture(picture));
        Assert.Equal(Visibility.Visible, pictureToolsTab.Visibility);
        Assert.True(pictureToolsTab.IsEnabled);
        fixture.Ribbon.SelectedTab = pictureToolsTab;
        var pictureWidth = Assert.IsType<RibbonTextBox>(window.FindName("PictureWidthBox"));
        var pictureHeight = Assert.IsType<RibbonTextBox>(window.FindName("PictureHeightBox"));
        var pictureDimensions = Assert.IsType<Grid>(
            window.FindName("PictureDimensionsPanel"));
        Assert.Equal(2, pictureDimensions.ColumnDefinitions.Count);
        Assert.Equal(2, pictureDimensions.RowDefinitions.Count);
        Assert.Same(pictureDimensions, pictureWidth.Parent);
        Assert.Same(pictureDimensions, pictureHeight.Parent);
        Assert.Equal(1, Grid.GetColumn(pictureWidth));
        Assert.Equal(1, Grid.GetColumn(pictureHeight));
        Assert.Equal(0, Grid.GetRow(pictureWidth));
        Assert.Equal(1, Grid.GetRow(pictureHeight));
        Assert.Equal("120", pictureWidth.Text);
        Assert.Equal("80", pictureHeight.Text);
        pictureWidth.Focus();
        await PumpAsync();
        Assert.True(window.PictureInteractionController.HasSelection);
        Assert.Equal(Visibility.Visible, pictureToolsTab.Visibility);
        Assert.Same(pictureToolsTab, fixture.Ribbon.SelectedTab);
        pictureWidth.Text = "160";
        pictureHeight.Text = "80";
        Click(Assert.IsType<RibbonButton>(window.FindName("ApplyPictureSizeButton")));
        await PumpAsync();
        Assert.True(window.PictureInteractionController.HasSelection);
        Assert.Equal(Visibility.Visible, pictureToolsTab.Visibility);
        Assert.Same(pictureToolsTab, fixture.Ribbon.SelectedTab);
        picture = window.PictureInteractionController.SelectedContainer!;
        Assert.NotNull(picture);
        Assert.True(WriterInlineInsertion.TryGetImage(picture, out var resizedPicture));
        Assert.Equal(160, resizedPicture.Width, 6);
        Assert.Equal(80, resizedPicture.Height, 6);

        var hyperlinkRun = Assert.IsType<Run>(hyperlink.Inlines.FirstInline);
        var hyperlinkCaret = hyperlinkRun.ContentStart.GetPositionAtOffset(1,
            LogicalDirection.Forward);
        editor.Selection.Select(hyperlinkCaret, hyperlinkCaret);
        window.EditorContextMenuController.Refresh();
        var hyperlinkMenu = FindMenuItem(window.EditorContextMenuController.Menu.Items, "Hyperlink");
        Assert.NotNull(FindMenuItem(hyperlinkMenu.Items, "Edit Hyperlink..."));
        var removeHyperlink = FindMenuItem(hyperlinkMenu.Items, "Remove Hyperlink");
        removeHyperlink.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent, removeHyperlink));
        Assert.Null(hyperlink.Parent);
        Assert.Contains("link", new TextRange(objectParagraph.ContentStart,
            objectParagraph.ContentEnd).Text);

        editor.Selection.Select(picture.ElementStart, picture.ElementEnd);
        window.EditorContextMenuController.Refresh();
        var pictureMenu = FindMenuItem(window.EditorContextMenuController.Menu.Items, "Picture");
        var removePicture = FindMenuItem(pictureMenu.Items, "Remove Picture");
        removePicture.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent, removePicture));
        Assert.Null(picture.Parent);
        Assert.Equal(Visibility.Collapsed, pictureToolsTab.Visibility);
        Assert.Same(Assert.IsType<RibbonTab>(window.FindName("HomeTab")), fixture.Ribbon.SelectedTab);
        Assert.True(editor.CanUndo);
        Assert.True(window.EditingController.State.CanUndo);
        Assert.True(window.EditingController.TryExecute(ApplicationCommands.Undo));
        Assert.Single(objectParagraph.Inlines.OfType<InlineUIContainer>());
        Assert.True(editor.CanRedo);
        Assert.True(window.EditingController.State.CanRedo);
        Assert.True(window.EditingController.TryExecute(ApplicationCommands.Redo));
        Assert.Empty(objectParagraph.Inlines.OfType<InlineUIContainer>());

        editor.Selection.Select(ordinaryRun.ContentStart, ordinaryRun.ContentEnd);
        window.EditorContextMenuController.Refresh();
        var ordinaryHeaders = window.EditorContextMenuController.Menu.Items.OfType<MenuItem>()
            .Select(item => item.Header?.ToString()).ToArray();
        Assert.DoesNotContain("Table", ordinaryHeaders);
        Assert.DoesNotContain("Picture", ordinaryHeaders);
        Assert.DoesNotContain("Hyperlink", ordinaryHeaders);

        document.Content.Blocks.Clear();
        document.Content.Blocks.Add(new Paragraph());
        editor.IsUndoEnabled = false;
        editor.IsUndoEnabled = true;
        document.MarkClean();
        fixture.Editor.SelectAll();
        var imageBytes = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");
        Assert.True(new WriterImageService().TryInsertImage(fixture.Editor, imageBytes,
            new WriterImageInsertionOptions { WidthDip = 120, HeightDip = 80 }));
        var recentPath = fixture.File("recent-picture.rkw");
        var persistence = new WriterDocumentPersistence();
        Assert.True(await persistence.SaveAsync(fixture.Shell.CurrentDocument, recentPath,
            WriterDocumentFormat.RibbonKitWriter, default));
        fixture.Shell.CurrentDocument.MarkClean();
        fixture.Persistence.LoadHandler = persistence.LoadAsync;
        Assert.True(await fixture.Shell.OpenRecentAsync(new RecentFileEntry(recentPath,
            WriterDocumentFormat.RibbonKitWriter, DateTimeOffset.UtcNow)));
        await PumpAsync();

        var loadedPicture = Assert.Single(
            WriterInlineInsertion.EnumerateImages(fixture.Editor.Document));
        fixture.Editor.AppendText("undo anchor");
        fixture.Editor.Selection.Select(loadedPicture.ElementStart, loadedPicture.ElementEnd);
        window.EditorContextMenuController.Refresh();
        var loadedPictureMenu = FindMenuItem(
            window.EditorContextMenuController.Menu.Items, "Picture");
        var removeLoadedPicture = FindMenuItem(loadedPictureMenu.Items, "Remove Picture");
        removeLoadedPicture.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent, removeLoadedPicture));
        Assert.Empty(WriterInlineInsertion.EnumerateImages(fixture.Editor.Document));
        Assert.True(window.EditingController.TryExecute(ApplicationCommands.Undo));
        Assert.Single(WriterInlineInsertion.EnumerateImages(fixture.Editor.Document));
        Assert.True(window.EditingController.TryExecute(ApplicationCommands.Undo));
        Assert.DoesNotContain("undo anchor", TextOf(fixture.Editor.Document));
        Assert.Single(WriterInlineInsertion.EnumerateImages(fixture.Editor.Document));
        Assert.True(window.EditingController.TryExecute(ApplicationCommands.Redo));
        Assert.Contains("undo anchor", TextOf(fixture.Editor.Document));
        Assert.Single(WriterInlineInsertion.EnumerateImages(fixture.Editor.Document));
        Assert.True(window.EditingController.TryExecute(ApplicationCommands.Redo));
        Assert.Empty(WriterInlineInsertion.EnumerateImages(fixture.Editor.Document));
        Assert.True(window.EditingController.TryExecute(ApplicationCommands.Undo));
        Assert.Single(WriterInlineInsertion.EnumerateImages(fixture.Editor.Document));

        var keyboardPicture = Assert.Single(
            WriterInlineInsertion.EnumerateImages(fixture.Editor.Document));
        fixture.Editor.Selection.Select(keyboardPicture.ElementStart, keyboardPicture.ElementEnd);
        var deletePictureKey = new KeyEventArgs(Keyboard.PrimaryDevice,
            PresentationSource.FromVisual(window), 0, Key.Delete)
        {
            RoutedEvent = Keyboard.PreviewKeyDownEvent
        };
        fixture.Editor.RaiseEvent(deletePictureKey);
        Assert.True(deletePictureKey.Handled);
        Assert.Empty(WriterInlineInsertion.EnumerateImages(fixture.Editor.Document));
        Assert.True(window.EditingController.TryExecute(ApplicationCommands.Undo));
        keyboardPicture = Assert.Single(
            WriterInlineInsertion.EnumerateImages(fixture.Editor.Document));
        var afterPicture = keyboardPicture.ElementEnd.GetInsertionPosition(
            LogicalDirection.Forward);
        fixture.Editor.Selection.Select(afterPicture, afterPicture);
        var backspacePictureKey = new KeyEventArgs(Keyboard.PrimaryDevice,
            PresentationSource.FromVisual(window), 0, Key.Back)
        {
            RoutedEvent = Keyboard.PreviewKeyDownEvent
        };
        fixture.Editor.RaiseEvent(backspacePictureKey);
        Assert.True(backspacePictureKey.Handled);
        Assert.Empty(WriterInlineInsertion.EnumerateImages(fixture.Editor.Document));
        Assert.True(window.EditingController.TryExecute(ApplicationCommands.Undo));
        Assert.Single(WriterInlineInsertion.EnumerateImages(fixture.Editor.Document));
        using (var preview = new WriterPreviewCloneService().CreateSnapshot(
                   fixture.Editor.Document, fixture.Shell.CurrentDocument.PageSettings))
        {
            var previewPicture = Assert.Single(
                WriterInlineInsertion.EnumerateImages(preview.SourceClone));
            Assert.IsType<Image>(previewPicture.Child);
        }
        var restoredPath = fixture.File("recent-picture-restored.rkw");
        Assert.True(await persistence.SaveAsync(fixture.Shell.CurrentDocument, restoredPath,
            WriterDocumentFormat.RibbonKitWriter, default));
        var restored = await persistence.LoadAsync(restoredPath,
            WriterDocumentFormat.RibbonKitWriter, default);
        Assert.NotNull(restored);
        Assert.Single(WriterInlineInsertion.EnumerateImages(restored!.Content));

        fixture.Shell.CurrentDocument.Content.Blocks.Clear();
        fixture.Shell.CurrentDocument.Content.Blocks.Add(new Paragraph());
        fixture.Shell.CurrentDocument.MarkClean();
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
        fontFamily.SelectedItem = Assert.Single(fontFamily.Items.OfType<WriterFontChoice>(),
            choice => string.Equals(choice.SourceName, "Arial", StringComparison.OrdinalIgnoreCase));
        fontFamily.IsDropDownOpen = false;
        await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
        fixture.Window.EditingController.RefreshState();
        Assert.Equal("Arial", fixture.Window.EditingController.State.FontFamily.Value.Source);

        fontFamily.ApplyTemplate();
        var editableFontBox = Assert.IsType<TextBox>(fontFamily.Template.FindName(
            "PART_EditableTextBox", fontFamily));
        Assert.True(editableFontBox.Focus());
        fontFamily.Text = "Ari";
        fixture.Window.EditingController.RefreshState();
        Assert.Equal("Ari", fontFamily.Text);
        fontFamily.Text = "Arial";
        Assert.True(fixture.Editor.Focus());
        await Dispatcher.Yield(DispatcherPriority.ContextIdle);
        fixture.Window.EditingController.RefreshState();
        Assert.Equal("Arial", fixture.Window.EditingController.State.FontFamily.Value.Source);

        var fontSize = Assert.IsType<RibbonComboBox>(fixture.Window.FindName("FontSizeCombo"));
        fontSize.IsDropDownOpen = true;
        fontSize.SelectedItem = Assert.Single(fontSize.Items.OfType<ComboBoxItem>(),
            item => string.Equals(item.Content?.ToString(), "12", StringComparison.Ordinal));
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

        var textColor = Assert.IsType<RibbonSplitButton>(fixture.Window.FindName("TextColorButton"));
        Invoke(Assert.Single(textColor.Items.OfType<RibbonMenuItem>(),
            item => string.Equals(item.Header, "Blue", StringComparison.Ordinal)));
        await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
        fixture.Window.EditingController.RefreshState();
        Assert.Equal(Color.FromRgb(0x00, 0x70, 0xC0),
            fixture.Window.EditingController.State.Foreground.Value);
        AssertEditorFocusRestored(fixture);

        var highlightColor = Assert.IsType<RibbonSplitButton>(fixture.Window.FindName("HighlightColorButton"));
        Invoke(Assert.Single(highlightColor.Items.OfType<RibbonMenuItem>(),
            item => string.Equals(item.Header, "Yellow", StringComparison.Ordinal)));
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
        Assert.False(Assert.IsType<RibbonSplitButton>(
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
        if (fixture.Window.CurrentProfile.Format != WriterDocumentFormat.RibbonKitWriter)
            Assert.True(await fixture.Shell.NewAsync(WriterDocumentProfiles.RibbonKitWriter));
        fixture.Window.Width = 1500;
        fixture.Window.UpdateLayout();
        var window = fixture.Window;
        var ribbon = fixture.Ribbon;
        var editor = fixture.Editor;
        var surface = Assert.IsType<WriterEditorSurface>(window.FindName("EditorSurface"));
        var preview = Assert.IsType<WriterDocumentPreviewView>(window.FindName("PreviewView"));
        var previewTopSeparator = Assert.IsType<Border>(window.FindName("PreviewTopSeparator"));
        Assert.Equal(Visibility.Collapsed, previewTopSeparator.Visibility);
        Assert.Equal(1, previewTopSeparator.Height);
        Assert.False(previewTopSeparator.IsHitTestVisible);
        Assert.True(DependencyPropertyHelper.GetValueSource(
            previewTopSeparator, Border.BackgroundProperty).IsExpression);

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
            ("ZoomInButton", "ZoomIn", "Writer.Home.Editing.ZoomIn"),
            ("RulerToggleButton", "ViewRuler", "Writer.View.Show.Ruler"),
            ("MarginGuidesToggleButton", "ViewMarginGuides", "Writer.View.Show.MarginGuides")
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

        Assert.Equal(7, ribbon.Tabs.Count);
        var home = ribbon.Tabs.Single(tab => Equals(tab.Header, "Home"));
        Assert.DoesNotContain(FindLogicalDescendants<FrameworkElement>(home), element =>
            AutomationProperties.GetAutomationId(element) is "ZoomOut" or "ZoomReset" or "ZoomIn");
        var view = ribbon.Tabs.Single(tab => Equals(tab.Header, "View"));
        Assert.Equal(new[] { "Document Views", "Zoom", "Show" },
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

        var ruler = Assert.IsType<WriterRuler>(window.FindName("HorizontalRuler"));
        var editorTopSeparator = Assert.IsType<Border>(window.FindName("EditorTopSeparator"));
        Assert.Equal(Visibility.Visible, ruler.Visibility);
        Assert.Equal(Visibility.Collapsed, editorTopSeparator.Visibility);
        Toggle(Assert.IsType<RibbonToggleButton>(window.FindName("RulerToggleButton")));
        await Dispatcher.Yield(DispatcherPriority.DataBind);
        Assert.Equal(Visibility.Collapsed, ruler.Visibility);
        Assert.Equal(Visibility.Visible, editorTopSeparator.Visibility);
        Toggle(Assert.IsType<RibbonToggleButton>(window.FindName("RulerToggleButton")));
        await Dispatcher.Yield(DispatcherPriority.DataBind);
        Assert.Equal(Visibility.Visible, ruler.Visibility);
        Assert.Equal(Visibility.Collapsed, editorTopSeparator.Visibility);

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
        Assert.Equal(Visibility.Visible, previewTopSeparator.Visibility);
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
        Assert.Same(firstSnapshot!.PrintPaginator, printDevice.SubmittedPaginator);
        Assert.NotSame(firstSnapshot.Paginator, printDevice.SubmittedPaginator);
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
        Assert.Equal(Visibility.Collapsed, previewTopSeparator.Visibility);

        var pageSettingChanges = 0;
        document.PropertyChanged += CountPageSettings;
        ribbon.SelectedTab = ribbon.Tabs.Single(tab => Equals(tab.Header, "Page"));
        window.UpdateLayout();
        Assert.Equal(WriterDocumentFormat.RibbonKitWriter, window.CurrentProfile.Format);
        Assert.True(Assert.IsType<RibbonGroup>(window.FindName("PageSetupGroup")).IsEnabled);
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
        Assert.Same(colouredSnapshot.PrintPaginator, colouredPrintDevice.SubmittedPaginator);
        Assert.NotSame(colouredSnapshot.Paginator, colouredPrintDevice.SubmittedPaginator);

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
        Assert.Equal(Visibility.Collapsed, previewTopSeparator.Visibility);
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
        foreach (var automationId in new[] { "FileOpen", "FileSave", "FileSaveAs" })
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

        // The focus contract is owned by the single IsBackstageOpen transition observer, so a
        // close with no particular menu action receives the same restoration.
        fixture.Ribbon.IsBackstageOpen = true;
        await PumpAsync();
        FocusManager.SetFocusedElement(fixture.Window, fixture.Ribbon);
        fixture.Ribbon.IsBackstageOpen = false;
        await PumpAsync();
        AssertEditorFocusRestored(fixture);
    }

    private static void AssertBackstageScrollBarChrome(ScrollViewer scrollViewer)
    {
        var sharedStyle = Assert.IsType<Style>(
            scrollViewer.FindResource("RibbonKit.ScrollBarStyle"));
        var scopedStyle = Assert.IsType<Style>(
            scrollViewer.Resources[typeof(ScrollBar)]);
        Assert.Same(sharedStyle, scopedStyle.BasedOn?.BasedOn);

        var scrollBars = FindVisualDescendants<ScrollBar>(scrollViewer).ToArray();
        Assert.NotEmpty(scrollBars);
        Assert.All(scrollBars, scrollBar => Assert.Same(scopedStyle, scrollBar.Style));
        Assert.All(scrollBars, scrollBar => Assert.Equal(new Thickness(0), scrollBar.Margin));
    }

    private static double LeftWithin(FrameworkElement element, Visual ancestor) =>
        element.TransformToAncestor(ancestor).Transform(new Point()).X;

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
        var paragraphDialog = window.CreateParagraphDialog();
        try
        {
            Assert.Equal("0", Assert.IsAssignableFrom<ComboBox>(
                paragraphDialog.FindName("LeftIndentBox")).Text);
            Assert.Equal("0", Assert.IsAssignableFrom<ComboBox>(
                paragraphDialog.FindName("RightIndentBox")).Text);
            Assert.Equal("0", Assert.IsAssignableFrom<ComboBox>(
                paragraphDialog.FindName("SpacingBeforeBox")).Text);
            Assert.Equal("0", Assert.IsAssignableFrom<ComboBox>(
                paragraphDialog.FindName("SpacingAfterBox")).Text);
            Assert.Equal("None", Assert.IsAssignableFrom<ComboBox>(
                paragraphDialog.FindName("SpecialBox")).Text);
        }
        finally
        {
            paragraphDialog.Close();
        }
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
        var newPage = backstage.Items.OfType<BackstageTabItem>().Single(item =>
            AutomationProperties.GetAutomationId(item) == "FileNew");
        Assert.False(newPage.IsButton);
        Assert.Null(newPage.Command);
        var fileActions = backstage.Items.OfType<BackstageTabItem>().Where(item => item.IsButton).ToArray();
        Assert.Equal(new[] { "Open", "Save", "Save As", "Exit" },
            fileActions.Select(item => item.Header?.ToString()).ToArray());
        Assert.Equal(new[] { "FileOpen", "FileSave", "FileSaveAs", "FileExit" },
            fileActions.Select(AutomationProperties.GetAutomationId).ToArray());
        Assert.All(fileActions, item => Assert.False(string.IsNullOrWhiteSpace(
            AutomationProperties.GetName(item))));
        Assert.All(fileActions, item => Assert.NotNull(item.Command));

        Assert.Equal(new[] { "Home", "Insert", "Table Tools", "Picture Tools", "Page", "View", "Print Preview" },
            ribbon.Tabs.Select(tab => tab.Header?.ToString()).ToArray());
        var previewTab = Assert.Single(ribbon.Tabs, tab => tab.IsModal);
        Assert.Equal(Visibility.Collapsed, previewTab.Visibility);
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
            ("TextColorMoreColors", "Writer.Home.Font.TextColor.More"),
            ("HighlightNone", "Writer.Home.Font.Highlight.None"),
            ("HighlightMoreColors", "Writer.Home.Font.Highlight.More"),
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
        }
        var textColor = Assert.IsType<RibbonSplitButton>(window.FindName("TextColorButton"));
        var highlightColor = Assert.IsType<RibbonSplitButton>(window.FindName("HighlightColorButton"));
        Assert.True(textColor.Items.OfType<RibbonMenuItem>().Count() >= 10);
        Assert.True(highlightColor.Items.OfType<RibbonMenuItem>().Count() >= 10);
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
        var paste = Assert.IsType<RibbonSplitButton>(window.FindName("PasteButton"));
        Assert.Equal(RibbonControlSize.Large, paste.Size);
        Assert.Equal(RibbonSplitButtonLayout.Vertical, paste.Layout);
        Assert.Same(fixture.Window.TryFindResource("Icon.WriterPaste.Large"), paste.LargeIcon);
        var paragraphSpacing = Assert.IsType<RibbonDropDownButton>(window.FindName("ParagraphSpacingButton"));
        Assert.Equal(RibbonControlSize.Large, paragraphSpacing.Size);
        Assert.NotNull(paragraphSpacing.LargeIcon);
        var paragraphSeparator = Assert.IsType<RibbonGroupSeparator>(
            window.FindName("ParagraphGroupSeparator"));
        Assert.False(paragraphSeparator.Focusable);
        Assert.False(paragraphSeparator.IsHitTestVisible);
        Assert.True(string.IsNullOrWhiteSpace(KeyTip.GetKeys(paragraphSeparator)));
        Assert.Null(UIElementAutomationPeer.CreatePeerForElement(paragraphSeparator));
        Assert.False(ribbon.AddToQuickAccess(paragraphSeparator));
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

        Assert.True(iconKeys.Length >= 110, $"Expected at least 110 Writer icons, found {iconKeys.Length}.");
        foreach (var key in new[]
        {
            "Icon.WriterNew", "Icon.WriterOpen", "Icon.WriterSaveAs", "Icon.WriterExportPdf",
            "Icon.WriterBackstageNew", "Icon.WriterBackstageOpen", "Icon.WriterBackstageSave",
            "Icon.WriterBackstageSaveAs", "Icon.WriterBackstagePrint",
            "Icon.WriterPrint", "Icon.WriterPrintPreview", "Icon.WriterPageSize", "Icon.WriterPortrait",
            "Icon.WriterLandscape", "Icon.WriterMargins", "Icon.WriterPageColor", "Icon.WriterColumns",
            "Icon.WriterPageBreak", "Icon.WriterEditLayout", "Icon.WriterTwoPages", "Icon.WriterPageWidth",
            "Icon.WriterImage", "Icon.WriterHyperlink", "Icon.WriterDateTime", "Icon.WriterTable",
            "Icon.WriterAddRowAbove", "Icon.WriterAddColumnRight", "Icon.WriterDeleteRow",
            "Icon.WriterMergeCells", "Icon.WriterSplitCells", "Icon.WriterDistributeColumns",
            "Icon.WriterCellAlignMiddle", "Icon.WriterCellShading", "Icon.WriterBorders",
            "Icon.WriterTheme", "Icon.WriterDarkMode", "Icon.WriterBackdrop", "Icon.WriterCustomizeRibbon",
            "Icon.WriterHome", "Icon.WriterOptions", "Icon.WriterExit", "Icon.WriterWarning", "Icon.WriterInformation", "Icon.WriterError",
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

    private static void SetShellBusy(WriterShellViewModel shell, bool value)
    {
        var property = typeof(WriterShellViewModel).GetProperty(nameof(WriterShellViewModel.IsBusy));
        Assert.NotNull(property);
        property.SetValue(shell, value);
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
                new WriterUnsavedChangesDecider(Dialogs), new WriterSaveDestinationProvider(Dialogs),
                transitionDecider: new WriterFormatTransitionDecider(Dialogs));
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
        public int PlainTextWarningCalls { get; private set; }
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
        public Task<WriterFormatTransitionDecision> ConfirmFormatTransitionAsync(
            WriterDocument document, WriterDocumentFormatTransition transition,
            CancellationToken cancellationToken = default)
        {
            if (transition.RequiresConfirmation)
                PlainTextWarningCalls++;
            return Task.FromResult(PlainTextFidelity
                ? WriterFormatTransitionDecision.Continue
                : WriterFormatTransitionDecision.Cancel);
        }
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

    private static MenuItem FindMenuItem(ItemCollection items, string header) =>
        items.OfType<MenuItem>().Single(item => Equals(item.Header?.ToString(), header));

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
