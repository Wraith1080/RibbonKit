using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

internal static class GalleryBackstageGeometrySettings
{
    internal static RibbonTheme Gallery(RibbonTheme t) => t with { GalleryContentPadding = 10, GalleryUtilityWidth = 40, GalleryTilePadding = 10, GalleryHeadingHorizontalPadding = 16, GalleryHeadingVerticalPadding = 12 };
    internal static RibbonTheme ResetGallery(RibbonTheme t) => t with { GalleryContentPadding = null, GalleryUtilityWidth = null, GalleryTilePadding = null, GalleryHeadingHorizontalPadding = null, GalleryHeadingVerticalPadding = null };
    internal static RibbonTheme Navigation(RibbonTheme t) => t with { BackstageMinimumRowHeight = 62, BackstageNavigationPadding = 14, BackstageBackButtonSize = 52, BackstageBackButtonInset = 28, BackstageBackButtonBottomGap = 32, BackstageIconTextGap = 22 };
    internal static RibbonTheme ResetNavigation(RibbonTheme t) => t with { BackstageMinimumRowHeight = null, BackstageNavigationPadding = null, BackstageBackButtonSize = null, BackstageBackButtonInset = null, BackstageBackButtonBottomGap = null, BackstageIconTextGap = null };
}

public partial class DesignerForm
{
    internal void EnableGalleryBackstageGeometryDemo()
    {
        var tab = new RibbonTab { Text = "Gallery geometry" };
        AddChromeSettings(tab, "Gallery spacing", "Roomier galleries", "Reset galleries", GalleryBackstageGeometrySettings.Gallery, GalleryBackstageGeometrySettings.ResetGallery);
        var group = new RibbonGroup { Text = "Built-in tiles / headings", ReductionMode = RibbonGroupReductionMode.Collapse };
        var gallery = GeometryGallery("Built-in styles"); group.Items.Add(gallery); tab.Groups.Add(group);
        var native = new RibbonGroup { Text = "Native cards / same geometry", ReductionMode = RibbonGroupReductionMode.Collapse };
        var cards = GeometryGallery("Native styles"); cards.InRibbonPresenterType = typeof(GalleryStylesStripControl); cards.DropDownPresenterType = typeof(GalleryStylesControl); cards.UseDropDownPresenterInQuickAccess = true;
        native.Items.Add(cards); tab.Groups.Add(native);
        var controls = new RibbonGroup { Text = "Projection review", CanResize = false };
        RibbonQuickAccessDropDownReference? reference = null; RibbonQuickAccessGroupReference? borrowed = null;
        Disposed += (_, _) => { reference?.Dispose(); borrowed?.Dispose(); };
        var qat = new RibbonToggleItem { Text = "Gallery in QAT", SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium) };
        qat.Click += (_, _) =>
        {
            if (qat.Checked)
            {
                reference ??= new RibbonQuickAccessDropDownReference { Source = cards }; borrowed ??= new RibbonQuickAccessGroupReference { Source = group };
                ribbon.QuickAccessItems.Add(reference); ribbon.QuickAccessItems.Add(borrowed);
            }
            else { ribbon.QuickAccessItems.Remove(reference!); ribbon.QuickAccessItems.Remove(borrowed!); }
            statusLabel.Text = "Review direct native gallery and borrowed built-in group in QAT; both retain source selection.";
        };
        var open = DemoAction("Open built-in gallery"); open.Click += (_, _) => gallery.ShowDropDown();
        controls.Items.AddRange(qat, open); tab.Groups.Add(controls); AddChromeModeToggle(tab); ribbon.Tabs.Add(tab);

        var back = new RibbonTab { Text = "Backstage geometry" };
        AddChromeSettings(back, "Navigation spacing", "Roomier navigation", "Reset navigation", GalleryBackstageGeometrySettings.Navigation, GalleryBackstageGeometrySettings.ResetNavigation);
        AddChromeSettings(back, "Native page container", "Inset page content", "Reset page inset", t => t with { BackstagePagePadding = 18 }, t => t with { BackstagePagePadding = null });
        var actions = new RibbonGroup { Text = "Existing authored pages", CanResize = false };
        var originalMode = ribbon.FileMenuMode;
        var show = DemoAction("Open Backstage"); show.Click += (_, _) => { ribbon.FileMenuMode = RibbonFileMenuMode.Backstage; ribbon.IsBackstageOpen = true; };
        var restore = DemoAction("Restore File mode"); restore.Click += (_, _) => ribbon.FileMenuMode = originalMode;
        actions.Items.AddRange(show, restore); back.Groups.Add(actions); ribbon.Tabs.Add(back); ribbon.SelectedTab = tab;
        statusLabel.Text = "Gallery / Backstage geometry: compare Roomier and Reset, native cards, QAT and existing Info/Print pages.";
    }

    private static RibbonGalleryItem GeometryGallery(string text)
    {
        var gallery = new RibbonGalleryItem { Text = text, IsInRibbon = true, InRibbonColumnCount = 2, ColumnCount = 3, MaxVisibleRows = 2, TileWidth = 100,
            SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
        for (int i = 0; i < 18; i++) gallery.Items.Add(new RibbonGalleryTile { Text = "Style " + (i + 1), GroupName = i < 9 ? "Document styles" : "Other styles", Icon = RibbonStockIcons.Create("open") });
        gallery.SelectedItem = (RibbonGalleryTile)gallery.Items[0]; return gallery;
    }

    internal void VerifyGalleryBackstageDemoContract()
    {
        ribbon.Theme = ribbon.Theme with { CommandContentVerticalPadding = 16, MenuLeadingPadding = 14, QuickAccessItemGap = 6 };
        var original = ribbon.Theme; var sources = ribbon.QuickAccessItems.ToArray(); var pages = ribbon.Backstage.Items.ToArray(); var mode = ribbon.FileMenuMode;
        EnableGalleryBackstageGeometryDemo(); var galleryTab = ribbon.SelectedTab!; var back = ribbon.Tabs.Single(t => t.Text == "Backstage geometry");
        galleryTab.Groups[0].Items[0].PerformClick(); back.Groups[0].Items[0].PerformClick(); back.Groups[1].Items[0].PerformClick();
        galleryTab.Groups[0].Items[1].PerformClick();
        if (ribbon.Theme.GalleryContentPadding is not null || ribbon.Theme.BackstageMinimumRowHeight != 62 || ribbon.Theme.BackstagePagePadding != 18) throw new InvalidOperationException("Gallery reset changed Backstage roles.");
        back.Groups[0].Items[1].PerformClick();
        if (ribbon.Theme.BackstagePagePadding != 18) throw new InvalidOperationException("Navigation reset changed the page inset.");
        back.Groups[1].Items[1].PerformClick(); galleryTab.Groups[3].Items[0].PerformClick(); galleryTab.Groups[3].Items[0].PerformClick();
        if (ribbon.Theme != original || ribbon.FileMenuMode != mode || !ribbon.QuickAccessItems.SequenceEqual(sources) || !ribbon.Backstage.Items.SequenceEqual(pages)) throw new InvalidOperationException("Demo reset changed earlier spacing, File routing or authored source ownership.");
    }
}
