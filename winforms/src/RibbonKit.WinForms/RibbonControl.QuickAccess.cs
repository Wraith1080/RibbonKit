using System.ComponentModel;
using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

public partial class RibbonControl
{
    private bool _quickAccessVisible = true;
    private RibbonQuickAccessPosition _quickAccessPosition;
    private int _quickAccessMaxWidth = 240;
    internal RibbonQuickAccessOverflow QuickAccessOverflow { get; private set; } = null!;

    /// <summary>Gets the owned toolbar declarations. References borrow commands; directly declared buttons and toggles belong only to this toolbar.</summary>
    [Category("Data"), DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    public RibbonQuickAccessCollection QuickAccessItems { get; }
    /// <summary>Gets or sets toolbar visibility without discarding its order or placement.</summary>
    [Category("Appearance"), Bindable(true), DefaultValue(true)]
    public bool QuickAccessVisible
    {
        get => _quickAccessVisible;
        set { VerifyAccess(); if (_quickAccessVisible == value) return; _quickAccessVisible = value; ModelChanged(); QuickAccessVisibleChanged?.Invoke(this, EventArgs.Empty); }
    }
    /// <summary>Occurs after toolbar visibility changes.</summary>
    public event EventHandler? QuickAccessVisibleChanged;
    /// <summary>Gets or sets the toolbar's ordinary-window placement.</summary>
    [Category("Appearance"), Bindable(true), DefaultValue(RibbonQuickAccessPosition.TabRow)]
    public RibbonQuickAccessPosition QuickAccessPosition
    {
        get => _quickAccessPosition;
        set { VerifyAccess(); if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value)); if (_quickAccessPosition == value) return; _quickAccessPosition = value; ModelChanged(); QuickAccessPositionChanged?.Invoke(this, EventArgs.Empty); }
    }
    /// <summary>Occurs after toolbar placement changes.</summary>
    public event EventHandler? QuickAccessPositionChanged;
    /// <summary>Gets or sets the logical width cap beside the tabs. The below-ribbon row uses the full available width.</summary>
    [Category("Layout"), DefaultValue(240)]
    public int QuickAccessMaxWidth
    {
        get => _quickAccessMaxWidth;
        set { VerifyAccess(); if (value < RibbonQuickAccess.ButtonWidth) throw new ArgumentOutOfRangeException(nameof(value)); if (_quickAccessMaxWidth == value) return; _quickAccessMaxWidth = value; ModelChanged(); }
    }
    internal bool HasQuickAccess => QuickAccessVisible && QuickAccessItems.Any(e => e.Visible);
    internal int QuickAccessHeight => HasQuickAccess && QuickAccessPosition == RibbonQuickAccessPosition.BelowRibbon ? RibbonLayoutEngine.Scale(Math.Max(RibbonDensityMetrics.For(Density).QuickAccessRow, LayoutTheme.TabHeight), DeviceDpi) : 0;
    internal void ActivateQuickAccess(RibbonElement entry, Control opener, Rectangle anchor, bool dropDownPart = false)
    {
        if (!RibbonQuickAccess.Available(entry)) return;
        if (entry is RibbonQuickAccessGroupReference group)
        {
            _popups.Toggle(group, opener, anchor);
            return;
        }
        if (entry is RibbonQuickAccessComboBoxReference reference)
        {
            ExitKeyTips(false);
            _editors.OpenQuickAccess(reference, opener, anchor);
            return;
        }
        if (entry is RibbonQuickAccessDropDownReference menu && (menu.Source is not RibbonSplitButtonItem || dropDownPart))
        {
            _popups.Toggle(menu, opener, anchor);
            return;
        }
        var command = RibbonQuickAccess.Command(entry)!;
        bool keepOverflow = opener is RibbonPopupSurface { OwnerElement: RibbonQuickAccessOverflow }
            && command is RibbonToggleItem or RibbonCheckBoxItem or RibbonRadioButtonItem or RibbonSplitButtonItem { CheckOnClick: true };
        if (keepOverflow) ((RibbonPopupSurface)opener).FocusElement(entry);
        command.PerformClick();
        if (!keepOverflow) _popups.CloseAll(false);
        ExitKeyTips(false);
    }
    private bool KeepQuickAccessGroupOpen(RibbonItem command, Control? opener)
    {
        var surface = opener as RibbonPopupSurface ?? _popups.Top;
        if (surface?.GroupSource is not { } group || !RibbonQuickAccess.RetainsGroup(group)
            || command is not (RibbonToggleItem or RibbonCheckBoxItem or RibbonRadioButtonItem or RibbonSplitButtonItem { CheckOnClick: true })) return false;
        surface.FocusElement(command);
        return true;
    }
}
