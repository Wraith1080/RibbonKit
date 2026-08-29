using System.Windows;
using System.Windows.Controls;

namespace RibbonKit.Controls;

/// <summary>
/// A non-interactive, theme-owned divider between command clusters inside a
/// <see cref="RibbonGroup"/>.
/// </summary>
/// <remarks>
/// Place the separator between children of a horizontal group layout. It follows the owning
/// group's adaptive size state; when the group is collapsed, the separator returns to its large
/// presentation with the rest of the content in the group flyout. It is deliberately not a
/// command, focus target, KeyTip target, quick-access source, or UI Automation control element.
/// </remarks>
public class RibbonGroupSeparator : Control, IRibbonSizeAware
{
    private static readonly DependencyPropertyKey SizeStatePropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(SizeState),
            typeof(RibbonGroupSizeState),
            typeof(RibbonGroupSeparator),
            new FrameworkPropertyMetadata(
                RibbonGroupSizeState.Large,
                FrameworkPropertyMetadataOptions.AffectsMeasure));

    /// <summary>Identifies the read-only <see cref="SizeState"/> dependency property.</summary>
    public static readonly DependencyProperty SizeStateProperty = SizeStatePropertyKey.DependencyProperty;

    static RibbonGroupSeparator()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(RibbonGroupSeparator),
            new FrameworkPropertyMetadata(typeof(RibbonGroupSeparator)));
    }

    /// <summary>
    /// The effective adaptive size of the separator. Collapsed groups expose their content in a
    /// full-size flyout, so <see cref="RibbonGroupSizeState.Collapsed"/> maps to
    /// <see cref="RibbonGroupSizeState.Large"/> here.
    /// </summary>
    public RibbonGroupSizeState SizeState => (RibbonGroupSizeState)GetValue(SizeStateProperty);

    /// <inheritdoc />
    void IRibbonSizeAware.ApplySizeState(RibbonGroupSizeState state) =>
        SetValue(
            SizeStatePropertyKey,
            state == RibbonGroupSizeState.Collapsed ? RibbonGroupSizeState.Large : state);
}
