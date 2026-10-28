namespace RibbonKit.WinForms;

/// <summary>A UI-thread-affine customization transaction. Apply changes live models; Cancel retains the latest applied checkpoint.</summary>
public sealed class RibbonCustomizationSession : IDisposable
{
    private readonly RibbonCommandCatalog _catalog;
    private readonly string _applicationId;
    private readonly RibbonCustomizationState _defaults;
    private readonly RibbonCustomizationTarget? _target;
    private RibbonCustomizationState _checkpoint;
    private bool _disposed;
    private bool QatScope => _target is null or RibbonCustomizationTarget.QuickAccess;
    private bool RibbonScope => _target is null or RibbonCustomizationTarget.Ribbon or RibbonCustomizationTarget.Simplified;
    private bool SimplifiedScope => _target is null or RibbonCustomizationTarget.Simplified;
    internal int ApplyVersion { get; private set; }
    internal RibbonCustomizationSession(RibbonCommandCatalog catalog, string applicationId, RibbonCustomizationState defaults, RibbonCustomizationTarget? target)
    {
        _catalog = catalog; _applicationId = applicationId; _target = target; _defaults = defaults;
        if (RibbonScope) _defaults.Ribbon = catalog.Structure.Defaults;
        if (SimplifiedScope) _defaults.Simplified = new();
        _checkpoint = Capture(); Draft = RibbonCustomizationSerializer.Copy(_checkpoint);
    }
    /// <summary>Gets detached editable preferences. Cancel/Reload/Reset/Import replace this object; DTO edits never change live models.</summary>
    public RibbonCustomizationState Draft { get; private set; }
    /// <summary>Occurs after successful Apply and checkpoint update. The application may persist ExportJson here.</summary>
    public event EventHandler? Applied;
    private void Check() { ObjectDisposedException.ThrowIf(_disposed, this); _catalog.Check(); }
    private RibbonCustomizationState Capture()
    {
        var state = _catalog.Capture(_applicationId);
        if (_target is RibbonCustomizationTarget.Ribbon or RibbonCustomizationTarget.Simplified) state.Ribbon = _catalog.Structure.Capture();
        return state;
    }
    private bool Same(RibbonCustomizationState a, RibbonCustomizationState b) =>
        (!QatScope || RibbonCustomizationSerializer.Same(a, b))
        && (!RibbonScope || RibbonCustomizationSerializer.SameLayout(a.Ribbon, b.Ribbon))
        && (!SimplifiedScope || RibbonCustomizationSerializer.SameSimplified(a.Simplified, b.Simplified));
    internal bool HasChanges => !Same(Draft, _checkpoint);
    /// <summary>Preflights and applies this session's scope. Invalid native input returns false; invalid/stale profiles throw before mutation. Version-one/QAT-only data preserves live Ribbon structure.</summary>
    public bool Apply()
    {
        Check(); RibbonCustomizationSerializer.Validate(Draft, _applicationId);
        if (!Same(Capture(), _checkpoint)) throw new InvalidOperationException("Customization changed outside this session. Reload before applying.");
        List<RibbonElement>? qat = null, oldQat = null; RibbonStructureManager.Plan? ribbon = null;
        RibbonSimplifiedLayout? simplified = null;
        bool success = false;
        void Prepare()
        {
            if (QatScope) qat = _catalog.Prepare(Draft, false);
            if (RibbonScope && Draft.Ribbon is { } layout
                && (_catalog.Structure.IsCustomized || !RibbonCustomizationSerializer.SameLayout(layout, _catalog.Structure.Capture())))
                ribbon = _catalog.Structure.Prepare(layout, false);
            simplified = SimplifiedScope && Draft.Simplified is { } simple ? _catalog.Host.Simplified.Validate(simple, false, ribbon)
                : ribbon is not null ? _catalog.Host.Simplified.Validate(_catalog.Host.Simplified.Preferences, false, ribbon, prune: true) : null;
            ValidateCombined(qat, ribbon);
        }
        void Release()
        {
            if (qat is not null) _catalog.ReleasePlan(qat); qat = null;
            if (ribbon is not null) _catalog.Structure.Release(ribbon); ribbon = null;
        }
        try
        {
            Prepare();
            if (!_catalog.Host.Editors.CommitFocused()) return false;
            if (!Same(Capture(), _checkpoint)) throw new InvalidOperationException("Customization changed while committing input. Reload before applying.");
            Release(); Prepare();
            var previous = _catalog.Capture(_applicationId); var structure = _catalog.Structure; var saved = structure.Save();
            var previousSimplified = _catalog.Host.Simplified.Preferences;
            var selected = _catalog.Host.SelectedTab;
            if (qat is not null) oldQat = _catalog.Prepare(previous, false);
            var host = _catalog.Host; bool pendingEnd = true;
            host.Popups.CloseAll(false); host.GalleryStrips.CancelPreviews(); host.BeginUpdate(); structure.Applying = ribbon is not null;
            try
            {
                if (ribbon is not null) structure.Apply(ribbon);
                if (qat is not null) _catalog.Apply(Draft, qat);
                if (simplified is not null) host.Simplified.Preferences = simplified;
                structure.Applying = false; pendingEnd = false; host.EndUpdate();
                if (selected is not null && host.GetPresentedTabs().Contains(selected) && selected.Visible && selected.EffectiveEnabled) host.SelectedTab = selected;
                success = true;
            }
            catch
            {
                if (!pendingEnd) { host.BeginUpdate(); pendingEnd = true; }
                structure.Applying = true;
                host.Simplified.Preferences = previousSimplified;
                if (ribbon is not null) structure.Restore(saved);
                if (oldQat is not null) _catalog.Apply(previous, oldQat);
                if (ribbon is not null) structure.Discard(ribbon);
                structure.Applying = false; pendingEnd = false; host.EndUpdate();
                if (selected is not null && host.GetPresentedTabs().Contains(selected) && selected.Visible && selected.EffectiveEnabled) host.SelectedTab = selected;
                throw;
            }
            finally { structure.Applying = false; if (pendingEnd) host.EndUpdate(); }
        }
        finally { Release(); if (oldQat is not null) _catalog.ReleasePlan(oldQat); }
        if (!success) return false;
        _checkpoint = Capture(); Draft = RibbonCustomizationSerializer.Copy(_checkpoint); ApplyVersion++;
        _catalog.Host.OnCustomizationApplied(); Applied?.Invoke(this, EventArgs.Empty); return true;
    }
    /// <summary>Discards unapplied edits without rolling back prior Apply or overwriting external changes.</summary>
    public void Cancel() { Check(); Draft = RibbonCustomizationSerializer.Copy(_checkpoint); }
    /// <summary>Replaces the checkpoint and draft with live preferences for this scope.</summary>
    public void Reload() { Check(); _checkpoint = Capture(); Draft = RibbonCustomizationSerializer.Copy(_checkpoint); }
    /// <summary>Stages captured defaults for this scope. No live models change until Apply.</summary>
    public void Reset() { Check(); Draft = RibbonCustomizationSerializer.Copy(_defaults); }
    /// <summary>Stages validated native JSON. Ribbon-scoped sessions require a Ribbon section; legacy QAT profiles remain usable with QAT/full sessions.</summary>
    public void ImportJson(string json)
    {
        Check(); var state = RibbonCustomizationSerializer.Deserialize(json, _applicationId);
        if (_target == RibbonCustomizationTarget.Ribbon && state.Ribbon is null) throw new InvalidOperationException("This profile contains no Ribbon preferences.");
        if (_target == RibbonCustomizationTarget.Simplified && state.Simplified is null) throw new InvalidOperationException("This profile contains no Simplified preferences.");
        ValidatePersistent(state); Draft = state;
    }
    private void ValidatePersistent(RibbonCustomizationState state, bool all = false)
    {
        List<RibbonElement>? qat = null; RibbonStructureManager.Plan? ribbon = null;
        try
        {
            if (all || QatScope) qat = _catalog.Prepare(state, true);
            if ((all || RibbonScope) && state.Ribbon is { } layout) ribbon = _catalog.Structure.Prepare(layout, true);
            if ((all || SimplifiedScope) && state.Simplified is { } simple) _catalog.Host.Simplified.Validate(simple, true, ribbon);
            ValidateCombined(qat, ribbon);
        }
        finally { if (qat is not null) _catalog.ReleasePlan(qat); if (ribbon is not null) _catalog.Structure.Release(ribbon); }
    }
    private void ValidateCombined(List<RibbonElement>? qat, RibbonStructureManager.Plan? ribbon)
    {
        if (qat is null || ribbon is null) return;
        var ids = qat.SelectMany(e => e.Tree()).Select(e => e.Id).ToHashSet(StringComparer.Ordinal);
        void Check(string id) { if (ids.Contains(id)) throw new InvalidOperationException("Ribbon and QAT presentation identities conflict: " + id); }
        foreach (var tab in ribbon.Tabs)
        {
            Check(tab.Model.Id);
            foreach (var group in tab.Groups)
            {
                Check(group.Model.Id);
                foreach (var item in group.Items)
                {
                    Check(item.Id);
                    if (_catalog.Structure.Source(item) is RibbonGalleryItem gallery)
                        foreach (var tile in gallery.Items) Check(item.Id + ":tile:" + tile.Id);
                }
            }
        }
    }
    /// <summary>Exports the scoped draft merged with the other scope's live preferences. Applications own all file/storage operations.</summary>
    public string ExportJson()
    {
        Check(); var state = RibbonCustomizationSerializer.Copy(Draft); var live = _catalog.Capture(_applicationId);
        if (!RibbonScope) state.Ribbon = live.Ribbon;
        if (!QatScope) { state.QuickAccess = live.QuickAccess; state.QuickAccessPosition = live.QuickAccessPosition; state.QuickAccessVisible = live.QuickAccessVisible; }
        if (!SimplifiedScope) state.Simplified = live.Simplified;
        state.Version = 3; RibbonCustomizationSerializer.Validate(state, _applicationId);
        ValidatePersistent(state, true);
        return RibbonCustomizationSerializer.Serialize(state);
    }
    /// <summary>Releases this transaction without applying pending edits or disposing source models.</summary>
    public void Dispose() => _disposed = true;
}
