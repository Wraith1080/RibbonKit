using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Threading;
using RibbonKit.Localization;

namespace RibbonKit.Controls;

// The public customization page owns the operation; the shared modal only chooses
// its target/position and reuses the edit dialog's chrome, close and result handling.
[TemplatePart(Name = "PART_PositionSelector", Type = typeof(Selector))]
[TemplatePart(Name = "PART_TargetTabSelector", Type = typeof(Selector))]
internal sealed class RibbonCustomizeMoveTabDialog : RibbonCustomizeEditDialog
{
    private Selector? _position;
    private Selector? _target;
    private ButtonBase? _ok;
    private bool _after;
    private RibbonTab? _targetTab;

    static RibbonCustomizeMoveTabDialog() => DefaultStyleKeyProperty.OverrideMetadata(
        typeof(RibbonCustomizeMoveTabDialog), new FrameworkPropertyMetadata(typeof(RibbonCustomizeMoveTabDialog)));

    internal RibbonCustomizeMoveTabDialog()
    {
        Height = 240;
        SetBinding(TitleProperty, new Binding("[MoveTab]") { Source = RibbonLocalizationBindingSource.Instance });
        Loaded += (_, _) => Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() => { if (IsVisible) _position?.Focus(); }));
    }

    internal IReadOnlyList<SimplifiedTabChoice> Targets { get; init; } = Array.Empty<SimplifiedTabChoice>();
    internal bool After { get => (_position?.SelectedItem as MovePositionChoice)?.After ?? _after; init => _after = value; }
    internal RibbonTab? Target { get => (_target?.SelectedItem as SimplifiedTabChoice)?.Tab ?? _targetTab; init => _targetTab = value; }

    public override void OnApplyTemplate()
    {
        _after = After; _targetTab = Target;
        if (_position is not null) _position.SelectionChanged -= OnSelectionChanged;
        if (_target is not null) _target.SelectionChanged -= OnSelectionChanged;
        base.OnApplyTemplate();
        _position = GetTemplateChild("PART_PositionSelector") as Selector;
        _target = GetTemplateChild("PART_TargetTabSelector") as Selector;
        _ok = GetTemplateChild("PART_OkButton") as ButtonBase;
        if (_position is not null)
        {
            _position.ItemsSource = new[] { new MovePositionChoice(false), new MovePositionChoice(true) };
            _position.SelectedItem = _position.Items.Cast<MovePositionChoice>().First(choice => choice.After == _after);
            _position.SelectionChanged += OnSelectionChanged;
        }
        if (_target is not null)
        {
            _target.ItemsSource = Targets;
            _target.SelectedItem = Targets.FirstOrDefault(choice => choice.Tab == _targetTab) ?? Targets.FirstOrDefault();
            _target.SelectionChanged += OnSelectionChanged;
        }
        UpdateOk();
    }

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateOk();
    private void UpdateOk() => _ok?.SetCurrentValue(IsEnabledProperty, _position?.SelectedItem is MovePositionChoice && _target?.SelectedItem is SimplifiedTabChoice);

    private sealed class MovePositionChoice : DependencyObject
    {
        private static readonly DependencyProperty DisplayNameProperty = DependencyProperty.Register(
            nameof(DisplayName), typeof(string), typeof(MovePositionChoice), new PropertyMetadata(string.Empty));
        internal MovePositionChoice(bool after)
        {
            After = after;
            BindingOperations.SetBinding(this, DisplayNameProperty, new Binding(after ? "[After]" : "[Before]") { Source = RibbonLocalizationBindingSource.Instance });
        }
        internal bool After { get; }
        public string DisplayName => (string?)GetValue(DisplayNameProperty) ?? string.Empty;
    }
}
