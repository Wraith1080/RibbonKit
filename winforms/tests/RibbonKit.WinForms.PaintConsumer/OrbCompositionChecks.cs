using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using RibbonKit.WinForms;

internal static class OrbCompositionChecks
{
    internal static void Defaults(string directory, List<string> log)
    {
        int count = 0;
        foreach (var (name, theme) in new[] { ("2024", RibbonThemes.Office2024Light), ("2019", RibbonThemes.Office2019Light), ("2013", RibbonThemes.Office2013Light), ("2010", RibbonThemes.Office2010Blue) })
        foreach (bool office in new[] { false, true }) foreach (int dpi in new[] { 96, 120, 144, 192 })
        {
            using RibbonRenderer painter = office ? new RibbonOffice2024Renderer() : new RibbonRenderer();
            using var image = new Bitmap(650, 100); using var g = Graphics.FromImage(image); g.Clear(Color.Magenta);
            using var menu = new RibbonDropDownItem { Text = "File" };
            for (int state = 0; state < 5; state++)
            {
                menu.Enabled = state != 3;
                var context = new RibbonPaintContext(g, new(5 + state * 130, 5, 120, 90), theme, Control.DefaultFont, dpi, Hot: state == 1, Pressed: state == 2, Focused: state == 4);
                var saved = g.Save(); g.SetClip(context.Bounds); painter.DrawSurface(context, false); painter.DrawFileButton(context, menu); g.Restore(saved);
            }
            image.Save(Path.Combine(directory, $"default-file-{name}-{office}-{dpi}.png")); count++;
        }
        log.Add($"PASS: {count} existing rectangular File paint images; four families, both public painters, four DPI values and five states.");
    }
#if G2
    internal static void Features(string directory, List<string> log)
    {
        var appearance = new RibbonApplicationButtonAppearanceSettings
        {
            Background = new(Color.White, Color.LightGray), HoverBackground = new(Color.LightYellow, Color.Gold), PressedBackground = new(Color.Orange, Color.DarkOrange),
            OpenBackground = new(Color.DarkOrange, Color.Orange), BorderColor = Color.Silver, HoverBorderColor = Color.Gold, InnerBorderColor = Color.White, GlyphColor = Color.SteelBlue
        };
        var theme = RibbonThemes.Office2010Blue with { ApplicationButtonShape = RibbonApplicationButtonShape.Orb, ApplicationButtonAppearance = appearance, PopupCornerRadius = 0, PopupShadowEnabled = false };
        using var host = new RibbonForm { Bounds = new(-16000, -16000, 1000, 700), StartPosition = FormStartPosition.Manual, ShowInTaskbar = false };
        using var ribbon = new Probe { Theme = theme, AnimationLevel = RibbonAnimationLevel.None };
        ribbon.FileMenu.Id = "consumer.file"; ribbon.FileMenu.Visible = true; var tab = new RibbonTab { Text = "Consumer" }; var group = new RibbonGroup { Text = "Native fields", CanResize = false };
        var field = new RibbonTextBoxItem { Id = "retained.field", Value = "Retained document", Text = "Name", SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
        var save = new RibbonButtonItem { Text = "Save", SimplifiedPlacement = RibbonSimplifiedPlacement.Inline }; group.Items.AddRange(field, save); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        ribbon.QuickAccessItems.Add(new RibbonQuickAccessReference { Source = save });
        int primary = 0, opening = 0;
        var pane = new RibbonSplitButtonItem { Id = "save.as", Text = "Save as" }; pane.Click += (_, _) => primary++;
        using var content = new UserControl { Size = new(300, 220) }; var native = new TextBox { Text = "Menu native editor", Dock = DockStyle.Top }; content.Controls.Add(native); pane.Content = content;
        ribbon.FileMenu.Items.Add(pane); ribbon.ApplicationMenu.DefaultPane.Items.Add(new RibbonButtonItem { Text = "Recent document" });
        ribbon.FileMenu.DropDownOpening += (_, _) => opening++;
        host.Controls.Add(ribbon); host.Ribbon = ribbon; host.Show(); Application.DoEvents();
        var editor = Children(ribbon).OfType<TextBox>().Single(t => t.Name == field.Id); nint handle = editor.Handle; var parent = editor.Parent; editor.Select(2, 4);
        int scopes = 0;
        foreach (var mode in Enum.GetValues<RibbonLayoutMode>()) foreach (var density in Enum.GetValues<RibbonDensity>()) foreach (var qat in Enum.GetValues<RibbonQuickAccessPosition>())
        {
            ribbon.LayoutMode = mode; ribbon.Density = density; ribbon.QuickAccessPosition = qat;
            var box = ribbon.ApplicationButtonBounds; Require(box.Width == box.Height && box.Top < box.Bottom, "Measured circular File bounds");
            Require(editor.Handle == handle && ReferenceEquals(editor.Parent, parent) && editor.Text == "Retained document" && editor.SelectionStart == 2 && editor.SelectionLength == 4, "Native field survives shape/mode/density/QAT placement");
            var upper = new Point(box.Left + box.Width / 2, box.Top + box.Height / 4);
            Require(ribbon.HitCode(upper) == 1, "Upper Orb is native client input");
            using var image = new Bitmap(ribbon.Width, ribbon.Height); ribbon.DrawToBitmap(image, ribbon.ClientRectangle);
            image.Save(Path.Combine(directory, $"orb-{mode}-{density}-{qat}.png")); scopes++;
        }
        log.Add($"PASS: {scopes} public-only native layout scopes at {ribbon.DeviceDpi} DPI: one File target, upper-half native hit test, retained field HWND/parent/text/selection, Classic/Simplified, Compact/Touch and all QAT placements.");
        ribbon.LayoutMode = RibbonLayoutMode.Classic; ribbon.Density = RibbonDensity.Compact; ribbon.QuickAccessPosition = RibbonQuickAccessPosition.TitleBar;
        ribbon.FileMenu.ShowDropDown(); var popup = Popup(); Require(ribbon.FileMenu.IsDropDownOpen, "File opens once"); var origin = popup.Location;
        pane.ShowDropDown(); Require(primary == 0, "Pane selection does not execute primary command");
        nint menuHandle = native.Handle; native.Select(1, 3); Require(ReferenceEquals(native.Parent, content), "Native menu content retains parent"); Require(popup.Location == origin, "Pane switch retains clamped origin");
        ribbon.FileMenu.CloseDropDown(); Require(!content.IsDisposed && native.Handle == menuHandle && native.SelectionStart == 1 && native.SelectionLength == 3, "Close retains borrowed native content");
        var shape = TypeDescriptor.GetProperties(ribbon)[nameof(ribbon.ApplicationButtonShape)]!;
        ribbon.ApplicationButtonShape = RibbonApplicationButtonShape.Rectangular; ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2019Light; ribbon.BuiltInTheme = null;
        Require(ribbon.EffectiveApplicationButtonShape == RibbonApplicationButtonShape.Rectangular, "Explicit rectangle wins"); shape.ResetValue(ribbon); Require(ribbon.EffectiveApplicationButtonShape == RibbonApplicationButtonShape.Orb, "Shape Reset resumes application theme");
        Require(ribbon.FileMenuMode == RibbonFileMenuMode.CommandMenu, "Shape Reset preserves route");
        ribbon.Focus(); Require(FilterKey(ribbon, Keys.F10) && FilterKey(ribbon, Keys.F), "F10/File KeyTip follows the existing keyboard route"); Require(ribbon.FileMenu.IsDropDownOpen, "KeyTip opens File"); ribbon.FileMenu.CloseDropDown();
        RunUiaChild(ribbon, directory, log);
        Require(primary == 0 && opening >= 3, "Keyboard/UIA share the declaration without primary split execution");
        log.Add("PASS: persistent open state, one File declaration, split pane/native-content HWND/parent/text/selection retention, stable anchor, shape override/Reset, F10/F KeyTip and external UIA Expand/Collapse.");
    }
    private static bool FilterKey(Control control, Keys key)
    {
        var message = Message.Create(control.Handle, 0x100, (nint)key, 0);
        return Application.FilterMessage(ref message) || control.PreProcessMessage(ref message);
    }
    private static void RunUiaChild(RibbonControl ribbon, string directory, List<string> log)
    {
        var output = Path.Combine(directory, "external-uia.log");
        var start = new ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden };
        foreach (string value in new[] { "--orb-uia-client", ribbon.Handle.ToInt64().ToString(System.Globalization.CultureInfo.InvariantCulture), ribbon.FileMenu.Id, output }) start.ArgumentList.Add(value);
        using var child = Process.Start(start)!; var watch = Stopwatch.StartNew();
        while (!child.HasExited && watch.Elapsed < TimeSpan.FromSeconds(20)) { Application.DoEvents(); Thread.Sleep(10); }
        if (!child.HasExited) { child.Kill(); throw new TimeoutException("External UIA client timed out."); }
        var result = File.Exists(output) ? File.ReadAllText(output) : "No UIA log"; log.Add(result.Trim()); Require(child.ExitCode == 0, result);
    }
    private static ToolStripDropDown Popup()
    {
        ToolStripDropDown? result = null;
        EnumThreadWindows(GetCurrentThreadId(), (window, _) => { if (Control.FromHandle(window) is ToolStripDropDown { Visible: true } popup) result = popup; return true; }, 0);
        return result ?? throw new InvalidOperationException("No native menu window.");
    }
    private sealed class Probe : RibbonControl
    {
        internal int HitCode(Point local) { var p = PointToScreen(local); var message = Message.Create(Handle, 0x84, 0, (nint)((p.X & 0xffff) | (p.Y << 16))); WndProc(ref message); return (int)message.Result; }
    }
    private static IEnumerable<Control> Children(Control root) => root.Controls.Cast<Control>().SelectMany(c => new[] { c }.Concat(Children(c)));
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private delegate bool WindowCallback(nint window, nint argument);
    [DllImport("user32.dll")] private static extern bool EnumThreadWindows(uint threadId, WindowCallback callback, nint argument);
    [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
#endif
}
