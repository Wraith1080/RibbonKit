using System.ComponentModel;
using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class OptionItemTests
{
    [Fact]
    public void Check_states_binding_and_events_follow_one_activation_path() => FoundationTests.Sta(() =>
    {
        using var form = new Form(); var ribbon = new RibbonControl();
        var tab = new RibbonTab(); var group = new RibbonGroup();
        var box = new RibbonCheckBoxItem { ThreeState = true };
        group.Items.Add(box); tab.Groups.Add(group); ribbon.Tabs.Add(tab); form.Controls.Add(ribbon);
        using var source = new BindingSource(); var state = new State(); source.DataSource = state;
        box.DataBindings.Add(new Binding(nameof(box.CheckState), source, nameof(state.Check), false, DataSourceUpdateMode.OnPropertyChanged));
        form.Show(); Application.DoEvents();
        var events = new List<string>();
        box.CheckedChanged += (_, _) => events.Add("checked:" + box.Checked);
        box.CheckStateChanged += (_, _) => events.Add("state:" + box.CheckState);
        box.Click += (_, _) => events.Add("click");
        box.PerformClick();
        Assert.Equal(new[] { "checked:True", "state:Checked", "click" }, events);
        Assert.Equal(CheckState.Checked, state.Check);
        events.Clear(); box.PerformClick();
        Assert.True(box.Checked); Assert.Equal(CheckState.Indeterminate, state.Check);
        Assert.Equal(new[] { "state:Indeterminate", "click" }, events);
        events.Clear(); box.PerformClick();
        Assert.Equal(new[] { "checked:False", "state:Unchecked", "click" }, events);
        events.Clear(); state.Check = CheckState.Indeterminate;
        Assert.Equal(CheckState.Indeterminate, box.CheckState); Assert.DoesNotContain("click", events);
        box.Checked = true; Assert.Equal(CheckState.Indeterminate, box.CheckState);
        box.ThreeState = false; box.PerformClick(); Assert.False(box.Checked);
        box.Enabled = false; events.Clear(); box.PerformClick(); Assert.Empty(events);
        box.Enabled = true; box.Visible = false; box.PerformClick(); Assert.Empty(events);
        Assert.Throws<ArgumentOutOfRangeException>(() => box.CheckState = (CheckState)99);
        box.Visible = true;
        box.CheckedChanged += (_, _) => box.Dispose(); box.PerformClick();
        Assert.DoesNotContain("click", events); Assert.Empty(group.Items);
    });

    [Fact]
    public void Radio_scope_binding_attachment_and_reentrant_removal_keep_exclusivity() => FoundationTests.Sta(() =>
    {
        using var form = new Form(); var ribbon = new RibbonControl(); var tab = new RibbonTab();
        var first = new RibbonGroup(); var second = new RibbonGroup();
        var a = new RibbonRadioButtonItem { Checked = true }; var b = new RibbonRadioButtonItem();
        var independent = new RibbonRadioButtonItem { Checked = true };
        first.Items.AddRange(a, b); second.Items.Add(independent); tab.Groups.AddRange(first, second); ribbon.Tabs.Add(tab); form.Controls.Add(ribbon);
        using var source = new BindingSource(); var state = new State(); source.DataSource = state;
        a.DataBindings.Add(new Binding(nameof(a.Checked), source, nameof(state.First), true, DataSourceUpdateMode.OnPropertyChanged));
        b.DataBindings.Add(new Binding(nameof(b.Checked), source, nameof(state.Second), true, DataSourceUpdateMode.OnPropertyChanged));
        form.Show(); Application.DoEvents();
        var order = new List<string>(); a.CheckedChanged += (_, _) => order.Add("a:" + a.Checked);
        b.CheckedChanged += (_, _) => order.Add("b:" + b.Checked); b.Click += (_, _) => order.Add("click");
        b.PerformClick();
        Assert.Equal(new[] { "a:False", "b:True", "click" }, order);
        Assert.False(state.First); Assert.True(state.Second); Assert.True(independent.Checked);
        order.Clear(); b.PerformClick(); Assert.Equal(new[] { "click" }, order); Assert.True(b.Checked);
        state.First = true; Assert.True(a.Checked); Assert.False(b.Checked); Assert.False(state.Second);
        a.GroupName = "view"; independent.GroupName = "view";
        Assert.False(a.Checked); Assert.True(independent.Checked);
        independent.Visible = false; independent.Enabled = false; a.Checked = true; Assert.False(independent.Checked);
        using var menu = new RibbonDropDownItem();
        using var attached = new RibbonRadioButtonItem { GroupName = "view", Checked = true };
        menu.Items.Add(attached); second.Items.Add(menu);
        Assert.False(a.Checked); Assert.True(attached.Checked);
        second.Items.Remove(menu); a.Checked = true;
        Assert.True(a.Checked); Assert.True(attached.Checked);
        second.Items.Add(menu); Assert.False(a.Checked);
        attached.GroupName = "separate"; a.Checked = true; Assert.True(attached.Checked);
        attached.GroupName = "view"; Assert.False(a.Checked);
        attached.CheckedChanged += (_, _) => { if (!attached.Checked) a.Dispose(); };
        a.Checked = true; Assert.DoesNotContain(a, first.Items); Assert.False(attached.Checked);
        b.Checked = false; Assert.False(b.Checked);
        form.Close();
    });

    [Fact]
    public void Compact_layout_and_indicator_rendering_keep_alignment_and_symmetric_edges() => FoundationTests.Sta(() =>
    {
        using var group = new RibbonGroup();
        var box = new RibbonCheckBoxItem { Text = "Show ruler" }; var radio = new RibbonRadioButtonItem { Text = "Print layout" };
        group.Items.AddRange(box, radio, new RibbonButtonItem { Text = "Command", SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium) });
        using var flat = new RibbonRenderer(); using var office = new RibbonOffice2024Renderer();
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        {
            var engine = new RibbonLayoutEngine();
            foreach (int state in new[] { 0, 1, 2 })
            {
                var measured = engine.Measure(group, state, SystemFonts.DefaultFont, dpi, RibbonThemes.Office2024Light);
                Assert.All(measured.Items, i => Assert.Equal(RibbonItemSize.Medium, i.Size));
                Assert.All(measured.Items, i => Assert.True(i.ReserveIcon));
                Assert.Single(measured.Items.Select(i => i.Bounds.Left).Distinct());
                Assert.Single(measured.Items.Select(i => i.Bounds.Width).Distinct());
            }
            foreach (var renderer in new[] { flat, office })
            foreach (int extra in new[] { 0, 1 })
            {
                int side = RibbonLayoutEngine.Scale(16, dpi) + extra;
                using var bitmap = new Bitmap(side, side); using var graphics = Graphics.FromImage(bitmap);
                var context = new RibbonPaintContext(graphics, new(0, 0, side, side), RibbonThemes.Office2024Light, SystemFonts.DefaultFont, dpi);
                foreach (bool circular in new[] { false, true })
                {
                    graphics.Clear(Color.Transparent);
                    if (circular) renderer.DrawRadioButtonIndicator(context, radio); else renderer.DrawCheckBoxIndicator(context, box);
                    for (int y = 0; y < side; y++) for (int x = 0; x < side; x++)
                    {
                        Assert.Equal(bitmap.GetPixel(x, y), bitmap.GetPixel(side - 1 - x, y));
                        Assert.Equal(bitmap.GetPixel(x, y), bitmap.GetPixel(x, side - 1 - y));
                    }
                }
                box.Enabled = false; graphics.Clear(Color.Transparent); renderer.DrawCheckBoxIndicator(context, box);
                using var disabled = (Bitmap)bitmap.Clone();
                graphics.Clear(Color.Transparent); renderer.DrawCheckBoxIndicator(context with { Hot = true, Pressed = true, Focused = true }, box);
                for (int y = 0; y < side; y++) for (int x = 0; x < side; x++) Assert.Equal(disabled.GetPixel(x, y), bitmap.GetPixel(x, y));
                box.Enabled = true;
                foreach (var mark in new[] { CheckState.Checked, CheckState.Indeterminate })
                {
                    box.CheckState = mark;
                    graphics.Clear(context.Theme.Surface); renderer.DrawCheckBoxIndicator(context, box);
                    double ink = 0, horizontalMoment = 0, verticalMoment = 0;
                    int inset = RibbonLayoutEngine.Scale(2, dpi);
                    for (int y = inset; y < side - inset; y++) for (int x = inset; x < side - inset; x++)
                    {
                        double coverage = Math.Max(0, bitmap.GetPixel(x, y).R - context.Theme.Accent.R);
                        ink += coverage; horizontalMoment += x * coverage; verticalMoment += y * coverage;
                    }
                    Assert.True(ink > 0);
                    double center = verticalMoment / ink;
                    double centerX = horizontalMoment / ink;
                    Assert.True(Math.Abs(centerX - (side - 1) / 2d) <= .35,
                        $"{mark}, DPI={dpi}, side={side}: mark X {centerX:F3}, box center {(side - 1) / 2d:F3}");
                    Assert.True(Math.Abs(center - (side - 1) / 2d) <= .35,
                        $"{mark}, DPI={dpi}, side={side}: mark center {center:F3}, box center {(side - 1) / 2d:F3}");
                }
                box.CheckState = CheckState.Unchecked;
            }
        }
    });

    private sealed class State : INotifyPropertyChanged
    {
        private CheckState _check; private bool _first = true, _second;
        public CheckState Check { get => _check; set { if (_check == value) return; _check = value; PropertyChanged?.Invoke(this, new(nameof(Check))); } }
        public bool First { get => _first; set { if (_first == value) return; _first = value; PropertyChanged?.Invoke(this, new(nameof(First))); } }
        public bool Second { get => _second; set { if (_second == value) return; _second = value; PropertyChanged?.Invoke(this, new(nameof(Second))); } }
        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
