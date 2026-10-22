using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;

namespace RibbonKit.Controls;

// An independent native editor for the original Simplified slot. Application
// bindings, selection objects and authored item containers stay on the source.
internal sealed class RibbonComboBoxSimplifiedProxy : RibbonComboBox
{
    private readonly RibbonComboBox _source;
    private bool _listening;
    private bool _refreshPending;
    private bool _refreshing = true;
    private bool _updatingSource;
    private bool _overflowFocusPending;
    private IInputElement? _lastFocus;
    private IInputElement? _modeFallback;
    private INotifyCollectionChanged? _groupDescriptions;

    internal RibbonComboBoxSimplifiedProxy(RibbonComboBox source)
    {
        _source = source;
        foreach (DependencyProperty property in new[]
        {
            HeaderProperty, IconProperty, InputWidthProperty, IsEditableProperty, IsReadOnlyProperty,
            StaysOpenOnEditProperty, IsTextSearchEnabledProperty, IsTextSearchCaseSensitiveProperty,
            MaxDropDownHeightProperty, DisplayMemberPathProperty, ItemContainerStyleProperty,
            ItemContainerStyleSelectorProperty, GroupStyleSelectorProperty, ItemTemplateProperty,
            ItemTemplateSelectorProperty, ItemStringFormatProperty, TextSearch.TextPathProperty,
            ScreenTipTitleProperty, ScreenTipTextProperty, DataContextProperty, StyleProperty, TemplateProperty,
            FontFamilyProperty, FontSizeProperty, FontWeightProperty, FontStyleProperty, ForegroundProperty, LanguageProperty,
        }) SetBinding(property, new Binding { Source = source, Path = new PropertyPath(property), Mode = BindingMode.OneWay });
        IsSynchronizedWithCurrentItem = false;
        Loaded += OnLoaded;
        Unloaded += (_, _) => StopListening();
        RefreshRows();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (!_listening)
        {
            _listening = true;
            CollectionChangedEventManager.AddHandler(_source.Items, OnItemsChanged);
            CollectionChangedEventManager.AddHandler(_source.GroupStyle, OnItemsChanged);
            WeakEventManager<ComboBox, EventArgs>.AddHandler(_source, nameof(DropDownOpened), OnSourceDropDownOpened);
        }
        RefreshRows();
    }

    private void StopListening()
    {
        if (!_listening) return;
        CollectionChangedEventManager.RemoveHandler(_source.Items, OnItemsChanged);
        CollectionChangedEventManager.RemoveHandler(_source.GroupStyle, OnItemsChanged);
        WeakEventManager<ComboBox, EventArgs>.RemoveHandler(_source, nameof(DropDownOpened), OnSourceDropDownOpened);
        if (_groupDescriptions is not null)
            CollectionChangedEventManager.RemoveHandler(_groupDescriptions, OnItemsChanged);
        _groupDescriptions = null;
        _listening = false;
        SetCurrentValue(IsDropDownOpenProperty, false);
    }

