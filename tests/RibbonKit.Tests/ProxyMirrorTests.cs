using System.Windows;
using RibbonKit.Controls;
using Xunit;

namespace RibbonKit.Tests;

/// <summary>
/// A quick-access / overflow / custom-group proxy follows its source's ENABLED state — design
/// notes §3.45. One factory (<c>Ribbon.CreateCommandProxy</c>) builds all three, so these tests
/// cover all three.
/// </summary>
/// <remarks>
/// <para>
/// Worth testing rather than clicking, because the failure is silent and asymmetric: the ribbon
/// button greys exactly as the app intended, while its copy elsewhere stays live and still invokes
/// the command the app just switched off. Nothing looks broken until someone uses the copy.
/// </para>
/// <para>
/// ⚠ <see cref="Parking_does_not_sever_the_source_mirror"/> is the one that guards the actual
/// design decision. Two independent things disable a proxy — its source, and being parked while a
/// merged source is away — and they are combined in a single <c>MultiBinding</c> rather than
/// written separately, because assigning to a property that carries a one-way binding CLEARS that
/// binding. Split them back into two writes and this test fails while the other three still pass.
/// </para>
/// </remarks>
public class ProxyMirrorTests
{
    [Fact]
    public void A_proxy_greys_when_its_source_is_disabled() => Sta.Run(() =>
    {
        (RibbonButton source, FrameworkElement proxy) = Pair();

        Assert.True(proxy.IsEnabled);

        source.IsEnabled = false;

        Assert.False(proxy.IsEnabled);
    });

    [Fact]
    public void A_proxy_comes_back_when_its_source_does() => Sta.Run(() =>
    {
        (RibbonButton source, FrameworkElement proxy) = Pair();

        source.IsEnabled = false;
        source.IsEnabled = true;

        Assert.True(proxy.IsEnabled);
    });

    [Fact]
    public void Parking_greys_a_proxy_whose_source_is_still_enabled() => Sta.Run(() =>
    {
        (RibbonButton source, FrameworkElement proxy) = Pair();

        Ribbon.SetIsCommandParkedInternal(proxy, true);

        Assert.True(source.IsEnabled);
        Assert.False(proxy.IsEnabled);

        Ribbon.SetIsCommandParkedInternal(proxy, false);

        Assert.True(proxy.IsEnabled);
    });

    [Fact]
    public void Parking_does_not_sever_the_source_mirror() => Sta.Run(() =>
    {
        (RibbonButton source, FrameworkElement proxy) = Pair();

        // A full park/revive cycle first — this is what used to write IsEnabled directly and drop
        // the binding on the floor.
        Ribbon.SetIsCommandParkedInternal(proxy, true);
        Ribbon.SetIsCommandParkedInternal(proxy, false);

        source.IsEnabled = false;

        Assert.False(proxy.IsEnabled);
    });

    [Fact]
    public void A_disabled_source_produces_an_already_grey_proxy() => Sta.Run(() =>
    {
        // Order matters to nobody, but a proxy added to the QAT while its command is switched off
        // must not flash in enabled and only catch up on the next change.
        var ribbon = new Ribbon();
        var source = new RibbonButton { Header = "Paste", IsEnabled = false };

        FrameworkElement proxy = ribbon.CreateCommandProxy(source, RibbonControlSize.Small);

        Assert.False(proxy.IsEnabled);
    });

    /// <summary>A source button and a small proxy of it, both outside any visual tree.</summary>
    private static (RibbonButton Source, FrameworkElement Proxy) Pair()
    {
        var ribbon = new Ribbon();
        var source = new RibbonButton { Header = "Paste" };

        return (source, ribbon.CreateCommandProxy(source, RibbonControlSize.Small));
    }
}
