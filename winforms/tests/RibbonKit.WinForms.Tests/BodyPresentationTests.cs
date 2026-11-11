using System.Drawing.Imaging;
using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class BodyPresentationTests
{
    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Body_message_and_joined_qat_widths_ink_and_connected_edges_use_authored_values(int dpi) => FoundationTests.Sta(() =>
    {
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
        using var painter = new RibbonOffice2024Renderer(); using var image = new Bitmap(S(260), S(180)); using var g = Graphics.FromImage(image);
        var appearance = new RibbonBodyAppearanceSettings { BorderWidth = 3, MessageBorderWidth = 3,
            QuickAccessSeparatorWidth = 3, QuickAccessSeparatorInset = 7, QuickAccessSeparatorColor = Color.Magenta };
        var theme = RibbonThemes.Office2024Light with { BodyAppearance = appearance, BodyCornerRadius = 0, BodyShadowColor = Color.Transparent, BodyBorder = Color.DarkRed,
            BodyBackground = new(Color.White), MessageBackground = new(Color.LightBlue), MessageBorder = Color.DarkBlue };
        var bounds = new Rectangle(S(10), S(10), S(220), S(100)); var context = new RibbonPaintContext(g, bounds, theme, Control.DefaultFont, dpi);
        foreach (bool bottomOnly in new[] { false, true })
        {
            g.Clear(Color.Beige); painter.DrawBody(context with { Theme = theme with { BodyBottomBorderOnly = bottomOnly } });
            Assert.Equal(Color.DarkRed.ToArgb(), image.GetPixel(bounds.Left + bounds.Width / 2, bounds.Bottom - S(3)).ToArgb());
            Assert.Equal((bottomOnly ? Color.White : Color.DarkRed).ToArgb(), image.GetPixel(bounds.Left, bounds.Top + bounds.Height / 2).ToArgb());
            painter.DrawBody(context with { Theme = theme with { BodyBottomBorderOnly = bottomOnly, BodyAppearance = appearance with { BorderWidth = 0 } } });
            Assert.Equal(Color.White.ToArgb(), image.GetPixel(bounds.Left, bounds.Bottom - 1).ToArgb());
        }
        foreach (bool sides in new[] { false, true })
        {
            g.Clear(Color.Beige); painter.DrawMessageBar(context with { Theme = theme with { MessageSideBordersVisible = sides } });
            Assert.Equal(Color.DarkBlue.ToArgb(), image.GetPixel(bounds.Left + bounds.Width / 2, bounds.Top + S(2)).ToArgb());
            Assert.Equal((sides ? Color.DarkBlue : Color.LightBlue).ToArgb(), image.GetPixel(bounds.Left, bounds.Top + bounds.Height / 2).ToArgb());
            painter.DrawMessageBar(context with { IsTopEdgeConnected = true, Theme = theme with { MessageSideBordersVisible = sides } });
            Assert.Equal(Color.LightBlue.ToArgb(), image.GetPixel(bounds.Left + bounds.Width / 2, bounds.Top).ToArgb());
        }
        g.Clear(Color.Beige); painter.DrawQuickAccessToolbar(context with { IsQuickAccessToolbarJoined = true }, RibbonQuickAccessPosition.BelowRibbon);
        Assert.Equal(new Rectangle(bounds.Left + S(7), bounds.Top, bounds.Width - 2 * S(7), S(3)), Ink(image, Color.Magenta));
        var alpha = Color.FromArgb(128, 220, 20, 120); g.Clear(Color.White);
        painter.DrawQuickAccessToolbar(context with { IsQuickAccessToolbarJoined = true, Theme = theme with { BodyAppearance = appearance with { QuickAccessSeparatorColor = alpha } } }, RibbonQuickAccessPosition.BelowRibbon);
        using var reference = new Bitmap(1, 1); using var expected = Graphics.FromImage(reference); expected.Clear(Color.White); using var brush = new SolidBrush(alpha); expected.FillRectangle(brush, 0, 0, 1, 1);
        Assert.Equal(reference.GetPixel(0, 0), image.GetPixel(bounds.Left + S(8), bounds.Top));
        g.Clear(Color.Beige); painter.DrawQuickAccessToolbar(context with { IsQuickAccessToolbarJoined = true, Theme = theme with { BodyAppearance = appearance with { QuickAccessSeparatorWidth = 0 } } }, RibbonQuickAccessPosition.BelowRibbon);
        Assert.True(Ink(image, Color.Magenta).IsEmpty);
    });

    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Group_frame_dividers_share_actual_renderer_hooks_and_keep_reduction_bounds(int dpi) => FoundationTests.Sta(() =>
    {
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
        using var painter = new Hooks(); using var group = new RibbonGroup { Text = "Editing" };
        var theme = RibbonThemes.Office2024Light with { BodyAppearance = new() { GroupSeparatorWidth = 3, GroupSeparatorColor = Color.Magenta } };
        using var image = new Bitmap(S(260), S(160)); using var graphics = Graphics.FromImage(image); graphics.Clear(Color.Beige);
        var frame = new Rectangle(S(10), S(10), S(220), S(110)); var context = new RibbonPaintContext(graphics, frame, theme, Control.DefaultFont, dpi) { GroupSeparatorBounds = frame };
        painter.DrawGroup(context, group, false);
        Assert.Equal(new Rectangle(frame.Right - S(3), frame.Top + S(12), S(3), frame.Height - 2 * S(12)), Ink(image, Color.Magenta));
        Assert.Single(painter.Separators);
        painter.ReplaceSeparator = true; graphics.Clear(Color.Beige); painter.DrawGroup(context, group, false);
        Assert.Equal(new Rectangle(frame.Right - S(3), frame.Top + S(12), S(3), frame.Height - 2 * S(12)), Ink(image, Color.Lime));
        Assert.True(Ink(image, Color.Magenta).IsEmpty); painter.ReplaceSeparator = false;
        painter.Separators.Clear(); graphics.Clear(Color.Beige);
        painter.DrawGroup(context with { Theme = theme with { BodyAppearance = theme.BodyAppearance with { GroupSeparatorWidth = 0 } } }, group, false);
        Assert.Empty(painter.Separators); Assert.True(Ink(image, Color.Magenta).IsEmpty);
        using var ribbon = new RibbonControl { Width = 900, Renderer = painter, LayoutMode = RibbonLayoutMode.Simplified, Theme = theme };
        var tab = new RibbonTab();
        foreach (string text in new[] { "First", "Second" })
        { var section = new RibbonGroup { Text = text }; section.Items.Add(new RibbonButtonItem { Text = text, SimplifiedPlacement = RibbonSimplifiedPlacement.Inline }); tab.Groups.Add(section); }
        ribbon.Tabs.Add(tab); var before = ribbon.Snapshot; Assert.Single(before.SimplifiedDividers);
        using var native = new Bitmap(ribbon.Width, ribbon.Height); painter.Separators.Clear(); ribbon.DrawToBitmap(native, ribbon.ClientRectangle);
        var divider = before.SimplifiedDividers[0]; int actualWidth = RibbonLayoutEngine.Scale(3, ribbon.DeviceDpi);
        Assert.Contains(painter.Separators, rectangle => rectangle == new Rectangle(divider.Right - actualWidth, divider.Top, actualWidth, divider.Height));
        ribbon.Theme = theme with { BodyAppearance = null };
        Assert.Equal(before.Targets.Select(target => (target.Bounds, target.Clip)).ToArray(), ribbon.Snapshot.Targets.Select(target => (target.Bounds, target.Clip)).ToArray());
    });

    [Theory] [InlineData(RibbonLayoutMode.Classic)] [InlineData(RibbonLayoutMode.Simplified)]
    public void Join_preferences_refresh_existing_geometry_and_keep_native_editors_models_and_reset(RibbonLayoutMode mode) => FoundationTests.Sta(() =>
    {
        using var host = new Form { Bounds = new(-16000, -16000, 1000, 700), ShowInTaskbar = false };
        using var ribbon = new RibbonControl { QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon };
        var field = new RibbonTextBoxItem { Text = "Name", Value = "Retained joins", SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
        var command = new RibbonButtonItem { Text = "Save", SimplifiedPlacement = RibbonSimplifiedPlacement.Inline }; var tab = new RibbonTab(); var group = new RibbonGroup { CanResize = false };
        group.Items.AddRange(field, command); tab.Groups.Add(group); ribbon.Tabs.Add(tab); ribbon.QuickAccessItems.Add(new RibbonQuickAccessReference { Source = command });
        ribbon.MessageBar.Items.Add(new RibbonMessage { Title = "Notice", Text = "Stable" }); host.Controls.Add(ribbon); host.Show(); Application.DoEvents();
        var editor = Assert.IsAssignableFrom<TextBoxBase>(ribbon.Editors.EditorFor(field)); nint handle = editor.Handle; editor.Select(2, 4); ribbon.LayoutMode = mode; var parent = editor.Parent;
        foreach (var density in Enum.GetValues<RibbonDensity>())
        foreach (bool minimized in new[] { false, true })
        {
            ribbon.Density = density; ribbon.IsMinimized = minimized; var original = ribbon.Theme; var before = ribbon.Snapshot; int height = ribbon.Height;
            ribbon.Theme = original with { BodyAppearance = new() { JoinBelowRibbonQuickAccess = false, JoinMessageBarToBody = true } };
            Assert.False(ribbon.JoinBelowRibbonQuickAccess); Assert.False(ribbon.JoinMessageBar); Assert.Equal(0, ribbon.Snapshot.QuickAccessBounds.Left);
            ribbon.Theme = original with { BodyAppearance = new() { JoinBelowRibbonQuickAccess = true, JoinMessageBarToBody = false } };
            Assert.True(ribbon.JoinBelowRibbonQuickAccess); Assert.False(ribbon.JoinMessageBar); Assert.Equal(0, ribbon.Snapshot.MessageBounds.Left);
            Assert.Equal(handle, editor.Handle); Assert.Same(parent, editor.Parent); Assert.Equal("Retained joins", editor.Text); Assert.Equal(2, editor.SelectionStart); Assert.Equal(4, editor.SelectionLength);
            ribbon.Theme = original; Assert.Equal(before.BodyFrameBounds, ribbon.Snapshot.BodyFrameBounds); Assert.Equal(before.MessageBounds, ribbon.Snapshot.MessageBounds); Assert.Equal(before.QuickAccessBounds, ribbon.Snapshot.QuickAccessBounds); Assert.Equal(height, ribbon.Height);
            Assert.Equal(before.Targets.Select(target => (target.Bounds, target.Clip)).ToArray(), ribbon.Snapshot.Targets.Select(target => (target.Bounds, target.Clip)).ToArray());
        }
        using var separate = new Separate(); ribbon.Renderer = separate; ribbon.Theme = ribbon.Theme with { BodyAppearance = new() { JoinBelowRibbonQuickAccess = true, JoinMessageBarToBody = true } };
        Assert.False(ribbon.JoinBelowRibbonQuickAccess); Assert.False(ribbon.JoinMessageBar);
        ribbon.IsMinimized = false; ribbon.Theme = ribbon.Theme with { BodyCornerRadius = 20 }; using var image = new Bitmap(180, 120); using var g = Graphics.FromImage(image);
        separate.DrawBody(new(g, new(10, 10, 150, 90), ribbon.Theme, ribbon.Font, 120)); Assert.True(separate.RadiusCalls > 0);
    });

    [Theory] [InlineData("BorderWidth", 32)] [InlineData("MessageBorderWidth", 32)] [InlineData("GroupSeparatorWidth", 32)]
    [InlineData("QuickAccessSeparatorWidth", 32)] [InlineData("QuickAccessSeparatorInset", 1024)]
    public void Invalid_frame_values_do_not_mutate_theme_or_selection_and_members_reset_independently(string name, int maximum) => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { BuiltInTheme = RibbonBuiltInTheme.Office2019Light }; var original = ribbon.Theme;
        var property = typeof(RibbonBodyAppearanceSettings).GetProperty(name)!;
        foreach (int invalid in new[] { -1, maximum + 1 })
        {
            var settings = new RibbonBodyAppearanceSettings(); property.SetValue(settings, invalid);
            Assert.Throws<ArgumentOutOfRangeException>(() => ribbon.Theme = original with { BodyAppearance = settings }); Assert.Same(original, ribbon.Theme); Assert.Equal(RibbonBuiltInTheme.Office2019Light, ribbon.BuiltInTheme);
        }
        foreach (int? valid in new int?[] { 0, maximum, null })
        { var settings = new RibbonBodyAppearanceSettings(); property.SetValue(settings, valid); ribbon.Theme = original with { BodyAppearance = settings }; Assert.Same(settings, ribbon.Theme.BodyAppearance); }
    });

    [Fact]
    public void Immutable_frame_group_restores_application_theme_without_global_or_selector_coupling() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl(); using var other = new RibbonControl();
        var settings = new RibbonBodyAppearanceSettings { BorderWidth = 3, GroupSeparatorWidth = 2, JoinMessageBarToBody = false };
        var application = RibbonThemes.Office2019Light with { BodyAppearance = settings }; ribbon.Theme = application;
        ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2024Light; ribbon.BuiltInTheme = null; Assert.Same(application, ribbon.Theme); Assert.Null(other.Theme.BodyAppearance);
        ribbon.Theme = application with { BodyAppearance = settings with { BorderWidth = null } }; Assert.Equal(2, ribbon.Theme.BodyAppearance!.GroupSeparatorWidth); Assert.Equal(3, settings.BorderWidth);
        Assert.Equal(application.BodyBottomBorderOnly, ribbon.Theme.BodyBottomBorderOnly); Assert.Equal(application.HeaderPresentation, ribbon.Theme.HeaderPresentation);
        ribbon.Theme = application with { BodyAppearance = null }; Assert.Null(ribbon.Theme.BodyAppearance);
        var originalMetrics = new RibbonBodyFrameMetrics(application, 120, RibbonPresentationResolver.OfficeBody);
        var mixedMetrics = new RibbonBodyFrameMetrics(application with { HeaderPresentation = RibbonHeaderPresentation.Underline, BackstageStyle = RibbonBackstageStyle.Modern }, 120, RibbonPresentationResolver.OfficeBody);
        Assert.Equal(originalMetrics, mixedMetrics); Assert.NotEqual(RibbonPresentationResolver.OfficeBody, RibbonPresentationResolver.NeutralBody);
    });

    private sealed class Hooks : RibbonOffice2024Renderer
    {
        internal readonly List<Rectangle> Separators = [];
        internal bool ReplaceSeparator;
        public override void DrawGroupSeparator(RibbonPaintContext context)
        {
            Separators.Add(context.Bounds);
            if (ReplaceSeparator) { using var brush = new SolidBrush(Color.Lime); context.Graphics.FillRectangle(brush, context.Bounds); }
            else base.DrawGroupSeparator(context);
        }
        public override void DrawText(RibbonPaintContext context, string text, Color color, TextFormatFlags flags) { }
    }
    private sealed class Separate : RibbonOffice2024Renderer
    {
        internal int RadiusCalls;
        public override bool JoinBelowRibbonQuickAccess => false;
        public override bool JoinMessageBarToBody => false;
        public override int GetBodyCornerRadius(RibbonTheme theme, int dpi) { RadiusCalls++; return 9; }
    }
    private static Rectangle Ink(Bitmap image, Color color)
    {
        var result = Rectangle.Empty;
        for (int y = 0; y < image.Height; y++) for (int x = 0; x < image.Width; x++)
            if (image.GetPixel(x, y).ToArgb() == color.ToArgb()) result = result.IsEmpty ? new(x, y, 1, 1) : Rectangle.Union(result, new(x, y, 1, 1));
        return result;
    }

    [Fact]
    public void Default_frame_group_and_connected_paint_matches_saved_images() => FoundationTests.Sta(() =>
    {
        foreach (bool classic in new[] { false, true })
        foreach (bool office in new[] { false, true })
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        foreach (bool emptyGroup in new[] { false, true })
        {
            int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
            var theme = (classic ? RibbonThemes.Office2019Light : RibbonThemes.Office2024Light) with { BodyAppearance = emptyGroup ? new() : null };
            using RibbonRenderer painter = office ? new RibbonOffice2024Renderer() : new RibbonRenderer();
            using var image = new Bitmap(S(700), S(390)); using var g = Graphics.FromImage(image); g.Clear(theme.Background);
            using var group = new RibbonGroup { Text = "Editing" }; using var font = new Font("Segoe UI", 9 * dpi / 96f);
            for (int state = 0; state < 2; state++)
            {
                var context = new RibbonPaintContext(g, new(S(10 + state * 350), S(10), S(320), S(115)), theme, font, dpi) { IsBottomEdgeConnected = state == 1 };
                painter.DrawBody(context);
                painter.DrawQuickAccessToolbar(context with { Bounds = new(context.Bounds.Left, S(125), context.Bounds.Width, S(32)), IsQuickAccessToolbarJoined = state == 1 }, RibbonQuickAccessPosition.BelowRibbon);
                painter.DrawMessageBar(context with { Bounds = new(context.Bounds.Left, S(157), context.Bounds.Width, S(46)), IsTopEdgeConnected = state == 1, IsBottomEdgeConnected = false });
                painter.DrawGroup(context with { Bounds = new(context.Bounds.Left, S(220), S(180), S(110)), GroupSeparatorBounds = new(context.Bounds.Left, S(220), S(180), S(110)) }, group, false);
            }
            Compare($"paint-{classic}-{office}-{dpi}", image, $"radius={painter.GetBodyCornerRadius(theme, dpi)};shadow={painter.GetBodyShadowPadding(theme, dpi)}");
        }
    });

    [Fact]
    public void Default_live_body_layout_and_paint_match_saved_modes_joins_and_native_bounds() => FoundationTests.Sta(() =>
    {
        foreach (bool classic in new[] { false, true })
        foreach (var backdrop in new[] { RibbonWindowBackdrop.None, RibbonWindowBackdrop.Mica })
        {
            using var host = new RibbonForm { StartPosition = FormStartPosition.Manual, Bounds = new(-16000, -16000, 1000, 700), ShowInTaskbar = false, Backdrop = backdrop };
            using var ribbon = new RibbonControl { Theme = classic ? RibbonThemes.Office2019Light : RibbonThemes.Office2024Light, QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon };
            var tab = new RibbonTab { Text = "Home" }; var edit = new RibbonGroup { Text = "Editing", CanResize = false }; var field = new RibbonTextBoxItem { Text = "Name", Value = "Retained", SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
            var command = new RibbonButtonItem { Text = "Save", SimplifiedPlacement = RibbonSimplifiedPlacement.Inline }; edit.Items.AddRange(field, command);
            var layout = new RibbonGroup { Text = "Layout", CanResize = false }; layout.Items.Add(new RibbonButtonItem { Text = "Apply", SimplifiedPlacement = RibbonSimplifiedPlacement.Inline }); tab.Groups.AddRange(edit, layout); ribbon.Tabs.Add(tab);
            ribbon.QuickAccessItems.Add(new RibbonQuickAccessReference { Source = command }); ribbon.MessageBar.Items.Add(new RibbonMessage { Title = "Notice", Text = "Frame review" });
            host.Controls.Add(ribbon); host.Ribbon = ribbon; host.Show(); Application.DoEvents();
            foreach (var mode in Enum.GetValues<RibbonLayoutMode>())
            foreach (var density in Enum.GetValues<RibbonDensity>())
            foreach (bool minimized in new[] { false, true })
            foreach (int width in new[] { 1000, 380 })
            {
                ribbon.LayoutMode = mode; ribbon.Density = density; ribbon.IsMinimized = minimized; host.ClientSize = new(width, 700); Application.DoEvents();
                using var image = new Bitmap(ribbon.Width, ribbon.Height); ribbon.DrawToBitmap(image, ribbon.ClientRectangle);
                var frame = ribbon.Snapshot;
                string geometry = $"{backdrop}/{host.EffectiveBackdrop}/{ribbon.DeviceDpi};height={ribbon.Height};body={frame.BodyBounds};frame={frame.BodyFrameBounds};qat={frame.QuickAccessBounds};message={frame.MessageBounds};viewport={frame.BodyViewport};extent={frame.BodyExtent};" +
                    string.Join(";", frame.Targets.Select(target => $"{target.Kind}/{target.Element?.Text}:{target.Bounds}:{target.Clip}"));
                Compare($"live-{classic}-{backdrop}-{mode}-{density}-{minimized}-{width}", image, geometry);
            }
        }
    });
    private static void Compare(string name, Bitmap image, string geometry)
    {
        using var stream = new MemoryStream(); image.Save(stream, ImageFormat.Png); byte[] png = stream.ToArray();
        if (Environment.GetEnvironmentVariable("RIBBONKIT_BODY_CAPTURE") is { Length: > 0 } output)
        { Directory.CreateDirectory(output); File.WriteAllBytes(Path.Combine(output, name + ".png"), png); File.WriteAllText(Path.Combine(output, name + ".txt"), geometry); }
        if (Environment.GetEnvironmentVariable("RIBBONKIT_BODY_BASELINE") is { Length: > 0 } baseline)
        { Assert.Equal(File.ReadAllBytes(Path.Combine(baseline, name + ".png")), png); Assert.Equal(File.ReadAllText(Path.Combine(baseline, name + ".txt")), geometry); }
    }
}
