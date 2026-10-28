using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class PopupRefinementTests
{
    [Theory]
    [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Enlarged_menu_artwork_stays_inside_primary_rows_and_clear_of_text_at_each_density(int dpi) => FoundationTests.Sta(() =>
    {
        using var renderer = new RibbonOffice2024Renderer(); using var font = new Font("Segoe UI", 9);
        using var bitmap = new Bitmap(1000, 500); using var graphics = Graphics.FromImage(bitmap);
        foreach (var density in Enum.GetValues<RibbonDensity>())
        foreach (bool described in new[] { false, true })
        {
            var icon = new IconProbe(); using var command = new RibbonSplitButtonItem { Text = "Paste", Icon = icon, MenuDescription = described ? "Paste the clipboard content with its original formatting." : "" };
            int S(int value) => RibbonLayoutEngine.Scale(value, dpi);
            int width = S(320), height = RibbonMenuLayout.RowHeight(command, width, font, dpi, density), arrow = S(RibbonDensityMetrics.For(density).SplitArrow);
            var row = new Rectangle(0, 0, width - arrow, height); var context = new RibbonPaintContext(graphics, row, RibbonThemes.Office2024Light, font, dpi) { Density = density };
            renderer.DrawSplitItem(context, context with { Bounds = new(width - arrow, 0, arrow, height) }, command, RibbonItemSize.Medium, true);
            Assert.True(row.Contains(icon.Bounds)); Assert.True(icon.Bounds.Width >= S(density == RibbonDensity.Touch ? 32 : 24));
            var content = RibbonMenuLayout.Content(RibbonContentLayout.Calculate(row, RibbonItemSize.Medium, true, true, dpi, font, density), command, row, font, dpi, density);
            Assert.True(icon.Bounds.Right < content.Text.Left); Assert.Equal(row.Top + (row.Height - icon.Bounds.Height) / 2, icon.Bounds.Top);
            if (described) Assert.True(row.Contains(RibbonMenuLayout.Description(content.Text, row, font, dpi, density)));
        }
    });
    private sealed class IconProbe : IRibbonIcon
    {
        internal Rectangle Bounds;
        public void Draw(Graphics graphics, Rectangle bounds, RibbonIconContext context) => Bounds = bounds;
    }
    [Fact]
    public void Command_popups_use_the_tallest_visible_row_and_touch_expands_described_rows_at_each_dpi() => FoundationTests.Sta(() =>
    {
        using var probe = new Control(); using var host = new RibbonControl { Width = 1400 }; var tab = new RibbonTab(); var group = new RibbonGroup(); tab.Groups.Add(group); host.Tabs.Add(tab);
        var menu = new RibbonDropDownItem { Text = "Paste", DropDownHeader = "Paste options" }; group.Items.Add(menu);
        var shortRow = new RibbonButtonItem { Text = "Short", MenuDescription = "One short line." };
        var tallRow = new RibbonButtonItem { Text = "Detailed", MenuDescription = "Insert clipboard content without retaining its original document formatting or paragraph styles." };
        var separator = new RibbonMenuSeparatorItem(); var editor = new RibbonTextBoxItem { Text = "Name" };
        menu.Items.AddRange(shortRow, separator, tallRow, editor, new RibbonButtonItem { Text = "Disabled", Enabled = false }, new RibbonButtonItem { Text = "Hidden", Visible = false });
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        foreach (int points in new[] { 9, 14 })
        {
            using var font = new Font("Segoe UI", points * dpi / (float)probe.DeviceDpi);
            var visible = menu.Items.Where(i => i.Visible).ToArray(); int width = RibbonLayoutEngine.Scale(280, dpi);
            var compact = RibbonMenuLayout.RowHeights(visible, width, font, dpi, RibbonDensity.Compact);
            var touch = RibbonMenuLayout.RowHeights(visible, RibbonLayoutEngine.Scale(320, dpi), font, dpi, RibbonDensity.Touch);
            Assert.Single(visible.Select((item, i) => (item, i)).Where(p => p.item is not RibbonMenuSeparatorItem).Select(p => compact[p.i]).Distinct());
            Assert.Single(visible.Select((item, i) => (item, i)).Where(p => p.item is not RibbonMenuSeparatorItem).Select(p => touch[p.i]).Distinct());
            Assert.True(touch[0] > compact[0]); Assert.Equal(RibbonLayoutEngine.Scale(9, dpi), compact[1]); Assert.Equal(compact[1], touch[1]);
            var field = new Rectangle(0, 0, RibbonLayoutEngine.Scale(320, dpi), touch[0]);
            var content = RibbonMenuLayout.Content(RibbonContentLayout.Calculate(field, RibbonItemSize.Medium, true, true, dpi, font), tallRow, field, font, dpi, RibbonDensity.Touch);
            var description = RibbonMenuLayout.Description(content.Text, field, font, dpi, RibbonDensity.Touch);
            Assert.True(field.Contains(content.Icon)); Assert.True(content.Icon.Right < content.Text.Left); Assert.True(description.Top > content.Text.Bottom);
            Assert.Equal(2 * font.Height, description.Height); Assert.True(field.Contains(description));
        }
        using var compactPopup = new RibbonPopupSurface(host, menu, new(1200, 900));
        host.Density = RibbonDensity.Touch; using var touchPopup = new RibbonPopupSurface(host, menu, new(1200, 900));
        Assert.True(touchPopup.Width > compactPopup.Width); Assert.True(touchPopup.Height > compactPopup.Height);
        Assert.Equal(5, touchPopup.Targets.Count); Assert.Single(touchPopup.Targets.Where(t => t.Element is not RibbonMenuSeparatorItem).Select(t => t.Bounds.Height).Distinct());
        var references = new[] { new RibbonQuickAccessReference { Source = shortRow }, new RibbonQuickAccessReference { Source = tallRow } }; host.QuickAccessItems.AddRange(references); host.QuickAccessMaxWidth = 28;
        using var overflow = new RibbonPopupSurface(host, host.QuickAccessOverflow, new(1200, 900)); Assert.Single(overflow.Targets.Select(t => t.Bounds.Height).Distinct());
    });

    [Fact]
    public void Alt_f4_cleans_up_owned_popups_and_requests_cancellable_host_close_once_per_chord() => FoundationTests.Sta(() =>
    {
        using var form = new Form { Width = 1200, Height = 600 }; var host = new RibbonControl(); form.Controls.Add(host); var tab = new RibbonTab(); var group = new RibbonGroup(); tab.Groups.Add(group); host.Tabs.Add(tab);
        var menu = new RibbonDropDownItem { Text = "Menu" }; var child = new RibbonDropDownItem { Text = "Child" }; child.Items.Add(new RibbonButtonItem { Text = "Choice" }); var field = new RibbonTextBoxItem { Text = "Name", Value = "Retained" }; menu.Items.AddRange(child, field); group.Items.Add(menu);
        int hostCloseRequests = 0; form.FormClosing += (_, e) => { hostCloseRequests++; e.Cancel = true; };
        form.Show(); Application.DoEvents(); menu.ShowDropDown(); var editor = host.Editors.EditorFor(field)!; nint handle = editor.Handle; child.ShowDropDown();
        int childClosed = 0, menuClosed = 0; child.DropDownClosed += (_, _) => childClosed++; menu.DropDownClosed += (_, _) => menuClosed++;
        using var filter = new RibbonInputFilter(host);
        var down = Message.Create(host.Popups.Top!.Handle, 0x104, (nint)Keys.F4, (nint)(1L << 29)); Assert.True(filter.PreFilterMessage(ref down));
        Assert.False(child.IsDropDownOpen); Assert.False(menu.IsDropDownOpen); Assert.Equal(1, childClosed); Assert.Equal(1, menuClosed); Assert.Equal(1, hostCloseRequests); Assert.True(form.Visible);
        var repeat = Message.Create(host.Handle, 0x104, (nint)Keys.F4, (nint)((1L << 29) | (1L << 30))); Assert.True(filter.PreFilterMessage(ref repeat)); Assert.Equal(1, hostCloseRequests);
        var up = Message.Create(host.Handle, 0x105, (nint)Keys.F4, 0); filter.PreFilterMessage(ref up);
        down = Message.Create(host.Handle, 0x104, (nint)Keys.F4, (nint)(1L << 29)); Assert.False(filter.PreFilterMessage(ref down));
        menu.ShowDropDown(); Assert.Same(editor, host.Editors.EditorFor(field)); Assert.Equal(handle, editor.Handle);
        down = Message.Create(editor.Handle, 0x104, (nint)Keys.F4, (nint)(1L << 29)); Assert.True(filter.PreFilterMessage(ref down));
        Assert.False(host.Popups.HasOpen); Assert.Equal(2, menuClosed); Assert.Equal(2, hostCloseRequests); Assert.False(editor.IsDisposed); Assert.Equal(handle, editor.Handle); Assert.Equal("Retained", field.Value);
        filter.PreFilterMessage(ref up); down = Message.Create(host.Handle, 0x104, (nint)Keys.F4, (nint)(1L << 29)); Assert.False(filter.PreFilterMessage(ref down));
        menu.ShowDropDown(); Assert.Same(editor, host.Editors.EditorFor(field)); Assert.Equal(handle, editor.Handle);
    });
}
