using System.Collections.ObjectModel;
using System.Windows.Media;
using System.Windows.Threading;

namespace RibbonKit.Writer.Editing;

/// <summary>Identifies one font family item that can be displayed by the Writer font picker.</summary>
/// <remarks>
/// <see cref="FontFamily"/> is deliberately exposed separately from <see cref="DisplayName"/>.
/// A picker can bind the former to the item preview so each row renders in its own face while the
/// latter remains a stable, searchable label. The object is a snapshot; it does not own or mutate
/// the installed-font collection.
/// </remarks>
public sealed record WriterFontChoice
{
    /// <summary>Creates a font choice.</summary>
    /// <param name="fontFamily">The WPF font family used for selection and preview.</param>
    /// <param name="displayName">Optional label; the font family's source is used by default.</param>
    public WriterFontChoice(FontFamily fontFamily, string? displayName = null)
    {
        FontFamily = fontFamily ?? throw new ArgumentNullException(nameof(fontFamily));
        DisplayName = string.IsNullOrWhiteSpace(displayName)
            ? fontFamily.Source
            : displayName.Trim();

        if (string.IsNullOrWhiteSpace(DisplayName))
            throw new ArgumentException("A font choice must have a display name.", nameof(displayName));
    }

    /// <summary>Gets the WPF family used by a font preview and selection command.</summary>
    public FontFamily FontFamily { get; }

    /// <summary>Gets the user-facing and searchable display label.</summary>
    public string DisplayName { get; }

    /// <summary>Gets the stable family source used for case-insensitive identity.</summary>
    public string SourceName => FontFamily.Source;

    internal string Identity => SourceName;
}

/// <summary>A duplicate-free, immutable view of the font sections shown by the picker.</summary>
public sealed class WriterFontCatalogProjection
{
    internal WriterFontCatalogProjection(
        WriterFontChoice? current,
        IReadOnlyList<WriterFontChoice> recommended,
        IReadOnlyList<WriterFontChoice> recent,
        IReadOnlyList<WriterFontChoice> remainingInstalled,
        IReadOnlyList<WriterFontChoice> items)
    {
        Current = current;
        Recommended = recommended;
        Recent = recent;
        RemainingInstalled = remainingInstalled;
        Items = items;
    }

    /// <summary>Gets the current family, including one that is outside the short picker sections.</summary>
    public WriterFontChoice? Current { get; }

    /// <summary>Gets recommended families, excluding the current family.</summary>
    public IReadOnlyList<WriterFontChoice> Recommended { get; }

    /// <summary>Gets recent families, excluding current and recommended families.</summary>
    public IReadOnlyList<WriterFontChoice> Recent { get; }

    /// <summary>Gets installed families not already represented by another section.</summary>
    public IReadOnlyList<WriterFontChoice> RemainingInstalled { get; }

    /// <summary>
    /// Gets the display order: current, recommended, recent, then the remaining installed families.
    /// No family identity occurs more than once in this list.
    /// </summary>
    public IReadOnlyList<WriterFontChoice> Items { get; }
}

/// <summary>
/// Provides a cached, searchable and deterministic view of installed Writer font families.
/// </summary>
/// <remarks>
/// The system source is evaluated lazily on the calling thread. WPF font discovery is therefore
/// intentionally not performed by a background worker. Hosts that bind the result to WPF controls
/// can pass their UI <see cref="Dispatcher"/>; all cache, projection and recent-list operations then
/// verify dispatcher access and fail clearly instead of returning thread-affine state from an
/// arbitrary worker. Tests can inject a plain source and omit the dispatcher.
/// </remarks>
public sealed class WriterFontCatalog
{
    /// <summary>The default maximum number of recent families retained by one catalog.</summary>
    public const int DefaultRecentLimit = 8;

    private static readonly IReadOnlyList<string> DefaultRecommendedNames =
        CreateReadOnlyStrings("Aptos", "Calibri", "Cambria", "Segoe UI", "Arial", "Times New Roman");

    private static readonly IReadOnlyList<string> DefaultFallbackNames =
        CreateReadOnlyStrings("Arial", "Calibri", "Segoe UI", "Times New Roman");

    private readonly Func<IEnumerable<FontFamily>> _installedFontFamilySource;
    private readonly IReadOnlyList<WriterFontChoice> _fallbackFonts;
    private readonly IReadOnlyList<string> _recommendedFamilyNames;
    private readonly List<WriterFontChoice> _recentFonts = [];
    private readonly int _recentLimit;
    private readonly Dispatcher? _dispatcher;
    private IReadOnlyList<WriterFontChoice>? _installedFonts;

    /// <summary>Creates a catalog backed by the process's installed WPF font families.</summary>
    /// <param name="recentLimit">Maximum recent-family count; must be positive.</param>
    /// <param name="dispatcher">
    /// Optional UI dispatcher used to make WPF thread ownership explicit. When supplied, all
    /// catalog operations must run on it.
    /// </param>
    public WriterFontCatalog(int recentLimit = DefaultRecentLimit, Dispatcher? dispatcher = null)
        : this(static () => Fonts.SystemFontFamilies, recentLimit, dispatcher)
    {
    }

