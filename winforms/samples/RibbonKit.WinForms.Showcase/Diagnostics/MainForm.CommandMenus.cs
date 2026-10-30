namespace RibbonKit.WinForms.Showcase;

public partial class MainForm
{
    internal async Task CaptureCommandMenus(string directory)
    {
        Directory.CreateDirectory(directory); var work = Screen.FromControl(this).WorkingArea;
        Width = Math.Min(1500, work.Width - 32); Left = work.Left + Math.Max(0, (work.Width - Width) / 2); Activate(); ribbon.SelectedTab = home;
        using var recorder = new CommandMenuCaptureRenderer(editing); var originalRenderer = ribbon.Renderer; var cursor = Cursor.Position; ribbon.Renderer = recorder;
        try
        {
            foreach (var density in Enum.GetValues<RibbonDensity>())
            {
                ribbon.Density = density; await Task.Delay(100); ribbon.Refresh();
                foreach (var (menu, name) in new[] { (paste, "paste"), ((RibbonDropDownItem)select, "select"), (trackChanges, "track-changes") })
                    await Capture(menu, name, density);
                ribbon.SelectedTab = inputs; await Task.Delay(100); ribbon.Refresh();
                await Capture(inputMenu, "input-options", density);
                ribbon.SelectedTab = home; await Task.Delay(100); ribbon.Refresh();
            }
        }
        finally { ribbon.Renderer = originalRenderer; Cursor.Position = cursor; }

        async Task Capture(RibbonDropDownItem menu, string name, RibbonDensity density)
        {
            if (!ContainsFocus) throw new InvalidOperationException("Showcase lost foreground before command-menu capture.");
            if ((ReferenceEquals(menu, select) || ReferenceEquals(menu, trackChanges)) && recorder.EditingCollapsed)
            {
                var bounds = recorder.EditingBounds; Cursor.Position = ribbon.PointToScreen(new(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2));
                DensityCaptureMouse(2, 0, 0, 0, 0); DensityCaptureMouse(4, 0, 0, 0, 0); await Task.Delay(100);
            }
            menu.ShowDropDown(); await Task.Delay(100);
            if (!menu.IsDropDownOpen || Control.FromHandle(GetForegroundWindow()) is not ToolStripDropDown popup) throw new InvalidOperationException("Expected the source command popup.");
            using var bitmap = new Bitmap(popup.Width, popup.Height); using var g = Graphics.FromImage(bitmap);
            g.CopyFromScreen(popup.Location, Point.Empty, popup.Size); bitmap.Save(Path.Combine(directory, density + "-" + name + ".png"));
            menu.CloseDropDown(); await Task.Delay(50);
            if (Control.FromHandle(GetForegroundWindow()) is ToolStripDropDown) { SendKeys.SendWait("{ESC}"); await Task.Delay(50); }
        }
    }
    private sealed class CommandMenuCaptureRenderer(RibbonGroup editingGroup) : RibbonOffice2024Renderer
    {
        internal bool EditingCollapsed; internal Rectangle EditingBounds;
        public override void DrawGroup(RibbonPaintContext context, RibbonGroup group, bool collapsed)
        { if (ReferenceEquals(group, editingGroup)) { EditingCollapsed = collapsed; EditingBounds = context.Bounds; } base.DrawGroup(context, group, collapsed); }
    }
}
