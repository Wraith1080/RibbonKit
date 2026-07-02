using System.Windows.Controls;
using RibbonKit.Controls;
using Xunit;

namespace RibbonKit.Tests;

public class RibbonTests
{
    [Fact]
    public void Ribbon_is_a_lookless_wpf_control()
    {
        // Phase 0 smoke test: the assembly loads and Ribbon derives from Control,
        // which is the contract for a templatable (lookless) custom control.
        Assert.True(typeof(Ribbon).IsSubclassOf(typeof(Control)));
    }

    [Fact]
    public void Ribbon_can_be_instantiated_on_an_sta_thread()
    {
        Exception? failure = null;

        var thread = new Thread(() =>
        {
            try
            {
                var ribbon = new Ribbon();
                Assert.NotNull(ribbon);
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        Assert.Null(failure);
    }
}
