namespace RibbonKit.WinForms;

/// <summary>Chooses a row or page scroll in a custom dropdown gallery's own layout.</summary>
public enum RibbonGalleryScroll
{
    /// <summary>Scrolls one row toward the beginning.</summary>
    RowBackward,
    /// <summary>Scrolls one row toward the end.</summary>
    RowForward,
    /// <summary>Scrolls one viewport toward the beginning.</summary>
    PageBackward,
    /// <summary>Scrolls one viewport toward the end.</summary>
    PageForward
}

/// <summary>Implemented by an application UserControl selected through DropDownPresenterType or returned by DropDownPresenterFactory. RibbonKit owns the view and navigation; the presenter owns its item layout and drawing. Root-control pointer input is routed by the library; native child pointer handlers use context.Preview, FocusItem and Commit instead of directly executing tiles. All coordinates are client pixels of that control, including its current scroll offset. Methods run on the owning UI thread.</summary>
public interface IRibbonGalleryPresenter
{
    /// <summary>Refreshes drawing/layout from the authoritative source and display context. Do not commit, execute or create a second selection model here; context input requests are rejected during synchronization.</summary>
    void Synchronize(RibbonGalleryPresenterContext context);
    /// <summary>Gets a source tile's current client bounds, including scrolling, or Empty when it has no presentation.</summary>
    Rectangle GetItemBounds(RibbonGalleryTile item);
    /// <summary>Finds the source tile at a control-client point, or null. The library revalidates ownership, visibility and availability.</summary>
    RibbonGalleryTile? HitTest(Point location);
    /// <summary>Resolves arrows, Home/End, PageUp/PageDown and Tab/Shift+Tab in the actual item layout. Return null if there is no available destination. Do not commit or preview here.</summary>
    RibbonGalleryTile? Navigate(RibbonGalleryTile? current, Keys key);
    /// <summary>Scrolls an available source tile into view without changing selection.</summary>
    void Reveal(RibbonGalleryTile item);
    /// <summary>Gets whether the view can scroll toward its beginning.</summary>
    bool CanScrollBackward { get; }
    /// <summary>Gets whether the view can scroll toward its end.</summary>
    bool CanScrollForward { get; }
    /// <summary>Scrolls within the view. Report asynchronous/internal layout changes through context.NotifyViewChanged.</summary>
    void ScrollView(RibbonGalleryScroll amount);
}

/// <summary>Connects one owned dropdown-gallery view to its original model. Preview, activation and bounds notifications are accepted only while this popup is current. The source, font, renderer and application assets are borrowed.</summary>
public sealed class RibbonGalleryPresenterContext
{
    private RibbonPopupSurface? _surface;
    internal RibbonGalleryPresenterContext(RibbonPopupSurface surface, RibbonGalleryItem gallery, RibbonControl ribbon)
    {
        _surface = surface; Gallery = gallery; Theme = ribbon.Theme; Renderer = ribbon.Painter;
        Font = ribbon.Font; Density = ribbon.Density; Dpi = ribbon.DeviceDpi;
    }
    /// <summary>Gets the original gallery, including owned items and authoritative SelectedItem. Never clone its tiles or independently execute their actions.</summary>
    public RibbonGalleryItem Gallery { get; }
    /// <summary>Gets the current immutable ribbon theme.</summary>
    public RibbonTheme Theme { get; }
    /// <summary>Gets the borrowed shared renderer for optional themed tile/heading drawing.</summary>
    public RibbonRenderer Renderer { get; }
    /// <summary>Gets the borrowed ribbon font.</summary>
    public Font Font { get; }
    /// <summary>Gets the ribbon's input density.</summary>
    public RibbonDensity Density { get; }
    /// <summary>Gets the presentation DPI. Use this when measuring before native parenting.</summary>
    public int Dpi { get; }
    /// <summary>Gets the constrained body viewport in physical client pixels, excluding library scroll buttons.</summary>
    public Size Viewport { get; internal set; }
    /// <summary>Gets the keyboard-focused source tile, independent of committed selection.</summary>
    public RibbonGalleryTile? FocusedItem { get; internal set; }
    /// <summary>Gets whether this context still belongs to a current open popup.</summary>
    public bool IsActive => _surface?.IsCurrentPresenter(this) == true;
    /// <summary>Requests temporary preview of an available owned tile, or cancels this view's preview with null. Returns false for a stale context or unavailable tile.</summary>
    public bool Preview(RibbonGalleryTile? item) => _surface?.PresenterPreview(this, item) == true;
    /// <summary>Requests keyboard focus and reveal for an available source tile without selecting or previewing it. Use this when a native child interaction identifies a tile. Returns false for an unavailable tile or stale context.</summary>
    public bool FocusItem(RibbonGalleryTile item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return _surface?.PresenterFocus(this, item) == true;
    }
    /// <summary>Requests accepted activation through the ribbon's existing validation, commit and dismissal path. Returns false for a stale context or unavailable tile.</summary>
    public bool Commit(RibbonGalleryTile item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return _surface?.PresenterCommit(this, item) == true;
    }
    /// <summary>Reports changed item bounds or scroll availability. This never changes gallery selection or customization persistence.</summary>
    public void NotifyViewChanged() => _surface?.PresenterViewChanged(this);
    internal void Detach() => _surface = null;
}
