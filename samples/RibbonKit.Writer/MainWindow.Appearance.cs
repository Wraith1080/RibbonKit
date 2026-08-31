using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using RibbonKit.Animation;
using RibbonKit.Controls;
using RibbonKit.Interop;
using RibbonKit.Localization;
using RibbonKit.Theming;
using RibbonKit.Writer.Appearance;

namespace RibbonKit.Writer;

public partial class MainWindow
{
    private readonly WriterSettingsStore _writerSettings =
        new(WriterSettingsPaths.CreateDefault());
    private WriterAppearancePreferences _appearancePreferences = new();
    private string? _baselineRibbonLayout;
    private RibbonOptionsDialog? _settingsDialog;
    private DependencyPropertyDescriptor? _quickAccessPositionDescriptor;
    private bool _suppressRibbonPersistence;

    private void InitializeWriterSettings()
    {
        // Real Writer startup always has App.Current. Isolated window tests intentionally do not;
        // keep them hermetic rather than reading or writing the interactive user's settings.
        if (Application.Current is null)
            return;

        _appearancePreferences = _writerSettings.LoadAppearance();
        ApplyAppearance(_appearancePreferences);

        MainRibbon.QuickAccessCustomizeRequested += OnQuickAccessCustomizeRequested;
        MainRibbon.RibbonCustomizeRequested += OnRibbonCustomizeRequested;
        Loaded += OnWriterWindowLoaded;
    }

    private void OnWriterWindowLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnWriterWindowLoaded;
        _baselineRibbonLayout = RibbonCustomizationSerializer.Serialize(MainRibbon);

        string? layout = _writerSettings.LoadRibbonLayout();
        if (!string.IsNullOrWhiteSpace(layout))
        {
            try
            {
                _suppressRibbonPersistence = true;
                RibbonCustomizationSerializer.Apply(MainRibbon, layout);
            }
            catch (InvalidOperationException)
            {
                // A foreign or structurally invalid layout leaves the factory ribbon intact.
                RibbonCustomizationSerializer.Apply(MainRibbon, _baselineRibbonLayout);
            }
            finally
            {
                _suppressRibbonPersistence = false;
            }
        }

