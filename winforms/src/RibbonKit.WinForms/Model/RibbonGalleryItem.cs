using System.ComponentModel;

namespace RibbonKit.WinForms;

/// <summary>Describes a gallery tile temporarily previewed by pointer or keyboard navigation.</summary>
public sealed class RibbonGalleryPreviewEventArgs : EventArgs
{
    /// <summary>Initializes preview data for an owned gallery tile.</summary>
    public RibbonGalleryPreviewEventArgs(RibbonGalleryTile item) => Item = item ?? throw new ArgumentNullException(nameof(item));
    /// <summary>Gets the previewed tile. The gallery still owns the tile and its application assets remain borrowed.</summary>
    public RibbonGalleryTile Item { get; }
}

/// <summary>A native gallery with source-owned selection, separate temporary preview and optional in-ribbon presentation.</summary>
[DefaultEvent(nameof(SelectionCommitted))]
public sealed class RibbonGalleryItem : RibbonDropDownItem
{
    private RibbonGalleryTile? _selectedItem, _previewItem;
    private object? _previewOwner;
    private bool _isInRibbon;
    private int _inRibbonColumnCount = 3;
    private int _columnCount = 4, _tileWidth = 80, _tileHeight = 72, _maxVisibleRows = 4;
    /// <summary>Gets the owned tile declarations. Only RibbonGalleryTile entries are permitted.</summary>
    [Category("Data"), DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    [Editor("RibbonKit.WinForms.Design.GalleryTileCollectionEditor, RibbonKit.WinForms.Design", "System.Drawing.Design.UITypeEditor, System.Drawing")]
    public new RibbonItemCollection Items => base.Items;
    /// <summary>Gets or sets whether Large presentations use an in-ribbon tile strip. Medium/Small presentations remain dropdown commands. Defaults to false.</summary>
    [Category("Appearance"), DefaultValue(false), Bindable(true)]
    public bool IsInRibbon
    {
        get => _isInRibbon;
        set { Ribbon?.VerifyAccess(); if (_isInRibbon == value) return; _isInRibbon = value; Changed(); IsInRibbonChanged?.Invoke(this, EventArgs.Empty); }
    }
    /// <summary>Occurs after IsInRibbon changes.</summary>
    public event EventHandler? IsInRibbonChanged;
    /// <summary>Gets or sets the visible columns in the single-row in-ribbon strip, from 1 to 12. Popup ColumnCount remains independent.</summary>
    [Category("Layout"), DefaultValue(3)]
    public int InRibbonColumnCount { get => _inRibbonColumnCount; set => SetMetric(ref _inRibbonColumnCount, value, 1, 12); }
    /// <summary>Gets or selects an owned tile, or null for no selection. Programmatic changes raise SelectedItemChanged, never SelectionCommitted. Removing the selected tile clears selection; hiding/disabling it retains authored selection.</summary>
    [Category("Data"), Bindable(true), DefaultValue(null)]
    public RibbonGalleryTile? SelectedItem
    {
        get => _selectedItem;
        set
        {
            Ribbon?.VerifyAccess();
            if (value is not null && (value.IsDisposed || !Items.Contains(value))) throw new ArgumentException("Select a tile belonging to this gallery.", nameof(value));
            if (ReferenceEquals(_selectedItem, value)) return;
            CancelPreview();
            if (IsDisposed || value is not null && (value.IsDisposed || !Items.Contains(value))) return;
            _selectedItem = value;
            Changed(false); SelectedItemChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    /// <summary>Gets or sets the preferred number of popup columns, from 1 to 32. The available screen width can reduce the displayed column count.</summary>
    [Category("Layout"), DefaultValue(4)]
    public int ColumnCount { get => _columnCount; set => SetMetric(ref _columnCount, value, 1, 32); }
    /// <summary>Gets or sets tile width in 96-DPI logical pixels, from 32 to 512. Narrow screens may constrain displayed width.</summary>
    [Category("Layout"), DefaultValue(80)]
    public int TileWidth { get => _tileWidth; set => SetMetric(ref _tileWidth, value, 32, 512); }
    /// <summary>Gets or sets minimum tile height in 96-DPI logical pixels, from 32 to 512. Larger fonts can increase displayed height.</summary>
    [Category("Layout"), DefaultValue(72)]
    public int TileHeight { get => _tileHeight; set => SetMetric(ref _tileHeight, value, 32, 512); }
    /// <summary>Gets or sets the preferred maximum visible tile rows, from 1 to 32. Group headings and the screen work area also constrain the scrollable viewport.</summary>
    [Category("Layout"), DefaultValue(4)]
    public int MaxVisibleRows { get => _maxVisibleRows; set => SetMetric(ref _maxVisibleRows, value, 1, 32); }
    private void SetMetric(ref int field, int value, int minimum, int maximum)
    {
        Ribbon?.VerifyAccess();
        if (value < minimum || value > maximum) throw new ArgumentOutOfRangeException(nameof(value));
        if (field == value) return;
        field = value; Changed();
    }
    /// <summary>Occurs after authored selection changes, including selected-tile removal. Preview never changes selection.</summary>
    public event EventHandler? SelectedItemChanged;
    /// <summary>Occurs once for an accepted tile activation, including activation of the already selected tile. Read SelectedItem for the accepted value.</summary>
    public event EventHandler? SelectionCommitted;
    /// <summary>Occurs when an available tile is temporarily previewed. The application owns any document preview and rollback.</summary>
    public event EventHandler<RibbonGalleryPreviewEventArgs>? ItemPreview;
    /// <summary>Occurs once when an active preview is cancelled by leaving, dismissal, mutation or programmatic selection. Restore the application presentation from SelectedItem. Successful commit ends preview without cancellation.</summary>
    public event EventHandler? ItemPreviewCancelled;
    internal RibbonGalleryTile? PreviewItem => _previewItem;
    internal bool Available(RibbonGalleryTile tile) => !IsDisposed && Visible && EffectiveEnabled && !tile.IsDisposed && tile.Visible && tile.EffectiveEnabled && Items.Contains(tile);
    internal void Preview(RibbonGalleryTile? tile, object? owner = null)
    {
        if (tile is not null && !Available(tile)) tile = null;
        if (tile is null) { CancelPreview(owner); return; }
        if (ReferenceEquals(_previewItem, tile) && ReferenceEquals(_previewOwner, owner)) return;
        _previewItem = tile;
        _previewOwner = owner;
        ItemPreview?.Invoke(this, new(tile));
    }
    internal void CancelPreview(object? owner = null)
    {
        if (_previewItem is null || owner is not null && !ReferenceEquals(owner, _previewOwner)) return;
        _previewItem = null; _previewOwner = null; ItemPreviewCancelled?.Invoke(this, EventArgs.Empty);
    }
    internal void Reconcile()
    {
        if (_previewItem is { } preview && !Available(preview)) CancelPreview();
        if (_selectedItem is { } selected && (selected.IsDisposed || !Items.Contains(selected))) SelectedItem = null;
    }
    internal void Commit(RibbonGalleryTile tile)
    {
        Ribbon?.VerifyAccess();
        if (!Available(tile)) return;
        _previewItem = null; _previewOwner = null;
        SelectedItem = tile;
        if (!IsDisposed && Available(tile) && ReferenceEquals(_selectedItem, tile)) SelectionCommitted?.Invoke(this, EventArgs.Empty);
    }
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing) CancelPreview();
        base.Dispose(disposing);
    }
}

/// <summary>A selectable gallery tile. Text, images, vector icon, availability and bindings use normal ribbon-item conventions; assets remain application-owned.</summary>
public sealed class RibbonGalleryTile : RibbonItem
{
    private string _groupName = "";
    /// <summary>Gets or sets an optional group heading. Consecutive visible tiles with the same heading share a group; authored order is preserved.</summary>
    [Category("Appearance"), Localizable(true), DefaultValue("")]
    public string GroupName
    {
        get => _groupName;
        set { Ribbon?.VerifyAccess(); value ??= ""; if (_groupName == value) return; _groupName = value; Changed(); }
    }
    /// <summary>Commits this available tile through its owning gallery. Programmatic SelectedItem assignment remains separate from activation.</summary>
    public override void PerformClick() { if (Parent is RibbonGalleryItem gallery) gallery.Commit(this); }
}
