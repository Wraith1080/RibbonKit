using System.Diagnostics;
using System.Runtime.InteropServices;

namespace RibbonKit.WinForms.Showcase;

public partial class MainForm
{
    internal async Task CaptureMotionInput(string directory, bool tailOnly = false, bool interactionsOnly = false, bool hoverOnly = false, bool openingOnly = false, bool opaqueSlideOnly = false)
    {
        Directory.CreateDirectory(directory); List<string> log = []; var pointer = Cursor.Position;
        try
        {
            Activate(); SetForegroundWindow(Handle); await Task.Delay(150); Guard(); Location = new(240, 160); Width = 1250; Height = 760;
            ribbon.AnimationLevel = RibbonAnimationLevel.None; ribbon.IsBackstageOpen = false; ribbon.IsMinimized = false; ribbon.SelectedTab = inputs; ribbon.Focus();
            Cursor.Position = new(Right + 15, Top + 100); await Task.Delay(80); ribbon.AnimationLevel = RibbonAnimationLevel.Expressive;
            if (opaqueSlideOnly)
            {
                foreach (var material in new[] { RibbonWindowBackdrop.Mica, RibbonWindowBackdrop.Acrylic })
                foreach (var state in new[] { FormWindowState.Normal, FormWindowState.Maximized })
                {
                    ribbon.AnimationLevel = RibbonAnimationLevel.None; ribbon.IsBackstageOpen = false;
                    Backdrop = material; ribbon.Backstage.NavigationBackgroundOpacity = 1;
                    WindowState = state; await Task.Delay(400); Guard();
                    Cursor.Position = new(Right - 4, Top + 15); ribbon.AnimationLevel = RibbonAnimationLevel.Expressive;
                    await Record($"{material}-{state}-opaque-open", () => { ribbon.IsBackstageOpen = true; return Task.CompletedTask; }, true);
                    await Record($"{material}-{state}-opaque-close", () => { ribbon.IsBackstageOpen = false; return Task.CompletedTask; }, true);
                }
                log.Add("PASS: complete opaque material slides captured"); return;
            }
            if (hoverOnly)
            {
                async Task Sweep(int y)
                {
                    for (int step = 0; step < 30; step++) { Guard(); Cursor.Position = new(Left + 24 + step * (Width - 280) / 30, y); await Task.Delay(8); }
                }
                await Record("document-hover-caption", () => Sweep(Top + 22), false);
                await Record("document-hover-tabs", () => Sweep(ribbon.PointToScreen(new(0, (int)Math.Round(55 * DeviceDpi / 96d))).Y), false);
                await Record("document-hover-tab-motion", async () => { ribbon.SelectedTab = home; await Sweep(Top + 22); }, false);
                await Record("document-hover-document", () => Sweep(document.PointToScreen(new(0, 70)).Y), false);
                ribbon.IsBackstageOpen = true; var opening = Stopwatch.StartNew(); while (ribbon.IsAnimating && opening.ElapsedMilliseconds < 1500) await Task.Delay(5);
                await Record("document-close", async () => { ribbon.IsBackstageOpen = false; await Sweep(Top + 22); }, false);
                log.Add("PASS: controlled pointer movement and document capture completed"); return;
            }
            if (interactionsOnly)
            {
                foreach (var density in Enum.GetValues<RibbonDensity>())
                {
                    ribbon.AnimationLevel = RibbonAnimationLevel.None; ribbon.Density = density; ribbon.LayoutMode = RibbonLayoutMode.Classic; ribbon.SelectedTab = inputs; Width = 1250; ribbon.Focus();
                    Cursor.Position = new(Right + 15, Top + 100); await Task.Delay(60); ribbon.AnimationLevel = RibbonAnimationLevel.Expressive;
                    if (openingOnly && Backdrop == RibbonWindowBackdrop.None)
                    {
                        Guard(); var doc = document.RectangleToScreen(document.ClientRectangle);
                        int top = Math.Max(doc.Top, ribbon.PointToScreen(new(0, ribbon.Height)).Y + 15);
                        var area = Rectangle.FromLTRB(doc.Left + 15, top, doc.Right - 25, doc.Bottom - 15);
                        using var before = new Bitmap(area.Width, area.Height);
                        using (var graphics = Graphics.FromImage(before)) graphics.CopyFromScreen(area.Location, Point.Empty, area.Size);
                        ribbon.IsBackstageOpen = true; FlushMotionCompositor();
                        using var opening = new Bitmap(area.Width, area.Height);
                        using (var graphics = Graphics.FromImage(opening)) graphics.CopyFromScreen(area.Location, Point.Empty, area.Size);
                        before.Save(Path.Combine(directory, density + "-richtext-before.png")); opening.Save(Path.Combine(directory, density + "-richtext-opening.png"));
                        int changed = Changed(before, opening, new(Point.Empty, area.Size));
                        log.Add($"{density}: initial RichTextBox changed pixels={changed}");
                        if (changed > area.Width * area.Height / 400) throw new InvalidOperationException("RichTextBox drawing flickered before the Solid fade began.");
                        ribbon.AnimationLevel = RibbonAnimationLevel.None; ribbon.IsBackstageOpen = false; await Task.Delay(60); ribbon.AnimationLevel = RibbonAnimationLevel.Expressive;
                    }
                    await Record(density + "-hover-open", async () =>
                    {
                        ribbon.IsBackstageOpen = true; await Task.Delay(55); Guard(); var back = Descendants(this).First(c => c.Visible && c.AccessibleName == "Back");
                        Cursor.Position = back.PointToScreen(new(back.Width / 2, back.Height / 2)); await Task.Delay(35);
                    }, true);
                    if (openingOnly)
                    {
                        ribbon.AnimationLevel = RibbonAnimationLevel.None; ribbon.IsBackstageOpen = false;
                        continue;
                    }
                    var backButton = Descendants(this).First(c => c.Visible && c.AccessibleName == "Back");
                    await Record(density + "-pointer-close", () => Click(backButton), false);
                    var field = Descendants(ribbon).OfType<TextBox>().Single(c => c.Visible && c.Name == nameInput.Id);
                    var choice = Descendants(ribbon).OfType<ComboBox>().Single(c => c.Visible && c.Name == fontInput.Id);
                    nint fieldHandle = field.Handle, choiceHandle = choice.Handle; string oldName = nameInput.Value; int oldFont = fontInput.SelectedIndex;
                    await Record(density + "-type-name", async () => { await Click(field); Guard(); SendKeys.SendWait("^aMotion check{ENTER}"); }, false);
                    if (nameInput.Value != "Motion check") throw new InvalidOperationException("Native ribbon TextBox did not commit input.");
                    await Record(density + "-choose-font", async () => { await Click(choice); Guard(); SendKeys.SendWait("{F4}{HOME}{DOWN}{ENTER}"); }, false);
                    if (fontInput.SelectedIndex != 1) throw new InvalidOperationException("Native ribbon ComboBox did not commit its choice.");
                    ribbon.Focus();
                    await Record(density + "-drag-narrow", async () => { var edge = new Point(Right - 2, Top + Height / 2); await Drag(edge, new(edge.X - 600, edge.Y)); }, false);
                    await Record(density + "-drag-wide", async () => { var edge = new Point(Right - 2, Top + Height / 2); await Drag(edge, new(edge.X + 600, edge.Y)); }, false);
                    if (field.Handle != fieldHandle || choice.Handle != choiceHandle || nameInput.Value != "Motion check" || fontInput.SelectedIndex != 1)
                        throw new InvalidOperationException("Physical resize lost native editor handles or values.");
                    nameInput.Value = oldName; fontInput.SelectedIndex = oldFont;
                    log.Add($"PASS: {density} hover interruption, pointer Back, native typing/choice and physical resize retained handles and data");
                }
                log.Add("PASS: unattended interaction capture completed"); return;
            }
            for (int i = 0; i < (tailOnly ? 0 : 3); i++)
            {
                await Record("keys-open-" + i, async () => { Guard(); SendKeys.SendWait("{F10}"); await Task.Delay(20); Guard(); SendKeys.SendWait("f"); }, true);
                await Record("keys-close-" + i, () => { Guard(); SendKeys.SendWait("{ESC}"); return Task.CompletedTask; }, false);
            }
            await Record("inputs-to-home", () => { ribbon.SelectedTab = home; return Task.CompletedTask; }, false);
            await Record("home-to-inputs", () => { ribbon.SelectedTab = inputs; return Task.CompletedTask; }, false);
            await Record("minimize", () => { Guard(); SendKeys.SendWait("^{F1}"); return Task.CompletedTask; }, false);
            await Record("restore", () => { Guard(); SendKeys.SendWait("^{F1}"); return Task.CompletedTask; }, false);
            if (tailOnly)
            {
                ribbon.IsBackstageOpen = true;
                var opening = Stopwatch.StartNew(); while (ribbon.IsAnimating && opening.ElapsedMilliseconds < 1500) await Task.Delay(5);
                await Record("document-close", () => { ribbon.IsBackstageOpen = false; return Task.CompletedTask; }, false);
            }
            log.Add("PASS: full-window input capture completed");
        }
        catch (Exception error) { log.Add("FAIL: " + error); Environment.ExitCode = 1; }
        finally { Cursor.Position = pointer; File.WriteAllLines(Path.Combine(directory, "result.log"), log); }

        void Guard()
        {
            GetWindowThreadProcessId(GetForegroundWindow(), out uint process);
            GetWindowThreadProcessId(MotionWindowFromPoint(new(Left + 40, Top + 20)), out uint visibleProcess);
            if (process != Environment.ProcessId || visibleProcess != Environment.ProcessId || !TraceVisible(Handle))
                throw new InvalidOperationException("Showcase is not visibly foreground; input/capture stopped.");
        }
        async Task Click(Control control)
        {
            Guard(); Cursor.Position = control.PointToScreen(new(control.Width / 2, control.Height / 2)); await Task.Delay(25); Guard(); DensityCaptureMouse(2, 0, 0, 0, 0); await Task.Delay(35); DensityCaptureMouse(4, 0, 0, 0, 0); await Task.Delay(60);
        }
        async Task Drag(Point from, Point to)
        {
            Guard(); Cursor.Position = from; await Task.Delay(20); DensityCaptureMouse(2, 0, 0, 0, 0); await Task.Delay(35);
            try { for (int step = 1; step <= 10; step++) { Guard(); Cursor.Position = new(from.X + (to.X - from.X) * step / 10, from.Y + (to.Y - from.Y) * step / 10); await Task.Delay(18); } }
            finally { DensityCaptureMouse(4, 0, 0, 0, 0); }
            await Task.Delay(45);
        }
        async Task Record(string name, Func<Task> action, bool inspectBackstage)
        {
            Guard(); var bounds = Rectangle.Intersect(Bounds, SystemInformation.VirtualScreen); var images = new List<(long Milliseconds, bool Animating, Bitmap Image)>();
            long committed = 0; int oldRibbonBottom = ribbon.PointToScreen(new(0, ribbon.Height)).Y - bounds.Y;
            Exception? error = null; using var stop = new CancellationTokenSource(); var watch = Stopwatch.StartNew();
            var worker = new Thread(() =>
            {
                try
                {
                    while (!stop.IsCancellationRequested && images.Count < 48)
                    {
                        Guard(); var image = new Bitmap(bounds.Width, bounds.Height);
                        using (var g = Graphics.FromImage(image)) g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
                        images.Add((watch.ElapsedMilliseconds, ribbon.IsAnimating, image)); Thread.Sleep(4);
                    }
                }
                catch (Exception exception) { error = exception; }
            });
            worker.Start();
            try
            {
                await Task.Delay(20); await action(); committed = watch.ElapsedMilliseconds; await Task.Delay(450);
            }
            finally { stop.Cancel(); worker.Join(); }
            try
            {
                if (error is not null) throw error;
                int blackStrips = 0;
                foreach (var frame in images)
                {
                    frame.Image.Save(Path.Combine(directory, $"{name}-{frame.Milliseconds:D4}.png"));
                    if (inspectBackstage)
                    {
                        // Sample the compositor independently of the UI timer.
                        // A tall dark strip is never page content or a soft shadow.
                        int darkColumns = 0;
                        for (int x = 8; x < frame.Image.Width - 8; x += 3)
                        {
                            int dark = 0, samples = 0;
                            for (int y = 120; y < frame.Image.Height - 50; y += 8)
                            { samples++; var pixel = frame.Image.GetPixel(x, y); if (pixel.R < 25 && pixel.G < 25 && pixel.B < 25) dark++; }
                            if (samples > 0 && dark > samples * .7) darkColumns++;
                        }
                        if (darkColumns > 2) blackStrips++;
                        if (opaqueSlideOnly)
                        {
                            // Opaque-layer artifacts can also be short horizontal
                            // bars, which the tall-column check does not detect.
                            int top = Math.Max(12, ribbon.PointToScreen(Point.Empty).Y - bounds.Y + 4);
                            bool horizontal = false;
                            for (int y = top; y < frame.Image.Height - 12 && !horizontal; y += 3)
                            {
                                int run = 0;
                                for (int x = 12; x < frame.Image.Width - 12; x += 3)
                                {
                                    var pixel = frame.Image.GetPixel(x, y);
                                    run = pixel.R < 25 && pixel.G < 25 && pixel.B < 25 ? run + 3 : 0;
                                    if (run > 120) { horizontal = true; break; }
                                }
                            }
                            if (horizontal) blackStrips++;
                        }
                    }
                }
                int documentChanges = 0, editorChanges = 0;
                if (name.StartsWith("document-hover", StringComparison.Ordinal) || name.Contains("-to-", StringComparison.Ordinal) || name is "minimize" or "restore" or "document-close")
                {
                    var doc = document.RectangleToScreen(document.ClientRectangle); doc.Offset(-bounds.X, -bounds.Y);
                    int ribbonBottom = ribbon.PointToScreen(new(0, ribbon.Height)).Y - bounds.Y;
                    doc = Rectangle.FromLTRB(doc.Left + 8, Math.Max(doc.Top, Math.Max(oldRibbonBottom, ribbonBottom)) + 25, doc.Right - 30, doc.Bottom - 25);
                    long compareAfter = name.StartsWith("document-hover", StringComparison.Ordinal) ? 20 : committed + 35;
                    documentChanges = images.Where(f => f.Milliseconds >= compareAfter && (name != "document-close" || !f.Animating)).Select(f => Changed(f.Image, images[^1].Image, doc)).DefaultIfEmpty().Max();
                    var editorBounds = Descendants(ribbon).Where(c => c.Visible && (c.Name == nameInput.Id || c.Name == fontInput.Id) && c is TextBoxBase or ComboBox).Select(c => { var r = c.RectangleToScreen(c.ClientRectangle); r.Offset(-bounds.X, -bounds.Y); return r; }).ToArray();
                    editorChanges = images.Where(f => f.Milliseconds >= committed + 35 && !f.Animating).Select(f => editorBounds.Sum(r => Changed(f.Image, images[^1].Image, r))).DefaultIfEmpty().Max();
                }
                log.Add($"{name}: frames={images.Count}, active frames={images.Count(f => f.Animating)}, black strips={blackStrips}, document changes={documentChanges}, settled editor changes={editorChanges}");
                File.WriteAllLines(Path.Combine(directory, "progress.log"), log);
                if (blackStrips > 0) throw new InvalidOperationException("A tall black strip appeared beside the moving Backstage page.");
                if (documentChanges > 4 || editorChanges > 4) throw new InvalidOperationException("Document or native editor pixels flashed outside the intended animation.");
            }
            finally { foreach (var frame in images) frame.Image.Dispose(); }
        }
    }