    /// <summary>Creates a catalog with an injectable installed-font source.</summary>
    /// <param name="installedFontFamilySource">
    /// Source evaluated on first access and on <see cref="RefreshInstalledFonts"/>. Its result is
    /// copied immediately, so the source may return a transient collection.
    /// </param>
    /// <param name="recentLimit">Maximum recent-family count; must be positive.</param>
    /// <param name="dispatcher">Optional dispatcher that owns the returned WPF font objects.</param>
    /// <param name="fallbackFonts">Optional choices used when the source fails or is empty.</param>
    /// <param name="recommendedFamilyNames">Optional default recommended-family order.</param>
    public WriterFontCatalog(
        Func<IEnumerable<FontFamily>> installedFontFamilySource,
        int recentLimit = DefaultRecentLimit,
        Dispatcher? dispatcher = null,
        IEnumerable<WriterFontChoice>? fallbackFonts = null,
        IEnumerable<string>? recommendedFamilyNames = null)
    {
        ArgumentNullException.ThrowIfNull(installedFontFamilySource);
        if (recentLimit <= 0)
            throw new ArgumentOutOfRangeException(nameof(recentLimit), "The recent limit must be positive.");

        _installedFontFamilySource = installedFontFamilySource;
        _recentLimit = recentLimit;
        _dispatcher = dispatcher;
        _fallbackFonts = NormalizeChoices(fallbackFonts ?? CreateFallbackFonts());
        _recommendedFamilyNames = NormalizeNames(recommendedFamilyNames ?? DefaultRecommendedNames);

        if (_fallbackFonts.Count == 0)
            throw new ArgumentException("At least one valid fallback font is required.", nameof(fallbackFonts));
    }

    /// <summary>Gets the dispatcher explicitly associated with this catalog, if any.</summary>
    public Dispatcher? Dispatcher => _dispatcher;

    /// <summary>
    /// Gets whether the current thread may use this catalog. A catalog without an explicit
    /// dispatcher is caller-thread based and always returns <see langword="true"/>.
    /// </summary>
    public bool IsOnRequiredDispatcher => _dispatcher is null || _dispatcher.CheckAccess();

    /// <summary>Gets the configured recent-family bound.</summary>
    public int RecentLimit => _recentLimit;

    /// <summary>Gets the deterministic default recommended-family names.</summary>
    public static IReadOnlyList<string> DefaultRecommendedFamilyNames => DefaultRecommendedNames;

    /// <summary>Gets the fallback names used when installed-font enumeration is unavailable.</summary>
    public static IReadOnlyList<string> DefaultFallbackFamilyNames => DefaultFallbackNames;

    /// <summary>Gets the cached installed families, loading them on first access.</summary>
    public IReadOnlyList<WriterFontChoice> InstalledFonts
    {
        get
        {
            VerifyDispatcherAccess();
            return _installedFonts ??= LoadInstalledFonts();
        }
    }

    /// <summary>Gets the current recent-family snapshot, most recent first.</summary>
    public IReadOnlyList<WriterFontChoice> RecentFonts
    {
        get
        {
            VerifyDispatcherAccess();
            return Snapshot(_recentFonts);
        }
    }

    /// <summary>Re-evaluates the source and replaces the installed-family cache.</summary>
    public IReadOnlyList<WriterFontChoice> RefreshInstalledFonts()
    {
        VerifyDispatcherAccess();
        _installedFonts = LoadInstalledFonts();
        return _installedFonts;
    }

