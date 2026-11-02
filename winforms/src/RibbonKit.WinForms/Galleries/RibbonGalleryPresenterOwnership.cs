using System.Runtime.CompilerServices;

namespace RibbonKit.WinForms;

internal static class RibbonGalleryPresenterOwnership
{
    // A parked view can be unparented and still owned. Weak keys retain neither
    // controls nor ribbons after disposal, and prevent sharing across live hosts.
    private static readonly ConditionalWeakTable<Control, object> Owned = new();
    internal static UserControl Accept(Control? control)
    {
        bool alreadyOwned = control is not null && Owned.TryGetValue(control, out _);
        if (control is not UserControl view || control is not IRibbonGalleryPresenter || control.IsDisposed || control.Parent is not null || alreadyOwned)
        {
            if (!alreadyOwned && control is { IsDisposed: false, Parent: null }) control.Dispose();
            throw new InvalidOperationException("A gallery presenter must be a fresh, unparented UserControl implementing IRibbonGalleryPresenter.");
        }
        Owned.Add(view, new object());
        return view;
    }
}
