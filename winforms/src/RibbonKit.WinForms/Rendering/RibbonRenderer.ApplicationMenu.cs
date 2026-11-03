namespace RibbonKit.WinForms;

public partial class RibbonRenderer
{
    /// <summary>Paints the Application Menu's measured columns and fixed footer using shared menu surface and border roles. Native application content keeps its own styling.</summary>
    public virtual void DrawApplicationMenuBackground(RibbonPaintContext context, Rectangle navigation, Rectangle pane, Rectangle footer)
    {
        using var border = new Pen(context.Theme.Border);
        int x = Math.Max(navigation.Right, pane.Left - 1);
        context.Graphics.DrawLine(border, x, navigation.Top, x, Math.Max(navigation.Top, navigation.Bottom - 1));
        if (footer.Height > 0) context.Graphics.DrawLine(border, context.Bounds.Left, Math.Max(0, footer.Top - 2), context.Bounds.Right - 1, Math.Max(0, footer.Top - 2));
    }
}
