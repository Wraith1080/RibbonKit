using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using RibbonKit.WinForms;
using WF = System.Windows.Forms;

namespace IndependentConsumer;

// Uses only public models, renderer hooks, native Controls and native input.
internal static class TextBoxInteractionChecks
{
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(nint handle);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern nint SendMessage(nint handle, uint message, nint wParam, nint lParam);
    internal static int Run(string directory)
    {
        WF.Application.SetHighDpiMode(WF.HighDpiMode.PerMonitorV2);
        WF.Application.EnableVisualStyles();
        Directory.CreateDirectory(directory);
        using var form = new WF.Form { Text = "Public native editor checks", Width = 900, Height = 460, StartPosition = WF.FormStartPosition.CenterScreen };
        using var renderer = new Recorder();
        using var source = new WF.BindingSource();
        var state = new State { Value = "Initial name" }; source.DataSource = state;
        var ribbon = new RibbonControl { Renderer = renderer };
        var tab = new RibbonTab { Text = "Home", KeyTip = "H" };
        var group = new RibbonGroup { Text = "Native editors", KeyTip = "G" };
        var name = new RibbonTextBoxItem { Id = "input.name", Text = "Name", KeyTip = "N", InputWidth = 180 };
        var readOnly = new RibbonTextBoxItem { Id = "input.readOnly", Text = "Read-only", ReadOnly = true, Value = "Select and copy", InputWidth = 180, KeyTip = "R" };
        var disabled = new RibbonTextBoxItem { Id = "input.disabled", Text = "Unavailable", Enabled = false, Value = "Disabled", InputWidth = 180 };
        name.DataBindings.Add(new WF.Binding(nameof(name.Value), source, nameof(state.Value), true, WF.DataSourceUpdateMode.OnPropertyChanged));
        int commits = 0; name.ValueCommitted += (_, _) => commits++;
        name.ValueValidating += (_, e) => e.Cancel = name.Value.Contains('!');
        var menu = new RibbonDropDownItem { Text = "Menu", KeyTip = "M" };
        var popupField = new RibbonTextBoxItem { Id = "input.popup", Text = "Popup value", Value = "Original popup", InputWidth = 160 };
        var nested = new RibbonDropDownItem { Text = "Nested" };
        var nestedField = new RibbonTextBoxItem { Id = "input.nested", Text = "Nested value", Value = "Original nested" };
        nested.Items.Add(nestedField); menu.Items.AddRange(popupField, nested);
        group.Items.AddRange(name, readOnly, disabled, menu); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        var document = new WF.TextBox { Dock = WF.DockStyle.Fill, Multiline = true, Text = "Independent native TextBox consumer" };
        form.Controls.Add(document); form.Controls.Add(ribbon);
        var results = new List<string>(); Exception? error = null;
        form.Shown += async (_, _) =>
        {
            try
            {
                // Complete Shown before starting focus-sensitive interaction.
                await Task.Yield();
                form.Activate(); SetForegroundWindow(form.Handle);
                for (int attempt = 0; attempt < 20 && GetForegroundWindow() != form.Handle; attempt++) await Task.Delay(50);
                Check(GetForegroundWindow() == form.Handle, "Validation Form owns foreground focus before native input");
                name.PerformClick();
                var editor = Editor(ribbon, name.Id);
                Check(editor.Focused, $"Public item activation focuses the real native TextBox (visible={editor.Visible}, enabled={editor.Enabled}, canFocus={editor.CanFocus})");
                Capture(ribbon, "native-fields");
                await Key("^aEdited name");
                Check(name.Value == "Edited name" && state.Value == name.Value, $"Native typing updates model and BindingSource through the shared value path (model={name.Value}, source={state.Value})");
                await Key("{HOME}{RIGHT}+{RIGHT}");
                Check(editor.Focused && editor.SelectionStart == 1 && editor.SelectionLength == 1, "Caret and selection keys stay inside the native editor");
                await Key("{ENTER}"); Check(commits == 1, "Enter validates and commits once");
                await Key("^aDraft{ESC}");
                Check(name.Value == "Edited name" && state.Value == name.Value && ribbon.Focused, "Escape rolls back the live binding and returns to the ribbon");
                name.PerformClick(); await Key("^aInvalid!{TAB}");
                Check(editor.Focused && commits == 1, "Validation cancellation blocks Tab without accepting the value");
                await Key("{F6}"); Check(editor.Focused, "Validation cancellation also blocks F6 navigation");
                Click(document, document.ClientRectangle); await Task.Delay(50);
                Check(editor.Focused, "Ordinary Form validation prevents an invalid mouse focus transfer");
                await Key("{ESC}"); name.PerformClick(); await Key("{TAB}");
                var readOnlyEditor = Editor(ribbon, readOnly.Id);
                Check(readOnlyEditor.Focused, "Tab enters the neighbouring editor");
                await Key("^aReplacement");
                Check(readOnly.Value == "Select and copy" && readOnlyEditor.SelectionLength > 0, "Read-only input preserves native selection while rejecting typing");
                await Key("{F6}"); Check(document.Focused, "F6 leaves a native ribbon editor for the document");
                await Key("{F10}hn"); Check(editor.Focused, "KeyTips focus a native editor without injecting text");
                state.Value = "External update"; await Task.Delay(50);
                Check(editor.Text == "External update", "External BindingSource changes reach the active native editor");
                name.MaxLength = 6; await Key("^a123456789");
                Check(name.Value == "123456", "Native MaxLength limits user input");
                await Key("{ENTER}"); name.MaxLength = 32767;
                Check(!Editor(ribbon, disabled.Id).Enabled, "Disabled item disables its real editor");

                menu.ShowDropDown(); await Task.Delay(80);
                var popup = Popup(); var popupEditor = Editor(popup, popupField.Id);
                Check(popupEditor.Focused, "A menu can own a live native editor");
                Capture(popup, "menu-editor"); await Key("^aPopup draft{TAB}{ENTER}");
                var inner = Popup(); var innerEditor = Editor(inner, nestedField.Id);
                Check(innerEditor.Focused && popupField.Value == "Popup draft", "Tab commits the parent editor and opens a nested editor popup");
                await Key("^aNested draft{ESC}");
                Check(nestedField.Value == "Original nested" && !innerEditor.IsDisposed && popup.Visible, "Escape cancels only the innermost editor and keeps its parent popup");
                await Key("{ESC}"); Check(ribbon.Focused && !popupEditor.IsDisposed, "Closing the outer menu parks its editor without disposing it");

                menu.ShowDropDown(); await Task.Delay(50); popup = Popup();
                await Key("^aCommitted on deactivate");
                using (var other = new WF.Form { Text = "Other application Form", Width = 300, Height = 200 })
                {
                    var otherEditor = new WF.TextBox { Dock = WF.DockStyle.Fill }; other.Controls.Add(otherEditor);
                    other.Show(); other.Activate(); otherEditor.Focus(); SetForegroundWindow(other.Handle);
                    await Task.Delay(100);
                    Check(!popup.Visible && otherEditor.Focused && GetForegroundWindow() == other.Handle,
                        "Deactivation closes a native-editor popup without stealing another Form's focus");
                    Check(popupField.Value == "Committed on deactivate" && !popupEditor.IsDisposed,
                        "Deactivation commits valid text and retains the parked editor");
                    other.Close();
                }
                form.Activate(); SetForegroundWindow(form.Handle); await Task.Delay(50);

                name.PerformClick(); editor.Select(2, 3); form.Width = 160; await Task.Delay(80); ribbon.Refresh();
                Check(renderer.Collapsed && !editor.IsDisposed && !editor.Visible, "Collapse parks the existing editor");
                Click(ribbon, renderer.GroupBounds); await Task.Delay(80);
                var groupPopup = Popup();
                Check(ReferenceEquals(editor, Editor(groupPopup, name.Id)) && editor.SelectionStart == 2 && editor.SelectionLength == 3,
                    "Collapsed-group transfer preserves the same TextBox and its selection");
                Capture(groupPopup, "collapsed-editor");
                await Key("^aCancel{ESC}");
                Check(name.Value == "123456" && !editor.IsDisposed, "Escape from a collapsed editor cancels and returns its presentation safely");
                form.Width = 900; await Task.Delay(80); ribbon.Refresh();
                Check(ReferenceEquals(editor, Editor(ribbon, name.Id)) && editor.Visible, "Expansion restores the same native TextBox to the strip");
                name.PerformClick(); await Key("^aReject!"); form.Width = 160; await Task.Delay(80);
                Check(name.Value == "123456", "Forced layout hiding rolls back a value rejected by validation");
                form.Width = 900; await Task.Delay(80); name.PerformClick();
                await Key("^aReject!"); name.Enabled = false; await Task.Delay(50);
                Check(name.Value == "123456" && !editor.Enabled, "Disabling a dirty editor resolves rejected edits before disabling native input");
                name.Enabled = true; name.PerformClick();
                readOnly.Enabled = false;
                Capture(ribbon, "disabled-field");
                group.Items.Remove(name);
                Check(editor.IsDisposed && name.Value == "123456", "Removal disposes the presentation without disposing the detached model");
                group.Items.Add(name); ribbon.Refresh(); name.PerformClick();
                Check(!Editor(ribbon, name.Id).IsDisposed, "Reinserting a detached model creates a valid new presentation");
                Check(renderer.EditorCalls > 0, "Native editor chrome uses the public shared renderer hook");
                name.Dispose();
            }
            catch (Exception failure) { error = failure; results.Add(failure.ToString()); }
            finally { form.Close(); }
        };
        WF.Application.Run(form);
        File.WriteAllLines(Path.Combine(directory, "result.log"), results);
        foreach (var result in results) Console.WriteLine(result);
        return error is null ? 0 : 1;

        void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); results.Add("PASS " + message); }
        async Task Key(string keys)
        {
            nint foreground = GetForegroundWindow();
            if (foreground != form.Handle && WF.Control.FromHandle(foreground) is not WF.ToolStripDropDown)
                throw new InvalidOperationException("Validation Form lost foreground focus before native input");
            WF.SendKeys.SendWait(keys); await Task.Delay(70);
        }
        void Capture(WF.Control control, string name)
        {
            control.Refresh(); using var bitmap = new Bitmap(control.Width, control.Height);
            control.DrawToBitmap(bitmap, control.ClientRectangle);
            bitmap.Save(Path.Combine(directory, $"{name}-{control.DeviceDpi}dpi.png"));
        }
    }
    private static IEnumerable<WF.Control> Tree(WF.Control root) => root.Controls.Cast<WF.Control>().SelectMany(c => new[] { c }.Concat(Tree(c)));
    private static WF.TextBox Editor(WF.Control root, string id) => Tree(root).OfType<WF.TextBox>().Single(c => c.Name == id);
    private static WF.Control Popup() => WF.Control.FromHandle(GetForegroundWindow()) is WF.ToolStripDropDown window
        ? window.Items.OfType<WF.ToolStripControlHost>().Single().Control : throw new InvalidOperationException("Expected an active library popup");
    private static void Click(WF.Control control, Rectangle bounds)
    {
        int x = bounds.Left + bounds.Width / 2, y = bounds.Top + bounds.Height / 2;
        nint point = (nint)((y << 16) | (x & 0xffff));
        SendMessage(control.Handle, 0x201, 1, point); SendMessage(control.Handle, 0x202, 0, point);
    }
    private sealed class Recorder : RibbonOffice2024Renderer
    {
        internal Rectangle GroupBounds;
        internal bool Collapsed;
        internal int EditorCalls;
        public override void DrawGroup(RibbonPaintContext context, RibbonGroup group, bool collapsed)
        { GroupBounds = context.Bounds; Collapsed = collapsed; base.DrawGroup(context, group, collapsed); }
        public override void DrawTextBox(RibbonPaintContext context, RibbonTextBoxItem item, Rectangle editorBounds)
        { EditorCalls++; base.DrawTextBox(context, item, editorBounds); }
    }
    private sealed class State : INotifyPropertyChanged
    {
        private string _value = "";
        public string Value { get => _value; set { if (_value == value) return; _value = value; PropertyChanged?.Invoke(this, new(nameof(Value))); } }
        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
