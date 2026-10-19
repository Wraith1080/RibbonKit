using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using RibbonKit.Controls;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Tests;

public sealed class AdaptiveQuickAccessIconTests
{
    [Theory]
    [InlineData("#2B579A")]
    [InlineData("#7030A0")]
    [InlineData("#F5D547")]
    public void Layered_vectors_retain_contrast_alpha_geometry_and_original_source(string surface) => Sta.Run(() =>
    {
        var drawing = SampleDrawing();
        var source = new DrawingImage(drawing); source.Freeze();
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(surface));
        var adapted = Assert.IsType<DrawingImage>(QatAdaptiveIcon.Adapt(source, brush));
        var result = Assert.IsType<DrawingGroup>(adapted.Drawing);
        Assert.NotSame(source, adapted);
        Assert.True(adapted.IsFrozen);
        Assert.Equal(source.Width, adapted.Width);
        Assert.Equal(source.Height, adapted.Height);
        Assert.Same(adapted, QatAdaptiveIcon.Adapt(source, brush.Clone()));
        Assert.Equal(Colors.DimGray, ColorAt(drawing, 1));
        Assert.Equal(Colors.White, ColorAt(drawing, 2));
        Assert.True(Contrast(ColorAt(result, 1), brush.Color) >= 3);
        if (ColorAt(result, 2).A == 255)
            Assert.True(Contrast(ColorAt(result, 2), ColorAt(result, 1)) >= 3);
        else Assert.Equal(64, ColorAt(result, 2).A);
        Assert.True(ColorAt(result, 3).B > ColorAt(result, 3).R);
        Assert.Equal(0, ColorAt(result, 0).A);
        for (int index = 0; index < drawing.Children.Count; index++)
        {
            var before = Assert.IsType<GeometryDrawing>(drawing.Children[index]);
            var after = Shape(result.Children[index]);
            Assert.Equal(before.Geometry.ToString(), after.Geometry.ToString());
            if (index != 2) Assert.Equal(((SolidColorBrush)before.Brush).Color.A, ((SolidColorBrush)after.Brush).Color.A);
        }
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Would_be_black_fill_reveals_the_surface_without_erasing_later_accents(bool transformed) => Sta.Run(() =>
    {
        var group = SampleDrawing();
        group.Children.Add(new GeometryDrawing(Brushes.OrangeRed, null, new RectangleGeometry(new Rect(6, 3, 3, 3))));
        if (transformed) group.Transform = new TranslateTransform(2, 1);
        var source = new DrawingImage(group); source.Freeze();
        var result = Assert.IsType<DrawingImage>(QatAdaptiveIcon.Adapt(source, Brushes.DarkBlue));
        // Draw in the image's own bounds, avoiding image auto-scaling masking a geometry error.
        var bitmap = Render(result);
        Assert.Equal(255, AlphaAt(bitmap, 1, 1));
        Assert.InRange(AlphaAt(bitmap, 4, 4), 63, 65);
        Assert.Equal(255, AlphaAt(bitmap, 7, 4));
        Assert.Equal(255, AlphaAt(Render(source), 4, 4));
        Assert.Equal(Colors.White, ColorAt(group, 2));
        Assert.Equal(source.Width, result.Width);
        Assert.Equal(source.Height, result.Height);
    });

    [Fact]
    public void Would_be_black_stroke_cuts_through_its_own_fill() => Sta.Run(() =>
    {
        var source = new DrawingImage(new GeometryDrawing(Brushes.White, new Pen(Brushes.White, 2),
            new RectangleGeometry(new Rect(0, 0, 16, 16))));
        source.Freeze();
        var result = Assert.IsType<DrawingImage>(QatAdaptiveIcon.Adapt(source, Brushes.DarkBlue));
        var bitmap = Render(result);
        Assert.Equal(255, AlphaAt(bitmap, 9, 9));
        Assert.InRange(AlphaAt(bitmap, 1, 9), 63, 65);
        Assert.Equal(255, AlphaAt(Render(source), 1, 9));
    });

    [Fact]
    public void Authored_black_details_remain_opaque_and_a_generated_dark_fill_becomes_partial_alpha() => Sta.Run(() =>
    {
        var group = new DrawingGroup();
        group.Children.Add(new GeometryDrawing(Brushes.White, null, new RectangleGeometry(new Rect(0, 0, 16, 16))));
        group.Children.Add(new GeometryDrawing(Brushes.Black, null, new RectangleGeometry(new Rect(4, 4, 4, 4))));
        var source = new DrawingImage(group); source.Freeze();
        var result = Assert.IsType<DrawingImage>(QatAdaptiveIcon.Adapt(source, Brushes.DarkBlue));
        Assert.Equal(Colors.Black, ColorAt((DrawingGroup)result.Drawing, 1));
        Assert.Equal(255, AlphaAt(Render(result), 5, 5));
        var single = new DrawingImage(new GeometryDrawing(Brushes.White, null, new RectangleGeometry(new Rect(0, 0, 16, 16))));
        single.Freeze();
        Assert.InRange(AlphaAt(Render(Assert.IsType<DrawingImage>(QatAdaptiveIcon.Adapt(single, Brushes.Gold))), 5, 5), 63, 65);
    });

    [Fact]
    public void Unsupported_artwork_is_preserved_and_cache_does_not_hide_mutable_changes() => Sta.Run(() =>
    {
        var surface = Brushes.DarkBlue;
        var bitmap = BitmapSource.Create(1, 1, 96, 96, PixelFormats.Bgra32, null, new byte[] { 10, 20, 30, 255 }, 4);
        Assert.Same(bitmap, QatAdaptiveIcon.Adapt(bitmap, surface));
        var gradient = new DrawingImage(new GeometryDrawing(new LinearGradientBrush(Colors.Red, Colors.Blue, 90),
            null, new RectangleGeometry(new Rect(0, 0, 16, 16))));
        gradient.Freeze();
        Assert.Same(gradient, QatAdaptiveIcon.Adapt(gradient, surface));
        var source = new DrawingImage(SampleDrawing());
        var first = QatAdaptiveIcon.Adapt(source, surface);
        Assert.Same(first, QatAdaptiveIcon.Adapt(source, surface));
        ((GeometryDrawing)((DrawingGroup)source.Drawing).Children[3]).Brush = Brushes.OrangeRed;
        var second = Assert.IsType<DrawingImage>(QatAdaptiveIcon.Adapt(source, surface));
        Assert.NotSame(first, second);
        Assert.True(ColorAt((DrawingGroup)second.Drawing, 3).R > ColorAt((DrawingGroup)second.Drawing, 3).B);
        source.Freeze();
        Assert.NotSame(QatAdaptiveIcon.Adapt(source, surface), QatAdaptiveIcon.Adapt(source, Brushes.Gold));
    });

    [Fact]
    public void Live_icon_binding_mutations_surface_changes_and_unload_restore_correct_sources() => Sta.Run(() =>
    {
        var application = Sta.UseApplication();
        ThemeManager.Apply(application, RibbonTheme.Office2024);
        var source = new DrawingImage(SampleDrawing());
        var owner = new RibbonButton { Icon = source };
        var image = new Image { Width = 16, Height = 16 };
        BindingOperations.SetBinding(image, QatAdaptiveIcon.SourceProperty, new Binding(nameof(RibbonButton.Icon)) { Source = owner });
        QatAdaptiveIcon.SetSurface(image, Brushes.DarkBlue);
        QatAdaptiveIcon.SetActive(image, true);
        var window = new Window { Content = image, Width = 100, Height = 100, Left = -10000, Top = -10000,
            ShowActivated = false, ShowInTaskbar = false };
        try
        {
            window.Show(); Layout(window);
            var first = image.Source;
            ((GeometryDrawing)((DrawingGroup)source.Drawing).Children[3]).Brush = Brushes.OrangeRed;
            Layout(window);
            Assert.NotSame(first, image.Source);
            Assert.Same(source, owner.Icon);
            var changed = image.Source;
            var liveSurface = new SolidColorBrush(Colors.DarkBlue);
            QatAdaptiveIcon.SetSurface(image, liveSurface);
            liveSurface.Color = Colors.Gold;
            Assert.NotSame(changed, image.Source);
            source.Freeze(); Layout(window);
            Assert.NotSame(source, image.Source);
            QatAdaptiveIcon.SetActive(image, false);
            Assert.Same(source, image.Source);
            var replacement = new DrawingImage(SampleDrawing()); replacement.Freeze();
            owner.Icon = replacement; Layout(window);
            Assert.Same(replacement, image.Source);
            QatAdaptiveIcon.SetActive(image, true);
            Assert.NotSame(replacement, image.Source);
            window.Content = null; Layout(window);
            window.Content = image; Layout(window);
            Assert.NotSame(replacement, image.Source);
        }
        finally { window.Close(); Sta.ResetApplication(); }
    });

    [Fact]
    public void Nested_transforms_clipping_pens_and_opacity_survive_adaptation() => Sta.Run(() =>
    {
        var nested = SampleDrawing();
        nested.Transform = new TranslateTransform(4, 5);
        nested.ClipGeometry = new RectangleGeometry(new Rect(0, 0, 14, 16));
        nested.Opacity = .8;
        var path = new GeometryDrawing(null, new Pen(Brushes.DimGray, 1.5)
        { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round }, Geometry.Parse("M4,4 L8,4"));
        nested.Children.Add(path);
        var root = new DrawingGroup { Transform = new ScaleTransform(1.2, 1.2) };
        root.Children.Add(nested);
        var source = new DrawingImage(root); source.Freeze();
        var adapted = Assert.IsType<DrawingImage>(QatAdaptiveIcon.Adapt(source, Brushes.DarkBlue));
        var resultRoot = Assert.IsType<DrawingGroup>(adapted.Drawing);
        var result = Assert.IsType<DrawingGroup>(Assert.Single(resultRoot.Children));
        Assert.Equal(root.Transform.Value, resultRoot.Transform.Value);
        Assert.Equal(nested.Transform.Value, result.Transform.Value);
        Assert.Equal(nested.ClipGeometry.ToString(), result.ClipGeometry.ToString());
        Assert.Equal(nested.Opacity, result.Opacity);
        var pen = Assert.IsType<GeometryDrawing>(result.Children[^1]).Pen;
        Assert.Equal(path.Pen.Thickness, pen.Thickness);
        Assert.Equal(path.Pen.StartLineCap, pen.StartLineCap);
        Assert.Equal(path.Pen.EndLineCap, pen.EndLineCap);
    });

    [Theory]
    [InlineData(RibbonTheme.Office2024, "#2B579A")]
    [InlineData(RibbonTheme.Office2024, "#7030A0")]
    [InlineData(RibbonTheme.Office2024, "#F5D547")]
    [InlineData(RibbonTheme.Office2007, "#2B579A")]
    [InlineData(RibbonTheme.Office2007, "#7030A0")]
    [InlineData(RibbonTheme.Office2007, "#F5D547")]
    public void Authored_icons_render_with_all_three_choices_on_colored_bands(RibbonTheme theme, string accent) => Sta.Run(() =>
    {
        var application = Sta.UseApplication(showcaseResources: true);
        string? directory = Environment.GetEnvironmentVariable("RIBBONKIT_QAT_ADAPTIVE_DIAGNOSTICS");
        var window = new RibbonWindow { Width = 760, Height = 260, Left = -10000, Top = -10000,
            ShowActivated = false, ShowInTaskbar = false, UseLayoutRounding = true };
        try
        {
            var ribbon = new Ribbon { QuickAccessPosition = RibbonQuickAccessPosition.TitleBar, QuickAccessMaxWidth = 600 };
            var tab = new RibbonTab { Header = "Home" };
            var group = new RibbonGroup { Header = "Commands" }; tab.Groups.Add(group);
            ribbon.Tabs.Add(tab); ribbon.SelectedTab = tab;
            foreach (string name in new[] { "Save", "Undo", "Redo", "Mouse", "Touch", "Copy", "Paste", "Bold", "Find" })
            {
                var button = new RibbonButton { Header = name, Icon = (ImageSource)application.FindResource("Icon." + name) };
                group.Items.Add(button); ribbon.AddToQuickAccess(button);
            }
            window.Content = ribbon; window.Show();
            foreach (double scale in new[] { 1d, 1.25, 1.5, 2d })
            {
                ThemeManager.Apply(application, theme);
                ThemeManager.SetAccentedTitleBar(application, true);
                ThemeManager.SetAccent(application, (Color)ColorConverter.ConvertFromString(accent));
                VisualTreeHelper.SetRootDpi(window, new DpiScale(scale, scale));
                foreach (var appearance in Enum.GetValues<RibbonQuickAccessIconAppearance>())
                {
                    ribbon.QuickAccessIconAppearance = appearance; Layout(window);
                    foreach (Control proxy in ribbon.QuickAccessItems)
                    {
                        var image = (Image)proxy.Template.FindName("SmallImage", proxy);
                        Assert.Null(image.Effect);
                        if (appearance == RibbonQuickAccessIconAppearance.AdaptiveColor)
                        {
                            Assert.True(image.IsVisible);
                            Assert.NotSame(((RibbonButton)proxy).Icon, image.Source);
                        }
                    }
                    if (string.IsNullOrEmpty(directory)) continue;
                    Directory.CreateDirectory(directory);
                    var bitmap = new RenderTargetBitmap((int)Math.Ceiling(window.ActualWidth * scale),
                        (int)Math.Ceiling(window.ActualHeight * scale), 96 * scale, 96 * scale, PixelFormats.Pbgra32);
                    bitmap.Render(window);
                    var strip = new CroppedBitmap(bitmap, new Int32Rect(0, 0, (int)(330 * scale), (int)(40 * scale)));
                    var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(strip));
                    using var file = File.Create(Path.Combine(directory, $"{theme}-{accent[1..]}-{scale * 100:0}-{appearance}.png"));
                    encoder.Save(file);
                }
            }
        }
        finally { window.Close(); Sta.ResetApplication(); }
    });

