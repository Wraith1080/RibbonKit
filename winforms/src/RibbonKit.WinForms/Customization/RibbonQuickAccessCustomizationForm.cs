using System.ComponentModel;

namespace RibbonKit.WinForms;

/// <summary>An extensible options dialog containing the shared QAT page. Add application pages to Pages; attached pages are dialog-owned.</summary>
public sealed class RibbonQuickAccessCustomizationForm : RibbonOptionsDialog
{
    /// <summary>Creates the QAT options dialog. Each page applies independently; Cancel discards only unapplied edits since its latest checkpoint.</summary>
    public RibbonQuickAccessCustomizationForm(RibbonControl ribbon, string applicationId) : base(ribbon)
    {
        Name = "QuickAccessCustomizationForm";
        Editor = new(ribbon, applicationId) { Dock = DockStyle.Fill };
        var page = new QuickAccessPage(Editor) { Text = "Quick Access Toolbar", Name = "QuickAccessOptionsPage" };
        Pages.Add(page); SelectedPage = page;
    }
    /// <summary>Gets the owned reusable editor and its draft session.</summary>
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public RibbonQuickAccessCustomizationEditor Editor { get; }
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
