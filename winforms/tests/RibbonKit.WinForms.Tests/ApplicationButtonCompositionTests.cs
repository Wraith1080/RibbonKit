using System.ComponentModel;
using System.Drawing.Drawing2D;
using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class ApplicationButtonCompositionTests
{
    private static RibbonTheme OrbTheme => RibbonThemes.Office2010Blue with
    {
        ApplicationButtonShape = RibbonApplicationButtonShape.Orb,
        ApplicationButtonAppearance = new()
        {
            Background = new(Color.Silver), HoverBackground = new(Color.Gold), PressedBackground = new(Color.Orange), OpenBackground = new(Color.DarkOrange),
            BorderColor = Color.Navy, HoverBorderColor = Color.Red, OpenBorderColor = Color.DarkRed, GlyphSize = 0
        }
    };

    [Fact]
    public void Shape_override_reset_is_independent_of_theme_route_and_borrowed_glyph() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { Theme = OrbTheme, FileMenuMode = RibbonFileMenuMode.Backstage };
        var file = ribbon.FileMenu; var property = TypeDescriptor.GetProperties(ribbon)[nameof(ribbon.ApplicationButtonShape)]!;
        Assert.Null(ribbon.ApplicationButtonShape); Assert.Equal(RibbonApplicationButtonShape.Orb, ribbon.EffectiveApplicationButtonShape);
        ribbon.ApplicationButtonShape = RibbonApplicationButtonShape.Rectangular; Assert.True(property.ShouldSerializeValue(ribbon));
        ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2013Light; ribbon.BuiltInTheme = null;
        Assert.Equal(RibbonApplicationButtonShape.Rectangular, ribbon.EffectiveApplicationButtonShape);
        property.ResetValue(ribbon); Assert.Equal(RibbonApplicationButtonShape.Orb, ribbon.EffectiveApplicationButtonShape);
        Assert.Same(file, ribbon.FileMenu); Assert.Equal("F", file.KeyTip); Assert.Equal(RibbonFileMenuMode.Backstage, ribbon.FileMenuMode);
        var icon = new Glyph(); ribbon.ApplicationButtonIcon = icon; ribbon.ApplicationButtonIcon = null; ribbon.Dispose(); Assert.False(icon.Disposed);
    });

    [Fact]
    public void Invalid_shape_and_geometry_are_rejected_before_theme_or_override_mutation() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { Theme = OrbTheme }; var original = ribbon.Theme;
        Assert.Throws<ArgumentOutOfRangeException>(() => ribbon.ApplicationButtonShape = (RibbonApplicationButtonShape)91); Assert.Null(ribbon.ApplicationButtonShape);
        Assert.Throws<ArgumentOutOfRangeException>(() => ribbon.Theme = original with { ApplicationButtonShape = (RibbonApplicationButtonShape)91 });
        foreach (var bad in new[] { new RibbonApplicationButtonAppearanceSettings { Diameter = 15 }, new() { TouchDiameter = 43 }, new() { CaptionOverlap = -1 }, new() { LeftInset = 1025 }, new() { RightGap = -1 }, new() { MenuOffset = new(1025, 0) }, new() { MenuTopBandHeight = -1 }, new() { BorderWidth = 33 }, new() { InnerBorderWidth = -1 }, new() { GlyphSize = 129 }, new() { ShadowBlur = 33 } })
        { Assert.Throws<ArgumentOutOfRangeException>(() => ribbon.Theme = original with { ApplicationButtonAppearance = bad }); Assert.Same(original, ribbon.Theme); }
    });

    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Orb_geometry_is_square_uses_the_available_header_and_clamps_narrow_hosts(int dpi)
    {
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
        var appearance = new RibbonApplicationButtonAppearanceSettings();
        foreach (var density in Enum.GetValues<RibbonDensity>()) foreach (int caption in new[] { 0, S(36), S(48) }) foreach (int available in new[] { 0, 1, S(25), S(200) })
        {
            var box = RibbonApplicationButtonMetrics.Measure(appearance, density, dpi, caption, S(32), available);
            Assert.Equal(box.Width, box.Height);
            if (box.IsEmpty) continue;
            Assert.True(box.Left >= 0 && box.Right <= available); Assert.InRange(box.Top, 0, caption + S(32)); Assert.True(box.Bottom <= caption + S(32));
            Assert.True(RibbonApplicationButtonMetrics.Contains(box, new(box.Left + box.Width / 2, box.Top + box.Height / 2)));
            if (box.Width > 4) Assert.False(RibbonApplicationButtonMetrics.Contains(box, box.Location));
        }
    }

    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Both_painters_use_one_circle_for_state_rims_clipping_focus_and_disabled_feedback(int dpi) => FoundationTests.Sta(() =>
    {
        foreach (bool office in new[] { false, true })
        {
            using RibbonRenderer painter = office ? new RibbonOffice2024Renderer() : new RibbonRenderer();
            using var menu = new RibbonDropDownItem(); using var image = new Bitmap(81, 81); using var g = Graphics.FromImage(image);
            var c = new RibbonPaintContext(g, new(10, 10, 61, 61), OrbTheme, Control.DefaultFont, dpi);
            foreach (var (hot, pressed, open, color) in new[] { (false, false, false, Color.Silver), (true, false, false, Color.Gold), (true, true, false, Color.Orange), (false, false, true, Color.DarkOrange) })
            {
                g.Clear(Color.Transparent); painter.DrawFileButton(c with { Hot = hot, Pressed = pressed, FileMenuPreviewOpen = open }, menu);
                Assert.Equal(color.ToArgb(), image.GetPixel(40, 40).ToArgb()); Assert.Equal(0, image.GetPixel(10, 10).A);
                for (int y = 0; y < 61; y++) for (int x = 0; x < 61; x++)
                {
                    Assert.Equal(image.GetPixel(10 + x, 10 + y), image.GetPixel(70 - x, 10 + y));
                    Assert.Equal(image.GetPixel(10 + x, 10 + y), image.GetPixel(10 + x, 70 - y));
                }
            }
            menu.Enabled = false; g.Clear(Color.Transparent); painter.DrawFileButton(c with { Hot = true, Pressed = true, FileMenuPreviewOpen = true }, menu);
            Assert.Equal(Color.Silver.ToArgb(), image.GetPixel(40, 40).ToArgb());
            menu.Enabled = true; g.SetClip(new Rectangle(35, 20, 8, 30)); g.CompositingMode = CompositingMode.SourceCopy;
            painter.DrawFileButton(c with { Focused = true }, menu); Assert.Equal(CompositingMode.SourceCopy, g.CompositingMode); Assert.Equal(new Rectangle(35, 20, 8, 30), Rectangle.Round(g.ClipBounds));
        }
    });

    [Fact]
    public void Shared_glyph_hook_uses_borrowed_artwork_and_never_substitutes_form_icons() => FoundationTests.Sta(() =>
    {
        using var painter = new GlyphPainter(); using var image = new Bitmap(70, 70); using var g = Graphics.FromImage(image); using var menu = new RibbonDropDownItem();
        var icon = new Glyph(); var theme = OrbTheme with { ApplicationButtonAppearance = OrbTheme.ApplicationButtonAppearance! with { GlyphSize = 22 } };
        var c = new RibbonPaintContext(g, new(2, 2, 60, 60), theme, Control.DefaultFont, 120) { ApplicationButtonIcon = icon };
        painter.DrawFileButton(c, menu); Assert.Equal(1, painter.Calls); Assert.Equal(1, icon.Calls); Assert.True(icon.Enabled); Assert.True(c.Bounds.Contains(icon.Bounds));
        menu.Enabled = false; painter.DrawFileButton(c, menu); Assert.False(icon.Enabled); Assert.False(icon.Disposed);
        painter.DrawFileButton(c with { ApplicationButtonIcon = null }, menu); Assert.Equal(2, icon.Calls); Assert.Equal(3, painter.Calls);
        menu.Enabled = true; painter.DrawFileButton(c with { Focused = true }, menu); Assert.Equal(1, painter.FocusCalls);
    });

    [Theory] [InlineData(RibbonDensity.Compact)] [InlineData(RibbonDensity.Touch)]
    public void Caption_overlap_is_a_real_file_target_and_qat_title_tabs_and_native_resize_remain_separate(RibbonDensity density) => FoundationTests.Sta(() =>
    {
        using var form = new RibbonForm { Bounds = new(-16000, -16000, 1000, 650), StartPosition = FormStartPosition.Manual, ShowInTaskbar = false };
        using var ribbon = new Probe { Theme = OrbTheme, Density = density }; ribbon.FileMenu.Visible = true; var tab = new RibbonTab { Text = "Home" }; ribbon.Tabs.Add(tab);
        ribbon.QuickAccessItems.Add(new RibbonQuickAccessReference { Source = new RibbonButtonItem { Text = "Save" } });
        form.Controls.Add(ribbon); form.Ribbon = ribbon; form.Show(); Application.DoEvents();
        foreach (var placement in Enum.GetValues<RibbonQuickAccessPosition>()) foreach (int width in new[] { 900, 300, 120 })
        {
            form.ClientSize = new(width, 650); ribbon.QuickAccessPosition = placement;
            var file = ribbon.Snapshot.Targets.FirstOrDefault(t => t.Kind == TargetKind.FileMenu);
            if (file is null) { Assert.True(width < 300); Assert.True(ribbon.ApplicationButtonBounds.IsEmpty); continue; }
            Assert.True(file.Circular); Assert.Same(ribbon.FileMenu, file.Element);
            Assert.Equal(file.Bounds, ribbon.ApplicationButtonBounds);
            if (width >= 300) Assert.True(file.Bounds.Top < ribbon.WindowCaptionHeight);
            else Assert.True(file.Bounds.Top >= ribbon.WindowCaptionHeight);
            Assert.All(form.Caption.Targets, target => Assert.False(target.Bounds.IntersectsWith(file.Bounds)));
            Assert.True(ribbon.Snapshot.TabViewport.Left >= file.Bounds.Right);
            if (ribbon.ShowQuickAccess && !ribbon.Snapshot.QuickAccessBounds.IsEmpty && ribbon.EffectiveQuickAccessPosition != RibbonQuickAccessPosition.BelowRibbon)
                Assert.True(ribbon.Snapshot.QuickAccessBounds.Left >= file.Bounds.Right);
            if (file.Bounds.Top < ribbon.WindowCaptionHeight) Assert.True(form.CaptionTextBounds(ribbon).Left >= file.Bounds.Right);
            var upper = new Point(file.Bounds.Left + file.Bounds.Width / 2, file.Bounds.Top + file.Bounds.Height / 4);
            Assert.Equal(1, ribbon.NativeHit(upper)); Assert.Equal(1, form.CaptionHitTest(upper));
            Assert.Equal(12, form.CaptionHitTest(new(width / 2, 0)));
            ribbon.FileMenu.Enabled = false; Assert.Equal(1, ribbon.NativeHit(upper)); ribbon.FileMenu.Enabled = true;
        }
    });

    [Fact]
    public void Changing_shape_or_theme_releases_capture_and_keeps_one_file_uia_identity() => FoundationTests.Sta(() =>
    {
        using var host = new RibbonForm { Bounds = new(-16000, -16000, 900, 650), ShowInTaskbar = false, StartPosition = FormStartPosition.Manual };
        using var ribbon = new Probe { Theme = OrbTheme }; ribbon.FileMenu.Visible = true; ribbon.Tabs.Add(new RibbonTab()); host.Controls.Add(ribbon); host.Ribbon = ribbon; host.Show(); Application.DoEvents();
        var file = ribbon.Snapshot.Targets.Single(t => t.Kind == TargetKind.FileMenu); var point = new Point(file.Bounds.Left + file.Bounds.Width / 2, file.Bounds.Top + file.Bounds.Height / 3);
        var node = ribbon.AutomationRoot.Node(ribbon.FileMenu); Assert.Single(ribbon.AutomationRoot.Children, c => ReferenceEquals(c.Element, ribbon.FileMenu));
        Assert.NotNull(node.GetPatternProvider(10005)); Assert.Equal("Alt, F", node.GetPropertyValue(30006));
        var screen = ribbon.PointToScreen(point); Assert.Same(node, ribbon.AutomationRoot.ElementProviderFromPoint(screen.X, screen.Y));
        var corner = ribbon.PointToScreen(file.Bounds.Location); Assert.NotSame(node, ribbon.AutomationRoot.ElementProviderFromPoint(corner.X, corner.Y));
        ribbon.Down(point); Assert.True(ribbon.Capture); ribbon.ApplicationButtonShape = RibbonApplicationButtonShape.Rectangular; Assert.False(ribbon.Capture);
        ribbon.ApplicationButtonShape = null; ribbon.Down(point); Assert.True(ribbon.Capture); ribbon.Theme = RibbonThemes.Office2019Light; Assert.False(ribbon.Capture);
        Assert.Equal(0, ribbon.AutomationRoot.Node(ribbon.FileMenu).ExpandCollapseState);
    });

    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Menu_native_and_layered_exclusions_share_screen_geometry_and_content_clearance(int dpi)
    {
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
        var orb = new Rectangle(S(106), S(118), S(50), S(50)); var caption = new Rectangle(S(100), S(100), S(900), S(36));
        var composition = new RibbonApplicationMenuComposition(orb, caption); var canvas = new Rectangle(S(106), S(126), S(500), S(400));
        using var excluded = composition.Exclusion(canvas); Assert.True(excluded.IsVisible(S(25), S(20))); Assert.True(excluded.IsVisible(S(150), S(5))); Assert.False(excluded.IsVisible(S(150), S(50)));
        Assert.True(composition.TopClearance(canvas.Top, S(18)) >= orb.Bottom - canvas.Top);
        using var native = new Region(new Rectangle(Point.Empty, canvas.Size)); native.Exclude(excluded);
        using var image = new Bitmap(canvas.Width, canvas.Height); using var g = Graphics.FromImage(image); g.Clear(Color.Transparent); g.ExcludeClip(excluded); g.FillRectangle(Brushes.Blue, new Rectangle(Point.Empty, image.Size));
        Assert.Equal(0, image.GetPixel(S(25), S(20)).A); Assert.Equal(255, image.GetPixel(S(150), S(50)).A);
        Assert.False(native.IsVisible(S(25), S(20))); Assert.True(native.IsVisible(S(150), S(50)));
        var work = new Rectangle(0, 0, S(1000), S(700)); var origin = new Point(S(106), S(126));
        foreach (var size in new[] { new Size(S(400), S(300)), new Size(S(500), S(400)), new Size(S(1400), S(900)) })
        { var bounds = RibbonPopupManager.CalculateOrbMenuBounds(origin, work, size); Assert.Equal(origin.Y, bounds.Top); Assert.True(work.Contains(bounds)); }
        Assert.False(composition.OpensAbove(origin, work, S(18), S(96), out _));
        var bottomComposition = new RibbonApplicationMenuComposition(new(S(106), S(640), S(50), S(50)), new(S(100), S(620), S(900), S(40)));
        Assert.True(bottomComposition.OpensAbove(new(S(106), S(650)), work, S(18), S(96), out int bottom));
        var above = RibbonPopupManager.CalculateOrbMenuBounds(new(S(106), bottom), work, new(S(500), S(400)), true);
        Assert.True(work.Contains(above)); Assert.True(above.Bottom <= bottomComposition.Caption.Top && above.Bottom <= bottomComposition.Orb.Top);
    }

    [Fact]
    public void Above_caption_fallback_pins_the_measured_menu_height_across_large_and_small_panes() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { Theme = OrbTheme };
        ribbon.FileMenu.Visible = true; ribbon.FileMenu.Items.Add(new RibbonDropDownItem { Text = "Open" });
        var initial = RibbonApplicationMenuLayout.Build(ribbon, ribbon.ApplicationMenu.DefaultPane, new(800, 600), 18);
        using var pane = new RibbonDropDownItem();
        for (int i = 0; i < 30; i++) pane.Items.Add(new RibbonButtonItem { Text = "Command " + i });
        foreach (var active in new[] { pane, ribbon.ApplicationMenu.DefaultPane })
        {
            var next = RibbonApplicationMenuLayout.Build(ribbon, active, new(800, 600), 18, initial.Size.Height);
            Assert.Equal(initial.Size, next.Size); Assert.True(new Rectangle(Point.Empty, next.Size).Contains(next.Frame));
        }
    });

    [Fact]
    public void Menu_pane_switch_keyboard_uia_and_close_reuse_the_existing_native_owner() => FoundationTests.Sta(() =>
    {
        using var form = new RibbonForm { Bounds = new(-16000, -16000, 1000, 700), StartPosition = FormStartPosition.Manual, ShowInTaskbar = false };
        using var ribbon = new Probe { Theme = OrbTheme, AnimationLevel = RibbonAnimationLevel.None }; ribbon.FileMenu.Visible = true; ribbon.Tabs.Add(new RibbonTab()); form.Controls.Add(ribbon); form.Ribbon = ribbon;
        int primary = 0; var pane = new RibbonSplitButtonItem { Text = "Save as" }; pane.Click += (_, _) => primary++;
        using var content = new UserControl { Size = new(320, 240) }; var field = new TextBox { Text = "Retained native content" }; content.Controls.Add(field); pane.Content = content; ribbon.FileMenu.Items.Add(pane);
        ribbon.ApplicationMenu.DefaultPane.Items.Add(new RibbonButtonItem { Text = "Recent file" }); form.Show(); Application.DoEvents();
        var node = ribbon.AutomationRoot.Node(ribbon.FileMenu); node.Expand(); var surface = Assert.IsType<RibbonPopupSurface>(ribbon.Popups.Top); var window = Assert.IsType<RibbonPopupWindow>(surface.Parent);
        var composition = window.ApplicationComposition; Assert.False(composition.IsEmpty); Assert.Null(window.OpeningMotion); Assert.True(ribbon.FileMenu.IsDropDownOpen); Assert.Equal(1, node.ExpandCollapseState);
        var origin = window.Location; Assert.True(ribbon.Popups.SelectApplicationPane(pane)); nint handle = field.Handle; var parent = field.Parent; field.Select(2, 4);
        Assert.Equal(0, primary); Assert.Equal(origin, window.Location); Assert.Same(content, parent); Assert.Equal(handle, field.Handle); Assert.Equal(2, field.SelectionStart); Assert.Equal(4, field.SelectionLength);
        surface.Focus();
        var key = typeof(RibbonPopupSurface).GetMethod("ProcessCmdKey", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        Assert.True((bool)key.Invoke(surface, [Message.Create(surface.Handle, 0x100, (nint)Keys.Escape, 0), Keys.Escape])!);
        Assert.False(ribbon.Popups.HasOpen); Assert.False(ribbon.FileMenu.IsDropDownOpen); Assert.Equal(0, node.ExpandCollapseState); Assert.False(content.IsDisposed);
        node.Expand(); Assert.True(ribbon.Popups.HasOpen); node.Collapse(); Assert.False(ribbon.Popups.HasOpen); Assert.Equal(0, primary);
    });

    [Theory] [InlineData(RibbonFileMenuMode.CommandMenu)] [InlineData(RibbonFileMenuMode.Backstage)]
    public void Upper_orb_click_and_uia_collapse_use_the_retained_route_exactly_once(RibbonFileMenuMode route) => FoundationTests.Sta(() =>
    {
        using var form = new RibbonForm { Bounds = new(-16000, -16000, 1000, 700), StartPosition = FormStartPosition.Manual, ShowInTaskbar = false };
        using var ribbon = new Probe { Theme = OrbTheme, FileMenuMode = route, AnimationLevel = RibbonAnimationLevel.None };
        ribbon.FileMenu.Visible = true; ribbon.Tabs.Add(new RibbonTab()); ribbon.ApplicationMenu.DefaultPane.Items.Add(new RibbonButtonItem { Text = "Recent" });
        ribbon.Backstage.Items.Add(new RibbonBackstagePageItem { Text = "Info", Content = new Panel() });
        form.Controls.Add(ribbon); form.Ribbon = ribbon; form.Show(); Application.DoEvents();
        int openings = 0; ribbon.FileMenu.DropDownOpening += (_, _) => openings++;
        var box = ribbon.ApplicationButtonBounds; var point = new Point(box.Left + box.Width / 2, box.Top + box.Height / 4);
        ribbon.Down(point); ribbon.Up(point); Assert.True(ribbon.FileMenu.IsDropDownOpen);
        var node = ribbon.AutomationRoot.Node(ribbon.FileMenu); Assert.Equal(1, node.ExpandCollapseState); Assert.Equal(route == RibbonFileMenuMode.Backstage, ribbon.IsBackstageOpen);
        Assert.Equal(1, openings); node.Expand(); Assert.Equal(1, openings);
        node.Collapse(); Assert.False(ribbon.FileMenu.IsDropDownOpen); Assert.Equal(0, node.ExpandCollapseState); Assert.Equal(route, ribbon.FileMenuMode);
    });

    [Fact]
    public void Hidden_native_popup_and_child_regions_exclude_the_real_orb_and_caption() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { Theme = OrbTheme with { PopupCornerRadius = 0, PopupShadowEnabled = false } };
        using var popup = new RibbonPopupWindow(ribbon, () => { }) { AutoSize = false, Size = new(320, 240), Location = new(100, 100) };
        using var child = new Panel { Size = new(316, 236), BackColor = Color.Blue };
        popup.Items.Add(new ToolStripControlHost(child) { AutoSize = false, Size = child.Size, Margin = Padding.Empty, Padding = Padding.Empty });
        popup.ApplicationComposition = new(new(110, 80, 60, 60), new(100, 70, 900, 36));
        popup.PerformLayout(); using var image = new Bitmap(popup.Width, popup.Height); popup.DrawToBitmap(image, popup.ClientRectangle);
        Assert.NotNull(popup.Region); Assert.False(popup.Region.IsVisible(popup.PointToClient(new(140, 115))));
        Assert.False(popup.Region.IsVisible(popup.PointToClient(new(250, 102)))); Assert.True(popup.Region.IsVisible(new Point(150, 100)));
        Assert.NotNull(child.Region); Assert.False(child.Region.IsVisible(child.PointToClient(new(140, 115)))); Assert.Same(popup, child.Parent);
    });

    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Shadow_clearance_and_zero_rims_are_independent_of_sphere_and_glyph_geometry(int dpi) => FoundationTests.Sta(() =>
    {
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
        var settings = new RibbonApplicationButtonAppearanceSettings { Background = new(Color.White), GlyphSize = 0, BorderWidth = 0, InnerBorderWidth = 0, ShadowColor = Color.FromArgb(128, Color.Black), ShadowBlur = 3, RightGap = 0 };
        var bounds = RibbonApplicationButtonMetrics.Measure(settings, RibbonDensity.Compact, dpi, S(36), S(32), S(200));
        int pad = RibbonApplicationButtonMetrics.ShadowPadding(settings, dpi); Assert.Equal(S(3), pad); Assert.True(bounds.Left >= pad && bounds.Top >= pad && bounds.Bottom + pad <= S(68));
        using var painter = new RibbonRenderer(); using var menu = new RibbonDropDownItem(); using var image = new Bitmap(S(230), S(90)); using var g = Graphics.FromImage(image); g.Clear(Color.Transparent);
        var context = new RibbonPaintContext(g, bounds, OrbTheme with { ApplicationButtonAppearance = settings }, Control.DefaultFont, dpi);
        painter.DrawFileButton(context, menu); int center = bounds.Left + bounds.Width / 2; Assert.True(image.GetPixel(center, bounds.Bottom).A > 0);
        Assert.Equal(Color.White.ToArgb(), image.GetPixel(center, bounds.Top + bounds.Height / 2).ToArgb());
        g.Clear(Color.Transparent); painter.DrawFileButton(context with { Theme = context.Theme with { ApplicationButtonAppearance = settings with { ShadowBlur = 0 } } }, menu);
        Assert.Equal(0, image.GetPixel(center, bounds.Bottom).A);
    });

    [Fact]
    public void Body_only_repaint_does_not_rerender_the_orb_or_borrowed_glyph() => FoundationTests.Sta(() =>
    {
        using var form = new RibbonForm { Bounds = new(-16000, -16000, 1000, 700), StartPosition = FormStartPosition.Manual, ShowInTaskbar = false };
        using var painter = new GlyphPainter(); using var ribbon = new Probe { Renderer = painter, Theme = OrbTheme with { ApplicationButtonAppearance = OrbTheme.ApplicationButtonAppearance! with { GlyphSize = 22 } } };
        ribbon.FileMenu.Visible = true; ribbon.Tabs.Add(new RibbonTab()); form.Controls.Add(ribbon); form.Ribbon = ribbon; form.Show(); Application.DoEvents();
        using var image = new Bitmap(ribbon.Width, ribbon.Height); using var g = Graphics.FromImage(image);
        painter.Calls = 0; ribbon.PaintRegion(g, ribbon.Snapshot.BodyViewport); Assert.Equal(0, painter.Calls);
        ribbon.PaintRegion(g, ribbon.ApplicationButtonBounds); Assert.Equal(1, painter.Calls);
    });

    private sealed class Probe : RibbonControl
    {
        internal void Down(Point point) => OnMouseDown(new(MouseButtons.Left, 1, point.X, point.Y, 0));
        internal void Up(Point point) => OnMouseUp(new(MouseButtons.Left, 1, point.X, point.Y, 0));
        internal void PaintRegion(Graphics graphics, Rectangle clip) { var saved = graphics.Save(); try { graphics.SetClip(clip); OnPaint(new(graphics, clip)); } finally { graphics.Restore(saved); } }
        internal int NativeHit(Point point) { var p = PointToScreen(point); var m = Message.Create(Handle, 0x84, 0, (nint)((p.X & 0xffff) | (p.Y << 16))); WndProc(ref m); return (int)m.Result; }
    }
    private sealed class Glyph : IRibbonIcon, IDisposable
    {
        internal int Calls; internal bool Enabled, Disposed; internal Rectangle Bounds;
        public void Draw(Graphics graphics, Rectangle bounds, RibbonIconContext context) { Calls++; Enabled = context.Enabled; Bounds = bounds; graphics.FillRectangle(Brushes.Red, bounds); }
        public void Dispose() => Disposed = true;
    }
    private sealed class GlyphPainter : RibbonOffice2024Renderer
    {
        internal int Calls;
        internal int FocusCalls;
        public override void DrawApplicationButtonGlyph(RibbonPaintContext context, bool enabled) { Calls++; base.DrawApplicationButtonGlyph(context, enabled); }
        public override void DrawFocus(RibbonPaintContext context) { FocusCalls++; base.DrawFocus(context); }
    }
}
