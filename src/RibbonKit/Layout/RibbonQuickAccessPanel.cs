using System.Windows;
using System.Windows.Controls;
using RibbonKit.Controls;

namespace RibbonKit.Layout;

/// <summary>
/// Items panel for a <see cref="RibbonQuickAccessToolBar"/>: lays its children out in a single
/// row and reports the ones that don't fit, so the toolbar can offer them through an overflow
/// button instead of letting the strip run off the edge.
/// </summary>
/// <remarks>
/// <para>
/// The reason this isn't a <see cref="StackPanel"/>: a horizontal StackPanel measures its children
/// with INFINITE width in the stacking direction, so it can never know that it has run out of room.
/// This panel measures each child at its natural width but honours the finite width it is given,
/// and everything past that point becomes overflow.
/// </para>
/// <para>
/// It deliberately does NOT reserve space for the overflow button itself. The toolbar's template
/// docks the button to the right of the presenter, so once the button becomes visible the panel is
/// simply measured with that much less width on the next pass. Reserving it here as well would
/// double-count. The two-pass settle converges because overflow only ever grows when the width
/// shrinks — it never flips back and forth.
/// </para>
/// </remarks>
public class RibbonQuickAccessPanel : Panel
{
    private readonly List<UIElement> _overflow = new();

    /// <summary>The children that did not fit, in their original order.</summary>
    internal IReadOnlyList<UIElement> OverflowedChildren => _overflow;

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        double natural = 0;
        double height = 0;

        foreach (UIElement child in InternalChildren)
        {
            // Natural width: what the button wants, never squeezed. A QAT item that had to shrink
            // to fit would be worse than one moved into the overflow menu.
            child.Measure(new Size(double.PositiveInfinity, availableSize.Height));
            natural += child.DesiredSize.Width;
            height = Math.Max(height, child.DesiredSize.Height);
        }

        _overflow.Clear();

        // An unconstrained host (below the ribbon, where the strip owns a full-width row) never
        // overflows — which is exactly the behaviour that placement wants.
        if (double.IsInfinity(availableSize.Width) || natural <= availableSize.Width)
        {
            UpdateOwner();
            return new Size(natural, height);
        }

        double used = 0;
        bool overflowing = false;

        foreach (UIElement child in InternalChildren)
        {
            if (overflowing)
            {
                _overflow.Add(child);
                continue;
            }

            double width = child.DesiredSize.Width;
            if (used + width > availableSize.Width)
            {
                overflowing = true;
                _overflow.Add(child);
                continue;
            }

            used += width;
        }

        UpdateOwner();
        return new Size(used, height);
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize)
    {
        double x = 0;

        foreach (UIElement child in InternalChildren)
        {
            if (_overflow.Contains(child))
            {
                // Zero-sized rather than Collapsed: Visibility is the app's to own (a QAT item may
                // legitimately be hidden), and collapsing here would fight that. A zero rect keeps
                // the element out of sight and out of hit-testing without touching its state.
                child.Arrange(new Rect(0, 0, 0, 0));
                continue;
            }

            child.Arrange(new Rect(x, 0, child.DesiredSize.Width, finalSize.Height));
            x += child.DesiredSize.Width;
        }

        return finalSize;
    }

    private void UpdateOwner()
    {
        if (ItemsControl.GetItemsOwner(this) is RibbonQuickAccessToolBar toolBar)
        {
            toolBar.OnOverflowChanged(this, _overflow.Count > 0);
        }
    }
}
