using System.Windows;
using System.Windows.Controls;
using RibbonKit.Controls;
using Xunit;

namespace RibbonKit.Tests;

public sealed class GalleryPopupWidthTests
{
    [Fact]
    public void Popup_width_accepts_auto_and_positive_finite_values() => Sta.Run(() =>
    {
        var gallery = new InRibbonGallery();
        Assert.True(double.IsNaN(gallery.PopupWidth));
        gallery.PopupWidth = 440;
        Assert.Equal(440, gallery.PopupWidth);
        foreach (double invalid in new[] { 0, -1, double.PositiveInfinity, double.NegativeInfinity })
            Assert.Throws<ArgumentException>(() => gallery.PopupWidth = invalid);
        gallery.PopupWidth = double.NaN;
        Assert.True(double.IsNaN(gallery.PopupWidth));
    });

    [Fact]
    public void A_consumer_group_style_survives_template_application() => Sta.Run(() =>
    {
        Sta.UseApplication();
        var gallery = new InRibbonGallery();
        var groupStyle = new GroupStyle { HeaderTemplate = new DataTemplate() };
        gallery.GroupStyle.Add(groupStyle);
        gallery.ApplyTemplate();
        Assert.Same(groupStyle, Assert.Single(gallery.GroupStyle));
        Sta.ResetApplication();
    });
}
