using System.IO;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml.Linq;
using RibbonKit.Animation;
using RibbonKit.Controls;
using Xunit;
using IOPath = System.IO.Path;
using RibbonKeyTipService = RibbonKit.Controls.KeyTipService;

namespace RibbonKit.Tests;

/// <summary>Focused RibbonKit reproductions promoted from the Writer consumer-friction log.</summary>
public sealed class WriterConsumerFrictionTests
{
    private const string RibbonContentBackgroundKey =
        "RibbonKit.Brushes.Ribbon.ContentBackground";

    public static TheoryData<RibbonBackstageDesign> BackstageDesigns => new()
    {
        RibbonBackstageDesign.Modern,
        RibbonBackstageDesign.Classic2010,
        RibbonBackstageDesign.Classic2007,
    };

    [Fact]
    public void Showcase_demonstrates_compact_direct_and_stacked_group_separators()
    {
        string showcaseRoot = IOPath.Combine(
            RepositoryRoot(),
            "samples",
            "RibbonKit.Showcase");
        XDocument window = XDocument.Load(IOPath.Combine(showcaseRoot, "MainWindow.xaml"));
        XDocument icons = XDocument.Load(IOPath.Combine(showcaseRoot, "Icons.xaml"));
        XNamespace ribbonKit = "urn:ribbonkit";
        XNamespace presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        XNamespace xaml = "http://schemas.microsoft.com/winfx/2006/xaml";

        XElement Separator(string name) => Assert.Single(
            window.Descendants(ribbonKit + "RibbonGroupSeparator"),
            element => (string?)element.Attribute(xaml + "Name") == name);

        XElement compact = Separator("FontCompactSeparator");
        Assert.Equal("20", (string?)compact.Attribute("Height"));
        XElement compactRow = Assert.IsType<XElement>(compact.Parent);
        XElement[] compactItems = compactRow.Elements().ToArray();
        int compactIndex = Array.IndexOf(compactItems, compact);
        Assert.Equal("cmd.underline", (string?)compactItems[compactIndex - 1]
            .Attribute(ribbonKit + "Ribbon.CommandId"));
        Assert.Equal("SuperscriptButton", (string?)compactItems[compactIndex + 1]
            .Attribute(xaml + "Name"));

        XElement illustrations = Separator("IllustrationsGroupSeparator");
        Assert.Equal("Illustrations", (string?)illustrations.Parent?.Attribute("Header"));
        Assert.Equal("ScreenshotButton", (string?)illustrations.ElementsAfterSelf().First()
            .Attribute(xaml + "Name"));

        XElement zoom = Separator("ViewZoomGroupSeparator");
        Assert.Equal("Zoom", (string?)zoom.Parent?.Attribute("Header"));
        XElement zoomStack = zoom.ElementsAfterSelf().First();
        Assert.Equal("StackPanel", zoomStack.Name.LocalName);
        Assert.Equal(
            new[] { "ActualSizeButton", "PageWidthButton" },
            zoomStack.Elements().Select(element => (string?)element.Attribute(xaml + "Name")));

        Assert.Single(
            icons.Descendants(presentation + "DrawingImage"),
            element => (string?)element.Attribute(xaml + "Key") == "Icon.Superscript");
    }

    [Fact]
    public void Showcase_keeps_document_view_commands_separate_from_ribbon_diagnostics()
    {
        XDocument window = XDocument.Load(IOPath.Combine(
            RepositoryRoot(),
            "samples",
            "RibbonKit.Showcase",
            "MainWindow.xaml"));
        XNamespace ribbonKit = "urn:ribbonkit";

        XElement Tab(string commandId) => Assert.Single(
            window.Descendants(ribbonKit + "RibbonTab"),
            element => (string?)element.Attribute(ribbonKit + "Ribbon.CommandId") == commandId);

        static string?[] GroupHeaders(XElement tab) => tab
            .Elements("{urn:ribbonkit}RibbonGroup")
            .Select(group => (string?)group.Attribute("Header"))
            .ToArray();

        Assert.Equal(new[] { "Zoom" }, GroupHeaders(Tab("tab.view")));

        XElement ribbonLab = Tab("tab.ribbonLab");
        Assert.Equal("Ribbon Lab", (string?)ribbonLab.Attribute("Header"));
        Assert.Equal(
            new[] { "Theme", "Aero Frame", "Accent", "Motion", "Inputs", "Backstage", "Application" },
            GroupHeaders(ribbonLab));
    }

