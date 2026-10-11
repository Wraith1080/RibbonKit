using System.IO;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using RibbonKit.Animation;
using RibbonKit.Controls;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Portability.Tests;

internal static class BackstageCaptionPortabilityChecks
{
    internal static void Verify(Application application)
    {
        var motion = RibbonAnimation.GlobalLevel;
        RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
        ThemeManager.Apply(application, RibbonTheme.Office2024);
        var backstage = new Backstage { Design = RibbonBackstageDesign.Classic };
        backstage.Items.Add(new BackstageTabItem
        {
            Header = "Home", Content = new TextBlock { Text = "Backstage page", FontSize = 24 },
        });
        backstage.Items.Add(new BackstageTabItem { Header = "Open" });
        backstage.SelectedIndex = 0;
        var ribbon = new Ribbon { Backstage = backstage };
        ribbon.Tabs.Add(new RibbonTab { Header = "Home" });
        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.Children.Add(ribbon);
        var document = new Border { Background = Brushes.White };
        Grid.SetRow(document, 1);
        root.Children.Add(document);
        var icon = new DrawingImage(new GeometryDrawing(Brushes.Blue, null, new RectangleGeometry(new Rect(0, 0, 16, 16))));
        var window = new RibbonWindow
        {
            Title = "RibbonKit-only Backstage", Icon = icon, Content = root,
            Width = 840, Height = 500, Left = -10000, Top = -10000,
            ShowActivated = false, ShowInTaskbar = false,
        };
        try
        {
            window.Show();
            foreach (bool dark in new[] { false, true })
            foreach (RibbonDensity density in new[] { RibbonDensity.Compact, RibbonDensity.Touch })
            foreach (FlowDirection flow in new[] { FlowDirection.LeftToRight, FlowDirection.RightToLeft })
            {
                ThemeManager.Apply(application, RibbonTheme.Office2013);
                ThemeManager.SetDarkMode(application, dark);
                window.Resources.MergedDictionaries.Clear();
                window.Resources.MergedDictionaries.Add(ThemeManager.CreatePalette(RibbonTheme.Office2013, Colors.MediumPurple, dark));
                backstage.Resources.MergedDictionaries.Clear();
                backstage.Resources.MergedDictionaries.Add(ThemeManager.CreatePalette(RibbonTheme.Office2013, Colors.MediumPurple, dark));
                window.FlowDirection = flow;
                ribbon.FlowDirection = flow;
                ribbon.Density = density;
                backstage.Design = RibbonBackstageDesign.Classic;
                Drain(window);
                var originalTitle = Part<Border>(window, "TitleBarBackgroundLayer").Background;
                var titleIcon = Part<Image>(window, "PART_WindowIcon");
                Assert.Equal(Visibility.Visible, titleIcon.Visibility);
                ribbon.IsBackstageOpen = true;
                Drain(window);
                VerifySeam(window, backstage, icon);
                foreach (double scale in new[] { 1.25, 2d })
                {
                    VisualTreeHelper.SetRootDpi(window, new DpiScale(scale, scale));
                    InvalidateDpiLayout(window);
                    Drain(window);
                    Assert.Equal(scale, VisualTreeHelper.GetDpi(window).DpiScaleX, 4);
                    Assert.Equal(flow, backstage.FlowDirection);
                    VerifySeam(window, backstage, icon);
                    Capture(window, $"office2013-{(dark ? "dark" : "light")}-{density}-{flow}-{scale * 100:0}", scale);
                    VerifyJoinPixels(window, backstage, scale);
                }

                // A replacement template publishes its new realized rail/page references.
                var template = backstage.Template;
                backstage.Template = null;
                backstage.Template = template;
                backstage.ApplyTemplate();
                Drain(window);
                VerifySeam(window, backstage, icon);

                backstage.Design = RibbonBackstageDesign.Modern;
                Drain(window);
                Assert.Equal(Visibility.Collapsed, Part<Grid>(window, "BackstageCaptionBand").Visibility);
                Assert.Equal(Visibility.Visible, titleIcon.Visibility);
                Assert.Same(originalTitle, Part<Border>(window, "TitleBarBackgroundLayer").Background);
                backstage.Design = RibbonBackstageDesign.Classic;
                Drain(window);
                VerifySeam(window, backstage, icon);
                ribbon.IsBackstageOpen = false;
                Drain(window);
                Assert.Same(DependencyProperty.UnsetValue, backstage.ReadLocalValue(FrameworkElement.FlowDirectionProperty));
                Assert.Equal(Visibility.Collapsed, Part<Grid>(window, "BackstageCaptionBand").Visibility);
                Assert.Equal(Visibility.Visible, titleIcon.Visibility);
                Assert.Same(originalTitle, Part<Border>(window, "TitleBarBackgroundLayer").Background);
                Assert.Same(icon, window.Icon);
            }
            VerifyMotion(window, ribbon, backstage, icon);
        }
        finally
        {
            ribbon.IsBackstageOpen = false;
            window.Close();
            ThemeManager.SetDarkMode(application, false);
            RibbonAnimation.GlobalLevel = motion;
        }
    }

