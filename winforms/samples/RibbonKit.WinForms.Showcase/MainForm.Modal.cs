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
    private void InitializeModal()
    {
        var source = new RibbonMergeSource { Text = "Print preview mode" }; components!.Add(source);
        var tab = new RibbonTab { Id = "modal.preview", Text = "Print Preview", KeyTip = "P", IsModal = true, CloseButtonText = "Close Print Preview" };
        var settings = new RibbonGroup { Text = "Preview settings" };
        var copies = new RibbonTextBoxItem { Text = "Copies", KeyTip = "N", Value = "1", InputWidth = 75 };
        copies.ValueValidating += (_, e) => { e.Cancel = !int.TryParse(copies.Value, out int value) || value is < 1 or > 99; if (e.Cancel) status.Text = "Enter 1 to 99 copies, or press Escape to restore the value."; };
        var zoom = new RibbonComboBoxItem { Text = "Zoom", KeyTip = "Z", InputWidth = 90 }; zoom.Items.AddRange("50%", "100%", "150%"); zoom.SelectedIndex = 1;
        var print = new RibbonButtonItem { Text = "Print request", KeyTip = "R" };
        print.Click += (_, _) => status.Text = $"Application print request: {copies.Value} copies.";
        settings.Items.AddRange(copies, zoom, print); tab.Groups.Add(settings); source.Tabs.Add(tab);
        zoom.SimplifiedPlacement = print.SimplifiedPlacement = RibbonSimplifiedPlacement.Inline;
        var preview = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(226, 228, 232), Visible = false };
        Controls.Add(preview);
        preview.Paint += (_, e) =>
        {
            int inset = Math.Max(16, (int)Math.Round(24 * DeviceDpi / 96d));
            var page = new Rectangle(inset, inset, Math.Max(1, Math.Min(560, preview.Width - 2 * inset)), Math.Max(1, preview.Height - 2 * inset));
            using var paper = new SolidBrush(Color.White); e.Graphics.FillRectangle(paper, page);
            using var border = new Pen(Color.SlateGray); e.Graphics.DrawRectangle(border, page);
            var text = Rectangle.Inflate(page, -inset, -inset);
            TextRenderer.DrawText(e.Graphics, "Application-owned print preview\r\n\r\nZoom: " + zoom.Value + "\r\nCopies: " + copies.Value + "\r\n\r\n" + document.Text, Font, text, Color.FromArgb(32, 31, 30), TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        };
        zoom.SelectedIndexChanged += (_, _) => preview.Invalidate(); copies.ValueChanged += (_, _) => preview.Invalidate();
        ribbon.ModalEntered += (_, e) => { if (e.Tab == tab) { document.Visible = false; preview.Visible = true; preview.BringToFront(); status.Text = "Print Preview: File and minimize are unavailable. Quick Access remains available."; } };
        ribbon.ModalExited += (_, e) => { if (e.Tab == tab) { preview.Visible = false; document.Visible = true; ribbon.Unmerge(source); if (ContainsFocus) document.Focus(); status.Text = "Print Preview closed; previous ribbon presentation restored."; } };
        var launch = new RibbonButtonItem { Id = "modal.launch", Text = "Print preview", KeyTip = "P" };
        launch.Click += (_, _) => { ribbon.Merge(source); if (!ribbon.EnterModal(tab)) ribbon.Unmerge(source); };
        var group = new RibbonGroup { Id = "group.modalPreview", Text = "Modal preview" }; group.Items.Add(launch); ribbon.Tabs.Single(t => t.Id == "tab.merging").Groups.Add(group);
        document.AppendText("\r\n\r\nView > Print preview enters a modal tab. Quick Access remains available; File and minimize are blocked. Close Print Preview or Escape restores the prior ribbon selection and minimized state. The preview page and print action belong to this application.");
    }
}
