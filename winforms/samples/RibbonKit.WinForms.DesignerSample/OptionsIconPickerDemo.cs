using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

public partial class DesignerForm
{
    private void EnableOptionsIconPickerDemo()
    {
        var tab = ThemeEffectsReviewTab; if (tab.Groups.Any(g => g.Id == "review-options-picker")) return;
        var preview = new RibbonGroup { Id = "review-options-picker-preview", Text = "Try group icon picker" };
        Add(preview, "Open group icon preview", () =>
        {
            using var model = CreateOptionsRowPreviewModel(RibbonLayoutMode.Classic); using var dialog = CreateOptionsRowPreview(model, false);
            dialog.Shown += (_, _) => dialog.BeginInvoke((Action)(() => { if (dialog.IsDisposed) return; try { ((Button)dialog.RibbonEditor.Controls.Find("NewRibbonGroup", true).Single()).PerformClick(); } finally { dialog.Close(); } }));
            dialog.ShowDialog(this);
        });
        var selection = new RibbonGroup { Id = "review-options-picker", Text = "Icon picker selection" };
        Add(selection, "Rounded outlined icon tiles", () => Set(Current() with { IconPickerCornerRadius = 9, IconPickerBorderWidth = 3, IconPickerBorderColor = Color.Navy }));
        Add(selection, "Inset icon-tile selection", () => Set(Current() with { IconPickerInset = 4 }));
        Add(selection, "Gradient icon-tile fill", () => Set(Current() with { IconPickerSelectedBackground = new(Color.LightBlue, Color.LightGreen) }));
        Add(selection, "Hide icon-tile outline", () => Set(Current() with { IconPickerBorderWidth = 0 }));
        Add(selection, "Hide icon-tile fill", () => Set(Current() with { IconPickerSelectedBackground = new(Color.Transparent) }));
        Add(selection, "Default picker selection", () => Set(Current() with { IconPickerCornerRadius = null, IconPickerInset = null, IconPickerBorderWidth = null, IconPickerBorderColor = Color.Empty, IconPickerSelectedBackground = null }));
        var content = new RibbonGroup { Id = "review-options-picker-content", Text = "Icon picker content" };
        Add(content, "Smaller picker tiles", () => Set(Current() with { IconPickerIconSize = 16 }));
        Add(content, "Larger picker tiles", () => Set(Current() with { IconPickerIconSize = 48 }));
        Add(content, "Hide picker artwork", () => Set(Current() with { IconPickerIconSize = 0 }));
        Add(content, "Inset None label", () => Set(Current() with { IconPickerTextInset = 8 }));
        Add(content, "Default picker content", () => Set(Current() with { IconPickerIconSize = null, IconPickerTextInset = null }));
        Add(content, "Default all picker effects", () => Set(Current() with { IconPickerCornerRadius = null, IconPickerInset = null, IconPickerBorderWidth = null, IconPickerBorderColor = Color.Empty, IconPickerSelectedBackground = null, IconPickerIconSize = null, IconPickerTextInset = null }));
        tab.Groups.AddRange(preview, selection, content);
        RibbonOptionsAppearanceSettings Current() => ribbon.Theme.OptionsAppearance ?? new();
        void Set(RibbonOptionsAppearanceSettings a) => ribbon.Theme = ribbon.Theme with { OptionsAppearance = a };
        void Add(RibbonGroup group, string text, Action action) { var item = new RibbonButtonItem { Text = text, ScreenTipDescription = "Choose presets before opening. Artwork size resizes shared square cells for artwork and None; selection inset constrains both content types. The preview uses sample commands and closes after the group editor.", SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium), SimplifiedPlacement = RibbonSimplifiedPlacement.Inline }; item.Click += (_, _) => action(); group.Items.Add(item); }
    }
    internal void VerifyOptionsIconPickerDemo(string output)
    {
        StartPosition = FormStartPosition.Manual; Location = new(-16000, -16000); Width = 1900; ShowInTaskbar = false; EnableOffice2019ThemeDemo(); Show(); Application.DoEvents(); var tab = ThemeEffectsReviewTab; ribbon.SelectedTab = tab;
        if (tab.Text != "Theme effects 10" || tab.Groups.Count != 5 || ThemeEffects9ReviewTab.Groups.Count != 6 || ThemeEffects8ReviewTab.Groups.Count != 5 || ThemeEffects7ReviewTab.Groups.Count != 8 || ThemeEffects6ReviewTab.Groups.Count != 3 || ThemeEffects5ReviewTab.Groups.Count != 6 || ThemeEffects4ReviewTab.Groups.Count != 7 || ThemeEffects3ReviewTab.Groups.Count != 4 || ThemeEffects2ReviewTab.Groups.Count != 5 || ribbon.Tabs.Single(t => t.Text == "Office 2019 Light").Groups.Count != 7) throw new InvalidOperationException("Picker groups displaced accepted tabs.");
        var selection = tab.Groups.Single(g => g.Id == "review-options-picker"); var content = tab.Groups.Single(g => g.Id == "review-options-picker-content"); ribbon.Theme = ribbon.Theme with { OptionsAppearance = new() { TreeCheckSize = 12, RowIconSize = 16, FrameBorderWidth = 3 } }; var sources = ribbon.Tabs.ToArray(); var mode = ribbon.LayoutMode;
        foreach (int i in new[] { 0, 1, 2 }) selection.Items[i].PerformClick(); content.Items[0].PerformClick(); content.Items[3].PerformClick(); if (ribbon.Theme.OptionsAppearance is not { IconPickerCornerRadius: 9, IconPickerInset: 4, IconPickerBorderWidth: 3, IconPickerIconSize: 16, IconPickerTextInset: 8 }) throw new InvalidOperationException("Picker presets bypassed public settings.");
        content.Items[1].PerformClick();
        using (var model = CreateOptionsRowPreviewModel(RibbonLayoutMode.Classic)) OptionsIconPickerCheck.WithPicker(model, this, (dialog, _) => { using var image = new Bitmap(dialog.Width, dialog.Height); dialog.DrawToBitmap(image, new(Point.Empty, image.Size)); image.Save(Path.Combine(output, "review-picker.png")); });
        selection.Items[3].PerformClick(); selection.Items[4].PerformClick(); content.Items[2].PerformClick(); if (ribbon.Theme.OptionsAppearance is not { IconPickerBorderWidth: 0, IconPickerIconSize: 0 } || ribbon.Theme.OptionsAppearance.IconPickerSelectedBackground?.Start.A != 0) throw new InvalidOperationException("Picker hide presets failed."); selection.Items[5].PerformClick(); if (ribbon.Theme.OptionsAppearance.IconPickerTextInset != 8 || ribbon.Theme.OptionsAppearance.IconPickerBorderWidth is not null) throw new InvalidOperationException("Picker selection reset cleared content."); content.Items[1].PerformClick(); if (ribbon.Theme.OptionsAppearance.IconPickerIconSize != 48) throw new InvalidOperationException("Larger artwork preset failed."); content.Items[4].PerformClick(); content.Items[5].PerformClick(); if (ribbon.Theme.OptionsAppearance is not { IconPickerIconSize: null, IconPickerTextInset: null, TreeCheckSize: 12, RowIconSize: 16, FrameBorderWidth: 3 } || !sources.SequenceEqual(ribbon.Tabs) || ribbon.LayoutMode != mode) throw new InvalidOperationException("Picker preview/reset changed accepted roles or real sources.");
        EnableOptionsIconPickerDemo(); if (tab.Groups.Count != 5) throw new InvalidOperationException("Picker groups duplicated."); using var pixels = new Bitmap(ribbon.Width, ribbon.Height); ribbon.DrawToBitmap(pixels, ribbon.ClientRectangle); pixels.Save(Path.Combine(output, "review-tab.png")); Close();
    }
}
