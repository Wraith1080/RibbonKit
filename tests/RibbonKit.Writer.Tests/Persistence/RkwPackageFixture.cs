using System.IO;
using System.IO.Compression;
using System.Text;

namespace RibbonKit.Writer.Tests.Persistence;

internal static class RkwPackageFixture
{
    public const string ValidManifestJson = "{\r\n  \"format\": \"RibbonKit.Writer\",\r\n  \"schemaVersion\": 1,\r\n  \"minimumReaderVersion\": 1,\r\n  \"contentSchemaVersion\": 1,\r\n  \"settingsSchemaVersion\": 1,\r\n  \"requiredFeatures\": []\r\n}\r\n";

    public const string ValidSettingsJson = "{\r\n  \"schemaVersion\": 1,\r\n  \"paperSize\": \"Letter\",\r\n  \"portraitWidthDip\": 816,\r\n  \"portraitHeightDip\": 1056,\r\n  \"orientation\": \"Portrait\",\r\n  \"marginsDip\": {\r\n    \"left\": 96,\r\n    \"top\": 96,\r\n    \"right\": 96,\r\n    \"bottom\": 96\r\n  }\r\n}\r\n";

    public static byte[] Utf8(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return Encoding.UTF8.GetBytes(value);
    }

    public static void WriteOuterPackage(string path,
        IEnumerable<(string Name, byte[] Content)> entries)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(entries);

        using var stream = File.Create(path);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Create);
        WriteEntries(archive, entries);
    }

    public static byte[] CreateOuterPackage(
        IEnumerable<(string Name, byte[] Content)> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
            WriteEntries(archive, entries);
        return stream.ToArray();
    }

    public static byte[] CreateInnerXamlPackage(string? documentXaml = null)
    {
        documentXaml ??= "<Section xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"><Paragraph /></Section>";

        return CreateOuterPackage(new[]
        {
            XamlEntry(documentXaml),
            RelationshipsEntry(),
            ContentTypesEntry()
        });
    }

    public static (string Name, byte[] Content) XamlEntry(string? documentXaml = null) =>
        ("Xaml/Document.xaml", Utf8(documentXaml ??
            "<Section xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"><Paragraph /></Section>"));

    public static (string Name, byte[] Content) RelationshipsEntry(string? xml = null) =>
        ("_rels/.rels", Utf8(xml ??
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rDocument\" Type=\"http://schemas.microsoft.com/wpf/2005/10/xaml/entry\" Target=\"/Xaml/Document.xaml\" /></Relationships>\r\n"));

    public static (string Name, byte[] Content) ContentTypesEntry(string? xml = null) =>
        ("[Content_Types].xml", Utf8(xml ??
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\" /><Default Extension=\"xaml\" ContentType=\"application/vnd.ms-wpf.xaml+xml\" /></Types>\r\n"));

    public static (string Name, byte[] Content) ManifestEntry() =>
        ("manifest.json", Utf8(ValidManifestJson));

    public static (string Name, byte[] Content) SettingsEntry() =>
        ("document-settings.json", Utf8(ValidSettingsJson));

    public static (string Name, byte[] Content) ContentEntry(string? documentXaml = null) =>
        ("content.xamlpackage", CreateInnerXamlPackage(documentXaml));

    private static void WriteEntries(ZipArchive archive,
        IEnumerable<(string Name, byte[] Content)> entries)
    {
        foreach (var (name, content) in entries)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentNullException.ThrowIfNull(content);
            var entry = archive.CreateEntry(name, CompressionLevel.NoCompression);
            using var target = entry.Open();
            target.Write(content);
        }
    }
}
