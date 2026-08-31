using System.Text.Json;
using System.Text.Json.Serialization;
using RibbonKit;
using RibbonKit.Animation;
using RibbonKit.Controls;
using RibbonKit.Interop;
using RibbonKit.Theming;

namespace RibbonKit.Writer.Appearance;

internal sealed record WriterAppearancePreferences
{
    internal const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; init; } = CurrentSchemaVersion;

    public RibbonTheme Theme { get; init; } = RibbonTheme.Office2024;

    public bool DarkPalette { get; init; }

    /// <summary>A canonical #AARRGGBB color, or null for the theme default.</summary>
    public string? Accent { get; init; }

    public bool AccentedTitleBar { get; init; }

    public RibbonBackstageDesign BackstageDesign { get; init; } = RibbonBackstageDesign.Modern;

    public bool BackstageTranslucent { get; init; }

    public RibbonBackdrop Backdrop { get; init; } = RibbonBackdrop.None;

    public RibbonWindowFrameAppearance FrameAppearance { get; init; }

    public RibbonApplicationButtonShape ApplicationButtonShape { get; init; } =
        RibbonApplicationButtonShape.Tab;

    public RibbonAnimationLevel AnimationLevel { get; init; } = RibbonAnimationLevel.Subtle;

    public bool RespectSystemReducedMotion { get; init; } = true;

    public bool ShowRuler { get; init; } = true;

    public bool ShowMarginGuides { get; init; } = true;
}

internal static class WriterAppearancePreferencesSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public static string Serialize(WriterAppearancePreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        WriterAppearancePreferences normalized = WriterAppearanceCompatibility.Normalize(preferences) with
        {
            SchemaVersion = WriterAppearancePreferences.CurrentSchemaVersion,
        };
        return JsonSerializer.Serialize(normalized, Options);
    }

    public static bool TryDeserialize(string? json, out WriterAppearancePreferences preferences)
    {
        preferences = new WriterAppearancePreferences();
        if (string.IsNullOrWhiteSpace(json))
            return false;

        WriterAppearancePreferences? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<WriterAppearancePreferences>(json, Options);
        }
        catch (JsonException)
        {
            return false;
        }

        if (parsed is null
            || parsed.SchemaVersion != WriterAppearancePreferences.CurrentSchemaVersion
            || !Enum.IsDefined(parsed.Theme)
            || !Enum.IsDefined(parsed.BackstageDesign)
            || !Enum.IsDefined(parsed.Backdrop)
            || !Enum.IsDefined(parsed.FrameAppearance)
            || !Enum.IsDefined(parsed.ApplicationButtonShape)
            || !Enum.IsDefined(parsed.AnimationLevel))
        {
            return false;
        }

        string? accent = WriterAppearanceCompatibility.NormalizeAccent(parsed.Accent);
        if (parsed.Accent is not null && accent is null)
            return false;

        preferences = WriterAppearanceCompatibility.Normalize(parsed with
        {
            SchemaVersion = WriterAppearancePreferences.CurrentSchemaVersion,
            Accent = accent,
        });
        return true;
    }
}

internal static class WriterAppearanceCompatibility
{
    public static WriterAppearancePreferences Normalize(WriterAppearancePreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);

        RibbonWindowFrameAppearance frame = IsFrameSupported(
            preferences.Theme,
            preferences.FrameAppearance)
                ? preferences.FrameAppearance
                : RibbonWindowFrameAppearance.Default;
        RibbonApplicationButtonShape button = preferences.Theme == RibbonTheme.Office2007
            ? RibbonApplicationButtonShape.Orb
            : RibbonApplicationButtonShape.Tab;
        RibbonBackstageDesign backstage = IsBackstageDesignSupported(
            preferences.Theme,
            preferences.BackstageDesign)
                ? preferences.BackstageDesign
                : DefaultBackstageDesign(preferences.Theme);
        RibbonBackdrop backdrop = IsBackdropCompatible(frame, preferences.Backdrop)
            ? preferences.Backdrop
            : RibbonBackdrop.None;

        return preferences with
        {
            Accent = NormalizeAccent(preferences.Accent),
            FrameAppearance = frame,
            ApplicationButtonShape = button,
            Backdrop = backdrop,
            BackstageTranslucent = backdrop != RibbonBackdrop.None
                && backstage != RibbonBackstageDesign.Classic2007
                && preferences.BackstageTranslucent,
            BackstageDesign = backstage,
            AccentedTitleBar = frame == RibbonWindowFrameAppearance.Default
                && preferences.AccentedTitleBar,
        };
    }

    public static RibbonBackstageDesign DefaultBackstageDesign(RibbonTheme theme) => theme switch
    {
        RibbonTheme.Office2024 => RibbonBackstageDesign.Modern,
        RibbonTheme.Office2019 or RibbonTheme.Office2013 => RibbonBackstageDesign.Classic,
        RibbonTheme.Office2010 or RibbonTheme.Office2007 => RibbonBackstageDesign.Classic2010,
        _ => RibbonBackstageDesign.Modern,
    };

    public static bool IsBackstageDesignSupported(RibbonTheme theme, RibbonBackstageDesign design) =>
        design switch
        {
            RibbonBackstageDesign.Modern or RibbonBackstageDesign.Classic => true,
            RibbonBackstageDesign.Classic2010 =>
                theme is RibbonTheme.Office2010 or RibbonTheme.Office2007,
            RibbonBackstageDesign.Glass2007 or RibbonBackstageDesign.Classic2007 =>
                theme == RibbonTheme.Office2007,
            _ => false,
        };

    public static bool IsFrameSupported(RibbonTheme theme, RibbonWindowFrameAppearance frame) =>
        frame switch
        {
            RibbonWindowFrameAppearance.Default => true,
            RibbonWindowFrameAppearance.Office2007Aero => theme == RibbonTheme.Office2007,
            RibbonWindowFrameAppearance.Office2010Aero => theme == RibbonTheme.Office2010,
            _ => false,
        };

    public static bool IsBackdropCompatible(
        RibbonWindowFrameAppearance frame,
        RibbonBackdrop backdrop) =>
        frame == RibbonWindowFrameAppearance.Default
        || backdrop is RibbonBackdrop.None or RibbonBackdrop.Acrylic;

    public static RibbonBackdrop ResolveBackdrop(
        WriterAppearancePreferences preferences,
        bool systemBackdropSupported)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        return systemBackdropSupported
               && IsBackdropCompatible(preferences.FrameAppearance, preferences.Backdrop)
            ? preferences.Backdrop
            : RibbonBackdrop.None;
    }

    public static bool CanUseBackstageTranslucency(
        WriterAppearancePreferences preferences,
        bool systemBackdropSupported) =>
        ResolveBackdrop(preferences, systemBackdropSupported) != RibbonBackdrop.None
        && preferences.BackstageDesign != RibbonBackstageDesign.Classic2007;

    internal static string? NormalizeAccent(string? value)
    {
        if (value is null)
            return null;

        string candidate = value.Trim();
        if (candidate.Length == 7 && candidate[0] == '#')
            candidate = "#FF" + candidate[1..];

        if (candidate.Length != 9 || candidate[0] != '#')
            return null;

        for (int i = 1; i < candidate.Length; i++)
        {
            if (!Uri.IsHexDigit(candidate[i]))
                return null;
        }

        return candidate.ToUpperInvariant();
    }
}
