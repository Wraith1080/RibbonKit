using System.ComponentModel;
using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class MinimizeTests
{
    [Fact]
    public void Minimized_state_notifies_and_serializes_without_changing_the_selected_model() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { Width = 1000 };
        var tab = new RibbonTab { Text = "Home" }; var group = new RibbonGroup();
        var toggle = new RibbonToggleItem { Text = "Bold", Checked = true }; group.Items.Add(toggle); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        int expandedHeight = ribbon.Height; int changes = 0;
        ribbon.IsMinimizedChanged += (_, _) => { changes++; Assert.Equal(ribbon.IsMinimized, ribbon.Snapshot.BodyBounds.IsEmpty); };
        var property = TypeDescriptor.GetProperties(ribbon)[nameof(ribbon.IsMinimized)]!;
        Assert.Equal(false, ((DefaultValueAttribute)property.Attributes[typeof(DefaultValueAttribute)]!).Value);
        Assert.True(((BindableAttribute)property.Attributes[typeof(BindableAttribute)]!).Bindable);
        Assert.Equal(DesignerSerializationVisibility.Visible, property.SerializationVisibility);
        ribbon.IsMinimized = true; ribbon.IsMinimized = true;
        Assert.Equal(1, changes); Assert.Equal(ribbon.Snapshot.TabViewport.Height, ribbon.Height);
        Assert.Empty(ribbon.Snapshot.Groups); Assert.All(ribbon.Snapshot.Targets, t => Assert.True(t.Kind is TargetKind.Tab or TargetKind.Minimize));
        Assert.Same(tab, ribbon.SelectedTab); Assert.Same(group, toggle.Parent); Assert.True(toggle.Checked);
        ribbon.IsMinimized = false; Assert.Equal(2, changes); Assert.Equal(expandedHeight, ribbon.Height);
        Assert.Contains(ribbon.Snapshot.Targets, t => ReferenceEquals(t.Element, toggle));
        ribbon.Enabled = false; ribbon.ToggleMinimizedByUser(); Assert.False(ribbon.IsMinimized);
        ribbon.IsMinimized = true; Assert.True(ribbon.IsMinimized); // Authored state is independent of input availability.
        using var batched = new RibbonControl(); int beforeBatch = batched.Height;
        batched.BeginUpdate(); batched.IsMinimized = true; Assert.Equal(beforeBatch, batched.Height);
        batched.EndUpdate(); Assert.Equal(batched.Snapshot.TabViewport.Height, batched.Height);
    });

    [Fact]
    public void Temporary_tab_body_reuses_group_and_split_geometry_and_keeps_narrow_commands_reachable() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl(); var tab = new RibbonTab { Text = "Home" }; ribbon.Tabs.Add(tab);
        var group = new RibbonGroup { Text = "Clipboard", ReductionMode = RibbonGroupReductionMode.ResizeThenCollapse };
        group.Items.AddRange(new RibbonSplitButtonItem { Text = "Paste", Layout = RibbonSplitButtonLayout.Vertical },
            new RibbonTextBoxItem { Text = "Name", InputWidth = 140 }, new RibbonComboBoxItem { Text = "Font", InputWidth = 140 });
        tab.Groups.Add(group);
        for (int i = 0; i < 4; i++)
        {
            var extra = new RibbonGroup { Text = "Group " + i, ReductionMode = RibbonGroupReductionMode.ResizeThenCollapse };
            extra.Items.AddRange(new RibbonButtonItem { Text = "Command " + i }, new RibbonToggleItem { Text = "Choice", Checked = true }); tab.Groups.Add(extra);
        }
        foreach (int points in new[] { 9, 18 })
        {
            using var font = new Font("Segoe UI", points); ribbon.Font = font;
            foreach (int width in new[] { 260, 720, 1100 })
            {
            ribbon.IsMinimized = false; ribbon.Width = width;
            var expanded = ribbon.Snapshot;
            using var popup = new RibbonPopupSurface(ribbon, tab, new(1800, 1200));
            int top = expanded.TabViewport.Height;
            foreach (var target in expanded.Targets.Where(t => t.Kind is TargetKind.Item or TargetKind.Group or TargetKind.BodyBack or TargetKind.BodyNext))
            {
                var same = Assert.Single(popup.Targets, t => t.Identity == target.Identity);
                Assert.Equal(target.Bounds with { Y = target.Bounds.Y - top }, same.Bounds);
                Assert.Equal(target.Size, same.Size); Assert.Equal(target.ReserveIcon, same.ReserveIcon);
            }
            ribbon.IsMinimized = true;
            Assert.Empty(ribbon.Snapshot.Groups); Assert.DoesNotContain(ribbon.Snapshot.Targets, t => t.Kind is TargetKind.Group or TargetKind.Item);
            Assert.Same(tab, ribbon.SelectedTab);
            }
            ribbon.Font = SystemFonts.DefaultFont;
        }
        ribbon.Width = 70;
        using var narrow = new RibbonPopupSurface(ribbon, tab, new(1800, 1200));
        Assert.True(narrow.Width >= RibbonLayoutEngine.Scale(200, ribbon.DeviceDpi));
        Assert.Contains(narrow.Targets, t => t.Element is RibbonGroup && !t.VisibleBounds.IsEmpty);
        var last = narrow.Targets.Last(t => t.Element is not null).Element!;
        narrow.FocusElement(last); Assert.Contains(narrow.Targets, t => ReferenceEquals(t.Element, last) && !t.VisibleBounds.IsEmpty);
    });

    [Fact]
    public void Utility_button_has_disjoint_tab_scroll_bounds_and_shared_vector_direction_at_each_scale() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl();
        for (int i = 0; i < 6; i++) ribbon.Tabs.Add(new RibbonTab { Text = "Tab " + i });
        foreach (int width in new[] { 20, 90, 260, 1100 })
        {
            ribbon.Width = width; var layout = ribbon.Snapshot;
            var button = Assert.Single(layout.Targets, t => t.Kind == TargetKind.Minimize);
            Assert.Equal(width, button.Bounds.Right);
            Assert.All(layout.Targets.Where(t => t.Kind is TargetKind.TabBack or TargetKind.TabNext), t => Assert.True(t.VisibleBounds.Right <= button.Bounds.Left));
            Assert.True(layout.TabViewport.Right <= button.Bounds.Left);
            if (width > RibbonLayoutEngine.Scale(28, ribbon.DeviceDpi)) Assert.True(layout.TabViewport.Width > 0);
        }
        using var flat = new RibbonRenderer(); using var office = new RibbonOffice2024Renderer();
        foreach (var painter in new[] { flat, office })
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        foreach (int odd in new[] { 0, 1 })
        {
            int width = RibbonLayoutEngine.Scale(28, dpi) + odd, height = RibbonLayoutEngine.Scale(32, dpi) + odd;
            using var up = new Bitmap(width, height); using var down = new Bitmap(width, height);
            using (var g = Graphics.FromImage(up)) painter.DrawMinimizeButton(new(g, new(0, 0, width, height), RibbonThemes.Office2024Light, SystemFonts.DefaultFont, dpi), false);
            using (var g = Graphics.FromImage(down)) painter.DrawMinimizeButton(new(g, new(0, 0, width, height), RibbonThemes.Office2024Light, SystemFonts.DefaultFont, dpi), true);
            Assert.Equal(RibbonThemes.Office2024Light.Background.ToArgb(), up.GetPixel(0, 0).ToArgb());
            Assert.Equal(RibbonThemes.Office2024Light.Background.ToArgb(), down.GetPixel(width - 1, height - 1).ToArgb());
            if (Environment.GetEnvironmentVariable("RIBBONKIT_NATIVE_CAPTURE_DIR") is { Length: > 0 } capture)
            {
                Directory.CreateDirectory(capture);
                up.Save(Path.Combine(capture, $"minimize-up-{painter.GetType().Name}-{dpi}-{odd}.png"));
                down.Save(Path.Combine(capture, $"minimize-down-{painter.GetType().Name}-{dpi}-{odd}.png"));
            }
            double upTop = 0, upBottom = 0, downTop = 0, downBottom = 0;
            double centreX = (width - 1) / 2d, centreY = (height - 1) / 2d;
            for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
            {
                int a = 255 - up.GetPixel(x, y).R, b = 255 - down.GetPixel(x, y).R;
                if (a > 0 || b > 0) Assert.True(x > 0 && y > 0 && x < width - 1 && y < height - 1);
                // GDI+ stroke antialiasing differs under reflection. Check the
                // actual chevron direction through the spread of its ink.
                double moment = (x - centreX) * (x - centreX);
                if (y < centreY) { upTop += a * moment; downTop += b * moment; }
                if (y > centreY) { upBottom += a * moment; downBottom += b * moment; }
            }
            Assert.True(upTop < upBottom); Assert.True(downTop > downBottom);
        }
    });
}
