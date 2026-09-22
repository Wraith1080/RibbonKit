using System.Windows;
using System.Windows.Media;
using RibbonKit.Controls;

namespace RibbonKit.Showcase;

/// <summary>Isolated, consumer-scoped exploration of a glass-inspired Office 2024 palette.</summary>
public partial class CrystalPreviewWindow : RibbonWindow
{
    private ResourceDictionary? _crystal;
    private readonly CrystalBackstagePresentation _backstagePresentation;
    private readonly CrystalScreenTipPalette _screenTipPalette;
    private readonly string _baselineLayout;

    public CrystalPreviewWindow()
    {
        InitializeComponent();
        // Group content is reparented by ribbon layout, so use the actual source
        // rather than resolving a window name from the borrowed content tree.
        foreach (var panel in new[] { CrystalOptionsPanel, CrystalSpacingPanel })
            panel.SetBinding(IsEnabledProperty, new System.Windows.Data.Binding(nameof(EnableOptionsToggle.IsChecked))
            { Source = EnableOptionsToggle, Mode = System.Windows.Data.BindingMode.OneWay });
        _crystal = Resources.MergedDictionaries[1];
        // Backstage is reparented into an adorner. Give it explicit palette and data
        // sources instead of depending on the window's visual tree or namescope.
        _backstagePresentation = new CrystalBackstagePresentation(CrystalBackstage,
            Resources.MergedDictionaries[0], BackstageDocumentTitle, TitleInput);
        _screenTipPalette = new CrystalScreenTipPalette(Resources.MergedDictionaries[0]);
        foreach (var control in new FrameworkElement[] { CompareToggle, ArrangeButton, AccentSelector,
            FontInput, SizeInput, TitleInput, CrystalStylesGallery, UnavailableButton })
            if (control.ToolTip is RibbonScreenTip tip) _screenTipPalette.Attach(tip);
        UpdateCrystalDetails(true);
        PreviewRibbon.Loaded += (_, _) =>
        {
            CrystalFileHover.Apply(PreviewRibbon, CompareToggle.IsChecked != true);
            CrystalQuickAccess.Apply(PreviewRibbon, CompareToggle.IsChecked != true);
        };
        _baselineLayout = RibbonCustomizationSerializer.Serialize(PreviewRibbon);
        PreviewRibbon.RibbonCustomizeRequested += (_, _) => CreateCustomizationDialog(false).ShowDialog();
        PreviewRibbon.QuickAccessCustomizeRequested += (_, _) => CreateCustomizationDialog(true).ShowDialog();
    }

    private void OnCustomize(object sender, RoutedEventArgs e) => CreateCustomizationDialog(false).ShowDialog();

    internal RibbonOptionsDialog CreateCustomizationDialog(bool quickAccess)
    {
        var dialog = new RibbonOptionsDialog { Title = "Customize Crystal", Owner = this };
        // An owned window has its own resource lookup; explicitly carry the current
        // preview palette, including baseline comparison, into the dialog.
        foreach (var dictionary in Resources.MergedDictionaries)
            dialog.Resources.MergedDictionaries.Add(dictionary);
        var ribbonPage = new RibbonOptionsPage
        {
            Header = "Customize Ribbon",
            Content = new RibbonCustomizePage { Ribbon = PreviewRibbon, ResetLayout = _baselineLayout },
        };
        var quickAccessPage = new RibbonOptionsPage
        {
            Header = "Quick Access Toolbar",
            Content = new RibbonQuickAccessPage { Ribbon = PreviewRibbon },
        };
        dialog.Pages.Add(ribbonPage);
        dialog.Pages.Add(quickAccessPage);
        dialog.SelectedPage = quickAccess ? quickAccessPage : ribbonPage;
        if (CompareToggle.IsChecked != true && _crystal != null)
            CrystalCustomization.Apply(dialog, _crystal);
        dialog.Applied += (_, _) => StatusText.Text = "Customization applied to this preview.";
        return dialog;
    }

    private void OnCompare(object sender, RoutedEventArgs e)
    {
        if (_crystal == null)
            return;

        if (CompareToggle.IsChecked == true)
        {
            Resources.MergedDictionaries.Remove(_crystal);
            PreviewLabel.Text = "OFFICE 2024 / BASELINE";
        }
        else
        {
            Resources.MergedDictionaries.Add(_crystal);
            PreviewLabel.Text = "CRYSTAL / LIGHT STUDY";
        }
        UpdateCrystalDetails(CompareToggle.IsChecked != true);
    }

