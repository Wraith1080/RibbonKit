using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace RibbonKit.Controls;

// Completes an activation whose checked/selected state has already been changed.
// Raising Click alone does not run ButtonBase's command stage; calling OnClick would toggle again.
internal static class ButtonCommandInvocation
{
    internal static void RaiseClickAndExecute(ButtonBase source)
    {
        source.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, source));
        var command = source.Command;
        var parameter = source.CommandParameter;
        if (command is RoutedCommand routed)
        {
            var target = source.CommandTarget ?? source;
            if (routed.CanExecute(parameter, target)) routed.Execute(parameter, target);
        }
        else if (command?.CanExecute(parameter) == true)
            command.Execute(parameter);
    }
}
