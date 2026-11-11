namespace RibbonKit.WinForms.Layout;

// Intrinsic glyphs/Large content keep their existing layout. These roles govern
// real Small/Medium command content and the rows which contain it.
internal readonly record struct RibbonCommandContentMetrics(RibbonTheme? Theme)
{
    internal int HorizontalShift(int dpi) => Theme?.CommandContentHorizontalPadding is { } padding
        ? RibbonLayoutEngine.Scale(padding, dpi) - RibbonLayoutEngine.Scale(5, dpi) : 0;
    internal int ExtraWidth(int dpi) => 2 * Math.Max(0, HorizontalShift(dpi));
    internal bool HasVertical => Theme?.CommandContentVerticalPadding is not null;
    internal int MinimumHeight(Font font, int dpi) => Math.Max(font.Height, RibbonLayoutEngine.Scale(16, dpi))
        + 2 * RibbonLayoutEngine.Scale(Theme?.CommandContentVerticalPadding ?? 3, dpi);
    internal int RowLogical(Font font, int dpi) => HasVertical ? (int)Math.Ceiling(MinimumHeight(font, dpi) * 96d / dpi) + 1 : 0;
    internal static RibbonItemSize Size(RibbonItem item, int state) => item is RibbonCheckBoxItem or RibbonRadioButtonItem ? RibbonItemSize.Medium : item.SizePolicy.At(state);
    internal static bool Applies(RibbonItem item, RibbonItemSize size) => size != RibbonItemSize.Large && item is not (IRibbonEditorItem or RibbonMenuSeparatorItem or RibbonGalleryItem);
    internal static bool HasCandidate(RibbonGroup group) => group.Visible && group.Items.Any(i => i.Visible &&
        (Applies(i, Size(i, 0)) || group.CanResize && (Applies(i, Size(i, 1)) || Applies(i, Size(i, 2)))));
    internal static void Validate(RibbonTheme theme)
    {
        RibbonCommandAppearanceSettings.Validate(theme.CommandAppearance);
        if (theme.CommandContentHorizontalPadding is < 0 or > 1024 || theme.CommandContentVerticalPadding is < 0 or > 1024)
            throw new ArgumentOutOfRangeException(nameof(theme), "Command content padding must be 0..1024 logical pixels, or null for existing geometry.");
    }
}
