using System.Text.Json;
using System.Text.Json.Serialization;

namespace RibbonKit.WinForms;

/// <summary>Data-only, versioned QAT customization state. Appearance, command state and application storage are excluded.</summary>
public sealed class RibbonCustomizationState
{
    /// <summary>Initializes a version-one empty QAT draft.</summary>
    public RibbonCustomizationState() { }
    /// <summary>Gets or sets the native format discriminator.</summary>
    [JsonRequired] public string Format { get; set; } = "RibbonKit.WinForms.Customization";
    /// <summary>Gets or sets the schema version. This release supports version one.</summary>
    [JsonRequired] public int Version { get; set; } = 1;
    /// <summary>Gets or sets the application's stable profile identity.</summary>
    [JsonRequired] public string ApplicationId { get; set; } = "";
    /// <summary>Gets or sets the desired QAT placement.</summary>
    [JsonRequired] public RibbonQuickAccessPosition QuickAccessPosition { get; set; }
    /// <summary>Gets or sets toolbar visibility without changing command visibility.</summary>
    [JsonRequired] public bool QuickAccessVisible { get; set; } = true;
    /// <summary>Gets or sets the ordered permanent QAT presentations.</summary>
    [JsonRequired] public List<RibbonCustomizationEntry> QuickAccess { get; set; } = [];
}

/// <summary>A presentation identity referring to one catalog source; contains no model or UI object.</summary>
public sealed class RibbonCustomizationEntry
{
    /// <summary>Initializes a new presentation with a generated identity.</summary>
    public RibbonCustomizationEntry() { }
    /// <summary>Gets or sets the presentation identity, distinct from its source identity. Keep it when moving an entry.</summary>
    [JsonRequired] public string Id { get; set; } = "qat:" + Guid.NewGuid().ToString("N");
    /// <summary>Gets or sets the catalog source ID.</summary>
    [JsonRequired] public string CommandId { get; set; } = "";
}

/// <summary>Converts native customization data to/from JSON. It never reads/writes files or applies state to controls.</summary>
public static class RibbonCustomizationSerializer
{
    private static readonly JsonSerializerOptions Options = CreateOptions();
    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow, MaxDepth = 12 };
        options.Converters.Add(new JsonStringEnumConverter<RibbonQuickAccessPosition>(allowIntegerValues: false));
        return options;
    }
    /// <summary>Serializes structurally valid native state. A session's ExportJson additionally validates source identities.</summary>
    public static string Serialize(RibbonCustomizationState state)
    {
        Validate(state, state?.ApplicationId ?? "");
        return JsonSerializer.Serialize(state, Options);
    }
    /// <summary>Rejects malformed, duplicate-property, foreign, unsupported-version and structurally invalid JSON. No live model is changed.</summary>
    public static RibbonCustomizationState Deserialize(string json, string applicationId)
    {
        ArgumentNullException.ThrowIfNull(json); ArgumentException.ThrowIfNullOrWhiteSpace(applicationId);
        if (json.Length > 1_048_576) throw new JsonException("Customization JSON exceeds the supported size.");
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 12 });
        CheckDuplicates(document.RootElement);
        var state = JsonSerializer.Deserialize<RibbonCustomizationState>(json, Options) ?? throw new JsonException("Customization state is null.");
        Validate(state, applicationId); return state;
    }
    private static void CheckDuplicates(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in element.EnumerateObject())
            { if (!names.Add(property.Name)) throw new JsonException("Duplicate customization property: " + property.Name); CheckDuplicates(property.Value); }
        }
        else if (element.ValueKind == JsonValueKind.Array) foreach (var item in element.EnumerateArray()) CheckDuplicates(item);
    }
    internal static void Validate(RibbonCustomizationState? state, string applicationId)
    {
        ArgumentNullException.ThrowIfNull(state); ArgumentException.ThrowIfNullOrWhiteSpace(applicationId);
        if (state.Format != "RibbonKit.WinForms.Customization" || state.Version != 1)
            throw new InvalidOperationException("Unsupported native customization format or version.");
        if (!StringComparer.Ordinal.Equals(state.ApplicationId, applicationId)) throw new InvalidOperationException("Customization belongs to another application.");
        if (!Enum.IsDefined(state.QuickAccessPosition) || state.QuickAccess is null) throw new InvalidOperationException("Invalid QAT customization state.");
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in state.QuickAccess)
            if (entry is null || string.IsNullOrWhiteSpace(entry.Id) || string.IsNullOrWhiteSpace(entry.CommandId) || !ids.Add(entry.Id))
                throw new InvalidOperationException("QAT entries need unique presentation IDs and nonempty command IDs.");
    }
    internal static RibbonCustomizationState Copy(RibbonCustomizationState state) => new()
    {
        ApplicationId = state.ApplicationId, Format = state.Format, Version = state.Version,
        QuickAccessPosition = state.QuickAccessPosition, QuickAccessVisible = state.QuickAccessVisible,
        QuickAccess = state.QuickAccess.Select(e => new RibbonCustomizationEntry { Id = e.Id, CommandId = e.CommandId }).ToList()
    };
    internal static bool Same(RibbonCustomizationState first, RibbonCustomizationState second) =>
        first.QuickAccessPosition == second.QuickAccessPosition && first.QuickAccessVisible == second.QuickAccessVisible &&
        first.QuickAccess.Select(e => (e.Id, e.CommandId)).SequenceEqual(second.QuickAccess.Select(e => (e.Id, e.CommandId)));
}
