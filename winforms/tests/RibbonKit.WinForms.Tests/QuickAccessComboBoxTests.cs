using System.ComponentModel;
using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class QuickAccessComboBoxTests
{
    [Fact]
    public void Reference_metadata_ownership_and_mixed_width_overflow_preserve_declarations() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { Width = 800 }; using var other = new RibbonControl();
        var tab = new RibbonTab(); var group = new RibbonGroup(); var combo = new RibbonComboBoxItem { Text = "Font" };
        group.Items.Add(combo); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        var reference = new RibbonQuickAccessComboBoxReference { Source = combo };
        var button = new RibbonButtonItem { Text = "Save" }; ribbon.QuickAccessItems.AddRange(button, reference);
        var targets = ribbon.Snapshot.Targets.Where(t => t.Kind == TargetKind.QuickAccess).ToArray();
        Assert.Equal(RibbonLayoutEngine.Scale(28, ribbon.DeviceDpi), targets[0].Bounds.Width);
        Assert.Equal(RibbonLayoutEngine.Scale(40, ribbon.DeviceDpi), targets[1].Bounds.Width);
        Assert.Equal(targets[0].Bounds.Right, targets[1].Bounds.Left); Assert.Same(group, combo.Parent);
        var property = TypeDescriptor.GetProperties(reference)[nameof(reference.Source)]!;
        Assert.Equal(typeof(RibbonComboBoxItem), property.PropertyType); Assert.Equal(DesignerSerializationVisibility.Visible, property.SerializationVisibility);
        Assert.Equal(DesignerSerializationVisibility.Hidden, TypeDescriptor.GetProperties(reference)[nameof(reference.IsDropDownOpen)]!.SerializationVisibility);
        ribbon.QuickAccessMaxWidth = 56; Assert.Contains(reference, ribbon.Snapshot.QuickAccessOverflowItems);
        using var popup = new RibbonPopupSurface(ribbon, ribbon.QuickAccessOverflow, new(800, 600));
        Assert.Contains(popup.Targets, t => ReferenceEquals(t.Element, reference) && t.Kind == TargetKind.QuickAccess && t.Element is not IRibbonEditorItem);
        var foreignTab = new RibbonTab(); var foreignGroup = new RibbonGroup(); var foreign = new RibbonComboBoxItem(); foreignGroup.Items.Add(foreign); foreignTab.Groups.Add(foreignGroup); other.Tabs.Add(foreignTab);
        Assert.Throws<ArgumentException>(() => reference.Source = foreign); Assert.Same(combo, reference.Source);
        Assert.Throws<ArgumentException>(() => other.QuickAccessItems.Add(new RibbonQuickAccessComboBoxReference { Source = combo }));
        var icon = new EnabledIcon(); combo.Icon = icon; reference.Enabled = false;
        using (var painter = new RibbonRenderer())
        using (var bitmap = new Bitmap(200, 40))
        using (var graphics = Graphics.FromImage(bitmap))
            painter.DrawQuickAccessItem(new(graphics, new(0, 0, 200, 40), ribbon.Theme, ribbon.Font, ribbon.DeviceDpi), reference, true);
        Assert.False(icon.Enabled);
        reference.Dispose(); Assert.False(combo.IsDisposed); Assert.Same(group, combo.Parent);
    });

    [Fact]
    public void Inactive_chooser_shares_selection_validation_and_binding_without_another_native_editor() => FoundationTests.Sta(() =>
    {
        using var form = new Form { Width = 900 }; var ribbon = new RibbonControl(); form.Controls.Add(ribbon);
        var tab = new RibbonTab { Text = "Source" }; var inactive = new RibbonTab { Text = "Other" }; var group = new RibbonGroup();
        var combo = new RibbonComboBoxItem { Text = "Font", IsEditable = true }; combo.Items.AddRange("Alpha", "Beta", "Blocked"); combo.SelectedIndex = 0;
        group.Items.Add(combo); tab.Groups.Add(group); ribbon.Tabs.AddRange(tab, inactive);
        var first = new RibbonQuickAccessComboBoxReference { Source = combo }; var second = new RibbonQuickAccessComboBoxReference { Source = combo };
        ribbon.QuickAccessItems.AddRange(first, second);
        using var binding = new BindingSource(); var state = new State(); binding.DataSource = state;
        combo.DataBindings.Add(new Binding(nameof(combo.Value), binding, nameof(state.Value), false, DataSourceUpdateMode.OnValidation));
        int committed = 0; combo.ValueCommitted += (_, _) => committed++;
        combo.ValueValidating += (_, e) => e.Cancel = combo.Value == "Blocked";
        form.Show(); Application.DoEvents(); var editor = ribbon.Editors.EditorFor(combo)!;
        ribbon.SelectedTab = inactive;
        Open(first); Assert.True(first.IsDropDownOpen); Assert.False(second.IsDropDownOpen); Assert.False(combo.IsDropDownOpen);
        Assert.True(ribbon.Editors.ProcessListKey(Keys.Down)); Assert.Equal("Beta", combo.Value); Assert.Equal("Alpha", state.Value);
        Assert.True(ribbon.Editors.ProcessListKey(Keys.Escape)); Assert.Equal("Alpha", combo.Value); Assert.Equal(0, committed);
        Open(second); ribbon.Editors.ProcessListKey(Keys.End); ribbon.Editors.ProcessListKey(Keys.Enter);
        Assert.True(second.IsDropDownOpen); Assert.Equal("Alpha", state.Value); Assert.Equal(0, committed);
        ribbon.Editors.ProcessListKey(Keys.Escape); Assert.Equal("Alpha", combo.Value);
        Open(first); ribbon.Editors.ProcessListKey(Keys.Down); ribbon.Editors.ProcessListKey(Keys.Enter);
        Assert.False(first.IsDropDownOpen); Assert.Equal("Beta", state.Value); Assert.Equal(1, committed); Assert.Same(inactive, ribbon.SelectedTab);
        ribbon.SelectedTab = tab; Assert.Same(editor, ribbon.Editors.EditorFor(combo)); Assert.Equal(1, editor.SelectedIndex);
        Assert.Equal(ComboBoxStyle.DropDown, editor.DropDownStyle);

        void Open(RibbonQuickAccessComboBoxReference reference)
        {
            ribbon.FocusElement(reference); ribbon.Activate(ribbon.Snapshot.Targets.Single(t => ReferenceEquals(t.Element, reference)));
        }
    });

    [Fact]
    public void Mutation_removal_and_projection_replacement_close_stale_choices_safely() => FoundationTests.Sta(() =>
    {
        using var form = new Form { Width = 900 }; var ribbon = new RibbonControl(); form.Controls.Add(ribbon);
        var tab = new RibbonTab(); var group = new RibbonGroup(); var combo = new RibbonComboBoxItem { Text = "Font" };
        combo.Items.AddRange("Alpha", "Beta", "Gamma"); combo.SelectedIndex = 0; group.Items.Add(combo); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        var reference = new RibbonQuickAccessComboBoxReference { Source = combo }; ribbon.QuickAccessItems.Add(reference);
        form.Show(); Application.DoEvents();
        Open(); ribbon.Editors.ProcessListKey(Keys.Down); combo.Value = "Gamma";
        Assert.False(reference.IsDropDownOpen); Assert.Equal("Gamma", combo.Value);
        Open(); combo.Items.Insert(0, "Start"); Assert.False(reference.IsDropDownOpen); Assert.Equal("Gamma", combo.Value); Assert.Equal(3, combo.SelectedIndex);
        Open(); ribbon.QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon;
        Assert.False(reference.IsDropDownOpen);
        var replacement = new RibbonComboBoxItem { Text = "Replacement" }; replacement.Items.AddRange("One", "Two"); replacement.SelectedIndex = 0; group.Items.Add(replacement);
        CancelEventHandler replaceWhileValidating = (_, _) => reference.Source = replacement;
        Open(); ribbon.Editors.ProcessListKey(Keys.Home); combo.ValueValidating += replaceWhileValidating;
        ribbon.Editors.ProcessListKey(Keys.Enter); combo.ValueValidating -= replaceWhileValidating;
        Assert.False(reference.IsDropDownOpen); Assert.Same(replacement, reference.Source); Assert.Equal("Gamma", combo.Value); Assert.Equal("One", replacement.Value);
        reference.Source = combo;
        Open(); ribbon.QuickAccessItems.Remove(reference); Assert.False(reference.IsDropDownOpen); Assert.False(combo.IsDisposed);
        ribbon.QuickAccessItems.Add(reference); Open(); group.Items.Remove(combo); Assert.False(reference.IsDropDownOpen);
        Assert.False(RibbonQuickAccess.Available(reference)); group.Items.Add(combo); Assert.True(RibbonQuickAccess.Available(reference));
        Open(); combo.Dispose(); Assert.False(reference.IsDropDownOpen); Assert.False(RibbonQuickAccess.Available(reference));
        reference.Source = null; Assert.Null(reference.Source);

        void Open()
        {
            ribbon.FocusElement(reference); ribbon.Activate(ribbon.Snapshot.Targets.Single(t => ReferenceEquals(t.Element, reference)));
            Assert.True(reference.IsDropDownOpen);
        }
    });

    private sealed class State : INotifyPropertyChanged
    {
        private string _value = "Alpha";
        public string Value { get => _value; set { if (_value == value) return; _value = value; PropertyChanged?.Invoke(this, new(nameof(Value))); } }
        public event PropertyChangedEventHandler? PropertyChanged;
    }
    private sealed class EnabledIcon : IRibbonIcon
    {
        internal bool Enabled = true;
        public void Draw(Graphics graphics, Rectangle bounds, RibbonIconContext context) => Enabled = context.Enabled;
    }
}
