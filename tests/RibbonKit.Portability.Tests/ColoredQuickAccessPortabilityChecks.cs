using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using RibbonKit.Animation;
using RibbonKit.Controls;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Portability.Tests;

// Real QAT proxies and nested templates, without any Showcase resources or helpers.
internal static class ColoredQuickAccessPortabilityChecks
{
    internal static void Verify(Application application)
    {
        var animation = RibbonAnimation.GlobalLevel;
        RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
        var icon = new DrawingImage(new GeometryDrawing(Brushes.SteelBlue, null,
            Geometry.Parse("M4,2 L12,2 L12,15 L4,15 Z")));
        var source = new RibbonSplitButton { Header = "Paste", Icon = icon, Size = RibbonControlSize.Small };
        source.Items.Add(new RibbonMenuItem { Header = "Paste Special" });
        var dropdownSource = new RibbonDropDownButton { Header = "Select", Icon = icon, Size = RibbonControlSize.Small };
        dropdownSource.Items.Add(new RibbonMenuItem { Header = "Select All" });
        var group = new RibbonGroup { Header = "Clipboard", CanResize = false };
        group.Items.Add(source); group.Items.Add(dropdownSource);
        var tab = new RibbonTab { Header = "Home" }; tab.Groups.Add(group);
        var ribbon = new Ribbon(); ribbon.Tabs.Add(tab); ribbon.SelectedTab = tab;
        ribbon.AddToQuickAccess(source); ribbon.AddToQuickAccess(dropdownSource);
        var split = Assert.IsType<RibbonSplitButton>(ribbon.QuickAccessItems[0]);
        var dropdown = Assert.IsType<RibbonDropDownButton>(ribbon.QuickAccessItems[1]);
        var window = new RibbonWindow { Content = ribbon, Width = 840, Height = 330,
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false,
            UseLayoutRounding = true };
        var accent = Color.FromRgb(0x2B, 0x57, 0x9A);
        try
        {
            window.Show();
            foreach (var theme in new[] { RibbonTheme.Office2024, RibbonTheme.Office2019,
                RibbonTheme.Office2013, RibbonTheme.Office2010, RibbonTheme.Office2007 })
            foreach (bool dark in new[] { false, true })
            {
                ThemeManager.Apply(application, theme); ThemeManager.SetDarkMode(application, dark);
                ThemeManager.SetAccent(application, accent); ThemeManager.SetAccentedTitleBar(application, true);
                window.FlowDirection = dark ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
                double scale = dark ? 2d : 1.25;
                VisualTreeHelper.SetRootDpi(window, new DpiScale(scale, scale));
                var frames = theme is RibbonTheme.Office2007 or RibbonTheme.Office2010
                    ? new[] { RibbonWindowFrameAppearance.Default, theme == RibbonTheme.Office2007
                        ? RibbonWindowFrameAppearance.Office2007Aero : RibbonWindowFrameAppearance.Office2010Aero }
                    : new[] { RibbonWindowFrameAppearance.Default };
                foreach (var frame in frames)
                {
                    window.FrameAppearance = frame;
                    ribbon.QuickAccessPosition = RibbonQuickAccessPosition.TitleBar; Layout(window);
                    Assert.True(Ribbon.GetQatOnColoredSurface(split));
                    VerifyColored(split, dropdown, window, theme, accent);
                    Save(window, ribbon, $"{theme}-{dark}-{frame}");
                }
                // A second accent updates already realized nested parts, including the companion wash.
                var green = Color.FromRgb(0x0B, 0x8A, 0x4A);
                ThemeManager.SetAccent(application, green); Layout(window);
                VerifyColored(split, dropdown, window, theme, green);
                // The detached title-bar host must honor a token authored on the ribbon.
                const string companionKey = "RibbonKit.Brushes.Qat.ColoredCompanionBackground";
                ribbon.Resources[companionKey] = Brushes.Magenta;
                ThemeManager.SetAccent(application, accent); Layout(window);
                var arrow = Part<ToggleButton>(split, "PART_Toggle");
                SetHover(arrow, true); Layout(window);
                Assert.Same(Brushes.Magenta, Part<Border>(Part<Button>(split, "PART_Primary"), "Chrome").Background);
                SetHover(arrow, false); ribbon.Resources.Remove(companionKey);
                ThemeManager.SetAccent(application, green); Layout(window);
                foreach (var placement in new[] { RibbonQuickAccessPosition.TabRow, RibbonQuickAccessPosition.BelowRibbon })
                {
                    ribbon.QuickAccessPosition = placement; Layout(window);
                    if (placement == RibbonQuickAccessPosition.TabRow && theme == RibbonTheme.Office2019)
                        VerifyColored(split, dropdown, window, theme, green);
                    else
                        VerifyNeutral(split, dropdown, window);
                }
                ribbon.QuickAccessPosition = RibbonQuickAccessPosition.TitleBar;
                ThemeManager.SetAccentedTitleBar(application, false); Layout(window);
                VerifyNeutral(split, dropdown, window);
            }
        }
        finally
        {
            window.Close(); ThemeManager.SetAccentedTitleBar(application, false);
            ThemeManager.ClearAccent(application); ThemeManager.SetDarkMode(application, false);
            RibbonAnimation.GlobalLevel = animation;
        }
    }

