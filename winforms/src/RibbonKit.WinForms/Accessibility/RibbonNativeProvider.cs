using System.ComponentModel;
using System.Runtime.InteropServices;

namespace RibbonKit.WinForms.Accessibility;

// Native HRESULT signatures, in SDK vtable order. Keep the published managed
// interfaces unchanged; only this boundary is handed to UIAutomationCore.
/// <summary>Reserved Windows UI Automation HRESULT ABI; not an application extension point.</summary>
[ComVisible(true), EditorBrowsable(EditorBrowsableState.Never), Guid("d6dd68d1-86fd-4332-8666-9abedea2d24c"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface INativeRawProvider
{
    /// <summary>Reserved native COM method; returns its HRESULT without exception translation.</summary>
    [PreserveSig] int ProviderOptions(out int value);
    /// <summary>Reserved native COM method; returns its HRESULT without exception translation.</summary>
    [PreserveSig] int GetPatternProvider(int id, [MarshalAs(UnmanagedType.IUnknown)] out object? value);
    /// <summary>Reserved native COM method; returns its HRESULT without exception translation.</summary>
    [PreserveSig] int GetPropertyValue(int id, [MarshalAs(UnmanagedType.Struct)] out object? value);
    /// <summary>Reserved native COM method; returns its HRESULT without exception translation.</summary>
    [PreserveSig] int HostRawElementProvider(out INativeRawProvider? value);
}
/// <summary>Reserved Windows UI Automation HRESULT ABI; not an application extension point.</summary>
[ComVisible(true), EditorBrowsable(EditorBrowsableState.Never), Guid("f7063da8-8359-439c-9297-bbc5299a7d87"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface INativeFragmentProvider
{
    /// <summary>Reserved native COM method; returns its HRESULT without exception translation.</summary>
    [PreserveSig] int Navigate(int direction, out INativeFragmentProvider? value);
    /// <summary>Reserved native COM method; returns its HRESULT without exception translation.</summary>
    [PreserveSig] int GetRuntimeId([MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_I4)] out int[]? value);
    /// <summary>Reserved native COM method; returns its HRESULT without exception translation.</summary>
    [PreserveSig] int BoundingRectangle(out UiaRectangle value);
    /// <summary>Reserved native COM method; returns its HRESULT without exception translation.</summary>
    [PreserveSig] int GetEmbeddedFragmentRoots([MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_UNKNOWN)] out INativeFragmentRootProvider[]? value);
    /// <summary>Reserved native COM method; returns its HRESULT without exception translation.</summary>
    [PreserveSig] int SetFocus();
    /// <summary>Reserved native COM method; returns its HRESULT without exception translation.</summary>
    [PreserveSig] int FragmentRoot(out INativeFragmentRootProvider? value);
}
/// <summary>Reserved Windows UI Automation HRESULT ABI; not an application extension point.</summary>
[ComVisible(true), EditorBrowsable(EditorBrowsableState.Never), Guid("620ce2a5-ab8f-40a9-86cb-de3c75599b58"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface INativeFragmentRootProvider
{
    /// <summary>Reserved native COM method; returns its HRESULT without exception translation.</summary>
    [PreserveSig] int ElementProviderFromPoint(double x, double y, out INativeFragmentProvider? value);
    /// <summary>Reserved native COM method; returns its HRESULT without exception translation.</summary>
    [PreserveSig] int GetFocus(out INativeFragmentProvider? value);
}
/// <summary>Reserved Windows UI Automation HRESULT ABI; not an application extension point.</summary>
[ComVisible(true), EditorBrowsable(EditorBrowsableState.Never), Guid("54fcb24b-e18e-47a2-b4d3-eccbe77599a2"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface INativeInvokeProvider
{
    /// <summary>Reserved native COM method; returns its HRESULT without exception translation.</summary>
    [PreserveSig] int Invoke();
}
/// <summary>Reserved Windows UI Automation HRESULT ABI; not an application extension point.</summary>
[ComVisible(true), EditorBrowsable(EditorBrowsableState.Never), Guid("56d00bd0-c4f4-433c-a836-1a52a57e0892"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface INativeToggleProvider
{
    /// <summary>Reserved native COM method; returns its HRESULT without exception translation.</summary>
    [PreserveSig] int Toggle();
    /// <summary>Reserved native COM method; returns its HRESULT without exception translation.</summary>
    [PreserveSig] int ToggleState(out int value);
}
/// <summary>Reserved Windows UI Automation HRESULT ABI; not an application extension point.</summary>
[ComVisible(true), EditorBrowsable(EditorBrowsableState.Never), Guid("fb8b03af-3bdf-48d4-bd36-1a65793be168"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface INativeSelectionProvider
{
    /// <summary>Reserved native COM method; returns its HRESULT without exception translation.</summary>
    [PreserveSig] int GetSelection([MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_UNKNOWN)] out INativeRawProvider[]? value);
    /// <summary>Reserved native COM method; returns its HRESULT without exception translation.</summary>
    [PreserveSig] int CanSelectMultiple([MarshalAs(UnmanagedType.Bool)] out bool value);
    /// <summary>Reserved native COM method; returns its HRESULT without exception translation.</summary>
    [PreserveSig] int IsSelectionRequired([MarshalAs(UnmanagedType.Bool)] out bool value);
}
/// <summary>Reserved Windows UI Automation HRESULT ABI; not an application extension point.</summary>
[ComVisible(true), EditorBrowsable(EditorBrowsableState.Never), Guid("2acad808-b2d4-452d-a407-91ff1ad167b2"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface INativeSelectionItemProvider
{
    /// <summary>Reserved native COM method; returns its HRESULT without exception translation.</summary>
    [PreserveSig] int Select();
    /// <summary>Reserved native COM method; returns its HRESULT without exception translation.</summary>
    [PreserveSig] int AddToSelection();
    /// <summary>Reserved native COM method; returns its HRESULT without exception translation.</summary>
    [PreserveSig] int RemoveFromSelection();
    /// <summary>Reserved native COM method; returns its HRESULT without exception translation.</summary>
    [PreserveSig] int IsSelected([MarshalAs(UnmanagedType.Bool)] out bool value);
    /// <summary>Reserved native COM method; returns its HRESULT without exception translation.</summary>
    [PreserveSig] int SelectionContainer(out INativeRawProvider? value);
}
/// <summary>Reserved Windows UI Automation HRESULT ABI; not an application extension point.</summary>
[ComVisible(true), EditorBrowsable(EditorBrowsableState.Never), Guid("d847d3a5-cab0-4a98-8c32-ecb45c59ad24"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface INativeExpandProvider
{
    /// <summary>Reserved native COM method; returns its HRESULT without exception translation.</summary>
    [PreserveSig] int Expand();
    /// <summary>Reserved native COM method; returns its HRESULT without exception translation.</summary>
    [PreserveSig] int Collapse();
    /// <summary>Reserved native COM method; returns its HRESULT without exception translation.</summary>
    [PreserveSig] int ExpandCollapseState(out int value);
}

[ComVisible(true), ClassInterface(ClassInterfaceType.None), ComDefaultInterface(typeof(INativeRawProvider))]
internal class RibbonNativeProvider(RibbonAutomationNode node) : INativeRawProvider, INativeFragmentProvider,
    INativeInvokeProvider, INativeToggleProvider, INativeSelectionProvider, INativeSelectionItemProvider, INativeExpandProvider
{
    internal const int Unavailable = unchecked((int)0x80040201), Disabled = unchecked((int)0x80040200);
    protected RibbonAutomationRoot Root => node.OwnerRoot;
    protected int Read<T>(Func<T> action, out T value, bool enabled = false)
    {
        int status = Root.ReadNative(() =>
        {
            if (node.Element is { } element && (element.IsDisposed || !Root.Ribbon.AllElements.Contains(element)))
                return (Status: Unavailable, Value: default(T)!);
            if (enabled && (node.Element is { EffectiveEnabled: false } || !Root.Control.Enabled))
                return (Status: Disabled, Value: default(T)!);
            return (Status: 0, Value: action());
        }, out var result);
        value = status < 0 ? default! : result.Value;
        return status < 0 ? status : result.Status;
    }
    private int Run(Action action) => Read(() => { action(); return 0; }, out _, enabled: true);
    protected static RibbonNativeProvider? Wrap(object? value) => (value as RibbonAutomationNode)?.NativeProvider;
    public int ProviderOptions(out int value) { value = 2; return 0; }
    public int GetPatternProvider(int id, out object? value) => Read<object?>(() => Wrap(node.GetPatternProvider(id)), out value);
    public int GetPropertyValue(int id, out object? value) => Read(() => node.GetPropertyValue(id), out value);
    public int HostRawElementProvider(out INativeRawProvider? value)
    {
        int status = Root.ReadNative(() =>
        {
            INativeRawProvider? host = null;
            int result = node is RibbonAutomationRoot ? UiaNative.UiaNativeHostProviderFromHwnd(Root.Control.Handle, out host) : 0;
            return (Status: result, Value: host);
        }, out var result);
        value = status < 0 ? null : result.Value; return status < 0 ? status : result.Status;
    }
    public int Navigate(int direction, out INativeFragmentProvider? value) => Read<INativeFragmentProvider?>(() => Wrap(node.Navigate(direction)), out value);
    public int GetRuntimeId(out int[]? value) => Read<int[]?>(node.GetRuntimeId, out value);
    public int BoundingRectangle(out UiaRectangle value) => Read(() => node.BoundingRectangle, out value);
    public int GetEmbeddedFragmentRoots(out INativeFragmentRootProvider[]? value) => Read<INativeFragmentRootProvider[]?>(() => null, out value);
    public int SetFocus() => Run(node.SetFocus);
    public int FragmentRoot(out INativeFragmentRootProvider? value) => Read<INativeFragmentRootProvider?>(() => (INativeFragmentRootProvider)Root.NativeProvider, out value);
    public int Invoke() => Run(node.Invoke);
    public int Toggle() => Run(node.Toggle);
    public int ToggleState(out int value) => Read(() => node.ToggleState, out value);
    public int GetSelection(out INativeRawProvider[]? value) => Read<INativeRawProvider[]?>(() => node.GetSelection().Select(item => (INativeRawProvider)Wrap(item)!).ToArray(), out value);
    public int CanSelectMultiple(out bool value) => Read(() => node.CanSelectMultiple, out value);
    public int IsSelectionRequired(out bool value) => Read(() => node.IsSelectionRequired, out value);
    public int Select() => Run(node.Select);
    public int AddToSelection() => Run(node.AddToSelection);
    public int RemoveFromSelection()
    {
        int status = Read(() => unchecked((int)0x80131509), out int result, enabled: true);
        return status < 0 ? status : result;
    }
    public int IsSelected(out bool value) => Read(() => node.IsSelected, out value);
    public int SelectionContainer(out INativeRawProvider? value) => Read<INativeRawProvider?>(() => Wrap(node.SelectionContainer), out value);
    public int Expand() => Run(node.Expand);
    public int Collapse() => Run(node.Collapse);
    public int ExpandCollapseState(out int value) => Read(() => node.ExpandCollapseState, out value);
}

[ComVisible(true), ClassInterface(ClassInterfaceType.None), ComDefaultInterface(typeof(INativeRawProvider))]
internal sealed class RibbonNativeRootProvider(RibbonAutomationRoot root) : RibbonNativeProvider(root), INativeFragmentRootProvider
{
    public int ElementProviderFromPoint(double x, double y, out INativeFragmentProvider? value) => Read<INativeFragmentProvider?>(() => Wrap(Root.ElementProviderFromPoint(x, y)), out value);
    public int GetFocus(out INativeFragmentProvider? value) => Read<INativeFragmentProvider?>(() => Wrap(Root.GetFocus()), out value);
}
