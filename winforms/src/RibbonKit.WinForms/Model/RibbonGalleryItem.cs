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
    private bool _useDropDownPresenterInQuickAccess;
    private int _inRibbonColumnCount = 3;
    private int _columnCount = 4, _tileWidth = 80, _tileHeight = 72, _maxVisibleRows = 4;
    private Func<Control>? _dropDownPresenterFactory;
    private Type? _dropDownPresenterType;
    private Func<Control>? _inRibbonPresenterFactory;
    private Type? _inRibbonPresenterType;
    /// <summary>Gets or selects a public concrete UserControl implementing IRibbonGalleryPresenter with a public parameterless constructor for inline strip slots. Creation is lazy. RibbonKit retains the ordinary ribbon view across hiding and creates separate session-owned views for collapsed-group, minimized-tab and QAT-group bodies. Views are never moved between hosts. Replacement, final source detach and ribbon disposal release owned views; closing a temporary host releases its view. Clear InRibbonPresenterFactory first. Null for both inline selectors preserves the built-in strip. Expanded QAT gallery popups use the independent dropdown selectors when UseDropDownPresenterInQuickAccess is true.</summary>
    [Browsable(false), Category("Data"), DefaultValue(null)]
    [Description("Custom inline gallery UserControl class, independent of the dropdown presenter. Body authoring stays in the ordinary UserControl designer; inline class discovery is a later designer increment.")]
    public Type? InRibbonPresenterType
    {
        get => _inRibbonPresenterType;
        set
        {
            Ribbon?.VerifyAccess(); if (_inRibbonPresenterType == value) return;
            if (value is not null && !IsPresenterType(value)) throw new ArgumentException("Choose a public concrete UserControl implementing IRibbonGalleryPresenter with a public parameterless constructor.", nameof(value));
            if (value is not null && _inRibbonPresenterFactory is not null) throw new InvalidOperationException("Clear InRibbonPresenterFactory before selecting InRibbonPresenterType.");
            _inRibbonPresenterType = value; Changed(); InRibbonPresenterTypeChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    /// <summary>Occurs after the optional inline gallery presenter class changes.</summary>
    public event EventHandler? InRibbonPresenterTypeChanged;
    /// <summary>Gets or sets an optional runtime factory for inline gallery bodies. Return a fresh unparented UserControl implementing IRibbonGalleryPresenter for each requested slot. RibbonKit retains the ordinary ribbon view and disposes each separate temporary group/tab/QAT-group view with its popup host. Views are never moved between hosts or into an expanded popup. Clear InRibbonPresenterType first. Null for both inline selectors preserves the built-in strip. Factory delegates and view state are not serialized.</summary>
    [Browsable(false), DefaultValue(null), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Func<Control>? InRibbonPresenterFactory
    {
        get => _inRibbonPresenterFactory;
        set
        {
            Ribbon?.VerifyAccess(); if (_inRibbonPresenterFactory == value) return;
            if (value is not null && _inRibbonPresenterType is not null) throw new InvalidOperationException("Clear InRibbonPresenterType before assigning InRibbonPresenterFactory.");
            _inRibbonPresenterFactory = value; Changed(); InRibbonPresenterFactoryChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    /// <summary>Occurs after the optional inline gallery presenter factory changes.</summary>
    public event EventHandler? InRibbonPresenterFactoryChanged;
    internal bool HasCustomInRibbonPresenter => _inRibbonPresenterType is not null || _inRibbonPresenterFactory is not null;
    internal Control CreateInRibbonPresenter() => _inRibbonPresenterType is { } type ? (Control)Activator.CreateInstance(type)! : _inRibbonPresenterFactory!();
    /// <summary>Gets or sets whether gallery popups opened through Quick Access use this source's dropdown presenter selectors, including overflow and nested/group routes. Defaults to false, preserving the built-in QAT popup. This does not replace the compact QAT launcher or select an inline presenter. Changing the setting closes this source's QAT gallery popup; the next opening resolves the setting again.</summary>
    [Category("Behavior"), DefaultValue(false)]
    [Description("Use the gallery's DropDownPresenterType or DropDownPresenterFactory for popups opened through Quick Access. The toolbar launcher stays compact; null dropdown selectors still use the built-in popup.")]
    public bool UseDropDownPresenterInQuickAccess
    {
        get => _useDropDownPresenterInQuickAccess;
        set
        {
            Ribbon?.VerifyAccess(); if (_useDropDownPresenterInQuickAccess == value) return;
            _useDropDownPresenterInQuickAccess = value;
            Ribbon?.Popups.CloseQuickAccessGallery(this);
            Changed(false); UseDropDownPresenterInQuickAccessChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    /// <summary>Occurs after UseDropDownPresenterInQuickAccess changes.</summary>
    public event EventHandler? UseDropDownPresenterInQuickAccessChanged;
    /// <summary>Gets or sets an optional runtime factory for dropdown-gallery bodies. Return a fresh unparented UserControl implementing IRibbonGalleryPresenter. RibbonKit creates it after accepted opening and disposes it with that popup; reopening creates another view. Clear DropDownPresenterType before assigning a factory. Null for both dropdown selectors preserves the built-in popup. Inline selectors are independent; QAT popups use this factory only when UseDropDownPresenterInQuickAccess is true. Factory delegates and presenter state are not serialized.</summary>
    [Browsable(false), DefaultValue(null), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Func<Control>? DropDownPresenterFactory
    {
        get => _dropDownPresenterFactory;
        set
        {
            Ribbon?.VerifyAccess(); if (_dropDownPresenterFactory == value) return;
            if (value is not null && _dropDownPresenterType is not null) throw new InvalidOperationException("Clear DropDownPresenterType before assigning DropDownPresenterFactory.");
            _dropDownPresenterFactory = value; Changed(); DropDownPresenterFactoryChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    /// <summary>Occurs after the optional dropdown presenter factory changes.</summary>
    public event EventHandler? DropDownPresenterFactoryChanged;
    /// <summary>Gets or selects a public concrete UserControl implementing IRibbonGalleryPresenter with a public parameterless constructor. RibbonKit creates a fresh view after each accepted gallery opening and disposes it with the popup. Designer selection never creates the control. Clear DropDownPresenterFactory before selecting a type; null for both dropdown selectors uses the built-in popup. Inline selectors are independent; QAT popups use this class only when UseDropDownPresenterInQuickAccess is true.</summary>
    [Category("Data"), DefaultValue(null)]
    [Description("Custom dropdown gallery UserControl class. Edit its body in the UserControl designer. Build the project to refresh available classes; (None) clears the class selection.")]
    public Type? DropDownPresenterType
    {
        get => _dropDownPresenterType;
        set
        {
            Ribbon?.VerifyAccess(); if (_dropDownPresenterType == value) return;
            if (value is not null && !IsPresenterType(value)) throw new ArgumentException("Choose a public concrete UserControl implementing IRibbonGalleryPresenter with a public parameterless constructor.", nameof(value));
            if (value is not null && _dropDownPresenterFactory is not null) throw new InvalidOperationException("Clear DropDownPresenterFactory before selecting DropDownPresenterType.");
            _dropDownPresenterType = value; Changed(); DropDownPresenterTypeChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    /// <summary>Occurs after the optional dropdown presenter class changes.</summary>
    public event EventHandler? DropDownPresenterTypeChanged;
    internal static bool IsPresenterType(Type type) => typeof(UserControl).IsAssignableFrom(type) && typeof(IRibbonGalleryPresenter).IsAssignableFrom(type)
        && type.IsVisible && !type.IsAbstract && !type.ContainsGenericParameters && type.GetConstructor(Type.EmptyTypes) is not null;
    internal bool HasCustomDropDownPresenter => _dropDownPresenterType is not null || _dropDownPresenterFactory is not null;
    internal Control CreateDropDownPresenter() => _dropDownPresenterType is { } type ? (Control)Activator.CreateInstance(type)! : _dropDownPresenterFactory!();
    /// <summary>Gets the owned tile declarations. Only RibbonGalleryTile entries are permitted.</summary>
    [Category("Data"), DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    [Editor("RibbonKit.WinForms.Design.GalleryTileCollectionEditor, RibbonKit.WinForms.Design", "System.Drawing.Design.UITypeEditor, System.Drawing")]
    public new RibbonItemCollection Items => base.Items;
    /// <summary>Gets or sets whether the gallery uses an in-ribbon tile strip. Medium/Small entries in SizePolicy select a full-column Large dropdown fallback; authored policy values remain intact. Defaults to false.</summary>
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
            if (Ribbon?.CommandCatalog.Structure is { Synchronizing: false } structure && !ReferenceEquals(structure.Source(this), this)
                && structure.Source(this) is RibbonGalleryItem source)
            { source.SelectedItem = value is null ? null : (RibbonGalleryTile)structure.Source(value); return; }
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
    internal RibbonGalleryTile? PreviewItem => Ribbon?.CommandCatalog.Structure is { } structure && structure.IsProxy(this) ? structure.GalleryPreview(this) : _previewItem;
    internal bool Available(RibbonGalleryTile tile) => !IsDisposed && Visible && EffectiveEnabled && !tile.IsDisposed && tile.Visible && tile.EffectiveEnabled && Items.Contains(tile);
    internal void Preview(RibbonGalleryTile? tile, object? owner = null)
    {
        if (Ribbon?.CommandCatalog.Structure is { } structure && structure.IsProxy(this) && structure.Source(this) is RibbonGalleryItem source)
        { source.Preview(tile is not null && Available(tile) ? (RibbonGalleryTile)structure.Source(tile) : null, owner); return; }
        if (tile is not null && !Available(tile)) tile = null;
        if (tile is null) { CancelPreview(owner); return; }
        if (ReferenceEquals(_previewItem, tile) && ReferenceEquals(_previewOwner, owner)) return;
        _previewItem = tile;
        _previewOwner = owner;
        ItemPreview?.Invoke(this, new(tile));
    }
    internal void CancelPreview(object? owner = null)
    {
        if (Ribbon?.CommandCatalog.Structure is { Synchronizing: false } structure && structure.IsProxy(this) && structure.Source(this) is RibbonGalleryItem source)
        { source.CancelPreview(owner); return; }
        if (_previewItem is null || owner is not null && !ReferenceEquals(owner, _previewOwner)) return;
        _previewItem = null; _previewOwner = null; ItemPreviewCancelled?.Invoke(this, EventArgs.Empty);
    }
    internal void Reconcile()
    {
        if (_previewItem is { } preview && !Available(preview)) CancelPreview();
        if (_selectedItem is { } selected && (selected.IsDisposed || !Items.Contains(selected))) SelectedItem = null;
    }
    internal bool Commit(RibbonGalleryTile tile)
    {
        if (Ribbon?.CommandCatalog.Structure is { } structure && structure.IsProxy(this) && structure.Source(this) is RibbonGalleryItem source)
        { return Available(tile) && source.Commit((RibbonGalleryTile)structure.Source(tile)); }
        Ribbon?.VerifyAccess();
        if (!Available(tile)) return false;
        _previewItem = null; _previewOwner = null;
        SelectedItem = tile;
        if (IsDisposed || !Available(tile) || !ReferenceEquals(_selectedItem, tile)) return false;
        SelectionCommitted?.Invoke(this, EventArgs.Empty); return true;
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
