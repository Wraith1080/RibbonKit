using System.Drawing.Drawing2D;
using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

/// <summary>Extensible painting over library-measured device-pixel geometry.</summary>
public class RibbonRenderer : IDisposable
{
    private readonly RoundedPainter _rounded = new();
    /// <summary>Paints the ribbon or popup background.</summary>
    public virtual void DrawSurface(RibbonPaintContext context, bool popup)
    {
        using var brush = new SolidBrush(popup ? context.Theme.Surface : context.Theme.Background);
        context.Graphics.FillRectangle(brush, context.Bounds);
    }
    /// <summary>Paints a tab using library-measured bounds.</summary>
    public virtual void DrawTab(RibbonPaintContext context, RibbonTab tab, bool selected)
    {
        if (selected || context.Hot)
            DrawRounded(context, selected ? new(context.Theme.Surface) : context.Theme.Hover, Color.Transparent);
        TextRenderer.DrawText(context.Graphics, tab.Text, context.Font, context.Bounds, tab.EffectiveEnabled ? context.Theme.Foreground : SystemColors.GrayText, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
        if (selected)
        {
            using var pen = new Pen(context.Theme.Accent, Math.Max(2, context.DeviceDpi / 48));
            context.Graphics.DrawLine(pen, context.Bounds.Left + 8, context.Bounds.Bottom - 2, context.Bounds.Right - 8, context.Bounds.Bottom - 2);
        }
        if (context.Focused)
            DrawFocus(context);
    }
    /// <summary>Paints the group caption or collapsed opener.</summary>
    public virtual void DrawGroup(RibbonPaintContext context, RibbonGroup group, bool collapsed)
    {
        if (collapsed)
        {
            if (context.Hot || context.Pressed)
                DrawRounded(context, context.Pressed ? context.Theme.Pressed : context.Theme.Hover, context.Theme.Border);
            var bounds = Rectangle.Inflate(context.Bounds, -5, -5);
            TextRenderer.DrawText(context.Graphics, group.Text + "  ▾", context.Font, bounds, group.EffectiveEnabled ? context.Theme.Foreground : SystemColors.GrayText, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);
            if (context.Focused)
                DrawFocus(context);
        }
        else
        {
            int height = Math.Max(context.Font.Height + RibbonLayoutEngine.Scale(6, context.DeviceDpi), RibbonLayoutEngine.Scale(20, context.DeviceDpi));
            var caption = new Rectangle(context.Bounds.X + 4, context.Bounds.Bottom - height, Math.Max(0, context.Bounds.Width - 8), height);
            TextRenderer.DrawText(context.Graphics, group.Text, context.Font, caption, context.Theme.SecondaryText, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            using var pen = new Pen(context.Theme.Border);
            context.Graphics.DrawLine(pen, context.Bounds.Right - 1, context.Bounds.Top + 6, context.Bounds.Right - 1, context.Bounds.Bottom - 6);
        }
    }
    /// <summary>Paints a command using library-measured bounds and shared content alignment.</summary>
    public virtual void DrawItem(RibbonPaintContext context, RibbonItem item, RibbonItemSize size, bool menu)
    {
        bool enabled = item.EffectiveEnabled;
        bool check = item is RibbonToggleItem { Checked: true };
        if (check || context.Hot || context.Pressed)
            DrawRounded(context, context.Pressed ? context.Theme.Pressed : context.Hot ? context.Theme.Hover : context.Theme.Checked, Color.Transparent);
        bool hasIcon = item.Icon is not null || item.Image is not null || item.LargeImage is not null;
        var content = RibbonContentLayout.Calculate(context.Bounds, size, menu, context.ReserveIcon || hasIcon, context.DeviceDpi, context.Font);
        var iconBounds = content.Icon;
        var textBounds = content.Text;
        var flags = content.Flags;
        var color = enabled ? context.Theme.Foreground : SystemColors.GrayText;
        var saved = context.Graphics.Save();
        try
        {
            context.Graphics.SetClip(iconBounds, CombineMode.Intersect);
            if (item.Icon is { } icon)
                icon.Draw(context.Graphics, iconBounds, new(context.DeviceDpi, color, enabled));
            else if ((size == RibbonItemSize.Large ? item.LargeImage ?? item.Image : item.Image ?? item.LargeImage) is { } image)
            {
                if (enabled)
                    context.Graphics.DrawImage(image, iconBounds);
                else
                {
                    using var attributes = new System.Drawing.Imaging.ImageAttributes();
                    attributes.SetColorMatrix(new System.Drawing.Imaging.ColorMatrix(new float[][]
                    {
                        [0.3f, 0.3f, 0.3f, 0, 0], [0.59f, 0.59f, 0.59f, 0, 0],
                        [0.11f, 0.11f, 0.11f, 0, 0], [0, 0, 0, 0.55f, 0], [0, 0, 0, 0, 1]
                    }));
                    context.Graphics.DrawImage(image, iconBounds, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, attributes);
                }
            }
            else if (menu && check)
                TextRenderer.DrawText(context.Graphics, "✓", context.Font, iconBounds, color, TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter);
        }
        finally { context.Graphics.Restore(saved); }
        if (size != RibbonItemSize.Small || menu || !hasIcon)
            TextRenderer.DrawText(context.Graphics, item.Text + (item is RibbonDropDownItem ? "  ▾" : ""), context.Font, textBounds, color, flags);
        if (context.Focused)
            DrawFocus(context);
    }
    /// <summary>Paints a visible keyboard focus outline.</summary>
    public virtual void DrawFocus(RibbonPaintContext context)
    {
        var bounds = Rectangle.Inflate(context.Bounds, -2, -2);
        _rounded.Draw(context.Graphics, bounds, Math.Max(1, context.Theme.CornerRadius * context.DeviceDpi / 96), new(Color.Transparent), context.Theme.Accent);
    }
    /// <summary>Paints a resolved keyboard badge.</summary>
    public virtual void DrawKeyTip(RibbonPaintContext context, string keys)
    {
        _rounded.Draw(context.Graphics, context.Bounds, 2, new(Color.FromArgb(255, 255, 225)), context.Theme.Border);
        TextRenderer.DrawText(context.Graphics, keys, context.Font, context.Bounds, Color.Black, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
    }
    /// <summary>Paints a fill and outline using mirrored corner coverage.</summary>
    protected void DrawRounded(RibbonPaintContext context, RibbonFill fill, Color border) => _rounded.Draw(context.Graphics, Rectangle.Inflate(context.Bounds, -1, -1), context.Theme.CornerRadius * context.DeviceDpi / 96, fill, border, border.A == 0 ? 0 : 1);
    /// <summary>Releases renderer-owned resources; borrowed icons and images remain caller-owned.</summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    /// <summary>Releases renderer-owned resources; borrowed icons and images remain caller-owned.</summary>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
            _rounded.Dispose();
    }
}
