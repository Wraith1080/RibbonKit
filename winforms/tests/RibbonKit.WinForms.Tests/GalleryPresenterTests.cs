using System.ComponentModel;
using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class GalleryPresenterTests
{
    private sealed class Fixture : IDisposable
    {
        internal readonly Form Form = new() { StartPosition = FormStartPosition.Manual, Bounds = new(180, 120, 900, 580) };
        internal readonly RibbonControl Ribbon = new();
        internal readonly RibbonGalleryItem Gallery = new() { Text = "Custom styles" };
        internal readonly List<View> Views = [];
        internal Fixture()
        {
            var tab = new RibbonTab { Text = "Home" }; var group = new RibbonGroup { Text = "Styles" };
            group.Items.Add(Gallery); tab.Groups.Add(group); Ribbon.Tabs.Add(tab); Form.Controls.Add(Ribbon);
            for (int i = 0; i < 16; i++) Gallery.Items.Add(new RibbonGalleryTile { Text = "Style " + i });
            Gallery.DropDownPresenterFactory = () => { var view = new View(); Views.Add(view); return view; };
            Form.Show(); Application.DoEvents();
        }
        internal RibbonPopupSurface Open()
        { Gallery.ShowDropDown(); Assert.NotNull(Ribbon.Popups.Top); return Ribbon.Popups.Top!; }
        public void Dispose() => Form.Dispose();
    }
    private sealed class View : UserControl, IRibbonGalleryPresenter
    {
        internal RibbonGalleryPresenterContext Context = null!;
        internal int Offset, Navigations, Disposals;
        internal RibbonGalleryTile? Revealed;
        internal bool ThrowOnSync;
        internal bool PaintMarker;
        internal int CaptureDelay;
        internal bool TryInputDuringSync, ActiveDuringSync, CommitDuringSync;
        private RibbonGalleryTile[] Tiles => Context.Gallery.Items.OfType<RibbonGalleryTile>().Where(t => t.Visible).ToArray();
        public void Synchronize(RibbonGalleryPresenterContext context)
        {
            Context = context; if (ThrowOnSync) throw new InvalidOperationException("Presenter failure");
            if (TryInputDuringSync) { ActiveDuringSync = context.IsActive; CommitDuringSync = context.Commit((RibbonGalleryTile)context.Gallery.Items[0]); }
            Invalidate();
        }
        public override Size GetPreferredSize(Size proposedSize) => new(230, 120);
        public Rectangle GetItemBounds(RibbonGalleryTile item)
        { int index = Array.IndexOf(Tiles, item); return index < 0 ? Rectangle.Empty : new(index % 2 * 110, index / 2 * 40 - Offset, 105, 36); }
        public RibbonGalleryTile? HitTest(Point point) => Tiles.FirstOrDefault(tile => GetItemBounds(tile).Contains(point));
        public RibbonGalleryTile? Navigate(RibbonGalleryTile? current, Keys key)
        {
            Navigations++; var available = Tiles.Where(t => t.Enabled).ToArray(); if (available.Length == 0) return null;
            int index = Math.Max(0, Array.IndexOf(available, current));
            return key switch
            {
                Keys.Home => available[0], Keys.End => available[^1],
                Keys.Up => available[Math.Max(0, index - 2)], Keys.Down => available[Math.Min(available.Length - 1, index + 2)],
                Keys.PageUp => available[Math.Max(0, index - 6)], Keys.PageDown => available[Math.Min(available.Length - 1, index + 6)],
                _ => available[Math.Clamp(index + (key is Keys.Left or (Keys.Tab | Keys.Shift) ? -1 : 1), 0, available.Length - 1)]
            };
        }
        public void Reveal(RibbonGalleryTile item)
        {
            Revealed = item; var bounds = GetItemBounds(item);
            if (bounds.Top < 0) Offset = Math.Max(0, Offset + bounds.Top);
            else if (bounds.Bottom > Context.Viewport.Height) Offset += bounds.Bottom - Context.Viewport.Height;
        }
        public bool CanScrollBackward => Offset > 0;
        public bool CanScrollForward => Offset < Math.Max(0, (Tiles.Length + 1) / 2 * 40 - Context.Viewport.Height);
        public void ScrollView(RibbonGalleryScroll amount)
        {
            int step = amount is RibbonGalleryScroll.PageBackward or RibbonGalleryScroll.PageForward ? Context.Viewport.Height : 40;
            Offset = Math.Clamp(Offset + (amount is RibbonGalleryScroll.RowBackward or RibbonGalleryScroll.PageBackward ? -step : step), 0, Math.Max(0, (Tiles.Length + 1) / 2 * 40 - Context.Viewport.Height));
            Context.NotifyViewChanged(); Invalidate();
        }
        internal void Pointer(Point down, Point up)
        { OnMouseDown(new(MouseButtons.Left, 1, down.X, down.Y, 0)); OnMouseUp(new(MouseButtons.Left, 1, up.X, up.Y, 0)); }
        internal void RepeatedMouseMove(Point location) => OnMouseMove(new(MouseButtons.None, 0, location.X, location.Y, 0));
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (CaptureDelay > 0) Thread.Sleep(CaptureDelay); // Simulate an expensive application-owned first drawing.
            if (PaintMarker) e.Graphics.FillRectangle(Brushes.Magenta, 7, 9, 71, 35);
        }
        protected override void Dispose(bool disposing) { if (disposing && !IsDisposed) Disposals++; base.Dispose(disposing); }
    }

    [Fact]
    public void Factory_is_lazy_cancelable_and_each_popup_owns_a_fresh_view_with_inert_old_contexts() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); Assert.Empty(f.Views);
        CancelEventHandler cancel = (_, e) => e.Cancel = true; f.Gallery.DropDownOpening += cancel;
        f.Gallery.ShowDropDown(); Assert.Empty(f.Views); Assert.False(f.Gallery.IsDropDownOpen); f.Gallery.DropDownOpening -= cancel;
        var first = f.Open(); var view = Assert.Single(f.Views); var context = view.Context;
        Assert.True(first.HasGalleryPresenter); Assert.True(context.IsActive); Assert.Same(f.Gallery, context.Gallery);
        Assert.Same(f.Ribbon.Font, context.Font); Assert.Equal(f.Ribbon.DeviceDpi, context.Dpi);
        Assert.Equal(DesignerSerializationVisibility.Hidden, TypeDescriptor.GetProperties(f.Gallery)[nameof(f.Gallery.DropDownPresenterFactory)]!.SerializationVisibility);
        f.Gallery.CloseDropDown(); Assert.True(view.IsDisposed); Assert.Equal(1, view.Disposals); Assert.False(context.IsActive);
        Assert.False(context.Preview((RibbonGalleryTile)f.Gallery.Items[0])); Assert.False(context.Commit((RibbonGalleryTile)f.Gallery.Items[0])); context.NotifyViewChanged();
        f.Open(); Assert.Equal(2, f.Views.Count); Assert.NotSame(view, f.Views[1]); Assert.True(f.Views[1].Context.IsActive);
        Assert.Equal(16, f.Gallery.Items.Count);
    });

    [Fact]
    public void Programmatic_selection_reveals_without_commit_and_presenter_preview_commit_use_source_events_once() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); var first = (RibbonGalleryTile)f.Gallery.Items[0]; var last = (RibbonGalleryTile)f.Gallery.Items[^1];
        f.Gallery.SelectedItem = first; var events = new List<string>();
        f.Gallery.ItemPreview += (_, e) => events.Add("preview:" + e.Item.Text); f.Gallery.ItemPreviewCancelled += (_, _) => events.Add("cancel");
        f.Gallery.SelectionCommitted += (_, _) => events.Add("commit");
        f.Open(); var view = f.Views[0]; f.Gallery.SelectedItem = last;
        Assert.Same(last, view.Revealed); Assert.Empty(events);
        Assert.True(view.Context.Preview(first)); Assert.True(view.Context.Preview(first)); Assert.Same(last, f.Gallery.SelectedItem);
        Assert.True(view.Context.Commit(first)); Assert.Same(first, f.Gallery.SelectedItem); Assert.False(f.Gallery.IsDropDownOpen);
        Assert.Equal(new[] { "preview:" + first.Text, "commit" }, events); Assert.Equal(1, view.Disposals);
        f.Open(); events.Clear(); Assert.True(f.Views[^1].Context.Commit(first)); Assert.Equal(new[] { "commit" }, events);
    });

    [Fact]
    public void A_view_cancels_only_its_preview_and_dismissal_releases_the_requesting_view() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); var tile = (RibbonGalleryTile)f.Gallery.Items[0]; int cancels = 0;
        f.Gallery.ItemPreviewCancelled += (_, _) => cancels++;
        f.Open(); var context = f.Views[0].Context;
        Assert.True(context.Preview(tile)); f.Gallery.CloseDropDown(); Assert.Equal(1, cancels); Assert.Null(f.Gallery.PreviewItem);
        f.Open(); context = f.Views[^1].Context; var otherView = new object(); f.Gallery.Preview(tile, otherView);
        Assert.True(context.Preview(null)); f.Gallery.CloseDropDown(); Assert.Same(tile, f.Gallery.PreviewItem); Assert.Equal(1, cancels);
        f.Gallery.CancelPreview(otherView); Assert.Equal(2, cancels);
    });

    [Fact]
    public void Keyboard_enters_selected_tile_navigates_once_and_commits_through_the_existing_input_filter() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); f.Gallery.SelectedItem = (RibbonGalleryTile)f.Gallery.Items[3];
        var surface = f.Open(); var view = f.Views[0]; Assert.Same(f.Gallery.SelectedItem, view.Context.FocusedItem);
        int commits = 0, previews = 0; f.Gallery.SelectionCommitted += (_, _) => commits++; f.Gallery.ItemPreview += (_, _) => previews++;
        using var input = new RibbonInputFilter(f.Ribbon);
        var down = Message.Create(view.Handle, 0x100, (int)Keys.Down, 0); Assert.True(input.PreFilterMessage(ref down));
        Assert.Equal(1, view.Navigations); Assert.Equal(1, previews); Assert.Same(f.Gallery.Items[5], surface.FocusedElement);
        var enter = Message.Create(view.Handle, 0x100, (int)Keys.Enter, 0); Assert.True(input.PreFilterMessage(ref enter));
        Assert.Equal(1, commits); Assert.Same(f.Gallery.Items[5], f.Gallery.SelectedItem); Assert.True(view.IsDisposed);
    });

    [Fact]
    public void Row_page_and_wheel_scrolling_update_bounds_and_button_availability_without_selection_changes() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); var surface = f.Open(); var view = f.Views[0];
        Assert.False(view.CanScrollBackward); Assert.True(view.CanScrollForward);
        var back = surface.Controls.OfType<Button>().Single(button => button.AccessibleName == "Previous gallery row");
        var next = surface.Controls.OfType<Button>().Single(button => button.AccessibleName == "Next gallery row");
        Assert.False(back.Enabled); Assert.True(next.Enabled); next.PerformClick(); Assert.Equal(40, view.Offset); Assert.True(back.Enabled);
        surface.ScrollPresenter(RibbonGalleryScroll.PageForward); Assert.Equal(160, view.Offset);
        using var input = new RibbonInputFilter(f.Ribbon);
        var wheel = Message.Create(view.Handle, 0x20A, 120 << 16, 0); Assert.True(input.PreFilterMessage(ref wheel)); Assert.Equal(120, view.Offset);
        for (int i = 0; i < 5; i++) surface.ScrollPresenter(RibbonGalleryScroll.PageForward);
        Assert.False(next.Enabled); Assert.True(back.Enabled);
        var last = (RibbonGalleryTile)f.Gallery.Items[^1]; var target = surface.Targets.Single(t => ReferenceEquals(t.Element, last));
        Assert.False(target.VisibleBounds.IsEmpty); Assert.Null(f.Gallery.SelectedItem); Assert.Equal(16, surface.Targets.Count);
        Assert.True(surface.Width < 1000); Assert.True(surface.Height < 600);
    });

    [Fact]
    public void Foreign_disabled_removed_and_disposed_items_are_rejected_and_factory_changes_close_the_old_view() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); var surface = f.Open(); var view = f.Views[0]; var tile = (RibbonGalleryTile)f.Gallery.Items[0];
        using var foreign = new RibbonGalleryTile(); Assert.False(view.Context.Preview(foreign)); Assert.False(view.Context.Commit(foreign));
        tile.Enabled = false; Assert.False(view.Context.Commit(tile)); Assert.False(view.Context.Preview(tile)); Assert.Null(view.Context.FocusedItem); Assert.Null(surface.FocusedElement);
        tile.Enabled = true; tile.Visible = false; Assert.True(view.IsDisposed); Assert.False(view.Context.Commit(tile));
        tile.Visible = true; f.Open(); view = f.Views[^1]; f.Gallery.Items.Remove(tile); Assert.False(view.Context.Commit(tile)); Assert.True(view.IsDisposed);
        f.Gallery.Items.Add(tile); f.Open(); view = f.Views[^1]; tile.Dispose(); Assert.True(view.IsDisposed); Assert.False(view.Context.Commit(tile));
        f.Open(); view = f.Views[^1]; f.Gallery.DropDownPresenterFactory = null; Assert.True(view.IsDisposed);
        surface = f.Open(); Assert.False(surface.HasGalleryPresenter);
    });

    [Fact]
    public void Qat_and_inline_presentations_keep_the_built_in_path_and_do_not_construct_custom_views() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); f.Gallery.IsInRibbon = true; var reference = new RibbonQuickAccessDropDownReference { Source = f.Gallery };
        f.Ribbon.QuickAccessItems.Add(reference); f.Ribbon.SyncEditors(); Assert.Empty(f.Views);
        var target = f.Ribbon.Snapshot.Targets.First(t => ReferenceEquals(t.Element, reference)); f.Ribbon.Activate(target);
        Assert.False(f.Ribbon.Popups.Top!.HasGalleryPresenter); Assert.Empty(f.Views); f.Ribbon.Popups.CloseAll(false);
        f.Open(); Assert.Single(f.Views); Assert.True(f.Ribbon.Popups.Top!.HasGalleryPresenter);
    });

    [Fact]
    public void Failed_initialization_disposes_owned_views_but_rejects_parented_controls_without_borrowing_them() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); View? failed = null;
        f.Gallery.DropDownPresenterFactory = () => failed = new View { ThrowOnSync = true };
        Assert.Throws<InvalidOperationException>(() => f.Gallery.ShowDropDown()); Assert.NotNull(failed); Assert.True(failed.IsDisposed); Assert.False(f.Gallery.IsDropDownOpen);
        using var parent = new Panel(); var borrowed = new View(); parent.Controls.Add(borrowed);
        f.Gallery.DropDownPresenterFactory = () => borrowed; Assert.Throws<InvalidOperationException>(() => f.Gallery.ShowDropDown()); Assert.False(borrowed.IsDisposed); Assert.Same(parent, borrowed.Parent);
        f.Gallery.DropDownPresenterFactory = () => { var view = new View(); f.Gallery.Visible = false; return failed = view; };
        f.Gallery.ShowDropDown(); Assert.True(failed!.IsDisposed); Assert.False(f.Gallery.IsDropDownOpen);
    });

    [Fact]
    public void Presenter_focus_reports_reveal_without_preview_or_commit_and_escape_disposes_one_popup_level() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); var original = (RibbonGalleryTile)f.Gallery.Items[0]; var last = (RibbonGalleryTile)f.Gallery.Items[^1]; f.Gallery.SelectedItem = original;
        var surface = f.Open(); var view = f.Views[0]; int previews = 0, commits = 0;
        f.Gallery.ItemPreview += (_, _) => previews++; f.Gallery.SelectionCommitted += (_, _) => commits++;
        Assert.True(view.Context.FocusItem(last)); Assert.Same(last, view.Context.FocusedItem); Assert.Same(last, surface.FocusedElement);
        Assert.False(surface.Targets.Single(t => ReferenceEquals(t.Element, last)).VisibleBounds.IsEmpty);
        Assert.Equal(0, previews); Assert.Equal(0, commits); Assert.Same(original, f.Gallery.SelectedItem);
        using var input = new RibbonInputFilter(f.Ribbon); var escape = Message.Create(view.Handle, 0x100, (int)Keys.Escape, 0);
        Assert.True(input.PreFilterMessage(ref escape)); Assert.True(view.IsDisposed); Assert.False(view.Context.FocusItem(last)); Assert.Same(original, f.Gallery.SelectedItem);
    });

    [Fact]
    public void Small_work_area_constrains_the_native_viewport_and_scroll_button_bounds() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); using var surface = new RibbonPopupSurface(f.Ribbon, f.Gallery, new(140, 150));
        var view = f.Views[0]; Assert.InRange(surface.Width, 1, 140); Assert.InRange(surface.Height, 1, 150);
        Assert.True(view.Context.Viewport.Width < 140); Assert.True(view.Context.Viewport.Height < 150);
        Assert.All(surface.Controls.OfType<Button>(), button => Assert.True(surface.ClientRectangle.Contains(button.Bounds)));
    });

    [Fact]
    public void Pointer_activation_requires_a_matching_press_and_release_then_commits_once() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); f.Open(); var view = f.Views[0]; int commits = 0; f.Gallery.SelectionCommitted += (_, _) => commits++;
        view.Pointer(new(5, 5), new(-10, -10)); Assert.Equal(0, commits); Assert.True(f.Gallery.IsDropDownOpen);
        view.Pointer(new(5, 5), new(115, 5)); Assert.Equal(0, commits); Assert.True(f.Gallery.IsDropDownOpen);
        view.Pointer(new(5, 5), new(5, 5)); Assert.Equal(1, commits); Assert.Same(f.Gallery.Items[0], f.Gallery.SelectedItem); Assert.True(view.IsDisposed);
    });

    [Fact]
    public void Editor_validation_can_cancel_commit_or_invalidate_the_view_without_executing_a_tile() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); var field = new RibbonTextBoxItem { Text = "Name", Value = "Original" };
        f.Ribbon.Tabs[0].Groups[0].Items.Add(field); f.Open(); var view = f.Views[0]; var editor = f.Ribbon.Editors.EditorFor(field)!;
        Assert.True(editor.Focus()); editor.Text = "Edited";
        int validations = 0, commits = 0; f.Gallery.SelectionCommitted += (_, _) => commits++;
        CancelEventHandler cancel = (_, e) => { validations++; e.Cancel = true; }; field.ValueValidating += cancel;
        Assert.False(view.Context.Commit((RibbonGalleryTile)f.Gallery.Items[0])); Assert.Equal(1, validations); Assert.Equal(0, commits); Assert.True(f.Gallery.IsDropDownOpen);
        field.ValueValidating -= cancel;
        field.ValueValidating += (_, _) => { validations++; f.Gallery.Enabled = false; };
        Assert.False(view.Context.Commit((RibbonGalleryTile)f.Gallery.Items[0])); Assert.Equal(2, validations); Assert.Equal(0, commits); Assert.True(view.IsDisposed);
        Assert.Null(f.Gallery.SelectedItem);
    });

    [Fact]
    public void Qat_group_gallery_submenus_keep_the_builtin_view_until_qat_presenters_are_implemented() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); var group = f.Ribbon.Tabs[0].Groups[0]; var reference = new RibbonQuickAccessGroupReference { Source = group };
        f.Ribbon.QuickAccessItems.Add(reference); var target = f.Ribbon.Snapshot.Targets.First(t => ReferenceEquals(t.Element, reference)); f.Ribbon.Activate(target);
        Assert.True(f.Ribbon.Popups.HasOpen); f.Gallery.ShowDropDown(); Assert.False(f.Ribbon.Popups.Top!.HasGalleryPresenter); Assert.Empty(f.Views);
    });

    [Fact]
    public void Synchronization_reports_a_current_context_but_cannot_turn_programmatic_selection_into_a_commit() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); f.Open(); var view = f.Views[0]; view.TryInputDuringSync = true; int commits = 0;
        f.Gallery.SelectionCommitted += (_, _) => commits++; var last = (RibbonGalleryTile)f.Gallery.Items[^1]; f.Gallery.SelectedItem = last;
        Assert.True(view.ActiveDuringSync); Assert.False(view.CommitDuringSync); Assert.Equal(0, commits); Assert.Same(last, f.Gallery.SelectedItem); Assert.True(f.Gallery.IsDropDownOpen);
    });

    [Fact]
    public void Native_scroll_buttons_own_their_entire_opaque_background_and_draw_only_arrow_ink() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); var surface = f.Open();
        foreach (var button in surface.Controls.OfType<Button>())
        {
            using var image = new Bitmap(button.Width, button.Height); button.DrawToBitmap(image, button.ClientRectangle);
            var glyph = new Rectangle(button.Width / 2 - 9, button.Height / 2 - 7, 18, 14);
            var background = new List<Color>();
            for (int y = 0; y < image.Height; y++) for (int x = 0; x < image.Width; x++) if (!glyph.Contains(x, y)) background.Add(image.GetPixel(x, y));
            Assert.True(background.All(color => color.ToArgb() == f.Ribbon.Theme.Surface.ToArgb()), "Native button background must not expose stale text or an unpainted backing.");
        }
    });

    [Fact]
    public void Repaint_mouse_messages_at_a_stationary_pointer_do_not_replace_keyboard_preview() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); var surface = f.Open(); var view = f.Views[0];
        Assert.True(surface.ProcessPresenterKey(Keys.End)); var last = (RibbonGalleryTile)f.Gallery.Items[^1];
        Assert.Same(last, f.Gallery.PreviewItem); view.RepeatedMouseMove(new(5, 5)); Assert.Same(last, f.Gallery.PreviewItem);
    });

    [Fact]
    public void Directional_page_endpoint_and_tab_navigation_follow_presenter_layout_without_selecting() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); var original = (RibbonGalleryTile)f.Gallery.Items[0]; f.Gallery.SelectedItem = original;
        var surface = f.Open(); var view = f.Views[0]; int moves = 0;
        foreach (var (key, index) in new[] { (Keys.Home, 0), (Keys.Down, 2), (Keys.PageDown, 8), (Keys.Up, 6), (Keys.End, 15),
            (Keys.PageUp, 9), (Keys.Left, 8), (Keys.Tab, 9), (Keys.Tab | Keys.Shift, 8), (Keys.Home, 0), (Keys.Right, 1) })
        {
            Assert.True(surface.ProcessPresenterKey(key)); Assert.Equal(++moves, view.Navigations);
            Assert.Same(f.Gallery.Items[index], view.Context.FocusedItem); Assert.Same(original, f.Gallery.SelectedItem);
            Assert.False(surface.Targets.Single(target => ReferenceEquals(target.Element, f.Gallery.Items[index])).VisibleBounds.IsEmpty);
        }
    });

    [Theory]
    [InlineData(RibbonTabContentAnimation.Fade)]
    [InlineData(RibbonTabContentAnimation.SlideDown)]
    public void Opening_animation_contains_the_native_usercontrol_body_before_the_live_view_is_uncovered(RibbonTabContentAnimation style) => FoundationTests.Sta(() =>
    {
        using var f = new Fixture();
        f.Gallery.DropDownPresenterFactory = () => { var view = new View { PaintMarker = true }; f.Views.Add(view); return view; };
        f.Ribbon.TabContentAnimation = style; f.Ribbon.AnimationLevel = RibbonAnimationLevel.Expressive; f.Ribbon.Motion.SystemPolicyOverride = true;
        var surface = f.Open(); var view = f.Views[0]; var window = (RibbonPopupWindow)surface.Parent!;
        var motion = window.OpeningMotion!; Assert.True(motion.IsRunning);
        var directory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../artifacts/gallery-opening-delay/unit")); Directory.CreateDirectory(directory);
        using var direct = new Bitmap(view.Width, view.Height); view.DrawToBitmap(direct, view.ClientRectangle); direct.Save(Path.Combine(directory, style + "-direct.png"));
        Assert.True(MarkerPixels(direct) > 100, "The real presenter must draw its content before diagnosing capture.");
        foreach (double progress in new[] { .25, .5, 1 })
        {
            using var frame = new Bitmap(motion.Bounds.Width, motion.Bounds.Height); using var graphics = Graphics.FromImage(frame); using var alpha = new System.Drawing.Imaging.ImageAttributes();
            motion.Draw(graphics, alpha, progress); frame.Save(Path.Combine(directory, $"{style}-{progress}.png"));
            Assert.True(MarkerPixels(frame) > 100, $"The animation frame at {progress} is blank while the live presenter is held.");
        }
        Assert.False(view.IsDisposed); Assert.True(view.Context.IsActive);
        static int MarkerPixels(Bitmap image)
        {
            int count = 0;
            for (int y = 0; y < image.Height; y++) for (int x = 0; x < image.Width; x++)
            { var color = image.GetPixel(x, y); if (color.A > 0 && color.R > 230 && color.B > 230 && color.G < 230) count++; }
            return count;
        }
    });

    [Fact]
    public void Slow_gallery_preparation_preserves_the_full_synchronized_card_fade_and_item_slide() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture();
        f.Gallery.DropDownPresenterFactory = () => new View { PaintMarker = true, CaptureDelay = 60 };
        f.Ribbon.AnimationLevel = RibbonAnimationLevel.Expressive; f.Ribbon.Motion.SystemPolicyOverride = true;
        var surface = f.Open(); var window = (RibbonPopupWindow)surface.Parent!; var motion = window.OpeningMotion!;
        Assert.True(motion.IsRunning); using var first = motion.CaptureFrame()!;
        using var middle = new Bitmap(first.Width, first.Height); using var last = new Bitmap(first.Width, first.Height);
        using (var alpha = new System.Drawing.Imaging.ImageAttributes())
        {
            using (var graphics = Graphics.FromImage(middle)) motion.Draw(graphics, alpha, .5);
            using (var graphics = Graphics.FromImage(last)) motion.Draw(graphics, alpha, 1);
        }
        int framePixels = 0, shadowPixels = 0, markerPixels = 0;
        for (int y = 0; y < first.Height; y++) for (int x = 0; x < first.Width; x++)
        {
            Assert.Equal(0, first.GetPixel(x, y).A);
            var final = last.GetPixel(x, y); var half = middle.GetPixel(x, y);
            if (final.A == 255 && half.A is > 0 and < 255) framePixels++;
            else if (final.A is > 2 and < 250 && half.A < final.A && half.A > 0) shadowPixels++;
            if (final.R > 230 && final.B > 230 && final.G < 230 && final.A == 255) markerPixels++;
        }
        Assert.True(framePixels > 1000, "The card must share the item fade instead of appearing opaque first.");
        if (SystemInformation.IsDropShadowEnabled) Assert.True(shadowPixels > 100, "The shadow must fade with the card.");
        Assert.True(markerPixels > 100);
        window.SettleMotion(); Assert.False(RibbonDrawingHold.HasOpacity(window.Handle)); Assert.False(RibbonDrawingHold.HasOpacity(surface.Handle));
    });
}
