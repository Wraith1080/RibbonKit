using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Documents;

namespace RibbonKit.Writer.Editing;

/// <summary>Projects persisted two-sided table placement onto an available editor width.</summary>
internal static class WriterTableMarginProjection
{
    private const double PlacementToleranceDip = 0.5;
    private static readonly ConditionalWeakTable<Table, ProjectionState> States = new();
    private const BindingFlags InstanceMembers = BindingFlags.Instance
        | BindingFlags.Public | BindingFlags.NonPublic;
    private static readonly PropertyInfo TextContainerProperty = typeof(TextPointer)
        .GetProperty("TextContainer", InstanceMembers)
        ?? throw new MissingMemberException(typeof(TextPointer).FullName, "TextContainer");
    private static readonly PropertyInfo UndoManagerProperty = TextContainerProperty.PropertyType
        .GetProperty("UndoManager", InstanceMembers)
        ?? throw new MissingMemberException(TextContainerProperty.PropertyType.FullName, "UndoManager");
    private static readonly PropertyInfo UndoCountProperty = UndoManagerProperty.PropertyType
        .GetProperty("UndoCount", InstanceMembers)
        ?? throw new MissingMemberException(UndoManagerProperty.PropertyType.FullName, "UndoCount");
    private static readonly PropertyInfo RedoStackProperty = UndoManagerProperty.PropertyType
        .GetProperty("RedoStack", InstanceMembers)
        ?? throw new MissingMemberException(UndoManagerProperty.PropertyType.FullName, "RedoStack");
    private static readonly MethodInfo PopUndoStackMethod = UndoManagerProperty.PropertyType
        .GetMethod("PopUndoStack", InstanceMembers, Type.EmptyTypes)
        ?? throw new MissingMethodException(UndoManagerProperty.PropertyType.FullName, "PopUndoStack");
    private static readonly MethodInfo SetRedoStackMethod = UndoManagerProperty.PropertyType
        .GetMethod("SetRedoStack", InstanceMembers, [typeof(Stack)])
        ?? throw new MissingMethodException(UndoManagerProperty.PropertyType.FullName, "SetRedoStack");

    internal static void Project(Table table, WriterTableHorizontalAlignment alignment,
        double tableWidth, double availableWidth)
    {
        ArgumentNullException.ThrowIfNull(table);
        Apply(table, alignment, tableWidth, availableWidth);
        Remember(table, alignment);
    }

    internal static void Project(Table table, double tableWidth, double availableWidth)
    {
        ArgumentNullException.ThrowIfNull(table);
        var margin = table.Margin;
        if (!AreFinite(margin))
            return;
        var state = States.GetOrCreateValue(table);
        var alignment = state.IsInitialized && state.ProjectedMargin == margin
            ? state.Alignment
            : InferAlignment(margin, tableWidth, availableWidth);
        Apply(table, alignment, tableWidth, availableWidth);
        Remember(table, alignment);
    }

    internal static void ProjectWithoutUndo(FlowDocument document, Table table,
        double tableWidth, double availableWidth)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(table);
        var textContainer = TextContainerProperty.GetValue(document.ContentStart)
            ?? throw new InvalidOperationException("The Writer document has no text container.");
        var undoManager = UndoManagerProperty.GetValue(textContainer);
        var undoCount = undoManager is null ? 0 : (int)UndoCountProperty.GetValue(undoManager)!;
        var redoStack = undoManager is null
            ? null
            : (Stack)((Stack)RedoStackProperty.GetValue(undoManager)!).Clone();

        Project(table, tableWidth, availableWidth);

        if (undoManager is not null)
        {
            while ((int)UndoCountProperty.GetValue(undoManager)! > undoCount)
                PopUndoStackMethod.Invoke(undoManager, null);
            SetRedoStackMethod.Invoke(undoManager, [redoStack!]);
        }
    }

    private static void Apply(Table table, WriterTableHorizontalAlignment alignment,
        double tableWidth, double availableWidth)
    {
        var margin = table.Margin;
        var remaining = Math.Max(0, availableWidth - tableWidth);
        var (left, right) = alignment switch
        {
            WriterTableHorizontalAlignment.Left => (0d, remaining),
            WriterTableHorizontalAlignment.Center => (remaining / 2d, remaining / 2d),
            WriterTableHorizontalAlignment.Right => (remaining, 0d),
            _ => throw new ArgumentOutOfRangeException(nameof(alignment))
        };
        var projected = new Thickness(left, margin.Top, right, margin.Bottom);
        if (margin != projected)
            table.Margin = projected;
    }

    private static WriterTableHorizontalAlignment InferAlignment(Thickness margin,
        double tableWidth, double availableWidth)
    {
        if (NearlyEqual(margin.Left, margin.Right))
            return WriterTableHorizontalAlignment.Center;
        if (margin.Left <= PlacementToleranceDip && margin.Right > PlacementToleranceDip)
            return WriterTableHorizontalAlignment.Left;
        if (margin.Right <= PlacementToleranceDip && margin.Left > PlacementToleranceDip)
        {
            var remaining = Math.Max(0, availableWidth - tableWidth);
            return NearlyEqual(margin.Left, remaining / 2d)
                ? WriterTableHorizontalAlignment.Center
                : WriterTableHorizontalAlignment.Right;
        }
        return margin.Left < margin.Right
            ? WriterTableHorizontalAlignment.Left
            : WriterTableHorizontalAlignment.Right;
    }

    private static bool AreFinite(Thickness margin) =>
        double.IsFinite(margin.Left) && double.IsFinite(margin.Top)
            && double.IsFinite(margin.Right) && double.IsFinite(margin.Bottom);

    private static bool NearlyEqual(double left, double right) =>
        Math.Abs(left - right) <= PlacementToleranceDip;

    private static void Remember(Table table, WriterTableHorizontalAlignment alignment)
    {
        var state = States.GetOrCreateValue(table);
        state.Alignment = alignment;
        state.ProjectedMargin = table.Margin;
        state.IsInitialized = true;
    }

    private sealed class ProjectionState
    {
        internal WriterTableHorizontalAlignment Alignment;
        internal Thickness ProjectedMargin;
        internal bool IsInitialized;
    }
}
