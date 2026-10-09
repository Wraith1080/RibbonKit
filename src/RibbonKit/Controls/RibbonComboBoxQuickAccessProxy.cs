using System.Collections.Specialized;
using System.Globalization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using static System.Windows.Controls.ContentControl;

namespace RibbonKit.Controls;

// The source owns items, selection, editable text and application handlers. Each
// projection owns its own popup and rows, including when the source tab is unrealized.
internal sealed class RibbonComboBoxQuickAccessProxy : RibbonDropDownButton
{
    private readonly RibbonComboBox _source;
    private bool _listening;
    private bool _refreshPending;
    private Popup? _popup;

    internal RibbonComboBoxQuickAccessProxy(RibbonComboBox source, Ribbon owner)
    {
        _source = source;
        // Restore the owning ribbon's direction whenever QAT placement reloads this
        // projection, including title-bar hosts outside the ribbon's visual tree.
        Loaded += (_, _) => SetBinding(FlowDirectionProperty, new Binding(nameof(FlowDirection)) { Source = owner });
        SetBinding(IconProperty, new Binding(nameof(source.Icon)) { Source = source });
        SetBinding(LargeIconProperty, new Binding(nameof(source.Icon)) { Source = source });
        var caption = new MultiBinding { Converter = CaptionConverter.Instance };
        caption.Bindings.Add(new Binding(nameof(source.Header)) { Source = source });
        caption.Bindings.Add(new Binding(nameof(source.ScreenTipTitle)) { Source = source });
        SetBinding(HeaderProperty, caption);
        SetBinding(DropDownHeaderProperty, new Binding(nameof(Header)) { Source = this });
        SetBinding(ScreenTipTitleProperty, new Binding(nameof(Header)) { Source = this });
        SetBinding(ScreenTipTextProperty, new Binding(nameof(source.ScreenTipText)) { Source = source });
        Unloaded += (_, _) =>
        {
            StopListening();
            SetCurrentValue(IsDropDownOpenProperty, false);
        };
    }

    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        if (e.Property == IsDropDownOpenProperty && _source is not null)
        {
            if (e.NewValue is true)
            {
                _source.SetCurrentValue(ComboBox.IsDropDownOpenProperty, false);
                RefreshRows();
                if (!_listening)
                {
                    CollectionChangedEventManager.AddHandler(_source.Items, OnItemsChanged);
                    _listening = true;
                }
            }
            else StopListening();
        }
        base.OnPropertyChanged(e);
    }

    public override void OnApplyTemplate()
    {
        if (_popup is not null) _popup.Opened -= OnPopupOpened;
        base.OnApplyTemplate();
        _popup = GetTemplateChild("PART_Popup") as Popup;
        if (_popup is not null) _popup.Opened += OnPopupOpened;
        if (Template.FindName("PART_PopupScrollViewer", this) is ScrollViewer viewer)
            viewer.SetBinding(MaxHeightProperty, new Binding(nameof(_source.MaxDropDownHeight)) { Source = _source });
    }

    private void OnPopupOpened(object? sender, EventArgs e) => RefreshPopupLayout();

    internal override bool TryFocusInitialMenuItem(bool last)
    {
        int index = _source.SelectedIndex;
        return !last && index >= 0 && index < Items.Count
            && Items[index] is RibbonComboBoxQuickAccessItem { IsEnabled: true, IsVisible: true } selected
            && selected.Focus();
    }

    private void RefreshPopupLayout()
    {
        if (_popup is { IsOpen: true, Child: { } child } && PresentationSource.FromVisual(child)?.RootVisual is UIElement root)
        {
            // Rebuilt rows can leave the popup's nonlogical decorators measured at
            // their earlier size. Remeasure the full popup branch before it paints.
            for (DependencyObject? current = child; current is not null; current = System.Windows.Media.VisualTreeHelper.GetParent(current))
                if (current is UIElement element) element.InvalidateMeasure();
            root.UpdateLayout();
        }
    }

    private void StopListening()
    {
        if (!_listening) return;
        CollectionChangedEventManager.RemoveHandler(_source.Items, OnItemsChanged);
        _listening = false;
    }

    private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_refreshPending) return;
        _refreshPending = true;
        Dispatcher.BeginInvoke(DispatcherPriority.DataBind, new Action(() =>
        {
            _refreshPending = false;
            if (IsDropDownOpen) RefreshRows();
        }));
    }

    private void RefreshRows()
    {
        object? focusedItem = (Keyboard.FocusedElement as RibbonComboBoxQuickAccessItem)?.SourceItem;
        Items.Clear();
        for (int index = 0; index < _source.Items.Count; index++)
        {
            object item = _source.Items[index];
            int selectionIndex = index;
            var row = new RibbonComboBoxQuickAccessItem { SourceItem = item };
            row.SetBinding(Ribbon.DensityProperty, new Binding
                { Source = this, Path = new PropertyPath(Ribbon.DensityProperty) });
            if (item is ComboBoxItem container)
            {
                row.SetBinding(ContentProperty, new Binding(nameof(container.Content))
                    { Source = container, Converter = VisualLabelConverter.Instance });
                SetPresentationBinding(row, ContentTemplateProperty, container, nameof(container.ContentTemplate), nameof(_source.ItemTemplate));
                SetPresentationBinding(row, ContentTemplateSelectorProperty, container, nameof(container.ContentTemplateSelector), nameof(_source.ItemTemplateSelector));
                SetPresentationBinding(row, ContentStringFormatProperty, container, nameof(container.ContentStringFormat), nameof(_source.ItemStringFormat));
            }
            else
            {
                row.Content = VisualLabelConverter.Label(item);
                SetPresentationBinding(row, ContentTemplateProperty, null, null, nameof(_source.ItemTemplate));
                SetPresentationBinding(row, ContentTemplateSelectorProperty, null, null, nameof(_source.ItemTemplateSelector));
                SetPresentationBinding(row, ContentStringFormatProperty, null, null, nameof(_source.ItemStringFormat));
            }
            if (item is UIElement element)
            {
                row.SetBinding(IsEnabledProperty, new Binding(nameof(IsEnabled)) { Source = element });
                row.SetBinding(VisibilityProperty, new Binding(nameof(Visibility)) { Source = element });
            }
            row.SetBinding(RibbonComboBoxQuickAccessItem.IsSelectedProperty,
                new Binding(nameof(_source.SelectedIndex)) { Source = _source,
                    Converter = SelectedIndexConverter.Instance, ConverterParameter = index });
            if (!string.IsNullOrEmpty(_source.DisplayMemberPath) && item is not ComboBoxItem)
                row.SetBinding(AutomationProperties.NameProperty, new Binding(_source.DisplayMemberPath) { Source = item });
            row.Click += (_, _) =>
            {
                if (IsEnabled && row.IsEnabled && selectionIndex < _source.Items.Count
                    && Equals(_source.Items[selectionIndex], item))
                    _source.SetCurrentValue(Selector.SelectedIndexProperty, selectionIndex);
            };
            Items.Add(row);
        }
        if (focusedItem is not null)
            Items.OfType<RibbonComboBoxQuickAccessItem>().FirstOrDefault(row => ReferenceEquals(row.SourceItem, focusedItem) && row.IsEnabled)?.Focus();
        RefreshPopupLayout();
    }

    private void SetPresentationBinding(RibbonComboBoxQuickAccessItem row, DependencyProperty property,
        ComboBoxItem? container, string? containerPath, string sourcePath)
    {
        var binding = new MultiBinding { Converter = new PresentationConverter(property, container is not null) };
        if (container is not null) binding.Bindings.Add(new Binding(containerPath) { Source = container });
        binding.Bindings.Add(new Binding(sourcePath) { Source = _source });
        if (property == ContentTemplateProperty)
        {
            binding.Bindings.Add(new Binding(nameof(_source.DisplayMemberPath)) { Source = _source });
            binding.Bindings.Add(new Binding(nameof(_source.ItemTemplateSelector)) { Source = _source });
            if (container is not null)
                binding.Bindings.Add(new Binding(nameof(container.ContentTemplateSelector)) { Source = container });
        }
        row.SetBinding(property, binding);
    }

    private sealed class PresentationConverter(DependencyProperty property, bool hasContainer) : IMultiValueConverter
    {
        private string? _path;
        private DataTemplate? _template;
        public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            bool templateProperty = property == ContentTemplateProperty;
            int extraValues = templateProperty ? (hasContainer ? 3 : 2) : 0;
            // An authored selector takes precedence over the combo's fallback template.
            if (templateProperty && hasContainer && values[0] is not DataTemplate && values[^1] is DataTemplateSelector)
                return null;
            int presentationCount = values.Length - extraValues;
            for (int i = 0; i < presentationCount; i++)
                if (values[i] is { } value && value != DependencyProperty.UnsetValue) return value;
            if (!templateProperty || values[^(extraValues - 1)] is DataTemplateSelector
                || values[^extraValues] is not string { Length: > 0 } path) return null;
            if (_template is null || _path != path)
            {
                _path = path;
                var text = new FrameworkElementFactory(typeof(TextBlock));
                text.SetBinding(TextBlock.TextProperty, new Binding(path));
                _template = new DataTemplate { VisualTree = text };
            }
            return _template;
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotSupportedException();
    }

    private sealed class SelectedIndexConverter : IValueConverter
    {
        internal static readonly SelectedIndexConverter Instance = new();
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => Equals(value, parameter);
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
    }

    private sealed class CaptionConverter : IMultiValueConverter
    {
        internal static readonly CaptionConverter Instance = new();
        public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture) =>
            values[0] is string { Length: > 0 } header ? header : values[1] as string;
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotSupportedException();
    }

    private sealed class VisualLabelConverter : IValueConverter
    {
        internal static readonly VisualLabelConverter Instance = new();
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture) => Label(value);
        internal static object? Label(object? value)
        {
            // UI elements have one parent. Data and templates can be rendered independently;
            // directly authored visuals use their accessible text instead of being reparented.
            if (value is not UIElement element) return value;
            string text = TextSearch.GetText(element);
            if (!string.IsNullOrEmpty(text)) return text;
            text = AutomationProperties.GetName(element);
            if (!string.IsNullOrEmpty(text)) return text;
            return value switch
            {
                TextBlock block => block.Text,
                AccessText access => access.Text,
                ContentControl content => Label(content.Content),
                Panel panel => string.Join(" ", panel.Children.Cast<UIElement>().Select(Label).Where(label => label is not null)),
                _ => value.ToString(),
            };
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
    }
}

internal sealed class RibbonComboBoxQuickAccessItem : Button
{
    internal object? SourceItem { get; init; }
    public static readonly DependencyProperty IsSelectedProperty =
        DependencyProperty.Register(nameof(IsSelected), typeof(bool), typeof(RibbonComboBoxQuickAccessItem), new FrameworkPropertyMetadata(false));
    public bool IsSelected { get => (bool)GetValue(IsSelectedProperty); set => SetValue(IsSelectedProperty, value); }
    static RibbonComboBoxQuickAccessItem() => DefaultStyleKeyProperty.OverrideMetadata(
        typeof(RibbonComboBoxQuickAccessItem), new FrameworkPropertyMetadata(typeof(RibbonComboBoxQuickAccessItem)));

    protected override void OnMouseEnter(MouseEventArgs e)
    {
        base.OnMouseEnter(e);
        // Choice rows use keyboard focus for menu navigation. Move that active
        // focus with pointer hover so the opening row cannot keep a second highlight.
        if (IsEnabled && !IsKeyboardFocusWithin) Focus();
    }
}
