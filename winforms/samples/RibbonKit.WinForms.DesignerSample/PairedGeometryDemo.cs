using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

public partial class DesignerForm
{
    // Opt-in review tabs. Authored models and InitializeComponent stay intact.
    internal void EnablePairedGeometryDemo()
    {
        var commands = new RibbonTab { Text = "Command content" };
        var settings = new RibbonGroup { Text = "Content spacing", CanResize = false };
        var apply = DemoAction("Roomier command content"); var reset = DemoAction("Reset command content");
        var mode = new RibbonToggleItem { Text = "Simplified layout", Checked = ribbon.LayoutMode == RibbonLayoutMode.Simplified,
            SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium), SimplifiedPlacement = RibbonSimplifiedPlacement.Inline, SimplifiedPriority = int.MaxValue };
        apply.Click += (_, _) =>
        {
            ribbon.Theme = ribbon.Theme with { CommandContentHorizontalPadding = 12, CommandContentVerticalPadding = 16 };
            statusLabel.Text = "Small/Medium content has more room. Compare Mouse/Touch and Classic/Simplified.";
        };
        reset.Click += (_, _) =>
        {
            ribbon.Theme = ribbon.Theme with { CommandContentHorizontalPadding = null, CommandContentVerticalPadding = null };
            statusLabel.Text = "Command content reset; other geometry and layout mode retained.";
        };
        mode.Click += (_, _) => ribbon.LayoutMode = mode.Checked ? RibbonLayoutMode.Simplified : RibbonLayoutMode.Classic;
        ribbon.LayoutModeChanged += (_, _) => mode.Checked = ribbon.LayoutMode == RibbonLayoutMode.Simplified;
        settings.Items.AddRange(apply, reset, mode); commands.Groups.Add(settings);
        var examples = new RibbonGroup { Text = "Small / Medium commands", CanResize = false };
        var small = new RibbonToggleItem { Text = "Bold", Icon = RibbonStockIcons.Create("bold"), SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Small),
            SimplifiedPlacement = RibbonSimplifiedPlacement.Inline, SimplifiedSize = RibbonItemSize.Small };
        var open = DemoAction("Open document"); open.Icon = RibbonStockIcons.Create("open");
        var split = CreateCommandSplitExample("Paste", RibbonItemSize.Medium, RibbonSplitButtonLayout.Horizontal);
        var check = new RibbonCheckBoxItem { Text = "Keep formatting", Checked = true, SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
        var radio = new RibbonRadioButtonItem { Text = "Original style", Checked = true, SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
        examples.Items.AddRange(small, open, split, check, radio); commands.Groups.Add(examples); ribbon.Tabs.Add(commands);
        var large = new RibbonGroup { Text = "Large layouts — compare with Roomier", CanResize = false };
        var largeButton = new RibbonButtonItem { Text = "Large button", Icon = RibbonStockIcons.Create("open"), SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Large), SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
        largeButton.Click += (_, _) => statusLabel.Text = "Large button invoked."; large.Items.Add(largeButton);
        var dropdown = new RibbonDropDownItem { Text = "Large dropdown", Icon = RibbonStockIcons.Create("open"), SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Large), SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
        dropdown.Items.Add(new RibbonButtonItem { Text = "Open document" }); large.Items.Add(dropdown);
        large.Items.AddRange(CreateCommandSplitExample("Large paste", RibbonItemSize.Large, RibbonSplitButtonLayout.Horizontal),
            CreateCommandSplitExample("Vertical paste", RibbonItemSize.Large, RibbonSplitButtonLayout.Vertical),
            CreateCommandSplitExample("Keep source formatting", RibbonItemSize.Large, RibbonSplitButtonLayout.Vertical));
        commands.Groups.Add(large);

        var application = new RibbonTab { Text = "Application Menu geometry" };
        var appSettings = new RibbonGroup { Text = "File spacing", CanResize = false };
        var appApply = DemoAction("Roomier Application Menu"); var appReset = DemoAction("Reset Application Menu");
        appApply.Click += (_, _) =>
        {
            ribbon.Theme = ribbon.Theme with { ApplicationMenuNavigationMinimumRowHeight = 64, ApplicationMenuPaneMinimumRowHeight = 72,
                ApplicationMenuFooterMinimumRowHeight = 52, ApplicationMenuPaneHorizontalPadding = 22, ApplicationMenuFooterItemGap = 12 };
            statusLabel.Text = "Application Menu spacing applied. Open File to compare its navigation, pane and footer.";
        };
        appReset.Click += (_, _) =>
        {
            ribbon.Theme = ribbon.Theme with { ApplicationMenuNavigationMinimumRowHeight = null, ApplicationMenuPaneMinimumRowHeight = null,
                ApplicationMenuFooterMinimumRowHeight = null, ApplicationMenuPaneHorizontalPadding = null, ApplicationMenuFooterItemGap = null };
            statusLabel.Text = "Application Menu spacing reset; command/editor/menu geometry retained.";
        };
        appSettings.Items.AddRange(appApply, appReset); application.Groups.Add(appSettings);
        var presentation = new RibbonGroup { Text = "Existing File example", CanResize = false };
        var originalFileMode = ribbon.FileMenuMode;
        var show = DemoAction("Open Application Menu"); var restore = DemoAction("Restore startup File mode");
        show.Click += (_, _) => { ribbon.FileMenuMode = RibbonFileMenuMode.CommandMenu; ribbon.FileMenu.ShowDropDown(); };
        restore.Click += (_, _) => ribbon.FileMenuMode = originalFileMode;
        presentation.Items.AddRange(show, restore); application.Groups.Add(presentation);
        ribbon.Tabs.Add(application); ribbon.SelectedTab = application;
    }

    private static RibbonButtonItem DemoAction(string text) => new() { Text = text, SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium),
        SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };

    internal void VerifyPairedDemoContract()
    {
        ribbon.Theme = ribbon.Theme with { EditorMinimumHeight = 54, IllustratedMenuIconSize = 48, CommandSplitArrowWidth = 48 };
        var original = ribbon.Theme; var fileMode = ribbon.FileMenuMode; var mode = ribbon.LayoutMode;
        var file = ribbon.FileMenu.Items.ToArray(); var recent = ribbon.ApplicationMenu.DefaultPane.Items.ToArray();
        var footer = ribbon.ApplicationMenu.FooterItems.ToArray(); var qat = ribbon.QuickAccessItems.ToArray();
        EnablePairedGeometryDemo(); var app = ribbon.SelectedTab!; var commands = ribbon.Tabs.First(t => t.Text == "Command content");
        var largeSources = commands.Groups[2].Items.ToArray();
        if (largeSources.Length != 5 || largeSources.Any(i => i.SizePolicy.Large != RibbonItemSize.Large))
            throw new InvalidOperationException("Large command examples are missing from the command-content tab.");
        commands.Groups[0].Items[0].PerformClick(); app.Groups[0].Items[0].PerformClick();
        if (ribbon.Theme.CommandContentHorizontalPadding != 12 || ribbon.Theme.CommandContentVerticalPadding != 16 ||
            ribbon.Theme.ApplicationMenuNavigationMinimumRowHeight != 64 || ribbon.Theme.ApplicationMenuPaneMinimumRowHeight != 72 ||
            ribbon.Theme.ApplicationMenuFooterMinimumRowHeight != 52 || ribbon.Theme.ApplicationMenuPaneHorizontalPadding != 22 || ribbon.Theme.ApplicationMenuFooterItemGap != 12)
            throw new InvalidOperationException("Paired demo did not apply all seven roles.");
        commands.Groups[1].Items[0].PerformClick(); var small = (RibbonToggleItem)commands.Groups[1].Items[0];
        commands.Groups[0].Items[2].PerformClick(); commands.Groups[0].Items[2].PerformClick();
        app.Groups[0].Items[1].PerformClick();
        if (ribbon.Theme.CommandContentHorizontalPadding != 12 || ribbon.Theme.ApplicationMenuPaneHorizontalPadding is not null)
            throw new InvalidOperationException("Application Menu reset changed command content.");
        commands.Groups[0].Items[1].PerformClick();
        if (ribbon.Theme != original || ribbon.FileMenuMode != fileMode || ribbon.LayoutMode != mode || !small.Checked ||
            !file.SequenceEqual(ribbon.FileMenu.Items) || !recent.SequenceEqual(ribbon.ApplicationMenu.DefaultPane.Items) ||
            !footer.SequenceEqual(ribbon.ApplicationMenu.FooterItems) || !qat.SequenceEqual(ribbon.QuickAccessItems) || !largeSources.SequenceEqual(commands.Groups[2].Items))
            throw new InvalidOperationException("Paired demo changed prior roles, File models, modes, toggle state or QAT ownership.");
    }
}
