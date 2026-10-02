using System.Windows;
using System.Windows.Media;
using RibbonKit.Theming;

namespace RibbonKit.Writer.Appearance;

/// <summary>Owns only Writer's optional palette and glass dictionaries in one resource scope.</summary>
internal sealed class WriterAppearanceScope : IDisposable
{
    private readonly FrameworkElement _owner;
    private ResourceDictionary? _palette;
    private ResourceDictionary? _glass;

    internal WriterAppearanceScope(FrameworkElement owner)
    {
        _owner = owner;
        if (owner.Resources.Source is not null)
        {
            var original = owner.Resources;
            owner.Resources = new ResourceDictionary();
            owner.Resources.MergedDictionaries.Add(original);
        }
    }

    internal void Apply(WriterAppearancePreferences preferences, bool nativeBackdrop = false)
    {
        Dispose();
        if (preferences.Theme == RibbonTheme.CrystalLight || Application.Current is null)
        {
            Color? accent = preferences.Accent is { } value
                ? (Color)ColorConverter.ConvertFromString(value) : null;
            _palette = ThemeManager.CreatePalette(preferences.Theme, accent, preferences.DarkPalette);
            // The host's native/title-bar policy follows ThemeManager; the material palette
            // must not hide its explicit accented or transparent caption.
            if (Application.Current is { } app && (preferences.AccentedTitleBar || nativeBackdrop))
                foreach (string key in new[] { "RibbonKit.Brushes.TitleBar.Background", "RibbonKit.Brushes.TitleBar.Foreground" })
                    _palette[key] = app.FindResource(key);
            _owner.Resources.MergedDictionaries.Add(_palette);
        }
        if (preferences.GlassSurfaces && !SystemParameters.HighContrast)
        {
            _glass = ThemeManager.CreateGlassOverlay(_owner, preferences.DarkPalette);
            _owner.Resources.MergedDictionaries.Add(_glass);
        }
    }

    public void Dispose()
    {
        if (_glass is not null) _owner.Resources.MergedDictionaries.Remove(_glass);
        if (_palette is not null) _owner.Resources.MergedDictionaries.Remove(_palette);
        _glass = null;
        _palette = null;
    }
}

internal static class WriterDialogAppearance
{
    // Owner relationships do not provide resource inheritance to a detached Window.
    internal static void Initialize(Window dialog) => dialog.Loaded += OnLoaded;

    private static void OnLoaded(object sender, RoutedEventArgs e)
    {
        var dialog = (Window)sender;
        dialog.Loaded -= OnLoaded;
        for (Window? owner = dialog.Owner; owner is not null; owner = owner.Owner)
            if (owner is MainWindow writer)
            {
                writer.RegisterAppearanceDialog(dialog);
                break;
            }
    }
}
