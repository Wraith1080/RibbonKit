using System.ComponentModel;
using Microsoft.VisualStudio.DesignTools.Extensibility.Metadata;

namespace RibbonKit.Design;

/// <summary>
/// Design-time Properties-window polish: puts RibbonKit-specific properties under one "RibbonKit"
/// category and gives them descriptions (the text shown at the bottom of the VS Properties grid).
/// Applied through the design attribute table, so runtime RibbonKit stays free of design attributes.
/// </summary>
internal static class PropertyMetadata
{
    private const string Category = "RibbonKit";

    public static void Register(AttributeTableBuilder b)
    {
        // ── Ribbon ───────────────────────────────────────────────────────────────────────
        Describe(b, "RibbonKit.Controls.Ribbon", "QuickAccessPosition",
            "Where the Quick Access Toolbar renders: title bar, tab row (default), or below the ribbon.");
        Describe(b, "RibbonKit.Controls.Ribbon", "IsMinimized",
            "Collapse the ribbon to just its tab headers.");
        Describe(b, "RibbonKit.Controls.Ribbon", "Backstage",
            "The File-button content (typically a Backstage). The File button is hidden while this is null.");
        Describe(b, "RibbonKit.Controls.Ribbon", "ApplicationButtonHeader",
            "Text of the application (File) button. Default: \"File\".");
        // IsBackstageOpen and SelectedIndex are RUNTIME properties: editing them in the Properties
        // grid changes runtime behavior (the grid can't write the design-time-only "d:" namespace).
        // Hide IsBackstageOpen from the grid — an app launching with the backstage open is ~always a
        // mistake; preview it via d:IsBackstageOpen in XAML. Browsable(false) is grid-only; the
        // property stays fully usable in XAML/binding and at runtime.
        b.AddCustomAttributes("RibbonKit.Controls.Ribbon", "IsBackstageOpen",
            new BrowsableAttribute(false),
            new CategoryAttribute(Category),
            new DescriptionAttribute(
                "Opens the backstage at RUNTIME. For a design-time-only preview, add d:IsBackstageOpen=\"True\" in XAML."));

        // SelectedIndex stays visible (choosing a startup tab is legitimate), but warn it's runtime.
        Describe(b, "RibbonKit.Controls.Ribbon", "SelectedIndex",
            "Selected tab index — a RUNTIME property (sets the startup tab). For a design-time-only preview that won't affect runtime, add d:SelectedIndex=\"N\" in XAML.");

        // ── Button family (shared properties) ────────────────────────────────────────────
        foreach (string type in new[]
        {
            "RibbonKit.Controls.RibbonButton",
            "RibbonKit.Controls.RibbonToggleButton",
            "RibbonKit.Controls.RibbonSplitButton",
            "RibbonKit.Controls.RibbonDropDownButton",
        })
        {
            Describe(b, type, "Header", "The button's label text.");
            Describe(b, type, "Icon", "16px icon used by the Medium and Small layouts.");
            Describe(b, type, "LargeIcon", "32px icon used by the Large layout (falls back to Icon).");
            Describe(b, type, "Size", "Current render size: Large, Medium, or Small.");
            Describe(b, type, "SizeDefinition",
                "Comma-separated sizes for the Large/Medium/Small group states, e.g. \"Large, Medium, Small\".");
            Describe(b, type, "ScreenTipTitle", "Bold first line of the rich ScreenTip tooltip.");
            Describe(b, type, "ScreenTipText", "Descriptive body of the ScreenTip tooltip.");
        }

        // ── Group / Tab ──────────────────────────────────────────────────────────────────
        Describe(b, "RibbonKit.Controls.RibbonGroup", "Header", "The group's title, shown under its content.");
        Describe(b, "RibbonKit.Controls.RibbonGroup", "Layout",
            "Item arrangement: Default (content-driven), Stacked (3-row columns), or Large (single row).");

        Describe(b, "RibbonKit.Controls.RibbonTab", "Header", "The tab's header text in the tab strip.");
        Describe(b, "RibbonKit.Controls.RibbonTab", "ContextualColor",
            "Tint for a contextual tab's header and underline; setting it marks the tab contextual.");

        // ── Backstage ────────────────────────────────────────────────────────────────────
        Describe(b, "RibbonKit.Controls.Backstage", "Design",
            "Chrome design: Classic (accent column) or Modern (light rail).");
        Describe(b, "RibbonKit.Controls.Backstage", "Translucent",
            "Render semi-transparent so a window backdrop (Mica) shows through.");

        Describe(b, "RibbonKit.Controls.BackstageTabItem", "Icon",
            "Optional glyph shown left of the header (tinted silhouette).");
        Describe(b, "RibbonKit.Controls.BackstageTabItem", "IsButton",
            "Make this an action (e.g. Options/Exit) instead of a page.");
        Describe(b, "RibbonKit.Controls.BackstageTabItem", "Placement",
            "Pin to the Top (default) or Bottom (footer) of the nav column.");
        Describe(b, "RibbonKit.Controls.BackstageTabItem", "Command",
            "Command run when an IsButton item is activated.");
    }

    private static void Describe(AttributeTableBuilder b, string type, string property, string description) =>
        b.AddCustomAttributes(type, property, new CategoryAttribute(Category), new DescriptionAttribute(description));
}
