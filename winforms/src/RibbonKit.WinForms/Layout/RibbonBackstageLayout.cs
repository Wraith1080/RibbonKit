namespace RibbonKit.WinForms.Layout;

internal sealed record RibbonBackstageLayout(Rectangle Back, Rectangle Navigation, Rectangle Page, int RowHeight, int Inset)
{
    internal static RibbonBackstageLayout Build(Size size, Font font, int dpi, RibbonBackstage? backstage = null)
    {
        int S(int value) => RibbonLayoutEngine.Scale(value, dpi);
        int inset = S(8), row = Math.Max(S(42), font.Height + S(16));
        int width = Math.Max(0, size.Width);
        // Preserve 240 logical pixels for a page when possible; very narrow hosts
        // divide their space evenly rather than letting either region disappear.
        int available = Math.Clamp(Math.Max(width / 2, width - S(240)), 0, width);
        int ToDevice(int logical) => (int)Math.Clamp(Math.Round(logical * (double)dpi / 96), 0, available);
        int minimum = ToDevice(backstage?.MinimumNavigationWidth ?? 160);
        int maximum = Math.Max(minimum, ToDevice(backstage?.MaximumNavigationWidth ?? 320));
        int auto = S(200);
        if (backstage is not null)
            foreach (var item in backstage.Items.Where(i => i.Visible && !i.IsDisposed))
            {
                int label = TextRenderer.MeasureText(item.Text, font, Size.Empty, TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding).Width;
                // Shared entry text begins at 44, ends 8 before the edge, and is
                // inset by 8 on each side. Reserve the native vertical scrollbar.
                auto = (int)Math.Min(int.MaxValue, Math.Max((long)auto, (long)label + S(68 + 18)));
            }
        int rail = Math.Clamp(backstage?.NavigationWidth is { } authored ? ToDevice(authored) : auto, minimum, maximum);
        int navigation = rail;
        int back = Math.Min(S(36), Math.Max(0, navigation - 2 * inset));
        int top = Math.Min(size.Height, S(20) + back + S(24));
        return new(new(Math.Min(S(20), Math.Max(0, navigation - back)), S(20), back, back), new(0, top, navigation, Math.Max(0, size.Height - top)),
            new(rail, 0, width - rail, size.Height), row, inset);
    }
}
