using System.Windows;

namespace RibbonKit.Theming;

/// <summary>The Office theme generations RibbonKit ships.</summary>
public enum RibbonTheme
{
    /// <summary>The modern Office look (light). Default theme.</summary>
    Office2024,

    // Office2019, Office2013, Office2010, and Office2007 arrive in Phase 6
    // (see docs/03-ROADMAP.md).
}

/// <summary>
/// Applies a RibbonKit theme to an application at runtime by swapping the theme
/// resource dictionary. Without an explicit call, controls use Office 2024 via
/// their default styles.
/// </summary>
public static class ThemeManager
{
    private static ResourceDictionary? _current;

    /// <summary>The theme most recently applied via <see cref="Apply"/>, if any.</summary>
    public static RibbonTheme? CurrentTheme { get; private set; }

    /// <summary>
    /// Applies <paramref name="theme"/> application-wide, replacing any previously
    /// applied RibbonKit theme dictionary.
    /// </summary>
    public static void Apply(Application application, RibbonTheme theme)
    {
        ArgumentNullException.ThrowIfNull(application);

        var dictionary = new ResourceDictionary
        {
            Source = new Uri(
                $"pack://application:,,,/RibbonKit;component/Themes/{theme}.xaml",
                UriKind.Absolute),
        };

        if (_current is not null)
        {
            application.Resources.MergedDictionaries.Remove(_current);
        }

        application.Resources.MergedDictionaries.Add(dictionary);
        _current = dictionary;
        CurrentTheme = theme;
    }
}
