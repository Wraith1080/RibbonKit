using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace RibbonKit.Writer.Preview;

/// <summary>Supported page arrangements for the Writer preview viewer.</summary>
public enum WriterPreviewViewMode
{
    /// <summary>Shows one page across the viewer.</summary>
    OnePage = 0,

    /// <summary>Shows at most two pages across the viewer.</summary>
    TwoPages = 1,

    /// <summary>Fits the page width to the viewer.</summary>
    PageWidth = 2
}

/// <summary>Displays stable fixed pages from a <see cref="WriterPreviewSnapshot"/>.</summary>
/// <remarks>
/// The page views and print service consume the snapshot's same eagerly materialized paginator.
/// This avoids reformatting the mutable flow paginator after a viewer has already requested pages.
/// </remarks>
public sealed class WriterDocumentPreviewView : Grid
{
    private const double HorizontalChromeAllowance = 48;
    private const double TwoPageGap = 16;
    private const double MinimumZoom = 10;
    private const double MaximumZoom = 500;
    private readonly StackPanel _pagePanel;
    private readonly Border _primaryPageHost;
    private readonly Border _secondaryPageHost;
    private WriterPreviewSnapshot? _snapshot;
    private WriterPreviewViewMode _viewMode;
    private int _currentPageNumber;
    private double _zoom = 100;

