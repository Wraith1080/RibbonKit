using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

public partial class DesignerForm
{
    private void EnableBodyPresentationDemo()
    {
        var tab = ThemeEffectsReviewTab;
        if (tab.Groups.Any(group => group.Id == "review-body-frames")) return;
        var frames = new RibbonGroup { Id = "review-body-frames", Text = "Body and connected frames" };
        Add("Preview connected panels", () =>
        {
            ribbon.QuickAccessVisible = true; ribbon.QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon;
            ribbon.IsMinimized = false;
            if (!ribbon.MessageBar.Items.Any(message => message.Id == "review-body-message"))
                ribbon.MessageBar.Items.Add(new RibbonMessage { Id = "review-body-message", Title = "Frame preview", Text = "Compare body, Quick Access and message borders." });
        });
        Add("Thicker frame borders", () => Set((ribbon.Theme.BodyAppearance ?? new()) with { BorderWidth = 3, MessageBorderWidth = 3 }));
        Add("Colored group dividers", () => Set((ribbon.Theme.BodyAppearance ?? new()) with { GroupSeparatorWidth = 3, GroupSeparatorColor = Color.Teal }));
        Add("Colored Quick Access separator", () => Set((ribbon.Theme.BodyAppearance ?? new()) with { QuickAccessSeparatorWidth = 3, QuickAccessSeparatorInset = 12, QuickAccessSeparatorColor = Color.DarkOrange }));
        Add("Join / separate panels", () =>
        {
            bool join = ribbon.Theme.BodyAppearance?.JoinBelowRibbonQuickAccess == false;
            Set((ribbon.Theme.BodyAppearance ?? new()) with { JoinBelowRibbonQuickAccess = join, JoinMessageBarToBody = join });
        });
        Add("Default body frame effects", () => ribbon.Theme = ribbon.Theme with { BodyAppearance = null });
        tab.Groups.Add(frames);
        void Set(RibbonBodyAppearanceSettings value) => ribbon.Theme = ribbon.Theme with { BodyAppearance = value };
        void Add(string text, Action action)
        {
            var button = new RibbonButtonItem { Text = text, SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium), SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
            button.Click += (_, _) => action(); frames.Items.Add(button);
        }
    }
    internal void VerifyBodyPresentationDemo()
    {
        StartPosition = FormStartPosition.Manual; Location = new(-16000, -16000); ShowInTaskbar = false;
        EnableOffice2019ThemeDemo(); Show(); Application.DoEvents();
        var original = ribbon.Tabs.Single(tab => tab.Text == "Office 2019 Light"); var review = ThemeEffectsReviewTab;
        if (original.Groups.Count != 7 || review.Groups.Count != 5) throw new InvalidOperationException("Body controls were not added to the existing review tab.");
        ribbon.SelectedTab = review;
        void Click(string text) => review.Groups.SelectMany(group => group.Items).Single(item => item.Text == text).PerformClick();
        Click("Preview connected panels"); Click("Thicker frame borders"); Click("Colored group dividers"); Click("Colored Quick Access separator"); Click("Join / separate panels");
        if (ribbon.Theme.BodyAppearance?.BorderWidth != 3 || ribbon.Theme.BodyAppearance.GroupSeparatorWidth != 3 || ribbon.Theme.BodyAppearance.QuickAccessSeparatorInset != 12 || ribbon.Theme.BodyAppearance.JoinBelowRibbonQuickAccess != false)
            throw new InvalidOperationException("Body review controls did not use public settings.");
        Click("Default body frame effects"); if (ribbon.Theme.BodyAppearance is not null) throw new InvalidOperationException("Body settings did not reset.");
        EnableBodyPresentationDemo(); if (review.Groups.Count != 5) throw new InvalidOperationException("Body review group was duplicated.");
        Close();
    }
}
