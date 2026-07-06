using System.Windows;
using System.Windows.Media;

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
/// <see cref="Apply"/> once at startup. A custom <see cref="SetAccent"/> and the
/// <see cref="SetAccentedTitleBar"/> toggle survive theme switches, re-deriving their
/// colors for whichever theme is active.
/// </remarks>
public static class ThemeManager
{
    private const string AccentKey = "RibbonKit.Brushes.Accent";
    private const string CheckedKey = "RibbonKit.Brushes.Control.CheckedBackground";
    private const string CheckedHoverKey = "RibbonKit.Brushes.Control.CheckedHoverBackground";
    private const string BackstageHoverKey = "RibbonKit.Brushes.Backstage.ItemHoverBackground";
    private const string BackstageSelectedKey = "RibbonKit.Brushes.Backstage.ItemSelectedBackground";
    private const string SelectedUnderlineKey = "RibbonKit.Brushes.Tab.SelectedUnderline";
    private const string SelectedForegroundKey = "RibbonKit.Brushes.Tab.SelectedForeground";
    private const string AppButtonBackgroundKey = "RibbonKit.Brushes.ApplicationButton.Background";
    private const string AppButtonHoverKey = "RibbonKit.Brushes.ApplicationButton.HoverBackground";
    private const string TitleBarBackgroundKey = "RibbonKit.Brushes.TitleBar.Background";
    private const string TitleBarForegroundKey = "RibbonKit.Brushes.TitleBar.Foreground";
    private const string CaptionHoverKey = "RibbonKit.Brushes.CaptionButton.HoverBackground";
    private const string CaptionPressedKey = "RibbonKit.Brushes.CaptionButton.PressedBackground";

    // "Colorful" themes (Office 2019) extend the accent title bar into the tab-strip band
    // behind the headers, so the title bar and strip read as one colored band.
    private const string RibbonBackgroundKey = "RibbonKit.Brushes.Ribbon.Background";
    private const string TabStripForegroundKey = "RibbonKit.Brushes.TabStrip.Foreground";
    private const string AppButtonForegroundKey = "RibbonKit.Brushes.ApplicationButton.Foreground";
    private const string TabHoverKey = "RibbonKit.Brushes.Tab.HoverBackground";

    // Every key the accent system may override, so a theme switch can clear the previous
    // theme's accent overrides before re-deriving for the new one.
    private static readonly string[] AccentOverrideKeys =
    {
        AccentKey, CheckedKey, CheckedHoverKey, BackstageHoverKey, BackstageSelectedKey,
        SelectedUnderlineKey, SelectedForegroundKey, AppButtonBackgroundKey, AppButtonHoverKey,
    };

    private static readonly Color DefaultAccent = Color.FromRgb(0x0F, 0x6C, 0xBD);

    private static ResourceDictionary? _current;
    private static Color? _accent;
    private static bool _accentTitleBar;

    /// <summary>The theme most recently applied via <see cref="Apply"/>, if any.</summary>
    public static RibbonTheme? CurrentTheme { get; private set; }

    /// <summary>
    /// Applies <paramref name="theme"/> application-wide, replacing any token
    /// dictionary previously applied by this manager (and, on first call, any token
    /// dictionary merged manually in App.xaml). Any custom accent (<see cref="SetAccent"/>)
    /// and the title-bar accent toggle are re-derived for the new theme.
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

