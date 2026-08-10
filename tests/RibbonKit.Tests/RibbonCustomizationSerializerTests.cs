using System.Windows;
using System.Windows.Media;
using RibbonKit.Controls;
using Xunit;

namespace RibbonKit.Tests;

public class RibbonCustomizationSerializerTests
{
    [Fact]
    public void Complete_round_trip_restores_structure_commands_icons_and_qat() => Sta.Run(() =>
    {
        Fixture source = FactoryRibbon();
        Customize(source);
        string json = RibbonCustomizationSerializer.Serialize(source.Ribbon);

        Fixture target = FactoryRibbon();
        RibbonCustomizationSerializer.Apply(target.Ribbon, json);

        AssertReferences(
            target.Ribbon.Tabs,
            target.Insert,
            target.Home,
            Assert.IsType<RibbonTab>(target.Ribbon.Tabs[2]),
            target.Developer,
            target.Contextual,
            target.IdlessTab);

        RibbonTab customTab = target.Ribbon.Tabs[2];
        Assert.True(Ribbon.GetIsCustom(customTab));
        Assert.Equal("custom:review", Ribbon.GetCommandId(customTab));
        Assert.Equal("Review", customTab.Header);

        Assert.Equal("Start", target.Home.Header);
        Assert.Equal(Visibility.Collapsed, target.Developer.Visibility);
        AssertReferences(
            target.Home.Groups,
            target.Font,
            Assert.IsType<RibbonGroup>(target.Home.Groups[1]),
            target.Clipboard,
            target.IdlessGroup);
        Assert.Equal("Clipboard Tools", target.Clipboard.Header);

        RibbonGroup favorites = target.Home.Groups[1];
        Assert.True(Ribbon.GetIsCustom(favorites));
        Assert.Equal("custom:favorites", Ribbon.GetCommandId(favorites));
        Assert.Equal("Favorites", favorites.Header);
        Assert.Equal(RibbonGroupLayout.Stacked, favorites.Layout);
        Assert.Same(target.Paste.Icon, favorites.Icon);

        FrameworkElement[] favoriteCommands = favorites.Items.OfType<FrameworkElement>().ToArray();
        Assert.Equal(3, favoriteCommands.Length);
        AssertProxy(favoriteCommands[0], target.Copy, "Duplicate", RibbonControlSize.Small);
        AssertProxy(favoriteCommands[1], target.Bold, "Strong", RibbonControlSize.Medium);
        AssertProxy(favoriteCommands[2], target.Untagged, "Mystery", RibbonControlSize.Medium);

        RibbonGroup review = Assert.Single(customTab.Groups);
        Assert.True(Ribbon.GetIsCustom(review));
        Assert.Equal("custom:review.commands", Ribbon.GetCommandId(review));
        Assert.Equal(RibbonGroupLayout.Large, review.Layout);
        Assert.Same(target.Pictures.Icon, review.Icon);
        AssertProxy(
            Assert.IsAssignableFrom<FrameworkElement>(Assert.Single(review.Items)),
            target.Pictures,
            "Insert Picture",
            RibbonControlSize.Large);

        Assert.Equal(RibbonQuickAccessPosition.BelowRibbon, target.Ribbon.QuickAccessPosition);
        Assert.Equal(3, target.Ribbon.QuickAccessItems.Count);
        AssertProxy(
            Assert.IsAssignableFrom<FrameworkElement>(target.Ribbon.QuickAccessItems[0]),
            target.Copy,
            "Copy",
            RibbonControlSize.Small);
        Assert.Same(target.DeclaredQat, target.Ribbon.QuickAccessItems[1]);
        AssertProxy(
            Assert.IsAssignableFrom<FrameworkElement>(target.Ribbon.QuickAccessItems[2]),
            target.Bold,
            "Bold",
            RibbonControlSize.Small);
    });

