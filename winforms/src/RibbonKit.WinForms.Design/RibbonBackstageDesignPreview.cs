using System.ComponentModel.Design;
using System.Drawing.Drawing2D;
using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms.Design;

// An inert designer preview model, not the runtime Backstage surface. Native page
// Controls are never hosted, moved, painted or captured by this preview.
internal sealed class RibbonBackstageDesignPreview(RibbonControl ribbon, ISelectionService? selection, Control surface)
{
    private bool _open;
    private RibbonBackstagePageItem? _page;
    private int _offset;
    internal bool ChangingSelection { get; private set; }
    internal event EventHandler? Changed;
    internal Control Surface => surface;
    internal bool IsOpen => _open && ribbon.IsInDesignMode && !ribbon.IsDisposed && !surface.IsDisposed;
    internal Rectangle Bounds
    {
        get
        {
            int top = surface is RibbonForm { CustomCaption: true } form ? form.CaptionHeight : 0;
            return new(0, top, surface.ClientSize.Width, Math.Max(0, surface.ClientSize.Height - top));
        }
    }
    internal RibbonBackstagePageItem? Page => _page is { IsDisposed: false } page && ribbon.Backstage.Contains(page) ? page : null;
    internal RibbonBackstageLayout Layout => RibbonBackstageLayout.Build(Bounds.Size, ribbon.Font, ribbon.DeviceDpi, ribbon.Backstage, Page);
    internal Rectangle BackBounds { get { var back = Layout.Back; back.Offset(Bounds.Location); return back; } }
    internal bool CanOpenFromFile(Point point) => ribbon.FileMenuMode == RibbonFileMenuMode.Backstage
        && ribbon.Snapshot.Targets.Any(target => target.Kind == TargetKind.FileMenu && target.VisibleBounds.Contains(point));
    internal void Open()
    {
        if (!ribbon.IsInDesignMode) throw new InvalidOperationException("Backstage preview requires a designer session.");
        if (surface.IsDisposed || ribbon.IsDisposed) return;
        _open = true; _offset = 0;
        _page = selection?.PrimarySelection is RibbonBackstagePageItem selected && ribbon.Backstage.Contains(selected) ? selected
            : ribbon.Backstage.SelectedPage ?? ribbon.Backstage.NavigationItems.OfType<RibbonBackstagePageItem>().FirstOrDefault(page => page.Visible);
        SelectComponent(ribbon);
        Changed?.Invoke(this, EventArgs.Empty);
    }
    internal void Close(bool selectRibbon = true)
    {
        if (!_open) return;
        _open = false; _page = null; _offset = 0;
        if (selectRibbon) SelectComponent(ribbon);
        Changed?.Invoke(this, EventArgs.Empty);
    }
    internal void ObserveSelection(object? component)
    {
        if (!IsOpen) return;
        if (component is RibbonItem item && ribbon.Backstage.Contains(item))
        {
            if (item is RibbonBackstagePageItem page) _page = page;
            var layout = Layout; var slot = layout.Arrange(ribbon.Backstage, 0, Page).FirstOrDefault(row => ReferenceEquals(row.Item, item));
            if (slot is null || slot.Footer && !layout.FooterInline) return;
            int top = slot.Bounds.Top - layout.Navigation.Top, bottom = top + slot.Bounds.Height;
            if (top < _offset) _offset = top;
            else if (bottom > _offset + layout.Navigation.Height) _offset = Math.Max(0, bottom - layout.Navigation.Height);
        }
        else if (!ReferenceEquals(component, ribbon) && !ReferenceEquals(component, ribbon.Backstage) && !ReferenceEquals(component, ribbon.FileMenu)) Close(false);
    }
    internal IReadOnlyList<RibbonBackstageDesignRow> Rows()
    {
        var layout = Layout;
        _offset = Math.Clamp(_offset, 0, Math.Max(0, layout.NavigationContentHeight - layout.Navigation.Height));
        var rows = new List<RibbonBackstageDesignRow>();
        foreach (var slot in layout.Arrange(ribbon.Backstage, _offset, Page))
        {
            var row = slot.Bounds; row.Offset(Bounds.Location); var clip = slot.Clip; clip.Offset(Bounds.Location);
            if (Rectangle.Intersect(row, clip) is { Width: > 0, Height: > 0 }) rows.Add(new(slot.Item, row, clip));
        }
        return rows;
    }
    internal bool Click(Point point)
    {
        if (!IsOpen || !Bounds.Contains(point)) return false;
        if (BackBounds.Contains(point)) { Close(); return true; }
        if (Rows().FirstOrDefault(row => row.VisibleBounds.Contains(point))?.Item is { } item)
        {
            if (ReferenceEquals(selection?.PrimarySelection, item) && (item is not RibbonBackstagePageItem || ReferenceEquals(item, _page))) return true;
            if (item is RibbonBackstagePageItem page) _page = page;
            SelectComponent(item);
            Changed?.Invoke(this, EventArgs.Empty);
        }
        // Consume page-area clicks rather than passing them to underlying Form
        // children. Selecting commands never calls their activation path.
        return true;
    }
    private void SelectComponent(object component)
    {
        ChangingSelection = true;
        try { selection?.SetSelectedComponents(new[] { component }, SelectionTypes.Replace); }
        finally { ChangingSelection = false; }
    }
    internal const string PlaceholderText = "Backstage page content\r\n\r\nShared design preview — select a navigation entry to edit its properties.\r\nDesign each page body in its separate UserControl designer. Use Back to return to the ribbon.";
    private Rectangle PageTextBounds
    {
        get { var bounds = Layout.PageContentBounds(); bounds.Offset(Bounds.Location); bounds.Inflate(-RibbonLayoutEngine.Scale(16, ribbon.DeviceDpi), -RibbonLayoutEngine.Scale(16, ribbon.DeviceDpi)); return bounds; }
    }
    private const TextFormatFlags PageTextFlags = TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix | TextFormatFlags.PreserveGraphicsClipping;
    internal RibbonBackstagePreviewState? CaptureState()
    {
        if (!IsOpen) return null;
        var textBounds = PageTextBounds;
        if (textBounds.Width > 0 && textBounds.Height > 0)
        {
            using var pixels = new Bitmap(1, 1); pixels.SetResolution(ribbon.DeviceDpi, ribbon.DeviceDpi); using var graphics = Graphics.FromImage(pixels);
            var measured = TextRenderer.MeasureText(graphics, PlaceholderText, ribbon.Font, textBounds.Size, PageTextFlags);
            textBounds.Height = Math.Min(textBounds.Height, measured.Height + RibbonLayoutEngine.Scale(2, ribbon.DeviceDpi));
        }
        return new(Bounds, PlaceholderText, textBounds, Rows().Select(row => new RibbonBackstagePreviewRowState(row.Item, row.VisibleBounds,
            ReferenceEquals(row.Item, Page), ReferenceEquals(row.Item, selection?.PrimarySelection) && row.Item is not RibbonBackstagePageItem)).ToArray());
    }
    internal void Paint(Graphics graphics, Point origin)
    {
        if (!IsOpen || Bounds.Width <= 0 || Bounds.Height <= 0) return;
        var bounds = Bounds;
        using var pixels = CreateFrame(graphics.DpiX, graphics.DpiY);
        graphics.DrawImageUnscaled(pixels, bounds.X + origin.X, bounds.Y + origin.Y);
    }
    internal Bitmap CreateFrame(float dpiX, float dpiY)
    {
        var bounds = Bounds;
        // Only library-painted navigation and the shared placeholder. No native
        // page content is instantiated, printed or captured for this frame.
        var pixels = new Bitmap(bounds.Width, bounds.Height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
        try
        {
            pixels.SetResolution(dpiX, dpiY);
            using var frame = Graphics.FromImage(pixels); PaintCore(frame, new(-bounds.X, -bounds.Y));
            return pixels;
        }
        catch { pixels.Dispose(); throw; }
    }
    private void PaintCore(Graphics graphics, Point origin)
    {
        if (!IsOpen || Bounds.Width <= 0 || Bounds.Height <= 0) return;
        var state = graphics.Save();
        try
        {
            var bounds = Bounds; bounds.Offset(origin); graphics.SetClip(bounds, CombineMode.Intersect);
            var layout = Layout;
            var context = new RibbonPaintContext(graphics, bounds, ribbon.Theme, ribbon.Font, ribbon.DeviceDpi) { Density = ribbon.Density };
            ribbon.Painter.DrawBackstageBackground(context, layout.Page.Left);
            var back = BackBounds; back.Offset(origin); ribbon.Painter.DrawBackstageEntry(context with { Bounds = back }, null, false);
            foreach (var row in Rows())
            {
                var target = row.Bounds; target.Offset(origin); var clip = row.Clip; clip.Offset(origin);
                var rowState = graphics.Save();
                try
                {
                    graphics.SetClip(clip, CombineMode.Intersect);
                    ribbon.Painter.DrawBackstageEntry(context with { Bounds = target, Hot = ReferenceEquals(selection?.PrimarySelection, row.Item) && row.Item is not RibbonBackstagePageItem }, row.Item, ReferenceEquals(row.Item, Page));
                }
                finally { graphics.Restore(rowState); }
            }
            var pageBounds = PageTextBounds; pageBounds.Offset(origin);
            if (pageBounds.Width > 0 && pageBounds.Height > 0)
                TextRenderer.DrawText(graphics, PlaceholderText, ribbon.Font, pageBounds, ribbon.Theme.Foreground, PageTextFlags);
        }
        finally { graphics.Restore(state); }
    }
}

internal sealed record RibbonBackstageDesignRow(RibbonItem Item, Rectangle Bounds, Rectangle Clip)
{
    internal Rectangle VisibleBounds => Rectangle.Intersect(Bounds, Clip);
}
