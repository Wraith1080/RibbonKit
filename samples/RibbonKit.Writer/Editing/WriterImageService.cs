using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace RibbonKit.Writer.Editing;

/// <summary>Options used when a portable image is inserted into Writer.</summary>
public sealed record WriterImageInsertionOptions
{
    /// <summary>Maximum encoded bytes accepted from a source stream.</summary>
    public long MaximumBytes { get; init; } = 16 * 1024 * 1024;

    /// <summary>Maximum decoded pixel count accepted from an image.</summary>
    public long MaximumPixels { get; init; } = 32 * 1024 * 1024;

    /// <summary>Maximum decoded width or height in pixels.</summary>
    public int MaximumDimension { get; init; } = 8192;

    /// <summary>Optional displayed width in device-independent units.</summary>
    public double? WidthDip { get; init; }

    /// <summary>Optional displayed height in device-independent units.</summary>
    public double? HeightDip { get; init; }
}

/// <summary>Loads and inserts inert, portable WPF images into the native Writer editor.</summary>
/// <remarks>
/// Sources are decoded with <see cref="BitmapCacheOption.OnLoad"/> before the caller's stream is
/// released. The resulting <see cref="BitmapImage"/> has no external URI, so native `.rkw`
/// persistence can package the image bytes rather than retaining a path to the source file.
/// OLE, XAML object graphs and executable attachments are not accepted.
/// </remarks>
public sealed class WriterImageService
{
    /// <summary>Default image limits used by the file and stream overloads.</summary>
    public static WriterImageInsertionOptions DefaultOptions { get; } = new();

    /// <summary>Tries to insert an image loaded from a local file.</summary>
    /// <param name="editor">The live native Writer editor.</param>
    /// <param name="path">The source image path.</param>
    /// <param name="options">Optional decoded-size and display-size limits.</param>
    /// <returns><see langword="true"/> only when an image was inserted.</returns>
    public bool TryInsertImage(RichTextBox editor, string path,
        WriterImageInsertionOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(editor);
        if (string.IsNullOrWhiteSpace(path))
            return false;

        try
        {
            using var source = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            return TryInsertImage(editor, source, options);
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (NotSupportedException)
        {
            return false;
        }
        catch (System.Security.SecurityException)
        {
            return false;
        }
    }

    /// <summary>Tries to insert an image read from a caller-owned stream.</summary>
    /// <remarks>The stream is not disposed and its current position is not changed.</remarks>
    public bool TryInsertImage(RichTextBox editor, Stream source,
        WriterImageInsertionOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(editor);
        ArgumentNullException.ThrowIfNull(source);
        options ??= DefaultOptions;
        if (!TryValidateOptions(options))
            return false;

        try
        {
            var bytes = ReadBounded(source, options.MaximumBytes);
            if (bytes is null)
                return false;
            var bitmap = Decode(bytes, options);
            if (bitmap is null)
                return false;

            var image = new Image
            {
                Source = bitmap,
                Stretch = Stretch.Uniform,
                IsHitTestVisible = false,
                SnapsToDevicePixels = true
            };
            if (options.WidthDip is { } width)
                image.Width = width;
            if (options.HeightDip is { } height)
                image.Height = height;

            return WriterInlineInsertion.TryReplaceSelection(editor,
                new InlineUIContainer(image) { BaselineAlignment = BaselineAlignment.Center });
        }
        catch (IOException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
        catch (NotSupportedException)
        {
            return false;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>Tries to insert an image from already bounded encoded bytes.</summary>
    public bool TryInsertImage(RichTextBox editor, byte[] bytes,
        WriterImageInsertionOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(editor);
        ArgumentNullException.ThrowIfNull(bytes);
        using var source = new MemoryStream(bytes, writable: false);
        return TryInsertImage(editor, source, options);
    }

    /// <summary>Removes the image containing the current caret or selection.</summary>
    /// <returns><see langword="true"/> when an image inline was removed.</returns>
    public bool TryRemoveSelectedImage(RichTextBox editor)
    {
        ArgumentNullException.ThrowIfNull(editor);
        var container = WriterInlineInsertion.FindImage(editor);
        return container is not null && TryRemoveImage(editor, container);
    }

    /// <summary>Removes one captured image only while it still belongs to the live editor document.</summary>
    public bool TryRemoveImage(RichTextBox editor, InlineUIContainer container)
    {
        ArgumentNullException.ThrowIfNull(editor);
        ArgumentNullException.ThrowIfNull(container);
        return container.Child is Image
            && WriterInlineInsertion.IsInlineInDocument(editor.Document, container)
            && WriterInlineInsertion.TryRemoveInline(editor, container);
    }

    /// <summary>Creates a frozen portable bitmap from encoded image bytes.</summary>
    /// <returns>A decoded image or <see langword="null"/> when validation fails.</returns>
    public BitmapImage? TryDecode(byte[] bytes, WriterImageInsertionOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        options ??= DefaultOptions;
        if (!TryValidateOptions(options) || bytes.LongLength > options.MaximumBytes)
            return null;
        try
        {
            return Decode(bytes, options);
        }
        catch (ArgumentException)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static byte[]? ReadBounded(Stream source, long maximumBytes)
    {
        var originalPosition = source.CanSeek ? source.Position : (long?)null;
        try
        {
            if (source.CanSeek)
                source.Position = originalPosition!.Value;
            using var destination = new MemoryStream();
            var buffer = new byte[81920];
            int read;
            while ((read = source.Read(buffer, 0, buffer.Length)) > 0)
            {
                if (destination.Length + read > maximumBytes)
                    return null;
                destination.Write(buffer, 0, read);
            }
            return destination.ToArray();
        }
        finally
        {
            if (originalPosition is { } position && source.CanSeek)
                source.Position = position;
        }
    }

    private static BitmapImage? Decode(byte[] bytes, WriterImageInsertionOptions options)
    {
        if (!WriterImageCodecValidation.IsAllowedSignature(bytes)
            || !WriterImageCodecValidation.TryReadDimensions(bytes, out var width, out var height)
            || !WriterImageCodecValidation.IsWithinLimits(width, height,
                options.MaximumPixels, options.MaximumDimension))
            return null;

        using var source = new MemoryStream(bytes, writable: false);
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
        bitmap.StreamSource = source;
        bitmap.EndInit();
        if (!WriterImageCodecValidation.IsWithinLimits(bitmap.PixelWidth, bitmap.PixelHeight,
            options.MaximumPixels, options.MaximumDimension))
            return null;
        bitmap.Freeze();
        return bitmap;
    }

    private static bool TryValidateOptions(WriterImageInsertionOptions options)
    {
        if (options.MaximumBytes is <= 0 or > 16 * 1024 * 1024
            || options.MaximumPixels is <= 0 or > 32 * 1024 * 1024
            || options.MaximumDimension is <= 0 or > 8192)
            return false;
        return IsValidDimension(options.WidthDip) && IsValidDimension(options.HeightDip);
    }

    private static bool IsValidDimension(double? value) => value is null
        || (double.IsFinite(value.Value) && value.Value > 0 && value.Value <= 8192);
}
