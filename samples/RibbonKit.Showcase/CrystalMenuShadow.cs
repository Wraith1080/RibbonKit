using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace RibbonKit.Showcase;

/// <summary>Casts the full menu silhouette without filling or shading its translucent interior.</summary>
internal sealed class CrystalMenuShadow
{
    private readonly Border _host;
    private readonly Border _frame;
    private readonly Grid _wrapper = new();
    private readonly Grid _shadow = new() { Name = "CrystalMenuShadow", IsHitTestVisible = false };
    private readonly Border _caster = new() { Background = Brushes.Black, IsHitTestVisible = false };
    private readonly CrystalMenuBackdrop _backdrop;

    public CrystalMenuShadow(Border host, Border frame, FrameworkElement owner)
    {
        _host = host;
        _frame = frame;
        _caster.SetBinding(Border.CornerRadiusProperty, new Binding(nameof(frame.CornerRadius)) { Source = frame });
        _caster.SetResourceReference(UIElement.EffectProperty, "Crystal.Effects.ApplicationMenuShadow");
        _shadow.Children.Add(_caster);
        host.Child = null;
        _wrapper.Children.Add(_shadow);
        _wrapper.Children.Add(frame);
        host.Child = _wrapper;
        _backdrop = new CrystalMenuBackdrop(owner, host, frame, _wrapper);
        // Content and footer buttons must not cast separate shadows into the clear frame.
        frame.Effect = null;
        frame.SizeChanged += OnSizeChanged;
        UpdateClip();
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e) => UpdateClip();

    public void RefreshBackdrop() => _backdrop.Refresh();

    public void UpdateClip()
    {
        var bounds = new Rect(_frame.RenderSize);
        var extent = bounds;
        extent.Inflate((_caster.Effect as DropShadowEffect)?.BlurRadius * 2 ?? 32,
            (_caster.Effect as DropShadowEffect)?.BlurRadius * 2 ?? 32);
        // Clip the parent, AFTER its child's shadow effect. Only the outside halo survives;
        // the opaque caster and any shadow beneath the glass are excluded.
        _shadow.Clip = new CombinedGeometry(GeometryCombineMode.Exclude,
            new RectangleGeometry(extent),
            new RectangleGeometry(bounds, _frame.CornerRadius.TopLeft, _frame.CornerRadius.TopLeft));
    }

    public void Remove()
    {
        _backdrop.Remove();
        _frame.SizeChanged -= OnSizeChanged;
        _host.Child = null;
        _wrapper.Children.Remove(_frame);
        _host.Child = _frame;
        _frame.ClearValue(UIElement.EffectProperty);
    }
}
