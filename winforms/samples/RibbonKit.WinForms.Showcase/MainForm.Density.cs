namespace RibbonKit.WinForms.Showcase;

public partial class MainForm
{
    [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "mouse_event")]
    private static extern void DensityCaptureMouse(uint flags, uint dx, uint dy, uint data, nuint extra);
    private RibbonDropDownItem _densitySelector = null!;
    private void InitializeDensity()
    {
        var mouseIcon = new DensityModeIcon(false); var touchIcon = new DensityModeIcon(true);
        var selector = _densitySelector = new RibbonDropDownItem { Id = "density.selector", Text = "Mouse", KeyTip = "D", Icon = mouseIcon,
            DropDownHeader = "Optimize spacing between commands", MenuPresentation = RibbonMenuPresentation.IllustratedChoices,
            ScreenTipDescription = "Choose compact Mouse spacing or roomier Touch targets. Text size stays the same." };
        var mouse = new RibbonRadioButtonItem { Text = "Mouse", KeyTip = "M", Icon = mouseIcon, GroupName = "showcase.density", MenuDescription = "Standard ribbon and commands.\nOptimized for use with mouse." };
        var touch = new RibbonRadioButtonItem { Text = "Touch", KeyTip = "T", Icon = touchIcon, GroupName = "showcase.density", MenuDescription = "More space between commands.\nOptimized for use with touch." };
        selector.Items.AddRange(mouse, touch);
        void Sync() { mouse.Checked = ribbon.Density == RibbonDensity.Compact; touch.Checked = !mouse.Checked; selector.Text = mouse.Checked ? "Mouse" : "Touch"; selector.Icon = mouse.Checked ? mouseIcon : touchIcon; }
        mouse.Click += (_, _) => { ribbon.Density = RibbonDensity.Compact; Sync(); };
        touch.Click += (_, _) => { ribbon.Density = RibbonDensity.Touch; Sync(); };
        ribbon.DensityChanged += (_, _) => { Sync(); status.Text = ribbon.Density == RibbonDensity.Touch ? "Touch spacing" : "Mouse spacing"; };
        var group = new RibbonGroup { Text = "Input mode" }; group.Items.Add(selector); ribbon.Tabs.Single(t => t.Id == "tab.merging").Groups.Add(group);
        ribbon.QuickAccessItems.Insert(2, new RibbonQuickAccessDropDownReference { Source = selector }); Sync();
        document.AppendText("\r\n\r\nThe third Quick Access entry (also Merging > Input mode) switches Mouse/Touch spacing. Touch keeps the same text size and commands while enlarging rows, fields, popup choices and Quick Access targets. The application owns this selector and preference storage.");
    }
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
