using System.Text.Json.Serialization;

namespace RibbonKit.WinForms;

/// <summary>Data-only ordinary Ribbon hierarchy. Contextual, merge and modal presentation is excluded.</summary>
public sealed class RibbonCustomizationLayout
{
    /// <summary>Creates an empty hierarchy.</summary>
    public RibbonCustomizationLayout() { }
    /// <summary>Gets or sets ordinary tabs in presentation order.</summary>
    [JsonRequired] public List<RibbonCustomizationTab> Tabs { get; set; } = [];
}
/// <summary>An authored tab identity or a user-created tab declaration.</summary>
public sealed class RibbonCustomizationTab
{
    /// <summary>Creates a custom tab declaration.</summary>
    public RibbonCustomizationTab() { }
    /// <summary>Gets or sets the stable tab identity; authored nodes use their source ID.</summary>
    [JsonRequired] public string Id { get; set; } = "user.tab:" + Guid.NewGuid().ToString("N");
    /// <summary>Gets or sets whether this is a user-created tab.</summary>
    [JsonRequired] public bool IsCustom { get; set; } = true;
    /// <summary>Gets or sets a presentation label, or null to use the authored label.</summary>
    public string? Label { get; set; }
    /// <summary>Gets or sets the user's visibility preference; application visibility remains authoritative.</summary>
    [JsonRequired] public bool Visible { get; set; } = true;
    /// <summary>Gets or sets ordered groups.</summary>
    [JsonRequired] public List<RibbonCustomizationGroup> Groups { get; set; } = [];
}
/// <summary>An authored group identity or a user-created group with command presentations.</summary>
public sealed class RibbonCustomizationGroup
{
    /// <summary>Creates a custom group declaration.</summary>
    public RibbonCustomizationGroup() { }
    /// <summary>Gets or sets the stable group identity.</summary>
    [JsonRequired] public string Id { get; set; } = "user.group:" + Guid.NewGuid().ToString("N");
    /// <summary>Gets or sets whether commands in this group are user-editable.</summary>
    [JsonRequired] public bool IsCustom { get; set; } = true;
    /// <summary>Gets or sets a presentation label, or null for the authored label.</summary>
    public string? Label { get; set; }
    /// <summary>Gets or sets custom command presentations. Authored groups must leave this empty.</summary>
    [JsonRequired] public List<RibbonCustomizationCommand> Commands { get; set; } = [];
}
/// <summary>A custom-group presentation borrowing one catalog command.</summary>
public sealed class RibbonCustomizationCommand
{
    /// <summary>Creates a command presentation identity.</summary>
    public RibbonCustomizationCommand() { }
    /// <summary>Gets or sets the presentation ID, distinct from its source command.</summary>
    [JsonRequired] public string Id { get; set; } = "user.command:" + Guid.NewGuid().ToString("N");
    /// <summary>Gets or sets the catalog source ID.</summary>
    [JsonRequired] public string CommandId { get; set; } = "";
    /// <summary>Gets or sets a presentation label, or null to follow the source.</summary>
    public string? Label { get; set; }
    /// <summary>Gets or sets the preferred command presentation size. Editors keep their native compact layout.</summary>
    [JsonRequired] public RibbonItemSize Size { get; set; } = RibbonItemSize.Medium;
}
