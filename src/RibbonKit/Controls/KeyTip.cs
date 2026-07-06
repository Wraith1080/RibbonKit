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
/// </summary>
public static class KeyTip
{
    /// <summary>
    /// Identifies the KeyTip.Keys attached property — the one or more characters typed
    /// (after <c>Alt</c>) to activate the element. Case-insensitive; usually one or two
    /// characters, e.g. <c>"H"</c> or <c>"FN"</c>.
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
