using RibbonKit.WinForms.Layout;
using RibbonKit.WinForms.Design;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class IllustratedMenuGeometryTests
{
    private static RibbonTheme Roomier => RibbonThemes.Office2024Light with
    { IllustratedMenuMinimumWidth = 540, IllustratedMenuMinimumRowHeight = 88, IllustratedMenuIconSize = 48,
        IllustratedMenuLeadingPadding = 18, IllustratedMenuTrailingPadding = 22, IllustratedMenuIconTextGap = 20, IllustratedMenuDescriptionGap = 8 };

    [Theory] [InlineData(false)] [InlineData(true)]
    public void Default_illustrated_measurement_and_paint_remain_identical(bool office) => FoundationTests.Sta(() =>
    {
        var record = new Record(); using RibbonRenderer painter = office ? new Office(record) : new Flat(record);
        foreach (var density in Enum.GetValues<RibbonDensity>())
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        foreach (int points in new[] { 9, 14 })
        {
            using var ribbon = new RibbonControl { Density = density };
            var tab = new RibbonTab(); var group = new RibbonGroup(); var menu = new RibbonDropDownItem { MenuPresentation = RibbonMenuPresentation.IllustratedChoices };
            menu.Items.AddRange(new RibbonButtonItem { Text = "Keep source formatting", MenuDescription = "Keep the original font and paragraph spacing.", Icon = RibbonStockIcons.Create("open") },
                new RibbonToggleItem { Text = "Match destination formatting", MenuDescription = "Use the destination document formatting.", Checked = true, Icon = RibbonStockIcons.Create("open") },
                new RibbonMenuSeparatorItem(), new RibbonButtonItem { Text = "Text only", Icon = RibbonStockIcons.Create("open") },
                new RibbonButtonItem { Text = "Unavailable", Enabled = false, MenuDescription = "This choice is unavailable.", Icon = RibbonStockIcons.Create("open") });
            group.Items.Add(menu); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
            using var font = new Font("Segoe UI", points * dpi / 96f);
            int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
            var heights = RibbonMenuLayout.RowHeights(menu.Items.ToArray(), S(420), font, dpi, density, theme: ribbon.Theme);
            using var pixels = new Bitmap(S(428), heights.Sum() + S(8)); using var graphics = Graphics.FromImage(pixels);
            var context = new RibbonPaintContext(graphics, new(Point.Empty, pixels.Size), ribbon.Theme, font, dpi) { Density = density };
            painter.DrawSurface(context, true); painter.DrawPopupFrame(context);
            var data = new List<string>(); int y = S(4);
            for (int i = 0; i < menu.Items.Count; i++)
            {
                var item = menu.Items[i]; var row = new Rectangle(S(4), y, S(420), heights[i]); y += row.Height;
                record.Text.Clear(); record.Image = Rectangle.Empty;
                painter.DrawItem(context with { Bounds = row, Hot = i == 0 }, item, RibbonItemSize.Medium, true);
                data.Add($"{i}:{row};icon={record.Image};" + string.Join(";", record.Text.Select(p => $"{p.Key}={p.Value}")));
            }
            string name = $"illustrated-{office}-{density}-{points}-{dpi}";
            using var encoded = new MemoryStream(); pixels.Save(encoded, System.Drawing.Imaging.ImageFormat.Png); byte[] png = encoded.ToArray();
            if (Environment.GetEnvironmentVariable("RIBBONKIT_ILLUSTRATED_CAPTURE") is { Length: > 0 } capture)
            {
                Directory.CreateDirectory(capture); File.WriteAllBytes(Path.Combine(capture, name + ".png"), png); File.WriteAllLines(Path.Combine(capture, name + ".txt"), data);
            }
            if (Environment.GetEnvironmentVariable("RIBBONKIT_ILLUSTRATED_BASELINE") is { Length: > 0 } baseline)
            {
                Assert.Equal(File.ReadAllLines(Path.Combine(baseline, name + ".txt")), data);
                Assert.Equal(File.ReadAllBytes(Path.Combine(baseline, name + ".png")), png);
            }
        }
    });

    [Theory] [InlineData(false)] [InlineData(true)]
    public void Overrides_share_artwork_title_description_and_row_measurements(bool office) => FoundationTests.Sta(() =>
    {
        var record = new Record(); using RibbonRenderer painter = office ? new Office(record) : new Flat(record);
        using var f = new Fixture();
        foreach (var density in Enum.GetValues<RibbonDensity>())
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        foreach (int points in new[] { 9, 14 })
        foreach (var theme in new[] { Roomier, Roomier with { IllustratedMenuMinimumRowHeight = 1, IllustratedMenuIconSize = 1, IllustratedMenuDescriptionGap = 0 },
            Roomier with { IllustratedMenuMinimumRowHeight = 1, IllustratedMenuIconSize = 72, IllustratedMenuDescriptionGap = 15 } })
        {
            f.Ribbon.Density = density; f.Ribbon.Theme = theme; using var font = new Font("Segoe UI", points * dpi / 96f);
            int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
            int height = RibbonMenuLayout.RowHeight(f.Choice, S(540), font, dpi, density, theme: theme);
            int expected = Math.Max(S(Math.Max(theme.IllustratedMenuMinimumRowHeight!.Value, density == RibbonDensity.Touch ? 44 : 1)),
                Math.Max(S(theme.IllustratedMenuIconSize!.Value) + S(12), 3 * font.Height + S(12) + S(theme.IllustratedMenuDescriptionGap!.Value) - S(2)));
            Assert.Equal(expected, height);
            var row = new Rectangle(S(4), S(4), S(532), height);
            using var pixels = new Bitmap(S(540), height + S(8)); using var graphics = Graphics.FromImage(pixels);
            var context = new RibbonPaintContext(graphics, row, theme, font, dpi) { Density = density, Hot = true };
            painter.DrawItem(context, f.Choice, RibbonItemSize.Medium, true);
            Assert.Equal(new Size(S(theme.IllustratedMenuIconSize.Value), S(theme.IllustratedMenuIconSize.Value)), record.Image.Size);
            Assert.Equal(row.Left + S(18), record.Image.Left); Assert.True(row.Contains(record.Image));
            var title = record.Text[f.Choice.Text]; var description = record.Text[f.Choice.MenuDescription];
            Assert.Equal(record.Image.Right + S(20), title.Left); Assert.Equal(row.Right - S(22), title.Right);
            Assert.Equal(title.Left, description.Left); Assert.Equal(title.Bottom + S(theme.IllustratedMenuDescriptionGap.Value), description.Top);
            Assert.Equal(2 * font.Height, description.Height); Assert.True(row.Contains(description)); Assert.True(record.Bold[f.Choice.Text]); Assert.False(record.Bold[f.Choice.MenuDescription]);
            Assert.False(record.Image.IntersectsWith(title)); Assert.False(record.Image.IntersectsWith(description));
            int[] heights = RibbonMenuLayout.RowHeights(f.Menu.Items.ToArray(), S(532), font, dpi, density, theme: theme);
            Assert.All(heights.Where((_, i) => f.Menu.Items[i] is not RibbonMenuSeparatorItem), h => Assert.Equal(heights[0], h));
            Assert.Equal(S(9), heights[2]);
            if (theme == Roomier && Environment.GetEnvironmentVariable("RIBBONKIT_ILLUSTRATED_CAPTURE") is { Length: > 0 } capture)
            { Directory.CreateDirectory(capture); pixels.Save(Path.Combine(capture, $"override-{office}-{density}-{points}-{dpi}.png")); }
        }
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        foreach (int width in new[] { 0, 1, 20 })
        {
            var row = new Rectangle(0, 0, width, 20);
            var content = RibbonMenuLayout.IllustratedContent(row, f.Ribbon.Font, dpi, true, Roomier with { IllustratedMenuLeadingPadding = 1024, IllustratedMenuTrailingPadding = 1024 });
            Assert.InRange(content.Icon.Left, 0, width); Assert.InRange(content.Text.Left, 0, width);
            Assert.Equal(0, content.Icon.Width); Assert.Equal(0, content.Text.Width); Assert.True(row.Contains(content.Text));
        }
    });

    [Theory]
    [InlineData(nameof(RibbonTheme.IllustratedMenuMinimumWidth), true)] [InlineData(nameof(RibbonTheme.IllustratedMenuMinimumRowHeight), true)]
    [InlineData(nameof(RibbonTheme.IllustratedMenuIconSize), true)] [InlineData(nameof(RibbonTheme.IllustratedMenuLeadingPadding), false)]
    [InlineData(nameof(RibbonTheme.IllustratedMenuTrailingPadding), false)] [InlineData(nameof(RibbonTheme.IllustratedMenuIconTextGap), false)]
    [InlineData(nameof(RibbonTheme.IllustratedMenuDescriptionGap), false)]
    public void Each_role_validates_atomically_and_resets_independently(string name, bool positive) => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl(); using var other = new RibbonControl(); var original = ribbon.Theme; var builtIn = ribbon.BuiltInTheme;
        var property = typeof(RibbonTheme).GetProperty(name)!;
        foreach (int value in new[] { positive ? 0 : -1, 1025 })
        {
            var invalid = original with { }; property.SetValue(invalid, value);
            Assert.Throws<ArgumentOutOfRangeException>(() => ribbon.Theme = invalid); Assert.Same(original, ribbon.Theme); Assert.Equal(builtIn, ribbon.BuiltInTheme);
        }
        foreach (int value in new[] { positive ? 1 : 0, 1024 })
        {
            var valid = original with { }; property.SetValue(valid, value); ribbon.Theme = valid; Assert.Equal(value, property.GetValue(ribbon.Theme));
            var reset = valid with { }; property.SetValue(reset, null); ribbon.Theme = reset; Assert.Equal(original, ribbon.Theme); Assert.Same(original, other.Theme);
        }
        ribbon.Theme = Roomier; ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2024Light; Assert.Equal(original, ribbon.Theme);
    });

    [Theory] [InlineData(0)] [InlineData(1)] [InlineData(2)]
    public void Source_qat_and_nested_menus_share_geometry_preview_uia_and_scroll_reveal(int kind) => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); using var form = new Form { Width = 1200, Height = 700 }; form.Controls.Add(f.Ribbon);
        if (kind == 2) { f.Group.Items.Remove(f.Menu); var outer = new RibbonDropDownItem(); outer.Items.Add(f.Menu); f.Group.Items.Add(outer); }
        for (int i = 0; i < 8; i++) f.Menu.Items.Add(new RibbonButtonItem { Text = "Choice " + i, MenuDescription = "A longer description for scroll/reveal.", Icon = RibbonStockIcons.Create("open") });
        form.Show(); Application.DoEvents();
        foreach (var density in Enum.GetValues<RibbonDensity>())
        {
            f.Ribbon.Density = density; f.Ribbon.Theme = Roomier; Application.DoEvents();
            RibbonElement owner = kind == 1 ? f.MenuReference : f.Menu;
            using var surface = new RibbonPopupSurface(f.Ribbon, owner, new(1000, 280)); form.Controls.Add(surface); surface.CreateControl();
            int S(int n) => RibbonLayoutEngine.Scale(n, f.Ribbon.DeviceDpi);
            Assert.True(surface.Width >= S(540)); Assert.InRange(surface.Height, 1, 280);
            Assert.All(surface.Targets.Where(t => t.Element is not RibbonMenuSeparatorItem), t => Assert.True(t.Bounds.Height >= S(88)));
            var last = f.Menu.Items.Last(); surface.FocusElement(last, enterEditor: false);
            var target = surface.Targets.Single(t => t.Element == last); Assert.Equal(target.Bounds, target.VisibleBounds);
            var node = surface.AutomationRoot.Node(last); var bounds = node.BoundingRectangle; var screen = surface.RectangleToScreen(target.VisibleBounds);
            Assert.Equal(screen.Left, bounds.Left); Assert.Equal(screen.Top, bounds.Top); Assert.Equal(screen.Width, bounds.Width); Assert.Equal(screen.Height, bounds.Height);
            Assert.Same(node, surface.AutomationRoot.ElementProviderFromPoint(screen.Left + screen.Width / 2, screen.Top + screen.Height / 2));
            var keys = f.Menu.Items.ToDictionary(i => (RibbonElement)i, i => i.Text); keys[f.Menu] = "menu";
            var record = new Record(); using var painter = new Office(record); f.Ribbon.Renderer = painter;
            var preview = RibbonPopupPreviewRenderer.Render(f.Ribbon, f.Menu, "menu", "", keys);
            Assert.NotEmpty(preview.Png); Assert.Equal(S(48), record.Image.Width); Assert.False(f.Menu.IsDropDownOpen);
            if (Environment.GetEnvironmentVariable("RIBBONKIT_ILLUSTRATED_CAPTURE") is { Length: > 0 } capture)
            { Directory.CreateDirectory(capture); File.WriteAllBytes(Path.Combine(capture, $"preview-{kind}-{density}.png"), Convert.FromBase64String(preview.Png)); }
            f.Ribbon.Renderer = null;
            using var constrained = new RibbonPopupSurface(f.Ribbon, owner, new(160, 180));
            Assert.InRange(constrained.Width, 1, 160); Assert.InRange(constrained.Height, 1, 180);
        }
        form.Close();
    });

    [Theory] [InlineData(false)] [InlineData(true)]
    public void Standard_qat_projection_and_application_menu_paint_retain_their_geometry(bool office) => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); using RibbonRenderer painter = office ? new RibbonOffice2024Renderer() : new RibbonRenderer();
        var ordinary = new RibbonButtonItem { Text = "Ordinary", MenuDescription = "A standard description.", Icon = RibbonStockIcons.Create("open") };
        var standard = new RibbonDropDownItem(); standard.Items.Add(ordinary); f.Group.Items.Add(standard);
        foreach (var density in Enum.GetValues<RibbonDensity>())
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        foreach (int layer in new[] { 0, 1, 2 })
        {
            f.Ribbon.Density = density;
            byte[] Draw(RibbonTheme theme)
            {
                f.Ribbon.Theme = theme; int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
                using var pixels = new Bitmap(S(360), S(100)); using var g = Graphics.FromImage(pixels); g.Clear(Color.White);
                var context = new RibbonPaintContext(g, new(0, 0, pixels.Width, pixels.Height), theme, f.Ribbon.Font, dpi) { Density = density, Hot = true };
                if (layer == 0) painter.DrawItem(context, ordinary, RibbonItemSize.Medium, true);
                else if (layer == 1) painter.DrawQuickAccessItem(context, f.ChoiceReference, true);
                else painter.DrawItem(context with { ApplicationMenuPart = RibbonApplicationMenuPart.Pane }, f.Choice, RibbonItemSize.Medium, true);
                using var stream = new MemoryStream(); pixels.Save(stream, System.Drawing.Imaging.ImageFormat.Png); return stream.ToArray();
            }
            Assert.Equal(Draw(RibbonThemes.Office2024Light), Draw(Roomier));
        }
        f.Ribbon.Theme = RibbonThemes.Office2024Light; using var before = new RibbonPopupSurface(f.Ribbon, standard, new(1000, 1000));
        f.Ribbon.Theme = Roomier; using var after = new RibbonPopupSurface(f.Ribbon, standard, new(1000, 1000)); Assert.Equal(before.Size, after.Size);
    });

    [Fact]
    public void Empty_constrained_artwork_slots_do_not_invoke_application_icons() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); var icon = new CountIcon(); f.Choice.Icon = icon;
        var theme = Roomier with { IllustratedMenuLeadingPadding = 1024, IllustratedMenuTrailingPadding = 1024 };
        using var painter = new RibbonOffice2024Renderer(); using var pixels = new Bitmap(160, 88); using var graphics = Graphics.FromImage(pixels);
        painter.DrawItem(new(graphics, new(0, 0, 160, 88), theme, f.Ribbon.Font, 96), f.Choice, RibbonItemSize.Medium, true);
        Assert.Equal(0, icon.Calls);
    });
    private sealed class CountIcon : IRibbonIcon
    {
        internal int Calls;
        public void Draw(Graphics graphics, Rectangle bounds, RibbonIconContext context) => Calls++;
    }

    [Fact]
    public void Plain_touch_choices_keep_the_target_floor_and_large_artwork_expands_desired_width() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture();
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        {
            using var font = new Font("Segoe UI", 9 * dpi / 96f);
            var small = Roomier with { IllustratedMenuMinimumRowHeight = 1, IllustratedMenuIconSize = 1 };
            Assert.Equal(Math.Max(RibbonLayoutEngine.Scale(44, dpi), font.Height + RibbonLayoutEngine.Scale(12, dpi)),
                RibbonMenuLayout.RowHeight(f.Menu.Items[1], 600, font, dpi, RibbonDensity.Touch, theme: small));
        }
        f.Ribbon.Theme = Roomier with { IllustratedMenuMinimumWidth = 1, IllustratedMenuIconSize = 400 };
        using var surface = new RibbonPopupSurface(f.Ribbon, f.Menu, new(2000, 2000));
        int S(int n) => RibbonLayoutEngine.Scale(n, f.Ribbon.DeviceDpi);
        Assert.True(surface.Width >= S(400) + S(18) + S(20) + S(22) + S(8));
    });

    [Fact]
    public void Theme_invalidation_retains_mixed_native_editor_and_original_menu_models() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); using var form = new Form { Width = 1200, Height = 750 }; form.Controls.Add(f.Ribbon);
        var text = new RibbonTextBoxItem { Text = "Name", Value = "Retained", FillMenuWidth = true }; f.Menu.Items.Add(text);
        var original = f.Ribbon.Theme with { EditorMinimumHeight = 54, EditorVerticalPadding = 10, MenuIconTextGap = 16 };
        f.Ribbon.Theme = original; var sources = f.Menu.Items.ToArray(); form.Show(); Application.DoEvents();
        f.Menu.ShowDropDown(); Assert.True(f.Menu.IsDropDownOpen); var editor = f.Ribbon.Editors.EditorFor(text)!; nint handle = editor.Handle;
        var size = f.Ribbon.Popups.Top!.Size;
        f.Ribbon.Theme = Roomier with { EditorMinimumHeight = 54, EditorVerticalPadding = 10, MenuIconTextGap = 16 };
        Assert.False(f.Menu.IsDropDownOpen); Application.DoEvents(); f.Menu.ShowDropDown();
        Assert.Equal(handle, f.Ribbon.Editors.EditorFor(text)!.Handle); Assert.Equal("Retained", text.Value);
        Assert.True(f.Ribbon.Popups.Top!.Width > size.Width); Assert.Equal(sources, f.Menu.Items);
        f.Ribbon.Theme = original; Application.DoEvents(); f.Menu.ShowDropDown(); Assert.Equal(size, f.Ribbon.Popups.Top!.Size);
        Assert.Equal(handle, f.Ribbon.Editors.EditorFor(text)!.Handle); f.Menu.CloseDropDown(); form.Close();
    });

    private sealed class Fixture : IDisposable
    {
        internal RibbonControl Ribbon { get; } = new() { AnimationLevel = RibbonAnimationLevel.None };
        internal RibbonGroup Group { get; } = new();
        internal RibbonDropDownItem Menu { get; } = new() { Text = "Paste", DropDownHeader = "Paste options", MenuPresentation = RibbonMenuPresentation.IllustratedChoices };
        internal RibbonToggleItem Choice { get; } = new() { Text = "Keep source formatting", MenuDescription = "Keep the original font and paragraph spacing.", Icon = RibbonStockIcons.Create("open") };
        internal RibbonQuickAccessDropDownReference MenuReference { get; }
        internal RibbonQuickAccessReference ChoiceReference { get; }
        internal Fixture()
        {
            Menu.Items.AddRange(Choice, new RibbonButtonItem { Text = "Text only", Icon = RibbonStockIcons.Create("open") }, new RibbonMenuSeparatorItem());
            Group.Items.Add(Menu); var tab = new RibbonTab(); tab.Groups.Add(Group); Ribbon.Tabs.Add(tab);
            MenuReference = new() { Source = Menu }; ChoiceReference = new() { Source = Choice }; Ribbon.QuickAccessItems.AddRange(MenuReference, ChoiceReference);
        }
        public void Dispose() => Ribbon.Dispose();
    }
    private sealed class Record
    {
        internal Rectangle Image;
        internal readonly Dictionary<string, Rectangle> Text = [];
        internal readonly Dictionary<string, bool> Bold = [];
    }
    private sealed class Flat(Record record) : RibbonRenderer
    {
        protected override void DrawItemImage(RibbonPaintContext c, RibbonItem item, RibbonItemSize size) { record.Image = c.Bounds; base.DrawItemImage(c, item, size); }
        public override void DrawText(RibbonPaintContext c, string text, Color color, TextFormatFlags flags) { record.Text[text] = c.Bounds; record.Bold[text] = c.Font.Bold; base.DrawText(c, text, color, flags); }
    }
    private sealed class Office(Record record) : RibbonOffice2024Renderer
    {
        protected override void DrawItemImage(RibbonPaintContext c, RibbonItem item, RibbonItemSize size) { record.Image = c.Bounds; base.DrawItemImage(c, item, size); }
        public override void DrawText(RibbonPaintContext c, string text, Color color, TextFormatFlags flags) { record.Text[text] = c.Bounds; record.Bold[text] = c.Font.Bold; base.DrawText(c, text, color, flags); }
    }
}
