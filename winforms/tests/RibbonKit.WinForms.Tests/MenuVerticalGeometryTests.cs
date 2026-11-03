using System.Drawing.Imaging;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using RibbonKit.WinForms.Design;
using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class MenuVerticalGeometryTests
{
    private static RibbonTheme Horizontal => RibbonThemes.Office2024Light with
    { MenuMinimumRowHeight = 64, MenuIconSize = 36, MenuLeadingPadding = 14, MenuTrailingPadding = 18, MenuIconTextGap = 16 };
    private static RibbonTheme Vertical => RibbonThemes.Office2024Light with
    { MenuIconVerticalPadding = 14, MenuDescriptionVerticalPadding = 14, MenuDescriptionGap = 10, MenuSeparatorHeight = 20 };
    private static RibbonTheme Reset(RibbonTheme theme) => theme with
    { MenuIconVerticalPadding = null, MenuDescriptionVerticalPadding = null, MenuDescriptionGap = null, MenuSeparatorHeight = null };

    [Fact]
    public void Defaults_and_accepted_horizontal_overrides_preserve_their_paint_and_measurements() => FoundationTests.Sta(() =>
    {
        var capture = Environment.GetEnvironmentVariable("RIBBONKIT_MENU_VERTICAL_CAPTURE");
        var baseline = Environment.GetEnvironmentVariable("RIBBONKIT_MENU_VERTICAL_BASELINE");
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        foreach (var density in new[] { RibbonDensity.Compact, RibbonDensity.Touch })
        foreach (bool office in new[] { true, false })
        foreach (int points in new[] { 9, 14 })
        foreach (bool horizontal in new[] { false, true })
        {
            using var font = new Font("Segoe UI", points * dpi / 96f);
            using RibbonRenderer renderer = office ? new RibbonOffice2024Renderer() : new RibbonRenderer();
            using var menu = new RibbonDropDownItem();
            menu.Items.AddRange(new RibbonButtonItem { Text = "Open document", Icon = RibbonStockIcons.Create("open"), MenuDescription = "Keep the original formatting and document layout." },
                new RibbonMenuSeparatorItem(), new RibbonCheckBoxItem { Text = "Keep formatting", Checked = true, MenuDescription = "Preserve fonts and paragraph spacing." },
                new RibbonTextBoxItem { Text = "Name", Value = "Retained editor" });
            var theme = horizontal ? Horizontal : RibbonThemes.Office2024Light;
            int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
            int width = S(350), top = S(4);
            var heights = RibbonMenuLayout.RowHeights(menu.Items.ToArray(), width - S(8), font, dpi, density, theme: theme);
            var rows = heights.Select(h => { var row = new Rectangle(S(4), top, width - S(8), h); top += h; return row; }).ToArray();
            using var pixels = new Bitmap(width, top + S(4));
            using (var graphics = Graphics.FromImage(pixels))
            {
                var context = new RibbonPaintContext(graphics, new(Point.Empty, pixels.Size), theme, font, dpi) { Density = density };
                renderer.DrawSurface(context, true); renderer.DrawPopupFrame(context);
                for (int i = 0; i < rows.Length; i++) renderer.DrawItem(context with { Bounds = rows[i] }, menu.Items[i], RibbonItemSize.Medium, true);
            }
            using var stream = new MemoryStream(); pixels.Save(stream, ImageFormat.Png);
            string name = $"{(office ? "office" : "flat")}-{density}-{dpi}-{points}-{(horizontal ? "horizontal" : "default")}";
            string measurements = string.Join("\n", rows.Select(r => r.ToString()));
            string hash = Convert.ToHexString(SHA256.HashData(stream.ToArray()));
            if (capture is not null)
            {
                Directory.CreateDirectory(capture); File.WriteAllBytes(Path.Combine(capture, name + ".png"), stream.ToArray());
                File.WriteAllText(Path.Combine(capture, name + ".txt"), measurements); File.WriteAllText(Path.Combine(capture, name + ".sha256"), hash);
            }
            if (baseline is not null)
            {
                Assert.Equal(File.ReadAllText(Path.Combine(baseline, name + ".txt")), measurements);
                Assert.Equal(File.ReadAllText(Path.Combine(baseline, name + ".sha256")), hash);
            }
            Assert.Equal(S(9), heights[1]); Assert.Equal(heights[0], heights[2]); Assert.Equal(heights[0], heights[3]);
        }
    });

    [Fact]
    public void Explicit_padding_reserves_complete_icon_and_description_clearance_at_fractional_DPI() => FoundationTests.Sta(() =>
    {
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        foreach (var density in new[] { RibbonDensity.Compact, RibbonDensity.Touch })
        foreach (bool office in new[] { true, false })
        foreach (int points in new[] { 9, 24 })
        {
            int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
            using var font = new Font("Segoe UI", points * dpi / 96f);
            var theme = Vertical with { MenuIconSize = 36 };
            using var item = new RibbonButtonItem { Text = "Open", Icon = RibbonStockIcons.Create("open"), MenuDescription = "First line\nSecond line" };
            int height = RibbonMenuLayout.RowHeight(item, S(350), font, dpi, density, theme: theme);
            var record = new Slots(); using RibbonRenderer renderer = office ? new Office(record) : new Flat(record);
            using var pixels = new Bitmap(S(350), height); using var graphics = Graphics.FromImage(pixels);
            var row = new Rectangle(Point.Empty, pixels.Size);
            renderer.DrawItem(new(graphics, row, theme, font, dpi) { Density = density }, item, RibbonItemSize.Medium, true);
            Assert.Equal(S(14), record.Texts[item.Text].Top);
            var description = record.Texts[item.MenuDescription];
            Assert.Equal(record.Texts[item.Text].Bottom + S(10), description.Top);
            int measured = Math.Min(2 * font.Height, TextRenderer.MeasureText(item.MenuDescription, font, new(description.Width, int.MaxValue), RibbonMenuLayout.DescriptionFlags).Height);
            Assert.True(description.Height >= measured, $"Full measured two-line description at {dpi}/{density}/{points}: {description.Height} < {measured}");
            Assert.True(description.Bottom <= row.Bottom - S(14));
            item.MenuDescription = "";
            height = RibbonMenuLayout.RowHeight(item, S(350), font, dpi, density, theme: theme);
            var content = RibbonMenuLayout.Content(default, item, new(0, 0, S(350), height), font, dpi, density, theme);
            Assert.True(content.Icon.Top >= S(14), $"Top icon clearance at {dpi}/{density}/{points}: {content.Icon.Top}");
            Assert.True(height - content.Icon.Bottom >= S(14));
        }
    });

    [Fact]
    public void Invalid_values_are_atomic_and_individual_reset_preserves_horizontal_settings_and_theme_selection() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { Theme = Horizontal }; using var other = new RibbonControl();
        var application = ribbon.Theme; ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2024Light;
        foreach (var invalid in new[] { Vertical with { MenuIconVerticalPadding = -1 }, Vertical with { MenuIconVerticalPadding = 1025 },
            Vertical with { MenuDescriptionVerticalPadding = -1 }, Vertical with { MenuDescriptionVerticalPadding = 1025 },
            Vertical with { MenuDescriptionGap = -1 }, Vertical with { MenuDescriptionGap = 1025 },
            Vertical with { MenuSeparatorHeight = 0 }, Vertical with { MenuSeparatorHeight = 1025 } })
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ribbon.Theme = invalid);
            Assert.Same(RibbonThemes.Office2024Light, ribbon.Theme); Assert.Equal(RibbonBuiltInTheme.Office2024Light, ribbon.BuiltInTheme);
        }
        ribbon.BuiltInTheme = null; Assert.Same(application, ribbon.Theme);
        ribbon.Theme = Horizontal with { MenuIconVerticalPadding = 14, MenuDescriptionVerticalPadding = 14, MenuDescriptionGap = 10, MenuSeparatorHeight = 20 };
        ribbon.Theme = Reset(ribbon.Theme); Assert.Equal(Horizontal, ribbon.Theme); Assert.Null(other.Theme.MenuDescriptionGap);
        ribbon.Theme = Vertical with { MenuIconVerticalPadding = 0, MenuDescriptionVerticalPadding = 0, MenuDescriptionGap = 0, MenuSeparatorHeight = 1 };
        Assert.Equal(0, RibbonMenuMetrics.For(RibbonDensity.Touch, ribbon.Theme).MenuDescriptionVerticalPadding);
        Assert.True(RibbonMenuMetrics.For(RibbonDensity.Touch, ribbon.Theme).MenuRow >= 44);
        Assert.Equal(DesignerSerializationVisibility.Hidden, TypeDescriptor.GetProperties(ribbon)[nameof(ribbon.Theme)]!.SerializationVisibility);
    });

    [Fact]
    public void Separator_rows_keep_one_device_pixel_of_ink_at_their_measured_center() => FoundationTests.Sta(() =>
    {
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        foreach (bool office in new[] { true, false })
        foreach (int logicalHeight in new[] { 1, 20 })
        {
            int S(int n) => RibbonLayoutEngine.Scale(n, dpi); using var separator = new RibbonMenuSeparatorItem();
            using RibbonRenderer renderer = office ? new RibbonOffice2024Renderer() : new RibbonRenderer();
            using var pixels = new Bitmap(S(200), S(logicalHeight)); using var graphics = Graphics.FromImage(pixels); graphics.Clear(Color.White);
            renderer.DrawMenuSeparator(new(graphics, new(Point.Empty, pixels.Size), Vertical with { MenuSeparatorHeight = logicalHeight }, Control.DefaultFont, dpi), separator);
            var rows = Enumerable.Range(0, pixels.Height).Where(y => pixels.GetPixel(pixels.Width / 2, y).ToArgb() == Vertical.Border.ToArgb()).ToArray();
            Assert.Equal(new[] { (pixels.Height - 1) / 2 }, rows);
        }
    });

    [Fact]
    public void Live_rows_keep_native_editors_keyboard_UIA_and_checkbox_retention_then_reset_exactly() => FoundationTests.Sta(() =>
    {
        using var form = new Form { Width = 1000, Height = 700 };
        var ribbon = new RibbonControl { Theme = Horizontal, AnimationLevel = RibbonAnimationLevel.None }; form.Controls.Add(ribbon);
        var tab = new RibbonTab(); var group = new RibbonGroup(); var menu = new RibbonDropDownItem { Text = "Menu" };
        var action = new RibbonButtonItem { Text = "Open", MenuDescription = "First line\nSecond line" };
        var separator = new RibbonMenuSeparatorItem(); var check = new RibbonCheckBoxItem { Text = "Keep formatting" };
        var field = new RibbonTextBoxItem { Text = "Name", Value = "Retained" }; menu.Items.AddRange(action, separator, check, field);
        group.Items.Add(menu); tab.Groups.Add(group); ribbon.Tabs.Add(tab); form.Show(); Application.DoEvents(); menu.ShowDropDown();
        var before = ribbon.Popups.Top!; var originalSize = before.Size; var originalRows = before.Targets.Select(t => t.Bounds).ToArray();
        var editor = ribbon.Editors.EditorFor(field)!; nint handle = editor.Handle; editor.Text = "Retained text";
        ribbon.Theme = Horizontal with { MenuIconVerticalPadding = 14, MenuDescriptionVerticalPadding = 14, MenuDescriptionGap = 10, MenuSeparatorHeight = 20 };
        Assert.True(before.IsDisposed); menu.ShowDropDown(); var popup = ribbon.Popups.Top!;
        Assert.Equal(handle, editor.Handle); Assert.Equal("Retained text", editor.Text); Assert.True(popup.Height > originalSize.Height);
        var separatorRow = popup.Targets.Single(t => t.Element == separator); Assert.False(separatorRow.Enabled);
        Assert.Equal(RibbonLayoutEngine.Scale(20, ribbon.DeviceDpi), separatorRow.Bounds.Height);
        Assert.DoesNotContain(popup.AutomationRoot.Children, node => node.Element == separator);
        var point = new Point(separatorRow.Bounds.Left + 20, separatorRow.Bounds.Top + separatorRow.Bounds.Height / 2);
        nint position = (nint)(point.Y << 16 | point.X); SendMessage(popup.Handle, 0x201, 1, position); SendMessage(popup.Handle, 0x202, 0, position);
        Assert.Same(popup, ribbon.Popups.Top); popup.FocusElement(action);
        var down = Message.Create(popup.Handle, 0x100, (nint)Keys.Down, 0); Assert.True(popup.PreProcessMessage(ref down)); Assert.Same(check, popup.FocusedElement);
        var space = Message.Create(popup.Handle, 0x100, (nint)Keys.Space, 0); Assert.True(popup.PreProcessMessage(ref space)); Assert.True(check.Checked); Assert.Same(popup, ribbon.Popups.Top);
        var target = popup.Targets.Single(t => t.Element == check); var screen = popup.RectangleToScreen(target.VisibleBounds); var uia = popup.AutomationRoot.Node(check).BoundingRectangle;
        Assert.Equal(screen.Left, uia.Left); Assert.Equal(screen.Top, uia.Top); Assert.Equal(screen.Height, uia.Height);
        Assert.True(popup.Targets.Single(t => t.Element == field).Bounds.Contains(editor.Parent!.Bounds));
        ribbon.Theme = Reset(ribbon.Theme); menu.ShowDropDown(); Assert.Equal(Horizontal, ribbon.Theme);
        Assert.Equal(originalSize, ribbon.Popups.Top!.Size); Assert.Equal(originalRows, ribbon.Popups.Top.Targets.Select(t => t.Bounds));
        Assert.Equal(handle, editor.Handle); Assert.Equal("Retained text", editor.Text);
    });

    [Theory] [InlineData(false)] [InlineData(true)]
    public void Described_rows_stay_reachable_with_workarea_scrolling(bool overflow) => FoundationTests.Sta(() =>
    {
        using var form = new Form { Width = 900, Height = 650 }; var ribbon = new RibbonControl { Theme = Vertical, QuickAccessMaxWidth = 28 }; form.Controls.Add(ribbon);
        var tab = new RibbonTab(); var group = new RibbonGroup(); var menu = new RibbonDropDownItem(); group.Items.Add(menu); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        var commands = Enumerable.Range(0, 20).Select(i => new RibbonButtonItem { Text = "Command " + i, MenuDescription = "First line\nSecond line" }).ToArray();
        if (overflow) ribbon.QuickAccessItems.AddRange(commands); else menu.Items.AddRange(commands);
        var area = new Size(360, 280); using var popup = new RibbonPopupSurface(ribbon, overflow ? ribbon.QuickAccessOverflow : menu, area);
        form.Controls.Add(popup); form.Show(); Application.DoEvents(); Assert.True(popup.Width <= area.Width); Assert.True(popup.Height <= area.Height);
        popup.FocusElement(commands[^1]); Assert.False(popup.Targets[^1].VisibleBounds.IsEmpty);
        if (overflow) Assert.True(popup.OverflowViewport.Contains(popup.Targets[^1].Bounds));
        popup.FocusElement(commands[0]); Assert.False(popup.Targets[0].VisibleBounds.IsEmpty);
    });

    [Fact]
    public void Illustrated_choices_and_application_menu_metrics_remain_independent() => FoundationTests.Sta(() =>
    {
        foreach (var density in new[] { RibbonDensity.Compact, RibbonDensity.Touch })
        foreach (bool office in new[] { true, false })
        {
            using var menu = new RibbonDropDownItem { MenuPresentation = RibbonMenuPresentation.IllustratedChoices };
            var item = new RibbonButtonItem { Text = "Document", MenuDescription = "First line\nSecond line" }; menu.Items.Add(item);
            using var separator = new RibbonMenuSeparatorItem(); var slots = new Slots(); using RibbonRenderer renderer = office ? new Office(slots) : new Flat(slots);
            using var pixels = new Bitmap(400, 150); using var graphics = Graphics.FromImage(pixels); using var font = new Font("Segoe UI", 9);
            var context = new RibbonPaintContext(graphics, new(Point.Empty, pixels.Size), RibbonThemes.Office2024Light, font, 120) { Density = density };
            renderer.DrawItem(context, item, RibbonItemSize.Medium, true); var title = slots.Texts[item.Text]; var description = slots.Texts[item.MenuDescription];
            renderer.DrawItem(context with { Theme = Vertical }, item, RibbonItemSize.Medium, true);
            Assert.Equal(title, slots.Texts[item.Text]); Assert.Equal(description, slots.Texts[item.MenuDescription]);
            Assert.Equal(RibbonMenuLayout.RowHeight(item, 400, font, 120, density), RibbonMenuLayout.RowHeight(item, 400, font, 120, density, theme: Vertical));
            Assert.Equal(RibbonLayoutEngine.Scale(9, 120), RibbonApplicationMenuMetrics.RowHeight(separator, 400, font, 120, density, true));
            Assert.Equal(RibbonLayoutEngine.Scale(20, 120), RibbonMenuLayout.RowHeight(separator, 400, font, 120, density, theme: Vertical));
        }
    });

    [Fact]
    public void Inert_designer_preview_uses_the_same_theme_rows_without_creating_native_editors_or_transactions() => FoundationTests.Sta(() =>
    {
        using var host = new DesignerAuthoringTests.Host(); using var ribbon = DesignerAuthoringTests.MakeRibbon(host); ribbon.Theme = Vertical;
        var menu = new RibbonDropDownItem { Text = "Menu" }; var item = new RibbonButtonItem { Text = "Open", MenuDescription = "First line\nSecond line" };
        var separator = new RibbonMenuSeparatorItem(); var field = new RibbonTextBoxItem { Text = "Name", Value = "Retained" }; menu.Items.AddRange(item, separator, field);
        ribbon.Tabs[0].Groups[0].Items.Add(menu);
        var keys = new Dictionary<RibbonElement, string> { [menu] = "menu", [item] = "item", [separator] = "separator", [field] = "field" };
        var preview = RibbonPopupPreviewRenderer.Render(ribbon, menu, "menu", "", keys);
        Assert.Equal(RibbonLayoutEngine.Scale(20, ribbon.DeviceDpi), preview.Hits.Single(h => h.Key == "separator").Height);
        Assert.Equal(preview.Hits.Single(h => h.Key == "item").Height, preview.Hits.Single(h => h.Key == "field").Height);
        Assert.False(menu.IsDropDownOpen); Assert.Null(ribbon.Popups.Top); Assert.Null(ribbon.Editors.EditorFor(field)); Assert.Equal(0, host.Transactions);
        Assert.Equal("Retained", field.Value); Assert.Equal(Vertical, ribbon.Theme);
    });

    [DllImport("user32.dll")] private static extern nint SendMessage(nint window, uint message, nint wParam, nint lParam);

    private sealed class Slots { internal readonly Dictionary<string, Rectangle> Texts = []; }
    private sealed class Office(Slots r) : RibbonOffice2024Renderer
    {
        public override void DrawText(RibbonPaintContext c, string text, Color color, TextFormatFlags flags) { r.Texts[text] = c.Bounds; base.DrawText(c, text, color, flags); }
    }
    private sealed class Flat(Slots r) : RibbonRenderer
    {
        public override void DrawText(RibbonPaintContext c, string text, Color color, TextFormatFlags flags) { r.Texts[text] = c.Bounds; base.DrawText(c, text, color, flags); }
    }
}
