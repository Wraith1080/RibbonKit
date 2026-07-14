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

namespace RibbonKit.Controls;

/// <summary>
/// Drives the Office-style KeyTip experience for one <see cref="Ribbon"/>: pressing
/// <c>Alt</c> (or <c>F10</c>) shows access-key badges over the File button, tabs, and
/// quick-access items; typing a key selects a tab (descending into its groups' badges),
/// opens the File backstage (descending into its page badges), opens a dropdown or
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
        if (_ribbon.IsBackstageOpen)
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

    // ---- Level builders -----------------------------------------------------------

    private KeyTipLevel BuildRootLevel()
    {
        var items = new List<KeyTipItem>();

        if (FindApplicationButton() is { IsVisible: true } appButton)
        {
            // With a backstage assigned, the File key opens it and descends; otherwise
            // it is just a normal invoke.
            KeyTipKind kind = _ribbon.Backstage is not null ? KeyTipKind.Backstage : KeyTipKind.Leaf;
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

        // Quick-access items get numbers (1..9) like Office, unless pinned explicitly.
        int digit = 1;
        foreach (object entry in _ribbon.QuickAccessItems)
        {
            if (entry is not UIElement element || !element.IsVisible)
            {
                continue;
            }

            string? keys = KeyTip.GetKeys(element) ?? (digit <= 9 ? digit.ToString() : null);
            digit++;
            items.Add(new KeyTipItem(element, KeyTipKind.Leaf, null, keys));
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

    private KeyTipLevel BuildBackstageLevel()
    {
        var items = new List<KeyTipItem>();

        if (_ribbon.Backstage is ItemsControl backstage)
        {
            foreach (object? entry in backstage.Items)
            {
                if (entry is UIElement { IsVisible: true } element)
                {
                    items.Add(new KeyTipItem(element, KeyTipKind.Leaf, GetLabel(element), KeyTip.GetKeys(element)));
                }
            }
        }

        AutoAssign(items);
        return new KeyTipLevel(items);
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
    /// Invokes a ribbon control's default action through its UI Automation patterns
    /// (Invoke/Toggle), with special handling for combos, galleries, and tab items.
    /// Shared by KeyTip invocation and the quick-access proxy buttons (see
    /// <see cref="Ribbon.AddToQuickAccess"/>), so both paths behave identically.
    /// </summary>
    internal static void InvokeControl(UIElement element)
    {
        switch (element)
        {
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
        else if (element is ButtonBase button)
        {
            button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        }
    }

    private ToggleButton? FindApplicationButton() =>
        FindDescendant<ToggleButton>(_ribbon, b => b.Name == "ApplicationButton");

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
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Reserve explicitly-set (and pre-seeded numeric) keys first.
        foreach (KeyTipItem item in items)
        {
            if (!string.IsNullOrEmpty(item.Keys))
            {
                item.Keys = item.Keys.ToUpperInvariant();
                used.Add(item.Keys);
            }
        }

        foreach (KeyTipItem item in items)
        {
            if (!string.IsNullOrEmpty(item.Keys))
            {
                continue;
            }

            string label = item.Label ?? string.Empty;
            string? pick = null;

            foreach (char ch in label)
            {
                if (char.IsLetterOrDigit(ch))
                {
                    string candidate = char.ToUpperInvariant(ch).ToString();
                    if (used.Add(candidate))
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
                    if (used.Add(candidate))
                    {
                        pick = candidate;
                        break;
                    }
                }
            }

            item.Keys = pick ?? string.Empty;
        }
    }

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
        Backstage,
    }

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
