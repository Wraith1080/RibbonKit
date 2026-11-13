using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

public partial class DesignerForm
{
    private void EnableOptionsRowDemo()
    {
        var tab = ThemeEffectsReviewTab; if (tab.Groups.Any(g => g.Id == "review-options-rows")) return;
        var preview = new RibbonGroup { Id = "review-options-row-preview", Text = "Try command rows" };
        Add(preview, "Open QAT row preview", () => Open(RibbonLayoutMode.Classic, true));
        Add(preview, "Open Classic row preview", () => Open(RibbonLayoutMode.Classic, false));
        Add(preview, "Open Simplified row preview", () => Open(RibbonLayoutMode.Simplified, false));
        var content = new RibbonGroup { Id = "review-options-rows", Text = "Command row content" };
        Add(content, "Roomier row text", () => Set(Current() with { RowTextPadding = 14, RowIconTextGap = 12 }));
        Add(content, "Larger row icons", () => Set(Current() with { RowIconSize = 28 }));
        Add(content, "Hide row icons", () => Set(Current() with { RowIconSize = 0 }));
        Add(content, "Remove icon-text gap", () => Set(Current() with { RowIconTextGap = 0 }));
        Add(content, "Regular row headings", () => Set(Current() with { RowHeadingBold = false }));
        Add(content, "Default row content", () => Set(Current() with { RowTextPadding = null, RowIconSize = null, RowIconTextGap = null, RowHeadingBold = null }));
        var selection = new RibbonGroup { Id = "review-options-row-selection", Text = "Command row selection" };
        Add(selection, "Rounded gradient rows", () => Set(Current() with { RowCornerRadius = 9, RowSelectedBackground = new(Color.LightBlue, Color.LightGreen) }));
        Add(selection, "Hide row selection fill", () => Set(Current() with { RowSelectedBackground = new(Color.Transparent) }));
        Add(selection, "Square row selection", () => Set(Current() with { RowCornerRadius = 0 }));
        Add(selection, "Default row selection", () => Set(Current() with { RowCornerRadius = null, RowSelectedBackground = null }));
        Add(selection, "Default all row effects", () => Set(Current() with { RowTextPadding = null, RowIconSize = null, RowIconTextGap = null, RowHeadingBold = null, RowCornerRadius = null, RowSelectedBackground = null }));
        tab.Groups.AddRange(preview, content, selection);
        RibbonOptionsAppearanceSettings Current() => ribbon.Theme.OptionsAppearance ?? new();
        void Set(RibbonOptionsAppearanceSettings a) => ribbon.Theme = ribbon.Theme with { OptionsAppearance = a };
        void Open(RibbonLayoutMode mode, bool qat) { using var model = CreateOptionsRowPreviewModel(mode); using var dialog = CreateOptionsRowPreview(model, qat); dialog.ShowDialog(this); }
        void Add(RibbonGroup group, string text, Action action) { var item = new RibbonButtonItem { Text = text, ScreenTipDescription = "Choose presets before opening. These previews use sample commands; edits stay in the preview.", SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium), SimplifiedPlacement = RibbonSimplifiedPlacement.Inline }; item.Click += (_, _) => action(); group.Items.Add(item); }
    }
    private RibbonControl CreateOptionsRowPreviewModel(RibbonLayoutMode mode)
    {
        var model = new RibbonControl { Theme = ribbon.Theme, Renderer = ribbon.Renderer, Font = ribbon.Font, Density = ribbon.Density, LayoutMode = mode, AnimationLevel = RibbonAnimationLevel.None };
        foreach (string name in new[] { "Home", "Insert" }) { var tab = new RibbonTab { Id = "row-preview." + name, Text = name }; var group = new RibbonGroup { Id = tab.Id + ".commands", Text = "Sample commands", Icon = RibbonStockIcons.Create("open") }; foreach (string command in new[] { "save", "open", "print" }) group.Items.Add(new RibbonButtonItem { Id = group.Id + "." + command, Text = name + " " + command, Icon = RibbonStockIcons.Create(command), SimplifiedPlacement = RibbonSimplifiedPlacement.Inline }); tab.Groups.Add(group); model.Tabs.Add(tab); }
        model.QuickAccessItems.Add(new RibbonQuickAccessReference { Source = (RibbonButtonItem)model.Tabs[0].Groups[0].Items[0] }); model.CommandCatalog.CaptureDefaults(); return model;
    }
    private static RibbonQuickAccessCustomizationForm CreateOptionsRowPreview(RibbonControl model, bool qat)
    { var dialog = new RibbonQuickAccessCustomizationForm(model, "RibbonKit.Rows.Preview") { Text = "Customization command row preview" }; dialog.SelectedPage = qat ? dialog.Pages.First(p => p.Name == "QuickAccessOptionsPage") : dialog.Pages.First(p => p.Name != "QuickAccessOptionsPage"); return dialog; }
    internal void VerifyOptionsRowDemo(string output)
    {
        StartPosition = FormStartPosition.Manual; Location = new(-16000, -16000); Width = 1800; ShowInTaskbar = false; EnableOffice2019ThemeDemo(); Show(); Application.DoEvents(); var tab = ThemeEffectsReviewTab; ribbon.SelectedTab = tab;
        if (tab.Text != "Theme effects 9" || tab.Groups.Count != 6 || ThemeEffects8ReviewTab.Groups.Count != 5 || ThemeEffects7ReviewTab.Groups.Count != 8 || ThemeEffects6ReviewTab.Groups.Count != 3 || ThemeEffects5ReviewTab.Groups.Count != 6 || ThemeEffects4ReviewTab.Groups.Count != 7 || ThemeEffects3ReviewTab.Groups.Count != 4 || ThemeEffects2ReviewTab.Groups.Count != 5 || ribbon.Tabs.Single(t => t.Text == "Office 2019 Light").Groups.Count != 7) throw new InvalidOperationException("Row groups displaced accepted tabs.");
        var content = tab.Groups.Single(g => g.Id == "review-options-rows"); var selection = tab.Groups.Single(g => g.Id == "review-options-row-selection"); ribbon.Theme = ribbon.Theme with { OptionsAppearance = new() { FrameBorderWidth = 3, ButtonBorderWidth = 2, NavigationCornerRadius = 12 } }; var sources = ribbon.Tabs.ToArray(); var mode = ribbon.LayoutMode;
        foreach (int i in new[] { 0, 1, 4 }) content.Items[i].PerformClick(); selection.Items[0].PerformClick(); if (ribbon.Theme.OptionsAppearance is not { RowTextPadding: 14, RowIconSize: 28, RowIconTextGap: 12, RowHeadingBold: false, RowCornerRadius: 9 }) throw new InvalidOperationException("Row presets bypassed public settings.");
        foreach (var previewMode in new[] { RibbonLayoutMode.Classic, RibbonLayoutMode.Simplified }) using (var model = CreateOptionsRowPreviewModel(previewMode)) using (var dialog = CreateOptionsRowPreview(model, false)) { dialog.StartPosition = FormStartPosition.Manual; dialog.Location = new(-16000, -16000); dialog.ShowInTaskbar = false; dialog.Show(this); Application.DoEvents(); using var image = new Bitmap(dialog.Width, dialog.Height); dialog.DrawToBitmap(image, new(Point.Empty, image.Size)); image.Save(Path.Combine(output, "review-rows-" + previewMode + ".png")); dialog.Close(); }
        content.Items[2].PerformClick(); if (ribbon.Theme.OptionsAppearance!.RowIconSize != 0) throw new InvalidOperationException("Row icon hide failed."); content.Items[3].PerformClick(); selection.Items[1].PerformClick(); if (ribbon.Theme.OptionsAppearance.RowSelectedBackground?.Start.A != 0 || ribbon.Theme.OptionsAppearance.RowIconTextGap != 0) throw new InvalidOperationException("Row gap/fill hide failed.");
        content.Items[5].PerformClick(); if (ribbon.Theme.OptionsAppearance.RowCornerRadius != 9 || ribbon.Theme.OptionsAppearance.RowIconSize is not null) throw new InvalidOperationException("Content reset cleared row selection."); selection.Items[2].PerformClick(); selection.Items[3].PerformClick(); selection.Items[4].PerformClick(); if (ribbon.Theme.OptionsAppearance is not { RowCornerRadius: null, RowSelectedBackground: null, FrameBorderWidth: 3, ButtonBorderWidth: 2, NavigationCornerRadius: 12 } || !sources.SequenceEqual(ribbon.Tabs) || ribbon.LayoutMode != mode) throw new InvalidOperationException("Row reset/preview changed accepted roles or the real ribbon.");
        EnableOptionsRowDemo(); if (tab.Groups.Count != 6) throw new InvalidOperationException("Row groups duplicated."); using var pixels = new Bitmap(ribbon.Width, ribbon.Height); ribbon.DrawToBitmap(pixels, ribbon.ClientRectangle); pixels.Save(Path.Combine(output, "review-tab.png")); Close();
    }
}
