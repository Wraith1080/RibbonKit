using System.Windows;
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
    }

    private void OnPreviewCommand(object sender, RoutedEventArgs e)
    {
        if (sender is RibbonMenuItem item)
            StatusText.Text = $"Selected: {item.Header}";
        else if (sender is RibbonButton button)
            StatusText.Text = $"Selected: {button.Header}";
    }
}
