using System.Runtime.CompilerServices;

namespace RibbonKit.WinForms;

internal static class RibbonCustomizationIcons
{
    private static readonly ConditionalWeakTable<Image, ImageIcon> Images = new();
    internal static IRibbonIcon? Icon(RibbonElement source) => source switch
    {
        RibbonItem { Icon: { } icon } => icon,
        RibbonItem { Image: { } image } => Images.GetValue(image, i => new(i)),
        RibbonItem { LargeImage: { } image } => Images.GetValue(image, i => new(i)),
        _ => null
    };
    private sealed class ImageIcon(Image image) : IRibbonIcon
    {
        public void Draw(Graphics graphics, Rectangle bounds, RibbonIconContext context) => graphics.DrawImage(image, bounds);
    }
}
