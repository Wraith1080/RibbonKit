using System.ComponentModel.Design;
using System.Drawing.Drawing2D;
using Microsoft.DotNet.DesignTools.Designers.Behaviors;

namespace RibbonKit.WinForms.Design;

internal sealed class RibbonSurfaceAdornment : IDisposable
{
    private readonly RibbonControl _ribbon;
    private readonly RibbonDesignSelection _selection;
    private readonly BehaviorService _service;
    private readonly Adorner _adorner = new();
    internal RibbonSurfaceAdornment(RibbonControl ribbon, RibbonDesignSelection selection, BehaviorService service)
    {
        _ribbon = ribbon; _selection = selection; _service = service;
        _adorner.Glyphs.Add(new SurfaceGlyph(ribbon, selection, service));
        service.Adorners.Add(_adorner);
        ribbon.Invalidated += Invalidated; ribbon.LocationChanged += Changed; ribbon.Layout += LayoutChanged;
        selection.Changed += Changed;
    }
    private void Invalidated(object? sender, InvalidateEventArgs e) => _adorner.Invalidate();
    private void Changed(object? sender, EventArgs e) => _adorner.Invalidate();
    private void LayoutChanged(object? sender, LayoutEventArgs e) => _adorner.Invalidate();
    public void Dispose()
    {
        _ribbon.Invalidated -= Invalidated; _ribbon.LocationChanged -= Changed; _ribbon.Layout -= LayoutChanged;
        _selection.Changed -= Changed; _service.Adorners.Remove(_adorner); _adorner.Glyphs.Clear();
    }
    private sealed class SurfaceGlyph(RibbonControl ribbon, RibbonDesignSelection selection, BehaviorService service)
        : Glyph(new SelectionBehavior(ribbon, selection, service))
    {
        public override Rectangle Bounds => service.ControlRectInAdornerWindow(ribbon);
        public override Cursor? GetHitTest(Point point)
        {
            var origin = service.ControlToAdornerWindow(ribbon);
            return selection.Active && selection.CanSelectAt(new(point.X - origin.X, point.Y - origin.Y)) ? Cursors.Hand : null;
        }
        public override void Paint(PaintEventArgs e)
        {
            if (!selection.Active || selection.SelectionBounds(selection.Selected) is not { IsEmpty: false } bounds) return;
            var origin = service.ControlToAdornerWindow(ribbon); bounds.Offset(origin);
            // Selection decoration belongs to the designer, never the renderer
            // or authored component. It has no resize/reorder semantics.
            var state = e.Graphics.Save();
            try
            {
                var clip = ribbon.ClientRectangle; clip.Offset(origin); e.Graphics.SetClip(clip, CombineMode.Intersect);
                using var pen = new Pen(SystemColors.Highlight, Math.Max(1, ribbon.DeviceDpi / 96f)) { DashStyle = DashStyle.Dash };
                bounds.Width = Math.Max(0, bounds.Width - 1); bounds.Height = Math.Max(0, bounds.Height - 1);
                e.Graphics.DrawRectangle(pen, bounds);
            }
            finally { e.Graphics.Restore(state); }
        }
    }
    private sealed class SelectionBehavior(RibbonControl ribbon, RibbonDesignSelection selection, BehaviorService service) : Behavior
    {
        public override bool OnMouseDown(Glyph? glyph, MouseButtons button, Point mouseLoc)
        {
            if (button != MouseButtons.Left) return false;
            var origin = service.ControlToAdornerWindow(ribbon);
            return selection.SelectAt(new(mouseLoc.X - origin.X, mouseLoc.Y - origin.Y));
        }
        public override bool OnMouseUp(Glyph? glyph, MouseButtons button, Point mouseLoc) => button == MouseButtons.Left;
        public override bool OnMouseDoubleClick(Glyph? glyph, MouseButtons button, Point mouseLoc) => button == MouseButtons.Left;
    }
}
