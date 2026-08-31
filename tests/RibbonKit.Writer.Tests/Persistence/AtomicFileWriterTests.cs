using System.Text;
using System.IO;
using RibbonKit.Writer.Services.Persistence;
using Xunit;

namespace RibbonKit.Writer.Tests.Persistence;

public sealed class AtomicFileWriterTests
{
    [Fact]
    public async Task FirstSaveAndReplacementAreAtomicAndClean()
    {
        using var directory = new TemporaryDirectory();
        var destination = Path.Combine(directory.Path, "document.txt");

        await AtomicFileWriter.WriteAsync(destination, Encoding.UTF8.GetBytes("first"), default);
        Assert.Equal("first", await File.ReadAllTextAsync(destination));

        await AtomicFileWriter.WriteAsync(destination, Encoding.UTF8.GetBytes("replacement"), default);
        Assert.Equal("replacement", await File.ReadAllTextAsync(destination));
        Assert.Empty(Directory.GetFiles(directory.Path, ".*"));
    }
    [Fact]
    public async Task ProducerFailurePreservesDestinationAndCleansArtifacts()
    {
        using var directory = new TemporaryDirectory();
        var destination = Path.Combine(directory.Path, "document.txt");
        await File.WriteAllTextAsync(destination, "old");

        await Assert.ThrowsAsync<InvalidOperationException>(() => AtomicFileWriter.WriteAsync(
            destination,
            async (stream, token) =>
            {
                await stream.WriteAsync(Encoding.UTF8.GetBytes("partial"), token);
                throw new InvalidOperationException("producer failed");
            },
            default));

        Assert.Equal("old", await File.ReadAllTextAsync(destination));
        Assert.Empty(Directory.GetFiles(directory.Path, ".*"));
    }

    [Fact]
    public async Task CancellationAfterPartialWritePreservesDestinationAndCleansArtifacts()
    {
        using var directory = new TemporaryDirectory();
        var destination = Path.Combine(directory.Path, "document.txt");
        await File.WriteAllTextAsync(destination, "old");
        using var cancellation = new CancellationTokenSource();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => AtomicFileWriter.WriteAsync(
            destination,
            async (stream, token) =>
            {
                await stream.WriteAsync(Encoding.UTF8.GetBytes("partial"), token);
                cancellation.Cancel();
                token.ThrowIfCancellationRequested();
            },
            cancellation.Token));

        Assert.Equal("old", await File.ReadAllTextAsync(destination));
        Assert.Empty(Directory.GetFiles(directory.Path, "*.tmp"));
        Assert.Empty(Directory.GetFiles(directory.Path, "*.backup"));
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory() => Path = Directory.CreateTempSubdirectory("rk-atomic-").FullName;

        public string Path { get; }

        public void Dispose() => Directory.Delete(Path, recursive: true);
    }
}
