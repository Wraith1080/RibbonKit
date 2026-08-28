using System.Collections.ObjectModel;
using System.Windows.Media;

namespace RibbonKit.Writer.Editing;

/// <summary>Identifies the text property to which a Writer colour action applies.</summary>
public enum WriterColorTarget
{
    /// <summary>Text foreground colour; null means Automatic.</summary>
    Foreground,

    /// <summary>Text highlight colour; null means No Color.</summary>
    Highlight
}

/// <summary>Identifies the source or semantic role of one Writer colour entry.</summary>
public enum WriterColorEntryKind
{
    /// <summary>One of the app-owned theme colours.</summary>
    Theme,

    /// <summary>One of the app-owned standard colours.</summary>
    Standard,

    /// <summary>A colour retained in the bounded recent-colour list.</summary>
    Recent,

    /// <summary>A custom colour returned by a colour dialog.</summary>
    Custom,

    /// <summary>The foreground default, which delegates to the document's automatic colour.</summary>
    Automatic,

    /// <summary>The highlight default, which removes highlighting.</summary>
    NoColor
}

/// <summary>Immutable app-owned colour metadata used by Writer galleries and dialogs.</summary>
/// <remarks>
/// Palette entries expose <see cref="Color"/> values only. They do not own WPF brushes or theme
/// resources, so a ribbon control may render a brush appropriate to its current visual theme while
/// the selected document value remains an ordinary solid colour.
/// </remarks>
public sealed record WriterColorEntry
{
    /// <summary>Creates a colour entry.</summary>
    /// <param name="key">Stable app-owned identity.</param>
    /// <param name="displayName">User-facing label.</param>
    /// <param name="color">Solid colour, or null for Automatic/No Color.</param>
    /// <param name="kind">Palette section or special semantic kind.</param>
    public WriterColorEntry(
        string key,
        string displayName,
        Color? color,
        WriterColorEntryKind kind)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("A colour entry must have a key.", nameof(key));
        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("A colour entry must have a display name.", nameof(displayName));

        var requiresColor = kind is WriterColorEntryKind.Theme or
            WriterColorEntryKind.Standard or
            WriterColorEntryKind.Recent or
            WriterColorEntryKind.Custom;
        if (requiresColor && !color.HasValue)
            throw new ArgumentException("Palette colours must contain a Color value.", nameof(color));
        if (!requiresColor && color.HasValue)
            throw new ArgumentException("Automatic and No Color entries cannot contain a Color value.", nameof(color));

        Key = key.Trim();
        DisplayName = displayName.Trim();
        Color = color;
        Kind = kind;
    }

    /// <summary>Gets the stable app-owned identity.</summary>
    public string Key { get; }

    /// <summary>Gets the user-facing label.</summary>
    public string DisplayName { get; }

    /// <summary>Gets the solid colour, or null for Automatic and No Color.</summary>
    public Color? Color { get; }

    /// <summary>Gets the entry's palette or semantic kind.</summary>
    public WriterColorEntryKind Kind { get; }

    /// <summary>Gets whether this entry represents the foreground Automatic action.</summary>
    public bool IsAutomatic => Kind == WriterColorEntryKind.Automatic;

    /// <summary>Gets whether this entry represents the highlight No Color action.</summary>
    public bool IsNoColor => Kind == WriterColorEntryKind.NoColor;

    /// <summary>Gets the immutable Automatic foreground entry.</summary>
    public static WriterColorEntry Automatic { get; } =
        new("automatic", "Automatic", null, WriterColorEntryKind.Automatic);

    /// <summary>Gets the immutable No Color highlight entry.</summary>
    public static WriterColorEntry NoColor { get; } =
        new("no-color", "No Color", null, WriterColorEntryKind.NoColor);
}

/// <summary>
/// Owns Writer's immutable theme/standard colour entries and mutable recent/last-used state.
/// </summary>
public sealed class WriterColorPalette
{
    /// <summary>The default maximum number of recent colours retained by one palette.</summary>
    public const int DefaultRecentLimit = 8;

    private static readonly IReadOnlyList<WriterColorEntry> DefaultTheme =
        Array.Empty<WriterColorEntry>();

