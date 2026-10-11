using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;

namespace RibbonKit.Controls;

// The shared dropdown chrome supplies sizing, KeyTips and ExpandCollapse UIA.
// Only the SOURCE group's popup opens; this opener never owns a second content view.
internal sealed class RibbonGroupQuickAccessProxy : RibbonDropDownButton
{
    private readonly RibbonGroup _source;
    private readonly Ribbon _owner;
    internal RibbonGroup Source => _source;
    internal override bool OwnsPopup => false;

    internal RibbonGroupQuickAccessProxy(RibbonGroup source, Ribbon owner)
    {
        _source = source;
        _owner = owner;
        SetBinding(IconProperty, new Binding(nameof(source.Icon)) { Source = source });
        SetBinding(LargeIconProperty, new Binding(nameof(source.Icon)) { Source = source });
        SetBinding(HeaderProperty, new Binding(nameof(source.Header)) { Source = source });
        SetBinding(ScreenTipTitleProperty, new Binding(nameof(Header)) { Source = this });
        SetBinding(FlowDirectionProperty, new Binding(nameof(owner.FlowDirection)) { Source = owner });
        Loaded += (_, _) => SetBinding(FlowDirectionProperty, new Binding(nameof(owner.FlowDirection)) { Source = owner });
        Unloaded += (_, _) => _source.EndQuickAccess(this);
        IsEnabledChanged += (_, _) => { if (!IsEnabled) _source.EndQuickAccess(this); };
    }

    public override void OnApplyTemplate()
    {
        _source.EndQuickAccess(this);
        if (GetTemplateChild("PART_Popup") is Popup unused)
        {
            // Replace the template's two-way expression: clearing/assigning it
            // can leave it tracking the opener and displaying an empty menu.
            BindingOperations.SetBinding(unused, Popup.IsOpenProperty,
                new Binding { Source = false, Mode = BindingMode.OneWay });
        }
        base.OnApplyTemplate();
    }

    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.Property != IsDropDownOpenProperty || _source is null) return;
        if (e.NewValue is true)
        {
            if (!_source.BeginQuickAccess(this, _owner))
                SetCurrentValue(IsDropDownOpenProperty, false);
        }
        else _source.CloseQuickAccess(this);
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);
        if (!e.Handled && e.Key is Key.Down or Key.Up && !IsDropDownOpen)
        {
            _source.FocusLastQuickAccessItem = e.Key == Key.Up;
            SetCurrentValue(IsDropDownOpenProperty, true);
            e.Handled = true;
        }
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new GroupPeer(this);

    private sealed class GroupPeer(RibbonGroupQuickAccessProxy owner)
        : RibbonKit.Automation.RibbonDropDownButtonAutomationPeer(owner)
    {
        protected override List<AutomationPeer>? GetChildrenCore() =>
            owner.IsDropDownOpen && owner._source.QuickAccessContent is { } content
                ? new FrameworkElementAutomationPeer(content).GetChildren() : base.GetChildrenCore();
    }
}
