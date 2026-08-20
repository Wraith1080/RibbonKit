using RibbonKit.Writer.Editing;
using Xunit;

namespace RibbonKit.Writer.Tests.Editing;

public sealed class WriterZoomModelTests
{
    [Fact]
    public void DefaultBoundsAndStepClampAtEndpoints()
    {
        var zoom = new WriterZoomModel();
        Assert.Equal(25, zoom.Minimum);
        Assert.Equal(100, zoom.Default);
        Assert.Equal(400, zoom.Maximum);
        Assert.Equal(10, zoom.Step);
        Assert.Equal(100, zoom.Value);

        Assert.True(zoom.TrySet(0));
        Assert.Equal(25, zoom.Value);
        Assert.True(zoom.Increase());
        Assert.Equal(35, zoom.Value);
        Assert.True(zoom.TrySet(500));
        Assert.Equal(400, zoom.Value);
        Assert.False(zoom.Increase());
        Assert.True(zoom.Decrease());
        Assert.Equal(390, zoom.Value);
    }

    [Fact]
    public void NonFiniteValuesAreRejectedWithoutEvents()
    {
        var zoom = new WriterZoomModel(50, 100, 150, 25);
        var events = 0;
        zoom.PropertyChanged += (_, args) =>
        {
            Assert.Equal(nameof(WriterZoomModel.Value), args.PropertyName);
            events++;
        };

        Assert.False(zoom.TrySet(double.NaN));
        Assert.False(zoom.TrySet(double.PositiveInfinity));
        Assert.False(zoom.TrySet(double.NegativeInfinity));
        zoom.Value = double.NaN;
        Assert.Equal(100, zoom.Value);
        Assert.Equal(0, events);

        Assert.False(zoom.TrySet(100));
        Assert.True(zoom.TrySet(125));
        Assert.Equal(1, events);
    }

    [Fact]
    public void ResetAndConstructorValidationAreDeterministic()
    {
        var zoom = new WriterZoomModel(25, 50, 100, 15);
        zoom.TrySet(90);
        Assert.True(zoom.Reset());
        Assert.Equal(50, zoom.Value);
        Assert.False(zoom.Reset());

        Assert.Throws<ArgumentOutOfRangeException>(() => new WriterZoomModel(0, 50, 100, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => new WriterZoomModel(100, 50, 25, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => new WriterZoomModel(25, 50, 100, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new WriterZoomModel(double.NaN, 50, 100, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => new WriterZoomModel(25, 101, 100, 10));
    }

}
