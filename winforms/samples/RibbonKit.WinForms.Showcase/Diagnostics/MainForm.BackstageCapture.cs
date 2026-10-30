namespace RibbonKit.WinForms.Showcase;

public partial class MainForm
{
    internal async Task CaptureBackstage(string directory)
    {
        Directory.CreateDirectory(directory);
        Activate();
        await Task.Delay(100);
        ribbon.IsBackstageOpen = true;
        if (!ribbon.IsBackstageOpen) throw new InvalidOperationException("Backstage did not open for capture.");
        foreach (var page in ribbon.Backstage.Items.OfType<RibbonBackstagePageItem>())
        {
            ribbon.Backstage.SelectedPage = page;
            Refresh(); Update();
            await Task.Delay(80);
            if (!ContainsFocus) throw new InvalidOperationException("Showcase lost foreground during capture.");
            using var bitmap = new Bitmap(Width, Height);
            using var graphics = Graphics.FromImage(bitmap);
            graphics.CopyFromScreen(Location, Point.Empty, bitmap.Size);
            bitmap.Save(Path.Combine(directory, page.Text.ToLowerInvariant() + ".png"));
        }
    }
}
