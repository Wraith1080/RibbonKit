using System.Windows;

namespace RibbonKit.Writer;

public partial class MainWindow
{
    private const string WriterOrbGlyphTemplateKey = "Writer.Templates.ApplicationOrbGlyph";

    private void InitializeWriterIdentity() =>
        MainRibbon.ApplicationOrbGlyphTemplate = (DataTemplate)FindResource(WriterOrbGlyphTemplateKey);

    internal bool HasWriterOrbTemplate() =>
        ReferenceEquals(MainRibbon.ApplicationOrbGlyphTemplate, FindResource(WriterOrbGlyphTemplateKey));
}
