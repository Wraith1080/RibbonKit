using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class OptionsAndCaptionRefinementTests
{
    private static T Child<T>(Control parent, string name) where T : Control => (T)parent.Controls.Find(name, true).Single();

    [Fact]
    public void Filtering_current_commands_keeps_remove_and_reorder_attached_to_presentation_identity() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl(); var tab = new RibbonTab(); var group = new RibbonGroup(); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        var sources = new[] { "Alpha", "Beta", "Gamma" }.Select(text => new RibbonButtonItem { Id = text, Text = text }).ToArray();
        group.Items.AddRange(sources); foreach (var source in sources) ribbon.QuickAccessItems.Add(new RibbonQuickAccessReference { Source = source });
        using var page = new RibbonQuickAccessCustomizationEditor(ribbon, "test.app");
        var search = Child<TextBox>(page, "ToolbarSearch"); var current = Child<ListBox>(page, "ToolbarCommands");
        search.Text = "Beta"; Assert.Single(current.Items.Cast<object>());
        Child<Button>(page, "MoveCommandUp").PerformClick(); Assert.Equal(new[] { "Beta", "Alpha", "Gamma" }, Order());
        Assert.False(Child<Button>(page, "MoveCommandUp").Enabled); Assert.True(Child<Button>(page, "MoveCommandDown").Enabled);
        Child<Button>(page, "MoveCommandDown").PerformClick(); Assert.Equal(new[] { "Alpha", "Beta", "Gamma" }, Order());
        Child<Button>(page, "RemoveCommand").PerformClick(); Assert.Equal(new[] { "Alpha", "Gamma" }, Order()); Assert.Empty(current.Items.Cast<object>());
        Assert.Equal(3, ribbon.QuickAccessItems.Count);
        search.Text = ""; Assert.Equal(2, current.Items.Count);
        page.Session.Cancel(); page.RefreshDraft();
        page.Session.Draft.QuickAccess.Add(new() { CommandId = "Alpha" }); page.RefreshDraft();
        string firstId = page.Session.Draft.QuickAccess[0].Id, duplicateId = page.Session.Draft.QuickAccess[^1].Id;
        search.Text = "Alpha"; Assert.Equal(2, current.Items.Count); current.SelectedIndex = 1;
        Child<Button>(page, "RemoveCommand").PerformClick();
        Assert.Contains(page.Session.Draft.QuickAccess, e => e.Id == firstId); Assert.DoesNotContain(page.Session.Draft.QuickAccess, e => e.Id == duplicateId);
        Assert.True(page.ApplyChanges()); Assert.Equal(new[] { "Alpha", "Beta", "Gamma" }, ribbon.QuickAccessItems.Cast<RibbonQuickAccessReference>().Select(r => r.Source!.Id));
        string[] Order() => page.Session.Draft.QuickAccess.Select(e => e.CommandId).ToArray();
    });

    [Fact]
    public void Both_searches_have_equal_vertical_gaps_and_keep_native_list_controls() => FoundationTests.Sta(() =>
    {
        foreach (bool office in new[] { true, false })
        foreach (var density in Enum.GetValues<RibbonDensity>())
        {
            using RibbonRenderer renderer = office ? new RibbonOffice2024Renderer() : new RibbonRenderer();
            using var ribbon = new RibbonControl { Renderer = renderer, Density = density };
            using var dialog = new RibbonQuickAccessCustomizationForm(ribbon, "test.app"); dialog.Show(); Application.DoEvents();
            var page = dialog.Editor; var reset = Child<Button>(page, "ResetCustomization"); var availableFrame = Child<Panel>(page, "AvailableCommandsFrame");
            int gap = reset.PointToScreen(Point.Empty).Y - availableFrame.PointToScreen(new(0, availableFrame.Height)).Y;
            Assert.True(gap > 0);
            foreach (var (field, frameName, listName, labelName) in new[] {
                ("CommandSearch", "CommandSearchFrame", "AvailableCommandsFrame", "AvailableCommandsLabel"),
                ("ToolbarSearch", "ToolbarSearchFrame", "ToolbarCommandsFrame", "ToolbarCommandsLabel") })
            {
                var search = Child<TextBox>(page, field); var frame = Child<Panel>(page, frameName); var listFrame = Child<Panel>(page, listName); var label = Child<Label>(page, labelName);
                Assert.Equal(gap, frame.Top - label.Bottom); Assert.Equal(gap, listFrame.Top - frame.Bottom);
                Assert.InRange(Math.Abs(search.Top * 2 + search.Height - frame.ClientSize.Height), 0, 1);
                Assert.Equal(office, listFrame.Controls.OfType<ListBox>().Single().Region is not null);
            }
            var ok = Child<Button>(dialog, "AcceptCustomization"); using var bitmap = new Bitmap(ok.Width, ok.Height); ok.DrawToBitmap(bitmap, ok.ClientRectangle);
            int corner = bitmap.GetPixel(0, 0).ToArgb(); Assert.Equal(corner, bitmap.GetPixel(ok.Width - 1, 0).ToArgb());
            Assert.Equal(corner, bitmap.GetPixel(0, ok.Height - 1).ToArgb()); Assert.Equal(corner, bitmap.GetPixel(ok.Width - 1, ok.Height - 1).ToArgb());
            Assert.Equal(office, corner != bitmap.GetPixel(ok.Width / 2, 0).ToArgb());
            dialog.Close();
        }
    });

    [Fact]
    public void Narrow_dialog_clips_a_page_at_its_minimum_width_and_preserves_a_larger_host_minimum() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl(); using var dialog = new RibbonQuickAccessCustomizationForm(ribbon, "test.app");
        dialog.Show(); Application.DoEvents();
        var page = dialog.SelectedPage!; int minimum = (int)Math.Ceiling(650 * dialog.DeviceDpi / 96d);
        dialog.Width = dialog.MinimumSize.Width; Application.DoEvents();
        Assert.True(page.Width >= minimum); Assert.True(page.Parent!.ClientSize.Width < page.Width); Assert.Equal(page.Width, dialog.Editor.Width);
        int authored = page.Width + 100; page.MinimumSize = new(authored, page.MinimumSize.Height);
        using var font = new Font(dialog.Font.FontFamily, dialog.Font.SizeInPoints + 1); dialog.Font = font; Application.DoEvents();
        Assert.True(page.MinimumSize.Width >= authored); Assert.True(page.Width >= authored); Assert.True(page.Parent.ClientSize.Width < page.Width);
        dialog.Close();
    });

    [Fact]
    public void Options_dialog_centers_its_final_size_on_the_owner_for_modal_and_modeless_opening() => FoundationTests.Sta(() =>
    {
        var work = Screen.PrimaryScreen!.WorkingArea;
        using var owner = new Form { StartPosition = FormStartPosition.Manual, Bounds = Rectangle.Inflate(work, -24, -24) };
        using var ribbon = new RibbonControl(); owner.Controls.Add(ribbon); owner.Show(); Application.DoEvents();
        foreach (bool modal in new[] { false, true })
        {
            using var dialog = new RibbonQuickAccessCustomizationForm(ribbon, "test.app"); bool shown = false;
            dialog.Shown += (_, _) =>
            {
                shown = true;
                var centered = new Rectangle(owner.Left + (owner.Width - dialog.Width) / 2, owner.Top + (owner.Height - dialog.Height) / 2, dialog.Width, dialog.Height);
                var expected = RibbonQuickAccessEditorMetrics.ClampWindow(centered, Screen.FromControl(owner).WorkingArea, dialog.DeviceDpi);
                Assert.InRange(Math.Abs(expected.Left - dialog.Left), 0, 1); Assert.InRange(Math.Abs(expected.Top - dialog.Top), 0, 1);
                if (modal) dialog.BeginInvoke(() => dialog.Close());
            };
            if (modal) dialog.ShowDialog(owner); else { dialog.Show(owner); Application.DoEvents(); dialog.Close(); }
            Assert.True(shown);
        }
    });

    [Fact]
    public void Shared_options_chrome_has_four_matching_corner_joins_at_odd_even_sizes_and_scaled_dpi() => FoundationTests.Sta(() =>
    {
        foreach (bool office in new[] { true, false })
        {
            using RibbonRenderer renderer = office ? new RibbonOffice2024Renderer() : new RibbonRenderer(); using var ribbon = new RibbonControl { Renderer = renderer };
            using var painter = new RoundedPainter();
            foreach (int dpi in new[] { 96, 120, 144, 192 })
            foreach (var size in new[] { new Size(133, 39), new Size(134, 40) })
            foreach (int stroke in new[] { 0, 1 })
            {
                using var bitmap = new Bitmap(size.Width, size.Height); using var graphics = Graphics.FromImage(bitmap); graphics.Clear(Color.Magenta);
                RibbonOptionsChrome.Surface(painter, graphics, new(Point.Empty, size), ribbon, dpi, new(ribbon.Theme.Accent), ribbon.Theme.PopupBorder, stroke);
                int radius = Math.Max(1, RibbonOptionsChrome.Radius(ribbon, dpi));
                for (int y = 0; y <= radius; y++) for (int x = 0; x <= radius; x++)
                {
                    int pixel = bitmap.GetPixel(x, y).ToArgb();
                    Assert.Equal(pixel, bitmap.GetPixel(size.Width - 1 - x, y).ToArgb());
                    Assert.Equal(pixel, bitmap.GetPixel(x, size.Height - 1 - y).ToArgb());
                    Assert.Equal(pixel, bitmap.GetPixel(size.Width - 1 - x, size.Height - 1 - y).ToArgb());
                }
                Assert.Equal(office, bitmap.GetPixel(0, 0).ToArgb() == Color.Magenta.ToArgb());
            }
        }
    });

    [Fact]
    public void Collapsed_groups_measure_captions_to_the_theme_limit_and_render_fitting_ellipsis_with_centered_arrow() => FoundationTests.Sta(() =>
    {
        using var painter = new CaptionRecorder(); using var ribbon = new RibbonControl { Renderer = painter };
        var tab = new RibbonTab(); var group = new RibbonGroup(); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        foreach (var density in Enum.GetValues<RibbonDensity>())
        foreach (int fontSize in new[] { 14, 20 })
        {
            ribbon.Density = density; using var font = new Font("Segoe UI", fontSize * dpi / 96f, GraphicsUnit.Pixel);
            int S(int value) => RibbonLayoutEngine.Scale(value, dpi);
            group.Text = "Clipboard"; ribbon.Engine.Clear();
            var measured = ribbon.Engine.Measure(group, 3, font, dpi, ribbon.Theme);
            int padding = 2 * S(RibbonLayoutEngine.CollapsedGroupSideInset) + 2 * S(RibbonLayoutEngine.CollapsedGroupCaptionInset);
            if (RibbonCaptionLayout.TextWidth(group.Text, font) + padding <= S(ribbon.Theme.CollapsedGroupMaxWidth))
                Assert.True(measured.Width - 1 - padding >= RibbonCaptionLayout.TextWidth(group.Text, font));
            group.Text = "An extraordinarily long collapsed group caption with many extra words"; ribbon.Engine.Clear();
            measured = ribbon.Engine.Measure(group, 3, font, dpi, ribbon.Theme);
            Assert.Equal(S(ribbon.Theme.CollapsedGroupMaxWidth) + 1, measured.Width);
            var bounds = new Rectangle(S(3), S(6), measured.Width - 1 - 2 * S(3), S(96));
            using var bitmap = new Bitmap(measured.Width, S(110)); using var graphics = Graphics.FromImage(bitmap);
            painter.Texts.Clear(); painter.Arrow = Rectangle.Empty;
            painter.DrawGroup(new(graphics, bounds, ribbon.Theme, font, dpi) { Density = density }, group, true);
            Assert.InRange(painter.Texts.Count, 1, 2); Assert.Contains(painter.Texts, row => row.Text.EndsWith('…'));
            foreach (var row in painter.Texts)
            {
                Assert.True(row.Flags.HasFlag(TextFormatFlags.EndEllipsis)); Assert.True(RibbonCaptionLayout.TextWidth(row.Text, font) <= row.Bounds.Width);
                Assert.Equal(row.Bounds.Left * 2 + row.Bounds.Width, painter.Arrow.Left * 2 + painter.Arrow.Width);
                Assert.True(row.Bounds.Bottom <= painter.Arrow.Top);
            }
        }
        Assert.Throws<ArgumentOutOfRangeException>(() => ribbon.Theme = ribbon.Theme with { CollapsedGroupMaxWidth = 79 });
        ribbon.Theme = ribbon.Theme with { CollapsedGroupMaxWidth = 160 };
        Assert.Equal(RibbonLayoutEngine.Scale(160, ribbon.DeviceDpi) + 1, ribbon.Engine.Measure(group, 3, ribbon.Font, ribbon.DeviceDpi, ribbon.Theme).Width);
    });

    private sealed class CaptionRecorder : RibbonOffice2024Renderer
    {
        internal readonly List<(string Text, Rectangle Bounds, TextFormatFlags Flags)> Texts = [];
        internal Rectangle Arrow;
        public override void DrawText(RibbonPaintContext context, string text, Color color, TextFormatFlags flags) { Texts.Add((text, context.Bounds, flags)); base.DrawText(context, text, color, flags); }
        public override void DrawDropDownArrow(RibbonPaintContext context, Color color) { Arrow = context.Bounds; base.DrawDropDownArrow(context, color); }
    }
}
