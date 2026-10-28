using System.ComponentModel;

namespace RibbonKit.WinForms;

/// <summary>Owns transient tabs and groups an application context contributes to one ribbon. Activation belongs to the application, not keyboard focus.</summary>
[DefaultProperty(nameof(Tabs))]
public sealed class RibbonMergeSource : RibbonElement
{
    private int _order;
    /// <summary>Initializes an empty source with its own binding context.</summary>
    public RibbonMergeSource()
    {
        Tabs = new(this); Groups = new(this); BindingContext = new BindingContext();
    }
    /// <summary>Gets the source-owned tabs. Merging never transfers or clones their models.</summary>
    [Category("Data"), DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    public RibbonTabCollection Tabs { get; }
    /// <summary>Gets the source-owned group contributions, addressed to authored host tabs by stable ID.</summary>
    [Category("Data"), DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    public RibbonGroupContributionCollection Groups { get; }
    /// <summary>Gets or sets tab ordering relative to host content (order zero). Equal orders retain this host's first-merge sequence; negative orders precede host tabs.</summary>
    [Category("Layout"), DefaultValue(0)]
    public int Order { get => _order; set { Ribbon?.VerifyAccess(); if (_order == value) return; _order = value; Changed(); } }
    /// <summary>Gets the borrowing host, or null while unmerged.</summary>
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public RibbonControl? MergedInto => Ribbon;
    /// <summary>Gets whether the source currently contributes to a host.</summary>
    [Browsable(false), Bindable(true), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool IsMerged => MergedInto is not null;
    /// <summary>Occurs after the source joins or leaves a host.</summary>
    public event EventHandler? IsMergedChanged;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal RibbonControl? RetainedBy { get; set; }
    internal override IEnumerable<RibbonElement> Children => Tabs.Cast<RibbonElement>().Concat(Groups);
    internal void NotifyMergeChanged() => IsMergedChanged?.Invoke(this, EventArgs.Empty);
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing && !IsDisposed) RetainedBy?.Merges.Forget(this);
        base.Dispose(disposing);
    }
}

/// <summary>A source-owned group presented inside an authored host tab. Missing target IDs are skipped until a matching tab exists.</summary>
[DefaultProperty(nameof(Group))]
public sealed class RibbonGroupContribution : RibbonElement
{
    private string _targetTabId = "";
    private int _order;
    /// <summary>Initializes the owned group.</summary>
    public RibbonGroupContribution() => Group = new() { Parent = this };
    /// <summary>Gets the owned group declaration. Its models and binding context remain with this contribution.</summary>
    [Category("Data"), DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    public RibbonGroup Group { get; }
    /// <summary>Gets or sets the exact stable ID of an authored host tab. Empty or unmatched IDs have no presentation.</summary>
    [Category("Data"), DefaultValue("")]
    public string TargetTabId
    {
        get => _targetTabId;
        set { Ribbon?.VerifyAccess(); value ??= ""; if (_targetTabId == value) return; _targetTabId = value; Changed(); }
    }
    /// <summary>Gets or sets group ordering relative to host groups (zero). Equal orders retain source first-merge sequence and declaration order.</summary>
    [Category("Layout"), DefaultValue(0)]
    public int Order { get => _order; set { Ribbon?.VerifyAccess(); if (_order == value) return; _order = value; Changed(); } }
    internal RibbonTab? TargetTab => Ribbon?.Tabs.FirstOrDefault(t => !t.IsDisposed && t.Id == TargetTabId);
    internal override IEnumerable<RibbonElement> Children => [Group];
}

/// <summary>Owns the group contributions of a merge source.</summary>
[Editor("RibbonKit.WinForms.Design.GroupContributionCollectionEditor, RibbonKit.WinForms.Design", "System.Drawing.Design.UITypeEditor, System.Drawing")]
public sealed class RibbonGroupContributionCollection : RibbonElementCollection<RibbonGroupContribution>
{
    internal RibbonGroupContributionCollection(RibbonMergeSource owner) : base(owner) { }
}