        // Re-derive customizations for the freshly-applied theme.
        ApplyAccentOverrides(application);
        ApplyTitleBarOverride(application);
    }

    /// <summary>
    /// Sets the accent color used across the current theme — the selection highlight,
    /// toggled-button fills, backstage highlights, the 2024 selected-tab underline/text,
    /// and the 2013 File button. Persists across <see cref="Apply"/> calls, re-deriving
    /// per theme (for example, the flat 2019/2013 themes never gain a selection underline).
    /// </summary>
    public static void SetAccent(Application application, Color accent)
    {
        ArgumentNullException.ThrowIfNull(application);
        _accent = accent;
        ApplyAccentOverrides(application);
        ApplyTitleBarOverride(application); // an accented title bar tracks the accent
    }

    /// <summary>Clears a custom accent set via <see cref="SetAccent"/>, reverting to the
    /// active theme's own accent colors.</summary>
    public static void ClearAccent(Application application)
    {
        ArgumentNullException.ThrowIfNull(application);
        _accent = null;
        ApplyAccentOverrides(application);
        ApplyTitleBarOverride(application);
    }

    /// <summary>
    /// Toggles an accent-colored title bar (white caption text/glyphs over the accent),
    /// à la Office 2024's colored title bar option. Uses the custom accent when one is
    /// set, otherwise the current theme's accent. Persists across <see cref="Apply"/>.
    /// </summary>
    public static void SetAccentedTitleBar(Application application, bool enabled)
    {
        ArgumentNullException.ThrowIfNull(application);
        _accentTitleBar = enabled;
        ApplyTitleBarOverride(application);
    }

    private static void ApplyAccentOverrides(Application application)
    {
        ResourceDictionary resources = application.Resources;

        // Clear any prior accent overrides first so switching themes never leaks a
        // theme-specific accent token (e.g. a 2024 underline onto flat 2019).
        foreach (string key in AccentOverrideKeys)
        {
            resources.Remove(key);
        }

        if (_accent is not Color accent)
        {
            return;
        }

        // Colors shared by every theme.
        resources[AccentKey] = Frozen(accent);
        resources[CheckedKey] = Frozen(Mix(accent, Colors.White, 0.82));
        resources[CheckedHoverKey] = Frozen(Mix(accent, Colors.White, 0.72));
        resources[BackstageHoverKey] = Frozen(Mix(accent, Colors.White, 0.22));
        resources[BackstageSelectedKey] = Frozen(Mix(accent, Colors.Black, 0.28));

        // Theme-specific accent tokens: only where that theme actually uses the accent,
        // so the flat themes keep their fill/outline selection untouched.
        switch (CurrentTheme ?? RibbonTheme.Office2024)
        {
            case RibbonTheme.Office2024:
                resources[SelectedUnderlineKey] = Frozen(accent);
                resources[SelectedForegroundKey] = Frozen(accent);
                break;
            case RibbonTheme.Office2013:
                resources[SelectedForegroundKey] = Frozen(accent);
                resources[AppButtonBackgroundKey] = Frozen(accent);
                resources[AppButtonHoverKey] = Frozen(Mix(accent, Colors.Black, 0.22));
                break;
        }
    }

    private static void ApplyTitleBarOverride(Application application)
    {
        ResourceDictionary resources = application.Resources;

        // Always clear first, then re-derive — so toggling off, or switching to a theme
        // whose strip shouldn't be colored, never leaks a previous colored-band override.
        resources.Remove(TitleBarBackgroundKey);
        resources.Remove(TitleBarForegroundKey);
        resources.Remove(CaptionHoverKey);
        resources.Remove(CaptionPressedKey);
        resources.Remove(RibbonBackgroundKey);
        resources.Remove(TabStripForegroundKey);
        resources.Remove(AppButtonForegroundKey);
        resources.Remove(TabHoverKey);

        if (!_accentTitleBar)
        {
            return;
        }

        Color accent = EffectiveAccent(application);
        resources[TitleBarBackgroundKey] = Frozen(accent);
        resources[TitleBarForegroundKey] = Frozen(Colors.White);
        resources[CaptionHoverKey] = Frozen(Mix(accent, Colors.White, 0.20));
        resources[CaptionPressedKey] = Frozen(Mix(accent, Colors.Black, 0.15));

        // Office 2019's tab-strip band tracks the title bar: color it (and its text) too,
        // so the whole top reads as one accent band. Other themes keep a neutral strip.
        if (CurrentTheme == RibbonTheme.Office2019)
        {
            resources[RibbonBackgroundKey] = Frozen(accent);
            resources[TabStripForegroundKey] = Frozen(Colors.White);
            resources[AppButtonForegroundKey] = Frozen(Colors.White);
            resources[TabHoverKey] = Frozen(Mix(accent, Colors.White, 0.18));
        }
    }

    private static Color EffectiveAccent(Application application)
    {
        if (_accent is Color accent)
        {
            return accent;
        }

        return application.Resources[AccentKey] is SolidColorBrush brush ? brush.Color : DefaultAccent;
    }

    private static SolidColorBrush Frozen(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

    private static Color Mix(Color a, Color b, double t)
    {
        t = Math.Clamp(t, 0.0, 1.0);
        byte Channel(byte x, byte y) => (byte)Math.Round(x + ((y - x) * t));
        return Color.FromArgb(255, Channel(a.R, b.R), Channel(a.G, b.G), Channel(a.B, b.B));
    }

    private static void RemoveExistingTokenDictionaries(Application application)
    {
        for (int i = application.Resources.MergedDictionaries.Count - 1; i >= 0; i--)
        {
            if (application.Resources.MergedDictionaries[i].Contains(AccentKey))
            {
                application.Resources.MergedDictionaries.RemoveAt(i);
            }
        }
    }
}
