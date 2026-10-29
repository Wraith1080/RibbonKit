namespace RibbonKit.WinForms.Showcase;

public partial class MainForm
{
    private void InitializeAnimation()
    {
        ribbon.AnimationLevel = RibbonAnimationLevel.Subtle;
        var group = new RibbonGroup { Id = "group.ribbonAnimation", Text = "Ribbon animation" };
        var levels = new Dictionary<RibbonAnimationLevel, RibbonRadioButtonItem>();
        var styles = new Dictionary<RibbonTabContentAnimation, RibbonRadioButtonItem>();
        void Sync()
        {
            foreach (var (level, item) in levels) item.Checked = ribbon.AnimationLevel == level;
            foreach (var (style, item) in styles) item.Checked = ribbon.TabContentAnimation == style;
        }
        foreach (var level in Enum.GetValues<RibbonAnimationLevel>())
        {
            var item = new RibbonRadioButtonItem { Id = "demo.animation." + level, Text = level.ToString(), KeyTip = level.ToString()[..1], GroupName = "showcase.animation.level", SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium), SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
            item.Click += (_, _) => { ribbon.AnimationLevel = level; Sync(); }; levels.Add(level, item); group.Items.Add(item);
        }
        var content = new RibbonDropDownItem { Id = "demo.animation.tabContent", Text = "Tab content", KeyTip = "T", DropDownHeader = "Tab content animation", SimplifiedPlacement = RibbonSimplifiedPlacement.Inline, ScreenTipDescription = "Choose Fade or Slide down for tab content and newly shown or merged headers. Groups share this choice: Slide moves left on collapse and right on restore." };
        foreach (var style in Enum.GetValues<RibbonTabContentAnimation>())
        {
            var item = new RibbonRadioButtonItem { Id = "demo.animation.tabContent." + style, Text = style == RibbonTabContentAnimation.Fade ? "Fade" : "Slide down", GroupName = "showcase.animation.tabContent" };
            item.Click += (_, _) => { ribbon.TabContentAnimation = style; Sync(); }; styles.Add(style, item); content.Items.Add(item);
        }
        group.Items.Add(content); var view = ribbon.Tabs.Single(t => t.Id == "tab.merging"); view.Groups.Add(group); Sync();
        ribbon.SelectedTabChanged += (_, _) => { if (ReferenceEquals(ribbon.SelectedTab, view)) Sync(); };
        document.AppendText("\r\n\r\nView > Ribbon animation selects None, Subtle or Expressive for ribbon motion, including minimizing/restoring with the chevron, Ctrl+F1 or a tab double-click, and opening command menus or galleries. Popup frames stay anchored while their contents fade and slide into place; input settles the effect before normal interaction. Backstage navigation slides in while live pages fade on opening and selection. Back or Escape fades Backstage out after validation. Tab content chooses Fade or Slide down for content and newly shown or merged headers. None and the system animation preference always keep transitions immediate. View > Messages collects Add, Reopen and Clear in one menu; those actions and Close animate the message rows and their joins to the ribbon/QAT.");
    }
    internal async Task CaptureView(string directory)
    {
        Directory.CreateDirectory(directory); Activate(); var view = ribbon.Tabs.Single(t => t.Id == "tab.merging"); ribbon.SelectedTab = view;
        foreach (var density in Enum.GetValues<RibbonDensity>())
        {
            ribbon.Density = density; await Task.Delay(300); Save(this, density + "-view");
        }
        ribbon.Density = RibbonDensity.Compact; await Task.Delay(300);
        foreach (var id in new[] { "demo.chart.menu", "demo.messages.menu", "demo.animation.tabContent" })
        {
            var menu = (RibbonDropDownItem)view.Groups.SelectMany(g => g.Items).Single(i => i.Id == id); menu.ShowDropDown(); await Task.Delay(70);
            if (Control.FromHandle(GetForegroundWindow()) is not ToolStripDropDown popup) throw new InvalidOperationException("Expected the View command menu.");
            Save(popup, id); menu.CloseDropDown();
        }
        var animation = view.Groups.Single(g => g.Id == "group.ribbonAnimation");
        foreach (var level in Enum.GetValues<RibbonAnimationLevel>())
        {
            animation.Items.Single(i => i.Id == "demo.animation." + level).PerformClick();
            if (ribbon.AnimationLevel != level) throw new InvalidOperationException("The animation level selection failed.");
        }
        var content = (RibbonDropDownItem)animation.Items.Single(i => i.Id == "demo.animation.tabContent"); content.Items.Single(i => i.Text == "Slide down").PerformClick();
        if (ribbon.TabContentAnimation != RibbonTabContentAnimation.SlideDown) throw new InvalidOperationException("The tab content selection failed.");
        var messages = (RibbonDropDownItem)view.Groups.SelectMany(g => g.Items).Single(i => i.Id == "demo.messages.menu");
        messages.Items[0].PerformClick(); messages.Items[1].PerformClick(); messages.Items[2].PerformClick();
        if (ribbon.MessageBar.Items.Count != 0) throw new InvalidOperationException("Messages did not clear.");
        var chart = (RibbonDropDownItem)view.Groups.SelectMany(g => g.Items).Single(i => i.Id == "demo.chart.menu");
        chart.Items[0].PerformClick(); chart.Items[1].PerformClick(); chart.Items[2].PerformClick();
        if (ribbon.Tabs.Any(t => t.Id == "merge.chart.tab")) throw new InvalidOperationException("Chart context did not close.");
        ribbon.SelectedTab = view; ribbon.LayoutMode = RibbonLayoutMode.Simplified; await Task.Delay(300); Save(this, "simplified-view");
        File.WriteAllText(Path.Combine(directory, "result.log"), "PASS: View layout and all level/style, message and chart actions; no QAT draft group; actual DPI=" + ribbon.DeviceDpi);
        void Save(Control control, string name) { control.Refresh(); using var image = new Bitmap(control.Width, control.Height); using var graphics = Graphics.FromImage(image); graphics.CopyFromScreen(control.PointToScreen(Point.Empty), Point.Empty, image.Size); image.Save(Path.Combine(directory, name + ".png")); }
    }
}