    internal void Release()
    {
        var focused = IsKeyboardFocusWithin ? Keyboard.FocusedElement
            : Ribbon.GetLayoutMode(this) == RibbonLayoutMode.Classic ? _lastFocus : null;
        var fallback = _modeFallback;
        StopListening();
        PauseSelection();
        if (focused is not null)
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
            {
                if (_source.IsVisible && _source.IsEnabled && (Keyboard.FocusedElement is null || ReferenceEquals(Keyboard.FocusedElement, focused)
                    || fallback is not null && ReferenceEquals(Keyboard.FocusedElement, fallback)))
                    _source.Focus();
            }));
    }

    internal void OnOverflow(UIElement? target)
    {
        bool focused = IsKeyboardFocusWithin;
        SetCurrentValue(IsDropDownOpenProperty, false);
        if (!focused || target is null || _overflowFocusPending) return;
        _overflowFocusPending = true;
        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
        {
            _overflowFocusPending = false;
            if (target.IsVisible && target.IsEnabled && (IsKeyboardFocusWithin || Keyboard.FocusedElement is null)) target.Focus();
        }));
    }

    protected override void OnDropDownOpened(EventArgs e)
    {
        _source.SetCurrentValue(IsDropDownOpenProperty, false);
        base.OnDropDownOpened(e);
    }

    private void OnSourceDropDownOpened(object? sender, EventArgs e) => SetCurrentValue(IsDropDownOpenProperty, false);

    private void PauseSelection()
    {
        _refreshing = true;
        BindingOperations.ClearBinding(this, TextProperty);
        BindingOperations.ClearBinding(this, SelectedIndexProperty);
    }

    protected override void OnSelectionChanged(SelectionChangedEventArgs e)
    {
        bool updating = _updatingSource;
        _updatingSource = true;
        try { base.OnSelectionChanged(e); }
        finally { _updatingSource = updating; }
        UpdateSource(SelectedIndexProperty);
    }

    protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
    {
        base.OnGotKeyboardFocus(e);
        _lastFocus = Keyboard.FocusedElement;
    }

    protected override void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e)
    {
        base.OnLostKeyboardFocus(e);
        // Hiding the Simplified presenter sends WPF focus to its tab control.
        // Remember only that fallback, never an unrelated later user choice.
        if (Ribbon.GetLayoutMode(this) == RibbonLayoutMode.Classic) _modeFallback = e.NewFocus as RibbonTabControl;
        else if (!IsKeyboardFocusWithin) _lastFocus = null;
    }

    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.Property == TextProperty) UpdateSource(TextProperty);
    }

    private void UpdateSource(DependencyProperty property)
    {
        // Explicit commits keep row rebuilds and selection-driven text changes
        // from publishing a transient empty value or selecting a duplicate label.
        if (_refreshing || _updatingSource) return;
        _updatingSource = true;
        try { GetBindingExpression(property)?.UpdateSource(); }
        finally { _updatingSource = false; }
    }

    private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // A source collection change may adjust its selection before our rows
        // are rebuilt. Do not coerce that index against an outdated choice list.
        PauseSelection();
        if (_refreshPending) return;
        _refreshPending = true;
        Dispatcher.BeginInvoke(DispatcherPriority.DataBind, new Action(() =>
        {
            _refreshPending = false;
            if (_listening) RefreshRows();
        }));
    }

    private void RefreshRows()
    {
        PauseSelection();
        int selected = _source.SelectedIndex;
        string text = _source.Text;
        SetCurrentValue(ItemsSourceProperty, null);
        Items.Clear();
        var rows = new List<ComboBoxItem>();
        var pathsByRow = new Dictionary<object, object?[]>();
        using var paths = RibbonComboBoxQuickAccessProxy.GroupPaths(_source.Items.Groups, Array.Empty<object?>()).GetEnumerator();
        foreach (object item in _source.Items)
        {
            var row = new ComboBoxItem();
            RibbonComboBoxQuickAccessProxy.BindChoicePresentation(row, _source, item);
            if (item is not UIElement) row.DataContext = item;
            string path = TextSearch.GetTextPath(_source);
            if (string.IsNullOrEmpty(path)) path = _source.DisplayMemberPath;
            if (item is ComboBoxItem container && !string.IsNullOrEmpty(TextSearch.GetText(container)))
                row.SetBinding(TextSearch.TextProperty, new Binding { Source = container, Path = new PropertyPath(TextSearch.TextProperty), Mode = BindingMode.OneWay });
            else row.SetBinding(TextSearch.TextProperty, new Binding(string.IsNullOrEmpty(path) ? nameof(ContentControl.Content) : $"Content.{path}")
                { Source = row, Converter = ChoiceTextConverter.Instance, Mode = BindingMode.OneWay });
            rows.Add(row);
            pathsByRow[row] = paths.MoveNext() ? paths.Current : Array.Empty<object?>();
        }
        GroupStyle.Clear();
        foreach (GroupStyle style in _source.GroupStyle) GroupStyle.Add(style);
        DropDownGrouping.ApplyDefault(this);
        if (_source.Items.GroupDescriptions is { Count: > 0 } descriptions)
        {
            var view = new ListCollectionView(rows);
            if (_source.Items.Culture is { } culture) view.Culture = culture;
            using (view.DeferRefresh())
                foreach (GroupDescription original in descriptions)
                {
                    var projected = new RibbonComboBoxQuickAccessProxy.ChoiceGroupDescription(original,
                        (row, level) => pathsByRow[row].ElementAtOrDefault(level)) { CustomSort = original.CustomSort };
                    foreach (object name in original.GroupNames) projected.GroupNames.Add(name);
                    foreach (SortDescription sort in original.SortDescriptions) projected.SortDescriptions.Add(sort);
                    view.GroupDescriptions.Add(projected);
                }
            SetCurrentValue(ItemsSourceProperty, view);
        }
        else foreach (var row in rows) Items.Add(row);
        SetCurrentValue(SelectedIndexProperty, selected);
        SetCurrentValue(TextProperty, text);
        SetBinding(SelectedIndexProperty, new Binding(nameof(SelectedIndex)) { Source = _source, Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.Explicit });
        SetBinding(TextProperty, new Binding(nameof(Text)) { Source = _source, Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.Explicit });
        _refreshing = false;
        if (_listening && !ReferenceEquals(_groupDescriptions, _source.Items.GroupDescriptions))
        {
            if (_groupDescriptions is not null)
                CollectionChangedEventManager.RemoveHandler(_groupDescriptions, OnItemsChanged);
            _groupDescriptions = _source.Items.GroupDescriptions;
            if (_groupDescriptions is not null)
                CollectionChangedEventManager.AddHandler(_groupDescriptions, OnItemsChanged);
        }
    }

    private sealed class ChoiceTextConverter : IValueConverter
    {
        internal static readonly ChoiceTextConverter Instance = new();
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            RibbonComboBoxQuickAccessProxy.ChoiceLabel(value)?.ToString() ?? string.Empty;
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
    }
}
