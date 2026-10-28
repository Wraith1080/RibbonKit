using System.Runtime.InteropServices;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class ComboAlignmentTests
{
    [StructLayout(LayoutKind.Sequential)] private struct Rect { internal int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] private struct ComboInfo { internal int Size; internal Rect Item, Button; internal int State; internal nint Combo, Edit, List; }
    [DllImport("user32.dll")] private static extern bool GetComboBoxInfo(nint handle, ref ComboInfo info);
    [DllImport("user32.dll")] private static extern bool GetWindowRect(nint handle, out Rect rect);

    [Fact]
    public void Native_combo_text_and_edit_windows_center_in_the_painted_field_across_density_font_and_popup_reparenting() => FoundationTests.Sta(() =>
    {
        using var painter = new Recorder(); using var form = new Form { Width = 1400, Height = 650 }; var host = new RibbonControl { Renderer = painter }; form.Controls.Add(host);
        var tab = new RibbonTab(); var group = new RibbonGroup(); tab.Groups.Add(group); host.Tabs.Add(tab);
        var list = new RibbonComboBoxItem { Text = "Choice" }; var edit = new RibbonComboBoxItem { Text = "Editable", IsEditable = true };
        foreach (var item in new[] { list, edit }) { item.Items.AddRange("Alpha", "Beta"); item.SelectedIndex = 1; group.Items.Add(item); }
        form.Show(); Application.DoEvents(); var natives = new[] { host.Editors.EditorFor(list)!, host.Editors.EditorFor(edit)! }; var handles = natives.Select(c => c.Handle).ToArray();
        foreach (int points in new[] { 9, 14 })
        {
            using var font = new Font("Segoe UI", points); host.Font = font;
            foreach (var density in new[] { RibbonDensity.Touch, RibbonDensity.Compact })
            {
                host.Density = density; Check(host);
                using var reference = new RibbonQuickAccessGroupReference { Source = group }; host.QuickAccessItems.Add(reference);
                host.Activate(host.Snapshot.Targets.Single(t => t.Element == reference)); Check(host.Popups.Top!); host.Popups.CloseAll(false);
                host.QuickAccessItems.Remove(reference); Check(host);
            }
        }
        void Check(Control surface)
        {
            using var bitmap = new Bitmap(surface.Width, surface.Height); surface.DrawToBitmap(bitmap, surface.ClientRectangle);
            for (int index = 0; index < natives.Length; index++)
            {
                var native = natives[index]; var item = index == 0 ? list : edit; Assert.Equal(handles[index], native.Handle); Assert.Equal("Beta", item.Value);
                var field = native.Parent!.RectangleToScreen(painter.Fields[item]);
                using var g = native.CreateGraphics(); var clip = native.RectangleToScreen(Rectangle.Round(native.Region!.GetBounds(g)));
                double delta = Math.Abs((clip.Top + clip.Bottom - field.Top - field.Bottom) / 2d);
                Assert.True(delta <= 1, $"{host.Density}, {item.Text}, font={host.Font.Size}: text center is off by {delta}px (actual height={native.Height}, preferred={native.PreferredHeight}, field={field}, text={clip}).");
                if (item.IsEditable)
                {
                    var info = new ComboInfo { Size = Marshal.SizeOf<ComboInfo>() }; Assert.True(GetComboBoxInfo(native.Handle, ref info)); Assert.True(GetWindowRect(info.Edit, out var child));
                    Assert.InRange(Math.Abs((child.Top + child.Bottom - field.Top - field.Bottom) / 2d), 0, 1);
                }
            }
        }
    });
    private sealed class Recorder : RibbonOffice2024Renderer
    {
        internal readonly Dictionary<RibbonComboBoxItem, Rectangle> Fields = [];
        public override void DrawComboBox(RibbonPaintContext c, RibbonComboBoxItem item, Rectangle field) { Fields[item] = field; base.DrawComboBox(c, item, field); }
    }
}
