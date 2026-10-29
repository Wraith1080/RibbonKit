using System.ComponentModel;
using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

public partial class RibbonControl
{
    /// <summary>Gets the stable owned repeatable message bar. Messages affect ribbon height; the application owns their content and action decisions.</summary>
    [Category("Data"), DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    public RibbonMessageBar MessageBar { get; }
    internal bool JoinMessageBar => Painter.JoinMessageBarToBody && (QuickAccessPosition != RibbonQuickAccessPosition.BelowRibbon || !HasQuickAccess || JoinBelowRibbonQuickAccess);
    internal int MessageWidth => Math.Max(0, ClientSize.Width - (JoinMessageBar ? RibbonLayoutEngine.Scale(Theme.BodyMargin.Left, DeviceDpi) + RibbonLayoutEngine.Scale(Theme.BodyMargin.Right, DeviceDpi) : 0));
    internal int MessageHeight => RibbonMessageLayout.Measure(this, MessageWidth, DeviceDpi).Height;
    private bool MessageTopConnected => JoinMessageBar && (!_snapshot.BodyBounds.IsEmpty || JoinBelowRibbonQuickAccess && !_snapshot.QuickAccessBounds.IsEmpty);
    private void PaintMessages(RibbonPaintContext context)
    {
        if (_snapshot.Messages.Count == 0) return;
        Painter.DrawMessageBar(context with { Bounds = _snapshot.MessageBounds, IsTopEdgeConnected = MessageTopConnected });
    }
    private void PaintMessageContents(RibbonPaintContext context)
    {
        for (int i = 0; i < _snapshot.Messages.Count; i++)
        {
            var row = _snapshot.Messages[i];
            Painter.DrawMessage(context with { Bounds = row.Bounds }, row.Message, row.Title, row.Text, row.Icon, i == 0);
        }
    }
    private void ActivateMessageTarget(RibbonTarget target)
    {
        if (target.Kind == TargetKind.MessageClose && CanAnimateMessageChange)
        { Motion.Change(false, () => ActivateMessageTarget(target), messages: true); return; }
        bool restore = Focused;
        if (target.Element is RibbonButtonItem button) button.PerformClick();
        if (IsDisposed || _disposing) return;
        _popups.CloseAll(false);
        if (restore && FindForm()?.ContainsFocus == true && (_focused is null || !Snapshot.Targets.Any(t => ReferenceEquals(t.Element, _focused) && t.Enabled)))
        {
            var next = Snapshot.Targets.FirstOrDefault(t => t.Enabled && t.Kind is TargetKind.MessageAction or TargetKind.MessageClose)
                ?? Snapshot.Targets.FirstOrDefault(t => t.Enabled && t.Kind == TargetKind.Tab);
            if (next?.Element is { } element) FocusElement(element);
        }
    }
}
