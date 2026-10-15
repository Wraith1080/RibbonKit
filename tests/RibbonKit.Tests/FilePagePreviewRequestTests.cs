using System.IO;
using System.Reflection;
using Xunit;

namespace RibbonKit.Tests;

public sealed class FilePagePreviewRequestTests
{
    [Fact]
    public void A_hidden_page_waits_for_realization_and_applies_once()
    {
        var host = new PreviewHost();
        using var request = host.Request();
        host.Layout();
        Assert.Equal(0, host.Applied);
        Assert.Equal(1, host.Subscribers);
        host.Ready = true;
        host.Layout();
        host.Layout();
        Assert.Equal(1, host.Applied);
        Assert.Equal(0, host.Subscribers);
    }

    [Fact]
    public void An_already_realized_page_applies_without_an_extra_layout()
    {
        var host = new PreviewHost { Ready = true };
        using var request = host.Request();
        Assert.Equal(1, host.Applied);
        Assert.Equal(0, host.Subscribers);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Cancelled_or_replaced_requests_do_not_apply_a_late_page(bool explicitlyCancelled)
    {
        var host = new PreviewHost();
        using var request = host.Request();
        if (explicitlyCancelled) request.Dispose();
        else host.Current = false;
        host.Ready = true;
        host.Layout();
        Assert.Equal(0, host.Applied);
        Assert.Equal(0, host.Subscribers);
    }

    private sealed class PreviewHost
    {
        private EventHandler? _layout;
        internal bool Ready;
        internal bool Current = true;
        internal int Applied;
        internal int Subscribers => _layout?.GetInvocationList().Length ?? 0;
        internal void Layout() => _layout?.Invoke(this, EventArgs.Empty);

        internal IDisposable Request()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "RibbonKit.sln"))) directory = directory.Parent;
            Assert.NotNull(directory);
            string configuration = typeof(FilePagePreviewRequestTests).Assembly.GetCustomAttribute<AssemblyConfigurationAttribute>()!.Configuration;
            var design = Assembly.LoadFrom(Path.Combine(directory.FullName, "src", "RibbonKit.Design", "bin", configuration, "net472", "RibbonKit.DesignTools.dll"));
            var type = design.GetType("RibbonKit.Design.FilePagePreviewRequest", throwOnError: true)!;
            return (IDisposable)Activator.CreateInstance(type, BindingFlags.Instance | BindingFlags.NonPublic, null,
                new object[] { (Func<bool>)(() => Current), (Func<bool>)(() => Ready), (Action)(() => Applied++),
                    (Action<EventHandler>)(handler => _layout += handler), (Action<EventHandler>)(handler => _layout -= handler) }, null)!;
        }
    }
}