    private static void VerifyColored(RibbonSplitButton split, RibbonDropDownButton dropdown,
        RibbonWindow window, RibbonTheme theme, Color accent)
    {
        var primary = Part<Button>(split, "PART_Primary");
        var arrow = Part<ToggleButton>(split, "PART_Toggle");
        var mainChrome = Part<Border>(primary, "Chrome");
        var arrowChrome = Part<Border>(arrow, "Chrome");
        var toggle = Part<ToggleButton>(dropdown, "PART_Toggle");
        var dropdownChrome = Part<Border>(toggle, "Chrome");
        SetHover(arrow, true); Layout(window);
        Assert.Equal("Companion", primary.Tag);
        Assert.Same(split.Resources[Ribbon.QatColoredHoverBackgroundKey], arrowChrome.Background);
        VerifyCompanion(mainChrome, theme, accent);
        VerifyBorder(primary); VerifyBorder(arrow);
        SetHover(arrow, false); SetHover(primary, true); Layout(window);
        Assert.Same(split.Resources[Ribbon.QatColoredHoverBackgroundKey], mainChrome.Background);
        VerifyCompanion(arrowChrome, theme, accent);
        VerifyBorder(primary); VerifyBorder(arrow);
        SetPressed(primary, true); Layout(window);
        Assert.Same(split.Resources[Ribbon.QatColoredPressedBackgroundKey], mainChrome.Background);
        VerifyBorder(primary); VerifyBorder(arrow);
        SetPressed(primary, false); SetHover(primary, false);
        split.IsDropDownOpen = true; Layout(window);
        Assert.Same(split.Resources[Ribbon.QatColoredPressedBackgroundKey], arrowChrome.Background);
        VerifyCompanion(mainChrome, theme, accent);
        VerifyBorder(primary); VerifyBorder(arrow);
        SetHover(arrow, true); Layout(window);
        Assert.Same(split.Resources[Ribbon.QatColoredPressedBackgroundKey], arrowChrome.Background);
        split.IsDropDownOpen = false; SetHover(arrow, false);
        SetHover(toggle, true); Layout(window);
        Assert.Same(dropdown.Resources[Ribbon.QatColoredHoverBackgroundKey], dropdownChrome.Background);
        AssertTransparent(dropdownChrome.BorderBrush);
        dropdown.IsDropDownOpen = true; Layout(window);
        Assert.Same(dropdown.Resources[Ribbon.QatColoredPressedBackgroundKey], dropdownChrome.Background);
        AssertTransparent(dropdownChrome.BorderBrush);
        dropdown.IsDropDownOpen = false; SetHover(toggle, false); Layout(window);
    }

