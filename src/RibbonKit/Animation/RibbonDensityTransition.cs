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

    internal RibbonDensityTransition(FrameworkElement surface, double offsetY = 0d,
        RibbonAnimationAction action = RibbonAnimationAction.DensityChange, double offsetX = 0d,
        Duration? durationOverride = null, IEasingFunction? easeOverride = null, bool fade = true)
    {
        _surface = surface;
        Duration duration = durationOverride ?? RibbonAnimation.GetDuration(action);
        IEasingFunction ease = easeOverride ?? RibbonAnimation.GetEase(action);

        if (offsetY != 0d || offsetX != 0d)
        {
            _originalTransform = surface.RenderTransform;
            _translate = new TranslateTransform(offsetX, offsetY);
            _transforms = new TransformGroup();
            _transforms.Children.Add(_originalTransform);
            _transforms.Children.Add(_translate);
            // SetCurrentValue retains a consumer's binding/resource expression.
            surface.SetCurrentValue(UIElement.RenderTransformProperty, _transforms);
            _translate.BeginAnimation(TranslateTransform.YProperty,
                new DoubleAnimation(offsetY, 0d, duration) { EasingFunction = ease });
            _translate.BeginAnimation(TranslateTransform.XProperty,
                new DoubleAnimation(offsetX, 0d, duration) { EasingFunction = ease });
        }

        // Start from a gentle dip, never transparency. No opacity seed is needed on
        // an already-visible surface, and leaving its base untouched preserves bindings.
        _fade = new DoubleAnimation(surface.Opacity * (fade ? 0.85d : 1d),
            (double)surface.GetAnimationBaseValue(UIElement.OpacityProperty), duration)
        {
            EasingFunction = ease,
            FillBehavior = FillBehavior.Stop,
        }.CreateClock();
        _fade.Completed += OnCompleted;
        surface.Unloaded += OnUnloaded;
        RibbonAnimation.Changed += OnCompleted;
        SystemParameters.StaticPropertyChanged += OnSystemParametersChanged;
        surface.ApplyAnimationClock(UIElement.OpacityProperty, _fade);
    }

    private void OnCompleted(object? sender, EventArgs e) => Dispose();
    private void OnUnloaded(object sender, RoutedEventArgs e) => Dispose();
    private void OnSystemParametersChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SystemParameters.ClientAreaAnimation)) Dispose();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _fade.Completed -= OnCompleted;
        _surface.Unloaded -= OnUnloaded;
        RibbonAnimation.Changed -= OnCompleted;
        SystemParameters.StaticPropertyChanged -= OnSystemParametersChanged;
        _surface.ApplyAnimationClock(UIElement.OpacityProperty, null);
        if (_translate is not null)
        {
            _translate.Y = 0d;
            _translate.X = 0d;
            _translate.BeginAnimation(TranslateTransform.YProperty, null);
            _translate.BeginAnimation(TranslateTransform.XProperty, null);
            // A consumer may have replaced its transform during the transition.
            if (ReferenceEquals(_surface.RenderTransform, _transforms))
                _surface.SetCurrentValue(UIElement.RenderTransformProperty, _originalTransform);
        }
    }
}
