using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shell;
using System.Windows.Threading;
using RibbonKit.Animation;
using RibbonKit.Controls;
using RibbonKit.Showcase;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Tests;

public class TouchModeShowcaseTests
{
    [Fact]
    public void Title_bar_touch_caption_buttons_fill_the_touch_band() => Sta.Run(() =>
    {
        // Shared resources only; this also checks the default consumer window template.
        var application = Sta.UseApplication();
        ThemeManager.Apply(application, RibbonTheme.Office2024);
        var ribbon = new Ribbon { QuickAccessPosition = RibbonQuickAccessPosition.TitleBar };
        ribbon.QuickAccessItems.Add(new RibbonButton { Header = "Save", Size = RibbonControlSize.Small });
        var window = new RibbonWindow { Content = ribbon, Width = 600, Height = 300,
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
        try
        {
            window.Show(); Layout(window);
            AssertCaptionHeight(34);
            ribbon.Density = RibbonDensity.Touch; Layout(window);
            AssertCaptionHeight(46);
            window.IsTitleBarContentVisible = false; Layout(window);
            AssertCaptionHeight(46);
            Save(window, "touch-caption-buttons");
            window.IsTitleBarContentVisible = true;
            ribbon.Density = RibbonDensity.Compact; Layout(window);
            AssertCaptionHeight(34);
            ribbon.Density = RibbonDensity.Touch;
            ribbon.QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon; Layout(window);
            AssertCaptionHeight(34);
        }
        finally { window.Close(); Sta.ResetApplication(); }

        void AssertCaptionHeight(double expected)
        {
            foreach (string name in new[] { "PART_MinimizeButton", "PART_MaximizeButton", "PART_RestoreButton", "PART_CloseButton" })
            {
                var button = Part<Button>(window, name);
                Assert.Equal(expected, button.Height);
                if (!button.IsVisible) continue;
                Assert.InRange(button.ActualHeight, expected - 1, expected + 1);
                Assert.Equal(46, button.ActualWidth);
                Assert.Equal(Part<Grid>(window, "TitleBarBand").ActualHeight, button.ActualHeight);
                Assert.Equal(button.ActualHeight, Part<Border>(button, "Chrome").ActualHeight);
            }
        }
    });

    [Theory]
    [InlineData(RibbonTheme.Office2007)]
    [InlineData(RibbonTheme.Office2010)]
    [InlineData(RibbonTheme.Office2013)]
    [InlineData(RibbonTheme.Office2019)]
    [InlineData(RibbonTheme.Office2024)]
    [InlineData(RibbonTheme.CrystalLight)]
    public void Touch_commands_are_consistent_and_the_body_fits_its_content(RibbonTheme theme) => Sta.Run(() =>
    {
        var application = Sta.UseApplication(showcaseResources: true);
        var animation = RibbonAnimation.GlobalLevel;
        RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
        MainWindow? window = null;
        try
        {
            ThemeManager.Apply(application, theme);
            window = new MainWindow { Width = 1540, Height = 900, Left = -10000, Top = -10000,
                ShowActivated = false, ShowInTaskbar = false };
            // Realize the factory UI without reading/writing the user's saved layouts.
            var type = typeof(MainWindow);
            window.Loaded -= (RoutedEventHandler)type.GetMethod("OnWindowLoaded",
                BindingFlags.Instance | BindingFlags.NonPublic)!.CreateDelegate(typeof(RoutedEventHandler), window);
            type.GetField("_restoringAppearance", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(window, true);
            window.Show();
            window.ThemeGallery.SelectedItem = window.ThemeGallery.Items.OfType<RibbonGalleryItem>()
                .Single(item => Equals(item.Tag, theme.ToString()));
            window.MainRibbon.SelectedTab = window.MainRibbon.Tabs.Single(tab => Equals(tab.Header, "View"));
            window.MainRibbon.Density = RibbonDensity.Compact; Layout(window);
            var messages = new[] { window.ProtectedViewMessage, window.SecurityNoticeMessage, window.UnavailableActionMessage };
            foreach (var message in messages) message.IsOpen = true;
            Layout(window);
            double messageFont = Part<TextBlock>(messages[0], "MessageText").FontSize;
            double actionFont = Part<Button>(messages[0], "PART_ActionButton").FontSize;
            Assert.True(Part<Button>(messages[0], "PART_ActionButton").ActualHeight < 44);
            AssertGallerySpacing(window);
            var tabs = Part<RibbonTabControl>(window.MainRibbon, "TabControlHost");
            var body = Part<Border>(tabs, "ContentHost");
            double compactIcon = Part<Image>(window.TouchModeToggle, "LargeImage").ActualWidth;
            window.TouchModeToggle.IsChecked = true; Layout(window);
            Assert.Equal(RibbonDensity.Touch, window.MainRibbon.Density);
            foreach (var message in messages)
            {
                Assert.Equal(RibbonDensity.Touch, Ribbon.GetDensity(message));
                Assert.True(message.ActualHeight >= 52);
                var action = Part<Button>(message, "PART_ActionButton");
                var dismiss = Part<Button>(message, "PART_CloseButton");
                Assert.True(action.ActualHeight >= 44 && action.ActualWidth >= 44);
                Assert.True(dismiss.ActualHeight >= 44 && dismiss.ActualWidth >= 44);
                Assert.Equal(messageFont, Part<TextBlock>(message, "MessageText").FontSize);
                Assert.Equal(actionFont, action.FontSize);
            }
            Assert.False(Part<Button>(window.UnavailableActionMessage, "PART_ActionButton").IsEnabled);
            Assert.InRange(body.ActualHeight, window.TouchModeToggle.ActualHeight,
                window.TouchModeToggle.ActualHeight + 48);
            Assert.True(Part<Image>(window.TouchModeToggle, "LargeImage").ActualWidth > compactIcon);
            foreach (bool colored in new[] { false, true })
            {
                window.AccentedTitleBarToggle.IsChecked = colored; Layout(window);
                foreach (var command in new[] { window.DarkModeToggle, window.AccentedTitleBarToggle,
                    window.TouchModeToggle, window.GlassTreatmentToggle, window.MicaToggle, window.AcrylicToggle })
                    Assert.InRange(command.ActualHeight, window.TouchModeToggle.ActualHeight - 1,
                        window.TouchModeToggle.ActualHeight + 1);
                if (window.MainRibbon.ApplicationButtonShape == RibbonApplicationButtonShape.Tab)
                {
                    var file = Part<ToggleButton>(tabs, "PART_ApplicationButton");
                    var caption = Part<ContentPresenter>(file, "ApplicationCaption");
                    var header = window.MainRibbon.SelectedTab!;
                    var headerChrome = Part<Border>(header, "HeaderChrome");
                    var headerText = Part<ContentPresenter>(header, "HeaderText");
                    Assert.InRange(file.ActualHeight, headerChrome.ActualHeight - 1, headerChrome.ActualHeight + 1);
                    Assert.True(file.ActualWidth >= 64);
                    double expectedInset = theme is RibbonTheme.Office2013 or RibbonTheme.Office2019 ? 0
                        : theme is RibbonTheme.Office2024 or RibbonTheme.CrystalLight ? 8 : 2;
                    Assert.InRange(file.TranslatePoint(new Point(), tabs).X, expectedInset - 1, expectedInset + 1);
                    double labelCenter = caption.TranslatePoint(new Point(0, caption.ActualHeight / 2), tabs).Y;
                    double tabCenter = headerText.TranslatePoint(new Point(0, headerText.ActualHeight / 2), tabs).Y;
                    Assert.InRange(labelCenter, tabCenter - 1, tabCenter + 1);
                }
            }
            var viewport = Part<ScrollViewer>(window.ThemeGallery, "PART_ScrollViewer");
            AssertGallerySpacing(window);
            if (theme == RibbonTheme.Office2010)
                Assert.IsType<SolidColorBrush>(Part<Border>(window.MainRibbon, "QatBelowHost").Background);
            var selectedTile = Assert.IsType<RibbonGalleryItem>(window.ThemeGallery.SelectedItem);
            Assert.True(selectedTile.ActualWidth <= viewport.ActualWidth + 1,
                $"{theme}: theme tile {selectedTile.ActualWidth}, viewport {viewport.ActualWidth}");
            Assert.InRange(selectedTile.ActualWidth, viewport.ViewportWidth - 1, viewport.ViewportWidth + 1);
            Assert.InRange(selectedTile.ActualHeight, viewport.ViewportHeight - 1, viewport.ViewportHeight + 1);
            var selectedBounds = selectedTile.TransformToAncestor(viewport).TransformBounds(new Rect(selectedTile.RenderSize));
            Assert.InRange(selectedBounds.Top, -1, 1);
            Assert.InRange(selectedBounds.Bottom, viewport.ViewportHeight - 1, viewport.ViewportHeight + 1);
            Assert.False(Part<RepeatButton>(window.ThemeGallery, "PART_LineUp").IsVisible);
            Assert.False(Part<RepeatButton>(window.ThemeGallery, "PART_LineDown").IsVisible);
            var opener = Part<ToggleButton>(window.ThemeGallery, "PART_ExpandToggle");
            Assert.True(opener.ActualWidth >= 44 && opener.ActualHeight >= 44);
            Save(window.MainRibbon, $"{theme}-showcase-view");
            window.MainRibbon.SelectedTab = window.MainRibbon.Tabs.Single(tab => Equals(tab.Header, "Home"));
            Layout(window);
            Assert.InRange(window.FontCompactSeparator.ActualHeight,
                window.SuperscriptButton.ActualHeight - 1, window.SuperscriptButton.ActualHeight + 1);
            var smallStack = Assert.IsType<StackPanel>(window.HomeTab.Groups[0].Items[1]);
            var paste = window.DemoSplitTarget;
            // Three individually rounded rows can differ by one physical pixel
            // each from a single rounded large button at fractional desktop DPI.
            Assert.InRange(paste.ActualHeight + paste.Margin.Top + paste.Margin.Bottom,
                smallStack.ActualHeight - 2, smallStack.ActualHeight + 2);
            Save(window.MainRibbon, $"{theme}-showcase-home");
            window.MainRibbon.EnterModal(window.PrintPreviewTab); Layout(window);
            var close = Part<Button>(tabs, "PART_ModalClose");
            var closeLabel = Part<ContentPresenter>(close, "Label");
            Assert.True(close.IsVisible && close.ActualWidth > 44);
            var labelBounds = closeLabel.TransformToAncestor(close).TransformBounds(new Rect(closeLabel.RenderSize));
            Assert.InRange(labelBounds.Left, 0, close.ActualWidth);
            Assert.InRange(labelBounds.Right, 0, close.ActualWidth);
            Assert.InRange(close.TranslatePoint(new Point(close.ActualWidth, 0), window).X, 0, window.ActualWidth);
            Save(window.MainRibbon, $"{theme}-showcase-modal");
            window.MainRibbon.ExitModal(); Layout(window);
            window.MainRibbon.QuickAccessPosition = RibbonQuickAccessPosition.TitleBar; Layout(window);
            double titleHeight = Part<Grid>(window, "TitleBarBand").ActualHeight;
            Assert.True(titleHeight >= 46);
            Assert.Equal(46, WindowChrome.GetWindowChrome(window).CaptionHeight);
            Save(window, $"{theme}-showcase-touch-title-messages");
            window.ApplicationMenuToggle.IsChecked = false;
            var fileSurface = Assert.IsType<Backstage>(window.MainRibbon.Backstage);
            fileSurface.Design = RibbonBackstageDesign.Modern;
            window.MainRibbon.IsBackstageOpen = true; Layout(window);
            Assert.False(window.IsTitleBarContentVisible);
            Assert.Equal(titleHeight, Part<Grid>(window, "TitleBarBand").ActualHeight);
            Assert.Equal(46, WindowChrome.GetWindowChrome(window).CaptionHeight);
            Save(window, $"{theme}-showcase-touch-title-backstage");
            window.MainRibbon.IsBackstageOpen = false; Layout(window);
            Assert.Equal(titleHeight, Part<Grid>(window, "TitleBarBand").ActualHeight);
            if (theme == RibbonTheme.Office2007)
            {
                window.ApplicationMenuToggle.IsChecked = false;
                var backstage = Assert.IsType<Backstage>(window.MainRibbon.Backstage);
                backstage.Design = RibbonBackstageDesign.Classic2007;
                window.MainRibbon.IsBackstageOpen = true; Layout(window);
                var nav = Part<Border>(backstage, "NavColumn");
                var page = Part<Border>(backstage, "ContentArea");
                Assert.Same(page.Background, nav.Background);
                Assert.Equal(page.TranslatePoint(new Point(), window).Y,
                    nav.TranslatePoint(new Point(), window).Y);
                Save(window, "Office2007-showcase-classic-backstage");
                window.MainRibbon.IsBackstageOpen = false; Layout(window);
            }
        }
        finally
        {
            window?.Close();
            RibbonAnimation.GlobalLevel = animation;
            Sta.ResetApplication();
        }
    });

    private static T Part<T>(Control owner, string name) where T : DependencyObject =>
        Assert.IsAssignableFrom<T>(owner.Template.FindName(name, owner));

    private static void AssertGallerySpacing(MainWindow window)
    {
        var command = window.AccentedTitleBarToggle;
        var gallery = window.AccentGallery;
        double gap = gallery.TranslatePoint(new Point(), window).X
            - command.TranslatePoint(new Point(command.ActualWidth, 0), window).X;
        Assert.True(gap >= 4, $"{window.MainRibbon.Density}: button/gallery spacing {gap}");
    }

    private static void Layout(Window window)
    {
        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        window.UpdateLayout();
    }

    private static void Save(FrameworkElement element, string name)
    {
        string? path = Environment.GetEnvironmentVariable("RIBBONKIT_TOUCH_DIAGNOSTICS");
        if (string.IsNullOrEmpty(path)) return;
        Directory.CreateDirectory(path);
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(element.ActualWidth),
            (int)Math.Ceiling(element.ActualHeight), 96, 96, PixelFormats.Pbgra32);
        var backing = new DrawingVisual();
        using (var context = backing.RenderOpen())
            context.DrawRectangle((Brush)element.FindResource("RibbonKit.Brushes.Window.Background"), null,
                new Rect(0, 0, element.ActualWidth, element.ActualHeight));
        bitmap.Render(backing); bitmap.Render(element);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var file = File.Create(Path.Combine(path, name + ".png")); encoder.Save(file);
    }
}
