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
    private readonly RibbonDesignerRepaint _repaint;
    private readonly RibbonBackstagePreviewLayer _previewLayer;
    private RibbonBackstagePreviewState? _previousPreview;
    private Rectangle _previousExtent, _previousOutline;
    internal RibbonSurfaceAdornment(RibbonControl ribbon, RibbonDesignSelection selection, BehaviorService service)
    {
        _ribbon = ribbon; _selection = selection; _service = service;
        _previewLayer = new(selection.Backstage.Surface);
        _repaint = new(action =>
        {
            var surface = selection.Backstage.Surface;
            if (surface.IsHandleCreated) surface.BeginInvoke(action);
            else action(); // Do not force native handles during designer loading.
        }, region =>
        {
            if (selection.Backstage.IsOpen)
                _previewLayer.Present(selection.Backstage.Bounds, () => selection.Backstage.CreateFrame(ribbon.DeviceDpi, ribbon.DeviceDpi));
            else _previewLayer.HidePreview();
            _adorner.Invalidate(region);
        });
        _adorner.Glyphs.Add(new SurfaceGlyph(ribbon, selection, service, _previewLayer.PaintRetained));
        service.Adorners.Add(_adorner);
        ribbon.Invalidated += Invalidated; ribbon.LocationChanged += GeometryChanged; ribbon.Layout += LayoutChanged;
        selection.Backstage.Surface.SizeChanged += GeometryChanged; selection.Backstage.Surface.LocationChanged += GeometryChanged;
        selection.Changed += Changed;
    }
    private Point Origin => _service.ControlToAdornerWindow(_selection.Backstage.Surface);
    private Rectangle InAdorner(Rectangle bounds) { if (bounds.IsEmpty) return bounds; bounds.Offset(Origin); return bounds; }
    private Rectangle Outline
    {
        get
        {
            if (_selection.Backstage.IsOpen || !_selection.Active) return Rectangle.Empty;
            var bounds = _selection.SelectionBounds(_selection.Selected); if (bounds.IsEmpty) return bounds;
            bounds.Offset(_service.ControlToAdornerWindow(_ribbon));
            int padding = Math.Max(2, (int)Math.Ceiling(_ribbon.DeviceDpi / 96f)); bounds.Inflate(padding, padding); return bounds;
        }
    }
    private bool Updating => _repaint.IsFlushing || _selection.Backstage.ChangingSelection;
    private void Invalidated(object? sender, InvalidateEventArgs e) { if (!Updating) GeometryChanged(sender, e); }
    private void LayoutChanged(object? sender, LayoutEventArgs e) { if (!Updating) GeometryChanged(sender, e); }
    private void GeometryChanged(object? sender, EventArgs e)
    {
        var current = _selection.Backstage.CaptureState(); var extent = current is null ? Rectangle.Empty : InAdorner(current.Bounds); var outline = Outline;
        _repaint.Request(_previousExtent, extent, _previousOutline, outline);
        _previousPreview = current; _previousExtent = extent; _previousOutline = outline;
    }
    private void Changed(object? sender, EventArgs e)
    {
        var current = _selection.Backstage.CaptureState(); var outline = Outline;
        var dirty = RibbonBackstagePreviewState.Changes(_previousPreview, current).Select(InAdorner).ToList();
        dirty.Add(_previousOutline); dirty.Add(outline);
        _repaint.Request(dirty.ToArray());
        _previousPreview = current; _previousExtent = current is null ? Rectangle.Empty : InAdorner(current.Bounds); _previousOutline = outline;
    }
    public void Dispose()
    {
        _repaint.Dispose();
        _previewLayer.Dispose();
        _ribbon.Invalidated -= Invalidated; _ribbon.LocationChanged -= GeometryChanged; _ribbon.Layout -= LayoutChanged;
        _selection.Changed -= Changed; _service.Adorners.Remove(_adorner); _adorner.Glyphs.Clear();
        _selection.Backstage.Surface.SizeChanged -= GeometryChanged; _selection.Backstage.Surface.LocationChanged -= GeometryChanged;
    }
    private sealed class SurfaceGlyph(RibbonControl ribbon, RibbonDesignSelection selection, BehaviorService service, Func<Graphics, Point, bool> paintRetained)
        : Glyph(new SelectionBehavior(ribbon, selection, service))
    {
        public override Rectangle Bounds
        {
            get
            {
                if (!selection.Backstage.IsOpen) return service.ControlRectInAdornerWindow(ribbon);
                var bounds = selection.Backstage.Bounds; bounds.Offset(service.ControlToAdornerWindow(selection.Backstage.Surface)); return bounds;
            }
        }
        public override Cursor? GetHitTest(Point point)
        {
            if (selection.Backstage.IsOpen)
            {
                var previewOrigin = service.ControlToAdornerWindow(selection.Backstage.Surface);
                var local = new Point(point.X - previewOrigin.X, point.Y - previewOrigin.Y);
                return selection.Backstage.Bounds.Contains(local) ? Cursors.Hand : null;
            }
            var origin = service.ControlToAdornerWindow(ribbon);
            return selection.Active && selection.CanSelectAt(new(point.X - origin.X, point.Y - origin.Y)) ? Cursors.Hand : null;
        }
        public override void Paint(PaintEventArgs e)
        {
            if (selection.Backstage.IsOpen)
            {
                var previewOrigin = service.ControlToAdornerWindow(selection.Backstage.Surface);
                // Keep the established glyph ordering over designer selection
                // handles, using the same retained pixels as the opaque layer.
                if (!paintRetained(e.Graphics, previewOrigin)) selection.Backstage.Paint(e.Graphics, previewOrigin);
                return;
            }
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
            if (selection.Backstage.IsOpen)
            {
                var previewOrigin = service.ControlToAdornerWindow(selection.Backstage.Surface);
                return selection.Backstage.Click(new(mouseLoc.X - previewOrigin.X, mouseLoc.Y - previewOrigin.Y));
            }
            var origin = service.ControlToAdornerWindow(ribbon);
            return selection.SelectAt(new(mouseLoc.X - origin.X, mouseLoc.Y - origin.Y));
        }
        public override bool OnMouseUp(Glyph? glyph, MouseButtons button, Point mouseLoc) => button == MouseButtons.Left;
        public override bool OnMouseDoubleClick(Glyph? glyph, MouseButtons button, Point mouseLoc) => button == MouseButtons.Left;
    }
}
