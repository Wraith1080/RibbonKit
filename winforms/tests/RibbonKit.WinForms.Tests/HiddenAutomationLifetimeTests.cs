using System.Runtime.InteropServices;
using RibbonKit.WinForms.Accessibility;
using Xunit;

namespace RibbonKit.WinForms.Tests;

// Deliberately hidden: emulate a client thread retaining popup providers while
// the owning UI thread closes the popup, without mouse input or visible windows.
public sealed class HiddenAutomationLifetimeTests
{
    [Theory] [InlineData(false, false)] [InlineData(true, false)] [InlineData(false, true)] [InlineData(true, true)]
    public void Queued_popup_query_reports_element_unavailable_when_popup_or_owner_closes(bool pattern, bool owner) => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { Visible = false };
        var menu = new RibbonDropDownItem { Text = "Menu" }; var item = new RibbonButtonItem { Text = "Choice" }; menu.Items.Add(item);
        var tab = new RibbonTab(); var group = new RibbonGroup(); group.Items.Add(menu); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        using var surface = new RibbonPopupSurface(ribbon, menu, new(1000, 700)) { Visible = false };
        _ = ribbon.Handle; _ = surface.Handle;
        var root = surface.AutomationRoot; var node = root.Node(item);
        Exception? failure = null;
        var worker = new Thread(() =>
        {
            try { if (pattern) _ = node.GetPatternProvider(10000); else _ = root.GetPropertyValue(30010); }
            catch (Exception error) { failure = error; }
        }) { IsBackground = true };
        int disposedExceptions = 0, unavailableOnUiThread = 0;
        int uiThread = Environment.CurrentManagedThreadId;
        void FirstChance(object? sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs args)
        {
            if (Thread.CurrentThread.ManagedThreadId == worker.ManagedThreadId && args.Exception is ObjectDisposedException) Interlocked.Increment(ref disposedExceptions);
            if (Environment.CurrentManagedThreadId == uiThread && args.Exception is COMException { ErrorCode: unchecked((int)0x80040201) }) Interlocked.Increment(ref unavailableOnUiThread);
        }
        AppDomain.CurrentDomain.FirstChanceException += FirstChance;
        try
        {
            worker.Start();
            Assert.True(SpinWait.SpinUntil(() => (worker.ThreadState & ThreadState.WaitSleepJoin) != 0 || !worker.IsAlive, 3000), "Client did not queue its synchronous query.");
            if (owner) ribbon.Dispose(); else surface.Dispose();
            var watch = System.Diagnostics.Stopwatch.StartNew();
            while (worker.IsAlive && watch.ElapsedMilliseconds < 3000) { Application.DoEvents(); Thread.Yield(); }
            Assert.True(worker.Join(100), "A stale client query must complete rather than hang.");
        }
        finally { AppDomain.CurrentDomain.FirstChanceException -= FirstChance; }
        var unavailable = Assert.IsType<COMException>(failure);
        Assert.Equal(unchecked((int)0x80040201), unavailable.ErrorCode);
        Assert.Equal(0, unavailableOnUiThread); // Report a stale request on the client, outside the UI dispatcher callback.
        if (!owner) Assert.Equal(0, disposedExceptions); // The reported popup race must not even throw from Control.Invoke.
        Assert.False(ribbon.Visible); Assert.False(surface.Visible);
    });

    [Fact]
    public void Live_provider_errors_are_not_misreported_as_stale_elements() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl(); _ = ribbon.Handle;
        var error = new InvalidOperationException("Consumer error");
        Assert.Same(error, Assert.Throws<InvalidOperationException>(() => ribbon.AutomationRoot.Read<int>(() => throw error)));
    });

    [Fact]
    public void Disconnected_popup_identity_properties_are_unavailable_too() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl(); using var menu = new RibbonDropDownItem { Text = "Menu" };
        using var surface = new RibbonPopupSurface(ribbon, menu, new(1000, 700));
        _ = ribbon.Handle; _ = surface.Handle; var root = surface.AutomationRoot;
        surface.Dispose();
        foreach (int property in new[] { 30003, 30005, 30011, 30012 })
            Assert.Equal(unchecked((int)0x80040201), Assert.Throws<COMException>(() => root.GetPropertyValue(property)).ErrorCode);
    });
}
