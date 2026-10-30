using System.Runtime.InteropServices;
using Xunit;

namespace RibbonKit.WinForms.Tests;

// Native handles only: no visible windows, activation or desktop input.
public sealed class HiddenEditorTabReturnTests
{
    private sealed class Fixture : IDisposable
    {
        internal readonly RibbonForm Form;
        internal readonly RibbonControl Ribbon = new() { Size = new(1100, 220) };
        internal readonly RibbonTextBoxItem Text = new() { Id = "name", Text = "Name", Value = "Retained text" };
        internal readonly RibbonComboBoxItem Choice = new() { Id = "choice", Text = "Choice", SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
        internal readonly RibbonComboBoxItem Editable = new() { Id = "font", Text = "Font", IsEditable = true, Value = "Free text", SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
        internal readonly RibbonTab Inputs = new() { Text = "Inputs" }, Other = new() { Text = "Other" };
        internal readonly Control[] Editors;
        internal Fixture(bool material, RibbonLayoutMode mode = RibbonLayoutMode.Classic)
        {
            Form = new() { Size = new(1200, 650), Backdrop = material ? RibbonWindowBackdrop.Acrylic : RibbonWindowBackdrop.None };
            Choice.Items.AddRange("First", "Second"); Choice.SelectedIndex = 1;
            var group = new RibbonGroup { Text = "Fields" }; group.Items.AddRange(Text, Choice, Editable);
            Inputs.Groups.Add(group); Ribbon.Tabs.AddRange(Inputs, Other); Ribbon.LayoutMode = mode;
            Form.Controls.Add(Ribbon); Form.Ribbon = Ribbon;
            _ = Form.Handle; _ = Ribbon.Handle; Form.PerformLayout(); Ribbon.SyncEditors();
            Editors = [Ribbon.Editors.EditorFor(Text)!, Ribbon.Editors.EditorFor(Choice)!, Ribbon.Editors.EditorFor(Editable)!];
            SetParent(Ribbon.Handle, Form.Handle);
            foreach (var editor in Editors) { SetParent(editor.Parent!.Handle, Ribbon.Handle); SetParent(editor.Handle, editor.Parent.Handle); }
            Ribbon.AnimationLevel = RibbonAnimationLevel.Subtle; Ribbon.Motion.SystemPolicyOverride = true;
        }
        public void Dispose() => Form.Dispose();
    }

    [Theory]
    [InlineData(false, RibbonLayoutMode.Classic)] [InlineData(true, RibbonLayoutMode.Classic)]
    [InlineData(false, RibbonLayoutMode.Simplified)] [InlineData(true, RibbonLayoutMode.Simplified)]
    public void Returning_fields_keep_handles_values_and_selection_without_repeated_refreshes(bool material, RibbonLayoutMode mode) => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(material, mode);
        var handles = f.Editors.Select(e => e.Handle).ToArray();
        // TextBox commands remain in full groups in Simplified mode; only the
        // two explicitly inline ComboBoxes have a live presentation there.
        var presenters = f.Editors.Skip(mode == RibbonLayoutMode.Simplified ? 1 : 0).Select(e => e.Parent!).ToArray();
        var text = (TextBox)f.Editors[0]; var editable = (ComboBox)f.Editors[2];
        text.Select(2, 4); editable.Select(1, 3);
        int commits = 0; f.Text.ValueCommitted += (_, _) => commits++; f.Editable.ValueCommitted += (_, _) => commits++;
        for (int round = 0; round < 3; round++)
        {
            f.Ribbon.SelectedTab = f.Other;
            f.Choice.SelectedIndex = round % 2;
            Assert.Equal(round % 2, f.Choice.SelectedIndex);
            f.Ribbon.SelectedTab = f.Inputs;
            Assert.Equal(round % 2, f.Choice.SelectedIndex);
            Assert.Equal(handles, f.Editors.Select(e => e.Handle));
            Assert.Equal("Retained text", text.Text); Assert.Equal("Free text", editable.Text);
            Assert.Equal(round % 2, ((ComboBox)f.Editors[1]).SelectedIndex);
            Assert.Equal((2, 4), (text.SelectionStart, text.SelectionLength));
            Assert.Equal((1, 3), (editable.SelectionStart, editable.SelectionLength));
        }
        var invalidations = new int[presenters.Length];
        for (int i = 0; i < presenters.Length; i++) { int index = i; presenters[i].Invalidated += (_, _) => invalidations[index]++; }
        for (int i = 0; i < 5; i++) f.Ribbon.SyncEditors();
        Assert.All(invalidations, count => Assert.Equal(0, count));
        f.Text.Value = "Updated text"; f.Choice.SelectedIndex = 1 - f.Choice.SelectedIndex; f.Editable.Text = "Renamed font";
        Assert.All(invalidations, count => Assert.True(count > 0, "Changed fields must still repaint."));
        if (mode == RibbonLayoutMode.Classic) Assert.Equal("Updated text", text.Text);
        Assert.Equal(f.Choice.Value, f.Editors[1].Text);
        Assert.Equal(handles, f.Editors.Select(e => e.Handle)); Assert.Equal(0, commits);
        Assert.False(f.Form.Visible); Assert.False(f.Ribbon.IsAnimating);
    });

    [Theory] [InlineData(false)] [InlineData(true)]
    public void Idle_motion_and_closed_list_cleanup_do_not_invalidate_native_fields(bool material) => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(material);
        f.Ribbon.SelectedTab = f.Other; f.Ribbon.SelectedTab = f.Inputs;
        int invalidations = 0;
        f.Ribbon.Invalidated += (_, _) => invalidations++;
        foreach (var editor in f.Editors) { editor.Invalidated += (_, _) => invalidations++; editor.Parent!.Invalidated += (_, _) => invalidations++; }
        for (int i = 0; i < 5; i++) f.Ribbon.SettleMotion();
        Assert.Equal(0, invalidations); Assert.False(f.Form.Visible);
        for (int i = 0; i < 5; i++) f.Ribbon.Editors.CloseLists();
        Assert.Equal(0, invalidations);
    });

    [DllImport("user32.dll")] private static extern nint SetParent(nint child, nint parent);
}
