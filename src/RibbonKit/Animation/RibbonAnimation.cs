using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media.Animation;

namespace RibbonKit.Animation;

/// <summary>
/// How strongly RibbonKit animates its transitions.
/// </summary>
public enum RibbonAnimationLevel
{
    /// <summary>No motion — every transition snaps to its final state instantly.</summary>
    None,

    /// <summary>Short, understated motion (the default): quick fades and small slides.</summary>
    Subtle,

    /// <summary>Longer, more pronounced motion with larger travel and springier easing.</summary>
    Expressive,
}

/// <summary>
/// The distinct ribbon transitions that can be animated. Each can override the global
/// <see cref="RibbonAnimation.GlobalLevel"/> individually.
/// </summary>
public enum RibbonAnimationAction
{
    /// <summary>Ribbon body collapsing / expanding when minimized or restored.</summary>
    RibbonMinimize,

    /// <summary>Backstage (File menu) opening and closing.</summary>
    Backstage,

    /// <summary>The selected-tab marker sliding from one tab to another.</summary>
    TabMarker,

    /// <summary>Tab content cross-fading when the active tab changes.</summary>
    TabSwitch,

    /// <summary>In-ribbon gallery expanding to / collapsing from its drop-down.</summary>
    Gallery,

    /// <summary>Scroll offset gliding instead of jumping: the ribbon's groups row / tab strip (chevron buttons or wheel) and the in-ribbon gallery (strip settling on a picked tile, and its up/down buttons).</summary>
    RibbonScroll,

    /// <summary>Drop-down, split-button, and group flyout menus opening and closing.</summary>
    DropdownMenu,

    /// <summary>Hover highlight fading in and out on buttons and items.</summary>
    Hover,

    /// <summary>Quick Access Toolbar cross-fading as it moves between hosts.</summary>
    QuickAccessMove,

    /// <summary>A contextual tab appearing/disappearing as its context activates.</summary>
    ContextualTab,

    /// <summary>KeyTip accelerator badges popping in when Alt is pressed.</summary>
    KeyTip,

    /// <summary>Toggle / checked state changing on a button.</summary>
    ToggleState,

    /// <summary>The whole ribbon cross-fading when the theme or accent changes.</summary>
    ThemeSwitch,
}

/// <summary>
/// Central, application-wide control of RibbonKit's motion. A single
/// <see cref="GlobalLevel"/> sets the baseline intensity (default <see cref="RibbonAnimationLevel.Subtle"/>),
/// and any <see cref="RibbonAnimationAction"/> can override it individually via
/// <see cref="SetActionLevel"/>.
/// </summary>
/// <remarks>
/// <para>
/// Motion is expressed through three per-action quantities — a <see cref="Duration"/>, an
/// <see cref="IEasingFunction"/>, and a slide offset (in DIPs) — each derived from the
/// action's <em>effective</em> level. Code-behind transitions read them via
/// <see cref="GetDuration"/>, <see cref="GetEase"/>, and <see cref="GetSlideOffset"/>.
/// Template storyboards read the same durations through <c>DynamicResource</c> tokens
/// (<c>RibbonKit.Animation.Duration.*</c>) that this class publishes into
/// <see cref="Application.Resources"/> once <see cref="Initialize"/> has been called.
/// </para>
/// <para>
/// When <see cref="RespectSystemReduceMotion"/> is <see langword="true"/> (the default) and
/// the OS "animate controls inside windows" setting is off, every action's effective level
/// drops to <see cref="RibbonAnimationLevel.None"/> regardless of configuration, so the
/// library honors a user's system-wide reduced-motion preference automatically.
/// </para>
/// <para>
/// All motion uses transform and opacity animation only (composited on the GPU); no
/// layout-affecting property (Width/Height/Margin) is ever animated, so transitions never
/// trigger a re-layout pass.
/// </para>
/// </remarks>
public static class RibbonAnimation
{
    private const string DurationKeyPrefix = "RibbonKit.Animation.Duration.";

