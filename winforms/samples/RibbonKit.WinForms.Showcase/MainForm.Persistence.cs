using RibbonKit.WinForms.Showcase.Persistence;

namespace RibbonKit.WinForms.Showcase;

public partial class MainForm
{
    internal static string DefaultSettingsDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RibbonKit", "WinForms.Showcase");
    private ShowcaseSettingsFile? _customizationFile, _preferencesFile;
    private bool _restoringPreferences = true, _profileSaveQueued;
    private ShowcaseWindowPlacement? _startupPlacement;
    private Rectangle _normalWindowBounds;
    private int _normalWindowDpi = 96;
    private FormWindowState _lastWindowState;
    private string? _persistenceWarning;

    private void InitializePersistence(string? directory)
    {
        if (directory is null) return; // Existing capture/measurement commands never touch user settings.
        _customizationFile = new(Path.Combine(directory, "ribbon-customization.json"));
        _preferencesFile = new(Path.Combine(directory, "preferences.json"));
        var level = ribbon.AnimationLevel;
        ribbon.AnimationLevel = RibbonAnimationLevel.None;
        try
        {
            ReportPersistence(_customizationFile.Load(json =>
            {
                using var session = ribbon.CommandCatalog.BeginCustomization(CustomizationApplicationId);
                session.ImportJson(json);
                if (!session.Apply()) throw new InvalidOperationException("Ribbon input blocked startup restore.");
            }));
            ReportPersistence(_preferencesFile.Load(json =>
            {
                var preferences = ShowcasePreferences.FromJson(json);
                ribbon.LayoutMode = preferences.LayoutMode;
                ribbon.Density = preferences.Density;
                ribbon.IsMinimized = preferences.IsMinimized;
                ribbon.TabContentAnimation = preferences.TabContentAnimation;
                document.WordWrap = preferences.WordWrap;
                Backdrop = preferences.Backdrop;
                ribbon.Backstage.NavigationBackgroundOpacity = preferences.TransparentBackstage ? 0 : 1;
                _startupPlacement = preferences.Window;
                level = preferences.AnimationLevel;
            }));
        }
        finally { ribbon.AnimationLevel = level; _syncAnimationChoices?.Invoke(); }
        if (_startupPlacement is { } placement)
        {
            StartPosition = FormStartPosition.Manual;
            // Select the target monitor before creating the HWND and its DPI context.
            Location = placement.Fit(Screen.FromRectangle(placement.Bounds).WorkingArea, MinimumSize, placement.Dpi).Location;
        }
        ribbon.DensityChanged += (_, _) => SavePreferences();
        ribbon.LayoutModeChanged += (_, _) => SavePreferences();
        ribbon.IsMinimizedChanged += (_, _) => SavePreferences();
        BackdropChanged += (_, _) => SavePreferences();
        ribbon.Backstage.NavigationBackgroundOpacityChanged += (_, _) => SavePreferences();
        ribbon.QuickAccessPositionChanged += (_, _) => QueueCustomizationSave();
        ribbon.QuickAccessVisibleChanged += (_, _) => QueueCustomizationSave();
        Move += (_, _) => TrackWindowPlacement();
        Resize += (_, _) => TrackWindowPlacement();
        ResizeEnd += (_, _) => SavePreferences();
        Shown += (_, _) => { if (_persistenceWarning is { } warning) status.Text = warning; };
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        if (_startupPlacement is { } placement)
        {
            Bounds = placement.Fit(Screen.FromRectangle(placement.Bounds).WorkingArea, MinimumSize, DeviceDpi);
            _normalWindowBounds = Bounds; _normalWindowDpi = DeviceDpi;
            if (placement.Maximized) WindowState = FormWindowState.Maximized;
        }
        else { _normalWindowBounds = Bounds; _normalWindowDpi = DeviceDpi; }
        _restoringPreferences = false;
        TrackWindowPlacement();
    }

    private void TrackWindowPlacement()
    {
        if (_restoringPreferences) return;
        if (WindowState != FormWindowState.Minimized) _lastWindowState = WindowState;
        if (WindowState == FormWindowState.Normal) { _normalWindowBounds = Bounds; _normalWindowDpi = DeviceDpi; }
    }

    private ShowcasePreferences CapturePreferences() => new()
    {
        LayoutMode = ribbon.LayoutMode, Density = ribbon.Density, IsMinimized = ribbon.IsMinimized,
        AnimationLevel = ribbon.AnimationLevel, TabContentAnimation = ribbon.TabContentAnimation, WordWrap = document.WordWrap,
        Backdrop = Backdrop, TransparentBackstage = ribbon.Backstage.NavigationBackgroundOpacity < 1,
        Window = new(_normalWindowBounds.X, _normalWindowBounds.Y, _normalWindowBounds.Width, _normalWindowBounds.Height,
            _normalWindowDpi, _lastWindowState == FormWindowState.Maximized)
    };

    private void SavePreferences()
    {
        if (_restoringPreferences || _preferencesFile is null) return;
        TrackWindowPlacement();
        ReportPersistence(_preferencesFile.Save(CapturePreferences().ToJson()));
    }

    private void QueueCustomizationSave()
    {
        if (_restoringPreferences || _customizationFile is null || _profileSaveQueued || !IsHandleCreated) return;
        _profileSaveQueued = true;
        // QAT property events can occur inside an Apply transaction. Capture only
        // after it finishes, including rollback; never persist a partially applied draft.
        BeginInvoke(() => { _profileSaveQueued = false; if (!IsDisposed) SaveCustomization(); });
    }

    private void SaveCustomization()
    {
        if (_restoringPreferences || _customizationFile is null) return;
        try
        {
            using var live = ribbon.CommandCatalog.BeginCustomization(CustomizationApplicationId);
            ReportPersistence(_customizationFile.Save(live.ExportJson()));
        }
        catch (Exception error) when (error is InvalidOperationException or ArgumentException)
        { ReportPersistence("Customization could not be saved: " + error.Message); }
    }

    private void ReportPersistence(string? warning)
    {
        if (warning is not null) { _persistenceWarning = warning; status.Text = warning; }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);
        if (!e.Cancel) { SaveCustomization(); SavePreferences(); }
    }
}
