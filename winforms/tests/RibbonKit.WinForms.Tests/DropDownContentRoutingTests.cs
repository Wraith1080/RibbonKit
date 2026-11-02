using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class DropDownContentRoutingTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void QAT_overflow_and_source_switches_keep_exactly_one_native_body(bool split) => FoundationTests.Sta(() =>
    {
        using var fixture = new DropDownContentTests.Fixture(split); var ribbon = fixture.Ribbon; var source = fixture.Item;
        var reference = new RibbonQuickAccessDropDownReference { Source = source, Text = "Body" }; ribbon.QuickAccessItems.Add(reference);
        source.ShowDropDown(); var body = source.Content!; nint handle = body.Handle; int closed = 0; source.DropDownClosed += (_, _) => closed++;
        ribbon.Popups.Toggle(reference, ribbon, Rectangle.Empty);
        Assert.True(reference.IsDropDownOpen); Assert.Equal(1, closed); Assert.Single(ribbon.Popups.Surfaces); Assert.Same(ribbon.Popups.Top, body.Parent); Assert.Equal(handle, body.Handle);
        source.CloseDropDown(); Assert.Equal(2, closed); Assert.Null(body.Parent);
        ribbon.QuickAccessPosition = RibbonQuickAccessPosition.TabRow; ribbon.QuickAccessMaxWidth = 28;
        ribbon.Activate(ribbon.Snapshot.Targets.Single(target => target.Kind == Layout.TargetKind.QuickAccessOverflow));
        var parent = ribbon.Popups.Top!; Assert.False(parent.HasCustomContent);
        var target = parent.Targets.Single(target => ReferenceEquals(target.Element, reference) && target.DropDownPart == split);
        ribbon.Activate(target, parent); Assert.Equal(2, ribbon.Popups.Surfaces.Count()); Assert.True(reference.IsDropDownOpen); Assert.Same(ribbon.Popups.Top, body.Parent); Assert.Equal(handle, body.Handle);
        source.CloseDropDown(); Assert.Same(parent, ribbon.Popups.Top); Assert.Null(body.Parent); Assert.False(body.IsDisposed);
    });

    [Fact]
    public void Nested_body_returns_focus_to_its_real_parent_menu_and_exports_native_children() => FoundationTests.Sta(() =>
    {
        using var fixture = new DropDownContentTests.Fixture(); var ribbon = fixture.Ribbon;
        var outer = new RibbonDropDownItem { Text = "Parent" }; var nested = new RibbonDropDownItem { Text = "Body", ContentType = typeof(DropDownContentTests.Body) }; outer.Items.Add(nested); ribbon.Tabs[0].Groups[0].Items.Add(outer);
        outer.ShowDropDown(); var parent = ribbon.Popups.Top!; nested.ShowDropDown(); var surface = ribbon.Popups.Top!; var body = nested.Content!;
        Assert.Same(surface, body.Parent); Assert.Equal(2, ribbon.Popups.Surfaces.Count());
        Assert.True(surface.AccessibilityObject.GetChildCount() > 0);
        Assert.Contains(Enumerable.Range(0, surface.AccessibilityObject.GetChildCount()), index => ReferenceEquals(surface.AccessibilityObject.GetChild(index), body.AccessibilityObject));
        nested.CloseDropDown(); Assert.Same(parent, ribbon.Popups.Top); Assert.Same(nested, parent.FocusedElement); Assert.True(parent.ContainsFocus); Assert.Null(body.Parent);
    });

    [Fact]
    public void Constructor_failure_is_reported_before_Load_and_can_be_replaced() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl(); ribbon.BeginInit(); var item = new RibbonDropDownItem { ContentType = typeof(FailingBody) }; ribbon.CommandCatalog.Commands.Add(item);
        Assert.Throws<System.Reflection.TargetInvocationException>(() => ribbon.EndInit()); Assert.Null(item.Content); Assert.False(item.IsDropDownOpen);
        item.ContentType = typeof(DropDownContentTests.Body); Assert.IsType<DropDownContentTests.Body>(item.Content);
    });
    public sealed class FailingBody : UserControl { public FailingBody() => throw new InvalidOperationException("Application constructor failed."); }
}
