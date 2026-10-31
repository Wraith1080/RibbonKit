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
    [DataMember] public List<RibbonDesignNode> Tabs { get; set; } = new();
    [DataMember] public List<RibbonDesignNode> Templates { get; set; } = new();
    [DataMember] public List<string> ReservedNames { get; set; } = new();
    [DataMember] public List<string> ReservedIds { get; set; } = new();

    public IEnumerable<RibbonDesignNode> Nodes => Flatten(Tabs);
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
        "Group" => new[] { "Button", "Toggle", "CheckBox", "RadioButton", "DropDown", "SplitButton", "TextBox", "ComboBox", "Gallery" },
        "DropDown" or "SplitButton" => new[] { "Button", "Toggle", "CheckBox", "RadioButton", "DropDown", "SplitButton", "TextBox", "ComboBox", "Gallery", "Separator" },
        "Gallery" => new[] { "GalleryTile" },
        _ => Array.Empty<string>()
    };
    public void Validate()
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);
        var names = new HashSet<string>(ReservedNames, StringComparer.OrdinalIgnoreCase);
        var ids = new HashSet<string>(ReservedIds, StringComparer.Ordinal);
        Check(Tabs, "Ribbon");
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
public sealed class RibbonDesignNode
{
    [DataMember] public string Key { get; set; } = "";
    [DataMember] public string Kind { get; set; } = "";
    [DataMember] public string ComponentName { get; set; } = "";
    [DataMember] public List<RibbonDesignProperty> Properties { get; set; } = new();
    [DataMember] public List<string> Choices { get; set; } = new();
    [DataMember] public List<RibbonDesignNode> Children { get; set; } = new();
    [DataMember] public RibbonDesignIcon Icon { get; set; } = new();
    public string Caption => Properties.FirstOrDefault(property => property.Name == "Text")?.Value ?? ComponentName;
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
