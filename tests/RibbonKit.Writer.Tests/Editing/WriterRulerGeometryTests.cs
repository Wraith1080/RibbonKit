using RibbonKit.Writer.Editing;
using RibbonKit.Writer.Models;
using Xunit;

namespace RibbonKit.Writer.Tests.Editing;

public sealed class WriterRulerGeometryTests
{
    [Fact]
    public void NormalLetterRulerUsesPageOriginZoomAndPhysicalMarginZones()
    {
        var settings = DocumentPageSettings.Letter();
        var layout = WriterRulerGeometry.Create(settings, 125, 48,
            new WriterRulerIndentation(12, 6, 0, 18));

        Assert.Equal(settings.WidthDip * 1.25, layout.PageWidthDip, 6);
        Assert.Equal(48 + settings.Margins.LeftDip * 1.25, layout.ContentStartDip, 6);
        Assert.Equal(48 + (settings.WidthDip - settings.Margins.RightDip) * 1.25,
            layout.ContentEndDip, 6);
        Assert.Equal(2, layout.MarginZones.Count);
        Assert.Equal(settings.Margins.LeftDip * 1.25, layout.MarginZones[0].WidthDip, 6);
        Assert.Equal(settings.Margins.RightDip * 1.25, layout.MarginZones[1].WidthDip, 6);
        Assert.Equal(9, layout.Ticks.Count(tick => tick.IsMajor));
        Assert.Equal(layout.ContentStartDip + 18 * 1.25,
            layout.GetMarkerPosition(WriterRulerIndentMarker.FirstLine)!.Value, 6);
        Assert.Equal(layout.ContentEndDip - 18 * 1.25,
            layout.GetMarkerPosition(WriterRulerIndentMarker.Right)!.Value, 6);
    }

    [Fact]
    public void NegativeTextIndentPlacesFirstLineBeforeTheBodyMarker()
    {
        var settings = DocumentPageSettings.Letter();
        var layout = WriterRulerGeometry.Create(settings, 100, 48,
            new WriterRulerIndentation(24, -10, 10, 18));

        Assert.Equal(-10, layout.Indentation.TextIndentDip, 6);
        Assert.Equal(layout.ContentStartDip + 14,
            layout.GetMarkerPosition(WriterRulerIndentMarker.FirstLine)!.Value, 6);
        Assert.Equal(layout.ContentStartDip + 24,
            layout.GetMarkerPosition(WriterRulerIndentMarker.Hanging)!.Value, 6);
        Assert.Equal(layout.GetMarkerPosition(WriterRulerIndentMarker.Hanging),
            layout.GetMarkerPosition(WriterRulerIndentMarker.Left));
    }

    [Fact]
    public void LandscapeCustomPageKeepsPhysicalOriginWhenHorizontalScrollMovesIt()
    {
        var settings = DocumentPageSettings.CreateCustom(500, 900,
            DocumentPageOrientation.Landscape,
            new DocumentPageMargins(17.25, 29.5, 41.75, 53.25));
        var first = WriterRulerGeometry.Create(settings, 150, 220);
        var scrolled = WriterRulerGeometry.Create(settings, 150, -37);

        Assert.Equal(900 * 1.5, first.PageWidthDip, 6);
        Assert.Equal(257, first.ContentStartDip - scrolled.ContentStartDip, 6);
        var renderedContent = first.ContentStartDip + 93 * first.Scale;
        Assert.Equal(93, WriterRulerGeometry.ToLogicalContentDip(first, renderedContent), 6);
        Assert.Equal(0, WriterRulerGeometry.LeftMarginFromRenderedX(first,
            first.PageOriginDip, settings.Margins.RightDip), 6);
        Assert.Equal(0, WriterRulerGeometry.RightMarginFromRenderedX(first,
            first.PageEndDip, settings.Margins.LeftDip), 6);
    }

    [Theory]
    [InlineData(100)]
    [InlineData(125)]
    [InlineData(150)]
    [InlineData(175)]
    [InlineData(200)]
    public void DpiScaleRoundingNeverMovesTicksOutsideThePhysicalPage(double zoom)
    {
        var settings = DocumentPageSettings.A4(DocumentPageOrientation.Landscape,
            new DocumentPageMargins(31.2, 24.8, 27.4, 18.6));
        var layout = WriterRulerGeometry.Create(settings, zoom, 13.37);

        Assert.Equal(layout.PageOriginDip, layout.Ticks[0].PositionDip, 6);
        Assert.Equal(layout.PageEndDip, layout.Ticks[^1].PositionDip, 6);
        Assert.All(layout.Ticks, tick => Assert.InRange(tick.PositionDip,
            layout.PageOriginDip - 0.001, layout.PageEndDip + 0.001));
    }
}
