using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Markup;
using RibbonKit.Interop;

namespace RibbonKit.Controls;

/// <summary>
/// A Word-style options dialog: a navigation rail of pages on the left, the selected
/// page's content on the right, and OK / Cancel at the bottom. Pages are
/// <see cref="RibbonOptionsPage"/>s whose content can be ANY element — including the
/// application's own user controls — so ribbon customization pages (for example
/// <see cref="RibbonQuickAccessPage"/>) and the app's options pages merge into one
/// dialog, like Office's Word Options.
/// <code language="xaml">
/// &lt;rk:RibbonOptionsDialog Title="Options"&gt;
///     &lt;rk:RibbonOptionsPage Header="General"&gt;&lt;local:GeneralOptionsView /&gt;&lt;/rk:RibbonOptionsPage&gt;
///     &lt;rk:RibbonOptionsPage Header="Quick Access Toolbar"&gt;
///         &lt;rk:RibbonQuickAccessPage Ribbon="{Binding ElementName=MainRibbon}" /&gt;
///     &lt;/rk:RibbonOptionsPage&gt;
/// &lt;/rk:RibbonOptionsDialog&gt;
/// </code>
/// </summary>
/// <remarks>
/// Result flow: OK raises <see cref="Applied"/> (the app's cue to persist settings) and
/// closes with <c>DialogResult = true</c>; Cancel closes with <c>false</c>. So the host
/// can either subscribe to <see cref="Applied"/> or check the <c>ShowDialog()</c> return.
/// </remarks>
[ContentProperty(nameof(Pages))]
[TemplatePart(Name = PageListPartName, Type = typeof(Selector))]
[TemplatePart(Name = ContentScrollPartName, Type = typeof(ScrollViewer))]
[TemplatePart(Name = OkButtonPartName, Type = typeof(ButtonBase))]
[TemplatePart(Name = CancelButtonPartName, Type = typeof(ButtonBase))]
[TemplatePart(Name = CloseButtonPartName, Type = typeof(ButtonBase))]
public class RibbonOptionsDialog : Window
{
    private const string PageListPartName = "PART_PageList";
    private const string ContentScrollPartName = "PART_ContentScroll";
    private const string OkButtonPartName = "PART_OkButton";
    private const string CancelButtonPartName = "PART_CancelButton";
    private const string CloseButtonPartName = "PART_CloseButton";

