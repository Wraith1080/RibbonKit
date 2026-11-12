using System.CodeDom;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.ComponentModel.Design.Serialization;
using RibbonKit.WinForms.Design;
using RibbonKit.WinForms.Design.Client;
using RibbonKit.WinForms.Design.Protocol;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class GalleryPresenterTypeTests
{
    [Fact]
    public void Class_selection_is_lazy_and_popup_owned_views_are_fresh_on_reopen() => FoundationTests.Sta(() =>
    {
        Presenter.Constructions = 0; Presenter.Last = null;
        using var form = new Form { Size = new(950, 650) }; using var ribbon = new RibbonControl();
        var tab = new RibbonTab { Text = "Home" }; var group = new RibbonGroup { Text = "Styles" };
        var gallery = new RibbonGalleryItem { Text = "Styles", DropDownPresenterType = typeof(Presenter) };
        gallery.Items.Add(new RibbonGalleryTile { Text = "One" }); group.Items.Add(gallery); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        form.Controls.Add(ribbon); form.Show(); Application.DoEvents();
        Assert.Equal(0, Presenter.Constructions);
        CancelEventHandler cancel = (_, e) => e.Cancel = true; gallery.DropDownOpening += cancel;
        gallery.ShowDropDown(); Assert.Equal(0, Presenter.Constructions); gallery.DropDownOpening -= cancel;
        gallery.ShowDropDown(); var first = Assert.IsType<Presenter>(Presenter.Last); var context = first.Context;
        Assert.Equal(1, Presenter.Constructions); Assert.True(context.IsActive); Assert.NotNull(first.Parent);
        gallery.CloseDropDown(); Assert.True(first.IsDisposed); Assert.False(context.IsActive);
        gallery.ShowDropDown(); var second = Assert.IsType<Presenter>(Presenter.Last);
        Assert.Equal(2, Presenter.Constructions); Assert.NotSame(first, second);
        gallery.DropDownPresenterType = null;
        Assert.True(second.IsDisposed); Assert.False(gallery.IsDropDownOpen);
        gallery.ShowDropDown(); Assert.False(ribbon.Popups.Top!.HasGalleryPresenter); Assert.Equal(2, Presenter.Constructions);
    });

    [Theory]
    [InlineData(typeof(UserControl))] [InlineData(typeof(AbstractPresenter))] [InlineData(typeof(NoDefaultPresenter))]
    [InlineData(typeof(GenericPresenter<>))] [InlineData(typeof(PrivatePresenter))]
    public void Invalid_classes_leave_the_existing_choice_untouched(Type invalid) => FoundationTests.Sta(() =>
    {
        using var gallery = new RibbonGalleryItem { DropDownPresenterType = typeof(Presenter) };
        Assert.Throws<ArgumentException>(() => gallery.DropDownPresenterType = invalid);
        Assert.Equal(typeof(Presenter), gallery.DropDownPresenterType);
    });

    [Fact]
    public void Class_and_factory_are_exclusive_and_changes_fire_only_for_actual_changes() => FoundationTests.Sta(() =>
    {
        using var gallery = new RibbonGalleryItem(); int changes = 0; gallery.DropDownPresenterTypeChanged += (_, _) => changes++;
        gallery.DropDownPresenterType = typeof(Presenter); gallery.DropDownPresenterType = typeof(Presenter);
        Assert.Equal(1, changes); Assert.Throws<InvalidOperationException>(() => gallery.DropDownPresenterFactory = () => new Presenter());
        Assert.Null(gallery.DropDownPresenterFactory); gallery.DropDownPresenterType = null;
        Func<Control> factory = () => new Presenter(); gallery.DropDownPresenterFactory = factory;
        Assert.Throws<InvalidOperationException>(() => gallery.DropDownPresenterType = typeof(Presenter));
        Assert.Same(factory, gallery.DropDownPresenterFactory); Assert.Null(gallery.DropDownPresenterType); Assert.Equal(2, changes);
    });

    [Theory]
    [InlineData("DropDownPresenterType")] [InlineData("InRibbonPresenterType")]
    public void Designer_discovery_draft_preview_apply_and_reset_never_construct_application_controls(string propertyName) => FoundationTests.Sta(() =>
    {
        Presenter.Constructions = 0;
        using var host = new DesignerAuthoringTests.Host { TypeDiscovery = new Discovery() }; using var ribbon = DesignerAuthoringTests.MakeRibbon(host);
        ribbon.Theme = ribbon.Theme with { GalleryAppearance = new() { TileCornerRadius = 10, TileBorderWidth = 2, TileBorderColor = Color.Navy, HeaderBold = false, HeaderBackground = new(Color.LightYellow) } };
        var gallery = ribbon.Tabs[0].Groups[0].Items.OfType<RibbonGalleryItem>().Single(); var selected = gallery.SelectedItem;
        gallery.IsInRibbon = true;
        Assert.True(TypeDescriptor.GetProperties(gallery)[propertyName]!.IsBrowsable);
        var draft = RibbonDesignSnapshot.Deserialize(RibbonDesignCommitter.Read(ribbon, host).Serialize());
        var property = draft.Nodes.Single(node => node.Kind == "Gallery").Properties.Single(property => property.Name == propertyName);
        Assert.Equal("(None)", property.Value); Assert.Equal(new[] { "(None)", RibbonGalleryPresenterTypes.Label(typeof(Presenter)) }, property.Options);
        var descriptors = TypeDescriptor.GetProperties(new RibbonEditorDialog.DraftProperties(draft.Nodes.Single(node => node.Kind == "Gallery")));
        Assert.True(descriptors[property.Name]!.Converter.GetStandardValuesExclusive());
        property.Value = RibbonGalleryPresenterTypes.Label(typeof(Presenter));
        _ = RibbonThemePreviewRenderer.Render(ribbon, host, draft);
        Assert.Null(SelectedType(gallery, propertyName)); Assert.Equal(0, host.Transactions); Assert.Equal(0, Presenter.Constructions);
        Assert.True(RibbonDesignCommitter.Apply(ribbon, host, draft)); Assert.Equal(1, host.Committed);
        Assert.Equal(typeof(Presenter), SelectedType(gallery, propertyName)); Assert.Same(selected, gallery.SelectedItem); Assert.Equal(0, Presenter.Constructions);
        host.TypeDiscovery = null; var retained = RibbonDesignCommitter.Read(ribbon, host);
        Assert.Contains(RibbonGalleryPresenterTypes.Label(typeof(Presenter)), retained.Nodes.Single(node => node.Kind == "Gallery").Properties.Single(value => value.Name == property.Name).Options);
        Assert.False(RibbonDesignCommitter.Apply(ribbon, host, retained)); Assert.Equal(1, host.Transactions);
        retained.Nodes.Single(node => node.Kind == "Gallery").Properties.Single(value => value.Name == property.Name).Value = "(None)";
        Assert.True(RibbonDesignCommitter.Apply(ribbon, host, retained)); Assert.Null(SelectedType(gallery, propertyName)); Assert.Equal(0, Presenter.Constructions);
    });

    [Theory]
    [InlineData("DropDownPresenterType")] [InlineData("InRibbonPresenterType")]
    public void Unavailable_classes_and_factory_conflicts_are_rejected_before_live_mutation(string propertyName) => FoundationTests.Sta(() =>
    {
        using var host = new DesignerAuthoringTests.Host { TypeDiscovery = new Discovery() }; using var ribbon = DesignerAuthoringTests.MakeRibbon(host);
        var gallery = ribbon.Tabs[0].Groups[0].Items.OfType<RibbonGalleryItem>().Single();
        Func<Control> factory = () => new Presenter();
        if (propertyName == "DropDownPresenterType") gallery.DropDownPresenterFactory = factory; else gallery.InRibbonPresenterFactory = factory;
        foreach (var label in new[] { "Untrusted.Type [OtherAssembly]", RibbonGalleryPresenterTypes.Label(typeof(Presenter)) })
        {
            var draft = RibbonDesignCommitter.Read(ribbon, host);
            var property = draft.Nodes.Single(node => node.Kind == "Gallery").Properties.Single(value => value.Name == propertyName);
            property.Value = label; property.Options = [label];
            Assert.Throws<ArgumentException>(() => RibbonDesignCommitter.Apply(ribbon, host, draft));
            Assert.Equal(0, host.Transactions); Assert.Null(SelectedType(gallery, propertyName));
            Assert.Same(factory, propertyName == "DropDownPresenterType" ? gallery.DropDownPresenterFactory : gallery.InRibbonPresenterFactory);
        }
    });

    [Theory]
    [InlineData("DropDownPresenterType")] [InlineData("InRibbonPresenterType")]
    public void Class_selection_is_one_framework_undo_redo_unit_without_creating_a_presenter(string propertyName) => FoundationTests.Sta(() =>
    {
        Presenter.Constructions = 0;
        using var surface = new DesignSurface(typeof(Form)); var host = (IDesignerHost)surface.GetService(typeof(IDesignerHost))!;
        var services = (IServiceContainer)surface.GetService(typeof(IServiceContainer))!;
        services.RemoveService(typeof(ITypeDiscoveryService)); services.AddService(typeof(ITypeDiscoveryService), new Discovery());
        services.RemoveService(typeof(ITypeResolutionService)); services.AddService(typeof(ITypeResolutionService), new Resolution());
        services.AddService(typeof(ComponentSerializationService), new CodeDomComponentSerializationService(host));
        var ribbon = (RibbonControl)host.CreateComponent(typeof(RibbonControl), "ribbon"); ((Form)host.RootComponent).Controls.Add(ribbon);
        var tab = (RibbonTab)host.CreateComponent(typeof(RibbonTab), "homeTab"); ribbon.Tabs.Add(tab);
        var group = (RibbonGroup)host.CreateComponent(typeof(RibbonGroup), "stylesGroup"); tab.Groups.Add(group);
        var gallery = (RibbonGalleryItem)host.CreateComponent(typeof(RibbonGalleryItem), "stylesGallery"); group.Items.Add(gallery);
        using var undo = new UndoRecorder(host);
        var draft = RibbonDesignCommitter.Read(ribbon, host);
        draft.Nodes.Single(node => node.Kind == "Gallery").Properties.Single(value => value.Name == propertyName).Value = RibbonGalleryPresenterTypes.Label(typeof(Presenter));
        Assert.True(RibbonDesignCommitter.Apply(ribbon, host, draft)); Assert.Equal(1, undo.Count); Assert.Equal(typeof(Presenter), SelectedType(gallery, propertyName));
        undo.Toggle(); Assert.Null(SelectedType(gallery, propertyName)); Assert.Same(gallery, group.Items.Single());
        undo.Toggle(); Assert.Equal(typeof(Presenter), SelectedType(gallery, propertyName)); Assert.Equal(0, Presenter.Constructions);
    });

    [Theory]
    [InlineData("DropDownPresenterType")] [InlineData("InRibbonPresenterType")]
    public void Real_serializer_emits_typeof_and_does_not_serialize_a_factory_or_control(string propertyName) => FoundationTests.Sta(() =>
    {
        using var gallery = new RibbonGalleryItem();
        var properties = TypeDescriptor.GetProperties(gallery);
        properties[propertyName]!.SetValue(gallery, typeof(Presenter));
        Assert.Equal(DesignerSerializationVisibility.Hidden, properties[nameof(gallery.DropDownPresenterFactory)]!.SerializationVisibility);
        Assert.Equal(DesignerSerializationVisibility.Hidden, properties[nameof(gallery.InRibbonPresenterFactory)]!.SerializationVisibility);
        var serialization = new DesignerSerializationManager(); using var session = serialization.CreateSession();
        var manager = (IDesignerSerializationManager)serialization; manager.SetName(gallery, "stylesGallery");
        var serializer = Assert.IsAssignableFrom<CodeDomSerializer>(manager.GetSerializer(typeof(RibbonGalleryItem), typeof(CodeDomSerializer)));
        var statements = serializer.SerializeMember(manager, gallery, properties[propertyName]!);
        Assert.Contains(statements.Cast<CodeStatement>().OfType<CodeAssignStatement>(), statement => statement.Left is CodePropertyReferenceExpression property && property.PropertyName == propertyName
            && statement.Right is CodeTypeOfExpression type && type.Type.BaseType == typeof(Presenter).FullName);
        Assert.Empty(serialization.Errors.Cast<object>());
    });

    [Fact]
    public void Inline_and_popup_choices_remain_independent_when_discovery_is_unavailable() => FoundationTests.Sta(() =>
    {
        Presenter.Constructions = 0;
        using var host = new DesignerAuthoringTests.Host(); using var ribbon = DesignerAuthoringTests.MakeRibbon(host);
        var gallery = ribbon.Tabs[0].Groups[0].Items.OfType<RibbonGalleryItem>().Single();
        gallery.InRibbonPresenterType = typeof(InlinePresenter); gallery.DropDownPresenterType = typeof(Presenter);
        Assert.Equal(new[] { typeof(InlinePresenter), typeof(Presenter) }, RibbonGalleryPresenterTypes.Available(host, ribbon));
        var draft = RibbonDesignCommitter.Read(ribbon, host);
        var node = draft.Nodes.Single(node => node.Kind == "Gallery");
        node.Properties.Single(property => property.Name == "InRibbonPresenterType").Value = "(None)";
        Assert.True(RibbonDesignCommitter.Apply(ribbon, host, draft));
        Assert.Null(gallery.InRibbonPresenterType); Assert.Equal(typeof(Presenter), gallery.DropDownPresenterType);
        Assert.Equal(0, Presenter.Constructions);
    });

    private static Type? SelectedType(RibbonGalleryItem gallery, string propertyName) => (Type?)TypeDescriptor.GetProperties(gallery)[propertyName]!.GetValue(gallery);

    public class Presenter : UserControl, IRibbonGalleryPresenter
    {
        internal static int Constructions;
        internal static Presenter? Last;
        internal RibbonGalleryPresenterContext Context = null!;
        public Presenter() { Constructions++; Last = this; Size = new(180, 120); }
        public void Synchronize(RibbonGalleryPresenterContext context) => Context = context;
        public Rectangle GetItemBounds(RibbonGalleryTile item) => new(2, 2, 80, 60);
        public RibbonGalleryTile? HitTest(Point location) => null;
        public RibbonGalleryTile? Navigate(RibbonGalleryTile? current, Keys key) => current;
        public void Reveal(RibbonGalleryTile item) { }
        public void ScrollView(RibbonGalleryScroll amount) { }
        public bool CanScrollBackward => false;
        public bool CanScrollForward => false;
    }
    public abstract class AbstractPresenter : Presenter { }
    public sealed class InlinePresenter : Presenter { }
    public sealed class NoDefaultPresenter : Presenter { public NoDefaultPresenter(int unused) { } }
    public sealed class GenericPresenter<T> : Presenter { }
    private sealed class PrivatePresenter : Presenter { }
    private sealed class Discovery : ITypeDiscoveryService
    {
        public System.Collections.ICollection GetTypes(Type? baseType, bool excludeGlobalTypes) => new[] { typeof(Presenter), typeof(UserControl), typeof(AbstractPresenter), typeof(NoDefaultPresenter) };
    }
    private sealed class UndoRecorder(IServiceProvider services) : UndoEngine(services)
    {
        private UndoUnit? _unit;
        internal int Count;
        internal void Toggle() => _unit!.Undo();
        protected override void AddUndoUnit(UndoUnit unit) { _unit = unit; Count++; }
    }
    // A real project designer resolves user assembly types as well as framework
    // components; the bare DesignSurface needs that project service supplied.
    private sealed class Resolution : ITypeResolutionService
    {
        public System.Reflection.Assembly? GetAssembly(System.Reflection.AssemblyName name) => GetAssembly(name, false);
        public System.Reflection.Assembly? GetAssembly(System.Reflection.AssemblyName name, bool throwOnError) => AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(assembly => assembly.GetName().Name == name.Name);
        public string? GetPathOfAssembly(System.Reflection.AssemblyName name) => GetAssembly(name)?.Location;
        public Type? GetType(string name) => GetType(name, false, false);
        public Type? GetType(string name, bool throwOnError) => GetType(name, throwOnError, false);
        public Type? GetType(string name, bool throwOnError, bool ignoreCase) => Type.GetType(name, false, ignoreCase)
            ?? AppDomain.CurrentDomain.GetAssemblies().Select(assembly => assembly.GetType(name, false, ignoreCase)).FirstOrDefault(type => type is not null);
        public void ReferenceAssembly(System.Reflection.AssemblyName name) { }
    }
}
