using System.Collections.Specialized;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using RibbonKit.Animation;
using RibbonMessageAutomationPeer = RibbonKit.Automation.RibbonMessageAutomationPeer;
using RibbonMessageBarAutomationPeer = RibbonKit.Automation.RibbonMessageBarAutomationPeer;

namespace RibbonKit.Controls;

/// <summary>
/// A repeatable strip of <see cref="RibbonMessage"/> notifications. Assign it to
/// <see cref="Ribbon.MessageBar"/> for theme-aware connected chrome. As an <see cref="ItemsControl"/>,
/// it supports both inline items and an MVVM <see cref="ItemsControl.ItemsSource"/>; visible
/// messages stack vertically.
/// </summary>
/// <example>
/// <code language="xaml">
/// &lt;rk:Ribbon.MessageBar&gt;
///     &lt;rk:RibbonMessageBar&gt;
///         &lt;rk:RibbonMessage Title="PROTECTED VIEW"
///                           Message="Files from the Internet can contain viruses."
///                           ActionContent="Enable Editing" /&gt;
///     &lt;/rk:RibbonMessageBar&gt;
/// &lt;/rk:Ribbon.MessageBar&gt;
/// </code>
/// </example>
public class RibbonMessageBar : ItemsControl
{
    private static readonly DependencyPropertyKey HasOpenMessagesPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(HasOpenMessages),
            typeof(bool),
            typeof(RibbonMessageBar),
            new FrameworkPropertyMetadata(false));

    /// <summary>Identifies the read-only <see cref="HasOpenMessages"/> dependency property.</summary>
    public static readonly DependencyProperty HasOpenMessagesProperty =
        HasOpenMessagesPropertyKey.DependencyProperty;

    private readonly List<RibbonMessage> _trackedMessages = [];

    internal event EventHandler? OpenMessagesChanged;

    static RibbonMessageBar()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(RibbonMessageBar),
            new FrameworkPropertyMetadata(typeof(RibbonMessageBar)));
    }

    /// <inheritdoc />
    protected override bool IsItemItsOwnContainerOverride(object item) => item is RibbonMessage;

    /// <inheritdoc />
    protected override DependencyObject GetContainerForItemOverride() => new RibbonMessage();

    /// <summary>
    /// Gets whether at least one generated message container is presented. Ribbon templates
    /// consume this state to connect or release their lower chrome automatically, retaining the
    /// connection until an animated dismissal has completed.
    /// </summary>
    public bool HasOpenMessages => (bool)GetValue(HasOpenMessagesProperty);

    /// <inheritdoc />
    protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
    {
        base.OnItemsChanged(e);

        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            foreach (RibbonMessage message in _trackedMessages)
            {
                message.PresentationStateChanged -= OnMessagePresentationStateChanged;
            }

            _trackedMessages.Clear();
            foreach (object item in Items)
            {
                if (item is RibbonMessage message)
                {
                    Track(message);
                }
            }
        }
        else
        {
            if (e.OldItems is not null)
            {
                foreach (object item in e.OldItems)
                {
                    if (item is RibbonMessage message)
                    {
                        Untrack(message);
                    }
                }
            }

            if (e.NewItems is not null)
            {
                foreach (object item in e.NewItems)
                {
                    if (item is RibbonMessage message)
                    {
                        Track(message);
                    }
                }
            }
        }

        UpdateHasOpenMessages();
    }

    /// <inheritdoc />
    protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
    {
        base.PrepareContainerForItemOverride(element, item);
        if (element is RibbonMessage message)
        {
            Track(message);
            UpdateHasOpenMessages();
        }
    }

    /// <inheritdoc />
    protected override void ClearContainerForItemOverride(DependencyObject element, object item)
    {
        if (element is RibbonMessage message)
        {
            Untrack(message);
        }

        base.ClearContainerForItemOverride(element, item);
        UpdateHasOpenMessages();
    }

    /// <inheritdoc />
    protected override AutomationPeer OnCreateAutomationPeer() => new RibbonMessageBarAutomationPeer(this);

    private void Track(RibbonMessage message)
    {
        if (!_trackedMessages.Contains(message))
        {
            _trackedMessages.Add(message);
            message.PresentationStateChanged += OnMessagePresentationStateChanged;
        }
    }

    private void Untrack(RibbonMessage message)
    {
        if (_trackedMessages.Remove(message))
        {
            message.PresentationStateChanged -= OnMessagePresentationStateChanged;
            message.SetIsLastOpenMessage(false);
        }
    }

    private void OnMessagePresentationStateChanged(object? sender, EventArgs e) => UpdateHasOpenMessages();

    private void UpdateHasOpenMessages()
    {
        RibbonMessage? lastPresentedMessage = null;
        for (int index = 0; index < Items.Count; index++)
        {
            RibbonMessage? message = Items[index] as RibbonMessage
                ?? ItemContainerGenerator.ContainerFromIndex(index) as RibbonMessage;
            if (message?.IsPresented == true)
            {
                lastPresentedMessage = message;
            }
        }

        // A generated container can reach PrepareContainerForItemOverride just before the
        // generator publishes it from ContainerFromIndex. Preserve ItemsSource support during
        // that narrow phase by falling back to deterministic preparation order.
        if (lastPresentedMessage is null)
        {
            foreach (RibbonMessage message in _trackedMessages)
            {
                if (message.IsPresented)
                {
                    lastPresentedMessage = message;
                }
            }
        }

        foreach (RibbonMessage message in _trackedMessages)
        {
            message.SetIsLastOpenMessage(ReferenceEquals(message, lastPresentedMessage));
        }

        bool hasOpenMessages = lastPresentedMessage is not null;
        if (hasOpenMessages == HasOpenMessages)
        {
            return;
        }

        SetValue(HasOpenMessagesPropertyKey, hasOpenMessages);
        OpenMessagesChanged?.Invoke(this, EventArgs.Empty);
    }
}

