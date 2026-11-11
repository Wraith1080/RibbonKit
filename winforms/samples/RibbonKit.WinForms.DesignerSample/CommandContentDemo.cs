using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

public partial class DesignerForm
{
    private void EnableCommandContentDemo()
    {
        var tab = ThemeEffects3ReviewTab;
        if (tab.Groups.Any(group => group.Id == "review-content-effects")) return;
        var preview = new RibbonGroup { Id = "review-content-preview", Text = "Try indicators / images" };
        preview.Items.AddRange(new RibbonCheckBoxItem { Text = "Checked mark", Checked = true },
            new RibbonCheckBoxItem { Text = "Mixed mark", ThreeState = true, CheckState = CheckState.Indeterminate },
            new RibbonCheckBoxItem { Text = "Disabled mark", Checked = true, Enabled = false },
            new RibbonRadioButtonItem { Text = "Selected dot", Checked = true, GroupName = "content-review" },
            new RibbonRadioButtonItem { Text = "Other choice", GroupName = "content-review" });
        var raster = new Bitmap(16, 16);
        using (var g = Graphics.FromImage(raster)) { g.Clear(Color.OrangeRed); g.FillRectangle(Brushes.RoyalBlue, 0, 0, 8, 8); }
        Disposed += (_, _) => raster.Dispose();
        preview.Items.Add(new RibbonButtonItem { Text = "Disabled image", Image = raster, Enabled = false });
        var drop = new RibbonDropDownItem { Text = "Dropdown glyph" }; drop.Items.Add(new RibbonButtonItem { Text = "Same command menu" }); preview.Items.Add(drop);
        foreach (var item in preview.Items) item.SimplifiedPlacement = RibbonSimplifiedPlacement.Inline;
        var effects = new RibbonGroup { Id = "review-content-effects", Text = "Command content effects" };
        Add("Indicator outlines", () => Set((ribbon.Theme.CommandContent ?? new()) with { IndicatorBorderWidth = 2, IndicatorBorderColor = Color.DarkBlue }));
        Add("Colored / smaller marks", () => Set((ribbon.Theme.CommandContent ?? new()) with { CheckMarkColor = Color.Yellow, CheckMarkStrokeWidth = 2.5, CheckMarkScale = .7 }));
        Add("Larger radio dots", () => Set((ribbon.Theme.CommandContent ?? new()) with { RadioDotInset = 2, RadioDotColor = Color.DarkOrange }));
        Add("Larger dropdown glyphs", () => Set((ribbon.Theme.CommandContent ?? new()) with { DropDownGlyphWidth = 14, DropDownGlyphHeight = 9 }));
        Add("Fainter disabled images", () => Set((ribbon.Theme.CommandContent ?? new()) with { DisabledImageOpacity = .2 }));
        Add("Colored disabled images", () => Set((ribbon.Theme.CommandContent ?? new()) with { DisabledImageGrayscaleAmount = 0 }));
        Add("Default content effects", () => ribbon.Theme = ribbon.Theme with { CommandContent = null });
        tab.Groups.AddRange(preview, effects);
        void Set(RibbonCommandContentSettings value) => ribbon.Theme = ribbon.Theme with { CommandContent = value };
        void Add(string text, Action action)
        {
            var item = new RibbonButtonItem { Text = text, SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium), SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
            item.Click += (_, _) => action(); effects.Items.Add(item);
        }
    }

    internal void VerifyCommandContentDemo(string output)
    {
        StartPosition = FormStartPosition.Manual; Location = new(-16000, -16000); ShowInTaskbar = false;
        EnableOffice2019ThemeDemo(); Show(); Application.DoEvents();
        var review = ThemeEffects3ReviewTab; int count = review.Groups.Count;
        if (ribbon.Tabs.Single(tab => tab.Text == "Office 2019 Light").Groups.Count != 7 || ThemeEffects2ReviewTab.Groups.Count != 5 || review.Text != "Theme effects 3") throw new InvalidOperationException("Content review controls changed a full tab.");
        ribbon.SelectedTab = review;
        var effects = review.Groups.Single(group => group.Id == "review-content-effects");
        foreach (var item in effects.Items.Take(effects.Items.Count - 1)) item.PerformClick();
        var value = ribbon.Theme.CommandContent;
        if (value?.IndicatorBorderWidth != 2 || value.CheckMarkScale != .7 || value.RadioDotInset != 2 || value.DropDownGlyphWidth != 14 || value.DisabledImageOpacity != .2 || value.DisabledImageGrayscaleAmount != 0) throw new InvalidOperationException("Content buttons did not apply public settings.");
        effects.Items.Last().PerformClick(); if (ribbon.Theme.CommandContent is not null) throw new InvalidOperationException("Content settings did not reset.");
        EnableCommandContentDemo(); if (count != review.Groups.Count) throw new InvalidOperationException("Content groups duplicated.");
        using (var image = new Bitmap(ribbon.Width, ribbon.Height)) { ribbon.DrawToBitmap(image, ribbon.ClientRectangle); image.Save(Path.Combine(output, "review-tab.png")); }
        Close();
    }
}
