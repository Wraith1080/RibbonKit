using System;
using System.Collections.Generic;
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
    private readonly CrystalMessagePresentation _messagePresentation;
    private readonly CrystalScrollBars[] _scrollBars;
    private readonly CrystalApplicationMenuPresentation _applicationMenuPresentation;
    private readonly CrystalPopupBackdrop[] _popupBackdrops;
    private readonly CrystalDocumentEdgeFade _documentEdgeFade;
    private readonly string _baselineLayout;
    private readonly List<RibbonTab> _scrollPreviewTabs = new();
    private readonly Dictionary<RibbonGroup, bool> _savedHomeResizing = new();
    private readonly List<RibbonButton> _overflowPreviewItems = new();
    private RibbonQuickAccessPosition _savedQuickAccessPosition;
    private double _savedQuickAccessWidth;
    private bool _documentEdgeFadeEnabled = true;
    private bool _documentQatUnderlayEnabled = true;

    public CrystalPreviewWindow()
    {
        InitializeComponent();
        PreviewRibbon.ApplicationMenu = null;
        _applicationMenuPresentation = new CrystalApplicationMenuPresentation(CrystalFileMenu, this);
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
        _messagePresentation = new CrystalMessagePresentation(CrystalMessageBar);
        _scrollBars = new[]
        {
            new CrystalScrollBars(CrystalDocumentScroll), new CrystalScrollBars(CrystalOverviewScroll),
            new CrystalScrollBars(CrystalAppearanceScroll), new CrystalScrollBars(CrystalAboutScroll),
        };
        _documentEdgeFade = new CrystalDocumentEdgeFade(PreviewRibbon, CrystalDocumentScroll);
        DocumentScrollDemo.ItemsSource = new[]
        {
            "A clear document gives the commands a quiet place to work. As the page moves, the ribbon stays steady and the quick access drawer keeps its place above the content.",
            "Soft reflections belong at the edges of a control. They make its shape visible without drawing attention away from the words and choices inside it.",
            "The document can pass beneath the translucent quick access drawer. Its text stays subdued there, then returns to full strength just below the drawer's lower edge.",
            "Scrolling is a useful test of the material. Watch the page surface, headings, and body text move past the drawer while its icons and border remain easy to read.",
            "A second look can reveal small changes in contrast. Try a different glass tint, then return to this page to see how the same content feels beneath the new color.",
            "The ribbon remains available while the document moves. Commands should feel close at hand without interrupting the natural flow of reading or editing.",
            "At narrower window sizes, these paragraphs wrap into more lines. The card grows with the text, giving the scrolling edge and scrollbar room to demonstrate their behavior.",
            "This longer sample is only a preview aid. Remove it from Preview controls to return to the short document and compare the original layout again.",
        };
        var popups = new List<CrystalPopupBackdrop>();
        foreach (var control in new RibbonDropDownButton[]
        {
            ArrangeButton, AccentSelector, PreviewControlsButton, StyleSplitButton,
            BackstageAccentSelector, BackstageLayoutSelector,
        })
            popups.Add(new CrystalPopupBackdrop(this, control, "PART_MenuHost"));
        foreach (RibbonTab tab in PreviewRibbon.Tabs)
            foreach (RibbonGroup group in tab.Groups)
                popups.Add(new CrystalPopupBackdrop(this, group, "PART_PopupHost"));
        _popupBackdrops = popups.ToArray();
        foreach (var control in new FrameworkElement[] { CompareToggle, ArrangeButton, AccentSelector,
            FontInput, SizeInput, TitleInput, CrystalStylesGallery, UnavailableButton })
            if (control.ToolTip is RibbonScreenTip tip) _screenTipPalette.Attach(tip);
        UpdateCrystalDetails(true);
        PreviewRibbon.Loaded += (_, _) =>
        {
            CrystalFileHover.Apply(PreviewRibbon, CompareToggle.IsChecked != true);
            CrystalQuickAccess.Apply(PreviewRibbon, CompareToggle.IsChecked != true);
            CrystalSplitButtons.Apply(PreviewRibbon, CompareToggle.IsChecked != true);
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
        CrystalUtilityChrome.Apply(this, enabled);
        _messagePresentation.Apply(enabled);
        _applicationMenuPresentation.Apply(enabled);
        foreach (var popup in _popupBackdrops) popup.Apply(enabled);
        _documentEdgeFade.Apply(enabled, _documentEdgeFadeEnabled, _documentQatUnderlayEnabled);
        DocumentFadeToggle.IsEnabled = enabled;
        DocumentQatUnderlayToggle.IsEnabled = enabled;
        CrystalTabShape.Apply(PreviewRibbon, enabled);
        CrystalSplitButtons.Apply(PreviewRibbon, enabled);
        if (PreviewRibbon.IsLoaded) CrystalQuickAccess.Apply(PreviewRibbon, enabled);
        if (PreviewRibbon.IsLoaded) CrystalFileHover.Apply(PreviewRibbon, enabled);
        _backstagePresentation.Apply(enabled ? _crystal : null);
        _screenTipPalette.Apply(enabled ? _crystal : null);
        var accent = ((SolidColorBrush)FindResource("RibbonKit.Brushes.Accent")).Color;
        foreach (var scrollBars in _scrollBars) scrollBars.Apply(enabled ? _crystal : null, accent);
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

    private void OnDocumentFadeToggle(object sender, RoutedEventArgs e)
    {
        _documentEdgeFadeEnabled = !_documentEdgeFadeEnabled;
        _documentEdgeFade.Apply(CompareToggle.IsChecked != true, _documentEdgeFadeEnabled,
            _documentQatUnderlayEnabled);
        DocumentFadeToggle.Header = _documentEdgeFadeEnabled
            ? "Hide document edge fade" : "Show document edge fade";
        StatusText.Text = _documentEdgeFadeEnabled
            ? "The scrolled document fades gently at its top edge."
            : "The scrolled document has a hard top edge.";
    }

    private void OnDocumentQatUnderlayToggle(object sender, RoutedEventArgs e)
    {
        _documentQatUnderlayEnabled = !_documentQatUnderlayEnabled;
        _documentEdgeFade.Apply(CompareToggle.IsChecked != true, _documentEdgeFadeEnabled,
            _documentQatUnderlayEnabled);
        DocumentQatUnderlayToggle.Header = _documentQatUnderlayEnabled
            ? "Keep document below QAT" : "Show document through QAT";
        StatusText.Text = _documentQatUnderlayEnabled
            ? "The document scrolls behind the below-ribbon QAT."
            : "The document stays below the QAT.";
    }

    private void OnDocumentLengthToggle(object sender, RoutedEventArgs e)
    {
        bool show = DocumentScrollDemo.Visibility != Visibility.Visible;
        DocumentScrollDemo.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        DocumentLengthToggle.Header = show ? "Remove scrolling text" : "Add scrolling text";
        StatusText.Text = show
            ? "Extra document text added. Scroll to inspect the page edge."
            : "Extra document text removed.";
    }

    private void OnToggleApplicationMenu(object sender, RoutedEventArgs e)
    {
        PreviewRibbon.IsBackstageOpen = false;
        bool useMenu = PreviewRibbon.ApplicationMenu == null;
        PreviewRibbon.ApplicationMenu = useMenu ? CrystalFileMenu : null;
        ApplicationMenuPreviewToggle.Header = useMenu ? "Use Backstage" : "Use application menu";
        StatusText.Text = useMenu ? "File now opens the application menu." : "File now opens Backstage.";
    }

    private void OnUseBackstage(object sender, RoutedEventArgs e)
    {
        PreviewRibbon.IsBackstageOpen = false;
        PreviewRibbon.ApplicationMenu = null;
        ApplicationMenuPreviewToggle.Header = "Use application menu";
        StatusText.Text = "File now opens Backstage.";
    }

    private void OnCloseApplicationMenu(object sender, RoutedEventArgs e) => CrystalFileMenu.RequestClose();

    private void OnApplicationMenuCommand(object sender, RoutedEventArgs e)
    {
        string label = sender is RibbonApplicationMenuItem item ? item.Header?.ToString() ?? "Command"
            : (sender as System.Windows.Controls.ContentControl)?.Content?.ToString() ?? "Command";
        StatusText.Text = $"{label} selected (preview only).";
        CrystalFileMenu.RequestClose();
    }

    private void OnShowMessage(object sender, RoutedEventArgs e)
    {
        foreach (var message in new[] { CrystalProtectedMessage, CrystalSecurityMessage })
        {
            if (message.IsOpen) continue;
            message.IsOpen = true;
            StatusText.Text = $"Showing {message.Title}. Use its action or close button, or show the next message.";
            return;
        }
        StatusText.Text = "Both sample messages are visible. Dismiss either one to try reopening it.";
    }

    private void OnDismissMessages(object sender, RoutedEventArgs e)
    {
        CrystalProtectedMessage.Dismiss();
        CrystalSecurityMessage.Dismiss();
        StatusText.Text = "Sample messages dismissed.";
    }

    private void OnMessageAction(object sender, RoutedEventArgs e)
    {
        if (sender is not RibbonMessage message) return;
        message.Dismiss();
        StatusText.Text = $"{message.Title} acknowledged. This action affects the preview only.";
    }

    private void OnScrollPreview(object sender, RoutedEventArgs e)
    {
        if (_scrollPreviewTabs.Count == 0)
        {
            int count = Math.Max(6, (int)Math.Ceiling(PreviewRibbon.ActualWidth / 120) + 2);
            for (int i = 1; i <= count; i++)
            {
                var tab = new RibbonTab { Header = $"Navigation preview {i}" };
                var group = new RibbonGroup { Header = "Sample commands" };
                var button = new RibbonButton { Header = "Select", Icon = (ImageSource)FindResource("Icon.Select"), Size = RibbonControlSize.Large };
                button.Click += OnPreviewCommand;
                group.Items.Add(button);
                tab.Groups.Add(group);
                _scrollPreviewTabs.Add(tab);
                PreviewRibbon.Tabs.Add(tab);
            }
            StatusText.Text = "Use the arrows at either end of the tab row. Choose Hide scroll arrows to remove the extra tabs.";
            ScrollPreviewToggle.Header = "Hide scroll arrows";
        }
        else
        {
            foreach (var tab in _scrollPreviewTabs) PreviewRibbon.Tabs.Remove(tab);
            _scrollPreviewTabs.Clear();
            StatusText.Text = "Scroll preview tabs removed.";
            ScrollPreviewToggle.Header = "Show scroll arrows";
        }
        // Realize the temporary headers before asking the scroller to discard its
        // cached constrained measurement of the old tab set.
        PreviewRibbon.UpdateLayout();
        if (PreviewRibbon.Template.FindName("TabControlHost", PreviewRibbon) is RibbonTabControl tabs &&
            tabs.Template.FindName("PART_TabScroll", tabs) is RibbonKit.Layout.RibbonScrollContentHost scroller)
            scroller.Refresh();
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
            StatusText.Text = "Home groups stay expanded. Narrow the window to try the ribbon-body scroll arrows.";
        }
        else
        {
            foreach (var entry in _savedHomeResizing)
                entry.Key.SetCurrentValue(RibbonGroup.CanResizeProperty, entry.Value);
            _savedHomeResizing.Clear();
            BodyScrollPreviewToggle.Header = "Keep Home groups expanded";
            StatusText.Text = "Home group resizing restored.";
        }
        PreviewRibbon.UpdateLayout();
        if (PreviewRibbon.Template.FindName("TabControlHost", PreviewRibbon) is RibbonTabControl tabs &&
            tabs.Template.FindName("PART_ContentScroll", tabs) is RibbonKit.Layout.RibbonScrollContentHost scroller)
            scroller.Refresh();
    }

    private void OnOverflowPreview(object sender, RoutedEventArgs e)
    {
        if (_overflowPreviewItems.Count == 0)
        {
            _savedQuickAccessPosition = PreviewRibbon.QuickAccessPosition;
            _savedQuickAccessWidth = PreviewRibbon.QuickAccessMaxWidth;
            foreach (var name in new[] { "Save", "Undo", "Redo", "Print" })
            {
                var button = new RibbonButton { Header = name, Icon = (ImageSource)FindResource("Icon." + name), Size = RibbonControlSize.Small };
                button.Click += OnPreviewCommand;
                _overflowPreviewItems.Add(button);
                PreviewRibbon.QuickAccessItems.Add(button);
            }
            PreviewRibbon.QuickAccessMaxWidth = 100;
            PreviewRibbon.QuickAccessPosition = RibbonQuickAccessPosition.TabRow;
            StatusText.Text = "QAT moved beside the tabs. Open its overflow arrow; choose Restore QAT position to finish.";
            OverflowPreviewToggle.Header = "Restore QAT position";
        }
        else
        {
            foreach (var button in _overflowPreviewItems) PreviewRibbon.QuickAccessItems.Remove(button);
            _overflowPreviewItems.Clear();
            PreviewRibbon.QuickAccessMaxWidth = _savedQuickAccessWidth;
            PreviewRibbon.QuickAccessPosition = _savedQuickAccessPosition;
            StatusText.Text = "Quick access toolbar restored.";
            OverflowPreviewToggle.Header = "Show QAT overflow";
        }
    }

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
