using System.Diagnostics;
using Microsoft.VisualStudio.DesignTools.Extensibility.Interaction;
using Microsoft.VisualStudio.DesignTools.Extensibility.Metadata;
using Microsoft.VisualStudio.DesignTools.Extensibility.Model;
using Microsoft.VisualStudio.DesignTools.Extensibility.Services;

namespace RibbonKit.Design;

/// <summary>The mutually-exclusive File-button surface shown only on the XAML design surface.</summary>
internal enum FileSurfacePreview
{
    Closed,
    Backstage,
    ApplicationMenu,
}

/// <summary>
/// Shared design-time state for the ribbon's design-only previews — the tab shown on the surface
/// (<c>SelectedIndex</c>) and the mutually-exclusive File surface — without
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

    private static ModelItem? _ribbon;
    private static int? _tabIndex;
    private static FileSurfacePreview _fileSurface;
    private static ModelItem? _backstage;
    private static int? _backstagePage;

    /// <summary>The currently previewed tab index, or null when no tab preview is active.</summary>
    public static int? CurrentIndex => _tabIndex;

    /// <summary>The current design-only File-surface choice.</summary>
    public static FileSurfacePreview CurrentFileSurface => _fileSurface;

    /// <summary>The currently previewed backstage page index, or null when no page preview is active.</summary>
    public static int? CurrentBackstagePage => _backstagePage;

    /// <summary>Sets (or clears, when null) the previewed tab and repaints the surface. Writes no XAML.</summary>
    public static void SetTab(ModelItem ribbon, int? index)
    {
        _ribbon = ribbon;
        _tabIndex = index;
        Invalidate(ribbon, "SelectedIndex");
    }

    /// <summary>
    /// Sets the design-only File surface and repaints it. Backstage and application menu are one
    /// mutually-exclusive choice: the provider temporarily hides ApplicationMenu only when the
    /// author explicitly asks to preview Backstage, matching Ribbon's runtime precedence rule.
    /// </summary>
    public static void SetFileSurface(ModelItem ribbon, FileSurfacePreview surface)
    {
        _ribbon = ribbon;
        _fileSurface = surface;
        Invalidate(ribbon, "IsBackstageOpen");
        Invalidate(ribbon, "ApplicationMenu");
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

    /// <summary>True (with the value) when File-surface state belongs to <paramref name="ribbon"/>.</summary>
    public static bool TryGetFileSurface(ModelItem ribbon, out FileSurfacePreview surface)
    {
        if (Equals(_ribbon, ribbon))
        {
            surface = _fileSurface;
            return true;
        }

        surface = FileSurfacePreview.Closed;
        return false;
    }

    /// <summary>Sets (or clears, when null) the previewed backstage page and repaints the surface. Writes no XAML.</summary>
    public static void SetBackstagePage(ModelItem? backstage, int? index)
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

    internal static void Invalidate(ModelItem ribbon, string propertyName) =>
        Invalidate(ribbon, RibbonType, propertyName);

    internal static void Invalidate(ModelItem item, string declaringTypeName, string propertyName)
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
/// Design-time-only translation of the Ribbon tab and File-surface properties:
/// when the editor has chosen a preview tab or File surface
/// (see <see cref="TabPreviewCoordinator"/>), the surface reflects it while the running app is
/// unaffected — <c>TranslatePropertyValue</c> is never invoked for run-time code and nothing is
/// serialized. This is the supported equivalent of hand-authored design-time values, which can't
/// be written programmatically. Registered on Ribbon in <see cref="Metadata"/>.
/// </summary>
public sealed class SelectedTabPreviewProvider : DesignModeValueProvider
{
    private const string RibbonType = "RibbonKit.Controls.Ribbon";

    public SelectedTabPreviewProvider()
    {
        Properties.Add(new TypeIdentifier(RibbonType), "SelectedIndex");
        Properties.Add(new TypeIdentifier(RibbonType), "IsBackstageOpen");
        Properties.Add(new TypeIdentifier(RibbonType), "ApplicationMenu");
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

        if (TabPreviewCoordinator.TryGetFileSurface(item, out FileSurfacePreview surface))
        {
            if (identifier.Name == "IsBackstageOpen")
            {
                bool open = surface != FileSurfacePreview.Closed;
                Debug.WriteLine("[RibbonKit] Preview IsBackstageOpen -> " + open);
                return open;
            }

            if (identifier.Name == "ApplicationMenu" && surface == FileSurfacePreview.Backstage)
            {
                Debug.WriteLine("[RibbonKit] Preview ApplicationMenu -> null (show Backstage)");
                return null!;
            }
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

/// <summary>
/// Design-session state for the application menu's active command pane. The runtime menu owns this
/// as read-only hover state, so the designer translates the four template-facing values directly
/// rather than adding a runtime-only selection API.
/// </summary>
internal static class ApplicationMenuPreviewCoordinator
{
    private const string MenuType = "RibbonKit.Controls.RibbonApplicationMenu";
    private const string ItemType = "RibbonKit.Controls.RibbonApplicationMenuItem";

    private static ModelItem? _menu;
    private static ModelItem? _item;

    public static ModelItem? CurrentItemFor(ModelItem menu) => Equals(_menu, menu) ? _item : null;

    public static void SetActiveItem(ModelItem? menu, ModelItem? item)
    {
        ModelItem? previous = _item;
        _menu = menu;
        _item = item;

        if (menu != null)
        {
            TabPreviewCoordinator.Invalidate(menu, MenuType, "ActivePaneContent");
            TabPreviewCoordinator.Invalidate(menu, MenuType, "ActivePaneHeader");
            TabPreviewCoordinator.Invalidate(menu, MenuType, "HasActivePane");
        }

        if (previous != null)
        {
            TabPreviewCoordinator.Invalidate(previous, ItemType, "IsActive");
        }
        if (item != null && !Equals(previous, item))
        {
            TabPreviewCoordinator.Invalidate(item, ItemType, "IsActive");
        }
    }

    public static bool TryGetActiveItem(ModelItem menu, out ModelItem? item)
    {
        if (Equals(_menu, menu))
        {
            item = _item;
            return true;
        }

        item = null;
        return false;
    }

    public static bool IsActive(ModelItem item) => Equals(_item, item);
}

/// <summary>
/// Translates the application menu's read-only pane state for design-only preview. It is registered
/// on both the menu and its command item type; no value is serialized and runtime behavior is
/// unchanged.
/// </summary>
public sealed class ApplicationMenuPanePreviewProvider : DesignModeValueProvider
{
    private const string MenuType = "RibbonKit.Controls.RibbonApplicationMenu";
    private const string ItemType = "RibbonKit.Controls.RibbonApplicationMenuItem";

    public ApplicationMenuPanePreviewProvider()
    {
        Properties.Add(new TypeIdentifier(MenuType), "ActivePaneContent");
        Properties.Add(new TypeIdentifier(MenuType), "ActivePaneHeader");
        Properties.Add(new TypeIdentifier(MenuType), "HasActivePane");
        Properties.Add(new TypeIdentifier(ItemType), "IsActive");
    }

    /// <inheritdoc />
    public override object TranslatePropertyValue(ModelItem item, PropertyIdentifier identifier, object value)
    {
        if (identifier.Name == "IsActive")
        {
            return ApplicationMenuPreviewCoordinator.IsActive(item);
        }

        if (ApplicationMenuPreviewCoordinator.TryGetActiveItem(item, out ModelItem? active))
        {
            if (identifier.Name == "HasActivePane")
            {
                return active != null;
            }

            if (active != null && identifier.Name == "ActivePaneHeader")
            {
                return ModelValue(active, "PaneHeader");
            }

            if (active != null && identifier.Name == "ActivePaneContent")
            {
                return ModelValue(active, "Content");
            }
        }

        return base.TranslatePropertyValue(item, identifier, value);
    }

    private static object ModelValue(ModelItem item, string propertyName)
    {
        try
        {
            ModelProperty property = item.Properties[propertyName];
            return property.ComputedValue ?? null!;
        }
        catch
        {
            return null!;
        }
    }
}
