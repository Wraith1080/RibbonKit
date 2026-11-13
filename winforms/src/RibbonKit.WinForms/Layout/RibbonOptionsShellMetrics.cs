namespace RibbonKit.WinForms.Layout;

internal sealed record RibbonOptionsShellDefaults(int ButtonInset, int ButtonFocusInset, int ButtonBorder, int AccentBorder, float HeadingIncrement, float HeadingMinimum, int HeadingBottomPadding);

internal readonly record struct RibbonOptionsShellMetrics(RibbonTheme Theme, int Dpi)
{
    private RibbonOptionsAppearanceSettings? A => Theme.OptionsAppearance;
    private static RibbonOptionsShellDefaults D => RibbonPresentationResolver.OptionsShell;
    private int S(int n) => (int)Math.Ceiling(n * Dpi / 96d);
    internal Rectangle ButtonBounds(Rectangle bounds)
    {
        int inset = Math.Min(S(A?.ButtonInset ?? D.ButtonInset), Math.Max(0, (Math.Min(bounds.Width, bounds.Height) - 1) / 2));
        return Rectangle.Inflate(bounds, -inset, -inset);
    }
    internal Rectangle ButtonFocusBounds(Rectangle bounds)
    {
        int inset = A?.ButtonFocusInset is { } n ? Math.Min(S(n), Math.Max(0, (Math.Min(bounds.Width, bounds.Height) - 1) / 2)) : D.ButtonFocusInset;
        return Rectangle.Inflate(bounds, -inset, -inset);
    }
    internal Color ButtonBorder(Color fallback) => A is { ButtonBorderColor.IsEmpty: false } ? A.ButtonBorderColor : fallback;
    internal int ButtonStroke(Rectangle bounds, Color ink, bool accent) => A?.ButtonBorderWidth is { } n ? Math.Min(S(n), Math.Max(0, Math.Min(bounds.Width, bounds.Height) / 2)) : accent ? D.AccentBorder : ink.A == 0 ? 0 : D.ButtonBorder;
    internal RibbonFill ButtonFill(bool accent, bool enabled, bool hot, bool pressed) => accent
        ? !enabled ? Theme.Hover : pressed ? Theme.Pressed : hot ? Theme.Checked : A?.AccentButtonBackground ?? new(Theme.Accent)
        : !enabled ? new(Theme.Surface) : pressed ? Theme.Pressed : hot ? Theme.Hover : A?.ButtonBackground ?? new(Theme.Surface);
    internal float HeadingPoints(Font font) => Math.Max(font.SizeInPoints + (A?.HeadingFontSizeIncrement ?? D.HeadingIncrement), A?.HeadingMinimumFontSize ?? D.HeadingMinimum);
    internal FontStyle HeadingStyle(Font font) => A?.HeadingBold is { } bold ? bold ? font.Style | FontStyle.Bold : font.Style & ~FontStyle.Bold : FontStyle.Bold;
    internal int HeadingPadding => S(A?.HeadingBottomPadding ?? D.HeadingBottomPadding);
}
