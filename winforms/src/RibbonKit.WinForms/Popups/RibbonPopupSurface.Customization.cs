namespace RibbonKit.WinForms;

internal sealed partial class RibbonPopupSurface
{
    private bool ProcessCustomizationKey(Keys keys)
    {
        if (_ribbon.Editors.ContainsFocus) return false;
        if (keys != (Keys.Shift | Keys.F10) && keys != Keys.Apps) return false;
        var bounds = Targets.FirstOrDefault(t => ReferenceEquals(t.Element, _focused))?.VisibleBounds ?? ClientRectangle;
        return _ribbon.ShowCustomizationMenu(this, PointToScreen(new(bounds.Left, bounds.Bottom)), GallerySource ?? _focused ?? GroupSource);
    }
    private bool HandleCustomizationContext(ref Message m)
    {
        if (m.Msg == 0x7B && ContextMenuStrip is null)
        {
            if (_ribbon.PopupOnly) return true;
            bool keyboard = m.LParam == new IntPtr(-1);
            var screen = keyboard ? PointToScreen(Point.Empty) : new Point(unchecked((short)(long)m.LParam), unchecked((short)((long)m.LParam >> 16)));
            var local = PointToClient(screen);
            var element = keyboard ? _focused : Targets.LastOrDefault(t => t.VisibleBounds.Contains(local))?.Element;
            var group = _bodyLayout?.Groups.FirstOrDefault(g => { var frame = g.Frame; frame.Offset(AutoScrollPosition); return frame.Contains(local); })?.Group;
            if (element is not null) FocusElement(element, enterEditor: false);
            if (_ribbon.ShowCustomizationMenu(this, screen, GallerySource ?? element ?? group ?? GroupSource)) return true;
        }
        return false;
    }
}
