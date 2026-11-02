namespace RibbonKit.WinForms;

public partial class RibbonControl
{
    private bool _popupContentInitialized, _initializingPopupContent;
    private void InitializePopupContent()
    {
        if (_disposing || IsDisposed || IsInDesignMode || _updateDepth > 0 || _initializingPopupContent) return;
        _popupContentInitialized = true; _initializingPopupContent = true;
        try
        {
            foreach (var item in AllElements.OfType<RibbonDropDownItem>().Distinct().ToArray())
                if (!item.IsDisposed && ReferenceEquals(item.Ribbon, this)) item.EnsureContent();
        }
        finally { _initializingPopupContent = false; }
    }
}
