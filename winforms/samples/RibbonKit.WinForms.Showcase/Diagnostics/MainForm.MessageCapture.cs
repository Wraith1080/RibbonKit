namespace RibbonKit.WinForms.Showcase;

public partial class MainForm
{
    internal async Task CaptureMessages(string directory)
    {
        Directory.CreateDirectory(directory); Width = 1100; Activate(); ribbon.SelectedTab = home;
        AddSampleMessage(); AddSampleMessage();
        foreach (var density in new[] { RibbonDensity.Compact, RibbonDensity.Touch })
        foreach (var position in new[] { RibbonQuickAccessPosition.TabRow, RibbonQuickAccessPosition.BelowRibbon })
        foreach (bool minimized in new[] { false, true })
        {
            ribbon.Density = density; ribbon.QuickAccessPosition = position; ribbon.IsMinimized = minimized;
            await Capture($"{density}-{position}-{(minimized ? "minimized" : "expanded")}");
        }
        ribbon.QuickAccessPosition = RibbonQuickAccessPosition.TabRow; Width = 440;
        await Capture("Touch-not-below-minimized-narrow");
        async Task Capture(string name)
        {
            await Task.Delay(100); Refresh(); Update();
            if (!ContainsFocus) throw new InvalidOperationException("Showcase lost focus before message capture.");
            using var bitmap = new Bitmap(Width, Height); using var g = Graphics.FromImage(bitmap);
            g.CopyFromScreen(Location, Point.Empty, Size); bitmap.Save(Path.Combine(directory, name + ".png"));
        }
    }
}
