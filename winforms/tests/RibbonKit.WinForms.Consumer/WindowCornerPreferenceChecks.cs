using System.ComponentModel;
using System.Runtime.InteropServices;
using RibbonKit.WinForms;
using WF = System.Windows.Forms;

namespace IndependentConsumer;

// Public package APIs only; no Showcase resources, designer helpers or internals.
internal static class WindowCornerPreferenceChecks
{
    internal static int Run(string directory)
    {
        Directory.CreateDirectory(directory); List<string> log = [];
        WF.Application.SetHighDpiMode(WF.HighDpiMode.PerMonitorV2); WF.Application.EnableVisualStyles();
        bool supported = OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000);
        log.Add($"OS={Environment.OSVersion.Version}; native corner support={supported}; API evidence only, no visual silhouette acceptance");
        try
        {
            foreach (var theme in Enum.GetValues<RibbonBuiltInTheme>())
            foreach (var backdrop in Enum.GetValues<RibbonWindowBackdrop>())
                Check(theme, backdrop);
            File.WriteAllLines(Path.Combine(directory, "result.log"), log); return 0;
        }
        catch (Exception error)
        { log.Add("FAIL: " + error); File.WriteAllLines(Path.Combine(directory, "result.log"), log); return 1; }

        void Check(RibbonBuiltInTheme theme, RibbonWindowBackdrop backdrop)
        {
            using var form = new Probe { Text = "Independent native corner consumer", Backdrop = backdrop,
                StartPosition = WF.FormStartPosition.Manual, Bounds = new(-16000, -16000, 1000, 650), ShowInTaskbar = false };
            using var ribbon = new RibbonControl { BuiltInTheme = theme, FileMenuMode = RibbonFileMenuMode.Backstage, AnimationLevel = RibbonAnimationLevel.None };
            ribbon.Tabs.Add(new RibbonTab { Text = "Home" }); ribbon.FileMenu.Visible = true;
            using var page = new WF.Panel(); var field = new WF.TextBox { Text = "Retained public consumer field", Bounds = new(30, 30, 300, 30) };
            page.Controls.Add(field); ribbon.Backstage.Items.Add(new RibbonBackstagePageItem { Text = "Info", Content = page });
            form.Controls.Add(ribbon); form.Ribbon = ribbon;
            Require(!form.IsHandleCreated, "Policy/association must not create the form handle");
            var expected = ribbon.Theme.WindowCornerPreference;
            Require(form.WindowCornerPreference is null && form.EffectiveWindowCornerPreference == expected, "Built-in default");
            form.Show(); WF.Application.DoEvents(); Verify(expected);
            log.Add($"{theme}/{backdrop}: actual DPI={form.DeviceDpi}; applied backdrop={form.EffectiveBackdrop}");
            var material = form.EffectiveBackdrop;
            ribbon.IsBackstageOpen = true; WF.Application.DoEvents();
            Require(ribbon.IsBackstageOpen && page.Visible, "Real native Backstage page opened");
            field.Focus(); field.Select(3, 7); nint fieldHandle = field.Handle, pageHandle = page.Handle, formHandle = form.Handle;
            var parent = page.Parent; int validations = 0; field.Validating += (_, _) => validations++;
            foreach (var value in Enum.GetValues<RibbonWindowCornerPreference>())
            {
                form.WindowCornerPreference = value; Verify(value);
                Require(formHandle == form.Handle && pageHandle == page.Handle && fieldHandle == field.Handle, "Corner update retained live handles");
                Require(ReferenceEquals(parent, page.Parent) && field.Focused && field.SelectionStart == 3 && field.SelectionLength == 7 && validations == 0, "Corner update retained parent, focus, selection and validation");
                Require(form.EffectiveBackdrop == material && form.Backdrop == backdrop, "Corner update retained backdrop");
                Require(form.Region is null && form.FormBorderStyle == WF.FormBorderStyle.Sizable && (form.Style & 0x000F0000) == 0x000F0000, "Resizable system frame retained");
            }
            Require(field.Text == "Retained public consumer field", "Native text retained");
            ribbon.IsBackstageOpen = false; WF.Application.DoEvents();
            var property = TypeDescriptor.GetProperties(form)[nameof(form.WindowCornerPreference)]!;
            property.ResetValue(form); Verify(expected);
            foreach (var builtIn in new[] { RibbonBuiltInTheme.Office2019Light, RibbonBuiltInTheme.Office2024Light })
            {
                ribbon.BuiltInTheme = builtIn;
                Verify(builtIn == RibbonBuiltInTheme.Office2019Light ? RibbonWindowCornerPreference.Square : RibbonWindowCornerPreference.SystemDefault);
            }
            var application = RibbonThemes.Office2024Light with { WindowCornerPreference = RibbonWindowCornerPreference.SmallRounded };
            ribbon.Theme = application; Verify(RibbonWindowCornerPreference.SmallRounded);
            form.WindowCornerPreference = RibbonWindowCornerPreference.Rounded;
            ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2019Light; Verify(RibbonWindowCornerPreference.Rounded);
            try { ribbon.Theme = application with { WindowCornerPreference = (RibbonWindowCornerPreference)99 }; throw new InvalidOperationException("Invalid theme accepted"); }
            catch (ArgumentOutOfRangeException) { }
            try { form.WindowCornerPreference = (RibbonWindowCornerPreference)99; throw new InvalidOperationException("Invalid override accepted"); }
            catch (ArgumentOutOfRangeException) { }
            Require(ribbon.BuiltInTheme == RibbonBuiltInTheme.Office2019Light && form.WindowCornerPreference == RibbonWindowCornerPreference.Rounded, "Invalid changes were atomic");
            property.ResetValue(form); Verify(RibbonWindowCornerPreference.Square);
            ribbon.BuiltInTheme = null; Require(ReferenceEquals(application, ribbon.Theme), "Application theme restored"); Verify(RibbonWindowCornerPreference.SmallRounded);
            form.Ribbon = null; Verify(RibbonWindowCornerPreference.SystemDefault);
            form.Ribbon = ribbon; Verify(RibbonWindowCornerPreference.SmallRounded);
            form.Size = new(1100, 700); Require(form.Width == 1100, "Native resize remains available");
            form.Rebuild(); WF.Application.DoEvents(); Verify(RibbonWindowCornerPreference.SmallRounded);
            Require(field.Text == "Retained public consumer field" && form.EffectiveBackdrop == material, "Recreated handle retained state/material");
            // Exercise the standard caption command route and Snap hover target;
            // this does not claim physical dragging or desktop Snap acceptance.
            var maxPoint = form.PointToScreen(new(form.ClientSize.Width - 69 * form.DeviceDpi / 96, 18 * form.DeviceDpi / 96));
            Require(SendMessage(form.Handle, 0x84, 0, Pack(maxPoint)) == 9, "Native HTMAXBUTTON Snap target");
            SendMessage(form.Handle, 0x112, 0xF030, 0); WF.Application.DoEvents();
            Require(form.WindowState == WF.FormWindowState.Maximized, "System maximize"); Verify(RibbonWindowCornerPreference.SmallRounded);
            Require(WF.Screen.FromHandle(form.Handle).WorkingArea.Contains(form.RectangleToScreen(form.ClientRectangle)), "Maximized client stays in work area");
            SendMessage(form.Handle, 0x112, 0xF120, 0); WF.Application.DoEvents();
            Require(form.WindowState == WF.FormWindowState.Normal, "System restore"); Verify(RibbonWindowCornerPreference.SmallRounded);
            SendMessage(form.Handle, 0x112, 0xF020, 0); WF.Application.DoEvents();
            Require(form.WindowState == WF.FormWindowState.Minimized, "System minimize");
            SendMessage(form.Handle, 0x112, 0xF120, 0); WF.Application.DoEvents();
            bool cancel = true; form.FormClosing += (_, e) => e.Cancel = cancel;
            SendMessage(form.Handle, 0x112, 0xF060, 0);
            Require(!form.IsDisposed, "Canceled system close retained form"); Verify(RibbonWindowCornerPreference.SmallRounded);
            cancel = false; form.ObserveDestruction = true;
            SendMessage(form.Handle, 0x112, 0xF060, 0);
            Require(form.IsDisposed, "System close");
            if (supported) Require(form.DestroyedCorner == 3, "Corner request retained through WM_DESTROY");
            log.Add($"PASS: {theme}/{backdrop}; defaults/all four choices/Reset/theme switch/validation/reassociation/recreation; native page retention; resize, HTMAXBUTTON, system maximize/restore/minimize/canceled close/final destruction");

            void Verify(RibbonWindowCornerPreference value)
            {
                Require(form.EffectiveWindowCornerPreference == value, "Resolved preference");
                if (supported) Require(Read(form.Handle) == (int)value, "DWM attribute 33 readback: " + value);
            }
        }
    }
    private sealed class Probe : RibbonForm
    {
        internal int Style => CreateParams.Style;
        internal bool ObserveDestruction;
        internal int DestroyedCorner = -1;
        internal void Rebuild() => RecreateHandle();
        protected override void WndProc(ref WF.Message message)
        {
            if (ObserveDestruction && message.Msg == 2 && OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000)) DestroyedCorner = Read(message.HWnd);
            base.WndProc(ref message);
        }
    }
    private static nint Pack(Point point) => (nint)((point.Y & 0xFFFF) << 16 | point.X & 0xFFFF);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static int Read(nint window)
    { Require(DwmGetWindowAttribute(window, 33, out int value, sizeof(int)) >= 0, "DWM readback available"); return value; }
    [DllImport("dwmapi.dll")] private static extern int DwmGetWindowAttribute(nint window, int attribute, out int value, int size);
    [DllImport("user32.dll")] private static extern nint SendMessage(nint window, int message, nint wParam, nint lParam);
}
