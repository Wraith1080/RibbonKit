namespace RibbonKit.WinForms.Showcase;

public partial class MainForm
{
    private void InitializeSimplified()
    {
        // Authored defaults, independently of the user's Classic/QAT structure.
        foreach (var tab in ribbon.Tabs)
        foreach (var group in tab.Groups)
        foreach (var item in group.Items.Where(i => i is RibbonButtonItem or RibbonDropDownItem or RibbonComboBoxItem).Take(2))
        { item.SimplifiedPlacement = RibbonSimplifiedPlacement.Inline; item.SimplifiedSize = RibbonItemSize.Medium; }
        foreach (var item in new RibbonItem[] { paste, copy, cut, bold, italic, fontInput, zoomInput })
        { item.SimplifiedPlacement = RibbonSimplifiedPlacement.Inline; item.SimplifiedPriority = item == paste || item == bold ? 10 : 0; }
        var mode = new RibbonToggleItem { Id = "demo.layout.simplified", Text = "Simplified Ribbon", SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium) };
        bool syncing = false;
        mode.CheckedChanged += (_, _) =>
        {
            if (syncing) return; ribbon.LayoutMode = mode.Checked ? RibbonLayoutMode.Simplified : RibbonLayoutMode.Classic;
            syncing = true; mode.Checked = ribbon.LayoutMode == RibbonLayoutMode.Simplified; syncing = false;
        };
        ribbon.LayoutModeChanged += (_, _) => { syncing = true; mode.Checked = ribbon.LayoutMode == RibbonLayoutMode.Simplified; syncing = false; status.Text = ribbon.LayoutMode + " ribbon; density and toolbar preferences are independent."; };
        ribbon.Tabs.Single(t => t.Id == "tab.options").Groups[0].Items.Add(mode);
        document.AppendText("\r\n\r\nOptions > Show > Simplified Ribbon switches to one command row. Paste, Copy, Cut, Bold, Italic and representative commands have authored inline defaults. More Commands keeps displaced commands and every full group reachable. Customize Ribbon follows the active layout; both modes share tabs/groups while Simplified choices and QAT apply independently.");
    }
    internal async Task CaptureSimplified(string directory)
    {
        Directory.CreateDirectory(directory); ribbon.LayoutMode = RibbonLayoutMode.Simplified;
        foreach (var density in Enum.GetValues<RibbonDensity>())
        {
            ribbon.Density = density;
            foreach (int width in new[] { 1280, 680 })
            {
                Width = width; ribbon.SelectedTab = home; await Task.Delay(80); Capture(density + "-" + width);
            }
            Width = 1280; ribbon.SelectedTab = inputs; await Task.Delay(80); Capture(density + "-inputs");
        }
        ribbon.Density = RibbonDensity.Compact; ribbon.SelectedTab = home;
        using var timer = new System.Windows.Forms.Timer { Interval = 160 }; Exception? failure = null;
        timer.Tick += async (_, _) =>
        {
            if (Application.OpenForms.OfType<RibbonQuickAccessCustomizationForm>().FirstOrDefault() is not { } dialog) return; timer.Stop();
            try { await Task.Delay(80); using var image = new Bitmap(dialog.Width, dialog.Height); dialog.DrawToBitmap(image, new(Point.Empty, dialog.Size)); image.Save(Path.Combine(directory, "simplified-options.png")); File.WriteAllText(Path.Combine(directory, "profile.json"), dialog.SimplifiedEditor.Session.ExportJson()); }
            catch (Exception error) { failure = error; }
            finally { dialog.Close(); }
        };
        timer.Start(); ribbon.ShowRibbonCustomization(this); if (failure is not null) throw failure;
        void Capture(string name) { PerformLayout(); Refresh(); using var image = new Bitmap(ClientSize.Width, ClientSize.Height); DrawToBitmap(image, ClientRectangle); image.Save(Path.Combine(directory, name + ".png")); }
    }
}
