using System.Windows;
using System.Windows.Controls;

namespace RibbonKit.Controls;

/// <summary>
/// The root Ribbon control. Hosts the tab strip, ribbon groups, application button,
/// and quick access toolbar.
/// </summary>
/// <remarks>
/// Phase 0 placeholder: renders a themed bar so the scaffold is visibly working.
/// The real tab/group structure lands in Phase 1 (see docs/03-ROADMAP.md).
/// </remarks>
public class Ribbon : Control
{
    static Ribbon()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(Ribbon),
            new FrameworkPropertyMetadata(typeof(Ribbon)));
    }
}
