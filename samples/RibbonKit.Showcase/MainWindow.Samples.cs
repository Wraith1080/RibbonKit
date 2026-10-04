using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using RibbonKit.Controls;
using RibbonKit.Theming;

namespace RibbonKit.Showcase;

public partial class MainWindow
{
    // No command binding: only this sample action is unavailable, not its message row.
    public static RoutedUICommand UnavailableMessageActionCommand { get; } =
        new("Enable Content", "UnavailableMessageAction", typeof(MainWindow));

    private readonly List<RibbonTab> _scrollPreviewTabs = new();
    private readonly Dictionary<RibbonGroup, bool> _savedHomeResizing = new();
    private readonly List<RibbonButton> _overflowPreviewItems = new();
    private readonly List<Paragraph> _scrollingParagraphs = new();
    private RibbonQuickAccessPosition _savedQuickAccessPosition;
    private double _savedQuickAccessWidth;
    private bool _changingSamples;
    private bool _comparingTheme;
    private bool _documentFadeEnabled;
    private bool _documentQatUnderlayEnabled;
    private CrystalDocumentEdgeFade? _documentEdgeFade;

    private void InitializeSamples()
    {
        // Explicit sources keep host bindings intact when groups or File content detach.
        foreach (var panel in new[] { SampleOptionsPanel, SampleSpacingPanel })
            panel.SetBinding(IsEnabledProperty, new Binding(nameof(EnableOptionsToggle.IsChecked))
            { Source = EnableOptionsToggle });
        DocumentTitle.SetBinding(Run.TextProperty, new Binding(nameof(TitleInput.Text)) { Source = TitleInput });
        BackstageDocumentTitle.SetBinding(System.Windows.Controls.TextBlock.TextProperty,
            new Binding(nameof(TitleInput.Text)) { Source = TitleInput });
        _documentEdgeFade = new CrystalDocumentEdgeFade(MainRibbon, DocumentEditor, DocumentSurface);
        UpdateDocumentEffects();
    }

    private void UpdateDocumentEffects()
    {
        bool crystal = ThemeManager.CurrentTheme == RibbonTheme.CrystalLight;
        _documentEdgeFade?.Apply(crystal, _documentFadeEnabled, _documentQatUnderlayEnabled);
        DocumentFadeToggle.IsEnabled = crystal;
        DocumentQatUnderlayToggle.IsEnabled = crystal;
        _comparingTheme = true;
        try
        {
            CompareToggle.IsEnabled = crystal || ThemeManager.CurrentTheme == RibbonTheme.Office2024;
            CompareToggle.IsChecked = ThemeManager.CurrentTheme == RibbonTheme.Office2024;
        }
        finally { _comparingTheme = false; }
    }

    private void OnCompare2024(object sender, RoutedEventArgs e)
    {
        if (_comparingTheme || ThemeGallery is null) return;
        ApplyTheme(CompareToggle.IsChecked == true ? RibbonTheme.Office2024 : RibbonTheme.CrystalLight);
    }

    private void OnDocumentFadeToggle(object sender, RoutedEventArgs e)
    {
        _documentFadeEnabled = !_documentFadeEnabled;
        DocumentFadeToggle.Header = _documentFadeEnabled ? "Hide document edge fade" : "Show document edge fade";
        UpdateDocumentEffects();
    }

    private void OnDocumentQatUnderlayToggle(object sender, RoutedEventArgs e)
    {
        _documentQatUnderlayEnabled = !_documentQatUnderlayEnabled;
        DocumentQatUnderlayToggle.Header = _documentQatUnderlayEnabled ? "Keep document below QAT" : "Show document through QAT";
        UpdateDocumentEffects();
    }

    private void OnDocumentLengthToggle(object sender, RoutedEventArgs e)
    {
        if (_scrollingParagraphs.Count == 0)
        {
            for (int i = 1; i <= 8; i++)
            {
                var paragraph = new Paragraph(new Run($"{i:00}  Surfaces and light. Scroll this editable sample to compare the document edge, the quick access drawer and the File surfaces. Switch the theme or tint while keeping your document text and control values."))
                { Margin = new Thickness(0, 18, 0, 0) };
                _scrollingParagraphs.Add(paragraph);
                DocumentEditor.Document.Blocks.Add(paragraph);
            }
            DocumentLengthToggle.Header = "Remove scrolling text";
        }
        else
        {
            foreach (var paragraph in _scrollingParagraphs) DocumentEditor.Document.Blocks.Remove(paragraph);
            _scrollingParagraphs.Clear();
            DocumentLengthToggle.Header = "Add scrolling text";
        }
        StatusReady.Content = _scrollingParagraphs.Count > 0 ? "Scrolling text added" : "Scrolling text removed";
    }

    private void OnDismissMessages(object sender, RoutedEventArgs e)
    {
        ProtectedViewMessage.Dismiss();
        SecurityNoticeMessage.Dismiss();
        UnavailableActionMessage.Dismiss();
        StatusReady.Content = "Sample messages dismissed";
    }

