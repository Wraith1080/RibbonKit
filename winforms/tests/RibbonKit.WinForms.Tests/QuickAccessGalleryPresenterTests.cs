using System.CodeDom;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using RibbonKit.WinForms.Layout;
using Xunit;
using View = RibbonKit.WinForms.Tests.InlineGalleryPresenterTests.View;

namespace RibbonKit.WinForms.Tests;

public sealed class QuickAccessGalleryPresenterTests
{
    private sealed class Fixture : IDisposable
    {
        internal readonly Form Form = new() { Width = 1200, Height = 650 };
        internal readonly RibbonControl Ribbon = new() { AnimationLevel = RibbonAnimationLevel.None };
        internal readonly RibbonGalleryItem Gallery = new() { Text = "Styles", IsInRibbon = true };
        internal readonly RibbonQuickAccessDropDownReference First, Second;
        internal readonly List<View> Views = [], Inline = [];
        internal readonly RibbonTab Home = new() { Text = "Home" }, Other = new() { Text = "Other" };
        internal Fixture()
        {
            var group = new RibbonGroup { Text = "Styles" }; group.Items.Add(Gallery); Home.Groups.Add(group); Ribbon.Tabs.AddRange(Home, Other);
            Gallery.Items.AddRange(Enumerable.Range(0, 15).Select(i => new RibbonGalleryTile { Text = "Style " + i }).ToArray());
            Gallery.SelectedItem = (RibbonGalleryTile)Gallery.Items[0];
            Gallery.DropDownPresenterFactory = () => { var view = new View(); Views.Add(view); return view; };
            Gallery.InRibbonPresenterFactory = () => { var view = new View(); Inline.Add(view); return view; };
            First = new() { Source = Gallery }; Second = new() { Source = Gallery }; Ribbon.QuickAccessItems.AddRange(First, Second);
            Form.Controls.Add(Ribbon); Form.Show(); Form.Activate(); Application.DoEvents();
        }
        internal void Open(RibbonElement entry, RibbonPopupSurface? parent = null) => Ribbon.Activate((parent?.Targets ?? Ribbon.Snapshot.Targets).Single(t => ReferenceEquals(t.Element, entry)), parent);
        internal View Current => Ribbon.Popups.Top!.Controls.Cast<Control>().SelectMany(Children).OfType<View>().Single();
        private static IEnumerable<Control> Children(Control root) => new[] { root }.Concat(root.Controls.Cast<Control>().SelectMany(Children));
        public void Dispose() => Form.Dispose();
    }