    private static readonly DependencyPropertyKey PagesPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(Pages),
            typeof(ObservableCollection<RibbonOptionsPage>),
            typeof(RibbonOptionsDialog),
            new FrameworkPropertyMetadata(null));

    /// <summary>Identifies the read-only <see cref="Pages"/> dependency property.</summary>
    public static readonly DependencyProperty PagesProperty = PagesPropertyKey.DependencyProperty;

    /// <summary>Identifies the <see cref="SelectedPage"/> dependency property.</summary>
    public static readonly DependencyProperty SelectedPageProperty =
        DependencyProperty.Register(
            nameof(SelectedPage),
            typeof(RibbonOptionsPage),
            typeof(RibbonOptionsDialog),
            new FrameworkPropertyMetadata(
                null,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnSelectedPageChanged));

    private ScrollViewer? _contentScroll;
    private ButtonBase? _okButton;
    private ButtonBase? _cancelButton;
    private ButtonBase? _closeButton;

    static RibbonOptionsDialog()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(RibbonOptionsDialog),
            new FrameworkPropertyMetadata(typeof(RibbonOptionsDialog)));
    }

    /// <summary>Initializes the dialog with sensible modal-dialog defaults.</summary>
    public RibbonOptionsDialog()
    {
        SetValue(PagesPropertyKey, new ObservableCollection<RibbonOptionsPage>());
        Width = 860;
        Height = 600;
        MinWidth = 560;
        MinHeight = 400;
        ShowInTaskbar = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        // Custom chrome: the template draws a white title bar with a single Close button
        // (no icon, no minimize/maximize — a modal dialog needs none). WindowChrome (in the
        // theme style) makes that title bar draggable and the borders resize-grabbable.
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.CanResize;

        // A page must be selected for the content area to show anything; default to the
        // first page once the dialog is up (unless the caller pre-selected one).
        Loaded += (_, _) => EnsurePageSelection();
    }

    /// <summary>The dialog's pages. Declare them as direct XAML content.</summary>
    public ObservableCollection<RibbonOptionsPage> Pages =>
        (ObservableCollection<RibbonOptionsPage>)GetValue(PagesProperty);

    /// <summary>The page whose content is currently shown.</summary>
    public RibbonOptionsPage? SelectedPage
    {
        get => (RibbonOptionsPage?)GetValue(SelectedPageProperty);
        set => SetValue(SelectedPageProperty, value);
    }

    /// <inheritdoc />
    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        // WindowStyle=None windows don't get Windows 11 rounded corners for free — ask the DWM
        // to round them (with a light border so the white dialog reads against a white backdrop).
        MicaHelper.SetRoundedCorners(this, borderColor: 0x00E0E0E0);
    }

    /// <summary>
    /// Raised when OK is pressed, immediately before the dialog closes with
    /// <c>DialogResult = true</c> — the host's cue to persist whatever the pages changed.
    /// </summary>
    public event EventHandler? Applied;

    /// <inheritdoc />
    public override void OnApplyTemplate()
    {
        if (_okButton is not null)
        {
            _okButton.Click -= OnOkClick;
        }

        if (_cancelButton is not null)
        {
            _cancelButton.Click -= OnCancelClick;
        }

        if (_closeButton is not null)
        {
            _closeButton.Click -= OnCancelClick;
        }

        base.OnApplyTemplate();

        _contentScroll = GetTemplateChild(ContentScrollPartName) as ScrollViewer;
        _okButton = GetTemplateChild(OkButtonPartName) as ButtonBase;
        _cancelButton = GetTemplateChild(CancelButtonPartName) as ButtonBase;
        _closeButton = GetTemplateChild(CloseButtonPartName) as ButtonBase;

        UpdateContentScrollMode();

        if (_okButton is not null)
        {
            _okButton.Click += OnOkClick;
        }

        if (_cancelButton is not null)
        {
            _cancelButton.Click += OnCancelClick;
        }

        // The title-bar Close button is a cancel (no Applied), like clicking Cancel.
        if (_closeButton is not null)
        {
            _closeButton.Click += OnCancelClick;
        }
    }

    private void EnsurePageSelection()
    {
        if (SelectedPage is null && Pages.Count > 0)
        {
            SelectedPage = Pages[0];
        }
    }

    private static void OnSelectedPageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((RibbonOptionsDialog)d).UpdateContentScrollMode();

    /// <summary>
    /// Chooses how the current page is hosted: a page whose content is an
    /// <see cref="IRibbonFillPage"/> gets a NON-scrolling viewport (vertical scroll disabled),
    /// so the ScrollViewer constrains it to the content area and it fills (and scrolls its own
    /// inner regions). Any other page keeps <see cref="ScrollBarVisibility.Auto"/> so tall
    /// content scrolls in the dialog — the convenient default for arbitrary app pages.
    /// </summary>
    private void UpdateContentScrollMode()
    {
        if (_contentScroll is null)
        {
            return;
        }

        _contentScroll.VerticalScrollBarVisibility = SelectedPage?.Content is IRibbonFillPage
            ? ScrollBarVisibility.Disabled
            : ScrollBarVisibility.Auto;
    }

    private void OnOkClick(object sender, RoutedEventArgs e)
    {
        Applied?.Invoke(this, EventArgs.Empty);
        CloseWithResult(true);
    }

    private void OnCancelClick(object sender, RoutedEventArgs e) => CloseWithResult(false);

    private void CloseWithResult(bool result)
    {
        try
        {
            // Only valid for ShowDialog(); setting it also closes the window.
            DialogResult = result;
        }
        catch (InvalidOperationException)
        {
            // Shown modeless via Show() — just close; the host has the Applied event.
            Close();
        }
    }
}

/// <summary>
/// Marks a page's <see cref="RibbonOptionsPage.Content"/> as self-managing its vertical space.
/// The <see cref="RibbonOptionsDialog"/> then hosts it WITHOUT a scrolling viewport, so it fills
/// the content area (and scrolls its own inner regions) instead of the dialog scrolling it.
/// Content that does NOT implement this gets the dialog's scrollbar when it's taller than the
/// content area — the convenient default for arbitrary application pages. The built-in
/// <see cref="RibbonQuickAccessPage"/> implements this so its command lists scroll internally.
/// </summary>
public interface IRibbonFillPage
{
}

/// <summary>
/// One page of a <see cref="RibbonOptionsDialog"/>: a <see cref="HeaderedContentControl.Header"/>
/// shown in the navigation rail, and any <see cref="ContentControl.Content"/> — typically the
/// application's own user control, or a RibbonKit customization page. The page's own template
/// renders ONLY the header (it is the nav entry); the dialog presents the content separately.
/// </summary>
public class RibbonOptionsPage : HeaderedContentControl
{
    static RibbonOptionsPage()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(RibbonOptionsPage),
            new FrameworkPropertyMetadata(typeof(RibbonOptionsPage)));
    }
}
