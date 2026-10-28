using System.ComponentModel;
using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class FileMenuTests
{
    [Fact]
    public void File_menu_is_stable_content_serialized_and_owns_its_models_but_borrows_icons() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { BindingContext = new BindingContext() }; var menu = ribbon.FileMenu;
        Assert.Same(menu, ribbon.FileMenu); Assert.Same(ribbon, menu.Parent); Assert.False(menu.Visible);
        Assert.Equal("File", menu.Text); Assert.Equal("F", menu.KeyTip);
        var property = TypeDescriptor.GetProperties(ribbon)[nameof(ribbon.FileMenu)]!;
        Assert.True(property.IsReadOnly); Assert.Equal(DesignerSerializationVisibility.Content, property.SerializationVisibility);
        using var image = new Bitmap(16, 16); image.SetPixel(0, 0, Color.Red);
        var command = new RibbonButtonItem { Id = "file.open", Text = "Open", Image = image };
        menu.Items.Add(command); Assert.Same(menu, command.Parent); Assert.Same(ribbon, command.Ribbon);
        Assert.Same(ribbon.BindingContext, command.BindingContext);
        Assert.Throws<ArgumentException>(() => ribbon.QuickAccessItems.Add(new RibbonButtonItem { Id = command.Id }));
        Assert.Throws<ArgumentException>(() => menu.Id = command.Id);
        using var foreign = new RibbonControl(); var tab = new RibbonTab(); var group = new RibbonGroup(); tab.Groups.Add(group); foreign.Tabs.Add(tab);
        Assert.Throws<InvalidOperationException>(() => group.Items.Add(menu));
        ribbon.Dispose(); Assert.True(menu.IsDisposed); Assert.True(command.IsDisposed); Assert.Equal(Color.Red.ToArgb(), image.GetPixel(0, 0).ToArgb());
    });

    [Fact]
    public void File_slot_precedes_qat_and_tabs_without_changing_body_height_or_below_row_width() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl(); ribbon.Tabs.AddRange(new RibbonTab { Text = "Home" }, new RibbonTab { Text = "Insert" });
        ribbon.QuickAccessItems.AddRange(Enumerable.Range(0, 5).Select(i => new RibbonButtonItem { Text = "Command " + i }).ToArray());
        foreach (int inset in new[] { 0, 7, 13 })
        {
        ribbon.Theme = ribbon.Theme with { BodyMargin = new(inset, 1, 7, 7) };
        foreach (int points in new[] { 9, 18 })
        {
            using var font = new Font("Segoe UI", points); ribbon.Font = font;
            foreach (int width in new[] { 180, 260, 900 })
            foreach (var placement in new[] { RibbonQuickAccessPosition.TabRow, RibbonQuickAccessPosition.BelowRibbon })
            foreach (bool minimized in new[] { false, true })
            {
                ribbon.Width = width; ribbon.QuickAccessPosition = placement; ribbon.IsMinimized = minimized; ribbon.FileMenu.Visible = false;
                var before = ribbon.Snapshot; int height = ribbon.Height; var body = before.BodyBounds;
                ribbon.FileMenu.Visible = true; var layout = ribbon.Snapshot; var file = layout.Targets.Single(t => t.Kind == TargetKind.FileMenu);
                Assert.Equal(height, ribbon.Height); Assert.Equal(body, layout.BodyBounds);
                Assert.Equal(RibbonLayoutEngine.Scale(inset, ribbon.DeviceDpi), file.Bounds.Left);
                if (!minimized) Assert.Equal(layout.BodyBounds.Left, file.Bounds.Left);
                Assert.Equal(layout.TabViewport.Height, file.Bounds.Height); Assert.False(file.VisibleBounds.IsEmpty);
                Assert.False(file.VisibleBounds.IntersectsWith(layout.Targets.Single(t => t.Kind == TargetKind.Minimize).VisibleBounds));
                if (placement == RibbonQuickAccessPosition.TabRow)
                {
                    Assert.Equal(file.Bounds.Right, layout.QuickAccessBounds.Left); Assert.True(layout.QuickAccessBounds.Width > 0);
                    Assert.True(layout.QuickAccessBounds.Right <= layout.TabViewport.Left);
                }
                else
                {
                    Assert.Equal(RibbonLayoutEngine.Scale(inset, ribbon.DeviceDpi), layout.QuickAccessBounds.Left);
                    Assert.Equal(width - RibbonLayoutEngine.Scale(inset, ribbon.DeviceDpi) - RibbonLayoutEngine.Scale(ribbon.Theme.BodyMargin.Right, ribbon.DeviceDpi), layout.QuickAccessBounds.Width);
                    Assert.True(file.Bounds.Right <= layout.TabViewport.Left);
                }
                ribbon.FileMenu.Text = new string('W', 90); layout = ribbon.Snapshot;
                Assert.True(layout.TabViewport.Width > 0); if (placement == RibbonQuickAccessPosition.TabRow) Assert.True(layout.QuickAccessBounds.Width > 0);
                ribbon.FileMenu.Text = "File";
            }
            ribbon.Font = SystemFonts.DefaultFont;
        }
        }
        ribbon.FileMenu.Dispose(); Assert.DoesNotContain(ribbon.Snapshot.Targets, t => t.Kind == TargetKind.FileMenu);
    });

    [Fact]
    public void File_opening_cancellation_lazy_population_keytips_and_lifecycle_use_the_existing_menu_stack() => FoundationTests.Sta(() =>
    {
        using var form = new Form { Width = 900 }; var ribbon = new RibbonControl(); form.Controls.Add(ribbon);
        var tab = new RibbonTab { Text = "Home", KeyTip = "H" }; ribbon.Tabs.Add(tab); ribbon.FileMenu.Visible = true;
        var open = new RibbonButtonItem { Text = "Open", KeyTip = "O" }; var recent = new RibbonDropDownItem { Text = "Recent", KeyTip = "R" }; recent.Items.Add(new RibbonButtonItem { Text = "Document" });
        bool cancel = true; int opening = 0, closed = 0, calls = 0;
        ribbon.FileMenu.DropDownOpening += (_, e) => { opening++; e.Cancel = cancel; if (!cancel && ribbon.FileMenu.Items.Count == 0) ribbon.FileMenu.Items.AddRange(open, recent); };
        ribbon.FileMenu.DropDownClosed += (_, _) => closed++; open.Click += (_, _) => calls++;
        form.Show(); Application.DoEvents(); ribbon.FileMenu.ShowDropDown(); Assert.Equal(1, opening); Assert.False(ribbon.Popups.HasOpen); Assert.Equal(0, closed);
        cancel = false; ribbon.ToggleKeyTips(); Assert.True(ribbon.ProcessRibbonKey(Keys.F));
        Assert.True(ribbon.FileMenu.IsDropDownOpen); Assert.Same(ribbon.FileMenu, ribbon.Popups.Top!.MenuSource); Assert.Same(tab, ribbon.SelectedTab);
        Assert.True(ribbon.ProcessRibbonKey(Keys.O)); Assert.Equal(1, calls); Assert.False(ribbon.Popups.HasOpen); Assert.Equal(1, closed);
        ribbon.FileMenu.ShowDropDown(); var parent = ribbon.Popups.Top!;
        ribbon.Activate(parent.Targets.Single(t => ReferenceEquals(t.Element, recent)), parent); Assert.True(recent.IsDropDownOpen);
        ribbon.FileMenu.Enabled = false; Assert.False(ribbon.Popups.HasOpen); Assert.False(ribbon.FileMenu.IsDropDownOpen); Assert.Equal(2, closed);
        ribbon.FileMenu.Enabled = true; ribbon.IsMinimized = true; ribbon.FileMenu.ShowDropDown(); Assert.True(ribbon.FileMenu.IsDropDownOpen); Assert.True(ribbon.IsMinimized);
        ribbon.FileMenu.Visible = false; Assert.False(ribbon.Popups.HasOpen); Assert.Same(tab, ribbon.SelectedTab);
        ribbon.FileMenu.Visible = true; ribbon.FileMenu.ShowDropDown(); ribbon.FileMenu.Dispose(); Assert.False(ribbon.Popups.HasOpen); Assert.True(open.IsDisposed);
    });

    [Fact]
    public void File_header_has_hover_pressed_focus_and_disabled_visuals_in_both_renderers() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl(); ribbon.FileMenu.Visible = true;
        using var flat = new RibbonRenderer(); using var office = new RibbonOffice2024Renderer();
        foreach (var renderer in new RibbonRenderer[] { flat, office })
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        {
            int S(int value) => RibbonLayoutEngine.Scale(value, dpi);
            using var normal = Draw(false, false, false); using var hover = Draw(true, false, false); using var pressed = Draw(true, true, false); using var focus = Draw(false, false, true);
            Assert.Equal(ribbon.Theme.Hover.Start.ToArgb(), hover.GetPixel(S(6), S(6)).ToArgb());
            Assert.Equal(ribbon.Theme.Pressed.Start.ToArgb(), pressed.GetPixel(S(6), S(6)).ToArgb());
            Assert.NotEqual(Pixels(normal), Pixels(focus));
            ribbon.FileMenu.Enabled = false; using var disabled = Draw(false, false, false); using var flags = Draw(true, true, true);
            Assert.Equal(Pixels(disabled), Pixels(flags)); ribbon.FileMenu.Enabled = true;

            Bitmap Draw(bool hot, bool down, bool focused)
            {
                var bitmap = new Bitmap(S(80), S(32)); using var graphics = Graphics.FromImage(bitmap); graphics.Clear(ribbon.Theme.Background);
                renderer.DrawFileButton(new(graphics, new(Point.Empty, bitmap.Size), ribbon.Theme, ribbon.Font, dpi, hot, down, focused), ribbon.FileMenu); return bitmap;
            }
        }
        static int[] Pixels(Bitmap bitmap) => Enumerable.Range(0, bitmap.Height).SelectMany(y => Enumerable.Range(0, bitmap.Width).Select(x => bitmap.GetPixel(x, y).ToArgb())).ToArray();
    });
}
