using RibbonKit.Writer.Models;

namespace RibbonKit.Writer.Services.RecentFiles;

/// <summary>A document shown in Writer's recent-file list.</summary>
public sealed record RecentFileEntry(string Path, WriterDocumentFormat Format, DateTimeOffset LastUsedUtc);
