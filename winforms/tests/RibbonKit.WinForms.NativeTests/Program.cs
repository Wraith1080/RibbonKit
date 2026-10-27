using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Automation;
using RibbonKit.WinForms;
using WF = System.Windows.Forms;

namespace RibbonKit.WinForms.NativeTests;

internal static partial class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Length == 2 && args[0] is "--minimize-checks" or "--minimize-checks-flat")
            return IndependentConsumer.MinimizeInteractionChecks.Run(args[1], args[0] == "--minimize-checks-flat");
        if (args.Length == 2 && args[0] is "--menu-checks" or "--menu-checks-flat")
            return IndependentConsumer.MenuInteractionChecks.Run(args[1], args[0] == "--menu-checks-flat");
        if (args.Length == 2 && args[0] is "--screentip-checks" or "--screentip-checks-flat")
            return IndependentConsumer.ScreenTipInteractionChecks.Run(args[1], args[0] == "--screentip-checks-flat");
        if (args.Length == 2 && args[0] is "--option-checks" or "--option-checks-flat")
            return IndependentConsumer.OptionInteractionChecks.Run(args[1], args[0] == "--option-checks-flat");
        if (args.Length == 2 && args[0] is "--combo-checks" or "--combo-checks-flat")
            return IndependentConsumer.ComboBoxInteractionChecks.Run(args[1], args[0] == "--combo-checks-flat");
        if (args.Length == 2 && args[0] == "--editor-checks")
            return IndependentConsumer.TextBoxInteractionChecks.Run(args[1]);
        if (args.Length == 2 && args[0] is "--split-checks" or "--split-checks-flat")
            return IndependentConsumer.SplitInteractionChecks.Run(args[1], args[0] == "--split-checks-flat");
        if (args.Contains("--server"))
        {
            Server();
            return 0;
        }
        if (args.Length == 2 && args[0] == "--benchmark")
            return Benchmark.Run(args[1]);
        if (args.Length == 2 && args[0] == "--benchmark-popups")
            return Benchmark.Run(args[1], false);
        if (args.Length == 2 && args[0] == "--benchmark-first-popups")
            return Benchmark.Run(args[1], false, 0);
        if (args.Length == 2 && args[0] == "--capture-popups")
            return PopupCapture.Run(args[1]);
        int appArgument = Array.IndexOf(args, "--app");
        string app = appArgument >= 0 && args.Length > appArgument + 1 ? args[appArgument + 1] : Environment.ProcessPath!;
        using var process = Process.Start(new ProcessStartInfo(app, "--server") { UseShellExecute = false })!;
        try
        {
            AutomationElement? window = null;
            Wait(() => (window = AutomationElement.RootElement.FindFirst(TreeScope.Children, new PropertyCondition(AutomationElement.ProcessIdProperty, process.Id))) is not null);
            var root = window!.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.AutomationIdProperty, "native.ribbon"));
            Check(root is not null, "Native ribbon root is discoverable");
            int focusArgument = Array.IndexOf(args, "--focus-cues");
            if (focusArgument >= 0)
            {
                FocusCues(window, root!, process, args[focusArgument + 1]);
                return 0;
            }
            if (args.Contains("--popup-dismissal"))
            {
                PopupDismissal(window, root!, process);
                return 0;
            }
            if (args.Contains("--keyboard"))
            {
                Keyboard(window, root!, process);
                return 0;
            }
            var copy = Find(root!, "cmd.copy");
            ((InvokePattern)copy.GetCurrentPattern(InvokePattern.Pattern)).Invoke();
            Wait(() => window.Current.Name.Contains("Copy=1"));
            var bold = Find(root!, "cmd.bold");
            var toggle = (TogglePattern)bold.GetCurrentPattern(TogglePattern.Pattern);
            Check(toggle.Current.ToggleState == ToggleState.Off, "Initial toggle state");
            using var stateChanged = new ManualResetEventSlim();
            AutomationPropertyChangedEventHandler changed = (_, e) => { if (e.Property == TogglePattern.ToggleStateProperty) stateChanged.Set(); };
            Automation.AddAutomationPropertyChangedEventHandler(bold, TreeScope.Element, changed, TogglePattern.ToggleStateProperty);
            toggle.Toggle();
            Wait(() => window.Current.Name.Contains("Bold=True"));
            Check(toggle.Current.ToggleState == ToggleState.On, "Toggle action updates state");
            Check(stateChanged.Wait(8000), "Toggle property change reaches an external UIA subscriber");
            Automation.RemoveAutomationPropertyChangedEventHandler(bold, changed);
            var disabled = Find(root!, "cmd.disabled");
            Check(!disabled.Current.IsEnabled, "Disabled state");
            bool rejected = false;
            try
            {
                ((InvokePattern)disabled.GetCurrentPattern(InvokePattern.Pattern)).Invoke();
            }
            catch (ElementNotEnabledException) { rejected = true; }
            Check(rejected, "Disabled Invoke is rejected");
            var tab = Find(root!, "tab.insert");
            ((SelectionItemPattern)tab.GetCurrentPattern(SelectionItemPattern.Pattern)).Select();
            Wait(() => Find(root!, "cmd.link", false) is not null);
            Check(Find(root!, "cmd.copy", false) is null, "Inactive commands are not in the active tree");
            ((SelectionItemPattern)Find(root!, "tab.home").GetCurrentPattern(SelectionItemPattern.Pattern)).Select();
            var menu = Find(root!, "cmd.menu");
            ((ExpandCollapsePattern)menu.GetCurrentPattern(ExpandCollapsePattern.Pattern)).Expand();
            AutomationElement? choice = null;
            Wait(() => (choice = AutomationElement.RootElement.FindFirst(TreeScope.Descendants, new AndCondition(new PropertyCondition(AutomationElement.ProcessIdProperty, process.Id), new PropertyCondition(AutomationElement.AutomationIdProperty, "cmd.choice")))) is not null);
            ((InvokePattern)choice!.GetCurrentPattern(InvokePattern.Pattern)).Invoke();
            Wait(() => window.Current.Name.Contains("Choice=1"));
            Check(((ExpandCollapsePattern)menu.GetCurrentPattern(ExpandCollapsePattern.Pattern)).Current.ExpandCollapseState == ExpandCollapseState.Collapsed, "Invoking a menu action closes its popup");
            SetForegroundWindow(process.MainWindowHandle);
            Find(root!, "cmd.copy").SetFocus();
            WF.SendKeys.SendWait("{ENTER}");
            Wait(() => window.Current.Name.Contains("Copy=2"));
            Console.WriteLine("PASS: Enter activates exactly once");
            WF.SendKeys.SendWait("{F10}hc");
            Wait(() => window.Current.Name.Contains("Copy=3"));
            Console.WriteLine("PASS: F10 and tab/command KeyTips activate exactly once");
            WF.SendKeys.SendWait("{F6}");
            Wait(() => AutomationElement.FocusedElement.Current.AutomationId == "document");
            WF.SendKeys.SendWait("{F6}");
            Wait(() => AutomationElement.FocusedElement.Current.AutomationId != "document");
            Console.WriteLine("PASS: F6 moves focus between content and ribbon");
            copy = Find(root!, "cmd.copy");
            copy.SetFocus();
            WF.SendKeys.SendWait(" ");
            Wait(() => window.Current.Name.Contains("Copy=4"));
            var rectangle = copy.Current.BoundingRectangle;
            Check(rectangle.Width > 0 && rectangle.Height > 0, "Action bounds are on screen");
            var oldCursor = WF.Cursor.Position;
            try
            {
                SetCursorPos((int)(rectangle.Left + rectangle.Width / 2), (int)(rectangle.Top + rectangle.Height / 2));
                MouseEvent(2, 0, 0, 0, 0);
                MouseEvent(4, 0, 0, 0, 0);
            }
            finally { WF.Cursor.Position = oldCursor; }
            Wait(() => window.Current.Name.Contains("Copy=5"));
            Console.WriteLine("PASS: Space and native mouse press each activate exactly once");
            var transform = (TransformPattern)window.GetCurrentPattern(TransformPattern.Pattern);
            transform.Move(window.Current.BoundingRectangle.Left, 50);
            transform.Resize(260, 600);
            AutomationElement? collapsed = null;
            Wait(() => (collapsed = Find(root!, "group.clipboard", false))?.Current.ControlType == ControlType.Button);
            ((ExpandCollapsePattern)collapsed!.GetCurrentPattern(ExpandCollapsePattern.Pattern)).Expand();
            var popupBold = FindProcessElement(process.Id, "cmd.bold")!;
            var popupToggle = (TogglePattern)popupBold.GetCurrentPattern(TogglePattern.Pattern);
            Check(popupToggle.Current.ToggleState == ToggleState.On, "Toggle state survives tab changes, resizing and collapse");
            popupToggle.Toggle();
            Wait(() => window.Current.Name.Contains("Bold=False"));
            ((ExpandCollapsePattern)collapsed.GetCurrentPattern(ExpandCollapsePattern.Pattern)).Expand();
            AutomationElement? innerMenu = null;
            Wait(() => (innerMenu = FindProcessElement(process.Id, "cmd.menu")) is not null);
            ((ExpandCollapsePattern)innerMenu!.GetCurrentPattern(ExpandCollapsePattern.Pattern)).Expand();
            Wait(() => FindProcessElement(process.Id, "cmd.choice") is not null);
            WF.SendKeys.SendWait("{ESC}");
            Wait(() => FindProcessElement(process.Id, "cmd.choice") is null);
            Check(((ExpandCollapsePattern)collapsed.GetCurrentPattern(ExpandCollapsePattern.Pattern)).Current.ExpandCollapseState == ExpandCollapseState.Expanded, "Nested Escape leaves the group popup open");
            WF.SendKeys.SendWait("{ESC}");
            Wait(() => ((ExpandCollapsePattern)collapsed.GetCurrentPattern(ExpandCollapsePattern.Pattern)).Current.ExpandCollapseState == ExpandCollapseState.Collapsed);
            Check(collapsed.Current.HasKeyboardFocus, "Keyboard dismissal returns focus to the collapsed opener");
            ((ExpandCollapsePattern)collapsed.GetCurrentPattern(ExpandCollapsePattern.Pattern)).Expand();
            var document = Find(window, "document");
            rectangle = document.Current.BoundingRectangle;
            oldCursor = WF.Cursor.Position;
            try
            {
                SetCursorPos((int)rectangle.Left + 20, (int)rectangle.Bottom - 20);
                MouseEvent(2, 0, 0, 0, 0);
                MouseEvent(4, 0, 0, 0, 0);
                Wait(() => ((ExpandCollapsePattern)collapsed.GetCurrentPattern(ExpandCollapsePattern.Pattern)).Current.ExpandCollapseState == ExpandCollapseState.Collapsed);
                Wait(() => AutomationElement.FocusedElement.Current.AutomationId == "document");
            }
            finally { WF.Cursor.Position = oldCursor; }
            Console.WriteLine("PASS: Outside press dismisses popup and retains clicked content focus");
            Console.WriteLine("PASS: external-process UIA discovery, Invoke, Toggle, disabled, selection, popup ExpandCollapse and command execution.");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
        finally { if (!process.HasExited) { process.CloseMainWindow(); if (!process.WaitForExit(3000)) process.Kill(); } }
    }
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(nint window);
    [DllImport("user32.dll")] private static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll", EntryPoint = "mouse_event")] private static extern void MouseEvent(uint flags, uint x, uint y, uint data, nuint extra);
    [DllImport("user32.dll", EntryPoint = "keybd_event")] private static extern void KeyEvent(byte key, byte scan, uint flags, nuint extra);
    private static void Keyboard(AutomationElement window, AutomationElement root, Process process)
    {
        SetForegroundWindow(process.MainWindowHandle);
        var document = Find(window, "document");
        document.SetFocus();
        KeyEvent(0x12, 0, 0, 0);
        KeyEvent(0x12, 0, 2, 0);
        WF.SendKeys.SendWait("hc");
        Wait(() => window.Current.Name.Contains("Copy=1"));
        Console.WriteLine("PASS: Alt press/release opens typeable tab and command KeyTips");
        Find(root, "tab.home").SetFocus();
        WF.SendKeys.SendWait("{RIGHT}");
        Wait(() => Find(root, "cmd.link", false) is not null);
        WF.SendKeys.SendWait("{LEFT}{DOWN}");
        Wait(() => AutomationElement.FocusedElement.Current.AutomationId == "cmd.copy");
        WF.SendKeys.SendWait("{TAB}");
        Wait(() => AutomationElement.FocusedElement.Current.AutomationId == "cmd.bold");
        WF.SendKeys.SendWait("{TAB}");
        Wait(() => AutomationElement.FocusedElement.Current.AutomationId == "cmd.menu");
        WF.SendKeys.SendWait("+{TAB}");
        Wait(() => AutomationElement.FocusedElement.Current.AutomationId == "cmd.bold");
        WF.SendKeys.SendWait("{HOME}");
        Wait(() => AutomationElement.FocusedElement.Current.AutomationId == "cmd.copy");
        WF.SendKeys.SendWait("{END}");
        Wait(() => AutomationElement.FocusedElement.Current.AutomationId == "cmd.menu");
        Console.WriteLine("PASS: Arrows, Home/End and Tab/Shift+Tab navigate and skip disabled commands");
        document.SetFocus();
        WF.SendKeys.SendWait("{F10}{F10}");
        Wait(() => AutomationElement.FocusedElement.Current.AutomationId == "document");
        KeyEvent(0x12, 0, 0, 0);
        KeyEvent(0x12, 0, 2, 0);
        WF.SendKeys.SendWait("{ESC}");
        Wait(() => AutomationElement.FocusedElement.Current.AutomationId == "document");
        Console.WriteLine("PASS: F10 toggle and Escape restore prior application content focus");
    }
    private static AutomationElement? FindProcessElement(int process, string id) => AutomationElement.RootElement.FindFirst(TreeScope.Descendants, new AndCondition(new PropertyCondition(AutomationElement.ProcessIdProperty, process), new PropertyCondition(AutomationElement.AutomationIdProperty, id)));
    private static AutomationElement Find(AutomationElement root, string id, bool required = true)
    {
        var element = root.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.AutomationIdProperty, id));
        if (required && element is null)
            throw new InvalidOperationException("Missing UIA element: " + id);
        return element!;
    }
    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
        Console.WriteLine("PASS: " + message);
    }
    private static void Wait(Func<bool> condition)
    {
        var timer = Stopwatch.StartNew();
        while (timer.Elapsed < TimeSpan.FromSeconds(8))
        {
            if (condition())
                return;
            Thread.Sleep(50);
        }
        throw new TimeoutException("Native UIA state did not arrive.");
    }
    private static void Server()
    {
        WF.Application.SetHighDpiMode(WF.HighDpiMode.PerMonitorV2);
        WF.Application.EnableVisualStyles();
        var form = new WF.Form { Text = "Native UIA fixture", Width = 1000, Height = 350, StartPosition = WF.FormStartPosition.CenterScreen };
        var ribbon = new RibbonControl { Name = "native.ribbon" };
        int copies = 0, choices = 0;
        bool checkedState = false;
        void UpdateTitle() => form.Text = $"Copy={copies}; Bold={checkedState}; Choice={choices}";
        var home = new RibbonTab { Id = "tab.home", Text = "Home", KeyTip = "H" };
        var group = new RibbonGroup { Id = "group.clipboard", Text = "Clipboard" };
        var copy = new RibbonButtonItem { Id = "cmd.copy", Text = "Copy" };
        copy.Click += (_, _) => { copies++; UpdateTitle(); };
        var bold = new RibbonToggleItem { Id = "cmd.bold", Text = "Bold" };
        bold.CheckedChanged += (_, _) => { checkedState = bold.Checked; UpdateTitle(); };
        var disabled = new RibbonButtonItem { Id = "cmd.disabled", Text = "Disabled", Enabled = false };
        var menu = new RibbonDropDownItem { Id = "cmd.menu", Text = "Menu" };
        var choice = new RibbonButtonItem { Id = "cmd.choice", Text = "Choice" };
        choice.Click += (_, _) => { choices++; UpdateTitle(); };
        menu.Items.Add(choice);
        group.Items.AddRange(copy, bold, disabled, menu);
        home.Groups.Add(group);
        var insert = new RibbonTab { Id = "tab.insert", Text = "Insert", KeyTip = "N" };
        var links = new RibbonGroup { Id = "group.links", Text = "Links" };
        links.Items.Add(new RibbonButtonItem { Id = "cmd.link", Text = "Link" });
        insert.Groups.Add(links);
        ribbon.Tabs.AddRange(home, insert);
        form.Controls.Add(new WF.TextBox { Name = "document", Dock = WF.DockStyle.Fill, Multiline = true, Text = "Document focus target" });
        form.Controls.Add(ribbon);
        WF.Form? otherWindow = null;
        var openOther = new WF.Button { Name = "other.window", Text = "Open another window", Dock = WF.DockStyle.Bottom, Height = 28, TabStop = false };
        openOther.Click += (_, _) =>
        {
            if (otherWindow is null || otherWindow.IsDisposed)
                otherWindow = new WF.Form { Name = "native.other", Text = "Another window in the ribbon process", Width = 400, Height = 250 };
            otherWindow.Show();
            otherWindow.Activate();
        };
        form.Controls.Add(openOther);
        form.FormClosed += (_, _) => otherWindow?.Dispose();
        UpdateTitle();
        WF.Application.Run(form);
    }
}
