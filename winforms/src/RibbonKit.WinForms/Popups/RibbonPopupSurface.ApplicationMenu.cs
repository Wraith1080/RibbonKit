using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

internal sealed partial class RibbonPopupSurface
{
    private RibbonApplicationMenuLayout? _applicationLayout;
    private RibbonDropDownItem? _applicationPane;
    private Size _applicationMaximum;
    private readonly Dictionary<Rectangle, VScrollBar> _applicationScrollers = [];
    private Panel? _applicationContentHost;
    private Region? _applicationItemClip;
    internal bool IsApplicationMenu => _applicationLayout is not null;
    internal RibbonDropDownItem? ApplicationPane => _applicationPane;
    internal RibbonApplicationMenuLayout? ApplicationLayout => _applicationLayout;
    internal void PreviewApplicationPane(RibbonDropDownItem pane)
    {
        if (!_ribbon.IsInDesignMode || !IsApplicationMenu) return;
        _applicationPane = pane; LayoutApplicationMenu();
    }
    internal bool IsApplicationNavigation(RibbonElement? item) => IsApplicationMenu && item is not null && _ribbon.FileMenu.Items.Contains(item);
    internal bool ValidateApplicationContent() => !IsApplicationMenu || _customContent is not ContainerControl container
        || container.ValidateChildren(ValidationConstraints.Enabled | ValidationConstraints.Visible);

    private void InitializeApplicationMenu(Size maximum)
    {
        AutoScroll = false;
        int S(int value) => RibbonLayoutEngine.Scale(value, _ribbon.DeviceDpi);
        _applicationMaximum = new(Math.Max(1, maximum.Width - S(24)), Math.Max(1, maximum.Height - S(32)));
        _applicationPane = _ribbon.ApplicationMenu.DefaultPane;
        try { LayoutApplicationMenu(); }
        catch { DetachContent(); DisposeApplicationMenu(); throw; }
    }
    private void LayoutApplicationMenu()
    {
        foreach (var bar in _applicationScrollers.Values) { Controls.Remove(bar); bar.Dispose(); }
        _applicationScrollers.Clear();
        _applicationLayout = RibbonApplicationMenuLayout.Build(_ribbon, _applicationPane!, _applicationMaximum);
        _targets.Clear(); _targets.AddRange(_applicationLayout.Targets);
        _applicationItemClip?.Dispose();
        _applicationItemClip = new RibbonApplicationMenuSurfaceMetrics(_ribbon.Theme, _ribbon.DeviceDpi, RibbonPresentationResolver.ApplicationMenuSurface)
            .ItemClip(_applicationLayout.Frame, _ribbon.Painter.GetPopupCornerRadius(_ribbon.Theme, _ribbon.DeviceDpi));
        Size = _applicationLayout.Size;
        void Scroller(Rectangle viewport, int extent, string name)
        {
            if (extent <= viewport.Height || viewport.Height <= 0) return;
            var bar = new VScrollBar { Name = name, AccessibleName = name, TabStop = false,
                Bounds = new(viewport.Right, viewport.Top, SystemInformation.GetVerticalScrollBarWidthForDpi(_ribbon.DeviceDpi), viewport.Height),
                Minimum = 0, Maximum = extent - 1, LargeChange = Math.Max(1, viewport.Height), SmallChange = Math.Max(1, Font.Height) };
            bar.ValueChanged += (_, _) => { _ribbon.Popups.PrepareChoice(this); _ribbon.SyncEditors(); Invalidate(); _ribbon.AccessibilityChanged(); };
            _applicationScrollers.Add(viewport, bar); Controls.Add(bar);
        }
        Scroller(_applicationLayout.Navigation, _applicationLayout.NavigationExtent, "Application menu navigation scroll");
        if (_applicationPane!.Content is null) Scroller(_applicationLayout.Pane, _applicationLayout.PaneExtent, "Application menu pane scroll");
        Scroller(_applicationLayout.Footer, _applicationLayout.FooterExtent, "Application menu footer scroll");
        AttachApplicationContent();
        Invalidate();
    }
    private IEnumerable<RibbonTarget> RawApplicationTargets() => _targets.SelectMany(t => RibbonApplicationMenuMetrics.Expand(t, _ribbon.DeviceDpi, _ribbon.Density));
    internal IReadOnlyList<RibbonTarget> ApplicationTargets() => RawApplicationTargets().Select(t =>
    {
        int offset = _applicationScrollers.TryGetValue(t.Clip, out var bar) ? bar.Value : 0;
        return t with { Bounds = t.Bounds with { Y = t.Bounds.Y - offset } };
    }).ToArray();
    private RibbonApplicationMenuPart ApplicationPart(RibbonTarget target) => _applicationLayout is not { } layout ? RibbonApplicationMenuPart.None
        : target.Clip == layout.Footer ? RibbonApplicationMenuPart.Footer : target.Clip == layout.Navigation ? RibbonApplicationMenuPart.Navigation : RibbonApplicationMenuPart.Pane;

