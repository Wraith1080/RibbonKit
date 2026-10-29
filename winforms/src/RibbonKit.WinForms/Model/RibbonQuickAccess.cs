using System.ComponentModel;

namespace RibbonKit.WinForms;

/// <summary>Places the Quick Access Toolbar in an application window.</summary>
public enum RibbonQuickAccessPosition
{
    /// <summary>Before the ribbon tabs.</summary>
    TabRow,
    /// <summary>In a full-width row below the ribbon body or minimized tab row.</summary>
    BelowRibbon,
    /// <summary>In an associated RibbonForm caption; otherwise displayed in TabRow while retaining this preference.</summary>
    TitleBar
}

/// <summary>A separate presentation of a borrowed button, toggle, checkbox or radio command. Disposing the reference never disposes its source.</summary>
public sealed class RibbonQuickAccessReference : RibbonElement
{
    private RibbonButtonItem? _source;
    /// <summary>Gets or sets the borrowed command, including checkbox/radio sources with their original state and grouping. An attached source must belong to the same ribbon. Inherited Text optionally overrides its caption.</summary>
    [Category("Data"), DefaultValue(null)]
    public RibbonButtonItem? Source
    {
        get => _source;
        set
        {
            Ribbon?.VerifyAccess();
            if (ReferenceEquals(_source, value)) return;
            RibbonQuickAccess.ValidateSource(value, Ribbon);
            _source = value;
            MergeProvenance = value is null ? null : RibbonControl.GetMergeSource(value);
            Changed();
            SourceChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    /// <summary>Occurs after the borrowed source changes.</summary>
    public event EventHandler? SourceChanged;
}

/// <summary>A chooser-only Quick Access presentation of a borrowed ComboBox. Text editing stays in the source's native field.</summary>
public sealed class RibbonQuickAccessComboBoxReference : RibbonElement
{
    private RibbonComboBoxItem? _source;
    /// <summary>Gets or sets the borrowed source. Its choices, selection, bindings and validation are shared; its native editor is not moved or duplicated.</summary>
    [Category("Data"), DefaultValue(null)]
    public RibbonComboBoxItem? Source
    {
        get => _source;
        set
        {
            Ribbon?.VerifyAccess();
            if (ReferenceEquals(_source, value)) return;
            RibbonQuickAccess.ValidateComboSource(value, Ribbon);
            _source = value;
            MergeProvenance = value is null ? null : RibbonControl.GetMergeSource(value);
            Changed();
            SourceChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    /// <summary>Occurs after the borrowed source changes.</summary>
    public event EventHandler? SourceChanged;
    /// <summary>Gets whether this reference's choice popup is open. The source's ordinary field popup remains separate.</summary>
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool IsDropDownOpen => Ribbon?.Editors.IsQuickAccessOpen(this) ?? false;
}

/// <summary>A Quick Access presentation of a borrowed dropdown, gallery or split button, with its own popup and optional split-menu KeyTip.</summary>
public sealed class RibbonQuickAccessDropDownReference : RibbonElement
{
    private RibbonDropDownItem? _source;
    private string _dropDownKeyTip = "";
    /// <summary>Gets or sets the borrowed dropdown, gallery or split source. Its commands and gallery selection retain their original owner.</summary>
    [Category("Data"), DefaultValue(null)]
    public RibbonDropDownItem? Source
    {
        get => _source;
        set
        {
            Ribbon?.VerifyAccess();
            if (ReferenceEquals(_source, value)) return;
            RibbonQuickAccess.ValidateMenuSource(value, Ribbon);
            _source = value; MergeProvenance = value is null ? null : RibbonControl.GetMergeSource(value); Changed(); SourceChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    /// <summary>Occurs after the borrowed source changes.</summary>
    public event EventHandler? SourceChanged;
    /// <summary>Gets whether this reference's own menu is open; the source command and other references have separate open state.</summary>
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool IsDropDownOpen => Ribbon?.Popups.IsExpanded(this) ?? false;
    /// <summary>Gets or sets the preferred KeyTip of a split source's menu half. Empty uses automatic resolution.</summary>
    [Category("Behavior"), DefaultValue("")]
    public string DropDownKeyTip
    {
        get => _dropDownKeyTip;
        set { Ribbon?.VerifyAccess(); value ??= ""; if (_dropDownKeyTip == value) return; _dropDownKeyTip = value; Changed(false); }
    }
}

/// <summary>A compact Quick Access presentation of a borrowed group. Attached references keep the source collapsed without owning its commands.</summary>
public sealed class RibbonQuickAccessGroupReference : RibbonElement
{
    private RibbonGroup? _source;
    /// <summary>Gets or sets the borrowed group. A live attached source must belong to the same ribbon; removing the last reference releases its sizing constraint.</summary>
    [Category("Data"), DefaultValue(null)]
    public RibbonGroup? Source
    {
        get => _source;
        set
        {
            Ribbon?.VerifyAccess();
            if (ReferenceEquals(_source, value)) return;
            RibbonQuickAccess.ValidateGroupSource(value, Ribbon);
            _source = value; MergeProvenance = value is null ? null : RibbonControl.GetMergeSource(value); Changed(); SourceChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    /// <summary>Occurs after the borrowed source changes.</summary>
    public event EventHandler? SourceChanged;
    /// <summary>Gets whether this reference's flyout is open. Other references and the source opener have separate open state.</summary>
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool IsDropDownOpen => Ribbon?.Popups.IsExpanded(this) ?? false;
}

/// <summary>Owns toolbar-only buttons, toggles and borrowed command, menu, group or ComboBox references in their authored order.</summary>
[Editor("RibbonKit.WinForms.Design.QuickAccessCollectionEditor, RibbonKit.WinForms.Design", "System.Drawing.Design.UITypeEditor, System.Drawing")]
public sealed class RibbonQuickAccessCollection : RibbonElementCollection<RibbonElement>
{
    internal RibbonQuickAccessCollection(RibbonControl owner) : base(owner) { }
    /// <inheritdoc />
    protected override void InsertItem(int index, RibbonElement item) { Validate(item); base.InsertItem(index, item); }
    /// <inheritdoc />
    protected override void SetItem(int index, RibbonElement item) { Validate(item); base.SetItem(index, item); }
    private static void Validate(RibbonElement item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (item is not RibbonQuickAccessReference and not RibbonQuickAccessComboBoxReference and not RibbonQuickAccessDropDownReference and not RibbonQuickAccessGroupReference && !RibbonQuickAccess.IsButton(item))
            throw new ArgumentException("Quick Access supports buttons, toggles and command, menu, group or ComboBox references.", nameof(item));
    }
}

internal static class RibbonQuickAccess
{
    internal const int RowHeight = 32, ButtonWidth = 28;
    internal static bool IsButton(RibbonElement element) => element is RibbonButtonItem and not RibbonCheckBoxItem and not RibbonRadioButtonItem;
    internal static RibbonItem? Command(RibbonElement entry) => entry switch { RibbonQuickAccessReference reference => reference.Source, RibbonQuickAccessComboBoxReference reference => reference.Source, RibbonQuickAccessDropDownReference reference => reference.Source, _ => entry as RibbonButtonItem };
    internal static RibbonElement? Source(RibbonElement entry) => entry is RibbonQuickAccessGroupReference group ? group.Source : Command(entry);
    internal static bool RetainsGroup(RibbonGroup group) => group.Ribbon is { } ribbon
        && ribbon.QuickAccessItems.OfType<RibbonQuickAccessGroupReference>().Any(r => ReferenceEquals(r.Source, group));
    internal static int Width(RibbonElement entry) => entry is RibbonQuickAccessComboBoxReference or RibbonQuickAccessDropDownReference or RibbonQuickAccessGroupReference ? 40 : ButtonWidth;
    internal static string Caption(RibbonElement entry) => entry.Text.Length > 0 ? entry.Text : Source(entry)?.DisplayText ?? "Unavailable";
    internal static bool Available(RibbonElement entry)
    {
        if (entry.Ribbon is { IsModal: true } host && ReferenceEquals(Source(entry), host.FileMenu)) return false;
        if (!entry.Visible || !entry.EffectiveEnabled || Source(entry) is not { IsDisposed: false } command ||
            !ReferenceEquals(command.Ribbon, entry.Ribbon) || entry.Ribbon is null || !command.EffectiveEnabled) return false;
        for (RibbonElement? node = command; node is not null; node = node.Parent as RibbonElement)
            if (!node.Visible) return false;
        return true;
    }
    internal static void ValidateSource(RibbonButtonItem? source, RibbonControl? ribbon)
    {
        if (source is null) return;
        if (source.IsDisposed) throw new ObjectDisposedException(nameof(source));
        if (ribbon is not null && source.Ribbon is not null && !ReferenceEquals(source.Ribbon, ribbon))
            throw new ArgumentException("A Quick Access source must belong to the same ribbon.", nameof(source));
    }
    internal static void ValidateComboSource(RibbonComboBoxItem? source, RibbonControl? ribbon)
    {
        if (source is null) return;
        if (source.IsDisposed) throw new ObjectDisposedException(nameof(source));
        if (ribbon is not null && source.Ribbon is not null && !ReferenceEquals(source.Ribbon, ribbon))
            throw new ArgumentException("A Quick Access source must belong to the same ribbon.", nameof(source));
    }
    internal static void ValidateMenuSource(RibbonDropDownItem? source, RibbonControl? ribbon)
    {
        if (source is null) return;
        if (source.IsDisposed) throw new ObjectDisposedException(nameof(source));
        if (ribbon is not null && source.Ribbon is not null && !ReferenceEquals(source.Ribbon, ribbon))
            throw new ArgumentException("A Quick Access source must belong to the same ribbon.", nameof(source));
    }
    internal static void ValidateGroupSource(RibbonGroup? source, RibbonControl? ribbon)
    {
        if (source is null) return;
        if (source.IsDisposed) throw new ObjectDisposedException(nameof(source));
        if (ribbon is not null && source.Ribbon is not null && !ReferenceEquals(source.Ribbon, ribbon))
            throw new ArgumentException("A Quick Access source must belong to the same ribbon.", nameof(source));
    }
    internal static string AutomaticKeyTip(int index, int count)
    {
        var culture = System.Globalization.CultureInfo.InvariantCulture;
        if (index < 9) return (index + 1).ToString(culture);
        // Equal-length suffixes avoid Q1 swallowing Q10 in a large overflow.
        int digits = Math.Max(1, count - 9).ToString(culture).Length;
        return "Q" + (index - 8).ToString("D" + digits.ToString(culture), culture);
    }
}

internal class RibbonQuickAccessOverflow : RibbonElement
{
    internal RibbonQuickAccessOverflow(RibbonControl ribbon, string text = "More Quick Access commands") { Text = text; KeyTip = "0"; Parent = ribbon; }
}
