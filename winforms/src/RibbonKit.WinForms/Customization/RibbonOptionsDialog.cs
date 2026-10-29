using System.Collections.ObjectModel;
using System.ComponentModel;

namespace RibbonKit.WinForms;

/// <summary>A native options dialog with a themed navigation rail, application pages and per-page Apply. Attached pages are owned; removed pages are detached without disposal.</summary>
public class RibbonOptionsDialog : Form
{
    private readonly RibbonControl _ribbon;
    private readonly ListBox _navigation = new() { Name = "OptionsNavigation", AccessibleName = "Options pages", DisplayMember = nameof(Text), BorderStyle = BorderStyle.None, DrawMode = DrawMode.OwnerDrawFixed, IntegralHeight = false, Dock = DockStyle.Fill };
    private readonly Panel _rail = new() { Dock = DockStyle.Left };
    private readonly Panel _viewport = new() { Dock = DockStyle.Fill, AutoScroll = true };
    private readonly Label _heading = new() { AutoSize = true, Dock = DockStyle.Top };
    private readonly FlowLayoutPanel _footer = new() { AutoSize = true, Dock = DockStyle.Bottom, FlowDirection = FlowDirection.RightToLeft };
    private readonly Panel _body = new() { Dock = DockStyle.Fill };
    private readonly RoundedPainter _rounded = new();
    private Font? _headingFont;
    private readonly List<Font> _headingFonts = [];
    private bool _accepted, _refreshingPages;
    private readonly PageCollection _pages;

