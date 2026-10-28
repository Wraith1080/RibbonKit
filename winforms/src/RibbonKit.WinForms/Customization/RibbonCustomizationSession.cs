namespace RibbonKit.WinForms;

/// <summary>A UI-thread-affine QAT editing transaction. Only Apply changes live models; Cancel keeps the latest applied checkpoint.</summary>
public sealed class RibbonCustomizationSession : IDisposable
{
    private readonly RibbonCommandCatalog _catalog;
    private readonly string _applicationId;
    private readonly RibbonCustomizationState _defaults;
    private RibbonCustomizationState _checkpoint;
    private bool _disposed;
    internal RibbonCustomizationSession(RibbonCommandCatalog catalog, string applicationId, RibbonCustomizationState defaults)
    {
        _catalog = catalog; _applicationId = applicationId; _defaults = defaults;
        _checkpoint = catalog.Capture(applicationId); Draft = RibbonCustomizationSerializer.Copy(_checkpoint);
    }
    /// <summary>Gets the detached editable state. Cancel/Reload/Reset/Import replace this object; no DTO mutation changes the ribbon.</summary>
    public RibbonCustomizationState Draft { get; private set; }
    /// <summary>Occurs after a successful Apply and checkpoint update. The application may save ExportJson here.</summary>
    public event EventHandler? Applied;
    private void Check() { ObjectDisposedException.ThrowIf(_disposed, this); _catalog.Check(); }
    /// <summary>Preflights a draft and applies it in a layout batch. Returns false for invalid focused native input; stale or invalid drafts throw before mutation.</summary>
    public bool Apply()
    {
        Check(); RibbonCustomizationSerializer.Validate(Draft, _applicationId);
        if (!RibbonCustomizationSerializer.Same(_catalog.Capture(_applicationId), _checkpoint)) throw new InvalidOperationException("QAT customization changed outside this session. Reload before applying.");
        var plan = _catalog.Prepare(Draft, false);
        try
        {
            if (!_catalog.Host.Editors.CommitFocused()) return false;
            if (!RibbonCustomizationSerializer.Same(_catalog.Capture(_applicationId), _checkpoint)) throw new InvalidOperationException("QAT customization changed while committing input. Reload before applying.");
            _catalog.ReleasePlan(plan);
            plan = _catalog.Prepare(Draft, false);
            _catalog.Apply(Draft, plan);
        }
        finally { _catalog.ReleasePlan(plan); }
        _checkpoint = _catalog.Capture(_applicationId); Draft = RibbonCustomizationSerializer.Copy(_checkpoint);
        Applied?.Invoke(this, EventArgs.Empty); return true;
    }
    /// <summary>Discards unapplied edits. It never rolls back an earlier successful Apply or overwrites external live changes.</summary>
    public void Cancel() { Check(); Draft = RibbonCustomizationSerializer.Copy(_checkpoint); }
    /// <summary>Replaces the draft/checkpoint with current live permanent QAT state after an external change.</summary>
    public void Reload() { Check(); _checkpoint = _catalog.Capture(_applicationId); Draft = RibbonCustomizationSerializer.Copy(_checkpoint); }
    /// <summary>Replaces only the draft with the catalog defaults captured when this session opened.</summary>
    public void Reset() { Check(); Draft = RibbonCustomizationSerializer.Copy(_defaults); }
    /// <summary>Replaces only the draft after strict JSON and source validation. No live models change until Apply.</summary>
    public void ImportJson(string json)
    {
        Check(); var state = RibbonCustomizationSerializer.Deserialize(json, _applicationId); _catalog.ValidateSources(state); Draft = state;
    }
    /// <summary>Exports the current draft after validation. All selected source IDs must be explicitly assigned; the application owns file/storage operations.</summary>
    public string ExportJson() { Check(); RibbonCustomizationSerializer.Validate(Draft, _applicationId); _catalog.ValidateSources(Draft); return RibbonCustomizationSerializer.Serialize(Draft); }
    /// <summary>Releases the transaction without applying pending edits or disposing its ribbon/catalog.</summary>
    public void Dispose() => _disposed = true;
}
