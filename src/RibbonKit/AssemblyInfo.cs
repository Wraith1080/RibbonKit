using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Markup;

// The unit tests exercise contracts that are deliberately not public API — the menu-borrowing
// protocol between a dropdown and its proxy, the quick access panel's overflow bookkeeping, the
// command-proxy factory. Testing them through the public surface would mean either widening that
// surface or driving real popups, and neither is worth it.
[assembly: InternalsVisibleTo("RibbonKit.Tests")]

[assembly: ThemeInfo(
    // Where theme-specific resource dictionaries are located
    ResourceDictionaryLocation.None,
    // Where the generic resource dictionary is located (Themes/Generic.xaml)
    ResourceDictionaryLocation.SourceAssembly)]

// Consumers use a single clean namespace: xmlns:rk="urn:ribbonkit"
[assembly: XmlnsDefinition("urn:ribbonkit", "RibbonKit")]
[assembly: XmlnsDefinition("urn:ribbonkit", "RibbonKit.Controls")]
[assembly: XmlnsDefinition("urn:ribbonkit", "RibbonKit.Layout")]
[assembly: XmlnsDefinition("urn:ribbonkit", "RibbonKit.Theming")]
[assembly: XmlnsPrefix("urn:ribbonkit", "rk")]
