using RibbonKit.WinForms.Layout;
using RibbonKit.WinForms.Design;
using System.ComponentModel;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class MenuContainerGeometryTests
{
    private static RibbonTheme Custom => RibbonThemes.Office2024Light with
    {
        MenuMinimumWidth = 410, MenuDescriptionMinimumWidth = 510, MenuHeaderHorizontalPadding = 18,
        MenuHeaderVerticalPadding = 14, MenuHeaderBottomGap = 12, MenuOverflowScrollButtonHeight = 46
    };

    [Theory] [InlineData(false)] [InlineData(true)]
    public void Heading_overrides_share_measurement_and_paint_but_leave_gallery_and_application_headings_independent(bool office) => FoundationTests.Sta(() =>
    {
        var record = new Record(); using RibbonRenderer painter = office ? new Office(record) : new Flat(record);
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        foreach (var density in Enum.GetValues<RibbonDensity>())
        foreach (int points in new[] { 9, 14 })
        foreach (bool composited in new[] { false, true })
        {
            int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
            using var font = new Font("Segoe UI", points * dpi / 96f);
            var metrics = RibbonMenuMetrics.For(density, Custom);
            Assert.Equal(410, metrics.MenuMinimumWidth); Assert.Equal(510, metrics.MenuDescriptionWidth);
            Assert.Equal(font.Height + 2 * S(14), metrics.HeaderHeight(font, dpi));
            Assert.Equal(2 * S(18) + S(16), metrics.HeaderWidthPadding(dpi));
            using var bitmap = new Bitmap(S(410), metrics.HeaderHeight(font, dpi)); using var graphics = Graphics.FromImage(bitmap);
            var context = new RibbonPaintContext(graphics, new(Point.Empty, bitmap.Size), Custom, font, dpi) { Density = density, CompositedText = composited };
            painter.DrawMenuHeader(context, "Heading"); Assert.Equal(S(18), record.Text.Left); Assert.Equal(bitmap.Width - 2 * S(18), record.Text.Width);
            Assert.True(context.Bounds.Contains(record.Text));
            painter.DrawGalleryHeading(context, "Heading"); Assert.Equal(S(6), record.Text.Left);
            painter.DrawMenuHeader(context with { Theme = RibbonThemes.Office2024Light, ApplicationMenuPart = RibbonApplicationMenuPart.Pane }, "Heading"); var application = record.Text;
            painter.DrawMenuHeader(context with { ApplicationMenuPart = RibbonApplicationMenuPart.Pane }, "Heading"); Assert.Equal(application, record.Text);
            painter.DrawMenuHeader(context with { Theme = Custom with { MenuHeaderHorizontalPadding = 1024 } }, "Heading"); Assert.Equal(0, record.Text.Width);
            if (Environment.GetEnvironmentVariable("RIBBONKIT_MENU_CONTAINER_CAPTURE") is { Length: > 0 } capture)
            {
                graphics.Clear(Custom.PopupColor); painter.DrawMenuHeader(context, "Roomier menu heading");
                Directory.CreateDirectory(capture); bitmap.Save(Path.Combine(capture, $"override-{office}-{density}-{dpi}-{points}-{composited}.png"));
            }
        }
    });

    [Theory] [InlineData(0)] [InlineData(1)] [InlineData(2)]
    public void Desired_widths_and_heading_gap_reach_ordinary_qat_and_more_containers(int kind) => FoundationTests.Sta(() =>
    {
        foreach (var density in Enum.GetValues<RibbonDensity>())
        {
            using var ribbon = new RibbonControl { Theme = Custom, Density = density, Width = 240, QuickAccessMaxWidth = 28 };
            var tab = new RibbonTab(); var group = new RibbonGroup(); var menu = new RibbonDropDownItem { DropDownHeader = "Heading" };
            var commands = Enumerable.Range(0, 20).Select(i => new RibbonButtonItem { Text = "Command " + i, SimplifiedPlacement = RibbonSimplifiedPlacement.Inline }).ToArray();
            group.Items.Add(menu); group.Items.AddRange(commands); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
            menu.Items.Add(new RibbonButtonItem { Text = "Open" }); ribbon.QuickAccessItems.AddRange(commands.Select(i => new RibbonQuickAccessReference { Source = i }).ToArray());
            if (kind == 2) ribbon.LayoutMode = RibbonLayoutMode.Simplified;
            var owner = kind == 0 ? (RibbonElement)menu : kind == 1 ? ribbon.QuickAccessOverflow : ribbon.Simplified.More;
            if (kind == 2) Assert.NotEmpty(ribbon.Snapshot.Targets);
            int S(int n) => RibbonLayoutEngine.Scale(n, ribbon.DeviceDpi);
            using (var plain = new RibbonPopupSurface(ribbon, owner, new(1200, 900))) Assert.Equal(S(410), plain.Width);
            if (kind == 0) menu.Items[0].MenuDescription = "Keep formatting."; else foreach (var command in commands) command.MenuDescription = "Keep formatting.";
            using var described = new RibbonPopupSurface(ribbon, owner, new(1200, 900)); Assert.Equal(S(510), described.Width);
            if (kind == 0)
            {
                Assert.Equal(ribbon.Font.Height + 2 * S(14), described.MenuHeaderBounds.Height);
                Assert.Equal(described.MenuHeaderBounds.Bottom + S(12), described.Targets[0].Bounds.Top);
                using var field = new RibbonTextBoxItem { Text = "Name", InputWidth = 700 };
                menu.Items.Add(field); using var fieldMenu = new RibbonPopupSurface(ribbon, menu, new(2000, 2000)); Assert.True(fieldMenu.Width > S(510));
                menu.Items.Remove(field);
            }
            using var constrained = new RibbonPopupSurface(ribbon, owner, new(300, 240));
            Assert.InRange(constrained.Width, 1, 300); Assert.InRange(constrained.Height, 1, 240);
        }
    });

    [Theory] [InlineData(false)] [InlineData(true)]
    public void Overflow_bands_share_scroll_hit_clip_and_reveal_geometry_with_touch_floor_and_tiny_area_clamps(bool more) => FoundationTests.Sta(() =>
    {
        foreach (var density in Enum.GetValues<RibbonDensity>())
        {
            using var form = new QuietForm { Width = 900, Height = 650, ShowInTaskbar = false, Location = new(-16000, -16000), StartPosition = FormStartPosition.Manual };
            var ribbon = new RibbonControl { Width = 240, Theme = Custom, Density = density, QuickAccessMaxWidth = 28 }; form.Controls.Add(ribbon);
            var tab = new RibbonTab(); var group = new RibbonGroup(); var commands = Enumerable.Range(0, 24).Select(i => new RibbonButtonItem { Text = "Command " + i, SimplifiedPlacement = RibbonSimplifiedPlacement.Inline }).ToArray();
            group.Items.AddRange(commands); tab.Groups.Add(group); ribbon.Tabs.Add(tab); ribbon.QuickAccessItems.AddRange(commands.Select(i => new RibbonQuickAccessReference { Source = i }).ToArray());
            if (more) { ribbon.LayoutMode = RibbonLayoutMode.Simplified; ribbon.Width = 240; }
            _ = ribbon.Snapshot;
            using var popup = new RibbonPopupSurface(ribbon, more ? ribbon.Simplified.More : ribbon.QuickAccessOverflow, new(390, 300));
            form.Controls.Add(popup); form.Show(); Application.DoEvents();
            int S(int n) => RibbonLayoutEngine.Scale(n, ribbon.DeviceDpi);
            Assert.Equal(S(46), popup.OverflowUpBounds.Height); Assert.Equal(S(46), popup.OverflowDownBounds.Height);
            Assert.All(popup.Targets, t => { Assert.Equal(popup.OverflowViewport, t.Clip); Assert.False(t.VisibleBounds.IntersectsWith(popup.OverflowUpBounds)); Assert.False(t.VisibleBounds.IntersectsWith(popup.OverflowDownBounds)); });
            popup.FocusElement(popup.Targets[^1].Element!); Assert.True(popup.OverflowViewport.Contains(popup.Targets[^1].Bounds));
            popup.FocusElement(popup.Targets[0].Element!); Assert.True(popup.ScrollOverflow(true));
            ribbon.Theme = Custom with { MenuOverflowScrollButtonHeight = 1 };
            using var floor = new RibbonPopupSurface(ribbon, more ? ribbon.Simplified.More : ribbon.QuickAccessOverflow, new(390, 300));
            Assert.Equal(S(density == RibbonDensity.Touch ? 28 : 1), floor.OverflowUpBounds.Height);
            ribbon.Theme = Custom with { MenuOverflowScrollButtonHeight = 1024 };
            using var tiny = new RibbonPopupSurface(ribbon, more ? ribbon.Simplified.More : ribbon.QuickAccessOverflow, new(180, 80));
            Assert.True(tiny.OverflowViewport.Height >= 1); Assert.True(2 * tiny.OverflowUpBounds.Height < tiny.Height);
        }
    });

    [Fact]
    public void Invalid_values_are_atomic_and_reset_is_isolated_from_accepted_geometry_and_designer_serialization() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { Theme = Custom }; using var other = new RibbonControl();
        foreach (var invalid in new[] { Custom with { MenuMinimumWidth = 0 }, Custom with { MenuMinimumWidth = 1025 },
            Custom with { MenuDescriptionMinimumWidth = 0 }, Custom with { MenuDescriptionMinimumWidth = 1025 },
            Custom with { MenuHeaderHorizontalPadding = -1 }, Custom with { MenuHeaderHorizontalPadding = 1025 },
            Custom with { MenuHeaderVerticalPadding = -1 }, Custom with { MenuHeaderVerticalPadding = 1025 },
            Custom with { MenuHeaderBottomGap = -1 }, Custom with { MenuHeaderBottomGap = 1025 },
            Custom with { MenuOverflowScrollButtonHeight = 0 }, Custom with { MenuOverflowScrollButtonHeight = 1025 } })
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ribbon.Theme = invalid); Assert.Equal(Custom, ribbon.Theme);
        }
        var accepted = Custom with { MenuIconTextGap = 16, MenuDescriptionGap = 10 };
        ribbon.Theme = accepted; ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2024Light;
        TypeDescriptor.GetProperties(ribbon)[nameof(ribbon.BuiltInTheme)]!.ResetValue(ribbon); Assert.Equal(accepted, ribbon.Theme);
        ribbon.Theme = accepted with { MenuMinimumWidth = null, MenuDescriptionMinimumWidth = null, MenuHeaderHorizontalPadding = null, MenuHeaderVerticalPadding = null, MenuHeaderBottomGap = null, MenuOverflowScrollButtonHeight = null };
        Assert.Equal(RibbonThemes.Office2024Light with { MenuIconTextGap = 16, MenuDescriptionGap = 10 }, ribbon.Theme);
        Assert.Same(RibbonThemes.Office2024Light, other.Theme); Assert.False(TypeDescriptor.GetProperties(ribbon)[nameof(ribbon.Theme)]!.IsBrowsable);
        ribbon.Theme = Custom with { MenuHeaderHorizontalPadding = 0, MenuHeaderVerticalPadding = 0, MenuHeaderBottomGap = 0 };
        using var menu = new RibbonDropDownItem { DropDownHeader = "Heading" }; menu.Items.Add(new RibbonButtonItem { Text = "Open" });
        using var surface = new RibbonPopupSurface(ribbon, menu, new(900, 700)); Assert.Equal(ribbon.Font.Height, surface.MenuHeaderBounds.Height); Assert.Equal(surface.MenuHeaderBounds.Bottom, surface.Targets[0].Bounds.Top);
    });

    [Fact]
    public void Detached_designer_preview_uses_header_bounds_and_width_without_running_editors_commands_or_transactions() => FoundationTests.Sta(() =>
    {
        using var host = new DesignerAuthoringTests.Host(); using var ribbon = DesignerAuthoringTests.MakeRibbon(host); ribbon.Theme = Custom;
        var menu = new RibbonDropDownItem { Text = "Menu", DropDownHeader = "Heading" }; var item = new RibbonButtonItem { Text = "Open" }; var field = new RibbonTextBoxItem { Text = "Name", Value = "Retained" };
        menu.Items.AddRange(item, field); ribbon.Tabs[0].Groups[0].Items.Add(menu);
        int calls = 0; item.Click += (_, _) => calls++; var keys = new Dictionary<RibbonElement, string> { [menu] = "menu", [item] = "item", [field] = "field" };
        var preview = RibbonPopupPreviewRenderer.Render(ribbon, menu, "menu", "", keys);
        using var stream = new MemoryStream(Convert.FromBase64String(preview.Png)); using var image = new Bitmap(stream);
        using var surface = new RibbonPopupSurface(ribbon, menu, new(1800, 16384), menu); Assert.Equal(surface.Size, image.Size);
        Assert.Null(ribbon.Editors.EditorFor(field)); Assert.Equal(0, calls); Assert.Equal(0, host.Committed); Assert.Equal("Retained", field.Value);
    });

    [Fact]
    public void Default_heading_and_scroll_paint_and_actual_container_bounds_match_baseline() => FoundationTests.Sta(() =>
    {
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        foreach (var density in Enum.GetValues<RibbonDensity>())
        foreach (bool office in new[] { false, true })
        foreach (int points in new[] { 9, 14 })
        foreach (bool classic in new[] { false, true })
        foreach (bool empty in new[] { false, true })
        {
            using var font = new Font("Segoe UI", points * dpi / 96f);
            using RibbonRenderer painter = office ? new RibbonOffice2024Renderer() : new RibbonRenderer();
            int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
            int headerHeight = font.Height + S(density == RibbonDensity.Touch ? 16 : 12), bandHeight = S(density == RibbonDensity.Touch ? 28 : 16);
            using var bitmap = new Bitmap(S(360), headerHeight + 2 * bandHeight + S(36)); using var g = Graphics.FromImage(bitmap);
            var theme = (classic ? RibbonThemes.Office2019Light : RibbonThemes.Office2024Light) with { MenuAppearance = empty ? new() : null };
            var context = new RibbonPaintContext(g, new(Point.Empty, bitmap.Size), theme, font, dpi) { Density = density };
            painter.DrawSurface(context, true); painter.DrawPopupFrame(context);
            painter.DrawMenuHeader(context with { Bounds = new(S(4), S(4), S(352), headerHeight) }, "Document options");
            painter.DrawOverflowScrollButton(context with { Bounds = new(S(4), headerHeight + S(8), S(352), bandHeight) }, true, false);
            painter.DrawOverflowScrollButton(context with { Bounds = new(S(4), headerHeight + bandHeight + S(16), S(352), bandHeight), Hot = true }, false, true);
            using var separator = new RibbonMenuSeparatorItem();
            painter.DrawMenuSeparator(context with { Bounds = new(S(4), headerHeight + 2 * bandHeight + S(20), S(352), S(9)) }, separator);
            CheckCapture($"paint-{classic}-{office}-{density}-{dpi}-{points}", bitmap, $"header={headerHeight}; band={bandHeight}");
        }
        foreach (var density in Enum.GetValues<RibbonDensity>())
        foreach (bool office in new[] { false, true })
        foreach (bool classic in new[] { false, true })
        foreach (bool empty in new[] { false, true })
        {
            using RibbonRenderer painter = office ? new RibbonOffice2024Renderer() : new RibbonRenderer();
            using var ribbon = new RibbonControl { Density = density, Renderer = painter, QuickAccessMaxWidth = 28, Theme = (classic ? RibbonThemes.Office2019Light : RibbonThemes.Office2024Light) with { MenuAppearance = empty ? new() : null } };
            var tab = new RibbonTab(); var group = new RibbonGroup(); var menu = new RibbonDropDownItem { DropDownHeader = "Document options" };
            menu.Items.AddRange(new RibbonButtonItem { Text = "Open", MenuDescription = "Keep the document formatting." }, new RibbonTextBoxItem { Text = "Name", Value = "Retained", InputWidth = 50 });
            group.Items.Add(menu); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
            ribbon.QuickAccessItems.AddRange(Enumerable.Range(0, 16).Select(i => new RibbonButtonItem { Text = "Command " + i }).ToArray());
            foreach (bool overflow in new[] { false, true })
            {
                using var surface = new RibbonPopupSurface(ribbon, overflow ? ribbon.QuickAccessOverflow : menu, new(520, 300));
                using var bitmap = new Bitmap(surface.Width, surface.Height); surface.DrawToBitmap(bitmap, surface.ClientRectangle);
                string bounds = $"{surface.Size};{surface.MenuHeaderBounds};{surface.OverflowUpBounds};{surface.OverflowViewport}\n" + string.Join("\n", surface.Targets.Select(t => t.Bounds));
                CheckCapture($"container-{classic}-{office}-{density}-{overflow}", bitmap, bounds);
            }
        }
    });

    private static void CheckCapture(string name, Bitmap bitmap, string bounds)
    {
        if (Environment.GetEnvironmentVariable("RIBBONKIT_MENU_CONTAINER_CAPTURE") is { Length: > 0 } capture)
        {
            Directory.CreateDirectory(capture); bitmap.Save(Path.Combine(capture, name + ".png")); File.WriteAllText(Path.Combine(capture, name + ".txt"), bounds);
        }
        if (Environment.GetEnvironmentVariable("RIBBONKIT_MENU_CONTAINER_BASELINE") is not { Length: > 0 } baseline) return;
        Assert.Equal(File.ReadAllText(Path.Combine(baseline, name + ".txt")), bounds);
        using var expected = new Bitmap(Path.Combine(baseline, name + ".png")); Assert.Equal(expected.Size, bitmap.Size);
        for (int y = 0; y < bitmap.Height; y++) for (int x = 0; x < bitmap.Width; x++) Assert.Equal(expected.GetPixel(x, y), bitmap.GetPixel(x, y));
    }
    private sealed class QuietForm : Form { protected override bool ShowWithoutActivation => true; }
    private sealed class Record { internal Rectangle Text; }
    private sealed class Flat(Record record) : RibbonRenderer
    { public override void DrawText(RibbonPaintContext c, string text, Color color, TextFormatFlags flags) { record.Text = c.Bounds; base.DrawText(c, text, color, flags); } }
    private sealed class Office(Record record) : RibbonOffice2024Renderer
    { public override void DrawText(RibbonPaintContext c, string text, Color color, TextFormatFlags flags) { record.Text = c.Bounds; base.DrawText(c, text, color, flags); } }
}
