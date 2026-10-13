using System.Windows;

namespace RibbonKit.Controls;

internal sealed record SimplifiedCommandChoice(FrameworkElement Source, RibbonControlSize Size, RibbonGroup? Group = null);
internal sealed record SimplifiedCommandSource(FrameworkElement Control, RibbonGroup Group, FrameworkElement Declaration);

public partial class Ribbon
{
    internal static readonly DependencyProperty SimplifiedCommandGroupProperty = DependencyProperty.RegisterAttached(
        "SimplifiedCommandGroup", typeof(RibbonGroup), typeof(Ribbon), new PropertyMetadata(null));
    private readonly Dictionary<RibbonTab, List<SimplifiedCommandChoice>> _simplifiedChoices = new();

    /// <summary>Raised when the user requests the simplified ribbon customization page.</summary>
    public event EventHandler? SimplifiedCustomizeRequested;

    /// <summary>Raised after simplified selections, order or labels change, or the simplified editor applies shared structure; hosts may persist with <see cref="RibbonCustomizationSerializer"/>.</summary>
    public event EventHandler? SimplifiedCustomizationChanged;

    /// <summary>Pins a supported original command (or its copy) in its group at the end of its tab's simplified row. Copies retain their group; original commands prefer the active eligible tab. Returns false for duplicates or unsupported/transient sources.</summary>
    public bool PinToSimplifiedRibbon(FrameworkElement source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var (tab, entry) = FindSimplifiedTarget(source);
        if (tab is null || entry is null) return false;
        source = entry.Control;
        var choices = GetSimplifiedChoices(tab);
        if (choices.Any(choice => choice.Group == entry.Group && SameSimplifiedCommand(choice.Source, source))) return false;
        choices.Add(new SimplifiedCommandChoice(source, GetSimplifiedSize(entry.Declaration), entry.Group));
        SetSimplifiedChoices(tab, choices);
        return true;
    }

    /// <summary>Removes a supported command (or its copy) in its group from the simplified row without changing its original group or QAT membership. Copies in other groups retain their selections.</summary>
    public bool RemoveFromSimplifiedRibbon(FrameworkElement source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var (tab, entry) = FindSimplifiedTarget(source);
        if (tab is null || entry is null) return false;
        source = entry.Control;
        var choices = GetSimplifiedChoices(tab);
        if (choices.RemoveAll(choice => choice.Group == entry.Group && SameSimplifiedCommand(choice.Source, source)) == 0) return false;
        SetSimplifiedChoices(tab, choices);
        return true;
    }

    internal static FrameworkElement OriginalSimplifiedSource(FrameworkElement source)
    {
        var visited = new HashSet<FrameworkElement>();
        while (visited.Add(source) && GetQuickAccessSource(source) is { } original) source = original;
        return source;
    }

    internal static bool SameSimplifiedCommand(FrameworkElement first, FrameworkElement second) =>
        ReferenceEquals(first, second) || (GetCommandId(first) is { Length: > 0 } id && id == GetCommandId(second));

    internal static List<SimplifiedCommandSource> SimplifiedSources(RibbonTab tab, bool customizableOnly = false)
    {
        var commands = new List<SimplifiedCommandSource>();
        foreach (RibbonGroup group in tab.Groups)
        {
            if (customizableOnly && GetIsMerged(group)) continue;
            foreach (FrameworkElement command in RibbonGroupsHost.Commands(group))
            {
                FrameworkElement original = OriginalSimplifiedSource(command);
                if (customizableOnly && (GetIsMerged(original) || GetMergeSource(original) is not null)) continue;
                if (!commands.Any(entry => entry.Group == group && SameSimplifiedCommand(entry.Control, original)))
                    commands.Add(new SimplifiedCommandSource(original, group, command));
            }
        }
        return commands;
    }

    internal RibbonTab? FindSimplifiedTab(FrameworkElement source) => Tabs.OrderByDescending(tab => ReferenceEquals(tab, SelectedTab)).FirstOrDefault(tab =>
        RibbonCustomizePage.IsCustomizableTab(tab) && SimplifiedSources(tab, true).Any(entry => SameSimplifiedCommand(entry.Control, source)));

    internal (RibbonTab? Tab, SimplifiedCommandSource? Entry) FindSimplifiedTarget(FrameworkElement declaration)
    {
        var source = OriginalSimplifiedSource(declaration);
        var group = declaration.GetValue(SimplifiedCommandGroupProperty) as RibbonGroup;
        var eligible = Tabs.Where(RibbonCustomizePage.IsCustomizableTab);
        var tab = group is not null
            ? eligible.FirstOrDefault(tab => SimplifiedSources(tab, true).Any(entry => entry.Group == group && SameSimplifiedCommand(entry.Control, source)))
            : !ReferenceEquals(source, declaration)
                ? eligible.FirstOrDefault(tab => SimplifiedSources(tab, true).Any(entry => ReferenceEquals(entry.Declaration, declaration)))
                : null;
        if (group is null) tab ??= FindSimplifiedTab(source);
        if (tab is null) return (null, null);
        var sources = SimplifiedSources(tab, true);
        var entry = sources.FirstOrDefault(entry => ReferenceEquals(entry.Declaration, declaration))
            ?? sources.FirstOrDefault(entry => group == entry.Group && SameSimplifiedCommand(entry.Control, source))
            ?? sources.FirstOrDefault(entry => SameSimplifiedCommand(entry.Control, source));
        return (tab, entry);
    }

