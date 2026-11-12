using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

public partial class DesignerForm
{
    private void EnableApplicationMenuSurfaceDemo()
    {
        var tab = ThemeEffects5ReviewTab;
        if (tab.Groups.Any(group => group.Id == "review-application-surface")) return;
        var surfaces = new RibbonGroup { Id = "review-application-surface", Text = "Application Menu surfaces" };
        Add(surfaces, "Colored menu surfaces", () => Set((ribbon.Theme.ApplicationMenuAppearance ?? new()) with { SurroundBackground = new(Color.LightYellow), NavigationBackground = new(Color.AliceBlue), DefaultPaneBackground = new(Color.Honeydew), ActivePaneBackground = new(Color.MistyRose) }));
        Add(surfaces, "Stronger default-pane tint", () => Set((ribbon.Theme.ApplicationMenuAppearance ?? new()) with { DefaultPaneTintStrength = .4, DefaultPaneBackground = null }));
        Add(surfaces, "Rounded inner frame", () => Set((ribbon.Theme.ApplicationMenuAppearance ?? new()) with { InnerFrameCornerRadius = 12, InnerFrameBorderWidth = 3, InnerFrameBorderColor = Color.DarkBlue }));
        Add(surfaces, "Colored navigation divider", () => Set((ribbon.Theme.ApplicationMenuAppearance ?? new()) with { NavigationDividerWidth = 3, NavigationDividerInset = 8, NavigationDividerColor = Color.DarkMagenta }));
        Add(surfaces, "Hide navigation divider", () => Set((ribbon.Theme.ApplicationMenuAppearance ?? new()) with { NavigationDividerWidth = 0 }));
        Add(surfaces, "Default menu surfaces", () => Set((ribbon.Theme.ApplicationMenuAppearance ?? new()) with { SurroundBackground = null, NavigationBackground = null, DefaultPaneBackground = null, ActivePaneBackground = null, DefaultPaneTintStrength = null,
            InnerFrameCornerRadius = null, InnerFrameBorderWidth = null, InnerFrameBorderColor = Color.Empty, NavigationDividerWidth = null, NavigationDividerInset = null, NavigationDividerColor = Color.Empty }));
        var headers = new RibbonGroup { Id = "review-application-heading", Text = "Application Menu headings" };
        Add(headers, "Regular heading weight", () => Set((ribbon.Theme.ApplicationMenuAppearance ?? new()) with { HeaderBold = false }));
        Add(headers, "Roomier heading text", () => Set((ribbon.Theme.ApplicationMenuAppearance ?? new()) with { DefaultHeaderTextInset = 12, ActiveHeaderTextInset = 18 }));
        Add(headers, "Rounded heading fills", () => Set((ribbon.Theme.ApplicationMenuAppearance ?? new()) with { HeaderCornerRadius = 8 }));
        Add(headers, "Colored heading rule", () => Set((ribbon.Theme.ApplicationMenuAppearance ?? new()) with { HeaderRuleThickness = 3, HeaderRuleColor = Color.DarkGreen }));
        Add(headers, "Hide heading rule", () => Set((ribbon.Theme.ApplicationMenuAppearance ?? new()) with { HeaderRuleThickness = 0 }));
        Add(headers, "Default menu headings", () => Set((ribbon.Theme.ApplicationMenuAppearance ?? new()) with { HeaderBold = null, DefaultHeaderTextInset = null, ActiveHeaderTextInset = null, HeaderCornerRadius = null, HeaderRuleThickness = null, HeaderRuleColor = Color.Empty }));
        tab.Groups.AddRange(surfaces, headers);
        void Set(RibbonApplicationMenuAppearanceSettings value) => ribbon.Theme = ribbon.Theme with { ApplicationMenuAppearance = value };
        void Add(RibbonGroup group, string text, Action action)
        {
            var item = new RibbonButtonItem { Text = text, SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium), SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
            item.Click += (_, _) => action(); group.Items.Add(item);
        }
    }
    internal void VerifyApplicationMenuSurfaceDemo(string output)
    {
        StartPosition = FormStartPosition.Manual; Location = new(-16000, -16000); ShowInTaskbar = false;
        EnableOffice2019ThemeDemo(); Show(); Application.DoEvents(); var tab = ThemeEffects5ReviewTab; ribbon.SelectedTab = tab;
        if (tab.Text != "Theme effects 5" || ThemeEffects4ReviewTab.Groups.Count != 7 || ThemeEffects3ReviewTab.Groups.Count != 4 || ThemeEffects2ReviewTab.Groups.Count != 5 || ribbon.Tabs.Single(t => t.Text == "Office 2019 Light").Groups.Count != 7) throw new InvalidOperationException("Application Menu surface controls displaced earlier groups.");
        var surfaces = tab.Groups.Single(g => g.Id == "review-application-surface"); var headers = tab.Groups.Single(g => g.Id == "review-application-heading");
        ribbon.Theme = ribbon.Theme with { ApplicationMenuAppearance = new() { RowInset = 4, SplitDividerWidth = 2 } };
        foreach (var item in surfaces.Items.Take(4)) item.PerformClick(); foreach (var item in headers.Items.Take(4)) item.PerformClick();
        var a = ribbon.Theme.ApplicationMenuAppearance!;
        if (a.DefaultPaneTintStrength != .4 || a.DefaultPaneBackground is not null || a.NavigationBackground is null || a.InnerFrameBorderWidth != 3 || a.NavigationDividerWidth != 3 || a.HeaderBold != false || a.HeaderRuleThickness != 3 || a.RowInset != 4) throw new InvalidOperationException("Surface/heading controls bypassed public settings.");
        surfaces.Items[4].PerformClick(); headers.Items[4].PerformClick(); if (ribbon.Theme.ApplicationMenuAppearance!.NavigationDividerWidth != 0 || ribbon.Theme.ApplicationMenuAppearance.HeaderRuleThickness != 0) throw new InvalidOperationException("Rule hide controls failed.");
        surfaces.Items.Last().PerformClick(); if (ribbon.Theme.ApplicationMenuAppearance!.HeaderBold != false || ribbon.Theme.ApplicationMenuAppearance.RowInset != 4) throw new InvalidOperationException("Surface reset cleared a different role.");
        headers.Items.Last().PerformClick(); if (ribbon.Theme.ApplicationMenuAppearance!.HeaderBold is not null || ribbon.Theme.ApplicationMenuAppearance.SplitDividerWidth != 2) throw new InvalidOperationException("Heading reset cleared a command role.");
        var mode = ribbon.FileMenuMode; var preview = tab.Groups.Single(g => g.Id == "review-application-preview");
        preview.Items[0].PerformClick(); if (!ribbon.FileMenu.IsDropDownOpen) throw new InvalidOperationException("Default-pane preview failed.");
        preview.Items[1].PerformClick(); if (!applicationSave.IsDropDownOpen) throw new InvalidOperationException("Selected-pane preview failed.");
        preview.Items[2].PerformClick(); if (ribbon.FileMenuMode != mode || ribbon.FileMenu.IsDropDownOpen) throw new InvalidOperationException("Preview File mode did not restore.");
        int count = tab.Groups.Count; EnableApplicationMenuSurfaceDemo(); if (tab.Groups.Count != count) throw new InvalidOperationException("Surface controls duplicated.");
        using var image = new Bitmap(ribbon.Width, ribbon.Height); ribbon.DrawToBitmap(image, ribbon.ClientRectangle); image.Save(Path.Combine(output, "review-tab.png")); Close();
    }
}
