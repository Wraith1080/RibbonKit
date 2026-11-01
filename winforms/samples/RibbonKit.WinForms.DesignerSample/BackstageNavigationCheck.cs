using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

public partial class DesignerForm
{
    internal void VerifyBackstageNavigation()
    {
        ribbon.AnimationLevel = RibbonAnimationLevel.None;
        ShowInTaskbar = false; StartPosition = FormStartPosition.Manual; Location = new(-16000, -16000); Show();
        Require(ribbon.Backstage.Items.Contains(backstageMainSeparator) && ribbon.Backstage.FooterItems.SequenceEqual(new RibbonItem[] { backstageFooterSeparator, backstageOptionsCommand }), "Generated navigation declarations");
        ribbon.IsBackstageOpen = true; Application.DoEvents();
        var options = ChildControls(this).Single(control => control is IButtonControl && control.Text == "Options");
        var info = ChildControls(this).Single(control => control is IButtonControl && control.Text == "Info");
        Require(!ReferenceEquals(options.Parent, info.Parent), "Bottom section has its own native viewport");
        Require(options.Parent!.Bottom > info.Parent!.Bottom && options.Bottom <= options.Parent.ClientSize.Height, "Bottom command visible below main viewport");
        var rules = ChildControls(this).Where(control => control.AccessibleRole == AccessibleRole.Separator).ToArray();
        Require(rules.Length == 2 && rules.All(control => !control.CanSelect && !control.TabStop), "Decorative main and bottom separators");
        var body = (BackstageInfoControl)infoPage.Content!; body.NotesEditor.Text = "Retained during resize"; body.NotesEditor.Select(1, 5);
        var handle = body.NotesEditor.Handle; var parent = body.Parent;
        ClientSize = new(900, 150); Application.DoEvents();
        Require(ReferenceEquals(options.Parent, info.Parent) && options.Parent is ScrollableControl { AutoScroll: true }, "Short-host combined scrollable navigation");
        options.Focus(); Application.DoEvents();
        Require(options.Focused && options.Bounds.IntersectsWith(options.Parent!.ClientRectangle), "Footer remains keyboard reachable in scrolling fallback");
        ClientSize = new(900, 600); Application.DoEvents();
        Require(!ReferenceEquals(options.Parent, info.Parent), "Restores pinned footer");
        Require(ReferenceEquals(body.Parent, parent) && body.NotesEditor.Handle == handle && body.NotesEditor.Text == "Retained during resize"
            && body.NotesEditor.SelectionStart == 1 && body.NotesEditor.SelectionLength == 5, "Native page identity, handle and selection");
        ((IButtonControl)options).PerformClick(); Require(!ribbon.IsBackstageOpen && statusLabel.Text == "Backstage Options", "Footer command closes Backstage before application event");
    }
}
