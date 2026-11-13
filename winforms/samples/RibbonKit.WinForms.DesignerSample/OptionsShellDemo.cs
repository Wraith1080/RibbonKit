using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

public partial class DesignerForm
{
    private void EnableOptionsShellDemo()
    {
        var tab = ThemeEffects8ReviewTab; if (tab.Groups.Any(g => g.Id == "review-options-buttons")) return;
        var preview = new RibbonGroup { Id = "review-options-shell-preview", Text = "Try Options shell" };
        Add(preview, "Open shell preview", () => { using var dialog = CreateOptionsAppearancePreview(); dialog.Text = "Options buttons and headings"; dialog.ShowDialog(this); });
        Add(preview, "Open ribbon/QAT options", () => { using var dialog = new RibbonQuickAccessCustomizationForm(ribbon, "RibbonKitDesignerConsumer"); dialog.ShowDialog(this); });
        var buttons = new RibbonGroup { Id = "review-options-buttons", Text = "Options buttons" };
        Add(buttons, "Outlined dialog buttons", () => Set((ribbon.Theme.OptionsAppearance ?? new()) with { ButtonBorderWidth = 2, ButtonBorderColor = Color.Navy }));
        Add(buttons, "Inset buttons and focus", () => Set((ribbon.Theme.OptionsAppearance ?? new()) with { ButtonInset = 2, ButtonFocusInset = 6 }));
        Add(buttons, "Alternate idle button fills", () => Set((ribbon.Theme.OptionsAppearance ?? new()) with { ButtonBackground = new(Color.LightYellow), AccentButtonBackground = new(Color.DarkSlateBlue) }));
        Add(buttons, "Hide dialog button outlines", () => Set((ribbon.Theme.OptionsAppearance ?? new()) with { ButtonBorderWidth = 0 }));
        Add(buttons, "Default Options buttons", () => Set((ribbon.Theme.OptionsAppearance ?? new()) with { ButtonInset = null, ButtonBorderWidth = null, ButtonBorderColor = Color.Empty, ButtonFocusInset = null, ButtonBackground = null, AccentButtonBackground = null }));
        var headings = new RibbonGroup { Id = "review-options-headings", Text = "Options headings" };
        Add(headings, "Roomier Options headings", () => Set((ribbon.Theme.OptionsAppearance ?? new()) with { HeadingFontSizeIncrement = 8, HeadingMinimumFontSize = 20, HeadingBottomPadding = 20 }));
        Add(headings, "Regular Options headings", () => Set((ribbon.Theme.OptionsAppearance ?? new()) with { HeadingBold = false }));
        Add(headings, "No heading gap", () => Set((ribbon.Theme.OptionsAppearance ?? new()) with { HeadingBottomPadding = 0 }));
        Add(headings, "Default Options headings", () => Set((ribbon.Theme.OptionsAppearance ?? new()) with { HeadingFontSizeIncrement = null, HeadingMinimumFontSize = null, HeadingBold = null, HeadingBottomPadding = null }));
        tab.Groups.AddRange(preview, buttons, headings);
        void Set(RibbonOptionsAppearanceSettings a) => ribbon.Theme = ribbon.Theme with { OptionsAppearance = a };
        void Add(RibbonGroup group, string text, Action action) { var item = new RibbonButtonItem { Text = text, ScreenTipDescription = "Choose the appearance, then open the Options preview to see the changes.", SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium), SimplifiedPlacement = RibbonSimplifiedPlacement.Inline }; item.Click += (_, _) => action(); group.Items.Add(item); }
    }
    internal void VerifyOptionsShellDemo(string output)
    {
        StartPosition = FormStartPosition.Manual; Location = new(-16000, -16000); Width = 1750; ShowInTaskbar = false; EnableOffice2019ThemeDemo(); Show(); Application.DoEvents(); var tab = ThemeEffects8ReviewTab; ribbon.SelectedTab = tab;
        if (tab.Text != "Theme effects 8" || tab.Groups.Count != 3 || ThemeEffects7ReviewTab.Groups.Count != 8 || ThemeEffects6ReviewTab.Groups.Count != 3 || ThemeEffects5ReviewTab.Groups.Count != 6 || ThemeEffects4ReviewTab.Groups.Count != 7 || ThemeEffects3ReviewTab.Groups.Count != 4 || ThemeEffects2ReviewTab.Groups.Count != 5 || ribbon.Tabs.Single(t => t.Text == "Office 2019 Light").Groups.Count != 7) throw new InvalidOperationException("Options shell controls displaced accepted review tabs.");
        var buttons = tab.Groups.Single(g => g.Id == "review-options-buttons"); var headings = tab.Groups.Single(g => g.Id == "review-options-headings"); ribbon.Theme = ribbon.Theme with { OptionsAppearance = new() { NavigationCornerRadius = 12, DividerWidth = 3 } };
        foreach (var item in buttons.Items.Take(3)) item.PerformClick(); headings.Items[0].PerformClick(); headings.Items[1].PerformClick();
        if (ribbon.Theme.OptionsAppearance is not { ButtonInset: 2, ButtonBorderWidth: 2, ButtonFocusInset: 6, HeadingFontSizeIncrement: 8, HeadingMinimumFontSize: 20, HeadingBold: false, HeadingBottomPadding: 20 }) throw new InvalidOperationException("Shell presets bypassed public settings.");
        using (var dialog = CreateOptionsAppearancePreview()) { dialog.StartPosition = FormStartPosition.Manual; dialog.Location = new(-16000, -16000); dialog.ShowInTaskbar = false; dialog.Show(this); Application.DoEvents(); using var image = new Bitmap(dialog.Width, dialog.Height); dialog.DrawToBitmap(image, new(Point.Empty, image.Size)); image.Save(Path.Combine(output, "review-options.png")); dialog.Close(); }
        buttons.Items[3].PerformClick(); if (ribbon.Theme.OptionsAppearance!.ButtonBorderWidth != 0) throw new InvalidOperationException("Outline hide failed."); buttons.Items[4].PerformClick(); if (ribbon.Theme.OptionsAppearance!.HeadingBold != false || ribbon.Theme.OptionsAppearance.DividerWidth != 3) throw new InvalidOperationException("Button reset cleared heading/navigation roles.");
        buttons.Items[0].PerformClick(); headings.Items[2].PerformClick(); if (ribbon.Theme.OptionsAppearance!.HeadingBottomPadding != 0) throw new InvalidOperationException("Heading gap hide failed."); headings.Items[3].PerformClick(); if (ribbon.Theme.OptionsAppearance!.ButtonBorderWidth != 2 || ribbon.Theme.OptionsAppearance.NavigationCornerRadius != 12 || ribbon.Theme.OptionsAppearance.HeadingBold is not null) throw new InvalidOperationException("Heading reset cleared button/navigation roles.");
        EnableOptionsShellDemo(); if (tab.Groups.Count != 3) throw new InvalidOperationException("Shell groups duplicated."); using var pixels = new Bitmap(ribbon.Width, ribbon.Height); ribbon.DrawToBitmap(pixels, ribbon.ClientRectangle); pixels.Save(Path.Combine(output, "review-tab.png")); Close();
    }
}
