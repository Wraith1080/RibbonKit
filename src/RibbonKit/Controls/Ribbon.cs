using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Threading;
using RibbonKit.Animation;
using RibbonKit.Localization;
// Alias: WPF's legacy Microsoft ribbon declares identically-named peers in
// System.Windows.Automation.Peers, so the reference must be disambiguated.
using RibbonAutomationPeer = RibbonKit.Automation.RibbonAutomationPeer;

namespace RibbonKit.Controls;

/// <summary>
/// The root Ribbon control. Hosts the tab strip and the groups row of the selected tab.
/// Declare tabs directly as content:
/// <code language="xaml">
/// &lt;rk:Ribbon&gt;
///     &lt;rk:RibbonTab Header="Home"&gt; ... &lt;/rk:RibbonTab&gt;
/// &lt;/rk:Ribbon&gt;
/// </code>
/// </summary>
[ContentProperty(nameof(Tabs))]
[TemplatePart(Name = ApplicationMenuOverlayLayerPartName, Type = typeof(Canvas))]
[TemplatePart(Name = ApplicationMenuOverlayPresenterPartName, Type = typeof(ContentPresenter))]
[TemplatePart(Name = ApplicationButtonOverlayPartName, Type = typeof(Border))]
public class Ribbon : Control
{
    private static readonly DependencyPropertyKey TabsPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(Tabs),
            typeof(ObservableCollection<RibbonTab>),
            typeof(Ribbon),
            new FrameworkPropertyMetadata(null));

    /// <summary>Identifies the read-only <see cref="Tabs"/> dependency property.</summary>
    public static readonly DependencyProperty TabsProperty = TabsPropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey QuickAccessItemsPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(QuickAccessItems),
            typeof(ObservableCollection<object>),
            typeof(Ribbon),
            new FrameworkPropertyMetadata(null));

    /// <summary>Identifies the read-only <see cref="QuickAccessItems"/> dependency property.</summary>
    public static readonly DependencyProperty QuickAccessItemsProperty =
        QuickAccessItemsPropertyKey.DependencyProperty;

    /// <summary>Identifies the <see cref="QuickAccessPosition"/> dependency property.</summary>
    public static readonly DependencyProperty QuickAccessPositionProperty =
        DependencyProperty.Register(
            nameof(QuickAccessPosition),
            typeof(RibbonQuickAccessPosition),
            typeof(Ribbon),
            new FrameworkPropertyMetadata(
                RibbonQuickAccessPosition.TabRow,
                OnQuickAccessPositionChanged));

    /// <summary>Identifies the <see cref="QuickAccessMaxWidth"/> dependency property.</summary>
    public static readonly DependencyProperty QuickAccessMaxWidthProperty =
        DependencyProperty.Register(
            nameof(QuickAccessMaxWidth),
            typeof(double),
            typeof(Ribbon),
            new FrameworkPropertyMetadata(240d));

    /// <summary>Identifies the <see cref="IsMinimized"/> dependency property.</summary>
    public static readonly DependencyProperty IsMinimizedProperty =
        DependencyProperty.Register(
            nameof(IsMinimized),
            typeof(bool),
            typeof(Ribbon),
            new FrameworkPropertyMetadata(
                false,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnIsMinimizedChanged));

    /// <summary>Identifies the <see cref="IsBackstageOpen"/> dependency property.</summary>
    public static readonly DependencyProperty IsBackstageOpenProperty =
        DependencyProperty.Register(
            nameof(IsBackstageOpen),
            typeof(bool),
            typeof(Ribbon),
            new FrameworkPropertyMetadata(
                false,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnIsBackstageOpenChanged));

    /// <summary>Identifies the <see cref="Backstage"/> dependency property.</summary>
    public static readonly DependencyProperty BackstageProperty =
        DependencyProperty.Register(
            nameof(Backstage),
            typeof(object),
            typeof(Ribbon),
            new FrameworkPropertyMetadata(null, OnBackstageChanged));

    /// <summary>Identifies the <see cref="ApplicationMenu"/> dependency property.</summary>
    public static readonly DependencyProperty ApplicationMenuProperty =
        DependencyProperty.Register(
            nameof(ApplicationMenu),
            typeof(object),
            typeof(Ribbon),
            new FrameworkPropertyMetadata(null, OnApplicationMenuChanged));

    /// <summary>Identifies the <see cref="MessageBar"/> dependency property.</summary>
    public static readonly DependencyProperty MessageBarProperty =
        DependencyProperty.Register(
            nameof(MessageBar),
            typeof(RibbonMessageBar),
            typeof(Ribbon),
            new FrameworkPropertyMetadata(null, OnMessageBarChanged));

    private static readonly DependencyPropertyKey HasOpenMessagesPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(HasOpenMessages),
            typeof(bool),
            typeof(Ribbon),
            new FrameworkPropertyMetadata(false));

    /// <summary>Identifies the read-only <see cref="HasOpenMessages"/> dependency property.</summary>
    public static readonly DependencyProperty HasOpenMessagesProperty =
        HasOpenMessagesPropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey IsApplicationMenuOpenPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(IsApplicationMenuOpen),
            typeof(bool),
            typeof(Ribbon),
            new FrameworkPropertyMetadata(false, OnIsApplicationMenuOpenChanged));

    /// <summary>Identifies the read-only <see cref="IsApplicationMenuOpen"/> dependency property.</summary>
    public static readonly DependencyProperty IsApplicationMenuOpenProperty =
        IsApplicationMenuOpenPropertyKey.DependencyProperty;

    /// <summary>
    /// Identifies the design-tool-only <see cref="DesignPreviewFileSurface"/> dependency property.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static readonly DependencyProperty DesignPreviewFileSurfaceProperty =
        DependencyProperty.Register(
            nameof(DesignPreviewFileSurface),
            typeof(int),
            typeof(Ribbon),
            new FrameworkPropertyMetadata(-1, OnDesignPreviewFileSurfaceChanged));

    /// <summary>
    /// Identifies the design-tool-only <see cref="DesignPreviewTheme"/> dependency property.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static readonly DependencyProperty DesignPreviewThemeProperty =
        DependencyProperty.Register(
            nameof(DesignPreviewTheme),
            typeof(int),
            typeof(Ribbon),
            new FrameworkPropertyMetadata(-1, OnDesignPreviewThemeChanged));

    /// <summary>Identifies the <see cref="ApplicationButtonHeader"/> dependency property.</summary>
    public static readonly DependencyProperty ApplicationButtonHeaderProperty =
        DependencyProperty.Register(
            nameof(ApplicationButtonHeader),
            typeof(string),
            typeof(Ribbon),
            new FrameworkPropertyMetadata("File", OnApplicationButtonHeaderChanged));

    private static readonly DependencyPropertyKey EffectiveApplicationButtonHeaderPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(EffectiveApplicationButtonHeader),
            typeof(string),
            typeof(Ribbon),
            new FrameworkPropertyMetadata("File"));

    /// <summary>
    /// Identifies the read-only <see cref="EffectiveApplicationButtonHeader"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty EffectiveApplicationButtonHeaderProperty =
        EffectiveApplicationButtonHeaderPropertyKey.DependencyProperty;

    /// <summary>Identifies the <see cref="ApplicationButtonShape"/> dependency property.</summary>
    public static readonly DependencyProperty ApplicationButtonShapeProperty =
        DependencyProperty.Register(
            nameof(ApplicationButtonShape),
            typeof(RibbonApplicationButtonShape),
            typeof(Ribbon),
            new FrameworkPropertyMetadata(
                RibbonApplicationButtonShape.Tab,
                OnApplicationButtonShapeChanged));

    /// <summary>
    /// Attached flag the ribbon sets on a QAT button while it sits on a colored surface
    /// (an accent title bar, or the colored Office 2019 tab strip). The button template
    /// then draws its icon as a white silhouette and uses the brushes published under
    /// <see cref="QatColoredHoverBackgroundKey"/> / <see cref="QatColoredPressedBackgroundKey"/>
    /// for its hover/pressed states, so the QAT blends with the colored band like Office.
    /// </summary>
    public static readonly DependencyProperty QatOnColoredSurfaceProperty =
        DependencyProperty.RegisterAttached(
            "QatOnColoredSurface",
            typeof(bool),
            typeof(Ribbon),
            // Inherits so a split/dropdown proxy's nested primary/chevron parts can read it too
            // (their hover triggers live in nested templates, below the element the ribbon sets).
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits));

    /// <summary>Sets the <see cref="QatOnColoredSurfaceProperty"/> for an element.</summary>
    public static void SetQatOnColoredSurface(DependencyObject element, bool value) =>
        element.SetValue(QatOnColoredSurfaceProperty, value);

    /// <summary>Gets the <see cref="QatOnColoredSurfaceProperty"/> for an element.</summary>
    public static bool GetQatOnColoredSurface(DependencyObject element) =>
        (bool)element.GetValue(QatOnColoredSurfaceProperty);

    /// <summary>
    /// Resource key under which the ribbon publishes, on each QAT button, the hover brush to
    /// use while the button sits on a colored surface. Button templates consume it with
    /// <c>{DynamicResource}</c>, which resolves through the element tree and always yields a
    /// concrete brush — unlike bindings to inherited attached properties, which template
    /// children could not reliably read (see design notes §3.21).
    /// </summary>
    public const string QatColoredHoverBackgroundKey = "RibbonKit.Brushes.Qat.ColoredHoverBackground";

    /// <summary>
    /// Resource key for the pressed/checked companion of <see cref="QatColoredHoverBackgroundKey"/>,
    /// so pressing/toggling shows a stable "active" state (matches the caption buttons).
    /// </summary>
    public const string QatColoredPressedBackgroundKey = "RibbonKit.Brushes.Qat.ColoredPressedBackground";

    // Name of the application (File/orb) ToggleButton inside the RibbonTabControl template. The
    // application menu's click-outside dismissal has to recognise it and stand down, or closing on
    // mouse-DOWN would race the toggle's own click on mouse-UP and the orb would re-open the menu
    // it just closed. Matching by NAME rather than by a cached reference is deliberate: the button
    // lives in the NESTED tab control's template, which Ribbon.GetTemplateChild cannot reach.
    internal const string ApplicationButtonPartName = "PART_ApplicationButton";
    internal const string ApplicationMenuOverlayLayerPartName = "ApplicationMenuOverlayLayer";
    internal const string ApplicationMenuOverlayPresenterPartName = "PART_ApplicationMenuOverlayPresenter";
    internal const string ApplicationButtonOverlayPartName = "PART_ApplicationButtonOverlay";

    private const string ApplicationMenuAnchorBelowButtonResourceKey =
        "RibbonKit.Behaviors.ApplicationMenuAnchorBelowButton";

    /// <summary>Identifies the <see cref="SelectedTab"/> dependency property.</summary>
    public static readonly DependencyProperty SelectedTabProperty =
        DependencyProperty.Register(
            nameof(SelectedTab),
            typeof(RibbonTab),
            typeof(Ribbon),
            new FrameworkPropertyMetadata(
                null,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnSelectedTabChanged));

    /// <summary>Identifies the <see cref="SelectedIndex"/> dependency property.</summary>
    public static readonly DependencyProperty SelectedIndexProperty =
        DependencyProperty.Register(
            nameof(SelectedIndex),
            typeof(int),
            typeof(Ribbon),
            new FrameworkPropertyMetadata(
                -1,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnSelectedIndexChanged));

    private static readonly DependencyPropertyKey ModalTabPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(ModalTab),
            typeof(RibbonTab),
            typeof(Ribbon),
            new FrameworkPropertyMetadata(null));

    /// <summary>Identifies the read-only <see cref="ModalTab"/> dependency property.</summary>
    public static readonly DependencyProperty ModalTabProperty = ModalTabPropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey IsModalPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(IsModal),
            typeof(bool),
            typeof(Ribbon),
            new FrameworkPropertyMetadata(false));

    /// <summary>Identifies the read-only <see cref="IsModal"/> dependency property.</summary>
    public static readonly DependencyProperty IsModalProperty = IsModalPropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey CanCloseModalPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(CanCloseModal),
            typeof(bool),
            typeof(Ribbon),
            new FrameworkPropertyMetadata(false));

    /// <summary>Identifies the read-only <see cref="CanCloseModal"/> dependency property.</summary>
    public static readonly DependencyProperty CanCloseModalProperty = CanCloseModalPropertyKey.DependencyProperty;

    static Ribbon()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(Ribbon),
            new FrameworkPropertyMetadata(typeof(Ribbon)));
    }

    /// <summary>Initializes a new ribbon with an empty <see cref="Tabs"/> collection.</summary>
    public Ribbon()
    {
        // Created first: the minimize/backstage guards consult the scope from property-changed
        // callbacks that can run as soon as the object is initialized from XAML.
        _modalScope = new RibbonModalScope(this);
        _exitModalCommand = new ModalCloseCommand(this);
        _mergeService = new RibbonMergeService(this);
        _mergedCaptionCommand = new CaptionActionCommand(this);

        var tabs = new ObservableCollection<RibbonTab>();
        tabs.CollectionChanged += OnTabsCollectionChanged;
        SetValue(TabsPropertyKey, tabs);
        var quickAccessItems = new ObservableCollection<object>();
        quickAccessItems.CollectionChanged += (_, _) => UpdateQatButtonContext();
        SetValue(QuickAccessItemsPropertyKey, quickAccessItems);
        _keyTipService = new KeyTipService(this);
        PropertyChangedEventManager.AddHandler(
            RibbonLocalizationBindingSource.Instance,
            OnLocalizationBindingSourceChanged,
            "Item[]");
        UpdateEffectiveApplicationButtonHeader();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        LayoutUpdated += OnLayoutUpdated;
    }

    /// <summary>The tabs hosted by this ribbon.</summary>
    public ObservableCollection<RibbonTab> Tabs =>
        (ObservableCollection<RibbonTab>)GetValue(TabsProperty);

    /// <summary>
    /// Small controls (typically <see cref="RibbonButton"/>s with
    /// <c>Size="Small"</c>) shown in the quick access strip next to the application
    /// button — Save/Undo/Redo territory. Moves into the title bar once RibbonWindow
    /// chrome integration lands.
    /// </summary>
    public ObservableCollection<object> QuickAccessItems =>
        (ObservableCollection<object>)GetValue(QuickAccessItemsProperty);

    /// <summary>
    /// Where <see cref="QuickAccessItems"/> render: in the tab strip row (default) or
    /// in a full-width row below the ribbon, like classic Office's
    /// "Show Quick Access Toolbar below the Ribbon".
    /// </summary>
    public RibbonQuickAccessPosition QuickAccessPosition
    {
        get => (RibbonQuickAccessPosition)GetValue(QuickAccessPositionProperty);
        set => SetValue(QuickAccessPositionProperty, value);
    }

    /// <summary>
    /// How wide the quick access strip may grow in the placements that share their row —
    /// <see cref="RibbonQuickAccessPosition.TabRow"/> (competing with the tabs) and
    /// <see cref="RibbonQuickAccessPosition.TitleBar"/> (competing with the window title).
    /// Items past the cap move into the strip's overflow flyout. Default 240 DIPs, roughly eight
    /// small buttons.
    /// <para>
    /// Ignored for <see cref="RibbonQuickAccessPosition.BelowRibbon"/>: that placement owns a
    /// full-width row of its own, so it stretches instead of overflowing.
    /// </para>
    /// </summary>
    public double QuickAccessMaxWidth
    {
        get => (double)GetValue(QuickAccessMaxWidthProperty);
        set => SetValue(QuickAccessMaxWidthProperty, value);
    }

    /// <summary>
    /// Whether the ribbon is minimized to just its tab strip. Toggled by double-clicking
    /// a tab header or the chevron button at the right end of the tab strip.
    /// </summary>
    public bool IsMinimized
    {
        get => (bool)GetValue(IsMinimizedProperty);
        set => SetValue(IsMinimizedProperty, value);
    }

    /// <summary>The currently selected tab, or <see langword="null"/> when none is selected.</summary>
    public RibbonTab? SelectedTab
    {
        get => (RibbonTab?)GetValue(SelectedTabProperty);
        set => SetValue(SelectedTabProperty, value);
    }

    /// <summary>
    /// The index of the selected tab within <see cref="Tabs"/> (a convenience over
    /// <see cref="SelectedTab"/>, kept in sync with it in both directions). <c>-1</c> when no
    /// tab is selected. Especially useful for <b>design-time preview</b>: set a design-time-only
    /// <c>d:SelectedIndex="2"</c> on the ribbon to view a specific tab's content on the XAML
    /// designer surface without changing the runtime selection.
    /// </summary>
    public int SelectedIndex
    {
        get => (int)GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

    // ==========================================================================
    // Tab merging. A child context (embedded editor, MDI child, plug-in) hands the
    // host ribbon a RibbonMergeSource; its tabs join the strip while the child is
    // active and leave when it isn't. Bookkeeping lives in RibbonMergeService.
    // ==========================================================================

    private readonly RibbonMergeService _mergeService;

    private static readonly DependencyPropertyKey IsMergedPropertyKey =
        DependencyProperty.RegisterAttachedReadOnly(
            "IsMerged",
            typeof(bool),
            typeof(Ribbon),
            new FrameworkPropertyMetadata(false));

    /// <summary>
    /// Identifies the read-only <c>IsMerged</c> attached property: whether a tab reached this
    /// ribbon through <see cref="Merge"/> rather than being declared on it. Merged content is
    /// excluded from ribbon customization — see <see cref="RibbonMergeSource"/>.
    /// </summary>
    public static readonly DependencyProperty IsMergedProperty = IsMergedPropertyKey.DependencyProperty;

    /// <summary>Whether the element was contributed by a <see cref="RibbonMergeSource"/>.</summary>
    public static bool GetIsMerged(DependencyObject element) =>
        (bool)element.GetValue(IsMergedProperty);

    internal static void SetIsMergedInternal(DependencyObject element, bool value) =>
        element.SetValue(IsMergedPropertyKey, value);

    private static readonly DependencyPropertyKey MergeSourcePropertyKey =
        DependencyProperty.RegisterAttachedReadOnly(
            "MergeSource",
            typeof(RibbonMergeSource),
            typeof(Ribbon),
            new FrameworkPropertyMetadata(null));

    /// <summary>
    /// Identifies the read-only <c>MergeSource</c> attached property: the source that contributed
    /// this element, or <see langword="null"/> for host-declared content.
    /// </summary>
    public static readonly DependencyProperty MergeSourceProperty = MergeSourcePropertyKey.DependencyProperty;

    /// <summary>The source that contributed this element, or <see langword="null"/>.</summary>
    public static RibbonMergeSource? GetMergeSource(DependencyObject element) =>
        (RibbonMergeSource?)element.GetValue(MergeSourceProperty);

    internal static void SetMergeSourceInternal(DependencyObject element, RibbonMergeSource? value) =>
        element.SetValue(MergeSourcePropertyKey, value);

    /// <summary>The merge sources currently contributing to this ribbon, in merge order.</summary>
    public IReadOnlyList<RibbonMergeSource> MergedSources => _mergeService.Sources;

    /// <summary>
    /// Inserts <paramref name="source"/>'s tabs into this ribbon. Position follows
    /// <see cref="RibbonMergeSource.Order"/> — host-declared tabs come first, then merged sources
    /// by order and then by when they first merged, so repeated merge/unmerge cycles are stable.
    /// </summary>
    /// <returns><see langword="false"/> when the source is already merged into this ribbon.</returns>
    /// <exception cref="InvalidOperationException">The source is merged into a different ribbon.</exception>
    public bool Merge(RibbonMergeSource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return _mergeService.Merge(source);
    }

    /// <summary>
    /// Removes the tabs <paramref name="source"/> contributed, restoring the ribbon to its
    /// host-declared state. Unmerging the tab currently held modal also ends modal mode.
    /// </summary>
    /// <returns><see langword="false"/> when the source is not merged into this ribbon.</returns>
    public bool Unmerge(RibbonMergeSource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return _mergeService.Unmerge(source);
    }

    /// <summary>Whether <paramref name="source"/> is currently merged into this ribbon.</summary>
    public bool IsMerged(RibbonMergeSource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return _mergeService.IsMerged(source);
    }

    // Used by RibbonCustomizationSerializer.Apply, which rebuilds Tabs wholesale: merged tabs step
    // out for the rebuild and step back in afterwards, rather than the service trying to re-assert
    // positions inside a collection that was cleared underneath it.
    internal List<RibbonMergeSource> UnmergeAllForRebuild() => _mergeService.UnmergeAll();

    internal void RemergeAfterRebuild(List<RibbonMergeSource> sources) => _mergeService.Remerge(sources);

    /// <summary>
    /// Called after any merge or unmerge. Keeps the selection valid and re-places the selection
    /// visuals: the sliding underline and the 2010/2013 body-border notch are positioned from the
    /// selected tab's transform, and adding or removing tabs raises no selection or size event —
    /// the same reason a theme swap and a modal transition re-place them explicitly (§3.29).
    /// </summary>
    internal void OnMergeChanged()
    {
        if (SelectedTab is null || !Tabs.Contains(SelectedTab) || SelectedTab.Visibility != Visibility.Visible)
        {
            SelectedTab = FindFirstVisibleTab();
        }

        RefreshSelectionVisuals();
    }

    /// <summary>
    /// Re-places the sliding selection underline and the 2010/2013 body-border notch.
    /// </summary>
    /// <remarks>
    /// Both are positioned from the SELECTED TAB'S TRANSFORM, so anything that moves the tab strip
    /// without raising a selection change or a size change on the tab control leaves them stranded.
    /// The tab control's own <c>SizeChanged</c> does NOT cover this: the strip lives in a star-width
    /// column, so a sibling in the same row growing or shrinking (the merged-caption icon appearing,
    /// the File button hiding for a modal tab) re-lays-out the strip while the tab control's size is
    /// unchanged. Every such caller must land here — layout is forced first so the transform is
    /// current when the marker and notch are measured (see design notes §3.29).
    /// </remarks>
    private void RefreshSelectionVisuals()
    {
        if (_ribbonTabControl is { } tabControl)
        {
            tabControl.InvalidateArrange();
            tabControl.UpdateLayout();
            tabControl.RefreshSelectionVisuals();
        }
    }

    // ==========================================================================
    // Merged caption. Classic MDI, when a child is maximized, moves the child's
    // system icon to the far left of the ribbon row and its window buttons to the
    // far right, and hides the child's own title bar. The ribbon just offers the
    // PLACEMENT and reports button presses; it deliberately knows nothing about
    // MDI (docs/05-MDI-EMULATION-PLAN.md §4 — caption merge and tab merge are
    // separate features and must stay that way).
    // ==========================================================================

    private static readonly DependencyPropertyKey HasMergedCaptionPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(HasMergedCaption),
            typeof(bool),
            typeof(Ribbon),
            new FrameworkPropertyMetadata(false));

    /// <summary>Identifies the read-only <see cref="HasMergedCaption"/> dependency property.</summary>
    public static readonly DependencyProperty HasMergedCaptionProperty = HasMergedCaptionPropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey MergedCaptionIconPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(MergedCaptionIcon),
            typeof(System.Windows.Media.ImageSource),
            typeof(Ribbon),
            new FrameworkPropertyMetadata(null));

    /// <summary>Identifies the read-only <see cref="MergedCaptionIcon"/> dependency property.</summary>
    public static readonly DependencyProperty MergedCaptionIconProperty = MergedCaptionIconPropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey MergedCaptionTitlePropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(MergedCaptionTitle),
            typeof(string),
            typeof(Ribbon),
            new FrameworkPropertyMetadata(null));

    /// <summary>Identifies the read-only <see cref="MergedCaptionTitle"/> dependency property.</summary>
    public static readonly DependencyProperty MergedCaptionTitleProperty = MergedCaptionTitlePropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey MergedCaptionCanClosePropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(MergedCaptionCanClose),
            typeof(bool),
            typeof(Ribbon),
            new FrameworkPropertyMetadata(false));

    /// <summary>Identifies the read-only <see cref="MergedCaptionCanClose"/> dependency property.</summary>
    public static readonly DependencyProperty MergedCaptionCanCloseProperty = MergedCaptionCanClosePropertyKey.DependencyProperty;

    private readonly CaptionActionCommand _mergedCaptionCommand;

    /// <summary>Whether a caption is currently merged into the ribbon row.</summary>
    public bool HasMergedCaption => (bool)GetValue(HasMergedCaptionProperty);

    /// <summary>The merged caption's icon, or <see langword="null"/>.</summary>
    public System.Windows.Media.ImageSource? MergedCaptionIcon =>
        (System.Windows.Media.ImageSource?)GetValue(MergedCaptionIconProperty);

    /// <summary>
    /// The merged caption's title. Deliberately NOT drawn in the tab strip — classic MDI puts it
    /// in the host window's own title bar ("Document1 - MyApp"), and a second title in the strip
    /// would eat tab space. Exposed so a host can bind its window title to it.
    /// </summary>
    public string? MergedCaptionTitle => (string?)GetValue(MergedCaptionTitleProperty);

    /// <summary>
    /// Whether a merged caption is present AND offers a close button — the close button's
    /// visibility binds to this alone, so it is <see langword="false"/> when no caption is docked.
    /// </summary>
    public bool MergedCaptionCanClose => (bool)GetValue(MergedCaptionCanCloseProperty);

    /// <summary>
    /// Command the merged caption's buttons invoke, with a
    /// <see cref="RibbonMergedCaptionAction"/> as parameter. Raises
    /// <see cref="MergedCaptionActionRequested"/>; also usable from an app's own chrome.
    /// </summary>
    public System.Windows.Input.ICommand MergedCaptionCommand => _mergedCaptionCommand;

    /// <summary>
    /// Raised when the user presses a button on the merged caption. The host that called
    /// <see cref="ShowMergedCaption"/> decides what minimize / restore / close mean.
    /// </summary>
    public event EventHandler<RibbonMergedCaptionEventArgs>? MergedCaptionActionRequested;

    /// <summary>
    /// Docks a caption into the ribbon row: <paramref name="icon"/> at the far left, before the
    /// application button, and minimize / restore / close buttons at the far right. The caller is
    /// responsible for hiding whatever chrome the caption replaces, and for the title (see
    /// <see cref="MergedCaptionTitle"/>).
    /// </summary>
    /// <param name="icon">The system icon to show, or <see langword="null"/> for none. An
    /// <c>ImageSource</c> rather than an element: the same icon is typically already displayed
    /// elsewhere, and a UIElement cannot have two visual parents.</param>
    /// <param name="title">The caption text, published on <see cref="MergedCaptionTitle"/>.</param>
    /// <param name="canClose">Whether to offer a close button.</param>
    public void ShowMergedCaption(System.Windows.Media.ImageSource? icon, string? title, bool canClose = true)
    {
        SetValue(MergedCaptionIconPropertyKey, icon);
        SetValue(MergedCaptionTitlePropertyKey, title);
        SetValue(MergedCaptionCanClosePropertyKey, canClose);
        SetValue(HasMergedCaptionPropertyKey, true);

        // The icon appearing at the left of the row shifts the whole tab strip sideways, and the
        // buttons at the right narrow it — but the tab control itself doesn't change size, so
        // nothing re-places the underline or the connect notch on its own.
        RefreshSelectionVisuals();
    }

    /// <summary>Removes a caption previously docked by <see cref="ShowMergedCaption"/>.</summary>
    public void ClearMergedCaption()
    {
        SetValue(HasMergedCaptionPropertyKey, false);
        SetValue(MergedCaptionIconPropertyKey, null);
        SetValue(MergedCaptionTitlePropertyKey, null);
        SetValue(MergedCaptionCanClosePropertyKey, false);

        // Same shift in reverse — the strip widens back out as the icon and buttons go away.
        RefreshSelectionVisuals();
    }

    // One command taking the action as its parameter, rather than three commands and three
    // events: the buttons differ only in which action they name.
    private sealed class CaptionActionCommand : System.Windows.Input.ICommand
    {
        private readonly Ribbon _ribbon;

        internal CaptionActionCommand(Ribbon ribbon) => _ribbon = ribbon;

        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }

        public bool CanExecute(object? parameter) => parameter is RibbonMergedCaptionAction;

        public void Execute(object? parameter)
        {
            if (parameter is RibbonMergedCaptionAction action)
            {
                _ribbon.MergedCaptionActionRequested?.Invoke(
                    _ribbon, new RibbonMergedCaptionEventArgs(action));
            }
        }
    }

    // ==========================================================================
    // Modal tabs (Print-Preview-style exclusive mode). State machine lives in
    // RibbonModalScope; this region is the public surface plus the guards that
    // keep minimize and the backstage out of reach while modal.
    // ==========================================================================

    private readonly RibbonModalScope _modalScope;
    private readonly ModalCloseCommand _exitModalCommand;

    // Set while a guard is reverting an attempt to minimize / open the backstage during modal
    // mode, so the revert's own property-changed notification doesn't run the normal path.
    private bool _suppressMinimizeChange;
    private bool _suppressBackstageChange;

    /// <summary>
    /// The tab currently held modal, or <see langword="null"/> when the ribbon is in its normal
    /// state. See <see cref="EnterModal"/>.
    /// </summary>
    public RibbonTab? ModalTab => (RibbonTab?)GetValue(ModalTabProperty);

    /// <summary>Whether a modal tab is active (a convenience over <see cref="ModalTab"/>).</summary>
    public bool IsModal => (bool)GetValue(IsModalProperty);

    /// <summary>
    /// Whether the modal tab currently active offers a close affordance
    /// (<see cref="RibbonTab.CanClose"/>). The tab strip binds its close button's visibility here.
    /// </summary>
    public bool CanCloseModal => (bool)GetValue(CanCloseModalProperty);

    /// <summary>
    /// Command that leaves modal mode with <see cref="RibbonModalReason.CloseButton"/>. Bound by
    /// the tab strip's close affordance; also usable from an app's own "Close Preview" button.
    /// </summary>
    public System.Windows.Input.ICommand ExitModalCommand => _exitModalCommand;

    /// <summary>
    /// Raised before a tab enters modal mode. Set <see cref="RibbonModalEventArgs.Cancel"/> to
    /// refuse (for example, when there is nothing to preview).
    /// </summary>
    public event EventHandler<RibbonModalEventArgs>? ModalEntering;

    /// <summary>Raised after a tab has entered modal mode.</summary>
    public event EventHandler<RibbonModalEventArgs>? ModalEntered;

    /// <summary>
    /// Raised before modal mode ends. Set <see cref="RibbonModalEventArgs.Cancel"/> to refuse —
    /// except when <see cref="RibbonModalEventArgs.Reason"/> is
    /// <see cref="RibbonModalReason.TabRemoved"/>, where the tab is already gone.
    /// </summary>
    public event EventHandler<RibbonModalEventArgs>? ModalExiting;

    /// <summary>Raised after modal mode has ended.</summary>
    public event EventHandler<RibbonModalEventArgs>? ModalExited;

    /// <summary>
    /// Enters modal mode on <paramref name="tab"/>: every other tab and the application (File)
    /// button hide, minimizing the ribbon and opening the backstage are blocked, and the quick
    /// access toolbar stays — Word's Print Preview behaviour. The tab must belong to this ribbon
    /// and have <see cref="RibbonTab.IsModal"/> set.
    /// </summary>
    /// <returns>
    /// <see langword="false"/> when a <see cref="ModalEntering"/> handler cancelled, or when an
    /// already-modal tab refused to leave first.
    /// </returns>
    public bool EnterModal(RibbonTab tab)
    {
        ArgumentNullException.ThrowIfNull(tab);

        if (!Tabs.Contains(tab))
        {
            throw new ArgumentException("The tab does not belong to this ribbon.", nameof(tab));
        }

        if (!tab.IsModal)
        {
            throw new ArgumentException(
                $"The tab '{tab.Header}' is not marked {nameof(RibbonTab.IsModal)}.", nameof(tab));
        }

        return _modalScope.Enter(tab, RibbonModalReason.Application);
    }

    /// <summary>
    /// Leaves modal mode, restoring every tab's pre-modal visibility and the previously selected
    /// tab. No-op when not modal.
    /// </summary>
    /// <returns><see langword="false"/> when a <see cref="ModalExiting"/> handler cancelled.</returns>
    public bool ExitModal() => ExitModal(RibbonModalReason.Application);

    /// <summary>
    /// Leaves modal mode, attributing the transition to <paramref name="reason"/> so handlers can
    /// tell a user-initiated close from an application-driven one.
    /// </summary>
    /// <returns><see langword="false"/> when a <see cref="ModalExiting"/> handler cancelled.</returns>
    public bool ExitModal(RibbonModalReason reason) => _modalScope.Exit(reason, force: false);

    /// <summary>
    /// The visibility <paramref name="tab"/> would have if modal mode were not active. Modal mode
    /// hides the other tabs with <see cref="UIElement.Visibility"/>, so anything that PERSISTS
    /// visibility must read it through here — otherwise state saved during Print Preview restores
    /// a ribbon with every tab hidden. <see cref="RibbonCustomizationSerializer"/> uses it.
    /// </summary>
    public Visibility GetAuthoredVisibility(RibbonTab tab)
    {
        ArgumentNullException.ThrowIfNull(tab);
        return _modalScope.GetAuthoredVisibility(tab);
    }

    /// <summary>
    /// Sets the visibility <paramref name="tab"/> should have once modal mode ends. While the tab
    /// is held hidden by a modal tab the change is deferred to the exit; otherwise it applies
    /// immediately. Use this instead of setting <see cref="UIElement.Visibility"/> directly when
    /// application state (a contextual tab's context) changes during modal mode.
    /// </summary>
    public void SetAuthoredVisibility(RibbonTab tab, Visibility value)
    {
        ArgumentNullException.ThrowIfNull(tab);
        _modalScope.SetAuthoredVisibility(tab, value);
    }

    internal bool RaiseModalEntering(RibbonTab tab, RibbonModalReason reason)
    {
        if (ModalEntering is null)
        {
            return true;
        }

        var args = new RibbonModalEventArgs(tab, reason);
        ModalEntering(this, args);
        return !args.Cancel;
    }

    internal void RaiseModalEntered(RibbonTab tab, RibbonModalReason reason) =>
        ModalEntered?.Invoke(this, new RibbonModalEventArgs(tab, reason));

    internal bool RaiseModalExiting(RibbonTab tab, RibbonModalReason reason)
    {
        if (ModalExiting is null)
        {
            return true;
        }

        var args = new RibbonModalEventArgs(tab, reason);
        ModalExiting(this, args);
        return !args.Cancel;
    }

    internal void RaiseModalExited(RibbonTab tab, RibbonModalReason reason) =>
        ModalExited?.Invoke(this, new RibbonModalEventArgs(tab, reason));

    /// <summary>
    /// Publishes the scope's state onto the read-only DPs the templates bind to, then re-places the
    /// selection visuals. The sliding underline and the 2010/2013 body-border notch are positioned
    /// from the selected tab's transform, and a modal transition changes which tabs are laid out
    /// without raising a selection or size event — so they must be re-placed explicitly, exactly as
    /// a theme swap does (see design notes §3.29).
    /// </summary>
    internal void OnModalStateChanged()
    {
        RibbonTab? modal = _modalScope.ModalTab;
        SetValue(ModalTabPropertyKey, modal);
        SetValue(IsModalPropertyKey, modal is not null);
        SetValue(CanCloseModalPropertyKey, modal is { CanClose: true });
        _exitModalCommand.RaiseCanExecuteChanged();

        if (modal is not null)
        {
            // Entering: leave any state the mode forbids, through the normal (animated) paths.
            SetCurrentValue(IsBackstageOpenProperty, false);
            SetCurrentValue(IsMinimizedProperty, false);
        }

        RefreshSelectionVisuals();
    }

    // Bound by the tab strip's modal close affordance. A tiny ICommand rather than a
    // RoutedUICommand: the target is unambiguous (the ribbon that owns the template), so command
    // routing would only add a way to get it wrong.
    private sealed class ModalCloseCommand : System.Windows.Input.ICommand
    {
        private readonly Ribbon _ribbon;

        internal ModalCloseCommand(Ribbon ribbon) => _ribbon = ribbon;

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => _ribbon.CanCloseModal;

        public void Execute(object? parameter) => _ribbon.ExitModal(RibbonModalReason.CloseButton);

        internal void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    private static readonly DependencyPropertyKey QuickAccessSourcePropertyKey =
        DependencyProperty.RegisterAttachedReadOnly(
            "QuickAccessSource",
            typeof(FrameworkElement),
            typeof(Ribbon),
            new FrameworkPropertyMetadata(null));

    /// <summary>
    /// Identifies the read-only <c>QuickAccessSource</c> attached property: on a quick-access
    /// proxy created by <see cref="AddToQuickAccess"/>, the ribbon control it mirrors.
    /// </summary>
    public static readonly DependencyProperty QuickAccessSourceProperty =
        QuickAccessSourcePropertyKey.DependencyProperty;

    /// <summary>Gets the ribbon control a quick-access proxy mirrors, or <see langword="null"/>
    /// for hand-declared quick-access items.</summary>
    public static FrameworkElement? GetQuickAccessSource(DependencyObject element) =>
        (FrameworkElement?)element.GetValue(QuickAccessSourceProperty);

    private static readonly DependencyPropertyKey IsCommandParkedPropertyKey =
        DependencyProperty.RegisterAttachedReadOnly(
            "IsCommandParked",
            typeof(bool),
            typeof(Ribbon),
            new FrameworkPropertyMetadata(false));

    /// <summary>
    /// Identifies the read-only <c>IsCommandParked</c> attached property: set on a proxy whose
    /// merged source has stepped out, so it greys like Office instead of disappearing.
    /// </summary>
    public static readonly DependencyProperty IsCommandParkedProperty =
        IsCommandParkedPropertyKey.DependencyProperty;

    /// <summary>Whether a proxy is parked because the merged source it mirrors is not present.</summary>
    public static bool GetIsCommandParked(DependencyObject element) =>
        (bool)element.GetValue(IsCommandParkedProperty);

    /// <summary>
    /// Parks or revives a proxy. Feeds the proxy's enabled state through the same MultiBinding as
    /// the source's own <see cref="UIElement.IsEnabled"/> — see <see cref="CreateCommandProxy"/> for
    /// why the two must not be separate writes to the same property.
    /// </summary>
    internal static void SetIsCommandParkedInternal(DependencyObject element, bool value) =>
        element.SetValue(IsCommandParkedPropertyKey, value);

    private static readonly DependencyPropertyKey QuickAccessOverflowItemPropertyKey =
        DependencyProperty.RegisterAttachedReadOnly(
            "QuickAccessOverflowItem",
            typeof(FrameworkElement),
            typeof(Ribbon),
            new FrameworkPropertyMetadata(null));

    /// <summary>
    /// Identifies the read-only <c>QuickAccessOverflowItem</c> attached property: on a proxy shown
    /// in the quick access toolbar's OVERFLOW flyout, the real <see cref="QuickAccessItems"/> entry
    /// it stands for. Lets a right-click inside the flyout act on the real item.
    /// </summary>
    public static readonly DependencyProperty QuickAccessOverflowItemProperty =
        QuickAccessOverflowItemPropertyKey.DependencyProperty;

    /// <summary>The quick-access item an overflow-flyout proxy represents, or <see langword="null"/>.</summary>
    public static FrameworkElement? GetQuickAccessOverflowItem(DependencyObject element) =>
        (FrameworkElement?)element.GetValue(QuickAccessOverflowItemProperty);

    internal static void SetQuickAccessOverflowItemInternal(DependencyObject element, FrameworkElement? value) =>
        element.SetValue(QuickAccessOverflowItemPropertyKey, value);

    /// <summary>
    /// Raised when the user picks "Customize Quick Access Toolbar…" from a right-click menu.
    /// The application typically responds by opening a <see cref="RibbonOptionsDialog"/>
    /// containing a <see cref="RibbonQuickAccessPage"/> (plus its own options pages).
    /// </summary>
    public event EventHandler? QuickAccessCustomizeRequested;

    /// <summary>
    /// Raised when the user picks "Customize the Ribbon…" from a right-click menu. The
    /// application typically responds by opening a <see cref="RibbonOptionsDialog"/>
    /// containing a <see cref="RibbonCustomizePage"/>.
    /// </summary>
    public event EventHandler? RibbonCustomizeRequested;

    /// <summary>
    /// Identifies the <c>IsCustom</c> attached property: marks a <see cref="RibbonTab"/> or
    /// <see cref="RibbonGroup"/> as user-created (or user-editable). The customize page
    /// (<see cref="RibbonCustomizePage"/>) only allows destructive operations — removing, and
    /// adding commands into — on custom containers, mirroring Office's rules. Tabs/groups the
    /// page creates are marked automatically; an app may pre-mark its own XAML-declared ones
    /// to make them user-editable.
    /// </summary>
    public static readonly DependencyProperty IsCustomProperty =
        DependencyProperty.RegisterAttached(
            "IsCustom",
            typeof(bool),
            typeof(Ribbon),
            new FrameworkPropertyMetadata(false));

    /// <summary>Marks an element as user-created/user-editable for the customize page.</summary>
    public static void SetIsCustom(DependencyObject element, bool value) =>
        element.SetValue(IsCustomProperty, value);

    /// <summary>Whether the element is user-created/user-editable (see <see cref="IsCustomProperty"/>).</summary>
    public static bool GetIsCustom(DependencyObject element) =>
        (bool)element.GetValue(IsCustomProperty);

    /// <summary>
    /// Identifies the <c>CommandId</c> attached property: a stable string identity used by
    /// <see cref="RibbonCustomizationSerializer"/> to persist and restore customization across
    /// runs. Assign a unique id to each built-in tab, group, and command an app wants
    /// persistable (proxies reference their source command by this id, so a saved custom group
    /// re-finds its commands even though the proxy objects don't survive). Custom tabs/groups
    /// created by <see cref="RibbonCustomizePage"/> auto-get a generated id.
    /// </summary>
    public static readonly DependencyProperty CommandIdProperty =
        DependencyProperty.RegisterAttached(
            "CommandId",
            typeof(string),
            typeof(Ribbon),
            new FrameworkPropertyMetadata(null));

    /// <summary>Sets the persistence identity of a tab/group/command (see <see cref="CommandIdProperty"/>).</summary>
    public static void SetCommandId(DependencyObject element, string? value) =>
        element.SetValue(CommandIdProperty, value);

    /// <summary>Gets the persistence identity of a tab/group/command, or <see langword="null"/>.</summary>
    public static string? GetCommandId(DependencyObject element) =>
        (string?)element.GetValue(CommandIdProperty);

    internal void RaiseRibbonCustomizeRequested() =>
        RibbonCustomizeRequested?.Invoke(this, EventArgs.Empty);

    /// <summary>
    /// Adds <paramref name="source"/> (a button, toggle, split button, or drop-down button living
    /// in a ribbon group) to the quick access toolbar. Because a WPF element can only have one
    /// visual parent, the control is not moved: a small PROXY button is created that mirrors its
    /// 16px icon and ScreenTip and invokes it (toggles stay state-synced via a two-way IsChecked
    /// binding). Returns <see langword="false"/> when the control is already in the QAT or its type
    /// does not have a supported quick-access representation.
    /// </summary>
    public bool AddToQuickAccess(FrameworkElement source)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (!IsSupportedQuickAccessSource(source) || IsInQuickAccess(source))
        {
            return false;
        }

        FrameworkElement proxy = CreateCommandProxy(source, RibbonControlSize.Small);

        // Carry the command's merge provenance onto the proxy. That's what lets the proxy be
        // PARKED (disabled, like Office greys an unavailable command) instead of orphaned when
        // the source unmerges, and keeps it out of persisted state — a proxy of a transient
        // child's command must not come back on the next run pointing at nothing.
        if (GetMergeSource(source) is { } mergeSource)
        {
            SetMergeSourceInternal(proxy, mergeSource);
        }

        QuickAccessItems.Add(proxy);
        return true;
    }

    private static bool IsSupportedQuickAccessSource(FrameworkElement source) =>
        source is RibbonButton or RibbonToggleButton or RibbonDropDownButton;

    /// <summary>Whether <paramref name="source"/> is already in the quick access toolbar,
    /// either directly or via a proxy created by <see cref="AddToQuickAccess"/>.</summary>
    public bool IsInQuickAccess(FrameworkElement source) =>
        QuickAccessItems.Any(item =>
            ReferenceEquals(item, source)
            || (item is DependencyObject d && ReferenceEquals(GetQuickAccessSource(d), source)));

    /// <summary>
    /// Creates a proxy button mirroring <paramref name="source"/>'s icon/ScreenTip that invokes
    /// it, at the given <paramref name="size"/>. Small proxies serve the quick access toolbar;
    /// Medium ones (icon + label) serve custom ribbon groups built by the customize page.
    /// </summary>
    internal FrameworkElement CreateCommandProxy(FrameworkElement source, RibbonControlSize size)
    {
        FrameworkElement proxy;
        switch (source)
        {
            case RibbonToggleButton toggle:
            {
                // State lives on the SOURCE: the proxy's IsChecked is two-way bound to it, so
                // clicking either updates both and the source's Checked/Unchecked handlers run.
                var proxyToggle = new RibbonToggleButton
                {
                    Size = size,
                    Icon = toggle.Icon ?? toggle.LargeIcon,
                    LargeIcon = toggle.LargeIcon ?? toggle.Icon,
                    // Small-sized sources often have no Header (icon-only); fall back to the
                    // ScreenTip title minus its "(Ctrl+B)"-style shortcut, so a Medium/Large
                    // proxy still gets a label.
                    Header = toggle.Header ?? StripShortcutSuffix(toggle.ScreenTipTitle),
                    ScreenTipTitle = toggle.ScreenTipTitle ?? toggle.Header,
                    ScreenTipText = toggle.ScreenTipText,
                };
                proxyToggle.SetBinding(
                    System.Windows.Controls.Primitives.ToggleButton.IsCheckedProperty,
                    new System.Windows.Data.Binding(nameof(RibbonToggleButton.IsChecked))
                    {
                        Source = toggle,
                        Mode = System.Windows.Data.BindingMode.TwoWay,
                    });

                // Also raise the source's Click so a Click-wired action runs (the two-way binding above
                // only fires the source's Checked/Unchecked). This makes clicking the proxy equivalent to
                // clicking the source. RaiseEvent does NOT re-toggle IsChecked (the binding already did),
                // so there's no double-toggle; by the time this runs the source's IsChecked is updated.
                proxyToggle.Click += (_, _) =>
                    toggle.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent, toggle));

                proxy = proxyToggle;
                break;
            }

            case RibbonSplitButton split:
            {
                // A real split proxy: the primary part invokes the source's primary action, and
                // the chevron opens the source's menu under the proxy (borrowed, like the dropdown
                // proxy) — so the dropdown IS included in the QAT, matching Office.
                var proxySplit = new RibbonSplitButton
                {
                    Size = size,
                    Icon = split.Icon ?? split.LargeIcon,
                    LargeIcon = split.LargeIcon ?? split.Icon,
                    Header = split.Header ?? StripShortcutSuffix(split.ScreenTipTitle),
                    ScreenTipTitle = split.ScreenTipTitle ?? split.Header,
                    ScreenTipText = split.ScreenTipText,
                };
                proxySplit.Click += (_, _) => KeyTipService.InvokeControl(split);
                proxySplit.BorrowMenuFrom(split);
                proxy = proxySplit;
                break;
            }

            case RibbonDropDownButton dropDown:
            {
                // A real dropdown proxy with its OWN popup: the flyout opens under the proxy and
                // toggles/dismisses correctly, and it works even when the source's tab isn't
                // realized. It borrows the source's menu items while open (see BorrowMenuFrom).
                var proxyDrop = new RibbonDropDownButton
                {
                    Size = size,
                    Icon = dropDown.Icon ?? dropDown.LargeIcon,
                    LargeIcon = dropDown.LargeIcon ?? dropDown.Icon,
                    Header = dropDown.Header ?? StripShortcutSuffix(dropDown.ScreenTipTitle),
                    ScreenTipTitle = dropDown.ScreenTipTitle ?? dropDown.Header,
                    ScreenTipText = dropDown.ScreenTipText,
                };
                proxyDrop.BorrowMenuFrom(dropDown);
                proxy = proxyDrop;
                break;
            }

            case RibbonButton button:
            {
                var proxyButton = MakeProxyButton(size, button.Icon ?? button.LargeIcon, button.LargeIcon ?? button.Icon, button.Header, button.ScreenTipTitle, button.ScreenTipText);
                proxyButton.Click += (_, _) => KeyTipService.InvokeControl(button);
                proxy = proxyButton;
                break;
            }

            default:
            {
                // Unknown control type: generic proxy that invokes via UIA patterns.
                var proxyButton = MakeProxyButton(size, null, null, null, source.ToString(), null);
                proxyButton.Click += (_, _) => KeyTipService.InvokeControl(source);
                proxy = proxyButton;
                break;
            }
        }

        proxy.SetValue(QuickAccessSourcePropertyKey, source);
        MirrorEnabledState(proxy, source);
        return proxy;
    }

    /// <summary>
    /// Makes a proxy follow its source's enabled state, so disabling a ribbon command in code greys
    /// its quick-access, overflow and custom-group copies too.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Binding to the source's <see cref="UIElement.IsEnabled"/> picks up the COERCED value, which
    /// is what we want: a button inside a group the app disabled reports false even though its own
    /// property was never touched, and its proxies grey with it.
    /// </para>
    /// <para>
    /// ⚠ A <see cref="System.Windows.Data.MultiBinding"/> rather than a plain one because there are TWO independent
    /// reasons a proxy is disabled — the source, and <see cref="IsCommandParkedProperty"/> for a
    /// merged source that has stepped out. They cannot be separate writes to the same property:
    /// assigning a value to a property carrying a ONE-WAY binding clears that binding, so the merge
    /// service's <c>proxy.IsEnabled = false</c> would silently sever the source mirror on the first
    /// park and never restore it. Combining both inputs into one expression removes the ordering
    /// question entirely.
    /// </para>
    /// </remarks>
    private static void MirrorEnabledState(FrameworkElement proxy, FrameworkElement source)
    {
        var enabled = new System.Windows.Data.MultiBinding { Converter = ProxyEnabledConverter.Instance };
        enabled.Bindings.Add(new System.Windows.Data.Binding(nameof(IsEnabled)) { Source = source });
        enabled.Bindings.Add(new System.Windows.Data.Binding
        {
            Source = proxy,
            Path = new PropertyPath("(0)", IsCommandParkedProperty),
        });

        proxy.SetBinding(IsEnabledProperty, enabled);
    }

    /// <summary>Enabled = the source is enabled AND this proxy is not parked.</summary>
    private sealed class ProxyEnabledConverter : System.Windows.Data.IMultiValueConverter
    {
        internal static readonly ProxyEnabledConverter Instance = new();

        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture) =>
            values is { Length: 2 } && values[0] is true && values[1] is false;

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture) =>
            throw new NotSupportedException("A proxy's enabled state is derived, never pushed back.");
    }

    private static RibbonButton MakeProxyButton(
        RibbonControlSize size,
        System.Windows.Media.ImageSource? icon,
        System.Windows.Media.ImageSource? largeIcon,
        string? header,
        string? tipTitle,
        string? tipText) =>
        new()
        {
            Size = size,
            Icon = icon,
            LargeIcon = largeIcon,
            // Small-sized sources often have no Header (icon-only); derive one from the
            // ScreenTip title minus its "(Ctrl+B)"-style shortcut suffix.
            Header = header ?? StripShortcutSuffix(tipTitle),
            ScreenTipTitle = tipTitle ?? header,
            ScreenTipText = tipText,
        };

    /// <summary>"Bold (Ctrl+B)" → "Bold": drops one trailing parenthesized suffix, the common
    /// shortcut convention in ScreenTip titles, when deriving a label from one.</summary>
    private static string? StripShortcutSuffix(string? tipTitle)
    {
        if (string.IsNullOrWhiteSpace(tipTitle))
        {
            return tipTitle;
        }

        int open = tipTitle.LastIndexOf(" (", StringComparison.Ordinal);
        return open > 0 && tipTitle.EndsWith(")", StringComparison.Ordinal)
            ? tipTitle[..open]
            : tipTitle;
    }

    /// <summary>
    /// Right-clicking a command control in a ribbon group offers "Add to Quick Access
    /// Toolbar" (Office-style). Quick-access items are NOT handled here — their hosts carry
    /// the shared placement menu, which opens (and marks the event handled) before it
    /// bubbles this far.
    /// </summary>
    protected override void OnContextMenuOpening(System.Windows.Controls.ContextMenuEventArgs e)
    {
        base.OnContextMenuOpening(e);

        // A quick-access ITEM was right-clicked (the tab-row / below-ribbon hosts live INSIDE
        // the ribbon, so the event bubbles here). Its proxy is a RibbonButton, which
        // ResolveCommandControl would wrongly match and hijack with the "Add to QAT" menu —
        // suppressing the host's placement/Remove menu. Bail so the host menu opens. (The
        // title-bar host is projected into the window, outside this tree, so it never hit this.)
        if (ResolveQuickAccessItem(e.OriginalSource as DependencyObject) is not null)
        {
            return;
        }

        if (e.Handled || ResolveCommandControl(e.OriginalSource as DependencyObject) is not { } target)
        {
            return;
        }

        e.Handled = true;

        var addItem = new System.Windows.Controls.MenuItem
        {
            Header = RibbonLocalization.GetString(RibbonString.AddToQuickAccessToolbar),
            IsEnabled = !IsInQuickAccess(target),
        };
        addItem.Click += (_, _) => AddToQuickAccess(target);

        var customizeItem = new System.Windows.Controls.MenuItem
        {
            Header = RibbonLocalization.GetString(RibbonString.CustomizeQuickAccessToolbar),
        };
        customizeItem.Click += (_, _) => QuickAccessCustomizeRequested?.Invoke(this, EventArgs.Empty);

        var customizeRibbonItem = new System.Windows.Controls.MenuItem
        {
            Header = RibbonLocalization.GetString(RibbonString.CustomizeRibbon),
        };
        customizeRibbonItem.Click += (_, _) => RaiseRibbonCustomizeRequested();

        var collapseItem = new System.Windows.Controls.MenuItem
        {
            Header = RibbonLocalization.GetString(RibbonString.CollapseRibbon),
            IsChecked = IsMinimized,
        };
        collapseItem.Click += (_, _) => SetCurrentValue(IsMinimizedProperty, !IsMinimized);

        var menu = new System.Windows.Controls.ContextMenu
        {
            PlacementTarget = target,
            // ContextMenu is hosted in a separate popup window and therefore cannot inherit this
            // from the command's visual tree. Copy it explicitly so item layout and submenu arrows
            // mirror with the owning ribbon.
            FlowDirection = target.FlowDirection,
        };
        ApplyModernMenuStyle(menu);
        menu.Items.Add(addItem);
        menu.Items.Add(customizeItem);
        menu.Items.Add(customizeRibbonItem);
        menu.Items.Add(new System.Windows.Controls.Separator());
        menu.Items.Add(collapseItem);
        RibbonPopupMotion.SuppressNativeContextMenuAnimationForOpen(menu);
        menu.IsOpen = true;
    }

    // Cached menu-style dictionary (Themes/Menus.xaml). It lives in its OWN dictionary — NOT the
    // theme's Office2024.xaml — because a keyed resource in the assembly theme dictionary isn't
    // reachable by a runtime lookup from a ContextMenu (a PresentationFramework type resolves its
    // theme resources against PresentationFramework's theme, not RibbonKit's Generic.xaml). Loading
    // it explicitly and assigning the Style directly sidesteps that; the style's brushes are
    // DynamicResource, so they still resolve — and re-theme — from the app-merged token set.
    private static System.Windows.ResourceDictionary? _menuResources;

    private static System.Windows.ResourceDictionary MenuResources =>
        _menuResources ??= new System.Windows.ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/RibbonKit;component/Themes/Menus.xaml", UriKind.Absolute),
        };

    // Restyles a context menu to the modern RibbonKit look (rounded flyout + RibbonMenuItem-style
    // rows) instead of the native WPF menu. The flyout chrome comes from the ContextMenu Style; the
    // per-item look is injected as IMPLICIT styles on the menu's own Resources so every MenuItem (incl.
    // submenus) and Separator resolves it. ItemContainerStyle can't be used here — WPF would apply the
    // MenuItem style to Separator items too and throw. If the dictionary can't load, the menu keeps the
    // system default.
    internal static void ApplyModernMenuStyle(System.Windows.Controls.ContextMenu menu)
    {
        System.Windows.ResourceDictionary dictionary = MenuResources;

        if (dictionary["RibbonKit.ContextMenu"] is System.Windows.Style menuStyle)
        {
            menu.Style = menuStyle;
        }

        if (dictionary["RibbonKit.MenuItem"] is System.Windows.Style itemStyle)
        {
            menu.Resources[typeof(System.Windows.Controls.MenuItem)] = itemStyle;
        }

        if (dictionary["RibbonKit.MenuSeparator"] is System.Windows.Style separatorStyle)
        {
            menu.Resources[System.Windows.Controls.MenuItem.SeparatorStyleKey] = separatorStyle;
        }
    }

    // Walks up from the right-clicked element (visual parent first, logical as fallback so
    // popup content and text elements still resolve) to the nearest ribbon command control,
    // stopping at the ribbon itself.
    private FrameworkElement? ResolveCommandControl(DependencyObject? node)
    {
        while (node is not null && !ReferenceEquals(node, this))
        {
            if (node is RibbonButton or RibbonToggleButton or RibbonDropDownButton)
            {
                return (FrameworkElement)node;
            }

            // VisualTreeHelper.GetParent throws for non-visual nodes (a Run in a header,
            // FlowDocument content), so only visuals take the visual-tree step.
            DependencyObject? next = node is System.Windows.Media.Visual or System.Windows.Media.Media3D.Visual3D
                ? System.Windows.Media.VisualTreeHelper.GetParent(node)
                : null;
            node = next ?? LogicalTreeHelper.GetParent(node);
        }

        return null;
    }

    /// <summary>
    /// Whether the application (File) button's surface is open. Which surface that is depends on
    /// what is assigned: the <see cref="ApplicationMenu"/> drop-down when one is set, otherwise the
    /// <see cref="Backstage"/> overlay. It stays named for the backstage because that is the case
    /// it has always driven, and because a single flag is what makes the File button a plain
    /// two-state toggle regardless of which surface is behind it — see
    /// <see cref="IsApplicationMenuOpen"/> for the discriminator a template needs.
    /// </summary>
    public bool IsBackstageOpen
    {
        get => (bool)GetValue(IsBackstageOpenProperty);
        set => SetValue(IsBackstageOpenProperty, value);
    }

    /// <summary>
    /// <see langword="true"/> while <see cref="IsBackstageOpen"/> is showing the
    /// <see cref="ApplicationMenu"/> rather than the <see cref="Backstage"/>. Templates key the
    /// differences off this: the orb stays VISIBLE over an application menu (it is drawn on top of
    /// it, as in Office 2007) but hides under a backstage, and the title-bar quick access strip is
    /// only hidden by the backstage.
    /// </summary>
    public bool IsApplicationMenuOpen
    {
        get => (bool)GetValue(IsApplicationMenuOpenProperty);
        private set => SetValue(IsApplicationMenuOpenPropertyKey, value);
    }

    /// <summary>
    /// The backstage content opened by the application (File) button — typically a
    /// <see cref="Controls.Backstage"/>. When this and <see cref="ApplicationMenu"/> are both
    /// <see langword="null"/>, the File button is hidden.
    /// </summary>
    public object? Backstage
    {
        get => GetValue(BackstageProperty);
        set => SetValue(BackstageProperty, value);
    }

    /// <summary>
    /// The Office 2007 two-pane application menu — typically a
    /// <see cref="Controls.RibbonApplicationMenu"/>. <b>When this is set it wins:</b> the File
    /// button opens the menu and the <see cref="Backstage"/> is left alone, so an app can keep both
    /// assigned and switch generations at runtime by nulling one out.
    /// <para>
    /// Unlike the backstage, the menu is NOT an overlay in the window's adorner layer — it is
    /// rendered inside the ribbon's own tab-strip row, underneath the application button, so the
    /// orb keeps sitting on top of it. Pair it with
    /// <see cref="RibbonApplicationButtonShape.Orb"/> and the Office 2007 theme.
    /// </para>
    /// </summary>
    public object? ApplicationMenu
    {
        get => GetValue(ApplicationMenuProperty);
        set => SetValue(ApplicationMenuProperty, value);
    }

    /// <summary>
    /// Gets or sets the repeatable notification surface connected directly below the ribbon
    /// chrome. Hosting a <see cref="RibbonMessageBar"/> here lets the active theme join the
    /// preceding ribbon/QAT surface to its first open message without a rounded gap or shadow.
    /// </summary>
    public RibbonMessageBar? MessageBar
    {
        get => (RibbonMessageBar?)GetValue(MessageBarProperty);
        set => SetValue(MessageBarProperty, value);
    }

    /// <summary>
    /// Gets whether <see cref="MessageBar"/> currently contains at least one open message.
    /// Shared templates use this direct discriminator to join or release their lower chrome.
    /// </summary>
    public bool HasOpenMessages => (bool)GetValue(HasOpenMessagesProperty);

    /// <summary>
    /// Gets or sets the transient File-surface preview requested by RibbonKit's XAML design tools.
    /// Values are -1 for no override, 0 for closed, 1 for Backstage, and 2 for application menu.
    /// Runtime instances ignore this property.
    /// </summary>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int DesignPreviewFileSurface
    {
        get => (int)GetValue(DesignPreviewFileSurfaceProperty);
        set => SetValue(DesignPreviewFileSurfaceProperty, value);
    }

    /// <summary>
    /// Gets or sets the design-tool-only theme generation. <c>-1</c> inherits the project's
    /// resources; non-negative values map to <see cref="Theming.RibbonTheme"/>. Runtime instances
    /// ignore this property.
    /// </summary>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int DesignPreviewTheme
    {
        get => (int)GetValue(DesignPreviewThemeProperty);
        set => SetValue(DesignPreviewThemeProperty, value);
    }

    /// <summary>
    /// Gets or sets the application button text. When no local value, style or binding supplies
    /// one, the getter returns RibbonKit's live localized <c>File</c> string.
    /// </summary>
    public string ApplicationButtonHeader
    {
        get
        {
            string? configured = (string?)GetValue(ApplicationButtonHeaderProperty);
            return UsesDefaultApplicationButtonHeader() || configured is null
                ? RibbonLocalization.GetString(RibbonString.File)
                : configured;
        }

        set => SetValue(ApplicationButtonHeaderProperty, value);
    }

    /// <summary>
    /// Gets the application button text after applying the localized default. Templates should
    /// bind to this value so provider and UI-culture refreshes do not replace an application-owned
    /// <see cref="ApplicationButtonHeader"/> value or binding.
    /// </summary>
    public string EffectiveApplicationButtonHeader =>
        (string)GetValue(EffectiveApplicationButtonHeaderProperty);

    private static void OnApplicationButtonHeaderChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e) =>
        ((Ribbon)d).UpdateEffectiveApplicationButtonHeader();

    private static void OnApplicationButtonShapeChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e)
    {
        var ribbon = (Ribbon)d;
        ribbon.UpdateApplicationMenuHost();
        ribbon.UpdateApplicationMenuOverlayPlacement();
        ribbon.UpdateRibbonWindowApplicationButtonShape();
    }

    private void OnLocalizationBindingSourceChanged(
        object? sender,
        PropertyChangedEventArgs e)
    {
        if (Dispatcher.CheckAccess())
        {
            UpdateEffectiveApplicationButtonHeader();
        }
        else if (!Dispatcher.HasShutdownStarted && !Dispatcher.HasShutdownFinished)
        {
            Dispatcher.BeginInvoke(
                DispatcherPriority.DataBind,
                new Action(UpdateEffectiveApplicationButtonHeader));
        }
    }

    private void UpdateEffectiveApplicationButtonHeader()
    {
        string? configured = (string?)GetValue(ApplicationButtonHeaderProperty);
        string effective = UsesDefaultApplicationButtonHeader() || configured is null
            ? RibbonLocalization.GetString(RibbonString.File)
            : configured;
        SetValue(EffectiveApplicationButtonHeaderPropertyKey, effective);
    }

    private bool UsesDefaultApplicationButtonHeader() =>
        DependencyPropertyHelper.GetValueSource(this, ApplicationButtonHeaderProperty).BaseValueSource
            == BaseValueSource.Default;

    /// <summary>
    /// Whether the application button renders as a rectangular File tab (default) or as the round
    /// Office 2007 orb. This is an application choice, not a theme one: the theme system colors
    /// controls through tokens and never changes their shape, so an app pairing the Office 2007
    /// theme with the orb sets this explicitly.
    /// </summary>
    public RibbonApplicationButtonShape ApplicationButtonShape
    {
        get => (RibbonApplicationButtonShape)GetValue(ApplicationButtonShapeProperty);
        set => SetValue(ApplicationButtonShapeProperty, value);
    }

    private BackstageAdorner? _backstageAdorner;

    // While a TRANSLUCENT backstage is open, the content behind it (the adorned root) is fully
    // hidden — Opacity 0 + hit-testing off — so a window system backdrop (Mica/Acrylic)
    // composites directly behind the backstage. The DWM draws the material only through pixels
    // the window never painted, so ANY in-app rendering of the content (even blurred, as the
    // earlier frosted-acrylic approach did) is opaque to the compositor and blocks the material
    // entirely. Prior state is saved here and restored when the backstage closes.
    private UIElement? _backstageHiddenRoot;
    private double _backstageHiddenRootOpacity = 1d;
    private bool _backstageHiddenRootHitTestVisible = true;

    // Design-time-only host for the backstage preview (see UpdateDesignTimeBackstage). The
    // runtime adorner path needs a real Window the XAML designer doesn't provide.
    private Border? _designBackstageHost;
    private ResourceDictionary? _designPreviewThemeDictionary;

    // Guards the SelectedTab <-> SelectedIndex mirroring so setting one to reflect the other
    // never bounces back and re-enters.
    private bool _syncingSelection;

    // Owns the Alt/F10 KeyTip experience for this ribbon; wires itself to the host
    // window on Loaded. Held so it lives as long as the ribbon.
    private readonly KeyTipService _keyTipService;

    // Quick-access-toolbar placement plumbing. When QuickAccessPosition is TitleBar the
    // items are projected into the host RibbonWindow's TitleBarContent via this host; the
    // shared context menu lets the user move the QAT between placements (like Office).
    private RibbonQuickAccessToolBar? _titleBarQatHost;
    private object? _savedTitleBarContent;
    private RibbonWindow? _applicationButtonShapeWindow;
    private System.Windows.Controls.ContextMenu? _qatContextMenu;

    /// <summary>
    /// The constrained QAT host currently on screen, if this placement can overflow. The title-bar
    /// host lives outside this ribbon's visual tree, so KeyTips cannot discover it by walking from
    /// the ribbon; this is the deliberate bridge back to the owning service.
    /// </summary>
    internal RibbonQuickAccessToolBar? ActiveQuickAccessToolBar => QuickAccessPosition switch
    {
        RibbonQuickAccessPosition.TitleBar => _titleBarQatHost,
        RibbonQuickAccessPosition.TabRow => _qatTabRowHost as RibbonQuickAccessToolBar
            ?? FindDescendantByType<RibbonQuickAccessToolBar>(this),
        _ => null,
    };

    // Coalesces the deferred selection-visual refresh triggered by the tab-row QAT resizing.
    private bool _selectionVisualsPending;
    private System.Windows.Controls.MenuItem? _qatTitleBarItem;
    private System.Windows.Controls.MenuItem? _qatAboveItem;
    private System.Windows.Controls.MenuItem? _qatBelowItem;
    private System.Windows.Controls.MenuItem? _qatRemoveItem;
    private System.Windows.Controls.MenuItem? _qatCustomizeItem;
    private System.Windows.Controls.Separator? _qatRemoveSeparator;

    // The quick-access item under the cursor when the QAT context menu was opened —
    // captured in the hosts' ContextMenuOpening (the menu itself is shared between hosts,
    // so the Opened event alone cannot tell which item was right-clicked).
    private FrameworkElement? _qatMenuTarget;

    // Cross-fade plumbing: the nested tab control whose selection changes drive a content
    // cross-fade, and the ribbon body host that fades.
    private RibbonTabControl? _ribbonTabControl;
    private FrameworkElement? _ribbonContentHost;

    // Application menus belong in the Ribbon template's outer overlay so the menu alone can paint
    // above the QAT/message rows. While an Office 2007 menu is open, the real application button is
    // temporarily moved into that overlay and an inert same-size placeholder preserves its slot;
    // this keeps one exactly positioned orb without promoting the tab-control/body-shadow branch.
    // The nested presenter is retained only as a custom-template fallback.
    private Canvas? _applicationMenuOverlayLayer;
    private ContentPresenter? _applicationMenuOverlayPresenter;
    private Border? _applicationButtonOverlay;
    private ContentPresenter? _nestedApplicationMenuPresenter;
    private FrameworkElement? _applicationButton;
    private Panel? _applicationButtonOriginalParent;
    private Border? _applicationButtonPlaceholder;
    private int _applicationButtonOriginalIndex = -1;

    // Below-ribbon quick-access bar and the last measured body height, so the bar can glide
    // by that height (staying visible) as the body collapses/expands on minimize/restore.
    private FrameworkElement? _qatBelowHost;
    private double _lastRibbonBodyHeight;

    // Tab-row quick-access host. Cached because UpdateQatButtonContext has to reach it on every
    // theme/accent change and it lives in the NESTED RibbonTabControl's template — GetTemplateChild
    // on this control cannot see it, so the only alternative is a full visual-tree walk each time.
    private FrameworkElement? _qatTabRowHost;

    // Backstage close is animated (slide out), so the adorner is removed only after the
    // exit animation; this guards against a re-open racing the pending removal.
    private bool _backstageClosing;

    private static void OnIsBackstageOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ribbon = (Ribbon)d;

        // A modal tab blocks the backstage (Word's Print Preview hides File entirely). The
        // application button is hidden by a template trigger, so this only catches programmatic
        // attempts — revert rather than coerce, so no stale "true" springs back on exit.
        if (ribbon._modalScope.IsActive && (bool)e.NewValue && !ribbon._suppressBackstageChange)
        {
            ribbon._suppressBackstageChange = true;
            try
            {
                ribbon.SetCurrentValue(IsBackstageOpenProperty, false);
            }
            finally
            {
                ribbon._suppressBackstageChange = false;
            }

            return;
        }

        if (ribbon._suppressBackstageChange)
        {
            return;
        }

        ribbon.UpdateApplicationMenuState();
        ribbon.UpdateBackstageOverlay((bool)e.NewValue);

        // CLOSING: re-place the selection marker and the body-border notch.
        //
        // Both are positioned from the SELECTED TAB'S TRANSFORM, and while the backstage overlay is
        // up the ribbon underneath is hidden — so anything that re-lays-out the tab strip during
        // that time never reaches them. Maximizing the window with the backstage open is the easy
        // repro: the strip gets wider, but the tab control raises no size or selection change that
        // the notch listens to, so on close the notch stays parked under wherever the old selected
        // tab used to be until the selection changes. Same class of bug as §3.29 and the
        // theme-switch case in §3.27's fourth pass.
        //
        // Dispatched at Loaded priority so the overlay is actually gone and layout has settled
        // before the transform is measured.
        if (!(bool)e.NewValue)
        {
            ribbon.Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.Loaded,
                new Action(ribbon.RefreshSelectionVisuals));
        }
    }

    private static void OnIsMinimizedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        // The ribbon body's visibility is managed here (not by a template trigger) so the
        // body can slide UP + fade OUT before it collapses, and slide DOWN + fade IN when it
        // reappears. Only transform/opacity animate — the row height is never animated, so
        // the window's layout still snaps cleanly once the body is hidden/shown.
        var ribbon = (Ribbon)d;

        // Minimizing is blocked while a modal tab is active: the mode owns the whole ribbon.
        // Same revert-not-coerce reasoning as the backstage guard above.
        if (ribbon._modalScope.IsActive && (bool)e.NewValue && !ribbon._suppressMinimizeChange)
        {
            ribbon._suppressMinimizeChange = true;
            try
            {
                ribbon.SetCurrentValue(IsMinimizedProperty, false);
            }
            finally
            {
                ribbon._suppressMinimizeChange = false;
            }

            return;
        }

        if (ribbon._suppressMinimizeChange)
        {
            return;
        }

        ribbon._ribbonContentHost ??= FindDescendantByName(ribbon, "ContentHost");
        if (ribbon._ribbonContentHost is not { } host)
        {
            return;
        }

        // The below-ribbon quick-access bar sits under the body; when the body collapses it
        // would jump up by the body's height. Glide it by that height so it follows the body
        // (staying visible) instead of snapping. Only relevant when the QAT is below.
        FrameworkElement? qat = ribbon.QuickAccessPosition == RibbonQuickAccessPosition.BelowRibbon
            ? ribbon._qatBelowHost
            : null;

        if ((bool)e.NewValue)
        {
            // Remember the body height (still visible now) so restore can reuse it.
            if (host.ActualHeight > 0d)
            {
                ribbon._lastRibbonBodyHeight = host.ActualHeight;
            }

            // The bar glides UP by the body height in step with the body's fade-out...
            RibbonMotion.AnimateTranslateY(qat, RibbonAnimationAction.RibbonMinimize, 0d, -ribbon._lastRibbonBodyHeight);

            // Minimize: lift the body away, then collapse the row. Resetting the bar's
            // transform in the same step as the collapse keeps it visually stationary.
            RibbonMotion.PlayClose(
                host,
                RibbonAnimationAction.RibbonMinimize,
                RibbonSlideFrom.Top,
                () =>
                {
                    if (ribbon.IsMinimized)
                    {
                        host.Visibility = Visibility.Collapsed;
                        RibbonMotion.Rest(qat);
                    }
                });
        }
        else
        {
            // Restore: show the row and slide + fade the body back in. The bar starts at the
            // minimized (raised) offset and glides DOWN to rest in step with the body — the
            // From value is applied on the same frame as the row appears, so it stays put.
            host.Visibility = Visibility.Visible;
            RibbonMotion.PlayOpen(host, RibbonAnimationAction.RibbonMinimize, RibbonSlideFrom.Top);
            RibbonMotion.AnimateTranslateY(qat, RibbonAnimationAction.RibbonMinimize, -ribbon._lastRibbonBodyHeight, 0d);
        }
    }

    private void UpdateBackstageOverlay(bool open)
    {
        // The designer translates one primitive preview property so it can switch between two
        // authored File surfaces without clearing either object-valued model property. The VS 2022
        // isolated designer can corrupt later ModelProperty.Value reads after such a clear.
        if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
        {
            UpdateDesignTimeBackstage(open && !IsApplicationMenuOpen);
            return;
        }

        // AN APPLICATION MENU TAKES THE WHOLE PATH OVER. It is not an overlay at all: the template
        // renders it inside the tab-strip row, gated purely on IsApplicationMenuOpen, so there is no
        // adorner to add, nothing to hide behind, and — crucially — no title-bar QAT to hide either
        // (Office 2007 keeps the quick access strip visible under the menu). Everything below this
        // point is backstage-only.
        if (ApplicationMenu is not null)
        {
            return;
        }

        // Office hides title-bar quick access content while the backstage is open;
        // the overlay only covers the window CONTENT, so the title bar must opt in.
        if (Window.GetWindow(this) is RibbonWindow ribbonWindow)
        {
            ribbonWindow.SetCurrentValue(RibbonWindow.IsTitleBarContentVisibleProperty, !open);
        }

        RibbonSlideFrom slideEdge = BackstageSlideEdge(FlowDirection);

        if (open)
        {
            _backstageClosing = false;

            // Reopening while a close animation is still running: reuse the existing overlay
            // and just replay the entrance (never create a second adorner for the same
            // content, which a single UIElement can't have two of).
            if (_backstageAdorner is not null)
            {
                if (Backstage is FrameworkElement reopening)
                {
                    reopening.Focus();
                    RibbonMotion.PlayOpen(reopening, RibbonAnimationAction.Backstage, slideEdge);
                }

                HideContentBehindBackstage(_backstageAdorner.AdornedElement);
                return;
            }

            if (Backstage is not UIElement content)
            {
                return;
            }

            // Host the backstage in the window's adorner layer so the overlay lives
            // INSIDE the window (follows moves, minimize, resize) — a Popup would be
            // its own top-level window and do none of those things.
            if (Window.GetWindow(this)?.Content is not UIElement root
                || AdornerLayer.GetAdornerLayer(root) is not { } layer)
            {
                return;
            }

            _backstageAdorner = new BackstageAdorner(root, content, this);
            layer.Add(_backstageAdorner);

            // Hide the content behind a translucent backstage entirely so a window system
            // backdrop (Mica) composites raw behind it. The hide targets the adorned root; the
            // backstage lives in the adorner layer (a SIBLING visual branch inside the
            // AdornerDecorator, not a child of the root), so it stays fully visible on top.
            HideContentBehindBackstage(root);

            if (content is FrameworkElement element)
            {
                element.Focusable = true;
                element.Focus(); // So Esc works immediately.

                // Slide from the logical leading edge (honors RTL and the global animation level).
                RibbonMotion.PlayOpen(element, RibbonAnimationAction.Backstage, slideEdge);
            }
        }
        else if (_backstageAdorner is not null && !_backstageClosing)
        {
            // Slide the backstage back out through the logical leading edge, then remove
            // the adorner once the exit animation finishes.
            _backstageClosing = true;
            BackstageAdorner adorner = _backstageAdorner;
            FrameworkElement? content = Backstage as FrameworkElement;

            // Restore the hidden content at the START of the exit (not on completion): the
            // backstage slides out through its logical leading edge, and the ribbon/document must
            // already be there for the slide to reveal — restoring at the end would leave the bare
            // backdrop
            // showing during the whole exit animation. If a re-open cancels this close
            // mid-flight, the open path simply hides the content again.
            RestoreContentBehindBackstage();

            RibbonMotion.PlayClose(
                content,
                RibbonAnimationAction.Backstage,
                slideEdge,
                () =>
                {
                    // A re-open may have cancelled the close mid-flight; only tear down if
                    // we're still closing.
                    if (!_backstageClosing)
                    {
                        return;
                    }

                    AdornerLayer.GetAdornerLayer(adorner.AdornedElement)?.Remove(adorner);
                    adorner.Detach();
                    _backstageAdorner = null;
                    _backstageClosing = false;
                    RibbonMotion.Rest(content);
                });
        }
    }

    /// <summary>
    /// Hides <paramref name="root"/> (the window content behind the backstage) when the backstage
    /// is <see cref="Controls.Backstage.Translucent"/>: a fast fade to Opacity 0 (synced to the
    /// backstage slide-in) plus hit-testing off. No-op for an opaque backstage.
    /// </summary>
    /// <remarks>
    /// Hiding — not blurring — is what lets a window system backdrop (Mica/Acrylic) reach the
    /// backstage. The DWM composites the material BENEATH the window and only through pixels the
    /// window never painted; a blurred pixel is still a painted pixel, so the previous in-app
    /// BlurEffect approach could never reveal it. With the content not rendering at all, the only
    /// layers behind the backstage overlay are the window root and title-bar band — both already
    /// transparent in backdrop mode — so the translucent nav rail composites over the raw
    /// material, exactly like Office on Windows 11. Without a backdrop (Windows 10, or Mica off)
    /// the rail sits on the plain window background instead. Opacity-zero is used rather than
    /// Visibility so the hide is animatable and cannot disturb the adorner layer hosting the
    /// backstage (a sibling branch, unaffected by the root's opacity). Hit-testing is disabled
    /// because a zero-opacity element still receives input (WPF hit-testing ignores opacity).
    /// Prior values are saved so <see cref="RestoreContentBehindBackstage"/> restores exactly.
    /// </remarks>
    private void HideContentBehindBackstage(UIElement root)
    {
        if (Backstage is not Backstage { Translucent: true })
        {
            return;
        }

        // Save state only on the first hide of this open (a reopen-while-closing re-hides the
        // same root; overwriting the saved state then would capture the mid-fade values).
        if (_backstageHiddenRoot is null)
        {
            _backstageHiddenRoot = root;
            _backstageHiddenRootOpacity = root.Opacity;
            _backstageHiddenRootHitTestVisible = root.IsHitTestVisible;
        }

        root.IsHitTestVisible = false;

        if (RibbonAnimation.IsEnabled(RibbonAnimationAction.Backstage))
        {
            var fade = new System.Windows.Media.Animation.DoubleAnimation(
                0d, RibbonAnimation.GetDuration(RibbonAnimationAction.Backstage))
            {
                EasingFunction = RibbonAnimation.GetEase(RibbonAnimationAction.Backstage),
            };
            root.BeginAnimation(UIElement.OpacityProperty, fade);
        }
        else
        {
            root.BeginAnimation(UIElement.OpacityProperty, null);
            root.Opacity = 0d;
        }
    }

    /// <summary>
    /// Restores the content hidden by <see cref="HideContentBehindBackstage"/> — instantly, no
    /// fade: it runs at the START of the backstage's exit slide, so the ribbon/document must
    /// already be fully present underneath for the slide-out to reveal. Idempotent.
    /// </summary>
    private void RestoreContentBehindBackstage()
    {
        if (_backstageHiddenRoot is { } root)
        {
            root.BeginAnimation(UIElement.OpacityProperty, null);
            root.Opacity = _backstageHiddenRootOpacity;
            root.IsHitTestVisible = _backstageHiddenRootHitTestVisible;
            _backstageHiddenRoot = null;
        }
    }

    /// <summary>
    /// Design-time-only backstage rendering. Hosts the <see cref="Backstage"/> content directly
    /// in the ribbon template's <c>PART_DesignBackstageHost</c> (no window, no adorner layer, no
    /// animation), so it shows and can be edited on the XAML designer surface. Only ever called
    /// under <see cref="System.ComponentModel.DesignerProperties.GetIsInDesignMode"/>; the runtime
    /// path is untouched. Safe to parent the element here because the runtime adorner path is
    /// skipped in design mode, so the backstage is not hosted anywhere else.
    /// </summary>
    private void UpdateDesignTimeBackstage(bool open)
    {
        if (_designBackstageHost is null)
        {
            return;
        }

        if (open && Backstage is UIElement content)
        {
            _designBackstageHost.Child = content;
            _designBackstageHost.Visibility = Visibility.Visible;
        }
        else
        {
            _designBackstageHost.Child = null;
            _designBackstageHost.Visibility = Visibility.Collapsed;
        }
    }

    private static void OnBackstageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ribbon = (Ribbon)d;
        if (e.OldValue is Backstage oldBackstage)
        {
            oldBackstage.BackRequested -= ribbon.OnBackstageBackRequested;
        }

        if (e.NewValue is Backstage newBackstage)
        {
            newBackstage.BackRequested += ribbon.OnBackstageBackRequested;
        }
    }

    private void OnBackstageBackRequested(object? sender, EventArgs e) =>
        SetCurrentValue(IsBackstageOpenProperty, false);

    private static void OnApplicationMenuChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ribbon = (Ribbon)d;
        if (e.OldValue is RibbonApplicationMenu oldMenu)
        {
            oldMenu.CloseRequested -= ribbon.OnApplicationMenuCloseRequested;
        }

        if (e.NewValue is RibbonApplicationMenu newMenu)
        {
            newMenu.CloseRequested += ribbon.OnApplicationMenuCloseRequested;
        }

        // Assigning or clearing a menu changes WHICH surface IsBackstageOpen means, so the
        // discriminator has to be recomputed even though the open flag itself did not move.
        ribbon.UpdateApplicationMenuState();
        ribbon.UpdateApplicationMenuHost();

        if (DesignerProperties.GetIsInDesignMode(ribbon) && ribbon.DesignPreviewFileSurface >= 0)
        {
            ribbon.ApplyDesignPreviewFileSurface(ribbon.DesignPreviewFileSurface);
        }

    }

    private static void OnMessageBarChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ribbon = (Ribbon)d;
        if (e.OldValue is RibbonMessageBar oldMessageBar)
        {
            oldMessageBar.OpenMessagesChanged -= ribbon.OnOpenMessagesChanged;
        }

        if (e.NewValue is RibbonMessageBar newMessageBar)
        {
            newMessageBar.OpenMessagesChanged += ribbon.OnOpenMessagesChanged;
        }

        ribbon.UpdateHasOpenMessages();
    }

    private void OnOpenMessagesChanged(object? sender, EventArgs e) => UpdateHasOpenMessages();

    private void UpdateHasOpenMessages() =>
        SetValue(HasOpenMessagesPropertyKey, MessageBar?.HasOpenMessages == true);

    private static void OnIsApplicationMenuOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ribbon = (Ribbon)d;
        ribbon.UpdateApplicationMenuHost();
        if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(ribbon))
        {
            ribbon.UpdateDesignTimeBackstage(ribbon.IsBackstageOpen && !(bool)e.NewValue);
        }
    }

    private static void OnDesignPreviewFileSurfaceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ribbon = (Ribbon)d;
        if (DesignerProperties.GetIsInDesignMode(ribbon))
        {
            ribbon.ApplyDesignPreviewFileSurface((int)e.NewValue);
        }
    }

    private static void OnDesignPreviewThemeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ribbon = (Ribbon)d;
        if (DesignerProperties.GetIsInDesignMode(ribbon))
        {
            ribbon.ApplyDesignPreviewTheme((int)e.NewValue);
        }
    }

    private void ApplyDesignPreviewTheme(int preview)
    {
        if (_designPreviewThemeDictionary is not null)
        {
            Resources.MergedDictionaries.Remove(_designPreviewThemeDictionary);
            _designPreviewThemeDictionary = null;
        }

        if (preview >= 0 && Enum.IsDefined(typeof(Theming.RibbonTheme), preview))
        {
            var theme = (Theming.RibbonTheme)preview;
            var dictionary = new ResourceDictionary
            {
                Source = new Uri(
                    $"pack://application:,,,/RibbonKit;component/Themes/Tokens.{theme}.xaml",
                    UriKind.Absolute),
            };
            Resources.MergedDictionaries.Add(dictionary);
            _designPreviewThemeDictionary = dictionary;
        }

        // Local DynamicResources re-resolve automatically, but the selection underline and the
        // classic connected-tab notch are geometry-driven and need the same post-layout refresh as
        // a runtime ThemeManager switch. The scoped dictionary never touches Application.Resources.
        InvalidateMeasure();
        UpdateQatButtonContext();
        RequestSelectionVisualsRefresh();
    }

    private void ApplyDesignPreviewFileSurface(int surface)
    {
        if (surface < 0)
        {
            return;
        }

        bool open = surface is 1 or 2;
        bool applicationMenuOpen = surface == 2;

        // One primitive design value drives the complete transition synchronously. Rendering cannot
        // observe the runtime-precedence intermediate state produced by IsBackstageOpen's callback.
        SetCurrentValue(IsBackstageOpenProperty, open);
        IsApplicationMenuOpen = applicationMenuOpen;
        UpdateDesignTimeBackstage(open && !applicationMenuOpen);
    }

    private void OnApplicationMenuCloseRequested(object? sender, EventArgs e) =>
        SetCurrentValue(IsBackstageOpenProperty, false);

    private void UpdateApplicationMenuState() =>
        IsApplicationMenuOpen = IsBackstageOpen && ApplicationMenu is not null;

    private void UpdateApplicationMenuHost()
    {
        ResolveApplicationMenuParts();

        // The shipping template always uses the outer host. Fall back to the historical nested
        // presenter only for a custom template that has not adopted the new overlay part.
        bool useNestedHost = _applicationMenuOverlayPresenter is null;
        ContentPresenter? active = useNestedHost
            ? _nestedApplicationMenuPresenter
            : _applicationMenuOverlayPresenter;
        ContentPresenter? inactive = useNestedHost
            ? _applicationMenuOverlayPresenter
            : _nestedApplicationMenuPresenter;

        ClearApplicationMenuPresenter(inactive);
        if (active is null)
        {
            RestoreApplicationButtonFromOverlay();
            return;
        }

        if (!ReferenceEquals(active.Content, ApplicationMenu))
        {
            // Collapse before replacing the content so RibbonApplicationMenu receives its normal
            // IsVisibleChanged(false) cleanup when a host or menu object changes while open.
            active.Visibility = Visibility.Collapsed;
            active.Content = null;
            active.Content = ApplicationMenu;
        }

        active.Visibility = IsApplicationMenuOpen && ApplicationMenu is not null
            ? Visibility.Visible
            : Visibility.Collapsed;

        if (!useNestedHost && active.Visibility == Visibility.Visible)
        {
            UpdateApplicationMenuOverlayPlacement();
        }

        UpdateApplicationButtonOverlay();

        if (!useNestedHost && active.Visibility == Visibility.Visible)
        {
            // The first call anchors from the real button before an orb is reparented. This second
            // call follows the placeholder after it has been realized, and is a no-op until then.
            UpdateApplicationMenuOverlayPlacement();
        }
    }

    private void ResolveApplicationMenuParts()
    {
        if (_nestedApplicationMenuPresenter is not null && _applicationButton is not null)
        {
            return;
        }

        RibbonTabControl? tabControl = GetTemplateChild("TabControlHost") as RibbonTabControl
            ?? FindDescendantByType<RibbonTabControl>(this);
        if (tabControl is null)
        {
            return;
        }

        tabControl.ApplyTemplate();
        _nestedApplicationMenuPresenter ??=
            FindDescendantByName(tabControl, RibbonTabControl.ApplicationMenuPresenterPartName)
                as ContentPresenter;
        _applicationButton ??=
            FindDescendantByName(tabControl, ApplicationButtonPartName);
    }

    private void UpdateApplicationMenuOverlayPlacement()
    {
        ResolveApplicationMenuParts();
        FrameworkElement? anchor = _applicationButtonPlaceholder ?? _applicationButton;
        if (_applicationMenuOverlayLayer is null
            || _applicationMenuOverlayPresenter is null
            || anchor is null
            || _applicationButton is null
            || _applicationButton.ActualHeight <= 0d
            || (_applicationButtonPlaceholder is not null && !anchor.IsArrangeValid))
        {
            return;
        }

        try
        {
            Point origin = anchor
                .TransformToVisual(_applicationMenuOverlayLayer)
                .Transform(new Point(0d, 0d));
            if (_applicationButtonPlaceholder is not null)
            {
                origin.Offset(_applicationButton.Margin.Left, _applicationButton.Margin.Top);
            }

            bool anchorBelow = TryFindResource(ApplicationMenuAnchorBelowButtonResourceKey) is true;
            Canvas.SetLeft(_applicationMenuOverlayPresenter, origin.X);
            Canvas.SetTop(
                _applicationMenuOverlayPresenter,
                origin.Y + (anchorBelow ? _applicationButton.ActualHeight : 0d));
        }
        catch (InvalidOperationException)
        {
            // The nested button and outer layer are not connected under one visual root yet.
            // LayoutUpdated retries after template realization/reflow completes.
        }
    }

    private void UpdateApplicationButtonOverlay()
    {
        ResolveApplicationMenuParts();
        if (_applicationButtonOverlay is null
            || _applicationMenuOverlayLayer is null
            || _applicationMenuOverlayPresenter?.Visibility != Visibility.Visible
            || _applicationButton is null
            || ApplicationButtonShape != RibbonApplicationButtonShape.Orb
            || !IsApplicationMenuOpen
            || ApplicationMenu is null)
        {
            RestoreApplicationButtonFromOverlay();
            return;
        }

        if (_applicationButtonPlaceholder is null)
        {
            MoveApplicationButtonToOverlay();
        }

        UpdateApplicationButtonOverlayPlacement();
    }

    private void MoveApplicationButtonToOverlay()
    {
        if (_applicationButton is null
            || _applicationMenuOverlayLayer is null
            || _applicationButtonOverlay is null
            || _applicationButton.Parent is not Panel originalParent
            || _applicationButton.ActualWidth <= 0d
            || _applicationButton.ActualHeight <= 0d)
        {
            return;
        }

        int originalIndex = originalParent.Children.IndexOf(_applicationButton);
        if (originalIndex < 0)
        {
            return;
        }

        try
        {
            Point origin = _applicationButton
                .TransformToVisual(_applicationMenuOverlayLayer)
                .Transform(new Point(0d, 0d));
            Thickness margin = _applicationButton.Margin;
            double slotWidth = Math.Max(
                0d,
                _applicationButton.ActualWidth + margin.Left + margin.Right);
            double slotHeight = Math.Max(
                0d,
                _applicationButton.ActualHeight + margin.Top + margin.Bottom);

            var placeholder = new Border
            {
                Width = slotWidth,
                Height = slotHeight,
                HorizontalAlignment = _applicationButton.HorizontalAlignment,
                VerticalAlignment = _applicationButton.VerticalAlignment,
                IsHitTestVisible = false,
            };

            _applicationButtonOriginalParent = originalParent;
            _applicationButtonOriginalIndex = originalIndex;
            _applicationButtonPlaceholder = placeholder;

            originalParent.Children.RemoveAt(originalIndex);
            originalParent.Children.Insert(originalIndex, placeholder);

            _applicationButtonOverlay.Width = slotWidth;
            _applicationButtonOverlay.Height = slotHeight;
            Canvas.SetLeft(_applicationButtonOverlay, origin.X - margin.Left);
            Canvas.SetTop(_applicationButtonOverlay, origin.Y - margin.Top);
            _applicationButtonOverlay.Child = _applicationButton;
            _applicationButtonOverlay.Visibility = Visibility.Visible;
        }
        catch (InvalidOperationException)
        {
            // The button and outer layer have not joined the same visual root yet. Leave the real
            // button in its ordinary slot; LayoutUpdated retries after realization/reflow.
            RestoreApplicationButtonFromOverlay();
        }
    }

    private void UpdateApplicationButtonOverlayPlacement()
    {
        if (_applicationButtonOverlay is null
            || _applicationMenuOverlayLayer is null
            || _applicationButtonPlaceholder is null)
        {
            return;
        }

        try
        {
            Thickness margin = _applicationButton?.Margin ?? default;
            if (_applicationButton is not null)
            {
                double slotWidth = Math.Max(
                    0d,
                    _applicationButton.ActualWidth + margin.Left + margin.Right);
                double slotHeight = Math.Max(
                    0d,
                    _applicationButton.ActualHeight + margin.Top + margin.Bottom);
                _applicationButtonPlaceholder.Width = slotWidth;
                _applicationButtonPlaceholder.Height = slotHeight;
                _applicationButtonOverlay.Width = slotWidth;
                _applicationButtonOverlay.Height = slotHeight;
            }

            if (!_applicationButtonPlaceholder.IsArrangeValid)
            {
                return;
            }

            Point origin = _applicationButtonPlaceholder
                .TransformToVisual(_applicationMenuOverlayLayer)
                .Transform(new Point(0d, 0d));
            Canvas.SetLeft(_applicationButtonOverlay, origin.X);
            Canvas.SetTop(_applicationButtonOverlay, origin.Y);
        }
        catch (InvalidOperationException)
        {
            // A layout/template transition can briefly disconnect the placeholder. The next
            // LayoutUpdated pass will position the host once both elements share a visual root.
        }
    }

    private void RestoreApplicationButtonFromOverlay()
    {
        if (_applicationButtonOverlay is not null)
        {
            if (ReferenceEquals(_applicationButtonOverlay.Child, _applicationButton))
            {
                _applicationButtonOverlay.Child = null;
            }

            _applicationButtonOverlay.Visibility = Visibility.Collapsed;
            _applicationButtonOverlay.ClearValue(FrameworkElement.WidthProperty);
            _applicationButtonOverlay.ClearValue(FrameworkElement.HeightProperty);
            _applicationButtonOverlay.ClearValue(Canvas.LeftProperty);
            _applicationButtonOverlay.ClearValue(Canvas.TopProperty);
        }

        if (_applicationButton is not null
            && _applicationButtonOriginalParent is not null
            && _applicationButtonPlaceholder is not null)
        {
            _applicationButtonOriginalParent.Children.Remove(_applicationButtonPlaceholder);
            int index = Math.Clamp(
                _applicationButtonOriginalIndex,
                0,
                _applicationButtonOriginalParent.Children.Count);
            _applicationButtonOriginalParent.Children.Insert(index, _applicationButton);
        }

        _applicationButtonOriginalParent = null;
        _applicationButtonPlaceholder = null;
        _applicationButtonOriginalIndex = -1;
    }

    private static void ClearApplicationMenuPresenter(ContentPresenter? presenter)
    {
        if (presenter is null)
        {
            return;
        }

        presenter.Visibility = Visibility.Collapsed;
        presenter.Content = null;
    }

    /// <inheritdoc />
    protected override AutomationPeer OnCreateAutomationPeer() => new RibbonAutomationPeer(this);

    /// <inheritdoc />
    public override void OnApplyTemplate()
    {
        ClearApplicationMenuPresenter(_applicationMenuOverlayPresenter);
        ClearApplicationMenuPresenter(_nestedApplicationMenuPresenter);
        RestoreApplicationButtonFromOverlay();

        base.OnApplyTemplate();

        _applicationMenuOverlayLayer =
            GetTemplateChild(ApplicationMenuOverlayLayerPartName) as Canvas;
        _applicationMenuOverlayPresenter =
            GetTemplateChild(ApplicationMenuOverlayPresenterPartName) as ContentPresenter;
        _applicationButtonOverlay =
            GetTemplateChild(ApplicationButtonOverlayPartName) as Border;
        _nestedApplicationMenuPresenter = null;
        _applicationButton = null;

        // Right-clicking either in-ribbon QAT host opens the placement menu (which also
        // offers Remove-from-QAT when the click lands on an item).
        if (GetTemplateChild("QatTabRowHost") is FrameworkElement tabRowHost)
        {
            _qatTabRowHost = tabRowHost;
            AttachQatContextMenu(tabRowHost);
            TrackTabRowQatSize(tabRowHost);
        }

        if (GetTemplateChild("QatBelowHost") is FrameworkElement belowHost)
        {
            _qatBelowHost = belowHost;
            AttachQatContextMenu(belowHost);
        }

        _designBackstageHost = GetTemplateChild("PART_DesignBackstageHost") as Border;

        UpdateApplicationMenuHost();

        // In the designer the runtime adorner path can't run (no host Window). If the backstage
        // was already flagged open before the template was applied, reflect it into the
        // design-time host now that the host exists.
        if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
        {
            if (DesignPreviewFileSurface >= 0)
            {
                ApplyDesignPreviewFileSurface(DesignPreviewFileSurface);
            }
            else
            {
                UpdateDesignTimeBackstage(IsBackstageOpen && !IsApplicationMenuOpen);
            }
        }

        UpdateQuickAccessPlacement();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        EnsureSelection();

        // The tab-row QAT host lives in the nested RibbonTabControl's template, so it isn't
        // reachable via this control's GetTemplateChild — find it in the realized visual
        // tree (available by Loaded) and give it the same placement menu.
        if (FindDescendantByName(this, "QatTabRowHost") is { ContextMenu: null } tabRowHost)
        {
            _qatTabRowHost = tabRowHost;
            AttachQatContextMenu(tabRowHost);
            TrackTabRowQatSize(tabRowHost);
        }

        // React to accent / colored-title-bar / theme changes so the QAT icons + hover keep
        // matching their surface. Re-hook defensively (Loaded can fire more than once).
        Theming.ThemeManager.Changed -= OnThemeConfigurationChanged;
        Theming.ThemeManager.Changed += OnThemeConfigurationChanged;

        // Subscribe to the nested tab control's selection so switching tabs can cross-fade
        // the ribbon body (the control lives in the RibbonTabControl template, not ours).
        if (_ribbonTabControl is null && FindDescendantByType<RibbonTabControl>(this) is { } tabControl)
        {
            _ribbonTabControl = tabControl;
            tabControl.SelectionChanged += OnRibbonTabSelectionChanged;
        }

        // Visibility of the ribbon body is code-managed (see OnIsMinimizedChanged); sync it
        // to the current state in case the ribbon loaded already minimized.
        _ribbonContentHost ??= FindDescendantByName(this, "ContentHost");
        if (_ribbonContentHost is not null)
        {
            _ribbonContentHost.Visibility = IsMinimized ? Visibility.Collapsed : Visibility.Visible;
        }

        UpdateQuickAccessPlacement();
        UpdateQatButtonContext();
        UpdateApplicationMenuHost();
        UpdateApplicationMenuOverlayPlacement();
        UpdateRibbonWindowApplicationButtonShape();
    }

    private void OnLayoutUpdated(object? sender, EventArgs e)
    {
        if (IsApplicationMenuOpen)
        {
            UpdateApplicationButtonOverlay();
            UpdateApplicationMenuOverlayPlacement();
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        Theming.ThemeManager.Changed -= OnThemeConfigurationChanged;
        UnregisterRibbonWindowApplicationButtonShape();
        if (_ribbonTabControl is not null)
        {
            _ribbonTabControl.SelectionChanged -= OnRibbonTabSelectionChanged;
            _ribbonTabControl = null;
        }
    }

    private void UpdateRibbonWindowApplicationButtonShape()
    {
        var window = Window.GetWindow(this) as RibbonWindow;
        if (!ReferenceEquals(window, _applicationButtonShapeWindow))
        {
            _applicationButtonShapeWindow?.UnregisterApplicationButton(this);
            _applicationButtonShapeWindow = window;
        }

        window?.UpdateApplicationButtonShape(this, ApplicationButtonShape);
    }

    private void UnregisterRibbonWindowApplicationButtonShape()
    {
        _applicationButtonShapeWindow?.UnregisterApplicationButton(this);
        _applicationButtonShapeWindow = null;
    }

    private void OnRibbonTabSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        // Only the tab strip's own selection counts — ignore selection bubbling up from
        // galleries, combo boxes, or the backstage nested inside a tab's content.
        if (!ReferenceEquals(e.OriginalSource, _ribbonTabControl) || IsMinimized)
        {
            return;
        }

        _ribbonContentHost ??= FindDescendantByName(this, "ContentHost");
        FrameworkElement? target = (_ribbonContentHost as System.Windows.Controls.Border)?.Child as FrameworkElement
            ?? _ribbonContentHost;
        // Slide (no fade): the new content is already realized at full opacity, so a fade
        // would flash it transparent for a frame — a subtle rise reads cleanly instead.
        RibbonMotion.PlaySlideIn(target, RibbonAnimationAction.TabSwitch, RibbonSlideFrom.Top);
    }

    private void OnThemeConfigurationChanged(object? sender, EventArgs e)
    {
        UpdateQatButtonContext();
        // Soften a theme/accent swap with a quick opacity settle on the ribbon strip.
        RibbonMotion.PlayThemeCrossfade(_ribbonTabControl, RibbonAnimationAction.ThemeSwitch);

        // A theme whose selected tab connects into the body does so with a themed negative body
        // margin (+ the tab strip's ZIndex). The margin token updates on the swap, but the overlap
        // only re-PAINTS after a layout pass — otherwise the active tab stays unmerged until the
        // next hover happens to re-arrange the strip. Force that pass now so it connects immediately.
        if (_ribbonTabControl is { } tabControl)
        {
            tabControl.InvalidateArrange();
            tabControl.UpdateLayout();

            // The selection marker and the body-border notch are gated on theme tokens
            // (Tab.SelectedUnderline / Tab.ConnectNotch) that just changed, but a theme swap
            // fires no selection or size event — re-place them explicitly so e.g. switching
            // 2024 → 2010 cuts the border under the active tab immediately.
            tabControl.RefreshSelectionVisuals();
        }
    }

    /// <summary>
    /// Sets, on each QAT button, whether it currently sits on a colored surface and the
    /// hover brush to use there — so the button template can white-out its icon and match
    /// the surrounding band's hover. Applied directly (not via inheritance) so it is robust
    /// regardless of how the items are hosted.
    /// </summary>
    private void UpdateQatButtonContext()
    {
        bool accentTitleBar = Theming.ThemeManager.IsAccentedTitleBar;
        bool titleBarColored = QuickAccessPosition == RibbonQuickAccessPosition.TitleBar && accentTitleBar;
        bool tabRowColored = QuickAccessPosition == RibbonQuickAccessPosition.TabRow
            && accentTitleBar
            && Theming.ThemeManager.CurrentTheme == Theming.RibbonTheme.Office2019;
        bool colored = titleBarColored || tabRowColored;

        // Match the hover of the neighbouring chrome: the caption buttons in the title bar,
        // the tabs on the strip.
        string? hoverKey = titleBarColored ? "RibbonKit.Brushes.CaptionButton.HoverBackground"
            : tabRowColored ? "RibbonKit.Brushes.Tab.HoverBackground"
            : null;

        // Pressed/checked-state brush for the same colored surfaces (matches the caption buttons'
        // pressed look on the title bar). Falls back to the hover brush on the tab strip.
        string? pressedKey = titleBarColored ? "RibbonKit.Brushes.CaptionButton.PressedBackground"
            : tabRowColored ? "RibbonKit.Brushes.Tab.HoverBackground"
            : null;

        foreach (object entry in QuickAccessItems)
        {
            if (entry is not FrameworkElement element)
            {
                continue;
            }

            SetQatOnColoredSurface(element, colored);
            if (colored && hoverKey is not null)
            {
                // Publish the band's brushes as RESOURCES on the proxy, not as attached-property
                // values. Template children (the dropdown/split PART_Toggle/PART_Primary chrome)
                // could not reliably read an inherited brush property through a Setter binding —
                // it came back null, and a Border whose trigger sets Background to null drops out
                // of hit-testing, so on the WindowChrome caption the click fell through to the
                // title bar. {DynamicResource} lookup, by contrast, walks the element tree from
                // the Chrome border up to this proxy, always finds these entries, and re-resolves
                // automatically when we rewrite them on a theme/accent change. Resolve the actual
                // brush from the Ribbon (guaranteed connected to the theme resource scope; a QAT
                // element may not be), and never store null — fall back to Transparent, which
                // stays hit-testable.
                element.Resources[QatColoredHoverBackgroundKey] =
                    TryFindResource(hoverKey) as System.Windows.Media.Brush
                    ?? System.Windows.Media.Brushes.Transparent;
                element.Resources[QatColoredPressedBackgroundKey] =
                    TryFindResource(pressedKey) as System.Windows.Media.Brush
                    ?? System.Windows.Media.Brushes.Transparent;
            }
            else
            {
                element.Resources.Remove(QatColoredHoverBackgroundKey);
                element.Resources.Remove(QatColoredPressedBackgroundKey);
            }
        }

        // The HOSTS need the same treatment, not just the items. The overflow chevron (»)
        // lives in RibbonQuickAccessToolBar's own TEMPLATE, so it is not a QuickAccessItems
        // entry and the loop above never reaches it — which is why it kept a dark stroke and a
        // grey hover chip on an accent title bar in every theme except 2019 (2019 got away with
        // it only because its colored strip also repaints TabStrip.Foreground white).
        // QatOnColoredSurface inherits, so setting it on the host carries it into the template.
        // Per-host, not one shared flag: only the host in the active placement is on the band.
        ApplyQatSurfaceContext(_titleBarQatHost, titleBarColored, hoverKey, pressedKey);
        ApplyQatSurfaceContext(_qatTabRowHost, tabRowColored, hoverKey, pressedKey);
        ApplyQatSurfaceContext(_qatBelowHost, false, hoverKey, pressedKey);
    }

    /// <summary>
    /// Marks a quick-access host as sitting (or not) on a colored band and publishes the band's
    /// hover/pressed brushes in its resource scope, so chrome inside the host's template can pick
    /// them up by <c>{DynamicResource}</c> exactly as the item buttons do.
    /// </summary>
    private void ApplyQatSurfaceContext(
        FrameworkElement? host,
        bool colored,
        string? hoverKey,
        string? pressedKey)
    {
        if (host is null)
        {
            return;
        }

        SetQatOnColoredSurface(host, colored);

        if (colored && hoverKey is not null && pressedKey is not null)
        {
            // Resolved against the RIBBON, not the host: a title-bar host lives outside this
            // control's visual tree and may not reach the theme scope. Never store null — a
            // Border whose Background trigger sets null drops out of hit-testing.
            host.Resources[QatColoredHoverBackgroundKey] =
                TryFindResource(hoverKey) as System.Windows.Media.Brush
                ?? System.Windows.Media.Brushes.Transparent;
            host.Resources[QatColoredPressedBackgroundKey] =
                TryFindResource(pressedKey) as System.Windows.Media.Brush
                ?? System.Windows.Media.Brushes.Transparent;
        }
        else
        {
            host.Resources.Remove(QatColoredHoverBackgroundKey);
            host.Resources.Remove(QatColoredPressedBackgroundKey);
        }
    }

    private static FrameworkElement? FindDescendantByName(DependencyObject root, string name)
    {
        int count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < count; i++)
        {
            DependencyObject child = System.Windows.Media.VisualTreeHelper.GetChild(root, i);
            if (child is FrameworkElement element && element.Name == name)
            {
                return element;
            }

            if (FindDescendantByName(child, name) is { } found)
            {
                return found;
            }
        }

        return null;
    }

    private static T? FindDescendantByType<T>(DependencyObject root) where T : DependencyObject
    {
        int count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < count; i++)
        {
            DependencyObject child = System.Windows.Media.VisualTreeHelper.GetChild(root, i);
            if (child is T match)
            {
                return match;
            }

            if (FindDescendantByType<T>(child) is { } found)
            {
                return found;
            }
        }

        return null;
    }

    private static void OnQuickAccessPositionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((Ribbon)d).UpdateQuickAccessPlacement();

    /// <summary>
    /// Projects the quick-access items into the host <see cref="RibbonWindow"/>'s title
    /// bar when <see cref="QuickAccessPosition"/> is <see cref="RibbonQuickAccessPosition.TitleBar"/>,
    /// and restores the window's prior title-bar content otherwise. Exactly one host binds
    /// the (single-parent) item elements at a time, so the switch reparents them cleanly.
    /// </summary>
    private void UpdateQuickAccessPlacement()
    {
        // The quick-access items are single-parent UIElements shared between hosts, so the
        // OLD host must release them before the NEW one claims them. When leaving the title
        // bar, release synchronously (the title-bar host is higher in the tree, so it frees
        // the items at the next layout before the in-ribbon host — lower — claims them).
        if (QuickAccessPosition != RibbonQuickAccessPosition.TitleBar && _titleBarQatHost is not null)
        {
            _titleBarQatHost.ItemsSource = null;
        }

        // Apply the final placement after a layout pass, so whichever host currently owns
        // the items has released them before we (re)claim — avoids a transient double-parent.
        Dispatcher.BeginInvoke(
            System.Windows.Threading.DispatcherPriority.Background,
            (Action)ApplyQuickAccessPlacement);
    }

    private void ApplyQuickAccessPlacement()
    {
        var window = Window.GetWindow(this) as RibbonWindow;

        if (QuickAccessPosition == RibbonQuickAccessPosition.TitleBar && window is not null)
        {
            _titleBarQatHost ??= CreateTitleBarQatHost();

            // Remember whatever the window was showing (unless it's already our host) so we
            // can put it back when the QAT leaves the title bar.
            if (!ReferenceEquals(window.TitleBarContent, _titleBarQatHost))
            {
                _savedTitleBarContent = window.TitleBarContent;
            }

            window.SetCurrentValue(RibbonWindow.TitleBarContentProperty, _titleBarQatHost);
            _titleBarQatHost.ItemsSource = QuickAccessItems;
        }
        else
        {
            if (_titleBarQatHost is not null)
            {
                _titleBarQatHost.ItemsSource = null;
            }

            if (window is not null && ReferenceEquals(window.TitleBarContent, _titleBarQatHost))
            {
                window.SetCurrentValue(RibbonWindow.TitleBarContentProperty, _savedTitleBarContent);
                _savedTitleBarContent = null;
            }
        }

        UpdateQatButtonContext();

        // The quick access strip entering or leaving the TAB ROW slides every tab header, so the
        // sliding underline and the 2010/2013 connect notch have to be re-placed.
        //
        // SizeChanged does NOT cover this, which is why it needs its own call: the placement
        // triggers toggle the tab-row host's VISIBILITY, and a Collapsed element is skipped by
        // layout entirely — it is never measured, keeps its stale RenderSize, and raises no
        // SizeChanged going either way.
        RequestSelectionVisualsRefresh();
    }

    private RibbonQuickAccessToolBar CreateTitleBarQatHost()
    {
        var host = new RibbonQuickAccessToolBar
        {
            VerticalAlignment = VerticalAlignment.Center,
        };

        // Themed, not hard-coded: the Office 2007 orb overhangs up into the caption, so that theme
        // reserves enough left inset here for the QAT to clear it. Every theme defines the key, and
        // the non-2007 values reproduce the previous hard-coded Thickness(2, 0, 6, 0).
        host.SetResourceReference(MarginProperty, "RibbonKit.Metrics.TitleBarQatMargin");

        // Bound, not assigned: the cap can be retuned at runtime, and the title bar is the
        // placement most likely to need it (the window title has to keep its room).
        host.SetBinding(
            MaxWidthProperty,
            new System.Windows.Data.Binding(nameof(QuickAccessMaxWidth)) { Source = this });

        AttachQatContextMenu(host);
        return host;
    }

    private System.Windows.Controls.ContextMenu EnsureQatContextMenu()
    {
        if (_qatContextMenu is not null)
        {
            return _qatContextMenu;
        }

        _qatTitleBarItem = new System.Windows.Controls.MenuItem
        {
            Header = RibbonLocalization.GetString(RibbonString.ShowQuickAccessToolbarInTitleBar),
        };
        _qatTitleBarItem.Click += (_, _) =>
            SetCurrentValue(QuickAccessPositionProperty, RibbonQuickAccessPosition.TitleBar);

        _qatAboveItem = new System.Windows.Controls.MenuItem
        {
            Header = RibbonLocalization.GetString(RibbonString.ShowQuickAccessToolbarAboveRibbon),
        };
        _qatAboveItem.Click += (_, _) =>
            SetCurrentValue(QuickAccessPositionProperty, RibbonQuickAccessPosition.TabRow);

        _qatBelowItem = new System.Windows.Controls.MenuItem
        {
            Header = RibbonLocalization.GetString(RibbonString.ShowQuickAccessToolbarBelowRibbon),
        };
        _qatBelowItem.Click += (_, _) =>
            SetCurrentValue(QuickAccessPositionProperty, RibbonQuickAccessPosition.BelowRibbon);

        // Shown only when the right-click landed on a quick-access ITEM (the hosts'
        // ContextMenuOpening captures which one into _qatMenuTarget).
        _qatRemoveItem = new System.Windows.Controls.MenuItem
        {
            Header = RibbonLocalization.GetString(RibbonString.RemoveFromQuickAccessToolbar),
        };
        _qatRemoveItem.Click += (_, _) =>
        {
            if (_qatMenuTarget is not null)
            {
                QuickAccessItems.Remove(_qatMenuTarget);
                _qatMenuTarget = null;
            }
        };

        _qatCustomizeItem = new System.Windows.Controls.MenuItem
        {
            Header = RibbonLocalization.GetString(RibbonString.CustomizeQuickAccessToolbar),
        };
        _qatCustomizeItem.Click += (_, _) => QuickAccessCustomizeRequested?.Invoke(this, EventArgs.Empty);

        _qatRemoveSeparator = new System.Windows.Controls.Separator();

        _qatContextMenu = new System.Windows.Controls.ContextMenu();
        ApplyModernMenuStyle(_qatContextMenu);
        _qatContextMenu.Items.Add(_qatRemoveItem);
        _qatContextMenu.Items.Add(_qatRemoveSeparator);
        _qatContextMenu.Items.Add(_qatTitleBarItem);
        _qatContextMenu.Items.Add(_qatAboveItem);
        _qatContextMenu.Items.Add(_qatBelowItem);
        _qatContextMenu.Items.Add(new System.Windows.Controls.Separator());
        _qatContextMenu.Items.Add(_qatCustomizeItem);
        _qatContextMenu.Opened += OnQatContextMenuOpened;
        return _qatContextMenu;
    }

    /// <summary>
    /// Attaches the shared QAT context menu to a host, plus the opening hook that records
    /// which quick-access item (if any) was under the cursor — the menu itself is shared,
    /// so the target must be captured per-open.
    /// </summary>
    /// <summary>
    /// Watches the tab-row quick access strip's size so the selection visuals follow it.
    /// </summary>
    /// <remarks>
    /// The strip is a SIBLING of the tab strip in the same row, so anything that changes its width
    /// slides every tab header sideways: the QAT moving into or out of the tab row, items being
    /// added or removed, the overflow button appearing. None of that changes the tab control's own
    /// size, so nothing re-places the sliding underline or the 2010/2013 connect notch — see
    /// <see cref="RefreshSelectionVisuals"/>. Handling it here covers every cause at once.
    /// </remarks>
    private void TrackTabRowQatSize(FrameworkElement tabRowHost)
    {
        tabRowHost.SizeChanged -= OnTabRowQatSizeChanged;
        tabRowHost.SizeChanged += OnTabRowQatSizeChanged;
    }

    private void OnTabRowQatSizeChanged(object sender, SizeChangedEventArgs e) =>
        RequestSelectionVisualsRefresh();

    /// <summary>
    /// Queues a selection-visual refresh for after the next layout pass, coalescing repeats.
    /// </summary>
    /// <remarks>
    /// Deferred rather than immediate for two reasons: <c>SizeChanged</c> fires DURING layout and
    /// <see cref="RefreshSelectionVisuals"/> forces a layout pass, so running it inline would
    /// re-enter layout from inside layout; and quick-access placement re-parents its items
    /// asynchronously, so the strip's final width isn't known until that settles.
    /// <c>Loaded</c> priority is below <c>Render</c>, which is what makes it "after layout".
    /// </remarks>
    private void RequestSelectionVisualsRefresh()
    {
        if (_selectionVisualsPending)
        {
            return;
        }

        _selectionVisualsPending = true;
        Dispatcher.BeginInvoke(
            System.Windows.Threading.DispatcherPriority.Loaded,
            (Action)(() =>
            {
                _selectionVisualsPending = false;
                RefreshSelectionVisuals();
            }));
    }

    private void AttachQatContextMenu(FrameworkElement host)
    {
        host.ContextMenu = EnsureQatContextMenu();
        host.ContextMenuOpening -= OnQatHostContextMenuOpening;
        host.ContextMenuOpening += OnQatHostContextMenuOpening;

        // Every quick-access host passes through here, which makes it the one place that can hand
        // the toolbar its owning ribbon. It can't find us by walking up: in the TitleBar placement
        // the toolbar lives in the window's title bar, outside the ribbon's visual tree.
        if (host is RibbonQuickAccessToolBar toolBar)
        {
            toolBar.Owner = this;
        }
    }

    private void OnQatHostContextMenuOpening(object sender, System.Windows.Controls.ContextMenuEventArgs e)
    {
        _qatMenuTarget = ResolveQuickAccessItem(e.OriginalSource as DependencyObject);
        if (sender is FrameworkElement { ContextMenu: { } menu } host)
        {
            PrepareQatContextMenu(host, menu);
        }
    }

    internal static RibbonSlideFrom BackstageSlideEdge(FlowDirection flowDirection) =>
        flowDirection == FlowDirection.RightToLeft
            ? RibbonSlideFrom.Right
            : RibbonSlideFrom.Left;

    private static void PrepareQatContextMenu(
        FrameworkElement host,
        System.Windows.Controls.ContextMenu menu)
    {
        // The menu is hosted in another popup visual tree, so it cannot inherit flow direction.
        menu.FlowDirection = host.FlowDirection;
        RibbonPopupMotion.SuppressNativeContextMenuAnimationForOpen(menu);
    }

    // Walks up from the right-clicked element to the element that is itself a member of
    // QuickAccessItems (the proxy/declared small button), or null when the click landed on
    // host chrome rather than an item.
    private FrameworkElement? ResolveQuickAccessItem(DependencyObject? node)
    {
        while (node is not null)
        {
            if (node is FrameworkElement element)
            {
                if (QuickAccessItems.Contains(element))
                {
                    return element;
                }

                // Inside the overflow flyout the clicked control is a PROXY, not a member of
                // QuickAccessItems — so without this, "Remove from Quick Access Toolbar" was
                // hidden for exactly the items the user most wants to remove. Map it back to the
                // real entry the proxy stands for.
                if (GetQuickAccessOverflowItem(element) is { } represented
                    && QuickAccessItems.Contains(represented))
                {
                    return represented;
                }
            }

            DependencyObject? next = node is System.Windows.Media.Visual or System.Windows.Media.Media3D.Visual3D
                ? System.Windows.Media.VisualTreeHelper.GetParent(node)
                : null;
            node = next ?? LogicalTreeHelper.GetParent(node);
        }

        return null;
    }

    private void OnQatContextMenuOpened(object sender, RoutedEventArgs e)
    {
        RefreshQatContextMenuText();

        // "Remove" applies only when the right-click landed on an actual QAT item.
        Visibility removeVisibility = _qatMenuTarget is null ? Visibility.Collapsed : Visibility.Visible;
        if (_qatRemoveItem is not null)
        {
            _qatRemoveItem.Visibility = removeVisibility;
        }

        if (_qatRemoveSeparator is not null)
        {
            _qatRemoveSeparator.Visibility = removeVisibility;
        }

        // Show a check next to the current placement.
        if (_qatTitleBarItem is not null)
        {
            _qatTitleBarItem.IsChecked = QuickAccessPosition == RibbonQuickAccessPosition.TitleBar;
        }

        if (_qatAboveItem is not null)
        {
            _qatAboveItem.IsChecked = QuickAccessPosition == RibbonQuickAccessPosition.TabRow;
        }

        if (_qatBelowItem is not null)
        {
            _qatBelowItem.IsChecked = QuickAccessPosition == RibbonQuickAccessPosition.BelowRibbon;
        }
    }

    private void RefreshQatContextMenuText()
    {
        if (_qatTitleBarItem is not null)
        {
            _qatTitleBarItem.Header = RibbonLocalization.GetString(RibbonString.ShowQuickAccessToolbarInTitleBar);
        }

        if (_qatAboveItem is not null)
        {
            _qatAboveItem.Header = RibbonLocalization.GetString(RibbonString.ShowQuickAccessToolbarAboveRibbon);
        }

        if (_qatBelowItem is not null)
        {
            _qatBelowItem.Header = RibbonLocalization.GetString(RibbonString.ShowQuickAccessToolbarBelowRibbon);
        }

        if (_qatRemoveItem is not null)
        {
            _qatRemoveItem.Header = RibbonLocalization.GetString(RibbonString.RemoveFromQuickAccessToolbar);
        }

        if (_qatCustomizeItem is not null)
        {
            _qatCustomizeItem.Header = RibbonLocalization.GetString(RibbonString.CustomizeQuickAccessToolbar);
        }
    }

    private void OnTabsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Track visibility so selection can escape a contextual tab that hides.
        if (e.OldItems is not null)
        {
            foreach (RibbonTab tab in e.OldItems)
            {
                tab.IsVisibleChanged -= OnTabIsVisibleChanged;

                // Removing the modal tab ends modal mode; removing any other drops its
                // recorded pre-modal visibility.
                _modalScope.OnTabRemoved(tab);

                // A merged tab removed by some other path (the customize page, an app editing
                // Tabs directly) must leave its merge record too, or unmerge chases a ghost.
                _mergeService.OnTabRemoved(tab);
            }
        }

        if (e.NewItems is not null)
        {
            foreach (RibbonTab tab in e.NewItems)
            {
                tab.IsVisibleChanged += OnTabIsVisibleChanged;

                // A tab arriving during modal mode is hidden immediately and recorded, so
                // exiting reveals it correctly.
                _modalScope.OnTabAdded(tab);
            }
        }

        // Tabs.Clear() reports a Reset with no OldItems — the customization serializer rebuilds
        // the collection that way, so reconcile modal state against the new contents.
        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            _modalScope.OnCollectionReset();
        }

        // Keep the selection valid as tabs come and go.
        if (SelectedTab is not null && !Tabs.Contains(SelectedTab))
        {
            SelectedTab = null;
        }

        // A SelectedIndex set before its target tab existed (including a design-time
        // d:SelectedIndex applied during tree construction) takes effect once the tab arrives.
        if (SelectedTab is null && SelectedIndex >= 0)
        {
            ApplySelectedIndex(SelectedIndex);
        }

        if (IsLoaded)
        {
            EnsureSelection();
        }
    }

    private void OnTabIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        // A selected tab that becomes invisible (a contextual tab deactivating)
        // hands selection to the first visible tab.
        if (sender is RibbonTab { IsVisible: false } tab && ReferenceEquals(SelectedTab, tab))
        {
            SelectedTab = FindFirstVisibleTab();
        }
    }

    private static void OnSelectedTabChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ribbon = (Ribbon)d;
        if (ribbon._syncingSelection)
        {
            return;
        }

        // Mirror the selected tab back into SelectedIndex so the two stay in lock-step.
        ribbon._syncingSelection = true;
        try
        {
            ribbon.SelectedIndex = e.NewValue is RibbonTab tab ? ribbon.Tabs.IndexOf(tab) : -1;
        }
        finally
        {
            ribbon._syncingSelection = false;
        }
    }

    private static void OnSelectedIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((Ribbon)d).ApplySelectedIndex((int)e.NewValue);

    private void ApplySelectedIndex(int index)
    {
        // Ignore re-entrancy from the SelectedTab mirror, and out-of-range indices — the tabs
        // may not be populated yet (a XAML attribute, or a design-time d:SelectedIndex, is
        // applied before the child tabs are parsed). OnTabsCollectionChanged re-applies a pending
        // index once the tabs exist.
        if (_syncingSelection || index < 0 || index >= Tabs.Count)
        {
            return;
        }

        _syncingSelection = true;
        try
        {
            SelectedTab = Tabs[index];
        }
        finally
        {
            _syncingSelection = false;
        }
    }

    private void EnsureSelection()
    {
        if (SelectedTab is not null || Tabs.Count == 0)
        {
            return;
        }

        // Honor a pending SelectedIndex (e.g. a design-time d:SelectedIndex applied before the
        // tabs were parsed); otherwise fall back to the first visible tab.
        int index = SelectedIndex;
        SelectedTab = index >= 0 && index < Tabs.Count ? Tabs[index] : FindFirstVisibleTab();
    }

    private RibbonTab? FindFirstVisibleTab() =>
        Tabs.FirstOrDefault(tab => tab.Visibility == Visibility.Visible);
}
