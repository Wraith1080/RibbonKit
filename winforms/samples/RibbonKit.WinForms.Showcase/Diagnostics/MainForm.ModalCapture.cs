namespace RibbonKit.WinForms.Showcase;

public partial class MainForm
{
    internal async Task CaptureModal(string directory)
    {
        Directory.CreateDirectory(directory); Activate(); await Task.Delay(100);
        using var recorder = new HeaderCaptureRenderer(); var renderer = ribbon.Renderer; var cursor = Cursor.Position;
        try
        {
            ribbon.Renderer = recorder;
            ribbon.SelectedTab = ribbon.Tabs.Single(t => t.Id == "tab.merging");
            ribbon.SelectedTab.Groups.SelectMany(g => g.Items).Single(i => i.Id == "modal.launch").PerformClick();
            await Task.Delay(100);
            if (!ContainsFocus || !ribbon.IsModal) throw new InvalidOperationException("Expected foreground Showcase modal preview.");
            Capture("print-preview");
            await Hover(recorder.Close); Capture("modal-close-hover");
            ribbon.ExitModal(); ribbon.Refresh(); await Hover(recorder.Minimize); Capture("minimize-hover");
        }
        finally { Cursor.Position = cursor; ribbon.Renderer = renderer; }
        async Task Hover(Rectangle bounds)
        {
            if (bounds.IsEmpty) throw new InvalidOperationException("Header command was not painted.");
            Cursor.Position = ribbon.PointToScreen(new(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2)); await Task.Delay(100);
        }
        void Capture(string name)
        {
            if (!ContainsFocus) throw new InvalidOperationException("Showcase lost foreground before capture.");
            using var bitmap = new Bitmap(Width, Height); using var graphics = Graphics.FromImage(bitmap); graphics.CopyFromScreen(Location, Point.Empty, Size);
            bitmap.Save(Path.Combine(directory, name + ".png"));
        }
    }
    private sealed class HeaderCaptureRenderer : RibbonOffice2024Renderer
    {
        internal Rectangle Close, Minimize;
        public override void DrawModalClose(RibbonPaintContext context, string text) { Close = context.Bounds; base.DrawModalClose(context, text); }
        public override void DrawMinimizeButton(RibbonPaintContext context, bool minimized) { Minimize = context.Bounds; base.DrawMinimizeButton(context, minimized); }
    }
}