    /// <summary>Creates a preview view with one-page mode selected.</summary>
    public WriterDocumentPreviewView()
    {
        PrimaryPageView = CreatePageView();
        SecondaryPageView = CreatePageView();
        _primaryPageHost = CreatePageHost(PrimaryPageView);
        _secondaryPageHost = CreatePageHost(SecondaryPageView);
        _secondaryPageHost.Margin = new Thickness(TwoPageGap, 0, 0, 0);
        _pagePanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(HorizontalChromeAllowance / 2)
        };
        _pagePanel.Children.Add(_primaryPageHost);
        _pagePanel.Children.Add(_secondaryPageHost);
        Viewer = new ScrollViewer
        {
            Content = _pagePanel,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Background = new SolidColorBrush(Color.FromRgb(224, 226, 229)),
            CanContentScroll = false
        };
        Viewer.SizeChanged += OnViewerSizeChanged;
        Viewer.ScrollChanged += OnViewerScrollChanged;
        Children.Add(Viewer);
        UpdatePages();
    }

    /// <summary>Gets the scrolling page viewport.</summary>
    public ScrollViewer Viewer { get; }

    /// <summary>Gets the page view for the current master page.</summary>
    public DocumentPageView PrimaryPageView { get; }

    /// <summary>Gets the optional following page used by two-page mode.</summary>
    public DocumentPageView SecondaryPageView { get; }

    /// <summary>Gets or sets the page arrangement.</summary>
    public WriterPreviewViewMode ViewMode
    {
        get => _viewMode;
        set
        {
            if (!Enum.IsDefined(value))
                throw new ArgumentOutOfRangeException(nameof(value), value,
                    "Unknown preview view mode.");
            if (_viewMode == value)
                return;
            _viewMode = value;
            UpdatePages();
            StateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>Gets or sets the page scale as a percentage.</summary>
    public double Zoom
    {
        get => _zoom;
        set
        {
            if (!double.IsFinite(value) || value < MinimumZoom || value > MaximumZoom)
                throw new ArgumentOutOfRangeException(nameof(value), value,
                    $"Preview zoom must be between {MinimumZoom} and {MaximumZoom} percent.");
            if (Math.Abs(_zoom - value) < 0.01)
                return;
            _zoom = value;
            ApplyPageSizes();
            StateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>Gets the minimum supported zoom percentage.</summary>
    public double MinZoom => MinimumZoom;

    /// <summary>Gets the maximum supported zoom percentage.</summary>
    public double MaxZoom => MaximumZoom;

    /// <summary>Gets the currently displayed snapshot, or <see langword="null"/>.</summary>
    public WriterPreviewSnapshot? Snapshot => _snapshot;

    /// <summary>Gets the one-based current master page number, or zero when no snapshot is loaded.</summary>
    public int CurrentPageNumber => _currentPageNumber;

    /// <summary>Gets the current paginator's page count.</summary>
    public int PageCount => _snapshot?.Paginator.PageCount ?? 0;

    /// <summary>Gets whether the viewer can navigate to a previous page.</summary>
    public bool CanGoToPreviousPage => _currentPageNumber > 1;

    /// <summary>Gets whether the viewer can navigate to a next page.</summary>
    public bool CanGoToNextPage => _currentPageNumber > 0 && _currentPageNumber < PageCount;

    /// <summary>Raised when the snapshot, page position, zoom, or view mode changes.</summary>
    public event EventHandler? StateChanged;

    /// <summary>Replaces the preview snapshot and resets navigation to its first page.</summary>
    public void SetSnapshot(WriterPreviewSnapshot? snapshot)
    {
        _snapshot = snapshot;
        _currentPageNumber = snapshot is null ? 0 : 1;
        UpdatePages();
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Moves to a specific one-based page number.</summary>
    public void GoToPage(int pageNumber)
    {
        if (pageNumber < 1 || pageNumber > PageCount)
            throw new ArgumentOutOfRangeException(nameof(pageNumber), pageNumber,
                "The requested one-based page is outside the current preview.");
        if (_currentPageNumber == pageNumber)
            return;
        _currentPageNumber = pageNumber;
        UpdatePages();
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Moves to the previous page when one is available.</summary>
    public bool GoToPreviousPage()
    {
        if (!CanGoToPreviousPage)
            return false;
        _currentPageNumber--;
        UpdatePages();
        StateChanged?.Invoke(this, EventArgs.Empty);
        return true;
    }

    /// <summary>Moves to the next page when one is available.</summary>
    public bool GoToNextPage()
    {
        if (!CanGoToNextPage)
            return false;
        _currentPageNumber++;
        UpdatePages();
        StateChanged?.Invoke(this, EventArgs.Empty);
        return true;
    }

    private static DocumentPageView CreatePageView() => new()
    {
        Stretch = Stretch.Fill,
        StretchDirection = StretchDirection.Both
    };

    private static Border CreatePageHost(DocumentPageView pageView) => new()
    {
        Child = pageView,
        Background = Brushes.White,
        BorderBrush = new SolidColorBrush(Color.FromRgb(196, 198, 202)),
        BorderThickness = new Thickness(1),
        SnapsToDevicePixels = true
    };

    private void OnViewerSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (_viewMode is WriterPreviewViewMode.PageWidth or WriterPreviewViewMode.TwoPages)
            UpdateAutomaticZoom();
    }

    private void OnViewerScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (Math.Abs(e.ViewportWidthChange) > 0.01 &&
            _viewMode is WriterPreviewViewMode.PageWidth or WriterPreviewViewMode.TwoPages)
            UpdateAutomaticZoom();
    }

    private void UpdatePages()
    {
        var paginator = _snapshot?.Paginator;
        PrimaryPageView.DocumentPaginator = paginator;
        SecondaryPageView.DocumentPaginator = paginator;
        PrimaryPageView.PageNumber = paginator is null ? -1 : _currentPageNumber - 1;

        var showSecondPage = paginator is not null &&
            _viewMode == WriterPreviewViewMode.TwoPages && _currentPageNumber < PageCount;
        SecondaryPageView.PageNumber = showSecondPage ? _currentPageNumber : -1;
        _secondaryPageHost.Visibility = showSecondPage ? Visibility.Visible : Visibility.Collapsed;

        if (_viewMode is WriterPreviewViewMode.PageWidth or WriterPreviewViewMode.TwoPages)
            UpdateAutomaticZoom();
        else
            ApplyPageSizes();
    }

    private void UpdateAutomaticZoom()
    {
        if (_snapshot is null)
            return;
        var viewportWidth = Viewer.ViewportWidth > 0 ? Viewer.ViewportWidth : Viewer.ActualWidth;
        var viewportHeight = Viewer.ViewportHeight > 0 ? Viewer.ViewportHeight : Viewer.ActualHeight;
        if (viewportWidth <= 0 || viewportHeight <= 0)
            return;

        var widthForPages = Math.Max(1, viewportWidth - HorizontalChromeAllowance);
        double zoom;
        if (_viewMode == WriterPreviewViewMode.TwoPages)
        {
            widthForPages = Math.Max(1, (widthForPages - TwoPageGap) / 2);
            var widthZoom = widthForPages / _snapshot.PageSize.Width * 100;
            var heightZoom = Math.Max(1, viewportHeight - HorizontalChromeAllowance) /
                _snapshot.PageSize.Height * 100;
            zoom = Math.Min(widthZoom, heightZoom);
        }
        else
        {
            zoom = widthForPages / _snapshot.PageSize.Width * 100;
        }

        _zoom = Math.Clamp(zoom, MinimumZoom, MaximumZoom);
        ApplyPageSizes();
    }

    private void ApplyPageSizes()
    {
        if (_snapshot is null)
        {
            _primaryPageHost.Width = 0;
            _primaryPageHost.Height = 0;
            _secondaryPageHost.Width = 0;
            _secondaryPageHost.Height = 0;
            return;
        }

        var scale = _zoom / 100;
        var width = _snapshot.PageSize.Width * scale;
        var height = _snapshot.PageSize.Height * scale;
        _primaryPageHost.Width = width;
        _primaryPageHost.Height = height;
        _secondaryPageHost.Width = width;
        _secondaryPageHost.Height = height;
    }
}
