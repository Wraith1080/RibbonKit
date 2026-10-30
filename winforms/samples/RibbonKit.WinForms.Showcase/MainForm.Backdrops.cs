using System.Diagnostics;

namespace RibbonKit.WinForms.Showcase;

public partial class MainForm
{
    internal async Task CheckBackdropOff(string directory)
    {
        Directory.CreateDirectory(directory); List<string> log = [];
        foreach (var material in new[] { RibbonWindowBackdrop.Mica, RibbonWindowBackdrop.Acrylic })
        {
            Backdrop = material; await Task.Delay(250); Backdrop = RibbonWindowBackdrop.None;
            int worst = 0;
            for (int frame = 0; frame < 16; frame++)
            {
                if (!ContainsFocus) throw new InvalidOperationException("Showcase lost focus; backdrop-off capture stopped.");
                using var image = new Bitmap(ClientSize.Width, ClientSize.Height);
                using (var graphics = Graphics.FromImage(image)) graphics.CopyFromScreen(PointToScreen(Point.Empty), Point.Empty, image.Size);
                int black = 0, samples = 0;
                for (int y = 4; y < image.Height - 4; y += 3) for (int x = 4; x < image.Width - 4; x += 3)
                { var pixel = image.GetPixel(x, y); if (pixel.R < 30 && pixel.G < 30 && pixel.B < 30) black++; samples++; }
                int percent = 100 * black / samples; worst = Math.Max(worst, percent);
                if (frame is 0 or 15 || percent >= 25) image.Save(Path.Combine(directory, $"{material}-off-{frame}.png"));
                if (percent >= 25) throw new InvalidOperationException($"Backdrop-off frame was substantially black: {material}/{frame}, {percent}%.");
                await Task.Delay(8);
            }
            log.Add($"PASS: {material} to Solid; 16 client samples, worst dark coverage {worst}%");
        }
        File.WriteAllLines(Path.Combine(directory, "result.log"), log);
    }
    internal async Task CheckTransparentBackstage(string directory)
    {
        Directory.CreateDirectory(directory); List<string> log = [];
        Backdrop = RibbonWindowBackdrop.Acrylic; ribbon.Backstage.NavigationBackgroundOpacity = 0;
        foreach (var page in ribbon.Backstage.Items.OfType<RibbonBackstagePageItem>())
        {
            ribbon.IsBackstageOpen = true; ribbon.Backstage.SelectedPage = page;
            var watch = Stopwatch.StartNew();
            while (ribbon.IsAnimating && watch.ElapsedMilliseconds < 2000) await Task.Delay(10);
            await Task.Delay(80);
            if (!ContainsFocus) throw new InvalidOperationException("Showcase lost focus; capture stopped.");
            var area = page.Content!.RectangleToScreen(page.Content.ClientRectangle);
            using var bitmap = new Bitmap(area.Width, area.Height);
            using (var graphics = Graphics.FromImage(bitmap)) graphics.CopyFromScreen(area.Location, Point.Empty, area.Size);
            bitmap.Save(Path.Combine(directory, page.Id + ".png"));
            int ink = 0;
            for (int y = 8; y < Math.Min(bitmap.Height, 380); y++) for (int x = 8; x < bitmap.Width - 8; x++)
            { var pixel = bitmap.GetPixel(x, y); if (pixel.R < 110 && pixel.G < 110 && pixel.B < 110) ink++; }
            log.Add($"{page.Text}: ink={ink}; dpi={DeviceDpi}");
            if (ink < 200) { File.WriteAllLines(Path.Combine(directory, "result.log"), log); throw new InvalidOperationException("Backstage native page was blank: " + page.Text); }
        }
        ribbon.IsBackstageOpen = false;
        while (ribbon.IsAnimating) await Task.Delay(10);
        var view = ribbon.Tabs.Single(tab => tab.Id == "tab.merging");
        var launch = view.Groups.Single(group => group.Id == "group.modalPreview").Items.Single();
        foreach (var material in new[] { RibbonWindowBackdrop.None, RibbonWindowBackdrop.Acrylic })
        {
            Backdrop = material; ribbon.SelectedTab = view; await Task.Delay(400);
            using var before = Edge(); launch.PerformClick();
            for (int frame = 0; frame < 16; frame++)
            {
                await Task.Delay(16);
                if (!ContainsFocus) throw new InvalidOperationException("Showcase lost focus; modal capture stopped.");
                using var current = Edge(); int changed = 0;
                for (int y = 0; y < current.Height; y++) for (int x = 0; x < current.Width; x++)
                { var a = before.GetPixel(x, y); var b = current.GetPixel(x, y); if (Math.Abs(a.R - b.R) + Math.Abs(a.G - b.G) + Math.Abs(a.B - b.B) > 36) changed++; }
                if (changed > current.Width * current.Height * .7) { current.Save(Path.Combine(directory, $"{material}-edge-failed.png")); throw new InvalidOperationException("Modal entry changed the top client strip: " + material); }
            }
            if (!ribbon.IsModal) throw new InvalidOperationException("Print Preview did not enter modal mode.");
            log.Add($"PASS: {material} Print Preview entry; 16 top-edge samples without a wide bar");
            ribbon.ExitModal(); while (ribbon.IsAnimating) await Task.Delay(10);
        }
        log.Add("PASS: actual Showcase pages and modal checks completed without Refresh or mouse input");
        File.WriteAllLines(Path.Combine(directory, "result.log"), log);
        Bitmap Edge()
        {
            var image = new Bitmap(Math.Max(1, ClientSize.Width - 100), 4);
            using var graphics = Graphics.FromImage(image); graphics.CopyFromScreen(PointToScreen(new(50, 2)), Point.Empty, image.Size); return image;
        }
    }
    internal async Task CaptureBackdropCaption(string directory)
    {
        Directory.CreateDirectory(directory); Activate(); SetForegroundWindow(Handle);
        Location = new(240, 160); Size = new(1250, 760);
        foreach (var density in Enum.GetValues<RibbonDensity>())
        foreach (var material in Enum.GetValues<RibbonWindowBackdrop>())
        {
            ribbon.Density = density; Backdrop = material;
            foreach (var state in new[] { FormWindowState.Normal, FormWindowState.Maximized })
            {
                WindowState = state; await Task.Delay(450);
                GetWindowThreadProcessId(GetForegroundWindow(), out uint foreground);
                if (foreground != Environment.ProcessId) throw new InvalidOperationException("Showcase lost foreground; caption capture stopped.");
                using var frame = new Bitmap(Width, Math.Min(Height, (int)Math.Ceiling(80 * DeviceDpi / 96d)));
                using (var graphics = Graphics.FromImage(frame)) graphics.CopyFromScreen(Location, Point.Empty, frame.Size);
                frame.Save(Path.Combine(directory, $"{density}-{material}-{state}.png"));
                if (state == FormWindowState.Normal)
                {
                    using var body = new Bitmap(Width, Height);
                    using (var graphics = Graphics.FromImage(body)) graphics.CopyFromScreen(Location, Point.Empty, body.Size);
                    body.Save(Path.Combine(directory, $"{density}-{material}-gallery.png"));
                }
            }
            WindowState = FormWindowState.Normal;
        }
        File.WriteAllText(Path.Combine(directory, "result.log"), "PASS: caption capture completed across densities, materials and normal/maximized states; pixel review is separate.");
    }
    private void InitializeBackdrops()
    {
        // Application-owned GDI content uses the library's opaque host. The
        // surrounding transparent margins expose the full-form native material.
        var content = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        var opaque = new RibbonBackdropPanel { Dock = DockStyle.Fill, BackColor = SystemColors.Control };
        statusStrip.BackColor = SystemColors.Control; status.BackColor = Color.Empty;
        Controls.Remove(document); Controls.Remove(statusStrip); opaque.Controls.Add(document); opaque.Controls.Add(statusStrip);
        if (_printPreview is { } preview)
        {
            // Preview shares the document's opaque client host. Even its first
            // visible/layout frame must stay below the ribbon and caption.
            Controls.Remove(preview); opaque.Controls.Add(preview); opaque.Controls.SetChildIndex(preview, 0);
        }
        content.Controls.Add(opaque); Controls.Add(content); Controls.SetChildIndex(content, 0);
        BackdropChanged += (_, _) => content.Padding = Backdrop == RibbonWindowBackdrop.None ? Padding.Empty : new Padding(12);
        var group = new RibbonGroup { Id = "group.window", Text = "Window" };
        var menu = new RibbonDropDownItem { Id = "demo.window.backdrop", Text = "Backdrop", KeyTip = "B", DropDownHeader = "Window backdrop", SimplifiedPlacement = RibbonSimplifiedPlacement.Inline,
            ScreenTipDescription = "Choose Solid, Mica or Acrylic for the window and ribbon. Native fields and the document stay on opaque surfaces. Windows may use a solid fallback." };
        Dictionary<RibbonWindowBackdrop, RibbonRadioButtonItem> choices = [];
        var transparent = new RibbonCheckBoxItem { Id = "demo.window.backdrop.transparentBackstage", Text = "Transparent Backstage",
            ScreenTipDescription = "Use a fully transparent Backstage navigation background. Page content stays opaque." };
        transparent.Click += (_, _) => ribbon.Backstage.NavigationBackgroundOpacity = transparent.Checked ? 0 : 1;
        void Sync()
        {
            foreach (var (value, item) in choices) item.Checked = Backdrop == value;
            transparent.Enabled = EffectiveBackdrop != RibbonWindowBackdrop.None;
            transparent.Checked = ribbon.Backstage.NavigationBackgroundOpacity < 1;
        }
        foreach (var value in Enum.GetValues<RibbonWindowBackdrop>())
        {
            var item = new RibbonRadioButtonItem { Id = "demo.window.backdrop." + value, Text = value == RibbonWindowBackdrop.None ? "Solid" : value.ToString(), GroupName = "showcase.window.backdrop" };
            item.Click += (_, _) =>
            {
                Backdrop = value; Sync();
                status.Text = value == RibbonWindowBackdrop.None ? "Solid window" : EffectiveBackdrop == value ? value + " window backdrop" : value + " requested; solid fallback is active";
            };
            choices.Add(value, item); menu.Items.Add(item);
        }
        menu.Items.AddRange(new RibbonMenuSeparatorItem(), transparent);
        BackdropChanged += (_, _) => Sync();
        menu.DropDownOpening += (_, _) => Sync(); group.Items.Add(menu); ribbon.Tabs.Single(t => t.Id == "tab.merging").Groups.Add(group); Sync();
        document.AppendText("\r\n\r\nView > Window > Backdrop selects Solid, Mica or Acrylic across the form and ribbon. Native fields and this document stay on opaque surfaces. With material enabled the header stays live while command content animates. Solid retains the full header animation.");
    }
    internal async Task CaptureBackdrops(string directory)
    {
        Directory.CreateDirectory(directory); List<string> log = [];
        try
        {
            Activate(); await Task.Delay(150); Guard(); Location = new(240, 160); Width = 1250; Height = 760;
            var pages = ribbon.Backstage.Items.OfType<RibbonBackstagePageItem>().ToArray();
            foreach (var density in Enum.GetValues<RibbonDensity>())
            {
                ribbon.AnimationLevel = RibbonAnimationLevel.None; ribbon.Density = density; ribbon.IsBackstageOpen = false; ribbon.SelectedTab = inputs;
                var field = Descendants(ribbon).OfType<TextBox>().Single(c => c.Name == nameInput.Id);
                var choice = Descendants(ribbon).OfType<ComboBox>().Single(c => c.Name == fontInput.Id);
                var fieldHandle = field.Handle; var choiceHandle = choice.Handle; string name = nameInput.Value, font = fontInput.Value;
                ribbon.AnimationLevel = RibbonAnimationLevel.Expressive;
                foreach (var material in new[] { RibbonWindowBackdrop.Mica, RibbonWindowBackdrop.Acrylic, RibbonWindowBackdrop.None })
                {
                    Backdrop = material; await Task.Delay(180); Save($"{density}-{material}-inputs");
                    ribbon.SelectedTab = home; await Task.Delay(55); Save($"{density}-{material}-tab-mid"); await Finish(); ribbon.SelectedTab = inputs; await Finish();
                    ribbon.IsMinimized = true; await Task.Delay(60); Save($"{density}-{material}-minimize-mid"); await Finish(); ribbon.IsMinimized = false; await Finish();
                    ribbon.IsBackstageOpen = true; await Task.Delay(70); Save($"{density}-{material}-backstage-mid"); await Finish();
                    foreach (var page in pages) { ribbon.Backstage.SelectedPage = page; await Finish(); Save($"{density}-{material}-{page.Text}"); }
                    ribbon.IsBackstageOpen = false; await Finish();
                    if (field.Handle != fieldHandle || choice.Handle != choiceHandle || nameInput.Value != name || fontInput.Value != font) throw new InvalidOperationException("Backdrop interaction changed native editor identity or data.");
                    log.Add($"PASS: {density}/{material}; effective={EffectiveBackdrop}; actual Inputs/Info/Print native pages, tab/minimize and retained ribbon editors");
                }
            }
            log.Add("PASS: actual Showcase backdrop capture completed; DPI=" + DeviceDpi);
        }
        catch (Exception error) { log.Add("FAIL: " + error); Environment.ExitCode = 1; }
        finally { File.WriteAllLines(Path.Combine(directory, "result.log"), log); }
        void Guard() { GetWindowThreadProcessId(GetForegroundWindow(), out uint process); if (process != Environment.ProcessId) throw new InvalidOperationException("Showcase lost foreground; capture stopped."); }
        async Task Finish() { var watch = Stopwatch.StartNew(); while (ribbon.IsAnimating && watch.ElapsedMilliseconds < 1800) await Task.Delay(5); if (ribbon.IsAnimating) throw new InvalidOperationException("Motion did not finish."); await Task.Delay(35); Guard(); }
        void Save(string name) { Guard(); using var image = new Bitmap(Width, Height); using (var graphics = Graphics.FromImage(image)) graphics.CopyFromScreen(Location, Point.Empty, image.Size); image.Save(Path.Combine(directory, name + ".png")); }
    }
    internal async Task CaptureBackdropMenu(string directory)
    {
        Directory.CreateDirectory(directory); List<string> log = [];
        try
        {
            Activate(); await Task.Delay(150); Location = new(200, 160); Width = 1350; Height = 760;
            var view = ribbon.Tabs.Single(t => t.Id == "tab.merging"); ribbon.SelectedTab = view;
            var menu = (RibbonDropDownItem)view.Groups.Single(g => g.Id == "group.window").Items.Single();
            foreach (var density in Enum.GetValues<RibbonDensity>())
            {
                ribbon.Density = density; await Task.Delay(300); menu.ShowDropDown(); await Task.Delay(80);
                if (Control.FromHandle(GetForegroundWindow()) is not ToolStripDropDown popup) throw new InvalidOperationException("Expected the Backdrop menu.");
                using (var image = new Bitmap(popup.Width, popup.Height)) { using (var graphics = Graphics.FromImage(image)) graphics.CopyFromScreen(popup.Location, Point.Empty, image.Size); image.Save(Path.Combine(directory, density + "-menu.png")); }
                menu.CloseDropDown();
                foreach (var value in Enum.GetValues<RibbonWindowBackdrop>())
                {
                    menu.Items.Single(i => i.Id == "demo.window.backdrop." + value).PerformClick();
                    if (Backdrop != value) throw new InvalidOperationException("The Showcase choice did not set the public Backdrop property.");
                }
                log.Add("PASS: " + density + " Backdrop menu icons, open/close and all three application commands");
            }
        }
        catch (Exception error) { log.Add("FAIL: " + error); Environment.ExitCode = 1; }
        finally { File.WriteAllLines(Path.Combine(directory, "result.log"), log); }
    }
    internal async Task CaptureBackdropDensity(string directory)
    {
        Directory.CreateDirectory(directory); List<string> log = [];
        try
        {
            Activate(); SetForegroundWindow(Handle); await Task.Delay(150); Location = new(240, 160); Width = 1250; Height = 760;
            ribbon.SelectedTab = inputs; ribbon.IsMinimized = false;
            foreach (var material in new[] { RibbonWindowBackdrop.Mica, RibbonWindowBackdrop.Acrylic })
            foreach (var placement in new[] { RibbonQuickAccessPosition.TitleBar, RibbonQuickAccessPosition.TabRow })
            {
                ribbon.AnimationLevel = RibbonAnimationLevel.None; ribbon.Density = RibbonDensity.Touch; ribbon.QuickAccessPosition = placement; Backdrop = material; await Task.Delay(100);
                ribbon.AnimationLevel = RibbonAnimationLevel.Expressive;
                foreach (var target in new[] { RibbonDensity.Compact, RibbonDensity.Touch })
                {
                    ribbon.Density = target; if (!ribbon.IsAnimating) throw new InvalidOperationException("Density body motion did not start.");
                    int black = 0;
                    for (int frame = 0; frame < 7; frame++)
                    {
                        await Task.Delay(35); GetWindowThreadProcessId(GetForegroundWindow(), out uint process); if (process != Environment.ProcessId) throw new InvalidOperationException("Showcase lost foreground; capture stopped.");
                        using var image = new Bitmap(Width, Height); using (var graphics = Graphics.FromImage(image)) graphics.CopyFromScreen(Location, Point.Empty, image.Size);
                        image.Save(Path.Combine(directory, $"{material}-{placement}-{target}-{frame}.png"));
                        int rows = 0; for (int y = 70; y < 160; y++) { var color = image.GetPixel(Width - 150, y); if (color.R + color.G + color.B < 36) rows++; }
                        black = Math.Max(black, rows);
                    }
                    await Task.Delay(80); if (black > 2) throw new InvalidOperationException("Captured header blackened the body during density motion: " + black);
                    log.Add($"PASS: {material}/{placement}/to-{target}; black body rows={black}");
                }
            }
        }
        catch (Exception error) { log.Add("FAIL: " + error); Environment.ExitCode = 1; }
        finally { File.WriteAllLines(Path.Combine(directory, "result.log"), log); }
    }
}
