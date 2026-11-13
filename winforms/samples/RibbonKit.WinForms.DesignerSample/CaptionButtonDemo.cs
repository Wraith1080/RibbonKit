using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

public partial class DesignerForm
{
    private RibbonForm CreateCaptionButtonPreview()
    {
        var form = new RibbonForm { Text = "Caption button preview — close this window to return", ClientSize = new(960, 540), Backdrop = Backdrop,
            Font = Font, TitleBarColor = TitleBarColor, TitleBarTintOpacity = TitleBarTintOpacity };
        var model = new RibbonControl { Theme = ribbon.Theme, FileMenuMode = RibbonFileMenuMode.Backstage, AnimationLevel = ribbon.AnimationLevel };
        model.FileMenu.Visible = true; model.Tabs.Add(new RibbonTab { Text = "Preview" });
        var page = new TextBox { Multiline = true, Text = "Native preview page. Use Back or Escape to close Backstage." };
        model.Backstage.Items.Add(new RibbonBackstagePageItem { Text = "Info", Content = page });
        form.Disposed += (_, _) => page.Dispose();
        form.Controls.Add(model); form.Ribbon = model;
        form.Controls.Add(new Label { Text = "Hover and press the caption buttons. Maximize/restore changes the middle glyph. Closing this preview leaves the main sample open.", AutoSize = true, Location = new(20, 250) });
        return form;
    }
    private void EnableCaptionButtonDemo()
    {
        var tab = ThemeEffectsReviewTab;
        if (tab.Groups.Any(g => g.Id == "review-caption-buttons")) return;
        var preview = new RibbonGroup { Id = "review-caption-button-preview", Text = "Try caption buttons" };
        Add(preview, "Open caption button preview", () => { using var form = CreateCaptionButtonPreview(); form.ShowDialog(this); });
        var states = new RibbonGroup { Id = "review-caption-buttons", Text = "Caption button fills" };
        Add(states, "Blue hover / green pressed", () => Set(Current() with { HoverBackground = new(Color.LightBlue, Color.RoyalBlue), PressedBackground = new(Color.LightGreen) }));
        Add(states, "Purple close states", () => Set(Current() with { CloseHoverBackground = new(Color.MediumPurple), ClosePressedBackground = new(Color.Indigo) }));
        Add(states, "Translucent caption states", () => Set(Current() with { HoverBackground = new(Color.FromArgb(80, Color.RoyalBlue)), PressedBackground = new(Color.FromArgb(140, Color.RoyalBlue)), CloseHoverBackground = new(Color.FromArgb(120, Color.Purple)), ClosePressedBackground = new(Color.FromArgb(180, Color.Purple)) }));
        Add(states, "Hide caption state fills", () => Set(Current() with { HoverBackground = new(Color.Transparent), PressedBackground = new(Color.Transparent), CloseHoverBackground = new(Color.Transparent), ClosePressedBackground = new(Color.Transparent) }));
        Add(states, "Default caption state fills", () => Set(Current() with { HoverBackground = null, PressedBackground = null, CloseHoverBackground = null, ClosePressedBackground = null }));
        var shape = new RibbonGroup { Id = "review-caption-button-shape", Text = "Caption button shape" };
        Add(shape, "Rounded inset caption states", () => Set(Current() with { StateInset = 4, CornerRadius = 8 }));
        Add(shape, "Square caption states", () => Set(Current() with { CornerRadius = 0 }));
        Add(shape, "Default caption shape", () => Set(Current() with { StateInset = null, CornerRadius = null }));
        var glyphs = new RibbonGroup { Id = "review-caption-button-glyphs", Text = "Caption button glyphs" };
        Add(glyphs, "Larger caption glyphs", () => Set(Current() with { GlyphSize = 18 }));
        Add(glyphs, "Thicker caption glyphs", () => Set(Current() with { GlyphStrokeWidth = 2 }));
        Add(glyphs, "Colored caption glyphs", () => Set(Current() with { GlyphColor = Color.DarkOrange, CloseHotGlyphColor = Color.Gold }));
        Add(glyphs, "Wider restore offset", () => Set(Current() with { RestoreOffset = 6 }));
        Add(glyphs, "Hide caption glyphs", () => Set(Current() with { GlyphSize = 0 }));
        Add(glyphs, "Default caption glyphs", () => Set(Current() with { GlyphSize = null, GlyphStrokeWidth = null, GlyphColor = Color.Empty, CloseHotGlyphColor = Color.Empty, RestoreOffset = null }));
        Add(glyphs, "Default all caption buttons", () => ribbon.Theme = ribbon.Theme with { CaptionButtonAppearance = null });
        tab.Groups.AddRange(preview, states, shape, glyphs);
        RibbonCaptionButtonAppearanceSettings Current() => ribbon.Theme.CaptionButtonAppearance ?? new();
        void Set(RibbonCaptionButtonAppearanceSettings value) => ribbon.Theme = ribbon.Theme with { CaptionButtonAppearance = value };
        void Add(RibbonGroup group, string text, Action action)
        {
            var item = new RibbonButtonItem { Text = text, ScreenTipDescription = "Choose presets, then hover/press caption buttons or open the separate preview. Fill, shape and glyph Reset retain each other; native window targets and title animation stay unchanged.", SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium), SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
            item.Click += (_, _) => action(); group.Items.Add(item);
        }
    }
    internal void VerifyCaptionButtonDemo(string output)
    {
        Directory.CreateDirectory(output); StartPosition = FormStartPosition.Manual; Location = new(-16000, -16000); Width = 1900; ShowInTaskbar = false;
        EnableOffice2019ThemeDemo(); Show(); Application.DoEvents(); var tab = ThemeEffectsReviewTab; ribbon.SelectedTab = tab;
        if (tab.Text != "Theme effects 11" || tab.Groups.Count != 4 || ThemeEffects10ReviewTab.Groups.Count != 5 || ThemeEffects9ReviewTab.Groups.Count != 6 || ThemeEffects8ReviewTab.Groups.Count != 5 || ThemeEffects7ReviewTab.Groups.Count != 8 || ThemeEffects6ReviewTab.Groups.Count != 3 || ThemeEffects5ReviewTab.Groups.Count != 6 || ThemeEffects4ReviewTab.Groups.Count != 7 || ThemeEffects3ReviewTab.Groups.Count != 4 || ThemeEffects2ReviewTab.Groups.Count != 5 || ribbon.Tabs.Single(t => t.Text == "Office 2019 Light").Groups.Count != 7) throw new InvalidOperationException("Caption-button controls displaced accepted tabs.");
        var states = tab.Groups.Single(g => g.Id == "review-caption-buttons"); var shape = tab.Groups.Single(g => g.Id == "review-caption-button-shape"); var glyphs = tab.Groups.Single(g => g.Id == "review-caption-button-glyphs");
        states.Items[0].PerformClick(); states.Items[1].PerformClick(); shape.Items[0].PerformClick(); foreach (int i in new[] { 0, 1, 2, 3 }) glyphs.Items[i].PerformClick();
        if (ribbon.Theme.CaptionButtonAppearance is not { StateInset: 4, CornerRadius: 8, GlyphSize: 18, GlyphStrokeWidth: 2, RestoreOffset: 6 }) throw new InvalidOperationException("Caption-button presets bypassed public settings.");
        var sources = ribbon.Tabs.ToArray(); var mode = ribbon.LayoutMode;
        using (var preview = CreateCaptionButtonPreview()) if (preview.Ribbon is not { } model || !Equals(model.Theme, ribbon.Theme) || ReferenceEquals(model, ribbon)) throw new InvalidOperationException("Caption preview did not use an isolated public ribbon.");
        states.Items[2].PerformClick(); if (ribbon.Theme.CaptionButtonAppearance.HoverBackground?.Start.A != 80) throw new InvalidOperationException("Caption alpha preset failed.");
        states.Items[3].PerformClick(); glyphs.Items[4].PerformClick(); if (ribbon.Theme.CaptionButtonAppearance is not { GlyphSize: 0 } || ribbon.Theme.CaptionButtonAppearance.HoverBackground?.Start.A != 0) throw new InvalidOperationException("Caption hide presets failed.");
        states.Items[4].PerformClick(); if (ribbon.Theme.CaptionButtonAppearance.GlyphSize != 0 || ribbon.Theme.CaptionButtonAppearance.CornerRadius != 8) throw new InvalidOperationException("State Reset cleared shape/glyphs.");
        shape.Items[2].PerformClick(); glyphs.Items[5].PerformClick(); glyphs.Items[6].PerformClick(); if (ribbon.Theme.CaptionButtonAppearance is not null || !sources.SequenceEqual(ribbon.Tabs) || ribbon.LayoutMode != mode) throw new InvalidOperationException("Caption Reset/preview changed live models.");
        EnableCaptionButtonDemo(); if (tab.Groups.Count != 4) throw new InvalidOperationException("Caption review duplicated groups.");
        using var image = new Bitmap(ribbon.Width, ribbon.Height); ribbon.DrawToBitmap(image, ribbon.ClientRectangle); image.Save(Path.Combine(output, "review-tab.png")); Close();
    }
}
