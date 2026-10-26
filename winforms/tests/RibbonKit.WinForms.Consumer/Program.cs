using System.ComponentModel;
using RibbonKit.WinForms;

namespace IndependentConsumer;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
        using var form = new ConsumerForm();
        Application.Run(form);
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
