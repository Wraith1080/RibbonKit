using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class OptionsIconPickerGeometryTests
{
    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Shared_square_size_and_inset_reserve_the_same_content_for_none_and_artwork(int dpi) => FoundationTests.Sta(() =>
    {
        foreach (var density in new[] { RibbonDensity.Compact, RibbonDensity.Touch })
        {
            var plain = new RibbonOptionsIconPickerMetrics(RibbonThemes.Office2019Light, dpi, density); int previous = 0;
            foreach (int size in new[] { 8, 16, 24, 48, 256 })
            {
                var a = new RibbonOptionsAppearanceSettings { IconPickerIconSize = size, IconPickerBorderWidth = 3 }; var theme = RibbonThemes.Office2019Light with { OptionsAppearance = a }; var m = new RibbonOptionsIconPickerMetrics(theme, dpi, density); int side = m.CellSide(17); Assert.InRange(side, previous, 256); previous = side;
                var tile = m.TileBounds(new(13, 17, side, side + 40), side); Assert.Equal(new Rectangle(13, 17, side, side), tile);
                foreach (int inset in new[] { 0, 4, 1024 })
                {
                    var current = new RibbonOptionsIconPickerMetrics(theme with { OptionsAppearance = a with { IconPickerInset = inset } }, dpi, density); var paint = current.SelectionBounds(tile); var text = current.TextBounds(tile); var icon = current.IconBounds(tile); Assert.Equal(paint.Width, paint.Height); Assert.True(tile.Contains(paint)); Assert.True(paint.Contains(text)); if (!icon.IsEmpty) Assert.True(text.Contains(icon));
                    using var painter = new RoundedPainter(); using var none = new Bitmap(side, side); using var art = new Bitmap(side, side); using var ng = Graphics.FromImage(none); using var ag = Graphics.FromImage(art); var painted = theme with { OptionsAppearance = a with { IconPickerInset = inset, IconPickerCornerRadius = 8, IconPickerBorderColor = Color.Navy, IconPickerSelectedBackground = new(Color.LightBlue), IconPickerIconSize = 0 }, Foreground = Color.Transparent }; var c = new RibbonPaintContext(ng, new(Point.Empty, none.Size), painted, Control.DefaultFont, dpi) { Density = density };
                    RibbonOptionsIconPickerRenderer.Draw(painter, c, null, true, 0); RibbonOptionsIconPickerRenderer.Draw(painter, c with { Graphics = ag }, new BlankIcon(), true, 0); Assert.Equal(Bytes(none), Bytes(art));
                }
            }
            Assert.Equal(plain.CellSide(17), new RibbonOptionsIconPickerMetrics(RibbonThemes.Office2019Light with { OptionsAppearance = new() { IconPickerIconSize = 0 } }, dpi, density).CellSide(17));
        }
    });
    [Theory] [InlineData(RibbonDensity.Compact, 9)] [InlineData(RibbonDensity.Touch, 9)] [InlineData(RibbonDensity.Compact, 14)] [InlineData(RibbonDensity.Touch, 14)]
    public void Native_grid_keeps_square_hits_names_and_ownership_across_size_inset_font_and_scroll(RibbonDensity density, int points) => FoundationTests.Sta(() =>
    {
        Application.EnableVisualStyles(); using var font = new Font("Segoe UI", points); using var ribbon = new RibbonControl { Density = density }; var tab = new RibbonTab { Id = "home", Text = "Home" }; var group = new RibbonGroup { Id = "commands", Text = "Commands" }; for (int i = 0; i < 24; i++) group.Items.Add(new RibbonButtonItem { Id = "icon" + i, Text = "Long command name with several words " + i, Icon = RibbonStockIcons.Create("save") }); tab.Groups.Add(group); ribbon.Tabs.Add(tab); ribbon.CommandCatalog.CaptureDefaults(); using var host = new Form { Font = font, StartPosition = FormStartPosition.Manual, Location = new(-16000, -16000), ShowInTaskbar = false }; host.Show(); using var dialog = new RibbonCustomizationGroupDialog(ribbon, host, new() { Label = "Group" }, "Group", false) { StartPosition = FormStartPosition.Manual, Location = new(-16000, -16000), ShowInTaskbar = false }; dialog.Show(host); Application.DoEvents(); var list = (ListView)dialog.Controls.Find("RibbonGroupIcons", true).Single(); nint handle = list.Handle; var parent = list.Parent; var items = list.Items.Cast<ListViewItem>().ToArray(); string[] names = items.Select(i => i.Text).ToArray(); var selected = items[2]; selected.Selected = true; int small = 0, large = 0;
        foreach (int size in new[] { 16, 48, 0 }) foreach (int inset in new[] { 0, 4 })
        {
            ribbon.Theme = ribbon.Theme with { OptionsAppearance = new() { IconPickerIconSize = size, IconPickerInset = inset, IconPickerBorderWidth = 3 } }; list.PerformLayout(); list.Refresh(); var m = new RibbonOptionsIconPickerMetrics(ribbon.Theme, list.DeviceDpi, density); int side = m.CellSide(list.Font.Height); if (size == 16) small = side; if (size == 48) large = side;
            int spacing = (int)SendMessage(handle, 0x1033, 0, 0); Assert.Equal(side, spacing & 0xffff); Assert.Equal(side, (spacing >> 16) & 0xffff); Assert.NotEqual(0, (int)SendMessage(handle, 0x1037, 0, 0) & 0x20000); Assert.Equal(items, list.Items.Cast<ListViewItem>()); Assert.Equal(names, items.Select(i => i.Text)); Assert.Equal(handle, list.Handle); Assert.Same(parent, list.Parent); Assert.Same(selected, list.SelectedItems.Cast<ListViewItem>().Single()); Assert.True(items.Select(i => i.Bounds.Top).Distinct().Count() > 1);
            foreach (var item in new[] { items[0], items[1], items[^1] })
            {
                item.EnsureVisible(); var bounds = item.GetBounds(ItemBoundsPortion.Icon); Assert.True(side == bounds.Width && side == bounds.Height, $"Expected square {side}, native icon {bounds}, entire {item.Bounds}, image {_ImageSize(list)}, dpi {list.DeviceDpi}"); var point = new Point(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2); Assert.Same(item, list.HitTest(point).Item); PostMessage(handle, 0x201, 1, (nint)((point.Y << 16) | (point.X & 0xffff))); PostMessage(handle, 0x202, 0, (nint)((point.Y << 16) | (point.X & 0xffff))); Application.DoEvents(); Assert.Same(item, list.SelectedItems.Cast<ListViewItem>().Single());
            }
            selected.Selected = true;
        }
        Assert.True(large > small); using var larger = new Font("Segoe UI", points + 2); list.Font = larger; list.PerformLayout(); Assert.Equal(list.Items[0].GetBounds(ItemBoundsPortion.Icon).Width, list.Items[0].GetBounds(ItemBoundsPortion.Icon).Height); Assert.Equal(handle, list.Handle); Assert.Same(selected, list.SelectedItems.Cast<ListViewItem>().Single()); Assert.Equal("None", list.AccessibilityObject.GetChild(0)!.Name); Assert.Equal(names[1], list.AccessibilityObject.GetChild(1)!.Name);
    });
    [DllImport("user32.dll")] private static extern bool PostMessage(nint window, int message, nint wParam, nint lParam);
    private static Size _ImageSize(ListView list) => list.LargeImageList!.ImageSize;
    private static byte[] Bytes(Bitmap image) { using var stream = new MemoryStream(); image.Save(stream, ImageFormat.Png); return stream.ToArray(); }
    private sealed class BlankIcon : IRibbonIcon { public void Draw(Graphics graphics, Rectangle bounds, RibbonIconContext context) { } }
    [DllImport("user32.dll")] private static extern nint SendMessage(nint window, int message, nint wParam, nint lParam);
}
