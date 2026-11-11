namespace RibbonKit.WinForms.Layout;

internal sealed record RibbonEditorChromeDefaults(int Inset, int BorderWidth, int ChoiceInset, int ChoiceTextPadding);

// Resolved paint values only; native geometry and lifecycle stay in their owners.
internal readonly record struct RibbonEditorAppearanceMetrics(RibbonTheme Theme, int Dpi, RibbonEditorChromeDefaults Defaults)
{
    internal int ChoiceTextPadding => Theme.EditorAppearance?.ChoiceTextHorizontalPadding ?? Defaults.ChoiceTextPadding;
    internal Color ActiveBorder => Theme.EditorAppearance is { ActiveBorderColor.IsEmpty: false } settings ? settings.ActiveBorderColor : Theme.Accent;
    internal int Radius(int fallback, bool choice) => (choice ? Theme.EditorAppearance?.ChoiceCornerRadius : Theme.EditorAppearance?.CornerRadius) is { } radius
        ? RibbonLayoutEngine.Scale(radius, Dpi) : fallback;
    internal Rectangle PaintBounds(Rectangle target, bool choice)
    {
        int? authored = choice ? Theme.EditorAppearance?.ChoiceInset : Theme.EditorAppearance?.Inset;
        int inset = authored is { } n ? Math.Min(RibbonLayoutEngine.Scale(n, Dpi), Math.Max(0, (Math.Min(target.Width, target.Height) - 1) / 2)) : choice ? Defaults.ChoiceInset : Defaults.Inset;
        return Rectangle.Inflate(target, -inset, -inset);
    }
    internal int BorderWidth(Rectangle bounds, Color ink) => ink.A == 0 ? 0 : Theme.EditorAppearance?.BorderWidth is { } n
        ? Math.Min(RibbonLayoutEngine.Scale(n, Dpi), Math.Max(0, Math.Min(bounds.Width, bounds.Height) / 2)) : Defaults.BorderWidth;
    internal Rectangle ChoiceTextBounds(Rectangle row)
    {
        int inset = Theme.EditorAppearance?.ChoiceTextHorizontalPadding is { } n ? Math.Min(RibbonLayoutEngine.Scale(n, Dpi), Math.Max(0, row.Width / 2))
            : RibbonLayoutEngine.Scale(Defaults.ChoiceTextPadding, Dpi);
        return Rectangle.Inflate(row, -inset, 0);
    }
}
