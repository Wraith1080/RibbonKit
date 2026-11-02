using System.CodeDom;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.ComponentModel.Design.Serialization;
using RibbonKit.WinForms.Design;
using RibbonKit.WinForms.Design.Client;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class DropDownContentDesignerTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void Discovery_draft_preview_apply_and_reset_are_inert_and_preserve_menu_children(bool split) => FoundationTests.Sta(() =>
    {
        Body.Constructions = 0;
        using var host = new DesignerAuthoringTests.Host { TypeDiscovery = new Discovery() }; using var ribbon = DesignerAuthoringTests.MakeRibbon(host);
        ribbon.Site = new DesignSite(ribbon);
        var item = CreateItem(split); host.Container.Add(item, "popup"); ribbon.Tabs[0].Groups[0].Items.Add(item);
        var child = new RibbonButtonItem { Text = "Menu child" }; host.Container.Add(child, "child"); item.Items.Add(child);
        var draft = RibbonDesignCommitter.Read(ribbon, host); var node = draft.Nodes.Single(node => node.ComponentName == "popup");
        var property = node.Properties.Single(value => value.Name == "ContentType");
        Assert.Contains(RibbonBackstageContentTypes.Label(typeof(Body)), property.Options);
        Assert.DoesNotContain(RibbonBackstageContentTypes.Label(typeof(DropDownContentTests.NoDefaultBody)), property.Options);
        Assert.True(TypeDescriptor.GetProperties(new RibbonEditorDialog.DraftProperties(node))[property.Name]!.Converter.GetStandardValuesExclusive());
        Assert.DoesNotContain(draft.Nodes.Single(node => node.Kind == "Gallery").Properties, property => property.Name == "ContentType");
        property.Value = RibbonBackstageContentTypes.Label(typeof(Body)); _ = RibbonThemePreviewRenderer.Render(ribbon, host, draft);
        Assert.Null(item.ContentType); Assert.Equal(0, Body.Constructions); Assert.Equal(0, host.Transactions);
        Assert.True(RibbonDesignCommitter.Apply(ribbon, host, draft)); Assert.Equal(typeof(Body), item.ContentType); Assert.Null(item.Content); Assert.Equal(1, host.Committed); Assert.Same(child, item.Items.Single());
        host.TypeDiscovery = null; var retained = RibbonDesignCommitter.Read(ribbon, host);
        Assert.Contains(property.Value, retained.Nodes.Single(node => node.ComponentName == "popup").Properties.Single(value => value.Name == "ContentType").Options);
        Assert.False(RibbonDesignCommitter.Apply(ribbon, host, retained)); Assert.Equal(1, host.Transactions);
        retained.Nodes.Single(node => node.ComponentName == "popup").Properties.Single(value => value.Name == "ContentType").Value = "(None)";
        Assert.True(RibbonDesignCommitter.Apply(ribbon, host, retained)); Assert.Null(item.ContentType); Assert.Equal(0, Body.Constructions); Assert.Same(child, item.Items.Single());
    });

    [Fact]
    public void Unknown_types_and_borrowed_content_conflicts_fail_before_a_transaction() => FoundationTests.Sta(() =>
    {
        using var host = new DesignerAuthoringTests.Host { TypeDiscovery = new Discovery() }; using var ribbon = DesignerAuthoringTests.MakeRibbon(host);
        using var borrowed = new UserControl(); var item = new RibbonDropDownItem { Content = borrowed }; host.Container.Add(item, "popup"); ribbon.Tabs[0].Groups[0].Items.Add(item);
        foreach (var label in new[] { "Missing.Type [Missing]", RibbonBackstageContentTypes.Label(typeof(Body)) })
        {
            var draft = RibbonDesignCommitter.Read(ribbon, host); var property = draft.Nodes.Single(node => node.ComponentName == "popup").Properties.Single(value => value.Name == "ContentType"); property.Value = label; property.Options = [label];
            Assert.ThrowsAny<ArgumentException>(() => RibbonDesignCommitter.Apply(ribbon, host, draft)); Assert.Equal(0, host.Transactions); Assert.Same(borrowed, item.Content);
        }
    });

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void CodeDom_serializes_typeof_excludes_owned_body_and_keeps_borrowed_reference(bool split) => FoundationTests.Sta(() =>
    {
        using var item = CreateItem(split); item.ContentType = typeof(Body); item.EnsureContent();
        var properties = TypeDescriptor.GetProperties(item); Assert.False(properties["Content"]!.ShouldSerializeValue(item));
        var serialization = new DesignerSerializationManager(); using var session = serialization.CreateSession(); var manager = (IDesignerSerializationManager)serialization; manager.SetName(item, "popup");
        var serializer = (CodeDomSerializer)manager.GetSerializer(item.GetType(), typeof(CodeDomSerializer))!;
        var statements = serializer.SerializeMember(manager, item, properties["ContentType"]!);
        Assert.Contains(statements.Cast<CodeStatement>().OfType<CodeAssignStatement>(), statement => statement.Left is CodePropertyReferenceExpression { PropertyName: "ContentType" } && statement.Right is CodeTypeOfExpression type && type.Type.BaseType == typeof(Body).FullName);
        var body = item.Content!; item.ContentType = null; Assert.True(body.IsDisposed); using var borrowed = new Body(); item.Content = borrowed; manager.SetName(borrowed, "borrowedBody");
        statements = serializer.SerializeMember(manager, item, properties["Content"]!);
        Assert.Contains(statements.Cast<CodeStatement>().OfType<CodeAssignStatement>(), statement => statement.Left is CodePropertyReferenceExpression { PropertyName: "Content" } && statement.Right is CodeVariableReferenceExpression { VariableName: "borrowedBody" });
        Assert.Empty(serialization.Errors.Cast<object>());
    });

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void Actual_UndoEngine_retains_type_and_model_identity_without_running_constructor(bool split) => FoundationTests.Sta(() =>
    {
        Body.Constructions = 0; using var surface = new DesignSurface(typeof(Form)); var host = (IDesignerHost)surface.GetService(typeof(IDesignerHost))!;
        var services = (IServiceContainer)surface.GetService(typeof(IServiceContainer))!;
        services.RemoveService(typeof(ITypeDiscoveryService)); services.AddService(typeof(ITypeDiscoveryService), new Discovery());
        services.RemoveService(typeof(ITypeResolutionService)); services.AddService(typeof(ITypeResolutionService), new Resolution());
        services.AddService(typeof(ComponentSerializationService), new CodeDomComponentSerializationService(host));
        var ribbon = (RibbonControl)host.CreateComponent(typeof(RibbonControl), "ribbon"); ((Form)host.RootComponent).Controls.Add(ribbon);
        var tab = (RibbonTab)host.CreateComponent(typeof(RibbonTab), "homeTab"); ribbon.Tabs.Add(tab);
        var group = (RibbonGroup)host.CreateComponent(typeof(RibbonGroup), "commandsGroup"); tab.Groups.Add(group);
        var item = (RibbonDropDownItem)host.CreateComponent(split ? typeof(RibbonSplitButtonItem) : typeof(RibbonDropDownItem), "popup"); group.Items.Add(item);
        using var undo = new UndoRecorder(host); var draft = RibbonDesignCommitter.Read(ribbon, host);
        draft.Nodes.Single(node => node.ComponentName == "popup").Properties.Single(value => value.Name == "ContentType").Value = RibbonBackstageContentTypes.Label(typeof(Body));
        Assert.True(RibbonDesignCommitter.Apply(ribbon, host, draft)); Assert.Equal(1, undo.Count); Assert.Equal(typeof(Body), item.ContentType);
        undo.Toggle(); Assert.Null(item.ContentType); Assert.Same(item, group.Items.Single());
        undo.Toggle(); Assert.Equal(typeof(Body), item.ContentType); Assert.Same(item, group.Items.Single()); Assert.Equal(0, Body.Constructions);
    });

    private static RibbonDropDownItem CreateItem(bool split) => split ? new RibbonSplitButtonItem() : new RibbonDropDownItem();
    public sealed class Body : UserControl { internal static int Constructions; public Body() => Constructions++; }
    private sealed class Discovery : ITypeDiscoveryService
    {
        public System.Collections.ICollection GetTypes(Type? baseType, bool excludeGlobalTypes) => new[] { typeof(Body), typeof(DropDownContentTests.NoDefaultBody), typeof(DropDownContentTests.AbstractBody) };
    }
    private sealed class UndoRecorder(IServiceProvider services) : UndoEngine(services)
    {
        private UndoUnit? _unit; internal int Count; internal void Toggle() => _unit!.Undo(); protected override void AddUndoUnit(UndoUnit unit) { _unit = unit; Count++; }
    }
    private sealed class Resolution : ITypeResolutionService
    {
        public System.Reflection.Assembly? GetAssembly(System.Reflection.AssemblyName name) => GetAssembly(name, false);
        public System.Reflection.Assembly? GetAssembly(System.Reflection.AssemblyName name, bool throwOnError) => AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(assembly => assembly.GetName().Name == name.Name);
        public string? GetPathOfAssembly(System.Reflection.AssemblyName name) => GetAssembly(name)?.Location;
        public Type? GetType(string name) => GetType(name, false, false); public Type? GetType(string name, bool throwOnError) => GetType(name, throwOnError, false);
        public Type? GetType(string name, bool throwOnError, bool ignoreCase) => Type.GetType(name, false, ignoreCase) ?? AppDomain.CurrentDomain.GetAssemblies().Select(assembly => assembly.GetType(name, false, ignoreCase)).FirstOrDefault(type => type is not null);
        public void ReferenceAssembly(System.Reflection.AssemblyName name) { }
    }
    private sealed class DesignSite(IComponent component) : ISite
    {
        public IComponent Component => component; public IContainer? Container => null; public bool DesignMode => true; public string? Name { get; set; } = "ribbon"; public object? GetService(Type serviceType) => null;
    }
}
