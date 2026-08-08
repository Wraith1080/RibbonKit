using System.Globalization;
using System.Windows;
using System.Windows.Controls.Primitives;
using RibbonKit.Controls;
using RibbonKit.Localization;

namespace RibbonKit.Showcase;

/// <summary>
/// Interactive Phase 6 lab for RibbonKit-owned localization, RTL layout, disconnected context
/// menus, popup direction, and the two built-in customization pages.
/// </summary>
public partial class LocalizationRtlDemo : RibbonWindow
{
    private readonly PseudoLocalizationProvider _pseudoProvider = new();
    private readonly RibbonApplicationMenu _applicationMenu;
    private readonly string _baselineLayout;
    private IRibbonLocalizationProvider? _providerBeforePseudoLocalization;
    private MainWindow? _applicationSurfaceSource;

    public LocalizationRtlDemo()
    {
        InitializeComponent();
        _applicationMenu = DemoApplicationMenu;
        DemoRibbon.ApplicationMenu = null;
        _baselineLayout = RibbonCustomizationSerializer.Serialize(DemoRibbon);
        UpdateStatus();
    }

    internal void AttachApplicationSurfaceSource(MainWindow source)
    {
        if (ReferenceEquals(_applicationSurfaceSource, source))
        {
            return;
        }

        DetachApplicationSurfaceSource();
        _applicationSurfaceSource = source;
        source.ApplicationSurfaceChanged += OnApplicationSurfaceChanged;
        ApplyApplicationSurfaceState(source.ApplicationSurfaceState);
    }

    protected override void OnClosed(EventArgs e)
    {
        DetachApplicationSurfaceSource();
        DisablePseudoLocalization();
        base.OnClosed(e);
    }

    private void OnApplicationSurfaceChanged(object? sender, EventArgs e)
    {
        if (_applicationSurfaceSource is { } source)
        {
            ApplyApplicationSurfaceState(source.ApplicationSurfaceState);
        }
    }

    private void ApplyApplicationSurfaceState(ShowcaseApplicationSurfaceState state)
    {
        bool usesApplicationMenu = DemoRibbon.ApplicationMenu is not null;
        if (usesApplicationMenu != state.UsesApplicationMenu)
        {
            DemoRibbon.IsBackstageOpen = false;
            DemoRibbon.ApplicationMenu = state.UsesApplicationMenu ? _applicationMenu : null;
        }

        DemoRibbon.ApplicationButtonShape = state.ApplicationButtonShape;
        DemoBackstage.Design = state.BackstageDesign;
        DemoBackstage.Translucent = state.BackstageTranslucent;
        UpdateStatus();
    }

    private void DetachApplicationSurfaceSource()
    {
        if (_applicationSurfaceSource is { } source)
        {
            source.ApplicationSurfaceChanged -= OnApplicationSurfaceChanged;
            _applicationSurfaceSource = null;
        }
    }

    private void OnTogglePseudoLocalization(object sender, RoutedEventArgs e)
    {
        bool enabled = (sender as ToggleButton)?.IsChecked == true;
        if (enabled)
        {
            if (!ReferenceEquals(RibbonLocalization.Provider, _pseudoProvider))
            {
                _providerBeforePseudoLocalization = RibbonLocalization.Provider;
                RibbonLocalization.Provider = _pseudoProvider;
            }
        }
        else
        {
            DisablePseudoLocalization();
        }

        UpdateStatus();
    }

    private void OnToggleRightToLeft(object sender, RoutedEventArgs e)
    {
        FlowDirection = (sender as ToggleButton)?.IsChecked == true
            ? FlowDirection.RightToLeft
            : FlowDirection.LeftToRight;
        UpdateStatus();
    }

    private void OnOpenCustomizeRibbon(object sender, RoutedEventArgs e) =>
        OpenOptionsDialog(showQuickAccessPage: false);

    private void OnOpenQuickAccess(object sender, RoutedEventArgs e) =>
        OpenOptionsDialog(showQuickAccessPage: true);

    private void OnCustomizeQuickAccess(object sender, EventArgs e) =>
        OpenOptionsDialog(showQuickAccessPage: true);

    private void OnCustomizeRibbon(object sender, EventArgs e) =>
        OpenOptionsDialog(showQuickAccessPage: false);

    private void OnCloseLab(object sender, RoutedEventArgs e) => Close();

    private void OpenOptionsDialog(bool showQuickAccessPage)
    {
        var customizePage = new RibbonOptionsPage
        {
            Header = RibbonLocalization.GetString(RibbonString.CustomizeRibbonPage),
            Content = new RibbonCustomizePage
            {
                Ribbon = DemoRibbon,
                ResetLayout = _baselineLayout,
            },
        };
        var quickAccessPage = new RibbonOptionsPage
        {
            Header = RibbonLocalization.GetString(RibbonString.QuickAccessToolbarPage),
            Content = new RibbonQuickAccessPage { Ribbon = DemoRibbon },
        };
        var dialog = new RibbonOptionsDialog
        {
            Title = "Localization & RTL Options",
            Owner = this,
            FlowDirection = FlowDirection,
        };
        dialog.Pages.Add(customizePage);
        dialog.Pages.Add(quickAccessPage);
        dialog.SelectedPage = showQuickAccessPage ? quickAccessPage : customizePage;
        dialog.ShowDialog();
    }

    private void DisablePseudoLocalization()
    {
        if (ReferenceEquals(RibbonLocalization.Provider, _pseudoProvider))
        {
            RibbonLocalization.Provider = _providerBeforePseudoLocalization;
        }

        _providerBeforePseudoLocalization = null;
    }

    private void UpdateStatus()
    {
        if (StatusText is null)
        {
            return;
        }

        string flow = FlowDirection == FlowDirection.RightToLeft
            ? "Right-to-left"
            : "Left-to-right";
        string localization = ReferenceEquals(RibbonLocalization.Provider, _pseudoProvider)
            ? "Pseudo localization enabled"
            : "Embedded/application provider";
        string surface = DemoRibbon.ApplicationMenu is not null
            ? $"Application menu ({DemoRibbon.ApplicationButtonShape})"
            : $"{DemoBackstage.Design} Backstage ({DemoRibbon.ApplicationButtonShape})";
        StatusText.Text =
            $"Layout: {flow}\nFile surface: {surface}\nBuilt-in strings: {localization}\nUI culture: {CultureInfo.CurrentUICulture.Name}";
    }

    private sealed class PseudoLocalizationProvider : IRibbonLocalizationProvider
    {
        public string? GetString(RibbonString key, CultureInfo culture) => key switch
        {
            // Preserve the pipe-delimited syntax required by Win32 file dialogs.
            RibbonString.RibbonLayoutFileFilter => null,

            // Preserve the format placeholder consumed by string.Format.
            RibbonString.CustomItemFormat => "⟦CUSTOM: {0}⟧",

            _ => $"⟦{key}⟧",
        };
    }
}