    private static IEnumerable<Control> Descendants(Control parent) => parent.Controls.Cast<Control>().SelectMany(c => new[] { c }.Concat(Descendants(c)));
    private static int Changed(Bitmap first, Bitmap last, Rectangle area)
    {
        if (ReferenceEquals(first, last)) return 0;
        area = Rectangle.Intersect(area, new(Point.Empty, first.Size)); if (area.Width <= 0 || area.Height <= 0) return 0;
        var a = first.LockBits(new(Point.Empty, first.Size), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        var b = last.LockBits(new(Point.Empty, last.Size), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        try
        {
            int count = 0; int[] left = new int[area.Width], right = new int[area.Width];
            for (int y = area.Top; y < area.Bottom; y += 2)
            {
                Marshal.Copy(a.Scan0 + y * a.Stride + area.Left * 4, left, 0, left.Length); Marshal.Copy(b.Scan0 + y * b.Stride + area.Left * 4, right, 0, right.Length);
                for (int x = 0; x < left.Length; x += 2)
                {
                    // The alpha-1 input hold can leave a one-level RGB tint for
                    // one compositor frame; count visible redraw/geometry changes.
                    int av = left[x], bv = right[x];
                    if (Math.Abs((av & 255) - (bv & 255)) > 12 || Math.Abs(((av >> 8) & 255) - ((bv >> 8) & 255)) > 12 || Math.Abs(((av >> 16) & 255) - ((bv >> 16) & 255)) > 12) count++;
                }
            }
            return count;
        }
        finally { first.UnlockBits(a); last.UnlockBits(b); }
    }

    // Diagnostic capture of the actual Showcase pages; no replacement page tree.
    internal async Task CaptureBackstageOpening(string directory)
    {
        foreach (var backdrop in new[] { RibbonWindowBackdrop.None, RibbonWindowBackdrop.Mica, RibbonWindowBackdrop.Acrylic })
        foreach (var opacity in backdrop == RibbonWindowBackdrop.None ? new[] { 1f } : new[] { 0f, 1f })
        {
            ribbon.AnimationLevel = RibbonAnimationLevel.None; ribbon.IsBackstageOpen = false;
            Backdrop = backdrop; ribbon.Backstage.NavigationBackgroundOpacity = opacity;
            await CaptureMotionInput(Path.Combine(directory, $"{backdrop}-{opacity}"), interactionsOnly: true, openingOnly: true);
            if (Environment.ExitCode != 0) return;
        }
    }
    internal async Task CaptureBackstageMotion(string directory)
    {
        Directory.CreateDirectory(directory); List<string> log = []; var cursor = Cursor.Position;
        try
        {
            Activate(); await Task.Delay(120); Guard(); Cursor.Position = new(Right + 10, Top + 100);
            var pages = ribbon.Backstage.Items.OfType<RibbonBackstagePageItem>().ToArray();
            var fields = pages.SelectMany(p => p.Content!.Controls.OfType<TextBox>()).ToArray();
            log.Add($"Actual Showcase Info/Print pages; native TextBoxes={fields.Length}; DPI={DeviceDpi}");
            foreach (var density in Enum.GetValues<RibbonDensity>())
            {
                ribbon.AnimationLevel = RibbonAnimationLevel.None; ribbon.IsBackstageOpen = false; ribbon.Density = density;
                ribbon.Backstage.SelectedPage = pages[0]; ribbon.AnimationLevel = RibbonAnimationLevel.Expressive;
                ribbon.IsBackstageOpen = true; await Frames(density + "-opening", pages[0].Content!);
                // Establish identity after the real page has inherited its first
                // binding context and DPI, without forcing unparented HWNDs.
                var state = fields.Select(f => (Field: f, Handle: f.Handle, f.Text, f.SelectionStart, f.SelectionLength)).ToArray();
                for (int i = 0; i < 4; i++)
                {
                    var page = pages[(i + 1) % pages.Length]; ribbon.Backstage.SelectedPage = page;
                    await Frames($"{density}-{i}-{page.Text}", page.Content!);
                }
                foreach (var retained in state)
                    if (retained.Field.Handle != retained.Handle || retained.Field.Text != retained.Text || retained.Field.SelectionStart != retained.SelectionStart || retained.Field.SelectionLength != retained.SelectionLength)
                        throw new InvalidOperationException("A real Showcase TextBox lost its handle, text or selection.");
                log.Add($"PASS: {density} actual page TextBox handles, text and selections retained");
            }
            ribbon.IsBackstageOpen = false; await Finish(); log.Add("PASS: actual Showcase Backstage fade capture completed");
        }
        catch (Exception error) { log.Add("FAIL: " + error); Environment.ExitCode = 1; }
        finally { File.WriteAllLines(Path.Combine(directory, "result.log"), log); Cursor.Position = cursor; }

        void Guard() { GetWindowThreadProcessId(GetForegroundWindow(), out uint process); if (process != Environment.ProcessId) throw new InvalidOperationException("Showcase lost foreground; capture stopped."); }
        async Task Finish() { var watch = Stopwatch.StartNew(); while (ribbon.IsAnimating && watch.ElapsedMilliseconds < 1500) await Task.Delay(5); if (ribbon.IsAnimating) throw new InvalidOperationException("Backstage motion did not complete."); }
        async Task Frames(string name, Control page)
        {
            var bounds = page.RectangleToScreen(page.ClientRectangle); var frames = new List<(bool Active, Bitmap Image)>();
            try
            {
                var watch = Stopwatch.StartNew();
                while (watch.ElapsedMilliseconds < 440)
                {
                    Guard(); var image = new Bitmap(bounds.Width, bounds.Height); using (var g = Graphics.FromImage(image)) g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
                    frames.Add((ribbon.IsAnimating, image)); await Task.Delay(8);
                }
                var settled = frames.Where(f => !f.Active).Select(f => f.Image).ToArray();
                if (settled.Length < 2) throw new InvalidOperationException("No settled Backstage frames.");
                int changed = settled.Max(frame => Differences(settled[^1], frame));
                log.Add($"{name}: settled page changed pixels={changed}");
                for (int i = 0; i < frames.Count; i++) frames[i].Image.Save(Path.Combine(directory, $"{name}-{i:D2}-{frames[i].Active}.png"));
                if (changed != 0) throw new InvalidOperationException("Actual Showcase page flickered after fade completion.");
            }
            finally { foreach (var frame in frames) frame.Image.Dispose(); }
        }
        static int Differences(Bitmap first, Bitmap last)
        {
            int changed = 0;
            for (int y = 0; y < first.Height; y++) for (int x = 0; x < first.Width; x++) if (first.GetPixel(x, y) != last.GetPixel(x, y)) changed++;
            return changed;
        }
    }
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(nint window);
    [DllImport("user32.dll", EntryPoint = "WindowFromPoint")] private static extern nint MotionWindowFromPoint(Point point);
    [DllImport("dwmapi.dll", EntryPoint = "DwmFlush")] private static extern int FlushMotionCompositor();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint window, out uint process);
}
