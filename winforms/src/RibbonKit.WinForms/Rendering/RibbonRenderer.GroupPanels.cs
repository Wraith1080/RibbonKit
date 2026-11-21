using System.Drawing.Drawing2D;
using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

public partial class RibbonRenderer
{
    /// <summary>Paints optional group fill, rims and label band behind content using GroupFrameBounds/GroupCaptionBounds. Called by DrawGroup and for measured inline Simplified runs. Overrides can replace decoration without replacing content, layout or input.</summary>
    public virtual void DrawGroupPanel(RibbonPaintContext context, RibbonGroup group, bool collapsed)
    {
        if (context.Theme.BodyAppearance?.GroupPanel is not { HasOverrides: true } settings) return;
        if (context.IsSimplifiedGroup && !settings.ShowInSimplified) return;
        var metrics = RibbonGroupPanelMetrics.Resolve(context, collapsed);
        if (metrics.Panel.Width <= 0 || metrics.Panel.Height <= 0) return;
        var saved = context.Graphics.Save();
        try
        {
            context.Graphics.SetClip(metrics.Panel, CombineMode.Intersect);
            context.Graphics.CompositingMode = CompositingMode.SourceOver;
            if (settings.Background is { } background)
                _rounded.Draw(context.Graphics, metrics.Panel, metrics.Radius, background, Color.Transparent, 0);
            if (settings.LabelBackground is { } label && metrics.Caption.Width > 0 && metrics.Caption.Height > 0)
            {
                var bandState = context.Graphics.Save();
                try
                {
                    context.Graphics.SetClip(metrics.Caption, CombineMode.Intersect);
                    // Keep the full panel corner coverage but sample the band in its own coordinates.
                    _rounded.Draw(context.Graphics, metrics.Interior, Math.Max(0, metrics.Radius - metrics.Stroke), label, Color.Transparent, 0, fillBounds: metrics.Caption);
                }
                finally { context.Graphics.Restore(bandState); }
            }
            _rounded.Draw(context.Graphics, metrics.Panel, metrics.Radius, new(Color.Transparent), settings.BorderColor.IsEmpty ? Color.Transparent : settings.BorderColor, metrics.Stroke);
            if (settings.InnerBorderWidth > 0 && !settings.InnerBorderColor.IsEmpty && settings.InnerBorderColor.A > 0)
            {
                int limit = Math.Max(0, (Math.Min(metrics.Panel.Width, metrics.Panel.Height) - 1) / 2);
                int inset = Math.Clamp(metrics.Stroke + RibbonLayoutEngine.Scale(settings.InnerBorderInset, context.DeviceDpi), 0, limit);
                _rounded.Draw(context.Graphics, Rectangle.Inflate(metrics.Panel, -inset, -inset), Math.Max(0, metrics.Radius - inset), new(Color.Transparent),
                    settings.InnerBorderColor, RibbonLayoutEngine.Scale(settings.InnerBorderWidth, context.DeviceDpi));
            }
        }
        finally { context.Graphics.Restore(saved); }
    }
}
