using System.Reflection;
using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class ApplicationMenuTests
{
    [Theory]
    [InlineData(RibbonDensity.Compact, 9, 1000, 700)] [InlineData(RibbonDensity.Touch, 9, 1000, 700)]
    [InlineData(RibbonDensity.Compact, 18, 360, 260)] [InlineData(RibbonDensity.Touch, 18, 360, 260)]
    public void Measured_columns_scroll_independently_and_footer_remains_reachable(RibbonDensity density, int points, int width, int height) => FoundationTests.Sta(() =>
    {
        using var font = new Font("Segoe UI", points); using var ribbon = new RibbonControl { Font = font, Density = density };
        for (int i = 0; i < 35; i++) { ribbon.FileMenu.Items.Add(new RibbonButtonItem { Text = "Long command " + i }); ribbon.ApplicationMenu.DefaultPane.Items.Add(new RibbonButtonItem { Text = "Document " + i }); }
        for (int i = 0; i < 8; i++) ribbon.ApplicationMenu.FooterItems.Add(new RibbonButtonItem { Text = "Footer " + i });
        using var surface = new RibbonPopupSurface(ribbon, ribbon.FileMenu, new(width, height));
        Assert.True(surface.IsApplicationMenu); var layout = surface.ApplicationLayout!;
        Assert.True(surface.Width <= width); Assert.True(surface.Height <= height);
        Assert.True(layout.Footer.Top >= layout.Navigation.Bottom); Assert.True(layout.Footer.Top >= layout.Pane.Bottom);
        Assert.Equal(2 + (layout.FooterExtent > layout.Footer.Height ? 1 : 0), surface.Controls.OfType<VScrollBar>().Count());
        var first = surface.Targets.First(t => t.Element == ribbon.FileMenu.Items[0]).Bounds;
        surface.FocusElement(ribbon.ApplicationMenu.FooterItems[^1]);
        var footer = surface.Targets.Single(t => t.Element == ribbon.ApplicationMenu.FooterItems[^1]);
        Assert.False(footer.VisibleBounds.IsEmpty); Assert.Equal(first, surface.Targets.First(t => t.Element == ribbon.FileMenu.Items[0]).Bounds);
        surface.FocusElement(ribbon.FileMenu.Items[^1]); Assert.False(surface.Targets.Single(t => t.Element == ribbon.FileMenu.Items[^1]).VisibleBounds.IsEmpty);
        surface.FocusElement(ribbon.ApplicationMenu.DefaultPane.Items[^1]); Assert.False(surface.Targets.Single(t => t.Element == ribbon.ApplicationMenu.DefaultPane.Items[^1]).VisibleBounds.IsEmpty);
    });

    [Fact]
    public void Pane_commands_split_primary_keyboard_and_footer_preserve_source_identity_and_invoke_once() => FoundationTests.Sta(() =>
    {
        using var form = new Form { Width = 1000, Height = 600 }; var ribbon = new RibbonControl { AnimationLevel = RibbonAnimationLevel.None }; form.Controls.Add(ribbon);
        ribbon.FileMenu.Visible = true; ribbon.Tabs.Add(new RibbonTab { Text = "Home" });
        var recent = new RibbonDropDownItem { Text = "Open", KeyTip = "O", DropDownHeader = "Open documents" };
        var split = new RibbonSplitButtonItem { Text = "Save", KeyTip = "S", DropDownKeyTip = "A", CheckOnClick = true };
        var doc = new RibbonButtonItem { Text = "Report", KeyTip = "R" }; recent.Items.Add(doc); split.Items.Add(new RibbonButtonItem { Text = "Save copy" });
        var options = new RibbonButtonItem { Text = "Options", KeyTip = "P" }; ribbon.ApplicationMenu.FooterItems.Add(options);
        ribbon.FileMenu.Items.AddRange(recent, split); int calls = 0; doc.Click += (_, _) => calls++; split.Click += (_, _) => { Assert.True(split.Checked); calls++; }; options.Click += (_, _) => calls++;
        form.Show(); Application.DoEvents(); ribbon.FileMenu.ShowDropDown(); var surface = ribbon.Popups.Top!;
        ribbon.Activate(surface.Targets.Single(t => t.Element == recent), surface);
        Assert.Same(surface, ribbon.Popups.Top); Assert.Same(recent, surface.ApplicationPane); Assert.True(recent.IsDropDownOpen);
        Assert.Contains(surface.Targets, t => ReferenceEquals(t.Element, doc));
        ribbon.Activate(surface.Targets.Single(t => t.Element == doc), surface); Assert.Equal(1, calls); Assert.False(ribbon.FileMenu.IsDropDownOpen);
        ribbon.FileMenu.ShowDropDown(); surface = ribbon.Popups.Top!; Assert.Same(ribbon.ApplicationMenu.DefaultPane, surface.ApplicationPane);
        ribbon.Activate(surface.Targets.Single(t => t.Element == split && t.DropDownPart), surface); Assert.Same(split, surface.ApplicationPane); Assert.Equal(1, calls);
        ribbon.Activate(surface.Targets.Single(t => t.Element == split && !t.DropDownPart), surface); Assert.Equal(2, calls); Assert.False(ribbon.Popups.HasOpen);
        ribbon.FileMenu.ShowDropDown(); surface = ribbon.Popups.Top!; ribbon.Activate(surface.Targets.Single(t => t.Element == options), surface); Assert.Equal(3, calls); Assert.False(ribbon.Popups.HasOpen);
        form.Close();
    });

    [Fact]
    public void Borrowed_controls_survive_switch_reopen_and_disposal_and_validation_vetoes_switch_and_action() => FoundationTests.Sta(() =>
    {
        using var body = new UserControl { Size = new(450, 500) }; var input = new TextBox { Text = "Retained text" }; body.Controls.Add(input);
        using var form = new Form { Width = 1000, Height = 600 }; var ribbon = new RibbonControl { AnimationLevel = RibbonAnimationLevel.None }; form.Controls.Add(ribbon);
        ribbon.FileMenu.Visible = true; var page = new RibbonDropDownItem { Text = "Native", Content = body }; var other = new RibbonDropDownItem { Text = "Other" }; var action = new RibbonButtonItem { Text = "Run" };
        ribbon.FileMenu.Items.AddRange(page, other, action); bool veto = false; input.Validating += (_, e) => e.Cancel = veto; int calls = 0; action.Click += (_, _) => calls++;
        form.Show(); Application.DoEvents(); ribbon.FileMenu.ShowDropDown(); page.ShowDropDown(); var surface = ribbon.Popups.Top!;
        Assert.Same(page, surface.ApplicationPane); input.Focus(); input.Select(2, 3); var handle = input.Handle; veto = true;
        other.ShowDropDown(); Assert.Same(page, surface.ApplicationPane);
        ribbon.Activate(surface.Targets.Single(t => t.Element == action), surface); Assert.Equal(0, calls); Assert.True(ribbon.Popups.HasOpen);
        veto = false; other.ShowDropDown(); Assert.Null(body.Parent); page.ShowDropDown(); Assert.Equal(handle, input.Handle); Assert.Equal("Retained text", input.Text);
        ribbon.FileMenu.CloseDropDown(); Assert.Null(body.Parent); Assert.False(body.IsDisposed);
        ribbon.FileMenu.ShowDropDown(); page.ShowDropDown(); Assert.Equal(handle, input.Handle);
        ribbon.Dispose(); Assert.Null(body.Parent); Assert.False(body.IsDisposed); form.Close();
    });

    [Theory]
    [InlineData("cancel")] [InlineData("close")] [InlineData("remove")]
    public void Pane_opening_callbacks_do_not_orphan_or_reopen_a_closed_file_surface(string behavior) => FoundationTests.Sta(() =>
    {
        using var form = new Form { Width = 900 }; var ribbon = new RibbonControl { AnimationLevel = RibbonAnimationLevel.None }; form.Controls.Add(ribbon); ribbon.FileMenu.Visible = true;
        var page = new RibbonDropDownItem { Text = "Open" }; ribbon.FileMenu.Items.Add(page);
        page.DropDownOpening += (_, e) => { if (behavior == "cancel") e.Cancel = true; if (behavior == "close") ribbon.FileMenu.CloseDropDown(); if (behavior == "remove") ribbon.FileMenu.Items.Remove(page); };
        form.Show(); Application.DoEvents(); ribbon.FileMenu.ShowDropDown(); page.ShowDropDown();
        if (behavior == "cancel") { Assert.True(ribbon.Popups.HasOpen); Assert.Same(ribbon.ApplicationMenu.DefaultPane, ribbon.Popups.Top!.ApplicationPane); }
        else Assert.False(ribbon.Popups.HasOpen);
        form.Close(); page.Dispose();
    });

    [Fact]
    public void Keytips_switch_panes_and_activate_right_commands_and_esc_restores_file() => FoundationTests.Sta(() =>
    {
        using var form = new Form { Width = 1000 }; var ribbon = new RibbonControl { AnimationLevel = RibbonAnimationLevel.None }; form.Controls.Add(ribbon); ribbon.FileMenu.Visible = true;
        var page = new RibbonDropDownItem { Text = "Open", KeyTip = "O" }; var item = new RibbonButtonItem { Text = "Report", KeyTip = "R" }; page.Items.Add(item); ribbon.FileMenu.Items.Add(page); int calls = 0; item.Click += (_, _) => calls++;
        form.Show(); Application.DoEvents(); ribbon.ToggleKeyTips(); Assert.True(ribbon.ProcessRibbonKey(Keys.F)); Assert.True(ribbon.ProcessRibbonKey(Keys.O));
        Assert.True(ribbon.FileMenu.IsDropDownOpen); Assert.True(ribbon.ProcessRibbonKey(Keys.R)); Assert.Equal(1, calls); Assert.False(ribbon.Popups.HasOpen);
        ribbon.FileMenu.ShowDropDown(); var surface = ribbon.Popups.Top!; surface.FocusElement(page);
        var process = typeof(RibbonPopupSurface).GetMethod("ProcessPopupKey", BindingFlags.Instance | BindingFlags.NonPublic)!;
        Assert.True((bool)process.Invoke(surface, [Keys.Right])!); Assert.Same(page, surface.ApplicationPane);
        Assert.True((bool)process.Invoke(surface, [Keys.Escape])!); Assert.False(ribbon.Popups.HasOpen); Assert.Same(ribbon.FileMenu, ribbon.FocusedElement); form.Close();
    });

    [Fact]
    public void Whole_body_override_and_exclusive_owned_default_collection_remain_compatible() => FoundationTests.Sta(() =>
    {
        using var body = new UserControl { Size = new(240, 180) }; using var ribbon = new RibbonControl(); ribbon.FileMenu.Content = body;
        using (var surface = new RibbonPopupSurface(ribbon, ribbon.FileMenu, new(1000, 700))) { Assert.False(surface.IsApplicationMenu); Assert.True(surface.HasCustomContent); }
        Assert.Null(body.Parent); Assert.False(body.IsDisposed); Assert.Equal(0, (int)RibbonFileMenuMode.CommandMenu);
        var action = new RibbonButtonItem(); ribbon.ApplicationMenu.FooterItems.Add(action);
        Assert.Throws<InvalidOperationException>(() => ribbon.ApplicationMenu.DefaultPane.Items.Add(action));
        Assert.Throws<ArgumentException>(() => ribbon.ApplicationMenu.DefaultPane.Id = action.Id);
        ribbon.Dispose(); Assert.True(action.IsDisposed);
    });

    [Fact]
    public void Application_validation_does_not_change_ordinary_custom_popup_outside_dismissal() => FoundationTests.Sta(() =>
    {
        using var body = new UserControl(); var input = new TextBox(); body.Controls.Add(input); input.Validating += (_, e) => e.Cancel = true;
        using var form = new Form { Width = 900 }; var ribbon = new RibbonControl { AnimationLevel = RibbonAnimationLevel.None }; form.Controls.Add(ribbon);
        var menu = new RibbonDropDownItem { Text = "Ordinary popup", Content = body }; var group = new RibbonGroup(); group.Items.Add(menu); var tab = new RibbonTab(); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        form.Show(); Application.DoEvents(); menu.ShowDropDown(); input.Focus();
        Assert.True(menu.IsDropDownOpen); Assert.False(ribbon.Popups.Top!.IsApplicationMenu);
        ribbon.Popups.OutsidePress(new(-10000, -10000));
        Assert.False(menu.IsDropDownOpen); Assert.Null(body.Parent); Assert.False(body.IsDisposed); form.Close();
    });
}
