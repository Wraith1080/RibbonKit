using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class HiddenDisabledComboTests
{
    [Fact]
    public void Motion_inside_the_same_combo_field_does_not_keep_repainting_its_native_text() => FoundationTests.Sta(() =>
    {
        using var form = new RibbonForm { Size = new(1000, 650), Backdrop = RibbonWindowBackdrop.Mica };
        using var ribbon = new RibbonControl(); var choice = new RibbonComboBoxItem { Text = "Font" }; choice.Items.Add("Segoe UI"); choice.SelectedIndex = 0;
        var group = new RibbonGroup(); group.Items.Add(choice); var tab = new RibbonTab(); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        form.Controls.Add(ribbon); form.Ribbon = ribbon; _ = form.Handle; _ = ribbon.Handle; form.PerformLayout(); ribbon.SyncEditors();
        var editor = ribbon.Editors.EditorFor(choice)!; var presenter = editor.Parent!; _ = presenter.Handle;
        int paints = 0; presenter.Invalidated += (_, _) => paints++;
        var method = presenter.GetType().GetMethod("OnMouseMove", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        for (int i = 0; i < 8; i++) method.Invoke(presenter, [new MouseEventArgs(MouseButtons.None, 0, editor.Left + 4 + i, editor.Top + editor.Height / 2, 0)]);
        Assert.Equal(1, paints); Assert.Equal("Segoe UI", editor.Text); Assert.False(form.Visible);
    });
    [Fact]
    public void Synchronizing_unchanged_disabled_choice_does_not_request_another_presenter_paint() => FoundationTests.Sta(() =>
    {
        using var form = new RibbonForm { Size = new(1100, 600) };
        using var ribbon = new RibbonControl();
        var combo = new RibbonComboBoxItem { Id = "disabled", Text = "Unavailable", Enabled = false };
        combo.Items.Add("Disabled choice"); combo.SelectedIndex = 0;
        var group = new RibbonGroup { Text = "Fields" }; group.Items.Add(combo);
        var tab = new RibbonTab { Text = "Inputs" }; tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        form.Controls.Add(ribbon); form.Ribbon = ribbon; _ = form.Handle; _ = ribbon.Handle; form.PerformLayout(); ribbon.SyncEditors();
        var editor = ribbon.Editors.EditorFor(combo)!; Assert.NotNull(editor); _ = editor.Handle;
        var presenter = editor.Parent!; _ = presenter.Handle;
        ribbon.SyncEditors(); int invalidations = 0;
        presenter.Invalidated += (_, _) => invalidations++;
        for (int i = 0; i < 5; i++) ribbon.SyncEditors();
        Assert.Equal(0, invalidations);
        Assert.False(editor.Enabled); Assert.Equal("Disabled choice", editor.Text); Assert.False(form.Visible);
        nint handle = editor.Handle;
        combo.Text = "Renamed unavailable field"; ribbon.SyncEditors();
        Assert.True(invalidations > 0, "A changed label must still repaint."); invalidations = 0;
        combo.Items.Add("Updated choice"); combo.SelectedIndex = 1; ribbon.SyncEditors();
        Assert.True(invalidations > 0, "Changed choices/value must still repaint.");
        Assert.Equal("Updated choice", editor.Text); invalidations = 0;
        ribbon.Theme = ribbon.Theme with { Border = Color.Green }; ribbon.SyncEditors();
        Assert.True(invalidations > 0, "An updated palette must still repaint."); invalidations = 0;
        using var renderer = new RibbonRenderer(); ribbon.Renderer = renderer; ribbon.SyncEditors();
        Assert.True(invalidations > 0, "Replacing the renderer must still repaint."); ribbon.Renderer = null; invalidations = 0;
        combo.Enabled = true; combo.Enabled = false; ribbon.SyncEditors();
        Assert.True(invalidations > 0, "Disabling a field again must refresh its disabled presentation."); invalidations = 0;
        for (int i = 0; i < 3; i++) ribbon.SyncEditors();
        Assert.Equal(0, invalidations); Assert.Equal(handle, editor.Handle); Assert.False(editor.Enabled);
    });
}
