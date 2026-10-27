using System.ComponentModel;

namespace RibbonKit.WinForms;

/// <summary>The permitted steps when a group no longer fits.</summary>
public enum RibbonGroupReductionMode
{
    /// <summary>Collapses directly to a group opener.</summary>
    Collapse,
    /// <summary>Reduces item sizes without collapsing the group.</summary>
    Resize,
    /// <summary>Reduces item sizes before collapsing the group.</summary>
    ResizeThenCollapse
}
/// <summary>The presentation size of a ribbon command.</summary>
public enum RibbonItemSize
{
    /// <summary>The large command presentation.</summary>
    Large,
    /// <summary>The medium command presentation.</summary>
    Medium,
    /// <summary>The small command presentation.</summary>
    Small
}

/// <summary>A tab owning a collection of ribbon groups.</summary>
public sealed class RibbonTab : RibbonElement
{
    /// <summary>Initializes a RibbonTab instance.</summary>
    public RibbonTab() => Groups = new(this);
    /// <summary>Gets the stable, owned group collection.</summary>
    [Category("Data"), DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    public RibbonGroupCollection Groups
    {
        get;
    }
    internal override IEnumerable<RibbonElement> Children => Groups;
}

/// <summary>A captioned group with an explicit reduction policy.</summary>
public sealed class RibbonGroup : RibbonElement
{
    private RibbonGroupReductionMode _reductionMode;
    private int? _priority;
    private bool _canResize = true;
    /// <summary>Initializes a RibbonGroup instance.</summary>
    public RibbonGroup() => Items = new(this);
    /// <summary>Gets the stable, owned command collection.</summary>
    [Category("Data"), DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    public RibbonItemCollection Items
    {
        get;
    }
    /// <summary>Gets or sets the permitted group reduction steps.</summary>
    [DefaultValue(RibbonGroupReductionMode.Collapse), Category("Layout")]
    public RibbonGroupReductionMode ReductionMode
    {
        get => _reductionMode; set
        {
            Ribbon?.VerifyAccess();
            if (!Enum.IsDefined(value))
                throw new ArgumentOutOfRangeException(nameof(value));
            if (_reductionMode == value)
                return;
            _reductionMode = value;
            Changed();
        }
    }
    /// <summary>Gets or sets reduction order: higher explicit priorities reduce first; null uses width order.</summary>
    [DefaultValue(null), Category("Layout")]
    public int? ReductionPriority
    {
        get => _priority; set
        {
            Ribbon?.VerifyAccess();
            if (_priority == value)
                return;
            _priority = value;
            Changed();
        }
    }
    /// <summary>Gets or sets whether the group can reduce or collapse.</summary>
    [DefaultValue(true), Category("Layout")]
    public bool CanResize
    {
        get => _canResize; set
        {
            Ribbon?.VerifyAccess();
            if (_canResize == value)
                return;
            _canResize = value;
            Changed();
        }
    }
    internal override IEnumerable<RibbonElement> Children => Items;
}

/// <summary>The base for ribbon commands and popup rows.</summary>
public abstract class RibbonItem : RibbonElement
{
    private string _accessibleName = "", _description = "";
    private string _screenTipTitle = "", _screenTipDescription = "";
    private Image? _image, _largeImage;
    private IRibbonIcon? _icon;
    private RibbonSizePolicy _sizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Large);
    /// <summary>Gets or sets the passive hover tip title. Empty uses the complete Text caption, including text ellipsized on screen.</summary>
    [DefaultValue(""), Localizable(true), Category("Appearance")]
    public string ScreenTipTitle
    {
        get => _screenTipTitle;
        set
        {
            Ribbon?.VerifyAccess();
            value ??= "";
            if (_screenTipTitle == value) return;
            _screenTipTitle = value;
            Changed(false);
        }
    }
    /// <summary>Gets or sets optional descriptive text for the passive hover tip, including for disabled items. This does not change command or accessibility state.</summary>
    [DefaultValue(""), Localizable(true), Category("Appearance")]
    public string ScreenTipDescription
    {
        get => _screenTipDescription;
        set
        {
            Ribbon?.VerifyAccess();
            value ??= "";
            if (_screenTipDescription == value) return;
            _screenTipDescription = value;
            Changed(false);
        }
    }
    /// <summary>Gets or sets the accessible label; empty uses Text.</summary>
    [DefaultValue(""), Localizable(true), Category("Accessibility")]
    public string AccessibleName
    {
        get => _accessibleName; set
        {
            Ribbon?.VerifyAccess();
            _accessibleName = value ?? "";
            Changed(false);
        }
    }
    /// <summary>Gets or sets the help text exposed to accessibility clients.</summary>
    [DefaultValue(""), Localizable(true), Category("Accessibility")]
    public string AccessibleDescription
    {
        get => _description; set
        {
            Ribbon?.VerifyAccess();
            _description = value ?? "";
            Changed(false);
        }
    }
    /// <summary>Gets or sets a borrowed small image; the caller retains disposal ownership.</summary>
    [DefaultValue(null), Category("Appearance"), Localizable(true)]
    public Image? Image
    {
        get => _image; set
        {
            Ribbon?.VerifyAccess();
            _image = value;
            Changed();
        }
    }
    /// <summary>Gets or sets a borrowed large image; the caller retains disposal ownership.</summary>
    [DefaultValue(null), Category("Appearance"), Localizable(true)]
    public Image? LargeImage
    {
        get => _largeImage; set
        {
            Ribbon?.VerifyAccess();
            _largeImage = value;
            Changed();
        }
    }
    /// <summary>Gets or sets a borrowed code-authored vector icon provider.</summary>
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public IRibbonIcon? Icon
    {
        get => _icon; set
        {
            Ribbon?.VerifyAccess();
            _icon = value;
            Changed();
        }
    }
    /// <summary>Gets or sets the item size used at each group reduction step.</summary>
    [Category("Layout")]
    public RibbonSizePolicy SizePolicy
    {
        get => _sizePolicy; set
        {
            Ribbon?.VerifyAccess();
            if (_sizePolicy == value)
                return;
            _sizePolicy = value;
            Changed();
        }
    }
    private bool ShouldSerializeSizePolicy() => SizePolicy != RibbonSizePolicy.Fixed(RibbonItemSize.Large);
    private void ResetSizePolicy() => SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Large);
    /// <summary>Activates the command through the shared activation path.</summary>
    public abstract void PerformClick();
}

