using System.ComponentModel;
using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

/// <summary>Selects the ordinary ribbon body presentation independently of density.</summary>
public enum RibbonLayoutMode
{
    /// <summary>Adaptive multi-row groups.</summary>
    Classic,
    /// <summary>One source-backed command row with More Commands and full-group access.</summary>
    Simplified
}
/// <summary>Defines an authored command's default Simplified placement.</summary>
public enum RibbonSimplifiedPlacement
{
    /// <summary>Keep the command in its full-group flyout unless customized inline.</summary>
    Overflow,
    /// <summary>Offer the command inline, subject to priority and available space.</summary>
    Inline
}

public partial class RibbonControl
{
    private readonly RibbonSimplifiedManager _simplified;
    private RibbonLayoutMode _layoutMode;
    internal RibbonSimplifiedManager Simplified => _simplified;
    /// <summary>Gets or sets Classic or Simplified presentation. Invalid focused input blocks a change. Applications own mode persistence.</summary>
    [Category("Layout"), DefaultValue(RibbonLayoutMode.Classic)]
    public RibbonLayoutMode LayoutMode
    {
        get => _layoutMode;
        set
        {
            VerifyAccess(); if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value));
            if (_layoutMode == value || !Editors.CommitFocused()) return;
            if (AnimationLevel != RibbonAnimationLevel.None && _motion?.Changing != true && _updateDepth == 0) { Motion.Change(false, () => LayoutMode = value); return; }
            Popups.CloseAll(false); ScreenTips.Cancel(); ExitKeyTips(false); GalleryStrips.CancelPreviews();
            _layoutMode = value; _bodyOffset = 0; ModelChanged();
            if (_focused is not null && !Snapshot.Targets.Any(t => ReferenceEquals(t.Element, _focused))) { _focused = SelectedTab; _focusedDropDown = false; }
            LayoutModeChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    /// <summary>Occurs after a layout-mode change, including native geometry refresh.</summary>
    public event EventHandler? LayoutModeChanged;
    internal void RefreshBorrowedGroupPresentation()
    {
        if (LayoutMode != RibbonLayoutMode.Simplified) return;
        _layoutDirty = true;
        foreach (var surface in Popups.Surfaces) surface.RefreshSimplifiedBody();
        Invalidate();
    }
}

internal sealed class RibbonSimplifiedMore(RibbonControl ribbon) : RibbonQuickAccessOverflow(ribbon, "More Commands")
{
    internal readonly List<RibbonElement> Entries = [];
}