    internal static SimplifiedCommandSource? ResolveSimplifiedChoice(IEnumerable<SimplifiedCommandSource> sources, SimplifiedCommandChoice choice) =>
        sources.FirstOrDefault(entry => (choice.Group is null || ReferenceEquals(entry.Group, choice.Group))
            && SameSimplifiedCommand(entry.Control, OriginalSimplifiedSource(choice.Source)));

    internal List<SimplifiedCommandChoice> GetSimplifiedDefaults(RibbonTab tab) => SimplifiedSources(tab, true)
        .Where(entry => GetSimplifiedPlacement(entry.Declaration) == RibbonSimplifiedPlacement.Inline)
        .Select(entry => new SimplifiedCommandChoice(entry.Control, GetSimplifiedSize(entry.Declaration), entry.Group)).ToList();

    internal List<SimplifiedCommandChoice> GetSimplifiedChoices(RibbonTab tab)
    {
        var sources = SimplifiedSources(tab, true);
        return _simplifiedChoices.TryGetValue(tab, out var choices)
            ? choices.Where(choice => ResolveSimplifiedChoice(sources, choice) is not null).ToList()
            : GetSimplifiedDefaults(tab);
    }

    internal List<SimplifiedCommandChoice> GetSimplifiedRow(RibbonTab tab)
    {
        if (!RibbonCustomizePage.IsCustomizableTab(tab))
            return SimplifiedSources(tab).Where(entry => GetSimplifiedPlacement(entry.Declaration) == RibbonSimplifiedPlacement.Inline)
                .Select(entry => new SimplifiedCommandChoice(entry.Control, GetSimplifiedSize(entry.Declaration), entry.Group)).ToList();
        var choices = GetSimplifiedChoices(tab);
        // Transient merge contributions keep their app-authored policy and are never saved.
        foreach (var entry in SimplifiedSources(tab).Where(entry => GetIsMerged(entry.Group) || GetMergeSource(entry.Control) is not null))
            if (GetSimplifiedPlacement(entry.Declaration) == RibbonSimplifiedPlacement.Inline && !choices.Any(choice => choice.Group == entry.Group && SameSimplifiedCommand(choice.Source, entry.Control)))
                choices.Add(new SimplifiedCommandChoice(entry.Control, GetSimplifiedSize(entry.Declaration), entry.Group));
        return choices;
    }

    internal void SetSimplifiedChoices(RibbonTab tab, IEnumerable<SimplifiedCommandChoice>? choices, bool notify = true)
    {
        if (!Tabs.Contains(tab) || !RibbonCustomizePage.IsCustomizableTab(tab)) return;
        if (choices is null) _simplifiedChoices.Remove(tab);
        else
        {
            var sources = SimplifiedSources(tab, true);
            var selected = new List<SimplifiedCommandChoice>();
            foreach (var choice in choices)
            {
                var entry = ResolveSimplifiedChoice(sources, choice);
                if (entry is not null && !selected.Any(item => item.Group == entry.Group && SameSimplifiedCommand(item.Source, entry.Control))
                    && choice.Size is RibbonControlSize.Small or RibbonControlSize.Medium)
                    selected.Add(new SimplifiedCommandChoice(entry.Control, choice.Size, entry.Group));
            }
            _simplifiedChoices[tab] = selected;
        }
        if (tab.Content is RibbonGroupsHost host) host.RequestSimplifiedRefresh();
        if (notify) SimplifiedCustomizationChanged?.Invoke(this, EventArgs.Empty);
    }

    internal IEnumerable<KeyValuePair<RibbonTab, List<SimplifiedCommandChoice>>> SimplifiedOverrides =>
        _simplifiedChoices.Where(pair => Tabs.Contains(pair.Key) && RibbonCustomizePage.IsCustomizableTab(pair.Key));

    internal void ResetSimplifiedOverrides()
    {
        foreach (RibbonTab tab in _simplifiedChoices.Keys.ToArray()) SetSimplifiedChoices(tab, null, false);
        _simplifiedChoices.Clear();
    }

    internal void NotifySimplifiedCustomizationChanged() => SimplifiedCustomizationChanged?.Invoke(this, EventArgs.Empty);
}
