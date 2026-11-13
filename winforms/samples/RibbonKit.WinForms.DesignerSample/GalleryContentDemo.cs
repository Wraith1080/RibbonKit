using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

public partial class DesignerForm
{
    private void EnableGalleryContentDemo()
    {
        var tab = ThemeEffects7ReviewTab; if (tab.Groups.Any(g => g.Id == "review-gallery-content")) return;
        var preview = new RibbonGroup { Id = "review-gallery-content-preview", Text = "Try gallery content", CanResize = false };
        var popup = Make(false); popup.Text = "Grouped content"; popup.SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium); var inline = Make(true); inline.Text = "Inline content"; preview.Items.AddRange(popup, inline);
        var content = new RibbonGroup { Id = "review-gallery-content", Text = "Gallery artwork and labels" };
        Add(content, "Three label lines", () => Set((ribbon.Theme.GalleryContent ?? new()) with { LabelMaxLines = 3 }));
        Add(content, "Hide gallery labels", () => Set((ribbon.Theme.GalleryContent ?? new()) with { LabelMaxLines = 0 }));
        Add(content, "Bottom-right labels", () => Set((ribbon.Theme.GalleryContent ?? new()) with { LabelAlignment = ContentAlignment.BottomRight }));
        Add(content, "Roomier artwork", () => Set((ribbon.Theme.GalleryContent ?? new()) with { ArtworkHorizontalInset = 14, ArtworkTopInset = 10, ArtworkHeightReserve = 30 }));
        Add(content, "Default artwork and labels", () => Set((ribbon.Theme.GalleryContent ?? new()) with { LabelMaxLines = null, LabelAlignment = null, ArtworkHorizontalInset = null, ArtworkTopInset = null, ArtworkHeightReserve = null }));
        var frames = new RibbonGroup { Id = "review-gallery-frames", Text = "Gallery inline frames" };
        Add(frames, "Rounded gallery frame", () => Appearance((ribbon.Theme.GalleryAppearance ?? new()) with { StripCornerRadius = 12, StripInset = 2, StripBorderWidth = 2, StripBorderColor = Color.DarkBlue, StripBackground = new(Color.LightYellow) }));
        Add(frames, "Outlined utility states", () => Appearance((ribbon.Theme.GalleryAppearance ?? new()) with { UtilityCornerRadius = 4, UtilityInset = 1, UtilityBorderWidth = 2, UtilityBorderColor = Color.DarkGreen }));
        Add(frames, "Hide frame outline", () => Appearance((ribbon.Theme.GalleryAppearance ?? new()) with { StripBorderWidth = 0 }));
        Add(frames, "Default frame and utilities", () => Appearance((ribbon.Theme.GalleryAppearance ?? new()) with { StripCornerRadius = null, StripInset = null, StripBorderWidth = null, StripBorderColor = Color.Empty, StripBackground = null,
            UtilityCornerRadius = null, UtilityInset = null, UtilityBorderWidth = null, UtilityBorderColor = Color.Empty }));
        var glyphs = new RibbonGroup { Id = "review-gallery-glyphs", Text = "Gallery utility glyphs" };
        Add(glyphs, "Larger expand glyph", () => Set((ribbon.Theme.GalleryContent ?? new()) with { ExpandGlyphHeight = 24, ExpandBarWidth = 14, ExpandBarInset = 2, ExpandBarStrokeWidth = 3, UtilityGlyphColor = Color.DarkMagenta }));
        Add(glyphs, "Hide expand bar", () => Set((ribbon.Theme.GalleryContent ?? new()) with { ExpandBarWidth = 0 }));
        Add(glyphs, "Hide utility glyphs", () => Set((ribbon.Theme.GalleryContent ?? new()) with { UtilityGlyphColor = Color.Transparent }));
        Add(glyphs, "Default utility glyphs", () => Set((ribbon.Theme.GalleryContent ?? new()) with { ExpandGlyphHeight = null, ExpandBarWidth = null, ExpandBarInset = null, ExpandBarStrokeWidth = null, UtilityGlyphColor = Color.Empty }));
        Add(glyphs, "Default all gallery content", () => ribbon.Theme = ribbon.Theme with { GalleryContent = null });
        tab.Groups.AddRange(preview, content, frames, glyphs);
        void Set(RibbonGalleryContentSettings value) => ribbon.Theme = ribbon.Theme with { GalleryContent = value };
        void Appearance(RibbonGalleryAppearanceSettings value) => ribbon.Theme = ribbon.Theme with { GalleryAppearance = value };
        void Add(RibbonGroup group, string text, Action action) { var item = new RibbonButtonItem { Text = text, SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium), SimplifiedPlacement = RibbonSimplifiedPlacement.Inline }; item.Click += (_, _) => action(); group.Items.Add(item); }
        RibbonGalleryItem Make(bool inRibbon)
        {
            var gallery = new RibbonGalleryItem { IsInRibbon = inRibbon, InRibbonColumnCount = 2, ColumnCount = 3, MaxVisibleRows = 3, TileWidth = 90, TileHeight = 80, SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
            gallery.Items.AddRange(Enumerable.Range(0, 9).Select(i => new RibbonGalleryTile { Text = "Document style variation " + (i + 1), GroupName = i < 5 ? "Document styles" : "More styles", Icon = RibbonStockIcons.Create(i % 2 == 0 ? "save" : "open") }).ToArray()); gallery.SelectedItem = (RibbonGalleryTile)gallery.Items[0]; return gallery;
        }
    }
    internal void VerifyGalleryContentDemo(string output)
    {
        StartPosition = FormStartPosition.Manual; Location = new(-16000, -16000); ShowInTaskbar = false; EnableOffice2019ThemeDemo(); Show(); Application.DoEvents(); var tab = ThemeEffects7ReviewTab; ribbon.SelectedTab = tab;
        if (tab.Text != "Theme effects 7" || ThemeEffects6ReviewTab.Groups.Count != 3 || ThemeEffects5ReviewTab.Groups.Count != 6 || ThemeEffects4ReviewTab.Groups.Count != 7 || ThemeEffects3ReviewTab.Groups.Count != 4 || ThemeEffects2ReviewTab.Groups.Count != 5 || ribbon.Tabs.Single(t => t.Text == "Office 2019 Light").Groups.Count != 7) throw new InvalidOperationException("Gallery content displaced an earlier tab.");
        var content = tab.Groups.Single(g => g.Id == "review-gallery-content"); var frames = tab.Groups.Single(g => g.Id == "review-gallery-frames"); var glyphs = tab.Groups.Single(g => g.Id == "review-gallery-glyphs");
        ribbon.Theme = ribbon.Theme with { GalleryAppearance = new() { HeaderBold = false, TileCornerRadius = 10 } };
        content.Items[0].PerformClick(); content.Items[2].PerformClick(); content.Items[3].PerformClick(); frames.Items[0].PerformClick(); frames.Items[1].PerformClick(); glyphs.Items[0].PerformClick();
        var a = ribbon.Theme.GalleryContent!; if (a.LabelMaxLines != 3 || a.LabelAlignment != ContentAlignment.BottomRight || a.ArtworkTopInset != 10 || a.ExpandBarStrokeWidth != 3 || ribbon.Theme.GalleryAppearance!.StripBorderWidth != 2) throw new InvalidOperationException("Gallery content presets bypassed public settings.");
        content.Items[1].PerformClick(); if (ribbon.Theme.GalleryContent!.LabelMaxLines != 0) throw new InvalidOperationException("Label hide failed."); content.Items[4].PerformClick(); if (ribbon.Theme.GalleryContent!.ExpandGlyphHeight != 24) throw new InvalidOperationException("Label reset cleared glyph settings.");
        frames.Items[2].PerformClick(); if (ribbon.Theme.GalleryAppearance!.StripBorderWidth != 0) throw new InvalidOperationException("Frame hide failed."); frames.Items[3].PerformClick(); if (ribbon.Theme.GalleryAppearance!.HeaderBold != false || ribbon.Theme.GalleryAppearance.TileCornerRadius != 10) throw new InvalidOperationException("Frame reset cleared accepted tile/headings.");
        glyphs.Items[1].PerformClick(); glyphs.Items[2].PerformClick(); if (ribbon.Theme.GalleryContent!.ExpandBarWidth != 0 || ribbon.Theme.GalleryContent.UtilityGlyphColor != Color.Transparent) throw new InvalidOperationException("Glyph hide failed."); glyphs.Items[3].PerformClick(); glyphs.Items[4].PerformClick(); if (ribbon.Theme.GalleryContent is not null || ribbon.Theme.GalleryAppearance!.HeaderBold != false) throw new InvalidOperationException("Content group reset cleared appearance.");
        var popup = tab.Groups.Single(g => g.Id == "review-gallery-content-preview").Items.OfType<RibbonGalleryItem>().First(); var selected = popup.SelectedItem; popup.ShowDropDown(); if (!popup.IsDropDownOpen) throw new InvalidOperationException("Content preview did not open."); popup.CloseDropDown(); if (!ReferenceEquals(selected, popup.SelectedItem)) throw new InvalidOperationException("Preview changed source selection.");
        int count = tab.Groups.Count; EnableGalleryContentDemo(); if (tab.Groups.Count != count) throw new InvalidOperationException("Content groups duplicated."); using var image = new Bitmap(ribbon.Width, ribbon.Height); ribbon.DrawToBitmap(image, ribbon.ClientRectangle); image.Save(Path.Combine(output, "review-tab.png")); Close();
    }
}
