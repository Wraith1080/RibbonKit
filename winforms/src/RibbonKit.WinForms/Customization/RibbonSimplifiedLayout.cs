using System.Text.Json.Serialization;

namespace RibbonKit.WinForms;

/// <summary>Data-only Simplified overrides. An absent tab uses authored defaults; an empty command list means More-only.</summary>
public sealed class RibbonSimplifiedLayout
{
    /// <summary>Creates an empty override set.</summary>
    public RibbonSimplifiedLayout() { }
    /// <summary>Gets or sets independent per-tab command selections.</summary>
    [JsonRequired] public List<RibbonSimplifiedTab> Tabs { get; set; } = [];
}
/// <summary>An ordered Simplified command selection for one ordinary tab.</summary>
public sealed class RibbonSimplifiedTab
{
    /// <summary>Creates an empty tab selection.</summary>
    public RibbonSimplifiedTab() { }
    /// <summary>Gets or sets the stable authored/custom tab identity.</summary>
    [JsonRequired] public string TabId { get; set; } = "";
    /// <summary>Gets or sets ordered original command identities and presentation sizes.</summary>
    [JsonRequired] public List<RibbonSimplifiedCommand> Commands { get; set; } = [];
}
/// <summary>A source-backed Simplified command preference.</summary>
public sealed class RibbonSimplifiedCommand
{
    /// <summary>Creates a Medium command preference.</summary>
    public RibbonSimplifiedCommand() { }
    /// <summary>Gets or sets the original command identity.</summary>
    [JsonRequired] public string CommandId { get; set; } = "";
    /// <summary>Gets or sets Small (icon only) or Medium (icon and label).</summary>
    [JsonRequired] public RibbonItemSize Size { get; set; } = RibbonItemSize.Medium;
}
