using System.Globalization;
using System.Windows.Media;
using RibbonKit.Controls;
using Xunit;

namespace RibbonKit.Tests;

public class BackstageCaptionForegroundTests
{
    [Fact]
    public void Caption_blend_preserves_mutable_host_brushes_and_endpoint_identity() => Sta.Run(() =>
    {
        var normal = new SolidColorBrush(Colors.White);
        var page = new LinearGradientBrush(Colors.Black, Colors.Gray, 0);
        var converter = new BackstageCaptionForegroundConverter();
        var blended = Assert.IsType<DrawingBrush>(converter.Convert(
            new object[] { normal, page, .5d }, typeof(Brush), null!, CultureInfo.InvariantCulture));
        Assert.False(normal.IsFrozen);
        Assert.False(page.IsFrozen);
        normal.Color = Colors.Blue;
        page.GradientStops[0].Color = Colors.Red;
        var drawing = Assert.IsType<DrawingGroup>(blended.Drawing);
        Assert.Same(normal, Assert.IsType<GeometryDrawing>(drawing.Children[0]).Brush);
        var overlay = Assert.IsType<DrawingGroup>(drawing.Children[1]);
        Assert.Same(page, Assert.IsType<GeometryDrawing>(overlay.Children[0]).Brush);
        Assert.Same(normal, converter.Convert(new object[] { normal, page, 0d }, typeof(Brush), null!, CultureInfo.InvariantCulture));
        Assert.Same(page, converter.Convert(new object[] { normal, page, 1d }, typeof(Brush), null!, CultureInfo.InvariantCulture));
    });
}
