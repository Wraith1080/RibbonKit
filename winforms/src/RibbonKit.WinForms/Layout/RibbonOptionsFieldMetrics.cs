namespace RibbonKit.WinForms.Layout;

internal sealed record RibbonOptionsFieldDefaults(int FrameBorder, int NativeClearance, int SearchInset, int SearchDeduction, int ChoiceInset);

internal readonly record struct RibbonOptionsFieldMetrics(RibbonTheme Theme, int Dpi)
{
    private static RibbonOptionsFieldDefaults D => RibbonPresentationResolver.OptionsFields;
    private int S(int n) => RibbonLayoutEngine.Scale(n, Dpi);
    private int C(int n) => (int)Math.Ceiling(n * Dpi / 96d);
    internal int Radius(int fallback) => Theme.OptionsAppearance?.FrameCornerRadius is { } n ? S(n) : fallback;
    internal Color Border => Theme.OptionsAppearance is { FrameBorderColor.IsEmpty: false } a ? a.FrameBorderColor : Theme.PopupBorder;
    internal int RequestedBorder => Theme.OptionsAppearance?.FrameBorderWidth is { } n ? S(n) : D.FrameBorder;
    internal int BorderWidth(Size size) => Theme.OptionsAppearance?.FrameBorderWidth is not null ? Math.Min(RequestedBorder, Math.Max(0, (Math.Min(size.Width, size.Height) - 1) / 2)) : D.FrameBorder;
    internal int Clearance(Size size) => Math.Max(D.NativeClearance, BorderWidth(size));
    internal int ChildRadius(Size size, int fallback) => Math.Max(0, Math.Min(Radius(fallback), Math.Max(0, Math.Min(size.Width, size.Height) / 2)) - Clearance(size));
    internal int TextFrameHeight(int desired, int textHeight) => Math.Max(desired, textHeight + 2 * Math.Max(D.NativeClearance, RequestedBorder));
    internal Rectangle SearchTextBounds(Rectangle bounds, int textHeight, bool summed)
    {
        int clearance = Clearance(bounds.Size);
        int inset = Math.Max(clearance, Theme.EditorTextInset is { } n ? Math.Min(S(n), Math.Max(0, (bounds.Width - 1) / 2)) : C(D.SearchInset));
        int deduction = Theme.EditorTextInset is null && Theme.OptionsAppearance?.FrameBorderWidth is null && summed ? C(D.SearchDeduction) : 2 * inset;
        return new(bounds.Left + inset, bounds.Top + Math.Max(0, (bounds.Height - textHeight) / 2), Math.Max(1, bounds.Width - deduction), textHeight);
    }
    internal Rectangle ChoiceTextBounds(Rectangle field, RibbonDensity density)
    {
        var arrow = RibbonEditorLayout.ComboButton(field, Dpi, density, Theme); int inset = Math.Min(S(Theme.EditorTextInset ?? D.ChoiceInset), Math.Max(0, arrow.Left - field.Left));
        var text = new Rectangle(field.Left + inset, field.Top, Math.Max(0, arrow.Left - field.Left - inset), field.Height);
        if (Theme.EditorAppearance is { BorderWidth: not null } or { Inset: not null })
        {
            var metrics = new RibbonEditorAppearanceMetrics(Theme, Dpi, RibbonPresentationResolver.EditorChrome); var paint = metrics.PaintBounds(field, choice: false); int stroke = metrics.BorderWidth(paint, Color.Black);
            int left = Math.Max(text.Left, paint.Left + stroke), top = Math.Max(text.Top, paint.Top + stroke);
            text = Rectangle.FromLTRB(left, top, Math.Max(left, Math.Min(text.Right, paint.Right - stroke)), Math.Max(top, Math.Min(text.Bottom, paint.Bottom - stroke)));
        }
        return text;
    }
}
