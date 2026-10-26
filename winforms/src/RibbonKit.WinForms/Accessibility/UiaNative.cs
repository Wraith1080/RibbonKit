using System.ComponentModel;
using System.Runtime.InteropServices;

namespace RibbonKit.WinForms.Accessibility;

// ABI declarations from Windows SDK 10.0.26100.0 UIAutomationCore.h.
// No WindowsBase, PresentationCore, WPF peers, or private WinForms interfaces.
/// <summary>Windows UI Automation COM ABI member; reserved for the native provider bridge.</summary>
[ComVisible(true), Guid("d6dd68d1-86fd-4332-8666-9abedea2d24c"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), EditorBrowsable(EditorBrowsableState.Never)]
public interface IRawProvider
{
    /// <summary>Windows UI Automation COM ABI member; reserved for the native provider bridge.</summary>
    int ProviderOptions
    {
        get;
    }
    /// <summary>Windows UI Automation COM ABI member; reserved for the native provider bridge.</summary>
    [return: MarshalAs(UnmanagedType.IUnknown)] object? GetPatternProvider(int patternId);
    /// <summary>Windows UI Automation COM ABI member; reserved for the native provider bridge.</summary>
    [return: MarshalAs(UnmanagedType.Struct)] object? GetPropertyValue(int propertyId);
    /// <summary>Windows UI Automation COM ABI member; reserved for the native provider bridge.</summary>
    IRawProvider? HostRawElementProvider
    {
        get;
    }
}
/// <summary>Windows UI Automation COM ABI member; reserved for the native provider bridge.</summary>
[ComVisible(true), Guid("f7063da8-8359-439c-9297-bbc5299a7d87"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), EditorBrowsable(EditorBrowsableState.Never)]
public interface IFragmentProvider
{
    /// <summary>Windows UI Automation COM ABI member; reserved for the native provider bridge.</summary>
    IFragmentProvider? Navigate(int direction);
    /// <summary>Windows UI Automation COM ABI member; reserved for the native provider bridge.</summary>
    [return: MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_I4)] int[] GetRuntimeId();
    /// <summary>Windows UI Automation COM ABI member; reserved for the native provider bridge.</summary>
    UiaRectangle BoundingRectangle
    {
        get;
    }
    /// <summary>Windows UI Automation COM ABI member; reserved for the native provider bridge.</summary>
    [return: MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_UNKNOWN)] IFragmentRootProvider[]? GetEmbeddedFragmentRoots();
    /// <summary>Windows UI Automation COM ABI member; reserved for the native provider bridge.</summary>
    void SetFocus();
    /// <summary>Windows UI Automation COM ABI member; reserved for the native provider bridge.</summary>
    IFragmentRootProvider FragmentRoot
    {
        get;
    }
}
/// <summary>Windows UI Automation COM ABI member; reserved for the native provider bridge.</summary>
[StructLayout(LayoutKind.Sequential), EditorBrowsable(EditorBrowsableState.Never)]
public struct UiaRectangle
{
    /// <summary>Windows UI Automation COM ABI member; reserved for the native provider bridge.</summary>
    public double Left, Top, Width, Height; internal UiaRectangle(Rectangle r) => (Left, Top, Width, Height) = (r.X, r.Y, r.Width, r.Height);
}
/// <summary>Windows UI Automation COM ABI member; reserved for the native provider bridge.</summary>
[ComVisible(true), Guid("620ce2a5-ab8f-40a9-86cb-de3c75599b58"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), EditorBrowsable(EditorBrowsableState.Never)]
public interface IFragmentRootProvider
{
    /// <summary>Windows UI Automation COM ABI member; reserved for the native provider bridge.</summary>
    IFragmentProvider? ElementProviderFromPoint(double x, double y);
    /// <summary>Windows UI Automation COM ABI member; reserved for the native provider bridge.</summary>
    IFragmentProvider? GetFocus();
}
/// <summary>Windows UI Automation COM ABI member; reserved for the native provider bridge.</summary>
[ComVisible(true), Guid("54fcb24b-e18e-47a2-b4d3-eccbe77599a2"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), EditorBrowsable(EditorBrowsableState.Never)]
public interface IInvokeProvider
{
    /// <summary>Windows UI Automation COM ABI member; reserved for the native provider bridge.</summary>
    void Invoke();
}
/// <summary>Windows UI Automation COM ABI member; reserved for the native provider bridge.</summary>
[ComVisible(true), Guid("56d00bd0-c4f4-433c-a836-1a52a57e0892"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), EditorBrowsable(EditorBrowsableState.Never)]
public interface IToggleProvider
{
    /// <summary>Windows UI Automation COM ABI member; reserved for the native provider bridge.</summary>
    void Toggle();
    /// <summary>Windows UI Automation COM ABI member; reserved for the native provider bridge.</summary>
    int ToggleState
    {
        get;
    }
}
/// <summary>Windows UI Automation COM ABI member; reserved for the native provider bridge.</summary>
[ComVisible(true), Guid("fb8b03af-3bdf-48d4-bd36-1a65793be168"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), EditorBrowsable(EditorBrowsableState.Never)]
public interface ISelectionProvider
{
    /// <summary>Windows UI Automation COM ABI member; reserved for the native provider bridge.</summary>
    [return: MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_UNKNOWN)] IRawProvider[] GetSelection();
    /// <summary>Windows UI Automation COM ABI member; reserved for the native provider bridge.</summary>
    bool CanSelectMultiple
    {
        [return: MarshalAs(UnmanagedType.Bool)]
        get;
    }
    /// <summary>Windows UI Automation COM ABI member; reserved for the native provider bridge.</summary>
    bool IsSelectionRequired
    {
        [return: MarshalAs(UnmanagedType.Bool)]
        get;
    }
}
/// <summary>Windows UI Automation COM ABI member; reserved for the native provider bridge.</summary>
[ComVisible(true), Guid("2acad808-b2d4-452d-a407-91ff1ad167b2"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), EditorBrowsable(EditorBrowsableState.Never)]
public interface ISelectionItemProvider
{
    /// <summary>Windows UI Automation COM ABI member; reserved for the native provider bridge.</summary>
    void Select();
    /// <summary>Windows UI Automation COM ABI member; reserved for the native provider bridge.</summary>
    void AddToSelection();
    /// <summary>Windows UI Automation COM ABI member; reserved for the native provider bridge.</summary>
    void RemoveFromSelection();
    /// <summary>Windows UI Automation COM ABI member; reserved for the native provider bridge.</summary>
    bool IsSelected
    {
        [return: MarshalAs(UnmanagedType.Bool)]
        get;
    }
    /// <summary>Windows UI Automation COM ABI member; reserved for the native provider bridge.</summary>
    IRawProvider SelectionContainer
    {
        get;
    }
}
/// <summary>Windows UI Automation COM ABI member; reserved for the native provider bridge.</summary>
[ComVisible(true), Guid("d847d3a5-cab0-4a98-8c32-ecb45c59ad24"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), EditorBrowsable(EditorBrowsableState.Never)]
public interface IExpandProvider
{
    /// <summary>Windows UI Automation COM ABI member; reserved for the native provider bridge.</summary>
    void Expand();
    /// <summary>Windows UI Automation COM ABI member; reserved for the native provider bridge.</summary>
    void Collapse();
    /// <summary>Windows UI Automation COM ABI member; reserved for the native provider bridge.</summary>
    int ExpandCollapseState
    {
        get;
    }
}
internal static class UiaNative
{
    [DllImport("UIAutomationCore.dll")] internal static extern nint UiaReturnRawElementProvider(nint hwnd, nint wParam, nint lParam, [MarshalAs(UnmanagedType.Interface)] IRawProvider provider);
    [DllImport("UIAutomationCore.dll")] internal static extern int UiaHostProviderFromHwnd(nint hwnd, [MarshalAs(UnmanagedType.Interface)] out IRawProvider provider);
    [DllImport("UIAutomationCore.dll")] internal static extern int UiaRaiseAutomationEvent([MarshalAs(UnmanagedType.Interface)] IRawProvider provider, int eventId);
    [DllImport("UIAutomationCore.dll")] internal static extern int UiaRaiseStructureChangedEvent([MarshalAs(UnmanagedType.Interface)] IRawProvider provider, int changeType, [In] int[]? runtimeId, int length);
    [DllImport("UIAutomationCore.dll")] internal static extern int UiaRaiseAutomationPropertyChangedEvent([MarshalAs(UnmanagedType.Interface)] IRawProvider provider, int propertyId, [MarshalAs(UnmanagedType.Struct)] object oldValue, [MarshalAs(UnmanagedType.Struct)] object newValue);
    [DllImport("UIAutomationCore.dll")] internal static extern int UiaDisconnectProvider([MarshalAs(UnmanagedType.Interface)] IRawProvider provider);
}
