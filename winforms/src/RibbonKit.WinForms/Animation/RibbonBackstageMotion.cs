using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

// Navigation uses an inert drawing; application pages fade through their native
// parent HWND's opacity. Transparent closing fades already composed screen pixels
// as one panel; it never prints application controls or changes their parents.
internal sealed class RibbonBackstageMotion : IDisposable
{
    private enum Phase { Opening, Page, Closing }
    private readonly Phase _phase;
    private readonly RibbonControl _ribbon;
    private readonly RibbonBackstageSurface _surface;
    private readonly Form _form;
    private readonly Control[] _controls;
    private readonly List<RibbonDrawingHold> _holds = [];
    private RibbonDrawingHold? _surfaceHold;
    private readonly Control _target;
    private Action? _completed;
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 8 };
    private readonly ColorMatrix _fade = new();
    private readonly bool _expressive;
    private bool _snapshotClosing, _surfaceHidden;
    private readonly double _duration;
    private readonly int _travel;
    private readonly Point _initialPointer = Cursor.Position;
    private RibbonMotionWindow? _window;
    private Bitmap? _image;
    private SolidBrush? _background;
    private long _start;
    private bool _disposed, _subscribed;
    internal bool IsRunning => !_disposed && (_phase == Phase.Opening || _snapshotClosing ? _image is not null : _surfaceHold is not null);
    internal bool IsClosing => _phase == Phase.Closing;
    internal Rectangle Bounds { get; private set; }
    private RibbonBackstageMotion(RibbonControl ribbon, RibbonBackstageSurface surface, Form form, Control[] controls, Phase phase = Phase.Opening)
    {
        _ribbon = ribbon; _surface = surface; _form = form; _controls = controls;
        _phase = phase; _target = surface;
        _expressive = ribbon.AnimationLevel == RibbonAnimationLevel.Expressive; _duration = _expressive ? 308 : 220;
        _travel = RibbonLayoutEngine.Scale(_expressive ? 50 : 28, ribbon.DeviceDpi); _timer.Tick += Tick;
    }
    internal static RibbonBackstageMotion? Prepare(RibbonControl ribbon, RibbonBackstageSurface surface, Form form)
    {
        var bounds = surface.NavigationBounds;
        if (ribbon.AnimationLevel == RibbonAnimationLevel.None || !ribbon.CanAnimatePopupOpening || !ribbon.Motion.SystemEnabled || form.WindowState == FormWindowState.Minimized ||
            form.Opacity != 1 || form.TransparencyKey != Color.Empty || bounds.Width < 1 || bounds.Height < 1 || (long)bounds.Width * bounds.Height > 4_194_304) return null;
        var controls = surface.NavigationControls;
        if (surface.HasApplicationOpacity || RibbonDrawingHold.HasOpacity(surface.PageArea.Handle) || controls.Any(c => RibbonDrawingHold.HasOpacity(c.Handle))) return null;
        var motion = new RibbonBackstageMotion(ribbon, surface, form, controls);
        try { motion._surfaceHold = surface.AcquireMotionOpacity(1); foreach (var control in controls) motion._holds.Add(RibbonDrawingHold.Create(control.Handle)); return motion; }
        catch (Win32Exception error) { Trace.WriteLine("Backstage motion skipped: " + error.Message); motion.Dispose(); return null; }
        catch { motion.Dispose(); throw; }
    }
    internal static RibbonBackstageMotion? PrepareFade(RibbonControl ribbon, RibbonBackstageSurface surface, Form form, bool closing, Action? completed = null)
    {
        Control target = surface;
        if (ribbon.AnimationLevel == RibbonAnimationLevel.None || !ribbon.CanAnimatePopupOpening || !ribbon.Motion.SystemEnabled || form.WindowState == FormWindowState.Minimized ||
            form.Opacity != 1 || form.TransparencyKey != Color.Empty || target.Width < 1 || target.Height < 1 || surface.HasApplicationOpacity) return null;
        bool snapshot = closing && surface.TransparentNavigation;
        if (snapshot && ((long)surface.Width * surface.Height > 4_194_304 || !SystemInformation.VirtualScreen.Contains(surface.RectangleToScreen(surface.ClientRectangle)))) return null;
        var motion = new RibbonBackstageMotion(ribbon, surface, form, closing ? [] : surface.NavigationControls, closing ? Phase.Closing : Phase.Page)
            { _snapshotClosing = snapshot };
        try
        {
            if (!snapshot) motion._surfaceHold = surface.AcquireMotionOpacity(closing ? (byte)255 : (byte)1);
            if (surface.TransparentNavigation && !closing)
                foreach (var control in motion._controls) motion._holds.Add(RibbonDrawingHold.Create(control.Handle));
            motion._completed = completed; return motion;
        }
        catch (Win32Exception error) { Trace.WriteLine("Backstage fade skipped: " + error.Message); motion.Dispose(); return null; }
        catch { motion.Dispose(); throw; }
    }
    internal void Start()
    {
        if (_disposed) return;
        if (!_surface.Visible || !_ribbon.IsBackstageOpen) { Dispose(); return; }
        try
        {
            bool navigation = _phase != Phase.Closing;
            Bounds = navigation ? _surface.RectangleToScreen(_surface.NavigationBounds) : _target.RectangleToScreen(_target.ClientRectangle);
            if (!_form.RectangleToScreen(_form.ClientRectangle).Contains(Bounds)) { Dispose(); return; }
            if (_snapshotClosing)
            {
                // Copy the visible, composed result once. WM_PRINT/DrawToBitmap
                // would ask native TextBox/RichTextBox children to draw again.
                _image = new(Bounds.Width, Bounds.Height, PixelFormat.Format24bppRgb);
                using (var graphics = Graphics.FromImage(_image)) graphics.CopyFromScreen(Bounds.Location, Point.Empty, Bounds.Size);
                _background = new(Color.Transparent); _window = new(_form, clipToOwner: _form is RibbonForm);
                _window.RenderBackstage(this, 0);
                _surfaceHidden = true; _surface.Visible = false;
            }
            else if (navigation)
            {
                _image = new(Bounds.Width, Bounds.Height, _surface.TransparentNavigation ? PixelFormat.Format32bppPArgb : PixelFormat.Format24bppRgb);
                // Printing a container can reach application descendants even when
                // PRF_CHILDREN is omitted. Paint only the background directly.
                if (_surface.TransparentNavigation)
                {
                    using var buffer = new RibbonBackdropBuffer();
                    using (var graphics = Graphics.FromImage(_image))
                        buffer.Paint(graphics, _image.Size, _image.Height, Color.Transparent, _surface.PaintNavigation);
                    using var copy = Graphics.FromImage(_image); copy.CompositingMode = CompositingMode.SourceCopy; copy.DrawImageUnscaled(buffer.Image, Point.Empty);
                }
                else
                {
                    using (var graphics = Graphics.FromImage(_image))
                        _ribbon.Painter.DrawBackstageBackground(new(graphics, _surface.ClientRectangle, _ribbon.Theme, _surface.Font, _surface.DeviceDpi) { Density = _ribbon.Density }, Bounds.Width);
                    RibbonMotionScene.CaptureChildren(_ribbon, _controls, _image);
                }
                if (_disposed) return;
                _background = new(_surface.TransparentNavigation ? Color.Transparent : _ribbon.Theme.BackstageNavigationBackground); _window = new(_form, clipToOwner: _form is RibbonForm); _window.RenderBackstage(this, 0);
                if (_surface.TransparentNavigation) _surface.MaskNavigation(true);
            }
            _start = Stopwatch.GetTimestamp();
            _surface.ClientSizeChanged += Unsafe; _surface.HandleDestroyed += Unsafe; _surface.VisibleChanged += Visibility; _surface.Disposed += Unsafe;
            _form.LocationChanged += Unsafe; _form.Deactivate += Unsafe; _form.HandleDestroyed += Unsafe;
            _subscribed = true; _timer.Start();
        }
        catch (Win32Exception error) { Trace.WriteLine("Backstage motion skipped: " + error.Message); Dispose(); }
        catch { Dispose(); throw; }
    }
    internal void Draw(Graphics graphics, ImageAttributes alpha, double progress)
    {
        graphics.CompositingMode = CompositingMode.SourceCopy; graphics.Clear(Color.Transparent); graphics.CompositingMode = CompositingMode.SourceOver;
        graphics.FillRectangle(_background!, new Rectangle(Point.Empty, Bounds.Size));
        double p = _phase == Phase.Page ? 1 : 1 - Math.Pow(1 - Math.Clamp(progress, 0, 1), _expressive ? 5 : 3);
        _fade.Matrix33 = (float)(_phase == Phase.Closing ? 1 - p : p); alpha.SetColorMatrix(_fade);
        graphics.DrawImage(_image!, new Rectangle(_phase == Phase.Closing ? 0 : -(int)Math.Round(_travel * (1 - p)), 0, _image!.Width, _image.Height), 0, 0, _image.Width, _image.Height, GraphicsUnit.Pixel, alpha);
    }
    private void Tick(object? sender, EventArgs e)
    {
        if (!IsRunning || !_ribbon.Motion.SystemEnabled || _ribbon.AnimationLevel == RibbonAnimationLevel.None) { Dispose(); return; }
        double progress = Stopwatch.GetElapsedTime(_start).TotalMilliseconds / _duration;
        try
        {
            double p = 1 - Math.Pow(1 - Math.Clamp(progress, 0, 1), _expressive ? 5 : 3);
            _surfaceHold?.SetOpacity((byte)Math.Clamp((int)Math.Round(255 * (_phase == Phase.Closing ? 1 - p : p)), 1, 255));
            if (_window is not null) _window.RenderBackstage(this, Math.Min(1, progress));
        }
        catch { Dispose(); throw; }
        if (progress >= 1) Dispose();
    }
    internal bool ObserveInput(ref Message message)
    {
        var control = Control.FromChildHandle(message.HWnd);
        if (!IsRunning || control?.FindForm() != _form) return false;
        if (message.Msg == 0x200)
        {
            if (Cursor.Position != _initialPointer && Bounds.Contains(Cursor.Position)) Dispose();
        }
        else if (message.Msg is 0x100 or 0x104 or 0x201 or 0x204 or 0x207 or 0x20A or 0x20E or 0x240 or 0x246)
        {
            bool consume = IsClosing && (ReferenceEquals(control, _surface) || _surface.Contains(control) || _snapshotClosing &&
                (message.Msg is 0x100 or 0x104 || Bounds.Contains(Cursor.Position))) && !((message.Msg is 0x100 or 0x104) && (Keys)(int)message.WParam == Keys.F4 && Control.ModifierKeys.HasFlag(Keys.Alt));
            Dispose(); return consume;
        }
        return false;
    }
    internal void Cancel() { _completed = null; Dispose(); }
    private void Unsafe(object? sender, EventArgs e) => Dispose();
    private void Visibility(object? sender, EventArgs e) { if (!_surface.Visible && !_surfaceHidden) Dispose(); }
    public void Dispose()
    {
        if (_disposed) return; _disposed = true; _timer.Stop();
        if (_subscribed)
        {
            _surface.ClientSizeChanged -= Unsafe; _surface.HandleDestroyed -= Unsafe; _surface.VisibleChanged -= Visibility; _surface.Disposed -= Unsafe;
            _form.LocationChanged -= Unsafe; _form.Deactivate -= Unsafe; _form.HandleDestroyed -= Unsafe; _subscribed = false;
        }
        var completed = _completed; _completed = null;
        try { completed?.Invoke(); } // Hide a closing surface before restoring opacity; no final flash.
        finally
        {
            if (_surfaceHidden && !_surface.IsDisposed && _ribbon.IsBackstageOpen)
            { _surface.Visible = true; _surface.BringToFront(); }
            _surface.MaskNavigation(false);
            foreach (var hold in _holds) hold.Dispose(); _holds.Clear();
            if (_surfaceHold is not null || _surfaceHidden) _surface.RestoreMotionOpacity(); _surfaceHold = null;
            _window?.Dispose(); _window = null;
            _image?.Dispose(); _image = null; _background?.Dispose(); _background = null; _timer.Dispose();
        }
    }
}
