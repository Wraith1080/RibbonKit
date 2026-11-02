using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace RibbonKit.WinForms.Design.Protocol;

[DataContract]
public sealed class RibbonDesignSnapshot
{
    [DataMember] public string Revision { get; set; } = "";
    [DataMember] public string SelectionKey { get; set; } = "";
    [DataMember] public string ComponentName { get; set; } = "";
    [DataMember] public string BuiltInTheme { get; set; } = "";
    [DataMember] public string ThemeRevision { get; set; } = "";
    [DataMember] public string ApplicationTheme { get; set; } = "";
    [DataMember] public string[] ThemeChoices { get; set; } = Array.Empty<string>();
    [DataMember] public string AnimationLevel { get; set; } = "None";
    [DataMember] public string[] AnimationChoices { get; set; } = Array.Empty<string>();
    [DataMember] public string TabContentAnimation { get; set; } = "Fade";
    [DataMember] public string[] TabContentAnimationChoices { get; set; } = Array.Empty<string>();
    [DataMember] public bool CanSetBackdrop { get; set; }
    [DataMember] public string Backdrop { get; set; } = "None";
    [DataMember] public string[] BackdropChoices { get; set; } = Array.Empty<string>();
    [DataMember] public float NavigationBackgroundOpacity { get; set; } = 1;
    [DataMember] public List<RibbonDesignNode> Tabs { get; set; } = new();
    [DataMember] public List<RibbonDesignNode> CatalogCommands { get; set; } = new();
    [DataMember] public List<RibbonDesignNode> QuickAccessItems { get; set; } = new();
    [DataMember] public List<RibbonDesignSource> QuickAccessSources { get; set; } = new();
    [DataMember] public bool QuickAccessVisible { get; set; } = true;
    [DataMember] public string QuickAccessPosition { get; set; } = "TabRow";
    [DataMember] public List<RibbonDesignNode> Templates { get; set; } = new();
    [DataMember] public List<string> ReservedNames { get; set; } = new();
    [DataMember] public List<string> ReservedIds { get; set; } = new();

