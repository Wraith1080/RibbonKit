using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

public partial class DesignerForm
{
    private void EnableCaptionAppearanceDemo()
    {
        var tab = ThemeEffects10ReviewTab;
        if (tab.Groups.Any(g => g.Id == "review-caption-tint")) return;
        var tint = new RibbonGroup { Id = "review-caption-tint", Text = "Backstage caption tint" };
        Add(tint, "Enable / disable caption tint", () => Set(Current() with { CaptionTintEnabled = !(Current().CaptionTintEnabled ?? ribbon.Theme.BackstageStyle == RibbonBackstageStyle.Modern) }));
        Add(tint, "Blue caption tint", () => Set(Current() with { CaptionTintColor = Color.Navy }));
        Add(tint, "Translucent purple tint", () => Set(Current() with { CaptionTintColor = Color.FromArgb(128, Color.Purple) }));
        Add(tint, "Half-strength caption tint", () => Set(Current() with { CaptionTintOpacity = .5 }));
        Add(tint, "Hide caption tint", () => Set(Current() with { CaptionTintOpacity = 0 }));
        Add(tint, "Default caption tint", () => Set(Current() with { CaptionTintEnabled = null, CaptionTintColor = Color.Empty, CaptionTintOpacity = null }));
        var preview = new RibbonGroup { Id = "review-caption-preview", Text = "Try caption and title" };
        Add(preview, "Separate / full-height caption", () => ribbon.Theme = ribbon.Theme with { BackstageExtendsIntoTitleBar = !ribbon.Theme.BackstageExtendsIntoTitleBar });
        Add(preview, "Open caption review", () =>
        {
            var previous = ribbon.FileMenuMode;
            ribbon.FileMenuMode = RibbonFileMenuMode.Backstage;
            EventHandler? restore = null;
            restore = (_, _) => { if (ribbon.IsBackstageOpen) return; ribbon.IsBackstageOpenChanged -= restore; ribbon.FileMenuMode = previous; };
            ribbon.IsBackstageOpenChanged += restore;
            ribbon.IsBackstageOpen = true;
            if (!ribbon.IsBackstageOpen) { ribbon.IsBackstageOpenChanged -= restore; ribbon.FileMenuMode = previous; }
        });
        Add(preview, "Authored caption ink", () => ribbon.Theme = ribbon.Theme with { TitleBarForeground = Color.DarkOrange });
        Add(preview, "Default caption ink", () => ribbon.Theme = ribbon.Theme with { TitleBarForeground = Color.Empty });
        tab.Groups.AddRange(tint, preview);
        RibbonBackstageAppearanceSettings Current() => ribbon.Theme.BackstageAppearance ?? new();
        void Set(RibbonBackstageAppearanceSettings value) => ribbon.Theme = ribbon.Theme with { BackstageAppearance = value };
        void Add(RibbonGroup group, string text, Action action)
        {
            var item = new RibbonButtonItem { Text = text, ScreenTipDescription = "For tint, choose a separate neutral caption and enable tint. Authored Form/theme caption colors take precedence. Open Backstage and close with Back or Escape to review the title. Tint Reset preserves the other Backstage effects.", SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium), SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
            item.Click += (_, _) => action(); group.Items.Add(item);
        }
    }
    internal void VerifyCaptionAppearanceDemo(string output)
    {
        Directory.CreateDirectory(output); StartPosition = FormStartPosition.Manual; Location = new(-16000, -16000); Width = 1900; ShowInTaskbar = false;
        EnableOffice2019ThemeDemo(); Show(); Application.DoEvents(); var tab = ThemeEffects10ReviewTab; ribbon.SelectedTab = tab;
        if (tab.Groups.Count != 5 || ThemeEffects9ReviewTab.Groups.Count != 6 || ThemeEffects8ReviewTab.Groups.Count != 5 || ThemeEffects7ReviewTab.Groups.Count != 8 || ThemeEffects6ReviewTab.Groups.Count != 3 || ThemeEffects5ReviewTab.Groups.Count != 6 || ThemeEffects4ReviewTab.Groups.Count != 7 || ThemeEffects3ReviewTab.Groups.Count != 4 || ThemeEffects2ReviewTab.Groups.Count != 5 || ribbon.Tabs.Single(t => t.Text == "Office 2019 Light").Groups.Count != 7) throw new InvalidOperationException("Caption controls displaced earlier review groups.");
        ribbon.Theme = ribbon.Theme with { BackstageAppearance = new() { MarkerWidth = 8 } };
        var group = tab.Groups.Single(g => g.Id == "review-caption-tint"); var preview = tab.Groups.Single(g => g.Id == "review-caption-preview");
        group.Items[0].PerformClick(); group.Items[1].PerformClick(); group.Items[3].PerformClick();
        if (ribbon.Theme.BackstageAppearance is not { CaptionTintEnabled: true, CaptionTintColor: var color, CaptionTintOpacity: .5 } || color != Color.Navy) throw new InvalidOperationException("Caption presets bypassed public settings.");
        group.Items[2].PerformClick(); group.Items[4].PerformClick();
        if (ribbon.Theme.BackstageAppearance.CaptionTintColor.A != 128 || ribbon.Theme.BackstageAppearance.CaptionTintOpacity != 0) throw new InvalidOperationException("Tint alpha/hide presets failed.");
        group.Items[5].PerformClick();
        if (ribbon.Theme.BackstageAppearance is not { CaptionTintEnabled: null, CaptionTintOpacity: null, MarkerWidth: 8 } || !ribbon.Theme.BackstageAppearance.CaptionTintColor.IsEmpty) throw new InvalidOperationException("Caption Reset cleared another appearance role.");
        preview.Items[0].PerformClick(); if (ribbon.Theme.BackstageExtendsIntoTitleBar) throw new InvalidOperationException("Caption structure toggle failed.");
        preview.Items[2].PerformClick(); if (ribbon.Theme.TitleBarForeground != Color.DarkOrange) throw new InvalidOperationException("Caption ink preset failed."); preview.Items[3].PerformClick();
        var previous = ribbon.FileMenuMode; ribbon.AnimationLevel = RibbonAnimationLevel.None; preview.Items[1].PerformClick();
        if (!ribbon.IsBackstageOpen) throw new InvalidOperationException("Caption review did not open Backstage."); ribbon.IsBackstageOpen = false;
        if (ribbon.FileMenuMode != previous) throw new InvalidOperationException("Caption review did not restore File mode.");
        EnableCaptionAppearanceDemo(); if (tab.Groups.Count != 5) throw new InvalidOperationException("Caption groups duplicated.");
        using var image = new Bitmap(ribbon.Width, ribbon.Height); ribbon.DrawToBitmap(image, ribbon.ClientRectangle); image.Save(Path.Combine(output, "review-tab.png")); Close();
    }
}
