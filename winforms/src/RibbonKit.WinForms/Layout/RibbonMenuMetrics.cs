namespace RibbonKit.WinForms.Layout;

// Ordinary command menus only. Keep component-specific Application Menu,
// illustrated-choice, native field and ComboBox-list geometry independent.
internal readonly record struct RibbonMenuMetrics(RibbonDensity Density, RibbonTheme? Theme)
{
    internal static RibbonMenuMetrics For(RibbonDensity density, RibbonTheme? theme = null) => new(density, theme);
    internal bool Touch => Density == RibbonDensity.Touch;
    internal int MenuRow => Math.Max(Theme?.MenuMinimumRowHeight ?? (Touch ? 44 : 28), Touch ? 44 : 1);
    internal int MenuIcon => Theme?.MenuIconSize ?? (Touch ? 32 : 24);
    internal int MenuIconInset => Theme?.MenuLeadingPadding ?? 8;
    internal int MenuIconGap => Theme?.MenuIconTextGap ?? (Touch ? 12 : 10);
    internal int MenuIconVerticalPadding => Theme?.MenuIconVerticalPadding ?? (Touch ? 6 : 4);
    internal int MenuTextInset => MenuIconInset + MenuIcon + MenuIconGap;
    internal int MenuTextRightPadding => Theme?.MenuTrailingPadding ?? 8;
    internal int MenuIconMinimumRow => MenuIcon + 2 * MenuIconVerticalPadding;
    internal int SubmenuArrow => Theme?.MenuSubmenuArrowWidth ?? (Touch ? 28 : 16);
    internal int SplitArrow => Math.Max(Theme?.MenuSplitArrowWidth ?? RibbonDensityMetrics.For(Density).SplitArrow, Touch ? 28 : 1);
    internal int ChooserArrow => Theme?.MenuChooserArrowWidth ?? 16;
    internal int MenuSeparatorHorizontalPadding => Theme?.MenuSeparatorHorizontalPadding ?? 8;
    internal int MenuWidthPadding => MenuTextInset + MenuTextRightPadding + Math.Max(SplitArrow, SubmenuArrow) + 8;
    internal int QuickAccessWidthPadding => MenuTextInset + MenuTextRightPadding + Math.Max(SplitArrow, ChooserArrow) + 8;
    internal int MenuMinimumWidth => Theme?.MenuMinimumWidth ?? (Touch ? 220 : 170);
    internal int MenuDescriptionWidth => Theme?.MenuDescriptionMinimumWidth ?? (Touch ? 320 : 280);
    internal int MenuHeaderHorizontalPadding => Theme?.MenuHeaderHorizontalPadding ?? 6;
    internal int MenuHeaderVerticalPadding => Theme?.MenuHeaderVerticalPadding ?? (Touch ? 8 : 6);
    internal int MenuHeaderBottomGap => Theme?.MenuHeaderBottomGap ?? 4;
    internal int MenuOverflowScrollButtonHeight => Math.Max(Theme?.MenuOverflowScrollButtonHeight ?? (Touch ? 28 : 16), Touch ? 28 : 1);
    internal int HeaderHeight(Font font, int dpi) => font.Height + (Theme?.MenuHeaderVerticalPadding is null
        ? RibbonLayoutEngine.Scale(2 * MenuHeaderVerticalPadding, dpi) : 2 * RibbonLayoutEngine.Scale(MenuHeaderVerticalPadding, dpi));
    internal int HeaderWidthPadding(int dpi) => Theme?.MenuHeaderHorizontalPadding is null
        ? RibbonLayoutEngine.Scale(2 * MenuHeaderHorizontalPadding + 16, dpi)
        : 2 * RibbonLayoutEngine.Scale(MenuHeaderHorizontalPadding, dpi) + RibbonLayoutEngine.Scale(16, dpi);
    internal int MenuDescriptionVerticalPadding => Theme?.MenuDescriptionVerticalPadding ?? (Touch ? 10 : 5);
    internal int MenuDescriptionGap => Theme?.MenuDescriptionGap ?? (Touch ? 4 : 2);
    internal int MenuSeparatorHeight => Theme?.MenuSeparatorHeight ?? 9;
    // Explicit padding reserves both separately rounded sides. Unset padding
    // preserves the pre-consolidation summed rounding and default paint exactly.
    internal int IconMinimumRowHeight(int dpi) => Theme?.MenuIconVerticalPadding is null
        ? RibbonLayoutEngine.Scale(MenuIconMinimumRow, dpi)
        : RibbonLayoutEngine.Scale(MenuIcon, dpi) + 2 * RibbonLayoutEngine.Scale(MenuIconVerticalPadding, dpi);
    internal int DescriptionPaddingHeight(int dpi) => Theme?.MenuDescriptionVerticalPadding is null
        ? RibbonLayoutEngine.Scale(2 * MenuDescriptionVerticalPadding, dpi)
        : 2 * RibbonLayoutEngine.Scale(MenuDescriptionVerticalPadding, dpi);

    internal static void Validate(RibbonTheme theme)
    {
        if (theme.MenuMinimumRowHeight is < 1 or > 1024 || theme.MenuIconSize is < 1 or > 1024 ||
            theme.MenuLeadingPadding is < 0 or > 1024 || theme.MenuTrailingPadding is < 0 or > 1024 ||
            theme.MenuIconTextGap is < 0 or > 1024 || theme.MenuIconVerticalPadding is < 0 or > 1024 ||
            theme.MenuDescriptionVerticalPadding is < 0 or > 1024 || theme.MenuDescriptionGap is < 0 or > 1024 ||
            theme.MenuSeparatorHeight is < 1 or > 1024 || theme.MenuMinimumWidth is < 1 or > 1024 ||
            theme.MenuDescriptionMinimumWidth is < 1 or > 1024 || theme.MenuHeaderHorizontalPadding is < 0 or > 1024 ||
            theme.MenuHeaderVerticalPadding is < 0 or > 1024 || theme.MenuHeaderBottomGap is < 0 or > 1024 ||
            theme.MenuOverflowScrollButtonHeight is < 1 or > 1024 || theme.MenuSubmenuArrowWidth is < 1 or > 1024 ||
            theme.MenuSplitArrowWidth is < 1 or > 1024 || theme.MenuChooserArrowWidth is < 1 or > 1024 ||
            theme.MenuSeparatorHorizontalPadding is < 0 or > 1024)
            throw new ArgumentOutOfRangeException(nameof(theme), "Menu row/icon/separator/width/scroll dimensions must be 1..1024 and padding/gap values 0..1024, or null for density defaults.");
    }
}
