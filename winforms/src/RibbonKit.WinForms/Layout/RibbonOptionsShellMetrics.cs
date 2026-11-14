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
    internal RibbonFill ButtonFill(bool accent, bool enabled, bool hot, bool pressed)
    {
        if (accent && enabled)
        {
            if (pressed && A?.AccentButtonPressedBackground is { } down) return down;
            if (!pressed && hot && A?.AccentButtonHoverBackground is { } hover) return hover;
        }
        if (Theme.OptionsButtonAccentStates is not { } states)
            return accent ? !enabled ? Theme.Hover : pressed ? Theme.Pressed : hot ? Theme.Checked : A?.AccentButtonBackground ?? new(Theme.Accent)
                : !enabled ? new(Theme.Surface) : pressed ? Theme.Pressed : hot ? Theme.Hover : A?.ButtonBackground ?? new(Theme.Surface);
        if (!enabled) return accent ? Theme.Hover : new(Theme.Surface);
        if (accent)
        {
            var idle = A?.AccentButtonBackground ?? new(Theme.Accent);
            return pressed ? Theme.PressedOverride ?? states.PressedBackground ?? Darken(idle, A?.AccentButtonPressedDarkening ?? .20)
                : hot ? Theme.CheckedOverride ?? Darken(idle, A?.AccentButtonHoverDarkening ?? .10) : idle;
        }
        if (pressed) return Theme.PressedOverride ?? states.PressedBackground ?? RibbonAccentStateSettings.Wash(Theme.Accent, Theme.Surface, states.PressedOpacity);
        if (hot) return Theme.HoverOverride ?? states.HoverBackground ?? RibbonAccentStateSettings.Wash(Theme.Accent, Theme.Surface, states.HoverOpacity);
        return A?.ButtonBackground ?? new(Theme.Surface);
    }
    internal Color ButtonForeground(bool accent, bool enabled, bool hot, bool pressed) => !enabled ? accent ? SystemColors.GrayText : Theme.DisabledColor
        : accent && A is { AccentButtonForeground.IsEmpty: false } ? A.AccentButtonForeground
        : accent && (Theme.OptionsButtonAccentStates is not null || !hot && !pressed) ? Theme.Surface : Theme.Foreground;
    private static RibbonFill Darken(RibbonFill fill, double amount)
    {
        if (amount == 0) return fill;
        Color Stop(Color c) => Color.FromArgb(c.A, (int)Math.Round(c.R * (1 - amount)), (int)Math.Round(c.G * (1 - amount)), (int)Math.Round(c.B * (1 - amount)));
        return RibbonFillColors.Map(fill, Stop);
    }
    internal float HeadingPoints(Font font) => Math.Max(font.SizeInPoints + (A?.HeadingFontSizeIncrement ?? D.HeadingIncrement), A?.HeadingMinimumFontSize ?? D.HeadingMinimum);
    internal FontStyle HeadingStyle(Font font) => A?.HeadingBold is { } bold ? bold ? font.Style | FontStyle.Bold : font.Style & ~FontStyle.Bold : FontStyle.Bold;
    internal int HeadingPadding => S(A?.HeadingBottomPadding ?? D.HeadingBottomPadding);
}