    [Fact]
    public void Applying_baseline_resets_from_customized_state_and_is_idempotent() => Sta.Run(() =>
    {
        Fixture fixture = FactoryRibbon();
        string baseline = RibbonCustomizationSerializer.Serialize(fixture.Ribbon);
        Customize(fixture);

        RibbonCustomizationSerializer.Apply(fixture.Ribbon, baseline);
        string once = RibbonCustomizationSerializer.Serialize(fixture.Ribbon);
        RibbonCustomizationSerializer.Apply(fixture.Ribbon, baseline);
        string twice = RibbonCustomizationSerializer.Serialize(fixture.Ribbon);

        Assert.Equal(baseline, once);
        Assert.Equal(once, twice);
        Assert.DoesNotContain(fixture.Ribbon.Tabs, Ribbon.GetIsCustom);
        Assert.DoesNotContain(fixture.Home.Groups, Ribbon.GetIsCustom);
        AssertReferences(
            fixture.Ribbon.Tabs,
            fixture.Home,
            fixture.Insert,
            fixture.Developer,
            fixture.Contextual,
            fixture.IdlessTab);
        AssertReferences(
            fixture.Home.Groups,
            fixture.Clipboard,
            fixture.Font,
            fixture.IdlessGroup);
        Assert.Equal("Home", fixture.Home.Header);
        Assert.Equal("Clipboard", fixture.Clipboard.Header);
        Assert.Equal(Visibility.Visible, fixture.Developer.Visibility);
        Assert.Same(fixture.DeclaredQat, Assert.Single(fixture.Ribbon.QuickAccessItems));
        Assert.Equal(RibbonQuickAccessPosition.TitleBar, fixture.Ribbon.QuickAccessPosition);
    });

    [Fact]
    public void Unknown_commands_are_skipped_without_losing_the_custom_group() => Sta.Run(() =>
    {
        Fixture source = FactoryRibbon();
        Customize(source);
        string json = RibbonCustomizationSerializer.Serialize(source.Ribbon);

        Fixture target = FactoryRibbon();
        target.Clipboard.Items.Remove(target.Copy);
        target.Font.Items.Remove(target.Bold);

        RibbonCustomizationSerializer.Apply(target.Ribbon, json);

        RibbonGroup favorites = target.Home.Groups.Single(Ribbon.GetIsCustom);
        FrameworkElement command = Assert.IsAssignableFrom<FrameworkElement>(Assert.Single(favorites.Items));
        AssertProxy(command, target.Untagged, "Mystery", RibbonControlSize.Medium);
        Assert.DoesNotContain(
            target.Ribbon.QuickAccessItems.OfType<FrameworkElement>(),
            item => ReferenceEquals(Ribbon.GetQuickAccessSource(item), target.Copy)
                || ReferenceEquals(Ribbon.GetQuickAccessSource(item), target.Bold));
        Assert.Same(target.DeclaredQat, Assert.Single(target.Ribbon.QuickAccessItems));
    });

