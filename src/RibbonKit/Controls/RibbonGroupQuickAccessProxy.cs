using System.Globalization;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace RibbonKit.Controls;

// The original template content travels as it does in the collapsed-group flyout.
// Items, logical ownership, bindings, resources and application handlers stay put.
internal sealed class RibbonGroupQuickAccessProxy : RibbonDropDownButton
{
    private static readonly DependencyProperty FallbackIconProperty = DependencyProperty.Register(
        "FallbackIcon", typeof(ImageSource), typeof(RibbonGroupQuickAccessProxy));
    private readonly RibbonGroup _source;
    private readonly Ribbon _owner;
    private readonly Border _host;
    private bool _returning;
    private bool _routingCommand;
    private int _opening;

    internal RibbonGroupQuickAccessProxy(RibbonGroup source, Ribbon owner)
    {
        _source = source;
        _owner = owner;
        _host = new GroupHost(source);
        SetResourceReference(FallbackIconProperty, "RibbonKit.Images.QuickAccessGroup");
        var icon = new MultiBinding { Converter = IconConverter.Instance };
        icon.Bindings.Add(new Binding(nameof(source.Icon)) { Source = source });
        icon.Bindings.Add(new Binding { Source = this, Path = new PropertyPath(FallbackIconProperty) });
        SetBinding(IconProperty, icon);
        SetBinding(LargeIconProperty, new Binding(nameof(Icon)) { Source = this });
        SetBinding(HeaderProperty, new Binding(nameof(source.Header)) { Source = source });
        SetBinding(ScreenTipTitleProperty, new Binding(nameof(Header)) { Source = this });
        _host.SetBinding(DataContextProperty, new Binding(nameof(source.DataContext)) { Source = source });
        Items.Add(_host);
        KeyboardNavigation.SetTabNavigation(_host, KeyboardNavigationMode.Cycle);
        KeyboardNavigation.SetDirectionalNavigation(_host, KeyboardNavigationMode.Cycle);
        _host.PreviewKeyDown += OnGroupKeyDown;
        _host.AddHandler(CommandManager.CanExecuteEvent, new CanExecuteRoutedEventHandler(OnCanExecute));
        _host.AddHandler(CommandManager.ExecutedEvent, new ExecutedRoutedEventHandler(OnExecuted));
        Loaded += (_, _) => SetBinding(FlowDirectionProperty, new Binding(nameof(FlowDirection)) { Source = owner });
        Unloaded += (_, _) => EnsureBorrowedItemsReturned();
        IsEnabledChanged += (_, _) => { if (!IsEnabled) EnsureBorrowedItemsReturned(); };
    }

    internal UIElement? GroupContent => _host.Child;

    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        if (e.Property == IsDropDownOpenProperty && _source is not null)
        {
            if (e.NewValue is true)
            {
                _opening++;
                if (!_source.BeginQuickAccess(_host, _owner, EnsureBorrowedItemsReturned))
                    Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(EnsureBorrowedItemsReturned));
            }
            else
            {
                bool returnFocus = _host.IsKeyboardFocusWithin;
                ReturnContent();
                if (returnFocus && !WasPointerDismissed) Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() =>
                {
                    if (!IsDropDownOpen && _source.QuickAccessContent is null && _source.FlyoutContent is null
                        && (Keyboard.FocusedElement is null || _source.IsKeyboardFocusWithin))
                        (GetTemplateChild("PART_Toggle") as UIElement)?.Focus();
                }));
            }
        }
        base.OnPropertyChanged(e);
    }

    public override void OnApplyTemplate()
    {
        EnsureBorrowedItemsReturned();
        base.OnApplyTemplate();
    }

    internal override void EnsureBorrowedItemsReturned()
    {
        SetCurrentValue(IsDropDownOpenProperty, false);
        ReturnContent();
    }

    internal void CloseSource() => _source.CloseQuickAccess();

    private void ReturnContent()
    {
        if (_returning) return;
        _returning = true;
        try { _source.EndQuickAccess(_host); }
        finally { _returning = false; }
    }

    internal override bool TryFocusInitialMenuItem(bool last) =>
        _host.MoveFocus(new TraversalRequest(last ? FocusNavigationDirection.Last : FocusNavigationDirection.First));

    internal override void OnMenuItemClicked(object sender, RoutedEventArgs e)
    {
        if (RibbonGroup.KeepsFlyoutOpen(e.OriginalSource)) return;
        // Finish command and nested-popup dispatch before returning the template.
        int opening = _opening;
        Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
        {
            if (opening == _opening) EnsureBorrowedItemsReturned();
        }));
    }

    private void OnGroupKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Handled || Keyboard.Modifiers != ModifierKeys.None || Keyboard.FocusedElement is not ButtonBase button) return;
        if (e.Key is Key.Left or Key.Right)
            e.Handled = button.MoveFocus(new TraversalRequest(e.Key == Key.Left ? FocusNavigationDirection.Left : FocusNavigationDirection.Right));
    }

    internal override void OnMenuKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is Key.Down or Key.Up && Keyboard.FocusedElement is FrameworkElement
            { TemplatedParent: RibbonDropDownButton { IsDropDownOpen: false } nested }
            && !ReferenceEquals(nested, this)) return;
        base.OnMenuKeyDown(sender, e);
    }

    // A title-bar QAT popup's visual route does not include the original group
    // or ribbon. Let child/editor bindings run first, then finish unresolved
    // routed commands through the source group's normal route.
    private void OnCanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        if (_routingCommand || e.Command is not RoutedCommand command) return;
        _routingCommand = true;
        try { e.CanExecute = command.CanExecute(e.Parameter, _source); e.Handled = true; }
        finally { _routingCommand = false; }
    }

    private void OnExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        if (_routingCommand || e.Command is not RoutedCommand command) return;
        _routingCommand = true;
        try { e.Handled = true; command.Execute(e.Parameter, _source); }
        finally { _routingCommand = false; }
    }

    private sealed class GroupHost(RibbonGroup source) : Border
    {
        protected override AutomationPeer OnCreateAutomationPeer() => new GroupHostPeer(this, source);
    }

    private sealed class GroupHostPeer(GroupHost owner, RibbonGroup source) : FrameworkElementAutomationPeer(owner)
    {
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;
        protected override string GetNameCore() => source.Header?.ToString() ?? base.GetNameCore();
    }

    private sealed class IconConverter : IMultiValueConverter
    {
        internal static readonly IconConverter Instance = new();
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) =>
            values.OfType<ImageSource>().FirstOrDefault() ?? DependencyProperty.UnsetValue;
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotSupportedException();
    }
}
