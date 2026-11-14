using System.Runtime.InteropServices;
using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

// Capture composed frames on a worker while the UI thread opens/closes its own
// choice popup. Reading only after DoEvents would miss the opening-time blank.
internal static class ComboBackdropPaintCheck
{
    internal static void Verify(string output)
    {
        Directory.CreateDirectory(output);
        using var log = new StreamWriter(Path.Combine(output, "result.log")) { AutoFlush = true };
        var failures = new List<Exception>();
        foreach (var backdrop in new[] { RibbonWindowBackdrop.Mica, RibbonWindowBackdrop.Acrylic, RibbonWindowBackdrop.None })
        {
            using var form = new DesignerForm { Backdrop = backdrop, StartPosition = FormStartPosition.Manual, Location = new(140, 120) };
            var ribbon = form.Ribbon!; ribbon.AnimationLevel = RibbonAnimationLevel.None;
            Exception? failure = null;
            form.Shown += async (_, _) =>
            {
                try
                {
                    await Task.Delay(300);
                    log.WriteLine($"Requested={backdrop}; Effective={form.EffectiveBackdrop}");
                    Check(form.EffectiveBackdrop == backdrop, "Requested backdrop is active: " + backdrop);
                    var item = ribbon.Tabs.SelectMany(t => t.Groups).SelectMany(g => g.Items).OfType<RibbonComboBoxItem>().Single(i => i.Text == "Font :");
                    var native = Descendants(ribbon).OfType<ComboBox>().Single(c => c.Name == item.Id);
                    var presenter = native.Parent!; nint owner = form.Handle;
                    item.SelectedIndex = 2; await Task.Delay(150);
                    Rectangle area;
                    using (var measure = native.CreateGraphics())
                        area = native.RectangleToScreen(Rectangle.Round(native.Region!.GetBounds(measure)));
                    var nativeArea = native.RectangleToScreen(native.ClientRectangle);
                    var arrowArea = Rectangle.FromLTRB(area.Right + 2, area.Top, nativeArea.Right - 2, area.Bottom);
                    var frames = new List<(Bitmap Image, int Ink, string Stage, string Target)>();
                    var popupGate = new object(); Rectangle[]? popupRows = null;
                    using var stop = new CancellationTokenSource();
                    string stage = "baseline";
                    Point originalPointer = Cursor.Position;
                    var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                    int skipped = 0;
                    form.Activate();
                    var capture = Task.Run(() =>
                    {
                        while (!stop.IsCancellationRequested)
                        {
                            DwmFlush();
                            if (GetForegroundWindow() != owner) { skipped++; Thread.Sleep(8); continue; }
                            Capture(area, "field");
                            Capture(arrowArea, "arrow");
                            lock (popupGate)
                                if (popupRows is { } rows)
                                    for (int row = 0; row < rows.Length; row++) Capture(rows[row], "row" + row);
                            Thread.Sleep(8);
                        }
                        void Capture(Rectangle bounds, string target)
                        {
                            var image = new Bitmap(bounds.Width, bounds.Height);
                            using (var graphics = Graphics.FromImage(image)) graphics.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
                            int ink = 0;
                            for (int y = 0; y < image.Height; y++) for (int x = 0; x < image.Width; x++)
                            { var p = image.GetPixel(x, y); if (p.R < 120 && p.G < 120 && p.B < 120) ink++; }
                            frames.Add((image, ink, Volatile.Read(ref stage), target));
                            ready.TrySetResult();
                        }
                    });
                    try
                    {
                        await ready.Task.WaitAsync(TimeSpan.FromSeconds(3));
                        await Task.Delay(150);
                        var point = native.Location + new Size(native.Width - 6, native.Height / 2);
                        nint position = (nint)(point.Y << 16 | point.X);
                        for (int cycle = 0; cycle < 6; cycle++)
                        {
                            Volatile.Write(ref stage, cycle + "-hover");
                            SendMessage(presenter.Handle, 0x200, 0, position); await Task.Delay(50);
                            Volatile.Write(ref stage, cycle + "-press");
                            SendMessage(presenter.Handle, 0x201, 1, position); await Task.Delay(50);
                            Volatile.Write(ref stage, cycle + "-open");
                            SendMessage(presenter.Handle, 0x202, 0, position); await Task.Delay(180);
                            Check(item.IsDropDownOpen, "Choice popup opened");
                            Volatile.Write(ref stage, cycle + "-body-hover");
                            Cursor.Position = native.PointToScreen(new(native.Width / 2, native.Height / 2)); await Task.Delay(80);
                            var list = FindList(item.Id + ".choices");
                            if (cycle == 0)
                            {
                                DwmFlush(); var bounds = list.RectangleToScreen(list.ClientRectangle);
                                using var popup = new Bitmap(bounds.Width, bounds.Height);
                                using (var graphics = Graphics.FromImage(popup)) graphics.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
                                popup.Save(Path.Combine(output, backdrop + "-full-popup.png"));
                            }
                            lock (popupGate) popupRows = Enumerable.Range(0, 3).Select(i => list.RectangleToScreen(list.GetItemRectangle(i))).ToArray();
                            for (int rowIndex = 0; rowIndex < 3; rowIndex++)
                            {
                                Volatile.Write(ref stage, cycle + "-row-hover-" + rowIndex);
                                var row = list.GetItemRectangle(rowIndex);
                                Cursor.Position = list.PointToScreen(new(row.Left + row.Width / 2, row.Top + row.Height / 2)); await Task.Delay(80);
                            }
                            Volatile.Write(ref stage, cycle + "-return-body");
                            Cursor.Position = native.PointToScreen(new(native.Width / 2, native.Height / 2)); await Task.Delay(80);
                            Volatile.Write(ref stage, cycle + "-close");
                            lock (popupGate) { popupRows = null; item.CloseDropDown(); }
                            await Task.Delay(100);
                        }
                    }
                    finally { stop.Cancel(); await capture; Cursor.Position = originalPointer; }
                    try
                    {
                        int baseline = frames.Where(f => f.Stage == "baseline" && f.Target == "field").Select(f => f.Ink).DefaultIfEmpty(0).Max();
                        int arrowBaseline = frames.Where(f => f.Stage == "baseline" && f.Target == "arrow").Select(f => f.Ink).DefaultIfEmpty(0).Max();
                        log.WriteLine($"Captured={frames.Count}; foregroundSkips={skipped}; baselineInk={baseline}; first={string.Join(",", frames.Take(5).Select(f => f.Target + ":" + f.Stage + ":" + f.Ink))}");
                        Check(baseline > 10, "Visible baseline text captured");
                        Check(arrowBaseline > 3, "Visible baseline arrow captured");
                        int blank = 0, black = 0;
                        for (int i = 0; i < frames.Count; i++)
                        {
                            var frame = frames[i];
                            bool empty = frame.Ink < (frame.Target == "field" ? baseline / 3 : frame.Target == "arrow" ? Math.Max(2, arrowBaseline / 3) : 10), dark = frame.Ink > frame.Image.Width * frame.Image.Height / 2;
                            if (empty) blank++; if (dark) black++;
                            log.WriteLine($"{backdrop} frame={i:000} target={frame.Target} stage={frame.Stage} ink={frame.Ink} blank={empty} black={dark}");
                            if (empty || dark || i % 12 == 0) frame.Image.Save(Path.Combine(output, $"{backdrop}-{i:000}-{frame.Target}-{frame.Stage}.png"));
                        }
                        log.WriteLine($"{backdrop}: frames={frames.Count}; baselineInk={baseline}; blank={blank}; black={black}; DPI={ribbon.DeviceDpi}");
                        Check(blank == 0 && black == 0, "Composed field flickered: " + backdrop);
                    }
                    finally { foreach (var frame in frames) frame.Image.Dispose(); }
                }
                catch (Exception error) { failure = error; }
                finally { form.Close(); }
            };
            Application.Run(form);
            if (failure is not null) { log.WriteLine("FAIL: " + failure.Message); failures.Add(failure); }
        }
        if (failures.Count > 0) throw new AggregateException(failures);
        log.WriteLine("PASS: composed-screen font field, arrow and popup rows remained drawn through repeated hover, press and popup open/close in Solid/Mica/Acrylic. Fixture-owned input; no physical input or mixed-monitor claim.");
    }
    private static IEnumerable<Control> Descendants(Control owner) => owner.Controls.Cast<Control>().SelectMany(c => new[] { c }.Concat(Descendants(c)));
    private static ListBox FindList(string name)
    {
        ListBox? found = null;
        EnumThreadWindows(GetCurrentThreadId(), (handle, _) =>
        {
            if (Control.FromHandle(handle) is { } window)
                found ??= Descendants(window).OfType<ListBox>().SingleOrDefault(c => c.Name == name);
            return true;
        }, 0);
        return found ?? throw new InvalidOperationException("Choice list not found: " + name);
    }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    [DllImport("dwmapi.dll")] private static extern int DwmFlush();
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern nint SendMessage(nint window, uint message, nint wParam, nint lParam);
    private delegate bool EnumWindow(nint handle, nint parameter);
    [DllImport("user32.dll")] private static extern bool EnumThreadWindows(uint thread, EnumWindow callback, nint parameter);
    [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
}