/// <summary>
/// One independently actionable and dismissible entry in a <see cref="RibbonMessageBar"/>.
/// The default theme presents it as Office's yellow warning/information strip.
/// </summary>
[TemplatePart(Name = RootPartName, Type = typeof(FrameworkElement))]
[TemplatePart(Name = ActionButtonPartName, Type = typeof(ButtonBase))]
[TemplatePart(Name = CloseButtonPartName, Type = typeof(ButtonBase))]
public class RibbonMessage : Control
{
    internal const string RootPartName = "PART_Root";
    internal const string ActionButtonPartName = "PART_ActionButton";
    internal const string CloseButtonPartName = "PART_CloseButton";

    private static readonly DependencyPropertyKey IsPresentedPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(IsPresented),
            typeof(bool),
            typeof(RibbonMessage),
            new FrameworkPropertyMetadata(true));

    /// <summary>
    /// Identifies the read-only <see cref="IsPresented"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty IsPresentedProperty =
        IsPresentedPropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey IsLastOpenMessagePropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(IsLastOpenMessage),
            typeof(bool),
            typeof(RibbonMessage),
            new FrameworkPropertyMetadata(false));

    /// <summary>
    /// Identifies the read-only <see cref="IsLastOpenMessage"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty IsLastOpenMessageProperty =
        IsLastOpenMessagePropertyKey.DependencyProperty;

    /// <summary>Identifies the <see cref="Title"/> dependency property.</summary>
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(
            nameof(Title),
            typeof(string),
            typeof(RibbonMessage),
            new FrameworkPropertyMetadata(null));

    /// <summary>Identifies the <see cref="Message"/> dependency property.</summary>
    public static readonly DependencyProperty MessageProperty =
        DependencyProperty.Register(
            nameof(Message),
            typeof(string),
            typeof(RibbonMessage),
            new FrameworkPropertyMetadata(null));

    /// <summary>Identifies the <see cref="Icon"/> dependency property.</summary>
    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(
            nameof(Icon),
            typeof(ImageSource),
            typeof(RibbonMessage),
            new FrameworkPropertyMetadata(null));

    /// <summary>Identifies the <see cref="ActionContent"/> dependency property.</summary>
    public static readonly DependencyProperty ActionContentProperty =
        DependencyProperty.Register(
            nameof(ActionContent),
            typeof(object),
            typeof(RibbonMessage),
            new FrameworkPropertyMetadata(null));

    /// <summary>Identifies the <see cref="ActionCommand"/> dependency property.</summary>
    public static readonly DependencyProperty ActionCommandProperty =
        DependencyProperty.Register(
            nameof(ActionCommand),
            typeof(ICommand),
            typeof(RibbonMessage),
            new FrameworkPropertyMetadata(null));

    /// <summary>Identifies the <see cref="ActionCommandParameter"/> dependency property.</summary>
    public static readonly DependencyProperty ActionCommandParameterProperty =
        DependencyProperty.Register(
            nameof(ActionCommandParameter),
            typeof(object),
            typeof(RibbonMessage),
            new FrameworkPropertyMetadata(null));

    /// <summary>Identifies the <see cref="IsDismissible"/> dependency property.</summary>
    public static readonly DependencyProperty IsDismissibleProperty =
        DependencyProperty.Register(
            nameof(IsDismissible),
            typeof(bool),
            typeof(RibbonMessage),
            new FrameworkPropertyMetadata(true));

    /// <summary>Identifies the <see cref="IsOpen"/> dependency property.</summary>
    public static readonly DependencyProperty IsOpenProperty =
        DependencyProperty.Register(
            nameof(IsOpen),
            typeof(bool),
            typeof(RibbonMessage),
            new FrameworkPropertyMetadata(
                true,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnIsOpenChanged));

    /// <summary>Raised when the message's optional action button is invoked.</summary>
    public static readonly RoutedEvent ActionClickEvent = EventManager.RegisterRoutedEvent(
        nameof(ActionClick),
        RoutingStrategy.Bubble,
        typeof(RoutedEventHandler),
        typeof(RibbonMessage));

    /// <summary>Raised when <see cref="Dismiss"/> closes the message.</summary>
    public static readonly RoutedEvent DismissedEvent = EventManager.RegisterRoutedEvent(
        nameof(Dismissed),
        RoutingStrategy.Bubble,
        typeof(RoutedEventHandler),
        typeof(RibbonMessage));

    private ButtonBase? _actionButton;
    private ButtonBase? _closeButton;
    private FrameworkElement? _presentationRoot;
    private bool _entranceHandledForCurrentOpen;
    private bool _entrancePending;
    private int _transitionVersion;

    internal event EventHandler? PresentationStateChanged;

    static RibbonMessage()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(RibbonMessage),
            new FrameworkPropertyMetadata(typeof(RibbonMessage)));
    }

    /// <summary>Initializes a new instance of the <see cref="RibbonMessage"/> class.</summary>
    public RibbonMessage()
    {
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    /// <summary>Short emphasized text at the start of the message, such as "PROTECTED VIEW".</summary>
    public string? Title
    {
        get => (string?)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>The explanatory message text. The default template wraps it when space is tight.</summary>
    public string? Message
    {
        get => (string?)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    /// <summary>
    /// Optional custom icon. When omitted, the template displays its built-in information shield.
    /// </summary>
    public ImageSource? Icon
    {
        get => (ImageSource?)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>Content of the optional action button; <see langword="null"/> hides it.</summary>
    public object? ActionContent
    {
        get => GetValue(ActionContentProperty);
        set => SetValue(ActionContentProperty, value);
    }

    /// <summary>Command executed by the optional action button.</summary>
    public ICommand? ActionCommand
    {
        get => (ICommand?)GetValue(ActionCommandProperty);
        set => SetValue(ActionCommandProperty, value);
    }

    /// <summary>Parameter supplied to <see cref="ActionCommand"/>.</summary>
    public object? ActionCommandParameter
    {
        get => GetValue(ActionCommandParameterProperty);
        set => SetValue(ActionCommandParameterProperty, value);
    }

    /// <summary>Whether the close button is shown. Defaults to <see langword="true"/>.</summary>
    public bool IsDismissible
    {
        get => (bool)GetValue(IsDismissibleProperty);
        set => SetValue(IsDismissibleProperty, value);
    }

    /// <summary>
    /// Whether this message is currently shown. Defaults to <see langword="true"/> and binds
    /// two-way by default so view models can retain notification state.
    /// </summary>
    public bool IsOpen
    {
        get => (bool)GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    /// <summary>
    /// Gets whether this row is still participating in presentation. During an animated close,
    /// this remains <see langword="true"/> until the exit transition finishes even though
    /// <see cref="IsOpen"/> is already <see langword="false"/>.
    /// </summary>
    public bool IsPresented => (bool)GetValue(IsPresentedProperty);

    /// <summary>
    /// Gets whether this is the final presented row in its containing <see cref="RibbonMessageBar"/>.
    /// During an animated close that can outlast <see cref="IsOpen"/>; the shared template uses
    /// the presentation state for generation-specific lower corners.
    /// </summary>
    public bool IsLastOpenMessage => (bool)GetValue(IsLastOpenMessageProperty);

    /// <summary>Raised when the optional action button is invoked.</summary>
    public event RoutedEventHandler ActionClick
    {
        add => AddHandler(ActionClickEvent, value);
        remove => RemoveHandler(ActionClickEvent, value);
    }

    /// <summary>Raised once when the user or application dismisses an open message.</summary>
    public event RoutedEventHandler Dismissed
    {
        add => AddHandler(DismissedEvent, value);
        remove => RemoveHandler(DismissedEvent, value);
    }

    /// <inheritdoc />
    public override void OnApplyTemplate()
    {
        if (_actionButton is not null)
        {
            _actionButton.Click -= OnActionClick;
        }

        if (_closeButton is not null)
        {
            _closeButton.Click -= OnCloseClick;
        }

        base.OnApplyTemplate();

        _presentationRoot = GetTemplateChild(RootPartName) as FrameworkElement;
        _actionButton = GetTemplateChild(ActionButtonPartName) as ButtonBase;
        _closeButton = GetTemplateChild(CloseButtonPartName) as ButtonBase;

        if (_actionButton is not null)
        {
            _actionButton.Click += OnActionClick;
        }

        if (_closeButton is not null)
        {
            _closeButton.Click += OnCloseClick;
        }

        if (IsOpen)
        {
            if (!TryPlayPendingEntrance())
            {
                RibbonMotion.Rest(_presentationRoot);
            }
        }
        else if (IsPresented)
        {
            BeginHide();
        }
    }

    /// <summary>
    /// Closes an open message and raises <see cref="Dismissed"/>. Calling it again is a no-op.
    /// </summary>
    public void Dismiss()
    {
        if (!IsOpen)
        {
            return;
        }

        SetCurrentValue(IsOpenProperty, false);
        RaiseEvent(new RoutedEventArgs(DismissedEvent, this));
    }

    /// <inheritdoc />
    protected override AutomationPeer OnCreateAutomationPeer() => new RibbonMessageAutomationPeer(this);

    internal void SetIsLastOpenMessage(bool value) =>
        SetValue(IsLastOpenMessagePropertyKey, value);

    private void OnActionClick(object sender, RoutedEventArgs e) =>
        RaiseEvent(new RoutedEventArgs(ActionClickEvent, this));

    private void OnCloseClick(object sender, RoutedEventArgs e) => Dismiss();

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (!IsOpen)
        {
            SetIsPresented(false);
            return;
        }

        if (!_entranceHandledForCurrentOpen && !_entrancePending)
        {
            BeginShow();
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        // Template realization can produce an Unloaded/Loaded pair without changing IsOpen.
        // Preserve the per-open guard so that visual-tree churn cannot replay this entrance.
        // BeginHide is the sole reset point for the next logical open.
        _entrancePending = false;
        _transitionVersion++;
        RibbonMotion.Rest(_presentationRoot);
        SetIsPresented(IsOpen);
    }

    private void BeginShow()
    {
        _transitionVersion++;
        SetIsPresented(true);

        if (_entranceHandledForCurrentOpen || _entrancePending)
        {
            return;
        }

        if (!IsLoaded)
        {
            RibbonMotion.Rest(_presentationRoot);
            return;
        }

        if (!RibbonAnimation.IsEnabled(RibbonAnimationAction.MessageBar))
        {
            _entranceHandledForCurrentOpen = true;
            RibbonMotion.Rest(_presentationRoot);
            return;
        }

        _entrancePending = true;
        if (_presentationRoot is null)
        {
            // A row that began Collapsed may not have instantiated its template yet. Realize it
            // now; OnApplyTemplate consumes the pending entrance before the new root can render.
            ApplyTemplate();
        }

        _ = TryPlayPendingEntrance();
    }

    private void BeginHide()
    {
        _entranceHandledForCurrentOpen = false;
        _entrancePending = false;
        int version = ++_transitionVersion;
        if (!IsPresented)
        {
            return;
        }

        if (!IsLoaded)
        {
            RibbonMotion.Rest(_presentationRoot);
            SetIsPresented(false);
            return;
        }

        RibbonMotion.PlayClose(
            _presentationRoot,
            RibbonAnimationAction.MessageBar,
            RibbonSlideFrom.Top,
            () =>
            {
                if (version != _transitionVersion || IsOpen)
                {
                    return;
                }

                RibbonMotion.Rest(_presentationRoot);
                SetIsPresented(false);
            });
    }

    private bool TryPlayPendingEntrance()
    {
        if (!_entrancePending || _presentationRoot is null)
        {
            return false;
        }

        _entrancePending = false;
        _entranceHandledForCurrentOpen = true;
        if (!IsOpen || !IsLoaded || !RibbonAnimation.IsEnabled(RibbonAnimationAction.MessageBar))
        {
            RibbonMotion.Rest(_presentationRoot);
            return true;
        }

        // Start in the same dispatcher turn that made the row presentable. A Loaded-priority
        // hop runs after Render and can expose one fully-rested frame before the animation starts.
        RibbonMotion.PlayOpen(
            _presentationRoot,
            RibbonAnimationAction.MessageBar,
            RibbonSlideFrom.Top);
        return true;
    }

    private void SetIsPresented(bool value)
    {
        if (value == IsPresented)
        {
            return;
        }

        SetValue(IsPresentedPropertyKey, value);
        PresentationStateChanged?.Invoke(this, EventArgs.Empty);
    }

    private static void OnIsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var message = (RibbonMessage)d;
        if ((bool)e.NewValue)
        {
            message.BeginShow();
        }
        else
        {
            message.BeginHide();
        }
    }
}