    private static void VerifyMotion(RibbonWindow window, Ribbon ribbon, Backstage backstage, ImageSource icon)
    {
        bool respectReducedMotion = RibbonAnimation.RespectSystemReduceMotion;
        var actionOverride = RibbonAnimation.GetActionOverride(RibbonAnimationAction.Backstage);
        try
        {
            RibbonAnimation.RespectSystemReduceMotion = false;
            RibbonAnimation.ClearActionLevel(RibbonAnimationAction.Backstage);
            window.Resources["RibbonKit.Brushes.TitleBar.Background"] = Brushes.Navy;
            window.Resources["RibbonKit.Brushes.TitleBar.Foreground"] = Brushes.White;
            backstage.Resources["RibbonKit.Brushes.Text.Primary"] = Brushes.Black;
            backstage.Design = RibbonBackstageDesign.Classic;
            foreach (RibbonAnimationLevel level in new[] { RibbonAnimationLevel.Subtle, RibbonAnimationLevel.Expressive })
            foreach (FlowDirection flow in new[] { FlowDirection.LeftToRight, FlowDirection.RightToLeft })
            {
                RibbonAnimation.GlobalLevel = level;
                window.FlowDirection = ribbon.FlowDirection = flow;
                Drain(window);
                var band = Part<Grid>(window, "BackstageCaptionBand");
                var normalBackground = Part<Border>(window, "TitleBarBackgroundLayer").Background;
                ribbon.IsBackstageOpen = true;
                Assert.Equal(0d, backstage.Opacity); // The first frame uses the shared open seed.
                SampleMotion(window, backstage, $"{level}-{flow}-opening");
                PumpUntil(() => !backstage.HasAnimatedProperties);
                Drain(window);
                VerifySeam(window, backstage, icon);
                Assert.Same(normalBackground, Part<Border>(window, "TitleBarBackgroundLayer").Background);

                ribbon.IsBackstageOpen = false;
                Assert.Equal(Visibility.Visible, band.Visibility); // Paint survives the logical close.
                SampleMotion(window, backstage, $"{level}-{flow}-closing");
                // Reopening replaces the body clocks and keeps this one caption surface attached.
                ribbon.IsBackstageOpen = true;
                Drain(window);
                SampleMotion(window, backstage, $"{level}-{flow}-reopening");
                PumpUntil(() => !backstage.HasAnimatedProperties);
                Drain(window);
                Assert.True(ribbon.IsBackstageOpen);
                VerifySeam(window, backstage, icon);
                ribbon.IsBackstageOpen = false;
                SampleMotion(window, backstage, $"{level}-{flow}-final-close");
                PumpUntil(() => band.Visibility == Visibility.Collapsed);
                Drain(window);
                Assert.Equal(Visibility.Visible, Part<Image>(window, "PART_WindowIcon").Visibility);
                Assert.Same(normalBackground, Part<Border>(window, "TitleBarBackgroundLayer").Background);
                Assert.Same(Brushes.White, Part<TextBlock>(window, "PART_Title").Foreground);
            }

            // Per-action disabling follows the same instant path as global/reduced motion.
            RibbonAnimation.SetActionLevel(RibbonAnimationAction.Backstage, RibbonAnimationLevel.None);
            ribbon.IsBackstageOpen = true;
            Drain(window);
            VerifySeam(window, backstage, icon);
            Assert.Equal(1d, Part<Grid>(window, "BackstageCaptionBand").Opacity);
            Assert.Equal(Matrix.Identity, Part<Grid>(window, "BackstageCaptionBand").RenderTransform.Value);
            ribbon.IsBackstageOpen = false;
            Drain(window);
            Assert.Equal(Visibility.Collapsed, Part<Grid>(window, "BackstageCaptionBand").Visibility);
        }
        finally
        {
            RibbonAnimation.SetActionLevel(RibbonAnimationAction.Backstage, RibbonAnimationLevel.None);
            ribbon.IsBackstageOpen = false;
            Drain(window);
            if (actionOverride is { } original) RibbonAnimation.SetActionLevel(RibbonAnimationAction.Backstage, original);
            else RibbonAnimation.ClearActionLevel(RibbonAnimationAction.Backstage);
            RibbonAnimation.RespectSystemReduceMotion = respectReducedMotion;
        }
    }

