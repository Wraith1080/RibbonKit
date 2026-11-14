namespace RibbonKit.WinForms;

public static partial class RibbonThemes
{
    /// <summary>Gets normal Office 2010 Blue with dimensional gold states, below-tabs Backstage and Rounded native corner preference.</summary>
    public static RibbonTheme Office2010Blue { get; } = CreateOffice2010Blue();
    private static RibbonTheme CreateOffice2010Blue()
    {
        static Color C(int rgb) => RibbonIdentityAccentSettings.Rgb(rgb);
        static RibbonFill Gold(int start, int middle, int lower, int end) => new(C(start), C(end))
        { IntermediateStops = new(new[] { new RibbonGradientStop(.5, C(middle)), new RibbonGradientStop(.9, C(lower)) }) };
        var goldChrome = new RibbonStateChromeSettings
        {
            HoverBorderColor = C(0xE9C25E), PressedBorderColor = C(0xCB9A34), CheckedBorderColor = C(0xE0B24E), CheckedHoverBorderColor = C(0xD39E28),
            InnerBorderColor = Color.FromArgb(136, Color.White), InnerBorderWidth = 1
        };
        var goldHover = Gold(0xFFF7CE, 0xFFDC8C, 0xFFEFB6, 0xFFFFFB);
        return new()
        {
            WindowCornerPreference = RibbonWindowCornerPreference.Rounded,
            BackstageStyle = RibbonBackstageStyle.Office2010, BackstageFullWidthNavigation = true,
            BackstageNavigationBackgroundFill = new(C(0xE4EDF7), C(0xCBDAEC)), BackstageSelectedForeground = Color.White,
            BackstageNavigationHover = new(C(0xE8F0F9)), BackstageSelectionIndicatorVisible = false,
            BackstageAppearance = new() { NavigationHoverEdgeColor = C(0xF6FAFE), NavigationHoverEdgeWidth = 1 },
            BackstagePageBorderWidth = 0, BackstagePageCornerRadius = 0,
            Accent = C(0x1E5A9C), IdentityAccent = new(), AccentStates = null,
            Background = C(0xCBDAEC), BackgroundFill = new(C(0xD8E5F3), C(0xCBDAEC)),
            TitleBarBackgroundFill = new(C(0xEAF1F9), C(0xD8E5F3)), ExtendTitleBarColorToHeader = false,
            Surface = Color.White, Foreground = C(0x1E1E1E), SecondaryText = C(0x5C6B7F),
            Border = C(0x8DA5C4), PopupBorder = C(0x8DA5C4), EditorBorder = C(0x8DA5C4),
            BodyBackground = new(C(0xF6F9FC), C(0xDAE5F1)), BodyBorder = C(0x8DA5C4),
            BodyCornerRadius = 0, BodyMargin = Padding.Empty, BodyShadowColor = Color.Transparent,
            BodyAppearance = new() { BorderWidth = 1, GroupSeparatorColor = C(0xB4C9E1), QuickAccessSeparatorColor = C(0x8DA5C4) },
            QuickAccessBackgroundFill = new(C(0xDAE5F1)),
            HeaderPresentation = RibbonHeaderPresentation.Filled, HeaderFilledCornerRadius = 3, FileButtonMinimumWidth = 64,
            HeaderForeground = C(0x15428B), HeaderSelectedForeground = C(0x15428B),
            HeaderSelectedBackground = new(C(0xFCFDFF), C(0xF6F9FC)),
            HeaderHover = new(Color.FromArgb(168, C(0xFFF7DF)), Color.FromArgb(168, C(0xFBE9B0))),
            HeaderPressed = RibbonIdentityAccentSettings.Ramp(0xF3C258, 0xF8CC6A, 0xFCDA86),
            HeaderAppearance = new() { ConnectSelectedTabToBody = true, FilledTabBottomBorderVisible = false, FilledHoverBorderColor = C(0xD39E28), FilledPressedBorderColor = C(0xCB9A34),
                UtilityHoverBackground = Gold(0xFFF7CE, 0xFFDC8C, 0xFFEFB6, 0xFFFFFB), UtilityPressedBackground = RibbonIdentityAccentSettings.Ramp(0xF3C258, 0xF8CC6A, 0xFCDA86), UtilityStateChrome = goldChrome },
            Hover = goldHover,
            Pressed = RibbonIdentityAccentSettings.Ramp(0xF3C258, 0xF8CC6A, 0xFCDA86),
            Checked = Gold(0xFFEFB2, 0xFFD880, 0xFFE8A4, 0xFFF7D8), CheckedHover = Gold(0xFFEAA2, 0xFFD074, 0xFFE196, 0xFFF2C6),
            CommandAppearance = new() { StateChrome = goldChrome, SplitCompanionBackground = new(Color.Transparent), SplitSeparatorColor = C(0xE9C25E) },
            FileButtonAppearance = new()
            {
                SquareBottomCorners = true, DisabledForeground = C(0xBECBD9),
                StateChrome = new() { InnerBorderColor = Color.FromArgb(70, Color.White), InnerBorderWidth = 1 }
            },
            CornerRadius = 3, PopupCornerRadius = 3, FocusCornerRadius = 2, CheckBoxCornerRadius = 2,
            OptionsButtonCornerRadius = 3, KeyTipCornerRadius = 2,
            PopupBackgroundFill = new(C(0xF6F9FC), C(0xDAE5F1)),
            ApplicationMenuFooterCornerRadius = 3,
            ApplicationMenuAppearance = new()
            {
                SurroundBackground = new(C(0xEDF3FA), C(0xD9E5F2)), FooterSurfaceBackground = new(C(0xE4EDF7), C(0xCBDAEC)),
                SurroundRimColor = Color.White, SurroundRimWidth = 1, SurroundRimInset = 1,
                NavigationBackground = new(C(0xFAFAFA)), DefaultPaneBackground = new(C(0xECEFF4)), ActivePaneBackground = new(Color.White),
                InnerFrameCornerRadius = 0, NavigationDividerColor = C(0xC3CFDF),
                DefaultHeaderBackground = new(Color.Transparent), ActiveHeaderBackground = new(C(0xDCE6F2)), HeaderRuleColor = C(0xC3CFDF),
                RowCornerRadius = 3, RowBorderWidth = 1, RowStateChrome = goldChrome,
                OpenBackground = goldHover, SelectedPrimaryBackground = RibbonFillColors.Map(goldHover, c => Color.FromArgb(89, c)),
                SplitDividerWidth = 2, SplitDividerColor = C(0xE9C25E), SplitDividerHighlightColor = Color.FromArgb(136, Color.White), SplitDividerHighlightWidth = 1,
                FooterBackground = new(C(0xF6FAFE), C(0xDCE9F8)), FooterStateChrome = goldChrome
            },
            GalleryHeadingBackground = new(C(0xDBE7F3)), GalleryHeadingForeground = C(0x1E1E1E),
            MenuHeadingBackground = new(C(0xDBE7F3)), MenuHeadingForeground = C(0x174F90),
            GalleryAppearance = new() { StateChrome = goldChrome, UtilityStateChrome = goldChrome, TileCornerRadius = 3, StripCornerRadius = 3, UtilityCornerRadius = 2 },
            CaptionButtonAppearance = new()
            {
                StateInset = 2, CornerRadius = 3,
                HoverBackground = Gold(0xEAF7FF, 0x92C5E8, 0xB3DCF5, 0xE8F8FF),
                PressedBackground = RibbonIdentityAccentSettings.Ramp(0x6599C3, 0xA4D1EC, 0x82BBDD),
                CloseHoverBackground = Gold(0xFFB5AE, 0xD94438, 0xED6654, 0xFFB6A8),
                ClosePressedBackground = RibbonIdentityAccentSettings.Ramp(0xA62B22, 0xD85545, 0xB8372C),
                StateChrome = new() { HoverBorderColor = C(0x6B98B9), PressedBorderColor = C(0x3C6C91), InnerBorderColor = Color.FromArgb(180, Color.White), InnerBorderWidth = 1 },
                CloseStateChrome = new() { HoverBorderColor = C(0xA43128), PressedBorderColor = C(0x7D231C), InnerBorderColor = Color.FromArgb(160, Color.White), InnerBorderWidth = 1 }
            },
            EditorAppearance = new() { StateChrome = goldChrome, CornerRadius = 3, ChoiceCornerRadius = 3 }
        };
    }
}