    private static RibbonAnimationLevel _globalLevel = RibbonAnimationLevel.Subtle;
    private static bool _respectSystemReduceMotion = true;
    private static readonly Dictionary<RibbonAnimationAction, RibbonAnimationLevel> _overrides = new();
    private static Application? _application;

    /// <summary>
    /// Raised whenever the animation configuration changes (global level, an override, or
    /// the reduce-motion policy), so controls can re-read durations if they cache them.
    /// </summary>
    public static event EventHandler? Changed;

    /// <summary>
    /// The baseline intensity applied to every action that has no explicit override.
    /// Defaults to <see cref="RibbonAnimationLevel.Subtle"/>.
    /// </summary>
    public static RibbonAnimationLevel GlobalLevel
    {
        get => _globalLevel;
        set
        {
            if (_globalLevel == value)
            {
                return;
            }

            _globalLevel = value;
            Publish();
        }
    }

    /// <summary>
    /// When <see langword="true"/> (default), a system-wide reduced-motion preference
    /// (the OS "animate controls and elements inside windows" setting being off) forces
    /// every action's effective level to <see cref="RibbonAnimationLevel.None"/>.
    /// </summary>
    public static bool RespectSystemReduceMotion
    {
        get => _respectSystemReduceMotion;
        set
        {
            if (_respectSystemReduceMotion == value)
            {
                return;
            }

            _respectSystemReduceMotion = value;
            Publish();
        }
    }

    /// <summary>Whether the OS currently signals a reduced-motion preference.</summary>
    public static bool SystemReduceMotion => !SystemParameters.ClientAreaAnimation;

    /// <summary>
    /// Registers the application whose <see cref="Application.Resources"/> receive the
    /// <c>RibbonKit.Animation.Duration.*</c> tokens that template storyboards bind to, and
    /// seeds them for the current configuration. Call once at startup (order-independent
    /// with <see cref="Theming.ThemeManager.Apply"/>).
    /// </summary>
    public static void Initialize(Application application)
    {
        ArgumentNullException.ThrowIfNull(application);
        _application = application;
        SeedTokens(application);
    }

    /// <summary>Overrides the global level for a single action.</summary>
    public static void SetActionLevel(RibbonAnimationAction action, RibbonAnimationLevel level)
    {
        if (_overrides.TryGetValue(action, out RibbonAnimationLevel existing) && existing == level)
        {
            return;
        }

        _overrides[action] = level;
        Publish();
    }

    /// <summary>Removes a per-action override so the action follows <see cref="GlobalLevel"/> again.</summary>
    public static void ClearActionLevel(RibbonAnimationAction action)
    {
        if (_overrides.Remove(action))
        {
            Publish();
        }
    }

    /// <summary>The explicit override set for an action, or <see langword="null"/> if it follows the global level.</summary>
    public static RibbonAnimationLevel? GetActionOverride(RibbonAnimationAction action)
        => _overrides.TryGetValue(action, out RibbonAnimationLevel level) ? level : null;

    /// <summary>
    /// The level actually in effect for an action after applying its override (if any) and
    /// the system reduced-motion policy.
    /// </summary>
    public static RibbonAnimationLevel GetEffectiveLevel(RibbonAnimationAction action)
    {
        if (_respectSystemReduceMotion && SystemReduceMotion)
        {
            return RibbonAnimationLevel.None;
        }

        return _overrides.TryGetValue(action, out RibbonAnimationLevel level) ? level : _globalLevel;
    }

    /// <summary>Whether an action animates at all right now (its effective level is not None).</summary>
    public static bool IsEnabled(RibbonAnimationAction action)
        => GetEffectiveLevel(action) != RibbonAnimationLevel.None;

    /// <summary>The animation duration for an action at its effective level (zero when disabled).</summary>
    public static Duration GetDuration(RibbonAnimationAction action)
        => new(TimeSpan.FromMilliseconds(GetDurationMs(action)));

