namespace RibbonKit.WinForms;

// Supplies existing popup services with the Options form's lifetime and DPI.
// The disabled main form is only a visual reference; no ribbon UI is displayed.
internal sealed class RibbonOptionsPopupHost(RibbonControl appearance) : IDisposable
{
    internal RibbonControl Ribbon { get; } = new() { PopupOnly = true, Visible = false, TabStop = false, Dock = DockStyle.None, Size = new(1, 1) };
    internal void Prepare(Control opener)
    {
        var form = opener.FindForm() ?? throw new InvalidOperationException("An options popup needs an owning form.");
        if (!ReferenceEquals(Ribbon.Parent, form)) form.Controls.Add(Ribbon);
        Ribbon.Font = opener.Font; Ribbon.Renderer = appearance.Painter; Ribbon.Theme = appearance.Theme; Ribbon.Density = appearance.Density;
        _ = Ribbon.Handle;
    }
    public void Dispose() => Ribbon.Dispose();
}
