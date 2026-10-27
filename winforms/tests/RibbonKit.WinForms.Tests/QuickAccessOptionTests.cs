using System.ComponentModel;
using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class QuickAccessOptionTests
{
    [Fact]
    public void Checkbox_references_share_three_state_binding_and_event_order_without_closing_overflow() => FoundationTests.Sta(() =>
    {
        using var form = new Form { Width = 900 }; var ribbon = new RibbonControl { QuickAccessMaxWidth = 28 }; form.Controls.Add(ribbon);
        var home = new RibbonTab(); var other = new RibbonTab(); var group = new RibbonGroup(); var box = new RibbonCheckBoxItem { Text = "Grid", ThreeState = true };
        group.Items.Add(box); home.Groups.Add(group); ribbon.Tabs.AddRange(home, other);
        var first = new RibbonQuickAccessReference { Source = box }; var duplicate = new RibbonQuickAccessReference { Source = box };
        var command = new RibbonButtonItem { Text = "Copy" }; ribbon.QuickAccessItems.AddRange(first, duplicate, command);
        using var binding = new BindingSource(); var state = new State(); binding.DataSource = state;
        box.DataBindings.Add(new Binding(nameof(box.CheckState), binding, nameof(state.Check), false, DataSourceUpdateMode.OnPropertyChanged));
        ribbon.SelectedTab = other; form.Show(); Application.DoEvents();
        int calls = 0; var order = new List<string>(); box.CheckedChanged += (_, _) => order.Add("checked"); box.CheckStateChanged += (_, _) => order.Add("state"); box.Click += (_, _) => { calls++; order.Add("click"); };
        ribbon.Activate(ribbon.Snapshot.Targets.Single(t => t.Kind == TargetKind.QuickAccessOverflow)); var popup = ribbon.Popups.Top!;
        var entries = new[] { first, duplicate, first }; var expected = new[] { CheckState.Checked, CheckState.Indeterminate, CheckState.Unchecked };
        for (int i = 0; i < entries.Length; i++)
        {
            order.Clear(); ribbon.Activate(popup.Targets.Single(t => ReferenceEquals(t.Element, entries[i])), popup);
            Assert.Equal(expected[i], box.CheckState); Assert.Equal(expected[i], state.Check); Assert.Equal(i + 1, calls);
            Assert.Equal(i == 1 ? new[] { "state", "click" } : new[] { "checked", "state", "click" }, order);
            Assert.Same(popup, ribbon.Popups.Top); Assert.Same(entries[i], popup.FocusedElement); Assert.Same(group, box.Parent); Assert.Same(other, ribbon.SelectedTab);
        }
        state.Check = CheckState.Indeterminate; Assert.Equal(CheckState.Indeterminate, box.CheckState); Assert.Equal(3, calls);
        first.Enabled = false; ribbon.Activate(popup.Targets.Single(t => ReferenceEquals(t.Element, first)), popup); Assert.Equal(3, calls); Assert.True(box.Enabled);
        box.Enabled = false; Assert.False(RibbonQuickAccess.Available(duplicate)); box.Enabled = true;
        ribbon.Activate(popup.Targets.Single(t => ReferenceEquals(t.Element, command)), popup); Assert.True(popup.IsDisposed);
        Assert.Throws<ArgumentException>(() => ribbon.QuickAccessItems.Add(new RibbonCheckBoxItem()));
        using var foreign = new RibbonControl(); Assert.Throws<ArgumentException>(() => foreign.QuickAccessItems.Add(new RibbonQuickAccessReference { Source = box }));
        duplicate.Dispose(); Assert.False(box.IsDisposed);
    });

    [Fact]
    public void Radio_references_keep_original_unnamed_and_named_scopes_and_never_toggle_selected_sources_off() => FoundationTests.Sta(() =>
    {
        using var form = new Form { Width = 900 }; var ribbon = new RibbonControl { QuickAccessMaxWidth = 28 }; form.Controls.Add(ribbon);
        var tab = new RibbonTab(); var firstGroup = new RibbonGroup(); var secondGroup = new RibbonGroup();
        var a = new RibbonRadioButtonItem { Text = "Print", Checked = true }; var b = new RibbonRadioButtonItem { Text = "Web" }; var c = new RibbonRadioButtonItem { Text = "Independent", Checked = true };
        firstGroup.Items.AddRange(a, b); secondGroup.Items.Add(c); tab.Groups.AddRange(firstGroup, secondGroup); ribbon.Tabs.Add(tab);
        var ar = new RibbonQuickAccessReference { Source = a }; var br = new RibbonQuickAccessReference { Source = b }; var cr = new RibbonQuickAccessReference { Source = c };
        ribbon.QuickAccessItems.AddRange(ar, br, cr); form.Show(); Application.DoEvents();
        var order = new List<string>(); a.CheckedChanged += (_, _) => order.Add("a:" + a.Checked); b.CheckedChanged += (_, _) => order.Add("b:" + b.Checked); b.Click += (_, _) => order.Add("click");
        ribbon.Activate(ribbon.Snapshot.Targets.Single(t => t.Kind == TargetKind.QuickAccessOverflow)); var popup = ribbon.Popups.Top!;
        Select(br); Assert.Equal(new[] { "a:False", "b:True", "click" }, order); Assert.True(c.Checked); Assert.Same(firstGroup, b.Parent); Assert.Same(secondGroup, c.Parent);
        order.Clear(); Select(br); Assert.Equal(new[] { "click" }, order); Assert.True(b.Checked); Assert.Same(popup, ribbon.Popups.Top);
        b.GroupName = "view"; c.GroupName = "view"; Assert.False(b.Checked); Assert.True(c.Checked);
        Select(br); Assert.True(b.Checked); Assert.False(c.Checked); Assert.Same(popup, ribbon.Popups.Top);
        b.Enabled = false; Select(br); Assert.True(b.Checked); b.Enabled = true;
        using var disposed = new RibbonRadioButtonItem(); disposed.Dispose(); Assert.Throws<ObjectDisposedException>(() => br.Source = disposed);
        Assert.Throws<ArgumentException>(() => ribbon.QuickAccessItems.Add(new RibbonRadioButtonItem()));

        void Select(RibbonQuickAccessReference reference) => ribbon.Activate(popup.Targets.Single(t => ReferenceEquals(t.Element, reference)), popup);
    });

    [Fact]
    public void Option_references_use_icon_toggle_chrome_with_mixed_state_and_disabled_fallbacks() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl(); var tab = new RibbonTab(); var group = new RibbonGroup();
        var icon = new Icon();
        var box = new RibbonCheckBoxItem { Text = "Grid", CheckState = CheckState.Checked, Icon = icon };
        var radio = new RibbonRadioButtonItem { Text = "Print", Checked = true, Icon = icon };
        var toggle = new RibbonToggleItem { Text = "Grid", Checked = true, Icon = icon };
        group.Items.AddRange(box, radio, toggle); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        var boxRef = new RibbonQuickAccessReference { Source = box, Text = "Grid alias" }; var radioRef = new RibbonQuickAccessReference { Source = radio };
        var toggleRef = new RibbonQuickAccessReference { Source = toggle };
        ribbon.QuickAccessItems.AddRange(boxRef, radioRef, toggleRef);
        using var office = new Office(); using var flat = new Flat();
        foreach (var (renderer, record) in new (RibbonRenderer, Recording)[] { (office, office.Record), (flat, flat.Record) })
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        {
            int S(int value) => RibbonLayoutEngine.Scale(value, dpi);
            foreach (var (hot, pressed) in new[] { (false, false), (true, false), (true, true) })
            {
                using var expected = Paint(toggleRef, hot, pressed); using var checkbox = Paint(boxRef, hot, pressed); using var selectedRadio = Paint(radioRef, hot, pressed);
                SamePixels(expected, checkbox); SamePixels(expected, selectedRadio);
                Assert.True(icon.Context.Enabled); Assert.Equal(new Size(S(16), S(16)), icon.Bounds.Size);
            }
            box.CheckState = CheckState.Indeterminate;
            using (var mixed = Paint(boxRef))
                Assert.Equal(ribbon.Theme.Foreground.ToArgb(), mixed.GetPixel(S(14), S(26)).ToArgb());
            boxRef.Enabled = false;
            using (var disabled = Paint(boxRef, true, true))
            {
                Assert.False(icon.Context.Enabled); Assert.Equal(SystemColors.GrayText.ToArgb(), disabled.GetPixel(S(14), S(26)).ToArgb());
            }
            box.CheckState = CheckState.Checked; radioRef.Enabled = toggleRef.Enabled = false;
            using (var expected = Paint(toggleRef, true, true))
            using (var checkbox = Paint(boxRef, true, true))
            using (var selectedRadio = Paint(radioRef, true, true)) { SamePixels(expected, checkbox); SamePixels(expected, selectedRadio); }
            using (var menu = Paint(boxRef, menu: true)) { Assert.Equal("Grid alias", record.Caption); Assert.False(icon.Context.Enabled); }
            boxRef.Enabled = radioRef.Enabled = toggleRef.Enabled = true;
            box.Icon = null;
            using (var fallback = Paint(boxRef)) Assert.Equal("G", record.Caption);
            using var image = new Bitmap(16, 16); using (var g = Graphics.FromImage(image)) g.Clear(Color.Magenta);
            box.Image = image;
            using (var bitmap = Paint(boxRef)) Assert.Equal(Color.Magenta.ToArgb(), bitmap.GetPixel(S(14), S(16)).ToArgb());
            box.Image = null; box.Icon = icon;
            Assert.Equal(0, record.Indicators);

            Bitmap Paint(RibbonElement entry, bool hot = false, bool pressed = false, bool menu = false)
            {
                var bitmap = new Bitmap(S(menu ? 240 : 28), S(menu ? 56 : 32)); using var graphics = Graphics.FromImage(bitmap); graphics.Clear(Color.White);
                renderer.DrawQuickAccessItem(new(graphics, new(Point.Empty, bitmap.Size), ribbon.Theme, ribbon.Font, dpi, hot, pressed), entry, menu); return bitmap;
            }
        }
        Assert.True(box.Enabled); Assert.True(radio.Enabled); Assert.True(radio.Checked);
    });

    private static void SamePixels(Bitmap expected, Bitmap actual)
    {
        Assert.Equal(expected.Size, actual.Size);
        for (int y = 0; y < actual.Height; y++) for (int x = 0; x < actual.Width; x++) Assert.Equal(expected.GetPixel(x, y), actual.GetPixel(x, y));
    }

    private sealed class Icon : IRibbonIcon
    {
        internal RibbonIconContext Context; internal Rectangle Bounds;
        public void Draw(Graphics graphics, Rectangle bounds, RibbonIconContext context)
        {
            Context = context; Bounds = bounds; using var brush = new SolidBrush(context.Foreground);
            graphics.FillRectangle(brush, Rectangle.Inflate(bounds, -3, -3));
        }
    }

    private sealed class State : INotifyPropertyChanged
    {
        private CheckState _check;
        public CheckState Check { get => _check; set { if (_check == value) return; _check = value; PropertyChanged?.Invoke(this, new(nameof(Check))); } }
        public event PropertyChangedEventHandler? PropertyChanged;
    }
    private sealed class Recording
    {
        internal int Indicators; internal string? Caption;
    }
    private sealed class Office : RibbonOffice2024Renderer
    {
        internal readonly Recording Record = new();
        public override void DrawCheckBoxIndicator(RibbonPaintContext c, RibbonCheckBoxItem item) { Record.Indicators++; base.DrawCheckBoxIndicator(c, item); }
        public override void DrawRadioButtonIndicator(RibbonPaintContext c, RibbonRadioButtonItem item) { Record.Indicators++; base.DrawRadioButtonIndicator(c, item); }
        public override void DrawText(RibbonPaintContext c, string text, Color color, TextFormatFlags flags) { Record.Caption = text; base.DrawText(c, text, color, flags); }
    }
    private sealed class Flat : RibbonRenderer
    {
        internal readonly Recording Record = new();
        public override void DrawCheckBoxIndicator(RibbonPaintContext c, RibbonCheckBoxItem item) { Record.Indicators++; base.DrawCheckBoxIndicator(c, item); }
        public override void DrawRadioButtonIndicator(RibbonPaintContext c, RibbonRadioButtonItem item) { Record.Indicators++; base.DrawRadioButtonIndicator(c, item); }
        public override void DrawText(RibbonPaintContext c, string text, Color color, TextFormatFlags flags) { Record.Caption = text; base.DrawText(c, text, color, flags); }
    }
}