    /// <summary>Creates a shell borrowing the ribbon's palette, font and density. Applications add pages containing ordinary native controls.</summary>
    public RibbonOptionsDialog(RibbonControl ribbon)
    {
        ArgumentNullException.ThrowIfNull(ribbon); ribbon.VerifyAccess(); _ribbon = ribbon; _pages = new(this);
        AutoScaleMode = AutoScaleMode.Dpi; AutoScaleDimensions = new(96, 96); Font = ribbon.Font;
        Text = "Options"; Name = "RibbonOptionsDialog"; ShowInTaskbar = false; ShowIcon = false;
        MinimizeBox = false; MaximizeBox = false; StartPosition = FormStartPosition.CenterParent;
        BackColor = ribbon.Theme.Surface; ForeColor = ribbon.Theme.Foreground;
        _rail.BackColor = _navigation.BackColor = ribbon.Theme.BackstageNavigationBackground;
        _navigation.SelectedIndexChanged += (_, _) => ShowSelectedPage(); _navigation.DrawItem += DrawNavigation;
        _rail.Controls.Add(_navigation);
        _rail.Paint += (_, e) => { using var pen = new Pen(ribbon.Theme.Border); e.Graphics.DrawLine(pen, _rail.Width - 1, 0, _rail.Width - 1, _rail.Height); };
        var cancel = Button("CancelCustomization", "Cancel", () => { DialogResult = DialogResult.Cancel; Close(); });
        cancel.CausesValidation = false;
        var ok = new RibbonOptionsAccentButton(ribbon) { Name = "AcceptCustomization", Text = "OK", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        ok.Click += (_, _) => { using var update = new RibbonOptionsListUpdate(this); if (ApplyAll()) { _accepted = true; DialogResult = DialogResult.OK; Close(); } };
        var apply = Button("ApplyCustomization", "&Apply", () => { using var update = new RibbonOptionsListUpdate(this); SelectedPage?.ApplyChanges(); });
        _footer.Controls.AddRange([apply, cancel, ok]); CancelButton = cancel; AcceptButton = ok;
        _body.Controls.Add(_viewport); _body.Controls.Add(_heading);
        var middle = new Panel { Dock = DockStyle.Fill }; middle.Controls.Add(_body); middle.Controls.Add(_rail);
        Controls.Add(middle); Controls.Add(_footer);
        _footer.Paint += (_, e) => { using var pen = new Pen(ribbon.Theme.Border); e.Graphics.DrawLine(pen, 0, 0, _footer.Width, 0); };
        _viewport.Resize += (_, _) => LayoutPage(); _viewport.Layout += (_, _) => LayoutPage(); UpdateMetrics(false);
    }
    /// <summary>Gets the owned pages. Removing a page detaches it without disposal. A page can belong to only one dialog.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    public Collection<RibbonOptionsPage> Pages => _pages;
    /// <summary>Gets or sets the selected page. A non-null selection must belong to Pages.</summary>
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public RibbonOptionsPage? SelectedPage
    {
        get => _navigation.SelectedItem as RibbonOptionsPage;
        set { if (value is not null && !_pages.Contains(value)) throw new ArgumentException("The selected page must belong to this dialog.", nameof(value)); _navigation.SelectedItem = value; }
    }
    private bool ApplyAll()
    {
        foreach (var page in _pages.ToArray()) if (!page.ValidateDraft()) { SelectedPage = page; return false; }
        foreach (var page in _pages.ToArray()) if (!page.CommitDraft()) { SelectedPage = page; return false; }
        return true;
    }
    private static Button Button(string name, string text, Action action)
    {
        var button = new Button { Name = name, Text = text, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, UseVisualStyleBackColor = true };
        button.Click += (_, _) => action(); return button;
    }
    private int ScaleLogical(int value) => (int)Math.Ceiling(value * DeviceDpi / 96d);
    private void RefreshPages(RibbonOptionsPage? preferred = null)
    {
        var selected = preferred ?? SelectedPage; _refreshingPages = true;
        _navigation.BeginUpdate(); _navigation.Items.Clear(); foreach (var page in _pages) _navigation.Items.Add(page);
        _navigation.SelectedItem = selected is not null && _pages.Contains(selected) ? selected : _pages.FirstOrDefault();
        _navigation.EndUpdate(); _refreshingPages = false; ShowSelectedPage();
    }
    private void PageTextChanged(object? sender, EventArgs e) { _navigation.Invalidate(); if (ReferenceEquals(sender, SelectedPage)) _heading.Text = SelectedPage?.Text; }
    private void ShowSelectedPage()
    {
        if (_refreshingPages) return;
        foreach (var page in _pages) page.Visible = ReferenceEquals(page, SelectedPage);
        _heading.Text = SelectedPage?.Text ?? ""; _footer.Controls["ApplyCustomization"]!.Enabled = SelectedPage is not null;
        _viewport.AutoScrollPosition = Point.Empty; LayoutPage();
    }
    private void LayoutPage()
    {
        if (SelectedPage is not { } page) return;
        var minimum = page.MinimumSize; _viewport.AutoScrollMinSize = minimum;
        page.Bounds = new(_viewport.AutoScrollPosition.X, _viewport.AutoScrollPosition.Y, Math.Max(minimum.Width, _viewport.ClientSize.Width), Math.Max(minimum.Height, _viewport.ClientSize.Height));
    }
    private void UpdateMetrics(bool initial)
    {
        if (_ribbon is null || _pages is null || Disposing || IsDisposed) return;
        SuspendLayout(); int gap = ScaleLogical(8);
        _rail.Width = Math.Max(ScaleLogical(184), Font.Height * 11); _rail.Padding = new(gap, ScaleLogical(14), gap, gap);
        _body.Padding = new(ScaleLogical(22), ScaleLogical(16), ScaleLogical(16), gap);
        _navigation.ItemHeight = RibbonQuickAccessEditorMetrics.ButtonHeight(DeviceDpi, Font.Height, _ribbon.Density) + ScaleLogical(8);
        // WinForms can still measure a previous heading font during a parent
        // DPI autoscale pass. Retain these owned fonts until child disposal.
        _headingFont = new(Font.FontFamily, Math.Max(Font.SizeInPoints + 5, 14), FontStyle.Bold); _headingFonts.Add(_headingFont);
        _heading.Font = _headingFont; _heading.Padding = new(0, 0, 0, ScaleLogical(12));
        _footer.Padding = new(gap);
        foreach (var button in _footer.Controls.Cast<Button>())
        { button.MinimumSize = new(ScaleLogical(88), RibbonQuickAccessEditorMetrics.ButtonHeight(DeviceDpi, Font.Height, _ribbon.Density)); button.Margin = new(gap / 2); }
        var work = Screen.FromControl(this).WorkingArea;
        var available = new Size(Math.Max(1, work.Width - ScaleLogical(16)), Math.Max(1, work.Height - ScaleLogical(16)));
        bool touch = _ribbon.Density == RibbonDensity.Touch;
        MinimumSize = new(Math.Min(ScaleLogical(760), available.Width), Math.Min(ScaleLogical(touch ? 470 : 390), available.Height));
        if (initial) Size = new(Math.Min(ScaleLogical(touch ? 1030 : 920), available.Width), Math.Min(ScaleLogical(touch ? 650 : 520), available.Height));
        if (Width > available.Width || Height > available.Height) Size = new(Math.Min(Width, available.Width), Math.Min(Height, available.Height));
        ResumeLayout(true); LayoutPage(); _navigation.Invalidate();
    }
    private void DrawNavigation(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || _navigation.Items[e.Index] is not RibbonOptionsPage page) return;
        var theme = _ribbon.Theme; using var background = new SolidBrush(theme.BackstageNavigationBackground); e.Graphics.FillRectangle(background, e.Bounds);
        bool selected = e.State.HasFlag(DrawItemState.Selected); var bounds = Rectangle.Inflate(e.Bounds, 0, -ScaleLogical(2));
        if (selected)
        {
            _rounded.Draw(e.Graphics, bounds, _ribbon.Painter.GetPopupCornerRadius(theme, DeviceDpi), theme.Checked, Color.Transparent, 0);
            var marker = new Rectangle(bounds.Left, bounds.Top + ScaleLogical(7), ScaleLogical(3), Math.Max(0, bounds.Height - ScaleLogical(14)));
            _rounded.Draw(e.Graphics, marker, marker.Width / 2, new(theme.Accent), Color.Transparent, 0);
        }
        bounds.Inflate(-ScaleLogical(12), 0);
        TextRenderer.DrawText(e.Graphics, page.Text, Font, bounds, selected ? theme.Accent : theme.Foreground, TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        if (selected && _navigation.Focused) ControlPaint.DrawFocusRectangle(e.Graphics, Rectangle.Inflate(e.Bounds, -1, -1), theme.Accent, theme.BackstageNavigationBackground);
    }
    /// <inheritdoc />
    protected override void OnLoad(EventArgs e) { base.OnLoad(e); RefreshPages(); UpdateMetrics(true); }
    /// <inheritdoc />
    protected override void OnShown(EventArgs e)
    {
        // Load/DPI layout establishes the final size after the initial native
        // startup placement. Center that final rectangle on the actual owner.
        if (StartPosition == FormStartPosition.CenterParent) CenterToParent();
        FitWorkingArea();
        base.OnShown(e);
    }
    private void FitWorkingArea()
    {
        if (!IsDisposed && !Disposing) Bounds = RibbonQuickAccessEditorMetrics.ClampWindow(Bounds, Screen.FromControl(this).WorkingArea, DeviceDpi);
    }
    /// <inheritdoc />
    protected override void OnDpiChanged(DpiChangedEventArgs e)
    {
        base.OnDpiChanged(e); UpdateMetrics(false);
        // Run once more after WinForms installs the new monitor's suggested
        // bounds and scales child controls. Use that monitor's working area.
        if (IsHandleCreated) BeginInvoke(() => { UpdateMetrics(false); FitWorkingArea(); });
    }
    /// <inheritdoc />
    protected override void OnFontChanged(EventArgs e) { base.OnFontChanged(e); UpdateMetrics(false); }
    /// <inheritdoc />
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);
        if (e.Cancel) { _accepted = false; return; }
        if (!_accepted) foreach (var page in _pages.ToArray()) page.CancelDraft();
    }
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing) foreach (var page in _pages) page.TextChanged -= PageTextChanged;
        base.Dispose(disposing);
        if (disposing) { foreach (var font in _headingFonts) font.Dispose(); _headingFonts.Clear(); _headingFont = null; _rounded.Dispose(); }
    }
    private sealed class PageCollection(RibbonOptionsDialog owner) : Collection<RibbonOptionsPage>
    {
        protected override void InsertItem(int index, RibbonOptionsPage item)
        {
            ArgumentNullException.ThrowIfNull(item);
            if (item.IsDisposed || item.Parent is not null || Contains(item)) throw new InvalidOperationException("Options pages must be live, unattached controls.");
            var selected = owner.SelectedPage; base.InsertItem(index, item);
            item.Visible = false; owner._viewport.Controls.Add(item); item.TextChanged += owner.PageTextChanged; owner.RefreshPages(selected);
        }
        protected override void RemoveItem(int index)
        {
            var item = this[index]; item.TextChanged -= owner.PageTextChanged; owner._viewport.Controls.Remove(item); base.RemoveItem(index); owner.RefreshPages();
        }
        protected override void ClearItems() { while (Count > 0) RemoveAt(Count - 1); }
        protected override void SetItem(int index, RibbonOptionsPage item)
        {
            ArgumentNullException.ThrowIfNull(item); if (ReferenceEquals(item, this[index])) return;
            if (item.IsDisposed || item.Parent is not null || Contains(item)) throw new InvalidOperationException("Options pages must be live, unattached controls.");
            RemoveItem(index); InsertItem(index, item);
        }
    }
}
