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

/// <summary>Owns Backstage navigation models. Applications retain ownership of all page Controls.</summary>
public sealed class RibbonBackstage : RibbonElement
{
    private RibbonBackstagePageItem? _selectedPage;
    private int _fallbackIndex = -1;
    private int? _navigationWidth;
    private int _minimumNavigationWidth = 160, _maximumNavigationWidth = 320;
    /// <summary>Initializes an empty native Backstage declaration.</summary>
    public RibbonBackstage() => Items = new(this);
    /// <summary>Gets the owned page and button entries, in navigation order.</summary>
    [Category("Data"), DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    [Editor("RibbonKit.WinForms.Design.BackstageCollectionEditor, RibbonKit.WinForms.Design", "System.Drawing.Design.UITypeEditor, System.Drawing")]
    public RibbonItemCollection Items { get; }
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
    internal bool Available(RibbonBackstagePageItem page) => Items.Contains(page) && !page.IsDisposed && page.Visible && page.EffectiveEnabled;
    internal void Reconcile()
    {
        if (_selectedPage is null || !Available(_selectedPage))
        {
            int index = _fallbackIndex >= 0 ? _fallbackIndex : _selectedPage is null ? 0 : Math.Max(0, Items.IndexOf(_selectedPage));
            _fallbackIndex = -1;
            SetSelection(Items.Skip(index).OfType<RibbonBackstagePageItem>().FirstOrDefault(Available)
                ?? Items.Take(index).OfType<RibbonBackstagePageItem>().LastOrDefault(Available));
        }
    }
    internal void Removing(RibbonElement item, int index) { if (ReferenceEquals(item, _selectedPage)) _fallbackIndex = index; }
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
    internal override IEnumerable<RibbonElement> Children => Items;
}

/// <summary>A Backstage page borrowing one initially unparented, non-top-level native Control. Do not reparent or dispose the Control while it is assigned. Release Content or dispose the page before disposing application content.</summary>
public sealed class RibbonBackstagePageItem : RibbonItem
{
    private static readonly ConditionalWeakTable<Control, RibbonBackstagePageItem> Claims = new();
    private Control? _content;
    /// <summary>Gets or assigns borrowed native content. One Control belongs to one page declaration; it keeps its library parent while pages are hidden or Backstage is closed. Release, removal and ribbon/page disposal detach without disposing content. The application owns its bindings, values, styling and disposal.</summary>
    [Category("Data"), DefaultValue(null)]
    public Control? Content
    {
        get => _content;
        set
        {
            Ribbon?.VerifyAccess();
            if (ReferenceEquals(value, _content)) return;
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
    /// <summary>Occurs after Content changes or the application disposes it.</summary>
    public event EventHandler? ContentChanged;
    private void ContentDisposed(object? sender, EventArgs e) => Content = null;
    private void ReleaseContent()
    {
        if (_content is not { } old) return;
        old.Disposed -= ContentDisposed;
        Ribbon?.DetachBackstageContent(old);
        Claims.Remove(old);
        _content = null;
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
