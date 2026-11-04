using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

internal static class ChromeGeometrySettings
{
    internal static RibbonTheme Body(RibbonTheme theme) => theme with { GroupHorizontalPadding = 12, GroupColumnGap = 10, BodyContentHorizontalPadding = 12, GroupSeparatorVerticalInset = 20 };
    internal static RibbonTheme ResetBody(RibbonTheme theme) => theme with { GroupHorizontalPadding = null, GroupColumnGap = null, BodyContentHorizontalPadding = null, GroupSeparatorVerticalInset = null };
    internal static RibbonTheme Header(RibbonTheme theme) => theme with { TabHorizontalPadding = 22, TabMinimumWidth = 90, HeaderIndicatorPadding = 8 };
    internal static RibbonTheme ResetHeader(RibbonTheme theme) => theme with { TabHorizontalPadding = null, TabMinimumWidth = null, HeaderIndicatorPadding = null };
    internal static RibbonTheme Utilities(RibbonTheme theme) => theme with { QuickAccessHorizontalPadding = 8, QuickAccessItemGap = 6, MessageActionGap = 18, MessageTitleTextGap = 12 };
    internal static RibbonTheme ResetUtilities(RibbonTheme theme) => theme with { QuickAccessHorizontalPadding = null, QuickAccessItemGap = null, MessageActionGap = null, MessageTitleTextGap = null };
}

public partial class DesignerForm
{
    internal void EnableChromeGeometryDemo()
    {
        var body = new RibbonTab { Text = "Body and groups" };
        AddChromeSettings(body, "Group / body spacing", "Roomier body and groups", "Reset body and groups", ChromeGeometrySettings.Body, ChromeGeometrySettings.ResetBody);
        var examples = new RibbonGroup { Text = "Resize the window to collapse this group", ReductionMode = RibbonGroupReductionMode.Collapse };
        examples.Items.AddRange(CreateCommandSplitExample("Paste example", RibbonItemSize.Medium, RibbonSplitButtonLayout.Horizontal),
            new RibbonCheckBoxItem { Text = "Keep formatting", Checked = true }, new RibbonTextBoxItem { Text = "Name", Value = "Retained field" });
        var second = new RibbonGroup { Text = "Another group", CanResize = false };
        second.Items.AddRange(DemoAction("First command"), DemoAction("Second command"), DemoAction("Third command"));
        body.Groups.Add(examples); body.Groups.Add(second); AddChromeModeToggle(body); ribbon.Tabs.Add(body);

        var header = new RibbonTab { Text = "Header QAT messages" };
        AddChromeSettings(header, "Tab / File spacing", "Roomier headers", "Reset headers", ChromeGeometrySettings.Header, ChromeGeometrySettings.ResetHeader);
        AddChromeSettings(header, "QAT / message spacing", "Roomier QAT and messages", "Reset QAT and messages", ChromeGeometrySettings.Utilities, ChromeGeometrySettings.ResetUtilities);
        var controls = new RibbonGroup { Text = "Review controls", CanResize = false };
        var position = DemoAction("Move QAT"); position.Click += (_, _) =>
        {
            ribbon.QuickAccessPosition = ribbon.QuickAccessPosition switch { RibbonQuickAccessPosition.TabRow => RibbonQuickAccessPosition.BelowRibbon,
                RibbonQuickAccessPosition.BelowRibbon => RibbonQuickAccessPosition.TitleBar, _ => RibbonQuickAccessPosition.TabRow };
            statusLabel.Text = "QAT position: " + ribbon.QuickAccessPosition + ". Existing sources/order retained.";
        };
        var messageToggle = new RibbonToggleItem { Text = "Review message", SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium), SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
        RibbonMessage? message = null;
        messageToggle.Click += (_, _) =>
        {
            if (message is null)
            {
                message = new RibbonMessage { Title = "Spacing review", Text = "Review the document settings and compare message wrapping." }; message.Action.Text = "Review";
                message.Action.Click += (_, _) => statusLabel.Text = "Review action invoked on the same message.";
                message.Dismissed += (_, _) => messageToggle.Checked = false; ribbon.MessageBar.Items.Add(message);
            }
            message.IsOpen = messageToggle.Checked;
        };
        controls.Items.AddRange(position, messageToggle); header.Groups.Add(controls); AddChromeModeToggle(header);
        ribbon.Tabs.Add(header); ribbon.SelectedTab = header;
    }

