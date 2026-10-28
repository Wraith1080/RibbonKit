using System.IO;
using System.Runtime.InteropServices;
using RibbonKit.WinForms;
using WF = System.Windows.Forms;

namespace IndependentConsumer;

internal static class RibbonCustomizationInteractionChecks
{
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(nint window);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern nint SendMessage(nint window, uint message, nint wParam, nint lParam);
    internal static int Run(string directory, bool neutral)
    {
        WF.Application.SetHighDpiMode(WF.HighDpiMode.PerMonitorV2); WF.Application.EnableVisualStyles();
        Directory.CreateDirectory(directory); List<string> log = []; Exception? failure = null;
        using RibbonRenderer renderer = neutral ? new RibbonRenderer() : new RibbonOffice2024Renderer();
        using var host = new WF.Form { Text = "Independent Customize Ribbon consumer", Width = 1280, Height = 720, StartPosition = WF.FormStartPosition.CenterScreen };
        var ribbon = new RibbonControl { Renderer = renderer, CustomizationApplicationId = "consumer.ribbon" };
        var home = new RibbonTab { Id = "home", Text = "Home" }; var insert = new RibbonTab { Id = "insert", Text = "Insert" };
        var font = new RibbonGroup { Id = "font", Text = "Font" }; var insertGroup = new RibbonGroup { Id = "insert.group", Text = "Illustrations" };
        var bold = new RibbonToggleItem { Id = "bold", Text = "Bold", Icon = new SampleIcon() }; var italic = new RibbonToggleItem { Id = "italic", Text = "Italic", Icon = new SampleIcon() };
        var copy = new RibbonButtonItem { Id = "copy", Text = "Copy", Icon = new SampleIcon() }; var field = new RibbonTextBoxItem { Id = "name", Text = "Name", Value = "Original" };
        font.Items.AddRange(bold, italic, copy, field); home.Groups.Add(font); insert.Groups.Add(insertGroup); ribbon.Tabs.AddRange(home, insert);
        ribbon.QuickAccessItems.Add(new RibbonQuickAccessReference { Source = bold }); ribbon.CommandCatalog.CaptureDefaults();
        host.Controls.Add(new WF.TextBox { Dock = WF.DockStyle.Fill, Multiline = true, Text = "Independent public API consumer. No Showcase resources or helpers." }); host.Controls.Add(ribbon);
        host.Shown += async (_, _) =>
        {
            try
            {
                await Task.Yield(); host.Activate(); SetForegroundWindow(host.Handle); await Task.Delay(150);
                using (var dialog = new RibbonQuickAccessCustomizationForm(ribbon, "consumer.ribbon"))
                {
                    dialog.SelectedPage = dialog.Pages.Single(p => p.Name == "CustomizeRibbonOptionsPage"); dialog.Show(host); dialog.Activate(); SetForegroundWindow(dialog.Handle); await Task.Delay(150);
                    var page = dialog.RibbonEditor; var tree = Child<WF.TreeView>(page, "RibbonHierarchy");
                    Check(dialog.SelectedPage!.Text == "Customize Ribbon" && tree.Nodes.Count == 2, "Shared options page contains authored hierarchy");
                    Geometry(page); PointerTree(page); await Capture(dialog, "ribbon-compact");
                    Check(!Child<WF.Button>(page, "RemoveRibbonItem").Enabled, "Authored tab cannot be removed");
                    tree.Focus(); tree.SelectedNode = tree.Nodes[0]; WF.SendKeys.SendWait(" ");
                    Check(!page.Session.Draft.Ribbon!.Tabs[0].Visible, "Space toggles the selected tab visibility checkbox");
                    tree.SelectedNode = tree.Nodes[1]; WF.SendKeys.SendWait(" ");
                    Check(page.Session.Draft.Ribbon.Tabs[1].Visible, "Last visible tab cannot be hidden");
                    tree.SelectedNode = tree.Nodes[0]; WF.SendKeys.SendWait(" ");
                    Child<WF.Button>(page, "NewRibbonTab").PerformClick(); Child<WF.TextBox>(page, "RibbonCommandSearch").Text = "Bold";
                    Check(Child<WF.ListBox>(page, "RibbonAvailableCommands").Items.Count == 1, "Available search excludes groups and filters commands");
                    Child<WF.Button>(page, "AddRibbonCommand").PerformClick(); Check(ribbon.Tabs.Count == 2, "Structural edit remains a draft");
                    var group = page.Session.Draft.Ribbon!.Tabs.Single(t => t.IsCustom).Groups[0];
                    Exception? editError = null;
                    using (var editTimer = new WF.Timer { Interval = 100 })
                    {
                        editTimer.Tick += async (_, _) =>
                        {
                            if (WF.Application.OpenForms.Cast<WF.Form>().FirstOrDefault(f => f.Text == "Edit ribbon item") is not { } edit) return;
                            editTimer.Stop();
                            try { await Capture(edit, "edit-ribbon-command"); Child<WF.TextBox>(edit, "RibbonItemName").Text = "Strong emphasis"; Child<WF.ComboBox>(edit, "RibbonItemSize").SelectedItem = RibbonItemSize.Small; ((WF.Button)edit.AcceptButton!).PerformClick(); }
                            catch (Exception error) { editError = error; edit.Close(); }
                        };
                        editTimer.Start(); Child<WF.Button>(page, "EditRibbonItem").PerformClick(); editTimer.Stop();
                    }
                    if (editError is not null) throw editError;
                    Check(group.Commands[0].Label == "Strong emphasis" && group.Commands[0].Size == RibbonItemSize.Small, "Native Edit dialog stages name and Stacked command size");
                    page.RefreshDraft(); Child<WF.TextBox>(page, "RibbonHierarchySearch").Text = "Strong";
                    Check(tree.Nodes.Count == 1 && page.Session.Draft.Ribbon.Tabs.Count == 3, "Hierarchy search filters the view without deleting hidden definitions");
                    Child<WF.TextBox>(page, "RibbonHierarchySearch").Clear(); await Capture(dialog, "ribbon-pending");
                    Child<WF.Button>(dialog, "ApplyCustomization").PerformClick();
                    Check(ribbon.Tabs.Count == 3 && ReferenceEquals(home.Groups[0], font) && font.Items.Contains(bold), "Apply retains authored tab/group/command objects");
                    string profile = page.Session.ExportJson(); File.WriteAllText(Path.Combine(directory, "profile.json"), profile);
                    dialog.SelectedPage = dialog.Pages.Single(p => p.Name == "QuickAccessOptionsPage");
                    dialog.Editor.Session.Draft.QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon;
                    Child<WF.Button>(dialog, "ApplyCustomization").PerformClick();
                    dialog.SelectedPage = dialog.Pages.Single(p => p.Name == "CustomizeRibbonOptionsPage");
                    Check(page.ApplyChanges() && ribbon.QuickAccessPosition == RibbonQuickAccessPosition.BelowRibbon, "Applying Ribbon after QAT preserves the independent page's changes");
                    Child<WF.Button>(page, "NewRibbonTab").PerformClick(); Child<WF.Button>(dialog, "CancelCustomization").PerformClick();
                    Check(ribbon.Tabs.Count == 3, "Cancel discards only pending structure after last Apply");
                }
                var customTab = ribbon.Tabs.Single(t => t.Id.StartsWith("user.tab:")); ribbon.SelectedTab = customTab;
                var proxy = (RibbonToggleItem)customTab.Groups[0].Items[0]; int clicks = 0; bold.Click += (_, _) => clicks++;
                proxy.PerformClick(); Check(clicks == 1 && bold.Checked && proxy.Checked && ReferenceEquals(ribbon.CommandCatalog.GetSource(proxy), bold), "Custom presentation invokes original command exactly once");
                host.Activate(); SetForegroundWindow(host.Handle); await Task.Delay(300); // Finish the owned modeless window's native close animation before capture.
                await Capture(host, "ribbon-applied");
                ribbon.Density = RibbonDensity.Touch;
                using (var dialog = new RibbonQuickAccessCustomizationForm(ribbon, "consumer.ribbon"))
                {
                    dialog.SelectedPage = dialog.Pages.Single(p => p.Name == "CustomizeRibbonOptionsPage"); dialog.Show(host); dialog.Activate(); SetForegroundWindow(dialog.Handle); await Task.Delay(150);
                    Geometry(dialog.RibbonEditor); PointerTree(dialog.RibbonEditor); await Capture(dialog, "ribbon-touch");
                    using var larger = new Font(host.Font.FontFamily, 13f); dialog.Font = larger; await Task.Delay(80); Geometry(dialog.RibbonEditor); await Capture(dialog, "ribbon-touch-large-font");
                    dialog.Font = host.Font; dialog.Width = dialog.MinimumSize.Width; await Task.Delay(60);
                    Check(dialog.RibbonEditor.Width >= (int)Math.Ceiling(650 * ribbon.DeviceDpi / 96d), "Narrow dialog preserves the logical page minimum"); await Capture(dialog, "ribbon-touch-narrow");
                    Child<WF.Button>(dialog.RibbonEditor, "ResetRibbon").PerformClick(); Check(ribbon.Tabs.Count == 3 && dialog.RibbonEditor.Session.Draft.Ribbon!.Tabs.Count == 2, "Reset stages authored defaults");
                    Child<WF.Button>(dialog, "ApplyCustomization").PerformClick(); Check(ribbon.Tabs.Count == 2 && ReferenceEquals(ribbon.Tabs[0], home), "Reset Apply restores original models"); dialog.Close();
                }
                bool selectedRibbon = false;
                using var timer = new WF.Timer { Interval = 100 };
                timer.Tick += (_, _) => { if (WF.Application.OpenForms.OfType<RibbonQuickAccessCustomizationForm>().FirstOrDefault() is not { } dialog) return; timer.Stop(); selectedRibbon = dialog.SelectedPage!.Text == "Customize Ribbon"; Child<WF.Button>(dialog, "CancelCustomization").PerformClick(); };
                timer.Start(); var result = ribbon.ShowRibbonCustomization(host); timer.Stop(); Check(selectedRibbon && result == WF.DialogResult.Cancel, "Root convenience selects Customize Ribbon in the owner-centered dialog");
                log.Add($"PASS: native DPI={ribbon.DeviceDpi}; palette={(neutral ? "neutral" : "Office2024")}; focused M5C checks only");
            }
            catch (Exception error) { failure = error; log.Add(error.ToString()); }
            finally { File.WriteAllLines(Path.Combine(directory, "result.log"), log); host.Close(); }
        };
        WF.Application.Run(host); return failure is null ? 0 : 1;
        void Check(bool condition, string text) { if (!condition) throw new InvalidOperationException(text); log.Add("PASS: " + text); }
        void Geometry(RibbonCustomizationEditor page)
        {
            var left = Child<WF.Control>(page, "RibbonAvailableFrame"); var right = Child<WF.Control>(page, "RibbonHierarchyFrame");
            var a = Child<WF.Control>(page, "RibbonSearchFrame"); var b = Child<WF.Control>(page, "RibbonHierarchySearchFrame");
            Check(left.Top == right.Top && left.Bottom == right.Bottom && a.Top == b.Top && a.Height == b.Height, "Both panes and searches share vertical alignment");
            int gap = (int)Math.Ceiling(8 * page.DeviceDpi / 96d); Check(a.Bottom + gap == left.Top && b.Bottom + gap == right.Top, "Search bottom spacing follows the shared logical gap");
            Check(Child<WF.TreeView>(page, "RibbonHierarchy").ItemHeight == Child<WF.ListBox>(page, "RibbonAvailableCommands").ItemHeight, "Native hierarchy and command rows use the same density height");
        }
        void PointerTree(RibbonCustomizationEditor page)
        {
            var tree = Child<WF.TreeView>(page, "RibbonHierarchy"); var node = tree.Nodes[0]; node.EnsureVisible(); node.Expand(); tree.SelectedNode = node; tree.Focus();
            void Click(int x)
            {
                int y = node.Bounds.Top + tree.ItemHeight / 2; nint point = (nint)((y << 16) | x);
                SendMessage(tree.Handle, 0x201, 1, point); SendMessage(tree.Handle, 0x202, 0, point);
            }
            Click(2); Check(!node.IsExpanded, "Outer expander target collapses exactly once");
            WF.SendKeys.SendWait("{RIGHT}"); Check(node.IsExpanded, "Native right-arrow navigation expands the themed node");
            bool visible = page.Session.Draft.Ribbon!.Tabs[0].Visible;
            Click(tree.Indent + 1); Check(page.Session.Draft.Ribbon.Tabs[0].Visible != visible, "Outer checkbox target toggles the draft exactly once");
            Click(tree.Indent + 1); Check(page.Session.Draft.Ribbon.Tabs[0].Visible == visible, "Second pointer click restores tab visibility");
        }
        async Task Capture(WF.Control control, string name)
        {
            await Task.Delay(80); control.Refresh();
            if (GetForegroundWindow() != control.Handle) throw new InvalidOperationException("Capture target lost foreground");
            var bounds = control.RectangleToScreen(control.ClientRectangle); using var bitmap = new Bitmap(bounds.Width, bounds.Height); using var graphics = Graphics.FromImage(bitmap);
            graphics.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size); bitmap.Save(Path.Combine(directory, name + ".png"));
        }
    }
    private static T Child<T>(WF.Control root, string name) where T : WF.Control => (T)root.Controls.Find(name, true).Single();
    private sealed class SampleIcon : IRibbonIcon
    {
        public void Draw(Graphics graphics, Rectangle bounds, RibbonIconContext context)
        { using var pen = new Pen(context.Foreground, Math.Max(1, context.DeviceDpi / 96f)); bounds.Inflate(-Math.Max(1, bounds.Width / 6), -Math.Max(1, bounds.Height / 8)); graphics.DrawRectangle(pen, bounds); graphics.DrawLine(pen, bounds.Left, bounds.Top + bounds.Height / 2, bounds.Right, bounds.Top + bounds.Height / 2); }
    }
}
