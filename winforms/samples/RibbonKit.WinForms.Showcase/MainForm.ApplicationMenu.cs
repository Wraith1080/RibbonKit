namespace RibbonKit.WinForms.Showcase;

public partial class MainForm
{
    private void InitializeApplicationMenu()
    {
        RibbonButtonItem Command(string id, string text, string icon, string description = "")
        {
            var command = new RibbonButtonItem { Id = id, Text = text, Icon = RibbonStockIcons.Create(icon), MenuDescription = description };
            command.Click += ActionClick; return command;
        }
        RibbonDropDownItem Pane(string id, string text, string icon, string header, params RibbonItem[] items)
        {
            var pane = new RibbonDropDownItem { Id = id, Text = text, Icon = RibbonStockIcons.Create(icon), DropDownHeader = header, MenuPresentation = RibbonMenuPresentation.IllustratedChoices };
            pane.Items.AddRange(items); return pane;
        }
        var sources = ribbon.FileMenu.Items.ToArray();
        var newDocument = sources.Single(i => i.Id == "file.new"); var open = sources.Single(i => i.Id == "file.open"); var save = sources.Single(i => i.Id == "file.save");
        foreach (var command in new[] { newDocument, open, save }) { if (command.ScreenTipDescription.Length == 0) command.ScreenTipDescription = command.MenuDescription; command.MenuDescription = ""; }
        var saveAs = Pane("application.saveAs", "Save As", "save-copy", "Save a copy of the document",
            Command("application.saveAs.word", "Word Document", "save", "Save the document in the default file format."),
            Command("application.saveAs.template", "Word Template", "new", "Save as a template that can be used to format future documents."),
            Command("application.saveAs.legacy", "Word 97-2003 Document", "save", "Save a copy that is compatible with Word 97-2003."),
            Command("application.saveAs.other", "Other Formats", "settings", "Open Save As to choose from all possible file types."));
        saveAs.KeyTip = "A";
        var print = Pane("application.print", "Print", "print", "Print document",
            Command("application.print.options", "Print", "print", "Choose a printer, copies and other print options."),
            Command("application.print.quick", "Quick Print", "print", "Send the document to your default printer.")); print.KeyTip = "P";
        var prepare = Pane("application.prepare", "Prepare", "settings", "Prepare the document",
            Command("application.prepare.properties", "Document Properties", "info", "Review document details before sharing."),
            Command("application.prepare.inspect", "Inspect Document", "find", "Check the document before publishing it.")); prepare.KeyTip = "E";
        var send = Pane("application.send", "Send", "send", "Send a copy of the document",
            Command("application.send.email", "Email", "mail", "Send the document as an attachment."),
            Command("application.send.pdf", "Email as PDF", "document-pdf", "Send a fixed-layout PDF copy.")); send.KeyTip = "D";
        var publish = Pane("application.publish", "Publish", "share", "Publish the document",
            Command("application.publish.pdf", "Create PDF", "document-pdf", "Publish a copy that preserves the document layout."),
            Command("application.publish.shared", "Shared Location", "folder", "Publish to a location that other people can access.")); publish.KeyTip = "U";
        var close = Command("application.close", "Close", "arrow-exit"); close.KeyTip = "C";
        ribbon.FileMenu.Items.Clear();
        ribbon.FileMenu.Items.AddRange(newDocument, open, save, saveAs, sources.OfType<RibbonMenuSeparatorItem>().First(), print, prepare, send, publish, new RibbonMenuSeparatorItem(), close);
        ribbon.ApplicationMenu.DefaultPane.DropDownHeader = "Recent Documents";
        string[] names = ["Quarterly report.docx", "Meeting notes.docx", "RibbonKit roadmap.docx", "Office 2024 theme plan.docx"];
        for (int i = 0; i < names.Length; i++)
        {
            var recent = new RibbonButtonItem { Id = "application.recent." + i, Text = (i + 1) + "  " + names[i], KeyTip = (i + 1).ToString() };
            recent.Click += ActionClick; ribbon.ApplicationMenu.DefaultPane.Items.Add(recent);
        }
        var manage = new RibbonButtonItem { Id = "application.recent.manage", Text = "Manage recent locations", KeyTip = "M" }; manage.Click += ActionClick;
        ribbon.ApplicationMenu.DefaultPane.Items.Add(manage);
        var recentSource = (RibbonDropDownItem)sources.Single(i => i.Id == "file.recent"); recentSource.Text = "More recent documents…";
        ribbon.ApplicationMenu.DefaultPane.Items.Add(recentSource);
        var backstage = new RibbonButtonItem { Id = "application.backstage", Text = "Backstage", KeyTip = "B", Icon = RibbonStockIcons.Create("document") };
        backstage.Click += (_, _) => { ribbon.FileMenuMode = RibbonFileMenuMode.Backstage; ribbon.FileMenu.ShowDropDown(); };
        var options = Command("application.options", "Options", "settings"); options.KeyTip = "T";
        var exit = new RibbonButtonItem { Id = "application.exit", Text = "Exit", KeyTip = "X", Icon = RibbonStockIcons.Create("arrow-exit") }; exit.Click += (_, _) => Close();
        ribbon.ApplicationMenu.FooterItems.AddRange(options, backstage, exit);
        var applicationMenu = new RibbonButtonItem { Id = "backstage.applicationMenu", Text = "Application Menu", KeyTip = "A" };
        applicationMenu.Click += (_, _) => { ribbon.FileMenuMode = RibbonFileMenuMode.CommandMenu; ribbon.FileMenu.ShowDropDown(); };
        ribbon.Backstage.FooterItems.Add(applicationMenu);
        ribbon.FileMenuMode = RibbonFileMenuMode.CommandMenu;
    }
}
