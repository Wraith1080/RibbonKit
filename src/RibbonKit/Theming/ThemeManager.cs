using System.Windows;

namespace RibbonKit.Theming;

/// <summary>The Office theme generations RibbonKit ships.</summary>
public enum RibbonTheme
{
    /// <summary>The modern Office look (light). Default theme.</summary>
    Office2024,

    /// <summary>Office 2019 (colorful): blue tab strip, white tab text, flat chrome.</summary>
    Office2019,

    /// <summary>Office 2013 ("White"): flat, white tab strip, blue title bar, solid File button.</summary>
    Office2013,

    // Office2010 and Office2007 arrive in later Phase 6 batches (see docs/03-ROADMAP.md).
}

/// <summary>
/// Applies a RibbonKit theme at runtime by swapping the active token dictionary in
/// <see cref="Application.Resources"/>. The shared control templates reference tokens
/// via <c>DynamicResource</c>, so replacing the token dictionary re-colors every
/// control instantly — no template is duplicated per theme.
/// </summary>
/// <remarks>
/// A token dictionary must be present for controls to render correctly. Either merge
/// one in App.xaml (for example <c>Themes/Tokens.Office2024.xaml</c>) or call
/// <see cref="Apply"/> once at startup.
/// </remarks>
public static class ThemeManager
{
    private const string TokenMarkerKey = "RibbonKit.Brushes.Accent";

    private static ResourceDictionary? _current;

    /// <summary>The theme most recently applied via <see cref="Apply"/>, if any.</summary>
    public static RibbonTheme? CurrentTheme { get; private set; }

    /// <summary>
    /// Applies <paramref name="theme"/> application-wide, replacing any token
    /// dictionary previously applied by this manager (and, on first call, any token
    /// dictionary merged manually in App.xaml).
    /// </summary>
    public static void Apply(Application application, RibbonTheme theme)
    {
        ArgumentNullException.ThrowIfNull(application);

        var dictionary = new ResourceDictionary
        {
            Source = new Uri(
                $"pack://application:,,,/RibbonKit;component/Themes/Tokens.{theme}.xaml",
                UriKind.Absolute),
        };

        // Remove the dictionary we added last time...
        if (_current is not null)
        {
            application.Resources.MergedDictionaries.Remove(_current);
        }
        else
        {
            // ...or, on the first call, remove whatever token dictionary the app
            // merged manually (identified by a known token key) so we don't stack two.
            RemoveExistingTokenDictionaries(application);
        }

        application.Resources.MergedDictionaries.Add(dictionary);
        _current = dictionary;
        CurrentTheme = theme;
    }

    private static void RemoveExistingTokenDictionaries(Application application)
    {
        for (int i = application.Resources.MergedDictionaries.Count - 1; i >= 0; i--)
        {
            if (application.Resources.MergedDictionaries[i].Contains(TokenMarkerKey))
            {
                application.Resources.MergedDictionaries.RemoveAt(i);
            }
        }
    }
}
