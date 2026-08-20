using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Xml.Linq;
using RibbonKit.Controls;
using Xunit;

namespace RibbonKit.Tests;

/// <summary>Realized-layout contracts for the Word-style Office 2010 Backstage placement.</summary>
public sealed class Office2010BackstagePlacementTests
{
    private static readonly XNamespace Xaml =
        "http://schemas.microsoft.com/winfx/2006/xaml";

    [Fact]
    public void Shipping_tab_template_publishes_the_below_backstage_anchor()
    {
        XDocument document = XDocument.Load(ThemePart("Controls.RibbonChrome.xaml"));

        Assert.Single(
            document.Descendants(),
            element => (string?)element.Attribute(Xaml + "Name")
                == RibbonTabControl.TabHeaderHostPartName);
    }

    [Fact]
    public void Backstage_adorner_tracks_the_live_bottom_of_the_tab_header() => Sta.Run(() =>
    {
        var adornedRoot = new Grid();
        adornedRoot.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        adornedRoot.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1d, GridUnitType.Star) });

        var tabHeader = new Border { Height = 42d, Background = Brushes.LightBlue };
        adornedRoot.Children.Add(tabHeader);

        var document = new Border { Background = Brushes.White };
        Grid.SetRow(document, 1);
        adornedRoot.Children.Add(document);

        var decorator = new AdornerDecorator
        {
            Width = 500d,
            Height = 320d,
            Child = adornedRoot,
        };
        var available = new Size(500d, 320d);
        decorator.Measure(available);
        decorator.Arrange(new Rect(available));
        decorator.UpdateLayout();

        var ribbon = new Ribbon();
        var backstage = new Border { Background = Brushes.White };
        var adorner = new BackstageAdorner(adornedRoot, backstage, ribbon, tabHeader);
        AdornerLayer layer = Assert.IsType<AdornerLayer>(AdornerLayer.GetAdornerLayer(adornedRoot));
        layer.Add(adorner);
        decorator.UpdateLayout();

        Assert.True(adorner.IsInsetPlacementActive);
        Assert.Equal(42d, adorner.TopInset, precision: 6);
        Assert.Equal(278d, backstage.RenderSize.Height, precision: 6);
        Assert.Equal(
            42d,
            backstage.TransformToVisual(adorner).Transform(default).Y,
            precision: 6);
        Assert.Same(
            tabHeader,
            VisualTreeHelper.HitTest(decorator, new Point(12d, 12d))?.VisualHit);
        Assert.Same(
            backstage,
            VisualTreeHelper.HitTest(decorator, new Point(12d, 72d))?.VisualHit);

        tabHeader.Height = 56d;
        decorator.UpdateLayout();
        Sta.Drain();
        decorator.UpdateLayout();

        Assert.Equal(56d, adorner.TopInset, precision: 6);
        Assert.Equal(264d, backstage.RenderSize.Height, precision: 6);
        Assert.Equal(
            56d,
            backstage.TransformToVisual(adorner).Transform(default).Y,
            precision: 6);

        layer.Remove(adorner);
        adorner.Detach();
    });

    [Fact]
    public void Below_tabs_placement_uses_the_visible_File_tab_instead_of_a_back_button() => Sta.Run(() =>
    {
        var root = new FrameworkElementFactory(typeof(Grid));
        root.AppendChild(new FrameworkElementFactory(typeof(Button), "PART_BackButton"));
        var backstage = new Backstage
        {
            Template = new ControlTemplate(typeof(Backstage)) { VisualTree = root },
        };

        backstage.ApplyTemplate();
        var available = new Size(300d, 200d);
        backstage.Measure(available);
        backstage.Arrange(new Rect(available));
        Button button = Assert.IsType<Button>(backstage.Template.FindName("PART_BackButton", backstage));

        backstage.SetBelowTabsPlacement(true);
        Assert.Equal(Visibility.Collapsed, button.Visibility);

        backstage.SetBelowTabsPlacement(false);
        Assert.Equal(Visibility.Visible, button.Visibility);
    });

    [Fact]
    public void Classic2010_backstage_draws_a_file_colored_seam_below_the_tabs()
    {
        XDocument document = XDocument.Load(ThemePart("Controls.Backstage.xaml"));
        XElement seam = Assert.Single(
            document.Descendants(),
            element => (string?)element.Attribute(Xaml + "Name") == "Classic2010TopSeam");

        Assert.Equal("3", (string?)seam.Attribute("Height"));
        Assert.Contains(
            "RibbonKit.Brushes.ApplicationButton.MenuOpenBottom",
            (string?)seam.Attribute("Background"),
            StringComparison.Ordinal);

        XElement classicTrigger = Assert.Single(
            document.Descendants(Presentation("Trigger")),
            trigger => (string?)trigger.Attribute("Property") == "controls:Backstage.Design"
                && (string?)trigger.Attribute("Value") == "Classic2010"
                && trigger.Elements(Presentation("Setter")).Any(
                    setter => (string?)setter.Attribute("TargetName") == "Classic2010TopSeam"));
        Assert.Contains(
            classicTrigger.Elements(Presentation("Setter")),
            setter => (string?)setter.Attribute("TargetName") == "Classic2010TopSeam"
                && (string?)setter.Attribute("Property") == "Visibility"
                && (string?)setter.Attribute("Value") == "Visible");
    }

    [Fact]
    public void Open_backstage_uses_the_dimensional_application_button_fill()
    {
        XDocument document = XDocument.Load(ThemePart("Controls.RibbonChrome.xaml"));
        XElement applicationButton = Assert.Single(
            document.Descendants(Presentation("ToggleButton")),
            element => (string?)element.Attribute(Xaml + "Name") == "PART_ApplicationButton");
        XElement checkedTrigger = Assert.Single(
            applicationButton.Descendants(Presentation("Trigger")),
            trigger => (string?)trigger.Attribute("Property") == "IsChecked"
                && (string?)trigger.Attribute("Value") == "True");

        Assert.Contains(
            checkedTrigger.Elements(Presentation("Setter")),
            setter => (string?)setter.Attribute("TargetName") == "Chrome"
                && (string?)setter.Attribute("Property") == "Background"
                && setter.Attribute("Value")!.Value.Contains(
                    "RibbonKit.Brushes.ApplicationButton.MenuOpenBackground",
                    StringComparison.Ordinal));
    }

    [Fact]
    public void Selected_ribbon_tab_drops_its_selected_chrome_while_backstage_is_open()
    {
        XDocument document = XDocument.Load(ThemePart("Controls.Groups.xaml"));
        XElement restTrigger = Assert.Single(
            document.Descendants(Presentation("Trigger")),
            trigger => (string?)trigger.Attribute("Property") == "IsBackstageActive"
                && (string?)trigger.Attribute("Value") == "True");
        Assert.Contains(
            restTrigger.Elements(Presentation("Setter")),
            setter => (string?)setter.Attribute("TargetName") == "HeaderChrome"
                && (string?)setter.Attribute("Property") == "Background"
                && (string?)setter.Attribute("Value") == "Transparent");
        Assert.Contains(
            restTrigger.Elements(Presentation("Setter")),
            setter => setter.Attribute("TargetName") is null
                && (string?)setter.Attribute("Property") == "Foreground"
                && setter.Attribute("Value")!.Value.Contains(
                    "RibbonKit.Brushes.TabStrip.Foreground",
                    StringComparison.Ordinal));

        XElement hoverTrigger = Assert.Single(
            document.Descendants(Presentation("MultiTrigger")),
            trigger => trigger
                .Descendants(Presentation("Condition"))
                .Any(condition => (string?)condition.Attribute("Property") == "IsBackstageActive"
                    && (string?)condition.Attribute("Value") == "True"));
        Assert.Contains(
            hoverTrigger.Descendants(Presentation("Condition")),
            condition => (string?)condition.Attribute("SourceName") == "HeaderChrome"
                && (string?)condition.Attribute("Property") == "IsMouseOver"
                && (string?)condition.Attribute("Value") == "True");
        Assert.Contains(
            hoverTrigger.Elements(Presentation("Setter")),
            setter => (string?)setter.Attribute("TargetName") == "HeaderChrome"
                && (string?)setter.Attribute("Property") == "Background"
                && setter.Attribute("Value")!.Value.Contains(
                    "RibbonKit.Brushes.Tab.HoverBackground",
                    StringComparison.Ordinal));
    }

    [Fact]
    public void Host_and_each_native_or_merged_tab_own_the_backstage_state() => Sta.Run(() =>
    {
        var tabControl = new RibbonTabControl();
        var ribbon = new Ribbon { Backstage = new Backstage() };
        var native = new RibbonTab { Header = "Home" };
        var source = new RibbonMergeSource();
        var merged = new RibbonTab { Header = "Chart Design", IsContextual = true };
        ribbon.Tabs.Add(native);
        source.Tabs.Add(merged);
        ribbon.Merge(source);

        Assert.False(tabControl.IsBackstageActive);
        tabControl.SetBackstageActive(true);
        Assert.True(tabControl.IsBackstageActive);
        tabControl.SetBackstageActive(false);
        Assert.False(tabControl.IsBackstageActive);

        Assert.False(native.IsBackstageActive);
        Assert.False(merged.IsBackstageActive);
        ribbon.IsBackstageOpen = true;
        Assert.True(native.IsBackstageActive);
        Assert.True(merged.IsBackstageActive);
        ribbon.Unmerge(source);
        Assert.True(native.IsBackstageActive);
        Assert.False(merged.IsBackstageActive);
        ribbon.Merge(source);
        Assert.True(merged.IsBackstageActive);
        ribbon.IsBackstageOpen = false;
        Assert.False(native.IsBackstageActive);
        Assert.False(merged.IsBackstageActive);
    });

    private static XName Presentation(string localName) =>
        XName.Get(localName, "http://schemas.microsoft.com/winfx/2006/xaml/presentation");

    private static string ThemePart(string name) =>
        Path.Combine(RepositoryRoot(), "src", "RibbonKit", "Themes", name);

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "RibbonKit.sln")))
        {
            directory = directory.Parent;
        }

        Assert.True(directory is not null, $"Could not find RibbonKit.sln above {AppContext.BaseDirectory}.");
        return directory!.FullName;
    }
}
