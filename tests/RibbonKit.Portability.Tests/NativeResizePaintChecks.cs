using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using RibbonKit.Controls;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Portability.Tests;

internal static class NativeResizePaintChecks
{
    internal static void Verify(Application application)
    {
        foreach (var theme in new[] { RibbonTheme.Office2024Flat, RibbonTheme.CrystalLight })
        foreach (var placement in Enum.GetValues<RibbonQuickAccessPosition>())
        foreach (var density in new[] { RibbonDensity.Compact, RibbonDensity.Touch })
        {
            ThemeManager.Apply(application, theme);
            var ribbon = new Ribbon { QuickAccessPosition = placement, Density = density };
            var tab = new RibbonTab { Header = "Home" }; var group = new RibbonGroup { Header = "Resize paint" };
            var icon = new DrawingImage(new GeometryDrawing(Brushes.Blue, null, new RectangleGeometry(new Rect(0, 0, 16, 16))));
            var button = new RibbonButton { Header = "Live command", Icon = icon, LargeIcon = icon };
            group.Items.Add(button); tab.Groups.Add(group); ribbon.Tabs.Add(tab); ribbon.SelectedTab = tab;
            ribbon.QuickAccessItems.Add(new RibbonButton { Header = "Save", Icon = icon, Size = RibbonControlSize.Small });
            var editor = new TextBox { Text = "Host text remains live while ribbon paint is reused." };
            var host = new DockPanel(); DockPanel.SetDock(ribbon, Dock.Top); host.Children.Add(ribbon); host.Children.Add(editor);
            var window = new RibbonWindow { Content = host, Title = "Resize paint witness", Width = 800, Height = 340,
                Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false, UseLayoutRounding = true };
            try
            {
                window.Show(); Layout();
                string name = $"{theme}-{placement}-{density}";
                Save(window, name + "-before");
                IntPtr hwnd = new WindowInteropHelper(window).Handle;
                SendMessage(hwnd, 0x0231, IntPtr.Zero, IntPtr.Zero);
                Assert.IsType<BitmapCache>(ribbon.CacheMode);
                if (placement == RibbonQuickAccessPosition.TitleBar)
                    Assert.IsType<BitmapCache>(Assert.IsType<RibbonQuickAccessToolBar>(window.TitleBarContent).CacheMode);
                Assert.Null(editor.CacheMode);
                window.Width += 24; window.Height += 9; Layout();
                int clicked = 0; button.Click += (_, _) => clicked++;
                button.RaiseEvent(new RoutedEventArgs(RibbonButton.ClickEvent, button));
                Assert.Equal(1, clicked);
                button.Header = "Updated while cached"; Layout();
                Save(window, name + "-during");
                SendMessage(hwnd, 0x0232, IntPtr.Zero, IntPtr.Zero);
                Assert.Null(ribbon.CacheMode);
                if (placement == RibbonQuickAccessPosition.TitleBar)
                    Assert.Null(Assert.IsType<RibbonQuickAccessToolBar>(window.TitleBarContent).CacheMode);
                Layout(); Save(window, name + "-after");

                void Layout()
                {
                    window.UpdateLayout(); Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle); window.UpdateLayout();
                }
            }
            finally { window.Close(); }
        }
    }

    private static void Save(RibbonWindow window, string name)
    {
        string? folder = Environment.GetEnvironmentVariable("RIBBONKIT_RESIZE_PAINT_DIAGNOSTICS");
        if (string.IsNullOrEmpty(folder)) return;
        Directory.CreateDirectory(folder);
        var frame = (FrameworkElement)window.Template.FindName("PhysicalWindowFrameHost", window);
        var dpi = VisualTreeHelper.GetDpi(frame);
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(frame.ActualWidth * dpi.DpiScaleX),
            (int)Math.Ceiling(frame.ActualHeight * dpi.DpiScaleY), dpi.PixelsPerInchX, dpi.PixelsPerInchY, PixelFormats.Pbgra32);
        bitmap.Render(frame);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(Path.Combine(folder, name + ".png")); encoder.Save(stream);
    }

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hwnd, uint message, IntPtr wparam, IntPtr lparam);
}
