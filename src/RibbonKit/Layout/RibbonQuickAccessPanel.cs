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
                    _overflowFocus.Add(child, new OverflowFocusState(child));
                child.Arrange(new Rect(0, 0, 0, 0));
                continue;
            }

            child.Arrange(new Rect(x, 0, child.DesiredSize.Width, finalSize.Height));
            x += child.DesiredSize.Width;
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
