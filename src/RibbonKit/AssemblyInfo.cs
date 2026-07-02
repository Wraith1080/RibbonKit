using System.Windows;
using System.Windows.Markup;

[assembly: ThemeInfo(
    // Where theme-specific resource dictionaries are located
    ResourceDictionaryLocation.None,
    // Where the generic resource dictionary is located (Themes/Generic.xaml)
    ResourceDictionaryLocation.SourceAssembly)]

// Consumers can use a single clean namespace: xmlns:rk="urn:ribbonkit"
[assembly: XmlnsDefinition("urn:ribbonkit", "RibbonKit.Controls")]
[assembly: XmlnsPrefix("urn:ribbonkit", "rk")]
