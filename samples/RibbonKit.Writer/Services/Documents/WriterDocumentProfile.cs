using System.IO;
using System.Windows.Documents;
using RibbonKit.Writer.Models;
using RibbonKit.Writer.Services.Persistence;

namespace RibbonKit.Writer.Services.Documents;

/// <summary>Commands that a Writer document profile can expose while preserving its identity.</summary>
[Flags]
public enum WriterDocumentCommandCapabilities
{
    /// <summary>No profile-specific commands.</summary>
    None = 0,

    /// <summary>Insert and edit character content.</summary>
    TextEditing = 1 << 0,

    /// <summary>Use the native clipboard commands.</summary>
    Clipboard = 1 << 1,

    /// <summary>Use native undo and redo.</summary>
    UndoRedo = 1 << 2,

    /// <summary>Search and replace character content.</summary>
    FindReplace = 1 << 3,

    /// <summary>Use native spelling support.</summary>
    SpellCheck = 1 << 4,

    /// <summary>Apply character-level formatting.</summary>
    CharacterFormatting = 1 << 5,

    /// <summary>Apply paragraph-level formatting.</summary>
    ParagraphFormatting = 1 << 6,

    /// <summary>Edit and persist Writer page settings.</summary>
    PageSettings = 1 << 7,

    /// <summary>Open the Writer preview presentation.</summary>
    Preview = 1 << 8,

    /// <summary>Print the current document.</summary>
    Printing = 1 << 9
}

/// <summary>Content features that a Writer document profile can preserve.</summary>
[Flags]
public enum WriterDocumentContentCapabilities
{
    /// <summary>No content feature.</summary>
    None = 0,

    /// <summary>Plain character content.</summary>
    Text = 1 << 0,

    /// <summary>Character-level formatting.</summary>
    CharacterFormatting = 1 << 1,

    /// <summary>Paragraph-level formatting.</summary>
    ParagraphFormatting = 1 << 2,

    /// <summary>Images embedded in the document.</summary>
    Images = 1 << 3,

    /// <summary>Hyperlinks embedded in the document.</summary>
    Hyperlinks = 1 << 4,

    /// <summary>Native FlowDocument tables.</summary>
    Tables = 1 << 5
}

/// <summary>Page metadata that a Writer document profile can preserve.</summary>
[Flags]
public enum WriterDocumentPageMetadataCapabilities
{
    /// <summary>No page metadata.</summary>
    None = 0,

    /// <summary>Paper size, orientation and margins.</summary>
    PageSettings = 1 << 0
}

