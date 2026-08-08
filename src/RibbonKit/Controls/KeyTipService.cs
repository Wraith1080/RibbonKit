using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using RibbonKit.Animation;
using RibbonKit.Layout;
using RibbonKit.Localization;

namespace RibbonKit.Controls;

/// <summary>
/// Drives the Office-style KeyTip experience for one <see cref="Ribbon"/>: pressing
/// <c>Alt</c> (or <c>F10</c>) shows access-key badges over the File button, tabs, and
/// quick-access items; typing a key selects a tab (descending into its groups' badges),
/// opens the File surface (descending into backstage page badges when the backstage is
/// the assigned surface), opens a dropdown or
/// collapsed-group flyout (descending into their badges), or invokes a control.
/// <c>Backspace</c> climbs back a level; <c>Esc</c>/<c>Alt</c>/a mouse click exits.
/// </summary>
/// <remarks>
/// KeyTips not set explicitly via <see cref="KeyTip"/> are auto-derived from each
/// control's label, unique within the level, matching Office. Split buttons get two
/// badges (primary action and menu), and groups can badge their dialog launcher.
/// </remarks>
internal sealed class KeyTipService
{
    private readonly Ribbon _ribbon;
    private readonly Stack<KeyTipLevel> _levels = new();
    private Window? _window;
    private bool _active;
    private bool _altArmed;
    private bool _transitioning;
    private string _typed = string.Empty;

    internal KeyTipService(Ribbon ribbon)
    {
        _ribbon = ribbon;
        _ribbon.Loaded += (_, _) => AttachToWindow();
        _ribbon.Unloaded += (_, _) => Exit();
    }

    private void AttachToWindow()
    {
        Window? window = Window.GetWindow(_ribbon);
        if (ReferenceEquals(window, _window))
        {
            return;
        }

        if (_window is not null)
        {
            _window.PreviewKeyDown -= OnPreviewKeyDown;
            _window.PreviewKeyUp -= OnPreviewKeyUp;
            _window.PreviewMouseDown -= OnMouseDown;
            _window.Deactivated -= OnWindowDeactivated;
        }

        _window = window;
        if (_window is null)
        {
            return;
        }

        _window.PreviewKeyDown += OnPreviewKeyDown;
        _window.PreviewKeyUp += OnPreviewKeyUp;
        _window.PreviewMouseDown += OnMouseDown;
        _window.Deactivated += OnWindowDeactivated;
    }

