using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

public partial class DesignerForm
{
    private void EnableGalleryAppearanceDemo()
    {
        var tab = ThemeEffects6ReviewTab;
        if (tab.Groups.Any(group => group.Id == "review-gallery-preview")) return;
        var preview = new RibbonGroup { Id = "review-gallery-preview", Text = "Try built-in galleries", CanResize = false };
        var popup = Make(false); popup.Text = "Grouped gallery"; popup.SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium);
        var inline = Make(true); inline.Text = "Inline styles";
        preview.Items.AddRange(popup, inline);
        var tiles = new RibbonGroup { Id = "review-gallery-tiles", Text = "Gallery tile states" };
        Add(tiles, "Rounded tile states", () => Set((ribbon.Theme.GalleryAppearance ?? new()) with { TileCornerRadius = 10, TileInset = 3 }));
        Add(tiles, "Outlined active tiles", () => Set((ribbon.Theme.GalleryAppearance ?? new()) with { TileBorderWidth = 2, TileBorderColor = Color.DarkBlue }));
        Add(tiles, "Hide tile outlines", () => Set((ribbon.Theme.GalleryAppearance ?? new()) with { TileBorderWidth = 0 }));
        Add(tiles, "Default tile states", () => Set((ribbon.Theme.GalleryAppearance ?? new()) with { TileCornerRadius = null, TileInset = null, TileBorderWidth = null, TileBorderColor = Color.Empty }));
        var headings = new RibbonGroup { Id = "review-gallery-headings", Text = "Gallery headings" };
        Add(headings, "Colored gallery headings", () => Set((ribbon.Theme.GalleryAppearance ?? new()) with { HeaderBackground = new(Color.LightYellow, Color.LightSkyBlue) }));
        Add(headings, "Outlined gallery headings", () => Set((ribbon.Theme.GalleryAppearance ?? new()) with { HeaderCornerRadius = 8, HeaderInset = 2, HeaderBorderWidth = 2, HeaderBorderColor = Color.DarkGreen }));
        Add(headings, "Regular gallery headings", () => Set((ribbon.Theme.GalleryAppearance ?? new()) with { HeaderBold = false }));
        Add(headings, "Default gallery headings", () => Set((ribbon.Theme.GalleryAppearance ?? new()) with { HeaderBackground = null, HeaderCornerRadius = null, HeaderInset = null, HeaderBorderWidth = null, HeaderBorderColor = Color.Empty, HeaderBold = null }));
        Add(headings, "Default all gallery effects", () => ribbon.Theme = ribbon.Theme with { GalleryAppearance = null });
        tab.Groups.AddRange(preview, tiles, headings);
        void Set(RibbonGalleryAppearanceSettings value) => ribbon.Theme = ribbon.Theme with { GalleryAppearance = value };
        void Add(RibbonGroup group, string text, Action action)
        {
            var item = new RibbonButtonItem { Text = text, SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium), SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
            item.Click += (_, _) => action(); group.Items.Add(item);
        }
        RibbonGalleryItem Make(bool inRibbon)
        {
            var gallery = new RibbonGalleryItem { IsInRibbon = inRibbon, InRibbonColumnCount = 2, ColumnCount = 3, MaxVisibleRows = 3, TileWidth = 90, TileHeight = 80, SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
            gallery.Items.AddRange(Enumerable.Range(0, 9).Select(i => new RibbonGalleryTile { Text = "Style " + (i + 1), GroupName = i < 5 ? "Document styles" : "More styles", Icon = RibbonStockIcons.Create(i % 2 == 0 ? "save" : "open") }).ToArray()); gallery.SelectedItem = (RibbonGalleryTile)gallery.Items[0]; return gallery;
        }
    }
    internal void VerifyGalleryAppearanceDemo(string output)
    {
        StartPosition = FormStartPosition.Manual; Location = new(-16000, -16000); ShowInTaskbar = false; EnableOffice2019ThemeDemo(); Show(); Application.DoEvents();
        var tab = ThemeEffects6ReviewTab; ribbon.SelectedTab = tab;
        if (tab.Text != "Theme effects 6" || ThemeEffects5ReviewTab.Groups.Count != 6 || ThemeEffects4ReviewTab.Groups.Count != 7 || ThemeEffects3ReviewTab.Groups.Count != 4 || ThemeEffects2ReviewTab.Groups.Count != 5 || ribbon.Tabs.Single(t => t.Text == "Office 2019 Light").Groups.Count != 7) throw new InvalidOperationException("Gallery controls displaced earlier tabs.");
        var tiles = tab.Groups.Single(g => g.Id == "review-gallery-tiles"); var headings = tab.Groups.Single(g => g.Id == "review-gallery-headings");
        tiles.Items[0].PerformClick(); tiles.Items[1].PerformClick(); foreach (var item in headings.Items.Take(3)) item.PerformClick(); var a = ribbon.Theme.GalleryAppearance!;
        if (a.TileInset != 3 || a.TileBorderWidth != 2 || a.HeaderBackground is null || a.HeaderBorderWidth != 2 || a.HeaderBold != false) throw new InvalidOperationException("Gallery buttons bypassed public settings.");
        tiles.Items[2].PerformClick(); if (ribbon.Theme.GalleryAppearance!.TileBorderWidth != 0) throw new InvalidOperationException("Tile outline hide failed.");
        tiles.Items[3].PerformClick(); if (ribbon.Theme.GalleryAppearance!.TileInset is not null || ribbon.Theme.GalleryAppearance.HeaderBold != false) throw new InvalidOperationException("Tile reset cleared headings.");
        headings.Items[3].PerformClick(); if (ribbon.Theme.GalleryAppearance!.HeaderBold is not null) throw new InvalidOperationException("Heading reset failed.");
        var gallery = tab.Groups.Single(g => g.Id == "review-gallery-preview").Items.OfType<RibbonGalleryItem>().First(); var selected = gallery.SelectedItem; gallery.ShowDropDown(); if (!gallery.IsDropDownOpen) throw new InvalidOperationException("Built-in grouped preview failed."); gallery.CloseDropDown();
        headings.Items[4].PerformClick(); if (ribbon.Theme.GalleryAppearance is not null || !ReferenceEquals(selected, gallery.SelectedItem)) throw new InvalidOperationException("Gallery reset changed selection.");
        int count = tab.Groups.Count; EnableGalleryAppearanceDemo(); if (tab.Groups.Count != count) throw new InvalidOperationException("Gallery groups duplicated.");
        using var image = new Bitmap(ribbon.Width, ribbon.Height); ribbon.DrawToBitmap(image, ribbon.ClientRectangle); image.Save(Path.Combine(output, "review-tab.png")); Close();
    }
}