    private void AddChromeSettings(RibbonTab tab, string caption, string applyText, string resetText, Func<RibbonTheme, RibbonTheme> apply, Func<RibbonTheme, RibbonTheme> reset)
    {
        var group = new RibbonGroup { Text = caption, CanResize = false }; var a = DemoAction(applyText); var r = DemoAction(resetText);
        a.Click += (_, _) => { ribbon.Theme = apply(ribbon.Theme); statusLabel.Text = applyText + " applied. Compare Mouse/Touch and resize the window."; };
        r.Click += (_, _) => { ribbon.Theme = reset(ribbon.Theme); statusLabel.Text = resetText + ": other spacing, models and current modes retained."; };
        group.Items.AddRange(a, r); tab.Groups.Add(group);
    }

    private void AddChromeModeToggle(RibbonTab tab)
    {
        var group = new RibbonGroup { Text = "Layout mode", CanResize = false };
        var toggle = new RibbonToggleItem { Text = "Simplified layout", Checked = ribbon.LayoutMode == RibbonLayoutMode.Simplified,
            SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium), SimplifiedPlacement = RibbonSimplifiedPlacement.Inline, SimplifiedPriority = int.MaxValue };
        toggle.Click += (_, _) => ribbon.LayoutMode = toggle.Checked ? RibbonLayoutMode.Simplified : RibbonLayoutMode.Classic;
        ribbon.LayoutModeChanged += (_, _) => toggle.Checked = ribbon.LayoutMode == RibbonLayoutMode.Simplified;
        group.Items.Add(toggle); tab.Groups.Add(group);
    }

    internal void VerifyChromeDemoContract()
    {
        ribbon.Theme = ribbon.Theme with { CommandContentHorizontalPadding = 12, ApplicationMenuPaneHorizontalPadding = 22, EditorMinimumHeight = 54 };
        var original = ribbon.Theme; var qat = ribbon.QuickAccessItems.ToArray(); var file = ribbon.FileMenu.Items.ToArray();
        var position = ribbon.QuickAccessPosition; var fileMode = ribbon.FileMenuMode; var mode = ribbon.LayoutMode; var existingMessages = ribbon.MessageBar.Items.ToArray();
        EnableChromeGeometryDemo(); var header = ribbon.SelectedTab!; var body = ribbon.Tabs.Single(t => t.Text == "Body and groups");
        body.Groups[0].Items[0].PerformClick(); header.Groups[0].Items[0].PerformClick(); header.Groups[1].Items[0].PerformClick();
        if (ribbon.Theme.GroupColumnGap != 10 || ribbon.Theme.TabMinimumWidth != 90 || ribbon.Theme.QuickAccessItemGap != 6 || ribbon.Theme.MessageActionGap != 18)
            throw new InvalidOperationException("Chrome presets did not apply.");
        header.Groups[2].Items[1].PerformClick(); var message = ribbon.MessageBar.Items.Last(); message.Action.PerformClick(); message.Dismiss();
        if (((RibbonToggleItem)header.Groups[2].Items[1]).Checked || !ReferenceEquals(message, ribbon.MessageBar.Items.Last())) throw new InvalidOperationException("Demo message identity/dismissal changed.");
        header.Groups[2].Items[1].PerformClick(); if (!message.IsOpen) throw new InvalidOperationException("Message did not reopen.");
        for (int i = 0; i < 3; i++) header.Groups[2].Items[0].PerformClick();
        header.Groups[3].Items[0].PerformClick(); header.Groups[3].Items[0].PerformClick();
        header.Groups[0].Items[1].PerformClick();
        if (ribbon.Theme.GroupColumnGap != 10 || ribbon.Theme.QuickAccessItemGap != 6) throw new InvalidOperationException("Header reset changed other batches.");
        body.Groups[0].Items[1].PerformClick(); header.Groups[1].Items[1].PerformClick();
        ribbon.MessageBar.Items.Remove(message); message.Dispose();
        if (ribbon.Theme != original || ribbon.QuickAccessPosition != position || ribbon.LayoutMode != mode || ribbon.FileMenuMode != fileMode ||
            !qat.SequenceEqual(ribbon.QuickAccessItems) || !file.SequenceEqual(ribbon.FileMenu.Items) || !existingMessages.SequenceEqual(ribbon.MessageBar.Items))
            throw new InvalidOperationException("Chrome demo reset changed prior roles, models, state or modes.");
    }
}
