using System.ComponentModel;

namespace RibbonKit.WinForms;

/// <summary>Chooses the presentation of leaf commands and options in a command dropdown.</summary>
public enum RibbonMenuPresentation
{
    /// <summary>Standard command rows and compact option indicators.</summary>
    Standard,
    /// <summary>Larger artwork, bold titles, descriptions and full-row selected option fills.</summary>
    IllustratedChoices
}

public partial class RibbonDropDownItem
{
    private string _dropDownHeader = "";
    private RibbonMenuPresentation _menuPresentation;
    /// <summary>Gets or sets a noninteractive heading above this command dropdown's items. Empty omits it; gallery dropdowns retain their own group headings.</summary>
    [Category("Appearance"), DefaultValue(""), Localizable(true), Bindable(true)]
    public string DropDownHeader
    {
        get => _dropDownHeader;
        set { Ribbon?.VerifyAccess(); value ??= ""; if (_dropDownHeader == value) return; _dropDownHeader = value; DropDownHeaderChanged?.Invoke(this, EventArgs.Empty); Changed(); }
    }
    /// <summary>Occurs after DropDownHeader changes.</summary>
    public event EventHandler? DropDownHeaderChanged;
    /// <summary>Gets or sets how leaf commands and options appear in this command dropdown. Native editors, separators, submenus and gallery tiles retain their existing layouts.</summary>
    [Category("Appearance"), DefaultValue(RibbonMenuPresentation.Standard), Bindable(true)]
    public RibbonMenuPresentation MenuPresentation
    {
        get => _menuPresentation;
        set { Ribbon?.VerifyAccess(); if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value)); if (_menuPresentation == value) return; _menuPresentation = value; MenuPresentationChanged?.Invoke(this, EventArgs.Empty); Changed(); }
    }
    /// <summary>Occurs after MenuPresentation changes.</summary>
    public event EventHandler? MenuPresentationChanged;
}
