using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace RibbonKit.Animation;

// One already-visible density surface. Its layout is final before this is created;
// neither the opacity base value nor the host's existing transform is discarded.
internal sealed class RibbonDensityTransition : IDisposable
{
    private readonly FrameworkElement _surface;
    private readonly AnimationClock _fade;
    private readonly Transform? _originalTransform;
    private readonly TransformGroup? _transforms;
    private readonly TranslateTransform? _translate;
    private bool _disposed;

    internal RibbonDensityTransition(FrameworkElement surface, double offsetY = 0d)
    {
        _surface = surface;
        const RibbonAnimationAction action = RibbonAnimationAction.DensityChange;
        Duration duration = RibbonAnimation.GetDuration(action);
        IEasingFunction ease = RibbonAnimation.GetEase(action);

        if (offsetY != 0d)
        {
            _originalTransform = surface.RenderTransform;
            _translate = new TranslateTransform(0d, offsetY);
            _transforms = new TransformGroup();
            _transforms.Children.Add(_originalTransform);
            _transforms.Children.Add(_translate);
            // SetCurrentValue retains a consumer's binding/resource expression.
            surface.SetCurrentValue(UIElement.RenderTransformProperty, _transforms);
            _translate.BeginAnimation(TranslateTransform.YProperty,
                new DoubleAnimation(offsetY, 0d, duration) { EasingFunction = ease });
        }

        // Start from a gentle dip, never transparency. No opacity seed is needed on
        // an already-visible surface, and leaving its base untouched preserves bindings.
        _fade = new DoubleAnimation(surface.Opacity * 0.85d,
            (double)surface.GetAnimationBaseValue(UIElement.OpacityProperty), duration)
        {
            EasingFunction = ease,
            FillBehavior = FillBehavior.Stop,
        }.CreateClock();
        _fade.Completed += OnCompleted;
        surface.Unloaded += OnUnloaded;
        surface.ApplyAnimationClock(UIElement.OpacityProperty, _fade);
    }

    private void OnCompleted(object? sender, EventArgs e) => Dispose();
    private void OnUnloaded(object sender, RoutedEventArgs e) => Dispose();

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _fade.Completed -= OnCompleted;
        _surface.Unloaded -= OnUnloaded;
        _surface.ApplyAnimationClock(UIElement.OpacityProperty, null);
        if (_translate is not null)
        {
            _translate.Y = 0d;
            _translate.BeginAnimation(TranslateTransform.YProperty, null);
            // A consumer may have replaced its transform during the transition.
            if (ReferenceEquals(_surface.RenderTransform, _transforms))
                _surface.SetCurrentValue(UIElement.RenderTransformProperty, _originalTransform);
        }
    }
}
