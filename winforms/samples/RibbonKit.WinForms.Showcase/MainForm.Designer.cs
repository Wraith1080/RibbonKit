#nullable enable
namespace RibbonKit.WinForms.Showcase;

partial class MainForm
{
    private System.ComponentModel.IContainer? components;
    private RibbonControl ribbon = null!;
    private RibbonTab home = null!, insert = null!;
    private RibbonGroup clipboard = null!, font = null!, editing = null!, illustrations = null!, links = null!;
    private RibbonButtonItem copy = null!, paste = null!, cut = null!, find = null!, disabled = null!, picture = null!, link = null!, rectangle = null!, ellipse = null!;
    private RibbonToggleItem bold = null!, italic = null!;
    private RibbonDropDownItem select = null!, shapes = null!;
    private RibbonButtonItem selectAll = null!, selectNone = null!;
    private TextBox document = null!;
    private StatusStrip statusStrip = null!;
    private ToolStripStatusLabel status = null!;
    private BindingSource formattingSource = null!;
    protected override void Dispose(bool disposing) { if (disposing) components?.Dispose(); base.Dispose(disposing); }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        ribbon = new RibbonControl(); home = new RibbonTab(); insert = new RibbonTab();
        clipboard = new RibbonGroup(); font = new RibbonGroup(); editing = new RibbonGroup(); illustrations = new RibbonGroup(); links = new RibbonGroup();
        copy = new RibbonButtonItem(); paste = new RibbonButtonItem(); cut = new RibbonButtonItem(); find = new RibbonButtonItem(); disabled = new RibbonButtonItem(); picture = new RibbonButtonItem(); link = new RibbonButtonItem(); rectangle = new RibbonButtonItem(); ellipse = new RibbonButtonItem();
        bold = new RibbonToggleItem(); italic = new RibbonToggleItem(); select = new RibbonDropDownItem(); shapes = new RibbonDropDownItem(); selectAll = new RibbonButtonItem(); selectNone = new RibbonButtonItem();
        document = new TextBox(); statusStrip = new StatusStrip(); status = new ToolStripStatusLabel(); formattingSource = new BindingSource(components);
        ((System.ComponentModel.ISupportInitialize)ribbon).BeginInit(); SuspendLayout();
        components.Add(home, "home"); components.Add(insert, "insert"); components.Add(clipboard, "clipboard"); components.Add(font, "font"); components.Add(editing, "editing"); components.Add(illustrations, "illustrations"); components.Add(links, "links");
        components.Add(copy, "copy"); components.Add(paste, "paste"); components.Add(cut, "cut"); components.Add(find, "find"); components.Add(disabled, "disabled"); components.Add(picture, "picture"); components.Add(link, "link"); components.Add(rectangle, "rectangle"); components.Add(ellipse, "ellipse"); components.Add(bold, "bold"); components.Add(italic, "italic"); components.Add(select, "select"); components.Add(shapes, "shapes"); components.Add(selectAll, "selectAll"); components.Add(selectNone, "selectNone");
        ribbon.Name = "ribbon";
        home.Id = "tab.home"; home.Text = "Home"; home.KeyTip = "H";
        insert.Id = "tab.insert"; insert.Text = "Insert"; insert.KeyTip = "N";
        clipboard.Id = "group.clipboard"; clipboard.Text = "Clipboard"; clipboard.KeyTip = "ZC"; clipboard.ReductionMode = RibbonGroupReductionMode.ResizeThenCollapse;
        font.Id = "group.font"; font.Text = "Font"; font.KeyTip = "ZF"; font.ReductionMode = RibbonGroupReductionMode.ResizeThenCollapse;
        editing.Id = "group.editing"; editing.Text = "Editing"; editing.ReductionPriority = 10; editing.KeyTip = "ZE";
        illustrations.Id = "group.illustrations"; illustrations.Text = "Illustrations"; illustrations.ReductionMode = RibbonGroupReductionMode.ResizeThenCollapse;
        links.Id = "group.links"; links.Text = "Links"; links.CanResize = false;
        copy.Id = "cmd.copy"; copy.Text = "Copy"; copy.KeyTip = "C"; copy.SizePolicy = RibbonSizePolicy.Adaptive(RibbonItemSize.Large, RibbonItemSize.Medium, RibbonItemSize.Small); copy.Click += CopyClick;
        paste.Id = "cmd.paste"; paste.Text = "Paste"; paste.KeyTip = "V"; paste.SizePolicy = RibbonSizePolicy.Adaptive(RibbonItemSize.Large, RibbonItemSize.Medium, RibbonItemSize.Small); paste.Click += PasteClick;
        cut.Id = "cmd.cut"; cut.Text = "Cut"; cut.KeyTip = "X"; cut.SizePolicy = RibbonSizePolicy.Adaptive(RibbonItemSize.Large, RibbonItemSize.Medium, RibbonItemSize.Small); cut.Click += ActionClick;
        bold.Id = "cmd.bold"; bold.Text = "Bold"; bold.KeyTip = "B"; bold.SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium); bold.CheckedChanged += BoldChanged;
        italic.Id = "cmd.italic"; italic.Text = "Italic"; italic.KeyTip = "I"; italic.SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium);
        disabled.Id = "cmd.disabled"; disabled.Text = "Unavailable"; disabled.Enabled = false; disabled.SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium);
        find.Id = "cmd.find"; find.Text = "Find in document"; find.KeyTip = "F"; find.Click += ActionClick;
        select.Id = "cmd.select"; select.Text = "Select"; select.KeyTip = "S";
        selectAll.Id = "cmd.selectAll"; selectAll.Text = "Select all"; selectAll.KeyTip = "A"; selectAll.Click += ActionClick;
        selectNone.Id = "cmd.selectNone"; selectNone.Text = "Clear selection"; selectNone.KeyTip = "C"; selectNone.Click += ActionClick;
        picture.Id = "cmd.picture"; picture.Text = "Picture"; picture.KeyTip = "P"; picture.SizePolicy = RibbonSizePolicy.Adaptive(RibbonItemSize.Large, RibbonItemSize.Medium, RibbonItemSize.Small); picture.Click += ActionClick;
        shapes.Id = "cmd.shapes"; shapes.Text = "Shapes"; shapes.KeyTip = "S"; shapes.SizePolicy = RibbonSizePolicy.Adaptive(RibbonItemSize.Large, RibbonItemSize.Medium, RibbonItemSize.Small);
        rectangle.Id = "cmd.rectangle"; rectangle.Text = "Rectangle"; rectangle.KeyTip = "R"; rectangle.Click += ActionClick;
        ellipse.Id = "cmd.ellipse"; ellipse.Text = "Ellipse"; ellipse.KeyTip = "E"; ellipse.Click += ActionClick;
        link.Id = "cmd.link"; link.Text = "Hyperlink"; link.KeyTip = "L"; link.Click += ActionClick;
        select.Items.AddRange(new RibbonItem[] { selectAll, selectNone }); shapes.Items.AddRange(new RibbonItem[] { rectangle, ellipse });
        clipboard.Items.AddRange(new RibbonItem[] { paste, copy, cut }); font.Items.AddRange(new RibbonItem[] { bold, italic, disabled }); editing.Items.AddRange(new RibbonItem[] { find, select });
        illustrations.Items.AddRange(new RibbonItem[] { picture, shapes }); links.Items.Add(link);
        home.Groups.AddRange(new RibbonGroup[] { clipboard, font, editing }); insert.Groups.AddRange(new RibbonGroup[] { illustrations, links }); ribbon.Tabs.AddRange(new RibbonTab[] { home, insert });
        document.Dock = DockStyle.Fill; document.Multiline = true; document.ScrollBars = ScrollBars.Vertical; document.Name = "document";
        statusStrip.Items.AddRange(new ToolStripItem[] { status }); status.Text = "Ready";
        AutoScaleMode = AutoScaleMode.Dpi; ClientSize = new Size(1080, 540); MinimumSize = new Size(240, 300); StartPosition = FormStartPosition.CenterScreen; Text = "RibbonKit.WinForms — native foundation";
        Controls.Add(document); Controls.Add(ribbon); Controls.Add(statusStrip);
        ((System.ComponentModel.ISupportInitialize)ribbon).EndInit(); ResumeLayout(false); PerformLayout();
    }
}