/// <summary>
/// The capability matrix for one Writer creation profile. Persistence facts are sourced from
/// <see cref="WriterPersistenceCapabilities"/> so the profile and serializer cannot advertise different fidelity.
/// </summary>
public sealed record WriterDocumentProfileCapabilities
{
    /// <summary>Creates a profile capability matrix.</summary>
    public WriterDocumentProfileCapabilities(
        WriterPersistenceCapabilities persistence,
        WriterDocumentCommandCapabilities commands,
        WriterDocumentContentCapabilities content,
        WriterDocumentPageMetadataCapabilities pageMetadata)
    {
        Persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));
        var expectedContent = GetContentCapabilities(Persistence);
        var expectedPageMetadata = GetPageMetadataCapabilities(Persistence);
        if (content != expectedContent)
            throw new ArgumentException("Content capabilities must match persistence capabilities.", nameof(content));
        if (pageMetadata != expectedPageMetadata)
            throw new ArgumentException("Page metadata capabilities must match persistence capabilities.", nameof(pageMetadata));
        if (Persistence.PreservesFormatting != commands.HasFlag(WriterDocumentCommandCapabilities.CharacterFormatting)
            || Persistence.PreservesFormatting != commands.HasFlag(WriterDocumentCommandCapabilities.ParagraphFormatting))
            throw new ArgumentException("Formatting commands must match persistence capabilities.", nameof(commands));
        if (Persistence.PreservesPageSettings != commands.HasFlag(WriterDocumentCommandCapabilities.PageSettings))
            throw new ArgumentException("Page-settings commands must match persistence capabilities.", nameof(commands));
        Commands = commands;
        Content = content;
        PageMetadata = pageMetadata;
    }

    /// <summary>Gets the serializer's fidelity facts for the profile's format.</summary>
    public WriterPersistenceCapabilities Persistence { get; }

    /// <summary>Alias for <see cref="Persistence"/> used by capability projections.</summary>
    public WriterPersistenceCapabilities PersistenceCapabilities => Persistence;

    /// <summary>Gets the commands that are valid for this profile.</summary>
    public WriterDocumentCommandCapabilities Commands { get; }

    /// <summary>Alias for <see cref="Commands"/>.</summary>
    public WriterDocumentCommandCapabilities CommandCapabilities => Commands;

    /// <summary>Gets the content features that round-trip in this profile.</summary>
    public WriterDocumentContentCapabilities Content { get; }

    /// <summary>Alias for <see cref="Content"/>.</summary>
    public WriterDocumentContentCapabilities ContentCapabilities => Content;

    /// <summary>Gets the page metadata that round-trips in this profile.</summary>
    public WriterDocumentPageMetadataCapabilities PageMetadata { get; }

    /// <summary>Alias for <see cref="PageMetadata"/>.</summary>
    public WriterDocumentPageMetadataCapabilities PageMetadataCapabilities => PageMetadata;

    /// <summary>Whether the profile can expose a command set in full.</summary>
    public bool Supports(WriterDocumentCommandCapabilities commands) =>
        (Commands & commands) == commands;

    /// <summary>Whether the profile can preserve a content feature set in full.</summary>
    public bool Preserves(WriterDocumentContentCapabilities content) =>
        (Content & content) == content;

    /// <summary>Whether the profile can preserve a page metadata feature set in full.</summary>
    public bool Preserves(WriterDocumentPageMetadataCapabilities pageMetadata) =>
        (PageMetadata & pageMetadata) == pageMetadata;

    /// <summary>Whether character and paragraph formatting are both preserved.</summary>
    public bool PreservesFormatting => Persistence.PreservesFormatting;

    /// <summary>Whether images are preserved.</summary>
    public bool PreservesImages => Persistence.PreservesImages;

    /// <summary>Whether tables are preserved.</summary>
    public bool PreservesTables => Persistence.PreservesTables;

    /// <summary>Whether page settings are preserved.</summary>
    public bool PreservesPageSettings => Persistence.PreservesPageSettings;

    /// <summary>Creates a matrix whose persisted content and metadata are derived from one fact source.</summary>
    public static WriterDocumentProfileCapabilities Create(
        WriterPersistenceCapabilities persistence,
        WriterDocumentCommandCapabilities commands)
    {
        ArgumentNullException.ThrowIfNull(persistence);
        return new(persistence, commands, GetContentCapabilities(persistence),
            GetPageMetadataCapabilities(persistence));
    }

    private static WriterDocumentContentCapabilities GetContentCapabilities(
        WriterPersistenceCapabilities persistence)
    {
        var capabilities = WriterDocumentContentCapabilities.Text;
        if (persistence.PreservesFormatting)
            capabilities |= WriterDocumentContentCapabilities.CharacterFormatting
                | WriterDocumentContentCapabilities.ParagraphFormatting;
        if (persistence.PreservesImages)
            capabilities |= WriterDocumentContentCapabilities.Images;
        if (persistence.PreservesHyperlinks)
            capabilities |= WriterDocumentContentCapabilities.Hyperlinks;
        if (persistence.PreservesTables)
            capabilities |= WriterDocumentContentCapabilities.Tables;
        return capabilities;
    }

    private static WriterDocumentPageMetadataCapabilities GetPageMetadataCapabilities(
        WriterPersistenceCapabilities persistence) => persistence.PreservesPageSettings
            ? WriterDocumentPageMetadataCapabilities.PageSettings
            : WriterDocumentPageMetadataCapabilities.None;
}

/// <summary>
/// An explicit creation profile over a <see cref="WriterDocumentFormat"/> identity.
/// Profiles describe persistence and command capabilities; they do not contain document templates.
/// </summary>
public sealed record WriterDocumentProfile
{
    /// <summary>Creates a validated Writer document profile.</summary>
    public WriterDocumentProfile(
        WriterDocumentFormat format,
        string displayName,
        string description,
        string defaultExtension,
        WriterDocumentProfileCapabilities capabilities)
    {
        ValidateFormat(format);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ArgumentException.ThrowIfNullOrWhiteSpace(defaultExtension);
        if (!defaultExtension.StartsWith(".", StringComparison.Ordinal)
            || defaultExtension.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
            || defaultExtension.Length == 1
            || defaultExtension.Contains(Path.DirectorySeparatorChar)
            || defaultExtension.Contains(Path.AltDirectorySeparatorChar))
            throw new ArgumentException("The default extension must be one valid dot-prefixed extension.",
                nameof(defaultExtension));

        Capabilities = capabilities ?? throw new ArgumentNullException(nameof(capabilities));
        Format = format;
        DisplayName = displayName;
        Description = description;
        DefaultExtension = defaultExtension.ToLowerInvariant();
    }

