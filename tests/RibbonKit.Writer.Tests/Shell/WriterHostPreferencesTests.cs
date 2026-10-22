using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using RibbonKit.Animation;
using RibbonKit.Controls;
using RibbonKit.Interop;
using RibbonKit.Theming;
using RibbonKit.Writer.Appearance;
using RibbonKit.Writer.Editing;
using RibbonKit.Writer.Models;
using RibbonKit.Writer.Page;
using RibbonKit.Writer.Preview;
using RibbonKit.Writer.Printing;
using RibbonKit.Writer.Tests.Document;
using Xunit;

namespace RibbonKit.Writer.Tests.Shell;

public sealed partial class MainWindowIntegrationTests
{
    [Theory]
    [InlineData(RibbonTheme.Office2024Flat, false)]
    [InlineData(RibbonTheme.CrystalLight, true)]
    public async Task WriterInputModeDropdownMatchesShowcaseAndWorksFromQuickAccess(RibbonTheme theme, bool dark)
    {
        await StaTestHelper.RunAsync(async () =>
        {
            using var directory = new TemporaryDirectory();
            var store = new WriterSettingsStore(new WriterSettingsPaths(directory.File("appearance.json"), directory.File("ribbon.json")));
            store.SaveAppearance(new WriterAppearancePreferences { Theme = theme, DarkPalette = dark,
                Density = RibbonDensity.Touch, AnimationLevel = RibbonAnimationLevel.None });
            using var fixture = new WindowFixture(settings: store);
            fixture.Show(); fixture.Window.Opacity = 1;
            fixture.Ribbon.SelectedTab = (RibbonTab)fixture.Window.FindName("ViewTab");
            await PumpAsync();
            var selector = (RibbonDropDownButton)fixture.Window.FindName("InputModeSelector");
            var mouse = (RibbonMenuItem)fixture.Window.FindName("MouseModeChoice");
            var touch = (RibbonMenuItem)fixture.Window.FindName("TouchModeChoice");
            Assert.Equal("Touch/Mouse Mode", selector.Header);
            Assert.Equal("Optimize spacing between commands", selector.DropDownHeader);
            Assert.Equal("Writer.View.InputMode", Ribbon.GetCommandId(selector));
            Assert.Equal(RibbonControlSize.Large, selector.Size);
            Assert.Equal(2, selector.Items.Count);
            Assert.NotNull(mouse.LargeIcon); Assert.NotNull(touch.LargeIcon);
            Assert.Contains("Optimized for use with mouse.", mouse.Description);
            Assert.Contains("Optimized for use with touch.", touch.Description);
            selector.IsDropDownOpen = true; await PumpAsync();
            Assert.Same(selector.FindResource("RibbonKit.Brushes.Control.CheckedBackground"), touch.Background);
            SaveMenu(selector, "Touch");
            SelectWriterDensity(fixture, RibbonDensity.Compact); await PumpAsync();
            Assert.False(selector.IsDropDownOpen);
            Assert.Equal(RibbonDensity.Compact, fixture.Ribbon.Density);
            Assert.Equal(RibbonDensity.Compact, store.LoadAppearance().Density);

            fixture.Ribbon.AddToQuickAccess(selector); await PumpAsync();
            var proxy = fixture.Ribbon.QuickAccessItems.OfType<RibbonDropDownButton>().Single(item =>
                ReferenceEquals(Ribbon.GetQuickAccessSource(item), selector));
            proxy.IsDropDownOpen = true; await PumpAsync();
            Assert.Equal(selector.DropDownHeader, ((TextBlock)proxy.Template.FindName("DropDownHeaderText", proxy)).Text);
            Assert.Same(proxy.FindResource("RibbonKit.Brushes.Control.CheckedBackground"), mouse.Background);
            SaveMenu(proxy, "Mouse-QAT");
            SelectWriterDensity(fixture, RibbonDensity.Touch); await PumpAsync();
            Assert.False(proxy.IsDropDownOpen);
            Assert.Equal(RibbonDensity.Touch, store.LoadAppearance().Density);
            Assert.Equal(2, selector.Items.Count); Assert.Empty(proxy.Items);
            fixture.Ribbon.CanUserChangeDensity = false;
            await PumpAsync();
            Assert.False(selector.IsEnabled);
            SelectWriterDensity(fixture, RibbonDensity.Compact);
            Assert.Equal(RibbonDensity.Touch, fixture.Ribbon.Density);

            void SaveMenu(RibbonDropDownButton owner, string state)
            {
                var menu = (Border)owner.Template.FindName("PART_MenuHost", owner);
                Assert.True(menu.ActualWidth > 0 && menu.ActualHeight > 0);
                var bitmap = new RenderTargetBitmap((int)Math.Ceiling(menu.ActualWidth),
                    (int)Math.Ceiling(menu.ActualHeight), 96, 96, PixelFormats.Pbgra32);
                bitmap.Render(menu);
                var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
                string folder = Path.Combine(AppContext.BaseDirectory, "writer-theme-diagnostics");
                Directory.CreateDirectory(folder);
                using var file = File.Create(Path.Combine(folder, $"WriterInputMode-{theme}-{state}.png"));
                encoder.Save(file);
            }
        }, TimeSpan.FromSeconds(45));
    }

