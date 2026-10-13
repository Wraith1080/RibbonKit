using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using RibbonKit.Animation;
using Xunit;

namespace RibbonKit.Tests;

public class LayoutAnimationTests
{
    [Fact]
    public void Layout_action_is_additive_and_density_shares_its_timing() => Sta.Run(() =>
    {
        Assert.Equal(16, (int)RibbonAnimationAction.LayoutModeChange);
        Assert.Equal(15, (int)RibbonAnimationAction.DensityChange);
        var reduce = RibbonAnimation.RespectSystemReduceMotion;
        var level = RibbonAnimation.GlobalLevel;
        var previous = RibbonAnimation.GetActionOverride(RibbonAnimationAction.LayoutModeChange);
        try
        {
            RibbonAnimation.RespectSystemReduceMotion = false;
            RibbonAnimation.ClearActionLevel(RibbonAnimationAction.LayoutModeChange);
            foreach (var (intensity, milliseconds) in new[]
            {
                (RibbonAnimationLevel.None, 0d), (RibbonAnimationLevel.Subtle, 130d),
                (RibbonAnimationLevel.Expressive, 182d),
            })
            {
                RibbonAnimation.GlobalLevel = intensity;
                Assert.Equal(milliseconds, RibbonAnimation.GetDuration(RibbonAnimationAction.LayoutModeChange).TimeSpan.TotalMilliseconds);
            }
            RibbonAnimation.SetActionLevel(RibbonAnimationAction.LayoutModeChange, RibbonAnimationLevel.None);
            Assert.False(RibbonAnimation.IsEnabled(RibbonAnimationAction.LayoutModeChange));
            RibbonAnimation.RespectSystemReduceMotion = true;
            RibbonAnimation.SetActionLevel(RibbonAnimationAction.LayoutModeChange, RibbonAnimationLevel.Subtle);
            Assert.Equal(!RibbonAnimation.SystemReduceMotion, RibbonAnimation.IsEnabled(RibbonAnimationAction.LayoutModeChange));
        }
        finally
        {
            RibbonAnimation.GlobalLevel = level;
            RibbonAnimation.RespectSystemReduceMotion = reduce;
            if (previous is { } value) RibbonAnimation.SetActionLevel(RibbonAnimationAction.LayoutModeChange, value);
            else RibbonAnimation.ClearActionLevel(RibbonAnimationAction.LayoutModeChange);
        }
    });

    [Fact]
    public void Layout_cover_hides_before_the_first_timing_tick_without_replacing_the_binding() => Sta.Run(() =>
    {
        var surface = new Border();
        surface.SetBinding(UIElement.OpacityProperty, new Binding(".") { Source = 0.8d });
        var clock = RibbonLayoutDrawing.HideImmediately(surface);
        try
        {
            Assert.Equal(0, surface.Opacity);
            Assert.Equal(0.8d, (double)surface.GetAnimationBaseValue(UIElement.OpacityProperty));
            Assert.True(BindingOperations.IsDataBound(surface, UIElement.OpacityProperty));
        }
        finally
        {
            surface.ApplyAnimationClock(UIElement.OpacityProperty, null);
            clock.Controller?.Remove();
        }
        Assert.Equal(0.8d, surface.Opacity);
    });

    [Fact]
    public void Simplified_tab_motion_preserves_bindings_and_releases_on_policy_change() => Sta.Run(() =>
    {
        var level = RibbonAnimation.GlobalLevel;
        var reduce = RibbonAnimation.RespectSystemReduceMotion;
        var previous = RibbonAnimation.GetActionOverride(RibbonAnimationAction.TabSwitch);
        try
        {
            RibbonAnimation.RespectSystemReduceMotion = false;
            RibbonAnimation.GlobalLevel = RibbonAnimationLevel.Subtle;
            RibbonAnimation.ClearActionLevel(RibbonAnimationAction.TabSwitch);
            var surface = new Border();
            var original = new TranslateTransform(2, 3);
            surface.SetBinding(UIElement.RenderTransformProperty, new Binding(".") { Source = original });
            surface.SetBinding(UIElement.OpacityProperty, new Binding(".") { Source = 0.8d });
            using var transition = RibbonMotion.PlaySimplifiedTabSlide(surface);
            Assert.NotNull(transition);
            var transforms = Assert.IsType<TransformGroup>(surface.RenderTransform);
            Assert.Same(original, transforms.Children[0]);
            Assert.Equal(-8, Assert.IsType<TranslateTransform>(transforms.Children[1]).Y);
            Assert.Equal(0.8, surface.Opacity);
            RibbonAnimation.SetActionLevel(RibbonAnimationAction.TabSwitch, RibbonAnimationLevel.None);
            Assert.Same(original, surface.RenderTransform);
            Assert.False(surface.HasAnimatedProperties);
            Assert.True(BindingOperations.IsDataBound(surface, UIElement.RenderTransformProperty));
            Assert.True(BindingOperations.IsDataBound(surface, UIElement.OpacityProperty));
        }
        finally
        {
            RibbonAnimation.GlobalLevel = level;
            RibbonAnimation.RespectSystemReduceMotion = reduce;
            if (previous is { } value) RibbonAnimation.SetActionLevel(RibbonAnimationAction.TabSwitch, value);
            else RibbonAnimation.ClearActionLevel(RibbonAnimationAction.TabSwitch);
        }
    });
}
