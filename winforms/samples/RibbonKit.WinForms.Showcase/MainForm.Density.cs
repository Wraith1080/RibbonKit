namespace RibbonKit.WinForms.Showcase;

public partial class MainForm
{
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
        void Sync()
        {
            mouse.Checked = ribbon.Density == RibbonDensity.Compact; touch.Checked = !mouse.Checked; selector.Text = mouse.Checked ? "Mouse" : "Touch";
            var icon = mouse.Checked ? mouseIcon : touchIcon;
            // Icon assignment invalidates layout, even for the same borrowed
            // object. A repeated Sync must not cancel an in-flight transition.
            if (!ReferenceEquals(selector.Icon, icon)) selector.Icon = icon;
        }
        mouse.Click += (_, _) => { ribbon.Density = RibbonDensity.Compact; Sync(); };
        touch.Click += (_, _) => { ribbon.Density = RibbonDensity.Touch; Sync(); };
        ribbon.DensityChanged += (_, _) => { Sync(); status.Text = ribbon.Density == RibbonDensity.Touch ? "Touch spacing" : "Mouse spacing"; };
        var group = new RibbonGroup { Id = "group.inputMode", Text = "Input mode" }; group.Items.Add(selector); ribbon.Tabs.Single(t => t.Id == "tab.merging").Groups.Add(group);
        ribbon.QuickAccessItems.Insert(2, new RibbonQuickAccessDropDownReference { Source = selector }); Sync();
        document.AppendText("\r\n\r\nThe third Quick Access entry (also View > Input mode) switches Mouse/Touch spacing. Touch keeps the same text size and commands while enlarging rows, fields, popup choices and Quick Access targets. The application owns this selector and preference storage.");
    }
}
