namespace RibbonKit.WinForms;

internal sealed class RibbonOptionsActionMenu : IDisposable
{
    private readonly RibbonOptionsPopupHost _host;
    private readonly RibbonDropDownItem _menu = new();
    internal RibbonOptionsActionMenu(RibbonControl ribbon) { _host = new(ribbon); _host.Ribbon.CommandCatalog.Commands.Add(_menu); }
    internal RibbonItemCollection Items => _menu.Items;
    internal Font Font { get => _host.Ribbon.Font; set => _host.Ribbon.Font = value; }
    internal RibbonPopupSurface? Surface => _host.Ribbon.Popups.Top;
    internal void Action(string text, Action action, bool enabled = true, bool check = false)
    {
        var item = new RibbonButtonItem { Text = text, Enabled = enabled, Icon = check ? RibbonMenuCheckmark.Instance : null };
        item.Click += (_, _) => { _host.Ribbon.Popups.CloseAll(true); action(); }; _menu.Items.Add(item);
    }
    internal void Separator() => _menu.Items.Add(new RibbonMenuSeparatorItem());
    internal void Show(Control anchor, Point location)
    { _host.Prepare(anchor); _host.Ribbon.Popups.Toggle(_menu, anchor, new(location.X, location.Y - anchor.Height, anchor.Width, anchor.Height)); }
    internal void Close() => _host.Ribbon.Popups.CloseAll(false);
    public void Dispose() => _host.Dispose();
}