    private void UpdateCrystalDetails(bool enabled)
    {
        CrystalTabShape.Apply(PreviewRibbon, enabled);
        if (PreviewRibbon.IsLoaded) CrystalQuickAccess.Apply(PreviewRibbon, enabled);
        if (PreviewRibbon.IsLoaded) CrystalFileHover.Apply(PreviewRibbon, enabled);
        _backstagePresentation.Apply(enabled ? _crystal : null);
        _screenTipPalette.Apply(enabled ? _crystal : null);
        AccentSelector.IsEnabled = enabled;
        BackstageAccentSelector.IsEnabled = enabled;
        BackstageLayoutSelector.IsEnabled = enabled;
        BackstageTintLabel.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;
        PictureTab.CrystalEnabled = enabled;
        TableTab.CrystalEnabled = enabled;
        foreach (var panel in new[] { CompactClipboard, CompactText })
        {
            if (enabled) panel.Resources["RibbonKit.Metrics.ControlCornerRadius"] = new CornerRadius(3);
            else panel.Resources.Remove("RibbonKit.Metrics.ControlCornerRadius");
        }
    }

    private void OnReturnToDocument(object sender, RoutedEventArgs e) => PreviewRibbon.IsBackstageOpen = false;

    private void OnBackstageLayoutSelected(object sender, RoutedEventArgs e)
    {
        if (sender is not RibbonMenuItem { Tag: string layout } item) return;
        _backstagePresentation.SetLayout(layout == "Floating"
            ? CrystalBackstageLayout.Floating : CrystalBackstageLayout.Sidebar);
        BackstageLayoutLabel.Text = $"Current layout: {item.Header}";
    }

    private void OnAccentSelected(object sender, RoutedEventArgs e)
    {
        if (_crystal == null || sender is not RibbonMenuItem { Tag: string value } item)
            return;
        var replacement = CrystalPalette.Create((Color)ColorConverter.ConvertFromString(value));
        int index = Resources.MergedDictionaries.IndexOf(_crystal);
        if (index < 0) return;
        Resources.MergedDictionaries[index] = replacement;
        _crystal = replacement;
        UpdateCrystalDetails(true);
        StatusText.Text = $"Glass tint: {item.Header}";
        BackstageTintLabel.Text = $"Current tint: {item.Header}";
    }

    private void OnChangeContextTint(object sender, RoutedEventArgs e)
    {
        CrystalContextualTab tab = PictureTab.IsSelected ? PictureTab : TableTab;
        Color current = (tab.ContextualColor as SolidColorBrush)?.Color ?? Colors.Teal;
        tab.ContextualColor = new SolidColorBrush(current.R > current.G
            ? Color.FromRgb(40, 126, 120) : Color.FromRgb(134, 89, 165));
        StatusText.Text = $"Changed {tab.Header} tint.";
    }

    private void OnPreviewCommand(object sender, RoutedEventArgs e)
    {
        if (sender is RibbonMenuItem item)
            StatusText.Text = $"Selected: {item.Header}";
        else if (sender is RibbonButton button)
            StatusText.Text = $"Selected: {button.Header}";
    }

    private void OnDisableInputs(object sender, RoutedEventArgs e)
    {
        if (FontInput == null || SizeInput == null || TitleInput == null)
            return;
        bool enabled = DisableInputsToggle.IsChecked != true;
        FontInput.IsEnabled = SizeInput.IsEnabled = TitleInput.IsEnabled = enabled;
    }

    private void OnGalleryStyleSelected(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (DocumentHeading == null || CrystalStylesGallery.SelectedItem is not RibbonGalleryItem { Tag: string style }) return;
        DocumentHeading.FontFamily = new FontFamily(style is "Quote" or "Classic" ? "Georgia" : "Segoe UI");
        DocumentHeading.FontSize = style == "Title" ? 32 : 24;
        DocumentHeading.FontWeight = style == "Heading" ? FontWeights.SemiBold : style == "Title" ? FontWeights.Light : FontWeights.Normal;
        DocumentHeading.FontStyle = style is "Quote" or "Emphasis" ? FontStyles.Italic : FontStyles.Normal;
        StatusText.Text = $"Document style: {style}";
    }

    private void OnDisableGallery(object sender, RoutedEventArgs e)
    {
        if (CrystalStylesGallery != null) CrystalStylesGallery.IsEnabled = DisableGalleryToggle.IsChecked != true;
    }
}
