using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

public partial class DesignerForm
{
    private void EnableCommandAppearanceDemo()
    {
        var tab = ThemeEffectsReviewTab;
        if (tab.Groups.Any(group => group.Id == "review-command-effects")) return;
        var commands = new RibbonGroup { Id = "review-command-preview", Text = "Try commands" };
        var button = new RibbonButtonItem { Text = "Push command", SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
        button.Click += (_, _) => statusLabel.Text = "Push command invoked.";
        var toggle = new RibbonToggleItem { Text = "Checked toggle", Checked = true, SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
        var split = new RibbonSplitButtonItem { Text = "Split command", Checked = true, SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
        split.Click += (_, _) => statusLabel.Text = "Split primary action invoked.";
        split.Items.Add(new RibbonButtonItem { Text = "Split menu action" });
        var vertical = new RibbonSplitButtonItem { Text = "Vertical split", Layout = RibbonSplitButtonLayout.Vertical, Checked = true, SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Large) };
        vertical.Items.Add(new RibbonButtonItem { Text = "Vertical menu action" }); commands.Items.AddRange(button, toggle, split, vertical);
        var effects = new RibbonGroup { Id = "review-command-effects", Text = "Command state effects" };
        Add("Rounded command outlines", () => Set((ribbon.Theme.CommandAppearance ?? new()) with { CornerRadius = 8, BorderWidth = 2, BorderColor = Color.DarkBlue, Inset = 0 }));
        Add("Inset command chrome", () => Set((ribbon.Theme.CommandAppearance ?? new()) with { Inset = 4 }));
        Add("Colored split separators", () => Set((ribbon.Theme.CommandAppearance ?? new()) with { SplitSeparatorWidth = 3, SplitSeparatorInset = 5, SplitSeparatorColor = Color.DarkOrange }));
        Add("Split companion fill", () => Set((ribbon.Theme.CommandAppearance ?? new()) with { SplitCompanionBackground = new(Color.LightSkyBlue) }));
        Add("Default command effects", () => ribbon.Theme = ribbon.Theme with { CommandAppearance = null });
        tab.Groups.AddRange(commands, effects);
        void Set(RibbonCommandAppearanceSettings value) => ribbon.Theme = ribbon.Theme with { CommandAppearance = value };
        void Add(string text, Action action)
        {
            var item = new RibbonButtonItem { Text = text, SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium), SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
            item.Click += (_, _) => action(); effects.Items.Add(item);
        }
    }
    internal void VerifyCommandAppearanceDemo()
    {
        StartPosition = FormStartPosition.Manual; Location = new(-16000, -16000); ShowInTaskbar = false;
        EnableOffice2019ThemeDemo(); Show(); Application.DoEvents();
        var original = ribbon.Tabs.Single(tab => tab.Text == "Office 2019 Light"); var previous = ThemeEffects2ReviewTab; var review = ThemeEffectsReviewTab;
        if (original.Groups.Count != 7 || previous.Groups.Count != 5 || review.Groups.Count != 2 || review.Text != "Theme effects 3") throw new InvalidOperationException("Command review controls changed a full tab.");
        ribbon.SelectedTab = review;
        void Click(string text) => review.Groups.SelectMany(group => group.Items).Single(item => item.Text == text).PerformClick();
        Click("Rounded command outlines"); Click("Inset command chrome"); Click("Colored split separators"); Click("Split companion fill");
        if (ribbon.Theme.CommandAppearance?.CornerRadius != 8 || ribbon.Theme.CommandAppearance.Inset != 4 || ribbon.Theme.CommandAppearance.SplitSeparatorWidth != 3 || ribbon.Theme.CommandAppearance.SplitCompanionBackground is null) throw new InvalidOperationException("Command controls did not use public theme settings.");
        Click("Default command effects"); if (ribbon.Theme.CommandAppearance is not null) throw new InvalidOperationException("Command group did not reset.");
        EnableCommandAppearanceDemo(); if (review.Groups.Count != 2) throw new InvalidOperationException("Command groups were duplicated.");
        Close();
    }
}
