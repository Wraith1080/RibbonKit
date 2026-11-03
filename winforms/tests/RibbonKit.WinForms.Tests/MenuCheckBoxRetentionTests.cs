using System.Runtime.InteropServices;
using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class MenuCheckBoxRetentionTests
{
    [Theory]
    [InlineData("ordinary", RibbonDensity.Compact)] [InlineData("ordinary", RibbonDensity.Touch)]
    [InlineData("nested", RibbonDensity.Compact)] [InlineData("nested", RibbonDensity.Touch)]
    [InlineData("qat-menu", RibbonDensity.Compact)] [InlineData("qat-menu", RibbonDensity.Touch)]
    public void Checkbox_pointer_and_keyboard_toggle_without_replacing_the_menu_or_its_native_editor(string presentation, RibbonDensity density) => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(presentation, density); var popup = f.Open();
        var editor = f.Ribbon.Editors.EditorFor(f.Field)!; nint handle = editor.Handle;
        editor.Text = "Retained text"; editor.Select(2, 4);
        var events = new List<string>(); f.Check.CheckedChanged += (_, _) => events.Add("state"); f.Check.Click += (_, _) => events.Add(f.Check.Checked ? "checked click" : "unchecked click");
        var target = popup.Targets.Single(t => t.Element == f.Check);
        var point = new Point(target.Bounds.Right - 3, target.Bounds.Top + target.Bounds.Height / 2);
        nint position = (nint)(point.Y << 16 | point.X);
        SendMessage(popup.Handle, 0x201, 1, position); SendMessage(popup.Handle, 0x202, 0, position);
        Assert.True(f.Check.Checked); Assert.Equal(new[] { "state", "checked click" }, events);
        Assert.Same(popup, f.Ribbon.Popups.Top); Assert.False(popup.IsDisposed); Assert.Same(f.Check, popup.FocusedElement);
        foreach (var key in new[] { Keys.Space, Keys.Enter })
        {
            popup.FocusElement(f.Check); var message = Message.Create(popup.Handle, 0x100, (nint)key, 0);
            Assert.True(popup.PreProcessMessage(ref message)); Assert.Same(popup, f.Ribbon.Popups.Top); Assert.False(popup.IsDisposed);
        }
        Assert.True(f.Check.Checked); Assert.Equal(new[] { "state", "checked click", "state", "unchecked click", "state", "checked click" }, events);
        Assert.Equal(handle, editor.Handle); Assert.Equal("Retained text", editor.Text); Assert.Same(f.Field, f.Menu.Items[1]);
        if (presentation == "nested") Assert.True(f.Root.IsDropDownOpen);
        var escape = Message.Create(popup.Handle, 0x100, (nint)Keys.Escape, 0); Assert.True(popup.PreProcessMessage(ref escape));
        Assert.True(popup.IsDisposed);
        Assert.Equal(presentation == "nested", f.Root.IsDropDownOpen);
    });

    [Theory] [InlineData("ordinary")] [InlineData("nested")] [InlineData("qat-menu")]
    public void Ordinary_commands_radio_choices_and_explicit_checkbox_callback_closure_keep_their_dismissal(string presentation) => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(presentation, RibbonDensity.Compact);
        var action = new RibbonButtonItem { Text = "Run command" }; var radio = new RibbonRadioButtonItem { Text = "Choose format" }; f.Menu.Items.AddRange(action, radio);
        var popup = f.Open(); f.Activate(popup, action); Assert.False(f.Ribbon.Popups.HasOpen);
        popup = f.Open(); f.Activate(popup, radio); Assert.True(radio.Checked); Assert.False(f.Ribbon.Popups.HasOpen);
        popup = f.Open(); f.Check.Click += (_, _) => f.Root.CloseDropDown(); f.Activate(popup, f.Check);
        Assert.True(f.Check.Checked); Assert.False(f.Ribbon.Popups.HasOpen); Assert.True(popup.IsDisposed);
    });

    private sealed class Fixture : IDisposable
    {
        internal readonly Form Form = new() { Width = 1000, Height = 700 };
        internal readonly RibbonControl Ribbon = new() { AnimationLevel = RibbonAnimationLevel.None };
        internal readonly RibbonDropDownItem Root = new() { Text = "Settings" };
        internal readonly RibbonDropDownItem Menu;
        internal readonly RibbonCheckBoxItem Check = new() { Text = "Keep formatting" };
        internal readonly RibbonTextBoxItem Field = new() { Text = "Name", Value = "Retained" };
        private readonly string _presentation;
        private readonly RibbonQuickAccessDropDownReference? _reference;
        internal Fixture(string presentation, RibbonDensity density)
        {
            _presentation = presentation; Ribbon.Density = density;
            var tab = new RibbonTab { Text = "Home" }; var group = new RibbonGroup { Text = "Commands" }; group.Items.Add(Root); tab.Groups.Add(group); Ribbon.Tabs.Add(tab);
            Menu = presentation == "nested" ? new RibbonDropDownItem { Text = "Formatting" } : Root;
            if (!ReferenceEquals(Menu, Root)) Root.Items.Add(Menu);
            Menu.Items.AddRange(Check, Field);
            if (presentation == "qat-menu") { _reference = new() { Source = Root }; Ribbon.QuickAccessItems.Add(_reference); }
            Form.Controls.Add(Ribbon); Form.Show(); Application.DoEvents();
        }
        internal RibbonPopupSurface Open()
        {
            Ribbon.Popups.CloseAll(false);
            if (_reference is { } reference) Ribbon.Activate(Ribbon.Snapshot.Targets.Single(t => t.Element == reference)); else Root.ShowDropDown();
            if (!ReferenceEquals(Menu, Root)) Menu.ShowDropDown();
            return Ribbon.Popups.Top!;
        }
        internal void Activate(RibbonPopupSurface surface, RibbonItem item) => Ribbon.Activate(surface.Targets.Single(t => t.Element == item), surface);
        public void Dispose() => Form.Dispose();
    }
    [DllImport("user32.dll")] private static extern nint SendMessage(nint window, uint message, nint wParam, nint lParam);
}
