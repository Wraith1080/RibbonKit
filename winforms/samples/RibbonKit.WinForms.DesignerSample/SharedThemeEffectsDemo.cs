using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

public partial class DesignerForm
{
    // Keep accepted controls in place; D7b uses the requested new tab 8.
    private RibbonTab? _themeEffects2ReviewTab;
    private RibbonTab? _themeEffects3ReviewTab;
    private RibbonTab? _themeEffects4ReviewTab;
    private RibbonTab? _themeEffects5ReviewTab;
    private RibbonTab? _themeEffects6ReviewTab;
    private RibbonTab? _themeEffects7ReviewTab;
    private RibbonTab? _themeEffectsReviewTab;
    private RibbonTab ThemeEffects2ReviewTab => _themeEffects2ReviewTab ??= AddReviewTab("review-theme-effects-2", "Theme effects 2");
    private RibbonTab ThemeEffects3ReviewTab => _themeEffects3ReviewTab ??= AddReviewTab("review-theme-effects-3", "Theme effects 3");
    private RibbonTab ThemeEffects4ReviewTab => _themeEffects4ReviewTab ??= AddReviewTab("review-theme-effects-4", "Theme effects 4");
    private RibbonTab ThemeEffects5ReviewTab => _themeEffects5ReviewTab ??= AddReviewTab("review-theme-effects-5", "Theme effects 5");
    private RibbonTab ThemeEffects6ReviewTab => _themeEffects6ReviewTab ??= AddReviewTab("review-theme-effects-6", "Theme effects 6");
    private RibbonTab ThemeEffects7ReviewTab => _themeEffects7ReviewTab ??= AddReviewTab("review-theme-effects-7", "Theme effects 7");
    private RibbonTab ThemeEffectsReviewTab => _themeEffectsReviewTab ??= AddReviewTab("review-theme-effects-8", "Theme effects 8");
    private RibbonTab AddReviewTab(string id, string text)
    {
        var tab = new RibbonTab { Id = id, Text = text };
        ribbon.Tabs.Add(tab); return tab;
    }
    private void EnableSharedThemeEffectsDemo()
    {
        var tab = ThemeEffects2ReviewTab;
        if (tab.Groups.Any(group => group.Id == "review-header-details")) return;
        var header = new RibbonGroup { Id = "review-header-details", Text = "Header details" };
        Add(header, "Filled tab borders", () => { ribbon.Theme = ribbon.Theme with { HeaderPresentation = RibbonHeaderPresentation.Filled }; SetHeader((ribbon.Theme.HeaderAppearance ?? new()) with { FilledBorderColor = Color.DarkOrange, FilledBorderWidth = 2 }); });
        Add(header, "Rounded scrollers", () => { ribbon.Theme = ribbon.Theme with { HeaderScrollButtonBackground = new(Color.LightBlue), HeaderScrollButtonBorder = Color.DarkBlue }; SetHeader((ribbon.Theme.HeaderAppearance ?? new()) with { ScrollerCornerRadius = 8, ScrollerBorderWidth = 2 }); });
        Add(header, "Stronger contextual ink", () => SetHeader((ribbon.Theme.HeaderAppearance ?? new()) with { ContextualUnselectedOpacity = 1 }));
        Add(header, "Default header details", () => ribbon.Theme = ribbon.Theme with { HeaderAppearance = null, HeaderScrollButtonBackground = null, HeaderScrollButtonBorder = Color.Empty });
        var shared = new RibbonGroup { Text = "Chevrons and focus" };
        Add(shared, "Larger chevrons", () => ribbon.Theme = ribbon.Theme with { Chevrons = new() { Length = 16, Depth = 9, StrokeWidth = 2 } });
        Add(shared, "Thicker focus outline", () => ribbon.Theme = ribbon.Theme with { FocusInset = 4, FocusStrokeWidth = 2 });
        Add(shared, "Default chevrons / focus", () => ribbon.Theme = ribbon.Theme with { Chevrons = null, FocusInset = null, FocusStrokeWidth = null });
        var motion = new RibbonGroup { Text = "Shared tab motion" };
        Add(motion, "Longer slide", () => { ribbon.AnimationLevel = RibbonAnimationLevel.Subtle; ribbon.TabContentAnimation = RibbonTabContentAnimation.SlideDown; ribbon.Theme = ribbon.Theme with { TabContentSlideDistance = 24 }; });
        Add(motion, "More indicator spring", () => { ribbon.AnimationLevel = RibbonAnimationLevel.Expressive; ribbon.Theme = ribbon.Theme with { HeaderPresentation = RibbonHeaderPresentation.Underline, HeaderIndicatorSpringAmount = .7 }; });
        Add(motion, "No indicator spring", () => ribbon.Theme = ribbon.Theme with { HeaderIndicatorSpringAmount = 0 });
        Add(motion, "Default tab motion", () => ribbon.Theme = ribbon.Theme with { TabContentSlideDistance = null, HeaderIndicatorSpringAmount = null });
        var file = new RibbonGroup { Text = "Open File highlight" };
        Add(file, "CommandMenu / Backstage", () => ribbon.FileMenuMode = ribbon.FileMenuMode == RibbonFileMenuMode.CommandMenu ? RibbonFileMenuMode.Backstage : RibbonFileMenuMode.CommandMenu);
        Add(file, "Blue File highlight", () => SetHeader((ribbon.Theme.HeaderAppearance ?? new()) with { FileHighlightBackground = new(Color.Navy), FileHighlightForeground = Color.White }));
        Add(file, "Larger File inset", () => SetHeader((ribbon.Theme.HeaderAppearance ?? new()) with { FileHighlightInset = 7 }));
        Add(file, "Default File highlight", () => SetHeader((ribbon.Theme.HeaderAppearance ?? new()) with { FileHighlightBackground = null, FileHighlightForeground = Color.Empty, FileHighlightInset = null }));
        tab.Groups.AddRange(header, shared, motion, file);
        void SetHeader(RibbonHeaderAppearanceSettings value) => ribbon.Theme = ribbon.Theme with { HeaderAppearance = value };
        void Add(RibbonGroup group, string text, Action action)
        {
            var button = new RibbonButtonItem { Text = text, SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium), SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
            button.Click += (_, _) => action(); group.Items.Add(button);
        }
    }
    internal void VerifySharedThemeEffectsDemo()
    {
        StartPosition = FormStartPosition.Manual; Location = new(-16000, -16000); ShowInTaskbar = false;
        EnableOffice2019ThemeDemo(); Show(); Application.DoEvents();
        var original = ribbon.Tabs.Single(tab => tab.Text == "Office 2019 Light");
        var review = ribbon.Tabs.Single(tab => tab.Text == "Theme effects 2");
        if (original.Groups.Count != 7 || review.Groups.Count < 4) throw new InvalidOperationException("Review groups were misplaced.");
        ribbon.SelectedTab = review;
        void Click(string text) => review.Groups.SelectMany(group => group.Items).Single(item => item.Text == text).PerformClick();
        Click("Filled tab borders"); Click("Larger chevrons"); Click("Thicker focus outline"); Click("Longer slide"); Click("More indicator spring"); Click("Blue File highlight");
        if (ribbon.Theme.HeaderAppearance?.FilledBorderWidth != 2 || ribbon.Theme.Chevrons?.Length != 16 || ribbon.Theme.FocusStrokeWidth != 2 || ribbon.Theme.TabContentSlideDistance != 24 || ribbon.Theme.HeaderIndicatorSpringAmount != .7 || ribbon.Theme.HeaderAppearance.FileHighlightForeground != Color.White)
            throw new InvalidOperationException("Review buttons did not use public settings.");
        Click("Default File highlight"); Click("Default header details"); Click("Default chevrons / focus"); Click("Default tab motion");
        if (ribbon.Theme.HeaderAppearance is not null || ribbon.Theme.Chevrons is not null || ribbon.Theme.FocusInset is not null || ribbon.Theme.TabContentSlideDistance is not null) throw new InvalidOperationException("Review reset did not clear the effects.");
        int groupCount = review.Groups.Count; EnableSharedThemeEffectsDemo(); if (review.Groups.Count != groupCount) throw new InvalidOperationException("Review tab duplicated groups.");
        Close();
    }
}
