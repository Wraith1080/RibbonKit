using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using RibbonKit.Interop;
using RibbonKit.Theming;

namespace RibbonKit.Controls;

/// <summary>The optional frame presentation composed by <see cref="RibbonWindow"/>.</summary>
public enum RibbonWindowFrameAppearance
{
    /// <summary>Use the selected theme's ordinary title and client-edge treatment.</summary>
    Default,

    /// <summary>
    /// Use the Office 2007 Aero-inspired restored frame and title treatment. This visual choice
    /// does not request a system backdrop; hosts may independently apply
    /// <see cref="RibbonBackdrop.Acrylic"/> through <see cref="MicaHelper"/>.
    /// </summary>
    Office2007Aero,
}

/// <summary>
/// A window with Office-style chrome: a custom title bar hosting the window title,
/// optional <see cref="TitleBarContent"/> (quick access buttons live well there),
/// and themed caption buttons — while keeping native behaviors (drag, double-click
/// maximize, resize borders, system menu) via <see cref="System.Windows.Shell.WindowChrome"/>.
/// <code language="xaml">
/// &lt;rk:RibbonWindow ...&gt;
///     &lt;rk:RibbonWindow.TitleBarContent&gt;
///         &lt;StackPanel Orientation="Horizontal"&gt; ...quick access buttons... &lt;/StackPanel&gt;
///     &lt;/rk:RibbonWindow.TitleBarContent&gt;
///     ...
/// &lt;/rk:RibbonWindow&gt;
/// </code>
/// </summary>
[TemplatePart(Name = WindowRootPartName, Type = typeof(FrameworkElement))]
[TemplatePart(Name = TitlePartName, Type = typeof(FrameworkElement))]
[TemplatePart(Name = WindowIconPartName, Type = typeof(Image))]
[TemplatePart(Name = MaximizeButtonPartName, Type = typeof(FrameworkElement))]
[TemplatePart(Name = RestoreButtonPartName, Type = typeof(FrameworkElement))]
public class RibbonWindow : Window
{
    private const string WindowRootPartName = "PART_WindowRoot";
    private const string TitlePartName = "PART_Title";
    private const string WindowIconPartName = "PART_WindowIcon";
    private const string MaximizeButtonPartName = "PART_MaximizeButton";
    private const string RestoreButtonPartName = "PART_RestoreButton";
    private const string AeroCaptionHoverKey =
        "RibbonKit.Brushes.WindowFrame.AeroCaptionHover";
    private const string AeroCaptionPressedKey =
        "RibbonKit.Brushes.WindowFrame.AeroCaptionPressed";

