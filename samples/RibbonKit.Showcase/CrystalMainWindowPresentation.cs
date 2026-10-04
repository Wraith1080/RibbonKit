using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using RibbonKit.Controls;
using RibbonKit.Theming;

namespace RibbonKit.Showcase;

/// <summary>Owns the main Showcase palette scope and optional captured-backdrop registrations.</summary>
internal sealed class CrystalMainWindowPresentation
{
    private readonly RibbonWindow _window;
    private readonly Ribbon _ribbon;
    private readonly Backstage _backstage;
    private readonly ResourceDictionary _backstageScope = new();
    private readonly CapturedBackdrop _menuBackdrop;
    private readonly Dictionary<Control, CapturedBackdrop> _popups = new();
    private readonly HashSet<INotifyCollectionChanged> _collections = new();
    private ResourceDictionary? _palette;
    private bool _enabled;
    private bool _refreshing;
    private bool _closed;

    public ResourceDictionary? Palette => _palette;

    public CrystalMainWindowPresentation(RibbonWindow window, Ribbon ribbon,
        RibbonApplicationMenu menu, Backstage backstage)
    {
        _window = window;
        _ribbon = ribbon;
        _backstage = backstage;
        _menuBackdrop = new CapturedBackdrop(menu, window);
        ribbon.Loaded += OnRibbonLoaded;
        window.Closed += (_, _) =>
        {
            _closed = true;
            ribbon.Loaded -= OnRibbonLoaded;
            _menuBackdrop.Dispose();
            ClearRegistrations();
        };
    }

    public void Apply(bool enabled, Color? tint = null)
    {
        if (_closed) return;
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
        if (enabled) RefreshRegistrations();
        else ClearRegistrations();
    }

    private void OnRibbonLoaded(object sender, RoutedEventArgs e) => RefreshRegistrations();
    private void OnControlsChanged(object? sender, NotifyCollectionChangedEventArgs e) => RefreshRegistrations();

    private void RefreshRegistrations()
    {
        if (!_enabled || _closed || _refreshing) return;
        _refreshing = true;
        try
        {
            var controls = new HashSet<Control>();
            var collections = new HashSet<INotifyCollectionChanged>
            {
                _ribbon.Tabs,
                _ribbon.QuickAccessItems,
            };
            var visited = new HashSet<DependencyObject>();
            foreach (RibbonTab tab in _ribbon.Tabs)
            {
                collections.Add(tab.Groups);
                foreach (RibbonGroup group in tab.Groups) Visit(group);
            }
            foreach (DependencyObject item in _ribbon.QuickAccessItems.OfType<DependencyObject>()) Visit(item);

            foreach (INotifyCollectionChanged obsolete in _collections.Except(collections).ToArray())
            {
                obsolete.CollectionChanged -= OnControlsChanged;
                _collections.Remove(obsolete);
            }
            foreach (INotifyCollectionChanged collection in collections)
                if (_collections.Add(collection)) collection.CollectionChanged += OnControlsChanged;

            foreach (Control obsolete in _popups.Keys.Except(controls).ToArray())
            {
                _popups[obsolete].Dispose();
                _popups.Remove(obsolete);
            }
            foreach (Control control in controls)
            {
                if (!_popups.TryGetValue(control, out var popup))
                    _popups.Add(control, popup = new CapturedBackdrop(control, _window));
                popup.Apply(true);
            }

            void Visit(DependencyObject node)
            {
                if (!visited.Add(node)) return;
                if (node is RibbonDropDownButton or RibbonGroup) controls.Add((Control)node);
                if (node is ItemsControl items)
                {
                    collections.Add((INotifyCollectionChanged)items.Items);
                    foreach (DependencyObject item in items.Items.OfType<DependencyObject>()) Visit(item);
                }
                foreach (DependencyObject child in LogicalTreeHelper.GetChildren(node).OfType<DependencyObject>()) Visit(child);
            }
        }
        finally
        {
            _refreshing = false;
        }
    }

    private void ClearRegistrations()
    {
        foreach (INotifyCollectionChanged collection in _collections)
            collection.CollectionChanged -= OnControlsChanged;
        _collections.Clear();
        foreach (var popup in _popups.Values) popup.Dispose();
        _popups.Clear();
    }
}
