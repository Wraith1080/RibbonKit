using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using RibbonKit.Controls;
using RibbonKit.Theming;
using RibbonKit.Writer.Appearance;
using RibbonKit.Writer.Editing;
using RibbonKit.Writer.Tests.Document;
using Xunit;

namespace RibbonKit.Writer.Tests.Shell;

public sealed partial class MainWindowIntegrationTests
{
    [Theory]
    [InlineData(RibbonTheme.Office2024Flat, false, RibbonDensity.Compact, FlowDirection.LeftToRight)]
    [InlineData(RibbonTheme.Office2024Flat, true, RibbonDensity.Touch, FlowDirection.RightToLeft)]
    [InlineData(RibbonTheme.CrystalLight, false, RibbonDensity.Compact, FlowDirection.RightToLeft)]
    [InlineData(RibbonTheme.CrystalLight, true, RibbonDensity.Touch, FlowDirection.LeftToRight)]
    public async Task TablePickerKeepsEightColumnsAndFullWidthFooterAcrossPresentations(
        RibbonTheme theme, bool dark, RibbonDensity density, FlowDirection direction)
    {
        await StaTestHelper.RunAsync(async () =>
        {
            using var directory = new TemporaryDirectory();
            var settings = new WriterSettingsStore(new WriterSettingsPaths(directory.File("appearance.json"), directory.File("ribbon.json")));
            settings.SaveAppearance(new WriterAppearancePreferences { Theme = theme, DarkPalette = dark, Density = density,
                AnimationLevel = RibbonKit.Animation.RibbonAnimationLevel.None });
            using var fixture = new WindowFixture(settings: settings);
            fixture.Window.FlowDirection = direction;
            fixture.Window.Width = 1400;
            fixture.Ribbon.SelectedTab = (RibbonTab)fixture.Window.FindName("InsertTab");
            fixture.Show();
            Assert.True(await fixture.Shell.NewAsync(RibbonKit.Writer.Services.Documents.WriterDocumentProfiles.RibbonKitWriter));
            fixture.Ribbon.SelectedTab = (RibbonTab)fixture.Window.FindName("InsertTab");
            await PumpAsync();
            var gallery = (InRibbonGallery)fixture.Window.FindName("TableGridPicker");
            Assert.True(gallery.IsEnabled);
            var items = gallery.Items.Cast<RibbonGalleryItem>().ToArray();
            Assert.Equal(25, items.Length);
            Assert.Equal(FlowDirection.LeftToRight, gallery.FlowDirection);
            string name = $"{theme}-{dark}-{density}-{direction}";
            try
            {
                VerifyGrid(items, density);
                var viewport = (ScrollViewer)gallery.Template.FindName("PART_ScrollViewer", gallery);
                var first = items[0].TranslatePoint(new Point(), viewport);
                var last = items[7].TranslatePoint(new Point(), viewport);
                Assert.Equal(first.Y, last.Y, 2);
                Assert.True(last.X + items[7].ActualWidth <= viewport.ActualWidth + .5);
                CaptureTablePicker(gallery, name + "-strip");

                gallery.IsDropDownOpen = true; await PumpAsync();
                VerifyGrid(items, density); VerifyHighlight();
                CaptureTablePicker((FrameworkElement)gallery.Template.FindName("PART_PopupHost", gallery), name + "-popup");
                gallery.IsDropDownOpen = false; await PumpAsync();

                fixture.Ribbon.QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon;
                Assert.True(fixture.Ribbon.AddToQuickAccess(gallery)); await PumpAsync();
                var qat = Assert.Single(fixture.Ribbon.QuickAccessItems.OfType<RibbonDropDownButton>(),
                    item => ReferenceEquals(Ribbon.GetQuickAccessSource(item), gallery));
                fixture.Window.Width = 620; await PumpAsync();
                qat.IsDropDownOpen = true; await PumpAsync();
                VerifyGrid(items, density); VerifyHighlight();
                CaptureTablePicker((FrameworkElement)qat.Template.FindName("PART_MenuHost", qat), name + "-qat");
                qat.IsDropDownOpen = false; await PumpAsync();
                fixture.Window.Width = 1400; await PumpAsync();

                var tab = (RibbonTab)fixture.Window.FindName("InsertTab");
                var group = new RibbonGroup { Header = "Favorites", CanResize = false };
                Ribbon.SetIsCustom(group, true);
                var custom = (RibbonDropDownButton)typeof(Ribbon).GetMethod("CreateCommandProxy",
                    BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(fixture.Ribbon,
                        new object[] { gallery, RibbonControlSize.Medium, false })!;
                group.Items.Add(custom); tab.Groups.Add(group); await PumpAsync();
                custom.IsDropDownOpen = true; await PumpAsync();
                VerifyGrid(items, density); VerifyHighlight();
                CaptureTablePicker((FrameworkElement)custom.Template.FindName("PART_MenuHost", custom), name + "-custom");
                custom.IsDropDownOpen = false; await PumpAsync();
                tab.Groups.Remove(group);

                fixture.Ribbon.LayoutMode = RibbonLayoutMode.Simplified; await PumpAsync();
                var simplified = Assert.Single(FindVisualDescendants<RibbonDropDownButton>(fixture.Ribbon),
                    item => item.IsVisible && !fixture.Ribbon.QuickAccessItems.Contains(item)
                        && ReferenceEquals(Ribbon.GetQuickAccessSource(item), gallery));
                simplified.IsDropDownOpen = true; await PumpAsync();
                VerifyGrid(items, density); VerifyHighlight();
                CaptureTablePicker((FrameworkElement)simplified.Template.FindName("PART_MenuHost", simplified), name + "-simplified");
                simplified.IsDropDownOpen = false; await PumpAsync();
                fixture.Ribbon.LayoutMode = RibbonLayoutMode.Classic; await PumpAsync();
                VerifyGrid(items, density);
                Assert.Same(items[0], gallery.Items[0]);
                Assert.Equal("Writer.Insert.Table.Grid", Ribbon.GetCommandId(gallery));
                foreach (var changedDensity in new[] { density == RibbonDensity.Touch ? RibbonDensity.Compact : RibbonDensity.Touch, density })
                {
                    fixture.Window.ApplyAppearance(new WriterAppearancePreferences { Theme = theme, DarkPalette = dark,
                        Density = changedDensity, AnimationLevel = RibbonKit.Animation.RibbonAnimationLevel.None });
                    await PumpAsync(); VerifyGrid(items, changedDensity);
                }
            }
            finally { gallery.IsDropDownOpen = false; fixture.Shell.CurrentDocument.MarkClean(); }

            void VerifyHighlight()
            {
                gallery.RaiseEvent(new RibbonGalleryPreviewEventArgs(RibbonGallery.ItemPreviewEvent, gallery, items[11]));
                foreach (var item in items.Take(24))
                {
                    var choice = (WriterTableGridChoice)item.Tag;
                    var preview = (WriterTableGridCellPreview)((Button)item.Content).Content;
                    Assert.Equal(choice.Rows <= 2 && choice.Columns <= 4, preview.IsHighlighted);
                }
            }
        }, TimeSpan.FromSeconds(45));

        static void VerifyGrid(RibbonGalleryItem[] items, RibbonDensity density)
        {
            var panel = (FrameworkElement)VisualTreeHelper.GetParent(items[0]);
            Point origin = items[0].TranslatePoint(new Point(), panel);
            double width = items[0].ActualWidth, height = items[0].ActualHeight;
            Assert.True(width > 0 && height > 0);
            if (density == RibbonDensity.Touch) Assert.True(width >= 44 && height >= 44);
            foreach (var item in items.Take(24))
            {
                var choice = (WriterTableGridChoice)item.Tag;
                var point = item.TranslatePoint(new Point(), panel);
                Assert.Equal(items[choice.Columns - 1].TranslatePoint(new Point(), panel).X, point.X, 2);
                Assert.Equal(items[(choice.Rows - 1) * 8].TranslatePoint(new Point(), panel).Y, point.Y, 2);
            }
            var footer = items[24];
            Assert.Equal(origin.X, footer.TranslatePoint(new Point(), panel).X, 2);
            Assert.Equal(items[16].TranslatePoint(new Point(), panel).Y + items[16].ActualHeight,
                footer.TranslatePoint(new Point(), panel).Y, 2);
            Assert.Equal(items[7].TranslatePoint(new Point(), panel).X + items[7].ActualWidth - origin.X, footer.ActualWidth, 2);
            var button = (Button)((StackPanel)footer.Content).Children[1];
            Assert.True(button.ActualWidth >= footer.ActualWidth - 20);
            if (density == RibbonDensity.Touch) Assert.True(button.ActualHeight >= 44);
        }
    }

    [Fact]
    public async Task TouchTablePickerChoiceInsertsItsLogicalSizeOnce()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            using var directory = new TemporaryDirectory();
            var settings = new WriterSettingsStore(new WriterSettingsPaths(directory.File("appearance.json"), directory.File("ribbon.json")));
            using var fixture = new WindowFixture(settings: settings);
            fixture.Show();
            try
            {
                Assert.True(await fixture.Shell.NewAsync(RibbonKit.Writer.Services.Documents.WriterDocumentProfiles.RibbonKitWriter));
                fixture.Ribbon.Density = RibbonDensity.Touch;
                fixture.Window.Width = 1400;
                fixture.Ribbon.SelectedTab = (RibbonTab)fixture.Window.FindName("InsertTab");
                fixture.Editor.Focus(); await PumpAsync();
                var gallery = (InRibbonGallery)fixture.Window.FindName("TableGridPicker");
                gallery.IsDropDownOpen = true; await PumpAsync();
                var choice = gallery.Items.Cast<RibbonGalleryItem>().Single(item => item.Tag is WriterTableGridChoice(2, 3));
                var invoke = (Button)choice.Content;
                invoke.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, invoke)); await PumpAsync();
                var table = Assert.Single(fixture.Editor.Document.Blocks.OfType<Table>());
                Assert.Equal(2, table.RowGroups[0].Rows.Count);
                Assert.All(table.RowGroups[0].Rows.Cast<TableRow>(), row => Assert.Equal(3, row.Cells.Count));
                Assert.False(gallery.IsDropDownOpen);
            }
            finally { fixture.Shell.CurrentDocument.MarkClean(); }
        }, TimeSpan.FromSeconds(30));
    }

    private static void CaptureTablePicker(FrameworkElement element, string name)
    {
        string? folder = Environment.GetEnvironmentVariable("RIBBONKIT_WRITER_TABLE_DIAGNOSTICS");
        if (string.IsNullOrEmpty(folder)) return;
        Directory.CreateDirectory(folder);
        // Capture the complete popup visual root, including its flow-direction
        // transforms. Rendering an inner RTL border alone can mirror its text.
        if (!name.EndsWith("-strip", StringComparison.Ordinal))
            while (VisualTreeHelper.GetParent(element) is FrameworkElement parent) element = parent;
        var dpi = VisualTreeHelper.GetDpi(element);
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(element.ActualWidth * dpi.DpiScaleX),
            (int)Math.Ceiling(element.ActualHeight * dpi.DpiScaleY), dpi.PixelsPerInchX, dpi.PixelsPerInchY, PixelFormats.Pbgra32);
        bitmap.Render(element);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(Path.Combine(folder, name + ".png")); encoder.Save(stream);
    }
}
