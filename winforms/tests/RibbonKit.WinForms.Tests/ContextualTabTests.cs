using System.ComponentModel;
using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class ContextualTabTests
{
    [Fact]
    public void Contextual_metadata_binds_without_changing_visibility_selection_or_measurement() => FoundationTests.Sta(() =>
    {
        using var form = new Form { Width = 1000 }; var ribbon = new RibbonControl(); form.Controls.Add(ribbon);
        var home = new RibbonTab { Text = "Home" }; var context = new RibbonTab { Text = "Picture Format" }; ribbon.Tabs.AddRange(home, context);
        var metadata = TypeDescriptor.GetProperties(context);
        Assert.False(context.IsContextual); Assert.True(context.ContextualColor.IsEmpty);
        foreach (string name in new[] { nameof(context.IsContextual), nameof(context.ContextualColor) })
        {
            Assert.True(((BindableAttribute)metadata[name]!.Attributes[typeof(BindableAttribute)]!).Bindable);
            Assert.Equal(DesignerSerializationVisibility.Visible, metadata[name]!.SerializationVisibility);
            Assert.False(metadata[name]!.ShouldSerializeValue(context));
        }
        Assert.Equal(Color.Empty, ((DefaultValueAttribute)metadata[nameof(context.ContextualColor)]!.Attributes[typeof(DefaultValueAttribute)]!).Value);
        using var binding = new BindingSource(); var state = new State(); binding.DataSource = state;
        context.DataBindings.Add(new Binding(nameof(context.IsContextual), binding, nameof(state.Active), false, DataSourceUpdateMode.OnPropertyChanged));
        context.DataBindings.Add(new Binding(nameof(context.ContextualColor), binding, nameof(state.Tint), false, DataSourceUpdateMode.OnPropertyChanged));
        form.Show(); Application.DoEvents(); var snapshot = ribbon.Snapshot; int flags = 0, colors = 0;
        context.IsContextualChanged += (_, _) => flags++; context.ContextualColorChanged += (_, _) => colors++;
        state.Active = true; state.Tint = Color.Purple;
        Assert.True(context.IsContextual); Assert.Equal(Color.Purple, context.ContextualColor); Assert.Equal(1, flags); Assert.Equal(1, colors);
        Assert.Same(snapshot, ribbon.Snapshot); Assert.Same(home, ribbon.SelectedTab); Assert.True(context.Visible);
        context.IsContextual = false; context.ContextualColor = Color.Teal; context.ContextualColor = Color.Teal;
        Assert.False(state.Active); Assert.Equal(Color.Teal, state.Tint); Assert.Equal(2, flags); Assert.Equal(2, colors);
        metadata[nameof(context.ContextualColor)]!.ResetValue(context); Assert.True(context.ContextualColor.IsEmpty); Assert.True(state.Tint.IsEmpty);
    });

    [Fact]
    public void Contextual_visibility_and_lifetime_reuse_selection_fallback_and_source_qat_rules() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { Width = 1000 };
        var home = new RibbonTab { Text = "Home" }; var context = new RibbonTab { Text = "Format", IsContextual = true, Visible = false }; var after = new RibbonTab { Text = "View" };
        var group = new RibbonGroup(); var button = new RibbonButtonItem { Text = "Crop" }; group.Items.Add(button); context.Groups.Add(group); ribbon.Tabs.AddRange(home, context, after);
        var reference = new RibbonQuickAccessReference { Source = button }; ribbon.QuickAccessItems.Add(reference); int clicks = 0; button.Click += (_, _) => clicks++;
        Assert.Same(home, ribbon.SelectedTab); Assert.DoesNotContain(ribbon.Snapshot.Targets, t => ReferenceEquals(t.Element, context)); Assert.False(RibbonQuickAccess.Available(reference));
        context.Visible = true; Assert.Same(home, ribbon.SelectedTab); Assert.True(RibbonQuickAccess.Available(reference));
        ribbon.Activate(ribbon.Snapshot.Targets.Single(t => ReferenceEquals(t.Element, reference))); Assert.Equal(1, clicks); Assert.Same(home, ribbon.SelectedTab);
        ribbon.SelectedTab = context; context.Visible = false; Assert.Same(after, ribbon.SelectedTab); Assert.False(RibbonQuickAccess.Available(reference));
        context.Visible = true; ribbon.SelectedTab = context; context.Enabled = false; Assert.Same(after, ribbon.SelectedTab);
        context.Enabled = true; Assert.Same(after, ribbon.SelectedTab); ribbon.SelectedTab = context; ribbon.Tabs.Remove(context);
        Assert.Same(after, ribbon.SelectedTab); Assert.False(button.IsDisposed); Assert.False(RibbonQuickAccess.Available(reference));
        ribbon.Tabs.Insert(1, context); Assert.Same(after, ribbon.SelectedTab); Assert.True(RibbonQuickAccess.Available(reference));
        ribbon.SelectedTab = context; context.Dispose(); Assert.Same(after, ribbon.SelectedTab); Assert.True(button.IsDisposed); Assert.False(RibbonQuickAccess.Available(reference));
    });

    [Fact]
    public void Contextual_colors_and_office_underline_follow_state_dpi_and_theme_fallback() => FoundationTests.Sta(() =>
    {
        using var tab = new RibbonTab { Text = "Picture Format", IsContextual = true, ContextualColor = Color.FromArgb(196, 62, 150) };
        using var office = new Office(); using var flat = new Flat(); var theme = RibbonThemes.Office2024Light;
        foreach (var (renderer, record) in new (RibbonRenderer, Recording)[] { (office, office.Record), (flat, flat.Record) })
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        {
            int S(int value) => RibbonLayoutEngine.Scale(value, dpi);
            using var bitmap = new Bitmap(S(180), S(32)); using var graphics = Graphics.FromImage(bitmap);
            var context = new RibbonPaintContext(graphics, new(Point.Empty, bitmap.Size), theme, SystemFonts.DefaultFont, dpi);
            Draw(false); Assert.Equal(Color.FromArgb(220, 139, 192), record.Text);
            Assert.Equal(theme.Background.ToArgb(), bitmap.GetPixel(S(90), 0).ToArgb());
            Draw(true); Assert.Equal(tab.ContextualColor, record.Text);
            if (renderer is Office) Assert.Equal(tab.ContextualColor.ToArgb(), bitmap.GetPixel(S(90), bitmap.Height - S(2)).ToArgb());
            Draw(false, true, true); Assert.Equal(Color.FromArgb(220, 139, 192), record.Text);
            tab.Enabled = false; Draw(true, true, true); Assert.Equal(SystemColors.GrayText, record.Text);
            Assert.Equal(theme.Background.ToArgb(), bitmap.GetPixel(S(90), bitmap.Height - S(2)).ToArgb()); tab.Enabled = true;
            tab.ContextualColor = Color.Empty; Draw(true); Assert.Equal(theme.Accent, record.Text);
            var different = context with { Theme = theme with { Accent = Color.DarkGreen } };
            renderer.DrawTab(different, tab, true); Assert.Equal(Color.DarkGreen.ToArgb(), record.Text.ToArgb());
            tab.ContextualColor = Color.FromArgb(128, 0, 0, 0); Draw(true); Assert.Equal(Color.FromArgb(127, 127, 127), record.Text);
            tab.IsContextual = false; Draw(false); Assert.Equal(theme.Foreground, record.Text);
            tab.IsContextual = true; tab.ContextualColor = Color.FromArgb(196, 62, 150);

            void Draw(bool selected, bool hot = false, bool pressed = false) { graphics.Clear(theme.Background); renderer.DrawTab(context with { Hot = hot, Pressed = pressed }, tab, selected); }
        }
    });

    private sealed class State : INotifyPropertyChanged
    {
        private bool _active; private Color _tint = Color.Empty;
        public bool Active { get => _active; set { if (_active == value) return; _active = value; PropertyChanged?.Invoke(this, new(nameof(Active))); } }
        public Color Tint { get => _tint; set { if (_tint == value) return; _tint = value; PropertyChanged?.Invoke(this, new(nameof(Tint))); } }
        public event PropertyChangedEventHandler? PropertyChanged;
    }
    private sealed class Recording { internal Color Text; }
    private sealed class Office : RibbonOffice2024Renderer
    {
        internal readonly Recording Record = new();
        public override void DrawText(RibbonPaintContext c, string text, Color color, TextFormatFlags flags) { Record.Text = color; base.DrawText(c, text, color, flags); }
    }
    private sealed class Flat : RibbonRenderer
    {
        internal readonly Recording Record = new();
        public override void DrawText(RibbonPaintContext c, string text, Color color, TextFormatFlags flags) { Record.Text = color; base.DrawText(c, text, color, flags); }
    }
}