    private static readonly IReadOnlyList<WriterColorEntry> DefaultStandard =
        CreateReadOnlyEntries(
            new WriterColorEntry("standard-black", "Black", Color.FromRgb(0x00, 0x00, 0x00), WriterColorEntryKind.Standard),
            new WriterColorEntry("standard-gray", "Gray", Color.FromRgb(0x99, 0x99, 0x99), WriterColorEntryKind.Standard),
            new WriterColorEntry("standard-white", "White", Color.FromRgb(0xFF, 0xFF, 0xFF), WriterColorEntryKind.Standard),
            new WriterColorEntry("standard-red", "Red", Color.FromRgb(0xFF, 0x00, 0x00), WriterColorEntryKind.Standard),
            new WriterColorEntry("standard-orange", "Orange", Color.FromRgb(0xF7, 0x96, 0x46), WriterColorEntryKind.Standard),
            new WriterColorEntry("standard-yellow", "Yellow", Color.FromRgb(0xFF, 0xFF, 0x00), WriterColorEntryKind.Standard),
            new WriterColorEntry("standard-green", "Green", Color.FromRgb(0x00, 0xB0, 0x50), WriterColorEntryKind.Standard),
            new WriterColorEntry("standard-cyan", "Cyan", Color.FromRgb(0x00, 0xB0, 0xF0), WriterColorEntryKind.Standard),
            new WriterColorEntry("standard-blue", "Blue", Color.FromRgb(0x00, 0x70, 0xC0), WriterColorEntryKind.Standard),
            new WriterColorEntry("standard-purple", "Purple", Color.FromRgb(0x70, 0x30, 0xA0), WriterColorEntryKind.Standard));

    private readonly IReadOnlyList<WriterColorEntry> _themeColors;
    private readonly IReadOnlyList<WriterColorEntry> _standardColors;
    private readonly List<Color> _recentColors = [];
    private readonly int _recentLimit;
    private WriterColorEntry? _lastUsedForeground;
    private WriterColorEntry? _lastUsedHighlight;

    /// <summary>Creates the default Writer theme and standard palette.</summary>
    public WriterColorPalette(int recentLimit = DefaultRecentLimit)
        : this(DefaultTheme, DefaultStandard, recentLimit)
    {
    }

    /// <summary>Creates a palette with injectable immutable base entries for deterministic tests.</summary>
    /// <param name="themeColors">Theme entries; each must have <see cref="WriterColorEntryKind.Theme"/> kind.</param>
    /// <param name="standardColors">Standard entries; each must have <see cref="WriterColorEntryKind.Standard"/> kind.</param>
    /// <param name="recentLimit">Maximum recent-colour count; must be positive.</param>
    public WriterColorPalette(
        IEnumerable<WriterColorEntry> themeColors,
        IEnumerable<WriterColorEntry> standardColors,
        int recentLimit = DefaultRecentLimit)
    {
        ArgumentNullException.ThrowIfNull(themeColors);
        ArgumentNullException.ThrowIfNull(standardColors);
        if (recentLimit <= 0)
            throw new ArgumentOutOfRangeException(nameof(recentLimit), "The recent limit must be positive.");

        _themeColors = NormalizeBaseColors(themeColors, WriterColorEntryKind.Theme, nameof(themeColors));
        _standardColors = NormalizeBaseColors(standardColors, WriterColorEntryKind.Standard, nameof(standardColors));
        _recentLimit = recentLimit;
    }

    /// <summary>Gets the immutable default theme entries.</summary>
    public static IReadOnlyList<WriterColorEntry> DefaultThemeColors => DefaultTheme;

    /// <summary>Gets the immutable default standard entries.</summary>
    public static IReadOnlyList<WriterColorEntry> DefaultStandardColors => DefaultStandard;

    /// <summary>Gets the immutable theme entries used by this palette.</summary>
    public IReadOnlyList<WriterColorEntry> ThemeColors => _themeColors;

    /// <summary>Gets the immutable standard entries used by this palette.</summary>
    public IReadOnlyList<WriterColorEntry> StandardColors => _standardColors;

    /// <summary>Gets the configured recent-colour bound.</summary>
    public int RecentLimit => _recentLimit;

    /// <summary>Gets recent colours, most recent first, without duplicates.</summary>
    public IReadOnlyList<Color> RecentColors =>
        new ReadOnlyCollection<Color>(_recentColors.ToArray());

    /// <summary>Gets the last foreground colour, or null when the primary action is Automatic.</summary>
    public Color? LastUsedForegroundColor => _lastUsedForeground?.Color;

    /// <summary>Gets the last highlight colour, or null when the primary action is No Color.</summary>
    public Color? LastUsedHighlightColor => _lastUsedHighlight?.Color;

    /// <summary>Gets the primary foreground action, defaulting to Automatic.</summary>
    public WriterColorEntry ForegroundPrimaryAction => _lastUsedForeground ?? WriterColorEntry.Automatic;

    /// <summary>Gets the primary highlight action, defaulting to No Color.</summary>
    public WriterColorEntry HighlightPrimaryAction => _lastUsedHighlight ?? WriterColorEntry.NoColor;

