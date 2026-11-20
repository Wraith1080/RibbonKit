namespace RibbonKit.WinForms;

// Library-owned footer decoration; native layout, validation and Button actions
// retain their original owners. Application pages are never traversed/restyled.
internal sealed class RibbonOptionsFooter(RibbonControl ribbon, bool legacyDivider = false) : FlowLayoutPanel
{
    private readonly RoundedPainter _painter = new();
    internal void DrawButtonBackground(Graphics graphics, Rectangle bounds, Point location)
    {
        if (ribbon.Theme.OptionsAppearance?.FooterBackground is not { } fill) return;
        var saved = graphics.Save();
        try { graphics.SetClip(bounds, System.Drawing.Drawing2D.CombineMode.Intersect); _painter.Draw(graphics, new(-location.X, -location.Y, Width, Height), 0, fill, Color.Transparent, 0); }
        finally { graphics.Restore(saved); }
    }
    protected override void OnPaintBackground(PaintEventArgs e)
    {
        base.OnPaintBackground(e);
        if (ribbon.Theme.OptionsAppearance?.FooterBackground is { } fill)
            _painter.Draw(e.Graphics, ClientRectangle, 0, fill, Color.Transparent, 0);
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (legacyDivider || ribbon.Theme.OptionsAppearance?.FooterBackground is not null)
            RibbonOptionsRenderer.DrawDivider(e.Graphics, ClientRectangle, ribbon.Theme, DeviceDpi, vertical: false);
    }
    protected override void Dispose(bool disposing) { base.Dispose(disposing); if (disposing) _painter.Dispose(); }
}
