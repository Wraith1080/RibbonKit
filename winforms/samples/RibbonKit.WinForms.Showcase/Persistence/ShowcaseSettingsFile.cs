using System.Text;
using System.Text.Json;

namespace RibbonKit.WinForms.Showcase.Persistence;

// Each file is replaced atomically on the same volume. Rejected input remains
// untouched for the whole process; the other settings file can still be saved.
internal sealed class ShowcaseSettingsFile(string path)
{
    internal string Path { get; } = path;
    private bool _writeBlocked;
    private string? _lastSaved;
    internal const int MaximumBytes = 1024 * 1024;

    internal string? Load(Action<string> restore)
    {
        try
        {
            using var stream = new FileStream(Path, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (stream.Length > MaximumBytes) throw new InvalidOperationException("The settings file is too large.");
            using var reader = new StreamReader(stream, Encoding.UTF8, true);
            string json = reader.ReadToEnd();
            restore(json); _lastSaved = json;
            return null;
        }
        catch (FileNotFoundException) { return null; }
        catch (DirectoryNotFoundException) { return null; }
        catch (Exception error) when (IsStorageError(error))
        {
            _writeBlocked = true;
            return $"{System.IO.Path.GetFileName(Path)} could not be restored and will be kept unchanged: {error.Message}";
        }
    }

    internal string? Save(string json)
    {
        if (_writeBlocked) return $"{System.IO.Path.GetFileName(Path)} is kept unchanged because startup restore failed.";
        if (_lastSaved == json) return null;
        string? temporary = null;
        try
        {
            if (Encoding.UTF8.GetByteCount(json) > MaximumBytes) throw new InvalidOperationException("The settings file is too large.");
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
            temporary = Path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                var bytes = Encoding.UTF8.GetBytes(json); stream.Write(bytes); stream.Flush(flushToDisk: true);
            }
            if (File.Exists(Path)) File.Replace(temporary, Path, null);
            else File.Move(temporary, Path);
            _lastSaved = json;
            return null;
        }
        catch (Exception error) when (IsStorageError(error))
        {
            return $"{System.IO.Path.GetFileName(Path)} could not be saved: {error.Message}";
        }
        finally
        {
            if (temporary is not null)
                try { File.Delete(temporary); } catch (Exception error) when (error is IOException or UnauthorizedAccessException) { }
        }
    }

    private static bool IsStorageError(Exception error) => error is IOException or UnauthorizedAccessException
        or JsonException or InvalidOperationException or ArgumentException or NotSupportedException;
}
