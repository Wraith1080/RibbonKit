using System.CodeDom;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.ComponentModel.Design.Serialization;
using RibbonKit.WinForms.Design;
using RibbonKit.WinForms.Design.Client;
using RibbonKit.WinForms.Design.Protocol;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class DesignerNameTests
{
    private sealed class Fixture : IDisposable
    {
        internal readonly DesignSurface Surface = new(typeof(Form));
        internal readonly IDesignerHost Host;
        internal readonly RibbonControl Ribbon;
        internal readonly RibbonButtonItem Button, Catalog;
        internal readonly RibbonQuickAccessReference Reference;
        internal readonly RibbonBackstagePageItem Page;
        internal Fixture()
        {
            Host = (IDesignerHost)Surface.GetService(typeof(IDesignerHost))!;
            Host.RootComponent.Site!.Name = "form";
            Ribbon = Create<RibbonControl>("ribbon"); Ribbon.Name = "ribbon"; ((Form)Host.RootComponent).Controls.Add(Ribbon);
            var tab = Create<RibbonTab>("homeTab"); tab.Text = "Home"; Ribbon.Tabs.Add(tab);
            var group = Create<RibbonGroup>("commandsGroup"); tab.Groups.Add(group);
            Button = Create<RibbonButtonItem>("button1"); Button.Text = "Save"; Button.Id = "save"; group.Items.Add(Button);
            group.Items.Add(Create<RibbonToggleItem>("toggle1")); group.Items.Add(Create<RibbonCheckBoxItem>("check1")); group.Items.Add(Create<RibbonRadioButtonItem>("radio1"));
            var menu = Create<RibbonDropDownItem>("menu1"); group.Items.Add(menu);
            menu.Items.Add(Create<RibbonButtonItem>("menuButton1")); menu.Items.Add(Create<RibbonMenuSeparatorItem>("menuSeparator1"));
            group.Items.Add(Create<RibbonSplitButtonItem>("split1")); group.Items.Add(Create<RibbonTextBoxItem>("text1")); group.Items.Add(Create<RibbonComboBoxItem>("combo1"));
            var gallery = Create<RibbonGalleryItem>("gallery1"); group.Items.Add(gallery); gallery.Items.Add(Create<RibbonGalleryTile>("tile1"));
            Catalog = Create<RibbonButtonItem>("catalog1"); Ribbon.CommandCatalog.Commands.Add(Catalog);
            Reference = Create<RibbonQuickAccessReference>("qatSave"); Reference.Source = Button; Ribbon.QuickAccessItems.Add(Reference);
            Page = Create<RibbonBackstagePageItem>("infoPage"); Page.Text = "Info"; Ribbon.Backstage.Items.Add(Page);
            Ribbon.Backstage.Items.Add(Create<RibbonButtonItem>("backstageButton1")); Ribbon.Backstage.FooterItems.Add(Create<RibbonMenuSeparatorItem>("backstageSeparator1"));
        }
        internal T Create<T>(string name) where T : IComponent => (T)Host.CreateComponent(typeof(T), name);
        internal RibbonDesignSnapshot Read() => RibbonDesignSnapshot.Deserialize(RibbonDesignCommitter.Read(Ribbon, Host).Serialize());
        public void Dispose() => Surface.Dispose();
    }

    [Fact]
    public void Every_named_node_has_a_detached_Name_field_and_organizational_sections_do_not() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); var draft = f.Read(); var backstage = RibbonBackstageCommitter.Read(f.Ribbon, f.Host);
        foreach (var node in draft.AllNodes.Concat(backstage.Entries))
        {
            var properties = new RibbonEditorDialog.DraftProperties(node);
            var name = TypeDescriptor.GetProperties(properties)["(Name)"]!;
            Assert.NotNull(name); Assert.Equal("Design", name.Category); Assert.False(name.IsReadOnly);
            name.SetValue(properties, "edited_" + node.ComponentName);
            Assert.StartsWith("edited_", node.ComponentName);
        }
        Assert.Null(TypeDescriptor.GetProperties(new RibbonEditorDialog.DraftProperties(new() { Kind = "Backstage" }, named: false))["(Name)"]);
        Assert.Equal("button1", f.Button.Site!.Name); Assert.Equal("Save", f.Button.Text); Assert.Equal("save", f.Button.Id);
        Assert.Equal("infoPage", f.Page.Site!.Name); Assert.Empty(f.Ribbon.Controls);
    });

    [Fact]
    public void Rename_preserves_every_ribbon_object_source_event_binding_and_collection_in_one_undo_unit() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); var draft = f.Read(); var originals = draft.AllNodes.ToDictionary(n => n.Key, n => f.Host.Container.Components[n.ComponentName]!);
        var source = new BindingSource { DataSource = new BindingValue() };
        f.Button.DataBindings.Add(new Binding(nameof(f.Button.Text), source, nameof(BindingValue.Text), false, DataSourceUpdateMode.OnPropertyChanged));
        var binding = f.Button.DataBindings[0]; int clicks = 0; f.Button.Click += (_, _) => clicks++;
        foreach (var node in draft.AllNodes) node.ComponentName = "renamed_" + node.ComponentName;
        draft.ComponentName = "mainRibbon";
        AddSerialization(f); using var undo = new UndoRecorder(f.Host);
        int collectionChanges = 0;
        ((IComponentChangeService)f.Host.GetService(typeof(IComponentChangeService))!).ComponentChanged += (_, e) =>
        { if (e.Member?.Name is "Tabs" or "Groups" or "Items" or "Commands" or "QuickAccessItems") collectionChanges++; };
        Assert.True(RibbonDesignCommitter.Apply(f.Ribbon, f.Host, draft));
        foreach (var node in draft.AllNodes) Assert.Same(originals[node.Key], f.Host.Container.Components[node.ComponentName]);
        Assert.Equal("mainRibbon", f.Ribbon.Site!.Name); Assert.Equal("mainRibbon", f.Ribbon.Name);
        Assert.Same(f.Button, f.Reference.Source); Assert.Same(binding, f.Button.DataBindings[0]); Assert.Equal("save", f.Button.Id);
        f.Button.PerformClick(); Assert.Equal(1, clicks); Assert.Equal(0, collectionChanges); Assert.Equal(1, undo.Count);
        undo.Toggle(); Assert.Equal("button1", f.Button.Site!.Name); Assert.Equal("ribbon", f.Ribbon.Site!.Name); Assert.Equal("ribbon", f.Ribbon.Name); Assert.Same(f.Button, f.Reference.Source);
        undo.Toggle(); Assert.Equal("renamed_button1", f.Button.Site!.Name); Assert.Equal("mainRibbon", f.Ribbon.Name); Assert.Same(f.Button, f.Reference.Source);
        source.Dispose();
    });

    [Fact]
    public void Backstage_renames_preserve_native_page_content_and_sources_without_rebuilding_navigation() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); using var content = new UserControl(); var notes = new TextBox { Text = "Keep notes" }; content.Controls.Add(notes); f.Page.Content = content;
        _ = notes.Handle; notes.Select(2, 3); var handle = notes.Handle;
        var reference = f.Create<RibbonQuickAccessReference>("qatBackstage"); reference.Source = f.Ribbon.Backstage.Items.OfType<RibbonButtonItem>().Single(); f.Ribbon.QuickAccessItems.Add(reference);
        var draft = RibbonBackstageCommitter.Read(f.Ribbon, f.Host); var entries = f.Ribbon.Backstage.NavigationItems.ToArray();
        foreach (var node in draft.Entries) node.ComponentName = "renamed_" + node.ComponentName;
        AddSerialization(f); using var undo = new UndoRecorder(f.Host); int collectionChanges = 0;
        ((IComponentChangeService)f.Host.GetService(typeof(IComponentChangeService))!).ComponentChanged += (_, e) => { if (e.Member?.Name is "Items" or "FooterItems") collectionChanges++; };
        Assert.True(RibbonBackstageCommitter.Apply(f.Ribbon, f.Host, draft));
        Assert.Equal(entries, f.Ribbon.Backstage.NavigationItems); Assert.Same(content, f.Page.Content); Assert.Equal(handle, notes.Handle); Assert.Equal(2, notes.SelectionStart); Assert.Equal(3, notes.SelectionLength);
        Assert.Equal("renamed_infoPage", f.Page.Site!.Name); Assert.Equal(0, collectionChanges); Assert.Equal(1, undo.Count);
        Assert.Same(entries.OfType<RibbonButtonItem>().Single(), reference.Source);
        undo.Toggle(); Assert.Equal("infoPage", f.Page.Site!.Name); Assert.Same(content, f.Page.Content);
        undo.Toggle(); Assert.Equal("renamed_infoPage", f.Page.Site!.Name); Assert.Equal("Keep notes", notes.Text);
    });

    [Theory]
    [InlineData("")] [InlineData("with space")] [InlineData("123bad")] [InlineData("class")]
    [InlineData("form")] [InlineData("catalog1")] [InlineData("ribbon")]
    public void Invalid_or_occupied_names_are_rejected_before_a_live_transaction_in_both_editors(string name) => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); var draft = f.Read(); draft.Nodes.Single(n => n.ComponentName == "button1").ComponentName = name;
        var backstage = RibbonBackstageCommitter.Read(f.Ribbon, f.Host); backstage.Entries.Single(n => n.ComponentName == "infoPage").ComponentName = name;
        int transactions = 0; f.Host.TransactionOpened += (_, _) => transactions++;
        Assert.ThrowsAny<ArgumentException>(() => RibbonDesignCommitter.Apply(f.Ribbon, f.Host, draft));
        Assert.ThrowsAny<ArgumentException>(() => RibbonBackstageCommitter.Apply(f.Ribbon, f.Host, backstage));
        Assert.Equal(0, transactions); Assert.Equal("button1", f.Button.Site!.Name); Assert.Equal("infoPage", f.Page.Site!.Name); Assert.Same(f.Button, f.Reference.Source);
    });

    [Fact]
    public void New_commands_accept_the_requested_name_before_first_creation() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); var draft = f.Read(); using var editor = new RibbonEditorDialog(draft, _ => null);
        editor.AddQuickAccessCommand(false); var command = draft.CatalogCommands.Last();
        var properties = new RibbonEditorDialog.DraftProperties(command); TypeDescriptor.GetProperties(properties)["(Name)"]!.SetValue(properties, "btnSave");
        Assert.True(RibbonDesignCommitter.Apply(f.Ribbon, f.Host, draft));
        var created = Assert.IsType<RibbonButtonItem>(f.Host.Container.Components["btnSave"]); Assert.Same(created, RibbonQuickAccess.Source(f.Ribbon.QuickAccessItems.Last()));
    });

    private sealed class BindingValue { public string Text { get; set; } = "Save"; }
    [Theory]
    [InlineData(false, false)] [InlineData(true, false)]
    [InlineData(false, true)] [InlineData(true, true)]
    public void Host_name_service_does_not_revalidate_already_owned_names(bool backstage, bool caseOnly) => FoundationTests.Sta(() =>
    {
        using var f = new Fixture();
        var service = new OccupancyAwareNames(f.Host);
        var services = (IServiceContainer)f.Surface.GetService(typeof(IServiceContainer))!;
        services.RemoveService(typeof(INameCreationService)); services.AddService(typeof(INameCreationService), service);
        var component = backstage ? (IComponent)f.Page : f.Catalog;
        string oldName = component.Site!.Name!, nextName = caseOnly ? oldName.ToUpperInvariant() : "saveButton";
        if (backstage)
        {
            var draft = RibbonBackstageCommitter.Read(f.Ribbon, f.Host);
            draft.Entries.Single(node => node.ComponentName == oldName).ComponentName = nextName;
            Assert.True(RibbonBackstageCommitter.Apply(f.Ribbon, f.Host, draft));
        }
        else
        {
            var draft = f.Read(); draft.CatalogCommands.Single().ComponentName = nextName;
            Assert.True(RibbonDesignCommitter.Apply(f.Ribbon, f.Host, draft));
        }
        Assert.Equal(nextName, component.Site!.Name);
        Assert.Equal("homeTab", f.Ribbon.Tabs[0].Site!.Name); Assert.Same(f.Button, f.Reference.Source);
        Assert.All(service.Validated, name => Assert.Equal(nextName, name));
    });

    [Fact]
    public void Unchanged_accept_skips_the_occupancy_aware_host_validator() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); var service = new OccupancyAwareNames(f.Host);
        var services = (IServiceContainer)f.Surface.GetService(typeof(IServiceContainer))!;
        services.RemoveService(typeof(INameCreationService)); services.AddService(typeof(INameCreationService), service);
        Assert.False(RibbonDesignCommitter.Apply(f.Ribbon, f.Host, f.Read()));
        Assert.False(RibbonBackstageCommitter.Apply(f.Ribbon, f.Host, RibbonBackstageCommitter.Read(f.Ribbon, f.Host)));
        Assert.Empty(service.Validated);
    });

    [Fact]
    public void A_case_only_rename_still_rejects_a_language_keyword_before_a_transaction() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); f.Catalog.Site!.Name = "Class";
        var services = (IServiceContainer)f.Surface.GetService(typeof(IServiceContainer))!;
        services.RemoveService(typeof(INameCreationService)); services.AddService(typeof(INameCreationService), new OccupancyAwareNames(f.Host));
        var draft = f.Read(); draft.CatalogCommands.Single().ComponentName = "class";
        int transactions = 0; f.Host.TransactionOpened += (_, _) => transactions++;
        Assert.Throws<ArgumentException>(() => RibbonDesignCommitter.Apply(f.Ribbon, f.Host, draft));
        Assert.Equal("Class", f.Catalog.Site!.Name); Assert.Equal(0, transactions);
    });

    [Fact]
    public void Proposed_names_still_obey_host_rules_before_a_transaction() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); var service = new OccupancyAwareNames(f.Host, "inheritedField");
        var services = (IServiceContainer)f.Surface.GetService(typeof(IServiceContainer))!;
        services.RemoveService(typeof(INameCreationService)); services.AddService(typeof(INameCreationService), service);
        var draft = f.Read(); draft.CatalogCommands.Single().ComponentName = "inheritedField";
        int transactions = 0; f.Host.TransactionOpened += (_, _) => transactions++;
        Assert.Throws<ArgumentException>(() => RibbonDesignCommitter.Apply(f.Ribbon, f.Host, draft));
        Assert.Equal("catalog1", f.Catalog.Site!.Name); Assert.Equal(0, transactions); Assert.Contains("inheritedField", service.Validated);
    });

    private sealed class OccupancyAwareNames(IDesignerHost host, string? inherited = null) : INameCreationService
    {
        internal readonly List<string> Validated = [];
        public void ValidateName(string name)
        {
            Validated.Add(name);
            if (host.Container.Components[name] is not null || name == inherited) throw new ArgumentException("There is already a component named '" + name + "'.");
            using var provider = System.CodeDom.Compiler.CodeDomProvider.CreateProvider("CSharp");
            if (!provider.IsValidIdentifier(name)) throw new ArgumentException("Invalid identifier: " + name);
        }
        public bool IsValidName(string name) { try { ValidateName(name); return true; } catch (ArgumentException) { return false; } }
        public string CreateName(IContainer? container, Type dataType) => throw new NotSupportedException();
    }

    [Fact]
    public void Renamed_qat_sources_serialize_the_new_programmatic_field_name() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); var draft = f.Read();
        draft.Nodes.Single(node => node.ComponentName == "button1").ComponentName = "btnSave";
        draft.QuickAccessItems.Single().ComponentName = "qatSaveButton";
        RibbonDesignCommitter.Apply(f.Ribbon, f.Host, draft);
        var serialization = new DesignerSerializationManager(f.Host); using var session = serialization.CreateSession(); var manager = (IDesignerSerializationManager)serialization;
        manager.Context.Push(new CodeTypeDeclaration("NamedDesignerForm")); manager.Context.Push(new RootContext(new CodeThisReferenceExpression(), f.Host.RootComponent));
        manager.SetName(f.Ribbon, f.Ribbon.Site!.Name!); manager.SetName(f.Button, f.Button.Site!.Name!); manager.SetName(f.Reference, f.Reference.Site!.Name!);
        var serializer = (CodeDomSerializer)manager.GetSerializer(f.Reference.GetType(), typeof(CodeDomSerializer))!;
        var statements = serializer.SerializeMember(manager, f.Reference, TypeDescriptor.GetProperties(f.Reference)["Source"]!).OfType<CodeAssignStatement>();
        var assignment = Assert.Single(statements, statement => statement.Left is CodePropertyReferenceExpression { PropertyName: "Source" });
        Assert.Equal("btnSave", Assert.IsType<CodeFieldReferenceExpression>(assignment.Right).FieldName);
        Assert.Equal("qatSaveButton", manager.GetName(f.Reference)); Assert.Empty(serialization.Errors.Cast<object>());
    });

    private static void AddSerialization(Fixture f) => ((IServiceContainer)f.Surface.GetService(typeof(IServiceContainer))!).AddService(typeof(ComponentSerializationService), new CodeDomComponentSerializationService(f.Host));
    private sealed class UndoRecorder(IServiceProvider services) : UndoEngine(services)
    {
        private readonly List<UndoUnit> _units = [];
        internal int Count => _units.Count;
        internal void Toggle() => Assert.Single(_units).Undo();
        protected override void AddUndoUnit(UndoUnit unit) => _units.Add(unit);
    }
}
