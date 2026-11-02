using System.ComponentModel;
using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class TemporaryGalleryPresenterTests
{
    private sealed class Fixture : IDisposable
    {
        internal readonly Form Form = new() { Width = 1200, Height = 650 };
        internal readonly RibbonControl Ribbon = new() { AnimationLevel = RibbonAnimationLevel.None };
        internal readonly RibbonGroup Group = new() { Text = "Styles" };
        internal readonly RibbonGalleryItem Gallery = new() { Text = "Styles", IsInRibbon = true, UseDropDownPresenterInQuickAccess = true };
        internal readonly List<InlineGalleryPresenterTests.View> Bodies = [], Popups = [];
        internal RibbonQuickAccessGroupReference? Reference;
        internal Fixture(bool reuse = false, bool throwOnTemporarySync = false)
        {
            Gallery.Items.AddRange(Enumerable.Range(0, 15).Select(i => new RibbonGalleryTile { Text = "Style " + i }).ToArray()); Gallery.SelectedItem = (RibbonGalleryTile)Gallery.Items[0];
            Gallery.InRibbonPresenterFactory = () =>
            {
                if (reuse && Bodies.Count > 0) return Bodies[0];
                var view = new InlineGalleryPresenterTests.View { ThrowOnSync = throwOnTemporarySync && Bodies.Count > 0 }; Bodies.Add(view); return view;
            };
            Gallery.DropDownPresenterFactory = () => { var view = new InlineGalleryPresenterTests.View(); Popups.Add(view); return view; };
            Group.Items.Add(Gallery); var tab = new RibbonTab { Text = "Home" }; tab.Groups.Add(Group); Ribbon.Tabs.Add(tab); Form.Controls.Add(Ribbon); Form.Show(); Form.Activate(); Application.DoEvents();
        }
        internal RibbonPopupSurface Open(int route)
        {
            // 0 collapsed, 1 collapsed with a QAT group reference, 2 direct QAT
            // group, 3 minimized tab, 4 QAT group through actual QAT overflow.
            if (route is 1 or 2 or 4)
            {
                Reference = new RibbonQuickAccessGroupReference { Source = Group }; Ribbon.QuickAccessItems.Add(Reference);
            }
            if (route is 0 or 1)
            {
                Form.Width = 240; Application.DoEvents();
                Assert.Contains(Ribbon.Snapshot.Targets, target => ReferenceEquals(target.Element, Group));
                Ribbon.Activate(Ribbon.Snapshot.Targets.Single(target => ReferenceEquals(target.Element, Group)));
            }
            else if (route == 3)
            {
                Ribbon.IsMinimized = true; Ribbon.Activate(Ribbon.Snapshot.Targets.Single(target => target.Kind == TargetKind.Tab));
            }
            else if (route == 4)
            {
                Ribbon.QuickAccessPosition = RibbonQuickAccessPosition.TabRow; Ribbon.QuickAccessMaxWidth = 28;
                Ribbon.Activate(Ribbon.Snapshot.Targets.Single(target => target.Kind == TargetKind.QuickAccessOverflow)); var parent = Ribbon.Popups.Top!;
                Ribbon.Activate(parent.Targets.Single(target => ReferenceEquals(target.Element, Reference)), parent);
            }
            else Ribbon.Activate(Ribbon.Snapshot.Targets.Single(target => ReferenceEquals(target.Element, Reference)));
            return Ribbon.Popups.Top!;
        }
        internal RibbonGalleryInlineSurface Surface(RibbonPopupSurface host) => Ribbon.GalleryStrips.InlineSurfaceFor(Gallery, host)!;
        public void Dispose() => Form.Dispose();
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)]
    public void Every_temporary_strip_uses_a_fresh_owned_inline_presenter_and_preserves_the_permanent_body(int route) => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); var permanent = Assert.Single(f.Bodies); nint handle = permanent.Handle, childHandle = permanent.Child.Handle;
        var host = f.Open(route); var surface = f.Surface(host); Assert.NotNull(surface); var temporary = Assert.IsType<InlineGalleryPresenterTests.View>(surface.PresenterControl); var context = temporary.Context;
        Assert.Same(host, surface.Parent); Assert.NotSame(permanent, temporary); Assert.Equal(RibbonGalleryPresentation.InRibbon, context.Presentation); Assert.Same(f.Gallery, context.Gallery);
        Assert.Equal(route is 2 or 4, context.IsQuickAccess); Assert.True(context.IsActive); Assert.False(permanent.Context.IsActive); Assert.False(temporary.InputDuringSync);
        Assert.Null(f.Ribbon.GalleryStrips.SurfaceFor(f.Gallery)); Assert.Equal(handle, permanent.Handle); Assert.Equal(childHandle, permanent.Child.Handle);
        f.Ribbon.Popups.CloseAll(true); Assert.True(temporary.IsDisposed); Assert.True(temporary.Child.IsDisposed); Assert.Equal(1, temporary.Disposals); Assert.False(context.IsActive); Assert.False(context.Commit((RibbonGalleryTile)f.Gallery.Items[1]));
        Assert.False(permanent.IsDisposed); Assert.Equal(handle, permanent.Handle); Assert.Equal(15, f.Gallery.Items.Count);
    });

    [Theory]
    [InlineData(false, false)] [InlineData(false, true)] [InlineData(true, false)] [InlineData(true, true)]
    public void Expansion_obeys_QAT_policy_and_cancel_restores_the_same_temporary_view_row_and_focus(bool qat, bool optIn) => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); f.Gallery.UseDropDownPresenterInQuickAccess = optIn; var host = f.Open(qat ? 2 : 0); var surface = f.Surface(host); var view = (InlineGalleryPresenterTests.View)surface.PresenterControl;
        surface.FocusView(); surface.Scroll(RibbonGalleryScroll.RowForward); var old = view.Context; int row = view.Row; nint handle = view.Handle;
        Assert.True(surface.ProcessKey(Keys.F4)); Assert.NotSame(host, f.Ribbon.Popups.Top); Assert.False(old.IsActive); Assert.False(view.Context.IsActive);
        if (!qat || optIn) { var popup = Assert.Single(f.Popups); Assert.Equal(qat, popup.Context.IsQuickAccess); Assert.Equal(RibbonGalleryPresentation.DropDown, popup.Context.Presentation); Assert.NotSame(view, popup); }
        else { Assert.Empty(f.Popups); Assert.False(f.Ribbon.Popups.Top!.HasGalleryPresenter); }
        f.Gallery.CloseDropDown(); Assert.Same(host, f.Ribbon.Popups.Top); Assert.False(view.IsDisposed); Assert.True(view.Context.IsActive); Assert.False(old.IsActive); Assert.Equal(handle, view.Handle); Assert.Equal(row, view.Row); Assert.True(view.Focused);
        f.Ribbon.Popups.CloseTop(true); Assert.True(view.IsDisposed);
    });

    [Theory]
    [InlineData(0)] [InlineData(2)] [InlineData(3)]
    public void Native_child_keys_and_wheel_dispatch_once_and_commit_closes_the_host_without_selecting_a_source_tab(int route) => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); var selected = f.Ribbon.SelectedTab;
        if (route == 2) { var other = new RibbonTab { Text = "Other" }; f.Ribbon.Tabs.Add(other); f.Ribbon.SelectedTab = other; selected = other; }
        var host = f.Open(route); var surface = f.Surface(host); var view = (InlineGalleryPresenterTests.View)surface.PresenterControl; surface.FocusView();
        using var input = new RibbonInputFilter(f.Ribbon); int commits = 0; f.Gallery.SelectionCommitted += (_, _) => commits++;
        var key = Message.Create(view.Child.Handle, 0x100, (nint)Keys.Down, 0); Assert.True(input.PreFilterMessage(ref key)); Assert.Equal(1, view.Navigations); Assert.Same(f.Gallery.Items[3], f.Gallery.PreviewItem);
        var wheel = Message.Create(view.Child.Handle, 0x20A, (nint)unchecked((int)0xff880000), 0); Assert.True(input.PreFilterMessage(ref wheel)); Assert.Equal(2, view.Row);
        key = Message.Create(view.Child.Handle, 0x100, (nint)Keys.Enter, 0); Assert.True(input.PreFilterMessage(ref key)); Assert.Equal(1, commits); Assert.True(view.IsDisposed); Assert.False(f.Ribbon.Popups.HasOpen); Assert.Same(selected, f.Ribbon.SelectedTab);
    });

    [Fact]
    public void Tab_moves_to_the_parent_menu_sibling_and_Escape_retires_the_temporary_context() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); var after = new RibbonButtonItem { Text = "After" }; f.Group.Items.Add(after); var host = f.Open(2); var surface = f.Surface(host); var view = (InlineGalleryPresenterTests.View)surface.PresenterControl;
        surface.FocusView(); surface.ProcessKey(Keys.Tab); Assert.Same(after, host.FocusedElement); Assert.True(host.Focused); Assert.False(view.IsDisposed);
        surface.FocusView(); var context = view.Context; using var input = new RibbonInputFilter(f.Ribbon); var key = Message.Create(view.Handle, 0x100, (nint)Keys.Escape, 0);
        Assert.True(input.PreFilterMessage(ref key)); Assert.True(view.IsDisposed); Assert.False(context.IsActive); Assert.False(f.Ribbon.Popups.HasOpen);
    });

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void Failed_or_reused_factories_unwind_the_host_without_disposing_the_permanent_view(bool reuse) => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(reuse, !reuse); var permanent = f.Bodies[0]; nint handle = permanent.Handle;
        Assert.Throws<InvalidOperationException>(() => f.Open(2)); Assert.False(f.Ribbon.Popups.HasOpen);
        Assert.False(permanent.IsDisposed); Assert.Equal(handle, permanent.Handle);
        f.Ribbon.QuickAccessItems.Remove(f.Reference!); Assert.True(permanent.Context.IsActive);
        if (!reuse) { Assert.Equal(2, f.Bodies.Count); Assert.True(f.Bodies[1].IsDisposed); Assert.Equal(1, f.Bodies[1].Disposals); }
    });

    [Fact]
    public void Validation_rejects_one_activation_and_source_disposal_invalidates_all_owned_views() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); var field = new RibbonTextBoxItem { Text = "Name", Value = "Before" }; f.Group.Items.Add(field); var host = f.Open(2); var surface = f.Surface(host); var view = (InlineGalleryPresenterTests.View)surface.PresenterControl;
        var editor = f.Ribbon.Editors.EditorFor(field)!; Assert.True(editor.Focus()); editor.Text = "After"; int validations = 0, commits = 0;
        CancelEventHandler reject = (_, e) => { validations++; e.Cancel = true; }; field.ValueValidating += reject; f.Gallery.SelectionCommitted += (_, _) => commits++;
        Assert.False(view.Context.Commit((RibbonGalleryTile)f.Gallery.Items[1])); Assert.Equal(1, validations); Assert.Equal(0, commits); Assert.False(view.IsDisposed);
        field.ValueValidating -= reject; var context = view.Context; f.Gallery.Dispose(); Assert.False(context.IsActive); Assert.True(view.IsDisposed); Assert.All(f.Bodies, body => Assert.True(body.IsDisposed)); Assert.Equal(0, commits);
    });

    [Fact]
    public void Native_accessible_bounds_and_motion_capture_include_the_custom_temporary_body() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); var host = f.Open(2); var surface = f.Surface(host); var view = (InlineGalleryPresenterTests.View)surface.PresenterControl; nint handle = view.Handle;
        var accessible = surface.AccessibilityObject; Assert.False(accessible.GetChild(0)!.Bounds.IsEmpty); Assert.True(accessible.GetChild(14)!.Bounds.IsEmpty);
        using var bitmap = new Bitmap(host.Width, host.Height); RibbonMotionScene.Capture(f.Ribbon, host, bitmap);
        var point = host.PointToClient(view.PointToScreen(new Point(8, view.Height - 8))); Assert.Equal(Color.Magenta.ToArgb(), bitmap.GetPixel(point.X, point.Y).ToArgb()); Assert.Equal(handle, view.Handle);
        var context = view.Context; f.Reference!.Source = null; Assert.False(context.IsActive); Assert.True(view.IsDisposed); Assert.False(f.Bodies[0].IsDisposed);
    });
}
