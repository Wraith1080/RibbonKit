namespace RibbonKit.WinForms.Layout;

// Shared space negotiation and DPI arithmetic; families own their defaults.
internal abstract class RibbonBackstageLayoutProvider
{
    internal abstract RibbonBackstageLayoutDefaults Defaults { get; }
    internal virtual RibbonBackstageLayout Build(Size size, in RibbonBackstageLayoutInput input)
    {
        int dpi = input.DeviceDpi;
        int S(int value) => RibbonLayoutEngine.Scale(value, dpi);
        var metrics = input.Metrics;
        int inset = S(metrics.NavigationPadding), row = Math.Max(S(metrics.RowHeight), input.FontHeight + S(metrics.RowTextVerticalPadding));
        int width = Math.Max(0, size.Width);
        int shadow = input.PageShadowInset;
        // Preserve the family's minimum page space when possible; very narrow hosts
        // divide their space evenly rather than letting either region disappear.
        int available = Math.Clamp(Math.Max(width / 2, width - S(metrics.MinimumPageWidth)), 0, width);
        int ToDevice(int logical) => (int)Math.Clamp(Math.Round(logical * (double)dpi / 96), 0, available);
        int minimum = ToDevice(input.MinimumNavigationWidth);
        int maximum = Math.Max(minimum, ToDevice(input.MaximumNavigationWidth));
        int auto = S(metrics.AutoMinimumWidth);
        // Reserve the same entry text offset as painting, row side padding,
        // trailing clearance and the native vertical scrollbar.
        if (input.MaximumLabelWidth is { } label)
            auto = (int)Math.Min(int.MaxValue, Math.Max((long)auto, (long)label + S(metrics.AutoWidthPadding) + shadow));
        int rail = Math.Clamp(input.NavigationWidth is { } authored ? ToDevice(authored) : auto, minimum, maximum);
        int navigation = rail;
        inset = Math.Min(inset, Math.Max(0, (navigation - shadow) / 2));
        int backTop = Math.Min(Math.Max(0, input.CaptionHeight) + S(metrics.BackInset), Math.Max(0, size.Height));
        int back = Math.Min(S(metrics.BackSize), Math.Min(Math.Max(0, navigation - 2 * inset), Math.Max(0, size.Height - backTop)));
        int top = Math.Min(size.Height, backTop + back + S(metrics.BackGap));
        int separator = S(metrics.SeparatorHeight);
        int mainHeight = checked(input.MainRows * row + input.MainSeparators * separator);
        int bottomHeight = checked(input.FooterRows * row + input.FooterSeparators * separator);
        int footerPadding = metrics.FooterUsesNavigationInset ? inset : Math.Min(S(metrics.FooterBottomPadding), Math.Max(0, size.Height));
        int availableHeight = Math.Max(0, size.Height - top), footerHeight = bottomHeight == 0 ? 0 : bottomHeight + footerPadding;
        bool inline = footerHeight > 0 && footerHeight + Math.Min(row, mainHeight) > availableHeight;
        if (inline) footerHeight = 0;
        var nav = new Rectangle(0, top, Math.Max(0, navigation - shadow), availableHeight - footerHeight);
        return new(new(Math.Min(S(metrics.BackInset), Math.Max(0, navigation - back)), backTop, back, back), nav,
            new(rail, shadow, width - rail, Math.Max(0, size.Height - shadow)), row, inset)
        {
            Footer = footerHeight > 0 ? new(0, size.Height - footerHeight, nav.Width, footerHeight) : Rectangle.Empty,
            FooterInline = inline, SeparatorHeight = separator, NavigationContentHeight = mainHeight + (inline ? bottomHeight : 0),
            PagePadding = S(metrics.PagePadding)
        };
    }
}
