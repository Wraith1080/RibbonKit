namespace RibbonKit.WinForms.Showcase;

public partial class MainForm
{
    internal async Task CheckEditorHover(string directory)
    {
        const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        ribbon.SelectedTab = inputs;
        while (ribbon.IsAnimating) await Task.Delay(10);
        var editor = Descendants(ribbon).OfType<ComboBox>().Single(c => c.Name == fontInput.Id);
        var presenter = editor.Parent!;
        var move = presenter.GetType().GetMethod("OnMouseMove", flags)!;
        nint handle = editor.Handle, documentHandle = document.Handle; string text = document.Text, value = editor.Text;
        var selection = (document.SelectionStart, document.SelectionLength);
        var tipType = typeof(RibbonControl).Assembly.GetType("RibbonKit.WinForms.RibbonScreenTipWindow")!;
        var anchor = editor.RectangleToScreen(editor.ClientRectangle);
        using var tip = (Form)Activator.CreateInstance(tipType, flags, null, [ribbon, fontInput, anchor, new Point(anchor.Right, anchor.Bottom)], null)!;
        inputMenu.ShowDropDown(); tip.Show(this); tipType.GetMethod("RaiseAbovePopups", flags)!.Invoke(tip, null);
        await Task.Delay(120);
        var field = editor.RectangleToScreen(editor.ClientRectangle); field.Width = Math.Min(field.Width - 24, 130);
        using var baseline = Capture(field); int initialInk = Ink(baseline), leastInk = initialInk;
        var documentArea = document.RectangleToScreen(document.ClientRectangle);
        documentArea.Inflate(-8, -8); documentArea.Size = new(Math.Min(600, documentArea.Width), Math.Min(110, documentArea.Height));
        using var documentBaseline = Capture(documentArea); int documentInk = Ink(documentBaseline), leastDocumentInk = documentInk;
        if (initialInk < 12) throw new InvalidOperationException("ComboBox text was absent before hover capture.");
        var styles = _sampleGalleries[0]; var preview = typeof(RibbonGalleryItem).GetMethod("Preview", flags)!;
        var tiles = styles.Items.OfType<RibbonGalleryTile>().Take(3).ToArray();
        try
        {
            for (int frame = 0; frame < 36; frame++)
            {
                var point = frame % 3 == 0 ? Point.Empty : new Point(editor.Left + 4 + frame % 5, editor.Top + editor.Height / 2);
                move.Invoke(presenter, [new MouseEventArgs(MouseButtons.None, 0, point.X, point.Y, 0)]);
                preview.Invoke(styles, [tiles[frame % tiles.Length], null]);
                await Task.Delay(8);
                using var image = Capture(field); int ink = Ink(image); leastInk = Math.Min(leastInk, ink);
                using var documentImage = Capture(documentArea); int contentInk = Ink(documentImage); leastDocumentInk = Math.Min(leastDocumentInk, contentInk);
                if (frame is 0 or 35 || ink < initialInk * .7) image.Save(Path.Combine(directory, $"combo-{frame}.png"));
                if (ink < initialInk * .7) throw new InvalidOperationException($"ComboBox text disappeared during hover: ink={ink}, baseline={initialInk}, frame={frame}.");
                if (contentInk < documentInk * .6)
                { documentImage.Save(Path.Combine(directory, $"document-failed-{frame}.png")); throw new InvalidOperationException("Native document ink disappeared during style preview."); }
            }
        }
        finally { typeof(RibbonGalleryItem).GetMethod("CancelPreview", flags)!.Invoke(styles, [null]); inputMenu.CloseDropDown(); }
        if (editor.Handle != handle || editor.Text != value || document.Handle != documentHandle || document.Text != text || selection != (document.SelectionStart, document.SelectionLength))
            throw new InvalidOperationException("Native editor/document identity or content changed during hover.");
        File.AppendAllText(Path.Combine(directory, "result.log"), $"\r\nPASS: 36 ComboBox hover samples with passive ScreenTip, native editor popup and live document font previews; combo baseline ink={initialInk}, least={leastInk}; document baseline ink={documentInk}, least={leastDocumentInk}; HWNDs/text/selection retained.");
        static int Ink(Bitmap image)
        {
            int count = 0;
            for (int y = 0; y < image.Height; y++) for (int x = 0; x < image.Width; x++)
            { var p = image.GetPixel(x, y); if (p.R < 130 && p.G < 130 && p.B < 130) count++; }
            return count;
        }
        Bitmap Capture(Rectangle area)
        {
            GetWindowThreadProcessId(GetForegroundWindow(), out uint foreground);
            if (foreground != Environment.ProcessId) throw new InvalidOperationException("Showcase lost focus; editor hover capture stopped.");
            var image = new Bitmap(area.Width, area.Height);
            using var graphics = Graphics.FromImage(image); graphics.CopyFromScreen(area.Location, Point.Empty, image.Size); return image;
        }
    }
    internal static string PrepareBackdropStartupSettings(string directory, RibbonWindowBackdrop backdrop)
    {
        string settings = Path.Combine(directory, "isolated-preferences");
        if (Directory.Exists(settings)) throw new InvalidOperationException("The startup diagnostic needs a fresh directory.");
        Directory.CreateDirectory(settings);
        // Exercise the actual constructor restore and the saved-bound resize in
        // OnLoad, rather than enabling material after startup has already finished.
        File.WriteAllText(Path.Combine(settings, "preferences.json"), new Persistence.ShowcasePreferences
        {
            Backdrop = backdrop, Density = RibbonDensity.Touch, AnimationLevel = RibbonAnimationLevel.Expressive,
            TabContentAnimation = RibbonTabContentAnimation.SlideDown, TransparentBackstage = true,
            Window = new(311, 186, 1204, 842, 120, false)
        }.ToJson());
        return settings;
    }
    // Diagnostic only: sample actual startup pixels without Refresh or input.
    internal async Task CheckBackdropStartup(string directory)
    {
        Directory.CreateDirectory(directory);
        int worstDocument = 0, worstStatus = 0, unrevealed = 0;
        var timeout = System.Diagnostics.Stopwatch.StartNew();
        for (int frame = 0; frame < 16; frame++)
        {
            if (!ContainsFocus) throw new InvalidOperationException("Showcase lost focus; startup capture stopped.");
            using var image = new Bitmap(ClientSize.Width, ClientSize.Height);
            using (var graphics = Graphics.FromImage(image)) graphics.CopyFromScreen(PointToScreen(Point.Empty), Point.Empty, image.Size);
            int pink = 0, tested = 0;
            for (int y = 10; y < image.Height; y += 20) for (int x = 10; x < image.Width; x += 20)
            { var p = image.GetPixel(x, y); if (p.R > 240 && p.G < 15 && p.B > 240) pink++; tested++; }
            if (pink > tested * .8)
            {
                if (timeout.ElapsedMilliseconds > 2000) throw new InvalidOperationException("Startup remained unrevealed.");
                unrevealed++; frame--; await Task.Delay(8); continue;
            }
            int editor = Dark(document), strip = Dark(statusStrip);
            worstDocument = Math.Max(worstDocument, editor); worstStatus = Math.Max(worstStatus, strip);
            if (frame is 0 or 15 || editor > 25 || strip > 25) image.Save(Path.Combine(directory, $"startup-{frame}.png"));
            if (editor > 25 || strip > 25) throw new InvalidOperationException($"Black startup content: {Backdrop}; document={editor}%, status={strip}%.");
            await Task.Delay(8);

            int Dark(Control control)
            {
                var area = RectangleToClient(control.RectangleToScreen(control.ClientRectangle)); area.Inflate(-4, -4);
                int dark = 0, count = 0;
                for (int y = area.Top; y < area.Bottom; y += 3) for (int x = area.Left; x < area.Right; x += 3)
                { var p = image.GetPixel(x, y); if (p.R < 30 && p.G < 30 && p.B < 30) dark++; count++; }
                return count == 0 ? 0 : 100 * dark / count;
            }
        }
        File.WriteAllText(Path.Combine(directory, "result.log"), $"PASS: {Backdrop} requested/{EffectiveBackdrop} applied; 16 samples from first presented startup frame, no 100ms delay; unrevealed samples={unrevealed}; worst document dark={worstDocument}%, status={worstStatus}%.");
    }
    internal async Task CheckBackdropHover(string directory)
    {
        // Exercise our own mouse handlers, without injecting desktop input.
        // Screen pixels are checked separately from managed paint notifications.
        const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        nint editor = document.Handle; string text = document.Text;
        var moveRibbon = typeof(RibbonControl).GetMethod("OnMouseMove", flags)!;
        paste.ShowDropDown();
        while (ribbon.IsAnimating) await Task.Delay(10);
        await Task.Delay(60);
        var manager = typeof(RibbonControl).GetProperty("Popups", flags)!.GetValue(ribbon)!;
        var surface = (Control)manager.GetType().GetProperty("Top", flags)!.GetValue(manager)!;
        var movePopup = surface.GetType().GetMethod("OnMouseMove", flags)!;
        var area = RectangleToClient(document.RectangleToScreen(document.ClientRectangle)); area.Inflate(-8, -8);
        var covered = RectangleToClient(surface.RectangleToScreen(surface.ClientRectangle)); covered.Inflate(14, 14);
        var strip = RectangleToClient(statusStrip.RectangleToScreen(statusStrip.ClientRectangle)); strip.Inflate(-4, -4);
        using var baseline = Capture(); int worst = 0;
        for (int frame = 0; frame < 48; frame++)
        {
            Move(movePopup, surface, new(20 + frame % 4, frame % 2 == 0 ? 65 : surface.Height - 24));
            Move(moveRibbon, ribbon, new(frame % 2 == 0 ? ribbon.Width - 10 : ribbon.Width / 2, ribbon.Height - 15));
            await Task.Delay(8);
            using var current = Capture(); int changed = Differences(current); worst = Math.Max(worst, changed);
            if (frame is 0 or 47 || changed > 4) current.Save(Path.Combine(directory, $"hover-{frame}.png"));
            if (changed > 4) throw new InvalidOperationException($"Document/status drawing changed during popup/ribbon hover: {changed} sampled pixels, {Backdrop}, frame {frame}.");
        }
        paste.CloseDropDown();
        if (document.Handle != editor || document.Text != text) throw new InvalidOperationException("Hover check changed the native document handle or text.");
        File.AppendAllText(Path.Combine(directory, "result.log"), $"\r\nPASS: 48 popup/ribbon handler hover samples; worst document/status changed pixels={worst}; native document handle and text retained. Physical pointer review remains separate.");
        static void Move(System.Reflection.MethodInfo method, Control control, Point point) => method.Invoke(control, [new MouseEventArgs(MouseButtons.None, 0, point.X, point.Y, 0)]);
        Bitmap Capture()
        {
            GetWindowThreadProcessId(GetForegroundWindow(), out uint foreground);
            if (foreground != Environment.ProcessId) throw new InvalidOperationException("Showcase lost focus; hover capture stopped.");
            var image = new Bitmap(ClientSize.Width, ClientSize.Height);
            using var graphics = Graphics.FromImage(image); graphics.CopyFromScreen(PointToScreen(Point.Empty), Point.Empty, image.Size); return image;
        }
        int Differences(Bitmap current)
        {
            int count = 0;
            foreach (var region in new[] { area, strip })
                for (int y = region.Top; y < region.Bottom; y += 3) for (int x = region.Left; x < region.Right; x += 3)
                {
                    if (covered.Contains(x, y)) continue;
                    var a = baseline.GetPixel(x, y); var b = current.GetPixel(x, y);
                    if (Math.Abs(a.R - b.R) + Math.Abs(a.G - b.G) + Math.Abs(a.B - b.B) > 36) count++;
                }
            return count;
        }
    }
}
