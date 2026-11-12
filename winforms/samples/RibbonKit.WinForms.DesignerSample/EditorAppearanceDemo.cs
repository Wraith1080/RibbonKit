using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

public partial class DesignerForm
{
    private void EnableEditorAppearanceDemo()
    {
        var tab = ThemeEffects4ReviewTab;
        if (tab.Groups.Any(group => group.Id == "review-editor-effects")) return;
        var fields = new RibbonGroup { Id = "review-editor-fields", Text = "Try text fields", CanResize = false };
        fields.Items.AddRange(new RibbonTextBoxItem { Text = "Name", Value = "Edit me", InputWidth = 140 },
            new RibbonTextBoxItem { Text = "Read only", Value = "Retained selection", ReadOnly = true, InputWidth = 140 },
            new RibbonTextBoxItem { Text = "Disabled", Value = "Unavailable", Enabled = false, InputWidth = 140 });
        var choices = new RibbonGroup { Id = "review-editor-choices", Text = "Try choices", CanResize = false };
        var list = new RibbonComboBoxItem { Text = "Choice", InputWidth = 140 }; list.Items.AddRange("Aptos", "Segoe UI", "Calibri"); list.SelectedIndex = 0;
        var editable = new RibbonComboBoxItem { Text = "Editable", IsEditable = true, InputWidth = 140 }; editable.Items.AddRange("One", "Two", "Three"); editable.Value = "Free text";
        var menu = new RibbonDropDownItem { Text = "Fields in a menu" };
        var menuChoice = new RibbonComboBoxItem { Text = "Menu choice", InputWidth = 140 }; menuChoice.Items.AddRange("First", "Second", "Third"); menuChoice.SelectedIndex = 0;
        menu.Items.AddRange(new RibbonTextBoxItem { Text = "Menu name", Value = "Same native editor", InputWidth = 140 }, menuChoice);
        choices.Items.AddRange(list, editable, menu);
        foreach (var item in fields.Items.Concat(choices.Items)) item.SimplifiedPlacement = RibbonSimplifiedPlacement.Inline;
        var effects = new RibbonGroup { Id = "review-editor-effects", Text = "Editor appearance effects" };
        Add("Rounded field frames", () => Set((ribbon.Theme.EditorAppearance ?? new()) with { CornerRadius = 8, BorderWidth = 2 }));
        Add("Orange active borders", () => Set((ribbon.Theme.EditorAppearance ?? new()) with { ActiveBorderColor = Color.DarkOrange }));
        Add("Inset field frames", () => Set((ribbon.Theme.EditorAppearance ?? new()) with { Inset = 2 }));
        Add("Choice row details", () => Set((ribbon.Theme.EditorAppearance ?? new()) with { ChoiceCornerRadius = 8, ChoiceInset = 3, ChoiceTextHorizontalPadding = 16 }));
        Add("Square edge-to-edge fields", () => Set((ribbon.Theme.EditorAppearance ?? new()) with { CornerRadius = 0, Inset = 0, BorderWidth = 1 }));
        Add("Default editor effects", () => ribbon.Theme = ribbon.Theme with { EditorAppearance = null });
        tab.Groups.AddRange(fields, choices, effects);
        void Set(RibbonEditorAppearanceSettings value) => ribbon.Theme = ribbon.Theme with { EditorAppearance = value };
        void Add(string text, Action action)
        {
            var item = new RibbonButtonItem { Text = text, SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium), SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
            item.Click += (_, _) => action(); effects.Items.Add(item);
        }
    }
    internal void VerifyEditorAppearanceDemo(string output)
    {
        StartPosition = FormStartPosition.Manual; Location = new(-16000, -16000); ShowInTaskbar = false;
        EnableOffice2019ThemeDemo(); Show(); Application.DoEvents(); var review = ThemeEffects4ReviewTab;
        if (ribbon.Tabs.Single(tab => tab.Text == "Office 2019 Light").Groups.Count != 7 || ThemeEffects2ReviewTab.Groups.Count != 5 || ThemeEffects3ReviewTab.Groups.Count != 4 || review.Text != "Theme effects 4") throw new InvalidOperationException("Editor review groups changed an earlier tab.");
        ribbon.SelectedTab = review;
        var effects = review.Groups.Single(group => group.Id == "review-editor-effects");
        foreach (var item in effects.Items.Take(4)) item.PerformClick();
        var value = ribbon.Theme.EditorAppearance;
        if (value?.CornerRadius != 8 || value.BorderWidth != 2 || value.ActiveBorderColor != Color.DarkOrange || value.Inset != 2 || value.ChoiceTextHorizontalPadding != 16) throw new InvalidOperationException("Editor buttons bypassed public settings.");
        effects.Items.Last().PerformClick(); if (ribbon.Theme.EditorAppearance is not null) throw new InvalidOperationException("Editor group did not reset.");
        int count = review.Groups.Count; EnableEditorAppearanceDemo(); if (review.Groups.Count != count) throw new InvalidOperationException("Editor review groups duplicated.");
        using (var image = new Bitmap(ribbon.Width, ribbon.Height)) { ribbon.DrawToBitmap(image, ribbon.ClientRectangle); image.Save(Path.Combine(output, "review-tab.png")); }
        Close();
    }
}
