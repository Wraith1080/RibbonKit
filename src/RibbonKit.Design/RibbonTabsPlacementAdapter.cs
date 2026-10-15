using System.Windows;
using Microsoft.VisualStudio.DesignTools.Extensibility.Interaction;
using Microsoft.VisualStudio.DesignTools.Extensibility.Model;

namespace RibbonKit.Design;

/// <summary>
/// Keeps ribbon, tab and group children in their collection-owned layout instead of assigning
/// free-form coordinates or dimensions during a Toolbox drop.
/// </summary>
public sealed class RibbonTabsPlacementAdapter : PlacementAdapter
{
    /// <inheritdoc />
    public override bool CanSetPosition(PlacementIntent intent, RelativePosition position) => false;

    /// <inheritdoc />
    public override RelativeValueCollection GetPlacement(ModelItem item, params RelativePosition[] positions)
    {
        var values = new RelativeValueCollection();
        Rect bounds = GetPlacementBoundary(item);
        if (bounds.IsEmpty) return values;
        foreach (var position in positions)
        {
            if (position == RelativePositions.LeftSide) values.Add(new RelativeValue(position, bounds.Left));
            else if (position == RelativePositions.RightSide) values.Add(new RelativeValue(position, bounds.Right));
            else if (position == RelativePositions.TopSide) values.Add(new RelativeValue(position, bounds.Top));
            else if (position == RelativePositions.BottomSide) values.Add(new RelativeValue(position, bounds.Bottom));
        }
        return values;
    }

    // RibbonKit owns collection arrangement. These callbacks must not write Width,
    // Height, Margin or alignment, nor clear existing authored assignments.
    /// <inheritdoc />
    public override void SetPlacements(ModelItem item, PlacementIntent intent, params RelativeValue[] positions) { }

    /// <inheritdoc />
    public override void SetPlacements(ModelItem item, PlacementIntent intent, RelativeValueCollection placement) { }

    /// <inheritdoc />
    public override Rect GetPlacementBoundary(ModelItem item) => item.View?.SelectionFrameBounds ?? Rect.Empty;

    /// <inheritdoc />
    public override Rect GetPlacementBoundary(ModelItem item, PlacementIntent intent, params RelativeValue[] positions) =>
        GetPlacementBoundary(item);
}