    /// <summary>Returns the complete duplicate-free gallery for the selected target.</summary>
    /// <remarks>The target-specific Automatic or No Color entry is first.</remarks>
    public IReadOnlyList<WriterColorEntry> GetEntries(WriterColorTarget target)
    {
        EnsureTarget(target);
        var entries = new List<WriterColorEntry>
        {
            target == WriterColorTarget.Foreground ? WriterColorEntry.Automatic : WriterColorEntry.NoColor
        };
        var colors = new HashSet<Color>();

        AddBaseEntries(_themeColors, entries, colors);
        AddBaseEntries(_standardColors, entries, colors);
        foreach (var recent in _recentColors)
        {
            if (colors.Add(recent))
            {
                entries.Add(new WriterColorEntry(
                    CreateColorKey(recent),
                    $"Recent {recent}",
                    recent,
                    WriterColorEntryKind.Recent));
            }
        }

        return new ReadOnlyCollection<WriterColorEntry>(entries);
    }

    /// <summary>Adds a colour to the bounded recent list, moving duplicates to the front.</summary>
    public bool RememberRecent(Color color)
    {
        _recentColors.RemoveAll(existing => existing == color);
        _recentColors.Insert(0, color);
        if (_recentColors.Count > _recentLimit)
            _recentColors.RemoveRange(_recentLimit, _recentColors.Count - _recentLimit);
        return true;
    }

    /// <summary>
    /// Sets the last-used colour for a target. Null restores Automatic for foreground or No Color
    /// for highlight and does not add a recent entry.
    /// </summary>
    public bool SetLastUsed(WriterColorTarget target, Color? color)
    {
        EnsureTarget(target);
        if (!color.HasValue)
        {
            SetLastUsedEntry(target, null);
            return true;
        }

        RememberRecent(color.Value);
        var entry = FindBaseEntry(color.Value) ?? new WriterColorEntry(
            CreateColorKey(color.Value),
            $"Custom {color.Value}",
            color.Value,
            WriterColorEntryKind.Custom);
        SetLastUsedEntry(target, entry);
        return true;
    }

    /// <summary>Gets the currently selected primary colour value, or null for the target default.</summary>
    public Color? GetPrimaryColor(WriterColorTarget target)
    {
        EnsureTarget(target);
        return target == WriterColorTarget.Foreground
            ? LastUsedForegroundColor
            : LastUsedHighlightColor;
    }

    private void SetLastUsedEntry(WriterColorTarget target, WriterColorEntry? entry)
    {
        if (target == WriterColorTarget.Foreground)
            _lastUsedForeground = entry;
        else
            _lastUsedHighlight = entry;
    }

    private WriterColorEntry? FindBaseEntry(Color color)
    {
        return _themeColors.Concat(_standardColors)
            .FirstOrDefault(entry => entry.Color.HasValue && entry.Color.Value == color);
    }

    private static void AddBaseEntries(
        IEnumerable<WriterColorEntry> source,
        ICollection<WriterColorEntry> entries,
        ISet<Color> colors)
    {
        foreach (var entry in source)
        {
            if (entry.Color.HasValue && colors.Add(entry.Color.Value))
                entries.Add(entry);
        }
    }

    private static IReadOnlyList<WriterColorEntry> NormalizeBaseColors(
        IEnumerable<WriterColorEntry> source,
        WriterColorEntryKind expectedKind,
        string parameterName)
    {
        try
        {
            var entries = new List<WriterColorEntry>();
            var colors = new HashSet<Color>();
            foreach (var entry in source)
            {
                if (entry is null)
                    continue;
                if (entry.Kind != expectedKind || !entry.Color.HasValue)
                    throw new ArgumentException(
                        $"All entries in {parameterName} must be {expectedKind} colours.",
                        parameterName);
                if (colors.Add(entry.Color.Value))
                    entries.Add(entry);
            }

            return new ReadOnlyCollection<WriterColorEntry>(entries.ToArray());
        }
        catch (ArgumentException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new ArgumentException("The palette entries could not be enumerated.", parameterName, exception);
        }
    }

    private static IReadOnlyList<WriterColorEntry> CreateReadOnlyEntries(params WriterColorEntry[] entries)
    {
        return new ReadOnlyCollection<WriterColorEntry>(entries);
    }

    private static string CreateColorKey(Color color)
    {
        return $"color-{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    private static void EnsureTarget(WriterColorTarget target)
    {
        if (!Enum.IsDefined(target))
            throw new ArgumentOutOfRangeException(nameof(target));
    }
}
