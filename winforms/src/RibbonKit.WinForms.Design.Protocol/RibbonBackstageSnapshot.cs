using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace RibbonKit.WinForms.Design.Protocol;

[DataContract]
public sealed class RibbonBackstageSnapshot
{
    [DataMember] public string Revision { get; set; } = "";
    [DataMember] public string SelectionKey { get; set; } = "";
    [DataMember] public string ContentRevision { get; set; } = "";
    [DataMember] public List<RibbonDesignProperty> Settings { get; set; } = new();
    [DataMember] public List<RibbonDesignNode> Entries { get; set; } = new();
    [DataMember] public List<RibbonDesignNode> Templates { get; set; } = new();
    [DataMember] public List<string> ReservedNames { get; set; } = new();
    [DataMember] public List<string> ReservedIds { get; set; } = new();
    [DataMember] public Dictionary<string, string> ContentNames { get; set; } = new();
    [DataMember] public int PreviewWidth { get; set; } = 700;
    [DataMember] public int PreviewHeight { get; set; } = 260;
    [DataMember] public int PreviewDpi { get; set; } = 96;
    public string Serialize() => Json(this);
    public static RibbonBackstageSnapshot Deserialize(string json) => FromJson<RibbonBackstageSnapshot>(json);
    public static string Json<T>(T value)
    {
        using var stream = new MemoryStream(); new DataContractJsonSerializer(typeof(T)).WriteObject(stream, value);
        return Encoding.UTF8.GetString(stream.ToArray());
    }
    public static T FromJson<T>(string json)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        return (T)new DataContractJsonSerializer(typeof(T)).ReadObject(stream)!;
    }
}
[DataContract]
public sealed class RibbonBackstagePreview
{
    [DataMember] public string Png { get; set; } = "";
    [DataMember] public List<RibbonBackstagePreviewRegion> Regions { get; set; } = new();
}
[DataContract]
public sealed class RibbonBackstagePreviewRegion
{
    [DataMember] public string Key { get; set; } = "";
    [DataMember] public int X { get; set; }
    [DataMember] public int Y { get; set; }
    [DataMember] public int Width { get; set; }
    [DataMember] public int Height { get; set; }
}
