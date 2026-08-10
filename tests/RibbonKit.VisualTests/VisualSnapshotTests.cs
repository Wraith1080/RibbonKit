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
    private const double BaseDpi = 96d;

    private static readonly (RibbonTheme Theme, bool Dark, string Name)[] Themes =
    {
        (RibbonTheme.Office2007, false, "office2007-default"),
        (RibbonTheme.Office2010, false, "office2010-default"),
        (RibbonTheme.Office2013, false, "office2013-default"),
        (RibbonTheme.Office2019, false, "office2019-default"),
        (RibbonTheme.Office2024, false, "office2024-default"),
        (RibbonTheme.Office2007, true, "office2007-dark"),
        (RibbonTheme.Office2010, true, "office2010-dark"),
        (RibbonTheme.Office2013, true, "office2013-dark"),
        (RibbonTheme.Office2019, true, "office2019-dark"),
        (RibbonTheme.Office2024, true, "office2024-dark"),
    };

    private static readonly (int Percent, double Scale)[] DpiScales =
    {
        (100, 1d),
        (125, 1.25d),
        (150, 1.5d),
        (200, 2d),
    };

    [Fact]
    public void Every_theme_variant_and_dpi_matches_its_approved_snapshot() =>
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
                Assert.All(
                    Enum.GetValues<RibbonTheme>(),
                    theme => Assert.True(ThemeManager.SupportsDarkMode(theme)));
                AssertDarkBackdropRoundTrip(application);

                foreach ((RibbonTheme theme, bool dark, string name) in Themes)
                {
                    ThemeManager.Apply(application, theme);
                    ThemeManager.SetDarkMode(application, dark);

                    foreach ((int percent, double scale) in DpiScales)
                    {
                        AssertSnapshot($"{name}-{percent}", scale);
                    }
                }

                // Office 2010 has generation-specific glass states that the neutral matrix scene
                // cannot expose: its File button is hidden without Backstage/ApplicationMenu, and
                // an ordinary RibbonButton cannot be forced into the read-only IsMouseOver state
                // in this disconnected visual tree. An open File button plus a checked toggle
                // exercise the same state gradients deterministically at the smallest useful DPI.
                ThemeManager.Apply(application, RibbonTheme.Office2010);
                ThemeManager.SetDarkMode(application, false);
                AssertSnapshot(
                    "office2010-button-states-100",
                    1d,
                    FlowDirection.LeftToRight,
                    CreateOffice2010ButtonStateScene);
                AssertSnapshot(
                    "office2010-backstage-shell-100",
                    1d,
                    FlowDirection.LeftToRight,
                    CreateOffice2010BackstageShellScene);
                AssertSnapshot(
                    "office2010-message-bar-connected-100",
                    1d,
                    FlowDirection.LeftToRight,
                    CreateMessageBarStackScene,
                    270);

                ThemeManager.Apply(application, RibbonTheme.Office2007);
                ThemeManager.SetDarkMode(application, false);
                AssertSnapshot(
                    "office2007-backstage-shell-100",
                    1d,
                    FlowDirection.LeftToRight,
                    CreateOffice2010BackstageShellScene);
                AssertSnapshot(
                    "office2007-message-bar-connected-100",
                    1d,
                    FlowDirection.LeftToRight,
                    CreateMessageBarStackScene,
                    270);
                AssertSnapshot(
                    "office2007-orb-application-menu-message-bar-100",
                    1d,
                    FlowDirection.LeftToRight,
                    CreateOrbApplicationMenuMessageBarScene,
                    360);
                AssertApplicationButtonMarginResourceSurvivesOrbRoundTrip();

                ThemeManager.Apply(application, RibbonTheme.Office2024);
                ThemeManager.SetDarkMode(application, false);
                AssertSnapshot(
                    "office2024-input-controls-100",
                    1d,
                    FlowDirection.LeftToRight,
                    CreateInputControlsScene);
                AssertSnapshot(
                    "office2024-rtl-input-controls-100",
                    1d,
                    FlowDirection.RightToLeft,
                    CreateInputControlsScene);
                ThemeManager.SetDarkMode(application, true);
                AssertSnapshot(
                    "office2024-dark-input-controls-100",
                    1d,
                    FlowDirection.LeftToRight,
                    CreateInputControlsScene);
                ThemeManager.SetDarkMode(application, false);
                AssertSnapshot(
                    "office2024-backstage-shell-100",
                    1d,
                    FlowDirection.LeftToRight,
                    CreateModernBackstageShellScene);
                AssertSnapshot(
                    "office2024-message-bar-stack-100",
                    1d,
                    FlowDirection.LeftToRight,
                    CreateMessageBarStackScene,
                    270);
                AssertSnapshot(
                    "office2024-application-menu-message-bar-100",
                    1d,
                    FlowDirection.LeftToRight,
                    CreateApplicationMenuMessageBarScene,
                    360);

                ThemeManager.Apply(application, RibbonTheme.Office2010);
                ThemeManager.SetAccent(application, Color.FromRgb(0x0B, 0x8A, 0x4A));
                ThemeManager.SetAccentedTitleBar(application, true);
                AssertSnapshot(
                    "office2010-colored-titlebar-100",
                    1d,
                    FlowDirection.LeftToRight,
                    CreateColoredTitleBarScene);
                ThemeManager.SetAccentedTitleBar(application, false);
                ThemeManager.ClearAccent(application);

                ThemeManager.Apply(application, RibbonTheme.Office2007);
                ThemeManager.SetDarkMode(application, true);
                AssertSnapshot(
                    "office2007-dark-application-menu-100",
                    1d,
                    FlowDirection.LeftToRight,
                    CreateApplicationMenuScene);
                AssertSnapshot(
                    "office2007-dark-backstage-shell-100",
                    1d,
                    FlowDirection.LeftToRight,
                    CreateOffice2010BackstageShellScene);

                ThemeManager.Apply(application, RibbonTheme.Office2010);
                ThemeManager.SetDarkMode(application, true);
                AssertSnapshot(
                    "office2010-dark-application-menu-100",
                    1d,
                    FlowDirection.LeftToRight,
                    CreateApplicationMenuScene);
                AssertSnapshot(
                    "office2010-dark-backstage-shell-100",
                    1d,
                    FlowDirection.LeftToRight,
                    CreateOffice2010BackstageShellScene);

                ThemeManager.Apply(application, RibbonTheme.Office2013);
                ThemeManager.SetDarkMode(application, true);
                AssertSnapshot(
                    "office2013-dark-classic-backstage-100",
                    1d,
                    FlowDirection.LeftToRight,
                    CreateClassicBackstageShellScene);

                // Smallest deterministic RTL slice: isolate FlowDirection from localization and
                // DPI variables while exercising the same real tokens/templates as the matrix.
                ThemeManager.Apply(application, RibbonTheme.Office2024);
                ThemeManager.SetDarkMode(application, false);
                AssertSnapshot("office2024-rtl-100", 1d, FlowDirection.RightToLeft);
                AssertSnapshot(
                    "office2024-rtl-qat-customize-100",
                    1d,
                    FlowDirection.RightToLeft,
                    CreateQuickAccessCustomizeScene);
                AssertSnapshot(
                    "office2024-rtl-bidirectional-backstage-100",
                    1d,
                    FlowDirection.RightToLeft,
                    CreateBidirectionalBackstageScene);
                AssertSnapshot(
                    "office2024-rtl-message-bar-stack-100",
                    1d,
                    FlowDirection.RightToLeft,
                    CreateMessageBarStackScene,
                    270);
            }
            finally
            {
                ThemeManager.SetDarkMode(application, false);
                application.Shutdown();
            }
        });

    private static void AssertDarkBackdropRoundTrip(Application application)
    {
        ThemeManager.Apply(application, RibbonTheme.Office2024);
        ThemeManager.SetDarkMode(application, true);
        Assert.Equal(Color.FromRgb(0x18, 0x18, 0x18), ResourceColor(application, "RibbonKit.Brushes.Ribbon.Background"));

        ThemeManager.SetTitleBarBackdrop(application, true);
        Assert.Equal(Colors.Transparent, ResourceColor(application, "RibbonKit.Brushes.Ribbon.Background"));

        ThemeManager.SetTitleBarBackdrop(application, false);
        Assert.Equal(Color.FromRgb(0x18, 0x18, 0x18), ResourceColor(application, "RibbonKit.Brushes.Ribbon.Background"));

        ThemeManager.SetDarkMode(application, false);
    }

    private static Color ResourceColor(Application application, string key) =>
        Assert.IsType<SolidColorBrush>(application.TryFindResource(key)).Color;

    private static void AssertSnapshot(
        string snapshotName,
        double dpiScale,
        FlowDirection flowDirection = FlowDirection.LeftToRight,
        Func<FlowDirection, FrameworkElement>? sceneFactory = null,
        int height = Height)
    {
        BitmapSource actual = RenderScene(dpiScale, flowDirection, sceneFactory, height);
        BitmapSource repeated = RenderScene(dpiScale, flowDirection, sceneFactory, height);

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

    private static BitmapSource RenderScene(
        double dpiScale,
        FlowDirection flowDirection,
        Func<FlowDirection, FrameworkElement>? sceneFactory,
        int height)
    {
        FrameworkElement scene = (sceneFactory ?? CreateScene)(flowDirection);
        var requestedDpi = new DpiScale(dpiScale, dpiScale);
        VisualTreeHelper.SetRootDpi(scene, requestedDpi);
        DpiScale effectiveDpi = VisualTreeHelper.GetDpi(scene);
        if (Math.Abs(effectiveDpi.DpiScaleX - dpiScale) > 0.001
            || Math.Abs(effectiveDpi.DpiScaleY - dpiScale) > 0.001)
        {
            throw new XunitException(
                $"The snapshot requested {BaseDpi * dpiScale:F0} DPI, but WPF assigned " +
                $"{effectiveDpi.PixelsPerInchX:F0}×{effectiveDpi.PixelsPerInchY:F0} DPI " +
                "to its visual root.");
        }

        var size = new Size(Width, height);

        scene.Measure(size);
        scene.Arrange(new Rect(size));
        scene.UpdateLayout();
        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        scene.Measure(size);
        scene.Arrange(new Rect(size));
        scene.UpdateLayout();

        int pixelWidth = checked((int)Math.Round(Width * dpiScale, MidpointRounding.AwayFromZero));
        int pixelHeight = checked((int)Math.Round(height * dpiScale, MidpointRounding.AwayFromZero));
        double dpi = BaseDpi * dpiScale;
        var bitmap = new RenderTargetBitmap(pixelWidth, pixelHeight, dpi, dpi, PixelFormats.Pbgra32);
        bitmap.Render(scene);
        bitmap.Freeze();
        return bitmap;
    }

    private static FrameworkElement CreateScene(FlowDirection flowDirection)
    {
        var root = new Grid
        {
            Width = Width,
            Height = Height,
            UseLayoutRounding = true,
            SnapsToDevicePixels = true,
            Language = XmlLanguage.GetLanguage("en-US"),
            FlowDirection = flowDirection,
        };
        TextOptions.SetTextFormattingMode(root, TextFormattingMode.Display);
        TextOptions.SetTextHintingMode(root, TextHintingMode.Fixed);
        TextOptions.SetTextRenderingMode(root, TextRenderingMode.Grayscale);
        root.SetResourceReference(
            Panel.BackgroundProperty,
            "RibbonKit.Brushes.Window.Background");

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

    private static FrameworkElement CreateOffice2010ButtonStateScene(FlowDirection flowDirection)
    {
        var root = (Grid)CreateScene(flowDirection);
        Assert.Single(root.Children);
        var ribbon = Assert.IsType<Ribbon>(root.Children[0]);
        var home = Assert.IsType<RibbonTab>(ribbon.Tabs[0]);

        var stateGroup = new RibbonGroup { Header = "States" };
        stateGroup.Items.Add(new RibbonToggleButton
        {
            Header = "Checked",
            Size = RibbonControlSize.Large,
            Icon = Icon("M3,8 L7,12 L14,3"),
            LargeIcon = Icon("M3,8 L7,12 L14,3"),
            IsChecked = true,
            UseLayoutRounding = true,
            SnapsToDevicePixels = true,
        });
        stateGroup.Items.Add(new RibbonDropDownButton
        {
            Header = "Drop",
            Size = RibbonControlSize.Large,
            Icon = Icon("M3,5 L8,10 L13,5"),
            LargeIcon = Icon("M3,5 L8,10 L13,5"),
            IsDropDownOpen = true,
            UseLayoutRounding = true,
            SnapsToDevicePixels = true,
        });
        stateGroup.Items.Add(new RibbonSplitButton
        {
            Header = "Split",
            Size = RibbonControlSize.Large,
            Icon = Icon("M3,3 L13,3 L13,13 L3,13 Z"),
            LargeIcon = Icon("M3,3 L13,3 L13,13 L3,13 Z"),
            IsDropDownOpen = true,
            UseLayoutRounding = true,
            SnapsToDevicePixels = true,
        });
        home.Groups.Add(stateGroup);

        var backstage = new Backstage { Design = RibbonBackstageDesign.Classic2010 };
        backstage.Items.Add(new BackstageTabItem { Header = "Info" });
        ribbon.Backstage = backstage;
        ribbon.IsBackstageOpen = true;
        return root;
    }

    private static FrameworkElement CreateInputControlsScene(FlowDirection flowDirection)
    {
        var root = (Grid)CreateScene(flowDirection);
        var ribbon = Assert.IsType<Ribbon>(Assert.Single(root.Children));
        var home = Assert.IsType<RibbonTab>(ribbon.Tabs[0]);

        var checks = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            Children =
            {
                new RibbonCheckBox { Header = "Checked", IsChecked = true },
                new RibbonCheckBox { Header = "Unchecked" },
                new RibbonCheckBox { Header = "Indeterminate", IsThreeState = true, IsChecked = null },
            },
        };
        var radios = new StackPanel
        {
            Margin = new Thickness(12, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Children =
            {
                new RibbonRadioButton { Header = "Selected", GroupName = "SnapshotDensity", IsChecked = true },
                new RibbonRadioButton { Header = "Unselected", GroupName = "SnapshotDensity" },
                new RibbonRadioButton { Header = "Disabled", GroupName = "SnapshotDensity", IsEnabled = false },
            },
        };
        var textBoxes = new StackPanel
        {
            Margin = new Thickness(12, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Children =
            {
                new RibbonTextBox { Header = "Editable", InputWidth = 76, Text = "RibbonKit" },
                new RibbonTextBox { Header = "Read only", InputWidth = 76, Text = "Select me", IsReadOnly = true },
                new RibbonTextBox { Header = "Disabled", InputWidth = 76, Text = "Unavailable", IsEnabled = false },
            },
        };
        var options = new RibbonGroup { Header = "Inputs" };
        options.Items.Add(new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center,
            Children = { checks, radios, textBoxes },
        });
        home.Groups.Add(options);
        return root;
    }

    private static FrameworkElement CreateQuickAccessCustomizeScene(FlowDirection flowDirection)
    {
        var root = (Grid)CreateScene(flowDirection);
        var ribbon = Assert.IsType<Ribbon>(Assert.Single(root.Children));
        root.Children.Clear();

        root.Children.Add(new RibbonQuickAccessPage
        {
            Ribbon = ribbon,
            Margin = new Thickness(8),
            UseLayoutRounding = true,
            SnapsToDevicePixels = true,
        });
        return root;
    }

    private static FrameworkElement CreateBidirectionalBackstageScene(FlowDirection flowDirection)
    {
        var root = (Grid)CreateScene(flowDirection);
        root.Children.Clear();
        root.Language = XmlLanguage.GetLanguage("ar-SA");

        var content = new StackPanel();
        content.Children.Add(new TextBlock
        {
            Text = "تقرير الإصدار — RibbonKit 2026",
            FontSize = 22,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 10),
        });
        content.Children.Add(new TextBlock
        {
            Text = "تقرير ربع سنوي — Quarterly report — ٢٠٢٦",
            FontSize = 14,
            Margin = new Thickness(0, 0, 0, 8),
        });
        content.Children.Add(new TextBlock
        {
            Text = "Report-2026-Q3.docx",
            FlowDirection = FlowDirection.LeftToRight,
            TextAlignment = TextAlignment.Left,
            FontFamily = new FontFamily("Consolas"),
        });

        var backstage = new Backstage
        {
            Design = RibbonBackstageDesign.Modern,
            UseLayoutRounding = true,
            SnapsToDevicePixels = true,
        };
        backstage.Items.Add(new BackstageTabItem
        {
            Header = "معلومات — Info",
            Content = content,
        });
        backstage.Items.Add(new BackstageTabItem { Header = "Recent — الأخيرة" });
        backstage.Items.Add(new BackstageTabItem { Header = "خيارات — Options" });
        backstage.SelectedIndex = 0;

        root.Children.Add(backstage);
        return root;
    }

    private static FrameworkElement CreateOffice2010BackstageShellScene(FlowDirection flowDirection)
    {
        var root = (Grid)CreateScene(flowDirection);
        root.Children.Clear();

        var backstage = new Backstage
        {
            Design = RibbonBackstageDesign.Classic2010,
            UseLayoutRounding = true,
            SnapsToDevicePixels = true,
        };
        backstage.Items.Add(new BackstageTabItem
        {
            Header = "Info",
            Content = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                Children =
                {
                    new TextBlock
                    {
                        Text = "Information",
                        FontSize = 24,
                        FontWeight = FontWeights.Light,
                        Margin = new Thickness(0, 0, 0, 10),
                    },
                    new TextBlock
                    {
                        Text = "RibbonKit document",
                        FontSize = 14,
                        FontWeight = FontWeights.SemiBold,
                    },
                },
            },
        });
        backstage.Items.Add(new BackstageTabItem { Header = "Recent" });
        backstage.Items.Add(new BackstageTabItem { Header = "New" });
        backstage.SelectedIndex = 0;

        root.Children.Add(backstage);
        return root;
    }

    private static FrameworkElement CreateClassicBackstageShellScene(FlowDirection flowDirection)
    {
        var root = (Grid)CreateScene(flowDirection);
        root.Children.Clear();

        var backstage = new Backstage
        {
            Design = RibbonBackstageDesign.Classic,
            UseLayoutRounding = true,
            SnapsToDevicePixels = true,
        };
        backstage.Items.Add(new BackstageTabItem
        {
            Header = "Info",
            Content = new TextBlock
            {
                Text = "Dark Gray Backstage",
                FontSize = 24,
                FontWeight = FontWeights.Light,
            },
        });
        backstage.Items.Add(new BackstageTabItem { Header = "New" });
        backstage.Items.Add(new BackstageTabItem { Header = "Open" });
        backstage.SelectedIndex = 0;
        root.Children.Add(backstage);
        return root;
    }

    private static FrameworkElement CreateModernBackstageShellScene(FlowDirection flowDirection)
    {
        var root = (Grid)CreateScene(flowDirection);
        root.Children.Clear();

        var backstage = new Backstage
        {
            Design = RibbonBackstageDesign.Modern,
            UseLayoutRounding = true,
            SnapsToDevicePixels = true,
        };
        backstage.Items.Add(new BackstageTabItem
        {
            Header = "Home",
            Content = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                Children =
                {
                    new TextBlock
                    {
                        Text = "Good morning",
                        FontSize = 24,
                        FontWeight = FontWeights.Light,
                        Margin = new Thickness(0, 0, 0, 10),
                    },
                    new TextBlock
                    {
                        Text = "New document",
                        FontSize = 14,
                        FontWeight = FontWeights.SemiBold,
                    },
                },
            },
        });
        backstage.Items.Add(new BackstageTabItem { Header = "New" });
        backstage.Items.Add(new BackstageTabItem { Header = "Open" });
        backstage.SelectedIndex = 0;

        root.Children.Add(backstage);
        return root;
    }

    private static FrameworkElement CreateColoredTitleBarScene(FlowDirection flowDirection)
    {
        var root = (Grid)CreateScene(flowDirection);
        root.Children.Clear();

        var titleBar = new Border
        {
            Height = 44,
            VerticalAlignment = VerticalAlignment.Top,
            UseLayoutRounding = true,
            SnapsToDevicePixels = true,
            Child = new TextBlock
            {
                Text = "RibbonKit Showcase",
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            },
        };
        titleBar.SetResourceReference(
            Border.BackgroundProperty,
            "RibbonKit.Brushes.TitleBar.Background");
        root.Children.Add(titleBar);
        return root;
    }

    private static FrameworkElement CreateMessageBarStackScene(FlowDirection flowDirection)
    {
        var root = (Grid)CreateScene(flowDirection);
        root.Children.Clear();
        root.Height = 270;

        var messageBar = new RibbonMessageBar
        {
            UseLayoutRounding = true,
            SnapsToDevicePixels = true,
        };
        messageBar.Items.Add(new RibbonMessage
        {
            Title = "PROTECTED VIEW",
            Message = "Files from the Internet can contain viruses. It is safer to stay in Protected View.",
            ActionContent = "Enable Editing",
        });
        messageBar.Items.Add(new RibbonMessage
        {
            Title = "SECURITY NOTICE",
            Message = "Macros have been disabled in this document.",
            ActionContent = "Review Settings",
        });

        var ribbon = new Ribbon
        {
            MessageBar = messageBar,
            QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon,
            VerticalAlignment = VerticalAlignment.Top,
            UseLayoutRounding = true,
            SnapsToDevicePixels = true,
        };
        var home = new RibbonTab { Header = "Home" };
        var clipboard = new RibbonGroup { Header = "Clipboard" };
        clipboard.Items.Add(new RibbonButton { Header = "Paste", Size = RibbonControlSize.Large });
        clipboard.Items.Add(new RibbonButton { Header = "Copy", Size = RibbonControlSize.Small });
        home.Groups.Add(clipboard);
        ribbon.Tabs.Add(home);
        ribbon.Tabs.Add(new RibbonTab { Header = "Insert" });
        ribbon.QuickAccessItems.Add(new RibbonButton { Header = "Save", Size = RibbonControlSize.Small });
        ribbon.QuickAccessItems.Add(new RibbonButton { Header = "Undo", Size = RibbonControlSize.Small });

        root.Children.Add(ribbon);
        return root;
    }

    private static FrameworkElement CreateApplicationMenuScene(FlowDirection flowDirection)
    {
        var root = (Grid)CreateScene(flowDirection);
        root.Children.Clear();

        root.Children.Add(CreateApplicationMenu());
        return root;
    }

    private static FrameworkElement CreateApplicationMenuMessageBarScene(FlowDirection flowDirection)
        => CreateApplicationMenuMessageBarScene(
            flowDirection,
            RibbonApplicationButtonShape.Tab);

    private static FrameworkElement CreateOrbApplicationMenuMessageBarScene(
        FlowDirection flowDirection)
        => CreateApplicationMenuMessageBarScene(
            flowDirection,
            RibbonApplicationButtonShape.Orb);

    private static void AssertApplicationButtonMarginResourceSurvivesOrbRoundTrip()
    {
        var root = (Grid)CreateApplicationMenuMessageBarScene(
            FlowDirection.LeftToRight,
            RibbonApplicationButtonShape.Orb);
        var ribbon = Assert.IsType<Ribbon>(Assert.Single(root.Children));
        var size = new Size(Width, 360);

        // Realize the open 2007 menu first so the real orb takes the outer-host path. Closing it
        // must preserve the button's DynamicResource expression: Office 2019/2024 both replace
        // this 2007 margin with 8,4,2,0 during a live theme switch.
        root.Measure(size);
        root.Arrange(new Rect(size));
        root.UpdateLayout();
        var button = Assert.IsAssignableFrom<FrameworkElement>(
            FindDescendantByName(root, "PART_ApplicationButton"));
        Assert.Equal(new Thickness(2d, 2d, 2d, 0d), button.Margin);

        ribbon.SetCurrentValue(Ribbon.IsBackstageOpenProperty, false);
        ribbon.ApplicationButtonShape = RibbonApplicationButtonShape.Tab;
        var modernMargin = new Thickness(8d, 4d, 2d, 0d);
        ribbon.Resources["RibbonKit.Metrics.ApplicationButtonMargin"] = modernMargin;
        root.Measure(size);
        root.Arrange(new Rect(size));
        root.UpdateLayout();

        Assert.Equal(modernMargin, button.Margin);
    }

    private static FrameworkElement CreateApplicationMenuMessageBarScene(
        FlowDirection flowDirection,
        RibbonApplicationButtonShape applicationButtonShape)
    {
        var root = (Grid)CreateScene(flowDirection);
        root.Children.Clear();
        root.Height = 360;

        var messageBar = new RibbonMessageBar
        {
            UseLayoutRounding = true,
            SnapsToDevicePixels = true,
        };
        messageBar.Items.Add(new RibbonMessage
        {
            Title = "PROTECTED VIEW",
            Message = "Files from the Internet can contain viruses.",
            ActionContent = "Enable Editing",
        });

        RibbonApplicationMenu menu = CreateApplicationMenu();
        menu.Height = 300;

        var ribbon = new Ribbon
        {
            ApplicationMenu = menu,
            ApplicationButtonShape = applicationButtonShape,
            MessageBar = messageBar,
            QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon,
            IsBackstageOpen = true,
            VerticalAlignment = VerticalAlignment.Top,
            UseLayoutRounding = true,
            SnapsToDevicePixels = true,
        };
        var home = new RibbonTab { Header = "Home" };
        var clipboard = new RibbonGroup { Header = "Clipboard" };
        clipboard.Items.Add(new RibbonButton { Header = "Paste", Size = RibbonControlSize.Large });
        clipboard.Items.Add(new RibbonButton { Header = "Copy", Size = RibbonControlSize.Small });
        home.Groups.Add(clipboard);
        ribbon.Tabs.Add(home);
        ribbon.Tabs.Add(new RibbonTab { Header = "Insert" });
        ribbon.QuickAccessItems.Add(new RibbonButton { Header = "Save", Size = RibbonControlSize.Small });
        ribbon.QuickAccessItems.Add(new RibbonButton { Header = "Undo", Size = RibbonControlSize.Small });

        root.Children.Add(ribbon);
        return root;
    }

    private static RibbonApplicationMenu CreateApplicationMenu()
    {

        var heading = new TextBlock
        {
            Text = "Recent Documents",
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(2, 0, 0, 5),
        };
        heading.SetResourceReference(
            TextBlock.ForegroundProperty,
            "RibbonKit.Brushes.ApplicationMenu.HeadingForeground");

        var defaultPage = new StackPanel();
        defaultPage.Children.Add(heading);
        defaultPage.Children.Add(new RibbonApplicationMenuPaneItem { Content = "1  Quarterly report.docx" });
        defaultPage.Children.Add(new RibbonApplicationMenuPaneItem { Content = "2  Meeting notes.docx" });

        var footer = new StackPanel { Orientation = Orientation.Horizontal };
        footer.Children.Add(new RibbonApplicationMenuButton { Content = "Options" });
        footer.Children.Add(new RibbonApplicationMenuButton { Content = "Exit" });

        var menu = new RibbonApplicationMenu
        {
            DefaultContent = defaultPage,
            DefaultHeader = "Recent Documents",
            FooterContent = footer,
            UseLayoutRounding = true,
            SnapsToDevicePixels = true,
        };
        menu.Items.Add(new RibbonApplicationMenuItem { Header = "New" });
        menu.Items.Add(new RibbonApplicationMenuItem { Header = "Open" });

        return menu;
    }

    private static DependencyObject? FindDescendantByName(
        DependencyObject root,
        string name)
    {
        if (root is FrameworkElement element && element.Name == name)
        {
            return element;
        }

        int count = VisualTreeHelper.GetChildrenCount(root);
        for (int index = 0; index < count; index++)
        {
            DependencyObject? match = FindDescendantByName(
                VisualTreeHelper.GetChild(root, index),
                name);
            if (match is not null)
            {
                return match;
            }
        }

        return null;
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
        RibbonTheme theme = ThemeManager.CurrentTheme ?? RibbonTheme.Office2024;
        bool lightGlyph = ThemeManager.IsDarkMode
            && theme is RibbonTheme.Office2013 or RibbonTheme.Office2019 or RibbonTheme.Office2024;
        Color foreground = lightGlyph
            ? Color.FromRgb(0xD0, 0xD0, 0xD0)
            : Color.FromRgb(0x44, 0x54, 0x6A);
        var pen = new Pen(new SolidColorBrush(foreground), 1.35)
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
            expected.DpiX,
            expected.DpiY,
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
