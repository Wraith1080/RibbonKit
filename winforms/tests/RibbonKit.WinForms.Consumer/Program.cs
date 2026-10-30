using System.ComponentModel;
using RibbonKit.WinForms;

namespace IndependentConsumer;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Length == 2 && args[0] == "--backdrop-startup-checks") return WindowBackdropChecks.RunStartup(args[1]);
        if (args.Length == 2 && args[0] is "--backdrop-transparency-checks" or "--backdrop-transparency-checks-flat") return WindowBackdropChecks.Run(args[1], flat: args[0].EndsWith("-flat", StringComparison.Ordinal), transparency: true);
        if (args.Length == 2 && args[0] == "--backdrop-simplified-checks") return WindowBackdropChecks.Run(args[1], simplified: true);
        if (args.Length == 2 && args[0] is "--backdrop-checks" or "--backdrop-checks-flat") return WindowBackdropChecks.Run(args[1], args[0].EndsWith("-flat", StringComparison.Ordinal));
        if (args.Length == 3 && args[0] is "--window-motion-regression" or "--window-motion-regression-flat")
            return IndependentConsumer.WindowMotionRegressionChecks.Run(args[2], args[1], args[0].EndsWith("-flat", StringComparison.Ordinal));
        if (args.Length == 2 && args[0] is "--window-projection-checks" or "--window-projection-checks-flat")
            return IndependentConsumer.WindowIntegrationChecks.Run(args[1], args[0] == "--window-projection-checks-flat", projectionsOnly: true);
        if (args.Length == 2 && args[0] is "--window-snap-checks" or "--window-snap-checks-flat")
            return IndependentConsumer.WindowIntegrationChecks.Run(args[1], args[0] == "--window-snap-checks-flat", frameOnly: true, snapOnly: true);
        if (args.Length == 2 && args[0] is "--window-tail-checks" or "--window-tail-checks-flat")
            return IndependentConsumer.WindowIntegrationChecks.Run(args[1], args[0] == "--window-tail-checks-flat", frameOnly: true, tailOnly: true);
        if (args.Length == 2 && args[0] is "--window-frame-checks" or "--window-frame-checks-flat")
            return IndependentConsumer.WindowIntegrationChecks.Run(args[1], args[0] == "--window-frame-checks-flat", frameOnly: true);
        if (args.Length == 2 && args[0] is "--validate-window" or "--validate-window-flat")
            return IndependentConsumer.WindowIntegrationChecks.Run(args[1], args[0] == "--validate-window-flat");
        if (args.Length == 2 && args[0] is "--validate-group-fade" or "--validate-group-fade-flat")
            return GroupMotionInteractionChecks.Run(args[1], args[0] == "--validate-group-fade-flat", true);
        if (args.Length == 2 && args[0] is "--validate-group-motion" or "--validate-group-motion-flat")
            return GroupMotionInteractionChecks.Run(args[1], args[0] == "--validate-group-motion-flat");
        if (args.Length == 2 && args[0] is "--validate-backstage-motion" or "--validate-backstage-motion-flat")
            return BackstageInteractionChecks.Run(args[1], args[0] == "--validate-backstage-motion-flat", motionChecks: true);
        if (args.Length == 2 && args[0] is "--validate-popup-motion-input" or "--validate-popup-motion-input-flat")
            return PopupMotionInteractionChecks.Run(args[1], args[0] == "--validate-popup-motion-input-flat", true);
        if (args.Length == 2 && args[0] is "--validate-popup-motion" or "--validate-popup-motion-flat")
            return PopupMotionInteractionChecks.Run(args[1], args[0] == "--validate-popup-motion-flat");
        if (args.Length == 2 && args[0] is "--validate-minimize-motion" or "--validate-minimize-motion-flat")
            return GeometryMotionInteractionChecks.Run(args[1], args[0] == "--validate-minimize-motion-flat", minimizeOnly: true);
        if (args.Length == 2 && args[0] == "--validate-expressive-pill")
            return GeometryMotionInteractionChecks.Run(args[1], false, tabsOnly: true, tabLevel: RibbonAnimationLevel.Expressive);
        if (args.Length == 2 && args[0] is "--validate-header-arrivals" or "--validate-header-arrivals-flat")
            return GeometryMotionInteractionChecks.Run(args[1], args[0] == "--validate-header-arrivals-flat", headersOnly: true);
        if (args.Length == 2 && args[0] is "--validate-tab-slide-motion" or "--validate-tab-slide-motion-flat")
            return GeometryMotionInteractionChecks.Run(args[1], args[0] == "--validate-tab-slide-motion-flat", false, true, RibbonTabContentAnimation.SlideDown);
        if (args.Length == 2 && args[0] is "--validate-tab-motion" or "--validate-tab-motion-flat")
            return GeometryMotionInteractionChecks.Run(args[1], args[0] == "--validate-tab-motion-flat", false, true);
        if (args.Length == 2 && args[0] is "--validate-geometry-menu-motion" or "--validate-geometry-menu-motion-flat")
            return GeometryMotionInteractionChecks.Run(args[1], args[0] == "--validate-geometry-menu-motion-flat", true);
        if (args.Length == 2 && args[0] is "--validate-geometry-motion" or "--validate-geometry-motion-flat")
            return GeometryMotionInteractionChecks.Run(args[1], args[0] == "--validate-geometry-motion-flat");
        if (args.Length == 2 && args[0] is "--validate-simplified-context" or "--validate-simplified-context-flat")
            return SimplifiedContextInteractionChecks.Run(args[1], args[0] == "--validate-simplified-context-flat");
        if (args.Length == 2 && args[0] is "--validate-review" or "--validate-review-flat")
            return ReviewInteractionChecks.Run(args[1], args[0] == "--validate-review-flat");
        if (args.Length == 2 && args[0] is "--validate-options-popups" or "--validate-options-popups-flat")
            return OptionsPopupInteractionChecks.Run(args[1], args[0] == "--validate-options-popups-flat");
        if (args.Length == 2 && args[0] is "--validate-simplified" or "--validate-simplified-flat")
            return SimplifiedInteractionChecks.Run(args[1], args[0] == "--validate-simplified-flat");
        if (args.Length == 2 && args[0] is "--validate-customization-editor" or "--validate-customization-editor-flat")
            return CustomizationEditorInteractionChecks.Run(args[1], args[0] == "--validate-customization-editor-flat");
        if (args.Length == 2 && args[0] is "--validate-popup-refinements" or "--validate-popup-refinements-flat")
            return PopupRefinementChecks.Run(args[1], args[0] == "--validate-popup-refinements-flat");
        if (args.Length == 2 && args[0] is "--validate-gallery-reduction" or "--validate-gallery-reduction-flat")
            return InRibbonGalleryInteractionChecks.Run(args[1], args[0] == "--validate-gallery-reduction-flat", reductionOnly: true);
        if (args.Length == 2 && args[0] is "--validate-density" or "--validate-density-flat")
            return DensityInteractionChecks.Run(args[1], args[0] == "--validate-density-flat");
        if (args.Length == 2 && args[0] is "--validate-modal" or "--validate-modal-flat")
            return ModalInteractionChecks.Run(args[1], args[0] == "--validate-modal-flat");
        if (args.Length == 2 && args[0] is "--validate-merge" or "--validate-merge-flat")
            return MergeInteractionChecks.Run(args[1], args[0] == "--validate-merge-flat");
        if (args.Length == 2 && args[0] is "--validate-gallery-strip" or "--validate-gallery-strip-flat")
            return InRibbonGalleryInteractionChecks.Run(args[1], args[0] == "--validate-gallery-strip-flat", stripOnly: true);
        if (args.Length == 2 && args[0] is "--validate-qat-gallery" or "--validate-qat-gallery-flat")
            return InRibbonGalleryInteractionChecks.Run(args[1], args[0] == "--validate-qat-gallery-flat", true);
        if (args.Length == 2 && args[0] is "--validate-in-ribbon-gallery" or "--validate-in-ribbon-gallery-flat")
            return InRibbonGalleryInteractionChecks.Run(args[1], args[0] == "--validate-in-ribbon-gallery-flat");
        if (args.Length == 2 && args[0] is "--validate-gallery" or "--validate-gallery-flat")
            return GalleryInteractionChecks.Run(args[1], args[0] == "--validate-gallery-flat");
        if (args.Length == 2 && args[0] is "--validate-backstage-width" or "--validate-backstage-width-flat")
            return BackstageInteractionChecks.Run(args[1], args[0] == "--validate-backstage-width-flat", true);
        if (args.Length == 2 && args[0] is "--validate-backstage" or "--validate-backstage-flat")
            return BackstageInteractionChecks.Run(args[1], args[0] == "--validate-backstage-flat");
        if (args.Length == 2 && args[0] is "--validate-file" or "--validate-file-flat")
            return FileMenuInteractionChecks.Run(args[1], args[0] == "--validate-file-flat");
        if (args.Length == 2 && args[0] is "--validate-contextual" or "--validate-contextual-flat")
            return ContextualTabInteractionChecks.Run(args[1], args[0] == "--validate-contextual-flat");
        if (args.Length == 2 && args[0] is "--validate-qat-options" or "--validate-qat-options-flat")
            return QuickAccessOptionInteractionChecks.Run(args[1], args[0] == "--validate-qat-options-flat");
        if (args.Length == 2 && args[0] is "--validate-qat-groups" or "--validate-qat-groups-flat")
            return QuickAccessGroupInteractionChecks.Run(args[1], args[0] == "--validate-qat-groups-flat");
        if (args.Length == 2 && args[0] is "--validate-qat-menus" or "--validate-qat-menus-flat")
            return QuickAccessMenuInteractionChecks.Run(args[1], args[0] == "--validate-qat-menus-flat");
        if (args.Length == 2 && args[0] is "--validate-qat-combo" or "--validate-qat-combo-flat")
            return QuickAccessComboInteractionChecks.Run(args[1], args[0] == "--validate-qat-combo-flat");
        if (args.Length == 2 && args[0] is "--validate-qat" or "--validate-qat-flat")
            return QuickAccessInteractionChecks.Run(args[1], args[0] == "--validate-qat-flat");
        if (args.Length == 2 && args[0] is "--validate-minimize" or "--validate-minimize-flat")
            return MinimizeInteractionChecks.Run(args[1], args[0] == "--validate-minimize-flat");
        if (args.Length == 2 && args[0] is "--validate-menus" or "--validate-menus-flat")
            return MenuInteractionChecks.Run(args[1], args[0] == "--validate-menus-flat");
        if (args.Length == 2 && args[0] is "--validate-screentips" or "--validate-screentips-flat")
            return ScreenTipInteractionChecks.Run(args[1], args[0] == "--validate-screentips-flat");
        if (args.Length == 2 && args[0] is "--validate-options" or "--validate-options-flat")
            return OptionInteractionChecks.Run(args[1], args[0] == "--validate-options-flat");
        if (args.Length == 2 && args[0] is "--validate-combo" or "--validate-combo-flat")
            return ComboBoxInteractionChecks.Run(args[1], args[0] == "--validate-combo-flat");
        if (args.Length == 2 && args[0] == "--validate-editors")
            return TextBoxInteractionChecks.Run(args[1]);
        if (args.Length == 2 && args[0] is "--validate-split" or "--validate-split-flat")
            return SplitInteractionChecks.Run(args[1], args[0] == "--validate-split-flat");
        if (args.Length == 2 && args[0] == "--validate-group-padding")
            return SplitInteractionChecks.Run(args[1], groupPaddingOnly: true);
        if (args.Length == 2 && args[0] is "--validate-touch-split" or "--validate-touch-split-flat")
            return SplitInteractionChecks.Run(args[1], args[0] == "--validate-touch-split-flat", true);
        if (args.Length == 2 && args[0] is "--validate-messages" or "--validate-messages-flat")
            return MessageInteractionChecks.Run(args[1], args[0] == "--validate-messages-flat");
        if (args.Length == 2 && args[0] is "--validate-message-motion" or "--validate-message-motion-flat")
            return MessageInteractionChecks.Run(args[1], args[0] == "--validate-message-motion-flat", true);
        if (args.Length == 2 && args[0] is "--validate-pre-m6b-review" or "--validate-pre-m6b-review-flat")
            return PreM6BReviewChecks.Run(args[1], args[0] == "--validate-pre-m6b-review-flat");
        if (args.Length == 2 && args[0] is "--validate-customization-alignment" or "--validate-customization-alignment-flat")
            return PreM6BReviewChecks.Run(args[1], args[0] == "--validate-customization-alignment-flat", optionsOnly: true);
        if (args.Length == 2 && args[0] is "--validate-review-followup" or "--validate-review-followup-flat")
            return ReviewFollowupChecks.Run(args[1], args[0] == "--validate-review-followup-flat");
        if (args.Length == 2 && args[0] is "--validate-customization" or "--validate-customization-flat")
            return CustomizationInteractionChecks.Run(args[1], args[0] == "--validate-customization-flat");
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
        using var form = new ConsumerForm();
        Application.Run(form);
        return 0;
    }
}

