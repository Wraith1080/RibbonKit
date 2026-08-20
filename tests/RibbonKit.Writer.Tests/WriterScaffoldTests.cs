using RibbonKit.Controls;
using RibbonKit.Writer;
using Xunit;

namespace RibbonKit.Writer.Tests;

public sealed class WriterScaffoldTests
{
    [Fact]
    public void MainWindowDerivesFromRibbonWindow()
    {
        Assert.True(typeof(RibbonWindow).IsAssignableFrom(typeof(MainWindow)));
    }
}
