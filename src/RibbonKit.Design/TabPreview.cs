using System.Diagnostics;
using Microsoft.VisualStudio.DesignTools.Extensibility.Interaction;
using Microsoft.VisualStudio.DesignTools.Extensibility.Metadata;
using Microsoft.VisualStudio.DesignTools.Extensibility.Model;
using Microsoft.VisualStudio.DesignTools.Extensibility.Services;

namespace RibbonKit.Design;

/// <summary>
/// Shared design-time state for the ribbon's design-only previews — the tab shown on the surface
/// (<c>SelectedIndex</c>) and whether the backstage is open (<c>IsBackstageOpen</c>) — without
/// touching the serialized XAML or the running app. The editor sets these;
/// <see cref="SelectedTabPreviewProvider"/> reads them back when the designer re-evaluates the property.
/// </summary>
/// <remarks>
/// The new designer calls a <see cref="DesignModeValueProvider"/> lazily — only when a property is
/// edited in the designer or when <c>ValueTranslationService.InvalidateProperty</c> is called, never
/// on initial load (confirmed on Windows). So each preview is driven by an explicit invalidation here.
/// </remarks>
internal static class TabPreviewCoordinator
{
    private const string RibbonType = "RibbonKit.Controls.Ribbon";
    private const string BackstageType = "RibbonKit.Controls.Backstage";
    // Backstage.SelectedIndex is INHERITED from Selector, so the property identifier's declaring type
    // may be reported as Selector rather than Backstage. Which one the designer uses for an inherited
    // DP is unverified, so we invalidate under both (and BackstagePagePreviewProvider registers both).
    private const string SelectorType = "System.Windows.Controls.Primitives.Selector";

    private static ModelItem _ribbon;
    private static int? _tabIndex;
    private static bool? _backstageOpen;
    private static ModelItem _backstage;
    private static int? _backstagePage;

    /// <summary>The currently previewed tab index, or null when no tab preview is active.</summary>
    public static int? CurrentIndex => _tabIndex;

    /// <summary>The current backstage-open override, or null when not overridden.</summary>
    public static bool? CurrentBackstageOpen => _backstageOpen;

    /// <summary>The currently previewed backstage page index, or null when no page preview is active.</summary>
    public static int? CurrentBackstagePage => _backstagePage;

    /// <summary>Sets (or clears, when null) the previewed tab and repaints the surface. Writes no XAML.</summary>
    public static void SetTab(ModelItem ribbon, int? index)
    {
        _ribbon = ribbon;
        _tabIndex = index;
        Invalidate(ribbon, "SelectedIndex");
    }

    /// <summary>Sets (or clears, when null) the design-only backstage-open state and repaints the surface.</summary>
    public static void SetBackstage(ModelItem ribbon, bool? open)
    {
        _ribbon = ribbon;
        _backstageOpen = open;
        Invalidate(ribbon, "IsBackstageOpen");
    }

    /// <summary>True (with the index) when a tab preview is active for <paramref name="ribbon"/>.</summary>
    public static bool TryGetTab(ModelItem ribbon, out int index)
    {
        if (_tabIndex.HasValue && Equals(_ribbon, ribbon))
        {
            index = _tabIndex.Value;
            return true;
        }

        index = 0;
        return false;
    }

    /// <summary>True (with the value) when a backstage-open override is active for <paramref name="ribbon"/>.</summary>
    public static bool TryGetBackstage(ModelItem ribbon, out bool open)
    {
        if (_backstageOpen.HasValue && Equals(_ribbon, ribbon))
        {
            open = _backstageOpen.Value;
            return true;
        }

        open = false;
        return false;
    }

    /// <summary>Sets (or clears, when null) the previewed backstage page and repaints the surface. Writes no XAML.</summary>
    public static void SetBackstagePage(ModelItem backstage, int? index)
    {
        _backstage = backstage;
        _backstagePage = index;
        if (backstage != null)
        {
            // Invalidate under both possible declaring types (see SelectorType note above).
            Invalidate(backstage, BackstageType, "SelectedIndex");
            Invalidate(backstage, SelectorType, "SelectedIndex");
        }
    }

    /// <summary>True (with the index) when a backstage-page preview is active for <paramref name="backstage"/>.</summary>
    public static bool TryGetBackstagePage(ModelItem backstage, out int index)
    {
        if (_backstagePage.HasValue && Equals(_backstage, backstage))
        {
            index = _backstagePage.Value;
            return true;
        }

        index = 0;
        return false;
    }

