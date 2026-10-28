using System.ComponentModel;
using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class DensityTests
{
    [Fact]
    public void Density_metadata_scope_and_repeated_measurement_preserve_compact_geometry_and_three_touch_rows() => FoundationTests.Sta(() =>
    {
        using var host = new RibbonControl { Width = 1200 }; using var other = new RibbonControl(); var (tab, group) = Attach(host);
        var items = Enumerable.Range(0, 6).Select(i => new RibbonButtonItem { Text = "Action " + i, SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium) }).ToArray(); group.Items.AddRange(items);
        Assert.Equal(RibbonDensity.Compact, host.Density); Assert.True(((BindableAttribute)TypeDescriptor.GetProperties(host)[nameof(host.Density)]!.Attributes[typeof(BindableAttribute)]!).Bindable);
        Assert.Throws<ArgumentOutOfRangeException>(() => host.Density = (RibbonDensity)99);
        var compact = host.Snapshot.Targets.Where(t => t.Kind == TargetKind.Item).Select(t => t.Bounds).ToArray(); int height = host.Height, changes = 0; var font = host.Font; var theme = host.Theme;
        host.DensityChanged += (_, _) => changes++;
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        {
            for (int cycle = 0; cycle < 2; cycle++)
            {
                host.Density = RibbonDensity.Touch; var measured = host.Engine.Measure(group, 0, host.Font, dpi, host.LayoutTheme);
                Assert.Equal(3, measured.Items.Take(3).Select(i => i.Bounds.Top).Distinct().Count()); Assert.Single(measured.Items.Take(3).Select(i => i.Bounds.Left).Distinct());
                Assert.True(measured.Items[3].Bounds.Left > measured.Items[0].Bounds.Left);
                Assert.All(measured.Items, i => Assert.True(i.Bounds.Height >= RibbonLayoutEngine.Scale(44, dpi)));
                Assert.True(host.Height > height); Assert.Same(font, host.Font); Assert.Same(theme, host.Theme); Assert.Equal(RibbonDensity.Compact, other.Density);
                host.Density = RibbonDensity.Compact;
                Assert.Equal(compact, host.Snapshot.Targets.Where(t => t.Kind == TargetKind.Item).Select(t => t.Bounds)); Assert.Equal(height, host.Height);
            }
        }
        Assert.Equal(16, changes); Assert.Same(tab, host.SelectedTab); Assert.Equal(items, group.Items);
    });

    [Fact]
    public void Native_editors_keep_handles_values_selection_and_focus_and_reject_invalid_density_changes() => FoundationTests.Sta(() =>
    {
        using var form = new Form { Width = 1500, Height = 650 }; var host = new RibbonControl(); form.Controls.Add(host); var (_, group) = Attach(host); group.CanResize = false;
        var text = new RibbonTextBoxItem { Text = "Name", Value = "Accepted text" }; var combo = new RibbonComboBoxItem { Text = "Font", IsEditable = true }; combo.Items.AddRange("Arial", "Calibri"); combo.SelectedIndex = 1; group.Items.AddRange(text, combo);
        text.ValueValidating += (_, e) => e.Cancel = text.Value == "";
        form.Show(); Application.DoEvents(); text.PerformClick(); var editor = host.Editors.EditorFor(text)!; var nativeCombo = host.Editors.EditorFor(combo)!; nint textHandle = editor.Handle, comboHandle = nativeCombo.Handle;
        editor.Text = ""; host.Density = RibbonDensity.Touch; Assert.Equal(RibbonDensity.Compact, host.Density); Assert.True(editor.Focused);
        editor.Text = "Accepted text"; editor.Select(2, 4); int oldField = editor.Parent!.Height;
        for (int i = 0; i < 2; i++)
        {
            host.Density = RibbonDensity.Touch; Assert.Equal(textHandle, editor.Handle); Assert.Equal(comboHandle, nativeCombo.Handle); Assert.True(editor.Focused); Assert.Equal(2, editor.SelectionStart); Assert.Equal(4, editor.SelectionLength); Assert.True(editor.Parent!.Height > oldField);
            combo.PerformClick(); nativeCombo.Select(1, 3); host.Density = RibbonDensity.Compact; Assert.Equal(comboHandle, nativeCombo.Handle); Assert.Equal(1, nativeCombo.SelectionStart); Assert.Equal(3, nativeCombo.SelectionLength);
            text.PerformClick(); editor.Select(2, 4);
        }
        Assert.Equal("Accepted text", text.Value); Assert.Equal("Calibri", combo.Value);
    });

    [Fact]
    public void Touch_popups_qat_split_fields_gallery_and_backstage_consume_the_same_density_without_replacing_models() => FoundationTests.Sta(() =>
    {
        using var form = new Form { Width = 1500, Height = 700 }; var host = new RibbonControl { QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon }; form.Controls.Add(host); var (_, group) = Attach(host);
        var menu = new RibbonDropDownItem { Text = "Menu" }; var choice = new RibbonButtonItem { Text = "Choice" }; menu.Items.Add(choice);
        var split = new RibbonSplitButtonItem { Text = "Split", Layout = RibbonSplitButtonLayout.Horizontal }; split.Items.Add(new RibbonButtonItem { Text = "More" });
        var gallery = new RibbonGalleryItem { Text = "Styles", IsInRibbon = true, TileWidth = 32, TileHeight = 32 }; gallery.Items.AddRange(Enumerable.Range(0, 12).Select(i => new RibbonGalleryTile { Text = "Tile " + i }).ToArray()); gallery.SelectedItem = (RibbonGalleryTile)gallery.Items[3];
        group.Items.AddRange(menu, split, gallery); var qat = new RibbonQuickAccessDropDownReference { Source = split }; host.QuickAccessItems.Add(qat);
        form.Show(); Application.DoEvents(); var strip = host.GalleryStrips.SurfaceFor(gallery)!; var compactRow = RibbonMenuLayout.RowHeight(choice, 200, host.Font, 96); var compactBack = RibbonBackstageLayout.Build(new(1000, 700), host.Font, 96, host.Backstage);
        menu.ShowDropDown(); Assert.True(menu.IsDropDownOpen); host.Density = RibbonDensity.Touch; Assert.False(menu.IsDropDownOpen); Assert.Same(strip, host.GalleryStrips.SurfaceFor(gallery));
        Assert.True(RibbonMenuLayout.RowHeight(choice, 200, host.Font, 96) > compactRow);
        var back = RibbonBackstageLayout.Build(new(1000, 700), host.Font, 96, host.Backstage); Assert.True(back.RowHeight > compactBack.RowHeight); Assert.True(back.Back.Width > compactBack.Back.Width);
        var qa = host.Snapshot.Targets.Where(t => t.Element == qat).ToArray(); Assert.Equal(2, qa.Length); Assert.Equal(qa[0].Bounds.Right, qa[1].Bounds.Left); Assert.False(qa[0].Bounds.IntersectsWith(qa[1].Bounds));
        Assert.True(qa[0].Bounds.Height >= RibbonLayoutEngine.Scale(44, host.DeviceDpi));
        var splitBounds = RibbonSplitLayout.Calculate(new(0, 0, 120, 130), split, RibbonItemSize.Large, false, 96); Assert.True(splitBounds.Primary.Width > splitBounds.DropDown.Width); Assert.Equal(44, splitBounds.DropDown.Width);
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        {
            var layout = RibbonGalleryLayout.Build(gallery, host.Font, dpi, new(1200, 900)); Assert.All(layout.Targets, t => { Assert.True(t.Bounds.Width >= RibbonLayoutEngine.Scale(64, dpi)); Assert.True(t.Bounds.Height >= RibbonLayoutEngine.Scale(80, dpi)); });
            var field = new Rectangle(0, 0, RibbonLayoutEngine.Scale(100, dpi), RibbonLayoutEngine.Scale(42, dpi)); var button = RibbonEditorLayout.ComboButton(field, dpi, host.Density); var label = RibbonEditorLayout.ComboText(field, dpi, host.Density); Assert.False(button.IntersectsWith(label));
        }
        gallery.Preview((RibbonGalleryTile)gallery.Items[4]); host.Density = RibbonDensity.Compact; Assert.Null(gallery.PreviewItem); Assert.Same(gallery.Items[3], gallery.SelectedItem);
        using var source = new RibbonMergeSource(); var awayTab = new RibbonTab(); var awayGroup = new RibbonGroup(); var awayCommand = new RibbonButtonItem { Text = "Inactive context" }; awayGroup.Items.Add(awayCommand); awayTab.Groups.Add(awayGroup); source.Tabs.Add(awayTab);
        var parked = new RibbonQuickAccessReference { Source = awayCommand }; host.QuickAccessItems.Add(parked); host.QuickAccessPosition = RibbonQuickAccessPosition.TabRow; host.QuickAccessMaxWidth = 28; host.Density = RibbonDensity.Touch;
        host.Activate(host.Snapshot.Targets.Single(t => t.Kind == TargetKind.QuickAccessOverflow)); var row = host.Popups.Top!.Targets.Single(t => t.Element == parked); Assert.False(row.Enabled); Assert.True(row.Bounds.Height >= RibbonLayoutEngine.Scale(44, host.DeviceDpi));
    });

    [Fact]
    public void Density_switches_keep_modal_merge_and_backstage_state_and_narrow_commands_reachable() => FoundationTests.Sta(() =>
    {
        using var form = new Form { Width = 1100, Height = 600 }; var host = new RibbonControl { FileMenuMode = RibbonFileMenuMode.Backstage }; form.Controls.Add(host); var (home, group) = Attach(host);
        group.Items.Add(new RibbonButtonItem { Text = "Host action" }); using var source = new RibbonMergeSource(); var modal = new RibbonTab { IsModal = true, Text = "Preview" }; source.Tabs.Add(modal); host.Merge(source); host.EnterModal(modal);
        host.Density = RibbonDensity.Touch; Assert.True(host.IsModal); Assert.True(source.IsMerged); Assert.Same(modal, host.SelectedTab); host.ExitModal(); host.Unmerge(source); Assert.Same(home, host.SelectedTab);
        using var page = new Panel(); var input = new TextBox { Text = "Page state" }; page.Controls.Add(input); host.Backstage.Items.Add(new RibbonBackstagePageItem { Text = "Info", Content = page }); host.FileMenu.Visible = true;
        form.Show(); Application.DoEvents(); host.IsBackstageOpen = true; input.Focus(); nint handle = input.Handle; host.Density = RibbonDensity.Compact; Assert.True(host.IsBackstageOpen); Assert.Equal(handle, input.Handle); Assert.True(input.Focused); Assert.Equal("Page state", input.Text); host.IsBackstageOpen = false;
        for (int i = 0; i < 8; i++) { var extra = new RibbonGroup { Text = "Group " + i }; extra.Items.Add(new RibbonButtonItem { Text = "Action " + i }); home.Groups.Add(extra); }
        host.Density = RibbonDensity.Touch; host.Width = 340;
        foreach (var target in host.Snapshot.Targets.Where(t => t.Kind == TargetKind.Group && !t.VisibleBounds.IsEmpty).ToArray())
        {
            host.Activate(target); Assert.NotNull(host.Popups.Top); Assert.Contains(host.Popups.Top!.Targets, t => t.Element is RibbonItem); host.Popups.CloseAll(true);
        }
        host.IsMinimized = true; host.Density = RibbonDensity.Compact; Assert.True(host.IsMinimized); Assert.Same(home, host.SelectedTab);
    });
    private static (RibbonTab, RibbonGroup) Attach(RibbonControl host)
    {
        var tab = new RibbonTab { Text = "Home" }; var group = new RibbonGroup { Text = "Commands" }; tab.Groups.Add(group); host.Tabs.Add(tab); return (tab, group);
    }
}
