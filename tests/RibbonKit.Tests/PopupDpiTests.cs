using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using RibbonKit.Animation;
using RibbonKit.Controls;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Tests;

public class PopupDpiTests
{
    [Fact]
    public void Popup_margin_tracks_dpi_without_changing_the_requested_value_or_rounding_opt_out() => Sta.Run(() =>
    {
        var requested = new Thickness(4, 2, 8, 8);
        var border = new PopupBorder { Margin = requested, UseLayoutRounding = true };
        foreach (double scale in new[] { 2, 1.5, 1.25, 1.75, 1, 2, 1.25 })
        {
            VisualTreeHelper.SetRootDpi(border, new DpiScale(scale, scale));
            Assert.Equal(Math.Round(2 * scale) / scale, border.Margin.Top);
            Assert.Equal(requested, border.ReadLocalValue(FrameworkElement.MarginProperty));
            border.UseLayoutRounding = false;
            Assert.Equal(requested, border.Margin);
            border.UseLayoutRounding = true;
        }
        border.ClipToBounds = true;
        border.Child = new Border { Width = 300, Height = 300 };
        border.Measure(new Size(100, 100));
        border.Arrange(new Rect(0, 0, 100, 100));
        Assert.NotNull(VisualTreeHelper.GetClip(border));
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Popup_margin_and_layout_slot_use_the_same_pixel_grid(bool split) => Sta.Run(() =>
    {
        var application = Sta.UseApplication();
        ThemeManager.Apply(application, RibbonTheme.Office2024);
        RibbonDropDownButton button = split ? new RibbonSplitButton() : new RibbonDropDownButton();
        var owner = new Window { Content = button, Width = 300, Height = 200,
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
        owner.Show();
        Sta.Drain();
        var surface = Assert.IsAssignableFrom<Border>(button.Template.FindName("PART_MenuHost", button));
        ((Popup)button.Template.FindName("PART_Popup", button)).Child = null;
        // Exercise a fractional content demand and rounded popup allocation.
        // This guards geometry/paint; the OS transition remains a live check.
        surface.Child = new Border { Width = 160.5, Height = 182.5, UseLayoutRounding = false };
        var root = new Border
        {
            Width = 182.4, Height = 201.6, UseLayoutRounding = true,
            SnapsToDevicePixels = true, Background = Brushes.White, Child = surface,
        };
        try
        {
            VisualTreeHelper.SetRootDpi(root, new DpiScale(1.25, 1.25));
            root.Measure(new Size(182.4, 201.6));
            root.Arrange(new Rect(0, 0, 182.4, 201.6));
            root.UpdateLayout();
            Assert.Equal(Math.Round(surface.Margin.Top * 1.25), surface.Margin.Top * 1.25);
            var bitmap = new RenderTargetBitmap(228, 252, 120, 120, PixelFormats.Pbgra32);
            bitmap.Render(root);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using (var output = File.Create(Path.Combine(AppContext.BaseDirectory,
                $"popup-shadow-{surface.GetType().Name}-{split}.png"))) encoder.Save(output);
            Assert.Null(VisualTreeHelper.GetClip(surface));
            Point bottom = surface.TransformToAncestor(root).Transform(
                new Point(surface.ActualWidth / 2, surface.ActualHeight));
            var pixel = new byte[4];
            bitmap.CopyPixels(new Int32Rect((int)(bottom.X * 1.25), (int)Math.Ceiling(bottom.Y * 1.25) + 2, 1, 1), pixel, 4, 0);
            Assert.True(pixel[0] < 250, "The shadow must paint below the border, inside the reserved popup space.");
        }
        finally { root.Child = null; owner.Close(); Sta.ResetApplication(); }
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    // This changes WPF's owner DPI, not the OS monitor's DPI. It guards retained
    // layout/headroom; it does not reproduce native display-scale timing.
    public void Reopened_menu_preserves_content_and_shadow_headroom_after_simulated_owner_dpi_changes(bool split) => Sta.Run(() =>
    {
        var application = Sta.UseApplication();
        RibbonAnimationLevel originalMotion = RibbonAnimation.GlobalLevel;
        RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
        RibbonDropDownButton button = split
            ? new RibbonSplitButton { Header = "Paste", Layout = RibbonSplitButtonLayout.Vertical }
            : new RibbonDropDownButton { Header = "Layout" };
        foreach (string label in new[] { "2013 Rail", "2024 Rail", "2010 Glass", "2007 Modern",
            "2007 Classic", "Crystal Sidebar", "Crystal Floating" })
            button.Items.Add(new RibbonMenuItem { Header = label });
        var window = new Window
        {
            Content = new StackPanel { Children = { button } }, Width = 700, Height = 500,
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false,
            UseLayoutRounding = true, SnapsToDevicePixels = true,
        };
        try
        {
            ThemeManager.Apply(application, RibbonTheme.Office2024);
            window.Show();
            Sta.Drain();
            foreach (RibbonTheme theme in new[] { RibbonTheme.Office2024, RibbonTheme.CrystalLight })
            {
                ThemeManager.Apply(application, theme);
                Sta.Drain();
                foreach (double scale in new[] { 1.25, 2, 1.25, 1.5, 2, 1.25, 2, 1.25 })
                {
                    button.IsDropDownOpen = false;
                    Sta.Drain();
                    VisualTreeHelper.SetRootDpi(window, new DpiScale(scale, scale));
                    window.UpdateLayout();
                    button.IsDropDownOpen = true;
                    Sta.Drain();
                    var popup = Assert.IsType<Popup>(button.Template.FindName("PART_Popup", button));
                    var surface = Assert.IsAssignableFrom<Border>(popup.Child);
                    surface.UpdateLayout();
                    Sta.Drain();
                    var source = Assert.IsType<HwndSource>(PresentationSource.FromVisual(surface));
                    Assert.True(GetWindowRect(source.Handle, out NativeRect native));
                    var last = Assert.IsType<RibbonMenuItem>(button.Items[button.Items.Count - 1]);
                    Point top = surface.PointToScreen(new Point());
                    Point bottom = surface.PointToScreen(new Point(surface.ActualWidth, surface.ActualHeight));
                    Point lastBottom = last.PointToScreen(new Point(last.ActualWidth, last.ActualHeight));
                    Assert.True(bottom.Y <= native.Bottom + 1,
                        $"{theme}, split={split}, scale={scale}: surface ends at {bottom.Y}; HWND ends at {native.Bottom}.");
                    Assert.True(lastBottom.Y <= bottom.Y,
                        $"{theme}, split={split}, scale={scale}: last row ends at {lastBottom.Y}; surface ends at {bottom.Y}.");
                    Assert.True(top.Y >= native.Top - 1);
                    double pixelScale = source.CompositionTarget.TransformToDevice.M22;
                    Assert.InRange(native.Bottom - bottom.Y,
                        surface.Margin.Bottom * pixelScale - 1, surface.Margin.Bottom * pixelScale + 1);
                    Assert.True(surface.ActualHeight > 100);
                    Assert.Null(VisualTreeHelper.GetClip(surface));
                    button.IsDropDownOpen = false;
                    Sta.Drain();
                }
            }
        }
        finally
        {
            button.IsDropDownOpen = false;
            window.Close();
            Sta.Drain();
            RibbonAnimation.GlobalLevel = originalMotion;
            Sta.ResetApplication();
        }
    });

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr window, out NativeRect rect);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left, Top, Right, Bottom;
    }
}
