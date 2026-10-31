using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

public partial class RibbonControl
{
    // Design-server presentation only. Never assign model properties, select a
    // runtime startup tab, or raise application selection/command events here.
    private RibbonTab? _designPreviewTab;
    internal RibbonTab? DesignPreviewTab => IsInDesignMode && _designPreviewTab is { IsDisposed: false } tab && Tabs.Contains(tab) ? tab : null;
    internal RibbonTab? DisplaySelectedTab => DesignPreviewTab ?? _selectedTab;
    internal void SetDesignPreviewTab(RibbonTab? tab)
    {
        VerifyAccess();
        if (!IsInDesignMode) throw new InvalidOperationException("Tab preview requires a designer session.");
        if (tab is not null && (!Tabs.Contains(tab) || tab.IsDisposed)) throw new ArgumentException("Preview an authored tab belonging to this ribbon.", nameof(tab));
        if (ReferenceEquals(_designPreviewTab, tab)) return;
        _designPreviewTab = tab;
        _bodyOffset = 0;
        _layoutDirty = true;
        if (DisplaySelectedTab is { } displayed && Snapshot.Targets.FirstOrDefault(target => ReferenceEquals(target.Element, displayed)) is { } header)
        {
            var viewport = Snapshot.TabViewport;
            int delta = header.Bounds.Left < viewport.Left ? header.Bounds.Left - viewport.Left
                : header.Bounds.Right > viewport.Right ? Math.Min(header.Bounds.Right - viewport.Right, header.Bounds.Left - viewport.Left) : 0;
            if (delta != 0) { _tabOffset += delta; _layoutDirty = true; }
        }
        Invalidate();
    }
    private void DrawDesignEditors(Graphics graphics)
    {
        foreach (var target in _snapshot.Targets)
        {
            if (target.Element is not RibbonItem item || item is not IRibbonEditorItem || target.VisibleBounds.IsEmpty) continue;
            var state = graphics.Save();
            try
            {
                graphics.SetClip(target.Clip, System.Drawing.Drawing2D.CombineMode.Intersect);
                var context = new RibbonPaintContext(graphics, target.Bounds, Theme, Font, DeviceDpi) { Density = Density };
                var field = RibbonEditorLayout.Field(target.Bounds, item, target.LabelWidth, Font, DeviceDpi, LayoutMode == RibbonLayoutMode.Simplified);
                string text; Rectangle textBounds;
                if (item is RibbonComboBoxItem combo)
                {
                    Painter.DrawComboBox(context, combo, field); text = combo.Value;
                    textBounds = RibbonEditorLayout.ComboText(field, DeviceDpi, Density);
                }
                else if (item is RibbonTextBoxItem input)
                {
                    Painter.DrawTextBox(context, input, field); text = input.Value;
                    textBounds = Rectangle.Inflate(field, -RibbonLayoutEngine.Scale(4, DeviceDpi), 0);
                }
                else continue;
                if (textBounds.Width <= 0 || textBounds.Height <= 0) continue;
                graphics.SetClip(field, System.Drawing.Drawing2D.CombineMode.Intersect);
                TextRenderer.DrawText(graphics, text, Font, textBounds, item.EffectiveEnabled ? Theme.Foreground : Theme.DisabledColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix
                    | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis | TextFormatFlags.PreserveGraphicsClipping);
            }
            finally { graphics.Restore(state); }
        }
    }
}
