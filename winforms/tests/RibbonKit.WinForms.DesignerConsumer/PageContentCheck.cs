using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

public partial class DesignerForm
{
    internal void VerifyPageContent()
    {
        Require(infoPage.ContentType == typeof(BackstageInfoControl) && printPage.ContentType == typeof(BackstagePrintControl), "Designer-authored ContentType assignments");
        Require(infoPage.Content is BackstageInfoControl && printPage.Content is BackstagePrintControl, "All declared page instances exist after InitializeComponent, before Form.Load or File clicks");
        var infoAtInitialization = (BackstageInfoControl)infoPage.Content!; var printAtInitialization = (BackstagePrintControl)printPage.Content!;
        Require(infoAtInitialization.Parent is null && printAtInitialization.Parent is null && !ribbon.IsBackstageOpen, "Initialization does not open Backstage or parent its page bodies");
        bool shown = Visible; var animation = ribbon.AnimationLevel;
        bool readyAtLoad = false;
        EventHandler loaded = (_, _) =>
        {
            readyAtLoad = ReferenceEquals(infoAtInitialization, infoPage.Content) && ReferenceEquals(printAtInitialization, printPage.Content);
            if (printPage.Content is BackstagePrintControl printBody) ChildControls(printBody).OfType<ComboBox>().Single().SelectedIndex = 1;
        };
        Load += loaded;
        ribbon.AnimationLevel = RibbonAnimationLevel.None;
        if (!shown) { ShowInTaskbar = false; StartPosition = FormStartPosition.Manual; Location = new Point(-16000, -16000); Show(); }
        try
        {
            Require(readyAtLoad || shown, "Form.Load can access both existing page instances");
            ribbon.IsBackstageOpen = true; Application.DoEvents();
            Require(ribbon.IsBackstageOpen && ReferenceEquals(infoAtInitialization, infoPage.Content) && ReferenceEquals(printAtInitialization, printPage.Content), "Opening reuses both initialized page instances");
            var info = (BackstageInfoControl)infoPage.Content!;
            Require(info.Controls.OfType<TableLayoutPanel>().Any() && ChildControls(info).OfType<FlowLayoutPanel>().Any(), "Ordinary designer-authored table and flow layouts");
            info.NotesEditor.Text = "Notes retained across pages"; info.NotesEditor.Select(2, 5); var handle = info.NotesEditor.Handle;
            ribbon.Backstage.SelectedPage = printPage; Application.DoEvents();
            var print = printPage.Content as BackstagePrintControl; Require(print is not null, "Switching reuses the initialized Print page");
            var orientation = ChildControls(print!).OfType<ComboBox>().Single(); Require(orientation.SelectedIndex == 1, "Print settings assigned in Form.Load survive first activation"); var printHandle = orientation.Handle;
            ribbon.Backstage.SelectedPage = infoPage; ribbon.IsBackstageOpen = false; ribbon.IsBackstageOpen = true; Application.DoEvents();
            Require(ReferenceEquals(info, infoPage.Content) && info.NotesEditor.Handle == handle && info.NotesEditor.Text == "Notes retained across pages"
                && info.NotesEditor.SelectionStart == 2 && info.NotesEditor.SelectionLength == 5, "Info identity, HWND, notes and selection retained across switching and reopening");
            ribbon.Backstage.SelectedPage = printPage;
            Require(ReferenceEquals(print, printPage.Content) && orientation.Handle == printHandle && orientation.SelectedIndex == 1, "Print editor identity and selected value retained");
            ribbon.Backstage.SelectedPage = infoPage; ribbon.IsBackstageOpen = false;
        }
        finally { Load -= loaded; ribbon.AnimationLevel = animation; if (!shown) Hide(); }
    }
    internal void VerifyPageContentDisposal()
    {
        var info = infoPage.Content!; var print = printPage.Content!;
        Dispose(); Require(info.IsDisposed && print.IsDisposed, "Form/ribbon disposal releases both page-owned UserControls");
    }
}
