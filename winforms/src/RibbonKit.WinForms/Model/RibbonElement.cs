using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace RibbonKit.WinForms;

/// <summary>A named, owned ribbon component with ordinary WinForms binding.</summary>
[DefaultProperty(nameof(Text)), ToolboxItem(false), DesignTimeVisible(false)]
[Designer("RibbonKit.WinForms.Design.RibbonElementDesigner, RibbonKit.WinForms.Design")]
public abstract class RibbonElement : Component, IBindableComponent
{
    private string _id = "rk:" + Guid.NewGuid().ToString("N");
    private string _text = "";
    private string _keyTip = "";
    private bool _enabled = true, _visible = true;
    internal bool HasExplicitId { get; private set; }
    private BindingContext? _bindingContext;
    private ControlBindingsCollection? _bindings;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal object? Parent
    {
        get; set;
    }
    internal bool IsDisposed
    {
        get; private set;
    }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal RibbonMergeSource? MergeProvenance { get; set; }
    internal RibbonControl? Ribbon => Parent is RibbonControl ribbon ? ribbon : (Parent as RibbonElement)?.Ribbon;
    internal virtual IEnumerable<RibbonElement> Children => [];

    /// <summary>Stable identity, independent of the component name and display text.</summary>
    [Category("Design"), DefaultValue("")]
    public string Id
    {
        get => _id;
        set
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            Ribbon?.VerifyAccess();
            if (value == _id)
            {
                HasExplicitId = true;
                return;
            }
            Ribbon?.ValidateIdentityChange(this, value);
            if (Ribbon is null)
            {
                RibbonElement root = this;
                while (root.Parent is RibbonElement parent)
                    root = parent;
                if (root.Tree().Any(n => !ReferenceEquals(n, this) && n.Id == value))
                    throw new ArgumentException("Duplicate component identity: " + value);
            }
            _id = value;
            HasExplicitId = true;
            Changed(false);
            IdChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    /// <summary>Gets or sets the localizable display label.</summary>
    [Category("Appearance"), DefaultValue(""), Localizable(true)]
    public string Text
    {
        get => _text; set
        {
            Ribbon?.VerifyAccess();
            value ??= "";
            if (_text == value)
                return;
            _text = value;
            Changed();
            TextChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    /// <summary>Gets or sets whether this component permits user activation.</summary>
    [Category("Behavior"), DefaultValue(true)]
    public bool Enabled
    {
        get => _enabled; set
        {
            Ribbon?.VerifyAccess();
            if (_enabled == value)
                return;
            _enabled = value;
            Changed(false);
            EnabledChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    /// <summary>Gets or sets whether this component participates in presentation.</summary>
    [Category("Behavior"), DefaultValue(true)]
    public bool Visible
    {
        get => _visible; set
        {
            Ribbon?.VerifyAccess();
            if (_visible == value)
                return;
            if (!IsDisposed && this is RibbonMessage or RibbonMessageBar && Ribbon is { CanAnimateMessageChange: true } messageHost)
            { messageHost.Motion.Change(false, () => Visible = value, messages: true); return; }
            if (value && Ribbon is { CanAnimateHeaderArrival: true } ribbon &&
                (this is RibbonTab tab && ribbon.GetPresentedTabs().Contains(tab) || this is RibbonMergeSource source && source.Tabs.Any(t => t.Visible)))
            { ribbon.Motion.Change(false, () => Visible = value, headers: true); return; }
            _visible = value;
            Changed();
            VisibleChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    /// <summary>Gets or sets the preferred ASCII key sequence, resolved within its current level.</summary>
    [Category("Behavior"), DefaultValue("")]
    public string KeyTip
    {
        get => _keyTip; set
        {
            Ribbon?.VerifyAccess();
            value ??= "";
            if (_keyTip == value)
                return;
            _keyTip = value;
            Changed(false);
            KeyTipChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    /// <summary>Occurs after the stable identity changes.</summary>
    public event EventHandler? IdChanged;
    /// <summary>Occurs after Text changes.</summary>
    public event EventHandler? TextChanged;
    /// <summary>Occurs after Enabled changes.</summary>
    public event EventHandler? EnabledChanged;
    /// <summary>Occurs after Visible changes.</summary>
    public event EventHandler? VisibleChanged;
    /// <summary>Occurs after KeyTip changes.</summary>
    public event EventHandler? KeyTipChanged;
    /// <summary>Gets or sets the binding context shared with owned descendants.</summary>
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [AllowNull]
    public BindingContext BindingContext
    {
        get => _bindingContext ??= Ribbon?.BindingContext ?? new BindingContext();
        set
        {
            Ribbon?.VerifyAccess();
            if (ReferenceEquals(value, _bindingContext))
                return;
            _bindingContext = value;
            BindingContextChanged?.Invoke(this, EventArgs.Empty);
            foreach (var child in Children)
                child.BindingContext = value;
        }
    }
    /// <summary>Gets the ordinary WinForms data bindings for this component.</summary>
    [Category("Data"), DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    public ControlBindingsCollection DataBindings => _bindings ??= new(this);
    /// <summary>Occurs after the binding context changes.</summary>
    public event EventHandler? BindingContextChanged;
    internal bool EffectiveEnabled => !IsDisposed && Enabled && (Parent is RibbonElement parent ? parent.EffectiveEnabled : Ribbon?.Enabled ?? true)
        && (this is not RibbonGroupContribution contribution || contribution.TargetTab is { Visible: true, EffectiveEnabled: true })
        && (this is not RibbonMessage message || message.IsOpen);
    internal void Changed(bool layout = true)
    {
        if (Ribbon is { } ribbon) ribbon.ModelChanged(layout, ribbon.Popups.IsPreparingContent(this), this);
    }
    internal string DisplayText => Ribbon?.CommandCatalog?.Structure.Label(this) ?? Text;
    internal IEnumerable<RibbonElement> Tree()
    {
        yield return this;
        foreach (var child in Children)
            foreach (var node in child.Tree())
                yield return node;
    }
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing && !IsDisposed)
        {
            Ribbon?.VerifyAccess();
            IsDisposed = true;
            foreach (var child in Children.ToArray())
                child.Dispose();
            _bindings?.Clear();
            RibbonControl.DetachElement(this);
        }
        base.Dispose(disposing);
    }
    /// <inheritdoc />
    public override string ToString() => string.IsNullOrEmpty(Text) ? GetType().Name : Text;
}