    private static readonly DependencyPropertyKey ActiveBackdropPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(ActiveBackdrop),
            typeof(RibbonBackdrop),
            typeof(RibbonWindow),
            new FrameworkPropertyMetadata(RibbonBackdrop.None));

    /// <summary>Identifies the <see cref="TitleBarContent"/> dependency property.</summary>
    public static readonly DependencyProperty TitleBarContentProperty =
        DependencyProperty.Register(
            nameof(TitleBarContent),
            typeof(object),
            typeof(RibbonWindow),
            new FrameworkPropertyMetadata(null));

    /// <summary>Identifies the <see cref="IsTitleBarContentVisible"/> dependency property.</summary>
    public static readonly DependencyProperty IsTitleBarContentVisibleProperty =
        DependencyProperty.Register(
            nameof(IsTitleBarContentVisible),
            typeof(bool),
            typeof(RibbonWindow),
            new FrameworkPropertyMetadata(true, OnIsTitleBarContentVisibleChanged));

    /// <summary>Identifies the <see cref="FrameAppearance"/> dependency property.</summary>
    public static readonly DependencyProperty FrameAppearanceProperty =
        DependencyProperty.Register(
            nameof(FrameAppearance),
            typeof(RibbonWindowFrameAppearance),
            typeof(RibbonWindow),
            new FrameworkPropertyMetadata(
                RibbonWindowFrameAppearance.Default,
                OnFrameAppearanceChanged));

    /// <summary>Identifies the <see cref="AeroFrameTint"/> dependency property.</summary>
    public static readonly DependencyProperty AeroFrameTintProperty =
        DependencyProperty.Register(
            nameof(AeroFrameTint),
            typeof(Brush),
            typeof(RibbonWindow),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Identifies the <see cref="AeroFrameTintIntensity"/> dependency property.</summary>
    public static readonly DependencyProperty AeroFrameTintIntensityProperty =
        DependencyProperty.Register(
            nameof(AeroFrameTintIntensity),
            typeof(double),
            typeof(RibbonWindow),
            new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsRender),
            IsValidAeroFrameTintIntensity);

    /// <summary>Identifies the read-only <see cref="ActiveBackdrop"/> dependency property.</summary>
    public static readonly DependencyProperty ActiveBackdropProperty =
        ActiveBackdropPropertyKey.DependencyProperty;

    private FrameworkElement? _windowRoot;
    private FrameworkElement? _title;
    private Image? _windowIcon;
    private Button? _maximizeButton;
    private Button? _restoreButton;
    private bool _snapButtonPressed;

    // Pending title-shift capture: where the title was BEFORE the layout change, and whether the
    // one-shot LayoutUpdated handler that consumes it is currently subscribed.
    private double _titleShiftFrom;
    private bool _titleShiftPending;
    private readonly HashSet<Ribbon> _orbApplicationButtonOwners = [];

    internal bool IsTitleBarIconSuppressed => _orbApplicationButtonOwners.Count > 0;

    static RibbonWindow()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(RibbonWindow),
            new FrameworkPropertyMetadata(typeof(RibbonWindow)));
    }

    /// <summary>Initializes the window and wires the caption button commands.</summary>
    public RibbonWindow()
    {
        CommandBindings.Add(new System.Windows.Input.CommandBinding(
            SystemCommands.MinimizeWindowCommand,
            (_, _) => SystemCommands.MinimizeWindow(this)));
        CommandBindings.Add(new System.Windows.Input.CommandBinding(
            SystemCommands.MaximizeWindowCommand,
            (_, _) => SystemCommands.MaximizeWindow(this)));
        CommandBindings.Add(new System.Windows.Input.CommandBinding(
            SystemCommands.RestoreWindowCommand,
            (_, _) => SystemCommands.RestoreWindow(this)));
        CommandBindings.Add(new System.Windows.Input.CommandBinding(
            SystemCommands.CloseWindowCommand,
            (_, _) => SystemCommands.CloseWindow(this)));

        // A maximized WindowChrome window is resized by Windows to hang past every screen
        // edge (the layout of the resize frame), and re-measuring on SizeChanged keeps the
        // compensation inset current if the window is dragged to a monitor of a different
        // resolution/DPI while maximized.
        SizeChanged += (_, _) => UpdateMaximizeInset();
        ThemeManager.Changed += OnThemeManagerChanged;
        Closed += (_, _) => ThemeManager.Changed -= OnThemeManagerChanged;
    }

    /// <summary>
    /// Content shown in the title bar between the window edge and the centered title —
    /// the natural home for quick access buttons.
    /// </summary>
    public object? TitleBarContent
    {
        get => GetValue(TitleBarContentProperty);
        set => SetValue(TitleBarContentProperty, value);
    }

    /// <summary>
    /// Whether <see cref="TitleBarContent"/> is currently shown. The hosted
    /// <see cref="Ribbon"/> sets this false while its backstage is open, matching
    /// Office.
    /// </summary>
    public bool IsTitleBarContentVisible
    {
        get => (bool)GetValue(IsTitleBarContentVisibleProperty);
        set => SetValue(IsTitleBarContentVisibleProperty, value);
    }

    /// <summary>
    /// Gets or sets the optional frame presentation. Selecting
    /// <see cref="RibbonWindowFrameAppearance.Office2007Aero"/> changes only RibbonKit-authored
    /// geometry and overlays; it never enables Acrylic or another DWM material by itself.
    /// </summary>
    public RibbonWindowFrameAppearance FrameAppearance
    {
        get => (RibbonWindowFrameAppearance)GetValue(FrameAppearanceProperty);
        set => SetValue(FrameAppearanceProperty, value);
    }

    /// <summary>
    /// Gets or sets the tint brush used by the Aero-inspired frame and title overlays. The shared
    /// style supplies the selected theme's default brush; setting a local value lets a host use an
    /// accent or another app-owned frame color without changing ribbon theme resources.
    /// </summary>
    public Brush? AeroFrameTint
    {
        get => (Brush?)GetValue(AeroFrameTintProperty);
        set => SetValue(AeroFrameTintProperty, value);
    }

    /// <summary>
    /// Gets or sets the opacity applied to <see cref="AeroFrameTint"/>, from 0 (no authored tint)
    /// through 1 (fully opaque tint). Reflection, grain, and the inner highlight remain separate.
    /// </summary>
    public double AeroFrameTintIntensity
    {
        get => (double)GetValue(AeroFrameTintIntensityProperty);
        set => SetValue(AeroFrameTintIntensityProperty, value);
    }

    /// <summary>
    /// Gets the system backdrop most recently accepted for this window through
    /// <see cref="MicaHelper.TrySetBackdrop"/>. This is derived runtime state, not an appearance
    /// preference to serialize.
    /// </summary>
    public RibbonBackdrop ActiveBackdrop => (RibbonBackdrop)GetValue(ActiveBackdropProperty);

    internal void SetActiveBackdrop(RibbonBackdrop backdrop) =>
        SetValue(ActiveBackdropPropertyKey, backdrop);

    private static bool IsValidAeroFrameTintIntensity(object value) =>
        value is double intensity
        && double.IsFinite(intensity)
        && intensity is >= 0d and <= 1d;

    private static void OnFrameAppearanceChanged(
        DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs e)
    {
        var window = (RibbonWindow)dependencyObject;
        window._snapButtonPressed = false;
        window.SetSnapButtonVisualState(SnapButtonVisualState.Normal);
    }

    /// <inheritdoc />
    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        // Drop any pending title-shift subscription while the OLD part is still in hand — after
        // the reassignment below there is no way to unhook it, and it would keep the discarded
        // element alive.
        if (_titleShiftPending && _title is not null)
        {
            _title.LayoutUpdated -= OnTitleLayoutUpdated;
            _titleShiftPending = false;
        }

        _windowRoot = GetTemplateChild(WindowRootPartName) as FrameworkElement;
        _title = GetTemplateChild(TitlePartName) as FrameworkElement;
        _windowIcon = GetTemplateChild(WindowIconPartName) as Image;
        _maximizeButton = GetTemplateChild(MaximizeButtonPartName) as Button;
        _restoreButton = GetTemplateChild(RestoreButtonPartName) as Button;
        UpdateTitleBarIconVisibility();
        SetSnapButtonVisualState(SnapButtonVisualState.Normal);
        UpdateMaximizeInset();
    }

    internal void UpdateApplicationButtonShape(
        Ribbon owner,
        RibbonApplicationButtonShape shape)
    {
        bool changed = shape == RibbonApplicationButtonShape.Orb
            ? _orbApplicationButtonOwners.Add(owner)
            : _orbApplicationButtonOwners.Remove(owner);

        if (changed)
        {
            UpdateTitleBarIconVisibility();
        }
    }

    internal void UnregisterApplicationButton(Ribbon owner)
    {
        if (_orbApplicationButtonOwners.Remove(owner))
        {
            UpdateTitleBarIconVisibility();
        }
    }

    private void UpdateTitleBarIconVisibility()
    {
        if (_windowIcon is null)
        {
            return;
        }

        Visibility before = _windowIcon.Visibility;
        if (IsTitleBarIconSuppressed)
        {
            _windowIcon.SetCurrentValue(VisibilityProperty, Visibility.Collapsed);
        }
        else
        {
            // Return control to the template: its null-Icon trigger still collapses the slot
            // without changing Window.Icon, which remains the executable/taskbar identity.
            _windowIcon.ClearValue(VisibilityProperty);
        }

        if (_windowIcon.Visibility != before)
        {
            AnimateTitleShift();
        }
    }

    private static void OnIsTitleBarContentVisibleChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e)
        => ((RibbonWindow)d).AnimateTitleShift();

    /// <summary>
    /// Glides the centered title to its new position when <see cref="TitleBarContent"/> appears
    /// or disappears, instead of letting it jump.
    /// </summary>
    /// <remarks>
    /// The title lives in the star column between the quick-access slot and the caption buttons,
    /// so collapsing that slot moves the column's centre by half the slot's width — a visible
    /// teleport every time the backstage opens or closes.
    /// <para>
    /// Measure-then-remeasure ("FLIP"), not arithmetic on the slot's width: the layout that
    /// produces the shift involves an Auto column, a themed margin (Office 2007 insets the slot to
    /// clear the overhanging orb) and a trimmed TextBlock, so anything computed by hand would
    /// drift from what actually renders. The first measurement is safe to take inline regardless
    /// of whether the template trigger has already collapsed the slot: hit-test geometry still
    /// reflects the last COMPLETED layout until the next pass runs.
    /// </para>
    /// <para>
    /// The second is taken in a one-shot <c>LayoutUpdated</c> handler,
    /// NOT on a dispatcher hop. <c>LayoutUpdated</c> fires at the end of the arrange pass, still
    /// inside the frame that is about to be presented, so the start offset is in place before
    /// anything reaches the screen. A <c>DispatcherPriority.Loaded</c> callback runs AFTER Render
    /// priority, which let the composition thread present one frame with the title already at its
    /// destination — the intermittent "snaps to the end, then animates properly" flicker.
    /// </para>
    /// <para>
    /// The two measurements are deliberately asymmetric. The BEFORE value includes any transform
    /// still running from a previous toggle, because that is where the title visually is right
    /// now; the AFTER value subtracts it, because that transform is about to be replaced and what
    /// is wanted is the resting position. Reading both the same way would make a fast
    /// open-close-open sequence jump.
    /// </para>
    /// </remarks>
    private void AnimateTitleShift()
    {
        if (_title is null || !IsLoaded)
        {
            return;
        }

        double before = GetTitleOffset(includeTransform: true);
        if (double.IsNaN(before))
        {
            return;
        }

        // A toggle arriving while a capture is still pending has not moved anything yet (no
        // layout pass has run in between), so the newest reading is the right one to keep —
        // but the handler must not be subscribed twice.
        _titleShiftFrom = before;
        if (_titleShiftPending)
        {
            return;
        }

        _titleShiftPending = true;
        _title.LayoutUpdated += OnTitleLayoutUpdated;
    }

    private void OnTitleLayoutUpdated(object? sender, EventArgs e)
    {
        if (_title is not null)
        {
            _title.LayoutUpdated -= OnTitleLayoutUpdated;
        }

        _titleShiftPending = false;

        double after = GetTitleOffset(includeTransform: false);
        if (double.IsNaN(after))
        {
            return;
        }

        double delta = _titleShiftFrom - after;

        // Sub-pixel shifts aren't worth a storyboard, and animating one would only add a
        // frame of jitter.
        if (Math.Abs(delta) < 0.5)
        {
            return;
        }

        if (_title is not { } title)
        {
            return;
        }

        double localDelta = ConvertAncestorDeltaToLocalX(title, this, delta);
        if (double.IsNaN(localDelta))
        {
            return;
        }

        Animation.RibbonMotion.AnimateTranslateX(
            title,
            Animation.RibbonAnimationAction.Backstage,
            localDelta,
            0d);
    }

    /// <summary>
    /// The title's horizontal offset within this window, or <see cref="double.NaN"/> when it
    /// cannot be measured (no template, collapsed, or not yet connected).
    /// </summary>
    private double GetTitleOffset(bool includeTransform)
    {
        if (_title is null || !_title.IsVisible)
        {
            return double.NaN;
        }

        try
        {
            GeneralTransform transform = _title.TransformToAncestor(this);
            Point origin = transform.Transform(default);
            double x = origin.X;
            if (!includeTransform && _title.RenderTransform is TranslateTransform translate)
            {
                double ancestorXPerLocalX = AncestorXPerLocalX(transform, origin);
                if (Math.Abs(ancestorXPerLocalX) < 1e-9)
                {
                    return double.NaN;
                }

                x -= translate.X * ancestorXPerLocalX;
            }

            return x;
        }
        catch (InvalidOperationException)
        {
            // Not a descendant yet (template swap mid-flight) — skip the animation.
            return double.NaN;
        }
    }

    /// <summary>
    /// Converts a horizontal displacement measured in an ancestor's physical coordinates into
    /// the local X displacement that produces it on <paramref name="element"/>.
    /// </summary>
    /// <remarks>
    /// RibbonWindow deliberately keeps its outer frame LTR and mirrors a nested logical host.
    /// Across that boundary a positive local X displacement becomes a negative window-space
    /// displacement. Applying a window-space FLIP delta directly therefore makes the title
    /// overshoot to the wrong side in RTL. Deriving the axis from the realized transform also
    /// remains correct for custom templates that introduce scaling or a different boundary.
    /// </remarks>
    internal static double ConvertAncestorDeltaToLocalX(
        FrameworkElement element,
        Visual ancestor,
        double ancestorDelta)
    {
        try
        {
            GeneralTransform transform = element.TransformToAncestor(ancestor);
            Point origin = transform.Transform(default);
            double ancestorXPerLocalX = AncestorXPerLocalX(transform, origin);
            return Math.Abs(ancestorXPerLocalX) < 1e-9
                ? double.NaN
                : ancestorDelta / ancestorXPerLocalX;
        }
        catch (InvalidOperationException)
        {
            return double.NaN;
        }
    }

    private static double AncestorXPerLocalX(GeneralTransform transform, Point origin) =>
        transform.Transform(new Point(1d, 0d)).X - origin.X;

    /// <inheritdoc />
    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        ApplyNativeDarkMode();

        // First line of defence: ask Windows to keep the maximized window inside the
        // monitor's WORK AREA (so it respects the taskbar and doesn't overhang). This is
        // the classic WM_GETMINMAXINFO fix and it's enough for a bare Window — but a
        // WindowChrome window re-introduces the overhang through its own (miscalculated)
        // non-client frame sizing, so the measured inset below is what actually guarantees
        // the caption buttons and ribbon stay on-screen. Keeping the hook is still worth
        // it: when it does constrain the window, the measured overhang simply comes out as
        // zero, so the two mechanisms never double up.
        IntPtr handle = new WindowInteropHelper(this).EnsureHandle();
        HwndSource.FromHwnd(handle)?.AddHook(WindowHook);
        UpdateMaximizeInset();
    }

    private void OnThemeManagerChanged(object? sender, EventArgs e)
    {
        if (new WindowInteropHelper(this).Handle != IntPtr.Zero)
        {
            ApplyNativeDarkMode();
        }
    }

    private void ApplyNativeDarkMode()
    {
        RibbonTheme theme = ThemeManager.CurrentTheme ?? RibbonTheme.Office2024;
        bool dark = ThemeManager.IsDarkMode && ThemeManager.SupportsDarkMode(theme);
        MicaHelper.TrySetDarkMode(this, dark);
    }

    /// <inheritdoc />
    protected override void OnStateChanged(EventArgs e)
    {
        base.OnStateChanged(e);
        _snapButtonPressed = false;
        SetSnapButtonVisualState(SnapButtonVisualState.Normal);
        UpdateMaximizeInset();
    }

    /// <inheritdoc />
    protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
    {
        base.OnDpiChanged(oldDpi, newDpi);
        UpdateMaximizeInset();
    }

    /// <summary>
    /// Insets the window root by the exact amount the maximized window spills past the
    /// monitor's work area, so content sits flush at the visible edges (nothing clipped,
    /// the caption buttons stay on-screen, and the ribbon card keeps its side margin).
    /// The overhang is MEASURED from the real window rect vs. the monitor rect and
    /// converted from device pixels to DIPs, so it is exact at every DPI — no reliance on
    /// <see cref="SystemParameters.WindowResizeBorderThickness"/>, whose value is
    /// ambiguous across Windows versions and is what makes the usual fixes flaky.
    /// </summary>
    private void UpdateMaximizeInset()
    {
        if (_windowRoot is null)
        {
            return;
        }

        Thickness target = default;

        if (WindowState == WindowState.Maximized)
        {
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd != IntPtr.Zero
                && GetWindowRect(hwnd, out NativeRect win))
            {
                IntPtr monitor = MonitorFromWindow(hwnd, MonitorDefaultToNearest);
                var monitorInfo = new NativeMonitorInfo { cbSize = Marshal.SizeOf<NativeMonitorInfo>() };
                if (monitor != IntPtr.Zero && GetMonitorInfo(monitor, ref monitorInfo))
                {
                    NativeRect work = monitorInfo.rcWork;
                    DpiScale dpi = VisualTreeHelper.GetDpi(this);
                    double sx = dpi.DpiScaleX <= 0 ? 1.0 : dpi.DpiScaleX;
                    double sy = dpi.DpiScaleY <= 0 ? 1.0 : dpi.DpiScaleY;

                    target = CalculateMaximizeInset(
                        new Rect(
                            win.Left,
                            win.Top,
                            win.Right - win.Left,
                            win.Bottom - win.Top),
                        new Rect(
                            work.Left,
                            work.Top,
                            work.Right - work.Left,
                            work.Bottom - work.Top),
                        new DpiScale(sx, sy));
                }
            }
        }

        // Avoid re-triggering layout (SizeChanged -> UpdateMaximizeInset -> ...) when the
        // inset hasn't actually changed.
        if (!ThicknessesClose(_windowRoot.Margin, target))
        {
            _windowRoot.Margin = target;
        }
    }

    /// <summary>
    /// Converts a maximized WindowChrome overhang from device pixels to the WPF margin that keeps
    /// authored content flush with the monitor work area. Keeping this geometry independent from
    /// the HWND query makes every supported DPI and negative-coordinate monitor arrangement
    /// directly testable without changing the user's display configuration.
    /// </summary>
    internal static Thickness CalculateMaximizeInset(
        Rect windowRectPixels,
        Rect workAreaPixels,
        DpiScale dpi)
    {
        double sx = dpi.DpiScaleX <= 0d ? 1d : dpi.DpiScaleX;
        double sy = dpi.DpiScaleY <= 0d ? 1d : dpi.DpiScaleY;

        return new Thickness(
            Math.Max(0d, workAreaPixels.Left - windowRectPixels.Left) / sx,
            Math.Max(0d, workAreaPixels.Top - windowRectPixels.Top) / sy,
            Math.Max(0d, windowRectPixels.Right - workAreaPixels.Right) / sx,
            Math.Max(0d, windowRectPixels.Bottom - workAreaPixels.Bottom) / sy);
    }

    private static bool ThicknessesClose(Thickness a, Thickness b)
    {
        const double eps = 0.5;
        return Math.Abs(a.Left - b.Left) < eps
            && Math.Abs(a.Top - b.Top) < eps
            && Math.Abs(a.Right - b.Right) < eps
            && Math.Abs(a.Bottom - b.Bottom) < eps;
    }

    private IntPtr WindowHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        const int WmGetMinMaxInfo = 0x0024;
        const int WmNcHitTest = 0x0084;
        const int WmNcMouseMove = 0x00a0;
        const int WmNcLButtonDown = 0x00a1;
        const int WmNcLButtonUp = 0x00a2;
        const int WmNcLButtonDoubleClick = 0x00a3;
        const int WmCancelMode = 0x001f;
        const int WmCaptureChanged = 0x0215;
        const int WmNcMouseLeave = 0x02a2;
        const int HtMaxButton = 9;

        // Windows 11 discovers the Snap Layout hover surface through non-client hit testing.
        // WindowChrome.IsHitTestVisibleInChrome deliberately makes our themed caption buttons
        // ordinary WPF client controls, so advertise just the visible maximize/restore bounds as
        // HTMAXBUTTON. Windows then owns the flyout while the same custom visuals remain in place.
        if (msg == WmNcHitTest)
        {
            bool isOverButton = IsOverMaximizeOrRestoreButton(lParam);
            SetSnapButtonVisualState(
                isOverButton
                    ? _snapButtonPressed
                        ? SnapButtonVisualState.Pressed
                        : SnapButtonVisualState.Hot
                    : SnapButtonVisualState.Normal);

            if (isOverButton)
            {
                TrackNonClientMouseLeave(hwnd);
                handled = true;
                return new IntPtr(HtMaxButton);
            }
        }

        if (msg == WmNcMouseMove && wParam.ToInt64() == HtMaxButton)
        {
            SetSnapButtonVisualState(
                _snapButtonPressed
                    ? SnapButtonVisualState.Pressed
                    : SnapButtonVisualState.Hot);
            TrackNonClientMouseLeave(hwnd);
        }

        // HTMAXBUTTON turns the WPF control into a native non-client target. Handle its button
        // messages here so the themed control still gets a real pressed state and exactly one
        // maximize/restore action; forwarding them to DefWindowProc would instead ask Windows to
        // paint and invoke a native caption button over our custom one (visible through Mica).
        if ((msg == WmNcLButtonDown || msg == WmNcLButtonDoubleClick)
            && wParam.ToInt64() == HtMaxButton)
        {
            _snapButtonPressed = true;
            SetSnapButtonVisualState(SnapButtonVisualState.Pressed);
            handled = true;
            return IntPtr.Zero;
        }

        if (msg == WmNcLButtonUp && _snapButtonPressed)
        {
            bool invoke = IsOverMaximizeOrRestoreButton(lParam);
            _snapButtonPressed = false;
            SetSnapButtonVisualState(
                invoke ? SnapButtonVisualState.Hot : SnapButtonVisualState.Normal);
            handled = true;

            if (invoke)
            {
                if (WindowState == WindowState.Maximized)
                {
                    SystemCommands.RestoreWindow(this);
                }
                else
                {
                    SystemCommands.MaximizeWindow(this);
                }
            }

            return IntPtr.Zero;
        }

        if (msg == WmNcMouseLeave || msg == WmCancelMode || msg == WmCaptureChanged)
        {
            _snapButtonPressed = false;
            SetSnapButtonVisualState(SnapButtonVisualState.Normal);
        }

        if (msg == WmGetMinMaxInfo && ConstrainMaximizedBounds(hwnd, lParam))
        {
            handled = true;
        }

        return IntPtr.Zero;
    }

    private void SetSnapButtonVisualState(SnapButtonVisualState state)
    {
        SetSnapButtonBackground(_maximizeButton, state);
        SetSnapButtonBackground(_restoreButton, state);
    }

    private void SetSnapButtonBackground(Button? button, SnapButtonVisualState state)
    {
        if (button is null)
        {
            return;
        }

        switch (state)
        {
            case SnapButtonVisualState.Hot:
                button.SetResourceReference(
                    Control.BackgroundProperty,
                    FrameAppearance == RibbonWindowFrameAppearance.Office2007Aero
                        ? AeroCaptionHoverKey
                        : ThemeManager.CaptionHoverKey);
                break;

            case SnapButtonVisualState.Pressed:
                button.SetResourceReference(
                    Control.BackgroundProperty,
                    FrameAppearance == RibbonWindowFrameAppearance.Office2007Aero
                        ? AeroCaptionPressedKey
                        : ThemeManager.CaptionPressedKey);
                break;

            default:
                button.ClearValue(Control.BackgroundProperty);
                break;
        }
    }

    private static void TrackNonClientMouseLeave(IntPtr hwnd)
    {
        var tracking = new NativeTrackMouseEvent
        {
            cbSize = Marshal.SizeOf<NativeTrackMouseEvent>(),
            dwFlags = TmeLeave | TmeNonClient,
            hwndTrack = hwnd,
        };

        _ = TrackMouseEvent(ref tracking);
    }

    private bool IsOverMaximizeOrRestoreButton(IntPtr lParam)
    {
        FrameworkElement? button = _maximizeButton?.IsVisible == true
            ? _maximizeButton
            : _restoreButton?.IsVisible == true
                ? _restoreButton
                : null;

        if (button is null
            || !button.IsEnabled
            || button.ActualWidth <= 0d
            || button.ActualHeight <= 0d)
        {
            return false;
        }

        try
        {
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            var clientOrigin = new NativePoint();
            if (hwnd == IntPtr.Zero || !ClientToScreen(hwnd, ref clientOrigin))
            {
                return false;
            }

            GeneralTransform transform = button.TransformToAncestor(this);
            Point firstCorner = transform.Transform(default);
            Point secondCorner = transform.Transform(new Point(button.ActualWidth, button.ActualHeight));
            Rect screenBounds = CalculateScreenBounds(
                new Point(clientOrigin.X, clientOrigin.Y),
                firstCorner,
                secondCorner,
                VisualTreeHelper.GetDpi(this));

            return IsScreenPointWithinBounds(
                ScreenPointFromLParam(lParam),
                screenBounds.TopLeft,
                screenBounds.BottomRight);
        }
        catch (InvalidOperationException)
        {
            // The template can be replaced while native messages are still in flight. If the
            // old part is no longer connected, let WindowChrome perform its normal hit test.
            return false;
        }
    }

    /// <summary>
    /// Converts opposite corners measured in client-space DIPs into native screen-pixel bounds.
    /// This avoids <c>PointToScreen</c>, whose cached screen transform can
    /// briefly retain the previous monitor's scale during a per-monitor DPI transition.
    /// </summary>
    internal static Rect CalculateScreenBounds(
        Point clientOriginPixels,
        Point firstCornerDips,
        Point secondCornerDips,
        DpiScale dpi)
    {
        double sx = dpi.DpiScaleX <= 0d ? 1d : dpi.DpiScaleX;
        double sy = dpi.DpiScaleY <= 0d ? 1d : dpi.DpiScaleY;

        var first = new Point(
            clientOriginPixels.X + (firstCornerDips.X * sx),
            clientOriginPixels.Y + (firstCornerDips.Y * sy));
        var second = new Point(
            clientOriginPixels.X + (secondCornerDips.X * sx),
            clientOriginPixels.Y + (secondCornerDips.Y * sy));

        return new Rect(first, second);
    }

    /// <summary>Decodes the signed screen coordinates packed into a mouse-message LPARAM.</summary>
    internal static Point ScreenPointFromLParam(IntPtr lParam)
    {
        long packed = lParam.ToInt64();
        return new Point(
            unchecked((short)(packed & 0xffff)),
            unchecked((short)((packed >> 16) & 0xffff)));
    }

    /// <summary>
    /// Returns whether a screen point falls inside two opposite corners, allowing mirrored or
    /// otherwise transformed templates to report those corners in either order.
    /// </summary>
    internal static bool IsScreenPointWithinBounds(Point point, Point firstCorner, Point secondCorner)
    {
        double left = Math.Min(firstCorner.X, secondCorner.X);
        double top = Math.Min(firstCorner.Y, secondCorner.Y);
        double right = Math.Max(firstCorner.X, secondCorner.X);
        double bottom = Math.Max(firstCorner.Y, secondCorner.Y);

        // Match Win32 PtInRect: left/top are inclusive; right/bottom are exclusive.
        return point.X >= left && point.X < right && point.Y >= top && point.Y < bottom;
    }

    private static bool ConstrainMaximizedBounds(IntPtr hwnd, IntPtr lParam)
    {
        IntPtr monitor = MonitorFromWindow(hwnd, MonitorDefaultToNearest);
        if (monitor == IntPtr.Zero)
        {
            return false;
        }

        var monitorInfo = new NativeMonitorInfo { cbSize = Marshal.SizeOf<NativeMonitorInfo>() };
        if (!GetMonitorInfo(monitor, ref monitorInfo))
        {
            return false;
        }

        var mmi = Marshal.PtrToStructure<NativeMinMaxInfo>(lParam);
        NativeRect work = monitorInfo.rcWork;   // desktop minus taskbar, in device pixels
        NativeRect area = monitorInfo.rcMonitor; // full monitor, in device pixels

        int width = work.Right - work.Left;
        int height = work.Bottom - work.Top;

        mmi.ptMaxPosition.X = work.Left - area.Left;
        mmi.ptMaxPosition.Y = work.Top - area.Top;
        mmi.ptMaxSize.X = width;
        mmi.ptMaxSize.Y = height;
        mmi.ptMaxTrackSize.X = width;
        mmi.ptMaxTrackSize.Y = height;

        Marshal.StructureToPtr(mmi, lParam, true);
        return true;
    }

    private const int MonitorDefaultToNearest = 0x00000002;
    private const int TmeLeave = 0x00000002;
    private const int TmeNonClient = 0x00000010;

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, int flags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref NativeMonitorInfo lpmi);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr hwnd, out NativeRect lpRect);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ClientToScreen(IntPtr hwnd, ref NativePoint lpPoint);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool TrackMouseEvent(ref NativeTrackMouseEvent lpEventTrack);

    private enum SnapButtonVisualState
    {
        Normal,
        Hot,
        Pressed,
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeTrackMouseEvent
    {
        public int cbSize;
        public int dwFlags;
        public IntPtr hwndTrack;
        public int dwHoverTime;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeMinMaxInfo
    {
        public NativePoint ptReserved;
        public NativePoint ptMaxSize;
        public NativePoint ptMaxPosition;
        public NativePoint ptMinTrackSize;
        public NativePoint ptMaxTrackSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeMonitorInfo
    {
        public int cbSize;
        public NativeRect rcMonitor;
        public NativeRect rcWork;
        public int dwFlags;
    }
}
