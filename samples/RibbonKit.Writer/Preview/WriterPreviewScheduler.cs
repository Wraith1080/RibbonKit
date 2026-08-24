using System.Windows.Threading;

namespace RibbonKit.Writer.Preview;

/// <summary>Schedules a dispatcher-affine preview rebuild.</summary>
public interface IWriterPreviewScheduler
{
    /// <summary>Schedules <paramref name="callback"/> after a trailing-edge delay.</summary>
    IDisposable Schedule(TimeSpan delay, Action callback);
}

/// <summary>Schedules preview rebuilds with a WPF dispatcher timer.</summary>
public sealed class WriterDispatcherPreviewScheduler : IWriterPreviewScheduler
{
    private readonly Dispatcher _dispatcher;

    /// <summary>Creates a scheduler for the supplied dispatcher.</summary>
    public WriterDispatcherPreviewScheduler(Dispatcher dispatcher)
    {
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
    }

    /// <inheritdoc />
    public IDisposable Schedule(TimeSpan delay, Action callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        if (delay < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(delay), delay,
                "The preview debounce delay cannot be negative.");
        if (!_dispatcher.CheckAccess())
            throw new InvalidOperationException("Preview scheduling must occur on the editor dispatcher.");

        if (delay == TimeSpan.Zero)
        {
            var operation = _dispatcher.BeginInvoke(DispatcherPriority.Background, callback);
            return new DispatcherOperationRegistration(operation);
        }

        var timer = new DispatcherTimer(DispatcherPriority.Background, _dispatcher)
        {
            Interval = delay
        };
        TimerRegistration? registration = null;
        EventHandler handler = (_, _) =>
        {
            registration!.Dispose();
            callback();
        };
        timer.Tick += handler;
        registration = new TimerRegistration(timer, handler);
        timer.Start();
        return registration;
    }

    private sealed class TimerRegistration(DispatcherTimer timer, EventHandler handler) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            timer.Stop();
            timer.Tick -= handler;
        }
    }

    private sealed class DispatcherOperationRegistration(DispatcherOperation operation) : IDisposable
    {
        public void Dispose() => operation.Abort();
    }
}
