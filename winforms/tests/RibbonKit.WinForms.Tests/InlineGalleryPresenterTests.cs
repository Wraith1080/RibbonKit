using System.ComponentModel;
using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class InlineGalleryPresenterTests
{
    private sealed class Fixture : IDisposable
    {
        internal readonly Form Form = new() { Width = 1250, Height = 650 };
        internal readonly RibbonControl Ribbon = new() { AnimationLevel = RibbonAnimationLevel.None };
        internal readonly RibbonGalleryItem Gallery = new() { Text = "Styles", IsInRibbon = true };
        internal readonly List<View> Inline = [], Popups = [];
        internal Fixture(bool show = true)
        {
            var tab = new RibbonTab { Text = "Home" }; var group = new RibbonGroup { Text = "Styles" };
            group.Items.Add(Gallery); tab.Groups.Add(group); Ribbon.Tabs.Add(tab); Form.Controls.Add(Ribbon);
            Gallery.Items.AddRange(Enumerable.Range(0, 15).Select(i => new RibbonGalleryTile { Text = "Style " + i }).ToArray());
            Gallery.SelectedItem = (RibbonGalleryTile)Gallery.Items[0];
            Gallery.InRibbonPresenterFactory = () => { var view = new View(); Inline.Add(view); return view; };
            Gallery.DropDownPresenterFactory = () => { var view = new View(); Popups.Add(view); return view; };
            if (show) Show();
        }
        internal void Show() { Form.Show(); Form.Activate(); Application.DoEvents(); }
        internal RibbonGalleryInlineSurface Surface => Ribbon.GalleryStrips.InlineSurfaceFor(Gallery)!;
        public void Dispose() => Form.Dispose();
    }

    public sealed class View : UserControl, IRibbonGalleryPresenter
    {
        internal RibbonGalleryPresenterContext Context = null!;
        internal int Row, Navigations, Disposals;
        internal bool InputDuringSync;
        internal bool ThrowOnSync;
        internal readonly Button Child = new() { Text = "Native child", TabStop = false, Size = new(80, 24) };
        public View() { Controls.Add(Child); TabStop = true; }
        private RibbonGalleryTile[] Tiles => Context.Gallery.Items.OfType<RibbonGalleryTile>().Where(t => t.Visible).ToArray();
        public void Synchronize(RibbonGalleryPresenterContext context)
        {
            Context = context;
            if (ThrowOnSync) throw new InvalidOperationException("Failed view");
            Row = Math.Clamp(Row, 0, Math.Max(0, (Tiles.Length - 1) / 3));
            if (Tiles.Length > 0) InputDuringSync = context.Commit(Tiles[0]);
        }
        public override Size GetPreferredSize(Size proposedSize) => new(300, 150);
        public Rectangle GetItemBounds(RibbonGalleryTile item)
        {
            int i = Array.IndexOf(Tiles, item), width = Context.Viewport.Width / 3;
            return i < 0 ? Rectangle.Empty : new(i % 3 * width, (i / 3 - Row) * Context.Viewport.Height, width, Context.Viewport.Height);
        }
        public RibbonGalleryTile? HitTest(Point point) => Tiles.FirstOrDefault(t => GetItemBounds(t).Contains(point));
        public RibbonGalleryTile? Navigate(RibbonGalleryTile? current, Keys key)
        {
            Navigations++; var available = Tiles.Where(t => t.EffectiveEnabled).ToArray(); if (available.Length == 0) return null;
            int i = Math.Max(0, Array.IndexOf(available, current));
            return key switch
            {
                Keys.Home => available[0], Keys.End => available[^1],
                Keys.Up or Keys.PageUp => available[Math.Max(0, i - 3)], Keys.Down or Keys.PageDown => available[Math.Min(available.Length - 1, i + 3)],
                _ => available[Math.Clamp(i + (key == Keys.Left ? -1 : 1), 0, available.Length - 1)]
            };
        }
        public void Reveal(RibbonGalleryTile item) { int i = Array.IndexOf(Tiles, item); if (i >= 0) Row = i / 3; }
        public bool CanScrollBackward => Row > 0;
        public bool CanScrollForward => Row < (Tiles.Length - 1) / 3;
        public void ScrollView(RibbonGalleryScroll amount)
        {
            Row = Math.Clamp(Row + (amount is RibbonGalleryScroll.RowBackward or RibbonGalleryScroll.PageBackward ? -1 : 1), 0, Math.Max(0, (Tiles.Length - 1) / 3));
            Context.NotifyViewChanged();
        }
        internal void Pointer(Point down, Point up, MouseButtons button = MouseButtons.Left)
        { OnMouseDown(new(button, 1, down.X, down.Y, 0)); OnMouseUp(new(button, 1, up.X, up.Y, 0)); }
        protected override void Dispose(bool disposing) { if (disposing && !IsDisposed) Disposals++; base.Dispose(disposing); }
        protected override void OnPaint(PaintEventArgs e) { base.OnPaint(e); e.Graphics.FillRectangle(Brushes.Magenta, 4, Math.Max(0, Height - 12), 36, 8); }
    }

    [Fact]
    public void Inline_creation_is_lazy_and_hidden_retained_controls_receive_fresh_contexts() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(show: false); Assert.Empty(f.Inline); Assert.Empty(f.Popups);
        f.Show(); var view = Assert.Single(f.Inline); var context = view.Context; var handle = view.Handle;
        Assert.Equal(RibbonGalleryPresentation.InRibbon, context.Presentation); Assert.True(context.IsActive); Assert.False(view.InputDuringSync);
        f.Surface.Scroll(RibbonGalleryScroll.RowForward); Assert.Equal(1, view.Row);
        var other = new RibbonTab { Text = "Other" }; f.Ribbon.Tabs.Add(other); var home = f.Ribbon.SelectedTab;
        f.Ribbon.SelectedTab = other;
        Assert.False(view.Visible); Assert.False(view.IsDisposed); Assert.False(context.IsActive);
        Assert.False(context.Commit((RibbonGalleryTile)f.Gallery.Items[0])); context.NotifyViewChanged();
        f.Ribbon.SelectedTab = home;
        Assert.Same(view, Assert.Single(f.Inline)); Assert.Equal(handle, view.Handle); Assert.Equal(1, view.Row);
        Assert.NotSame(context, view.Context); Assert.False(context.IsActive); Assert.True(view.Context.IsActive);
        f.Gallery.Visible = false; Assert.False(view.Context.IsActive);
        f.Gallery.SelectedItem = (RibbonGalleryTile)f.Gallery.Items[^1]; f.Gallery.Visible = true;
        Assert.Equal(4, view.Row); Assert.Same(view, Assert.Single(f.Inline));
        f.Gallery.IsInRibbon = false; Assert.True(view.IsDisposed); Assert.Equal(1, view.Disposals); Assert.Equal(15, f.Gallery.Items.Count);
    });

    [Fact]
    public void Inline_and_expanded_views_share_tiles_but_not_controls_contexts_or_scroll_and_cancel_preserves_row() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); var inline = Assert.Single(f.Inline); var parent = inline.Parent;
        f.Ribbon.FocusElement(f.Gallery); f.Surface.Scroll(RibbonGalleryScroll.RowForward); var old = inline.Context;
        f.Gallery.ShowDropDown(); var popup = Assert.Single(f.Popups);
        Assert.NotSame(inline, popup); Assert.Same(f.Gallery, popup.Context.Gallery); Assert.Same(f.Gallery, inline.Context.Gallery);
        Assert.Equal(RibbonGalleryPresentation.DropDown, popup.Context.Presentation); Assert.True(inline.Visible); Assert.Same(parent, inline.Parent);
        Assert.False(old.IsActive); Assert.False(inline.Context.IsActive); Assert.True(popup.Context.IsActive);
        var last = (RibbonGalleryTile)f.Gallery.Items[^1]; Assert.True(popup.Context.Preview(last)); Assert.False(old.Preview(null)); Assert.Same(last, f.Gallery.PreviewItem);
        f.Gallery.CloseDropDown(); Assert.True(popup.IsDisposed); Assert.False(inline.IsDisposed); Assert.Equal(1, inline.Row);
        Assert.True(inline.Context.IsActive); Assert.False(old.IsActive); Assert.True(inline.Focused);
        f.Gallery.ShowDropDown(); Assert.Equal(2, f.Popups.Count); Assert.NotSame(popup, f.Popups[1]);
    });

    [Fact]
    public void Selection_is_shared_without_commit_and_reentrant_activation_is_rejected() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); var view = Assert.Single(f.Inline); var first = (RibbonGalleryTile)f.Gallery.Items[0]; var last = (RibbonGalleryTile)f.Gallery.Items[^1];
        int commits = 0; bool nested = true;
        f.Gallery.SelectionCommitted += (_, _) => { commits++; nested = view.Context.Commit(first); };
        f.Gallery.SelectedItem = last; Assert.Equal(4, view.Row); Assert.Equal(0, commits);
        Assert.True(view.Context.Commit(first)); Assert.Equal(1, commits); Assert.False(nested); Assert.Equal(0, view.Row);
        Assert.True(view.Context.Commit(first)); Assert.Equal(2, commits); Assert.False(view.IsDisposed);
        using var foreign = new RibbonGalleryTile(); Assert.False(view.Context.Commit(foreign));
        first.Enabled = false; Assert.False(view.Context.Preview(first)); Assert.False(view.Context.Commit(first));
        first.Enabled = true; f.Gallery.Items.Remove(first); Assert.False(view.Context.Commit(first)); Assert.Null(f.Gallery.SelectedItem);
        Assert.Equal(2, commits); Assert.False(view.InputDuringSync);
    });

    [Fact]
    public void Validation_cancellation_and_reentrant_replacement_do_not_commit() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); var field = new RibbonTextBoxItem { Text = "Name", Value = "Original" };
        f.Ribbon.Tabs[0].Groups[0].Items.Add(field); var view = f.Inline[0]; var editor = f.Ribbon.Editors.EditorFor(field)!;
        Assert.True(editor.Focus()); editor.Text = "Edited"; int validations = 0, commits = 0;
        f.Gallery.SelectionCommitted += (_, _) => commits++;
        CancelEventHandler cancel = (_, e) => { validations++; e.Cancel = true; }; field.ValueValidating += cancel;
        Assert.False(view.Context.Commit((RibbonGalleryTile)f.Gallery.Items[1])); Assert.Equal(1, validations); Assert.Equal(0, commits);
        field.ValueValidating -= cancel; var context = view.Context;
        field.ValueValidating += (_, _) => { validations++; f.Gallery.InRibbonPresenterFactory = null; };
        Assert.False(context.Commit((RibbonGalleryTile)f.Gallery.Items[1])); Assert.Equal(2, validations); Assert.Equal(0, commits);
        Assert.True(view.IsDisposed); Assert.False(context.IsActive); Assert.Same(f.Gallery.Items[0], f.Gallery.SelectedItem);
    });

    [Fact]
    public void Native_descendant_keys_and_wheel_route_once_and_tab_exits_the_inline_gallery() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); var view = f.Inline[0]; f.Ribbon.FocusElement(f.Gallery);
        using var input = new RibbonInputFilter(f.Ribbon); int commits = 0; f.Gallery.SelectionCommitted += (_, _) => commits++;
        var key = Message.Create(view.Child.Handle, 0x100, (nint)Keys.Down, 0); Assert.True(input.PreFilterMessage(ref key));
        Assert.Equal(1, view.Navigations); Assert.Same(f.Gallery.Items[3], f.Gallery.PreviewItem); Assert.Same(f.Gallery.Items[0], f.Gallery.SelectedItem);
        key = Message.Create(view.Child.Handle, 0x100, (nint)Keys.Enter, 0); Assert.True(input.PreFilterMessage(ref key)); Assert.Equal(1, commits);
        var wheel = Message.Create(view.Child.Handle, 0x20A, -120 << 16, 0); Assert.True(input.PreFilterMessage(ref wheel)); Assert.Equal(2, view.Row);
        for (int i = 0; i < 10; i++) f.Surface.Scroll(RibbonGalleryScroll.PageForward);
        Assert.False(view.CanScrollForward); Assert.True(view.CanScrollBackward); Assert.Equal(4, view.Row);
        key = Message.Create(view.Child.Handle, 0x100, (nint)Keys.Tab, 0); Assert.True(input.PreFilterMessage(ref key)); Assert.Null(f.Gallery.PreviewItem);
    });

    [Fact]
    public void Root_pointer_requires_matching_left_press_release_and_clipped_available_tile() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); var view = f.Inline[0]; int commits = 0; f.Gallery.SelectionCommitted += (_, _) => commits++;
        view.Pointer(new(5, 5), new(-5, -5)); view.Pointer(new(5, 5), new(5, 5), MouseButtons.Right); Assert.Equal(0, commits);
        view.Pointer(new(5, 5), new(5, 5)); Assert.Equal(1, commits); Assert.False(view.IsDisposed);
        f.Gallery.Enabled = false; var inactive = view.Context; view.Pointer(new(5, 5), new(5, 5)); Assert.Equal(1, commits);
        f.Gallery.Enabled = true; Assert.False(inactive.IsActive); Assert.True(view.Context.IsActive);
    });

    [Fact]
    public void Display_refresh_retains_native_views_and_replaces_only_context_and_constrains_minimum_size() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); var view = f.Inline[0]; var handle = view.Handle; var childHandle = view.Child.Handle; var old = view.Context;
        view.MinimumSize = new(4000, 3000); f.Ribbon.SyncEditors();
        Assert.True(view.Width >= 4000); Assert.True(view.Context.Viewport.Width < 1000); Assert.True(view.Context.Viewport.Height < 300);
        f.Ribbon.Density = RibbonDensity.Touch; Assert.Equal(RibbonDensity.Touch, view.Context.Density); Assert.False(old.IsActive);
        old = view.Context; using var font = new Font(f.Ribbon.Font.FontFamily, 12); f.Ribbon.Font = font;
        Assert.Same(font, view.Context.Font); Assert.False(old.IsActive); Assert.Equal(handle, view.Handle); Assert.Equal(childHandle, view.Child.Handle);
        Assert.Same(view, Assert.Single(f.Inline)); Assert.Equal(f.Ribbon.DeviceDpi, view.Context.Dpi);
    });

    [Fact]
    public void Invalid_classes_conflicts_reset_and_final_detach_preserve_source_and_dispose_views_once() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); var view = f.Inline[0]; var factory = f.Gallery.InRibbonPresenterFactory;
        Assert.Throws<InvalidOperationException>(() => f.Gallery.InRibbonPresenterType = typeof(View));
        Assert.Throws<ArgumentException>(() => f.Gallery.InRibbonPresenterType = typeof(UserControl)); Assert.Same(factory, f.Gallery.InRibbonPresenterFactory);
        f.Gallery.InRibbonPresenterFactory = null; Assert.True(view.IsDisposed); Assert.NotNull(f.Ribbon.GalleryStrips.SurfaceFor(f.Gallery));
        f.Gallery.InRibbonPresenterType = typeof(View); var typed = Assert.IsType<View>(f.Surface.PresenterControl);
        var context = typed.Context; var group = (RibbonGroup)f.Gallery.Parent!; group.Items.Remove(f.Gallery);
        Assert.True(typed.IsDisposed); Assert.False(context.IsActive); Assert.False(f.Gallery.IsDisposed); Assert.Equal(15, f.Gallery.Items.Count);
        Assert.Equal(DesignerSerializationVisibility.Hidden, TypeDescriptor.GetProperties(f.Gallery)[nameof(f.Gallery.InRibbonPresenterFactory)]!.SerializationVisibility);
        Assert.True(TypeDescriptor.GetProperties(f.Gallery)[nameof(f.Gallery.InRibbonPresenterType)]!.IsBrowsable);
        Assert.Null(f.Gallery.InRibbonPresenterFactory); Assert.Equal(typeof(View), f.Gallery.InRibbonPresenterType);
    });

    [Fact]
    public void Popup_cannot_take_an_already_owned_inline_control_even_if_application_unparents_it() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); var view = f.Inline[0]; view.Parent = null;
        f.Gallery.DropDownPresenterFactory = () => view;
        Assert.Throws<InvalidOperationException>(() => f.Gallery.ShowDropDown());
        Assert.False(view.IsDisposed); Assert.False(f.Gallery.IsDropDownOpen); Assert.Equal(15, f.Gallery.Items.Count);
    });

    [Fact]
    public void Failed_initial_sync_disposes_the_partial_native_tree_without_disposing_source_items() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); var old = f.Inline[0]; var failed = new View { ThrowOnSync = true };
        Assert.Throws<InvalidOperationException>(() => f.Gallery.InRibbonPresenterFactory = () => failed);
        Assert.True(old.IsDisposed); Assert.True(failed.IsDisposed); Assert.True(failed.Child.IsDisposed);
        Assert.False(failed.Context.IsActive); Assert.Equal(15, f.Gallery.Items.Count); Assert.Same(f.Gallery.Items[0], f.Gallery.SelectedItem);
        f.Gallery.InRibbonPresenterFactory = null;
    });

    [Fact]
    public void Qat_gallery_opt_out_stays_builtin_while_group_bodies_use_separate_custom_controls() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); var view = f.Inline[0]; var parent = view.Parent; var handle = view.Handle;
        var reference = new RibbonQuickAccessDropDownReference { Source = f.Gallery }; f.Ribbon.QuickAccessItems.Add(reference);
        f.Ribbon.Activate(f.Ribbon.Snapshot.Targets.Single(t => ReferenceEquals(t.Element, reference)));
        Assert.False(f.Ribbon.Popups.Top!.HasGalleryPresenter); Assert.Empty(f.Popups); Assert.True(view.Visible); Assert.False(view.Context.IsActive);
        f.Ribbon.Popups.CloseAll(true);
        var groupReference = new RibbonQuickAccessGroupReference { Source = (RibbonGroup)f.Gallery.Parent! }; f.Ribbon.QuickAccessItems.Add(groupReference);
        f.Ribbon.Activate(f.Ribbon.Snapshot.Targets.Single(t => ReferenceEquals(t.Element, groupReference)));
        var host = f.Ribbon.Popups.Top!; var temporary = f.Ribbon.GalleryStrips.InlineSurfaceFor(f.Gallery, host); Assert.NotNull(temporary); Assert.Same(host, temporary.Parent);
        Assert.False(view.IsDisposed); Assert.Same(parent, view.Parent); Assert.False(view.Context.IsActive);
        Assert.NotSame(view, temporary.PresenterControl); Assert.Equal(2, f.Inline.Count);
        f.Ribbon.Popups.CloseAll(true); f.Ribbon.QuickAccessItems.Remove(groupReference);
        Assert.True(temporary.IsDisposed); Assert.True(view.Visible); Assert.Equal(handle, view.Handle); Assert.Same(view, f.Inline[0]);
    });

    [Fact]
    public void Accessible_tiles_have_clipped_bounds_shared_selection_and_one_validated_action_with_inert_stale_children() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); var root = f.Surface.AccessibilityObject;
        var first = root.GetChild(0)!; var last = root.GetChild(14)!;
        Assert.Equal(18, root.GetChildCount()); Assert.False(first.Bounds.IsEmpty); Assert.True(last.Bounds.IsEmpty);
        Assert.True(first.State.HasFlag(AccessibleStates.Selected)); Assert.True(last.State.HasFlag(AccessibleStates.Offscreen));
        int commits = 0; f.Gallery.SelectionCommitted += (_, _) => commits++;
        first.DoDefaultAction(); Assert.Equal(1, commits);
        f.Gallery.SelectedItem = (RibbonGalleryTile)f.Gallery.Items[^1];
        Assert.False(last.Bounds.IsEmpty); Assert.True(first.Bounds.IsEmpty); Assert.Same(last, root.GetSelected()); Assert.Equal(1, commits);
        f.Gallery.IsInRibbon = false; first.DoDefaultAction(); Assert.Equal(1, commits); Assert.True(first.Bounds.IsEmpty);
    });

    [Fact]
    public void Source_disposal_and_reentrant_factory_detach_release_all_owned_native_children() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); var view = f.Inline[0]; var context = view.Context;
        f.Gallery.Dispose(); Assert.True(view.IsDisposed); Assert.True(view.Child.IsDisposed); Assert.False(context.IsActive);
        using var other = new Fixture(); var created = new View(); var group = (RibbonGroup)other.Gallery.Parent!;
        other.Gallery.InRibbonPresenterFactory = () => { group.Items.Remove(other.Gallery); return created; };
        Assert.True(created.IsDisposed); Assert.True(created.Child.IsDisposed); Assert.False(other.Gallery.IsDisposed);
        Assert.Null(other.Ribbon.GalleryStrips.InlineSurfaceFor(other.Gallery));
    });

    [Fact]
    public void Source_reordering_reveals_the_same_selected_tile_without_committing_or_recreating_views() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); var view = f.Inline[0]; var selected = (RibbonGalleryTile)f.Gallery.Items[6]; f.Gallery.SelectedItem = selected;
        int commits = 0; f.Gallery.SelectionCommitted += (_, _) => commits++;
        f.Gallery.Items.Insert(0, new RibbonGalleryTile { Text = "Inserted" });
        Assert.Equal(2, view.Row); Assert.Same(selected, f.Gallery.SelectedItem);
        f.Gallery.Items.Insert(0, new RibbonGalleryTile { Text = "Another" }); f.Gallery.Items.Insert(0, new RibbonGalleryTile { Text = "Third" });
        Assert.Equal(3, view.Row); Assert.Same(view, Assert.Single(f.Inline)); Assert.Equal(0, commits);
    });

    [Fact]
    public void Motion_capture_includes_the_real_custom_body_and_keeps_native_control_identity() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); var view = f.Inline[0]; var handle = view.Handle; var childHandle = view.Child.Handle;
        using var bitmap = new Bitmap(f.Ribbon.Width, f.Ribbon.Height);
        RibbonMotionScene.Capture(f.Ribbon, f.Ribbon, bitmap);
        var origin = f.Ribbon.PointToClient(view.PointToScreen(new Point(8, view.Height - 8)));
        Assert.Equal(Color.Magenta.ToArgb(), bitmap.GetPixel(origin.X, origin.Y).ToArgb());
        Assert.Equal(handle, view.Handle); Assert.Equal(childHandle, view.Child.Handle);
        f.Ribbon.SetBackdropBodyMasked(true); f.Ribbon.SetBackdropBodyMasked(false);
        Assert.Equal(handle, view.Handle); Assert.Equal(childHandle, view.Child.Handle); Assert.True(view.Context.IsActive);
    });

    [Fact]
    public void Pointer_focus_validation_cancellation_abandons_the_press_without_a_second_validation_or_commit() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); var field = new RibbonTextBoxItem { Text = "Name", Value = "Original" };
        f.Ribbon.Tabs[0].Groups[0].Items.Add(field); var editor = f.Ribbon.Editors.EditorFor(field)!;
        Assert.True(editor.Focus()); editor.Text = "Edited"; int validations = 0, commits = 0;
        CancelEventHandler reject = (_, e) => { validations++; e.Cancel = true; }; field.ValueValidating += reject;
        f.Gallery.SelectionCommitted += (_, _) => commits++;
        f.Inline[0].Pointer(new(5, 5), new(5, 5));
        Assert.Equal(1, validations); Assert.Equal(0, commits); Assert.True(editor.Focused); Assert.Same(f.Gallery.Items[0], f.Gallery.SelectedItem);
        field.ValueValidating -= reject; field.ValueValidating += (_, _) => validations++;
        f.Inline[0].Pointer(new(5, 5), new(5, 5)); Assert.Equal(2, validations); Assert.Equal(1, commits); Assert.True(f.Inline[0].Focused);
    });
}
