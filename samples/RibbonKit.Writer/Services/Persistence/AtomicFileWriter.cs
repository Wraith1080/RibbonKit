namespace RibbonKit.Writer.Services.Persistence;

using System.IO;

/// <summary>Provides durable, same-directory atomic file replacement.</summary>
public static class AtomicFileWriter
{
    /// <summary>Asynchronously atomically writes bytes to a destination.</summary>
    public static Task WriteAsync(string destination, byte[] content, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(destination);
        ArgumentNullException.ThrowIfNull(content);
        return WriteAsync(destination, new ByteProducer(content).WriteAsync, cancellationToken);
    }

    /// <summary>Produces a temporary file and atomically replaces the destination only after the producer completes.</summary>
    public static async Task WriteAsync(string destination,
        Func<Stream, CancellationToken, Task> producer, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(destination);
        ArgumentNullException.ThrowIfNull(producer);
        var fullPath = Path.GetFullPath(destination);
        var directory = Path.GetDirectoryName(fullPath);
        if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
            throw new DirectoryNotFoundException("The destination directory does not exist.");

        var temporary = Path.Combine(directory, $".{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            await using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write,
                       FileShare.None, 4096, FileOptions.WriteThrough))
            {
                await producer(stream, cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                stream.Flush(flushToDisk: true);
            }
            cancellationToken.ThrowIfCancellationRequested();
            if (File.Exists(fullPath))
            {
                var backup = temporary + ".backup";
                try
                {
                    File.Replace(temporary, fullPath, backup, ignoreMetadataErrors: true);
                    TryDelete(backup);
                }
                finally
                {
                    TryDelete(backup);
                }
            }
            else
            {
                File.Move(temporary, fullPath);
            }
        }
        finally
        {
            TryDelete(temporary);
        }
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private sealed class ByteProducer(byte[] content)
    {
        public Task WriteAsync(Stream stream, CancellationToken cancellationToken) =>
            stream.WriteAsync(content.AsMemory(), cancellationToken).AsTask();
    }
}