        ((INotifyCollectionChanged)MainRibbon.QuickAccessItems).CollectionChanged +=
            OnQuickAccessItemsChanged;
        _quickAccessPositionDescriptor = DependencyPropertyDescriptor.FromProperty(
            Ribbon.QuickAccessPositionProperty,
            typeof(Ribbon));
        _quickAccessPositionDescriptor?.AddValueChanged(MainRibbon, OnQuickAccessPositionChanged);
    }

    private void OnQuickAccessItemsChanged(object? sender, NotifyCollectionChangedEventArgs e) =>
        SaveRibbonLayoutIfReady();

    private void OnQuickAccessPositionChanged(object? sender, EventArgs e) =>
        SaveRibbonLayoutIfReady();

    private void SaveRibbonLayoutIfReady()
    {
        if (_suppressRibbonPersistence || _settingsDialog is not null || _baselineRibbonLayout is null)
            return;

        _writerSettings.SaveRibbonLayout(RibbonCustomizationSerializer.Serialize(MainRibbon));
    }

    private void OnBackstageSettings(object sender, RoutedEventArgs e)
    {
        MainRibbon.IsBackstageOpen = false;
        Dispatcher.BeginInvoke(
            DispatcherPriority.ContextIdle,
            () => OpenSettingsDialog(WriterSettingsPage.Appearance));
    }

    private void OnQuickAccessCustomizeRequested(object? sender, EventArgs e) =>
        OpenSettingsDialog(WriterSettingsPage.QuickAccess);

    private void OnRibbonCustomizeRequested(object? sender, EventArgs e) =>
        OpenSettingsDialog(WriterSettingsPage.CustomizeRibbon);

    private void OpenSettingsDialog(WriterSettingsPage selected)
    {
        if (_settingsDialog is not null)
        {
            _settingsDialog.Activate();
            return;
        }

        string openingRibbon = RibbonCustomizationSerializer.Serialize(MainRibbon);
        WriterAppearancePreferences rollbackAppearance = _appearancePreferences;
        string rollbackRibbon = openingRibbon;

        var appearanceContent = new WriterAppearancePage();
        appearanceContent.SetPreferences(_appearancePreferences);
        var appearancePage = new RibbonOptionsPage
        {
            Header = "Appearance",
            Content = appearanceContent,
        };
        var customizePage = new RibbonOptionsPage
        {
            Header = RibbonLocalization.GetString(RibbonString.CustomizeRibbonPage),
            Content = new RibbonCustomizePage
            {
                Ribbon = MainRibbon,
                ResetLayout = _baselineRibbonLayout,
            },
        };
        var quickAccessPage = new RibbonOptionsPage
        {
            Header = RibbonLocalization.GetString(RibbonString.QuickAccessToolbarPage),
            Content = new RibbonQuickAccessPage { Ribbon = MainRibbon },
        };

        var dialog = new RibbonOptionsDialog
        {
            Title = "Settings",
            Owner = this,
        };
        dialog.Pages.Add(appearancePage);
        dialog.Pages.Add(customizePage);
        dialog.Pages.Add(quickAccessPage);
        dialog.SelectedPage = selected switch
        {
            WriterSettingsPage.CustomizeRibbon => customizePage,
            WriterSettingsPage.QuickAccess => quickAccessPage,
            _ => appearancePage,
        };

        appearanceContent.PreferencesChanged += (_, preferences) =>
        {
            _appearancePreferences = WriterAppearanceCompatibility.Normalize(preferences);
            ApplyAppearance(_appearancePreferences, dialog);
        };
        appearanceContent.ApplyRequested += (_, _) =>
        {
            PersistSettings();
            rollbackAppearance = _appearancePreferences;
            rollbackRibbon = RibbonCustomizationSerializer.Serialize(MainRibbon);
        };

        bool accepted = false;
        dialog.Applied += (_, _) =>
        {
            accepted = true;
            PersistSettings();
        };

        _settingsDialog = dialog;
        try
        {
            dialog.ShowDialog();
        }
        finally
        {
            _settingsDialog = null;
        }

        if (accepted)
            return;

        _suppressRibbonPersistence = true;
        try
        {
            RibbonCustomizationSerializer.Apply(MainRibbon, rollbackRibbon);
            _appearancePreferences = rollbackAppearance;
            ApplyAppearance(_appearancePreferences);
        }
        finally
        {
            _suppressRibbonPersistence = false;
        }
    }

    private void PersistSettings()
    {
        _writerSettings.SaveAppearance(_appearancePreferences);
        _writerSettings.SaveRibbonLayout(RibbonCustomizationSerializer.Serialize(MainRibbon));
    }

    private void ApplyAppearance(
        WriterAppearancePreferences preferences,
        RibbonOptionsDialog? openDialog = null)
    {
        preferences = WriterAppearanceCompatibility.Normalize(preferences);
        _appearancePreferences = preferences;

        ThemeManager.Apply(Application.Current, preferences.Theme);
        ThemeManager.SetDarkMode(Application.Current, preferences.DarkPalette);
        if (preferences.Accent is { } accent
            && ColorConverter.ConvertFromString(accent) is Color color)
        {
            ThemeManager.SetAccent(Application.Current, color);
        }
        else
        {
            ThemeManager.ClearAccent(Application.Current);
        }

        ThemeManager.SetAccentedTitleBar(Application.Current, preferences.AccentedTitleBar);
        FrameAppearance = preferences.FrameAppearance;
        MainRibbon.ApplicationButtonShape = preferences.ApplicationButtonShape;
        QueueWriterOrbTemplate();
        WriterBackstage.Design = preferences.BackstageDesign;

        RibbonAnimation.GlobalLevel = preferences.AnimationLevel;
        RibbonAnimation.RespectSystemReduceMotion = preferences.RespectSystemReducedMotion;

        _rulerVisible = preferences.ShowRuler;
        _marginGuidesVisible = preferences.ShowMarginGuides;
        if (HorizontalRuler is not null)
        {
            ApplyRulerVisibility();
            UpdateViewButtons();
        }

        ApplyBackdrop(preferences);
        WriterBackstage.Translucent = preferences.BackstageTranslucent
            && WriterAppearanceCompatibility.CanUseBackstageTranslucency(
                preferences,
                ActiveBackdrop != RibbonBackdrop.None);

        bool dark = preferences.DarkPalette && ThemeManager.SupportsDarkMode(preferences.Theme);
        MicaHelper.TrySetDarkMode(this, dark);
        if (openDialog is not null)
            MicaHelper.TrySetDarkMode(openDialog, dark);

        // WriterRuler draws brushes obtained from theme resources manually. Theme/accent dictionary
        // replacement therefore needs an explicit redraw for preview, Apply and Cancel rollback.
        HorizontalRuler?.RefreshAppearance();
    }

    private void ApplyBackdrop(WriterAppearancePreferences preferences)
    {
        RibbonBackdrop requested = WriterAppearanceCompatibility.ResolveBackdrop(
            preferences,
            MicaHelper.IsSupported);
        bool applied = requested != RibbonBackdrop.None
            && MicaHelper.TrySetBackdrop(this, requested);

        if (!applied)
        {
            MicaHelper.TrySetBackdrop(this, RibbonBackdrop.None);
            MicaHelper.ShowNativeCaptionButtons(this, true);
            ThemeManager.SetTitleBarBackdrop(Application.Current, false);
            SetResourceReference(BackgroundProperty, "RibbonKit.Brushes.Window.Background");
            WriterContentRoot.SetResourceReference(
                Panel.BackgroundProperty,
                "RibbonKit.Brushes.Window.Background");
            ApplyWorkspaceBackground(
                isBackdropActive: false,
                darkPalette: preferences.DarkPalette,
                theme: preferences.Theme);
            return;
        }

        MicaHelper.ExtendGlassFrame(this, full: true);
        MicaHelper.ShowNativeCaptionButtons(this, false);
        ThemeManager.SetTitleBarBackdrop(Application.Current, true);
        Background = Brushes.Transparent;
        WriterContentRoot.Background = Brushes.Transparent;
        ApplyWorkspaceBackground(
            isBackdropActive: true,
            darkPalette: preferences.DarkPalette,
            theme: preferences.Theme);
    }

    private void ApplyWorkspaceBackground(
        bool isBackdropActive,
        bool darkPalette,
        RibbonTheme theme)
    {
        Brush background;
        if (isBackdropActive)
        {
            background = Brushes.Transparent;
        }
        else if (SystemParameters.HighContrast)
        {
            background = SystemColors.ControlBrush;
        }
        else
        {
            var color = darkPalette
                ? Color.FromRgb(0x34, 0x34, 0x34)
                : Color.FromRgb(0xE7, 0xE7, 0xE7);
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            background = brush;
        }

        DocumentPresentationHost.Background = background;
        EditorSurface.Background = background;
        EditorViewport.Background = background;
        HorizontalRuler.IsSurfaceTransparent = ShouldUseTransparentRulerSurface(
            theme,
            isBackdropActive,
            SystemParameters.HighContrast);
    }

    internal static bool ShouldUseTransparentRulerSurface(
        RibbonTheme theme,
        bool isBackdropActive,
        bool highContrast) =>
        theme == RibbonTheme.Office2024 && isBackdropActive && !highContrast;

    private void DisposeWriterSettings()
    {
        Loaded -= OnWriterWindowLoaded;
        MainRibbon.QuickAccessCustomizeRequested -= OnQuickAccessCustomizeRequested;
        MainRibbon.RibbonCustomizeRequested -= OnRibbonCustomizeRequested;
        ((INotifyCollectionChanged)MainRibbon.QuickAccessItems).CollectionChanged -=
            OnQuickAccessItemsChanged;
        if (_quickAccessPositionDescriptor is not null)
        {
            _quickAccessPositionDescriptor.RemoveValueChanged(
                MainRibbon,
                OnQuickAccessPositionChanged);
            _quickAccessPositionDescriptor = null;
        }
    }

    private enum WriterSettingsPage
    {
        Appearance,
        CustomizeRibbon,
        QuickAccess,
    }
}
