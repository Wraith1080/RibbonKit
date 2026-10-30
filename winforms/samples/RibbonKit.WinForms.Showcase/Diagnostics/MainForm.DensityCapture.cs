namespace RibbonKit.WinForms.Showcase;

public partial class MainForm
{
    [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "mouse_event")]
    private static extern void DensityCaptureMouse(uint flags, uint dx, uint dy, uint data, nuint extra);
    internal async Task CaptureDensity(string directory)
    {
        Directory.CreateDirectory(directory); var work = Screen.FromControl(this).WorkingArea; Width = Math.Min(1500, work.Width - 32); Left = work.Left + Math.Max(0, (work.Width - Width) / 2); Activate();
        using var recorder = new DensityCaptureRenderer(paste); var renderer = ribbon.Renderer; var cursor = Cursor.Position;
        try
        {
            ribbon.Renderer = recorder;
            foreach (var density in new[] { RibbonDensity.Compact, RibbonDensity.Touch })
            {
                ribbon.Density = density; ribbon.SelectedTab = home; await Capture(density.ToString());
            }
            Width = Math.Min(1100, work.Width - 32); Left = work.Left + Math.Max(0, (work.Width - Width) / 2); await Capture("Touch-narrow");
            Width = Math.Min(760, work.Width - 32); await Capture("Touch-collapsed");
            Width = Math.Min(1100, work.Width - 32); ribbon.Refresh();
            await Hover(recorder.Menu); await Capture("Touch-menu-hover");
            await Hover(recorder.Primary); await Capture("Touch-primary-hover");
            var originalLayout = paste.Layout;
            try
            {
                paste.Layout = RibbonSplitButtonLayout.Horizontal; ribbon.Refresh();
                await Hover(recorder.Menu); await Capture("Touch-horizontal-split");
            }
            finally { paste.Layout = originalLayout; }
            foreach (var density in new[] { RibbonDensity.Compact, RibbonDensity.Touch })
            {
                ribbon.Density = density; ribbon.SelectedTab = home; Activate(); ribbon.Refresh(); await Hover(recorder.Selector);
                DensityCaptureMouse(2, 0, 0, 0, 0); DensityCaptureMouse(4, 0, 0, 0, 0); await Task.Delay(120);
                if (Control.FromHandle(GetForegroundWindow()) is not ToolStripDropDown popup) throw new InvalidOperationException("Expected the QAT density chooser.");
                using var bitmap = new Bitmap(popup.Width, popup.Height); using var graphics = Graphics.FromImage(bitmap);
                graphics.CopyFromScreen(popup.Location, Point.Empty, popup.Size); bitmap.Save(Path.Combine(directory, density + "-chooser.png"));
                _densitySelector.CloseDropDown();
            }
            var originalPosition = ribbon.QuickAccessPosition;
            try
            {
                ribbon.QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon;
                foreach (var density in new[] { RibbonDensity.Compact, RibbonDensity.Touch })
                {
                    ribbon.Density = density;
                    foreach (bool minimized in new[] { false, true })
                    {
                        ribbon.IsMinimized = minimized;
                        await Capture($"{density}-qat-below-{(minimized ? "minimized" : "expanded")}");
                    }
                }
            }
            finally { ribbon.IsMinimized = false; ribbon.QuickAccessPosition = originalPosition; }
        }
        finally { ribbon.Renderer = renderer; Cursor.Position = cursor; }

        async Task Hover(Rectangle bounds)
        {
            if (bounds.IsEmpty) throw new InvalidOperationException("Showcase split target was not painted.");
            Cursor.Position = ribbon.PointToScreen(new(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2)); await Task.Delay(60);
        }
        async Task Capture(string name)
        {
            await Task.Delay(100); ribbon.Refresh();
            if (!ContainsFocus) throw new InvalidOperationException("Showcase lost foreground before density capture.");
            using var bitmap = new Bitmap(Width, Height); using var graphics = Graphics.FromImage(bitmap); graphics.CopyFromScreen(Location, Point.Empty, Size); bitmap.Save(Path.Combine(directory, name + ".png"));
        }
    }
    private sealed class DensityCaptureRenderer(RibbonSplitButtonItem source) : RibbonOffice2024Renderer
    {
        internal Rectangle Primary, Menu, Selector;
        public override void DrawQuickAccessItem(RibbonPaintContext context, RibbonElement entry, bool menu)
        {
            if (!menu && entry is RibbonQuickAccessDropDownReference { Source.Id: "density.selector" }) Selector = context.Bounds;
            base.DrawQuickAccessItem(context, entry, menu);
        }
        public override void DrawSplitItem(RibbonPaintContext primary, RibbonPaintContext dropDown, RibbonSplitButtonItem item, RibbonItemSize size, bool menu)
        {
            if (ReferenceEquals(item, source) && size == RibbonItemSize.Large && !menu) { Primary = primary.Bounds; Menu = dropDown.Bounds; }
            base.DrawSplitItem(primary, dropDown, item, size, menu);
        }
    }
}
