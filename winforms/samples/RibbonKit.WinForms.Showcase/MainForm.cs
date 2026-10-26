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
        copy.Icon = new SampleIcon(0);
        paste.Icon = new SampleIcon(1);
        cut.Icon = new SampleIcon(2);
        bold.Icon = new SampleIcon(3);
        italic.Icon = new SampleIcon(4);
        find.Icon = new SampleIcon(5);
        shapes.Icon = new SampleIcon(6);
        picture.Icon = new SampleIcon(7);
        link.Icon = new SampleIcon(8);
        document.Text = "Native RibbonKit.WinForms foundation\r\n\r\nResize the window to exercise adaptive groups and collapsed flyouts.\r\n\r\nUse Alt or F10 for KeyTips. F6 moves between the ribbon and this editor.\r\n\r\nThe editor, status text and icons belong to this application.";
    }
    private void CopyClick(object? sender, EventArgs e) => status.Text = "Copy requested";
    private void PasteClick(object? sender, EventArgs e) => status.Text = "Paste requested";
    private void ActionClick(object? sender, EventArgs e) => status.Text = (sender as RibbonItem)?.Text + " requested";
    private void BoldChanged(object? sender, EventArgs e) => status.Text = "Bold: " + bold.Checked;
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