    private static DrawingGroup SampleDrawing()
    {
        var group = new DrawingGroup();
        group.Children.Add(new GeometryDrawing(Brushes.Transparent, null, new RectangleGeometry(new Rect(0, 0, 16, 16))));
        group.Children.Add(new GeometryDrawing(Brushes.DimGray, null, new RectangleGeometry(new Rect(0, 0, 16, 16))));
        group.Children.Add(new GeometryDrawing(Brushes.White, null, new RectangleGeometry(new Rect(3, 2, 10, 6))));
        group.Children.Add(new GeometryDrawing(Brushes.RoyalBlue, null, new RectangleGeometry(new Rect(3, 11, 7, 3))));
        return group;
    }

    private static GeometryDrawing Shape(Drawing drawing) => drawing is DrawingGroup group
        ? Shape(Assert.Single(group.Children)) : Assert.IsType<GeometryDrawing>(drawing);
    private static Color ColorAt(DrawingGroup group, int index) => ((SolidColorBrush)Shape(group.Children[index]).Brush).Color;
    private static RenderTargetBitmap Render(DrawingImage image)
    {
        var visual = new DrawingVisual();
        using (var context = visual.RenderOpen()) context.DrawImage(image, new Rect(0, 0, image.Width, image.Height));
        var bitmap = new RenderTargetBitmap((int)image.Width, (int)image.Height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        return bitmap;
    }
    private static byte AlphaAt(BitmapSource bitmap, int x, int y)
    {
        var pixel = new byte[4];
        bitmap.CopyPixels(new Int32Rect(x, y, 1, 1), pixel, 4, 0);
        return pixel[3];
    }
    private static double Contrast(Color first, Color second)
    {
        static double Luminance(Color color)
        {
            static double Linear(byte value) => value / 255d <= .04045 ? value / 255d / 12.92 : Math.Pow((value / 255d + .055) / 1.055, 2.4);
            return .2126 * Linear(color.R) + .7152 * Linear(color.G) + .0722 * Linear(color.B);
        }
        double a = Luminance(first), b = Luminance(second);
        return (Math.Max(a, b) + .05) / (Math.Min(a, b) + .05);
    }

    private static void Layout(Window window)
    {
        window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        window.UpdateLayout();
    }
}
