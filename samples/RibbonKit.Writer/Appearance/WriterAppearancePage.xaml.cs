using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using RibbonKit.Animation;
using RibbonKit.Controls;
using RibbonKit.Interop;
using RibbonKit.Theming;
using RibbonKit.Writer.Editing;

namespace RibbonKit.Writer.Appearance;

internal partial class WriterAppearancePage : UserControl
{
    private bool _updating;
    private string? _accent;

    public WriterAppearancePage()
    {
        InitializeComponent();
        SetPreferences(new WriterAppearancePreferences());
    }

    public event EventHandler<WriterAppearancePreferences>? PreferencesChanged;

    public event EventHandler? ApplyRequested;

    public WriterAppearancePreferences Preferences => BuildPreferences();

    public void SetPreferences(WriterAppearancePreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        preferences = WriterAppearanceCompatibility.Normalize(preferences);

        _updating = true;
        try
        {
            Select(ThemeCombo, preferences.Theme);
            Select(PaletteCombo, preferences.DarkPalette.ToString());
            _accent = preferences.Accent;
            CustomAccentCheck.IsChecked = _accent is not null;
            UpdateAccentButton();
            AccentedTitleBarCheck.IsChecked = preferences.AccentedTitleBar;
            Select(BackstageCombo, preferences.BackstageDesign);
            Select(BackdropCombo, preferences.Backdrop);
            Select(FrameCombo, preferences.FrameAppearance);
            Select(ApplicationButtonCombo, preferences.ApplicationButtonShape);
            Select(AnimationCombo, preferences.AnimationLevel);
            BackstageTranslucentCheck.IsChecked = preferences.BackstageTranslucent;
            RespectReducedMotionCheck.IsChecked = preferences.RespectSystemReducedMotion;
            ShowRulerCheck.IsChecked = preferences.ShowRuler;
            ShowMarginGuidesCheck.IsChecked = preferences.ShowMarginGuides;
            RefreshCompatibility(preferences);
        }
        finally
        {
            _updating = false;
        }
    }

    private WriterAppearancePreferences BuildPreferences()
    {
        var preferences = new WriterAppearancePreferences
        {
            Theme = Selected<RibbonTheme>(ThemeCombo),
            DarkPalette = string.Equals(
                SelectedTag(PaletteCombo)?.ToString(),
                bool.TrueString,
                StringComparison.OrdinalIgnoreCase),
            Accent = CustomAccentCheck.IsChecked == true ? _accent ?? "#FF2B579A" : null,
            AccentedTitleBar = AccentedTitleBarCheck.IsChecked == true,
            BackstageDesign = Selected<RibbonBackstageDesign>(BackstageCombo),
            Backdrop = Selected<RibbonBackdrop>(BackdropCombo),
            FrameAppearance = Selected<RibbonWindowFrameAppearance>(FrameCombo),
            ApplicationButtonShape = Selected<RibbonApplicationButtonShape>(ApplicationButtonCombo),
            AnimationLevel = Selected<RibbonAnimationLevel>(AnimationCombo),
            BackstageTranslucent = BackstageTranslucentCheck.IsChecked == true,
            RespectSystemReducedMotion = RespectReducedMotionCheck.IsChecked == true,
            ShowRuler = ShowRulerCheck.IsChecked == true,
            ShowMarginGuides = ShowMarginGuidesCheck.IsChecked == true,
        };
        return WriterAppearanceCompatibility.Normalize(preferences);
    }

    private void OnValueChanged(object sender, RoutedEventArgs e) => PublishChange();

    private void OnValueChanged(object sender, SelectionChangedEventArgs e) => PublishChange();

    private void OnAccentModeChanged(object sender, RoutedEventArgs e)
    {
        if (!_updating && CustomAccentCheck.IsChecked == true && _accent is null)
            _accent = "#FF2B579A";

        UpdateAccentButton();
        PublishChange();
    }

    private void OnChooseAccent(object sender, RoutedEventArgs e)
    {
        Color initial = ParseColor(_accent) ?? Color.FromRgb(0x2B, 0x57, 0x9A);
        var dialog = new WriterColorDialog(initial, owner: Window.GetWindow(this));
        if (dialog.ShowDialog() != true || dialog.Result is not Color color)
            return;

        _accent = $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
        CustomAccentCheck.IsChecked = true;
        UpdateAccentButton();
        PublishChange();
    }

