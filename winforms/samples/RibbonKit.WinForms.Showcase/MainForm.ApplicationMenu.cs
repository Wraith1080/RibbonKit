namespace RibbonKit.WinForms.Showcase;

public partial class MainForm
{
    private void InitializeApplicationMenu()
    {
        ribbon.ApplicationMenu.DefaultPane.DropDownHeader = "Recent documents";
        foreach (string name in new[] { "Quarterly report", "Project notes", "Example document" })
        {
            var recent = new RibbonButtonItem { Id = "application.recent." + name, Text = name };
            recent.Click += ActionClick; ribbon.ApplicationMenu.DefaultPane.Items.Add(recent);
        }
        var backstage = new RibbonButtonItem { Id = "application.backstage", Text = "Backstage", KeyTip = "B" };
        backstage.Click += (_, _) => { ribbon.FileMenuMode = RibbonFileMenuMode.Backstage; ribbon.FileMenu.ShowDropDown(); };
        var options = new RibbonButtonItem { Id = "application.options", Text = "Options", KeyTip = "P" }; options.Click += ActionClick;
        ribbon.ApplicationMenu.FooterItems.AddRange(options, backstage);
        var applicationMenu = new RibbonButtonItem { Id = "backstage.applicationMenu", Text = "Application Menu", KeyTip = "A" };
        applicationMenu.Click += (_, _) => { ribbon.FileMenuMode = RibbonFileMenuMode.CommandMenu; ribbon.FileMenu.ShowDropDown(); };
        ribbon.Backstage.FooterItems.Add(applicationMenu);
        ribbon.FileMenuMode = RibbonFileMenuMode.CommandMenu;
    }
}
