using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml.Linq;
using RibbonKit.Controls;
using Xunit;

namespace RibbonKit.Tests;

/// <summary>Guards the public, behavioral, theme, gallery, and Showcase scrollbar contracts.</summary>
public sealed class RibbonScrollBarTests
{
    private static readonly XNamespace Presentation =
        "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

    private static readonly XNamespace Xaml =
        "http://schemas.microsoft.com/winfx/2006/xaml";

    [Fact]
    public void Ribbon_scrollbar_is_a_lookless_native_scrollbar_with_range_automation() => Sta.Run(() =>
    {
        var scrollBar = new TestRibbonScrollBar
        {
            Minimum = 0d,
            Maximum = 100d,
            Value = 25d,
            SmallChange = 5d,
        };

        Assert.IsAssignableFrom<ScrollBar>(scrollBar);
        Assert.Equal(typeof(RibbonScrollBar), scrollBar.StyleKey);

        ScrollBar.LineDownCommand.Execute(null, scrollBar);
        Assert.Equal(30d, scrollBar.Value, precision: 6);

        scrollBar.Orientation = Orientation.Horizontal;
        ScrollBar.LineRightCommand.Execute(null, scrollBar);
        Assert.Equal(35d, scrollBar.Value, precision: 6);

        var peer = Assert.IsType<ScrollBarAutomationPeer>(
            UIElementAutomationPeer.CreatePeerForElement(scrollBar));
        Assert.Equal(AutomationControlType.ScrollBar, peer.GetAutomationControlType());
        Assert.NotNull(peer.GetPattern(PatternInterface.RangeValue));

        var nativeScrollBar = new ScrollBar();
        RibbonScrollBar.SetButtonCornerRadius(nativeScrollBar, new CornerRadius(1d));
        RibbonScrollBar.SetThumbCornerRadius(nativeScrollBar, new CornerRadius(5d));
        RibbonScrollBar.SetRailCornerRadius(nativeScrollBar, new CornerRadius(2d));
        Assert.Equal(new CornerRadius(1d), RibbonScrollBar.GetButtonCornerRadius(nativeScrollBar));
        Assert.Equal(new CornerRadius(5d), RibbonScrollBar.GetThumbCornerRadius(nativeScrollBar));
        Assert.Equal(new CornerRadius(2d), RibbonScrollBar.GetRailCornerRadius(nativeScrollBar));
    });

    [Fact]
    public void Standalone_scrollbars_realize_the_shared_vertical_and_horizontal_templates() => Sta.Run(() =>
    {
        var vertical = new RibbonScrollBar
        {
            Height = 80d,
            Maximum = 100d,
            Value = 30d,
            ViewportSize = 20d,
            ButtonCornerRadius = new CornerRadius(0d),
            ThumbCornerRadius = new CornerRadius(6d),
            RailCornerRadius = new CornerRadius(4d),
        };
        var horizontal = new RibbonScrollBar
        {
            Orientation = Orientation.Horizontal,
            Width = 120d,
            Maximum = 100d,
            Value = 40d,
            ViewportSize = 20d,
        };
        var panel = new StackPanel();
        panel.Children.Add(vertical);
        panel.Children.Add(horizontal);
        var window = new Window
        {
            Width = 180d,
            Height = 140d,
            Left = -10000d,
            Top = -10000d,
            ShowActivated = false,
            ShowInTaskbar = false,
            WindowStyle = WindowStyle.None,
            Content = panel,
        };
        window.Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri(
                "/RibbonKit;component/Themes/Tokens.Office2024.xaml",
                UriKind.RelativeOrAbsolute),
        });

