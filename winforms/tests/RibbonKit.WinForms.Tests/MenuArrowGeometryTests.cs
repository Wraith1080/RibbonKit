using RibbonKit.WinForms.Layout;
using RibbonKit.WinForms.Design;
using System.ComponentModel;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class MenuArrowGeometryTests
{
    private static RibbonTheme Custom => RibbonThemes.Office2024Light with
    { MenuSubmenuArrowWidth = 44, MenuSplitArrowWidth = 64, MenuChooserArrowWidth = 40, MenuSeparatorHorizontalPadding = 24, MenuMinimumWidth = 520 };

    [Theory] [InlineData(false)] [InlineData(true)]
    public void Menu_arrows_and_caption_ends_share_the_split_column_with_default_and_mixed_role_widths(bool office) => FoundationTests.Sta(() =>
    {
        var record = new Record(); using RibbonRenderer painter = office ? new Office(record) : new Flat(record);
        var errors = new List<string>();
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        foreach (var density in Enum.GetValues<RibbonDensity>())
        foreach (int points in new[] { 9, 14 })
        foreach (var theme in new[] { RibbonThemes.Office2024Light, Custom,
            Custom with { MenuTrailingPadding = 18, MenuLeadingPadding = 14, MenuIconSize = 36, MenuIconTextGap = 16 } })
        {
            using var f = new Fixture(density); f.Ribbon.Theme = theme; using var font = new Font("Segoe UI", points * dpi / 96f);
            int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
            using var pixels = new Bitmap(S(450), S(100)); using var graphics = Graphics.FromImage(pixels);
            var row = new Rectangle(S(4), S(4), S(440), S(90));
            var context = new RibbonPaintContext(graphics, row, theme, font, dpi) { Density = density };
            var parts = RibbonSplitLayout.Calculate(row, f.Split, RibbonItemSize.Medium, true, dpi, font);
            painter.DrawSplitItem(context with { Bounds = parts.Primary }, context with { Bounds = parts.DropDown }, f.Split, RibbonItemSize.Medium, true);
            double center = record.Arrow.Left + record.Arrow.Width / 2d; int captionRight = record.Text[f.Split.Text].Right;
            painter.DrawItem(context, f.Drop, RibbonItemSize.Medium, true); Check("submenu", f.Drop.Text);
            foreach (var chooser in new RibbonElement[] { f.MenuReference, f.ComboReference, f.GroupReference })
            { painter.DrawQuickAccessItem(context, chooser, true); Check(chooser.GetType().Name, RibbonQuickAccess.Caption(chooser)); }
            var target = new RibbonTarget(TargetKind.QuickAccess, f.SplitReference, row, row, RibbonItemSize.Medium);
            var qa = RibbonSplitLayout.Expand(target, dpi, true).ToArray();
            painter.DrawQuickAccessSplitItem(context with { Bounds = qa[0].Bounds }, context with { Bounds = qa[1].Bounds }, f.SplitReference, true);
            Check("QAT split", f.Split.Text); Assert.Equal(parts.DropDown, qa[1].Bounds);

            void Check(string kind, string caption)
            {
                if (record.Arrow.Left + record.Arrow.Width / 2d != center || record.Text[caption].Right != captionRight)
                    errors.Add($"{kind}: {density}, {dpi} DPI, custom={theme.MenuSplitArrowWidth.HasValue}, arrow={record.Arrow}, captionRight={record.Text[caption].Right}; expected center={center}, captionRight={captionRight}");
            }
        }
        Assert.True(errors.Count == 0, string.Join(Environment.NewLine, errors));
    });

    [Fact]
    public void Shared_column_keeps_narrow_split_targets_and_larger_caption_reservations_inside_the_menu_contract() => FoundationTests.Sta(() =>
    {
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        foreach (var density in Enum.GetValues<RibbonDensity>())
        {
            using var f = new Fixture(density); f.Ribbon.Theme = Custom with { MenuSubmenuArrowWidth = 160, MenuChooserArrowWidth = 192 };
            foreach (int width in new[] { 0, 1, 2, 12, 30, 440 })
            {
                var row = new Rectangle(4, 4, RibbonLayoutEngine.Scale(width, dpi), 90);
                var column = RibbonMenuLayout.ArrowColumn(row, dpi, density, f.Ribbon.Theme);
                var split = RibbonSplitLayout.Calculate(row, f.Split, RibbonItemSize.Medium, true, dpi);
                var qat = RibbonSplitLayout.Expand(new(TargetKind.QuickAccess, f.SplitReference, row, row, RibbonItemSize.Medium), dpi, true).ToArray();
                Assert.Equal(split.DropDown, column); Assert.Equal(qat[1].Bounds, column);
                Assert.InRange(column.Width, 0, row.Width); Assert.Equal(row.Right, column.Right); Assert.True(split.Primary.Width >= 0);
                Assert.True(RibbonMenuLayout.ArrowCaptionReservation(row.Width, dpi, density, f.Ribbon.Theme, false) >= column.Width);
                Assert.True(RibbonMenuLayout.ArrowCaptionReservation(row.Width, dpi, density, f.Ribbon.Theme, true) >= column.Width);
            }
            Assert.Equal(RibbonLayoutEngine.Scale(160, dpi), RibbonMenuLayout.ArrowCaptionReservation(1000, dpi, density, f.Ribbon.Theme, false));
            Assert.Equal(RibbonLayoutEngine.Scale(192, dpi), RibbonMenuLayout.ArrowCaptionReservation(1000, dpi, density, f.Ribbon.Theme, true));
        }
    });

    [Theory] [InlineData(false)] [InlineData(true)]
    public void Overrides_share_reserved_caption_arrow_and_split_regions_at_every_density_and_dpi(bool office) => FoundationTests.Sta(() =>
    {
        var record = new Record(); using RibbonRenderer painter = office ? new Office(record) : new Flat(record);
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        foreach (var density in Enum.GetValues<RibbonDensity>())
        foreach (int points in new[] { 9, 14 })
        {
            using var f = new Fixture(density); f.Ribbon.Theme = Custom; using var font = new Font("Segoe UI", points * dpi / 96f);
            int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
            var row = new Rectangle(S(4), S(4), S(440), S(90));
            using var pixels = new Bitmap(S(450), S(100)); using var graphics = Graphics.FromImage(pixels);
            var context = new RibbonPaintContext(graphics, row, Custom, font, dpi) { Density = density, Hot = true };
            painter.DrawItem(context, f.Drop, RibbonItemSize.Medium, true);
            Assert.Equal(S(64), record.Arrow.Width); Assert.False(record.Text[f.Drop.Text].IntersectsWith(record.Arrow));
            var parts = RibbonSplitLayout.Calculate(row, f.Split, RibbonItemSize.Medium, true, dpi, font);
            Assert.Equal(S(64), parts.DropDown.Width); Assert.Equal(row, Rectangle.Union(parts.Primary, parts.DropDown)); Assert.False(parts.Primary.IntersectsWith(parts.DropDown));
            painter.DrawSplitItem(context with { Bounds = parts.Primary }, context with { Bounds = parts.DropDown }, f.Split, RibbonItemSize.Medium, true);
            Assert.Equal(parts.DropDown, record.Arrow);
            foreach (var chooser in new RibbonElement[] { f.MenuReference, f.ComboReference, f.GroupReference })
            {
                painter.DrawQuickAccessItem(context, chooser, true); Assert.Equal(S(64), record.Arrow.Width);
            }
            var target = new RibbonTarget(TargetKind.QuickAccess, f.SplitReference, row, row, RibbonItemSize.Medium);
            var targets = RibbonSplitLayout.Expand(target, dpi, true).ToArray(); Assert.Equal(S(64), targets[1].Bounds.Width);
            painter.DrawQuickAccessSplitItem(context with { Bounds = targets[0].Bounds }, context with { Bounds = targets[1].Bounds }, f.SplitReference, true); Assert.Equal(targets[1].Bounds, record.Arrow);
            var narrow = RibbonSplitLayout.Calculate(row with { Width = S(30) }, f.Split, RibbonItemSize.Medium, true, dpi);
            Assert.Equal(S(30) / 3, narrow.DropDown.Width); Assert.True(narrow.Primary.Width > 0);
            var stripBefore = RibbonSplitLayout.Calculate(row, f.Split, RibbonItemSize.Medium, false, dpi);
            f.Ribbon.Theme = RibbonThemes.Office2024Light; Assert.Equal(stripBefore, RibbonSplitLayout.Calculate(row, f.Split, RibbonItemSize.Medium, false, dpi));
            f.Ribbon.Theme = Custom with { MenuSplitArrowWidth = 1 };
            Assert.Equal(S(density == RibbonDensity.Touch ? 28 : 1), RibbonSplitLayout.Calculate(row, f.Split, RibbonItemSize.Medium, true, dpi).DropDown.Width);
        }
    });

    [Theory] [InlineData(0)] [InlineData(1)] [InlineData(2)]
    public void Popup_split_geometry_matches_keyboard_selection_and_uia_bounds_for_menu_qat_and_more(int kind) => FoundationTests.Sta(() =>
    {
        using var form = new Form { Width = 1200, Height = 700 }; using var f = new Fixture(RibbonDensity.Compact); form.Controls.Add(f.Ribbon);
        f.Ribbon.Theme = Custom; f.Ribbon.QuickAccessMaxWidth = 28;
        if (kind == 2)
        {
            ((RibbonDropDownItem)f.Split.Parent!).Items.Remove(f.Split); f.Group.Items.Add(f.Split);
            f.Split.SimplifiedPlacement = RibbonSimplifiedPlacement.Inline; f.Split.SimplifiedPriority = -1000;
            f.Group.Items.AddRange(Enumerable.Range(0, 15).Select(i => new RibbonButtonItem { Text = "Long command " + i, SimplifiedPlacement = RibbonSimplifiedPlacement.Inline }).ToArray());
            f.Ribbon.LayoutMode = RibbonLayoutMode.Simplified; f.Ribbon.Width = 240;
        }
        form.Show(); Application.DoEvents(); if (kind == 2) f.Ribbon.Width = 240; _ = f.Ribbon.Snapshot;
        var owner = kind == 0 ? (RibbonElement)f.Split.Parent! : kind == 1 ? f.Ribbon.QuickAccessOverflow : f.Ribbon.Simplified.More;
        using var popup = new RibbonPopupSurface(f.Ribbon, owner, new(1000, 650)); form.Controls.Add(popup); popup.CreateControl();
        var parts = popup.Targets.Where(t => ReferenceEquals(t.Element, f.Split) || t.Element is RibbonQuickAccessDropDownReference r && ReferenceEquals(r.Source, f.Split)).ToArray();
        Assert.Equal(2, parts.Length); Assert.Equal(RibbonLayoutEngine.Scale(64, f.Ribbon.DeviceDpi), parts[1].Bounds.Width);
        Assert.Equal(parts[0].Bounds.Right, parts[1].Bounds.Left); Assert.False(parts[0].Bounds.IntersectsWith(parts[1].Bounds));
        popup.FocusElement(parts[1].Element!, true); Assert.True(popup.FocusedDropDownPart);
        popup.FocusElement(parts[0].Element!, false); Assert.False(popup.FocusedDropDownPart);
        // Existing UIA exposes the command's primary target; changing the
        // secondary width must update that same target, not invent a new node.
        var screen = popup.RectangleToScreen(parts[0].VisibleBounds);
        var node = popup.AutomationRoot.Node(parts[0].Element!); var bounds = node.BoundingRectangle;
        Assert.Equal(screen.Left, bounds.Left); Assert.Equal(screen.Top, bounds.Top); Assert.Equal(screen.Width, bounds.Width); Assert.Equal(screen.Height, bounds.Height);
    });

    [Fact]
    public void Separator_insets_change_only_ordinary_lines_and_invalid_themes_remain_atomic() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { Theme = Custom }; using var other = new RibbonControl();
        foreach (var invalid in new[] { Custom with { MenuSubmenuArrowWidth = 0 }, Custom with { MenuSubmenuArrowWidth = 1025 },
            Custom with { MenuSplitArrowWidth = 0 }, Custom with { MenuSplitArrowWidth = 1025 }, Custom with { MenuChooserArrowWidth = 0 },
            Custom with { MenuChooserArrowWidth = 1025 }, Custom with { MenuSeparatorHorizontalPadding = -1 }, Custom with { MenuSeparatorHorizontalPadding = 1025 } })
        { Assert.Throws<ArgumentOutOfRangeException>(() => ribbon.Theme = invalid); Assert.Equal(Custom, ribbon.Theme); }
        ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2024Light; TypeDescriptor.GetProperties(ribbon)[nameof(ribbon.BuiltInTheme)]!.ResetValue(ribbon); Assert.Equal(Custom, ribbon.Theme);
        ribbon.Theme = Custom with { MenuSubmenuArrowWidth = null, MenuSplitArrowWidth = null, MenuChooserArrowWidth = null, MenuSeparatorHorizontalPadding = null };
        Assert.Equal(RibbonThemes.Office2024Light with { MenuMinimumWidth = 520 }, ribbon.Theme); Assert.Same(RibbonThemes.Office2024Light, other.Theme);
        using var separator = new RibbonMenuSeparatorItem(); using var painter = new RibbonRenderer();
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        {
            int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
            using var pixels = new Bitmap(S(180), S(9)); using var graphics = Graphics.FromImage(pixels); graphics.Clear(Color.White);
            var context = new RibbonPaintContext(graphics, new(Point.Empty, pixels.Size), Custom, ribbon.Font, dpi);
            painter.DrawMenuSeparator(context, separator); int y = (pixels.Height - 1) / 2;
            Assert.Equal(Color.White.ToArgb(), pixels.GetPixel(S(24) - 1, y).ToArgb()); Assert.Equal(Custom.Border.ToArgb(), pixels.GetPixel(S(24), y).ToArgb());
            graphics.Clear(Color.White); painter.DrawMenuSeparator(context with { ApplicationMenuPart = RibbonApplicationMenuPart.Pane }, separator);
            Assert.Equal(Custom.Border.ToArgb(), pixels.GetPixel(S(8), y).ToArgb());
            graphics.Clear(Color.White); painter.DrawMenuSeparator(context with { Theme = Custom with { MenuSeparatorHorizontalPadding = 1024 } }, separator);
            Assert.Equal(Color.White.ToArgb(), pixels.GetPixel(pixels.Width / 2, y).ToArgb());
        }
    });

    [Fact]
    public void Chooser_description_budgets_and_inert_preview_hit_regions_use_their_own_arrow_roles() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(RibbonDensity.Compact); f.Ribbon.Theme = Custom;
        f.Drop.MenuDescription = "A short line"; int width = 240;
        var ordinary = RibbonMenuLayout.RowHeight(f.Drop, width, f.Ribbon.Font, 96, theme: Custom);
        var chooser = RibbonMenuLayout.RowHeight(f.Drop, width, f.Ribbon.Font, 96, theme: Custom, quickAccess: true);
        Assert.Equal(ordinary, RibbonMenuLayout.RowHeight(f.Drop, width, f.Ribbon.Font, 96, theme: Custom with { MenuChooserArrowWidth = 1000 }));
        Assert.Equal(chooser, RibbonMenuLayout.RowHeight(f.Drop, width, f.Ribbon.Font, 96, theme: Custom with { MenuSubmenuArrowWidth = 1000 }, quickAccess: true));
        Assert.True(RibbonMenuLayout.RowHeight(f.Drop, width, f.Ribbon.Font, 96, theme: Custom with { MenuSubmenuArrowWidth = 1000 }) > ordinary);
        Assert.True(RibbonMenuLayout.RowHeight(f.Drop, width, f.Ribbon.Font, 96, theme: Custom with { MenuChooserArrowWidth = 1000 }, quickAccess: true) > chooser);
        var root = (RibbonDropDownItem)f.Split.Parent!;
        var keys = root.Items.ToDictionary(i => (RibbonElement)i, i => i.Text); keys[root] = "root";
        var preview = RibbonPopupPreviewRenderer.Render(f.Ribbon, root, "root", "", keys);
        var hit = Assert.Single(preview.Hits, h => h.Key == f.Drop.Text && h.OpensSubmenu);
        Assert.Equal(RibbonLayoutEngine.Scale(64, f.Ribbon.DeviceDpi), hit.Width);
        var split = Assert.Single(preview.Hits, h => h.Key == f.Split.Text && h.OpensSubmenu); Assert.Equal(RibbonLayoutEngine.Scale(64, f.Ribbon.DeviceDpi), split.Width);
        Assert.False(f.Drop.IsDropDownOpen); Assert.Null(f.Ribbon.Popups.Top);
    });

    [Fact]
    public void Default_menu_arrow_and_separator_paint_and_split_bounds_match_baseline() => FoundationTests.Sta(() =>
    {
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        foreach (var density in Enum.GetValues<RibbonDensity>())
        foreach (bool office in new[] { false, true })
        foreach (int points in new[] { 9, 14 })
        {
            using var font = new Font("Segoe UI", points * dpi / 96f); using var f = new Fixture(density);
            using RibbonRenderer painter = office ? new RibbonOffice2024Renderer() : new RibbonRenderer();
            int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
            using var bitmap = new Bitmap(S(400), S(448)); using var graphics = Graphics.FromImage(bitmap);
            var context = new RibbonPaintContext(graphics, new(Point.Empty, bitmap.Size), f.Ribbon.Theme, font, dpi) { Density = density };
            painter.DrawSurface(context, true); painter.DrawPopupFrame(context);
            var row = new Rectangle(S(4), S(4), S(392), S(60)); var bounds = new List<string>();
            painter.DrawItem(context with { Bounds = row, Hot = true }, f.Drop, RibbonItemSize.Medium, true); row.Y += S(64);
            var parts = RibbonSplitLayout.Calculate(row, f.Split, RibbonItemSize.Medium, true, dpi, font); bounds.Add($"{parts}");
            painter.DrawSplitItem(context with { Bounds = parts.Primary }, context with { Bounds = parts.DropDown, Hot = true }, f.Split, RibbonItemSize.Medium, true); row.Y += S(64);
            foreach (var chooser in new RibbonElement[] { f.MenuReference, f.ComboReference, f.GroupReference })
            {
                painter.DrawQuickAccessItem(context with { Bounds = row, Hot = true }, chooser, true); row.Y += S(64);
            }
            var target = new RibbonTarget(TargetKind.QuickAccess, f.SplitReference, row, context.Bounds, RibbonItemSize.Medium);
            var targets = RibbonSplitLayout.Expand(target, dpi, true).ToArray(); bounds.Add(string.Join(";", targets.Select(t => t.Bounds)));
            painter.DrawQuickAccessSplitItem(context with { Bounds = targets[0].Bounds }, context with { Bounds = targets[1].Bounds, Hot = true }, f.SplitReference, true); row.Y += S(64);
            painter.DrawMenuSeparator(context with { Bounds = row with { Height = S(9) } }, f.Separator);
            string name = $"arrows-{office}-{density}-{dpi}-{points}", measurement = string.Join("\n", bounds);
            if (Environment.GetEnvironmentVariable("RIBBONKIT_MENU_ARROW_CAPTURE") is { Length: > 0 } capture)
            { Directory.CreateDirectory(capture); bitmap.Save(Path.Combine(capture, name + ".png")); File.WriteAllText(Path.Combine(capture, name + ".txt"), measurement); }
            if (Environment.GetEnvironmentVariable("RIBBONKIT_MENU_ARROW_BASELINE") is not { Length: > 0 } baseline) continue;
            Assert.Equal(File.ReadAllText(Path.Combine(baseline, name + ".txt")), measurement);
            using var expected = new Bitmap(Path.Combine(baseline, name + ".png")); Assert.Equal(expected.Size, bitmap.Size);
            for (int y = 0; y < bitmap.Height; y++) for (int x = 0; x < bitmap.Width; x++) Assert.Equal(expected.GetPixel(x, y), bitmap.GetPixel(x, y));
        }
    });

    private sealed class Fixture : IDisposable
    {
        internal RibbonControl Ribbon { get; }
        internal RibbonGroup Group { get; } = new() { Text = "Commands" };
        internal RibbonDropDownItem Drop { get; } = new() { Text = "Export", MenuDescription = "Choose a document format.", Icon = RibbonStockIcons.Create("open") };
        internal RibbonSplitButtonItem Split { get; } = new() { Text = "Paste", CheckOnClick = true };
        internal RibbonComboBoxItem Combo { get; } = new() { Text = "Font family" };
        internal RibbonMenuSeparatorItem Separator { get; } = new();
        internal RibbonQuickAccessDropDownReference MenuReference { get; }
        internal RibbonQuickAccessDropDownReference SplitReference { get; }
        internal RibbonQuickAccessComboBoxReference ComboReference { get; }
        internal RibbonQuickAccessGroupReference GroupReference { get; }
        internal Fixture(RibbonDensity density)
        {
            Ribbon = new() { Density = density, Width = 1200 };
            Drop.Items.Add(new RibbonButtonItem { Text = "PDF" }); Split.Items.Add(new RibbonButtonItem { Text = "Text only" });
            var menu = new RibbonDropDownItem(); menu.Items.AddRange(Drop, Split, Separator); Group.Items.AddRange(menu, Combo);
            var tab = new RibbonTab(); tab.Groups.Add(Group); Ribbon.Tabs.Add(tab);
            MenuReference = new() { Source = Drop }; SplitReference = new() { Source = Split }; ComboReference = new() { Source = Combo }; GroupReference = new() { Source = Group };
            Ribbon.QuickAccessItems.AddRange(MenuReference, SplitReference, ComboReference, GroupReference);
        }
        public void Dispose() => Ribbon.Dispose();
    }
    private sealed class Record { internal Rectangle Arrow; internal Dictionary<string, Rectangle> Text { get; } = []; }
    private sealed class Flat(Record record) : RibbonRenderer
    {
        public override void DrawText(RibbonPaintContext c, string text, Color color, TextFormatFlags flags) { record.Text[text] = c.Bounds; base.DrawText(c, text, color, flags); }
        public override void DrawSubmenuArrow(RibbonPaintContext c, Color color) { record.Arrow = c.Bounds; base.DrawSubmenuArrow(c, color); }
    }
    private sealed class Office(Record record) : RibbonOffice2024Renderer
    {
        public override void DrawText(RibbonPaintContext c, string text, Color color, TextFormatFlags flags) { record.Text[text] = c.Bounds; base.DrawText(c, text, color, flags); }
        public override void DrawSubmenuArrow(RibbonPaintContext c, Color color) { record.Arrow = c.Bounds; base.DrawSubmenuArrow(c, color); }
    }
}
