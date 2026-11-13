using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

public partial class DesignerForm
{
    private void EnableOptionsFieldDemo()
    {
        var tab = ThemeEffects8ReviewTab; if (tab.Groups.Any(g => g.Id == "review-options-fields")) return;
        var preview = new RibbonGroup { Id = "review-options-field-preview", Text = "Try frames and choices" };
        Add(preview, "Open frame/choice preview", () => { using var model = CreateOptionsFieldPreviewModel(); using var dialog = CreateOptionsFieldPreview(model); dialog.ShowDialog(this); });
        var effects = new RibbonGroup { Id = "review-options-fields", Text = "Options frames and text" };
        Add(effects, "Rounded Options frames", () => Set((ribbon.Theme.OptionsAppearance ?? new()) with { FrameCornerRadius = 12 }));
        Add(effects, "Thicker Options frames", () => Set((ribbon.Theme.OptionsAppearance ?? new()) with { FrameBorderWidth = 3, FrameBorderColor = Color.Navy }));
        Add(effects, "Hide Options frame outlines", () => Set((ribbon.Theme.OptionsAppearance ?? new()) with { FrameBorderWidth = 0 }));
        Add(effects, "Inset green editor text", () => ribbon.Theme = ribbon.Theme with { EditorTextInset = 12, EditorForeground = Color.DarkGreen });
        Add(effects, "Outlined choice fields", () => ribbon.Theme = ribbon.Theme with { EditorAppearance = (ribbon.Theme.EditorAppearance ?? new()) with { CornerRadius = 8, BorderWidth = 3, Inset = 2 } });
        Add(effects, "Default Options frames", () => Set((ribbon.Theme.OptionsAppearance ?? new()) with { FrameCornerRadius = null, FrameBorderWidth = null, FrameBorderColor = Color.Empty }));
        Add(effects, "Default editor text", () => ribbon.Theme = ribbon.Theme with { EditorTextInset = null, EditorForeground = Color.Empty });
        Add(effects, "Default choice fields", () => ribbon.Theme = ribbon.Theme with { EditorAppearance = (ribbon.Theme.EditorAppearance ?? new()) with { CornerRadius = null, BorderWidth = null, Inset = null } });
        tab.Groups.AddRange(preview, effects);
        void Set(RibbonOptionsAppearanceSettings a) => ribbon.Theme = ribbon.Theme with { OptionsAppearance = a };
        void Add(RibbonGroup group, string text, Action action) { var item = new RibbonButtonItem { Text = text, ScreenTipDescription = "Choose presets before opening the preview. The preview uses sample commands; changes stay in the preview.", SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium), SimplifiedPlacement = RibbonSimplifiedPlacement.Inline }; item.Click += (_, _) => action(); group.Items.Add(item); }
    }
    private RibbonControl CreateOptionsFieldPreviewModel()
    {
        var model = new RibbonControl { Theme = ribbon.Theme, Renderer = ribbon.Renderer, Font = ribbon.Font, Density = ribbon.Density, LayoutMode = RibbonLayoutMode.Simplified, AnimationLevel = RibbonAnimationLevel.None };
        foreach (var label in new[] { "Home", "Insert" }) { var tab = new RibbonTab { Id = "field-preview." + label, Text = label }; var group = new RibbonGroup { Id = tab.Id + ".commands", Text = "Commands" }; group.Items.Add(new RibbonButtonItem { Id = group.Id + ".save", Text = "Sample " + label }); tab.Groups.Add(group); model.Tabs.Add(tab); } model.CommandCatalog.CaptureDefaults(); return model;
    }
    private static RibbonQuickAccessCustomizationForm CreateOptionsFieldPreview(RibbonControl model)
    { var dialog = new RibbonQuickAccessCustomizationForm(model, "RibbonKit.Fields.Preview") { Text = "Options frame and choice preview" }; dialog.SelectedPage = dialog.Pages.First(p => p.Name != "QuickAccessOptionsPage"); return dialog; }
    internal void VerifyOptionsFieldDemo(string output)
    {
        StartPosition = FormStartPosition.Manual; Location = new(-16000, -16000); Width = 1800; ShowInTaskbar = false; EnableOffice2019ThemeDemo(); Show(); Application.DoEvents(); var tab = ThemeEffects8ReviewTab; ribbon.SelectedTab = tab;
        if (tab.Text != "Theme effects 8" || tab.Groups.Count != 5 || ThemeEffects7ReviewTab.Groups.Count != 8 || ThemeEffects6ReviewTab.Groups.Count != 3 || ThemeEffects5ReviewTab.Groups.Count != 6 || ThemeEffects4ReviewTab.Groups.Count != 7 || ThemeEffects3ReviewTab.Groups.Count != 4 || ThemeEffects2ReviewTab.Groups.Count != 5 || ribbon.Tabs.Single(t => t.Text == "Office 2019 Light").Groups.Count != 7) throw new InvalidOperationException("Frame controls displaced accepted tabs.");
        var effects = tab.Groups.Single(g => g.Id == "review-options-fields"); var sources = ribbon.Tabs.ToArray(); var mode = ribbon.LayoutMode; ribbon.Theme = ribbon.Theme with { OptionsAppearance = new() { ButtonBorderWidth = 2, HeadingBold = false, NavigationCornerRadius = 12 } };
        foreach (int i in new[] { 0, 1, 3, 4 }) effects.Items[i].PerformClick(); if (ribbon.Theme.OptionsAppearance is not { FrameCornerRadius: 12, FrameBorderWidth: 3 } || ribbon.Theme.EditorTextInset != 12 || ribbon.Theme.EditorForeground != Color.DarkGreen || ribbon.Theme.EditorAppearance?.BorderWidth != 3) throw new InvalidOperationException("Frame presets bypassed public roles.");
        using (var model = CreateOptionsFieldPreviewModel()) using (var dialog = CreateOptionsFieldPreview(model)) { dialog.StartPosition = FormStartPosition.Manual; dialog.Location = new(-16000, -16000); dialog.ShowInTaskbar = false; dialog.Show(this); Application.DoEvents(); using var image = new Bitmap(dialog.Width, dialog.Height); dialog.DrawToBitmap(image, new(Point.Empty, image.Size)); image.Save(Path.Combine(output, "review-fields.png")); dialog.Close(); }
        effects.Items[2].PerformClick(); if (ribbon.Theme.OptionsAppearance!.FrameBorderWidth != 0) throw new InvalidOperationException("Frame outline hide failed."); effects.Items[5].PerformClick(); if (ribbon.Theme.OptionsAppearance!.ButtonBorderWidth != 2 || ribbon.Theme.OptionsAppearance.HeadingBold != false || ribbon.Theme.OptionsAppearance.NavigationCornerRadius != 12 || ribbon.Theme.OptionsAppearance.FrameCornerRadius is not null) throw new InvalidOperationException("Frame reset cleared accepted roles."); effects.Items[6].PerformClick(); effects.Items[7].PerformClick(); if (ribbon.Theme.EditorTextInset is not null || !ribbon.Theme.EditorForeground.IsEmpty || ribbon.Theme.EditorAppearance!.BorderWidth is not null || !sources.SequenceEqual(ribbon.Tabs) || ribbon.LayoutMode != mode) throw new InvalidOperationException("Preview/reset changed the real ribbon or existing roles.");
        EnableOptionsFieldDemo(); if (tab.Groups.Count != 5) throw new InvalidOperationException("Frame groups duplicated."); using var pixels = new Bitmap(ribbon.Width, ribbon.Height); ribbon.DrawToBitmap(pixels, ribbon.ClientRectangle); pixels.Save(Path.Combine(output, "review-tab.png")); Close();
    }
}
