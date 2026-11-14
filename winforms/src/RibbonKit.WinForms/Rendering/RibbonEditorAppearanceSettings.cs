namespace RibbonKit.WinForms;

/// <summary>Immutable native-field and choice-row decoration overrides. Native text, caret, selection, validation, field bounds and popup ownership retain their existing owners.</summary>
public sealed record RibbonEditorAppearanceSettings
{
    /// <summary>Gets optional choice-row selected/hover outlines and inner rims. Native field wells and field outlines are unaffected.</summary>
    public RibbonStateChromeSettings? StateChrome { get; init; }
    /// <summary>Gets choice-row outline width in logical pixels, 0..32. Null uses one logical pixel only when state chrome supplies ink; zero hides it. Native field borders are independent.</summary>
    public int? ChoiceBorderWidth { get; init; }
    /// <summary>Initializes overrides that retain existing editor appearance.</summary>
    public RibbonEditorAppearanceSettings() { }
    /// <summary>Gets optional field corner radius in logical pixels, 0..64. Null retains square neutral or the active Office radius. Native input and hit-target bounds are unchanged.</summary>
    public int? CornerRadius { get; init; }
    /// <summary>Gets optional field border width in logical pixels, 0..32. Null retains one device pixel when border ink exists; zero hides the border. Width clamps inside the painted field.</summary>
    public int? BorderWidth { get; init; }
    /// <summary>Gets optional field paint inset in logical pixels, 0..1024. Null retains one device pixel. Authored values clamp inside the measured field; native text bounds are unchanged.</summary>
    public int? Inset { get; init; }
    /// <summary>Gets optional active field-border ink, including alpha. Empty retains Accent; Transparent hides active border paint. TextBox focus and ComboBox hot/focus/press/open eligibility, including disabled masking, remain unchanged. EditorBorder still supplies resting ink.</summary>
    public Color ActiveBorderColor { get; init; } = Color.Empty;
    /// <summary>Gets optional active choice-row corner radius in logical pixels, 0..64. Null retains the actual renderer's square neutral or Office radius; field corners reset independently.</summary>
    public int? ChoiceCornerRadius { get; init; }
    /// <summary>Gets optional active choice-row paint inset in logical pixels, 0..1024. Null retains one device pixel. Authored values clamp inside the unchanged row target; zero fills it.</summary>
    public int? ChoiceInset { get; init; }
    /// <summary>Gets optional choice-text padding on each horizontal side in logical pixels, 0..1024. Null retains six. Authored padding clamps to the available row width without changing row targets or native input text bounds.</summary>
    public int? ChoiceTextHorizontalPadding { get; init; }

    internal bool HasFieldChrome => CornerRadius is not null || BorderWidth is not null || Inset is not null;
    internal bool HasChoiceChrome => StateChrome?.HasOverrides == true || ChoiceBorderWidth is not null || ChoiceCornerRadius is not null || ChoiceInset is not null;
    internal static void Validate(RibbonEditorAppearanceSettings? settings)
    {
        if (settings is null) return;
        if (settings.CornerRadius is < 0 or > 64 || settings.ChoiceCornerRadius is < 0 or > 64 || settings.BorderWidth is < 0 or > 32
            || settings.Inset is < 0 or > 1024 || settings.ChoiceInset is < 0 or > 1024 || settings.ChoiceTextHorizontalPadding is < 0 or > 1024)
            throw new ArgumentOutOfRangeException(nameof(settings), "Editor/choice radii must be 0..64, border width 0..32 and insets/padding 0..1024 logical pixels, or null for existing paint.");
    }
}