        try
        {
            window.Show();
            Sta.Drain(DispatcherPriority.Render);

            var verticalDecrease = Assert.IsType<RepeatButton>(
                vertical.Template.FindName("DecreaseButton", vertical));
            var verticalIncrease = Assert.IsType<RepeatButton>(
                vertical.Template.FindName("IncreaseButton", vertical));
            Assert.Equal(vertical.ActualWidth, verticalDecrease.ActualHeight, precision: 6);
            Assert.Equal(vertical.ActualWidth, verticalIncrease.ActualHeight, precision: 6);
            verticalDecrease.ApplyTemplate();
            var verticalButtonChrome = Assert.IsType<Border>(
                verticalDecrease.Template.FindName("Chrome", verticalDecrease));
            Assert.Equal(new CornerRadius(0d), verticalButtonChrome.CornerRadius);
            var verticalRail = Assert.IsType<Border>(vertical.Template.FindName("Root", vertical));
            Assert.Equal(new CornerRadius(4d), verticalRail.CornerRadius);
            var verticalTrack = Assert.IsAssignableFrom<Track>(vertical.Template.FindName("PART_Track", vertical));
            Assert.Equal(Orientation.Vertical, verticalTrack.Orientation);
            verticalTrack.Thumb.ApplyTemplate();
            Assert.Equal(default, verticalTrack.Thumb.Margin);
            var verticalPill = Assert.IsType<Border>(
                verticalTrack.Thumb.Template.FindName("Pill", verticalTrack.Thumb));
            Assert.True(
                verticalTrack.Thumb.ActualHeight >= 20d,
                $"Track arranged a {verticalTrack.Thumb.ActualHeight:0.###}-DIP vertical thumb.");
            Assert.True(
                verticalPill.ActualHeight >= 18d,
                $"The inner vertical pill rendered at only {verticalPill.ActualHeight:0.###} DIP.");
            Assert.Equal(verticalTrack.Thumb.ActualHeight, verticalPill.ActualHeight, precision: 6);
            Assert.Equal(vertical.ActualWidth, verticalPill.ActualWidth, precision: 6);
            Assert.Equal(default, verticalPill.Margin);
            Assert.Equal(new CornerRadius(6d), verticalPill.CornerRadius);
            verticalTrack.Value = 55d;
            Assert.Equal(55d, vertical.Value, precision: 6);

            var horizontalDecrease = Assert.IsType<RepeatButton>(
                horizontal.Template.FindName("DecreaseButton", horizontal));
            var horizontalIncrease = Assert.IsType<RepeatButton>(
                horizontal.Template.FindName("IncreaseButton", horizontal));
            Assert.Equal(horizontal.ActualHeight, horizontalDecrease.ActualWidth, precision: 6);
            Assert.Equal(horizontal.ActualHeight, horizontalIncrease.ActualWidth, precision: 6);
            var horizontalTrack = Assert.IsAssignableFrom<Track>(horizontal.Template.FindName("PART_Track", horizontal));
            Assert.Equal(Orientation.Horizontal, horizontalTrack.Orientation);
            horizontalTrack.Thumb.ApplyTemplate();
            Assert.Equal(default, horizontalTrack.Thumb.Margin);
            var horizontalPill = Assert.IsType<Border>(
                horizontalTrack.Thumb.Template.FindName("Pill", horizontalTrack.Thumb));
            Assert.Equal(horizontalTrack.Thumb.ActualWidth, horizontalPill.ActualWidth, precision: 6);
            Assert.Equal(horizontal.ActualHeight, horizontalPill.ActualHeight, precision: 6);
            Assert.Equal(default, horizontalPill.Margin);
            horizontalTrack.Value = 65d;
            Assert.Equal(65d, horizontal.Value, precision: 6);
        }
        finally
        {
            window.Close();
        }
    });

    [Theory]
    [InlineData("Tokens.Office2010.xaml", 17d, 18.01d, 17d, 2d, true)]
    [InlineData("Tokens.Office2024.xaml", 15d, 16.01d, 19d, 3d, false)]
    public void Compact_vertical_example_keeps_complete_thumb_and_bottom_button_geometry(
        string themeFile,
        double minimumButtonExtent,
        double maximumButtonExtent,
        double minimumThumbLength,
        double railRadius,
        bool hasNormalButtonChrome) => Sta.Run(() =>
    {
        var scrollBar = new RibbonScrollBar
        {
            Height = 56d,
            Minimum = 0d,
            Maximum = 100d,
            Value = 35d,
            ViewportSize = 25d,
        };
        var window = new Window
        {
            Width = 80d,
            Height = 100d,
            Left = -10000d,
            Top = -10000d,
            ShowActivated = false,
            ShowInTaskbar = false,
            WindowStyle = WindowStyle.None,
            Content = scrollBar,
        };
        window.Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri(
                $"/RibbonKit;component/Themes/{themeFile}",
                UriKind.RelativeOrAbsolute),
        });

        try
        {
            window.Show();
            Sta.Drain(DispatcherPriority.Render);

            var root = Assert.IsType<Border>(scrollBar.Template.FindName("Root", scrollBar));
            var decrease = Assert.IsType<RepeatButton>(
                scrollBar.Template.FindName("DecreaseButton", scrollBar));
            var increase = Assert.IsType<RepeatButton>(
                scrollBar.Template.FindName("IncreaseButton", scrollBar));
            var track = Assert.IsAssignableFrom<Track>(scrollBar.Template.FindName("PART_Track", scrollBar));
            track.Thumb.ApplyTemplate();
            var pill = Assert.IsType<Border>(track.Thumb.Template.FindName("Pill", track.Thumb));
            Rect thumbSlot = LayoutInformation.GetLayoutSlot(track.Thumb);
            Geometry? thumbClip = LayoutInformation.GetLayoutClip(track.Thumb);

            if (hasNormalButtonChrome)
            {
                Assert.IsType<LinearGradientBrush>(decrease.Background);
                Assert.NotEqual(Brushes.Transparent, decrease.BorderBrush);
            }
            else
            {
                Assert.Equal(Colors.Transparent, Assert.IsType<SolidColorBrush>(decrease.Background).Color);
                Assert.Equal(Colors.Transparent, Assert.IsType<SolidColorBrush>(decrease.BorderBrush).Color);
            }

            Assert.Equal(new CornerRadius(railRadius), root.CornerRadius);
            Assert.InRange(decrease.ActualHeight, minimumButtonExtent, maximumButtonExtent);
            Assert.InRange(increase.ActualHeight, minimumButtonExtent, maximumButtonExtent);
            Assert.Equal(track.Thumb.ActualHeight, pill.ActualHeight, precision: 6);
            Assert.True(
                pill.ActualHeight >= minimumThumbLength,
                $"The compact vertical pill is only {pill.ActualHeight:0.###} DIP tall; slot={thumbSlot}; clip={thumbClip?.Bounds}." );
            Assert.Equal(thumbSlot.Height, pill.ActualHeight, precision: 6);
            Assert.Null(thumbClip);
            Assert.Equal(scrollBar.ActualWidth, pill.ActualWidth, precision: 6);
            Assert.True(
                increase.TranslatePoint(new Point(0d, increase.ActualHeight), root).Y <= root.ActualHeight + 0.01d,
                "The bottom line button extends beyond the rounded rail host.");
        }
        finally
        {
            window.Close();
        }
    });

    [Fact]
    public void Every_theme_variant_defines_the_complete_scrollbar_palette_and_metrics()
    {
        string[] themeFiles =
        [
            "Tokens.Office2007.xaml",
            "Tokens.Office2007.Dark.xaml",
            "Tokens.Office2010.xaml",
            "Tokens.Office2010.Dark.xaml",
            "Tokens.Office2013.xaml",
            "Tokens.Office2013.Dark.xaml",
            "Tokens.Office2019.xaml",
            "Tokens.Office2019.Dark.xaml",
            "Tokens.Office2024.xaml",
            "Tokens.Office2024.Dark.xaml",
        ];
        string[] brushKeys =
        [
            "RibbonKit.Brushes.ScrollBar.Track",
            "RibbonKit.Brushes.ScrollBar.ButtonBackground",
            "RibbonKit.Brushes.ScrollBar.ButtonBorder",
            "RibbonKit.Brushes.ScrollBar.Thumb",
            "RibbonKit.Brushes.ScrollBar.ThumbHover",
            "RibbonKit.Brushes.ScrollBar.ThumbPressed",
            "RibbonKit.Brushes.ScrollBar.ThumbBorder",
            "RibbonKit.Brushes.ScrollBar.Glyph",
            "RibbonKit.Brushes.Dialog.ActionBackground",
            "RibbonKit.Brushes.Dialog.ActionBorder",
        ];
        string[] metricKeys =
        [
            "RibbonKit.Metrics.ScrollBar.Thickness",
            "RibbonKit.Metrics.ScrollBar.MinThumbLength",
            "RibbonKit.Metrics.ScrollBar.ButtonCornerRadius",
            "RibbonKit.Metrics.ScrollBar.ThumbCornerRadius",
            "RibbonKit.Metrics.ScrollBar.RailCornerRadius",
            "RibbonKit.Metrics.ScrollBar.ThumbBorderThickness",
        ];

        foreach (string themeFile in themeFiles)
        {
            XDocument document = XDocument.Load(ThemePart(themeFile));
            foreach (string key in brushKeys)
            {
                XElement resource = Resource(document, key);
                Assert.EndsWith("Brush", resource.Name.LocalName, StringComparison.Ordinal);
            }

            Assert.Equal("Double", Resource(document, metricKeys[0]).Name.LocalName);
            Assert.Equal("Double", Resource(document, metricKeys[1]).Name.LocalName);
            Assert.Equal("CornerRadius", Resource(document, metricKeys[2]).Name.LocalName);
            Assert.Equal("CornerRadius", Resource(document, metricKeys[3]).Name.LocalName);
            Assert.Equal("CornerRadius", Resource(document, metricKeys[4]).Name.LocalName);
            Assert.Equal("Thickness", Resource(document, metricKeys[5]).Name.LocalName);
        }
    }

    [Theory]
    [InlineData("Tokens.Office2007.xaml")]
    [InlineData("Tokens.Office2007.Dark.xaml")]
    [InlineData("Tokens.Office2010.xaml")]
    [InlineData("Tokens.Office2010.Dark.xaml")]
    public void Office_2007_and_2010_use_outlined_gel_thumb_brushes(string themeFile)
    {
        XDocument document = XDocument.Load(ThemePart(themeFile));

        Assert.Equal("LinearGradientBrush", Resource(document, "RibbonKit.Brushes.ScrollBar.Thumb").Name.LocalName);
        Assert.Equal("LinearGradientBrush", Resource(document, "RibbonKit.Brushes.ScrollBar.ThumbHover").Name.LocalName);
        Assert.Equal("LinearGradientBrush", Resource(document, "RibbonKit.Brushes.ScrollBar.ThumbPressed").Name.LocalName);
        Assert.Equal(
            "LinearGradientBrush",
            Resource(document, "RibbonKit.Brushes.ScrollBar.ButtonBackground").Name.LocalName);
        Assert.NotEqual(
            "Transparent",
            Resource(document, "RibbonKit.Brushes.ScrollBar.ButtonBorder").Attribute("Color")?.Value);
        Assert.Equal(
            "1",
            Resource(document, "RibbonKit.Metrics.ScrollBar.ThumbBorderThickness").Value.Trim());
        Assert.Equal(
            "LinearGradientBrush",
            Resource(document, "RibbonKit.Brushes.Dialog.ActionBackground").Name.LocalName);
        Assert.NotEqual(
            "Transparent",
            Resource(document, "RibbonKit.Brushes.Dialog.ActionBorder").Attribute("Color")?.Value);
    }

    [Theory]
    [InlineData("Tokens.Office2013.xaml")]
    [InlineData("Tokens.Office2013.Dark.xaml")]
    [InlineData("Tokens.Office2019.xaml")]
    [InlineData("Tokens.Office2019.Dark.xaml")]
    public void Office_2013_and_2019_scrollbar_chrome_is_square(string themeFile)
    {
        XDocument document = XDocument.Load(ThemePart(themeFile));

        Assert.Equal("0", Resource(document, "RibbonKit.Metrics.ScrollBar.ButtonCornerRadius").Value.Trim());
        Assert.Equal("0", Resource(document, "RibbonKit.Metrics.ScrollBar.ThumbCornerRadius").Value.Trim());
        Assert.Equal("0", Resource(document, "RibbonKit.Metrics.ScrollBar.RailCornerRadius").Value.Trim());
    }

    [Fact]
    public void Modern_dialog_action_tokens_define_visible_flat_normal_chrome()
    {
        string[] themeFiles =
        [
            "Tokens.Office2013.xaml",
            "Tokens.Office2013.Dark.xaml",
            "Tokens.Office2019.xaml",
            "Tokens.Office2019.Dark.xaml",
            "Tokens.Office2024.xaml",
            "Tokens.Office2024.Dark.xaml",
        ];

        foreach (string themeFile in themeFiles)
        {
            XDocument document = XDocument.Load(ThemePart(themeFile));
            XElement background = Resource(document, "RibbonKit.Brushes.Dialog.ActionBackground");
            XElement border = Resource(document, "RibbonKit.Brushes.Dialog.ActionBorder");

            Assert.Equal("SolidColorBrush", background.Name.LocalName);
            Assert.Equal("SolidColorBrush", border.Name.LocalName);
            Assert.NotEqual("Transparent", background.Attribute("Color")?.Value);
            Assert.NotEqual("Transparent", border.Attribute("Color")?.Value);
        }
    }

    [Fact]
    public void Shared_templates_cover_both_orientations_high_contrast_and_gallery_overflow()
    {
        XDocument scrollBars = XDocument.Load(ThemePart("Controls.ScrollBars.xaml"));
        XElement[] templates = scrollBars.Root!
            .Elements(Presentation + "ControlTemplate")
            .Where(element => ((string?)element.Attribute(Xaml + "Key"))?.Contains("ScrollBarTemplate") == true)
            .ToArray();

        Assert.Equal(2, templates.Length);
        Assert.All(
            templates,
            template =>
            {
                Assert.Equal("{x:Type ScrollBar}", (string?)template.Attribute("TargetType"));
                Assert.Single(
                    template.Descendants().Where(element => element.Name.LocalName == "RibbonScrollBarTrack"),
                    track => (string?)track.Attribute(Xaml + "Name") == "PART_Track");
                Assert.Contains(
                    template.Descendants(Presentation + "DataTrigger"),
                    trigger => ((string?)trigger.Attribute("Binding"))?.Contains("SystemParameters.HighContrast") == true);
            });

        XElement derivedStyle = Assert.Single(
            scrollBars.Root!.Elements(Presentation + "Style"),
            style => (string?)style.Attribute("TargetType") == "{x:Type controls:RibbonScrollBar}");
        Assert.Equal("{StaticResource RibbonKit.ScrollBarStyle}", (string?)derivedStyle.Attribute("BasedOn"));

        XDocument galleries = XDocument.Load(ThemePart("Controls.Galleries.xaml"));
        Assert.Equal(
            2,
            galleries.Descendants(Presentation + "ScrollViewer")
                .Count(viewer => viewer
                    .Descendants(Presentation + "Style")
                    .Any(style => (string?)style.Attribute("BasedOn")
                        == "{StaticResource RibbonKit.GalleryScrollBarStyle}")));

        XDocument aggregate = XDocument.Load(ThemePart("Office2024.xaml"));
        Assert.Contains(
            aggregate.Descendants(Presentation + "ResourceDictionary"),
            dictionary => ((string?)dictionary.Attribute("Source"))?.EndsWith("Controls.ScrollBars.xaml") == true);

        string toolbox = File.ReadAllText(Path.Combine(
            RepositoryRoot(),
            "src",
            "RibbonKit",
            "tools",
            "VisualStudioToolsManifest.xml"));
        Assert.Contains("RibbonKit.Controls.RibbonScrollBar", toolbox, StringComparison.Ordinal);
    }

    [Fact]
    public void Options_dialog_uses_scrollbar_button_chrome_without_restyling_primary_or_close_buttons()
    {
        XDocument customize = XDocument.Load(ThemePart("Controls.Customize.xaml"));
        XDocument options = XDocument.Load(ThemePart("Controls.OptionsDialog.xaml"));

        Assert.Contains(
            customize.Root!
                .Element(Presentation + "ResourceDictionary.MergedDictionaries")!
                .Elements(Presentation + "ResourceDictionary"),
            dictionary => ((string?)dictionary.Attribute("Source"))
                ?.EndsWith("Controls.ScrollBars.xaml", StringComparison.Ordinal) == true);

        XElement adapter = Assert.Single(
            customize.Root!.Elements(Presentation + "Style"),
            style => (string?)style.Attribute(Xaml + "Key")
                == "RibbonKit.CustomizePageScrollBarStyle");
        Assert.Equal("{x:Type ScrollBar}", (string?)adapter.Attribute("TargetType"));

        XElement quickAccessStyle = Assert.Single(
            customize.Root!.Elements(Presentation + "Style"),
            style => (string?)style.Attribute("TargetType")
                == "{x:Type controls:RibbonQuickAccessPage}");
        XElement quickAccessScrollBarStyle = Assert.Single(
            quickAccessStyle.Descendants(Presentation + "Style"),
            style => (string?)style.Attribute("BasedOn")
                == "{StaticResource RibbonKit.CustomizePageScrollBarStyle}");
        Assert.Null(quickAccessScrollBarStyle.Attribute(Xaml + "Key"));
        Assert.Equal("{x:Type ScrollBar}", (string?)quickAccessScrollBarStyle.Attribute("TargetType"));

        XElement customizeStyle = Assert.Single(
            customize.Root!.Elements(Presentation + "Style"),
            style => (string?)style.Attribute("TargetType")
                == "{x:Type controls:RibbonCustomizePage}");
        XElement scopedStyle = Assert.Single(
            customizeStyle.Descendants(Presentation + "Style"),
            style => (string?)style.Attribute("BasedOn")
                == "{StaticResource RibbonKit.CustomizePageScrollBarStyle}");
        Assert.Null(scopedStyle.Attribute(Xaml + "Key"));
        Assert.Equal("{x:Type ScrollBar}", (string?)scopedStyle.Attribute("TargetType"));

        XElement actionStyle = Assert.Single(
            options.Root!.Elements(Presentation + "Style"),
            style => (string?)style.Attribute(Xaml + "Key")
                == "OptionsDialogActionButtonStyle");
        Assert.Null(actionStyle.Attribute("BasedOn"));
        Assert.Contains(
            actionStyle.Elements(Presentation + "Setter"),
            setter => (string?)setter.Attribute("Property") == "MinWidth"
                && (string?)setter.Attribute("Value") == "88");
        Assert.Contains(
            actionStyle.Descendants(Presentation + "Setter"),
            setter => (string?)setter.Attribute("Property")
                == "controls:RibbonScrollBar.ButtonCornerRadius");
        Assert.Contains(
            actionStyle.Descendants(Presentation + "Setter"),
            setter => (string?)setter.Attribute("Property") == "Background"
                && (string?)setter.Attribute("Value")
                    == "{DynamicResource RibbonKit.Brushes.Dialog.ActionBackground}");
        Assert.Contains(
            actionStyle.Descendants(Presentation + "Setter"),
            setter => (string?)setter.Attribute("Property") == "BorderThickness"
                && (string?)setter.Attribute("Value") == "1");

        XElement compactStyle = Assert.Single(
            options.Root!.Elements(Presentation + "Style"),
            style => (string?)style.Attribute(Xaml + "Key")
                == "OptionsDialogReorderButtonStyle");
        Assert.Equal(
            "{StaticResource OptionsDialogActionButtonStyle}",
            (string?)compactStyle.Attribute("BasedOn"));

        XElement[] quickAccessButtons = quickAccessStyle.Descendants(Presentation + "Button").ToArray();
        Assert.Equal(4, quickAccessButtons.Length);
        Assert.Equal(
            2,
            quickAccessButtons.Count(button => (string?)button.Attribute("Style")
                == "{StaticResource OptionsDialogActionButtonStyle}"));
        Assert.Equal(
            2,
            quickAccessButtons.Count(button => (string?)button.Attribute("Style")
                == "{StaticResource OptionsDialogReorderButtonStyle}"));

        XElement[] pageButtons = customizeStyle.Descendants(Presentation + "Button").ToArray();
        Assert.Equal(10, pageButtons.Length);
        Assert.Equal(
            6,
            pageButtons.Count(button => (string?)button.Attribute("Style")
                == "{StaticResource OptionsDialogActionButtonStyle}"));
        Assert.Equal(
            4,
            pageButtons.Count(button => (string?)button.Attribute("Style")
                == "{StaticResource OptionsDialogReorderButtonStyle}"));

        XElement optionsDialogStyle = Assert.Single(
            options.Root!.Elements(Presentation + "Style"),
            style => (string?)style.Attribute("TargetType")
                == "{x:Type controls:RibbonOptionsDialog}");
        XElement okButton = Assert.Single(
            optionsDialogStyle.Descendants(Presentation + "Button"),
            button => (string?)button.Attribute(Xaml + "Name") == "PART_OkButton");
        XElement cancelButton = Assert.Single(
            optionsDialogStyle.Descendants(Presentation + "Button"),
            button => (string?)button.Attribute(Xaml + "Name") == "PART_CancelButton");
        XElement closeButton = Assert.Single(
            optionsDialogStyle.Descendants(Presentation + "Button"),
            button => (string?)button.Attribute(Xaml + "Name") == "PART_CloseButton");
        Assert.Equal(
            "{StaticResource OptionsDialogPrimaryButtonStyle}",
            (string?)okButton.Attribute("Style"));
        Assert.Equal(
            "{StaticResource OptionsDialogActionButtonStyle}",
            (string?)cancelButton.Attribute("Style"));
        Assert.Equal(
            "{StaticResource OptionsDialogCloseButtonStyle}",
            (string?)closeButton.Attribute("Style"));

        XElement editDialogStyle = Assert.Single(
            customize.Root!.Elements(Presentation + "Style"),
            style => (string?)style.Attribute("TargetType")
                == "{x:Type controls:RibbonCustomizeEditDialog}");
        XElement editOkButton = Assert.Single(
            editDialogStyle.Descendants(Presentation + "Button"),
            button => (string?)button.Attribute(Xaml + "Name") == "PART_OkButton");
        XElement editCancelButton = Assert.Single(
            editDialogStyle.Descendants(Presentation + "Button"),
            button => (string?)button.Attribute(Xaml + "Name") == "PART_CancelButton");
        Assert.Equal(
            "{StaticResource OptionsDialogPrimaryButtonStyle}",
            (string?)editOkButton.Attribute("Style"));
        Assert.Equal(
            "{StaticResource OptionsDialogActionButtonStyle}",
            (string?)editCancelButton.Attribute("Style"));

        XElement primaryStyle = Assert.Single(
            options.Root!.Elements(Presentation + "Style"),
            style => (string?)style.Attribute(Xaml + "Key")
                == "OptionsDialogPrimaryButtonStyle");
        Assert.Single(primaryStyle.Descendants(Presentation + "ControlTemplate"));
        Assert.Contains(
            primaryStyle.Elements(Presentation + "Setter"),
            setter => (string?)setter.Attribute("Property") == "Background"
                && (string?)setter.Attribute("Value")
                    == "{DynamicResource RibbonKit.Brushes.Dialog.PrimaryBackground}");
    }

    [Fact]
    public void Customize_ribbon_overflow_realizes_the_office_2010_scrollbars_and_scrolls() => Sta.Run(() =>
    {
        var resources = new ResourceDictionary
        {
            Source = new Uri(
                "/RibbonKit;component/Themes/Controls.Customize.xaml",
                UriKind.Relative),
        };
        var page = new RibbonCustomizePage
        {
            Style = Assert.IsType<Style>(resources[typeof(RibbonCustomizePage)]),
        };
        var window = new Window
        {
            Width = 760d,
            Height = 330d,
            Left = -10000d,
            Top = -10000d,
            ShowActivated = false,
            ShowInTaskbar = false,
            WindowStyle = WindowStyle.None,
            Content = page,
        };
        window.Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri(
                "/RibbonKit;component/Themes/Tokens.Office2010.xaml",
                UriKind.RelativeOrAbsolute),
        });

        try
        {
            window.Show();
            Sta.Drain(DispatcherPriority.Loaded);

            var available = Assert.IsType<ListBox>(page.Template.FindName("PART_AvailableList", page));
            var tree = Assert.IsType<TreeView>(page.Template.FindName("PART_Tree", page));
            available.ItemsSource = Enumerable.Range(1, 60)
                .Select(index => $"Home › Group › Command {index}")
                .ToArray();
            tree.ItemsSource = Enumerable.Range(1, 60)
                .Select(index => $"Tab {index}")
                .ToArray();

            Sta.Drain(DispatcherPriority.Render);

            AssertThemedCustomizationScrollBar(available);
            AssertThemedCustomizationScrollBar(tree);

            var newTab = Assert.IsType<Button>(page.Template.FindName("PART_NewTabButton", page));
            newTab.ApplyTemplate();
            var chrome = Assert.IsType<Border>(newTab.Template.FindName("Chrome", newTab));
            Assert.IsType<LinearGradientBrush>(newTab.Background);
            Assert.Equal(new CornerRadius(2d), RibbonScrollBar.GetButtonCornerRadius(newTab));
            Assert.Equal(new CornerRadius(2d), chrome.CornerRadius);
            Assert.NotEqual(Colors.Transparent, Assert.IsType<SolidColorBrush>(newTab.BorderBrush).Color);
        }
        finally
        {
            window.Close();
        }
    });

    [Fact]
    public void Quick_access_overflow_realizes_the_office_2010_scrollbars_and_scrolls() => Sta.Run(() =>
    {
        var resources = new ResourceDictionary
        {
            Source = new Uri(
                "/RibbonKit;component/Themes/Controls.Customize.xaml",
                UriKind.Relative),
        };
        var page = new RibbonQuickAccessPage
        {
            Style = Assert.IsType<Style>(resources[typeof(RibbonQuickAccessPage)]),
        };
        var window = new Window
        {
            Width = 760d,
            Height = 330d,
            Left = -10000d,
            Top = -10000d,
            ShowActivated = false,
            ShowInTaskbar = false,
            WindowStyle = WindowStyle.None,
            Content = page,
        };
        window.Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri(
                "/RibbonKit;component/Themes/Tokens.Office2010.xaml",
                UriKind.RelativeOrAbsolute),
        });

        try
        {
            window.Show();
            Sta.Drain(DispatcherPriority.Loaded);

            var available = Assert.IsType<ListBox>(page.Template.FindName("PART_AvailableList", page));
            var current = Assert.IsType<ListBox>(page.Template.FindName("PART_CurrentList", page));
            available.ItemsSource = Enumerable.Range(1, 60)
                .Select(index => $"Home › Group › Command {index}")
                .ToArray();
            current.ItemsSource = Enumerable.Range(1, 60)
                .Select(index => $"Quick Access Command {index}")
                .ToArray();

            Sta.Drain(DispatcherPriority.Render);

            AssertThemedCustomizationScrollBar(available);
            AssertThemedCustomizationScrollBar(current);
        }
        finally
        {
            window.Close();
        }
    });

    [Fact]
    public void Options_dialog_content_overflow_realizes_the_shared_scrollbar_and_scrolls() => Sta.Run(() =>
    {
        var dialog = new RibbonOptionsDialog
        {
            Width = 640d,
            Height = 420d,
            Left = -10000d,
            Top = -10000d,
            ShowActivated = false,
            ShowInTaskbar = false,
        };
        dialog.Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri(
                "/RibbonKit;component/Themes/Tokens.Office2010.xaml",
                UriKind.RelativeOrAbsolute),
        });
        var page = new RibbonOptionsPage
        {
            Header = "Tall page",
            Content = new Border { Height = 900d },
        };
        dialog.Pages.Add(page);
        dialog.SelectedPage = page;

        try
        {
            dialog.Show();
            Sta.Drain(DispatcherPriority.Loaded);
            Sta.Drain(DispatcherPriority.Render);

            var contentScroll = Assert.IsType<ScrollViewer>(
                dialog.Template.FindName("PART_ContentScroll", dialog));
            Assert.Equal(new Thickness(0d, 0d, 1d, 0d), contentScroll.Margin);
            ScrollBar scrollBar = Assert.Single(
                VisualDescendants<ScrollBar>(contentScroll),
                candidate => candidate.Orientation == Orientation.Vertical);

            Style generatedStyle = Assert.IsType<Style>(contentScroll.Resources[typeof(ScrollBar)]);
            Style sharedStyle = Assert.IsType<Style>(
                contentScroll.FindResource("RibbonKit.ScrollBarStyle"));
            Assert.Same(sharedStyle, generatedStyle);
            Assert.Same(generatedStyle, scrollBar.Style);
            Assert.Equal(Visibility.Visible, scrollBar.Visibility);
            Assert.NotNull(scrollBar.Template.FindName("DecreaseButton", scrollBar));
            Assert.NotNull(scrollBar.Template.FindName("IncreaseButton", scrollBar));
            Assert.NotNull(scrollBar.Template.FindName("PART_Track", scrollBar));
            Assert.Equal(new CornerRadius(2d), RibbonScrollBar.GetButtonCornerRadius(scrollBar));
            Assert.Equal(new CornerRadius(3d), RibbonScrollBar.GetThumbCornerRadius(scrollBar));
            Assert.Equal(new CornerRadius(2d), RibbonScrollBar.GetRailCornerRadius(scrollBar));
            Assert.True(contentScroll.ScrollableHeight > 0d);

            double before = contentScroll.VerticalOffset;
            ScrollBar.LineDownCommand.Execute(null, scrollBar);
            Sta.Drain(DispatcherPriority.Render);
            Assert.True(contentScroll.VerticalOffset > before);
        }
        finally
        {
            dialog.Close();
        }
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Ribbon_combo_box_popup_overflow_realizes_the_shared_scrollbar_and_scrolls(bool isEditable) => Sta.Run(() =>
    {
        var comboBox = new RibbonComboBox
        {
            Width = 180d,
            IsEditable = isEditable,
            MaxDropDownHeight = 120d,
            ItemsSource = Enumerable.Range(1, 60).Select(index => $"Item {index}").ToArray(),
        };
        var window = new Window
        {
            Width = 240d,
            Height = 100d,
            Left = -10000d,
            Top = -10000d,
            ShowActivated = false,
            ShowInTaskbar = false,
            WindowStyle = WindowStyle.None,
            Content = comboBox,
        };
        window.Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri(
                "/RibbonKit;component/Themes/Tokens.Office2010.xaml",
                UriKind.RelativeOrAbsolute),
        });

        try
        {
            window.Show();
            Sta.Drain(DispatcherPriority.Loaded);
            comboBox.IsDropDownOpen = true;
            Sta.Drain(DispatcherPriority.Render);

            var editableTextBox = Assert.IsType<TextBox>(
                comboBox.Template.FindName("PART_EditableTextBox", comboBox));
            Assert.Equal(isEditable ? Visibility.Visible : Visibility.Hidden, editableTextBox.Visibility);

            var popup = Assert.IsType<Popup>(comboBox.Template.FindName("PART_Popup", comboBox));
            var popupRoot = Assert.IsType<Border>(popup.Child);
            var popupScrollViewer = Assert.Single(VisualDescendants<ScrollViewer>(popupRoot));
            ScrollBar scrollBar = Assert.Single(
                VisualDescendants<ScrollBar>(popupScrollViewer),
                candidate => candidate.Orientation == Orientation.Vertical);

            Style generatedStyle = Assert.IsType<Style>(popupScrollViewer.Resources[typeof(ScrollBar)]);
            Style sharedStyle = Assert.IsType<Style>(
                popupScrollViewer.FindResource("RibbonKit.ScrollBarStyle"));
            Assert.Same(sharedStyle, generatedStyle);
            Assert.Same(generatedStyle, scrollBar.Style);
            Assert.Equal(Visibility.Visible, scrollBar.Visibility);
            Assert.NotNull(scrollBar.Template.FindName("DecreaseButton", scrollBar));
            Assert.NotNull(scrollBar.Template.FindName("IncreaseButton", scrollBar));
            Assert.NotNull(scrollBar.Template.FindName("PART_Track", scrollBar));
            Assert.Equal(new CornerRadius(2d), RibbonScrollBar.GetButtonCornerRadius(scrollBar));
            Assert.Equal(new CornerRadius(3d), RibbonScrollBar.GetThumbCornerRadius(scrollBar));
            Assert.Equal(new CornerRadius(2d), RibbonScrollBar.GetRailCornerRadius(scrollBar));
            Assert.True(popupScrollViewer.ScrollableHeight > 0d);

            double before = popupScrollViewer.VerticalOffset;
            ScrollBar.LineDownCommand.Execute(null, scrollBar);
            Sta.Drain(DispatcherPriority.Render);
            Assert.True(popupScrollViewer.VerticalOffset > before);
        }
        finally
        {
            comboBox.IsDropDownOpen = false;
            window.Close();
        }
    });

    [Fact]
    public void Office_2024_dialog_action_button_has_visible_flat_normal_chrome() => Sta.Run(() =>
    {
        var resources = new ResourceDictionary
        {
            Source = new Uri(
                "/RibbonKit;component/Themes/Controls.OptionsDialog.xaml",
                UriKind.Relative),
        };
        var button = new Button
        {
            Content = "Cancel",
            Style = Assert.IsType<Style>(resources["OptionsDialogActionButtonStyle"]),
        };
        var window = new Window
        {
            Width = 180d,
            Height = 90d,
            Left = -10000d,
            Top = -10000d,
            ShowActivated = false,
            ShowInTaskbar = false,
            WindowStyle = WindowStyle.None,
            Content = button,
        };
        window.Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri(
                "/RibbonKit;component/Themes/Tokens.Office2024.xaml",
                UriKind.RelativeOrAbsolute),
        });

        try
        {
            window.Show();
            Sta.Drain(DispatcherPriority.Render);

            button.ApplyTemplate();
            var chrome = Assert.IsType<Border>(button.Template.FindName("Chrome", button));
            Assert.NotEqual(Colors.Transparent, Assert.IsType<SolidColorBrush>(button.Background).Color);
            Assert.NotEqual(Colors.Transparent, Assert.IsType<SolidColorBrush>(button.BorderBrush).Color);
            Assert.Equal(new Thickness(1d), button.BorderThickness);
            Assert.Equal(new CornerRadius(3d), chrome.CornerRadius);
        }
        finally
        {
            window.Close();
        }
    });

    [Fact]
    public void In_ribbon_gallery_overflow_realizes_the_themed_scrollbar_and_scrolls() => Sta.Run(() =>
    {
        var gallery = new InRibbonGallery
        {
            Width = 240d,
            Height = 54d,
        };
        for (int index = 0; index < 36; index++)
        {
            gallery.Items.Add(new RibbonGalleryItem
            {
                Content = $"Style {index + 1}",
                Width = 72d,
                Height = 48d,
            });
        }

        var window = new Window
        {
            Width = 280d,
            Height = 120d,
            Left = -10000d,
            Top = -10000d,
            ShowActivated = false,
            ShowInTaskbar = false,
            WindowStyle = WindowStyle.None,
            Content = gallery,
        };
        window.Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri(
                "/RibbonKit;component/Themes/Tokens.Office2024.xaml",
                UriKind.RelativeOrAbsolute),
        });

        try
        {
            window.Show();
            Sta.Drain(DispatcherPriority.Render);
            gallery.IsDropDownOpen = true;
            Sta.Drain(DispatcherPriority.Render);

            var popupScrollViewer = Assert.IsType<ScrollViewer>(
                gallery.Template.FindName("PART_PopupScrollViewer", gallery));
            ScrollBar scrollBar = Assert.Single(
                VisualDescendants<ScrollBar>(popupScrollViewer),
                candidate => candidate.Orientation == Orientation.Vertical);

            Assert.Equal(Visibility.Visible, scrollBar.Visibility);
            Assert.NotNull(scrollBar.Template.FindName("DecreaseButton", scrollBar));
            Assert.NotNull(scrollBar.Template.FindName("IncreaseButton", scrollBar));
            Assert.Equal(new CornerRadius(3d), RibbonScrollBar.GetButtonCornerRadius(scrollBar));
            Assert.Equal(new CornerRadius(5d), RibbonScrollBar.GetThumbCornerRadius(scrollBar));
            Assert.Equal(new CornerRadius(3d), RibbonScrollBar.GetRailCornerRadius(scrollBar));
            Assert.True(popupScrollViewer.ScrollableHeight > 0d);

            double before = popupScrollViewer.VerticalOffset;
            ScrollBar.LineDownCommand.Execute(null, scrollBar);
            Sta.Drain(DispatcherPriority.Render);
            Assert.True(popupScrollViewer.VerticalOffset > before);
        }
        finally
        {
            gallery.IsDropDownOpen = false;
            window.Close();
        }
    });

    [Fact]
    public void Showcase_exposes_vertical_and_horizontal_scrollbar_examples_on_ribbon_lab()
    {
        XDocument showcase = XDocument.Load(Path.Combine(
            RepositoryRoot(),
            "samples",
            "RibbonKit.Showcase",
            "MainWindow.xaml"));
        XNamespace rk = "urn:ribbonkit";

        XElement group = Assert.Single(
            showcase.Descendants(rk + "RibbonGroup"),
            element => (string?)element.Attribute("Header") == "Scrolling");
        XElement[] examples = group.Descendants(rk + "RibbonScrollBar").ToArray();

        Assert.Equal(2, examples.Length);
        Assert.Contains(examples, element => element.Attribute("Orientation") is null);
        Assert.Contains(examples, element => (string?)element.Attribute("Orientation") == "Horizontal");
        XElement verticalExample = Assert.Single(examples, element => element.Attribute("Orientation") is null);
        Assert.True(
            (double?)verticalExample.Attribute("Height") <= 56d,
            "The labeled vertical example must fit above the RibbonGroup footer without clipping its bottom button.");

        XElement accentGallery = Assert.Single(
            showcase.Descendants(rk + "InRibbonGallery"),
            element => (string?)element.Attribute(Xaml + "Name") == "AccentGallery");
        Assert.True(
            accentGallery.Elements(rk + "RibbonGalleryItem").Count() >= 24,
            "AccentGallery must keep enough rows to exercise popup overflow in the live Showcase.");
    }

    private static XElement Resource(XDocument document, string key) => Assert.Single(
        document.Root!.Elements(),
        element => (string?)element.Attribute(Xaml + "Key") == key);

    private static void AssertThemedCustomizationScrollBar(ItemsControl itemsControl)
    {
        var scrollViewer = Assert.Single(VisualDescendants<ScrollViewer>(itemsControl));
        ScrollBar scrollBar = Assert.Single(
            VisualDescendants<ScrollBar>(scrollViewer),
            candidate => candidate.Orientation == Orientation.Vertical);

        Assert.Equal(Visibility.Visible, scrollBar.Visibility);
        Assert.NotNull(scrollBar.Template.FindName("DecreaseButton", scrollBar));
        Assert.NotNull(scrollBar.Template.FindName("IncreaseButton", scrollBar));
        Assert.NotNull(scrollBar.Template.FindName("PART_Track", scrollBar));
        Assert.Equal(new CornerRadius(2d), RibbonScrollBar.GetButtonCornerRadius(scrollBar));
        Assert.Equal(new CornerRadius(3d), RibbonScrollBar.GetThumbCornerRadius(scrollBar));
        Assert.Equal(new CornerRadius(2d), RibbonScrollBar.GetRailCornerRadius(scrollBar));
        Assert.True(scrollViewer.ScrollableHeight > 0d);

        double before = scrollViewer.VerticalOffset;
        ScrollBar.LineDownCommand.Execute(null, scrollBar);
        Sta.Drain(DispatcherPriority.Render);
        Assert.True(scrollViewer.VerticalOffset > before);
    }

    private static string ThemePart(string name) =>
        Path.Combine(RepositoryRoot(), "src", "RibbonKit", "Themes", name);

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "RibbonKit.sln")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return directory!.FullName;
    }

    private static IEnumerable<T> VisualDescendants<T>(DependencyObject root)
        where T : DependencyObject
    {
        for (int index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(root, index);
            if (child is T match)
            {
                yield return match;
            }

            foreach (T descendant in VisualDescendants<T>(child))
            {
                yield return descendant;
            }
        }
    }

    private sealed class TestRibbonScrollBar : RibbonScrollBar
    {
        internal object? StyleKey => DefaultStyleKey;
    }
}
