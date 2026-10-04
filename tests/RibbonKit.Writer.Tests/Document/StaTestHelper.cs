using System.Threading;
using System.Windows.Threading;

namespace RibbonKit.Writer.Tests.Document;

internal static class StaTestHelper
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static readonly Lazy<Task<Dispatcher>> SharedDispatcher = new(StartDispatcher);

    public static void Run(Action action) => RunAsync(() =>
    {
        action();
        return Task.CompletedTask;
    }).GetAwaiter().GetResult();

    public static async Task RunAsync(Func<Task> action, TimeSpan? timeout = null)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (SharedDispatcher.IsValueCreated && SharedDispatcher.Value.IsCompletedSuccessfully
            && SharedDispatcher.Value.Result.CheckAccess())
        {
            // Existing persistence fixtures compose STA helpers inside another STA test.
            // They already own the gate and must continue on that dispatcher.
            await action();
            return;
        }
        // WPF caches theme Freezables (including unfrozen WindowChrome) for the process.
        // Keep all Writer UI tests on their owning dispatcher, with one test active at a time.
        await Gate.WaitAsync().ConfigureAwait(false);
        try
        {
            var dispatcher = await SharedDispatcher.Value.ConfigureAwait(false);
            var completion = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
            _ = dispatcher.BeginInvoke(async () =>
            {
                try { await action(); completion.TrySetResult(null); }
                catch (Exception ex) { completion.TrySetException(ex); }
            });
            await completion.Task.WaitAsync(timeout ?? Timeout).ConfigureAwait(false);
        }
        finally { Gate.Release(); }
    }

    private static Task<Dispatcher> StartDispatcher()
    {
        var ready = new TaskCompletionSource<Dispatcher>(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            var dispatcher = Dispatcher.CurrentDispatcher;
            SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(dispatcher));
            ready.TrySetResult(dispatcher);
            Dispatcher.Run();
        }) { IsBackground = true, Name = "Writer test UI" };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return ready.Task;
    }
}
