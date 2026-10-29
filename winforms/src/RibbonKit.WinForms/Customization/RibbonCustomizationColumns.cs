namespace RibbonKit.WinForms;

// All built-in pages share the same two command columns. Footer button captions
// must not participate in their width measurement or move the split on selection.
internal sealed class RibbonCustomizationColumns : TableLayoutPanel
{
    private int _transfer, _order, _buttonHeight, _buttonMargin, _searchClearance;
    internal RibbonCustomizationColumns()
    {
        Dock = DockStyle.Fill; ColumnCount = 4; Margin = Padding.Empty;
        for (int i = 0; i < 4; i++) ColumnStyles.Add(new(SizeType.Absolute));
    }
    internal void UpdateMetrics(int dpi, Font font, RibbonDensity density)
    {
        int S(int value) => RibbonKit.WinForms.Layout.RibbonLayoutEngine.Scale(value, dpi);
        _transfer = Math.Max(S(94), TextRenderer.MeasureText("« Remove", font).Width + S(20));
        _order = Math.Max(S(48), TextRenderer.MeasureText("▲", font).Width + S(20));
        _buttonHeight = RibbonQuickAccessEditorMetrics.ButtonHeight(dpi, font.Height, density);
        _buttonMargin = S(4); _searchClearance = _buttonHeight + S(16);
        PerformLayout();
    }
    internal void AddActions(int column, int listRow, Button first, Button second)
    {
        var stack = new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Anchor = AnchorStyles.None,
            FlowDirection = FlowDirection.TopDown, WrapContents = false, Margin = Padding.Empty };
        first.AutoSize = second.AutoSize = false;
        stack.Controls.AddRange([first, second]); Controls.Add(stack, column, 1); SetRowSpan(stack, listRow);
    }
    protected override void OnLayout(LayoutEventArgs e)
    {
        if (ColumnStyles.Count == 4)
        {
            int available = Math.Max(0, ClientSize.Width - Padding.Horizontal - _transfer - _order);
            int[] widths = [available / 2, _transfer, available - available / 2, _order];
            for (int i = 0; i < 4; i++) if (ColumnStyles[i].Width != widths[i]) ColumnStyles[i].Width = widths[i];
            foreach (int column in new[] { 1, 3 })
                if (GetControlFromPosition(column, 1) is FlowLayoutPanel stack)
                {
                    // Center against the same content area on every page,
                    // reserving one common search row above its list baseline.
                    stack.Padding = new(0, _searchClearance, 0, 0);
                    foreach (var button in stack.Controls.OfType<Button>())
                    {
                        button.Margin = new(_buttonMargin);
                        button.Size = new(Math.Max(0, widths[column] - 2 * _buttonMargin), _buttonHeight);
                    }
                }
        }
        base.OnLayout(e);
    }
}
