using System.ComponentModel;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class BackstageContentInitializationTests
{
    [Theory]
    [InlineData(false, false)] [InlineData(false, true)] [InlineData(true, false)] [InlineData(true, true)]
    public void All_pages_exist_before_form_load_without_opening_backstage(bool integrated, bool useInit) => FoundationTests.Sta(() =>
    {
        using Form form = integrated ? new RibbonForm() : new Form();
        using var ribbon = new RibbonControl(); if (useInit) ribbon.BeginInit();
        form.Controls.Add(ribbon); if (form is RibbonForm host) host.Ribbon = ribbon;
        var pages = new[]
        {
            new RibbonBackstagePageItem { ContentType = typeof(Body), Text = "Info" },
            new RibbonBackstagePageItem { ContentType = typeof(Body), Text = "Hidden", Visible = false },
            new RibbonBackstagePageItem { ContentType = typeof(Body), Text = "Disabled", Enabled = false }
        };
        ribbon.Backstage.Items.AddRange(pages);
        if (useInit)
        {
            Assert.All(pages, page => Assert.Null(page.Content));
            ribbon.EndInit(); Assert.All(pages, page => Assert.IsType<Body>(page.Content));
        }
        bool loaded = false, readyAtLoad = false;
        form.Load += (_, _) =>
        {
            loaded = true; readyAtLoad = pages.All(page => page.Content is Body);
            if (pages[1].Content is Body body) body.Editor.Text = "Set from Form.Load";
        };
        form.ShowInTaskbar = false; form.StartPosition = FormStartPosition.Manual; form.Location = new(-16000, -16000);
        form.Show();
        Assert.True(loaded); Assert.True(readyAtLoad, "All declared pages must be available to Form.Load, including hidden and disabled pages.");
        Assert.False(ribbon.IsBackstageOpen); Assert.Null(ribbon.BackstageSurface);
        Assert.All(pages, page => Assert.Null(page.Content!.Parent));
        Assert.Equal("Set from Form.Load", ((Body)pages[1].Content!).Editor.Text);
        var controls = pages.Select(page => page.Content!).ToArray(); form.Hide(); form.Show();
        Assert.Equal(controls, pages.Select(page => page.Content));
        ribbon.Dispose(); Assert.All(controls, control => Assert.True(control.IsDisposed));
    });

    [Fact]
    public void Runtime_init_creates_late_pages_and_replacements_and_honors_update_batches() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl(); ribbon.BeginInit(); ribbon.EndInit();
        var first = new RibbonBackstagePageItem { ContentType = typeof(Body) }; ribbon.Backstage.Items.Add(first);
        var original = Assert.IsType<Body>(first.Content);
        first.ContentType = typeof(OtherBody); Assert.True(original.IsDisposed); Assert.IsType<OtherBody>(first.Content);
        ribbon.BeginUpdate();
        var late = new RibbonBackstagePageItem { ContentType = typeof(Body) }; ribbon.Backstage.Items.Add(late); Assert.Null(late.Content);
        ribbon.EndUpdate(); Assert.IsType<Body>(late.Content); Assert.False(ribbon.IsBackstageOpen);
    });

    [Fact]
    public void Designer_end_init_and_control_creation_never_construct_user_content() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl(); ribbon.Site = new DesignSite(ribbon);
        ribbon.BeginInit(); var page = new RibbonBackstagePageItem { ContentType = typeof(Body) }; ribbon.Backstage.Items.Add(page); ribbon.EndInit();
        ribbon.CreateControl(); Assert.Null(page.Content); Assert.False(ribbon.IsBackstageOpen);
    });

    [Fact]
    public void Constructor_failure_is_reported_during_initialization_and_can_be_corrected() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl(); ribbon.BeginInit();
        var page = new RibbonBackstagePageItem { ContentType = typeof(FailingBody) }; ribbon.Backstage.Items.Add(page);
        Assert.Throws<System.Reflection.TargetInvocationException>(() => ribbon.EndInit()); Assert.Null(page.Content);
        page.ContentType = typeof(Body); Assert.IsType<Body>(page.Content); Assert.False(ribbon.IsBackstageOpen);
    });

    public sealed class Body : UserControl { internal TextBox Editor { get; } = new(); public Body() => Controls.Add(Editor); }
    public sealed class OtherBody : UserControl { }
    public sealed class FailingBody : UserControl { public FailingBody() => throw new InvalidOperationException("Application constructor failed."); }
    private sealed class DesignSite(IComponent component) : ISite
    {
        public IComponent Component => component; public IContainer? Container => null;
        public bool DesignMode => true; public string? Name { get; set; } = "ribbon";
        public object? GetService(Type serviceType) => null;
    }
}
