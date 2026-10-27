using System.ComponentModel;
using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class TextBoxTests
{
    [Theory]
    [InlineData(120)]
    [InlineData(192)]
    public void Fields_keep_compact_rows_and_align_editor_edges_across_labels_and_reduction(int dpi) => FoundationTests.Sta(() =>
    {
        using var group = new RibbonGroup();
        group.Items.AddRange(new RibbonTextBoxItem { Text = "Name" }, new RibbonComboBoxItem { Text = "Description" }, new RibbonTextBoxItem());
        using var font = new Font("Segoe UI", 9 * dpi / 96f);
        var engine = new RibbonLayoutEngine();
        foreach (int state in new[] { 0, 1, 2 })
        {
            var measured = engine.Measure(group, state, font, dpi, RibbonThemes.Office2024Light);
            var fields = measured.Items.Select(i => RibbonEditorLayout.Field(i.Bounds, i.Item, i.LabelWidth, font, dpi)).ToArray();
            Assert.Single(fields.Select(f => f.Left).Distinct());
            Assert.Single(fields.Select(f => f.Right).Distinct());
            Assert.All(fields, f => Assert.Equal(RibbonLayoutEngine.Scale(120, dpi), f.Width));
            Assert.All(measured.Items, i => Assert.Equal(RibbonItemSize.Medium, i.Size));
            Assert.True(fields.Zip(fields.Skip(1)).All(pair => pair.First.Bottom <= pair.Second.Top));
        }
    });

    [Fact]
    public void Native_value_binding_commit_validation_and_escape_share_one_state_path() => FoundationTests.Sta(() =>
    {
        using var form = new Form { Width = 800 };
        var ribbon = new RibbonControl();
        var tab = new RibbonTab(); var group = new RibbonGroup(); var item = new RibbonTextBoxItem { Text = "Name" };
        var next = new RibbonButtonItem { Text = "Next", SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium) };
        group.Items.AddRange(item, next); tab.Groups.Add(group); ribbon.Tabs.Add(tab); form.Controls.Add(ribbon);
        using var source = new BindingSource(); var state = new State { Value = "initial" }; source.DataSource = state;
        item.DataBindings.Add(new Binding(nameof(item.Value), source, nameof(state.Value), true, DataSourceUpdateMode.OnPropertyChanged));
        form.Show(); Application.DoEvents(); item.PerformClick();
        var editor = Assert.IsAssignableFrom<TextBox>(ribbon.Editors.EditorFor(item));
        Assert.True(editor.Focused);
        var events = new List<string>();
        item.ValueValidating += (_, e) => { events.Add("validating"); e.Cancel = item.Value == "invalid"; };
        item.ValueValidated += (_, _) => events.Add("validated");
        item.ValueCommitted += (_, _) => events.Add("committed");
        item.EditCanceled += (_, _) => events.Add("canceled");
        editor.Text = "invalid";
        Assert.Equal("invalid", state.Value);
        Key(editor, Keys.Tab);
        Assert.True(editor.Focused);
        Assert.Equal(new[] { "validating" }, events);
        events.Clear(); editor.Text = "accepted"; Key(editor, Keys.Enter);
        Assert.Equal(new[] { "validating", "validated", "committed" }, events);
        events.Clear(); editor.Text = "draft"; Key(editor, Keys.Escape);
        Assert.Equal("accepted", item.Value); Assert.Equal("accepted", state.Value);
        Assert.Contains("canceled", events);
        state.Value = "external"; Application.DoEvents();
        Assert.Equal("external", editor.Text);
        form.Close();
    });

    [Fact]
    public void Validation_binding_waits_for_acceptance_and_readonly_retains_selection() => FoundationTests.Sta(() =>
    {
        using var form = new Form { Width = 800 };
        var ribbon = new RibbonControl(); var tab = new RibbonTab(); var group = new RibbonGroup();
        var item = new RibbonTextBoxItem { ReadOnly = true, MaxLength = 7 };
        group.Items.Add(item); tab.Groups.Add(group); ribbon.Tabs.Add(tab); form.Controls.Add(ribbon);
        using var source = new BindingSource(); var state = new State { Value = "initial" }; source.DataSource = state;
        item.DataBindings.Add(new Binding(nameof(item.Value), source, nameof(state.Value), true, DataSourceUpdateMode.OnValidation));
        item.ValueValidating += (_, e) => e.Cancel = item.Value == "blocked";
        form.Show(); Application.DoEvents(); item.PerformClick();
        var editor = ribbon.Editors.EditorFor(item)!;
        Assert.True(editor.ReadOnly); Assert.Equal(7, editor.MaxLength);
        editor.Select(2, 3); item.PerformClick(); Assert.Equal(2, editor.SelectionStart); Assert.Equal(3, editor.SelectionLength);
        item.ReadOnly = false; editor.Text = "blocked"; Key(editor, Keys.Enter);
        Assert.Equal("initial", state.Value); Assert.True(editor.Focused);
        item.ReadOnly = false; editor.Text = "draft";
        Assert.Equal("initial", state.Value);
        Key(editor, Keys.Enter); Assert.Equal("draft", state.Value);
        editor.Text = "cancel"; Key(editor, Keys.Escape);
        Assert.Equal("draft", item.Value); Assert.Equal("draft", state.Value);
        form.Close();
    });

    [Fact]
    public void Validation_callbacks_can_remove_the_editor_without_recursion_or_stale_focus() => FoundationTests.Sta(() =>
    {
        using var form = new Form { Width = 800 };
        var ribbon = new RibbonControl(); var tab = new RibbonTab(); var group = new RibbonGroup();
        using var item = new RibbonTextBoxItem { Value = "initial" };
        group.Items.Add(item); tab.Groups.Add(group); ribbon.Tabs.Add(tab); form.Controls.Add(ribbon);
        form.Show(); Application.DoEvents(); item.PerformClick();
        var editor = ribbon.Editors.EditorFor(item)!;
        int validations = 0; item.ValueValidating += (_, _) => { validations++; group.Items.Remove(item); };
        editor.Text = "draft"; Key(editor, Keys.Enter);
        Assert.Equal(1, validations); Assert.True(editor.IsDisposed); Assert.False(item.IsDisposed);
        form.Close();
    });

    private static void Key(TextBox editor, Keys key)
    {
        var message = Message.Create(editor.Handle, 0x100, (nint)(key & Keys.KeyCode), 0);
        Assert.True(editor.PreProcessMessage(ref message));
        Application.DoEvents();
    }
    private sealed class State : INotifyPropertyChanged
    {
        private string _value = "";
        public string Value { get => _value; set { if (_value == value) return; _value = value; PropertyChanged?.Invoke(this, new(nameof(Value))); } }
        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
