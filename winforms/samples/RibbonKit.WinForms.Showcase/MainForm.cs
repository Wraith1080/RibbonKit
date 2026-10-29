using System.ComponentModel;

namespace RibbonKit.WinForms.Showcase;

public partial class MainForm : RibbonForm
{
    private readonly FormattingState _formatting = new();
    public MainForm()
    {
        InitializeComponent();
        Ribbon = ribbon;
        ribbon.QuickAccessPosition = RibbonQuickAccessPosition.TitleBar;
        paste.DropDownHeader = "Paste options"; select.DropDownHeader = "Selection options";
        trackChanges.DropDownHeader = "Track changes"; shapes.DropDownHeader = "Choose a shape"; inputMenu.DropDownHeader = "Input options";
        foreach (var dropdown in components!.Components.OfType<RibbonDropDownItem>().Where(d => d is not RibbonGalleryItem && d.DropDownHeader.Length == 0)) dropdown.DropDownHeader = dropdown.Text;
        ribbon.FileMenu.Id = "file"; ribbon.FileMenu.Visible = true;
        ribbon.FileMenu.ScreenTipDescription = "Open document commands.";
        var newDocument = new RibbonButtonItem { Id = "file.new", Text = "New", KeyTip = "N", MenuDescription = "Create a document." };
        var openDocument = new RibbonButtonItem { Id = "file.open", Text = "Open", KeyTip = "O", MenuDescription = "Open an existing document." };
        var saveDocument = new RibbonButtonItem { Id = "file.save", Text = "Save", KeyTip = "S" };
        var recentDocuments = new RibbonDropDownItem { Id = "file.recent", Text = "Recent", KeyTip = "R", DropDownHeader = "Recent documents" };
        var recentDocument = new RibbonButtonItem { Id = "file.recent.example", Text = "Example document", KeyTip = "E" }; recentDocument.Click += ActionClick;
        recentDocuments.Items.Add(recentDocument);
        newDocument.Click += ActionClick; openDocument.Click += ActionClick; saveDocument.Click += ActionClick;
        ribbon.FileMenu.Items.AddRange(newDocument, openDocument, saveDocument, new RibbonMenuSeparatorItem(), recentDocuments);
        formattingSource.DataSource = _formatting;
        InitializeBackstage();
        InitializeGalleries();
        bold.DataBindings.Add(new Binding(nameof(RibbonToggleItem.Checked), formattingSource, nameof(FormattingState.Bold), true, DataSourceUpdateMode.OnPropertyChanged));
        nameInput.DataBindings.Add(new Binding(nameof(RibbonTextBoxItem.Value), formattingSource, nameof(FormattingState.Name), true, DataSourceUpdateMode.OnPropertyChanged));
        fontInput.DataBindings.Add(new Binding(nameof(RibbonComboBoxItem.Value), formattingSource, nameof(FormattingState.FontName), true, DataSourceUpdateMode.OnPropertyChanged));
        zoomInput.DataBindings.Add(new Binding(nameof(RibbonComboBoxItem.Value), formattingSource, nameof(FormattingState.Zoom), true, DataSourceUpdateMode.OnPropertyChanged));
        showRuler.DataBindings.Add(new Binding(nameof(RibbonCheckBoxItem.Checked), formattingSource, nameof(FormattingState.Ruler), true, DataSourceUpdateMode.OnPropertyChanged));
        popupRuler.DataBindings.Add(new Binding(nameof(RibbonCheckBoxItem.Checked), formattingSource, nameof(FormattingState.Ruler), true, DataSourceUpdateMode.OnPropertyChanged));
        showGrid.DataBindings.Add(new Binding(nameof(RibbonCheckBoxItem.CheckState), formattingSource, nameof(FormattingState.Grid), false, DataSourceUpdateMode.OnPropertyChanged));
        ribbon.QuickAccessItems.AddRange(
            new RibbonQuickAccessReference { Source = bold },
            new RibbonQuickAccessReference { Source = italic });
        var moveQuickAccess = new RibbonButtonItem { Id = "cmd.qat.move", Text = "Move Quick Access Toolbar", ScreenTipDescription = "Move the toolbar between the title bar, tab row and below the ribbon." };
        moveQuickAccess.Click += (_, _) => ribbon.QuickAccessPosition = ribbon.QuickAccessPosition switch { RibbonQuickAccessPosition.TitleBar => RibbonQuickAccessPosition.TabRow, RibbonQuickAccessPosition.TabRow => RibbonQuickAccessPosition.BelowRibbon, _ => RibbonQuickAccessPosition.TitleBar };
        ribbon.CommandCatalog.Commands.Add(moveQuickAccess);
        document.Text = "Native RibbonKit.WinForms\r\n\r\nInputs demonstrates native TextBox and ComboBox editing, binding, read-only/disabled fields and popup ownership. Font selects a choice; Zoom also accepts typed percentages (25% to 500%). Resize to open the same editors in collapsed groups.\r\n\r\nF4 or Alt+Down opens the choice popup. Escape closes that list first, then its containing ribbon popup. Enter validates and commits; Escape outside a list restores the last committed value. Tab visits fields; F6 returns to this document. Alt/F10, E, F focuses Font.\r\n\r\nThe document, status text, validation rules and data belong to this application. Home still demonstrates split buttons.";
        document.AppendText("\r\n\r\nOptions demonstrates compact check/radio choices. Gridlines cycles through three states. Ruler and its popup entry share an application binding. Resize to open the same options in collapsed groups.");
        document.AppendText("\r\n\r\nScreenTips: pause over Home's shortened Find caption to read it in full. Copy/Paste have descriptions; Unavailable explains its disabled state. Inputs and popup editors retain focus and selection while their tips are shown. Options includes check/radio examples.");
        document.AppendText("\r\n\r\nQuick Access starts with Bold, Italic and Mouse/Touch mode. Right-click to change toolbar placement or visibility, or open Customize Quick Access Toolbar to add other commands. Font and Zoom offer choice lists; Select opens its source menu, while Paste and Track changes retain their split primary/menu actions. F10 shows toolbar KeyTips; 0 opens overflow when the window is narrow.");
        document.AppendText("\r\n\r\nThe Fields group can be added through Customize Quick Access Toolbar. It borrows the original native editors, values and bindings. Options commands can also be added: Gridlines keeps its three-state binding, and layout choices retain their radio group.");
        document.AppendText("\r\n\r\nChoose Insert > Picture to show the colored Picture Format tab. Close picture tools hides it and returns to a visible tab. The application decides when picture tools apply.");
        document.AppendText("\r\n\r\nFile opens native Backstage. Info and Print retain their native fields when switching pages or reopening; Save and New run application commands. F10, F enters Backstage, F6 moves to its page, and Escape returns to this document. File remains available while the ribbon is minimized.");
        ribbon.SelectedTab = home;
        InitializeMerging();
        InitializeModal();
        InitializeDensity();
        InitializeMessages();
        InitializeSimplified();
        InitializeAnimation();
        InitializeCustomization();
        InitializeCommandIcons();
        document.AppendText("\r\n\r\nView > Chart contains the three chart context actions. Chart context adds a source-owned Chart Design tab and a Chart context group on Home. Switch the context off and on to retain its fields and bindings. Application activation drives merging; moving keyboard focus does not.");
        document.AppendText("\r\n\r\nHome > Galleries shows Styles inside the ribbon. Use its up/down buttons to browse a row, and the bottom button or F4 to expand all styles. Preview leaves selection unchanged; click or Enter applies the style. Styles and Text color can be added to Quick Access through customization.");
    }
    private void NameValidating(object? sender, CancelEventArgs e)
    {
        e.Cancel = string.IsNullOrWhiteSpace(nameInput.Value);
        if (e.Cancel) status.Text = "Enter a name, or press Escape to restore it.";
    }
    private void NameCommitted(object? sender, EventArgs e) => status.Text = $"Name committed: {_formatting.Name}";
    private void ZoomValidating(object? sender, CancelEventArgs e)
    {
        e.Cancel = !int.TryParse(zoomInput.Value.Trim().TrimEnd('%'), out int value) || value is < 25 or > 500;
        if (e.Cancel) status.Text = "Enter a zoom from 25% to 500%, or press Escape.";
    }
    private void ChoiceCommitted(object? sender, EventArgs e) => status.Text = $"Font: {_formatting.FontName}; Zoom: {_formatting.Zoom}";
    private void ResetNameClick(object? sender, EventArgs e) { _formatting.Name = "Untitled document"; status.Text = "Bound name reset by the application."; }
    private void CopyClick(object? sender, EventArgs e) => status.Text = "Copy requested";
    private void PasteClick(object? sender, EventArgs e) => status.Text = "Paste requested";
    private void ActionClick(object? sender, EventArgs e) => status.Text = (sender as RibbonItem)?.Text + " requested";
    private void ShowPictureTools(object? sender, EventArgs e) { pictureFormat.Visible = true; ribbon.SelectedTab = pictureFormat; status.Text = "Picture tools available"; }
    private void ClosePictureTools(object? sender, EventArgs e) { pictureFormat.Visible = false; status.Text = "Picture context cleared"; }
    private void BoldChanged(object? sender, EventArgs e) => status.Text = "Bold: " + bold.Checked;
    private void TrackChangesChanged(object? sender, EventArgs e) => status.Text = "Track changes: " + trackChanges.Checked;
    private void OptionClick(object? sender, EventArgs e) => status.Text = sender switch
    {
        RibbonCheckBoxItem check => check.Text + ": " + check.CheckState,
        RibbonRadioButtonItem radio => radio.Text + ": selected",
        _ => "Ready"
    };
    internal void PaintRibbonForMeasurement() => ribbon.Refresh();
    internal void CaptureSnapshots(string directory)
    {
        Directory.CreateDirectory(directory);
        foreach (int width in new[] { 1080, 720, 420, 260 })
        {
            Width = width;
            PerformLayout();
            ribbon.Refresh();
            Update();
            using var bitmap = new Bitmap(ClientSize.Width, ClientSize.Height);
            DrawToBitmap(bitmap, ClientRectangle);
            bitmap.Save(Path.Combine(directory, $"native-{width}-{DeviceDpi}dpi.png"));
        }
    }
    private sealed class FormattingState : INotifyPropertyChanged
    {
        private bool _bold;
        private string _name = "Untitled document";
        private string _fontName = "Segoe UI", _zoom = "100%";
        private bool _ruler = true;
        private CheckState _grid = CheckState.Indeterminate;
        public bool Ruler { get => _ruler; set { if (_ruler == value) return; _ruler = value; PropertyChanged?.Invoke(this, new(nameof(Ruler))); } }
        public CheckState Grid { get => _grid; set { if (_grid == value) return; _grid = value; PropertyChanged?.Invoke(this, new(nameof(Grid))); } }
        public string FontName { get => _fontName; set { if (_fontName == value) return; _fontName = value; PropertyChanged?.Invoke(this, new(nameof(FontName))); } }
        public string Zoom { get => _zoom; set { if (_zoom == value) return; _zoom = value; PropertyChanged?.Invoke(this, new(nameof(Zoom))); } }
        public string Name
        {
            get => _name;
            set { if (_name == value) return; _name = value; PropertyChanged?.Invoke(this, new(nameof(Name))); }
        }
        public bool Bold
        {
            get => _bold; set
            {
                if (_bold == value)
                    return;
                _bold = value;
                PropertyChanged?.Invoke(this, new(nameof(Bold)));
            }
        }
        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
