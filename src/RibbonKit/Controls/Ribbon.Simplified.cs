using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace RibbonKit.Controls;

public partial class Ribbon
{
    /// <summary>Identifies the inherited attached <see cref="LayoutMode"/> property.</summary>
    public static readonly DependencyProperty LayoutModeProperty = DependencyProperty.RegisterAttached(
        nameof(LayoutMode), typeof(RibbonLayoutMode), typeof(Ribbon),
        new FrameworkPropertyMetadata(RibbonLayoutMode.Classic,
            FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsMeasure,
            OnLayoutModeChanged), value => value is RibbonLayoutMode.Classic or RibbonLayoutMode.Simplified);

    /// <summary>Gets or sets the body layout. Hosts own the selector and appearance persistence.</summary>
    public RibbonLayoutMode LayoutMode
    {
        get => GetLayoutMode(this);
        set => SetLayoutMode(this, value);
    }

    /// <summary>Gets the layout mode inherited or explicitly set on an element.</summary>
    public static RibbonLayoutMode GetLayoutMode(DependencyObject element) =>
        (RibbonLayoutMode)element.GetValue(LayoutModeProperty);

    /// <summary>Sets a ribbon layout mode or an explicit layout scope.</summary>
    public static void SetLayoutMode(DependencyObject element, RibbonLayoutMode value) =>
        element.SetValue(LayoutModeProperty, value);

    /// <summary>Identifies the attached simplified command placement property.</summary>
    public static readonly DependencyProperty SimplifiedPlacementProperty = DependencyProperty.RegisterAttached(
        "SimplifiedPlacement", typeof(RibbonSimplifiedPlacement), typeof(Ribbon),
        new FrameworkPropertyMetadata(RibbonSimplifiedPlacement.Overflow, OnSimplifiedCommandPolicyChanged),
        value => value is RibbonSimplifiedPlacement.Overflow or RibbonSimplifiedPlacement.Inline);

    /// <summary>Gets a command's simplified placement. The default is overflow.</summary>
    public static RibbonSimplifiedPlacement GetSimplifiedPlacement(DependencyObject element) =>
        (RibbonSimplifiedPlacement)element.GetValue(SimplifiedPlacementProperty);

    /// <summary>Chooses inline or overflow presentation without changing classic group layout.</summary>
    public static void SetSimplifiedPlacement(DependencyObject element, RibbonSimplifiedPlacement value) =>
        element.SetValue(SimplifiedPlacementProperty, value);

    /// <summary>Identifies the attached simplified inline command size property.</summary>
    public static readonly DependencyProperty SimplifiedSizeProperty = DependencyProperty.RegisterAttached(
        "SimplifiedSize", typeof(RibbonControlSize), typeof(Ribbon),
        new FrameworkPropertyMetadata(RibbonControlSize.Medium, OnSimplifiedCommandPolicyChanged),
        value => value is RibbonControlSize.Small or RibbonControlSize.Medium);

    /// <summary>Gets a command's simplified inline size. Medium shows its icon and label; Small shows its icon.</summary>
    public static RibbonControlSize GetSimplifiedSize(DependencyObject element) =>
        (RibbonControlSize)element.GetValue(SimplifiedSizeProperty);

    /// <summary>Sets Small or Medium inline presentation independently of classic sizing. Overflow entries retain labels.</summary>
    public static void SetSimplifiedSize(DependencyObject element, RibbonControlSize value) =>
        element.SetValue(SimplifiedSizeProperty, value);

    /// <summary>Identifies the attached simplified resize priority property.</summary>
    public static readonly DependencyProperty SimplifiedPriorityProperty = DependencyProperty.RegisterAttached(
        "SimplifiedPriority", typeof(int), typeof(Ribbon), new FrameworkPropertyMetadata(0, OnSimplifiedCommandPolicyChanged));

    /// <summary>Gets the retention priority. Higher values remain inline longer; ties overflow from the end.</summary>
    public static int GetSimplifiedPriority(DependencyObject element) => (int)element.GetValue(SimplifiedPriorityProperty);

    /// <summary>Sets a command's retention priority for simplified ribbon resizing.</summary>
    public static void SetSimplifiedPriority(DependencyObject element, int value) => element.SetValue(SimplifiedPriorityProperty, value);

    private static void OnLayoutModeChanged(DependencyObject element, DependencyPropertyChangedEventArgs args)
    {
        if (element is RibbonGroupsHost host) host.RequestSimplifiedRefresh();
        if (element is Ribbon ribbon)
        {
            ribbon.RequestLayoutMotion(Animation.RibbonAnimationAction.LayoutModeChange);
            // A mode toggle may be inside a collapsed-group flyout. Let its
            // routed click/command finish before returning the group's content.
            ribbon.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                (Action)ribbon.DismissLayoutModePopups);
            ribbon._keyTipService?.OnLayoutModeChanged();
            ribbon.RequestSelectionVisualsRefresh();
        }
    }

    private void DismissLayoutModePopups()
    {
        PopupDismissHelper.DismissOpenPopupsWithin(this);
        if (_titleBarQatHost is { } title) PopupDismissHelper.DismissOpenPopupsWithin(title);
        _openRibbonContextMenu?.SetCurrentValue(System.Windows.Controls.ContextMenu.IsOpenProperty, false);
        _qatContextMenu?.SetCurrentValue(System.Windows.Controls.ContextMenu.IsOpenProperty, false);
        if (IsApplicationMenuOpen) SetCurrentValue(IsBackstageOpenProperty, false);
    }

    private static void OnSimplifiedCommandPolicyChanged(DependencyObject element, DependencyPropertyChangedEventArgs args)
    {
        for (DependencyObject? node = element; node is not null;
             node = LogicalTreeHelper.GetParent(node) ?? (node is Visual visual ? VisualTreeHelper.GetParent(visual) : null))
        {
            if (node is RibbonGroupsHost host) { host.RequestSimplifiedRefresh(); break; }
        }
    }
}
