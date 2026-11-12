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
    public virtual void DrawGalleryStripFrame(RibbonPaintContext context) => RibbonGalleryRenderer.DrawStripFrame(this, context);
    /// <summary>Paints one in-ribbon gallery row-navigation or expansion button in shared measured bounds.</summary>
    public virtual void DrawGalleryStripButton(RibbonPaintContext context, RibbonGalleryStripButton button, bool enabled)
    {
        context = EnabledContext(context, enabled);
        RibbonGalleryContentRenderer.DrawUtility(this, context, button, enabled);
    }
    /// <summary>Paints a noninteractive heading above a consecutive group of gallery tiles.</summary>
    public virtual void DrawGalleryHeading(RibbonPaintContext context, string text)
    {
        if (context.IsMenuHeading) { RibbonMenuDecorationRenderer.DrawHeading(this, context, text, galleryFallback: false); return; }
        RibbonGalleryRenderer.DrawHeading(this, context, text, office: false);
    }
    /// <summary>Paints an owned gallery tile using its borrowed artwork, selection and independent preview/focus state. Bounds are measured device pixels.</summary>
    public virtual void DrawGalleryTile(RibbonPaintContext context, RibbonGalleryTile tile, bool selected)
    {
        bool enabled = tile.EffectiveEnabled;
        context = EnabledContext(context, enabled);
        RibbonGalleryRenderer.DrawTileState(this, context, selected);
        RibbonGalleryContentRenderer.DrawTile(this, context, tile);
    }
    internal void PaintGalleryImage(RibbonPaintContext context, RibbonGalleryTile tile) => DrawItemImage(context, tile, RibbonItemSize.Large);
}
