using System.ComponentModel;
using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class BackstageNavigationTests
{
    [Fact]
    public void Empty_footer_preserves_layout_and_owned_component_contracts() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl(); var backstage = ribbon.Backstage;
        Assert.Empty(backstage.FooterItems);
        Assert.Equal(DesignerSerializationVisibility.Content, TypeDescriptor.GetProperties(backstage)[nameof(backstage.FooterItems)]!.SerializationVisibility);
        var before = RibbonBackstageLayout.Build(new(900, 500), ribbon.Font, 96);
        var after = RibbonBackstageLayout.Build(new(900, 500), ribbon.Font, 96, backstage);
        Assert.Equal(before, after);
        var separator = new RibbonMenuSeparatorItem { Id = "separator" }; backstage.FooterItems.Add(separator);
        Assert.Same(backstage, separator.Parent); Assert.Contains(separator, ribbon.AllElements);
        Assert.Throws<InvalidOperationException>(() => backstage.Items.Add(separator));
        Assert.Throws<ArgumentException>(() => backstage.Items.Add(new RibbonButtonItem { Id = separator.Id }));
        separator.Dispose(); Assert.Empty(backstage.FooterItems);
        var command = new RibbonButtonItem(); backstage.FooterItems.Add(command); ribbon.Dispose(); Assert.True(command.IsDisposed);
    });

    [Theory]
    [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Footer_is_pinned_above_bottom_padding_while_main_scrolls_and_short_hosts_fall_back(int dpi) => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl(); var backstage = ribbon.Backstage;
        for (int i = 0; i < 15; i++) backstage.Items.Add(new RibbonButtonItem { Text = "Command " + i });
        var mainRule = new RibbonMenuSeparatorItem(); backstage.Items.Insert(1, mainRule);
        var rule = new RibbonMenuSeparatorItem(); var page = new RibbonBackstagePageItem { Text = "Options" };
        backstage.FooterItems.AddRange(rule, page, new RibbonButtonItem { Visible = false, Text = "Hidden" });
        var layout = RibbonBackstageLayout.Build(new(1200, 1000), ribbon.Font, dpi, backstage);
        Assert.False(layout.FooterInline); Assert.Equal(layout.Navigation.Bottom, layout.Footer.Top);
        var first = layout.Arrange(backstage); var scrolled = layout.Arrange(backstage, 100);
        Assert.Equal(first.Single(slot => slot.Item == page).Bounds, scrolled.Single(slot => slot.Item == page).Bounds);
        Assert.Equal(first[0].Bounds.Top - 100, scrolled[0].Bounds.Top);
        Assert.Equal(RibbonLayoutEngine.Scale(9, dpi), first.Single(slot => slot.Item == mainRule).Bounds.Height);
        Assert.Equal(layout.Footer.Bottom - layout.Inset, first.Single(slot => slot.Item == page).Bounds.Bottom);
        Assert.DoesNotContain(first, slot => !slot.Item.Visible);
        var shortLayout = RibbonBackstageLayout.Build(new(1200, layout.Navigation.Top + layout.RowHeight), ribbon.Font, dpi, backstage);
        Assert.True(shortLayout.FooterInline); Assert.True(shortLayout.Footer.IsEmpty);
        int maxOffset = shortLayout.NavigationContentHeight - shortLayout.Navigation.Height;
        var last = shortLayout.Arrange(backstage, maxOffset).Last();
        Assert.Same(page, last.Item); Assert.Equal(last.Bounds, last.VisibleBounds);
        Assert.True(shortLayout.Navigation.Contains(last.VisibleBounds));
    });

    [Fact]
    public void Footer_caption_affects_auto_width_and_selection_fallback_crosses_sections() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl(); var backstage = ribbon.Backstage;
        var main = new RibbonBackstagePageItem(); var footer = new RibbonBackstagePageItem { Text = new string('W', 100) };
        backstage.Items.Add(main); var narrow = RibbonBackstageLayout.Build(new(1000, 500), ribbon.Font, 96, backstage);
        backstage.FooterItems.Add(footer); var wide = RibbonBackstageLayout.Build(new(1000, 500), ribbon.Font, 96, backstage);
        Assert.True(wide.Page.Left > narrow.Page.Left); Assert.Same(main, backstage.SelectedPage);
        main.Visible = false; Assert.Same(footer, backstage.SelectedPage);
        main.Visible = true; footer.Dispose(); Assert.Same(main, backstage.SelectedPage); Assert.Empty(backstage.FooterItems);
    });

    [Fact]
    public void Footer_content_types_initialize_at_end_init_and_remain_owned() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl(); ribbon.BeginInit();
        var page = new RibbonBackstagePageItem { ContentType = typeof(BackstageContentInitializationTests.Body), Visible = false };
        ribbon.Backstage.FooterItems.Add(page); Assert.Null(page.Content); ribbon.EndInit();
        var body = Assert.IsType<BackstageContentInitializationTests.Body>(page.Content);
        Assert.False(ribbon.IsBackstageOpen); ribbon.Dispose(); Assert.True(body.IsDisposed);
    });

    [Fact]
    public void Native_footer_keyboard_separators_and_resize_keep_live_page_handles_and_state() => FoundationTests.Sta(() =>
    {
        using var form = new Form { ClientSize = new(900, 600) };
        using var content = new TextBox { Text = "Native notes" };
        var ribbon = new RibbonControl { FileMenuMode = RibbonFileMenuMode.Backstage, AnimationLevel = RibbonAnimationLevel.None }; form.Controls.Add(ribbon); ribbon.FileMenu.Visible = true;
        var main = new RibbonBackstagePageItem { Text = "Info" };
        var footer = new RibbonBackstagePageItem { Text = "Options", KeyTip = "O", Content = content };
        var separator = new RibbonMenuSeparatorItem { KeyTip = "X" }; int clicks = 0;
        var command = new RibbonButtonItem { Text = "Exit", KeyTip = "E" }; command.Click += (_, _) => clicks++;
        ribbon.Backstage.Items.Add(main); ribbon.Backstage.FooterItems.AddRange(separator, footer, command);
        form.Show(); ribbon.IsBackstageOpen = true; Application.DoEvents();
        var surface = ribbon.BackstageSurface!; var all = Descendants(surface).ToArray();
        var rule = all.Single(control => control.AccessibleRole == AccessibleRole.Separator);
        Assert.False(rule.CanSelect); Assert.False(rule.TabStop);
        var option = all.Single(control => control is IButtonControl && control.Text == "Options");
        var exit = all.Single(control => control is IButtonControl && control.Text == "Exit");
        Assert.NotSame(option.Parent, all.Single(control => control is IButtonControl && control.Text == "Info").Parent);
        ribbon.ProcessBackstageKey(Keys.End); Assert.True(exit.Focused);
        ribbon.ProcessBackstageKey(Keys.Up); Assert.True(option.Focused);
        ribbon.ProcessBackstageKey(Keys.Up); Assert.Equal("Info", RibbonControl.FocusedControl(surface)!.Text);
        ribbon.ActivateBackstage(separator); separator.PerformClick(); Assert.True(ribbon.IsBackstageOpen); Assert.Equal(0, clicks);
        surface.ToggleKeyTips(); ribbon.ProcessBackstageKey(Keys.X); Assert.Equal(0, clicks); Assert.Same(main, ribbon.Backstage.SelectedPage);
        ribbon.ProcessBackstageKey(Keys.O); Assert.Same(footer, ribbon.Backstage.SelectedPage);
        var parent = content.Parent; var handle = content.Handle; content.Select(2, 4);
        form.ClientSize = new(900, 150); Application.DoEvents();
        Assert.Same(option.Parent, all.Single(control => control is IButtonControl && control.Text == "Info").Parent);
        option.Focus(); Assert.True(option.Focused); ribbon.ProcessBackstageKey(Keys.Down); Assert.True(exit.Focused);
        form.ClientSize = new(900, 600); Application.DoEvents();
        Assert.Same(parent, content.Parent); Assert.Equal(handle, content.Handle); Assert.Equal("Native notes", content.Text);
        Assert.Equal(2, content.SelectionStart); Assert.Equal(4, content.SelectionLength);
        surface.ToggleKeyTips(); ribbon.ProcessBackstageKey(Keys.E); Assert.Equal(1, clicks); Assert.False(ribbon.IsBackstageOpen);
    });

    private static IEnumerable<Control> Descendants(Control parent) => parent.Controls.Cast<Control>().SelectMany(control => new[] { control }.Concat(Descendants(control)));
}
