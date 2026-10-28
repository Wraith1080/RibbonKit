using System.ComponentModel;

namespace RibbonKit.WinForms;

/// <summary>Chooses command target size independently of theme, font and DPI.</summary>
public enum RibbonDensity
{
    /// <summary>The default mouse/keyboard geometry.</summary>
    Compact,
    /// <summary>Roomier targets and spacing while retaining authored text size and models.</summary>
    Touch
}

public partial class RibbonControl
{
    private RibbonDensity _density;
    private bool _changingDensity;
    /// <summary>Gets or sets this ribbon's density, including library popups, QAT and Backstage navigation. Invalid focused ribbon input blocks changes. Accepted changes commit valid native edits, close transient popups, cancel gallery previews and retain owned native editors; application content and preference storage remain host-owned.</summary>
    [Category("Appearance"), DefaultValue(RibbonDensity.Compact), Bindable(true)]
    public RibbonDensity Density
    {
        get => _density;
        set
        {
            VerifyAccess(); if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value));
            if (_density == value || _changingDensity || _disposing) return;
            _changingDensity = true;
            try
            {
                var focus = FindForm() is { } form ? FocusedControl(form) : null;
                bool restore = ContainsFocus || _popups.ContainsFocus || _editors.ContainsFocus;
                var selection = focus switch { TextBox text => (text.SelectionStart, text.SelectionLength), ComboBox { DropDownStyle: ComboBoxStyle.DropDown } combo => (combo.SelectionStart, combo.SelectionLength), _ => (-1, 0) };
                if (!_editors.CommitFocused()) return;
                _screenTips.Cancel(); _popups.CloseAll(false); _editors.CloseLists();
                foreach (var gallery in AllElements.OfType<RibbonGalleryItem>().ToArray()) gallery.CancelPreview();
                ExitKeyTips(false);
                if (_disposing || IsDisposed) return;
                _density = value; ModelChanged();
                if (restore && !IsBackstageOpen && FindForm()?.ContainsFocus == true)
                {
                    if (focus is { IsDisposed: false, Visible: true, CanFocus: true }) focus.Focus();
                    else
                    {
                        var target = Snapshot.Targets.FirstOrDefault(t => ReferenceEquals(t.Element, _focused) && t.Enabled);
                        if (target?.Element is { } element) FocusElement(element);
                        else if (SelectedTab is { } selected) FocusElement(selected);
                    }
                    if (focus is { IsDisposed: false } && selection.Item1 >= 0)
                    {
                        int start = Math.Min(selection.Item1, focus.Text.Length), length = Math.Min(selection.Item2, focus.Text.Length - start);
                        if (focus is TextBox text) text.Select(start, length);
                        else if (focus is ComboBox combo) combo.Select(start, length);
                    }
                }
                DensityChanged?.Invoke(this, EventArgs.Empty);
            }
            finally { _changingDensity = false; }
        }
    }
    /// <summary>Occurs after an accepted density change and its geometry refresh.</summary>
    public event EventHandler? DensityChanged;
}
