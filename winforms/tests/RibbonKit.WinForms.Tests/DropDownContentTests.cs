using System.ComponentModel;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class DropDownContentTests
{
    [Theory]
    [InlineData(false, false)] [InlineData(true, false)] [InlineData(false, true)] [InlineData(true, true)]
    public void Retained_body_opening_animates_without_a_native_drawing_hold_failure(bool split, bool created) => FoundationTests.Sta(() =>
    {
        using var fixture = new Fixture(split);
        var ribbon = fixture.Ribbon; var item = fixture.Item; var body = item.Content!;
        if (created) _ = body.Handle;
        ribbon.AnimationLevel = RibbonAnimationLevel.Subtle; ribbon.Motion.SystemPolicyOverride = true;
        var failures = new List<Exception>();
        void ObserveException(object? sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
        {
            if (e.Exception is Win32Exception && e.Exception.Message.StartsWith("Native child drawing hold failed:", StringComparison.Ordinal)) failures.Add(e.Exception);
        }
        AppDomain.CurrentDomain.FirstChanceException += ObserveException;
        try
        {
            for (int i = 0; i < 2; i++)
            {
                item.ShowDropDown();
                Assert.Empty(failures);
                var surface = ribbon.Popups.Top!; var window = (RibbonPopupWindow)surface.Parent!;
                Assert.NotNull(window.OpeningMotion); Assert.True(window.OpeningMotion.IsRunning);
                Assert.True(RibbonDrawingHold.HasOpacity(surface.Handle));
                window.SettleMotion(); Assert.False(RibbonDrawingHold.HasOpacity(surface.Handle));
                Assert.Same(body, item.Content); Assert.False(body.IsDisposed);
                item.CloseDropDown(); Assert.Null(body.Parent);
            }
        }
        finally { AppDomain.CurrentDomain.FirstChanceException -= ObserveException; }
    });

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void Native_body_Load_failure_or_owner_removal_leaves_no_popup_and_retains_the_body(bool throws) => FoundationTests.Sta(() =>
    {
        using var fixture = new Fixture(); var item = fixture.Item; var body = (Body)item.Content!;
        body.Load += (_, _) =>
        {
            if (throws) throw new InvalidOperationException("Application Load failed");
            item.Visible = false;
        };
        if (throws) Assert.Throws<InvalidOperationException>(item.ShowDropDown);
        else item.ShowDropDown();
        Assert.False(item.IsDropDownOpen); Assert.Null(fixture.Ribbon.Popups.Top);
        Assert.Null(body.Parent); Assert.False(body.IsDisposed);
    });

    [Theory]
    [InlineData(false, false)] [InlineData(true, false)] [InlineData(false, true)] [InlineData(true, true)]
    public void Types_exist_before_Load_including_nested_and_unavailable_items(bool split, bool init) => FoundationTests.Sta(() =>
    {
        using var form = new Form { ShowInTaskbar = false };
        var ribbon = new RibbonControl(); form.Controls.Add(ribbon); if (init) ribbon.BeginInit();
        var item = MakeItem(split); var hidden = new RibbonDropDownItem { ContentType = typeof(Body), Visible = false };
        item.Items.Add(hidden); var group = new RibbonGroup(); group.Items.Add(item); var tab = new RibbonTab(); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        if (init) { Assert.Null(item.Content); ribbon.EndInit(); Assert.IsType<Body>(item.Content); }
        bool loaded = false; form.Load += (_, _) => { loaded = true; Assert.IsType<Body>(item.Content); Assert.IsType<Body>(hidden.Content); Assert.False(item.IsDropDownOpen); };
        form.Show(); Assert.True(loaded); Assert.Null(item.Content!.Parent); Assert.Null(hidden.Content!.Parent);
        var original = item.Content; ribbon.BeginUpdate(); var late = new RibbonDropDownItem { ContentType = typeof(Body) }; group.Items.Add(late);
        Assert.Null(late.Content); ribbon.EndUpdate(); Assert.IsType<Body>(late.Content);
        item.ContentType = typeof(OtherBody); Assert.True(original.IsDisposed); Assert.IsType<OtherBody>(item.Content);
        var other = item.Content; item.ContentType = null; Assert.True(other!.IsDisposed); Assert.Null(item.Content);
    });

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void Reopening_and_QAT_reuse_body_handles_values_and_selection(bool split) => FoundationTests.Sta(() =>
    {
        using var fixture = new Fixture(split); var item = fixture.Item; var ribbon = fixture.Ribbon;
        item.ShowDropDown(); var body = Assert.IsType<Body>(item.Content); Assert.Same(body.Parent, ribbon.Popups.Top);
        Assert.Empty(ribbon.Popups.Top!.Targets); Assert.False(ribbon.Popups.Top.AutoScroll);
        Assert.Equal(body.Width + 2 * Scale(4, ribbon), ribbon.Popups.Top.Width);
        Assert.Equal(body.Height + 2 * Scale(4, ribbon), ribbon.Popups.Top.Height);
        body.Editor.Text = "Retained input"; body.Editor.Select(2, 5); var handle = body.Editor.Handle;
        item.CloseDropDown(); Assert.False(body.IsDisposed); Assert.Null(body.Parent);
        item.ShowDropDown(); Assert.Same(body, item.Content); Assert.Equal(handle, body.Editor.Handle); Assert.Equal(2, body.Editor.SelectionStart); Assert.Equal(5, body.Editor.SelectionLength);
        item.CloseDropDown();
        var reference = new RibbonQuickAccessDropDownReference { Source = item }; ribbon.QuickAccessItems.Add(reference);
        var otherTab = new RibbonTab { Text = "Other" }; ribbon.Tabs.Add(otherTab); ribbon.SelectedTab = otherTab;
        ribbon.Popups.Toggle(reference, ribbon, Rectangle.Empty); Assert.True(reference.IsDropDownOpen); Assert.Same(body.Parent, ribbon.Popups.Top); Assert.Same(otherTab, ribbon.SelectedTab);
        Assert.Equal(handle, body.Editor.Handle); Assert.Equal("Retained input", body.Editor.Text); item.CloseDropDown(); Assert.Null(body.Parent);
        item.Dispose(); Assert.True(body.IsDisposed);
    });

    [Fact]
    public void Borrowed_body_survives_close_replacement_and_ribbon_disposal_and_claims_include_Backstage() => FoundationTests.Sta(() =>
    {
        using var fixture = new Fixture(); var item = fixture.Item; item.ContentType = null;
        using var borrowed = new Body(); item.Content = borrowed;
        using var second = new RibbonDropDownItem(); using var page = new RibbonBackstagePageItem();
        Assert.Throws<ArgumentException>(() => second.Content = borrowed); Assert.Throws<ArgumentException>(() => page.Content = borrowed);
        Assert.Throws<InvalidOperationException>(() => item.ContentType = typeof(Body));
        item.ShowDropDown(); Assert.NotNull(borrowed.Parent); item.Content = null; Assert.Null(borrowed.Parent); Assert.False(borrowed.IsDisposed); Assert.False(item.IsDropDownOpen);
        page.Content = borrowed; Assert.Throws<ArgumentException>(() => item.Content = borrowed); page.Content = null;
        item.Content = borrowed; fixture.Ribbon.Dispose(); Assert.False(borrowed.IsDisposed); Assert.Null(borrowed.Parent);
    });

    [Theory]
    [InlineData(typeof(TextBox))] [InlineData(typeof(AbstractBody))] [InlineData(typeof(NoDefaultBody))] [InlineData(typeof(GenericBody<>))]
    public void Invalid_type_does_not_replace_valid_content(Type type) => FoundationTests.Sta(() =>
    {
        using var fixture = new Fixture(); var old = fixture.Item.Content;
        Assert.Throws<ArgumentException>(() => fixture.Item.ContentType = type); Assert.Same(old, fixture.Item.Content); Assert.False(old!.IsDisposed);
    });

    [Fact]
    public void Parent_disposed_shared_and_gallery_bodies_are_rejected() => FoundationTests.Sta(() =>
    {
        using var item = new RibbonDropDownItem(); using var parent = new Panel(); using var body = new Body(); parent.Controls.Add(body);
        Assert.Throws<ArgumentException>(() => item.Content = body); parent.Controls.Remove(body); body.Dispose(); Assert.Throws<ObjectDisposedException>(() => item.Content = body);
        using var gallery = new RibbonGalleryItem(); Assert.Throws<InvalidOperationException>(() => gallery.ContentType = typeof(Body));
        using var form = new Form(); Assert.Throws<ArgumentException>(() => item.Content = form);
    });

    [Fact]
    public void Clearing_content_restores_unchanged_menu_rows_and_split_primary_is_separate() => FoundationTests.Sta(() =>
    {
        using var fixture = new Fixture(true); var item = (RibbonSplitButtonItem)fixture.Item; int clicks = 0; item.Click += (_, _) => clicks++;
        var row = new RibbonButtonItem { Text = "Original menu" }; item.Items.Add(row);
        item.PerformClick(); Assert.Equal(1, clicks); Assert.False(item.IsDropDownOpen);
        item.ShowDropDown(); Assert.True(item.IsDropDownOpen); Assert.Equal(1, clicks); Assert.Empty(fixture.Ribbon.Popups.Top!.Targets);
        item.ContentType = null; Assert.False(item.IsDropDownOpen); item.ShowDropDown(); Assert.Contains(fixture.Ribbon.Popups.Top!.Targets, target => ReferenceEquals(target.Element, row)); Assert.Same(row, item.Items.Single());
    });

    [Fact]
    public void Canceled_opening_never_parents_body_and_disabling_or_external_disposal_closes_it() => FoundationTests.Sta(() =>
    {
        using var fixture = new Fixture(); var item = fixture.Item; var body = item.Content!;
        CancelEventHandler cancel = (_, e) => e.Cancel = true; item.DropDownOpening += cancel; item.ShowDropDown(); Assert.False(item.IsDropDownOpen); Assert.Null(body.Parent);
        item.DropDownOpening -= cancel; item.ShowDropDown(); item.Enabled = false; Assert.False(item.IsDropDownOpen); Assert.Null(body.Parent); Assert.False(body.IsDisposed);
        item.Enabled = true; item.ContentType = null; using var borrowed = new Body(); item.Content = borrowed; item.ShowDropDown(); borrowed.Dispose(); Assert.Null(item.Content); Assert.False(item.IsDropDownOpen);
    });

    [Fact]
    public void Body_resize_updates_popup_and_monitor_constraint_does_not_resize_the_body_or_add_scrollbars() => FoundationTests.Sta(() =>
    {
        using var fixture = new Fixture(); var item = fixture.Item; item.ShowDropDown(); var body = item.Content!; var surface = fixture.Ribbon.Popups.Top!;
        body.Size = new(390, 240); Assert.Equal(new Size(390 + 2 * Scale(4, fixture.Ribbon), 240 + 2 * Scale(4, fixture.Ribbon)), surface.Size);
        Assert.Equal(surface.Size, surface.Parent!.ClientSize - new Size(2, 2)); item.CloseDropDown();
        body.Size = new(1600, 1000); using var constrained = new RibbonPopupSurface(fixture.Ribbon, item, new(450, 300));
        Assert.True(constrained.Width <= 450 && constrained.Height <= 300); Assert.Equal(new Size(1600, 1000), body.Size); Assert.False(constrained.AutoScroll); Assert.False(constrained.HorizontalScroll.Visible); Assert.False(constrained.VerticalScroll.Visible);
        constrained.Dispose(); Assert.Null(body.Parent); Assert.False(body.IsDisposed);
    });

    [Fact]
    public void Native_Tab_validation_can_cancel_and_Enter_is_left_to_application_controls() => FoundationTests.Sta(() =>
    {
        using var fixture = new Fixture(); fixture.Item.ShowDropDown(); var surface = fixture.Ribbon.Popups.Top!; var body = (Body)fixture.Item.Content!;
        body.Editor.Focus(); int validations = 0; bool cancel = true; body.Editor.Validating += (_, e) => { validations++; e.Cancel = cancel; };
        Assert.True(surface.ProcessContentKey(Keys.Tab)); Assert.True(body.Editor.Focused); Assert.Equal(1, validations);
        cancel = false; Assert.True(surface.ProcessContentKey(Keys.Tab)); Assert.True(body.Choice.Focused); Assert.Equal(2, validations);
        Assert.False(surface.ProcessContentKey(Keys.Enter)); Assert.True(fixture.Item.IsDropDownOpen);
    });

    [Fact]
    public void Designer_initialization_never_constructs_content() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl(); ribbon.Site = new DesignSite(ribbon); ribbon.BeginInit(); var item = MakeItem(false); ribbon.CommandCatalog.Commands.Add(item); ribbon.EndInit(); ribbon.CreateControl(); Assert.Null(item.Content);
    });

    private static RibbonDropDownItem MakeItem(bool split) => split ? new RibbonSplitButtonItem { Text = "Custom split", ContentType = typeof(Body) } : new RibbonDropDownItem { Text = "Custom dropdown", ContentType = typeof(Body) };
    private static int Scale(int value, RibbonControl ribbon) => Layout.RibbonLayoutEngine.Scale(value, ribbon.DeviceDpi);
    internal sealed class Fixture : IDisposable
    {
        internal Form Form { get; } = new() { Width = 1100, Height = 650, ShowInTaskbar = false };
        internal RibbonControl Ribbon { get; } = new() { AnimationLevel = RibbonAnimationLevel.None };
        internal RibbonDropDownItem Item { get; }
        internal Fixture(bool split = false)
        {
            Item = MakeItem(split); Ribbon.BeginInit(); var tab = new RibbonTab { Text = "Home" }; var group = new RibbonGroup { Text = "Content" }; group.Items.Add(Item); tab.Groups.Add(group); Ribbon.Tabs.Add(tab); Form.Controls.Add(Ribbon); Ribbon.EndInit(); Form.Show(); Application.DoEvents();
        }
        public void Dispose() => Form.Dispose();
    }
    public sealed class Body : UserControl
    {
        internal TextBox Editor { get; } = new() { Location = new(12, 12), Width = 200, TabIndex = 0 };
        internal ComboBox Choice { get; } = new() { Location = new(12, 52), Width = 200, TabIndex = 1 };
        public Body() { Size = new(320, 180); Controls.AddRange([Editor, Choice]); Choice.Items.AddRange(["One", "Two"]); }
    }
    public sealed class OtherBody : UserControl { }
    public abstract class AbstractBody : UserControl { }
    public sealed class NoDefaultBody : UserControl { public NoDefaultBody(int value) { _ = value; } }
    public sealed class GenericBody<T> : UserControl { }
    private sealed class DesignSite(IComponent component) : ISite
    {
        public IComponent Component => component; public IContainer? Container => null; public bool DesignMode => true; public string? Name { get; set; } = "ribbon"; public object? GetService(Type serviceType) => null;
    }
}
