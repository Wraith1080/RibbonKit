using System.ComponentModel;

namespace RibbonKit.WinForms;

/// <summary>Selects the available-command list. Groups belong only to QuickAccess.</summary>
public enum RibbonCustomizationTarget
{
    /// <summary>Commands that may be offered to a future structural Ribbon editor.</summary>
    Ribbon,
    /// <summary>QAT-compatible commands and whole groups.</summary>
    QuickAccess
}

/// <summary>A catalog description borrowing an existing source and its state.</summary>
public sealed class RibbonCommandCatalogEntry
{
    internal RibbonCommandCatalogEntry(RibbonElement source, string category) { Source = source; CommandId = source.Id; Category = category; }
    /// <summary>Gets the source ID captured for this description.</summary>
    public string CommandId { get; }
    /// <summary>Gets the borrowed source model.</summary>
    public RibbonElement Source { get; }
    /// <summary>Gets its authored tab/group or application-command category.</summary>
    public string Category { get; }
    /// <summary>Gets the current source label without replacing its binding.</summary>
    public string Text => Source.Text;
    /// <summary>Gets whether this entry is a whole group.</summary>
    public bool IsGroup => Source is RibbonGroup;
    /// <summary>Gets whether the source can use an existing QAT projection.</summary>
    public bool CanAddToQuickAccess => RibbonCommandCatalog.SupportsQuickAccess(Source);
    /// <summary>Gets whether the source currently has an explicitly assigned, unchanged persistence identity.</summary>
    public bool HasStableIdentity => Source.HasExplicitId && Source.Id == CommandId;
}

