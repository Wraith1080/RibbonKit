using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RibbonKit.WinForms;

// A retained native body has one declaration owner, even while it is unparented.
internal static class RibbonContentOwnership
{
    private static readonly ConditionalWeakTable<Control, object> Claims = new();
    internal static bool IsClaimed(Control control) => Claims.TryGetValue(control, out _);
    internal static void Claim(Control control, object owner) => Claims.Add(control, owner);
    internal static void Release(Control control) => Claims.Remove(control);
}

public partial class RibbonDropDownItem
{
    private Control? _content;
    private Type? _contentType;
    private bool _ownsContent, _creatingContent, _releasingContent;

    /// <summary>Gets the retained popup body or assigns an unparented application-owned Control. Custom content replaces the Items presentation without changing the collection. Clear ContentType before assigning borrowed content. The application owns borrowed content disposal; do not reparent it while assigned. Galleries use their separate presenter contract.</summary>
    [Category("Data")]
    public Control? Content
    {
        get => _content;
        set
        {
            Ribbon?.VerifyAccess();
            if (ReferenceEquals(value, _content)) return;
            VerifyContentSupport();
            if (_creatingContent || _releasingContent) throw new InvalidOperationException("Content cannot change while popup content is being created or released.");
            if (_contentType is not null) throw new InvalidOperationException("Clear ContentType before assigning application-owned Content.");
            if (value is not null)
            {
                ObjectDisposedException.ThrowIf(value.IsDisposed, value);
                if (value.Parent is not null || value is Form || value is RibbonControl || RibbonContentOwnership.IsClaimed(value))
                    throw new ArgumentException("Popup content must be an unparented native Control used by only one declaration.", nameof(value));
            }
            ReleaseContent();
            _content = value;
            if (value is not null) { RibbonContentOwnership.Claim(value, this); value.Disposed += ContentDisposed; }
            Changed();
            ContentChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    private bool ShouldSerializeContent() => _contentType is null && _content is not null;
    private void ResetContent() => Content = null;

    /// <summary>Gets or selects a public, concrete UserControl with a public parameterless constructor. Ribbon initialization creates selected types before Form.Load without opening a popup. Instances are reused by source and QAT openings and disposed on replacement/reset or item disposal. Late changes initialize immediately, or after EndUpdate. Designers never construct the body. Clear borrowed Content first. The body replaces Items visually and uses its native Size plus popup padding, bounded by the monitor; the application owns its internal layout and scrolling.</summary>
    [Category("Data"), DefaultValue(null)]
    public Type? ContentType
    {
        get => _contentType;
        set
        {
            Ribbon?.VerifyAccess();
            if (_contentType == value) return;
            VerifyContentSupport();
            if (value is not null && !RibbonBackstagePageItem.IsContentType(value))
                throw new ArgumentException("Choose a public, concrete UserControl with a public parameterless constructor.", nameof(value));
            if (!_ownsContent && _content is not null) throw new InvalidOperationException("Clear application-owned Content before selecting ContentType.");
            if (_creatingContent || _releasingContent) throw new InvalidOperationException("ContentType cannot change while popup content is being created or released.");
            ReleaseContent();
            _contentType = value;
            Changed();
            ContentTypeChanged?.Invoke(this, EventArgs.Empty);
            ContentChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    /// <summary>Occurs after ContentType changes.</summary>
    public event EventHandler? ContentTypeChanged;
    /// <summary>Occurs after Content is assigned, created, released, or disposed by the application.</summary>
    public event EventHandler? ContentChanged;

    private void VerifyContentSupport()
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        if (this is RibbonGalleryItem) throw new InvalidOperationException("Use the gallery presenter properties for gallery content.");
    }
    internal void EnsureContent()
    {
        if (_contentType is null || _content is not null || _releasingContent || IsDisposed || Ribbon?.IsInDesignMode == true || Site?.DesignMode == true) return;
        if (_creatingContent) throw new InvalidOperationException("Recursive popup content creation is not supported.");
        _creatingContent = true;
        try
        {
            var body = (UserControl)Activator.CreateInstance(_contentType)!;
            if (IsDisposed || body.IsDisposed || body.Parent is not null || RibbonContentOwnership.IsClaimed(body))
            {
                if (!body.IsDisposed && body.Parent is null && !RibbonContentOwnership.IsClaimed(body)) body.Dispose();
                throw new InvalidOperationException("A popup UserControl constructor must return a live, unparented control.");
            }
            _content = body; _ownsContent = true;
            RibbonContentOwnership.Claim(body, this); body.Disposed += ContentDisposed;
        }
        finally { _creatingContent = false; }
        ContentChanged?.Invoke(this, EventArgs.Empty);
    }
    private void ContentDisposed(object? sender, EventArgs e)
    {
        ReleaseContent(); Changed(); ContentChanged?.Invoke(this, EventArgs.Empty);
    }
    private void ReleaseContent()
    {
        if (_content is not { } old) return;
        _releasingContent = true;
        try
        {
            old.Disposed -= ContentDisposed;
            _content = null;
            bool owned = _ownsContent; _ownsContent = false;
            try { Ribbon?.Popups.CloseMenu(this, false); }
            finally
            {
                RibbonContentOwnership.Release(old);
                if (owned && !old.IsDisposed && !old.Disposing) old.Dispose();
            }
        }
        finally { _releasingContent = false; }
    }
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing) ReleaseContent();
        base.Dispose(disposing);
    }
}
