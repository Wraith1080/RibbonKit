using System.Windows;
using System.Windows.Controls;
using RibbonKit.Controls;

namespace RibbonKit.Showcase;

/// <summary>Attaches only the host-owned captured backdrop to the shared menu surface.</summary>
internal sealed class CrystalApplicationMenuBackdrop
{
    private readonly RibbonApplicationMenu _menu;
    private readonly FrameworkElement _owner;
    private CrystalMenuBackdrop? _backdrop;
    private Border? _frame;
    private bool _enabled;

    public CrystalApplicationMenuBackdrop(RibbonApplicationMenu menu, FrameworkElement owner)
    {
        _menu = menu;
        _owner = owner;
        menu.Loaded += (_, _) => Update();
        menu.Unloaded += (_, _) => Remove();
        if (owner is Window window) window.Closed += (_, _) => Remove();
    }

    public void Apply(bool enabled)
    {
        _enabled = enabled;
        if (enabled) _menu.ApplyTemplate();
        Update();
    }

    private void Update()
    {
        if (!_enabled) { Remove(); return; }
        if (_menu.Template?.FindName("PART_Frame", _menu) is not Border host ||
            _menu.Template.FindName("Frame", _menu) is not Border frame ||
            _menu.Template.FindName("MenuSurface", _menu) is not Grid surface) return;
        if (!ReferenceEquals(frame, _frame))
        {
            Remove();
            _frame = frame;
            _backdrop = new CrystalMenuBackdrop(_owner, host, frame, surface);
        }
        _backdrop?.Refresh();
    }

    private void Remove()
    {
        _backdrop?.Remove();
        _backdrop = null;
        _frame = null;
    }
}
