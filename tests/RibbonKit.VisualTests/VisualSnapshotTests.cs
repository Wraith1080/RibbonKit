using System.Globalization;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using RibbonKit.Animation;
using RibbonKit.Controls;
using RibbonKit.Theming;
using Xunit;
using Xunit.Sdk;

namespace RibbonKit.VisualTests;

/// <summary>
/// The smallest end-to-end visual-regression slice: real theme tokens, shared control templates,
/// WPF layout, bitmap rendering, an approved image, and a diagnostic difference image.
/// </summary>
public sealed class VisualSnapshotTests
{
    private const int Width = 760;
    private const int Height = 170;
    private const double Dpi = 96d;

    private static readonly (RibbonTheme Theme, string SnapshotName)[] ThemeSnapshots =
    {
        (RibbonTheme.Office2007, "office2007-default-100"),
        (RibbonTheme.Office2010, "office2010-default-100"),
        (RibbonTheme.Office2013, "office2013-default-100"),
        (RibbonTheme.Office2019, "office2019-default-100"),
        (RibbonTheme.Office2024, "office2024-default-100"),
    };

    [Fact]
    public void Every_theme_at_100_percent_matches_its_approved_snapshot() =>
        SnapshotThread.Run(() =>
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
            RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;

            var application = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
            RibbonAnimation.RespectSystemReduceMotion = false;

            try
            {
                foreach ((RibbonTheme theme, string snapshotName) in ThemeSnapshots)
                {
                    ThemeManager.Apply(application, theme);
                    AssertSnapshot(snapshotName);
                }
            }
            finally
            {
                application.Shutdown();
            }
        });

    private static void AssertSnapshot(string snapshotName)
    {
        BitmapSource actual = RenderScene();
        BitmapSource repeated = RenderScene();

        if (!Pixels(actual).AsSpan().SequenceEqual(Pixels(repeated)))
        {
            throw new XunitException(
                $"The visual scene '{snapshotName}' produced different pixels twice in one process. " +
                "The fixture must be deterministic before its approved image can be trusted.");
        }

        string repositoryRoot = FindRepositoryRoot();
        string sourceBaseline = Path.Combine(
            repositoryRoot,
            "tests",
            "RibbonKit.VisualTests",
            "Snapshots",
            "approved",
            snapshotName + ".png");

        if (ShouldUpdateSnapshots())
        {
            SavePng(actual, sourceBaseline);
            return;
        }

        string outputBaseline = Path.Combine(
            AppContext.BaseDirectory,
            "Snapshots",
            "approved",
            snapshotName + ".png");
        string baseline = File.Exists(outputBaseline) ? outputBaseline : sourceBaseline;

        if (!File.Exists(baseline))
        {
            throw new XunitException(
                $"Approved snapshot not found: {sourceBaseline}{Environment.NewLine}" +
                "Set RIBBONKIT_UPDATE_SNAPSHOTS=1 and rerun this project to create it.");
        }

        BitmapSource expected = LoadPng(baseline);
        SnapshotComparison comparison = Compare(expected, actual);
        if (comparison.Passed)
        {
            return;
        }

        string failureDirectory = Path.Combine(repositoryRoot, "TestResults", "visual");
        string actualPath = Path.Combine(failureDirectory, snapshotName + ".actual.png");
        string differencePath = Path.Combine(failureDirectory, snapshotName + ".diff.png");
        SavePng(actual, actualPath);
        SavePng(CreateDifference(expected, actual), differencePath);

        throw new XunitException(
            $"Visual snapshot '{snapshotName}' changed: {comparison.Message}{Environment.NewLine}" +
            $"Actual: {actualPath}{Environment.NewLine}" +
            $"Diff:   {differencePath}{Environment.NewLine}" +
            "If this change is intentional, review those images and regenerate the approved PNG.");
    }

    private static BitmapSource RenderScene()
    {
        FrameworkElement scene = CreateScene();
        DpiScale effectiveDpi = VisualTreeHelper.GetDpi(scene);
        if (Math.Abs(effectiveDpi.DpiScaleX - 1d) > 0.001
            || Math.Abs(effectiveDpi.DpiScaleY - 1d) > 0.001)
        {
            throw new XunitException(
                "The 100% snapshot row requires WPF's effective display scale to be 100% " +
                $"(96 DPI), but this process reports {effectiveDpi.PixelsPerInchX:F0}×" +
                $"{effectiveDpi.PixelsPerInchY:F0} DPI. Move the test process to a 100% display " +
                "or use the future explicit-DPI harness for higher-scale baselines.");
        }

        var size = new Size(Width, Height);

        scene.Measure(size);
        scene.Arrange(new Rect(size));
        scene.UpdateLayout();
        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        scene.Measure(size);
        scene.Arrange(new Rect(size));
        scene.UpdateLayout();

        var bitmap = new RenderTargetBitmap(Width, Height, Dpi, Dpi, PixelFormats.Pbgra32);
        bitmap.Render(scene);
        bitmap.Freeze();
        return bitmap;
    }

    private static FrameworkElement CreateScene()
    {
        var root = new Grid
        {
            Width = Width,
            Height = Height,
            Background = Brushes.White,
            UseLayoutRounding = true,
            SnapsToDevicePixels = true,
            Language = XmlLanguage.GetLanguage("en-US"),
        };
        TextOptions.SetTextFormattingMode(root, TextFormattingMode.Display);
        TextOptions.SetTextHintingMode(root, TextHintingMode.Fixed);
        TextOptions.SetTextRenderingMode(root, TextRenderingMode.Grayscale);

        var ribbon = new Ribbon
        {
            Width = Width,
            VerticalAlignment = VerticalAlignment.Top,
            ApplicationButtonHeader = "File",
            QuickAccessPosition = RibbonQuickAccessPosition.TabRow,
            UseLayoutRounding = true,
            SnapsToDevicePixels = true,
        };

        ribbon.QuickAccessItems.Add(Button(null, RibbonControlSize.Small, Icon("M2,8 L6,12 L14,3")));
        ribbon.QuickAccessItems.Add(Button(null, RibbonControlSize.Small, Icon("M3,3 L13,3 L13,13 L3,13 Z")));

        var home = new RibbonTab { Header = "Home" };
        var insert = new RibbonTab { Header = "Insert" };

        var clipboard = new RibbonGroup { Header = "Clipboard" };
        clipboard.Items.Add(Button("Paste", RibbonControlSize.Large, Icon("M4,2 L12,2 L12,5 L14,5 L14,15 L2,15 L2,5 L4,5 Z")));
        clipboard.Items.Add(new StackPanel
        {
            Children =
            {
                Button("Cut", RibbonControlSize.Medium, Icon("M3,3 L13,13 M13,3 L3,13")),
                Button("Copy", RibbonControlSize.Medium, Icon("M3,2 L11,2 L11,5 L14,5 L14,14 L6,14 L6,11 L3,11 Z")),
                Button("Format", RibbonControlSize.Medium, Icon("M3,12 L12,3 L14,5 L5,14 Z")),
            },
        });

        var editing = new RibbonGroup { Header = "Editing" };
        editing.Items.Add(new StackPanel
        {
            Children =
            {
                Button("Find", RibbonControlSize.Medium, Icon("M7,2 A5,5 0 1 1 6.99,2 M11,11 L15,15")),
                Button("Replace", RibbonControlSize.Medium, Icon("M2,5 L12,5 L9,2 M12,11 L2,11 L5,14")),
                Button(null, RibbonControlSize.Small, Icon("M2,4 L14,4 M2,8 L11,8 M2,12 L8,12")),
            },
        });

        home.Groups.Add(clipboard);
        home.Groups.Add(editing);
        insert.Groups.Add(new RibbonGroup { Header = "Illustrations" });
        ribbon.Tabs.Add(home);
        ribbon.Tabs.Add(insert);
        ribbon.SelectedTab = home;

        root.Children.Add(ribbon);
        return root;
    }

    private static RibbonButton Button(string? header, RibbonControlSize size, ImageSource icon) =>
        new()
        {
            Header = header,
            Size = size,
            Icon = icon,
            LargeIcon = icon,
            UseLayoutRounding = true,
            SnapsToDevicePixels = true,
        };

    private static ImageSource Icon(string geometryData)
    {
        var geometry = Geometry.Parse(geometryData);
        var pen = new Pen(new SolidColorBrush(Color.FromRgb(0x44, 0x54, 0x6A)), 1.35)
        {
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round,
            LineJoin = PenLineJoin.Round,
        };
        var drawing = new GeometryDrawing(null, pen, geometry);
        var image = new DrawingImage(drawing);
        image.Freeze();
        return image;
    }

    private static SnapshotComparison Compare(BitmapSource expected, BitmapSource actual)
    {
        if (expected.PixelWidth != actual.PixelWidth || expected.PixelHeight != actual.PixelHeight)
        {
            return new SnapshotComparison(
                false,
                $"expected {expected.PixelWidth}x{expected.PixelHeight}, " +
                $"rendered {actual.PixelWidth}x{actual.PixelHeight}");
        }

        byte[] expectedPixels = Pixels(expected);
        byte[] actualPixels = Pixels(actual);
        long absoluteDifference = 0;
        int significantPixels = 0;
        int pixelCount = expected.PixelWidth * expected.PixelHeight;

        for (int offset = 0; offset < expectedPixels.Length; offset += 4)
        {
            int greatestChannelDifference = 0;
            for (int channel = 0; channel < 4; channel++)
            {
                int difference = Math.Abs(expectedPixels[offset + channel] - actualPixels[offset + channel]);
                absoluteDifference += difference;
                greatestChannelDifference = Math.Max(greatestChannelDifference, difference);
            }

            if (greatestChannelDifference > 8)
            {
                significantPixels++;
            }
        }

        double significantRatio = (double)significantPixels / pixelCount;
        double meanChannelDifference = (double)absoluteDifference / (pixelCount * 4);
        bool passed = significantRatio <= 0.001 && meanChannelDifference <= 0.05;
        return new SnapshotComparison(
            passed,
            $"{significantPixels:N0} significant pixels ({significantRatio:P3}); " +
            $"mean channel difference {meanChannelDifference:F4}");
    }

    private static BitmapSource CreateDifference(BitmapSource expected, BitmapSource actual)
    {
        if (expected.PixelWidth != actual.PixelWidth || expected.PixelHeight != actual.PixelHeight)
        {
            return actual;
        }

        byte[] left = Pixels(expected);
        byte[] right = Pixels(actual);
        byte[] difference = new byte[left.Length];
        for (int offset = 0; offset < left.Length; offset += 4)
        {
            for (int channel = 0; channel < 3; channel++)
            {
                difference[offset + channel] = (byte)Math.Min(
                    byte.MaxValue,
                    Math.Abs(left[offset + channel] - right[offset + channel]) * 6);
            }

            difference[offset + 3] = byte.MaxValue;
        }

        BitmapSource bitmap = BitmapSource.Create(
            expected.PixelWidth,
            expected.PixelHeight,
            Dpi,
            Dpi,
            PixelFormats.Bgra32,
            null,
            difference,
            expected.PixelWidth * 4);
        bitmap.Freeze();
        return bitmap;
    }

    private static BitmapSource LoadPng(string path)
    {
        using var stream = File.OpenRead(path);
        BitmapFrame frame = BitmapFrame.Create(
            stream,
            BitmapCreateOptions.PreservePixelFormat,
            BitmapCacheOption.OnLoad);
        var converted = new FormatConvertedBitmap(frame, PixelFormats.Pbgra32, null, 0d);
        converted.Freeze();
        return converted;
    }

    private static void SavePng(BitmapSource bitmap, string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(path);
        encoder.Save(stream);
    }

    private static byte[] Pixels(BitmapSource bitmap)
    {
        int stride = bitmap.PixelWidth * 4;
        var pixels = new byte[stride * bitmap.PixelHeight];
        bitmap.CopyPixels(pixels, stride, 0);
        return pixels;
    }

    private static bool ShouldUpdateSnapshots()
    {
        string? value = Environment.GetEnvironmentVariable("RIBBONKIT_UPDATE_SNAPSHOTS");
        return string.Equals(value, "1", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
    }

    private static string FindRepositoryRoot()
    {
        for (DirectoryInfo? directory = new(AppContext.BaseDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "RibbonKit.sln")))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException(
            $"Could not find RibbonKit.sln above {AppContext.BaseDirectory}.");
    }

    private sealed record SnapshotComparison(bool Passed, string Message);

    private static class SnapshotThread
    {
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

        public static void Run(Action body)
        {
            ExceptionDispatchInfo? failure = null;
            var thread = new Thread(() =>
            {
                try
                {
                    body();
                }
                catch (Exception exception)
                {
                    failure = ExceptionDispatchInfo.Capture(exception);
                }
                finally
                {
                    Dispatcher.CurrentDispatcher.InvokeShutdown();
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.IsBackground = true;
            thread.Start();

            if (!thread.Join(Timeout))
            {
                throw new TimeoutException(
                    $"The visual snapshot did not finish within {Timeout.TotalSeconds:0} seconds.");
            }

            failure?.Throw();
        }
    }
}
