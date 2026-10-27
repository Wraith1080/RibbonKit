using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Automation;
using WF = System.Windows.Forms;

namespace RibbonKit.WinForms.NativeTests;

internal static partial class Program
{
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();

    private static void PopupDismissal(AutomationElement window, AutomationElement root, Process process)
    {
        using var receiver = new WF.Form { Text = "Popup dismissal focus receiver", Width = 430, Height = 230, StartPosition = WF.FormStartPosition.Manual, Location = new(80, 80) };
        receiver.Controls.Add(new WF.TextBox { Dock = WF.DockStyle.Fill, Text = "Focus must stay here when another application's ribbon popup closes." });
        void FocusReceiver()
        {
            receiver.Show();
            receiver.Activate();
            SetForegroundWindow(receiver.Handle);
            Wait(() => { WF.Application.DoEvents(); return GetForegroundWindow() == receiver.Handle; });
        }
        // Establish genuine foreground/input ownership before opening any popup.
        // Subsequent activation changes use no outside mouse press, which could
        // otherwise dismiss through the separate outside-click path.
        receiver.Show();
        receiver.BringToFront();
        WF.Application.DoEvents();
        var cursor = WF.Cursor.Position;
        try
        {
            SetCursorPos(receiver.Left + 60, receiver.Top + 12);
            MouseEvent(2, 0, 0, 0, 0);
            MouseEvent(4, 0, 0, 0, 0);
            Wait(() => { WF.Application.DoEvents(); return GetForegroundWindow() == receiver.Handle; });
        }
        finally { WF.Cursor.Position = cursor; }
        receiver.Hide();
        SetForegroundWindow(process.MainWindowHandle);
        var menu = (ExpandCollapsePattern)Find(root, "cmd.menu").GetCurrentPattern(ExpandCollapsePattern.Pattern);
        menu.Expand();
        Wait(() => FindProcessElement(process.Id, "cmd.choice") is not null);
        Check(menu.Current.ExpandCollapseState == ExpandCollapseState.Expanded, "Dropdown stays open while its own surface has focus");
        FocusReceiver();
        Wait(() => { WF.Application.DoEvents(); return menu.Current.ExpandCollapseState == ExpandCollapseState.Collapsed && FindProcessElement(process.Id, "cmd.choice") is null; });
        Check(GetForegroundWindow() == receiver.Handle, "Switching to another process closes dropdown without stealing focus");

        receiver.Hide();
        SetForegroundWindow(process.MainWindowHandle);
        var transform = (TransformPattern)window.GetCurrentPattern(TransformPattern.Pattern);
        transform.Move(window.Current.BoundingRectangle.Left, 50);
        transform.Resize(260, 600);
        AutomationElement? group = null;
        Wait(() => (group = Find(root, "group.clipboard", false))?.Current.ControlType == ControlType.Button);
        var groupPattern = (ExpandCollapsePattern)group!.GetCurrentPattern(ExpandCollapsePattern.Pattern);
        groupPattern.Expand();
        Wait(() => FindProcessElement(process.Id, "cmd.menu") is not null);
        var nested = (ExpandCollapsePattern)FindProcessElement(process.Id, "cmd.menu")!.GetCurrentPattern(ExpandCollapsePattern.Pattern);
        nested.Expand();
        Wait(() => FindProcessElement(process.Id, "cmd.choice") is not null);
        Check(groupPattern.Current.ExpandCollapseState == ExpandCollapseState.Expanded, "Opening a nested dropdown preserves the enclosing group");
        FocusReceiver();
        Wait(() => { WF.Application.DoEvents(); return groupPattern.Current.ExpandCollapseState == ExpandCollapseState.Collapsed && FindProcessElement(process.Id, "cmd.menu") is null && FindProcessElement(process.Id, "cmd.choice") is null; });
        Check(GetForegroundWindow() == receiver.Handle, "Switching away dismisses the complete nested popup stack");

        if (Find(window, "other.window", false) is { } openOther)
        {
            receiver.Hide();
            SetForegroundWindow(process.MainWindowHandle);
            groupPattern.Expand();
            Wait(() => FindProcessElement(process.Id, "cmd.menu") is not null);
            ((InvokePattern)openOther.GetCurrentPattern(InvokePattern.Pattern)).Invoke();
            AutomationElement? other = null;
            Wait(() => (other = AutomationElement.RootElement.FindFirst(TreeScope.Children, new AndCondition(
                new PropertyCondition(AutomationElement.ProcessIdProperty, process.Id),
                new PropertyCondition(AutomationElement.AutomationIdProperty, "native.other")))) is not null);
            nint otherHandle = (nint)other!.Current.NativeWindowHandle;
            Wait(() => GetForegroundWindow() == otherHandle && groupPattern.Current.ExpandCollapseState == ExpandCollapseState.Collapsed);
            Check(GetForegroundWindow() == otherHandle, "Another Form in the same process closes the popup and retains focus");
        }
    }
}
