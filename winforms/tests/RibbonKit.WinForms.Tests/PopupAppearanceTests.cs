using System.Drawing.Imaging;
using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class PopupAppearanceTests
{
    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Border_width_alpha_hide_and_four_corners_use_existing_radius_and_palette(int dpi) => FoundationTests.Sta(() =>
    {
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
        foreach (bool office in new[] { false, true })
        foreach (int odd in new[] { 0, 1 })
        {
            using RibbonRenderer painter = office ? new RibbonOffice2024Renderer() : new RibbonRenderer();
            using var image = new Bitmap(S(150), S(100)); using var g = Graphics.FromImage(image);
            var bounds = new Rectangle(S(10), S(10), S(125) + odd, S(70) + odd);
            var settings = new RibbonPopupAppearanceSettings { BorderWidth = 3 };
            var theme = RibbonThemes.Office2024Light with { PopupAppearance = settings, PopupBorder = Color.DarkBlue, PopupBackground = Color.LightYellow };
            var c = new RibbonPaintContext(g, bounds, theme, Control.DefaultFont, dpi);
            g.Clear(Color.Transparent); painter.DrawPopupFrame(c);
            Assert.Equal(S(3), painter.GetPopupBorderWidth(theme, dpi));
            Assert.Equal(Color.DarkBlue.ToArgb(), image.GetPixel(bounds.Left, bounds.Top + bounds.Height / 2).ToArgb());
            Assert.Equal(Color.LightYellow.ToArgb(), image.GetPixel(bounds.Left + S(4), bounds.Top + bounds.Height / 2).ToArgb());
            for (int y = 0; y < bounds.Height; y++) for (int x = 0; x < bounds.Width; x++)
            { var color = image.GetPixel(bounds.Left + x, bounds.Top + y); Assert.Equal(color, image.GetPixel(bounds.Right - 1 - x, bounds.Top + y)); Assert.Equal(color, image.GetPixel(bounds.Left + x, bounds.Bottom - 1 - y)); }
            g.Clear(Color.Transparent); painter.DrawPopupFrame(c with { Theme = theme with { PopupAppearance = settings with { BorderWidth = 0 } } });
            Assert.Equal(Color.LightYellow.ToArgb(), image.GetPixel(bounds.Left, bounds.Top + bounds.Height / 2).ToArgb());
            g.Clear(Color.Transparent); painter.DrawPopupFrame(c with { Theme = theme with { PopupBorder = Color.FromArgb(128, 0, 0, 255) } });
            Assert.Equal(128, image.GetPixel(bounds.Left, bounds.Top + bounds.Height / 2).A);
            g.Clear(Color.Transparent); painter.DrawPopupFrame(c with { Bounds = new(bounds.Location, new Size(1, 1)), Theme = theme with { PopupAppearance = settings with { BorderWidth = 32 } } });
        }
    });

    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Horizontal_offsets_falloff_cache_and_clearance_keep_shadows_inside_allocated_bounds(int dpi) => FoundationTests.Sta(() =>
    {
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
        using var painter = new RibbonOffice2024Renderer();
        using var image = new Bitmap(S(380), S(260)); using var g = Graphics.FromImage(image);
        var bounds = new Rectangle(S(60), S(60), S(240), S(140));
        var theme = RibbonThemes.Office2024Light with { PopupShadowBlur = 12, PopupShadowVerticalOffset = -4, PopupShadowColor = Color.FromArgb(120, 0, 0, 0) };
        var c = new RibbonPaintContext(g, bounds, theme, Control.DefaultFont, dpi);
        g.Clear(Color.Transparent); painter.DrawPopupShadow(c); var resting = Ink(image);
        foreach (int offset in new[] { -16, 0, 16 })
        {
            var settings = new RibbonPopupAppearanceSettings { ShadowHorizontalOffset = offset, ShadowFalloff = 1.4 };
            var value = theme with { PopupAppearance = settings };
            Assert.Equal(S(12) + Math.Max(Math.Abs(S(offset)), S(4)), painter.GetPopupShadowPadding(value, dpi));
            g.Clear(Color.Transparent); painter.DrawPopupShadow(c with { Theme = value with { PopupAppearance = settings with { ShadowFalloff = null } } });
            var shifted = resting; shifted.Offset(S(offset), 0); Assert.Equal(shifted, Ink(image));
            int padding = painter.GetPopupShadowPadding(value, dpi); var allocation = Rectangle.Inflate(bounds, padding, padding);
            Assert.True(allocation.Contains(Ink(image)));
        }
        int x = bounds.Right + S(4), y = bounds.Top + bounds.Height / 2;
        g.Clear(Color.Transparent); painter.DrawPopupShadow(c with { Theme = theme with { PopupAppearance = new() { ShadowFalloff = 1.4 } } }); int soft = image.GetPixel(x, y).A;
        g.Clear(Color.Transparent); painter.DrawPopupShadow(c with { Theme = theme with { PopupAppearance = new() { ShadowFalloff = 4.5 } } }); Assert.True(soft > image.GetPixel(x, y).A);
        g.Clear(Color.Transparent); painter.DrawPopupShadow(c with { Theme = theme with { PopupAppearance = new() { ShadowFalloff = 1.4 } } }); Assert.Equal(soft, image.GetPixel(x, y).A);
        foreach (double falloff in new[] { .1, 16 })
        { g.Clear(Color.Transparent); painter.DrawPopupShadow(c with { Bounds = new(bounds.Location, new Size(1, 1)), Theme = theme with { PopupAppearance = new() { ShadowFalloff = falloff } } }); }
        g.Clear(Color.White); painter.DrawBody(c); using var body = (Bitmap)image.Clone();
        g.Clear(Color.White); painter.DrawBody(c with { Theme = theme with { PopupAppearance = new() { ShadowFalloff = 1.4, ShadowHorizontalOffset = 16 } } }); Same(body, image);
    });

    [Theory] [InlineData(false, 0)] [InlineData(false, 3)] [InlineData(true, 3)]
    public void Native_menu_choice_and_tip_clearance_share_actual_renderer_border_without_replacing_editors(bool custom, int width) => FoundationTests.Sta(() =>
    {
        using var painter = new BorderHook { Custom = custom };
        using var form = new Form { Bounds = new(-16000, -16000, 1000, 700), ShowInTaskbar = false };
        using var ribbon = new RibbonControl { Renderer = painter, AnimationLevel = RibbonAnimationLevel.None, Theme = RibbonThemes.Office2024Light with { PopupAppearance = new() { BorderWidth = width }, EditorAppearance = new() { ChoiceTextHorizontalPadding = 16 } } };
        var tab = new RibbonTab(); var group = new RibbonGroup(); var menu = new RibbonDropDownItem { Text = "Menu" }; var text = new RibbonTextBoxItem { Text = "Name", Value = "Retained" }; menu.Items.Add(text); group.Items.Add(menu); tab.Groups.Add(group); ribbon.Tabs.Add(tab); form.Controls.Add(ribbon);
        form.Show(); Application.DoEvents(); menu.ShowDropDown();
        var surface = ribbon.Popups.Top!; var window = (RibbonPopupWindow)surface.Parent!;
        int inset = Math.Max(1, painter.GetPopupBorderWidth(ribbon.Theme, ribbon.DeviceDpi));
        Assert.Equal(new Padding(inset), window.Padding); Assert.Equal(surface.Width + 2 * inset, window.Width); Assert.Equal(surface.Height + 2 * inset, window.Height);
        Assert.Equal(new Point(inset, inset), surface.Location); Assert.True(painter.WidthCalls > 0);
        var editor = ribbon.Editors.EditorFor(text)!; nint handle = editor.Handle; editor.Select(1, 3); menu.CloseDropDown(); menu.ShowDropDown();
        Assert.Equal(handle, editor.Handle); Assert.Equal("Retained", editor.Text); Assert.Equal(1, editor.SelectionStart); Assert.Equal(3, editor.SelectionLength); menu.CloseDropDown();
        using var combo = new RibbonComboBoxItem(); combo.Items.AddRange("A sufficiently long choice", "Another choice");
        using var choices = new RibbonChoicePopup(ribbon, combo, new(200, 200, 80, 30), _ => { }, () => { }, () => { });
        var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        var list = (ListBox)typeof(RibbonChoicePopup).GetField("_list", flags)!.GetValue(choices)!;
        var shell = (RibbonPopupWindow)typeof(RibbonChoicePopup).GetField("_window", flags)!.GetValue(choices)!;
        Assert.Equal(new Padding(inset), shell.Padding); Assert.Equal(shell.Width - 2 * inset, list.Width); Assert.Equal(shell.Height - 2 * inset, list.Height);
        Assert.True(list.Width >= TextRenderer.MeasureText(combo.Items[0], ribbon.Font, new(int.MaxValue, int.MaxValue), TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding).Width + 2 * RibbonLayoutEngine.Scale(16, ribbon.DeviceDpi));
        var tip = RibbonScreenTipLayout.Measure("Title", "Description", ribbon.Font, ribbon.DeviceDpi, new(600, 400), ribbon.Theme, inset);
        var originalTip = RibbonScreenTipLayout.Measure("Title", "Description", ribbon.Font, ribbon.DeviceDpi, new(600, 400), ribbon.Theme);
        Assert.Equal(originalTip.Title.Left + inset - 1, tip.Title.Left);
        Assert.InRange(RibbonPopupFrameMetrics.ContentInset(painter, ribbon.Theme, ribbon.DeviceDpi, new(3, 3)), 1, 1);
        choices.Dispose(); form.Close();
    });

    [Theory] [InlineData("BorderWidth", 0, 32)] [InlineData("ShadowHorizontalOffset", -32, 32)]
    public void Invalid_integral_roles_leave_theme_and_builtin_selection_unchanged(string name, int min, int max) => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { BuiltInTheme = RibbonBuiltInTheme.Office2019Light }; var theme = ribbon.Theme; var property = typeof(RibbonPopupAppearanceSettings).GetProperty(name)!;
        foreach (int n in new[] { min - 1, max + 1 })
        {
            var settings = new RibbonPopupAppearanceSettings(); property.SetValue(settings, n); Assert.Throws<ArgumentOutOfRangeException>(() => ribbon.Theme = theme with { PopupAppearance = settings });
            Assert.Same(theme, ribbon.Theme); Assert.Equal(RibbonBuiltInTheme.Office2019Light, ribbon.BuiltInTheme);
        }
        foreach (int? n in new int?[] { min, max, null }) { var settings = new RibbonPopupAppearanceSettings(); property.SetValue(settings, n); ribbon.Theme = theme with { PopupAppearance = settings }; Assert.Same(settings, ribbon.Theme.PopupAppearance); }
    });

    [Fact]
    public void Falloff_validation_immutable_resets_and_public_frame_shadow_hooks_keep_precedence() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { BuiltInTheme = RibbonBuiltInTheme.Office2019Light }; using var other = new RibbonControl(); var theme = ribbon.Theme;
        foreach (double n in new[] { 0d, .099, 16.01, double.NaN, double.PositiveInfinity, double.NegativeInfinity })
        { Assert.Throws<ArgumentOutOfRangeException>(() => ribbon.Theme = theme with { PopupAppearance = new() { ShadowFalloff = n } }); Assert.Same(theme, ribbon.Theme); Assert.Equal(RibbonBuiltInTheme.Office2019Light, ribbon.BuiltInTheme); }
        var settings = new RibbonPopupAppearanceSettings { BorderWidth = 3, ShadowHorizontalOffset = 8, ShadowFalloff = .1 };
        var application = theme with { PopupAppearance = settings, PopupShadowBlur = 12 }; ribbon.Theme = application; ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2024Light; ribbon.BuiltInTheme = null; Assert.Same(application, ribbon.Theme); Assert.Null(other.Theme.PopupAppearance);
        ribbon.Theme = application with { PopupAppearance = settings with { BorderWidth = null, ShadowFalloff = 16 } }; Assert.Equal(8, ribbon.Theme.PopupAppearance!.ShadowHorizontalOffset); Assert.Equal(3, settings.BorderWidth);
        ribbon.Theme = application with { PopupAppearance = null }; Assert.Equal(12, ribbon.Theme.PopupShadowBlur);
        using var painter = new FrameReplacement(); using var image = new Bitmap(100, 80); using var g = Graphics.FromImage(image);
        var c = new RibbonPaintContext(g, new(5, 5, 90, 70), application, ribbon.Font, 120);
        painter.DrawPopupFrame(c); painter.DrawPopupShadow(c); Assert.Equal(Color.Magenta.ToArgb(), image.GetPixel(15, 15).ToArgb()); Assert.Equal(1, painter.Frames); Assert.Equal(1, painter.Shadows);
    });

    private static Rectangle Ink(Bitmap image)
    {
        var bounds = Rectangle.Empty;
        for (int y = 0; y < image.Height; y++) for (int x = 0; x < image.Width; x++) if (image.GetPixel(x, y).A > 0) bounds = bounds.IsEmpty ? new(x, y, 1, 1) : Rectangle.Union(bounds, new(x, y, 1, 1));
        return bounds;
    }
    private static void Same(Bitmap a, Bitmap b) { for (int y = 0; y < a.Height; y++) for (int x = 0; x < a.Width; x++) Assert.Equal(a.GetPixel(x, y), b.GetPixel(x, y)); }
    private sealed class BorderHook : RibbonOffice2024Renderer
    {
        internal bool Custom;
        internal int WidthCalls;
        public override int GetPopupBorderWidth(RibbonTheme theme, int dpi) { WidthCalls++; return Custom ? RibbonLayoutEngine.Scale(5, dpi) : base.GetPopupBorderWidth(theme, dpi); }
    }
    private sealed class FrameReplacement : RibbonRenderer
    {
        internal int Frames, Shadows;
        public override void DrawPopupFrame(RibbonPaintContext c) { Frames++; c.Graphics.FillRectangle(Brushes.Magenta, c.Bounds); }
        public override void DrawPopupShadow(RibbonPaintContext c) => Shadows++;
    }

    [Fact]
    public void Default_popup_paint_clearance_and_placement_match_saved_images() => FoundationTests.Sta(() =>
    {
        foreach (bool classic in new[] { false, true })
        foreach (bool office in new[] { false, true })
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        foreach (int odd in new[] { 0, 1 })
        foreach (bool empty in new[] { false, true })
        {
            using RibbonRenderer renderer = office ? new RibbonOffice2024Renderer() : new RibbonRenderer();
            using var ribbon = new RibbonControl { Renderer = renderer, Theme = (classic ? RibbonThemes.Office2019Light : RibbonThemes.Office2024Light) with { PopupAppearance = empty ? new() : null } };
            using var window = new RibbonPopupWindow(ribbon, () => { });
            int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
            using var image = new Bitmap(S(360), S(240)); using var graphics = Graphics.FromImage(image);
            var bounds = new Rectangle(S(40), S(35), S(275) + odd, S(160) + odd);
            var context = new RibbonPaintContext(graphics, bounds, ribbon.Theme, ribbon.Font, dpi);
            renderer.DrawPopupShadow(context); renderer.DrawPopupFrame(context);
            var anchor = new Rectangle(S(70), S(60), S(100), S(30)); var work = new Rectangle(0, 0, S(1000), S(800));
            var geometry = string.Join(";", renderer.GetPopupCornerRadius(ribbon.Theme, dpi), renderer.GetPopupShadowPadding(ribbon.Theme, dpi), window.Padding,
                RibbonPopupManager.CalculateBounds(anchor, work, new(S(250), S(200)), false, dpi),
                RibbonChoicePopup.CalculateBounds(anchor, work, S(220), S(28), 12, dpi));
            string name = $"popup-{classic}-{office}-{dpi}-{odd}";
            using var stream = new MemoryStream(); image.Save(stream, ImageFormat.Png); var png = stream.ToArray();
            if (Environment.GetEnvironmentVariable("RIBBONKIT_POPUP_APPEARANCE_CAPTURE") is { Length: > 0 } capture)
            { Directory.CreateDirectory(capture); File.WriteAllBytes(Path.Combine(capture, name + ".png"), png); File.WriteAllText(Path.Combine(capture, name + ".txt"), geometry); }
            if (Environment.GetEnvironmentVariable("RIBBONKIT_POPUP_APPEARANCE_BASELINE") is { Length: > 0 } baseline)
            { Assert.Equal(File.ReadAllBytes(Path.Combine(baseline, name + ".png")), png); Assert.Equal(File.ReadAllText(Path.Combine(baseline, name + ".txt")), geometry); }
        }
    });
}
