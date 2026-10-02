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
    private bool _hasInjectedSettings;
    private WriterAppearanceScope? _appearanceScope;
    private readonly Dictionary<Window, WriterAppearanceScope> _dialogAppearanceScopes = new();
    private readonly Dictionary<Control, CapturedBackdrop> _capturedPopups = new();

    private void InitializeWriterSettings()
    {
        // Real Writer startup always has App.Current. Isolated window tests intentionally do not;
        // keep them hermetic rather than reading or writing the interactive user's settings.
        if (Application.Current is null && !_hasInjectedSettings)
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
        ApplyCapturedPopups(_appearancePreferences.CapturedPopupBackdrop && !SystemParameters.HighContrast);
    }

    private void OnQuickAccessItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        ApplyCapturedPopups(_appearancePreferences.CapturedPopupBackdrop && !SystemParameters.HighContrast);
        SaveRibbonLayoutIfReady();
    }

    private void OnQuickAccessPositionChanged(object? sender, EventArgs e) =>
        SaveRibbonLayoutIfReady();

    private void SaveDocumentViewPreferences()
    {
        _appearancePreferences = _appearancePreferences with
        {
            ShowRuler = _rulerVisible,
            ShowMarginGuides = _marginGuidesVisible,
        };
        if (_settingsDialog is null && (Application.Current is not null || _hasInjectedSettings))
            _writerSettings.SaveAppearance(_appearancePreferences);
    }

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

    internal void OpenSettingsDialog(WriterSettingsPage selected)
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
        RegisterAppearanceDialog(dialog);
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
        ApplyCapturedPopups(_appearancePreferences.CapturedPopupBackdrop && !SystemParameters.HighContrast);
        _writerSettings.SaveAppearance(_appearancePreferences);
        _writerSettings.SaveRibbonLayout(RibbonCustomizationSerializer.Serialize(MainRibbon));
    }

    internal void ApplyAppearance(
        WriterAppearancePreferences preferences,
        RibbonOptionsDialog? openDialog = null)
    {
        preferences = WriterAppearanceCompatibility.Normalize(preferences);
        _appearancePreferences = preferences;

        if (Application.Current is { } application)
        {
            ThemeManager.Apply(application, preferences.Theme);
            ThemeManager.SetDarkMode(application, preferences.DarkPalette);
            if (preferences.Accent is { } accent
                && ColorConverter.ConvertFromString(accent) is Color color)
            {
                ThemeManager.SetAccent(application, color);
            }
            else
            {
                ThemeManager.ClearAccent(application);
            }

            ThemeManager.SetAccentedTitleBar(application, preferences.AccentedTitleBar);
        }
        FrameAppearance = preferences.FrameAppearance;
        MainRibbon.ApplicationButtonShape = preferences.ApplicationButtonShape;
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
        _appearanceScope ??= new WriterAppearanceScope(this);
        _appearanceScope.Apply(preferences, ActiveBackdrop != RibbonBackdrop.None);
        foreach (var (dialog, scope) in _dialogAppearanceScopes)
        {
            scope.Apply(preferences);
            MicaHelper.TrySetDarkMode(dialog, preferences.DarkPalette);
        }
        ApplyCapturedPopups(preferences.CapturedPopupBackdrop && !SystemParameters.HighContrast);
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
            if (Application.Current is { } app) ThemeManager.SetTitleBarBackdrop(app, false);
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
        if (Application.Current is { } application) ThemeManager.SetTitleBarBackdrop(application, true);
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
        else if (theme == RibbonTheme.CrystalLight)
        {
            // Reveal the same window gradient behind both the ribbon and page,
            // including live light/dark palette and tint changes.
            background = Brushes.Transparent;
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
        PreviewView.Background = background;
        PreviewView.Viewer.Background = background;
        // Paper needs a steady outline rather than the ribbon's reflective rim.
        PaperCanvas.SetResourceReference(
            Border.BorderBrushProperty,
            theme == RibbonTheme.CrystalLight && !SystemParameters.HighContrast
                ? "RibbonKit.Brushes.MdiChild.InactiveBorder"
                : "RibbonKit.Brushes.Ribbon.Border");
        HorizontalRuler.UseCrystalGlassEdge = theme == RibbonTheme.CrystalLight;
        HorizontalRuler.IsSurfaceTransparent = ShouldUseTransparentRulerSurface(
            theme,
            isBackdropActive,
            SystemParameters.HighContrast);
    }

    internal static bool ShouldUseTransparentRulerSurface(
        RibbonTheme theme,
        bool isBackdropActive,
        bool highContrast) =>
        theme is RibbonTheme.Office2024 or RibbonTheme.CrystalLight && isBackdropActive && !highContrast;

    internal void RegisterAppearanceDialog(Window dialog)
    {
        if (_dialogAppearanceScopes.ContainsKey(dialog)) return;
        var scope = new WriterAppearanceScope(dialog);
        _dialogAppearanceScopes.Add(dialog, scope);
        scope.Apply(_appearancePreferences);
        MicaHelper.TrySetDarkMode(dialog, _appearancePreferences.DarkPalette);
        dialog.Closed += OnAppearanceDialogClosed;
    }

    private void OnAppearanceDialogClosed(object? sender, EventArgs e)
    {
        if (sender is not Window dialog) return;
        dialog.Closed -= OnAppearanceDialogClosed;
        if (_dialogAppearanceScopes.Remove(dialog, out var scope)) scope.Dispose();
    }

    private void ApplyCapturedPopups(bool enabled)
    {
        if (!enabled)
        {
            foreach (var capture in _capturedPopups.Values) capture.Dispose();
            _capturedPopups.Clear();
            return;
        }
        // Writer has Backstage rather than a RibbonApplicationMenu. Register only
        // supported dropdown/split buttons and collapsed groups, never Backstage.
        var controls = MainRibbon.Tabs.SelectMany(tab => tab.Groups)
            .SelectMany(group => new Control[] { group }.Concat(FindLogicalDescendants<Control>(group)))
            .Concat(MainRibbon.QuickAccessItems.OfType<Control>())
            .Where(control => control is RibbonDropDownButton or RibbonGroup).ToHashSet();
        foreach (Control obsolete in _capturedPopups.Keys.Except(controls).ToArray())
        {
            _capturedPopups[obsolete].Dispose();
            _capturedPopups.Remove(obsolete);
        }
        foreach (Control control in controls)
        {
            if (!_capturedPopups.TryGetValue(control, out var capture))
                _capturedPopups.Add(control, capture = new CapturedBackdrop(control, this));
            capture.Apply(true);
        }
    }

    private void DisposeWriterSettings()
    {
        ApplyCapturedPopups(false);
        _appearanceScope?.Dispose();
        foreach (var (dialog, scope) in _dialogAppearanceScopes)
        {
            dialog.Closed -= OnAppearanceDialogClosed;
            scope.Dispose();
        }
        _dialogAppearanceScopes.Clear();
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

    internal enum WriterSettingsPage
    {
        Appearance,
        CustomizeRibbon,
        QuickAccess,
    }
}
