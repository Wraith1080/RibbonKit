using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using RibbonKit.Controls;

namespace RibbonKit.Showcase;

/// <summary>Applies Crystal materials to the shared application-menu template.</summary>
internal sealed class CrystalApplicationMenuPresentation
{
    private readonly RibbonApplicationMenu _menu;
    private readonly FrameworkElement _owner;
    private bool _enabled;
    private Grid? _innerContent;
    private CrystalMenuShadow? _shadow;
    private static readonly (string Target, string Source)[] Brushes =
    {
        ("Foreground", "RibbonKit.Brushes.Text.Primary"),
        ("SecondaryForeground", "RibbonKit.Brushes.Text.Secondary"),
        ("HeadingForeground", "RibbonKit.Brushes.Accent"),
        ("FrameBorder", "RibbonKit.Brushes.Control.HoverBorder"),
        ("FrameRim", "RibbonKit.Brushes.Control.InnerGlow"),
        ("FrameBand", "Crystal.Brushes.FrostedFrame"),
        ("TopBandBackground", "Crystal.Brushes.ApplicationMenuClearBand"),
        ("FooterBackground", "Crystal.Brushes.ApplicationMenuClearBand"),
        ("NavBackground", "RibbonKit.Brushes.Tab.SelectedBackground"),
        ("PaneBackground", "Crystal.Brushes.ScreenTipSurface"),
        ("PaneSurface", "Crystal.Brushes.ApplicationMenuClearBand"),
        ("PaneBorder", "RibbonKit.Brushes.Control.HoverBorder"),
        ("HeaderBackground", "RibbonKit.Brushes.Tab.HoverBackground"),
        ("Separator", "RibbonKit.Brushes.Group.Separator"),
        ("SeparatorHighlight", "RibbonKit.Brushes.Control.InnerGlow"),
        ("ButtonBackground", "RibbonKit.Brushes.Control.CheckedBackground"),
        ("ButtonBorder", "RibbonKit.Brushes.Control.HoverBorder"),
    };
    private static readonly (string Name, object Value)[] Metrics =
    {
        ("CornerRadius", new CornerRadius(14)), ("InnerCornerRadius", new CornerRadius(13)),
        ("TopBandCornerRadius", new CornerRadius(12, 12, 0, 0)),
        ("FooterCornerRadius", new CornerRadius(0, 0, 12, 12)),
        ("NavWidth", 160d), ("ItemHeight", 42d), ("TopBandHeight", 8d), ("FooterHeight", 44d),
    };

    public CrystalApplicationMenuPresentation(RibbonApplicationMenu menu, FrameworkElement owner)
    {
        _menu = menu;
        _owner = owner;
        owner.SizeChanged += (_, _) => UpdateWidth();
        menu.Loaded += (_, _) => UpdateInnerFrame();
        foreach (var entry in menu.Items)
            if (entry is RibbonApplicationMenuItem item)
            {
                item.Loaded += (_, _) => UpdateRow(item);
                item.SizeChanged += (_, _) => UpdateRow(item);
            }
    }

    public void Apply(bool enabled)
    {
        _enabled = enabled;
        foreach (var (target, source) in Brushes)
        {
            var key = "RibbonKit.Brushes.ApplicationMenu." + target;
            if (enabled) _menu.Resources[key] = _owner.FindResource(source);
            else _menu.Resources.Remove(key);
        }
        foreach (var (name, value) in Metrics)
        {
            var key = "RibbonKit.Metrics.ApplicationMenu" + name;
            if (enabled) _menu.Resources[key] = value;
            else _menu.Resources.Remove(key);
        }
        const string shadow = "RibbonKit.Effects.ApplicationMenuShadow";
        if (enabled) _menu.Resources[shadow] = _owner.FindResource("Crystal.Effects.ApplicationMenuShadow");
        else _menu.Resources.Remove(shadow);
        UpdateWidth();
        // Install the shadow layer before the first opening animation chooses its content surface.
        if (enabled) _menu.ApplyTemplate();
        UpdateInnerFrame();
        foreach (var entry in _menu.Items)
        {
            if (entry is RibbonApplicationMenuItem item) UpdateRow(item);
            else if (entry is RibbonApplicationMenuSeparator separator)
            {
                if (enabled) separator.Margin = new Thickness(12, 4, 12, 4);
                else separator.ClearValue(FrameworkElement.MarginProperty);
            }
        }
    }

    private void UpdateWidth()
    {
        const string key = "RibbonKit.Metrics.ApplicationMenuPaneWidth";
        if (_enabled) _menu.Resources[key] = Math.Clamp(_owner.ActualWidth - 192, 180, 300);
        else _menu.Resources.Remove(key);
    }

    private void UpdateInnerFrame()
    {
        if (_enabled && _shadow == null &&
            _menu.Template?.FindName("PART_Frame", _menu) is Border host &&
            host.Child is Border frame)
            _shadow = new CrystalMenuShadow(host, frame, _owner);
        else if (!_enabled && _shadow != null)
        {
            _shadow.Remove();
            _shadow = null;
        }
        _shadow?.UpdateClip();
        _shadow?.RefreshBackdrop();
        if (_menu.Template?.FindName("PART_Pane", _menu) is not Border pane ||
            VisualTreeHelper.GetParent(pane) is not Grid content ||
            VisualTreeHelper.GetParent(content) is not Border outline ||
            VisualTreeHelper.GetParent(outline) is not Border rim ||
            _menu.Template.FindName("ActivePage", _menu) is not Border activePage) return;

        if (!ReferenceEquals(_innerContent, content))
        {
            if (_innerContent != null) _innerContent.SizeChanged -= OnInnerSizeChanged;
            _innerContent = content;
            content.SizeChanged += OnInnerSizeChanged;
        }
        if (_enabled)
        {
            rim.CornerRadius = new CornerRadius(11);
            outline.CornerRadius = new CornerRadius(10);
            outline.SetResourceReference(Border.BorderBrushProperty, "Crystal.Brushes.ApplicationMenuOutline");
            // One outline surrounds both columns; avoid a second square frame in the right pane.
            activePage.BorderThickness = new Thickness(0);
            activePage.Margin = new Thickness(0);
        }
        else
        {
            rim.ClearValue(Border.CornerRadiusProperty);
            outline.ClearValue(Border.CornerRadiusProperty);
            outline.ClearValue(Border.BorderBrushProperty);
            activePage.ClearValue(Border.BorderThicknessProperty);
            activePage.ClearValue(FrameworkElement.MarginProperty);
        }
        UpdateInnerClip();
    }

    private void OnInnerSizeChanged(object sender, SizeChangedEventArgs e) => UpdateInnerClip();

    private void UpdateInnerClip()
    {
        if (_innerContent == null) return;
        if (_enabled) _innerContent.Clip = new RectangleGeometry(new Rect(_innerContent.RenderSize), 9, 9);
        else _innerContent.ClearValue(UIElement.ClipProperty);
    }

    private void UpdateRow(RibbonApplicationMenuItem item)
    {
        if (item.Template?.FindName("Root", item) is not Grid root) return;
        if (_enabled)
        {
            item.Margin = new Thickness(4, 2, 4, 2);
            // Both split fills share the rounded outer silhouette; the inner split stays square.
            root.Clip = new RectangleGeometry(new Rect(root.RenderSize), 8, 8);
        }
        else
        {
            item.ClearValue(FrameworkElement.MarginProperty);
            root.ClearValue(UIElement.ClipProperty);
        }
    }
}
