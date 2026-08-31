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
public sealed class WriterImageService : IWriterEditingUndoExtension
{
    private const int MaximumTrackedRemovals = 64;
    private readonly List<RemovedImageUndoRecord> _removedImages = [];
    private readonly List<RemovedImageUndoRecord> _customRedo = [];
    private readonly List<ResizedImageUndoRecord> _resizedImages = [];
    private FlowDocument? _undoDocument;
    private int _textChangeSuppression;

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
        if (!WriterInlineInsertion.TryGetImage(container, out var image)
            || !WriterInlineInsertion.IsInlineInDocument(editor.Document, container)
            || WriterInlineInsertion.GetOwnerCollection(container) is not { } owner)
            return false;

        var index = owner.Cast<Inline>().TakeWhile(inline => !ReferenceEquals(inline, container)).Count();
        if (index >= owner.Count)
            return false;
        var record = new RemovedImageUndoRecord(editor.Document, container.Parent!, index,
            CloneImageElement(image));
        if (!WriterInlineInsertion.TryRemoveInline(editor, container))
            return false;

        if (!ReferenceEquals(_undoDocument, editor.Document))
            ResetUndoHistory(editor.Document);
        var obsolete = _removedImages.Where(existing =>
            ReferenceEquals(existing.RestoredContainer, container)).ToArray();
        _removedImages.RemoveAll(existing => obsolete.Contains(existing));
        _customRedo.RemoveAll(existing => obsolete.Contains(existing));
        _removedImages.Add(record);
        if (_removedImages.Count > MaximumTrackedRemovals)
            _removedImages.RemoveRange(0, _removedImages.Count - MaximumTrackedRemovals);
        return true;
    }

    /// <summary>
    /// Replaces one live picture with a committed dimension snapshot inside one native undo unit.
    /// Pointer preview property changes are restored before the structural commit, so Undo returns
    /// to the exact opening geometry and Redo returns to the committed geometry.
    /// </summary>
    internal bool TryResizeImage(RichTextBox editor, InlineUIContainer container,
        Image openingSnapshot, double width, double height,
        out InlineUIContainer replacement)
    {
        ArgumentNullException.ThrowIfNull(editor);
        ArgumentNullException.ThrowIfNull(container);
        ArgumentNullException.ThrowIfNull(openingSnapshot);
        replacement = null!;
        if (!editor.IsEnabled || editor.IsReadOnly || !IsValidDimension(width)
            || !IsValidDimension(height)
            || !WriterInlineInsertion.TryGetImage(container, out var liveImage)
            || !WriterInlineInsertion.IsInlineInDocument(editor.Document, container)
            || WriterInlineInsertion.GetOwnerCollection(container) is not { } owner)
            return false;

        var index = owner.Cast<Inline>().TakeWhile(inline => !ReferenceEquals(inline, container)).Count();
        if (index >= owner.Count)
            return false;
        var ownerObject = container.Parent!;
        var committedSnapshot = CloneImageElement(liveImage);
        committedSnapshot.Width = width;
        committedSnapshot.Height = height;
        RestoreDimension(liveImage, openingSnapshot, FrameworkElement.WidthProperty);
        RestoreDimension(liveImage, openingSnapshot, FrameworkElement.HeightProperty);
        replacement = new InlineUIContainer(CloneImageElement(committedSnapshot))
        {
            BaselineAlignment = container.BaselineAlignment
        };

        try
        {
            editor.BeginChange();
            owner.InsertBefore(container, replacement);
            owner.Remove(container);
        }
        catch (InvalidOperationException)
        {
            if (replacement.Parent is not null)
                owner.Remove(replacement);
            RestoreDimension(liveImage, committedSnapshot, FrameworkElement.WidthProperty);
            RestoreDimension(liveImage, committedSnapshot, FrameworkElement.HeightProperty);
            replacement = null!;
            return false;
        }
        finally
        {
            editor.EndChange();
        }

        if (!ReferenceEquals(_undoDocument, editor.Document))
            ResetUndoHistory(editor.Document);
        _resizedImages.Add(new ResizedImageUndoRecord(editor.Document, ownerObject, index,
            CloneImageElement(openingSnapshot), CloneImageElement(committedSnapshot), replacement));
        if (_resizedImages.Count > MaximumTrackedRemovals)
            _resizedImages.RemoveRange(0, _resizedImages.Count - MaximumTrackedRemovals);
        editor.Selection.Select(replacement.ElementStart, replacement.ElementEnd);
        return true;
    }

    /// <summary>
    /// Repairs WPF's empty-Grid placeholder when native Undo recreates a removed image container
    /// without its UIElement child.
    /// </summary>
    internal bool TryRestoreAfterUndo(RichTextBox editor)
    {
        ArgumentNullException.ThrowIfNull(editor);
        if (!ReferenceEquals(_undoDocument, editor.Document))
        {
            ResetUndoHistory(editor.Document);
            return false;
        }

        for (var index = _removedImages.Count - 1; index >= 0; index--)
        {
            var record = _removedImages[index];
            if (record.IsRestored || !ReferenceEquals(record.Document, editor.Document)
                || GetOwnerCollection(record.Owner) is not { } owner
                || record.Index < 0 || record.Index >= owner.Count
                || owner.ElementAt(record.Index) is not InlineUIContainer candidate)
                continue;

            if (candidate.Child is Image)
            {
                record.RestoredContainer = candidate;
                return false;
            }
            if (candidate.Child is not Grid { Children.Count: 0 })
                continue;

            var placeholder = (Grid)candidate.Child;
            placeholder.Children.Add(CloneImageElement(record.ImageSnapshot));
            record.RestoredContainer = candidate;
            _customRedo.Add(record);
            return true;
        }
        return TryRepairResizeAfterUndo(editor);
    }

    /// <summary>Updates tracked picture state after WPF reapplies one native redo unit.</summary>
    internal void NotifyRedo(RichTextBox editor)
    {
        ArgumentNullException.ThrowIfNull(editor);
        if (!ReferenceEquals(_undoDocument, editor.Document))
        {
            ResetUndoHistory(editor.Document);
            return;
        }
        foreach (var record in _removedImages.Where(record => record.IsRestored))
        {
            if (record.RestoredContainer?.Parent is null)
                record.RestoredContainer = null;
        }
        TryRepairResizeAfterRedo(editor);
    }

    /// <summary>Invalidates repaired redo state after a new editor-owned change.</summary>
    internal void NotifyTextChanged(RichTextBox editor)
    {
        ArgumentNullException.ThrowIfNull(editor);
        if (_textChangeSuppression == 0)
        {
            if (_customRedo.Count > 0)
                _customRedo.Clear();
            _resizedImages.RemoveAll(record => record.IsUndone);
        }
    }

    /// <summary>Clears picture-removal state at a document-lifetime boundary.</summary>
    internal void ResetUndoHistory(FlowDocument? document = null)
    {
        _removedImages.Clear();
        _customRedo.Clear();
        _resizedImages.Clear();
        _undoDocument = document;
    }

    bool IWriterEditingUndoExtension.CanRedo(RichTextBox editor) =>
        ReferenceEquals(_undoDocument, editor.Document)
        && _customRedo.Any(record => record.RestoredContainer?.Parent is not null);

    bool IWriterEditingUndoExtension.TryRedo(RichTextBox editor)
    {
        if (!ReferenceEquals(_undoDocument, editor.Document))
            return false;
        while (_customRedo.Count > 0)
        {
            var index = _customRedo.Count - 1;
            var record = _customRedo[index];
            _customRedo.RemoveAt(index);
            if (record.RestoredContainer is not { Parent: not null } container
                || !WriterInlineInsertion.IsInlineInDocument(editor.Document, container))
                continue;

            _textChangeSuppression++;
            try
            {
                if (!WriterInlineInsertion.TryRemoveInline(editor, container))
                    return false;
                record.RestoredContainer = null;
                return true;
            }
            finally
            {
                _textChangeSuppression--;
            }
        }
        return false;
    }

    void IWriterEditingUndoExtension.BeginHistoryTraversal() => _textChangeSuppression++;

    void IWriterEditingUndoExtension.EndHistoryTraversal()
    {
        if (_textChangeSuppression > 0)
            _textChangeSuppression--;
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

    private bool TryRepairResizeAfterUndo(RichTextBox editor)
    {
        for (var index = _resizedImages.Count - 1; index >= 0; index--)
        {
            var record = _resizedImages[index];
            if (record.IsUndone || !ReferenceEquals(record.Document, editor.Document)
                || record.CommittedContainer.Parent is not null
                || GetOwnerCollection(record.Owner) is not { } owner
                || record.Index < 0 || record.Index >= owner.Count
                || owner.ElementAt(record.Index) is not InlineUIContainer candidate)
                continue;
            var repaired = RepairCandidate(candidate, record.OpeningSnapshot);
            record.UndoneContainer = candidate;
            record.IsUndone = true;
            return repaired;
        }
        return false;
    }

    private void TryRepairResizeAfterRedo(RichTextBox editor)
    {
        for (var index = _resizedImages.Count - 1; index >= 0; index--)
        {
            var record = _resizedImages[index];
            if (!record.IsUndone || !ReferenceEquals(record.Document, editor.Document)
                || record.UndoneContainer?.Parent is not null
                || GetOwnerCollection(record.Owner) is not { } owner
                || record.Index < 0 || record.Index >= owner.Count
                || owner.ElementAt(record.Index) is not InlineUIContainer candidate)
                continue;
            RepairCandidate(candidate, record.CommittedSnapshot);
            record.CommittedContainer = candidate;
            record.UndoneContainer = null;
            record.IsUndone = false;
            return;
        }
    }

    private static bool RepairCandidate(InlineUIContainer candidate, Image snapshot)
    {
        if (candidate.Child is Image)
            return false;
        if (candidate.Child is not Grid { Children.Count: 0 } placeholder)
            return false;
        placeholder.Children.Add(CloneImageElement(snapshot));
        return true;
    }

    private static void RestoreDimension(Image target, Image snapshot, DependencyProperty property)
    {
        var value = snapshot.ReadLocalValue(property);
        if (value == DependencyProperty.UnsetValue)
            target.ClearValue(property);
        else
            target.SetValue(property, value);
    }

    private static InlineCollection? GetOwnerCollection(DependencyObject owner) => owner switch
    {
        Paragraph paragraph => paragraph.Inlines,
        Span span => span.Inlines,
        _ => null
    };

    internal static Image CloneImageElement(Image source)
    {
        var clone = new Image { IsHitTestVisible = false };
        foreach (var property in new[]
                 {
                     Image.SourceProperty,
                     Image.StretchProperty,
                     Image.StretchDirectionProperty,
                     FrameworkElement.WidthProperty,
                     FrameworkElement.HeightProperty,
                     FrameworkElement.MinWidthProperty,
                     FrameworkElement.MinHeightProperty,
                     FrameworkElement.MaxWidthProperty,
                     FrameworkElement.MaxHeightProperty,
                     FrameworkElement.HorizontalAlignmentProperty,
                     FrameworkElement.VerticalAlignmentProperty,
                     FrameworkElement.MarginProperty,
                     FrameworkElement.FlowDirectionProperty,
                     UIElement.OpacityProperty,
                     UIElement.SnapsToDevicePixelsProperty
                 })
        {
            var value = source.ReadLocalValue(property);
            if (value != DependencyProperty.UnsetValue)
                clone.SetValue(property, value);
        }
        return clone;
    }

    private sealed class RemovedImageUndoRecord(
        FlowDocument document,
        DependencyObject owner,
        int index,
        Image imageSnapshot)
    {
        internal FlowDocument Document { get; } = document;
        internal DependencyObject Owner { get; } = owner;
        internal int Index { get; } = index;
        internal Image ImageSnapshot { get; } = imageSnapshot;
        internal InlineUIContainer? RestoredContainer { get; set; }
        internal bool IsRestored => RestoredContainer is not null;
    }

    private sealed class ResizedImageUndoRecord(
        FlowDocument document,
        DependencyObject owner,
        int index,
        Image openingSnapshot,
        Image committedSnapshot,
        InlineUIContainer committedContainer)
    {
        internal FlowDocument Document { get; } = document;
        internal DependencyObject Owner { get; } = owner;
        internal int Index { get; } = index;
        internal Image OpeningSnapshot { get; } = openingSnapshot;
        internal Image CommittedSnapshot { get; } = committedSnapshot;
        internal InlineUIContainer CommittedContainer { get; set; } = committedContainer;
        internal InlineUIContainer? UndoneContainer { get; set; }
        internal bool IsUndone { get; set; }
    }
}