/// <summary>A command activated by mouse, keyboard or automation.</summary>
[DefaultEvent(nameof(Click))]
public class RibbonButtonItem : RibbonItem
{
    /// <summary>Occurs once after successful activation and any state change.</summary>
    public event EventHandler? Click;
    /// <summary>Activates once through the same path as native input and automation.</summary>
    public override void PerformClick()
    {
        Ribbon?.VerifyAccess();
        Ribbon?.ScreenTips.Cancel();
        if (!EffectiveEnabled || !Visible)
            return;
        BeforeClick();
        if (!IsDisposed)
            Click?.Invoke(this, EventArgs.Empty);
    }
    /// <summary>Updates command state immediately before Click is raised.</summary>
    protected virtual void BeforeClick()
    {
    }
}

/// <summary>A two-state command that changes Checked before Click.</summary>
public sealed class RibbonToggleItem : RibbonButtonItem
{
    private bool _checked;
    /// <summary>Gets or sets the two-state value; assignment raises no Click event.</summary>
    [DefaultValue(false), Bindable(true), Category("Behavior")]
    public bool Checked
    {
        get => _checked; set
        {
            Ribbon?.VerifyAccess();
            if (_checked == value)
                return;
            _checked = value;
            Changed(false);
            CheckedChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    /// <summary>Occurs after Checked changes and before an activating Click.</summary>
    public event EventHandler? CheckedChanged;
    /// <inheritdoc />
    protected override void BeforeClick() => Checked = !Checked;
}

/// <summary>A command that opens owned popup rows without a primary Click action.</summary>
public class RibbonDropDownItem : RibbonItem
{
    /// <summary>Gets whether this item's owned popup is currently open.</summary>
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool IsDropDownOpen => Ribbon?.Popups.IsExpanded(this) ?? false;
    /// <summary>Initializes a RibbonDropDownItem instance.</summary>
    public RibbonDropDownItem() => Items = new(this);
    /// <summary>Gets the stable, owned command collection.</summary>
    [Category("Data"), DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    public RibbonItemCollection Items
    {
        get;
    }
    /// <summary>Occurs before opening; cancellation prevents the popup.</summary>
    public event CancelEventHandler? DropDownOpening;
    /// <summary>Occurs after the owned popup closes.</summary>
    public event EventHandler? DropDownClosed;
    /// <summary>Opens the popup when attached, visible and enabled.</summary>
    public void ShowDropDown()
    {
        Ribbon?.VerifyAccess();
        if (EffectiveEnabled && Visible)
            Ribbon?.OpenDropDown(this);
    }
    /// <summary>Closes this popup and restores keyboard focus.</summary>
    public void CloseDropDown()
    {
        Ribbon?.VerifyAccess();
        Ribbon?.CloseDropDown(this);
    }
    /// <inheritdoc />
    public override void PerformClick() => ShowDropDown();
    internal bool CanOpen()
    {
        var e = new CancelEventArgs();
        DropDownOpening?.Invoke(this, e);
        return !e.Cancel && EffectiveEnabled && Visible && Ribbon is not null;
    }
    internal void OnClosed() => DropDownClosed?.Invoke(this, EventArgs.Empty);
    internal override IEnumerable<RibbonElement> Children => Items;
}
