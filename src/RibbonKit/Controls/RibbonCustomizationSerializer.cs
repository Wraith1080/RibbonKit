using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Media;

namespace RibbonKit.Controls;

/// <summary>
/// Saves and restores a ribbon's user customization (tab order/visibility/renames, custom
/// tabs, custom groups anywhere, custom-group commands, group layout/icon, item sizes, and
/// the quick access toolbar) as JSON — so customizations survive restarts, and so Reset /
/// Import / Export are just Apply of a different string.
/// </summary>
/// <remarks>
/// <para>
/// <b>Identity.</b> Everything is keyed by <see cref="Ribbon.CommandIdProperty"/> — a stable
/// string the app assigns to the built-in tabs, groups, and commands it wants persistable
/// (proxies reference their source command by this id, so a saved "custom group contains Bold"
/// survives even though the proxy object does not). Custom tabs/groups created by
/// <see cref="RibbonCustomizePage"/> auto-get a generated id. Elements without an id are left
/// alone: not serialized, never moved/removed by <see cref="Apply"/>.
/// </para>
/// <para>
/// <b>Typical flow.</b> At startup, after the ribbon is built: capture a baseline with
/// <see cref="Serialize"/> (for Reset), then <see cref="Apply"/> the saved JSON if present.
/// On change/close, <see cref="Serialize"/> and store. <see cref="Apply"/> reconciles from ANY
/// state, so passing the baseline resets, and passing an imported string imports.
/// </para>
/// </remarks>
public static class RibbonCustomizationSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>Captures the ribbon's current customizable structure + QAT as a JSON string.</summary>
    public static string Serialize(Ribbon ribbon)
    {
        ArgumentNullException.ThrowIfNull(ribbon);
        return JsonSerializer.Serialize(BuildLayout(ribbon), Options);
    }

    /// <summary>
    /// Reconciles <paramref name="ribbon"/> to match a JSON layout produced by
    /// <see cref="Serialize"/>: reorders/hides/renames built-in tabs and groups, rebuilds custom
    /// tabs/groups and their proxy commands, and rebuilds the QAT. Robust from any starting state
    /// (so it doubles as Reset when given a baseline). Missing ids are skipped; unknown
    /// (non-persisted) elements are preserved at the end of their parent. Silently ignores an
    /// empty/invalid string.
    /// </summary>
    public static void Apply(Ribbon ribbon, string? json)
    {
        ArgumentNullException.ThrowIfNull(ribbon);
        if (string.IsNullOrWhiteSpace(json))
        {
            return;
        }

        RibbonLayoutDto? layout;
        try
        {
            layout = JsonSerializer.Deserialize<RibbonLayoutDto>(json, Options);
        }
        catch (JsonException)
        {
            return; // Corrupt/foreign string — leave the ribbon as-is.
        }

        if (layout is not null)
        {
            ApplyLayout(ribbon, layout);
        }
    }

    // ---- Serialize ---------------------------------------------------------------

    private static RibbonLayoutDto BuildLayout(Ribbon ribbon)
    {
        BuildIdentity(ribbon, out _, out Dictionary<FrameworkElement, string> idOf, out Dictionary<string, ImageSource> iconById);
        var layout = new RibbonLayoutDto();

        foreach (RibbonTab tab in ribbon.Tabs)
        {
            if (tab.IsContextual)
            {
                continue; // App-driven visibility; never persisted.
            }

            string? tabId = Ribbon.GetCommandId(tab);
            bool tabCustom = Ribbon.GetIsCustom(tab);
            if (!tabCustom && tabId is null)
            {
                continue; // Unidentifiable built-in tab — can't be re-found on load.
            }

            var tabDto = new TabDto
            {
                Id = tabId,
                IsCustom = tabCustom,
                Header = tab.Header?.ToString(),
                Visible = tab.Visibility == Visibility.Visible,
            };

            foreach (RibbonGroup group in tab.Groups)
            {
                string? groupId = Ribbon.GetCommandId(group);
                bool groupCustom = Ribbon.GetIsCustom(group);
                if (!groupCustom && groupId is null)
                {
                    continue;
                }

                var groupDto = new GroupDto
                {
                    Id = groupId,
                    IsCustom = groupCustom,
                    Header = group.Header?.ToString(),
                    Layout = group.Layout,
                };

                if (groupCustom)
                {
                    groupDto.IconCommandId = FindIconId(group.Icon, iconById);
                    groupDto.Commands = new List<CommandDto>();
                    foreach (FrameworkElement proxy in group.Items.OfType<FrameworkElement>())
                    {
                        FrameworkElement source = Ribbon.GetQuickAccessSource(proxy) ?? proxy;

                        // Prefer the app-assigned CommandId; fall back to the auto-derived
                        // path id so a proxy of an UNTAGGED built-in still round-trips.
                        string? sourceId = idOf.TryGetValue(source, out string? id) ? id : Ribbon.GetCommandId(source);
                        if (sourceId is null)
                        {
                            continue; // Source can't be re-found on load — skip this proxy.
                        }

                        groupDto.Commands.Add(new CommandDto
                        {
                            SourceId = sourceId,
                            Header = HeaderOf(proxy),
                            Size = SizeOf(proxy),
                        });
                    }
                }

                tabDto.Groups.Add(groupDto);
            }

            layout.Tabs.Add(tabDto);
        }

        foreach (object item in ribbon.QuickAccessItems)
        {
            if (item is not FrameworkElement element)
            {
                continue;
            }

            // A proxy persists its SOURCE's id (explicit or auto-derived); a hand-declared item
            // persists its OWN id.
            FrameworkElement? proxySource = Ribbon.GetQuickAccessSource(element);
            string? refId = proxySource is not null
                ? (idOf.TryGetValue(proxySource, out string? sid) ? sid : Ribbon.GetCommandId(proxySource))
                : Ribbon.GetCommandId(element);
            if (refId is not null)
            {
                layout.QuickAccess.Add(new QatDto { Ref = refId });
            }
        }

        return layout;
    }

    // ---- Apply -------------------------------------------------------------------

    private static void ApplyLayout(Ribbon ribbon, RibbonLayoutDto layout)
    {
        // 1. Catalog identity → live element, from the CURRENT ribbon (custom-group proxies are
        //    excluded by the catalog, so `sources` holds only real command controls + QAT items).
        //    `sources` is keyed by BOTH the app's explicit CommandId AND the auto-derived path id,
        //    so a saved proxy resolves whichever id form it was serialized with.
        BuildIdentity(ribbon, out Dictionary<string, FrameworkElement> sources, out _, out Dictionary<string, ImageSource> iconById);

        var builtInTabs = new Dictionary<string, RibbonTab>();
        foreach (RibbonTab tab in ribbon.Tabs)
        {
            if (!Ribbon.GetIsCustom(tab) && Ribbon.GetCommandId(tab) is { } tabId)
            {
                builtInTabs.TryAdd(tabId, tab);
            }
        }

        // Hand-declared QAT items are also re-addable sources (keyed by their own id).
        var declaredQat = new Dictionary<string, FrameworkElement>();
        foreach (object item in ribbon.QuickAccessItems)
        {
            if (item is FrameworkElement fe && Ribbon.GetQuickAccessSource(fe) is null
                && Ribbon.GetCommandId(fe) is { } id)
            {
                declaredQat.TryAdd(id, fe);
                sources.TryAdd(id, fe);
            }
        }

        // 2. Strip existing customizations back to the built-in skeleton.
        foreach (RibbonTab custom in ribbon.Tabs.Where(Ribbon.GetIsCustom).ToList())
        {
            ribbon.Tabs.Remove(custom);
        }

        foreach (RibbonTab tab in ribbon.Tabs)
        {
            foreach (RibbonGroup customGroup in tab.Groups.Where(Ribbon.GetIsCustom).ToList())
            {
                tab.Groups.Remove(customGroup);
            }
        }

        // 3. Build the desired tab list (creating custom tabs, configuring built-in ones).
        var desiredTabs = new List<RibbonTab>();
        foreach (TabDto tabDto in layout.Tabs)
        {
            RibbonTab? tab;
            if (tabDto.IsCustom)
            {
                tab = new RibbonTab { Header = tabDto.Header ?? "New Tab" };
                Ribbon.SetIsCustom(tab, true);
                if (tabDto.Id is not null)
                {
                    Ribbon.SetCommandId(tab, tabDto.Id);
                }
            }
            else if (tabDto.Id is not null && builtInTabs.TryGetValue(tabDto.Id, out tab))
            {
                if (tabDto.Header is not null)
                {
                    tab.Header = tabDto.Header;
                }
            }
            else
            {
                continue; // Built-in tab no longer exists.
            }

            tab.Visibility = tabDto.Visible ? Visibility.Visible : Visibility.Collapsed;
            ReconcileGroups(ribbon, tab, tabDto, sources, iconById);
            desiredTabs.Add(tab);
        }

        // 4. Reorder tabs to match, preserving any current tab the layout didn't mention
        //    (a newly-shipped built-in tab, or a contextual tab) at the end.
        var extraTabs = ribbon.Tabs.Where(t => !desiredTabs.Contains(t)).ToList();
        ribbon.Tabs.Clear();
        foreach (RibbonTab tab in desiredTabs)
        {
            ribbon.Tabs.Add(tab);
        }

        foreach (RibbonTab tab in extraTabs)
        {
            ribbon.Tabs.Add(tab);
        }

        // 5. Rebuild the QAT in the saved order.
        ribbon.QuickAccessItems.Clear();
        foreach (QatDto qat in layout.QuickAccess)
        {
            if (qat.Ref is null)
            {
                continue;
            }

            if (declaredQat.TryGetValue(qat.Ref, out FrameworkElement? declared))
            {
                ribbon.QuickAccessItems.Add(declared);
            }
            else if (sources.TryGetValue(qat.Ref, out FrameworkElement? source))
            {
                ribbon.QuickAccessItems.Add(ribbon.CreateCommandProxy(source, RibbonControlSize.Small));
            }
        }
    }

    private static void ReconcileGroups(
        Ribbon ribbon,
        RibbonTab tab,
        TabDto tabDto,
        Dictionary<string, FrameworkElement> sources,
        Dictionary<string, ImageSource> iconById)
    {
        var builtInGroups = new Dictionary<string, RibbonGroup>();
        foreach (RibbonGroup group in tab.Groups)
        {
            if (!Ribbon.GetIsCustom(group) && Ribbon.GetCommandId(group) is { } gid)
            {
                builtInGroups.TryAdd(gid, group);
            }
        }

        var desiredGroups = new List<RibbonGroup>();
        foreach (GroupDto groupDto in tabDto.Groups)
        {
            RibbonGroup? group;
            if (groupDto.IsCustom)
            {
                group = new RibbonGroup { Header = groupDto.Header ?? "New Group" };
                Ribbon.SetIsCustom(group, true);
                if (groupDto.Id is not null)
                {
                    Ribbon.SetCommandId(group, groupDto.Id);
                }

                group.Layout = groupDto.Layout == RibbonGroupLayout.Default
                    ? RibbonGroupLayout.Stacked
                    : groupDto.Layout;

                if (groupDto.IconCommandId is not null && iconById.TryGetValue(groupDto.IconCommandId, out ImageSource? icon))
                {
                    group.Icon = icon;
                }

                foreach (CommandDto commandDto in groupDto.Commands ?? new List<CommandDto>())
                {
                    if (commandDto.SourceId is null || !sources.TryGetValue(commandDto.SourceId, out FrameworkElement? source))
                    {
                        continue;
                    }

                    FrameworkElement proxy = ribbon.CreateCommandProxy(source, commandDto.Size);
                    if (commandDto.Header is not null)
                    {
                        SetHeader(proxy, commandDto.Header);
                    }

                    group.Items.Add(proxy);
                }
            }
            else if (groupDto.Id is not null && builtInGroups.TryGetValue(groupDto.Id, out group))
            {
                if (groupDto.Header is not null)
                {
                    group.Header = groupDto.Header;
                }
            }
            else
            {
                continue;
            }

            desiredGroups.Add(group);
        }

        var extraGroups = tab.Groups.Where(g => !desiredGroups.Contains(g)).ToList();
        tab.Groups.Clear();
        foreach (RibbonGroup group in desiredGroups)
        {
            tab.Groups.Add(group);
        }

        foreach (RibbonGroup group in extraGroups)
        {
            tab.Groups.Add(group);
        }
    }

    // ---- Helpers -----------------------------------------------------------------

    /// <summary>
    /// Walks the ribbon once and builds the identity maps every command persists through:
    /// <list type="bullet">
    /// <item><paramref name="byId"/>: id → live control, registered under BOTH the app's explicit
    /// <see cref="Ribbon.CommandIdProperty"/> (when set) AND an auto-derived path id
    /// (<c>auto:tab/group/caption#index</c>) — so a saved proxy resolves whichever form it used;</item>
    /// <item><paramref name="idOf"/>: control → the id to serialize (explicit preferred, else auto);</item>
    /// <item><paramref name="iconById"/>: id → icon, so a custom group's icon (harvested from a
    /// command's icon) round-trips by that command's id.</item>
    /// </list>
    /// The auto id makes tagging OPTIONAL: an app need not assign a CommandId to every command for
    /// it to be addable to a custom group and survive a restart. Explicit ids are still preferred
    /// (they're stable across built-in reorders/renames); the auto id is the fallback. Proxies are
    /// excluded by the catalog, so only real controls get ids, and the per-group index is stable
    /// because custom groups (all-proxy) contribute no controls.
    /// </summary>
    private static void BuildIdentity(
        Ribbon ribbon,
        out Dictionary<string, FrameworkElement> byId,
        out Dictionary<FrameworkElement, string> idOf,
        out Dictionary<string, ImageSource> iconById)
    {
        byId = new Dictionary<string, FrameworkElement>();
        idOf = new Dictionary<FrameworkElement, string>();
        iconById = new Dictionary<string, ImageSource>();

        foreach (RibbonTab tab in ribbon.Tabs)
        {
            string tabKey = Ribbon.GetCommandId(tab) ?? tab.Header?.ToString() ?? "tab";
            foreach (RibbonGroup group in tab.Groups)
            {
                string groupKey = Ribbon.GetCommandId(group) ?? group.Header?.ToString() ?? "group";
                if (Ribbon.GetCommandId(group) is { } gid && group.Icon is { } gi)
                {
                    iconById.TryAdd(gid, gi);
                }

                int index = 0;
                foreach (FrameworkElement control in RibbonCommandCatalog.CollectControls(group))
                {
                    string? explicitId = Ribbon.GetCommandId(control);
                    RibbonCommandEntry described = RibbonCommandCatalog.Describe(control);
                    string autoId = $"auto:{tabKey}/{groupKey}/{described.DisplayName}#{index}";
                    index++;

                    idOf.TryAdd(control, explicitId ?? autoId);
                    if (explicitId is not null)
                    {
                        byId.TryAdd(explicitId, control);
                    }

                    byId.TryAdd(autoId, control);

                    if (described.Icon is { } ci)
                    {
                        if (explicitId is not null)
                        {
                            iconById.TryAdd(explicitId, ci);
                        }

                        iconById.TryAdd(autoId, ci);
                    }
                }
            }
        }
    }

    private static string? FindIconId(ImageSource? icon, Dictionary<string, ImageSource> iconById)
    {
        if (icon is null)
        {
            return null;
        }

        foreach (KeyValuePair<string, ImageSource> pair in iconById)
        {
            if (ReferenceEquals(pair.Value, icon))
            {
                return pair.Key;
            }
        }

        return null;
    }

    private static string? HeaderOf(FrameworkElement element) => element switch
    {
        RibbonButton b => b.Header,
        RibbonToggleButton t => t.Header,
        RibbonDropDownButton d => d.Header,
        _ => null,
    };

    private static RibbonControlSize SizeOf(FrameworkElement element) => element switch
    {
        RibbonButton b => b.Size,
        RibbonToggleButton t => t.Size,
        RibbonDropDownButton d => d.Size,
        _ => RibbonControlSize.Medium,
    };

    private static void SetHeader(FrameworkElement element, string header)
    {
        switch (element)
        {
            case RibbonButton b:
                b.Header = header;
                break;
            case RibbonToggleButton t:
                t.Header = header;
                break;
            case RibbonDropDownButton d:
                d.Header = header;
                break;
        }
    }

    // ---- DTOs --------------------------------------------------------------------

    internal sealed class RibbonLayoutDto
    {
        public List<TabDto> Tabs { get; set; } = new();

        public List<QatDto> QuickAccess { get; set; } = new();
    }

    internal sealed class TabDto
    {
        public string? Id { get; set; }

        public bool IsCustom { get; set; }

        public string? Header { get; set; }

        public bool Visible { get; set; } = true;

        public List<GroupDto> Groups { get; set; } = new();
    }

    internal sealed class GroupDto
    {
        public string? Id { get; set; }

        public bool IsCustom { get; set; }

        public string? Header { get; set; }

        public string? IconCommandId { get; set; }

        public RibbonGroupLayout Layout { get; set; }

        public List<CommandDto>? Commands { get; set; }
    }

    internal sealed class CommandDto
    {
        public string? SourceId { get; set; }

        public string? Header { get; set; }

        public RibbonControlSize Size { get; set; }
    }

    internal sealed class QatDto
    {
        public string? Ref { get; set; }
    }
}
