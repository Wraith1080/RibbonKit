using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using RibbonKit.Animation;
using RibbonKit.Controls;
using Xunit;

namespace RibbonKit.Tests;

/// <summary>Realized-layout contracts for the live RTL window and Backstage path.</summary>
public class RtlBackstageWindowTests
{
    [Fact]
    public void Title_shift_converts_physical_window_delta_through_rtl_logical_host() =>
        Sta.Run(() =>
        {
            var physicalFrame = new Grid
            {
                Width = 300,
                Height = 40,
                FlowDirection = FlowDirection.LeftToRight,
            };
            var logicalHost = new Grid { FlowDirection = FlowDirection.RightToLeft };
            var title = new Border
            {
                Width = 50,
                Height = 20,
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            physicalFrame.Children.Add(logicalHost);
            logicalHost.Children.Add(title);

            var size = new Size(300, 40);
            physicalFrame.Measure(size);
            physicalFrame.Arrange(new Rect(size));
            physicalFrame.UpdateLayout();

            double localDelta = RibbonWindow.ConvertAncestorDeltaToLocalX(
                title,
                physicalFrame,
                ancestorDelta: 40d);
            Assert.Equal(-40d, localDelta, precision: 6);

            double before = title.TransformToAncestor(physicalFrame).Transform(default).X;
            title.RenderTransform = new TranslateTransform(localDelta, 0d);
            physicalFrame.Arrange(new Rect(size));
            physicalFrame.UpdateLayout();
            double after = title.TransformToAncestor(physicalFrame).Transform(default).X;
            Assert.Equal(40d, after - before, precision: 6);
        });

    [Fact]
    public void Backstage_adorner_inherits_live_flow_from_its_ribbon() => Sta.Run(() =>
    {
        var ribbon = new Ribbon { FlowDirection = FlowDirection.RightToLeft };
        var adorned = new Border();
        var backstage = new Backstage();
        var decorator = new AdornerDecorator
        {
            Width = 300,
            Height = 200,
            Child = adorned,
        };
        var size = new Size(300, 200);
        decorator.Measure(size);
        decorator.Arrange(new Rect(size));
        decorator.UpdateLayout();

        var adorner = new BackstageAdorner(adorned, backstage, ribbon);
        AdornerLayer layer = Assert.IsType<AdornerLayer>(AdornerLayer.GetAdornerLayer(adorned));
        layer.Add(adorner);
        decorator.UpdateLayout();

        Assert.Equal(FlowDirection.RightToLeft, adorner.FlowDirection);
        Assert.Equal(FlowDirection.RightToLeft, backstage.FlowDirection);

        ribbon.FlowDirection = FlowDirection.LeftToRight;
        Sta.Drain(DispatcherPriority.DataBind);
        decorator.UpdateLayout();
        Assert.Equal(FlowDirection.LeftToRight, adorner.FlowDirection);
        Assert.Equal(FlowDirection.LeftToRight, backstage.FlowDirection);

        layer.Remove(adorner);
        adorner.Detach();

        var explicitBackstage = new Backstage { FlowDirection = FlowDirection.LeftToRight };
        var explicitAdorner = new BackstageAdorner(adorned, explicitBackstage, ribbon);
        ribbon.FlowDirection = FlowDirection.RightToLeft;
        Sta.Drain(DispatcherPriority.DataBind);
        Assert.Equal(FlowDirection.LeftToRight, explicitBackstage.FlowDirection);
        explicitAdorner.Detach();
    });

    [Fact]
    public void Backstage_slide_uses_the_logical_leading_edge()
    {
        Assert.Equal(
            RibbonSlideFrom.Left,
            Ribbon.BackstageSlideEdge(FlowDirection.LeftToRight));
        Assert.Equal(
            RibbonSlideFrom.Right,
            Ribbon.BackstageSlideEdge(FlowDirection.RightToLeft));
    }
}
