using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

public partial class DesignerForm
{
    private RibbonForm CreateBackstagePagePreferencePreview(bool auto)
    {
        var form = new RibbonForm { Text = "Backstage page-space preview — resize to compare", ClientSize = new(900, 540), StartPosition = FormStartPosition.CenterParent,
            Font = Font, Backdrop = Backdrop, TitleBarColor = TitleBarColor, TitleBarTintOpacity = TitleBarTintOpacity };
        var model = new RibbonControl { Theme = ribbon.Theme, LayoutMode = ribbon.LayoutMode, Density = ribbon.Density,
            FileMenuMode = RibbonFileMenuMode.Backstage, AnimationLevel = RibbonAnimationLevel.None };
        model.FileMenu.Visible = true; model.Tabs.Add(new RibbonTab { Text = "Preview" });
        model.Backstage.MinimumNavigationWidth = 80; model.Backstage.MaximumNavigationWidth = 1000; model.Backstage.NavigationWidth = auto ? null : 1000;
        var page = new TextBox { Multiline = true, BorderStyle = BorderStyle.None, Text = "Resize this preview. The page-space preference limits wide navigation. Choose a different preset on the main ribbon before reopening." };
        model.Backstage.Items.Add(new RibbonBackstagePageItem { Text = auto ? "Document information, page layout, printing, publishing, export, accessibility, application preferences and advanced document settings" : "Info", Content = page });
        form.Disposed += (_, _) => page.Dispose(); form.Controls.Add(model); form.Ribbon = model;
        form.Controls.Add(new Label { Text = "Close this preview to return to the main sample.", AutoSize = true, Location = new(20, 250) });
        form.Shown += (_, _) => model.IsBackstageOpen = true; return form;
    }
    private void EnableBackstagePagePreferenceDemo()
    {
        var tab = ThemeEffectsReviewTab;
        if (tab.Groups.Any(g => g.Id == "review-backstage-page-preference")) return;
        var group = new RibbonGroup { Id = "review-backstage-page-preference", Text = "Backstage page space" };
        Add("Page preference 160", () => Set(160)); Add("Page preference 360", () => Set(360)); Add("Page preference 600", () => Set(600));
        Add("No page reservation", () => Set(0)); Add("Default page preference", () => Set(null));
        Add("Preview fixed navigation", () => { using var form = CreateBackstagePagePreferencePreview(false); form.ShowDialog(this); });
        Add("Preview Auto navigation", () => { using var form = CreateBackstagePagePreferencePreview(true); form.ShowDialog(this); });
        tab.Groups.Add(group);
        void Set(int? value) => ribbon.Theme = ribbon.Theme with { BackstagePreferredMinimumPageWidth = value };
        void Add(string text, Action action)
        {
            var item = new RibbonButtonItem { Text = text, ScreenTipDescription = "Choose a preference, then open a preview with wide navigation and resize it. Default prefers 240 pixels at 100% scaling; No page reservation removes this preference. The main sample's navigation settings stay the same.", SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium), SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
            item.Click += (_, _) => action(); group.Items.Add(item);
        }
    }
    internal void VerifyBackstagePagePreferenceDemo(string output)
    {
        Directory.CreateDirectory(output); StartPosition = FormStartPosition.Manual; Location = new(-16000, -16000); Width = 1900; ShowInTaskbar = false;
        EnableOffice2019ThemeDemo(); Show(); Application.DoEvents(); var tab = ThemeEffectsReviewTab; ribbon.SelectedTab = tab;
        if (tab.Text != "Theme effects 11" || tab.Groups.Count != 5 || ThemeEffects10ReviewTab.Groups.Count != 5 || ThemeEffects9ReviewTab.Groups.Count != 6 || ThemeEffects8ReviewTab.Groups.Count != 5 || ThemeEffects7ReviewTab.Groups.Count != 8 || ThemeEffects6ReviewTab.Groups.Count != 3 || ThemeEffects5ReviewTab.Groups.Count != 6 || ThemeEffects4ReviewTab.Groups.Count != 7 || ThemeEffects3ReviewTab.Groups.Count != 4 || ThemeEffects2ReviewTab.Groups.Count != 5 || ribbon.Tabs.Single(t => t.Text == "Office 2019 Light").Groups.Count != 7) throw new InvalidOperationException("Page preference controls displaced accepted tabs.");
        var group = tab.Groups.Single(g => g.Id == "review-backstage-page-preference");
        var width = ribbon.Backstage.NavigationWidth; int minimum = ribbon.Backstage.MinimumNavigationWidth, maximum = ribbon.Backstage.MaximumNavigationWidth;
        var tabs = ribbon.Tabs.ToArray(); var mode = ribbon.LayoutMode;
        ribbon.Theme = ribbon.Theme with { CaptionButtonAppearance = new() { GlyphSize = 18 } };
        foreach (var (index, value) in new[] { (0, 160), (1, 360), (2, 600), (3, 0) }) { group.Items[index].PerformClick(); if (ribbon.Theme.BackstagePreferredMinimumPageWidth != value) throw new InvalidOperationException("Page preference preset bypassed public settings."); }
        group.Items[1].PerformClick();
        foreach (bool auto in new[] { false, true })
        {
            using var preview = CreateBackstagePagePreferencePreview(auto); var model = preview.Ribbon!;
            if (ReferenceEquals(model, ribbon) || model.Theme.BackstagePreferredMinimumPageWidth != 360 || model.Backstage.NavigationWidth != (auto ? (int?)null : 1000)) throw new InvalidOperationException("Page preview did not isolate public model settings.");
        }
        group.Items[4].PerformClick();
        if (ribbon.Theme.BackstagePreferredMinimumPageWidth is not null || ribbon.Theme.CaptionButtonAppearance?.GlyphSize != 18 || ribbon.Backstage.NavigationWidth != width || ribbon.Backstage.MinimumNavigationWidth != minimum || ribbon.Backstage.MaximumNavigationWidth != maximum || !tabs.SequenceEqual(ribbon.Tabs) || ribbon.LayoutMode != mode) throw new InvalidOperationException("Preference Reset/preview changed unrelated settings or models.");
        EnableBackstagePagePreferenceDemo(); if (tab.Groups.Count != 5) throw new InvalidOperationException("Page preference groups duplicated.");
        using var image = new Bitmap(ribbon.Width, ribbon.Height); ribbon.DrawToBitmap(image, ribbon.ClientRectangle); image.Save(Path.Combine(output, "review-tab.png")); Close();
    }
}
