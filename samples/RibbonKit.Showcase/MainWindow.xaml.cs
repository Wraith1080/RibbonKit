using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using RibbonKit.Controls;
using RibbonKit.Theming;

namespace RibbonKit.Showcase;

/// <summary>
/// Main window of the showcase app, hosting the RibbonKit ribbon and demonstrating
/// gallery live preview: hovering a style previews it on the sample text, clicking
/// commits it, and leaving the gallery reverts to the committed style.
/// </summary>
public partial class MainWindow : RibbonWindow
{
    private string _committedStyle = "Normal";

    public MainWindow()
    {
        InitializeComponent();
    }

    private void OnApplyOffice2024(object sender, RoutedEventArgs e) =>
        ThemeManager.Apply(Application.Current, RibbonTheme.Office2024);

    private void OnApplyOffice2019(object sender, RoutedEventArgs e) =>
        ThemeManager.Apply(Application.Current, RibbonTheme.Office2019);

    private void OnApplyOffice2013(object sender, RoutedEventArgs e) =>
        ThemeManager.Apply(Application.Current, RibbonTheme.Office2013);

    private void OnPictureSelected(object sender, RoutedEventArgs e)
    {
        if (PictureFormatTab is not null)
        {
            PictureFormatTab.Visibility = Visibility.Visible;
        }
    }

    private void OnPictureDeselected(object sender, RoutedEventArgs e)
    {
        if (PictureFormatTab is not null)
        {
            PictureFormatTab.Visibility = Visibility.Collapsed;
        }
    }

    private void OnStylePreview(object sender, RibbonGalleryPreviewEventArgs e)
    {
        if (GetStyleTag(e.PreviewedItem) is { } tag)
        {
            ApplyStyleToSample(tag);
        }
    }

    private void OnStylePreviewCancelled(object sender, RoutedEventArgs e)
    {
        ApplyStyleToSample(_committedStyle);
    }

    private void OnStyleCommitted(object sender, SelectionChangedEventArgs e)
    {
        // SelectionChanged fires during InitializeComponent (SelectedIndex="0" in
        // XAML) — before later-declared elements exist. Guard against that.
        if (StylesGallery is null)
        {
            return;
        }

        if (GetStyleTag(StylesGallery.SelectedItem) is { } tag)
        {
            _committedStyle = tag;
            ApplyStyleToSample(tag);
        }
    }

    private static string? GetStyleTag(object? item) =>
        (item as FrameworkElement)?.Tag as string;

    private void ApplyStyleToSample(string style)
    {
        // Events can fire during XAML parse, before this element is constructed.
        if (StylePreviewText is null)
        {
            return;
        }

        // Reset to the Normal baseline, then apply the style's deltas.
        StylePreviewText.FontSize = 14;
        StylePreviewText.FontWeight = FontWeights.Normal;
        StylePreviewText.FontStyle = FontStyles.Normal;
        StylePreviewText.Foreground = new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x1A));

        switch (style)
        {
            case "Heading 1":
                StylePreviewText.FontSize = 20;
                StylePreviewText.FontWeight = FontWeights.SemiBold;
                StylePreviewText.Foreground = new SolidColorBrush(Color.FromRgb(0x0F, 0x6C, 0xBD));
                break;
            case "Heading 2":
                StylePreviewText.FontSize = 17;
                StylePreviewText.FontWeight = FontWeights.SemiBold;
                StylePreviewText.Foreground = new SolidColorBrush(Color.FromRgb(0x2B, 0x7F, 0xC7));
                break;
            case "Title":
                StylePreviewText.FontSize = 26;
                StylePreviewText.FontWeight = FontWeights.Light;
                break;
            case "Subtitle":
                StylePreviewText.FontSize = 15;
                StylePreviewText.Foreground = new SolidColorBrush(Color.FromRgb(0x61, 0x61, 0x61));
                break;
            case "Quote":
                StylePreviewText.FontStyle = FontStyles.Italic;
                StylePreviewText.Foreground = new SolidColorBrush(Color.FromRgb(0x61, 0x61, 0x61));
                break;
            case "Strong":
                StylePreviewText.FontWeight = FontWeights.Bold;
                break;
            case "Emphasis":
                StylePreviewText.FontStyle = FontStyles.Italic;
                break;
        }
    }
}
