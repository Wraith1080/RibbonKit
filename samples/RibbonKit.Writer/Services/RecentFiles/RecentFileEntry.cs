using System.Text.Json.Serialization;
using RibbonKit.Writer.Models;

namespace RibbonKit.Writer.Services.RecentFiles;

/// <summary>A document shown in Writer's recent-file list.</summary>
public sealed record RecentFileEntry(string Path, WriterDocumentFormat Format, DateTimeOffset LastUsedUtc)
{
    /// <summary>Gets the display name without exposing the full path as the primary label.</summary>
    [JsonIgnore]
    public string FileName => GetFileName(Path);

    /// <summary>Gets the containing directory used as the secondary row label.</summary>
    [JsonIgnore]
    public string FolderPath => GetFolderPath(Path);

    /// <summary>Gets the user-facing format label for the recent row metadata.</summary>
    [JsonIgnore]
    public string FormatLabel => Format switch
    {
        WriterDocumentFormat.RichText => "Rich Text",
        WriterDocumentFormat.PlainText => "Plain Text",
        WriterDocumentFormat.RibbonKitWriter => "RibbonKit Writer",
        _ => Format.ToString()
    };

    /// <summary>Gets a stable, local-time last-used label derived from the persisted timestamp.</summary>
    [JsonIgnore]
    public string LastUsedLabel => $"Last used {LastUsedUtc.ToLocalTime():g}";

    private static string GetFileName(string path)
    {
        try
        {
            var fileName = System.IO.Path.GetFileName(path);
            return string.IsNullOrWhiteSpace(fileName) ? path : fileName;
        }
        catch (ArgumentException)
        {
            return path;
        }
    }

    private static string GetFolderPath(string path)
    {
        try
        {
            var folder = System.IO.Path.GetDirectoryName(path);
            return string.IsNullOrWhiteSpace(folder) ? "This folder" : folder;
        }
        catch (ArgumentException)
        {
            return "Location unavailable";
        }
    }
}
