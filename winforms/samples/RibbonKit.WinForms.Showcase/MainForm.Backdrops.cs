using System.Diagnostics;

namespace RibbonKit.WinForms.Showcase;

public partial class MainForm
{
    private void InitializeBackdrops()
    {
        // Application-owned GDI content uses the library's opaque host. The
        // surrounding transparent margins expose the full-form native material.
        var content = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        var opaque = new RibbonBackdropPanel { Dock = DockStyle.Fill };
        Controls.Remove(document); Controls.Remove(statusStrip); opaque.Controls.Add(document); opaque.Controls.Add(statusStrip);
        content.Controls.Add(opaque); Controls.Add(content); Controls.SetChildIndex(content, 0);
        BackdropChanged += (_, _) => content.Padding = Backdrop == RibbonWindowBackdrop.None ? Padding.Empty : new Padding(12);
        var group = new RibbonGroup { Id = "group.window", Text = "Window" };
        var menu = new RibbonDropDownItem { Id = "demo.window.backdrop", Text = "Backdrop", KeyTip = "B", DropDownHeader = "Window backdrop", SimplifiedPlacement = RibbonSimplifiedPlacement.Inline,
            ScreenTipDescription = "Choose Solid, Mica or Acrylic for the window and ribbon. Native fields and the document stay on opaque surfaces. Windows may use a solid fallback." };
        Dictionary<RibbonWindowBackdrop, RibbonRadioButtonItem> choices = [];
        void Sync() { foreach (var (value, item) in choices) item.Checked = Backdrop == value; }
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
