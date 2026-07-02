using System.Windows;
using System.Windows.Controls;

namespace RibbonKit.Controls;

/// <summary>
/// The tab host inside a <see cref="Ribbon"/>: a restyled <see cref="TabControl"/>
/// whose headers form the ribbon tab strip and whose content area shows the selected
/// tab's groups row. Usually created by the <see cref="Ribbon"/> template rather than
/// used directly.
/// </summary>
public class RibbonTabControl : TabControl
{
    static RibbonTabControl()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(RibbonTabControl),
            new FrameworkPropertyMetadata(typeof(RibbonTabControl)));
    }

    /// <inheritdoc />
    protected override bool IsItemItsOwnContainerOverride(object item) => item is RibbonTab;

    /// <inheritdoc />
    protected override DependencyObject GetContainerForItemOverride() => new RibbonTab();
}
