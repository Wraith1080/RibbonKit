using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using RibbonKit.Controls;

namespace RibbonKit.Showcase;

/// <summary>Adds a non-interactive rim to the shared utility templates in this preview.</summary>
internal static class CrystalUtilityChrome
{
    private static readonly DependencyProperty EnabledProperty = DependencyProperty.RegisterAttached(
        "Enabled", typeof(bool), typeof(CrystalUtilityChrome),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits, OnEnabledChanged));
    private static readonly DependencyProperty OverlayProperty = DependencyProperty.RegisterAttached(
        "Overlay", typeof(OverlayState), typeof(CrystalUtilityChrome));
    private static readonly DependencyProperty ScrollSurfaceProperty = DependencyProperty.RegisterAttached(
        "ScrollSurface", typeof(Brush), typeof(CrystalUtilityChrome));

    public static void Apply(FrameworkElement root, bool enabled) => root.SetValue(EnabledProperty, enabled);

    private static void OnEnabledChanged(DependencyObject target, DependencyPropertyChangedEventArgs args)
    {
        if (target is not Border chrome) return;
        if (chrome.IsLoaded) Update(chrome);
        else
        {
            chrome.Loaded -= OnLoaded;
            chrome.Loaded += OnLoaded;
        }
    }

    private static void OnLoaded(object sender, RoutedEventArgs args)
    {
        var chrome = (Border)sender;
        chrome.Loaded -= OnLoaded;
        Update(chrome);
    }

    private static void Update(Border chrome)
    {
        if (!(bool)chrome.GetValue(EnabledProperty))
        {
            if (chrome.GetValue(OverlayProperty) is OverlayState state)
            {
                if (state.IsScrollArrow) chrome.ClearValue(Border.BackgroundProperty);
                if (state.IsBodyScrollArrow)
                {
                    chrome.ClearValue(Border.CornerRadiusProperty);
                    ((ButtonBase)chrome.TemplatedParent).ClearValue(FrameworkElement.WidthProperty);
                }
                chrome.Child = null;
                if (state.Content != null) state.Wrapper.Children.Remove(state.Content);
                chrome.Child = state.Content;
                chrome.ClearValue(OverlayProperty);
            }
            return;
        }
        if (chrome.GetValue(OverlayProperty) != null || chrome.Name != "Chrome" ||
            chrome.TemplatedParent is not ButtonBase button || !IsUtility(button)) return;

        var content = chrome.Child;
        bool isScrollArrow = button is RepeatButton;
        bool isBodyScrollArrow = isScrollArrow && button.TemplatedParent is RibbonTabControl tabs &&
            tabs.Template.FindName("PART_ContentScroll", tabs) is FrameworkElement bodyScroll &&
            VisualTreeHelper.GetParent(button) == VisualTreeHelper.GetParent(bodyScroll);
        if (isBodyScrollArrow)
        {
            chrome.SetResourceReference(Border.CornerRadiusProperty, "Crystal.Metrics.BodyScrollCornerRadius");
            button.SetResourceReference(FrameworkElement.WidthProperty, "Crystal.Metrics.BodyScrollWidth");
        }
        var wrapper = new Grid();
        var thickness = chrome.BorderThickness;
        var rim = new Border
        {
            Name = "CrystalUtilityRim", BorderThickness = new Thickness(1),
            Margin = new Thickness(-thickness.Left, -thickness.Top, -thickness.Right, -thickness.Bottom),
            IsHitTestVisible = false, SnapsToDevicePixels = true,
        };
        rim.SetBinding(Border.CornerRadiusProperty, new Binding(nameof(chrome.CornerRadius)) { Source = chrome });
        var style = new Style(typeof(Border));
        style.Setters.Add(new Setter(UIElement.OpacityProperty, 0d));
        style.Setters.Add(new Setter(Border.BorderBrushProperty,
            new DynamicResourceExtension("RibbonKit.Brushes.Control.HoverBorder")));
        if (isScrollArrow)
        {
            // Keep the shared utility palette intact; only arrows exchange idle/hover surfaces.
            style.Setters.Add(new Setter(ScrollSurfaceProperty,
                new DynamicResourceExtension("RibbonKit.Brushes.TabStrip.ControlHoverBackground")));
            chrome.SetBinding(Border.BackgroundProperty,
                new Binding { Source = rim, Path = new PropertyPath(ScrollSurfaceProperty) });
        }
        AddState(nameof(button.IsMouseOver), surfaceKey: "RibbonKit.Brushes.TabStrip.ScrollButtonBackground");
        if (button is ToggleButton) AddState(nameof(ToggleButton.IsChecked));
        AddState(nameof(button.IsPressed), "RibbonKit.Brushes.Control.PressedBorder",
            "RibbonKit.Brushes.TabStrip.ControlPressedBackground");
        rim.Style = style;
        chrome.Child = null;
        if (content != null) wrapper.Children.Add(content);
        wrapper.Children.Add(rim);
        chrome.SetValue(OverlayProperty, new OverlayState(wrapper, content, isScrollArrow, isBodyScrollArrow));
        chrome.Child = wrapper;

        void AddState(string property, string? borderKey = null, string? surfaceKey = null)
        {
            var trigger = new DataTrigger { Binding = new Binding(property) { Source = button }, Value = true };
            trigger.Setters.Add(new Setter(UIElement.OpacityProperty, 1d));
            if (borderKey != null)
                trigger.Setters.Add(new Setter(Border.BorderBrushProperty, new DynamicResourceExtension(borderKey)));
            if (isScrollArrow && surfaceKey != null)
                trigger.Setters.Add(new Setter(ScrollSurfaceProperty, new DynamicResourceExtension(surfaceKey)));
            style.Triggers.Add(trigger);
        }
    }

    private static bool IsUtility(ButtonBase button) =>
        button.Name is "MinimizeToggle" or "PART_ModalClose" or "PART_OverflowButton" ||
        button is RepeatButton { TemplatedParent: RibbonTabControl } ||
        button.Style is { } style && (
            style == button.TryFindResource("RibbonKit.ScrollLeftButton") ||
            style == button.TryFindResource("RibbonKit.ScrollRightButton") ||
            style == button.TryFindResource("RibbonKit.MergedCaptionButton"));

    private sealed record OverlayState(Grid Wrapper, UIElement? Content, bool IsScrollArrow, bool IsBodyScrollArrow);
}
