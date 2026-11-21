using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class Office2007CommandFeedbackTests
{
    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Split_halves_sample_complete_gel_ramps_with_shared_perimeter_and_unchanged_clips(int dpi) => FoundationTests.Sta(() =>
    {
        var t = RibbonThemes.Office2007Blue; Assert.True(t.CommandAppearance!.SplitFillUsesPartBounds);
        foreach (bool office in new[] { false, true }) foreach (bool vertical in new[] { false, true }) foreach (bool arrowHot in new[] { false, true }) foreach (bool pressed in new[] { false, true })
        {
            using RibbonRenderer painter = office ? new RibbonOffice2024Renderer() : new RibbonRenderer();
            using var split = new RibbonSplitButtonItem { Layout = vertical ? RibbonSplitButtonLayout.Vertical : RibbonSplitButtonLayout.Horizontal };
            var bounds = new Rectangle(4, 4, RibbonLayoutEngine.Scale(130, dpi), RibbonLayoutEngine.Scale(140, dpi));
            var (primary, arrow) = RibbonSplitLayout.Calculate(bounds, split, RibbonItemSize.Large, false, dpi);
            using var image = new Bitmap(bounds.Right + 4, bounds.Bottom + 4); using var g = Graphics.FromImage(image); g.Clear(Color.Magenta);
            var c = new RibbonPaintContext(g, primary, t, Control.DefaultFont, dpi, Hot: !arrowHot, Pressed: !arrowHot && pressed);
            painter.DrawSplitItem(c, c with { Bounds = arrow, Hot = arrowHot, Pressed = arrowHot && pressed }, split, RibbonItemSize.Large, false);
            var paint = Rectangle.Inflate(bounds, -1, -1); var active = Rectangle.Intersect(paint, arrowHot ? arrow : primary); var fill = pressed ? t.Pressed : t.Hover;
            int x = active.Left + RibbonLayoutEngine.Scale(9, dpi);
            foreach (double offset in new[] { .18, .34, .44, .7 })
            {
                int y = active.Top + (int)Math.Round((active.Height - 1) * offset);
                Assert.Equal(RibbonFillColors.At(fill, (y - active.Top) / (double)(active.Height - 1)).ToArgb(), image.GetPixel(x, y).ToArgb());
            }
            using var full = (Bitmap)image.Clone(); g.Clear(Color.Magenta); var clip = Rectangle.Inflate(bounds, -5, -5); g.SetClip(clip);
            painter.DrawSplitItem(c, c with { Bounds = arrow, Hot = arrowHot, Pressed = arrowHot && pressed }, split, RibbonItemSize.Large, false);
            Assert.Equal(full.GetPixel(x, active.Top + active.Height / 2), image.GetPixel(x, active.Top + active.Height / 2));
            Assert.Equal(Color.Magenta.ToArgb(), image.GetPixel(bounds.Left, bounds.Top).ToArgb());
            g.ResetClip();
            if (vertical && arrowHot && !pressed && office)
            {
                string output = Path.Combine(Environment.CurrentDirectory, "artifacts/office2007-g6/command-feedback"); Directory.CreateDirectory(output); full.Save(Path.Combine(output, $"split-{dpi}.png"));
            }
        }
    });

    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Qat_and_overflow_keep_gold_orange_states_after_real_caption_recoloring(int dpi) => FoundationTests.Sta(() =>
    {
        var t = RibbonThemes.Office2007Blue;
        using var host = new RibbonForm { TitleBarColor = Color.DarkBlue }; var caption = host.CaptionTheme(t, false);
        Assert.NotEqual(t.Hover, caption.Hover);
        foreach (bool office in new[] { false, true }) foreach (bool composited in new[] { false, true }) foreach (int state in new[] { 0, 1, 2, 3 })
        {
            using RibbonRenderer painter = office ? new RibbonOffice2024Renderer() : new RibbonRenderer();
            using var model = new RibbonControl { Theme = t };
            using var source = new RibbonToggleItem { Checked = state >= 2 }; using var entry = new RibbonQuickAccessReference { Source = source };
            model.CommandCatalog.Commands.Add(source); model.QuickAccessItems.Add(entry);
            var bounds = new Rectangle(4, 4, 96, 64); using var image = new Bitmap(105, 75); using var g = Graphics.FromImage(image); g.Clear(Color.LightBlue);
            var c = new RibbonPaintContext(g, bounds, caption, Control.DefaultFont, dpi, Hot: state != 2, Pressed: state == 1) { CompositedText = composited };
            painter.DrawQuickAccessItem(c, entry, false);
            var fill = state == 0 ? t.Hover : state == 1 ? t.Pressed : state == 2 ? t.Checked : t.CheckedHover!.Value;
            Assert.Equal(RibbonFillColors.At(fill, 17d / 61).ToArgb(), image.GetPixel(16, 22).ToArgb());
            foreach (bool down in new[] { false, true })
            {
                g.Clear(Color.LightBlue); painter.DrawQuickAccessOverflow(c with { Hot = !down, Pressed = down });
                Assert.Equal(RibbonFillColors.At(down ? t.Pressed : t.Hover, 17d / 61).ToArgb(), image.GetPixel(16, 22).ToArgb());
            }
        }
    });

    [Fact]
    public void Qat_roles_reset_independently_preserve_alpha_stops_and_do_not_recolor_menus_or_header_utilities()
    {
        using var image = new Bitmap(4, 4); using var g = Graphics.FromImage(image); var t = RibbonThemes.Office2007Blue;
        var fill = new RibbonFill(Color.FromArgb(200, Color.Red), Color.FromArgb(80, Color.Blue)) { IntermediateStops = RibbonGradientStops.WithHardTransitions([new(.4, Color.FromArgb(160, Color.Gold)), new(.4, Color.FromArgb(120, Color.Orange))]), Direction = RibbonGradientDirection.Horizontal };
        t = t with { QuickAccessBackdropOpacity = .5f, CommandAppearance = t.CommandAppearance! with { QuickAccessHoverBackground = fill }, BackdropHover = new(Color.Purple) };
        var c = new RibbonPaintContext(g, new(0, 0, 4, 4), t, Control.DefaultFont, 96, Hot: true) { CompositedText = true };
        var painted = RibbonRenderer.QuickAccessFill(c, t.Hover, false);
        Assert.Equal(100, painted.Start.A); Assert.Equal(new[] { 80, 60 }, painted.IntermediateStops!.Select(s => (int)s.Color.A)); Assert.Equal(fill.Direction, painted.Direction);
        Assert.Equal(new[] { .4, .4 }, painted.IntermediateStops!.Select(s => s.Offset));
        Assert.Equal(t.Hover, RibbonRenderer.QuickAccessFill(c, t.Hover, true));
        Assert.Equal(Color.Purple.R, RibbonRenderer.QuickAccessFill(c, t.Hover, false, useOverrides: false).Start.R);
        Assert.Equal(Color.Purple.B, RibbonRenderer.QuickAccessFill(c with { Theme = t with { CommandAppearance = t.CommandAppearance with { QuickAccessHoverBackground = null } } }, t.Hover, false).Start.B);
        Assert.Equal(0, RibbonRenderer.QuickAccessFill(c with { Theme = t with { QuickAccessBackdropOpacity = 0 } }, t.Hover, false).Start.A);
    }

    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Qat_split_projection_retains_part_canvases_for_authored_horizontal_gradients(int dpi) => FoundationTests.Sta(() =>
    {
        var t = RibbonThemes.Office2007Blue; var hover = t.Hover with { Direction = RibbonGradientDirection.Horizontal }; var pressed = t.Pressed with { Direction = RibbonGradientDirection.Horizontal };
        t = t with { CommandAppearance = t.CommandAppearance! with { QuickAccessHoverBackground = hover, QuickAccessPressedBackground = pressed } };
        using var model = new RibbonControl { Theme = t }; using var source = new RibbonSplitButtonItem(); model.CommandCatalog.Commands.Add(source);
        using var entry = new RibbonQuickAccessDropDownReference { Source = source }; model.QuickAccessItems.Add(entry);
        using var painter = new RibbonOffice2024Renderer(); using var image = new Bitmap(250, 90); using var g = Graphics.FromImage(image);
        var primary = new Rectangle(4, 4, 180, 80); var arrow = new Rectangle(184, 4, 60, 80); var canvas = Rectangle.Intersect(Rectangle.Inflate(Rectangle.Union(primary, arrow), -1, -1), arrow);
        foreach (bool down in new[] { false, true })
        {
            g.Clear(Color.Magenta); var c = new RibbonPaintContext(g, primary, t, Control.DefaultFont, dpi);
            painter.DrawQuickAccessSplitItem(c, c with { Bounds = arrow, Hot = true, Pressed = down }, entry, false);
            int x = canvas.Left + 12;
            Assert.Equal(RibbonFillColors.At(down ? pressed : hover, 12d / (canvas.Width - 1)).ToArgb(), image.GetPixel(x, 20).ToArgb());
        }
    });

    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Simplified_panels_are_optional_while_classic_frames_and_label_bands_stay_identical(int dpi) => FoundationTests.Sta(() =>
    {
        using var painter = new RibbonOffice2024Renderer(); using var group = new RibbonGroup(); var t = RibbonThemes.Office2007Blue;
        Assert.True(new RibbonGroupPanelAppearanceSettings().ShowInSimplified); Assert.False(t.BodyAppearance!.GroupPanel!.ShowInSimplified);
        using var image = new Bitmap(250, 110); using var g = Graphics.FromImage(image); var bounds = new Rectangle(4, 4, 240, 100);
        var c = new RibbonPaintContext(g, bounds, t, Control.DefaultFont, dpi) { GroupFrameBounds = bounds, IsSimplifiedGroup = true };
        g.Clear(Color.Magenta); painter.DrawGroupPanel(c, group, false); Assert.Equal(Color.Magenta.ToArgb(), image.GetPixel(80, 50).ToArgb());
        var panels = t with { BodyAppearance = t.BodyAppearance with { GroupPanel = t.BodyAppearance.GroupPanel with { ShowInSimplified = true } } };
        painter.DrawGroupPanel(c with { Theme = panels }, group, false); Assert.NotEqual(Color.Magenta.ToArgb(), image.GetPixel(80, 50).ToArgb());
        g.Clear(Color.Magenta); painter.DrawGroupPanel(c with { IsSimplifiedGroup = false }, group, false); using var classic = (Bitmap)image.Clone();
        g.Clear(Color.Magenta); painter.DrawGroupPanel(c with { Theme = panels, IsSimplifiedGroup = false }, group, false);
        for (int y = 0; y < image.Height; y++) for (int x = 0; x < image.Width; x++) Assert.Equal(classic.GetPixel(x, y), image.GetPixel(x, y));
    });

    [Theory] [InlineData(RibbonDensity.Compact)] [InlineData(RibbonDensity.Touch)]
    public void Native_simplified_separators_and_open_overflow_use_existing_snapshot_targets(RibbonDensity density) => FoundationTests.Sta(() =>
    {
        using var painter = new Probe(); using var host = new RibbonForm { Bounds = new(-16000, -16000, 1050, 650), ShowInTaskbar = false };
        using var ribbon = new RibbonControl { BuiltInTheme = RibbonBuiltInTheme.Office2007Blue, LayoutMode = RibbonLayoutMode.Simplified, Density = density, Renderer = painter, AnimationLevel = RibbonAnimationLevel.None }; host.Controls.Add(ribbon); host.Ribbon = ribbon;
        var tab = new RibbonTab { Text = "Home" }; ribbon.Tabs.Add(tab);
        for (int i = 0; i < 3; i++) { var group = new RibbonGroup { Text = "Group " + i }; tab.Groups.Add(group); group.Items.Add(new RibbonButtonItem { Text = "Command " + i, SimplifiedPlacement = RibbonSimplifiedPlacement.Inline }); }
        host.Show(); Application.DoEvents(); var before = ribbon.Snapshot.Targets.Select(t => t.Bounds).ToArray();
        using var image = new Bitmap(ribbon.Width, ribbon.Height); painter.Separators.Clear(); ribbon.DrawToBitmap(image, ribbon.ClientRectangle);
        Assert.NotEmpty(painter.Separators); Assert.Equal(ribbon.Snapshot.SimplifiedDividers.Count, painter.Separators.Count);
        var more = ribbon.Snapshot.Targets.Single(t => t.Element == ribbon.Simplified.More); painter.OverflowPressed = false; ribbon.Activate(more); ribbon.DrawToBitmap(image, ribbon.ClientRectangle);
        Assert.True(painter.OverflowPressed, "Open More must retain its pressed visual"); Assert.True(ribbon.Popups.HasOpen, "Native More surface is open"); ribbon.Popups.CloseAll(false);
        var original = ribbon.Theme; ribbon.Theme = original with { BodyAppearance = original.BodyAppearance! with { GroupPanel = original.BodyAppearance.GroupPanel! with { ShowInSimplified = true } } };
        Assert.Equal(before, ribbon.Snapshot.Targets.Select(t => t.Bounds)); painter.Separators.Clear(); ribbon.DrawToBitmap(image, ribbon.ClientRectangle); Assert.Empty(painter.Separators);
        host.Close();
    });
    private sealed class Probe : RibbonOffice2024Renderer
    {
        internal List<Rectangle> Separators = []; internal bool OverflowPressed;
        public override void DrawGroupSeparator(RibbonPaintContext c) { Separators.Add(c.Bounds); base.DrawGroupSeparator(c); }
        public override void DrawQuickAccessOverflow(RibbonPaintContext c) { OverflowPressed |= c.Pressed; base.DrawQuickAccessOverflow(c); }
    }
}
