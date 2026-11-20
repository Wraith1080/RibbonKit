using System.Drawing.Drawing2D;

namespace RibbonKit.WinForms;

// Screen-space exclusions remain anchored to the real ribbon, not to a proxy
// command or reparented visual. Both native clipping and layered paint use them.
internal readonly record struct RibbonApplicationMenuComposition(Rectangle Orb, Rectangle Caption)
{
    internal bool IsEmpty => Orb.IsEmpty && Caption.IsEmpty;
    internal Region Exclusion(Rectangle canvas)
    {
        var region = new Region(); region.MakeEmpty();
        if (!Orb.IsEmpty)
        {
            var orb = Orb; orb.Offset(-canvas.X, -canvas.Y);
            using var path = new GraphicsPath(); path.AddEllipse(orb); region.Union(path);
        }
        if (!Caption.IsEmpty)
        {
            var caption = Caption; caption.Offset(-canvas.X, -canvas.Y); region.Union(caption);
        }
        return region;
    }
    internal int TopClearance(int originY, int minimum) => Math.Max(minimum, Math.Max(0, Math.Max(Orb.Bottom, Caption.IsEmpty ? originY : Caption.Bottom) - originY));
    internal bool OpensAbove(Point origin, Rectangle work, int minimumBand, int minimumContent, out int bottom)
    {
        bottom = Math.Clamp(Math.Min(Orb.Top, Caption.IsEmpty ? Orb.Top : Caption.Top), work.Top, work.Bottom);
        int below = work.Bottom - origin.Y;
        return below < TopClearance(origin.Y, minimumBand) + minimumContent && bottom - work.Top > below;
    }
}
