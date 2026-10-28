using System.ComponentModel;

namespace RibbonKit.WinForms;

/// <summary>Arranges the primary and dropdown halves of a split button.</summary>
public enum RibbonSplitButtonLayout
{
    /// <summary>Places the primary action on the left and menu arrow on the right.</summary>
    Horizontal,
    /// <summary>Places the icon above the caption/menu half at Large size; smaller sizes use Horizontal.</summary>
    Vertical
}

/// <summary>A primary command with a separately actionable owned dropdown.</summary>
[DefaultEvent(nameof(Click))]
public sealed class RibbonSplitButtonItem : RibbonDropDownItem
{
    private RibbonSplitButtonLayout _layout;
    private string _dropDownKeyTip = "";
    private bool _checked, _checkOnClick;
    /// <summary>Initializes a split button with a primary command and an owned item collection.</summary>
    public RibbonSplitButtonItem() { }
    /// <summary>Gets or sets whether primary activation toggles Checked before Click. Opening the menu never toggles it.</summary>
    [DefaultValue(false), Category("Behavior")]
    public bool CheckOnClick
    {
        get => _checkOnClick;
        set { Ribbon?.VerifyAccess(); _checkOnClick = value; }
    }
    /// <summary>Gets or sets the primary half's two-state value; assignment raises no Click event.</summary>
    [DefaultValue(false), Bindable(true), Category("Behavior")]
    public bool Checked
    {
        get => _checked;
        set
        {
            Ribbon?.VerifyAccess();
            if (_checked == value) return;
            _checked = value;
            Changed(false);
            CheckedChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    /// <summary>Occurs after Checked changes and before an activating primary Click.</summary>
    public event EventHandler? CheckedChanged;
    /// <summary>Gets or sets the preferred arrangement; Vertical applies only at Large size.</summary>
    [DefaultValue(RibbonSplitButtonLayout.Horizontal), Category("Layout")]
    public RibbonSplitButtonLayout Layout
    {
        get => _layout;
        set
        {
            Ribbon?.VerifyAccess();
            if (!Enum.IsDefined(value))
                throw new ArgumentOutOfRangeException(nameof(value));
            if (_layout == value)
                return;
            _layout = value;
            Changed();
        }
    }
    /// <summary>Gets or sets the menu half's preferred KeyTip; empty uses automatic resolution.</summary>
    [DefaultValue(""), Category("Behavior")]
    public string DropDownKeyTip
    {
        get => _dropDownKeyTip;
        set
        {
            Ribbon?.VerifyAccess();
            value ??= "";
            if (_dropDownKeyTip == value)
                return;
            _dropDownKeyTip = value;
            Changed(false);
        }
    }
    /// <summary>Occurs once when the primary command is activated; opening the menu raises no Click.</summary>
    public event EventHandler? Click;
    /// <summary>Invokes the primary command once; use ShowDropDown to open the menu.</summary>
    public override void PerformClick()
    {
        Ribbon?.VerifyAccess();
        Ribbon?.ScreenTips.Cancel();
        if (!EffectiveEnabled || !Visible) return;
        if (Ribbon?.CommandCatalog.Structure.Invoke(this) == true) return;
        if (CheckOnClick) Checked = !Checked;
        if (!IsDisposed) Click?.Invoke(this, EventArgs.Empty);
    }
}
