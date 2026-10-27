using System.Collections.ObjectModel;
using System.ComponentModel;

namespace RibbonKit.WinForms;

/// <summary>A collection that gives each component exactly one logical owner.</summary>
public class RibbonElementCollection<T> : Collection<T> where T : RibbonElement
{
    private readonly object _owner;
    internal RibbonElementCollection(object owner) => _owner = owner;
    private RibbonControl? Ribbon => _owner is RibbonControl r ? r : (_owner as RibbonElement)?.Ribbon;
    private void Changed()
    {
        if (Ribbon is { } ribbon) ribbon.ModelChanged(preservePopups: ribbon.Popups.IsPreparingContent(_owner as RibbonElement, true));
    }
    private void Verify(T item, T? replacing = null)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (item is RibbonBackstagePageItem && _owner is not RibbonBackstage)
            throw new ArgumentException("Backstage pages belong to Backstage.Items.", nameof(item));
        if (_owner is RibbonBackstage && item is not RibbonBackstagePageItem && item.GetType() != typeof(RibbonButtonItem))
            throw new ArgumentException("Backstage entries must be pages or button commands.", nameof(item));
        if (item is RibbonMenuSeparatorItem && _owner is not RibbonDropDownItem)
            throw new ArgumentException("Menu separators belong to a dropdown's Items collection.", nameof(item));
        if (item.IsDisposed)
            throw new ObjectDisposedException(item.GetType().Name);
        if (item.Parent is not null && !ReferenceEquals(item, replacing))
            throw new InvalidOperationException("A ribbon component can only belong to one collection.");
        if (item.Tree().Contains(_owner))
            throw new InvalidOperationException("A ribbon component cannot contain its owner.");
        Ribbon?.ValidateAddition(item, replacing);
        if (Ribbon is null && _owner is RibbonElement owner)
        {
            while (owner.Parent is RibbonElement parent)
                owner = parent;
            var excluded = replacing?.Tree().ToHashSet() ?? [];
            var existing = owner.Tree().Where(n => !excluded.Contains(n)).Select(n => n.Id).ToHashSet(StringComparer.Ordinal);
            foreach (var node in item.Tree())
                if (!existing.Add(node.Id))
                    throw new ArgumentException("Duplicate component identity: " + node.Id);
        }
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var node in item.Tree())
            if (!ids.Add(node.Id))
                throw new ArgumentException("Duplicate component identity: " + node.Id);
    }
    /// <inheritdoc />
    protected override void InsertItem(int index, T item)
    {
        Verify(item);
        Ribbon?.VerifyAccess();
        base.InsertItem(index, item);
        item.Parent = _owner;
        if (Ribbon is { } ribbon)
            item.BindingContext = ribbon.BindingContext;
        RibbonRadioButtonItem.NormalizeAttached(item);
        Changed();
    }
    /// <inheritdoc />
    protected override void SetItem(int index, T item)
    {
        var previous = this[index];
        if (ReferenceEquals(item, previous))
            return;
        Verify(item, previous);
        Ribbon?.VerifyAccess();
        if (_owner is RibbonBackstage backstage) backstage.Removing(previous, index);
        previous.Parent = null;
        base.SetItem(index, item);
        item.Parent = _owner;
        if (Ribbon is { } ribbon)
            item.BindingContext = ribbon.BindingContext;
        RibbonRadioButtonItem.NormalizeAttached(item);
        Changed();
    }
    /// <inheritdoc />
    protected override void RemoveItem(int index)
    {
        Ribbon?.VerifyAccess();
        var ribbon = Ribbon;
        var item = this[index];
        if (_owner is RibbonBackstage backstage) backstage.Removing(item, index);
        if (item is RibbonTab tab)
            ribbon?.RemovingTab(tab, index);
        item.Parent = null;
        base.RemoveItem(index);
        Changed();
    }
    /// <inheritdoc />
    protected override void ClearItems()
    {
        Ribbon?.VerifyAccess();
        foreach (var item in this)
            item.Parent = null;
        base.ClearItems();
        Changed();
    }
    /// <summary>Adds authored components in order.</summary>
    public void AddRange(params T[] items)
    {
        foreach (var item in items)
            Add(item);
    }
}

/// <summary>Owns the tabs of one ribbon.</summary>
[Editor("RibbonKit.WinForms.Design.TabCollectionEditor, RibbonKit.WinForms.Design", "System.Drawing.Design.UITypeEditor, System.Drawing")]
public sealed class RibbonTabCollection : RibbonElementCollection<RibbonTab>
{
    internal RibbonTabCollection(object owner) : base(owner) { }
}
/// <summary>Owns the groups of one tab.</summary>
[Editor("RibbonKit.WinForms.Design.GroupCollectionEditor, RibbonKit.WinForms.Design", "System.Drawing.Design.UITypeEditor, System.Drawing")]
public sealed class RibbonGroupCollection : RibbonElementCollection<RibbonGroup>
{
    internal RibbonGroupCollection(object owner) : base(owner) { }
}
/// <summary>Owns the commands of one group or dropdown.</summary>
[Editor("RibbonKit.WinForms.Design.ItemCollectionEditor, RibbonKit.WinForms.Design", "System.Drawing.Design.UITypeEditor, System.Drawing")]
public sealed class RibbonItemCollection : RibbonElementCollection<RibbonItem>
{
    internal RibbonItemCollection(object owner) : base(owner) { }
}
