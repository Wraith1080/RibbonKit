using System.ComponentModel;

namespace RibbonKit.WinForms;

public sealed partial class RibbonTab
{
    private bool _isModal, _canClose = true;
    private string _closeButtonText = "";
    /// <summary>Gets or sets whether this tab can be entered as an exclusive modal scope. Setting it does not enter the mode.</summary>
    [Category("Behavior"), DefaultValue(false), Bindable(true)]
    public bool IsModal
    {
        get => _isModal;
        set { Ribbon?.VerifyAccess(); if (_isModal == value) return; _isModal = value; Changed(); IsModalChanged?.Invoke(this, EventArgs.Empty); }
    }
    /// <summary>Gets or sets whether the header close button and user Escape can exit this tab's modal scope. Application exit remains available.</summary>
    [Category("Behavior"), DefaultValue(true), Bindable(true)]
    public bool CanClose
    {
        get => _canClose;
        set { Ribbon?.VerifyAccess(); if (_canClose == value) return; _canClose = value; Changed(); CanCloseChanged?.Invoke(this, EventArgs.Empty); }
    }
    /// <summary>Gets or sets the modal close caption. Empty uses the library's default Close caption.</summary>
    [Category("Appearance"), DefaultValue(""), Localizable(true)]
    public string CloseButtonText
    {
        get => _closeButtonText;
        set { Ribbon?.VerifyAccess(); value ??= ""; if (_closeButtonText == value) return; _closeButtonText = value; Changed(); }
    }
    /// <summary>Occurs after IsModal changes.</summary>
    public event EventHandler? IsModalChanged;
    /// <summary>Occurs after CanClose changes.</summary>
    public event EventHandler? CanCloseChanged;
}
