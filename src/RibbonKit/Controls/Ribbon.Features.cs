using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;

namespace RibbonKit.Controls;

public partial class Ribbon
{
    /// <summary>Identifies the <see cref="IsSimplifiedRibbonEnabled"/> dependency property.</summary>
    public static readonly DependencyProperty IsSimplifiedRibbonEnabledProperty = DependencyProperty.Register(
        nameof(IsSimplifiedRibbonEnabled), typeof(bool), typeof(Ribbon), new FrameworkPropertyMetadata(true, OnSimplifiedCapabilityChanged));

    /// <summary>Whether Simplified Ribbon is available. False coerces the effective layout to Classic while preserving its requested value and binding. Defaults to true.</summary>
    public bool IsSimplifiedRibbonEnabled
    {
        get => (bool)GetValue(IsSimplifiedRibbonEnabledProperty);
        set => SetValue(IsSimplifiedRibbonEnabledProperty, value);
    }

    /// <summary>Identifies the <see cref="CanUserCustomizeRibbon"/> dependency property.</summary>
    public static readonly DependencyProperty CanUserCustomizeRibbonProperty = DependencyProperty.Register(
        nameof(CanUserCustomizeRibbon), typeof(bool), typeof(Ribbon), new FrameworkPropertyMetadata(true, OnUserPolicyChanged));

    /// <summary>Whether users may edit Classic or Simplified Ribbon customization. Host configuration and restoration remain available. Defaults to true.</summary>
    public bool CanUserCustomizeRibbon
    {
        get => (bool)GetValue(CanUserCustomizeRibbonProperty);
        set => SetValue(CanUserCustomizeRibbonProperty, value);
    }

    /// <summary>Identifies the <see cref="CanUserCustomizeQuickAccess"/> dependency property.</summary>
    public static readonly DependencyProperty CanUserCustomizeQuickAccessProperty = DependencyProperty.Register(
        nameof(CanUserCustomizeQuickAccess), typeof(bool), typeof(Ribbon), new FrameworkPropertyMetadata(true, OnUserPolicyChanged));

    /// <summary>Whether users may add, remove or reorder Quick Access Toolbar items. Existing items and host configuration are preserved. Defaults to true.</summary>
    public bool CanUserCustomizeQuickAccess
    {
        get => (bool)GetValue(CanUserCustomizeQuickAccessProperty);
        set => SetValue(CanUserCustomizeQuickAccessProperty, value);
    }

    /// <summary>Identifies the <see cref="CanUserRepositionQuickAccess"/> dependency property.</summary>
    public static readonly DependencyProperty CanUserRepositionQuickAccessProperty = DependencyProperty.Register(
        nameof(CanUserRepositionQuickAccess), typeof(bool), typeof(Ribbon), new FrameworkPropertyMetadata(true, OnUserPolicyChanged));

    /// <summary>Whether users may move the Quick Access Toolbar. The host may still set QuickAccessPosition. Defaults to true.</summary>
    public bool CanUserRepositionQuickAccess
    {
        get => (bool)GetValue(CanUserRepositionQuickAccessProperty);
        set => SetValue(CanUserRepositionQuickAccessProperty, value);
    }

    /// <summary>Identifies the <see cref="CanUserToggleQuickAccessVisibility"/> dependency property.</summary>
    public static readonly DependencyProperty CanUserToggleQuickAccessVisibilityProperty = DependencyProperty.Register(
        nameof(CanUserToggleQuickAccessVisibility), typeof(bool), typeof(Ribbon), new FrameworkPropertyMetadata(true, OnUserPolicyChanged));

    /// <summary>Whether users may show or hide the Quick Access Toolbar. The host may still set IsQuickAccessVisible. Defaults to true.</summary>
    public bool CanUserToggleQuickAccessVisibility
    {
        get => (bool)GetValue(CanUserToggleQuickAccessVisibilityProperty);
        set => SetValue(CanUserToggleQuickAccessVisibilityProperty, value);
    }

    /// <summary>Identifies the <see cref="CanUserMinimizeRibbon"/> dependency property.</summary>
    public static readonly DependencyProperty CanUserMinimizeRibbonProperty = DependencyProperty.Register(
        nameof(CanUserMinimizeRibbon), typeof(bool), typeof(Ribbon), new FrameworkPropertyMetadata(true, OnUserPolicyChanged));

    /// <summary>Whether users may minimize or restore the ribbon. The host may still set IsMinimized. Defaults to true.</summary>
    public bool CanUserMinimizeRibbon
    {
        get => (bool)GetValue(CanUserMinimizeRibbonProperty);
        set => SetValue(CanUserMinimizeRibbonProperty, value);
    }

    /// <summary>Identifies the <see cref="CanUserChangeLayoutMode"/> dependency property.</summary>
    public static readonly DependencyProperty CanUserChangeLayoutModeProperty = DependencyProperty.Register(
        nameof(CanUserChangeLayoutMode), typeof(bool), typeof(Ribbon), new FrameworkPropertyMetadata(true, OnUserPolicyChanged));

    /// <summary>Whether users may change the ribbon layout. The host may still select any available LayoutMode. Defaults to true.</summary>
    public bool CanUserChangeLayoutMode
    {
        get => (bool)GetValue(CanUserChangeLayoutModeProperty);
        set => SetValue(CanUserChangeLayoutModeProperty, value);
    }

    /// <summary>Identifies the <see cref="CanUserChangeDensity"/> dependency property.</summary>
    public static readonly DependencyProperty CanUserChangeDensityProperty = DependencyProperty.Register(
        nameof(CanUserChangeDensity), typeof(bool), typeof(Ribbon), new FrameworkPropertyMetadata(true, OnUserPolicyChanged));