    /// <summary>Gets the persistence identity used by this profile.</summary>
    public WriterDocumentFormat Format { get; }

    /// <summary>Gets the user-facing profile name.</summary>
    public string DisplayName { get; }

    /// <summary>Gets the user-facing profile description.</summary>
    public string Description { get; }

    /// <summary>Gets the default extension used by Save for an untitled document.</summary>
    public string DefaultExtension { get; }

    /// <summary>Alias for <see cref="DefaultExtension"/> used by file-dialog projections.</summary>
    public string DefaultFileExtension => DefaultExtension;

    /// <summary>Gets the profile's command/content/page capability matrix.</summary>
    public WriterDocumentProfileCapabilities Capabilities { get; }

    /// <summary>Gets the persistence fidelity facts for this profile.</summary>
    public WriterPersistenceCapabilities PersistenceCapabilities => Capabilities.Persistence;

    /// <summary>Gets the commands that this profile can expose.</summary>
    public WriterDocumentCommandCapabilities CommandCapabilities => Capabilities.Commands;

    /// <summary>Gets the content features that this profile can preserve.</summary>
    public WriterDocumentContentCapabilities ContentCapabilities => Capabilities.Content;

    /// <summary>Gets the page metadata that this profile can preserve.</summary>
    public WriterDocumentPageMetadataCapabilities PageMetadataCapabilities => Capabilities.PageMetadata;

    /// <summary>Creates a clean untitled document with this profile's explicit format identity.</summary>
    public WriterDocument CreateUntitledDocument(DocumentPageSettings? pageSettings = null) =>
        new(new FlowDocument(), format: Format, pageSettings: pageSettings);

    /// <summary>Whether this profile supports all of the supplied commands.</summary>
    public bool Supports(WriterDocumentCommandCapabilities commands) => Capabilities.Supports(commands);

    /// <summary>Whether this profile preserves all of the supplied content features.</summary>
    public bool Preserves(WriterDocumentContentCapabilities content) => Capabilities.Preserves(content);

    /// <summary>Whether this profile preserves all of the supplied page metadata.</summary>
    public bool Preserves(WriterDocumentPageMetadataCapabilities pageMetadata) => Capabilities.Preserves(pageMetadata);

    private static void ValidateFormat(WriterDocumentFormat format)
    {
        if (!Enum.IsDefined(format))
            throw new ArgumentOutOfRangeException(nameof(format), format, "Unknown Writer document format.");
    }

    /// <summary>Gets the canonical Plain Text profile.</summary>
    public static WriterDocumentProfile PlainText => WriterDocumentProfiles.PlainText;

    /// <summary>Gets the canonical Rich Text profile.</summary>
    public static WriterDocumentProfile RichText => WriterDocumentProfiles.RichText;

    /// <summary>Gets the canonical RibbonKit Writer profile.</summary>
    public static WriterDocumentProfile RibbonKitWriter => WriterDocumentProfiles.RibbonKitWriter;

    /// <summary>Gets all canonical profiles in New-gallery order.</summary>
    public static IReadOnlyList<WriterDocumentProfile> All => WriterDocumentProfiles.All;

    /// <summary>Gets the canonical profile for a format.</summary>
    public static WriterDocumentProfile ForFormat(WriterDocumentFormat format) =>
        WriterDocumentProfiles.ForFormat(format);
}

/// <summary>Canonical Writer creation profiles and format mappings.</summary>
public static class WriterDocumentProfiles
{
    private const WriterDocumentCommandCapabilities CommonCommands =
        WriterDocumentCommandCapabilities.TextEditing
        | WriterDocumentCommandCapabilities.Clipboard
        | WriterDocumentCommandCapabilities.UndoRedo
        | WriterDocumentCommandCapabilities.FindReplace
        | WriterDocumentCommandCapabilities.SpellCheck
        | WriterDocumentCommandCapabilities.Preview
        | WriterDocumentCommandCapabilities.Printing;

    private const WriterDocumentCommandCapabilities FormattedCommands =
        CommonCommands
        | WriterDocumentCommandCapabilities.CharacterFormatting
        | WriterDocumentCommandCapabilities.ParagraphFormatting;

