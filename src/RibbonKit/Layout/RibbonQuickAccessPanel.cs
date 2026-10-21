using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
[System.ComponentModel.ToolboxItem(false)]
[System.ComponentModel.DesignTimeVisible(false)]
public class RibbonQuickAccessPanel : Panel
{
    private readonly List<UIElement> _overflow = new();
    private readonly Dictionary<UIElement, OverflowFocusState> _overflowFocus = new();

    /// <summary>Initializes a new <see cref="RibbonQuickAccessPanel"/>.</summary>
    public RibbonQuickAccessPanel()
    {
        Unloaded += (_, _) => RestoreOverflowFocus();
        IsVisibleChanged += (_, _) =>
        {
            if (!IsVisible) RestoreOverflowFocus();
            else InvalidateArrange();
        };
    }

    internal void RestoreOverflowFocus()
    {
        foreach (var state in _overflowFocus.Values) state.Restore();
        _overflowFocus.Clear();
        InvalidateArrange();
    }

    /// <summary>The children that did not fit, in their original order.</summary>
    internal IReadOnlyList<UIElement> OverflowedChildren => _overflow;

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        if (ItemsControl.GetItemsOwner(this) is RibbonQuickAccessToolBar { IsSimplified: true } simplified)
            return MeasureSimplified(availableSize, simplified);
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

    private Size MeasureSimplified(Size availableSize, RibbonQuickAccessToolBar owner)
    {
        var visible = InternalChildren.Cast<UIElement>()
            .Where(child => child.Visibility == Visibility.Visible).ToList();
        foreach (UIElement child in visible) child.Measure(new Size(double.PositiveInfinity, availableSize.Height));
        var inline = visible.Where(child => child is not RibbonGroupQuickAccessProxy and not RibbonGroupSeparator).ToList();
        var preceding = new Dictionary<UIElement, RibbonGroupSeparator>();
        RibbonGroupSeparator? section = null;
        foreach (UIElement child in InternalChildren)
        {
            if (child is RibbonGroupSeparator separator) section = separator;
            else if (section is not null) preceding[child] = section;
        }
        List<UIElement> WithSeparators()
        {
            var kept = new List<UIElement>();
            RibbonGroup? previous = null;
            foreach (UIElement child in inline)
            {
                if (child is not FrameworkElement element || !owner.SimplifiedGroups.TryGetValue(element, out RibbonGroup? group)) continue;
                if (previous is not null && previous != group)
                {
                    var divider = preceding.GetValueOrDefault(child);
                    if (divider is not null) kept.Add(divider);
                }
                kept.Add(child);
                previous = group;
            }
            return kept;
        }
        var kept = WithSeparators();
        foreach (UIElement child in inline.OrderBy(Ribbon.GetSimplifiedPriority)
                     .ThenByDescending(child => InternalChildren.IndexOf(child)).ToArray())
        {
            if (SimplifiedSpan(kept, availableSize.Height) <= availableSize.Width) break;
            inline.Remove(child);
            kept = WithSeparators();
        }
        _overflow.Clear();
        foreach (UIElement child in InternalChildren)
            if (!kept.Contains(child)) _overflow.Add(child);
        owner.OnOverflowChanged(this, _overflow.Any(child => child.Visibility == Visibility.Visible && child is not RibbonGroupSeparator));
        return new Size(SimplifiedSpan(kept, availableSize.Height),
            kept.Count == 0 ? 0 : kept.Max(child => child.DesiredSize.Height));
    }

    private double SimplifiedSpan(IEnumerable<UIElement> items, double rowHeight)
    {
        double width = 0;
        UIElement? previous = null;
        foreach (UIElement item in items)
        {
            width += SimplifiedGap(previous, item) + SimplifiedWidth(item, rowHeight);
            previous = item;
        }
        return width;
    }

    // A divider already supplies clearance on both sides. Add row spacing only
    // between neighboring commands, with the same budget for measure/overflow.
    private double SimplifiedGap(UIElement? previous, UIElement item) =>
        previous is null or RibbonGroupSeparator || item is RibbonGroupSeparator
            ? 0 : Math.Max(0, (double)GetValue(RibbonSimplifiedRow.SpacingProperty));