    private void OnDisableGallery(object sender, RoutedEventArgs e)
    {
        if (StylesGallery is not null) StylesGallery.IsEnabled = DisableGalleryToggle.IsChecked != true;
    }

    private void OnChangeContextTint(object sender, RoutedEventArgs e)
    {
        Color current = (PictureFormatTab.ContextualColor as SolidColorBrush)?.Color ?? Colors.Teal;
        PictureFormatTab.ContextualColor = new SolidColorBrush(current.R > current.G
            ? Color.FromRgb(40, 126, 120) : Color.FromRgb(134, 89, 165));
        PictureFormatTab.Visibility = Visibility.Visible;
        MainRibbon.SelectedTab = PictureFormatTab;
        StatusReady.Content = "Picture Format tint changed";
    }

    private void OnScrollPreview(object sender, RoutedEventArgs e)
    {
        if (_scrollPreviewTabs.Count == 0)
        {
            int count = Math.Max(6, (int)Math.Ceiling(MainRibbon.ActualWidth / 120) + 2);
            for (int i = 1; i <= count; i++)
            {
                var tab = new RibbonTab { Header = $"Navigation preview {i}" };
                var group = new RibbonGroup { Header = "Sample commands" };
                group.Items.Add(new RibbonButton { Header = "Select", Icon = (ImageSource)FindResource("Icon.Select"), Size = RibbonControlSize.Large });
                tab.Groups.Add(group);
                _scrollPreviewTabs.Add(tab);
                MainRibbon.Tabs.Add(tab);
            }
            ScrollPreviewToggle.Header = "Hide scroll arrows";
            StatusReady.Content = "Use the arrows at either end of the tab row";
        }
        else
        {
            foreach (var tab in _scrollPreviewTabs) MainRibbon.Tabs.Remove(tab);
            _scrollPreviewTabs.Clear();
            ScrollPreviewToggle.Header = "Show scroll arrows";
            StatusReady.Content = "Scroll preview tabs removed";
        }
        RefreshSampleScroller("PART_TabScroll");
    }

    private void OnBodyScrollPreview(object sender, RoutedEventArgs e)
    {
        if (_savedHomeResizing.Count == 0)
        {
            foreach (var group in HomeTab.Groups)
            {
                _savedHomeResizing.Add(group, group.CanResize);
                group.SetCurrentValue(RibbonGroup.CanResizeProperty, false);
            }
            BodyScrollPreviewToggle.Header = "Restore Home group resizing";
            StatusReady.Content = "Home groups stay expanded; narrow the window to try the scroll arrows";
        }
        else
        {
            foreach (var entry in _savedHomeResizing) entry.Key.SetCurrentValue(RibbonGroup.CanResizeProperty, entry.Value);
            _savedHomeResizing.Clear();
            BodyScrollPreviewToggle.Header = "Keep Home groups expanded";
            StatusReady.Content = "Home group resizing restored";
        }
        MainRibbon.SelectedIndex = MainRibbon.Tabs.IndexOf(HomeTab);
        RefreshSampleScroller("PART_ContentScroll");
    }

    private void OnOverflowPreview(object sender, RoutedEventArgs e)
    {
        _changingSamples = true;
        try
        {
            if (_overflowPreviewItems.Count == 0)
            {
                _savedQuickAccessPosition = MainRibbon.QuickAccessPosition;
                _savedQuickAccessWidth = MainRibbon.QuickAccessMaxWidth;
                foreach (var name in new[] { "Save", "Undo", "Redo", "Print" })
                {
                    var button = new RibbonButton { Header = name, Icon = (ImageSource)FindResource("Icon." + name), Size = RibbonControlSize.Small };
                    _overflowPreviewItems.Add(button);
                    MainRibbon.QuickAccessItems.Add(button);
                }
                MainRibbon.QuickAccessMaxWidth = 100;
                MainRibbon.QuickAccessPosition = RibbonQuickAccessPosition.TabRow;
                OverflowPreviewToggle.Header = "Restore QAT position";
                StatusReady.Content = "Open the QAT overflow arrow beside the tabs";
            }
            else
            {
                foreach (var button in _overflowPreviewItems) MainRibbon.QuickAccessItems.Remove(button);
                _overflowPreviewItems.Clear();
                MainRibbon.QuickAccessMaxWidth = _savedQuickAccessWidth;
                MainRibbon.QuickAccessPosition = _savedQuickAccessPosition;
                OverflowPreviewToggle.Header = "Show QAT overflow";
                StatusReady.Content = "Quick access toolbar restored";
            }
        }
        finally { _changingSamples = false; }
        SaveCustomization();
    }

    private void RefreshSampleScroller(string name)
    {
        MainRibbon.UpdateLayout();
        if (MainRibbon.Template?.FindName("TabControlHost", MainRibbon) is RibbonTabControl tabs &&
            tabs.Template?.FindName(name, tabs) is RibbonKit.Layout.RibbonScrollContentHost scroller)
            scroller.Refresh();
    }
}
