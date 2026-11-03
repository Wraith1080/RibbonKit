using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

public partial class DesignerForm
{
    private void MouseModeOption_Click(object? sender, EventArgs e)
    {
        ribbon.Density = RibbonDensity.Compact;
        SynchronizeInputMode();
    }

    private void TouchModeOption_Click(object? sender, EventArgs e)
    {
        ribbon.Density = RibbonDensity.Touch;
        SynchronizeInputMode();
    }

    private void Ribbon_DensityChanged(object? sender, EventArgs e)
    {
        SynchronizeInputMode();
        statusLabel.Text = ribbon.Density == RibbonDensity.Touch ? "Touch spacing" : "Mouse spacing";
    }

    private void SynchronizeInputMode()
    {
        bool mouse = ribbon.Density == RibbonDensity.Compact;
        mouseModeOption.Checked = mouse;
        touchModeOption.Checked = !mouse;
        inputModeSelector.Text = mouse ? "Mouse" : "Touch";
        var icon = mouse ? mouseModeOption.Icon : touchModeOption.Icon;
        // Reassign only when the choice changes, preserving an active transition.
        if (!ReferenceEquals(inputModeSelector.Icon, icon)) inputModeSelector.Icon = icon;
    }
}
