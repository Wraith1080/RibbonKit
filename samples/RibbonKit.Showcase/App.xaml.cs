using System.Windows;
using RibbonKit.Animation;

namespace RibbonKit.Showcase;

/// <summary>
/// Showcase application — grows a page per feature as the library develops.
/// </summary>
public partial class App : Application
{
    /// <inheritdoc />
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Register the app so RibbonKit can publish its animation-duration tokens and honor
        // the global level (default Subtle). Optional — controls animate via code-behind too.
        RibbonAnimation.Initialize(this);
    }
}
