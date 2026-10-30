namespace RibbonKit.WinForms.Showcase;

public partial class MainForm
{
    internal async Task CaptureReviewFollowup(string directory, bool scrollersOnly = false)
    {
        Directory.CreateDirectory(directory); Activate(); BringToFront(); await Task.Delay(150); ribbon.AnimationLevel = RibbonAnimationLevel.None; ribbon.LayoutMode = RibbonLayoutMode.Simplified;
        picture.PerformClick(); await Task.Delay(60);
        Check(ribbon.Tabs.Last() == pictureFormat && ribbon.Tabs.Where(t => !t.IsContextual && !t.IsModal).All(t => ribbon.Tabs.IndexOf(t) < ribbon.Tabs.IndexOf(pictureFormat)), "Picture Format follows authored normal tabs"); if (!scrollersOnly) Capture("picture-context");
        var chartMenu = ribbon.Tabs.SelectMany(t => t.Groups).SelectMany(g => g.Items).OfType<RibbonDropDownItem>().Single(i => i.Id == "demo.chart.menu");
        chartMenu.Items[1].PerformClick(); await Task.Delay(60); var chart = ribbon.SelectedTab!;
        Check(chart.Id == "merge.chart.tab" && chart.Groups.SelectMany(g => g.Items).Count(i => i.SimplifiedPlacement == RibbonSimplifiedPlacement.Inline) >= 2, "Chart Design has authored inline defaults");
        var order = ribbon.GetPresentedTabs(); Check(order.ToList().IndexOf(pictureFormat) < order.ToList().IndexOf(chart), "Merged chart context follows the authored contextual tab");
        if (scrollersOnly) { Width = 650; await Task.Delay(80); Capture("tab-scrollers"); File.WriteAllText(Path.Combine(directory, "result.log"), "PASS: narrow tab-scroller native capture; actual DPI=" + ribbon.DeviceDpi); return; }
        Capture("chart-context");
        ribbon.Tabs.SelectMany(t => t.Groups).SelectMany(g => g.Items).Single(i => i.Id == "modal.launch").PerformClick(); await Task.Delay(60); var preview = ribbon.SelectedTab!;
        Check(preview.Id == "modal.preview" && preview.Groups.SelectMany(g => g.Items).Count(i => i.SimplifiedPlacement == RibbonSimplifiedPlacement.Inline) >= 2, "Print Preview has authored inline defaults"); Capture("print-preview"); ribbon.ExitModal();
        File.WriteAllText(Path.Combine(directory, "result.log"), "PASS: Showcase Picture Format ordering and Chart/Print Preview Simplified defaults; actual DPI=" + ribbon.DeviceDpi);
        static void Check(bool ok, string text) { if (!ok) throw new InvalidOperationException(text); }
        void Capture(string name) { Check(GetForegroundWindow() == Handle, "Showcase owns the foreground for capture"); Refresh(); using var image = new Bitmap(Width, Height); using var g = Graphics.FromImage(image); g.CopyFromScreen(Location, Point.Empty, Size); image.Save(Path.Combine(directory, name + ".png")); }
    }
}
