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

            // The backdrop only composites where the DWM glass reaches: without extending
            // the glass across the client area, a transparent window renders BLACK.
            MicaHelper.ExtendGlassFrame(this, full: true);

            // Strip WS_SYSMENU so the DWM's native min/max/close buttons don't show through a
            // transparent title bar and overlap our custom caption buttons.
            MicaHelper.ShowNativeCaptionButtons(this, false);

            _opaqueWindowBackground ??= Background;
            Background = Brushes.Transparent;
            MainContentArea.Background = Brushes.Transparent;

            // Let the title bar go transparent so Mica shows through it — but only for the 2024
            // look with a non-colored title bar (ThemeManager enforces that rule and re-derives
            // it across theme/accent changes, so switching themes no longer reverts it).
            ThemeManager.SetTitleBarBackdrop(Application.Current, true);

            // Backstage stays opaque (Translucent left false): it fully covers the content
            // behind it, so Mica shows in the title bar / ribbon chrome but does not bleed
            // through the backstage page.
        }
        else
        {
            MicaHelper.TrySetBackdrop(this, RibbonBackdrop.None);

            // Do NOT collapse the glass frame here. The RibbonWindow template keeps
            // GlassFrameThickness="-1" as its resting state, and that extended glass is what
            // gives the window its Windows 11 border and rounded corners (WindowChrome strips
            // the native non-client frame, so the DWM glass is all that renders them).
            // Collapsing it to 0 destroys the border/corners — and there's no black-background
            // risk to avoid, because the visible content is opaque white again below.
            MicaHelper.ShowNativeCaptionButtons(this, true);
            ThemeManager.SetTitleBarBackdrop(Application.Current, false);
            Background = _opaqueWindowBackground ?? Brushes.White;
            MainContentArea.Background = Brushes.White;
        }
    }

    private void OnOpenOptions(object sender, RoutedEventArgs e) =>
        OpenOptionsDialog(selectQuickAccessPage: false);

    // Raised by the ribbon's right-click "Customize Quick Access Toolbar…" items.
    private void OnCustomizeQuickAccess(object sender, EventArgs e) =>
        OpenOptionsDialog(selectQuickAccessPage: true);

    /// <summary>
    /// The merged options dialog: an app-provided page ("Editor") next to RibbonKit's
    /// built-in Quick Access Toolbar customization page — one dialog, like Word Options.
    /// </summary>
    private void OpenOptionsDialog(bool selectQuickAccessPage)
    {
        var editorPage = new RibbonOptionsPage { Header = "Editor", Content = BuildEditorOptionsContent() };
        var qatPage = new RibbonOptionsPage
        {
            Header = "Quick Access Toolbar",
            Content = new RibbonQuickAccessPage { Ribbon = MainRibbon },
        };

        var dialog = new RibbonOptionsDialog { Title = "Options", Owner = this };
        dialog.Pages.Add(editorPage);
        dialog.Pages.Add(qatPage);
        dialog.SelectedPage = selectQuickAccessPage ? qatPage : editorPage;

        // The dialog raises Applied on OK — the app's cue to persist its settings (the QAT
        // page edits the ribbon live). ShowDialog's bool result carries the same decision.
        dialog.Applied += (_, _) => StatusReady.Content = "Options applied";
        dialog.ShowDialog();
    }

    /// <summary>Stand-in for an application options page — any UserControl works here.</summary>
    private static object BuildEditorOptionsContent()
    {
        var panel = new StackPanel { MaxWidth = 460, HorizontalAlignment = HorizontalAlignment.Left };
        panel.Children.Add(new TextBlock
        {
            Text = "An app-provided page living in the same dialog as RibbonKit's customization pages — add any UserControl as a RibbonOptionsPage's content.",
            TextWrapping = TextWrapping.Wrap,
            Foreground = new SolidColorBrush(Color.FromRgb(0x61, 0x61, 0x61)),
            Margin = new Thickness(0, 0, 0, 14),
        });
        panel.Children.Add(new CheckBox { Content = "Check spelling as you type", IsChecked = true, Margin = new Thickness(0, 0, 0, 6) });
        panel.Children.Add(new CheckBox { Content = "Autosave every 10 minutes", IsChecked = true, Margin = new Thickness(0, 0, 0, 6) });
        panel.Children.Add(new CheckBox { Content = "Show formatting marks" });
        return panel;
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
