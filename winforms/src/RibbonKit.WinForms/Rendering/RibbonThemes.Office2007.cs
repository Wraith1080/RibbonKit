namespace RibbonKit.WinForms;

public static partial class RibbonThemes
{
    /// <summary>Gets normal opaque Office 2007 Blue: valley surfaces, exact creased gold states, framed groups and an Orb. File routing, window material and explicit application-button shape remain host choices. Dedicated Application Menu, Options and Aero palettes are separate.</summary>
    public static RibbonTheme Office2007Blue { get; } = CreateOffice2007Blue();

    private static RibbonTheme CreateOffice2007Blue()
    {
        static Color C(int rgb) => RibbonIdentityAccentSettings.Rgb(rgb);
        static RibbonFill Ramp(int start, int end, params (double Offset, int Rgb)[] stops) => new(C(start), C(end))
        { IntermediateStops = RibbonGradientStops.WithHardTransitions(stops.Select(s => new RibbonGradientStop(s.Offset, C(s.Rgb)))) };
        var hover = Ramp(0xFFFFF7, 0xFFE89F, (.38, 0xFFE798), (.38, 0xFFD758), (.48, 0xFFD350));
        var pressed = Ramp(0xFEB96C, 0xFEBE63, (.40, 0xFDA461), (.40, 0xFC8F3D), (.63, 0xFC973D));
        var goldChrome = new RibbonStateChromeSettings
        {
            HoverBorderColor = C(0xC1A877), PressedBorderColor = C(0xB6A892), CheckedBorderColor = C(0xC19A4B),
            InnerBorderColor = Color.FromArgb(170, Color.White), InnerBorderWidth = 1
        };
        var body = Ramp(0xDBE6F4, 0xE3F4FF, (.17, 0xC8D9ED));
        var orbPressed = Ramp(0xF5D98A, 0xF7D06A, (.18, 0xEFBE4A), (.5, 0xD8931A), (.82, 0xE8A82A));
        return new()
        {
            Accent = C(0x1E5A9C), AccentStates = null,
            IdentityAccent = new()
            {
                FileBackground = Ramp(0x6FB4E4, 0x4C93CD, (.45, 0x3585C6), (.45, 0x1F63A6)),
                FileHoverBackground = Ramp(0x8ACBF2, 0x63A9DD, (.45, 0x4A9AD6), (.45, 0x2C77BA)),
                FilePressedBackground = Ramp(0x3D86C4, 0x2C71AC, (.45, 0x1F63A6), (.45, 0x14487E)),
                FileOpenBackground = new(C(0x1E5A9C)), FileBorderColor = C(0x17578F),
                CaptionBackground = Ramp(0x6FB4E4, 0x4C93CD, (.45, 0x3585C6), (.45, 0x1F63A6))
            },
            WindowCornerPreference = RibbonWindowCornerPreference.Rounded,
            Background = C(0xBFDBFF), TitleBarBackgroundFill = Ramp(0xE4EBF6, 0xE4EFFD, (.14, 0xD5E5FA), (.28, 0xCADEF7)),
            TitleBarForeground = C(0x15428B), ExtendTitleBarColorToHeader = false,
            Surface = Color.White, Foreground = C(0x1E1E1E), SecondaryText = C(0x15428B),
            Border = C(0x8DB2E3), PopupBorder = C(0x8DB2E3), EditorBorder = C(0x8DB2E3),
            EditorBackground = Color.White, EditorForeground = C(0x1E1E1E),
            BodyBackground = body, BodyBorder = C(0x8DB2E3), BodyCornerRadius = 0, BodyMargin = Padding.Empty, BodyShadowColor = Color.Transparent,
            QuickAccessBackgroundFill = body,
            BodyAppearance = new()
            {
                BorderWidth = 1, GroupSeparatorColor = Color.Transparent, QuickAccessSeparatorColor = C(0x8DB2E3),
                GroupPanel = new()
                {
                    Background = Ramp(0xDEE8F5, 0xD9E8F6, (.16, 0xC7D8ED)), BorderColor = C(0xA8BFD4),
                    BorderWidth = 1, CornerRadius = 3, Inset = 2,
                    InnerBorderColor = C(0xE4ECF5), InnerBorderWidth = 1,
                    LabelBackground = new(C(0xC1D9F1)), LabelForeground = C(0x15428B)
                }
            },
            HeaderPresentation = RibbonHeaderPresentation.Filled, HeaderFilledCornerRadius = 3, FileButtonMinimumWidth = 64,
            HeaderForeground = C(0x15428B), HeaderSelectedForeground = C(0x15428B),
            HeaderSelectedBackground = Ramp(0xF0F6FF, 0xDBE6F5, (.08, 0xF6FAFF), (.15, 0xEBF3FE)),
            HeaderHover = Ramp(0xFFFDF0, 0xFFF0BE, (.38, 0xFFF0BC), (.38, 0xFFE388)), HeaderPressed = pressed,
            HeaderAppearance = new()
            {
                ConnectSelectedTabToBody = true, FilledTabBottomBorderVisible = false, FilledBorderWidth = 1,
                FilledHoverBorderColor = C(0xC1A877), FilledPressedBorderColor = C(0xB6A892), ContextualUnselectedOpacity = 1,
                UtilityHoverBackground = new(C(0xF0F8FF), C(0xD5E8FF)),
                UtilityPressedBackground = Ramp(0xB7D2F0, 0xE4F0FD, (.45, 0xA6C6EB), (.45, 0xC3DCF7))
            },
            Hover = hover, Pressed = pressed,
            Checked = Ramp(0xFFF3D2, 0xFFDC8A, (.38, 0xFFDE84), (.38, 0xFFCB43), (.48, 0xFFC739)),
            CheckedHover = Ramp(0xFFEEC0, 0xFFD478, (.38, 0xFFD670), (.38, 0xFFC12E), (.48, 0xFFBD26)),
            CommandAppearance = new() { StateChrome = goldChrome, SplitCompanionBackground = new(Color.Transparent), SplitSeparatorColor = C(0xC1A877) },
            ApplicationButtonShape = RibbonApplicationButtonShape.Orb,
            ApplicationButtonAppearance = new()
            {
                Background = Ramp(0xFBFCFD, 0xFFFFFF, (.18, 0xF5F7F9), (.5, 0xDDE4EC), (.82, 0xEFF3F7)),
                HoverBackground = Ramp(0xFFFEFC, 0xFDFAB7, (.18, 0xFFF8D4), (.5, 0xF7C93E), (.82, 0xF7D96F)),
                PressedBackground = orbPressed, OpenBackground = orbPressed,
                BorderColor = C(0xF0F4F9), HoverBorderColor = C(0xFFEA37), PressedBorderColor = C(0xFFEA37), OpenBorderColor = C(0xFFEA37),
                GlyphColor = C(0x15428B), ShadowColor = Color.FromArgb(128, C(0x2E4257)), ShadowBlur = 2
            },
            FileButtonAppearance = new() { SquareBottomCorners = true, DisabledForeground = C(0xBECBD9) },
            CornerRadius = 3, PopupCornerRadius = 3, FocusCornerRadius = 2, CheckBoxCornerRadius = 2, KeyTipCornerRadius = 2,
            KeyTipBackground = new(C(0xFDFEFF), C(0xDDE9F8)), KeyTipBorder = C(0x8DB2E3),
            EditorAppearance = new() { StateChrome = goldChrome, CornerRadius = 3, ChoiceCornerRadius = 3 },
            GalleryAppearance = new() { StateChrome = goldChrome, UtilityStateChrome = goldChrome, TileCornerRadius = 3, StripCornerRadius = 3, UtilityCornerRadius = 2 },
            GalleryHeadingBackground = new(C(0xDBE7F3)), GalleryHeadingForeground = C(0x1E1E1E),
            MenuHeadingBackground = new(C(0xDBE7F3)), MenuHeadingForeground = C(0x15428B),
            CaptionButtonAppearance = new()
            {
                StateInset = 2, CornerRadius = 3,
                HoverBackground = Ramp(0xEAF3FD, 0xD7E7F9, (.45, 0xCFE2F7), (.45, 0xAFCDEE)),
                PressedBackground = Ramp(0x9CBDE2, 0xD2E4F8, (.45, 0x8FB3DC), (.45, 0xAECBEB)),
                CloseHoverBackground = Ramp(0xF58089, 0xE85560, (.45, 0xE9333F), (.45, 0xC60E1B)),
                ClosePressedBackground = Ramp(0xFAB4BA, 0xF59AA2, (.45, 0xF27A84), (.45, 0xE2505C))
            }
        };
    }
}
