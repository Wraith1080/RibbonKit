using System.Text.Json;
using System.Text.Json.Serialization;

namespace RibbonKit.WinForms;

/// <summary>Data-only, versioned QAT and optional Ribbon customization. Appearance, command values and application storage are excluded.</summary>
public sealed class RibbonCustomizationState
{
    /// <summary>Initializes a version-two profile with an empty QAT and no Ribbon override.</summary>
    public RibbonCustomizationState() { }
    /// <summary>Gets or sets the native format discriminator.</summary>
    [JsonRequired] public string Format { get; set; } = "RibbonKit.WinForms.Customization";
    /// <summary>Gets or sets the schema version. Version one QAT profiles remain readable; version two adds optional Ribbon structure.</summary>
    [JsonRequired] public int Version { get; set; } = 2;
    /// <summary>Gets or sets the application's stable profile identity.</summary>
    [JsonRequired] public string ApplicationId { get; set; } = "";
    /// <summary>Gets or sets the desired QAT placement.</summary>
    [JsonRequired] public RibbonQuickAccessPosition QuickAccessPosition { get; set; }
    /// <summary>Gets or sets toolbar visibility without changing command visibility.</summary>
    [JsonRequired] public bool QuickAccessVisible { get; set; } = true;
    /// <summary>Gets or sets the ordered permanent QAT presentations.</summary>
    [JsonRequired] public List<RibbonCustomizationEntry> QuickAccess { get; set; } = [];
    /// <summary>Gets or sets an ordinary Ribbon override. Null preserves the current structure when applying an older/QAT-only profile.</summary>
    public RibbonCustomizationLayout? Ribbon { get; set; }
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
        options.Converters.Add(new JsonStringEnumConverter<RibbonItemSize>(allowIntegerValues: false));
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
        if (state.Format != "RibbonKit.WinForms.Customization" || state.Version is not (1 or 2) || state.Version == 1 && state.Ribbon is not null)
            throw new InvalidOperationException("Unsupported native customization format or version.");
        if (!StringComparer.Ordinal.Equals(state.ApplicationId, applicationId)) throw new InvalidOperationException("Customization belongs to another application.");
        if (!Enum.IsDefined(state.QuickAccessPosition) || state.QuickAccess is null) throw new InvalidOperationException("Invalid QAT customization state.");
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in state.QuickAccess)
            if (entry is null || string.IsNullOrWhiteSpace(entry.Id) || string.IsNullOrWhiteSpace(entry.CommandId) || !ids.Add(entry.Id))
                throw new InvalidOperationException("QAT entries need unique presentation IDs and nonempty command IDs.");
        ValidateLayout(state.Ribbon);
    }
    internal static RibbonCustomizationState Copy(RibbonCustomizationState state) => new()
    {
        ApplicationId = state.ApplicationId, Format = state.Format, Version = state.Version,
        QuickAccessPosition = state.QuickAccessPosition, QuickAccessVisible = state.QuickAccessVisible,
        QuickAccess = state.QuickAccess.Select(e => new RibbonCustomizationEntry { Id = e.Id, CommandId = e.CommandId }).ToList(), Ribbon = CopyLayout(state.Ribbon)
    };
    internal static bool Same(RibbonCustomizationState first, RibbonCustomizationState second) =>
        first.QuickAccessPosition == second.QuickAccessPosition && first.QuickAccessVisible == second.QuickAccessVisible &&
        first.QuickAccess.Select(e => (e.Id, e.CommandId)).SequenceEqual(second.QuickAccess.Select(e => (e.Id, e.CommandId)));
    internal static RibbonCustomizationLayout? CopyLayout(RibbonCustomizationLayout? layout) => layout is null ? null : JsonSerializer.Deserialize<RibbonCustomizationLayout>(JsonSerializer.Serialize(layout, Options), Options);
    internal static bool SameLayout(RibbonCustomizationLayout? first, RibbonCustomizationLayout? second) => JsonSerializer.Serialize(first, Options) == JsonSerializer.Serialize(second, Options);
    private static void ValidateLayout(RibbonCustomizationLayout? layout)
    {
        if (layout is null) return;
        if (layout.Tabs is null || layout.Tabs.Count > 256) throw new InvalidOperationException("Invalid Ribbon tab list.");
        var ids = new HashSet<string>(StringComparer.Ordinal);
        void Node(string id, string? label)
        {
            if (string.IsNullOrWhiteSpace(id) || !ids.Add(id) || label is not null && (string.IsNullOrWhiteSpace(label) || label.Length > 512))
                throw new InvalidOperationException("Ribbon nodes need unique IDs and nonempty labels of at most 512 characters.");
            if (ids.Count > 10000) throw new InvalidOperationException("Ribbon profile contains too many nodes.");
        }
        foreach (var tab in layout.Tabs)
        {
            if (tab is null || tab.Groups is null) throw new InvalidOperationException("Invalid Ribbon tab."); Node(tab.Id, tab.Label);
            foreach (var group in tab.Groups)
            {
                if (group is null || group.Commands is null || !group.IsCustom && group.Commands.Count != 0) throw new InvalidOperationException("Only custom groups have editable command entries."); Node(group.Id, group.Label);
                foreach (var command in group.Commands)
                {
                    if (command is null || string.IsNullOrWhiteSpace(command.CommandId) || !Enum.IsDefined(command.Size)) throw new InvalidOperationException("Invalid Ribbon command presentation."); Node(command.Id, command.Label);
                }
            }
        }
        if (layout.Tabs.Count > 0 && !layout.Tabs.Any(t => t.Visible)) throw new InvalidOperationException("Keep at least one ordinary tab visible.");
    }
}
