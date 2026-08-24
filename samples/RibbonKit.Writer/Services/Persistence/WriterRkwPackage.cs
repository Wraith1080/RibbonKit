using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Windows.Documents;
using RibbonKit.Writer.Models;

namespace RibbonKit.Writer.Services.Persistence;

internal readonly record struct WriterRkwPackageData(
    FlowDocument Content,
    DocumentPageSettings PageSettings);

/// <summary>Reads and writes the bounded, versioned RibbonKit Writer native package.</summary>
internal static class WriterRkwPackage
{
    internal const int MaximumFileBytes = 64 * 1024 * 1024;
    private const int MaximumMetadataBytes = 64 * 1024;
    private const int MaximumExpandedBytes = 61 * 1024 * 1024;
    private const string ManifestPart = "manifest.json";
    private const string SettingsPart = "document-settings.json";
    private const string ContentPart = "content.xamlpackage";
    private const string FormatIdentity = "RibbonKit.Writer";
    private const int CurrentSchemaVersion = 1;
    private static readonly HashSet<string> RequiredParts = new(StringComparer.Ordinal)
    {
        ManifestPart, SettingsPart, ContentPart
    };

    internal static byte[] Save(WriterDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        var contentBytes = WriterRkwContentSerializer.Save(document.Content);
        _ = WriterRkwContentSerializer.Load(contentBytes);
        var manifestBytes = WriteManifest();
        var settingsBytes = WriteSettings(document.PageSettings);

        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteEntry(archive, ManifestPart, manifestBytes, CompressionLevel.Optimal);
            WriteEntry(archive, SettingsPart, settingsBytes, CompressionLevel.Optimal);
            WriteEntry(archive, ContentPart, contentBytes, CompressionLevel.NoCompression);
        }
        if (stream.Length > MaximumFileBytes)
            throw new InvalidDataException("The native Writer package exceeds the file size limit.");
        return stream.ToArray();
    }

    internal static WriterRkwPackageData Load(byte[] packageBytes)
    {
        ArgumentNullException.ThrowIfNull(packageBytes);
        if (packageBytes.Length is 0 or > MaximumFileBytes)
            throw new InvalidDataException("The native Writer package has an invalid size.");

        try
        {
            using var stream = new MemoryStream(packageBytes, writable: false);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false);
            var entries = ValidateEntries(archive);
            ValidateManifest(ReadEntry(entries[ManifestPart], MaximumMetadataBytes));
            var pageSettings = ReadSettings(ReadEntry(entries[SettingsPart], MaximumMetadataBytes));
            var content = WriterRkwContentSerializer.Load(
                ReadEntry(entries[ContentPart], WriterRkwContentSerializer.MaximumPackageBytes));
            return new WriterRkwPackageData(content, pageSettings);
        }
        catch (InvalidDataException)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException or JsonException
                                         or DecoderFallbackException or ArgumentException
                                         or InvalidOperationException or OverflowException)
        {
            throw new InvalidDataException("The native Writer package is corrupt or unsupported.", exception);
        }
    }

    private static Dictionary<string, ZipArchiveEntry> ValidateEntries(ZipArchive archive)
    {
        if (archive.Entries.Count != RequiredParts.Count)
            throw new InvalidDataException("The native Writer package must contain exactly three required parts.");

        var entries = new Dictionary<string, ZipArchiveEntry>(StringComparer.Ordinal);
        var collisionNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        long totalLength = 0;
        foreach (var entry in archive.Entries)
        {
            if (!RequiredParts.Contains(entry.FullName) || !collisionNames.Add(entry.FullName)
                || !entries.TryAdd(entry.FullName, entry))
                throw new InvalidDataException("The native Writer package contains an invalid, unexpected or duplicate part.");
            if (entry.Length < 0 || entry.Length > WriterRkwContentSerializer.MaximumPackageBytes)
                throw new InvalidDataException($"Package part '{entry.FullName}' exceeds its size limit.");
            totalLength = checked(totalLength + entry.Length);
            if (totalLength > MaximumExpandedBytes)
                throw new InvalidDataException("The native Writer package exceeds its expanded size limit.");
        }
        if (!RequiredParts.SetEquals(entries.Keys))
            throw new InvalidDataException("The native Writer package is missing a required part.");
        return entries;
    }

    private static byte[] ReadEntry(ZipArchiveEntry entry, int maximumBytes)
    {
        if (entry.Length < 0 || entry.Length > maximumBytes)
            throw new InvalidDataException($"Package part '{entry.FullName}' exceeds its size limit.");

        using var source = entry.Open();
        using var destination = new MemoryStream((int)entry.Length);
        var buffer = new byte[81920];
        int read;
        while ((read = source.Read(buffer, 0, buffer.Length)) > 0)
        {
            if (destination.Length + read > maximumBytes)
                throw new InvalidDataException($"Package part '{entry.FullName}' exceeds its size limit.");
            destination.Write(buffer, 0, read);
        }
        return destination.ToArray();
    }

    private static byte[] WriteManifest()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
        {
            writer.WriteStartObject();
            writer.WriteString("format", FormatIdentity);
            writer.WriteNumber("schemaVersion", CurrentSchemaVersion);
            writer.WriteNumber("minimumReaderVersion", CurrentSchemaVersion);
            writer.WriteNumber("contentSchemaVersion", CurrentSchemaVersion);
            writer.WriteNumber("settingsSchemaVersion", CurrentSchemaVersion);
            writer.WriteStartArray("requiredFeatures");
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    private static void ValidateManifest(byte[] bytes)
    {
        using var document = ParseJson(bytes, "manifest");
        var root = RequireObject(document.RootElement, "manifest");
        var schemaVersion = RequireUniqueInt(root, "schemaVersion");
        switch (schemaVersion)
        {
            case 1:
                ValidateManifestVersion1(root);
                return;
            default:
                throw new InvalidDataException("The native Writer package uses an unsupported manifest version.");
        }
    }

    private static void ValidateManifestVersion1(JsonElement root)
    {
        RequireProperties(root,
            ["format", "schemaVersion", "minimumReaderVersion", "contentSchemaVersion",
                "settingsSchemaVersion", "requiredFeatures"]);
        if (RequireString(root, "format") != FormatIdentity)
            throw new InvalidDataException("The package is not a RibbonKit Writer document.");
        if (RequireInt(root, "schemaVersion") != CurrentSchemaVersion
            || RequireInt(root, "minimumReaderVersion") != CurrentSchemaVersion
            || RequireInt(root, "contentSchemaVersion") != CurrentSchemaVersion
            || RequireInt(root, "settingsSchemaVersion") != CurrentSchemaVersion)
            throw new InvalidDataException("The native Writer package uses an unsupported mandatory version.");

        var features = root.GetProperty("requiredFeatures");
        if (features.ValueKind != JsonValueKind.Array || features.GetArrayLength() != 0)
            throw new InvalidDataException("The native Writer package requires unsupported features.");
    }

    private static byte[] WriteSettings(DocumentPageSettings settings)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
        {
            writer.WriteStartObject();
            writer.WriteNumber("schemaVersion", CurrentSchemaVersion);
            writer.WriteString("paperSize", settings.PaperSize.ToString());
            writer.WriteNumber("portraitWidthDip", settings.PortraitWidthDip);
            writer.WriteNumber("portraitHeightDip", settings.PortraitHeightDip);
            writer.WriteString("orientation", settings.Orientation.ToString());
            writer.WriteStartObject("marginsDip");
            writer.WriteNumber("left", settings.Margins.LeftDip);
            writer.WriteNumber("top", settings.Margins.TopDip);
            writer.WriteNumber("right", settings.Margins.RightDip);
            writer.WriteNumber("bottom", settings.Margins.BottomDip);
            writer.WriteEndObject();
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    private static DocumentPageSettings ReadSettings(byte[] bytes)
    {
        using var document = ParseJson(bytes, "document settings");
        var root = RequireObject(document.RootElement, "document settings");
        var schemaVersion = RequireUniqueInt(root, "schemaVersion");
        return schemaVersion switch
        {
            1 => ReadSettingsVersion1(root),
            _ => throw new InvalidDataException("The document settings use an unsupported version.")
        };
    }

    private static DocumentPageSettings ReadSettingsVersion1(JsonElement root)
    {
        RequireProperties(root,
            ["schemaVersion", "paperSize", "portraitWidthDip", "portraitHeightDip",
                "orientation", "marginsDip"]);
        if (RequireInt(root, "schemaVersion") != CurrentSchemaVersion)
            throw new InvalidDataException("The document settings use an unsupported version.");
        if (!Enum.TryParse<DocumentPaperSize>(RequireString(root, "paperSize"), false, out var paperSize)
            || !Enum.IsDefined(paperSize))
            throw new InvalidDataException("The document settings contain an invalid paper size.");
        if (!Enum.TryParse<DocumentPageOrientation>(RequireString(root, "orientation"), false,
                out var orientation) || !Enum.IsDefined(orientation))
            throw new InvalidDataException("The document settings contain an invalid orientation.");

        var width = RequireFiniteDouble(root, "portraitWidthDip");
        var height = RequireFiniteDouble(root, "portraitHeightDip");
        var marginsElement = RequireObject(root.GetProperty("marginsDip"), "page margins");
        RequireProperties(marginsElement, ["left", "top", "right", "bottom"]);
        var margins = new DocumentPageMargins(
            RequireFiniteDouble(marginsElement, "left"),
            RequireFiniteDouble(marginsElement, "top"),
            RequireFiniteDouble(marginsElement, "right"),
            RequireFiniteDouble(marginsElement, "bottom"));

        try
        {
            if (paperSize == DocumentPaperSize.Custom)
                return DocumentPageSettings.CreateCustom(width, height, orientation, margins);

            var preset = DocumentPageSettings.CreatePreset(paperSize, orientation, margins);
            if (Math.Abs(preset.PortraitWidthDip - width) > 0.000000001
                || Math.Abs(preset.PortraitHeightDip - height) > 0.000000001)
                throw new InvalidDataException("Named paper dimensions do not match their canonical preset.");
            return preset;
        }
        catch (ArgumentException exception)
        {
            throw new InvalidDataException("The document page settings are invalid.", exception);
        }
    }

    private static JsonDocument ParseJson(byte[] bytes, string description)
    {
        try
        {
            return JsonDocument.Parse(bytes, new JsonDocumentOptions
            {
                AllowTrailingCommas = false,
                CommentHandling = JsonCommentHandling.Disallow,
                MaxDepth = 16
            });
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException($"The {description} JSON is invalid.", exception);
        }
    }

    private static JsonElement RequireObject(JsonElement element, string description)
    {
        if (element.ValueKind != JsonValueKind.Object)
            throw new InvalidDataException($"The {description} must be a JSON object.");
        return element;
    }

    private static void RequireProperties(JsonElement element, string[] requiredNames)
    {
        var required = requiredNames.ToHashSet(StringComparer.Ordinal);
        var found = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in element.EnumerateObject())
        {
            if (!required.Contains(property.Name) || !found.Add(property.Name))
                throw new InvalidDataException($"JSON contains an unknown or duplicate property '{property.Name}'.");
        }
        if (!required.SetEquals(found))
            throw new InvalidDataException("JSON is missing a required property.");
    }

    private static string RequireString(JsonElement element, string propertyName)
    {
        var property = element.GetProperty(propertyName);
        if (property.ValueKind != JsonValueKind.String || property.GetString() is not { } value
            || value.Length is 0 or > 128)
            throw new InvalidDataException($"JSON property '{propertyName}' must be a bounded string.");
        return value;
    }

    private static int RequireInt(JsonElement element, string propertyName)
    {
        var property = element.GetProperty(propertyName);
        if (property.ValueKind != JsonValueKind.Number || !property.TryGetInt32(out var value))
            throw new InvalidDataException($"JSON property '{propertyName}' must be an integer.");
        return value;
    }

    private static int RequireUniqueInt(JsonElement element, string propertyName)
    {
        var matches = element.EnumerateObject()
            .Where(property => property.NameEquals(propertyName))
            .ToArray();
        if (matches.Length != 1 || matches[0].Value.ValueKind != JsonValueKind.Number
            || !matches[0].Value.TryGetInt32(out var value))
            throw new InvalidDataException($"JSON property '{propertyName}' must be one unique integer.");
        return value;
    }

    private static double RequireFiniteDouble(JsonElement element, string propertyName)
    {
        var property = element.GetProperty(propertyName);
        if (property.ValueKind != JsonValueKind.Number || !property.TryGetDouble(out var value)
            || !double.IsFinite(value))
            throw new InvalidDataException($"JSON property '{propertyName}' must be a finite number.");
        return value;
    }

    private static void WriteEntry(ZipArchive archive, string name, byte[] content,
        CompressionLevel compressionLevel)
    {
        var entry = archive.CreateEntry(name, compressionLevel);
        using var stream = entry.Open();
        stream.Write(content, 0, content.Length);
    }
}