    private static void SampleMotion(RibbonWindow window, Backstage backstage, string name)
    {
        var band = Part<Grid>(window, "BackstageCaptionBand");
        (double BodyOpacity, double CaptionOpacity, Matrix BodyTransform, Matrix CaptionTransform,
            double RailX, double CaptionRailX, Brush Foreground)? sample = null;
        PumpUntil(() =>
        {
            double opacity = backstage.Opacity;
            if (opacity <= .001 || opacity >= .999) return false;
            sample = (opacity, band.Opacity, backstage.RenderTransform.Value, band.RenderTransform.Value,
                Part<Border>(backstage, "NavColumn").TransformToAncestor(window).Transform(default).X,
                Part<Border>(window, "BackstageCaptionRail").TransformToAncestor(window).Transform(default).X,
                Part<TextBlock>(window, "PART_Title").Foreground);
            Capture(window, "motion-" + name, VisualTreeHelper.GetDpi(window).DpiScaleX);
            return true;
        });
        var observed = sample!.Value;
        Assert.Equal(observed.BodyOpacity, observed.CaptionOpacity, 6);
        Assert.Equal(observed.BodyTransform, observed.CaptionTransform);
        Assert.Equal(observed.RailX, observed.CaptionRailX, 4);
        Assert.NotEqual(0d, observed.BodyTransform.OffsetX);
        Assert.Equal(Matrix.Identity, backstage.LayoutTransform.Value);
        Assert.Equal(Matrix.Identity, band.LayoutTransform.Value);

        // White caption glyphs fade to the black page foreground with the same progress.
        var visual = new DrawingVisual();
        using (var drawing = visual.RenderOpen()) drawing.DrawRectangle(observed.Foreground, null, new Rect(0, 0, 1, 1));
        var bitmap = new RenderTargetBitmap(1, 1, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        var pixel = new byte[4];
        bitmap.CopyPixels(pixel, 4, 0);
        for (int i = 0; i < 3; i++) Assert.InRange(Math.Abs(pixel[i] - 255 * (1 - observed.BodyOpacity)), 0, 1);
        Assert.Equal(255, pixel[3]);
    }

    private static void PumpUntil(Func<bool> condition)
    {
        if (condition()) return;
        bool reached = false;
        var watch = Stopwatch.StartNew();
        var frame = new DispatcherFrame();
        EventHandler render = (_, _) => { if (condition()) { reached = true; frame.Continue = false; } };
        CompositionTarget.Rendering += render;
        var timer = new DispatcherTimer(DispatcherPriority.Normal) { Interval = TimeSpan.FromMilliseconds(10) };
        timer.Tick += (_, _) =>
        {
            if (condition()) { reached = true; frame.Continue = false; }
            else if (watch.Elapsed > TimeSpan.FromSeconds(2)) frame.Continue = false;
        };
        timer.Start();
        try { Dispatcher.PushFrame(frame); }
        finally { timer.Stop(); CompositionTarget.Rendering -= render; }
        Assert.True(reached, "The Backstage caption transition did not reach its expected state within two seconds.");
    }

    private static void VerifySeam(RibbonWindow window, Backstage backstage, ImageSource icon)
    {
        var rail = Part<Border>(backstage, "NavColumn");
        var page = Part<Border>(backstage, "ContentArea");
        var caption = Part<Border>(window, "BackstageCaptionRail");
        Assert.Same(rail.Background, caption.Background);
        Assert.Same(page.Background, Part<Grid>(window, "BackstageCaptionBand").Background);
        Assert.Same(TextElement.GetForeground(page), Part<TextBlock>(window, "PART_Title").Foreground);
        foreach (string name in new[] { "PART_MinimizeButton", "PART_MaximizeButton", "PART_RestoreButton", "PART_CloseButton" })
            Assert.Same(TextElement.GetForeground(page), Part<Button>(window, name).Foreground);
        Assert.Equal(rail.ActualWidth, caption.ActualWidth, 4);
        Assert.Equal(rail.TransformToAncestor(window).Transform(default).X,
            caption.TransformToAncestor(window).Transform(default).X, 4);
        double railX = rail.TransformToAncestor(window).Transform(default).X;
        if (backstage.FlowDirection == FlowDirection.RightToLeft)
            Assert.True(railX > window.ActualWidth / 2, "The RTL rail must be on the right.");
        else
            Assert.InRange(railX, 0, 1);
        Assert.Equal(Visibility.Visible, Part<Grid>(window, "BackstageCaptionBand").Visibility);
        Assert.Equal(Visibility.Collapsed, Part<Image>(window, "PART_WindowIcon").Visibility);
        Assert.Same(icon, window.Icon);
    }

    private static T Part<T>(Control control, string name) where T : FrameworkElement =>
        Assert.IsType<T>(control.Template.FindName(name, control));

    private static void Drain(Window window)
    {
        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        window.UpdateLayout();
        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
    }

    private static void InvalidateDpiLayout(DependencyObject visual)
    {
        if (visual is UIElement element) { element.InvalidateMeasure(); element.InvalidateArrange(); }
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(visual); i++)
            InvalidateDpiLayout(VisualTreeHelper.GetChild(visual, i));
    }

