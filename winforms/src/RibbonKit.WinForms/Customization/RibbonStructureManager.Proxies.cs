namespace RibbonKit.WinForms;

internal sealed partial class RibbonStructureManager
{
    internal sealed class Proxy(RibbonItem model, RibbonItem source, RibbonCustomizationCommand entry)
    {
        internal readonly RibbonItem Model = model, Source = source;
        internal RibbonCustomizationCommand Entry = entry;
    }
    private readonly Dictionary<RibbonItem, Proxy> _proxies = [];
    private readonly Dictionary<RibbonGalleryTile, RibbonGalleryTile> _tiles = [];
    internal RibbonElement Source(RibbonElement element) => element is RibbonGalleryTile tile && _tiles.TryGetValue(tile, out var original) ? original
        : element is RibbonItem item && _proxies.TryGetValue(item, out var proxy) ? proxy.Source : element;
    internal bool IsProxy(RibbonElement element) => element is RibbonItem item && _proxies.ContainsKey(item) || element is RibbonGalleryTile tile && _tiles.ContainsKey(tile);
    internal bool Invoke(RibbonItem item)
    {
        if (ReferenceEquals(Source(item), item)) return false;
        if (item.EffectiveEnabled && item.Visible && Source(item) is RibbonItem source && !source.IsDisposed)
        { source.PerformClick(); Synchronize(); }
        return true;
    }
    private Proxy CreateProxy(RibbonItem source, RibbonCustomizationCommand entry)
    {
        RibbonItem model = source switch
        {
            RibbonSplitButtonItem => new RibbonSplitButtonItem(), RibbonGalleryItem => new RibbonGalleryItem(),
            RibbonDropDownItem => new RibbonDropDownItem(), RibbonCheckBoxItem => new RibbonCheckBoxItem(),
            RibbonRadioButtonItem => new RibbonRadioButtonItem(), RibbonToggleItem => new RibbonToggleItem(),
            RibbonButtonItem => new RibbonButtonItem(), RibbonTextBoxItem => new RibbonTextBoxItem(), RibbonComboBoxItem => new RibbonComboBoxItem(),
            _ => throw new InvalidOperationException("This command has no native Ribbon presentation.")
        };
        model.Id = entry.Id;
        var proxy = new Proxy(model, source, entry); _proxies.Add(model, proxy);
        if (model is RibbonTextBoxItem text && source is RibbonTextBoxItem sourceText)
            text.ValueChanged += (_, _) => { if (!Synchronizing) sourceText.Value = text.Value; };
        if (model is RibbonComboBoxItem combo && source is RibbonComboBoxItem sourceCombo)
        {
            combo.ValueChanged += (_, _) => { if (!Synchronizing) sourceCombo.Value = combo.Value; };
            combo.SelectedIndexChanged += (_, _) => { if (!Synchronizing) sourceCombo.SelectedIndex = combo.SelectedIndex; };
        }
        return proxy;
    }
    private void RemoveProxy(RibbonElement model)
    {
        if (model is RibbonItem item) _proxies.Remove(item);
        if (model is RibbonGalleryItem gallery) foreach (var tile in gallery.Items.OfType<RibbonGalleryTile>()) _tiles.Remove(tile);
    }
    internal void Synchronize(RibbonElement? origin = null)
    {
        if (Synchronizing || origin is not null && IsProxy(origin)) return;
        Synchronizing = true;
        try
        {
            foreach (var proxy in _proxies.Values.ToArray())
            {
                var source = proxy.Source; var model = proxy.Model;
                if (model.IsDisposed) continue;
                if (source.IsDisposed || !ReferenceEquals(source.Ribbon, Host)) { model.Enabled = false; continue; }
                model.Text = proxy.Entry.Label ?? source.Text; model.KeyTip = source.KeyTip;
                model.Icon = source.Icon; model.Image = source.Image; model.LargeImage = source.LargeImage;
                model.Enabled = source.EffectiveEnabled && AncestorsVisible(source); model.Visible = source.Visible;
                model.ScreenTipDescription = source.ScreenTipDescription; model.ScreenTipTitle = source.ScreenTipTitle; model.MenuDescription = source.MenuDescription;
                model.SizePolicy = RibbonSizePolicy.Fixed(proxy.Entry.Size);
                switch (model, source)
                {
                    case (RibbonToggleItem p, RibbonToggleItem s): p.Checked = s.Checked; break;
                    case (RibbonCheckBoxItem p, RibbonCheckBoxItem s): p.ThreeState = s.ThreeState; p.CheckState = s.CheckState; break;
                    case (RibbonRadioButtonItem p, RibbonRadioButtonItem s): p.Checked = s.Checked; break;
                    case (RibbonSplitButtonItem p, RibbonSplitButtonItem s): p.Layout = s.Layout; p.CheckOnClick = s.CheckOnClick; p.Checked = s.Checked; p.DropDownKeyTip = s.DropDownKeyTip; break;
                    case (RibbonTextBoxItem p, RibbonTextBoxItem s): p.InputWidth = s.InputWidth; p.MaxLength = s.MaxLength; p.ReadOnly = s.ReadOnly; p.Value = s.Value; break;
                    case (RibbonComboBoxItem p, RibbonComboBoxItem s):
                        p.InputWidth = s.InputWidth; p.MaxLength = s.MaxLength; p.IsEditable = s.IsEditable;
                        if (!p.Items.SequenceEqual(s.Items)) { p.Items.Clear(); p.Items.AddRange(s.Items.ToArray()); }
                        p.SelectedIndex = s.SelectedIndex; p.Value = s.Value; break;
                    case (RibbonGalleryItem p, RibbonGalleryItem s): SyncGallery(p, s); break;
                }
                if (model is RibbonDropDownItem menu && source is RibbonDropDownItem sourceMenu)
                { menu.DropDownHeader = sourceMenu.DropDownHeader; menu.MenuPresentation = sourceMenu.MenuPresentation; }
            }
        }
        finally { Synchronizing = false; }
    }
    private static bool AncestorsVisible(RibbonElement source)
    {
        for (var node = source.Parent as RibbonElement; node is not null; node = node.Parent as RibbonElement) if (!node.Visible || node.IsDisposed) return false;
        return true;
    }
    private void SyncGallery(RibbonGalleryItem model, RibbonGalleryItem source)
    {
        model.IsInRibbon = source.IsInRibbon; model.InRibbonColumnCount = source.InRibbonColumnCount; model.ColumnCount = source.ColumnCount;
        model.TileWidth = source.TileWidth; model.TileHeight = source.TileHeight; model.MaxVisibleRows = source.MaxVisibleRows;
        var sourceTiles = source.Items.Cast<RibbonGalleryTile>().ToArray();
        foreach (var tile in model.Items.Cast<RibbonGalleryTile>().Where(t => !_tiles.TryGetValue(t, out var s) || !sourceTiles.Contains(s)).ToArray())
        { _tiles.Remove(tile); tile.Dispose(); }
        for (int i = 0; i < sourceTiles.Length; i++)
        {
            var s = sourceTiles[i]; var p = model.Items.OfType<RibbonGalleryTile>().FirstOrDefault(t => _tiles.GetValueOrDefault(t) == s);
            if (p is null) { p = new RibbonGalleryTile { Id = model.Id + ":tile:" + s.Id }; _tiles[p] = s; model.Items.Insert(i, p); }
            else if (model.Items.IndexOf(p) != i) { model.Items.Remove(p); model.Items.Insert(i, p); }
            p.Text = s.Text; p.Icon = s.Icon; p.Image = s.Image; p.LargeImage = s.LargeImage; p.GroupName = s.GroupName;
            p.Enabled = s.EffectiveEnabled; p.Visible = s.Visible;
        }
        model.SelectedItem = source.SelectedItem is { } selected ? model.Items.OfType<RibbonGalleryTile>().FirstOrDefault(t => _tiles.GetValueOrDefault(t) == selected) : null;
    }
    internal RibbonGalleryTile? GalleryPreview(RibbonGalleryItem model) => Source(model) is RibbonGalleryItem source && source.PreviewItem is { } tile
        ? model.Items.OfType<RibbonGalleryTile>().FirstOrDefault(t => _tiles.GetValueOrDefault(t) == tile) : null;
}
