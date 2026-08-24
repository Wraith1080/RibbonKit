using System.Threading;
using System.Windows.Threading;

namespace RibbonKit.Writer.Tests.Document;

internal static class StaTestHelper
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    public static void Run(Action action) => RunAsync(() =>
    {
        action();
        return Task.CompletedTask;
    }).GetAwaiter().GetResult();

    public static Task RunAsync(Func<Task> action, TimeSpan? timeout = null)
    {
        ArgumentNullException.ThrowIfNull(action);
        var completion = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var dispatcherReady = new TaskCompletionSource<Dispatcher>(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() => RunOnSta(action, completion, dispatcherReady)) { IsBackground = true };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return WaitForCompletionAsync(completion, thread, dispatcherReady.Task, timeout ?? Timeout);
    }

    private static void RunOnSta(Func<Task> action, TaskCompletionSource<object?> completion,
        TaskCompletionSource<Dispatcher> dispatcherReady)
    {
        var dispatcher = Dispatcher.CurrentDispatcher;
        dispatcherReady.TrySetResult(dispatcher);
        SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(dispatcher));
        dispatcher.BeginInvoke(async () =>
        {
            try { await action(); completion.TrySetResult(null); }
            catch (Exception ex) { completion.TrySetException(ex); }
            finally { dispatcher.BeginInvokeShutdown(DispatcherPriority.Normal); }
        });
        Dispatcher.Run();
    }

    private static async Task WaitForCompletionAsync(TaskCompletionSource<object?> completion, Thread thread,
        Task<Dispatcher> dispatcherTask, TimeSpan timeout)
    {
        var completed = await Task.WhenAny(completion.Task, Task.Delay(timeout)).ConfigureAwait(false);
        if (completed != completion.Task)
        {
            if (dispatcherTask.IsCompletedSuccessfully)
                dispatcherTask.Result.BeginInvokeShutdown(DispatcherPriority.Normal);
            throw new TimeoutException($"STA test did not complete within {timeout}.");
        }
        await completion.Task.ConfigureAwait(false);
        if (!thread.Join(timeout))
            throw new TimeoutException("STA test dispatcher did not shut down.");
    }
}