    private static void Invalidate(ModelItem ribbon, string propertyName) =>
        Invalidate(ribbon, RibbonType, propertyName);

    private static void Invalidate(ModelItem item, string declaringTypeName, string propertyName)
    {
        try
        {
            var pid = new PropertyIdentifier(new TypeIdentifier(declaringTypeName), propertyName);
            item.Context.Services.GetRequiredService<ValueTranslationService>().InvalidateProperty(item, pid);
        }
        catch
        {
            // Best-effort: without the service the surface just won't refresh until the next touch.
        }
    }
}

/// <summary>
/// Design-time-only translation of <c>Ribbon.SelectedIndex</c> and <c>Ribbon.IsBackstageOpen</c>:
/// when the editor has chosen a preview tab or toggled the backstage
/// (see <see cref="TabPreviewCoordinator"/>), the surface reflects it while the running app is
/// unaffected — <c>TranslatePropertyValue</c> is never invoked for run-time code and nothing is
/// serialized. This is the supported equivalent of hand-authored <c>d:SelectedIndex</c> /
/// <c>d:IsBackstageOpen</c>, which can't be written programmatically. Registered on Ribbon in <see cref="Metadata"/>.
/// </summary>
public sealed class SelectedTabPreviewProvider : DesignModeValueProvider
{
    private const string RibbonType = "RibbonKit.Controls.Ribbon";

    public SelectedTabPreviewProvider()
    {
        Properties.Add(new TypeIdentifier(RibbonType), "SelectedIndex");
        Properties.Add(new TypeIdentifier(RibbonType), "IsBackstageOpen");
    }

    /// <inheritdoc />
    public override object TranslatePropertyValue(ModelItem item, PropertyIdentifier identifier, object value)
    {
        if (identifier.Name == "SelectedIndex" && TabPreviewCoordinator.TryGetTab(item, out int index))
        {
            int count = TabCount(item);
            if (index >= 0 && index < count)
            {
                Debug.WriteLine("[RibbonKit] Preview SelectedIndex -> " + index);
                return index;
            }
        }

        if (identifier.Name == "IsBackstageOpen" && TabPreviewCoordinator.TryGetBackstage(item, out bool open))
        {
            Debug.WriteLine("[RibbonKit] Preview IsBackstageOpen -> " + open);
            return open;
        }

        return base.TranslatePropertyValue(item, identifier, value);
    }

    private static int TabCount(ModelItem ribbon)
    {
        try
        {
            ModelProperty tabs = ribbon.Properties["Tabs"];
            return tabs?.Collection?.Count ?? 0;
        }
        catch
        {
            return 0;
        }
    }
}

/// <summary>
/// Design-time-only translation of the backstage's <c>SelectedIndex</c> so the editor can preview a
/// specific backstage page on the surface (the equivalent of hand-authored <c>d:SelectedIndex</c> on the
/// backstage). Same mechanism as <see cref="SelectedTabPreviewProvider"/> but attached to
/// <c>Backstage</c>; nothing is serialized and the running app is untouched. Registered in
/// <see cref="Metadata"/>. Registers both the Backstage and Selector declaring types because
/// <c>SelectedIndex</c> is inherited and which one the designer reports for it is unverified.
/// </summary>
public sealed class BackstagePagePreviewProvider : DesignModeValueProvider
{
    private const string BackstageType = "RibbonKit.Controls.Backstage";
    private const string SelectorType = "System.Windows.Controls.Primitives.Selector";

    public BackstagePagePreviewProvider()
    {
        Properties.Add(new TypeIdentifier(BackstageType), "SelectedIndex");
        Properties.Add(new TypeIdentifier(SelectorType), "SelectedIndex");
    }

    /// <inheritdoc />
    public override object TranslatePropertyValue(ModelItem item, PropertyIdentifier identifier, object value)
    {
        if (identifier.Name == "SelectedIndex" && TabPreviewCoordinator.TryGetBackstagePage(item, out int index))
        {
            int count = PageCount(item);
            if (index >= 0 && index < count)
            {
                Debug.WriteLine("[RibbonKit] Preview Backstage SelectedIndex -> " + index);
                return index;
            }
        }

        return base.TranslatePropertyValue(item, identifier, value);
    }

    private static int PageCount(ModelItem backstage)
    {
        try
        {
            ModelProperty items = backstage.Properties["Items"];
            return items?.Collection?.Count ?? 0;
        }
        catch
        {
            return 0;
        }
    }
}
