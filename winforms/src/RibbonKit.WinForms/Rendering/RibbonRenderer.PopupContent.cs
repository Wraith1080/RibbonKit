using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

public partial class RibbonRenderer
{
    internal static int PopupContentBorderWidth(RibbonTheme theme, int dpi) => theme.PopupAppearance?.ContentBorderWidth is { } width ? RibbonLayoutEngine.Scale(width, dpi) : 1;
    /// <summary>Paints a theme-owned frame around a custom popup or gallery presenter viewport. Bounds include the outside border; application content and the popup outer frame retain their owners.</summary>
    public virtual void DrawPopupContentFrame(RibbonPaintContext context)
    {
        int width = Math.Min(PopupContentBorderWidth(context.Theme, context.DeviceDpi), Math.Max(0, Math.Min(context.Bounds.Width, context.Bounds.Height) / 2));
        var color = context.Theme.PopupAppearance is { ContentBorderColor.IsEmpty: false } a ? a.ContentBorderColor : context.Theme.EditorBorderColor;
        if (width <= 0 || color.A == 0) return;
        using var border = new SolidBrush(color); var b = context.Bounds;
        context.Graphics.FillRectangle(border, b.Left, b.Top, b.Width, width);
        context.Graphics.FillRectangle(border, b.Left, b.Bottom - width, b.Width, width);
        context.Graphics.FillRectangle(border, b.Left, b.Top + width, width, Math.Max(0, b.Height - 2 * width));
        context.Graphics.FillRectangle(border, b.Right - width, b.Top + width, width, Math.Max(0, b.Height - 2 * width));
    }
}
