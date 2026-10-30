namespace RibbonKit.WinForms.Showcase;

public partial class MainForm
{
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern nint GetForegroundWindow();
    internal async Task CaptureGalleries(string directory)
    {
        Directory.CreateDirectory(directory);
        Width = Math.Min(1600, Screen.FromControl(this).WorkingArea.Width - 32);
        Activate(); await Task.Delay(100);
        using (var strip = new Bitmap(Width, Height))
        {
            using var graphics = Graphics.FromImage(strip); graphics.CopyFromScreen(Location, Point.Empty, Size);
            strip.Save(Path.Combine(directory, "in-ribbon-styles.png"));
        }
        foreach (var gallery in _sampleGalleries)
        {
            if (!ContainsFocus) throw new InvalidOperationException("Showcase lost foreground before gallery capture.");
            gallery.ShowDropDown(); await Task.Delay(100);
            if (!gallery.IsDropDownOpen || Control.FromHandle(GetForegroundWindow()) is not ToolStripDropDown popup) throw new InvalidOperationException("Expected shown Showcase gallery.");
            var bounds = Rectangle.Union(Bounds, popup.Bounds);
            using var bitmap = new Bitmap(bounds.Width, bounds.Height); using var graphics = Graphics.FromImage(bitmap);
            graphics.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
            bitmap.Save(Path.Combine(directory, gallery.Id + ".png"));
            gallery.CloseDropDown();
        }
    }
}
