using System.Runtime.InteropServices;
using RibbonKit.WinForms.Accessibility;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class NativeProviderBoundaryTests
{
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int PatternQuery(nint self, int id, out nint value);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int PropertyQuery(nint self, int id, [MarshalAs(UnmanagedType.Struct)] out object? value);

    [Theory] [InlineData(false, false)] [InlineData(true, false)] [InlineData(false, true)] [InlineData(true, true)]
    public void Closed_popup_returns_native_HRESULT_without_a_managed_exception(bool pattern, bool child) => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { Visible = false };
        using var menu = new RibbonDropDownItem(); var item = new RibbonButtonItem { Text = "Choice" }; menu.Items.Add(item);
        using var surface = new RibbonPopupSurface(ribbon, menu, new(1000, 700)) { Visible = false };
        _ = ribbon.Handle; _ = surface.Handle;
        var root = surface.AutomationRoot; var provider = (child ? root.Node(item) : root).NativeProvider;
        nint raw = Marshal.GetComInterfaceForObject(provider, typeof(INativeRawProvider));
        var getPattern = Marshal.GetDelegateForFunctionPointer<PatternQuery>(Marshal.ReadIntPtr(Marshal.ReadIntPtr(raw), 4 * IntPtr.Size));
        var getProperty = Marshal.GetDelegateForFunctionPointer<PropertyQuery>(Marshal.ReadIntPtr(Marshal.ReadIntPtr(raw), 5 * IntPtr.Size));
        int result = 0, exceptions = 0; Exception? failure = null;
        void FirstChance(object? sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
        { if (e.Exception is COMException { ErrorCode: RibbonNativeProvider.Unavailable } or ObjectDisposedException) Interlocked.Increment(ref exceptions); }
        AppDomain.CurrentDomain.FirstChanceException += FirstChance;
        try
        {
            var worker = new Thread(() =>
            {
                try
                {
                    if (pattern) { result = getPattern(raw, 10000, out nint value); Assert.Equal(0, value); }
                    else { result = getProperty(raw, 30010, out var value); Assert.Null(value); }
                }
                catch (Exception error) { failure = error; }
            }) { IsBackground = true };
            worker.Start();
            Assert.True(SpinWait.SpinUntil(() => (worker.ThreadState & ThreadState.WaitSleepJoin) != 0 || !worker.IsAlive, 3000));
            surface.Dispose();
            var watch = System.Diagnostics.Stopwatch.StartNew();
            while (worker.IsAlive && watch.ElapsedMilliseconds < 3000) { Application.DoEvents(); Thread.Yield(); }
            Assert.True(worker.Join(100)); Assert.Null(failure);
            Assert.Equal(RibbonNativeProvider.Unavailable, result); Assert.Equal(0, exceptions);
            // A subsequent direct COM query must be exception-free as well.
            Assert.Equal(RibbonNativeProvider.Unavailable, getPattern(raw, 10000, out _));
            Assert.Equal(0, exceptions);
        }
        finally { AppDomain.CurrentDomain.FirstChanceException -= FirstChance; Marshal.Release(raw); }
    });

    [Fact]
    public void Native_boundary_preserves_live_patterns_and_returns_disabled_without_throwing() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl(); var item = new RibbonButtonItem { Text = "Action" };
        var group = new RibbonGroup(); group.Items.Add(item); var tab = new RibbonTab(); tab.Groups.Add(group); ribbon.Tabs.Add(tab); _ = ribbon.Handle;
        var node = ribbon.AutomationRoot.Node(item); var native = node.NativeProvider;
        Assert.Equal(0, native.GetPropertyValue(30005, out var name)); Assert.Equal("Action", name);
        Assert.Equal(0, native.GetPatternProvider(10000, out var pattern)); Assert.Same(native, pattern);
        item.Enabled = false;
        Assert.Equal(RibbonNativeProvider.Disabled, native.Invoke());
        Assert.Equal(0, native.GetPropertyValue(30010, out var enabled)); Assert.Equal(false, enabled);
        item.Dispose(); Assert.Equal(RibbonNativeProvider.Unavailable, native.GetPropertyValue(30005, out _));
    });
}
