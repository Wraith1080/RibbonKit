using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class CommandSplitGeometryTests
{
    private static RibbonTheme Roomier => RibbonThemes.Office2024Light with
    { CommandSplitArrowWidth = 48, CommandSplitPrimaryMinimumWidth = 104, CommandLargeSplitPrimaryMinimumWidth = 144 };

    [Theory] [InlineData(false)] [InlineData(true)]
    public void Unset_command_split_measurements_and_paint_match_saved_defaults(bool office) => FoundationTests.Sta(() =>
    {
        using RibbonRenderer painter = office ? new RibbonOffice2024Renderer() : new RibbonRenderer();
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        foreach (int points in new[] { 9, 14 })
        foreach (var density in Enum.GetValues<RibbonDensity>())
        {
            using var f = new Fixture(); f.Ribbon.Density = density;
            using var font = new Font("Segoe UI", points * dpi / 96f); f.Ribbon.Font = font;
            int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
            var theme = f.Ribbon.Theme with { BodyHeight = 200 };
            var items = f.Ribbon.Engine.Measure(f.Group, 0, font, dpi, theme).Items;
            using var pixels = new Bitmap(items.Max(i => i.Bounds.Width) + S(16), items.Sum(i => i.Bounds.Height + S(8)) + S(8));
            using var g = Graphics.FromImage(pixels); g.Clear(Color.White); var data = new List<string>(); int y = S(8);
            foreach (var item in items)
            {
                var split = (RibbonSplitButtonItem)item.Item; var row = new Rectangle(S(8), y, item.Bounds.Width, item.Bounds.Height); y = row.Bottom + S(8);
                var parts = RibbonSplitLayout.Calculate(row, split, item.Size, false, dpi, font);
                var context = new RibbonPaintContext(g, parts.Primary, theme, font, dpi) { Density = density, Hot = true };
                painter.DrawSplitItem(context, context with { Bounds = parts.DropDown, Hot = false }, split, item.Size, false);
                data.Add($"classic:{item.Size}/{split.Layout}:{item.Bounds};primary={parts.Primary};arrow={parts.DropDown}");
            }
            f.Ribbon.LayoutMode = RibbonLayoutMode.Simplified;
            data.Add($"simplified-native-dpi={f.Ribbon.DeviceDpi}");
            data.AddRange(f.Ribbon.Snapshot.Targets.Where(t => t.Element is RibbonSplitButtonItem).Select(t => $"simplified:{t.Element!.Text}/{t.DropDownPart}:{t.Bounds}"));
            string name = $"command-split-{office}-{density}-{points}-{dpi}";
            using var encoded = new MemoryStream(); pixels.Save(encoded, System.Drawing.Imaging.ImageFormat.Png); byte[] png = encoded.ToArray();
            if (Environment.GetEnvironmentVariable("RIBBONKIT_COMMAND_SPLIT_CAPTURE") is { Length: > 0 } capture)
            { Directory.CreateDirectory(capture); File.WriteAllBytes(Path.Combine(capture, name + ".png"), png); File.WriteAllLines(Path.Combine(capture, name + ".txt"), data); }
            if (Environment.GetEnvironmentVariable("RIBBONKIT_COMMAND_SPLIT_BASELINE") is { Length: > 0 } baseline)
            { Assert.Equal(File.ReadAllLines(Path.Combine(baseline, name + ".txt")), data); Assert.Equal(File.ReadAllBytes(Path.Combine(baseline, name + ".png")), png); }
        }
    });

    [Theory] [InlineData(false)] [InlineData(true)]
    public void Explicit_geometry_reserves_full_arrows_and_primary_caption_budgets(bool office) => FoundationTests.Sta(() =>
    {
        var record = new Record(); using RibbonRenderer painter = office ? new Office(record) : new Flat(record);
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        foreach (int points in new[] { 9, 14 })
        foreach (var density in Enum.GetValues<RibbonDensity>())
        {
            using var f = new Fixture(); f.Ribbon.Density = density; using var font = new Font("Segoe UI", points * dpi / 96f);
            int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
            var original = f.Ribbon.Theme with { BodyHeight = 200 };
            var before = f.Ribbon.Engine.Measure(f.Group, 0, font, dpi, original).Items;
            foreach (var theme in new[] { Roomier, Roomier with { CommandSplitArrowWidth = 80 },
                RibbonThemes.Office2024Light with { CommandSplitPrimaryMinimumWidth = 180 },
                RibbonThemes.Office2024Light with { CommandLargeSplitPrimaryMinimumWidth = 200 },
                Roomier with { CommandSplitArrowWidth = 1, CommandSplitPrimaryMinimumWidth = 0, CommandLargeSplitPrimaryMinimumWidth = 0 } })
            {
                f.Ribbon.Theme = theme;
                var measured = f.Ribbon.Engine.Measure(f.Group, 0, font, dpi, theme with { BodyHeight = 200 }).Items;
                using var pixels = new Bitmap(measured.Max(i => i.Bounds.Width) + S(16), measured.Sum(i => i.Bounds.Height + S(8)) + S(8));
                using var g = Graphics.FromImage(pixels); g.Clear(Color.White); int y = S(8);
                for (int i = 0; i < measured.Count; i++)
                {
                    var item = measured[i]; var split = (RibbonSplitButtonItem)item.Item;
                    var bounds = new Rectangle(S(8), y, item.Bounds.Width, item.Bounds.Height); y = bounds.Bottom + S(8);
                    var parts = RibbonSplitLayout.Calculate(bounds, split, item.Size, false, dpi, font);
                    // The old geometry is evaluated with the old theme, independently of the new measurement.
                    f.Ribbon.Theme = RibbonThemes.Office2024Light; var old = RibbonSplitLayout.Calculate(before[i].Bounds, split, item.Size, false, dpi, font); f.Ribbon.Theme = theme;
                    Assert.Equal(bounds, Rectangle.Union(parts.Primary, parts.DropDown)); Assert.False(parts.Primary.IntersectsWith(parts.DropDown));
                    bool large = item.Size == RibbonItemSize.Large;
                    bool affected = theme.CommandSplitArrowWidth is not null || (large ? theme.CommandLargeSplitPrimaryMinimumWidth is not null : theme.CommandSplitPrimaryMinimumWidth is not null);
                    if (RibbonSplitLayout.IsVertical(split, item.Size, false))
                    { Assert.Equal(before[i].Bounds.Size, item.Bounds.Size); Assert.Equal(old.Primary.Height, parts.Primary.Height); continue; }
                    if (affected)
                    {
                        int arrow = Math.Max(theme.CommandSplitArrowWidth ?? (density == RibbonDensity.Touch ? 44 : 20), density == RibbonDensity.Touch ? 44 : 1);
                        int primary = Math.Max((large ? theme.CommandLargeSplitPrimaryMinimumWidth : theme.CommandSplitPrimaryMinimumWidth) ?? (density == RibbonDensity.Touch ? large ? 72 : 56 : 0), density == RibbonDensity.Touch ? large ? 72 : 56 : 0);
                        Assert.Equal(S(arrow), parts.DropDown.Width); Assert.True(parts.Primary.Width >= S(primary)); Assert.True(parts.Primary.Width >= old.Primary.Width);
                    }
                    else Assert.Equal(before[i].Bounds.Size, item.Bounds.Size);
                    var context = new RibbonPaintContext(g, parts.Primary, theme, font, dpi) { Density = density, Hot = true };
                    painter.DrawSplitItem(context, context with { Bounds = parts.DropDown, Hot = false }, split, item.Size, false);
                    Assert.Equal(parts.DropDown, record.Arrow);
                }
                if (theme == Roomier && Environment.GetEnvironmentVariable("RIBBONKIT_COMMAND_SPLIT_CAPTURE") is { Length: > 0 } capture)
                { Directory.CreateDirectory(capture); pixels.Save(Path.Combine(capture, $"override-{office}-{density}-{points}-{dpi}.png")); }
            }
        }
    });

    [Theory]
    [InlineData(nameof(RibbonTheme.CommandSplitArrowWidth), true)]
    [InlineData(nameof(RibbonTheme.CommandSplitPrimaryMinimumWidth), false)]
    [InlineData(nameof(RibbonTheme.CommandLargeSplitPrimaryMinimumWidth), false)]
    public void Split_roles_validate_atomically_and_reset_independently(string name, bool positive) => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl(); using var other = new RibbonControl(); var original = ribbon.Theme; var builtIn = ribbon.BuiltInTheme;
        var property = typeof(RibbonTheme).GetProperty(name)!;
        foreach (int value in new[] { positive ? 0 : -1, 1025 })
        {
            var bad = original with { }; property.SetValue(bad, value);
            Assert.Throws<ArgumentOutOfRangeException>(() => ribbon.Theme = bad); Assert.Same(original, ribbon.Theme); Assert.Equal(builtIn, ribbon.BuiltInTheme);
        }
        foreach (int value in new[] { positive ? 1 : 0, 1024 })
        {
            var valid = original with { }; property.SetValue(valid, value); ribbon.Theme = valid;
            Assert.Equal(value, property.GetValue(ribbon.Theme)); Assert.Same(original, other.Theme);
            var reset = valid with { }; property.SetValue(reset, null); ribbon.Theme = reset; Assert.Equal(original, ribbon.Theme);
        }
        ribbon.Theme = Roomier; ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2024Light; Assert.Equal(original, ribbon.Theme);
    });

    [Fact]
    public void Constrained_and_zero_width_horizontal_targets_tile_without_negative_regions() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); var split = f.Splits[1];
        foreach (var density in Enum.GetValues<RibbonDensity>())
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        foreach (var theme in new[] { RibbonThemes.Office2024Light, Roomier })
        foreach (int width in new[] { 0, 1, 2, 8, 32, 60, 180 })
        {
            f.Ribbon.Density = density; f.Ribbon.Theme = theme; var row = new Rectangle(5, 5, width, 40);
            var parts = RibbonSplitLayout.Calculate(row, split, RibbonItemSize.Medium, false, dpi);
            Assert.Equal(row, Rectangle.Union(parts.Primary, parts.DropDown)); Assert.True(parts.Primary.Width >= 0); Assert.True(parts.DropDown.Width >= 0);
            Assert.False(parts.Primary.IntersectsWith(parts.DropDown)); Assert.Equal(parts.Primary.Right, parts.DropDown.Left);
            if (width > 2) Assert.True(parts.DropDown.Width <= (density == RibbonDensity.Touch ? (width - 1) / 2 : Math.Max(1, width / 3)));
        }
    });

    [Theory] [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)]
    public void Native_classic_simplified_and_group_targets_share_paint_keys_uia_and_separate_actions(int kind) => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); using var form = new Form { Width = 1600, Height = 750 }; form.Controls.Add(f.Ribbon);
        var record = new Record(); using var painter = new Office(record); f.Ribbon.Renderer = painter;
        f.Ribbon.LayoutMode = kind is 1 or 3 ? RibbonLayoutMode.Simplified : RibbonLayoutMode.Classic;
        for (int i = 0; i < f.Splits.Length; i++) { f.Splits[i].KeyTip = ((char)('A' + 2 * i)).ToString(); f.Splits[i].DropDownKeyTip = ((char)('B' + 2 * i)).ToString(); }
        var action = f.Splits[1]; action.CheckOnClick = true; int clicks = 0, openings = 0; bool stateAtClick = false;
        action.Click += (_, _) => { clicks++; stateAtClick = action.Checked; };
        action.DropDownOpening += (_, e) => { openings++; e.Cancel = true; };
        form.Show(); Application.DoEvents();
        foreach (var density in Enum.GetValues<RibbonDensity>())
        {
            f.Ribbon.Density = density; f.Ribbon.Theme = Roomier; Application.DoEvents();
            using var popup = kind >= 2 ? new RibbonPopupSurface(f.Ribbon, f.Group, new(1400, 650)) : null;
            if (popup is not null) { form.Controls.Add(popup); popup.CreateControl(); }
            Control surface = popup ?? (Control)f.Ribbon;
            var targets = (popup?.Targets ?? f.Ribbon.Snapshot.Targets).Where(t => t.Element is RibbonSplitButtonItem).ToArray();
            int S(int n) => RibbonLayoutEngine.Scale(n, f.Ribbon.DeviceDpi);
            foreach (var split in f.Splits)
            {
                var pair = targets.Where(t => t.Element == split).ToArray(); Assert.Equal(2, pair.Length);
                var primary = pair.Single(t => !t.DropDownPart); var secondary = pair.Single(t => t.DropDownPart);
                if (RibbonSplitLayout.IsVertical(split, primary.Size, false)) continue;
                Assert.Equal(S(48), secondary.Bounds.Width); Assert.True(primary.Bounds.Width >= S(primary.Size == RibbonItemSize.Large ? 144 : 104));
                Assert.Equal(split.KeyTip, primary.PreferredKeyTip); Assert.Equal(split.DropDownKeyTip, secondary.PreferredKeyTip);
                var root = popup?.AutomationRoot ?? f.Ribbon.AutomationRoot; var node = root.Node(split); var bounds = node.BoundingRectangle;
                var screen = surface.RectangleToScreen(primary.VisibleBounds);
                Assert.Equal(screen.Left, bounds.Left); Assert.Equal(screen.Top, bounds.Top); Assert.Equal(screen.Width, bounds.Width); Assert.Equal(screen.Height, bounds.Height);
                Assert.Same(node, root.ElementProviderFromPoint(screen.Left + screen.Width / 2, screen.Top + screen.Height / 2));
                using var pixels = new Bitmap(surface.Width, surface.Height); using var graphics = Graphics.FromImage(pixels);
                var context = new RibbonPaintContext(graphics, primary.Bounds, Roomier, f.Ribbon.Font, f.Ribbon.DeviceDpi) { Density = density };
                painter.DrawSplitItem(context, context with { Bounds = secondary.Bounds }, split, primary.Size, false); Assert.Equal(secondary.Bounds, record.Arrow);
            }
            var main = targets.Single(t => t.Element == action && !t.DropDownPart); var arrow = targets.Single(t => t.Element == action && t.DropDownPart);
            bool old = action.Checked; int oldClicks = clicks, oldOpenings = openings;
            f.Ribbon.Activate(main, popup); Assert.Equal(oldClicks + 1, clicks); Assert.Equal(!old, action.Checked); Assert.Equal(action.Checked, stateAtClick); Assert.Equal(oldOpenings, openings);
            f.Ribbon.Activate(arrow, popup); Assert.Equal(oldClicks + 1, clicks); Assert.Equal(oldOpenings + 1, openings); Assert.False(action.IsDropDownOpen);
        }
        form.Close();
    });

    [Fact]
    public void Menu_qat_and_vertical_large_regions_keep_their_existing_roles() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); var split = f.Splits[1]; var reference = new RibbonQuickAccessDropDownReference { Source = split }; f.Ribbon.QuickAccessItems.Add(reference);
        foreach (var density in Enum.GetValues<RibbonDensity>())
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        {
            f.Ribbon.Density = density; f.Ribbon.Theme = RibbonThemes.Office2024Light;
            var row = new Rectangle(0, 0, RibbonLayoutEngine.Scale(300, dpi), RibbonLayoutEngine.Scale(160, dpi));
            var menu = RibbonSplitLayout.Calculate(row, split, RibbonItemSize.Medium, true, dpi);
            var vertical = RibbonSplitLayout.Calculate(row, f.Splits[3], RibbonItemSize.Large, false, dpi);
            var target = new RibbonTarget(TargetKind.QuickAccess, reference, row, row, RibbonItemSize.Medium);
            var qat = RibbonSplitLayout.Expand(target, dpi, false).ToArray();
            f.Ribbon.Theme = Roomier;
            Assert.Equal(menu, RibbonSplitLayout.Calculate(row, split, RibbonItemSize.Medium, true, dpi));
            Assert.Equal(vertical, RibbonSplitLayout.Calculate(row, f.Splits[3], RibbonItemSize.Large, false, dpi));
            Assert.Equal(qat, RibbonSplitLayout.Expand(target, dpi, false));
        }
    });

    [Fact]
    public void Shared_columns_retain_native_editor_handles_widths_and_values() => FoundationTests.Sta(() =>
    {
        using var form = new Form { Width = 1500, Height = 750 };
        var ribbon = new RibbonControl { AnimationLevel = RibbonAnimationLevel.None }; form.Controls.Add(ribbon);
        var group = new RibbonGroup { CanResize = false }; var tab = new RibbonTab();
        var text = new RibbonTextBoxItem { Text = "Name", InputWidth = 120, Value = "Retained" };
        var combo = new RibbonComboBoxItem { Text = "Font", InputWidth = 120 }; combo.Items.Add("Aptos"); combo.SelectedIndex = 0;
        group.Items.AddRange(text, combo, new RibbonSplitButtonItem { Text = "Paste", SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium) });
        tab.Groups.Add(group); ribbon.Tabs.Add(tab); form.Show(); Application.DoEvents();
        foreach (var density in Enum.GetValues<RibbonDensity>())
        {
            ribbon.Density = density; ribbon.Theme = RibbonThemes.Office2024Light with { EditorMinimumHeight = 54, EditorVerticalPadding = 10 }; Application.DoEvents();
            var nativeText = ribbon.Editors.EditorFor(text)!; var nativeCombo = ribbon.Editors.EditorFor(combo)!;
            nint textHandle = nativeText.Handle, comboHandle = nativeCombo.Handle; int width = nativeText.Width, comboWidth = nativeCombo.Width;
            var original = ribbon.Theme;
            ribbon.Theme = original with { CommandSplitArrowWidth = 48, CommandSplitPrimaryMinimumWidth = 104, CommandLargeSplitPrimaryMinimumWidth = 144 }; Application.DoEvents();
            Assert.Equal(textHandle, ribbon.Editors.EditorFor(text)!.Handle); Assert.Equal(comboHandle, ribbon.Editors.EditorFor(combo)!.Handle);
            Assert.Equal(width, nativeText.Width); Assert.Equal(comboWidth, nativeCombo.Width); Assert.Equal("Retained", nativeText.Text); Assert.Equal("Aptos", combo.Value);
            ribbon.Theme = original; Application.DoEvents(); Assert.Equal(width, nativeText.Width); Assert.Equal(comboWidth, nativeCombo.Width);
        }
        form.Close();
    });

    private sealed class Record { internal Rectangle Arrow; }
    private sealed class Flat(Record record) : RibbonRenderer
    { public override void DrawDropDownArrow(RibbonPaintContext c, Color color) { record.Arrow = c.Bounds; base.DrawDropDownArrow(c, color); } }
    private sealed class Office(Record record) : RibbonOffice2024Renderer
    { public override void DrawDropDownArrow(RibbonPaintContext c, Color color) { record.Arrow = c.Bounds; base.DrawDropDownArrow(c, color); } }

    private sealed class Fixture : IDisposable
    {
        internal RibbonControl Ribbon { get; } = new() { Width = 2000, AnimationLevel = RibbonAnimationLevel.None };
        internal RibbonGroup Group { get; } = new() { CanResize = false };
        internal RibbonSplitButtonItem[] Splits { get; } =
        [new() { Text = "Small", Icon = RibbonStockIcons.Create("open"), SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Small), SimplifiedSize = RibbonItemSize.Small },
         new() { Text = "Medium paste", Icon = RibbonStockIcons.Create("open"), SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium) },
         new() { Text = "Large paste", Icon = RibbonStockIcons.Create("open"), SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Large) },
         new() { Text = "Vertical paste", Icon = RibbonStockIcons.Create("open"), Layout = RibbonSplitButtonLayout.Vertical, SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Large) }];
        internal Fixture()
        {
            foreach (var split in Splits) { split.SimplifiedPlacement = RibbonSimplifiedPlacement.Inline; split.Items.Add(new RibbonButtonItem { Text = "Text only" }); }
            Group.Items.AddRange(Splits); var tab = new RibbonTab(); tab.Groups.Add(Group); Ribbon.Tabs.Add(tab);
        }
        public void Dispose() => Ribbon.Dispose();
    }
}
