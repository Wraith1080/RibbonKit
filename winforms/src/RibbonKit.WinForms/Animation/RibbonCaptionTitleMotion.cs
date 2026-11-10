using System.Diagnostics;

namespace RibbonKit.WinForms;

// Only the title's drawing rectangle moves. Caption controls and native pages
// retain their layout; resize explicitly snaps to the newly measured position.
internal sealed class RibbonCaptionTitleMotion : IDisposable
{
    private readonly RibbonForm _form;
    private readonly RibbonControl _ribbon;
    private readonly Rectangle _from, _to;
    private readonly double _duration;
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 16 };
    private readonly long _start = Stopwatch.GetTimestamp();
    private bool _disposed;
    internal Rectangle Current { get; private set; }
    internal bool IsRunning => !_disposed;
    internal RibbonCaptionTitleMotion(RibbonForm form, RibbonControl ribbon, Rectangle from, Rectangle to)
    {
        _form = form; _ribbon = ribbon; _from = Current = from; _to = to;
        _duration = RibbonBackstageMotion.DurationFor(ribbon.AnimationLevel);
        _timer.Tick += Tick; _timer.Start();
    }
    internal void SetProgress(double progress)
    {
        if (_disposed) return;
        double p = 1 - Math.Pow(1 - Math.Clamp(progress, 0, 1), 3);
        int L(int a, int b) => (int)Math.Round(a + (b - a) * p);
        Current = new(L(_from.X, _to.X), L(_from.Y, _to.Y), L(_from.Width, _to.Width), L(_from.Height, _to.Height));
        _form.InvalidateCaption();
        if (progress >= 1) _form.CompleteCaptionTitleMotion(this);
    }
    private void Tick(object? sender, EventArgs e)
    {
        if (_form.IsDisposed || _ribbon.IsDisposed || _ribbon.AnimationLevel == RibbonAnimationLevel.None || !_ribbon.Motion.SystemEnabled)
        { _form.SnapCaptionTitle(); return; }
        SetProgress(Stopwatch.GetElapsedTime(_start).TotalMilliseconds / _duration);
    }
    public void Dispose() { if (_disposed) return; _disposed = true; _timer.Stop(); _timer.Dispose(); }
}
