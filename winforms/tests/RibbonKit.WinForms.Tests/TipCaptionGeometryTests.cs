using System.Text.Json;
using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class TipCaptionGeometryTests
{
    private static RibbonTheme Roomier => RibbonThemes.Office2024Light with { ScreenTipMaximumWidth = 420, ScreenTipContentPadding = 20, ScreenTipTitleGap = 12, ScreenTipPlacementGap = 16,
        KeyTipHorizontalPadding = 6, KeyTipVerticalPadding = 4, CaptionLeadingPadding = 22, CaptionIconTextGap = 18, CaptionQuickAccessTextGap = 18, CaptionTitleReservationWidth = 180 };
    public static TheoryData<string, bool> Roles => new()
    {
        { nameof(RibbonTheme.ScreenTipMaximumWidth), true }, { nameof(RibbonTheme.ScreenTipContentPadding), false }, { nameof(RibbonTheme.ScreenTipTitleGap), false }, { nameof(RibbonTheme.ScreenTipPlacementGap), false },
        { nameof(RibbonTheme.KeyTipHorizontalPadding), false }, { nameof(RibbonTheme.KeyTipVerticalPadding), false }, { nameof(RibbonTheme.CaptionLeadingPadding), false }, { nameof(RibbonTheme.CaptionIconTextGap), false },
        { nameof(RibbonTheme.CaptionQuickAccessTextGap), false }, { nameof(RibbonTheme.CaptionTitleReservationWidth), true }
    };
    [Theory] [MemberData(nameof(Roles))]
    public void Optional_roles_validate_atomically_and_reset_independently(string name, bool width) => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl(); using var other = new RibbonControl(); ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2024Light;
        var original = ribbon.Theme; var property = typeof(RibbonTheme).GetProperty(name)!;
        foreach (int invalid in new[] { width ? 0 : -1, 1025 })
        {
            var bad = Roomier with { }; property.SetValue(bad, invalid); Assert.Throws<ArgumentOutOfRangeException>(() => ribbon.Theme = bad);
            Assert.Same(original, ribbon.Theme); Assert.Equal(RibbonBuiltInTheme.Office2024Light, ribbon.BuiltInTheme);
        }
        foreach (int value in new[] { width ? 1 : 0, 1024 })
        {
            var valid = Roomier with { GalleryContentPadding = 10, BackstagePagePadding = 18, CommandContentVerticalPadding = 16 }; property.SetValue(valid, value); ribbon.Theme = valid;
            var reset = valid with { }; property.SetValue(reset, null); ribbon.Theme = reset;
            foreach (var row in Roles) Assert.Equal((string)row[0] == name ? null : typeof(RibbonTheme).GetProperty((string)row[0])!.GetValue(valid), typeof(RibbonTheme).GetProperty((string)row[0])!.GetValue(ribbon.Theme));
            Assert.Same(original, other.Theme); Assert.Equal(10, ribbon.Theme.GalleryContentPadding); Assert.Equal(18, ribbon.Theme.BackstagePagePadding); Assert.Equal(16, ribbon.Theme.CommandContentVerticalPadding);
        }
    });

    [Theory] [InlineData(false)] [InlineData(true)]
    public void Tip_content_and_placement_fit_width_font_and_work_area_limits(bool described) => FoundationTests.Sta(() =>
    {
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        foreach (int points in new[] { 9, 24 })
        foreach (int width in new[] { 1, 31, 190, 900 })
        foreach (int padding in new[] { 0, 20, 1024 })
        {
            int S(int n) => RibbonLayoutEngine.Scale(n, dpi); using var font = new Font("Segoe UI", points * dpi / 96f);
            var theme = Roomier with { ScreenTipContentPadding = padding }; var available = new Size(S(width), S(450));
            var tip = RibbonScreenTipLayout.Measure("Find matching text throughout the document", described ? "Search without changing the document or native selection." : "", font, dpi, available, theme);
            var bounds = new Rectangle(Point.Empty, tip.Size); Assert.InRange(tip.Size.Width, 1, Math.Min(S(420), available.Width)); Assert.True(tip.Size.Height <= available.Height);
            Assert.True(bounds.Contains(tip.Title)); if (!tip.Description.IsEmpty) { Assert.True(bounds.Contains(tip.Description)); Assert.Equal(S(12), tip.Description.Top - tip.Title.Bottom); }
            var work = new Rectangle(-600, -300, available.Width, available.Height);
            foreach (var anchor in new[] { new Rectangle(work.Left, work.Top, 20, 20), new Rectangle(work.Right - 20, work.Bottom - 20, 20, 20) })
                Assert.True(work.Contains(RibbonScreenTipLayout.Place(anchor, anchor.Location, tip.Size, work, dpi, theme)));
            var roomy = RibbonScreenTipLayout.Measure("Title", "Description", font, dpi, new(S(900), S(600)), Roomier);
            var placed = RibbonScreenTipLayout.Place(new(0, 0, S(50), S(30)), Point.Empty, roomy.Size, new(0, 0, S(900), S(600)), dpi, Roomier);
            Assert.Equal(S(46), placed.Top);
        }
    });

    [Theory] [InlineData(false)] [InlineData(true)]
    public void Root_popup_and_backstage_badges_use_padding_without_changing_keyboard_targets(bool office) => FoundationTests.Sta(() =>
    {
        var record = new BadgeRecord(); using RibbonRenderer painter = office ? new Office(record) : new Neutral(record);
        using var ribbon = new RibbonControl { Renderer = painter, Theme = Roomier, Width = 900, AnimationLevel = RibbonAnimationLevel.None };
        var item = new RibbonButtonItem { Text = "Open", KeyTip = "AB" }; var tab = new RibbonTab(); var group = new RibbonGroup(); group.Items.Add(item); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        using var bitmap = new Bitmap(900, 200); using var graphics = Graphics.FromImage(bitmap); graphics.Clear(Color.White);
        var target = new RibbonTarget(TargetKind.Item, item, new(30, 30, 180, 80), new(0, 0, 900, 200), RibbonItemSize.Medium);
        ribbon.DrawTips(graphics, [target]); int S(int n) => RibbonLayoutEngine.Scale(n, ribbon.DeviceDpi); var measured = TextRenderer.MeasureText("AB", ribbon.Font);
        Assert.Equal(measured.Width + 2 * S(6), record.Bounds.Width); Assert.Equal(measured.Height + 2 * S(4), record.Bounds.Height); Assert.True(target.Bounds.Contains(record.Bounds));
        var original = target.Bounds; ribbon.Theme = Roomier with { KeyTipHorizontalPadding = 1024, KeyTipVerticalPadding = 1024 }; ribbon.DrawTips(graphics, [target]); Assert.Equal(original, record.Bounds);
        ribbon.Theme = Roomier; using var popup = new RibbonPopupSurface(ribbon, group, new(900, 600));
        using var popupImage = new Bitmap(popup.Width, popup.Height); popup.DrawToBitmap(popupImage, popup.ClientRectangle); ribbon.DrawTips(graphics, popup.Targets);
        Assert.Equal(measured.Width + 2 * S(6), record.Bounds.Width);
        ribbon.Theme = RibbonThemes.Office2024Light;
        using var actual = new Bitmap(900, 200); using (var g = Graphics.FromImage(actual)) { g.Clear(Color.White); ribbon.DrawTips(g, [target]); }
        using var expected = new Bitmap(900, 200); using (var g = Graphics.FromImage(expected))
        {
            g.Clear(Color.White); var badge = new Rectangle(target.Bounds.Left + (target.Bounds.Width - measured.Width) / 2, target.Bounds.Bottom - measured.Height, measured.Width, measured.Height);
            painter.DrawKeyTip(new(g, badge, ribbon.Theme, ribbon.Font, ribbon.DeviceDpi), "AB");
        }
        using var a = new MemoryStream(); using var b = new MemoryStream(); actual.Save(a, System.Drawing.Imaging.ImageFormat.Png); expected.Save(b, System.Drawing.Imaging.ImageFormat.Png); Assert.Equal(b.ToArray(), a.ToArray());
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        {
            var metrics = new RibbonTipMetrics(null); var text = new Size(20, 16);
            Assert.Equal(text, metrics.BadgeSize(text, dpi)); Assert.Equal(new Size(20 + RibbonLayoutEngine.Scale(8, dpi), 16 + RibbonLayoutEngine.Scale(4, dpi)), metrics.BadgeSize(text, dpi, true));
        }
        if (Environment.GetEnvironmentVariable("RIBBONKIT_TIP_CAPTION_CAPTURE") is { Length: > 0 } output)
        { Directory.CreateDirectory(output); bitmap.Save(Path.Combine(output, $"keytip-override-{office}.png")); }
    });

    [Fact]
    public void Passive_tip_and_backstage_badges_retain_native_focus_selection_and_handles() => FoundationTests.Sta(() =>
    {
        using var form = new Form { Width = 1100, Height = 720 }; var record = new BadgeRecord(); using var painter = new Office(record);
        var ribbon = new RibbonControl { Theme = Roomier, Renderer = painter, AnimationLevel = RibbonAnimationLevel.None }; form.Controls.Add(ribbon);
        var item = new RibbonButtonItem { Text = "Open document", ScreenTipDescription = "Search without changing the document." }; var tab = new RibbonTab(); var group = new RibbonGroup(); group.Items.Add(item); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        using var body = new UserControl(); var input = new TextBox { Text = "Retained native draft" }; body.Controls.Add(input);
        var page = new RibbonBackstagePageItem { Text = "Information", KeyTip = "AB", Content = body }; ribbon.Backstage.Items.Add(page); ribbon.FileMenuMode = RibbonFileMenuMode.Backstage; ribbon.FileMenu.Visible = true;
        form.Show(); Application.DoEvents(); ribbon.IsBackstageOpen = true; Application.DoEvents(); input.Focus(); input.Select(2, 4);
        nint handle = input.Handle; var parent = body.Parent; var surface = ribbon.BackstageSurface!; surface.ToggleKeyTips();
        using var pixels = new Bitmap(surface.Width, surface.Height); surface.DrawToBitmap(pixels, surface.ClientRectangle);
        var entry = Descendants(surface).Single(c => c.Text == "Information"); int S(int n) => RibbonLayoutEngine.Scale(n, ribbon.DeviceDpi);
        var measured = TextRenderer.MeasureText("AB", entry.Font, Size.Empty, TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
        Assert.Equal(measured.Width + 2 * S(6), record.Bounds.Width); Assert.Equal(measured.Height + 2 * S(4), record.Bounds.Height);
        using var tip = new RibbonScreenTipWindow(ribbon, item, new(200, 200, 100, 30), new(210, 210)); tip.Show(form);
        Assert.True(input.Focused); Assert.Equal(handle, input.Handle); Assert.Same(parent, body.Parent); Assert.Equal(2, input.SelectionStart); Assert.Equal(4, input.SelectionLength);
        Assert.Same(page, ribbon.Backstage.SelectedPage); tip.Dispose(); Assert.True(input.Focused);
        ribbon.Theme = ribbon.Theme with { ScreenTipContentPadding = null, KeyTipHorizontalPadding = null, KeyTipVerticalPadding = null }; Assert.Equal(handle, input.Handle); Assert.Same(parent, body.Parent);
        static IEnumerable<Control> Descendants(Control root) => root.Controls.Cast<Control>().SelectMany(c => new[] { c }.Concat(Descendants(c)));
    });

    [Theory] [InlineData(RibbonDensity.Compact)] [InlineData(RibbonDensity.Touch)]
    public void Caption_spacing_keeps_system_buttons_native_handles_and_qat_identity(RibbonDensity density) => FoundationTests.Sta(() =>
    {
        using var form = new RibbonForm { Width = 1000, Height = 650 }; var ribbon = new RibbonControl { Density = density, AnimationLevel = RibbonAnimationLevel.None }; form.Controls.Add(ribbon); form.Ribbon = ribbon;
        var tab = new RibbonTab(); var group = new RibbonGroup(); var field = new RibbonTextBoxItem { Text = "Name", Value = "Retained draft" }; var source = new RibbonButtonItem { Text = "Save" };
        group.Items.AddRange(field, source); tab.Groups.Add(group); ribbon.Tabs.Add(tab); ribbon.QuickAccessItems.Add(new RibbonQuickAccessReference { Source = source }); ribbon.QuickAccessMaxWidth = 900; ribbon.QuickAccessPosition = RibbonQuickAccessPosition.TitleBar;
        form.Show(); Application.DoEvents(); var editor = ribbon.Editors.EditorFor(field)!; nint handle = editor.Handle, window = form.Handle;
        var before = form.Caption; var slot = form.QuickAccessSlot(ribbon); var entries = ribbon.QuickAccessItems.ToArray(); ribbon.Theme = Roomier; Application.DoEvents();
        int S(int n) => RibbonLayoutEngine.Scale(n, ribbon.DeviceDpi); Assert.Equal(S(22), form.Caption.Icon.Left); Assert.Equal(form.Caption.Icon.Right + S(18), form.Caption.ContentLeft);
        Assert.Equal(before.Targets, form.Caption.Targets); Assert.True(form.QuickAccessSlot(ribbon).Width < slot.Width); Assert.Equal(window, form.Handle); Assert.Equal(handle, editor.Handle); Assert.Equal("Retained draft", editor.Text);
        Assert.Equal(ribbon.Snapshot.QuickAccessBounds.Right + S(18), form.CaptionTextBounds(ribbon).Left); Assert.Equal(form.Caption.Buttons.Left, form.CaptionTextBounds(ribbon).Right);
        foreach (var target in ribbon.Snapshot.Targets.Where(t => t.Kind == TargetKind.QuickAccess)) Assert.Equal(1, form.CaptionHitTest(new(target.Bounds.Left + target.Bounds.Width / 2, target.Bounds.Top + target.Bounds.Height / 2)));
        foreach (var (bounds, hit) in form.Caption.Targets) Assert.Equal(hit, form.CaptionHitTest(new(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2)));
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        foreach (int width in new[] { 0, 19, 90, 800 })
        {
            int Scale(int n) => RibbonLayoutEngine.Scale(n, dpi);
            var measured = RibbonCaptionGeometry.Measure(Scale(width), Scale(36), dpi, true, true, true, true, Roomier with { CaptionLeadingPadding = 1024, CaptionIconTextGap = 1024 });
            Assert.True(measured.ContentLeft <= measured.Buttons.Left); Assert.True(measured.Icon.IsEmpty || measured.Icon.Right <= measured.Buttons.Left);
        }
        ribbon.Theme = RibbonThemes.Office2024Light; Assert.Equal(before.Icon, form.Caption.Icon); Assert.Equal(before.ContentLeft, form.Caption.ContentLeft); Assert.Equal(slot, form.QuickAccessSlot(ribbon)); Assert.Equal(entries, ribbon.QuickAccessItems);
    });

    private sealed class BadgeRecord { internal Rectangle Bounds; }
    private sealed class Office(BadgeRecord record) : RibbonOffice2024Renderer
    { public override void DrawKeyTip(RibbonPaintContext c, string keys) { record.Bounds = c.Bounds; base.DrawKeyTip(c, keys); } }
    private sealed class Neutral(BadgeRecord record) : RibbonRenderer
    { public override void DrawKeyTip(RibbonPaintContext c, string keys) { record.Bounds = c.Bounds; base.DrawKeyTip(c, keys); } }

    [Fact]
    public void Default_measurements_and_paint_match_saved_tip_caption_baseline() => FoundationTests.Sta(() =>
    {
        foreach (bool office in new[] { false, true })
        foreach (var density in Enum.GetValues<RibbonDensity>())
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        foreach (int points in new[] { 9, 14 })
        {
            int S(int n) => RibbonLayoutEngine.Scale(n, dpi); using var font = new Font("Segoe UI", points * dpi / 96f);
            using RibbonRenderer painter = office ? new RibbonOffice2024Renderer() : new RibbonRenderer();
            using var form = new RibbonForm { ClientSize = new(800, 400), Text = "Document - geometry review", Font = font };
            var ribbon = new RibbonControl { Density = density, Renderer = painter, Font = font }; form.Controls.Add(ribbon); form.Ribbon = ribbon;
            var tab = new RibbonTab { Text = "Home" }; var group = new RibbonGroup { Text = "Commands" }; var item = new RibbonButtonItem { Text = "Open", KeyTip = "AB" };
            group.Items.Add(item); tab.Groups.Add(group); ribbon.Tabs.Add(tab); ribbon.QuickAccessItems.Add(new RibbonQuickAccessReference { Source = item }); ribbon.QuickAccessPosition = RibbonQuickAccessPosition.TitleBar;
            string title = "Find matching text throughout the document", description = "Search without changing the native editor selection.\r\nThe full command description remains in the model.";
            var tip = RibbonScreenTipLayout.Measure(title, description, font, dpi, new(S(600), S(380)));
            var placement = RibbonScreenTipLayout.Place(new(60, 80, 100, 50), new(80, 100), tip.Size, new(0, 0, S(800), S(600)), dpi);
            var caption = RibbonCaptionGeometry.Measure(S(800), S(density == RibbonDensity.Touch ? 48 : 36), dpi, true, true, true, true);
            var keyTarget = new RibbonTarget(TargetKind.Item, item, new(20, 200, 180, 65), Rectangle.Empty, RibbonItemSize.Medium);
            var data = JsonSerializer.Serialize(new { tip.Size, tip.Title, tip.Description, Placement = placement, caption.Icon, caption.ContentLeft, caption.Buttons,
                CaptionTargets = caption.Targets.Select(t => t.Bounds), NativeCaption = form.Caption.Icon, Qat = form.QuickAccessSlot(ribbon), NativeDpi = ribbon.DeviceDpi });
            using var bitmap = new Bitmap(Math.Max(S(500), 800), Math.Max(S(400), 300)); using var graphics = Graphics.FromImage(bitmap); graphics.Clear(Color.White);
            painter.DrawPopupFrame(new(graphics, new(Point.Empty, tip.Size), ribbon.Theme, font, dpi));
            painter.DrawScreenTip(new(graphics, new(Point.Empty, tip.Size), ribbon.Theme, font, dpi), title, description, tip.Title, tip.Description);
            ribbon.DrawTips(graphics, [keyTarget]); var state = graphics.Save(); graphics.TranslateTransform(0, bitmap.Height - form.CaptionHeight);
            form.PaintCaption(graphics, ribbon); graphics.Restore(state);
            using var stream = new MemoryStream(); bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png); string name = $"tips-caption-{office}-{density}-{dpi}-{points}";
            if (Environment.GetEnvironmentVariable("RIBBONKIT_TIP_CAPTION_CAPTURE") is { Length: > 0 } output)
            { Directory.CreateDirectory(output); File.WriteAllBytes(Path.Combine(output, name + ".png"), stream.ToArray()); File.WriteAllText(Path.Combine(output, name + ".json"), data); }
            if (Environment.GetEnvironmentVariable("RIBBONKIT_TIP_CAPTION_BASELINE") is { Length: > 0 } baseline)
            { Assert.Equal(File.ReadAllBytes(Path.Combine(baseline, name + ".png")), stream.ToArray()); Assert.Equal(File.ReadAllText(Path.Combine(baseline, name + ".json")), data); }
        }
    });
}