    /// <summary>The easing function for an action at its effective level.</summary>
    public static IEasingFunction GetEase(RibbonAnimationAction action)
    {
        switch (GetEffectiveLevel(action))
        {
            case RibbonAnimationLevel.Expressive:
                // A gentle overshoot gives expressive motion its "spring".
                return action is RibbonAnimationAction.TabMarker
                    or RibbonAnimationAction.QuickAccessMove
                    or RibbonAnimationAction.KeyTip
                    or RibbonAnimationAction.ToggleState
                    ? new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.35 }
                    : new QuinticEase { EasingMode = EasingMode.EaseOut };
            case RibbonAnimationLevel.Subtle:
                return SharedCubicOut;
            default:
                return SharedCubicOut;
        }
    }

    /// <summary>
    /// The slide travel distance (in DIPs) for an action at its effective level. Returns 0
    /// for actions that fade without sliding, or when the action is disabled.
    /// </summary>
    public static double GetSlideOffset(RibbonAnimationAction action)
    {
        RibbonAnimationLevel level = GetEffectiveLevel(action);
        if (level == RibbonAnimationLevel.None)
        {
            return 0d;
        }

        double baseOffset = action switch
        {
            RibbonAnimationAction.Backstage => 28d,
            RibbonAnimationAction.RibbonMinimize => 12d,
            RibbonAnimationAction.DropdownMenu => 8d,
            RibbonAnimationAction.Gallery => 10d,
            RibbonAnimationAction.ContextualTab => 6d,
            RibbonAnimationAction.QuickAccessMove => 6d,
            RibbonAnimationAction.TabSwitch => 10d,
            RibbonAnimationAction.KeyTip => 4d,
            _ => 0d,
        };

        return level == RibbonAnimationLevel.Expressive ? baseOffset * 1.8d : baseOffset;
    }

    private static readonly CubicEase SharedCubicOut = new() { EasingMode = EasingMode.EaseOut };

    private static double GetDurationMs(RibbonAnimationAction action)
    {
        RibbonAnimationLevel level = GetEffectiveLevel(action);
        if (level == RibbonAnimationLevel.None)
        {
            return 0d;
        }

        double subtle = action switch
        {
            RibbonAnimationAction.Hover => 90d,
            RibbonAnimationAction.DropdownMenu => 130d,
            RibbonAnimationAction.KeyTip => 120d,
            RibbonAnimationAction.ToggleState => 120d,
            RibbonAnimationAction.TabSwitch => 130d,
            RibbonAnimationAction.RibbonScroll => 160d,
            RibbonAnimationAction.QuickAccessMove => 150d,
            RibbonAnimationAction.ThemeSwitch => 160d,
            RibbonAnimationAction.RibbonMinimize => 180d,
            RibbonAnimationAction.Gallery => 180d,
            RibbonAnimationAction.TabMarker => 180d,
            RibbonAnimationAction.ContextualTab => 200d,
            RibbonAnimationAction.Backstage => 220d,
            _ => 150d,
        };

        return level == RibbonAnimationLevel.Expressive ? subtle * 1.4d : subtle;
    }

    private static void Publish()
    {
        if (_application is not null)
        {
            SeedTokens(_application);
        }

        Changed?.Invoke(null, EventArgs.Empty);
    }

    /// <summary>
    /// Publishes a <see cref="Duration"/> token per action into the application's resources
    /// so template storyboards can bind their <c>Duration</c> via <c>DynamicResource</c>.
    /// A disabled action publishes a zero duration, so its storyboard snaps instantly.
    /// </summary>
    private static void SeedTokens(Application application)
    {
        ResourceDictionary resources = application.Resources;
        foreach (RibbonAnimationAction action in Enum.GetValues<RibbonAnimationAction>())
        {
            resources[DurationKeyPrefix + action] = GetDuration(action);
        }
    }
}
