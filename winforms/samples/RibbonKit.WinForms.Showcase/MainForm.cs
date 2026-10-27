using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace RibbonKit.WinForms.Showcase;

public partial class MainForm : Form
{
    private readonly FormattingState _formatting = new();
    public MainForm()
    {
        InitializeComponent();
        formattingSource.DataSource = _formatting;
        bold.DataBindings.Add(new Binding(nameof(RibbonToggleItem.Checked), formattingSource, nameof(FormattingState.Bold), true, DataSourceUpdateMode.OnPropertyChanged));
        nameInput.DataBindings.Add(new Binding(nameof(RibbonTextBoxItem.Value), formattingSource, nameof(FormattingState.Name), true, DataSourceUpdateMode.OnPropertyChanged));
        fontInput.DataBindings.Add(new Binding(nameof(RibbonComboBoxItem.Value), formattingSource, nameof(FormattingState.FontName), true, DataSourceUpdateMode.OnPropertyChanged));
        zoomInput.DataBindings.Add(new Binding(nameof(RibbonComboBoxItem.Value), formattingSource, nameof(FormattingState.Zoom), true, DataSourceUpdateMode.OnPropertyChanged));
        showRuler.DataBindings.Add(new Binding(nameof(RibbonCheckBoxItem.Checked), formattingSource, nameof(FormattingState.Ruler), true, DataSourceUpdateMode.OnPropertyChanged));
        popupRuler.DataBindings.Add(new Binding(nameof(RibbonCheckBoxItem.Checked), formattingSource, nameof(FormattingState.Ruler), true, DataSourceUpdateMode.OnPropertyChanged));
        showGrid.DataBindings.Add(new Binding(nameof(RibbonCheckBoxItem.CheckState), formattingSource, nameof(FormattingState.Grid), false, DataSourceUpdateMode.OnPropertyChanged));
        copy.Icon = new SampleIcon(0);
        paste.Icon = new SampleIcon(1);
        cut.Icon = new SampleIcon(2);
        bold.Icon = new SampleIcon(3);
        italic.Icon = new SampleIcon(4);
        find.Icon = new SampleIcon(5);
        shapes.Icon = new SampleIcon(6);
        picture.Icon = new SampleIcon(7);
        link.Icon = new SampleIcon(8);
        trackChanges.Icon = new SampleIcon(9);
        document.Text = "Native RibbonKit.WinForms\r\n\r\nInputs demonstrates native TextBox and ComboBox editing, binding, read-only/disabled fields and popup ownership. Font selects a choice; Zoom also accepts typed percentages (25% to 500%). Resize to open the same editors in collapsed groups.\r\n\r\nF4 or Alt+Down opens the choice popup. Escape closes that list first, then its containing ribbon popup. Enter validates and commits; Escape outside a list restores the last committed value. Tab visits fields; F6 returns to this document. Alt/F10, E, F focuses Font.\r\n\r\nThe document, status text, validation rules and data belong to this application. Home still demonstrates split buttons.";
        document.AppendText("\r\n\r\nOptions demonstrates compact check/radio choices. Gridlines cycles through three states. Ruler and its popup entry share an application binding. Resize to open the same options in collapsed groups.");
        document.AppendText("\r\n\r\nScreenTips: pause over Home's shortened Find caption to read it in full. Copy/Paste have descriptions; Unavailable explains its disabled state. Inputs and popup editors retain focus and selection while their tips are shown. Options includes check/radio examples.");
        ribbon.SelectedTab = home;
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
    private sealed class SampleIcon(int kind) : IRibbonIcon
    {
        public void Draw(Graphics graphics, Rectangle bounds, RibbonIconContext context)
        {
            var saved = graphics.Save();
            try
            {
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var pen = new Pen(context.Enabled ? Color.FromArgb(43, 87, 154) : SystemColors.GrayText, Math.Max(1, bounds.Width / 14f));
                var r = Rectangle.Inflate(bounds, -Math.Max(2, bounds.Width / 6), -Math.Max(2, bounds.Height / 6));
                if (kind is 3 or 4)
                    TextRenderer.DrawText(graphics, kind == 3 ? "B" : "I", SystemFonts.DefaultFont, bounds, context.Foreground, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                else if (kind == 9)
                {
                    graphics.DrawLines(pen, new Point[] { new(r.Left, r.Bottom), new(r.Left + r.Width / 5, r.Bottom - r.Height / 2), new(r.Right - r.Width / 5, r.Top), new(r.Right, r.Top + r.Height / 5), new(r.Left + r.Width / 2, r.Bottom - r.Height / 5), new(r.Left, r.Bottom) });
                    graphics.DrawLine(pen, r.Left + r.Width / 5, r.Bottom - r.Height / 2, r.Left + r.Width / 2, r.Bottom - r.Height / 5);
                }
                else if (kind is 5 or 8)
                {
                    graphics.DrawEllipse(pen, r);
                    graphics.DrawLine(pen, r.Right - 2, r.Bottom - 2, bounds.Right - 1, bounds.Bottom - 1);
                }
                else if (kind == 6)
                {
                    graphics.DrawEllipse(pen, r.X, r.Y, r.Width / 2, r.Height / 2);
                    graphics.DrawRectangle(pen, r.X + r.Width / 3, r.Y + r.Height / 3, r.Width * 2 / 3, r.Height * 2 / 3);
                }
                else
                {
                    graphics.DrawRectangle(pen, r);
                    graphics.DrawLine(pen, r.Left + 2, r.Top + r.Height / 3, r.Right - 2, r.Top + r.Height / 3);
                    graphics.DrawLine(pen, r.Left + 2, r.Top + r.Height * 2 / 3, r.Right - 2, r.Top + r.Height * 2 / 3);
                }
            }
            finally { graphics.Restore(saved); }
        }
    }
}