    /// <summary>Plain character-only documents saved as UTF-8 text.</summary>
    public static WriterDocumentProfile PlainText { get; } = new(
        WriterDocumentFormat.PlainText,
        "Plain Text",
        "Character-only documents without formatting or Writer page metadata.",
        ".txt",
        WriterDocumentProfileCapabilities.Create(
            WriterDocumentPersistence.GetCapabilities(WriterDocumentFormat.PlainText),
            CommonCommands));

    /// <summary>Interoperable formatted documents saved as RTF.</summary>
    public static WriterDocumentProfile RichText { get; } = new(
        WriterDocumentFormat.RichText,
        "Rich Text",
        "Interoperable formatted text with best-effort compatibility for advanced content.",
        ".rtf",
        WriterDocumentProfileCapabilities.Create(
            WriterDocumentPersistence.GetCapabilities(WriterDocumentFormat.RichText),
            FormattedCommands));

    /// <summary>RibbonKit Writer's native fidelity profile.</summary>
    public static WriterDocumentProfile RibbonKitWriter { get; } = new(
        WriterDocumentFormat.RibbonKitWriter,
        "RibbonKit Writer",
        "Writer's native formatted-document profile with page settings and versioned persistence.",
        ".rkw",
        WriterDocumentProfileCapabilities.Create(
            WriterDocumentPersistence.GetCapabilities(WriterDocumentFormat.RibbonKitWriter),
            FormattedCommands | WriterDocumentCommandCapabilities.PageSettings));

    /// <summary>All profiles in stable New-gallery order.</summary>
    public static IReadOnlyList<WriterDocumentProfile> All { get; } =
        new[] { PlainText, RichText, RibbonKitWriter };

    /// <summary>The configured default for no-argument New.</summary>
    public static WriterDocumentProfile Default => RichText;

    /// <summary>Finds the canonical profile for a persistence format.</summary>
    public static WriterDocumentProfile ForFormat(WriterDocumentFormat format) => format switch
    {
        WriterDocumentFormat.PlainText => PlainText,
        WriterDocumentFormat.RichText => RichText,
        WriterDocumentFormat.RibbonKitWriter => RibbonKitWriter,
        _ => throw new ArgumentOutOfRangeException(nameof(format), format,
            "Unknown Writer document format.")
    };

    /// <summary>Returns the canonical extension for a persistence format.</summary>
    public static string GetDefaultExtension(WriterDocumentFormat format) => ForFormat(format).DefaultExtension;

    /// <summary>Attempts to find a canonical profile without throwing for unknown values.</summary>
    public static bool TryGet(WriterDocumentFormat format, out WriterDocumentProfile? profile)
    {
        if (Enum.IsDefined(format))
        {
            profile = ForFormat(format);
            return true;
        }

        profile = null;
        return false;
    }

    /// <summary>Whether the supplied instance is one of the canonical creation profiles.</summary>
    public static bool IsCanonical(WriterDocumentProfile profile) =>
        ReferenceEquals(profile, PlainText)
        || ReferenceEquals(profile, RichText)
        || ReferenceEquals(profile, RibbonKitWriter);

    /// <summary>Validates and returns a canonical profile for an API boundary.</summary>
    public static WriterDocumentProfile EnsureCanonical(
        WriterDocumentProfile profile,
        string parameterName = "profile")
    {
        ArgumentNullException.ThrowIfNull(profile, parameterName);
        if (!IsCanonical(profile))
            throw new ArgumentException("Only a canonical Writer document profile may be used here.", parameterName);
        return profile;
    }
}

/// <summary>Convenience mappings from the existing format identity to its profile.</summary>
public static class WriterDocumentFormatExtensions
{
    /// <summary>Gets the canonical creation profile for a format.</summary>
    public static WriterDocumentProfile GetProfile(this WriterDocumentFormat format) =>
        WriterDocumentProfiles.ForFormat(format);

    /// <summary>Gets the default extension for a format.</summary>
    public static string GetDefaultExtension(this WriterDocumentFormat format) =>
        WriterDocumentProfiles.GetDefaultExtension(format);

    /// <summary>Creates a clean untitled document with an explicit format identity.</summary>
    public static WriterDocument CreateUntitledDocument(this WriterDocumentFormat format) =>
        WriterDocumentProfiles.ForFormat(format).CreateUntitledDocument();
}
