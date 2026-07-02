namespace RibbonKit.Layout;

/// <summary>
/// Parses SizeDefinition strings such as <c>"Large, Medium, Small"</c>, which declare
/// the sizes a control renders at as its parent group's state steps down.
/// </summary>
public static class RibbonSizeDefinition
{
    /// <summary>
    /// Parses a comma-separated size definition. Valid tokens: <c>Large</c>,
    /// <c>Medium</c> (alias <c>Middle</c>), <c>Small</c> — case-insensitive.
    /// </summary>
    /// <exception cref="ArgumentException">The string is empty or contains an unknown token.</exception>
    public static RibbonControlSize[] Parse(string definition)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(definition);

        var parts = definition.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            throw new ArgumentException("Size definition contains no entries.", nameof(definition));
        }

        var sizes = new RibbonControlSize[parts.Length];
        for (int i = 0; i < parts.Length; i++)
        {
            sizes[i] = parts[i].ToLowerInvariant() switch
            {
                "large" => RibbonControlSize.Large,
                "medium" or "middle" => RibbonControlSize.Medium,
                "small" => RibbonControlSize.Small,
                _ => throw new ArgumentException(
                    $"Unknown size token '{parts[i]}'. Expected Large, Medium, or Small.", nameof(definition)),
            };
        }

        return sizes;
    }

    /// <summary>
    /// Returns the control size a definition prescribes for a group state, clamping
    /// to the last entry when the definition has fewer entries than there are states.
    /// </summary>
    public static RibbonControlSize SizeFor(string definition, RibbonGroupSizeState state)
    {
        var sizes = Parse(definition);
        int index = Math.Min((int)state, sizes.Length - 1);
        return sizes[index];
    }
}
