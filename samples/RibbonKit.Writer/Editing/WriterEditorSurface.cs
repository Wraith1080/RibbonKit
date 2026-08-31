using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using RibbonKit.Writer.Models;

namespace RibbonKit.Writer.Editing;

/// <summary>
/// App-owned host for Writer's one live <see cref="RichTextBox"/> editing surface.
/// </summary>
/// <remarks>
/// The host only changes layout around the native editor. It never replaces the editor or its
/// document, and therefore leaves selection, undo, clipboard, IME and spelling state in WPF's
/// editor. Paper mode is a centred canvas, not a paginator and does not insert page breaks.
/// </remarks>
public sealed class WriterEditorSurface : Grid
{
    private RichTextBox? _editor;
    private ScrollViewer? _viewport;
    private Border? _paperCanvas;
    private FlowDocument? _document;
    private Thickness _continuousPadding;
    private ScrollBarVisibility _continuousVerticalScrollBarVisibility;
    private Thickness _continuousPagePadding;
    private double _continuousPageWidth;
    private double _continuousPageHeight;
    private DocumentPageSettings _pageSettings = DocumentPageSettings.Letter();
    private double _zoomPercent = 100d;
    private WriterEditorViewMode _viewMode;

    /// <summary>Initializes a Writer editor surface.</summary>
    public WriterEditorSurface()
    {
        Loaded += OnLoaded;
    }

    /// <summary>Gets or sets the current presentation mode.</summary>
    public WriterEditorViewMode ViewMode
    {
        get => _viewMode;
        set
        {
            if (!Enum.IsDefined(value))
                throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown editor view mode.");
            if (_viewMode == value)
                return;
            _viewMode = value;
            ApplyLayout();
        }
    }

