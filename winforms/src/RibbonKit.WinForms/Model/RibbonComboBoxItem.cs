using System.Collections.ObjectModel;
using System.ComponentModel;

namespace RibbonKit.WinForms;

/// <summary>A compact native string chooser with optional text editing. Text is the label; Value is the content.</summary>
[DefaultProperty(nameof(Items)), DefaultEvent(nameof(ValueChanged))]
public sealed class RibbonComboBoxItem : RibbonItem, IRibbonEditorItem
{
    private string _value = "";
    private int _selectedIndex = -1, _inputWidth = 120, _maxLength = 32767;
    private bool _isEditable;
    private readonly record struct EditValue(string Value, int Index);
    /// <summary>Initializes an empty chooser with no selected item.</summary>
    public RibbonComboBoxItem() => Items = new(this);
    /// <summary>Gets the authored string choices. Mutations preserve a surviving selection and close a stale choice popup.</summary>
    [Category("Data"), Localizable(true), DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    public RibbonComboBoxChoiceCollection Items { get; }
    /// <summary>Gets or sets the live text. An unmatched value clears a selection-only chooser; editable mode retains it.</summary>
    [DefaultValue(""), Category("Data"), Bindable(true), Localizable(true)]
    public string Value
    {
        get => _value;
        set
        {
            Ribbon?.VerifyAccess();
            value ??= "";
            int index = value == _value && _selectedIndex >= 0 ? _selectedIndex : Items.IndexOf(value);
            SetState(!_isEditable && index < 0 ? "" : value, index);
        }
    }
    /// <summary>Gets or sets the selected choice index, or -1 for no selection. Editable free text can have index -1.</summary>
    [DefaultValue(-1), Category("Data"), Bindable(true)]
    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            Ribbon?.VerifyAccess();
            if (value < -1 || value >= Items.Count) throw new ArgumentOutOfRangeException(nameof(value));
            SetState(value >= 0 ? Items[value] : _isEditable ? _value : "", value);
        }
    }
    /// <summary>Gets or sets whether native text entry is allowed in addition to selecting a choice.</summary>
    [DefaultValue(false), Category("Behavior")]
    public bool IsEditable
    {
        get => _isEditable;
        set
        {
            Ribbon?.VerifyAccess();
            if (_isEditable == value) return;
            _isEditable = value;
            if (!SetState(!value && _selectedIndex < 0 ? "" : _value, _selectedIndex)) Changed(false);
        }
    }
    /// <summary>Gets or sets the native editor width in 96-DPI logical pixels, excluding its label.</summary>
    [DefaultValue(120), Category("Layout")]
    public int InputWidth
    {
        get => _inputWidth;
        set
        {
            Ribbon?.VerifyAccess();
            if (value < 24) throw new ArgumentOutOfRangeException(nameof(value));
            if (_inputWidth == value) return;
            _inputWidth = value;
            Changed();
        }
    }
    /// <summary>Gets or sets the native editable-input limit; zero means unlimited. Programmatic values are not truncated.</summary>
    [DefaultValue(32767), Category("Behavior")]
    public int MaxLength
    {
        get => _maxLength;
        set
        {
            Ribbon?.VerifyAccess();
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
            if (_maxLength == value) return;
            _maxLength = value;
            Changed(false);
        }
    }
    /// <summary>Gets whether this item's library-owned choice popup is open.</summary>
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool IsDropDownOpen => Ribbon?.Editors.IsDropDownOpen(this) ?? false;
    /// <summary>Occurs after the live Value changes, including cancellation or a choice mutation.</summary>
    public event EventHandler? ValueChanged;
    /// <summary>Occurs after the selected index changes, including a shift caused by inserting or removing choices.</summary>
    public event EventHandler? SelectedIndexChanged;
    /// <summary>Occurs before committing an edit or user selection. Cancellation prevents acceptance.</summary>
    public event CancelEventHandler? ValueValidating;
    /// <summary>Occurs after validation succeeds and deferred bindings have been written.</summary>
    public event EventHandler? ValueValidated;
    /// <summary>Occurs once after accepting a changed value or selection.</summary>
    public event EventHandler? ValueCommitted;
    /// <summary>Occurs after restoring a changed edit or unaccepted choice preview.</summary>
    public event EventHandler? EditCanceled;
    /// <summary>Focuses the active native editor while preserving editable text selection.</summary>
    public override void PerformClick()
    {
        Ribbon?.VerifyAccess();
        if (EffectiveEnabled && Visible) Ribbon?.FocusEditor(this);
    }
    /// <summary>Focuses the native editor and opens its themed choice popup when visible and enabled.</summary>
    public void ShowDropDown()
    {
        PerformClick();
        if (EffectiveEnabled && Visible) Ribbon?.Editors.SetDropDown(this, true);
    }
    /// <summary>Closes only the choice popup and cancels its unaccepted selection.</summary>
    public void CloseDropDown()
    {
        Ribbon?.VerifyAccess();
        Ribbon?.Editors.SetDropDown(this, false);
    }
    internal int ChoicesVersion { get; private set; }
    private bool SetState(string value, int index)
    {
        bool textChanged = _value != value, indexChanged = _selectedIndex != index;
        if (!textChanged && !indexChanged) return false;
        _value = value; _selectedIndex = index;
        Changed(false);
        if (textChanged && !IsDisposed) ValueChanged?.Invoke(this, EventArgs.Empty);
        if (indexChanged && !IsDisposed) SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
        return true;
    }
    internal void ChoicesChanged(int index, int delta, bool clear = false)
    {
        ChoicesVersion++;
        int selection = _selectedIndex;
        if (clear || delta < 0 && index == selection) selection = -1;
        else if (selection >= 0 && index <= selection) selection += delta;
        string value = selection >= 0 ? Items[selection] : _isEditable ? _value : "";
        if (!SetState(value, selection)) Changed(false);
    }
    object IRibbonEditorItem.CaptureEdit() => new EditValue(Value, SelectedIndex);
    bool IRibbonEditorItem.IsEditChanged(object baseline) => (EditValue)baseline != new EditValue(Value, SelectedIndex);
    void IRibbonEditorItem.ValidateEdit(CancelEventArgs args) => ValueValidating?.Invoke(this, args);
    void IRibbonEditorItem.AcceptEdit(bool changed)
    {
        foreach (var binding in DataBindings.Cast<Binding>().ToArray())
        {
            if (IsDisposed) return;
            if (binding.DataSourceUpdateMode == DataSourceUpdateMode.OnValidation) binding.WriteValue();
        }
        if (IsDisposed) return;
        ValueValidated?.Invoke(this, EventArgs.Empty);
        if (changed && !IsDisposed) ValueCommitted?.Invoke(this, EventArgs.Empty);
    }
    void IRibbonEditorItem.CancelEdit(object baseline)
    {
        var previous = (EditValue)baseline;
        int index = previous.Index >= 0 && previous.Index < Items.Count && Items[previous.Index] == previous.Value
            ? previous.Index : Items.IndexOf(previous.Value);
        bool changed = SetState(!_isEditable && index < 0 ? "" : previous.Value, index);
        if (changed && !IsDisposed) EditCanceled?.Invoke(this, EventArgs.Empty);
    }
}

