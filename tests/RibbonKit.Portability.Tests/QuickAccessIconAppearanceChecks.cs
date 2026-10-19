using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using RibbonKit.Animation;
using RibbonKit.Controls;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Portability.Tests;

// RibbonKit-only QAT proxies, including detached title-bar hosting and live choices.
internal static class QuickAccessIconAppearanceChecks
{
    internal static void Verify(Application application)
    {
        var motion = RibbonAnimation.GlobalLevel;
        RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
        var drawing = new DrawingGroup();
        drawing.Children.Add(new GeometryDrawing(Brushes.RoyalBlue, null, new RectangleGeometry(new Rect(0, 0, 16, 16), 2, 2)));
        drawing.Children.Add(new GeometryDrawing(Brushes.White, null, new RectangleGeometry(new Rect(3, 2, 10, 5))));
        drawing.Children.Add(new GeometryDrawing(Brushes.OrangeRed, null, new RectangleGeometry(new Rect(3, 10, 6, 4))));
        var icon = new DrawingImage(drawing); icon.Freeze();
        var button = new RibbonButton { Header = "Save", Icon = icon };
        var toggle = new RibbonToggleButton { Header = "Preview", Icon = icon, IsChecked = true };
        var dropdown = new RibbonDropDownButton { Header = "More", Icon = icon };
        dropdown.Items.Add(new RibbonMenuItem { Header = "Open" });
        var split = new RibbonSplitButton { Header = "Paste", Icon = icon };
        split.Items.Add(new RibbonMenuItem { Header = "Special" });
        var group = new RibbonGroup { Header = "Commands" };
        foreach (var command in new Control[] { button, toggle, dropdown, split }) group.Items.Add(command);
        var tab = new RibbonTab { Header = "Home" }; tab.Groups.Add(group);
        var ribbon = new Ribbon { QuickAccessMaxWidth = 400 };
        ribbon.Tabs.Add(tab); ribbon.SelectedTab = tab;
        foreach (var command in new Control[] { button, toggle, dropdown, split }) ribbon.AddToQuickAccess(command);
        var window = new RibbonWindow { Content = ribbon, Width = 800, Height = 280,
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false, UseLayoutRounding = true };
        try
        {
            Assert.Equal(RibbonQuickAccessIconAppearance.WhiteOverlay, ribbon.QuickAccessIconAppearance);
            Assert.Throws<ArgumentException>(() => ribbon.QuickAccessIconAppearance = (RibbonQuickAccessIconAppearance)99);
            window.Show();
            foreach (var theme in Enum.GetValues<RibbonTheme>())
            foreach (bool dark in new[] { false, true })
            foreach (double scale in new[] { 1d, 1.25, 1.5, 2d })
            foreach (var density in new[] { RibbonDensity.Compact, RibbonDensity.Touch })
            {
                ThemeManager.Apply(application, theme); ThemeManager.SetDarkMode(application, dark);
                ThemeManager.SetAccentedTitleBar(application, true);
                VisualTreeHelper.SetRootDpi(window, new DpiScale(scale, scale));
                window.FlowDirection = dark ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
                ribbon.Density = density;
                foreach (var position in Enum.GetValues<RibbonQuickAccessPosition>())
                {
                    ribbon.QuickAccessPosition = position; Layout();
                    var sizes = ribbon.QuickAccessItems.Cast<FrameworkElement>().Select(item => item.RenderSize).ToArray();
                    foreach (var appearance in new[] { RibbonQuickAccessIconAppearance.ColorWithGlow,
                        RibbonQuickAccessIconAppearance.WhiteOverlay, RibbonQuickAccessIconAppearance.ColorWithGlow })
                    {
                        ribbon.QuickAccessIconAppearance = appearance; Layout();
                        int index = 0;
                        foreach (Control proxy in ribbon.QuickAccessItems)
                        {
                            Assert.Equal(sizes[index++], proxy.RenderSize);
                            bool colored = Ribbon.GetQatOnColoredSurface(proxy);
                            if (position == RibbonQuickAccessPosition.BelowRibbon) Assert.False(colored);
                            bool glow = colored && appearance == RibbonQuickAccessIconAppearance.ColorWithGlow;
                            var image = Part<Image>(proxy, "SmallImage");
                            var halo = Part<Image>(proxy, "SmallImageGlow");
                            var tint = Part<Rectangle>(proxy, "SmallImageTint");
                            Assert.Same(icon, image.Source); Assert.Null(image.Effect);
                            Assert.Same(icon, halo.Source);
                            Assert.Equal(colored && !glow ? Visibility.Collapsed : Visibility.Visible, image.Visibility);
                            Assert.Equal(colored && !glow ? Visibility.Visible : Visibility.Collapsed, tint.Visibility);
                            Assert.Equal(glow ? Visibility.Visible : Visibility.Collapsed, halo.Visibility);
                            var effect = Assert.IsType<DropShadowEffect>(halo.Effect);
                            Assert.Equal(Colors.White, effect.Color); Assert.Equal(0, effect.ShadowDepth);
                            if (glow) Assert.Equal(image.RenderSize, halo.RenderSize);
                        }
                        if (position == RibbonQuickAccessPosition.TitleBar && theme is RibbonTheme.Office2007 or RibbonTheme.Office2024)
                            Capture(window, $"{theme}-{dark}-{density}-{appearance}-{scale * 100:0}", scale);
                    }
                }
            }
            ribbon.QuickAccessPosition = RibbonQuickAccessPosition.TitleBar;
            ribbon.QuickAccessIconAppearance = RibbonQuickAccessIconAppearance.ColorWithGlow;
            var scoped = new DropShadowEffect { Color = Colors.White, BlurRadius = 2, ShadowDepth = 0 };
            ribbon.Resources["RibbonKit.Effects.Qat.IconGlow"] = scoped;
            ThemeManager.Apply(application, RibbonTheme.Office2024); Layout();
            Assert.Same(scoped, Part<Image>((Control)ribbon.QuickAccessItems[0], "SmallImageGlow").Effect);
            ThemeManager.SetAccentedTitleBar(application, false); Layout();
            Assert.All(ribbon.QuickAccessItems.Cast<Control>(), proxy =>
                Assert.Equal(Visibility.Collapsed, Part<Image>(proxy, "SmallImageGlow").Visibility));
            Assert.Equal(RibbonQuickAccessIconAppearance.ColorWithGlow, ribbon.QuickAccessIconAppearance);

            ThemeManager.Apply(application, RibbonTheme.Office2019);
            ThemeManager.SetAccentedTitleBar(application, true);
            ribbon.QuickAccessPosition = RibbonQuickAccessPosition.TabRow;
            ribbon.QuickAccessMaxWidth = 60; Layout();
            var tabs = Part<RibbonTabControl>(ribbon, "TabControlHost");
            var toolbar = Assert.IsType<RibbonQuickAccessToolBar>(tabs.Template.FindName("QatTabRowHost", tabs));
            Assert.True(toolbar.HasOverflow);
            Part<ToggleButton>(toolbar, "PART_OverflowButton").IsChecked = true; Layout();
            var overflow = Part<ItemsControl>(toolbar, "PART_OverflowHost");
            Assert.NotEmpty(overflow.Items);
            foreach (Control entry in overflow.Items)
            {
                Assert.False(Ribbon.GetQatOnColoredSurface(entry));
                Assert.Equal(Visibility.Collapsed, Part<Image>(entry, "SmallImageGlow").Visibility);
                Assert.Equal(Visibility.Collapsed, Part<Rectangle>(entry, "SmallImageTint").Visibility);
            }
            Part<ToggleButton>(toolbar, "PART_OverflowButton").IsChecked = false;
        }
        finally
        {
            window.Close(); ThemeManager.SetAccentedTitleBar(application, false);
            ThemeManager.SetDarkMode(application, false); RibbonAnimation.GlobalLevel = motion;
        }

        void Layout()
        {
            window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
            window.UpdateLayout();
        }
    }

    private static T Part<T>(Control owner, string name) where T : FrameworkElement =>
        Assert.IsType<T>(owner.Template.FindName(name, owner));

    private static void Capture(RibbonWindow window, string name, double scale)
    {
        string? directory = Environment.GetEnvironmentVariable("RIBBONKIT_QAT_ICON_DIAGNOSTICS");
        if (string.IsNullOrEmpty(directory)) return;
        Directory.CreateDirectory(directory);
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(window.ActualWidth * scale),
            (int)Math.Ceiling(window.ActualHeight * scale), 96 * scale, 96 * scale, PixelFormats.Pbgra32);
        bitmap.Render(window);
        var strip = new CroppedBitmap(bitmap, new Int32Rect(0, 0, bitmap.PixelWidth, (int)(54 * scale)));
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(strip));
        using var file = File.Create(System.IO.Path.Combine(directory, name + ".png")); encoder.Save(file);
    }
}