    [Fact]
    public async Task WriterHostPreferencesReachEveryDetachedDialogAndNestedColorPicker()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            using var fixture = new WindowFixture();
            fixture.Show();
            using var snapshot = new WriterPreviewCloneService().CreateSnapshot(fixture.Editor.Document, DocumentPageSettings.A4());
            var font = new WriterFontDialogResult(new FontFamily("Segoe UI"), 11, FontStyles.Normal,
                FontWeights.Normal, false, WriterStrikethroughStyle.None, WriterBaselineEffect.Normal, Colors.Black);
            Window[] dialogs = [new WriterFontDialog(font, new WriterFontCatalog(() => [new FontFamily("Segoe UI")])),
                new WriterColorDialog(Colors.Purple), new WriterParagraphDialog(), new WriterTableSizeDialog(),
                new WriterPictureInsertDialog(), new WriterHyperlinkDialog(), new WriterDateTimeDialog(),
                new WriterCustomMarginsDialog(DocumentPageSettings.A4()),
                new WriterFindReplaceDialog(new WriterFindReplaceService(fixture.Editor)),
                new WriterPrintSetupDialog(snapshot, [new WriterPrinterChoice(null, "Test printer")], "Test printer")];
            try
            {
                fixture.Window.ApplyAppearance(new WriterAppearancePreferences { Theme = RibbonTheme.Office2024Flat,
                    Density = RibbonDensity.Touch, AnimationLevel = RibbonAnimationLevel.None });
                foreach (var dialog in dialogs)
                {
                    dialog.Owner = dialog is WriterColorDialog ? dialogs[0] : fixture.Window;
                    dialog.WindowStartupLocation = WindowStartupLocation.Manual;
                    dialog.Left = -10000; dialog.Top = -10000;
                    dialog.ShowActivated = false; dialog.ShowInTaskbar = false;
                    dialog.Show();
                }
                await PumpAsync();
                foreach (var density in new[] { RibbonDensity.Touch, RibbonDensity.Compact, RibbonDensity.Touch })
                {
                    fixture.Window.ApplyAppearance(new WriterAppearancePreferences { Theme = RibbonTheme.Office2024Flat,
                        Density = density, DarkPalette = density == RibbonDensity.Touch, AnimationLevel = RibbonAnimationLevel.None });
                    foreach (var dialog in dialogs)
                    {
                        dialog.UpdateLayout();
                        Assert.Equal(density, Ribbon.GetDensity(dialog));
                        Assert.Equal(density, Ribbon.GetDensity((DependencyObject)dialog.Content));
                        Assert.Same(dialog.FindResource("RibbonKit.Brushes.Control.SurfaceBackground"), dialog.Background);
                        foreach (var input in FindVisualDescendants<RibbonTextBox>(dialog))
                            Assert.Equal(density, Ribbon.GetDensity(input));
                        var root = (FrameworkElement)dialog.Content;
                        Assert.True(root.ActualWidth > 0 && root.ActualHeight > 0);
                        Assert.True(root.ActualHeight + root.Margin.Top + root.Margin.Bottom + 1 >= root.DesiredSize.Height,
                            $"{dialog.GetType().Name}/{density}: content is clipped");
                    }
                }
            }
            finally { foreach (var dialog in dialogs.AsEnumerable().Reverse()) dialog.Close(); }
        }, TimeSpan.FromSeconds(60));
    }

    [Fact]
    public async Task WriterHostPreferencesPreviewApplyCancelOkAndRestartRetainAppliedCheckpoint()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            using var directory = new TemporaryDirectory();
            var store = new WriterSettingsStore(new WriterSettingsPaths(directory.File("appearance.json"), directory.File("ribbon.json")));
            store.SaveAppearance(new WriterAppearancePreferences { Density = RibbonDensity.Touch });
            using (var fixture = new WindowFixture(settings: store))
            {
                // Restoration precedes Show and cannot play a startup transition.
                Assert.Equal(RibbonDensity.Touch, fixture.Ribbon.Density);
                int started = 0;
                fixture.Ribbon.LayoutTransitionStarting += (_, _) => started++;
                fixture.Show();
                await PumpAsync();
                Assert.Equal(0, started);
                fixture.Editor.AppendText("Writer host preferences retain document text and selection.");
                SelectWriterDensity(fixture, RibbonDensity.Compact);
                Assert.Equal(RibbonDensity.Compact, store.LoadAppearance().Density);
                SelectWriterDensity(fixture, RibbonDensity.Touch);

                fixture.Editor.Selection.Select(fixture.Editor.Document.ContentStart, fixture.Editor.Document.ContentEnd);
                string text = fixture.Editor.Selection.Text;
                var document = fixture.Editor.Document;
                var settings = fixture.Shell.CurrentDocument.PageSettings;
                bool dirty = fixture.Shell.CurrentDocument.IsDirty;
                string ribbon = RibbonCustomizationSerializer.Serialize(fixture.Ribbon);
                RunHostSettings(fixture, dialog =>
                {
                    var page = (WriterAppearancePage)dialog.Pages[0].Content;
                    var selected = dialog.SelectedPage;
                    SelectHostChoice((ComboBox)page.FindName("ThemeCombo"), RibbonTheme.Office2024Flat);
                    SelectHostChoice((ComboBox)page.FindName("PaletteCombo"), "True");
                    SelectHostChoice((ComboBox)page.FindName("QatIconCombo"), RibbonQuickAccessIconAppearance.ColorWithGlow);
                    ((CheckBox)page.FindName("GlassSurfacesCheck")).IsChecked = true;
                    Assert.Same(selected, dialog.SelectedPage);
                    Assert.Equal(RibbonDensity.Touch, Ribbon.GetDensity(dialog));
                    Assert.Equal(RibbonDensity.Touch, page.Preferences.Density);
                    Assert.Equal(RibbonQuickAccessIconAppearance.ColorWithGlow, fixture.Ribbon.QuickAccessIconAppearance);
                    ClickHostButton((ButtonBase)page.FindName("AppearanceApplyButton"));
                    Assert.Equal(page.Preferences, store.LoadAppearance());
                    SelectHostChoice((ComboBox)page.FindName("ThemeCombo"), RibbonTheme.Office2007);
                    SelectHostChoice((ComboBox)page.FindName("QatIconCombo"), RibbonQuickAccessIconAppearance.AdaptiveColor);
                    dialog.Close();
                });
                Assert.Equal(RibbonTheme.Office2024Flat, store.LoadAppearance().Theme);
                Assert.Equal(RibbonQuickAccessIconAppearance.ColorWithGlow, fixture.Ribbon.QuickAccessIconAppearance);
                Assert.Same(document, fixture.Editor.Document);
                Assert.Same(settings, fixture.Shell.CurrentDocument.PageSettings);
                Assert.Equal(dirty, fixture.Shell.CurrentDocument.IsDirty);
                Assert.Equal(text, fixture.Editor.Selection.Text);
                Assert.Equal(ribbon, RibbonCustomizationSerializer.Serialize(fixture.Ribbon));
                RunHostSettings(fixture, dialog =>
                {
                    var page = (WriterAppearancePage)dialog.Pages[0].Content;
                    SelectHostChoice((ComboBox)page.FindName("QatIconCombo"), RibbonQuickAccessIconAppearance.AdaptiveColor);
                    dialog.ApplyTemplate();
                    ClickHostButton((ButtonBase)dialog.Template.FindName("PART_OkButton", dialog));
                });
            }
            using var restarted = new WindowFixture(settings: store);
            Assert.Equal(RibbonDensity.Touch, restarted.Ribbon.Density);
            Assert.Equal(RibbonQuickAccessIconAppearance.AdaptiveColor, restarted.Ribbon.QuickAccessIconAppearance);
            restarted.Show();
            await PumpAsync();
            Assert.Equal(RibbonTheme.Office2024Flat, store.LoadAppearance().Theme);
            Assert.True(store.LoadAppearance().DarkPalette);
            Assert.True(store.LoadAppearance().GlassSurfaces);
        }, TimeSpan.FromSeconds(60));
    }

    [Fact]
    public async Task WriterHostPreferencesRenderAcrossDensityQatLayoutAndThemeWithoutDocumentMutation()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            using var fixture = new WindowFixture();
            fixture.Show();
            fixture.Window.Opacity = 1;
            await PumpAsync();
            var window = fixture.Window;
            var surface = (WriterEditorSurface)window.FindName("EditorSurface");
            fixture.Editor.AppendText("Flat, Touch and QAT appearance preserve this text.");
            ClickHostButton((RibbonButton)window.FindName("ZoomInButton"));
            fixture.Editor.Selection.Select(fixture.Editor.Document.ContentStart, fixture.Editor.Document.ContentEnd);
            string selected = fixture.Editor.Selection.Text;
            var document = fixture.Editor.Document;
            var pageSettings = fixture.Shell.CurrentDocument.PageSettings;
            bool dirty = fixture.Shell.CurrentDocument.IsDirty;
            double zoom = surface.ZoomPercent;
            var preferences = new WriterAppearancePreferences { Theme = RibbonTheme.Office2024Flat,
                Accent = "#FF7030A0", AccentedTitleBar = true, GlassSurfaces = true,
                AnimationLevel = RibbonAnimationLevel.None };
            fixture.Ribbon.SelectedTab = (RibbonTab)window.FindName("ViewTab");
            string output = Path.Combine(AppContext.BaseDirectory, "writer-theme-diagnostics");
            Directory.CreateDirectory(output);
            foreach (bool dark in new[] { false, true })
            foreach (RibbonDensity density in Enum.GetValues<RibbonDensity>())
            foreach (RibbonQuickAccessIconAppearance icons in Enum.GetValues<RibbonQuickAccessIconAppearance>())
            {
                window.ApplyAppearance(preferences with { DarkPalette = dark, Density = density, QuickAccessIconAppearance = icons });
                fixture.Ribbon.LayoutMode = density == RibbonDensity.Touch ? RibbonLayoutMode.Simplified : RibbonLayoutMode.Classic;
                fixture.Ribbon.QuickAccessPosition = icons switch
                {
                    RibbonQuickAccessIconAppearance.WhiteOverlay => RibbonQuickAccessPosition.TitleBar,
                    RibbonQuickAccessIconAppearance.ColorWithGlow => RibbonQuickAccessPosition.TabRow,
                    _ => RibbonQuickAccessPosition.BelowRibbon,
                };
                fixture.Ribbon.SelectedTab = (RibbonTab)window.FindName("ViewTab");
                await PumpAsync();
                Assert.Equal(density, fixture.Ribbon.Density);
                Assert.Equal(density, Ribbon.GetDensity((DependencyObject)window.FindName("InputModeSelector")));
                Assert.Equal(icons, fixture.Ribbon.QuickAccessIconAppearance);
                var bitmap = new RenderTargetBitmap((int)window.ActualWidth, (int)window.ActualHeight, 96, 96, PixelFormats.Pbgra32);
                bitmap.Render(window);
                var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
                using (var file = File.Create(Path.Combine(output, $"Writer-Flat-{dark}-{density}-{icons}.png"))) encoder.Save(file);
                Assert.Same(document, fixture.Editor.Document);
                Assert.Same(pageSettings, fixture.Shell.CurrentDocument.PageSettings);
                Assert.Equal(dirty, fixture.Shell.CurrentDocument.IsDirty);
                Assert.Equal(selected, fixture.Editor.Selection.Text);
                Assert.Equal(zoom, surface.ZoomPercent);
            }
            // Flat uses the Office workspace and ruler policy, and leaves no Crystal edge/margin behind.
            foreach (var theme in new[] { RibbonTheme.CrystalLight, RibbonTheme.Office2024Flat, RibbonTheme.Office2024 })
            {
                window.ApplyAppearance(preferences with { Theme = theme });
                await PumpAsync();
                var ruler = (WriterRuler)window.FindName("HorizontalRuler");
                Assert.Equal(theme == RibbonTheme.CrystalLight, ruler.UseCrystalGlassEdge);
                Assert.Equal(new Thickness(), ((FrameworkElement)window.FindName("DocumentPresentationHost")).Margin);
                Assert.Equal(theme == RibbonTheme.CrystalLight,
                    ReferenceEquals(Brushes.Transparent, ((Panel)window.FindName("EditorSurface")).Background));
            }
            // Supply the same colored-band token used by ThemeManager's application
            // caption policy, while retaining this fixture's isolated resource scope.
            fixture.Ribbon.Resources["RibbonKit.Metrics.QatTitleBarColored"] = true;
            fixture.Ribbon.QuickAccessPosition = RibbonQuickAccessPosition.TitleBar;
            foreach (var icons in Enum.GetValues<RibbonQuickAccessIconAppearance>())
            {
                window.ApplyAppearance(preferences with { QuickAccessIconAppearance = icons });
                await PumpAsync();
                var proxy = Assert.IsType<RibbonButton>(fixture.Ribbon.QuickAccessItems[0]);
                var image = (Image)proxy.Template.FindName("SmallImage", proxy);
                var glow = (Image)proxy.Template.FindName("SmallImageGlow", proxy);
                var tint = (FrameworkElement)proxy.Template.FindName("SmallImageTint", proxy);
                Assert.Equal(icons == RibbonQuickAccessIconAppearance.WhiteOverlay ? Visibility.Visible : Visibility.Collapsed, tint.Visibility);
                Assert.Equal(icons == RibbonQuickAccessIconAppearance.ColorWithGlow ? Visibility.Visible : Visibility.Collapsed, glow.Visibility);
                Assert.Null(image.Effect);
                if (icons == RibbonQuickAccessIconAppearance.AdaptiveColor) Assert.NotSame(proxy.Icon, image.Source);
            }
        }, TimeSpan.FromSeconds(60));
    }

    private static void RunHostSettings(WindowFixture fixture, Action<RibbonOptionsDialog> action)
    {
        Exception? failure = null;
        fixture.Window.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() =>
        {
            var dialog = fixture.Window.OwnedWindows.OfType<RibbonOptionsDialog>().Single();
            try { action(dialog); }
            catch (Exception ex) { failure = ex; dialog.Close(); }
        }));
        fixture.Window.OpenSettingsDialog(MainWindow.WriterSettingsPage.Appearance);
        if (failure is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
    }

    private static void SelectHostChoice(ComboBox combo, object value) => combo.SelectedItem = combo.Items.Cast<ComboBoxItem>()
        .Single(item => Equals(item.Tag, value));

    private static void SelectWriterDensity(WindowFixture fixture, RibbonDensity density) =>
        ClickHostButton((RibbonMenuItem)fixture.Window.FindName(density == RibbonDensity.Touch ? "TouchModeChoice" : "MouseModeChoice"));

    private static void ClickHostButton(ButtonBase button) => button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
}
