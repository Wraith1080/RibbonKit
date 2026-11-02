using System.Reflection;
using System.Runtime.InteropServices;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class ChoicePopupFocusTests
{
    [Theory]
    [InlineData(false, false)] [InlineData(true, false)]
    [InlineData(false, true)] [InlineData(true, true)]
    public void Mouse_accept_keeps_owner_active_and_reuses_the_native_editor(bool editable, bool quickAccess) => FoundationTests.Sta(() =>
    {
        using var form = new RibbonForm { Backdrop = RibbonWindowBackdrop.Mica, Width = 900 };
        var ribbon = new RibbonControl(); form.Controls.Add(ribbon); form.Ribbon = ribbon;
        var tab = new RibbonTab(); var group = new RibbonGroup();
        var item = new RibbonComboBoxItem { Id = "font", IsEditable = editable };
        item.Items.AddRange("Alpha", "Beta", "Gamma"); item.SelectedIndex = 0;
        group.Items.Add(item); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        var reference = new RibbonQuickAccessComboBoxReference { Source = item }; ribbon.QuickAccessItems.Add(reference);
        form.Show(); Application.DoEvents();
        var editor = ribbon.Editors.EditorFor(item)!;
        nint handle = editor.Handle;
        if (quickAccess) ribbon.FocusElement(reference); else editor.Focus();
        int deactivated = 0, activated = 0, committed = 0;
        form.Deactivate += (_, _) => deactivated++; form.Activated += (_, _) => activated++;
        item.ValueCommitted += (_, _) => committed++;
        foreach (int index in new[] { 2, 0 })
        {
            if (quickAccess) ribbon.Activate(ribbon.Snapshot.Targets.Single(t => ReferenceEquals(t.Element, reference)));
            else item.ShowDropDown();
            var choice = (RibbonChoicePopup)editor.Parent!.GetType().GetProperty("Choice", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(editor.Parent)!;
            var window = Field<RibbonPopupWindow>(choice, "_window");
            var list = Field<ListBox>(choice, "_list");
            Assert.Equal(form.Handle, GetActiveWindow());
            Assert.Equal((nint)3, SendMessage(list.Handle, 0x21, window.Handle, (nint)(0x201 << 16 | 1)));
            var row = list.GetItemRectangle(index);
            nint point = (nint)((row.Top + row.Height / 2) << 16 | (row.Left + 8));
            SendMessage(list.Handle, 0x201, 1, point); SendMessage(list.Handle, 0x202, 0, point);
            Assert.False(item.IsDropDownOpen); Assert.False(reference.IsDropDownOpen);
            Assert.Equal(index, item.SelectedIndex); Assert.Equal(item.Items[index], editor.Text);
            Assert.Equal(form.Handle, GetActiveWindow()); Assert.Equal(handle, editor.Handle);
            if (!quickAccess) Assert.True(editor.ContainsFocus);
        }
        Assert.Equal(2, committed); Assert.Equal(0, deactivated); Assert.Equal(0, activated);
    });

    [Fact]
    public void Command_popup_keeps_its_normal_activation_policy() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl(); using var window = new RibbonPopupWindow(ribbon, () => { });
        Assert.Equal(0, GetWindowLong(window.Handle, -20) & 0x08000000);
    });

    [Fact]
    public void Choice_rows_and_scrollbar_do_not_request_activation() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl();
        using var item = new RibbonComboBoxItem { Id = "choice" };
        item.Items.AddRange(Enumerable.Range(0, 12).Select(i => "Choice " + i).ToArray());
        item.SelectedIndex = 0;
        int accepted = 0, cancelled = 0;
        nint active = GetActiveWindow(), foreground = GetForegroundWindow();
        using var choice = new RibbonChoicePopup(ribbon, item, new(10, 10, 160, 24),
            index => item.SelectedIndex = index, () => accepted++, () => cancelled++);
        var window = Field<RibbonPopupWindow>(choice, "_window");
        var list = Field<ListBox>(choice, "_list");
        window.Location = new(-30000, -30000);
        Assert.Equal(active, GetActiveWindow());
        Assert.Equal(foreground, GetForegroundWindow());
        // WM_MOUSEACTIVATE precedes both row and native scrollbar presses.
        Assert.Equal((nint)3, SendMessage(list.Handle, 0x21, window.Handle, (nint)(0x201 << 16 | 1)));
        Assert.Equal((nint)3, SendMessage(list.Handle, 0x21, window.Handle, (nint)(0xA1 << 16 | 7)));
        Assert.NotEqual(0, GetWindowLong(window.Handle, -20) & 0x08000000); // WS_EX_NOACTIVATE
        choice.Move(Keys.End);
        Assert.Equal(11, item.SelectedIndex);
        choice.Character('C');
        Assert.Equal(0, accepted); Assert.Equal(0, cancelled);
        Assert.Equal(active, GetActiveWindow());
        Assert.Equal(foreground, GetForegroundWindow());
    });

    private static T Field<T>(object value, string name) => (T)value.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(value)!;
    [DllImport("user32.dll")] private static extern nint GetActiveWindow();
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern nint SendMessage(nint window, uint message, nint wParam, nint lParam);
    [DllImport("user32.dll", EntryPoint = "GetWindowLongW")] private static extern int GetWindowLong(nint window, int index);
}
