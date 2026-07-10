using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;

namespace RibbonKit.Controls;

/// <summary>
/// Shared discovery/description helpers for the customization pages: finds a ribbon's
/// command controls, and describes them ("Bold" + icon, or with a "Home › Font › Bold"
/// path). Used by <see cref="RibbonQuickAccessPage"/> and <see cref="RibbonCustomizePage"/>
/// so both agree on what counts as a command.
/// </summary>
internal static class RibbonCommandCatalog
{
    /// <summary>All commandable controls in the ribbon's groups, with their tab › group path.</summary>
    internal static ObservableCollection<RibbonCommandEntry> CollectAvailable(Ribbon ribbon)
    {
        var entries = new ObservableCollection<RibbonCommandEntry>();
        foreach (RibbonTab tab in ribbon.Tabs)
        {
            string tabName = tab.Header?.ToString() ?? "Tab";
            foreach (RibbonGroup group in tab.Groups)
            {
                string groupName = group.Header?.ToString() ?? "Group";
                foreach (FrameworkElement control in CollectControls(group))
                {
                    RibbonCommandEntry described = Describe(control);
                    entries.Add(new RibbonCommandEntry(
                        control, $"{tabName} › {groupName} › {described.DisplayName}", described.Icon));
                }
            }
        }

        return entries;
    }

    /// <summary>The commandable controls directly reachable inside a group's content.</summary>
    internal static List<FrameworkElement> CollectControls(RibbonGroup group)
    {
        var results = new List<FrameworkElement>();
        foreach (object item in group.Items)
        {
            CollectControls(item as DependencyObject, results, depth: 0);
        }

        return results;
    }

    /// <summary>
    /// Walks the logical tree under a group item collecting commandable controls. Groups host
    /// arbitrary content (stack panels, grids…), hence the walk; depth-capped defensively, and
    /// popup content (menu items, gallery tiles) is never reached because those types aren't
    /// descended into. Quick-access/custom-group PROXIES are skipped — offering a proxy as an
    /// addable command would create proxy-of-proxy chains.
    /// </summary>
    private static void CollectControls(DependencyObject? node, List<FrameworkElement> results, int depth)
    {
        if (node is null || depth > 6)
        {
            return;
        }

        if (Ribbon.GetQuickAccessSource(node) is not null)
        {
            return; // A proxy — its source is already listed at its original location.
        }

        switch (node)
        {
            case RibbonToggleButton toggle:
                results.Add(toggle);
                return;
            case RibbonButton button:
                results.Add(button);
                return;
            // Covers RibbonSplitButton too (it derives from RibbonDropDownButton). Collected as
            // a whole; a proxy invokes the primary action (split) or opens the menu (dropdown).
            case RibbonDropDownButton dropDown:
                results.Add(dropDown);
                return;
        }

        foreach (object child in LogicalTreeHelper.GetChildren(node))
        {
            CollectControls(child as DependencyObject, results, depth + 1);
        }
    }

    /// <summary>
    /// Describes a single control (or proxy — described via its source) with caption + icon,
    /// no path prefix. Used for QAT items and for command nodes in the customize tree.
    /// </summary>
    internal static RibbonCommandEntry Describe(FrameworkElement element)
    {
        FrameworkElement subject = Ribbon.GetQuickAccessSource(element) ?? element;
        (string? header, string? tipTitle, ImageSource? icon) = subject switch
        {
            RibbonToggleButton t => (t.Header, t.ScreenTipTitle, t.Icon ?? t.LargeIcon),
            RibbonButton b => (b.Header, b.ScreenTipTitle, b.Icon ?? b.LargeIcon),
            RibbonDropDownButton d => (d.Header, d.ScreenTipTitle, d.Icon ?? d.LargeIcon),
            _ => (null, null, null),
        };

        // A renamed PROXY shows its own header, not the source's.
        if (!ReferenceEquals(subject, element))
        {
            string? proxyHeader = element switch
            {
                RibbonToggleButton pt => pt.Header,
                RibbonButton pb => pb.Header,
                _ => null,
            };
            header = proxyHeader ?? header;
        }

        return new RibbonCommandEntry(element, Caption(header, tipTitle), icon);
    }

    internal static string Caption(string? header, string? screenTipTitle) =>
        !string.IsNullOrWhiteSpace(header) ? header
        : !string.IsNullOrWhiteSpace(screenTipTitle) ? screenTipTitle
        : "(command)";

    /// <summary>
    /// Every distinct icon the ribbon already uses (command icons + group icons), for the
    /// custom-group icon picker — self-contained and automatically app-consistent.
    /// </summary>
    internal static List<ImageSource> CollectIcons(Ribbon ribbon)
    {
        var icons = new List<ImageSource>();

        void Add(ImageSource? icon)
        {
            if (icon is not null && !icons.Contains(icon))
            {
                icons.Add(icon);
            }
        }

        foreach (RibbonTab tab in ribbon.Tabs)
        {
            foreach (RibbonGroup group in tab.Groups)
            {
                Add(group.Icon);
                foreach (FrameworkElement control in CollectControls(group))
                {
                    Add(Describe(control).Icon);
                }
            }
        }

        return icons;
    }
}