internal sealed class ConsumerForm : Form
{
    private readonly BindingSource _source = new();
    private readonly FormatState _state = new();
    private readonly Bitmap _image = new(32, 32);
    private int _copies, _choices;

    public ConsumerForm()
    {
        Width = 1000; Height = 350; StartPosition = FormStartPosition.CenterScreen;
        using (var g = Graphics.FromImage(_image))
        {
            g.Clear(Color.Transparent);
            using var pen = new Pen(Color.SteelBlue, 2);
            g.DrawRectangle(pen, 7, 3, 18, 25);
            g.DrawLine(pen, 10, 12, 22, 12);
        }
        var ribbon = new RibbonControl { Name = "native.ribbon" };
        var home = new RibbonTab { Id = "tab.home", Text = "Home", KeyTip = "H" };
        var group = new RibbonGroup { Id = "group.clipboard", Text = "Clipboard" };
        var copy = new RibbonButtonItem { Id = "cmd.copy", Text = "Copy", Image = _image };
        copy.Click += (_, _) => { _copies++; UpdateTitle(); };
        var bold = new RibbonToggleItem { Id = "cmd.bold", Text = "Bold" };
        _source.DataSource = _state;
        bold.DataBindings.Add(new Binding(nameof(RibbonToggleItem.Checked), _source, nameof(FormatState.Bold), true, DataSourceUpdateMode.OnPropertyChanged));
        _state.PropertyChanged += (_, _) => UpdateTitle();
        var menu = new RibbonDropDownItem { Id = "cmd.menu", Text = "Menu" };
        var choice = new RibbonButtonItem { Id = "cmd.choice", Text = "Choice" };
        choice.Click += (_, _) => { _choices++; UpdateTitle(); };
        menu.Items.Add(choice);
        group.Items.AddRange(copy, bold, new RibbonButtonItem { Id = "cmd.disabled", Text = "Disabled", Enabled = false }, menu);
        home.Groups.Add(group);
        var insert = new RibbonTab { Id = "tab.insert", Text = "Insert", KeyTip = "N" };
        var links = new RibbonGroup { Id = "group.links", Text = "Links" };
        links.Items.Add(new RibbonButtonItem { Id = "cmd.link", Text = "Link" }); insert.Groups.Add(links);
        ribbon.Tabs.AddRange(home, insert);
        Controls.Add(new TextBox { Name = "document", Dock = DockStyle.Fill, Multiline = true, Text = "Independent package consumer with application-owned image and BindingSource." });
        Controls.Add(ribbon);
        UpdateTitle();
    }
    private void UpdateTitle() => Text = $"Copy={_copies}; Bold={_state.Bold}; Choice={_choices}";
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) { _source.Dispose(); _image.Dispose(); }
    }
    private sealed class FormatState : INotifyPropertyChanged
    {
        private bool _bold;
        public bool Bold { get => _bold; set { if (_bold == value) return; _bold = value; PropertyChanged?.Invoke(this, new(nameof(Bold))); } }
        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
