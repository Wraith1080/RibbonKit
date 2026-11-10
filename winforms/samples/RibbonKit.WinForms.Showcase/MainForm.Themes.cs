namespace RibbonKit.WinForms.Showcase;

public partial class MainForm
{
    private Action? _syncThemeChoices;

    private void InitializeThemes()
    {
        var group = new RibbonGroup { Id = "group.theme", Text = "Theme" };
        var selector = new RibbonDropDownItem
        {
            Id = "demo.theme", Text = "Theme", KeyTip = "H", DropDownHeader = "Light theme",
            SimplifiedPlacement = RibbonSimplifiedPlacement.Inline,
            ScreenTipDescription = "Choose Office 2019 Light, Office 2024 Light or the application's theme."
        };
        var choices = new List<(RibbonBuiltInTheme? Theme, RibbonRadioButtonItem Item)>();
        void Sync()
        {
            foreach (var (theme, item) in choices) item.Checked = ribbon.BuiltInTheme == theme;
            selector.Text = choices.Single(c => c.Theme == ribbon.BuiltInTheme).Item.Text;
            BackColor = ribbon.Theme.Background;
        }
        Add(null, "Application theme", "application");
        Add(RibbonBuiltInTheme.Office2019Light, "Office 2019 Light", "Office2019Light");
        Add(RibbonBuiltInTheme.Office2024Light, "Office 2024 Light", "Office2024Light");
        selector.DropDownOpening += (_, _) => Sync();
        group.Items.Add(selector);
        ribbon.Tabs.Single(t => t.Id == "tab.merging").Groups.Insert(0, group);
        _syncThemeChoices = Sync; Sync();
        document.AppendText("\r\n\r\nView > Theme switches between Office 2019 Light, Office 2024 Light and the application theme. The choice is saved with this application's preferences. Native page content keeps its application styling.");

        void Add(RibbonBuiltInTheme? theme, string text, string id)
        {
            var item = new RibbonRadioButtonItem { Id = "demo.theme." + id, Text = text, GroupName = "showcase.theme" };
            item.Click += (_, _) => { ribbon.BuiltInTheme = theme; Sync(); SavePreferences(); status.Text = text; };
            choices.Add((theme, item)); selector.Items.Add(item);
        }
    }
}
