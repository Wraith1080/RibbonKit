using System.Windows;
using System.Windows.Controls;
using RibbonKit.Theming;

namespace RibbonKit.Controls;

/// <summary>Optionally paints a blurred host snapshot behind a ribbon menu or popup.</summary>
/// <remarks>
/// The host supplies a loaded WPF capture source on the control's dispatcher. Capture samples
/// that visual only, not the desktop or a native Acrylic backdrop. The shared templates remain
/// usable without this integration. Call <see cref="Refresh"/> after document paint changes and
/// <see cref="Dispose"/> when the registration is no longer needed. Unload releases capture
/// resources; a subsequent load reattaches while enabled. Custom templates without the shared
/// surface parts retain their ordinary paint.
/// A <see cref="Ribbon"/> registration follows its scoped Glass overlay and owns only group,
/// QAT-overflow and Simplified-overflow registrations. Ordinary command menus, combo lists and
/// galleries retain their material. Legacy Aero and Crystal palettes keep their existing paths.
/// </remarks>
public sealed class CapturedBackdrop : IDisposable
{
    private readonly Control _control;
    private readonly FrameworkElement _source;
    private CapturedMenuBackdrop? _menu;
    private CapturedPopupBackdrop? _popup;
    private Border? _frame;
    private bool _enabled;
    private bool _disposed;
    private readonly bool _continuation;
    private readonly Dictionary<Control, CapturedBackdrop> _continuations = new();

    /// <summary>Registers a menu, dropdown/split button, group, overflow toolbar, or a ribbon's Glass flyouts.</summary>
    /// <param name="control">The control whose shared surface should receive captured paint.</param>
    /// <param name="source">The host visual to sample; use an ancestor for an application menu.</param>
    /// <exception cref="ArgumentException">The control is unsupported or uses another dispatcher.</exception>
    public CapturedBackdrop(Control control, FrameworkElement source) : this(control, source, control is RibbonQuickAccessToolBar) { }

    private CapturedBackdrop(Control control, FrameworkElement source, bool continuation)
    {
        ArgumentNullException.ThrowIfNull(control);
        ArgumentNullException.ThrowIfNull(source);
        if (control is not RibbonApplicationMenu and not RibbonDropDownButton and not RibbonGroup
            and not RibbonQuickAccessToolBar and not Ribbon)
            throw new ArgumentException("Use a ribbon, application menu, dropdown/split button, group, or overflow toolbar.", nameof(control));
        if (control.Dispatcher != source.Dispatcher)
            throw new ArgumentException("Capture source and control must share a dispatcher.", nameof(source));
        control.VerifyAccess();
        _control = control;
        _source = source;
        _continuation = continuation;
        control.Loaded += OnLoaded;
        control.Unloaded += OnUnloaded;
        source.Loaded += OnLoaded;
        source.Unloaded += OnUnloaded;
        ThemeManager.Changed += OnThemeChanged;
    }

    /// <summary>Enables or disables capture; repeated enable also refreshes the current paint.</summary>
    public void Apply(bool enabled)
    {
        _control.VerifyAccess();
        ObjectDisposedException.ThrowIf(_disposed, this);
        _enabled = enabled;
        _control.LayoutUpdated -= OnLayoutUpdated;
        if (enabled) _control.LayoutUpdated += OnLayoutUpdated;
        else Release();
        Refresh();
    }

    /// <summary>Coalesces a refresh after host content, palette or template changes.</summary>
    public void Refresh()
    {
        _control.VerifyAccess();
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!_enabled || !_source.IsLoaded || !_control.IsLoaded) return;
        Attach();
        foreach (var capture in _continuations.Values.ToArray()) capture.Refresh();
        _menu?.Refresh();
        _popup?.Refresh();
    }

    private void Attach()
    {
        if (_disposed || !_enabled) return;
        if (_control is Ribbon ribbon)
        {
            // Hosts opt in once with their own capture source. Discover only
            // ribbon continuations, including the detached title-bar QAT.
            bool glass = ribbon.TryFindResource("RibbonKit.Metrics.Ribbon.SupportsGlassFlyouts") is true
                && ribbon.TryFindResource("RibbonKit.Metrics.Ribbon.FlyoutGlassOpacity") is double opacity && opacity < 1;
            var targets = new HashSet<Control>();
            if (glass)
            {
                foreach (var tab in ribbon.Tabs)
                {
                    foreach (var group in tab.Groups) targets.Add(group);
                    if (tab.Content is RibbonGroupsHost { SimplifiedToolBar: { } simplified }) targets.Add(simplified);
                }
                if (ribbon.ActiveQuickAccessToolBar is { } qat) targets.Add(qat);
            }
            foreach (var obsolete in _continuations.Keys.Except(targets).ToArray())
            { _continuations[obsolete].Dispose(); _continuations.Remove(obsolete); }
            foreach (var target in targets)
                if (!_continuations.ContainsKey(target))
                {
                    var capture = new CapturedBackdrop(target, _source, true);
                    _continuations.Add(target, capture); capture.Apply(true);
                }
            return;
        }
        _control.ApplyTemplate();
        if (_control is RibbonApplicationMenu)
        {
            var template = _control.Template;
            var frame = template?.FindName("Frame", _control) as Border;
            if (ReferenceEquals(frame, _frame)) return;
            Release();
            if (frame == null || template?.FindName("PART_Frame", _control) is not Border host ||
                template.FindName("MenuSurface", _control) is not Grid surface) return;
            _frame = frame;
            _menu = new CapturedMenuBackdrop(_source, host, frame, surface);
        }
        else
        {
            if (_popup == null)
            {
                _popup = new CapturedPopupBackdrop(_source, _control,
                    _control is RibbonQuickAccessToolBar ? "PART_OverflowPopupHost"
                        : _control is RibbonGroup ? "PART_PopupHost" : "PART_MenuHost",
                    _control is RibbonQuickAccessToolBar ? "PART_OverflowPopup" : "PART_Popup", _continuation);
                _popup.Apply(true);
            }
            else _popup.Attach();
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e) => Refresh();
    private void OnUnloaded(object sender, RoutedEventArgs e) => Release();
    private void OnThemeChanged(object? sender, EventArgs e) => Refresh();
    private void OnLayoutUpdated(object? sender, EventArgs e)
    {
        if (_source.IsLoaded && _control.IsLoaded) Attach();
    }

    private void Release()
    {
        foreach (var capture in _continuations.Values.ToArray()) capture.Dispose();
        _continuations.Clear();
        _menu?.Remove();
        _popup?.Remove();
        _menu = null;
        _popup = null;
        _frame = null;
    }

    /// <summary>Releases paint, queued captures and all registration event handlers.</summary>
    public void Dispose()
    {
        _control.VerifyAccess();
        if (_disposed) return;
        _disposed = true;
        Release();
        _control.LayoutUpdated -= OnLayoutUpdated;
        _control.Loaded -= OnLoaded;
        _control.Unloaded -= OnUnloaded;
        _source.Loaded -= OnLoaded;
        _source.Unloaded -= OnUnloaded;
        ThemeManager.Changed -= OnThemeChanged;
    }
}