    private void OnWindowDeactivated(object? sender, EventArgs e) => Exit();

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (_active)
        {
            Exit();
        }
    }

    private void OnPreviewKeyUp(object sender, KeyEventArgs e)
    {
        // Entering: a lone Alt press-and-release toggles KeyTip mode (Office behaviour).
        if (!_active && _altArmed && IsAltKey(ResolveKey(e)))
        {
            _altArmed = false;
            Enter();
            e.Handled = true;
        }
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        Key key = ResolveKey(e);

        if (!_active)
        {
            // Arm on a clean, lone Alt; disarm the moment anything else is pressed so
            // Alt+letter mnemonics and Alt+Tab are never swallowed.
            if (IsAltKey(key) && !e.IsRepeat && Keyboard.Modifiers == ModifierKeys.Alt)
            {
                _altArmed = true;
            }
            else if (key == Key.F10 && Keyboard.Modifiers == ModifierKeys.None)
            {
                Enter();
                e.Handled = true;
            }
            else
            {
                _altArmed = false;
            }

            return;
        }

        // Active: the ribbon owns the keyboard until the user leaves KeyTip mode.
        // While a level is being built (a popup/tab is realizing), swallow input so a
        // stray keystroke can't act on the level we're leaving.
        if (_transitioning)
        {
            e.Handled = true;
            return;
        }

        switch (key)
        {
            case Key.Escape:
                PopOrExit();
                e.Handled = true;
                return;
            case Key.Back:
                OnBackspace();
                e.Handled = true;
                return;
            case Key.LeftAlt:
            case Key.RightAlt:
            case Key.F10:
                Exit();
                e.Handled = true;
                return;
        }

        if (KeyToChar(key) is char c)
        {
            AppendChar(c);
            e.Handled = true;
        }
    }

    private void Enter()
    {
        if (_active)
        {
            return;
        }

        KeyTipLevel level;
        if (_ribbon.IsApplicationMenuOpen)
        {
            // The two-pane menu was opened by mouse. Badge its live visual contents, and do not
            // close it merely because the user leaves KeyTip mode.
            level = BuildApplicationMenuLevel();
            level.PersistOnActivate = true;
            level.IsTerminal = true;
        }
        else if (_ribbon.IsBackstageOpen)
        {
            // The backstage is already open (opened by mouse) — badge only its pages, not
            // the covered-up ribbon. Since KeyTips didn't open it, leaving KeyTip mode
            // must not close it, so no OnExit is attached.
            level = BuildBackstageLevel();
            level.PersistOnActivate = true;
            level.IsTerminal = true;
        }
        else
        {
            level = BuildRootLevel();
        }

        if (level.Items.Count == 0)
        {
            return;
        }

        _active = true;
        _typed = string.Empty;
        _levels.Push(level);
        AddAdorners(level);
    }

    /// <summary>Full teardown (Esc at root, Alt, click, deactivate): closes open popups
    /// and the backstage too.</summary>
    private void Exit() => TearDown(respectPersist: false);

    /// <summary>Teardown after invoking a leaf: closes menus/flyouts but leaves a
    /// persistent surface (the backstage, whose page the user just chose) open.</summary>
    private void ExitAfterActivate() => TearDown(respectPersist: true);

    private void TearDown(bool respectPersist)
    {
        while (_levels.Count > 0)
        {
            KeyTipLevel level = _levels.Pop();
            RemoveAdorners(level);
            if (!(respectPersist && level.PersistOnActivate))
            {
                level.OnExit?.Invoke();
            }
        }

        _active = false;
        _altArmed = false;
        _transitioning = false;
        _typed = string.Empty;
    }

    private void PopOrExit()
    {
        if (_levels.Count == 0)
        {
            _active = false;
            return;
        }

        // Backing out of a terminal surface (the backstage) leaves it entirely.
        if (_levels.Peek().IsTerminal || _levels.Count <= 1)
        {
            Exit();
            return;
        }

        KeyTipLevel child = _levels.Pop();
        RemoveAdorners(child);
        child.OnExit?.Invoke();

        _typed = string.Empty;
        AddAdorners(_levels.Peek());
    }

    private void OnBackspace()
    {
        if (_typed.Length > 0)
        {
            _typed = _typed[..^1];
            UpdateDim();
        }
        else
        {
            PopOrExit();
        }
    }

    private void AppendChar(char c)
    {
        string candidate = _typed + char.ToUpperInvariant(c);
        KeyTipLevel level = _levels.Peek();

        List<KeyTipItem> matches = level.Items
            .Where(i => i.Keys.StartsWith(candidate, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matches.Count == 0)
        {
            // No badge starts with this — ignore the keystroke (keep the current prefix).
            return;
        }

        _typed = candidate;
        UpdateDim();

        // Activate only when a single badge remains AND it is fully typed (so a key that
        // is a prefix of a longer one still waits for the next character).
        if (matches.Count == 1 &&
            string.Equals(matches[0].Keys, _typed, StringComparison.OrdinalIgnoreCase))
        {
            Activate(matches[0]);
        }
    }

    private void UpdateDim()
    {
        foreach (KeyTipItem item in _levels.Peek().Items)
        {
            if (item.Adorner is not null)
            {
                item.Adorner.Dimmed = !item.Keys.StartsWith(_typed, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    private void Activate(KeyTipItem item)
    {
        _typed = string.Empty;

        switch (item.Kind)
        {
            case KeyTipKind.Tab when item.Payload is RibbonTab tab:
                DescendIntoTab(tab);
                break;

            case KeyTipKind.MenuOpener when item.Payload is RibbonDropDownButton opener:
                DescendIntoMenu(opener);
                break;

            case KeyTipKind.GroupFlyout when item.Payload is RibbonGroup group:
                DescendIntoGroupFlyout(group);
                break;

            case KeyTipKind.QuickAccessOverflow when item.Payload is RibbonQuickAccessToolBar toolBar:
                DescendIntoQuickAccessOverflow(toolBar);
                break;

            case KeyTipKind.ApplicationMenu:
                DescendIntoApplicationMenu();
                break;

            case KeyTipKind.ApplicationMenuPaneOpener:
                RefreshApplicationMenuPane(item);
                break;

            case KeyTipKind.BackstagePage:
                RefreshBackstagePage(item);
                break;

            case KeyTipKind.Backstage:
                DescendIntoBackstage();
                break;

            default:
                // Leaf: fire the control, then tear the session down (keeping the
                // backstage open if that is where the just-chosen page lives).
                InvokeControl(item.Target);
                ExitAfterActivate();
                break;
        }
    }

    private void DescendIntoTab(RibbonTab tab)
    {
        KeyTipLevel parent = _levels.Peek();
        RemoveAdorners(parent);
        _transitioning = true;
        _ribbon.SetCurrentValue(Ribbon.SelectedTabProperty, tab);

        // Let the selected tab realize and lay out its groups before measuring badges.
        _ribbon.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, (Action)(() =>
        {
            _transitioning = false;
            if (!_active)
            {
                return;
            }

            KeyTipLevel level = BuildTabGroupsLevel(tab);
            if (level.Items.Count == 0)
            {
                AddAdorners(parent); // nothing actionable — stay at the current level
                return;
            }

            _levels.Push(level);
            AddAdorners(level);
        }));
    }

    private void DescendIntoMenu(RibbonDropDownButton opener)
    {
        KeyTipLevel parent = _levels.Peek();
        RemoveAdorners(parent);
        _transitioning = true;
        opener.SetCurrentValue(RibbonDropDownButton.IsDropDownOpenProperty, true);

        _ribbon.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, (Action)(() =>
        {
            _transitioning = false;
            if (!_active)
            {
                return;
            }

            KeyTipLevel level = BuildMenuLevel(opener);
            level.OnExit = () =>
                opener.SetCurrentValue(RibbonDropDownButton.IsDropDownOpenProperty, false);

            if (level.Items.Count == 0)
            {
                level.OnExit();
                AddAdorners(parent);
                return;
            }

            _levels.Push(level);
            AddAdorners(level);
        }));
    }

    private void DescendIntoGroupFlyout(RibbonGroup group)
    {
        KeyTipLevel parent = _levels.Peek();
        RemoveAdorners(parent);
        _transitioning = true;
        group.CollapsedButton?.SetCurrentValue(ToggleButton.IsCheckedProperty, true);

        _ribbon.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, (Action)(() =>
        {
            _transitioning = false;
            if (!_active)
            {
                return;
            }

            KeyTipLevel level = BuildFlyoutLevel(group);
            level.OnExit = () =>
                group.CollapsedButton?.SetCurrentValue(ToggleButton.IsCheckedProperty, false);

            if (level.Items.Count == 0)
            {
                level.OnExit();
                AddAdorners(parent);
                return;
            }

            _levels.Push(level);
            AddAdorners(level);
        }));
    }

    private void DescendIntoQuickAccessOverflow(RibbonQuickAccessToolBar toolBar)
    {
        KeyTipLevel parent = _levels.Peek();
        RemoveAdorners(parent);
        _transitioning = true;
        toolBar.OpenOverflow();

        // Opening the popup realizes a separate visual tree and its proxy controls. Wait for that
        // layout before asking each proxy for an adorner layer, exactly as dropdown levels do.
        _ribbon.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, (Action)(() =>
        {
            _transitioning = false;
            if (!_active)
            {
                return;
            }

            KeyTipLevel level = BuildQuickAccessOverflowLevel(toolBar);
            level.OnExit = toolBar.CloseOverflow;

            if (level.Items.Count == 0)
            {
                level.OnExit();
                AddAdorners(parent);
                return;
            }

            _levels.Push(level);
            AddAdorners(level);
        }));
    }

    private void DescendIntoBackstage()
    {
        KeyTipLevel parent = _levels.Peek();
        RemoveAdorners(parent);
        _transitioning = true;
        _ribbon.SetCurrentValue(Ribbon.IsBackstageOpenProperty, true);

        _ribbon.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, (Action)(() =>
        {
            _transitioning = false;
            if (!_active)
            {
                return;
            }

            KeyTipLevel level = BuildBackstageLevel();
            level.OnExit = () => _ribbon.SetCurrentValue(Ribbon.IsBackstageOpenProperty, false);
            level.PersistOnActivate = true; // choosing a page keeps the backstage open
            level.IsTerminal = true;        // Backspace/Esc leaves the backstage entirely

            if (level.Items.Count == 0)
            {
                level.OnExit();
                Exit();
                return;
            }

            _levels.Push(level);
            AddAdorners(level);
        }));
    }

    private void DescendIntoApplicationMenu()
    {
        KeyTipLevel parent = _levels.Peek();
        RemoveAdorners(parent);
        _transitioning = true;
        _ribbon.SetCurrentValue(Ribbon.IsBackstageOpenProperty, true);

        _ribbon.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, (Action)(() =>
        {
            _transitioning = false;
            if (!_active)
            {
                return;
            }

            KeyTipLevel level = BuildApplicationMenuLevel();
            level.OnExit = () => _ribbon.SetCurrentValue(Ribbon.IsBackstageOpenProperty, false);
            level.PersistOnActivate = true;
            level.IsTerminal = true;

            if (level.Items.Count == 0)
            {
                level.OnExit();
                Exit();
                return;
            }

            _levels.Push(level);
            AddAdorners(level);
        }));
    }

    private void RefreshApplicationMenuPane(KeyTipItem opener)
    {
        KeyTipLevel current = _levels.Peek();
        RemoveAdorners(current);
        _transitioning = true;

        // Invoke the same template part as a mouse click. For a true split row this is the arrow;
        // for a merged drop-down row it is the primary hit area. Both paths claim the pane without
        // executing the split row's default command or dismissing the application menu.
        if (opener.Target is ButtonBase button)
        {
            // Raise the routed click synchronously. UIA's Button invocation may defer its click;
            // rebuilding at Loaded priority could otherwise inspect the old pane first.
            button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        }

        _ribbon.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, (Action)(() =>
        {
            _transitioning = false;
            if (!_active || _levels.Count == 0 || !ReferenceEquals(_levels.Peek(), current))
            {
                return;
            }

            KeyTipLevel refreshed = BuildApplicationMenuLevel();
            refreshed.OnExit = current.OnExit;
            refreshed.PersistOnActivate = current.PersistOnActivate;
            refreshed.IsTerminal = current.IsTerminal;

            if (refreshed.Items.Count == 0)
            {
                AddAdorners(current);
                return;
            }

            _levels.Pop();
            _levels.Push(refreshed);
            AddAdorners(refreshed);
        }));
    }

    private void RefreshBackstagePage(KeyTipItem page)
    {
        KeyTipLevel current = _levels.Peek();
        RemoveAdorners(current);
        _transitioning = true;

        // Page selection is synchronous, but its arbitrary content is presented through the
        // Backstage template. Wait for that presenter to realize the new visual tree before
        // discovering explicitly tagged custom controls and asking for their adorner layers.
        InvokeControl(page.Target);

        _ribbon.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, (Action)(() =>
        {
            _transitioning = false;
            if (!_active || _levels.Count == 0 || !ReferenceEquals(_levels.Peek(), current))
            {
                return;
            }

            KeyTipLevel refreshed = BuildBackstageLevel();
            refreshed.OnExit = current.OnExit;
            refreshed.PersistOnActivate = current.PersistOnActivate;
            refreshed.IsTerminal = current.IsTerminal;

            if (refreshed.Items.Count == 0)
            {
                AddAdorners(current);
                return;
            }

            _levels.Pop();
            _levels.Push(refreshed);
            AddAdorners(refreshed);
        }));
    }

    // ---- Level builders -----------------------------------------------------------

    private KeyTipLevel BuildRootLevel()
    {
        var items = new List<KeyTipItem>();

        if (FindApplicationButton(_ribbon) is { IsVisible: true } appButton)
        {
            // The application menu wins over the backstage everywhere else in Ribbon, so it must
            // win here too. Both application surfaces descend into their own command level.
            KeyTipKind kind = ApplicationButtonOpensApplicationMenu(_ribbon)
                ? KeyTipKind.ApplicationMenu
                : ApplicationButtonOpensBackstage(_ribbon)
                    ? KeyTipKind.Backstage
                    : KeyTipKind.Leaf;
            items.Add(new KeyTipItem(appButton, kind, "File", KeyTip.GetKeys(appButton)));
        }

        foreach (RibbonTab tab in _ribbon.Tabs)
        {
            if (tab.Visibility == Visibility.Visible)
            {
                items.Add(new KeyTipItem(tab, KeyTipKind.Tab, tab.Header?.ToString(), KeyTip.GetKeys(tab))
                {
                    Payload = tab,
                });
            }
        }

        // Quick-access items get numbers (1..9) like Office, unless pinned explicitly. Elements
        // moved to overflow remain Visibility=Visible but receive a zero-sized layout slot, so
        // IsVisible alone is insufficient — badge only the entries the active panel kept.
        RibbonQuickAccessToolBar? quickAccessToolBar = _ribbon.ActiveQuickAccessToolBar;
        int digit = 1;
        foreach (object entry in _ribbon.QuickAccessItems)
        {
            if (entry is not UIElement element
                || !element.IsVisible
                || quickAccessToolBar?.IsOverflowed(element) == true)
            {
                continue;
            }

            string? keys = KeyTip.GetKeys(element) ?? (digit <= 9 ? digit.ToString() : null);
            digit++;
            items.Add(new KeyTipItem(element, KeyTipKind.Leaf, null, keys));
        }

        // The chevron represents every zero-slotted item as one root action. It takes the next QAT
        // digit and descends into the proxy entries in the popup, rather than letting the hidden
        // originals stack their badges at the strip's (0,0) origin.
        if (quickAccessToolBar is { HasOverflow: true, OverflowButton.IsVisible: true } toolBar)
        {
            ToggleButton overflowButton = toolBar.OverflowButton!;
            string? keys = KeyTip.GetKeys(overflowButton) ?? (digit <= 9 ? digit.ToString() : null);
            items.Add(new KeyTipItem(
                overflowButton,
                KeyTipKind.QuickAccessOverflow,
                RibbonLocalization.GetString(RibbonString.MoreQuickAccessCommands),
                keys)
            {
                Payload = toolBar,
            });
        }

        AutoAssign(items);
        return new KeyTipLevel(items);
    }

    private KeyTipLevel BuildTabGroupsLevel(RibbonTab tab)
    {
        var items = new List<KeyTipItem>();

        foreach (RibbonGroup group in tab.Groups)
        {
            if (group.Visibility != Visibility.Visible)
            {
                continue;
            }

            if (group.SizeState == RibbonGroupSizeState.Collapsed)
            {
                // A collapsed group shows a single button; its KeyTip opens the flyout
                // and descends into the controls inside it.
                if (group.CollapsedButton is { IsVisible: true } collapsed)
                {
                    items.Add(new KeyTipItem(collapsed, KeyTipKind.GroupFlyout, group.Header?.ToString(), KeyTip.GetKeys(group))
                    {
                        Payload = group,
                    });
                }

                continue;
            }

            // Controls are often nested inside layout panels, so walk the group's visual
            // subtree rather than just its direct Items.
            var controls = new List<UIElement>();
            CollectKeyTipControls(group, controls);
            foreach (UIElement control in controls)
            {
                AddControlItems(control, items);
            }

            // The group's dialog launcher (the small ↘ corner button), if shown.
            if (group.ShowDialogLauncher && group.DialogLauncher is { IsVisible: true } launcher)
            {
                items.Add(new KeyTipItem(launcher, KeyTipKind.Leaf, group.Header?.ToString(), KeyTip.GetKeys(group)));
            }
        }

        AutoAssign(items);
        return new KeyTipLevel(items);
    }

    private KeyTipLevel BuildFlyoutLevel(RibbonGroup group)
    {
        var items = new List<KeyTipItem>();

        if (group.FlyoutContent is { } content)
        {
            var controls = new List<UIElement>();
            CollectKeyTipControls(content, controls);
            foreach (UIElement control in controls)
            {
                AddControlItems(control, items);
            }
        }

        AutoAssign(items);
        return new KeyTipLevel(items);
    }

    private KeyTipLevel BuildMenuLevel(RibbonDropDownButton opener)
    {
        var items = new List<KeyTipItem>();

        foreach (object? entry in opener.Items)
        {
            if (entry is UIElement { IsVisible: true } element)
            {
                items.Add(CreateControlItem(element));
            }
        }

        AutoAssign(items);
        return new KeyTipLevel(items);
    }

    private static KeyTipLevel BuildQuickAccessOverflowLevel(RibbonQuickAccessToolBar toolBar)
    {
        var items = new List<KeyTipItem>();

        foreach (FrameworkElement entry in toolBar.OverflowEntries)
        {
            if (entry.IsVisible)
            {
                AddControlItems(entry, items);
            }
        }

        AutoAssign(items);
        return new KeyTipLevel(items);
    }

    private KeyTipLevel BuildBackstageLevel()
    {
        var items = new List<KeyTipItem>();

        if (_ribbon.Backstage is ItemsControl backstage)
        {
            foreach (object? entry in backstage.Items)
            {
                BackstageTabItem? item = entry as BackstageTabItem
                    ?? backstage.ItemContainerGenerator.ContainerFromItem(entry) as BackstageTabItem;
                if (item is { IsVisible: true })
                {
                    items.Add(new KeyTipItem(
                        item,
                        item.IsButton ? KeyTipKind.Leaf : KeyTipKind.BackstagePage,
                        GetLabel(item),
                        KeyTip.GetKeys(item)));
                }
            }

            AddContentItems(GetBackstageContentKeyTipTargets(backstage), items);
        }

        AutoAssign(items);
        return new KeyTipLevel(items);
    }

    private KeyTipLevel BuildApplicationMenuLevel()
    {
        var items = new List<KeyTipItem>();

        if (_ribbon.ApplicationMenu is not RibbonApplicationMenu menu)
        {
            return new KeyTipLevel(items);
        }

        // A split nav row has two actions, just like RibbonSplitButton: its primary command and its
        // pane-opening arrow. A merged drop-down has only one visual action, so its primary hit area
        // is registered as the pane opener and the implementation-only arrow gets no duplicate tip.
        // Explicit row keys belong to the first/primary target; a true split arrow auto-derives.
        foreach (object? entry in menu.Items)
        {
            RibbonApplicationMenuItem? item = entry as RibbonApplicationMenuItem
                ?? menu.ItemContainerGenerator.ContainerFromItem(entry) as RibbonApplicationMenuItem;
            if (item is not { IsVisible: true })
            {
                continue;
            }

            IReadOnlyList<ApplicationMenuNavTarget> targets = GetApplicationMenuNavTargets(item);
            for (int index = 0; index < targets.Count; index++)
            {
                ApplicationMenuNavTarget target = targets[index];
                if (!target.Target.IsVisible)
                {
                    continue;
                }

                items.Add(new KeyTipItem(
                    target.Target,
                    target.OpensPane ? KeyTipKind.ApplicationMenuPaneOpener : KeyTipKind.Leaf,
                    item.Header?.ToString(),
                    index == 0 ? KeyTip.GetKeys(item) : null));
            }
        }

        // The currently visible pane (default Recent Documents or a claimed nav pane) and footer
        // are arbitrary content. Their two RibbonKit command types remain automatic targets; any
        // other UIElement must explicitly opt in with KeyTip.Keys.
        AddContentItems(GetApplicationMenuContentKeyTipTargets(menu), items);

        AutoAssign(items);
        return new KeyTipLevel(items);
    }

    /// <summary>
    /// Returns the distinct keyboard actions exposed by an application-menu nav row. Kept as one
    /// shape decision so the KeyTip service cannot drift from the control's split/drop-down model.
    /// </summary>
    internal static IReadOnlyList<ApplicationMenuNavTarget> GetApplicationMenuNavTargets(
        RibbonApplicationMenuItem item)
    {
        item.ApplyTemplate();
        if (item.PrimaryPart is not { } primary)
        {
            return Array.Empty<ApplicationMenuNavTarget>();
        }

        if (!item.HasPane)
        {
            return [new ApplicationMenuNavTarget(primary, OpensPane: false)];
        }

        if (!item.IsSplitPresentation)
        {
            return [new ApplicationMenuNavTarget(primary, OpensPane: true)];
        }

        return item.ArrowPart is { } arrow
            ? [
                new ApplicationMenuNavTarget(primary, OpensPane: false),
                new ApplicationMenuNavTarget(arrow, OpensPane: true),
            ]
            : [new ApplicationMenuNavTarget(primary, OpensPane: false)];
    }

    private static void AddControlItems(UIElement control, List<KeyTipItem> items)
    {
        // A split button gets TWO badges: one on the primary command part, one on the
        // chevron that opens the menu. Its explicit KeyTip (if any) applies to the
        // primary; the menu part auto-derives.
        if (control is RibbonSplitButton split &&
            split.PrimaryPart is { } primary &&
            split.TogglePart is { } toggle)
        {
            items.Add(new KeyTipItem(primary, KeyTipKind.Leaf, GetLabel(split), KeyTip.GetKeys(split)));
            items.Add(new KeyTipItem(toggle, KeyTipKind.MenuOpener, GetLabel(split), null)
            {
                Payload = split,
            });
            return;
        }

        items.Add(CreateControlItem(control));
    }

    private static KeyTipItem CreateControlItem(UIElement control)
    {
        // Split buttons derive from dropdown buttons, so this ordering matters.
        KeyTipKind kind = control is RibbonDropDownButton ? KeyTipKind.MenuOpener : KeyTipKind.Leaf;
        var item = new KeyTipItem(control, kind, GetLabel(control), KeyTip.GetKeys(control));
        if (kind == KeyTipKind.MenuOpener)
        {
            item.Payload = control;
        }

        return item;
    }

    private static void CollectKeyTipControls(DependencyObject root, List<UIElement> results)
    {
        int count = VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < count; i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(root, i);

            // A ribbon control is a KeyTip leaf/opener — collect it and stop descending
            // (we don't want its inner glyphs, and a dropdown's items belong to a deeper
            // level reached by activating it).
            if (child is RibbonButton or RibbonToggleButton or RibbonDropDownButton or RibbonComboBox or InRibbonGallery)
            {
                if (child is UIElement { IsVisible: true } control)
                {
                    results.Add(control);
                }

                continue;
            }

            CollectKeyTipControls(child, results);
        }
    }

    private static void AddContentItems(IEnumerable<UIElement> controls, List<KeyTipItem> items)
    {
        var existing = new HashSet<UIElement>(items.Select(item => item.Target));
        foreach (UIElement control in controls)
        {
            if (existing.Add(control))
            {
                items.Add(new KeyTipItem(
                    control,
                    KeyTipKind.Leaf,
                    GetLabel(control),
                    KeyTip.GetKeys(control)));
            }
        }
    }

    /// <summary>
    /// Finds explicitly tagged controls in the currently realized Backstage page. Navigation items
    /// are registered separately, so they are excluded even when they carry authored keys.
    /// </summary>
    internal static IReadOnlyList<UIElement> GetBackstageContentKeyTipTargets(DependencyObject root) =>
        GetContentKeyTipTargets(
            root,
            includeBuiltIn: static _ => false,
            exclude: static element => element is BackstageTabItem);

    /// <summary>
    /// Finds the built-in application-menu pane/footer commands plus arbitrary visible controls
    /// that explicitly opt in with <see cref="KeyTip.KeysProperty"/>. Navigation rows are registered
    /// through their primary/arrow template parts and must not appear a second time here.
    /// </summary>
    internal static IReadOnlyList<UIElement> GetApplicationMenuContentKeyTipTargets(
        DependencyObject root) =>
        GetContentKeyTipTargets(
            root,
            includeBuiltIn: static element =>
                element is RibbonApplicationMenuPaneItem or RibbonApplicationMenuButton,
            exclude: static element => element is RibbonApplicationMenuItem);

    private static IReadOnlyList<UIElement> GetContentKeyTipTargets(
        DependencyObject root,
        Func<UIElement, bool> includeBuiltIn,
        Func<UIElement, bool> exclude)
    {
        var results = new List<UIElement>();
        var seen = new HashSet<UIElement>();

        Visit(root);
        return results;

        void Visit(DependencyObject parent)
        {
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int index = 0; index < count; index++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, index);
                if (child is UIElement element)
                {
                    // A collapsed/hidden branch may remain in the visual tree. Stop at that branch
                    // so only the selected Backstage page or active/default application-menu pane
                    // contributes targets.
                    if (element.Visibility != Visibility.Visible)
                    {
                        continue;
                    }

                    bool hasExplicitKey = !string.IsNullOrWhiteSpace(KeyTip.GetKeys(element));
                    if (!exclude(element) &&
                        (includeBuiltIn(element) || hasExplicitKey) &&
                        seen.Add(element))
                    {
                        results.Add(element);
                    }
                }

                Visit(child);
            }
        }
    }

    // ---- Adorner lifecycle --------------------------------------------------------

    private static void AddAdorners(KeyTipLevel level)
    {
        foreach (KeyTipItem item in level.Items)
        {
            if (item.Shown || string.IsNullOrEmpty(item.Keys))
            {
                continue;
            }

            AdornerLayer? layer = AdornerLayer.GetAdornerLayer(item.Target);
            if (layer is null)
            {
                continue;
            }

            item.Adorner ??= new KeyTipAdorner(item.Target, item.Keys);
            layer.Add(item.Adorner);
            item.Layer = layer;
            item.Shown = true;
            item.Adorner.Dimmed = false;
            RibbonMotion.PlayKeyTipPop(item.Adorner, RibbonAnimationAction.KeyTip);
        }
    }

    private static void RemoveAdorners(KeyTipLevel level)
    {
        foreach (KeyTipItem item in level.Items)
        {
            if (item is { Shown: true, Adorner: not null })
            {
                item.Layer?.Remove(item.Adorner);
                item.Shown = false;
            }
        }
    }

    // ---- Helpers ------------------------------------------------------------------

    /// <summary>
    /// Invokes an element's default action through its UI Automation patterns
    /// (Invoke/Toggle), with special handling for combos, galleries, and tab items.
    /// Shared by KeyTip invocation and the quick-access proxy buttons (see
    /// <see cref="Ribbon.AddToQuickAccess"/>), so both paths behave identically.
    /// </summary>
    internal static void InvokeControl(UIElement element)
    {
        switch (element)
        {
            // Text inputs have no Invoke/Toggle — a KeyTip transfers keyboard focus so typing can
            // begin without changing the existing selection or value.
            case TextBox textBox:
                textBox.Focus();
                return;
            // A combo box or gallery has no Invoke/Toggle — focus it and drop its list.
            case ComboBox combo:
                combo.Focus();
                combo.SetCurrentValue(ComboBox.IsDropDownOpenProperty, true);
                return;
            case InRibbonGallery gallery:
                gallery.Focus();
                gallery.SetCurrentValue(InRibbonGallery.IsDropDownOpenProperty, true);
                return;
            // A backstage page: select it (and keep the backstage open).
            case BackstageTabItem { IsButton: true } backstageAction:
                backstageAction.InvokeAction();
                return;
            case TabItem tabItem:
                tabItem.SetCurrentValue(TabItem.IsSelectedProperty, true);
                tabItem.Focus();
                return;
        }

        AutomationPeer? peer = UIElementAutomationPeer.CreatePeerForElement(element);

        if (peer?.GetPattern(PatternInterface.Invoke) is IInvokeProvider invoke)
        {
            invoke.Invoke();
        }
        else if (peer?.GetPattern(PatternInterface.Toggle) is IToggleProvider toggle)
        {
            toggle.Toggle();
        }
        else if (peer?.GetPattern(PatternInterface.SelectionItem) is ISelectionItemProvider selection)
        {
            // RadioButton exposes SelectionItem rather than Toggle. Use its UIA contract so a
            // KeyTip selects it exactly like keyboard Space/click, including GroupName exclusivity.
            selection.Select();
        }
        else if (element is ButtonBase button)
        {
            button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        }
    }

    /// <summary>
    /// Finds the application toggle inside the nested <see cref="RibbonTabControl"/> template.
    /// The shared part-name constant is also used by application-menu light-dismiss; keeping both
    /// consumers on it prevents a template rename from silently breaking only keyboard access.
    /// </summary>
    internal static ToggleButton? FindApplicationButton(DependencyObject root) =>
        FindDescendant<ToggleButton>(root, button => button.Name == Ribbon.ApplicationButtonPartName);

    /// <summary>
    /// Whether activating the root File KeyTip should descend into a backstage level. An assigned
    /// application menu wins over the backstage, matching <see cref="Ribbon.IsApplicationMenuOpen"/>.
    /// </summary>
    internal static bool ApplicationButtonOpensBackstage(Ribbon ribbon) =>
        ribbon.ApplicationMenu is null && ribbon.Backstage is not null;

    /// <summary>Whether activating the root File KeyTip should enter the two-pane menu level.</summary>
    internal static bool ApplicationButtonOpensApplicationMenu(Ribbon ribbon) =>
        ribbon.ApplicationMenu is not null;

    private static T? FindDescendant<T>(DependencyObject root, Func<T, bool> match)
        where T : DependencyObject
    {
        int count = VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < count; i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(root, i);
            if (child is T typed && match(typed))
            {
                return typed;
            }

            if (FindDescendant(child, match) is { } found)
            {
                return found;
            }
        }

        return null;
    }

    private static void AutoAssign(List<KeyTipItem> items)
    {
        string[] resolved = ResolveKeys(items
            .Select(item => new KeyTipCandidate(item.Label, item.Keys))
            .ToArray());

        for (int index = 0; index < items.Count; index++)
        {
            items[index].Keys = resolved[index];
        }
    }

    /// <summary>
    /// Resolves one KeyTip level into deterministic, typeable key sequences. Explicit assignments
    /// are reserved before automatic derivation so authored access keys always take precedence.
    /// When two explicit assignments are equal or one is a prefix of the other, the first wins and
    /// the later item falls back to label derivation; otherwise neither badge could be activated.
    /// </summary>
    internal static string[] ResolveKeys(IReadOnlyList<KeyTipCandidate> candidates)
    {
        var resolved = new string?[candidates.Count];
        var used = new List<string>();

        // Reserve every usable explicit (and pre-seeded numeric) key before deriving any labels.
        // This means an automatic item earlier in visual order cannot steal a later authored key.
        for (int index = 0; index < candidates.Count; index++)
        {
            string? explicitKeys = candidates[index].ExplicitKeys;
            if (string.IsNullOrEmpty(explicitKeys))
            {
                continue;
            }

            string normalized = explicitKeys.Trim().ToUpperInvariant();
            if (normalized.Length == 0 || normalized.Any(ch => !IsTypeableKeyTipChar(ch)))
            {
                continue;
            }

            if (!ConflictsWithAny(normalized, used))
            {
                resolved[index] = normalized;
                used.Add(normalized);
            }
        }

        for (int index = 0; index < candidates.Count; index++)
        {
            if (resolved[index] is not null)
            {
                continue;
            }

            string label = candidates[index].Label ?? string.Empty;
            string? pick = null;

            foreach (char ch in label)
            {
                if (IsTypeableKeyTipChar(ch))
                {
                    string candidate = char.ToUpperInvariant(ch).ToString();
                    if (!ConflictsWithAny(candidate, used))
                    {
                        pick = candidate;
                        break;
                    }
                }
            }

            if (pick is null)
            {
                const string fallback = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
                foreach (char ch in fallback)
                {
                    string candidate = ch.ToString();
                    if (!ConflictsWithAny(candidate, used))
                    {
                        pick = candidate;
                        break;
                    }
                }
            }

            resolved[index] = pick ?? string.Empty;
            if (pick is not null)
            {
                used.Add(pick);
            }
        }

        return resolved.Select(keys => keys ?? string.Empty).ToArray();
    }

    private static bool ConflictsWithAny(string candidate, IEnumerable<string> used) =>
        used.Any(existing =>
            candidate.StartsWith(existing, StringComparison.OrdinalIgnoreCase) ||
            existing.StartsWith(candidate, StringComparison.OrdinalIgnoreCase));

    private static bool IsTypeableKeyTipChar(char value) =>
        value is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9';

    private static string? GetLabel(UIElement element) => element switch
    {
        RibbonTab tab => tab.Header?.ToString(),
        RibbonButton button => button.Header,
        RibbonToggleButton toggle => toggle.Header,
        RibbonSplitButton split => split.Header,
        RibbonDropDownButton dropDown => dropDown.Header,
        RibbonMenuItem menuItem => menuItem.Header,
        RibbonComboBox combo => combo.Header,
        HeaderedItemsControl headered => headered.Header?.ToString(),
        HeaderedContentControl headeredContent => headeredContent.Header?.ToString(),
        ContentControl content => content.Content?.ToString(),
        _ => null,
    };

    private static Key ResolveKey(KeyEventArgs e) => e.Key == Key.System ? e.SystemKey : e.Key;

    private static bool IsAltKey(Key key) => key is Key.LeftAlt or Key.RightAlt;

    private static char? KeyToChar(Key key)
    {
        if (key is >= Key.A and <= Key.Z)
        {
            return (char)('A' + (key - Key.A));
        }

        if (key is >= Key.D0 and <= Key.D9)
        {
            return (char)('0' + (key - Key.D0));
        }

        if (key is >= Key.NumPad0 and <= Key.NumPad9)
        {
            return (char)('0' + (key - Key.NumPad0));
        }

        return null;
    }

    private enum KeyTipKind
    {
        Leaf,
        Tab,
        MenuOpener,
        GroupFlyout,
        QuickAccessOverflow,
        ApplicationMenu,
        ApplicationMenuPaneOpener,
        BackstagePage,
        Backstage,
    }

    internal readonly record struct ApplicationMenuNavTarget(ButtonBase Target, bool OpensPane);

    private sealed class KeyTipItem(UIElement target, KeyTipKind kind, string? label, string? keys)
    {
        public UIElement Target { get; } = target;

        public KeyTipKind Kind { get; } = kind;

        public string? Label { get; } = label;

        public string Keys { get; set; } = keys ?? string.Empty;

        public object? Payload { get; set; }

        public KeyTipAdorner? Adorner { get; set; }

        public AdornerLayer? Layer { get; set; }

        public bool Shown { get; set; }
    }

    private sealed class KeyTipLevel(List<KeyTipItem> items)
    {
        public List<KeyTipItem> Items { get; } = items;

        public Action? OnExit { get; set; }

        public bool PersistOnActivate { get; set; }

        public bool IsTerminal { get; set; }
    }
}

internal readonly record struct KeyTipCandidate(string? Label, string? ExplicitKeys);
