using System.Runtime.InteropServices;
using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class PopupLayoutTests
{
    [DllImport("user32.dll")] private static extern nint SendMessage(nint window, uint message, nint wParam, nint lParam);

    [Theory]
    [InlineData(18)]
    public void Single_column_group_fits_commands_and_uses_the_right_edge_for_paint_and_input(int fontPoints) => FoundationTests.Sta(() =>
    {
        using var form = new Form { Width = 900, Height = 700 };
        using var font = new Font("Segoe UI", fontPoints);
        var ribbon = new RibbonControl { Font = font };
        var tab = new RibbonTab();
        var group = new RibbonGroup { Text = "A long strip caption that is not drawn inside the popup" };
        var bold = new RibbonToggleItem { Text = "Bold", Checked = true, SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium) };
        group.Items.AddRange(bold,
            new RibbonToggleItem { Text = "Italic", SizePolicy = bold.SizePolicy },
            new RibbonButtonItem { Text = "Unavailable", Enabled = false, SizePolicy = bold.SizePolicy });
        tab.Groups.Add(group);
        ribbon.Tabs.Add(tab);
        form.Controls.Add(ribbon);
        form.Show();
        Application.DoEvents();
        using var popup = new RibbonPopupSurface(ribbon, group, new(1200, 800)) { Location = new(20, ribbon.Bottom + 10) };
        form.Controls.Add(popup);
        Application.DoEvents();
        int padding = RibbonLayoutEngine.Scale(4, ribbon.DeviceDpi);
        Assert.Equal(popup.AutoScrollMinSize, popup.Size);
        Assert.False(popup.VerticalScroll.Visible);
        Assert.False(popup.HorizontalScroll.Visible);
        Assert.True(popup.Width < ribbon.Engine.Measure(group, 0, font, ribbon.DeviceDpi, ribbon.LayoutTheme).Width);
        Assert.All(popup.Targets, t => Assert.Equal(popup.ClientSize.Width - padding, t.Bounds.Right));
        Assert.Equal(padding, popup.Targets.Min(t => t.Bounds.Top));
        Assert.Equal(popup.ClientSize.Height - padding, popup.Targets.Max(t => t.Bounds.Bottom));

        using var bitmap = new Bitmap(popup.Width, popup.Height);
        popup.DrawToBitmap(bitmap, popup.ClientRectangle);
        var row = popup.Targets[0].Bounds;
        var sample = new Point(popup.ClientSize.Width - padding - 5, row.Top + row.Height / 2);
        Assert.Equal(ribbon.Theme.Checked.Start.ToArgb(), bitmap.GetPixel(sample.X, sample.Y).ToArgb());
        int clicks = 0;
        bold.Click += (_, _) => clicks++;
        nint position = (nint)((sample.Y << 16) | (sample.X & 0xffff));
        SendMessage(popup.Handle, 0x200, 0, position); // hover on the trailing side of the row
        popup.DrawToBitmap(bitmap, popup.ClientRectangle);
        Assert.Equal(ribbon.Theme.Hover.Start.ToArgb(), bitmap.GetPixel(sample.X, sample.Y).ToArgb());
        if (Environment.GetEnvironmentVariable("RIBBONKIT_NATIVE_CAPTURE_DIR") is { Length: > 0 } capture)
        {
            Directory.CreateDirectory(capture);
            bitmap.Save(Path.Combine(capture, $"popup-aligned-font{fontPoints}-{ribbon.DeviceDpi}dpi.png"));
        }
        SendMessage(popup.Handle, 0x201, 1, position);
        SendMessage(popup.Handle, 0x202, 0, position);
        Assert.Equal(1, clicks);
        Assert.False(bold.Checked);
    });

    [Fact]
    public void Multiple_group_columns_retain_distinct_nonoverlapping_command_bounds() => FoundationTests.Sta(() =>
    {
        using var form = new Form { Width = 1000 };
        var ribbon = new RibbonControl();
        var tab = new RibbonTab();
        var group = new RibbonGroup();
        for (int i = 0; i < 4; i++)
            group.Items.Add(new RibbonButtonItem { Text = "Command " + i });
        tab.Groups.Add(group);
        ribbon.Tabs.Add(tab);
        form.Controls.Add(ribbon);
        form.Show();
        Application.DoEvents();
        using var popup = new RibbonPopupSurface(ribbon, group, new(1200, 800));
        form.Controls.Add(popup);
        Application.DoEvents();
        var targets = popup.Targets;
        Assert.Equal(4, targets.Select(t => t.Bounds.Left).Distinct().Count());
        for (int i = 0; i < targets.Count; i++)
        for (int j = i + 1; j < targets.Count; j++)
            Assert.False(targets[i].Bounds.IntersectsWith(targets[j].Bounds));
        Assert.Equal(popup.Size, popup.AutoScrollMinSize);
        Assert.Equal(popup.ClientSize.Width - RibbonLayoutEngine.Scale(4, ribbon.DeviceDpi), targets.Max(t => t.Bounds.Right));
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Dropdown_rows_fill_the_client_width_and_only_overflowing_content_reserves_scrollbars(bool overflow) => FoundationTests.Sta(() =>
    {
        using var form = new Form { Width = 900, Height = 700 };
        var ribbon = new RibbonControl();
        var tab = new RibbonTab();
        var group = new RibbonGroup();
        var menu = new RibbonDropDownItem();
        for (int i = 0; i < (overflow ? 40 : 3); i++)
            menu.Items.Add(new RibbonButtonItem { Text = "Choice " + i });
        group.Items.Add(menu);
        tab.Groups.Add(group);
        ribbon.Tabs.Add(tab);
        form.Controls.Add(ribbon);
        form.Show();
        Application.DoEvents();
        int S(int value) => RibbonLayoutEngine.Scale(value, ribbon.DeviceDpi);
        var workArea = overflow ? new Size(S(185), S(160)) : new Size(1200, 800);
        using var popup = new RibbonPopupSurface(ribbon, menu, workArea) { Location = new(20, ribbon.Bottom + 10) };
        form.Controls.Add(popup);
        Application.DoEvents();
        if (!overflow)
        {
            Assert.Equal(popup.AutoScrollMinSize, popup.Size);
            Assert.False(popup.VerticalScroll.Visible);
            Assert.False(popup.HorizontalScroll.Visible);
            Assert.All(popup.Targets, t => Assert.Equal(popup.ClientSize.Width - S(4), t.Bounds.Right));
        }
        else
        {
            Assert.True(popup.VerticalScroll.Visible);
            Assert.True(popup.HorizontalScroll.Visible);
            Assert.True(popup.Width <= workArea.Width - S(24));
            Assert.True(popup.Height <= workArea.Height - S(32));
            popup.FocusElement(menu.Items[^1]);
            Assert.True(popup.Targets[^1].VisibleBounds.Height > 0);
        }
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        foreach (var work in new[] { new Rectangle(-1200, -200, 1200, 800), new Rectangle(40, 60, 360, 210) })
        foreach (bool bottom in new[] { false, true })
        {
            var anchor = new Rectangle(work.Right - 50, bottom ? work.Bottom - 35 : work.Top + 10, 180, 30);
            int rowHeight = RibbonLayoutEngine.Scale(28, dpi);
            var choiceBounds = RibbonChoicePopup.CalculateBounds(anchor, work, work.Width + 200, rowHeight, overflow ? 40 : 0, dpi);
            Assert.True(work.Contains(choiceBounds));
            Assert.True(choiceBounds.Height <= Math.Max(1, overflow ? 8 : 1) * rowHeight + 2);
            if (bottom) Assert.True(choiceBounds.Bottom <= anchor.Top);
            else Assert.True(choiceBounds.Top >= anchor.Bottom);
        }
    });
}
