using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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

        // The caption icon. Wrapped in an Image because MdiChild.Icon takes an element; the
        // container unwraps it to the ImageSource when it merges the caption into the ribbon
        // (one UIElement cannot have two visual parents).
        if (TryFindResource("Icon.Copy") is ImageSource icon)
        {
            document.Icon = new Image { Source = icon };
        }

        // Per-document ribbon content — the MDI case tab merging was designed for. It joins the
        // ribbon when this document is activated and leaves when another takes over; the container
        // does that wiring because Ribbon is set in XAML.
        document.MergeSource = CreateDocumentTools(n, editor);
    }

    // Built in code because each document needs its own instance. In a real app the same shape is
    // far more natural in XAML — a <rk:RibbonMergeSource> inside the document's UserControl, with
    // MdiDocument.MergeSource (or the attached RibbonMergeSource.Source) pointing at it.
    private RibbonMergeSource CreateDocumentTools(int number, TextBox editor)
    {
        var upper = new RibbonButton
        {
            Header = "UPPERCASE",
            Size = RibbonControlSize.Large,
            Icon = TryFindResource("Icon.Font") as ImageSource,
            LargeIcon = TryFindResource("Icon.Font") as ImageSource,
            ScreenTipTitle = "Uppercase",
            ScreenTipText = "Shout the whole document. Proof that a merged tab's commands act on "
                            + "the document that contributed them, not on whichever is active.",
        };
        upper.Click += (_, _) => editor.Text = editor.Text.ToUpperInvariant();

        var group = new RibbonGroup { Header = "Text", Icon = TryFindResource("Icon.Font") as ImageSource };
        group.Items.Add(upper);

        var tab = new RibbonTab
        {
            Header = $"Doc {number} Tools",
            IsContextual = true,
            ContextualColor = new SolidColorBrush(Color.FromRgb(0x1F, 0x7A, 0x4D)),
        };
        tab.Groups.Add(group);

        var source = new RibbonMergeSource();
        source.Tabs.Add(tab);
        return source;
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