    [Fact]
    public void Default_false_and_null_dropdown_selectors_preserve_builtin_qat_and_ordinary_popups_stay_independent() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); Assert.False(f.Gallery.UseDropDownPresenterInQuickAccess);
        f.Open(f.First); Assert.False(f.Ribbon.Popups.Top!.HasGalleryPresenter); Assert.Empty(f.Views); f.Ribbon.Popups.CloseAll(false);
        f.Gallery.ShowDropDown(); var ordinary = Assert.Single(f.Views); Assert.False(ordinary.Context.IsQuickAccess);
        int changes = 0; f.Gallery.UseDropDownPresenterInQuickAccessChanged += (_, _) => changes++;
        f.Gallery.UseDropDownPresenterInQuickAccess = true; f.Gallery.UseDropDownPresenterInQuickAccess = true;
        Assert.False(ordinary.IsDisposed); Assert.True(ordinary.Context.IsActive); Assert.Equal(1, changes);
        f.Gallery.UseDropDownPresenterInQuickAccess = false; Assert.False(ordinary.IsDisposed); Assert.Equal(2, changes);
        f.Gallery.CloseDropDown(); f.Gallery.DropDownPresenterFactory = null; f.Gallery.UseDropDownPresenterInQuickAccess = true;
        f.Open(f.First); Assert.False(f.Ribbon.Popups.Top!.HasGalleryPresenter); Assert.Single(f.Views); Assert.Single(f.Inline);
    });

    [Fact]
    public void Opted_in_type_uses_fresh_popup_controls_and_retains_the_original_inline_tree_and_source() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); f.Gallery.DropDownPresenterFactory = null; f.Gallery.DropDownPresenterType = typeof(View);
        f.Gallery.UseDropDownPresenterInQuickAccess = true; var inline = f.Inline[0]; var inlineHandle = inline.Handle;
        f.Open(f.First); var first = f.Current; var context = first.Context;
        Assert.True(context.IsQuickAccess); Assert.Equal(RibbonGalleryPresentation.DropDown, context.Presentation); Assert.Same(f.Gallery, context.Gallery);
        Assert.NotSame(inline, first); Assert.True(inline.Visible); Assert.False(inline.Context.IsQuickAccess); Assert.False(inline.Context.IsActive);
        f.Open(f.Second); var second = f.Current;
        Assert.True(first.IsDisposed); Assert.Equal(1, first.Disposals); Assert.False(context.IsActive); Assert.NotSame(first, second);
        Assert.Single(f.Ribbon.Popups.Surfaces); Assert.False(f.First.IsDropDownOpen); Assert.True(f.Second.IsDropDownOpen);
        f.Gallery.CloseDropDown(); Assert.True(second.IsDisposed); Assert.Equal(inlineHandle, inline.Handle); Assert.True(inline.Context.IsActive);
        Assert.Equal(15, f.Gallery.Items.Count); Assert.All(f.Gallery.Items, item => Assert.Same(f.Gallery, item.Parent));
    });

    [Fact]
    public void Inactive_tab_and_overflow_routes_share_selection_preview_and_focus_without_selecting_the_source_tab() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); f.Gallery.UseDropDownPresenterInQuickAccess = true; f.Ribbon.SelectedTab = f.Other;
        var last = (RibbonGalleryTile)f.Gallery.Items[^1]; int commits = 0, cancels = 0;
        f.Gallery.SelectionCommitted += (_, _) => commits++; f.Gallery.ItemPreviewCancelled += (_, _) => cancels++;
        f.Open(f.First); var view = f.Current; Assert.True(view.Context.IsQuickAccess);
        Assert.True(view.Context.Preview(last)); Assert.Same(f.Gallery.Items[0], f.Gallery.SelectedItem);
        Assert.True(view.Context.Preview(null)); Assert.Equal(1, cancels);
        Assert.True(view.Context.Commit(last)); Assert.Equal(1, commits); Assert.Same(last, f.Gallery.SelectedItem); Assert.Same(f.Other, f.Ribbon.SelectedTab);
        Assert.Same(f.First, f.Ribbon.FocusedElement);
        f.Ribbon.QuickAccessMaxWidth = 28; f.Open(f.Ribbon.QuickAccessOverflow); var overflow = f.Ribbon.Popups.Top!;
        f.Open(f.Second, overflow); view = f.Current; var stale = view.Context;
        Assert.True(stale.IsQuickAccess); Assert.Equal(2, f.Ribbon.Popups.Surfaces.Count());
        f.Ribbon.Popups.CloseTop(true); Assert.True(view.IsDisposed); Assert.False(overflow.IsDisposed); Assert.Same(f.Second, overflow.FocusedElement);
        Assert.False(stale.Commit(last)); Assert.False(stale.Preview(last));
        f.Open(f.Second, overflow); Assert.True(f.Current.Context.Commit((RibbonGalleryTile)f.Gallery.Items[0]));
        Assert.Equal(2, commits); Assert.False(f.Ribbon.Popups.HasOpen); Assert.Same(f.Other, f.Ribbon.SelectedTab);
    });

    [Fact]
    public void Qat_ancestry_reaches_nested_menus_and_group_flyouts_without_marking_an_ordinary_opening() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); var group = f.Home.Groups[0]; group.Items.Remove(f.Gallery);
        var menu = new RibbonDropDownItem { Text = "Formatting" }; var nested = new RibbonDropDownItem { Text = "Styles" };
        nested.Items.Add(f.Gallery); menu.Items.Add(nested); group.Items.Add(menu); f.First.Source = menu;
        f.Gallery.UseDropDownPresenterInQuickAccess = true;
        f.Open(f.First); f.Open(nested, f.Ribbon.Popups.Top); f.Open(f.Gallery, f.Ribbon.Popups.Top);
        Assert.True(f.Current.Context.IsQuickAccess); Assert.Same(f.Gallery, f.Current.Context.Gallery); Assert.Equal(3, f.Ribbon.Popups.Surfaces.Count());
        f.Ribbon.Popups.CloseAll(false);
        menu.ShowDropDown(); f.Open(nested, f.Ribbon.Popups.Top); f.Open(f.Gallery, f.Ribbon.Popups.Top);
        Assert.False(f.Current.Context.IsQuickAccess); f.Ribbon.Popups.CloseAll(false);
        var groupReference = new RibbonQuickAccessGroupReference { Source = group }; f.Ribbon.QuickAccessItems.Add(groupReference);
        f.Open(groupReference); f.Open(menu, f.Ribbon.Popups.Top); f.Open(nested, f.Ribbon.Popups.Top); f.Open(f.Gallery, f.Ribbon.Popups.Top);
        Assert.True(f.Current.Context.IsQuickAccess); Assert.Equal(4, f.Ribbon.Popups.Surfaces.Count());
    });

    [Fact]
    public void Policy_changes_close_only_the_affected_qat_gallery_and_reopen_resolves_the_current_choice() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); var inlineHandle = f.Inline[0].Handle;
        f.Open(f.First); Assert.False(f.Ribbon.Popups.Top!.HasGalleryPresenter);
        f.Gallery.UseDropDownPresenterInQuickAccess = true; Assert.False(f.First.IsDropDownOpen); Assert.Empty(f.Views);
        f.Open(f.First); var view = f.Current; var context = view.Context;
        f.Gallery.UseDropDownPresenterInQuickAccess = true; Assert.False(view.IsDisposed);
        f.Gallery.UseDropDownPresenterInQuickAccess = false;
        Assert.True(view.IsDisposed); Assert.False(context.IsActive); Assert.False(f.First.IsDropDownOpen); Assert.Equal(inlineHandle, f.Inline[0].Handle);
        f.Open(f.First); Assert.False(f.Ribbon.Popups.Top!.HasGalleryPresenter);
        f.Ribbon.Popups.CloseAll(false); f.Gallery.ShowDropDown(); var ordinary = f.Current;
        f.Gallery.UseDropDownPresenterInQuickAccess = true; Assert.False(ordinary.IsDisposed); Assert.True(ordinary.Context.IsActive);
    });

    [Fact]
    public void Cancelled_opening_reference_rebinding_and_policy_changed_during_factory_cannot_publish_stale_views() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); f.Gallery.UseDropDownPresenterInQuickAccess = true;
        CancelEventHandler cancel = (_, e) => e.Cancel = true; f.Gallery.DropDownOpening += cancel;
        f.Open(f.First); Assert.Empty(f.Views); Assert.False(f.First.IsDropDownOpen); f.Gallery.DropDownOpening -= cancel;
        CancelEventHandler rebind = (_, _) => f.First.Source = null; f.Gallery.DropDownOpening += rebind;
        f.Open(f.First); Assert.Empty(f.Views); f.Gallery.DropDownOpening -= rebind; f.First.Source = f.Gallery;
        var created = new View(); f.Gallery.DropDownPresenterFactory = () => { f.Gallery.UseDropDownPresenterInQuickAccess = false; return created; };
        f.Open(f.First); Assert.True(created.IsDisposed); Assert.False(created.Context.IsActive); Assert.False(f.First.IsDropDownOpen);
        f.Open(f.First); Assert.False(f.Ribbon.Popups.Top!.HasGalleryPresenter); Assert.Equal(15, f.Gallery.Items.Count);
    });

    [Fact]
    public void Validation_can_cancel_or_retire_the_qat_view_and_reentrant_commit_cannot_execute_again() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); f.Gallery.UseDropDownPresenterInQuickAccess = true;
        var field = new RibbonTextBoxItem { Text = "Name", Value = "Original" }; f.Home.Groups[0].Items.Add(field);
        f.Open(f.First); var view = f.Current; var editor = f.Ribbon.Editors.EditorFor(field)!;
        Assert.True(editor.Focus()); editor.Text = "Edited"; int validations = 0, commits = 0;
        f.Gallery.SelectionCommitted += (_, _) => commits++;
        CancelEventHandler reject = (_, e) => { validations++; e.Cancel = true; }; field.ValueValidating += reject;
        Assert.False(view.Context.Commit((RibbonGalleryTile)f.Gallery.Items[1])); Assert.Equal(1, validations); Assert.Equal(0, commits); Assert.True(f.First.IsDropDownOpen);
        field.ValueValidating -= reject; CancelEventHandler retire = (_, _) => { validations++; f.Gallery.UseDropDownPresenterInQuickAccess = false; }; field.ValueValidating += retire;
        Assert.False(view.Context.Commit((RibbonGalleryTile)f.Gallery.Items[1])); Assert.Equal(2, validations); Assert.True(view.IsDisposed); Assert.Equal(0, commits);
        field.ValueValidating -= retire; Assert.True(f.Ribbon.Editors.CommitFocused());
        f.Gallery.UseDropDownPresenterInQuickAccess = true; f.Open(f.First); view = f.Current; bool nested = true;
        f.Gallery.SelectionCommitted += (_, _) => nested = view.Context.Commit((RibbonGalleryTile)f.Gallery.Items[2]);
        Assert.True(view.Context.Commit((RibbonGalleryTile)f.Gallery.Items[1])); Assert.False(nested); Assert.Equal(1, commits);
    });

    [Fact]
    public void Native_descendant_keyboard_scroll_and_capture_use_the_same_popup_body_without_duplicate_commit() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); f.Gallery.UseDropDownPresenterInQuickAccess = true; f.Open(f.First); var view = f.Current;
        using var input = new RibbonInputFilter(f.Ribbon); var wheel = Message.Create(view.Child.Handle, 0x20A, -120 << 16, 0);
        Assert.True(input.PreFilterMessage(ref wheel)); Assert.Equal(1, view.Row);
        var key = Message.Create(view.Child.Handle, 0x100, (nint)Keys.End, 0); Assert.True(input.PreFilterMessage(ref key));
        var last = (RibbonGalleryTile)f.Gallery.Items[^1]; Assert.Same(last, view.Context.FocusedItem); Assert.Same(last, f.Gallery.PreviewItem);
        Assert.False(f.Ribbon.Popups.Top!.Targets.Single(t => ReferenceEquals(t.Element, last)).VisibleBounds.IsEmpty);
        using var image = new Bitmap(f.Ribbon.Popups.Top.Width, f.Ribbon.Popups.Top.Height); RibbonMotionScene.Capture(f.Ribbon, f.Ribbon.Popups.Top, image);
        int commits = 0; f.Gallery.SelectionCommitted += (_, _) => commits++;
        key = Message.Create(view.Child.Handle, 0x100, (nint)Keys.Enter, 0); Assert.True(input.PreFilterMessage(ref key));
        Assert.Equal(1, commits); Assert.True(view.IsDisposed); Assert.Same(last, f.Gallery.SelectedItem); Assert.False(f.Inline[0].IsDisposed);
    });

    [Fact]
    public void Removing_a_reference_or_disposing_source_releases_only_owned_views_and_makes_contexts_inert() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); f.Gallery.UseDropDownPresenterInQuickAccess = true; f.Open(f.First); var first = f.Current;
        f.Ribbon.QuickAccessItems.Remove(f.First); Assert.True(first.IsDisposed); Assert.False(first.Context.IsActive); Assert.False(f.Gallery.IsDisposed);
        f.Open(f.Second); var second = f.Current; var source = f.Gallery; f.Second.Source = null;
        Assert.True(second.IsDisposed); Assert.False(second.Context.IsActive); Assert.False(source.IsDisposed); Assert.Equal(15, source.Items.Count);
        f.Second.Source = source; f.Open(f.Second); var last = f.Current; source.Dispose();
        Assert.True(last.IsDisposed); Assert.True(last.Child.IsDisposed); Assert.False(last.Context.IsActive); Assert.True(f.Inline[0].IsDisposed);
    });

    [Fact]
    public void Default_metadata_and_native_serializer_store_only_the_nondefault_bool_without_constructing_controls() => FoundationTests.Sta(() =>
    {
        using var gallery = new RibbonGalleryItem { DropDownPresenterType = typeof(View) };
        var property = TypeDescriptor.GetProperties(gallery)[nameof(gallery.UseDropDownPresenterInQuickAccess)]!;
        Assert.Equal(false, Assert.IsType<DefaultValueAttribute>(property.Attributes[typeof(DefaultValueAttribute)]).Value);
        Assert.False(property.ShouldSerializeValue(gallery)); gallery.UseDropDownPresenterInQuickAccess = true; Assert.True(property.ShouldSerializeValue(gallery));
        var serialization = new DesignerSerializationManager(); using var session = serialization.CreateSession();
        var manager = (IDesignerSerializationManager)serialization; manager.SetName(gallery, "stylesGallery");
        var serializer = Assert.IsAssignableFrom<CodeDomSerializer>(manager.GetSerializer(typeof(RibbonGalleryItem), typeof(CodeDomSerializer)));
        var statements = serializer.SerializeMember(manager, gallery, property);
        Assert.Contains(statements.Cast<CodeStatement>().OfType<CodeAssignStatement>(), statement => statement.Left is CodePropertyReferenceExpression { PropertyName: "UseDropDownPresenterInQuickAccess" }
            && statement.Right is CodePrimitiveExpression { Value: true });
        Assert.Empty(serialization.Errors.Cast<object>());
    });

    [Theory]
    [InlineData(RibbonSimplifiedPlacement.Inline)]
    [InlineData(RibbonSimplifiedPlacement.Overflow)]
    public void Ordinary_simplified_references_and_more_commands_are_not_qat_even_when_the_source_has_qat_references(RibbonSimplifiedPlacement placement) => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); f.Gallery.IsInRibbon = false; f.Gallery.SimplifiedPlacement = placement; f.Ribbon.LayoutMode = RibbonLayoutMode.Simplified;
        RibbonPopupSurface? parent = null;
        RibbonElement entry;
        if (placement == RibbonSimplifiedPlacement.Overflow)
        {
            f.Open(f.Ribbon.Simplified.More); parent = f.Ribbon.Popups.Top; Assert.False(parent!.IsQuickAccess);
            var groupEntry = parent.Targets.Select(target => target.Element).OfType<RibbonQuickAccessGroupReference>().Single(reference => ReferenceEquals(reference.Source, f.Home.Groups[0]));
            Assert.DoesNotContain(groupEntry, f.Ribbon.QuickAccessItems); f.Open(groupEntry, parent); parent = f.Ribbon.Popups.Top;
            Assert.False(parent!.IsQuickAccess); entry = f.Gallery;
        }
        else
        {
            entry = f.Gallery;
            Assert.Contains(f.Ribbon.Snapshot.Targets, target => ReferenceEquals(target.Element, entry));
        }
        f.Open(entry, parent); var view = f.Current;
        Assert.False(view.Context.IsQuickAccess); Assert.False(f.Gallery.UseDropDownPresenterInQuickAccess);
        Assert.Same(f.Gallery, view.Context.Gallery); f.Gallery.UseDropDownPresenterInQuickAccess = true;
        Assert.False(view.IsDisposed); Assert.True(view.Context.IsActive);
    });
}