    private static double SimplifiedWidth(UIElement child, double rowHeight)
    {
        if (child is not (RibbonButton { Size: RibbonControlSize.Small } or RibbonToggleButton { Size: RibbonControlSize.Small })
            || child is not FrameworkElement element || !double.IsNaN(element.Width) || !double.IsFinite(rowHeight))
            return child.DesiredSize.Width;

        // Small actions stretch to the row's height. Reserve the same width for
        // their chrome before deciding overflow; outer margins need not be equal.
        Thickness margin = element.Margin;
        double height = Math.Max(0, rowHeight - margin.Top - margin.Bottom);
        double width = Math.Max(element.MinWidth, Math.Min(height, element.MaxWidth));
        return Math.Max(child.DesiredSize.Width, width + margin.Left + margin.Right);
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize)
    {
        double x = 0;
        bool simplified = ItemsControl.GetItemsOwner(this) is RibbonQuickAccessToolBar { IsSimplified: true };
        UIElement? previous = null;

        foreach (var child in _overflowFocus.Keys.ToArray())
        {
            if (IsVisible && InternalChildren.Contains(child) && _overflow.Contains(child)) continue;
            _overflowFocus[child].Restore();
            _overflowFocus.Remove(child);
        }

        foreach (UIElement child in InternalChildren)
        {
            if (_overflow.Contains(child))
            {
                // Zero-sized rather than Collapsed: Visibility is the app's to own (a QAT item may
                // legitimately be hidden), and collapsing here would fight that. A zero rect keeps
                // the element out of sight and out of hit-testing. Its original subtree also
                // needs to leave keyboard navigation while a separate overflow proxy is used.
                if (IsVisible && !_overflowFocus.ContainsKey(child))
                {
                    if (child is RibbonComboBoxSimplifiedProxy editor && ItemsControl.GetItemsOwner(this) is RibbonQuickAccessToolBar toolBar)
                        editor.OnOverflow(toolBar.OverflowButton);
                    _overflowFocus.Add(child, new OverflowFocusState(child));
                }
                child.Arrange(new Rect(0, 0, 0, 0));
                continue;
            }

            double width = simplified ? SimplifiedWidth(child, finalSize.Height) : child.DesiredSize.Width;
            if (simplified) x += SimplifiedGap(previous, child);
            child.Arrange(new Rect(x, 0, width, finalSize.Height));
            x += width;
            previous = child;
        }

        return finalSize;
    }

    private sealed class OverflowFocusState
    {
        private readonly UIElement _child;
        private readonly List<SuppressedValue> _values = new();

        internal OverflowFocusState(UIElement child)
        {
            _child = child;
            Suppress(UIElement.FocusableProperty, false);
            Suppress(KeyboardNavigation.TabNavigationProperty, KeyboardNavigationMode.None);
            Suppress(KeyboardNavigation.ControlTabNavigationProperty, KeyboardNavigationMode.None);
            Suppress(KeyboardNavigation.DirectionalNavigationProperty, KeyboardNavigationMode.None);
        }

        private void Suppress(DependencyProperty property, object value)
        {
            var descriptor = DependencyPropertyDescriptor.FromProperty(property, _child.GetType());
            var state = new SuppressedValue(property, descriptor);
            bool updating = false;
            void Apply()
            {
                if (updating) return;
                updating = true;
                try
                {
                    state.WasCurrent = DependencyPropertyHelper.GetValueSource(_child, property).IsCurrent;
                    state.RequestedValue = _child.GetValue(property);
                    _child.SetCurrentValue(property, value);
                }
                finally { updating = false; }
            }
            state.Changed = (_, _) => Apply();
            _values.Add(state);
            Apply();
            descriptor.AddValueChanged(_child, state.Changed);
        }

        internal void Restore()
        {
            foreach (var state in _values)
            {
                state.Descriptor.RemoveValueChanged(_child, state.Changed);
                // SetValue/binding updates can replace the override without changing the
                // effective value, in which case no change event was raised. Leave them intact.
                if (!DependencyPropertyHelper.GetValueSource(_child, state.Property).IsCurrent) continue;
                // Invalidation removes only our SetCurrentValue override. The current local
                // value, resource or binding remains owned by the application.
                _child.InvalidateProperty(state.Property);
                if (state.WasCurrent) _child.SetCurrentValue(state.Property, state.RequestedValue);
            }
        }

        private sealed class SuppressedValue(DependencyProperty property, DependencyPropertyDescriptor descriptor)
        {
            internal DependencyProperty Property { get; } = property;
            internal DependencyPropertyDescriptor Descriptor { get; } = descriptor;
            internal EventHandler Changed { get; set; } = null!;
            internal bool WasCurrent { get; set; }
            internal object RequestedValue { get; set; } = null!;
        }
    }

    private void UpdateOwner()
    {
        if (ItemsControl.GetItemsOwner(this) is RibbonQuickAccessToolBar toolBar)
        {
            toolBar.OnOverflowChanged(this, _overflow.Count > 0);
        }
    }
}
