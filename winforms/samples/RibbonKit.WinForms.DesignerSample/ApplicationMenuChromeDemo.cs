using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

public partial class DesignerForm
{
    private void EnableApplicationMenuChromeDemo()
    {
        var tab = ThemeEffects5ReviewTab;
        if (tab.Groups.Any(group => group.Id == "review-application-chrome")) return;
        var preview = new RibbonGroup { Id = "review-application-preview", Text = "Try Application Menu" };
        bool previewing = false; RibbonFileMenuMode priorMode = default;
        Add(preview, "Open File menu", () => Open(false));
        Add(preview, "Open selected split pane", () => Open(true));
        Add(preview, "Restore File mode", RestoreMode);
        var effects = new RibbonGroup { Id = "review-application-chrome", Text = "Application Menu state effects" };
        Add(effects, "Rounded outlined menu rows", () => Set((ribbon.Theme.ApplicationMenuAppearance ?? new()) with { RowCornerRadius = 8, RowInset = 2, RowBorderWidth = 2, RowBorderColor = Color.DarkBlue }));
        Add(effects, "Footer outlines", () => Set((ribbon.Theme.ApplicationMenuAppearance ?? new()) with { FooterInset = 2, FooterBorderWidth = 2, FooterBorderColor = Color.DarkOrange }));
        Add(effects, "Colored split dividers", () => Set((ribbon.Theme.ApplicationMenuAppearance ?? new()) with { SplitDividerWidth = 3, SplitDividerInset = 6, SplitDividerColor = Color.DarkMagenta }));
        Add(effects, "Stronger selected-primary tint", () => Set((ribbon.Theme.ApplicationMenuAppearance ?? new()) with { SelectedPrimaryTintStrength = .55 }));
        Add(effects, "Selected primary fill", () => Set((ribbon.Theme.ApplicationMenuAppearance ?? new()) with { SelectedPrimaryBackground = new(Color.LightSkyBlue) }));
        Add(effects, "Show / hide split dividers", () => ribbon.Theme = ribbon.Theme with { ApplicationMenuSplitDividerVisible = !ribbon.Theme.ApplicationMenuSplitDividerVisible });
        Add(effects, "Derived primary tint", () => Set((ribbon.Theme.ApplicationMenuAppearance ?? new()) with { SelectedPrimaryBackground = null }));
        Add(effects, "Default Application Menu effects", () => ribbon.Theme = ribbon.Theme with { ApplicationMenuAppearance = null });
        tab.Groups.AddRange(preview, effects);
        void Open(bool split)
        {
            if (!previewing) { priorMode = ribbon.FileMenuMode; previewing = true; }
            ribbon.FileMenuMode = RibbonFileMenuMode.CommandMenu;
            if (!ribbon.FileMenu.IsDropDownOpen) ribbon.FileMenu.ShowDropDown();
            if (split) applicationSave.ShowDropDown();
        }
        void RestoreMode()
        {
            if (!previewing) return;
            ribbon.FileMenu.CloseDropDown(); ribbon.FileMenuMode = priorMode; previewing = false;
        }
        void Set(RibbonApplicationMenuAppearanceSettings value) => ribbon.Theme = ribbon.Theme with { ApplicationMenuAppearance = value };
        void Add(RibbonGroup group, string text, Action action)
        {
            var item = new RibbonButtonItem { Text = text, SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium), SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
            item.Click += (_, _) => action(); group.Items.Add(item);
        }
    }
    internal void VerifyApplicationMenuChromeDemo(string output)
    {
        StartPosition = FormStartPosition.Manual; Location = new(-16000, -16000); ShowInTaskbar = false;
        EnableOffice2019ThemeDemo(); Show(); Application.DoEvents(); var tab = ThemeEffects5ReviewTab;
        if (tab.Text != "Theme effects 5" || ribbon.Tabs.Single(item => item.Text == "Office 2019 Light").Groups.Count != 7 || ThemeEffects2ReviewTab.Groups.Count != 5 || ThemeEffects3ReviewTab.Groups.Count != 4 || ThemeEffects4ReviewTab.Groups.Count != 7) throw new InvalidOperationException("Application Menu controls displaced an earlier tab.");
        ribbon.SelectedTab = tab; var effects = tab.Groups.Single(group => group.Id == "review-application-chrome");
        foreach (var item in effects.Items.Take(5)) item.PerformClick(); var settings = ribbon.Theme.ApplicationMenuAppearance;
        if (settings?.RowCornerRadius != 8 || settings.FooterBorderWidth != 2 || settings.SplitDividerWidth != 3 || settings.SelectedPrimaryTintStrength != .55 || settings.SelectedPrimaryBackground is null) throw new InvalidOperationException("Application Menu buttons bypassed public settings.");
        bool dividerVisible = ribbon.Theme.ApplicationMenuSplitDividerVisible;
        effects.Items[5].PerformClick(); if (ribbon.Theme.ApplicationMenuSplitDividerVisible == dividerVisible) throw new InvalidOperationException("Existing split-divider visibility did not toggle.");
        effects.Items[6].PerformClick(); if (ribbon.Theme.ApplicationMenuAppearance!.SelectedPrimaryBackground is not null || ribbon.Theme.ApplicationMenuAppearance.SelectedPrimaryTintStrength != .55) throw new InvalidOperationException("Selected-primary member reset cleared another role.");
        var mode = ribbon.FileMenuMode; var preview = tab.Groups.Single(group => group.Id == "review-application-preview");
        preview.Items[1].PerformClick(); if (ribbon.FileMenuMode != RibbonFileMenuMode.CommandMenu || !ribbon.FileMenu.IsDropDownOpen || !applicationSave.IsDropDownOpen) throw new InvalidOperationException("Selected split preview did not open.");
        preview.Items[2].PerformClick(); if (ribbon.FileMenuMode != mode || ribbon.FileMenu.IsDropDownOpen) throw new InvalidOperationException("File mode did not restore.");
        effects.Items.Last().PerformClick(); if (ribbon.Theme.ApplicationMenuAppearance is not null || ribbon.Theme.ApplicationMenuSplitDividerVisible == dividerVisible) throw new InvalidOperationException("Application Menu group reset changed another role.");
        effects.Items[5].PerformClick();
        int count = tab.Groups.Count; EnableApplicationMenuChromeDemo(); if (tab.Groups.Count != count) throw new InvalidOperationException("Application Menu groups duplicated.");
        using (var image = new Bitmap(ribbon.Width, ribbon.Height)) { ribbon.DrawToBitmap(image, ribbon.ClientRectangle); image.Save(Path.Combine(output, "review-tab.png")); }
        Close();
    }
}
