using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

public partial class DesignerForm
{
    private void EnableApplicationMenuContentDemo()
    {
        var tab = ThemeEffectsReviewTab;
        if (tab.Groups.Any(group => group.Id == "review-application-content")) return;
        var content = new RibbonGroup { Id = "review-application-content", Text = "Application Menu content" };
        Add(content, "Larger menu icons", () => Set((ribbon.Theme.ApplicationMenuContent ?? new()) with { LargeIconSize = 40, SmallIconSize = 24 }));
        Add(content, "Roomier content text", () => Set((ribbon.Theme.ApplicationMenuContent ?? new()) with { LargeTextInset = 72, TextInset = 10, TextTrailingPadding = 9, IconTextGap = 12 }));
        Add(content, "Taller command rows", () => Set((ribbon.Theme.ApplicationMenuContent ?? new()) with { RowTextVerticalPadding = 20, IconVerticalPadding = 12 }));
        Add(content, "Wider arrow columns", () => Set((ribbon.Theme.ApplicationMenuContent ?? new()) with { ArrowColumnWidth = 38 }));
        Add(content, "Roomier footer content", () => Set((ribbon.Theme.ApplicationMenuContent ?? new()) with { FooterHorizontalPadding = 14 }));
        Add(content, "Default all menu content", () => ribbon.Theme = ribbon.Theme with { ApplicationMenuContent = null });
        var descriptions = new RibbonGroup { Id = "review-application-description", Text = "Application Menu descriptions" };
        Add(descriptions, "More description lines", () => Set((ribbon.Theme.ApplicationMenuContent ?? new()) with { DescriptionMaxLines = 3 }));
        Add(descriptions, "Roomier descriptions", () => Set((ribbon.Theme.ApplicationMenuContent ?? new()) with { DescriptionGap = 6, DescriptionVerticalInset = 8 }));
        Add(descriptions, "Regular illustrated titles", () => Set((ribbon.Theme.ApplicationMenuContent ?? new()) with { RichTitleBold = false }));
        Add(descriptions, "Hide descriptions", () => Set((ribbon.Theme.ApplicationMenuContent ?? new()) with { DescriptionMaxLines = 0 }));
        Add(descriptions, "Colored menu separators", () => Set((ribbon.Theme.ApplicationMenuContent ?? new()) with { SeparatorHeight = 13, SeparatorHorizontalPadding = 12, SeparatorThickness = 3, SeparatorColor = Color.DarkMagenta }));
        Add(descriptions, "Default descriptions / separators", () => Set((ribbon.Theme.ApplicationMenuContent ?? new()) with { DescriptionMaxLines = null, DescriptionGap = null, DescriptionVerticalInset = null, RichTitleBold = null,
            SeparatorHeight = null, SeparatorHorizontalPadding = null, SeparatorThickness = null, SeparatorColor = Color.Empty }));
        tab.Groups.AddRange(content, descriptions);
        void Set(RibbonApplicationMenuContentSettings value) => ribbon.Theme = ribbon.Theme with { ApplicationMenuContent = value };
        void Add(RibbonGroup group, string text, Action action)
        {
            var item = new RibbonButtonItem { Text = text, SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium), SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
            item.Click += (_, _) => action(); group.Items.Add(item);
        }
    }
    internal void VerifyApplicationMenuContentDemo(string output)
    {
        StartPosition = FormStartPosition.Manual; Location = new(-16000, -16000); ShowInTaskbar = false;
        EnableOffice2019ThemeDemo(); Show(); Application.DoEvents(); var tab = ThemeEffectsReviewTab; ribbon.SelectedTab = tab;
        if (tab.Text != "Theme effects 5" || ThemeEffects4ReviewTab.Groups.Count != 7 || ThemeEffects3ReviewTab.Groups.Count != 4 || ThemeEffects2ReviewTab.Groups.Count != 5 || ribbon.Tabs.Single(t => t.Text == "Office 2019 Light").Groups.Count != 7) throw new InvalidOperationException("Content controls displaced earlier tabs.");
        var content = tab.Groups.Single(g => g.Id == "review-application-content"); var descriptions = tab.Groups.Single(g => g.Id == "review-application-description");
        ribbon.Theme = ribbon.Theme with { ApplicationMenuAppearance = new() { RowInset = 4 } };
        foreach (var item in content.Items.Take(5)) item.PerformClick(); foreach (var item in descriptions.Items.Take(3)) item.PerformClick();
        var a = ribbon.Theme.ApplicationMenuContent!;
        if (a.LargeIconSize != 40 || a.SmallIconSize != 24 || a.LargeTextInset != 72 || a.ArrowColumnWidth != 38 || a.FooterHorizontalPadding != 14 || a.RichTitleBold != false || a.DescriptionMaxLines != 3) throw new InvalidOperationException("Content presets bypassed public settings.");
        descriptions.Items[3].PerformClick(); descriptions.Items[4].PerformClick(); if (ribbon.Theme.ApplicationMenuContent!.DescriptionMaxLines != 0 || ribbon.Theme.ApplicationMenuContent.SeparatorThickness != 3) throw new InvalidOperationException("Description/separator controls failed.");
        descriptions.Items.Last().PerformClick(); if (ribbon.Theme.ApplicationMenuContent!.DescriptionMaxLines is not null || ribbon.Theme.ApplicationMenuContent.LargeIconSize != 40) throw new InvalidOperationException("Description reset cleared another role.");
        var mode = ribbon.FileMenuMode; var preview = tab.Groups.Single(g => g.Id == "review-application-preview");
        preview.Items[0].PerformClick(); if (!ribbon.FileMenu.IsDropDownOpen) throw new InvalidOperationException("File preview failed.");
        preview.Items[1].PerformClick(); if (!applicationSave.IsDropDownOpen) throw new InvalidOperationException("Illustrated pane preview failed.");
        preview.Items[2].PerformClick(); if (ribbon.FileMenuMode != mode || ribbon.FileMenu.IsDropDownOpen) throw new InvalidOperationException("File mode did not restore.");
        content.Items.Last().PerformClick(); if (ribbon.Theme.ApplicationMenuContent is not null || ribbon.Theme.ApplicationMenuAppearance!.RowInset != 4) throw new InvalidOperationException("Content group reset cleared appearance.");
        int count = tab.Groups.Count; EnableApplicationMenuContentDemo(); if (tab.Groups.Count != count) throw new InvalidOperationException("Content groups duplicated.");
        using var image = new Bitmap(ribbon.Width, ribbon.Height); ribbon.DrawToBitmap(image, ribbon.ClientRectangle); image.Save(Path.Combine(output, "review-tab.png")); Close();
    }
}
