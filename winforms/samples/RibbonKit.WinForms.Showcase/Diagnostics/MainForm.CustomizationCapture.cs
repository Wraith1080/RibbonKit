namespace RibbonKit.WinForms.Showcase;

public partial class MainForm
{
    internal async Task CaptureOptions(string directory, bool touch = false)
    {
        Directory.CreateDirectory(directory); Exception? failure = null;
        ribbon.Density = touch ? RibbonDensity.Touch : RibbonDensity.Compact;
        using (var snapshot = ribbon.CommandCatalog.BeginCustomization(CustomizationApplicationId))
            File.WriteAllText(Path.Combine(directory, "default-qat.json"), snapshot.ExportJson());
        using var timer = new System.Windows.Forms.Timer { Interval = 150 };
        timer.Tick += async (_, _) =>
        {
            if (Application.OpenForms.OfType<RibbonQuickAccessCustomizationForm>().FirstOrDefault() is not { } dialog) return;
            timer.Stop();
            try
            {
                await Capture("qat");
                var fullBounds = dialog.Bounds;
                dialog.Width = dialog.MinimumSize.Width; await Capture("qat-narrow"); dialog.Bounds = fullBounds;
                dialog.SelectedPage = dialog.Pages.Single(p => p.Name == "CustomizeRibbonOptionsPage"); await Capture("customize-ribbon");
                File.WriteAllText(Path.Combine(directory, "ribbon-profile.json"), dialog.RibbonEditor.Session.ExportJson());
                dialog.SelectedPage = dialog.Pages.Single(p => p.Name == "ApplicationEditorOptions"); await Capture("application-editor");
                async Task Capture(string name)
                {
                    await Task.Delay(100); dialog.Refresh();
                    if (!dialog.ContainsFocus || GetForegroundWindow() != dialog.Handle) throw new InvalidOperationException("Options dialog lost foreground before capture.");
                    var bounds = dialog.Bounds;
                    using var bitmap = new Bitmap(bounds.Width, bounds.Height); using var graphics = Graphics.FromImage(bitmap);
                    graphics.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size); bitmap.Save(Path.Combine(directory, name + ".png"));
                }
            }
            catch (Exception e) { failure = e; }
            finally { dialog.Close(); }
        };
        Activate(); timer.Start(); ribbon.ShowQuickAccessCustomization(this);
        if (failure is not null) throw new InvalidOperationException("Options capture failed.", failure);
        await Task.Delay(300); // Let the native dialog close animation finish before capturing its owner.
        Activate(); Refresh(); Update();
        if (GetForegroundWindow() != Handle) throw new InvalidOperationException("Showcase lost foreground before ribbon capture.");
        using var ribbonBitmap = new Bitmap(Width, Height); using var ribbonGraphics = Graphics.FromImage(ribbonBitmap);
        ribbonGraphics.CopyFromScreen(Location, Point.Empty, Size); ribbonBitmap.Save(Path.Combine(directory, "ribbon.png"));
    }
    internal async Task CaptureCustomization(string directory)
    {
        Directory.CreateDirectory(directory); Width = 1300; Activate(); ribbon.SelectedTab = home;
        using var session = ribbon.CommandCatalog.BeginCustomization(CustomizationApplicationId, RibbonCustomizationTarget.QuickAccess);
        await Capture("before"); session.Draft.QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon; await Capture("draft-unapplied");
        if (!session.Apply()) throw new InvalidOperationException("Showcase customization input validation blocked capture.");
        string saved = session.ExportJson(); status.Text = "Toolbar changes applied."; await Capture("applied");
        session.Draft.QuickAccess.Clear(); session.Cancel(); status.Text = "Unapplied edits discarded; the latest Apply remains."; await Capture("cancel-after-apply");
        session.Reset(); if (!session.Apply()) throw new InvalidOperationException("Showcase default reset was blocked."); status.Text = "Original toolbar restored."; await Capture("reset");
        session.ImportJson(saved); if (!session.Apply()) throw new InvalidOperationException("Showcase import Apply was blocked."); status.Text = "Saved toolbar restored."; await Capture("restored");
        async Task Capture(string name)
        {
            await Task.Delay(100); Refresh(); Update();
            if (!ContainsFocus) throw new InvalidOperationException("Showcase lost foreground before customization capture.");
            using var bitmap = new Bitmap(Width, Height); using var g = Graphics.FromImage(bitmap); g.CopyFromScreen(Location, Point.Empty, Size); bitmap.Save(Path.Combine(directory, name + ".png"));
        }
    }
}