/// <summary>Owns a ComboBox item's authored string choices and notifies its native presentation.</summary>
public sealed class RibbonComboBoxChoiceCollection : Collection<string>
{
    private readonly RibbonComboBoxItem _owner;
    internal RibbonComboBoxChoiceCollection(RibbonComboBoxItem owner) => _owner = owner;
    /// <summary>Adds authored choices in order.</summary>
    public void AddRange(params string[] values)
    {
        foreach (var value in values) Add(value);
    }
    /// <inheritdoc />
    protected override void InsertItem(int index, string item)
    {
        _owner.Ribbon?.VerifyAccess(); ArgumentNullException.ThrowIfNull(item);
        base.InsertItem(index, item); _owner.ChoicesChanged(index, 1);
    }
    /// <inheritdoc />
    protected override void SetItem(int index, string item)
    {
        _owner.Ribbon?.VerifyAccess(); ArgumentNullException.ThrowIfNull(item);
        base.SetItem(index, item); _owner.ChoicesChanged(index, 0);
    }
    /// <inheritdoc />
    protected override void RemoveItem(int index)
    {
        _owner.Ribbon?.VerifyAccess(); base.RemoveItem(index); _owner.ChoicesChanged(index, -1);
    }
    /// <inheritdoc />
    protected override void ClearItems()
    {
        _owner.Ribbon?.VerifyAccess(); base.ClearItems(); _owner.ChoicesChanged(0, 0, true);
    }
}
