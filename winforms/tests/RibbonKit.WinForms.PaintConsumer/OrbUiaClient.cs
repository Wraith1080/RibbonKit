using System.Runtime.InteropServices;

// Standalone Windows UIAutomationCore client. GUIDs and vtable order follow the
// installed Windows SDK UIAutomationClient.h; no WPF UIA assemblies or sample helpers.
internal static class OrbUiaClient
{
    internal static int Run(string handle, string fileId, string output)
    {
        try
        {
            Task.Run(() => Check((nint)long.Parse(handle, System.Globalization.CultureInfo.InvariantCulture), fileId)).GetAwaiter().GetResult();
            File.WriteAllText(output, "PASS: external UIAutomationCore raw tree contains one File with stable identity, name/access key and ExpandCollapse; Expand/Collapse report the real menu state.");
            return 0;
        }
        catch (Exception error) { File.WriteAllText(output, "FAIL external UIA: " + error); return 1; }
    }
    private static void Check(nint handle, string fileId)
    {
        var automation = (Automation)Activator.CreateInstance(Type.GetTypeFromCLSID(new("FF48DBA4-60EF-4201-AA87-54103EEF594E"), throwOnError: true)!)!;
        Hr(automation.ElementFromHandle(handle, out var root)); Hr(automation.RawViewWalker(out var walker));
        Hr(walker.First(root, out var child)); Element? file = null; int count = 0;
        while (child is not null)
        {
            Hr(child.Property(30011, out var id));
            if (Equals(id, fileId)) { file = child; count++; }
            Hr(walker.Next(child, out child));
        }
        if (count != 1 || file is null) throw new InvalidOperationException("Expected exactly one File child, got " + count);
        Hr(file.Property(30005, out var name)); Hr(file.Property(30006, out var access));
        if (!Equals(name, "File") || !Equals(access, "Alt, F")) throw new InvalidOperationException("File metadata changed.");
        Hr(file.RuntimeId(out var before)); Hr(file.Pattern(10005, out var pattern));
        var expand = (ExpandCollapse)pattern; Hr(expand.State(out int state)); if (state != 0) throw new InvalidOperationException("File begins expanded.");
        Hr(expand.Expand()); Hr(expand.State(out state)); if (state != 1) throw new InvalidOperationException("File did not expand.");
        Hr(expand.Collapse()); Hr(expand.State(out state)); if (state != 0) throw new InvalidOperationException("File did not collapse.");
        Hr(file.RuntimeId(out var after)); if (!before.SequenceEqual(after)) throw new InvalidOperationException("File identity changed.");
    }
    private static void Hr(int value) => Marshal.ThrowExceptionForHR(value);

    [ComImport, Guid("30cbe57d-d9d0-452a-ab13-7ac5ac4825ee"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface Automation
    {
        [PreserveSig] int CompareElements();
        [PreserveSig] int CompareRuntimeIds();
        [PreserveSig] int Root();
        [PreserveSig] int ElementFromHandle(nint hwnd, [MarshalAs(UnmanagedType.Interface)] out Element element);
        [PreserveSig] int ElementFromPoint();
        [PreserveSig] int Focused();
        [PreserveSig] int RootCache();
        [PreserveSig] int HandleCache();
        [PreserveSig] int PointCache();
        [PreserveSig] int FocusCache();
        [PreserveSig] int CreateWalker();
        [PreserveSig] int ControlWalker();
        [PreserveSig] int ContentWalker();
        [PreserveSig] int RawViewWalker([MarshalAs(UnmanagedType.Interface)] out Walker walker);
    }
    [ComImport, Guid("4042c624-389c-4afc-a630-9df854a541fc"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface Walker
    {
        [PreserveSig] int Parent(Element element, out Element? result);
        [PreserveSig] int First(Element element, out Element? result);
        [PreserveSig] int Last(Element element, out Element? result);
        [PreserveSig] int Next(Element element, out Element? result);
    }
    [ComImport, Guid("d22108aa-8ac5-49a5-837b-37bbb3d7591e"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface Element
    {
        [PreserveSig] int SetFocus();
        [PreserveSig] int RuntimeId([MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_I4)] out int[] runtimeId);
        [PreserveSig] int FindFirst();
        [PreserveSig] int FindAll();
        [PreserveSig] int FirstCache();
        [PreserveSig] int AllCache();
        [PreserveSig] int UpdatedCache();
        [PreserveSig] int Property(int propertyId, [MarshalAs(UnmanagedType.Struct)] out object value);
        [PreserveSig] int PropertyEx();
        [PreserveSig] int CachedProperty();
        [PreserveSig] int CachedPropertyEx();
        [PreserveSig] int PatternAs();
        [PreserveSig] int CachedPatternAs();
        [PreserveSig] int Pattern(int patternId, [MarshalAs(UnmanagedType.IUnknown)] out object pattern);
    }
    [ComImport, Guid("619be086-1f4e-4ee4-bafa-210128738730"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface ExpandCollapse
    {
        [PreserveSig] int Expand();
        [PreserveSig] int Collapse();
        [PreserveSig] int State(out int state);
    }
}
