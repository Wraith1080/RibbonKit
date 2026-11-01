using System.CodeDom;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.ComponentModel.Design.Serialization;
using RibbonKit.WinForms.Design;
using RibbonKit.WinForms.Design.Client;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class BackstageContentTypeTests
{
    [Fact]
    public void Unattached_type_selection_waits_for_initialization_and_owned_content_is_reused_while_borrowed_content_stays_borrowed() => FoundationTests.Sta(() =>
    {
        CountingPage.ConstructionCount = 0;
        using var page = new RibbonBackstagePageItem { ContentType = typeof(CountingPage) };
        Assert.Null(page.Content); Assert.Equal(0, CountingPage.ConstructionCount);
        page.EnsureContent(); var first = Assert.IsType<CountingPage>(page.Content);
        first.Editor.Text = "Keep this"; first.Editor.Select(2, 3); var handle = first.Editor.Handle;
        page.EnsureContent(); Assert.Same(first, page.Content); Assert.Equal(1, CountingPage.ConstructionCount);
        Assert.Equal(handle, first.Editor.Handle); Assert.Equal(2, first.Editor.SelectionStart);
        page.ContentType = typeof(OtherPage); Assert.True(first.IsDisposed); Assert.Null(page.Content);
        page.EnsureContent(); var second = Assert.IsType<OtherPage>(page.Content);
        page.ContentType = null; Assert.True(second.IsDisposed);
        using var borrowed = new UserControl(); page.Content = borrowed;
        Assert.Throws<InvalidOperationException>(() => page.ContentType = typeof(CountingPage));
        Assert.Same(borrowed, page.Content); page.Dispose(); Assert.False(borrowed.IsDisposed);
    });

    [Theory]
    [InlineData(typeof(TextBox))] [InlineData(typeof(AbstractPage))] [InlineData(typeof(NoDefaultPage))]
    [InlineData(typeof(GenericPage<>))] [InlineData(typeof(PrivatePage))]
    public void Unsuitable_types_are_rejected_before_replacing_current_content(Type invalid) => FoundationTests.Sta(() =>
    {
        using var page = new RibbonBackstagePageItem { ContentType = typeof(CountingPage) }; page.EnsureContent(); var original = page.Content!;
        Assert.Throws<ArgumentException>(() => page.ContentType = invalid);
        Assert.Equal(typeof(CountingPage), page.ContentType); Assert.Same(original, page.Content); Assert.False(original.IsDisposed);
        using var replacement = new UserControl(); Assert.Throws<InvalidOperationException>(() => page.Content = replacement);
    });

    [Fact]
    public void Designer_selection_preview_and_one_commit_do_not_construct_page_controls() => FoundationTests.Sta(() =>
    {
        CountingPage.ConstructionCount = 0;
        using var host = new DesignerAuthoringTests.Host { TypeDiscovery = new Discovery() };
        using var ribbon = DesignerAuthoringTests.MakeRibbon(host);
        ribbon.Site = new DesignSite(ribbon) { Name = "ribbon" };
        var page = new RibbonBackstagePageItem { Text = "Info" }; host.Container.Add(page, "infoPage"); ribbon.Backstage.Items.Add(page);
        var draft = RibbonBackstageCommitter.Read(ribbon, host);
        var value = draft.Entries[0].Properties.Single(property => property.Name == "ContentType");
        Assert.Contains(RibbonBackstageContentTypes.Label(typeof(CountingPage)), value.Options);
        Assert.DoesNotContain(RibbonBackstageContentTypes.Label(typeof(NoDefaultPage)), value.Options);
        var properties = TypeDescriptor.GetProperties(new RibbonEditorDialog.DraftProperties(draft.Entries[0]));
        Assert.True(properties["ContentType"]!.Converter.GetStandardValuesExclusive());
        value.Value = RibbonBackstageContentTypes.Label(typeof(CountingPage));
        RibbonBackstagePreviewRenderer.Render(ribbon, host, draft);
        Assert.Null(page.ContentType); Assert.Equal(0, host.Transactions); Assert.Equal(0, CountingPage.ConstructionCount);
        Assert.True(RibbonBackstageCommitter.Apply(ribbon, host, draft));
        Assert.Equal(typeof(CountingPage), page.ContentType); Assert.Null(page.Content); Assert.Equal(0, CountingPage.ConstructionCount);
        Assert.Equal(1, host.Committed);
        Assert.False(RibbonBackstageCommitter.Apply(ribbon, host, RibbonBackstageCommitter.Read(ribbon, host))); Assert.Equal(1, host.Transactions);
    });

    [Fact]
    public void Unknown_types_and_application_owned_content_are_rejected_before_a_transaction() => FoundationTests.Sta(() =>
    {
        using var host = new DesignerAuthoringTests.Host { TypeDiscovery = new Discovery() }; using var ribbon = DesignerAuthoringTests.MakeRibbon(host);
        using var content = new UserControl(); var page = new RibbonBackstagePageItem { Content = content }; host.Container.Add(page, "infoPage"); ribbon.Backstage.Items.Add(page);
        foreach (string choice in new[] { "Untrusted.Type [OtherAssembly]", RibbonBackstageContentTypes.Label(typeof(CountingPage)) })
        {
            var draft = RibbonBackstageCommitter.Read(ribbon, host);
            var value = draft.Entries[0].Properties.Single(property => property.Name == "ContentType"); value.Value = choice; value.Options = [choice];
            Assert.ThrowsAny<ArgumentException>(() => RibbonBackstageCommitter.Apply(ribbon, host, draft));
            Assert.Equal(0, host.Transactions); Assert.Same(content, page.Content); Assert.False(content.IsDisposed);
        }
    });

    [Fact]
    public void Real_serializer_emits_typeof_and_excludes_the_realized_owned_instance() => FoundationTests.Sta(() =>
    {
        using var page = new RibbonBackstagePageItem { ContentType = typeof(CountingPage) }; page.EnsureContent();
        var properties = TypeDescriptor.GetProperties(page);
        Assert.False(properties[nameof(page.Content)]!.ShouldSerializeValue(page));
        var serialization = new DesignerSerializationManager(); using var session = serialization.CreateSession();
        var manager = (IDesignerSerializationManager)serialization; manager.SetName(page, "infoPage");
        var serializer = Assert.IsAssignableFrom<CodeDomSerializer>(manager.GetSerializer(typeof(RibbonBackstagePageItem), typeof(CodeDomSerializer)));
        var statements = serializer.SerializeMember(manager, page, properties[nameof(page.ContentType)]!);
        Assert.Contains(statements.Cast<CodeStatement>().OfType<CodeAssignStatement>(), statement => statement.Left is CodePropertyReferenceExpression { PropertyName: "ContentType" }
            && statement.Right is CodeTypeOfExpression type && type.Type.BaseType == typeof(CountingPage).FullName);
        Assert.Empty(serialization.Errors.Cast<object>());
        var content = serializer.SerializeMember(manager, page, properties[nameof(page.Content)]!);
        Assert.DoesNotContain(content.Cast<CodeStatement>().OfType<CodeAssignStatement>(), statement => statement.Left is CodePropertyReferenceExpression { PropertyName: "Content" });
    });

    [Fact]
    public void Borrowed_content_reference_still_serializes_and_resets_without_disposal() => FoundationTests.Sta(() =>
    {
        using var content = new UserControl(); using var page = new RibbonBackstagePageItem { Content = content };
        var property = TypeDescriptor.GetProperties(page)[nameof(page.Content)]!;
        Assert.True(property.ShouldSerializeValue(page)); Assert.True(property.CanResetValue(page));
        var serialization = new DesignerSerializationManager(); using var session = serialization.CreateSession();
        var manager = (IDesignerSerializationManager)serialization; manager.SetName(page, "page"); manager.SetName(content, "body");
        var serializer = (CodeDomSerializer)manager.GetSerializer(typeof(RibbonBackstagePageItem), typeof(CodeDomSerializer))!;
        var statements = serializer.SerializeMember(manager, page, property);
        Assert.Contains(statements.Cast<CodeStatement>().OfType<CodeAssignStatement>(), statement => statement.Left is CodePropertyReferenceExpression { PropertyName: "Content" });
        property.ResetValue(page); Assert.Null(page.Content); Assert.False(content.IsDisposed);
    });

    [Fact]
    public void Page_disposal_releases_a_realized_owned_instance() => FoundationTests.Sta(() =>
    {
        CountingPage.ConstructionCount = 0;
        var page = new RibbonBackstagePageItem { ContentType = typeof(CountingPage) }; page.EnsureContent(); var content = page.Content!;
        content.Disposed += (_, _) => page.EnsureContent();
        page.Dispose(); Assert.True(content.IsDisposed); Assert.Null(page.Content); Assert.Equal(1, CountingPage.ConstructionCount);
    });

    [Fact]
    public void A_design_sited_page_does_not_construct_content_and_a_failed_runtime_constructor_can_be_replaced() => FoundationTests.Sta(() =>
    {
        CountingPage.ConstructionCount = 0;
        using var page = new RibbonBackstagePageItem { ContentType = typeof(CountingPage) };
        page.Site = new DesignSite(page); page.EnsureContent(); Assert.Null(page.Content); Assert.Equal(0, CountingPage.ConstructionCount);
        page.Site = null; page.ContentType = typeof(FailingPage);
        Assert.Throws<System.Reflection.TargetInvocationException>(() => page.EnsureContent()); Assert.Null(page.Content);
        page.ContentType = typeof(CountingPage); page.EnsureContent(); Assert.IsType<CountingPage>(page.Content);
    });

    public sealed class CountingPage : UserControl
    {
        internal static int ConstructionCount;
        internal TextBox Editor { get; } = new();
        public CountingPage() { ConstructionCount++; Controls.Add(Editor); }
    }
    public sealed class OtherPage : UserControl { }
    public sealed class FailingPage : UserControl { public FailingPage() => throw new InvalidOperationException("Application constructor failed."); }
    public abstract class AbstractPage : UserControl { }
    public sealed class NoDefaultPage : UserControl { public NoDefaultPage(int value) { Tag = value; } }
    public sealed class GenericPage<T> : UserControl { }
    private sealed class PrivatePage : UserControl { }
    private sealed class Discovery : ITypeDiscoveryService
    {
        public System.Collections.ICollection GetTypes(Type? baseType, bool excludeGlobalTypes) => new[] { typeof(CountingPage), typeof(OtherPage), typeof(NoDefaultPage), typeof(AbstractPage) };
    }
    private sealed class DesignSite(IComponent component) : ISite
    {
        public IComponent Component => component; public IContainer? Container => null;
        public bool DesignMode => true; public string? Name { get; set; } = "page";
        public object? GetService(Type serviceType) => null;
    }
}