    private static void VerifyCompanion(Border chrome, RibbonTheme theme, Color accent)
    {
        if (theme is RibbonTheme.Office2007 or RibbonTheme.Office2010)
        {
            AssertTransparent(chrome.Background);
            return;
        }
        // Render the actual half over the accent band. It must stay blue/green and be
        // paler than the active hover (20% white in the title bar, 18% on the 2019 strip).
        var visual = new DrawingVisual();
        using (var drawing = visual.RenderOpen())
        {
            drawing.DrawRectangle(new SolidColorBrush(accent), null, new Rect(chrome.RenderSize));
            drawing.DrawRectangle(new VisualBrush(chrome) { Stretch = Stretch.None }, null, new Rect(chrome.RenderSize));
        }
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(chrome.ActualWidth),
            (int)Math.Ceiling(chrome.ActualHeight), 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        var pixel = new byte[4]; bitmap.CopyPixels(new Int32Rect(4, 5, 1, 1), pixel, 4, 0);
        Assert.InRange(pixel[0], Mix(accent.B) - 2, Mix(accent.B) + 2);
        Assert.InRange(pixel[1], Mix(accent.G) - 2, Mix(accent.G) + 2);
        Assert.InRange(pixel[2], Mix(accent.R) - 2, Mix(accent.R) + 2);
        static int Mix(byte value) => (int)Math.Round(value * 0.7 + 255 * 0.3);
    }

    private static void VerifyNeutral(RibbonSplitButton split, RibbonDropDownButton dropdown, Window window)
    {
        Assert.False(Ribbon.GetQatOnColoredSurface(split));
        Assert.False(split.Resources.Contains(Ribbon.QatColoredHoverBackgroundKey));
        Assert.False(split.Resources.Contains("RibbonKit.Brushes.Qat.ColoredCompanionBackground"));
        var primary = Part<Button>(split, "PART_Primary");
        var arrow = Part<ToggleButton>(split, "PART_Toggle");
        var toggle = Part<ToggleButton>(dropdown, "PART_Toggle");
        SetHover(arrow, true); SetHover(toggle, true); Layout(window);
        Assert.Same(split.FindResource("RibbonKit.Brushes.Control.CompanionBackground"), Part<Border>(primary, "Chrome").Background);
        Assert.Same(split.FindResource("RibbonKit.Brushes.Control.CompanionBorder"), Part<Border>(primary, "Chrome").BorderBrush);
        Assert.Same(split.FindResource("RibbonKit.Brushes.Control.DropDownHoverBorder"), Part<Border>(arrow, "Chrome").BorderBrush);
        Assert.Same(dropdown.FindResource("RibbonKit.Brushes.Control.DropDownHoverBorder"), Part<Border>(toggle, "Chrome").BorderBrush);
        SetHover(arrow, false); SetHover(toggle, false); Layout(window);
    }

    private static void VerifyBorder(Control half)
    {
        AssertTransparent(Part<Border>(half, "Chrome").BorderBrush);
        AssertTransparent(Part<Border>(half, "CompanionRim").BorderBrush);
    }
    private static void AssertTransparent(Brush? brush) =>
        Assert.True(brush is null || brush.Opacity == 0 || brush is SolidColorBrush { Color.A: 0 });
    private static T Part<T>(Control control, string name) where T : DependencyObject =>
        Assert.IsAssignableFrom<T>(control.Template.FindName(name, control));
    private static void SetHover(UIElement target, bool value) => target.SetValue(
        (DependencyPropertyKey)typeof(UIElement).GetField("IsMouseOverPropertyKey", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!, value);
    private static void SetPressed(Button target, bool value) => target.SetValue(
        (DependencyPropertyKey)typeof(ButtonBase).GetField("IsPressedPropertyKey", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!, value);
    private static void Layout(Window window)
    {
        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle); window.UpdateLayout();
        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
    }
    private static void Save(RibbonWindow window, Ribbon ribbon, string name)
    {
        string? directory = Environment.GetEnvironmentVariable("RIBBONKIT_COLORED_QAT_DIAGNOSTICS");
        if (string.IsNullOrEmpty(directory)) return;
        var split = (RibbonSplitButton)ribbon.QuickAccessItems[0];
        var dropdown = (RibbonDropDownButton)ribbon.QuickAccessItems[1];
        SetHover(Part<ToggleButton>(split, "PART_Toggle"), true);
        SetHover(Part<ToggleButton>(dropdown, "PART_Toggle"), true); Layout(window);
        Directory.CreateDirectory(directory);
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(window.ActualWidth), 70, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(window);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var file = File.Create(Path.Combine(directory, name + ".png")); encoder.Save(file);
        SetHover(Part<ToggleButton>(split, "PART_Toggle"), false);
        SetHover(Part<ToggleButton>(dropdown, "PART_Toggle"), false); Layout(window);
    }
}
