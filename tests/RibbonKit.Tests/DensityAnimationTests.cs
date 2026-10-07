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
                (RibbonAnimationLevel.Subtle, 160d, 4d),
                (RibbonAnimationLevel.Expressive, 224d, 7.2d),
            })
            {
                RibbonAnimation.SetActionLevel(RibbonAnimationAction.DensityChange, level);
                Assert.Equal(milliseconds, RibbonAnimation.GetDuration(RibbonAnimationAction.DensityChange).TimeSpan.TotalMilliseconds);
                Assert.Equal(offset, RibbonAnimation.GetSlideOffset(RibbonAnimationAction.DensityChange), 6);
                var ease = Assert.IsType<CubicEase>(RibbonAnimation.GetEase(RibbonAnimationAction.DensityChange));
                Assert.Equal(EasingMode.EaseInOut, ease.EasingMode);
                Assert.True(ease.IsFrozen);
                // Keep the dip visible during the first quarter of the transition.
                Assert.InRange(ease.Ease(0.25d), 0d, 0.1d);
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
