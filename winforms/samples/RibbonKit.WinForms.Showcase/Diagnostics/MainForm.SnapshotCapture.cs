namespace RibbonKit.WinForms.Showcase;

public partial class MainForm
{
    internal void PaintRibbonForMeasurement() => ribbon.Refresh();
    internal void CaptureSnapshots(string directory)
    {
        Directory.CreateDirectory(directory);
        foreach (int width in new[] { 1080, 720, 420, 260 })
        {
            Width = width;
            PerformLayout();
            ribbon.Refresh();
            Update();
            using var bitmap = new Bitmap(ClientSize.Width, ClientSize.Height);
            DrawToBitmap(bitmap, ClientRectangle);
            bitmap.Save(Path.Combine(directory, $"native-{width}-{DeviceDpi}dpi.png"));
        }
    }
}
