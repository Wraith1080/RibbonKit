using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class ReviewFollowupTests
{
    [Theory] [InlineData(false)] [InlineData(true)]
    public void Backstage_letter_badges_fit_their_actual_navigation_font(bool touch) => FoundationTests.Sta(() =>
    {
        using var painter = new Badges(); using var form = new Form { Size = new(1000, 700) }; using var ribbon = new RibbonControl { Renderer = painter, FileMenuMode = RibbonFileMenuMode.Backstage, Density = touch ? RibbonDensity.Touch : RibbonDensity.Compact };
        ribbon.FileMenu.Visible = true; ribbon.Tabs.Add(new RibbonTab()); ribbon.Backstage.Items.Add(new RibbonButtonItem { Text = "Save", KeyTip = "SV" }); form.Controls.Add(ribbon); form.Show(); Application.DoEvents();
        ribbon.IsBackstageOpen = true; ribbon.BackstageSurface!.ToggleKeyTips(); using var image = new Bitmap(form.Width, form.Height); form.DrawToBitmap(image, new(Point.Empty, image.Size));
        Assert.NotEmpty(painter.Seen); Assert.All(painter.Seen, b => { var text = TextRenderer.MeasureText(b.Keys, b.Font); Assert.True(b.Bounds.Width >= text.Width); Assert.True(b.Bounds.Height >= text.Height + 2); });
    });
    private sealed class Badges : RibbonOffice2024Renderer
    {
        internal readonly List<(Rectangle Bounds, Font Font, string Keys)> Seen = [];
        public override void DrawKeyTip(RibbonPaintContext c, string keys) { Seen.Add((c.Bounds, c.Font, keys)); base.DrawKeyTip(c, keys); }
    }

    [Theory] [InlineData(RibbonDensity.Compact)] [InlineData(RibbonDensity.Touch)]
    public void Simplified_combo_fields_grow_inside_the_existing_row_and_full_groups_keep_their_height(RibbonDensity density) => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { Density = density }; var tab = new RibbonTab(); var group = new RibbonGroup(); var combo = new RibbonComboBoxItem(); group.Items.Add(combo); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        {
            int S(int n) => RibbonLayoutEngine.Scale(n, dpi); using var font = new Font("Segoe UI", 9 * dpi / 96f);
            var slot = new Rectangle(0, 0, S(180), S(RibbonDensityMetrics.For(density).SimplifiedBody - 2 * RibbonDensityMetrics.For(density).SimplifiedClearance));
            var normal = RibbonEditorLayout.Field(slot, combo, 0, font, dpi); var simple = RibbonEditorLayout.Field(slot, combo, 0, font, dpi, true);
            Assert.True(simple.Height > normal.Height); Assert.True(slot.Contains(simple)); Assert.InRange(Math.Abs(slot.Height - simple.Bottom - simple.Top), 0, 1);
        }
    });

    [Theory] [InlineData(false)] [InlineData(true)]
    public void Full_group_borrowing_uses_a_transient_medium_button_and_restores_live_sources(bool minimized) => FoundationTests.Sta(() =>
    {
        using var form = new Form { Size = new(1250, 700) }; using var ribbon = new RibbonControl { LayoutMode = RibbonLayoutMode.Simplified, IsMinimized = minimized };
        var tab = new RibbonTab { Id = "home", Text = "Home" }; var group = new RibbonGroup { Id = "fields", Text = "Native editors" };
        var combo = new RibbonComboBoxItem { Id = "font", Text = "Font", Value = "Live", IsEditable = true, SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
        var toggle = new RibbonToggleItem { Id = "bold", Text = "Bold", SimplifiedPlacement = RibbonSimplifiedPlacement.Inline }; group.Items.AddRange(combo, toggle); tab.Groups.Add(group); ribbon.Tabs.Add(tab); ribbon.CommandCatalog.CaptureDefaults();
        form.Controls.Add(ribbon); form.Show(); Application.DoEvents();
        RibbonPopupSurface? body = null;
        if (minimized) { ribbon.Activate(ribbon.Snapshot.Targets.Single(t => t.Element == tab)); body = ribbon.Popups.Top!; }
        IReadOnlyList<RibbonTarget> Targets() => body?.Targets ?? ribbon.Snapshot.Targets;
        var before = Targets().Select(t => t.Identity).ToArray(); nint handle = ribbon.Editors.EditorFor(combo)!.Handle; using var edit = ribbon.CommandCatalog.BeginCustomization("review"); string profile = edit.ExportJson();
        ribbon.Activate(Targets().Single(t => t.Element == ribbon.Simplified.More), body); var more = ribbon.Popups.Top!;
        ribbon.Activate(more.Targets.Single(t => t.Element is RibbonQuickAccessGroupReference r && r.Source == group), more);
        var full = ribbon.Popups.Top!; Assert.Same(group, full.GroupSource); var collapsed = Assert.Single(Targets(), t => t.Element == group); Assert.Equal(RibbonItemSize.Medium, collapsed.Size);
        Assert.DoesNotContain(Targets(), t => t.Element == combo || t.Element == toggle); Assert.Equal(handle, ribbon.Editors.EditorFor(combo)!.Handle); Assert.Same(full, ribbon.Editors.EditorFor(combo)!.Parent!.Parent);
        combo.Value = "Updated"; toggle.Checked = true; Assert.True(ribbon.Popups.BorrowsGroup(group)); Assert.Equal(profile, edit.ExportJson());
        ribbon.Popups.CloseTop(false); Assert.Equal(before, Targets().Select(t => t.Identity)); Assert.Equal(handle, ribbon.Editors.EditorFor(combo)!.Handle); Assert.True(toggle.Checked); Assert.Equal("Updated", combo.Value); Assert.Equal(profile, edit.ExportJson());
        ribbon.Activate(more.Targets.Single(t => t.Element is RibbonQuickAccessGroupReference r && r.Source == group), more);
        var button = Targets().Single(t => t.Element == group).VisibleBounds; var surface = (Control?)body ?? ribbon;
        Assert.True(ribbon.Popups.OutsidePress(surface.PointToScreen(new(button.Left + button.Width / 2, button.Top + button.Height / 2))));
        Assert.False(ribbon.Popups.BorrowsGroup(group)); Assert.Equal(before, Targets().Select(t => t.Identity)); Assert.Equal(minimized, ribbon.Popups.HasOpen);
    });

    [Fact]
    public void Transfer_artwork_is_swapped_without_swapping_actions_and_apply_keeps_tree_position() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl(); var tab = new RibbonTab { Id = "home", Text = "Home" };
        for (int i = 0; i < 18; i++) { var group = new RibbonGroup { Id = "g" + i, Text = "Group " + i }; group.Items.Add(new RibbonButtonItem { Id = "cmd" + i, Text = "Command " + i }); tab.Groups.Add(group); } ribbon.Tabs.Add(tab); ribbon.CommandCatalog.CaptureDefaults();
        using var dialog = new RibbonQuickAccessCustomizationForm(ribbon, "review"); dialog.SelectedPage = dialog.ActiveRibbonPage; dialog.Show(); Application.DoEvents();
        var editor = dialog.RibbonEditor; var tree = (TreeView)editor.Controls.Find("RibbonHierarchy", true).Single(); tree.TopNode = tree.Nodes[0].Nodes[8]; string top = tree.TopNode.Text; nint handle = tree.Handle;
        Assert.True(editor.ApplyChanges()); Assert.Equal(top, tree.TopNode.Text); Assert.Equal(handle, tree.Handle);
        foreach (var page in new UserControl[] { editor, dialog.SimplifiedEditor })
        {
            string importName = page == editor ? "ImportRibbonProfile" : "SimplifiedImport", exportName = page == editor ? "ExportRibbonProfile" : "SimplifiedExport";
            var import = (Button)page.Controls.Find(importName, true).Single(); var export = (Button)page.Controls.Find(exportName, true).Single();
            using var expectedImport = RibbonOptionsTransferIcons.Image(ribbon, import.Image!.Width, false); using var expectedExport = RibbonOptionsTransferIcons.Image(ribbon, export.Image!.Width, true);
            for (int y = 0; y < expectedImport.Height; y++) for (int x = 0; x < expectedImport.Width; x++) { Assert.Equal(expectedImport.GetPixel(x, y), ((Bitmap)import.Image).GetPixel(x, y)); Assert.Equal(expectedExport.GetPixel(x, y), ((Bitmap)export.Image).GetPixel(x, y)); }
        }
    });
}
