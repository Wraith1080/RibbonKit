using System.ComponentModel;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class ComboBoxTests
{
    [Fact]
    public void Choice_mutations_preserve_surviving_selection_and_editable_text()
    {
        using var item = new RibbonComboBoxItem();
        item.Items.AddRange("Alpha", "Beta", "Beta");
        item.SelectedIndex = 2;
        item.Value = "Beta";
        Assert.Equal(2, item.SelectedIndex);
        item.Items.Insert(0, "Start"); Assert.Equal(3, item.SelectedIndex);
        item.Items.RemoveAt(1); Assert.Equal(2, item.SelectedIndex);
        item.Items[2] = "Gamma"; Assert.Equal("Gamma", item.Value);
        item.Items.RemoveAt(2); Assert.Equal(-1, item.SelectedIndex); Assert.Equal("", item.Value);
        item.IsEditable = true; item.Value = "Custom"; Assert.Equal(-1, item.SelectedIndex);
        item.Items.Clear(); Assert.Equal("Custom", item.Value);
        item.IsEditable = false; Assert.Equal("", item.Value);
    }

    [Fact]
    public void Deferred_binding_waits_for_validation_and_escape_restores_the_committed_choice() => FoundationTests.Sta(() =>
    {
        using var form = new Form { Width = 800 };
        var ribbon = new RibbonControl(); var tab = new RibbonTab(); var group = new RibbonGroup();
        var item = new RibbonComboBoxItem { IsEditable = true }; item.Items.AddRange("Alpha", "Beta");
        group.Items.Add(item); tab.Groups.Add(group); ribbon.Tabs.Add(tab); form.Controls.Add(ribbon);
        using var source = new BindingSource(); var state = new State { Value = "Alpha" }; source.DataSource = state;
        item.DataBindings.Add(new Binding(nameof(item.Value), source, nameof(state.Value), true, DataSourceUpdateMode.OnValidation));
        item.ValueValidating += (_, e) => e.Cancel = item.Value == "blocked";
        int commits = 0; item.ValueCommitted += (_, _) => commits++;
        form.Show(); Application.DoEvents(); item.PerformClick();
        var editor = ribbon.Editors.EditorFor(item)!;
        editor.Text = "blocked"; Key(editor, Keys.Enter);
        Assert.Equal("Alpha", state.Value); Assert.True(editor.ContainsFocus);
        editor.Text = "Beta"; Key(editor, Keys.Enter);
        Assert.Equal("Beta", state.Value); Assert.Equal(1, item.SelectedIndex); Assert.Equal(1, commits);
        editor.Text = "Custom"; Key(editor, Keys.Escape);
        Assert.Equal("Beta", item.Value); Assert.Equal(1, item.SelectedIndex); Assert.Equal("Beta", state.Value);
        form.Close();
    });

    [Fact]
    public void Native_list_refresh_and_style_changes_reuse_the_control_and_keep_model_state() => FoundationTests.Sta(() =>
    {
        using var form = new Form { Width = 800 };
        var ribbon = new RibbonControl(); var tab = new RibbonTab(); var group = new RibbonGroup();
        var item = new RibbonComboBoxItem(); item.Items.AddRange("Alpha", "Beta"); item.SelectedIndex = 1;
        using var source = new BindingSource(); var state = new State { Index = 1 }; source.DataSource = state;
        item.DataBindings.Add(new Binding(nameof(item.SelectedIndex), source, nameof(state.Index), true, DataSourceUpdateMode.OnPropertyChanged));
        group.Items.Add(item); tab.Groups.Add(group); ribbon.Tabs.Add(tab); form.Controls.Add(ribbon);
        Rectangle openingBounds = default;
        form.Load += (_, _) => openingBounds = ribbon.Bounds;
        form.Show(); Application.DoEvents();
        var editor = ribbon.Editors.EditorFor(item)!;
        Assert.Equal(openingBounds, ribbon.Bounds);
        AssertPaintKeepsNativeBounds();
        Assert.Equal(ComboBoxStyle.DropDownList, editor.DropDownStyle);
        Assert.Equal(1, editor.SelectedIndex); Assert.Equal("Beta", editor.Text);
        var other = new RibbonTab { Text = "Other" }; ribbon.Tabs.Add(other);
        ribbon.SelectedTab = other; Assert.False(editor.Visible);
        ribbon.SelectedTab = tab; Assert.True(editor.Visible);
        Assert.Same(editor, ribbon.Editors.EditorFor(item));
        ribbon.Visible = false; Assert.False(editor.Visible);
        ribbon.Visible = true; Assert.True(editor.Visible);
        editor.SelectedIndex = 0; Assert.Equal(0, state.Index);
        state.Index = 1; Assert.Equal(1, editor.SelectedIndex);
        item.Items.Insert(0, "Start"); Assert.Equal(2, editor.SelectedIndex); Assert.Equal("Beta", editor.Text);
        item.Items[2] = "Updated"; Assert.Equal("Updated", editor.Text);
        item.IsEditable = true; item.Value = "Free text";
        Assert.Same(editor, ribbon.Editors.EditorFor(item)); Assert.Equal(ComboBoxStyle.DropDown, editor.DropDownStyle);
        AssertPaintKeepsNativeBounds();
        Assert.Equal("Free text", editor.Text); Assert.Equal(-1, editor.SelectedIndex);
        item.Value = "alpha";
        Assert.Equal("alpha", editor.Text); Assert.Equal(-1, editor.SelectedIndex);
        item.IsEditable = false; item.SelectedIndex = 1;
        item.ShowDropDown(); Assert.True(item.IsDropDownOpen); Assert.False(editor.DroppedDown);
        Assert.True(ribbon.Editors.ProcessListKey(Keys.End)); Assert.Equal("Updated", item.Value);
        Assert.True(ribbon.Editors.ProcessListKey(Keys.Escape)); Assert.Equal("Alpha", item.Value);
        item.ValueCommitted += (_, _) => item.CloseDropDown();
        item.ShowDropDown(); Assert.True(ribbon.Editors.ProcessListKey(Keys.End));
        Assert.True(ribbon.Editors.ProcessListKey(Keys.Enter));
        Assert.Equal("Updated", item.Value); Assert.False(item.IsDropDownOpen);
        item.Items.Clear(); item.ShowDropDown(); Assert.True(item.IsDropDownOpen);
        Assert.True(ribbon.Editors.ProcessListKey(Keys.Down)); Assert.Equal(-1, item.SelectedIndex);
        item.CloseDropDown(); Assert.False(item.IsDropDownOpen);
        item.Items.AddRange("Alpha", "Beta"); item.SelectedIndex = 0;
        item.ShowDropDown();
        ribbon.Theme = ribbon.Theme with { PopupCornerRadius = 8 };
        Assert.False(item.IsDropDownOpen);
        item.ShowDropDown();
        item.ValueChanged += (_, _) => group.Items.Remove(item);
        Assert.True(ribbon.Editors.ProcessListKey(Keys.End));
        Assert.True(editor.IsDisposed); Assert.False(item.IsDropDownOpen); Assert.False(item.IsDisposed);
        item.Dispose(); form.Close();

        void AssertPaintKeepsNativeBounds()
        {
            using var positions = new PositionWatch(editor);
            var bounds = editor.Bounds;
            ribbon.Refresh();
            Assert.Equal(bounds, editor.Bounds);
            Assert.Equal(0, positions.Changes);
            Assert.Equal(0, positions.Selections);
        }
    });

    private sealed class PositionWatch : NativeWindow, IDisposable
    {
        internal int Changes;
        internal int Selections;
        internal PositionWatch(Control control) => AssignHandle(control.Handle);
        protected override void WndProc(ref Message message)
        {
            if (message.Msg == 0x47) Changes++; // WM_WINDOWPOSCHANGED
            if (message.Msg == 0x142) Selections++; // CB_SETEDITSEL
            base.WndProc(ref message);
        }
        public void Dispose() => ReleaseHandle();
    }

    private static void Key(ComboBox editor, Keys key)
    {
        var message = Message.Create(editor.Handle, 0x100, (nint)key, 0);
        Assert.True(editor.PreProcessMessage(ref message));
        Application.DoEvents();
    }
    private sealed class State : INotifyPropertyChanged
    {
        private string _value = "";
        private int _index;
        public string Value { get => _value; set { if (_value == value) return; _value = value; PropertyChanged?.Invoke(this, new(nameof(Value))); } }
        public int Index { get => _index; set { if (_index == value) return; _index = value; PropertyChanged?.Invoke(this, new(nameof(Index))); } }
        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
