using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

public partial class DesignerForm
{
    internal void EnableOffice2019ThemeDemo()
    {
        // The authored sample uses a white caption override. Let this demo use
        // the theme's neutral caption until the user explicitly colors it.
        TitleBarColor = Color.Empty;
        RibbonFileMenuMode? previousFileMode = null;
        void RestoreFileMode()
        {
            if (ribbon.IsBackstageOpen || previousFileMode is not { } previous) return;
            previousFileMode = null;
            if (ribbon.FileMenuMode == RibbonFileMenuMode.Backstage) ribbon.FileMenuMode = previous;
        }
        ribbon.IsBackstageOpenChanged += (_, _) => RestoreFileMode();
        var tab = new RibbonTab { Text = "Office 2019 Light" };
        var themes = new RibbonGroup { Text = "Light themes" };
        Add(themes, "Office 2019 Light", () => ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2019Light);
        Add(themes, "Office 2024 Light", () => ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2024Light);
        Add(themes, "Application theme", () => ribbon.BuiltInTheme = null);
        Add(themes, "Custom 2019 accent", () => ribbon.Theme = RibbonThemes.Office2019Light with { Accent = Color.Purple });
        var caption = new RibbonGroup { Text = "Header and layout" };
        Add(caption, "Blue title bar", () => TitleBarColor = Color.FromArgb(43, 87, 154));
        Add(caption, "Neutral title bar", () => TitleBarColor = Color.Empty);
        Add(caption, "Mouse / Touch", () => ribbon.Density = ribbon.Density == RibbonDensity.Compact ? RibbonDensity.Touch : RibbonDensity.Compact);
        Add(caption, "Classic / Simplified", () => ribbon.LayoutMode = ribbon.LayoutMode == RibbonLayoutMode.Classic ? RibbonLayoutMode.Simplified : RibbonLayoutMode.Classic);
        var surfaces = new RibbonGroup { Text = "Square surfaces" };
        var menu = new RibbonDropDownItem { Text = "Try popup", Icon = RibbonStockIcons.Create("open"), DropDownHeader = "Bordered and shadowed popup", ScreenTipTitle = "Square popup", ScreenTipDescription = "The popup keeps its border and shadow while the body stays flat.", SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
        var nested = new RibbonDropDownItem { Text = "Nested menu" }; nested.Items.Add(new RibbonButtonItem { Text = "Nested action" });
        menu.Items.AddRange(new RibbonCheckBoxItem { Text = "Keep formatting", Checked = true },
            new RibbonTextBoxItem { Text = "Name", Value = "Retained popup field", FillMenuWidth = true }, nested);
        surfaces.Items.Add(menu);
        Add(surfaces, "Toggle Backstage", () =>
        {
            if (ribbon.IsBackstageOpen) { ribbon.IsBackstageOpen = false; return; }
            previousFileMode = ribbon.FileMenuMode;
            ribbon.FileMenuMode = RibbonFileMenuMode.Backstage;
            try { ribbon.IsBackstageOpen = true; }
            finally { RestoreFileMode(); } // Also restore after a canceled opening.
        });
        Add(surfaces, "Show message", () => ribbon.MessageBar.Items.Add(new RibbonMessage { Title = "Office 2019", Text = "Square joins and a flat ribbon body." }));
        var settings = new RibbonGroup { Text = "Shared theme settings" };
        Add(settings, "Larger inner padding", () => ribbon.Theme = ribbon.Theme with { BodyContentHorizontalPadding = 12 });
        Add(settings, "Default inner padding", () => ribbon.Theme = ribbon.Theme with { BodyContentHorizontalPadding = null });
        Add(settings, "Wider popup shadow", () => ribbon.Theme = ribbon.Theme with { PopupShadowBlur = 12, PopupShadowVerticalOffset = 4 });
        Add(settings, "Default popup shadow", () => ribbon.Theme = ribbon.Theme with { PopupShadowBlur = null, PopupShadowVerticalOffset = null });
        var headers = new RibbonGroup { Text = "Header effects" };
        Add(headers, "Filled / Underlined tabs", () => ribbon.Theme = ribbon.Theme with { HeaderPresentation = ribbon.Theme.HeaderPresentation == RibbonHeaderPresentation.Filled ? RibbonHeaderPresentation.Underline : RibbonHeaderPresentation.Filled });
        Add(headers, "Thicker underline", () => ribbon.Theme = ribbon.Theme with { HeaderIndicatorThickness = 6, HeaderIndicatorBottomInset = 4 });
        Add(headers, "Rounded filled tabs", () => ribbon.Theme = ribbon.Theme with { HeaderFilledCornerRadius = 8 });
        Add(headers, "Selected tab colors", () => ribbon.Theme = ribbon.Theme with { HeaderSelectedBackground = new(Color.LightBlue), HeaderSelectedForeground = Color.DarkBlue, HeaderIndicatorHoverColor = Color.Teal });
        Add(headers, "Default header effects", () => ribbon.Theme = ribbon.Theme with { HeaderSelectedBackground = null, HeaderSelectedForeground = Color.Empty,
            HeaderFilledCornerRadius = null, HeaderIndicatorThickness = null, HeaderIndicatorBottomInset = null, HeaderIndicatorHoverColor = Color.Empty });
        var backstageMotion = new RibbonGroup { Text = "Backstage motion" };
        Add(backstageMotion, "Fade / Slide only", () => SetMotion((ribbon.Theme.BackstageMotion ?? new()) with { PaneFadeAmount = ribbon.Theme.BackstageMotion?.PaneFadeAmount == 0 ? 1 : 0 }));
        Add(backstageMotion, "Slower Backstage", () => SetMotion((ribbon.Theme.BackstageMotion ?? new()) with { PaneDuration = 500, PageDuration = 240 }));
        Add(backstageMotion, "Short page slide", () => { ribbon.TabContentAnimation = RibbonTabContentAnimation.SlideDown; SetMotion((ribbon.Theme.BackstageMotion ?? new()) with { PageSlideDistance = 4 }); });
        Add(backstageMotion, "Steady motion", () => SetMotion((ribbon.Theme.BackstageMotion ?? new()) with { PaneEasing = RibbonBackstageEasing.Linear, TitleEasing = RibbonBackstageEasing.Linear, PageEasing = RibbonBackstageEasing.Linear }));
        Add(backstageMotion, "Default Backstage motion", () => ribbon.Theme = ribbon.Theme with { BackstageMotion = null });
        void SetMotion(RibbonBackstageMotionSettings value)
        {
            if (ribbon.AnimationLevel == RibbonAnimationLevel.None) ribbon.AnimationLevel = RibbonAnimationLevel.Subtle;
            ribbon.Theme = ribbon.Theme with { BackstageMotion = value };
        }
        var backstageAppearance = new RibbonGroup { Text = "Backstage appearance" };
        Add(backstageAppearance, "Show / hide marker", () => ribbon.Theme = ribbon.Theme with { BackstageSelectionIndicatorVisible = !ribbon.Theme.BackstageSelectionIndicatorVisible });
        Add(backstageAppearance, "Larger marker", () => ribbon.Theme = ribbon.Theme with {
            BackstageAppearance = (ribbon.Theme.BackstageAppearance ?? new()) with { MarkerWidth = 6, MarkerLeftInset = 8, MarkerTopInset = 6, MarkerBottomInset = 6, MarkerColor = Color.Gold } });
        Add(backstageAppearance, "Larger Back arrow", () => ribbon.Theme = ribbon.Theme with { BackstageAppearance = (ribbon.Theme.BackstageAppearance ?? new()) with { BackGlyphScale = 1.6, BackGlyphStrokeWidth = 2.5, BackVisualInset = 5 } });
        Add(backstageAppearance, "Outlined navigation", () => ribbon.Theme = ribbon.Theme with { BackstageAppearance = (ribbon.Theme.BackstageAppearance ?? new()) with { NavigationCornerRadius = 8, NavigationBorderWidth = 2, NavigationBorderColor = Color.DarkOrange } });
        Add(backstageAppearance, "Roomier navigation", () => ribbon.Theme = ribbon.Theme with { BackstageRowTextVerticalPadding = 34, BackstageSeparatorHeight = 16, BackstageTrailingPadding = 20 });
        Add(backstageAppearance, "Default Backstage appearance", () => ribbon.Theme = ribbon.Theme with { BackstageAppearance = null,
            BackstageRowTextVerticalPadding = null, BackstageSeparatorHeight = null, BackstageTrailingPadding = null });
        tab.Groups.AddRange(themes, caption, surfaces, settings, headers, backstageMotion, backstageAppearance); ribbon.Tabs.Add(tab); ribbon.SelectedTab = tab;
        ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2019Light;
        EnableSharedThemeEffectsDemo();
        EnableBodyPresentationDemo();
        statusLabel.Text = "Office 2019 Light: compare neutral/blue headers, flat body, inner padding and square shadowed popups.";

        void Add(RibbonGroup group, string text, Action action)
        {
            var button = new RibbonButtonItem { Text = text, SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium), SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
            button.Click += (_, _) => action(); group.Items.Add(button);
        }
    }

    internal void VerifyOffice2019ReviewDemo(string output)
    {
        Directory.CreateDirectory(output); EnableOffice2019ThemeDemo();
        ribbon.AnimationLevel = RibbonAnimationLevel.None;
        void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        var toggle = ribbon.SelectedTab!.Groups.SelectMany(g => g.Items).Single(i => i.Text == "Toggle Backstage");
        var originalMode = ribbon.FileMenuMode;
        Show(); Application.DoEvents();
        Require(TitleBarColor.IsEmpty && ribbon.Theme.Background == RibbonThemes.Office2019Light.Background, "Demo did not start with the neutral theme header.");
        var pages = ribbon.Backstage.Items.OfType<RibbonBackstagePageItem>().Select(p => p.Content).ToArray();
        System.ComponentModel.CancelEventHandler cancel = (_, e) => e.Cancel = true;
        ribbon.Backstage.Opening += cancel; toggle.PerformClick(); ribbon.Backstage.Opening -= cancel;
        Require(!ribbon.IsBackstageOpen && ribbon.FileMenuMode == originalMode, "Canceled opening changed File routing.");
        toggle.PerformClick(); Application.DoEvents();
        Require(ribbon.IsBackstageOpen && ribbon.FileMenuMode == RibbonFileMenuMode.Backstage, "Toggle did not open Backstage.");
        ribbon.Backstage.Closing += cancel; toggle.PerformClick(); ribbon.Backstage.Closing -= cancel;
        Require(ribbon.IsBackstageOpen && ribbon.FileMenuMode == RibbonFileMenuMode.Backstage, "Canceled close lost Backstage or routing.");
        toggle.PerformClick(); Application.DoEvents();
        Require(!ribbon.IsBackstageOpen && ribbon.FileMenuMode == originalMode, "Toggle close did not restore File routing.");
        toggle.PerformClick(); ribbon.IsBackstageOpen = false; Application.DoEvents();
        Require(ribbon.FileMenuMode == originalMode, "Back/Escape close did not restore File routing.");
        Require(pages.SequenceEqual(ribbon.Backstage.Items.OfType<RibbonBackstagePageItem>().Select(p => p.Content)), "Demo replaced native page content.");
        Office2019ThemeCheck.Capture(this, Path.Combine(output, "neutral-demo.png"));
        Close();
        File.WriteAllText(Path.Combine(output, "result.log"), "PASS: neutral demo startup; toggle open/close, Back/Escape close, canceled opening/closing, prior File mode and retained native pages.");
    }
}
