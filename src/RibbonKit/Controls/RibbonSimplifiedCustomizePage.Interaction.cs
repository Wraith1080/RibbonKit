using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using RibbonKit.Localization;

namespace RibbonKit.Controls;

public partial class RibbonSimplifiedCustomizePage
{
    private enum CustomizeMenu { New, Edit, Reset }
    private ContextMenu? _actionMenu;
    private ListBox? _dragList;
    private Point? _pressPoint;
    private SimplifiedCustomizeEntry? _pressedEntry;
    private Point _dragPoint;
    private InsertionAdorner? _insertion;
    private DispatcherTimer? _dragScroll;
    private int _dragGeneration;
    private sealed record CommandDrag(RibbonSimplifiedCustomizePage Page, RibbonTab Tab, FrameworkElement Source, RibbonGroup Group, int Generation);

    private void CloseActions()
    {
        if (_actionMenu is { } menu) { _actionMenu = null; menu.IsOpen = false; }
    }

    private void OpenActions(string part, CustomizeMenu kind)
    {
        if (!IsEnabled || GetTemplateChild(part) is not ButtonBase button) return;
        CloseActions();
        var menu = new ContextMenu { PlacementTarget = button, Placement = PlacementMode.Bottom, FlowDirection = FlowDirection };
        Controls.Ribbon.ApplyModernMenuStyle(menu);
        Controls.Ribbon.SetDensity(menu, Ribbon?.Density ?? RibbonDensity.Compact);
        MenuItem AddAction(RibbonString key, Action action, bool enabled)
        {
            var item = new MenuItem { IsEnabled = enabled };
            item.SetBinding(HeaderedItemsControl.HeaderProperty, new Binding($"[{key}]") { Source = RibbonLocalizationBindingSource.Instance });
            item.Click += (_, _) => action(); menu.Items.Add(item); return item;
        }
        switch (kind)
        {
            case CustomizeMenu.New:
                AddAction(RibbonString.NewTab, NewTab, Ribbon is not null);
                AddAction(RibbonString.NewGroup, NewGroup, _tab is not null);
                break;
            case CustomizeMenu.Edit:
                AddAction(RibbonString.RenameTab, () => Rename(_tab), _tab is not null);
                AddAction(RibbonString.RenameGroup, () => Rename(_group), _group is not null);
                AddAction(RibbonString.MoveTab, MoveTab, _tab is not null && Controls.Ribbon.GetIsCustom(_tab) && DraftTabs().Count > 1);
                if (_current?.SelectedItem is SimplifiedCustomizeEntry entry)
                {
                    menu.Items.Add(new Separator());
                    var label = AddAction(RibbonString.ShowItemLabel, () => { }, true);
                    label.IsCheckable = true; label.IsChecked = entry.ShowLabel;
                    label.Checked += (_, _) => entry.ShowLabel = true;
                    label.Unchecked += (_, _) => entry.ShowLabel = false;
                }
                break;
            case CustomizeMenu.Reset:
                AddAction(RibbonString.ResetTab, () => Reset(false), _tab is not null);
                AddAction(RibbonString.ResetAllTabs, () => Reset(true), Ribbon is not null);
                AddResetUtilities(menu, AddAction);
                break;
        }
        _actionMenu = menu;
        button.ContextMenu = menu;
        bool escape = false;
        menu.PreviewKeyDown += (_, e) => { if (e.Key == Key.Escape) escape = true; };
        menu.Opened += (_, _) => Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() =>
        {
            if (_actionMenu == menu && menu.IsOpen)
                menu.Items.OfType<MenuItem>().FirstOrDefault(item => item.IsEnabled)?.Focus();
        }));
        menu.Closed += (_, _) =>
        {
            if (_actionMenu != menu) return;
            _actionMenu = null;
            if (escape && IsLoaded && button.IsVisible) button.Focus();
        };
        menu.IsOpen = true;
    }

    private void RevealSelection()
    {
        if (_current?.SelectedItem is not { } selected) return;
        _current.ScrollIntoView(selected);
        _current.UpdateLayout();
        (_current.ItemContainerGenerator.ContainerFromItem(selected) as ListBoxItem)?.Focus();
    }

    private void HookReordering()
    {
        CloseActions(); ClearInsertion();
        if (_dragList is not null)
        {
            _dragList.PreviewMouseLeftButtonDown -= OnReorderPress;
            _dragList.PreviewMouseMove -= OnReorderMove;
            _dragList.PreviewMouseLeftButtonUp -= OnReorderRelease;
            _dragList.PreviewDragOver -= OnReorderOver;
            _dragList.PreviewDragLeave -= OnReorderLeave;
            _dragList.PreviewDrop -= OnReorderDrop;
        }
        _pressPoint = null; _pressedEntry = null;
        _dragGeneration++;
        _dragList = _current;
        if (_dragList is null) return;
        _dragList.AllowDrop = _groups is null;
        _dragList.PreviewMouseLeftButtonDown += OnReorderPress;
        _dragList.PreviewMouseMove += OnReorderMove;
        _dragList.PreviewMouseLeftButtonUp += OnReorderRelease;
        _dragList.PreviewDragOver += OnReorderOver;
        _dragList.PreviewDragLeave += OnReorderLeave;
        _dragList.PreviewDrop += OnReorderDrop;
    }

    private void OnReorderPress(object sender, MouseButtonEventArgs e)
    {
        _pressPoint = null; _pressedEntry = null;
        if (!IsEnabled || _groups is not null || _current is null || e.OriginalSource is not DependencyObject target) return;
        if (ItemsControl.ContainerFromElement(_current, target) is ListBoxItem { Content: SimplifiedCustomizeEntry entry })
        { _pressPoint = e.GetPosition(_current); _pressedEntry = entry; }
    }
    private void OnReorderRelease(object sender, MouseButtonEventArgs e) { _pressPoint = null; _pressedEntry = null; }
    private void OnReorderMove(object sender, MouseEventArgs e)
    {
        if (!IsEnabled || e.LeftButton != MouseButtonState.Pressed || _pressPoint is not { } start || _pressedEntry is not { } entry || _current is null || _tab is null) return;
        Point point = e.GetPosition(_current);
        if (Math.Abs(point.X - start.X) < SystemParameters.MinimumHorizontalDragDistance && Math.Abs(point.Y - start.Y) < SystemParameters.MinimumVerticalDragDistance) return;
        _pressPoint = null; _pressedEntry = null;
        try { DragDrop.DoDragDrop(_current, new DataObject(typeof(CommandDrag), new CommandDrag(this, _tab, entry.Control, entry.Group, _dragGeneration)), DragDropEffects.Move); }
        finally { ClearInsertion(); }
    }
    private bool AcceptDrag(DragEventArgs e, out CommandDrag? drag)
    {
        drag = e.Data.GetDataPresent(typeof(CommandDrag)) ? e.Data.GetData(typeof(CommandDrag)) as CommandDrag : null;
        var candidate = drag;
        return IsEnabled && _groups is null && _current is not null && candidate is not null && candidate.Page == this && candidate.Tab == _tab && candidate.Generation == _dragGeneration
            && Choices().Any(choice => SameChoice(choice, candidate.Source, candidate.Group));
    }
    private void OnReorderOver(object sender, DragEventArgs e)
    {
        e.Handled = true; e.Effects = DragDropEffects.None;
        if (!AcceptDrag(e, out _)) { ClearInsertion(); return; }
        e.Effects = DragDropEffects.Move; _dragPoint = e.GetPosition(_current);
        ShowInsertion();
        _dragScroll ??= new DispatcherTimer(TimeSpan.FromMilliseconds(60), DispatcherPriority.Input, (_, _) => ScrollDrag(), Dispatcher);
        _dragScroll.Start();
    }
    private void OnReorderLeave(object sender, DragEventArgs e) => ClearInsertion();
    private void OnReorderDrop(object sender, DragEventArgs e)
    {
        e.Handled = true; e.Effects = DragDropEffects.None;
        if (AcceptDrag(e, out var drag))
        {
            _dragPoint = e.GetPosition(_current);
            int slot = DropSlot(out _);
            if (ReorderCommand(drag!.Source, drag.Group, slot)) e.Effects = DragDropEffects.Move;
        }
        ClearInsertion();
    }

    internal bool ReorderCommand(FrameworkElement source, RibbonGroup group, int insertionSlot)
    {
        if (!IsEnabled || _current is null) return false;
        var choices = Choices();
        int from = choices.FindIndex(choice => SameChoice(choice, source, group));
        if (from < 0 || insertionSlot < 0 || insertionSlot > choices.Count) return false;
        int to = insertionSlot > from ? insertionSlot - 1 : insertionSlot;
        if (to != from)
        {
            var choice = choices[from]; choices.RemoveAt(from); choices.Insert(to, choice); SaveDraft(choices);
            RefreshLists(source, group);
        }
        RevealSelection(); return true;
    }
    private int DropSlot(out double y)
    {
        int commands = 0; y = 0;
        if (_current is null) return commands;
        for (int i = 0; i < _current.Items.Count; i++)
        {
            if (_current.ItemContainerGenerator.ContainerFromIndex(i) is ListBoxItem row)
            {
                Rect bounds = row.TransformToAncestor(_current).TransformBounds(new Rect(row.RenderSize));
                if (_dragPoint.Y < bounds.Top + bounds.Height / 2) { y = bounds.Top; return commands; }
                y = bounds.Bottom;
            }
            if (_current.Items[i] is SimplifiedCustomizeEntry) commands++;
        }
        return commands;
    }
    private void ShowInsertion()
    {
        if (_current is null || AdornerLayer.GetAdornerLayer(_current) is not { } layer) return;
        DropSlot(out double y);
        var scroll = FindScrollViewer(_current);
        var viewport = scroll?.Template.FindName("PART_ScrollContentPresenter", scroll) as FrameworkElement ?? scroll;
        Rect bounds = viewport is null ? new Rect(_current.RenderSize) : viewport.TransformToAncestor(_current).TransformBounds(new Rect(viewport.RenderSize));
        if (_insertion is null) { _insertion = new InsertionAdorner(_current); layer.Add(_insertion); }
        _insertion.Line = new Rect(bounds.Left + 3, Math.Clamp(y, bounds.Top + 2, Math.Max(bounds.Top + 2, bounds.Bottom - 2)), Math.Max(0, bounds.Width - 6), 0);
        _insertion.InvalidateVisual();
    }
    private void ScrollDrag()
    {
        if (_current is null || FindScrollViewer(_current) is not { } scroll) return;
        Rect bounds = scroll.TransformToAncestor(_current).TransformBounds(new Rect(scroll.RenderSize));
        double delta = _dragPoint.Y < bounds.Top + 24 ? -12 : _dragPoint.Y > bounds.Bottom - 24 ? 12 : 0;
        if (delta == 0) return;
        scroll.ScrollToVerticalOffset(scroll.VerticalOffset + delta); _current.UpdateLayout(); ShowInsertion();
    }
    private void ClearInsertion()
    {
        _dragScroll?.Stop();
        if (_insertion is not null) { AdornerLayer.GetAdornerLayer(_insertion.AdornedElement)?.Remove(_insertion); _insertion = null; }
    }
    private static ScrollViewer? FindScrollViewer(DependencyObject root)
    {
        if (root is ScrollViewer scroll) return scroll;
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            if (FindScrollViewer(VisualTreeHelper.GetChild(root, i)) is { } child) return child;
        return null;
    }
    private sealed class InsertionAdorner : Adorner
    {
        internal Rect Line;
        internal InsertionAdorner(UIElement element) : base(element) { IsHitTestVisible = false; }
        protected override void OnRender(DrawingContext drawingContext)
        {
            var brush = (AdornedElement as FrameworkElement)?.TryFindResource("RibbonKit.Brushes.Accent") as Brush ?? SystemColors.HighlightBrush;
            drawingContext.DrawLine(new Pen(brush, 2), Line.TopLeft, Line.TopRight);
        }
    }
}
