using System.Windows;

namespace RibbonKit.Writer.Editing;

/// <summary>Identifies one physical handle on the Writer picture-selection frame.</summary>
public enum WriterPictureResizeHandle
{
    TopLeft,
    Top,
    TopRight,
    Right,
    BottomRight,
    Bottom,
    BottomLeft,
    Left
}

/// <summary>Pure DIP geometry used by pointer, ribbon, keyboard and UIA picture sizing paths.</summary>
public static class WriterPictureResizeGeometry
{
    /// <summary>The smallest supported displayed picture dimension in device-independent pixels.</summary>
    public const double MinimumDimension = 12d;

    /// <summary>Calculates a bounded picture size for one handle drag.</summary>
    public static Size Resize(Size opening, Vector delta, WriterPictureResizeHandle handle,
        Size maximum)
    {
        ValidateSize(opening, nameof(opening));
        ValidateSize(maximum, nameof(maximum));
        if (!double.IsFinite(delta.X) || !double.IsFinite(delta.Y))
            throw new ArgumentOutOfRangeException(nameof(delta), delta, "Drag deltas must be finite.");
        if (!Enum.IsDefined(handle))
            throw new ArgumentOutOfRangeException(nameof(handle), handle, "Unknown picture handle.");

        var changesWidth = handle is WriterPictureResizeHandle.TopLeft
            or WriterPictureResizeHandle.TopRight or WriterPictureResizeHandle.Right
            or WriterPictureResizeHandle.BottomRight or WriterPictureResizeHandle.BottomLeft
            or WriterPictureResizeHandle.Left;
        var changesHeight = handle is WriterPictureResizeHandle.TopLeft
            or WriterPictureResizeHandle.Top or WriterPictureResizeHandle.TopRight
            or WriterPictureResizeHandle.BottomRight or WriterPictureResizeHandle.Bottom
            or WriterPictureResizeHandle.BottomLeft;
        var horizontalSign = handle is WriterPictureResizeHandle.TopLeft
            or WriterPictureResizeHandle.BottomLeft or WriterPictureResizeHandle.Left ? -1d : 1d;
        var verticalSign = handle is WriterPictureResizeHandle.TopLeft
            or WriterPictureResizeHandle.Top or WriterPictureResizeHandle.TopRight ? -1d : 1d;

        if (changesWidth && changesHeight)
        {
            var widthScale = (opening.Width + horizontalSign * delta.X) / opening.Width;
            var heightScale = (opening.Height + verticalSign * delta.Y) / opening.Height;
            var scale = Math.Abs(widthScale - 1d) >= Math.Abs(heightScale - 1d)
                ? widthScale
                : heightScale;
            var minimumScale = Math.Max(MinimumDimension / opening.Width,
                MinimumDimension / opening.Height);
            var maximumScale = Math.Min(maximum.Width / opening.Width,
                maximum.Height / opening.Height);
            scale = Math.Clamp(scale, minimumScale, maximumScale);
            return new Size(opening.Width * scale, opening.Height * scale);
        }

        var width = changesWidth
            ? Math.Clamp(opening.Width + horizontalSign * delta.X,
                MinimumDimension, maximum.Width)
            : opening.Width;
        var height = changesHeight
            ? Math.Clamp(opening.Height + verticalSign * delta.Y,
                MinimumDimension, maximum.Height)
            : opening.Height;
        return new Size(width, height);
    }

    /// <summary>Returns pixel-aligned handle rectangles for a rendered picture size and DPI scale.</summary>
    public static IReadOnlyDictionary<WriterPictureResizeHandle, Rect> GetHandleRects(
        Size renderedSize, DpiScale dpi, double handleSize = 8d)
    {
        ValidateSize(renderedSize, nameof(renderedSize));
        if (!double.IsFinite(dpi.DpiScaleX) || !double.IsFinite(dpi.DpiScaleY)
            || dpi.DpiScaleX <= 0 || dpi.DpiScaleY <= 0)
            throw new ArgumentOutOfRangeException(nameof(dpi), dpi, "DPI scales must be finite and positive.");
        if (!double.IsFinite(handleSize) || handleSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(handleSize), handleSize,
                "Handle size must be finite and positive.");

        var width = Align(renderedSize.Width, dpi.DpiScaleX);
        var height = Align(renderedSize.Height, dpi.DpiScaleY);
        var handleWidth = Math.Max(1d / dpi.DpiScaleX, Align(handleSize, dpi.DpiScaleX));
        var handleHeight = Math.Max(1d / dpi.DpiScaleY, Align(handleSize, dpi.DpiScaleY));
        var halfWidth = handleWidth / 2d;
        var halfHeight = handleHeight / 2d;
        var centerX = width / 2d;
        var centerY = height / 2d;

        return new Dictionary<WriterPictureResizeHandle, Rect>
        {
            [WriterPictureResizeHandle.TopLeft] = At(0, 0),
            [WriterPictureResizeHandle.Top] = At(centerX, 0),
            [WriterPictureResizeHandle.TopRight] = At(width, 0),
            [WriterPictureResizeHandle.Right] = At(width, centerY),
            [WriterPictureResizeHandle.BottomRight] = At(width, height),
            [WriterPictureResizeHandle.Bottom] = At(centerX, height),
            [WriterPictureResizeHandle.BottomLeft] = At(0, height),
            [WriterPictureResizeHandle.Left] = At(0, centerY)
        };

        Rect At(double x, double y) => new(Align(x - halfWidth, dpi.DpiScaleX),
            Align(y - halfHeight, dpi.DpiScaleY),
            handleWidth, handleHeight);
    }

    private static double Align(double value, double scale) => Math.Round(value * scale) / scale;

    private static void ValidateSize(Size size, string parameterName)
    {
        if (!double.IsFinite(size.Width) || !double.IsFinite(size.Height)
            || size.Width <= 0 || size.Height <= 0)
            throw new ArgumentOutOfRangeException(parameterName, size,
                "Dimensions must be finite and positive.");
    }
}
