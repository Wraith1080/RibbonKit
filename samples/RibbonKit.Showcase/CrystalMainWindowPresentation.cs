using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using RibbonKit.Controls;
using RibbonKit.Theming;

namespace RibbonKit.Showcase;

/// <summary>Applies the preview's host-owned Crystal details to the main Showcase window.</summary>
internal sealed class CrystalMainWindowPresentation
{
    private readonly RibbonWindow _window;
    private readonly Ribbon _ribbon;
    private readonly Backstage _backstage;
    private readonly ResourceDictionary _backstageScope = new();
    private readonly CrystalApplicationMenuBackdrop _menuBackdrop;
    private readonly Dictionary<Control, CrystalPopupBackdrop> _popups = new();
    private readonly HashSet<RibbonTab> _tabs = new();
    private ResourceDictionary? _palette;
    private bool _enabled;

    public ResourceDictionary? Palette => _palette;

    public CrystalMainWindowPresentation(RibbonWindow window, Ribbon ribbon,
        RibbonApplicationMenu menu, Backstage backstage)
    {
        _window = window;
        _ribbon = ribbon;
        _backstage = backstage;
        _menuBackdrop = new CrystalApplicationMenuBackdrop(menu, window);
        foreach (RibbonTab tab in ribbon.Tabs)
            AttachTab(tab);
        ribbon.Tabs.CollectionChanged += OnTabsChanged;
    }

    public void Apply(bool enabled, Color? tint = null)
    {
        _enabled = enabled;
        if (_palette != null)
        {
            _window.Resources.MergedDictionaries.Remove(_palette);
            _backstageScope.MergedDictionaries.Remove(_palette);
            _palette = null;
        }
        if (enabled)
        {
            _palette = CrystalPalette.Create(tint ?? CrystalPalette.Blue, ThemeManager.IsDarkMode);
            // The existing main-window tint uses the raw accent for its thumb wash.
            // Shared templates and metrics come directly from the Crystal palette.
            _palette["RibbonKit.Brushes.ScrollBar.WashAccent"] = new SolidColorBrush(tint ?? CrystalPalette.Blue);
            _window.Resources.MergedDictionaries.Add(_palette);
            _backstageScope.MergedDictionaries.Add(_palette);
            if (!_backstage.Resources.MergedDictionaries.Contains(_backstageScope))
                _backstage.Resources.MergedDictionaries.Add(_backstageScope);
        }
        else
        {
            _backstage.Resources.MergedDictionaries.Remove(_backstageScope);
        }
        _menuBackdrop.Apply(enabled);
        foreach (var popup in _popups.Values) popup.Apply(enabled);
    }

    private void OnTabsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            foreach (RibbonTab tab in _tabs.ToArray()) RemoveTab(tab);
            foreach (RibbonTab tab in _ribbon.Tabs) AttachTab(tab);
            return;
        }
        if (e.OldItems != null)
            foreach (RibbonTab tab in e.OldItems)
                RemoveTab(tab);
        if (e.NewItems != null)
            foreach (RibbonTab tab in e.NewItems)
                AttachTab(tab);
    }

    private void AttachTab(RibbonTab tab)
    {
        if (!_tabs.Add(tab)) return;
        foreach (RibbonGroup group in tab.Groups)
        {
            AddPopup(group, "PART_PopupHost");
            foreach (var dropDown in group.Items.OfType<RibbonDropDownButton>())
            {
                AddPopup(dropDown, "PART_MenuHost");
            }
        }
    }

    private void RemoveTab(RibbonTab tab)
    {
        if (!_tabs.Remove(tab)) return;
        foreach (RibbonGroup group in tab.Groups) RemovePopup(group);
    }

    private void AddPopup(Control control, string hostPart)
    {
        if (_popups.ContainsKey(control)) return;
        var popup = new CrystalPopupBackdrop(_window, control, hostPart);
        _popups.Add(control, popup);
        if (_enabled) popup.Apply(true);
    }

    private void RemovePopup(RibbonGroup group)
    {
        Remove(group);
        foreach (var dropDown in group.Items.OfType<RibbonDropDownButton>())
        {
            Remove(dropDown);
        }
        void Remove(Control control)
        {
            if (!_popups.Remove(control, out var popup)) return;
            popup.Remove();
        }
    }
}
