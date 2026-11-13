using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

public partial class DesignerForm
{
    private void EnableOptionsTreeDemo()
    {
        var tab = ThemeEffectsReviewTab; if (tab.Groups.Any(g => g.Id == "review-options-tree")) return;
        var preview = new RibbonGroup { Id = "review-options-tree-preview", Text = "Try customization tree" };
        Add(preview, "Open tree preview", () => { using var model = CreateOptionsRowPreviewModel(RibbonLayoutMode.Classic); using var dialog = CreateTreePreview(model); dialog.ShowDialog(this); });
        var glyphs = new RibbonGroup { Id = "review-options-tree", Text = "Tree glyphs" };
        Add(glyphs, "Smaller tree checkboxes", () => Set(Current() with { TreeCheckSize = 12 }));
        Add(glyphs, "Hide tree checkbox paint", () => Set(Current() with { TreeCheckSize = 0 }));
        Add(glyphs, "Smaller inset tree icons", () => Set(Current() with { TreeIconSize = 10, TreeIconTextGap = 4 }));
        Add(glyphs, "Hide tree icons", () => Set(Current() with { TreeIconSize = 0 }));
        Add(glyphs, "Larger shared chevrons", () => ribbon.Theme = ribbon.Theme with { Chevrons = new() { Length = 20, Depth = 10, StrokeWidth = 2.5 } });
        Add(glyphs, "Hide shared chevrons", () => ribbon.Theme = ribbon.Theme with { Chevrons = new() { StrokeWidth = 0 } });
        Add(glyphs, "Default tree glyphs", () => Set(Current() with { TreeCheckSize = null, TreeIconSize = null, TreeIconTextGap = null }));
        Add(glyphs, "Default shared chevrons", () => ribbon.Theme = ribbon.Theme with { Chevrons = null });
        var state = new RibbonGroup { Id = "review-options-tree-state", Text = "Tree selection and focus" };
        Add(state, "Rounded gradient tree rows", () => Set(Current() with { TreeCornerRadius = 9, TreeSelectedBackground = new(Color.LightBlue, Color.LightGreen) }));
        Add(state, "Hide tree selection fill", () => Set(Current() with { TreeSelectedBackground = new(Color.Transparent) }));
        Add(state, "Inset tree focus", () => Set(Current() with { TreeFocusInset = 5 }));
        Add(state, "Default tree selection/focus", () => Set(Current() with { TreeCornerRadius = null, TreeSelectedBackground = null, TreeFocusInset = null }));
        Add(state, "Default all tree effects", () => Set(Current() with { TreeCornerRadius = null, TreeSelectedBackground = null, TreeFocusInset = null, TreeCheckSize = null, TreeIconSize = null, TreeIconTextGap = null }));
        tab.Groups.AddRange(preview, glyphs, state);
        RibbonOptionsAppearanceSettings Current() => ribbon.Theme.OptionsAppearance ?? new();
        void Set(RibbonOptionsAppearanceSettings a) => ribbon.Theme = ribbon.Theme with { OptionsAppearance = a };
        void Add(RibbonGroup group, string text, Action action) { var item = new RibbonButtonItem { Text = text, ScreenTipDescription = "Choose presets before opening. Glyphs fit existing tree slots; hiding their paint keeps click targets. Shared chevrons also affect other ribbon utilities. Preview edits use sample commands.", SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium), SimplifiedPlacement = RibbonSimplifiedPlacement.Inline }; item.Click += (_, _) => action(); group.Items.Add(item); }
    }
    private static RibbonQuickAccessCustomizationForm CreateTreePreview(RibbonControl model)
    { var dialog = CreateOptionsRowPreview(model, false); dialog.Text = "Customization tree preview"; return dialog; }
    internal void VerifyOptionsTreeDemo(string output)
    {
        StartPosition = FormStartPosition.Manual; Location = new(-16000, -16000); Width = 1900; ShowInTaskbar = false; EnableOffice2019ThemeDemo(); Show(); Application.DoEvents(); var tab = ThemeEffectsReviewTab; ribbon.SelectedTab = tab;
        if (tab.Text != "Theme effects 9" || tab.Groups.Count != 6 || ThemeEffects8ReviewTab.Groups.Count != 5 || ThemeEffects7ReviewTab.Groups.Count != 8 || ThemeEffects6ReviewTab.Groups.Count != 3 || ThemeEffects5ReviewTab.Groups.Count != 6 || ThemeEffects4ReviewTab.Groups.Count != 7 || ThemeEffects3ReviewTab.Groups.Count != 4 || ThemeEffects2ReviewTab.Groups.Count != 5 || ribbon.Tabs.Single(t => t.Text == "Office 2019 Light").Groups.Count != 7) throw new InvalidOperationException("Tree groups displaced accepted tabs.");
        var glyphs = tab.Groups.Single(g => g.Id == "review-options-tree"); var state = tab.Groups.Single(g => g.Id == "review-options-tree-state"); ribbon.Theme = ribbon.Theme with { OptionsAppearance = new() { RowIconSize = 16, RowCornerRadius = 6, FrameBorderWidth = 3, NavigationCornerRadius = 12 } }; var sources = ribbon.Tabs.ToArray(); var mode = ribbon.LayoutMode;
        foreach (int i in new[] { 0, 2, 4 }) glyphs.Items[i].PerformClick(); state.Items[0].PerformClick(); state.Items[2].PerformClick(); if (ribbon.Theme.OptionsAppearance is not { TreeCheckSize: 12, TreeIconSize: 10, TreeIconTextGap: 4, TreeCornerRadius: 9, TreeFocusInset: 5 } || ribbon.Theme.Chevrons?.Length != 20) throw new InvalidOperationException("Tree presets bypassed public roles.");
        using (var model = CreateOptionsRowPreviewModel(RibbonLayoutMode.Classic)) using (var dialog = CreateTreePreview(model)) { dialog.StartPosition = FormStartPosition.Manual; dialog.Location = new(-16000, -16000); dialog.ShowInTaskbar = false; dialog.Show(this); Application.DoEvents(); using var image = new Bitmap(dialog.Width, dialog.Height); dialog.DrawToBitmap(image, new(Point.Empty, image.Size)); image.Save(Path.Combine(output, "review-tree.png")); dialog.Close(); }
        glyphs.Items[1].PerformClick(); glyphs.Items[3].PerformClick(); glyphs.Items[5].PerformClick(); state.Items[1].PerformClick(); if (ribbon.Theme.OptionsAppearance is not { TreeCheckSize: 0, TreeIconSize: 0 } || ribbon.Theme.OptionsAppearance.TreeSelectedBackground?.Start.A != 0 || ribbon.Theme.Chevrons?.StrokeWidth != 0) throw new InvalidOperationException("Tree hide presets failed.");
        glyphs.Items[6].PerformClick(); if (ribbon.Theme.OptionsAppearance is not { TreeCheckSize: null, TreeFocusInset: 5, TreeCornerRadius: 9 }) throw new InvalidOperationException("Tree glyph reset cleared selection/focus."); glyphs.Items[7].PerformClick(); if (ribbon.Theme.Chevrons is not null || ribbon.Theme.OptionsAppearance.TreeCornerRadius != 9) throw new InvalidOperationException("Shared chevron reset cleared tree roles."); glyphs.Items[0].PerformClick(); state.Items[3].PerformClick(); if (ribbon.Theme.OptionsAppearance.TreeCheckSize != 12) throw new InvalidOperationException("Selection reset cleared tree glyphs."); state.Items[4].PerformClick(); if (ribbon.Theme.OptionsAppearance is not { TreeCheckSize: null, TreeCornerRadius: null, RowIconSize: 16, RowCornerRadius: 6, FrameBorderWidth: 3, NavigationCornerRadius: 12 } || !sources.SequenceEqual(ribbon.Tabs) || ribbon.LayoutMode != mode) throw new InvalidOperationException("Tree preview/reset changed accepted roles or the real ribbon.");
        EnableOptionsTreeDemo(); if (tab.Groups.Count != 6) throw new InvalidOperationException("Tree groups duplicated."); using var pixels = new Bitmap(ribbon.Width, ribbon.Height); ribbon.DrawToBitmap(pixels, ribbon.ClientRectangle); pixels.Save(Path.Combine(output, "review-tab.png")); Close();
    }
}
