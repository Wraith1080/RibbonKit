using System.ComponentModel;

namespace RibbonKit.WinForms;

/// <summary>A native options page. Text supplies its navigation label and heading; add application controls to Controls. The dialog owns attached pages and their children.</summary>
[ToolboxItem(false)]
public class RibbonOptionsPage : UserControl
{
    /// <summary>Creates an options page whose settings and draft remain application-owned.</summary>
    public RibbonOptionsPage() { AutoScaleMode = AutoScaleMode.Dpi; AutoScaleDimensions = new(96, 96); }
    /// <summary>Occurs before Apply or OK commits this page. Cancel to preserve the draft and keep the dialog open.</summary>
    public event CancelEventHandler? Applying;
    /// <summary>Occurs after this page successfully applies. Commit application settings and update their Cancel checkpoint here.</summary>
    public event EventHandler? Applied;
    /// <summary>Occurs on Cancel/window close. Discard only settings staged since this page's latest Apply.</summary>
    public event EventHandler? Cancelled;
    /// <summary>Validates and applies this page, retaining an independent checkpoint through its Applied handler.</summary>
    public bool ApplyChanges() => ValidateDraft() && CommitDraft();
    internal bool ValidateDraft() { var args = new CancelEventArgs(); Applying?.Invoke(this, args); return !args.Cancel; }
    internal bool CommitDraft() { if (!ApplyCore()) return false; Applied?.Invoke(this, EventArgs.Empty); return true; }
    internal void CancelDraft() { CancelCore(); Cancelled?.Invoke(this, EventArgs.Empty); }
    internal virtual bool ApplyCore() => true;
    internal virtual void CancelCore() { }
}

/// <summary>Provides the shared options dialog before the built-in context command opens it.</summary>
public sealed class RibbonOptionsDialogEventArgs : EventArgs
{
    /// <summary>Creates event data borrowing the dialog for its normal lifetime.</summary>
    public RibbonOptionsDialogEventArgs(RibbonOptionsDialog dialog) { ArgumentNullException.ThrowIfNull(dialog); Dialog = dialog; }
    /// <summary>Gets the dialog. Add application pages or select a page before it opens; do not dispose it in the event.</summary>
    public RibbonOptionsDialog Dialog { get; }
}
