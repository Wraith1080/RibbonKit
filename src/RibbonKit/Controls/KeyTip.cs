using System.Windows;

namespace RibbonKit.Controls;

/// <summary>
/// Provides the <see cref="KeysProperty"/> attached property that assigns a KeyTip
/// (the access-key badge shown when the user presses <c>Alt</c>) to a ribbon element.
/// <code language="xaml">
/// &lt;rk:RibbonButton Header="Bold" rk:KeyTip.Keys="1" /&gt;
/// &lt;rk:RibbonTab Header="Home" rk:KeyTip.Keys="H" /&gt;
/// </code>
/// When a control has no explicit KeyTip, the ribbon derives a unique one from its
/// label (matching Office), so setting this is only needed to pin a specific key.
/// Within one KeyTip level, exact duplicates and prefix collisions cannot both be
/// typed; the first explicit assignment wins and later conflicting assignments fall
/// back to automatic derivation. An explicit key also opts an ordinary visible
/// <see cref="UIElement"/> inside the selected Backstage page or active application-menu
/// pane/footer into that surface's KeyTip level, provided the element supports a native
/// invocation path or an appropriate UI Automation pattern. Disabled targets are never invoked.
/// </summary>
public static class KeyTip
{
    /// <summary>
    /// Identifies the KeyTip.Keys attached property — the one or more characters typed
    /// (after <c>Alt</c>) to activate the element. Case-insensitive; usually one or two
    /// ASCII letters or digits, e.g. <c>"H"</c> or <c>"FN"</c>.
    /// </summary>
    public static readonly DependencyProperty KeysProperty =
        DependencyProperty.RegisterAttached(
            "Keys",
            typeof(string),
            typeof(KeyTip),
            new FrameworkPropertyMetadata(null));

    /// <summary>Sets the <see cref="KeysProperty"/> access key(s) for an element.</summary>
    public static void SetKeys(DependencyObject element, string? value) =>
        element.SetValue(KeysProperty, value);

    /// <summary>Gets the <see cref="KeysProperty"/> access key(s) for an element.</summary>
    public static string? GetKeys(DependencyObject element) =>
        (string?)element.GetValue(KeysProperty);
}