    /// <summary>Gets or sets the page settings used by Paper mode.</summary>
    public DocumentPageSettings PageSettings
    {
        get => _pageSettings;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (ReferenceEquals(_pageSettings, value))
                return;
            _pageSettings = value;
            ApplyLayout();
        }
    }

    /// <summary>Gets or sets the current editor zoom percentage.</summary>
    public double ZoomPercent
    {
        get => _zoomPercent;
        set
        {
            if (!double.IsFinite(value) || value <= 0)
                throw new ArgumentOutOfRangeException(nameof(value), value, "Zoom must be finite and positive.");
            if (Math.Abs(_zoomPercent - value) < 0.0001)
                return;
            _zoomPercent = value;
            ApplyLayout();
        }
    }

    /// <summary>Gets the live editor attached by the Writer window.</summary>
    public RichTextBox? Editor => _editor;

    /// <summary>Gets the paper width after applying the current zoom.</summary>
    public double PaperWidthDip => _pageSettings.WidthDip * _zoomPercent / 100d;

    /// <summary>Gets the paper height after applying the current zoom.</summary>
    public double PaperHeightDip => _pageSettings.HeightDip * _zoomPercent / 100d;

    /// <summary>
    /// Connects the XAML-owned paper canvas and native editor without replacing either. This is
    /// intentionally a small seam so the surface can be exercised independently.
    /// </summary>
    public void Attach(RichTextBox editor, ScrollViewer viewport, Border paperCanvas)
    {
        ArgumentNullException.ThrowIfNull(editor);
        ArgumentNullException.ThrowIfNull(viewport);
        ArgumentNullException.ThrowIfNull(paperCanvas);
        if (_editor is not null && !ReferenceEquals(_editor, editor))
            throw new InvalidOperationException("A Writer editor surface cannot switch native editors.");

        _editor = editor;
        _viewport = viewport;
        _paperCanvas = paperCanvas;
        _continuousPadding = editor.Padding;
        _continuousVerticalScrollBarVisibility = editor.VerticalScrollBarVisibility;
        SetDocument(editor.Document);
    }

    /// <summary>Updates the live document while retaining the native editor instance.</summary>
    public void SetDocument(FlowDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (_editor is not null && !ReferenceEquals(_editor.Document, document))
            _editor.Document = document;
        _document = document;
        _continuousPagePadding = document.PagePadding;
        _continuousPageWidth = document.PageWidth;
        _continuousPageHeight = document.PageHeight;
        ApplyLayout();
    }

    private void ApplyLayout()
    {
        if (_editor is null || _viewport is null || _paperCanvas is null || _document is null)
            return;

        var hadFocus = _editor.IsKeyboardFocusWithin;

        if (_viewMode == WriterEditorViewMode.Continuous)
        {
            _viewport.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
            _viewport.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
            _viewport.HorizontalContentAlignment = HorizontalAlignment.Stretch;
            _paperCanvas.HorizontalAlignment = HorizontalAlignment.Stretch;
            _paperCanvas.VerticalAlignment = VerticalAlignment.Stretch;
            _paperCanvas.Width = double.NaN;
            _paperCanvas.Height = double.NaN;
            _paperCanvas.MinWidth = 0;
            _paperCanvas.MinHeight = 0;
            _paperCanvas.Margin = new Thickness(0);
            _paperCanvas.Padding = new Thickness(0);
            _paperCanvas.BorderThickness = new Thickness(0);
            _paperCanvas.Background = Brushes.Transparent;
            _editor.HorizontalAlignment = HorizontalAlignment.Stretch;
            _editor.VerticalAlignment = VerticalAlignment.Stretch;
            _editor.Width = double.NaN;
            _editor.Height = double.NaN;
            _editor.MinWidth = 0;
            _editor.MinHeight = 0;
            _editor.Padding = _continuousPadding;
            _editor.VerticalScrollBarVisibility = _continuousVerticalScrollBarVisibility;
            _document.PagePadding = _continuousPagePadding;
            _document.PageWidth = _continuousPageWidth;
            _document.PageHeight = _continuousPageHeight;
            RestoreFocus(hadFocus);
            return;
        }

        _viewport.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
        _viewport.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        _viewport.HorizontalContentAlignment = HorizontalAlignment.Center;
        _paperCanvas.HorizontalAlignment = HorizontalAlignment.Center;
        _paperCanvas.VerticalAlignment = VerticalAlignment.Top;
        _paperCanvas.Width = PaperWidthDip;
        _paperCanvas.Height = double.NaN;
        _paperCanvas.MinHeight = PaperHeightDip;
        _paperCanvas.Margin = new Thickness(28, 28, 28, 28);
        _paperCanvas.Padding = new Thickness(0);
        _paperCanvas.BorderThickness = new Thickness(1);
        _paperCanvas.Background = Brushes.White;
        _editor.HorizontalAlignment = HorizontalAlignment.Left;
        _editor.VerticalAlignment = VerticalAlignment.Top;
        _editor.Width = _pageSettings.WidthDip;
        _editor.Height = double.NaN;
        _editor.MinWidth = 0;
        _editor.MinHeight = _pageSettings.HeightDip;
        _editor.Padding = new Thickness(0);
        _editor.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
        var margins = _pageSettings.Margins;
        _document.PageWidth = _pageSettings.WidthDip;
        _document.PageHeight = _pageSettings.HeightDip;
        _document.PagePadding = new Thickness(margins.LeftDip, margins.TopDip, margins.RightDip, margins.BottomDip);

        // The controller owns the editor's LayoutTransform. The host only scales its logical
        // paper dimensions so the single transform is not applied twice.
        RestoreFocus(hadFocus);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // The initial FlowDocument is assigned before the native RichTextBox has completed its
        // first load/template pass. Reassert the selected presentation after that pass so a late
        // native initialization cannot leave Paper mode with zero PagePadding until New is used.
        ApplyLayout();
    }

    private void RestoreFocus(bool hadFocus)
    {
        if (hadFocus)
            _editor?.Focus();
    }
}
