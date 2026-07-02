using System.Windows;
using System.Windows.Markup;

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