    [Fact]
    public void Newly_shipped_contextual_and_idless_content_is_preserved_at_the_end() => Sta.Run(() =>
    {
        Fixture source = FactoryRibbon();
        string json = RibbonCustomizationSerializer.Serialize(source.Ribbon);

        Fixture target = FactoryRibbon();
        var newTab = Tab("Draw", "TAB_DRAW");
        var newGroup = Group("New Commands", "GROUP_NEW");
        target.Ribbon.Tabs.Insert(0, newTab);
        target.Home.Groups.Insert(0, newGroup);

        RibbonCustomizationSerializer.Apply(target.Ribbon, json);

        AssertReferences(
            target.Ribbon.Tabs,
            target.Home,
            target.Insert,
            target.Developer,
            newTab,
            target.Contextual,
            target.IdlessTab);
        AssertReferences(
            target.Home.Groups,
            target.Clipboard,
            target.Font,
            newGroup,
            target.IdlessGroup);
    });

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("{\"foreign\":true}")]
    [InlineData("{\"Tabs\":null,\"QuickAccess\":[]}")]
    [InlineData("{\"Tabs\":[],\"QuickAccess\":null}")]
    [InlineData("{\"Tabs\":[null],\"QuickAccess\":[]}")]
    [InlineData("{\"Tabs\":[{\"Groups\":null}],\"QuickAccess\":[]}")]
    [InlineData("{\"Tabs\":[{\"Groups\":[{\"Layout\":999}]}],\"QuickAccess\":[]}")]
    [InlineData("{\"Tabs\":[{\"Groups\":[{\"Commands\":[null]}]}],\"QuickAccess\":[]}")]
    [InlineData("{\"Tabs\":[],\"QuickAccess\":[null]}")]
    [InlineData("{\"Tabs\":[],\"QuickAccess\":[],\"QuickAccessPosition\":999}")]
    public void Empty_corrupt_or_foreign_documents_leave_the_ribbon_unchanged(string? json) => Sta.Run(() =>
    {
        Fixture fixture = FactoryRibbon();
        Customize(fixture);
        string before = RibbonCustomizationSerializer.Serialize(fixture.Ribbon);

        RibbonCustomizationSerializer.Apply(fixture.Ribbon, json);

        Assert.Equal(before, RibbonCustomizationSerializer.Serialize(fixture.Ribbon));
    });

    [Fact]
    public void Older_document_without_qat_position_keeps_the_current_placement() => Sta.Run(() =>
    {
        Fixture source = FactoryRibbon();
        System.Text.Json.Nodes.JsonObject layout = System.Text.Json.Nodes.JsonNode
            .Parse(RibbonCustomizationSerializer.Serialize(source.Ribbon))!
            .AsObject();
        layout.Remove(nameof(RibbonCustomizationSerializer.RibbonLayoutDto.QuickAccessPosition));
        string json = layout.ToJsonString();

        Fixture target = FactoryRibbon();
        target.Ribbon.QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon;

        RibbonCustomizationSerializer.Apply(target.Ribbon, json);

        Assert.Equal(RibbonQuickAccessPosition.BelowRibbon, target.Ribbon.QuickAccessPosition);
        Assert.Same(target.DeclaredQat, Assert.Single(target.Ribbon.QuickAccessItems));
    });

    [Fact]
    public void Public_entry_points_reject_a_null_ribbon()
    {
        Assert.Throws<ArgumentNullException>(() => RibbonCustomizationSerializer.Serialize(null!));
        Assert.Throws<ArgumentNullException>(() => RibbonCustomizationSerializer.Apply(null!, "{}"));
    }

    private static Fixture FactoryRibbon()
    {
        var ribbon = new Ribbon { QuickAccessPosition = RibbonQuickAccessPosition.TitleBar };
        RibbonTab home = Tab("Home", "TAB_HOME");
        RibbonTab insert = Tab("Insert", "TAB_INSERT");
        RibbonTab developer = Tab("Developer", "TAB_DEVELOPER");
        RibbonTab contextual = Tab("Picture Format", "TAB_PICTURE_FORMAT");
        contextual.IsContextual = true;
        var idlessTab = new RibbonTab { Header = "Local" };

        RibbonGroup clipboard = Group("Clipboard", "GROUP_CLIPBOARD");
        RibbonGroup font = Group("Font", "GROUP_FONT");
        RibbonGroup illustrations = Group("Illustrations", "GROUP_ILLUSTRATIONS");
        var idlessGroup = new RibbonGroup { Header = "Local Group" };

        var paste = new RibbonButton { Header = "Paste", Icon = Icon(0) };
        Ribbon.SetCommandId(paste, "CMD_PASTE");
        var copy = new RibbonButton { Header = "Copy", Icon = Icon(1) };
        Ribbon.SetCommandId(copy, "CMD_COPY");
        var untagged = new RibbonButton { Header = "Untagged", Icon = Icon(2) };
        var bold = new RibbonToggleButton { Header = "Bold", Icon = Icon(3) };
        Ribbon.SetCommandId(bold, "CMD_BOLD");
        var pictures = new RibbonDropDownButton { Header = "Pictures", Icon = Icon(4) };
        Ribbon.SetCommandId(pictures, "CMD_PICTURES");

        clipboard.Items.Add(untagged);
        clipboard.Items.Add(paste);
        clipboard.Items.Add(copy);
        font.Items.Add(bold);
        illustrations.Items.Add(pictures);
        home.Groups.Add(clipboard);
        home.Groups.Add(font);
        home.Groups.Add(idlessGroup);
        insert.Groups.Add(illustrations);

        ribbon.Tabs.Add(home);
        ribbon.Tabs.Add(insert);
        ribbon.Tabs.Add(developer);
        ribbon.Tabs.Add(contextual);
        ribbon.Tabs.Add(idlessTab);

        var declaredQat = new RibbonButton { Header = "Save", Icon = Icon(5) };
        Ribbon.SetCommandId(declaredQat, "QAT_SAVE");
        ribbon.QuickAccessItems.Add(declaredQat);

        return new Fixture(
            ribbon,
            home,
            insert,
            developer,
            contextual,
            idlessTab,
            clipboard,
            font,
            idlessGroup,
            paste,
            copy,
            untagged,
            bold,
            pictures,
            declaredQat);
    }

    private static void Customize(Fixture fixture)
    {
        fixture.Home.Header = "Start";
        fixture.Clipboard.Header = "Clipboard Tools";
        fixture.Developer.Visibility = Visibility.Collapsed;
        fixture.Ribbon.Tabs.Move(fixture.Ribbon.Tabs.IndexOf(fixture.Insert), 0);
        fixture.Home.Groups.Move(fixture.Home.Groups.IndexOf(fixture.Font), 0);

        var favorites = CustomGroup("Favorites", "custom:favorites", RibbonGroupLayout.Stacked);
        favorites.Icon = fixture.Paste.Icon;
        FrameworkElement copy = fixture.Ribbon.CreateCommandProxy(fixture.Copy, RibbonControlSize.Small);
        SetHeader(copy, "Duplicate");
        FrameworkElement bold = fixture.Ribbon.CreateCommandProxy(fixture.Bold, RibbonControlSize.Medium);
        SetHeader(bold, "Strong");
        FrameworkElement untagged = fixture.Ribbon.CreateCommandProxy(fixture.Untagged, RibbonControlSize.Medium);
        SetHeader(untagged, "Mystery");
        favorites.Items.Add(copy);
        favorites.Items.Add(bold);
        favorites.Items.Add(untagged);
        fixture.Home.Groups.Insert(1, favorites);

        var reviewTab = new RibbonTab { Header = "Review" };
        Ribbon.SetIsCustom(reviewTab, true);
        Ribbon.SetCommandId(reviewTab, "custom:review");
        var review = CustomGroup("Review Commands", "custom:review.commands", RibbonGroupLayout.Large);
        review.Icon = fixture.Pictures.Icon;
        FrameworkElement picture = fixture.Ribbon.CreateCommandProxy(fixture.Pictures, RibbonControlSize.Large);
        SetHeader(picture, "Insert Picture");
        review.Items.Add(picture);
        reviewTab.Groups.Add(review);
        fixture.Ribbon.Tabs.Insert(2, reviewTab);

        fixture.Ribbon.QuickAccessItems.Clear();
        fixture.Ribbon.QuickAccessItems.Add(
            fixture.Ribbon.CreateCommandProxy(fixture.Copy, RibbonControlSize.Small));
        fixture.Ribbon.QuickAccessItems.Add(fixture.DeclaredQat);
        fixture.Ribbon.QuickAccessItems.Add(
            fixture.Ribbon.CreateCommandProxy(fixture.Bold, RibbonControlSize.Small));
        fixture.Ribbon.QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon;
    }

    private static RibbonTab Tab(string header, string id)
    {
        var tab = new RibbonTab { Header = header };
        Ribbon.SetCommandId(tab, id);
        return tab;
    }

    private static RibbonGroup Group(string header, string id)
    {
        var group = new RibbonGroup { Header = header };
        Ribbon.SetCommandId(group, id);
        return group;
    }

    private static RibbonGroup CustomGroup(string header, string id, RibbonGroupLayout layout)
    {
        var group = new RibbonGroup { Header = header };
        Ribbon.SetIsCustom(group, true);
        Ribbon.SetCommandId(group, id);
        group.Layout = layout;
        return group;
    }

    private static DrawingImage Icon(double offset) => new(
        new GeometryDrawing(
            Brushes.SteelBlue,
            null,
            new RectangleGeometry(new Rect(offset, 0, 1, 1))));

    private static void AssertProxy(
        FrameworkElement proxy,
        FrameworkElement source,
        string header,
        RibbonControlSize size)
    {
        Assert.Same(source, Ribbon.GetQuickAccessSource(proxy));
        Assert.Equal(header, HeaderOf(proxy));
        Assert.Equal(size, SizeOf(proxy));
    }

    private static string? HeaderOf(FrameworkElement element) => element switch
    {
        RibbonButton button => button.Header,
        RibbonToggleButton toggle => toggle.Header,
        RibbonDropDownButton dropDown => dropDown.Header,
        _ => null,
    };

    private static RibbonControlSize SizeOf(FrameworkElement element) => element switch
    {
        RibbonButton button => button.Size,
        RibbonToggleButton toggle => toggle.Size,
        RibbonDropDownButton dropDown => dropDown.Size,
        _ => throw new Xunit.Sdk.XunitException($"Unexpected proxy type {element.GetType().Name}."),
    };

    private static void SetHeader(FrameworkElement element, string header)
    {
        switch (element)
        {
            case RibbonButton button:
                button.Header = header;
                break;
            case RibbonToggleButton toggle:
                toggle.Header = header;
                break;
            case RibbonDropDownButton dropDown:
                dropDown.Header = header;
                break;
            default:
                throw new Xunit.Sdk.XunitException($"Unexpected proxy type {element.GetType().Name}.");
        }
    }

    private static void AssertReferences<T>(IEnumerable<T> actual, params T[] expected)
        where T : class
    {
        Assert.Equal(expected.Length, actual.Count());
        Assert.All(actual.Zip(expected), pair => Assert.Same(pair.Second, pair.First));
    }

    private sealed record Fixture(
        Ribbon Ribbon,
        RibbonTab Home,
        RibbonTab Insert,
        RibbonTab Developer,
        RibbonTab Contextual,
        RibbonTab IdlessTab,
        RibbonGroup Clipboard,
        RibbonGroup Font,
        RibbonGroup IdlessGroup,
        RibbonButton Paste,
        RibbonButton Copy,
        RibbonButton Untagged,
        RibbonToggleButton Bold,
        RibbonDropDownButton Pictures,
        RibbonButton DeclaredQat);
}