    private void OnDefaults(object sender, RoutedEventArgs e)
    {
        SetPreferences(new WriterAppearancePreferences());
        PreferencesChanged?.Invoke(this, Preferences);
    }

    private void OnApply(object sender, RoutedEventArgs e) => ApplyRequested?.Invoke(this, EventArgs.Empty);

    private void PublishChange()
    {
        if (_updating || ThemeCombo.SelectedItem is null)
            return;

        WriterAppearancePreferences preferences = BuildPreferences();
        SetPreferences(preferences);
        PreferencesChanged?.Invoke(this, preferences);
    }

    private void RefreshCompatibility(WriterAppearancePreferences preferences)
    {
        foreach (ComboBoxItem item in BackstageCombo.Items)
        {
            item.IsEnabled = item.Tag is RibbonBackstageDesign design
                && WriterAppearanceCompatibility.IsBackstageDesignSupported(preferences.Theme, design);
        }

        foreach (ComboBoxItem item in FrameCombo.Items)
        {
            item.IsEnabled = item.Tag is RibbonWindowFrameAppearance frame
                && WriterAppearanceCompatibility.IsFrameSupported(preferences.Theme, frame);
        }

        foreach (ComboBoxItem item in ApplicationButtonCombo.Items)
        {
            item.IsEnabled = item.Tag is RibbonApplicationButtonShape shape
                && (preferences.Theme == RibbonTheme.Office2007
                    ? shape == RibbonApplicationButtonShape.Orb
                    : shape == RibbonApplicationButtonShape.Tab);
        }

        foreach (ComboBoxItem item in BackdropCombo.Items)
        {
            item.IsEnabled = item.Tag is RibbonBackdrop backdrop
                && (backdrop == RibbonBackdrop.None
                    || MicaHelper.IsSupported
                    && WriterAppearanceCompatibility.IsBackdropCompatible(
                        preferences.FrameAppearance,
                        backdrop));
        }

        bool translucent = WriterAppearanceCompatibility.CanUseBackstageTranslucency(
            preferences,
            MicaHelper.IsSupported);
        BackstageTranslucentCheck.IsEnabled = translucent;
        AccentedTitleBarCheck.IsEnabled =
            preferences.FrameAppearance == RibbonWindowFrameAppearance.Default;
        AccentButton.IsEnabled = CustomAccentCheck.IsChecked == true;

        var notes = new List<string>();
        if (!MicaHelper.IsSupported)
            notes.Add("Mica, Acrylic, and Tabbed backdrops require Windows 11 22H2 or newer; Writer uses None on this system.");
        else if (preferences.FrameAppearance != RibbonWindowFrameAppearance.Default)
            notes.Add("Historical Aero frames support None or Acrylic; incompatible backdrop choices fall back to None.");

        if (preferences.Theme is not RibbonTheme.Office2007 and not RibbonTheme.Office2010)
            notes.Add("Historical Backstage and Aero choices remain visible but are available only with their matching Office generation.");
        notes.Add(preferences.Theme == RibbonTheme.Office2007
            ? "Office 2007 always uses the Orb so its application surface and Classic 2007 Backstage share the correct anchor."
            : "The File tab is used automatically outside Office 2007.");
        if (!translucent)
            notes.Add("Backstage translucency needs an active supported backdrop and is unavailable for Classic 2007.");

        CompatibilityText.Text = string.Join(" ", notes);
    }

    private void UpdateAccentButton()
    {
        AccentButton.IsEnabled = CustomAccentCheck.IsChecked == true;
        string value = _accent ?? "#FF2B579A";
        AccentButton.Content = value;
        if (ParseColor(value) is Color color)
            AccentButton.Background = new SolidColorBrush(color);
    }

    private static Color? ParseColor(string? value)
    {
        try
        {
            return ColorConverter.ConvertFromString(value) is Color color ? color : null;
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private static void Select(ComboBox combo, object value)
    {
        foreach (ComboBoxItem item in combo.Items)
        {
            if (Equals(item.Tag, value) || string.Equals(
                    item.Tag?.ToString(),
                    value.ToString(),
                    StringComparison.OrdinalIgnoreCase))
            {
                combo.SelectedItem = item;
                return;
            }
        }
    }

    private static object? SelectedTag(ComboBox combo) =>
        (combo.SelectedItem as ComboBoxItem)?.Tag;

    private static T Selected<T>(ComboBox combo) where T : struct, Enum =>
        SelectedTag(combo) is T value ? value : default;
}
