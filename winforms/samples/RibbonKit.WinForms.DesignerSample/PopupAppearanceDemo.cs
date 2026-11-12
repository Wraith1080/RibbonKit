using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

public partial class DesignerForm
{
    private void EnablePopupAppearanceDemo()
    {
        var tab = ThemeEffects4ReviewTab;
        if (tab.Groups.Any(group => group.Id == "review-popup-effects")) return;
        var preview = new RibbonGroup { Id = "review-popup-preview", Text = "Try popup frames" };
        var menu = new RibbonDropDownItem { Text = "Menu / submenu", ScreenTipTitle = "Shared popup frame", ScreenTipDescription = "Hover to compare the passive tip, then open the menu and its nested submenu." };
        var child = new RibbonDropDownItem { Text = "Nested submenu" }; child.Items.AddRange(new RibbonButtonItem { Text = "Nested command" }, new RibbonCheckBoxItem { Text = "Retained check", Checked = true });
        menu.Items.AddRange(new RibbonButtonItem { Text = "Menu command" }, new RibbonTextBoxItem { Text = "Name", Value = "Retained native text" }, child);
        var scroll = new RibbonDropDownItem { Text = "Long command menu" }; for (int i = 1; i <= 40; i++) scroll.Items.Add(new RibbonButtonItem { Text = "Command " + i });
        var choice = new RibbonComboBoxItem { Text = "Choices", InputWidth = 140, SimplifiedSize = RibbonItemSize.Medium }; choice.Items.AddRange("First choice", "Second choice", "A longer choice for frame clearance"); choice.SelectedIndex = 0;
        preview.Items.AddRange(menu, scroll, choice); foreach (var item in preview.Items) item.SimplifiedPlacement = RibbonSimplifiedPlacement.Inline;
        var effects = new RibbonGroup { Id = "review-popup-effects", Text = "Popup frame effects" };
        Add("Thicker popup borders", () => Set((ribbon.Theme.PopupAppearance ?? new()) with { BorderWidth = 3 }));
        Add("Horizontal popup shadows", () => Set((ribbon.Theme.PopupAppearance ?? new()) with { ShadowHorizontalOffset = 6 }));
        Add("Softer popup shadows", () => Set((ribbon.Theme.PopupAppearance ?? new()) with { ShadowFalloff = 1.4 }));
        Add("Default popup effects", () => ribbon.Theme = ribbon.Theme with { PopupAppearance = null });
        tab.Groups.AddRange(preview, effects);
        void Set(RibbonPopupAppearanceSettings value) => ribbon.Theme = ribbon.Theme with { PopupAppearance = value };
        void Add(string text, Action action)
        {
            var item = new RibbonButtonItem { Text = text, SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium), SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
            item.Click += (_, _) => action(); effects.Items.Add(item);
        }
    }
    internal void VerifyPopupAppearanceDemo(string output)
    {
        StartPosition = FormStartPosition.Manual; Location = new(-16000, -16000); ShowInTaskbar = false;
        EnableOffice2019ThemeDemo(); Show(); Application.DoEvents(); var review = ThemeEffects4ReviewTab;
        if (ribbon.Tabs.Single(tab => tab.Text == "Office 2019 Light").Groups.Count != 7 || ThemeEffects2ReviewTab.Groups.Count != 5 || ThemeEffects3ReviewTab.Groups.Count != 4 || review.Text != "Theme effects 4" || !review.Groups.Any(group => group.Id == "review-editor-effects")) throw new InvalidOperationException("Popup review controls changed earlier groups.");
        ribbon.SelectedTab = review; var effects = review.Groups.Single(group => group.Id == "review-popup-effects");
        foreach (var item in effects.Items.Take(3)) item.PerformClick();
        if (ribbon.Theme.PopupAppearance?.BorderWidth != 3 || ribbon.Theme.PopupAppearance.ShadowHorizontalOffset != 6 || ribbon.Theme.PopupAppearance.ShadowFalloff != 1.4) throw new InvalidOperationException("Popup review buttons bypassed public settings.");
        effects.Items.Last().PerformClick(); if (ribbon.Theme.PopupAppearance is not null) throw new InvalidOperationException("Popup group did not reset.");
        int count = review.Groups.Count; EnablePopupAppearanceDemo(); if (review.Groups.Count != count) throw new InvalidOperationException("Popup review groups duplicated.");
        using (var image = new Bitmap(ribbon.Width, ribbon.Height)) { ribbon.DrawToBitmap(image, ribbon.ClientRectangle); image.Save(Path.Combine(output, "review-tab.png")); }
        Close();
    }
}
