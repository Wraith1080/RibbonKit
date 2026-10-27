using System.ComponentModel;

namespace RibbonKit.WinForms;

/// <summary>A compact check option with a bindable two-state or three-state value.</summary>
public sealed class RibbonCheckBoxItem : RibbonButtonItem
{
    private CheckState _state;
    private bool _threeState;
    private int _version;
    /// <summary>Initializes a compact check option.</summary>
    public RibbonCheckBoxItem() { }
    /// <summary>Gets or sets whether the state is checked or indeterminate. Assignment raises no Click.</summary>
    [DefaultValue(false), Bindable(true), Category("Behavior")]
    public bool Checked
    {
        get => _state != CheckState.Unchecked;
        set
        {
            Ribbon?.VerifyAccess();
            if (value != Checked) CheckState = value ? CheckState.Checked : CheckState.Unchecked;
        }
    }
    /// <summary>Gets or sets the exact check state. Programmatic indeterminate values are allowed independently of ThreeState.</summary>
    /// <remarks>Formatted WinForms binding treats Indeterminate as a null source value. Disable formatting when binding an enum-valued source.</remarks>
    [DefaultValue(CheckState.Unchecked), Bindable(true), Category("Behavior")]
    public CheckState CheckState
    {
        get => _state;
        set
        {
            Ribbon?.VerifyAccess();
            if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value));
            if (_state == value) return;
            bool before = Checked;
            _state = value;
            int version = ++_version;
            Changed(false);
            if (before != Checked) CheckedChanged?.Invoke(this, EventArgs.Empty);
            if (!IsDisposed && version == _version) CheckStateChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    /// <summary>Gets or sets whether activation cycles through Unchecked, Checked and Indeterminate.</summary>
    [DefaultValue(false), Category("Behavior")]
    public bool ThreeState
    {
        get => _threeState;
        set { Ribbon?.VerifyAccess(); _threeState = value; }
    }
    /// <summary>Occurs when Checked changes, before CheckStateChanged and an activating Click.</summary>
    public event EventHandler? CheckedChanged;
    /// <summary>Occurs when the exact state changes, before an activating Click.</summary>
    public event EventHandler? CheckStateChanged;
    /// <inheritdoc />
    protected override void BeforeClick() => CheckState = _state switch
    {
        CheckState.Unchecked => CheckState.Checked,
        CheckState.Checked when ThreeState => CheckState.Indeterminate,
        _ => CheckState.Unchecked
    };
}

/// <summary>A compact mutually exclusive option in a logical collection or named ribbon group.</summary>
public sealed class RibbonRadioButtonItem : RibbonButtonItem
{
    private bool _checked;
    private string _groupName = "";
    private int _version;
    /// <summary>Initializes a compact radio option.</summary>
    public RibbonRadioButtonItem() { }
    /// <summary>Gets or sets selection. Selecting clears peers; assignment raises no Click. Clearing permits an empty group.</summary>
    [DefaultValue(false), Bindable(true), Category("Behavior")]
    public bool Checked
    {
        get => _checked;
        set
        {
            Ribbon?.VerifyAccess();
            if (_checked == value) return;
            int version = ++_version;
            // Publish peer changes before staging this value. BindingSource can
            // refresh every binding when one peer writes its source property.
            if (value && !ClearPeers()) return;
            if (IsDisposed || version != _version) return;
            _checked = value;
            Changed(false);
            CheckedChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    /// <summary>Gets or sets the group name. Empty groups siblings; named groups span the owning ribbon or detached logical tree.</summary>
    [DefaultValue(""), Category("Behavior")]
    public string GroupName
    {
        get => _groupName;
        set
        {
            Ribbon?.VerifyAccess();
            value ??= "";
            if (_groupName == value) return;
            _groupName = value;
            if (_checked) Normalize();
        }
    }
    /// <summary>Occurs after selection changes and before an activating Click.</summary>
    public event EventHandler? CheckedChanged;
    /// <inheritdoc />
    protected override void BeforeClick() => Checked = true;

    private object Scope
    {
        get
        {
            if (_groupName.Length == 0) return Parent ?? this;
            if (Ribbon is { } ribbon) return ribbon;
            RibbonElement root = this;
            while (root.Parent is RibbonElement parent) root = parent;
            return root;
        }
    }
    private void Normalize()
    {
        if (!ClearPeers() && _checked && !IsDisposed) Checked = false;
    }
    private bool ClearPeers()
    {
        var scope = Scope;
        var elements = scope is RibbonControl ribbon ? ribbon.AllElements : ((RibbonElement)scope).Tree();
        var peers = elements.OfType<RibbonRadioButtonItem>().Where(p => !ReferenceEquals(p, this)).ToArray();
        bool Matches(RibbonRadioButtonItem peer) => !peer.IsDisposed && peer._groupName == _groupName && ReferenceEquals(Scope, peer.Scope);
        foreach (var peer in peers)
        {
            if (IsDisposed) return false;
            if (peer.Checked && Matches(peer))
            {
                peer.Checked = false;
                if (peer.Checked && Matches(peer)) return false;
            }
        }
        return !peers.Any(p => p.Checked && Matches(p));
    }
    internal static void NormalizeAttached(RibbonElement element)
    {
        foreach (var radio in element.Tree().OfType<RibbonRadioButtonItem>().ToArray())
            if (radio._checked && !radio.IsDisposed) radio.Normalize();
    }
}
