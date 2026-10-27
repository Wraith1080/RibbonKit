using System.ComponentModel;

namespace RibbonKit.WinForms;

/// <summary>A compact, single-line native editor. Text is its label; Value is its editable content.</summary>
[DefaultProperty(nameof(Value)), DefaultEvent(nameof(ValueChanged))]
public sealed class RibbonTextBoxItem : RibbonItem, IRibbonEditorItem
{
    private string _value = "";
    private int _inputWidth = 120, _maxLength = 32767;
    private bool _readOnly;
    /// <summary>Gets or sets the live text, including uncommitted user edits.</summary>
    [DefaultValue(""), Bindable(true), Localizable(true), Category("Data")]
    public string Value
    {
        get => _value;
        set
        {
            Ribbon?.VerifyAccess();
            value ??= "";
            if (_value == value) return;
            _value = value;
            Changed(false);
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    /// <summary>Gets or sets the editor width in 96-DPI logical pixels, excluding its label.</summary>
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
    /// <summary>Gets or sets whether typing is prevented while selection and copying remain available.</summary>
    [DefaultValue(false), Category("Behavior")]
    public bool ReadOnly
    {
        get => _readOnly;
        set
        {
            Ribbon?.VerifyAccess();
            if (_readOnly == value) return;
            _readOnly = value;
            Changed(false);
        }
    }
    /// <summary>Gets or sets the native user-input limit; zero means unlimited. Programmatic Value is not truncated.</summary>
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
    /// <summary>Occurs once after the live Value changes, including an Escape rollback.</summary>
    public event EventHandler? ValueChanged;
    /// <summary>Occurs before an edit is committed. Cancellation retains editing focus during ordinary navigation.</summary>
    public event CancelEventHandler? ValueValidating;
    /// <summary>Occurs after successful validation and writing OnValidation bindings.</summary>
    public event EventHandler? ValueValidated;
    /// <summary>Occurs after a changed value is successfully committed by Enter or leaving the editor.</summary>
    public event EventHandler? ValueCommitted;
    /// <summary>Occurs after Escape restores the last committed or focus-entry value.</summary>
    public event EventHandler? EditCanceled;
    /// <summary>Focuses this item's active native editor without changing its value or selection.</summary>
    public override void PerformClick()
    {
        Ribbon?.VerifyAccess();
        if (EffectiveEnabled && Visible) Ribbon?.FocusEditor(this);
    }
    // Component bindings automatically write on an event named Validating,
    // before later subscribers can cancel. Validate explicitly before writing.
    object IRibbonEditorItem.CaptureEdit() => Value;
    bool IRibbonEditorItem.IsEditChanged(object baseline) => Value != (string)baseline;
    void IRibbonEditorItem.ValidateEdit(CancelEventArgs args) => ValueValidating?.Invoke(this, args);
    void IRibbonEditorItem.AcceptEdit(bool changed)
    {
        foreach (var binding in DataBindings.Cast<Binding>().ToArray())
        {
            if (IsDisposed) return;
            if (binding.DataSourceUpdateMode == DataSourceUpdateMode.OnValidation)
                binding.WriteValue();
        }
        if (IsDisposed) return;
        ValueValidated?.Invoke(this, EventArgs.Empty);
        if (changed && !IsDisposed) ValueCommitted?.Invoke(this, EventArgs.Empty);
    }
    void IRibbonEditorItem.CancelEdit(object baseline)
    {
        string value = (string)baseline;
        bool changed = Value != value;
        Value = value;
        if (changed && !IsDisposed) EditCanceled?.Invoke(this, EventArgs.Empty);
    }
}