/// <summary>Shared command discovery, retained application commands and customization checkpoints for one ribbon.</summary>
public sealed class RibbonCommandCatalog : RibbonElement
{
    private readonly RibbonControl _host;
    private readonly Dictionary<string, RibbonElement> _presentations = new(StringComparer.Ordinal);
    private readonly Dictionary<RibbonElement, string> _keys = [];
    private RibbonCustomizationState? _defaults;
    internal bool Applying;
    internal RibbonCommandCatalog(RibbonControl host) { _host = host; Commands = new(this); }
    /// <summary>Gets owned application commands with no required ribbon slot. QAT presentations borrow these sources.</summary>
    [Category("Data"), DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    public RibbonItemCollection Commands { get; }
    internal override IEnumerable<RibbonElement> Children => Commands;
    /// <summary>Gets fresh descriptions of authored sources. Group entries appear only for QuickAccess; transient and popup-only sources are not discovered.</summary>
    public IReadOnlyList<RibbonCommandCatalogEntry> GetAvailable(RibbonCustomizationTarget target)
    {
        Check(); if (!Enum.IsDefined(target)) throw new ArgumentOutOfRangeException(nameof(target));
        var entries = new List<RibbonCommandCatalogEntry>(); var seen = new HashSet<RibbonElement>();
        void Add(RibbonElement source, string category)
        {
            if (!Ordinary(source) || !seen.Add(source) || source is not (RibbonItem or RibbonGroup) || source is RibbonMenuSeparatorItem or RibbonGalleryTile) return;
            if (source is RibbonGroup && target != RibbonCustomizationTarget.QuickAccess || target == RibbonCustomizationTarget.QuickAccess && !SupportsQuickAccess(source)) return;
            entries.Add(new(source, category));
        }
        foreach (var tab in _host.Tabs.Where(t => !t.IsDisposed && !t.IsModal))
        foreach (var group in tab.Groups.Where(g => !g.IsDisposed))
        {
            Add(group, tab.Text);
            foreach (var item in group.Items) Add(item, tab.Text + " › " + group.Text);
        }
        foreach (var command in Commands) Add(command, Text);
        foreach (var entry in _host.QuickAccessItems)
            if (RibbonQuickAccess.Source(entry) is { } source) Add(source, "Quick Access");
        if (entries.GroupBy(e => e.CommandId, StringComparer.Ordinal).Any(g => g.Count() > 1)) throw new InvalidOperationException("Catalog source IDs must be unique.");
        return entries.AsReadOnly();
    }
    /// <summary>Creates an unattached QAT reference using the shared button/dropdown/group/ComboBox projection contracts.</summary>
    public RibbonElement CreateQuickAccessEntry(string commandId)
    {
        var entry = Sources().GetValueOrDefault(commandId) ?? throw new InvalidOperationException("Unknown QAT command: " + commandId);
        return Create(entry.Source);
    }
    /// <summary>Gets a live permanent QAT presentation's draft identity, including duplicate source presentations. Transient/unmanaged entries cannot be customized.</summary>
    public string GetQuickAccessEntryId(RibbonElement presentation)
    {
        Check(); ArgumentNullException.ThrowIfNull(presentation); Capture("lookup");
        if (!_host.QuickAccessItems.Contains(presentation) || !Managed(presentation, Sources()) || !_keys.TryGetValue(presentation, out var id))
            throw new InvalidOperationException("This is not a permanent QAT presentation of this catalog.");
        return id;
    }
    /// <summary>Captures the current permanent QAT as Reset defaults. Call after authoring and before importing saved preferences.</summary>
    public void CaptureDefaults() { Check(); _defaults = Capture("defaults"); }
    /// <summary>Creates an isolated QAT draft and an Apply checkpoint. Its JSON is scoped to the supplied application identity.</summary>
    public RibbonCustomizationSession BeginCustomization(string applicationId)
    {
        Check(); ArgumentException.ThrowIfNullOrWhiteSpace(applicationId);
        _defaults ??= Capture("defaults");
        var defaults = RibbonCustomizationSerializer.Copy(_defaults); defaults.ApplicationId = applicationId;
        return new(this, applicationId, defaults);
    }
    internal RibbonControl Host => _host;
    internal void Check() { _host.VerifyAccess(); ObjectDisposedException.ThrowIf(IsDisposed || _host.IsDisposed, this); }
    private bool Ordinary(RibbonElement source)
    {
        if (source.IsDisposed || !ReferenceEquals(source.Ribbon, _host) || RibbonControl.GetMergeSource(source) is not null) return false;
        for (RibbonElement? node = source; node is not null; node = node.Parent as RibbonElement)
            if (node is RibbonMessage or RibbonMessageBar or RibbonBackstage || node is RibbonTab { IsModal: true } || ReferenceEquals(node, _host.FileMenu)) return false;
        return true;
    }
    internal static bool SupportsQuickAccess(RibbonElement source) => source is RibbonGroup or RibbonComboBoxItem or RibbonDropDownItem or RibbonButtonItem;
    internal Dictionary<string, RibbonCommandCatalogEntry> Sources() => GetAvailable(RibbonCustomizationTarget.QuickAccess).ToDictionary(e => e.CommandId, StringComparer.Ordinal);
    internal bool Managed(RibbonElement presentation, Dictionary<string, RibbonCommandCatalogEntry> sources) =>
        RibbonControl.GetMergeSource(presentation) is null && RibbonQuickAccess.Source(presentation) is { } source && sources.TryGetValue(source.Id, out var entry) && ReferenceEquals(entry.Source, source);
    internal RibbonCustomizationState Capture(string applicationId)
    {
        Check(); var sources = Sources(); var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        var state = new RibbonCustomizationState { ApplicationId = applicationId, QuickAccessPosition = _host.QuickAccessPosition, QuickAccessVisible = _host.QuickAccessVisible };
        foreach (var presentation in _host.QuickAccessItems.Where(p => Managed(p, sources)))
        {
            var source = RibbonQuickAccess.Source(presentation)!; int count = counts.GetValueOrDefault(source.Id); counts[source.Id] = count + 1;
            if (!_keys.TryGetValue(presentation, out var key))
            {
                string candidate = presentation.HasExplicitId && !ReferenceEquals(presentation, source) ? presentation.Id : $"qat:{source.Id}:{count}";
                key = candidate;
                for (int suffix = 1; _presentations.TryGetValue(key, out var prior) && !ReferenceEquals(prior, presentation); suffix++) key = candidate + ":" + suffix;
                _keys[presentation] = key; _presentations[key] = presentation;
            }
            state.QuickAccess.Add(new() { Id = key, CommandId = source.Id });
        }
        return state;
    }
    internal List<RibbonElement> Prepare(RibbonCustomizationState state, bool persistent)
    {
        Check(); RibbonCustomizationSerializer.Validate(state, state.ApplicationId); var sources = Sources();
        var result = new List<RibbonElement>();
        var excluded = _host.QuickAccessItems.SelectMany(e => e.Tree()).ToHashSet();
        var sourceModels = sources.Values.SelectMany(e => e.Source.Tree()).ToHashSet();
        var occupied = _host.AllElements.Where(e => !excluded.Contains(e) || sourceModels.Contains(e)).Select(e => e.Id).ToHashSet(StringComparer.Ordinal);
        try
        {
            foreach (var row in state.QuickAccess)
            {
                if (!sources.TryGetValue(row.CommandId, out var entry) || persistent && !entry.HasStableIdentity)
                    throw new InvalidOperationException("Unknown, transient or nonpersistent command: " + row.CommandId);
                RibbonElement model;
                if (_presentations.TryGetValue(row.Id, out var retained) && !retained.IsDisposed && ReferenceEquals(RibbonQuickAccess.Source(retained), entry.Source)
                    && (retained.Parent is null || ReferenceEquals(retained.Parent, _host) || ReferenceEquals(retained.Parent, this))) model = retained;
                else { model = Create(entry.Source); model.Id = row.Id; }
                result.Add(model);
                // Direct commands retain their source ID when moving between
                // the toolbar and Commands; reference IDs remain separate.
                if (!ReferenceEquals(model, entry.Source) && !occupied.Add(model.Id))
                    throw new InvalidOperationException("A QAT presentation identity conflicts with another component: " + model.Id);
            }
            if (result.Distinct().Count() != result.Count) throw new InvalidOperationException("A presentation model cannot occupy two QAT slots.");
            return result;
        }
        catch { ReleasePlan(result); throw; }
    }
    internal void ReleasePlan(IEnumerable<RibbonElement> plan)
    {
        foreach (var model in plan.Where(m => !m.IsDisposed && m.Parent is null && !_keys.ContainsKey(m)).Distinct()) model.Dispose();
    }
    internal void ValidateSources(RibbonCustomizationState state)
    {
        var plan = Prepare(state, true); ReleasePlan(plan);
    }
    private static RibbonElement Create(RibbonElement source) => source switch
    {
        RibbonGroup group => new RibbonQuickAccessGroupReference { Source = group },
        RibbonComboBoxItem combo => new RibbonQuickAccessComboBoxReference { Source = combo },
        RibbonDropDownItem menu => new RibbonQuickAccessDropDownReference { Source = menu },
        RibbonButtonItem button => new RibbonQuickAccessReference { Source = button },
        _ => throw new InvalidOperationException("The command has no QAT presentation.")
    };
    internal void Apply(RibbonCustomizationState state, List<RibbonElement> desired)
    {
        if (Applying) throw new InvalidOperationException("Customization Apply is already active.");
        var sources = Sources(); var old = _host.QuickAccessItems.ToArray(); var queue = new Queue<RibbonElement>(desired); var combined = new List<RibbonElement>();
        foreach (var entry in old)
            if (!Managed(entry, sources)) combined.Add(entry);
            else if (queue.TryDequeue(out var next)) combined.Add(next);
        combined.AddRange(queue);
        var oldPosition = _host.QuickAccessPosition; bool oldVisible = _host.QuickAccessVisible;
        var fromCatalog = desired.OfType<RibbonItem>().Where(e => ReferenceEquals(e.Parent, this)).ToArray();
        Applying = true; _host.BeginUpdate();
        try
        {
            _host.QuickAccessItems.Clear();
            foreach (var command in old.OfType<RibbonItem>().Where(e => !combined.Contains(e)))
                if (!command.IsDisposed && command.Parent is null) Commands.Add(command);
            foreach (var command in fromCatalog) Commands.Remove(command);
            _host.QuickAccessItems.AddRange(combined.ToArray());
            _host.QuickAccessPosition = state.QuickAccessPosition; _host.QuickAccessVisible = state.QuickAccessVisible;
            foreach (var row in state.QuickAccess.Zip(desired)) { _keys[row.Second] = row.First.Id; _presentations[row.First.Id] = row.Second; }
        }
        catch
        {
            _host.QuickAccessItems.Clear();
            foreach (var command in old.OfType<RibbonItem>().Where(e => ReferenceEquals(e.Parent, this))) Commands.Remove(command);
            foreach (var command in fromCatalog.Where(e => e.Parent is null)) Commands.Add(command);
            _host.QuickAccessItems.AddRange(old); _host.QuickAccessPosition = oldPosition; _host.QuickAccessVisible = oldVisible;
            throw;
        }
        finally { try { _host.EndUpdate(); } finally { Applying = false; } }
    }
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing && !IsDisposed)
        {
            foreach (var model in _presentations.Values.Distinct().Where(m => !m.IsDisposed && m.Parent is null).ToArray()) model.Dispose();
            _presentations.Clear(); _keys.Clear();
        }
        base.Dispose(disposing);
    }
}
