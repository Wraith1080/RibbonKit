using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using RibbonKit.Animation;
using RibbonKit.Controls;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Tests;

public class RibbonWindowTitleTransitionTests
{
    [Theory]
    [InlineData(FlowDirection.LeftToRight)]
    [InlineData(FlowDirection.RightToLeft)]
    public void Icon_visibility_preserves_the_title_position_before_its_glide(FlowDirection flow) => Sta.Run(() =>
    {
        var originalLevel = RibbonAnimation.GlobalLevel;
        bool originalPolicy = RibbonAnimation.RespectSystemReduceMotion;
        var originalOverride = RibbonAnimation.GetActionOverride(RibbonAnimationAction.Backstage);
        var window = new RibbonWindow
        {
            Title = "Animated title", FlowDirection = flow,
            Icon = new DrawingImage(new GeometryDrawing(Brushes.Blue, null, new RectangleGeometry(new Rect(0, 0, 16, 16)))),
            TitleBarContent = new Border { Width = 120, Height = 20 }, Content = new Grid(),
            Width = 840, Height = 400, Left = -10000, Top = -10000,
            ShowActivated = false, ShowInTaskbar = false,
        };
        try
        {
            ThemeManager.Apply(Sta.UseApplication(), RibbonTheme.Office2013);
            RibbonAnimation.GlobalLevel = RibbonAnimationLevel.Subtle;
            RibbonAnimation.RespectSystemReduceMotion = false;
            RibbonAnimation.ClearActionLevel(RibbonAnimationAction.Backstage);
            window.Show();
            Sta.Drain(DispatcherPriority.ApplicationIdle);
            window.UpdateLayout();
            var title = Assert.IsType<TextBlock>(window.Template.FindName("PART_Title", window));
            var icon = Assert.IsType<Image>(window.Template.FindName("PART_WindowIcon", window));
            double before = TitleX();
            icon.Visibility = Visibility.Collapsed;
            window.UpdateLayout();
            Assert.Equal(before, TitleX(), 4);
            Assert.NotEqual(0d, title.RenderTransform.Value.OffsetX);

            // Hold a real intermediate translate clock, then restore the icon before it ends.
            var slide = Assert.IsType<TranslateTransform>(title.RenderTransform);
            double start = slide.X;
            var clock = new DoubleAnimation(start, 0, TimeSpan.FromSeconds(1)).CreateClock();
            slide.ApplyAnimationClock(TranslateTransform.XProperty, clock);
            clock.Controller!.Pause();
            clock.Controller.SeekAlignedToLastTick(TimeSpan.FromMilliseconds(500), TimeSeekOrigin.BeginTime);
            before = TitleX();
            icon.Visibility = Visibility.Visible;
            window.UpdateLayout();
            Assert.Equal(before, TitleX(), 4);
            Assert.NotEqual(0d, title.RenderTransform.Value.OffsetX);

            RibbonAnimation.SetActionLevel(RibbonAnimationAction.Backstage, RibbonAnimationLevel.None);
            icon.Visibility = Visibility.Collapsed;
            window.UpdateLayout();
            Assert.Equal(Matrix.Identity, title.RenderTransform.Value);

            // Hidden still reserves the image width; collapsing that width must glide too.
            icon.Visibility = Visibility.Hidden;
            window.UpdateLayout();
            before = TitleX();
            RibbonAnimation.ClearActionLevel(RibbonAnimationAction.Backstage);
            icon.Visibility = Visibility.Collapsed;
            window.UpdateLayout();
            Assert.Equal(before, TitleX(), 4);

            before = TitleX();
            icon.Visibility = Visibility.Visible;
            window.IsTitleBarContentVisible = false;
            window.UpdateLayout();
            Assert.Equal(before, TitleX(), 4);

            var oldIcon = icon;
            var template = window.Template;
            window.Template = null;
            window.Template = template;
            window.ApplyTemplate();
            window.UpdateLayout();
            var replacementTitle = Assert.IsType<TextBlock>(window.Template.FindName("PART_Title", window));
            Assert.Equal(Matrix.Identity, replacementTitle.RenderTransform.Value);
            oldIcon.Visibility = Visibility.Visible;
            window.UpdateLayout();
            Assert.Equal(Matrix.Identity, replacementTitle.RenderTransform.Value);

            double TitleX() => title.TransformToAncestor(window).Transform(default).X;
        }
        finally
        {
            window.Close();
            RibbonAnimation.GlobalLevel = originalLevel;
            RibbonAnimation.RespectSystemReduceMotion = originalPolicy;
            if (originalOverride is { } level) RibbonAnimation.SetActionLevel(RibbonAnimationAction.Backstage, level);
            else RibbonAnimation.ClearActionLevel(RibbonAnimationAction.Backstage);
            Sta.ResetApplication();
        }
    });
}
