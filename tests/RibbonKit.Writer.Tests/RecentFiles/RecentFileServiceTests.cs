using System.Text.Json;
using System.IO;
using System.Linq;
using RibbonKit.Writer.Models;
using RibbonKit.Writer.Services.RecentFiles;
using RibbonKit.Writer.Tests.Document;
using Xunit;

namespace RibbonKit.Writer.Tests.RecentFiles;

public sealed class RecentFileServiceTests
{
    [Fact]
    public void InMemoryOrderingDeduplicationAndCapacityAreEnforced()
    {
        using var directory = new TemporaryDirectory();
        var service = new RecentFileService(Path.Combine(directory.Path, "recent.json"), capacity: 2);
        var now = DateTimeOffset.UtcNow;
        Assert.True(service.TryAdd(Path.Combine(directory.Path, "a.txt"), WriterDocumentFormat.PlainText, now.AddMinutes(-2)));
        Assert.True(service.TryAdd(Path.Combine(directory.Path, "b.rtf"), WriterDocumentFormat.RichText, now.AddMinutes(-1)));
        Assert.True(service.TryAdd(Path.Combine(directory.Path, "A.TXT"), WriterDocumentFormat.PlainText, now));
        Assert.Equal(2, service.Entries.Count);
        Assert.Equal("A.TXT", Path.GetFileName(service.Entries[0].Path));
        Assert.Equal("b.rtf", Path.GetFileName(service.Entries[1].Path));
    }

    [Fact]
    public void OutOfOrderTimestampsReloadNewestFirst()
    {
        using var directory = new TemporaryDirectory();
        var settingsPath = Path.Combine(directory.Path, "recent.json");
        var service = new RecentFileService(settingsPath, capacity: 3);
        var now = DateTimeOffset.UtcNow;
        service.TryAdd(Path.Combine(directory.Path, "old.txt"), WriterDocumentFormat.PlainText, now.AddDays(-2));
        service.TryAdd(Path.Combine(directory.Path, "new.rtf"), WriterDocumentFormat.RichText, now);
        service.TryAdd(Path.Combine(directory.Path, "middle.txt"), WriterDocumentFormat.PlainText, now.AddDays(-1));
        var reloaded = new RecentFileService(settingsPath, capacity: 3);
        reloaded.Load();
        Assert.Equal(new[] { "new.rtf", "middle.txt", "old.txt" }, reloaded.Entries.Select(entry => Path.GetFileName(entry.Path)));
    }

    [Fact]
    public void ReloadCanonicalizesPathsAndUtcOffsetsRetainsRulesAndAcceptsRibbonKitWriter()
    {
        using var directory = new TemporaryDirectory();
        var settingsPath = Path.Combine(directory.Path, "recent.json");
        var storedPath = Path.Combine(directory.Path, "relative.txt");
        var timestamp = new DateTimeOffset(2026, 8, 20, 12, 0, 0, TimeSpan.FromHours(7));
        var json = JsonSerializer.Serialize(new
        {
            Version = 1,
            Files = new[]
            {
                new { Path = storedPath, Format = WriterDocumentFormat.PlainText, LastUsedUtc = timestamp },
                new { Path = storedPath.ToUpperInvariant(), Format = WriterDocumentFormat.PlainText, LastUsedUtc = timestamp.AddMinutes(1) },
                new { Path = Path.Combine(directory.Path, "document.rkw"), Format = WriterDocumentFormat.RibbonKitWriter, LastUsedUtc = timestamp }
            }
        });
        File.WriteAllText(settingsPath, json);
        var service = new RecentFileService(settingsPath, capacity: 2);
        service.Load();
        Assert.Equal(2, service.Entries.Count);
        Assert.Equal(Path.GetFullPath(storedPath.ToUpperInvariant()), service.Entries[0].Path);
        Assert.Equal(TimeSpan.Zero, service.Entries[0].LastUsedUtc.Offset);
        Assert.Contains(service.Entries, entry => entry.Format == WriterDocumentFormat.RibbonKitWriter);
    }

    [Fact]
    public void CorruptJsonAndUnknownVersionDegradeToEmpty()
    {
        using var directory = new TemporaryDirectory();
        var settingsPath = Path.Combine(directory.Path, "recent.json");
        var service = new RecentFileService(settingsPath);
        File.WriteAllText(settingsPath, "not json");
        service.Load();
        Assert.Empty(service.Entries);
        File.WriteAllText(settingsPath, "{\"Version\":99,\"Files\":[]}");
        service.Load();
        Assert.Empty(service.Entries);
    }

    [Fact]
    public void InvalidConstructorAndEntryArgumentsThrowExactExceptions()
    {
        Assert.Throws<ArgumentException>(() => new RecentFileService("   "));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RecentFileService(capacity: 0));
        using var directory = new TemporaryDirectory();
        var service = new RecentFileService(Path.Combine(directory.Path, "recent.json"));
        Assert.Throws<ArgumentException>(() => service.TryAdd("   ", WriterDocumentFormat.PlainText));
        Assert.Throws<ArgumentOutOfRangeException>(() => service.TryAdd(Path.Combine(directory.Path, "file.txt"), (WriterDocumentFormat)99));
    }

    [Fact]
    public void PersistenceFailureReturnsFalseAndLeavesServiceUsable()
    {
        using var directory = new TemporaryDirectory();
        var parentFile = Path.Combine(directory.Path, "parent-file");
        File.WriteAllText(parentFile, "not a directory");
        var service = new RecentFileService(Path.Combine(parentFile, "recent.json"), capacity: 2);
        Assert.False(service.TryAdd(Path.Combine(directory.Path, "failed.txt"), WriterDocumentFormat.PlainText));
        Assert.Empty(service.Entries);
        using var validDirectory = new TemporaryDirectory();
        var usable = new RecentFileService(Path.Combine(validDirectory.Path, "recent.json"));
        Assert.True(usable.TryAdd(Path.Combine(validDirectory.Path, "works.txt"), WriterDocumentFormat.PlainText));
        Assert.Single(usable.Entries);
    }

    [Fact]
    public void CancellationPreservesExistingList()
    {
        using var directory = new TemporaryDirectory();
        var service = new RecentFileService(Path.Combine(directory.Path, "recent.json"));
        service.TryAdd(Path.Combine(directory.Path, "existing.txt"), WriterDocumentFormat.PlainText);
        var before = service.Entries.ToArray();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.Throws<OperationCanceledException>(() => service.TryAdd(Path.Combine(directory.Path, "new.txt"), WriterDocumentFormat.PlainText, cancellationToken: cancellation.Token));
        Assert.Equal(before, service.Entries);
    }

    [Fact]
    public async Task TryAddCompletesWhenInvokedOnStaDispatcher()
    {
        await StaTestHelper.RunAsync(() =>
        {
            using var directory = new TemporaryDirectory();
            var service = new RecentFileService(Path.Combine(directory.Path, "recent.json"));
            Assert.True(service.TryAdd(Path.Combine(directory.Path, "dispatcher.txt"), WriterDocumentFormat.PlainText));
            return Task.CompletedTask;
        });
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory() => Path = Directory.CreateTempSubdirectory("rk-recent-").FullName;
        public string Path { get; }
        public void Dispose() => Directory.Delete(Path, recursive: true);
    }
}
