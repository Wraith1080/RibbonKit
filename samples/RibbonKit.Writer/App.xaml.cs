using System.Windows;
using RibbonKit.Animation;

namespace RibbonKit.Writer;

/// <summary>
/// Application bootstrap for the RibbonKit Writer reference application.
/// </summary>
public partial class App : Application
{
    /// <inheritdoc />
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        RibbonAnimation.Initialize(this);
    }
}
