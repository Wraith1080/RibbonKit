using System;

namespace RibbonKit.Design;

// The isolated designer must realize a File surface before translating a child
// page property. Use its layout notification rather than a timer or fixed delay.
internal sealed class FilePagePreviewRequest : IDisposable
{
    private readonly Func<bool> _isCurrent;
    private readonly Func<bool> _isReady;
    private readonly Action _apply;
    private readonly Action<EventHandler> _unsubscribe;
    private bool _completed;

    internal FilePagePreviewRequest(Func<bool> isCurrent, Func<bool> isReady, Action apply,
        Action<EventHandler> subscribe, Action<EventHandler> unsubscribe)
    {
        _isCurrent = isCurrent;
        _isReady = isReady;
        _apply = apply;
        _unsubscribe = unsubscribe;
        subscribe(OnLayoutUpdated);
        OnLayoutUpdated(null, EventArgs.Empty);
    }

    private void OnLayoutUpdated(object? sender, EventArgs e)
    {
        if (_completed) return;
        try
        {
            if (!_isCurrent()) { Dispose(); return; }
            if (!_isReady()) return;
            Dispose();
            _apply();
        }
        catch (Exception error)
        {
            Dispose();
            DesignLog.Error("File page preview", error);
        }
    }

    public void Dispose()
    {
        if (_completed) return;
        _completed = true;
        _unsubscribe(OnLayoutUpdated);
    }
}
