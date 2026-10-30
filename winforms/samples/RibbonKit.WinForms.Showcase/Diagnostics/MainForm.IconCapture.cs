namespace RibbonKit.WinForms.Showcase;

public partial class MainForm
{
    private readonly List<string> _iconAudit = [];
    internal void CaptureCommandIcons(string directory)
    {
        Directory.CreateDirectory(directory); File.WriteAllLines(Path.Combine(directory, "coverage.txt"), _iconAudit.Order());
        const string prefix = "RibbonKit.WinForms.Showcase.Assets.CommandIcons.";
        var names = typeof(MainForm).Assembly.GetManifestResourceNames().Where(n => n.StartsWith(prefix, StringComparison.Ordinal) && n.EndsWith(".svg", StringComparison.Ordinal)).Select(n => n[prefix.Length..^4]).Order().ToArray();
        using var sheet = new Bitmap(1000, ((names.Length + 9) / 10) * 92); using var graphics = Graphics.FromImage(sheet); graphics.Clear(ribbon.Theme.Surface);
        using var font = new Font("Segoe UI", 8); using var format = new StringFormat { Alignment = StringAlignment.Center };
        for (int i = 0; i < names.Length; i++)
        {
            int x = i % 10 * 100, y = i / 10 * 92; new CommandIcon(names[i], ribbon).Draw(graphics, new(x + 26, y + 6, 48, 48), new(DeviceDpi, ribbon.Theme.Foreground, true));
            graphics.DrawString(names[i], font, Brushes.Black, new RectangleF(x + 1, y + 58, 98, 32), format);
        }
        sheet.Save(Path.Combine(directory, "icon-sheet.png"));
        foreach (var tab in ribbon.Tabs.Where(t => !t.IsContextual && !t.IsModal)) { ribbon.SelectedTab = tab; Capture(tab.Id); }
        ribbon.SelectedTab = home; Width = 440; Capture("collapsed-groups");
        void Capture(string name) { PerformLayout(); ribbon.Refresh(); using var image = new Bitmap(ClientSize.Width, ClientSize.Height); DrawToBitmap(image, ClientRectangle); image.Save(Path.Combine(directory, name + ".png")); }
    }
}
