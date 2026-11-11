using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

// Style-free contract. The active public renderer retains text, focus and
// indicator hooks, drawing primitives, caches and lifetime ownership.
internal abstract class RibbonHeaderRenderer
{
    internal abstract void DrawTab(RibbonRenderer owner, RibbonPaintContext context, RibbonTab tab, bool selected, RibbonHeaderMetrics metrics);
    internal abstract void DrawFileButton(RibbonRenderer owner, RibbonPaintContext context, RibbonDropDownItem menu, RibbonHeaderMetrics metrics);
    internal abstract Rectangle GetTabIndicatorBounds(RibbonPaintContext context, RibbonTab tab, RibbonHeaderMetrics metrics);
    internal abstract void DrawTabIndicator(RibbonRenderer owner, RibbonPaintContext context, RibbonTab tab, bool selected, RibbonHeaderMetrics metrics);
}
