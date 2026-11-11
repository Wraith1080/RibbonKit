namespace RibbonKit.WinForms.Layout;

// One logical role resolver for native fields, paint, previews and choice lists.
internal readonly record struct RibbonEditorMetrics(RibbonDensity Density, RibbonTheme? Theme)
{
    internal static RibbonEditorMetrics For(RibbonDensity density, RibbonTheme? theme = null) => new(density, theme);
    internal int LabelGap => Theme?.EditorLabelGap ?? 6;
    internal int TextInset => Theme?.EditorTextInset ?? 4;
    internal static bool HasHeightOverride(RibbonTheme? theme) => theme?.EditorMinimumHeight is not null || theme?.EditorVerticalPadding is not null;
    internal int MinimumHeight(bool simplified) => Math.Max(Theme?.EditorMinimumHeight ?? RibbonDensityMetrics.For(Density).EditorHeight,
        simplified ? RibbonDensityMetrics.For(Density).SimplifiedEditorMinimum : Density == RibbonDensity.Touch ? 42 : 0);
    internal int FieldPaddingHeight(int dpi) => Theme?.EditorVerticalPadding is { } padding
        ? 2 * RibbonLayoutEngine.Scale(padding, dpi) : RibbonLayoutEngine.Scale(8, dpi);
    internal int ComboButton => Math.Max(Theme?.ComboBoxButtonWidth ?? RibbonDensityMetrics.For(Density).ComboButton,
        Density == RibbonDensity.Touch ? 32 : 1);
    internal int ChoiceMinimum => Math.Max(Theme?.ComboBoxMinimumChoiceHeight ?? RibbonDensityMetrics.For(Density).ComboChoiceRow,
        Density == RibbonDensity.Touch ? 44 : 1);
    internal int ChoicePaddingHeight(int dpi) => Theme?.ComboBoxChoiceVerticalPadding is { } padding
        ? 2 * RibbonLayoutEngine.Scale(padding, dpi) : RibbonLayoutEngine.Scale(10, dpi);

    internal static void Validate(RibbonTheme theme)
    {
        RibbonEditorAppearanceSettings.Validate(theme.EditorAppearance);
        if (theme.EditorLabelGap is < 0 or > 1024 || theme.EditorTextInset is < 0 or > 1024 ||
            theme.ComboBoxButtonWidth is < 1 or > 1024 || theme.ComboBoxMinimumChoiceHeight is < 1 or > 1024 ||
            theme.ComboBoxChoiceVerticalPadding is < 0 or > 1024 || theme.EditorMinimumHeight is < 1 or > 1024 ||
            theme.EditorVerticalPadding is < 0 or > 1024)
            throw new ArgumentOutOfRangeException(nameof(theme), "Editor/choice padding and gaps must be 0..1024, and button/field/choice dimensions 1..1024, or null for density defaults.");
    }
}
