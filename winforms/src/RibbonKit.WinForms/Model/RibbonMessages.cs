using System.ComponentModel;

namespace RibbonKit.WinForms;

/// <summary>Owns repeatable ribbon notifications without owning application content or commands.</summary>
public sealed class RibbonMessageBar : RibbonElement
{
    private bool _hadOpen;
    /// <summary>Initializes an empty message bar.</summary>
    public RibbonMessageBar() => Items = new(this);
    /// <summary>Gets the owned message models in presentation order.</summary>
    [Category("Data"), DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    public RibbonMessageCollection Items { get; }
    /// <summary>Gets whether a visible, undisposed message is open.</summary>
    [Browsable(false)]
    public bool HasOpenMessages => !IsDisposed && Visible && Items.Any(m => !m.IsDisposed && m.Visible && m.IsOpen);
    /// <summary>Occurs when the presence of open messages changes.</summary>
    public event EventHandler? HasOpenMessagesChanged;
    internal override IEnumerable<RibbonElement> Children => Items;
    internal void Reconcile()
    {
        bool open = HasOpenMessages;
        if (_hadOpen == open) return;
        _hadOpen = open; HasOpenMessagesChanged?.Invoke(this, EventArgs.Empty);
    }
}

/// <summary>Owns messages for one message bar.</summary>
[Editor("RibbonKit.WinForms.Design.MessageCollectionEditor, RibbonKit.WinForms.Design", "System.Drawing.Design.UITypeEditor, System.Drawing")]
public sealed class RibbonMessageCollection : RibbonElementCollection<RibbonMessage>
{
    internal RibbonMessageCollection(RibbonMessageBar owner) : base(owner) { }
}

/// <summary>A bindable notification with inherited Text as its body, an optional title/icon/action, and retained open state.</summary>
public sealed class RibbonMessage : RibbonElement
{
    private string _title = "";
    private bool _isOpen = true, _isDismissible = true;
    private IRibbonIcon? _icon;
    private Image? _image;
    /// <summary>Initializes a message and its stable owned action.</summary>
    public RibbonMessage()
    {
        Action = new() { Parent = this };
        Close = new() { Parent = this, Text = "Close message" };
        Close.Click += (_, _) => Dismiss();
    }
    /// <summary>Gets or sets the localizable bold title.</summary>
    [Category("Appearance"), DefaultValue(""), Localizable(true), Bindable(true)]
    public string Title
    {
        get => _title;
        set { Ribbon?.VerifyAccess(); value ??= ""; if (_title == value) return; _title = value; Changed(); TitleChanged?.Invoke(this, EventArgs.Empty); }
    }
    /// <summary>Occurs after Title changes.</summary>
    public event EventHandler? TitleChanged;
    /// <summary>Gets or sets whether this message is presented. Closing retains the model, bindings and action. The state commits immediately; AnimationLevel can animate the panel drawing.</summary>
    [Category("Behavior"), DefaultValue(true), Bindable(true)]
    public bool IsOpen
    {
        get => _isOpen;
        set { Ribbon?.VerifyAccess(); if (_isOpen == value) return; if (!IsDisposed && Ribbon is { CanAnimateMessageChange: true } ribbon) { ribbon.Motion.Change(false, () => IsOpen = value, messages: true); return; } _isOpen = value; Changed(); IsOpenChanged?.Invoke(this, EventArgs.Empty); }
    }
    /// <summary>Occurs after IsOpen changes.</summary>
    public event EventHandler? IsOpenChanged;
    /// <summary>Gets or sets whether the user has a close target. Programmatic Dismiss remains available.</summary>
    [Category("Behavior"), DefaultValue(true), Bindable(true)]
    public bool IsDismissible
    {
        get => _isDismissible;
        set { Ribbon?.VerifyAccess(); if (_isDismissible == value) return; _isDismissible = value; Changed(); IsDismissibleChanged?.Invoke(this, EventArgs.Empty); }
    }
    /// <summary>Occurs after IsDismissible changes.</summary>
    public event EventHandler? IsDismissibleChanged;
    /// <summary>Gets or sets a borrowed vector icon; takes precedence over Image. Null uses the shared default shield.</summary>
    [Category("Appearance"), DefaultValue(null), Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public IRibbonIcon? Icon { get => _icon; set { Ribbon?.VerifyAccess(); if (ReferenceEquals(_icon, value)) return; _icon = value; Changed(false); } }
    /// <summary>Gets or sets a borrowed raster image. The application owns its lifetime.</summary>
    [Category("Appearance"), DefaultValue(null), Localizable(true)]
    public Image? Image { get => _image; set { Ribbon?.VerifyAccess(); if (ReferenceEquals(_image, value)) return; _image = value; Changed(false); } }
    /// <summary>Gets the stable owned optional action. Set Text or an icon to present it, and handle Click. Invocation does not dismiss the message automatically.</summary>
    [Category("Behavior"), DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    public RibbonButtonItem Action { get; }
    /// <summary>Closes an open message once and raises Dismissed. It remains in its collection and can be reopened.</summary>
    public void Dismiss()
    {
        Ribbon?.VerifyAccess(); if (IsDisposed || !IsOpen) return;
        if (Ribbon is { CanAnimateMessageChange: true } ribbon) { ribbon.Motion.Change(false, Dismiss, messages: true); return; }
        IsOpen = false; Dismissed?.Invoke(this, EventArgs.Empty);
    }
    /// <summary>Occurs once for each successful Dismiss call; assigning IsOpen alone raises only IsOpenChanged.</summary>
    public event EventHandler? Dismissed;
    internal RibbonButtonItem Close { get; }
    internal bool HasAction => !Action.IsDisposed && Action.Visible && (Action.Text.Length > 0 || Action.Icon is not null || Action.Image is not null);
    internal override IEnumerable<RibbonElement> Children => [Action, Close];
}
