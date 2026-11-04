using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

internal static class TipCaptionGeometrySettings
{
    internal static RibbonTheme ScreenTips(RibbonTheme t) => t with { ScreenTipMaximumWidth = 420, ScreenTipContentPadding = 20, ScreenTipTitleGap = 12, ScreenTipPlacementGap = 16 };
    internal static RibbonTheme ResetScreenTips(RibbonTheme t) => t with { ScreenTipMaximumWidth = null, ScreenTipContentPadding = null, ScreenTipTitleGap = null, ScreenTipPlacementGap = null };
    internal static RibbonTheme KeyTips(RibbonTheme t) => t with { KeyTipHorizontalPadding = 6, KeyTipVerticalPadding = 4 };
    internal static RibbonTheme ResetKeyTips(RibbonTheme t) => t with { KeyTipHorizontalPadding = null, KeyTipVerticalPadding = null };
    internal static RibbonTheme Caption(RibbonTheme t) => t with { CaptionLeadingPadding = 22, CaptionIconTextGap = 18, CaptionQuickAccessTextGap = 18, CaptionTitleReservationWidth = 180 };
    internal static RibbonTheme ResetCaption(RibbonTheme t) => t with { CaptionLeadingPadding = null, CaptionIconTextGap = null, CaptionQuickAccessTextGap = null, CaptionTitleReservationWidth = null };
}

public partial class DesignerForm
{
    internal void EnableTipCaptionGeometryDemo()
    {
        var tips = new RibbonTab { Text = "Tips geometry", KeyTip = "TG" };
        AddChromeSettings(tips, "Passive tip spacing", "Roomier ScreenTips", "Reset ScreenTips", TipCaptionGeometrySettings.ScreenTips, TipCaptionGeometrySettings.ResetScreenTips);
        AddChromeSettings(tips, "Keyboard badges", "Roomier KeyTips", "Reset KeyTips", TipCaptionGeometrySettings.KeyTips, TipCaptionGeometrySettings.ResetKeyTips);
        var group = new RibbonGroup { Text = "Hover; F10 then TG for badges", CanResize = false };
        var example = DemoAction("Hover here"); example.KeyTip = "H"; example.ScreenTipTitle = "Find matching text throughout the current document";
        example.ScreenTipDescription = "Compare the content padding, title gap and maximum width. The passive tip preserves native editor selection and stays within the monitor work area.";
        var menu = new RibbonDropDownItem { Text = "Tip menu", KeyTip = "M", SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
        menu.Items.AddRange(new RibbonButtonItem { Text = "Hover over this command", KeyTip = "C", ScreenTipDescription = "Ordinary popup tips and keyboard badges use the same selected spacing." }, new RibbonTextBoxItem { Text = "Name", Value = "Retained draft", KeyTip = "N" });
        group.Items.AddRange(example, menu); tips.Groups.Add(group);
        var backstage = new RibbonGroup { Text = "Existing page badges", CanResize = false }; var mode = ribbon.FileMenuMode;
        var open = DemoAction("Backstage KeyTips"); open.Click += (_, _) => { ribbon.FileMenuMode = RibbonFileMenuMode.Backstage; ribbon.IsBackstageOpen = true; };
        var restore = DemoAction("Restore File mode"); restore.Click += (_, _) => ribbon.FileMenuMode = mode; backstage.Items.AddRange(open, restore); tips.Groups.Add(backstage); AddChromeModeToggle(tips); ribbon.Tabs.Add(tips);
        var caption = new RibbonTab { Text = "Caption geometry", KeyTip = "CG" };
        AddChromeSettings(caption, "Caption content spacing", "Roomier caption", "Reset caption", TipCaptionGeometrySettings.Caption, TipCaptionGeometrySettings.ResetCaption);
        var qat = new RibbonGroup { Text = "TitleBar QAT allocation", CanResize = false }; var original = ribbon.QuickAccessPosition;
        var toggle = new RibbonToggleItem { Text = "Use caption QAT", Checked = original == RibbonQuickAccessPosition.TitleBar, SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium), SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
        toggle.Click += (_, _) => ribbon.QuickAccessPosition = toggle.Checked ? RibbonQuickAccessPosition.TitleBar : original == RibbonQuickAccessPosition.TitleBar ? RibbonQuickAccessPosition.TabRow : original;
        qat.Items.Add(toggle); caption.Groups.Add(qat); ribbon.Tabs.Add(caption); ribbon.SelectedTab = tips;
        statusLabel.Text = "Hover for passive tips. F10 then TG shows command badges; F10 in Backstage shows page badges. Caption settings retain native button/drag/resize behavior.";
    }

    internal void VerifyTipCaptionDemoContract()
    {
        ribbon.Theme = ribbon.Theme with { GalleryContentPadding = 10, BackstagePagePadding = 18, CommandContentVerticalPadding = 16 }; var original = ribbon.Theme;
        var qat = ribbon.QuickAccessItems.ToArray(); var pages = ribbon.Backstage.Items.ToArray(); var file = ribbon.FileMenuMode;
        EnableTipCaptionGeometryDemo(); var tips = ribbon.SelectedTab!; var caption = ribbon.Tabs.Single(t => t.Text == "Caption geometry");
        tips.Groups[0].Items[0].PerformClick(); tips.Groups[1].Items[0].PerformClick(); caption.Groups[0].Items[0].PerformClick();
        tips.Groups[0].Items[1].PerformClick(); if (ribbon.Theme.KeyTipHorizontalPadding != 6 || ribbon.Theme.CaptionLeadingPadding != 22) throw new InvalidOperationException("ScreenTip reset changed another role.");
        tips.Groups[1].Items[1].PerformClick(); if (ribbon.Theme.CaptionLeadingPadding != 22) throw new InvalidOperationException("KeyTip reset changed caption roles.");
        caption.Groups[0].Items[1].PerformClick();
        if (ribbon.Theme != original || ribbon.FileMenuMode != file || !ribbon.QuickAccessItems.SequenceEqual(qat) || !ribbon.Backstage.Items.SequenceEqual(pages)) throw new InvalidOperationException("Tip/caption demo reset changed prior geometry or authored owners.");
    }
}