    /// <summary>Searches cached installed families using a case-insensitive display-name match.</summary>
    /// <param name="query">Text to find; null or whitespace returns the full cached list.</param>
    public IReadOnlyList<WriterFontChoice> Search(string? query)
    {
        VerifyDispatcherAccess();
        if (string.IsNullOrWhiteSpace(query))
            return InstalledFonts;

        var text = query.Trim();
        return Snapshot(InstalledFonts.Where(choice =>
            choice.DisplayName.Contains(text, StringComparison.OrdinalIgnoreCase) ||
            choice.SourceName.Contains(text, StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>Remembers a family at the front of the bounded recent list.</summary>
    /// <returns><see langword="false"/> when the family cannot be identified.</returns>
    public bool RememberRecent(FontFamily? fontFamily, string? displayName = null)
    {
        VerifyDispatcherAccess();
        if (fontFamily is null)
            return false;

        return RememberRecent(new WriterFontChoice(fontFamily, displayName));
    }

    /// <summary>Remembers a font choice at the front of the bounded recent list.</summary>
    public bool RememberRecent(WriterFontChoice? choice)
    {
        VerifyDispatcherAccess();
        if (choice is null || string.IsNullOrWhiteSpace(choice.SourceName))
            return false;

        var canonical = FindInstalled(choice.SourceName) ?? choice;
        _recentFonts.RemoveAll(existing => IdentityEquals(existing.Identity, canonical.Identity));
        _recentFonts.Insert(0, canonical);
        if (_recentFonts.Count > _recentLimit)
            _recentFonts.RemoveRange(_recentLimit, _recentFonts.Count - _recentLimit);
        return true;
    }

    /// <summary>
    /// Creates a duplicate-free projection for a current family. A current family outside the
    /// installed list is retained as a valid current item rather than silently discarded.
    /// </summary>
    /// <param name="current">The family currently observed in the selection, if one exists.</param>
    /// <param name="recommendedFamilyNames">Optional per-call recommended ordering.</param>
    public WriterFontCatalogProjection CreateProjection(
        FontFamily? current = null,
        IEnumerable<string>? recommendedFamilyNames = null)
    {
        VerifyDispatcherAccess();
        var installed = InstalledFonts;
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        WriterFontChoice? currentChoice = null;
        if (current is not null && !string.IsNullOrWhiteSpace(current.Source))
        {
            currentChoice = FindInstalled(current.Source) ?? new WriterFontChoice(current);
            used.Add(currentChoice.Identity);
        }

        var recommended = new List<WriterFontChoice>();
        foreach (var name in NormalizeNames(recommendedFamilyNames ?? _recommendedFamilyNames))
        {
            var choice = FindInstalled(name);
            if (choice is not null && used.Add(choice.Identity))
                recommended.Add(choice);
        }

        var recent = new List<WriterFontChoice>();
        foreach (var recentChoice in _recentFonts)
        {
            var choice = FindInstalled(recentChoice.SourceName) ?? recentChoice;
            if (used.Add(choice.Identity))
                recent.Add(choice);
        }

        var remainingInstalled = installed
            .Where(choice => used.Add(choice.Identity))
            .ToArray();

        var items = new List<WriterFontChoice>(
            (currentChoice is null ? 0 : 1) + recommended.Count + recent.Count + remainingInstalled.Length);
        if (currentChoice is not null)
            items.Add(currentChoice);
        items.AddRange(recommended);
        items.AddRange(recent);
        items.AddRange(remainingInstalled);

        return new WriterFontCatalogProjection(
            currentChoice,
            Snapshot(recommended),
            Snapshot(recent),
            Snapshot(remainingInstalled),
            Snapshot(items));
    }

    private IReadOnlyList<WriterFontChoice> LoadInstalledFonts()
    {
        try
        {
            var loaded = NormalizeFamilies(_installedFontFamilySource());
            return loaded.Count == 0 ? _fallbackFonts : loaded;
        }
        catch (Exception)
        {
            // Font enumeration is an OS-facing convenience. A fallback keeps the editor's picker
            // usable when the WPF font collection is unavailable or fails during enumeration.
            return _fallbackFonts;
        }
    }

    private WriterFontChoice? FindInstalled(string sourceName)
    {
        return InstalledFonts.FirstOrDefault(choice => IdentityEquals(choice.Identity, sourceName));
    }

    private void VerifyDispatcherAccess()
    {
        if (_dispatcher is not null && !_dispatcher.CheckAccess())
            throw new InvalidOperationException(
                "WriterFontCatalog must be used on its associated WPF dispatcher thread.");
    }

    private static IReadOnlyList<WriterFontChoice> NormalizeFamilies(IEnumerable<FontFamily>? families)
    {
        if (families is null)
            return [];

        try
        {
            return NormalizeChoices(families
                .Where(family => family is not null)
                .Select(family => new WriterFontChoice(family)));
        }
        catch (Exception)
        {
            return [];
        }
    }

    private static IReadOnlyList<WriterFontChoice> NormalizeChoices(IEnumerable<WriterFontChoice>? choices)
    {
        if (choices is null)
            return [];

        try
        {
            return choices
                .Where(choice => choice is not null && !string.IsNullOrWhiteSpace(choice.SourceName))
                .OrderBy(choice => choice.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(choice => choice.DisplayName, StringComparer.Ordinal)
                .GroupBy(choice => choice.Identity, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .ToArray();
        }
        catch (Exception)
        {
            return [];
        }
    }

    private static IReadOnlyList<string> NormalizeNames(IEnumerable<string>? names)
    {
        if (names is null)
            return [];

        try
        {
            return names
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(name => name.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
        catch (Exception)
        {
            return [];
        }
    }

    private static IReadOnlyList<WriterFontChoice> CreateFallbackFonts()
    {
        var fallback = new List<WriterFontChoice>();
        foreach (var name in DefaultFallbackNames)
        {
            try
            {
                fallback.Add(new WriterFontChoice(new FontFamily(name)));
            }
            catch (Exception)
            {
                // Continue through the conservative fallback set. FontFamily construction should
                // normally succeed even when the requested face is not installed.
            }
        }

        return fallback;
    }

    private static IReadOnlyList<T> Snapshot<T>(IEnumerable<T> values)
    {
        return new ReadOnlyCollection<T>(values.ToArray());
    }

    private static IReadOnlyList<string> CreateReadOnlyStrings(params string[] values)
    {
        return new ReadOnlyCollection<string>(values);
    }

    private static bool IdentityEquals(string first, string second)
    {
        return StringComparer.OrdinalIgnoreCase.Equals(first, second);
    }
}
