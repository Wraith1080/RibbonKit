using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class OptionsPopupTests
{
    [Fact]
    public void Modal_options_menu_uses_real_ribbon_surface_plain_tick_and_dialog_input_owner() => FoundationTests.Sta(() =>
    {
        using var main = new Form(); using var ribbon = new RibbonControl(); main.Controls.Add(ribbon); main.Show();
        using var dialog = new Form { Owner = main }; var anchor = new Button { Text = "Edit" }; dialog.Controls.Add(anchor);
        using var menu = new RibbonOptionsActionMenu(ribbon); int calls = 0;
        menu.Action("Show label", () => calls++, check: true); menu.Separator(); menu.Action("Unavailable", () => calls++, enabled: false);
        Exception? failure = null; using var timer = new System.Windows.Forms.Timer { Interval = 50 };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            try
            {
                Assert.True(dialog.Modal); menu.Show(anchor, new(0, anchor.Height)); var surface = Assert.IsType<RibbonPopupSurface>(menu.Surface);
                Assert.Same(dialog, surface.Ribbon.FindForm()); Assert.True(surface.Ribbon.Enabled); Assert.False(surface.Ribbon.Visible);
                Assert.Empty(surface.MenuSource!.DropDownHeader);
                var checkedItem = Assert.IsType<RibbonButtonItem>(menu.Items[0]); Assert.NotNull(checkedItem.Icon); Assert.DoesNotContain(menu.Items, i => i is RibbonCheckBoxItem);
                var disabled = surface.Targets.Single(t => t.Element?.Text == "Unavailable"); Assert.False(disabled.Enabled);
                checkedItem.PerformClick(); Assert.Equal(1, calls); Assert.Null(menu.Surface); Assert.True(anchor.Focused);
                Assert.False(main.IsDisposed); Assert.False(dialog.IsDisposed);
            }
            catch (Exception error) { failure = error; }
            finally { dialog.Close(); }
        };
        timer.Start(); dialog.ShowDialog(main); if (failure is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
    });
    [Fact]
    public void Options_choice_uses_ribbon_list_and_stages_selection_until_accept() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl(); using var form = new Form(); using var combo = new RibbonOptionsComboBox(ribbon) { Name = "tabs", DropDownStyle = ComboBoxStyle.DropDownList, DrawMode = DrawMode.OwnerDrawFixed };
        combo.Items.AddRange(["Home", "Insert", "Review"]); combo.SelectedIndex = 0; form.Controls.Add(combo); form.Show(); combo.Focus(); int changed = 0; combo.SelectedIndexChanged += (_, _) => changed++;
        combo.OpenChoice(); Assert.True(combo.ChoiceOpen);
        var down = Message.Create(combo.Handle, 0x100, (nint)Keys.Down, 0); Assert.True(combo.PreFilterMessage(ref down)); Assert.Equal(0, combo.SelectedIndex);
        combo.CloseChoice(false, true); Assert.Equal(0, combo.SelectedIndex); Assert.Equal(0, changed);
        combo.OpenChoice(); Assert.True(combo.PreFilterMessage(ref down)); combo.CloseChoice(true, true); Assert.Equal(1, combo.SelectedIndex); Assert.Equal(1, changed); Assert.True(combo.Focused);
        combo.OpenChoice(); combo.Visible = false; Assert.False(combo.ChoiceOpen); Assert.Equal(1, changed);
        combo.Visible = true; combo.OpenChoice(); combo.SelectedIndex = 2; Assert.False(combo.ChoiceOpen); Assert.Equal(2, changed);
        combo.OpenChoice(); combo.Enabled = false; Assert.False(combo.ChoiceOpen); Assert.Equal(2, combo.SelectedIndex);
    });
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void Classic_command_size_editor_uses_the_shared_options_picker(bool touch) => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { Density = touch ? RibbonDensity.Touch : RibbonDensity.Compact }; var home = new RibbonTab { Id = "home", Text = "Home" }; var group = new RibbonGroup { Id = "group" }; group.Items.Add(new RibbonButtonItem { Id = "copy", Text = "Copy" }); home.Groups.Add(group); ribbon.Tabs.Add(home); ribbon.CommandCatalog.CaptureDefaults();
        using var form = new Form { Width = 1200, Height = 700 }; using var page = new RibbonCustomizationEditor(ribbon, "options.test") { Dock = DockStyle.Fill }; form.Controls.Add(page); form.Show();
        var command = new RibbonCustomizationCommand { CommandId = "copy" }; page.Session.Draft.Ribbon!.Tabs.Add(new() { Label = "Custom", Groups = [new() { Label = "Commands", Commands = [command] }] }); page.RefreshDraft();
        var tree = (TreeView)page.Controls.Find("RibbonHierarchy", true).Single(); tree.SelectedNode = tree.Nodes[tree.Nodes.Count - 1].Nodes[0].Nodes[0];
        Exception? failure = null; using var timer = new System.Windows.Forms.Timer { Interval = 30 };
        timer.Tick += (_, _) =>
        {
            if (Application.OpenForms.Cast<Form>().FirstOrDefault(f => f.Text == "Edit ribbon item") is not { } edit) return; timer.Stop();
            try
            {
                var picker = Assert.IsType<RibbonOptionsComboBox>(edit.Controls.Find("RibbonItemSize", true).Single()); picker.OpenChoice(); Assert.True(picker.ChoiceOpen); picker.CloseChoice(false, true);
                picker.SelectedItem = RibbonItemSize.Small; edit.DialogResult = DialogResult.OK;
            }
            catch (Exception error) { failure = error; edit.DialogResult = DialogResult.Cancel; }
            finally { edit.Close(); }
        };
        timer.Start(); ((Button)page.Controls.Find("EditRibbonItem", true).Single()).PerformClick(); timer.Stop();
        if (failure is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw(); Assert.Equal(RibbonItemSize.Small, command.Size);
    });
    [Theory]
    [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Checked_menu_artwork_has_tick_ink_and_no_box_at_representative_dpi(int dpi) => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl(); using var menu = new RibbonOptionsActionMenu(ribbon); menu.Action("Marked", () => { }, check: true);
        int side = (int)Math.Ceiling(24 * dpi / 96d); using var image = new Bitmap(side, side); using var graphics = Graphics.FromImage(image); graphics.Clear(Color.White);
        var item = Assert.IsType<RibbonButtonItem>(menu.Items[0]); item.Icon!.Draw(graphics, new(0, 0, side, side), new(dpi, Color.Black, true));
        Assert.Contains(Enumerable.Range(0, side), y => Enumerable.Range(0, side).Any(x => image.GetPixel(x, y).R < 128));
        Assert.All(Enumerable.Range(0, side), i => { Assert.Equal(Color.White.ToArgb(), image.GetPixel(i, 0).ToArgb()); Assert.Equal(Color.White.ToArgb(), image.GetPixel(i, side - 1).ToArgb()); Assert.Equal(Color.White.ToArgb(), image.GetPixel(0, i).ToArgb()); Assert.Equal(Color.White.ToArgb(), image.GetPixel(side - 1, i).ToArgb()); });
    });
}
