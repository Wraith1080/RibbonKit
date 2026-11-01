using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RibbonKit.WinForms;

/// <summary>Chooses the surface opened by the ribbon's single File entry.</summary>
public enum RibbonFileMenuMode
{
    /// <summary>Uses the existing FileMenu command dropdown.</summary>
    CommandMenu,
    /// <summary>Uses the owned Backstage navigation and native pages.</summary>
    Backstage
}

/// <summary>Owns main and bottom Backstage navigation models. Explicit page Content remains borrowed; ContentType-created controls are page-owned.</summary>
public sealed class RibbonBackstage : RibbonElement
{
    private RibbonBackstagePageItem? _selectedPage;
    private int _fallbackIndex = -1;
    private int? _navigationWidth;
    private int _minimumNavigationWidth = 160, _maximumNavigationWidth = 320;
    private float _navigationBackgroundOpacity = 1;
    /// <summary>Gets or sets navigation background coverage from 0 to 1. Default 1 is opaque. Lower values expose an active RibbonForm backdrop; text, icons and native page content retain their opacity. Solid/unsupported hosts retain an opaque navigation surface.</summary>
    [Category("Appearance"), Bindable(true), DefaultValue(1f)]
    public float NavigationBackgroundOpacity
    {
        get => _navigationBackgroundOpacity;
        set
        {
            Ribbon?.VerifyAccess();
            if (!float.IsFinite(value) || value < 0 || value > 1) throw new ArgumentOutOfRangeException(nameof(value));
            if (_navigationBackgroundOpacity == value) return;
            _navigationBackgroundOpacity = value; Changed(false); NavigationBackgroundOpacityChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    /// <summary>Occurs after NavigationBackgroundOpacity changes.</summary>
    public event EventHandler? NavigationBackgroundOpacityChanged;
    /// <summary>Initializes an empty native Backstage declaration.</summary>
    public RibbonBackstage() { Items = new(this); FooterItems = new(this); }
    /// <summary>Gets the owned main page, button and decorative separator entries, in navigation order.</summary>
    [Category("Data"), DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    [Editor("RibbonKit.WinForms.Design.BackstageCollectionEditor, RibbonKit.WinForms.Design", "System.Drawing.Design.UITypeEditor, System.Drawing")]
    public RibbonItemCollection Items { get; }
    /// <summary>Gets the owned bottom-aligned page, button and decorative separator entries. Empty by default. Main entries scroll above this section; when both sections cannot fit, they share one scrollable navigation list. Order within each collection is preserved.</summary>
    [Category("Data"), DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    [Editor("RibbonKit.WinForms.Design.BackstageCollectionEditor, RibbonKit.WinForms.Design", "System.Drawing.Design.UITypeEditor, System.Drawing")]
    public RibbonItemCollection FooterItems { get; }
    internal IEnumerable<RibbonItem> NavigationItems => Items.Concat(FooterItems);
    internal bool Contains(RibbonItem item) => Items.Contains(item) || FooterItems.Contains(item);
    /// <summary>Gets or sets the preferred navigation width in 96-DPI logical pixels. Null selects Auto, measured from visible captions, icon space and padding. Available page space and the minimum/maximum limits constrain display without changing this value. Use Reset in the designer to restore Auto.</summary>
    [Category("Layout"), Bindable(true), DefaultValue(null)]
    [TypeConverter(typeof(RibbonNavigationWidthConverter))]
    public int? NavigationWidth
    {
        get => _navigationWidth;
        set
        {
            Ribbon?.VerifyAccess();
            if (value is <= 0) throw new ArgumentOutOfRangeException(nameof(value));
            if (_navigationWidth == value) return;
            _navigationWidth = value; Changed(); NavigationWidthChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    /// <summary>Gets or sets the preferred minimum navigation width in 96-DPI logical pixels. Must be at least 80; defaults to 160. Page-space constraints can reduce the display below this minimum.</summary>
    [Category("Layout"), Bindable(true), DefaultValue(160)]
    public int MinimumNavigationWidth
    {
        get => _minimumNavigationWidth;
        set
        {
            Ribbon?.VerifyAccess();
            if (value < 80) throw new ArgumentOutOfRangeException(nameof(value));
            if (_minimumNavigationWidth == value) return;
            _minimumNavigationWidth = value; Changed(); MinimumNavigationWidthChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    /// <summary>Gets or sets the preferred maximum navigation width in 96-DPI logical pixels. Must be at least 80; defaults to 320. A larger MinimumNavigationWidth takes precedence, allowing designer properties to be assigned in either order.</summary>
    [Category("Layout"), Bindable(true), DefaultValue(320)]
    public int MaximumNavigationWidth
    {
        get => _maximumNavigationWidth;
        set
        {
            Ribbon?.VerifyAccess();
            if (value < 80) throw new ArgumentOutOfRangeException(nameof(value));
            if (_maximumNavigationWidth == value) return;
            _maximumNavigationWidth = value; Changed(); MaximumNavigationWidthChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    /// <summary>Occurs after NavigationWidth changes, including an Auto reset.</summary>
    public event EventHandler? NavigationWidthChanged;
    /// <summary>Occurs after MinimumNavigationWidth changes.</summary>
    public event EventHandler? MinimumNavigationWidthChanged;
    /// <summary>Occurs after MaximumNavigationWidth changes.</summary>
    public event EventHandler? MaximumNavigationWidthChanged;
    /// <summary>Gets or selects an available owned page. Failed native page validation keeps the current selection. Commands never become selected pages.</summary>
    [Bindable(true), Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public RibbonBackstagePageItem? SelectedPage
    {
        get => _selectedPage;
        set
        {
            Ribbon?.VerifyAccess();
            if (value is not null && !Available(value)) throw new ArgumentException("Select an available page belonging to this Backstage.", nameof(value));
            if (ReferenceEquals(value, _selectedPage) || Ribbon?.ValidateBackstagePage() == false) return;
            SetSelection(value);
        }
    }
    /// <summary>Occurs after the selected page changes, including availability fallback.</summary>
    public event EventHandler? SelectedPageChanged;
    /// <summary>Occurs after FileMenu.DropDownOpening; cancellation prevents Backstage opening.</summary>
    public event CancelEventHandler? Opening;
    /// <summary>Occurs after Backstage is visible and initial navigation focus is established.</summary>
    public event EventHandler? Opened;
    /// <summary>Occurs before a requested close, after page validation. Cancellation retains Backstage. Forced teardown does not raise this event.</summary>
    public event CancelEventHandler? Closing;
    /// <summary>Occurs after Backstage closes, including forced availability or lifetime teardown.</summary>
    public event EventHandler? Closed;
    internal bool Available(RibbonBackstagePageItem page) => Contains(page) && !page.IsDisposed && page.Visible && page.EffectiveEnabled;
    internal void Reconcile()
    {
        if (_selectedPage is null || !Available(_selectedPage))
        {
            var entries = NavigationItems.ToArray();
            int index = _fallbackIndex >= 0 ? _fallbackIndex : _selectedPage is null ? 0 : Math.Max(0, Array.IndexOf(entries, _selectedPage));
            _fallbackIndex = -1;
            SetSelection(entries.Skip(index).OfType<RibbonBackstagePageItem>().FirstOrDefault(Available)
                ?? entries.Take(index).OfType<RibbonBackstagePageItem>().LastOrDefault(Available));
        }
    }
    internal void Removing(RibbonElement item, int index) { if (ReferenceEquals(item, _selectedPage)) _fallbackIndex = Math.Max(0, Array.IndexOf(NavigationItems.ToArray(), item)); }
    private void SetSelection(RibbonBackstagePageItem? page)
    {
        if (ReferenceEquals(page, _selectedPage)) return;
        _selectedPage = page;
        Ribbon?.RefreshBackstage();
        SelectedPageChanged?.Invoke(this, EventArgs.Empty);
    }
    internal bool CanOpen() { var e = new CancelEventArgs(); Opening?.Invoke(this, e); return !e.Cancel; }
    internal bool CanClose() { var e = new CancelEventArgs(); Closing?.Invoke(this, e); return !e.Cancel; }
    internal void OnOpened() => Opened?.Invoke(this, EventArgs.Empty);
    internal void OnClosed() => Closed?.Invoke(this, EventArgs.Empty);
    internal override IEnumerable<RibbonElement> Children => NavigationItems;
}

/// <summary>A Backstage page with borrowed native Content or an optional, page-owned UserControl selected through ContentType.</summary>
public sealed class RibbonBackstagePageItem : RibbonItem
{
    private static readonly ConditionalWeakTable<Control, RibbonBackstagePageItem> Claims = new();
    private Control? _content;
    private Type? _contentType;
    private bool _ownsContent, _creatingContent, _releasingContent;
    /// <summary>Gets the realized content or assigns borrowed native content when ContentType is null. Borrowed content must initially be unparented and used by only one page; the application owns its disposal. ContentType-created content is retained across page switches and disposed by the page. Do not reparent content while assigned.</summary>
    [Category("Data")]
    public Control? Content
    {
        get => _content;
        set
        {
            Ribbon?.VerifyAccess();
            if (ReferenceEquals(value, _content)) return;
            if (_contentType is not null) throw new InvalidOperationException("Clear ContentType before assigning borrowed Content.");
            if (value is not null)
            {
                ObjectDisposedException.ThrowIf(value.IsDisposed, value);
                if (value.Parent is not null || value is Form { TopLevel: true } || value is RibbonControl || Claims.TryGetValue(value, out _))
                    throw new ArgumentException("Backstage content must be an unparented native Control used by only one page.", nameof(value));
            }
            ReleaseContent();
            _content = value;
            if (value is not null) { Claims.Add(value, this); value.Disposed += ContentDisposed; }
            Changed();
            ContentChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    private bool ShouldSerializeContent() => _contentType is null && _content is not null;
    private void ResetContent() => Content = null;

    /// <summary>Gets or selects a public, concrete UserControl type with a public parameterless constructor. Null preserves application-owned Content. All selected page types are created during runtime ribbon initialization before Form.Load, reused across page switches, and disposed on type replacement/reset or page disposal. Types assigned after initialization are created immediately, or at the end of an active update batch. Clear borrowed Content before selecting a type. Designer previews never create it.</summary>
    [Category("Data"), DefaultValue(null)]
    public Type? ContentType
    {
        get => _contentType;
        set
        {
            Ribbon?.VerifyAccess();
            if (_contentType == value) return;
            if (value is not null && !IsContentType(value)) throw new ArgumentException("Choose a public, concrete UserControl with a public parameterless constructor.", nameof(value));
            if (!_ownsContent && _content is not null) throw new InvalidOperationException("Clear application-owned Content before selecting ContentType.");
            if (_creatingContent) throw new InvalidOperationException("ContentType cannot change while its UserControl is being created.");
            ReleaseContent();
            _contentType = value;
            Changed();
            ContentTypeChanged?.Invoke(this, EventArgs.Empty);
            ContentChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    /// <summary>Occurs after ContentType changes.</summary>
    public event EventHandler? ContentTypeChanged;
    internal static bool IsContentType(Type type) => typeof(UserControl).IsAssignableFrom(type) && type.IsVisible && !type.IsAbstract
        && !type.ContainsGenericParameters && type.GetConstructor(Type.EmptyTypes) is not null;
    internal void EnsureContent()
    {
        if (_contentType is null || _content is not null || _releasingContent || Ribbon?.IsInDesignMode == true || Site?.DesignMode == true || IsDisposed) return;
        if (_creatingContent) throw new InvalidOperationException("Recursive Backstage content creation is not supported.");
        _creatingContent = true;
        try
        {
            var control = (UserControl)Activator.CreateInstance(_contentType)!;
            if (control.IsDisposed || control.Parent is not null || Claims.TryGetValue(control, out _))
            { control.Dispose(); throw new InvalidOperationException("A page UserControl constructor must return a live, unparented control."); }
            _content = control; _ownsContent = true; Claims.Add(control, this); control.Disposed += ContentDisposed;
        }
        finally { _creatingContent = false; }
        ContentChanged?.Invoke(this, EventArgs.Empty);
    }
    /// <summary>Occurs after Content changes or the application disposes it.</summary>
    public event EventHandler? ContentChanged;
    private void ContentDisposed(object? sender, EventArgs e)
    {
        ReleaseContent(); Changed(); ContentChanged?.Invoke(this, EventArgs.Empty);
    }
    private void ReleaseContent()
    {
        if (_content is not { } old) return;
        old.Disposed -= ContentDisposed;
        Ribbon?.DetachBackstageContent(old);
        Claims.Remove(old);
        _content = null;
        bool owned = _ownsContent; _ownsContent = false;
        if (owned && !old.IsDisposed && !old.Disposing)
        {
            _releasingContent = true;
            try { old.Dispose(); }
            finally { _releasingContent = false; }
        }
    }
    /// <summary>Selects this available page through its Backstage owner, preserving native validation.</summary>
    public override void PerformClick()
    {
        Ribbon?.VerifyAccess();
        if (Parent is RibbonBackstage backstage && backstage.Available(this)) backstage.SelectedPage = this;
    }
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing) ReleaseContent();
        base.Dispose(disposing);
    }
}
