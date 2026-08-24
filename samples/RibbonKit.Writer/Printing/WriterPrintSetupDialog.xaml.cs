using System.Printing;
using System.Windows;
using System.Windows.Threading;
using RibbonKit.Writer.Page;
using RibbonKit.Writer.Preview;

namespace RibbonKit.Writer.Printing;

/// <summary>A Writer-owned printer picker with a real preview of the exact fixed paginator.</summary>
public partial class WriterPrintSetupDialog : Window
{
    private readonly WriterPreviewSnapshot _snapshot;

    /// <summary>Creates print setup around a current preview snapshot and available queues.</summary>
    public WriterPrintSetupDialog(WriterPreviewSnapshot snapshot,
        IReadOnlyList<WriterPrinterChoice> printers, string? defaultPrinterName)
    {
        _snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        ArgumentNullException.ThrowIfNull(printers);
        InitializeComponent();
        PrinterBox.ItemsSource = printers;
        PrinterBox.SelectedItem = printers.FirstOrDefault(printer =>
            string.Equals(printer.Queue?.FullName, defaultPrinterName,
                StringComparison.OrdinalIgnoreCase)) ?? printers.FirstOrDefault();
        PrintButton.IsEnabled = PrinterBox.SelectedItem is WriterPrinterChoice { Queue: not null };
        PageSummaryText.Text = WriterPageUi.FormatSummary(snapshot.PageSettings,
            snapshot.Paginator.PageCount, snapshot.SourceClone.Background);
        Preview.StateChanged += OnPreviewStateChanged;
        Preview.SetSnapshot(snapshot);
        UpdatePreviewState();
        Loaded += OnLoaded;
        SizeChanged += OnSizeChanged;
        Closed += OnClosed;
    }

    /// <summary>Gets the printer selected after the user accepts the dialog.</summary>
    public WriterPrinterChoice? SelectedPrinter { get; private set; }

    private void OnPrint(object sender, RoutedEventArgs e)
    {
        if (PrinterBox.SelectedItem is not WriterPrinterChoice printer || printer.Queue is null)
            return;
        SelectedPrinter = printer;
        DialogResult = true;
    }

    private void OnPrevious(object sender, RoutedEventArgs e) => Preview.GoToPreviousPage();

    private void OnNext(object sender, RoutedEventArgs e) => Preview.GoToNextPage();

    private void OnPreviewStateChanged(object? sender, EventArgs e) => UpdatePreviewState();

    private void OnLoaded(object sender, RoutedEventArgs e) => QueueFitPreview();

    private void OnSizeChanged(object sender, SizeChangedEventArgs e) => QueueFitPreview();

    private void QueueFitPreview() => _ = Dispatcher.BeginInvoke(DispatcherPriority.Loaded,
        new Action(FitPreview));

    private void FitPreview()
    {
        var viewportWidth = Preview.Viewer.ViewportWidth > 0
            ? Preview.Viewer.ViewportWidth
            : Preview.Viewer.ActualWidth;
        var viewportHeight = Preview.Viewer.ViewportHeight > 0
            ? Preview.Viewer.ViewportHeight
            : Preview.Viewer.ActualHeight;
        if (viewportWidth <= 0 || viewportHeight <= 0)
            return;
        const double chromeAllowance = 56;
        var widthZoom = Math.Max(1, viewportWidth - chromeAllowance) /
            _snapshot.PageSize.Width * 100;
        var heightZoom = Math.Max(1, viewportHeight - chromeAllowance) /
            _snapshot.PageSize.Height * 100;
        Preview.Zoom = Math.Clamp(Math.Min(widthZoom, heightZoom),
            Preview.MinZoom, Preview.MaxZoom);
    }

    private void UpdatePreviewState()
    {
        PreviousButton.IsEnabled = Preview.CanGoToPreviousPage;
        NextButton.IsEnabled = Preview.CanGoToNextPage;
        PagePositionText.Text = Preview.PageCount > 0
            ? $"Page {Preview.CurrentPageNumber:N0} of {Preview.PageCount:N0}"
            : "No pages";
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        Closed -= OnClosed;
        Loaded -= OnLoaded;
        SizeChanged -= OnSizeChanged;
        Preview.StateChanged -= OnPreviewStateChanged;
        Preview.SetSnapshot(null);
    }
}

/// <summary>One printer displayed by Writer print setup.</summary>
public sealed record WriterPrinterChoice(PrintQueue? Queue, string DisplayName)
{
    /// <summary>Creates a display choice for a real print queue.</summary>
    public WriterPrinterChoice(PrintQueue queue)
        : this(queue ?? throw new ArgumentNullException(nameof(queue)), queue.FullName)
    {
    }
}
