using System.Runtime.InteropServices;
using Xunit;

namespace RibbonKit.WinForms.Tests;

// No Show, Activate, Focus or input injection: these tests never take over the desktop.
public sealed class HiddenEditorCompositionTests
{
    private sealed class Fixture : IDisposable
    {
        internal readonly RibbonForm Form = new() { Size = new(1100, 650), Backdrop = RibbonWindowBackdrop.Acrylic };
        internal readonly RibbonControl Ribbon = new() { Size = new(1000, 220) };
        internal readonly RibbonTextBoxItem Name = new() { Id = "name", Text = "Name", Value = "Retained name" };
        internal readonly RibbonComboBoxItem Font = new() { Id = "font", Text = "Font", IsEditable = true, Value = "Retained font" };
        internal Fixture()
        {
            var group = new RibbonGroup { Text = "Fields" }; group.Items.AddRange(Name, Font);
            var tab = new RibbonTab { Text = "Inputs" }; tab.Groups.Add(group); Ribbon.Tabs.Add(tab);
            Form.Controls.Add(Ribbon); Form.Ribbon = Ribbon;
            _ = Form.Handle; _ = Ribbon.Handle; Form.PerformLayout(); Ribbon.SyncEditors();
            CompleteNativeParenting();
        }
        internal void CompleteNativeParenting()
        {
            // WinForms intentionally parks invisible controls. Complete only
            // their native parenting here, without showing/activating any window.
            SetParent(Ribbon.Handle, Form.Handle);
            foreach (var editor in new Control[] { Ribbon.Editors.EditorFor(Name)!, Ribbon.Editors.EditorFor(Font)! })
            { SetParent(editor.Parent!.Handle, Ribbon.Handle); SetParent(editor.Handle, editor.Parent.Handle); }
        }
        public void Dispose() { Form.Dispose(); }
    }

    [Fact]
    public void Retained_editor_leaving_material_host_releases_only_its_library_composition() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture();
        if (f.Form.EffectiveBackdrop == RibbonWindowBackdrop.None) return;
        var field = f.Ribbon.Editors.EditorFor(f.Name)!;
        Assert.NotNull(field); nint handle = field.Handle; var presenter = field.Parent!;
        _ = presenter.Handle; f.Ribbon.PrepareBackdropEditors(true);
        Assert.True(GetLayeredWindowAttributes(presenter.Handle, out _, out byte alpha, out _), $"root={GetAncestor(presenter.Handle, 2)}; host={f.Form.Handle}; parent={presenter.Parent?.GetType().Name}; material={f.Form.EffectiveBackdrop}"); Assert.Equal(255, alpha);
        Assert.False(ManagedBuffered(presenter)); // Native composition owns buffering over material.
        using var popup = new Form(); var surface = new Panel(); popup.Controls.Add(surface); _ = popup.Handle; _ = surface.Handle;
        SetParent(surface.Handle, popup.Handle);
        surface.Controls.Add(presenter); SetParent(presenter.Handle, surface.Handle); f.Ribbon.PrepareBackdropEditors(true);
        Assert.False(RibbonDrawingHold.HasOpacity(presenter.Handle));
        Assert.True(ManagedBuffered(presenter)); // Ordinary opaque popup returns to normal WinForms buffering.
        Assert.Equal(handle, field.Handle); Assert.Equal("Retained name", field.Text);
        f.Ribbon.Controls.Add(presenter); SetParent(presenter.Handle, f.Ribbon.Handle); f.Ribbon.SyncEditors(); f.Ribbon.PrepareBackdropEditors(true);
        Assert.True(GetLayeredWindowAttributes(presenter.Handle, out _, out alpha, out _)); Assert.Equal(255, alpha);
        Assert.False(ManagedBuffered(presenter));
        Assert.Equal(handle, field.Handle); Assert.False(f.Form.Visible); Assert.False(popup.Visible);
    });

    private static bool ManagedBuffered(Control control) => (bool)typeof(Control).GetMethod("GetStyle",
        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.Invoke(control, [ControlStyles.OptimizedDoubleBuffer])!;

    [Fact]
    public void Mask_interruptions_and_repeated_sync_restore_both_native_fields_without_recreating_them() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture();
        if (f.Form.EffectiveBackdrop == RibbonWindowBackdrop.None) return;
        var text = f.Ribbon.Editors.EditorFor(f.Name)!; var combo = f.Ribbon.Editors.EditorFor(f.Font)!;
        Assert.NotNull(text); Assert.NotNull(combo); nint textHandle = text.Handle, comboHandle = combo.Handle;
        _ = text.Parent!.Handle; _ = combo.Parent!.Handle; f.Ribbon.PrepareBackdropEditors(true);
        for (int i = 0; i < 5; i++)
        {
            f.Ribbon.SetBackdropBodyMasked(true); f.Ribbon.SyncEditors(); f.CompleteNativeParenting(); f.Ribbon.PrepareBackdropEditors(true);
            foreach (var editor in new Control[] { text, combo })
            { Assert.True(GetLayeredWindowAttributes(editor.Parent!.Handle, out _, out byte alpha, out _)); Assert.Equal(1, alpha); }
            f.Ribbon.SetBackdropBodyMasked(false); f.Ribbon.SyncEditors(); f.CompleteNativeParenting(); f.Ribbon.PrepareBackdropEditors(true);
            foreach (var editor in new Control[] { text, combo })
            { Assert.True(GetLayeredWindowAttributes(editor.Parent!.Handle, out _, out byte alpha, out _)); Assert.Equal(255, alpha); }
        }
        Assert.Equal(textHandle, text.Handle); Assert.Equal(comboHandle, combo.Handle);
        Assert.Equal("Retained name", text.Text); Assert.Equal("Retained font", combo.Text); Assert.False(f.Form.Visible);
    });

    [DllImport("user32.dll")] private static extern bool GetLayeredWindowAttributes(nint window, out uint key, out byte alpha, out uint flags);
    [DllImport("user32.dll")] private static extern nint GetAncestor(nint window, uint flags);
    [DllImport("user32.dll")] private static extern nint SetParent(nint child, nint parent);
}
