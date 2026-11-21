using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class Office2007BackstageTests
{
    [Theory]
    [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void One_workspace_uses_equal_row_top_and_side_padding_and_retains_footer_and_value_snapshots(int dpi) => FoundationTests.Sta(() =>
    {
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
        foreach (var density in new[] { RibbonDensity.Compact, RibbonDensity.Touch })
        {
            using var ribbon = new RibbonControl { Theme = RibbonThemes.Office2007Blue, Density = density };
            ribbon.Backstage.Items.AddRange(new RibbonBackstagePageItem { Text = "Info" }, new RibbonMenuSeparatorItem(), new RibbonButtonItem { Text = "Save" });
            ribbon.Backstage.FooterItems.AddRange(new RibbonMenuSeparatorItem(), new RibbonButtonItem { Text = "Options" });
            var size = new Size(S(1000), S(700));
            var family = RibbonPresentationResolver.Backstage(ribbon.Theme);
            var input = RibbonBackstageLayoutMeasurement.Measure(ribbon.Font, dpi, ribbon.Backstage, null, 0, family.ResolveMetrics(ribbon.Theme, density), 0);
            var layout = family.Layout.Build(size, input);
            Assert.Equal(S(8), layout.Workspace.Left); Assert.Equal(S(8), size.Width - layout.Workspace.Right);
            Assert.Equal(S(8), layout.Workspace.Top - layout.Header.Bottom); Assert.Equal(S(8), size.Height - layout.Workspace.Bottom);
            Assert.Equal(S(4), layout.Inset); Assert.Equal(layout.Inset, layout.Navigation.Top - layout.NavigationCanvas.Top);
            var rows = layout.Arrange(ribbon.Backstage);
            Assert.All(rows, slot => { Assert.Equal(layout.Inset, slot.Bounds.Left - slot.Clip.Left); Assert.Equal(layout.Inset, slot.Clip.Right - slot.Bounds.Right); });
            Assert.Equal(rows[0].Bounds.Left, rows[1].Bounds.Left); Assert.Equal(rows[0].Bounds.Right, rows[1].Bounds.Right);
            Assert.Equal(layout.Navigation.Bottom, layout.Footer.Top); Assert.Equal(layout.Inset, layout.Footer.Bottom - rows[^1].Bounds.Bottom);
            Assert.Equal(rows[^1].Bounds, layout.Arrange(ribbon.Backstage, S(30))[^1].Bounds);
            Assert.True(layout.PageContentBounds().Left > layout.Page.Left);
            var shortLayout = family.Layout.Build(new(S(1000), S(140)), input);
            Assert.True(shortLayout.FooterInline);
            foreach (var tiny in new[] { Size.Empty, new Size(1, 1), new Size(9, 7), new Size(S(160), S(60)) })
            {
                var small = family.Layout.Build(tiny, input);
                foreach (var rectangle in new[] { small.Header, small.Workspace, small.NavigationCanvas, small.Page, small.Navigation })
                { Assert.True(rectangle.Width >= 0 && rectangle.Height >= 0); Assert.InRange(rectangle.Right, 0, tiny.Width); Assert.InRange(rectangle.Bottom, 0, tiny.Height); }
            }
            ribbon.Dispose(); Assert.Equal(layout, family.Layout.Build(size, input));
        }
    });

    [Theory]
    [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Shared_frame_divider_gradient_crease_and_separator_have_exact_clipped_pixels(int dpi) => FoundationTests.Sta(() =>
    {
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
        using var ribbon = new RibbonControl { Theme = RibbonThemes.Office2007Blue };
        var separator = new RibbonMenuSeparatorItem(); ribbon.Backstage.Items.AddRange(new RibbonBackstagePageItem { Text = "Info" }, separator);
        var layout = RibbonBackstageLayout.Build(new(S(800), S(600)), ribbon.Font, dpi, ribbon.Backstage);
        foreach (RibbonRenderer painter in new RibbonRenderer[] { new(), new RibbonOffice2024Renderer() })
        using (painter)
        using (var image = new Bitmap(S(800), S(600)))
        using (var g = Graphics.FromImage(image))
        {
            var c = new RibbonPaintContext(g, new(Point.Empty, image.Size), ribbon.Theme, ribbon.Font, dpi);
            painter.DrawBackstageBackground(c, layout.Page.Left);
            var rim = ribbon.Theme.BackstageAppearance!.Workspace!.BorderColor;
            Assert.Equal(rim.ToArgb(), image.GetPixel(layout.Workspace.Left + layout.Workspace.Width / 2, layout.Workspace.Top).ToArgb());
            Assert.Equal(rim.ToArgb(), image.GetPixel(layout.Page.Left - 1, layout.NavigationCanvas.Top + S(20)).ToArgb());
            Assert.Equal(Color.White.ToArgb(), image.GetPixel(layout.Page.Left + S(12), layout.Page.Bottom - S(12)).ToArgb());
            int x = layout.NavigationCanvas.Left + S(8), y = layout.NavigationCanvas.Top + S(20);
            Assert.Equal(RibbonFillColors.At(ribbon.Theme.BackstageNavigationFill, (y - layout.NavigationCanvas.Top) / (double)(layout.NavigationCanvas.Height - 1)).ToArgb(), image.GetPixel(x, y).ToArgb());
            var header = RibbonBackstage2007Renderer.HeaderFill(ribbon.Theme);
            int crease = (int)Math.Ceiling((layout.Header.Height - 1) * .45);
            Assert.Equal(RibbonFillColors.At(header, crease / (double)(layout.Header.Height - 1)).ToArgb(), image.GetPixel(S(400), crease).ToArgb());
            var slot = layout.Arrange(ribbon.Backstage).Single(s => s.Item == separator);
            painter.DrawBackstageEntry(c with { Bounds = slot.Bounds, BackgroundBounds = layout.NavigationCanvas }, separator, false);
            Assert.Equal(ribbon.Theme.Border.ToArgb(), image.GetPixel(slot.Bounds.Left, slot.Bounds.Top + (slot.Bounds.Height - 1) / 2).ToArgb());
            Assert.Equal(ribbon.Theme.Border.ToArgb(), image.GetPixel(slot.Bounds.Right - 1, slot.Bounds.Top + (slot.Bounds.Height - 1) / 2).ToArgb());
            using var original = (Bitmap)image.Clone();
            var clip = new Rectangle(S(70), S(120), S(23), S(29)); g.SetClip(clip); using (var brush = new SolidBrush(Color.Fuchsia)) g.FillRectangle(brush, clip);
            painter.DrawBackstageBackground(c, layout.Page.Left); g.ResetClip();
            Assert.Equal(original.GetPixel(clip.Left - 1, clip.Top), image.GetPixel(clip.Left - 1, clip.Top));
            Assert.Equal(original.GetPixel(clip.Left, clip.Top), image.GetPixel(clip.Left, clip.Top));
        }
    });

    [Theory]
    [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Rounded_frame_alpha_is_symmetric_and_authored_strokes_and_null_resets_are_independent(int dpi) => FoundationTests.Sta(() =>
    {
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
        var t = RibbonThemes.Office2007Blue;
        var a = t.BackstageAppearance!;
        var w = a.Workspace! with { HeaderHeight = 0, BorderColor = Color.FromArgb(180, Color.Magenta), InnerBorderWidth = 0, CornerRadius = 8 };
        t = t with { BackstageNavigationBackgroundFill = new(Color.Transparent), Surface = Color.Transparent, BackstageAppearance = a with { Workspace = w } };
        var bounds = new Rectangle(0, 0, S(400), S(300));
        var metrics = RibbonPresentationResolver.Backstage(t).ResolveMetrics(t, RibbonDensity.Compact);
        var frame = RibbonBackstageWorkspaceGeometry.Measure(bounds, dpi, metrics).Frame;
        using var painter = new RibbonOffice2024Renderer(); using var image = new Bitmap(bounds.Width, bounds.Height); using var g = Graphics.FromImage(image);
        painter.DrawBackstageBackground(new(g, bounds, t, Control.DefaultFont, dpi), 0);
        for (int y = 0; y < S(10); y++) for (int x = 0; x < S(10); x++)
        {
            byte alpha = image.GetPixel(frame.Left + x, frame.Top + y).A;
            Assert.Equal(alpha, image.GetPixel(frame.Right - 1 - x, frame.Top + y).A);
            Assert.Equal(alpha, image.GetPixel(frame.Left + x, frame.Bottom - 1 - y).A);
            Assert.Equal(alpha, image.GetPixel(frame.Right - 1 - x, frame.Bottom - 1 - y).A);
        }
        Assert.Equal(180, image.GetPixel(frame.Left + frame.Width / 2, frame.Top).A);
        Assert.Equal(0, image.GetPixel(frame.Left + frame.Width / 2, frame.Top + 1).A);
        var wider = t with { BackstageAppearance = a with { Workspace = w with { BorderWidth = 3 } } };
        g.Clear(Color.Transparent); painter.DrawBackstageBackground(new(g, bounds, wider, Control.DefaultFont, dpi), 0);
        Assert.Equal(180, image.GetPixel(frame.Left + frame.Width / 2, frame.Top + S(3) - 1).A);
        Assert.Null((a with { Workspace = null }).Workspace); Assert.NotNull((w with { HeaderBackground = null }).NavigationStateChrome);
    });

    [Fact]
    public void Workspace_validation_is_atomic_and_ignores_other_families_without_mutating_routes() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl(); var prior = ribbon.Theme; var identity = ribbon.BuiltInTheme;
        foreach (var bad in new[] { new RibbonBackstageWorkspaceSettings { HeaderHeight = -1 }, new() { ReturnWidth = 1025 }, new() { FrameInset = -1 },
            new() { BorderWidth = 33 }, new() { InnerBorderWidth = -1 }, new() { CornerRadius = 65 }, new() { SeparatorInset = 1025 }, new() { NavigationStateChrome = new() { InnerBorderWidth = 33 } } })
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ribbon.Theme = prior with { BackstageAppearance = new() { Workspace = bad } });
            Assert.Same(prior, ribbon.Theme); Assert.Equal(identity, ribbon.BuiltInTheme);
        }
        foreach (var family in new[] { RibbonThemes.Office2024Light, RibbonThemes.Office2019Light, RibbonThemes.Office2013Light, RibbonThemes.Office2010Blue })
        {
            ribbon.Theme = family; var before = RibbonBackstageLayout.Build(new(900, 600), ribbon.Font, 96, ribbon.Backstage);
            ribbon.Theme = family with { BackstageAppearance = (family.BackstageAppearance ?? new()) with { Workspace = new() { FrameInset = 100, HeaderHeight = 90 } } };
            Assert.Equal(before, RibbonBackstageLayout.Build(new(900, 600), ribbon.Font, 96, ribbon.Backstage));
        }
        ribbon.FileMenuMode = RibbonFileMenuMode.Backstage; ribbon.ApplicationButtonShape = RibbonApplicationButtonShape.Rectangular;
        ribbon.Theme = RibbonThemes.Office2007Blue;
        Assert.Equal(RibbonFileMenuMode.Backstage, ribbon.FileMenuMode); Assert.Equal(RibbonApplicationButtonShape.Rectangular, ribbon.EffectiveApplicationButtonShape);
        Assert.False(RibbonPresentationResolver.Backstage(ribbon.Theme).Caption.Resolve(ribbon.Theme).ExtendsIntoTitleBar);
    });

    [Theory] [InlineData(RibbonWindowBackdrop.None)] [InlineData(RibbonWindowBackdrop.Mica)]
    public void Opaque_workspace_retains_native_pages_host_material_preference_and_both_return_paths(RibbonWindowBackdrop backdrop) => FoundationTests.Sta(() =>
    {
        using var host = new RibbonForm { ClientSize = new(1000, 700), Backdrop = backdrop };
        using var ribbon = new RibbonControl { Theme = RibbonThemes.Office2007Blue, AnimationLevel = RibbonAnimationLevel.None, FileMenuMode = RibbonFileMenuMode.Backstage };
        ribbon.Theme = ribbon.Theme with { BackstageAppearance = ribbon.Theme.BackstageAppearance! with { BackButtonVisible = true } };
        ribbon.FileMenu.Visible = true; ribbon.Tabs.Add(new RibbonTab { Text = "Home" });
        using var page = new Panel { BackColor = Color.White }; using var field = new TextBox { Text = "Retained", Bounds = new(20, 40, 200, 30) }; page.Controls.Add(field);
        ribbon.Backstage.Items.Add(new RibbonBackstagePageItem { Text = "Info", Content = page });
        ribbon.Backstage.NavigationBackgroundOpacity = .25f;
        host.Controls.Add(ribbon); host.Ribbon = ribbon; host.Show(); Application.DoEvents();
        ribbon.IsBackstageOpen = true; Application.DoEvents();
        var surface = ribbon.BackstageSurface!; var pageHandle = page.Handle; var parent = page.Parent;
        Assert.False(RibbonDrawingHold.HasOpacity(surface.Handle), "An opaque, nonanimated workspace must not borrow a whole-surface material layer.");
        Assert.False(surface.TransparentNavigation); Assert.Equal(backdrop, host.Backdrop); Assert.Equal(.25f, ribbon.Backstage.NavigationBackgroundOpacity);
        var layout = RibbonBackstageLayout.Build(surface.ClientSize, ribbon.Font, ribbon.DeviceDpi, ribbon.Backstage);
        var file = ribbon.Snapshot.Targets.Single(t => t.Kind == TargetKind.FileMenu);
        Assert.True(ribbon.IsExposedBackstageTarget(file));
        var back = surface.Controls.OfType<Button>().Single();
        Assert.Equal("Back to document", back.AccessibleName); Assert.Equal(layout.Back, back.Bounds);
        bool reject = true; field.Validating += (_, e) => e.Cancel = reject; field.Focus(); Application.DoEvents();
        back.PerformClick(); Assert.True(ribbon.IsBackstageOpen); reject = false;
        back.PerformClick(); Assert.False(ribbon.IsBackstageOpen);
        ribbon.IsBackstageOpen = true; Application.DoEvents();
        Assert.Equal(pageHandle, page.Handle); Assert.Same(parent, page.Parent); Assert.Equal("Retained", field.Text);
        ribbon.Theme = RibbonThemes.Office2010Blue; ribbon.Theme = RibbonThemes.Office2007Blue;
        Assert.True(ribbon.IsBackstageOpen); Assert.Equal(pageHandle, page.Handle); Assert.Same(parent, page.Parent);
        ribbon.Theme = ribbon.Theme with { BackstageAppearance = ribbon.Theme.BackstageAppearance! with { BackButtonVisible = false } };
        Assert.False(back.Visible);
        var orb = surface.RectangleToClient(ribbon.RectangleToScreen(ribbon.ApplicationButtonBounds));
        var point = new Point(orb.Left + orb.Width / 2, Math.Min(surface.Height - 1, orb.Bottom - 4));
        nint packed = (nint)((point.Y << 16) | (point.X & 65535));
        SendMessage(surface.Handle, 0x201, 1, packed); SendMessage(surface.Handle, 0x202, 0, packed);
        Assert.False(ribbon.IsBackstageOpen);
    });

    [Fact]
    public void Header_keeps_existing_back_file_glyph_and_text_hooks_and_exact_caller_clipping() => FoundationTests.Sta(() =>
    {
        var theme = RibbonThemes.Office2007Blue;
        theme = theme with { BackstageAppearance = theme.BackstageAppearance! with { BackButtonVisible = true, Workspace = theme.BackstageAppearance.Workspace! with { ReturnText = "Return to editor" } } };
        using var painter = new HeaderProbe(); using var menu = new RibbonDropDownItem { Text = "Document" };
        using var image = new Bitmap(800, 100); using var g = Graphics.FromImage(image); g.Clear(Color.Fuchsia);
        var target = new Rectangle(5, 7, 780, 70); var clip = new Rectangle(20, 10, 500, 50); g.SetClip(clip);
        painter.DrawBackstageHeader(new(g, target, theme, Control.DefaultFont, 120) { BackgroundBounds = target }, menu, new(12, -8, 58, 58));
        Assert.Equal(1, painter.BackCalls); Assert.Equal(1, painter.FileCalls); Assert.Equal(1, painter.GlyphCalls);
        Assert.Contains("Return to editor", painter.Labels); Assert.Contains("Document", painter.Labels);
        painter.DrawBackstageHeader(new(g, target, theme with { BackstageAppearance = theme.BackstageAppearance! with { BackButtonVisible = false } }, Control.DefaultFont, 120)
            { BackgroundBounds = target }, menu, new(12, -8, 58, 58));
        Assert.Equal(1, painter.BackCalls); Assert.Equal(2, painter.FileCalls);
        Assert.Equal(Color.Fuchsia.ToArgb(), image.GetPixel(clip.Left - 1, clip.Top).ToArgb());
        Assert.Equal(Color.Fuchsia.ToArgb(), image.GetPixel(clip.Right, clip.Top).ToArgb());
        Assert.Equal(clip, Rectangle.Round(g.ClipBounds));
    });

    private sealed class HeaderProbe : RibbonOffice2024Renderer
    {
        internal int BackCalls, FileCalls, GlyphCalls; internal List<string> Labels = [];
        public override void DrawBackstageEntry(RibbonPaintContext context, RibbonItem? item, bool selected) { if (item is null) BackCalls++; base.DrawBackstageEntry(context, item, selected); }
        public override void DrawFileButton(RibbonPaintContext context, RibbonDropDownItem menu) { FileCalls++; base.DrawFileButton(context, menu); }
        public override void DrawApplicationButtonGlyph(RibbonPaintContext context, bool enabled) { GlyphCalls++; base.DrawApplicationButtonGlyph(context, enabled); }
        public override void DrawText(RibbonPaintContext context, string text, Color color, TextFormatFlags flags) { Labels.Add(text); base.DrawText(context, text, color, flags); }
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern nint SendMessage(nint window, uint message, nint wParam, nint lParam);

    [Fact]
    public void Repeated_header_paint_keeps_authored_alpha_stops_direction_and_transparent_reset_exact() => FoundationTests.Sta(() =>
    {
        var t = RibbonThemes.Office2007Blue; var a = t.BackstageAppearance!;
        var fill = new RibbonFill(Color.FromArgb(128, Color.Red), Color.FromArgb(96, Color.Blue))
        { Direction = RibbonGradientDirection.Horizontal, IntermediateStops = RibbonGradientStops.WithHardTransitions(new[] { new RibbonGradientStop(.45, Color.FromArgb(64, Color.Green)), new RibbonGradientStop(.45, Color.FromArgb(160, Color.Gold)) }) };
        t = t with { BackstageAppearance = a with { Workspace = a.Workspace! with { HeaderBackground = fill, ReturnText = "" } } };
        using var painter = new RibbonRenderer(); using var menu = new RibbonDropDownItem();
        using var image = new Bitmap(800, 60); using var g = Graphics.FromImage(image);
        var c = new RibbonPaintContext(g, new(Point.Empty, image.Size), t, Control.DefaultFont, 96);
        painter.DrawBackstageHeader(c, menu, Rectangle.Empty); int pixel = image.GetPixel(400, 40).ToArgb();
        painter.DrawBackstageHeader(c, menu, Rectangle.Empty); Assert.Equal(pixel, image.GetPixel(400, 40).ToArgb());
        using var expected = new Bitmap(800, 60); using var eg = Graphics.FromImage(expected); eg.Clear(Color.FromArgb(255, t.BackstageNavigationColor));
        painter.PaintFill(c with { Graphics = eg }, fill); Assert.Equal(expected.GetPixel(400, 40).ToArgb(), pixel);
        painter.DrawBackstageHeader(c with { Theme = t with { BackstageAppearance = a with { Workspace = a.Workspace! with { HeaderBackground = new(Color.Transparent) } } } }, menu, Rectangle.Empty);
        Assert.Equal(Color.FromArgb(255, t.BackstageNavigationColor).ToArgb(), image.GetPixel(400, 40).ToArgb());
    });

    [Theory]
    [InlineData(RibbonWindowBackdrop.None, RibbonAnimationLevel.Subtle)]
    [InlineData(RibbonWindowBackdrop.None, RibbonAnimationLevel.Expressive)]
    [InlineData(RibbonWindowBackdrop.Mica, RibbonAnimationLevel.Subtle)]
    [InlineData(RibbonWindowBackdrop.Mica, RibbonAnimationLevel.Expressive)]
    public void Default_orb_only_workspace_seeds_the_unmoved_whole_surface_before_animation(RibbonWindowBackdrop backdrop, RibbonAnimationLevel level) => FoundationTests.Sta(() =>
    {
        using var host = new RibbonForm { StartPosition = FormStartPosition.Manual, Bounds = new(-16000, -16000, 1000, 700), ShowInTaskbar = false, Backdrop = backdrop };
        using var ribbon = new RibbonControl { Theme = RibbonThemes.Office2007Blue, FileMenuMode = RibbonFileMenuMode.Backstage, AnimationLevel = level };
        using var painter = new SeedProbe(ribbon); ribbon.Renderer = painter;
        ribbon.FileMenu.Visible = true; ribbon.Tabs.Add(new RibbonTab { Text = "Home" });
        using var page = new Panel(); using var field = new TextBox { Text = "Retained", Bounds = new(20, 20, 200, 30) }; page.Controls.Add(field);
        ribbon.Backstage.Items.Add(new RibbonBackstagePageItem { Text = "Info", Content = page });
        host.Controls.Add(ribbon); host.Ribbon = ribbon; host.Show(); Application.DoEvents();
        ribbon.Motion.SystemPolicyOverride = true; painter.Armed = true; ribbon.IsBackstageOpen = true;
        var surface = ribbon.BackstageSurface!; var motion = ribbon.BackstageOpeningMotion!;
        Assert.NotNull(motion); Assert.True(ribbon.IsAnimating);
        Assert.False(surface.Controls.OfType<Button>().Single().Visible);
        Assert.True(RibbonDrawingHold.HasOpacity(surface.Handle)); Assert.False(RibbonDrawingHold.HasOpacity(surface.PageArea.Handle));
        Assert.NotEmpty(painter.Frames);
        Assert.Equal(0, painter.Frames[0].Left); Assert.Equal(1, painter.Frames[0].Alpha);
        var handle = field.Handle; var parent = page.Parent;
        Assert.NotEmpty(painter.Orbs); var initialOrb = painter.Orbs[0];
        foreach (double progress in new[] { .15, .5, 1d })
        {
            motion.SetProgress(progress); surface.RepaintBackdrop();
            Assert.Equal(initialOrb, painter.Orbs[^1]);
        }
        ribbon.SettleMotion();
        Assert.Equal(0, surface.Left); Assert.Equal(255, surface.MotionAlpha); Assert.Equal(handle, field.Handle); Assert.Same(parent, page.Parent);
        ribbon.IsBackstageOpen = false; ribbon.SettleMotion(); Assert.False(ribbon.IsBackstageOpen);
    });

    private sealed class SeedProbe(RibbonControl ribbon) : RibbonOffice2024Renderer
    {
        internal bool Armed; internal List<(int Left, byte Alpha)> Frames = []; internal List<Rectangle> Orbs = [];
        public override void DrawBackstageBackground(RibbonPaintContext c, int navigationWidth)
        {
            if (Armed && ribbon.BackstageSurface is { Visible: true } surface) Frames.Add((surface.Left, surface.MotionAlpha));
            base.DrawBackstageBackground(c, navigationWidth);
        }
        public override void DrawBackstageHeader(RibbonPaintContext c, RibbonDropDownItem menu, Rectangle orb)
        { if (Armed) Orbs.Add(orb); base.DrawBackstageHeader(c, menu, orb); }
    }
}
