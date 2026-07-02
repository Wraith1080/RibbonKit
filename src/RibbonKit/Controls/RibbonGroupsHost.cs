using System.Windows;
using System.Windows.Controls;

namespace RibbonKit.Controls;

/// <summary>
/// The ItemsControl that hosts a tab's groups on the adaptive
/// <see cref="Layout.RibbonGroupsPanel"/>. Created by <see cref="RibbonTab"/>;
/// not intended for direct use.
/// </summary>
public class RibbonGroupsHost : ItemsControl
{
    static RibbonGroupsHost()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(RibbonGroupsHost),
            new FrameworkPropertyMetadata(typeof(RibbonGroupsHost)));
    }
}
