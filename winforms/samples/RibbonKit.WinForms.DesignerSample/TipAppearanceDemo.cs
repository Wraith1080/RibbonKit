using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

public partial class DesignerForm
{
    private void EnableTipAppearanceDemo()
    {
        var tab = ThemeEffects7ReviewTab; if (tab.Groups.Any(g => g.Id == "review-tip-appearance")) return;
        var preview = new RibbonGroup { Id = "review-tip-preview", Text = "Try tips and badges" };
        RibbonFileMenuMode? priorMode = null;
        preview.Items.AddRange(new RibbonButtonItem { Text = "Hover for description", KeyTip = "TP", ScreenTipTitle = "Search the current document", ScreenTipDescription = "Hover here to review title weight and shared popup styling. Press Alt or F10 to review keyboard badges.", SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium), SimplifiedPlacement = RibbonSimplifiedPlacement.Inline },
            new RibbonButtonItem { Text = "Disabled explanation", Enabled = false, ScreenTipTitle = "This command is unavailable", ScreenTipDescription = "Disabled commands can explain themselves without becoming active.", SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium), SimplifiedPlacement = RibbonSimplifiedPlacement.Inline });
        var restore = new RibbonButtonItem { Text = "Restore File mode", SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium) }; restore.Click += (_, _) => { ribbon.IsBackstageOpen = false; if (priorMode is { } mode) ribbon.FileMenuMode = mode; priorMode = null; }; preview.Items.Add(restore);
        var appearance = new RibbonGroup { Id = "review-tip-appearance", Text = "Tip presentation" };
        Add("Regular tip titles", () => Set((ribbon.Theme.TipAppearance ?? new()) with { ScreenTipTitleBold = false }));
        Add("Thicker badge outlines", () => Set((ribbon.Theme.TipAppearance ?? new()) with { KeyTipInset = 2, KeyTipBorderWidth = 3 }));
        Add("Hide badge outlines", () => Set((ribbon.Theme.TipAppearance ?? new()) with { KeyTipBorderWidth = 0 }));
        Add("Inset Backstage badges", () => { Set((ribbon.Theme.TipAppearance ?? new()) with { BackstageKeyTipRightClearance = 24, BackstageKeyTipHorizontalClearance = 32, BackstageKeyTipVerticalClearance = 12 }); priorMode ??= ribbon.FileMenuMode; ribbon.FileMenuMode = RibbonFileMenuMode.Backstage; ribbon.IsBackstageOpen = true; });
        Add("Default badge placement", () => Set((ribbon.Theme.TipAppearance ?? new()) with { BackstageKeyTipRightClearance = null, BackstageKeyTipHorizontalClearance = null, BackstageKeyTipVerticalClearance = null }));
        Add("Default tip presentation", () => ribbon.Theme = ribbon.Theme with { TipAppearance = null });
        tab.Groups.AddRange(preview, appearance);
        void Set(RibbonTipAppearanceSettings a) => ribbon.Theme = ribbon.Theme with { TipAppearance = a };
        void Add(string text, Action action) { var item = new RibbonButtonItem { Text = text, SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium), SimplifiedPlacement = RibbonSimplifiedPlacement.Inline }; item.Click += (_, _) => action(); appearance.Items.Add(item); }
    }
    internal void VerifyTipAppearanceDemo(string output)
    {
        StartPosition = FormStartPosition.Manual; Location = new(-16000, -16000); Width = 1800; ShowInTaskbar = false; EnableOffice2019ThemeDemo(); Show(); Application.DoEvents(); var tab = ThemeEffects7ReviewTab; ribbon.SelectedTab = tab;
        if (tab.Text != "Theme effects 7" || tab.Groups.Count != 6 || ThemeEffects6ReviewTab.Groups.Count != 3 || ThemeEffects5ReviewTab.Groups.Count != 6 || ThemeEffects4ReviewTab.Groups.Count != 7 || ThemeEffects3ReviewTab.Groups.Count != 4 || ThemeEffects2ReviewTab.Groups.Count != 5 || ribbon.Tabs.Single(t => t.Text == "Office 2019 Light").Groups.Count != 7) throw new InvalidOperationException("Tip controls displaced earlier review groups.");
        var group = tab.Groups.Single(g => g.Id == "review-tip-appearance"); ribbon.Theme = ribbon.Theme with { KeyTipBorder = Color.Navy, KeyTipHorizontalPadding = 6, ScreenTipContentPadding = 18, GalleryContent = new() { LabelMaxLines = 3 } };
        group.Items[0].PerformClick(); group.Items[1].PerformClick(); var mode = ribbon.FileMenuMode; group.Items[3].PerformClick();
        if (ribbon.Theme.TipAppearance is not { ScreenTipTitleBold: false, KeyTipInset: 2, KeyTipBorderWidth: 3, BackstageKeyTipRightClearance: 24 } || !ribbon.IsBackstageOpen) throw new InvalidOperationException("Tip presets bypassed public settings.");
        tab.Groups.Single(g => g.Id == "review-tip-preview").Items[2].PerformClick(); if (ribbon.IsBackstageOpen || ribbon.FileMenuMode != mode) throw new InvalidOperationException("Tip preview did not restore File mode."); group.Items[4].PerformClick(); if (ribbon.Theme.TipAppearance!.BackstageKeyTipRightClearance is not null || ribbon.Theme.TipAppearance.KeyTipBorderWidth != 3) throw new InvalidOperationException("Placement reset cleared other tip roles.");
        group.Items[2].PerformClick(); if (ribbon.Theme.TipAppearance!.KeyTipBorderWidth != 0) throw new InvalidOperationException("Outline hide failed."); group.Items[5].PerformClick();
        if (ribbon.Theme.TipAppearance is not null || ribbon.Theme.KeyTipHorizontalPadding != 6 || ribbon.Theme.ScreenTipContentPadding != 18 || ribbon.Theme.KeyTipBorder != Color.Navy || ribbon.Theme.GalleryContent?.LabelMaxLines != 3) throw new InvalidOperationException("Tip Reset cleared existing settings.");
        EnableTipAppearanceDemo(); if (tab.Groups.Count != 6) throw new InvalidOperationException("Tip groups duplicated."); using var image = new Bitmap(ribbon.Width, ribbon.Height); ribbon.DrawToBitmap(image, ribbon.ClientRectangle); image.Save(Path.Combine(output, "review-tab.png")); Close();
    }
}
