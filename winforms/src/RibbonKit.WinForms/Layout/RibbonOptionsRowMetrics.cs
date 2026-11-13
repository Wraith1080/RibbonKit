namespace RibbonKit.WinForms.Layout;

internal sealed record RibbonOptionsRowDefaults(int CommandPadding, int SimplifiedPadding, int IconSize, int SelectionRadius);
internal readonly record struct RibbonOptionsRowSlots(Rectangle Icon, Rectangle Text);
internal readonly record struct RibbonOptionsRowMetrics(RibbonTheme Theme, int Dpi)
{
    private static RibbonOptionsRowDefaults D => RibbonPresentationResolver.OptionsRows;
    private int S(int n) => RibbonLayoutEngine.Scale(n, Dpi);
    private int C(int n) => (int)Math.Ceiling(n * Dpi / 96d);
    internal int Radius => S(Theme.OptionsAppearance?.RowCornerRadius ?? D.SelectionRadius);
    internal FontStyle HeadingStyle(FontStyle host) => Theme.OptionsAppearance?.RowHeadingBold is { } bold ? bold ? host | FontStyle.Bold : host & ~FontStyle.Bold : FontStyle.Bold;
    internal RibbonOptionsRowSlots Resolve(Rectangle row, bool simplified, bool heading)
    {
        var a = Theme.OptionsAppearance; int legacy = simplified ? D.SimplifiedPadding : D.CommandPadding;
        bool authored = a?.RowTextPadding is not null || a?.RowIconSize is not null || a?.RowIconTextGap is not null;
        int padding = a?.RowTextPadding is { } authoredPadding ? S(authoredPadding) : C(legacy);
        if (authored) padding = Math.Min(padding, Math.Max(0, row.Width / 2));
        var content = Rectangle.Inflate(row, -padding, 0);
        if (heading) return new(Rectangle.Empty, content with { Width = Math.Max(0, content.Width) });
        int side = a?.RowIconSize is { } authoredSize ? S(authoredSize) : C(D.IconSize);
        if (authored) side = Math.Min(side, Math.Max(0, Math.Min(content.Width, row.Height)));
        var icon = side > 0 ? new Rectangle(content.Left, row.Top + (row.Height - side) / 2, side, side) : Rectangle.Empty;
        int gap = side == 0 ? 0 : a?.RowIconTextGap is { } authoredGap ? S(authoredGap) : C(legacy);
        if (authored) gap = Math.Min(gap, Math.Max(0, content.Width - side));
        content.X += side + gap; content.Width = Math.Max(0, content.Width - side - gap);
        return new(icon, content);
    }
}
