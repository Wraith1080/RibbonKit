using System.Text.Json;
using System.Text.Json.Serialization;

namespace RibbonKit.WinForms.Showcase.Persistence;

// Application preferences deliberately stay outside the library's customization schema.
internal sealed record ShowcasePreferences
{
    [JsonRequired] public string Format { get; init; } = "RibbonKit.WinForms.Showcase.Preferences";
    [JsonRequired] public int Version { get; init; } = 1;
    public RibbonLayoutMode LayoutMode { get; init; }
    public RibbonDensity Density { get; init; }
    public bool IsMinimized { get; init; }
    public RibbonAnimationLevel AnimationLevel { get; init; } = RibbonAnimationLevel.Subtle;
    public RibbonTabContentAnimation TabContentAnimation { get; init; }
    public bool WordWrap { get; init; } = true;
    public ShowcaseWindowPlacement? Window { get; init; }

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow, MaxDepth = 8,
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false) }
    };

    internal string ToJson() => JsonSerializer.Serialize(this, Options);
    internal static ShowcasePreferences FromJson(string json)
    {
        var value = JsonSerializer.Deserialize<ShowcasePreferences>(json, Options)
            ?? throw new InvalidOperationException("The preferences file is empty.");
        if (value.Format != new ShowcasePreferences().Format || value.Version != 1
            || !Enum.IsDefined(value.LayoutMode) || !Enum.IsDefined(value.Density)
            || !Enum.IsDefined(value.AnimationLevel) || !Enum.IsDefined(value.TabContentAnimation))
            throw new InvalidOperationException("The preferences format or version is unsupported.");
        value.Window?.Validate();
        return value;
    }
}

internal sealed record ShowcaseWindowPlacement(int X, int Y, int Width, int Height, int Dpi, bool Maximized)
{
    internal void Validate()
    {
        if (Math.Abs((long)X) > 1_000_000 || Math.Abs((long)Y) > 1_000_000
            || Width is < 1 or > 100_000 || Height is < 1 or > 100_000 || Dpi is < 48 or > 960)
            throw new InvalidOperationException("The saved window bounds are invalid.");
    }

    internal Rectangle Bounds => new(X, Y, Width, Height);
    internal Rectangle Fit(Rectangle workArea, Size minimum, int currentDpi)
    {
        int width = Math.Min(workArea.Width, Math.Max(minimum.Width, (int)Math.Round((double)Width * currentDpi / Dpi)));
        int height = Math.Min(workArea.Height, Math.Max(minimum.Height, (int)Math.Round((double)Height * currentDpi / Dpi)));
        return new(Math.Clamp(X, workArea.Left, workArea.Right - width), Math.Clamp(Y, workArea.Top, workArea.Bottom - height), width, height);
    }
}
