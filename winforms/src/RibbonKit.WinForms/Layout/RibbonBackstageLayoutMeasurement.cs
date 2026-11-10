namespace RibbonKit.WinForms.Layout;

// A value-only measurement snapshot: no theme/model, Font, window or renderer
// references enter the shared arrangement algorithm.
internal readonly record struct RibbonBackstageLayoutInput(
    RibbonBackstageMetrics Metrics, int DeviceDpi, int FontHeight,
    int? NavigationWidth, int MinimumNavigationWidth, int MaximumNavigationWidth,
    int? MaximumLabelWidth, int MainRows, int MainSeparators, int FooterRows, int FooterSeparators,
    int CaptionHeight, int PageShadowInset);

internal static class RibbonBackstageLayoutMeasurement
{
    internal static RibbonBackstageLayout Build(Size size, Font font, int dpi, RibbonBackstage? backstage, RibbonItem? reveal, int captionHeight)
    {
        var ribbon = backstage?.Ribbon;
        var theme = ribbon?.Theme;
        var presentation = RibbonPresentationResolver.Backstage(theme);
        var metrics = presentation.ResolveMetrics(theme, ribbon?.Density ?? RibbonDensity.Compact);
        int shadow = ribbon is { HasWindowBackdrop: true } ? RibbonBackstageLayout.PageShadowInset(ribbon.Painter, theme!, dpi, size) : 0;
        var input = Measure(font, dpi, backstage, reveal, captionHeight, metrics, shadow);
        return presentation.Layout.Build(size, input);
    }

    internal static RibbonBackstageLayoutInput Measure(Font font, int dpi, RibbonBackstage? backstage, RibbonItem? reveal,
        int captionHeight, RibbonBackstageMetrics metrics, int shadow)
    {
        int mainRows = 0, mainSeparators = 0, footerRows = 0, footerSeparators = 0;
        int? maximumLabel = null;
        void Add(IEnumerable<RibbonItem> items, bool footer)
        {
            foreach (var item in items.Where(item => !item.IsDisposed && (item.Visible || ReferenceEquals(item, reveal))))
            {
                if (item is RibbonMenuSeparatorItem)
                {
                    if (footer) footerSeparators++; else mainSeparators++;
                }
                else
                {
                    if (footer) footerRows++; else mainRows++;
                    int label = TextRenderer.MeasureText(item.Text, font, Size.Empty, TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding).Width;
                    maximumLabel = Math.Max(maximumLabel ?? 0, label);
                }
            }
        }
        if (backstage is not null) { Add(backstage.Items, false); Add(backstage.FooterItems, true); }
        return new(metrics, dpi, font.Height, backstage?.NavigationWidth, backstage?.MinimumNavigationWidth ?? 160,
            backstage?.MaximumNavigationWidth ?? 320, maximumLabel, mainRows, mainSeparators, footerRows, footerSeparators, captionHeight, shadow);
    }
}