    /// <summary>Whether users may change Compact or Touch density. The host may still set Density. Defaults to true.</summary>
    public bool CanUserChangeDensity
    {
        get => (bool)GetValue(CanUserChangeDensityProperty);
        set => SetValue(CanUserChangeDensityProperty, value);
    }

    // Private inherited state cannot be locally overridden by authored LayoutMode scopes.
    // Every Ribbon establishes its own boundary, including nested ribbons.
    private static readonly DependencyPropertyKey SimplifiedCapabilityPropertyKey = DependencyProperty.RegisterAttachedReadOnly(
        "SimplifiedCapability", typeof(bool), typeof(Ribbon),
        new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.Inherits,
            (d, _) => d.CoerceValue(LayoutModeProperty)));
    private static readonly DependencyProperty SimplifiedCapabilityProperty = SimplifiedCapabilityPropertyKey.DependencyProperty;

    private static object CoerceLayoutMode(DependencyObject element, object requested) =>
        (bool)element.GetValue(SimplifiedCapabilityProperty) ? requested : RibbonLayoutMode.Classic;

    private static void OnUserPolicyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ribbon = (Ribbon)d;
        // Close cached choices; handlers also check permission when invoked.
        ribbon.Dispatcher.BeginInvoke(DispatcherPriority.Background, (Action)(() =>
        {
            ribbon._openRibbonContextMenu?.SetCurrentValue(ContextMenu.IsOpenProperty, false);
            ribbon._qatContextMenu?.SetCurrentValue(ContextMenu.IsOpenProperty, false);
        }));
    }

    private static void OnSimplifiedCapabilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ribbon = (Ribbon)d;
        int generation = ++ribbon._featureFocusGeneration;
        FrameworkElement? focused = Keyboard.FocusedElement as FrameworkElement;
        FrameworkElement? source = null;
        bool restoreFocus = false;
        if (!(bool)e.NewValue && focused is not null)
        {
            foreach (var toolbar in ribbon.Tabs.Select(tab => tab.Content).OfType<RibbonGroupsHost>()
                .Select(host => host.SimplifiedToolBar).OfType<RibbonQuickAccessToolBar>())
            {
                var copy = toolbar.Items.OfType<FrameworkElement>().FirstOrDefault(item => item.IsKeyboardFocusWithin);
                if (copy is not null || toolbar.IsKeyboardFocusWithin)
                {
                    restoreFocus = true;
                    source = copy is null ? null : GetQuickAccessSource(copy);
                    break;
                }
            }
        }
        ribbon.SetValue(SimplifiedCapabilityPropertyKey, e.NewValue);
        ribbon.ReconcileFeatureTabs();
        ribbon.CoerceValue(LayoutModeProperty);
        ribbon.CancelDensityTransition();
        ribbon._keyTipService?.OnLayoutModeChanged();
        if (restoreFocus)
            ribbon.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, (Action)(() =>
            {
                if (generation != ribbon._featureFocusGeneration || ribbon.IsSimplifiedRibbonEnabled || !ribbon.IsLoaded) return;
                var currentFocus = Keyboard.FocusedElement;
                if (currentFocus is not null && !ReferenceEquals(currentFocus, focused)
                    && !ReferenceEquals(currentFocus, source) && !ReferenceEquals(currentFocus, ribbon._ribbonTabControl)
                    && !ReferenceEquals(currentFocus, ribbon.SelectedTab)) return;
                (Window.GetWindow(ribbon) as FrameworkElement ?? ribbon).UpdateLayout();
                if (source is { IsVisible: true, IsEnabled: true, Focusable: true } && source.Focus()) return;
                if (ribbon.SelectedTab is { } selected
                    && ribbon._ribbonTabControl?.ItemContainerGenerator.ContainerFromItem(selected) is UIElement tab) tab.Focus();
            }));
    }

    private int _featureFocusGeneration;
    private readonly HashSet<RibbonTab> _featureTabs = new();

    private void ReconcileFeatureTabs()
    {
        // Tabs are collection-owned before the template gives them a logical parent.
        // Preserve the same capability for inactive tabs and remove it on collection Reset.
        foreach (var tab in _featureTabs.Where(tab => !Tabs.Contains(tab)).ToArray())
        {
            tab.ClearValue(SimplifiedCapabilityPropertyKey);
            _featureTabs.Remove(tab);
        }
        foreach (var tab in Tabs)
        {
            _featureTabs.Add(tab);
            // Enabled is the inherited/default state. Avoid installing redundant local
            // values on every tab rebuild in the compatibility-default path.
            if (IsSimplifiedRibbonEnabled) tab.ClearValue(SimplifiedCapabilityPropertyKey);
            else tab.SetValue(SimplifiedCapabilityPropertyKey, false);
        }
    }

    private static void NormalizeMenuSeparators(ContextMenu menu)
    {
        bool preceding = false;
        Separator? pending = null;
        foreach (var item in menu.Items.OfType<UIElement>())
        {
            if (item is Separator separator)
            {
                separator.Visibility = Visibility.Collapsed;
                if (preceding) pending ??= separator;
            }
            else if (item.Visibility == Visibility.Visible)
            {
                if (pending is not null) pending.Visibility = Visibility.Visible;
                pending = null;
                preceding = true;
            }
        }
    }

    // Shared by detached customization pages. Binding keeps eligibility current even before Loaded.
    internal static void ObserveUserPermission(FrameworkElement page, DependencyProperty observer, Ribbon? ribbon, string property)
    {
        if (ribbon is null) page.ClearValue(observer);
        else page.SetBinding(observer, new Binding(property) { Source = ribbon });
        page.CoerceValue(IsEnabledProperty);
    }
}
