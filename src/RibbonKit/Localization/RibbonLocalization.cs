using System.ComponentModel;
using System.Globalization;
using System.Resources;
using System.Windows.Data;
using System.Windows.Markup;

namespace RibbonKit.Localization;

/// <summary>Identifies a user-facing string supplied by RibbonKit itself.</summary>
/// <remarks>
/// Application content such as tab, group and command headers remains owned and localized by the
/// host application. These identifiers cover only chrome and commands created by RibbonKit.
/// </remarks>
public enum RibbonString
{
    /// <summary>"Add to Quick Access Toolbar".</summary>
    AddToQuickAccessToolbar,

    /// <summary>"Customize Quick Access Toolbar…".</summary>
    CustomizeQuickAccessToolbar,

    /// <summary>"Customize the Ribbon…".</summary>
    CustomizeRibbon,

    /// <summary>"Collapse the Ribbon".</summary>
    CollapseRibbon,

    /// <summary>"Show Quick Access Toolbar in the Title Bar".</summary>
    ShowQuickAccessToolbarInTitleBar,

    /// <summary>"Show Quick Access Toolbar Above the Ribbon".</summary>
    ShowQuickAccessToolbarAboveRibbon,

    /// <summary>"Show Quick Access Toolbar Below the Ribbon".</summary>
    ShowQuickAccessToolbarBelowRibbon,

    /// <summary>"Remove from Quick Access Toolbar".</summary>
    RemoveFromQuickAccessToolbar,

    /// <summary>"Choose commands from the ribbon:".</summary>
    ChooseCommandsFromRibbon,

    /// <summary>"Current Quick Access Toolbar:".</summary>
    CurrentQuickAccessToolbar,

    /// <summary>"Customize the ribbon:".</summary>
    CustomizeRibbonLabel,

    /// <summary>Title of the built-in Customize Ribbon options page.</summary>
    CustomizeRibbonPage,

    /// <summary>Title of the built-in Quick Access Toolbar options page.</summary>
    QuickAccessToolbarPage,

    /// <summary>"Add".</summary>
    Add,

    /// <summary>"Remove".</summary>
    Remove,

    /// <summary>"Move Up".</summary>
    MoveUp,

    /// <summary>"Move Down".</summary>
    MoveDown,

    /// <summary>"Reset".</summary>
    Reset,

    /// <summary>"Import".</summary>
    Import,

    /// <summary>"Export".</summary>
    Export,

    /// <summary>Tooltip describing the import customization action.</summary>
    ImportRibbonLayoutTooltip,

    /// <summary>Tooltip describing the export customization action.</summary>
    ExportRibbonLayoutTooltip,

    /// <summary>"New Tab".</summary>
    NewTab,

    /// <summary>"New Group".</summary>
    NewGroup,

    /// <summary>"Edit…".</summary>
    Edit,

    /// <summary>"Close".</summary>
    Close,

    /// <summary>"Name:".</summary>
    NameLabel,

    /// <summary>Label for the customization icon picker.</summary>
    IconFromRibbonCommandsLabel,

    /// <summary>"None".</summary>
    None,

    /// <summary>"Group layout:".</summary>
    GroupLayoutLabel,

    /// <summary>"Button size:".</summary>
    ButtonSizeLabel,

    /// <summary>"Stacked".</summary>
    Stacked,

    /// <summary>"Large".</summary>
    Large,

    /// <summary>"Medium".</summary>
    Medium,

    /// <summary>"Small".</summary>
    Small,

    /// <summary>"OK".</summary>
    Ok,

    /// <summary>"Cancel".</summary>
    Cancel,

    /// <summary>Format string used to mark a customization entry as custom.</summary>
    CustomItemFormat,

    /// <summary>"Edit Tab".</summary>
    EditTab,

    /// <summary>"Edit Group".</summary>
    EditGroup,

    /// <summary>"Edit Command".</summary>
    EditCommand,

    /// <summary>"Export Ribbon Customization".</summary>
    ExportRibbonCustomization,

    /// <summary>"Import Ribbon Customization".</summary>
    ImportRibbonCustomization,

    /// <summary>File-dialog filter for RibbonKit customization JSON files.</summary>
    RibbonLayoutFileFilter,

    /// <summary>"Ribbon Customization".</summary>
    RibbonCustomization,

    /// <summary>Error shown when an exported customization file cannot be saved.</summary>
    CouldNotSaveFile,

    /// <summary>Error shown when an imported customization file cannot be read.</summary>
    CouldNotReadFile,

    /// <summary>Tooltip for navigating back from Backstage.</summary>
    Back,

    /// <summary>Tooltip for a standard minimize caption button.</summary>
    Minimize,

    /// <summary>Tooltip for a standard maximize caption button.</summary>
    Maximize,

    /// <summary>Tooltip for a standard restore-down caption button.</summary>
    RestoreDown,

