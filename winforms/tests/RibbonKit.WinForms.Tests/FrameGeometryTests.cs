using System.Text.Json;
using System.Runtime.InteropServices;
using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class FrameGeometryTests
{
    private static RibbonTheme Rounder => RibbonThemes.Office2024Light with { BodyCornerRadius = 14, BackstagePageCornerRadius = 18, BodyShadowBlur = 12, BodyShadowVerticalOffset = 4, PopupShadowBlur = 12, PopupShadowVerticalOffset = 4 };
    public static TheoryData<string, int, int> Roles => new()
    {
        { nameof(RibbonTheme.FocusCornerRadius), 0, 64 }, { nameof(RibbonTheme.KeyTipCornerRadius), 0, 64 },
        { nameof(RibbonTheme.CheckBoxCornerRadius), 0, 64 }, { nameof(RibbonTheme.BackstageBackButtonCornerRadius), 0, 64 },
        { nameof(RibbonTheme.NavigationMarkerCornerRadius), 0, 64 }, { nameof(RibbonTheme.ApplicationMenuFooterCornerRadius), 0, 64 },
        { nameof(RibbonTheme.ApplicationMenuFileButtonCornerRadius), 0, 64 }, { nameof(RibbonTheme.HeaderIndicatorCornerRadius), 0, 64 },
        { nameof(RibbonTheme.BodyCornerRadius), 0, 64 }, { nameof(RibbonTheme.BackstagePageCornerRadius), 0, 64 },
        { nameof(RibbonTheme.BodyShadowBlur), 0, 32 }, { nameof(RibbonTheme.PopupShadowBlur), 0, 32 },
        { nameof(RibbonTheme.BodyShadowVerticalOffset), -32, 32 }, { nameof(RibbonTheme.PopupShadowVerticalOffset), -32, 32 }
    };
    [Theory] [MemberData(nameof(Roles))]
    public void Frame_roles_validate_before_assignment_and_reset_independently(string name, int minimum, int maximum) => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl(); using var other = new RibbonControl(); ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2024Light; var original = ribbon.Theme;
        var property = typeof(RibbonTheme).GetProperty(name)!;
        foreach (int value in new[] { minimum - 1, maximum + 1 })
        {
            var invalid = Rounder with { }; property.SetValue(invalid, value); Assert.Throws<ArgumentOutOfRangeException>(() => ribbon.Theme = invalid);
            Assert.Same(original, ribbon.Theme); Assert.Equal(RibbonBuiltInTheme.Office2024Light, ribbon.BuiltInTheme);
        }
        foreach (int value in new[] { minimum, maximum })
        {
            var valid = Rounder with { BodyMargin = new(13, 3, 9, 11), GalleryContentPadding = 10, BackstagePagePadding = 18, KeyTipHorizontalPadding = 6 };
            property.SetValue(valid, value); ribbon.Theme = valid; var reset = valid with { }; property.SetValue(reset, null); ribbon.Theme = reset;
            foreach (var row in Roles) Assert.Equal((string)row[0] == name ? null : typeof(RibbonTheme).GetProperty((string)row[0])!.GetValue(valid), typeof(RibbonTheme).GetProperty((string)row[0])!.GetValue(ribbon.Theme));
            Assert.Same(original, other.Theme); Assert.Equal(new Padding(13, 3, 9, 11), ribbon.Theme.BodyMargin); Assert.Equal(10, ribbon.Theme.GalleryContentPadding); Assert.Equal(18, ribbon.Theme.BackstagePagePadding); Assert.Equal(6, ribbon.Theme.KeyTipHorizontalPadding);
        }
    });

    [Theory] [InlineData(false)] [InlineData(true)]
    public void Frame_radius_shadow_and_clearance_agree_at_each_dpi_and_preserve_corner_symmetry(bool office) => FoundationTests.Sta(() =>
    {
        using RibbonRenderer painter = office ? new RibbonOffice2024Renderer() : new RibbonRenderer();
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        foreach (int offset in new[] { -4, 0, 4 })
        foreach (int odd in new[] { 0, 1 })
        {
            int S(int n) => RibbonLayoutEngine.Scale(n, dpi); var theme = Rounder with { BodyShadowVerticalOffset = offset, PopupShadowVerticalOffset = offset };
            var bounds = new Rectangle(S(24), S(24), S(240) + odd, S(130) + odd); using var image = new Bitmap(bounds.Right + S(24), bounds.Bottom + S(24)); using var g = Graphics.FromImage(image);
            g.Clear(Color.White); painter.DrawBody(new(g, bounds, theme with { BodyShadowColor = Color.Transparent }, Control.DefaultFont, dpi));
            Assert.Equal(office ? S(14) : 0, painter.GetBodyCornerRadius(theme, dpi)); Assert.Equal(office ? S(18) : 0, painter.GetBackstagePageCornerRadius(theme, dpi));
            if (office)
            {
                int radius = S(14);
                for (int y = 0; y < radius; y++) for (int x = 0; x < radius; x++)
                {
                    var c = image.GetPixel(bounds.Left + x, bounds.Top + y);
                    Assert.Equal(c, image.GetPixel(bounds.Right - 1 - x, bounds.Top + y)); Assert.Equal(c, image.GetPixel(bounds.Left + x, bounds.Bottom - 1 - y)); Assert.Equal(c, image.GetPixel(bounds.Right - 1 - x, bounds.Bottom - 1 - y));
                }
            }
            var bodyPadding = painter.GetBodyShadowPadding(theme, dpi); Assert.Equal(office ? new Padding(S(12), Math.Max(0, S(12) - S(offset)), S(12), Math.Max(0, S(12) + S(offset))) : Padding.Empty, bodyPadding);
            Assert.Equal(S(12) + Math.Abs(S(offset)), painter.GetPopupShadowPadding(theme, dpi));
            g.Clear(Color.Transparent); painter.DrawPopupShadow(new(g, bounds, theme, Control.DefaultFont, dpi));
            int padding = painter.GetPopupShadowPadding(theme, dpi); var allocated = Rectangle.Inflate(bounds, padding, padding);
            for (int y = 0; y < image.Height; y++) for (int x = 0; x < image.Width; x++) if (!allocated.Contains(x, y)) Assert.Equal(0, image.GetPixel(x, y).A);
            if (odd == 0 && Environment.GetEnvironmentVariable("RIBBONKIT_FRAME_CAPTURE") is { Length: > 0 } output)
            { Directory.CreateDirectory(output); painter.DrawPopupFrame(new(g, bounds, theme, Control.DefaultFont, dpi)); image.Save(Path.Combine(output, $"override-{office}-{dpi}-{offset}.png")); }
        }
        foreach (int blur in new[] { 0, 32 })
        foreach (int offset in new[] { -32, 32 })
        {
            var theme = Rounder with { BodyShadowBlur = blur, PopupShadowBlur = blur, BodyShadowVerticalOffset = offset, PopupShadowVerticalOffset = offset };
            using var image = new Bitmap(220, 200); using var g = Graphics.FromImage(image);
            painter.DrawBody(new(g, new(80, 70, 1, 1), theme, Control.DefaultFont, 96)); painter.DrawPopupShadow(new(g, new(80, 70, 1, 1), theme, Control.DefaultFont, 96));
            if (blur == 0) Assert.Equal(Padding.Empty, painter.GetBodyShadowPadding(theme, 96));
        }
    });

    [Theory] [InlineData(RibbonLayoutMode.Classic)] [InlineData(RibbonLayoutMode.Simplified)]
    public void Effective_margins_join_body_qat_and_messages_without_changing_native_models(RibbonLayoutMode mode) => FoundationTests.Sta(() =>
    {
        using var form = new Form { Width = 1250, Height = 800 }; var ribbon = new RibbonControl { AnimationLevel = RibbonAnimationLevel.None }; form.Controls.Add(ribbon);
        var tab = new RibbonTab { Text = "Home" }; var group = new RibbonGroup { Text = "Editing", CanResize = false }; tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        var field = new RibbonTextBoxItem { Text = "Name", Value = "Retained", SimplifiedPlacement = RibbonSimplifiedPlacement.Inline }; var command = new RibbonButtonItem { Text = "Open", SimplifiedPlacement = RibbonSimplifiedPlacement.Inline }; group.Items.AddRange(field, command);
        ribbon.FileMenu.Visible = true; ribbon.QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon; ribbon.QuickAccessItems.Add(new RibbonQuickAccessReference { Source = command });
        ribbon.MessageBar.Items.Add(new RibbonMessage { Title = "Notice", Text = "Retained message" });
        form.Show(); Application.DoEvents(); var editor = ribbon.Editors.EditorFor(field)!; var editorSize = editor.Size; nint handle = editor.Handle; var qat = ribbon.QuickAccessItems.ToArray();
        // Simplified uses a launcher for this field; retain the real editor while parked.
        ribbon.LayoutMode = mode;
        foreach (var density in Enum.GetValues<RibbonDensity>())
        foreach (bool minimized in new[] { false, true })
        {
            ribbon.Theme = RibbonThemes.Office2024Light; ribbon.Density = density; ribbon.IsMinimized = minimized; Application.DoEvents(); var before = ribbon.Snapshot; int beforeHeight = ribbon.Height;
            ribbon.Theme = Rounder; Application.DoEvents(); var changed = ribbon.Snapshot; int S(int n) => RibbonLayoutEngine.Scale(n, ribbon.DeviceDpi);
            Assert.True(ribbon.Height >= beforeHeight); Assert.Equal(new Padding(7, 1, 7, 7), ribbon.Theme.BodyMargin);
            Assert.True(S(ribbon.LayoutTheme.BodyMargin.Left) >= S(12)); Assert.True(S(ribbon.LayoutTheme.BodyMargin.Bottom) >= S(16));
            Assert.Equal(changed.QuickAccessBounds.Left, changed.MessageBounds.Left); Assert.Equal(changed.QuickAccessBounds.Right, changed.MessageBounds.Right);
            Assert.Equal(changed.QuickAccessBounds.Bottom, changed.MessageBounds.Top);
            if (!minimized) { Assert.Equal(changed.BodyBounds.Left, changed.QuickAccessBounds.Left); Assert.Equal(changed.BodyBounds.Right, changed.QuickAccessBounds.Right); Assert.Equal(changed.BodyBounds.Left, changed.Targets.Single(t => t.Kind == TargetKind.FileMenu).Bounds.Left); }
            Assert.Equal(handle, editor.Handle); Assert.Equal("Retained", field.Value); Assert.Equal(qat, ribbon.QuickAccessItems);
            ribbon.Theme = RibbonThemes.Office2024Light; Assert.Equal(before.BodyBounds, ribbon.Snapshot.BodyBounds); Assert.Equal(before.MessageBounds, ribbon.Snapshot.MessageBounds); Assert.Equal(beforeHeight, ribbon.Height);
        }
        ribbon.IsMinimized = false; ribbon.Density = RibbonDensity.Compact; Application.DoEvents(); Assert.Equal(editorSize, editor.Size);
    });

    [Theory] [InlineData(false)] [InlineData(true)]
    public void Popup_chrome_cache_and_gallery_motion_share_the_renderer_footprint(bool custom) => FoundationTests.Sta(() =>
    {
        using var painter = new FootprintRenderer { Custom = custom }; using var form = new Form { Bounds = new(100, 100, 1100, 750) };
        var ribbon = new RibbonControl { Renderer = painter, Theme = Rounder, AnimationLevel = RibbonAnimationLevel.Expressive }; form.Controls.Add(ribbon);
        var gallery = new RibbonGalleryItem { Text = "Styles", ColumnCount = 3, MaxVisibleRows = 2 };
        for (int i = 0; i < 24; i++) gallery.Items.Add(new RibbonGalleryTile { Text = "Style " + i });
        var group = new RibbonGroup(); group.Items.Add(gallery); var tab = new RibbonTab { Text = "Home" }; tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        form.Show(); Application.DoEvents(); ribbon.Motion.SystemPolicyOverride = true;
        foreach (int offset in new[] { -4, 4 })
        {
            ribbon.Theme = Rounder with { PopupShadowVerticalOffset = offset }; gallery.ShowDropDown();
            var surface = ribbon.Popups.Top!; var popup = (RibbonPopupWindow)surface.Parent!; var motion = popup.OpeningMotion!;
            int padding = RibbonFrameMetrics.PopupPadding(painter, ribbon.Theme, ribbon.DeviceDpi); var expected = Rectangle.Inflate(popup.Bounds, padding, padding);
            Assert.True(motion.FadesGallery); Assert.Equal(expected, motion.Bounds); var selection = gallery.SelectedItem; nint handle = surface.Handle;
            popup.SettleMotion();
            using (var dismissal = RibbonTabPopupDismissal.Start(ribbon, popup, surface)) { Assert.NotNull(dismissal); Assert.Equal(expected, dismissal!.Bounds); }
            nint chrome = GetWindow(popup.Handle, 2); Assert.NotEqual(0, chrome); Assert.True(GetWindowRect(chrome, out var bounds)); Assert.Equal(expected, bounds.Rectangle);
            Assert.Equal(0x080800A0L, GetWindowLongPtrW(chrome, -20).ToInt64() & 0x080800A0L); Assert.Same(selection, gallery.SelectedItem); Assert.Equal(handle, surface.Handle);
            Assert.NotNull(popup.Region); Assert.False(popup.Region!.IsVisible(0, 0)); Assert.True(popup.Region.IsVisible(popup.Width / 2, popup.Height / 2)); gallery.CloseDropDown();
        }
        // Exercise a mutable renderer without changing the theme, radius or native HWND.
        using var card = new Form { FormBorderStyle = FormBorderStyle.None, Bounds = new(200, 300, 320, 180) }; card.Show(form);
        using var cache = new PopupChromeWindow(); int radius = painter.GetPopupCornerRadius(ribbon.Theme, ribbon.DeviceDpi);
        cache.Update(card, ribbon, radius); nint cachedHandle = cache.Handle; int calls = painter.Frames;
        cache.Update(card, ribbon, radius); Assert.Equal(calls, painter.Frames);
        painter.Custom = true; painter.Padding = 41; cache.Update(card, ribbon, radius); Assert.Equal(cachedHandle, cache.Handle); Assert.True(painter.Frames > calls);
        Assert.True(GetWindowRect(cache.Handle, out var changed)); Assert.Equal(Rectangle.Inflate(card.Bounds, 41, 41), changed.Rectangle);
    });

    [Fact]
    public void Passive_tip_reserves_custom_shadow_clearance_at_the_work_area_edge_without_activation() => FoundationTests.Sta(() =>
    {
        using var painter = new FootprintRenderer { Custom = true, Padding = 42 }; using var form = new Form { Bounds = new(80, 80, 1000, 700) };
        var ribbon = new RibbonControl { Renderer = painter, Theme = Rounder }; var input = new TextBox { Text = "Retained selection", Bounds = new(40, 400, 240, 30) }; form.Controls.AddRange([ribbon, input]);
        form.Show(); Application.DoEvents(); input.Focus(); input.Select(2, 4); var handle = input.Handle; var work = Screen.FromHandle(form.Handle).WorkingArea;
        var item = new RibbonButtonItem { Text = "Edge tip", ScreenTipDescription = "The complete custom shadow stays within the working area." };
        using var tip = new RibbonScreenTipWindow(ribbon, item, new(work.Right - 40, work.Bottom - 30, 30, 20), new(work.Right - 15, work.Bottom - 10)); tip.Show(form); tip.RaiseAbovePopups(); Application.DoEvents();
        Assert.True(work.Contains(Rectangle.Inflate(tip.Bounds, 42, 42))); Assert.True(input.Focused); Assert.Equal(handle, input.Handle); Assert.Equal(2, input.SelectionStart); Assert.Equal(4, input.SelectionLength);
    });

    [Fact]
    public void Null_popup_clearance_keeps_legacy_summed_rounding_at_intermediate_dpi()
    {
        using var painter = new RibbonRenderer();
        for (int dpi = 96; dpi <= 240; dpi++) Assert.Equal(RibbonLayoutEngine.Scale(8, dpi), painter.GetPopupShadowPadding(RibbonThemes.Office2024Light, dpi));
    }

    [Fact]
    public void Custom_shadow_clearance_is_bounded_and_does_not_rewrite_authored_margins() => FoundationTests.Sta(() =>
    {
        using var painter = new FootprintRenderer { Custom = true, Padding = int.MaxValue };
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        {
            int S(int n) => RibbonLayoutEngine.Scale(n, dpi); var theme = Rounder with { BodyMargin = new(9, 2, 13, 11) };
            Assert.Equal(S(128), RibbonFrameMetrics.PopupPadding(painter, theme, dpi));
            Assert.Equal(new Padding(128), new RibbonFrameMetrics(theme).LayoutMargin(painter, dpi)); Assert.Equal(new Padding(9, 2, 13, 11), theme.BodyMargin);
            Assert.Equal(theme.BodyMargin, new RibbonFrameMetrics(theme with { BodyShadowBlur = null, BodyShadowVerticalOffset = null }).LayoutMargin(painter, dpi));
            painter.Padding = -10; Assert.Equal(0, RibbonFrameMetrics.PopupPadding(painter, theme, dpi)); Assert.Equal(theme.BodyMargin, new RibbonFrameMetrics(theme).LayoutMargin(painter, dpi)); painter.Padding = int.MaxValue;
        }
    });

    [Fact]
    public void Overridden_joined_qat_adds_only_its_single_device_pixel_separator() => FoundationTests.Sta(() =>
    {
        using var painter = new RibbonOffice2024Renderer();
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        {
            int S(int n) => RibbonLayoutEngine.Scale(n, dpi); var bounds = new Rectangle(S(24), S(24), S(260), S(160)); var row = new Rectangle(bounds.Left, bounds.Bottom - S(32), bounds.Width, S(32));
            using var expected = new Bitmap(bounds.Right + S(24), bounds.Bottom + S(24)); using var actual = new Bitmap(expected.Width, expected.Height);
            using (var g = Graphics.FromImage(expected)) { g.Clear(Rounder.Background); painter.DrawBody(new(g, bounds, Rounder, Control.DefaultFont, dpi)); }
            using (var g = Graphics.FromImage(actual)) { g.Clear(Rounder.Background); painter.DrawBody(new(g, bounds, Rounder, Control.DefaultFont, dpi)); painter.DrawQuickAccessToolbar(new(g, row, Rounder, Control.DefaultFont, dpi) { IsQuickAccessToolbarJoined = true }, RibbonQuickAccessPosition.BelowRibbon); }
            for (int y = 0; y < actual.Height; y++) for (int x = 0; x < actual.Width; x++)
                Assert.Equal(y == row.Top && x > row.Left && x < row.Right - 1 ? Rounder.Border.ToArgb() : expected.GetPixel(x, y).ToArgb(), actual.GetPixel(x, y).ToArgb());
        }
    });

    private sealed class FootprintRenderer : RibbonOffice2024Renderer
    {
        internal bool Custom; internal int Padding = 30, Frames;
        public override int GetPopupShadowPadding(RibbonTheme theme, int deviceDpi) => Custom ? Padding : base.GetPopupShadowPadding(theme, deviceDpi);
        public override Padding GetBodyShadowPadding(RibbonTheme theme, int deviceDpi) => Custom ? new(Padding) : base.GetBodyShadowPadding(theme, deviceDpi);
        public override void DrawPopupFrame(RibbonPaintContext c) { Frames++; base.DrawPopupFrame(c); }
    }
    [StructLayout(LayoutKind.Sequential)] private struct NativeRect { internal int Left, Top, Right, Bottom; internal readonly Rectangle Rectangle => Rectangle.FromLTRB(Left, Top, Right, Bottom); }
    [DllImport("user32.dll")] private static extern bool GetWindowRect(nint window, out NativeRect bounds);
    [DllImport("user32.dll")] private static extern nint GetWindow(nint window, uint command);
    [DllImport("user32.dll")] private static extern nint GetWindowLongPtrW(nint window, int index);

    [Fact]
    public void Backstage_radius_changes_share_native_page_clip_and_preserve_controls() => FoundationTests.Sta(() =>
    {
        using var form = new Form { Width = 1050, Height = 700 }; var ribbon = new RibbonControl { FileMenuMode = RibbonFileMenuMode.Backstage, AnimationLevel = RibbonAnimationLevel.None }; form.Controls.Add(ribbon); ribbon.FileMenu.Visible = true;
        using var page = new TextBox { Text = "Retained page" }; var model = new RibbonBackstagePageItem { Text = "Info", Content = page }; ribbon.Backstage.Items.Add(model);
        form.Show(); ribbon.IsBackstageOpen = true; Application.DoEvents(); nint handle = page.Handle; var parent = page.Parent; var frame = page.Parent!.Parent!;
        var before = frame.Bounds;
        foreach (int radius in new[] { 0, 18, 64 })
        {
            ribbon.Theme = Rounder with { BackstagePageCornerRadius = radius }; Application.DoEvents(); Assert.Equal(handle, page.Handle); Assert.Same(parent, page.Parent); Assert.Equal(before, frame.Bounds);
            Assert.Equal("Retained page", page.Text); Assert.Same(model, ribbon.Backstage.SelectedPage);
            Assert.Equal(radius == 0, frame.Region is null); if (radius > 0) { Assert.False(frame.Region!.IsVisible(0, 0)); Assert.True(frame.Region.IsVisible(frame.Width / 2, 0)); }
        }
        ribbon.Theme = RibbonThemes.Office2024Light; Assert.Equal(handle, page.Handle);
    });
    [Fact]
    public void Null_frame_geometry_and_pixels_match_saved_defaults() => FoundationTests.Sta(() =>
    {
        foreach (bool office in new[] { false, true })
        foreach (var density in Enum.GetValues<RibbonDensity>())
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        foreach (bool minimized in new[] { false, true })
        {
            using RibbonRenderer painter = office ? new RibbonOffice2024Renderer() : new RibbonRenderer();
            using var ribbon = new RibbonControl { Width = 900, Renderer = painter, Density = density, IsMinimized = minimized, AnimationLevel = RibbonAnimationLevel.None };
            var tab = new RibbonTab { Text = "Home" }; var group = new RibbonGroup { Text = "Editing", CanResize = false }; var button = new RibbonButtonItem { Text = "Open", Icon = RibbonStockIcons.Create("open") };
            group.Items.AddRange(button, new RibbonTextBoxItem { Text = "Name", Value = "Retained" }); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
            ribbon.QuickAccessItems.Add(new RibbonQuickAccessReference { Source = button }); ribbon.QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon;
            ribbon.MessageBar.Items.Add(new RibbonMessage { Title = "Notice", Text = "Review the document." });
            int S(int n) => RibbonLayoutEngine.Scale(n, dpi); using var image = new Bitmap(S(680), S(310)); using var g = Graphics.FromImage(image); g.Clear(ribbon.Theme.Background);
            var panel = new Rectangle(S(16), S(16), S(300), S(160)); var popup = new Rectangle(S(360), S(16), S(280), S(180));
            var context = new RibbonPaintContext(g, panel, ribbon.Theme, ribbon.Font, dpi) { Density = density };
            painter.DrawBody(context); painter.DrawQuickAccessToolbar(context with { Bounds = new(panel.Left, panel.Bottom - S(32), panel.Width, S(32)), IsQuickAccessToolbarJoined = office && !minimized }, RibbonQuickAccessPosition.BelowRibbon);
            painter.DrawPopupShadow(context with { Bounds = popup }); painter.DrawPopupFrame(context with { Bounds = popup });
            painter.DrawBackstageBackground(context with { Bounds = new(S(16), S(220), S(624), S(70)) }, S(160));
            var snapshot = ribbon.Snapshot;
            var data = JsonSerializer.Serialize(new { snapshot.BodyBounds, snapshot.BodyFrameBounds, snapshot.QuickAccessBounds, snapshot.MessageBounds, Height = ribbon.Height, Margin = ribbon.LayoutTheme.BodyMargin,
                PopupRadius = painter.GetPopupCornerRadius(ribbon.Theme, dpi), PageRadius = painter.GetBackstagePageCornerRadius(ribbon.Theme, dpi), Targets = snapshot.Targets.Select(t => t.Bounds) });
            using var png = new MemoryStream(); image.Save(png, System.Drawing.Imaging.ImageFormat.Png); string name = $"frame-{office}-{density}-{dpi}-{minimized}";
            if (Environment.GetEnvironmentVariable("RIBBONKIT_FRAME_CAPTURE") is { Length: > 0 } output)
            { Directory.CreateDirectory(output); File.WriteAllBytes(Path.Combine(output, name + ".png"), png.ToArray()); File.WriteAllText(Path.Combine(output, name + ".json"), data); }
            if (Environment.GetEnvironmentVariable("RIBBONKIT_FRAME_BASELINE") is { Length: > 0 } baseline)
            { Assert.Equal(File.ReadAllBytes(Path.Combine(baseline, name + ".png")), png.ToArray()); Assert.Equal(File.ReadAllText(Path.Combine(baseline, name + ".json")), data); }
        }
    });
}
