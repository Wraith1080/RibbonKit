namespace RibbonKit.WinForms.Showcase;

public partial class MainForm
{
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
