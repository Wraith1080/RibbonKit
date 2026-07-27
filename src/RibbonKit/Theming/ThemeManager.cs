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

    /// <summary>
    /// Office 2010 ("Blue"): the first non-flat theme — gradient silver-blue chrome, dark-blue
    /// tab labels, amber/gold glossy button highlights, a connected (outlined) selected tab, and
    /// a solid blue gradient File button.
    /// </summary>
    Office2010,

    // Office2007 arrives in a later Phase 6 batch (see docs/03-ROADMAP.md).
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
    private const string AppButtonPressedKey = "RibbonKit.Brushes.ApplicationButton.PressedBackground";
    private const string AppButtonBorderKey = "RibbonKit.Brushes.ApplicationButton.Border";
    private const string BackstageSelectedGlassKey = "RibbonKit.Brushes.Backstage.ItemSelectedGlass";
    private const string DialogPrimaryBackgroundKey = "RibbonKit.Brushes.Dialog.PrimaryBackground";
    private const string DialogPrimaryBorderKey = "RibbonKit.Brushes.Dialog.PrimaryBorder";
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
    private const string TabStripControlHoverKey = "RibbonKit.Brushes.TabStrip.ControlHoverBackground";

    // Every key the accent system may override, so a theme switch can clear the previous
    // theme's accent overrides before re-deriving for the new one.
    private static readonly string[] AccentOverrideKeys =
    {
        AccentKey, CheckedKey, CheckedHoverKey, BackstageHoverKey, BackstageSelectedKey,
        SelectedUnderlineKey, SelectedForegroundKey, AppButtonBackgroundKey, AppButtonHoverKey,
        AppButtonPressedKey, AppButtonBorderKey, BackstageSelectedGlassKey, DialogPrimaryBackgroundKey,
        DialogPrimaryBorderKey,
    };

    // Last-resort fallback when no theme Accent brush is resolvable. Office blue #2B579A —
    // the shared default of the 2024/2019/2013 token palettes (2024 was Fluent #0F6CBD until
    // its default was aligned with the older generations; keep this constant in step with
    // the token files if the default ever changes again).
    private static readonly Color DefaultAccent = Color.FromRgb(0x2B, 0x57, 0x9A);

    private static ResourceDictionary? _current;
    private static Color? _accent;
    private static bool _accentTitleBar;
    private static bool _titleBarBackdrop;

    /// <summary>The theme most recently applied via <see cref="Apply"/>, if any.</summary>
    public static RibbonTheme? CurrentTheme { get; private set; }

    /// <summary>Whether the accent title bar (<see cref="SetAccentedTitleBar"/>) is on.</summary>
    public static bool IsAccentedTitleBar => _accentTitleBar;

    /// <summary>Whether the transparent title bar (<see cref="SetTitleBarBackdrop"/>) is on.</summary>
    public static bool IsTitleBarBackdrop => _titleBarBackdrop;

    /// <summary>
    /// Raised whenever the theme, accent, or accent-title-bar configuration changes, so
    /// dependent visuals (e.g. the ribbon's quick-access icons) can re-evaluate.
    /// </summary>
    public static event EventHandler? Changed;

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
        Changed?.Invoke(null, EventArgs.Empty);
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
        Changed?.Invoke(null, EventArgs.Empty);
    }

    /// <summary>Clears a custom accent set via <see cref="SetAccent"/>, reverting to the
    /// active theme's own accent colors.</summary>
    public static void ClearAccent(Application application)
    {
        ArgumentNullException.ThrowIfNull(application);
        _accent = null;
        ApplyAccentOverrides(application);
        ApplyTitleBarOverride(application);
        Changed?.Invoke(null, EventArgs.Empty);
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
        // Re-establish the accent baseline first (it owns the File-button hover on 2013),
        // then layer the title-bar/strip colors on top.
        ApplyAccentOverrides(application);
        ApplyTitleBarOverride(application);
        Changed?.Invoke(null, EventArgs.Empty);
    }

    /// <summary>
    /// Toggles a transparent title bar so a window system backdrop (Mica/Acrylic) shows through
    /// it. The transparency is applied <b>only</b> for the Office 2024 look with a non-colored
    /// title bar; other themes keep their light band, and a colored title bar
    /// (<see cref="SetAccentedTitleBar"/>) keeps its accent — matching where a solid title bar is
    /// expected. Persists across <see cref="Apply"/>, so switching themes re-derives it correctly
    /// (a non-2024 theme reverts to its solid band instead of leaking the transparent override).
    /// The caller is still responsible for the actual DWM backdrop and glass frame (see
    /// <see cref="RibbonKit.Interop.MicaHelper"/>).
    /// </summary>
    public static void SetTitleBarBackdrop(Application application, bool enabled)
    {
        ArgumentNullException.ThrowIfNull(application);
        _titleBarBackdrop = enabled;
        ApplyTitleBarOverride(application);
        Changed?.Invoke(null, EventArgs.Empty);
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

        RibbonTheme theme = CurrentTheme ?? RibbonTheme.Office2024;

        // Colors shared by every theme.
        resources[AccentKey] = Frozen(accent);
        resources[BackstageHoverKey] = Frozen(Mix(accent, Colors.White, 0.22));
        resources[BackstageSelectedKey] = Frozen(Mix(accent, Colors.Black, 0.28));
        // The Classic2010 selected "glass" marker tracks the accent as a gel (only visible when
        // that backstage design is active, harmless otherwise). The dialog primary (OK) button
        // is flat accent by default; the Office 2010 case below swaps it for a glass gel.
        resources[BackstageSelectedGlassKey] = Gel(accent);
        resources[DialogPrimaryBackgroundKey] = Frozen(accent);
        resources[DialogPrimaryBorderKey] = Frozen(accent);

        // Toggled/checked highlight follows the accent — EXCEPT in Office 2010, where the
        // hover/press/toggle highlight is always the amber "hot" color regardless of the color
        // scheme (authentic 2010: the accent recolors the chrome, never the button highlight).
        // Leaving these unset keeps 2010's amber gradient (they were Removed above).
        if (theme != RibbonTheme.Office2010)
        {
            resources[CheckedKey] = Frozen(Mix(accent, Colors.White, 0.82));
            resources[CheckedHoverKey] = Frozen(Mix(accent, Colors.White, 0.72));
        }

        // Theme-specific accent tokens: only where that theme actually uses the accent,
        // so the flat themes keep their fill/outline selection untouched.
        switch (theme)
        {
            case RibbonTheme.Office2024:
                resources[SelectedUnderlineKey] = Frozen(accent);
                resources[SelectedForegroundKey] = Frozen(accent);
                break;
            case RibbonTheme.Office2013:
                resources[SelectedForegroundKey] = Frozen(accent);
                resources[AppButtonBackgroundKey] = Frozen(accent);
                resources[AppButtonHoverKey] = Frozen(Mix(accent, Colors.Black, 0.22));
                resources[AppButtonPressedKey] = Frozen(Mix(accent, Colors.Black, 0.38));
                break;
            case RibbonTheme.Office2010:
                // The File button tracks the accent — but as a GRADIENT (a smooth blue-style
                // gel in the accent hue) with a matching border, NOT a flat solid, so it keeps
                // the 2010 glass look when the accent changes. The connected selected tab keeps
                // its dark label (SelectedForeground left at the theme default).
                resources[AppButtonBackgroundKey] = Gel(accent);
                resources[AppButtonHoverKey] = Gel(Mix(accent, Colors.White, 0.18));
                resources[AppButtonPressedKey] = Gel(Mix(accent, Colors.Black, 0.22));
                resources[AppButtonBorderKey] = Frozen(Mix(accent, Colors.Black, 0.30));
                // The OK button borrows the same glass gel + border in 2010.
                resources[DialogPrimaryBackgroundKey] = Gel(accent);
                resources[DialogPrimaryBorderKey] = Frozen(Mix(accent, Colors.Black, 0.30));
                break;
        }
    }

    /// <summary>
    /// Builds a smooth 3-stop vertical "gel" gradient centered on <paramref name="baseColor"/>
    /// (lighter top, base middle, darker bottom) — the Office 2010 glossy-block look, derived so
    /// a custom accent keeps its gradient instead of flattening to a solid.
    /// </summary>
    private static LinearGradientBrush Gel(Color baseColor)
    {
        var brush = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(0, 1),
        };
        // Light top (inner glow), a slightly darker matte middle, then a LIGHTER bottom — the
        // small specular reflection of a 2010 glass button.
        brush.GradientStops.Add(new GradientStop(Mix(baseColor, Colors.White, 0.38), 0.0));
        brush.GradientStops.Add(new GradientStop(Mix(baseColor, Colors.Black, 0.10), 0.5));
        brush.GradientStops.Add(new GradientStop(Mix(baseColor, Colors.White, 0.20), 1.0));
        brush.Freeze();
        return brush;
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
        resources.Remove(TabStripControlHoverKey);
        // Note: ApplicationButton.HoverBackground is NOT cleared here — it is owned by the
        // accent system (which runs first and re-establishes its baseline); we only layer
        // an override onto it in the colored-strip branch below.

        // Transparent title bar so a window backdrop (Mica) shows through — but ONLY for the
        // Office 2024 look with a NON-colored title bar. A colored title bar falls through to
        // the accent branch below (opaque accent); any other theme with a non-colored title bar
        // returns with no override, keeping that theme's solid light band. The caption
        // foreground/hover tokens are intentionally left at their theme defaults (dark text, a
        // light hover) which read correctly over the material.
        if (_titleBarBackdrop
            && !_accentTitleBar
            && (CurrentTheme ?? RibbonTheme.Office2024) == RibbonTheme.Office2024)
        {
            resources[TitleBarBackgroundKey] = Brushes.Transparent;
            return;
        }

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
        // so the whole top reads as one accent band. The chrome buttons that sit on the
        // strip (File button, minimize toggle) get the same accent-tinted hover as the
        // tabs. Other themes keep a neutral strip.
        if (CurrentTheme == RibbonTheme.Office2019)
        {
            Color stripHover = Mix(accent, Colors.White, 0.18);
            resources[RibbonBackgroundKey] = Frozen(accent);
            resources[TabStripForegroundKey] = Frozen(Colors.White);
            resources[AppButtonForegroundKey] = Frozen(Colors.White);
            resources[TabHoverKey] = Frozen(stripHover);
            resources[TabStripControlHoverKey] = Frozen(stripHover);
            resources[AppButtonHoverKey] = Frozen(stripHover);
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
