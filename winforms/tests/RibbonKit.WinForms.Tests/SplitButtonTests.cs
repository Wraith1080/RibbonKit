using System.ComponentModel;
using System.Reflection;
using System.Runtime.InteropServices;
using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class SplitButtonTests
{
    [DllImport("user32.dll")] private static extern nint SendMessage(nint window, uint message, nint wParam, nint lParam);

    [Theory]
    [InlineData(120)]
    [InlineData(192)]
    public void Halves_tile_the_measured_item_and_vertical_applies_only_to_large_strip_items(int dpi) => FoundationTests.Sta(() =>
    {
        foreach (int fontSize in new[] { 9, 14 })
        foreach (var size in Enum.GetValues<RibbonItemSize>())
        foreach (var layout in Enum.GetValues<RibbonSplitButtonLayout>())
        foreach (bool menu in new[] { false, true })
        {
            using var font = new Font("Segoe UI", fontSize);
            using var group = new RibbonGroup();
            var split = new RibbonSplitButtonItem { Text = "Paste", Layout = layout, SizePolicy = RibbonSizePolicy.Fixed(size) };
            group.Items.Add(split);
            var measured = new RibbonLayoutEngine().Measure(group, 0, font, dpi, new RibbonTheme { BodyHeight = 160 }).Items.Single();
            var (primary, arrow) = RibbonSplitLayout.Calculate(measured.Bounds, split, size, menu, dpi);
            Assert.Equal(measured.Bounds, Rectangle.Union(primary, arrow));
            Assert.False(primary.IntersectsWith(arrow));
            Assert.True(primary.Width > 0 && primary.Height > 0 && arrow.Width > 0 && arrow.Height > 0);
            bool vertical = layout == RibbonSplitButtonLayout.Vertical && size == RibbonItemSize.Large && !menu;
            Assert.Equal(vertical, primary.Bottom == arrow.Top);
            if (!vertical)
            {
                Assert.Equal(primary.Right, arrow.Left);
                Assert.True(primary.Width > arrow.Width);
                Assert.Equal(measured.Bounds.Top, primary.Top);
            }
        }
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Mouse_and_keyboard_halves_do_not_cross_activate_and_preserve_focus_cue_policy(bool inPopup) => FoundationTests.Sta(() =>
    {
        using var form = new Form { Width = 900, Height = 500 };
        var ribbon = new RibbonControl();
        var tab = new RibbonTab { Text = "Home" };
        var group = new RibbonGroup();
        var split = new RibbonSplitButtonItem { Text = "Paste", Layout = RibbonSplitButtonLayout.Vertical, CheckOnClick = true };
        split.Items.Add(new RibbonButtonItem { Text = "Text only" });
        group.Items.Add(split); tab.Groups.Add(group); ribbon.Tabs.Add(tab); form.Controls.Add(ribbon);
        int clicks = 0, openings = 0;
        split.Click += (_, _) => clicks++;
        split.DropDownOpening += (_, e) => { openings++; e.Cancel = true; };
        form.Show(); Application.DoEvents();
        using var popup = inPopup ? new RibbonPopupSurface(ribbon, group, new(1200, 800)) : null;
        if (popup is not null) { form.Controls.Add(popup); popup.FocusFirst(); }
        Control surface = popup ?? (Control)ribbon;
        var targets = (popup?.Targets ?? ribbon.Snapshot.Targets).Where(t => ReferenceEquals(t.Element, split)).ToArray();
        var primary = targets.Single(t => !t.DropDownPart); var arrow = targets.Single(t => t.DropDownPart);
        Mouse(surface, 0x201, primary.Bounds); Mouse(surface, 0x202, arrow.Bounds);
        Assert.Equal(0, clicks); Assert.Equal(0, openings);
        Mouse(surface, 0x201, primary.Bounds); Mouse(surface, 0x202, primary.Bounds);
        Assert.Equal(1, clicks); Assert.False(ribbon.KeyboardFocusCues);
        Assert.True(split.Checked);
        Mouse(surface, 0x201, arrow.Bounds); Mouse(surface, 0x202, arrow.Bounds);
        Assert.Equal(1, clicks); Assert.Equal(1, openings);
        Key(surface, Keys.Enter);
        Assert.Equal(1, clicks); Assert.Equal(2, openings); Assert.True(ribbon.KeyboardFocusCues);
        Assert.True(split.Checked);
        Key(surface, Keys.Tab | Keys.Shift); Key(surface, Keys.Space);
        Assert.Equal(2, clicks); Assert.Equal(2, openings);
        Assert.False(split.Checked);
        split.Checked = true;
        split.Enabled = false;
        Mouse(surface, 0x201, primary.Bounds); Mouse(surface, 0x202, primary.Bounds);
        Mouse(surface, 0x201, arrow.Bounds); Mouse(surface, 0x202, arrow.Bounds);
        split.PerformClick(); split.ShowDropDown();
        Assert.Equal(2, clicks); Assert.Equal(2, openings); Assert.False(surface.Capture);
        Assert.True(split.Checked);
    });

    [Fact]
    public void Opening_revalidates_owner_after_callback_and_owned_children_dispose() => FoundationTests.Sta(() =>
    {
        using var form = new Form { Width = 900 };
        var ribbon = new RibbonControl(); var tab = new RibbonTab(); var group = new RibbonGroup();
        var split = new RibbonSplitButtonItem(); var child = new RibbonButtonItem();
        split.Items.Add(child); group.Items.Add(split); tab.Groups.Add(group); ribbon.Tabs.Add(tab); form.Controls.Add(ribbon);
        form.Show(); Application.DoEvents();
        split.DropDownOpening += (_, _) => group.Items.Remove(split);
        split.ShowDropDown();
        Assert.False(ribbon.Popups.HasOpen); Assert.Null(split.Ribbon);
        Assert.Same(split, child.Parent);
        split.Dispose(); Assert.True(child.IsDisposed);
    });

    [Fact]
    public void KeyTips_resolve_the_two_actions_and_collection_metadata_is_designer_serializable() => FoundationTests.Sta(() =>
    {
        using var form = new Form { Width = 900 };
        var ribbon = new RibbonControl(); var tab = new RibbonTab { KeyTip = "H" }; var group = new RibbonGroup();
        var split = new RibbonSplitButtonItem { Id = "paste", Text = "Paste", KeyTip = "V", DropDownKeyTip = "M" };
        split.Items.Add(new RibbonButtonItem { Id = "paste.text", Text = "Text only" });
        group.Items.Add(split); tab.Groups.Add(group); ribbon.Tabs.Add(tab); form.Controls.Add(ribbon);
        int clicks = 0, opens = 0;
        split.Click += (_, _) => clicks++;
        split.DropDownOpening += (_, e) => { opens++; e.Cancel = true; };
        form.Show(); Application.DoEvents();
        ribbon.ToggleKeyTips(); ribbon.ProcessRibbonKey(Keys.H); ribbon.ProcessRibbonKey(Keys.V);
        Assert.Equal(1, clicks); Assert.Equal(0, opens); Assert.False(ribbon.KeyTipsActive);
        ribbon.ToggleKeyTips(); ribbon.ProcessRibbonKey(Keys.H); ribbon.ProcessRibbonKey(Keys.M);
        Assert.Equal(1, clicks); Assert.Equal(1, opens);
        var property = TypeDescriptor.GetProperties(split)[nameof(split.Items)]!;
        Assert.Equal(DesignerSerializationVisibility.Content, property.SerializationVisibility);
        Assert.Equal(nameof(split.Click), TypeDescriptor.GetDefaultEvent(split)!.Name);
        Assert.False(split.CheckOnClick);
        Assert.Equal(BindableAttribute.Yes, TypeDescriptor.GetProperties(split)[nameof(split.Checked)]!.Attributes[typeof(BindableAttribute)]);
        split.PerformClick(); Assert.False(split.Checked);
        split.CheckOnClick = true;
        var order = new List<string>();
        split.CheckedChanged += (_, _) => order.Add("state");
        split.Click += (_, _) => order.Add("click");
        split.PerformClick();
        Assert.True(split.Checked);
        Assert.Equal(new[] { "state", "click" }, order);
        order.Clear(); split.Checked = false;
        Assert.Equal(new[] { "state" }, order);
        Assert.Throws<ArgumentOutOfRangeException>(() => split.Layout = (RibbonSplitButtonLayout)99);
    });

    private static void Mouse(Control control, uint message, Rectangle bounds)
    {
        int x = bounds.Left + bounds.Width / 2, y = bounds.Top + bounds.Height / 2;
        SendMessage(control.Handle, message, message == 0x201 ? 1 : 0, (nint)((y << 16) | (x & 0xffff)));
    }
    private static void Key(Control control, Keys key)
    {
        var method = control.GetType().GetMethod("ProcessCmdKey", BindingFlags.Instance | BindingFlags.NonPublic)!;
        Assert.Equal(true, method.Invoke(control, [Message.Create(control.Handle, 0x100, (nint)key, 0), key]));
    }
}
