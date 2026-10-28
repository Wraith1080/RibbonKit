namespace RibbonKit.WinForms.Layout;

// Logical target geometry shared by measurement, native fields, painting and
// borrowed surfaces. Compact values preserve the established native foundation.
internal readonly record struct RibbonDensityMetrics(bool Touch)
{
    internal static RibbonDensityMetrics For(RibbonDensity density) => new(density == RibbonDensity.Touch);
    internal int CommandRow => Touch ? 45 : 26;
    internal int TabHeight => Touch ? 48 : 32;
    internal int SmallWidth => Touch ? 44 : 28;
    internal int LargeWidth => Touch ? 80 : 60;
    internal int MediumWidth => Touch ? 64 : 54;
    internal int CaptionPadding => Touch ? 48 : 38;
    internal int ColumnGap => Touch ? 6 : 3;
    internal int MenuRow => Touch ? 44 : 28;
    internal int MenuIcon => Touch ? 32 : 24;
    internal int MenuIconInset => 8;
    internal int MenuIconGap => Touch ? 12 : 10;
    internal int MenuIconVerticalPadding => Touch ? 6 : 4;
    internal int MenuTextInset => MenuIconInset + MenuIcon + MenuIconGap;
    internal int MenuTextRightPadding => 8;
    internal int MenuIconMinimumRow => MenuIcon + 2 * MenuIconVerticalPadding;
    internal int MenuWidthPadding => MenuTextInset + MenuTextRightPadding + Math.Max(SplitArrow, Touch ? 28 : 16) + 8;
    internal int MenuMinimumWidth => Touch ? 220 : 170;
    internal int MenuDescriptionWidth => Touch ? 320 : 280;
    internal int MenuVerticalPadding => Touch ? 10 : 5;
    internal int MenuDescriptionGap => Touch ? 4 : 2;
    internal int EditorHeight => Touch ? 42 : 0;
    internal int ComboButton => Touch ? 32 : 24;
    internal int SplitArrow => Touch ? 28 : 20;
    internal int HorizontalSplitArrow => Touch ? 44 : 20;
    internal int HorizontalSplitPrimaryMinimum => Touch ? 56 : 0;
    internal int LargeHorizontalSplitPrimaryMinimum => Touch ? 72 : 0;
    internal int VerticalPrimary => Touch ? 90 : 44;
    internal int LargeIconCaptionGap => 8;
    internal int LargeCaptionArrowHeight => 12;
    internal int LargeCaptionArrowGap => 4;
    internal int QuickAccessButton => Touch ? 44 : 28;
    internal int QuickAccessMenu => Touch ? 64 : 40;
    internal int QuickAccessRow => Touch ? 48 : 32;
    internal int QuickAccessArrow => Touch ? 20 : 12;
    internal int ScrollButton => Touch ? 32 : 22;
    internal int GalleryUtility => Touch ? 32 : 18;
    internal int GalleryWidth => Touch ? 64 : 32;
    internal int GalleryHeight => Touch ? 80 : 32;
    internal int BackstageRow => Touch ? 52 : 42;
    internal int BackButton => Touch ? 44 : 36;
}
