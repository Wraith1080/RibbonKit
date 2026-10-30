namespace RibbonKit.WinForms.Showcase;

public partial class MainForm
{
    private void InitializeBackdrops()
    {
        // Application-owned GDI content uses the library's opaque host. The
        // surrounding transparent margins expose the full-form native material.
        var content = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        var opaque = new RibbonBackdropPanel { Dock = DockStyle.Fill, BackColor = SystemColors.Control };
        statusStrip.BackColor = SystemColors.Control; status.BackColor = Color.Empty;
        Controls.Remove(document); Controls.Remove(statusStrip); opaque.Controls.Add(document); opaque.Controls.Add(statusStrip);
        if (_printPreview is { } preview)
        {
            // Preview shares the document's opaque client host. Even its first
            // visible/layout frame must stay below the ribbon and caption.
            Controls.Remove(preview); opaque.Controls.Add(preview); opaque.Controls.SetChildIndex(preview, 0);
        }
        content.Controls.Add(opaque); Controls.Add(content); Controls.SetChildIndex(content, 0);
        BackdropChanged += (_, _) => content.Padding = Backdrop == RibbonWindowBackdrop.None ? Padding.Empty : new Padding(12);
        var group = new RibbonGroup { Id = "group.window", Text = "Window" };
        var menu = new RibbonDropDownItem { Id = "demo.window.backdrop", Text = "Backdrop", KeyTip = "B", DropDownHeader = "Window backdrop", SimplifiedPlacement = RibbonSimplifiedPlacement.Inline,
            ScreenTipDescription = "Choose Solid, Mica or Acrylic for the window and ribbon. Native fields and the document stay on opaque surfaces. Windows may use a solid fallback." };
        Dictionary<RibbonWindowBackdrop, RibbonRadioButtonItem> choices = [];
        var transparent = new RibbonCheckBoxItem { Id = "demo.window.backdrop.transparentBackstage", Text = "Transparent Backstage",
            ScreenTipDescription = "Use a fully transparent Backstage navigation background. Page content stays opaque." };
        transparent.Click += (_, _) => ribbon.Backstage.NavigationBackgroundOpacity = transparent.Checked ? 0 : 1;
        void Sync()
        {
            foreach (var (value, item) in choices) item.Checked = Backdrop == value;
            transparent.Enabled = EffectiveBackdrop != RibbonWindowBackdrop.None;
            transparent.Checked = ribbon.Backstage.NavigationBackgroundOpacity < 1;
        }
        foreach (var value in Enum.GetValues<RibbonWindowBackdrop>())
        {
            var item = new RibbonRadioButtonItem { Id = "demo.window.backdrop." + value, Text = value == RibbonWindowBackdrop.None ? "Solid" : value.ToString(), GroupName = "showcase.window.backdrop" };
            item.Click += (_, _) =>
            {
                Backdrop = value; Sync();
                status.Text = value == RibbonWindowBackdrop.None ? "Solid window" : EffectiveBackdrop == value ? value + " window backdrop" : value + " requested; solid fallback is active";
            };
            choices.Add(value, item); menu.Items.Add(item);
        }
        menu.Items.AddRange(new RibbonMenuSeparatorItem(), transparent);
        BackdropChanged += (_, _) => Sync();
        menu.DropDownOpening += (_, _) => Sync(); group.Items.Add(menu); ribbon.Tabs.Single(t => t.Id == "tab.merging").Groups.Add(group); Sync();
        document.AppendText("\r\n\r\nView > Window > Backdrop selects Solid, Mica or Acrylic across the form and ribbon. Native fields and this document stay on opaque surfaces. With material enabled the header stays live while command content animates. Solid retains the full header animation.");
    }
}
