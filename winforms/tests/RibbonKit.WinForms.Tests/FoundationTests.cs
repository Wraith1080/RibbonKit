using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class FoundationTests
{
    internal static void Sta(Action action)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
                action();
            }
            catch (Exception e) { failure = e; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (failure is not null)
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
    }
    [Fact]
    public void Ownership_rejects_duplicates_cycles_and_multiple_parents_without_mutation() => Sta(() =>
    {
        using var ribbon = new RibbonControl();
        var tab = new RibbonTab { Id = "tab" };
        var group = new RibbonGroup { Id = "group" };
        var item = new RibbonButtonItem { Id = "copy" };
        group.Items.Add(item);
        tab.Groups.Add(group);
        ribbon.Tabs.Add(tab);
        Assert.Throws<InvalidOperationException>(() => group.Items.Add(item));
        Assert.Throws<ArgumentException>(() => ribbon.Tabs.Add(new RibbonTab { Id = "copy" }));
        Assert.Throws<ArgumentException>(() => item.Id = "tab");
        Assert.Equal("copy", item.Id);
        var menu = new RibbonDropDownItem();
        Assert.Throws<InvalidOperationException>(() => menu.Items.Add(menu));
        Assert.Single(group.Items);
        Assert.Single(ribbon.Tabs);
        group.Items.Remove(item);
        var other = new RibbonGroup();
        tab.Groups.Add(other);
        other.Items.Add(item);
        Assert.Same(item, Assert.Single(other.Items));
        item.Dispose();
        Assert.Empty(other.Items);
    });
    [Fact]
    public void Toggle_activates_state_then_click_once_and_retains_two_way_binding() => Sta(() =>
    {
        using var form = new Form();
        using var source = new BindingSource();
        var state = new ToggleState();
        source.DataSource = state;
        var ribbon = new RibbonControl();
        var tab = new RibbonTab();
        var group = new RibbonGroup();
        var toggle = new RibbonToggleItem();
        group.Items.Add(toggle);
        tab.Groups.Add(group);
        ribbon.Tabs.Add(tab);
        form.Controls.Add(ribbon);
        toggle.DataBindings.Add(new Binding(nameof(toggle.Checked), source, nameof(state.Checked), true, DataSourceUpdateMode.OnPropertyChanged));
        form.Show();
        Application.DoEvents();
        var events = new List<string>();
        toggle.CheckedChanged += (_, _) => events.Add("state");
        toggle.Click += (_, _) => events.Add("click");
        toggle.PerformClick();
        Application.DoEvents();
        Assert.Equal(new[] { "state", "click" }, events);
        Assert.True(state.Checked);
        state.Checked = false;
        Application.DoEvents();
        Assert.False(toggle.Checked);
        events.Clear();
        toggle.Enabled = false;
        toggle.PerformClick();
        Assert.Empty(events);
        Assert.Single(toggle.DataBindings);
        var split = new RibbonSplitButtonItem { CheckOnClick = true };
        group.Items.Add(split);
        split.DataBindings.Add(new Binding(nameof(split.Checked), source, nameof(state.Checked), true, DataSourceUpdateMode.OnPropertyChanged));
        split.PerformClick(); Application.DoEvents();
        Assert.True(state.Checked);
        state.Checked = false; Application.DoEvents();
        Assert.False(split.Checked);
        form.Close();
    });
    [Fact]
    public void Dispose_preserves_borrowed_images_and_detached_items() => Sta(() =>
    {
        using var image = new Bitmap(8, 8);
        var ribbon = new RibbonControl();
        var tab = new RibbonTab();
        var group = new RibbonGroup();
        var owned = new RibbonButtonItem { Image = image };
        var detached = new RibbonButtonItem();
        group.Items.AddRange(owned, detached);
        tab.Groups.Add(group);
        ribbon.Tabs.Add(tab);
        group.Items.Remove(detached);
        ribbon.Dispose();
        ribbon.Dispose();
        Assert.True(owned.IsDisposed);
        Assert.False(detached.IsDisposed);
        Assert.Equal(8, image.Width);
        detached.Dispose();
    });
    [Fact]
    public void Size_policy_converter_reconstructs_values_and_collections_have_content_metadata()
    {
        var policy = RibbonSizePolicy.Adaptive(RibbonItemSize.Large, RibbonItemSize.Medium, RibbonItemSize.Small);
        var converter = TypeDescriptor.GetConverter(typeof(RibbonSizePolicy));
        var descriptor = Assert.IsType<InstanceDescriptor>(converter.ConvertTo(policy, typeof(InstanceDescriptor)));
        Assert.Equal(policy, descriptor.Invoke());
        Assert.Equal(policy, converter.ConvertFrom("Large, Medium, Small"));
        foreach (var pair in new[] { (typeof(RibbonControl), "Tabs"), (typeof(RibbonTab), "Groups"), (typeof(RibbonGroup), "Items"), (typeof(RibbonDropDownItem), "Items") })
        {
            var property = TypeDescriptor.GetProperties(pair.Item1)[pair.Item2]!;
            Assert.Equal(DesignerSerializationVisibility.Content, property.SerializationVisibility);
            Assert.True(property.IsReadOnly);
        }
    }
    [Theory]
    [InlineData(96, 80, 44)]
    [InlineData(120, 81, 45)]
    [InlineData(144, 120, 66)]
    [InlineData(192, 161, 89)]
    public void Actual_rounded_fill_and_stroke_have_symmetric_pixels(int dpi, int width, int height)
    {
        using var painter = new RoundedPainter();
        using var bitmap = new Bitmap(width, height);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.White);
        painter.Draw(graphics, new(0, 0, width, height), 6 * dpi / 96, new(Color.AliceBlue), Color.Navy, Math.Max(1, dpi / 96));
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                Assert.Equal(bitmap.GetPixel(x, y).ToArgb(), bitmap.GetPixel(width - 1 - x, y).ToArgb());
                Assert.Equal(bitmap.GetPixel(x, y).ToArgb(), bitmap.GetPixel(x, height - 1 - y).ToArgb());
            }
        graphics.Clear(Color.White);
        using var shadow = new ShadowPainter();
        int blur = Math.Max(1, 4 * dpi / 96);
        shadow.Draw(graphics, new(blur, blur, width - 2 * blur, height - 2 * blur), 6 * dpi / 96, blur, Color.FromArgb(32, Color.Black));
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                Assert.Equal(bitmap.GetPixel(x, y).ToArgb(), bitmap.GetPixel(width - 1 - x, y).ToArgb());
                Assert.Equal(bitmap.GetPixel(x, y).ToArgb(), bitmap.GetPixel(x, height - 1 - y).ToArgb());
            }
        using var office = new RibbonOffice2024Renderer();
        using var flat = new RibbonRenderer();
        foreach (var renderer in new RibbonRenderer[] { office, flat })
        {
            var theme = RibbonThemes.Office2024Light;
            int radius = renderer.GetPopupCornerRadius(theme, dpi);
            using var region = RoundedPainter.CreateOpaqueRegion(bitmap.Size, radius);
            graphics.Clear(Color.Transparent);
            renderer.DrawPopupFrame(new(graphics, new(0, 0, width, height), theme, SystemFonts.DefaultFont, dpi));
            bool partialEdge = false;
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    var color = bitmap.GetPixel(x, y);
                    Assert.Equal(color, bitmap.GetPixel(width - 1 - x, y));
                    Assert.Equal(color, bitmap.GetPixel(x, height - 1 - y));
                    Assert.True((color.A == 255) == region.IsVisible(x, y), $"DPI={dpi}, radius={radius}, pixel=({x},{y}), alpha={color.A}, region={region.IsVisible(x, y)}");
                    partialEdge |= color.A is > 0 and < 255;
                }
            Assert.Equal(radius > 0, partialEdge);
            Assert.Equal(radius == 0, region.IsVisible(.5f, .5f));
        }
    }
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Large_source_images_fit_small_icon_slots_even_when_disabled(bool enabled) => Sta(() =>
    {
        using var image = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(image))
            g.Clear(Color.Black);
        using var bitmap = new Bitmap(50, 50);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.White);
        using var renderer = new RibbonRenderer();
        using var item = new RibbonButtonItem { Image = image, Enabled = enabled };
        renderer.DrawItem(new(graphics, new(0, 0, 28, 26), RibbonThemes.Office2024Light, SystemFonts.DefaultFont, 96), item, RibbonItemSize.Small, false);
        Assert.NotEqual(Color.White.ToArgb(), bitmap.GetPixel(10, 10).ToArgb());
        Assert.Equal(Color.White.ToArgb(), bitmap.GetPixel(28, 20).ToArgb());
        Assert.Equal(Color.White.ToArgb(), bitmap.GetPixel(12, 28).ToArgb());
    });
    [Fact]
    public void Layout_reduces_by_priority_reexpands_and_retains_reachable_overflow() => Sta(() =>
    {
        using var ribbon = new RibbonControl { Width = 1100 };
        var tab = new RibbonTab();
        ribbon.Tabs.Add(tab);
        for (int i = 0; i < 3; i++)
        {
            var group = new RibbonGroup { Id = "g" + i, Text = "Group " + i, ReductionMode = RibbonGroupReductionMode.ResizeThenCollapse, ReductionPriority = i };
            for (int j = 0; j < 3; j++)
                group.Items.Add(new RibbonButtonItem { Text = "Command " + j, SizePolicy = RibbonSizePolicy.Adaptive(RibbonItemSize.Large, RibbonItemSize.Medium, RibbonItemSize.Small) });
            tab.Groups.Add(group);
        }
        Assert.All(ribbon.Snapshot.Groups, group => Assert.False(group.Collapsed));
        ribbon.Width = 190;
        Assert.Contains(ribbon.Snapshot.Groups, g => g.Collapsed);
        ribbon.Width = 90;
        var narrow = ribbon.Snapshot;
        Assert.True(narrow.BodyExtent > narrow.BodyViewport.Width);
        Assert.Contains(narrow.Targets, t => t.Kind == TargetKind.BodyNext);
        ribbon.Width = 1100;
        Assert.All(ribbon.Snapshot.Groups, g => Assert.False(g.Collapsed));
        Assert.Equal(9, ribbon.Snapshot.Targets.Count(t => t.Kind == TargetKind.Item));
        tab.Groups[0].Items[0].Text = "A longer runtime label";
        Assert.Contains(ribbon.Snapshot.Targets, t => t.Element?.Text == "A longer runtime label");
    });
    [Fact]
    public void Keytips_are_typeable_and_prefix_free_with_explicit_conflicts()
    {
        var keys = KeyTipResolver.Resolve(new[] { ("Home", " h "), ("Help", "H"), ("Find", "FN"), ("File", "F"), ("عربي", "") });
        Assert.Equal("H", keys[0]);
        Assert.Equal("FN", keys[2]);
        Assert.All(keys, key => Assert.All(key, c => Assert.True(c is >= 'A' and <= 'Z' or >= '0' and <= '9')));
        for (int i = 0; i < keys.Length; i++)
            for (int j = 0; j < keys.Length; j++)
                if (i != j)
                    Assert.False(keys[j].StartsWith(keys[i], StringComparison.Ordinal));
    }
    private sealed class ToggleState : INotifyPropertyChanged
    {
        private bool _checked;
        public bool Checked
        {
            get => _checked; set
            {
                _checked = value;
                PropertyChanged?.Invoke(this, new(nameof(Checked)));
            }
        }
        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