    internal bool SelectApplicationPane(RibbonDropDownItem pane, bool focus = false)
    {
        if (!IsApplicationNavigation(pane) || pane is RibbonGalleryItem) return false;
        return SetApplicationPane(pane, focus);
    }
    private void HoverApplicationNavigation(RibbonTarget? target)
    {
        if (target is not { Enabled: true, Element: RibbonItem item } || !IsApplicationNavigation(item)
            || item is RibbonMenuSeparatorItem or IRibbonEditorItem) return;
        if (item is RibbonDropDownItem pane and not RibbonGalleryItem) SelectApplicationPane(pane);
        else SetApplicationPane(_ribbon.ApplicationMenu.DefaultPane, false);
    }
    private bool SetApplicationPane(RibbonDropDownItem pane, bool focus)
    {
        _ribbon.Popups.SettleMotion();
        if (ReferenceEquals(_applicationPane, pane)) { if (focus) FocusApplicationPane(); return true; }
        if (!ValidateApplicationContent()) return true;
        bool defaultPane = ReferenceEquals(pane, _ribbon.ApplicationMenu.DefaultPane);
        if ((!defaultPane && !_ribbon.Popups.PrepareApplicationPane(this, pane)) || IsDisposed || !_ribbon.Popups.Surfaces.Contains(this)) return true;
        // The opening callback may deliberately close File or remove the source.
        if (!defaultPane && (!IsApplicationNavigation(pane) || !pane.Visible || !pane.EffectiveEnabled)) return true;
        _ribbon.Popups.PrepareChoice(this);
        if (IsDisposed || !_ribbon.Popups.Surfaces.Contains(this)) return true;
        int navigationOffset = _applicationLayout is { } layout && _applicationScrollers.TryGetValue(layout.Navigation, out var navigationBar) ? navigationBar.Value : 0;
        int footerOffset = _applicationLayout is { } footerLayout && _applicationScrollers.TryGetValue(footerLayout.Footer, out var footerBar) ? footerBar.Value : 0;
        _ribbon.Editors.Release(this);
        DetachContent();
        var old = _applicationPane;
        _applicationPane = pane;
        try { LayoutApplicationMenu(); }
        catch { _ribbon.Popups.CloseAll(false); throw; }
        RestoreOffset(_applicationLayout!.Navigation, navigationOffset);
        RestoreOffset(_applicationLayout.Footer, footerOffset);
        if (!ReferenceEquals(old, _ribbon.ApplicationMenu.DefaultPane)) old?.OnClosed();
        if (IsDisposed || !_ribbon.Popups.Surfaces.Contains(this)) return true;
        if (focus) FocusApplicationPane();
        _ribbon.SyncEditors(); _ribbon.AccessibilityChanged();
        return true;
        void RestoreOffset(Rectangle viewport, int value)
        {
            if (_applicationScrollers.TryGetValue(viewport, out var bar)) bar.Value = Math.Clamp(value, 0, Math.Max(0, bar.Maximum - bar.LargeChange + 1));
        }
    }
    private void AttachApplicationContent()
    {
        if (_applicationPane?.Content is not { } body || _ribbon.IsInDesignMode) return;
        if (body.IsDisposed || body.Parent is not null) throw new InvalidOperationException("Application Menu content must remain live and unparented while hidden.");
        _savedContentDock = body.Dock; _savedContentAnchor = body.Anchor; _savedContentLocation = body.Location;
        _savedContentVisible = body.Visible;
        _customContent = body;
        _applicationContentHost ??= new Panel { AutoScroll = true, TabStop = false, AccessibleName = "Application menu content" };
        _applicationContentHost.BackColor = _ribbon.Theme.PopupColor;
        _applicationContentHost.Bounds = _applicationLayout!.Pane;
        Controls.Add(_applicationContentHost);
        body.Dock = DockStyle.None; body.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        if (body.BindingContext is { } context) body.BindingContext = context;
        body.Location = Point.Empty;
        _applicationContentHost.Controls.Add(body);
        body.Visible = true;
    }
    private void FocusApplicationPane()
    {
        if (HasCustomContent) { FocusContent(); return; }
        if (Targets.FirstOrDefault(t => t.Clip == _applicationLayout!.Pane && t.Enabled && t.Element is not null)?.Element is { } item) FocusElement(item);
        else Focus();
    }
    internal void RevealApplicationTarget(RibbonTarget target)
    {
        if (!_applicationScrollers.TryGetValue(target.Clip, out var bar)) return;
        int next = bar.Value;
        if (target.Bounds.Top - next < target.Clip.Top) next = target.Bounds.Top - target.Clip.Top;
        else if (target.Bounds.Bottom - next > target.Clip.Bottom) next = target.Bounds.Bottom - target.Clip.Bottom;
        bar.Value = Math.Clamp(next, 0, Math.Max(0, bar.Maximum - bar.LargeChange + 1));
    }
    private bool ScrollApplicationMenu(MouseEventArgs e)
    {
        if (!IsApplicationMenu) return false;
        var bar = _applicationScrollers.FirstOrDefault(pair => pair.Key.Contains(e.Location)).Value;
        if (bar is not null) bar.Value = Math.Clamp(bar.Value - Math.Sign(e.Delta) * Math.Max(Font.Height * 3, bar.SmallChange), 0, Math.Max(0, bar.Maximum - bar.LargeChange + 1));
        if (e is HandledMouseEventArgs handled) handled.Handled = true;
        return true;
    }
    private bool ProcessApplicationKey(Keys keys)
    {
        if (!IsApplicationMenu) return false;
        if (keys == Keys.Escape) { if (ValidateApplicationContent()) _ribbon.Popups.CloseTop(true); return true; }
        if (keys == Keys.Left)
        {
            if (_applicationPane is { } pane && IsApplicationNavigation(pane)) FocusElement(pane, pane is RibbonSplitButtonItem);
            else if (Targets.FirstOrDefault(t => IsApplicationNavigation(t.Element) && t.Enabled)?.Element is { } first) FocusElement(first);
            return true;
        }
        if (keys == Keys.Right && _focused is RibbonDropDownItem drop && IsApplicationNavigation(drop))
        { SelectApplicationPane(drop, true); return true; }
        if (keys is Keys.Tab or (Keys.Shift | Keys.Tab) && HasCustomContent)
        {
            if (keys == (Keys.Tab | Keys.Shift) && _focused is RibbonItem footer && _ribbon.ApplicationMenu.FooterItems.Contains(footer)) { FocusContent(); return true; }
            if (IsApplicationNavigation(_focused) && keys == Keys.Tab) { FocusContent(); return true; }
        }
        return false;
    }
    private bool ProcessApplicationContentKey(Keys keys)
    {
        if (!IsApplicationMenu) return false;
        if (keys == Keys.Escape) { if (ValidateApplicationContent()) _ribbon.Popups.CloseTop(true); return true; }
        if (keys == Keys.F6) { FocusElement(_applicationPane is { } pane && IsApplicationNavigation(pane) ? pane : _ribbon.FileMenu.Items.FirstOrDefault(i => i.Visible && i.EffectiveEnabled) ?? _ribbon.FileMenu); return true; }
        if (_customContent is not { } body || keys is not (Keys.Tab or (Keys.Tab | Keys.Shift))) return false;
        var focused = Descendants(body).FirstOrDefault(c => c.Focused);
        if (body.SelectNextControl(focused, keys == Keys.Tab, true, true, false)) return true;
        var targets = Targets.Where(t => t.Enabled && t.Element is not null).ToArray();
        var next = keys == Keys.Tab ? targets.FirstOrDefault(t => t.Clip == _applicationLayout!.Footer) : targets.LastOrDefault(t => IsApplicationNavigation(t.Element));
        if (next?.Element is { } item) FocusElement(item, next.DropDownPart); else Focus();
        return true;
    }
    private void PaintApplicationMenu(RibbonPaintContext context)
    {
        if (_applicationLayout is not { } layout) return;
        context = context with { ApplicationMenuDefaultPane = ReferenceEquals(_applicationPane, _ribbon.ApplicationMenu.DefaultPane),
            ApplicationMenuDividerX = layout.NavigationDividerX };
        _ribbon.Painter.DrawApplicationMenuBackground(context, layout.Navigation, layout.Pane, layout.Footer);
        if (!layout.Header.IsEmpty)
        {
            var saved = context.Graphics.Save();
            try
            {
                if (_applicationItemClip is not null) context.Graphics.SetClip(_applicationItemClip, System.Drawing.Drawing2D.CombineMode.Intersect);
                _ribbon.Painter.DrawMenuHeader(context with { Bounds = layout.Header, ApplicationMenuPart = RibbonApplicationMenuPart.Pane }, _applicationPane!.DropDownHeader);
            }
            finally { context.Graphics.Restore(saved); }
        }
        if (_customContent is null && _applicationPane is { } pane && (pane.ContentType is not null || !pane.Items.Any(i => i.Visible)))
        {
            int inset = RibbonLayoutEngine.Scale(12, _ribbon.DeviceDpi);
            _ribbon.Painter.DrawText(context with { Bounds = Rectangle.Inflate(layout.Pane, -Math.Min(inset, Math.Max(0, (layout.Pane.Width - 1) / 2)), -Math.Min(inset, Math.Max(0, (layout.Pane.Height - 1) / 2))) },
                pane.ContentType is not null ? "UserControl: " + pane.ContentType.Name : "No items",
                _ribbon.Theme.SecondaryText, TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
        }
    }
    internal void DetachApplicationContent(Control body)
    {
        if (_applicationContentHost is null) return;
        if (ReferenceEquals(body.Parent, _applicationContentHost)) _applicationContentHost.Controls.Remove(body);
        Controls.Remove(_applicationContentHost);
    }
    private void DisposeApplicationMenu()
    {
        foreach (var bar in _applicationScrollers.Values) bar.Dispose();
        _applicationScrollers.Clear();
        _applicationItemClip?.Dispose(); _applicationItemClip = null;
        _applicationContentHost?.Dispose(); _applicationContentHost = null;
        var pane = _applicationPane; _applicationPane = null;
        if (!ReferenceEquals(pane, _ribbon.ApplicationMenu.DefaultPane)) pane?.OnClosed();
    }
}
