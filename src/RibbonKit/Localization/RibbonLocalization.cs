using System.Globalization;
using System.Resources;

namespace RibbonKit.Localization;

/// <summary>Identifies a user-facing string supplied by RibbonKit itself.</summary>
/// <remarks>
/// Application content such as tab, group and command headers remains owned and localized by the
/// host application. These identifiers cover only chrome and commands created by RibbonKit.
/// </remarks>
public enum RibbonString
{
    /// <summary>"Add to Quick Access Toolbar".</summary>
    AddToQuickAccessToolbar,

    /// <summary>"Customize Quick Access Toolbar…".</summary>
    CustomizeQuickAccessToolbar,

    /// <summary>"Customize the Ribbon…".</summary>
    CustomizeRibbon,

    /// <summary>"Collapse the Ribbon".</summary>
    CollapseRibbon,

    /// <summary>"Show Quick Access Toolbar in the Title Bar".</summary>
    ShowQuickAccessToolbarInTitleBar,

    /// <summary>"Show Quick Access Toolbar Above the Ribbon".</summary>
    ShowQuickAccessToolbarAboveRibbon,

    /// <summary>"Show Quick Access Toolbar Below the Ribbon".</summary>
    ShowQuickAccessToolbarBelowRibbon,

    /// <summary>"Remove from Quick Access Toolbar".</summary>
    RemoveFromQuickAccessToolbar,
}

/// <summary>
/// Supplies application-specific overrides for RibbonKit's built-in localized strings.
/// </summary>
/// <remarks>
/// Return <see langword="null"/> for a string the provider does not override; RibbonKit then falls
/// back to its embedded <c>.resx</c> resource for the requested culture.
/// </remarks>
public interface IRibbonLocalizationProvider
{
    /// <summary>Returns an override for <paramref name="key"/>, or <see langword="null"/>.</summary>
    string? GetString(RibbonString key, CultureInfo culture);
}

/// <summary>
/// Resolves RibbonKit's built-in strings from embedded localization resources, with an optional
/// application-provided override layer.
/// </summary>
public static class RibbonLocalization
{
    private static readonly ResourceManager ResourceManager =
        new("RibbonKit.Resources.Strings", typeof(RibbonLocalization).Assembly);

    private static IRibbonLocalizationProvider? _provider;

    /// <summary>
    /// Gets or sets the application override provider. A provider may override only selected
    /// strings by returning <see langword="null"/> for all others. Set to <see langword="null"/>
    /// to use RibbonKit's embedded resources exclusively.
    /// </summary>
    public static IRibbonLocalizationProvider? Provider
    {
        get => Volatile.Read(ref _provider);
        set => Volatile.Write(ref _provider, value);
    }

    /// <summary>
    /// Resolves <paramref name="key"/> for <see cref="CultureInfo.CurrentUICulture"/>.
    /// </summary>
    public static string GetString(RibbonString key)
    {
        CultureInfo culture = CultureInfo.CurrentUICulture;
        string? overridden = Provider?.GetString(key, culture);
        if (overridden is not null)
        {
            return overridden;
        }

        return ResourceManager.GetString(key.ToString(), culture) ?? key.ToString();
    }
}
