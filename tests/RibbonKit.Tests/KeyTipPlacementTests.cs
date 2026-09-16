using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using RibbonKit.Controls;
using Xunit;

namespace RibbonKit.Tests;

public class KeyTipPlacementTests
{
    [Theory]
    [InlineData(FlowDirection.LeftToRight, -30d)]
    [InlineData(FlowDirection.RightToLeft, 30d)]
    public void Backstage_badge_follows_slide_without_mouse_or_layout_input(
        FlowDirection flow, double offset) => Sta.Run(() =>
    {
        var target = new Button { Width = 120, Height = 40, Content = "Home" };
        var slide = new TranslateTransform(offset, 0);
        var surface = new Grid { RenderTransform = slide };
        surface.Children.Add(target);
        var content = new Border();
        var decorator = new AdornerDecorator { Child = content };
        var window = new Window
        {
            Content = decorator,
            Width = 400,
            Height = 250,
            ShowActivated = false,
            ShowInTaskbar = false,
            WindowStartupLocation = WindowStartupLocation.Manual,
            Left = -10000,
            Top = -10000,
        };
        var backstage = new BackstageAdorner(content, surface, new Ribbon { FlowDirection = flow });
        KeyTipAdorner? badge = null;
        try
        {
            window.Show();
            decorator.AdornerLayer.Add(backstage);
            decorator.UpdateLayout();
            badge = new KeyTipAdorner(target, "H");
            decorator.AdornerLayer.Add(badge);
            PumpFrames();
            AssertAligned();

            // Hold an intermediate frame, then finish the same render-only slide.
            // No hover, explicit UpdateLayout, or AdornerLayer.Update repairs placement.
            var clock = new DoubleAnimation(offset, 0, TimeSpan.FromSeconds(1)).CreateClock();
            slide.ApplyAnimationClock(TranslateTransform.XProperty, clock);
            clock.Controller!.Pause();
            clock.Controller.SeekAlignedToLastTick(TimeSpan.FromMilliseconds(500), TimeSeekOrigin.BeginTime);
            PumpFrames();
            AssertAligned();

            slide.BeginAnimation(TranslateTransform.XProperty, null);
            slide.X = 0;
            PumpFrames();
            AssertAligned();

            // The same badge can be removed and reused when navigating KeyTip levels.
            decorator.AdornerLayer.Remove(badge);
            PumpFrames();
            slide.X = offset;
            decorator.AdornerLayer.Add(badge);
            PumpFrames();
            slide.X = 0;
            PumpFrames();
            AssertAligned();
        }
        finally
        {
            if (badge is not null)
            {
                decorator.AdornerLayer.Remove(badge);
            }

            decorator.AdornerLayer.Remove(backstage);
            backstage.Detach();
            window.Close();
        }

        void AssertAligned()
        {
            var chip = (FrameworkElement)VisualTreeHelper.GetChild(badge!, 0);
            Point actual = chip.TransformToVisual(decorator).Transform(new Point(chip.ActualWidth / 2, 0));
            Point expected = target.TransformToVisual(decorator).Transform(
                new Point(target.ActualWidth / 2, target.ActualHeight - chip.ActualHeight / 2 - 1));
            Assert.InRange(Math.Abs(actual.X - expected.X), 0, 0.1);
            Assert.InRange(Math.Abs(actual.Y - expected.Y), 0, 0.1);
        }
    });

    private static void PumpFrames()
    {
        var frame = new DispatcherFrame();
        var timer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(120),
        };
        timer.Tick += (_, _) => frame.Continue = false;
        timer.Start();
        try
        {
            Dispatcher.PushFrame(frame);
        }
        finally
        {
            timer.Stop();
        }
    }
}
