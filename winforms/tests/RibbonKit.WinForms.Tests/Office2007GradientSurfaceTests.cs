using System.Runtime.InteropServices;
using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class Office2007GradientSurfaceTests
{
    [Fact]
    public void Opaque_fills_are_additive_color_setters_reset_them_and_validation_is_atomic() => FoundationTests.Sta(() =>
    {
        var t = RibbonThemes.Office2007Blue; var a = t.OptionsAppearance!;
        Assert.Equal(t.BodyBackground, t.PopupBackgroundFill); Assert.Equal(t.PopupBackgroundFill, a.BackgroundFill); Assert.Equal(a.BackgroundFill, a.ListBackgroundFill);
        Assert.Equal(.17, a.BackgroundFill!.Value.IntermediateStops!.Single().Offset);
        Assert.Null((a with { BackgroundColor = Color.Red }).BackgroundFill); Assert.Null((a with { ListBackgroundColor = Color.Lime }).ListBackgroundFill);
        Assert.NotNull((a with { ListBackgroundColor = Color.Lime }).BackgroundFill); Assert.NotNull((a with { BackgroundColor = Color.Red }).ListBackgroundFill);
        using var ribbon = new RibbonControl { BuiltInTheme = RibbonBuiltInTheme.Office2007Blue };
        var bad = new RibbonFill(Color.White) { IntermediateStops = new([new(.4, Color.FromArgb(128, Color.Blue))]) };
        foreach (bool list in new[] { false, true })
        {
            Assert.Throws<ArgumentException>(() => ribbon.Theme = t with { OptionsAppearance = list ? a with { ListBackgroundFill = bad } : a with { BackgroundFill = bad } });
            Assert.Same(t, ribbon.Theme); Assert.Equal(RibbonBuiltInTheme.Office2007Blue, ribbon.BuiltInTheme);
        }
    });

    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Rows_tree_and_icon_cells_use_the_complete_well_under_clips_and_local_buffers(int dpi) => FoundationTests.Sta(() =>
    {
        using var painter = new RoundedPainter(); using var owner = new RibbonOffice2024Renderer(); using var check = new RibbonCheckBoxItem();
        using var image = new Bitmap(240, 200); using var g = Graphics.FromImage(image); var t = RibbonThemes.Office2007Blue;
        foreach (bool local in new[] { false, true }) foreach (int kind in new[] { 0, 1, 2 })
        {
            var row = new Rectangle(0, local ? 0 : 60, 240, 32); var canvas = new Rectangle(0, local ? -60 : 0, 240, 200);
            var c = new RibbonPaintContext(g, row, t, Control.DefaultFont, dpi) { BackgroundBounds = canvas };
            g.Clear(Color.Magenta); g.SetClip(new Rectangle(30, row.Top + 3, 160, 22));
            if (kind == 0) RibbonOptionsRowRenderer.Draw(painter, c, null, "", false);
            else if (kind == 1) RibbonOptionsTreeRenderer.Draw(owner, painter, c, default, "", false, false, false, null, true, false, check, null);
            else RibbonOptionsIconPickerRenderer.Draw(painter, c, null, false, 0);
            Assert.Equal(Color.Magenta.ToArgb(), image.GetPixel(29, row.Top + 10).ToArgb());
            Assert.Equal(RibbonFillColors.At(t.OptionsAppearance!.ListBackgroundFill!.Value, 70d / 199).ToArgb(), image.GetPixel(180, row.Top + 10).ToArgb());
            g.ResetClip();
        }
    });

    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Default_button_outer_rims_are_one_device_pixel_and_authored_logical_widths_still_win(int dpi) => FoundationTests.Sta(() =>
    {
        using var painter = new RoundedPainter(); using var button = new Button { Text = "" }; using var image = new Bitmap(200, 70); using var g = Graphics.FromImage(image);
        var t = RibbonThemes.Office2007Blue; var a = t.OptionsAppearance!; var bounds = new Rectangle(4, 4, 192, 62);
        foreach (bool primary in new[] { false, true }) foreach (bool enabled in new[] { false, true })
        {
            button.Enabled = enabled; g.Clear(Color.Magenta);
            RibbonOptionsButtonRenderer.Draw(painter, new(g, bounds, t, Control.DefaultFont, dpi), button, primary, false, false, false, false, 3, Color.White);
            var border = enabled ? primary ? a.AccentButtonStateChrome!.IdleBorderColor : a.ButtonIdleStateChrome!.IdleBorderColor : a.ButtonDisabledBorderColor;
            Assert.Equal(border.ToArgb(), image.GetPixel(100, 4).ToArgb());
            var fill = new RibbonOptionsShellMetrics(t, dpi).ButtonFill(primary, enabled, false, false);
            Assert.Equal(RibbonFillColors.At(fill, 1d / 61).ToArgb(), image.GetPixel(100, 5).ToArgb());
            var wide = t with { OptionsAppearance = a with { ButtonBorderWidth = 3, ButtonBorderColor = Color.Red } };
            RibbonOptionsButtonRenderer.Draw(painter, new(g, bounds, wide, Control.DefaultFont, dpi), button, primary, false, false, false, false, 3, Color.White);
            int stroke = (int)Math.Ceiling(3 * dpi / 96d);
            Assert.Equal(Color.Red.ToArgb(), image.GetPixel(100, 4 + stroke - 1).ToArgb()); Assert.NotEqual(Color.Red.ToArgb(), image.GetPixel(100, 4 + stroke).ToArgb());
        }
    });

    [Theory] [InlineData(false, false)] [InlineData(false, true)] [InlineData(true, false)] [InlineData(true, true)]
    public void Native_options_body_rows_unused_space_scrolling_and_resize_keep_gradient_coordinates(bool touch, bool simplified) => FoundationTests.Sta(() =>
    {
        using var host = new Form { Bounds = new(-16000, -16000, 1100, 760), ShowInTaskbar = false };
        using var ribbon = new RibbonControl { BuiltInTheme = RibbonBuiltInTheme.Office2007Blue, Density = touch ? RibbonDensity.Touch : RibbonDensity.Compact, LayoutMode = simplified ? RibbonLayoutMode.Simplified : RibbonLayoutMode.Classic };
        host.Controls.Add(ribbon); var tab = new RibbonTab { Text = "Home" }; var group = new RibbonGroup { Text = "Commands" }; tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        for (int i = 0; i < 35; i++) group.Items.Add(new RibbonButtonItem { Text = "Command " + i, Id = "c" + i });
        host.Show(); using var options = new RibbonQuickAccessCustomizationForm(ribbon, "gradient-test") { StartPosition = FormStartPosition.Manual, Location = new(-16000, -16000), ShowInTaskbar = false };
        options.Show(host); options.Location = new(-16000, -16000); options.SelectedPage = options.Pages[0]; Application.DoEvents();
        var list = options.Controls.Find(simplified ? "SimplifiedAvailableCommands" : "RibbonAvailableCommands", true).OfType<ListBox>().Single(); list.SelectedIndex = -1;
        var handle = list.Handle; var parent = list.Parent; var fill = ribbon.Theme.OptionsAppearance!.ListBackgroundFill!.Value;
        void Verify(Control control)
        {
            using var image = new Bitmap(control.Width, control.Height); control.DrawToBitmap(image, new(Point.Empty, image.Size));
            int x = control.ClientSize.Width - 5;
            foreach (int y in new[] { 3, control.ClientSize.Height / 2, control.ClientSize.Height - 4 })
                Assert.Equal(RibbonFillColors.At(fill, y / (double)(control.ClientSize.Height - 1)).ToArgb(), image.GetPixel(x, y).ToArgb());
        }
        Verify(list); list.TopIndex = Math.Min(3, list.Items.Count - 1); Application.DoEvents(); Verify(list);
        int fullInvalidations = 0;
        list.Invalidated += (_, e) => { if (e.InvalidRect.Contains(list.ClientRectangle)) fullInvalidations++; };
        list.SelectedIndex = 0; fullInvalidations = 0;
        SendMessage(list.Handle, 0x100, 0x23, 0); // Native End key scrolls to the last command.
        Assert.Equal(list.Items.Count - 1, list.SelectedIndex); Assert.True(fullInvalidations > 0);
        list.SelectedIndex = -1; Application.DoEvents(); Verify(list);
        list.Size = new(list.Width, list.Height + 24); list.Update(); Verify(list);
        Assert.Equal(handle, list.Handle); Assert.Same(parent, list.Parent);
        var search = options.Controls.Find(simplified ? "SimplifiedAvailableSearch" : "RibbonCommandSearch", true).OfType<TextBox>().Single();
        search.Text = "Command 34"; Application.DoEvents(); list.SelectedIndex = -1; Verify(list); // Includes the unused native well below one row.
        if (!simplified) { var tree = options.Controls.Find("RibbonHierarchy", true).OfType<TreeView>().Single(); tree.SelectedNode = null; tree.Nodes.Clear(); Verify(tree); }
        var editor = simplified ? (Control)options.SimplifiedEditor : options.RibbonEditor;
        var body = editor.Parent!.Parent!.Parent!; using var bodyImage = new Bitmap(body.Width, body.Height); body.DrawToBitmap(bodyImage, new(Point.Empty, bodyImage.Size));
        int yBody = body.Height / 2;
        Assert.Equal(RibbonFillColors.At(ribbon.Theme.OptionsAppearance.BackgroundFill!.Value, yBody / (double)(body.Height - 1)).ToArgb(), bodyImage.GetPixel(body.Width - 4, yBody).ToArgb());
        string output = Path.Combine(Environment.CurrentDirectory, "artifacts/office2007-g6/gradient-native"); Directory.CreateDirectory(output);
        using var shot = new Bitmap(options.Width, options.Height); options.DrawToBitmap(shot, new(Point.Empty, shot.Size)); shot.Save(Path.Combine(output, $"options-{touch}-{simplified}.png"));
        options.Close(); host.Close();
    });

    [Theory] [InlineData(0)] [InlineData(1)] [InlineData(2)]
    public void Native_menu_gallery_and_collapsed_group_surfaces_paint_one_complete_gradient(int kind) => FoundationTests.Sta(() =>
    {
        using var host = new Form { Bounds = new(-16000, -16000, 1100, 760), ShowInTaskbar = false };
        using var ribbon = new RibbonControl { BuiltInTheme = RibbonBuiltInTheme.Office2007Blue, AnimationLevel = RibbonAnimationLevel.None }; host.Controls.Add(ribbon);
        var tab = new RibbonTab { Text = "Home" }; var group = new RibbonGroup { Text = "Group" }; tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        var menu = new RibbonDropDownItem { Text = "Menu" }; menu.Items.Add(new RibbonButtonItem { Text = "Command" }); group.Items.Add(menu);
        var gallery = new RibbonGalleryItem { Text = "Gallery" }; for (int i = 0; i < 8; i++) gallery.Items.Add(new RibbonGalleryTile { Text = "Tile " + i }); group.Items.Add(gallery);
        host.Show(); RibbonElement source = kind == 0 ? menu : kind == 1 ? gallery : group;
        using var popup = new RibbonPopupSurface(ribbon, source, new(900, 650), menuSource: kind == 0 ? menu : null) { Location = new(10, ribbon.Bottom + 10) }; host.Controls.Add(popup); Application.DoEvents();
        using var image = new Bitmap(popup.Width, popup.Height); popup.DrawToBitmap(image, new(Point.Empty, image.Size));
        foreach (int y in new[] { 1, popup.Height / 2, popup.Height - 2 })
            Assert.Equal(RibbonFillColors.At(ribbon.Theme.PopupFill, y / (double)(popup.ClientSize.Height - 1)).ToArgb(), image.GetPixel(popup.ClientSize.Width - 2, y).ToArgb());
        string output = Path.Combine(Environment.CurrentDirectory, "artifacts/office2007-g6/gradient-native"); Directory.CreateDirectory(output); image.Save(Path.Combine(output, "popup-" + kind + ".png"));
        host.Close();
    });
    [DllImport("user32.dll")] private static extern nint SendMessage(nint window, int message, nint wParam, nint lParam);
}
