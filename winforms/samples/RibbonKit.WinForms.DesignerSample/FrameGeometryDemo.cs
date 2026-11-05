using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

internal static class FrameGeometrySettings
{
    internal static RibbonTheme Corners(RibbonTheme t) => t with { BodyCornerRadius = 14, BackstagePageCornerRadius = 18, PopupCornerRadius = 12 };
    internal static RibbonTheme ResetCorners(RibbonTheme t, int popupRadius) => t with { BodyCornerRadius = null, BackstagePageCornerRadius = null, PopupCornerRadius = popupRadius };
    internal static RibbonTheme BodyShadow(RibbonTheme t) => t with { BodyShadowBlur = 12, BodyShadowVerticalOffset = 4 };
    internal static RibbonTheme ResetBodyShadow(RibbonTheme t) => t with { BodyShadowBlur = null, BodyShadowVerticalOffset = null };
    internal static RibbonTheme PopupShadow(RibbonTheme t) => t with { PopupShadowBlur = 12, PopupShadowVerticalOffset = 4 };
    internal static RibbonTheme ResetPopupShadow(RibbonTheme t) => t with { PopupShadowBlur = null, PopupShadowVerticalOffset = null };
}

public partial class DesignerForm
{
    internal void EnableFrameGeometryDemo()
    {
        int originalPopupRadius = ribbon.Theme.PopupCornerRadius;
        var tab = new RibbonTab { Text = "Frame geometry", KeyTip = "FG" };
        AddChromeSettings(tab, "Panel / popup / page corners", "Rounder corners", "Reset corners", FrameGeometrySettings.Corners, t => FrameGeometrySettings.ResetCorners(t, originalPopupRadius));
        AddChromeSettings(tab, "Panel shadow", "Wider panel shadow", "Reset panel shadow", FrameGeometrySettings.BodyShadow, FrameGeometrySettings.ResetBodyShadow);
        AddChromeSettings(tab, "Popup shadow", "Wider popup shadow", "Reset popup shadow", FrameGeometrySettings.PopupShadow, FrameGeometrySettings.ResetPopupShadow);
        var examples = new RibbonGroup { Text = "Opaque native content", CanResize = false };
        var menu = new RibbonDropDownItem { Text = "Popup example", KeyTip = "P", SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
        menu.Items.AddRange(new RibbonTextBoxItem { Text = "Name", Value = "Retained draft" }, new RibbonCheckBoxItem { Text = "Keep formatting", Checked = true });
        var nested = new RibbonDropDownItem { Text = "More options" }; nested.Items.Add(DemoAction("Nested command")); menu.Items.Add(nested);
        var gallery = new RibbonGalleryItem { Text = "Gallery popup", KeyTip = "G", ColumnCount = 3, MaxVisibleRows = 2, SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
        for (int i = 0; i < 24; i++) gallery.Items.Add(new RibbonGalleryTile { Text = "Style " + (i + 1), Icon = RibbonStockIcons.Create("open") });
        examples.Items.AddRange(menu, gallery); tab.Groups.Add(examples); AddChromeModeToggle(tab); ribbon.Tabs.Add(tab);

        var joins = new RibbonTab { Text = "Frame joins", KeyTip = "FJ" }; var controls = new RibbonGroup { Text = "Connected panel review", CanResize = false };
        var qat = DemoAction("Move QAT"); qat.Click += (_, _) => ribbon.QuickAccessPosition = ribbon.QuickAccessPosition switch
        { RibbonQuickAccessPosition.TabRow => RibbonQuickAccessPosition.BelowRibbon, RibbonQuickAccessPosition.BelowRibbon => RibbonQuickAccessPosition.TitleBar, _ => RibbonQuickAccessPosition.TabRow };
        var minimize = new RibbonToggleItem { Text = "Minimized ribbon", Checked = ribbon.IsMinimized, SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium), SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
        minimize.Click += (_, _) => ribbon.IsMinimized = minimize.Checked;
        var show = new RibbonToggleItem { Text = "Review message", SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium), SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
        RibbonMessage? message = null;
        show.Click += (_, _) =>
        {
            if (message is null)
            {
                message = new RibbonMessage { Title = "Frame review", Text = "Compare the connected body, QAT and message edges." };
                message.Dismissed += (_, _) => show.Checked = false; ribbon.MessageBar.Items.Add(message);
            }
            message.IsOpen = show.Checked;
        };
        controls.Items.AddRange(qat, show, minimize); joins.Groups.Add(controls);
        var pages = new RibbonGroup { Text = "Existing authored pages", CanResize = false }; var originalMode = ribbon.FileMenuMode;
        var open = DemoAction("Open Backstage"); open.Click += (_, _) => { ribbon.FileMenuMode = RibbonFileMenuMode.Backstage; ribbon.IsBackstageOpen = true; };
        var restore = DemoAction("Restore File mode"); restore.Click += (_, _) => ribbon.FileMenuMode = originalMode;
        pages.Items.AddRange(open, restore); joins.Groups.Add(pages); AddChromeModeToggle(joins); ribbon.Tabs.Add(joins); ribbon.SelectedTab = tab;
        statusLabel.Text = "Compare corners and shadows independently. Frame joins contains QAT, message, minimized and existing Backstage review controls.";
    }

    internal void VerifyFrameDemoContract()
    {
        ribbon.Theme = ribbon.Theme with { PopupCornerRadius = 9, GalleryContentPadding = 10, BackstagePagePadding = 18, CommandContentVerticalPadding = 16, KeyTipHorizontalPadding = 6 };
        var original = ribbon.Theme; var qat = ribbon.QuickAccessItems.ToArray(); var pages = ribbon.Backstage.Items.ToArray(); var mode = ribbon.FileMenuMode;
        EnableFrameGeometryDemo(); var tab = ribbon.SelectedTab!;
        for (int i = 0; i < 3; i++) tab.Groups[i].Items[0].PerformClick();
        tab.Groups[0].Items[1].PerformClick();
        if (ribbon.Theme.BodyShadowBlur != 12 || ribbon.Theme.PopupShadowBlur != 12 || ribbon.Theme.PopupCornerRadius != 9) throw new InvalidOperationException("Corner reset changed independent shadows or authored popup radius.");
        tab.Groups[1].Items[1].PerformClick(); if (ribbon.Theme.PopupShadowBlur != 12) throw new InvalidOperationException("Panel reset changed popup shadow."); tab.Groups[2].Items[1].PerformClick();
        if (ribbon.Theme != original || ribbon.FileMenuMode != mode || !ribbon.QuickAccessItems.SequenceEqual(qat) || !ribbon.Backstage.Items.SequenceEqual(pages)) throw new InvalidOperationException("Frame resets changed earlier geometry or authored owners.");
    }
}
