using System.ComponentModel;

namespace RibbonKit.WinForms;

/// <summary>An extensible options dialog containing shared Ribbon and QAT pages. Add application pages to Pages; attached pages are dialog-owned.</summary>
public sealed class RibbonQuickAccessCustomizationForm : RibbonOptionsDialog
{
    private readonly RibbonControl _ribbon;
    private readonly RibbonOptionsPage _classicPage, _simplifiedPage;
    internal RibbonOptionsPage ActiveRibbonPage => _ribbon.LayoutMode == RibbonLayoutMode.Simplified ? _simplifiedPage : _classicPage;
    /// <summary>Creates Ribbon and QAT options pages, selecting QAT initially. Each page applies independently; Cancel discards only unapplied edits since its latest checkpoint.</summary>
    public RibbonQuickAccessCustomizationForm(RibbonControl ribbon, string applicationId) : base(ribbon)
    {
        Name = "QuickAccessCustomizationForm";
        _ribbon = ribbon;
        Editor = new(ribbon, applicationId) { Dock = DockStyle.Fill };
        RibbonEditor = new(ribbon, applicationId) { Dock = DockStyle.Fill };
        SimplifiedEditor = new(ribbon, applicationId) { Dock = DockStyle.Fill };
        _classicPage = new CustomizeRibbonPage(RibbonEditor) { Text = "Customize Ribbon", Name = "CustomizeRibbonOptionsPage" };
        _simplifiedPage = new SimplifiedPage(SimplifiedEditor) { Text = "Customize Ribbon", Name = "CustomizeSimplifiedOptionsPage" };
        Pages.Add(ActiveRibbonPage);
        var page = new QuickAccessPage(Editor) { Text = "Quick Access Toolbar", Name = "QuickAccessOptionsPage" };
        Pages.Add(page); SelectedPage = page;
        ribbon.LayoutModeChanged += ModeChanged;
    }
    /// <summary>Gets the owned reusable editor and its draft session.</summary>
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public RibbonQuickAccessCustomizationEditor Editor { get; }
    /// <summary>Gets the owned reusable Customize Ribbon editor and its independent draft session.</summary>
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public RibbonCustomizationEditor RibbonEditor { get; }
    /// <summary>Gets the owned Simplified editor. Its page is offered only in Simplified mode; pending edits survive mode switches.</summary>
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public RibbonSimplifiedCustomizationEditor SimplifiedEditor { get; }
    private void ModeChanged(object? sender, EventArgs e)
    {
        var old = Pages.Contains(_classicPage) ? _classicPage : _simplifiedPage; var next = ActiveRibbonPage; if (ReferenceEquals(old, next)) return;
        var selected = SelectedPage; int at = Pages.IndexOf(old); if (at < 0) at = 0; else Pages.Remove(old);
        if (ReferenceEquals(next, _classicPage) && !RibbonEditor.Session.HasChanges) { RibbonEditor.Session.Reload(); RibbonEditor.RefreshDraft(); }
        if (ReferenceEquals(next, _simplifiedPage) && !SimplifiedEditor.Session.HasChanges) { SimplifiedEditor.Session.Reload(); SimplifiedEditor.RefreshDraft(); }
        Pages.Insert(at, next); SelectedPage = ReferenceEquals(selected, old) ? next : selected;
    }
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing) _ribbon.LayoutModeChanged -= ModeChanged;
        base.Dispose(disposing);
        if (disposing) { _classicPage.Dispose(); _simplifiedPage.Dispose(); }
    }
    private sealed class SimplifiedPage : RibbonOptionsPage
    {
        private readonly RibbonSimplifiedCustomizationEditor _editor;
        internal SimplifiedPage(RibbonSimplifiedCustomizationEditor editor) { _editor = editor; Controls.Add(editor); MinimumSize = editor.PageMinimum; }
        internal override bool ApplyCore() => _editor.ApplyChanges();
        internal override void CancelCore() => _editor.Session.Cancel();
        protected override void OnFontChanged(EventArgs e) { base.OnFontChanged(e); if (_editor is not null) { _editor.Font = Font; MinimumSize = _editor.PageMinimum; Parent?.PerformLayout(); } }
        protected override void OnDpiChangedAfterParent(EventArgs e) { base.OnDpiChangedAfterParent(e); MinimumSize = _editor.PageMinimum; Parent?.PerformLayout(); }
    }
    private sealed class CustomizeRibbonPage : RibbonOptionsPage
    {
        private readonly RibbonCustomizationEditor _editor;
        internal CustomizeRibbonPage(RibbonCustomizationEditor editor) { _editor = editor; Controls.Add(editor); MinimumSize = editor.PageMinimum; }
        internal override bool ApplyCore() => _editor.ApplyChanges();
        internal override void CancelCore() => _editor.Session.Cancel();
        private void RefreshMinimum() { var minimum = _editor.PageMinimum; MinimumSize = new(Math.Max(MinimumSize.Width, minimum.Width), minimum.Height); Parent?.PerformLayout(); }
        protected override void OnFontChanged(EventArgs e) { base.OnFontChanged(e); if (_editor is not null) { _editor.Font = Font; RefreshMinimum(); } }
        protected override void OnDpiChangedAfterParent(EventArgs e) { base.OnDpiChangedAfterParent(e); RefreshMinimum(); }
    }
    private sealed class QuickAccessPage : RibbonOptionsPage
    {
        private readonly RibbonQuickAccessCustomizationEditor _editor;
        internal QuickAccessPage(RibbonQuickAccessCustomizationEditor editor) { _editor = editor; Controls.Add(editor); MinimumSize = editor.PageMinimum; }
        internal override bool ApplyCore() => _editor.ApplyChanges();
        internal override void CancelCore() => _editor.Session.Cancel();
        private void RefreshMinimum()
        {
            var minimum = _editor.PageMinimum;
            MinimumSize = new(Math.Max(MinimumSize.Width, minimum.Width), minimum.Height);
            Parent?.PerformLayout();
        }
        protected override void OnFontChanged(EventArgs e) { base.OnFontChanged(e); if (_editor is not null) { _editor.Font = Font; RefreshMinimum(); } }
        protected override void OnDpiChangedAfterParent(EventArgs e) { base.OnDpiChangedAfterParent(e); RefreshMinimum(); }
    }
}
