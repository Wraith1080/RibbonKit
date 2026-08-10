using System.Text.Json;
using System.Text.Json.Serialization;
using RibbonKit.Controls;
using RibbonKit.Theming;

namespace RibbonKit.Showcase;

internal enum ShowcaseFileSurface
{
    Backstage,
    ApplicationMenu,
}

internal enum ShowcaseBackdropPreference
{
    None,
    Mica,
    Acrylic,
}

/// <summary>
/// App-owned appearance preferences for the Showcase. These deliberately remain separate from
/// RibbonCustomizationSerializer: importing or resetting structural ribbon customization must not
/// change the application's palette, File surface, or operating-system backdrop.
/// </summary>
internal sealed record ShowcaseAppearancePreferences
{
    internal const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; init; } = CurrentSchemaVersion;

    public RibbonTheme Theme { get; init; } = RibbonTheme.Office2024;

    /// <summary>A canonical #AARRGGBB color, or null to use the selected theme's default.</summary>
    public string? Accent { get; init; }

    public bool DarkMode { get; init; }

    public bool AccentedTitleBar { get; init; }

    public RibbonBackstageDesign BackstageDesign { get; init; } = RibbonBackstageDesign.Modern;

    public bool BackstageTranslucent { get; init; }

    public ShowcaseFileSurface FileSurface { get; init; } = ShowcaseFileSurface.Backstage;

    /// <summary>
    /// The requested material, not merely the last successfully applied material. This lets a
    /// preference survive a launch on a Windows build where the requested DWM backdrop is absent.
    /// </summary>
    public ShowcaseBackdropPreference Backdrop { get; init; }
}

internal static class ShowcaseAppearancePreferencesSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public static string Serialize(ShowcaseAppearancePreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);

        ShowcaseAppearancePreferences normalized = preferences with
        {
            SchemaVersion = ShowcaseAppearancePreferences.CurrentSchemaVersion,
            Accent = NormalizeAccent(preferences.Accent),
        };

        return JsonSerializer.Serialize(normalized, Options);
    }

    public static bool TryDeserialize(string? json, out ShowcaseAppearancePreferences preferences)
    {
        preferences = new ShowcaseAppearancePreferences();
        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        ShowcaseAppearancePreferences? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<ShowcaseAppearancePreferences>(json, Options);
        }
        catch (JsonException)
        {
            return false;
        }

        if (parsed is null
            || parsed.SchemaVersion != ShowcaseAppearancePreferences.CurrentSchemaVersion
            || !Enum.IsDefined(parsed.Theme)
            || !Enum.IsDefined(parsed.BackstageDesign)
            || !Enum.IsDefined(parsed.FileSurface)
            || !Enum.IsDefined(parsed.Backdrop))
        {
            return false;
        }

        string? accent = NormalizeAccent(parsed.Accent);
        if (parsed.Accent is not null && accent is null)
        {
            return false;
        }

        preferences = parsed with { Accent = accent };
        return true;
    }

    internal static string? NormalizeAccent(string? value)
    {
        if (value is null)
        {
            return null;
        }

        string candidate = value.Trim();
        if (candidate.Length == 7 && candidate[0] == '#')
        {
            candidate = "#FF" + candidate[1..];
        }

        if (candidate.Length != 9 || candidate[0] != '#')
        {
            return null;
        }

        for (int i = 1; i < candidate.Length; i++)
        {
            if (!Uri.IsHexDigit(candidate[i]))
            {
                return null;
            }
        }

        return candidate.ToUpperInvariant();
    }
}
