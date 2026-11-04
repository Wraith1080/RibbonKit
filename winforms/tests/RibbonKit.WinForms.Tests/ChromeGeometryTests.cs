using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class ChromeGeometryTests
{
    private static RibbonTheme Roomier => RibbonThemes.Office2024Light with { GroupHorizontalPadding = 12, GroupColumnGap = 10, BodyContentHorizontalPadding = 12,
        GroupSeparatorVerticalInset = 20, TabHorizontalPadding = 22, TabMinimumWidth = 90, HeaderIndicatorPadding = 8,
        QuickAccessHorizontalPadding = 8, QuickAccessItemGap = 6, MessageActionGap = 18, MessageTitleTextGap = 12 };

    public static TheoryData<string> Roles => new() { nameof(RibbonTheme.GroupHorizontalPadding), nameof(RibbonTheme.GroupColumnGap), nameof(RibbonTheme.BodyContentHorizontalPadding), nameof(RibbonTheme.GroupSeparatorVerticalInset), nameof(RibbonTheme.TabHorizontalPadding), nameof(RibbonTheme.TabMinimumWidth), nameof(RibbonTheme.HeaderIndicatorPadding), nameof(RibbonTheme.QuickAccessHorizontalPadding), nameof(RibbonTheme.QuickAccessItemGap), nameof(RibbonTheme.MessageActionGap), nameof(RibbonTheme.MessageTitleTextGap) };

    [Theory] [MemberData(nameof(Roles))]
    public void Roles_validate_before_assignment_and_reset_independently(string name) => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl(); using var other = new RibbonControl(); ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2024Light;
        var original = ribbon.Theme; var builtIn = ribbon.BuiltInTheme; var property = typeof(RibbonTheme).GetProperty(name)!;
        foreach (int value in new[] { name == nameof(RibbonTheme.TabMinimumWidth) ? 0 : -1, 1025 })
        {
            var bad = original with { }; property.SetValue(bad, value); Assert.Throws<ArgumentOutOfRangeException>(() => ribbon.Theme = bad);
            Assert.Same(original, ribbon.Theme); Assert.Equal(builtIn, ribbon.BuiltInTheme);
        }
        foreach (int value in new[] { name == nameof(RibbonTheme.TabMinimumWidth) ? 1 : 0, 1024 })
        {
            var valid = original with { MenuLeadingPadding = 16 }; property.SetValue(valid, value); ribbon.Theme = valid;
            Assert.Equal(value, property.GetValue(ribbon.Theme)); Assert.Same(original, other.Theme);
            var reset = valid with { }; property.SetValue(reset, null); ribbon.Theme = reset; Assert.Equal(original with { MenuLeadingPadding = 16 }, ribbon.Theme);
        }
    });

    [Theory] [InlineData(RibbonDensity.Compact)] [InlineData(RibbonDensity.Touch)]
    public void Group_padding_gaps_and_collapsed_slots_preserve_content_budgets(RibbonDensity density) => FoundationTests.Sta(() =>
    {
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        foreach (int points in new[] { 9, 24 })
        foreach (var layout in Enum.GetValues<RibbonGroupLayout>())
        {
            using var f = new Fixture(); using var font = new Font("Segoe UI", points * dpi / 96f); f.Ribbon.Font = font; f.Ribbon.Density = density; f.Group.Layout = layout;
            int S(int n) => RibbonLayoutEngine.Scale(n, dpi); f.Ribbon.Engine.Clear();
            var original = f.Ribbon.Engine.Measure(f.Group, 0, font, dpi, f.Ribbon.ClassicLayoutTheme); var collapsed = f.Ribbon.Engine.Measure(f.Group, 3, font, dpi, f.Ribbon.ClassicLayoutTheme);
            f.Ribbon.Theme = Roomier; f.Ribbon.Engine.Clear(); var changed = f.Ribbon.Engine.Measure(f.Group, 0, font, dpi, f.Ribbon.ClassicLayoutTheme);
            Assert.True(changed.Width >= original.Width); Assert.Equal(original.Items.Select(i => i.Bounds.Size), changed.Items.Select(i => i.Bounds.Size));
            Assert.True(changed.Items.Min(i => i.Bounds.Left) >= S(12)); Assert.True(changed.Width - 1 - changed.Items.Max(i => i.Bounds.Right) >= S(12));
            Assert.Equal(collapsed.Width + 2 * (S(12) - S(3)), f.Ribbon.Engine.Measure(f.Group, 3, font, dpi, f.Ribbon.ClassicLayoutTheme).Width);
            f.Ribbon.Theme = RibbonThemes.Office2024Light; f.Ribbon.Engine.Clear(); var reset = f.Ribbon.Engine.Measure(f.Group, 0, font, dpi, f.Ribbon.ClassicLayoutTheme);
            Assert.Equal(original.Width, reset.Width); Assert.Equal(original.Items, reset.Items);
        }
    });

    [Theory] [InlineData(RibbonDensity.Compact)] [InlineData(RibbonDensity.Touch)]
    public void Qat_spacing_selects_entries_and_overflow_without_changing_sources_or_sizes(RibbonDensity density) => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); f.Ribbon.Density = density; var sources = f.Ribbon.QuickAccessItems.ToArray(); f.Ribbon.Theme = Roomier;
        int S(int n) => RibbonLayoutEngine.Scale(n, f.Ribbon.DeviceDpi);
        foreach (bool fill in new[] { false, true })
        foreach (int width in new[] { 0, 15, 54, 160, 900 })
        {
            var slot = new Rectangle(11, 7, width, S(48)); var measured = RibbonQuickAccessLayout.Build(f.Ribbon, slot, fill);
            if (width == 0) { Assert.Empty(measured.Targets); Assert.Empty(measured.QuickAccessOverflowItems); continue; }
            var entries = measured.Targets.Where(t => t.Kind == TargetKind.QuickAccess).ToArray();
            Assert.Equal(sources.Take(entries.Length), entries.Select(t => t.Element));
            Assert.Equal(sources.Skip(entries.Length), measured.QuickAccessOverflowItems);
            for (int i = 1; i < entries.Length; i++) Assert.Equal(S(6), entries[i].Bounds.Left - entries[i - 1].Bounds.Right);
            Assert.All(measured.Targets, t => { Assert.True(t.Bounds.Width >= 0); Assert.True(t.Bounds.Left >= slot.Left); Assert.True(t.Bounds.Right <= slot.Right); });
            if (entries.Length > 0) Assert.Equal(slot.Left + Math.Min(S(8), width / 2), entries[0].Bounds.Left);
            if (entries.Length == sources.Length) Assert.All(entries, t => Assert.Equal(S(density == RibbonDensity.Touch ? 44 : 28), t.Bounds.Width));
        }
        Assert.Equal(sources, f.Ribbon.QuickAccessItems);
    });

    [Theory] [InlineData(RibbonDensity.Compact)] [InlineData(RibbonDensity.Touch)]
    public void Message_gaps_keep_inline_and_wrapped_content_and_actions_disjoint(RibbonDensity density) => FoundationTests.Sta(() =>
    {
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        foreach (int points in new[] { 9, 24 })
        {
            using var font = new Font("Segoe UI", points * dpi / 96f); using var f = new Fixture(); f.Ribbon.Density = density; f.Ribbon.Font = font;
            foreach (int spacing in new[] { 0, 18, 1024 })
            foreach (int width in new[] { 90, 800 })
            {
                f.Ribbon.Theme = Roomier with { MessageActionGap = spacing, MessageTitleTextGap = spacing };
                int S(int n) => RibbonLayoutEngine.Scale(n, dpi); var row = RibbonMessageLayout.Measure(f.Ribbon, S(width), dpi).Rows.Single();
                Assert.True(row.Bounds.Height >= font.Height); Assert.False(row.Title.IntersectsWith(row.Text));
                Assert.False(row.Action.IntersectsWith(row.Text)); Assert.False(row.Close.IntersectsWith(row.Action));
                if (row.Title.Top == row.Text.Top) Assert.Equal(S(spacing), row.Text.Left - row.Title.Right);
                Assert.True(row.Action.Width >= 0); Assert.True(row.Action.Right <= row.Bounds.Right);
                Assert.Same(f.Ribbon.MessageBar.Items[0], row.Message);
            }
        }
    });

    [Theory] [InlineData(RibbonLayoutMode.Classic)] [InlineData(RibbonLayoutMode.Simplified)]
    public void Combined_chrome_targets_keep_editor_handles_source_state_and_joined_frames(RibbonLayoutMode mode) => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); using var form = new RibbonForm { Width = 1300, Height = 750 }; form.Controls.Add(f.Ribbon); form.Ribbon = f.Ribbon;
        foreach (var item in f.Group.Items) item.SimplifiedPlacement = RibbonSimplifiedPlacement.Inline;
        var second = new RibbonGroup { Text = "Other", CanResize = false };
        second.Items.Add(new RibbonButtonItem { Text = "Other command", SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium), SimplifiedPlacement = RibbonSimplifiedPlacement.Inline });
        f.Ribbon.SelectedTab!.Groups.Add(second);
        f.Ribbon.LayoutMode = mode; form.Show(); Application.DoEvents();
        foreach (var density in Enum.GetValues<RibbonDensity>())
        foreach (var placement in Enum.GetValues<RibbonQuickAccessPosition>())
        {
            f.Ribbon.Density = density; f.Ribbon.QuickAccessPosition = placement;
            f.Ribbon.Theme = RibbonThemes.Office2024Light with { CommandContentHorizontalPadding = 12, CommandContentVerticalPadding = 16, EditorMinimumHeight = 54 };
            Application.DoEvents(); var original = f.Ribbon.Theme; var field = f.Group.Items.OfType<RibbonTextBoxItem>().Single();
            var editor = mode == RibbonLayoutMode.Classic ? f.Ribbon.Editors.EditorFor(field) : null; nint handle = editor?.Handle ?? 0; var size = editor?.Size;
            f.Ribbon.Theme = Roomier with { CommandContentHorizontalPadding = 12, CommandContentVerticalPadding = 16, EditorMinimumHeight = 54 }; Application.DoEvents();
            var snapshot = f.Ribbon.Snapshot; int S(int n) => RibbonLayoutEngine.Scale(n, f.Ribbon.DeviceDpi);
            Assert.True(snapshot.BodyViewport.Left >= snapshot.BodyBounds.Left + Math.Min(S(12), snapshot.BodyBounds.Width / 2));
            Assert.All(snapshot.Targets.Where(t => t.Kind == TargetKind.Tab), t => Assert.True(t.Bounds.Width >= S(90)));
            Assert.All(snapshot.SimplifiedDividers, r => Assert.Equal(S(20), r.Top - snapshot.BodyBounds.Top));
            if (mode == RibbonLayoutMode.Simplified) Assert.NotEmpty(snapshot.SimplifiedDividers);
            if (editor is not null) { Assert.Equal(handle, f.Ribbon.Editors.EditorFor(field)!.Handle); Assert.Equal(size, editor.Size); Assert.Equal("Retained", editor.Text); }
            var message = snapshot.Messages.Single(); Assert.Equal(S(12), message.Text.Left - message.Title.Right);
            Assert.True(message.Action.Left >= message.Text.Right + S(18));
            var action = snapshot.Targets.Single(t => t.Kind == TargetKind.MessageAction); Assert.Equal(message.Action, action.Bounds);
            Assert.Equal(action.Bounds, Rectangle.Intersect(action.Bounds, action.Clip));
            Assert.Equal(placement, f.Ribbon.EffectiveQuickAccessPosition);
            f.Ribbon.Theme = original; Application.DoEvents(); Assert.Equal(original, f.Ribbon.Theme);
        }
        form.Close();
    });

    [Theory] [InlineData(false)] [InlineData(true)]
    public void Indicator_padding_and_office_separator_clearance_use_synthetic_dpi_bounds(bool office) => FoundationTests.Sta(() =>
    {
        using RibbonRenderer painter = office ? new RibbonOffice2024Renderer() : new RibbonRenderer(); using var tab = new RibbonTab { Text = "Home" }; using var group = new RibbonGroup { Text = "Formatting" };
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        foreach (int points in new[] { 9, 24 })
        foreach (int padding in new[] { 0, 8, 1024 })
        {
            using var font = new Font("Segoe UI", points * dpi / 96f); int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
            using var pixels = new Bitmap(S(220), S(190)); using var graphics = Graphics.FromImage(pixels); graphics.Clear(Color.White);
            var c = new RibbonPaintContext(graphics, new(0, 0, S(180), S(40)), Roomier with { HeaderIndicatorPadding = padding, Border = Color.Magenta }, font, dpi);
            var indicator = painter.GetTabIndicatorBounds(c, tab);
            int expected = office ? Math.Max(0, Math.Min(RibbonCaptionLayout.TextWidth(tab.Text, font) + 2 * S(padding), c.Bounds.Width - 2 * S(padding))) : 0;
            Assert.Equal(expected, indicator.Width); if (!indicator.IsEmpty) Assert.True(c.Bounds.Contains(indicator));
            if (office)
            {
                var frame = new Rectangle(0, 0, S(200), S(180)); painter.DrawGroup(c with { Bounds = frame }, group, false);
                var ink = Enumerable.Range(0, frame.Height).Where(y => pixels.GetPixel(frame.Right - 1, y).ToArgb() == Color.Magenta.ToArgb()).ToArray();
                Assert.NotEmpty(ink); Assert.Equal(S(20), ink[0]); Assert.Equal(frame.Bottom - S(20) - 1, ink[^1]);
            }
        }
    });

    [Theory] [InlineData(RibbonDensity.Compact)] [InlineData(RibbonDensity.Touch)]
    public void Roomier_headers_and_bodies_keep_overflow_and_scroll_reveal_reachable(RibbonDensity density) => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); f.Ribbon.Width = 500; f.Ribbon.QuickAccessMaxWidth = 100; f.Ribbon.Density = density; f.Ribbon.Theme = Roomier;
        for (int i = 0; i < 12; i++) f.Ribbon.Tabs.Add(new RibbonTab { Text = "Long tab " + i });
        var extra = new RibbonGroup { Text = "Fixed commands", CanResize = false };
        for (int i = 0; i < 6; i++) extra.Items.Add(new RibbonButtonItem { Text = "Command " + i });
        f.Ribbon.SelectedTab!.Groups.Add(extra);
        var before = f.Ribbon.Snapshot; Assert.True(before.TabExtent > before.TabViewport.Width); Assert.True(before.BodyExtent > before.BodyViewport.Width);
        Assert.Contains(before.Targets, t => t.Kind == TargetKind.TabNext); Assert.Contains(before.Targets, t => t.Kind == TargetKind.BodyNext);
        f.Ribbon.Scroll(true, int.MaxValue); var tab = f.Ribbon.Snapshot.Targets.Single(t => t.Element == f.Ribbon.Tabs.Last());
        Assert.Equal(tab.Bounds, tab.VisibleBounds);
        f.Ribbon.Scroll(false, int.MaxValue); var command = f.Ribbon.Snapshot.Targets.Single(t => t.Element == extra.Items.Last());
        Assert.Equal(command.Bounds, command.VisibleBounds);
        Assert.Equal(f.Ribbon.QuickAccessItems.Count, before.QuickAccessOverflowItems.Count + before.Targets.Count(t => t.Kind == TargetKind.QuickAccess));
    });

    [Theory] [InlineData(false)] [InlineData(true)]
    public void Unset_chrome_roles_preserve_saved_pixels_and_measurements(bool office) => FoundationTests.Sta(() =>
    {
        foreach (var density in Enum.GetValues<RibbonDensity>())
        foreach (var mode in Enum.GetValues<RibbonLayoutMode>())
        foreach (var placement in new[] { RibbonQuickAccessPosition.TabRow, RibbonQuickAccessPosition.BelowRibbon })
        foreach (int points in new[] { 9, 14 })
        {
            using var f = new Fixture(); using var font = new Font("Segoe UI", points); f.Ribbon.Font = font;
            using RibbonRenderer painter = office ? new RibbonOffice2024Renderer() : new RibbonRenderer();
            f.Ribbon.Renderer = painter; f.Ribbon.Density = density; f.Ribbon.LayoutMode = mode; f.Ribbon.QuickAccessPosition = placement;
            using var pixels = new Bitmap(f.Ribbon.Width, f.Ribbon.Height); f.Ribbon.DrawToBitmap(pixels, f.Ribbon.ClientRectangle);
            var layout = f.Ribbon.Snapshot; var data = new List<string> { $"native-dpi={f.Ribbon.DeviceDpi};height={f.Ribbon.Height};body={layout.BodyBounds};viewport={layout.BodyViewport};qat={layout.QuickAccessBounds}" };
            data.AddRange(layout.Targets.Select(t => $"{t.Kind}/{t.Element?.Text}:{t.Bounds}/{t.Clip}"));
            data.AddRange(layout.Groups.Select(g => $"group:{g.Bounds}/{g.Frame}/{g.Collapsed}"));
            data.AddRange(layout.Messages.Select(m => $"message:{m.Bounds}/{m.Title}/{m.Text}/{m.Action}/{m.Close}"));
            foreach (int dpi in new[] { 96, 120, 144, 192 })
            {
                using var scaled = new Font("Segoe UI", points * dpi / 96f); f.Ribbon.Font = scaled; f.Ribbon.Engine.Clear();
                var measured = f.Ribbon.Engine.Measure(f.Group, 0, scaled, dpi, f.Ribbon.ClassicLayoutTheme);
                data.Add($"dpi={dpi}:group={measured.Width}"); data.AddRange(measured.Items.Select(i => $"{i.Item.Text}:{i.Bounds}"));
                var messages = RibbonMessageLayout.Measure(f.Ribbon, RibbonLayoutEngine.Scale(800, dpi), dpi);
                data.AddRange(messages.Rows.Select(m => $"dpi={dpi}:message:{m.Bounds}/{m.Title}/{m.Text}/{m.Action}/{m.Close}"));
                var c = new RibbonPaintContext(Graphics.FromImage(pixels), new(0, 0, RibbonLayoutEngine.Scale(160, dpi), RibbonLayoutEngine.Scale(40, dpi)), f.Ribbon.Theme, scaled, dpi);
                using (c.Graphics) data.Add($"dpi={dpi}:indicator={painter.GetTabIndicatorBounds(c, f.Ribbon.SelectedTab!)}");
            }
            using var stream = new MemoryStream(); pixels.Save(stream, System.Drawing.Imaging.ImageFormat.Png); var png = stream.ToArray();
            string name = $"chrome-{office}-{density}-{mode}-{placement}-{points}";
            if (Environment.GetEnvironmentVariable("RIBBONKIT_CHROME_CAPTURE") is { Length: > 0 } output)
            { Directory.CreateDirectory(output); File.WriteAllBytes(Path.Combine(output, name + ".png"), png); File.WriteAllLines(Path.Combine(output, name + ".txt"), data); }
            if (Environment.GetEnvironmentVariable("RIBBONKIT_CHROME_BASELINE") is { Length: > 0 } baseline)
            { Assert.Equal(File.ReadAllBytes(Path.Combine(baseline, name + ".png")), png); Assert.Equal(File.ReadAllLines(Path.Combine(baseline, name + ".txt")), data); }
        }
    });

    [Theory] [InlineData(RibbonDensity.Compact)] [InlineData(RibbonDensity.Touch)]
    public void Inert_designer_preview_uses_shared_chrome_without_committing_or_opening_popups(RibbonDensity density) => FoundationTests.Sta(() =>
    {
        using var host = new DesignerAuthoringTests.Host(); using var ribbon = DesignerAuthoringTests.MakeRibbon(host); using var painter = new PreviewRecorder();
        ribbon.Width = 1400; ribbon.Density = density; ribbon.Renderer = painter; ribbon.Theme = Roomier; var group = ribbon.Tabs[0].Groups[0]; group.CanResize = false;
        var original = group.Items.ToArray();
        foreach (var mode in Enum.GetValues<RibbonLayoutMode>())
        {
            ribbon.LayoutMode = mode; painter.Widths.Clear();
            var draft = RibbonKit.WinForms.Design.RibbonDesignCommitter.Read(ribbon, host);
            var preview = RibbonKit.WinForms.Design.RibbonThemePreviewRenderer.Render(ribbon, host, draft);
            Assert.NotEmpty(preview.RibbonPng); Assert.NotEmpty(painter.Widths);
            Assert.All(painter.Widths, width => Assert.True(width >= RibbonLayoutEngine.Scale(90, ribbon.DeviceDpi)));
            Assert.Equal(original, group.Items); Assert.Equal(0, host.Transactions); Assert.False(ribbon.FileMenu.IsDropDownOpen);
            if (Environment.GetEnvironmentVariable("RIBBONKIT_CHROME_CAPTURE") is { Length: > 0 } output)
                File.WriteAllBytes(Path.Combine(output, $"chrome-preview-{density}-{mode}.png"), Convert.FromBase64String(preview.RibbonPng));
        }
    });

    private sealed class PreviewRecorder : RibbonOffice2024Renderer
    {
        internal readonly List<int> Widths = [];
        public override void DrawTab(RibbonPaintContext c, RibbonTab tab, bool selected)
        { Assert.Equal(22, c.Theme.TabHorizontalPadding); Widths.Add(c.Bounds.Width); base.DrawTab(c, tab, selected); }
    }

    private sealed class Fixture : IDisposable
    {
        internal readonly RibbonControl Ribbon = new() { Width = 1000, AnimationLevel = RibbonAnimationLevel.None };
        internal readonly RibbonGroup Group = new() { Text = "Formatting", CanResize = false };
        internal Fixture()
        {
            Group.Items.AddRange(new RibbonToggleItem { Text = "Bold", Icon = RibbonStockIcons.Create("bold"), SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Small), SimplifiedSize = RibbonItemSize.Small },
                new RibbonButtonItem { Text = "Open document", Icon = RibbonStockIcons.Create("open"), SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium) },
                new RibbonCheckBoxItem { Text = "Keep formatting", Checked = true }, new RibbonTextBoxItem { Text = "Name", Value = "Retained" });
            var tab = new RibbonTab { Text = "Home" }; tab.Groups.Add(Group); Ribbon.Tabs.Add(tab); Ribbon.Tabs.Add(new RibbonTab { Text = "Document layout" });
            Ribbon.FileMenu.Visible = true; Ribbon.FileMenu.Items.Add(new RibbonButtonItem { Text = "New" });
            for (int i = 0; i < 5; i++) Ribbon.QuickAccessItems.Add(new RibbonButtonItem { Text = "Quick " + i, Icon = RibbonStockIcons.Create("open") });
            var message = new RibbonMessage { Title = "Notice", Text = "Document settings can be reviewed here." }; message.Action.Text = "Review"; Ribbon.MessageBar.Items.Add(message);
        }
        public void Dispose() => Ribbon.Dispose();
    }
}
