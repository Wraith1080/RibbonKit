using System.Diagnostics;
using Microsoft.VisualStudio.DesignTools.Extensibility.Interaction;
using Microsoft.VisualStudio.DesignTools.Extensibility.Metadata;
using Microsoft.VisualStudio.DesignTools.Extensibility.Model;
using Microsoft.VisualStudio.DesignTools.Extensibility.Services;

namespace RibbonKit.Design;

/// <summary>
/// Shared design-time state for the ribbon's "preview tab" — the tab shown on the design
/// surface without touching the serialized XAML or the running app. The editor dialog sets
/// it; <see cref="SelectedTabPreviewProvider"/> reads it back when the designer re-evaluates
/// <c>SelectedIndex</c>.
/// </summary>
/// <remarks>
/// The new designer calls a <see cref="DesignModeValueProvider"/> lazily — only when a
/// property is edited in the designer or when <c>ValueTranslationService.InvalidateProperty</c>
/// is called, never on initial load (confirmed on Windows: a load-time-only spike did nothing).
/// So the preview is driven by an explicit invalidation here rather than applied at open.
/// </remarks>
internal static class TabPreviewCoordinator
{
    private static ModelItem _ribbon;
    private static int? _index;

    /// <summary>The currently previewed tab index, or null when no preview is active.</summary>
    public static int? CurrentIndex => _index;

    /// <summary>
    /// Sets (or clears, when <paramref name="index"/> is null) the previewed tab for
    /// <paramref name="ribbon"/> and asks the designer to re-evaluate <c>SelectedIndex</c> so
    /// the surface repaints. Writes nothing to the XAML.
    /// </summary>
    public static void Set(ModelItem ribbon, int? index)
    {
        _ribbon = index.HasValue ? ribbon : null;
        _index = index;
        Invalidate(ribbon);
    }

    /// <summary>True (with the previewed index) when a preview is active for <paramref name="ribbon"/>.</summary>
    public static bool TryGet(ModelItem ribbon, out int index)
    {
        if (_index.HasValue && Equals(_ribbon, ribbon))
        {
            index = _index.Value;
            return true;
        }

        index = 0;
        return false;
    }

    private static void Invalidate(ModelItem ribbon)
    {
        try
        {
            var pid = new PropertyIdentifier(new TypeIdentifier("RibbonKit.Controls.Ribbon"), "SelectedIndex");
            ribbon.Context.Services.GetRequiredService<ValueTranslationService>().InvalidateProperty(ribbon, pid);
        }
        catch
        {
            // Best-effort: without the service the surface just won't refresh until the next touch.
        }
    }
}

/// <summary>
/// Design-time-only translation of <c>Ribbon.SelectedIndex</c>: when the editor has chosen a
/// preview tab (see <see cref="TabPreviewCoordinator"/>), the surface renders that tab while the
/// running app is unaffected — the migration docs note <c>TranslatePropertyValue</c> is never
/// invoked for run-time code, and nothing is serialized. This is the supported equivalent of a
/// hand-authored <c>d:SelectedIndex</c>, which cannot be written programmatically through the
/// model API. Registered on Ribbon in <see cref="Metadata"/>.
/// </summary>
public sealed class SelectedTabPreviewProvider : DesignModeValueProvider
{
    private const string RibbonType = "RibbonKit.Controls.Ribbon";

    public SelectedTabPreviewProvider()
    {
        Properties.Add(new TypeIdentifier(RibbonType), "SelectedIndex");
    }

    /// <inheritdoc />
    public override object TranslatePropertyValue(ModelItem item, PropertyIdentifier identifier, object value)
    {
        if (identifier.Name == "SelectedIndex" && TabPreviewCoordinator.TryGet(item, out int index))
        {
            int count = TabCount(item);
            if (index >= 0 && index < count)
            {
                Debug.WriteLine("[RibbonKit] Preview SelectedIndex -> " + index);
                return index;
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
