using System.Windows;
using System.Windows.Media;
using RibbonKit.Controls;

namespace RibbonKit.Showcase;

/// <summary>Isolated, consumer-scoped exploration of a glass-inspired Office 2024 palette.</summary>
public partial class CrystalPreviewWindow : RibbonWindow
{
    private ResourceDictionary? _crystal;

    public CrystalPreviewWindow()
    {
        InitializeComponent();
        _crystal = Resources.MergedDictionaries[1];
        UpdateCrystalDetails(true);
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
        PictureTab.CrystalEnabled = enabled;
        TableTab.CrystalEnabled = enabled;
        foreach (var panel in new[] { CompactClipboard, CompactText })
        {
            if (enabled) panel.Resources["RibbonKit.Metrics.ControlCornerRadius"] = new CornerRadius(3);
            else panel.Resources.Remove("RibbonKit.Metrics.ControlCornerRadius");
        }
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
}
