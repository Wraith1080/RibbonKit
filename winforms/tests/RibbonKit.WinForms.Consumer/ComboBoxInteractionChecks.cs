using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using RibbonKit.WinForms;
using WF = System.Windows.Forms;

namespace IndependentConsumer;

internal static class ComboBoxInteractionChecks
{
    [StructLayout(LayoutKind.Sequential)] private struct NativeRect { internal int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] private struct ComboInfo
    {
        internal int Size; internal NativeRect Item, Button; internal int State; internal nint Combo, Edit, List;
    }
    [DllImport("user32.dll")] private static extern bool GetComboBoxInfo(nint window, ref ComboInfo info);
    [DllImport("user32.dll")] private static extern bool GetWindowRect(nint window, out NativeRect rect);
    [DllImport("user32.dll")] private static extern bool ClientToScreen(nint window, ref Point point);
    [DllImport("user32.dll", EntryPoint = "SendMessageW")] private static extern nint ItemRect(nint window, uint message, nint index, ref NativeRect rect);
    [DllImport("user32.dll")] private static extern nint SendMessage(nint window, uint message, nint wParam, nint lParam);
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(nint window);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern nint GetWindow(nint window, uint command);
    [DllImport("user32.dll", EntryPoint = "GetWindowLongW")] private static extern int GetWindowLong(nint window, int index);
    [DllImport("user32.dll")] private static extern bool IsWindow(nint window);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(nint window);
    [DllImport("user32.dll")] private static extern void mouse_event(uint flags, uint x, uint y, uint data, nuint extra);