    [Fact]
    public void Group_separator_adapts_without_becoming_a_command_or_automation_element() => Sta.Run(() =>
    {
        var left = new Border { Width = 20d, Height = 20d };
        var separator = new RibbonGroupSeparator();
        var right = new Border { Width = 20d, Height = 20d };
        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Height = 70d,
        };
        row.Children.Add(left);
        row.Children.Add(separator);
        row.Children.Add(right);
        Window window = TestWindow(row, new Size(120d, 80d));

        try
        {
            window.Show();
            Sta.Drain(DispatcherPriority.Render);
            separator.ApplyTemplate();

            Assert.Equal(RibbonGroupSizeState.Large, separator.SizeState);
            Assert.Equal(9d, separator.ActualWidth);
            Assert.Equal(52d, separator.ActualHeight);
            var line = Assert.IsType<Rectangle>(VisualTreeHelper.GetChild(
                Assert.IsType<Grid>(VisualTreeHelper.GetChild(separator, 0)), 0));
            Assert.Same(separator.TryFindResource("RibbonKit.Brushes.Group.Separator"), line.Fill);

            ((IRibbonSizeAware)separator).ApplySizeState(RibbonGroupSizeState.Medium);
            row.UpdateLayout();
            Assert.Equal(RibbonGroupSizeState.Medium, separator.SizeState);
            Assert.Equal(7d, separator.ActualWidth);
            Assert.Equal(40d, separator.ActualHeight);

            ((IRibbonSizeAware)separator).ApplySizeState(RibbonGroupSizeState.Small);
            row.UpdateLayout();
            Assert.Equal(RibbonGroupSizeState.Small, separator.SizeState);
            Assert.Equal(5d, separator.ActualWidth);
            Assert.Equal(28d, separator.ActualHeight);

            ((IRibbonSizeAware)separator).ApplySizeState(RibbonGroupSizeState.Collapsed);
            row.UpdateLayout();
            Assert.Equal(RibbonGroupSizeState.Large, separator.SizeState);
            Assert.Equal(9d, separator.ActualWidth);
            Assert.Equal(52d, separator.ActualHeight);

            row.FlowDirection = FlowDirection.RightToLeft;
            row.UpdateLayout();
            double separatorX = separator.TransformToAncestor(row).Transform(default).X;
            double leftX = left.TransformToAncestor(row).Transform(default).X;
            double rightX = right.TransformToAncestor(row).Transform(default).X;
            Assert.InRange(separatorX, Math.Min(leftX, rightX), Math.Max(leftX, rightX));

            Assert.False(RibbonKeyTipService.IsRibbonKeyTipControl(separator));
            Assert.Null(UIElementAutomationPeer.CreatePeerForElement(separator));
            var ribbon = new Ribbon();
            Assert.False(ribbon.AddToQuickAccess(separator));
            Assert.Empty(ribbon.QuickAccessItems);

            var group = new RibbonGroup();
            var groupSeparator = new RibbonGroupSeparator();
            group.Items.Add(new RibbonButton { Header = "Command" });
            group.Items.Add(groupSeparator);
            Assert.Single(RibbonCommandCatalog.CollectControls(group));
            group.SetSizeState(RibbonGroupSizeState.Medium);
            Assert.Equal(RibbonGroupSizeState.Medium, groupSeparator.SizeState);
            group.SetSizeState(RibbonGroupSizeState.Collapsed);
            Assert.Equal(RibbonGroupSizeState.Large, groupSeparator.SizeState);
        }
        finally
        {
            window.Close();
        }
    });

    [Fact]
    public void In_ribbon_gallery_popup_resolves_its_surface_from_the_gallery_scope() => Sta.Run(() =>
    {
        var expected = new SolidColorBrush(Color.FromRgb(0x23, 0x45, 0x67));
        var gallery = new InRibbonGallery
        {
            Width = 240d,
            Height = 72d,
        };
        gallery.Items.Add(new RibbonGalleryItem { Content = "First" });
        Window window = TestWindow(gallery, new Size(260d, 100d));
        window.Resources[RibbonContentBackgroundKey] = expected;

        try
        {
            window.Show();
            Sta.Drain(DispatcherPriority.Render);
            gallery.UpdateLayout();

            var popupHost = Assert.IsType<Border>(
                gallery.Template.FindName("PART_PopupHost", gallery));

            // Reproduce the separate-HWND resource break seen by Writer: the template expression can
            // remain unresolved after the popup child leaves the ribbon's resource ancestry.
            popupHost.Background = null;
            Assert.Null(popupHost.Background);

            gallery.IsDropDownOpen = true;
            Sta.Drain(DispatcherPriority.Render);

            Assert.Same(expected, popupHost.Background);
            gallery.IsDropDownOpen = false;
        }
        finally
        {
            window.Close();
        }
    });

    [Fact]
    public void Revealing_a_middle_contextual_tab_repositions_the_selected_tab_marker() => Sta.Run(() =>
    {
        var home = new RibbonTab { Header = "Home" };
        var contextual = new RibbonTab
        {
            Header = "Picture Format",
            IsContextual = true,
            Visibility = Visibility.Collapsed,
        };
        var page = new RibbonTab { Header = "Page" };
        var tabControl = new RibbonTabControl
        {
            Width = 760d,
            Height = 150d,
            Template = MarkerTemplate(),
        };
        tabControl.Resources["RibbonKit.Brushes.Tab.SelectedUnderline"] = Brushes.Blue;
        tabControl.ItemsSource = new[] { home, contextual, page };
        tabControl.SelectedItem = page;

        Window window = TestWindow(tabControl, new Size(760d, 150d));

        try
        {
            window.Show();
            Sta.Drain(DispatcherPriority.Loaded);
            tabControl.UpdateLayout();

            var marker = Assert.IsType<Rectangle>(
                tabControl.Template.FindName("PART_TabMarker", tabControl));
            var translate = Assert.IsType<TranslateTransform>(marker.RenderTransform);
            var markerHost = Assert.IsAssignableFrom<Visual>(VisualTreeHelper.GetParent(marker));
            double before = translate.X;
            double expectedBefore = page.TransformToVisual(markerHost).Transform(default).X + 10d;
            Assert.Equal(expectedBefore, before, precision: 6);

            contextual.Visibility = Visibility.Visible;
            tabControl.UpdateLayout();
            Sta.Drain(DispatcherPriority.Loaded);
            tabControl.UpdateLayout();

            double expected = page.TransformToVisual(markerHost).Transform(default).X + 10d;
            Assert.True(expected > before, "The revealed middle tab should shift the selected tab right.");
            Assert.Equal(expected, translate.X, precision: 6);
        }
        finally
        {
            window.Close();
        }
    });

    [Theory]
    [MemberData(nameof(BackstageDesigns))]
    public void Backstage_closed_is_raised_after_teardown(RibbonBackstageDesign design) => Sta.Run(() =>
    {
        var backstage = new Backstage { Design = design };
        var ribbon = new Ribbon
        {
            Width = 760d,
            Height = 150d,
            Backstage = backstage,
        };
        ribbon.Tabs.Add(new RibbonTab { Header = "Home" });
        var root = new Grid();
        root.Children.Add(ribbon);
        Window window = TestWindow(root, new Size(760d, 400d));
        int closed = 0;
        ribbon.BackstageClosed += (_, _) =>
        {
            Assert.False(ribbon.IsBackstageOpen);
            Assert.False(backstage.IsVisible);
            closed++;
        };

        RibbonAnimation.SetActionLevel(
            RibbonAnimationAction.Backstage,
            RibbonAnimationLevel.None);
        try
        {
            window.Show();
            Sta.Drain(DispatcherPriority.Loaded);

            ribbon.IsBackstageOpen = true;
            Sta.Drain(DispatcherPriority.Loaded);
            Assert.Equal(0, closed);

            ribbon.IsBackstageOpen = false;
            Assert.Equal(1, closed);
        }
        finally
        {
            RibbonAnimation.ClearActionLevel(RibbonAnimationAction.Backstage);
            window.Close();
        }
    });

    [Fact]
    public void Application_menu_close_does_not_raise_backstage_closed() => Sta.Run(() =>
    {
        var ribbon = new Ribbon
        {
            ApplicationMenu = new RibbonApplicationMenu(),
        };
        int closed = 0;
        ribbon.BackstageClosed += (_, _) => closed++;

        ribbon.IsBackstageOpen = true;
        ribbon.IsBackstageOpen = false;

        Assert.Equal(0, closed);
    });

    [Fact]
    public void Reopening_backstage_cancels_the_pending_closed_notification() => Sta.Run(() =>
    {
        var ribbon = new Ribbon
        {
            Width = 760d,
            Height = 150d,
            Backstage = new Backstage(),
        };
        ribbon.Tabs.Add(new RibbonTab { Header = "Home" });
        var root = new Grid();
        root.Children.Add(ribbon);
        Window window = TestWindow(root, new Size(760d, 400d));
        int closed = 0;
        ribbon.BackstageClosed += (_, _) => closed++;

        bool respectSystemReduceMotion = RibbonAnimation.RespectSystemReduceMotion;
        RibbonAnimation.RespectSystemReduceMotion = false;
        RibbonAnimation.SetActionLevel(
            RibbonAnimationAction.Backstage,
            RibbonAnimationLevel.Expressive);
        try
        {
            window.Show();
            Sta.Drain(DispatcherPriority.Loaded);

            ribbon.IsBackstageOpen = true;
            ribbon.IsBackstageOpen = false;
            ribbon.IsBackstageOpen = true;

            Thread.Sleep(RibbonAnimation.GetDuration(RibbonAnimationAction.Backstage).TimeSpan +
                TimeSpan.FromMilliseconds(100d));
            Sta.Drain(DispatcherPriority.ApplicationIdle);

            Assert.True(ribbon.IsBackstageOpen);
            Assert.Equal(0, closed);

            RibbonAnimation.SetActionLevel(
                RibbonAnimationAction.Backstage,
                RibbonAnimationLevel.None);
            ribbon.IsBackstageOpen = false;
            Assert.Equal(1, closed);
        }
        finally
        {
            RibbonAnimation.ClearActionLevel(RibbonAnimationAction.Backstage);
            RibbonAnimation.RespectSystemReduceMotion = respectSystemReduceMotion;
            window.Close();
        }
    });

    private static Window TestWindow(FrameworkElement content, Size size) => new()
    {
        Width = size.Width,
        Height = size.Height,
        Left = -10000d,
        Top = -10000d,
        ShowActivated = false,
        ShowInTaskbar = false,
        WindowStyle = WindowStyle.None,
        Content = content,
    };

    private static ControlTemplate MarkerTemplate() => Assert.IsType<ControlTemplate>(XamlReader.Parse(
        """
        <ControlTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                         xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                         xmlns:rk="clr-namespace:RibbonKit.Controls;assembly=RibbonKit"
                         TargetType="{x:Type rk:RibbonTabControl}">
            <Grid>
                <TabPanel IsItemsHost="True" />
                <Rectangle x:Name="PART_TabMarker"
                           Width="0"
                           Height="3"
                           HorizontalAlignment="Left"
                           VerticalAlignment="Top">
                    <Rectangle.RenderTransform>
                        <TranslateTransform x:Name="PART_TabMarkerTranslate" />
                    </Rectangle.RenderTransform>
                </Rectangle>
            </Grid>
        </ControlTemplate>
        """));

    private static string RepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(IOPath.Combine(directory.FullName, "RibbonKit.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the RibbonKit repository root.");
    }

}
