using System.Windows;
using System.Windows.Controls;
using RibbonKit.Controls;

namespace RibbonKit.Layout;

/// <summary>
/// The items panel behind <see cref="MdiContainer"/>. Places each
/// <see cref="MdiChild"/> by its state: Normal at (<see cref="MdiChild.Left"/>,
/// <see cref="MdiChild.Top"/>) clamped so the caption stays reachable; Maximized
/// filling the panel; Minimized stacked as caption strips along the bottom edge,
/// in item order.
/// </summary>
public class MdiCanvasPanel : Panel
{
    // Gap between minimized caption strips and from the panel edges.
    private const double MinimizedGap = 4;

    // How much of a child must remain visible inside the panel.
    private const double KeepVisible = 48;

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        bool finite = !double.IsInfinity(availableSize.Width) && !double.IsInfinity(availableSize.Height);
        var unbounded = new Size(double.PositiveInfinity, double.PositiveInfinity);

        foreach (UIElement element in InternalChildren)
        {
            if (element is null)
            {
                continue;
            }

            // A maximized child measures at panel size so its content lays out to the
            // real client area; everything else measures unbounded so its explicit
            // Width/Height (or collapsed caption) decides.
            if (finite && element is MdiChild { WindowState: WindowState.Maximized })
            {
                element.Measure(availableSize);
            }
            else
            {
                element.Measure(unbounded);
            }
        }

        if (finite)
        {
            return availableSize;
        }

        // Unbounded constraint (unusual — e.g. a ScrollViewer): report the union of the
        // children's placements so nothing is clipped away.
        double width = 0;
        double height = 0;
        foreach (UIElement element in InternalChildren)
        {
            if (element is null)
            {
                continue;
            }

            double left = element is MdiChild c && !double.IsNaN(c.Left) ? Math.Max(0, c.Left) : 0;
            double top = element is MdiChild c2 && !double.IsNaN(c2.Top) ? Math.Max(0, c2.Top) : 0;
            width = Math.Max(width, left + element.DesiredSize.Width);
            height = Math.Max(height, top + element.DesiredSize.Height);
        }

        return new Size(width, height);
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize)
    {
        double minimizedX = MinimizedGap;

        foreach (UIElement element in InternalChildren)
        {
            if (element is null)
            {
                continue;
            }

            if (element is not MdiChild child)
            {
                element.Arrange(new Rect(element.DesiredSize));
                continue;
            }

            switch (child.WindowState)
            {
                case WindowState.Maximized:
                    child.Arrange(new Rect(finalSize));
                    break;

                case WindowState.Minimized:
                {
                    Size desired = child.DesiredSize;
                    double y = Math.Max(0, finalSize.Height - desired.Height - MinimizedGap);
                    child.Arrange(new Rect(minimizedX, y, desired.Width, desired.Height));
                    minimizedX += desired.Width + MinimizedGap;
                    break;
                }

                default:
                {
                    Size desired = child.DesiredSize;
                    double left = double.IsNaN(child.Left) ? 0 : child.Left;
                    double top = double.IsNaN(child.Top) ? 0 : child.Top;

                    // Clamp so at least KeepVisible of the child stays inside the panel
                    // (a container resize must never strand a child out of reach).
                    left = Math.Min(left, Math.Max(0, finalSize.Width - KeepVisible));
                    left = Math.Max(left, KeepVisible - desired.Width);
                    top = Math.Min(top, Math.Max(0, finalSize.Height - KeepVisible));
                    top = Math.Max(0, top);

                    child.Arrange(new Rect(left, top, desired.Width, desired.Height));
                    break;
                }
            }
        }

        return finalSize;
    }
}
