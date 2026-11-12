using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

public partial class DesignerForm
{
    private void EnableOptionsAppearanceDemo()
    {
        var tab = ThemeEffectsReviewTab; if (tab.Groups.Any(g => g.Id == "review-options-appearance")) return;
        var preview = new RibbonGroup { Id = "review-options-preview", Text = "Try Options dialogs" };
        Add(preview, "Open Options preview", () => { using var dialog = CreateOptionsAppearancePreview(); dialog.ShowDialog(this); });
        Add(preview, "Open ribbon/QAT options", () => { using var dialog = new RibbonQuickAccessCustomizationForm(ribbon, "RibbonKitDesignerConsumer"); dialog.ShowDialog(this); });
        var effects = new RibbonGroup { Id = "review-options-appearance", Text = "Options navigation" };
        Add(effects, "Rounded Options selection", () => Set((ribbon.Theme.OptionsAppearance ?? new()) with { NavigationCornerRadius = 10, NavigationSelectionInset = 3, NavigationTextPadding = 20, NavigationSelectedBackground = new(Color.LightYellow), NavigationFocusInset = 5 }));
        Add(effects, "Wide magenta Options marker", () => Set((ribbon.Theme.OptionsAppearance ?? new()) with { NavigationMarkerWidth = 6, NavigationMarkerInset = 10, NavigationMarkerFill = new(Color.DarkMagenta) }));
        Add(effects, "Hide Options marker", () => Set((ribbon.Theme.OptionsAppearance ?? new()) with { NavigationMarkerWidth = 0 }));
        Add(effects, "Thicker Options rules", () => Set((ribbon.Theme.OptionsAppearance ?? new()) with { DividerWidth = 3, DividerColor = Color.DarkBlue }));
        Add(effects, "Default Options marker", () => Set((ribbon.Theme.OptionsAppearance ?? new()) with { NavigationMarkerWidth = null, NavigationMarkerInset = null, NavigationMarkerFill = null }));
        Add(effects, "Default Options decoration", () => ribbon.Theme = ribbon.Theme with { OptionsAppearance = null });
        tab.Groups.AddRange(preview, effects);
        void Set(RibbonOptionsAppearanceSettings a) => ribbon.Theme = ribbon.Theme with { OptionsAppearance = a };
        void Add(RibbonGroup group, string text, Action action) { var item = new RibbonButtonItem { Text = text, ScreenTipDescription = "Choose the appearance, then open an Options preview. Existing native controls and Apply/Cancel remain in use.", SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium), SimplifiedPlacement = RibbonSimplifiedPlacement.Inline }; item.Click += (_, _) => action(); group.Items.Add(item); }
    }
    private RibbonOptionsDialog CreateOptionsAppearancePreview()
    {
        var dialog = new RibbonOptionsDialog(ribbon) { Text = "Options appearance preview" };
        var general = new RibbonOptionsPage { Text = "General", BackColor = ribbon.Theme.Surface, ForeColor = ribbon.Theme.Foreground }; var advanced = new RibbonOptionsPage { Text = "Advanced", BackColor = ribbon.Theme.Surface, ForeColor = ribbon.Theme.Foreground };
        general.Controls.Add(new TextBox { Text = "Editable application setting", Bounds = new(12, 12, 240, 28) }); advanced.Controls.Add(new CheckBox { Text = "Application-owned option", Checked = true, AutoSize = true, Location = new(12, 12) }); dialog.Pages.Add(general); dialog.Pages.Add(advanced); return dialog;
    }
    internal void VerifyOptionsAppearanceDemo(string output)
    {
        StartPosition = FormStartPosition.Manual; Location = new(-16000, -16000); Width = 1900; ShowInTaskbar = false; EnableOffice2019ThemeDemo(); Show(); Application.DoEvents(); var tab = ThemeEffectsReviewTab; ribbon.SelectedTab = tab;
        if (tab.Text != "Theme effects 7" || tab.Groups.Count != 8 || ThemeEffects6ReviewTab.Groups.Count != 3 || ThemeEffects5ReviewTab.Groups.Count != 6 || ThemeEffects4ReviewTab.Groups.Count != 7 || ThemeEffects3ReviewTab.Groups.Count != 4 || ThemeEffects2ReviewTab.Groups.Count != 5 || ribbon.Tabs.Single(t => t.Text == "Office 2019 Light").Groups.Count != 7) throw new InvalidOperationException("Options controls displaced earlier review groups.");
        var effects = tab.Groups.Single(g => g.Id == "review-options-appearance"); ribbon.Theme = ribbon.Theme with { OptionsButtonCornerRadius = 4, TipAppearance = new() { KeyTipBorderWidth = 3 }, GalleryContent = new() { LabelMaxLines = 3 } };
        effects.Items[0].PerformClick(); effects.Items[1].PerformClick(); effects.Items[3].PerformClick();
        if (ribbon.Theme.OptionsAppearance is not { NavigationCornerRadius: 10, NavigationTextPadding: 20, NavigationFocusInset: 5, NavigationMarkerWidth: 6, DividerWidth: 3, DividerColor: var color } || color != Color.DarkBlue) throw new InvalidOperationException("Options presets bypassed public settings.");
        using (var dialog = CreateOptionsAppearancePreview()) { dialog.StartPosition = FormStartPosition.Manual; dialog.Location = new(-16000, -16000); dialog.ShowInTaskbar = false; dialog.Show(this); Application.DoEvents(); if (dialog.Pages.Count != 2 || dialog.SelectedPage?.Text != "General") throw new InvalidOperationException("Options preview page contract changed."); using var image = new Bitmap(dialog.Width, dialog.Height); dialog.DrawToBitmap(image, new(Point.Empty, image.Size)); image.Save(Path.Combine(output, "review-options.png")); dialog.Close(); }
        effects.Items[2].PerformClick(); if (ribbon.Theme.OptionsAppearance!.NavigationMarkerWidth != 0) throw new InvalidOperationException("Options marker hide failed."); effects.Items[4].PerformClick(); if (ribbon.Theme.OptionsAppearance!.NavigationMarkerWidth is not null || ribbon.Theme.OptionsAppearance.DividerWidth != 3) throw new InvalidOperationException("Marker Reset cleared other roles."); effects.Items[5].PerformClick();
        if (ribbon.Theme.OptionsAppearance is not null || ribbon.Theme.OptionsButtonCornerRadius != 4 || ribbon.Theme.TipAppearance!.KeyTipBorderWidth != 3 || ribbon.Theme.GalleryContent!.LabelMaxLines != 3) throw new InvalidOperationException("Options Reset changed existing roles."); EnableOptionsAppearanceDemo(); if (tab.Groups.Count != 8) throw new InvalidOperationException("Options groups duplicated.");
        using var pixels = new Bitmap(ribbon.Width, ribbon.Height); ribbon.DrawToBitmap(pixels, ribbon.ClientRectangle); pixels.Save(Path.Combine(output, "review-tab.png")); Close();
    }
}
