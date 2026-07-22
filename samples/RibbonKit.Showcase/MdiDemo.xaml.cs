using System.Windows;
using System.Windows.Controls;
using RibbonKit.Controls;

namespace RibbonKit.Showcase;

/// <summary>
/// MDI emulation demo: injects plain UserControl-style content into an
/// <see cref="MdiContainer"/> via the imperative API. Drag captions to move,
/// grab edges to resize, double-click a caption to maximize, and use the
/// caption buttons for minimize/maximize/close.
/// </summary>
public partial class MdiDemo : RibbonWindow
{
    private int _documentNumber;

    public MdiDemo()
    {
        InitializeComponent();

        // A couple of starter documents so the window opens alive.
        OnNewDocument(this, new RoutedEventArgs());
        OnNewDocument(this, new RoutedEventArgs());
    }

    private void OnNewDocument(object sender, RoutedEventArgs e)
    {
        int n = ++_documentNumber;

        // Stand-in for a real UserControl: any FrameworkElement injects the same way.
        var editor = new TextBox
        {
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(8),
            Text = $"Document {n}\n\nType here — the dirty marker in the caption "
                   + "lights up on the first edit.",
        };

        MdiDocument document = Mdi.AddDocument(editor, $"Document {n}");
        editor.TextChanged += (_, _) => document.IsModified = true;
    }

    private void OnCloseActive(object sender, RoutedEventArgs e)
    {
        if (Mdi.ActiveDocument is { } active)
        {
            Mdi.CloseDocument(active);
        }
    }

    private void OnMaximizeActive(object sender, RoutedEventArgs e) => SetActiveState(WindowState.Maximized);

    private void OnMinimizeActive(object sender, RoutedEventArgs e) => SetActiveState(WindowState.Minimized);

    private void OnRestoreActive(object sender, RoutedEventArgs e) => SetActiveState(WindowState.Normal);

    private void SetActiveState(WindowState state)
    {
        if (Mdi.ActiveDocument is MdiDocument document)
        {
            document.WindowState = state;
        }
    }

    private void OnDocumentClosing(object? sender, MdiDocumentClosingEventArgs e)
    {
        // Unsaved-changes prompt, the classic MDI way.
        if (e.Document is MdiDocument { IsModified: true, Title: var title }
            && MessageBox.Show(
                this,
                $"“{title}” has unsaved changes. Close anyway?",
                "RibbonKit MDI Demo",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning) == MessageBoxResult.No)
        {
            e.Cancel = true;
        }
    }
}