    public IEnumerable<RibbonDesignNode> Nodes => Flatten(Tabs);
    public IEnumerable<RibbonDesignNode> CommandNodes => Nodes.Concat(Flatten(CatalogCommands));
    public IEnumerable<RibbonDesignNode> AllNodes => CommandNodes.Concat(QuickAccessItems.Where(node => node.Kind != "OwnedQat"));
    public static bool IsQuickAccessReference(string kind) => kind is "QatReference" or "QatMenuReference" or "QatComboReference" or "QatGroupReference";
    public static string QuickAccessKind(string sourceKind) => sourceKind switch
    {
        "Button" or "Toggle" or "CheckBox" or "RadioButton" => "QatReference",
        "DropDown" or "SplitButton" or "Gallery" => "QatMenuReference",
        "ComboBox" => "QatComboReference",
        "Group" => "QatGroupReference",
        _ => ""
    };
    public static IEnumerable<RibbonDesignNode> Flatten(IEnumerable<RibbonDesignNode> nodes)
    {
        foreach (var node in nodes)
        {
            yield return node;
            foreach (var child in Flatten(node.Children)) yield return child;
        }
    }
    public static string[] ChildKinds(string parent) => parent switch
    {
        "Ribbon" => new[] { "Tab" },
        "Tab" => new[] { "Group" },
        "Group" => new[] { "Button", "Toggle", "CheckBox", "RadioButton", "DropDown", "SplitButton", "TextBox", "ComboBox", "Gallery", "Separator" },
        "DropDown" or "SplitButton" => new[] { "Button", "Toggle", "CheckBox", "RadioButton", "DropDown", "SplitButton", "TextBox", "ComboBox", "Gallery", "Separator" },
        "Gallery" => new[] { "GalleryTile" },
        "Catalog" => new[] { "Button", "Toggle", "CheckBox", "RadioButton", "DropDown", "SplitButton", "TextBox", "ComboBox", "Gallery", "Separator", "GalleryTile" },
        "QuickAccess" => new[] { "QatReference", "QatMenuReference", "QatComboReference", "QatGroupReference" },
        _ => Array.Empty<string>()
    };
    public void Validate()
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);
        var names = new HashSet<string>(ReservedNames, StringComparer.OrdinalIgnoreCase);
        var ids = new HashSet<string>(ReservedIds, StringComparer.Ordinal);
        Check(Tabs, "Ribbon");
        Check(CatalogCommands, "Catalog");
        Check(QuickAccessItems.Where(node => node.Kind != "OwnedQat"), "QuickAccess");
        var sourceKeys = new HashSet<string>(CommandNodes.Select(node => node.Key).Concat(QuickAccessSources.Select(source => source.Key)), StringComparer.Ordinal);
        var ownedSources = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in QuickAccessItems)
        {
            if (entry.Kind == "OwnedQat")
            {
                if (string.IsNullOrWhiteSpace(entry.Key) || !keys.Add(entry.Key) || !ownedSources.Add(entry.SourceKey) ||
                    !CatalogCommands.Any(node => node.Key == entry.SourceKey && node.Kind is "Button" or "Toggle") ||
                    entry.Properties.Count != 0 || entry.Children.Count != 0 || entry.Choices.Count != 0 || entry.Icon.Kind != "None" || entry.ComponentName.Length != 0)
                    throw new ArgumentException("Invalid owned Quick Access entry.");
            }
            if (entry.SourceKey.Length != 0 && !sourceKeys.Contains(entry.SourceKey)) throw new ArgumentException("Remove the Quick Access reference before deleting its source command.");
        }
        if (CommandNodes.Any(node => node.SourceKey.Length != 0)) throw new ArgumentException("Command declarations cannot contain a Quick Access source reference.");
        void Check(IEnumerable<RibbonDesignNode> nodes, string parent)
        {
            foreach (var node in nodes)
            {
                if (node == null || string.IsNullOrWhiteSpace(node.Key) || !keys.Add(node.Key)) throw new ArgumentException("Each component must occur once in the ribbon.");
                if (!ChildKinds(parent).Contains(node.Kind)) throw new ArgumentException(node.Kind + " cannot be added to " + parent + ".");
                if (string.IsNullOrWhiteSpace(node.ComponentName) || !names.Add(node.ComponentName)) throw new ArgumentException("Each component needs a unique component name.");
                var id = node.Properties.SingleOrDefault(property => property.Name == "Id")?.Value;
                if (string.IsNullOrWhiteSpace(id) || !ids.Add(id!)) throw new ArgumentException("Each component needs a nonempty, unique Id.");
                if (node.Properties.GroupBy(property => property.Name, StringComparer.Ordinal).Any(group => group.Count() != 1)) throw new ArgumentException("Duplicate property in the draft.");
                Check(node.Children, node.Kind);
            }
        }
    }
    public string Serialize() => SerializeValue(this);
    public static RibbonDesignSnapshot Deserialize(string json)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        return (RibbonDesignSnapshot)new DataContractJsonSerializer(typeof(RibbonDesignSnapshot)).ReadObject(stream)!;
    }
    public static string SerializeValue(RibbonDesignSnapshot value)
    {
        using var stream = new MemoryStream();
        new DataContractJsonSerializer(typeof(RibbonDesignSnapshot)).WriteObject(stream, value);
        return Encoding.UTF8.GetString(stream.ToArray());
    }
}

[DataContract]
public sealed class RibbonThemePreview
{
    [DataMember] public string RibbonPng { get; set; } = "";
    [DataMember] public string BackstagePng { get; set; } = "";
}

[DataContract]
public sealed class RibbonDesignNode
{
    [DataMember] public string Key { get; set; } = "";
    [DataMember] public string Kind { get; set; } = "";
    [DataMember] public string ComponentName { get; set; } = "";
    [DataMember] public List<RibbonDesignProperty> Properties { get; set; } = new();
    [DataMember] public List<string> Choices { get; set; } = new();
    [DataMember] public List<RibbonDesignNode> Children { get; set; } = new();
    [DataMember] public RibbonDesignIcon Icon { get; set; } = new();
    [DataMember] public string SourceKey { get; set; } = "";
    public string Caption => Properties.FirstOrDefault(property => property.Name == "Text")?.Value ?? ComponentName;
}

[DataContract]
public sealed class RibbonDesignSource
{
    [DataMember] public string Key { get; set; } = "";
    [DataMember] public string Kind { get; set; } = "";
    [DataMember] public string Caption { get; set; } = "";
    [DataMember] public string Category { get; set; } = "";
}

[DataContract]
public sealed class RibbonDesignIcon
{
    [DataMember] public string Kind { get; set; } = "None";
    [DataMember] public string Source { get; set; } = "";
    [DataMember] public bool UseForegroundColor { get; set; }
    [DataMember] public string Name { get; set; } = "";
    [DataMember] public bool HasRasterImage { get; set; }
}

[DataContract]
public sealed class RibbonDesignProperty
{
    [DataMember] public string Name { get; set; } = "";
    [DataMember] public string Category { get; set; } = "";
    [DataMember] public string Description { get; set; } = "";
    [DataMember] public string Type { get; set; } = "String";
    [DataMember] public string Value { get; set; } = "";
    [DataMember] public string[] Options { get; set; } = Array.Empty<string>();
}
