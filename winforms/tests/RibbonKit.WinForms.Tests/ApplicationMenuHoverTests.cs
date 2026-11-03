using System.Reflection;
using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class ApplicationMenuHoverTests
{
    private static void Move(RibbonPopupSurface surface, Point point) => typeof(RibbonPopupSurface).GetMethod("OnMouseMove", BindingFlags.Instance | BindingFlags.NonPublic)!
        .Invoke(surface, [new MouseEventArgs(MouseButtons.None, 0, point.X, point.Y, 0)]);
    private static void Hover(RibbonPopupSurface surface, RibbonItem item, bool arrow = false)
    {
        var bounds = surface.Targets.Single(t => t.Element == item && t.DropDownPart == arrow).VisibleBounds;
        Assert.False(bounds.IsEmpty); Move(surface, new(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2));
    }

    [Theory] [InlineData(RibbonDensity.Compact)] [InlineData(RibbonDensity.Touch)]
    public void Row_entry_selects_without_invoking_and_plain_entry_restores_default_while_transit_is_neutral(RibbonDensity density) => FoundationTests.Sta(() =>
    {
        using var form = new Form { Width = 1000 }; var ribbon = new RibbonControl { AnimationLevel = RibbonAnimationLevel.None, Density = density }; form.Controls.Add(ribbon); ribbon.FileMenu.Visible = true;
        var plain = new RibbonButtonItem { Text = "Save" }; var drop = new RibbonDropDownItem { Text = "Print", DropDownHeader = "Print document" };
        var split = new RibbonSplitButtonItem { Text = "Save As" }; var nested = new RibbonDropDownItem { Text = "Export" }; nested.Items.Add(new RibbonButtonItem()); split.Items.Add(nested);
        var disabled = new RibbonButtonItem { Text = "Disabled", Enabled = false }; var separator = new RibbonMenuSeparatorItem();
        ribbon.FileMenu.Items.AddRange(plain, drop, split, separator, disabled); ribbon.ApplicationMenu.FooterItems.Add(new RibbonButtonItem { Text = "Options" });
        int actions = 0, openings = 0, closings = 0; split.Click += (_, _) => actions++; plain.Click += (_, _) => actions++; split.DropDownOpening += (_, _) => openings++; split.DropDownClosed += (_, _) => closings++;
        form.Show(); Application.DoEvents(); ribbon.FileMenu.ShowDropDown(); var surface = ribbon.Popups.Top!;
        Assert.Same(ribbon.ApplicationMenu.DefaultPane, surface.ApplicationPane);
        Hover(surface, split); Assert.Same(split, surface.ApplicationPane); Assert.Equal(0, actions); Assert.Equal(1, openings);
        Hover(surface, split, true); Assert.Equal(1, openings);
        var pane = surface.ApplicationLayout!.Pane; Move(surface, new(pane.Left + 2, pane.Top + 2)); Assert.Same(split, surface.ApplicationPane);
        Move(surface, new(surface.ApplicationLayout.Navigation.Right, surface.ApplicationLayout.Navigation.Top + 2));
        Hover(surface, separator); Hover(surface, disabled); Assert.Same(split, surface.ApplicationPane);
        typeof(RibbonPopupSurface).GetMethod("OnMouseLeave", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(surface, [EventArgs.Empty]); Assert.Same(split, surface.ApplicationPane);
        nested.ShowDropDown(); Assert.Equal(2, ribbon.Popups.Surfaces.Count());
        Hover(surface, plain); Assert.Same(ribbon.ApplicationMenu.DefaultPane, surface.ApplicationPane); Assert.False(split.IsDropDownOpen); Assert.False(nested.IsDropDownOpen); Assert.Equal(1, closings); Assert.Equal(0, actions);
        Hover(surface, drop); Assert.Same(drop, surface.ApplicationPane);
        Assert.Equal(surface.ApplicationLayout.Navigation.Right + 1, surface.ApplicationLayout.Header.Left);
        ribbon.FileMenu.CloseDropDown(); ribbon.FileMenu.ShowDropDown(); Assert.Same(ribbon.ApplicationMenu.DefaultPane, ribbon.Popups.Top!.ApplicationPane); form.Close();
    });

    [Fact]
    public void Hover_switch_and_default_reset_respect_native_validation_and_retain_the_body() => FoundationTests.Sta(() =>
    {
        using var body = new UserControl(); var input = new TextBox { Text = "Keep this" }; body.Controls.Add(input);
        using var form = new Form { Width = 1000 }; var ribbon = new RibbonControl { AnimationLevel = RibbonAnimationLevel.None }; form.Controls.Add(ribbon); ribbon.FileMenu.Visible = true;
        var page = new RibbonDropDownItem { Text = "Native", Content = body }; var other = new RibbonDropDownItem { Text = "Other" }; var plain = new RibbonButtonItem { Text = "New" }; ribbon.FileMenu.Items.AddRange(page, other, plain);
        bool veto = false; input.Validating += (_, e) => e.Cancel = veto;
        form.Show(); Application.DoEvents(); ribbon.FileMenu.ShowDropDown(); var surface = ribbon.Popups.Top!; Hover(surface, page); input.Focus(); var handle = input.Handle; veto = true;
        Hover(surface, other); Assert.Same(page, surface.ApplicationPane); Hover(surface, plain); Assert.Same(page, surface.ApplicationPane); Assert.NotNull(body.Parent);
        veto = false; Hover(surface, other); Hover(surface, plain); Assert.Same(ribbon.ApplicationMenu.DefaultPane, surface.ApplicationPane); Assert.Null(body.Parent);
        Hover(surface, page); Assert.Equal(handle, input.Handle); Assert.Equal("Keep this", input.Text); Assert.False(body.IsDisposed); form.Close();
    });

    [Fact]
    public void Hover_selection_preserves_navigation_scroll_position() => FoundationTests.Sta(() =>
    {
        using var form = new Form { Width = 1000, Height = 360 }; var ribbon = new RibbonControl { AnimationLevel = RibbonAnimationLevel.None }; form.Controls.Add(ribbon); ribbon.FileMenu.Visible = true;
        for (int i = 0; i < 30; i++) ribbon.FileMenu.Items.Add(new RibbonButtonItem { Text = "Command " + i });
        var drop = new RibbonDropDownItem { Text = "Formats" }; var plain = new RibbonButtonItem { Text = "Save" }; ribbon.FileMenu.Items.AddRange(drop, plain);
        form.Show(); Application.DoEvents(); ribbon.FileMenu.ShowDropDown(); var surface = ribbon.Popups.Top!; surface.FocusElement(plain);
        int offset = surface.Controls.OfType<VScrollBar>().Single(b => b.Name == "Application menu navigation scroll").Value; Assert.True(offset > 0);
        Hover(surface, drop); Assert.Same(drop, surface.ApplicationPane); Assert.Equal(offset, surface.Controls.OfType<VScrollBar>().Single(b => b.Name == "Application menu navigation scroll").Value);
        Hover(surface, plain); Assert.Same(ribbon.ApplicationMenu.DefaultPane, surface.ApplicationPane); Assert.Equal(offset, surface.Controls.OfType<VScrollBar>().Single(b => b.Name == "Application menu navigation scroll").Value); form.Close();
    });
}
