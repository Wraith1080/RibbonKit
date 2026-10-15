using System.Windows.Media.Animation;
using RibbonKit.Animation;
using Xunit;

namespace RibbonKit.Tests;

public class DensityAnimationTests
{
    [Fact]
    public void Density_action_is_appended_and_uses_existing_motion_policy() => Sta.Run(() =>
    {
        Assert.Equal(12, (int)RibbonAnimationAction.ThemeSwitch);
        Assert.Equal(13, (int)RibbonAnimationAction.MdiWindowState);
        Assert.Equal(14, (int)RibbonAnimationAction.MessageBar);
        Assert.Equal(15, (int)RibbonAnimationAction.DensityChange);
        var global = RibbonAnimation.GlobalLevel;
        var reduce = RibbonAnimation.RespectSystemReduceMotion;
        var previous = RibbonAnimation.GetActionOverride(RibbonAnimationAction.DensityChange);
        try
        {
            RibbonAnimation.RespectSystemReduceMotion = false;
            foreach (var (level, milliseconds, offset) in new[]
            {
                (RibbonAnimationLevel.None, 0d, 0d),
                (RibbonAnimationLevel.Subtle, 130d, 18d),
                (RibbonAnimationLevel.Expressive, 182d, 32.4d),
            })
            {
                RibbonAnimation.SetActionLevel(RibbonAnimationAction.DensityChange, level);
                Assert.Equal(milliseconds, RibbonAnimation.GetDuration(RibbonAnimationAction.DensityChange).TimeSpan.TotalMilliseconds);
                Assert.Equal(offset, RibbonAnimation.GetSlideOffset(RibbonAnimationAction.DensityChange), 6);
                var ease = RibbonAnimation.GetEase(RibbonAnimationAction.DensityChange);
                if (level == RibbonAnimationLevel.Expressive)
                    Assert.Equal(EasingMode.EaseOut, Assert.IsType<QuinticEase>(ease).EasingMode);
                else
                {
                    var cubic = Assert.IsType<CubicEase>(ease);
                    Assert.Equal(EasingMode.EaseOut, cubic.EasingMode); Assert.True(cubic.IsFrozen);
                }
                Assert.True(ease.Ease(0.25d) > 0.4d); // Same quick start as group expansion.
            }
            RibbonAnimation.ClearActionLevel(RibbonAnimationAction.DensityChange);
            RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
            Assert.False(RibbonAnimation.IsEnabled(RibbonAnimationAction.DensityChange));
            RibbonAnimation.SetActionLevel(RibbonAnimationAction.DensityChange, RibbonAnimationLevel.Subtle);
            Assert.True(RibbonAnimation.IsEnabled(RibbonAnimationAction.DensityChange));
            RibbonAnimation.RespectSystemReduceMotion = true;
            Assert.Equal(!RibbonAnimation.SystemReduceMotion, RibbonAnimation.IsEnabled(RibbonAnimationAction.DensityChange));
        }
        finally
        {
            RibbonAnimation.GlobalLevel = global;
            RibbonAnimation.RespectSystemReduceMotion = reduce;
            if (previous is { } value) RibbonAnimation.SetActionLevel(RibbonAnimationAction.DensityChange, value);
            else RibbonAnimation.ClearActionLevel(RibbonAnimationAction.DensityChange);
        }
    });
}
