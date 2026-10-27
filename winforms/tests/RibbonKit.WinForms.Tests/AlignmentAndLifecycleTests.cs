using RibbonKit.WinForms.Layout;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace RibbonKit.WinForms.Tests;

public sealed class AlignmentAndLifecycleTests
{
    [Fact]
    public void Body_padding_grows_downward_and_retains_command_and_editor_sizes() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { Width = 1600 };
        Assert.Equal(110, ribbon.Theme.BodyHeight);
        var tab = new RibbonTab { Text = "Home" }; var group = new RibbonGroup { Text = "Commands" };
        var large = new RibbonButtonItem { Text = "Large command" };
        var split = new RibbonSplitButtonItem { Text = "Split", Layout = RibbonSplitButtonLayout.Vertical };
        var text = new RibbonTextBoxItem { Text = "Name" }; var combo = new RibbonComboBoxItem { Text = "Font" };
        group.Items.AddRange(large, split, text, combo, new RibbonCheckBoxItem { Text = "Ruler" });
        tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        using var dpiProbe = new Control();
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        foreach (int points in new[] { 9, 14 })
        {
            using var font = new Font("Segoe UI", points * dpi / (float)dpiProbe.DeviceDpi);
            int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
            int fontHeight = (int)Math.Ceiling(font.Height * 96d / dpi);
            int row = Math.Max(26, fontHeight + 8), caption = Math.Max(20, fontHeight + 6);
            // Previous geometry is the reference: added space must not stretch commands.
            int oldHeight = Math.Max(104, 3 * row + caption + 8);
            var theme = ribbon.Theme with { BodyHeight = oldHeight + 6 };
            var measured = new RibbonLayoutEngine().Measure(group, 0, font, dpi, theme);
            foreach (var item in measured.Items.Where(i => i.Size == RibbonItemSize.Large))
            {
                Assert.Equal(S(6), item.Bounds.Top);
                Assert.Equal(S(oldHeight - caption - 8), item.Bounds.Height);
                var oldBounds = new Rectangle(item.Bounds.X, S(4), item.Bounds.Width, item.Bounds.Height);
                var before = RibbonContentLayout.Calculate(oldBounds, item.Size, false, true, dpi, font);
                var after = RibbonContentLayout.Calculate(item.Bounds, item.Size, false, true, dpi, font);
                Assert.Equal(before.Icon.Size, after.Icon.Size); Assert.Equal(before.Text.Size, after.Text.Size);
                int captionHeight = Math.Max(font.Height + S(6), S(20));
                int oldGap = S(oldHeight) - captionHeight - oldBounds.Bottom;
                int newGap = S(theme.BodyHeight) - captionHeight - item.Bounds.Bottom;
                // Body and command edges round separately; compare the added
                // clearance with the previous raster bounds at the same DPI.
                Assert.InRange(newGap - oldGap, S(4) - 1, S(4) + 1);
            }
            int index = 0;
            foreach (var item in measured.Items.Where(i => i.Item is IRibbonEditorItem))
            {
                Assert.Equal(S(6 + index * row), item.Bounds.Top);
                var oldBounds = new Rectangle(item.Bounds.X, S(4 + index * row), item.Bounds.Width, S(row - 1));
                var before = RibbonEditorLayout.Field(oldBounds, item.Item, item.LabelWidth, font, dpi);
                var after = RibbonEditorLayout.Field(item.Bounds, item.Item, item.LabelWidth, font, dpi);
                Assert.Equal(before.Size, after.Size); index++;
            }
        }
        int actualDpi = ribbon.DeviceDpi;
        int A(int n) => RibbonLayoutEngine.Scale(n, actualDpi);
        int logicalFont = (int)Math.Ceiling(ribbon.Font.Height * 96d / actualDpi);
        int previousHeight = Math.Max(104, 3 * Math.Max(26, logicalFont + 8) + Math.Max(20, logicalFont + 6) + 8);
        int tabHeight = A(Math.Max(32, logicalFont + 12));
        Assert.Equal(tabHeight, ribbon.Snapshot.TabViewport.Height);
        Assert.Equal(tabHeight + A(1), ribbon.Snapshot.BodyBounds.Top);
        Assert.Equal(A(previousHeight + 6), ribbon.Snapshot.BodyBounds.Height);
        Assert.Equal(tabHeight + A(previousHeight + 6) + A(1) + A(7), ribbon.Height);
        var full = ribbon.Snapshot.Targets.Single(t => ReferenceEquals(t.Element, large));
        ribbon.Width = 130;
        var collapsed = Assert.Single(ribbon.Snapshot.Groups);
        Assert.True(collapsed.Collapsed);
        Assert.Equal(ribbon.Snapshot.BodyBounds.Top + A(6), collapsed.Bounds.Top);
        Assert.Equal(ribbon.Snapshot.BodyBounds.Bottom - A(8), collapsed.Bounds.Bottom);
        using var popup = new RibbonPopupSurface(ribbon, group, new Size(1600, 1000));
        var popupLarge = popup.Targets.Single(t => ReferenceEquals(t.Element, large));
        Assert.Equal(full.Bounds.Size, popupLarge.Bounds.Size);
        Assert.Equal(A(4), popupLarge.Bounds.Top); // Group flyout retains its own compact padding.
    });

    [Theory]
    [InlineData(120, 9)]
    [InlineData(192, 18)]
    public void Neighbouring_rows_share_column_width_icon_slot_and_text_baseline(int dpi, int fontPoints) => FoundationTests.Sta(() =>
    {
        using var font = new Font("Segoe UI", fontPoints);
        using var image = new Bitmap(16, 16);
        var group = new RibbonGroup();
        group.Items.Add(new RibbonButtonItem { Text = "Short", Image = image, SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium) });
        group.Items.Add(new RibbonButtonItem { Text = "A longer neighbouring label", SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium) });
        group.Items.Add(new RibbonButtonItem { Text = "Third", SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium) });
        var measure = new RibbonLayoutEngine().Measure(group, 0, font, dpi, new RibbonTheme { BodyHeight = 300 });
        Assert.Single(measure.Items.Select(i => i.Bounds.Width).Distinct());
        Assert.All(measure.Items, i => Assert.True(i.ReserveIcon));
        var contents = measure.Items.Select(i => RibbonContentLayout.Calculate(i.Bounds, i.Size, false, i.ReserveIcon, dpi, font)).ToArray();
        Assert.Single(contents.Select(c => c.Text.Left).Distinct());
        Assert.Single(contents.Select(c => c.Icon.Left).Distinct());
        Assert.Single(contents.Select((c, i) => c.Text.Top - measure.Items[i].Bounds.Top).Distinct());
        Assert.All(contents, c => Assert.True(c.Text.Height >= font.Height));
        Assert.All(contents, c => Assert.True(c.Flags.HasFlag(TextFormatFlags.NoPadding)));
        for (int i = 0; i < contents.Length; i++)
        {
            int labelWidth = TextRenderer.MeasureText(group.Items[i].Text, font, new Size(1000, 1000), TextFormatFlags.SingleLine | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix).Width;
            // Width caps intentionally ellipsize exceptionally long labels.
            if (labelWidth + RibbonLayoutEngine.Scale(38, dpi) <= RibbonLayoutEngine.Scale(180, dpi))
                Assert.True(contents[i].Text.Width >= labelWidth);
        }
    });
    [Fact]
    public void Large_single_and_wrapped_labels_start_on_the_same_line() => FoundationTests.Sta(() =>
    {
        using var font = new Font("Segoe UI", 9);
        var a = RibbonContentLayout.Calculate(new(0, 0, 80, 100), RibbonItemSize.Large, false, true, 120, font);
        var b = RibbonContentLayout.Calculate(new(80, 0, 130, 100), RibbonItemSize.Large, false, true, 120, font);
        Assert.Equal(a.Icon.Top, b.Icon.Top);
        Assert.Equal(a.Text.Top, b.Text.Top);
        Assert.False(a.Flags.HasFlag(TextFormatFlags.VerticalCenter));
        using var configuration = new RibbonControl();
        Assert.Equal(108, configuration.Theme.LargeButtonMaxWidth);
        Assert.Throws<ArgumentOutOfRangeException>(() => configuration.Theme = configuration.Theme with { LargeButtonMaxWidth = 59 });
        using var dpiProbe = new Control();
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        foreach (int maximum in new[] { 60, 108, 160 })
        foreach (string label in new[] { "Paste", "Track changes", "Keep source formatting", "Keep the original document source formatting", "ExtraordinaryDocumentFormattingOptions", "First\nsecond\nthird", "文書の元の書式をすべて保持する", "🖉", "Re\u0301tablirLaMiseEnFormeOriginaleDuDocument", string.Concat(Enumerable.Repeat("e\u0301", 35)), string.Concat(Enumerable.Repeat("🖉", 35)) })
        {
            bool reportedHorizontal = label == "Track changes";
            using var captionFont = new Font("Segoe UI", 9f * dpi / dpiProbe.DeviceDpi);
            using var ribbon = new RibbonControl();
            ribbon.Theme = ribbon.Theme with { LargeButtonMaxWidth = maximum };
            int bodyHeight = ribbon.LayoutTheme.BodyHeight;
            using var tab = new RibbonTab();
            ribbon.Tabs.Add(tab);
            var group = new RibbonGroup(); tab.Groups.Add(group);
            RibbonItem[] items = reportedHorizontal
                ? [new RibbonSplitButtonItem { Text = "Track changes" }]
                : [new RibbonButtonItem(), new RibbonToggleItem(), new RibbonDropDownItem(),
                    new RibbonSplitButtonItem(), new RibbonSplitButtonItem { Layout = RibbonSplitButtonLayout.Vertical }];
            using var icon = new Bitmap(16, 16);
            foreach (var item in items)
            {
                item.Text = label;
                item.Image = icon; group.Items.Add(item);
            }
            Assert.Equal(bodyHeight, ribbon.LayoutTheme.BodyHeight);
            var engine = new RibbonLayoutEngine();
            int groupCaption = Math.Max(20, (int)Math.Ceiling(captionFont.Height * 96d / dpi) + 6);
            var theme = new RibbonTheme { BodyHeight = 78 + groupCaption + 14, LargeButtonMaxWidth = maximum };
            var measured = engine.Measure(group, 0, captionFont, dpi, theme);
            using var renderer = new CaptionRecorder();
            foreach (var entry in measured.Items)
            foreach (int state in new[] { 0, 1, 2 })
            {
                if (entry.Item is RibbonToggleItem toggle) toggle.Checked = state == 2;
                if (entry.Item is RibbonSplitButtonItem splitToggle) splitToggle.Checked = state == 2;
                using var bitmap = new Bitmap(entry.Bounds.Right + 4, entry.Bounds.Bottom + 4);
                using var graphics = Graphics.FromImage(bitmap);
                graphics.Clear(theme.Surface); renderer.Lines.Clear(); renderer.Arrow = Rectangle.Empty;
                var context = new RibbonPaintContext(graphics, entry.Bounds, theme, captionFont, dpi, Hot: state == 1);
                if (entry.Item is RibbonSplitButtonItem split)
                {
                    var parts = RibbonSplitLayout.Calculate(entry.Bounds, split, RibbonItemSize.Large, false, dpi);
                    renderer.DrawSplitItem(context with { Bounds = parts.Primary }, context with { Bounds = parts.DropDown, Hot = false }, split, RibbonItemSize.Large, false);
                }
                else renderer.DrawItem(context, entry.Item, RibbonItemSize.Large, false);
                Assert.InRange(renderer.Lines.Count, 1, 2);
                string Content(string value) => new(value.Where(c => !char.IsWhiteSpace(c)).ToArray());
                string visible = Content(string.Concat(renderer.Lines.Select(l => l.Text)));
                if (visible.EndsWith('…'))
                {
                    string prefix = visible[..^1];
                    Assert.StartsWith(prefix, Content(label));
                    Assert.True(System.Globalization.StringInfo.ParseCombiningCharacters(Content(label)).Contains(prefix.Length));
                }
                else Assert.Equal(Content(label), visible);
                Assert.Equal(label, entry.Item.Text);
                Assert.True(entry.Bounds.Width <= RibbonLayoutEngine.Scale(maximum, dpi));
                Assert.All(renderer.Lines, line => Assert.True(RibbonCaptionLayout.TextWidth(line.Text, captionFont) <= line.Bounds.Width));
                if (label == "Keep the original document source formatting" && maximum <= 108) Assert.EndsWith("…", renderer.Lines[^1].Text);
                var first = renderer.Lines[0]; var last = renderer.Lines[^1];
                Assert.True(last.Bounds.Top - first.Bounds.Top < captionFont.Height);
                Assert.True(last.Bounds.Bottom <= entry.Bounds.Bottom - RibbonLayoutEngine.Scale(3, dpi));
                Assert.Equal(entry.Bounds.Top + RibbonLayoutEngine.Scale(45, dpi), first.Bounds.Top);
                if (entry.Item is RibbonDropDownItem && entry.Item is not RibbonSplitButtonItem { Layout: RibbonSplitButtonLayout.Horizontal })
                {
                    if (renderer.Lines.Count == 2 || entry.Item is not RibbonSplitButtonItem)
                    {
                        Assert.Equal(last.Bounds.Right, renderer.Arrow.Left);
                        Assert.Equal(last.Bounds.Top, renderer.Arrow.Top);
                        Assert.True(renderer.Arrow.Right <= entry.Bounds.Right - RibbonLayoutEngine.Scale(5, dpi));
                    }
                    else Assert.True(renderer.Arrow.Top >= last.Bounds.Bottom);
                }
                // Compare the complete second glyph row with an unconstrained
                // native line, including descenders, rather than accepting bounds alone.
                using var reference = new Bitmap(last.Bounds.Width, captionFont.Height + 10);
                using var referenceGraphics = Graphics.FromImage(reference);
                var background = bitmap.GetPixel(last.Bounds.Left, last.Bounds.Top);
                referenceGraphics.Clear(background);
                TextRenderer.DrawText(referenceGraphics, last.Text, captionFont, new Rectangle(0, 0, reference.Width, reference.Height), theme.Foreground, last.Flags);
                for (int y = 0; y < captionFont.Height; y++)
                    for (int x = 0; x < reference.Width; x++)
                        Assert.True(reference.GetPixel(x, y) == background || reference.GetPixel(x, y) == bitmap.GetPixel(last.Bounds.Left + x, last.Bounds.Top + y),
                            $"DPI={dpi}, kind={entry.Item.GetType().Name}, text={entry.Item.Text}, state={state}, cell=({x},{y}), first={first.Bounds}, last={last.Bounds}, expected={reference.GetPixel(x,y)}, actual={bitmap.GetPixel(last.Bounds.Left+x,last.Bounds.Top+y)}");
            }
        }
    });
    [Fact]
    public void Detached_trees_reject_duplicate_ids_and_dispose_children_cleanly()
    {
        using var group = new RibbonGroup { Id = "group" };
        var first = new RibbonButtonItem { Id = "first" };
        group.Items.Add(first);
        Assert.Throws<ArgumentException>(() => group.Items.Add(new RibbonButtonItem { Id = "first" }));
        Assert.Throws<ArgumentException>(() => first.Id = "group");
        first.Dispose();
        Assert.Empty(group.Items);
    }
    [Fact]
    public void Removing_or_hiding_selected_tab_uses_next_then_previous() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl();
        var a = new RibbonTab();
        var b = new RibbonTab();
        var c = new RibbonTab();
        ribbon.Tabs.AddRange(a, b, c);
        ribbon.SelectedTab = b;
        ribbon.Tabs.Remove(b);
        Assert.Same(c, ribbon.SelectedTab);
        c.Visible = false;
        Assert.Same(a, ribbon.SelectedTab);
        a.Enabled = false;
        Assert.Null(ribbon.SelectedTab);
    });
    [Fact]
    public void Large_keytip_levels_terminate_and_remain_prefix_free()
    {
        var input = Enumerable.Range(0, 80).Select(i => ("Command", i < 36 ? "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"[i].ToString() : "")).ToArray();
        var result = KeyTipResolver.Resolve(input);
        Assert.Equal(80, result.Distinct().Count());
        foreach (var a in result)
            foreach (var b in result)
                if (a != b)
                    Assert.False(b.StartsWith(a, StringComparison.Ordinal));
    }
    private sealed class CaptionRecorder : RibbonOffice2024Renderer
    {
        internal readonly List<(string Text, Rectangle Bounds, TextFormatFlags Flags)> Lines = [];
        internal Rectangle Arrow;
        public override void DrawDropDownArrow(RibbonPaintContext context, Color color)
        { Arrow = context.Bounds; base.DrawDropDownArrow(context, color); }
        public override void DrawText(RibbonPaintContext context, string text, Color color, TextFormatFlags flags)
        { Lines.Add((text, context.Bounds, flags)); base.DrawText(context, text, color, flags); }
    }
}
