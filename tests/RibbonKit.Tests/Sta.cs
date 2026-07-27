using System.Runtime.ExceptionServices;
using System.Windows.Threading;

namespace RibbonKit.Tests;

/// <summary>
/// Runs a test body on a dedicated STA thread with a live <see cref="Dispatcher"/>.
/// </summary>
/// <remarks>
/// <para>
/// WPF objects are thread-affine and must be created on an STA thread, but xunit's runner threads
/// are MTA — hence the thread per test rather than a shared fixture, which also keeps one test's
/// leftover dispatcher queue out of the next test's <see cref="Drain"/>.
/// </para>
/// <para>
/// No WPF Application is started and no window is shown: these tests exercise control
/// LOGIC (property callbacks, layout passes, deferred dispatcher work), not rendering, so they run
/// unattended on a CI agent. Anything that genuinely needs a rendered popup belongs in the manual
/// showcase checklist instead.
/// </para>
/// </remarks>
internal static class Sta
{
    private static readonly TimeSpan BodyTimeout = TimeSpan.FromSeconds(30);

    /// <summary>Runs <paramref name="body"/> on an STA thread, rethrowing any failure here.</summary>
    public static void Run(Action body)
    {
        ExceptionDispatchInfo? failure = null;

        var thread = new Thread(() =>
        {
            try
            {
                body();
            }
            catch (Exception ex)
            {
                // Captured rather than rethrown, so the assertion's original stack survives the
                // hop back to the runner thread.
                failure = ExceptionDispatchInfo.Capture(ex);
            }
            finally
            {
                Dispatcher.CurrentDispatcher.InvokeShutdown();
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.IsBackground = true;
        thread.Start();

        if (!thread.Join(BodyTimeout))
        {
            throw new TimeoutException(
                $"The STA test body did not finish within {BodyTimeout.TotalSeconds:0}s — most likely " +
                "it is waiting on dispatcher work that never gets pumped.");
        }

        failure?.Throw();
    }

    /// <summary>
    /// Runs everything already queued on this thread's dispatcher at <paramref name="priority"/>
    /// or above, then returns.
    /// </summary>
    /// <remarks>
    /// RibbonKit defers state changes that must not happen mid-dispatch (returning borrowed menu
    /// items, publishing overflow) to <see cref="DispatcherPriority.Background"/>. Nothing pumps
    /// the queue in a test — there is no message loop — so a test that asserts on the result of
    /// deferred work must call this first. Invoking an empty callback at the same priority is the
    /// pump: same-priority operations run in the order they were queued, so ours going last means
    /// everything before it has already run.
    /// </remarks>
    public static void Drain(DispatcherPriority priority = DispatcherPriority.Background) =>
        Dispatcher.CurrentDispatcher.Invoke(() => { }, priority);
}