    private static void Capture(Window window, string name, double scale)
    {
        string? directory = Environment.GetEnvironmentVariable("RIBBONKIT_BACKSTAGE_CAPTION_DIAGNOSTICS");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        var bitmap = Render(window, scale);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(Path.Combine(directory, name + ".png"));
        encoder.Save(stream);
    }

    private static RenderTargetBitmap Render(Window window, double scale)
    {
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(window.ActualWidth * scale),
            (int)Math.Ceiling(window.ActualHeight * scale), 96 * scale, 96 * scale, PixelFormats.Pbgra32);
        bitmap.Render(window);
        return bitmap;
    }

    private static void VerifyJoinPixels(RibbonWindow window, Backstage backstage, double scale)
    {
        var rail = Part<Border>(backstage, "NavColumn");
        Color expected = Assert.IsType<SolidColorBrush>(rail.Background).Color;
        Rect bounds = Part<Grid>(window, "TitleBarBand").TransformToAncestor(window)
            .TransformBounds(new Rect(Part<Grid>(window, "TitleBarBand").RenderSize));
        Rect railBounds = rail.TransformToAncestor(window).TransformBounds(new Rect(rail.RenderSize));
        int x = (int)Math.Round((railBounds.Left + 8) * scale);
        int edge = (int)Math.Round(bounds.Bottom * scale);
        var bitmap = Render(window, scale);
        var pixel = new byte[4];
        for (int y = edge - 2; y <= edge + 2; y++)
        {
            bitmap.CopyPixels(new Int32Rect(x, y, 1, 1), pixel, 4, 0);
            Assert.True(Math.Abs(pixel[0] - expected.B) <= 1,
                $"DPI {scale}, edge {edge}, y {y}, rail {railBounds}, band {bounds}, expected {expected}, pixel {pixel[2]},{pixel[1]},{pixel[0]},{pixel[3]}");
            Assert.InRange(Math.Abs(pixel[1] - expected.G), 0, 1);
            Assert.InRange(Math.Abs(pixel[2] - expected.R), 0, 1);
            Assert.Equal(255, pixel[3]);
        }
    }
}
