using System.Text.Json;
using System.IO;
using RibbonKit.Writer.Models;
using RibbonKit.Writer.Services.Persistence;

namespace RibbonKit.Writer.Services.RecentFiles;

/// <summary>Best-effort, app-owned persistence for the Writer recent-file list.</summary>
public sealed class RecentFileService
{
    private const int CurrentVersion = 1;
    public const int DefaultCapacity = 20;
    private readonly string _path;
    private readonly int _capacity;
    private readonly List<RecentFileEntry> _entries = new();

    public RecentFileService(string? path = null, int capacity = DefaultCapacity)
    {
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        _capacity = capacity;
        _path = path ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RibbonKit", "Writer", "recent-files.json");
        ArgumentException.ThrowIfNullOrWhiteSpace(_path);
    }

    public IReadOnlyList<RecentFileEntry> Entries => _entries;

    public void Load(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _entries.Clear();
        try
        {
            if (!File.Exists(_path)) return;
            cancellationToken.ThrowIfCancellationRequested();
            using var stream = File.OpenRead(_path);
            var state = JsonSerializer.Deserialize<State>(stream);
            if (state?.Version != CurrentVersion || state.Files is null) return;
            // AddInMemory inserts at the front, so consume oldest-to-newest to leave newest first.
            foreach (var entry in state.Files.OrderBy(x => x?.LastUsedUtc))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (entry is not null && Enum.IsDefined(entry.Format) && !string.IsNullOrWhiteSpace(entry.Path))
                {
                    try { AddInMemory(entry with { Path = Path.GetFullPath(entry.Path), LastUsedUtc = entry.LastUsedUtc.ToUniversalTime() }); }
                    catch (ArgumentException) { }
                    catch (NotSupportedException) { }
                }
            }
        }
        catch (OperationCanceledException) { throw; }
        catch (IOException) { _entries.Clear(); }
        catch (UnauthorizedAccessException) { _entries.Clear(); }
        catch (JsonException) { _entries.Clear(); }
        catch (ArgumentException) { _entries.Clear(); }
    }

    public bool TryAdd(string path, WriterDocumentFormat format, DateTimeOffset? lastUsedUtc = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (!Enum.IsDefined(format))
            throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported recent-file format.");
        cancellationToken.ThrowIfCancellationRequested();
        var candidate = new RecentFileEntry(Path.GetFullPath(path), format,
            (lastUsedUtc ?? DateTimeOffset.UtcNow).ToUniversalTime());
        var old = _entries.ToArray();
        AddInMemory(candidate);
        try
        {
            var persisted = TryPersist(cancellationToken);
            if (!persisted)
            {
                _entries.Clear();
                _entries.AddRange(old);
            }
            return persisted;
        }
        catch { _entries.Clear(); _entries.AddRange(old); throw; }
    }

    private void AddInMemory(RecentFileEntry entry)
    {
        _entries.RemoveAll(existing => string.Equals(existing.Path, entry.Path,
            StringComparison.OrdinalIgnoreCase));
        _entries.Insert(0, entry);
        if (_entries.Count > _capacity) _entries.RemoveRange(_capacity, _entries.Count - _capacity);
    }

    private bool TryPersist(CancellationToken cancellationToken = default)
    {
        try
        {
            var directory = Path.GetDirectoryName(Path.GetFullPath(_path));
            if (string.IsNullOrEmpty(directory)) return false;
            Directory.CreateDirectory(directory);
            var state = new State(CurrentVersion, _entries);
            var json = JsonSerializer.SerializeToUtf8Bytes(state, new JsonSerializerOptions { WriteIndented = true });
            AtomicFileWriter.WriteAsync(_path, json, cancellationToken).GetAwaiter().GetResult();
            return true;
        }
        catch (OperationCanceledException) { throw; }
        catch (IOException) { return false; }
        catch (UnauthorizedAccessException) { return false; }
        catch (JsonException) { return false; }
    }

    private sealed record State(int Version, IReadOnlyList<RecentFileEntry> Files);
}
