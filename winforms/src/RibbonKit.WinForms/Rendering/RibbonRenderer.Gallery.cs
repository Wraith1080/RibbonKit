using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

/// <summary>Identifies a measured in-ribbon gallery utility button.</summary>
public enum RibbonGalleryStripButton
{
    /// <summary>Moves to the previous realized tile row.</summary>
    Up,
    /// <summary>Moves to the next realized tile row.</summary>
    Down,
    /// <summary>Opens the source gallery's full dropdown.</summary>
    Expand
}

public partial class RibbonRenderer
{
    /// <summary>Paints the library-owned in-ribbon gallery frame without altering its tile viewport or state.</summary>
    public virtual void DrawGalleryStripFrame(RibbonPaintContext context) => DrawChrome(context with { Hot = false, Pressed = false, Focused = false }, new(context.Theme.Surface), context.Theme.PopupBorder);
    /// <summary>Paints one in-ribbon gallery row-navigation or expansion button in shared measured bounds.</summary>
    public virtual void DrawGalleryStripButton(RibbonPaintContext context, RibbonGalleryStripButton button, bool enabled)
    {
        context = EnabledContext(context, enabled);
        if (context.Hot || context.Pressed) DrawChrome(context, context.Pressed ? context.Theme.Pressed : context.Theme.Hover, Color.Transparent);
        var color = enabled ? context.Theme.Foreground : context.Theme.DisabledColor;
        var glyph = context.Bounds;
        if (button == RibbonGalleryStripButton.Expand)
        {
            int height = Math.Min(glyph.Height, RibbonLayoutEngine.Scale(18, context.DeviceDpi));
            glyph = new(glyph.Left, glyph.Top + (glyph.Height - height) / 2, glyph.Width, height);
        }
        RibbonArrowPainter.DrawVertical(context.Graphics, glyph, context.DeviceDpi, color, button == RibbonGalleryStripButton.Up);
        if (button == RibbonGalleryStripButton.Expand)
        {
            int inset = RibbonLayoutEngine.Scale(4, context.DeviceDpi);
            using var pen = new Pen(color);
            context.Graphics.DrawLine(pen, glyph.Left + inset, glyph.Top + inset, glyph.Right - inset, glyph.Top + inset);
        }
    }
    /// <summary>Paints a noninteractive heading above a consecutive group of gallery tiles.</summary>
    public virtual void DrawGalleryHeading(RibbonPaintContext context, string text)
    {
        context = context with { Hot = false, Pressed = false, Focused = false };
        int inset = RibbonLayoutEngine.Scale(6, context.DeviceDpi);
        using var font = new Font(context.Font, context.Font.Style | FontStyle.Bold);
        DrawText(context with { Bounds = Rectangle.Inflate(context.Bounds, -inset, 0), Font = font }, text, context.Theme.SecondaryText,
            TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
    }
    /// <summary>Paints an owned gallery tile using its borrowed artwork, selection and independent preview/focus state. Bounds are measured device pixels.</summary>
    public virtual void DrawGalleryTile(RibbonPaintContext context, RibbonGalleryTile tile, bool selected)
    {
        bool enabled = tile.EffectiveEnabled;
        context = EnabledContext(context, enabled);
        if (selected || context.Hot || context.Pressed)
            DrawChrome(context, context.Pressed ? context.Theme.Pressed : context.Hot ? context.Theme.Hover : context.Theme.Checked,
                selected ? context.Theme.Accent : Color.Transparent);
        int S(int value) => RibbonLayoutEngine.Scale(value, context.DeviceDpi);
        var bounds = context.Bounds;
        int textHeight = Math.Min(2 * context.Font.Height, Math.Max(0, bounds.Height - S(24)));
        var label = new Rectangle(bounds.Left + S(4), bounds.Bottom - S(4) - textHeight, Math.Max(1, bounds.Width - S(8)), textHeight);
        var artwork = Rectangle.FromLTRB(bounds.Left + S(8), bounds.Top + S(6), bounds.Right - S(8), Math.Max(bounds.Top + S(6), label.Top - S(4)));
        var color = enabled ? context.Theme.Foreground : context.Theme.DisabledColor;
        if (artwork.Width > 0 && artwork.Height > 0)
        {
            if (tile.Icon is not null) DrawItemImage(context with { Bounds = artwork }, tile, RibbonItemSize.Large);
            else if ((tile.LargeImage ?? tile.Image) is { } image)
            {
                double scale = Math.Min((double)artwork.Width / image.Width, (double)artwork.Height / image.Height);
                var size = new Size(Math.Max(1, (int)Math.Round(image.Width * scale)), Math.Max(1, (int)Math.Round(image.Height * scale)));
                var destination = new Rectangle(artwork.Left + (artwork.Width - size.Width) / 2, artwork.Top + (artwork.Height - size.Height) / 2, size.Width, size.Height);
                DrawItemImage(context with { Bounds = destination }, tile, RibbonItemSize.Large);
            }
        }
        DrawText(context with { Bounds = label }, tile.Text, color,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
        if (context.Focused) DrawFocus(context);
    }
}
