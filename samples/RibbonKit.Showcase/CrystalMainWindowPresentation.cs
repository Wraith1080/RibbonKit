using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using RibbonKit.Controls;

namespace RibbonKit.Showcase;

/// <summary>Applies the preview's host-owned Crystal details to the main Showcase window.</summary>
internal sealed class CrystalMainWindowPresentation
{
    private readonly RibbonWindow _window;
    private readonly Ribbon _ribbon;
    private readonly ResourceDictionary _styles = new()
    {
        Source = new Uri("/RibbonKit.Showcase;component/Themes/Crystal.ControlStyles.xaml", UriKind.Relative),
    };
    private readonly CrystalMessagePresentation _messages;
    private readonly CrystalApplicationMenuPresentation _menu;
    private readonly List<CrystalPopupBackdrop> _popups = new();

    public CrystalMainWindowPresentation(RibbonWindow window, Ribbon ribbon,
        RibbonMessageBar messageBar, RibbonApplicationMenu menu)
    {
        _window = window;
        _ribbon = ribbon;
        _messages = new CrystalMessagePresentation(messageBar);
        _menu = new CrystalApplicationMenuPresentation(menu, window);

        foreach (RibbonTab tab in ribbon.Tabs)
            foreach (RibbonGroup group in tab.Groups)
            {
                _popups.Add(new CrystalPopupBackdrop(window, group, "PART_PopupHost"));
                foreach (var dropDown in group.Items.OfType<RibbonDropDownButton>())
                    _popups.Add(new CrystalPopupBackdrop(window, dropDown, "PART_MenuHost"));
            }
    }

    public void Apply(bool enabled)
    {
        if (enabled)
        {
            if (!_window.Resources.MergedDictionaries.Contains(_styles))
                _window.Resources.MergedDictionaries.Add(_styles);
        }
        else
            _window.Resources.MergedDictionaries.Remove(_styles);

        CrystalUtilityChrome.Apply(_window, enabled);
        CrystalTabShape.Apply(_ribbon, enabled);
        CrystalQuickAccess.Apply(_ribbon, enabled);
        CrystalFileHover.Apply(_ribbon, enabled);
        _messages.Apply(enabled);
        _menu.Apply(enabled);
        foreach (var popup in _popups) popup.Apply(enabled);
    }
}
