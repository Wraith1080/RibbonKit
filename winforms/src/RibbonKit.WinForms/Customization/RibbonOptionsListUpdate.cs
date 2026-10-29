namespace RibbonKit.WinForms;

// Nest native update counts across Apply, including icon replacement. For OK,
// the dialog closes before redraw is restored; failed validation redraws once.
internal sealed class RibbonOptionsListUpdate : IDisposable
{
    private readonly Control[] _controls;
    internal RibbonOptionsListUpdate(Control root)
    {
        _controls = Children(root).Where(c => c is TreeView or ListBox).ToArray();
        foreach (var control in _controls) { if (control is TreeView tree) tree.BeginUpdate(); else ((ListBox)control).BeginUpdate(); }
    }
    public void Dispose()
    {
        foreach (var control in _controls)
            if (!control.IsDisposed) { if (control is TreeView tree) tree.EndUpdate(); else ((ListBox)control).EndUpdate(); }
    }
    private static IEnumerable<Control> Children(Control root) => root.Controls.Cast<Control>().SelectMany(c => new[] { c }.Concat(Children(c)));
}
