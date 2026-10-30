using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Imaging;

namespace RibbonKit.WinForms;

// Keep the already composed document drawing stable while a Solid opening fade
// changes native sibling clipping. The application's controls remain untouched.
internal sealed class RibbonBackstageBackground : IDisposable
{
    private readonly Form _form;
    private readonly Rectangle _bounds;
    private readonly Bitmap _image;
    private Drawing? _drawing;
    private RibbonBackstageBackground(Form form, Rectangle bounds, Bitmap image) => (_form, _bounds, _image) = (form, bounds, image);
    internal static RibbonBackstageBackground? Capture(RibbonControl ribbon, Form form)
    {
        if (!form.ContainsFocus || ribbon.HasWindowBackdrop || ribbon.AnimationLevel == RibbonAnimationLevel.None || !ribbon.CanAnimatePopupOpening || !ribbon.Motion.SystemEnabled ||
            form.Opacity != 1 || form.TransparencyKey != Color.Empty || form.WindowState == FormWindowState.Minimized) return null;
        var client = form.RectangleToScreen(form.ClientRectangle);
        int top = Math.Clamp(ribbon.PointToScreen(new(0, ribbon.Height)).Y, client.Top, client.Bottom);
        var bounds = Rectangle.FromLTRB(client.Left, top, client.Right, client.Bottom);
        if (bounds.Width < 1 || bounds.Height < 1 || (long)bounds.Width * bounds.Height > 4_194_304 || !SystemInformation.VirtualScreen.Contains(bounds)) return null;
        Bitmap? image = null;
        try
        {
            image = new(bounds.Width, bounds.Height, PixelFormat.Format24bppRgb);
            using (var graphics = Graphics.FromImage(image)) graphics.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
            return new(form, bounds, image);
        }
        catch (Win32Exception error) { image?.Dispose(); Trace.WriteLine("Backstage background unavailable: " + error.Message); return null; }
        catch { image?.Dispose(); throw; }
    }
    internal void ShowBehind(Control surface)
    {
        _drawing = new(_image) { Visible = false, Bounds = new(_form.PointToClient(_bounds.Location), _bounds.Size) };
        _form.SuspendLayout();
        try
        {
            _form.Controls.Add(_drawing);
            _form.Controls.SetChildIndex(_drawing, Math.Min(_form.Controls.GetChildIndex(surface) + 1, _form.Controls.Count - 1));
            _drawing.Visible = true; _drawing.Update();
        }
        finally { _form.ResumeLayout(false); }
    }
    public void Dispose() { _drawing?.Dispose(); _drawing = null; _image.Dispose(); }
    private sealed class Drawing : Control
    {
        private readonly Bitmap _image;
        internal Drawing(Bitmap image)
        {
            _image = image; TabStop = false; CausesValidation = false;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            SetStyle(ControlStyles.Selectable, false);
        }
        protected override void OnPaint(PaintEventArgs e) => e.Graphics.DrawImageUnscaled(_image, Point.Empty);
        protected override void WndProc(ref Message message)
        {
            if (message.Msg == 0x84) { message.Result = -1; return; } // Passive drawing, never a native input target.
            base.WndProc(ref message);
        }
    }
}