internal sealed class RibbonSimplifiedManager
{
    private readonly RibbonControl _ribbon;
    private readonly Dictionary<RibbonElement, RibbonElement> _references = [];
    private readonly RibbonMenuSeparatorItem _separator;
    internal readonly RibbonSimplifiedMore More;
    internal RibbonSimplifiedLayout Preferences = new();
    internal RibbonSimplifiedManager(RibbonControl ribbon)
    {
        _ribbon = ribbon; More = new(ribbon);
        _separator = new() { Parent = ribbon };
    }
    internal IEnumerable<RibbonElement> Elements => _references.Values.Append(More).Append(_separator);
    internal sealed record Command(RibbonItem Item, RibbonGroup Group, string Id);
    internal static bool Supported(RibbonItem item) => item is RibbonButtonItem or RibbonDropDownItem or RibbonComboBoxItem;
    internal bool Permanent(RibbonElement element)
    {
        element = _ribbon.CommandCatalog.GetSource(element);
        if (RibbonControl.GetMergeSource(element) is not null) return false;
        for (var node = element; node is not null; node = node.Parent as RibbonElement) if (node is RibbonTab { IsContextual: true } or RibbonTab { IsModal: true }) return false;
        return true;
    }
    internal List<RibbonSimplifiedCommand> Defaults(RibbonTab tab) => Commands(tab, false).Where(c => Permanent(c.Item) && c.Item.SimplifiedPlacement == RibbonSimplifiedPlacement.Inline).Select(c => new RibbonSimplifiedCommand { CommandId = c.Id, Size = c.Item.SimplifiedSize }).ToList();
    internal IReadOnlyList<Command> Commands(RibbonTab tab, bool visible = true, RibbonStructureManager.Plan? plan = null)
    {
        var result = new List<Command>(); var seen = new HashSet<string>(StringComparer.Ordinal);
        var future = plan?.Tabs.FirstOrDefault(t => t.Model.Id == tab.Id);
        var groups = future is { Model: not null } f ? f.Groups.Select(g => (g.Model, Items: g.Data.IsCustom ? (IEnumerable<RibbonItem>)g.Items : g.Model.Items))
            : _ribbon.GetPresentedGroups(tab).Select(g => (g, Items: (IEnumerable<RibbonItem>)g.Items));
        foreach (var (group, items) in groups)
        {
            if (visible && !group.Visible) continue;
            foreach (var item in items)
            {
                if (!Supported(item) || visible && !item.Visible) continue;
                string id = _ribbon.CommandCatalog.GetSource(item).Id;
                if (seen.Add(id)) result.Add(new(item, group, id));
            }
        }
        return result;
    }
    internal IReadOnlyList<(Command Command, RibbonItemSize Size)> Selected(RibbonTab tab)
    {
        var commands = Commands(tab);
        if (Preferences.Tabs.FirstOrDefault(t => t.TabId == tab.Id) is { } selected)
            return selected.Commands.Select(row => (Command: commands.FirstOrDefault(c => c.Id == row.CommandId), row.Size)).Where(p => p.Command is not null).Select(p => (p.Command!, p.Size))
                .Concat(commands.Where(c => !Permanent(c.Item) && c.Item.SimplifiedPlacement == RibbonSimplifiedPlacement.Inline).Select(c => (c, c.Item.SimplifiedSize))).ToArray();
        return commands.Where(c => c.Item.SimplifiedPlacement == RibbonSimplifiedPlacement.Inline).Select(c => (c, c.Item.SimplifiedSize)).ToArray();
    }
    internal RibbonSimplifiedLayout Validate(RibbonSimplifiedLayout layout, bool persistent, RibbonStructureManager.Plan? plan = null, bool prune = false)
    {
        var copy = RibbonCustomizationSerializer.CopySimplified(layout)!;
        var tabs = plan is null ? _ribbon.Tabs.Where(t => !t.IsContextual && !t.IsModal).ToArray() : plan.Tabs.Select(t => t.Model).ToArray();
        foreach (var row in copy.Tabs.ToArray())
        {
            var tab = tabs.FirstOrDefault(t => t.Id == row.TabId);
            if (tab is null) { if (prune) { copy.Tabs.Remove(row); continue; } throw new InvalidOperationException("Unknown or transient Simplified tab: " + row.TabId); }
            if (persistent && !tab.HasExplicitId) throw new InvalidOperationException("Simplified tabs need explicit persistence identities.");
            var commands = Commands(tab, false, plan);
            foreach (var command in row.Commands.ToArray())
            {
                var source = commands.FirstOrDefault(c => c.Id == command.CommandId);
                if (source is null) { if (prune) { row.Commands.Remove(command); continue; } throw new InvalidOperationException("Unknown or unsupported Simplified command: " + command.CommandId); }
                if (persistent && !_ribbon.CommandCatalog.GetSource(source.Item).HasExplicitId) throw new InvalidOperationException("Simplified commands need explicit persistence identities.");
                if (!Permanent(source.Item)) throw new InvalidOperationException("Transient/contextual commands cannot be saved as Simplified preferences.");
            }
        }
        return copy;
    }
    private RibbonElement Reference(RibbonElement source)
    {
        if (_references.TryGetValue(source, out var retained) && !retained.IsDisposed) return retained;
        RibbonElement reference = source switch
        {
            RibbonGroup group => new RibbonQuickAccessGroupReference { Source = group },
            RibbonComboBoxItem combo => new RibbonQuickAccessComboBoxReference { Source = combo },
            RibbonDropDownItem menu => new RibbonQuickAccessDropDownReference { Source = menu },
            RibbonButtonItem button => new RibbonQuickAccessReference { Source = button },
            _ => throw new InvalidOperationException("Unsupported More Commands source.")
        };
        reference.Parent = _ribbon; _references[source] = reference; return reference;
    }
    internal void SetMore(IEnumerable<RibbonItem> displaced, IEnumerable<RibbonGroup> groups)
    {
        More.Entries.Clear(); More.Entries.AddRange(displaced.Select(Reference));
        var full = groups.ToArray(); if (More.Entries.Count > 0 && full.Length > 0) More.Entries.Add(_separator);
        More.Entries.AddRange(full.Select(Reference));
    }
    internal void Dispose()
    {
        More.Entries.Clear();
        foreach (var element in Elements.ToArray()) { element.Parent = null; element.Dispose(); }
        _references.Clear();
    }
}
