using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using RibbonKit.Writer.Models;

namespace RibbonKit.Writer.Page;

/// <summary>Transactional four-edge custom-margin editor for the current Writer page.</summary>
public partial class WriterCustomMarginsDialog : Window
{
    private readonly DocumentPageSettings _openingSettings;
    private DocumentPageSettings? _candidate;
    private bool _initializing;

    /// <summary>Creates a custom-margin dialog without mutating the supplied settings.</summary>
    public WriterCustomMarginsDialog(DocumentPageSettings openingSettings)
    {
        _openingSettings = openingSettings ?? throw new ArgumentNullException(nameof(openingSettings));
        _initializing = true;
        InitializeComponent();
        TopBox.Text = Format(openingSettings.Margins.TopDip);
        BottomBox.Text = Format(openingSettings.Margins.BottomDip);
        LeftBox.Text = Format(openingSettings.Margins.LeftDip);
        RightBox.Text = Format(openingSettings.Margins.RightDip);
        _initializing = false;
        ValidateInput();
    }

    /// <summary>Gets the validated replacement after Apply; null after Cancel or invalid input.</summary>
    public DocumentPageSettings? ResultSettings { get; private set; }

    private void OnMarginTextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_initializing && IsInitialized)
            ValidateInput();
    }

    private void ValidateInput()
    {
        var valid = WriterPageUi.TryCreateCustomSettings(_openingSettings,
            TopBox.Text, BottomBox.Text, LeftBox.Text, RightBox.Text,
            CultureInfo.CurrentCulture, out _candidate, out var error);
        ValidationText.Text = error;
        ApplyButton.IsEnabled = valid;
        UpdatePreview(valid ? _candidate : null);
    }

    private void UpdatePreview(DocumentPageSettings? settings)
    {
        if (settings is null)
        {
            MarginPreview.Visibility = Visibility.Collapsed;
            return;
        }

        MarginPreview.Visibility = Visibility.Visible;
        const double previewLongEdge = 194;
        if (settings.WidthDip >= settings.HeightDip)
        {
            PagePreview.Width = previewLongEdge;
            PagePreview.Height = previewLongEdge * settings.HeightDip / settings.WidthDip;
        }
        else
        {
            PagePreview.Height = previewLongEdge;
            PagePreview.Width = previewLongEdge * settings.WidthDip / settings.HeightDip;
        }
        var horizontalScale = PagePreview.Width / settings.WidthDip;
        var verticalScale = PagePreview.Height / settings.HeightDip;
        var scale = Math.Min(horizontalScale, verticalScale);
        var margins = settings.Margins;
        MarginPreview.Margin = new Thickness(
            margins.LeftDip * scale, margins.TopDip * scale,
            margins.RightDip * scale, margins.BottomDip * scale);
    }

    private void OnApply(object sender, RoutedEventArgs e)
    {
        ValidateInput();
        if (_candidate is null)
            return;
        ResultSettings = _candidate;
        DialogResult = true;
    }

    private static string Format(double dip) =>
        DocumentLength.DipsToInches(dip).ToString("0.##", CultureInfo.CurrentCulture);
}
