using System.ComponentModel;

namespace RibbonKit.WinForms;

public partial class RibbonForm
{
    private RibbonWindowCornerPreference? _windowCornerPreference;

    /// <summary>Gets or sets an optional native window-corner override. Null (the default and Reset value) follows the associated ribbon theme, or SystemDefault without a ribbon. Explicit values win over theme changes. Unsupported systems and designer/child windows retain the request without applying it. Does not change the native frame, control radii or backdrops.</summary>
    [Category("Appearance"), DefaultValue(null), Description("Optional native window corners. Reset follows the ribbon theme. Windows owns the final silhouette.")]
    public RibbonWindowCornerPreference? WindowCornerPreference
    {
        get => _windowCornerPreference;
        set
        {
            if (value is { } preference && !Enum.IsDefined(preference)) throw new ArgumentOutOfRangeException(nameof(value));
            _ribbon?.VerifyAccess();
            if (_windowCornerPreference == value) return;
            _windowCornerPreference = value;
            RefreshWindowCornerPreference();
            WindowCornerPreferenceChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>Occurs after the form's requested window-corner override changes. Theme changes do not change the override.</summary>
    public event EventHandler? WindowCornerPreferenceChanged;

    /// <summary>Gets the resolved window-corner request: the explicit form override, associated ribbon theme, or SystemDefault. This is policy, not native API success or proof of visible corners; Windows may ignore it.</summary>
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public RibbonWindowCornerPreference EffectiveWindowCornerPreference => _windowCornerPreference
        ?? _ribbon?.Theme.WindowCornerPreference ?? RibbonWindowCornerPreference.SystemDefault;

    internal static bool CanApplyWindowCornerPreference(bool supported, bool designer, bool topLevel)
        => supported && !designer && topLevel;

    internal void RefreshWindowCornerPreference()
    {
        if (!IsHandleCreated || IsDisposed || Disposing || _disposingBackdrop ||
            !CanApplyWindowCornerPreference(OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000),
                DesignMode || LicenseManager.UsageMode == LicenseUsageMode.Designtime, TopLevel)) return;
        // DWMWA_WINDOW_CORNER_PREFERENCE. Leave the native resizable frame and
        // material attributes alone. A failed/unsupported call retains policy.
        int preference = (int)EffectiveWindowCornerPreference;
        DwmSetWindowAttribute(Handle, 33, ref preference, sizeof(int));
        // Attributes belong to the HWND: never reset them during destruction
        // while DWM is taking the final closing frame.
    }
}
