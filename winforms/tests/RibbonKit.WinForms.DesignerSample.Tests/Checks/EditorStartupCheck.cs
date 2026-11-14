using System.Runtime.InteropServices;
using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

// Observe only fixture-owned HWNDs, including the first native show transaction.
internal static class EditorStartupCheck
{
    internal static void Verify(string output)
    {
        Directory.CreateDirectory(output);
        using var log = new StreamWriter(Path.Combine(output, "result.log")) { AutoFlush = true };
        using var background = new Form { StartPosition = FormStartPosition.Manual, Bounds = new(140, 100, 1180, 850),
            BackColor = Color.LightSteelBlue, FormBorderStyle = FormBorderStyle.None, ShowInTaskbar = false };
        Exception? failure = null;
        background.Shown += async (_, _) =>
        {
            try
            {
                foreach (var material in new[] { RibbonWindowBackdrop.None, RibbonWindowBackdrop.Mica, RibbonWindowBackdrop.Acrylic })
                {
                    using var form = new StartupForm(output, log, material);
                    form.Show(background); form.Activate();
                    for (int i = 0; i < 20; i++) { await Task.Delay(16); form.Observe("pump" + i); }
                    form.Finish(); form.Close();
                }
            }
            catch (Exception error) { failure = error; log.WriteLine("FAIL: " + error); }
            finally { background.Close(); }
        };
        Application.Run(background); if (failure is not null) throw failure;
    }
    private sealed class StartupForm : DesignerForm
    {
        private readonly string _output; private readonly StreamWriter _log;
        private readonly List<Probe> _probes = [];
        private RibbonControl? _ribbon; private bool _capturing; private int _frame, _blackFrames, _validFrames;
        internal StartupForm(string output, StreamWriter log, RibbonWindowBackdrop material)
        {
            _output = output; _log = log; Backdrop = material;
            Text = "Native editor startup check"; StartPosition = FormStartPosition.Manual; Location = new(165, 125);
        }
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e); _ribbon = Controls.OfType<RibbonControl>().Single();
            Watch(_ribbon);
        }
        private void Watch(Control control)
        {
            void Hook() { if (control.IsHandleCreated) _probes.Add(new Probe(control.Handle, this)); }
            if (control.IsHandleCreated) Hook(); else control.HandleCreated += (_, _) => Hook();
            control.ControlAdded += (_, e) => Watch(e.Control!);
            foreach (Control child in control.Controls) Watch(child);
        }
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (m.Msg is 0xF or 0x18 or 0x47) Observe("form-" + m.Msg.ToString("X"));
        }
        internal void Observe(string stage)
        {
            if (_capturing || _ribbon is not { IsHandleCreated: true, Visible: true } ribbon || !IsWindowVisible(Handle)) return;
            _capturing = true;
            try
            {
                if (GetForegroundWindow() != Handle) return;
                _ = DwmFlush(); var area = ribbon.RectangleToScreen(ribbon.ClientRectangle);
                using var image = new Bitmap(area.Width, area.Height);
                using (var graphics = Graphics.FromImage(image)) graphics.CopyFromScreen(area.Location, Point.Empty, area.Size);
                int S(int value) => (int)Math.Round(value * DeviceDpi / 96d);
                var body = image.GetPixel(image.Width - S(100), S(105));
                bool ready = body.R >= 245 && body.G >= 245 && body.B >= 245;
                int black = 0, total = 0;
                foreach (var combo in Descendants(ribbon).OfType<ComboBox>().Where(c => c.Visible && c.IsHandleCreated))
                {
                    var bounds = ribbon.RectangleToClient(combo.RectangleToScreen(combo.ClientRectangle));
                    bounds = Rectangle.Intersect(Rectangle.Inflate(bounds, -S(3), -S(3)), ribbon.ClientRectangle);
                    for (int y = bounds.Top; y < bounds.Bottom; y += 2)
                    for (int x = bounds.Left; x < bounds.Right; x += 2)
                    { var p = image.GetPixel(x, y); if (p.R < 35 && p.G < 35 && p.B < 35) black++; total++; }
                }
                int percent = total == 0 ? 0 : black * 100 / total;
                if (ready && total > 0) { _validFrames++; if (percent > 35) _blackFrames++; }
                image.Save(Path.Combine(_output, $"{Backdrop}-{_frame:000}.png"));
                _log.WriteLine($"{Backdrop} frame={_frame++:000} stage={stage} whiteBody={ready} blackField={percent}% samples={total}");
            }
            finally { _capturing = false; }
        }
        internal void Finish()
        {
            _log.WriteLine($"{(_blackFrames == 0 && _validFrames > 0 ? "PASS" : "FAIL")}: {Backdrop}; startup field observations={_validFrames}; black field frames={_blackFrames}; DPI={DeviceDpi}");
            if (_blackFrames > 0 || _validFrames == 0) throw new InvalidOperationException("A native field started black: " + Backdrop);
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) foreach (var probe in _probes) probe.ReleaseHandle();
            base.Dispose(disposing);
        }
    }
    private sealed class Probe : NativeWindow
    {
        private readonly StartupForm _form;
        internal Probe(nint handle, StartupForm form) { _form = form; AssignHandle(handle); }
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (m.Msg is 0xF or 0x18 or 0x47) _form.Observe("child-" + m.Msg.ToString("X"));
        }
    }
    private static IEnumerable<Control> Descendants(Control owner) => owner.Controls.Cast<Control>().SelectMany(child => new[] { child }.Concat(Descendants(child)));
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(nint window);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("dwmapi.dll")] private static extern int DwmFlush();
}
