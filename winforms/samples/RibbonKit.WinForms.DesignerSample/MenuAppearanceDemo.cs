using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

public partial class DesignerForm
{
    private void EnableMenuAppearanceDemo()
    {
        var tab = ThemeEffectsReviewTab;
        if (tab.Groups.Any(group => group.Id == "review-menu-effects")) return;
        var preview = new RibbonGroup { Id = "review-menu-preview", Text = "Try menu decoration" };
        var menu = new RibbonDropDownItem { Text = "Headings / separators", DropDownHeader = "Menu heading" };
        var child = new RibbonDropDownItem { Text = "Nested heading", DropDownHeader = "Nested menu heading" };
        child.Items.AddRange(new RibbonButtonItem { Text = "Nested command" }, new RibbonMenuSeparatorItem(), new RibbonCheckBoxItem { Text = "Retained check", Checked = true });
        menu.Items.AddRange(new RibbonButtonItem { Text = "Menu command" }, new RibbonMenuSeparatorItem(), new RibbonTextBoxItem { Text = "Name", Value = "Retained native text" }, child);
        preview.Items.Add(menu);
        var entries = new List<RibbonElement>(); int savedWidth = 0; bool savedVisible = true; RibbonQuickAccessPosition savedPosition = default;
        AddPreview("Try scrolling overflow", () =>
        {
            if (entries.Count > 0) return;
            savedWidth = ribbon.QuickAccessMaxWidth; savedVisible = ribbon.QuickAccessVisible; savedPosition = ribbon.QuickAccessPosition;
            entries.Add(new RibbonQuickAccessDropDownReference { Source = menu, Text = "Heading and separators" });
            for (int i = 1; i <= 60; i++)
            {
                var item = new RibbonButtonItem { Text = "Review command " + i }; item.Click += (_, _) => statusLabel.Text = "Overflow review command invoked."; entries.Add(item);
            }
            ribbon.QuickAccessItems.AddRange(entries.ToArray()); ribbon.QuickAccessVisible = true; ribbon.QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon; ribbon.QuickAccessMaxWidth = 28;
            statusLabel.Text = "Open More Quick Access commands to review the scroll bands.";
        });
        AddPreview("Restore preview toolbar", RestoreToolbar);
        FormClosed += (_, _) => RestoreToolbar();
        foreach (var item in preview.Items) item.SimplifiedPlacement = RibbonSimplifiedPlacement.Inline;
        var effects = new RibbonGroup { Id = "review-menu-effects", Text = "Menu decoration effects" };
        Add("Outlined rounded headings", () => Set((ribbon.Theme.MenuAppearance ?? new()) with { HeaderCornerRadius = 8, HeaderInset = 0, HeaderBorderWidth = 2, HeaderBorderColor = Color.DarkBlue }));
        Add("Regular heading text", () => Set((ribbon.Theme.MenuAppearance ?? new()) with { HeaderBold = false }));
        Add("Colored separator lines", () => Set((ribbon.Theme.MenuAppearance ?? new()) with { SeparatorThickness = 3, SeparatorColor = Color.DarkMagenta }));
        Add("Gradient scroll bands", () => Set((ribbon.Theme.MenuAppearance ?? new()) with { OverflowBackground = new(Color.LightYellow, Color.LightSkyBlue) }));
        Add("Outlined scroll states", () => Set((ribbon.Theme.MenuAppearance ?? new()) with { OverflowCornerRadius = 8, OverflowInset = 2, OverflowBorderWidth = 2, OverflowBorderColor = Color.DarkBlue }));
        Add("Default menu effects", () => ribbon.Theme = ribbon.Theme with { MenuAppearance = null });
        tab.Groups.AddRange(preview, effects);
        void Set(RibbonMenuAppearanceSettings value) => ribbon.Theme = ribbon.Theme with { MenuAppearance = value };
        void Add(string text, Action action) => AddButton(effects, text, action);
        void AddPreview(string text, Action action) => AddButton(preview, text, action);
        void AddButton(RibbonGroup group, string text, Action action)
        {
            var item = new RibbonButtonItem { Text = text, SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium), SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
            item.Click += (_, _) => action(); group.Items.Add(item);
        }
        void RestoreToolbar()
        {
            if (entries.Count == 0) return;
            foreach (var item in entries) { ribbon.QuickAccessItems.Remove(item); item.Dispose(); }
            entries.Clear(); ribbon.QuickAccessMaxWidth = savedWidth; ribbon.QuickAccessVisible = savedVisible; ribbon.QuickAccessPosition = savedPosition;
        }
    }
    internal void VerifyMenuAppearanceDemo(string output)
    {
        StartPosition = FormStartPosition.Manual; Location = new(-16000, -16000); ShowInTaskbar = false;
        EnableOffice2019ThemeDemo(); Show(); Application.DoEvents(); var tab = ThemeEffectsReviewTab;
        if (tab.Text != "Theme effects 4" || ribbon.Tabs.Single(item => item.Text == "Office 2019 Light").Groups.Count != 7 || ThemeEffects2ReviewTab.Groups.Count != 5 || ThemeEffects3ReviewTab.Groups.Count != 4 || !tab.Groups.Any(group => group.Id == "review-popup-effects")) throw new InvalidOperationException("Menu controls displaced earlier review groups.");
        ribbon.SelectedTab = tab; var effects = tab.Groups.Single(group => group.Id == "review-menu-effects");
        foreach (var item in effects.Items.Take(5)) item.PerformClick(); var value = ribbon.Theme.MenuAppearance;
        if (value?.HeaderCornerRadius != 8 || value.HeaderBold != false || value.SeparatorThickness != 3 || value.OverflowBackground is null || value.OverflowBorderWidth != 2) throw new InvalidOperationException("Menu controls bypassed public settings.");
        var before = ribbon.QuickAccessItems.ToArray(); int width = ribbon.QuickAccessMaxWidth; var position = ribbon.QuickAccessPosition;
        tab.Groups.SelectMany(group => group.Items).Single(item => item.Text == "Try scrolling overflow").PerformClick();
        if (ribbon.QuickAccessItems.Count != before.Length + 61 || ribbon.QuickAccessMaxWidth != 28) throw new InvalidOperationException("Overflow preview was not prepared.");
        tab.Groups.SelectMany(group => group.Items).Single(item => item.Text == "Restore preview toolbar").PerformClick();
        if (!before.SequenceEqual(ribbon.QuickAccessItems) || ribbon.QuickAccessMaxWidth != width || ribbon.QuickAccessPosition != position) throw new InvalidOperationException("Overflow preview did not restore authored toolbar items.");
        effects.Items.Last().PerformClick(); if (ribbon.Theme.MenuAppearance is not null) throw new InvalidOperationException("Menu effects did not reset.");
        int count = tab.Groups.Count; EnableMenuAppearanceDemo(); if (tab.Groups.Count != count) throw new InvalidOperationException("Menu review groups duplicated.");
        using (var image = new Bitmap(ribbon.Width, ribbon.Height)) { ribbon.DrawToBitmap(image, ribbon.ClientRectangle); image.Save(Path.Combine(output, "review-tab.png")); }
        Close();
    }
}
