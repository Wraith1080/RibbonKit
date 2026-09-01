using System.Collections.Immutable;
using System.Windows;
using System.Windows.Documents;

namespace RibbonKit.Writer.Pagination;

internal enum WriterPaginationObjectKind
{
    Table,
    Picture,
    Hyperlink
}

internal enum WriterPaginationResizeHandleKind
{
    PictureTopLeft,
    PictureTop,
    PictureTopRight,
    PictureRight,
    PictureBottomRight,
    PictureBottom,
    PictureBottomLeft,
    PictureLeft,
    TableColumn,
    TableRow,
    TableOverall
}

internal enum WriterPaginationResizePhase
{
    Start,
    Update,
    Commit,
    Cancel
}

internal readonly record struct WriterPaginationRectangle(
    double X,
    double Y,
    double Width,
    double Height)
{
    internal Rect ToRect() => new(X, Y, Width, Height);
}

internal readonly record struct WriterPaginationInsertionGeometry(
    int SourceOffset,
    int PageNumber,
    WriterPaginationRectangle Rectangle);

internal readonly record struct WriterPaginationObjectCapture(
    long ObjectIdentity,
    WriterPaginationObjectKind Kind,
    int StartOffset,
    int EndOffset);

internal readonly record struct WriterPaginationObjectGeometry(
    long ObjectIdentity,
    WriterPaginationObjectKind Kind,
    int SourceOffset,
    int PageNumber,
    WriterPaginationRectangle Rectangle);

internal readonly record struct WriterPaginationTableRowBoundary(
    int RowGroupIndex,
    int RowIndex,
    double PositionDip);

internal sealed record WriterPaginationTableGeometry(
    long ObjectIdentity,
    int PageNumber,
    WriterPaginationRectangle Bounds,
    ImmutableArray<double> ColumnBoundaries,
    bool HasTrustedColumnBoundaries,
    ImmutableArray<WriterPaginationTableRowBoundary> RowBoundaries,
    bool IsFirstFragment,
    bool IsLastFragment);

internal readonly record struct WriterPaginationResizeInteraction(
    long Generation,
    long DocumentIdentity,
    int PageNumber,
    long ObjectIdentity,
    WriterPaginationObjectKind ObjectKind,
    WriterPaginationResizeHandleKind Handle,
    WriterPaginationResizePhase Phase,
    double DeltaX,
    double DeltaY,
    int HandleIndex = -1,
    int RowGroupIndex = -1);

internal sealed record WriterPaginationFormatting(
    string FontFamily,
    double FontSize,
    int FontWeight,
    int FontStretch,
    string Language,
    FlowDirection FlowDirection,
    TextAlignment TextAlignment,
    double LineHeight,
    LineStackingStrategy LineStackingStrategy,
    bool IsHyphenationEnabled,
    bool IsOptimalParagraphEnabled,
    string? BackgroundXaml,
    string? ForegroundXaml,
    string? TextEffectsXaml,
    string? ColumnRuleBrushXaml,
    double ColumnRuleWidth);

internal readonly record struct WriterPaginationPageSettings(
    double WidthDip,
    double HeightDip,
    double ContentWidthDip,
    double LeftMarginDip,
    double TopMarginDip,
    double RightMarginDip,
    double BottomMarginDip);

internal sealed record WriterPaginationCapture(
    long Generation,
    long DocumentIdentity,
    int VisiblePage,
    ImmutableArray<byte> XamlPackage,
    WriterPaginationFormatting Formatting,
    WriterPaginationPageSettings PageSettings,
    double PixelScaleX,
    double PixelScaleY,
    ImmutableArray<WriterPaginationObjectCapture> StructuredObjects);

internal sealed record WriterPaginationPage(
    int PageNumber,
    ImmutableArray<byte> PngBytes);

internal sealed record WriterPaginationLayoutResult(
    long Generation,
    long DocumentIdentity,
    int VisiblePage,
    int PageCount,
    ImmutableArray<int> PageStartOffsets,
    ImmutableArray<int> MappedPages,
    ImmutableArray<WriterPaginationPage> Pages,
    ImmutableArray<WriterPaginationInsertionGeometry> Insertions,
    ImmutableArray<WriterPaginationObjectGeometry> StructuredObjects,
    ImmutableArray<WriterPaginationTableGeometry> Tables,
    WriterPaginationPageSettings PageSettings,
    int WorkerThreadId,
    ApartmentState WorkerApartment,
    double WorkerMilliseconds);

internal enum WriterPaginationCompletionKind
{
    Completed,
    SupersededBeforeStart,
    CanceledAfterStart
}

internal sealed record WriterPaginationCompletion(
    WriterPaginationCompletionKind Kind,
    WriterPaginationLayoutResult? Result,
    int CompletedMappedPages);

internal readonly record struct WriterPaginationWorkStatistics(
    int StartedCount,
    int CompletedCount,
    int CanceledActiveCount,
    int SupersededPendingCount);

internal readonly record struct WriterPaginationPageInteraction(
    long Generation,
    long DocumentIdentity,
    int PageNumber,
    Point PagePoint,
    long? ObjectIdentity,
    WriterPaginationObjectKind? ObjectKind);
