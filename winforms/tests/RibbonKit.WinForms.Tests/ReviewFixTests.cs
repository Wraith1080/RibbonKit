using System.Runtime.InteropServices;
using System.Text.Json.Nodes;
using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class ReviewFixTests
{
    private sealed class Fixture : IDisposable
    {
        internal readonly RibbonControl Ribbon = new() { Width = 1300 };
        internal readonly RibbonTab Home = new() { Id = "home", Text = "Home" };
        internal readonly RibbonGroup Group = new() { Id = "source", Text = "Source" };
        internal readonly RibbonButtonItem Copy = new() { Id = "copy", Text = "Copy", SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
        internal readonly Icon Artwork = new();
        internal Fixture() { Copy.Icon = Artwork; Group.Items.Add(Copy); Home.Groups.Add(Group); Ribbon.Tabs.Add(Home); Ribbon.CommandCatalog.CaptureDefaults(); }
        internal RibbonCustomizationSession Edit() => Ribbon.CommandCatalog.BeginCustomization("review.test", RibbonCustomizationTarget.Ribbon);
        internal static RibbonCustomizationGroup Add(RibbonCustomizationSession edit)
        {
            var group = new RibbonCustomizationGroup { Label = "My group", Commands = [new() { CommandId = "copy", Size = RibbonItemSize.Large }] }; edit.Draft.Ribbon!.Tabs.Add(new() { Label = "My tab", Groups = [group] }); return group;
        }
        public void Dispose() => Ribbon.Dispose();
    }
    private sealed class Icon : IRibbonIcon
    { public void Draw(Graphics g, Rectangle b, RibbonIconContext c) { using var pen = new Pen(c.Foreground); g.DrawRectangle(pen, Rectangle.Inflate(b, -2, -2)); } }
    [Fact]
    public void Custom_group_metadata_is_detached_shared_persisted_source_linked_and_normalized() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); using var edit = f.Edit(); var group = Fixture.Add(edit); group.IconCommandId = "copy"; group.Layout = RibbonCustomizationGroupLayout.Large;
        Assert.Single(f.Ribbon.Tabs); Assert.True(edit.Apply()); var live = f.Ribbon.Tabs.Last().Groups.Single(); Assert.Same(f.Artwork, live.Icon); Assert.Equal(RibbonItemSize.Large, live.Items[0].SizePolicy.Large); Assert.Same(f.Group, f.Copy.Parent);
        using var source = new Fixture(); using var imported = source.Edit(); imported.ImportJson(edit.ExportJson()); Assert.True(imported.Apply()); Assert.Same(source.Artwork, source.Ribbon.Tabs.Last().Groups[0].Icon);
        edit.Draft.Ribbon!.Tabs.Last().Groups[0].Layout = RibbonCustomizationGroupLayout.Stacked; Assert.True(edit.Apply()); Assert.Equal(RibbonItemSize.Medium, live.Items[0].SizePolicy.Large);
        Assert.Equal(RibbonCustomizationGroupLayout.Stacked, edit.Draft.Ribbon!.Tabs.Last().Groups[0].Layout);
        var replacement = new Icon(); f.Copy.Icon = replacement; Assert.Same(replacement, live.Icon);
        edit.Draft.Ribbon.Tabs.Last().Groups[0].IconCommandId = null; Assert.True(edit.Apply()); Assert.Null(live.Icon);
        edit.Draft.Ribbon.Tabs.Last().Groups[0].IconCommandId = "unknown"; Assert.Throws<InvalidOperationException>(() => edit.Apply()); Assert.Null(live.Icon);
    });
    [Theory] [InlineData(2)] [InlineData(3)]
    public void Older_profiles_without_group_layout_default_to_stacked_and_demote_large_commands(int version) => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); using var edit = f.Edit(); Fixture.Add(edit); var node = JsonNode.Parse(edit.ExportJson())!; node["version"] = version; node.AsObject().Remove("simplified");
        var group = node["ribbon"]!["tabs"]!.AsArray().Last()!["groups"]![0]!; group.AsObject().Remove("layout"); group.AsObject().Remove("iconCommandId"); group["commands"]![0]!["size"] = "Large";
        edit.ImportJson(node.ToJsonString()); Assert.Equal(RibbonCustomizationGroupLayout.Stacked, edit.Draft.Ribbon!.Tabs.Last().Groups[0].Layout); Assert.Equal(RibbonItemSize.Medium, edit.Draft.Ribbon.Tabs.Last().Groups[0].Commands[0].Size);
        Assert.True(edit.Apply()); Assert.Equal(RibbonItemSize.Medium, f.Ribbon.Tabs.Last().Groups[0].Items[0].SizePolicy.Large);
    });
    [Fact]
    public void Group_metadata_rolls_back_with_a_failed_structural_apply() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); using var edit = f.Edit(); var group = Fixture.Add(edit); group.IconCommandId = "copy"; Assert.True(edit.Apply()); var live = f.Ribbon.Tabs.Last().Groups[0]; f.Ribbon.SelectedTab = f.Ribbon.Tabs.Last();
        edit.Draft.Ribbon!.Tabs.Last().Groups[0].IconCommandId = null; edit.Draft.Ribbon.Tabs.Last().Groups[0].Layout = RibbonCustomizationGroupLayout.Large; edit.Draft.Ribbon.Tabs.Last().Visible = false; bool fail = true;
        f.Ribbon.SelectedTabChanged += (_, _) => { if (fail) { fail = false; throw new InvalidOperationException("Host rejected selection"); } };
        Assert.Throws<InvalidOperationException>(() => edit.Apply()); Assert.Same(f.Artwork, live.Icon); Assert.Equal(RibbonItemSize.Medium, live.Items[0].SizePolicy.Large);
    });
    [Theory] [InlineData(false)] [InlineData(true)]
    public void Group_dialog_drafts_name_icon_and_layout_and_cancel_keeps_original(bool touch) => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); f.Ribbon.Density = touch ? RibbonDensity.Touch : RibbonDensity.Compact; using var host = new Form(); host.Show(); var group = new RibbonCustomizationGroup { Label = "New Group" };
        using (var dialog = new RibbonCustomizationGroupDialog(f.Ribbon, host, group, group.Label, true))
        {
            dialog.Show(host); ((TextBox)dialog.Controls.Find("RibbonGroupName", true).Single()).Text = "Clipboard tools";
            var icons = (ListView)dialog.Controls.Find("RibbonGroupIcons", true).Single(); icons.Items["copy"]!.Selected = true;
            ((ComboBox)dialog.Controls.Find("RibbonGroupLayout", true).Single()).SelectedItem = RibbonCustomizationGroupLayout.Large;
            ((Button)dialog.Controls.Find("AcceptGroup", true).Single()).PerformClick(); Assert.Equal("Clipboard tools", group.Label); Assert.Equal("copy", group.IconCommandId); Assert.Equal(RibbonCustomizationGroupLayout.Large, group.Layout);
        }
        using var canceled = new RibbonCustomizationGroupDialog(f.Ribbon, host, group, group.Label!, false); canceled.Show(host); ((TextBox)canceled.Controls.Find("RibbonGroupName", true).Single()).Text = "Discard"; ((Button)canceled.Controls.Find("CancelGroup", true).Single()).PerformClick(); Assert.Equal("Clipboard tools", group.Label);
    });
    [Fact]
    public void Successful_apply_is_quiet_and_context_actions_have_no_artwork() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); using var classic = new RibbonCustomizationEditor(f.Ribbon, "review.test"); Assert.True(classic.ApplyChanges()); Assert.Empty(((Label)classic.Controls.Find("RibbonCustomizationStatus", true).Single()).Text);
        f.Ribbon.LayoutMode = RibbonLayoutMode.Simplified; using var simple = new RibbonSimplifiedCustomizationEditor(f.Ribbon, "review.test"); Assert.True(simple.ApplyChanges()); Assert.Empty(((Label)simple.Controls.Find("SimplifiedCustomizationStatus", true).Single()).Text);
        using var menu = f.Ribbon.CreateCustomizationMenu(f.Copy); Assert.All(menu.Items.Where(i => i.Id is "AddToQuickAccess" or "RemoveFromQuickAccess" or "ToggleSimplifiedPin"), i => { Assert.Null(i.Icon); Assert.Null(i.Image); });
    });
    [Fact]
    public void Tree_native_double_buffer_is_enabled_and_expander_hover_has_identical_pixels() => FoundationTests.Sta(() =>
    {
        Application.EnableVisualStyles();
        using var f = new Fixture(); using var host = new Form { Width = 900, Height = 650 }; using var editor = new RibbonCustomizationEditor(f.Ribbon, "review.test") { Dock = DockStyle.Fill }; host.Controls.Add(editor); host.Show(); Application.DoEvents();
        var tree = (RibbonCustomizationTree)editor.Controls.Find("RibbonHierarchy", true).Single(); Assert.NotEqual(0, (int)SendMessage(tree.Handle, 0x112D, 0, 0) & 4);
        using var before = new Bitmap(tree.Width, tree.Height); tree.DrawToBitmap(before, tree.ClientRectangle); var expand = tree.Geometry(tree.Nodes[0]).Expand; SendMessage(tree.Handle, 0x200, 0, (nint)(((expand.Top + expand.Height / 2) << 16) | (expand.Left + expand.Width / 2))); tree.Update();
        using var after = new Bitmap(tree.Width, tree.Height); tree.DrawToBitmap(after, tree.ClientRectangle);
        for (int y = expand.Top; y < expand.Bottom; y++) for (int x = expand.Left; x < expand.Right; x++) Assert.Equal(before.GetPixel(x, y), after.GetPixel(x, y));
    });
    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Header_minimize_visual_is_square_and_keeps_its_width_at_each_dpi(int dpi)
    {
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi); var theme = RibbonThemes.Office2024Light;
        Assert.Equal(40, theme.TabHeight); var slot = new Rectangle(0, 0, S(28), S(theme.TabHeight)); var original = RibbonHeaderButtonLayout.PaintBounds(slot, theme, dpi); var square = RibbonHeaderButtonLayout.PaintBounds(slot, theme, dpi, true);
        Assert.Equal(original.Width, square.Width); Assert.Equal(square.Width, square.Height); Assert.InRange(Math.Abs((square.Top * 2 + square.Height) - slot.Height), 0, 1);
    }
    [DllImport("user32.dll")] private static extern nint SendMessage(nint window, uint message, nint wParam, nint lParam);
}