    /// <summary>Tooltip and KeyTip description for Quick Access Toolbar overflow.</summary>
    MoreQuickAccessCommands,

    /// <summary>Tooltip for the ribbon minimize toggle.</summary>
    MinimizeRibbon,

    /// <summary>Tooltip for a merged child-window minimize button.</summary>
    MinimizeWindow,

    /// <summary>Tooltip for a merged child-window restore button.</summary>
    RestoreWindow,

    /// <summary>Tooltip for a merged child-window close button.</summary>
    CloseWindow,

    /// <summary>Tooltip for a ribbon group dialog launcher.</summary>
    MoreOptions,

    /// <summary>Default label for the ribbon application button.</summary>
    File,

    /// <summary>Label for a conventional application-menu options action.</summary>
    Options,

    /// <summary>Label for a conventional application-menu exit action.</summary>
    Exit,
}

/// <summary>
/// Supplies application-specific overrides for RibbonKit's built-in localized strings.
/// </summary>
/// <remarks>
/// Return <see langword="null"/> for a string the provider does not override; RibbonKit then falls
/// back to its embedded <c>.resx</c> resource for the requested culture.
/// </remarks>
public interface IRibbonLocalizationProvider
{
    /// <summary>Returns an override for <paramref name="key"/>, or <see langword="null"/>.</summary>
    string? GetString(RibbonString key, CultureInfo culture);
}

/// <summary>
/// Resolves RibbonKit's built-in strings from embedded localization resources, with an optional
/// application-provided override layer.
/// </summary>
public static class RibbonLocalization
{
    private static readonly ResourceManager ResourceManager =
        new("RibbonKit.Resources.Strings", typeof(RibbonLocalization).Assembly);

    private static IRibbonLocalizationProvider? _provider;

    internal static event EventHandler? LocalizationChanged;

    /// <summary>
    /// Gets or sets the application override provider. A provider may override only selected
    /// strings by returning <see langword="null"/> for all others. Set to <see langword="null"/>
    /// to use RibbonKit's embedded resources exclusively. Replacing the provider refreshes live
    /// <see cref="RibbonStringExtension"/> bindings.
    /// </summary>
    public static IRibbonLocalizationProvider? Provider
    {
        get => Volatile.Read(ref _provider);
        set
        {
            IRibbonLocalizationProvider? previous = Interlocked.Exchange(ref _provider, value);
            if (!ReferenceEquals(previous, value))
            {
                Refresh();
            }
        }
    }

    /// <summary>
    /// Refreshes localized bindings after the application changes
    /// <see cref="CultureInfo.CurrentUICulture"/> without replacing <see cref="Provider"/>.
    /// Call this on the UI thread after changing cultures.
    /// </summary>
    public static void Refresh() => LocalizationChanged?.Invoke(null, EventArgs.Empty);

    /// <summary>
    /// Resolves <paramref name="key"/> for <see cref="CultureInfo.CurrentUICulture"/>.
    /// </summary>
    public static string GetString(RibbonString key)
    {
        CultureInfo culture = CultureInfo.CurrentUICulture;
        string? overridden = Provider?.GetString(key, culture);
        if (overridden is not null)
        {
            return overridden;
        }

        return ResourceManager.GetString(key.ToString(), culture) ?? key.ToString();
    }
}

/// <summary>
/// Resolves a <see cref="RibbonString"/> in XAML and keeps the target current when the localization
/// provider or UI culture is refreshed.
/// </summary>
[MarkupExtensionReturnType(typeof(string))]
public sealed class RibbonStringExtension : MarkupExtension
{
    /// <summary>Initializes an empty extension. Set <see cref="Key"/> in XAML.</summary>
    public RibbonStringExtension()
    {
    }

    /// <summary>Initializes an extension for <paramref name="key"/>.</summary>
    public RibbonStringExtension(RibbonString key) => Key = key;

    /// <summary>Gets or sets the built-in string to resolve.</summary>
    [ConstructorArgument("key")]
    public RibbonString Key { get; set; }

    /// <inheritdoc />
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var binding = new Binding($"[{Key}]")
        {
            Source = RibbonLocalizationBindingSource.Instance,
            Mode = BindingMode.OneWay,
        };
        return binding.ProvideValue(serviceProvider);
    }
}

internal sealed class RibbonLocalizationBindingSource : INotifyPropertyChanged
{
    internal static readonly RibbonLocalizationBindingSource Instance = new();

    private RibbonLocalizationBindingSource() =>
        RibbonLocalization.LocalizationChanged += OnLocalizationChanged;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string this[string key] =>
        Enum.TryParse(key, out RibbonString parsed)
            ? RibbonLocalization.GetString(parsed)
            : key;

    private void OnLocalizationChanged(object? sender, EventArgs e) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
}
