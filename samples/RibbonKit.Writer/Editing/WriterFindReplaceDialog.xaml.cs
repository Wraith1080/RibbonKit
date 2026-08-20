using System.Windows;

namespace RibbonKit.Writer.Editing;

/// <summary>A small app-owned dialog over the deterministic Writer find/replace service.</summary>
public partial class WriterFindReplaceDialog : Window
{
    private readonly WriterFindReplaceService _service;

    /// <summary>Creates a find dialog over an existing native editor service.</summary>
    /// <param name="service">The Writer find/replace service.</param>
    /// <param name="showReplace">Whether replacement controls should initially be visible.</param>
    public WriterFindReplaceDialog(WriterFindReplaceService service, bool showReplace = true)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        InitializeComponent();
        if (!showReplace)
            HideReplaceControls();
        Loaded += (_, _) =>
        {
            QueryBox.Focus();
            QueryBox.SelectAll();
        };
    }

    /// <summary>Shows the replacement controls after a Find dialog was already opened.</summary>
    public void ShowReplaceControls()
    {
        ReplacementLabel.Visibility = Visibility.Visible;
        ReplacementBox.Visibility = Visibility.Visible;
        ReplaceButton.Visibility = Visibility.Visible;
        ReplaceAllButton.Visibility = Visibility.Visible;
        SizeToContent = SizeToContent.Height;
    }

    /// <summary>Updates replacement availability without disabling read-only find operations.</summary>
    public void SetCanReplace(bool canReplace)
    {
        ReplacementBox.IsEnabled = canReplace;
        ReplaceButton.IsEnabled = canReplace;
        ReplaceAllButton.IsEnabled = canReplace;
        ReplacementLabel.Opacity = canReplace ? 1d : 0.6d;
        if (!canReplace && ReplacementBox.Visibility == Visibility.Visible)
            ResultText.Text = "Replacement is unavailable while the document is read-only.";
    }

    private void HideReplaceControls()
    {
        ReplacementLabel.Visibility = Visibility.Collapsed;
        ReplacementBox.Visibility = Visibility.Collapsed;
        ReplaceButton.Visibility = Visibility.Collapsed;
        ReplaceAllButton.Visibility = Visibility.Collapsed;
    }

    private void OnFindNext(object sender, RoutedEventArgs e)
    {
        var result = _service.FindNext(new WriterFindOptions
        {
            Query = QueryBox.Text,
            MatchCase = MatchCaseBox.IsChecked == true,
            Wrap = true,
            StartBehavior = WriterFindStartBehavior.AfterCurrentSelection
        });
        ResultText.Text = result.EmptyQuery
            ? "Enter text to find."
            : result.Found
                ? result.Wrapped ? "Match found after wrapping to the start." : "Match found."
                : "No match found.";
    }

    private void OnReplace(object sender, RoutedEventArgs e)
    {
        if (!_service.CanMutate)
        {
            ResultText.Text = "Replacement is unavailable while the document is read-only.";
            return;
        }
        var result = _service.ReplaceNext(QueryBox.Text, ReplacementBox.Text,
            MatchCaseBox.IsChecked == true);
        ResultText.Text = result.EmptyQuery
            ? "Enter text to replace."
            : result.ReadOnly
                ? "Replacement is unavailable while the document is read-only."
            : result.StructuralBoundary
                ? "The match crosses a paragraph boundary and was left unchanged."
                : result.Replaced ? "One match replaced." : result.Found ? "The selected match was not replaceable." : "No match found.";
    }

    private void OnReplaceAll(object sender, RoutedEventArgs e)
    {
        if (!_service.CanMutate)
        {
            ResultText.Text = "Replacement is unavailable while the document is read-only.";
            return;
        }
        var count = _service.ReplaceAll(QueryBox.Text, ReplacementBox.Text,
            MatchCaseBox.IsChecked == true);
        ResultText.Text = QueryBox.Text.Length == 0
            ? "Enter text to replace."
            : $"{count:N0} match{(count == 1 ? string.Empty : "es")} replaced.";
    }

    private void OnClose(object sender, RoutedEventArgs e) => Close();
}
