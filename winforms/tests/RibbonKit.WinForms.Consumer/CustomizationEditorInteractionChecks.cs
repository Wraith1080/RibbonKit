using System.IO;
using System.Runtime.InteropServices;
using RibbonKit.WinForms;
using WF = System.Windows.Forms;

namespace IndependentConsumer;

internal static class CustomizationEditorInteractionChecks
{
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(nint handle);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern nint SendMessage(nint handle, uint message, nint wParam, nint lParam);
    internal static int Run(string directory, bool neutral)
    {
        WF.Application.SetHighDpiMode(WF.HighDpiMode.PerMonitorV2); WF.Application.EnableVisualStyles();
        Directory.CreateDirectory(directory); var log = new List<string>(); Exception? failure = null;
        var paint = new CaptionPaint();
        using RibbonRenderer renderer = neutral ? new FlatCaptionRenderer(paint) : new OfficeCaptionRenderer(paint);
        using var host = new WF.Form { Text = "Independent QAT editor consumer", Width = 1120, Height = 620, StartPosition = WF.FormStartPosition.CenterScreen };
        var ribbon = new RibbonControl { Renderer = renderer, CustomizationApplicationId = "consumer.editor" };
        var tab = new RibbonTab { Id = "tab.home", Text = "Home" }; var group = new RibbonGroup { Id = "group.clipboard", Text = "Clipboard" };
        var copy = new RibbonButtonItem { Id = "cmd.copy", Text = "Copy", Icon = new DocumentIcon() }; var undo = new RibbonButtonItem { Id = "cmd.undo", Text = "Undo", Icon = new DocumentIcon() };
        group.Items.Add(copy); tab.Groups.Add(group); ribbon.Tabs.Add(tab); ribbon.QuickAccessItems.Add(undo);
        ribbon.CommandCatalog.Commands.Add(new RibbonButtonItem { Id = "cmd.redo", Text = "Redo" }); ribbon.CommandCatalog.CaptureDefaults();
        host.Controls.Add(new WF.TextBox { Dock = WF.DockStyle.Fill, Multiline = true, Text = "This application references only RibbonKit.WinForms. The library owns this editor, its controls and context menu." }); host.Controls.Add(ribbon);
        int applied = 0; ribbon.CustomizationApplied += (_, _) => applied++;
        host.Shown += async (_, _) =>
        {
            try
            {
                await Task.Yield(); host.Activate(); SetForegroundWindow(host.Handle); await Task.Delay(180);
                using (var dialog = new RibbonQuickAccessCustomizationForm(ribbon, "consumer.editor"))
                {
                    var qatPage = dialog.SelectedPage;
                    var appPage = new RibbonOptionsPage { Text = "Editor", Name = "ConsumerEditorPage" };
                    var wrap = new WF.CheckBox { Text = "Wrap document text", AutoSize = true, Location = new(8, 12) }; appPage.Controls.Add(wrap);
                    bool savedWrap = false; int appApplies = 0;
                    appPage.Applied += (_, _) => { savedWrap = wrap.Checked; appApplies++; };
                    appPage.Cancelled += (_, _) => wrap.Checked = savedWrap;
                    dialog.Pages.Insert(0, appPage);
                    dialog.Show(host); dialog.Activate(); SetForegroundWindow(dialog.Handle); await Task.Delay(180);
                    dialog.SelectedPage = appPage; wrap.Checked = true; Child<WF.Button>(dialog, "ApplyCustomization").PerformClick();
                    Check(savedWrap && appApplies == 1 && applied == 0, "Application page applies independently of QAT"); await Capture(dialog, "application-options-page");
                    wrap.Checked = false; dialog.SelectedPage = qatPage;
                    var page = dialog.Editor; var search = Child<WF.TextBox>(page, "CommandSearch"); var available = Child<WF.ListBox>(page, "AvailableCommands");
                    var current = Child<WF.ListBox>(page, "ToolbarCommands");
                    Check(dialog.ContainsFocus, "Native editor receives focus"); Geometry(dialog); await Capture(dialog, "editor-compact");
                    search.Text = "Copy"; Child<WF.Button>(page, "AddCommand").PerformClick();
                    Check(page.Session.Draft.QuickAccess.Count == 2 && ribbon.QuickAccessItems.Count == 1, "Add stages without mutating live toolbar");
                    Child<WF.Button>(page, "MoveCommandUp").PerformClick();
                    var toolbarSearch = Child<WF.TextBox>(page, "ToolbarSearch"); toolbarSearch.Text = "Undo";
                    Check(current.Items.Count == 1 && page.Session.Draft.QuickAccess.Count == 2, "Right search filters only the view"); toolbarSearch.Clear();
                    Check(page.Controls.Find("ToolbarPosition", true).Length == 0 && page.Controls.Find("ToolbarVisible", true).Length == 0, "Placement and visibility are absent from dialog");
                    Child<WF.Button>(dialog, "ApplyCustomization").PerformClick();
                    Check(applied == 1 && ribbon.QuickAccessItems.Count == 2 && ribbon.QuickAccessPosition == RibbonQuickAccessPosition.TabRow && ribbon.QuickAccessVisible, "Apply commits command order and retains toolbar presentation");
                    toolbarSearch.Text = "Undo"; Child<WF.Button>(page, "RemoveCommand").PerformClick(); Child<WF.Button>(dialog, "ApplyCustomization").PerformClick();
                    Check(applied == 2 && ribbon.QuickAccessItems.Count == 1 && ribbon.QuickAccessItems[0] is RibbonQuickAccessReference { Source: var kept } && ReferenceEquals(kept, copy), "Filtered remove targets Undo and retains hidden Copy");
                    search.Text = "Undo"; Check(available.Items.Count == 1 && Child<WF.Button>(page, "AddCommand").Enabled, "Removed QAT-only command remains discoverable");
                    Child<WF.Button>(page, "AddCommand").PerformClick(); await Capture(dialog, "editor-pending");
                    Child<WF.Button>(dialog, "CancelCustomization").PerformClick();
                    Check(ribbon.QuickAccessItems.Count == 1 && applied == 2 && wrap.Checked, "Cancel discards pending settings and retains each page's last Apply");
                }
                await OpenMenu("context-compact");
                WF.SendKeys.SendWait("{ENTER}"); await Task.Delay(80);
                Check(ribbon.QuickAccessPosition == RibbonQuickAccessPosition.BelowRibbon && applied == 3, "Context menu keyboard command moves QAT");
                await OpenMenu("context-compact-below"); WF.SendKeys.SendWait("{DOWN}{ENTER}"); await Task.Delay(80);
                Check(!ribbon.QuickAccessVisible && applied == 4, "Context menu keyboard command changes QAT visibility");
                ribbon.Density = RibbonDensity.Touch;
                using (var touch = new RibbonQuickAccessCustomizationForm(ribbon, "consumer.editor"))
                {
                    using var largeFont = new Font(host.Font.FontFamily, 15f);
                    touch.Font = largeFont; touch.Show(host); touch.Activate(); SetForegroundWindow(touch.Handle); await Task.Delay(150);
                    Geometry(touch); await Capture(touch, "editor-touch-large-font");
                    var current = Child<WF.ListBox>(touch.Editor, "ToolbarCommands"); string? selected = current.GetItemText(current.SelectedItem);
                    int rowHeight = current.ItemHeight;
                    touch.Font = host.Font; await Task.Delay(80); touch.Font = largeFont; await Task.Delay(80);
                    Check(current.ItemHeight == rowHeight && current.GetItemText(current.SelectedItem) == selected, "Font changes recompute row sizes without cumulative scaling or lost selection");
                    Child<WF.Button>(touch, "ResetCustomization").PerformClick();
                    Check(touch.Editor.Session.Draft.QuickAccess.Count == 1 && ribbon.QuickAccessItems.Count == 1 && touch.Editor.Session.Draft.QuickAccess[0].CommandId == undo.Id
                        && touch.Editor.Session.Draft.QuickAccessPosition == ribbon.QuickAccessPosition && touch.Editor.Session.Draft.QuickAccessVisible == ribbon.QuickAccessVisible, "Reset stages default commands and preserves menu-only preferences");
                    ribbon.QuickAccessVisible = true;
                    Check(!touch.Editor.ApplyChanges(), "External changes block stale Apply");
                    Child<WF.Button>(touch, "ReloadCustomization").PerformClick();
                    Check(touch.Editor.Session.Draft.QuickAccessVisible && touch.Editor.Session.Draft.QuickAccess[0].CommandId == copy.Id, "Reload reflects current live state");
                    touch.Close();
                }
                await OpenMenu("context-touch");
                int closing = 0; bool veto = true;
                host.FormClosing += (_, e) => { if (veto) { closing++; e.Cancel = true; } };
                WF.SendKeys.SendWait("%{F4}"); await Task.Delay(100);
                Check(closing == 1 && !host.IsDisposed && host.Visible, "Alt+F4 from shared context popup reaches host and respects cancellation"); veto = false;
                bool opening = false; ribbon.OptionsDialogOpening += (_, e) => { opening = true; e.Dialog.Pages.Insert(0, new RibbonOptionsPage { Text = "Application" }); };
                using var timer = new WF.Timer { Interval = 100 };
                timer.Tick += (_, _) => { if (WF.Application.OpenForms.OfType<RibbonQuickAccessCustomizationForm>().FirstOrDefault() is { } editor) Child<WF.Button>(editor, "CancelCustomization").PerformClick(); };
                timer.Start(); var result = ribbon.ShowQuickAccessCustomization(host); timer.Stop();
                Check(opening && result == WF.DialogResult.Cancel && applied == 4, "Root convenience opens extensible modal editor and Cancel applies nothing");
                using var acceptTimer = new WF.Timer { Interval = 100 };
                acceptTimer.Tick += (_, _) =>
                {
                    if (WF.Application.OpenForms.OfType<RibbonQuickAccessCustomizationForm>().FirstOrDefault() is not { } editor) return;
                    acceptTimer.Stop(); var ok = Child<WF.Button>(editor, "AcceptCustomization");
                    nint center = (nint)((ok.Height / 2 << 16) | (ok.Width / 2));
                    SendMessage(ok.Handle, 0x201, 1, center); SendMessage(ok.Handle, 0x202, 0, center);
                };
                acceptTimer.Start(); result = ribbon.ShowQuickAccessCustomization(host); acceptTimer.Stop();
                Check(result == WF.DialogResult.OK && applied == 5, "Native OK button click commits and closes through the normal dialog path");
                ribbon.Density = RibbonDensity.Compact;
                for (int i = 0; i < 5; i++) group.Items.Add(new RibbonButtonItem { Id = "caption.extra." + i, Text = "Additional command " + i });
                host.Width = 380; host.Activate(); SetForegroundWindow(host.Handle); await Task.Delay(80); paint.Texts.Clear(); ribbon.Refresh();
                Check(paint.Collapsed && paint.Texts.Contains("Clipboard"), "Collapsed short caption fits without clipping");
                await Capture(host, "collapsed-group-short");
                group.Text = "Clipboard and document commands with a much longer group caption"; paint.Texts.Clear(); ribbon.Refresh();
                Check(paint.Collapsed && paint.Texts.Any(t => t.EndsWith('…')), "Collapsed long caption paints an ellipsis within the width cap");
                await Capture(host, "collapsed-group-long");
                log.Add($"PASS: actual DPI={ribbon.DeviceDpi}; palette={(neutral ? "neutral" : "Office2024")}; focused native editor checks only");
            }
            catch (Exception e) { failure = e; log.Add(e.ToString()); }
            finally { File.WriteAllLines(Path.Combine(directory, "result.log"), log); host.Close(); }
        };
        WF.Application.Run(host); return failure is null ? 0 : 1;

        void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); log.Add("PASS: " + message); }
        void Geometry(RibbonQuickAccessCustomizationForm dialog)
        {
            var page = dialog.Editor;
            foreach (var list in Tree(page).OfType<WF.ListBox>())
                Check(list.Width > 100 && list.Height >= list.ItemHeight && list.ItemHeight >= list.Font.Height + 8 * dialog.DeviceDpi / 96, "Compact list row fits text with padding");
            foreach (var button in Tree(dialog).OfType<WF.Button>())
                Check(button.Height >= button.Font.Height + 8 * dialog.DeviceDpi / 96 && button.Width >= WF.TextRenderer.MeasureText(button.Text.Replace("&", ""), button.Font).Width, "Button caption fits measured target");
            Check(WF.Screen.FromControl(dialog).WorkingArea.Contains(dialog.Bounds), "Dialog fits current monitor working area");
            var apply = Child<WF.Button>(dialog, "ApplyCustomization"); var cancel = Child<WF.Button>(dialog, "CancelCustomization"); var ok = Child<WF.Button>(dialog, "AcceptCustomization");
            Check(ReferenceEquals(apply.Parent, cancel.Parent) && apply.Left > cancel.Left && cancel.Left > ok.Left, "Footer reads OK, Cancel, Apply on one row");
            var search = Child<WF.TextBox>(page, "CommandSearch"); var frame = Child<WF.Panel>(page, "CommandSearchFrame");
            Check(frame.Height >= (ribbon.Density == RibbonDensity.Touch ? 44 : 28) * dialog.DeviceDpi / 96 && Math.Abs(2 * search.Top + search.Height - frame.ClientSize.Height) <= 1, "Search target follows density and centers native text vertically");
            var current = Child<WF.ListBox>(page, "ToolbarCommands"); var reset = Child<WF.Button>(page, "ResetCustomization");
            var listBottom = current.PointToScreen(new(0, current.Height)).Y; var resetTop = reset.PointToScreen(Point.Empty).Y;
            Check(resetTop >= listBottom && resetTop - listBottom < 16 * dialog.DeviceDpi / 96, "Lists extend down to Reset and Reload spacing");
            var availableFrame = Child<WF.Panel>(page, "AvailableCommandsFrame");
            int gap = resetTop - availableFrame.PointToScreen(new(0, availableFrame.Height)).Y;
            foreach (var (fieldName, frameName, listName, labelName) in new[] {
                ("CommandSearch", "CommandSearchFrame", "AvailableCommandsFrame", "AvailableCommandsLabel"),
                ("ToolbarSearch", "ToolbarSearchFrame", "ToolbarCommandsFrame", "ToolbarCommandsLabel") })
            {
                var field = Child<WF.TextBox>(page, fieldName); var fieldFrame = Child<WF.Panel>(page, frameName); var listFrame = Child<WF.Panel>(page, listName); var label = Child<WF.Label>(page, labelName);
                Check(fieldFrame.Top - label.Bottom == gap && listFrame.Top - fieldFrame.Bottom == gap, "Both search fields match Reset row spacing above and below");
                Check(Math.Abs(field.Top * 2 + field.Height - fieldFrame.ClientSize.Height) <= 1, "Both native search editors stay vertically centered");
                Check((listFrame.Controls.OfType<WF.ListBox>().Single().Region is not null) == !neutral, "List corner clipping follows renderer policy");
            }
        }
        async Task<WF.ToolStripDropDown> OpenMenu(string capture)
        {
            host.Activate(); SetForegroundWindow(host.Handle); ribbon.Focus(); await Task.Delay(80);
            var screen = ribbon.PointToScreen(new(ribbon.ClientSize.Width / 2, 4));
            SendMessage(ribbon.Handle, 0x7B, ribbon.Handle, (nint)((screen.Y << 16) | (screen.X & 0xffff))); await Task.Delay(100);
            var menu = WF.Control.FromHandle(GetForegroundWindow()) as WF.ToolStripDropDown ?? throw new InvalidOperationException("Expected shared ribbon context popup");
            Check(menu is not WF.ContextMenuStrip && menu.Items.OfType<WF.ToolStripControlHost>().Count() == 1, "Context uses shared ribbon popup surface and frame");
            await Capture(menu, capture); return menu;
        }
        async Task Capture(WF.Control control, string name)
        {
            await Task.Delay(70); control.Refresh();
            if (GetForegroundWindow() != control.Handle) throw new InvalidOperationException("Capture target lost foreground");
            var rect = control.RectangleToScreen(control.ClientRectangle);
            using var bitmap = new Bitmap(rect.Width, rect.Height); using var graphics = Graphics.FromImage(bitmap);
            graphics.CopyFromScreen(rect.Location, Point.Empty, rect.Size); bitmap.Save(Path.Combine(directory, name + ".png"));
        }
    }
    private static T Child<T>(WF.Control control, string name) where T : WF.Control => (T)control.Controls.Find(name, true).Single();
    private static IEnumerable<WF.Control> Tree(WF.Control parent) { foreach (WF.Control child in parent.Controls) { yield return child; foreach (var nested in Tree(child)) yield return nested; } }
    private sealed class CaptionPaint
    {
        internal bool Inside, Collapsed;
        internal readonly List<string> Texts = [];
    }
    private sealed class OfficeCaptionRenderer(CaptionPaint paint) : RibbonOffice2024Renderer
    {
        public override void DrawGroup(RibbonPaintContext context, RibbonGroup group, bool collapsed) { paint.Collapsed = collapsed; paint.Inside = true; try { base.DrawGroup(context, group, collapsed); } finally { paint.Inside = false; } }
        public override void DrawText(RibbonPaintContext context, string text, Color color, WF.TextFormatFlags flags) { if (paint.Inside) paint.Texts.Add(text); base.DrawText(context, text, color, flags); }
    }
    private sealed class FlatCaptionRenderer(CaptionPaint paint) : RibbonRenderer
    {
        public override void DrawGroup(RibbonPaintContext context, RibbonGroup group, bool collapsed) { paint.Collapsed = collapsed; paint.Inside = true; try { base.DrawGroup(context, group, collapsed); } finally { paint.Inside = false; } }
        public override void DrawText(RibbonPaintContext context, string text, Color color, WF.TextFormatFlags flags) { if (paint.Inside) paint.Texts.Add(text); base.DrawText(context, text, color, flags); }
    }
    private sealed class DocumentIcon : IRibbonIcon
    {
        public void Draw(Graphics graphics, Rectangle bounds, RibbonIconContext context)
        {
            var inset = Rectangle.Inflate(bounds, -Math.Max(1, bounds.Width / 6), -Math.Max(1, bounds.Height / 10));
            using var pen = new Pen(context.Foreground, Math.Max(1, context.DeviceDpi / 96f)); graphics.DrawRectangle(pen, inset);
            graphics.DrawLine(pen, inset.Left + 3, inset.Top + inset.Height / 3, inset.Right - 3, inset.Top + inset.Height / 3);
            graphics.DrawLine(pen, inset.Left + 3, inset.Top + inset.Height * 2 / 3, inset.Right - 3, inset.Top + inset.Height * 2 / 3);
        }
    }
}
