using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using RibbonKit.Animation;
using RibbonKit.Controls;
using RibbonKit.Interop;
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

    private void OnToggleAccentTitleBar(object sender, RoutedEventArgs e) =>
        ThemeManager.SetAccentedTitleBar(Application.Current, (sender as RibbonToggleButton)?.IsChecked == true);

    private void OnAccentGalleryChanged(object sender, SelectionChangedEventArgs e)
    {
        if (AccentGallery is null || GetStyleTag(AccentGallery.SelectedItem) is not { } tag)
        {
            return;
        }

        if (tag == "reset")
        {
            ThemeManager.ClearAccent(Application.Current);
        }
        else if (ColorConverter.ConvertFromString(tag) is Color color)
        {
            ThemeManager.SetAccent(Application.Current, color);
        }
    }

    private void OnAnimationOff(object sender, RoutedEventArgs e) =>
        RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;

    private void OnAnimationSubtle(object sender, RoutedEventArgs e) =>
        RibbonAnimation.GlobalLevel = RibbonAnimationLevel.Subtle;

    private void OnAnimationExpressive(object sender, RoutedEventArgs e) =>
        RibbonAnimation.GlobalLevel = RibbonAnimationLevel.Expressive;

    private void OnToggleRespectSystemMotion(object sender, RoutedEventArgs e) =>
        RibbonAnimation.RespectSystemReduceMotion = (sender as RibbonToggleButton)?.IsChecked == true;

    private void OnToggleBackstageDesign(object sender, RoutedEventArgs e)
    {
        if (ShowcaseBackstage is not null)
        {
            ShowcaseBackstage.Design = (sender as RibbonToggleButton)?.IsChecked == true
                ? RibbonBackstageDesign.Modern
                : RibbonBackstageDesign.Classic;
        }
    }

    private Brush? _opaqueWindowBackground;

    private void OnToggleMica(object sender, RoutedEventArgs e)
    {
        var toggle = sender as RibbonToggleButton;

        if (toggle?.IsChecked == true)
        {
            // Mica needs Windows 11 22H2+. If the DWM rejects it, undo the toggle and stop.
            if (!MicaHelper.TrySetBackdrop(this, RibbonBackdrop.Mica))
            {
                toggle.IsChecked = false;
                return;
            }

            _opaqueWindowBackground ??= Background;
            Background = Brushes.Transparent;
            MainContentArea.Background = Brushes.Transparent;
            ShowcaseBackstage.Translucent = false;
        }
        else
        {
            MicaHelper.TrySetBackdrop(this, RibbonBackdrop.None);
            Background = _opaqueWindowBackground ?? Brushes.White;
            MainContentArea.Background = Brushes.White;
            ShowcaseBackstage.Translucent = false;
        }
    }

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
