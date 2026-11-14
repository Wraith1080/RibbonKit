namespace RibbonKit.WinForms;

public static partial class RibbonThemes
{
    /// <summary>Gets the normal Office 2010 Blue ribbon/File shell with dimensional gold states and Rounded native corner preference. The below-tabs Backstage presentation is a separate slice.</summary>
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
        return new()
        {
            WindowCornerPreference = RibbonWindowCornerPreference.Rounded,
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
            HeaderAppearance = new() { ConnectSelectedTabToBody = true, FilledTabBottomBorderVisible = false, FilledHoverBorderColor = C(0xD39E28), FilledPressedBorderColor = C(0xCB9A34) },
            Hover = Gold(0xFFF7CE, 0xFFDC8C, 0xFFEFB6, 0xFFFFFB),
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
            GalleryHeadingBackground = new(C(0xDBE7F3)), GalleryHeadingForeground = C(0x1E1E1E),
            MenuHeadingBackground = new(C(0xDBE7F3)),
            GalleryAppearance = new() { StateChrome = goldChrome, TileCornerRadius = 3, StripCornerRadius = 3, UtilityCornerRadius = 2 },
            EditorAppearance = new() { StateChrome = goldChrome, CornerRadius = 3, ChoiceCornerRadius = 3 }
        };
    }
}