    internal static int Run(string directory, bool flat = false)
    {
        WF.Application.SetHighDpiMode(WF.HighDpiMode.PerMonitorV2); WF.Application.EnableVisualStyles();
        WF.Application.SetUnhandledExceptionMode(WF.UnhandledExceptionMode.ThrowException);
        Directory.CreateDirectory(directory);
        using var form = new WF.Form { Text = "Public native ComboBox checks", Width = 900, Height = 460, StartPosition = WF.FormStartPosition.CenterScreen };
        var observed = new Observations();
        using RibbonRenderer renderer = flat ? new FlatRecorder(observed) : new Recorder(observed);
        using var source = new WF.BindingSource();
        var state = new State(); source.DataSource = state;
        var ribbon = new RibbonControl { Renderer = renderer };
        var tab = new RibbonTab { Text = "Home", KeyTip = "H" }; var group = new RibbonGroup { Text = "Native choices", KeyTip = "G" };
        var choice = new RibbonComboBoxItem { Id = "choice.list", Text = "Choice", KeyTip = "C", InputWidth = 160 };
        choice.Items.AddRange("Alpha", "Beta", "Gamma");
        var edit = new RibbonComboBoxItem { Id = "choice.edit", Text = "Editable", IsEditable = true, KeyTip = "E", InputWidth = 160 };
        edit.Items.AddRange("100%", "125%", "150%");
        var text = new RibbonTextBoxItem { Id = "choice.neighbour", Text = "Name", Value = "Mixed editor rows", InputWidth = 160 };
        choice.DataBindings.Add(new WF.Binding(nameof(choice.Value), source, nameof(state.Choice), true, WF.DataSourceUpdateMode.OnPropertyChanged));
        var editBinding = new WF.Binding(nameof(edit.Value), source, nameof(state.Edit), true, WF.DataSourceUpdateMode.OnValidation);
        edit.DataBindings.Add(editBinding);
        edit.ValueValidating += (_, e) => e.Cancel = edit.Value == "bad";
        int commits = 0; choice.ValueCommitted += (_, _) => commits++;
        int editCommits = 0; edit.ValueCommitted += (_, _) => editCommits++;
        var menu = new RibbonDropDownItem { Text = "Popup", KeyTip = "M" };
        var popupChoice = new RibbonComboBoxItem { Id = "choice.popup", Text = "Popup choice", InputWidth = 160 };
        popupChoice.Items.AddRange("Alpha", "Beta", "Gamma"); popupChoice.SelectedIndex = 0; menu.Items.Add(popupChoice);
        group.Items.AddRange(choice, edit, text, menu); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        var document = new WF.TextBox { Multiline = true, Dock = WF.DockStyle.Fill, Text = "Independent ComboBox consumer" };
        form.Controls.Add(document); form.Controls.Add(ribbon);
        var cursor = WF.Cursor.Position; var results = new List<string>(); Exception? error = null; bool completed = false;
        form.Shown += async (_, _) =>
        {
            try
            {
                await Task.Yield(); form.Activate(); SetForegroundWindow(form.Handle);
                for (int i = 0; i < 20 && GetForegroundWindow() != form.Handle; i++) await Task.Delay(50);
                Check(GetForegroundWindow() == form.Handle, "Validation Form owns foreground focus");
                document.Focus(); Capture(ribbon, "idle");
                choice.PerformClick(); var combo = Find(ribbon, choice.Id); var editable = Find(ribbon, edit.Id);
                Check(combo.ContainsFocus && combo.DropDownStyle == WF.ComboBoxStyle.DropDownList, "Selection-only item focuses a real native ComboBox");
                Capture(ribbon, "mixed-editors");
                Check(combo.Left == editable.Left && combo.Width == editable.Width, "Mixed editor rows share native field edges");
                Check(combo.GetItemHeight(0) >= 28 * ribbon.DeviceDpi / 96, "Choice list rows have at least 28 logical pixels of height");
                document.Focus(); await Task.Delay(60);
                var editorHandles = Tree(ribbon).Where(c => c is WF.TextBox or WF.ComboBox).Select(c => c.Handle).ToHashSet();
                var editInfo = new ComboInfo { Size = Marshal.SizeOf<ComboInfo>() };
                GetComboBoxInfo(editable.Handle, ref editInfo);
                if (editInfo.Edit != 0) editorHandles.Add(editInfo.Edit);
                var watches = editorHandles.Select(h => new PaintWatch(h)).ToArray();
                try
                {
                    var menuBounds = observed.MenuBounds;
                    WF.Cursor.Position = ribbon.PointToScreen(new(menuBounds.Left + menuBounds.Width / 2, menuBounds.Top + menuBounds.Height / 2));
                    await Task.Delay(60);
                    mouse_event(2, 0, 0, 0, 0); await Task.Delay(60);
                    WF.Cursor.Position = ribbon.PointToScreen(new(ribbon.Width - 3, 3));
                    mouse_event(4, 0, 0, 0, 0); await Task.Delay(60);
                    WF.Cursor.Position = ribbon.PointToScreen(new(menuBounds.Left + menuBounds.Width / 2, menuBounds.Top + menuBounds.Height / 2));
                    mouse_event(2, 0, 0, 0, 0); mouse_event(4, 0, 0, 0, 0); await Task.Delay(60);
                    Check(menu.IsDropDownOpen, "Pointer activation opens the neighbouring command popup");
                    menu.CloseDropDown(); await Task.Delay(60);
                    Check(watches.All(w => w.Paints == 0 && w.Positions == 0 && w.Selections == 0),
                        $"Hover/press/click away from editors preserves their HWND paint, bounds and text selection (paints={watches.Sum(w => w.Paints)}, positions={watches.Sum(w => w.Positions)}, selections={watches.Sum(w => w.Selections)})");
                }
                finally { foreach (var watch in watches) watch.Dispose(); }
                choice.PerformClick();
                await Key("{DOWN}");
                Check(choice.Value == "Beta" && state.Choice == "Beta" && commits == 1, "Closed-list keyboard selection updates binding and commits once");
                await Key("{F4}{DOWN}{ESC}");
                Check(!choice.IsDropDownOpen && choice.Value == "Beta" && state.Choice == "Beta" && commits == 1,
                    "Escape cancels choice-list preview and restores the live binding");
                await Key("{F4}g");
                Check(choice.Value == "Gamma" && commits == 1 && combo.ContainsFocus, "Native ListBox incremental search previews without committing or moving editor focus");
                await Key("{ESC}");
                await Key("%{DOWN}{HOME}{ENTER}");
                Check(choice.Value == "Alpha" && commits == 2 && !choice.IsDropDownOpen, "Alt+Down and Enter accept a choice list selection once");
                await ClickArrow(combo); CaptureList(combo, "choice-list"); CheckChoiceChrome(combo);
                var hover = List(combo).GetItemRectangle(2);
                WF.Cursor.Position = List(combo).PointToScreen(new(hover.Left + 20, hover.Top + hover.Height / 2)); await Task.Delay(80);
                Check(choice.Value == "Gamma" && commits == 2 && combo.ContainsFocus, "Pointer hover previews a row while preserving native editor focus");
                choice.CloseDropDown(); Check(choice.Value == "Alpha" && state.Choice == "Alpha" && commits == 2, "Closing an unaccepted popup rolls back pointer preview");
                await ClickArrow(combo); await ClickChoice(combo, 1);
                Check(choice.Value == "Beta" && state.Choice == "Beta" && commits == 3,
                    $"Choice list mouse selection commits the selected value once (value={choice.Value}, native={combo.Text}, commits={commits})");
                document.Focus(); await Task.Delay(50);
                await ClickArrow(combo); await ClickChoice(combo, 2);
                Check(choice.Value == "Gamma" && state.Choice == "Gamma" && commits == 4,
                    $"Pointer selection from document focus persists (value={choice.Value}, native={combo.Text}, commits={commits})");
                document.Focus(); await Task.Delay(50);
                Check(choice.Value == "Gamma", "Leaving the chooser retains the pointer selection");
                choice.Value = "Beta";
                choice.ShowDropDown(); await Key("{DOWN}");
                WF.Cursor.Position = document.PointToScreen(new(20, document.Height - 20));
                mouse_event(2, 0, 0, 0, 0); mouse_event(4, 0, 0, 0, 0); await Task.Delay(80);
                Check(!choice.IsDropDownOpen && choice.Value == "Beta" && document.ContainsFocus && commits == 4,
                    "Outside pointer press cancels preview and reaches the document without an extra commit");
                document.Focus(); editBinding.DataSourceUpdateMode = WF.DataSourceUpdateMode.OnPropertyChanged;
                await ClickArrow(editable); await ClickChoice(editable, 1);
                Check(edit.Value == "125%" && state.Edit == "125%" && editCommits == 1, "Pointer selection from document focus commits the live-bound editable chooser");
                editBinding.DataSourceUpdateMode = WF.DataSourceUpdateMode.OnValidation;
                edit.Value = state.Edit = "100%"; editCommits = 0;

                edit.PerformClick(); await Key("^abad{ENTER}{TAB}");
                Check(editable.ContainsFocus && edit.Value == "bad" && state.Edit == "100%", "Editable validation rejects Enter and Tab before writing deferred binding");
                await Key("{ESC}"); edit.PerformClick(); await Key("^a111{%}{F4}{DOWN}{ESC}");
                Check(edit.Value == "111%" && !edit.IsDropDownOpen, "First Escape restores the choice list's opening text");
                await Key("{ESC}"); Check(edit.Value == "100%" && state.Edit == "100%", $"Next Escape restores the editor's committed value (value={edit.Value}, source={state.Edit}, commits={editCommits})");
                edit.PerformClick(); await Key("^a133{%}{ENTER}{F6}");
                Check(state.Edit == "133%" && document.Focused, "Typed free text commits through binding and F6 returns to the document");
                edit.PerformClick(); await Key("{F4}^a144{%}");
                Check(edit.IsDropDownOpen && editable.ContainsFocus && edit.Value == "144%" && state.Edit == "133%",
                    $"Typing with the choice popup open still uses the native editor and defers its binding (open={edit.IsDropDownOpen}, focus={editable.ContainsFocus}, value={edit.Value}, native={editable.Text}, bound={state.Edit})");
                await Key("{ESC}"); Check(edit.Value == "133%" && !edit.IsDropDownOpen, "Escape restores the text captured when the choice popup opened");
                await Key("{F10}hc"); Check(combo.ContainsFocus, "KeyTip focuses the native chooser");
                choice.Enabled = false; choice.ShowDropDown();
                Check(!combo.Enabled && !choice.IsDropDownOpen, "Disabled chooser rejects opening and native input"); choice.Enabled = true;

                menu.ShowDropDown(); await Task.Delay(60); var popup = Popup(); var nested = Find(popup, popupChoice.Id);
                var popupWindow = (WF.ToolStripDropDown)popup.Parent!;
                nint shadowWindow = WF.SystemInformation.IsDropShadowEnabled ? GetWindow(popupWindow.Handle, 2) : 0;
                Check(shadowWindow == 0 || (GetWindowLong(shadowWindow, -20) & 0x08080020) == 0x08080020,
                    "Soft shadow is layered, cannot activate and passes pointer input through");
                Check(popup.Location == new Point(1, 1) && popupWindow.ClientSize == new Size(popup.Width + 2, popup.Height + 2),
                    "Popup reserves an outer frame without covering content");
                int radius = renderer.GetPopupCornerRadius(ribbon.Theme, ribbon.DeviceDpi);
                Check(flat ? radius == 0 && popupWindow.Region is null && popup.Region is null
                    : radius > 0 && popupWindow.Region is not null && popup.Region is not null && !popupWindow.Region.IsVisible(0, 0),
                    flat ? "Neutral popup retains square corners" : "Office popup and hosted content exclude the rounded outer corners");
                if (!flat)
                {
                    bool symmetric = true;
                    for (int y = 0; y <= radius; y++)
                        for (int x = 0; x <= radius; x++)
                        {
                            bool inside = popupWindow.Region!.IsVisible(x, y);
                            symmetric &= inside == popupWindow.Region.IsVisible(popupWindow.Width - 1 - x, y)
                                && inside == popupWindow.Region.IsVisible(x, popupWindow.Height - 1 - y)
                                && inside == popupWindow.Region.IsVisible(popupWindow.Width - 1 - x, popupWindow.Height - 1 - y);
                        }
                    Check(symmetric, "All four native popup corners share the same clipping pixels");
                }
                Check(!popupWindow.DropShadowEnabled, "Dark native popup shadow is disabled");
                Check(menu.IsDropDownOpen, "Dropdown reports its latched open state");
                SendMessage(ribbon.Handle, 0x2A3, 0, 0);
                using (var strip = new Bitmap(ribbon.Width, ribbon.Height))
                {
                    ribbon.DrawToBitmap(strip, ribbon.ClientRectangle);
                    var target = observed.MenuBounds;
                    Check(strip.GetPixel(target.Left + 6, target.Top + 6).ToArgb() == ribbon.Theme.Pressed.Start.ToArgb(),
                        "Dropdown stays pressed after the pointer leaves while its menu is open");
                }
                int clearance = (int)Math.Round(8 * ribbon.DeviceDpi / 96.0) + 2;
                var frameBounds = popupWindow.Bounds; frameBounds.Inflate(clearance, clearance);
                using (var image = new Bitmap(frameBounds.Width, frameBounds.Height))
                {
                    using (var graphics = Graphics.FromImage(image)) graphics.CopyFromScreen(frameBounds.Location, Point.Empty, image.Size);
                    Check(new[] { image.GetPixel(clearance, clearance + popupWindow.Height / 2),
                        image.GetPixel(clearance + popupWindow.Width - 1, clearance + popupWindow.Height / 2),
                        image.GetPixel(clearance + popupWindow.Width / 2, clearance),
                        image.GetPixel(clearance + popupWindow.Width / 2, clearance + popupWindow.Height - 1) }
                        .All(color => color.ToArgb() == ribbon.Theme.PopupBorder.ToArgb()), "All four popup borders are actually visible on screen");
                    var shadow = image.GetPixel(clearance + popupWindow.Width + 1, clearance + popupWindow.Height / 2);
                    Check(!WF.SystemInformation.IsDropShadowEnabled || shadow.R is >= 231 and < 255,
                        $"Popup shadow is soft on the white document (pixel={shadow.R}; maximum opacity={ribbon.Theme.PopupShadowColor.A})");
                    image.Save(Path.Combine(directory, $"popup-frame-shadow-{ribbon.DeviceDpi}dpi.png"));
                }
                popupChoice.ShowDropDown(); await Task.Delay(50); CaptureList(nested, "popup-choice-list");
                GetWindowRect(List(nested).Handle, out var nativeBounds);
                Check(nativeBounds.Bottom > popup.RectangleToScreen(popup.ClientRectangle).Bottom,
                    "Choice list extends outside the containing ribbon popup");
                await ClickChoice(nested, 2);
                Check(popup.Visible && popupChoice.Value == "Gamma", "Clicking a choice list outside its parent popup preserves that popup");
                popupChoice.ShowDropDown(); await Key("{UP}{ESC}");
                Check(popup.Visible && !popupChoice.IsDropDownOpen && popupChoice.Value == "Gamma", "Escape closes only the choice list inside a ribbon popup");
                await Key("{ESC}"); Check(!popup.Visible && !menu.IsDropDownOpen && !nested.IsDisposed, "Next Escape clears the open state, closes the ribbon popup and parks its ComboBox");
                Check(shadowWindow == 0 || !IsWindow(shadowWindow), "Closing the popup destroys its shadow window");
                int shadowCalls = observed.Shadows;
                ribbon.Theme = ribbon.Theme with { PopupShadowEnabled = false };
                menu.ShowDropDown(); await Task.Delay(50);
                Check(observed.Shadows == shadowCalls && !((WF.ToolStripDropDown)Popup().Parent!).DropShadowEnabled,
                    "Disabling popup shadows suppresses both custom and native shadows");
                menu.CloseDropDown(); ribbon.Theme = ribbon.Theme with { PopupShadowEnabled = true };
                if (!flat)
                {
                    menu.ShowDropDown(); await Task.Delay(50);
                    var roundedWindow = (WF.ToolStripDropDown)Popup().Parent!;
                    WF.Cursor.Position = roundedWindow.Location;
                    await Task.Delay(50); mouse_event(2, 0, 0, 0, 0); await Task.Delay(40);
                    mouse_event(4, 0, 0, 0, 0); await Task.Delay(90);
                    Check(!menu.IsDropDownOpen && GetForegroundWindow() == form.Handle,
                        "Clicking through a removed rounded corner dismisses the popup and activates its host");
                }

                menu.ShowDropDown(); await Task.Delay(50); popup = Popup(); popupChoice.ShowDropDown();
                using (var other = new WF.Form { Text = "Other Form", Width = 300, Height = 180 })
                {
                    var otherEditor = new WF.TextBox { Dock = WF.DockStyle.Fill }; other.Controls.Add(otherEditor);
                    other.Show(); other.Activate(); otherEditor.Focus(); SetForegroundWindow(other.Handle); await Task.Delay(100);
                    Check(!popup.Visible && !popupChoice.IsDropDownOpen && otherEditor.Focused && GetForegroundWindow() == other.Handle,
                        "Deactivation dismisses both choice list and parent popup without stealing focus");
                    other.Close();
                }
                form.Activate(); SetForegroundWindow(form.Handle); await Task.Delay(50);

                edit.PerformClick(); editable.Select(1, 2); form.Width = 160; await Task.Delay(60); ribbon.Refresh();
                Check(observed.Collapsed && !editable.IsDisposed && !editable.Visible, "Collapsed group parks its native chooser");
                Click(ribbon, observed.GroupBounds); await Task.Delay(60); var groupPopup = Popup();
                edit.PerformClick();
                Check(ReferenceEquals(editable, Find(groupPopup, edit.Id)) && editable.SelectionStart == 1 && editable.SelectionLength == 2,
                    "Popup transfer reuses the editable ComboBox and preserves text selection");
                Capture(groupPopup, "collapsed-choices"); edit.ShowDropDown(); await Key("{ESC}");
                Check(groupPopup.Visible && !edit.IsDropDownOpen, "Choice list Escape preserves the collapsed group");
                await Key("{ESC}"); form.Width = 900; await Task.Delay(60); ribbon.Refresh();
                Check(ReferenceEquals(combo, Find(ribbon, choice.Id)) && combo.Visible, "Expansion restores the same native chooser");
                var many = new RibbonComboBoxItem { Id = "choice.many", Text = "Many", InputWidth = 160 };
                many.Items.AddRange(Enumerable.Range(0, 40).Select(i => $"Choice {i:00}").ToArray()); many.SelectedIndex = 0;
                group.Items.Add(many); var manyEditor = Find(ribbon, many.Id);
                bool rejectLast = true; int manyCommits = 0;
                many.ValueValidating += (_, e) => e.Cancel = rejectLast && many.SelectedIndex == 39;
                many.ValueCommitted += (_, _) => manyCommits++;
                many.ShowDropDown(); var rows = List(manyEditor); await Key("{END}{ENTER}");
                Check(many.IsDropDownOpen && many.SelectedIndex == 39 && rows.TopIndex > 0 && manyCommits == 0,
                    "Long-list End scrolls to the preview and rejected Enter keeps the popup open");
                await Key("{ESC}"); many.ShowDropDown(); await Key("{END}"); await ClickChoice(manyEditor, 39);
                Check(!many.IsDropDownOpen && many.SelectedIndex == 0 && manyCommits == 0, "Rejected pointer acceptance restores the opening selection");
                rejectLast = false; many.ShowDropDown(); rows = List(manyEditor);
                SendMessage(rows.Handle, 0x20A, unchecked((nint)(-120 << 16)), 0); await Task.Delay(60);
                Check(rows.TopIndex > 0, "Choice ListBox retains native wheel scrolling");
                await Key("{END}{PGUP}"); Check(many.SelectedIndex is > 0 and < 39, "PageUp moves by the visible native list page");
                await Key("{HOME}{DOWN}{TAB}");
                Check(!many.IsDropDownOpen && many.SelectedIndex == 1 && manyCommits == 1, "Tab accepts a choice once and leaves the popup");
                many.ShowDropDown(); await Key("{DOWN}"); many.Enabled = false;
                Check(!many.IsDropDownOpen && many.SelectedIndex == 1 && manyCommits == 1, "Disabling an open chooser cancels its unaccepted preview");
                many.Enabled = true; many.ShowDropDown(); many.SelectedIndex = 12;
                Check(!many.IsDropDownOpen && many.SelectedIndex == 12, "External selection update closes a stale preview without rolling back the new value");
                var savedBounds = form.Bounds; var work = WF.Screen.FromControl(form).WorkingArea;
                form.Location = new(work.Right - 500, work.Bottom - 100); await Task.Delay(60); many.ShowDropDown(); await Task.Delay(60);
                var fitted = (WF.ToolStripDropDown)List(manyEditor).Parent!;
                Check(work.Contains(fitted.Bounds) && fitted.Bottom <= manyEditor.RectangleToScreen(manyEditor.ClientRectangle).Top,
                    "Near the lower/right screen edges the choice popup opens above and stays inside the work area");
                CaptureList(manyEditor, "choice-screen-edge"); many.CloseDropDown(); form.Bounds = savedBounds; await Task.Delay(60);
                group.Items.Remove(many); many.Dispose();
                choice.PerformClick(); choice.ShowDropDown(); choice.Items.Insert(0, "Start");
                Check(!choice.IsDropDownOpen && choice.Value == "Beta" && combo.SelectedIndex == 2,
                    "Choice updates close a stale list while preserving the surviving selection");
                choice.ValueValidating += (_, _) => group.Items.Remove(choice);
                await Key("{DOWN}");
                Check(combo.IsDisposed && choice.Value == "Gamma", "Removal during a native selection callback disposes the presentation safely and retains the model");
                choice.Dispose(); Check(observed.ComboCalls > 0 && observed.Rows > 0, "Field chrome and list rows use shared renderer hooks");
                Check(observed.Frames > 0, "Floating popup frames use the shared renderer hook");
                Check(observed.Hot && observed.Pressed && observed.Open, "Renderer receives hover, pressed and open states");
                results.Add($"PASS ComboBox presentation: {(flat ? "flat" : "Office 2024")}, DPI={ribbon.DeviceDpi}");
                completed = true;
            }
            catch (Exception failure) { error = failure; results.Add(failure.ToString()); }
            finally { WF.Cursor.Position = cursor; form.Close(); }
        };
        WF.Application.Run(form);
        if (!completed && error is null) results.Add("FAIL Validation window closed before the scenario completed");
        File.WriteAllLines(Path.Combine(directory, "result.log"), results);
        foreach (string line in results) Console.WriteLine(line);
        return completed && error is null ? 0 : 1;

        void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); results.Add("PASS " + message); }
        async Task Key(string keys)
        {
            var foreground = GetForegroundWindow();
            if (foreground != form.Handle && WF.Control.FromHandle(foreground) is not WF.ToolStripDropDown)
                throw new InvalidOperationException("Validation window lost foreground focus before native input");
            WF.SendKeys.SendWait(keys); await Task.Delay(70);
        }
        void Capture(WF.Control control, string name)
        {
            control.Refresh(); using var bitmap = new Bitmap(control.Width, control.Height);
            using (var graphics = Graphics.FromImage(bitmap)) graphics.CopyFromScreen(control.PointToScreen(Point.Empty), Point.Empty, bitmap.Size);
            bitmap.Save(Path.Combine(directory, $"{name}-{control.DeviceDpi}dpi.png"));
        }
        void CaptureList(WF.ComboBox combo, string name)
        {
            GetWindowRect(List(combo).Parent!.Handle, out var bounds); using var bitmap = new Bitmap(bounds.Right - bounds.Left, bounds.Bottom - bounds.Top);
            using (var graphics = Graphics.FromImage(bitmap)) graphics.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bitmap.Size);
            bitmap.Save(Path.Combine(directory, $"{name}-{combo.DeviceDpi}dpi.png"));
        }
        void CheckChoiceChrome(WF.ComboBox combo)
        {
            var rows = List(combo); var window = (WF.ToolStripDropDown)rows.Parent!;
            var info = new ComboInfo { Size = Marshal.SizeOf<ComboInfo>() }; GetComboBoxInfo(combo.Handle, ref info);
            Check(!IsWindowVisible(info.List) && !window.DropShadowEnabled && rows.ItemHeight >= 28 * ribbon.DeviceDpi / 96,
                "Native ComboLBox stays hidden; the library ListBox owns padded rows with no dark Windows shadow");
            int radius = renderer.GetPopupCornerRadius(ribbon.Theme, ribbon.DeviceDpi);
            Check(flat ? window.Region is null && rows.Region is null : window.Region is not null && rows.Region is not null,
                "Choice window and native rows share the selected renderer's corner clipping");
            if (!flat)
            {
                bool symmetric = true;
                for (int y = 0; y <= radius; y++) for (int x = 0; x <= radius; x++)
                {
                    bool inside = window.Region!.IsVisible(x, y);
                    symmetric &= inside == window.Region.IsVisible(window.Width - 1 - x, y)
                        && inside == window.Region.IsVisible(x, window.Height - 1 - y)
                        && inside == window.Region.IsVisible(window.Width - 1 - x, window.Height - 1 - y);
                }
                Check(symmetric, "All four choice-popup corners share the same clipping pixels");
            }
            int clearance = (int)Math.Round(8 * ribbon.DeviceDpi / 96d) + 2;
            var capture = window.Bounds; capture.Inflate(clearance, clearance);
            using var image = new Bitmap(capture.Width, capture.Height);
            using (var graphics = Graphics.FromImage(image)) graphics.CopyFromScreen(capture.Location, Point.Empty, capture.Size);
            Check(new[] { image.GetPixel(clearance, clearance + window.Height / 2), image.GetPixel(clearance + window.Width - 1, clearance + window.Height / 2),
                image.GetPixel(clearance + window.Width / 2, clearance), image.GetPixel(clearance + window.Width / 2, clearance + window.Height - 1) }
                .All(color => color.ToArgb() == ribbon.Theme.PopupBorder.ToArgb()), "All four choice-popup borders are visible on screen");
            image.Save(Path.Combine(directory, $"choice-frame-shadow-{ribbon.DeviceDpi}dpi.png"));
        }
        async Task ClickChoice(WF.ComboBox combo, int index)
        {
            var list = List(combo); var rect = list.GetItemRectangle(index);
            var point = list.PointToScreen(new Point((rect.Left + rect.Right) / 2, (rect.Top + rect.Bottom) / 2));
            WF.Cursor.Position = point; await Task.Delay(150);
            mouse_event(2, 0, 0, 0, 0); await Task.Delay(50);
            mouse_event(4, 0, 0, 0, 0); await Task.Delay(90);
        }
        async Task ClickArrow(WF.ComboBox combo)
        {
            WF.Cursor.Position = combo.PointToScreen(new(combo.Width - 8, combo.Height / 2));
            await Task.Delay(50); Capture(combo.Parent!, "hover-" + combo.Name);
            mouse_event(2, 0, 0, 0, 0); await Task.Delay(50); Capture(combo.Parent!, "pressed-" + combo.Name);
            mouse_event(4, 0, 0, 0, 0); await Task.Delay(90); Capture(combo.Parent!, "open-" + combo.Name);
            Check(!combo.DroppedDown && List(combo).Visible, "Pointer opens the library choice popup while the OS ComboLBox stays closed");
        }
    }
    private delegate bool EnumWindow(nint window, nint data);
    [DllImport("user32.dll")] private static extern bool EnumThreadWindows(uint thread, EnumWindow callback, nint data);
    [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
    private static WF.ListBox List(WF.ComboBox combo)
    {
        WF.ListBox? result = null;
        EnumThreadWindows(GetCurrentThreadId(), (window, _) =>
        {
            if (WF.Control.FromHandle(window) is WF.ToolStripDropDown { Visible: true } popup)
                result ??= popup.Items.OfType<WF.ToolStripControlHost>().Select(host => host.Control).OfType<WF.ListBox>()
                    .SingleOrDefault(list => list.Name == combo.Name + ".choices");
            return result is null;
        }, 0);
        return result ?? throw new InvalidOperationException("Library choice ListBox is unavailable");
    }
    private static IEnumerable<WF.Control> Tree(WF.Control root) => root.Controls.Cast<WF.Control>().SelectMany(c => new[] { c }.Concat(Tree(c)));
    private static WF.ComboBox Find(WF.Control root, string id) => Tree(root).OfType<WF.ComboBox>().Single(c => c.Name == id);
    private static WF.Control Popup() => WF.Control.FromHandle(GetForegroundWindow()) is WF.ToolStripDropDown window
        ? window.Items.OfType<WF.ToolStripControlHost>().Single().Control : throw new InvalidOperationException("Expected a library popup");
    private static void Click(WF.Control control, Rectangle bounds)
    {
        nint point = (nint)(((bounds.Top + bounds.Height / 2) << 16) | ((bounds.Left + bounds.Width / 2) & 0xffff));
        SendMessage(control.Handle, 0x201, 1, point); SendMessage(control.Handle, 0x202, 0, point);
    }
    private sealed class Observations
    {
        internal Rectangle MenuBounds;
        internal Rectangle GroupBounds;
        internal bool Collapsed, Hot, Pressed, Open;
        internal int ComboCalls, Rows, Frames, Shadows;
        internal void Field(RibbonPaintContext context, RibbonComboBoxItem item)
        { ComboCalls++; Hot |= context.Hot; Pressed |= context.Pressed; Open |= item.IsDropDownOpen; }
    }
    private sealed class PaintWatch : WF.NativeWindow, IDisposable
    {
        internal int Paints, Positions, Selections;
        internal PaintWatch(nint handle) => AssignHandle(handle);
        protected override void WndProc(ref WF.Message message)
        {
            if (message.Msg == 0xF) Paints++;
            if (message.Msg == 0x47) Positions++;
            if (message.Msg is 0xB1 or 0x142) Selections++;
            base.WndProc(ref message);
        }
        public void Dispose() => ReleaseHandle();
    }
    private sealed class Recorder(Observations observed) : RibbonOffice2024Renderer
    {
        public override void DrawPopupShadow(RibbonPaintContext context) { observed.Shadows++; base.DrawPopupShadow(context); }
        public override void DrawItem(RibbonPaintContext context, RibbonItem item, RibbonItemSize size, bool menu)
        { if (item is RibbonDropDownItem) observed.MenuBounds = context.Bounds; base.DrawItem(context, item, size, menu); }
        public override void DrawPopupFrame(RibbonPaintContext context) { observed.Frames++; base.DrawPopupFrame(context); }
        public override void DrawGroup(RibbonPaintContext context, RibbonGroup group, bool collapsed)
        { observed.GroupBounds = context.Bounds; observed.Collapsed = collapsed; base.DrawGroup(context, group, collapsed); }
        public override void DrawComboBox(RibbonPaintContext context, RibbonComboBoxItem item, Rectangle editorBounds)
        { observed.Field(context, item); base.DrawComboBox(context, item, editorBounds); }
        public override void DrawComboBoxItem(RibbonPaintContext context, RibbonComboBoxItem item, int index, bool selected)
        { observed.Rows++; base.DrawComboBoxItem(context, item, index, selected); }
    }
    private sealed class FlatRecorder(Observations observed) : RibbonRenderer
    {
        public override void DrawPopupShadow(RibbonPaintContext context) { observed.Shadows++; base.DrawPopupShadow(context); }
        public override void DrawItem(RibbonPaintContext context, RibbonItem item, RibbonItemSize size, bool menu)
        { if (item is RibbonDropDownItem) observed.MenuBounds = context.Bounds; base.DrawItem(context, item, size, menu); }
        public override void DrawPopupFrame(RibbonPaintContext context) { observed.Frames++; base.DrawPopupFrame(context); }
        public override void DrawGroup(RibbonPaintContext context, RibbonGroup group, bool collapsed)
        { observed.GroupBounds = context.Bounds; observed.Collapsed = collapsed; base.DrawGroup(context, group, collapsed); }
        public override void DrawComboBox(RibbonPaintContext context, RibbonComboBoxItem item, Rectangle editorBounds)
        { observed.Field(context, item); base.DrawComboBox(context, item, editorBounds); }
        public override void DrawComboBoxItem(RibbonPaintContext context, RibbonComboBoxItem item, int index, bool selected)
        { observed.Rows++; base.DrawComboBoxItem(context, item, index, selected); }
    }
    private sealed class State : INotifyPropertyChanged
    {
        private string _choice = "Alpha", _edit = "100%";
        public string Choice { get => _choice; set { if (_choice == value) return; _choice = value; PropertyChanged?.Invoke(this, new(nameof(Choice))); } }
        public string Edit { get => _edit; set { if (_edit == value) return; _edit = value; PropertyChanged?.Invoke(this, new(nameof(Edit))); } }
        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
